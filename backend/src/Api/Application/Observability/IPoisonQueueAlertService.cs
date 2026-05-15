namespace Api.Application.Observability;

public interface IPoisonQueueAlertService
{
    Task NotifyGrowthAsync(string jobType, decimal queueDepth, CancellationToken cancellationToken);
}