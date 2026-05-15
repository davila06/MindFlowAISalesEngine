using Api.Contracts;

namespace Api.Middleware;

public sealed class ApiVersioningMiddleware
{
    private const string VersionHeader = "X-Api-Version";

    private readonly RequestDelegate _next;

    public ApiVersioningMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Request.Headers.TryGetValue(VersionHeader, out var providedVersion);
        var rawVersion = providedVersion.ToString();

        if (!string.IsNullOrWhiteSpace(rawVersion)
            && !string.Equals(rawVersion, "1", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(rawVersion, "v1", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = "unsupported_api_version",
                Message = "Unsupported API version. Supported values: 1, v1.",
                TraceId = context.TraceIdentifier
            });
            return;
        }

        context.Response.Headers[VersionHeader] = "1";
        await _next(context);
    }
}
