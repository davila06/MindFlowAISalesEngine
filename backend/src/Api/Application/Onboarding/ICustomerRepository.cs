using Api.Domain.Onboarding;

namespace Api.Application.Onboarding;

public interface ICustomerRepository
{
    Task AddAsync(Customer customer, CancellationToken cancellationToken);
    Task<Customer?> GetByLeadIdAsync(Guid leadId, CancellationToken cancellationToken);
    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task<Customer?> GetByTrackingTokenAsync(string trackingToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Customer>> ListByLeadIdsAsync(IReadOnlyCollection<Guid> leadIds, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
