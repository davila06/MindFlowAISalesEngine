---
name: novamind-frontend
description: Architecture and coding conventions for the NovaMind MindFlow AI Sales Engine Next.js frontend. Enforces feature-based structure, service layer pattern, tenant/auth hooks, and strict UI scope. WHEN: create frontend feature, add Next.js page, build pipeline UI, Kanban board, rules UI, email config UI, SMTP form, email templates UI, email logs view, dashboard analytics, admin page, add service, add hook, NovaMind frontend, MindFlow UI, create component, add React component, frontend architecture.
invocable: false
---

# NovaMind Frontend — Architecture & Conventions

## Project Context

NovaMind MindFlow frontend is the **operational and configuration UI** for the automated sales engine.  
It is **not** a general-purpose web app — every route maps to a specific business function.

Stack: **Next.js 14+ (App Router)** · **TypeScript** · **Tailwind CSS** · **React Query** · **Zustand (if needed)**

---

## Non-Negotiable Principles

1. **Feature-based structure** — code lives by feature, not by type. Never dump everything into `pages/`.
2. **Strict UI scope** — only build UI for modules that require it. Lead Intake and background jobs have **no UI**.
3. **Service layer is mandatory** — components never call `fetch` directly. Always use a `*.service.ts`.
4. **Hooks for cross-cutting state** — tenant, auth, permissions resolved via dedicated hooks, never inline.
5. **Components are dumb by default** — business logic lives in hooks or services, not in JSX.
6. **Feature toggles at layout** — features can be enabled/disabled per tenant in `layout.tsx` via `useTenant()`.

---

## Canonical Folder Structure

```
/frontend/
│
├── app/                            # Next.js App Router
│   ├── layout.tsx                  # Root layout — tenant guard, auth guard, nav
│   ├── page.tsx                    # Redirect to /dashboard
│   │
│   ├── dashboard/
│   │   └── page.tsx                # Analytics overview: leads/day, conversion, pipeline value
│   │
│   ├── pipeline/
│   │   └── page.tsx                # Kanban board — main operational view
│   │
│   ├── rules/
│   │   └── page.tsx                # Rules Engine CRUD: Trigger → Condition → Action
│   │
│   ├── email/
│   │   ├── smtp/
│   │   │   └── page.tsx            # SMTP config form per tenant
│   │   ├── templates/
│   │   │   └── page.tsx            # Email template CRUD
│   │   └── logs/
│   │       └── page.tsx            # Email logs (read-only)
│   │
│   └── admin/
│       └── page.tsx                # Tenant admin: users, plans, settings
│
├── components/
│   ├── ui/                         # Generic primitives: Button, Input, Modal, Badge, Table …
│   ├── layout/                     # Sidebar, Navbar, PageHeader, TenantBadge
│   ├── pipeline/                   # KanbanBoard, KanbanColumn, OpportunityCard, StageSelector
│   ├── rules/                      # RuleForm, TriggerSelector, ConditionBuilder, ActionSelector
│   └── email/                      # SmtpForm, TemplateEditor, EmailLogTable
│
├── services/                       # API communication — one file per domain
│   ├── apiClient.ts                # Base Axios/fetch instance with auth headers + tenant header
│   ├── leads.service.ts
│   ├── pipeline.service.ts
│   ├── rules.service.ts
│   └── email.service.ts
│
├── hooks/
│   ├── useTenant.ts                # Reads tenant from JWT / subdomain
│   ├── useAuth.ts                  # Auth state, login/logout
│   └── usePermissions.ts           # Role-based permission checks
│
├── types/
│   ├── lead.ts
│   ├── rule.ts
│   ├── email.ts
│   └── user.ts
│
├── styles/
└── public/
```

---

## UI Scope — What Has UI and What Does Not

| Module | Has UI? | Notes |
|---|---|---|
| Lead Intake | ❌ No | Machine-to-machine API only |
| Background Jobs / Automation | ❌ No | Hangfire dashboard is ops-only, not product UI |
| Pipeline | ✅ Yes | Main operational view (Kanban) |
| Rules Engine | ✅ Yes | Config UI — Trigger → Condition → Action |
| Email / SMTP Config | ✅ Yes | Admin config per tenant |
| Email Templates | ✅ Yes | CRUD, HTML editor with variables |
| Email Logs | ✅ Yes | Read-only table |
| Dashboard / Analytics | ✅ Yes | Metrics only — no actions |
| Admin | ✅ Yes | Users, plans, tenant settings |

**Never add UI for a module not in this table without an explicit product decision.**

---

## Service Layer Pattern

All API calls go through a service. Services use `apiClient.ts` as the base.

```typescript
// services/apiClient.ts
import axios from 'axios';

const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
});

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken(); // from auth store
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export default apiClient;
```

```typescript
// services/pipeline.service.ts
import apiClient from './apiClient';
import type { Opportunity, PipelineStage } from '@/types/lead';

export const pipelineService = {
  getStages: () =>
    apiClient.get<PipelineStage[]>('/api/pipeline/stages').then(r => r.data),

  getOpportunities: () =>
    apiClient.get<Opportunity[]>('/api/pipeline/opportunities').then(r => r.data),

  moveOpportunity: (opportunityId: string, targetStageId: string) =>
    apiClient.patch(`/api/pipeline/opportunities/${opportunityId}/stage`, { targetStageId })
      .then(r => r.data),
};
```

---

## Hooks Pattern

