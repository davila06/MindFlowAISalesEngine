export interface SmtpSettings {
  providerType?: "smtp" | "webhook";
  providerBaseUrl?: string;
  apiKey?: string;
  host: string;
  port: number;
  username: string;
  password?: string;
  fromEmail: string;
  fromName?: string;
  enableSsl: boolean;
}

export interface EmailTemplate {
  id: string;
  name: string;
  type: string;
  subject: string;
  bodyHtml: string;
  isActive: boolean;
}

export interface EmailLog {
  id: string;
  correlationId?: string;
  templateName: string;
  toEmail?: string;
  status: string;
  sentAtUtc: string;
  errorMessage?: string;
  // Tracking
  openCount: number;
  clickCount: number;
  firstOpenedAtUtc?: string;
  firstClickedAtUtc?: string;
  isOpened: boolean;
  isClicked: boolean;
}

export interface EmailTrackingMetrics {
  templateName: string;
  totalSent: number;
  totalOpened: number;
  totalClicked: number;
  openRatePercent: number;
  clickRatePercent: number;
  clickToOpenRatePercent: number;
}

export interface EmailTemplateVersion {
  id: string;
  templateKey: string;
  version: number;
  subject: string;
  bodyHtml: string;
  isCurrent: boolean;
  requiredVariables: string[];
}

export interface EmailTemplatePreview {
  subject: string;
  bodyHtml: string;
}

export interface EmailLogsQuery {
  page: number;
  pageSize: number;
  search: string;
  signal?: AbortSignal;
}

export interface EmailLogsPage {
  items: EmailLog[];
  page: number;
  pageSize: number;
  hasMore: boolean;
}