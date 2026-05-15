using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.Tests;

public class ContactCompanyEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ContactCompanyEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostContact_WithLeadId_ReturnsCreatedAndLinksToLead()
    {
        using var client = _factory.CreateClient();

        var leadResponse = await CreateLeadAsync(client);

        var unique = Guid.NewGuid().ToString("N");
        var uniquePhone = BuildUniquePhone();
        var payload = new
        {
            LeadId = leadResponse.Id,
            FullName = "  Jane Seller  ",
            Email = $"  {unique}@Example.com ",
            Phone = uniquePhone
        };

        var response = await client.PostAsJsonAsync("/api/contacts", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ContactResponse>();

        Assert.NotNull(body);
        Assert.Equal("jane seller", body.FullName);
        Assert.Equal($"{unique}@example.com", body.Email);
        Assert.Equal(uniquePhone, body.Phone);
        Assert.Equal(leadResponse.Id, body.LeadId);
    }

    [Fact]
    public async Task PostContact_WithDuplicatedEmail_ReturnsConflict()
    {
        using var client = _factory.CreateClient();

        var leadA = await CreateLeadAsync(client);
        var leadB = await CreateLeadAsync(client);
        var unique = Guid.NewGuid().ToString("N");

        var createFirst = await client.PostAsJsonAsync("/api/contacts", new
        {
            LeadId = leadA.Id,
            FullName = "first",
            Email = $"{unique}@example.com",
            Phone = BuildUniquePhone()
        });

        Assert.Equal(HttpStatusCode.Created, createFirst.StatusCode);

        var createSecond = await client.PostAsJsonAsync("/api/contacts", new
        {
            LeadId = leadB.Id,
            FullName = "second",
            Email = $" {unique}@EXAMPLE.com ",
            Phone = BuildUniquePhone()
        });

        Assert.Equal(HttpStatusCode.Conflict, createSecond.StatusCode);
    }

    [Fact]
    public async Task PostCompany_WithLeadId_ReturnsCreatedAndLinksToLead()
    {
        using var client = _factory.CreateClient();

        var leadResponse = await CreateLeadAsync(client);
        var unique = Guid.NewGuid().ToString("N");

        var payload = new
        {
            LeadId = leadResponse.Id,
            Name = $"  Acme {unique}  ",
            Website = " HTTPS://Acme.example.com "
        };

        var response = await client.PostAsJsonAsync("/api/companies", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CompanyResponse>();

        Assert.NotNull(body);
        Assert.Equal($"acme {unique}", body.Name);
        Assert.Equal("https://acme.example.com", body.Website);
        Assert.Equal(leadResponse.Id, body.LeadId);
    }

    [Fact]
    public async Task PostCompany_WithDuplicatedName_ReturnsConflict()
    {
        using var client = _factory.CreateClient();

        var leadA = await CreateLeadAsync(client);
        var leadB = await CreateLeadAsync(client);
        var unique = Guid.NewGuid().ToString("N");

        var createFirst = await client.PostAsJsonAsync("/api/companies", new
        {
            LeadId = leadA.Id,
            Name = $"Nova {unique}",
            Website = "https://nova-a.example.com"
        });

        Assert.Equal(HttpStatusCode.Created, createFirst.StatusCode);

        var createSecond = await client.PostAsJsonAsync("/api/companies", new
        {
            LeadId = leadB.Id,
            Name = $"  NOVA {unique} ",
            Website = "https://nova-b.example.com"
        });

        Assert.Equal(HttpStatusCode.Conflict, createSecond.StatusCode);
    }

    [Fact]
    public async Task GetContacts_WithPaginationAndSearch_ReturnsFilteredPage()
    {
        using var client = _factory.CreateClient();
        var lead = await CreateLeadAsync(client);

        await client.PostAsJsonAsync("/api/contacts", new
        {
            LeadId = lead.Id,
            FullName = "alpha one",
            Email = $"{Guid.NewGuid():N}@example.com",
            Phone = BuildUniquePhone()
        });
        await client.PostAsJsonAsync("/api/contacts", new
        {
            LeadId = lead.Id,
            FullName = "beta two",
            Email = $"{Guid.NewGuid():N}@example.com",
            Phone = BuildUniquePhone()
        });

        var response = await client.GetAsync($"/api/contacts?leadId={lead.Id}&search=beta&page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResponse<ContactResponse>>();
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(1, body.PageSize);
        Assert.True(body.TotalCount >= 1);
        Assert.Single(body.Items);
        Assert.Contains("beta", body.Items[0].FullName ?? string.Empty);
    }

    [Fact]
    public async Task ContactUpdateAndDelete_GenerateLeadAuditSnapshots()
    {
        using var client = _factory.CreateClient();
        var lead = await CreateLeadAsync(client);

        var create = await client.PostAsJsonAsync("/api/contacts", new
        {
            LeadId = lead.Id,
            FullName = "audit contact",
            Email = $"{Guid.NewGuid():N}@example.com",
            Phone = BuildUniquePhone()
        });
        var created = await create.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(created);

        var update = await client.PutAsJsonAsync($"/api/contacts/{created.Id}", new
        {
            FullName = "audit contact updated",
            Email = created.Email,
            Phone = created.Phone
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await client.DeleteAsync($"/api/contacts/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var audits = await client.GetFromJsonAsync<List<LeadAuditResponse>>($"/api/leads/{lead.Id}/audits");
        Assert.NotNull(audits);
        Assert.Contains(audits!, x => x.EventType == "contact.created");
        Assert.Contains(audits!, x => x.EventType == "contact.updated");
        Assert.Contains(audits!, x => x.EventType == "contact.deleted");
    }

    [Fact]
    public async Task BulkIntake_WithPartialValidation_ReturnsAcceptedAndRejected()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/leads/intake/bulk", new
        {
            Items = new object[]
            {
                new { Email = $"{Guid.NewGuid():N}@example.com", Source = "bulk", Country = "us" },
                new { Email = "invalid-email", Source = "bulk", Country = "us" }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<BulkIntakeResponse>();
        Assert.NotNull(body);
        Assert.Single(body.Accepted);
        Assert.Single(body.Rejected);
        Assert.False(string.IsNullOrWhiteSpace(body.Rejected[0].FailedRequestId));
    }

    [Fact]
    public async Task ReprocessFailedIntake_UsesStoredPayload()
    {
        using var client = _factory.CreateClient();

        var bulk = await client.PostAsJsonAsync("/api/leads/intake/bulk", new
        {
            Items = new object[]
            {
                new { Email = "bad-email", Source = "bulk", Country = "us" }
            }
        });
        var bulkBody = await bulk.Content.ReadFromJsonAsync<BulkIntakeResponse>();
        Assert.NotNull(bulkBody);
        Assert.Single(bulkBody!.Rejected);

        var failedId = bulkBody.Rejected[0].FailedRequestId;
        var failedItems = await client.GetFromJsonAsync<List<FailedIntakeItem>>("/api/leads/intake/failed");
        Assert.NotNull(failedItems);
        Assert.Contains(failedItems!, x => x.FailedRequestId == failedId);

        var reprocess = await client.PostAsJsonAsync($"/api/leads/intake/failed/{failedId}/reprocess", new { });
        Assert.Equal(HttpStatusCode.BadRequest, reprocess.StatusCode);
    }

    [Fact]
    public async Task MergeLead_DeletesDuplicateAndWritesAudit()
    {
        using var client = _factory.CreateClient();

        var primary = await CreateLeadAsync(client);
        var duplicate = await CreateLeadAsync(client);

        var merge = await client.PostAsJsonAsync("/api/leads/merge", new
        {
            PrimaryLeadId = primary.Id,
            DuplicateLeadId = duplicate.Id,
            Reason = "deduplicate"
        });

        Assert.Equal(HttpStatusCode.OK, merge.StatusCode);

        var primaryAudits = await client.GetFromJsonAsync<List<LeadAuditResponse>>($"/api/leads/{primary.Id}/audits");
        Assert.NotNull(primaryAudits);
        Assert.Contains(primaryAudits!, x => x.EventType == "lead.merged.primary");
    }

    private static async Task<LeadResponse> CreateLeadAsync(HttpClient client)
    {
        var unique = Guid.NewGuid().ToString("N");

        var response = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"{unique}@example.com",
            Phone = "5551230000",
            Source = "task-mvp-02"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(body);
        return body;
    }

    private static string BuildUniquePhone()
    {
        var digits = Math.Abs(Guid.NewGuid().GetHashCode()).ToString("0000000000");
        return digits[^10..];
    }

    private sealed class LeadResponse
    {
        public Guid Id { get; init; }
    }

    private sealed class ContactResponse
    {
        public Guid Id { get; init; }
        public Guid LeadId { get; init; }
        public string? FullName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
    }

    private sealed class CompanyResponse
    {
        public Guid Id { get; init; }
        public Guid LeadId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Website { get; init; }
    }

    private sealed class PagedResponse<T>
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public List<T> Items { get; init; } = new();
    }

    private sealed class LeadAuditResponse
    {
        public string EventType { get; init; } = string.Empty;
    }

    private sealed class BulkIntakeResponse
    {
        public List<BulkAcceptedItem> Accepted { get; init; } = new();
        public List<BulkRejectedItem> Rejected { get; init; } = new();
    }

    private sealed class BulkAcceptedItem
    {
        public int Index { get; init; }
    }

    private sealed class BulkRejectedItem
    {
        public int Index { get; init; }
        public string FailedRequestId { get; init; } = string.Empty;
    }

    private sealed class FailedIntakeItem
    {
        public string FailedRequestId { get; init; } = string.Empty;
    }
}