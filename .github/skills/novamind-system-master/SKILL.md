---
name: novamind-system-master
description: Master product skill for NovaMind MindFlow Sales Automation System. Defines end-to-end flow, module boundaries, UI scope, automation policies, and implementation guardrails across backend, frontend, infra, and docs. WHEN: design novamind module, plan end-to-end sales flow, lead intake, deduplication, scoring engine, assignment engine, pipeline kanban, rules engine, email automation, smtp config, email templates, proposals pdf, onboarding sequence, post-sale automation, analytics dashboard, scope ui for novamind, decisiones de arquitectura novamind, sistema de ventas automatizadas.
invocable: false
---

# NovaMind System Master

## Purpose

This skill represents the **master blueprint** of NovaMind MindFlow as an automated sales engine.
Use it to keep all implementation decisions aligned with the product vision, module scope, and operating model.

## Vision and Positioning

NovaMind is **not a classic CRM**.
It is a revenue engine designed to:
- capture leads automatically,
- qualify them intelligently,
- execute the sales process with minimal manual intervention,
- convert opportunities into customers systematically.

## Core Design Principles

1. Event-driven architecture.
2. Automated-first execution (human-in-the-loop only for control/override).
3. Multi-tenant SaaS readiness.
4. Rule-configurable behavior (no hardcoded business playbooks).
5. Scalability as a default requirement.
6. Strategic UI: only where control and decision quality are improved.

## End-to-End Value Flow

Lead Intake -> Processing -> Deduplication -> Scoring -> Assignment -> Pipeline -> Automation -> Closing -> Post-sale onboarding.

When implementing any feature, map it to this flow and avoid isolated solutions that break continuity.

## Module Map and Guardrails

### 1) Lead Intake (API only)
- Canonical endpoint: `POST /api/leads/intake`.
- Responsibilities: validation, normalization, source identification, logging.
- UI policy: **No UI** (machine-to-machine ingestion).

### 2) Deduplication
- Matching order: email, phone, fuzzy matching.
- Must support merge policy (automatic/manual review).

### 3) Scoring Engine
- Score rules are configurable.
- Score recalculation must be event-triggered.
- Score persistence is mandatory.

### 4) Assignment Engine
- Strategies: round robin + rule-based assignment (industry, country, score).
- Contact SLA is part of assignment policy.

### 5) Pipeline (UI)
- Main operational UI is Kanban.
- Stages must be configurable by tenant.
- Stage movement must create change history.

### 6) Rules Engine (Core + UI)
- Model: Trigger -> Condition -> Action.
- Must provide CRUD + activate/deactivate controls.
- Must remain understandable to business operators.

### 7) Automation Runtime (Background Jobs)
- Deferred and recurring jobs.
- Technical logs required.
- UI policy: **No operational UI**.

### 8) Email Management (Admin UI)
- SMTP settings are tenant-scoped and securely stored.
- Templates are automation assets, not manual send tools.
- Logs are read-only.

### 9) Proposals
- Proposal templates, PDF generation, automatic send, versioning.

### 10) Onboarding
- Won -> Customer conversion.
- Internal task creation.
- Welcome sequence automation.

### 11) Analytics (UI)
- Leads/day, conversion, pipeline value, average stage time.

## UI Scope Policy

UI is mandatory for:
- Pipeline,
- Rules Engine,
- Email configuration,
- Dashboard/Analytics.

UI is out of scope for:
- Lead Intake,
- Background automation runtime.

## Automation Baseline

Critical default automations:
1. Immediate intake email.
2. Follow-ups (24h, 48h).
3. Stagnation alerts.
4. Proposal reminders.
5. Automatic customer creation after win.

## Cross-Skill Routing

Use this skill to route implementation to the right project skill:
- Backend implementation details -> `novamind-backend`.
- Frontend and UI behavior -> `novamind-frontend`.
- Infra, environments, CI/CD -> `novamind-infra-devops`.
- Specs, ADRs, and traceability -> `novamind-documentation`.

If a task spans multiple layers, this skill defines global constraints first, then specialized skills apply.

## Non-Negotiable Constraints

- No feature may violate multi-tenant isolation.
- No business-critical automation should depend on manual operator steps.
- No UI should be added to modules explicitly marked as no-UI.
- No template should be manually executed if designed for rule-triggered automation.
- No module should drift from the end-to-end flow without an explicit architecture decision.
