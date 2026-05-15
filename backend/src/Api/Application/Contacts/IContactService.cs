using Api.Contracts;

namespace Api.Application.Contacts;

public interface IContactService
{
    Task<ContactResponse> CreateAsync(ContactCreateRequest request, CancellationToken cancellationToken);
    Task<ContactResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContactResponse>> ListAsync(Guid? leadId, string? search, CancellationToken cancellationToken);
    Task<ContactResponse> UpdateAsync(Guid id, ContactUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}