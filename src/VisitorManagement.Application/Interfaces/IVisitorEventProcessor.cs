using VisitorManagement.Application.Events;

namespace VisitorManagement.Application.Interfaces;

public interface IVisitorEventProcessor
{
    Task<bool> ProcessAsync(
        VisitorCreatedEvent visitorEvent,
        CancellationToken cancellationToken = default);
}