using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class ScoringFormulaProposalRequest
{
    [Required]
    [MaxLength(160)]
    public string RequestedBy { get; init; } = string.Empty;

    [Required]
    public ScoringFormulaUpdateRequest Formula { get; init; } = new();
}
