# QA Release Candidate Test Plan — MindFlow AI Sales Engine

> QA-19: Release candidate test plan.  
> Last updated: 2026-05-04  
> Version: 1.0

---

## 1. Purpose

This document defines the mandatory test plan that must be executed and pass before any release candidate (RC) is promoted to production. It is a living document owned by the QA lead and updated with each major release cycle.

---

## 2. Scope

All test activities below apply to every release candidate, regardless of change size. The "fast gate" (< 15 min) must run on every PR. The "full RC gate" runs on every release branch.

---

## 3. Fast Gate (every PR / < 15 min)

| Check | Tool | Pass Criterion | Command |
|-------|------|----------------|---------|
| Build | dotnet build | Exit 0, 0 errors | `dotnet build --configuration Release` |
| Unit + Integration tests | xUnit | All tests GREEN | `dotnet test` |
| Lint / format | dotnet-format | No unformatted files | `dotnet format --verify-no-changes` |
| Frontend lint | ESLint | 0 errors | `npm run lint` |
| Bundle budget | check-bundle-budget.mjs | < 380 KB | `npm run build:verified` |

---

## 4. Full RC Gate (release branches)

### 4.1 Backend

| Check | Tool | Pass Criterion |
|-------|------|----------------|
| Build Release | dotnet build | Exit 0 |
| Full test suite | xUnit | 0 failures |
| Coverage: Application | coverlet | ≥ 80% line rate |
| Coverage: Domain | coverlet | ≥ 90% line rate |
| Coverage: Infrastructure | coverlet | ≥ 70% line rate |
| Mutation test coverage (rules engine) | `QaMutationRulesTests` | All fixtures pass |
| E2E commercial close | `QaCommercialCloseE2ETests` | Full flow passes |
| API contract compatibility | `QaContractFirstApiTests` | All contract tests pass |
| Multi-tenant isolation | `QaMultiTenantIsolationTests` | All isolation tests pass |
| Security authorization | `QaAuthorizationSecurityTests` | All auth tests pass |
| Controlled degradation | `QaControlledDegradationTests` | 0 test failures |

### 4.2 Load Tests (run with API up)

| Scenario | Script | Pass Criterion |
|----------|--------|----------------|
| Intake smoke | `intake-analytics-load.js` (smoke) | failRate < 5%, p95 < 2s |
| Analytics smoke | `analytics-advanced-load.js` (smoke) | failRate < 5%, p95 < 2s |
| Email follow-up smoke | `email-followup-load.js` (smoke) | failRate < 5%, p95 < 500ms |

### 4.3 Security

| Check | Tool | Pass Criterion |
|-------|------|----------------|
| SAST scan | `.github/workflows/security-sast-dast.yml` | No critical findings |
| No hardcoded secrets | `run-quality-gate.ps1` step 4 | 0 secrets in source |
| Security headers present | `QaAuthorizationSecurityTests` | All header tests pass |
| OWASP ASVS baseline | `ia/security/asvs-baseline.md` | All must-have items compliant |

### 4.4 Frontend

| Check | Tool | Pass Criterion |
|-------|------|----------------|
| E2E Playwright | `tests/e2e/` | All 5/5 tests pass |
| Accessibility lint | ESLint a11y | 0 violations |
| Bundle budget | check-bundle-budget.mjs | < 380 KB |

---

## 5. Flakiness SLO (QA-18)

Before promoting to RC, verify flakiness metrics from the last 5 CI runs:

- **Target**: ≤ 5% of tests may flip between pass/fail across the last 5 runs.
- **Tool**: `run-quality-gate.ps1` STEP 6 compares TRX history.
- **Action**: Any test exceeding flakiness threshold must be fixed or marked `[Skip(reason)]` with a tracking issue before RC promotion.

---

## 6. RC Promotion Checklist

```
[ ] Fast gate passed on feature branch before merge
[ ] All merge commits are squashed to main/release branch
[ ] Full RC gate script passed: .\run-quality-gate.ps1
[ ] Load tests (smoke) passed against staging environment
[ ] Security scan passed (0 critical findings)
[ ] Flakiness SLO verified: ≤ 5%
[ ] CHANGELOG.md updated with this release's changes
[ ] ADRs updated for any architectural decisions made in this cycle
[ ] ia/05_progress.md updated with completed items
[ ] ia/mejoras.md checklist items marked [x] with evidence
[ ] DB migration scripts reviewed and tested on staging
[ ] Feature flags configured for new high-risk features
[ ] Rollback plan documented in release notes
[ ] Observability dashboards verified (alert thresholds active)
[ ] SLO targets confirmed active in production monitoring
```

---

## 7. Rollback Criteria

Automatically trigger rollback (blue/green swap or feature flag disable) if any of the following occur within 30 minutes of production deployment:

- Error rate > 5% on any top-10 endpoint for > 5 minutes.
- P95 latency > 5 seconds on intake or dashboard for > 2 minutes.
- Any 5xx error on `/health/ready`.
- Alert event created for `PoisonQueueDepth` with depth > 100.

---

## 8. Responsible Parties

| Role | Responsibility |
|------|---------------|
| QA Lead | Executes RC gate, signs off on checklist |
| Backend Lead | Owns coverage thresholds and test fixture quality |
| Frontend Lead | Owns Playwright E2E and bundle budget |
| Security | Reviews SAST results and ASVS compliance |
| Platform/DevOps | Executes load tests in staging, monitors rollback |

---

## 9. References

- [QA Traceability Matrix](./qa-traceability-matrix.md)
- [ASVS Baseline](../security/asvs-baseline.md)
- [Backup/Restore Drill](../ops/dat17-backup-restore-drill.md)
- [Incident Response Runbook](../security/incident-response-sec20.md)
- Quality Gate Script: `backend/tests/LoadTests/run-quality-gate.ps1`
