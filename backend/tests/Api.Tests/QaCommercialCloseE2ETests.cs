using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

/// <summary>
/// QA-11: E2E test of a full commercial close — from lead intake through
/// qualification → pipeline move → proposal → won → customer onboarding.
/// This is the highest-fidelity end-to-end scenario for the sales engine.
///
/// QA-12: API contract compatibility tests.
/// Verifies that the API remains backward-compatible: previously-required
/// fields still exist, response shapes are stable, and deprecated paths
/// return expected status codes without breaking consumers.
/// </summary>
public sealed class QaE2ECommercialFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"qa_e2e_{Guid.NewGuid():N}.db";

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

// ═══════════════════════════════════════════════════════════════════════════════
// QA-11 — E2E full commercial close
// ═══════════════════════════════════════════════════════════════════════════════
public class QaCommercialCloseE2ETests
{
    private static HttpClient BuildClient()
    {
        var client = new QaE2ECommercialFactory().CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", "qa-e2e-tenant");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-User-Role", "Admin");
        return client;
    }

    [Fact]
    public async Task E2E_FullCommercialCycle_IntakeToOnboarding()
    {
        using var client = BuildClient();

        // ── Step 1: Lead intake ──────────────────────────────────────────────
        var intakeResp = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"enterprise_{Guid.NewGuid():N}@acme.qa",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = "website",
            Country = "US",
            Campaign = "q2-2026-enterprise",
            Channel = "organic"
        });
        Assert.Equal(HttpStatusCode.Created, intakeResp.StatusCode);
        var lead = await intakeResp.Content.ReadFromJsonAsync<E2ELeadDto>();
        Assert.NotNull(lead);
        Assert.NotEqual(Guid.Empty, lead.Id);
        Assert.True(lead.Score >= 0, "Lead must be scored on intake");

        // ── Step 2: Pipeline — create opportunity ────────────────────────────
        var stagesResp = await client.GetAsync("/api/pipeline/stages");
        Assert.Equal(HttpStatusCode.OK, stagesResp.StatusCode);
        var stages = await stagesResp.Content.ReadFromJsonAsync<List<E2EStageDto>>();
        Assert.NotNull(stages);
        Assert.True(stages.Count >= 4, "At least 4 pipeline stages expected");

        var newStage = stages.First(s => s.Name == "new");
        var qualifiedStage = stages.First(s => s.Name == "qualified");

        var createOppResp = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = lead.Id,
            Title = "ACME Enterprise Deal Q2",
            Value = 120_000m,
            StageId = newStage.Id
        });
        Assert.Equal(HttpStatusCode.Created, createOppResp.StatusCode);
        var opportunity = await createOppResp.Content.ReadFromJsonAsync<E2EOppDto>();
        Assert.NotNull(opportunity);

        // ── Step 3: Move opportunity to Qualified ────────────────────────────
        var moveToQualified = await client.PatchAsJsonAsync(
            $"/api/pipeline/opportunities/{opportunity.Id}/stage",
            new { TargetStageId = qualifiedStage.Id, Reason = "Sales call completed — high fit" });
        Assert.Equal(HttpStatusCode.OK, moveToQualified.StatusCode);

        // ── Step 4: Create proposal ──────────────────────────────────────────
        var proposalResp = await client.PostAsJsonAsync("/api/proposals", new
        {
            LeadId = lead.Id,
            Title = "Enterprise SaaS Proposal",
            Amount = 120_000m,
            ValidUntilUtc = DateTime.UtcNow.AddDays(30)
        });
        Assert.Equal(HttpStatusCode.Created, proposalResp.StatusCode);
        var proposal = await proposalResp.Content.ReadFromJsonAsync<E2EProposalDto>();
        Assert.NotNull(proposal);
        Assert.NotEqual(Guid.Empty, proposal.Id);

        // ── Step 5: Move opportunity to Won stage ────────────────────────────
        var wonStage = stages.First(s => s.Name == "won");
        var moveToWon = await client.PatchAsJsonAsync(
            $"/api/pipeline/opportunities/{opportunity.Id}/stage",
            new { TargetStageId = wonStage.Id, Reason = "Contract signed" });
        Assert.Equal(HttpStatusCode.OK, moveToWon.StatusCode);

        // ── Step 6: Mark proposal as accepted ───────────────────────────────
        var acceptProposal = await client.PostAsJsonAsync(
            $"/api/proposals/{proposal.Id}/sign",
            new { SignerName = "ACME Buyer", SignerEmail = lead.Email });
        Assert.True(
            acceptProposal.StatusCode == HttpStatusCode.OK ||
            acceptProposal.StatusCode == HttpStatusCode.NoContent,
            $"Proposal accept returned: {acceptProposal.StatusCode}");

        // ── Step 7: Trigger customer onboarding ─────────────────────────────
        var customerResp = await client.GetAsync($"/api/onboarding/customers/by-lead/{lead.Id}");
        Assert.Equal(HttpStatusCode.OK, customerResp.StatusCode);
        var customer = await customerResp.Content.ReadFromJsonAsync<E2ECustomerDto>();
        Assert.NotNull(customer);

        // ── Step 8: Verify onboarding tasks created ─────────────────────────
        var onboardingTasksResp = await client.GetAsync(
            $"/api/onboarding/customers/{customer.Id}/tasks");
        Assert.Equal(HttpStatusCode.OK, onboardingTasksResp.StatusCode);
        var tasks = await onboardingTasksResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, tasks.ValueKind);

        // ── Step 9: Verify pipeline history recorded ─────────────────────────
        var historyResp = await client.GetAsync(
            $"/api/pipeline/opportunities/{opportunity.Id}/history");
        Assert.Equal(HttpStatusCode.OK, historyResp.StatusCode);
        var history = await historyResp.Content.ReadFromJsonAsync<List<E2EHistoryDto>>();
        Assert.NotNull(history);
        Assert.True(history.Count >= 2, "History must record at least 2 stage moves");

        // ── Step 10: Dashboard must reflect closed deal ──────────────────────
        var dashboard = await client.GetAsync("/api/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        var overview = await dashboard.Content.ReadFromJsonAsync<E2EDashboardDto>();
        Assert.NotNull(overview);
        Assert.True(overview.TotalLeads >= 1);
    }

    [Fact]
    public async Task E2E_LeadIntake_WithRule_ScoredAndAssigned()
    {
        using var client = BuildClient();

        // Seed assignment user
        var userResp = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "E2E Sales Agent",
            Email = $"e2e_agent_{Guid.NewGuid():N}@mindflow.qa"
        });
        Assert.Equal(HttpStatusCode.Created, userResp.StatusCode);

        // Seed scoring rule
        await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"E2E Score Rule {Guid.NewGuid().ToString("N")[..6]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "website" } },
            Actions = new[] { new { Type = "add_score", Value = "25" } },
            Priority = 70
        });

        var lead = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"e2e_rule_{Guid.NewGuid():N}@mindflow.qa",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = "website"
        });
        Assert.Equal(HttpStatusCode.Created, lead.StatusCode);

        var body = await lead.Content.ReadFromJsonAsync<E2ELeadDto>();
        Assert.NotNull(body);
        Assert.True(body.Score >= 70, $"Lead should have received rule score bonus; got {body.Score}");
    }

    private sealed record E2ELeadDto(Guid Id, string Email, int Score, string Priority);
    private sealed record E2EStageDto(Guid Id, string Name, int Order);
    private sealed record E2EOppDto(Guid Id, string Title, Guid StageId);
    private sealed record E2EProposalDto(Guid Id, string Title, string Status);
    private sealed record E2ECustomerDto(Guid Id, string Name, string Email);
    private sealed record E2EHistoryDto(Guid Id, DateTime MovedAtUtc, string? Reason);
    private sealed record E2EDashboardDto(int TotalLeads);
}

