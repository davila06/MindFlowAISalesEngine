# Platform Patching Cadence — MindFlow AI Sales Engine

> Version: 1.0 | Owner: Platform Engineering | Last reviewed: 2026-05-04

---

## Purpose

This document defines the patching cadence for all dependencies and platform components used by MindFlow, ensuring that security patches are applied promptly and that dependencies remain current.

---

## 1. Patching Schedule

| Component | Cadence | Day | Automated? |
|-----------|---------|-----|-----------|
| GitHub Actions | Weekly | Tuesday | Yes — Dependabot |
| NuGet packages (minor/patch) | Weekly | Tuesday | Yes — Dependabot (grouped) |
| npm packages (minor/patch) | Weekly | Tuesday | Yes — Dependabot (grouped) |
| Major version upgrades | Quarterly | Planned sprint | Manual review required |
| .NET runtime (LTS releases) | Semi-annual | Planned sprint | Manual |
| Node.js LTS | Semi-annual | Planned sprint | Manual |
| OS base images (Docker) | Monthly | First Tuesday | Manual or Renovate |

---

## 2. Automated Tooling

### 2.1 Dependabot Weekly PRs

Dependabot creates PRs every Tuesday at 07:00 UTC. The team reviews and merges by end of week.

All Dependabot PRs are labeled:
- `patching` + ecosystem label (`dotnet`, `npm`, `github-actions`)
- Assigned to the responsible team (`backend-team`, `frontend-team`, `ops-team`)

### 2.2 Weekly Staleness Report

`.github/workflows/dependency-review.yml` runs every Tuesday and:
1. Generates an outdated-packages report (NuGet + npm).
2. If any packages are outdated, automatically opens a tracking issue labeled `[OPS-18]`.

---

## 3. Priority Matrix

| Update Type | Priority | Target Time | Review Required |
|-------------|----------|-------------|----------------|
| Security patch (CRITICAL) | P0 | 24 hours | 1 engineer |
| Security patch (HIGH) | P1 | 7 days | 1 engineer |
| Security patch (MEDIUM) | P2 | 30 days | Team review |
| Functional bug fix | P2 | Next sprint | Team review |
| Minor version (features) | P3 | Monthly batch | Team review |
| Major version (breaking) | P4 | Quarterly | Architecture review |

---

## 4. Process for Applying Patches

### Standard Patch (Dependabot PR)

1. Dependabot opens a PR with the version bump.
2. CI runs: `quality-fullstack.yml` + `dependency-review.yml`.
3. Engineer reviews diff and test results.
4. If all checks pass → approve + squash merge.
5. CD pipeline deploys to staging → production (standard flow).

### Emergency Security Patch (CRITICAL/HIGH)

1. Engineer creates a hotfix branch from `main`.
2. Applies the version bump manually.
3. Runs tests locally:
   ```bash
   cd backend && dotnet test tests/Api.Tests/Api.Tests.csproj -c Release
   cd frontend && npm run build:verified
   ```
4. Opens PR with `[SECURITY]` prefix in title.
5. Requires 1 reviewer approval (emergency override for 2-person rule allowed only for CRITICAL).
6. Deploys immediately via `cd-release.yml`.

---

## 5. .NET Runtime Upgrade Process

When a new .NET LTS version is released:

1. **Assess**: review breaking changes in official release notes.
2. **Test**: upgrade in a feature branch; run full test suite.
3. **Update base image**: modify `Dockerfile` in `infra/docker/`.
4. **Update CI**: change `dotnet-version` in all GitHub Actions steps.
5. **Deploy to staging**: validate with smoke tests + manual QA.
6. **Deploy to production**: via standard blue/green release.
7. **Document**: add ADR in `ia/06_decisions.md`.

---

## 6. Compliance Audit

The weekly patching report is stored as a GitHub Actions artifact for **90 days**. This provides an audit trail showing:
- Which packages were outdated each week
- When patches were applied (via PR merge dates)
- Any exceptions granted with documented justification

---

## 7. Roles and Responsibilities

| Role | Responsibility |
|------|---------------|
| Platform Engineering Lead | Reviews major/runtime upgrades; owns this document |
| Backend Team | Reviews and merges NuGet Dependabot PRs |
| Frontend Team | Reviews and merges npm Dependabot PRs |
| All Engineers | Must not merge Dependabot PRs without CI green |

---

## 8. Change Log

| Version | Date | Author | Change |
|---------|------|--------|--------|
| 1.0 | 2026-05-04 | Platform Engineering | Initial patching cadence (OPS-18) |
