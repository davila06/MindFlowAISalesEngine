using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class RulesTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"rules_test_{Guid.NewGuid():N}.db";

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

public class RulesEngineEndpointTests
{
    private static HttpClient BuildClient() => new RulesTestFactory().CreateClient();

    [Fact]
    public async Task CreateRule_AndListRules_ReturnsCreatedRule()
    {
        using var client = BuildClient();

        var create = await CreateAddScoreRule(client, points: 20);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);
        Assert.True(rule.IsActive);
        Assert.Equal("lead.created", rule.Trigger);

        var list = await client.GetAsync("/api/rules");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var rules = await list.Content.ReadFromJsonAsync<List<RuleResponseDto>>();
        Assert.NotNull(rules);
        Assert.Contains(rules, r => r.Id == rule.Id);
    }

    [Fact]
    public async Task IntakeLead_WithActiveRule_AppliesAction()
    {
        using var client = BuildClient();

        var create = await CreateAddScoreRule(client, points: 20);
        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"rule_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "unknown"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);

        var lead = await intake.Content.ReadFromJsonAsync<LeadScoredDto>();
        Assert.NotNull(lead);
        Assert.Equal(90, lead.Score);
    }

    [Fact]
    public async Task DeactivateRule_StopsApplyingActions()
    {
        using var client = BuildClient();

        var create = await CreateAddScoreRule(client, points: 20);
        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);

        var deactivate = await client.PostAsync($"/api/rules/{rule.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"rule_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "unknown"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);

        var lead = await intake.Content.ReadFromJsonAsync<LeadScoredDto>();
        Assert.NotNull(lead);
        Assert.Equal(70, lead.Score);
    }

    [Fact]
    public async Task ActivateRule_AfterDeactivation_AppliesAgain()
    {
        using var client = BuildClient();

        var create = await CreateAddScoreRule(client, points: 20);
        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);

        await client.PostAsync($"/api/rules/{rule.Id}/deactivate", null);

        var activate = await client.PostAsync($"/api/rules/{rule.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"rule_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "unknown"
        });

        var lead = await intake.Content.ReadFromJsonAsync<LeadScoredDto>();
        Assert.NotNull(lead);
        Assert.Equal(90, lead.Score);
    }

    [Fact]
    public async Task DeleteRule_RemovesItFromCatalog()
    {
        using var client = BuildClient();

        var create = await CreateAddScoreRule(client, points: 20);
        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);

        var delete = await client.DeleteAsync($"/api/rules/{rule.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var byId = await client.GetAsync($"/api/rules/{rule.Id}");
        Assert.Equal(HttpStatusCode.NotFound, byId.StatusCode);
    }

    [Fact]
    public async Task PromoteRule_ToProduction_UpdatesGovernanceMetadata()
    {
        using var client = BuildClient();

        var create = await CreateAddScoreRule(client, points: 10);
        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);

        var promote = await client.PostAsJsonAsync($"/api/rules/{rule.Id}/promote", new
        {
            TargetEnvironment = "prod",
            ApprovedBy = "qa.lead@novamind.test"
        });

        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);

        var promoted = await promote.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(promoted);
        Assert.Equal("prod", promoted.Environment);
        Assert.Equal("approved", promoted.ApprovalStatus);
        Assert.Equal(2, promoted.Version);
    }

    [Fact]
    public async Task GetDriftSummary_ReturnsEnvironmentAndApprovalBreakdown()
    {
        using var client = BuildClient();

        await CreateAddScoreRule(client, points: 10);
        var secondCreate = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = "Stage rule",
            Trigger = "lead.created",
            IsActive = true,
            Environment = "stg",
            ApprovalStatus = "draft",
            Conditions = new[]
            {
                new { Field = "source", Operator = "eq", Value = "unknown" }
            },
            Actions = new[]
            {
                new { Type = "set_priority", Value = "High" }
            }
        });
        Assert.Equal(HttpStatusCode.Created, secondCreate.StatusCode);

        var summaryResponse = await client.GetAsync("/api/rules/drift-summary");
        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);

        var summary = await summaryResponse.Content.ReadFromJsonAsync<RuleDriftSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.TotalRules >= 2);
        Assert.True(summary.DraftRules >= 1);
        Assert.True(summary.NonProductionActiveRules >= 1);
        Assert.Contains(summary.ByEnvironment, x => x.Environment == "stg" && x.Count >= 1);
    }

    [Fact]
    public async Task CreateRule_WithInvalidDsl_ReturnsBadRequest()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = "invalid rule",
            Trigger = "lead.created",
            IsActive = true,
            Conditions = new[]
            {
                new { Field = "to_stage", Operator = "eq", Value = "proposal" }
            },
            Actions = new[]
            {
                new { Type = "move_stage", Value = "won" }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRule_ReplacesConditionsAndActions_WithoutServerError()
    {
        using var client = BuildClient();

        var create = await CreateAddScoreRule(client, points: 10);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var createdRule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(createdRule);

        var update = await client.PutAsJsonAsync($"/api/rules/{createdRule.Id}", new
        {
            Name = "Unknown source priority boost",
            Trigger = "lead.created",
            IsActive = true,
            Priority = 150,
            ConflictPolicy = "first_wins",
            CooldownMinutes = 30,
            Environment = "dev",
            ApprovalStatus = "approved",
            Conditions = new[]
            {
                new { Field = "source", Operator = "eq", Value = "unknown" },
                new { Field = "priority", Operator = "eq", Value = "High" }
            },
            Actions = new[]
            {
                new { Type = "add_score", Value = "10" },
                new { Type = "set_priority", Value = "High" }
            }
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var updatedRule = await update.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(updatedRule);
        Assert.Equal("Unknown source priority boost", updatedRule.Name);
        Assert.Equal(2, updatedRule.Conditions.Count);
        Assert.Equal(2, updatedRule.Actions.Count);

        var byId = await client.GetFromJsonAsync<RuleResponseDto>($"/api/rules/{createdRule.Id}");
        Assert.NotNull(byId);
        Assert.Equal(2, byId.Conditions.Count);
        Assert.Equal(2, byId.Actions.Count);
    }

    [Fact]
    public async Task DryRun_AndMetrics_ExposeRuleEffectiveness()
    {
        using var client = BuildClient();

        var create = await CreateAddScoreRule(client, points: 5);
        var rule = await create.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(rule);

        await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"dryrun_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "unknown"
        });

        var dryRun = await client.PostAsync($"/api/rules/{rule.Id}/dry-run", null);
        Assert.Equal(HttpStatusCode.OK, dryRun.StatusCode);
        var dryRunBody = await dryRun.Content.ReadFromJsonAsync<RuleDryRunDto>();
        Assert.NotNull(dryRunBody);
        Assert.True(dryRunBody.TotalEvaluated >= 1);
        Assert.True(dryRunBody.MatchedCount >= 1);

        var lead = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"metrics_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "unknown"
        });

        var leadBody = await lead.Content.ReadFromJsonAsync<LeadScoredDto>();
        Assert.NotNull(leadBody);

        var dispatch = await client.PostAsJsonAsync("/api/rules/events/dispatch", new
        {
            Trigger = "lead.responded",
            LeadId = leadBody.Id
        });
        Assert.Equal(HttpStatusCode.OK, dispatch.StatusCode);

        var metrics = await client.GetAsync($"/api/rules/{rule.Id}/metrics");
        Assert.Equal(HttpStatusCode.OK, metrics.StatusCode);
        var metricsBody = await metrics.Content.ReadFromJsonAsync<RuleMetricsDto>();
        Assert.NotNull(metricsBody);
        Assert.True(metricsBody.TotalExecutions >= 1);
    }

    [Fact]
    public async Task Templates_FixtureAndSandbox_AreAvailable()
    {
        using var client = BuildClient();

        var templates = await client.GetAsync("/api/rules/templates");
        Assert.Equal(HttpStatusCode.OK, templates.StatusCode);
        var templatesBody = await templates.Content.ReadFromJsonAsync<List<RuleTemplateDto>>();
        Assert.NotNull(templatesBody);
        Assert.NotEmpty(templatesBody);

        var createSandbox = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = "sandbox-rule",
            Trigger = "lead.responded",
            IsActive = true,
            Environment = "sandbox",
            ApprovalStatus = "approved",
            Conditions = new[]
            {
                new { Field = "source", Operator = "eq", Value = "unknown" }
            },
            Actions = new[]
            {
                new { Type = "set_priority", Value = "High" }
            }
        });

        Assert.Equal(HttpStatusCode.Created, createSandbox.StatusCode);
        var sandboxRule = await createSandbox.Content.ReadFromJsonAsync<RuleResponseDto>();
        Assert.NotNull(sandboxRule);
        Assert.Equal("sandbox", sandboxRule.Environment);

        var fixture = await client.PostAsJsonAsync("/api/rules/test-fixture", new
        {
            RuleId = sandboxRule.Id,
            Trigger = "lead.responded",
            Lead = new
            {
                Source = "unknown",
                Priority = "Low",
                Score = 50,
                HasEmail = true,
                HasPhone = true
            }
        });

        Assert.Equal(HttpStatusCode.OK, fixture.StatusCode);
        var fixtureBody = await fixture.Content.ReadFromJsonAsync<RuleFixtureTestDto>();
        Assert.NotNull(fixtureBody);
        Assert.True(fixtureBody.Matched);
    }

    [Fact]
    public async Task CreateRule_WithDestructiveActionWithoutGuardrail_ReturnsBadRequest()
    {
        using var client = BuildClient();

        var create = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = "destructive-stage-rule",
            Trigger = "stage_changed",
            IsActive = true,
            Conditions = new[]
            {
                new { Field = "to_stage", Operator = "eq", Value = "proposal" }
            },
            Actions = new[]
            {
                new { Type = "move_stage", Value = "won" }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    private static Task<HttpResponseMessage> CreateAddScoreRule(HttpClient client, int points)
    {
        return client.PostAsJsonAsync("/api/rules", new
        {
            Name = "Unknown source score boost",
            Trigger = "lead.created",
            IsActive = true,
            Conditions = new[]
            {
                new { Field = "source", Operator = "eq", Value = "unknown" }
            },
            Actions = new[]
            {
                new { Type = "add_score", Value = points.ToString() }
            }
        });
    }

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

file sealed class RuleResponseDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Trigger { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public int Version { get; init; }
    public string Environment { get; init; } = string.Empty;
    public string ApprovalStatus { get; init; } = string.Empty;
    public List<RuleConditionDto> Conditions { get; init; } = [];
    public List<RuleActionDto> Actions { get; init; } = [];
}

file sealed class RuleConditionDto
{
    public string Field { get; init; } = string.Empty;
    public string Operator { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

file sealed class RuleActionDto
{
    public string Type { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

file sealed class RuleDryRunDto
{
    public int TotalEvaluated { get; init; }
    public int MatchedCount { get; init; }
}

file sealed class RuleMetricsDto
{
    public int TotalExecutions { get; init; }
}

file sealed class RuleTemplateDto
{
    public string Key { get; init; } = string.Empty;
}

file sealed class RuleFixtureTestDto
{
    public bool Matched { get; init; }
}

file sealed class LeadScoredDto
{
    public Guid Id { get; init; }
    public int Score { get; init; }
}

file sealed class RuleDriftSummaryDto
{
    public int TotalRules { get; init; }
    public int DraftRules { get; init; }
    public int RejectedRules { get; init; }
    public int NonProductionActiveRules { get; init; }
    public List<RuleDriftEnvironmentDto> ByEnvironment { get; init; } = [];
}

file sealed class RuleDriftEnvironmentDto
{
    public string Environment { get; init; } = string.Empty;
    public int Count { get; init; }
}
