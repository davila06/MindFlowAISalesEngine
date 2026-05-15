# Production DB Provider PoC (ARC-13)

Status: Completed
Owner: Backend + Platform Engineering

## Objective

Select the target production multi-user provider for MindFlow SaaS based on measured evidence.

Candidates:
- SQL Server
- PostgreSQL

Current baseline:
- SQLite (development and current runtime)

## Evaluation Matrix

| Criterion | Weight | SQL Server | PostgreSQL |
|---|---:|---:|---:|
| Throughput (http_reqs/s, smoke) | 25 | 3.7129 | 3.7130 |
| p95 latency expected responses (ms) | 20 | 65.10 | 64.87 |
| Concurrency conflict behavior | 15 | Stable in smoke | Stable in smoke |
| Operational cost | 15 | Medium-high | Medium |
| Tooling/dev experience | 10 | Strong on Windows | Strong cross-platform |
| Backup/restore maturity | 10 | Mature | Mature |
| Migration complexity from SQLite | 5 | Medium | Medium |

## Benchmark Scenarios

1. Lead intake burst (`POST /api/leads/intake`).
2. Pipeline board high volume query.
3. Analytics advanced endpoints (`alert-events`, `heatmap`, `trends`).
4. Background job updates (follow-up, email dispatch, reminders).

## Work Plan

1. Prepare provider-specific connection profiles.
2. Seed equivalent dataset.
3. Run the same load profiles on each provider.
4. Capture throughput, p95/p99, error rate, lock/contention behavior.
5. Document findings and recommendation.
6. Register final decision in ADR.

## Execution Harness

- PoC runner: `backend/tests/LoadTests/run-db-provider-poc.ps1`
- Reuses existing k6 profile (`analytics-advanced-load.js`) to keep measurements comparable.
- Example runs:

```powershell
cd backend/tests/LoadTests
./run-db-provider-poc.ps1 -Provider sqlite -Mode smoke
./run-db-provider-poc.ps1 -Provider sqlserver -Mode smoke
./run-db-provider-poc.ps1 -Provider postgres -Mode smoke
```

## Deliverables

- Benchmark report with raw metrics.
- Decision ADR.
- Migration/cutover plan with rollback.

## Benchmark Artifacts

- `backend/tests/LoadTests/results/db-poc-sqlite-smoke-20260504-151000.json`
- `backend/tests/LoadTests/results/db-poc-sqlserver-smoke-20260504-151221.json`
- `backend/tests/LoadTests/results/db-poc-postgres-smoke-20260504-151314.json`

Full mode:
- `backend/tests/LoadTests/results/db-poc-sqlite-full-20260504-153218.json`
- `backend/tests/LoadTests/results/db-poc-sqlserver-full-20260504-154134.json`
- `backend/tests/LoadTests/results/db-poc-postgres-full-20260504-155332.json`

## Full-Mode Comparative Results

All providers were executed with the same full profile (`smoke + baseline + stress`) and reached the same functional bottleneck pattern in heavy analytics endpoints.

| Provider | checks | http_req_failed | http_reqs/s | p95 (all req, ms) | p95 expected_response (ms) |
|---|---:|---:|---:|---:|---:|
| SQLite | 1.33% | 98.67% | 151.8999 | 1.17 | 9068.79 |
| SQL Server (LocalDB) | 1.33% | 98.67% | 151.8912 | 1.54 | 8921.78 |
| PostgreSQL | 1.34% | 98.66% | 151.3464 | 1.85 | 8752.02 |

Observations:
1. The three providers show nearly identical throughput under full profile load.
2. The dominant issue remains timeout behavior in heavy analytics paths (`alert-events`, `heatmap`, `trends`), not provider-specific driver behavior.
3. PostgreSQL keeps the best p95 on expected successful responses in the full profile too.
4. Full runs crossed current k6 thresholds in all providers (`K6_EXIT=99`), confirming an endpoint/query optimization workstream is required independently of provider choice.

## Final Recommendation

Selected target provider for production: PostgreSQL.

Rationale:
1. Best p95 among expected responses in both smoke and full executed PoC sets.
2. Operational fit for SaaS multi-tenant workloads and ecosystem tooling.
3. Strong managed-service options and portability.

Note:
- All providers showed the same smoke failures concentrated in heavy analytics endpoints (`alert-events`, `heatmap`), indicating API/query optimization workstream remains independent of provider selection.
