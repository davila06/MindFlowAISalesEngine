using Api.Application.Email;
using Api.Domain.Email;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Email;

public class EmailLogRepository : IEmailLogRepository
{
    private readonly LeadsDbContext _context;

    public EmailLogRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailLog log, CancellationToken cancellationToken)
    {
        _context.EmailLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.EmailLogs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<EmailLog?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken)
    {
        return await _context.EmailLogs.FirstOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
    }

    public async Task<EmailLog?> GetByTrackingTokenAsync(Guid trackingToken, CancellationToken cancellationToken)
    {
        return await _context.EmailLogs
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TrackingToken == trackingToken, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailLog>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.EmailLogs
            .OrderByDescending(l => l.SentAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailLog>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var query = _context.EmailLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLowerInvariant();
            query = query.Where(l =>
                (l.ToEmail != null && l.ToEmail.ToLower().Contains(lower)) ||
                l.TemplateName.ToLower().Contains(lower) ||
                l.Status.ToLower().Contains(lower));
        }
        return await query
            .OrderByDescending(l => l.SentAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailTrackingMetrics>> GetMetricsByTemplateAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var query = _context.EmailLogs.Where(l => l.Succeeded);
        if (from.HasValue) query = query.Where(l => l.SentAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(l => l.SentAtUtc <= to.Value);

        var grouped = await query
            .GroupBy(l => l.TemplateName)
            .Select(g => new EmailTrackingMetrics(
                g.Key,
                g.Count(),
                g.Count(l => l.OpenCount > 0),
                g.Count(l => l.ClickCount > 0)))
            .ToListAsync(cancellationToken);
        return grouped;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
