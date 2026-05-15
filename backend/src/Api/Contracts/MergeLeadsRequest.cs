using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public class MergeLeadsRequest
{
    [Required]
    public Guid PrimaryLeadId { get; init; }

    [Required]
    public Guid DuplicateLeadId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}

public class MergeLeadsResponse
{
    public Guid PrimaryLeadId { get; init; }
    public Guid DuplicateLeadId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime MergedAtUtc { get; init; }
}
