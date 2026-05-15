import { apiClient } from "@/services/apiClient";
import type { Lead } from "@/types/lead";

export interface IntakeLeadPayload {
  email?: string;
  phone?: string;
  source: string;
}

export const leadsService = {
  intake: (payload: IntakeLeadPayload) =>
    apiClient.post<Lead>("/api/leads/intake", payload)
};