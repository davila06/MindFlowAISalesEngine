namespace Api.Application.Security;

public interface IAdminAuditService
{
    Task RecordAsync(string action, string target, string details, CancellationToken cancellationToken);
}
