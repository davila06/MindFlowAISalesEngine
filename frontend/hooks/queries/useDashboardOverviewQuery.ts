"use client";

import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/hooks/queries/queryKeys";
import { apiClient } from "@/services/apiClient";

export interface DashboardOverview {
  totalLeads: number;
  conversionRate: number;
  pipelineValue: number;
  leadsPerDay: Array<{ date: string; count: number }>;
}

export function useDashboardOverviewQuery(days: number) {
  return useQuery({
    queryKey: queryKeys.dashboard.overview(days),
    queryFn: ({ signal }) =>
      apiClient.get<DashboardOverview>(`/api/dashboard/overview?days=${days}`, { signal })
  });
}
