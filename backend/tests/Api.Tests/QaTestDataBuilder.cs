using System.Net.Http.Json;

namespace Api.Tests;

/// <summary>
/// QA-16: Centralised test-data builder with environment-aware defaults.
/// Provides fluent, deterministic test fixtures for all modules.
/// </summary>
public sealed class QaTestDataBuilder
{
    // ── Lead ─────────────────────────────────────────────────────────────────

    public record LeadPayload(
        string Email,
        string Phone,
        string Source,
        string? Country = null,
        string? Campaign = null,
        string? Channel = null);

    public static LeadPayload BuildLead(
        string? emailPrefix = null,
        string source = "web",
        string country = "US",
        string campaign = "qa-campaign",
        string channel = "organic") =>
        new(
            Email: $"{emailPrefix ?? "qa"}_{Guid.NewGuid():N}@mindflow.qa",
            Phone: BuildPhone(),
            Source: source,
            Country: country,
            Campaign: campaign,
            Channel: channel);

    public static string BuildPhone() =>
        $"+1{Random.Shared.Next(2_000_000_000, 2_099_999_999)}";

    // ── Assignment user ───────────────────────────────────────────────────────

    public record AssignmentUserPayload(string FullName, string Email);

    public static AssignmentUserPayload BuildAssignmentUser(string? name = null) =>
        new(
            FullName: name ?? $"QA Agent {Guid.NewGuid().ToString("N")[..6]}",
            Email: $"agent_{Guid.NewGuid().ToString("N")}@mindflow.qa");

    // ── Rule ─────────────────────────────────────────────────────────────────

    public record RulePayload(
        string Name,
        string Trigger,
        object Conditions,
        object Actions,
        int Priority = 50);

    public static RulePayload BuildAddScoreRule(int points = 10, string trigger = "lead.created") =>
        new(
            Name: $"QA Rule {Guid.NewGuid().ToString("N")[..8]}",
            Trigger: trigger,
            Conditions: new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions: new[] { new { Type = "add_score", Value = points.ToString() } },
            Priority: 50);

    public static RulePayload BuildAssignRule(string country = "US") =>
        new(
            Name: $"QA Assign {Guid.NewGuid().ToString("N")[..8]}",
            Trigger: "lead.created",
            Conditions: new[] { new { Field = "source", Operator = "eq", Value = "web" } },
            Actions: new[] { new { Type = "set_priority", Value = "hot" } },
            Priority: 60);

    // ── Alert threshold ───────────────────────────────────────────────────────

    public record AlertThresholdPayload(
        string EndpointName,
        decimal MaxErrorRatePercent,
        decimal MaxAverageLatencyMs,
        string NotificationEmail,
        bool IsActive = true);

    public static AlertThresholdPayload BuildAlertThreshold(
        string endpoint = "api/leads/intake",
        decimal maxErrorRate = 5m,
        decimal maxLatency = 2000m) =>
        new(
            EndpointName: endpoint,
            MaxErrorRatePercent: maxErrorRate,
            MaxAverageLatencyMs: maxLatency,
            NotificationEmail: "qa-ops@mindflow.qa");

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    public static HttpRequestMessage WithTenant(
        HttpRequestMessage req,
        string tenantId = "qa-tenant",
        string role = "Admin")
    {
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        req.Headers.TryAddWithoutValidation("X-User-Role", role);
        return req;
    }

    public static void AddTenantHeaders(
        HttpClient client,
        string tenantId = "qa-tenant",
        string role = "Admin")
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-User-Role", role);
    }

    // ── Async helpers ─────────────────────────────────────────────────────────

    /// <summary>Intake a lead and return the parsed response body.</summary>
    public static async Task<LeadIntakeResult> IntakeLeadAsync(
        HttpClient client,
        LeadPayload? payload = null,
        string tenantId = "qa-tenant")
    {
        payload ??= BuildLead();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/leads/intake")
        {
            Content = JsonContent.Create(new
            {
                Email = payload.Email,
                Phone = payload.Phone,
                Source = payload.Source,
                Country = payload.Country,
                Campaign = payload.Campaign,
                Channel = payload.Channel
            })
        };
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId);
        req.Headers.TryAddWithoutValidation("X-User-Role", "Admin");

        var response = await client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LeadIntakeResult>())!;
    }

    public record LeadIntakeResult(Guid Id, int Score, string Priority, string Status);

    // ── Shared response DTOs (used by multiple QA test files) ─────────────────

    public record QaPipelineStageDto(Guid Id, string Name, int Order);
    public record QaOpportunityDto(Guid Id, string Title, Guid StageId);
    public record QaRuleDto(Guid Id, string Name, bool IsActive, int Priority);
}

/// <summary>
/// QA-16: Environment-specific test configuration constants.
/// </summary>
public static class QaEnvironments
{
    public const string TenantA = "qa-tenant-a";
    public const string TenantB = "qa-tenant-b";
    public const string TenantC = "qa-tenant-c";
    public const string AdminRole = "Admin";
    public const string ViewerRole = "Viewer";
    public const string SalesRole = "Sales";

    /// <summary>Minimum coverage thresholds per layer (percentage).</summary>
    public static class CoverageThresholds
    {
        public const int DomainMinPercent = 90;
        public const int ApplicationMinPercent = 80;
        public const int InfrastructureMinPercent = 70;
        public const int OverallMinPercent = 75;
    }
}
