using Api.Application.Email;
using Api.Domain.Email;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Email;

public sealed class EmailStopListRepository : IEmailStopListRepository
{
    private readonly LeadsDbContext _context;

    public EmailStopListRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.EmailStopListEntries.AnyAsync(x => x.Email == normalized, cancellationToken);
    }

    public async Task AddAsync(EmailStopListEntry entry, CancellationToken cancellationToken)
    {
        var exists = await ExistsAsync(entry.Email, cancellationToken);
        if (exists)
        {
            return;
        }

        _context.EmailStopListEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}