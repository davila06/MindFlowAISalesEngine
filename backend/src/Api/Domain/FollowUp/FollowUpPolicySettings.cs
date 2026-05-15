using System.Text.Json;

namespace Api.Domain.FollowUp;

public sealed class FollowUpPolicySettings
{
    public Guid Id { get; private set; }
    public bool QuietHoursEnabled { get; private set; }
    public int QuietHoursStartHourUtc { get; private set; }
    public int QuietHoursEndHourUtc { get; private set; }
    public string RulesJson { get; private set; } = "[]";
    public DateTime UpdatedAtUtc { get; private set; }

    private FollowUpPolicySettings() { }

    public FollowUpPolicySettings(bool quietHoursEnabled, int quietHoursStartHourUtc, int quietHoursEndHourUtc, IEnumerable<FollowUpPolicyRule> rules)
    {
        Id = Guid.NewGuid();
        Update(quietHoursEnabled, quietHoursStartHourUtc, quietHoursEndHourUtc, rules);
    }

    public void Update(bool quietHoursEnabled, int quietHoursStartHourUtc, int quietHoursEndHourUtc, IEnumerable<FollowUpPolicyRule> rules)
    {
        QuietHoursEnabled = quietHoursEnabled;
        QuietHoursStartHourUtc = quietHoursStartHourUtc;
        QuietHoursEndHourUtc = quietHoursEndHourUtc;
        RulesJson = JsonSerializer.Serialize(rules ?? Array.Empty<FollowUpPolicyRule>());
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public IReadOnlyList<FollowUpPolicyRule> GetRules()
    {
        return JsonSerializer.Deserialize<List<FollowUpPolicyRule>>(RulesJson) ?? new List<FollowUpPolicyRule>();
    }
}