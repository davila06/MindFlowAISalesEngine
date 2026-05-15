using Api.Application.AnalyticsAdvanced;
using Api.Application.Observability;
using Api.Contracts.Analytics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;

namespace Api.Controllers;

[ApiController]
[Route("api/analytics/advanced")]
public class AnalyticsAdvancedController : ControllerBase
{
    private static readonly HashSet<string> AllowedGroupBy = new(StringComparer.OrdinalIgnoreCase)
    {
        "day",
        "week",
        "month"
    };

    private readonly IAnalyticsAdvancedService _service;
    private readonly IAnalyticsCsvExportService _csvExportService;
    private readonly IWeeklyAnalyticsReportService _weeklyAnalyticsReportService;
    private readonly IAnalyticsObservabilityService _observability;
    private readonly IObservabilitySnapshotRepository _snapshotRepository;
    private readonly IObservabilityPersistenceService _persistenceService;
    private readonly IObservabilityIncrementalAggregationService _incrementalAggregationService;

    public AnalyticsAdvancedController(
        IAnalyticsAdvancedService service,
        IAnalyticsCsvExportService csvExportService,
        IWeeklyAnalyticsReportService weeklyAnalyticsReportService,
        IAnalyticsObservabilityService observability,
        IObservabilitySnapshotRepository snapshotRepository,
        IObservabilityPersistenceService persistenceService,
        IObservabilityIncrementalAggregationService incrementalAggregationService)
    {
        _service = service;
        _csvExportService = csvExportService;
        _weeklyAnalyticsReportService = weeklyAnalyticsReportService;
        _observability = observability;
        _snapshotRepository = snapshotRepository;
        _persistenceService = persistenceService;
        _incrementalAggregationService = incrementalAggregationService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("overview", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("overview", async () =>
        {
            var response = await _service.GetOverviewAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("overview/csv")]
    public async Task<IActionResult> GetOverviewCsv([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("overview-csv", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("overview-csv", async () =>
        {
            var response = await _service.GetOverviewAsync(query, cancellationToken);
            var csv = _csvExportService.ExportAdvancedOverviewCsv(response);
            var fileName = $"analytics-advanced-overview-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        });
    }

    [HttpPost("reports/weekly/run")]
    public async Task<IActionResult> RunWeeklyReport(CancellationToken cancellationToken)
    {
        return await ExecuteTrackedAsync("weekly-report-run", async () =>
        {
            var response = await _weeklyAnalyticsReportService.GenerateAsync(cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("funnel")]
    public async Task<IActionResult> GetFunnel([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("funnel", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("funnel", async () =>
        {
            var response = await _service.GetFunnelAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("revenue", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("revenue", async () =>
        {
            var response = await _service.GetRevenueAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("velocity")]
    public async Task<IActionResult> GetVelocity([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("velocity", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("velocity", async () =>
        {
            var response = await _service.GetVelocityAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("sla")]
    public async Task<IActionResult> GetSla([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("sla", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("sla", async () =>
        {
            var response = await _service.GetSlaAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("onboarding-activation")]
    public async Task<IActionResult> GetOnboardingActivation([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("onboarding-activation", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("onboarding-activation", async () =>
        {
            var response = await _service.GetOnboardingActivationAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("metrics/scope")]
    public async Task<IActionResult> GetScopeMetrics([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("metrics-scope", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("metrics-scope", async () =>
        {
            var response = await _service.GetScopeMetricsAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("comparisons/period-over-period")]
    public async Task<IActionResult> GetPeriodOverPeriod([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("period-over-period", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("period-over-period", async () =>
        {
            var response = await _service.GetPeriodOverPeriodAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("segments")]
    public async Task<IActionResult> GetSegmentation([FromQuery] AnalyticsAdvancedQuery query, CancellationToken cancellationToken)
    {
        var badRequest = ValidateQuery(query);
        if (badRequest is not null)
        {
            _observability.TrackError("segments", 0);
            return badRequest;
        }

        return await ExecuteTrackedAsync("segments", async () =>
        {
            var response = await _service.GetSegmentationAsync(query, cancellationToken);
            return Ok(response);
        });
    }

    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        var snapshot = _observability.GetSnapshot();
        return Ok(snapshot);
    }

    [HttpGet("metrics/history")]
    public async Task<IActionResult> GetMetricsHistory(
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] string? endpointName,
        CancellationToken cancellationToken)
    {
        var records = await _snapshotRepository.QueryAsync(startUtc, endUtc, endpointName, cancellationToken);
        var response = new ObservabilityHistoryResponse
        {
            Records = records
                .Select(r => new ObservabilityMetricRecordResponse
                {
                    Id = r.Id,
                    EndpointName = r.EndpointName,
                    RequestCount = r.RequestCount,
                    SuccessCount = r.SuccessCount,
                    ErrorCount = r.ErrorCount,
                    AverageLatencyMs = r.AverageLatencyMs,
                    RecordedAtUtc = r.RecordedAtUtc
                })
                .ToList()
        };
        return Ok(response);
    }

    [HttpPost("metrics/history/snapshot")]
    public async Task<IActionResult> FlushSnapshot(CancellationToken cancellationToken)
    {
        await _persistenceService.FlushAsync(cancellationToken);
        return Ok(new { flushed = true });
    }

    [HttpPost("metrics/history/aggregate-incremental")]
    public async Task<IActionResult> AggregateHistoryIncremental(
        [FromQuery] int windowMinutes = 60,
        [FromQuery] int batchSize = 500,
        CancellationToken cancellationToken = default)
    {
        var result = await _incrementalAggregationService.RunAsync(windowMinutes, batchSize, cancellationToken);
        return Ok(new ObservabilityIncrementalAggregationResponse
        {
            ProcessedRecords = result.ProcessedRecords,
            UpsertedBatches = result.UpsertedBatches,
            LastProcessedRecordedAtUtc = result.LastProcessedRecordedAtUtc
        });
    }

    [HttpGet("metrics/history/aggregates")]
    public async Task<IActionResult> GetHistoryAggregates(
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        [FromQuery] string? endpointName,
        [FromQuery] int windowMinutes = 60,
        [FromQuery] int top = 200,
        CancellationToken cancellationToken = default)
    {
        var batches = await _incrementalAggregationService.QueryBatchesAsync(
            startUtc,
            endUtc,
            endpointName,
            windowMinutes,
            top,
            cancellationToken);

        return Ok(new ObservabilityAggregateBatchListResponse
        {
            Items = batches.Select(x => new ObservabilityAggregateBatchResponse
            {
                EndpointName = x.EndpointName,
                WindowStartUtc = x.WindowStartUtc,
                WindowEndUtc = x.WindowEndUtc,
                IncrementalRequestCount = x.IncrementalRequestCount,
                IncrementalSuccessCount = x.IncrementalSuccessCount,
                IncrementalErrorCount = x.IncrementalErrorCount,
                AverageLatencyMs = x.AverageLatencyMs,
                SampleCount = x.SampleCount,
                LastSourceRecordedAtUtc = x.LastSourceRecordedAtUtc
            }).ToList()
        });
    }

    private async Task<IActionResult> ExecuteTrackedAsync(string endpoint, Func<Task<IActionResult>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            _observability.TrackSuccess(endpoint, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch
        {
            _observability.TrackError(endpoint, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private BadRequestObjectResult? ValidateQuery(AnalyticsAdvancedQuery query)
    {
        var groupBy = string.IsNullOrWhiteSpace(query.GroupBy) ? "day" : query.GroupBy.Trim();
        if (!AllowedGroupBy.Contains(groupBy))
        {
            return BadRequest(new
            {
                error = "Invalid groupBy. Allowed values: day, week, month."
            });
        }

        if (query.StartDateUtc.HasValue && query.EndDateUtc.HasValue)
        {
            var start = query.StartDateUtc.Value.ToUniversalTime();
            var end = query.EndDateUtc.Value.ToUniversalTime();

            if (start > end)
            {
                return BadRequest(new
                {
                    error = "Invalid date range. StartDateUtc must be less than or equal to EndDateUtc."
                });
            }

            var maxWindowDays = 366;
            if ((end - start).TotalDays > maxWindowDays)
            {
                return BadRequest(new
                {
                    error = "Invalid date range. Maximum supported window is 366 days."
                });
            }
        }

        return null;
    }
}
