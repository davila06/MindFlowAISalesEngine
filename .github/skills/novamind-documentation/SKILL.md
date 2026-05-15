---
name: novamind-documentation
description: Documentation standards for NovaMind MindFlow architecture, API, UI, and product artifacts. Establishes required docs structure, update rules, traceability from epics to implementation, and decision logging. WHEN: write architecture doc, update rules-engine doc, document email module, create api openapi, update ui docs, create roadmap entries, write ADRs, update progress and tasks, maintain ia markdown files, project documentation for novamind.
invocable: false
---

# NovaMind Documentation Standards

## Goal

Keep technical and product documentation actionable, current, and directly traceable to implementation.

## Canonical Structure

```
/docs
├── architecture
│   ├── system-overview.md
│   ├── rules-engine.md
│   └── email-architecture.md
├── api
│   └── openapi.yaml
├── ui
│   ├── pipeline.md
│   ├── rules-ui.md
│   └── email-ui.md
└── product
    └── roadmap.md

/ia
├── 00_context.md
├── 01_requirements.md
├── 02_architecture.md
├── 03_plan.md
├── 04_tasks.md
├── 05_progress.md
├── 06_decisions.md
├── 07_issues.md
└── 08_retrospective.md
```

## Non-Negotiable Principles

1. Every implemented feature must have updated documentation in the corresponding domain.
2. Architecture docs describe decisions and boundaries, not TODO lists.
3. API behavior must be reflected in openapi.yaml.
4. UI docs must mirror actual routes, components, and permissions.
5. Product roadmap tracks intent; ia tasks and progress track execution.
6. Decisions are logged with context and trade-offs.

## Update Rules by Area

### Architecture docs

Use when changing module boundaries, event flows, or cross-cutting concerns.

Required sections:
- Purpose
- Scope and non-scope
- Data flow and event flow
- Dependencies
- Risks and constraints

### API docs

For each endpoint change:
- Update route, method, request schema, response schema, and errors in openapi.yaml.
- Mark auth requirements and tenant constraints.
- Include examples for critical endpoints.

### UI docs

For each page/component change:
- Update target route and user intent.
- Document data dependencies and service calls.
- Document states: loading, empty, success, error.
- Document role/plan restrictions.

### Product docs

Roadmap entries should include:
- Outcome expected
- Success metric
- Dependencies
- Risks

## IA Working Files Policy

### 00_context.md
Business context and constraints only.

### 01_requirements.md
Functional and non-functional requirements, acceptance criteria.

### 02_architecture.md
Current architecture snapshot, modules, integration boundaries.

### 03_plan.md
Execution plan by phases and milestones.

### 04_tasks.md
Actionable tasks with owner and status.

### 05_progress.md
Chronological progress updates with evidence.

### 06_decisions.md
Decision log (what, why, alternatives, impact).

### 07_issues.md
Open problems, blockers, mitigations, owner.

### 08_retrospective.md
What worked, what failed, and process improvements.

## Traceability Rules

- Each epic feature should map to:
  - one or more tasks
  - implementation changes
  - documentation updates
- Keep references between docs explicit by section titles.
- When a decision changes implementation, update decisions and architecture together.

## Documentation Quality Checklist

Before closing a feature:
- Requirements updated if scope changed.
- Architecture updated if boundaries changed.
- API contract updated if endpoint changed.
- UI docs updated if route/component changed.
- Progress and decisions logged.

## Anti-Patterns

- Documentation written only at project end.
- Architecture docs that copy code without design intent.
- API docs not matching real payloads.
- UI docs that omit permissions/tenant behavior.
- Progress notes without evidence or dates.
