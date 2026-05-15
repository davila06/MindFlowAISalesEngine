using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class ProposalSignRequest
{
    [Required]
    [MaxLength(160)]
    public string SignerName { get; init; } = string.Empty;

    [Required]
    [MaxLength(320)]
    public string SignerEmail { get; init; } = string.Empty;
}
