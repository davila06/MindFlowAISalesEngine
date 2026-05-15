# QA-19 — Release Candidate Test Plan

> Version: 1.0  
> Created: 2026-05-04  
> Owner: QA Lead / Tech Lead  
> Applies to: Every release promoted from `staging` → `production`

---

## 1. Purpose

This document defines the mandatory test gates that must be executed and passed
before any build is promoted as a Release Candidate (RC) for NovaMind MindFlow.

A build that fails **any Required gate** must NOT be tagged as RC.

---

## 2. Entry Criteria

Before beginning RC testing, verify:

- [ ] Feature branch is merged to `main` / `release/*`
- [ ] CI pipeline is green (build + unit tests)
- [ ] Staging environment is provisioned with latest migrations
- [ ] Test data is seeded (use `QaTestDataBuilder` patterns)
- [ ] Application Insights / observability is connected to staging

---

## 3. Gate Matrix

### Gate 1 — Build & Compile (Required)

| Check | Command | Pass Criteria |
|-------|---------|--------------|
| .NET Release build | `dotnet build MindFlow.Backend.sln -c Release` | Exit 0, 0 errors |
| Frontend build + budget | `npm run build:verified` | Exit 0, bundle within budget |

### Gate 2 — Unit & Integration Tests (Required)

| Suite | Command | Pass Criteria |
|-------|---------|--------------|
| All backend tests | `dotnet test tests/Api.Tests/Api.Tests.csproj -c Release` | 100% pass, 0 failures |
| Contract-first tests | `--filter QaContractFirstTests` | All pass |
| Multi-tenant isolation | `--filter QaMultiTenantIsolationTests` | All pass |
| Authorization matrix | `--filter QaAuthorizationMatrixTests` | All pass |
| E2E commercial close | `--filter QaCommercialCloseE2ETests` | All pass |
| API backward compat | `--filter QaApiContractCompatibilityTests` | All pass |

### Gate 3 — Mutation & Regression (Required)

| Suite | Command | Pass Criteria |
|-------|---------|--------------|
| Rules mutation tests | `--filter QaMutationRulesCriticalTests` | All pass |
| Concurrency tests | `--filter QaConcurrencyPipelineTests` | All pass |

### Gate 4 — Security (Required)

| Check | Command | Pass Criteria |
|-------|---------|--------------|
| Security hardening tests | `--filter SecurityHardeningEndpointTests` | All pass |
| SAST scan | `.github/workflows/security-sast-dast.yml` | No critical findings |
| Dependency audit | `dotnet list package --vulnerable` | No high/critical vulns |

### Gate 5 — Smoke (Required for staging)

| Check | Command | Pass Criteria |
|-------|---------|--------------|
| Live smoke test | `run-quality-gate.ps1 -BaseUrl https://staging.mindflow.io` | All endpoints 2xx |
| Health checks | `GET /health/live` + `/health/ready` | 200 OK |

### Gate 6 — Load (Recommended, Required before major release)

| Suite | Command | Pass Criteria |
|-------|---------|--------------|
| Intake smoke load | `run-intake-analytics-load-tests.ps1 -Mode smoke` | 0% error rate, p95 < 2.5s |
| Analytics smoke load | `run-load-tests.ps1 -Mode smoke` | < 5% error rate |
| Email follow-up smoke | `run-email-followup-load-tests.ps1 -Mode smoke` | 0% error rate |

### Gate 7 — Observability & Quality Report (Recommended)

| Check | Command | Pass Criteria |
|-------|---------|--------------|
| QA health report | `GET /api/dashboard/qa-health-report` | Grade ≥ B (score ≥ 80) |
| Flakiness SLO | `analyze-test-flakiness.ps1` | No test > 5% flakiness |
| Observability tests | `--filter QaObservabilityAlertsTests` | All pass |

---

## 4. RC Promotion Checklist

Before tagging the build as RC:

- [ ] Gate 1: Build passes ✓
- [ ] Gate 2: All tests green ✓
- [ ] Gate 3: Mutation & regression green ✓
- [ ] Gate 4: Security gates green ✓
- [ ] Gate 5: Smoke tests green on staging ✓
- [ ] Gate 6: Load smoke acceptable ✓ (or waiver documented)
- [ ] Gate 7: QA health grade ≥ B ✓ (or waiver documented)
- [ ] Release notes drafted
- [ ] Changelog updated
- [ ] ADRs updated for any new architectural decisions
- [ ] Runbooks updated for any new operational endpoints

**RC approval sign-off:** ___________________  Date: ___________

---

## 5. RC Regression Scope

Every RC must re-run the following modules end-to-end:

1. Lead intake → scoring → assignment
2. Pipeline move → history → WIP limits
3. Rules create → activate → evaluate → audit
4. Email template → dispatch → retry → KPIs
5. Proposals → PDF → status → onboarding tasks
6. Analytics → dashboard → observability → alerts
7. Multi-tenant data isolation (3-tenant scenario)

---

## 6. Rollback Criteria

Initiate rollback if any of the following are detected post-deploy:

- Error rate > 5% on any API endpoint within first 15 minutes
- Health check returning 503 for > 2 consecutive minutes
- Any P0 security finding (data leak, privilege escalation)
- Data integrity violation (cross-tenant leak confirmed)

Rollback procedure: `ia/ops/dat18-production-db-migration.md` § Rollback

---

## 7. Automated Gate Runner

Run the full gate automatically:

```powershell
# Full gate (build + tests + smoke)
Set-Location "c:\NovaMind - MindFlow AI sales engine\backend\tests"
.\run-quality-gate.ps1 -BaseUrl https://staging.mindflow.io -Configuration Release
```

Exit code `0` = all gates passed. Exit code `1` = one or more gates failed.

---

## 8. Reference Documents

- `ia/qa/traceability-matrix.md` — Req → Test mapping
- `ia/security/asvs-baseline.md` — Security compliance baseline
- `ia/security/incident-response-sec20.md` — Incident response runbook
- `ia/ops/dat17-backup-restore-drill.md` — Backup/restore procedure
- `ia/ops/dat18-production-db-migration.md` — Migration & rollback runbook
