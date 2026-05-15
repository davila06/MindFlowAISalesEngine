using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class MultiTenantRoleTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"tenant_role_test_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LeadsDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<LeadsDbContext>(options =>
                options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}

public class MultiTenantRoleEndpointTests
{
    private static HttpClient BuildClient() => new MultiTenantRoleTestFactory().CreateClient();

    [Fact]
    public async Task TenantIsolation_OverviewShowsOnlyCurrentTenantData()
    {
        using var client = BuildClient();

        var createLeadTenantA = new HttpRequestMessage(HttpMethod.Post, "/api/leads/intake")
        {
            Content = JsonContent.Create(new
            {
                Email = $"a_{Guid.NewGuid():N}@mindflow.test",
                Phone = BuildUniquePhone(),
                Source = "web"
            })
        };
        createLeadTenantA.Headers.Add("X-Tenant-Id", "tenant-a");
        createLeadTenantA.Headers.Add("X-User-Role", "Admin");

        var createResponse = await client.SendAsync(createLeadTenantA);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var overviewTenantA = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/overview");
        overviewTenantA.Headers.Add("X-Tenant-Id", "tenant-a");

        var overviewAResponse = await client.SendAsync(overviewTenantA);
        Assert.Equal(HttpStatusCode.OK, overviewAResponse.StatusCode);
        var bodyA = await overviewAResponse.Content.ReadFromJsonAsync<DashboardOverviewDto>();
        Assert.NotNull(bodyA);
        Assert.True(bodyA.TotalLeads >= 1);

        var overviewTenantB = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/overview");
        overviewTenantB.Headers.Add("X-Tenant-Id", "tenant-b");

        var overviewBResponse = await client.SendAsync(overviewTenantB);
        Assert.Equal(HttpStatusCode.OK, overviewBResponse.StatusCode);
        var bodyB = await overviewBResponse.Content.ReadFromJsonAsync<DashboardOverviewDto>();
        Assert.NotNull(bodyB);
        Assert.Equal(0, bodyB.TotalLeads);
    }

    [Fact]
    public async Task ViewerRole_CannotWriteEndpoints()
    {
        using var client = BuildClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads/intake")
        {
            Content = JsonContent.Create(new
            {
                Email = $"viewer_{Guid.NewGuid():N}@mindflow.test",
                Phone = BuildUniquePhone(),
                Source = "web"
            })
        };
        request.Headers.Add("X-Tenant-Id", "tenant-v");
        request.Headers.Add("X-User-Role", "Viewer");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SalesRole_CanWriteEndpoints()
    {
        using var client = BuildClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads/intake")
        {
            Content = JsonContent.Create(new
            {
                Email = $"sales_{Guid.NewGuid():N}@mindflow.test",
                Phone = BuildUniquePhone(),
                Source = "web"
            })
        };
        request.Headers.Add("X-Tenant-Id", "tenant-s");
        request.Headers.Add("X-User-Role", "Sales");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

file sealed class DashboardOverviewDto
{
    public int TotalLeads { get; init; }
    public decimal ConversionRate { get; init; }
    public decimal PipelineValue { get; init; }
}
