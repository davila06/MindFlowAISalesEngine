export interface RuleCondition {
  field: string;
  operator: "eq" | "gt" | "lt" | "contains";
  value: string;
}

export interface RuleAction {
  type: string;
  value: string;
}

export interface Rule {
  id: string;
  name: string;
  trigger: string;
  isActive: boolean;
  version?: number;
  priority?: number;
  conflictPolicy?: string;
  cooldownMinutes?: number;
  allowDestructiveActions?: boolean;
  conditions: RuleCondition[];
  actions: RuleAction[];
}