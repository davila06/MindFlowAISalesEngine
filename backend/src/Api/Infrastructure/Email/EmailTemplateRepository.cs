using Api.Application.Email;
using Api.Domain.Email;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Email;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly LeadsDbContext _context;

    public EmailTemplateRepository(LeadsDbContext context)
    {
        _context = context;
    }

    public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await GetCurrentByNameAsync(name, cancellationToken);
    }

    public async Task<EmailTemplate?> GetCurrentByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await _context.EmailTemplates
            .Where(t => t.Name == name && t.IsActive && t.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EmailTemplate?> GetByNameAndVersionAsync(string name, int version, CancellationToken cancellationToken)
    {
        return await _context.EmailTemplates
            .Where(t => t.Name == name && t.Version == version && t.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetVersionsAsync(string name, CancellationToken cancellationToken)
    {
        return await _context.EmailTemplates
            .Where(t => t.Name == name && t.IsActive)
            .OrderByDescending(t => t.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmailTemplate template, CancellationToken cancellationToken)
    {
        _context.EmailTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
