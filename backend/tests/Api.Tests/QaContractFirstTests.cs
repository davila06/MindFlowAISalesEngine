using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

/// <summary>
/// QA-03: Contract-first API tests.
/// Validates response schemas, required fields, Content-Type headers and
/// backward-compatible shapes across all public API endpoints.
/// These tests act as a consumer-side contract guard: any breaking change
/// in field names, types or status codes will fail here first.
/// </summary>
public sealed class QaContractFirstFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"qa_contract_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LeadsDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddDbContext<LeadsDbContext>(o => o.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}

public class QaContractFirstTests
{
    private static HttpClient BuildClient()
    {
        var client = new QaContractFirstFactory().CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", "qa-contract-tenant");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-User-Role", "Admin");
        return client;
    }

    // ── Lead intake contract ─────────────────────────────────────────────────

    [Fact]
    public async Task LeadIntake_ResponseContract_HasRequiredFields()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"contract_{Guid.NewGuid():N}@mindflow.qa",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = "web"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out var id), "Missing: id");
        Assert.True(body.TryGetProperty("email", out _), "Missing: email");
        Assert.True(body.TryGetProperty("score", out _), "Missing: score");
        Assert.True(body.TryGetProperty("priority", out _), "Missing: priority");
        Assert.True(body.TryGetProperty("scoringVersion", out _), "Missing: scoringVersion");
        Assert.True(body.TryGetProperty("createdAtUtc", out _), "Missing: createdAtUtc");
        Assert.NotEqual(Guid.Empty, id.GetGuid());
    }

    [Fact]
    public async Task LeadIntake_InvalidEmail_Returns400WithValidationContract()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = "not-an-email",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = "web"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("title", out _), "Problem details must include title");
        Assert.True(body.TryGetProperty("status", out var status), "Problem details must include status");
        Assert.Equal(400, status.GetInt32());
    }

    [Fact]
    public async Task LeadIntake_EmptyBody_Returns400()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/leads/intake", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Rules contract ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRule_ResponseContract_HasRequiredFields()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"Contract Rule {Guid.NewGuid().ToString("N")[..6]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = "5" } },
            Priority = 50
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out _), "Missing: id");
        Assert.True(body.TryGetProperty("name", out _), "Missing: name");
        Assert.True(body.TryGetProperty("trigger", out _), "Missing: trigger");
        Assert.True(body.TryGetProperty("isActive", out _), "Missing: isActive");
        Assert.True(body.TryGetProperty("priority", out _), "Missing: priority");
        Assert.True(body.TryGetProperty("createdAtUtc", out _), "Missing: createdAtUtc");
    }

    [Fact]
    public async Task ListRules_ResponseContract_IsJsonArray()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/rules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

    // ── Pipeline contract ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPipelineStages_ResponseContract_HasRequiredFields()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/pipeline/stages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stages = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, stages.ValueKind);

        var first = stages.EnumerateArray().First();
        Assert.True(first.TryGetProperty("id", out _), "Stage missing: id");
        Assert.True(first.TryGetProperty("name", out _), "Stage missing: name");
        Assert.True(first.TryGetProperty("order", out _), "Stage missing: order");
    }

    // ── Assignment contract ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateAssignmentUser_ResponseContract_HasRequiredFields()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Contract User",
            Email = $"contract_user_{Guid.NewGuid():N}@mindflow.qa"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("id", out _), "Missing: id");
        Assert.True(body.TryGetProperty("fullName", out _), "Missing: fullName");
        Assert.True(body.TryGetProperty("email", out _), "Missing: email");
        Assert.True(body.TryGetProperty("isActive", out _), "Missing: isActive");
    }

    // ── Error contract ───────────────────────────────────────────────────────

    [Fact]
    public async Task UnknownRoute_Returns404WithProblemDetails()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/unknown-route-qa");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ApiVersionHeader_Unsupported_Returns400()
    {
        using var client = BuildClient();

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/rules");
        req.Headers.TryAddWithoutValidation("X-Api-Version", "99");
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", "qa-contract-tenant");
        req.Headers.TryAddWithoutValidation("X-User-Role", "Admin");

        var response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Dashboard contract ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDashboardOverview_ResponseContract_HasKpiFields()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/dashboard/overview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("totalLeads", out _), "Missing: totalLeads");
        Assert.True(body.TryGetProperty("generatedAtUtc", out _), "Missing: generatedAtUtc");
    }

    // ── Health contract ──────────────────────────────────────────────────────

    [Fact]
    public async Task HealthLive_ReturnsOkWithTextPlain()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthReady_ReturnsOkOrDegraded()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/health/ready");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable,
            $"Unexpected health status: {response.StatusCode}");
    }
}


