using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public sealed class CancelFollowUpRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}
