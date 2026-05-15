using Api.Application.Proposals;
using Api.Domain.Proposals;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Proposals;

public class ProposalRepository : IProposalRepository
{
    private readonly LeadsDbContext _context;

    public ProposalRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Proposal proposal, CancellationToken cancellationToken)
    {
        _context.Add(proposal);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTemplateAsync(ProposalTemplate template, CancellationToken cancellationToken)
    {
        _context.Add(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Proposal?> GetByIdAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        return await _context.Set<Proposal>()
            .FirstOrDefaultAsync(x => x.Id == proposalId, cancellationToken);
    }

    public async Task<Proposal?> GetByTrackingTokenAsync(string trackingToken, CancellationToken cancellationToken)
    {
        return await _context.Set<Proposal>()
            .FirstOrDefaultAsync(x => x.TrackingToken == trackingToken, cancellationToken);
    }

    public async Task<ProposalTemplate?> GetCurrentTemplateAsync(string templateName, CancellationToken cancellationToken)
    {
        return await _context.Set<ProposalTemplate>()
            .Where(x => x.Name == templateName && x.IsCurrent)
            .OrderByDescending(x => x.Version)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProposalTemplate>> ListTemplatesAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<ProposalTemplate>()
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Proposal>> ListAsync(CancellationToken cancellationToken)
    {
        return await _context.Set<Proposal>()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
