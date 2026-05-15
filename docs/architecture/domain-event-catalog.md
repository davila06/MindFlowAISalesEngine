# Domain Event Catalog (Living)

Owner: Backend Architecture
Update trigger: Any new event producer/consumer.

## Purpose

Maintain a single source of truth for domain events emitted and consumed by MindFlow modules.

## Event Catalog

| Event | Producer | Consumers | Contract Notes | Idempotency |
|---|---|---|---|---|
| `lead.created` | Leads intake service | Scoring, Assignment, Follow-up, Email | Includes lead identity, tenant scope, source metadata | Required |
| `pipeline.stage.changed` | Pipeline service | Rules engine | Includes opportunity id, from/to stage, actor, reason | Required |
| `proposal.sent` | Proposal service | Rules engine, Analytics | Includes proposal id, lead id, template/version | Required |
| `email.dispatch.queued` | Email service | Dispatch worker, Analytics | Includes correlation id and channel | Required |
| `email.dispatch.failed` | Email dispatch service | Alert evaluation | Includes failure reason and retry count | Required |
| `onboarding.task.completed` | Onboarding service | Analytics | Includes customer id, task key, completion timestamp | Recommended |
| `alert.threshold.breached` | Alert evaluation service | Incident operations | Includes endpoint, metric, observed value | Required |

## Governance Rules

1. Every new event must include tenant context.
2. Every event must have a clear producer owner.
3. Event contracts must be backward compatible for one release cycle.
4. Consumers must be resilient to duplicate events.

## Change Management

- Add row for each new event.
- Update related ADR if event changes module boundary.
- Add test evidence under API tests for producer behavior.
