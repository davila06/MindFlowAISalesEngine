using Api.Contracts;

namespace Api.Application.Leads;

public interface ILeadQueryService
{
    /// <summary>
    /// Search leads with optional custom-field filters and sort.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page (max 100).</param>
    /// <param name="cfFilters">Key=value pairs that must all match for a lead to be included.</param>
    /// <param name="cfSort">Custom field key to sort by (null = sort by core field).</param>
    /// <param name="cfSortDir">"asc" or "desc".</param>
    /// <param name="sortBy">Core field to sort by when cfSort is null: createdAt|score|email|source.</param>
    /// <param name="sortDir">"asc" or "desc".</param>
    Task<LeadPageResponse> SearchAsync(
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string> cfFilters,
        string? cfSort,
        string cfSortDir,
        string sortBy,
        string sortDir,
        CancellationToken cancellationToken);
}
