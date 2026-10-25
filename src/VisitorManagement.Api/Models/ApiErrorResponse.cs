using System.Text.Json.Serialization;

namespace VisitorManagement.Api.Models;

public sealed class ApiErrorResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [JsonPropertyName("errors")]
    public Dictionary<string, string[]> Errors { get; set; } = new();

    public static ApiErrorResponse Create(
        int status,
        string title,
        string message,
        string correlationId,
        Dictionary<string, string[]>? errors = null)
    {
        return new ApiErrorResponse
        {
            Status = status,
            Title = title,
            Message = message,
            CorrelationId = correlationId,
            Errors = errors ?? new Dictionary<string, string[]>()
        };
    }
}