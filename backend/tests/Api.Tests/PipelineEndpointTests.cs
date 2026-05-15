using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public class PipelineEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PipelineEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStages_ReturnsDefaultOrderedStages()
    {
        using var client = CreateTenantClient();

        var response = await client.GetAsync("/api/pipeline/stages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<PipelineStageResponse>>();

        Assert.NotNull(body);
        Assert.True(body.Count >= 4);
        Assert.Equal("new", body[0].Name);
        Assert.Equal("qualified", body[1].Name);
    }

    [Fact]
    public async Task PostOpportunity_AndMoveStage_PersistsHistory()
    {
        using var client = CreateTenantClient();

        var lead = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);
        var fromStage = stages[0];
        var toStage = stages[1];

        var createOpportunity = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = lead.Id,
            Title = "  Enterprise Deal  ",
            Value = 25000,
            StageId = fromStage.Id
        });

        Assert.Equal(HttpStatusCode.Created, createOpportunity.StatusCode);

        var opportunity = await createOpportunity.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.NotNull(opportunity);
        Assert.Equal("enterprise deal", opportunity.Title);
        Assert.Equal(fromStage.Id, opportunity.StageId);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunity.Id}/stage", new
        {
            TargetStageId = toStage.Id,
            Reason = "qualified by scoring",
            Actor = "seller-a",
            ExpectedVersionToken = opportunity.VersionToken
        });

        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var moved = await moveResponse.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.NotNull(moved);
        Assert.Equal(toStage.Id, moved.StageId);
        Assert.NotEqual(opportunity.VersionToken, moved.VersionToken);

        var historyResponse = await client.GetAsync($"/api/pipeline/opportunities/{opportunity.Id}/history");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        var history = await historyResponse.Content.ReadFromJsonAsync<List<OpportunityHistoryResponse>>();
        Assert.NotNull(history);
        Assert.Single(history);
        Assert.Equal(fromStage.Id, history[0].FromStageId);
        Assert.Equal(toStage.Id, history[0].ToStageId);
        Assert.Equal("new", history[0].FromStageName);
        Assert.Equal("qualified", history[0].ToStageName);
        Assert.Equal("seller-a", history[0].Actor);
        Assert.False(history[0].IsAutomated);
    }

    [Fact]
    public async Task GetBoard_ReturnsOpportunitiesGroupedByStage()
    {
        using var client = CreateTenantClient();

        var lead = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);

        var createOpportunity = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = lead.Id,
            Title = "kanban card",
            Value = 8000,
            StageId = stages[0].Id
        });

        Assert.Equal(HttpStatusCode.Created, createOpportunity.StatusCode);

        var boardResponse = await client.GetAsync("/api/pipeline/board");

        Assert.Equal(HttpStatusCode.OK, boardResponse.StatusCode);

        var board = await boardResponse.Content.ReadFromJsonAsync<PipelineBoardResponse>();

        Assert.NotNull(board);
        Assert.True(board.Stages.Count >= 4);
        Assert.Contains(board.Opportunities, x => x.Title == "kanban card");
        Assert.True(board.TotalCount >= 1);
    }

    [Fact]
    public async Task GetBoard_AppliesFiltersSortingRiskAndPagination()
    {
        using var client = CreateTenantClient();

        await CreateLeadScoreRuleAsync(client, "pipeline-enterprise", 85);
        var highLead = await CreateLeadAsync(client, "pipeline-enterprise");
        var lowLead = await CreateLeadAsync(client, "pipeline-low");
        var stages = await GetStagesAsync(client);

        await CreateOpportunityAsync(client, highLead.Id, stages[0].Id, "alpha deal", 9000);
        await CreateOpportunityAsync(client, lowLead.Id, stages[0].Id, "beta deal", 1500);
        await CreateOpportunityAsync(client, highLead.Id, stages[1].Id, "gamma deal", 20000);

        var response = await client.GetAsync("/api/pipeline/board?source=pipeline-enterprise&minScore=80&sortBy=value&sortDirection=desc&page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var board = await response.Content.ReadFromJsonAsync<PipelineBoardResponse>();
        Assert.NotNull(board);
        Assert.Equal(2, board.TotalCount);
        Assert.True(board.HasMore);
        Assert.Single(board.Opportunities);
        Assert.Equal("gamma deal", board.Opportunities[0].Title);
        Assert.Equal("low", board.Opportunities[0].RiskLabel);
        Assert.All(board.Stages, stage => Assert.True(stage.WipLimit > 0));
    }

    [Fact]
    public async Task MoveOpportunityStage_WhenSkippingForwardMoreThanOneStage_ReturnsBadRequest()
    {
        using var client = CreateTenantClient();

        var lead = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);

        var createOpportunity = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = lead.Id,
            Title = "skip validation",
            Value = 1000,
            StageId = stages[0].Id
        });

        var opportunity = await createOpportunity.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.NotNull(opportunity);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunity.Id}/stage", new
        {
            TargetStageId = stages[2].Id,
            Reason = "trying to skip"
        });

        Assert.Equal(HttpStatusCode.BadRequest, moveResponse.StatusCode);
    }

    [Fact]
    public async Task MoveOpportunityStage_WhenMovingBackwardWithoutReason_ReturnsBadRequest()
    {
        using var client = CreateTenantClient();

        var lead = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);

        var createOpportunity = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = lead.Id,
            Title = "backward validation",
            Value = 1500,
            StageId = stages[1].Id
        });

        var opportunity = await createOpportunity.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.NotNull(opportunity);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunity.Id}/stage", new
        {
            TargetStageId = stages[0].Id,
            Reason = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, moveResponse.StatusCode);
    }

    [Fact]
    public async Task PipelineWipLimits_BlockOpportunityCreation_WhenStageIsFull()
    {
        using var client = CreateTenantClient();

        var stages = await GetStagesAsync(client);
        var targetStage = stages[0];

        var updateResponse = await client.PutAsJsonAsync($"/api/pipeline/wip-limits/{targetStage.Id}", new
        {
            Limit = 1
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var leadA = await CreateLeadAsync(client, "pipeline-wip-a");
        var leadB = await CreateLeadAsync(client, "pipeline-wip-b");

        var firstCreate = await CreateOpportunityAsync(client, leadA.Id, targetStage.Id, "wip slot one", 1000);
        Assert.NotNull(firstCreate);

        var secondCreateResponse = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = leadB.Id,
            Title = "wip slot two",
            Value = 2000,
            StageId = targetStage.Id
        });

        Assert.Equal(HttpStatusCode.BadRequest, secondCreateResponse.StatusCode);
    }

    [Fact]
    public async Task GetStageSlaAlerts_ReturnsResponseShape()
    {
        using var client = CreateTenantClient();

        var response = await client.GetAsync("/api/pipeline/stage-sla-alerts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PipelineStageSlaAlertResponse>();
        Assert.NotNull(body);
        Assert.True(body.TotalOpportunitiesEvaluated >= 0);
        Assert.True(body.TotalBreaches >= 0);
        Assert.NotNull(body.Items);
    }

    [Fact]
    public async Task GetStageSlaAlerts_WithImmediateBreachOverride_ReturnsOpportunityAlert()
    {
        using var client = CreateTenantClient();

        var beforeResponse = await client.GetAsync("/api/pipeline/stage-sla-alerts?defaultSlaHours=0");
        var beforeBody = await beforeResponse.Content.ReadFromJsonAsync<PipelineStageSlaAlertResponse>();
        Assert.NotNull(beforeBody);
        var beforeBreaches = beforeBody!.TotalBreaches;

        var lead = await CreateLeadAsync(client);
        var stages = await GetStagesAsync(client);

        var createOpportunity = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = lead.Id,
            Title = "sla breach",
            Value = 3000,
            StageId = stages[0].Id
        });

        var opportunity = await createOpportunity.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.NotNull(opportunity);

        var response = await client.GetAsync("/api/pipeline/stage-sla-alerts?defaultSlaHours=0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PipelineStageSlaAlertResponse>();
        Assert.NotNull(body);
        Assert.True(body.TotalOpportunitiesEvaluated >= 1);
        Assert.True(body.TotalBreaches >= beforeBreaches);

        var item = body.Items.FirstOrDefault(x => x.OpportunityId == opportunity.Id);
        Assert.NotNull(item);
        Assert.True(item!.IsBreached);
        Assert.True(item.SlaMinutes == 0);
    }

    [Fact]
    public async Task ExportBoardCsv_ReturnsCurrentBoardSlice()
    {
        using var client = CreateTenantClient();

        var lead = await CreateLeadAsync(client, "pipeline-export");
        var stages = await GetStagesAsync(client);
        await CreateOpportunityAsync(client, lead.Id, stages[0].Id, "csv export card", 4400);

        var response = await client.GetAsync("/api/pipeline/board/export?source=pipeline-export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("opportunityId,title,stage,ownerUserId,leadSource,leadScore,riskLabel,value,updatedAtUtc,versionToken", csv);
        Assert.Contains("csv export card", csv);
    }

    [Fact]
    public async Task GetThroughput_ReturnsStageFlowCounts()
    {
        using var client = CreateTenantClient();

        var lead = await CreateLeadAsync(client, "pipeline-throughput");
        var stages = await GetStagesAsync(client);
        var opportunity = await CreateOpportunityAsync(client, lead.Id, stages[0].Id, "throughput card", 7200);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunity.Id}/stage", new
        {
            TargetStageId = stages[1].Id,
            Reason = "qualified for throughput",
            Actor = "seller-b",
            ExpectedVersionToken = opportunity.VersionToken
        });

        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var throughputResponse = await client.GetAsync("/api/pipeline/throughput");
        Assert.Equal(HttpStatusCode.OK, throughputResponse.StatusCode);

        var throughput = await throughputResponse.Content.ReadFromJsonAsync<PipelineThroughputResponse>();
        Assert.NotNull(throughput);

        var newStage = throughput.Items.First(x => x.StageName == "new");
        var qualifiedStage = throughput.Items.First(x => x.StageName == "qualified");

        Assert.True(newStage.ExitedCount >= 1);
        Assert.True(qualifiedStage.EnteredCount >= 1);
    }

    [Fact]
    public async Task MoveOpportunityStage_AppliesAutoMoveRules_WithAuditableHistory()
    {
        using var client = CreateTenantClient();

        var createRuleResponse = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = "auto move qualified to proposal",
            Trigger = "pipeline.stage.changed",
            IsActive = true,
            Environment = "prod",
            ApprovalStatus = "approved",
            Conditions = new[]
            {
                new { Field = "to_stage", Operator = "eq", Value = "qualified" }
            },
            Actions = new[]
            {
                new { Type = "move_stage", Value = "proposal" }
            }
        });

        Assert.Equal(HttpStatusCode.Created, createRuleResponse.StatusCode);

        var lead = await CreateLeadAsync(client, "pipeline-rule");
        var stages = await GetStagesAsync(client);
        var opportunity = await CreateOpportunityAsync(client, lead.Id, stages[0].Id, "rule card", 5000);

        var moveResponse = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunity.Id}/stage", new
        {
            TargetStageId = stages[1].Id,
            Reason = "qualified manually",
            Actor = "seller-c",
            ExpectedVersionToken = opportunity.VersionToken
        });

        Assert.Equal(HttpStatusCode.OK, moveResponse.StatusCode);

        var moved = await moveResponse.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.NotNull(moved);
        Assert.Equal(stages[2].Id, moved.StageId);

        var historyResponse = await client.GetAsync($"/api/pipeline/opportunities/{opportunity.Id}/history");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<OpportunityHistoryResponse>>();
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.Contains(history, x => x.IsAutomated && x.Actor == "rules-engine" && x.ToStageName == "proposal");
    }

    [Fact]
    public async Task MoveOpportunityStage_WithStaleVersion_ReturnsConflict()
    {
        using var client = CreateTenantClient();

        var lead = await CreateLeadAsync(client, "pipeline-concurrency");
        var stages = await GetStagesAsync(client);
        var opportunity = await CreateOpportunityAsync(client, lead.Id, stages[0].Id, "concurrency card", 6100);

        var firstMove = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunity.Id}/stage", new
        {
            TargetStageId = stages[1].Id,
            Reason = "first move",
            Actor = "seller-d",
            ExpectedVersionToken = opportunity.VersionToken
        });

        Assert.Equal(HttpStatusCode.OK, firstMove.StatusCode);

        var staleMove = await client.PatchAsJsonAsync($"/api/pipeline/opportunities/{opportunity.Id}/stage", new
        {
            TargetStageId = stages[2].Id,
            Reason = "stale move",
            Actor = "seller-e",
            ExpectedVersionToken = opportunity.VersionToken
        });

        Assert.Equal(HttpStatusCode.Conflict, staleMove.StatusCode);
    }

    private static async Task<LeadResponse> CreateLeadAsync(HttpClient client, string source = "task-mvp-03")
    {
        var unique = Guid.NewGuid().ToString("N");

        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"{unique}@example.com",
            Phone = BuildUniquePhone(),
            Source = source
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(body);
        return body;
    }

    private static async Task<List<PipelineStageResponse>> GetStagesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/pipeline/stages");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<PipelineStageResponse>>();
        Assert.NotNull(body);
        return body;
    }

    private static async Task<OpportunityResponse> CreateOpportunityAsync(HttpClient client, Guid leadId, Guid stageId, string title, decimal value)
    {
        var response = await client.PostAsJsonAsync("/api/pipeline/opportunities", new
        {
            LeadId = leadId,
            Title = title,
            Value = value,
            StageId = stageId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.NotNull(body);
        return body;
    }

    private static async Task CreateLeadScoreRuleAsync(HttpClient client, string source, int score)
    {
        var response = await client.PostAsJsonAsync("/api/rules", new
        {
            Name = $"score {source}",
            Trigger = "lead.created",
            IsActive = true,
            Environment = "prod",
            ApprovalStatus = "approved",
            Conditions = new[]
            {
                new { Field = "source", Operator = "eq", Value = source }
            },
            Actions = new[]
            {
                new { Type = "add_score", Value = score.ToString() }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static string BuildUniquePhone()
    {
        var digits = Math.Abs(Guid.NewGuid().GetHashCode()).ToString("0000000000");
        return digits[^10..];
    }

    private HttpClient CreateTenantClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", $"pipeline-test-{Guid.NewGuid():N}");
        return client;
    }

    private sealed class LeadResponse
    {
        public Guid Id { get; init; }
    }

    private sealed class PipelineStageResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int Order { get; init; }
        public int WipLimit { get; init; }
    }

    private sealed class OpportunityResponse
    {
        public Guid Id { get; init; }
        public Guid LeadId { get; init; }
        public Guid StageId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string RiskLabel { get; init; } = string.Empty;
        public string VersionToken { get; init; } = string.Empty;
        public decimal Value { get; init; }
    }

    private sealed class OpportunityHistoryResponse
    {
        public Guid Id { get; init; }
        public Guid OpportunityId { get; init; }
        public Guid FromStageId { get; init; }
        public Guid ToStageId { get; init; }
        public string FromStageName { get; init; } = string.Empty;
        public string ToStageName { get; init; } = string.Empty;
        public string Actor { get; init; } = string.Empty;
        public bool IsAutomated { get; init; }
    }

    private sealed class PipelineBoardResponse
    {
        public int TotalCount { get; init; }
        public bool HasMore { get; init; }
        public List<PipelineStageResponse> Stages { get; init; } = [];
        public List<OpportunityResponse> Opportunities { get; init; } = [];
    }

    private sealed class PipelineStageSlaAlertResponse
    {
        public int TotalOpportunitiesEvaluated { get; init; }
        public int TotalBreaches { get; init; }
        public List<PipelineStageSlaAlertItemResponse> Items { get; init; } = [];
    }

    private sealed class PipelineStageSlaAlertItemResponse
    {
        public Guid OpportunityId { get; init; }
        public bool IsBreached { get; init; }
        public int SlaMinutes { get; init; }
        public int ExceededByMinutes { get; init; }
    }

    private sealed class PipelineThroughputResponse
    {
        public List<PipelineThroughputItemResponse> Items { get; init; } = [];
    }

    private sealed class PipelineThroughputItemResponse
    {
        public string StageName { get; init; } = string.Empty;
        public int EnteredCount { get; init; }
        public int ExitedCount { get; init; }
    }
}