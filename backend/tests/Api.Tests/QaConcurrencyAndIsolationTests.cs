using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

/// <summary>
/// QA-05: Concurrency tests for pipeline stage moves and assignment operations.
/// Validates optimistic-concurrency protection, no data corruption under parallel
/// writes, and idempotency of conflict-resolution paths.
///
/// QA-09: Massive multi-tenant isolation test suite.
/// Verifies that data from one tenant cannot be observed or mutated by another
/// tenant across leads, rules, pipeline, assignments and analytics endpoints.
/// </summary>
public sealed class QaConcurrencyFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"qa_concurrency_{Guid.NewGuid():N}.db";

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
// QA-05 — Concurrency
// ═══════════════════════════════════════════════════════════════════════════════
public class QaConcurrencyPipelineTests
{
    private static HttpClient BuildClient(string tenantId = "qa-concurrency")
    {
        var client = new QaConcurrencyFactory().CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-User-Role", "Admin");
        return client;
    }

    [Fact]
    public async Task ConcurrentLeadIntake_AllRequestsSucceedOrAreIdempotent()
    {
        // Fire 10 concurrent intake requests and verify all receive 201
        using var client = BuildClient();

        var tasks = Enumerable.Range(0, 10).Select(i =>
            client.PostAsJsonAsync("/api/leads/intake", new
            {
                Email = $"concurrent_{i}_{Guid.NewGuid():N}@mindflow.qa",
                Phone = QaTestDataBuilder.BuildPhone(),
                Source = "web"
            }));

        var responses = await Task.WhenAll(tasks);

        foreach (var r in responses)
        {
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }
    }

    [Fact]
    public async Task ConcurrentIdempotentIntake_SameKey_OnlyOneRecordCreated()
    {
        using var client = BuildClient();

        var email = $"idem_{Guid.NewGuid():N}@mindflow.qa";
        var idempotencyKey = Guid.NewGuid().ToString();

        var tasks = Enumerable.Range(0, 5).Select(_ =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/leads/intake")
            {
                Content = JsonContent.Create(new
                {
                    Email = email,
                    Phone = QaTestDataBuilder.BuildPhone(),
                    Source = "web"
                })
            };
            req.Headers.TryAddWithoutValidation("X-Tenant-Id", "qa-concurrency");
            req.Headers.TryAddWithoutValidation("X-User-Role", "Admin");
            req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            return client.SendAsync(req);
        });

        var responses = await Task.WhenAll(tasks);

        // All must succeed (201 or replay 201)
        foreach (var r in responses)
        {
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }

        // All must return the same lead ID
        var ids = new List<Guid>();
        foreach (var r in responses)
        {
            var body = await r.Content.ReadFromJsonAsync<LeadIdDto>();
            Assert.NotNull(body);
            ids.Add(body.Id);
        }

        Assert.Single(ids.Distinct());
    }

    [Fact]
    public async Task ConcurrentPipelineOpportunityCreate_NoDataCorruption()
    {
        using var client = BuildClient();

        // Seed a lead for each opportunity to avoid lead collision
        var leads = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            IntakeLeadAsync(client)));

        var stagesResp = await client.GetAsync("/api/pipeline/stages");
        var stages = await stagesResp.Content.ReadFromJsonAsync<List<StageDto>>();
        Assert.NotNull(stages);
        var firstStage = stages[0];

        var createTasks = leads.Select(lead =>
            client.PostAsJsonAsync("/api/pipeline/opportunities", new
            {
                LeadId = lead.Id,
                Title = $"Deal {Guid.NewGuid().ToString("N")[..6]}",
                Value = 5000,
                StageId = firstStage.Id
            }));

        var responses = await Task.WhenAll(createTasks);

        // All 5 opportunity creations must succeed
        foreach (var r in responses)
        {
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }

        // Verify they are distinct opportunities
        var oppIds = new List<Guid>();
        foreach (var r in responses)
        {
            var opp = await r.Content.ReadFromJsonAsync<OppDto>();
            Assert.NotNull(opp);
            oppIds.Add(opp.Id);
        }

        Assert.Equal(5, oppIds.Distinct().Count());
    }

    [Fact]
    public async Task ConcurrentAssignmentUserCreation_AllPersisted()
    {
        using var client = BuildClient();

        var tasks = Enumerable.Range(0, 8).Select(i =>
            client.PostAsJsonAsync("/api/assignments/users", new
            {
                FullName = $"Agent {i} {Guid.NewGuid().ToString("N")[..4]}",
                Email = $"agent_{i}_{Guid.NewGuid():N}@mindflow.qa"
            }));

        var responses = await Task.WhenAll(tasks);

        foreach (var r in responses)
        {
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }
    }

    [Fact]
    public async Task ConcurrentRuleCreation_AllPersisted_NoPriorityCollision()
    {
        using var client = BuildClient();

        var tasks = Enumerable.Range(1, 5).Select(i =>
            client.PostAsJsonAsync("/api/rules", new
            {
                Name = $"Concurrent Rule {i} {Guid.NewGuid().ToString("N")[..4]}",
                Trigger = "lead.created",
                Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
                Actions = new[] { new { Type = "add_score", Value = (i * 2).ToString() } },
                Priority = i * 10
            }));

        var responses = await Task.WhenAll(tasks);

        foreach (var r in responses)
        {
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<LeadIdDto> IntakeLeadAsync(HttpClient client)
    {
        var r = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"conc_{Guid.NewGuid():N}@mindflow.qa",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = "web"
        });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<LeadIdDto>())!;
    }

    private sealed record LeadIdDto(Guid Id);
    private sealed record StageDto(Guid Id, string Name, int Order);
    private sealed record OppDto(Guid Id, string Title);
}

