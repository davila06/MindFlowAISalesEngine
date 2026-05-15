namespace Api.Domain.Onboarding;

public class Customer
{
    public Guid Id { get; private set; }
    public Guid LeadId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string Status { get; private set; } = CustomerStatus.Active;
    public string Segment { get; private set; } = string.Empty;
    public string PlaybookKey { get; private set; } = string.Empty;
    public decimal HealthScore { get; private set; }
    public string TrackingToken { get; private set; } = string.Empty;
    public int TrackingActivations { get; private set; }
    public DateTime? LastTrackingActivatedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Customer() { }

    public Customer(Guid leadId, string email, string? phone, string segment, string playbookKey)
    {
        Id = Guid.NewGuid();
        LeadId = leadId;
        Email = email;
        Phone = phone;
        Status = CustomerStatus.Active;
        Segment = segment;
        PlaybookKey = playbookKey;
        HealthScore = 100m;
        TrackingToken = Guid.NewGuid().ToString("N");
        TrackingActivations = 0;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void TrackActivation()
    {
        TrackingActivations++;
        LastTrackingActivatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateHealth(decimal healthScore)
    {
        HealthScore = Math.Clamp(healthScore, 0m, 100m);
    }

    public void MarkAtRisk()
    {
        Status = CustomerStatus.AtRisk;
    }

    public void MarkChurnRisk()
    {
        Status = CustomerStatus.ChurnRisk;
    }

    public void MarkActive()
    {
        Status = CustomerStatus.Active;
    }
}
