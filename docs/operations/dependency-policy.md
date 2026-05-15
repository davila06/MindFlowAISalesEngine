# Dependency Security Policy — MindFlow AI Sales Engine

> Version: 1.0 | Owner: Platform Engineering | Last reviewed: 2026-05-04

---

## Purpose

This document defines the secure dependency update policy for the MindFlow platform, covering the .NET backend (NuGet) and Next.js frontend (npm). All changes must comply with this policy before being merged to `main`.

---

## 1. Vulnerability Severity SLA

| Severity | Update Deadline | Action |
|----------|----------------|--------|
| CRITICAL (CVSS 9.0–10.0) | **Within 24 hours** | Hotfix PR + emergency deployment |
| HIGH (CVSS 7.0–8.9) | **Within 7 days** | Fix PR + standard CD release |
| MEDIUM (CVSS 4.0–6.9) | **Within 30 days** | Bundled in next sprint patch |
| LOW (CVSS 0.1–3.9) | **Within 90 days** | Tracked in backlog |

---

## 2. Automated Enforcement

### 2.1 Pull Request Gate

Every PR to `main` or `develop` runs `.github/workflows/dependency-review.yml` which:

- Blocks merging if **any new HIGH or CRITICAL** vulnerable dependency is introduced.
- Reports the SPDX license of new dependencies and blocks non-approved licenses.
- Generates a full dependency inventory as a build artifact.

### 2.2 Dependabot

`.github/dependabot.yml` automatically opens PRs every Tuesday for:

| Ecosystem | Scope | PR Limit |
|-----------|-------|----------|
| GitHub Actions | All workflow actions | Unlimited |
| NuGet (backend) | Minor + patch grouped | 5 PRs/week |
| npm (frontend) | Grouped by ecosystem | 5 PRs/week |

Dependabot PRs **must not** be merged without passing the full CI quality gate.

---

## 3. Dependency Inventory

A full inventory is generated as an artifact on every run of `dependency-review.yml` and includes:

- `nuget-inventory.txt` — all direct + transitive NuGet packages
- `npm-inventory-direct.txt` — direct npm dependencies
- `nuget-outdated.txt` / `npm-outdated.txt` — packages with newer versions available

Artifacts are retained for **30 days** per run.

---

## 4. Approved License List

The following OSS licenses are approved for use in MindFlow:

- MIT
- Apache-2.0
- BSD-2-Clause
- BSD-3-Clause
- ISC
- 0BSD
- Unlicense

Any dependency with a license outside this list requires explicit written approval from the Platform Engineering Lead before being added.

---

## 5. Dependency Addition Process

1. **Check for known vulnerabilities**: run `npm audit` or `dotnet list package --vulnerable` locally.
2. **Verify license**: confirm the license is on the approved list.
3. **Minimize transitive dependencies**: prefer well-maintained packages with minimal dependency trees.
4. **Document in PR**: include the rationale for adding the dependency in the PR description.
5. **CI gate**: ensure the dependency-review workflow passes.

---

## 6. Emergency Vulnerability Response

When a CRITICAL vulnerability is discovered:

1. **Assess exposure**: determine if MindFlow uses the vulnerable code path.
2. **Open a hotfix PR** immediately with the patched version.
3. **Notify the team** via Slack `#security-alerts`.
4. **Deploy via emergency workflow**:
   ```bash
   gh workflow run cd-release.yml -f environment=production
   ```
5. **Post incident**: document in `ia/07_issues.md`.

---

## 7. Change Log

| Version | Date | Author | Change |
|---------|------|--------|--------|
| 1.0 | 2026-05-04 | Platform Engineering | Initial policy (OPS-17) |
