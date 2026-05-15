namespace Api.Contracts;

public class AssignmentCapacityLoadResponse
{
    public IReadOnlyList<AssignmentUserCapacityItem> Users { get; init; } = [];
}

public class AssignmentUserCapacityItem
{
    public Guid UserId { get; init; }
    public int CurrentLoad { get; init; }
    public int MaxActiveLeads { get; init; }
    public bool IsAtCapacity { get; init; }
}
