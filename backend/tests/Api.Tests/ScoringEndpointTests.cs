using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class ScoringTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"scoring_test_{Guid.NewGuid():N}.db";

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

public class ScoringEndpointTests
{
    private static HttpClient BuildClient() => new ScoringTestFactory().CreateClient();

    [Fact]
    public async Task IntakeLead_PersistsScoreAndPriority_InCreatedResponse()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"score_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "referral"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var lead = await response.Content.ReadFromJsonAsync<LeadScoredResponse>();
        Assert.NotNull(lead);
        Assert.True(lead.Score > 0);
        Assert.True(lead.Priority is "Low" or "Medium" or "High");
    }

    [Fact]
    public async Task GetLeadScore_ByLeadId_ReturnsPersistedScore()
    {
        using var client = BuildClient();

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"score_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "referral"
        });

        var lead = await intake.Content.ReadFromJsonAsync<LeadScoredResponse>();
        Assert.NotNull(lead);

        var scoreResponse = await client.GetAsync($"/api/scoring/leads/{lead.Id}");
        Assert.Equal(HttpStatusCode.OK, scoreResponse.StatusCode);

        var score = await scoreResponse.Content.ReadFromJsonAsync<LeadScoreResponse>();
        Assert.NotNull(score);
        Assert.Equal(lead.Id, score.LeadId);
        Assert.Equal(lead.Score, score.Score);
        Assert.Equal(lead.Priority, score.Priority);
    }

    [Fact]
    public async Task GetScoringRules_ReturnsRuleCatalogWithThresholds()
    {
        using var client = BuildClient();

        var response = await client.GetAsync("/api/scoring/rules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rules = await response.Content.ReadFromJsonAsync<List<ScoreRuleResponse>>();
        Assert.NotNull(rules);
        Assert.NotEmpty(rules);
        Assert.Contains(rules, r => r.Key == "has_email");
        Assert.Contains(rules, r => r.Key == "has_phone");
    }

    [Fact]
    public async Task IntakeLead_ReferralHasHigherScoreThanUnknownSource()
    {
        using var client = BuildClient();

        var referral = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"ref_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "referral"
        });

        var unknown = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"unk_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "unknown"
        });

        var referralLead = await referral.Content.ReadFromJsonAsync<LeadScoredResponse>();
        var unknownLead = await unknown.Content.ReadFromJsonAsync<LeadScoredResponse>();

        Assert.NotNull(referralLead);
        Assert.NotNull(unknownLead);
        Assert.True(referralLead.Score > unknownLead.Score);
    }

    [Fact]
    public async Task PriorityThresholds_GetAndUpdate_AreTenantScoped()
    {
        using var tenantAClient = BuildClient();
        using var tenantBClient = BuildClient();

        tenantAClient.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-a");
        tenantBClient.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-b");

        var defaults = await tenantAClient.GetFromJsonAsync<ScoringPriorityThresholdsDto>("/api/scoring/priority-thresholds");
        Assert.NotNull(defaults);
        Assert.Equal(80, defaults.HotMinScore);
        Assert.Equal(50, defaults.WarmMinScore);

        var updateResponse = await tenantAClient.PutAsJsonAsync("/api/scoring/priority-thresholds", new
        {
            HotMinScore = 95,
            WarmMinScore = 85
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var tenantAThresholds = await tenantAClient.GetFromJsonAsync<ScoringPriorityThresholdsDto>("/api/scoring/priority-thresholds");
        Assert.NotNull(tenantAThresholds);
        Assert.Equal(95, tenantAThresholds.HotMinScore);
        Assert.Equal(85, tenantAThresholds.WarmMinScore);

        var tenantBThresholds = await tenantBClient.GetFromJsonAsync<ScoringPriorityThresholdsDto>("/api/scoring/priority-thresholds");
        Assert.NotNull(tenantBThresholds);
        Assert.Equal(80, tenantBThresholds.HotMinScore);
        Assert.Equal(50, tenantBThresholds.WarmMinScore);
    }

    [Fact]
    public async Task IntakeLead_AfterThresholdUpdate_UsesTenantThresholdsForPriority()
    {
        using var client = BuildClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-thresholds");

        var updateResponse = await client.PutAsJsonAsync("/api/scoring/priority-thresholds", new
        {
            HotMinScore = 95,
            WarmMinScore = 85
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"score_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "referral"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);
        var lead = await intake.Content.ReadFromJsonAsync<LeadScoredResponse>();
        Assert.NotNull(lead);
        Assert.Equal(90, lead.Score);
        Assert.Equal("Medium", lead.Priority);
    }

    [Fact]
    public async Task ScoreDrift_WithInsufficientSamples_ReturnsNoDrift()
    {
        using var client = BuildClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-drift-empty");

        var response = await client.GetAsync("/api/scoring/drift?currentSampleSize=5&baselineSampleSize=5&driftThresholdPercent=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ScoringDriftDto>();
        Assert.NotNull(payload);
        Assert.False(payload.HasDrift);
        Assert.Equal(0, payload.CurrentSampleCount);
        Assert.Equal(0, payload.BaselineSampleCount);
    }

    [Fact]
    public async Task ScoreDrift_WhenRecentScoresDrop_ReturnsDriftSignal()
    {
        using var client = BuildClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-drift");

        for (var i = 0; i < 6; i++)
        {
            var highLead = await client.PostAsJsonAsync("/api/leads/intake", new
            {
                Email = $"high_{i}_{Guid.NewGuid():N}@mindflow.test",
                Phone = BuildUniquePhone(),
                Source = "referral"
            });
            Assert.Equal(HttpStatusCode.Created, highLead.StatusCode);
        }

        for (var i = 0; i < 6; i++)
        {
            var lowLead = await client.PostAsJsonAsync("/api/leads/intake", new
            {
                Email = $"low_{i}_{Guid.NewGuid():N}@mindflow.test",
                Source = "unknown"
            });
            Assert.Equal(HttpStatusCode.Created, lowLead.StatusCode);
        }

        var response = await client.GetAsync("/api/scoring/drift?currentSampleSize=6&baselineSampleSize=6&driftThresholdPercent=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ScoringDriftDto>();
        Assert.NotNull(payload);
        Assert.True(payload.HasDrift);
        Assert.Equal(6, payload.CurrentSampleCount);
        Assert.Equal(6, payload.BaselineSampleCount);
        Assert.True(payload.CurrentAverageScore < payload.BaselineAverageScore);
        Assert.Contains("average_score_drop", payload.DriftSignals);
    }

    [Fact]
    public async Task ScoringFormulaProposal_Approve_UpdatesActiveVersion()
    {
        using var client = BuildClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "tenant-formula");

        var create = await client.PostAsJsonAsync("/api/scoring/formula/proposals", new
        {
            RequestedBy = "admin@mindflow.test",
            Formula = new
            {
                Version = "v2.1",
                HasEmailPoints = 30,
                HasPhonePoints = 20,
                SourceReferralPoints = 30,
                SourceWebPoints = 20,
                SourceAdsPoints = 15,
                SourceOtherPoints = 10,
                EmailPhoneBonusPoints = 10
            }
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var proposal = await create.Content.ReadFromJsonAsync<ScoringFormulaProposalDto>();
        Assert.NotNull(proposal);
        Assert.Equal("pending", proposal.Status);

        var approve = await client.PostAsync($"/api/scoring/formula/proposals/{proposal.ProposalId}/approve?approvedBy=ops", null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);

        var current = await client.GetFromJsonAsync<ScoringFormulaDto>("/api/scoring/formula");
        Assert.NotNull(current);
        Assert.Equal("v2.1", current.Version);

        var versions = await client.GetFromJsonAsync<List<ScoringFormulaDto>>("/api/scoring/formula/versions");
        Assert.NotNull(versions);
        Assert.Contains(versions, x => x.Version == "v2.1");
    }

    [Fact]
    public async Task LeadExplainability_ReturnsContributionBreakdown()
    {
        using var client = BuildClient();

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"exp_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "referral"
        });
        var lead = await intake.Content.ReadFromJsonAsync<LeadScoredResponse>();
        Assert.NotNull(lead);

        var explain = await client.GetAsync($"/api/scoring/leads/{lead.Id}/explain");
        Assert.Equal(HttpStatusCode.OK, explain.StatusCode);
        var payload = await explain.Content.ReadFromJsonAsync<ScoreExplainabilityDto>();
        Assert.NotNull(payload);
        Assert.Equal(lead.Id, payload.LeadId);
        Assert.NotEmpty(payload.Contributions);
        Assert.Contains(payload.Contributions, x => x.Key == "source_referral" && x.Applied);
    }

    [Fact]
    public async Task ScoringSimulator_ReturnsResultsAndSummary()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/scoring/simulator", new
        {
            Samples = new[]
            {
                new { Email = (string?)"a@test.com", Phone = (string?)"+15550000001", Source = "referral" },
                new { Email = (string?)null, Phone = (string?)null, Source = "unknown" }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ScoringSimulatorDto>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Results.Count);
        Assert.True(payload.AverageScore > 0);
    }

    [Fact]
    public async Task ConversionLoop_ReturnsBucketsWithWonMetrics()
    {
        using var client = BuildClient();

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"conv_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "referral"
        });
        var lead = await intake.Content.ReadFromJsonAsync<LeadScoredResponse>();
        Assert.NotNull(lead);

        var opportunity = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = lead.Id,
            StageId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Title = "test opportunity",
            Value = 5000
        });
        Assert.Equal(HttpStatusCode.Created, opportunity.StatusCode);
        var created = await opportunity.Content.ReadFromJsonAsync<OpportunityDto>();
        Assert.NotNull(created);

        var move = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{created.Id}/stage", new
        {
            TargetStageId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Reason = "closed"
        });
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);

        var loop = await client.GetFromJsonAsync<ConversionLoopDto>("/api/scoring/conversion-loop");
        Assert.NotNull(loop);
        Assert.NotEmpty(loop.Buckets);
        Assert.Contains(loop.Buckets, x => x.Won >= 0);
    }

    [Fact]
    public async Task StatisticalRegression_ReferralAverage_RemainsHigherThanUnknown()
    {
        using var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/scoring/simulator", new
        {
            Samples = Enumerable.Range(0, 20)
                .Select(i => i < 10
                    ? new { Email = (string?)$"r{i}@test.com", Phone = (string?)"+15550001234", Source = "referral" }
                    : new { Email = (string?)null, Phone = (string?)null, Source = "unknown" })
                .ToArray()
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ScoringSimulatorDto>();
        Assert.NotNull(payload);

        var referralAvg = payload.Results.Where(x => x.Index < 10).Average(x => x.Score);
        var unknownAvg = payload.Results.Where(x => x.Index >= 10).Average(x => x.Score);

        Assert.True(referralAvg > unknownAvg);
    }

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

file sealed class LeadScoredResponse
{
    public Guid Id { get; init; }
    public int Score { get; init; }
    public string Priority { get; init; } = string.Empty;
}

file sealed class LeadScoreResponse
{
    public Guid LeadId { get; init; }
    public int Score { get; init; }
    public string Priority { get; init; } = string.Empty;
}

file sealed class ScoreRuleResponse
{
    public string Key { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Points { get; init; }
}

file sealed class ScoringPriorityThresholdsDto
{
    public int HotMinScore { get; init; }
    public int WarmMinScore { get; init; }
    public int ColdMaxScore { get; init; }
}

file sealed class ScoringDriftDto
{
    public bool HasDrift { get; init; }
    public int CurrentSampleCount { get; init; }
    public int BaselineSampleCount { get; init; }
    public decimal CurrentAverageScore { get; init; }
    public decimal BaselineAverageScore { get; init; }
    public List<string> DriftSignals { get; init; } = [];
}

file sealed class ScoringFormulaProposalDto
{
    public Guid ProposalId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string RequestedBy { get; init; } = string.Empty;
}

file sealed class ScoringFormulaDto
{
    public string Version { get; init; } = string.Empty;
    public int HasEmailPoints { get; init; }
}

file sealed class ScoreExplainabilityDto
{
    public Guid LeadId { get; init; }
    public int Score { get; init; }
    public string Priority { get; init; } = string.Empty;
    public List<ScoreContributionDto> Contributions { get; init; } = [];
}

file sealed class ScoreContributionDto
{
    public string Key { get; init; } = string.Empty;
    public bool Applied { get; init; }
}

file sealed class ScoringSimulatorDto
{
    public List<ScoringSimulatorResultDto> Results { get; init; } = [];
    public decimal AverageScore { get; init; }
}

file sealed class ScoringSimulatorResultDto
{
    public int Index { get; init; }
    public int Score { get; init; }
    public string Priority { get; init; } = string.Empty;
}

file sealed class OpportunityDto
{
    public Guid Id { get; init; }
}

file sealed class ConversionLoopDto
{
    public List<ConversionBucketDto> Buckets { get; init; } = [];
}

file sealed class ConversionBucketDto
{
    public string Bucket { get; init; } = string.Empty;
    public int Leads { get; init; }
    public int Won { get; init; }
    public decimal ConversionRatePercent { get; init; }
}
