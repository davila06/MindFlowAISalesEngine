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
 
## Fase 2 (Semana 6-9): Calidad enterprise (2026-05-09)

Implementado en frontend:
- Suite de accesibilidad automatizada en CI (`frontend/tests/e2e/accessibility.spec.ts`) usando Playwright + axe-core, cubriendo `/dashboard`, `/pipeline`, `/rules`, `/email/logs`.
- Visual regression testing por rutas clave (`frontend/tests/e2e/visual.spec.ts` + snapshots), con rutas y selectores críticos, tolerancia de diffs y preparación de estado para capturas estables.
- Contract tests FE-BE (`frontend/tests/e2e/contracts.spec.ts`) validando shape de payloads críticos (`/api/pipeline/board`, `/api/rules`, `/api/email/logs`) y headers multi-tenant.
- Dashboard de UX observability: Telemetría UX instrumentada (`frontend/services/uxTelemetry.ts`), endpoint de ingestión (`frontend/pages/api/ux/telemetry.ts`), eventos `view_loaded`, `user_action`, `request_error`, `time_to_insight`, `web_vital` y logging para análisis y alertas.

## Validación E2E Frontend — Mayo 2026

- Todas las suites E2E frontend (unitarias, accesibilidad, visuales, contratos) pasan en verde tras estabilización y actualización de snapshots.
- Evidencia y procedimiento: `docs/product/frontend-e2e-status-2026-05.md`.
- Referencias cruzadas en DoD y progreso (`docs/product/definition-of-done.md`, `ia/05_progress.md`).
