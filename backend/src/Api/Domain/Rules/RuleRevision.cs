namespace Api.Domain.Rules;

public class RuleRevision
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public int Version { get; private set; }
    public string SnapshotJson { get; private set; }
    public string Reason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private RuleRevision()
    {
        SnapshotJson = string.Empty;
        Reason = string.Empty;
    }

    public RuleRevision(Guid ruleId, int version, string snapshotJson, string reason)
    {
        Id = Guid.NewGuid();
        RuleId = ruleId;
        Version = version;
        SnapshotJson = snapshotJson;
        Reason = reason;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
