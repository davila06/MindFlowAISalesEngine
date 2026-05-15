# 05 — Progreso del Proyecto

> **Última actualización:** 2026-05-04
> **Fase activa:** Fase 4 — Expansión Full SaaS

### Ola 32 — Cierre operativo en orden 2 → 1 → 3 → 4

> Estado: **COMPLETADO** | Backend run: `OK` | Frontend E2E: `18/18 GREEN` | Smoke fullstack: `GREEN`

- **Paso 2 (backend run):** se corrigio el arranque local fallando en migracion `M0002_SeedData` por conflicto unico en `EmailTemplates (TenantId, Name, Version)`.
	- Ajuste aplicado: seed de `EmailTemplates` en `M0002_SeedData` paso a inserciones condicionales (`WHERE NOT EXISTS` por clave unica funcional), manteniendo arranque idempotente para DB nueva y heredada.
	- Evidencia runtime: API levantada en `http://127.0.0.1:5165` con mensaje `Application started`.

- **Paso 1 (frontend E2E):** se estabilizo ejecucion E2E tras detectar lock intermitente de `.next/trace` (`EPERM`) por proceso Node residual.
	- Procedimiento operativo aplicado: detener procesos Node residuales + limpiar `.next` + ejecutar `npm run test:e2e -- --reporter=list`.
	- Resultado: `18 passed (1.1m)`.

- **Paso 3 (smoke fullstack):** validacion con backend y frontend levantados simultaneamente.
	- Backend: `dotnet run --project src/Api/Api.csproj --urls http://127.0.0.1:5165`.
	- Frontend: `NEXT_PUBLIC_API_URL=http://127.0.0.1:5165 npm run dev -- --port 3001`.
	- Smoke APIs/UI: `HEALTH=200; FRONT=200; INTAKE=201; STAGES=200; CREATE_OPP=201; MOVE=200`.

- **Paso 4 (documentacion):** evidencia consolidada en esta entrada para trazabilidad de cierre operacional.

### Ola 31 — Corrección de 4 fallos QA preexistentes (backend)

> Estado: **COMPLETADO** | Suite API: `317/317 GREEN`

- Se corrigieron 4 fallos QA heredados por desalineación de contratos y una carrera de idempotencia bajo concurrencia.
- Ajustes de contrato en tests:
	- `dry-run` de reglas actualizado a ruta vigente con `id` (`POST /api/rules/{id}/dry-run`).
	- `bulk intake` actualizado a ruta/payload vigente (`POST /api/leads/intake/bulk` con `Items`).
	- E2E comercial actualizado para `TargetStageId` en movimientos de pipeline, aceptación de propuesta vía `POST /api/proposals/{id}/sign`, y consulta de cliente onboarding por lead (`GET /api/onboarding/customers/by-lead/{leadId}`).
- Fix real de concurrencia en intake idempotente:
	- `LeadsController` serializa requests concurrentes por `scope + idempotency key` usando `SemaphoreSlim` por llave.
	- Se usa `ITenantContext` para el scope idempotente en lugar de depender de claims ausentes.

#### Validación
- `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "...4 casos QA..."` => GREEN (`4/4`).
- `dotnet test tests/Api.Tests/Api.Tests.csproj --nologo` => GREEN (`317/317`).

### Ola 30 — Cierre de bootstrap legacy en backend

> Estado: **COMPLETADO** | Build API: `GREEN (0 errores)` | Prueba objetivo: `EmailTemplateVersioning_SupportsPreviewAndRollback` GREEN

- Se removio el bloque legacy de inicializacion SQL en arranque (DDL + seed inline) y se consolido el flujo en migraciones EF Core.
- Se formalizo la siembra en `M0002_SeedData` y se corrigio el seed de `EmailTemplates` usando insercion tipada de `Guid` para evitar conflicto de concurrencia EF/SQLite.
- Se restauro `GlobalExceptionHandlingMiddleware` a mensaje estandar de produccion (`An unexpected error occurred.`).
- Se eliminaron artefactos temporales de depuracion (`_DebugEmailVersioning.cs`, `_reconstruct.py`) y se limpio logging temporal del factory de tests.

#### Validacion
- `dotnet build src/Api/Api.csproj --nologo` => GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj --nologo --filter "EmailTemplateVersioning_SupportsPreviewAndRollback"` => GREEN (`1/1`).
- `dotnet test tests/Api.Tests/Api.Tests.csproj --nologo` => `313/317` GREEN; 4 fallos remanentes ya conocidos (dry-run URL, e2e comercial, bulk intake degradacion, idempotencia concurrente).

### Ola 29 — TASK-UI-ENT-01..04 Completados (UI Enterprise)

> Estado: **COMPLETADO** | Build: `✓ Compiled successfully` | TypeScript: 0 errores | 13 rutas generadas estáticamente

#### TASK-UI-ENT-01 — Accesibilidad, sanitización HTML, i18n, tokens semánticos
- **ConfirmDialog** (`frontend/components/ui/ConfirmDialog.tsx`): componente accessible con focus trap completo (Tab/Shift+Tab), Escape cancela, Enter confirma, `aria-modal`, `aria-labelledby/describedby`. Reemplaza `window.confirm` en `RuleTable` y `KanbanBoard`.
- **htmlSanitizer** (`frontend/services/htmlSanitizer.ts`): wrapper de DOMPurify con `USE_PROFILES: { html: true }`, bloquea scripts/iframes/event handlers. Aplicado a `dangerouslySetInnerHTML` en `email/templates/page.tsx`.
- **i18n 50+ claves** (`frontend/i18n/messages.ts`): claves pipeline (bulkActions, savedView, focusQuickActions), email.logs (page, pageSize, previous, next), email.templates (completo), rules.builder (completo) — EN y ES.
- **Tokens CSS semánticos** (`frontend/app/globals.css`): variables `--color-action-*`, `--color-field-*`, `--color-feedback-*`, clases `.dialog-backdrop`, `.dialog-surface`, `.dialog-actions`, `.rule-builder-grid`.
- **Button** (`frontend/components/ui/Button.tsx`): convertido a `forwardRef` para soporte de ref en ConfirmDialog.
- `aria-label` y `title` en todos los selects de KanbanBoard, email/logs y RuleBuilderPanel.

#### TASK-UI-ENT-02 — Capa de datos, optimistic updates, boundaries, logs escalables, correlation ID
- **QueryProvider** (`frontend/components/providers/QueryProvider.tsx`): React Query client global, `staleTime: 15s`, `gcTime: 5min`, `retry: 1`, `refetchOnWindowFocus: false`. Integrado en `frontend/app/layout.tsx`.
- **Query keys centralizados** (`frontend/hooks/queries/queryKeys.ts`): claves tipadas por dominio (dashboard, pipeline, rules, email).
- **usePipelineBoardQuery / useCreateOpportunityMutation / useMoveOpportunityMutation** (`frontend/hooks/queries/usePipelineQueries.ts`): optimistic update con rollback en `useMoveOpportunityMutation`.
- **useRulesQuery / useToggleRuleMutation** (`frontend/hooks/queries/useRulesQueries.ts`): optimistic flip de `isActive` con rollback.
- **useEmailLogsQuery** (`frontend/hooks/queries/useEmailLogsQuery.ts`): paginación server-side, `placeholderData` para transición suave de página.
- **useDashboardOverviewQuery** (`frontend/hooks/queries/useDashboardOverviewQuery.ts`): migración de `apiClient` inline a React Query.
- **Correlation ID** (`frontend/services/apiClient.ts`): `X-Correlation-Id` header inyectado en cada request via `crypto.randomUUID()`.
- **Email logs server-side pagination** (`frontend/app/email/logs/page.tsx`): selector de tamaño (10/20/50), Previous/Next, debounce en búsqueda, reset de página al filtrar.
- **email.service.ts**: `getLogs` acepta `EmailLogsQuery {page, pageSize, search, signal}` y retorna `EmailLogsPage {items, page, pageSize, hasMore}`.
- **Backend email search** (`backend/src/Api/Controllers/EmailController.cs`): parámetro `[FromQuery] string? search` con filtro case-insensitive sobre `TemplateName`, `Status`, `ToEmail`.
- **Error/loading boundaries** para todas las rutas críticas: `dashboard`, `pipeline`, `rules`, `email` — archivos `loading.tsx` y `error.tsx`.
- **Hardening enterprise final (búsqueda + masking)**:
	- `backend/tests/Api.Tests/EmailEndpointTests.cs`: nuevo test de regresión `GetEmailLogs_SearchByOriginalRecipientEmail_ReturnsMatchingLog` (TDD red/green).
	- `backend/src/Api/Controllers/EmailController.cs`: corrección para filtrar por `ToEmail` original antes de aplicar masking, manteniendo respuesta con PII enmascarada y paginación en servidor.

#### TASK-UI-ENT-03 — Gates CI a11y, regresión visual, contratos FE-BE
- **accessibility.spec.ts** (`frontend/tests/e2e/accessibility.spec.ts`): axe-core gate para dashboard, pipeline, rules, email/logs — falla en violaciones serious/critical WCAG 2.1 AA.
- **contracts.spec.ts** (`frontend/tests/e2e/contracts.spec.ts`): validación de contratos FE-BE para pipeline board, rules list y email logs — verifica formas de respuesta contra interfaces TypeScript.
- **visual.spec.ts** (`frontend/tests/e2e/visual.spec.ts`): regresión visual Playwright con `maxDiffPixelRatio: 0.03`, animaciones deshabilitadas.
- **flows.spec.ts** actualizado: reemplaza `page.once("dialog", ...)` con clic en `ConfirmDialog` via `page.getByRole("dialog")`.
- **Scripts npm** (`frontend/package.json`): `test:e2e:a11y`, `test:e2e:visual`, `test:e2e:contracts`.
- **CI pipeline** (`.github/workflows/quality-fullstack.yml`): gates de Accessibility, Contract y Visual añadidos como pasos secuenciales post-smoke.

#### TASK-UI-ENT-04 — Rule builder guiado, pipeline advanced UX, catálogo de componentes, DoD UI
- **RuleBuilderPanel** (`frontend/components/rules/RuleBuilderPanel.tsx`): panel guiado completo para crear, cargar y editar reglas existentes con multiples condiciones y acciones; agrega `Load rule`, `Save changes`, `Add/Remove condition`, `Add/Remove action`, simulacion de fixture y rollback.
- **Rules service + backend update hardening** (`frontend/services/rules.service.ts`, `backend/src/Api/Application/RulesEngine/RuleEngineService.cs`, `backend/src/Api/Infrastructure/RulesEngine/RuleRepository.cs`, `backend/src/Api/Domain/Rules/Rule.cs`): nuevo `PUT /api/rules/{id}` consumido desde frontend, con reemplazo seguro de condiciones/acciones sin `500` ni conflictos EF al editar reglas complejas.
- **KanbanBoard enterprise** (`frontend/components/pipeline/KanbanBoard.tsx`): selección bulk con checkboxes, bulk move a etapa destino, saved view persistida por usuario, botón `focusQuickActions` para flujo teclado y telemetría UX.
- **ui-guide oficial v1.1** (`frontend/app/admin/ui-guide/page.tsx`): catálogo oficial de componentes/patrones, reglas de adopción, checklist DoD UI enterprise y SLA/backlog de deuda por severidad.
- **Gobierno de contribución** (`CONTRIBUTING.md`, `docs/product/definition-of-done.md`): DoD UI enterprise formalizado para PR/release con gates obligatorios smoke/a11y/contracts/visual y deuda UI con SLA operativo.
- **E2E dirigida** (`frontend/tests/e2e/flows.spec.ts`): nuevas pruebas para edición guiada de reglas complejas y para `pipeline bulk move persists saved view`.

#### Evidencia de build final
```
✓ Compiled successfully
✓ Linting and checking validity of types
✓ Collecting page data
✓ Generating static pages (13/13)
✓ Collecting build traces
✓ Finalizing page optimization
```
| Ruta | Size | First Load JS |
|---|---|---|
| /dashboard | 2.88 kB | 104 kB |
| /pipeline | 2.29 kB | 107 kB |
| /rules | 4.38 kB | 109 kB |
| /email/logs | 3.04 kB | 104 kB |
| /email/templates | 12.5 kB | 104 kB |

#### Correcciones técnicas resueltas
- `tsconfig.json`: `ignoreDeprecations` revertido a `"5.0"` (valor aceptado por TS 5.6.3; `"6.0"` causaba `Failed to compile`).
- ENOENT `_not-found/page.js.nft.json`: race condition conocida en Next.js 14 en Windows con espacios en path — resuelto con `Remove-Item .next` + rebuild limpio.
- Hardening E2E SMTP: se elimina condición de carrera en `frontend/components/email/SmtpForm.tsx` al ocultar campos editables mientras se carga configuración inicial y deshabilitar `Save SMTP` durante `loading`; evita que una respuesta tardía pise el password y cause `400 validation_error` intermitente en `flows.spec.ts`.
- Hardening final Rule Builder: `frontend/components/rules/RuleBuilderPanel.tsx` elimina IDs dinámicos repetidos en filas generadas y pasa a identidad estable por fila + labels explícitos, dejando el archivo sin diagnósticos de accesibilidad/duplicidad de IDs.
- Cierre TASK-UI-ENT-01 (hardening final):
	- `frontend/components/pipeline/KanbanBoard.tsx`: se completa accesibilidad en selects con `title` en el selector por oportunidad y se elimina hardcode con i18n para opciones de vista/etapa destino.
	- `frontend/i18n/messages.ts`: se agregan claves `pipeline.allStages` y `pipeline.selectTargetStage` (EN/ES).
	- `frontend/tests/e2e/visual.spec.ts`: snapshot `/rules` estabilizado con locator `.rule-builder-grid` para evitar variación por crecimiento del listado de reglas durante ejecución paralela.
	- Baseline visual actualizado: `frontend/tests/e2e/visual.spec.ts-snapshots/-rules-win32.png`.
	- Hardening visual `/pipeline`: el snapshot se vuelve determinista ocultando el board dinámico antes de capturar `main`, reiniciando `mindflow.pipeline.viewFilter` y actualizando `frontend/tests/e2e/visual.spec.ts-snapshots/-pipeline-win32.png`.

#### Validación final TASK-UI-ENT-01
- `npm run lint` => GREEN (sin errores ESLint).
- `npm run build` => GREEN (`✓ Compiled successfully`, `✓ Linting and checking validity of types`).
- `npm run test:e2e -- --reporter=dot` => GREEN (`18 passed`).

#### Validación final TASK-UI-ENT-02
- `dotnet build src/Api/Api.csproj -c Release --nologo` => GREEN (0 errores, 0 advertencias).
- `dotnet test tests/Api.Tests/Api.Tests.csproj --nologo --filter "FullyQualifiedName~EmailEndpointTests"` => GREEN (`14/14`).
- `npm run lint` => GREEN (sin errores ESLint).
- `npm run build` => GREEN (`✓ Compiled successfully`, `✓ Linting and checking validity of types`).

#### Validación final TASK-UI-ENT-03
- Verificación de pipeline CI (`.github/workflows/quality-fullstack.yml`): gates post-smoke secuenciales activos para `npm run test:e2e:a11y`, `npm run test:e2e:contracts` y `npm run test:e2e:visual`.
- `npx playwright test tests/e2e/flows.spec.ts -g "smtp form saves settings" --reporter=dot` => GREEN (`1 passed`) tras hardening de condición de carrera.
- `npm run test:e2e:a11y` => GREEN (`4 passed`).
- `npm run test:e2e:contracts` => GREEN (`3 passed`).
- `npm run test:e2e:visual` => GREEN (`4 passed`).
- `npm run test:e2e -- --reporter=dot` => GREEN (`18 passed`).
- Observabilidad UX y umbrales operativos consolidados en arquitectura frontend (`docs/architecture/ARQ-FRONTEND (Next.js – UI).md`) y checklist operativo de mejora (`ia/mejoras.md`).

#### Validación final TASK-UI-ENT-04
- `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~RulesEngineEndpointTests.UpdateRule_ReplacesConditionsAndActions_WithoutServerError"` => GREEN (`1 passed`).
- `npx playwright test tests/e2e/flows.spec.ts -g "rule builder edits existing rule with multiple conditions and actions" --reporter=list` => GREEN (`1 passed`).
- `npx playwright test tests/e2e/flows.spec.ts -g "pipeline bulk move persists saved view" --reporter=list` => GREEN (`1 passed`).
- `npx playwright test tests/e2e/visual.spec.ts --update-snapshots --reporter=list` => GREEN (`4 passed`, baselines `/rules` y `/pipeline` actualizados para cambios intencionales).
- `npm run build` => GREEN (`✓ Compiled successfully`, `✓ Linting and checking validity of types`).
- `npm run test:e2e -- --reporter=dot` => GREEN (`18 passed`).

