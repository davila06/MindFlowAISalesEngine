"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/hooks/queries/queryKeys";
import { pipelineService } from "@/services/pipeline.service";
import type { Opportunity } from "@/types/lead";

export function usePipelineBoardQuery() {
  return useQuery({
    queryKey: queryKeys.pipeline.board,
    queryFn: ({ signal }) => pipelineService.getBoard(signal)
  });
}

export function useCreateOpportunityMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: pipelineService.createOpportunity,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.pipeline.board });
    }
  });
}

export function useMoveOpportunityMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ opportunityId, targetStageId }: { opportunityId: string; targetStageId: string }) =>
      pipelineService.moveOpportunity(opportunityId, targetStageId),
    onMutate: async ({ opportunityId, targetStageId }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.pipeline.board });
      const previous = queryClient.getQueryData<{
        stages: Array<{ id: string; name: string }>;
        opportunities: Opportunity[];
      }>(queryKeys.pipeline.board);

      if (previous) {
        queryClient.setQueryData(queryKeys.pipeline.board, {
          ...previous,
          opportunities: previous.opportunities.map((item) =>
            item.id === opportunityId ? { ...item, stageId: targetStageId } : item
          )
        });
      }

      return { previous };
    },
    onError: (_error, _input, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.pipeline.board, context.previous);
      }
    },
    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.pipeline.board });
    }
  });
}
