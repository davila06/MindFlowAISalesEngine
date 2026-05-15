namespace Api.Domain.Observability;

public class AlertEvent
{
    public Guid Id { get; private set; }
    public Guid ThresholdId { get; private set; }
    public string EndpointName { get; private set; } = string.Empty;
    public string MetricName { get; private set; } = string.Empty;
    public decimal ObservedValue { get; private set; }
    public decimal ThresholdValue { get; private set; }
    public DateTime TriggeredAtUtc { get; private set; }
    public bool NotificationSent { get; private set; }

    // AO-12: Alert lifecycle
    public string Status { get; private set; } = "open";
    public string? AcknowledgedBy { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public DateTime? SnoozedUntilUtc { get; private set; }
    public string? ResolvedBy { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? StatusNotes { get; private set; }

    private AlertEvent() { }

    public AlertEvent(
        Guid thresholdId,
        string endpointName,
        string metricName,
        decimal observedValue,
        decimal thresholdValue,
        DateTime triggeredAtUtc)
    {
        Id = Guid.NewGuid();
        ThresholdId = thresholdId;
        EndpointName = endpointName;
        MetricName = metricName;
        ObservedValue = observedValue;
        ThresholdValue = thresholdValue;
        TriggeredAtUtc = triggeredAtUtc;
        NotificationSent = false;
        Status = "open";
    }

    public void MarkNotificationSent()
    {
        NotificationSent = true;
    }

    public void Acknowledge(string actor, string? notes = null)
    {
        Status = "acknowledged";
        AcknowledgedBy = actor;
        AcknowledgedAtUtc = DateTime.UtcNow;
        StatusNotes = notes;
    }

    public void Snooze(DateTime snoozeUntilUtc, string actor, string? notes = null)
    {
        Status = "snoozed";
        SnoozedUntilUtc = snoozeUntilUtc;
        AcknowledgedBy = actor;
        AcknowledgedAtUtc = DateTime.UtcNow;
        StatusNotes = notes;
    }

    public void Resolve(string actor, string? notes = null)
    {
        Status = "resolved";
        ResolvedBy = actor;
        ResolvedAtUtc = DateTime.UtcNow;
        StatusNotes = notes;
    }
}