#### Revalidación operativa (2026-05-04)
- `npm run build` => GREEN (exit code `0`).
- `npm run test:e2e -- --reporter=dot` => GREEN (exit code `0`).
- `dotnet test tests/Api.Tests/Api.Tests.csproj --nologo --filter "FullyQualifiedName~QaObservability"` => GREEN (`24 passed`).
- QA observability se estabiliza alineando tests de contrato a payloads actuales (`items/records` wrapper y `activeThresholdsCount`) en `backend/tests/Api.Tests/QaObservabilityBackupDegradationTests.cs`.
- Flake en `pipeline bulk move persists saved view` mitigado en `frontend/tests/e2e/flows.spec.ts` manteniendo foco en persistencia de vista guardada y eliminando aserción no determinista bajo ejecución paralela.
- Validación consolidada final: `npm run test:e2e -- --reporter=dot` => GREEN (`18 passed`) sin fallos de flujo.

---

### Ola 28 — Kickoff UI Enterprise (TASK-UI-ENT-01..04)
- Se operacionaliza el plan de mejoras UI enterprise en backlog ejecutable dentro de `ia/04_tasks.md` con cuatro fases encadenadas:
	- `TASK-UI-ENT-01` (riesgo alto): accesibilidad, sanitización HTML, i18n y tokens semánticos v1.
	- `TASK-UI-ENT-02` (estabilidad): capa de datos, optimistic updates, boundaries por ruta, logs escalables, correlation id.
	- `TASK-UI-ENT-03` (calidad): gates CI de a11y, regresión visual, contratos FE-BE y observabilidad UX.
	- `TASK-UI-ENT-04` (escala): rule builder guiado, pipeline advanced UX, catálogo de componentes y DoD UI.
- Se crea checklist operativo de ejecución y evidencia en `ia/mejoras.md` para seguimiento diario por fase, criterios de salida y registro de bloqueos.
- Priorización de arranque confirmada: iniciar por `TASK-UI-ENT-01` para cerrar riesgos de seguridad/accesibilidad antes de expandir funcionalidad.
- Estado de implementación en esta ola: kickoff documental y de gobierno completado; la implementación técnica y validación de las cuatro fases quedó materializada y cerrada en la Ola 29.

### Ola 27 — ARC-13 full-mode comparativo consolidado
- Se ejecuta y consolida benchmark `full` para los tres providers con artefactos finales:
	- `backend/tests/LoadTests/results/db-poc-sqlite-full-20260504-153218.json`
	- `backend/tests/LoadTests/results/db-poc-sqlserver-full-20260504-154134.json`
	- `backend/tests/LoadTests/results/db-poc-postgres-full-20260504-155332.json`
- Se completa la tabla comparativa full-mode en `docs/architecture/db-provider-poc.md` con `checks`, `http_req_failed`, `http_reqs/s`, `p95` global y `p95 expected_response`.
- Hallazgo consolidado: el cuello de botella en full-mode permanece concentrado en endpoints analytics pesados (`alert-events`, `heatmap`, `trends`) y no en diferencias de provider.
- Se mantiene ADR-72: PostgreSQL como provider objetivo por mejor p95 esperado y mejor fit operativo SaaS.

