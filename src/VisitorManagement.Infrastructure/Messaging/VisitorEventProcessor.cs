using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VisitorManagement.Application.Events;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Domain.Entities;
using VisitorManagement.Infrastructure.Persistence;

namespace VisitorManagement.Infrastructure.Messaging;

public sealed class VisitorEventProcessor : IVisitorEventProcessor
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VisitorEventProcessor> _logger;

    public VisitorEventProcessor(
        ApplicationDbContext context,
        ILogger<VisitorEventProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(
        VisitorCreatedEvent visitorEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visitorEvent);

        if (visitorEvent.MessageId == Guid.Empty ||
            visitorEvent.VisitorId <= 0 ||
            visitorEvent.EventType != "VisitorCreated" ||
            string.IsNullOrWhiteSpace(visitorEvent.CorrelationId))
        {
            throw new ArgumentException(
                "The visitor event is invalid.",
                nameof(visitorEvent));
        }

        // Fast check for messages that have already been processed.
        if (await _context.ProcessedMessages
            .AsNoTracking()
            .AnyAsync(
                x => x.MessageId == visitorEvent.MessageId,
                cancellationToken))
        {
            _logger.LogInformation(
                "Duplicate visitor event skipped. MessageId: {MessageId}",
                visitorEvent.MessageId);

            return false;
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var visitorExists = await _context.Visitors
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == visitorEvent.VisitorId,
                    cancellationToken);

            if (!visitorExists)
            {
                throw new InvalidOperationException(
                    "The visitor referenced by the event does not exist.");
            }

            var processedMessage = new ProcessedMessage
            {
                MessageId = visitorEvent.MessageId,
                VisitorId = visitorEvent.VisitorId,
                EventType = visitorEvent.EventType,
                ProcessedAt = DateTime.UtcNow
            };

            var auditLog = new AuditLog
            {
                VisitorId = visitorEvent.VisitorId,
                Action = "VisitorCreatedEventProcessed",
                PerformedByUserId = null,
                CorrelationId = visitorEvent.CorrelationId,
                Source = "FUNCTION",
                Timestamp = DateTime.UtcNow
            };

            await _context.ProcessedMessages.AddAsync(
                processedMessage,
                cancellationToken);

            await _context.AuditLogs.AddAsync(
                auditLog,
                cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            // Simulated notification only. No real email or SMS is sent.
            _logger.LogInformation(
                "Simulated visitor notification completed. " +
                "VisitorId: {VisitorId}, MessageId: {MessageId}",
                visitorEvent.VisitorId,
                visitorEvent.MessageId);

            _logger.LogInformation(
                "Visitor event processed successfully. " +
                "VisitorId: {VisitorId}, " +
                "MessageId: {MessageId}, " +
                "CorrelationId: {CorrelationId}",
                visitorEvent.VisitorId,
                visitorEvent.MessageId,
                visitorEvent.CorrelationId);

            return true;
        }
        catch (DbUpdateException exception)
            when (IsDuplicateKeyException(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            // A concurrent Function invocation may have inserted
            // the same MessageId after our initial duplicate check.
            _context.ChangeTracker.Clear();

            _logger.LogInformation(
                "Concurrent duplicate visitor event skipped. " +
                "MessageId: {MessageId}",
                visitorEvent.MessageId);

            return false;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsDuplicateKeyException(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               sqlException.Number is 2601 or 2627;
    }
}