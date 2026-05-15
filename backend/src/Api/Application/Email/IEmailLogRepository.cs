using Api.Domain.Email;

namespace Api.Application.Email;

public interface IEmailLogRepository
{
    Task AddAsync(EmailLog log, CancellationToken cancellationToken);
    Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<EmailLog?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailLog>> GetAllAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
