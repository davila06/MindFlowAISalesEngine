# Incident Response Runbook (SEC-20)

## Objective
Provide a repeatable response process for security incidents affecting API, tenant isolation, auth, and operational automation.

## Severity levels
- Sev-1: confirmed data breach, cross-tenant exposure, unauthorized admin execution.
- Sev-2: active auth bypass attempt, repeated brute-force, significant service degradation from abuse.
- Sev-3: suspicious pattern with no confirmed impact.

## Detection sources
- API logs with `traceId` and global exception middleware output.
- Admin audit log entries (`AdminAuditLogs`).
- Security test gate failures in CI workflow.
- Health/readiness anomalies and alert event spikes.

## Response flow
1. Triage (0-15 min)
- Confirm indicator and collect trace IDs.
- Classify severity and assign incident commander.

2. Containment (15-60 min)
- Enable strict mode (if not already enabled) for auth/tenant checks.
- Rotate affected credentials (SMTP/API keys/JWT signing keys).
- Temporarily block impacted operational endpoints via role policy if needed.

3. Eradication (1-24h)
- Patch root cause.
- Execute focused regression tests and security test suite.
- Validate no new cross-tenant leakage.

4. Recovery
- Restore normal traffic gradually.
- Monitor for recurrence for at least 24h.

5. Postmortem (within 48h)
- Document timeline, blast radius, root cause, mitigations.
- Add backlog actions with owner/date.

## Tabletop simulation cadence
- Monthly tabletop on one scenario:
  - Tenant spoofing attempt.
  - Unauthorized operational execution.
  - SMTP secret compromise.
- Capture evidence in project progress docs.
