using Api.Domain.Email;

namespace Api.Application.Email;

public interface ISmtpSettingsRepository
{
    Task<SmtpSettings?> GetActiveAsync(CancellationToken cancellationToken);
    Task UpsertAsync(SmtpSettings settings, CancellationToken cancellationToken);
}
