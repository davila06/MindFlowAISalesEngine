# Coding Standards

## General Principles

1. Keep modules cohesive and boundaries explicit.
2. Prefer deterministic behavior over implicit conventions.
3. Favor small, testable units.
4. Ensure operational observability for critical flows.

## Backend Standards (.NET)

- Follow Clean Architecture layering (`Controllers -> Application -> Domain -> Infrastructure`).
- Avoid leaking infrastructure concerns into domain contracts.
- Use UTC for all timestamps.
- Preserve tenant isolation in every read/write path.
- Keep endpoint contracts explicit and stable.

## Frontend Standards (Next.js)

- Use feature-based organization.
- Cover loading, empty, error, and success states.
- Keep network layer centralized in services.
- Ensure keyboard navigation and semantic labels.

## Testing Standards

- Add or update tests for behavior changes.
- Keep regression evidence in task/progress docs.
- Avoid flaky tests by removing environment assumptions.

## Documentation Standards

- Any structural decision -> update ADRs.
- Any endpoint change -> update OpenAPI artifacts.
- Any operational change -> update runbook/playbook.
- Any release change -> update changelog.
