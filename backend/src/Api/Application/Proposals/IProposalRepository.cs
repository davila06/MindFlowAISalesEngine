using Api.Domain.Proposals;

namespace Api.Application.Proposals;

public interface IProposalRepository
{
    Task AddAsync(Proposal proposal, CancellationToken cancellationToken);
    Task AddTemplateAsync(ProposalTemplate template, CancellationToken cancellationToken);
    Task<Proposal?> GetByIdAsync(Guid proposalId, CancellationToken cancellationToken);
    Task<Proposal?> GetByTrackingTokenAsync(string trackingToken, CancellationToken cancellationToken);
    Task<ProposalTemplate?> GetCurrentTemplateAsync(string templateName, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalTemplate>> ListTemplatesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Proposal>> ListAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