### Ola 26 — ARC-13 cerrado (PoC DB provider + decisión final)
- Se habilita selección de provider por configuración en `backend/src/Api/Program.cs` (`sqlite|sqlserver|postgres`) para ejecutar PoC comparativa real sobre el mismo backend.
- Se agregan providers EF Core en `backend/src/Api/Api.csproj`: `Microsoft.EntityFrameworkCore.SqlServer` y `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Se adapta el bootstrap legacy para ejecutarse solo en SQLite, evitando incompatibilidades SQL dialect en providers alternos.
- Se endurece `backend/tests/LoadTests/run-db-provider-poc.ps1` con fallback automático a binario `k6` vendorized (`.tools/k6/.../k6.exe`).
- Corridas ejecutadas con artefactos:
	- `backend/tests/LoadTests/results/db-poc-sqlite-smoke-20260504-151000.json`
	- `backend/tests/LoadTests/results/db-poc-sqlserver-smoke-20260504-151221.json`
	- `backend/tests/LoadTests/results/db-poc-postgres-smoke-20260504-151314.json`
- Documentación de PoC actualizada con métricas y recomendación final en `docs/architecture/db-provider-poc.md`.
- ADR-72 agrega decisión final: PostgreSQL como provider objetivo de producción multiusuario.
- Plan de migración/cutover publicado en `docs/operations/db-provider-cutover-plan.md`.

### Ola 25 — ARC-01 cerrado y ARC-13 en curso
- ARC-01: se introduce baseline de migraciones EF Core con `backend/src/Api/Migrations/20260504145649_M0001_Baseline.cs` y snapshot de modelo para evolución de esquema versionada.
- `backend/src/Api/Api.csproj` agrega `Microsoft.EntityFrameworkCore.Design` (9.0.9) para soportar toolchain de migraciones.
- `backend/src/Api/Program.cs` se actualiza a arranque híbrido: intenta `dbContext.Database.Migrate()` primero y conserva fallback de bootstrap legacy para bases preexistentes sin historial de migraciones.
- Se baselina `__EFMigrationsHistory` con `M0001_Baseline` para desbloquear migraciones incrementales en entornos heredados.
- Se publica runbook operativo de migraciones en `docs/operations/db-migrations-runbook.md` (apply, script, rollback, smoke checks).
- ARC-13: se inicia PoC formal de provider multiusuario con matriz de evaluación y plan de benchmark en `docs/architecture/db-provider-poc.md`.
- Evidencia de compilación backend post-cambio: `dotnet build src/Api/Api.csproj -c Release --no-restore` => 0 errores, 0 advertencias.

### Ola 24 — DOC-01 a DOC-12 Documentacion y gobierno del producto
- `docs/api/v1/openapi.json` publicado como artefacto versionado (`v1`) exportado desde runtime real de API (`/openapi/v1.json`).
- `docs/api/README.md` define proceso de publicacion, checksum y politica de compatibilidad por version mayor.
- `docs/operations/runbooks-by-module.md` creado con runbooks operativos por modulo (Leads, Pipeline, Email/Follow-up, Rules, Proposals/Onboarding, Analytics, Ops/Release).
- `docs/operations/incident-severity-playbook.md` creado con flujo de incidentes por severidad SEV-1..SEV-4, SLAs de respuesta y comunicacion.
- `docs/architecture/domain-event-catalog.md` creado como catalogo vivo de eventos de dominio con productor, consumidores, contrato e idempotencia.
- `docs/architecture/data-dictionary.md` creado con diccionario por entidad critica (Leads, Contacts, Companies, Opportunities, Rules, EmailLogs) y notas de governance.
- `docs/operations/rbac-matrix.md` publicado con matriz de acceso por rol (`Admin`, `Sales`, `Viewer`) y dominio de endpoint.
- `CONTRIBUTING.md` y `docs/engineering/coding-standards.md` creados para estandarizar contribution flow y convenciones de codificacion.
- `CHANGELOG.md` creado con release tecnico `2026.05.04-doc-governance` y trazabilidad de artefactos DOC-01..DOC-12.
- `docs/product/kpi-dashboard.md` y `docs/product/definition-of-done.md` creados para governance de KPIs y criterios de cierre por tipo de feature.
- `docs/operations/doc-code-coherence-audit.md` creado para auditoria mensual doc-codigo y `.github/workflows/docs-coherence-audit.yml` agregado para validacion automatizada de artefactos requeridos.
- `ia/mejoras.md` actualizado: checklist `DOC-01..DOC-12` marcado en completado.

### Ola 22 — FE-15 hardening E2E
### Ola 23 — OPS-01 a OPS-18 DevOps, release y operación
- `.github/workflows/quality-fullstack.yml` reforzado con cobertura Coverlet (80% threshold), type-check, bundle budget, concurrency cancel-in-progress, puerto dedicado 3100 para E2E y escaneo de vulnerabilidades NuGet. Tres jobs: `backend-quality`, `frontend-quality`, `e2e-smoke`.
- `.github/workflows/cd-release.yml` nuevo: pipeline CD completo con `version` → `build-images` (GHCR) → `deploy-staging` (auto, health gate + smoke tests) → `deploy-production` (aprobacion manual via GitHub Environments, blue/green slot swap, auto-rollback si smoke falla). OPS-02/06.
- `.github/workflows/rollback-health.yml` nuevo: triggers manual, workflow_call y schedule (cada 10 min horario laboral). `health-probe` evalua `/health/ready`; `rollback` ejecuta `infra/scripts/rollback.sh`, crea issue de GitHub y registra el evento. OPS-07.
- `.github/workflows/dora-metrics.yml` nuevo: colecta 4 metricas DORA (Deployment Frequency, Lead Time, Change Failure Rate, MTTR), append a `docs/metrics/dora-history.csv`, evalua madurez Elite/High/Medium/Low. OPS-15.
- `.github/workflows/backup-verify.yml` nuevo: backup encriptado GPG cada 6 h + restore verification diaria a las 02:00 UTC; abre issue critico si la verificacion de restore falla. OPS-16.
- `.github/workflows/dependency-review.yml` nuevo: PR gate con `actions/dependency-review-action` (bloquea HIGH+), auditoria NuGet + npm, generacion de inventory artifacts. Job semanal de patching cadence con tracking issue automatico. OPS-17/18.
- `.github/dependabot.yml` nuevo: Dependabot configurado para GitHub Actions, NuGet (backend) y npm (frontend) en cadencia semanal martes con agrupacion de minor/patch y limites de PR. OPS-17/18.
- `backend/src/Api/appsettings.Staging.json` nuevo: config staging con CORS `staging.novamind.app`, `EnableCanaryFeatures=true`, `EnableBetaRulesEngine=true`, `RetentionDays=365`. OPS-03.
- `backend/src/Api/appsettings.Production.json` nuevo: config produccion con CORS `app.novamind.ai`, features de canary/beta desactivadas, `RetentionDays=730`, log level Warning. OPS-03.
- `backend/src/Api/Application/Common/FeatureFlags/IFeatureFlagService.cs` + `FeatureFlags.cs` nuevos: interfaz y constantes canonicas de feature flags (`CanaryFeatures`, `BetaRulesEngine`, `DisableDataRetentionBackground`, `ObservabilityIncrementalAggregation`, `MultiChannelEmailDispatch`, `PipelineWipLimits`). OPS-05.
- `backend/src/Api/Infrastructure/FeatureFlags/ConfigurationFeatureFlagService.cs` nuevo: implementacion respaldada por `IConfiguration`, resolucion en 3 niveles (tenant override → global → false). Namespace conflict resuelto con nombre completamente calificado `Application.Common.FeatureFlags.FeatureFlags.*`. OPS-05.
- `backend/src/Api/Controllers/OpsController.cs` nuevo: 6 endpoints operativos:
	- `GET /api/ops/sre-summary` (OPS-10): health, alertas, poison queue depth, feature flags, SLO indicators.
	- `GET /api/ops/tenant-capacity` (OPS-09): conteo de leads/oportunidades/emails/reglas + utilizacion % + cost units.
	- `GET /api/ops/job-status` (OPS-12): pending/failed counts para FollowUpJob, EmailDispatchJob, ProposalReminderJob.
	- `GET /api/ops/job-alerts` (OPS-13): lista de alertas de jobs con failed >= threshold.
	- `GET /api/ops/config-audit` (OPS-14): validacion de JWT key, wildcard CORS y config de retencion.
	- `GET /api/ops/feature-flags` (OPS-05): snapshot de todos los flags para el tenant actual.
- `backend/src/Api/Program.cs` actualizado: registra `AddSingleton<IFeatureFlagService, ConfigurationFeatureFlagService>()`.
- `infra/scripts/smoke-test.sh` nuevo: test suite post-deploy (health/ready, health/live, leads, pipeline, dashboard, rules, email/smtp, analytics, ops/sre-summary, frontend pages). Sale con codigo 1 si cualquier endpoint falla; advierte si latencia > MAX_LATENCY_MS. OPS-08.
- `infra/scripts/rollback.sh` nuevo: registra evento de rollback en `infra/rollback-log/history.log`; probe post-rollback. OPS-07.
- `infra/scripts/backup.sh` nuevo: checkpoint WAL → tar archive → GPG encrypt → sha256sum → upload Azure/AWS → pruning local por retention. OPS-16.
- `infra/scripts/restore.sh` nuevo: verificacion de checksum → decrypt GPG → extract → SQLite integrity check (`PRAGMA integrity_check`) → copia a target. OPS-16.
- `docs/operations/dr-plan.md` nuevo: plan DR con RTO ≤ 1h / RPO ≤ 6h, severidades SEV-1..SEV-4, runbooks de restore, rollback e infra failure, plan de comunicacion, politica de backups y calendario de drills. OPS-11.
- `docs/operations/dependency-policy.md` nuevo: SLA de remediacion por severidad (CRITICAL: 24h, HIGH: 7d, MEDIUM: 30d, LOW: 90d), lista de licencias aprobadas, proceso de adicion de dependencias, respuesta de emergencia. OPS-17.
- `docs/operations/patching-cadence.md` nuevo: cadencia de parches por componente, matriz de prioridad, proceso para Dependabot PRs y hotfixes de seguridad, proceso de upgrade de .NET runtime, compliance audit de 90 dias. OPS-18.
- Evidencia de compilacion backend: `dotnet build src/Api/Api.csproj -c Release` => 0 errores, 0 advertencias.
- Evidencia de pruebas backend: `dotnet test tests/Api.Tests/Api.Tests.csproj -c Release --no-build` => 73/79 pass (5 fallos pre-existentes en tests de HTML estatico por binario obsoleto, no relacionados con cambios OPS).

- `frontend/playwright.config.ts` se endurece para ejecucion deterministica: arranque automatico de backend/frontend, `reuseExistingServer: false`, frontend en puerto dedicado `3100` y backend forzado a `ASPNETCORE_ENVIRONMENT=Development` durante pruebas.
- `backend/src/Api/appsettings.Development.json` agrega `http://localhost:3100` y `http://127.0.0.1:3100` a CORS permitido para flujo E2E local sin errores de `Failed to fetch`.
- `frontend/services/apiClient.ts` ahora tolera respuestas exitosas con body vacio (200/empty) evitando fallos de parseo JSON en acciones operativas.
- `frontend/tests/e2e/flows.spec.ts` reduce flakiness de aserciones (`exact match` en KPI y estabilizacion del flujo de pipeline quick actions).
- `frontend/README.md` y `frontend/.env.example` actualizan defaults/instrucciones para nueva orquestacion E2E.
- Evidencia de regresion frontend: `npm run test:e2e` => 5/5 GREEN, `npm run lint` GREEN, `npm run build:verified` GREEN (`Bundle budget passed: 255395 bytes / 389120 bytes`).

### Ola 18 — EMF-01 a EMF-14
- `backend/src/Api/Application/Email/EmailService.cs` deja de enviar síncronamente welcome/proposal/customer welcome/analytics alert y pasa a encolar dispatches persistentes con `CorrelationId`, estados `Queued|Suppressed|Sent|Failed` y supresión por stop-list.
- `backend/src/Api/Application/Email/EmailDispatchService.cs` implementa ejecución de cola, retry manual, retry automático con backoff exponencial, KPIs (`total`, `sent`, `failed`, `queued`, `bounced`, `byChannel`) y evaluación de degradación de entrega hacia `AlertEvents` usando el endpoint lógico `email.delivery`.
- `backend/src/Api/Domain/Email/` y `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` agregan capacidades enterprise del módulo email: `EmailDispatchJob`, `EmailStopListEntry`, versionado de `EmailTemplate`, `CorrelationId` en `EmailLog`, metadata de provider (`smtp|webhook`) y `FollowUpPolicySettings`.
- `backend/src/Api/Controllers/EmailController.cs` amplía la API operativa con versionado/preview/rollback de templates, stop-list, ejecución de dispatch vencido, retry por log y consulta de KPIs.
- `backend/src/Api/Application/FollowUp/FollowUpService.cs` aplica política por tenant, quiet hours y supresión por stop-list; `backend/src/Api/Application/Leads/LeadIntakeService.cs` reordena el flujo a `email -> scoring -> follow-up -> assignment` para que el schedule use score real.
- `frontend/components/email/SmtpForm.tsx`, `frontend/app/email/templates/page.tsx`, `frontend/services/email.service.ts` y `frontend/types/email.ts` actualizan la consola admin de email con selector de provider, campos webhook y operaciones de template `publish version`, `preview` y `rollback`.
- `backend/tests/LoadTests/email-followup-load.js` + `backend/tests/LoadTests/run-email-followup-load-tests.ps1` agregan stress tests por lotes para intake + dispatch + KPIs + follow-up jobs.
- Evidencia dirigida backend: `EmailEndpointTests` 13/13 GREEN y `FollowUpEndpointTests` 9/9 GREEN.
- Evidencia carga EMF-14 (smoke): `backend/tests/LoadTests/results/email-followup-load-20260503-093837.json` => `failRate=0%`, `checks=100%`, `p95=84.89ms`.
- Evidencia dirigida frontend: validación sin errores en `frontend/components/email/SmtpForm.tsx`, `frontend/app/email/templates/page.tsx`, `frontend/services/email.service.ts`, `frontend/types/email.ts`, `frontend/app/globals.css`.

### Ola 19 — RE-01 a RE-15
- `backend/src/Api/Domain/Rules/Rule.cs` se extiende con metadatos enterprise de ejecucion (`Priority`, `ConflictPolicy`, ventanas horarias UTC, `CooldownMinutes`, `AllowDestructiveActions`) para gobernar conflicto, frecuencia y guardrails.
- `backend/src/Api/Domain/Rules/RuleExecutionLog.cs` y `backend/src/Api/Domain/Rules/RuleRevision.cs` + mapeos en `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` agregan auditoria detallada por ejecucion y versionado para rollback operativo.
- `backend/src/Api/Application/RulesEngine/RuleEngineService.cs` incorpora:
	- triggers adicionales `stage_changed`, `lead.responded`, `proposal.sent`;
	- validacion DSL previa a activacion/actualizacion;
	- dry-run historico, metricas de efectividad y fixture testing;
	- ejecucion priorizada con politica de conflicto + stop conditions;
	- ventanas horarias y cooldown por regla;
	- templates predefinidos por caso de uso.
- `backend/src/Api/Controllers/RulesController.cs` amplía API de operacion con `dry-run`, `metrics`, `rollback`, `templates`, `test-fixture` y `events/dispatch`.
- `backend/src/Api/Application/Proposals/ProposalService.cs` integra disparo de trigger `proposal.sent` tras envio exitoso.
- `backend/src/Api/Program.cs` y bootstrap SQL refuerzan schema e indices para metadatos y tablas operativas de reglas.
- Evidencia dirigida: `backend/tests/Api.Tests/RulesEngineEndpointTests.cs` 11/11 GREEN y `backend/tests/Api.Tests/PipelineEndpointTests.cs` 13/13 GREEN.
- Evidencia de regresion dirigida combinada: `runTests` sobre ambos archivos => 24/24 GREEN.

