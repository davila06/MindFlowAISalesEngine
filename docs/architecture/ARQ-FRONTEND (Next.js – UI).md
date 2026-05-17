FRONTEND (Next.js – UI)
Arquitectura: feature‑based, no “pages‑only”.

Estado actual (2026-05-02)
- Estructura `frontend/` implementada con App Router y rutas del documento.
- UI principal alineada en: `/dashboard`, `/pipeline`, `/rules`, `/email/smtp`, `/email/templates`, `/email/logs`, `/admin`.
- UI HTML legacy en `backend/src/Api/wwwroot` retirada del runtime; backend opera API-only para frontend desacoplado.

/frontend
│
├── app
│   ├── layout.tsx
│   ├── page.tsx
│   │
│   ├── dashboard
│   │   └── page.tsx
│   │
│   ├── pipeline
│   │   └── page.tsx
│   │
│   ├── rules
│   │   └── page.tsx
│   │
│   ├── email
│   │   ├── smtp
│   │   │   └── page.tsx
│   │   ├── templates
│   │   │   └── page.tsx
│   │   └── logs
│   │       └── page.tsx
│   │
│   └── admin
│       └── page.tsx
│
├── components
│   ├── ui
│   ├── layout
│   ├── pipeline
│   ├── rules
│   └── email
│
├── services
│   ├── apiClient.ts
│   ├── leads.service.ts
│   ├── pipeline.service.ts
│   ├── rules.service.ts
│   └── email.service.ts
│
├── hooks
│   ├── useTenant.ts
│   ├── useAuth.ts
│   └── usePermissions.ts
│
├── types
│   ├── lead.ts
│   ├── rule.ts
│   ├── email.ts
│   └── user.ts
│
├── styles
├── public
└── README.md

💡 Clave aquí

Email UI tiene su propio espacio
UI refleja exactamente el scope del documento
Fácil deshabilitar features por tenant/plan

## Baseline enterprise UX, accesibilidad y performance (2026-05-03 a 2026-05-09)

Implementado en frontend:
- Tokens visuales y estilos globales compartidos en `frontend/app/globals.css`.
- Librería base reusable en `frontend/components/ui` (`Button`, `Field`, `EmptyState`, `ErrorState`, `Skeleton`, `TableContainer`).
- Navegación global con estado activo, skip link, selector de idioma y tracking de Web Vitals en `frontend/components/layout/AppShell.tsx`.
- Internacionalización base EN/ES en `frontend/i18n/messages.ts` + `frontend/i18n/I18nProvider.tsx`.
- Persistencia de filtros y debounce en `frontend/hooks/usePersistedState.ts` y `frontend/hooks/useDebouncedValue.ts`.
- Telemetría UX no bloqueante (`view_loaded`, `user_action`, `request_error`, `time_to_insight`, `web_vital`) en `frontend/services/uxTelemetry.ts`.
- Requests cancelables y timeout por default en `frontend/services/apiClient.ts` (AbortSignal + timeout).
- Hardening por vista: estados vacío/error/skeleton, labels accesibles, confirmación+undo y tablas responsive en Dashboard/Pipeline/Rules/SMTP/Email Logs.
- Guía visual de patrones en `frontend/app/admin/ui-guide/page.tsx`.
- Budget de bundle inicial en `frontend/scripts/check-bundle-budget.mjs` + script `npm run check:bundle`.
- E2E operativos extendidos en `frontend/tests/e2e/flows.spec.ts`.
- Suite de accesibilidad automatizada en CI (`frontend/tests/e2e/accessibility.spec.ts`) usando Playwright + axe-core, cubriendo `/dashboard`, `/pipeline`, `/rules`, `/email/logs`.
- Visual regression testing por rutas clave (`frontend/tests/e2e/visual.spec.ts` + snapshots), con rutas y selectores críticos, tolerancia de diffs y preparación de estado para capturas estables.
- Contract tests FE-BE (`frontend/tests/e2e/contracts.spec.ts`) validando shape de payloads críticos (`/api/pipeline/board`, `/api/rules`, `/api/email/logs`) y headers multi-tenant.
- Dashboard de UX observability: Telemetría UX instrumentada (`frontend/services/uxTelemetry.ts`), endpoint de ingestión (`frontend/pages/api/ux/telemetry.ts`), eventos `view_loaded`, `user_action`, `request_error`, `time_to_insight`, `web_vital` y logging para análisis y alertas.

## Validación E2E Frontend — Mayo 2026

- Todas las suites E2E frontend (unitarias, accesibilidad, visuales, contratos) pasan en verde tras estabilización y actualización de snapshots.
- Evidencia y procedimiento: `docs/product/frontend-e2e-status-2026-05.md`.
- Referencias cruzadas en DoD y progreso (`docs/product/definition-of-done.md`, `ia/05_progress.md`).
