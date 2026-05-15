using Api.Application.Dashboard;
using Api.Application.AnalyticsAdvanced;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IAnalyticsCsvExportService _csvExportService;

    public DashboardController(
        IDashboardService dashboardService,
        IAnalyticsCsvExportService csvExportService)
    {
        _dashboardService = dashboardService;
        _csvExportService = csvExportService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] int days = 7, CancellationToken cancellationToken = default)
    {
        var response = await _dashboardService.GetOverviewAsync(days, cancellationToken);
        return Ok(response);
    }

    [HttpGet("overview/csv")]
    public async Task<IActionResult> GetOverviewCsv([FromQuery] int days = 7, CancellationToken cancellationToken = default)
    {
        var response = await _dashboardService.GetOverviewAsync(days, cancellationToken);
        var csv = _csvExportService.ExportDashboardOverviewCsv(response);
        var fileName = $"dashboard-overview-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    [HttpGet("data-quality")]
    public async Task<IActionResult> GetDataQualityOverview(CancellationToken cancellationToken = default)
    {
        var response = await _dashboardService.GetDataQualityOverviewAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("data-quality/anomalies")]
    public async Task<IActionResult> GetDataQualityAnomalies(
        [FromQuery] string? eventType,
        [FromQuery] DateTime? startUtc,
        [FromQuery] DateTime? endUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await _dashboardService.GetDataAnomalyEventsAsync(eventType, startUtc, endUtc, cancellationToken);
        return Ok(response);
    }

    /// <summary>QA-17: Automated weekly quality health report endpoint.</summary>
    [HttpGet("qa-health-report")]
    public async Task<IActionResult> GetQaHealthReport(
        [FromQuery] int windowDays = 7,
        CancellationToken cancellationToken = default)
    {
        var report = await _dashboardService.GetQaHealthReportAsync(windowDays, cancellationToken);
        return Ok(report);
    }
}
