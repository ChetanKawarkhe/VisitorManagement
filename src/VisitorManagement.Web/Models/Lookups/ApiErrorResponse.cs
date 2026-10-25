namespace VisitorManagement.Web.Models;

public sealed class ApiErrorResponse
{
    public int Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public Dictionary<string, string[]> Errors { get; set; } = new();
}