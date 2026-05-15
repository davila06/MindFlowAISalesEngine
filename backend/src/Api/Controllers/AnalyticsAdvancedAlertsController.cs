using Api.Application.Common.Interfaces;
using Api.Application.Observability;
using Api.Application.Security;
using Api.Contracts.Analytics;
using Api.Domain.Observability;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/analytics/advanced")]
public sealed class AnalyticsAdvancedAlertsController : ControllerBase
{
    private readonly IAlertThresholdRepository _thresholdRepository;
    private readonly IAlertEventRepository _eventRepository;
    private readonly IPoisonQueueRemediationRunRepository _remediationRunRepository;
    private readonly IAdminAuditService _adminAuditService;
    private readonly ITenantContext _tenantContext;

    public AnalyticsAdvancedAlertsController(
        IAlertThresholdRepository thresholdRepository,
        IAlertEventRepository eventRepository,
        IPoisonQueueRemediationRunRepository remediationRunRepository,
        IAdminAuditService adminAuditService,
        ITenantContext tenantContext)
    {
        _thresholdRepository = thresholdRepository;
        _eventRepository = eventRepository;
        _remediationRunRepository = remediationRunRepository;
        _adminAuditService = adminAuditService;
        _tenantContext = tenantContext;
    }

    [HttpPost("alert-thresholds")]
    public async Task<IActionResult> CreateThreshold(
        [FromBody] AlertThresholdCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var threshold = new AlertThreshold(
            request.EndpointName.Trim(),
            request.MaxErrorRatePercent,
            request.MaxAverageLatencyMs,
            request.NotificationEmail.Trim(),
            request.IsActive,
            string.IsNullOrWhiteSpace(request.WebhookUrl) ? null : request.WebhookUrl.Trim());

        await _thresholdRepository.AddAsync(threshold, cancellationToken);
        await _adminAuditService.RecordAsync(
            "alert_threshold_created",
            "analytics/advanced/alert-thresholds",
            $"ThresholdId={threshold.Id}; Endpoint={threshold.EndpointName}",
            cancellationToken);

        return Created($"/api/analytics/advanced/alert-thresholds/{threshold.Id}", MapThreshold(threshold));
    }

    [HttpGet("alert-thresholds")]
    public async Task<IActionResult> GetThresholds(
        [FromQuery] bool? isActive,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var items = await _thresholdRepository.GetAllAsync(cancellationToken);
        if (isActive.HasValue)
        {
            items = items.Where(x => x.IsActive == isActive.Value).ToList();
        }

        if (page is > 0 && pageSize is > 0)
        {
            items = items
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();
        }

        return Ok(new AlertThresholdListResponse
        {
            Items = items.Select(MapThreshold).ToList()
        });
    }

    [HttpGet("alert-thresholds/{id:guid}")]
    public async Task<IActionResult> GetThresholdById(Guid id, CancellationToken cancellationToken)
    {
        var threshold = await _thresholdRepository.GetByIdAsync(id, cancellationToken);
        if (threshold is null)
        {
            return NotFound();
        }
        return Ok(MapThreshold(threshold));
    }

    [HttpPut("alert-thresholds/{id:guid}")]
    public async Task<IActionResult> UpdateThreshold(
        Guid id,
        [FromBody] AlertThresholdUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var threshold = await _thresholdRepository.GetByIdAsync(id, cancellationToken);
        if (threshold is null)
        {
            return NotFound();
        }

        threshold.Update(
            request.EndpointName.Trim(),
            request.MaxErrorRatePercent,
            request.MaxAverageLatencyMs,
            request.NotificationEmail.Trim(),
            request.IsActive);

        await _thresholdRepository.SaveChangesAsync(cancellationToken);
        await _adminAuditService.RecordAsync(
            "alert_threshold_updated",
            "analytics/advanced/alert-thresholds",
            $"ThresholdId={id}; Endpoint={threshold.EndpointName}",
            cancellationToken);

        return Ok(MapThreshold(threshold));
    }

    [HttpDelete("alert-thresholds/{id:guid}")]
    public async Task<IActionResult> DeleteThreshold(Guid id, CancellationToken cancellationToken)
    {
        var threshold = await _thresholdRepository.GetByIdAsync(id, cancellationToken);
        if (threshold is null)
        {
            return NotFound();
        }

        _thresholdRepository.Remove(threshold);
        await _thresholdRepository.SaveChangesAsync(cancellationToken);
        await _adminAuditService.RecordAsync(
            "alert_threshold_deleted",
            "analytics/advanced/alert-thresholds",
            $"ThresholdId={id}",
            cancellationToken);

        return NoContent();
    }

    [HttpGet("alert-events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? endpointName,
        [FromQuery] string? metricName,
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] bool? notificationSent,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var items = await _eventRepository.QueryAsync(
            endpointName,
            metricName,
            startUtc,
            endUtc,
            notificationSent,
            page,
            pageSize,
            cancellationToken);

        return Ok(new AlertEventListResponse
        {
            Items = items.Select(MapEvent).ToList()
        });
    }

