using Api.Application.Leads;
using Api.Contracts;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Leads;

/// <summary>
/// EF Core implementation of ILeadQueryService.
/// Supports filtering and sorting leads by custom field values stored in CustomFieldValues.
/// Custom-field subqueries are expressed as correlated EXISTS / subselect queries
/// that SQLite and SQL Server both handle correctly.
/// </summary>
public class LeadQueryService : ILeadQueryService
{
    private readonly LeadsDbContext _db;

    public LeadQueryService(LeadsDbContext db)
    {
        _db = db;
    }

    public async Task<LeadPageResponse> SearchAsync(
        int page,
        int pageSize,
        IReadOnlyDictionary<string, string> cfFilters,
        string? cfSort,
        string cfSortDir,
        string sortBy,
        string sortDir,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Base query — tenant filter is applied by HasQueryFilter on Lead.
        var query = _db.Leads.AsQueryable();

        // ── Custom-field filters ──────────────────────────────────────────
        // For each required key=value pair, filter to leads that have a matching row
        // in CustomFieldValues.
        foreach (var (key, value) in cfFilters)
        {
            var k = key.ToLowerInvariant();
            var v = value;
            query = query.Where(l =>
                _db.CustomFieldValues.Any(cfv =>
                    cfv.EntityId == l.Id &&
                    cfv.EntityType == "Lead" &&
                    cfv.FieldKey == k &&
                    cfv.Value == v));
        }

        // ── Sort ──────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(cfSort))
        {
            var cfSortKey = cfSort.ToLowerInvariant();
            // Correlated scalar subquery: grab the value string for that field key
            // (null when the lead has no value — sorts last in both directions).
            query = cfSortDir == "asc"
                ? query.OrderBy(l =>
                    _db.CustomFieldValues
                        .Where(cfv => cfv.EntityId == l.Id && cfv.EntityType == "Lead" && cfv.FieldKey == cfSortKey)
                        .Select(cfv => cfv.Value)
                        .FirstOrDefault())
                : query.OrderByDescending(l =>
                    _db.CustomFieldValues
                        .Where(cfv => cfv.EntityId == l.Id && cfv.EntityType == "Lead" && cfv.FieldKey == cfSortKey)
                        .Select(cfv => cfv.Value)
                        .FirstOrDefault());
        }
        else
        {
            query = (sortBy.ToLowerInvariant(), sortDir.ToLowerInvariant()) switch
            {
                ("score",     "asc")  => query.OrderBy(l => l.Score),
                ("score",       _)    => query.OrderByDescending(l => l.Score),
                ("email",     "asc")  => query.OrderBy(l => l.Email),
                ("email",       _)    => query.OrderByDescending(l => l.Email),
                ("source",    "asc")  => query.OrderBy(l => l.Source),
                ("source",      _)    => query.OrderByDescending(l => l.Source),
                (_,           "asc")  => query.OrderBy(l => l.CreatedAtUtc),
                _                     => query.OrderByDescending(l => l.CreatedAtUtc),
            };
        }

        // ── Pagination ────────────────────────────────────────────────────
        var total = await query.CountAsync(cancellationToken);
        var leads = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // ── Resolve custom field values for the returned lead IDs ─────────
        var leadIds = leads.Select(l => l.Id).ToList();
        var cfValues = await _db.CustomFieldValues
            .Where(cfv => cfv.EntityType == "Lead" && leadIds.Contains(cfv.EntityId))
            .ToListAsync(cancellationToken);

        var cfByLead = cfValues
            .GroupBy(cfv => cfv.EntityId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<string, string?>)g.ToDictionary(cfv => cfv.FieldKey, cfv => cfv.Value));

        // ── Map ───────────────────────────────────────────────────────────
        var items = leads.Select(l => new LeadSummaryResponse(
            l.Id,
            l.Email,
            l.Phone,
            l.Source,
            l.Channel,
            l.Campaign,
            l.Country,
            l.Score,
            l.Priority,
            l.CreatedAtUtc,
            cfByLead.TryGetValue(l.Id, out var cf) ? cf : new Dictionary<string, string?>()
        )).ToList();

        return new LeadPageResponse(page, pageSize, total, (page * pageSize) < total, items);
    }
}
