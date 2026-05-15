namespace Api.Contracts.Analytics;

public sealed class AlertEventResponse
{
    public Guid Id { get; init; }
    public Guid ThresholdId { get; init; }
    public string EndpointName { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
    public decimal ObservedValue { get; init; }
    public decimal ThresholdValue { get; init; }
    public DateTime TriggeredAtUtc { get; init; }
    public bool NotificationSent { get; init; }
    public string Status { get; init; } = "open";
    public string? AcknowledgedBy { get; init; }
    public DateTime? AcknowledgedAtUtc { get; init; }
    public DateTime? SnoozedUntilUtc { get; init; }
    public string? ResolvedBy { get; init; }
    public DateTime? ResolvedAtUtc { get; init; }
    public string? StatusNotes { get; init; }
}

public sealed class AlertEventUpdateStatusRequest
{
    public string Action { get; init; } = string.Empty;   // acknowledge | snooze | resolve
    public string Actor { get; init; } = string.Empty;
    public DateTime? SnoozeUntilUtc { get; init; }
    public string? Notes { get; init; }
}

public sealed class AlertEventListResponse
{
    public IReadOnlyList<AlertEventResponse> Items { get; init; } = [];
}

public sealed class PoisonQueueTrendPointResponse
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

public sealed class PoisonQueueTrendResponse
{
    public IReadOnlyList<PoisonQueueTrendPointResponse> Items { get; init; } = [];
}

public sealed class PoisonQueuePriorityPointResponse
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

public sealed class PoisonQueuePriorityResponse
{
    public IReadOnlyList<PoisonQueuePriorityPointResponse> Items { get; init; } = [];
}

public sealed class PoisonQueueRemediationRunCreateRequest
{
    public string EndpointName { get; init; } = string.Empty;
    public string JobType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public string RemediationPath { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public string ExecutedBy { get; init; } = string.Empty;
    public DateTime? DetectedAtUtc { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class PoisonQueueRemediationRunUpdateRequest
{
    public string Outcome { get; init; } = string.Empty;
    public string ExecutedBy { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}

public sealed class PoisonQueueRemediationRunResponse
{
    public Guid Id { get; init; }
    public string EndpointName { get; init; } = string.Empty;
    public string JobType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string RecommendedAction { get; init; } = string.Empty;
    public string RemediationPath { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public string ExecutedBy { get; init; } = string.Empty;
    public DateTime ExecutedAtUtc { get; init; }
    public DateTime? DetectedAtUtc { get; init; }
    public decimal ResolutionLatencyMinutes { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class PoisonQueueRemediationRunListResponse
{
    public IReadOnlyList<PoisonQueueRemediationRunResponse> Items { get; init; } = [];
}

public sealed class PoisonQueueRemediationEffectivenessResponse
{
    public int TotalRuns { get; init; }
    public int ResolvedRuns { get; init; }
    public int PartialRuns { get; init; }
    public int FailedRuns { get; init; }
    public decimal SuccessRatePercent { get; init; }
    public decimal AverageResolutionLatencyMinutes { get; init; }
    public decimal AverageResolvedLatencyMinutes { get; init; }
}

public sealed class PoisonQueueRemediationImpactPointResponse
{
    public Guid RunId { get; init; }
    public string EndpointName { get; init; } = string.Empty;
    public string JobType { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public DateTime ExecutedAtUtc { get; init; }
    public decimal? PreDepth { get; init; }
    public decimal? PostDepth { get; init; }
    public decimal? DepthDelta { get; init; }
    public decimal? ReductionPercent { get; init; }
    public bool IsPositiveImpact { get; init; }
}

public sealed class PoisonQueueRemediationImpactResponse
{
    public IReadOnlyList<PoisonQueueRemediationImpactPointResponse> Items { get; init; } = [];
    public decimal PositiveImpactRatePercent { get; init; }
    public decimal AverageDepthDelta { get; init; }
}

public sealed class PoisonQueueImpactSegmentItemResponse
{
    public string SegmentKey { get; init; } = string.Empty;
    public int TotalRuns { get; init; }
    public int PositiveImpactRuns { get; init; }
    public decimal PositiveImpactRatePercent { get; init; }
    public decimal AverageDepthDelta { get; init; }
}

public sealed class PoisonQueueRemediationSegmentResponse
{
    public IReadOnlyList<PoisonQueueImpactSegmentItemResponse> ByJobType { get; init; } = [];
    public IReadOnlyList<PoisonQueueImpactSegmentItemResponse> BySeverity { get; init; } = [];
}


public sealed class SloStatusItemResponse
{
    public string EndpointName { get; init; } = string.Empty;
    public decimal SloErrorRateTarget { get; init; }
    public decimal SloLatencyTarget { get; init; }
    public decimal ObservedErrorRate { get; init; }
    public decimal ObservedLatencyMs { get; init; }
    public string Compliance { get; init; } = string.Empty;
}


public sealed class SeverityElevationCandidateResponse
{
    public string EndpointName { get; init; } = string.Empty;
    public int TotalRuns { get; init; }
    public decimal PositiveImpactRatePercent { get; init; }
}


public sealed class AlertRunbookResponse
{
    public string MetricName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<string> Steps { get; init; } = [];
}

public sealed class AlertHeatmapPointResponse
{
    public string EndpointName { get; init; } = string.Empty;
    public int HourOfDay { get; init; }
    public int EventCount { get; init; }
}


public sealed class AlertTenantSummaryResponse
{
    public string TenantId { get; init; } = string.Empty;
    public int ActiveThresholdsCount { get; init; }
    public int OpenEventsCount { get; init; }
    public int AcknowledgedEventsCount { get; init; }
    public int ResolvedEventsCount { get; init; }
    public decimal ResolutionRatePercent { get; init; }
}


public sealed class AlertEventsPurgeRequest
{
    public int RetentionDays { get; init; } = 90;
}

public sealed class AlertEventsPurgeResponse
{
    public int PurgedCount { get; init; }
    public int RetentionDays { get; init; }
    public DateTime PurgedBeforeUtc { get; init; }
}


public sealed class AlertTrendsResponse
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
