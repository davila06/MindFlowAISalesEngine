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

    public async Task<IReadOnlyList<EmailLog>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.EmailLogs
            .OrderByDescending(l => l.SentAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
