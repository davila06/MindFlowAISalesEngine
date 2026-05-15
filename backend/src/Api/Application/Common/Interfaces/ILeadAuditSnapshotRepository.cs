using Api.Domain.Leads;

namespace Api.Application.Common.Interfaces;

public interface ILeadAuditSnapshotRepository
{
    Task AddAsync(LeadAuditSnapshot snapshot, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeadAuditSnapshot>> ListByLeadAsync(Guid leadId, CancellationToken cancellationToken);
    Task<int> CountByEventTypePrefixAsync(string eventTypePrefix, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeadAuditSnapshot>> QueryByEventTypePrefixAsync(string eventTypePrefix, DateTime? startUtc, DateTime? endUtc, CancellationToken cancellationToken);
}
