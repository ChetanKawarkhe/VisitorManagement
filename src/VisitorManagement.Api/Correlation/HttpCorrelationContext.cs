using VisitorManagement.Api.Middleware;
using VisitorManagement.Application.Interfaces;

namespace VisitorManagement.Api.Correlation;

public sealed class HttpCorrelationContext : ICorrelationContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            if (context is not null &&
                context.Items.TryGetValue(
                    CorrelationIdMiddleware.ItemKey,
                    out var value) &&
                value is string correlationId)
            {
                return correlationId;
            }

            return Guid.NewGuid().ToString("D");
        }
    }
}