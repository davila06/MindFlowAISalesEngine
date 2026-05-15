"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { queryKeys } from "@/hooks/queries/queryKeys";
import { rulesService } from "@/services/rules.service";
import type { Rule } from "@/types/rule";

export function useRulesQuery() {
  return useQuery({
    queryKey: queryKeys.rules.list,
    queryFn: ({ signal }) => rulesService.list(signal)
  });
}

export function useToggleRuleMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ ruleId, nextActive }: { ruleId: string; nextActive: boolean }) =>
      nextActive ? rulesService.activate(ruleId) : rulesService.deactivate(ruleId),
    onMutate: async ({ ruleId, nextActive }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.rules.list });
      const previous = queryClient.getQueryData<Rule[]>(queryKeys.rules.list);

      if (previous) {
        queryClient.setQueryData<Rule[]>(
          queryKeys.rules.list,
          previous.map((rule) =>
            rule.id === ruleId ? { ...rule, isActive: nextActive } : rule
          )
        );
      }

      return { previous };
    },
    onError: (_error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.rules.list, context.previous);
      }
    },
    onSettled: async () => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.rules.list });
    }
  });
}
