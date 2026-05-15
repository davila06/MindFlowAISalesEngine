import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TENANT_ID = __ENV.TENANT_ID || 'default';
const WINDOW_HOURS = __ENV.WINDOW_HOURS || '168';
const ENDPOINT_FILTER = __ENV.ENDPOINT_FILTER || '';
const TARGET_ENDPOINTS = (__ENV.TARGET_ENDPOINTS || 'alert-events,heatmap,trends,tenant-summary,slo-status')
  .split(',')
  .map((x) => x.trim().toLowerCase())
  .filter((x) => x.length > 0);
const ALERT_EVENTS_PAGE_SIZE = Number(
  __ENV.ALERT_EVENTS_PAGE_SIZE || (__ENV.RUN_MODE === 'smoke' ? '25' : '100'),
);
const HEAVY_ENDPOINT_INTERVAL = Number(
  __ENV.HEAVY_ENDPOINT_INTERVAL || (__ENV.RUN_MODE === 'smoke' ? '1' : '3'),
);
const SLEEP_JITTER = Number(__ENV.SLEEP_JITTER || '0.35');

const profile = __ENV.RUN_MODE === 'smoke' ? 'smoke' : 'full';
const durationP95Threshold = __ENV.HTTP_P95_THRESHOLD || (profile === 'smoke' ? 'p(95)<1200' : 'p(95)<900');
const durationP99Threshold = __ENV.HTTP_P99_THRESHOLD || (profile === 'smoke' ? 'p(99)<2500' : 'p(99)<1400');
const errorRateThreshold = __ENV.HTTP_FAILED_THRESHOLD || (profile === 'smoke' ? 'rate<0.05' : 'rate<0.01');
const failuresRateThreshold = __ENV.FAILURES_THRESHOLD || (profile === 'smoke' ? 'rate<0.05' : 'rate<0.01');

const headers = {
  'Content-Type': 'application/json',
  'X-Tenant-Id': TENANT_ID,
};

const failures = new Rate('failures');

export const options = {
  scenarios: __ENV.RUN_MODE === 'smoke'
    ? {
        smoke: {
          executor: 'constant-vus',
          vus: Number(__ENV.SMOKE_VUS || 5),
          duration: __ENV.SMOKE_DURATION || '30s',
          gracefulStop: __ENV.GRACEFUL_STOP || '5s',
          exec: 'smokeSuite',
          tags: { profile: 'smoke' },
        },
      }
    : {
        smoke: {
          executor: 'constant-vus',
          vus: Number(__ENV.SMOKE_VUS || 5),
          duration: __ENV.SMOKE_DURATION || '30s',
          gracefulStop: __ENV.GRACEFUL_STOP || '5s',
          exec: 'smokeSuite',
          tags: { profile: 'smoke' },
        },
        baseline: {
          executor: 'ramping-vus',
          stages: [
            { duration: __ENV.BASELINE_RAMP_UP || '1m', target: Number(__ENV.BASELINE_TARGET_VUS || 25) },
            { duration: __ENV.BASELINE_HOLD || '2m', target: Number(__ENV.BASELINE_TARGET_VUS || 25) },
            { duration: __ENV.BASELINE_RAMP_DOWN || '1m', target: 0 },
          ],
          gracefulRampDown: __ENV.GRACEFUL_RAMP_DOWN || '5s',
          gracefulStop: __ENV.GRACEFUL_STOP || '5s',
          exec: 'baselineSuite',
          tags: { profile: 'baseline' },
          startTime: __ENV.BASELINE_START || '35s',
        },
        stress: {
          executor: 'ramping-vus',
          stages: [
            { duration: __ENV.STRESS_RAMP_UP || '1m', target: Number(__ENV.STRESS_TARGET_VUS || 75) },
            { duration: __ENV.STRESS_HOLD || '2m', target: Number(__ENV.STRESS_TARGET_VUS || 75) },
            { duration: __ENV.STRESS_RAMP_DOWN || '1m', target: 0 },
          ],
          gracefulRampDown: __ENV.GRACEFUL_RAMP_DOWN || '5s',
          gracefulStop: __ENV.GRACEFUL_STOP || '5s',
          exec: 'stressSuite',
          tags: { profile: 'stress' },
          startTime: __ENV.STRESS_START || '5m',
        },
      },
  thresholds: profile === 'smoke'
    ? {
        http_req_failed: [errorRateThreshold],
        failures: [failuresRateThreshold],
        'http_req_duration{expected_response:true}': [durationP95Threshold, durationP99Threshold],
      }
    : {
        http_req_failed: [errorRateThreshold],
        http_req_duration: [durationP95Threshold, durationP99Threshold],
        failures: [failuresRateThreshold],
      },
};

function get(name, path) {
  const res = http.get(`${BASE_URL}${path}`, {
    headers,
    tags: { endpoint: name },
    timeout: __ENV.REQ_TIMEOUT || '10s',
  });

  const ok = check(res, {
    [`status is 200 (${name})`]: (r) => r.status === 200,
  });

  failures.add(!ok);
  return res;
}

function readHeavyPaths() {
  const shouldRunHeavyEndpoints = HEAVY_ENDPOINT_INTERVAL <= 1 || (__ITER % HEAVY_ENDPOINT_INTERVAL === 0);

  if (TARGET_ENDPOINTS.includes('alert-events')) {
    get('alert-events', `/api/analytics/advanced/alert-events?endpointName=${encodeURIComponent(ENDPOINT_FILTER)}&page=1&pageSize=${ALERT_EVENTS_PAGE_SIZE}`);
  }

  if (TARGET_ENDPOINTS.includes('heatmap') && shouldRunHeavyEndpoints) {
    get('heatmap', `/api/analytics/advanced/alert-events/heatmap?endpointName=${encodeURIComponent(ENDPOINT_FILTER)}&windowHours=${WINDOW_HOURS}`);
  }

  if (TARGET_ENDPOINTS.includes('trends') && shouldRunHeavyEndpoints) {
    get('trends', `/api/analytics/advanced/alert-events/trends?endpointName=${encodeURIComponent(ENDPOINT_FILTER)}&metricName=AverageLatencyMs&windowHours=${WINDOW_HOURS}`);
  }

  if (TARGET_ENDPOINTS.includes('tenant-summary')) {
    get('tenant-summary', '/api/analytics/advanced/alert-events/tenant-summary');
  }

  if (TARGET_ENDPOINTS.includes('slo-status')) {
    get('slo-status', '/api/analytics/advanced/alert-events/slo-status');
  }
}

function pacedSleep(baseSeconds) {
  sleep(baseSeconds + (Math.random() * SLEEP_JITTER));
}

export function smokeSuite() {
  readHeavyPaths();
  pacedSleep(Number(__ENV.SMOKE_SLEEP_BASE || '1'));
}

export function baselineSuite() {
  readHeavyPaths();
  pacedSleep(Number(__ENV.BASELINE_SLEEP_BASE || '0.5'));
}

export function stressSuite() {
  readHeavyPaths();
  pacedSleep(Number(__ENV.STRESS_SLEEP_BASE || '0.25'));
}
