using System.Net.Http.Json;
using System.Text.Json;
using VisitorManagement.Web.Models;
using VisitorManagement.Web.Models.Visitors;

namespace VisitorManagement.Web.Services;

public sealed class VisitorApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public VisitorApiService(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<CreateVisitorResult> CreateAsync(
        CreateVisitorRequest visitor,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(
            "VisitorManagementApi");

        string correlationId = Guid.NewGuid().ToString("D");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/visitors");

        request.Headers.TryAddWithoutValidation(
            "X-Correlation-ID",
            correlationId);

        request.Content = JsonContent.Create(visitor);

        using var response = await client.SendAsync(
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new CreateVisitorResult
            {
                IsSuccessful = true,
                CorrelationId = correlationId
            };
        }

        ApiErrorResponse? apiError = null;

        try
        {
            apiError = await response.Content
                .ReadFromJsonAsync<ApiErrorResponse>(
                    cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            // Preserve the original correlation ID.
        }
        catch (NotSupportedException)
        {
            // The API returned a non-JSON response.
        }

        return new CreateVisitorResult
        {
            IsSuccessful = false,
            CorrelationId =
                string.IsNullOrWhiteSpace(apiError?.CorrelationId)
                    ? correlationId
                    : apiError.CorrelationId,
            Errors = apiError?.Errors
                ?? new Dictionary<string, string[]>()
        };
    }
}

public sealed class CreateVisitorResult
{
    public bool IsSuccessful { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public Dictionary<string, string[]> Errors { get; set; } = new();
}