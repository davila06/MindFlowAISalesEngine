using Api.Application.Workflows;
using Api.Domain.Workflows;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Controllers;

[ApiController]
[Route("api/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly WorkflowDefinitionService _service;

    public WorkflowsController(WorkflowDefinitionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await _service.ListAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var wf = await _service.GetByIdAsync(id, cancellationToken);
        return wf is null ? NotFound() : Ok(wf);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WorkflowDefinition workflow, CancellationToken cancellationToken)
    {
        workflow.Id = Guid.NewGuid();
        workflow.CreatedAtUtc = DateTime.UtcNow;
        await _service.AddAsync(workflow, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = workflow.Id }, workflow);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] WorkflowDefinition workflow, CancellationToken cancellationToken)
    {
        workflow.Id = id;
        workflow.UpdatedAtUtc = DateTime.UtcNow;
        await _service.UpdateAsync(workflow, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
