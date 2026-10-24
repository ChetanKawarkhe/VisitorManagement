using VisitorManagement.Application.DTOs.Lookups;

namespace VisitorManagement.Application.Interfaces;

public interface ILookupService
{
    Task<IReadOnlyList<HostResponse>> SearchHostsAsync(
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurposeResponse>> GetPurposesAsync(
        CancellationToken cancellationToken = default);
}