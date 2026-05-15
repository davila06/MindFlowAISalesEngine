namespace Api.Application.DataGovernance;

public interface ITenantDataGovernanceStore
{
    DataGovernanceOptions GetOrDefault(string tenantId, DataGovernanceOptions defaults);
    DataGovernanceOptions Set(string tenantId, DataGovernanceOptions settings);
}
