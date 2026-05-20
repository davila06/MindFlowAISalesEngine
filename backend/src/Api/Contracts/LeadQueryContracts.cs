namespace Api.Contracts;

// ---- Lead Query ----

/// <summary>Paged response for lead search results.</summary>
public record LeadPageResponse(
    int Page,
    int PageSize,
    int Total,
    bool HasMore,
    IReadOnlyList<LeadSummaryResponse> Items);

/// <summary>Single lead row with core fields + resolved custom field values.</summary>
public record LeadSummaryResponse(
    Guid Id,
    string? Email,
    string? Phone,
    string Source,
    string Channel,
    string Campaign,
    string Country,
    int Score,
    string Priority,
    DateTime CreatedAtUtc,
    IReadOnlyDictionary<string, string?> CustomFields);
