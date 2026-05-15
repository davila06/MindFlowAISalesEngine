using Api.Domain.Pipeline;

namespace Api.Application.Common.Interfaces;

public interface IOpportunityRepository
{
    Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken);
    Task<Opportunity?> GetByIdAsync(Guid opportunityId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Opportunity>> ListAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}