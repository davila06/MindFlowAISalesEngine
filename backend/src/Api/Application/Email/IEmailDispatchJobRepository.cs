using Api.Domain.Email;

namespace Api.Application.Email;

public interface IEmailDispatchJobRepository
{
    Task AddAsync(EmailDispatchJob job, CancellationToken cancellationToken);
    Task<EmailDispatchJob?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailDispatchJob>> GetDueAsync(DateTime utcNow, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailDispatchJob>> GetAllAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}