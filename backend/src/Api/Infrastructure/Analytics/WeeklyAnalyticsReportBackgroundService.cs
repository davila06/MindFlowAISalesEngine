using Api.Application.AnalyticsAdvanced;
using Microsoft.Extensions.Options;

namespace Api.Infrastructure.Analytics;

public sealed class WeeklyAnalyticsReportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WeeklyAnalyticsReportOptions _options;
    private readonly ILogger<WeeklyAnalyticsReportBackgroundService> _logger;

    public WeeklyAnalyticsReportBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<WeeklyAnalyticsReportOptions> options,
        ILogger<WeeklyAnalyticsReportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (_options.RunOnStartup)
        {
            await GenerateReportAsync(stoppingToken);
        }

        var intervalMinutes = _options.IntervalMinutes > 0 ? _options.IntervalMinutes : 7 * 24 * 60;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await GenerateReportAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weekly analytics report generation failed.");
            }
        }
    }

    private async Task GenerateReportAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IWeeklyAnalyticsReportService>();
        await service.GenerateAsync(cancellationToken);
    }
}
