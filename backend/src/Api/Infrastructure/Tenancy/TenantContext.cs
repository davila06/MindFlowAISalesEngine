using Api.Application.Common.Interfaces;
using Api.Application.Common.Security;

namespace Api.Infrastructure.Tenancy;

public class TenantContext : ITenantContext
{
    public const string DefaultTenantId = "default";

    public string TenantId { get; private set; } = DefaultTenantId;
    public string UserRole { get; private set; } = UserRoles.Admin;

    public void SetTenant(string? tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            TenantId = DefaultTenantId;
            return;
        }

        TenantId = tenantId.Trim().ToLowerInvariant();
    }

    public void SetRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            UserRole = UserRoles.Admin;
            return;
        }

        var normalized = role.Trim();
        UserRole = UserRoles.IsValid(normalized) ? normalized : UserRoles.Admin;
    }
}
