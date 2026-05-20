using Api.Application.CustomFields;
using Api.Domain.CustomFields;
using Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.CustomFields;

public class CustomFieldRepository : ICustomFieldRepository
{
    private readonly LeadsDbContext _db;
    public CustomFieldRepository(LeadsDbContext db) => _db = db;

    public Task<CustomFieldDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken cancellationToken) =>
        _db.CustomFieldDefinitions.FirstOrDefaultAsync(d => d.Key == key.ToLowerInvariant(), cancellationToken);

    public async Task<IReadOnlyList<CustomFieldDefinition>> GetDefinitionsAsync(string? entityType, CancellationToken cancellationToken)
    {
        var query = _db.CustomFieldDefinitions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(d => d.EntityType == entityType);
        return await query.OrderBy(d => d.Order).ThenBy(d => d.Key).ToListAsync(cancellationToken);
    }

    public async Task AddDefinitionAsync(CustomFieldDefinition definition, CancellationToken cancellationToken)
    {
        await _db.CustomFieldDefinitions.AddAsync(definition, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDefinitionAsync(CustomFieldDefinition definition, CancellationToken cancellationToken)
    {
        _db.CustomFieldDefinitions.Update(definition);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken)
    {
        var def = await _db.CustomFieldDefinitions.FindAsync([id], cancellationToken);
        if (def is not null)
        {
            _db.CustomFieldDefinitions.Remove(def);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<CustomFieldValue>> GetValuesAsync(Guid entityId, string entityType, CancellationToken cancellationToken) =>
        await _db.CustomFieldValues
            .Where(v => v.EntityId == entityId && v.EntityType == entityType)
            .ToListAsync(cancellationToken);

    public async Task UpsertValueAsync(Guid entityId, string entityType, string fieldKey, string? value, CancellationToken cancellationToken)
    {
        var existing = await _db.CustomFieldValues.FirstOrDefaultAsync(
            v => v.EntityId == entityId && v.EntityType == entityType && v.FieldKey == fieldKey.ToLowerInvariant(),
            cancellationToken);

        if (existing is not null)
        {
            existing.SetValue(value);
            _db.CustomFieldValues.Update(existing);
        }
        else
        {
            var newValue = CustomFieldValue.Create(entityId, entityType, fieldKey, value);
            await _db.CustomFieldValues.AddAsync(newValue, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
