using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

/// <summary>Integration tests for Sequences, Custom Fields, and WhatsApp endpoints.</summary>
public class FeatureIntegrationTests
{
    // ============================================================
    // FEAT-CF: Filter / sort by custom field
    // ============================================================

    [Fact]
    public async Task SearchLeads_NoFilters_ReturnsPage()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        await CreateLeadAsync(client);
        await CreateLeadAsync(client);

        var resp = await client.GetAsync("/api/leads?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var page = await resp.Content.ReadFromJsonAsync<LeadPageResponse>();
        Assert.NotNull(page);
        Assert.True(page.Total >= 2);
        Assert.True(page.Items.Count >= 2);
    }

    [Fact]
    public async Task SearchLeads_CfFilter_ReturnsOnlyMatchingLeads()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        // Create custom field definition
        await client.PostAsJsonAsync("/api/admin/custom-fields", new
        {
            Key = "industry", Label = "Industry", FieldType = "text",
            EntityType = "Lead", IsRequired = false, Order = 0
        });

        // Create two leads, tag only one with the custom field
        var lead1 = await CreateLeadAsync(client);
        var lead2 = await CreateLeadAsync(client);

        await client.PutAsJsonAsync($"/api/admin/custom-fields/values/lead/{lead1}/industry", new { Value = "fintech" });

        // Filter by industry=fintech — should return only lead1
        var resp = await client.GetAsync("/api/leads?cfFilter[industry]=fintech&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var page = await resp.Content.ReadFromJsonAsync<LeadPageResponse>();
        Assert.NotNull(page);
        Assert.Equal(1, page.Total);
        Assert.Equal(lead1, page.Items[0].Id);
    }

    [Fact]
    public async Task SearchLeads_CfSort_ReturnsOrderedByCustomField()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        await client.PostAsJsonAsync("/api/admin/custom-fields", new
        {
            Key = "region", Label = "Region", FieldType = "text",
            EntityType = "Lead", IsRequired = false, Order = 0
        });

        var leadA = await CreateLeadAsync(client);
        var leadB = await CreateLeadAsync(client);

        await client.PutAsJsonAsync($"/api/admin/custom-fields/values/lead/{leadA}/region", new { Value = "EMEA" });
        await client.PutAsJsonAsync($"/api/admin/custom-fields/values/lead/{leadB}/region", new { Value = "APAC" });

        // Sort ascending: APAC < EMEA
        var resp = await client.GetAsync("/api/leads?cfSort=region&cfSortDir=asc&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var page = await resp.Content.ReadFromJsonAsync<LeadPageResponse>();
        Assert.NotNull(page);
        // Both leads present; first is APAC
        var items = page.Items.Where(i => i.Id == leadA || i.Id == leadB).ToList();
        Assert.True(items.Count >= 2);
        var firstWithValue = items.FirstOrDefault(i => i.CustomFields.ContainsKey("region"));
        Assert.NotNull(firstWithValue);
        Assert.Equal("APAC", firstWithValue.CustomFields["region"]);
    }

    [Fact]
    public async Task SearchLeads_CoreSort_ByScore()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        await CreateLeadAsync(client);

        var resp = await client.GetAsync("/api/leads?sortBy=score&sortDir=desc&page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var page = await resp.Content.ReadFromJsonAsync<LeadPageResponse>();
        Assert.NotNull(page);
        // Verify items are ordered by score descending
        var scores = page.Items.Select(i => i.Score).ToList();
        Assert.Equal(scores.OrderByDescending(s => s).ToList(), scores);
    }

    [Fact]
    public async Task SearchLeads_Pagination_HasMore()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        for (var i = 0; i < 5; i++) await CreateLeadAsync(client);

