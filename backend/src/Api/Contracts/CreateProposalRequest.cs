using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class CreateProposalRequest
{
    [Required]
    public Guid LeadId { get; init; }

    [Required]
    [MaxLength(180)]
    public string Title { get; init; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal Amount { get; init; }

    [Required]
    [MaxLength(8)]
    public string Currency { get; init; } = "USD";

    [MaxLength(160)]
    public string? RecipientName { get; init; }
}
