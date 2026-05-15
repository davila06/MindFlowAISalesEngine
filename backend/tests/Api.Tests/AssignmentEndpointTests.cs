using System.Net;
using System.Net.Http.Json;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public sealed class AssignmentTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = $"assignment_test_{Guid.NewGuid():N}.db";

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

public class AssignmentEndpointTests
{
    private static HttpClient BuildClient() => new AssignmentTestFactory().CreateClient();

    [Fact]
    public async Task CreateAssignmentUser_WithValidPayload_ReturnsCreatedAndCanList()
    {
        using var client = BuildClient();

        var create = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Ana Sales",
            Email = $"ana_{Guid.NewGuid():N}@mindflow.test"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        Assert.NotNull(created);
        Assert.Equal("Ana Sales", created.FullName);
        Assert.True(created.IsActive);

        var list = await client.GetAsync("/api/assignments/users");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var users = await list.Content.ReadFromJsonAsync<List<AssignmentUserResponse>>();
        Assert.NotNull(users);
        Assert.Contains(users, u => u.Id == created.Id);
    }

    [Fact]
    public async Task IntakeLead_WithSingleAssignmentUser_AutoAssignsLead()
    {
        using var client = BuildClient();

        var userCreate = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Single Owner",
            Email = $"owner_{Guid.NewGuid():N}@mindflow.test"
        });
        var owner = await userCreate.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        Assert.NotNull(owner);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"lead_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "assignment-test"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);
        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        var assignmentRes = await client.GetAsync($"/api/assignments/leads/{lead.Id}");
        Assert.Equal(HttpStatusCode.OK, assignmentRes.StatusCode);

        var assignment = await assignmentRes.Content.ReadFromJsonAsync<LeadAssignmentResponse>();
        Assert.NotNull(assignment);
        Assert.Equal(lead.Id, assignment.LeadId);
        Assert.Equal(owner.Id, assignment.UserId);
        Assert.Equal("round_robin", assignment.Strategy);
    }

    [Fact]
    public async Task IntakeLead_WithTwoUsers_AssignsRoundRobin()
    {
        using var client = BuildClient();

        var create1 = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Rep One",
            Email = $"rep1_{Guid.NewGuid():N}@mindflow.test"
        });
        var create2 = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Rep Two",
            Email = $"rep2_{Guid.NewGuid():N}@mindflow.test"
        });

        var rep1 = await create1.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        var rep2 = await create2.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        Assert.NotNull(rep1);
        Assert.NotNull(rep2);

        var lead1Id = await IntakeLead(client);
        var lead2Id = await IntakeLead(client);
        var lead3Id = await IntakeLead(client);

        var a1 = await GetLeadAssignment(client, lead1Id);
        var a2 = await GetLeadAssignment(client, lead2Id);
        var a3 = await GetLeadAssignment(client, lead3Id);

        Assert.Equal(rep1.Id, a1.UserId);
        Assert.Equal(rep2.Id, a2.UserId);
        Assert.Equal(rep1.Id, a3.UserId);
    }

    [Fact]
    public async Task IntakeLead_WithNoUsers_StillCreatesLead_AndNoAssignmentExists()
    {
        using var client = BuildClient();

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"lead_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "assignment-no-users"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);
        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        var assignment = await client.GetAsync($"/api/assignments/leads/{lead.Id}");
        Assert.Equal(HttpStatusCode.NotFound, assignment.StatusCode);
    }

    [Fact]
    public async Task GetAssignments_ReturnsAuditableAssignmentLog()
    {
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Audit Rep",
            Email = $"audit_{Guid.NewGuid():N}@mindflow.test"
        });

        var lead1Id = await IntakeLead(client);
        var lead2Id = await IntakeLead(client);

        var response = await client.GetAsync("/api/assignments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var assignments = await response.Content.ReadFromJsonAsync<List<LeadAssignmentResponse>>();
        Assert.NotNull(assignments);
        Assert.True(assignments.Count >= 2);
        Assert.Contains(assignments, x => x.LeadId == lead1Id);
        Assert.Contains(assignments, x => x.LeadId == lead2Id);
        Assert.All(assignments, x => Assert.Equal("round_robin", x.Strategy));
    }

    [Fact]
    public async Task IntakeLead_WithRuleBasedUser_AssignsByCountryAndScore()
    {
        using var client = BuildClient();

        var specialist = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "US Specialist",
            Email = $"us_{Guid.NewGuid():N}@mindflow.test",
            PreferredCountry = "us",
            MinScoreToAssign = 80
        });
        var fallback = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Fallback Rep",
            Email = $"fallback_{Guid.NewGuid():N}@mindflow.test"
        });

        var specialistUser = await specialist.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        Assert.NotNull(specialistUser);

        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"rule_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "referral",
            Country = "us"
        });

        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);

        var assignment = await GetLeadAssignment(client, lead.Id);
        Assert.Equal(specialistUser.Id, assignment.UserId);
        Assert.Equal("rule_based", assignment.Strategy);
    }

    [Fact]
    public async Task IntakeLead_WhenUserAtCapacity_AssignsToNextAvailableUser()
    {
        using var client = BuildClient();

        var limited = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Limited Rep",
            Email = $"limited_{Guid.NewGuid():N}@mindflow.test",
            MaxActiveLeads = 1
        });
        var backup = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Backup Rep",
            Email = $"backup_{Guid.NewGuid():N}@mindflow.test",
            MaxActiveLeads = 10
        });

        var limitedUser = await limited.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        var backupUser = await backup.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        Assert.NotNull(limitedUser);
        Assert.NotNull(backupUser);

        var lead1Id = await IntakeLead(client);
        var lead2Id = await IntakeLead(client);

        var a1 = await GetLeadAssignment(client, lead1Id);
        var a2 = await GetLeadAssignment(client, lead2Id);

        Assert.Equal(limitedUser.Id, a1.UserId);
        Assert.Equal(backupUser.Id, a2.UserId);

        var capacity = await client.GetFromJsonAsync<AssignmentCapacityLoadResponseDto>("/api/assignments/capacity-load");
        Assert.NotNull(capacity);
        Assert.Contains(capacity.Users, x => x.UserId == limitedUser.Id && x.IsAtCapacity);
    }

    [Fact]
    public async Task UpdateAvailability_WhenUserDisabled_RebalancesAssignedLeads()
    {
        using var client = BuildClient();

        var user1 = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Rep One",
            Email = $"avail1_{Guid.NewGuid():N}@mindflow.test"
        });
        var user2 = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Rep Two",
            Email = $"avail2_{Guid.NewGuid():N}@mindflow.test"
        });

        var repOne = await user1.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        var repTwo = await user2.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        Assert.NotNull(repOne);
        Assert.NotNull(repTwo);

        var leadId = await IntakeLead(client);
        var firstAssignment = await GetLeadAssignment(client, leadId);
        Assert.Equal(repOne.Id, firstAssignment.UserId);

        var availability = await client.PutAsJsonAsync($"/api/assignments/users/{repOne.Id}/availability", new { IsActive = false });
        Assert.Equal(HttpStatusCode.OK, availability.StatusCode);

        var latest = await GetLeadAssignment(client, leadId);
        Assert.Equal(repTwo.Id, latest.UserId);
        Assert.Equal("rebalance_availability", latest.Strategy);
    }

    [Fact]
    public async Task AssignmentAudit_ReturnsDecisionTrail()
    {
        using var client = BuildClient();
        await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Audit Rep",
            Email = $"audittrail_{Guid.NewGuid():N}@mindflow.test"
        });

        var leadId = await IntakeLead(client);
        var response = await client.GetAsync($"/api/assignments/audit?leadId={leadId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var entries = await response.Content.ReadFromJsonAsync<List<AssignmentAuditEntryDto>>();
        Assert.NotNull(entries);
        Assert.NotEmpty(entries);
        Assert.Contains(entries, x => x.LeadId == leadId);
    }

    [Fact]
    public async Task AssignmentFairness_ReturnsDistributionAndRiskFlag()
    {
        using var client = BuildClient();
        await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Fairness One",
            Email = $"fair1_{Guid.NewGuid():N}@mindflow.test"
        });
        await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Fairness Two",
            Email = $"fair2_{Guid.NewGuid():N}@mindflow.test"
        });

        await IntakeLead(client);
        await IntakeLead(client);

        var fairness = await client.GetFromJsonAsync<AssignmentFairnessDto>("/api/assignments/fairness");
        Assert.NotNull(fairness);
        Assert.Equal(2, fairness.TotalAssignments);
        Assert.NotEmpty(fairness.Distribution);
    }

    [Fact]
    public async Task ManualAssignment_WithProtection_PreventsAutomaticOverwrite()
    {
        using var client = BuildClient();

        var user1 = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Manual One",
            Email = $"manual1_{Guid.NewGuid():N}@mindflow.test"
        });
        var user2 = await client.PostAsJsonAsync("/api/assignments/users", new
        {
            FullName = "Manual Two",
            Email = $"manual2_{Guid.NewGuid():N}@mindflow.test"
        });
        var repOne = await user1.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        var repTwo = await user2.Content.ReadFromJsonAsync<AssignmentUserResponse>();
        Assert.NotNull(repOne);
        Assert.NotNull(repTwo);

        var leadId = await IntakeLead(client);

        var manual = await client.PostAsJsonAsync($"/api/assignments/leads/{leadId}/manual", new
        {
            UserId = repOne.Id,
            Reason = "vip account",
            ProtectFromAutoOverwrite = true
        });
        Assert.Equal(HttpStatusCode.OK, manual.StatusCode);

        var disable = await client.PutAsJsonAsync($"/api/assignments/users/{repOne.Id}/availability", new
        {
            IsActive = false
        });
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        var latest = await GetLeadAssignment(client, leadId);
        Assert.Equal(repOne.Id, latest.UserId);
        Assert.Equal("manual", latest.Strategy);

        var audit = await client.GetFromJsonAsync<List<AssignmentAuditEntryDto>>($"/api/assignments/audit?leadId={leadId}");
        Assert.NotNull(audit);
        Assert.Contains(audit, x => x.IsManualProtected);
    }

    private static async Task<Guid> IntakeLead(HttpClient client)
    {
        var intake = await client.PostAsJsonAsync("/api/leads/intake", new
        {
            Email = $"lead_{Guid.NewGuid():N}@mindflow.test",
            Phone = BuildUniquePhone(),
            Source = "assignment-rr"
        });

        Assert.Equal(HttpStatusCode.Created, intake.StatusCode);

        var lead = await intake.Content.ReadFromJsonAsync<LeadResponse>();
        Assert.NotNull(lead);
        return lead.Id;
    }

    private static async Task<(Guid UserId, string Strategy)> GetLeadAssignment(HttpClient client, Guid leadId)
    {
        var assignmentRes = await client.GetAsync($"/api/assignments/leads/{leadId}");
        Assert.Equal(HttpStatusCode.OK, assignmentRes.StatusCode);

        var assignment = await assignmentRes.Content.ReadFromJsonAsync<LeadAssignmentResponse>();
        Assert.NotNull(assignment);
        return (assignment.UserId, assignment.Strategy);
    }

    private static string BuildUniquePhone()
    {
        var n = Math.Abs(Guid.NewGuid().GetHashCode()) % 10_000_000;
        return $"+1555{n:D7}";
    }
}

