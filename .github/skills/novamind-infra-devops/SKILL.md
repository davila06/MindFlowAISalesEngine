---
name: novamind-infra-devops
description: Infra and DevOps architecture conventions for NovaMind MindFlow on Azure. Defines Bicep module boundaries, CI/CD pipeline structure, secure configuration, environment promotion, and operational controls. WHEN: create bicep files, add appservice.bicep, add sql.bicep, add storage.bicep, add keyvault.bicep, create Azure pipeline yml, backend-ci, frontend-ci, release pipeline, setup environments dev staging prod, configure secrets, key vault integration, deployment strategy, infra module for novamind, devops for mindflow.
invocable: false
---

# NovaMind Infra and DevOps

## Goal

Provide a repeatable, secure, and maintainable deployment model for the NovaMind platform across environments.

Platform baseline: Azure App Service + Azure SQL + Azure Storage + Azure Key Vault.

## Canonical Structure

```
/infra
├── bicep
│   ├── appservice.bicep
│   ├── sql.bicep
│   ├── storage.bicep
│   └── keyvault.bicep
├── pipelines
│   ├── backend-ci.yml
│   ├── frontend-ci.yml
│   └── release.yml
├── scripts
│   ├── seed-db.sql
│   └── migrate.sh
└── README.md
```

## Non-Negotiable Principles

1. Use Infrastructure as Code first. No manual portal-only configuration for managed resources.
2. Keep resource definitions modular by concern: compute, data, storage, secrets.
3. Never store secrets in repo files or pipeline yaml. Use Key Vault references.
4. Separate build and release concerns. Build pipelines produce immutable artifacts; release pipeline deploys artifacts.
5. Promotion is environment-based: dev -> staging -> prod with approvals before production.
6. App configuration is externalized and environment-specific.
7. Multi-tenant readiness is enforced via naming, tagging, and configuration boundaries.

## Bicep Conventions

- Keep one file per major resource concern in infra/bicep.
- Expose required outputs from each module and consume via explicit parameters.
- Use parameter files per environment (for example: dev, staging, prod).
- Enforce secure defaults:
  - Key Vault purge protection enabled.
  - Storage public access disabled unless explicitly approved.
  - TLS minimum versions enforced where applicable.
- Tag all resources with minimum tags:
  - app=novamind
  - system=mindflow
  - environment=<env>
  - owner=<team>
  - costCenter=<value>

## Pipeline Conventions

### backend-ci.yml

Purpose: compile, test, and publish backend artifact.

Required stages:
1. Restore dependencies
2. Build
3. Unit and integration tests
4. Publish artifact

### frontend-ci.yml

Purpose: install, lint, test, and build frontend artifact.

Required stages:
1. Install dependencies
2. Lint and type-check
3. Tests
4. Build static/server artifact

### release.yml

Purpose: deploy infra and app artifacts in controlled order.

Deployment order:
1. Validate IaC (preview/what-if equivalent)
2. Provision/update infra modules
3. Deploy backend artifact
4. Deploy frontend artifact
5. Run smoke checks and health checks
6. Optional DB migration/seed task by environment policy

## Environment Model

- dev: rapid iteration, lower cost, relaxed scaling.
- staging: production-like validation and release candidate verification.
- prod: strict approvals, rollback strategy, full observability.

Do not mix environment secrets or artifacts.

## Secrets and Configuration

- Secrets source of truth: Key Vault.
- CI/CD identity should use least privilege and managed identity/service connection.
- App Service settings should reference Key Vault secrets where possible.
- Never log sensitive values in pipeline output.

## Observability and Reliability

- Enable platform and app logs for all environments.
- Add baseline health endpoints and post-deploy smoke checks.
- Track deployment metadata (version, commit, timestamp) in release annotations.
- Define rollback process for failed releases.

## Operational Checklist

Before merge:
- IaC passes validation.
- Build, lint, and tests pass.
- Security scan passes.

Before production deployment:
- Staging deployment validated.
- Change approval captured.
- Rollback plan confirmed.

After deployment:
- Health checks green.
- Key business flows verified.
- Error rate and latency within baseline.

## Anti-Patterns

- Deploying infra manually in portal without codifying changes.
- Committing secrets in appsettings or yaml.
- Single monolithic pipeline doing build and release with no gates.
- Skipping staging validation before production.
- Sharing one Key Vault for all environments without isolation policy.
