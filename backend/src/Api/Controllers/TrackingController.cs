using Api.Application.Email;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Public tracking endpoints — no tenant/auth guard (pixels are loaded by email clients).
/// Security: tokens are unguessable Guids; no PII in response; rate-limiting at infra level.
/// Apple MPP detection: user-agent contains "Apple" + "Mail" heuristic.
/// </summary>
[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    // 1x1 transparent GIF bytes (minimal, no metadata)
    private static readonly byte[] TrackingPixel = Convert.FromBase64String(
        "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    private readonly IEmailLogRepository _emailLogRepository;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(IEmailLogRepository emailLogRepository, ILogger<TrackingController> logger)
    {
        _emailLogRepository = emailLogRepository;
        _logger = logger;
    }

    /// <summary>GET /api/tracking/pixel/{token}.gif — returns 1×1 transparent GIF and records open.</summary>
    [HttpGet("pixel/{token}.gif")]
    [ResponseCache(Duration = 0, NoStore = true)]
    public async Task<IActionResult> TrackOpen(Guid token, CancellationToken cancellationToken)
    {
        var log = await _emailLogRepository.GetByTrackingTokenAsync(token, cancellationToken);
        if (log is not null)
        {
            var ua = Request.Headers.UserAgent.ToString();
            var isAppleMpp = ua.Contains("Apple", StringComparison.OrdinalIgnoreCase)
                          && ua.Contains("Mail", StringComparison.OrdinalIgnoreCase);
            log.RecordOpen(isAppleMpp);
            await _emailLogRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _logger.LogDebug("Tracking pixel hit for unknown token {Token}", token);
        }

        return File(TrackingPixel, "image/gif");
    }

    /// <summary>GET /api/tracking/click/{token}?url= — records click and redirects to destination.</summary>
    [HttpGet("click/{token}")]
    public async Task<IActionResult> TrackClick(Guid token, [FromQuery] string url, CancellationToken cancellationToken)
    {
        // Validate redirect target to prevent open-redirect abuse
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var destination)
            || (destination.Scheme != Uri.UriSchemeHttp && destination.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest("Invalid redirect url.");
        }

        var log = await _emailLogRepository.GetByTrackingTokenAsync(token, cancellationToken);
        if (log is not null)
        {
            log.RecordClick();
            await _emailLogRepository.SaveChangesAsync(cancellationToken);
        }

        return Redirect(destination.AbsoluteUri);
    }
}
