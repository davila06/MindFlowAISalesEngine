using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class ProposalRenewRequest
{
    [Range(1, 90)]
    public int NewExpiryDays { get; init; } = 14;
}
