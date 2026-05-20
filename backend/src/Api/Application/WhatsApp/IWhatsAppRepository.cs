using Api.Domain.WhatsApp;

namespace Api.Application.WhatsApp;

public interface IWhatsAppRepository
{
    Task<WhatsAppContact?> GetContactByPhoneAsync(string phone, CancellationToken cancellationToken);
    Task AddContactAsync(WhatsAppContact contact, CancellationToken cancellationToken);
    Task UpdateContactAsync(WhatsAppContact contact, CancellationToken cancellationToken);
    Task<IReadOnlyList<WhatsAppMessage>> GetMessagesAsync(string contactPhone, int page, int pageSize, CancellationToken cancellationToken);
    Task AddMessageAsync(WhatsAppMessage message, CancellationToken cancellationToken);
    Task UpdateMessageAsync(WhatsAppMessage message, CancellationToken cancellationToken);
    Task<bool> MessageExistsAsync(string externalMessageId, CancellationToken cancellationToken);
}
