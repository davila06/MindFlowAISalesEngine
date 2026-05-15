using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class AssignmentUserCreateRequest
{
    [Required]
    [MaxLength(160)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;

    [MaxLength(16)]
    public string? PreferredCountry { get; init; }

    [MaxLength(120)]
    public string? PreferredIndustry { get; init; }

    [Range(1, 10000)]
    public int MaxActiveLeads { get; init; } = 100;

    [Range(0, 100)]
    public int? MinScoreToAssign { get; init; }
}
