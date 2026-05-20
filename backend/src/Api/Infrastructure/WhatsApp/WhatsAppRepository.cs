using Api.Application.WhatsApp;
using Api.Domain.WhatsApp;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.WhatsApp;

public class WhatsAppRepository : IWhatsAppRepository
{
    private readonly LeadsDbContext _db;
    public WhatsAppRepository(LeadsDbContext db) => _db = db;

    public Task<WhatsAppContact?> GetContactByPhoneAsync(string phone, CancellationToken cancellationToken) =>
        _db.WhatsAppContacts.FirstOrDefaultAsync(c => c.PhoneNumber == phone, cancellationToken);

    public async Task AddContactAsync(WhatsAppContact contact, CancellationToken cancellationToken)
    {
        await _db.WhatsAppContacts.AddAsync(contact, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateContactAsync(WhatsAppContact contact, CancellationToken cancellationToken)
    {
        _db.WhatsAppContacts.Update(contact);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WhatsAppMessage>> GetMessagesAsync(string contactPhone, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await _db.WhatsAppMessages
            .Where(m => m.ContactPhone == contactPhone)
            .OrderByDescending(m => m.SentAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMessageAsync(WhatsAppMessage message, CancellationToken cancellationToken)
    {
        await _db.WhatsAppMessages.AddAsync(message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMessageAsync(WhatsAppMessage message, CancellationToken cancellationToken)
    {
        _db.WhatsAppMessages.Update(message);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> MessageExistsAsync(string externalMessageId, CancellationToken cancellationToken) =>
        _db.WhatsAppMessages.AnyAsync(m => m.ExternalMessageId == externalMessageId, cancellationToken);
}
