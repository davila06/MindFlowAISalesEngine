using Api.Domain.Observability;

namespace Api.Application.Observability;

public interface IAlertThresholdRepository
{
    Task AddAsync(AlertThreshold threshold, CancellationToken cancellationToken);
    Task<AlertThreshold?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertThreshold>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertThreshold>> GetActiveAsync(CancellationToken cancellationToken);
    Task<int> CountActiveAsync(CancellationToken cancellationToken);
    void Remove(AlertThreshold threshold);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
