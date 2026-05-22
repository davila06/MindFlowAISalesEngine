namespace Api.Domain.Leads;

public class Lead
{
    public Guid Id { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string Source { get; private set; }
    public string Channel { get; private set; }
    public string Campaign { get; private set; }
    public string Country { get; private set; }
    public string? ServiceInterest { get; private set; }
    public int Score { get; private set; }
    public string Priority { get; private set; }
    public string ScoringVersion { get; private set; }
    public DateTime? ScoredAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Lead()
    {
        Source = string.Empty;
        Channel = string.Empty;
        Campaign = string.Empty;
        Country = string.Empty;
        Priority = "Low";
        ScoringVersion = "unscored";
    }

    public Lead(
        string? email,
        string? phone,
        string source,
        string? channel = null,
        string? campaign = null,
        string? country = null,
        string? serviceInterest = null)
    {
        Id = Guid.NewGuid();
        Email = email;
        Phone = phone;
        Source = source;
        Channel = string.IsNullOrWhiteSpace(channel) ? "inbound" : channel;
        Campaign = string.IsNullOrWhiteSpace(campaign) ? "organic" : campaign;
        Country = string.IsNullOrWhiteSpace(country) ? "xx" : country;
        ServiceInterest = string.IsNullOrWhiteSpace(serviceInterest) ? null : serviceInterest.Trim();
        Score = 0;
        Priority = "Low";
        ScoringVersion = "unscored";
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetScore(int score, string priority, string scoringVersion)
    {
        Score = Math.Clamp(score, 0, 100);
        Priority = string.IsNullOrWhiteSpace(priority) ? "Low" : priority;
        ScoringVersion = string.IsNullOrWhiteSpace(scoringVersion) ? "unscored" : scoringVersion;
        ScoredAtUtc = DateTime.UtcNow;
    }
}
