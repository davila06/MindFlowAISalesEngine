using System.Net;
using System.Net.Http.Json;
using Api.Domain.Observability;
using Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class AnalyticsAdvancedAlertsEndpointTests
{
    private static HttpClient BuildClient() => new DashboardTestFactory().CreateClient();

    [Fact]
    public async Task AlertThresholds_CrudFlow_Works()
    {
        using var client = BuildClient();

        var createRequest = new AlertThresholdCreateDto
        {
            EndpointName = "funnel",
            MaxErrorRatePercent = 10,
            MaxAverageLatencyMs = 500,
            NotificationEmail = "ops@novamind.local",
            IsActive = true
        };

        var createResponse = await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<AlertThresholdDto>();
        Assert.NotNull(created);
        Assert.Equal("funnel", created.EndpointName);

        var listResponse = await client.GetAsync("/api/analytics/advanced/alert-thresholds");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<AlertThresholdListDto>();
        Assert.NotNull(list);
        Assert.NotEmpty(list.Items);

        var updateRequest = new AlertThresholdUpdateDto
        {
            EndpointName = "funnel",
            MaxErrorRatePercent = 5,
            MaxAverageLatencyMs = 400,
            NotificationEmail = "alerts@novamind.local",
            IsActive = true
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/analytics/advanced/alert-thresholds/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/analytics/advanced/alert-thresholds/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task AlertEvents_AfterThresholdExceeded_CreatesEvent()
    {
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", new AlertThresholdCreateDto
        {
            EndpointName = "funnel",
            MaxErrorRatePercent = 10,
            MaxAverageLatencyMs = 500,
            NotificationEmail = "ops@novamind.local",
            IsActive = true
        });

        // Force high error rate for funnel endpoint.
        await client.GetAsync("/api/analytics/advanced/funnel?groupBy=quarter");
        await client.GetAsync("/api/analytics/advanced/funnel?groupBy=quarter");

        var flushResponse = await client.PostAsync("/api/analytics/advanced/metrics/history/snapshot", null);
        Assert.Equal(HttpStatusCode.OK, flushResponse.StatusCode);

        var eventsResponse = await client.GetAsync("/api/analytics/advanced/alert-events?endpointName=funnel");
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);

        var payload = await eventsResponse.Content.ReadFromJsonAsync<AlertEventListDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);
        Assert.Contains(payload.Items, x => x.EndpointName == "funnel" && x.MetricName == "ErrorRatePercent");
    }

    [Fact]
    public async Task AlertEvents_WhenThresholdNotExceeded_DoesNotCreateEvent()
    {
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", new AlertThresholdCreateDto
        {
            EndpointName = "overview",
            MaxErrorRatePercent = 100,
            MaxAverageLatencyMs = 100000,
            NotificationEmail = "ops@novamind.local",
            IsActive = true
        });

        await client.GetAsync("/api/analytics/advanced/overview?groupBy=day");
        await client.PostAsync("/api/analytics/advanced/metrics/history/snapshot", null);

        var eventsResponse = await client.GetAsync("/api/analytics/advanced/alert-events?endpointName=overview");
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);

        var payload = await eventsResponse.Content.ReadFromJsonAsync<AlertEventListDto>();
        Assert.NotNull(payload);
        Assert.Empty(payload.Items);
    }

    [Fact]
    public async Task AlertEvents_WhenTriggered_CreatesNotificationEmailLog()
    {
        using var client = BuildClient();

        await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", new AlertThresholdCreateDto
        {
            EndpointName = "funnel",
            MaxErrorRatePercent = 1,
            MaxAverageLatencyMs = 500,
            NotificationEmail = "ops@novamind.local",
            IsActive = true
        });

        await client.GetAsync("/api/analytics/advanced/funnel?groupBy=quarter");
        await client.PostAsync("/api/analytics/advanced/metrics/history/snapshot", null);

        var logsResponse = await client.GetAsync("/api/email/logs");
        Assert.Equal(HttpStatusCode.OK, logsResponse.StatusCode);

        var logs = await logsResponse.Content.ReadFromJsonAsync<List<EmailLogDto>>();
        Assert.NotNull(logs);
        Assert.Contains(logs, x => x.TemplateName == "alert.analytics.degradation");
    }

    [Fact]
    public async Task PoisonQueueTrend_ReturnsGroupedSeriesByBucketAndEndpoint()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        await SeedPoisonQueueEventsAsync(factory);

        var response = await client.GetAsync("/api/analytics/advanced/alert-events/poison-queue-trend?bucket=hour");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PoisonQueueTrendDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);

        var proposalSeries = payload.Items.FirstOrDefault(x => x.EndpointName == "poison-queue/proposal-reminder");
        Assert.NotNull(proposalSeries);
        Assert.Equal("proposal-reminder", proposalSeries.JobType);
        Assert.Equal(2, proposalSeries.EventCount);
        Assert.Equal(4m, proposalSeries.MaxObservedDepth);
    }

    [Fact]
    public async Task PoisonQueueTrend_WithJobTypeFilter_ReturnsOnlyRequestedJobType()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        await SeedPoisonQueueEventsAsync(factory);

        var response = await client.GetAsync("/api/analytics/advanced/alert-events/poison-queue-trend?bucket=hour&jobType=follow-up");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PoisonQueueTrendDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);
        Assert.All(payload.Items, x => Assert.Equal("follow-up", x.JobType));
    }

    [Fact]
    public async Task PoisonQueuePriority_ReturnsRankedItemsWithSeverityAndVariation()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        await SeedPoisonQueuePriorityEventsAsync(factory);

        var response = await client.GetAsync("/api/analytics/advanced/alert-events/poison-queue-priority?bucket=hour&top=2&startUtc=2026-05-01T08:00:00Z&endUtc=2026-05-01T12:00:00Z");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PoisonQueuePriorityDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);
        Assert.True(payload.Items.Count <= 2);

        var first = payload.Items[0];
        Assert.Equal("proposal-reminder", first.JobType);
        Assert.Equal("critical", first.Severity);
        Assert.True(first.DeltaDepth > 0);
        Assert.True(first.DeltaPercent > 0);
        Assert.False(string.IsNullOrWhiteSpace(first.RecommendedAction));
        Assert.False(string.IsNullOrWhiteSpace(first.RunbookHint));
        Assert.Equal("/api/proposals/reminders/poison-queue", first.RemediationPath);
    }

    [Fact]
    public async Task PoisonQueuePriority_WithJobTypeFilter_ReturnsOnlyRequestedJobType()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        await SeedPoisonQueuePriorityEventsAsync(factory);

        var response = await client.GetAsync("/api/analytics/advanced/alert-events/poison-queue-priority?bucket=hour&jobType=onboarding-welcome&startUtc=2026-05-01T08:00:00Z&endUtc=2026-05-01T12:00:00Z");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PoisonQueuePriorityDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);
        Assert.All(payload.Items, x => Assert.Equal("onboarding-welcome", x.JobType));
        Assert.All(payload.Items, x => Assert.Equal("/api/onboarding/welcome-jobs/poison-queue", x.RemediationPath));
    }

    [Fact]
    public async Task PoisonQueueRemediationTelemetry_RecordAndEffectivenessSummary_Works()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        var recordResponse = await client.PostAsJsonAsync("/api/analytics/advanced/alert-events/poison-queue-remediation-runs", new
        {
            EndpointName = "poison-queue/proposal-reminder",
            JobType = "proposal-reminder",
            Severity = "high",
            RecommendedAction = "Execute remediation workflow",
            RemediationPath = "/api/proposals/reminders/poison-queue",
            Outcome = "resolved",
            ExecutedBy = "qa-operator",
            DetectedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            Notes = "Executed from integration test"
        });

        Assert.Equal(HttpStatusCode.Created, recordResponse.StatusCode);

        var effectivenessResponse = await client.GetAsync("/api/analytics/advanced/alert-events/poison-queue-remediation-effectiveness?windowHours=24");
        Assert.Equal(HttpStatusCode.OK, effectivenessResponse.StatusCode);

        var effectiveness = await effectivenessResponse.Content.ReadFromJsonAsync<PoisonQueueRemediationEffectivenessDto>();
        Assert.NotNull(effectiveness);
        Assert.True(effectiveness.TotalRuns >= 1);
        Assert.True(effectiveness.ResolvedRuns >= 1);
        Assert.True(effectiveness.SuccessRatePercent > 0);
    }

    [Fact]
    public async Task PoisonQueueRemediationTelemetry_UpdateOutcome_ChangesEffectivenessSummary()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/analytics/advanced/alert-events/poison-queue-remediation-runs", new
        {
            EndpointName = "poison-queue/follow-up",
            JobType = "follow-up",
            Severity = "medium",
            RecommendedAction = "Execute follow-up remediation workflow",
            RemediationPath = "/api/followup/poison-queue",
            Outcome = "opened",
            ExecutedBy = "qa-operator",
            DetectedAtUtc = DateTime.UtcNow.AddMinutes(-20),
            Notes = "Opened from test"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<PoisonQueueRemediationRunDto>();
        Assert.NotNull(created);

        var inProgressResponse = await client.PutAsJsonAsync(
            $"/api/analytics/advanced/alert-events/poison-queue-remediation-runs/{created.Id}",
            new
            {
                Outcome = "in_progress",
                ExecutedBy = "qa-operator",
                Notes = "Work started"
            });

        Assert.Equal(HttpStatusCode.OK, inProgressResponse.StatusCode);
        var inProgress = await inProgressResponse.Content.ReadFromJsonAsync<PoisonQueueRemediationRunDto>();
        Assert.NotNull(inProgress);
        Assert.Equal("in_progress", inProgress.Outcome);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/analytics/advanced/alert-events/poison-queue-remediation-runs/{created.Id}",
            new
            {
                Outcome = "resolved",
                ExecutedBy = "qa-operator",
                Notes = "Resolved from test"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<PoisonQueueRemediationRunDto>();
        Assert.NotNull(updated);
        Assert.Equal("resolved", updated.Outcome);

        var effectivenessResponse = await client.GetAsync("/api/analytics/advanced/alert-events/poison-queue-remediation-effectiveness?windowHours=24&jobType=follow-up");
        Assert.Equal(HttpStatusCode.OK, effectivenessResponse.StatusCode);

        var effectiveness = await effectivenessResponse.Content.ReadFromJsonAsync<PoisonQueueRemediationEffectivenessDto>();
        Assert.NotNull(effectiveness);
        Assert.True(effectiveness.TotalRuns >= 1);
        Assert.True(effectiveness.ResolvedRuns >= 1);
    }

    [Fact]
    public async Task PoisonQueueRemediationImpact_ReturnsPositiveDepthReductionAfterResolvedRun()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        await SeedPoisonQueueRemediationImpactScenarioAsync(factory);

        var response = await client.GetAsync("/api/analytics/advanced/alert-events/poison-queue-remediation-impact?windowHours=24&observationMinutes=180&jobType=proposal-reminder");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PoisonQueueRemediationImpactDto>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Items);

        var first = payload.Items[0];
        Assert.Equal("proposal-reminder", first.JobType);
        Assert.Equal("resolved", first.Outcome);
        Assert.True(first.IsPositiveImpact);
        Assert.True(first.DepthDelta < 0);
        Assert.True(payload.PositiveImpactRatePercent > 0);
    }

    [Fact]
    public async Task PoisonQueueRemediationImpactBySegment_ReturnsBreakdownByJobTypeAndSeverity()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        await SeedPoisonQueueRemediationImpactScenarioAsync(factory);

        var response = await client.GetAsync("/api/analytics/advanced/alert-events/poison-queue-remediation-impact/by-segment?observationMinutes=180");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PoisonQueueRemediationSegmentDto>();
        Assert.NotNull(payload);

        var jobTypeSegment = payload.ByJobType.FirstOrDefault(x => x.SegmentKey == "proposal-reminder");
        Assert.NotNull(jobTypeSegment);
        Assert.True(jobTypeSegment.TotalRuns >= 1);
        Assert.True(jobTypeSegment.PositiveImpactRuns >= 1);
        Assert.True(jobTypeSegment.PositiveImpactRatePercent > 0);

        var severitySegment = payload.BySeverity.FirstOrDefault(x => x.SegmentKey == "high");
        Assert.NotNull(severitySegment);
        Assert.True(severitySegment.TotalRuns >= 1);
        Assert.True(severitySegment.PositiveImpactRuns >= 1);
    }

    [Fact]
    public async Task AlertEventLifecycle_AcknowledgeSnoozeResolve_UpdatesStatus()
    {
        using var factory = new DashboardTestFactory();
        using var client = factory.CreateClient();

        // Seed one alert event
        Guid eventId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();
            var threshold = new AlertThreshold("funnel/leads", 10m, 500m, "ops@novamind.local", true);
            db.AlertThresholds.Add(threshold);
            var ev = new AlertEvent(threshold.Id, "funnel/leads", "ErrorRate", 25m, 10m,
                new DateTime(2026, 5, 2, 12, 0, 0, DateTimeKind.Utc));
            db.AlertEvents.Add(ev);
            await db.SaveChangesAsync();
            eventId = ev.Id;
        }

        // Acknowledge
        var ackResponse = await client.PutAsJsonAsync(
            $"/api/analytics/advanced/alert-events/{eventId}/status",
            new { action = "acknowledge", actor = "ops-user", notes = "Checking it" });
        Assert.Equal(HttpStatusCode.OK, ackResponse.StatusCode);
        var acked = await ackResponse.Content.ReadFromJsonAsync<AlertEventStatusDto>();
        Assert.NotNull(acked);
        Assert.Equal("acknowledged", acked.Status);
        Assert.Equal("ops-user", acked.AcknowledgedBy);

        // Snooze for 1 hour
        var snoozeUntil = new DateTime(2026, 5, 2, 14, 0, 0, DateTimeKind.Utc);
        var snoozeResponse = await client.PutAsJsonAsync(
            $"/api/analytics/advanced/alert-events/{eventId}/status",
            new { action = "snooze", actor = "ops-user", snoozeUntilUtc = snoozeUntil, notes = "Snoozed" });
        Assert.Equal(HttpStatusCode.OK, snoozeResponse.StatusCode);
        var snoozed = await snoozeResponse.Content.ReadFromJsonAsync<AlertEventStatusDto>();
        Assert.NotNull(snoozed);
        Assert.Equal("snoozed", snoozed.Status);
        Assert.Equal(snoozeUntil, snoozed.SnoozedUntilUtc);

        // Resolve
        var resolveResponse = await client.PutAsJsonAsync(
            $"/api/analytics/advanced/alert-events/{eventId}/status",
            new { action = "resolve", actor = "ops-user", notes = "Root cause fixed" });
        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        var resolved = await resolveResponse.Content.ReadFromJsonAsync<AlertEventStatusDto>();
        Assert.NotNull(resolved);
        Assert.Equal("resolved", resolved.Status);
        Assert.Equal("ops-user", resolved.ResolvedBy);
    }


    [Fact]
    public async Task AlertThreshold_WithWebhookUrl_IsPersistedAndReturned()
    {
        using var client = BuildClient();

        var createRequest = new
        {
            endpointName = "funnel/webhook-test",
            maxErrorRatePercent = 5m,
            maxAverageLatencyMs = 300m,
            notificationEmail = "ops@novamind.local",
            isActive = true,
            webhookUrl = "https://hooks.example.com/alerts"
        };

        var createResponse = await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<AlertThresholdWebhookDto>();
        Assert.NotNull(created);
        Assert.Equal("https://hooks.example.com/alerts", created.WebhookUrl);

        var getResponse = await client.GetAsync($"/api/analytics/advanced/alert-thresholds/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<AlertThresholdWebhookDto>();
        Assert.NotNull(fetched);
        Assert.Equal("https://hooks.example.com/alerts", fetched.WebhookUrl);
    }

    private static async Task SeedPoisonQueueEventsAsync(DashboardTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var proposalThreshold = new AlertThreshold(
            "poison-queue/proposal-reminder",
            100m,
            1m,
            "ops@novamind.local",
            isActive: true);

        var followUpThreshold = new AlertThreshold(
            "poison-queue/follow-up",
            100m,
            1m,
            "ops@novamind.local",
            isActive: true);

        var baseHour = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);

        dbContext.AlertThresholds.AddRange(proposalThreshold, followUpThreshold);
        dbContext.AlertEvents.AddRange(
            new AlertEvent(
                proposalThreshold.Id,
                "poison-queue/proposal-reminder",
                "PoisonQueueDepth",
                observedValue: 3m,
                thresholdValue: 1m,
                triggeredAtUtc: baseHour.AddMinutes(5)),
            new AlertEvent(
                proposalThreshold.Id,
                "poison-queue/proposal-reminder",
                "PoisonQueueDepth",
                observedValue: 4m,
                thresholdValue: 1m,
                triggeredAtUtc: baseHour.AddMinutes(20)),
            new AlertEvent(
                followUpThreshold.Id,
                "poison-queue/follow-up",
                "PoisonQueueDepth",
                observedValue: 2m,
                thresholdValue: 1m,
                triggeredAtUtc: baseHour.AddHours(1).AddMinutes(10)));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPoisonQueuePriorityEventsAsync(DashboardTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var proposalThreshold = new AlertThreshold("poison-queue/proposal-reminder", 100m, 1m, "ops@novamind.local", true);
        var onboardingThreshold = new AlertThreshold("poison-queue/onboarding-welcome", 100m, 1m, "ops@novamind.local", true);

        var firstBucket = new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        var secondBucket = firstBucket.AddHours(1);

        dbContext.AlertThresholds.AddRange(proposalThreshold, onboardingThreshold);
        dbContext.AlertEvents.AddRange(
            new AlertEvent(proposalThreshold.Id, "poison-queue/proposal-reminder", "PoisonQueueDepth", 3m, 1m, firstBucket.AddMinutes(5)),
            new AlertEvent(proposalThreshold.Id, "poison-queue/proposal-reminder", "PoisonQueueDepth", 8m, 1m, secondBucket.AddMinutes(4)),
            new AlertEvent(onboardingThreshold.Id, "poison-queue/onboarding-welcome", "PoisonQueueDepth", 2m, 1m, firstBucket.AddMinutes(10)),
            new AlertEvent(onboardingThreshold.Id, "poison-queue/onboarding-welcome", "PoisonQueueDepth", 3m, 1m, secondBucket.AddMinutes(12)));

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPoisonQueueRemediationImpactScenarioAsync(DashboardTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var threshold = new AlertThreshold("poison-queue/proposal-reminder", 100m, 1m, "ops@novamind.local", true);
        dbContext.AlertThresholds.Add(threshold);

        // Keep seeded events inside default analytics windows to avoid date-dependent flakes.
        var baseTime = DateTime.UtcNow.AddMinutes(-90);

        dbContext.AlertEvents.AddRange(
            new AlertEvent(threshold.Id, "poison-queue/proposal-reminder", "PoisonQueueDepth", 8m, 1m, baseTime.AddMinutes(-40)),
            new AlertEvent(threshold.Id, "poison-queue/proposal-reminder", "PoisonQueueDepth", 6m, 1m, baseTime.AddMinutes(30)),
            new AlertEvent(threshold.Id, "poison-queue/proposal-reminder", "PoisonQueueDepth", 4m, 1m, baseTime.AddMinutes(70)));

        dbContext.PoisonQueueRemediationRuns.Add(new PoisonQueueRemediationRun(
            endpointName: "poison-queue/proposal-reminder",
            jobType: "proposal-reminder",
            severity: "high",
            recommendedAction: "Execute remediation workflow",
            remediationPath: "/api/proposals/reminders/poison-queue",
            outcome: "resolved",
            executedBy: "qa-operator",
            executedAtUtc: baseTime,
            detectedAtUtc: baseTime.AddMinutes(-50),
            notes: "Impact scenario seed"));

        await dbContext.SaveChangesAsync();
    }
    [Fact]
    public async Task AlertEvents_SloStatus_ReturnsComplianceForThresholds()
    {
        using var client = BuildClient();

        // Create a threshold
        var createRequest = new
        {
            endpointName = "funnel/slo-test",
            maxErrorRatePercent = 5m,
            maxAverageLatencyMs = 300m,
            notificationEmail = "ops@novamind.local",
            isActive = true
        };
        var createResp = await client.PostAsJsonAsync("/api/analytics/advanced/alert-thresholds", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

        // Get SLO status
        var sloResp = await client.GetAsync("/api/analytics/advanced/alert-events/slo-status");
        Assert.Equal(HttpStatusCode.OK, sloResp.StatusCode);
        var items = await sloResp.Content.ReadFromJsonAsync<List<SloStatusItemDto>>();
        Assert.NotNull(items);
        var item = items.FirstOrDefault(x => x.EndpointName == "funnel/slo-test");
        Assert.NotNull(item);
        Assert.Equal("compliant", item.Compliance);
    }
    [Fact]
    public async Task PoisonQueueSegments_LowEffectiveness_ReturnsSeverityElevationFlag()
    {
        using var factory = new DashboardTestFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var baseTime = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 5; i++)
        {
            dbContext.PoisonQueueRemediationRuns.Add(new PoisonQueueRemediationRun(
                endpointName: "poison-queue/high-fail",
                jobType: "proposal-reminder",
                severity: "high",
                recommendedAction: "Retry",
                remediationPath: "/api/proposals",
                outcome: "failed",
                executedBy: "system",
                executedAtUtc: baseTime.AddHours(i),
                detectedAtUtc: null,
                notes: ""));
        }
        await dbContext.SaveChangesAsync();

        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/analytics/advanced/alert-events/severity-elevation-candidates");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var candidates = await resp.Content.ReadFromJsonAsync<List<SeverityElevationCandidateDto>>();
        Assert.NotNull(candidates);
        Assert.Contains(candidates, c => c.EndpointName == "poison-queue/high-fail");
    }

    [Fact]
    public async Task AlertRunbook_ByMetricName_ReturnsRunbookSteps()
    {
        using var client = BuildClient();
        var resp = await client.GetAsync("/api/analytics/advanced/alert-events/runbooks/ErrorRatePercent");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var runbook = await resp.Content.ReadFromJsonAsync<AlertRunbookDto>();
        Assert.NotNull(runbook);
        Assert.Equal("ErrorRatePercent", runbook.MetricName);
        Assert.NotEmpty(runbook.Steps);
    }

    [Fact]
    public async Task AlertRunbook_UnknownMetric_Returns404()
    {
        using var client = BuildClient();
        var resp = await client.GetAsync("/api/analytics/advanced/alert-events/runbooks/UnknownMetricXyz");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task AlertEventHeatmap_ReturnsHeatmapByHourAndEndpoint()
    {
        using var factory = new DashboardTestFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var threshold = new AlertThreshold("api/heatmap-test", 1m, 500m, "ops@novamind.local", true);
        dbContext.AlertThresholds.Add(threshold);
        var baseHour = new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc);
        for (int i = 0; i < 4; i++)
        {
            dbContext.AlertEvents.Add(new AlertEvent(
                threshold.Id, "api/heatmap-test", "ErrorRatePercent",
                observedValue: 5m, thresholdValue: 1m,
                triggeredAtUtc: baseHour.AddMinutes(i * 10)));
        }
        await dbContext.SaveChangesAsync();

        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/analytics/advanced/alert-events/heatmap?endpointName=api/heatmap-test");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await resp.Content.ReadFromJsonAsync<List<AlertHeatmapPointDto>>();
        Assert.NotNull(items);
        var pt = items.FirstOrDefault(x => x.HourOfDay == 10);
        Assert.NotNull(pt);
        Assert.Equal(4, pt.EventCount);
    }
    [Fact]
    public async Task AlertTenantSummary_ReturnsSummaryForCurrentTenant()
    {
        using var factory = new DashboardTestFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var threshold = new AlertThreshold("api/tenant-summary-ep", 5m, 300m, "ops@novamind.local", true);
        dbContext.AlertThresholds.Add(threshold);
        dbContext.AlertEvents.Add(new AlertEvent(
            threshold.Id, "api/tenant-summary-ep", "ErrorRatePercent",
            observedValue: 8m, thresholdValue: 5m,
            triggeredAtUtc: DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        using var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/analytics/advanced/alert-events/tenant-summary");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var summary = await resp.Content.ReadFromJsonAsync<AlertTenantSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.ActiveThresholdsCount >= 1);
        Assert.True(summary.OpenEventsCount >= 1);
        Assert.NotEmpty(summary.TenantId);
    }
    [Fact]
    public async Task AlertEvents_Purge_DeletesOldEventsAndReturnsCount()
    {
        using var factory = new DashboardTestFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var threshold = new AlertThreshold("api/purge-test", 3m, 200m, "ops@novamind.local", true);
        dbContext.AlertThresholds.Add(threshold);
        // Old event: 100 days ago
        dbContext.AlertEvents.Add(new AlertEvent(
            threshold.Id, "api/purge-test", "ErrorRatePercent",
            observedValue: 5m, thresholdValue: 3m,
            triggeredAtUtc: DateTime.UtcNow.AddDays(-100)));
        // Recent event: today
        dbContext.AlertEvents.Add(new AlertEvent(
            threshold.Id, "api/purge-test", "ErrorRatePercent",
            observedValue: 5m, thresholdValue: 3m,
            triggeredAtUtc: DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        using var client = factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/analytics/advanced/alert-events/purge",
            new AlertEventsPurgeRequestDto { RetentionDays = 30 });
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var result = await resp.Content.ReadFromJsonAsync<AlertEventsPurgeResultDto>();
        Assert.NotNull(result);
        Assert.True(result.PurgedCount >= 1);
    }
    [Fact]
    public async Task AlertTrends_Percentiles_ReturnsP50P90P99()
    {
        using var factory = new DashboardTestFactory();
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        var threshold = new AlertThreshold("api/percentile-ep", 5m, 300m, "ops@novamind.local", true);
        dbContext.AlertThresholds.Add(threshold);
        // Seed 10 events with varied observed values
        var values = new[] { 1m, 2m, 3m, 4m, 5m, 6m, 7m, 8m, 9m, 10m };
        foreach (var v in values)
        {
            dbContext.AlertEvents.Add(new AlertEvent(
                threshold.Id, "api/percentile-ep", "AverageLatencyMs",
                observedValue: v, thresholdValue: 300m,
                triggeredAtUtc: DateTime.UtcNow.AddMinutes(-(double)v)));
        }
        await dbContext.SaveChangesAsync();

        using var client = factory.CreateClient();
        var resp = await client.GetAsync(
            "/api/analytics/advanced/alert-events/trends?endpointName=api/percentile-ep&metricName=AverageLatencyMs");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var trends = await resp.Content.ReadFromJsonAsync<AlertTrendsResponseDto>();
        Assert.NotNull(trends);
        Assert.True(trends.P50 > 0);
        Assert.True(trends.P90 >= trends.P50);
        Assert.True(trends.P99 >= trends.P90);
        Assert.Equal(10, trends.SampleCount);
    }





}

file sealed class AlertThresholdCreateDto
{
    public string EndpointName { get; init; } = string.Empty;
    public decimal MaxErrorRatePercent { get; init; }
    public decimal MaxAverageLatencyMs { get; init; }
    public string NotificationEmail { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

file sealed class AlertThresholdUpdateDto
{
    public string EndpointName { get; init; } = string.Empty;
    public decimal MaxErrorRatePercent { get; init; }
    public decimal MaxAverageLatencyMs { get; init; }
    public string NotificationEmail { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

file sealed class AlertThresholdDto
{
    public string Id { get; init; } = string.Empty;
    public string EndpointName { get; init; } = string.Empty;
    public decimal MaxErrorRatePercent { get; init; }
    public decimal MaxAverageLatencyMs { get; init; }
    public string NotificationEmail { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

file sealed class AlertThresholdListDto
{
    public List<AlertThresholdDto> Items { get; init; } = [];
}

file sealed class AlertEventDto
{
    public string Id { get; init; } = string.Empty;
    public string EndpointName { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public decimal ObservedValue { get; init; }
    public decimal ThresholdValue { get; init; }
    public bool NotificationSent { get; init; }
}

file sealed class AlertEventListDto
{
    public List<AlertEventDto> Items { get; init; } = [];
}

file sealed class PoisonQueueTrendDto
{
    public List<PoisonQueueTrendPointDto> Items { get; init; } = [];
}

file sealed class PoisonQueueTrendPointDto
{
    public DateTime BucketStartUtc { get; init; }
    public string EndpointName { get; init; } = string.Empty;
    public string JobType { get; init; } = string.Empty;
    public int EventCount { get; init; }
    public decimal MaxObservedDepth { get; init; }
    public decimal AverageObservedDepth { get; init; }
    public decimal LastObservedDepth { get; init; }
    public DateTime LastTriggeredAtUtc { get; init; }
}

file sealed class PoisonQueuePriorityDto
{
    public List<PoisonQueuePriorityPointDto> Items { get; init; } = [];
}

file sealed class PoisonQueuePriorityPointDto
{
    public string EndpointName { get; init; } = string.Empty;
    public string JobType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public string RunbookHint { get; init; } = string.Empty;
    public string RemediationPath { get; init; } = string.Empty;
    public DateTime CurrentBucketStartUtc { get; init; }
    public DateTime? PreviousBucketStartUtc { get; init; }
    public decimal CurrentMaxObservedDepth { get; init; }
    public decimal PreviousMaxObservedDepth { get; init; }
    public decimal DeltaDepth { get; init; }
    public decimal DeltaPercent { get; init; }
    public int CurrentEventCount { get; init; }
}

file sealed class PoisonQueueRemediationEffectivenessDto
{
    public int TotalRuns { get; init; }
    public int ResolvedRuns { get; init; }
    public int PartialRuns { get; init; }
    public int FailedRuns { get; init; }
    public decimal SuccessRatePercent { get; init; }
    public decimal AverageResolutionLatencyMinutes { get; init; }
    public decimal AverageResolvedLatencyMinutes { get; init; }
}

file sealed class PoisonQueueRemediationRunDto
{
    public Guid Id { get; init; }
    public string Outcome { get; init; } = string.Empty;
}

file sealed class PoisonQueueRemediationImpactDto
{
    public List<PoisonQueueRemediationImpactPointDto> Items { get; init; } = [];
    public decimal PositiveImpactRatePercent { get; init; }
}

file sealed class PoisonQueueRemediationImpactPointDto
{
    public string JobType { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public bool IsPositiveImpact { get; init; }
    public decimal DepthDelta { get; init; }
}

file sealed class PoisonQueueRemediationSegmentDto
{
    public List<PoisonQueueImpactSegmentItemDto> ByJobType { get; init; } = [];
    public List<PoisonQueueImpactSegmentItemDto> BySeverity { get; init; } = [];
}

file sealed class PoisonQueueImpactSegmentItemDto
{
    public string SegmentKey { get; init; } = string.Empty;
    public int TotalRuns { get; init; }
    public int PositiveImpactRuns { get; init; }
    public decimal PositiveImpactRatePercent { get; init; }
    public decimal AverageDepthDelta { get; init; }
}

file sealed class EmailLogDto
{
    public string TemplateName { get; init; } = string.Empty;
}

file sealed class AlertEventStatusDto
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? AcknowledgedBy { get; init; }
    public DateTime? AcknowledgedAtUtc { get; init; }
    public DateTime? SnoozedUntilUtc { get; init; }
    public string? ResolvedBy { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public string? StatusNotes { get; init; }
}

file sealed class AlertThresholdWebhookDto
{
    public Guid Id { get; init; }
    public string EndpointName { get; init; } = string.Empty;
    public string? WebhookUrl { get; init; }
}
file sealed class SloStatusItemDto
{
    public string EndpointName { get; init; } = string.Empty;
    public decimal SloErrorRateTarget { get; init; }
    public decimal SloLatencyTarget { get; init; }
    public string Compliance { get; init; } = string.Empty;
}

file sealed class SeverityElevationCandidateDto
{
    public string EndpointName { get; init; } = string.Empty;
    public int TotalRuns { get; init; }
    public decimal PositiveImpactRatePercent { get; init; }
}

file sealed class AlertRunbookDto
{
    public string MetricName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public List<string> Steps { get; init; } = [];
}

file sealed class AlertHeatmapPointDto
{
    public int HourOfDay { get; init; }
    public int EventCount { get; init; }
    public string EndpointName { get; init; } = string.Empty;
}

file sealed class AlertTenantSummaryDto
{
    public string TenantId { get; init; } = string.Empty;
    public int ActiveThresholdsCount { get; init; }
    public int OpenEventsCount { get; init; }
    public int AcknowledgedEventsCount { get; init; }
    public int ResolvedEventsCount { get; init; }
    public decimal ResolutionRatePercent { get; init; }
}

file sealed class AlertEventsPurgeRequestDto
{
    public int RetentionDays { get; init; }
}

file sealed class AlertEventsPurgeResultDto
{
    public int PurgedCount { get; init; }
    public int RetentionDays { get; init; }
    public DateTime PurgedBeforeUtc { get; init; }
}

file sealed class AlertTrendsResponseDto
{
    public string EndpointName { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public decimal P50 { get; init; }
    public decimal P90 { get; init; }
    public decimal P99 { get; init; }
    public decimal Mean { get; init; }
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public int SampleCount { get; init; }
}
