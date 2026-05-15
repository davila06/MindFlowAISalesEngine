# DB Provider Cutover Plan (SQLite -> PostgreSQL)

Owner: Platform + Backend
Status: Approved for execution
Related ADR: ADR-72

## Objective

Migrate production persistence from SQLite to PostgreSQL with controlled risk, rollback path, and measurable validation.

## Scope

- Backend API persistence provider switch.
- Schema creation via EF Core migrations.
- Data export/import from SQLite to PostgreSQL.
- Post-cutover validation and rollback guardrails.

## Phase 1 - Preparation

1. Provision PostgreSQL target (managed or self-hosted) with:
   - HA enabled where possible.
   - Automated backups.
   - Monitoring/alerts enabled.
2. Configure secrets:
   - `ConnectionStrings:DefaultConnection` for PostgreSQL.
   - `Database:Provider=postgres`.
3. Validate baseline migration apply:
   - `dotnet ef database update --context LeadsDbContext`.

## Phase 2 - Rehearsal (Staging)

1. Export SQLite data snapshot.
2. Transform and import into PostgreSQL rehearsal DB.
3. Run smoke and load checks:
   - Health checks
   - Leads intake
   - Pipeline board
   - Analytics heavy endpoints
4. Record comparison vs baseline and confirm no regression on critical flows.

## Phase 3 - Production Cutover

1. Freeze writes window.
2. Final SQLite backup.
3. Export + import latest data.
4. Switch provider config to PostgreSQL.
5. Deploy API with migration-aware startup.
6. Run post-deploy smoke suite.

## Validation Criteria

- `/health/live` and `/health/ready` return 200.
- Core endpoints return expected status and data shape.
- Error rate and p95 latency remain within operational thresholds.
- No cross-tenant data leakage.

## Rollback Plan

1. Revert provider config to SQLite.
2. Restore last verified SQLite backup.
3. Redeploy previous stable release.
4. Re-run smoke tests and notify stakeholders.

## Evidence Artifacts

- Benchmark files in `backend/tests/LoadTests/results/db-poc-*.json`.
- Migration logs.
- Cutover checklist execution record.
