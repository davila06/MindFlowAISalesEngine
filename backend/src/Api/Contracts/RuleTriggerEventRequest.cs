using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class RuleTriggerEventRequest
{
    [Required]
    [MaxLength(100)]
    public string Trigger { get; init; } = string.Empty;

    public Guid? LeadId { get; init; }
    public Guid? OpportunityId { get; init; }
    public Guid? ProposalId { get; init; }

    [MaxLength(80)]
    public string? FromStage { get; init; }

    [MaxLength(80)]
    public string? ToStage { get; init; }
}
