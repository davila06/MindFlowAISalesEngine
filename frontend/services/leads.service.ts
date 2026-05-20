import { apiClient } from "@/services/apiClient";
import type { Lead, LeadActivitiesPage } from "@/types/lead";
import type { LeadPage } from "@/types/sequences";

export interface IntakeLeadPayload {
  email?: string;
  phone?: string;
  source: string;
}

export interface LeadSearchParams {
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
  cfSort?: string;
  cfSortDir?: "asc" | "desc";
  cfFilter?: Record<string, string>;
}

export const leadsService = {
  intake: (payload: IntakeLeadPayload) =>
    apiClient.post<Lead>("/api/leads/intake", payload),

  search: (params: LeadSearchParams = {}, signal?: AbortSignal): Promise<LeadPage> => {
    const qs = new URLSearchParams();
    if (params.page)     qs.set("page",     String(params.page));
    if (params.pageSize) qs.set("pageSize", String(params.pageSize));
    if (params.sortBy)   qs.set("sortBy",   params.sortBy);
    if (params.sortDir)  qs.set("sortDir",  params.sortDir);
    if (params.cfSort)   qs.set("cfSort",   params.cfSort);
    if (params.cfSortDir) qs.set("cfSortDir", params.cfSortDir);
    if (params.cfFilter) {
      for (const [key, value] of Object.entries(params.cfFilter)) {
        if (value !== "") qs.append(`cfFilter[${key}]`, value);
      }
    }
    return apiClient.get<LeadPage>(`/api/leads?${qs.toString()}`, { signal });
  },

  getActivities: ({
    leadId,
    page,
    pageSize,
    type,
    signal
  }: {
    leadId: string;
    page: number;
    pageSize: number;
    type?: string;
    signal?: AbortSignal;
  }) => {
    const query = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize)
    });

    if (type?.trim()) {
      query.set("type", type.trim());
    }

    return apiClient.get<LeadActivitiesPage>(`/api/leads/${leadId}/activities?${query.toString()}`, {
      signal
    });
  },

  addNote: ({ leadId, note }: { leadId: string; note: string }) =>
    apiClient.post<void>(`/api/leads/${leadId}/activities`, { note })
};