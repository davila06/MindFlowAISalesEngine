using Api.Infrastructure.Tenancy;
using Api.Contracts;
using Api.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Api.Middleware;

public class TenantMiddleware
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string RoleHeader = "X-User-Role";
    private const string AuthenticatedTenantHeader = "X-Authenticated-Tenant";
    private const string AuthenticatedRoleHeader = "X-Authenticated-Role";

    private readonly RequestDelegate _next;
    private readonly SecurityRuntimeOptions _securityOptions;

    public TenantMiddleware(
        RequestDelegate next,
        IOptions<SecurityRuntimeOptions> securityOptions)
    {
        _next = next;
        _securityOptions = securityOptions.Value;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        context.Request.Headers.TryGetValue(TenantHeader, out var tenantHeader);
        context.Request.Headers.TryGetValue(RoleHeader, out var roleHeader);
        context.Request.Headers.TryGetValue(AuthenticatedTenantHeader, out var authenticatedTenantHeader);
        context.Request.Headers.TryGetValue(AuthenticatedRoleHeader, out var authenticatedRoleHeader);

        var claimTenant = context.User.FindFirst("tenant_id")?.Value;
        var claimRole = context.User.FindFirst("role")?.Value;
        var trustedTenant = !string.IsNullOrWhiteSpace(claimTenant) ? claimTenant : authenticatedTenantHeader.ToString();
        var trustedRole = !string.IsNullOrWhiteSpace(claimRole) ? claimRole : authenticatedRoleHeader.ToString();

        if (_securityOptions.StrictMode)
        {
            if (!string.IsNullOrWhiteSpace(trustedTenant)
                && !string.IsNullOrWhiteSpace(tenantHeader)
                && !string.Equals(trustedTenant, tenantHeader.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new ApiErrorResponse
                {
                    Code = "tenant_context_mismatch",
                    Message = "Tenant context mismatch between token and request headers.",
                    TraceId = context.TraceIdentifier
                });
                return;
            }

            if (!string.IsNullOrWhiteSpace(trustedRole)
                && !string.IsNullOrWhiteSpace(roleHeader)
                && !string.Equals(trustedRole, roleHeader.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new ApiErrorResponse
                {
                    Code = "role_context_mismatch",
                    Message = "Role context mismatch between token and request headers.",
                    TraceId = context.TraceIdentifier
                });
                return;
            }
        }

        tenantContext.SetTenant(!string.IsNullOrWhiteSpace(trustedTenant) ? trustedTenant : tenantHeader.ToString());
        tenantContext.SetRole(!string.IsNullOrWhiteSpace(trustedRole) ? trustedRole : roleHeader.ToString());

        await _next(context);
    }
}
