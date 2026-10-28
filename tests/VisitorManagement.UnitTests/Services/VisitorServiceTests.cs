using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using VisitorManagement.Application.DTOs.Visitors;
using VisitorManagement.Application.Events;
using VisitorManagement.Application.Interfaces;
using VisitorManagement.Application.Services;
using VisitorManagement.Domain.Entities;
using VisitorManagement.Domain.Enums;

namespace VisitorManagement.UnitTests.Services;

public sealed class VisitorServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidRequest_SavesVisitorAndPublishesEvent()
    {
        // ARRANGE

        const int currentUserId = 1;
        const int savedVisitorId = 101;

        const string correlationId = "unit-test-correlation-001";

        var request = new CreateVisitorRequest
        {
            VisitorName = "  Rahul Sharma  ",
            CompanyName = "  ABC Technologies  ",
            MobileNumber = "9876543210",
            Email = "rahul@example.com",
            VisitDate = DateTime.UtcNow.AddDays(1),
            HostId = 1,
            PurposeId = 1,
            Remarks = "Business meeting"
        };

        var visitorRepository = new Mock<IVisitorRepository>();

        var hostRepository = new Mock<IHostRepository>();

        var purposeRepository = new Mock<IPurposeRepository>();

        var auditLogRepository = new Mock<IAuditLogRepository>();

        var unitOfWork = new Mock<IUnitOfWork>();

        var correlationContext = new Mock<ICorrelationContext>();

        var createValidator = new Mock<IValidator<CreateVisitorRequest>>();

        var updateValidator = new Mock<IValidator<UpdateVisitorRequest>>();

        var eventPublisher = new Mock<IVisitorEventPublisher>();

        var logger = new Mock<ILogger<VisitorService>>();

        correlationContext
            .Setup(x => x.CorrelationId)
            .Returns(correlationId);

        createValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<ValidationContext<CreateVisitorRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        hostRepository
            .Setup(x => x.ExistsAndActiveAsync(
                request.HostId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        purposeRepository
            .Setup(x => x.ExistsAndActiveAsync(
                request.PurposeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (Func<CancellationToken, Task> operation,
                 CancellationToken cancellationToken) =>
                    operation(cancellationToken));

        Visitor? savedVisitor = null;

        visitorRepository
            .Setup(x => x.AddAsync(
                It.IsAny<Visitor>(),
                It.IsAny<CancellationToken>()))
            .Callback<Visitor, CancellationToken>(
                (visitor, _) =>
                {
                    visitor.Id = savedVisitorId;
                    savedVisitor = visitor;
                })
            .Returns(Task.CompletedTask);

        visitorRepository
            .Setup(x => x.GetByIdAsync(
                savedVisitorId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedVisitor);

        auditLogRepository
            .Setup(x => x.AddAsync(
                It.IsAny<AuditLog>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        eventPublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<VisitorCreatedEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new VisitorService(
            visitorRepository.Object,
            hostRepository.Object,
            purposeRepository.Object,
            auditLogRepository.Object,
            unitOfWork.Object,
            correlationContext.Object,
            createValidator.Object,
            updateValidator.Object,
            eventPublisher.Object,
            logger.Object);

        // ACT

        var result = await service.CreateAsync(
            request,
            currentUserId);

        // ASSERT

        Assert.NotNull(result);

        Assert.Equal(savedVisitorId, result.Id);

        Assert.Equal("Rahul Sharma", result.VisitorName);

        Assert.Equal("ABC Technologies", result.CompanyName);

        Assert.Equal("Scheduled", result.Status);

        Assert.Equal(currentUserId, result.CreatedByUserId);

        unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        visitorRepository.Verify(
            x => x.AddAsync(
                It.Is<Visitor>(v =>
                    v.Id == savedVisitorId &&
                    v.VisitorName == "Rahul Sharma" &&
                    v.CompanyName == "ABC Technologies" &&
                    v.Status == VisitorStatus.Scheduled &&
                    v.CreatedByUserId == currentUserId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        auditLogRepository.Verify(
            x => x.AddAsync(
                It.Is<AuditLog>(a =>
                    a.VisitorId == savedVisitorId &&
                    a.Action == "VisitorCreated" &&
                    a.Source == "DB" &&
                    a.PerformedByUserId == currentUserId &&
                    a.CorrelationId == correlationId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        eventPublisher.Verify(
            x => x.PublishAsync(
                It.Is<VisitorCreatedEvent>(e =>
                    e.VisitorId == savedVisitorId &&
                    e.EventType == "VisitorCreated" &&
                    e.CorrelationId == correlationId &&
                    e.MessageId != Guid.Empty),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_QueuePublishingFails_StillReturnsSavedVisitor()
    {
        // ARRANGE

        const int savedVisitorId = 102;
        const int currentUserId = 1;

        var request = new CreateVisitorRequest
        {
            VisitorName = "Priya Patil",
            CompanyName = "XYZ Solutions",
            MobileNumber = "9876543210",
            VisitDate = DateTime.UtcNow.AddDays(1),
            HostId = 1,
            PurposeId = 1
        };

        var visitorRepository = new Mock<IVisitorRepository>();
        var hostRepository = new Mock<IHostRepository>();
        var purposeRepository = new Mock<IPurposeRepository>();
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var correlationContext = new Mock<ICorrelationContext>();
        var createValidator = new Mock<IValidator<CreateVisitorRequest>>();
        var updateValidator = new Mock<IValidator<UpdateVisitorRequest>>();
        var eventPublisher = new Mock<IVisitorEventPublisher>();
        var logger = new Mock<ILogger<VisitorService>>();

        correlationContext
            .Setup(x => x.CorrelationId)
            .Returns("queue-failure-test");

        createValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<ValidationContext<CreateVisitorRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        hostRepository
            .Setup(x => x.ExistsAndActiveAsync(
                request.HostId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        purposeRepository
            .Setup(x => x.ExistsAndActiveAsync(
                request.PurposeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (Func<CancellationToken, Task> operation,
                 CancellationToken cancellationToken) =>
                    operation(cancellationToken));

        Visitor? savedVisitor = null;

        visitorRepository
            .Setup(x => x.AddAsync(
                It.IsAny<Visitor>(),
                It.IsAny<CancellationToken>()))
            .Callback<Visitor, CancellationToken>(
                (visitor, _) =>
                {
                    visitor.Id = savedVisitorId;
                    savedVisitor = visitor;
                })
            .Returns(Task.CompletedTask);

        visitorRepository
            .Setup(x => x.GetByIdAsync(
                savedVisitorId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedVisitor);

        auditLogRepository
            .Setup(x => x.AddAsync(
                It.IsAny<AuditLog>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Simulate Azure Queue being unavailable.
        eventPublisher
            .Setup(x => x.PublishAsync(
                It.IsAny<VisitorCreatedEvent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Azure Queue is unavailable."));

        var service = new VisitorService(
            visitorRepository.Object,
            hostRepository.Object,
            purposeRepository.Object,
            auditLogRepository.Object,
            unitOfWork.Object,
            correlationContext.Object,
            createValidator.Object,
            updateValidator.Object,
            eventPublisher.Object,
            logger.Object);

        // ACT

        var result = await service.CreateAsync(
            request,
            currentUserId);

        // ASSERT

        Assert.NotNull(result);
        Assert.Equal(savedVisitorId, result.Id);
        Assert.Equal("Priya Patil", result.VisitorName);
        Assert.Equal("Scheduled", result.Status);

        visitorRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Visitor>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        auditLogRepository.Verify(
            x => x.AddAsync(
                It.IsAny<AuditLog>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        eventPublisher.Verify(
            x => x.PublishAsync(
                It.IsAny<VisitorCreatedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}