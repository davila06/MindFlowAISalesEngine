namespace Api.Application.AnalyticsAdvanced;

public interface IAnalyticsObservabilityService
{
    void TrackSuccess(string endpoint, long latencyMs);
    void TrackError(string endpoint, long latencyMs);
    AnalyticsObservabilitySnapshot GetSnapshot();
}