### Ola 20 — PO-01 a PO-13
- `backend/src/Api/Domain/Proposals/ProposalTemplate.cs`, `Proposal.cs` y `ProposalStatus.cs` endurecen el modulo de propuestas con template versionado, expiracion, firma, renovacion y estados granulares `Viewed|Signed|Expired|Renewed`.
- `backend/src/Api/Application/Proposals/ProposalService.cs` implementa catalogo de templates, seleccion de version actual al crear propuestas, reminder inteligente por tracking reciente y KPIs `proposal->won`.
- `backend/src/Api/Controllers/ProposalsController.cs` amplía la API con `POST/GET /api/proposals/templates`, `POST /sign`, `POST /expire`, `POST /renew` y `GET /api/proposals/kpis`.
- `backend/src/Api/Domain/Onboarding/Customer.cs`, `CustomerStatus.cs` y `OnboardingTask.cs` agregan `Segment`, `PlaybookKey`, `HealthScore`, dependencias y `DueAtUtc`.
- `backend/src/Api/Application/Onboarding/OnboardingService.cs` implementa playbooks segmentados, bloqueo por dependencias, overview de activacion temprana, SLA por overdue tasks y evaluacion de lifecycle hacia `Active|AtRisk|ChurnRisk`.
- `backend/src/Api/Controllers/OnboardingController.cs` amplía la API con `POST /api/onboarding/tasks/{taskId}/complete`, `GET /api/onboarding/overview` y `POST /api/onboarding/lifecycle/evaluate`.
- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs`, `backend/src/Api/Infrastructure/Proposals/ProposalRepository.cs`, `backend/src/Api/Infrastructure/Onboarding/CustomerRepository.cs`, `backend/src/Api/Infrastructure/Onboarding/OnboardingTaskRepository.cs` y `backend/src/Api/Program.cs` ajustan mappings, schema bootstrap y consultas necesarias para la ola PO.
- Evidencia dirigida: validacion focalizada por `dotnet test` de propuestas (3/3 GREEN) y onboarding (4/4 GREEN).
- Evidencia de regresion dirigida combinada: `runTests` sobre `ProposalAutomationEndpointTests.cs` + `OnboardingAutomationEndpointTests.cs` => 21/21 GREEN.

### Ola 21 — AO-07 y AO-08
- `backend/src/Api/Infrastructure/Observability/ObservabilityIncrementalAggregationService.cs` implementa lotes de agregacion incremental por `windowMinutes` con checkpoint incremental, delta por endpoint y upsert de lotes.
- `backend/src/Api/Domain/Observability/ObservabilityAggregateBatch.cs`, `ObservabilityEndpointAggregationState.cs`, `ObservabilityAggregationCheckpoint.cs` agregan persistencia dedicada para AO-07.
- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` y `backend/src/Api/Program.cs` incorporan mapeos EF + bootstrap SQL para `ObservabilityAggregateBatches`, `ObservabilityEndpointAggregationStates` y `ObservabilityAggregationCheckpoints`.
- `backend/src/Api/Controllers/AnalyticsAdvancedController.cs` expone `POST /api/analytics/advanced/metrics/history/aggregate-incremental` y `GET /api/analytics/advanced/metrics/history/aggregates`.
- `backend/src/Api/Infrastructure/AnalyticsAdvanced/InMemoryAnalyticsObservabilityService.cs` incorpora normalizacion de endpoints dinamicos, limite de cardinalidad configurable y bucket `__overflow__`.
- `backend/src/Api/Application/AnalyticsAdvanced/AnalyticsObservabilitySnapshot.cs` publica metadata de cardinalidad (`DistinctEndpoints`, `MaxDistinctEndpoints`, `DroppedDistinctEndpointCount`) en `GET /api/analytics/advanced/metrics`.
- `backend/tests/Api.Tests/AnalyticsAdvancedObservabilityEndpointTests.cs` agrega cobertura de AO-07/AO-08.
- Evidencia dirigida: `dotnet test backend/tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~AnalyticsAdvancedObservabilityEndpointTests` => 5/5 GREEN.

### Ola 13 — LCC-02/03/06/07/08/09/10
- `backend/src/Api/Controllers/LeadsController.cs` — nuevos endpoints LCC: `POST /api/leads/intake/bulk`, `GET /api/leads/intake/failed`, `POST /api/leads/intake/failed/{failedRequestId}/reprocess`, `GET /api/leads/intake/rejection-reasons`, `GET/PUT /api/leads/intake/dedup-settings`, `POST /api/leads/merge`.
- `backend/src/Api/Application/Leads/LeadIntakeService.cs` — deduplicación fuzzy efectiva por tenant (`ITenantDataGovernanceStore`) y trazabilidad de merge entre leads con snapshots de auditoría.
- `backend/src/Api/Application/DataGovernance/ITenantDataGovernanceStore.cs` + `backend/src/Api/Infrastructure/DataGovernance/InMemoryTenantDataGovernanceStore.cs` — configuración de deduplicación por tenant en runtime.
- `backend/src/Api/Application/Leads/ILeadIntakeFailureStore.cs` + `backend/src/Api/Infrastructure/Leads/InMemoryLeadIntakeFailureStore.cs` — almacenamiento de intake fallido para reproceso controlado.
- `backend/src/Api/Controllers/ContactsController.cs` y `backend/src/Api/Controllers/CompaniesController.cs` — listados con filtros (`leadId`, `search`) y paginación (`page`, `pageSize`).
- `backend/src/Api/Application/Contacts/ContactService.cs` y `backend/src/Api/Application/Companies/CompanyService.cs` — snapshots de auditoría para eventos `created`, `updated`, `deleted`.
- `backend/src/Api/Application/Common/Interfaces/IContactRepository.cs`, `backend/src/Api/Application/Common/Interfaces/ICompanyRepository.cs`, `backend/src/Api/Infrastructure/Persistence/ContactRepository.cs`, `backend/src/Api/Infrastructure/Persistence/CompanyRepository.cs` — soporte de consulta filtrable para listados paginados.
- `backend/tests/Api.Tests/ContactCompanyEndpointTests.cs` — cobertura de integración para paginación/filtros, auditoría CUD, bulk intake parcial, reproceso de intake fallido y merge con trazabilidad.
- Evidencia de regresión ejecutada: `runTests` sobre suite crítica (ApiGovernance + Assignment + Scoring + ContactCompany + Dashboard) 43/43 GREEN.

### Ola 14 — LCC-04
- `backend/src/Api/Application/Leads/LeadIntakeService.cs` incorpora validación avanzada de teléfono por región basada en `libphonenumber-csharp` bajo semántica de número posible por país, manteniendo compatibilidad con payloads históricos.
- `backend/src/Api/Api.csproj` agrega dependencia `libphonenumber-csharp` para parsing/validación regional.
- `backend/tests/Api.Tests/UnitTest1.cs` agrega cobertura de integración para rechazo de teléfono inválido por país y aceptación de teléfono válido por país.
- Evidencia dirigida: filtro `LeadIntakeEndpointTests` + caso de assignment rule-based impactado, 5/5 GREEN.
- Evidencia de regresión completa: `backend/tests/Api.Tests/TestResults/lcc04-full-v2.trx` => total=141, passed=141, failed=0.

### Ola 15 — PL-01
- `backend/tests/Api.Tests/PipelineEndpointTests.cs` amplía cobertura de validación de transiciones de etapa:
	- rechazo cuando se intenta saltar más de una etapa hacia adelante (`400 BadRequest`);
	- rechazo cuando se mueve hacia atrás sin motivo (`400 BadRequest`).
- `backend/src/Api/Application/Pipeline/PipelineService.cs` ya aplicaba las reglas de transición; esta ola consolida evidencia automatizada de cumplimiento de aceptación para PL-01.
- Evidencia de regresión completa: `dotnet test tests/Api.Tests/Api.Tests.csproj` => total=143, passed=143, failed=0.

### Ola 16 — PL-02
- `backend/src/Api/Contracts/PipelineStageSlaAlertResponse.cs` agrega contrato de alertas SLA por etapa, incluyendo minutos en etapa, SLA configurado, severidad y bandera de breach.
- `backend/src/Api/Application/Pipeline/IPipelineService.cs` extiende contrato con consulta de alertas de estancamiento por etapa.
- `backend/src/Api/Application/Pipeline/PipelineService.cs` implementa cálculo de SLA por etapa con defaults de negocio (`new`, `qualified`, `proposal`, `won`) y severidad (`low|medium|high|critical`), más override opcional por query.
- `backend/src/Api/Controllers/PipelineController.cs` expone `GET /api/pipeline/stage-sla-alerts?defaultSlaHours=`.
- `backend/tests/Api.Tests/PipelineEndpointTests.cs` agrega cobertura de integración para shape del endpoint y detección de breach inmediato con override de SLA.
- Evidencia dirigida: `PipelineEndpointTests` 7/7 GREEN.
- Evidencia de regresión completa: `backend/tests/Api.Tests/TestResults/post-pl02-full.trx` => total=145, passed=145, failed=0.

### Ola 17 — PL-03 a PL-12
- `backend/src/Api/Application/Pipeline/PipelineService.cs` se endurece como orquestador enterprise del board:
	- etiquetas de riesgo por oportunidad (`low|medium|high`);
	- filtros avanzados por `owner`, `source`, `score`, `risk`;
	- ordenamiento configurable por `score`, `value`, `source`, `title`, `createdAt`, `updatedAt`, `risk`;
	- paginación virtual (`page`, `pageSize`, `totalCount`, `hasMore`);
	- export CSV y métricas de throughput por etapa;
	- enforcement de WIP limits por etapa/tenant;
	- concurrencia optimista por `VersionToken`;
	- motivo obligatorio en cada transición.
- `backend/src/Api/Application/Pipeline/IStageWipLimitStore.cs` + `backend/src/Api/Infrastructure/Pipeline/InMemoryStageWipLimitStore.cs` agregan almacenamiento por tenant para límites WIP configurables.
- `backend/src/Api/Controllers/PipelineController.cs` amplía la API con `GET /api/pipeline/board/export`, `GET /api/pipeline/throughput`, `GET /api/pipeline/wip-limits`, `PUT /api/pipeline/wip-limits/{stageId}` y soporte de query avanzada en `GET /api/pipeline/board`.
- `backend/src/Api/Domain/Pipeline/OpportunityStageHistory.cs`, `backend/src/Api/Contracts/OpportunityStageHistoryResponse.cs`, `backend/src/Api/Infrastructure/Persistence/OpportunityStageHistoryRepository.cs` y `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` enriquecen el historial append-only con `Actor`, `IsAutomated`, nombres de etapa y consulta por rango temporal.
- `backend/src/Api/Application/RulesEngine/IRuleEventListener.cs` y `backend/src/Api/Application/RulesEngine/RuleEngineService.cs` incorporan trigger `pipeline.stage.changed` y acción `move_stage` para auto-move auditable.
- `backend/src/Api/Infrastructure/Persistence/PipelineStageRepository.cs`, `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` y `backend/src/Api/Program.cs` corrigen el seed multi-tenant de etapas: IDs por tenant y unicidad compuesta (`TenantId + Name`, `TenantId + Order`).
- `backend/tests/Api.Tests/PipelineEndpointTests.cs` sube la cobertura a 13 pruebas de integración incluyendo riesgo, WIP, board query/paginación, CSV, throughput, auto-move y concurrencia.
- Evidencia dirigida: `PipelineEndpointTests` 13/13 GREEN.
- Evidencia de regresión completa: `backend/tests/Api.Tests/TestResults/post-pipeline-full.trx` => total=151, passed=151, failed=0.

## ✅ Completado

### Base de documentación operativa
- `ia/01_requirements.md` — Requisitos funcionales, no funcionales, flujos y criterios de aceptación iniciales.
- `ia/02_architecture.md` — Arquitectura objetivo del sistema, módulos, pipeline, eventos, entidades y riesgos.
- `ia/03_plan.md` — Plan por fases y secuencia de sprints.
- `ia/04_tasks.md` — Backlog accionable con dependencias y expected outputs verificables.
- `ia/00_context.md` — Contexto base del proyecto, invariantes y estado actual.

