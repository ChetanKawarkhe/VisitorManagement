using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using VisitorManagement.Application.Events;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Infrastructure.Messaging;

public sealed class AzureQueueVisitorEventPublisher : IVisitorEventPublisher
{
    private const string QueueName = "visitor-created";

    private readonly QueueClient _queueClient;
    private readonly ILogger<AzureQueueVisitorEventPublisher> _logger;

    public AzureQueueVisitorEventPublisher(
        QueueServiceClient queueServiceClient,
        ILogger<AzureQueueVisitorEventPublisher> logger)
    {
        _queueClient = queueServiceClient.GetQueueClient(QueueName);
        _logger = logger;
    }

    public async Task PublishAsync(
        VisitorCreatedEvent visitorEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(visitorEvent);

        await _queueClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        string messageJson = JsonSerializer.Serialize(visitorEvent);

        await _queueClient.SendMessageAsync(
            messageJson,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Visitor event published. MessageId: {MessageId}, VisitorId: {VisitorId}, CorrelationId: {CorrelationId}",
            visitorEvent.MessageId,
            visitorEvent.VisitorId,
            visitorEvent.CorrelationId);
    }
}