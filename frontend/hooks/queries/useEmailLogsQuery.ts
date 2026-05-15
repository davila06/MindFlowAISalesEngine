"use client";

import { useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/hooks/queries/queryKeys";
import { emailService } from "@/services/email.service";

export function useEmailLogsQuery(page: number, pageSize: number, search: string) {
  return useQuery({
    queryKey: queryKeys.email.logs(page, pageSize, search),
    queryFn: ({ signal }) => emailService.getLogs({ page, pageSize, search, signal }),
    placeholderData: (previousData) => previousData
  });
}
