using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public sealed class EmailStopListRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? Reason { get; init; }
}