import { apiClient } from "@/services/apiClient";
import type {
  EmailLog,
  EmailLogsPage,
  EmailLogsQuery,
  EmailTemplatePreview,
  EmailTemplateVersion,
  SmtpSettings
} from "@/types/email";

function getLogsPath(page: number, pageSize: number, search: string) {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
    search
  });

  return `/api/email/logs?${params.toString()}`;
}

export const emailService = {
  getSmtp: () => apiClient.get<SmtpSettings>("/api/email/smtp-settings"),
  saveSmtp: (payload: SmtpSettings) =>
    apiClient.put<SmtpSettings>("/api/email/smtp-settings", payload),
  getLogs: async ({ page, pageSize, search, signal }: EmailLogsQuery): Promise<EmailLogsPage> => {
    const items = await apiClient.get<EmailLog[]>(getLogsPath(page, pageSize, search), {
      signal
    });

    return {
      items,
      page,
      pageSize,
      hasMore: items.length >= pageSize
    };
  },
  createTemplateVersion: (templateKey: string, payload: { subject: string; bodyHtml: string; requiredVariables: string[] }) =>
    apiClient.post<EmailTemplateVersion>(`/api/email/templates/${templateKey}/versions`, payload),
  previewTemplate: (templateKey: string, payload: { variables: Record<string, string> }) =>
    apiClient.post<EmailTemplatePreview>(`/api/email/templates/${templateKey}/preview`, payload),
  rollbackTemplate: (templateKey: string, targetVersion: number) =>
    apiClient.post<EmailTemplateVersion>(`/api/email/templates/${templateKey}/rollback`, { targetVersion })
};
