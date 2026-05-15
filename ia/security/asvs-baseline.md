# ASVS Baseline (SEC-17)

## Scope
- API backend (.NET 9): authentication, authorization, tenancy, observability, email automation.
- Static operational UIs served from backend (`wwwroot`).

## Target
- OWASP ASVS Level 2 baseline for internet-facing SaaS API.

## Control Mapping Snapshot
- V1 Architecture: partial-compliant (threat model + ADRs documented, periodic review pending automation).
- V2 Authentication: partial-compliant (JWT plumbing + strict mode; rollout plan required for all environments).
- V3 Session Management: n/a for API-token model.
- V4 Access Control: compliant for tenant isolation and role enforcement baseline.
- V5 Validation/Sanitization: compliant baseline with centralized model validation and typed contracts.
- V6 Stored Cryptography: compliant baseline for SMTP secrets at rest via Data Protection.
- V7 Error Handling/Logging: compliant baseline with global exception contract and trace IDs.
- V8 Data Protection: partial-compliant (retention service implemented; legal policy sign-off pending).
- V9 Communications: compliant baseline with HSTS/security headers and strict-mode API key intake option.
- V10 Malicious Code: partial-compliant (pipeline gate created, DAST depth to expand).
- V11 Business Logic: partial-compliant (idempotency + admin controls; anti-automation enhancements pending).
- V12 Files/Resources: compliant baseline for attachment validation in SMTP sender.
- V13 API/Web Service: compliant baseline (versioning, rate limiting, health checks, security headers).
- V14 Config: partial-compliant (strict mode feature-flagged, rollout per environment pending).

## Gaps and Plan
1. Enforce strict auth mode in non-dev environments with tenant-specific JWT issuer/audience keys.
2. Add automated DAST against staging endpoint in CI and block critical findings.
3. Add key rotation runbook + Key Vault migration for Data Protection keys.
4. Add quarterly ASVS control evidence review checklist.
