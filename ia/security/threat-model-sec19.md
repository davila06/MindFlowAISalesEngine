# Threat Model (SEC-19)

## Features in scope
- Lead intake (`/api/leads/intake`)
- Rules management and execution
- Proposals automation and reminders
- Observability metrics and manual operational triggers

## High-risk threats and mitigations

1. Spoofed tenant/role context
- Threat: client forges headers to escalate privileges or cross tenant boundaries.
- Mitigations:
  - Tenant query filters in EF Core.
  - Strict mode tenant/role integrity checks (`X-Authenticated-*` vs request headers / claims).
  - Admin-only gate on operational execution endpoints.

2. Replay and duplicate lead creation
- Threat: retries or malicious replay create duplicate commercial records.
- Mitigations:
  - Idempotency key support in intake endpoint with deterministic replay.

3. SMTP secret disclosure
- Threat: plaintext SMTP credentials leaked from DB/logs.
- Mitigations:
  - Encryption at rest in `SmtpSettingsRepository` with Data Protection.
  - Password omitted from API responses.
  - Global sanitized error contract (no stack trace to clients).

4. Operational endpoint abuse
- Threat: unauthorized user triggers snapshots/reminders/force execution.
- Mitigations:
  - Role middleware enforces Admin for sensitive operational routes.
  - Rate limiting by tenant/IP and brute-force protection for SMTP settings.

5. UI injection/XSS via static pages
- Threat: script injection via static UI contexts.
- Mitigations:
  - CSP baseline and security headers middleware.
  - Frame embedding blocked.

6. Data hoarding / privacy exposure
- Threat: sensitive logs retained indefinitely.
- Mitigations:
  - Scheduled retention cleanup for email logs, alert events, audit logs.

## Residual risks
- Strict auth mode is feature-flagged and requires environment rollout governance.
- Full external DAST depth depends on staging endpoint availability in CI.
- Key Vault integration for secret/key material remains pending infrastructure rollout.
