namespace Api.Application.Leads;

public interface ILeadIntakeFailureStore
{
    string Add(LeadIntakeFailedRecord record);
    bool TryGet(string failedRequestId, out LeadIntakeFailedRecord? record);
    IReadOnlyList<LeadIntakeFailedRecord> List(int take);
}
