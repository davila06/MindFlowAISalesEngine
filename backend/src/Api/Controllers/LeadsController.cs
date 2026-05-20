using Api.Application.Leads;
using Api.Application.Common.Interfaces;
using Api.Application.Common.Security;
using Api.Application.DataGovernance;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Api.Controllers;

[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> IdempotencyLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly ILeadIntakeService _leadIntakeService;
    private readonly ILeadQueryService _leadQueryService;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly ILeadAuditSnapshotRepository _leadAuditSnapshotRepository;
    private readonly ILeadIntakeFailureStore _leadIntakeFailureStore;
    private readonly ITenantDataGovernanceStore _tenantDataGovernanceStore;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(
        ILeadIntakeService leadIntakeService,
        ILeadQueryService leadQueryService,
        IIdempotencyStore idempotencyStore,
        ILeadAuditSnapshotRepository leadAuditSnapshotRepository,
        ILeadIntakeFailureStore leadIntakeFailureStore,
        ITenantDataGovernanceStore tenantDataGovernanceStore,
        ITenantContext tenantContext,
        ILogger<LeadsController> logger)
    {
        _leadIntakeService = leadIntakeService;
        _leadQueryService = leadQueryService;
        _idempotencyStore = idempotencyStore;
        _leadAuditSnapshotRepository = leadAuditSnapshotRepository;
        _leadIntakeFailureStore = leadIntakeFailureStore;
        _tenantDataGovernanceStore = tenantDataGovernanceStore;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Search / list leads with optional custom-field filters and sort.
    /// Custom-field filters: append query params as ?cfFilter[key]=value
    /// Custom-field sort: ?cfSort=fieldKey&amp;cfSortDir=asc|desc
    /// Core sort: ?sortBy=createdAt|score|email|source&amp;sortDir=asc|desc
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery(Name = "cfFilter")] Dictionary<string, string>? cfFilter = null,
        [FromQuery] string? cfSort = null,
        [FromQuery] string cfSortDir = "desc",
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] string sortDir = "desc",
        CancellationToken cancellationToken = default)
    {
        var filters = (IReadOnlyDictionary<string, string>)(cfFilter ?? new Dictionary<string, string>());
        var result = await _leadQueryService.SearchAsync(page, pageSize, filters, cfSort, cfSortDir, sortBy, sortDir, cancellationToken);
        return Ok(result);
    }

    [HttpPost("intake")]
    public async Task<IActionResult> Intake([FromBody] LeadIntakeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = _tenantContext.TenantId;
            var idempotencyKey = Request.Headers[IdempotencyHeader].ToString();
            var scope = $"{tenantId}:lead-intake";

            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var response = await _leadIntakeService.IntakeAsync(request, cancellationToken);
                return CreatedAtAction(
                    nameof(Intake),
                    new { id = response.Id },
                    response);
            }

            var lockKey = $"{scope}:{idempotencyKey}";
            var gate = IdempotencyLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (_idempotencyStore.TryGet<LeadIntakeResponse>(scope, idempotencyKey, out var cachedResponse)
                    && cachedResponse is not null)
                {
                    Response.Headers["X-Idempotent-Replay"] = "true";
                    return CreatedAtAction(
                        nameof(Intake),
                        new { id = cachedResponse.Id },
                        cachedResponse);
                }

                var response = await _leadIntakeService.IntakeAsync(request, cancellationToken);
                _idempotencyStore.Set(scope, idempotencyKey, response);

                return CreatedAtAction(
                    nameof(Intake),
                    new { id = response.Id },
                    response);
            }
            finally
            {
                gate.Release();
            }
        }
        catch (LeadIntakeValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(ex.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Lead intake validation failed."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during lead intake.");
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unexpected error during lead intake.");
        }
    }

    [HttpPost("intake/bulk")]
    public async Task<IActionResult> IntakeBulk([FromBody] LeadIntakeBulkRequest request, CancellationToken cancellationToken)
    {
        var accepted = new List<LeadIntakeBulkSuccessItem>();
        var rejected = new List<LeadIntakeBulkFailureItem>();

        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            try
            {
                var created = await _leadIntakeService.IntakeAsync(item, cancellationToken);
                accepted.Add(new LeadIntakeBulkSuccessItem
                {
                    Index = index,
                    Lead = created
                });
            }
            catch (LeadIntakeValidationException ex)
            {
                var message = string.Join("; ", ex.Errors.SelectMany(x => x.Value));
                var failedRequestId = _leadIntakeFailureStore.Add(new LeadIntakeFailedRecord
                {
                    Request = item,
                    Code = DomainErrorCodes.ValidationError,
                    Message = message,
                    FailedAtUtc = DateTime.UtcNow
                });

                rejected.Add(new LeadIntakeBulkFailureItem
                {
                    Index = index,
                    Code = DomainErrorCodes.ValidationError,
                    Message = message,
                    FailedRequestId = failedRequestId
                });
            }
        }

        return Ok(new LeadIntakeBulkResponse
        {
            Accepted = accepted,
            Rejected = rejected
        });
    }

    [HttpGet("intake/failed")]
    public IActionResult ListFailedIntakeRequests([FromQuery] int take = 100)
    {
        var items = _leadIntakeFailureStore.List(take)
            .Select(x => new LeadIntakeFailedRecordResponse
            {
                FailedRequestId = x.FailedRequestId,
                Request = x.Request,
                Code = x.Code,
                Message = x.Message,
                FailedAtUtc = x.FailedAtUtc
            })
            .ToList();

        return Ok(items);
    }

    [HttpPost("intake/failed/{failedRequestId}/reprocess")]
    public async Task<IActionResult> ReprocessFailed(string failedRequestId, CancellationToken cancellationToken)
    {
        if (!_leadIntakeFailureStore.TryGet(failedRequestId, out var record) || record is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Failed intake request not found."
            });
        }

        try
        {
            var response = await _leadIntakeService.IntakeAsync(record.Request, cancellationToken);
            return Ok(response);
        }
        catch (LeadIntakeValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(ex.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Lead intake validation failed."
            });
        }
    }

    [HttpGet("intake/rejection-reasons")]
    public IActionResult GetRejectionReasons()
    {
        return Ok(new[]
        {
            new LeadIntakeRejectionReasonResponse { Code = DomainErrorCodes.ValidationError, Description = "Payload validation failed." },
            new LeadIntakeRejectionReasonResponse { Code = DomainErrorCodes.LeadDuplicate, Description = "A duplicated lead was detected." },
            new LeadIntakeRejectionReasonResponse { Code = DomainErrorCodes.LeadInvalidCountry, Description = "Country is invalid and must be ISO-3166 alpha-2." }
        });
    }

    [HttpGet("intake/dedup-settings")]
    public IActionResult GetTenantDedupSettings()
    {
        var settings = _tenantDataGovernanceStore.GetOrDefault(_tenantContext.TenantId, new DataGovernanceOptions());
        return Ok(new TenantDeduplicationSettingsResponse
        {
            EnforceDuplicateRejection = settings.EnforceDuplicateRejection,
            DedupEmailDistanceThreshold = settings.DedupEmailDistanceThreshold,
            DedupPhoneSuffixLength = settings.DedupPhoneSuffixLength
        });
    }

    [HttpPut("intake/dedup-settings")]
    public IActionResult UpdateTenantDedupSettings([FromBody] TenantDeduplicationSettingsUpdateRequest request)
    {
        var updated = _tenantDataGovernanceStore.Set(_tenantContext.TenantId, new DataGovernanceOptions
        {
            EnforceDuplicateRejection = request.EnforceDuplicateRejection,
            DedupEmailDistanceThreshold = request.DedupEmailDistanceThreshold,
            DedupPhoneSuffixLength = request.DedupPhoneSuffixLength
        });

        return Ok(new TenantDeduplicationSettingsResponse
        {
            EnforceDuplicateRejection = updated.EnforceDuplicateRejection,
            DedupEmailDistanceThreshold = updated.DedupEmailDistanceThreshold,
            DedupPhoneSuffixLength = updated.DedupPhoneSuffixLength
        });
    }

    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] MergeLeadsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var merged = await _leadIntakeService.MergeAsync(request, cancellationToken);
            return Ok(merged);
        }
        catch (LeadIntakeValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(ex.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Lead merge validation failed."
            });
        }
    }

    [HttpGet("{id:guid}/audits")]
    public async Task<IActionResult> GetAudits(Guid id, CancellationToken cancellationToken)
    {
        var snapshots = await _leadAuditSnapshotRepository.ListByLeadAsync(id, cancellationToken);
        var response = snapshots.Select(x => new LeadAuditSnapshotResponse
        {
            Id = x.Id,
            LeadId = x.LeadId,
            EventType = x.EventType,
            Actor = x.Actor,
            PayloadJson = PiiMasking.MaskJsonPayload(x.PayloadJson),
            CreatedAtUtc = x.CreatedAtUtc
        });

        return Ok(response);
    }
}
