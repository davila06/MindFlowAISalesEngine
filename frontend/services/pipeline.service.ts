import { apiClient } from "@/services/apiClient";
import type { Opportunity, PipelineStage } from "@/types/lead";

export interface PipelineBoardResponse {
  stages: PipelineStage[];
  opportunities: Opportunity[];
}

export const pipelineService = {
  getBoard: (signal?: AbortSignal) =>
    apiClient.get<PipelineBoardResponse>("/api/pipeline/board", { signal }),
  getStages: () => apiClient.get<PipelineStage[]>("/api/pipeline/stages"),
  createOpportunity: (payload: {
    leadId: string;
    title: string;
    value: number;
    stageId: string;
  }) => apiClient.post<Opportunity>("/api/pipeline/opportunities", payload),
  moveOpportunity: (opportunityId: string, targetStageId: string) =>
    apiClient.patch<Opportunity>(
      `/api/pipeline/opportunities/${opportunityId}/stage`,
      {
        targetStageId,
        reason: "manual move from ui"
      }
    )
};
