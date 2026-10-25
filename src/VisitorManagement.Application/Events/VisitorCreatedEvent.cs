namespace VisitorManagement.Application.Events;

public sealed class VisitorCreatedEvent
{
    public Guid MessageId { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public int VisitorId { get; set; }

    public string EventType { get; set; } = "VisitorCreated";

    public DateTime OccurredAt { get; set; }
}