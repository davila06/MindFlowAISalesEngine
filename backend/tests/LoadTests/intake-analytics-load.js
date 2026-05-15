/**
 * QA-06: Load tests for lead intake and analytics advanced endpoints.
 *
 * Covers:
 *   - POST /api/leads/intake          (write path, critical SLA)
 *   - GET  /api/dashboard/overview    (light analytics)
 *   - GET  /api/analytics/advanced/metrics
 *   - GET  /api/pipeline/stages
 *   - GET  /api/scoring/health or /health/live (liveness)
 *
 * Run modes (via RUN_MODE env var):
 *   smoke   — 3 VUs, 30s, validates endpoints respond and checks pass
 *   full    — ramping VUs (up to 30), 5m scenario, validates p95/p99/error-rate
 *
 * Usage:
 *   Smoke:  k6 run -e RUN_MODE=smoke intake-analytics-load.js
 *   Full:   k6 run -e RUN_MODE=full  intake-analytics-load.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// ── Configuration ─────────────────────────────────────────────────────────────
const BASE_URL   = __ENV.BASE_URL   || 'http://localhost:5000';
const TENANT_ID  = __ENV.TENANT_ID  || 'qa-load-tenant';
const PROFILE    = __ENV.RUN_MODE === 'smoke' ? 'smoke' : 'full';
const SLEEP_JITTER = Number(__ENV.SLEEP_JITTER || '0.3');

const P95_INTAKE   = __ENV.P95_INTAKE   || (PROFILE === 'smoke' ? 'p(95)<2500' : 'p(95)<1200');
const P95_ANALYTICS= __ENV.P95_ANALYTICS|| (PROFILE === 'smoke' ? 'p(95)<3000' : 'p(95)<1500');
const ERROR_RATE   = __ENV.ERROR_RATE   || (PROFILE === 'smoke' ? 'rate<0.05'  : 'rate<0.02');
const FAIL_RATE    = __ENV.FAIL_RATE    || (PROFILE === 'smoke' ? 'rate<0.05'  : 'rate<0.02');

// ── Custom metrics ────────────────────────────────────────────────────────────
const intakeTrend    = new Trend('intake_duration_ms', true);
const analyticsTrend = new Trend('analytics_duration_ms', true);
const failures       = new Rate('qa_failures');

// ── Scenarios ─────────────────────────────────────────────────────────────────
export const options = {
  scenarios: PROFILE === 'smoke'
    ? {
        smoke: {
          executor:    'constant-vus',
          vus:          Number(__ENV.SMOKE_VUS || 3),
          duration:     __ENV.SMOKE_DURATION || '30s',
          gracefulStop: '5s',
          exec:         'intakeAndAnalyticsSuite',
          tags:         { profile: 'smoke' },
        },
      }
    : {
        smoke: {
          executor:    'constant-vus',
          vus:          3,
          duration:     '30s',
          gracefulStop: '5s',
          exec:         'intakeAndAnalyticsSuite',
          tags:         { profile: 'smoke' },
          startTime:    '0s',
        },
        ramp_intake: {
          executor: 'ramping-vus',
          stages: [
            { duration: __ENV.RAMP_UP   || '1m',  target: Number(__ENV.INTAKE_TARGET_VUS || 20) },
            { duration: __ENV.HOLD      || '2m',  target: Number(__ENV.INTAKE_TARGET_VUS || 20) },
            { duration: __ENV.RAMP_DOWN || '30s', target: 0 },
          ],
          gracefulRampDown: '10s',
          gracefulStop:     '10s',
          exec:             'intakeSuite',
          tags:             { profile: 'intake-load' },
          startTime:        '0s',
        },
        analytics_baseline: {
          executor: 'constant-vus',
          vus:       Number(__ENV.ANALYTICS_VUS || 10),
          duration:  __ENV.ANALYTICS_DURATION || '3m',
          gracefulStop: '10s',
          exec:      'analyticsSuite',
          tags:      { profile: 'analytics-load' },
          startTime: '30s',
        },
      },

  thresholds: {
    http_req_failed:    [ERROR_RATE],
    qa_failures:        [FAIL_RATE],
    intake_duration_ms: [P95_INTAKE],
    analytics_duration_ms: [P95_ANALYTICS],
  },
};

// ── Shared headers ────────────────────────────────────────────────────────────
const headers = {
  'Content-Type': 'application/json',
  'X-Tenant-Id':  TENANT_ID,
  'X-User-Role':  'Admin',
};

// ── Utility ───────────────────────────────────────────────────────────────────
function randomEmail() {
  return `load_${__ITER}_${Date.now()}@mindflow.qa`;
}

function randomPhone() {
  return `+1${Math.floor(2_000_000_000 + Math.random() * 99_999_999)}`;
}

function jitter() {
  sleep(0.1 + Math.random() * SLEEP_JITTER);
}

// ── Intake scenario ───────────────────────────────────────────────────────────
export function intakeSuite() {
  const payload = JSON.stringify({
    email:    randomEmail(),
    phone:    randomPhone(),
    source:   'load-test',
    country:  'US',
    campaign: 'qa-load',
    channel:  'direct',
  });

  const res = http.post(`${BASE_URL}/api/leads/intake`, payload, {
    headers,
    tags: { name: 'lead_intake' },
  });

  intakeTrend.add(res.timings.duration);

  const ok = check(res, {
    'intake: status 201':     (r) => r.status === 201,
    'intake: has id':         (r) => { try { return !!JSON.parse(r.body).id; } catch { return false; } },
    'intake: has score':      (r) => { try { return JSON.parse(r.body).score !== undefined; } catch { return false; } },
    'intake: duration < 5s':  (r) => r.timings.duration < 5000,
  });

  if (!ok) failures.add(1);
  else      failures.add(0);

  jitter();
}

// ── Analytics scenario ────────────────────────────────────────────────────────
export function analyticsSuite() {
  // 1. Dashboard overview
  const overview = http.get(`${BASE_URL}/api/dashboard/overview`, {
    headers,
    tags: { name: 'dashboard_overview' },
  });

  analyticsTrend.add(overview.timings.duration);

  const ovCheck = check(overview, {
    'analytics: overview 200': (r) => r.status === 200,
    'analytics: has totalLeads': (r) => {
      try { return JSON.parse(r.body).totalLeads !== undefined; } catch { return false; }
    },
  });

  if (!ovCheck) failures.add(1);
  else          failures.add(0);

  // 2. Advanced metrics
  const metrics = http.get(`${BASE_URL}/api/analytics/advanced/metrics`, {
    headers,
    tags: { name: 'analytics_metrics' },
  });

  analyticsTrend.add(metrics.timings.duration);

  check(metrics, {
    'analytics: metrics 200': (r) => r.status === 200,
    'analytics: metrics duration < 3s': (r) => r.timings.duration < 3000,
  });

  jitter();
}

// ── Combined smoke suite ───────────────────────────────────────────────────────
export function intakeAndAnalyticsSuite() {
  intakeSuite();
  jitter();
  analyticsSuite();
}

// ── Default export (no explicit scenario) ────────────────────────────────────
export default function () {
  intakeAndAnalyticsSuite();
}
