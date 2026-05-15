using Api.Domain.Contacts;

namespace Api.Application.Common.Interfaces;

public interface IContactRepository
{
    Task AddAsync(Contact contact, CancellationToken cancellationToken);
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Contact>> ListAsync(Guid? leadId, string? search, CancellationToken cancellationToken);
    Task<bool> ExistsByEmailOrPhoneAsync(string? email, string? phone, Guid? ignoreContactId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(Contact contact, CancellationToken cancellationToken);
}