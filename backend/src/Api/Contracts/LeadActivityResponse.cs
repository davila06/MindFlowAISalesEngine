namespace Api.Contracts;

public sealed class LeadActivityResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string ActivityType { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string? Description { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    public string Actor { get; init; } = string.Empty;
    public DateTime OccurredAtUtc { get; init; }
}

public sealed class AddLeadNoteRequest
{
    public string Note { get; init; } = string.Empty;
}

public sealed class LeadActivitiesPage
{
    public IReadOnlyList<LeadActivityResponse> Items { get; init; } = Array.Empty<LeadActivityResponse>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public bool HasMore { get; init; }
}
