using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class WeeklyAnalyticsReportTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"weekly_report_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WeeklyAnalyticsReports:Enabled"] = "true",
                ["WeeklyAnalyticsReports:RunOnStartup"] = "true",
                ["WeeklyAnalyticsReports:IntervalMinutes"] = "1",
                ["Features:DisableDataRetentionBackground"] = "true"
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LeadsDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<LeadsDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch (IOException)
            {
                // Ignore transient SQLite locks during teardown.
            }
        }
    }
}

public class WeeklyAnalyticsReportBackgroundTests
{
    [Fact]
    public async Task WeeklyReportBackgroundService_RunOnStartup_GeneratesAuditEntry()
    {
        using var factory = new WeeklyAnalyticsReportTestFactory();
        using var client = factory.CreateClient();

        // Trigger application startup pipeline.
        await client.GetAsync("/api/dashboard/overview");

        var found = false;
        for (var i = 0; i < 20; i++)
        {
            await Task.Delay(150);

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
            found = await dbContext.AdminAuditLogs.AnyAsync(x => x.Action == "analytics_weekly_report_generated");
            if (found)
            {
                break;
            }
        }

        Assert.True(found);
    }
}
