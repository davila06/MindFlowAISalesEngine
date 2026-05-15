using Api.Application.Common.Security;
using Api.Contracts;
using Api.Infrastructure.Security;
using Api.Infrastructure.Tenancy;
using Microsoft.Extensions.Options;

namespace Api.Middleware;

public class RoleAuthorizationMiddleware
{
    private static readonly string[] AdminOperationalPaths =
    [
        "/api/analytics/advanced/metrics/history/snapshot",
        "/api/proposals/reminders/execute-due",
        "/api/proposals/",
        "/api/followup/",
        "/api/onboarding/welcome-jobs/"
    ];

    private readonly RequestDelegate _next;
    private readonly SecurityRuntimeOptions _securityOptions;

    public RoleAuthorizationMiddleware(
        RequestDelegate next,
        IOptions<SecurityRuntimeOptions> securityOptions)
    {
        _next = next;
        _securityOptions = securityOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (_securityOptions.StrictMode
            && path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            && !path.Equals("/api/leads/intake", StringComparison.OrdinalIgnoreCase)
            && !(context.User.Identity?.IsAuthenticated ?? false)
            && string.IsNullOrWhiteSpace(context.Request.Headers["X-Authenticated-Role"].ToString())
            && string.IsNullOrWhiteSpace(context.Request.Headers["X-User-Role"].ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = "unauthorized",
                Message = "Authentication is required.",
                TraceId = context.TraceIdentifier
            });
            return;
        }

        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            && method != HttpMethods.Get
            && tenantContext.UserRole == UserRoles.Viewer)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = "forbidden",
                Message = "Viewer role cannot perform write operations.",
                TraceId = context.TraceIdentifier
            });
            return;
        }

        if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
            && AdminOperationalPaths.Any(prefix =>
                path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || (prefix.EndsWith('/') && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            && tenantContext.UserRole != UserRoles.Admin)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse
            {
                Code = "admin_required",
                Message = "This operational endpoint requires Admin role.",
                TraceId = context.TraceIdentifier
            });
            return;
        }

        await _next(context);
    }
}
