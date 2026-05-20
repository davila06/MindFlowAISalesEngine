using Api.Domain.CustomFields;

namespace Api.Application.CustomFields;

public class CustomFieldService : ICustomFieldService
{
    private readonly ICustomFieldRepository _repository;

    public CustomFieldService(ICustomFieldRepository repository) => _repository = repository;

    public async Task<CustomFieldDefinition> CreateDefinitionAsync(
        string key, string label, string fieldType, string entityType,
        string? options, bool isRequired, int order, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetDefinitionByKeyAsync(key, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A custom field with key '{key}' already exists.");

        var def = CustomFieldDefinition.Create(key, label, fieldType, entityType, options, isRequired, order);
        await _repository.AddDefinitionAsync(def, cancellationToken);
        return def;
    }

    public async Task<CustomFieldDefinition> UpdateDefinitionAsync(
        Guid id, string label, string fieldType, string? options,
        bool isRequired, int order, CancellationToken cancellationToken)
    {
        var definitions = await _repository.GetDefinitionsAsync(null, cancellationToken);
        var def = definitions.FirstOrDefault(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Custom field definition {id} not found.");

        def.Update(label, fieldType, options, isRequired, order);
        await _repository.UpdateDefinitionAsync(def, cancellationToken);
        return def;
    }

    public Task DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken) =>
        _repository.DeleteDefinitionAsync(id, cancellationToken);

    public async Task SetFieldValueAsync(Guid entityId, string entityType, string fieldKey, string? value, CancellationToken cancellationToken)
    {
        var def = await _repository.GetDefinitionByKeyAsync(fieldKey, cancellationToken)
            ?? throw new KeyNotFoundException($"Custom field '{fieldKey}' is not defined.");

        ValidateValue(def, value);
        await _repository.UpsertValueAsync(entityId, entityType, fieldKey, value, cancellationToken);
    }

    private static void ValidateValue(CustomFieldDefinition def, string? value)
    {
        if (def.IsRequired && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Field '{def.Key}' is required.");

        if (string.IsNullOrWhiteSpace(value)) return;

        switch (def.FieldType)
        {
            case CustomFieldDefinition.FieldTypes.Number:
                if (!decimal.TryParse(value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                    throw new ArgumentException($"Field '{def.Key}' expects a number.");
                break;

            case CustomFieldDefinition.FieldTypes.Date:
                if (!DateTime.TryParse(value, out _))
                    throw new ArgumentException($"Field '{def.Key}' expects a valid date.");
                break;

            case CustomFieldDefinition.FieldTypes.Boolean:
                if (!bool.TryParse(value, out _))
                    throw new ArgumentException($"Field '{def.Key}' expects true or false.");
                break;

            case CustomFieldDefinition.FieldTypes.Select:
                var allowed = (def.Options ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (allowed.Length > 0 && !allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException($"Field '{def.Key}' value must be one of: {string.Join(", ", allowed)}.");
                break;
        }
    }
}
