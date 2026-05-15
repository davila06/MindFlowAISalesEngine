using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class AssignmentUserAvailabilityUpdateRequest
{
    [Required]
    public bool IsActive { get; init; }
}
