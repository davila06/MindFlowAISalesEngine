using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class ManualLeadAssignmentRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;

    public bool ProtectFromAutoOverwrite { get; init; } = true;
}
