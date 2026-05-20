export interface Lead {
  id: string;
  email?: string;
  phone?: string;
  source: string;
  score?: number;
}

export interface PipelineStage {
  id: string;
  name: string;
  order: number;
  color?: string;
}

export interface Opportunity {
  id: string;
  leadId: string;
  stageId: string;
  title: string;
  value: number;
}

export interface LeadActivity {
  id: string;
  leadId: string;
  activityType: string;
  title?: string;
  description?: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  actor: string;
  occurredAtUtc: string;
}

export interface LeadActivitiesPage {
  items: LeadActivity[];
  page: number;
  pageSize: number;
  total: number;
  hasMore: boolean;
}