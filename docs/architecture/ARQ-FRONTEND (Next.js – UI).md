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

## Baseline enterprise UX, accesibilidad y performance (2026-05-03)

Implementado en frontend:
- Tokens visuales y estilos globales compartidos en `frontend/app/globals.css`.
- Libreria base reusable en `frontend/components/ui` (`Button`, `Field`, `EmptyState`, `ErrorState`, `Skeleton`, `TableContainer`).
- Navegacion global con estado activo, skip link, selector de idioma y tracking de Web Vitals en `frontend/components/layout/AppShell.tsx`.
- Internacionalizacion base EN/ES en `frontend/i18n/messages.ts` + `frontend/i18n/I18nProvider.tsx`.
- Persistencia de filtros y debounce en `frontend/hooks/usePersistedState.ts` y `frontend/hooks/useDebouncedValue.ts`.
- Telemetria UX no bloqueante (`view_loaded`, `user_action`, `request_error`, `time_to_insight`, `web_vital`) en `frontend/services/uxTelemetry.ts`.
- Requests cancelables y timeout por default en `frontend/services/apiClient.ts` (AbortSignal + timeout).
- Hardening por vista: estados vacio/error/skeleton, labels accesibles, confirmacion+undo y tablas responsive en Dashboard/Pipeline/Rules/SMTP/Email Logs.
- Guia visual de patrones en `frontend/app/admin/ui-guide/page.tsx`.
- Budget de bundle inicial en `frontend/scripts/check-bundle-budget.mjs` + script `npm run check:bundle`.
- E2E operativos extendidos en `frontend/tests/e2e/flows.spec.ts`.
