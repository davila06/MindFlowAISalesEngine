using Api.Application.CustomFields;
using Api.Contracts;
using Api.Domain.CustomFields;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin/custom-fields")]
public class CustomFieldsController : ControllerBase
{
    private readonly ICustomFieldService _service;
    private readonly ICustomFieldRepository _repository;

    public CustomFieldsController(ICustomFieldService service, ICustomFieldRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    /// <summary>GET all field definitions (optionally filtered by entityType).</summary>
    [HttpGet]
    public async Task<IActionResult> GetDefinitions([FromQuery] string? entityType, CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetDefinitionsAsync(entityType, cancellationToken);
        return Ok(definitions.Select(Map));
    }

    /// <summary>POST create a field definition.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomFieldRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Label))
            return BadRequest(new { message = "Key and Label are required." });

        try
        {
            var def = await _service.CreateDefinitionAsync(
                request.Key, request.Label, request.FieldType, request.EntityType ?? "Lead",
                request.Options, request.IsRequired, request.Order, cancellationToken);
            return CreatedAtAction(nameof(GetDefinitions), Map(def));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PUT update a field definition.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomFieldRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
            return BadRequest(new { message = "Label is required." });

        try
        {
            var def = await _service.UpdateDefinitionAsync(
                id, request.Label, request.FieldType, request.Options,
                request.IsRequired, request.Order, cancellationToken);
            return Ok(Map(def));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>DELETE a field definition.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteDefinitionAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>GET custom field values for a specific lead (or entity).</summary>
    [HttpGet("values/lead/{leadId:guid}")]
    public async Task<IActionResult> GetLeadValues(Guid leadId, CancellationToken cancellationToken)
    {
        var values = await _repository.GetValuesAsync(leadId, CustomFieldDefinition.EntityTypes.Lead, cancellationToken);
        return Ok(values.Select(v => new CustomFieldValueResponse(v.FieldKey, v.Value, v.UpdatedAtUtc)));
    }

    /// <summary>PUT upsert a custom field value for a lead.</summary>
    [HttpPut("values/lead/{leadId:guid}/{fieldKey}")]
    public async Task<IActionResult> SetLeadValue(Guid leadId, string fieldKey, [FromBody] SetCustomFieldValueRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _service.SetFieldValueAsync(leadId, CustomFieldDefinition.EntityTypes.Lead, fieldKey, request.Value, cancellationToken);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static CustomFieldDefinitionResponse Map(CustomFieldDefinition d) =>
        new(d.Id, d.Key, d.Label, d.FieldType, d.EntityType, d.Options, d.IsRequired, d.Order, d.CreatedAtUtc);
}
