using FluentValidation;
using Microsoft.Extensions.Logging;
using VisitorManagement.Application.DTOs.Visitors;
using VisitorManagement.Application.Events;
using VisitorManagement.Application.Exceptions;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Domain.Entities;
using VisitorManagement.Domain.Enums;

namespace VisitorManagement.Application.Services;

public sealed class VisitorService : IVisitorService
{
    private readonly IVisitorRepository _visitorRepository;
    private readonly IHostRepository _hostRepository;
    private readonly IPurposeRepository _purposeRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICorrelationContext _correlationContext;
    private readonly IValidator<CreateVisitorRequest> _createValidator;
    private readonly IValidator<UpdateVisitorRequest> _updateValidator;
    private readonly IVisitorEventPublisher _eventPublisher;
    private readonly ILogger<VisitorService> _logger;

    public VisitorService(
        IVisitorRepository visitorRepository,
        IHostRepository hostRepository,
        IPurposeRepository purposeRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ICorrelationContext correlationContext,
        IValidator<CreateVisitorRequest> createValidator,
        IValidator<UpdateVisitorRequest> updateValidator,
        IVisitorEventPublisher eventPublisher,
        ILogger<VisitorService> logger)
    {
        _visitorRepository = visitorRepository;
        _hostRepository = hostRepository;
        _purposeRepository = purposeRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _correlationContext = correlationContext;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<VisitorResponse> CreateAsync(
        CreateVisitorRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        await ValidateReferencesAsync(
            request.HostId,
            request.PurposeId,
            cancellationToken);

        var visitor = new Visitor
        {
            VisitorName = request.VisitorName.Trim(),
            CompanyName = request.CompanyName.Trim(),
            MobileNumber = request.MobileNumber.Trim(),
            Email = NormalizeOptional(request.Email),
            VisitDate = request.VisitDate,
            HostId = request.HostId,
            PurposeId = request.PurposeId,
            Remarks = NormalizeOptional(request.Remarks),
            Status = VisitorStatus.Scheduled,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUserId
        };

        // Save visitor and database audit log atomically.
        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _visitorRepository.AddAsync(visitor, ct);

                await _auditLogRepository.AddAsync(
                    new AuditLog
                    {
                        VisitorId = visitor.Id,
                        Action = "VisitorCreated",
                        PerformedByUserId = currentUserId,
                        CorrelationId = _correlationContext.CorrelationId,
                        Source = "DB",
                        Timestamp = DateTime.UtcNow
                    },
                    ct);
            },
            cancellationToken);

        var savedVisitor = await _visitorRepository.GetByIdAsync(
            visitor.Id,
            cancellationToken);

        if (savedVisitor is null)
        {
            throw new NotFoundException(
                "The saved visitor record could not be retrieved.");
        }

        var response = MapToResponse(savedVisitor);

        // Publish only after the database transaction has committed.
        var visitorEvent = new VisitorCreatedEvent
        {
            MessageId = Guid.NewGuid(),
            CorrelationId = _correlationContext.CorrelationId,
            VisitorId = visitor.Id,
            EventType = "VisitorCreated",
            OccurredAt = DateTime.UtcNow
        };

        try
        {
            await _eventPublisher.PublishAsync(
                visitorEvent,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Queue failure must not undo a successfully saved visitor.
            _logger.LogWarning(
                ex,
                "Visitor saved but queue publication failed. " +
                "VisitorId: {VisitorId}, MessageId: {MessageId}, " +
                "CorrelationId: {CorrelationId}",
                visitor.Id,
                visitorEvent.MessageId,
                visitorEvent.CorrelationId);
        }

        return response;
    }

    public async Task<VisitorResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var visitor = await _visitorRepository.GetByIdAsync(
            id,
            cancellationToken);

