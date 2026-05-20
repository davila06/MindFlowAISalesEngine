namespace Api.Domain.CustomFields;

/// <summary>Tenant-specific custom field schema for a given entity type (Lead, Contact).</summary>
public class CustomFieldDefinition
{
    public static class FieldTypes
    {
        public const string Text    = "text";
        public const string Number  = "number";
        public const string Date    = "date";
        public const string Select  = "select";
        public const string Boolean = "boolean";
    }

    public static class EntityTypes
    {
        public const string Lead    = "Lead";
        public const string Contact = "Contact";
    }

    public Guid Id { get; private set; }
    /// <summary>Machine-readable slug key (unique per tenant).</summary>
    public string Key { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public string FieldType { get; private set; } = FieldTypes.Text;
    public string EntityType { get; private set; } = EntityTypes.Lead;
    /// <summary>Comma-separated allowed values (for select type).</summary>
    public string? Options { get; private set; }
    public bool IsRequired { get; private set; }
    public int Order { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private CustomFieldDefinition() { }

    public static CustomFieldDefinition Create(string key, string label, string fieldType, string entityType, string? options, bool isRequired, int order)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        if (!new[] { FieldTypes.Text, FieldTypes.Number, FieldTypes.Date, FieldTypes.Select, FieldTypes.Boolean }.Contains(fieldType))
            throw new ArgumentException($"Invalid field type '{fieldType}'.", nameof(fieldType));

        return new CustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            Key = key.Trim().ToLowerInvariant(),
            Label = label.Trim(),
            FieldType = fieldType,
            EntityType = entityType,
            Options = options?.Trim(),
            IsRequired = isRequired,
            Order = order,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(string label, string fieldType, string? options, bool isRequired, int order)
    {
        Label = label.Trim();
        FieldType = fieldType;
        Options = options?.Trim();
        IsRequired = isRequired;
        Order = order;
    }
}
