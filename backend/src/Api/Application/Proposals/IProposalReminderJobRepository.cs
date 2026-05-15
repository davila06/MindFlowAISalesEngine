using Api.Domain.Proposals;

namespace Api.Application.Proposals;

public interface IProposalReminderJobRepository
{
    Task AddAsync(ProposalReminderJob job, CancellationToken cancellationToken);
    Task<ProposalReminderJob?> GetByProposalIdAsync(Guid proposalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalReminderJob>> GetScheduledDueAsync(DateTime utcNow, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalReminderJob>> GetDeadLetterAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProposalReminderJob>> GetPoisonQueueAsync(CancellationToken cancellationToken);
    Task<int> CountPoisonedAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