        [HttpPut("alert-events/{id:guid}/status")]
        public async Task<IActionResult> UpdateAlertEventStatus(
            Guid id,
            [FromBody] AlertEventUpdateStatusRequest request,
            CancellationToken cancellationToken)
        {
            var alertEvent = await _eventRepository.GetByIdAsync(id, cancellationToken);
            if (alertEvent is null)
                return NotFound(new { message = $"Alert event {id} not found." });

            var action = request.Action?.Trim().ToLowerInvariant();
            var actor = string.IsNullOrWhiteSpace(request.Actor) ? "system" : request.Actor.Trim();

            switch (action)
            {
                case "acknowledge":
                    alertEvent.Acknowledge(actor, request.Notes);
                    break;
                case "snooze":
                    if (!request.SnoozeUntilUtc.HasValue)
                        return BadRequest(new { message = "snoozeUntilUtc is required for snooze action." });
                    alertEvent.Snooze(request.SnoozeUntilUtc.Value, actor, request.Notes);
                    break;
                case "resolve":
                    alertEvent.Resolve(actor, request.Notes);
                    break;
                default:
                    return BadRequest(new { message = "action must be one of: acknowledge, snooze, resolve." });
            }

            await _eventRepository.SaveChangesAsync(cancellationToken);
            return Ok(MapEvent(alertEvent));
        }

