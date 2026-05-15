using Api.Domain.Email;

namespace Api.Application.Email;

public interface IEmailStopListRepository
{
    Task<bool> ExistsAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(EmailStopListEntry entry, CancellationToken cancellationToken);
}