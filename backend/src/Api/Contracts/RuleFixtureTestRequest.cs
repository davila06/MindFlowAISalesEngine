using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class RuleFixtureTestRequest
{
    [Required]
    public Guid RuleId { get; init; }

    [Required]
    [MaxLength(100)]
    public string Trigger { get; init; } = string.Empty;

    [Required]
    public RuleFixtureLeadRequest Lead { get; init; } = new();
}

public class RuleFixtureLeadRequest
{
    public string Source { get; init; } = "unknown";
    public string Priority { get; init; } = "Low";
    public int Score { get; init; }
    public bool HasEmail { get; init; }
    public bool HasPhone { get; init; }
    public string? FromStage { get; init; }
    public string? ToStage { get; init; }
}
