using Api.Application.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Observability;

public sealed class ObservabilityPersistenceBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ObservabilityPersistenceBackgroundService> _logger;

    public ObservabilityPersistenceBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ObservabilityPersistenceBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await PersistSnapshotAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting observability snapshot.");
            }
        }
    }

    private async Task PersistSnapshotAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IObservabilityPersistenceService>();
        await service.FlushAsync(cancellationToken);
    }
}
