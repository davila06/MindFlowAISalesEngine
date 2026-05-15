using Api.Contracts;
using Api.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Api.Middleware;

public sealed class LeadIntakeApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityRuntimeOptions _options;

    public LeadIntakeApiKeyMiddleware(
        RequestDelegate next,
        IOptions<SecurityRuntimeOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!_options.StrictMode || !path.Equals("/api/leads/intake", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.LeadIntakeApiKey))
        {
            await _next(context);
            return;
        }

        var provided = context.Request.Headers["X-Api-Key"].ToString();
        if (!string.Equals(provided, _options.LeadIntakeApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = "invalid_api_key",
                Message = "A valid API key is required for lead intake.",
                TraceId = context.TraceIdentifier
            });
            return;
        }

        await _next(context);
    }
}
