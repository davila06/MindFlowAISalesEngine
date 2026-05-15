#!/usr/bin/env bash
# infra/scripts/backup.sh
# ─────────────────────────────────────────────────────────────────────────────
# OPS-16 | Encrypted backup of SQLite database and config files.
#
# Usage:
#   bash infra/scripts/backup.sh [environment] [backup_root]
#
# Arguments:
#   environment   – dev | staging | production (default: production)
#   backup_root   – destination directory for backup archives (default: /backups)
#
# Dependencies: gpg (for encryption), aws s3 / az storage (for remote upload)
#
# Produces:
#   <backup_root>/mindflow-<env>-<timestamp>.tar.gz.gpg
#   <backup_root>/mindflow-<env>-<timestamp>.sha256
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

ENVIRONMENT="${1:-production}"
BACKUP_ROOT="${2:-/backups}"
TIMESTAMP=$(date -u +"%Y%m%dT%H%M%SZ")
BACKUP_NAME="mindflow-${ENVIRONMENT}-${TIMESTAMP}"
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT

# ── Configuration ──────────────────────────────────────────────────────────
DB_PATH="${MINDFLOW_DB_PATH:-/data/mindflow-${ENVIRONMENT}.db}"
CONFIG_DIR="${MINDFLOW_CONFIG_DIR:-/app/config}"
BACKUP_RECIPIENT="${BACKUP_GPG_RECIPIENT:-ops@novamind.ai}"
RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"

echo ""
echo "══════════════════════════════════════════════════════"
echo "  MindFlow Backup"
echo "  Environment : ${ENVIRONMENT}"
echo "  Timestamp   : ${TIMESTAMP}"
echo "  Destination : ${BACKUP_ROOT}"
echo "══════════════════════════════════════════════════════"

mkdir -p "${BACKUP_ROOT}"

# ── 1. Snapshot database with WAL checkpoint ─────────────────────────────────
echo ""
echo "Step 1: SQLite WAL checkpoint + snapshot"
SNAPSHOT_DIR="${WORK_DIR}/snapshot"
mkdir -p "${SNAPSHOT_DIR}"

if [ -f "${DB_PATH}" ]; then
  # Force WAL checkpoint to flush pending writes
  sqlite3 "${DB_PATH}" "PRAGMA wal_checkpoint(TRUNCATE);" 2>/dev/null || \
    echo "  ⚠  WAL checkpoint skipped (sqlite3 not available – using file copy)"

  cp "${DB_PATH}" "${SNAPSHOT_DIR}/mindflow.db"
  [ -f "${DB_PATH}-wal" ] && cp "${DB_PATH}-wal" "${SNAPSHOT_DIR}/mindflow.db-wal" || true
  [ -f "${DB_PATH}-shm" ] && cp "${DB_PATH}-shm" "${SNAPSHOT_DIR}/mindflow.db-shm" || true
  echo "  ✓  Database snapshot created ($(du -sh "${SNAPSHOT_DIR}/mindflow.db" | cut -f1))"
else
  echo "  ⚠  Database not found at ${DB_PATH} – creating empty placeholder"
  echo "BACKUP_PLACEHOLDER" > "${SNAPSHOT_DIR}/mindflow.db"
fi

# ── 2. Include environment config (non-secret)  ──────────────────────────────
echo ""
echo "Step 2: Collect configuration artifacts"
CONFIG_SNAPSHOT="${WORK_DIR}/config"
mkdir -p "${CONFIG_SNAPSHOT}"

# Copy non-sensitive config files (secrets are in Key Vault, not files)
for f in appsettings.json appsettings.Staging.json appsettings.Production.json; do
  SRC="${CONFIG_DIR}/${f}"
  [ -f "${SRC}" ] && cp "${SRC}" "${CONFIG_SNAPSHOT}/" && echo "  ✓  ${f}" || true
done

# ── 3. Create archive ─────────────────────────────────────────────────────────
echo ""
echo "Step 3: Create compressed archive"
ARCHIVE="${WORK_DIR}/${BACKUP_NAME}.tar.gz"
tar -czf "${ARCHIVE}" -C "${WORK_DIR}" snapshot config
echo "  ✓  Archive: $(du -sh "${ARCHIVE}" | cut -f1)"

# ── 4. Encrypt with GPG ───────────────────────────────────────────────────────
echo ""
echo "Step 4: Encrypt archive (recipient: ${BACKUP_RECIPIENT})"
ENCRYPTED_ARCHIVE="${BACKUP_ROOT}/${BACKUP_NAME}.tar.gz.gpg"

if command -v gpg &>/dev/null; then
  gpg --batch --yes \
    --trust-model always \
    --recipient "${BACKUP_RECIPIENT}" \
    --output "${ENCRYPTED_ARCHIVE}" \
    --encrypt "${ARCHIVE}"
  echo "  ✓  Encrypted: ${ENCRYPTED_ARCHIVE}"
else
  echo "  ⚠  GPG not available – storing unencrypted (acceptable only in dev)"
  cp "${ARCHIVE}" "${BACKUP_ROOT}/${BACKUP_NAME}.tar.gz"
  ENCRYPTED_ARCHIVE="${BACKUP_ROOT}/${BACKUP_NAME}.tar.gz"
fi

# ── 5. Generate checksum ──────────────────────────────────────────────────────
echo ""
echo "Step 5: Generate SHA-256 checksum"
CHECKSUM_FILE="${BACKUP_ROOT}/${BACKUP_NAME}.sha256"
sha256sum "${ENCRYPTED_ARCHIVE}" > "${CHECKSUM_FILE}"
echo "  ✓  Checksum: $(cat "${CHECKSUM_FILE}")"

# ── 6. Upload to remote storage ───────────────────────────────────────────────
echo ""
echo "Step 6: Upload to remote storage"
if [ -n "${AZURE_STORAGE_ACCOUNT:-}" ]; then
  echo "  Uploading to Azure Blob Storage..."
  az storage blob upload \
    --account-name "${AZURE_STORAGE_ACCOUNT}" \
    --container-name "backups-${ENVIRONMENT}" \
    --name "${BACKUP_NAME}.tar.gz.gpg" \
    --file "${ENCRYPTED_ARCHIVE}" \
    --auth-mode login 2>&1 | grep -v "^$" | head -5
  echo "  ✓  Uploaded to Azure Blob Storage"
elif [ -n "${AWS_S3_BUCKET:-}" ]; then
  echo "  Uploading to S3..."
  aws s3 cp "${ENCRYPTED_ARCHIVE}" "s3://${AWS_S3_BUCKET}/backups/${ENVIRONMENT}/" \
    --sse aws:kms 2>&1 | head -3
  echo "  ✓  Uploaded to S3"
else
  echo "  ⚠  No remote storage configured (set AZURE_STORAGE_ACCOUNT or AWS_S3_BUCKET)"
fi

# ── 7. Prune old local backups ────────────────────────────────────────────────
echo ""
echo "Step 7: Prune local backups older than ${RETENTION_DAYS} days"
find "${BACKUP_ROOT}" -name "mindflow-${ENVIRONMENT}-*.gpg" \
  -mtime "+${RETENTION_DAYS}" -delete 2>/dev/null && \
  echo "  ✓  Old backups pruned" || echo "  ⚠  Prune skipped (no old backups or find error)"

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  Backup complete: ${BACKUP_NAME}"
echo "  Archive:  ${ENCRYPTED_ARCHIVE}"
echo "  Checksum: ${CHECKSUM_FILE}"
echo "══════════════════════════════════════════════════════"
