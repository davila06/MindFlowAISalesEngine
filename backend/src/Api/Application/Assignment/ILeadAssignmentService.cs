using Api.Contracts;

namespace Api.Application.Assignment;

public interface ILeadAssignmentService
{
    Task<AssignmentUserResponse> CreateUserAsync(AssignmentUserCreateRequest request, CancellationToken cancellationToken);
    Task<AssignmentUserResponse?> UpdateUserAvailabilityAsync(Guid userId, bool isActive, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignmentUserResponse>> GetUsersAsync(CancellationToken cancellationToken);
    Task<AssignmentCapacityLoadResponse> GetCapacityLoadAsync(CancellationToken cancellationToken);
    Task<LeadAssignmentResponse?> AssignLeadAsync(Guid leadId, CancellationToken cancellationToken);
    Task<LeadAssignmentResponse?> AssignLeadManuallyAsync(Guid leadId, ManualLeadAssignmentRequest request, CancellationToken cancellationToken);
    Task<LeadAssignmentResponse?> GetLatestByLeadAsync(Guid leadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeadAssignmentResponse>> GetAssignmentsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignmentAuditEntryResponse>> GetAuditAsync(Guid? leadId, Guid? userId, int take, CancellationToken cancellationToken);
    Task<AssignmentFairnessResponse> GetFairnessAsync(CancellationToken cancellationToken);
}
