using Api.Domain.CustomFields;

namespace Api.Application.CustomFields;

public interface ICustomFieldRepository
{
    // Definitions
    Task<CustomFieldDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken cancellationToken);
    Task<IReadOnlyList<CustomFieldDefinition>> GetDefinitionsAsync(string? entityType, CancellationToken cancellationToken);
    Task AddDefinitionAsync(CustomFieldDefinition definition, CancellationToken cancellationToken);
    Task UpdateDefinitionAsync(CustomFieldDefinition definition, CancellationToken cancellationToken);
    Task DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken);

    // Values
    Task<IReadOnlyList<CustomFieldValue>> GetValuesAsync(Guid entityId, string entityType, CancellationToken cancellationToken);
    Task UpsertValueAsync(Guid entityId, string entityType, string fieldKey, string? value, CancellationToken cancellationToken);
}
