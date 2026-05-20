using Api.Domain.Email;

namespace Api.Application.Email;

public interface IEmailLogRepository
{
    Task AddAsync(EmailLog log, CancellationToken cancellationToken);
    Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<EmailLog?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken);
    Task<EmailLog?> GetByTrackingTokenAsync(Guid trackingToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailLog>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailLog>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmailTrackingMetrics>> GetMetricsByTemplateAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record EmailTrackingMetrics(
    string TemplateName,
    int TotalSent,
    int TotalOpened,
    int TotalClicked);
