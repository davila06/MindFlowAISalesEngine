import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TENANT_ID = __ENV.TENANT_ID || 'default';
const PROFILE = __ENV.RUN_MODE === 'smoke' ? 'smoke' : 'full';

const headers = {
  'Content-Type': 'application/json',
  'X-Tenant-Id': TENANT_ID,
};

const failures = new Rate('failures');

const durationP95Threshold = __ENV.HTTP_P95_THRESHOLD || (PROFILE === 'smoke' ? 'p(95)<1800' : 'p(95)<1200');
const durationP99Threshold = __ENV.HTTP_P99_THRESHOLD || (PROFILE === 'smoke' ? 'p(99)<3000' : 'p(99)<1800');
const errorRateThreshold = __ENV.HTTP_FAILED_THRESHOLD || (PROFILE === 'smoke' ? 'rate<0.08' : 'rate<0.03');
const failuresRateThreshold = __ENV.FAILURES_THRESHOLD || (PROFILE === 'smoke' ? 'rate<0.08' : 'rate<0.03');

export const options = {
  scenarios: __ENV.RUN_MODE === 'smoke'
    ? {
        smoke: {
          executor: 'constant-vus',
          vus: Number(__ENV.SMOKE_VUS || 1),
          duration: __ENV.SMOKE_DURATION || '35s',
          gracefulStop: __ENV.GRACEFUL_STOP || '5s',
          exec: 'smokeSuite',
          tags: { profile: 'smoke' },
        },
      }
    : {
        smoke: {
          executor: 'constant-vus',
          vus: Number(__ENV.SMOKE_VUS || 1),
          duration: __ENV.SMOKE_DURATION || '35s',
          gracefulStop: __ENV.GRACEFUL_STOP || '5s',
          exec: 'smokeSuite',
          tags: { profile: 'smoke' },
        },
        baseline: {
          executor: 'ramping-vus',
          stages: [
            { duration: __ENV.BASELINE_RAMP_UP || '45s', target: Number(__ENV.BASELINE_TARGET_VUS || 12) },
            { duration: __ENV.BASELINE_HOLD || '90s', target: Number(__ENV.BASELINE_TARGET_VUS || 12) },
            { duration: __ENV.BASELINE_RAMP_DOWN || '45s', target: 0 },
          ],
          gracefulRampDown: __ENV.GRACEFUL_RAMP_DOWN || '5s',
          gracefulStop: __ENV.GRACEFUL_STOP || '5s',
          exec: 'baselineSuite',
          tags: { profile: 'baseline' },
          startTime: __ENV.BASELINE_START || '45s',
        },
        stress: {
          executor: 'ramping-vus',
          stages: [
            { duration: __ENV.STRESS_RAMP_UP || '45s', target: Number(__ENV.STRESS_TARGET_VUS || 30) },
            { duration: __ENV.STRESS_HOLD || '90s', target: Number(__ENV.STRESS_TARGET_VUS || 30) },
            { duration: __ENV.STRESS_RAMP_DOWN || '45s', target: 0 },
          ],
          gracefulRampDown: __ENV.GRACEFUL_RAMP_DOWN || '5s',
          gracefulStop: __ENV.GRACEFUL_STOP || '5s',
          exec: 'stressSuite',
          tags: { profile: 'stress' },
          startTime: __ENV.STRESS_START || '4m',
        },
      },
  thresholds: {
    http_req_failed: [errorRateThreshold],
    http_req_duration: [durationP95Threshold, durationP99Threshold],
    failures: [failuresRateThreshold],
  },
};

export function setup() {
  const payload = JSON.stringify({
    providerType: 'webhook',
    providerBaseUrl: 'https://mail.example.test/hooks/send',
    apiKey: 'load-test-token',
    host: '',
    port: 443,
    username: '',
    password: '',
    fromEmail: 'noreply@example.com',
    fromName: 'MindFlow Load',
    enableSsl: true,
  });

  const response = http.put(`${BASE_URL}/api/email/smtp-settings`, payload, {
    headers,
    tags: { endpoint: 'smtp-settings' },
    timeout: __ENV.REQ_TIMEOUT || '12s',
  });

  check(response, {
    'setup smtp settings is 200': (r) => r.status === 200,
  });
}

function post(name, path, body, expectedStatuses) {
  const response = http.post(`${BASE_URL}${path}`, body, {
    headers,
    tags: { endpoint: name },
    timeout: __ENV.REQ_TIMEOUT || '12s',
  });

  const ok = check(response, {
    [`status valid (${name})`]: (r) => expectedStatuses.includes(r.status),
  });

  failures.add(!ok);
  return response;
}

function get(name, path) {
  const response = http.get(`${BASE_URL}${path}`, {
    headers,
    tags: { endpoint: name },
    timeout: __ENV.REQ_TIMEOUT || '12s',
  });

  const ok = check(response, {
    [`status is 200 (${name})`]: (r) => r.status === 200,
  });

  failures.add(!ok);
  return response;
}

function uniqueLeadPayload() {
  const id = `${__VU}-${__ITER}-${Date.now()}`;
  const phoneSuffix = (Math.floor(Math.random() * 9000000) + 1000000).toString();

  return JSON.stringify({
    email: `load.${id}@example.test`,
    phone: `+1555${phoneSuffix}`,
    source: 'email-followup-load',
  });
}

function runFlow() {
  post('lead-intake', '/api/leads/intake', uniqueLeadPayload(), [201]);

  if (__ITER % Number(__ENV.DISPATCH_EVERY || 2) === 0) {
    post('dispatch-execute-due', '/api/email/dispatch/execute-due', null, [200]);
  }

  if (__ITER % Number(__ENV.KPI_EVERY || 5) === 0) {
    get('email-kpis', '/api/email/kpis');
  }

  if (__ITER % Number(__ENV.FOLLOWUP_JOBS_EVERY || 7) === 0) {
    get('followup-jobs', '/api/followup/jobs');
  }
}

export function smokeSuite() {
  runFlow();
  sleep(Number(__ENV.SMOKE_SLEEP_BASE || '1.4'));
}

export function baselineSuite() {
  runFlow();
  sleep(Number(__ENV.BASELINE_SLEEP_BASE || '0.4'));
}

export function stressSuite() {
  runFlow();
  sleep(Number(__ENV.STRESS_SLEEP_BASE || '0.2'));
}
