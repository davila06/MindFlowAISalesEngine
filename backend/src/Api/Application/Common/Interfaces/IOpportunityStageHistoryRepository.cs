using Api.Domain.Pipeline;

namespace Api.Application.Common.Interfaces;

public interface IOpportunityStageHistoryRepository
{
    Task AddAsync(OpportunityStageHistory history, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpportunityStageHistory>> ListByOpportunityAsync(Guid opportunityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpportunityStageHistory>> ListByChangedRangeAsync(DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken);
}