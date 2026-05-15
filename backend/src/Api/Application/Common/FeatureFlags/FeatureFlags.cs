namespace Api.Application.Common.FeatureFlags;

/// <summary>
/// OPS-05 | Canonical feature flag keys.
/// Use these constants everywhere instead of raw strings to prevent typos
/// and allow refactoring with a single rename.
/// </summary>
public static class FeatureFlags
{
    // ── Canary / rollout flags ────────────────────────────────────────────
    /// <summary>Enables canary UI features visible only to opted-in tenants.</summary>
    public const string CanaryFeatures = "EnableCanaryFeatures";

    /// <summary>Enables the next-generation rules engine (behind flag during gradual rollout).</summary>
    public const string BetaRulesEngine = "EnableBetaRulesEngine";

    // ── Background services ───────────────────────────────────────────────
    /// <summary>Disables the SensitiveDataRetentionService background job (e.g., for test environments).</summary>
    public const string DisableDataRetentionBackground = "DisableDataRetentionBackground";

    // ── Analytics & observability ─────────────────────────────────────────
    /// <summary>Enables incremental aggregation of observability data (compute-intensive).</summary>
    public const string ObservabilityIncrementalAggregation = "EnableObservabilityIncrementalAggregation";

    // ── Email / follow-up ─────────────────────────────────────────────────
    /// <summary>Enables the multi-channel email dispatch (webhook + SMTP).</summary>
    public const string MultiChannelEmailDispatch = "EnableMultiChannelEmailDispatch";

    // ── Pipeline ──────────────────────────────────────────────────────────
    /// <summary>Enables WIP-limit enforcement on pipeline stages.</summary>
    public const string PipelineWipLimits = "EnablePipelineWipLimits";
}
