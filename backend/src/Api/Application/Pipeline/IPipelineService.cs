using Api.Contracts;

namespace Api.Application.Pipeline;

public interface IPipelineService
{
    Task<IReadOnlyList<PipelineStageResponse>> GetStagesAsync(CancellationToken cancellationToken);
    Task<PipelineBoardResponse> GetBoardAsync(PipelineBoardQueryRequest query, CancellationToken cancellationToken);
    Task<string> ExportBoardCsvAsync(PipelineBoardQueryRequest query, CancellationToken cancellationToken);
    Task<PipelineStageSlaAlertResponse> GetStageSlaAlertsAsync(int? defaultSlaHours, CancellationToken cancellationToken);
    Task<PipelineThroughputResponse> GetThroughputAsync(DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<PipelineWipLimitResponse>> GetWipLimitsAsync(CancellationToken cancellationToken);
    Task<PipelineWipLimitResponse> UpdateWipLimitAsync(Guid stageId, PipelineWipLimitUpdateRequest request, CancellationToken cancellationToken);
    Task<OpportunityResponse> CreateOpportunityAsync(OpportunityCreateRequest request, CancellationToken cancellationToken);
    Task<OpportunityResponse> MoveOpportunityStageAsync(Guid opportunityId, MoveOpportunityStageRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpportunityStageHistoryResponse>> GetOpportunityHistoryAsync(Guid opportunityId, CancellationToken cancellationToken);
}