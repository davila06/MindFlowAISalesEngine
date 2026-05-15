namespace Api.Domain.Security;

public sealed class DataRetentionRun
{
    public Guid Id { get; private set; }
    public int EmailLogsRemoved { get; private set; }
    public int AlertEventsRemoved { get; private set; }
    public int AdminAuditLogsRemoved { get; private set; }
    public DateTime ExecutedAtUtc { get; private set; }

    private DataRetentionRun()
    {
    }

    public DataRetentionRun(int emailLogsRemoved, int alertEventsRemoved, int adminAuditLogsRemoved)
    {
        Id = Guid.NewGuid();
        EmailLogsRemoved = Math.Max(0, emailLogsRemoved);
        AlertEventsRemoved = Math.Max(0, alertEventsRemoved);
        AdminAuditLogsRemoved = Math.Max(0, adminAuditLogsRemoved);
        ExecutedAtUtc = DateTime.UtcNow;
    }
}
