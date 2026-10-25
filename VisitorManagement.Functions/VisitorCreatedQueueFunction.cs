using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VisitorManagement.Application.Events;

namespace VisitorManagement.Functions;

public sealed class VisitorCreatedQueueFunction
{
    private readonly ILogger<VisitorCreatedQueueFunction> _logger;

    public VisitorCreatedQueueFunction(
        ILogger<VisitorCreatedQueueFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(VisitorCreatedQueueFunction))]
    public void Run(
        [QueueTrigger(
            "visitor-created",
            Connection = "AzureWebJobsStorage")]
        string message)
    {
        var visitorEvent =
            JsonSerializer.Deserialize<VisitorCreatedEvent>(
                message,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (visitorEvent is null)
        {
            throw new InvalidOperationException(
                "Queue message could not be deserialized.");
        }

        if (visitorEvent.MessageId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Queue message has an invalid MessageId.");
        }

        if (visitorEvent.VisitorId <= 0)
        {
            throw new InvalidOperationException(
                "Queue message has an invalid VisitorId.");
        }

        if (visitorEvent.EventType != "VisitorCreated")
        {
            throw new InvalidOperationException(
                "Unsupported visitor event type.");
        }

        if (string.IsNullOrWhiteSpace(visitorEvent.CorrelationId))
        {
            throw new InvalidOperationException(
                "Queue message has no CorrelationId.");
        }

        _logger.LogInformation(
            "VisitorCreated event received. " +
            "MessageId: {MessageId}, " +
            "CorrelationId: {CorrelationId}, " +
            "VisitorId: {VisitorId}, " +
            "EventType: {EventType}",
            visitorEvent.MessageId,
            visitorEvent.CorrelationId,
            visitorEvent.VisitorId,
            visitorEvent.EventType);
    }
}