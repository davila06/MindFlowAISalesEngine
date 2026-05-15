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