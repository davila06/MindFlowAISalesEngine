namespace Api.Application.Assignment;

public interface IAssignmentProtectionStore
{
    bool IsManualProtected(Guid leadId);
    void SetManualProtection(Guid leadId, bool enabled);
}
