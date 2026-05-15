#!/usr/bin/env bash
# infra/scripts/rollback.sh
# ─────────────────────────────────────────────────────────────────────────────
# OPS-07 | Automated rollback script.
#
# Usage:
#   bash infra/scripts/rollback.sh <environment> <target_version> <reason>
#
# Arguments:
#   environment     – staging | production
#   target_version  – semver tag to roll back to (e.g., "1.0.42")
#   reason          – audit reason string
#
# The script:
#   1. Logs the rollback event to infra/rollback-log/history.log.
#   2. Executes the cloud-provider-specific slot/revision reactivation.
#      (Replace the placeholder block with your actual provider commands.)
#   3. Waits for health check to confirm rollback succeeded.
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

ENVIRONMENT="${1:?environment required}"
TARGET_VERSION="${2:-previous}"
REASON="${3:-manual}"
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
LOG_FILE="infra/rollback-log/history.log"

echo ""
echo "══════════════════════════════════════════════════════"
echo "  MindFlow Rollback"
echo "  Environment  : ${ENVIRONMENT}"
echo "  Target       : v${TARGET_VERSION}"
echo "  Reason       : ${REASON}"
echo "  Time         : ${TIMESTAMP}"
echo "══════════════════════════════════════════════════════"

# ── Audit log ────────────────────────────────────────────────────────────────
mkdir -p "$(dirname "${LOG_FILE}")"
cat >> "${LOG_FILE}" <<ENTRY
${TIMESTAMP} | env=${ENVIRONMENT} | target=${TARGET_VERSION} | reason=${REASON} | actor=${GITHUB_ACTOR:-local}
ENTRY
echo "Audit entry written to ${LOG_FILE}"

# ── Execute rollback ──────────────────────────────────────────────────────────
# Replace this section with your cloud provider's deployment commands.
#
# Azure Container Apps example:
# ────────────────────────────────────────────────────────────────────────────
# REVISION=$(az containerapp revision list \
#   --name "novamind-mindflow-backend-${ENVIRONMENT}" \
#   --resource-group "${AZURE_RESOURCE_GROUP}" \
#   --query "[?contains(name, '${TARGET_VERSION}')].name | [0]" -o tsv)
#
# az containerapp ingress traffic set \
#   --name "novamind-mindflow-backend-${ENVIRONMENT}" \
#   --resource-group "${AZURE_RESOURCE_GROUP}" \
#   --revision-weight "${REVISION}=100"
# ────────────────────────────────────────────────────────────────────────────
#
# Kubernetes (kubectl) example:
# ────────────────────────────────────────────────────────────────────────────
# kubectl set image deployment/novamind-backend \
#   api="ghcr.io/${GITHUB_REPOSITORY}/backend:${TARGET_VERSION}" \
#   --namespace="${ENVIRONMENT}"
# kubectl rollout status deployment/novamind-backend --namespace="${ENVIRONMENT}" --timeout=120s
# ────────────────────────────────────────────────────────────────────────────

echo "PLACEHOLDER: Cloud-provider rollback commands go here."
echo "  → Activate revision/image tagged: ${TARGET_VERSION}"
echo "  → Shift 100%% traffic to that revision."

# ── Post-rollback health probe ────────────────────────────────────────────────
API_URL="${ROLLBACK_API_URL:-http://localhost:5165}"
echo ""
echo "Verifying health at ${API_URL}/health/ready ..."
for i in $(seq 1 24); do
  CODE=$(curl -o /dev/null -s -w "%{http_code}" --max-time 5 \
    "${API_URL}/health/ready" || echo "000")
  if [ "$CODE" = "200" ]; then
    echo "Health check PASSED after ${i} attempt(s)."
    exit 0
  fi
  echo "  Attempt ${i}/24: HTTP ${CODE} – waiting 5 s..."
  sleep 5
done

echo "ERROR: Post-rollback health check failed. Manual intervention required."
exit 1
