# Operational Changes History

This document tracks significant changes to the infrastructure, policies, and operational workflows for NovaMind.

## Change Log

### 2026-05-17
- **Key Vault Integration**:
  - Migrated all secrets from `appsettings.json` to Azure Key Vault.
  - Updated `local-setup-guide.md` with instructions for Key Vault usage.

- **Observability Enhancements**:
  - Added Application Insights for backend and frontend.
  - Configured Azure Monitor alerts for critical metrics.

### 2026-04-30
- **Infrastructure Upgrade**:
  - Upgraded SQL Server to version 2022.
  - Enabled geo-replication for disaster recovery.

- **CI/CD Pipeline**:
  - Integrated security scans into the build pipeline.
  - Added automated deployment to staging environment.

### 2026-03-15
- **Frontend Optimization**:
  - Implemented lazy loading for images and components.
  - Reduced bundle size by 30%.

- **Backend Refactoring**:
  - Modularized the rules engine for better maintainability.
  - Improved API response times by 20%.

## Guidelines

1. **Recording Changes**:
   - Document all significant changes to infrastructure, policies, or workflows.

2. **Format**:
   - Use the format: `Date`, `Change Description`, and `Impact`.

3. **Review**:
   - Ensure all changes are reviewed and approved by the operations team.