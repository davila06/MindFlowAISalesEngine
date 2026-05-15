# Contributing Guide

Thanks for contributing to MindFlow AI Sales Engine.

## Branch Strategy

- `main`: production-ready branch.
- Feature branches: `feat/<scope>-<short-name>`.
- Fix branches: `fix/<scope>-<short-name>`.
- Docs branches: `docs/<scope>-<short-name>`.

## Pull Request Rules

1. Keep PRs focused to one concern.
2. Link task or improvement ID (e.g., `OPS-05`, `DOC-02`).
3. Include evidence: build/test output and impacted docs.
4. Do not merge with failing required checks.

## Required Checks

- Backend build in Release mode.
- Frontend lint/build where applicable.
- When UI behavior changes: `npx playwright test tests/e2e/flows.spec.ts` for the touched flow or stronger equivalent.
- When shared UI or operational routes change: run `npm run test:e2e:a11y`, `npm run test:e2e:contracts`, and `npm run test:e2e:visual` before merge.
- Security/dependency checks.
- Documentation updates when behavior changes.

## UI Enterprise DoD

- No `window.confirm`; destructive flows must use `ConfirmDialog`.
- Dynamic HTML must be sanitized before render.
- UI strings must come from i18n resources.
- Critical routes must keep loading, empty, error, and success states.
- Rule and pipeline flows must stay operable with keyboard labels/focus.
- PR evidence for UI scope must include build plus the narrowest executable validation for the changed flow.

## UI Debt SLA

| Severity | Description | SLA |
|---|---|---|
| Critical | Blocks operation, causes data loss, or breaks accessibility/security path | Fix before release or within 24h |
| High | Major workflow degraded with workaround only | Fix within 5 business days |
| Medium | Noticeable UX inconsistency or non-blocking regression | Fix within 2 sprints |
| Low | Cosmetic or localized polish debt | Prioritize in continuous backlog within 1 quarter |

Backlog policy:
- Every accepted UI debt item must carry severity, owner, target milestone, and validation note.
- A release cannot close with unresolved Critical UI debt.

## Coding Standards Summary

- Backend: Clean Architecture boundaries, explicit contracts, UTC timestamps.
- Frontend: feature-based structure and accessible UI states.
- Infra/ops: deterministic scripts, non-interactive CI, auditable automation.
- Docs: update `ia/04_tasks.md`, `ia/05_progress.md`, and `ia/06_decisions.md` for structural changes.

## Commit Convention

- `feat(scope): description`
- `fix(scope): description`
- `docs(scope): description`
- `chore(scope): description`

## Security Reporting

Do not open public issues for vulnerabilities with exploit details.
Use internal security channel and include impact, reproduction, and mitigation proposal.
