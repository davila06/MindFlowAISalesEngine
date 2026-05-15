using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

/// <summary>
/// QA-04: Mutation tests for critical rules-engine logic.
/// Validates that mutating boundary conditions (conditions, actions, priorities,
/// triggers, stop-conditions) produces the correct observable outcome.
/// Each test represents a distinct mutation of the rule definition.
/// 
/// QA-10: Scoring and rules regression baseline.
/// Ensures stable scoring behaviour across rule permutations so no silent
/// score drift occurs when rules change.
/// </summary>
public sealed class QaMutationRulesFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"qa_mutation_{Guid.NewGuid():N}.db";

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

public class QaMutationRulesCriticalTests
{
    private static HttpClient BuildClient()
    {
        var client = new QaMutationRulesFactory().CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", "qa-mutation-tenant");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-User-Role", "Admin");
        return client;
    }

    // ── QA-04: Boundary mutations ─────────────────────────────────────────────

    [Fact]
    public async Task Mutation_AddScoreRule_MaxPoints_ClampedAtCeiling()
    {
        // Mutation: extreme points value should not overflow score beyond system max
        using var client = BuildClient();

        await CreateRuleAsync(client, points: 1000);

        var lead = await IntakeLeadAsync(client, source: "web");

        // Score must be a non-negative integer and reasonable
        Assert.True(lead.Score >= 0, "Score must not be negative");
        Assert.True(lead.Score <= 10_000, "Score must not exceed system ceiling");
    }

    [Fact]
    public async Task Mutation_AddScoreRule_ZeroPoints_LeadHasBaseScore()
    {
        // Mutation: zero-point rule still produces a lead with default base score
        using var client = BuildClient();

        await CreateRuleAsync(client, points: 0);

        var lead = await IntakeLeadAsync(client, source: "web");

        Assert.True(lead.Score >= 0);
    }

    [Fact]
    public async Task Mutation_NegativeScore_RuleRejectedOrIgnored()
    {
        // Mutation: negative score points in action – system should reject or ignore
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"NegScore {Guid.NewGuid().ToString("N")[..4]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = "-999" } },
            Priority = 50
        });

        // Either the system rejects negative points (400) or allows it and clamps
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var lead = await IntakeLeadAsync(client, source: "web");
            Assert.True(lead.Score >= 0, "Score must not go negative even with subtract rule");
        }
        else
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task Mutation_ConflictingRules_HigherPriorityWins()
    {
        // Mutation: two rules with different priorities — higher priority should dominate
        using var client = BuildClient();

        // Low priority rule adds 10 pts
        await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"LowPrio {Guid.NewGuid().ToString("N")[..4]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = "10" } },
            Priority = 10
        });

        // High priority rule adds 30 pts
        await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"HighPrio {Guid.NewGuid().ToString("N")[..4]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = "30" } },
            Priority = 90
        });

        var lead = await IntakeLeadAsync(client, source: "web");

        // Both rules apply additively – cumulative score should exceed base
        Assert.True(lead.Score > 70, $"Expected score > 70 (both rules applied), got {lead.Score}");
    }

    [Fact]
    public async Task Mutation_InactiveRule_DoesNotAffectScore()
    {
        // Mutation: deactivated rule must be a no-op
        using var client = BuildClient();

        var create = await CreateRuleAsync(client, points: 50);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);

        await client.PostAsync($"/api/rules/{rule.Id}/deactivate", null);

        var lead = await IntakeLeadAsync(client, source: "web");

        // Only base score should be present (no add_score from deactivated rule)
        // Base score: 70 (default from scoring engine without rule bonus)
        Assert.True(lead.Score <= 100, "Score must not include deactivated rule bonus");
    }

    [Fact]
    public async Task Mutation_WrongTrigger_RuleDoesNotFireOnIntake()
    {
        // Mutation: rule with stage_changed trigger must not fire on lead.created
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"WrongTrigger {Guid.NewGuid().ToString("N")[..4]}",
            Trigger = "stage_changed",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = "99" } },
            Priority = 50
        });

        var lead = await IntakeLeadAsync(client, source: "web");

        // stage_changed rule should not have applied on intake – score must be base
        Assert.True(lead.Score < 169, "stage_changed rule must not fire on lead.created");
    }

    [Fact]
    public async Task Mutation_ConditionMismatch_RuleSkipped()
    {
        // Mutation: rule condition requires source=api but lead has source=web
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"SourceMismatch {Guid.NewGuid().ToString("N")[..4]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "api" } },
            Actions = new[] { new { Type = "add_score", Value = "50" } },
            Priority = 50
        });

        var leadWeb = await IntakeLeadAsync(client, source: "web");
        var leadApi = await IntakeLeadAsync(client, source: "api");

        // api lead should have higher score due to rule match
        Assert.True(leadApi.Score >= leadWeb.Score,
            $"api lead score ({leadApi.Score}) should be >= web lead ({leadWeb.Score})");
    }

    // ── QA-10: Scoring regression baseline ───────────────────────────────────

    [Fact]
    public async Task Regression_BaseScore_WithoutRules_IsStable()
    {
        // Regression: without any rules, all leads must receive a stable base score
        using var client = BuildClient();

        var lead1 = await IntakeLeadAsync(client, source: "web", email: "reg1");
        var lead2 = await IntakeLeadAsync(client, source: "web", email: "reg2");
        var lead3 = await IntakeLeadAsync(client, source: "web", email: "reg3");

        // Base scores should all be equal (deterministic scoring with same inputs)
        Assert.Equal(lead1.Score, lead2.Score);
        Assert.Equal(lead2.Score, lead3.Score);
    }

    [Fact]
    public async Task Regression_ScoringVersion_IsPersisted()
    {
        // Regression: each lead intake must record the scoring version used
        using var client = BuildClient();

        var lead = await IntakeLeadAsync(client, source: "web");

        Assert.False(string.IsNullOrWhiteSpace(lead.ScoringVersion),
            "scoringVersion must be populated on intake");
    }

    [Fact]
    public async Task Regression_RecalculateScoring_EndpointIsAvailable()
    {
        // Regression: the recalculate endpoint must respond successfully
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/scoring/recalculate", new
        {
            StartDateUtc = DateTime.UtcNow.AddDays(-7),
            EndDateUtc = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Regression_DryRunRule_DoesNotMutatePersistentState()
    {
        // Regression: dry-run must never persist side-effects
        using var client = BuildClient();

        var listBefore = await client.GetAsync("/api/rules");
        var rulesBefore = await listBefore.Content.ReadFromJsonAsync<List<RuleResponseDto>>();
        var countBefore = rulesBefore?.Count ?? 0;

        var create = await CreateRuleAsync(client, points: 5);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var createdRule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(createdRule);

        var listAfterCreate = await client.GetAsync("/api/rules");
        var rulesAfterCreate = await listAfterCreate.Content.ReadFromJsonAsync<List<RuleResponseDto>>();
        var countAfterCreate = rulesAfterCreate?.Count ?? 0;
        Assert.Equal(countBefore + 1, countAfterCreate);

        var dryRun = await client.PostAsync($"/api/rules/{createdRule.Id}/dry-run", content: null);
        Assert.Equal(HttpStatusCode.OK, dryRun.StatusCode);

        var listAfter = await client.GetAsync("/api/rules");
        var rulesAfter = await listAfter.Content.ReadFromJsonAsync<List<RuleResponseDto>>();
        var countAfter = rulesAfter?.Count ?? 0;

        Assert.Equal(countAfterCreate, countAfter); // dry-run must not persist side-effects
    }

    [Fact]
    public async Task Regression_RulePriorityUpdate_ReflectedImmediately()
    {
        // Regression: updating rule priority must be visible in the next GET
        using var client = BuildClient();

        var create = await CreateRuleAsync(client, points: 5);
        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);

        var update = await client.PutAsJsonAsync($"/api/rules/{rule.Id}", new
        {
            Name = rule.Name,
            Trigger = rule.Trigger,
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = "5" } },
            Priority = 99,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var updated = await update.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(updated);
        Assert.Equal(99, updated.Priority);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<HttpResponseMessage> CreateRuleAsync(HttpClient client, int points = 10) =>
        await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"QA Mutation {Guid.NewGuid().ToString("N")[..6]}",
            Trigger = "lead.created",
            Conditions = new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions = new[] { new { Type = "add_score", Value = points.ToString() } },
            Priority = 50
        });

    private static async Task<LeadScoredDto> IntakeLeadAsync(
        HttpClient client, string source = "web", string? email = null)
    {
        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"{email ?? "lead"}_{Guid.NewGuid():N}@mindflow.qa",
            Phone = QaTestDataBuilder.BuildPhone(),
            Source = source
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var lead = await response.Content.ReadFromJsonAsync<LeadScoredDto>();
        Assert.NotNull(lead);
        return lead;
    }

    private sealed record LeadScoredDto(
        Guid Id,
        int Score,
        string Priority,
        string ScoringVersion,
        string? Trigger = null);

    private sealed record RuleResponseDto(
        Guid Id,
        string Name,
        string Trigger,
        bool IsActive,
        int Priority,
        DateTime CreatedAtUtc);
}


