import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/services/apiClient";

export interface OperationsKpi {
  deploymentFrequency: number;
  changeFailureRate: number;
  mttrHours: number;
  backgroundJobFailures: number;
  emailDeliverySuccessRate: number;
}

export function useOperationsKpiQuery(days: number) {
  return useQuery({
    queryKey: ["dashboard", "operations-kpis", days],
    queryFn: ({ signal }) =>
      apiClient.get<OperationsKpi>(`/api/dashboard/operations-kpis?days=${days}`, { signal })
  });
}
