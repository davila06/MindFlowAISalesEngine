using Api.Application.Sequences;
using Api.Contracts;
using Api.Domain.Sequences;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/sequences")]
public class SequencesController : ControllerBase
{
    private readonly ISequenceService _sequenceService;
    private readonly ISequenceRepository _sequenceRepository;
    private readonly ISequenceEnrollmentRepository _enrollmentRepository;

    public SequencesController(
        ISequenceService sequenceService,
        ISequenceRepository sequenceRepository,
        ISequenceEnrollmentRepository enrollmentRepository)
    {
        _sequenceService = sequenceService;
        _sequenceRepository = sequenceRepository;
        _enrollmentRepository = enrollmentRepository;
    }

    /// <summary>GET all sequences.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var sequences = await _sequenceRepository.GetAllAsync(cancellationToken);
        return Ok(sequences.Select(Map));
    }

    /// <summary>GET a single sequence by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sequence = await _sequenceRepository.GetByIdAsync(id, cancellationToken);
        if (sequence is null) return NotFound();
        return Ok(Map(sequence));
    }

    /// <summary>POST create a sequence.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSequenceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var steps = request.Steps?.Select(s => new SequenceStepInput(s.Order, s.ActionType, s.ActionValue, s.DelayDays))
            ?? Enumerable.Empty<SequenceStepInput>();

        var sequence = await _sequenceService.CreateAsync(request.Name, request.Description, steps, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sequence.Id }, Map(sequence));
    }

    /// <summary>PUT update a sequence.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSequenceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        try
        {
            var steps = request.Steps?.Select(s => new SequenceStepInput(s.Order, s.ActionType, s.ActionValue, s.DelayDays))
                ?? Enumerable.Empty<SequenceStepInput>();

            var sequence = await _sequenceService.UpdateAsync(id, request.Name, request.Description, request.IsActive, steps, cancellationToken);
            return Ok(Map(sequence));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>DELETE a sequence.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sequenceService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>POST enroll a lead in a sequence.</summary>
    [HttpPost("{id:guid}/enroll")]
    public async Task<IActionResult> Enroll(Guid id, [FromBody] EnrollLeadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var enrollment = await _sequenceService.EnrollLeadAsync(request.LeadId, id, cancellationToken);
            return CreatedAtAction(nameof(GetEnrollmentsByLead), new { leadId = request.LeadId }, MapEnrollment(enrollment));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>GET active enrollments for a lead.</summary>
    [HttpGet("enrollments/lead/{leadId:guid}")]
    public async Task<IActionResult> GetEnrollmentsByLead(Guid leadId, CancellationToken cancellationToken)
    {
        var enrollments = await _enrollmentRepository.GetActiveByLeadAsync(leadId, cancellationToken);
        return Ok(enrollments.Select(MapEnrollment));
    }

    /// <summary>DELETE (exit) an enrollment manually.</summary>
    [HttpDelete("enrollments/{enrollmentId:guid}")]
    public async Task<IActionResult> Unenroll(Guid enrollmentId, CancellationToken cancellationToken)
    {
        try
        {
            await _sequenceService.UnenrollLeadAsync(enrollmentId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private static SequenceResponse Map(Sequence s) => new(
        s.Id, s.Name, s.Description, s.IsActive, s.CreatedAtUtc, s.UpdatedAtUtc,
        s.Steps.OrderBy(st => st.Order)
               .Select(st => new SequenceStepResponse(st.Id, st.Order, st.ActionType, st.ActionValue, st.DelayDays))
               .ToList());

    private static SequenceEnrollmentResponse MapEnrollment(SequenceEnrollment e) => new(
        e.Id, e.LeadId, e.SequenceId, e.Status, e.NextStepOrder, e.NextStepDueAtUtc,
        e.EnrolledAtUtc, e.CompletedAtUtc, e.ExitedAtUtc, e.ExitReason);
}
