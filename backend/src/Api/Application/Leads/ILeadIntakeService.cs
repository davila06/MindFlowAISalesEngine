using Api.Contracts;

namespace Api.Application.Leads;

public interface ILeadIntakeService
{
    Task<LeadIntakeResponse> IntakeAsync(LeadIntakeRequest request, CancellationToken cancellationToken);
    Task<MergeLeadsResponse> MergeAsync(MergeLeadsRequest request, CancellationToken cancellationToken);
}
