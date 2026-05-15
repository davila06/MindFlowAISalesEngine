namespace Api.Domain.Assignment;

public class AssignmentUser
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public bool IsActive { get; private set; }
    public string? PreferredCountry { get; private set; }
    public string? PreferredIndustry { get; private set; }
    public int MaxActiveLeads { get; private set; }
    public int? MinScoreToAssign { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private AssignmentUser()
    {
        FullName = string.Empty;
        Email = string.Empty;
    }

    public AssignmentUser(
        string fullName,
        string email,
        bool isActive = true,
        string? preferredCountry = null,
        string? preferredIndustry = null,
        int maxActiveLeads = 100,
        int? minScoreToAssign = null)
    {
        Id = Guid.NewGuid();
        FullName = fullName;
        Email = email;
        IsActive = isActive;
        PreferredCountry = string.IsNullOrWhiteSpace(preferredCountry) ? null : preferredCountry.Trim().ToLowerInvariant();
        PreferredIndustry = string.IsNullOrWhiteSpace(preferredIndustry) ? null : preferredIndustry.Trim().ToLowerInvariant();
        MaxActiveLeads = Math.Max(maxActiveLeads, 1);
        MinScoreToAssign = minScoreToAssign;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetAvailability(bool isActive)
    {
        IsActive = isActive;
    }
}
