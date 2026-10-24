using VisitorManagement.Application.DTOs.Lookups;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Application.Services;

public sealed class LookupService : ILookupService
{
    private readonly IHostRepository _hostRepository;
    private readonly IPurposeRepository _purposeRepository;

    public LookupService(
        IHostRepository hostRepository,
        IPurposeRepository purposeRepository)
    {
        _hostRepository = hostRepository;
        _purposeRepository = purposeRepository;
    }

    public async Task<IReadOnlyList<HostResponse>> SearchHostsAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var hosts = await _hostRepository.SearchActiveAsync(
            search,
            10,
            cancellationToken);

        return hosts.Select(host => new HostResponse
        {
            Id = host.Id,
            FullName = host.FullName,
            Email = host.Email,
            Department = host.Department
        }).ToList();
    }

    public async Task<IReadOnlyList<PurposeResponse>> GetPurposesAsync(
        CancellationToken cancellationToken = default)
    {
        var purposes = await _purposeRepository.GetActiveAsync(
            cancellationToken);

        return purposes.Select(purpose => new PurposeResponse
        {
            Id = purpose.Id,
            Name = purpose.Name
        }).ToList();
    }
}