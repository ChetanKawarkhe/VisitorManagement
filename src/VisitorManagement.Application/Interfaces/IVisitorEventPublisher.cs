using VisitorManagement.Application.Events;

namespace VisitorManagement.Application.Interfaces;

public interface IVisitorEventPublisher
{
    Task PublishAsync(
        VisitorCreatedEvent visitorEvent,
        CancellationToken cancellationToken = default);
}