using Api.Domain.Assignment;
using Api.Domain.Companies;
using Api.Domain.Leads;
using Api.Domain.Onboarding;
using Api.Domain.Pipeline;

namespace Api.Application.AnalyticsAdvanced;

public sealed class AnalyticsAdvancedDataSnapshot
{
    public IReadOnlyList<Lead> Leads { get; init; } = [];
    public IReadOnlyList<Opportunity> Opportunities { get; init; } = [];
    public IReadOnlyList<OpportunityStageHistory> StageHistory { get; init; } = [];
    public IReadOnlyList<LeadAssignment> Assignments { get; init; } = [];
    public IReadOnlyList<AssignmentUser> AssignmentUsers { get; init; } = [];
    public IReadOnlyList<Company> Companies { get; init; } = [];
    public IReadOnlyList<Customer> Customers { get; init; } = [];
}