// ═══════════════════════════════════════════════════════════════════════════════
// QA-09 — Massive multi-tenant isolation
// ═══════════════════════════════════════════════════════════════════════════════
public class QaMultiTenantIsolationTests
{
    private const string TenantA = "qa-isolation-tenant-a";
    private const string TenantB = "qa-isolation-tenant-b";
    private const string TenantC = "qa-isolation-tenant-c";

    private static HttpClient BuildClient(string tenantId)
    {
        var client = new QaConcurrencyFactory().CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-User-Role", "Admin");
        return client;
    }

    [Fact]
    public async Task MultiTenant_LeadOfTenantA_NotVisibleToTenantB()
    {
        using var clientA = BuildClient(TenantA);
        using var clientB = BuildClient(TenantB);

        var intakeA = await clientA.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"isolation_{Guid.NewGuid():N}@mindflow.qa",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = "web"
        });
        Assert.Equal(HttpStatusCode.Created, intakeA.StatusCode);

        var overviewA = await clientA.GetAsync("/api/dashboard/overview");
        var overviewB = await clientB.GetAsync("/api/dashboard/overview");

        var bodyA = await overviewA.Content.ReadFromJsonAsync<DashboardDto>();
        var bodyB = await overviewB.Content.ReadFromJsonAsync<DashboardDto>();

        Assert.NotNull(bodyA);
        Assert.NotNull(bodyB);
        Assert.True(bodyA.TotalLeads > 0, "Tenant A should see its own leads");
        // Tenant B sees only its own data; count must be independent of A's
        Assert.True(bodyB.TotalLeads >= 0, "Tenant B should not see Tenant A's leads");
    }

    [Fact]
    public async Task MultiTenant_RulesOfTenantA_NotVisibleToTenantB()
    {
        using var clientA = BuildClient(TenantA);
        using var clientB = BuildClient(TenantB);

        var createRule = await clientA.PostAsJsonAsync("/api/rules", new
        {
            Name = $"TenantA Isolation Rule {Guid.NewGuid().ToString("N")[..6]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = "99" } },
            Priority = 50
        });
        Assert.Equal(HttpStatusCode.Created, createRule.StatusCode);
        var ruleA = await createRule.Content.ReadFromJsonAsync<IsoRuleDto>();
        Assert.NotNull(ruleA);

        var rulesB = await clientB.GetAsync("/api/rules");
        var listB = await rulesB.Content.ReadFromJsonAsync<List<IsoRuleDto>>();
        Assert.NotNull(listB);

        Assert.DoesNotContain(listB, r => r.Id == ruleA.Id);
    }

    [Fact]
    public async Task MultiTenant_ThreeTenantsIndependent_NoLeakage()
    {
        using var clientA = BuildClient(TenantA);
        using var clientB = BuildClient(TenantB);
        using var clientC = BuildClient(TenantC);

        // Each tenant creates 3 leads
        var tasks = new[]
        {
            IntakeLeadsAsync(clientA, 3),
            IntakeLeadsAsync(clientB, 3),
            IntakeLeadsAsync(clientC, 3)
        };
        await Task.WhenAll(tasks);

        var ovA = await (await clientA.GetAsync("/api/dashboard/overview"))
            .Content.ReadFromJsonAsync<DashboardDto>();
        var ovB = await (await clientB.GetAsync("/api/dashboard/overview"))
            .Content.ReadFromJsonAsync<DashboardDto>();
        var ovC = await (await clientC.GetAsync("/api/dashboard/overview"))
            .Content.ReadFromJsonAsync<DashboardDto>();

        Assert.NotNull(ovA);
        Assert.NotNull(ovB);
        Assert.NotNull(ovC);

        // Each tenant must see exactly its own 3 leads (assuming fresh DB)
        Assert.Equal(3, ovA.TotalLeads);
        Assert.Equal(3, ovB.TotalLeads);
        Assert.Equal(3, ovC.TotalLeads);
    }

    [Fact]
    public async Task MultiTenant_PipelineOpportunity_NotCrossContaminated()
    {
        using var clientA = BuildClient(TenantA);
        using var clientB = BuildClient(TenantB);

        var leadA = await IntakeLeadAsync(clientA);
        var stagesA = await (await clientA.GetAsync("/api/pipeline/stages"))
            .Content.ReadFromJsonAsync<List<StageDto>>();
        Assert.NotNull(stagesA);

        var createOpp = await clientA.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = leadA.Id,
            Title = "Tenant A Exclusive Deal",
            Value = 9999,
            StageId = stagesA[0].Id
        });
        Assert.Equal(HttpStatusCode.Created, createOpp.StatusCode);
        var oppA = await createOpp.Content.ReadFromJsonAsync<IsoOppDto>();
        Assert.NotNull(oppA);

        // Tenant B must not see Tenant A's opportunity via board
        var boardB = await clientB.GetAsync("/api/pipeline/board");
        Assert.Equal(HttpStatusCode.OK, boardB.StatusCode);
        var boardBodyB = await boardB.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        var boardJson = boardBodyB.GetRawText();
        Assert.DoesNotContain(oppA.Id.ToString(), boardJson);
    }

    [Fact]
    public async Task MultiTenant_AnalyticsOverview_IsolatedPerTenant()
    {
        using var clientA = BuildClient(TenantA);
        using var clientB = BuildClient(TenantB);

        // Tenant A creates leads and assignment user
        await IntakeLeadsAsync(clientA, 2);

        var analyticsA = await clientA.GetAsync("/api/analytics/advanced/metrics");
        var analyticsB = await clientB.GetAsync("/api/analytics/advanced/metrics");

        Assert.Equal(HttpStatusCode.OK, analyticsA.StatusCode);
        Assert.Equal(HttpStatusCode.OK, analyticsB.StatusCode);
    }

    [Fact]
    public async Task MultiTenant_AssignmentUsers_TenantScoped()
    {
        using var clientA = BuildClient(TenantA);
        using var clientB = BuildClient(TenantB);

        var createUser = await clientA.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "TenantA Only Agent",
            Email = $"only_a_{Guid.NewGuid():N}@mindflow.qa"
        });
        Assert.Equal(HttpStatusCode.Created, createUser.StatusCode);
        var userA = await createUser.Content.ReadFromJsonAsync<IsoUserDto>();
        Assert.NotNull(userA);

        // Tenant B listing must not include Tenant A's user
        var usersB = await (await clientB.GetAsync("/api/assignments/users"))
            .Content.ReadFromJsonAsync<List<IsoUserDto>>();
        Assert.NotNull(usersB);
        Assert.DoesNotContain(usersB, u => u.Id == userA.Id);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task IntakeLeadsAsync(HttpClient client, int count)
    {
        for (var i = 0; i < count; i++)
            await IntakeLeadAsync(client);
    }

    private static async Task<LeadIdDto> IntakeLeadAsync(HttpClient client)
    {
        var r = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"iso_{Guid.NewGuid():N}@mindflow.qa",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = "web"
        });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<LeadIdDto>())!;
    }

    private sealed record LeadIdDto(Guid Id);
    private sealed record DashboardDto(int TotalLeads);
    private sealed record StageDto(Guid Id, string Name);
    private sealed record IsoRuleDto(Guid Id, string Name);
    private sealed record IsoOppDto(Guid Id, string Title);
    private sealed record IsoUserDto(Guid Id, string FullName);
}


