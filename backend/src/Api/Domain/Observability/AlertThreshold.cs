namespace Api.Domain.Observability;

public class AlertThreshold
{
    public Guid Id { get; private set; }
    public string EndpointName { get; private set; } = string.Empty;
    public decimal MaxErrorRatePercent { get; private set; }
    public decimal MaxAverageLatencyMs { get; private set; }
    public string NotificationEmail { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string? WebhookUrl { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private AlertThreshold() { }

    public AlertThreshold(
        string endpointName,
        decimal maxErrorRatePercent,
        decimal maxAverageLatencyMs,
        string notificationEmail,
        bool isActive,
        string? webhookUrl = null)
    {
        Id = Guid.NewGuid();
        EndpointName = endpointName;
        MaxErrorRatePercent = maxErrorRatePercent;
        MaxAverageLatencyMs = maxAverageLatencyMs;
        NotificationEmail = notificationEmail;
        IsActive = isActive;
        WebhookUrl = webhookUrl;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void Update(
        string endpointName,
        decimal maxErrorRatePercent,
        decimal maxAverageLatencyMs,
        string notificationEmail,
        bool isActive,
        string? webhookUrl = null)
    {
        EndpointName = endpointName;
        MaxErrorRatePercent = maxErrorRatePercent;
        MaxAverageLatencyMs = maxAverageLatencyMs;
        NotificationEmail = notificationEmail;
        IsActive = isActive;
        WebhookUrl = webhookUrl;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
