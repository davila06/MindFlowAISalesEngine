// ---- Sequences ----
export interface SequenceStep {
  id: string;
  order: number;
  actionType: string;
  actionValue: string;
  delayDays: number;
}

export interface Sequence {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  steps: SequenceStep[];
}

export interface SequenceEnrollment {
  id: string;
  leadId: string;
  sequenceId: string;
  status: string;
  nextStepOrder: number;
  nextStepDueAtUtc: string;
  enrolledAtUtc: string;
  completedAtUtc?: string;
  exitedAtUtc?: string;
  exitReason?: string;
}

export interface CreateSequenceRequest {
  name: string;
  description?: string;
  steps: { order: number; actionType: string; actionValue: string; delayDays: number }[];
}

export interface UpdateSequenceRequest extends CreateSequenceRequest {
  isActive: boolean;
}

// ---- Custom Fields ----
export interface CustomFieldDefinition {
  id: string;
  key: string;
  label: string;
  fieldType: string;
  entityType: string;
  options?: string;
  isRequired: boolean;
  order: number;
  createdAtUtc: string;
}

export interface CustomFieldValue {
  key: string;
  value?: string;
  updatedAtUtc: string;
}

// ---- WhatsApp ----
export interface WhatsAppMessage {
  id: string;
  externalMessageId?: string;
  contactPhone: string;
  direction: string;
  body?: string;
  templateName?: string;
  status: string;
  leadId?: string;
  sentAtUtc: string;
}

// ---- Lead Query ----
export interface LeadSummary {
  id: string;
  email?: string;
  phone?: string;
  source: string;
  channel: string;
  campaign: string;
  country: string;
  score: number;
  priority: string;
  createdAtUtc: string;
  customFields: Record<string, string | null>;
}

export interface LeadPage {
  page: number;
  pageSize: number;
  total: number;
  hasMore: boolean;
  items: LeadSummary[];
}
