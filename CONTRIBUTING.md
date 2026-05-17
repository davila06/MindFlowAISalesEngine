# Contributing Guide

Thanks for contributing to MindFlow AI Sales Engine.

## Branch Strategy


## Pull Request Rules

1. Keep PRs focused to one concern.
2. Link task or improvement ID (e.g., `OPS-05`, `DOC-02`).
3. Include evidence: build/test output and impacted docs.
4. Do not merge with failing required checks.

## Required Checks


## UI Definition of Done (DoD)


## UI Enterprise DoD


## UI Debt SLA

| Severity | Description | SLA |
|---|---|---|
| Critical | Blocks operation, causes data loss, or breaks accessibility/security path | Fix before release or within 24h |
| High | Major workflow degraded with workaround only | Fix within 5 business days |
| Medium | Noticeable UX inconsistency or non-blocking regression | Fix within 2 sprints |
| Low | Cosmetic or localized polish debt | Prioritize in continuous backlog within 1 quarter |

Backlog policy:

## Coding Standards Summary


## Commit Convention


## Security Reporting

Do not open public issues for vulnerabilities with exploit details.
Use internal security channel and include impact, reproduction, and mitigation proposal.

## Evidencia de validación E2E — Mayo 2026


## Backend Contributions

When contributing to the backend, follow these additional guidelines:

1. **Code Structure**:
   - Place new domain logic in the appropriate `Domain/` folder.
   - Use `Application/` for commands, queries, and handlers.
   - Avoid placing business logic in controllers.

2. **Testing**:
   - Write unit tests for all new features in `tests/Api.Tests/`.
   - Use integration tests for API endpoints.

3. **Secrets Management**:
   - Do not hardcode secrets in `appsettings.json`. Use Azure Key Vault.

## Frontend Contributions

When contributing to the frontend, follow these additional guidelines:

1. **Component Structure**:
   - Place new components in the appropriate `components/` folder.
   - Use `hooks/` for reusable logic.

2. **Styling**:
   - Use `globals.css` for global styles and scoped CSS modules for components.

3. **Testing**:
   - Write unit tests for components using Jest.
   - Use Playwright for end-to-end tests.

4. **Environment Variables**:
   - Document any new variables in `frontend/.env.example`.

## Documentation Contributions

1. **Location**:
   - Place new documentation in the appropriate `docs/` subfolder.
