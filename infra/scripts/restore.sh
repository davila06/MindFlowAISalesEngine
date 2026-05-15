#!/usr/bin/env bash
# infra/scripts/restore.sh
# ─────────────────────────────────────────────────────────────────────────────
# OPS-16 | Automated restore from an encrypted backup archive.
#
# Usage:
#   bash infra/scripts/restore.sh <backup_file> [target_db_path]
#
# Arguments:
#   backup_file     – path to .tar.gz.gpg file (or .tar.gz for unencrypted)
#   target_db_path  – destination for the restored database (default: /data/restore)
#
# Steps:
#   1. Verify checksum of backup file.
#   2. Decrypt (if .gpg) using GPG private key.
#   3. Extract archive to target directory.
#   4. Run integrity check on the restored SQLite database.
#   5. Print restore summary.
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

BACKUP_FILE="${1:?backup_file required}"
TARGET_DIR="${2:-/data/restore}"
WORK_DIR=$(mktemp -d)
trap 'rm -rf "$WORK_DIR"' EXIT

echo ""
echo "══════════════════════════════════════════════════════"
echo "  MindFlow Restore"
echo "  Source    : ${BACKUP_FILE}"
echo "  Target    : ${TARGET_DIR}"
echo "══════════════════════════════════════════════════════"

# ── 1. Verify checksum ────────────────────────────────────────────────────────
echo ""
echo "Step 1: Verify checksum"
CHECKSUM_FILE="${BACKUP_FILE%%.gpg}.sha256"
[ "${BACKUP_FILE}" = "${BACKUP_FILE%%.gpg}" ] && \
  CHECKSUM_FILE="${BACKUP_FILE%%.tar.gz}.sha256"

if [ -f "${CHECKSUM_FILE}" ]; then
  sha256sum --check "${CHECKSUM_FILE}"
  echo "  ✓  Checksum verified"
else
  echo "  ⚠  No .sha256 file found at ${CHECKSUM_FILE} – skipping integrity check"
fi

# ── 2. Decrypt ────────────────────────────────────────────────────────────────
echo ""
ARCHIVE_FILE="${WORK_DIR}/backup.tar.gz"

if [[ "${BACKUP_FILE}" == *.gpg ]]; then
  echo "Step 2: Decrypt GPG-encrypted archive"
  if command -v gpg &>/dev/null; then
    gpg --batch --yes --output "${ARCHIVE_FILE}" --decrypt "${BACKUP_FILE}"
    echo "  ✓  Decrypted"
  else
    echo "  ERROR: GPG not available but file is .gpg encrypted"
    exit 1
  fi
else
  echo "Step 2: No decryption needed (unencrypted archive)"
  cp "${BACKUP_FILE}" "${ARCHIVE_FILE}"
fi

# ── 3. Extract ────────────────────────────────────────────────────────────────
echo ""
echo "Step 3: Extract archive"
EXTRACT_DIR="${WORK_DIR}/extracted"
mkdir -p "${EXTRACT_DIR}"
tar -xzf "${ARCHIVE_FILE}" -C "${EXTRACT_DIR}"
echo "  ✓  Extracted contents:"
find "${EXTRACT_DIR}" -type f | while read -r f; do
  echo "      $(du -sh "$f" | cut -f1)  ${f#"${EXTRACT_DIR}/"}"
done

# ── 4. Integrity check ────────────────────────────────────────────────────────
echo ""
echo "Step 4: SQLite integrity check"
DB_FILE=$(find "${EXTRACT_DIR}" -name "*.db" | head -1 || echo "")
if [ -n "${DB_FILE}" ]; then
  if command -v sqlite3 &>/dev/null; then
    INTEGRITY=$(sqlite3 "${DB_FILE}" "PRAGMA integrity_check;" 2>&1 || echo "error")
    if [ "${INTEGRITY}" = "ok" ]; then
      echo "  ✓  Database integrity: ok"
    else
      echo "  ERROR: Database integrity check failed: ${INTEGRITY}"
      exit 1
    fi
    # Row count sanity check
    LEAD_COUNT=$(sqlite3 "${DB_FILE}" "SELECT COUNT(*) FROM Leads;" 2>/dev/null || echo "0")
    echo "  ✓  Leads in backup: ${LEAD_COUNT}"
  else
    echo "  ⚠  sqlite3 not available – skipping integrity check"
  fi
else
  echo "  ⚠  No .db file found in archive"
fi

# ── 5. Copy to target ─────────────────────────────────────────────────────────
echo ""
echo "Step 5: Copy restored files to ${TARGET_DIR}"
mkdir -p "${TARGET_DIR}"
cp -r "${EXTRACT_DIR}"/* "${TARGET_DIR}/"
echo "  ✓  Files restored to ${TARGET_DIR}"

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════════"
echo "  Restore complete"
echo "  Target: ${TARGET_DIR}"
echo "══════════════════════════════════════════════════════"
echo ""
echo "IMPORTANT: Before starting the application against the restored database:"
echo "  1. Stop the current application."
echo "  2. Replace the production database with ${TARGET_DIR}/snapshot/mindflow.db"
echo "  3. Run dotnet run (migrations will apply automatically)."
echo "  4. Verify /health/ready returns 200."
