# Analytics Advanced Load Tests

This folder contains AO-18 load tests for analytics and observability endpoints.

It also contains EMF-14 load tests for email dispatch and follow-up batch flows.

## Targets

- GET /api/analytics/advanced/alert-events
- GET /api/analytics/advanced/alert-events/heatmap?windowHours=
- GET /api/analytics/advanced/alert-events/trends
- GET /api/analytics/advanced/alert-events/tenant-summary
- GET /api/analytics/advanced/alert-events/slo-status

## Email/Follow-up targets (EMF-14)

- PUT /api/email/smtp-settings
- POST /api/leads/intake
- POST /api/email/dispatch/execute-due
- GET /api/email/kpis
- GET /api/followup/jobs

## Scenarios

- smoke: low traffic sanity validation
- baseline: normal expected traffic profile
- stress: elevated traffic profile

## Prerequisites

1. API running locally or in a target environment.
2. k6 installed and available in PATH.
3. Seed data available for realistic trend and heatmap responses.

## Run

From this folder:

```powershell
./run-load-tests.ps1 -BaseUrl "http://localhost:5000" -TenantId "default" -EndpointFilter "api/percentile-ep"
```

Raw k6 command:

```powershell
$env:BASE_URL="http://localhost:5000"
$env:TENANT_ID="default"
k6 run --summary-export ./results/analytics-load.json ./analytics-advanced-load.js
```

Run email/follow-up load tests:

```powershell
./run-email-followup-load-tests.ps1 -BaseUrl "http://localhost:5000" -TenantId "default"
```

Raw email/follow-up k6 command:

```powershell
$env:BASE_URL="http://localhost:5000"
$env:TENANT_ID="default"
k6 run --summary-export ./results/email-followup-load.json ./email-followup-load.js
```

## Profiling por endpoint

Para aislar un endpoint especifico durante el diagnostico:

```powershell
$env:RUN_MODE="smoke"
$env:TARGET_ENDPOINTS="slo-status"
k6 run --summary-export ./results/analytics-load-slo.json ./analytics-advanced-load.js
```

Valores validos en `TARGET_ENDPOINTS`: `alert-events,heatmap,trends,tenant-summary,slo-status`.

Para estabilizar smoke en entornos SQLite locales:

- `ALERT_EVENTS_PAGE_SIZE` (default smoke: `25`, otros modos: `100`)
- `HEAVY_ENDPOINT_INTERVAL` (default smoke: `1`, otros modos: `3`) para muestrear `heatmap/trends` cada N iteraciones
- `SLEEP_JITTER` (default `0.35` segundos)
- `SMOKE_SLEEP_BASE`, `BASELINE_SLEEP_BASE`, `STRESS_SLEEP_BASE`

## Overrides de thresholds

El script aplica thresholds por modo:

- `RUN_MODE=smoke`: `http_req_failed rate<0.05`, `failures rate<0.05`, `p95<1200`, `p99<2500`
- `RUN_MODE!=smoke`: `http_req_failed rate<0.01`, `failures rate<0.01`, `p95<900`, `p99<1400`

Se pueden sobrescribir con variables:

- `HTTP_FAILED_THRESHOLD`
- `FAILURES_THRESHOLD`
- `HTTP_P95_THRESHOLD`
- `HTTP_P99_THRESHOLD`

## Output

- Text output in results/*.txt
- JSON summary in results/*.json

## Initial SLO targets

- error rate < 1%
- p95 latency < 900ms
- p99 latency < 1400ms

Adjust thresholds in analytics-advanced-load.js if production objectives change.
