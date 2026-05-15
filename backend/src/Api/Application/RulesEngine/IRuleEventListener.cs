namespace Api.Application.RulesEngine;

public interface IRuleEventListener
{
    Task OnLeadCreatedAsync(Guid leadId, CancellationToken cancellationToken);
    Task OnOpportunityStageChangedAsync(Guid opportunityId, string fromStageName, string toStageName, CancellationToken cancellationToken);
    Task OnLeadRespondedAsync(Guid leadId, CancellationToken cancellationToken);
    Task OnProposalSentAsync(Guid leadId, Guid proposalId, CancellationToken cancellationToken);
}
