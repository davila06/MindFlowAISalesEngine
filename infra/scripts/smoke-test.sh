#!/usr/bin/env bash
# infra/scripts/smoke-test.sh
# ─────────────────────────────────────────────────────────────────────────────
# OPS-08 | Post-deploy smoke test suite.
#
# Usage:
#   bash infra/scripts/smoke-test.sh <FRONTEND_BASE_URL> <API_BASE_URL>
#
# Exit codes:
#   0 – all smoke checks passed
#   1 – one or more checks failed (caller should trigger rollback)
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

FRONTEND_URL="${1:-http://localhost:3100}"
API_URL="${2:-http://localhost:5165}"
TENANT_ID="${SMOKE_TENANT_ID:-smoke-tenant}"
MAX_LATENCY_MS="${SMOKE_MAX_LATENCY_MS:-3000}"

PASS=0
FAIL=0
RESULTS=()

# ── Helpers ──────────────────────────────────────────────────────────────────

check() {
  local name="$1"
  local expected_code="$2"
  local url="$3"
  shift 3
  local extra_args=("$@")

  local START; START=$(date +%s%3N)
  local HTTP_CODE
  HTTP_CODE=$(curl -o /dev/null -s -w "%{http_code}" \
    --max-time 10 \
    -H "X-Tenant-Id: ${TENANT_ID}" \
    "${extra_args[@]}" \
    "$url" || echo "000")
  local END; END=$(date +%s%3N)
  local LATENCY=$(( END - START ))

  local STATUS="PASS"
  if [ "$HTTP_CODE" != "$expected_code" ]; then
    STATUS="FAIL"
    FAIL=$(( FAIL + 1 ))
  elif [ "$LATENCY" -gt "$MAX_LATENCY_MS" ]; then
    STATUS="WARN"
    echo "  ⚠  [WARN] ${name}: HTTP ${HTTP_CODE} but latency ${LATENCY}ms > ${MAX_LATENCY_MS}ms"
    PASS=$(( PASS + 1 ))
    RESULTS+=("WARN  | ${name} | ${HTTP_CODE} | ${LATENCY}ms")
    return
  else
    PASS=$(( PASS + 1 ))
  fi

  if [ "$STATUS" = "FAIL" ]; then
    echo "  ✗  [FAIL] ${name}: expected HTTP ${expected_code}, got ${HTTP_CODE} (${LATENCY}ms)"
  else
    echo "  ✓  [PASS] ${name}: HTTP ${HTTP_CODE} (${LATENCY}ms)"
  fi

  RESULTS+=("${STATUS} | ${name} | ${HTTP_CODE} | ${LATENCY}ms")
}

# ── Backend health checks ─────────────────────────────────────────────────────

echo ""
echo "═══════════════════════════════════════════════════════"
echo "  MindFlow Smoke Tests"
echo "  Frontend: ${FRONTEND_URL}"
echo "  API:      ${API_URL}"
echo "  Tenant:   ${TENANT_ID}"
echo "═══════════════════════════════════════════════════════"
echo ""
echo "── Backend Health ───────────────────────────────────────"

check "health/ready"   "200" "${API_URL}/health/ready"
check "health/live"    "200" "${API_URL}/health/live"

echo ""
echo "── API Endpoint Availability ───────────────────────────"

check "GET /api/leads (index)"            "200" "${API_URL}/api/leads"
check "GET /api/pipeline/stages"          "200" "${API_URL}/api/pipeline/stages"
check "GET /api/dashboard"                "200" "${API_URL}/api/dashboard"
check "GET /api/rules"                    "200" "${API_URL}/api/rules"
check "GET /api/email/smtp/settings"      "200" "${API_URL}/api/email/smtp/settings"
check "GET /api/analytics/advanced/metrics" "200" "${API_URL}/api/analytics/advanced/metrics"
check "GET /api/ops/sre-summary"          "200" "${API_URL}/api/ops/sre-summary"

echo ""
echo "── Security headers ─────────────────────────────────────"

# X-Content-Type-Options must be present
HEADERS=$(curl -sI --max-time 5 "${API_URL}/health/ready" || echo "")
if echo "$HEADERS" | grep -qi "x-content-type-options"; then
  echo "  ✓  [PASS] X-Content-Type-Options header present"
  PASS=$(( PASS + 1 ))
else
  echo "  ✗  [FAIL] X-Content-Type-Options header missing"
  FAIL=$(( FAIL + 1 ))
fi

echo ""
echo "── Frontend ──────────────────────────────────────────────"

check "Frontend home (200)"  "200" "${FRONTEND_URL}/"
check "Frontend /leads"      "200" "${FRONTEND_URL}/leads"
check "Frontend /pipeline"   "200" "${FRONTEND_URL}/pipeline"

echo ""
echo "═══════════════════════════════════════════════════════"
echo "  Results: ${PASS} passed, ${FAIL} failed"
echo "═══════════════════════════════════════════════════════"
echo ""

for r in "${RESULTS[@]}"; do
  echo "  ${r}"
done

echo ""

if [ "$FAIL" -gt 0 ]; then
  echo "SMOKE TESTS FAILED – ${FAIL} check(s) did not pass."
  exit 1
fi

echo "All smoke tests passed."
exit 0
