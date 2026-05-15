namespace Api.Contracts;

public class AssignmentUserResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string? PreferredCountry { get; init; }
    public string? PreferredIndustry { get; init; }
    public int MaxActiveLeads { get; init; }
    public int? MinScoreToAssign { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
