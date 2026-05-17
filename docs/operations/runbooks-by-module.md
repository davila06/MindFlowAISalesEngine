
# Runbooks By Module

Owner: Platform Engineering
Review cadence: Monthly

## Purpose

Define operational runbooks per module with trigger conditions, first response actions, and escalation paths.

## Leads And Intake

- Symptoms:
  - Increased 4xx/5xx on `POST /api/leads/intake`
  - Growth in failed intake queue
- First actions:
  1. Check API health on `/health/ready`.
  2. Inspect failed intake list endpoint.
  3. Validate idempotency key behavior and payload schema changes.
- Escalation:
  - Backend on-call if error rate > 5% for 10 minutes.

## Pipeline

- Symptoms:
  - Invalid stage transition spikes
  - Board latency degradation
- First actions:
  1. Validate stage catalog integrity.
  2. Check WIP limit configuration.
  3. Review optimistic concurrency conflicts.
- Escalation:
  - Product + backend if transitions fail for critical tenants.

## Email And Follow-up

- Symptoms:
  - Dispatch failures increase
  - Poison queue growth
- First actions:
  1. Check SMTP/provider settings endpoint.
  2. Run dispatch due jobs manually.
  3. Review stop-list and quiet hours configuration.
- Escalation:
  - Ops if delivery failure rate >= threshold from alert policy.

## Rules Engine

- Symptoms:
  - Rule execution errors
  - Unexpected automated stage moves
- First actions:
  1. Run rule dry-run on affected rule.
  2. Review last revision and rollback if needed.
  3. Validate cooldown and conflict policy.
- Escalation:
  - Backend owner for deterministic rollback.

## Proposals And Onboarding

- Symptoms:
  - PDF generation failures
  - Reminder queue backlog
- First actions:
  1. Validate proposal template active version.
  2. Requeue failed reminders.
  3. Verify onboarding job status.
- Escalation:
  - Product ops if onboarding SLA breach > 10%.

## Analytics And Observability

- Symptoms:
  - Alert flood or missing alerts
  - High latency in trends/heatmap endpoints
  - UX telemetry ingestion failures or missing dashboard signals
- First actions:
  1. Check alert threshold configuration.
  2. Review cardinality metadata and overflow bucket.
  3. Run incremental aggregation endpoint.
- Escalation:
  - Data/ops if SLO non-compliance persists for > 30 minutes.
  4. Validate `/api/ux/telemetry` endpoint health and event logging.
  5. Inspect UX observability dashboard for missing or anomalous signals.
- Escalation:
  - Data/ops if SLO non-compliance or UX dashboard gaps persist for > 30 minutes.

## Operations And Release

- Symptoms:
  - Failed deployment gates
  - Rollback events
- First actions:
  1. Execute smoke test script.
  2. Validate blue/green slot health.
  3. Trigger rollback workflow when required.
- Escalation:
  - Incident commander for SEV-1 and SEV-2.
