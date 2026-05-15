using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class ScoringSimulationRequest
{
    [Required]
    public IReadOnlyList<ScoringSimulationInput> Samples { get; init; } = [];
}

public class ScoringSimulationInput
{
    public string? Email { get; init; }
    public string? Phone { get; init; }
    [MaxLength(100)]
    public string Source { get; init; } = "unknown";
}
