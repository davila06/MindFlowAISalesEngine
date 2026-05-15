using Api.Application.Security;
using Api.Domain.Security;
using Api.Infrastructure.Persistence;
using Api.Infrastructure.Tenancy;

namespace Api.Infrastructure.Security;

public sealed class AdminAuditService : IAdminAuditService
{
    private readonly LeadsDbContext _dbContext;
    private readonly TenantContext _tenantContext;

    public AdminAuditService(LeadsDbContext dbContext, TenantContext tenantContext)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
    }

    public async Task RecordAsync(string action, string target, string details, CancellationToken cancellationToken)
    {
        var entry = new AdminAuditLog(
            action,
            target,
            details,
            _tenantContext.TenantId,
            _tenantContext.UserRole);

        await _dbContext.AdminAuditLogs.AddAsync(entry, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
