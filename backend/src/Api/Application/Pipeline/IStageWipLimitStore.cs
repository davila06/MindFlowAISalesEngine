namespace Api.Application.Pipeline;

public interface IStageWipLimitStore
{
    int GetLimit(string tenantId, Guid stageId, string stageName);
    IReadOnlyDictionary<Guid, int> GetAll(string tenantId, IReadOnlyDictionary<Guid, string> stageNames);
    int SetLimit(string tenantId, Guid stageId, int limit);
}
