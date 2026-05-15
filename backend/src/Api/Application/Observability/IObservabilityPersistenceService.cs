namespace Api.Application.Observability;

public interface IObservabilityPersistenceService
{
    Task FlushAsync(CancellationToken cancellationToken = default);
}
