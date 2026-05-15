using Api.Application.Onboarding;
using Api.Domain.Onboarding;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Onboarding;

public class CustomerRepository : ICustomerRepository
{
    private readonly LeadsDbContext _context;

    public CustomerRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        _context.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Customer?> GetByLeadIdAsync(Guid leadId, CancellationToken cancellationToken)
    {
        return await _context.Set<Customer>()
            .FirstOrDefaultAsync(x => x.LeadId == leadId, cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
    {
        return await _context.Set<Customer>()
            .FirstOrDefaultAsync(x => x.Id == customerId, cancellationToken);
    }

    public async Task<Customer?> GetByTrackingTokenAsync(string trackingToken, CancellationToken cancellationToken)
    {
        return await _context.Set<Customer>()
            .FirstOrDefaultAsync(x => x.TrackingToken == trackingToken, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Customer>()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListByLeadIdsAsync(IReadOnlyCollection<Guid> leadIds, CancellationToken cancellationToken)
    {
        return await _context.Set<Customer>()
            .Where(x => leadIds.Contains(x.LeadId))
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