        return visitor is null
            ? null
            : MapToResponse(visitor);
    }

    public async Task<PagedResult<VisitorResponse>> SearchAsync(
        VisitorSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        int page = Math.Max(request.Page, 1);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        string sortBy = request.SortBy?.Trim() ?? "VisitDate";

        if (!sortBy.Equals(
                "VisitorName",
                StringComparison.OrdinalIgnoreCase) &&
            !sortBy.Equals(
                "VisitDate",
                StringComparison.OrdinalIgnoreCase))
        {
            sortBy = "VisitDate";
        }

        var result = await _visitorRepository.SearchAsync(
            request.Search,
            sortBy,
            request.Descending,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<VisitorResponse>
        {
            Items = result.Items
                .Select(MapToResponse)
                .ToList(),

            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<VisitorResponse> UpdateAsync(
        int id,
        UpdateVisitorRequest request,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        await ValidateReferencesAsync(
            request.HostId,
            request.PurposeId,
            cancellationToken);

        var existing = await _visitorRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (existing is null)
        {
            throw new NotFoundException(
                "Visitor record was not found.");
        }

        byte[] expectedRowVersion =
            Convert.FromBase64String(request.RowVersion);

        if (!existing.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw new ConcurrencyConflictException(
                "This visitor record was modified by another user.");
        }

        existing.VisitorName = request.VisitorName.Trim();
        existing.CompanyName = request.CompanyName.Trim();
        existing.MobileNumber = request.MobileNumber.Trim();
        existing.Email = NormalizeOptional(request.Email);
        existing.VisitDate = request.VisitDate;
        existing.HostId = request.HostId;
        existing.PurposeId = request.PurposeId;
        existing.Remarks = NormalizeOptional(request.Remarks);

        existing.Status = Enum.Parse<VisitorStatus>(
            request.Status,
            true);

        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedByUserId = currentUserId;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(
                async ct =>
                {
                    await _visitorRepository.UpdateAsync(
                        existing,
                        expectedRowVersion,
                        ct);

                    await _auditLogRepository.AddAsync(
                        new AuditLog
                        {
                            VisitorId = existing.Id,
                            Action = "VisitorUpdated",
                            PerformedByUserId = currentUserId,
                            CorrelationId =
                                _correlationContext.CorrelationId,
                            Source = "DB",
                            Timestamp = DateTime.UtcNow
                        },
                        ct);
                },
                cancellationToken);
        }
        catch (Exception ex) when (
            ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            throw new ConcurrencyConflictException(
                "This visitor record was modified by another user.");
        }

        var updated = await _visitorRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (updated is null)
        {
            throw new NotFoundException(
                "Updated visitor record was not found.");
        }

        return MapToResponse(updated);
    }

    private async Task ValidateReferencesAsync(
        int hostId,
        int purposeId,
        CancellationToken cancellationToken)
    {
        var failures =
            new List<FluentValidation.Results.ValidationFailure>();

        if (!await _hostRepository.ExistsAndActiveAsync(
            hostId,
            cancellationToken))
        {
            failures.Add(
                new FluentValidation.Results.ValidationFailure(
                    "HostId",
                    "Please select a valid active host."));
        }

        if (!await _purposeRepository.ExistsAndActiveAsync(
            purposeId,
            cancellationToken))
        {
            failures.Add(
                new FluentValidation.Results.ValidationFailure(
                    "PurposeId",
                    "Please select a valid active visit purpose."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static VisitorResponse MapToResponse(Visitor visitor)
    {
        return new VisitorResponse
        {
            Id = visitor.Id,
            VisitorName = visitor.VisitorName,
            CompanyName = visitor.CompanyName,
            MobileNumber = visitor.MobileNumber,
            Email = visitor.Email,
            VisitDate = visitor.VisitDate,
            HostId = visitor.HostId,
            HostName = visitor.Host?.FullName ?? string.Empty,
            PurposeId = visitor.PurposeId,
            PurposeName = visitor.Purpose?.Name ?? string.Empty,
            Remarks = visitor.Remarks,
            Status = visitor.Status.ToString(),
            CreatedAt = visitor.CreatedAt,
            CreatedByUserId = visitor.CreatedByUserId,
            UpdatedAt = visitor.UpdatedAt,
            RowVersion = Convert.ToBase64String(
                visitor.RowVersion)
        };
    }
}