import { apiClient } from "@/services/apiClient";
import type { Rule } from "@/types/rule";

export interface RuleTemplate {
  key: string;
  name: string;
  description: string;
  template: {
    name: string;
    trigger: string;
    priority: number;
    conflictPolicy: string;
    cooldownMinutes: number;
    allowDestructiveActions: boolean;
    conditions: Array<{ field: string; operator: "eq" | "gt" | "lt" | "contains"; value: string }>;
    actions: Array<{ type: string; value: string }>;
  };
}

export interface RuleFixtureResponse {
  ruleId: string;
  matched: boolean;
  applied: boolean;
  actionsApplied: string[];
  skippedReasons: string[];
}

export interface RuleCreatePayload {
  name: string;
  trigger: string;
  isActive: boolean;
  priority: number;
  conflictPolicy: string;
  cooldownMinutes: number;
  allowDestructiveActions: boolean;
  conditions: Array<{ field: string; operator: "eq" | "gt" | "lt" | "contains"; value: string }>;
  actions: Array<{ type: string; value: string }>;
}

export const rulesService = {
  list: (signal?: AbortSignal) => apiClient.get<Rule[]>("/api/rules", { signal }),
  activate: (ruleId: string) => apiClient.post<void>(`/api/rules/${ruleId}/activate`, {}),
  deactivate: (ruleId: string) => apiClient.post<void>(`/api/rules/${ruleId}/deactivate`, {}),
  getTemplates: (signal?: AbortSignal) =>
    apiClient.get<RuleTemplate[]>("/api/rules/templates", { signal }),
  create: (payload: RuleCreatePayload) => apiClient.post<Rule>("/api/rules", payload),
  update: (ruleId: string, payload: RuleCreatePayload) =>
    apiClient.put<Rule>(`/api/rules/${ruleId}`, payload),
  testFixture: (payload: {
    ruleId: string;
    trigger: string;
    lead: {
      source: string;
      priority: string;
      score: number;
      hasEmail: boolean;
      hasPhone: boolean;
      fromStage?: string;
      toStage?: string;
    };
  }) => apiClient.post<RuleFixtureResponse>("/api/rules/test-fixture", payload),
  rollback: (ruleId: string, targetVersion: number) =>
    apiClient.post<Rule>(`/api/rules/${ruleId}/rollback`, { targetVersion }),
  /**
   * Fetches advanced rule builder metadata.
   * @param signal Optional AbortSignal for request cancellation.
   */
  getAdvancedBuilderMetadata: (signal?: AbortSignal) =>
    apiClient.get<{ fields: string[]; actions: string[]; operators: string[] }>(
      "/api/rules/advanced-builder-metadata",
      { signal }
    ),
};