file sealed class AssignmentUserResponse
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

file sealed class LeadAssignmentResponse
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public Guid UserId { get; init; }
    public string Strategy { get; init; } = string.Empty;
    public string? RuleKey { get; init; }
    public DateTime AssignedAtUtc { get; init; }
}

file sealed class LeadResponse
{
    public Guid Id { get; init; }
}

file sealed class AssignmentAuditEntryDto
{
    public Guid AssignmentId { get; init; }
    public Guid LeadId { get; init; }
    public Guid UserId { get; init; }
    public string Strategy { get; init; } = string.Empty;
    public string? RuleKey { get; init; }
    public bool IsManualProtected { get; init; }
    public DateTime AssignedAtUtc { get; init; }
}

file sealed class AssignmentFairnessDto
{
    public int TotalAssignments { get; init; }
    public decimal AverageAssignmentsPerUser { get; init; }
    public int MaxAssignmentsBySingleUser { get; init; }
    public int MinAssignmentsBySingleUser { get; init; }
    public decimal StandardDeviation { get; init; }
    public bool HasImbalanceRisk { get; init; }
    public List<AssignmentDistributionDto> Distribution { get; init; } = [];
}

file sealed class AssignmentDistributionDto
{
    public Guid UserId { get; init; }
    public int AssignedLeads { get; init; }
    public decimal SharePercent { get; init; }
}

file sealed class AssignmentCapacityLoadResponseDto
{
    public List<AssignmentCapacityLoadItemDto> Users { get; init; } = [];
}

file sealed class AssignmentCapacityLoadItemDto
{
    public Guid UserId { get; init; }
    public int CurrentLoad { get; init; }
    public int MaxActiveLeads { get; init; }
    public bool IsAtCapacity { get; init; }
}
