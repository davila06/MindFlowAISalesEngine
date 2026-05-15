using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Api.Application.AnalyticsAdvanced;

namespace Api.Infrastructure.AnalyticsAdvanced;

public sealed class InMemoryAnalyticsObservabilityService : IAnalyticsObservabilityService
{
    private const string OverflowEndpoint = "__overflow__";
    private static readonly Regex GuidSegmentRegex = new(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
    private static readonly Regex NumericSegmentRegex = new(@"^[0-9]{4,}$", RegexOptions.Compiled);
    private static readonly Regex LongTokenSegmentRegex = new(@"^[A-Za-z0-9_-]{16,}$", RegexOptions.Compiled);

    private sealed class EndpointCounters
    {
        public long RequestCount;
        public long SuccessCount;
        public long ErrorCount;
        public long TotalLatencyMs;
    }

    private readonly int _maxDistinctEndpoints;
    private int _droppedDistinctEndpointCount;
    private readonly ConcurrentDictionary<string, EndpointCounters> _metrics =
        new(StringComparer.OrdinalIgnoreCase);

    public InMemoryAnalyticsObservabilityService(int maxDistinctEndpoints = 200)
    {
        _maxDistinctEndpoints = Math.Clamp(maxDistinctEndpoints, 1, 5000);
    }

    public void TrackSuccess(string endpoint, long latencyMs)
    {
        var counters = GetCounters(endpoint);
        Interlocked.Increment(ref counters.RequestCount);
        Interlocked.Increment(ref counters.SuccessCount);
        Interlocked.Add(ref counters.TotalLatencyMs, Math.Max(latencyMs, 0));
    }

    public void TrackError(string endpoint, long latencyMs)
    {
        var counters = GetCounters(endpoint);
        Interlocked.Increment(ref counters.RequestCount);
        Interlocked.Increment(ref counters.ErrorCount);
        Interlocked.Add(ref counters.TotalLatencyMs, Math.Max(latencyMs, 0));
    }

    public AnalyticsObservabilitySnapshot GetSnapshot()
    {
        var endpoints = _metrics
            .OrderBy(x => x.Key)
            .Select(x =>
            {
                var count = Volatile.Read(ref x.Value.RequestCount);
                var totalLatency = Volatile.Read(ref x.Value.TotalLatencyMs);
                var average = count == 0
                    ? 0m
                    : Math.Round((decimal)totalLatency / count, 2);

                return new AnalyticsEndpointMetricSnapshot
                {
                    Endpoint = x.Key,
                    RequestCount = count,
                    SuccessCount = Volatile.Read(ref x.Value.SuccessCount),
                    ErrorCount = Volatile.Read(ref x.Value.ErrorCount),
                    AverageLatencyMs = average
                };
            })
            .ToList();

        return new AnalyticsObservabilitySnapshot
        {
            GeneratedAtUtc = DateTime.UtcNow,
            Endpoints = endpoints,
            Cardinality = new AnalyticsObservabilityCardinalitySnapshot
            {
                DistinctEndpoints = _metrics.Keys.Count(k => !string.Equals(k, OverflowEndpoint, StringComparison.OrdinalIgnoreCase)),
                MaxDistinctEndpoints = _maxDistinctEndpoints,
                DroppedDistinctEndpointCount = Volatile.Read(ref _droppedDistinctEndpointCount)
            }
        };
    }

    private EndpointCounters GetCounters(string endpoint)
    {
        var normalizedEndpoint = NormalizeEndpoint(endpoint);
        if (_metrics.TryGetValue(normalizedEndpoint, out var existing))
        {
            return existing;
        }

        if (!string.Equals(normalizedEndpoint, OverflowEndpoint, StringComparison.OrdinalIgnoreCase)
            && !_metrics.ContainsKey(normalizedEndpoint)
            && _metrics.Keys.Count(k => !string.Equals(k, OverflowEndpoint, StringComparison.OrdinalIgnoreCase)) >= _maxDistinctEndpoints)
        {
            Interlocked.Increment(ref _droppedDistinctEndpointCount);
            normalizedEndpoint = OverflowEndpoint;
        }

        return _metrics.GetOrAdd(normalizedEndpoint, _ => new EndpointCounters());
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return "unknown";
        }

        var sanitized = endpoint.Trim();
        var querySeparator = sanitized.IndexOf('?');
        if (querySeparator >= 0)
        {
            sanitized = sanitized[..querySeparator];
        }

        if (!sanitized.Contains('/'))
        {
            return sanitized.ToLowerInvariant();
        }

        var segments = sanitized
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToLowerInvariant())
            .ToArray();

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (GuidSegmentRegex.IsMatch(segment)
                || NumericSegmentRegex.IsMatch(segment)
                || LongTokenSegmentRegex.IsMatch(segment))
            {
                segments[i] = "{id}";
            }
        }

        return segments.Length == 0
            ? "unknown"
            : "/" + string.Join('/', segments);
    }
}