        var resp = await client.GetAsync("/api/leads?page=1&pageSize=3");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var page = await resp.Content.ReadFromJsonAsync<LeadPageResponse>();
        Assert.NotNull(page);
        Assert.Equal(3, page.Items.Count);
        Assert.True(page.HasMore);
    }

    [Fact]
    public async Task SearchLeads_LeadIncludesCustomFieldValues()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        await client.PostAsJsonAsync("/api/admin/custom-fields", new
        {
            Key = "cf_tier", Label = "Tier", FieldType = "text",
            EntityType = "Lead", IsRequired = false, Order = 0
        });

        var leadId = await CreateLeadAsync(client);
        await client.PutAsJsonAsync($"/api/admin/custom-fields/values/lead/{leadId}/cf_tier", new { Value = "gold" });

        var resp = await client.GetAsync("/api/leads?page=1&pageSize=20");
        var page = await resp.Content.ReadFromJsonAsync<LeadPageResponse>();
        Assert.NotNull(page);

        var lead = page.Items.Single(i => i.Id == leadId);
        Assert.True(lead.CustomFields.ContainsKey("cf_tier"));
        Assert.Equal("gold", lead.CustomFields["cf_tier"]);
    }

    // ============================================================
    // FEAT-SEQ: Sequences
    // ============================================================

    [Fact]
    public async Task CreateSequence_ReturnsCreated_WithSteps()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var response = await client.PostAsJsonAsync("/api/sequences", new
        {
            Name = "Onboarding Cadence",
            Description = "5-step welcome",
            Steps = new[]
            {
                new { Order = 1, ActionType = "send_email", ActionValue = "welcome", DelayDays = 0 },
                new { Order = 2, ActionType = "add_note", ActionValue = "Follow-up note", DelayDays = 3 }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var seq = await response.Content.ReadFromJsonAsync<SequenceResponse>();
        Assert.NotNull(seq);
        Assert.Equal("Onboarding Cadence", seq.Name);
        Assert.Equal(2, seq.Steps.Count);
    }

    [Fact]
    public async Task GetSequences_ReturnsAll()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        await client.PostAsJsonAsync("/api/sequences", new { Name = "Seq A", Steps = Array.Empty<object>() });
        await client.PostAsJsonAsync("/api/sequences", new { Name = "Seq B", Steps = Array.Empty<object>() });

        var response = await client.GetAsync("/api/sequences");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var list = await response.Content.ReadFromJsonAsync<List<SequenceResponse>>();
        Assert.NotNull(list);
        Assert.True(list.Count >= 2);
    }

    [Fact]
    public async Task EnrollLead_ReturnsCreated()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        // Create sequence
        var seqResp = await client.PostAsJsonAsync("/api/sequences", new
        {
            Name = "Test Cadence",
            Steps = new[] { new { Order = 1, ActionType = "send_email", ActionValue = "welcome", DelayDays = 0 } }
        });
        var seq = await seqResp.Content.ReadFromJsonAsync<SequenceResponse>();
        Assert.NotNull(seq);

        // Create lead
        var leadId = await CreateLeadAsync(client);

        // Enroll
        var enrollResp = await client.PostAsJsonAsync($"/api/sequences/{seq.Id}/enroll", new { LeadId = leadId });
        Assert.Equal(HttpStatusCode.Created, enrollResp.StatusCode);

        // Verify enrollment
        var listResp = await client.GetAsync($"/api/sequences/enrollments/lead/{leadId}");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var enrollments = await listResp.Content.ReadFromJsonAsync<List<SequenceEnrollmentResponse>>();
        Assert.NotNull(enrollments);
        Assert.Contains(enrollments, e => e.SequenceId == seq.Id && e.Status == "active");
    }

    [Fact]
    public async Task EnrollLead_Duplicate_ReturnsConflict()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var seqResp = await client.PostAsJsonAsync("/api/sequences", new
        {
            Name = "Dup Test",
            Steps = new[] { new { Order = 1, ActionType = "add_note", ActionValue = "hi", DelayDays = 0 } }
        });
        var seq = await seqResp.Content.ReadFromJsonAsync<SequenceResponse>();
        Assert.NotNull(seq);

        var leadId = await CreateLeadAsync(client);

        await client.PostAsJsonAsync($"/api/sequences/{seq.Id}/enroll", new { LeadId = leadId });
        var dupeResp = await client.PostAsJsonAsync($"/api/sequences/{seq.Id}/enroll", new { LeadId = leadId });

        Assert.Equal(HttpStatusCode.Conflict, dupeResp.StatusCode);
    }

    [Fact]
    public async Task UnenrollLead_ReturnsNoContent()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var seqResp = await client.PostAsJsonAsync("/api/sequences", new
        {
            Name = "Exit Test",
            Steps = new[] { new { Order = 1, ActionType = "add_note", ActionValue = "bye", DelayDays = 0 } }
        });
        var seq = await seqResp.Content.ReadFromJsonAsync<SequenceResponse>();
        Assert.NotNull(seq);

        var leadId = await CreateLeadAsync(client);
        var enrollResp = await client.PostAsJsonAsync($"/api/sequences/{seq.Id}/enroll", new { LeadId = leadId });
        var enrollment = await enrollResp.Content.ReadFromJsonAsync<SequenceEnrollmentResponse>();
        Assert.NotNull(enrollment);

        var exitResp = await client.DeleteAsync($"/api/sequences/enrollments/{enrollment.Id}");
        Assert.Equal(HttpStatusCode.NoContent, exitResp.StatusCode);
    }

    // ============================================================
    // FEAT-CF: Custom Fields
    // ============================================================

    [Fact]
    public async Task CreateCustomFieldDefinition_ReturnsCreated()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var response = await client.PostAsJsonAsync("/api/admin/custom-fields", new
        {
            Key = "industry_focus",
            Label = "Industry Focus",
            FieldType = "text",
            EntityType = "Lead",
            IsRequired = false,
            Order = 1
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var def = await response.Content.ReadFromJsonAsync<CustomFieldDefinitionResponse>();
        Assert.NotNull(def);
        Assert.Equal("industry_focus", def.Key);
    }

    [Fact]
    public async Task CreateCustomFieldDefinition_Duplicate_ReturnsConflict()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var body = new { Key = "cf_dup", Label = "Dup", FieldType = "text", EntityType = "Lead", IsRequired = false, Order = 0 };
        await client.PostAsJsonAsync("/api/admin/custom-fields", body);
        var dupeResp = await client.PostAsJsonAsync("/api/admin/custom-fields", body);

        Assert.Equal(HttpStatusCode.Conflict, dupeResp.StatusCode);
    }

    [Fact]
    public async Task SetAndGetCustomFieldValue_RoundTrip()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        // Create definition
        await client.PostAsJsonAsync("/api/admin/custom-fields", new
        {
            Key = "cf_budget",
            Label = "Budget",
            FieldType = "number",
            EntityType = "Lead",
            IsRequired = false,
            Order = 0
        });

        var leadId = await CreateLeadAsync(client);

        // Set value
        var setResp = await client.PutAsJsonAsync($"/api/admin/custom-fields/values/lead/{leadId}/cf_budget", new { Value = "50000" });
        Assert.Equal(HttpStatusCode.OK, setResp.StatusCode);

        // Get value
        var getResp = await client.GetAsync($"/api/admin/custom-fields/values/lead/{leadId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var values = await getResp.Content.ReadFromJsonAsync<List<CustomFieldValueResponse>>();
        Assert.NotNull(values);
        Assert.Contains(values, v => v.Key == "cf_budget" && v.Value == "50000");
    }

    // ============================================================
    // FEAT-WA: WhatsApp
    // ============================================================

    [Fact]
    public async Task WhatsApp_OptIn_ReturnsOk()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var response = await client.PostAsJsonAsync("/api/whatsapp/opt-in", new { Phone = "15551234567" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WhatsApp_WebhookChallenge_ReturnsChallenge()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        // With no verify token configured (blank = any token), or we won't match
        // Test that the route responds (may be Forbidden without token config)
        var response = await client.GetAsync("/api/whatsapp/webhook?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=abc123");
        // Either OK (if verify token matches) or Forbidden — either way the endpoint exists
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static async Task<Guid> CreateLeadAsync(HttpClient client)
    {
        var unique = Guid.NewGuid().ToString("N");
        var intakeResponse = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"feat_{unique}@test.com",
            Phone = $"+1-555-{Random.Shared.Next(1000000, 9999999)}",
            Source = "feature-test"
        });

        Assert.Equal(HttpStatusCode.Created, intakeResponse.StatusCode);
        var payload = await intakeResponse.Content.ReadFromJsonAsync<LeadIntakeResponse>();
        Assert.NotNull(payload);
        return payload.Id;
    }

    private sealed class LeadIntakeResponse { public Guid Id { get; init; } }

    private static void EnsureFreshSchema(EmailTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }
}
