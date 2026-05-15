using Api.Domain.Observability;

namespace Api.Application.Observability;

public interface IPoisonQueueRemediationRunRepository
{
    Task AddAsync(PoisonQueueRemediationRun run, CancellationToken cancellationToken);

    Task<PoisonQueueRemediationRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PoisonQueueRemediationRun>> QueryAsync(
        string? jobType,
        string? outcome,
        DateTime? startUtc,
        DateTime? endUtc,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
