using Api.Domain.CustomFields;

namespace Api.Application.CustomFields;

public interface ICustomFieldService
{
    Task<CustomFieldDefinition> CreateDefinitionAsync(string key, string label, string fieldType, string entityType, string? options, bool isRequired, int order, CancellationToken cancellationToken);
    Task<CustomFieldDefinition> UpdateDefinitionAsync(Guid id, string label, string fieldType, string? options, bool isRequired, int order, CancellationToken cancellationToken);
    Task DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken);
    Task SetFieldValueAsync(Guid entityId, string entityType, string fieldKey, string? value, CancellationToken cancellationToken);
}
