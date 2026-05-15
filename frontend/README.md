# MindFlow Frontend

Next.js frontend aligned to the documented feature-based architecture in ARQ-FRONTEND.

## Structure

- app/layout.tsx
- app/page.tsx
- app/dashboard/page.tsx
- app/pipeline/page.tsx
- app/rules/page.tsx
- app/email/smtp/page.tsx
- app/email/templates/page.tsx
- app/email/logs/page.tsx
- app/admin/page.tsx
- components/
- services/
- hooks/
- types/
- tests/e2e/

## Environment

Copy `.env.example` to `.env.local` and adjust values:

- `NEXT_PUBLIC_API_URL`: backend API base URL.
- `NEXT_PUBLIC_TENANT_ID`: tenant header used by the frontend API client.

## Run locally

```bash
npm install
npm run dev
```

Default frontend URL: `http://localhost:3000`.

## Production validation

Use the enterprise verification gate to validate production build + bundle budget:

```bash
npm run build:verified
```

## E2E smoke tests

Playwright suite validates critical user flows:

1. Dashboard KPI surface
2. Pipeline opportunity creation
3. Rules activate/deactivate
4. SMTP settings save

Run locally (Playwright auto-starts backend and frontend if not already running):

```bash
npx playwright install chromium
npm run test:e2e
```

Optional environment overrides:

- `FRONTEND_BASE_URL` (default `http://127.0.0.1:3100`)
- `API_BASE_URL` (default `http://127.0.0.1:5165`)
- `PLAYWRIGHT_API_COMMAND` (override backend startup command)
- `PLAYWRIGHT_FRONTEND_COMMAND` (override frontend startup command)
