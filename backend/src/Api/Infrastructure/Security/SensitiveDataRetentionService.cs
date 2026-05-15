using Api.Infrastructure.Persistence;
using Api.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Security;

public sealed class SensitiveDataRetentionService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SensitiveDataRetentionService> _logger;

    public SensitiveDataRetentionService(
        IServiceScopeFactory scopeFactory,
        ILogger<SensitiveDataRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sensitive data retention cleanup failed.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var emailCutoff = DateTime.UtcNow.AddDays(-180);
        var alertCutoff = DateTime.UtcNow.AddDays(-180);
        var auditCutoff = DateTime.UtcNow.AddDays(-365);

        var emailLogs = await dbContext.EmailLogs
            .Where(x => x.SentAtUtc < emailCutoff)
            .ToListAsync(cancellationToken);
        var alertEvents = await dbContext.AlertEvents
            .Where(x => x.TriggeredAtUtc < alertCutoff)
            .ToListAsync(cancellationToken);
        var auditLogs = await dbContext.AdminAuditLogs
            .Where(x => x.CreatedAtUtc < auditCutoff)
            .ToListAsync(cancellationToken);

        dbContext.EmailLogs.RemoveRange(emailLogs);
        dbContext.AlertEvents.RemoveRange(alertEvents);
        dbContext.AdminAuditLogs.RemoveRange(auditLogs);
        await dbContext.DataRetentionRuns.AddAsync(
            new DataRetentionRun(emailLogs.Count, alertEvents.Count, auditLogs.Count),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sensitive data retention cleanup removed EmailLogs={EmailLogs}, AlertEvents={AlertEvents}, AdminAuditLogs={AdminAuditLogs}.",
            emailLogs.Count,
            alertEvents.Count,
            auditLogs.Count);
    }
}
