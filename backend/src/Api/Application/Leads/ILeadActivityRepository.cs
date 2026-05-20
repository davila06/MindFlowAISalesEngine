using Api.Domain.Leads;

namespace Api.Application.Leads;

public interface ILeadActivityRepository
{
    Task AddAsync(LeadActivity activity, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<LeadActivity> activities, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeadActivity>> GetByLeadAsync(
        Guid leadId,
        int page,
        int pageSize,
        string? typeFilter,
        CancellationToken cancellationToken);
    Task<int> CountByLeadAsync(Guid leadId, CancellationToken cancellationToken);
}
