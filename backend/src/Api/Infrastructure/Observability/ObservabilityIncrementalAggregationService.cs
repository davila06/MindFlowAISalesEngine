using Api.Application.Observability;
using Api.Domain.Observability;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Observability;

public sealed class ObservabilityIncrementalAggregationService : IObservabilityIncrementalAggregationService
{
    private readonly LeadsDbContext _dbContext;

    public ObservabilityIncrementalAggregationService(LeadsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ObservabilityIncrementalAggregationResult> RunAsync(
        int windowMinutes,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var safeWindowMinutes = Math.Clamp(windowMinutes, 5, 1440);
        var safeBatchSize = Math.Clamp(batchSize, 50, 2000);
        var checkpointKey = $"observability:{safeWindowMinutes}";

        var checkpoint = await _dbContext.ObservabilityAggregationCheckpoints
            .FirstOrDefaultAsync(x => x.Key == checkpointKey, cancellationToken);

        checkpoint ??= new ObservabilityAggregationCheckpoint(checkpointKey);

        var records = await QueryNextRecordsAsync(
            checkpoint.LastProcessedRecordedAtUtc,
            checkpoint.LastProcessedRecordId,
            safeBatchSize,
            cancellationToken);

        if (records.Count == 0)
        {
            if (_dbContext.Entry(checkpoint).State == EntityState.Detached)
            {
                _dbContext.ObservabilityAggregationCheckpoints.Add(checkpoint);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return new ObservabilityIncrementalAggregationResult
            {
                ProcessedRecords = 0,
                UpsertedBatches = 0,
                LastProcessedRecordedAtUtc = checkpoint.LastProcessedRecordedAtUtc
            };
        }

        var endpointNames = records.Select(x => x.EndpointName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var states = await _dbContext.ObservabilityEndpointAggregationStates
            .Where(x => endpointNames.Contains(x.EndpointName))
            .ToDictionaryAsync(x => x.EndpointName, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var stagedBatchAccumulators = new Dictionary<string, ObservabilityAggregateBatch>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in records)
        {
            if (!states.TryGetValue(record.EndpointName, out var state))
            {
                state = new ObservabilityEndpointAggregationState(record.EndpointName);
                states[record.EndpointName] = state;
                _dbContext.ObservabilityEndpointAggregationStates.Add(state);
            }

            var requestDelta = Math.Max(record.RequestCount - state.LastRequestCount, 0);
            var successDelta = Math.Max(record.SuccessCount - state.LastSuccessCount, 0);
            var errorDelta = Math.Max(record.ErrorCount - state.LastErrorCount, 0);

            var windowStart = AlignToWindow(record.RecordedAtUtc, safeWindowMinutes);
            var windowEnd = windowStart.AddMinutes(safeWindowMinutes);
            var batchKey = BuildBatchKey(record.EndpointName, windowStart, windowEnd);

            if (!stagedBatchAccumulators.TryGetValue(batchKey, out var stagedBatch))
            {
                stagedBatch = await _dbContext.ObservabilityAggregateBatches
                    .FirstOrDefaultAsync(
                        x => x.EndpointName == record.EndpointName
                             && x.WindowStartUtc == windowStart
                             && x.WindowEndUtc == windowEnd,
                        cancellationToken)
                    ?? new ObservabilityAggregateBatch(record.EndpointName, windowStart, windowEnd, record.RecordedAtUtc);

                if (_dbContext.Entry(stagedBatch).State == EntityState.Detached)
                {
                    _dbContext.ObservabilityAggregateBatches.Add(stagedBatch);
                }

                stagedBatchAccumulators[batchKey] = stagedBatch;
            }

            stagedBatch.Accumulate(requestDelta, successDelta, errorDelta, record.AverageLatencyMs, record.RecordedAtUtc);
            state.Advance(record.RequestCount, record.SuccessCount, record.ErrorCount, record.RecordedAtUtc);
            checkpoint.Advance(record.RecordedAtUtc, record.Id);
        }

        if (_dbContext.Entry(checkpoint).State == EntityState.Detached)
        {
            _dbContext.ObservabilityAggregationCheckpoints.Add(checkpoint);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ObservabilityIncrementalAggregationResult
        {
            ProcessedRecords = records.Count,
            UpsertedBatches = stagedBatchAccumulators.Count,
            LastProcessedRecordedAtUtc = checkpoint.LastProcessedRecordedAtUtc
        };
    }

    public async Task<IReadOnlyList<ObservabilityAggregateBatchResult>> QueryBatchesAsync(
        DateTime? startUtc,
        DateTime? endUtc,
        string? endpointName,
        int windowMinutes,
        int top,
        CancellationToken cancellationToken = default)
    {
        var safeTop = Math.Clamp(top, 1, 500);
        var safeWindowMinutes = Math.Clamp(windowMinutes, 5, 1440);

        var query = _dbContext.ObservabilityAggregateBatches.AsNoTracking();

        if (startUtc.HasValue)
        {
            query = query.Where(x => x.WindowStartUtc >= startUtc.Value);
        }

        if (endUtc.HasValue)
        {
            query = query.Where(x => x.WindowEndUtc <= endUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(endpointName))
        {
            query = query.Where(x => x.EndpointName == endpointName);
        }

        return await query
            .OrderByDescending(x => x.WindowStartUtc)
            .Take(safeTop)
            .Select(x => new ObservabilityAggregateBatchResult
            {
                EndpointName = x.EndpointName,
                WindowStartUtc = x.WindowStartUtc,
                WindowEndUtc = x.WindowEndUtc,
                IncrementalRequestCount = x.IncrementalRequestCount,
                IncrementalSuccessCount = x.IncrementalSuccessCount,
                IncrementalErrorCount = x.IncrementalErrorCount,
                AverageLatencyMs = x.SampleCount == 0
                    ? 0m
                    : Math.Round(x.TotalLatencyMs / x.SampleCount, 2),
                SampleCount = x.SampleCount,
                LastSourceRecordedAtUtc = x.LastSourceRecordedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ObservabilityMetricRecord>> QueryNextRecordsAsync(
        DateTime? lastRecordedAtUtc,
        string? lastRecordId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ObservabilityMetricRecords.AsNoTracking();

        if (lastRecordedAtUtc.HasValue)
        {
            if (string.IsNullOrWhiteSpace(lastRecordId))
            {
                query = query.Where(x => x.RecordedAtUtc > lastRecordedAtUtc.Value);
            }
            else
            {
                query = query.Where(x => x.RecordedAtUtc > lastRecordedAtUtc.Value
                    || (x.RecordedAtUtc == lastRecordedAtUtc.Value && string.CompareOrdinal(x.Id, lastRecordId) > 0));
            }
        }

        return await query
            .OrderBy(x => x.RecordedAtUtc)
            .ThenBy(x => x.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    private static DateTime AlignToWindow(DateTime value, int windowMinutes)
    {
        var utc = value.ToUniversalTime();
        var ticks = TimeSpan.FromMinutes(windowMinutes).Ticks;
        var alignedTicks = utc.Ticks - (utc.Ticks % ticks);
        return new DateTime(alignedTicks, DateTimeKind.Utc);
    }

    private static string BuildBatchKey(string endpointName, DateTime windowStartUtc, DateTime windowEndUtc)
    {
        return $"{endpointName}|{windowStartUtc:O}|{windowEndUtc:O}";
    }
}