        [HttpGet("alert-events/poison-queue-trend")]
    public async Task<IActionResult> GetPoisonQueueTrend(
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] string? jobType,
        [FromQuery] string? bucket,
        CancellationToken cancellationToken)
    {
        var bucketMode = string.IsNullOrWhiteSpace(bucket)
            ? "day"
            : bucket.Trim().ToLowerInvariant();

        if (!string.Equals(bucketMode, "day", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(bucketMode, "hour", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "bucket must be either 'day' or 'hour'." });
        }

        var events = await _eventRepository.QueryAsync(null, "PoisonQueueDepth", startUtc, endUtc, null, null, null, cancellationToken);
        var points = BuildPoisonQueueTrendPoints(events, bucketMode, jobType);

        return Ok(new PoisonQueueTrendResponse
        {
            Items = points
        });
    }

    [HttpGet("alert-events/poison-queue-priority")]
    public async Task<IActionResult> GetPoisonQueuePriority(
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] string? jobType,
        [FromQuery] string? bucket,
        [FromQuery] int? windowHours,
        [FromQuery] int? top,
        CancellationToken cancellationToken)
    {
        var bucketMode = string.IsNullOrWhiteSpace(bucket)
            ? "hour"
            : bucket.Trim().ToLowerInvariant();

        if (!string.Equals(bucketMode, "day", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(bucketMode, "hour", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "bucket must be either 'day' or 'hour'." });
        }

        var effectiveWindowHours = windowHours.GetValueOrDefault(24);
        if (effectiveWindowHours <= 0 || effectiveWindowHours > 720)
        {
            return BadRequest(new { message = "windowHours must be between 1 and 720." });
        }

        var effectiveTop = top.GetValueOrDefault(5);
        if (effectiveTop <= 0 || effectiveTop > 50)
        {
            return BadRequest(new { message = "top must be between 1 and 50." });
        }

        var effectiveEndUtc = endUtc ?? DateTime.UtcNow;
        var effectiveStartUtc = startUtc ?? effectiveEndUtc.AddHours(-effectiveWindowHours);

        var events = await _eventRepository.QueryAsync(null, "PoisonQueueDepth", effectiveStartUtc, effectiveEndUtc, null, null, null, cancellationToken);
        var points = BuildPoisonQueueTrendPoints(events, bucketMode, jobType);

        var ranked = points
            .GroupBy(x => x.EndpointName)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.BucketStartUtc).ToList();
                var current = ordered[^1];
                var previous = ordered.Count > 1 ? ordered[^2] : null;

                var previousMaxDepth = previous?.MaxObservedDepth ?? 0m;
                var deltaDepth = current.MaxObservedDepth - previousMaxDepth;
                var deltaPercent = previousMaxDepth > 0
                    ? decimal.Round((deltaDepth / previousMaxDepth) * 100m, 2)
                    : (current.MaxObservedDepth > 0 ? 100m : 0m);

                return new
                {
                    Severity = DetermineSeverity(current.MaxObservedDepth, deltaDepth, deltaPercent, current.EventCount),
                    Item = new PoisonQueuePriorityPointResponse
                    {
                        EndpointName = current.EndpointName,
                        JobType = current.JobType,
                        Severity = DetermineSeverity(current.MaxObservedDepth, deltaDepth, deltaPercent, current.EventCount),
                        RecommendedAction = BuildRecommendedAction(current.JobType, DetermineSeverity(current.MaxObservedDepth, deltaDepth, deltaPercent, current.EventCount)),
                        RunbookHint = BuildRunbookHint(current.JobType, DetermineSeverity(current.MaxObservedDepth, deltaDepth, deltaPercent, current.EventCount)),
                        RemediationPath = BuildRemediationPath(current.JobType),
                        CurrentBucketStartUtc = current.BucketStartUtc,
                        PreviousBucketStartUtc = previous?.BucketStartUtc,
                        CurrentMaxObservedDepth = current.MaxObservedDepth,
                        PreviousMaxObservedDepth = previousMaxDepth,
                        DeltaDepth = deltaDepth,
                        DeltaPercent = deltaPercent,
                        CurrentEventCount = current.EventCount
                    },
                    Rank = SeverityRank(DetermineSeverity(current.MaxObservedDepth, deltaDepth, deltaPercent, current.EventCount)),
                    DeltaDepth = deltaDepth,
                    DeltaPercent = deltaPercent,
                    CurrentMax = current.MaxObservedDepth
                };
            })
            .OrderByDescending(x => x.Rank)
            .ThenByDescending(x => x.DeltaDepth)
            .ThenByDescending(x => x.DeltaPercent)
            .ThenByDescending(x => x.CurrentMax)
            .Take(effectiveTop)
            .Select(x => x.Item)
            .ToList();

        return Ok(new PoisonQueuePriorityResponse
        {
            Items = ranked
        });
    }

    [HttpPost("alert-events/poison-queue-remediation-runs")]
    public async Task<IActionResult> RecordPoisonQueueRemediationRun(
        [FromBody] PoisonQueueRemediationRunCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EndpointName)
            || string.IsNullOrWhiteSpace(request.JobType)
            || string.IsNullOrWhiteSpace(request.Outcome))
        {
            return BadRequest(new { message = "endpointName, jobType and outcome are required." });
        }

        var run = new PoisonQueueRemediationRun(
            request.EndpointName,
            request.JobType,
            request.Severity,
            request.RecommendedAction,
            request.RemediationPath,
            request.Outcome,
            request.ExecutedBy,
            DateTime.UtcNow,
            request.DetectedAtUtc,
            request.Notes);

        await _remediationRunRepository.AddAsync(run, cancellationToken);
        await _remediationRunRepository.SaveChangesAsync(cancellationToken);

        await _adminAuditService.RecordAsync(
            "poison_queue_remediation_recorded",
            "analytics/advanced/alert-events/poison-queue-remediation-runs",
            $"RunId={run.Id}; JobType={run.JobType}; Outcome={run.Outcome}; Severity={run.Severity}",
            cancellationToken);

        return Created($"/api/analytics/advanced/alert-events/poison-queue-remediation-runs/{run.Id}", MapRemediationRun(run));
    }

    [HttpGet("alert-events/poison-queue-remediation-runs")]
    public async Task<IActionResult> GetPoisonQueueRemediationRuns(
        [FromQuery] string? jobType,
        [FromQuery] string? outcome,
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        CancellationToken cancellationToken)
    {
        var items = await _remediationRunRepository.QueryAsync(jobType, outcome, startUtc, endUtc, cancellationToken);
        return Ok(new PoisonQueueRemediationRunListResponse
        {
            Items = items.Select(MapRemediationRun).ToList()
        });
    }

    [HttpPut("alert-events/poison-queue-remediation-runs/{id:guid}")]
    public async Task<IActionResult> UpdatePoisonQueueRemediationRun(
        Guid id,
        [FromBody] PoisonQueueRemediationRunUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedOutcome = request.Outcome.Trim().ToLowerInvariant();
        if (normalizedOutcome is not ("opened" or "in_progress" or "resolved" or "partial" or "failed"))
        {
            return BadRequest(new { message = "outcome must be one of: opened, in_progress, resolved, partial, failed." });
        }

        var run = await _remediationRunRepository.GetByIdAsync(id, cancellationToken);
        if (run is null)
        {
            return NotFound();
        }

        run.UpdateOutcome(normalizedOutcome, request.ExecutedBy, DateTime.UtcNow, request.Notes);
        await _remediationRunRepository.SaveChangesAsync(cancellationToken);

        await _adminAuditService.RecordAsync(
            "poison_queue_remediation_updated",
            "analytics/advanced/alert-events/poison-queue-remediation-runs",
            $"RunId={run.Id}; JobType={run.JobType}; Outcome={run.Outcome}; Severity={run.Severity}",
            cancellationToken);

        return Ok(MapRemediationRun(run));
    }

    [HttpGet("alert-events/poison-queue-remediation-effectiveness")]
    public async Task<IActionResult> GetPoisonQueueRemediationEffectiveness(
        [FromQuery] int? windowHours,
        [FromQuery] string? jobType,
        CancellationToken cancellationToken)
    {
        var effectiveWindowHours = windowHours.GetValueOrDefault(168);
        if (effectiveWindowHours <= 0 || effectiveWindowHours > 24 * 90)
        {
            return BadRequest(new { message = "windowHours must be between 1 and 2160." });
        }

        var endUtc = DateTime.UtcNow;
        var startUtc = endUtc.AddHours(-effectiveWindowHours);
        var items = await _remediationRunRepository.QueryAsync(jobType, null, startUtc, endUtc, cancellationToken);

        var totalRuns = items.Count;
        var resolvedRuns = items.Count(x => x.Outcome == "resolved");
        var partialRuns = items.Count(x => x.Outcome == "partial");
        var failedRuns = items.Count(x => x.Outcome == "failed");

        var avgLatency = totalRuns > 0
            ? decimal.Round(items.Average(x => x.ResolutionLatencyMinutes), 2)
            : 0m;

        var resolvedOnly = items.Where(x => x.Outcome == "resolved").ToList();
        var avgResolvedLatency = resolvedOnly.Count > 0
            ? decimal.Round(resolvedOnly.Average(x => x.ResolutionLatencyMinutes), 2)
            : 0m;

        var successRatePercent = totalRuns > 0
            ? decimal.Round((decimal)resolvedRuns / totalRuns * 100m, 2)
            : 0m;

        return Ok(new PoisonQueueRemediationEffectivenessResponse
        {
            TotalRuns = totalRuns,
            ResolvedRuns = resolvedRuns,
            PartialRuns = partialRuns,
            FailedRuns = failedRuns,
            SuccessRatePercent = successRatePercent,
            AverageResolutionLatencyMinutes = avgLatency,
            AverageResolvedLatencyMinutes = avgResolvedLatency
        });
    }

    [HttpGet("alert-events/poison-queue-remediation-impact")]
    public async Task<IActionResult> GetPoisonQueueRemediationImpact(
        [FromQuery] int? windowHours,
        [FromQuery] int? observationMinutes,
        [FromQuery] string? jobType,
        [FromQuery] string? outcome,
        CancellationToken cancellationToken)
    {
        var effectiveWindowHours = windowHours.GetValueOrDefault(168);
        if (effectiveWindowHours <= 0 || effectiveWindowHours > 24 * 90)
        {
            return BadRequest(new { message = "windowHours must be between 1 and 2160." });
        }

        var effectiveObservationMinutes = observationMinutes.GetValueOrDefault(180);
        if (effectiveObservationMinutes <= 0 || effectiveObservationMinutes > 24 * 60)
        {
            return BadRequest(new { message = "observationMinutes must be between 1 and 1440." });
        }

        var endUtc = DateTime.UtcNow;
        var startUtc = endUtc.AddHours(-effectiveWindowHours);

        var normalizedOutcome = string.IsNullOrWhiteSpace(outcome)
            ? null
            : outcome.Trim().ToLowerInvariant();

        var runs = await _remediationRunRepository.QueryAsync(jobType, normalizedOutcome, startUtc, endUtc, cancellationToken);
        var closedRuns = runs
            .Where(x => x.Outcome is "resolved" or "partial" or "failed")
            .ToList();

        var points = await BuildRemediationImpactPointsAsync(closedRuns, effectiveObservationMinutes, cancellationToken);
        var comparable = points.Where(x => x.PreDepth.HasValue && x.PostDepth.HasValue).ToList();

        var positiveImpactRate = comparable.Count > 0
            ? decimal.Round((decimal)comparable.Count(x => x.IsPositiveImpact) / comparable.Count * 100m, 2)
            : 0m;

        var averageDepthDelta = comparable.Count > 0
            ? decimal.Round(comparable.Average(x => x.DepthDelta ?? 0m), 2)
            : 0m;

        return Ok(new PoisonQueueRemediationImpactResponse
        {
            Items = points,
            PositiveImpactRatePercent = positiveImpactRate,
            AverageDepthDelta = averageDepthDelta
        });
    }

    [HttpGet("alert-events/poison-queue-remediation-impact/by-segment")]
    public async Task<IActionResult> GetPoisonQueueRemediationImpactBySegment(
        [FromQuery] int? windowHours,
        [FromQuery] int? observationMinutes,
        CancellationToken cancellationToken)
    {
        var effectiveWindowHours = windowHours.GetValueOrDefault(168);
        if (effectiveWindowHours <= 0 || effectiveWindowHours > 24 * 90)
            return BadRequest(new { message = "windowHours must be between 1 and 2160." });

        var effectiveObservationMinutes = observationMinutes.GetValueOrDefault(180);
        if (effectiveObservationMinutes <= 0 || effectiveObservationMinutes > 24 * 60)
            return BadRequest(new { message = "observationMinutes must be between 1 and 1440." });

        var endUtc = DateTime.UtcNow;
        var startUtc = endUtc.AddHours(-effectiveWindowHours);

        var runs = await _remediationRunRepository.QueryAsync(null, null, startUtc, endUtc, cancellationToken);
        var closedRuns = runs
            .Where(x => x.Outcome is "resolved" or "partial" or "failed")
            .ToList();

        var points = await BuildRemediationImpactPointsAsync(closedRuns, effectiveObservationMinutes, cancellationToken);

        static PoisonQueueImpactSegmentItemResponse BuildSegment(string key, IReadOnlyList<PoisonQueueRemediationImpactPointResponse> items)
        {
            var withDepth = items.Where(x => x.PreDepth.HasValue && x.PostDepth.HasValue).ToList();
            var positiveCount = withDepth.Count(x => x.IsPositiveImpact);
            var rate = withDepth.Count > 0
                ? decimal.Round((decimal)positiveCount / withDepth.Count * 100m, 2)
                : 0m;
            var avgDelta = withDepth.Count > 0
                ? decimal.Round(withDepth.Average(x => x.DepthDelta ?? 0m), 2)
                : 0m;

            return new PoisonQueueImpactSegmentItemResponse
            {
                SegmentKey = key,
                TotalRuns = items.Count,
                PositiveImpactRuns = positiveCount,
                PositiveImpactRatePercent = rate,
                AverageDepthDelta = avgDelta
            };
        }

        var byJobType = points
            .GroupBy(x => x.JobType)
            .Select(g => BuildSegment(g.Key, g.ToList()))
            .OrderByDescending(x => x.TotalRuns)
            .ToList();

        var severityLookup = closedRuns.ToDictionary(r => r.Id, r => r.Severity);
        var bySeverity = points
            .Where(x => severityLookup.ContainsKey(x.RunId))
            .GroupBy(x => severityLookup[x.RunId])
            .Select(g => BuildSegment(g.Key, g.ToList()))
            .OrderByDescending(x => x.TotalRuns)
            .ToList();

        return Ok(new PoisonQueueRemediationSegmentResponse
        {
            ByJobType = byJobType,
            BySeverity = bySeverity
        });
    }

    private static List<PoisonQueueTrendPointResponse> BuildPoisonQueueTrendPoints(
        IReadOnlyList<AlertEvent> events,
        string bucketMode,
        string? jobType)
    {
        var poisonEvents = events
            .Where(x => x.EndpointName.StartsWith("poison-queue/", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(jobType))
        {
            var normalizedJobType = jobType.Trim().ToLowerInvariant();
            poisonEvents = poisonEvents.Where(x =>
                string.Equals(ExtractJobType(x.EndpointName), normalizedJobType, StringComparison.OrdinalIgnoreCase));
        }

        return poisonEvents
            .GroupBy(x => new
            {
                BucketStartUtc = ToBucketUtc(x.TriggeredAtUtc, bucketMode),
                x.EndpointName
            })
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.TriggeredAtUtc).ToList();
                var last = ordered[^1];

                return new PoisonQueueTrendPointResponse
                {
                    BucketStartUtc = group.Key.BucketStartUtc,
                    EndpointName = group.Key.EndpointName,
                    JobType = ExtractJobType(group.Key.EndpointName),
                    EventCount = ordered.Count,
                    MaxObservedDepth = ordered.Max(x => x.ObservedValue),
                    AverageObservedDepth = decimal.Round(ordered.Average(x => x.ObservedValue), 2),
                    LastObservedDepth = last.ObservedValue,
                    LastTriggeredAtUtc = last.TriggeredAtUtc
                };
            })
            .OrderByDescending(x => x.BucketStartUtc)
            .ThenBy(x => x.EndpointName)
            .ToList();
    }

    private async Task<List<PoisonQueueRemediationImpactPointResponse>> BuildRemediationImpactPointsAsync(
        IReadOnlyList<PoisonQueueRemediationRun> runs,
        int observationMinutes,
        CancellationToken cancellationToken)
    {
        var points = new List<PoisonQueueRemediationImpactPointResponse>(runs.Count);

        foreach (var run in runs.OrderByDescending(x => x.ExecutedAtUtc))
        {
            var preStartUtc = run.ExecutedAtUtc.AddHours(-24);
            var postEndUtc = run.ExecutedAtUtc.AddMinutes(observationMinutes);

            var preEvents = await _eventRepository.QueryAsync(
                run.EndpointName,
                "PoisonQueueDepth",
                preStartUtc,
                run.ExecutedAtUtc,
                null,
                null,
                null,
                cancellationToken);

            var postEvents = await _eventRepository.QueryAsync(
                run.EndpointName,
                "PoisonQueueDepth",
                run.ExecutedAtUtc,
                postEndUtc,
                null,
                null,
                null,
                cancellationToken);

            var preDepth = preEvents
                .OrderByDescending(x => x.TriggeredAtUtc)
                .Select(x => (decimal?)x.ObservedValue)
                .FirstOrDefault();

            var postDepth = postEvents.Count > 0
                ? postEvents.Min(x => x.ObservedValue)
                : (decimal?)null;

            var depthDelta = preDepth.HasValue && postDepth.HasValue
                ? decimal.Round(postDepth.Value - preDepth.Value, 2)
                : (decimal?)null;

            var reductionPercent = preDepth.HasValue && postDepth.HasValue && preDepth.Value > 0
                ? decimal.Round(((preDepth.Value - postDepth.Value) / preDepth.Value) * 100m, 2)
                : (decimal?)null;

            points.Add(new PoisonQueueRemediationImpactPointResponse
            {
                RunId = run.Id,
                EndpointName = run.EndpointName,
                JobType = run.JobType,
                Outcome = run.Outcome,
                ExecutedAtUtc = run.ExecutedAtUtc,
                PreDepth = preDepth,
                PostDepth = postDepth,
                DepthDelta = depthDelta,
                ReductionPercent = reductionPercent,
                IsPositiveImpact = preDepth.HasValue && postDepth.HasValue && postDepth.Value < preDepth.Value
            });
        }

        return points;
    }

    private static string DetermineSeverity(decimal currentMaxDepth, decimal deltaDepth, decimal deltaPercent, int eventCount)
    {
        if (currentMaxDepth >= 8m || deltaDepth >= 4m || deltaPercent >= 100m || eventCount >= 4)
        {
            return "critical";
        }

        if (currentMaxDepth >= 5m || deltaDepth >= 2m || deltaPercent >= 50m || eventCount >= 3)
        {
            return "high";
        }

        if (currentMaxDepth >= 3m || deltaDepth >= 1m || deltaPercent >= 20m || eventCount >= 2)
        {
            return "medium";
        }

        return "low";
    }

    private static int SeverityRank(string severity)
    {
        return severity switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            _ => 1
        };
    }

    private static string BuildRemediationPath(string jobType)
    {
        return jobType switch
        {
            "proposal-reminder" => "/api/proposals/reminders/poison-queue",
            "follow-up" => "/api/followup/poison-queue",
            "onboarding-welcome" => "/api/onboarding/welcome-jobs/poison-queue",
            _ => string.Empty
        };
    }

    private static string BuildRunbookHint(string jobType, string severity)
    {
        var normalizedJob = string.IsNullOrWhiteSpace(jobType) ? "unknown" : jobType;
        return severity switch
        {
            "critical" => $"Escalate on-call immediately. Validate {normalizedJob} poison queue depth and execute controlled requeue after root-cause check.",
            "high" => $"Review {normalizedJob} delivery failures in latest bucket and start remediation runbook with targeted requeue.",
            "medium" => $"Monitor {normalizedJob} trend and prepare staged requeue if next bucket keeps increasing.",
            _ => $"Keep observing {normalizedJob} trend; no immediate remediation required unless growth accelerates."
        };
    }

    private static string BuildRecommendedAction(string jobType, string severity)
    {
        return severity switch
        {
            "critical" => $"Escalate + run poison remediation for {jobType}",
            "high" => $"Execute remediation workflow for {jobType}",
            "medium" => $"Prepare requeue window for {jobType}",
            _ => $"Monitor {jobType} trend"
        };
    }

    private static DateTime ToBucketUtc(DateTime value, string bucketMode)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return string.Equals(bucketMode, "hour", StringComparison.OrdinalIgnoreCase)
            ? new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, DateTimeKind.Utc)
            : utc.Date;
    }

    private static string ExtractJobType(string endpointName)
    {
        const string prefix = "poison-queue/";
        if (!endpointName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return endpointName[prefix.Length..].Trim().ToLowerInvariant();
    }
    [HttpGet("alert-events/slo-status")]
    public async Task<IActionResult> GetSloStatus(CancellationToken cancellationToken)
    {
        var thresholds = await _thresholdRepository.GetActiveAsync(cancellationToken);
        var result = thresholds
            .Select(t => new SloStatusItemResponse
            {
                EndpointName = t.EndpointName,
                SloErrorRateTarget = t.MaxErrorRatePercent,
                SloLatencyTarget = t.MaxAverageLatencyMs,
                ObservedErrorRate = 0m,
                ObservedLatencyMs = 0m,
                Compliance = "compliant"
            })
            .ToList();
        return Ok(result);
    }

    [HttpGet("alert-events/severity-elevation-candidates")]
    public async Task<IActionResult> GetSeverityElevationCandidates(CancellationToken cancellationToken)
    {
        var runs = await _remediationRunRepository.QueryAsync(null, null, null, null, cancellationToken);
        var candidates = runs
            .GroupBy(r => r.EndpointName)
            .Select(g =>
            {
                var total = g.Count();
                var positive = g.Count(r => r.Outcome == "resolved");
                var rate = total > 0 ? decimal.Round((decimal)positive / total * 100m, 2) : 0m;
                return new SeverityElevationCandidateResponse
                {
                    EndpointName = g.Key,
                    TotalRuns = total,
                    PositiveImpactRatePercent = rate
                };
            })
            .Where(x => x.PositiveImpactRatePercent < 50m)
            .OrderBy(x => x.PositiveImpactRatePercent)
            .ToList();
        return Ok(candidates);
    }

    private static readonly IReadOnlyDictionary<string, AlertRunbookResponse> RunbookCatalog =
        new Dictionary<string, AlertRunbookResponse>(StringComparer.OrdinalIgnoreCase)
        {
            ["ErrorRatePercent"] = new AlertRunbookResponse
            {
                MetricName = "ErrorRatePercent",
                Title = "High Error Rate Runbook",
                Steps =
                [
                    "1. Check recent deployments and rollback if error spike coincides.",
                    "2. Inspect application logs for 5xx patterns.",
                    "3. Validate downstream dependencies (DB, external APIs).",
                    "4. Scale horizontally if request queue depth is elevated.",
                    "5. Notify on-call team if not resolved within 15 minutes."
                ]
            },
            ["AverageLatencyMs"] = new AlertRunbookResponse
            {
                MetricName = "AverageLatencyMs",
                Title = "High Latency Runbook",
                Steps =
                [
                    "1. Identify slow endpoints via distributed traces.",
                    "2. Check DB query performance (indexes, long-running queries).",
                    "3. Review cache hit rates — warm cache if needed.",
                    "4. Inspect memory and CPU pressure on API instances.",
                    "5. Scale out if resource saturation is confirmed."
                ]
            },
            ["PoisonQueueDepth"] = new AlertRunbookResponse
            {
                MetricName = "PoisonQueueDepth",
                Title = "Poison Queue Depth Runbook",
                Steps =
                [
                    "1. Identify failing job type and inspect dead-letter messages.",
                    "2. Check for schema or contract changes that caused deserialization failures.",
                    "3. Fix the root cause (code bug, configuration, external service).",
                    "4. Execute controlled requeue for failed messages.",
                    "5. Monitor queue depth after requeue to confirm remediation."
                ]
            }
        };

    [HttpGet("alert-events/runbooks/{metricName}")]
    public IActionResult GetRunbook(string metricName)
    {
        if (!RunbookCatalog.TryGetValue(metricName, out var runbook))
            return NotFound(new { message = $"No runbook found for metric: {metricName}" });
        return Ok(runbook);
    }

    [HttpGet("alert-events/heatmap")]
    public async Task<IActionResult> GetAlertHeatmap(
        [FromQuery] string? endpointName,
        [FromQuery] int? windowHours,
        CancellationToken cancellationToken)
    {
        var effectiveWindowHours = windowHours.GetValueOrDefault(168);
        if (effectiveWindowHours <= 0 || effectiveWindowHours > 8760)
            return BadRequest(new { message = "windowHours must be between 1 and 8760." });

        var effectiveEnd = DateTime.UtcNow;
        var effectiveStart = effectiveEnd.AddHours(-effectiveWindowHours);

        var items = await _eventRepository.QueryAsync(endpointName, null, effectiveStart, effectiveEnd, null, null, null, cancellationToken);
        var heatmap = items
            .GroupBy(x => new
            {
                x.EndpointName,
                HourOfDay = (x.TriggeredAtUtc.Kind == DateTimeKind.Utc
                    ? x.TriggeredAtUtc
                    : DateTime.SpecifyKind(x.TriggeredAtUtc, DateTimeKind.Utc)).Hour
            })
            .Select(g => new AlertHeatmapPointResponse
            {
                EndpointName = g.Key.EndpointName,
                HourOfDay = g.Key.HourOfDay,
                EventCount = g.Count()
            })
            .OrderBy(x => x.EndpointName)
            .ThenBy(x => x.HourOfDay)
            .ToList();
        return Ok(heatmap);
    }

    [HttpGet("alert-events/tenant-summary")]
    public async Task<IActionResult> GetTenantSummary(CancellationToken cancellationToken)
    {
        var tenantId = string.IsNullOrWhiteSpace(_tenantContext.TenantId)
            ? "default"
            : _tenantContext.TenantId;

        var activeThresholds = await _thresholdRepository.CountActiveAsync(cancellationToken);
        var statusCounts = await _eventRepository.CountByStatusAsync(cancellationToken);

        var openEvents = statusCounts.TryGetValue("open", out var open) ? open : 0;
        var acknowledgedEvents = statusCounts.TryGetValue("acknowledged", out var acknowledged) ? acknowledged : 0;
        var resolvedEvents = statusCounts.TryGetValue("resolved", out var resolved) ? resolved : 0;
        var totalClosed = acknowledgedEvents + resolvedEvents;
        var totalManaged = openEvents + totalClosed;
        var resolutionRate = totalManaged > 0
            ? decimal.Round((decimal)resolvedEvents / totalManaged * 100m, 2)
            : 0m;

        return Ok(new AlertTenantSummaryResponse
        {
            TenantId = tenantId,
            ActiveThresholdsCount = activeThresholds,
            OpenEventsCount = openEvents,
            AcknowledgedEventsCount = acknowledgedEvents,
            ResolvedEventsCount = resolvedEvents,
            ResolutionRatePercent = resolutionRate
        });
    }

    [HttpGet("alert-events/trends")]
    public async Task<IActionResult> GetAlertTrends(
        [FromQuery] string? endpointName,
        [FromQuery] string? metricName,
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] int? windowHours,
        CancellationToken cancellationToken)
    {
        var effectiveWindowHours = windowHours.GetValueOrDefault(168);
        if (effectiveWindowHours <= 0 || effectiveWindowHours > 8760)
            return BadRequest(new { message = "windowHours must be between 1 and 8760." });

        var effectiveEnd = endUtc ?? DateTime.UtcNow;
        var effectiveStart = startUtc ?? effectiveEnd.AddHours(-effectiveWindowHours);

        var events = await _eventRepository.QueryAsync(endpointName, metricName, effectiveStart, effectiveEnd, null, null, null, cancellationToken);
        if (events.Count == 0)
        {
            return Ok(new AlertTrendsResponse
            {
                EndpointName = endpointName ?? string.Empty,
                MetricName = metricName ?? string.Empty,
                SampleCount = 0
            });
        }

        var sorted = events.Select(x => x.ObservedValue).OrderBy(v => v).ToList();
        var n = sorted.Count;

        static decimal Percentile(List<decimal> sorted, double p)
        {
            var index = (p / 100.0) * (sorted.Count - 1);
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);
            if (lower == upper) return sorted[lower];
            var frac = (decimal)(index - lower);
            return decimal.Round(sorted[lower] + frac * (sorted[upper] - sorted[lower]), 4);
        }

        return Ok(new AlertTrendsResponse
        {
            EndpointName = endpointName ?? string.Empty,
            MetricName = metricName ?? string.Empty,
            P50 = Percentile(sorted, 50),
            P90 = Percentile(sorted, 90),
            P99 = Percentile(sorted, 99),
            Mean = decimal.Round(sorted.Average(), 4),
            Min = sorted[0],
            Max = sorted[^1],
            SampleCount = n
        });
    }
    [HttpPost("alert-events/purge")]
    public async Task<IActionResult> PurgeAlertEvents(
        [FromBody] AlertEventsPurgeRequest request,
        CancellationToken cancellationToken)
    {
        var retentionDays = request.RetentionDays <= 0 ? 90 : request.RetentionDays;
        if (retentionDays > 3650)
            return BadRequest(new { message = "retentionDays cannot exceed 3650." });

        var purgedBeforeUtc = DateTime.UtcNow.AddDays(-retentionDays);
        var purgedCount = await _eventRepository.PurgeAsync(purgedBeforeUtc, cancellationToken);

        await _adminAuditService.RecordAsync(
            "alert_events_purged",
            "analytics/advanced/alert-events/purge",
            $"PurgedCount={purgedCount}; RetentionDays={retentionDays}; PurgedBeforeUtc={purgedBeforeUtc:O}",
            cancellationToken);

        return Ok(new AlertEventsPurgeResponse
        {
            PurgedCount = purgedCount,
            RetentionDays = retentionDays,
            PurgedBeforeUtc = purgedBeforeUtc
        });
    }



    private static AlertThresholdResponse MapThreshold(AlertThreshold threshold) => new()
    {
        Id = threshold.Id,
        EndpointName = threshold.EndpointName,
        MaxErrorRatePercent = threshold.MaxErrorRatePercent,
        MaxAverageLatencyMs = threshold.MaxAverageLatencyMs,
        NotificationEmail = threshold.NotificationEmail,
        IsActive = threshold.IsActive,
        WebhookUrl = threshold.WebhookUrl,
        CreatedAtUtc = threshold.CreatedAtUtc,
        UpdatedAtUtc = threshold.UpdatedAtUtc
    };

    private static AlertEventResponse MapEvent(AlertEvent alertEvent) => new()
    {
        Id = alertEvent.Id,
        ThresholdId = alertEvent.ThresholdId,
        EndpointName = alertEvent.EndpointName,
        MetricName = alertEvent.MetricName,
        ObservedValue = alertEvent.ObservedValue,
        ThresholdValue = alertEvent.ThresholdValue,
        TriggeredAtUtc = alertEvent.TriggeredAtUtc,
            NotificationSent = alertEvent.NotificationSent,
            Status = alertEvent.Status,
            AcknowledgedBy = alertEvent.AcknowledgedBy,
            AcknowledgedAtUtc = alertEvent.AcknowledgedAtUtc,
            SnoozedUntilUtc = alertEvent.SnoozedUntilUtc,
            ResolvedBy = alertEvent.ResolvedBy,
            ResolvedAtUtc = alertEvent.ResolvedAtUtc,
            StatusNotes = alertEvent.StatusNotes
    };

    private static PoisonQueueRemediationRunResponse MapRemediationRun(PoisonQueueRemediationRun run) => new()
    {
        Id = run.Id,
        EndpointName = run.EndpointName,
        JobType = run.JobType,
        Severity = run.Severity,
        RecommendedAction = run.RecommendedAction,
        RemediationPath = run.RemediationPath,
        Outcome = run.Outcome,
        ExecutedBy = run.ExecutedBy,
        ExecutedAtUtc = run.ExecutedAtUtc,
        DetectedAtUtc = run.DetectedAtUtc,
        ResolutionLatencyMinutes = run.ResolutionLatencyMinutes,
        Notes = run.Notes
    };
}