```typescript
// hooks/useTenant.ts
import { useAuth } from './useAuth';

export function useTenant() {
  const { user } = useAuth();
  return {
    tenantId: user?.tenantId,
    tenantSlug: user?.tenantSlug,
    plan: user?.plan,             // Used for feature gating in layout
  };
}
```

```typescript
// hooks/usePermissions.ts
import { useAuth } from './useAuth';

export function usePermissions() {
  const { user } = useAuth();
  return {
    canManageRules: user?.roles.includes('admin'),
    canConfigureSmtp: user?.roles.includes('admin'),
    canViewLogs: user?.roles.includes('admin') || user?.roles.includes('manager'),
  };
}
```

---

## Pipeline UI — Kanban Pattern

```typescript
// components/pipeline/KanbanBoard.tsx
'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { pipelineService } from '@/services/pipeline.service';
import { KanbanColumn } from './KanbanColumn';

export function KanbanBoard() {
  const queryClient = useQueryClient();

  const { data: stages } = useQuery({
    queryKey: ['pipeline', 'stages'],
    queryFn: pipelineService.getStages,
  });

  const { data: opportunities } = useQuery({
    queryKey: ['pipeline', 'opportunities'],
    queryFn: pipelineService.getOpportunities,
  });

  const moveMutation = useMutation({
    mutationFn: ({ oppId, stageId }: { oppId: string; stageId: string }) =>
      pipelineService.moveOpportunity(oppId, stageId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['pipeline'] }),
  });

  return (
    <div className="flex gap-4 overflow-x-auto p-4">
      {stages?.map(stage => (
        <KanbanColumn
          key={stage.id}
          stage={stage}
          opportunities={opportunities?.filter(o => o.stageId === stage.id) ?? []}
          onMove={(oppId) => moveMutation.mutate({ oppId, stageId: stage.id })}
        />
      ))}
    </div>
  );
}
```

---

## Rules Engine UI — Builder Pattern

Rules UI follows the domain model: **Trigger → Conditions → Actions**

```typescript
// types/rule.ts
export type RuleTrigger =
  | 'lead.created'
  | 'lead.scored'
  | 'opportunity.stage_changed'
  | 'email.bounced';

export interface RuleCondition {
  field: string;       // e.g. 'lead.score', 'lead.country'
  operator: 'eq' | 'gt' | 'lt' | 'contains';
  value: string;
}

export interface RuleAction {
  type: 'send_email' | 'assign_lead' | 'move_stage' | 'create_task';
  params: Record<string, string>;
}

export interface Rule {
  id: string;
  name: string;
  isActive: boolean;
  trigger: RuleTrigger;
  conditions: RuleCondition[];
  actions: RuleAction[];
}
```

- `TriggerSelector` → `ConditionBuilder` (dynamic fields based on trigger) → `ActionSelector`.
- Rules list shows active/inactive badge + trigger type.
- Toggle active/inactive without entering edit mode.

---

## Email Module UI Conventions

### SMTP Config (`/email/smtp`)
- Form fields: Host, Port, Encryption (select), Username, Password (masked), From Name, From Email.
- "Test Connection" button → calls `POST /api/email/smtp/test` → shows success/error inline.
- Password field: show/hide toggle. Never pre-filled after save (security).

### Templates (`/email/templates`)
- Table: Name, Type, Subject, Status (Active/Inactive), Actions.
- Editor: HTML textarea (or simple rich text). Preview pane showing rendered output.
- Dynamic variables shown as chips: `{{lead.name}}`, `{{company.name}}`, `{{pipeline.stage}}`.
- Templates cannot be sent manually — show a banner: _"Templates are triggered by Rules only."_

### Email Logs (`/email/logs`)
- Read-only table. Columns: Date, Lead, Template, Status (Sent/Failed), Error.
- Filter by date range, status, template.
- No actions — purely observational.

---

## Type Conventions

```typescript
// types/lead.ts
export interface Lead {
  id: string;
  email: string;
  phone?: string;
  source: string;
  score: number;
  assignedUserId?: string;
  createdAt: string; // ISO 8601
}

export interface Opportunity {
  id: string;
  leadId: string;
  stageId: string;
  title: string;
  value?: number;
  createdAt: string;
}

export interface PipelineStage {
  id: string;
  name: string;
  order: number;
  color?: string;
}
```

---

## Feature Gating in Layout

```typescript
// app/layout.tsx (simplified)
import { useTenant } from '@/hooks/useTenant';

export default function RootLayout({ children }) {
  const { plan } = useTenant();
  const showRulesEngine = plan !== 'starter'; // starter plan = no rules engine

  return (
    <html>
      <body>
        <Sidebar showRulesEngine={showRulesEngine} />
        <main>{children}</main>
      </body>
    </html>
  );
}
```

---

## Anti-Patterns to Avoid

| ❌ Don't | ✅ Do instead |
|---|---|
| `fetch()` directly in a component | Use `*.service.ts` + React Query |
| Business logic in JSX | Extract to a hook or service |
| Add UI for Lead Intake or jobs | Those are headless — no UI |
| Hardcode tenant ID in components | Use `useTenant()` hook |
| Allow manual email sends from UI | Templates are rules-only, show banner |
| Giant `pages/` flat file dump | Feature folders under `app/` |
| Store auth token in `localStorage` | Use secure httpOnly cookie or memory |
| Skip type definitions | All API shapes defined in `types/` |