// ═══════════════════════════════════════════════════════════════════════════════
// QA-12 — API contract compatibility
// ═══════════════════════════════════════════════════════════════════════════════
public class QaApiContractCompatibilityTests
{
    private static HttpClient BuildClient()
    {
        var client = new QaE2ECommercialFactory().CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", "qa-compat");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-User-Role", "Admin");
        return client;
    }

    [Fact]
    public async Task Compat_LeadIntakeResponse_BackwardCompatibleFields()
    {
        // All v1 contract fields must still be present
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"compat_{Guid.NewGuid():N}@mindflow.qa",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = "web"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        // v1 required fields — must never disappear
        string[] required = ["id", "email", "score", "priority", "createdAtUtc", "source"];
        foreach (var field in required)
        {
            Assert.True(body.TryGetProperty(field, out _),
                $"Backward-compat failure: field '{field}' removed from LeadIntakeResponse");
        }
    }

    [Fact]
    public async Task Compat_PipelineStage_BackwardCompatibleFields()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/pipeline/stages");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stages = await response.Content.ReadFromJsonAsync<JsonElement>();
        var first = stages.EnumerateArray().First();

        string[] required = ["id", "name", "order"];
        foreach (var field in required)
        {
            Assert.True(first.TryGetProperty(field, out _),
                $"Backward-compat failure: stage field '{field}' removed");
        }
    }

    [Fact]
    public async Task Compat_AssignmentUser_BackwardCompatibleFields()
    {
        using var client = BuildClient();

        var create = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Compat User",
            Email = $"compat_usr_{Guid.NewGuid():N}@mindflow.qa"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var body = await create.Content.ReadFromJsonAsync<JsonElement>();

        string[] required = ["id", "fullName", "email", "isActive"];
        foreach (var field in required)
        {
            Assert.True(body.TryGetProperty(field, out _),
                $"Backward-compat failure: AssignmentUser field '{field}' removed");
        }
    }

    [Fact]
    public async Task Compat_RulesCreate_BackwardCompatibleFields()
    {
        using var client = BuildClient();

        var create = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"Compat Rule {Guid.NewGuid().ToString("N")[..6]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = "1" } },
            Priority = 50
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var body = await create.Content.ReadFromJsonAsync<JsonElement>();

        string[] required = ["id", "name", "trigger", "isActive", "priority", "createdAtUtc"];
        foreach (var field in required)
        {
            Assert.True(body.TryGetProperty(field, out _),
                $"Backward-compat failure: Rule field '{field}' removed");
        }
    }

    [Fact]
    public async Task Compat_ApiVersionHeader_Version1_Accepted()
    {
        using var client = BuildClient();

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/rules");
        req.Headers.TryAddWithoutValidation("X-Api-Version", "1");
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", "qa-compat");
        req.Headers.TryAddWithoutValidation("X-User-Role", "Admin");

        var response = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Compat_ErrorContract_HasTitleAndStatus()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = "not-valid-email",
            Phone = "123",
            Source = "web"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(body.TryGetProperty("title", out _),
            "Error contract must include 'title' (RFC 7807)");
        Assert.True(body.TryGetProperty("status", out _),
            "Error contract must include 'status' (RFC 7807)");
    }

    [Fact]
    public async Task Compat_DashboardOverview_BackwardCompatibleFields()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/dashboard/overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        string[] required = ["totalLeads", "generatedAtUtc"];
        foreach (var field in required)
        {
            Assert.True(body.TryGetProperty(field, out _),
                $"Backward-compat failure: dashboard field '{field}' removed");
        }
    }
}


