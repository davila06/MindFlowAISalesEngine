# RBAC Matrix (Published)

Owner: Security + Platform
Review cadence: Monthly or when endpoints change.

## Roles

- `Admin`: full operational and configuration access.
- `Sales`: commercial flow operations with constrained admin actions.
- `Viewer`: read-only visibility where allowed.

## API Access Matrix

| Domain | Endpoint Pattern | Admin | Sales | Viewer |
|---|---|---:|---:|---:|
| Leads | `/api/leads/*` | Yes | Yes | Read-only |
| Contacts/Companies | `/api/contacts/*`, `/api/companies/*` | Yes | Yes | Read-only |
| Pipeline | `/api/pipeline/*` | Yes | Yes | Read-only board/history |
| Assignments | `/api/assignments/*` | Yes | Limited | No |
| Email Config | `/api/email/smtp-settings` | Yes | No | No |
| Email Operations | `/api/email/*` | Yes | Limited | Read-only logs |
| Rules | `/api/rules/*` | Yes | No | Read-only |
| Scoring | `/api/scoring/*` | Yes | Limited | Read-only |
| Proposals/Onboarding | `/api/proposals/*`, `/api/onboarding/*` | Yes | Yes | Read-only |
| Ops | `/api/ops/*` | Yes | No | No |
| Analytics Advanced | `/api/analytics/advanced/*` | Yes | Read-only selected endpoints | Read-only selected endpoints |

## Enforcement Notes

1. Authoritative role source is JWT claims in strict mode.
2. Tenant header spoofing is rejected when claims mismatch.
3. Viewer role cannot execute write operations.

## Change Control

Any new endpoint requires:
1. RBAC matrix row update.
2. Authorization test coverage.
3. Mention in release changelog.
