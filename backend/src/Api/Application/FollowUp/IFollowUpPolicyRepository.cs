using Api.Domain.FollowUp;

namespace Api.Application.FollowUp;

public interface IFollowUpPolicyRepository
{
    Task<FollowUpPolicySettings?> GetAsync(CancellationToken cancellationToken);
    Task UpsertAsync(FollowUpPolicySettings settings, CancellationToken cancellationToken);
}