namespace Api.Domain.Leads;

public sealed class LeadAuditSnapshot
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string EventType { get; private set; }
    public string Actor { get; private set; }
    public string PayloadJson { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private LeadAuditSnapshot()
    {
        EventType = string.Empty;
        Actor = string.Empty;
        PayloadJson = string.Empty;
    }

    public LeadAuditSnapshot(Guid leadId, string eventType, string actor, string payloadJson)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        EventType = eventType;
        Actor = actor;
        PayloadJson = payloadJson;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
