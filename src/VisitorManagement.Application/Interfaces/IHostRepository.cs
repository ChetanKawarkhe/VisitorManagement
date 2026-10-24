using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Application.Interfaces;

public interface IHostRepository
{
    Task<IReadOnlyList<Host>> SearchActiveAsync(
        string? search,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAndActiveAsync(
        int id,
        CancellationToken cancellationToken = default);
}