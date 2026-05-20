using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class LeadActivityEndpointTests
{
    [Fact]
    public async Task IntakeLead_RecordsLeadCreatedActivity()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var leadId = await CreateLeadAsync(client);

        var response = await client.GetAsync($"/api/leads/{leadId}/activities?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<LeadActivitiesPage>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.ActivityType == "lead_created");
    }

    [Fact]
    public async Task AddNote_ValidRequest_PersistsNoteActivity()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var leadId = await CreateLeadAsync(client);
        var noteText = "Follow-up call scheduled for next Tuesday.";

        var addNoteResponse = await client.PostAsJsonAsync($"/api/leads/{leadId}/activities", new { Note = noteText });
        Assert.Equal(HttpStatusCode.OK, addNoteResponse.StatusCode);

        var feedResponse = await client.GetAsync($"/api/leads/{leadId}/activities?page=1&pageSize=20&type=note_added");
        Assert.Equal(HttpStatusCode.OK, feedResponse.StatusCode);

        var page = await feedResponse.Content.ReadFromJsonAsync<LeadActivitiesPage>();
        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.ActivityType == "note_added" && item.Description == noteText);
    }

    [Fact]
    public async Task AddNote_WithTooLongText_ReturnsBadRequest()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var leadId = await CreateLeadAsync(client);
        var longNote = new string('x', 2001);

        var response = await client.PostAsJsonAsync($"/api/leads/{leadId}/activities", new { Note = longNote });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetActivities_FilterByType_ReturnsOnlyRequestedType()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var leadId = await CreateLeadAsync(client);
        await client.PostAsJsonAsync($"/api/leads/{leadId}/activities", new { Note = "Qualification note" });

        var response = await client.GetAsync($"/api/leads/{leadId}/activities?page=1&pageSize=20&type=note_added");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<LeadActivitiesPage>();
        Assert.NotNull(page);
        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, item => Assert.Equal("note_added", item.ActivityType));
    }

    [Fact]
    public async Task GetActivities_Pagination_ReturnsPagesAndHasMore()
    {
        using var factory = new EmailTestFactory();
        using var client = factory.CreateClient();
        EnsureFreshSchema(factory);

        var leadId = await CreateLeadAsync(client);
        await client.PostAsJsonAsync($"/api/leads/{leadId}/activities", new { Note = "n1" });
        await client.PostAsJsonAsync($"/api/leads/{leadId}/activities", new { Note = "n2" });
        await client.PostAsJsonAsync($"/api/leads/{leadId}/activities", new { Note = "n3" });

        var page1Response = await client.GetAsync($"/api/leads/{leadId}/activities?page=1&pageSize=2");
        var page2Response = await client.GetAsync($"/api/leads/{leadId}/activities?page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, page1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, page2Response.StatusCode);

        var page1 = await page1Response.Content.ReadFromJsonAsync<LeadActivitiesPage>();
        var page2 = await page2Response.Content.ReadFromJsonAsync<LeadActivitiesPage>();

        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.NotEmpty(page2.Items);
    }

    private static async Task<Guid> CreateLeadAsync(HttpClient client)
    {
        var unique = Guid.NewGuid().ToString("N");
        var intakeResponse = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"timeline_{unique}@test.com",
            Phone = $"+1-555-{Random.Shared.Next(1000000, 9999999)}",
            Source = "lead-activity-test"
        });

        Assert.Equal(HttpStatusCode.Created, intakeResponse.StatusCode);

        var payload = await intakeResponse.Content.ReadFromJsonAsync<LeadIntakeResponse>();
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.Id);
        return payload.Id;
    }

    private sealed class LeadIntakeResponse
    {
        public Guid Id { get; init; }
    }

    private static void EnsureFreshSchema(EmailTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }
}
