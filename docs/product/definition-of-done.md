# Definition Of Done By Feature Type

## Purpose

Standardize completion criteria across backend, frontend, operations, security, and documentation workstreams.

## Backend Feature DoD

- Functional acceptance criteria implemented.
- Integration or unit tests added/updated.
- API contract reflected in OpenAPI artifact.
- Tenant and authorization constraints validated.
- Observability and error handling in place.

## Frontend Feature DoD

- UX states implemented (loading/empty/error/success).
- Accessibility basics validated (keyboard, labels, focus).
- Dynamic HTML sanitized before render.
- UI copy routed through i18n resources.
- API integration through service layer only.
- E2E or integration tests updated where relevant.
- Shared UI changes validated against smoke/a11y/contracts/visual gates when applicable.

## Evidencia de cumplimiento E2E — Mayo 2026

- Todas las suites E2E frontend (unitarias, accesibilidad, visuales, contratos) pasan en verde.
- Cambios de configuración y procedimiento documentados en `docs/product/frontend-e2e-status-2026-05.md`.
- Validación registrada en `ia/05_progress.md` y cierre de tarea en `ia/04_tasks.md`.

## DevOps/Operations DoD

- Workflow/script is deterministic and non-interactive.
- Failure paths produce actionable diagnostics.
- Rollback/restore path documented and testable.
- Required secrets/configuration are documented.

## Security Feature DoD

- Threat scenario addressed with explicit control.
- Security tests or policy checks updated.
- No secrets in code or logs.
- RBAC impact documented.

## Documentation/Governance DoD

- `ia/04_tasks.md` updated with evidence.
- `ia/05_progress.md` updated chronologically.
- `ia/06_decisions.md` updated for structural decisions.
- Relevant docs under `docs/` updated.

## Release Closure DoD

- Build and required checks green.
- No open Critical UI debt and any accepted High UI debt has owner plus mitigation date.
- Changelog updated for release scope.
- Open issues and risks documented with owners.
