using System.Collections.Concurrent;
using Api.Application.Scoring;

namespace Api.Infrastructure.Scoring;

public sealed class InMemoryLeadScoringFormulaStore : ILeadScoringFormulaStore
{
    private readonly ConcurrentDictionary<string, LeadScoringFormula> _currentByTenant = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<LeadScoringFormula>> _versionsByTenant = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<ScoringFormulaProposal>> _proposalsByTenant = new(StringComparer.OrdinalIgnoreCase);

    public Task<LeadScoringFormula> GetCurrentAsync(string tenantId, CancellationToken cancellationToken)
    {
        var normalizedTenant = NormalizeTenant(tenantId);
        var current = _currentByTenant.GetOrAdd(normalizedTenant, _ => new LeadScoringFormula());
        var versions = _versionsByTenant.GetOrAdd(normalizedTenant, _ => [current]);
        if (versions.All(v => v.Version != current.Version))
        {
            versions.Add(current);
        }

        return Task.FromResult(current);
    }

    public Task<IReadOnlyList<LeadScoringFormula>> ListVersionsAsync(string tenantId, CancellationToken cancellationToken)
    {
        var normalizedTenant = NormalizeTenant(tenantId);
        var versions = _versionsByTenant.GetOrAdd(normalizedTenant, _ => [new LeadScoringFormula()]);
        return Task.FromResult((IReadOnlyList<LeadScoringFormula>)versions.OrderByDescending(v => v.UpdatedAtUtc).ToList());
    }

    public Task<ScoringFormulaProposal> CreateProposalAsync(string tenantId, string requestedBy, LeadScoringFormula formula, CancellationToken cancellationToken)
    {
        var normalizedTenant = NormalizeTenant(tenantId);
        var proposals = _proposalsByTenant.GetOrAdd(normalizedTenant, _ => []);

        var proposal = new ScoringFormulaProposal
        {
            RequestedBy = requestedBy,
            Formula = formula
        };

        proposals.Add(proposal);
        return Task.FromResult(proposal);
    }

    public Task<ScoringFormulaProposal?> ApproveProposalAsync(string tenantId, Guid proposalId, string approvedBy, CancellationToken cancellationToken)
    {
        var normalizedTenant = NormalizeTenant(tenantId);
        var proposals = _proposalsByTenant.GetOrAdd(normalizedTenant, _ => []);

        var proposal = proposals.FirstOrDefault(x => x.ProposalId == proposalId);
        if (proposal is null)
        {
            return Task.FromResult<ScoringFormulaProposal?>(null);
        }

        proposal.Status = "approved";
        proposal.ApprovedAtUtc = DateTime.UtcNow;
        proposal.ApprovedBy = approvedBy;

        _currentByTenant[normalizedTenant] = proposal.Formula;
        var versions = _versionsByTenant.GetOrAdd(normalizedTenant, _ => []);
        if (versions.All(v => v.Version != proposal.Formula.Version))
        {
            versions.Add(proposal.Formula);
        }

        return Task.FromResult<ScoringFormulaProposal?>(proposal);
    }

    public Task<IReadOnlyList<ScoringFormulaProposal>> ListProposalsAsync(string tenantId, CancellationToken cancellationToken)
    {
        var normalizedTenant = NormalizeTenant(tenantId);
        var proposals = _proposalsByTenant.GetOrAdd(normalizedTenant, _ => []);
        return Task.FromResult((IReadOnlyList<ScoringFormulaProposal>)proposals.OrderByDescending(x => x.RequestedAtUtc).ToList());
    }

    private static string NormalizeTenant(string tenantId)
    {
        return string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim().ToLowerInvariant();
    }
}
