namespace Api.Application.Scoring;

public interface ILeadPriorityThresholdStore
{
    Task<LeadPriorityThresholds> GetCurrentAsync(string tenantId, CancellationToken cancellationToken);
    Task<LeadPriorityThresholds> UpdateAsync(string tenantId, int hotMinScore, int warmMinScore, CancellationToken cancellationToken);
}
