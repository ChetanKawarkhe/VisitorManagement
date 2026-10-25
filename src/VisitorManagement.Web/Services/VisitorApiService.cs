using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Models.Visitors;

namespace VisitorManagement.Web.Services;

public sealed class VisitorApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public VisitorApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<CreateVisitorResult> CreateAsync(
        CreateVisitorRequest visitor,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("VisitorManagementApi");
        string correlationId = Guid.NewGuid().ToString("D");

        using var request = new HttpRequestMessage(
            HttpMethod.Post, "api/visitors");

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID", correlationId);

        request.Content = JsonContent.Create(visitor);

        using var response = await client.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new CreateVisitorResult
            {
                IsSuccessful = true,
                CorrelationId = correlationId
            };
        }

        var error = await ReadErrorAsync(response, cancellationToken);

        return new CreateVisitorResult
        {
            IsSuccessful = false,
            CorrelationId = GetCorrelationId(response, error, correlationId),
            Errors = error?.Errors ?? new Dictionary<string, string[]>()
        };
    }

    public async Task<PagedResult<VisitorResponse>> SearchAsync(
        VisitorSearchRequest searchRequest,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchRequest);

        var client = _httpClientFactory.CreateClient("VisitorManagementApi");

        var queryParameters = new List<string>
        {
            $"SortBy={Uri.EscapeDataString(searchRequest.SortBy)}",
            $"Descending={searchRequest.Descending.ToString().ToLowerInvariant()}",
            $"Page={searchRequest.Page}",
            $"PageSize={searchRequest.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(searchRequest.Search))
        {
            queryParameters.Add(
                $"Search={Uri.EscapeDataString(searchRequest.Search.Trim())}");
        }

        string url = "api/visitors?" + string.Join("&", queryParameters);

        using var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<PagedResult<VisitorResponse>>(
                cancellationToken: cancellationToken)
            ?? new PagedResult<VisitorResponse>();
    }

    // NEW: Get one visitor.
    public async Task<VisitorResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("VisitorManagementApi");

        using var response = await client.GetAsync(
            $"api/visitors/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<VisitorResponse>(
                cancellationToken: cancellationToken);
    }

    // NEW: Update visitor and preserve concurrency errors.
    public async Task<UpdateVisitorResult> UpdateAsync(
        int id,
        UpdateVisitorRequest visitor,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("VisitorManagementApi");
        string correlationId = Guid.NewGuid().ToString("D");

        using var request = new HttpRequestMessage(
            HttpMethod.Put, $"api/visitors/{id}");

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID", correlationId);

        request.Content = JsonContent.Create(visitor);

        using var response = await client.SendAsync(
            request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new UpdateVisitorResult
            {
                IsSuccessful = true,
                CorrelationId = GetCorrelationId(response, null, correlationId)
            };
        }

        var error = await ReadErrorAsync(response, cancellationToken);

        return new UpdateVisitorResult
        {
            IsSuccessful = false,
            IsConcurrencyConflict =
                response.StatusCode == HttpStatusCode.Conflict,
            IsNotFound = response.StatusCode == HttpStatusCode.NotFound,
            CorrelationId = GetCorrelationId(response, error, correlationId),
            Errors = error?.Errors ?? new Dictionary<string, string[]>()
        };
    }

    private static async Task<ApiErrorResponse?> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>(
                    cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string GetCorrelationId(
        HttpResponseMessage response,
        ApiErrorResponse? error,
        string fallback)
    {
        if (!string.IsNullOrWhiteSpace(error?.CorrelationId))
        {
            return error.CorrelationId;
        }

        if (response.Headers.TryGetValues(
            "X-Correlation-ID", out var values))
        {
            var headerValue = values.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue;
            }
        }

        return fallback;
    }
}

public sealed class CreateVisitorResult
{
    public bool IsSuccessful { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public Dictionary<string, string[]> Errors { get; set; } = new();
}

public sealed class UpdateVisitorResult
{
    public bool IsSuccessful { get; set; }

    public bool IsConcurrencyConflict { get; set; }

    public bool IsNotFound { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public Dictionary<string, string[]> Errors { get; set; } = new();
}