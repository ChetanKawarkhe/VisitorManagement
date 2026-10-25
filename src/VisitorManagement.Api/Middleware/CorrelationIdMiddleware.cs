using System.Text.RegularExpressions;
using Serilog.Context;

namespace VisitorManagement.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    public const string ItemKey = "CorrelationId";

    private static readonly Regex AllowedCharacters = new(
        @"^[A-Za-z0-9_-]{1,100}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? incomingCorrelationId =
            context.Request.Headers[HeaderName].FirstOrDefault();

        string correlationId =
            IsValidCorrelationId(incomingCorrelationId)
                ? incomingCorrelationId!
                : Guid.NewGuid().ToString("D");

        context.Items[ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;

            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(
            "CorrelationId",
            correlationId))
        using (LogContext.PushProperty(
            "Source",
            "API"))
        {
            await _next(context);
        }
    }

    private static bool IsValidCorrelationId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               AllowedCharacters.IsMatch(value);
    }
}