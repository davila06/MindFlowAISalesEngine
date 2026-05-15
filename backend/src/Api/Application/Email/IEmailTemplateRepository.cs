using Api.Domain.Email;

namespace Api.Application.Email;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<EmailTemplate?> GetCurrentByNameAsync(string name, CancellationToken cancellationToken);
    Task<EmailTemplate?> GetByNameAndVersionAsync(string name, int version, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailTemplate>> GetVersionsAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(EmailTemplate template, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
