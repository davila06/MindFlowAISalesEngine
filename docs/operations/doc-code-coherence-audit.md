# Monthly Documentation-Code Coherence Audit

Owner: Architecture + Platform
Frequency: Monthly

## Purpose

Ensure implemented behavior and published documentation remain consistent.

## Audit Checklist

1. API contracts
   - OpenAPI artifact is regenerated after endpoint changes.
   - Breaking changes have version bump and migration notes.

2. Operations
   - Runbooks reflect current scripts/workflows.
   - Incident playbook severity rules are current.
   - Backup/restore and rollback paths are still executable.

3. Security and governance
   - RBAC matrix matches authorization policies.
   - Sensitive controls and secrets guidance are accurate.

4. Product and KPI docs
   - KPI definitions match current analytics endpoints.
   - Definition of Done reflects current quality gates.

5. IA traceability
   - Task/progress/decision logs are synchronized.
   - Closed checklist items have concrete evidence references.

## Audit Output

- Score (0-100) for documentation coherence.
- Drift list with owner and due date.
- Follow-up actions tracked in `ia/07_issues.md`.

## Suggested Scoring Model

- API coherence: 25 points
- Operations coherence: 25 points
- Security/RBAC coherence: 20 points
- Product/KPI coherence: 15 points
- IA traceability coherence: 15 points

## Escalation Rule

If score < 85, open corrective action issue and block major release until top drift items are resolved.
