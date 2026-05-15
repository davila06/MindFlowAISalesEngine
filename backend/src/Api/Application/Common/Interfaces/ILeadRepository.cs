using Api.Domain.Leads;

namespace Api.Application.Common.Interfaces;

public interface ILeadRepository
{
    Task AddAsync(Lead lead, CancellationToken cancellationToken);
    Task<Lead?> GetByIdAsync(Guid leadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Lead>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Lead>> ListByCreatedRangeAsync(DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid leadId, CancellationToken cancellationToken);
    Task DeleteAsync(Lead lead, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
