# Disaster Recovery Plan — MindFlow AI Sales Engine

> Version: 1.0 | Owner: Platform Engineering | Last reviewed: 2026-05-04

---

## 1. Scope and Objectives

| Objective | Target |
|-----------|--------|
| Recovery Time Objective (RTO) | ≤ 1 hour |
| Recovery Point Objective (RPO) | ≤ 6 hours (automated backups every 6 h) |
| Availability SLO | 99.5% monthly uptime |
| Geographic scope | Single-region primary + cross-region backup storage |

This plan covers the full MindFlow platform: .NET 9 API backend, Next.js frontend, SQLite persistence, and all GitHub Actions automation.

---

## 2. Architecture Overview

```
┌─────────────────────────────────────────┐
│            Production Environment        │
│                                         │
│  ┌──────────────┐   ┌────────────────┐  │
│  │  Next.js FE   │   │  .NET 9 API    │  │
│  │  (Container) │◄──│  (Container)   │  │
│  └──────────────┘   └───────┬────────┘  │
│                             │           │
│                     ┌───────▼────────┐  │
│                     │  SQLite (disk) │  │
│                     └───────┬────────┘  │
└─────────────────────────────┼───────────┘
                              │ Daily encrypted backup
                              ▼
                    ┌──────────────────┐
                    │  Blob Storage    │
                    │  (geo-redundant) │
                    └──────────────────┘
```

---

## 3. Incident Severity Levels

| Level | Description | RTO | Notification |
|-------|-------------|-----|-------------|
| SEV-1 | Full service outage | 30 min | Immediate – all engineers |
| SEV-2 | Partial degradation (>20% error rate) | 1 hour | On-call engineer |
| SEV-3 | Performance degradation (latency >3×) | 4 hours | Ops team |
| SEV-4 | Non-critical anomaly | Next business day | Ticket |

---

## 4. Recovery Runbooks

### 4.1 Database Corruption or Loss (SEV-1)

```bash
# 1. Stop the application
#    az containerapp update --name mindflow-backend --min-replicas 0

# 2. Identify the latest clean backup
ls /backups/mindflow-production-*.gpg | sort | tail -5

# 3. Download backup from remote storage (if local copy unavailable)
az storage blob download \
  --account-name <STORAGE_ACCOUNT> \
  --container-name backups-production \
  --name mindflow-production-<TIMESTAMP>.tar.gz.gpg \
  --file /tmp/latest-backup.tar.gz.gpg

# 4. Restore
bash infra/scripts/restore.sh /tmp/latest-backup.tar.gz.gpg /data/restored

# 5. Replace database
cp /data/restored/snapshot/mindflow.db /data/mindflow.db

# 6. Start application
#    az containerapp update --name mindflow-backend --min-replicas 1

# 7. Verify health
curl https://api.novamind.ai/health/ready

# 8. Run smoke tests
bash infra/scripts/smoke-test.sh https://app.novamind.ai https://api.novamind.ai
```

### 4.2 Application Rollback (SEV-1 / SEV-2)

```bash
# Trigger automated rollback via GitHub Actions
# (requires production environment approval)
gh workflow run rollback-health.yml \
  -f environment=production \
  -f target_version=<PREVIOUS_VERSION> \
  -f reason="<INCIDENT_DESCRIPTION>"

# Monitor rollback progress
gh run watch

# Or manual rollback (cloud-provider-specific)
# Azure Container Apps:
# az containerapp revision activate \
#   --name mindflow-backend \
#   --resource-group mindflow-prod-rg \
#   --revision mindflow-backend--<PREV_REVISION>
```

### 4.3 Infrastructure Failure (SEV-1)

1. **Assess scope** using Azure Resource Health or `/health/ready`.
2. **Activate standby environment** (if pre-provisioned) or deploy to alternate region.
3. **Update DNS** to point to alternate endpoint.
4. **Verify backup storage** is accessible from alternate region.
5. **Restore from latest backup** following runbook 4.1.
6. **Send status update** to stakeholders within 15 min.

### 4.4 CI/CD Pipeline Failure (SEV-3)

- Hotfix deployments can be executed manually via:
  ```bash
  gh workflow run cd-release.yml -f environment=production
  ```
- Bypass CI gates (emergency only, requires two-person approval):
  ```bash
  # Tag a specific commit and push to trigger CD
  git tag v<VERSION>-hotfix
  git push origin v<VERSION>-hotfix
  ```

---

## 5. Communication Plan

| Audience | Channel | SLA |
|----------|---------|-----|
| Engineering team | Slack #incidents | Immediate |
| Product management | Email + Slack | Within 30 min |
| Customers (SEV-1 only) | Status page + email | Within 1 hour |

**Status page**: Update https://status.novamind.ai within 30 min of SEV-1/SEV-2 declaration.

---

## 6. Backup Policy Summary

| Type | Frequency | Retention | Storage |
|------|-----------|-----------|---------|
| Full database backup | Every 6 hours | 30 days local / 1 year remote | Azure Blob (LRS → GRS) |
| Daily restore verification | Daily 02:00 UTC | Test artifacts: 7 days | GitHub Actions artifacts |
| Config snapshot | Every backup | Same as database | Same container |

All backups are GPG-encrypted with key `ops@novamind.ai`. Keys are stored in Azure Key Vault, not in source control.

---

## 7. Testing Schedule

| Test | Frequency | Owner | Last Tested |
|------|-----------|-------|-------------|
| Restore drill (non-prod) | Monthly | Platform Engineering | — |
| Blue/green failover | Every deployment | Automated | — |
| SEV-1 tabletop exercise | Quarterly | Eng + Product | — |
| Full DR simulation | Semi-annual | Platform Engineering | — |

---

## 8. Contact Directory

| Role | Contact |
|------|---------|
| Platform Engineering Lead | Set in team roster |
| On-call Engineer | PagerDuty rotation |
| Azure Support | Portal case or +1-800-MICROSOFT |

---

## 9. Change Log

| Version | Date | Author | Change |
|---------|------|--------|--------|
| 1.0 | 2026-05-04 | Platform Engineering | Initial DR plan (OPS-11) |
