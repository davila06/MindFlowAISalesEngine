using Api.Contracts.Analytics;

namespace Api.Application.AnalyticsAdvanced;

public interface IAnalyticsAdvancedDataRepository
{
    Task<AnalyticsAdvancedDataSnapshot> LoadSnapshotAsync(
        AnalyticsAdvancedQuery query,
        CancellationToken cancellationToken);
}
