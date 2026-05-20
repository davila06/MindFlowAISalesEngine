"use client";

import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/hooks/queries/queryKeys";
import { leadsService } from "@/services/leads.service";

export function useLeadActivitiesQuery(
  leadId: string,
  page: number,
  pageSize: number,
  type: string,
  enabled: boolean
) {
  return useQuery({
    queryKey: queryKeys.leads.activities(leadId, page, pageSize, type),
    queryFn: ({ signal }) =>
      leadsService.getActivities({ leadId, page, pageSize, type: type || undefined, signal }),
    enabled,
    placeholderData: (previousData) => previousData
  });
}
