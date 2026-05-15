namespace Api.Domain.Assignment;

public class LeadAssignment
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public Guid UserId { get; private set; }
    public string Strategy { get; private set; }
    public string? RuleKey { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }

    private LeadAssignment()
    {
        Strategy = string.Empty;
    }

    public LeadAssignment(Guid leadId, Guid userId, string strategy, string? ruleKey)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        UserId = userId;
        Strategy = strategy;
        RuleKey = ruleKey;
        AssignedAtUtc = DateTime.UtcNow;
    }
}
