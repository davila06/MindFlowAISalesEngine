using System;
using System.Collections.Generic;

namespace Api.Domain.Workflows;

public class WorkflowDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<WorkflowStep> Steps { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class WorkflowStep
{
    public string Type { get; set; } = string.Empty; // e.g., "action", "condition"
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<WorkflowStep> Children { get; set; } = new(); // For branches/conditions
}
