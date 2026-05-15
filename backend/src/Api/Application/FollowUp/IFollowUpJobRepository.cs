using Api.Domain.FollowUp;

namespace Api.Application.FollowUp;

public interface IFollowUpJobRepository
{
    Task AddAsync(FollowUpJob job, CancellationToken cancellationToken);
    Task<FollowUpJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<FollowUpJob>> GetScheduledDueAsync(DateTime utcNow, CancellationToken cancellationToken);
    Task<IReadOnlyList<FollowUpJob>> GetByLeadIdAsync(Guid leadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FollowUpJob>> GetDeadLetterAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<FollowUpJob>> GetPoisonQueueAsync(CancellationToken cancellationToken);
    Task<int> CountPoisonedAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<FollowUpJob>> GetAllAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
