namespace Api.Domain.Security;

public sealed class AdminAuditLog
{
    public Guid Id { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Target { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public string TenantId { get; private set; } = string.Empty;
    public string UserRole { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private AdminAuditLog() { }

    public AdminAuditLog(string action, string target, string details, string tenantId, string userRole)
    {
        Id = Guid.NewGuid();
        Action = action;
        Target = target;
        Details = details;
        TenantId = tenantId;
        UserRole = userRole;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
