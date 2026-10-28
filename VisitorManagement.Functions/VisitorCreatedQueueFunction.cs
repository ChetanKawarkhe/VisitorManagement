using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using VisitorManagement.Application.Events;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Functions;

public sealed class VisitorCreatedQueueFunction
{
    private readonly IVisitorEventProcessor _processor;
    private readonly ILogger<VisitorCreatedQueueFunction> _logger;

    public VisitorCreatedQueueFunction(
        IVisitorEventProcessor processor,
        ILogger<VisitorCreatedQueueFunction> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [Function(nameof(VisitorCreatedQueueFunction))]
    public async Task RunAsync(
        [QueueTrigger(
            "visitor-created",
            Connection = "AzureWebJobsStorage")]
        string message,
        CancellationToken cancellationToken)
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

        if (visitorEvent.MessageId == Guid.Empty ||
            visitorEvent.VisitorId <= 0 ||
            visitorEvent.EventType != "VisitorCreated" ||
            string.IsNullOrWhiteSpace(visitorEvent.CorrelationId))
        {
            throw new InvalidOperationException(
                "Queue message contains invalid visitor event data.");
        }

        _logger.LogInformation(
            "Processing visitor event. " +
            "MessageId: {MessageId}, " +
            "VisitorId: {VisitorId}, " +
            "CorrelationId: {CorrelationId}",
            visitorEvent.MessageId,
            visitorEvent.VisitorId,
            visitorEvent.CorrelationId);

        var processed = await _processor.ProcessAsync(
            visitorEvent,
            cancellationToken);

        if (processed)
        {
            _logger.LogInformation(
                "Visitor event processing completed. MessageId: {MessageId}",
                visitorEvent.MessageId);
        }
        else
        {
            _logger.LogInformation(
                "Duplicate visitor event ignored. MessageId: {MessageId}",
                visitorEvent.MessageId);
        }
    }
}