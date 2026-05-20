namespace Api.Domain.CustomFields;

/// <summary>Persisted value of a custom field for a specific entity instance.</summary>
public class CustomFieldValue
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string FieldKey { get; private set; } = string.Empty;
    public string? Value { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private CustomFieldValue() { }

    public static CustomFieldValue Create(Guid entityId, string entityType, string fieldKey, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldKey);
        return new CustomFieldValue
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntityType = entityType,
            FieldKey = fieldKey.ToLowerInvariant(),
            Value = value,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void SetValue(string? value)
    {
        Value = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
