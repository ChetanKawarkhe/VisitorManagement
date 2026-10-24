using VisitorManagement.Domain.Entities;

namespace VisitorManagement.Application.Interfaces;

public interface IPurposeRepository
{
    Task<IReadOnlyList<VisitPurpose>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAndActiveAsync(
        int id,
        CancellationToken cancellationToken = default);
}