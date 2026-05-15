namespace Api.Domain.Pipeline;

public class OpportunityStageHistory
{
    public Guid Id { get; private set; }
    public Guid OpportunityId { get; private set; }
    public Guid FromStageId { get; private set; }
    public Guid ToStageId { get; private set; }
    public string? Reason { get; private set; }
    public string Actor { get; private set; }
    public bool IsAutomated { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }

    private OpportunityStageHistory()
    {
        Actor = string.Empty;
    }

    public OpportunityStageHistory(Guid opportunityId, Guid fromStageId, Guid toStageId, string? reason, string actor, bool isAutomated)
    {
        Id = Guid.NewGuid();
        OpportunityId = opportunityId;
        FromStageId = fromStageId;
        ToStageId = toStageId;
        Reason = reason;
        Actor = actor;
        IsAutomated = isAutomated;
        ChangedAtUtc = DateTime.UtcNow;
    }
}