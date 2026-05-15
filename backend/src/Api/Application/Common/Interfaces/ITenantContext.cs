namespace Api.Application.Common.Interfaces;

public interface ITenantContext
{
    string TenantId { get; }
    string UserRole { get; }
}
