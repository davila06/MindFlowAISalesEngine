# Incident Playbook By Severity

Owner: Incident Commander Rotation

## Severity Definitions

- SEV-1: Full production outage or data integrity risk.
- SEV-2: Major degradation affecting core sales flow.
- SEV-3: Partial degradation with workaround.
- SEV-4: Minor issue with low business impact.

## Common Workflow

1. Detect and classify severity.
2. Create incident channel and assign commander.
3. Stabilize service first, root cause second.
4. Communicate status updates in fixed intervals.
5. Run post-incident review and corrective actions.

## SEV-1 Response

- SLA:
  - Acknowledge in 5 minutes.
  - Mitigate in 30 minutes target.
- Actions:
  1. Freeze active deployments.
  2. Trigger rollback workflow if release-related.
  3. Validate health endpoints and smoke tests.
  4. If data risk exists, execute restore runbook.
- Communication:
  - Updates every 15 minutes to engineering and product stakeholders.

## SEV-2 Response

- SLA:
  - Acknowledge in 10 minutes.
  - Mitigate in 60 minutes target.
- Actions:
  1. Isolate affected module and tenant segments.
  2. Apply feature flag mitigation if possible.
  3. Execute module-specific runbook.
- Communication:
  - Updates every 30 minutes.

## SEV-3 Response

- SLA:
  - Acknowledge in 30 minutes.
  - Mitigate in business day.
- Actions:
  1. Apply workaround.
  2. Open tracked issue with owner and ETA.

## SEV-4 Response

- SLA:
  - Acknowledge in 1 business day.
- Actions:
  1. Backlog triage.
  2. Include in routine maintenance release.

## Exit Criteria

Incident closes when:
- Service metrics return to normal.
- Customer impact is contained.
- Corrective action items are logged with owners.
