namespace Api.Contracts;

public class AssignmentFairnessResponse
{
    public int TotalAssignments { get; init; }
    public decimal AverageAssignmentsPerUser { get; init; }
    public int MaxAssignmentsBySingleUser { get; init; }
    public int MinAssignmentsBySingleUser { get; init; }
    public decimal StandardDeviation { get; init; }
    public bool HasImbalanceRisk { get; init; }
    public IReadOnlyList<AssignmentUserDistributionItem> Distribution { get; init; } = [];
}

public class AssignmentUserDistributionItem
{
    public Guid UserId { get; init; }
    public int AssignedLeads { get; init; }
    public decimal SharePercent { get; init; }
}
