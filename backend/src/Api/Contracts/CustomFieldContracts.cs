namespace Api.Contracts;

// ---- Custom Fields ----

public record CreateCustomFieldRequest(
    string Key,
    string Label,
    string FieldType,
    string EntityType,
    string? Options,
    bool IsRequired,
    int Order);

public record UpdateCustomFieldRequest(
    string Label,
    string FieldType,
    string? Options,
    bool IsRequired,
    int Order);

public record SetCustomFieldValueRequest(string? Value);

public record CustomFieldDefinitionResponse(
    Guid Id,
    string Key,
    string Label,
    string FieldType,
    string EntityType,
    string? Options,
    bool IsRequired,
    int Order,
    DateTime CreatedAtUtc);

public record CustomFieldValueResponse(string Key, string? Value, DateTime UpdatedAtUtc);
