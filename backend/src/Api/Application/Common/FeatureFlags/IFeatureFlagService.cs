namespace Api.Application.Common.FeatureFlags;

/// <summary>
/// OPS-05 | Central feature-flag contract.
/// Flags are resolved per-tenant so high-risk changes can be rolled out
/// progressively without a full deployment.
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>Returns true when <paramref name="flagName"/> is enabled for the given tenant.</summary>
    bool IsEnabled(string flagName, string? tenantId = null);

    /// <summary>Returns a snapshot of all resolved flags for the current tenant.</summary>
    IReadOnlyDictionary<string, bool> GetAll(string? tenantId = null);
}
