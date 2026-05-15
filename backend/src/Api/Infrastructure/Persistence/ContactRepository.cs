using Api.Application.Common.Interfaces;
using Api.Domain.Contacts;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Persistence;

public class ContactRepository : IContactRepository
{
    private readonly LeadsDbContext _dbContext;

    public ContactRepository(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken)
    {
        await _dbContext.Contacts.AddAsync(contact, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Contacts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Contact>> ListAsync(Guid? leadId, string? search, CancellationToken cancellationToken)
    {
        var query = _dbContext.Contacts.AsQueryable();

        if (leadId.HasValue)
        {
            query = query.Where(x => x.LeadId == leadId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                (x.FullName != null && x.FullName.Contains(normalized))
                || (x.Email != null && x.Email.Contains(normalized))
                || (x.Phone != null && x.Phone.Contains(normalized)));
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByEmailOrPhoneAsync(string? email, string? phone, Guid? ignoreContactId, CancellationToken cancellationToken)
    {
        return _dbContext.Contacts.AnyAsync(x =>
                (ignoreContactId == null || x.Id != ignoreContactId.Value)
                && ((email != null && x.Email == email) || (phone != null && x.Phone == phone)),
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Contact contact, CancellationToken cancellationToken)
    {
        contact.MarkDeleted();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}