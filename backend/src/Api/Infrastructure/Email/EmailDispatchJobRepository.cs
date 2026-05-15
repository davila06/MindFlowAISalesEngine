using Api.Application.Email;
using Api.Domain.Email;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Email;

public sealed class EmailDispatchJobRepository : IEmailDispatchJobRepository
{
    private readonly LeadsDbContext _context;

    public EmailDispatchJobRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(EmailDispatchJob job, CancellationToken cancellationToken)
    {
        _context.EmailDispatchJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmailDispatchJob?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken)
    {
        return await _context.EmailDispatchJobs
            .FirstOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailDispatchJob>> GetDueAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        return await _context.EmailDispatchJobs
            .Where(x => x.Status == "Queued" && x.DueAtUtc <= utcNow)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailDispatchJob>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.EmailDispatchJobs
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}