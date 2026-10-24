using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Application.Interfaces;

public interface IVisitorRepository
{
    Task<Visitor?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Visitor> Items, int TotalCount)> SearchAsync(
        string? search,
        string sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Visitor visitor,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Visitor visitor,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        int id,
        CancellationToken cancellationToken = default);
}