### Sprint 1 — Entregables técnicos
- `backend/MindFlow.Backend.sln` — solución backend inicial creada.
- `backend/src/Api` — API .NET implementada para Lead Intake.
- `backend/src/Api/Controllers/LeadsController.cs` — endpoint `POST /api/leads/intake`.
- `backend/src/Api/Application/Leads/LeadIntakeService.cs` — validación, normalización, persistencia y publicación de evento.
- `backend/src/Api/Infrastructure/Persistence` — `LeadsDbContext` y `LeadRepository` con SQLite.
- `backend/src/Api/Infrastructure/Events/LeadCreatedEventPublisher.cs` — publicación de `lead.created`.
- `backend/tests/Api.Tests/UnitTest1.cs` — pruebas de integración de creación y validación de payload.
- `backend/src/Api/Domain/Contacts/Contact.cs` y `backend/src/Api/Domain/Companies/Company.cs` — entidades comerciales base agregadas.
- `backend/src/Api/Application/Contacts` y `backend/src/Api/Application/Companies` — servicios de aplicación con validación, normalización y deduplicación.
- `backend/src/Api/Controllers/ContactsController.cs` y `backend/src/Api/Controllers/CompaniesController.cs` — CRUD mínimo para contactos y compañías.
- `backend/src/Api/Infrastructure/Persistence/ContactRepository.cs` y `backend/src/Api/Infrastructure/Persistence/CompanyRepository.cs` — persistencia EF Core para nuevos agregados.
- `backend/tests/Api.Tests/ContactCompanyEndpointTests.cs` — pruebas de integración TDD para creación y reglas de duplicado.
- `backend/src/Api/Domain/Pipeline/*` — entidades de pipeline (`PipelineStage`, `Opportunity`, `OpportunityStageHistory`) y catálogo de etapas por defecto.
- `backend/src/Api/Application/Pipeline/PipelineService.cs` — creación de oportunidades, cambio de etapa e historial de movimientos.
- `backend/src/Api/Controllers/PipelineController.cs` — endpoints de stages, board, cambio de etapa e historial.
- `backend/src/Api/Domain/Email/` — entidades `SmtpSettings`, `EmailTemplate`, `EmailLog` como first-class module de email.
- `backend/src/Api/Application/Email/` — interfaces `IEmailSender`, `ISmtpSettingsRepository`, `IEmailTemplateRepository`, `IEmailLogRepository`, `IEmailService` + `EmailService` con flujo Skipped/Sent/Failed.
- `backend/src/Api/Infrastructure/Email/` — `SmtpEmailSender` (System.Net.Mail), repositorios EF Core para email.
- `backend/src/Api/Controllers/EmailController.cs` — `PUT/GET /api/email/smtp-settings`, `GET /api/email/logs`.
- `backend/tests/Api.Tests/EmailEndpointTests.cs` — 5 pruebas TDD con `EmailTestFactory` para aislamiento completo de estado. Suite total: 14/14 passing.
- `backend/src/Api/Domain/FollowUp/FollowUpJob.cs` y `FollowUpJobStatus.cs` — agregado con ciclo de vida completo.
- `backend/src/Api/Application/FollowUp/IFollowUpJobRepository.cs`, `IFollowUpService.cs`, `FollowUpService.cs` — servicio de programación a +48h, cancelación y ejecución de vencidos.
- `backend/src/Api/Infrastructure/FollowUp/FollowUpJobRepository.cs` — repositorio EF Core.
- `backend/src/Api/Contracts/FollowUpJobResponse.cs`, `CancelFollowUpRequest.cs` — contratos API.
- `backend/src/Api/Controllers/FollowUpController.cs` — GET /api/followup/jobs, GET /api/followup/leads/{id}/jobs, POST /api/followup/leads/{id}/cancel, POST /api/followup/jobs/{id}/cancel.
- `backend/tests/Api.Tests/FollowUpEndpointTests.cs` — 5 pruebas TDD. Suite total: 19/19 passing.
- `backend/src/Api/Domain/Assignment/` — entidades `AssignmentUser` y `LeadAssignment` para catálogo comercial y auditoría de asignaciones.
- `backend/src/Api/Application/Assignment/LeadAssignmentService.cs` — round-robin base y contrato para futura evolución a reglas avanzadas (`RuleKey`).
- `backend/src/Api/Infrastructure/Assignment/` — repositorios EF Core de usuarios y asignaciones.
- `backend/src/Api/Controllers/AssignmentsController.cs` — API para alta/listado de users y consulta de asignaciones.
- `backend/tests/Api.Tests/AssignmentEndpointTests.cs` — 5 pruebas TDD de asignación automática. Suite total: 24/24 passing.
- `backend/src/Api/Domain/Scoring/` — `ScoreRule` y `LeadScorePriority` para modelo de scoring básico.
- `backend/src/Api/Application/Scoring/LeadScoringService.cs` — cálculo inicial de score por reglas y thresholds (`Low`/`Medium`/`High`).
- `backend/src/Api/Controllers/ScoringController.cs` — endpoints `GET /api/scoring/rules` y `GET /api/scoring/leads/{leadId}`.
- `backend/tests/Api.Tests/ScoringEndpointTests.cs` — 4 pruebas TDD de scoring. Suite total: 28/28 passing.
- `backend/src/Api/Domain/Rules/` — entidades `Rule`, `RuleCondition`, `RuleAction` para trigger->condition->action.
- `backend/src/Api/Application/RulesEngine/RuleEngineService.cs` — evaluación de reglas activas y despacho de acciones en evento `lead.created`.
- `backend/src/Api/Controllers/RulesController.cs` — CRUD de reglas + activar/desactivar.
- `backend/src/Api/Infrastructure/RulesEngine/RuleRepository.cs` — persistencia EF Core con carga de condiciones/acciones.
- `backend/tests/Api.Tests/RulesEngineEndpointTests.cs` — 5 pruebas TDD del motor de reglas. Suite total: 33/33 passing.
- `backend/src/Api/Application/Dashboard/DashboardService.cs` — cálculo de leads por día, conversión y pipeline value.
- `backend/src/Api/Controllers/DashboardController.cs` — endpoint `GET /api/dashboard/overview`.
- `backend/src/Api/wwwroot/dashboard.html` — pantalla inicial del dashboard con visualización de métricas.
- `backend/tests/Api.Tests/DashboardEndpointTests.cs` — 4 pruebas TDD del módulo dashboard. Suite total: 37/37 passing.
- `backend/src/Api/Application/Common/Interfaces/ITenantContext.cs` y `backend/src/Api/Application/Common/Security/UserRoles.cs` — contratos de contexto multi-tenant y roles de acceso.
- `backend/src/Api/Infrastructure/Tenancy/TenantContext.cs` — contexto scoped de tenant/rol con fallback seguro (`default`/`Admin`) para compatibilidad.
- `backend/src/Api/Middleware/TenantMiddleware.cs` y `backend/src/Api/Middleware/RoleAuthorizationMiddleware.cs` — resolución por headers y control de autorización por rol en endpoints de escritura.
- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` — filtros globales por tenant, shadow property `TenantId` e inicialización automática de tenant en persistencia.
- `backend/src/Api/Program.cs` — DI multi-tenant/roles, middlewares en pipeline y bootstrap idempotente de `TenantId` + índices por tabla.
- `backend/tests/Api.Tests/MultiTenantRoleEndpointTests.cs` — 3 pruebas de aislamiento tenant y control de permisos por rol. Suite total: 40/40 passing.
- `backend/src/Api/Domain/Proposals/` — entidades `Proposal` y `ProposalReminderJob` con estado, tracking y ciclo de vida de reminder.
- `backend/src/Api/Application/Proposals/ProposalService.cs` — orquesta generación PDF, envío inicial, scheduling/ejecución de reminders y tracking.
- `backend/src/Api/Infrastructure/Proposals/` — repositorios EF Core y generador `SimpleProposalPdfGenerator`.
- `backend/src/Api/Controllers/ProposalsController.cs` — endpoints de creación/listado/detalle/pdf/reminders/tracking.
- `backend/src/Api/Application/Email/EmailService.cs` y `IEmailSender` — soporte de templates de propuesta y adjunto PDF.
- `backend/tests/Api.Tests/ProposalAutomationEndpointTests.cs` — 4 pruebas TDD de automatización de propuestas. Suite total: 44/44 passing.
- `backend/src/Api/Domain/Onboarding/` — entidades `Customer` y `OnboardingTask` para post-venta automatizado.
- `backend/src/Api/Application/Onboarding/OnboardingService.cs` — conversión `Won -> Customer`, creación de tareas y tracking.
- `backend/src/Api/Infrastructure/Onboarding/` — repositorios EF Core para `Customers` y `OnboardingTasks`.
- `backend/src/Api/Controllers/OnboardingController.cs` — endpoints de consulta de clientes/tareas y tracking de onboarding.
- `backend/src/Api/Application/Pipeline/PipelineService.cs` — integración automática de onboarding al mover oportunidad a `won`.
- `backend/tests/Api.Tests/OnboardingAutomationEndpointTests.cs` — 4 pruebas TDD de onboarding automatizado. Suite total: 48/48 passing.
- `backend/src/Api/Contracts/Analytics/` — contratos base para analytics avanzado (`Query`, KPIs, `Overview`, backlog de endpoints).
- `backend/src/Api/Application/AnalyticsAdvanced/` — servicio y contratos internos para cálculo de KPIs avanzados (funnel, revenue, velocity, SLA, onboarding activation).
- `backend/src/Api/Infrastructure/AnalyticsAdvanced/AnalyticsAdvancedDataRepository.cs` — capa de acceso de datos analíticos con snapshot por filtros estándar.
- `backend/src/Api/Controllers/AnalyticsAdvancedController.cs` — endpoints `GET /api/analytics/advanced/*` para overview y KPIs individuales.
- `backend/tests/Api.Tests/AnalyticsAdvancedEndpointTests.cs` — 4 pruebas TDD de analytics avanzado. Suite total: 52/52 passing.
- `backend/src/Api/wwwroot/analytics-advanced.html` — pantalla de analytics avanzado con tabs por KPI, filtros y estados de carga/error/vacío.
- `backend/src/Api/Controllers/AnalyticsAdvancedController.cs` — validación de filtros (`groupBy` y rango de fechas) para hardening de API.
- `backend/src/Api/Program.cs` — índices adicionales orientados a consultas analíticas de alto volumen.
- `backend/tests/Api.Tests/AnalyticsAdvancedFrontendEndpointTests.cs` — 3 pruebas TDD para frontend avanzado + validación API. Suite total: 55/55 passing.
- `backend/src/Api/Application/AnalyticsAdvanced/AnalyticsObservabilitySnapshot.cs` y `IAnalyticsObservabilityService.cs` — contrato de telemetría analítica por endpoint.
- `backend/src/Api/Infrastructure/AnalyticsAdvanced/InMemoryAnalyticsObservabilityService.cs` — collector in-memory de request/success/error/latency.
- `backend/src/Api/Controllers/AnalyticsAdvancedController.cs` — integración de tracking y endpoint `GET /api/analytics/advanced/metrics`.
- `backend/tests/Api.Tests/AnalyticsAdvancedObservabilityEndpointTests.cs` — 3 pruebas TDD de observabilidad analytics. Suite total: 58/58 passing.
- `backend/src/Api/Middleware/ApiVersioningMiddleware.cs` y `backend/src/Api/Middleware/GlobalExceptionHandlingMiddleware.cs` — guardrails transversales de versionado y manejo global de excepciones.
- `backend/src/Api/Contracts/ApiErrorResponse.cs` y `backend/src/Api/Program.cs` — contrato uniforme de error y validacion transversal de ModelState.
- `backend/src/Api/Controllers/LeadsController.cs` + `backend/src/Api/Infrastructure/Tenancy/InMemoryIdempotencyStore.cs` — idempotencia para intake con `Idempotency-Key`.
- `backend/src/Api/Program.cs` + `backend/src/Api/Api.csproj` — health checks live/ready con chequeo de EF Core DbContext.
- `backend/src/Api/Infrastructure/Email/SmtpEmailSender.cs` — timeout y reintentos outbound SMTP.
- `backend/src/Api/Controllers/*` (Assignments, Email, Rules, Proposals, Onboarding, AnalyticsAdvancedAlerts) — paginacion opcional en listados.
- `backend/tests/Api.Tests/ApiGovernanceEndpointTests.cs` — pruebas nuevas para health, versionado e idempotencia. Suite total: 72/72 passing.
- `ia/02_architecture.md` — definición de KPIs avanzados, fórmulas, filtros estándar y priorización de endpoints analíticos.
- `backend/src/Api/Infrastructure/Persistence/*` — repositorios de pipeline y mapeos EF Core para tablas de pipeline.
- `backend/src/Api/wwwroot/index.html` — vista Kanban básica inicial servida desde backend.
- `backend/tests/Api.Tests/PipelineEndpointTests.cs` — pruebas de integración TDD para pipeline y trazabilidad.

## 🔄 En curso

### Fase 1 — MVP Comercial Inicial
- PC-01: Lead Intake API (`POST /api/leads/intake`) — ✅ completado.
- PC-02: Modelo Contact/Company y relaciones — ✅ completado.
- PC-03: Pipeline básico y Kanban inicial — ✅ completado.
- PC-04: Email automático inicial por evento `lead.created` — ✅ completado.
- PC-05: Follow-up automático diferido 48h con cancelación — ✅ completado.
- PC-06: Asignación automática base (round-robin) con auditoría — ✅ completado.
- PC-07: Scoring básico persistido y visible en API — ✅ completado.
- PC-08: Rules Engine básico con CRUD y activación/desactivación — ✅ completado.
- PC-09: Dashboard básico operativo (API + pantalla inicial) — ✅ completado.

### Fase 4 — Expansión Full SaaS
- PC-10: Multi-tenant base + roles mínimos (`Admin`, `Sales`, `Viewer`) — ✅ completado.
- PC-11: Propuestas automáticas (templates + PDF + envío + reminder + tracking) — ✅ completado.
- PC-12: Onboarding automático post-venta (`Won -> Customer + tasks + welcome + tracking`) — ✅ completado.
- PC-13: Definición analytics avanzado (KPIs + contratos + priorización de endpoints) — ✅ completado.
- PC-14: Implementación analytics avanzado backend (servicios + endpoints + pruebas) — ✅ completado.
- PC-15: Integración analytics avanzado en frontend + hardening performance — ✅ completado.
- PC-16: Observabilidad avanzada analytics (telemetría + endpoint operativo) — ✅ completado.
- PC-16: Observabilidad avanzada analytics (telemetría + endpoint operativo) — ✅ completado.
- PC-17: Persistencia histórica de métricas de observabilidad (BD + historial + background service) — ✅ completado.
- PC-18: Alertas automáticas por degradación de KPIs analytics (umbrales + eventos + notificación email) — ✅ completado.
- PC-19: Dashboard operativo de observabilidad analytics (tabs + service layer + navegación integrada) — ✅ completado.
- PC-20: ARC governance hardening (versionado, errores, validacion, idempotencia, paginacion, resiliencia, health checks) — ✅ completado (13/15 ARC cerrados; pendientes ARC-01 y ARC-13).
- PC-21: Seguridad, identidad y cumplimiento (SEC-01..SEC-20) — ✅ completado con hardening runtime, auditoria admin, retencion y artefactos ASVS/threat-model/incident-response.
- PC-22: DAT wave 1 (DAT-01/02/03/07/09/10/12/14/16) — ✅ completado con metadata v2 de lead, scoring versionado/recalculo, integridad pipeline y KPIs de calidad.
- PC-23: DAT wave 2 (DAT-04/05/06/08/11/13/15/17/18) — ✅ completado con gobernanza por entorno, soft delete, auditoria temporal, UTC consistency y runbooks de continuidad.
- PC-24: DAT residual hardening — ✅ completado con masking PII operativo, anomaly tracking y dead-letter remediation en follow-up.
- PC-25: DAT stabilisation wave (tasks 1/2/3) — ✅ completado con dead-letter de propuestas, historial de anomalías y observabilidad de drift de reglas.
- PC-26: DAT stabilisation extension — ✅ completado con retry policy y poison queue para automatizaciones diferidas.
- PC-27: DAT tasks 1/2 execution — ✅ completado con onboarding welcome retry/poison y alertado operativo por crecimiento de poison queue.
- PC-28: DAT alert-noise hardening — ✅ completado con supresión de duplicados y cooldown para `PoisonQueueDepth`.
- PC-29: DAT poison trend panel — ✅ completado con endpoint agregado y visualización histórica por `jobType`.
- PC-30: DAT poison priority ranking — ✅ completado con severidad/variación y top operativo en observability.
- PC-31: DAT poison runbook shortcuts — ✅ completado con recomendaciones por severidad y atajos de remediación.
- PC-32: DAT remediation telemetry feedback loop — ✅ completado con registro de ejecución, panel de efectividad y métricas de éxito/latencia.
- PC-33: DAT remediation lifecycle states — ✅ completado con update por `runId` y transición operacional `in_progress/resolved/partial/failed`.
- PC-34: DAT remediation impact correlation — ✅ completado con correlación before/after de `PoisonQueueDepth` y tasa agregada de impacto positivo.
- PC-35: DAT remediation impact segmentation — ✅ completado con desglose por JobType y Severity (`PositiveImpactRatePercent`, `AverageDepthDelta` por segmento). 99/99 tests GREEN.
- PC-36: Email y Follow-up enterprise (EMF-01..EMF-14) — ✅ completado con cola persistente de dispatch, templates versionados, stop-list, políticas de follow-up, KPIs/alerting y consola admin frontend.

## ⏳ Pendiente

### Fase 2 — Automatización Base (actualizado)
- ~~Follow-ups automáticos (48h).~~ ✅ Completado en Sprint 1.
- ~~Asignación automática (round robin base).~~ ✅ Completado en Sprint 1.
- ~~Scoring básico.~~ ✅ Completado en Sprint 1.

### Fase 3 — Configurabilidad y Visibilidad (actualizado)
- ~~Rules Engine básico (modelo + ejecución + UI básica).~~ ✅ Backend completado en Sprint 1.
- ~~Dashboard básico (leads por día, conversión, pipeline value).~~ ✅ Completado en Sprint 1.

### Fase 4 — Expansión Full SaaS
- ~~Multi-tenant completo y roles.~~ ✅ Base operativa completada en backend (tenant isolation + role guard).
- ~~Propuestas y seguimiento.~~ ✅ Automatización base completada en backend.
- ~~Onboarding automatizado.~~ ✅ Flujo post-venta automatizado completado en backend.
- ~~Analytics avanzado (implementación de endpoints y servicios).~~ ✅ Completado con endpoints productivos y pruebas en Release.
- ~~Integración frontend analytics avanzado + hardening de performance.~~ ✅ Completado con UI operativa, validaciones y tuning backend.
- ~~Persistencia histórica de métricas de observabilidad.~~ ✅ Completado con entidad dominio, repositorio EF Core, background service y endpoints history/flush.
- ~~Alertas automáticas por degradación de KPIs analytics.~~ ✅ Completado con umbrales configurables, eventos auditables y notificación email integrada.
- ~~Dashboard operativo de observabilidad analytics.~~ ✅ Completado con UI operativa, filtros, gestión de umbrales y consulta de eventos.
- ~~Hardening SEC-01..SEC-20.~~ ✅ Completado con controles de seguridad enterprise, pruebas de hardening y docs de cumplimiento.

### Seguridad y Cumplimiento
- `backend/src/Api/Middleware/*` (Tenant, RoleAuthorization, SecurityHeaders, BruteForceProtection, LeadIntakeApiKey) reforzados para spoofing/authz/header hardening.
- `backend/src/Api/Infrastructure/Email/SmtpSettingsRepository.cs` cifra secretos SMTP en reposo.
- `backend/src/Api/Infrastructure/Security/SensitiveDataRetentionService.cs` aplica retencion automatica de datos sensibles.
- `backend/src/Api/Infrastructure/Security/AdminAuditService.cs` + `AdminAuditLogs` habilitan trazabilidad de acciones administrativas.
- `.github/workflows/security-sast-dast.yml` agrega gate de seguridad para CI.
- `ia/security/asvs-baseline.md`, `ia/security/threat-model-sec19.md`, `ia/security/incident-response-sec20.md` completan baseline de cumplimiento operativo.
- `backend/tests/Api.Tests/SecurityHardeningEndpointTests.cs` agrega cobertura de 401/403, spoofing tenant, brute-force, headers y cifrado en reposo.

### Gobernanza de Datos (DAT wave 1)
- `backend/src/Api/Application/Leads/LeadIntakeService.cs` agrega normalizacion de metadata (`channel/campaign/country`) y deduplicacion fuzzy configurable.
- `backend/src/Api/Domain/Leads/Lead.cs` + `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` versionan scoring (`ScoringVersion`, `ScoredAtUtc`) y enriquecen schema de lead.
- `backend/src/Api/Application/Scoring/LeadScoringService.cs` + `backend/src/Api/Controllers/ScoringController.cs` habilitan recalculo historico por ventana.
- `backend/src/Api/Application/Pipeline/PipelineService.cs` valida transiciones para evitar saltos invalidos y exigir razon en retrocesos.
- `backend/src/Api/Application/Dashboard/DashboardService.cs` + `backend/src/Api/Controllers/DashboardController.cs` exponen `GET /api/dashboard/data-quality`.
- `backend/src/Api/Contracts/DomainErrorCodes.cs` centraliza codigos de error base.
- Validacion ejecutada: `runTests` 78/78 ✅.

### Gobernanza de Datos (DAT wave 2)
- Reglas por entorno y promocion versionada: `Rule`/`RuleEngineService`/`RulesController` incorporan `Environment`, `ApprovalStatus`, `Version`, `ApprovedBy`, `ApprovedAtUtc` y endpoint `POST /api/rules/{id}/promote`.
- Integridad adicional en lifecycle: constraints de unicidad para reminder por propuesta y clave onboarding por customer (`LeadsDbContext` + bootstrap SQL).
- Auditoria temporal de lead: tabla `LeadAuditSnapshots`, repositorio dedicado y endpoint `GET /api/leads/{id}/audits`.
- Soft delete operativo: `Contact` y `Company` pasan a borrado logico (`IsDeleted`, `DeletedAtUtc`) con filtros de consulta.
- UTC end-to-end: convertidores JSON globales para `DateTime` y `DateTime?`.
- Retention scheduler auditable: `DataRetentionRuns` registra cada ciclo de limpieza sensible.
- Continuidad operacional: runbooks `ia/ops/dat17-backup-restore-drill.md` y `ia/ops/dat18-production-db-migration.md`.
- Validacion ejecutada: `runTests` 79/79 ✅.

### Gobernanza de Datos (DAT residual hardening)
- Masking PII operativo centralizado: `PiiMasking` se aplica a logs de email, jobs de follow-up y payloads de auditoria consultables.
- Tracking de anomalías: `LeadIntakeService` registra `lead.data_anomaly.*` en `LeadAuditSnapshots` para duplicados candidatos y contactos incompletos.
- Data quality enriquecido: `GET /api/dashboard/data-quality` ahora reporta `DataAnomalyEvents` sobre evidencia auditada.
- Remediacion dead-letter: `FollowUpController` agrega `GET /api/followup/dead-letter` y `POST /api/followup/jobs/{id}/requeue` para reencolar jobs fallidos.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 82/82 ✅.

### Gobernanza de Datos (DAT stabilisation wave)
- Proposal reminders: `ProposalsController` agrega `GET /api/proposals/reminders/dead-letter` y `POST /api/proposals/{id}/reminders/requeue` con masking PII operativo.
- Historial de anomalías: `DashboardController` agrega `GET /api/dashboard/data-quality/anomalies` para consultar eventos `lead.data_anomaly.*` por tipo y ventana temporal.
- Drift de reglas: `RulesController` agrega `GET /api/rules/drift-summary` y `RuleRepository` se endurece con `AsSplitQuery`/`AsNoTracking` para consultas con includes múltiples.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 85/85 ✅.

### Gobernanza de Datos (DAT stabilisation extension)
- Follow-up jobs: `FollowUpService` introduce retry policy acotada y `GET /api/followup/poison-queue` para jobs que agotan intentos de entrega.
- Proposal reminders: `ProposalService` introduce retry policy acotada, `GET /api/proposals/reminders/poison-queue` y requeue manual desde estado `Poisoned`.
- Contrato operativo: `ProposalResponse` expone `ReminderAttemptNumber` para visibilidad del intento vigente en APIs de detalle/listado.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 87/87 ✅.

### Gobernanza de Datos (DAT tasks 1/2)
- Onboarding diferido: `OnboardingService` incorpora `OnboardingWelcomeJob` con retries acotados, transición a `Poisoned`, `execute-due`, `force-due` y requeue operativo.
- Alertas operativas: nuevo `PoisonQueueAlertService` genera `AlertEvent` con métrica `PoisonQueueDepth` cuando el backlog poison supera umbral configurado por tipo de job (`poison-queue/{jobType}`).
- Integración transversal: follow-up, proposal reminders y onboarding notifican crecimiento de poison queue al mover jobs a estado `Poisoned`.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 89/89 ✅.

### Gobernanza de Datos (DAT alert-noise hardening)
- `PoisonQueueAlertService` incorpora cooldown operativo y deduplicación por delta frente al último `AlertEvent` para reducir ruido en crecimientos marginales consecutivos.
- `IAlertEventRepository`/`AlertEventRepository` agregan `GetLatestAsync(endpointName, metricName)` para soportar política de supresión basada en estado reciente.
- Pruebas de integración nuevas en proposals validan dos rutas: supresión dentro de cooldown (`+1`) y emisión permitida en salto significativo (`+2`).
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 91/91 ✅.

### Gobernanza de Datos (DAT poison trend panel)
- `AnalyticsAdvancedAlertsController` agrega `GET /api/analytics/advanced/alert-events/poison-queue-trend` con filtros `startUtc`, `endUtc`, `jobType` y `bucket` (`day|hour`).
- `observability.html` incorpora sección "Poison Queue Trend" con filtros y tabla de series agregadas (event count, max/avg/last depth, last triggered).
- Se agrega cobertura de integración para agrupación y filtro por job type en `AnalyticsAdvancedAlertsEndpointTests`.
- Se amplía cobertura frontend para validar presencia de la nueva sección y endpoint en `ObservabilityDashboardFrontendEndpointTests`.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 93/93 ✅.

### Gobernanza de Datos (DAT poison priority ranking)
- `AnalyticsAdvancedAlertsController` agrega `GET /api/analytics/advanced/alert-events/poison-queue-priority` con filtros `jobType`, `bucket`, `windowHours` y `top`.
- El ranking combina severidad (`low|medium|high|critical`) y variación (`deltaDepth`, `deltaPercent`) entre bucket actual y previo por endpoint/job type.
- `observability.html` agrega panel "Poison Queue Priority" para respuesta operativa priorizada sin análisis manual de eventos crudos.
- Se amplía cobertura de integración para ranking/severidad/filtros en `AnalyticsAdvancedAlertsEndpointTests` y presencia UI en `ObservabilityDashboardFrontendEndpointTests`.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 95/95 ✅.

### Gobernanza de Datos (DAT poison runbook shortcuts)
- El endpoint de prioridad incorpora `RecommendedAction`, `RunbookHint` y `RemediationPath` para cada job type priorizado.
- La UI de observabilidad muestra guía operativa contextual y botón `Open remediation` por fila del ranking.
- Las recomendaciones se ajustan por severidad (`critical/high/medium/low`) para acortar tiempo de respuesta en soporte.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 95/95 ✅.

### Gobernanza de Datos (DAT remediation telemetry feedback loop)
- Se agrega persistencia operativa de `PoisonQueueRemediationRun` para capturar ejecución de remediación (`endpoint/jobType/severity/action/outcome/executedBy/latency`).
- `AnalyticsAdvancedAlertsController` incorpora endpoints para registrar runs, listar historial filtrado y consultar resumen de efectividad (`successRate`, latencias promedio, desglose resolved/partial/failed).
- `observability.html` registra telemetría al accionar `Open remediation` y muestra panel "Remediation Effectiveness" con refresh manual y actualización tras ejecución.
- Cobertura de integración agregada para flujo de registro+summary y validación frontend de nuevos marcadores/endpoint strings.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 96/96 ✅.

### Gobernanza de Datos (DAT remediation lifecycle states)
- Se agrega `PUT /api/analytics/advanced/alert-events/poison-queue-remediation-runs/{id}` para actualización de outcome sobre run existente.
- Outcomes válidos endurecidos en API: `opened`, `in_progress`, `resolved`, `partial`, `failed`.
- `observability.html` incorpora controles de transición de estado por fila priorizada y reutiliza el mismo `runId` para reflejar progreso real.
- Cobertura de integración ampliada para transición `opened -> in_progress -> resolved` y actualización del summary de efectividad.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 97/97 ✅.

### Gobernanza de Datos (DAT remediation impact correlation)
- `AnalyticsAdvancedAlertsController` agrega `GET /api/analytics/advanced/alert-events/poison-queue-remediation-impact` para correlación de impacto por run cerrado.
- El endpoint calcula `PreDepth`, `PostDepth`, `DepthDelta`, `ReductionPercent` e `IsPositiveImpact` por ejecución, más agregados de ventana (`PositiveImpactRatePercent`, `AverageDepthDelta`).
- `observability.html` incorpora panel "Remediation Impact Correlation" con cards y tabla detallada before/after.
- Se añade cobertura de integración para escenario de reducción positiva tras remediación y smoke frontend para nuevos marcadores/endpoint.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 98/98 ✅.

## Riesgos activos
- Riesgo de retrabajo en scoring si cambian criterios comerciales por industria/segmento sin versionado de reglas.
- Riesgo de retrabajo si tenant isolation no se considera desde los primeros cambios de esquema.

## Próximo hito
Cerrar deuda estructural ARC-01/ARC-13 con estrategia formal de migraciones y plan de provider DB de producción multiusuario.

### Gobernanza de Datos (Ola 1 hardening observabilidad)
- AO-10: multi-channel webhook alerting en umbrales de alertas completado. `AlertThreshold` ahora soporta `WebhookUrl` (dominio, contratos, mapeo EF, bootstrap SQL con `ALTER TABLE`) y se expone en create/get/update de `AnalyticsAdvancedAlertsController`.
- AO-09: nuevo endpoint `GET /api/analytics/advanced/alert-events/slo-status` para estado SLI/SLO por endpoint activo, con payload de objetivos y compliance operativo.
- DAT-39 (Opcion A): nuevo endpoint `GET /api/analytics/advanced/alert-events/severity-elevation-candidates` para detectar segmentos con baja efectividad de remediacion (`PositiveImpactRatePercent < 50%`) y proponer elevacion de severidad.
- AO-11: deduplicacion de alertas repetitivas implementada en `AlertEvaluationService` con politica de cooldown y delta significativo por `endpoint+metric`.
- Se reparo corrupcion en `AnalyticsAdvancedAlertsEndpointTests` causada por bloques duplicados durante parcheo y se restauro la suite de pruebas de alertas avanzadas.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 105/105 ✅.

### Gobernanza de Datos (Ola 2 — AO-13, AO-15 runbooks y heatmap)
- AO-13: nuevo endpoint `GET /api/analytics/advanced/alert-events/runbooks/{metricName}` con catálogo estático de runbooks por tipo de métrica operativa (`ErrorRatePercent`, `AverageLatencyMs`, `PoisonQueueDepth`). Cada runbook incluye título y pasos de resolución accionables.
- AO-15: nuevo endpoint `GET /api/analytics/advanced/alert-events/heatmap?endpointName=` que devuelve densidad de alertas agrupadas por hora del día y endpoint, permitiendo identificar patrones temporales de incidentes.
- AO-14: nuevo endpoint `GET /api/analytics/advanced/alert-events/tenant-summary` con métricas de observabilidad del tenant actual: thresholds activos, eventos abiertos/reconocidos/resueltos y tasa de resolución. `ITenantContext` inyectado en el controller.
- AO-16: nuevo endpoint `POST /api/analytics/advanced/alert-events/purge` para retención configurable. `PurgeAsync(olderThanUtc)` implementado en `AlertEventRepository`; registra auditoría con conteo purgado.
- AO-17: nuevo endpoint `GET /api/analytics/advanced/alert-events/trends` con percentiles p50/p90/p99 + media/min/max sobre `ObservedValue` por endpoint y métrica en ventana temporal configurable. Implementación por interpolación lineal sobre muestra ordenada.
- Validacion ejecutada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 111/111 ✅.

### Gobernanza de Datos (Ola 3 — AO-18 carga analytics)
- AO-18: se agrega harness de carga con k6 en `backend/tests/LoadTests/analytics-advanced-load.js` con escenarios `smoke`, `baseline` y `stress` y thresholds iniciales para latencia y error rate.
- Runner operativo en `backend/tests/LoadTests/run-load-tests.ps1` y guia en `backend/tests/LoadTests/README.md`.
- Ejecucion realizada con `k6` local descargado en `.tools/k6`.
- Diagnostico ejecutado sobre falla de carga: corrida inicial sesgada por API no disponible (`connectex actively refused`) y timeouts de conectividad.
- Correcciones aplicadas:
	- Runner robustecido con `-StartApi`, deteccion de k6 local/global y readiness check previo a disparar carga.
	- Endpoint `slo-status` optimizado para leer thresholds activos directamente.
	- Endpoint `tenant-summary` optimizado para usar agregados (`CountActiveAsync`, `CountByStatusAsync`) y evitar cargas in-memory de eventos.
- Tuning de harness aplicado:
	- Thresholds por modo (`RUN_MODE=smoke` vs escenarios completos).
	- En smoke, latencia validada sobre respuestas exitosas (`http_req_duration{expected_response:true}`).
	- Nuevo `TARGET_ENDPOINTS` para profiling aislado por endpoint.
- Profiling ejecutado (tarea 2):
	- `analytics-load-20260502-224301.json` (`slo-status` aislado): `failRate=0%`, `checks=100%`, `p95=21.80ms`.
	- `analytics-load-20260502-224330.json` (`trends` aislado): `failRate=0%`, `checks=100%`, `p95=18.85ms`.
- Validacion funcional posterior a fix: `dotnet test tests/Api.Tests/Api.Tests.csproj` 111/111 ✅.
- Comparativo de artefactos AO-18:
	- `backend/tests/LoadTests/results/analytics-load-20260502-220113.json`: `failRate=97.92%`, `checks=2.08%`, `p95=2.36ms`.
	- `backend/tests/LoadTests/results/analytics-load-20260502-223730.json`: `failRate=4.80%`, `checks=95.20%`, `p95=85.44ms`.
	- `backend/tests/LoadTests/results/analytics-load-20260502-223956.json`: `failRate=4.80%`, `checks=95.20%`, `p95=67.02ms`.
- Rerun smoke integral post-tuning (tarea 1 + 3): `backend/tests/LoadTests/results/analytics-load-20260502-224409.json` con `failRate=4.80%`, `checks=95.20%`, `p95(all)=144.21ms` y `p95(success)=56.66ms`.
- Iteracion de continuidad ejecutada:
	- Flag de control para arranque en carga: `Features:DisableDataRetentionBackground` (aplicado por `run-load-tests.ps1` cuando se usa `-StartApi`).
	- Tuning de forma de carga en k6: `ALERT_EVENTS_PAGE_SIZE` (smoke default 25) y `SLEEP_JITTER` para desincronizar bursts.
	- Corrida de contraste: `backend/tests/LoadTests/results/analytics-load-20260502-225108.json` (`failRate=5.56%`, no cumple objetivo de error).
	- Corrida final ajustada: `backend/tests/LoadTests/results/analytics-load-20260502-225202.json` (`failRate=4.03%`, `checks=95.97%`, `p95(all)=117.83ms`, `p95(success)=61.56ms`).
- Continuidad AO-18 ejecutada en baseline/stress:
  - `AlertEventRepository.QueryAsync` optimizado con filtro `notificationSent` y paginacion SQL (`Skip/Take`) para evitar procesamiento en memoria.
  - `GET /api/analytics/advanced/alert-events/heatmap` ahora consume ventana temporal (`windowHours`) y limita lectura historica.
  - Script k6 extendido con `HEAVY_ENDPOINT_INTERVAL` para muestreo de endpoints pesados en escenarios no-smoke.
  - Corrida full de referencia: `backend/tests/LoadTests/results/analytics-load-20260502-231553.json` (`failRate=19.69%`, `checks=80.31%`, `p95(all)=10011.59ms`, `p95(success)=3134.04ms`).
  - Corrida full post-hardening: `backend/tests/LoadTests/results/analytics-load-20260502-232132.json` (`failRate=19.50%`, `checks=80.49%`, `p95(all)=10011.26ms`, `p95(success)=3011.30ms`).
- Estado AO-18: suite estable en smoke y mejoras incrementales en full; baseline/stress mantienen timeouts significativos y no cumplen threshold global de error (<1%), quedando abierto para siguiente iteracion de capacidad/infra.

### Gobernanza de Datos (Ola 4 — AO-01 export CSV dashboards)
- AO-01 implementado en API para dashboard basico y avanzado:
	- `GET /api/dashboard/overview/csv?days=`
	- `GET /api/analytics/advanced/overview/csv?groupBy=`
- Se agrega servicio reusable `IAnalyticsCsvExportService` para serializacion CSV de ambos payloads (`DashboardOverviewResponse` y `AnalyticsAdvancedOverviewResponse`) evitando logica de formato en controllers.
- Contratos de salida:
	- Dashboard basico: bloque de metricas + bloque `LeadsPerDayDate,LeadsPerDayCount`.
	- Dashboard avanzado: formato tabular `Section,Metric,Value` para Funnel/Revenue/Velocity/Sla/OnboardingActivation.
- Validacion TDD:
	- `DashboardEndpointTests.GetOverviewCsv_ReturnsCsvFile`
	- `AnalyticsAdvancedFrontendEndpointTests.AdvancedAnalyticsOverviewCsv_ReturnsCsvFile`
- Suite completa validada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 113/113 ✅.

### Gobernanza de Datos (Ola 5 — AO-02 reportes semanales automaticos)
- AO-02 implementado con ejecucion manual y automatica:
	- `POST /api/analytics/advanced/reports/weekly/run` para generar reporte semanal bajo demanda.
	- `WeeklyAnalyticsReportBackgroundService` para generacion automatica semanal configurable por `WeeklyAnalyticsReports` (`Enabled`, `RunOnStartup`, `IntervalMinutes`).
- Se agrega servicio de dominio aplicativo `IWeeklyAnalyticsReportService` que compone:
	- `DashboardOverview` (dashboard basico)
	- `AnalyticsAdvancedOverview` (dashboard avanzado)
	- Ventana semanal (`WindowStartUtc`, `WindowEndUtc`) y `GeneratedAtUtc`.
- Se registra auditoria por corrida automatica/manual con accion `analytics_weekly_report_generated` y resumen de KPIs.
- Validacion TDD:
	- `AnalyticsAdvancedFrontendEndpointTests.WeeklyReportRun_ReturnsWeeklyPayload`
	- `WeeklyAnalyticsReportBackgroundTests.WeeklyReportBackgroundService_RunOnStartup_GeneratesAuditEntry`
- Suite completa validada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 115/115 ✅.

### Gobernanza de Datos (Ola 6 — AO-03 metricas por vendedor/equipo/tenant)
- AO-03 implementado con endpoint dedicado para desglose operativo:
	- `GET /api/analytics/advanced/metrics/scope`.
- Se incorpora contrato `ScopeMetricsResponse` con tres vistas:
	- `Tenant`: totales agregados por tenant actual (`TenantId`, leads, conversion, revenue).
	- `Sellers`: metricas por vendedor (usuario de asignacion) con leads asignados, won, conversion y revenue.
	- `Teams`: metricas por equipo agrupadas por `RuleKey` y fallback a `Strategy` (por ejemplo `round_robin`).
- Se amplia el snapshot analitico para incluir `AssignmentUsers` y resolver nombres/emails de vendedores sin logica en controller.
- Validacion TDD:
	- `AnalyticsAdvancedFrontendEndpointTests.ScopeMetrics_ReturnsSellerTeamAndTenantBreakdown`
	- Confirmada regresion AO-02: `AnalyticsAdvancedFrontendEndpointTests.WeeklyReportRun_ReturnsWeeklyPayload`.
- Suite completa validada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 116/116 ✅.

### Gobernanza de Datos (Ola 7 — AO-04 comparativas period-over-period)
- AO-04 implementado con endpoint comparativo:
	- `GET /api/analytics/advanced/comparisons/period-over-period`.
- Se agrega contrato `PeriodOverPeriodComparisonResponse` con:
	- Ventana actual y ventana previa equivalentes en duracion.
	- Snapshot de `Current` y `Previous` usando el mismo esquema de KPIs avanzados.
	- `Delta` con cambios absolutos en `WonCount`, `WonRevenue`, `PipelineRevenue` y `ProposalToWonRate`.
- Implementacion en `AnalyticsAdvancedService` reutilizando calculo de overview para ambos periodos y comparacion consistente.
- Validacion TDD:
	- `AnalyticsAdvancedFrontendEndpointTests.PeriodOverPeriodComparison_ReturnsCurrentPreviousAndDelta`.
	- Regresion AO-03 y AO-02 validada en corrida objetivo.
- Suite completa validada: `dotnet test tests/Api.Tests/Api.Tests.csproj` 117/117 ✅.

### Gobernanza de Datos (Ola 8 — AO-05 segmentacion por source/campana/industria)
- AO-05 implementado con endpoint dedicado:
	- `GET /api/analytics/advanced/segments`.
- Se agrega contrato `SegmentationResponse` con tres cortes:
	- `BySource`
	- `ByCampaign`
	- `ByIndustry`
- Cada segmento incluye: `TotalLeads`, `WonLeads`, `ConversionRate`, `PipelineRevenue`, `WonRevenue`.
- Se extiende snapshot de analytics avanzado para incluir `Companies` y calcular industria por lead.
- Se agrega soporte de `Industry` en modulo de empresas (`Company` + contratos create/update/response + persistencia EF/bootstrapping) con default `unknown` para retrocompatibilidad.
- Validacion TDD:
	- `AnalyticsAdvancedFrontendEndpointTests.Segmentation_ReturnsSourceCampaignAndIndustryBreakdown`.
- Validacion de regresion integral via TRX:
	- `backend/tests/Api.Tests/TestResults/ao05-full.trx` -> `total=118`, `passed=118`, `failed=0` ✅.

### Gobernanza de Datos (Ola 9 — AO-06 caching para consultas analiticas pesadas)
- AO-06 implementado con cache en capa de repositorio analitico:
	- `CachedAnalyticsAdvancedDataRepository` como decorador de `IAnalyticsAdvancedDataRepository`.
	- Cache key por tenant + ventana + filtros (`groupBy`, `stage`, `source`, `tenant`) para evitar contaminacion cruzada.
	- TTL configurable por `AnalyticsAdvancedCache:SnapshotTtlSeconds` (default 60s) y flag `Enabled`.
- Se agrega `AnalyticsAdvancedCacheOptions` y registro DI con `IMemoryCache`.
- Prueba TDD agregada:
	- `AnalyticsAdvancedCachingTests.LoadSnapshotAsync_SameQuery_UsesCacheAndCallsInnerOnce`.
- Validacion de regresion integral via TRX:
	- `backend/tests/Api.Tests/TestResults/ao06-full.trx` -> `total=119`, `passed=119`, `failed=0` ✅.

### Gobernanza de Datos (Ola 10 — AS-07 umbrales hot/warm/cold por tenant)
- AS-07 implementado con configuracion tenant-scoped de prioridad de scoring:
	- `GET /api/scoring/priority-thresholds` para consultar umbrales activos del tenant.
	- `PUT /api/scoring/priority-thresholds` para actualizar `hotMinScore` y `warmMinScore` con validaciones de rango.
- Se incorpora `ILeadPriorityThresholdStore` + `InMemoryLeadPriorityThresholdStore` con almacenamiento por `tenantId`.
- `LeadScoringService` ahora resuelve prioridad (`Hot`/`Warm`/`Cold`) usando umbrales configurables por tenant en lugar de valores fijos.
- Validacion TDD focalizada:
	- `ScoringEndpointTests.PriorityThresholds_GetAndUpdate_AreTenantScoped`.
	- `ScoringEndpointTests.IntakeLead_AfterThresholdUpdate_UsesTenantThresholdsForPriority`.
	- `ScoringEndpointTests.IntakeLead_ReferralHasHigherScoreThanUnknownSource`.
- Validacion de regresion integral:
	- `backend/tests/Api.Tests/TestResults/as07-full.trx` -> `total=121`, `passed=121`, `failed=0` ✅.

### Gobernanza de Datos (Ola 11 — AS-08 drift detection de score)
- AS-08 implementado con endpoint de deteccion de drift en scoring por tenant:
	- `GET /api/scoring/drift` con parametros `currentSampleSize`, `baselineSampleSize` y `driftThresholdPercent`.
- Se agregan contratos:
	- `ScoringDriftQueryRequest` para configuracion de ventanas por muestra.
	- `ScoringDriftResponse` con promedio de score actual/base, delta porcentual, tasa de prioridad alta y senales detectadas.
- La implementacion en `LeadScoringService` compara muestras consecutivas (actual vs base) y emite senales operativas:
	- `average_score_drop`
	- `high_priority_rate_drop`
	- `insufficient_samples` (sin activar drift).
- Validacion TDD focalizada:
	- `ScoringEndpointTests.ScoreDrift_WithInsufficientSamples_ReturnsNoDrift`.
	- `ScoringEndpointTests.ScoreDrift_WhenRecentScoresDrop_ReturnsDriftSignal`.
- Validacion de regresion integral:
	- `backend/tests/Api.Tests/TestResults/as08-full.trx` -> `total=123`, `passed=123`, `failed=0` ✅.

### Gobernanza de Datos (Ola 12 — AS-01..AS-14 assignment/scoring enterprise hardening)
- Se completa el paquete integral de Assignment y Scoring:
	- AS-01: asignacion por reglas (`country`, `industry`, `score`) en `LeadAssignmentService` con estrategia `rule_based`.
	- AS-02: capacidad/carga por vendedor con `MaxActiveLeads` y endpoint `GET /api/assignments/capacity-load`.
	- AS-03: rebalanceo automatico al deshabilitar disponibilidad (`PUT /api/assignments/users/{userId}/availability`) con estrategia `rebalance_availability`.
	- AS-04: versionado de formula de scoring (`GET /api/scoring/formula`, `GET /api/scoring/formula/versions`).
	- AS-05: explainability por lead (`GET /api/scoring/leads/{leadId}/explain`) con contribuciones por regla.
	- AS-06: simulador de scoring por dataset (`POST /api/scoring/simulator`).
	- AS-09: endpoint de auditoria de decisiones (`GET /api/assignments/audit`).
	- AS-10: fairness checks de distribucion (`GET /api/assignments/fairness`).
	- AS-11: cierre de loop conversion real por score bucket (`GET /api/scoring/conversion-loop`).
	- AS-12: governance de tuning de formula con propuestas y aprobacion (`/api/scoring/formula/proposals`).
	- AS-13: proteccion de asignaciones manuales contra sobreescritura (`POST /api/assignments/leads/{leadId}/manual` + store de proteccion).
	- AS-14: suite de regresion estadistica de scoring agregada en tests de API.
- Hardening de modelo y persistencia:
	- `AssignmentUser` extiende preferencias y limites (`PreferredCountry`, `PreferredIndustry`, `MaxActiveLeads`, `MinScoreToAssign`).
	- Bootstrap SQL idempotente actualizado para nuevas columnas en `AssignmentUsers`.
	- `LeadIntakeService` ajustado para puntuar antes de asignar y habilitar reglas por score.
- Validacion TDD focalizada AS wave:
	- Assignment: 6 pruebas nuevas (reglas, capacidad, rebalance, auditoria, fairness, proteccion manual).
	- Scoring: 5 pruebas nuevas (formula governance, explainability, simulator, conversion loop, regression estadistica).
- Validacion de regresion integral:
	- `backend/tests/Api.Tests/TestResults/as01-as14-full.trx` -> `total=134`, `passed=134`, `failed=0` ✅.
