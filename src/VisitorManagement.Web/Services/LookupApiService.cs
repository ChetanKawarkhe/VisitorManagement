using System.Net.Http.Json;
using VisitorManagement.Web.Models.Lookups;

namespace VisitorManagement.Web.Services;

public sealed class LookupApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public LookupApiService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<HostResponse>> SearchHostsAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(
            "VisitorManagementApi");

        string url = string.IsNullOrWhiteSpace(search)
            ? "api/hosts"
            : $"api/hosts?search={Uri.EscapeDataString(search.Trim())}";

        var hosts = await client.GetFromJsonAsync<List<HostResponse>>(
            url,
            cancellationToken);

        return hosts ?? [];
    }

    public async Task<IReadOnlyList<PurposeResponse>> GetPurposesAsync(
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(
            "VisitorManagementApi");

        var purposes =
            await client.GetFromJsonAsync<List<PurposeResponse>>(
                "api/purposes",
                cancellationToken);

        return purposes ?? [];
    }
}