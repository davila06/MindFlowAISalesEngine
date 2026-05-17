# 04 — Tareas Accionables

> **Última actualización:** 2026-05-04
> **Prioridad actual:** Construir Sprint 1 del MVP comercial inicial

## TASK-ARC-01: Formalizar estrategia de migraciones de base de datos
**Estado:** ✅ Completado

## TASK-ARC-14: Eliminar bootstrap SQL legacy en arranque
**Estado:** ✅ Completado

Title: Retirar DDL/seed manual en Program.cs y consolidar en migraciones EF Core

Context:
El API mantenia un bloque legacy de bootstrap con `EnsureCreated` + `ALTER TABLE/CREATE TABLE` + seed SQL inline, lo que duplicaba responsabilidades con EF migrations y generaba riesgo de drift.

Steps:
1. Crear migracion de seed (`M0002_SeedData`) para plantillas email y pipeline stages.
2. Eliminar el bloque legacy de bootstrap del arranque y dejar unicamente `Database.Migrate()`.
3. Ajustar seed de `EmailTemplates` a insercion tipada (`InsertData` con `Guid`) para compatibilidad EF Core/SQLite.
4. Limpiar artefactos temporales de debugging y restaurar middleware de errores a mensaje estandar.
5. Validar build + suite de tests para confirmar ausencia de regresion en versionado de templates.

Expected Output:
Arranque limpio, sin SQL legacy inline, con esquema/seed gestionado completamente por migraciones EF Core.

Dependencies:
TASK-ARC-01.

Evidence:
- `backend/src/Api/Program.cs` (arranque con `dbContext.Database.Migrate()`).
- `backend/src/Api/Migrations/20260504160000_M0002_SeedData.cs` (seed formal sin SQL legacy inline en startup).
- `backend/src/Api/Controllers/EmailController.cs` (flujo de versionado estabilizado).
- `backend/src/Api/Middleware/GlobalExceptionHandlingMiddleware.cs` (mensaje de error estandar restaurado).
- `backend/tests/Api.Tests/EmailEndpointTests.cs` (`EmailTemplateVersioning_SupportsPreviewAndRollback` GREEN).

Title: Migrar de bootstrap SQL manual a baseline EF Core migration-aware

Context:
El backend operaba con `EnsureCreated` y DDL idempotente inline en `Program.cs`, sin historial formal de migraciones.

Steps:
1. Habilitar tooling de migraciones EF Core en el proyecto API.
2. Generar baseline migration con estado de esquema actual.
3. Ajustar arranque para preferir `Migrate()` manteniendo compatibilidad legacy.
4. Baselinar `__EFMigrationsHistory` en entornos heredados.
5. Publicar runbook operativo de apply/script/rollback.

Expected Output:
Esquema versionado por migraciones EF con ruta de evolución reproducible y compatible con entornos existentes.

Dependencies:
ARC-01.

Evidence:
- `backend/src/Api/Migrations/20260504145649_M0001_Baseline.cs`.
- `backend/src/Api/Migrations/LeadsDbContextModelSnapshot.cs`.
- `backend/src/Api/Api.csproj` con `Microsoft.EntityFrameworkCore.Design`.
- `backend/src/Api/Program.cs` con arranque híbrido `Migrate()` + fallback legacy.
- `docs/operations/db-migrations-runbook.md`.

## TASK-ARC-13-POC-01: Iniciar PoC de provider DB multiusuario
**Estado:** ✅ Completado

Title: Definir marco comparativo SQLite vs SQL Server vs PostgreSQL para decisión de producción

Context:
Falta evidencia comparativa de rendimiento/concurrencia para seleccionar provider de producción SaaS.

Steps:
1. Definir matriz de evaluación con pesos y métricas.
2. Definir escenarios de benchmark y dataset comparable.
3. Preparar corrida A/B y formato de reporte.
4. Registrar decisión final en ADR tras resultados.

Expected Output:
PoC ejecutada con benchmark comparativo y provider objetivo seleccionado para producción.

Dependencies:
ARC-13.

Evidence:
- `docs/architecture/db-provider-poc.md`.
- `backend/tests/LoadTests/results/db-poc-sqlite-smoke-20260504-151000.json`.
- `backend/tests/LoadTests/results/db-poc-sqlserver-smoke-20260504-151221.json`.
- `backend/tests/LoadTests/results/db-poc-postgres-smoke-20260504-151314.json`.
- `backend/tests/LoadTests/results/db-poc-sqlite-full-20260504-153218.json`.
- `backend/tests/LoadTests/results/db-poc-sqlserver-full-20260504-154134.json`.
- `backend/tests/LoadTests/results/db-poc-postgres-full-20260504-155332.json`.
- `ia/06_decisions.md` (ADR-72).
- `docs/operations/db-provider-cutover-plan.md`.

## TASK-FULL-RE-01: Cerrar ola Rules Engine (RE-01 a RE-15)
**Estado:** ✅ Completado

Title: Endurecer Rules Engine a nivel enterprise para automatizacion segura y auditable

Context:
Se requiere cerrar integralmente el backlog RE-01..RE-15 con trazabilidad de ejecucion, controles anti-riesgo y operacion por tenant.

Steps:
1. Extender metadatos de regla (prioridad, conflicto, ventanas horarias, cooldown, guardrails).
2. Ampliar triggers a `stage_changed`, `lead.responded` y `proposal.sent`.
3. Implementar validacion DSL previa a activacion/actualizacion.
4. Agregar dry-run historico, metricas por regla, templates y fixture testing.
5. Persistir auditoria por ejecucion y revisions para rollback.
6. Exponer endpoints operativos y cubrir con pruebas de integracion.

Expected Output:
Rules Engine enterprise con ejecucion controlada, auditable y operable por API.

Dependencies:
PC-08, PC-17, PC-24, PC-36.

Evidence:
- Dominio y contratos extendidos en `backend/src/Api/Domain/Rules/Rule.cs` y `backend/src/Api/Contracts/Rule*.cs`.
- Nuevas entidades operativas: `RuleExecutionLog` y `RuleRevision`.
- Motor endurecido en `backend/src/Api/Application/RulesEngine/RuleEngineService.cs`.
- Endpoints nuevos en `backend/src/Api/Controllers/RulesController.cs`: dry-run, metrics, rollback, templates, fixture test y dispatch de eventos.
- Integracion `proposal.sent` en `backend/src/Api/Application/Proposals/ProposalService.cs`.
- Persistencia EF/SQL actualizada en `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` y `backend/src/Api/Program.cs`.
- Pruebas de integracion en `backend/tests/Api.Tests/RulesEngineEndpointTests.cs`.
- Validacion ejecutada: `RulesEngineEndpointTests` 11/11 GREEN, `PipelineEndpointTests` 13/13 GREEN, corrida combinada 24/24 GREEN.

## TASK-FULL-PO-01: Cerrar ola Proposals y Onboarding (PO-01 a PO-13)
**Estado:** ✅ Completado

Title: Endurecer propuestas y onboarding a nivel enterprise para cierre comercial y activacion post-venta

Context:
Se requiere cerrar integralmente PO-01..PO-13 con versionado de templates, lifecycle completo de propuestas, playbooks segmentados y health/lifecycle de onboarding.

Steps:
1. Persistir templates versionados de propuestas y asociar nuevas propuestas a la version actual.
2. Extender lifecycle de propuestas con estados `Viewed|Signed|Expired|Renewed`, firma y renovacion.
3. Aplicar reminder inteligente segun tracking reciente y exponer KPIs de propuesta->won.
4. Implementar playbooks de onboarding por segmento con dependencias, due dates y completion guard.
5. Calcular overview de onboarding, early activation, health score y evaluacion de churn risk.
6. Exponer endpoints operativos y cubrir con pruebas de integracion de proposals/onboarding.

Expected Output:
Modulo proposals/onboarding enterprise, auditable y operable por API.

Dependencies:
PC-11, PC-12, PC-17, PC-24.

Evidence:
- Dominio extendido en `backend/src/Api/Domain/Proposals/Proposal.cs`, `ProposalTemplate.cs`, `ProposalStatus.cs`, `backend/src/Api/Domain/Onboarding/Customer.cs`, `CustomerStatus.cs`, `OnboardingTask.cs`.
- Contratos API nuevos/extendidos en `backend/src/Api/Contracts/CreateProposalTemplateRequest.cs`, `ProposalTemplateResponse.cs`, `ProposalSignRequest.cs`, `ProposalRenewRequest.cs`, `ProposalKpiResponse.cs`, `OnboardingOverviewResponse.cs`, `ProposalResponse.cs`, `CustomerResponse.cs`, `OnboardingTaskResponse.cs`.
- Servicios enterprise en `backend/src/Api/Application/Proposals/ProposalService.cs` y `backend/src/Api/Application/Onboarding/OnboardingService.cs`.
- Endpoints nuevos en `backend/src/Api/Controllers/ProposalsController.cs` y `backend/src/Api/Controllers/OnboardingController.cs`.
- Persistencia y bootstrap ajustados en `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` y `backend/src/Api/Program.cs`.
- Validacion ejecutada: `runTests` sobre `ProposalAutomationEndpointTests.cs` + `OnboardingAutomationEndpointTests.cs` => 21/21 GREEN.

## TASK-FULL-AO-01: Cerrar AO-07 y AO-08 de observabilidad
**Estado:** ✅ Completado

Title: Endurecer observabilidad con agregacion incremental y control de cardinalidad

Context:
Se requeria cerrar los dos pendientes de la ola AO: AO-07 (lotes de agregacion incremental) y AO-08 (control de cardinalidad de eventos), sin regresiones sobre endpoints existentes.

Steps:
1. Implementar pipeline incremental sobre snapshots persistidos con checkpoints por ventana.
2. Persistir lotes agregados y estado acumulado por endpoint para deltas consistentes.
3. Exponer endpoints de ejecucion y consulta de agregados.
4. Agregar normalizacion de endpoint y limite de cardinalidad con bucket overflow.
5. Publicar metadata de cardinalidad en snapshot operativo de metrics.
6. Cubrir con pruebas dirigidas de integracion y validacion de compatibilidad.

Expected Output:
Observabilidad con historico agregado incremental y telemetria resistente a explosion de cardinalidad.

Dependencies:
PC-16, PC-17, PC-18.

Evidence:
- Contratos app: `backend/src/Api/Application/Observability/IObservabilityIncrementalAggregationService.cs`.
- Servicio incremental: `backend/src/Api/Infrastructure/Observability/ObservabilityIncrementalAggregationService.cs`.
- Entidades de persistencia AO-07: `ObservabilityAggregateBatch`, `ObservabilityEndpointAggregationState`, `ObservabilityAggregationCheckpoint`.
- Modelo/SQL/DI actualizados: `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` y `backend/src/Api/Program.cs`.
- Endpoints nuevos: `POST /api/analytics/advanced/metrics/history/aggregate-incremental` y `GET /api/analytics/advanced/metrics/history/aggregates` en `backend/src/Api/Controllers/AnalyticsAdvancedController.cs`.
- AO-08 implementado en `backend/src/Api/Infrastructure/AnalyticsAdvanced/InMemoryAnalyticsObservabilityService.cs` + metadata en `AnalyticsObservabilitySnapshot`.
- Pruebas: `backend/tests/Api.Tests/AnalyticsAdvancedObservabilityEndpointTests.cs`.
- Validacion ejecutada: `dotnet test backend/tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~AnalyticsAdvancedObservabilityEndpointTests` => 5/5 GREEN.

## TASK-FULL-DOC-01: Cerrar ola Documentacion y gobierno de producto (DOC-01 a DOC-12)
**Estado:** ✅ Completado

Title: Institucionalizar documentacion de producto, operaciones y gobernanza tecnica

Context:
Se requiere cerrar integralmente DOC-01..DOC-12 para eliminar drift entre codigo y documentacion, publicar contrato OpenAPI versionado y establecer artefactos de gobierno operativo.

Steps:
1. DOC-01: Exportar y publicar OpenAPI versionado desde runtime real.
2. DOC-02: Crear runbooks operativos por modulo.
3. DOC-03: Definir playbook de incidentes por severidad (SEV-1..SEV-4).
4. DOC-04: Publicar catalogo vivo de eventos de dominio.
5. DOC-05: Publicar diccionario de datos por entidad.
6. DOC-06: Publicar matriz RBAC vigente por dominio de endpoint.
7. DOC-07: Definir contribution guide y coding standards.
8. DOC-08: Crear changelog tecnico por release.
9. DOC-09: Registrar decision estructural de governance documental en ADR.
10. DOC-10: Definir dashboard de KPIs de producto.
11. DOC-11: Definir DoD por tipo de feature.
12. DOC-12: Definir auditoria mensual doc-codigo y automatizar validacion base.

Expected Output:
Sistema documental enterprise con trazabilidad, control de versiones y auditoria recurrente de coherencia doc-codigo.

Dependencies:
Olas RE, PO, AO, FE y OPS completadas.

Evidence:
- `docs/api/v1/openapi.json` exportado desde `/openapi/v1.json` (runtime).
- `docs/api/README.md` con policy de versionado y publicacion.
- `docs/operations/runbooks-by-module.md` (DOC-02).
- `docs/operations/incident-severity-playbook.md` (DOC-03).
- `docs/architecture/domain-event-catalog.md` (DOC-04).
- `docs/architecture/data-dictionary.md` (DOC-05).
- `docs/operations/rbac-matrix.md` (DOC-06).
- `CONTRIBUTING.md` + `docs/engineering/coding-standards.md` (DOC-07).
- `CHANGELOG.md` (DOC-08).
- `ia/06_decisions.md` actualizado con ADR-70 (DOC-09).
- `docs/product/kpi-dashboard.md` (DOC-10).
- `docs/product/definition-of-done.md` (DOC-11).
- `docs/operations/doc-code-coherence-audit.md` + `.github/workflows/docs-coherence-audit.yml` (DOC-12).

## TASK-FULL-OPS-01: Cerrar ola DevOps, release y operación (OPS-01 a OPS-18)
**Estado:** ✅ Completado

Title: Implementar ciclo completo de DevOps, release management y operación para MindFlow

Context:
Se requiere cerrar integralmente el backlog OPS-01..OPS-18 con CI/CD enterprise, blue/green deployments, feature flags, observabilidad operacional, backups encriptados, DR plan y patching automatizado.

Steps:
1. OPS-01: Fortalecer CI quality gate (cobertura, type-check, bundle budget, E2E dedicado).
2. OPS-02/06: CD pipeline con aprobaciones de entorno, blue/green slot swap y auto-rollback.
3. OPS-03: Config isolation por entorno (Staging/Production appsettings).
4. OPS-04: Secrets management documentado en pipeline.
5. OPS-05: Feature flags service (IFeatureFlagService + ConfigurationFeatureFlagService + API endpoint).
6. OPS-07: Automated rollback (workflow + script de rollback).
7. OPS-08: Post-deploy smoke tests (infra/scripts/smoke-test.sh integrado en CD).
8. OPS-09/10: SRE summary endpoint y cost/capacity monitoring por tenant.
9. OPS-11: DR plan (docs/operations/dr-plan.md).
10. OPS-12/13: Background job observability y alertas de fallo.
11. OPS-14: Environment config audit endpoint.
12. OPS-15: DORA metrics workflow (Deployment Frequency, Lead Time, CFR, MTTR).
13. OPS-16: Backup encriptado GPG + restore + backup-verify workflow.
14. OPS-17: Dependency review workflow + dependency policy doc.
15. OPS-18: Dependabot + patching cadence doc.

Expected Output:
Plataforma MindFlow con ciclo DevOps completo, DR documentado, observabilidad operacional y dependencias gestionadas automaticamente.

Dependencies:
Todas las olas anteriores (FE 4.5, AO, EMF, RE, PO, LCC, PL).

Evidence:
- `.github/workflows/quality-fullstack.yml` reforzado (OPS-01).
- `.github/workflows/cd-release.yml` nuevo (OPS-02/06).
- `.github/workflows/rollback-health.yml` nuevo (OPS-07).
- `.github/workflows/dora-metrics.yml` nuevo (OPS-15).
- `.github/workflows/backup-verify.yml` nuevo (OPS-16).
- `.github/workflows/dependency-review.yml` nuevo (OPS-17/18).
- `.github/dependabot.yml` nuevo (OPS-17/18).
- `backend/src/Api/appsettings.Staging.json` nuevo (OPS-03).
- `backend/src/Api/appsettings.Production.json` nuevo (OPS-03).
- `backend/src/Api/Application/Common/FeatureFlags/IFeatureFlagService.cs` + `FeatureFlags.cs` nuevos (OPS-05).
- `backend/src/Api/Infrastructure/FeatureFlags/ConfigurationFeatureFlagService.cs` nuevo (OPS-05).
- `backend/src/Api/Controllers/OpsController.cs` nuevo con 6 endpoints (OPS-05/09/10/12/13/14).
- `infra/scripts/smoke-test.sh` nuevo (OPS-08).
- `infra/scripts/rollback.sh` nuevo (OPS-07).
- `infra/scripts/backup.sh` nuevo (OPS-16).
- `infra/scripts/restore.sh` nuevo (OPS-16).
- `docs/operations/dr-plan.md` nuevo (OPS-11).
- `docs/operations/dependency-policy.md` nuevo (OPS-17).
- `docs/operations/patching-cadence.md` nuevo (OPS-18).
- Compilacion backend: 0 errores, 0 advertencias.
- Pruebas backend: 73/79 pass (5 fallos pre-existentes no relacionados con OPS).

## TASK-FULL-FE-15-01: Endurecer FE-15 para ejecucion E2E deterministica
**Estado:** ✅ Completado

Title: Eliminar dependencia de runtime manual en pruebas E2E de frontend

Context:
La suite Playwright de FE-15 existia pero era sensible al entorno local: podia reutilizar servidores no relacionados y fallar cuando backend/frontend no estaban levantados manualmente.

Steps:
1. Configurar `webServer` en Playwright para auto-levantar backend y frontend.
2. Aislar el frontend E2E en puerto dedicado (`3100`) y desactivar `reuseExistingServer`.
3. Forzar `ASPNETCORE_ENVIRONMENT=Development` para alinear CORS en corridas de prueba.
4. Ajustar cliente API frontend para tolerar respuestas exitosas con body vacio.
5. Endurecer aserciones E2E para evitar flakiness por ambiguedad de selectores y normalizacion de datos.

Expected Output:
Suite FE-15 autoejecutable, estable y sin precondiciones manuales de runtime.

Dependencies:
FE-01..FE-16.

Evidence:
- Configuracion Playwright: `frontend/playwright.config.ts` (auto-start backend/frontend, puerto 3100, env dev).
- CORS dev actualizado: `backend/src/Api/appsettings.Development.json`.
- Hardening cliente API: `frontend/services/apiClient.ts`.
- Ajustes de suite E2E: `frontend/tests/e2e/flows.spec.ts`.
- Documentacion de ejecucion: `frontend/README.md` y `frontend/.env.example`.
- Validacion ejecutada: `npm run test:e2e` => 5/5 GREEN, `npm run lint` GREEN, `npm run build:verified` GREEN.

## TASK-UI-ENT-01: Sprint UI Enterprise Fase 0 (riesgo alto)
**Estado:** ✅ Completado

Title: Cerrar brechas criticas de accesibilidad, seguridad de render y consistencia i18n

Context:
El analisis de `mejorasUI.md` detecta tres riesgos inmediatos: uso de confirmaciones nativas no accesibles, preview HTML sin sanitizacion explicita y strings hardcodeadas en pantallas de email/templates.

Steps:
1. Reemplazar `window.confirm` por modal accesible reutilizable (`ConfirmDialog`) con focus trap, teclas ESC/Enter y ARIA completo.
2. Introducir sanitizacion robusta para preview HTML antes de `dangerouslySetInnerHTML`.
3. Extraer strings hardcodeadas de `email/templates` a i18n (`messages.ts`) y consumir via `t(...)`.
4. Publicar tokens semanticos UI v1 y mapear componentes base a dichos tokens.

Expected Output:
Base UI segura y accesible para escalar funcionalidades sin deuda critica.

Dependencies:
FE-15, DOC-07.

Acceptance Criteria:
- No existe `window.confirm` en `frontend/components` o `frontend/app`.
- Todo render HTML dinamico pasa por sanitizacion previa.
- 0 textos hardcodeados en `frontend/app/email/templates/page.tsx`.
- Componentes base (`Button`, `Field`, `EmptyState`, `ErrorState`) consumen tokens semanticos publicados.

Evidence (Expected):
- `frontend/components/ui/ConfirmDialog.tsx`.
- `frontend/app/email/templates/page.tsx` refactor i18n + preview seguro.
- `frontend/i18n/messages.ts` con nuevas claves.
- `frontend/app/globals.css` o `frontend/styles/tokens.css` con tokens UI v1.
- `npm run lint` + `npm run test:e2e` sin regresion en flujos existentes.

Evidence (Implemented):
- `frontend/components/ui/ConfirmDialog.tsx` operativo con foco gestionado, teclado y ARIA completo.
- `frontend/services/htmlSanitizer.ts` + consumo en `frontend/app/email/templates/page.tsx` para render HTML seguro.
- `frontend/i18n/messages.ts` ampliado para coverage de templates/pipeline/rules sin hardcode residual.
- Tokens semanticos y clases UI en `frontend/app/globals.css` (`.dialog-*`, `.rule-builder-grid`, variables de color semanticas).
- Validacion de cierre registrada en `ia/05_progress.md` (build/lint/E2E).

## TASK-UI-ENT-02: Sprint UI Enterprise Fase 1 (estabilidad operativa)
**Estado:** ✅ Completado

Title: Consolidar capa de datos UI, boundaries por ruta y logs escalables

Context:
La UI actual recarga vistas completas tras acciones puntuales y no tiene estrategia uniforme de cache/invalidation por dominio.

Steps:
1. Introducir capa de queries estandarizada por dominio (cache, invalidacion y optimistic updates).
2. Aplicar optimistic update a acciones frecuentes (`move opportunity`, `activate/deactivate rule`).
3. Agregar `loading.tsx`/`error.tsx` en rutas criticas (`dashboard`, `pipeline`, `rules`, `email`).
4. Implementar paginacion/filtering server-side en email logs.
5. Agregar correlation id FE-BE en requests para trazabilidad.

Expected Output:
UX mas fluida bajo carga, menor latencia percibida y mejor recuperacion ante errores por segmento.

Dependencies:
TASK-UI-ENT-01.

Acceptance Criteria:
- Query keys definidas por dominio y sin fetch duplicado observable en refresh simple.
- Acciones de pipeline y reglas actualizan UI sin refetch completo obligatorio.
- Rutas criticas tienen boundary visual de carga/error por segmento.
- Logs de email soportan paginacion server-side sin degradacion visible.
- Requests frontend incluyen identificador de correlacion.

Evidence (Expected):
- `frontend/services/*` + hooks de datos refactorizados.
- `frontend/app/**/loading.tsx` y `frontend/app/**/error.tsx`.
- `frontend/app/email/logs/page.tsx` con filtros/paginacion server-first.
- `frontend/services/apiClient.ts` con correlation id header.
- Medicion comparativa pre/post en `ia/05_progress.md`.

Evidence (Implemented):
- Capa de queries consolidada con React Query (`frontend/components/providers/QueryProvider.tsx`, `frontend/hooks/queries/*`).
- Optimistic updates aplicados a pipeline y reglas (`useMoveOpportunityMutation`, `useToggleRuleMutation`).
- Boundaries de carga/error por rutas criticas en `frontend/app/dashboard`, `frontend/app/pipeline`, `frontend/app/rules`, `frontend/app/email`.
- Escalado server-side en logs (`frontend/app/email/logs/page.tsx`, `frontend/services/email.service.ts`).
- Correlation id FE-BE en `frontend/services/apiClient.ts`.
- Evidencia de no regresion y resultados consolidados en `ia/05_progress.md`.

## TASK-UI-ENT-03: Sprint UI Enterprise Fase 2 (calidad y observabilidad)
**Estado:** ✅ Completado

Title: Endurecer calidad UI en CI con a11y, visual regression y contratos FE-BE

Context:
Actualmente la suite E2E cubre smoke funcional, pero falta control enterprise de accesibilidad, regresion visual y contratos de payload.

Steps:
1. Integrar pruebas automatizadas de accesibilidad por rutas criticas en CI.
2. Agregar visual regression snapshots para dashboard, pipeline, rules y email.
3. Agregar contract tests FE-BE para payloads de pipeline, rules y email.
4. Publicar dashboard de observabilidad UX (errores por pantalla, latencia endpoint UI, time-to-insight).
5. Configurar alertas de degradacion por p95/error-rate.

Expected Output:
Gate de calidad UI enterprise que previene regresiones funcionales, visuales y de accesibilidad.

Dependencies:
TASK-UI-ENT-02.

Acceptance Criteria:
- PR falla si rompe checks de a11y o visual regression en rutas criticas.
- Contratos FE-BE detectan cambios incompatibles de payload.
- Dashboard UX muestra metricas minimas (`time_to_insight`, `request_error`, `web_vital`).
- Alertas activas para degradacion de p95 y spikes de error.

Evidence (Expected):
- Workflows CI actualizados en `.github/workflows`.
- Nuevas suites en `frontend/tests` (a11y/visual/contract).
- Documentacion operativa en `docs/operations` para observabilidad UX.
- Registro de resultados en `ia/05_progress.md`.

Evidence (Implemented):
- Gate CI fullstack endurecido en `.github/workflows/quality-fullstack.yml` con checks post-smoke de a11y/contracts/visual.
- Suites dedicadas en `frontend/tests/e2e/accessibility.spec.ts`, `frontend/tests/e2e/contracts.spec.ts`, `frontend/tests/e2e/visual.spec.ts`.
- Flows E2E adaptados a confirm dialog accesible y hardening anti-flake en rutas criticas.
- Registro de resultados dirigido y suite completa en `ia/05_progress.md`.

## TASK-UI-ENT-04: Sprint UI Enterprise Fase 3 (escala funcional)
**Estado:** ✅ Completado

Title: Escalar UX operativa avanzada en Rules/Pipeline y gobernanza de componentes

Context:
Con la base estabilizada, se requiere aumentar productividad de equipos operativos y escalar el sistema de UI sin drift.

Steps:
1. Implementar rule builder guiado con simulador de fixtures y rollback visual.
2. Implementar pipeline advanced UX (bulk actions, vistas guardadas, flujo teclado).
3. Publicar catalogo oficial de componentes y patrones UI (Storybook o guia equivalente).
4. Formalizar DoD UI enterprise en guias de contribucion.
5. Definir backlog y SLA de deuda UI por severidad.

Expected Output:
UI enterprise escalable para operaciones de alto volumen con reglas y pipeline avanzados.

Dependencies:
TASK-UI-ENT-03.

Acceptance Criteria:
- Rules UI soporta crear/editar reglas complejas sin editar JSON manual.
- Pipeline UI soporta acciones masivas y configuracion de vistas por usuario.
- Existe catalogo oficial de componentes versionado y consumido por el equipo.
- DoD UI enterprise adoptado en proceso de PR/release.

Evidence (Expected):
- `frontend/components/rules/*` y `frontend/components/pipeline/*` ampliados.
- Documentacion de patrones en `docs/ui` o `frontend/app/admin/ui-guide` extendida.
- Actualizacion de `CONTRIBUTING.md`/estandares de ingenieria.
- Seguimiento de cierre en `ia/05_progress.md` y decisiones relevantes en `ia/06_decisions.md`.

Evidence (Implemented):
- `frontend/components/rules/RuleBuilderPanel.tsx` soporta carga de regla, edicion guiada multi-condicion/multi-accion, simulacion y rollback.
- `frontend/components/pipeline/KanbanBoard.tsx` consolida bulk move, vista guardada persistida y foco rapido por teclado.
- `frontend/app/admin/ui-guide/page.tsx` publica catalogo oficial UI v1.1 con reglas de adopcion y backlog/SLA de deuda.
- `CONTRIBUTING.md` y `docs/product/definition-of-done.md` formalizan DoD UI enterprise y gates obligatorios para PR/release.
- Validacion dirigida: `dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~RulesEngineEndpointTests.UpdateRule_ReplacesConditionsAndActions_WithoutServerError`, `npx playwright test tests/e2e/flows.spec.ts -g "rule builder edits existing rule with multiple conditions and actions"`, `npx playwright test tests/e2e/flows.spec.ts -g "pipeline bulk move persists saved view"`, `npm run build`.

## TASK-MVP-01: Implementar Lead Intake
**Estado:** ✅ Completado

Title: Crear el flujo base de intake de leads por API

Context:
Se necesita el punto de entrada machine-to-machine del sistema. Es la base para scoring, asignación, pipeline y automatización.

Steps:
1. Crear endpoint `POST /api/leads/intake`.
2. Definir request contract mínimo.
3. Validar y normalizar email, teléfono y fuente.
4. Persistir lead con logging y manejo de errores.
5. Emitir evento `lead.created`.

Expected Output:
Endpoint funcional que crea leads válidos y registra errores de intake.

Dependencies:
Ninguna.

Evidence:
- Solución backend creada en `backend/MindFlow.Backend.sln`.
- Endpoint implementado: `POST /api/leads/intake`.
- Request contract implementado en `backend/src/Api/Contracts/LeadIntakeRequest.cs`.
- Validación y normalización implementadas en `backend/src/Api/Application/Leads/LeadIntakeService.cs`.
- Persistencia implementada con EF Core SQLite (`LeadsDbContext`, `LeadRepository`).
- Emisión de evento `lead.created` implementada vía `LeadCreatedEventPublisher`.
- Pruebas de integración en `backend/tests/Api.Tests/UnitTest1.cs`.

## TASK-MVP-02: Modelar Contact y Company
**Estado:** ✅ Completado

Title: Crear entidades y relaciones comerciales base

Context:
El lead debe poder asociarse a un contacto y una empresa para soportar pipeline y propuestas futuras.

Steps:
1. Crear tablas o entidades `Contact` y `Company`.
2. Relacionarlas con `Lead`.
3. Definir reglas de duplicado.
4. Exponer CRUD mínimo para backend.

Expected Output:
Modelo de datos y endpoints base para contactos y empresas asociados a leads.

Dependencies:
TASK-MVP-01.

Evidence:
- Entidades implementadas: `backend/src/Api/Domain/Contacts/Contact.cs` y `backend/src/Api/Domain/Companies/Company.cs`.
- Relación con Lead implementada por `LeadId` + FK en `LeadsDbContext`.
- Reglas de duplicado implementadas:
	- Contact: conflicto por email o teléfono normalizado (`ContactService`).
	- Company: conflicto por nombre normalizado (`CompanyService`).
- CRUD mínimo expuesto en backend:
	- `POST/GET/PUT/DELETE /api/contacts`
	- `POST/GET/PUT/DELETE /api/companies`
- Persistencia EF Core agregada para `Contacts` y `Companies` con índices únicos.
- Pruebas de integración TDD agregadas en `backend/tests/Api.Tests/ContactCompanyEndpointTests.cs`.

## TASK-MVP-03: Construir Pipeline básico
**Estado:** ✅ Completado

Title: Habilitar oportunidades y movimiento por etapas

Context:
La UI principal del sistema es el pipeline Kanban, por lo que se necesita modelo, endpoints y vista inicial.

Steps:
1. Crear `PipelineStages` y `Opportunities`.
2. Implementar endpoint de cambio de etapa.
3. Persistir historial de cambios.
4. Crear UI Kanban básica.

Expected Output:
Pipeline funcional con etapas, movimiento y trazabilidad de cambios.

Dependencies:
TASK-MVP-01.

Evidence:
- Entidades de pipeline implementadas:
	- `backend/src/Api/Domain/Pipeline/PipelineStage.cs`
	- `backend/src/Api/Domain/Pipeline/Opportunity.cs`
	- `backend/src/Api/Domain/Pipeline/OpportunityStageHistory.cs`
- Catálogo de etapas por defecto implementado en `backend/src/Api/Domain/Pipeline/DefaultPipelineStages.cs`.
- Endpoints de pipeline implementados en `backend/src/Api/Controllers/PipelineController.cs`:
	- `GET /api/pipeline/stages`
	- `GET /api/pipeline/board`
	- `POST /api/pipeline/opportunities`
	- `PATCH /api/pipeline/opportunities/{opportunityId}/stage`
	- `GET /api/pipeline/opportunities/{opportunityId}/history`
- Historial append-only de cambios de etapa persistido en `OpportunityStageHistory`.
- Persistencia EF Core y repositorios agregados para `PipelineStages`, `Opportunities` y `OpportunityStageHistory`.
- UI Kanban básica inicial agregada en `backend/src/Api/wwwroot/index.html` (consumiendo API de pipeline).
- Pruebas de integración TDD agregadas en `backend/tests/Api.Tests/PipelineEndpointTests.cs`.

## TASK-MVP-04: Configurar Email automático inicial
**Estado:** ✅ Completado

Title: Disparar email al recibir un lead

Context:
El sistema debe reducir seguimiento manual desde el primer sprint.

Steps:
1. Integrar proveedor SMTP configurable.
2. Crear template base.
3. Suscribirse a `lead.created`.
4. Registrar éxito o fallo del envío.

Expected Output:
Email automático enviado tras intake válido, con logs de ejecución.

Dependencies:
TASK-MVP-01.

Evidence:
- Módulo de email implementado como first-class module siguiendo la estructura `Domain/Email`, `Application/Email`, `Infrastructure/Email`.
- Entidades de dominio implementadas: `backend/src/Api/Domain/Email/SmtpSettings.cs`, `backend/src/Api/Domain/Email/EmailTemplate.cs`, `backend/src/Api/Domain/Email/EmailLog.cs`.
- Interfaces de Application layer implementadas: `IEmailSender`, `ISmtpSettingsRepository`, `IEmailTemplateRepository`, `IEmailLogRepository`, `IEmailService`.
- `EmailService` implementado: resuelve config SMTP, template y envía email. Si no hay SMTP configurado → `Skipped`. Si falla el envío → `Failed`. Siempre registra log en `EmailLogs`.
- `SmtpEmailSender` implementado usando `System.Net.Mail.SmtpClient` con timeout de 15s.
- `LeadIntakeService` actualizado: inyecta `IEmailService` y llama `SendLeadWelcomeAsync` tras publicar evento `lead.created`.
- Repositorios EF Core implementados: `SmtpSettingsRepository` (upsert único activo), `EmailTemplateRepository`, `EmailLogRepository`.
- Contratos implementados: `SmtpSettingsRequest`, `SmtpSettingsResponse` (sin exponer password), `EmailLogResponse`.
- `EmailController` expone:
	- `PUT /api/email/smtp-settings` — upsert configuración SMTP.
	- `GET /api/email/smtp-settings` — obtener config activa (password excluido).
	- `GET /api/email/logs` — listado de logs ordenado por fecha.
- `LeadsDbContext` actualizado con `DbSet<SmtpSettings>`, `DbSet<EmailTemplate>`, `DbSet<EmailLog>` y mappings EF Core.
- Bootstrap SQL en `Program.cs`: crea tablas `SmtpSettings`, `EmailTemplates`, `EmailLogs` + seed del template por defecto `lead.welcome`.
- Pruebas de integración TDD (`EmailEndpointTests.cs`) con `EmailTestFactory` para aislamiento por DB única:
	- `GetSmtpSettings_WhenNotConfigured_Returns404`
	- `PutSmtpSettings_WithValidPayload_Returns200AndGetReturnsSettings`
	- `PutSmtpSettings_WithInvalidPort_Returns400`
	- `GetEmailLogs_Returns200WithList`
	- `IntakeLead_WithNoSmtpConfigured_Returns201AndLogsSkipped`
- Suite de 14 tests: 14/14 pasando en Release.

## TASK-MVP-05: Implementar Follow-up automático
**Estado:** ✅ Completado

Title: Programar y ejecutar seguimiento diferido

Context:
Una vez existe email inicial, el siguiente paso es automatizar seguimientos sin respuesta.

Steps:
1. Integrar Hangfire o equivalente.
2. Crear job diferido a 48h.
3. Cancelar job cuando el lead responda.
4. Guardar logs del job.

Expected Output:
Job de seguimiento funcionando con cancelación por cambio de estado.

Dependencies:
TASK-MVP-04.

Evidence:
- Dominio: `backend/src/Api/Domain/FollowUp/FollowUpJob.cs` — agregado con ciclo de vida Scheduled → Sent | Failed | Cancelled.
- Dominio: `backend/src/Api/Domain/FollowUp/FollowUpJobStatus.cs` — constantes de estado.
- Interfaces: `backend/src/Api/Application/FollowUp/IFollowUpJobRepository.cs` y `IFollowUpService.cs`.
- Servicio: `backend/src/Api/Application/FollowUp/FollowUpService.cs` — programa job a +48h, cancela por lead o por id, ejecuta jobs vencidos vía `IEmailService.SendLeadFollowUpAsync`.
- Repositorio: `backend/src/Api/Infrastructure/FollowUp/FollowUpJobRepository.cs` — EF Core sobre `LeadsDbContext.FollowUpJobs`.
- Contratos: `backend/src/Api/Contracts/FollowUpJobResponse.cs` y `CancelFollowUpRequest.cs`.
- Controller: `backend/src/Api/Controllers/FollowUpController.cs` — endpoints: GET /api/followup/jobs, GET /api/followup/leads/{id}/jobs, POST /api/followup/leads/{id}/cancel, POST /api/followup/jobs/{id}/cancel.
- Integración con intake: `LeadIntakeService` llama `IFollowUpService.ScheduleAsync` después del email de bienvenida.
- Email service: `IEmailService.SendLeadFollowUpAsync` añadido a `EmailService` con template `lead.followup`.
- Bootstrap: tabla `FollowUpJobs` y seed de template `lead.followup` en `Program.cs`.
- Tests: 5 tests de integración en `backend/tests/Api.Tests/FollowUpEndpointTests.cs` — todos GREEN.
- Build Release: 0 errores, 0 advertencias relevantes.
- Tests totales: 19/19 GREEN (14 previos + 5 nuevos follow-up).

## TASK-MVP-06: Implementar asignación automática
**Estado:** ✅ Completado

Title: Asignar leads sin intervención manual

Context:
La operación comercial requiere asignación automática para evitar cuellos de botella.

Steps:
1. Crear tabla o catálogo de usuarios.
2. Implementar round robin base.
3. Registrar resultado de asignación.
4. Dejar preparado soporte para reglas avanzadas.

Expected Output:
Leads asignados automáticamente con registro auditable.

Dependencies:
TASK-MVP-01.

Evidence:
- Dominio de asignación implementado:
	- `backend/src/Api/Domain/Assignment/AssignmentUser.cs`
	- `backend/src/Api/Domain/Assignment/LeadAssignment.cs`
- Contratos API implementados:
	- `backend/src/Api/Contracts/AssignmentUserCreateRequest.cs`
	- `backend/src/Api/Contracts/AssignmentUserResponse.cs`
	- `backend/src/Api/Contracts/LeadAssignmentResponse.cs`
- Capa de aplicación implementada con round-robin:
	- `backend/src/Api/Application/Assignment/IAssignmentUserRepository.cs`
	- `backend/src/Api/Application/Assignment/ILeadAssignmentRepository.cs`
	- `backend/src/Api/Application/Assignment/ILeadAssignmentService.cs`
	- `backend/src/Api/Application/Assignment/LeadAssignmentService.cs`
	- `backend/src/Api/Application/Assignment/AssignmentConflictException.cs`
- Persistencia EF Core implementada:
	- `backend/src/Api/Infrastructure/Assignment/AssignmentUserRepository.cs`
	- `backend/src/Api/Infrastructure/Assignment/LeadAssignmentRepository.cs`
	- `LeadsDbContext` actualizado con `DbSet<AssignmentUser>` y `DbSet<LeadAssignment>` + mappings e índices.
- API de asignación implementada:
	- `backend/src/Api/Controllers/AssignmentsController.cs`
	- Endpoints:
		- `POST /api/assignments/users`
		- `GET /api/assignments/users`
		- `GET /api/assignments`
		- `GET /api/assignments/leads/{leadId}`
- Integración automática en intake:
	- `backend/src/Api/Application/Leads/LeadIntakeService.cs` ahora llama `ILeadAssignmentService.AssignLeadAsync` tras persistir lead.
- Bootstrap SQL y DI actualizados en `backend/src/Api/Program.cs`:
	- Tablas `AssignmentUsers` y `LeadAssignments`.
	- Índices de auditoría y unicidad.
	- Registro de dependencias en contenedor DI.
- Pruebas TDD agregadas en `backend/tests/Api.Tests/AssignmentEndpointTests.cs` (5 pruebas).
- Validación técnica final:
	- `dotnet build -c Release` → 0 errores, 0 advertencias.
	- `dotnet test -c Release` → 24/24 tests GREEN.

## TASK-MVP-07: Implementar scoring básico
**Estado:** ✅ Completado

Title: Calcular prioridad inicial de leads

Context:
El score permite ordenar atención comercial y alimentar reglas posteriores.

Steps:
1. Crear modelo `ScoreRule`.
2. Implementar cálculo inicial por eventos.
3. Persistir score en lead.
4. Definir thresholds de prioridad.

Expected Output:
Score básico persistido y visible para ordenar atención comercial.

Dependencies:
TASK-MVP-01.

Evidence:
- Modelo de reglas de scoring implementado:
	- `backend/src/Api/Domain/Scoring/ScoreRule.cs`
	- `backend/src/Api/Domain/Scoring/LeadScorePriority.cs` (thresholds `Medium=50`, `High=80`).
- Cálculo inicial por evento de intake (`lead.created` flow) implementado en:
	- `backend/src/Api/Application/Scoring/ILeadScoringService.cs`
	- `backend/src/Api/Application/Scoring/LeadScoringService.cs`
- Persistencia de score en lead:
	- `backend/src/Api/Domain/Leads/Lead.cs` ahora incluye `Score` y `Priority` + método `SetScore`.
	- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` actualizado con mappings e índices (`IX_Leads_Score`, `IX_Leads_Priority`).
	- `backend/src/Api/Application/Common/Interfaces/ILeadRepository.cs` y `LeadRepository.cs` extendidos para carga/guardado de lead scored.
- Integración en flujo principal:
	- `backend/src/Api/Application/Leads/LeadIntakeService.cs` ahora invoca `ILeadScoringService.ScoreLeadAsync` tras persistir/publicar evento.
	- `backend/src/Api/Contracts/LeadIntakeResponse.cs` expone `Score` y `Priority`.
- API de consulta de scoring:
	- `backend/src/Api/Controllers/ScoringController.cs`
	- `GET /api/scoring/rules`
	- `GET /api/scoring/leads/{leadId}`
	- Contratos: `backend/src/Api/Contracts/ScoreRuleResponse.cs`, `LeadScoreResponse.cs`.
- DI y bootstrap actualizados en `backend/src/Api/Program.cs`:
	- Registro de `ILeadScoringService`.
	- Compatibilidad hacia atrás para `Leads` con columnas `Score` y `Priority` (`ALTER TABLE` idempotente + índices).
- Pruebas TDD agregadas:
	- `backend/tests/Api.Tests/ScoringEndpointTests.cs` (4 pruebas RED→GREEN).
- Validacion técnica final:
	- `dotnet build -c Release` → 0 errores, 0 advertencias.
	- `dotnet test -c Release` → 28/28 tests GREEN.

## TASK-MVP-08: Construir Rules Engine básico
**Estado:** ✅ Completado

Title: Hacer configurable la automatización principal del sistema

Context:
El sistema debe poder programarse sin código mediante reglas Trigger -> Condition -> Action.

Steps:
1. Crear entidades `Rule`, `RuleCondition` y `RuleAction`.
2. Implementar listener de eventos.
3. Evaluar condiciones y despachar acciones.
4. Exponer CRUD y activación/desactivación en UI.

Expected Output:
Motor básico de reglas ejecutando acciones sobre eventos relevantes.

Dependencies:
TASK-MVP-04, TASK-MVP-06, TASK-MVP-07.

Evidence:
- Dominio Rules implementado:
	- `backend/src/Api/Domain/Rules/Rule.cs`
	- `backend/src/Api/Domain/Rules/RuleCondition.cs`
	- `backend/src/Api/Domain/Rules/RuleAction.cs`
- Contratos API Rules implementados:
	- `backend/src/Api/Contracts/RuleCreateRequest.cs`
	- `backend/src/Api/Contracts/RuleUpdateRequest.cs`
	- `backend/src/Api/Contracts/RuleConditionRequest.cs`
	- `backend/src/Api/Contracts/RuleActionRequest.cs`
	- `backend/src/Api/Contracts/RuleResponse.cs`
	- `backend/src/Api/Contracts/RuleConditionResponse.cs`
	- `backend/src/Api/Contracts/RuleActionResponse.cs`
- Capa de aplicación Rules Engine:
	- `backend/src/Api/Application/RulesEngine/IRuleRepository.cs`
	- `backend/src/Api/Application/RulesEngine/IRuleService.cs`
	- `backend/src/Api/Application/RulesEngine/IRuleEventListener.cs`
	- `backend/src/Api/Application/RulesEngine/RuleEngineService.cs`
- Listener de eventos implementado:
	- `LeadIntakeService` invoca `IRuleEventListener.OnLeadCreatedAsync` tras scoring base.
- Evaluación y despacho de acciones implementados:
	- Trigger soportado: `lead.created`.
	- Condiciones soportadas: `source`, `priority`, `score`, `has_email`, `has_phone` con operadores `eq`, `neq`, `gte`, `lte`, `contains`.
	- Acciones soportadas: `add_score`, `set_priority`.
- Persistencia EF Core implementada:
	- `backend/src/Api/Infrastructure/RulesEngine/RuleRepository.cs`
	- `LeadsDbContext` actualizado con `DbSet<Rule>`, `DbSet<RuleCondition>`, `DbSet<RuleAction>` + mappings y relaciones cascade.
- API CRUD + activación/desactivación:
	- `backend/src/Api/Controllers/RulesController.cs`
	- Endpoints:
		- `POST /api/rules`
		- `GET /api/rules`
		- `GET /api/rules/{id}`
		- `PUT /api/rules/{id}`
		- `DELETE /api/rules/{id}`
		- `POST /api/rules/{id}/activate`
		- `POST /api/rules/{id}/deactivate`
- DI y bootstrap SQL actualizados en `backend/src/Api/Program.cs`:
	- Registro de `IRuleRepository`, `IRuleService`, `IRuleEventListener`.
	- Tablas `Rules`, `RuleConditions`, `RuleActions` + índices.
- Pruebas TDD agregadas:
	- `backend/tests/Api.Tests/RulesEngineEndpointTests.cs` (5 pruebas RED→GREEN).
- Validacion técnica final:
	- `dotnet build -c Release` → 0 errores, 0 advertencias.
	- `dotnet test -c Release` → 33/33 tests GREEN.

## TASK-MVP-09: Construir Dashboard básico
**Estado:** ✅ Completado

Title: Exponer visibilidad mínima de operación comercial

Context:
El negocio necesita ver volumen y conversión desde el MVP intermedio.

Steps:
1. Calcular leads por día.
2. Calcular conversión.
3. Calcular valor del pipeline.
4. Construir pantalla inicial de dashboard.

Expected Output:
Dashboard básico con tres métricas operativas visibles.

Dependencies:
TASK-MVP-03, TASK-MVP-07.

Evidence:
- Contratos dashboard implementados:
	- `backend/src/Api/Contracts/DashboardOverviewResponse.cs`
	- `backend/src/Api/Contracts/LeadsPerDayPointResponse.cs`
- Capa de aplicación dashboard implementada:
	- `backend/src/Api/Application/Dashboard/IDashboardService.cs`
	- `backend/src/Api/Application/Dashboard/DashboardService.cs`
	- Métricas calculadas: `TotalLeads`, `ConversionRate`, `PipelineValue`, serie `LeadsPerDay` (ventana configurable por `days`).
- Repositorio de leads extendido para analítica:
	- `backend/src/Api/Application/Common/Interfaces/ILeadRepository.cs` (método `ListAsync`).
	- `backend/src/Api/Infrastructure/Persistence/LeadRepository.cs` (implementación `ListAsync`).
- Endpoint dashboard implementado:
	- `backend/src/Api/Controllers/DashboardController.cs`
	- `GET /api/dashboard/overview?days=7`.
- Pantalla inicial dashboard implementada:
	- `backend/src/Api/wwwroot/dashboard.html`.
	- UI renderiza 3 métricas operativas y barras de leads por día.
- DI actualizado en `backend/src/Api/Program.cs`:
	- Registro de `IDashboardService` con `DashboardService`.
- Pruebas TDD agregadas:
	- `backend/tests/Api.Tests/DashboardEndpointTests.cs` (4 pruebas RED→GREEN).
	- Cobertura: endpoint sin datos, endpoint con datos, leads por día, serving de `dashboard.html`.
- Validacion técnica final:
	- `dotnet build -c Release` → 0 errores, 0 advertencias.
	- `dotnet test -c Release` → 37/37 tests GREEN.

## TASK-FULL-10: Habilitar multi-tenant y roles
**Estado:** ✅ Completada

Title: Convertir el MVP en base SaaS aislada por tenant

Context:
La versión full requiere separación segura de datos y permisos por rol.

Steps:
1. Agregar `TenantId` a las tablas requeridas.
2. Implementar middleware de contexto.
3. Aplicar seguridad por tenant.
4. Definir roles `Admin`, `Sales`, `Viewer`.

Expected Output:
Base multi-tenant con permisos y aislamiento mínimo operativo.

Dependencies:
TASK-MVP-01 a TASK-MVP-09.

Entregables implementados:
- Multi-tenant en backend con `TenantId` por tabla de negocio y filtros globales por tenant en EF Core.
- `TenantMiddleware` para resolver `X-Tenant-Id` y `X-User-Role` por request.
- `RoleAuthorizationMiddleware` para bloquear mutaciones (`POST/PUT/PATCH/DELETE`) bajo `/api/*` cuando el rol es `Viewer`.
- Wiring de DI/pipeline en `Program.cs` para contexto tenant+rol y bootstrap idempotente de columnas/índices `TenantId`.
- Pruebas de integración nuevas en `backend/tests/Api.Tests/MultiTenantRoleEndpointTests.cs`.

Evidencia de validacion:
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --no-build` → 40/40 tests GREEN.

## TASK-FULL-11: Automatizar propuestas
**Estado:** ✅ Completada

Title: Acelerar cierre mediante propuestas automáticas

Context:
La capa comercial avanzada necesita templates, PDF y recordatorios automáticos.

Steps:
1. Crear templates de propuesta.
2. Generar PDF.
3. Enviar por email.
4. Implementar recordatorio y tracking.

Expected Output:
Propuesta generada, enviada y seguida automáticamente.

Dependencies:
TASK-MVP-04, TASK-MVP-08.

Entregables implementados:
- Módulo de propuestas agregado en backend (`Domain/Proposals`, `Application/Proposals`, `Infrastructure/Proposals`).
- API de propuestas implementada en `backend/src/Api/Controllers/ProposalsController.cs`:
	- `POST /api/proposals` (genera propuesta, PDF, envío inicial y agenda reminder).
	- `GET /api/proposals`.
	- `GET /api/proposals/{proposalId}`.
	- `GET /api/proposals/{proposalId}/pdf`.
	- `POST /api/proposals/{proposalId}/reminders/force-due`.
	- `POST /api/proposals/reminders/execute-due`.
	- `GET /api/proposals/track/{trackingToken}`.
- Generación PDF implementada con `SimpleProposalPdfGenerator` (salida `application/pdf`).
- Envío de propuesta y recordatorio integrados en `EmailService` con templates:
	- `proposal.standard`.
	- `proposal.reminder`.
- Tracking implementado con token por propuesta + `ViewCount` y `LastViewedAtUtc`.
- Persistencia agregada para `Proposals` y `ProposalReminderJobs` en `LeadsDbContext` + bootstrap idempotente en `Program.cs`.
- Pruebas de integración nuevas en `backend/tests/Api.Tests/ProposalAutomationEndpointTests.cs`.

Evidencia de validacion:
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --filter "FullyQualifiedName~ProposalAutomationEndpointTests"` → 4/4 tests GREEN.
- `dotnet test -c Release --no-build` → 44/44 tests GREEN.

## TASK-FULL-12: Automatizar onboarding
**Estado:** ✅ Completada

Title: Ejecutar post-venta sin operación manual

Context:
Al ganar una oportunidad, el sistema debe crear cliente y disparar onboarding.

Steps:
1. Crear entidad `Customer`.
2. Convertir `Won -> Customer`.
3. Crear tareas de onboarding.
4. Enviar bienvenida y activar tracking.

Expected Output:
Onboarding automático disparado desde una oportunidad ganada.

Dependencies:
TASK-FULL-11 o estado `Won` operativo en pipeline.

Entregables implementados:
- Módulo de onboarding agregado en backend (`Domain/Onboarding`, `Application/Onboarding`, `Infrastructure/Onboarding`).
- Entidad `Customer` implementada con tracking token y activaciones (`TrackingActivations`, `LastTrackingActivatedAtUtc`).
- Conversión automática `Won -> Customer` integrada en `PipelineService` al mover oportunidades a etapa `won`.
- Creación automática de tareas de onboarding por cliente:
	- `kickoff-call`.
	- `requirements-checklist`.
	- `workspace-setup`.
- API de onboarding implementada en `backend/src/Api/Controllers/OnboardingController.cs`:
	- `GET /api/onboarding/customers`.
	- `GET /api/onboarding/customers/by-lead/{leadId}`.
	- `GET /api/onboarding/customers/{customerId}/tasks`.
	- `GET /api/onboarding/track/{trackingToken}`.
- Envío de bienvenida onboarding integrado en `EmailService` con template `customer.welcome`.
- Persistencia agregada para `Customers` y `OnboardingTasks` en `LeadsDbContext` + bootstrap idempotente en `Program.cs`.
- Pruebas de integración nuevas en `backend/tests/Api.Tests/OnboardingAutomationEndpointTests.cs`.

Evidencia de validacion:
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --filter "FullyQualifiedName~OnboardingAutomationEndpointTests"` → 4/4 tests GREEN.
- `dotnet test -c Release --no-build` → 48/48 tests GREEN.

## TASK-FULL-13: Definir analytics avanzado (KPIs + contratos)
**Estado:** ✅ Completada

Title: Diseñar capa analítica avanzada para operación comercial SaaS

Context:
Con onboarding automatizado completado, el siguiente paso es formalizar métricas avanzadas y contratos API para observabilidad de negocio y performance comercial.

Steps:
1. Definir catálogo de KPIs avanzados (funnel, revenue, velocity, SLA operativo, activación onboarding).
2. Diseñar contratos de respuesta y filtros (`dateRange`, `groupBy`, `stage`, `source`, `tenant`).
3. Priorizar endpoints backend analíticos para implementación incremental.
4. Documentar definiciones de cálculo y criterios de aceptación por KPI.

Expected Output:
Especificación aprobada de analytics avanzado con KPIs, fórmulas, contratos y backlog técnico priorizado.

Dependencies:
TASK-MVP-09, TASK-FULL-10, TASK-FULL-11, TASK-FULL-12.

Entregables implementados:
- Catálogo de KPIs avanzados definido para:
	- Funnel.
	- Revenue.
	- Velocity.
	- SLA operativo.
	- Activación onboarding.
- Fórmulas y criterios base documentados en arquitectura (`ia/02_architecture.md`).
- Filtros y agrupación estándar definidos (`StartDateUtc`, `EndDateUtc`, `GroupBy`, `Stage`, `Source`, `Tenant`).
- Contratos backend de analytics avanzado agregados en `backend/src/Api/Contracts/Analytics/`:
	- `AnalyticsAdvancedQuery`.
	- `AnalyticsAdvancedOverviewResponse`.
	- `FunnelKpiResponse`.
	- `RevenueKpiResponse`.
	- `VelocityKpiResponse`.
	- `SlaKpiResponse`.
	- `OnboardingActivationKpiResponse`.
	- `AnalyticsBacklogItemResponse`.
- Endpoints backend priorizados para implementación incremental de TASK-FULL-14.

Evidencia de validacion:
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --no-build` → 48/48 tests GREEN.

## TASK-FULL-14: Implementar analytics avanzado backend (ciclo enterprise)
**Estado:** ✅ Completada

Title: Construir endpoints analíticos avanzados con validación enterprise end-to-end

Context:
Con la especificación de KPIs definida, se requiere implementar servicios y endpoints analíticos con disciplina TDD y gates de calidad Release para evitar regresiones.

Steps:
1. Crear todo list detallado de implementación por KPI/endpoint prioritario.
2. Ejecutar TDD completo (RED → GREEN → REFACTOR) para cada bloque analítico.
3. Implementar servicios, repositorios/queries y controladores de analytics avanzado.
4. Validar compilación y suite completa en Release sin errores ni advertencias.
5. Actualizar documentación (`ia/04_tasks.md`, `ia/05_progress.md`, `ia/06_decisions.md`) con evidencia.

Expected Output:
Módulo de analytics avanzado operativo en backend, cubierto por pruebas, estable en Release y documentado.

Dependencies:
TASK-FULL-13.

Entregables implementados:
- Módulo de analytics avanzado implementado en backend:
	- `backend/src/Api/Application/AnalyticsAdvanced/IAnalyticsAdvancedService.cs`.
	- `backend/src/Api/Application/AnalyticsAdvanced/AnalyticsAdvancedService.cs`.
	- `backend/src/Api/Application/AnalyticsAdvanced/IAnalyticsAdvancedDataRepository.cs`.
	- `backend/src/Api/Application/AnalyticsAdvanced/AnalyticsAdvancedDataSnapshot.cs`.
	- `backend/src/Api/Infrastructure/AnalyticsAdvanced/AnalyticsAdvancedDataRepository.cs`.
	- `backend/src/Api/Controllers/AnalyticsAdvancedController.cs`.
- Endpoints analíticos implementados:
	- `GET /api/analytics/advanced/overview`.
	- `GET /api/analytics/advanced/funnel`.
	- `GET /api/analytics/advanced/revenue`.
	- `GET /api/analytics/advanced/velocity`.
	- `GET /api/analytics/advanced/sla`.
	- `GET /api/analytics/advanced/onboarding-activation`.
- DI actualizado en `backend/src/Api/Program.cs` para registrar servicio y repositorio de analytics avanzado.
- Suite TDD de integración agregada en `backend/tests/Api.Tests/AnalyticsAdvancedEndpointTests.cs`.

Evidencia de validacion:
- `dotnet test -c Release --filter "FullyQualifiedName~AnalyticsAdvancedEndpointTests"` → 4/4 tests GREEN.
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --no-build` → 52/52 tests GREEN.

## TASK-FULL-15: Integrar analytics avanzado en frontend + hardening de performance
**Estado:** ✅ Completada

Title: Consumir KPIs avanzados en UI y asegurar performance para escala SaaS

Context:
Con el backend analítico avanzado operativo, el siguiente paso es exponer métricas en frontend y fortalecer consultas para escenarios de mayor volumen.

Steps:
1. Diseñar vista analytics avanzada en frontend (overview + tabs por KPI).
2. Implementar capa de servicios frontend para consumo de `/api/analytics/advanced/*` con filtros estándar.
3. Agregar estados de carga/error/vacío y validación de filtros (`dateRange`, `groupBy`, `stage`, `source`).
4. Ejecutar optimización backend orientada a performance (índices adicionales y/o query tuning) sin romper contratos API.
5. Cubrir con pruebas de integración (backend) y pruebas funcionales/UI (frontend) para flujos críticos.
6. Validar release end-to-end y actualizar documentación (`ia/04_tasks.md`, `ia/05_progress.md`, `ia/06_decisions.md`).

Expected Output:
Dashboard analítico avanzado funcional en frontend, conectado a endpoints productivos, con rendimiento estable y cobertura de pruebas enterprise.

Dependencies:
TASK-FULL-14.

Entregables implementados:
- Frontend analítico avanzado agregado como artefacto operativo en:
	- `backend/src/Api/wwwroot/analytics-advanced.html`.
- Vista analytics avanzada implementada con:
	- Tabs por KPI (`overview`, `funnel`, `revenue`, `velocity`, `sla`, `onboarding-activation`).
	- Filtros estándar (`startDateUtc`, `endDateUtc`, `groupBy`, `stage`, `source`).
	- Estados `loading`, `error`, `empty` y navegación a dashboard/pipeline.
	- Service layer frontend (`analyticsService`) para consumo de `/api/analytics/advanced/*`.
- Hardening backend de performance aplicado en:
	- `backend/src/Api/Infrastructure/AnalyticsAdvanced/AnalyticsAdvancedDataRepository.cs` con `AsNoTracking` en todas las consultas analíticas.
	- `backend/src/Api/Program.cs` con índices adicionales orientados a analytics:
		- `IX_Opportunities_LeadId`, `IX_Opportunities_CreatedAtUtc`.
		- `IX_OpportunityStageHistory_ToStageId`, `IX_OpportunityStageHistory_ChangedAtUtc`.
		- `IX_LeadAssignments_LeadId_AssignedAtUtc`.
		- `IX_Customers_CreatedAtUtc`.
- Validacion de filtros en API agregada en `backend/src/Api/Controllers/AnalyticsAdvancedController.cs`:
	- `groupBy` permitido: `day|week|month`.
	- Rango de fechas válido y ventana máxima de 366 días.
- Pruebas TDD nuevas para frontend/API de analytics avanzado:
	- `backend/tests/Api.Tests/AnalyticsAdvancedFrontendEndpointTests.cs`.

Evidencia de validacion:
- `dotnet test -c Release --filter "FullyQualifiedName~AnalyticsAdvancedFrontendEndpointTests"` → 3/3 tests GREEN.
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --no-build` → 55/55 tests GREEN.

## TASK-FULL-16: Observabilidad avanzada de analytics (telemetría + endpoint operativo)
**Estado:** ✅ Completada

Title: Fortalecer visibilidad operacional de analytics avanzado para operación SaaS

Context:
Con analytics avanzado ya en producción funcional, se requiere telemetría explícita por endpoint para monitoreo y diagnóstico operativo sin depender de inspección manual.

Steps:
1. Diseñar contrato de métricas operativas por endpoint analítico.
2. Implementar servicio de observabilidad in-memory para request count, éxito/error y latencia promedio.
3. Integrar captura de telemetría en endpoints de analytics avanzado.
4. Exponer endpoint operativo de métricas para diagnóstico (`/api/analytics/advanced/metrics`).
5. Cubrir con pruebas de integración (RED→GREEN) para endpoint y acumulación de métricas.
6. Validar build/test en Release y actualizar documentación de cierre.

Expected Output:
Módulo analytics avanzado con observabilidad operativa integrada, métricas consultables por API y cobertura de pruebas enterprise.

Dependencies:
TASK-FULL-15.

Entregables implementados:
- Contratos de observabilidad analytics agregados en:
	- `backend/src/Api/Application/AnalyticsAdvanced/AnalyticsObservabilitySnapshot.cs`.
	- `backend/src/Api/Application/AnalyticsAdvanced/IAnalyticsObservabilityService.cs`.
- Servicio in-memory de observabilidad implementado en:
	- `backend/src/Api/Infrastructure/AnalyticsAdvanced/InMemoryAnalyticsObservabilityService.cs`.
	- Captura: `RequestCount`, `SuccessCount`, `ErrorCount`, `AverageLatencyMs` por endpoint.
- Integración de telemetría en endpoints analíticos en:
	- `backend/src/Api/Controllers/AnalyticsAdvancedController.cs`.
	- Tracking de éxitos y errores (incluyendo errores por validación de filtros).
- Endpoint operativo agregado:
	- `GET /api/analytics/advanced/metrics`.
- DI actualizado en `backend/src/Api/Program.cs`:
	- Registro singleton de `IAnalyticsObservabilityService`.
- Pruebas TDD nuevas:
	- `backend/tests/Api.Tests/AnalyticsAdvancedObservabilityEndpointTests.cs`.

Evidencia de validacion:
- `dotnet test -c Release --filter "FullyQualifiedName~AnalyticsAdvancedObservabilityEndpointTests"` → 3/3 tests GREEN.
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --no-build` → 58/58 tests GREEN.

Siguientes pasos recomendados:
1. `TASK-FULL-17` — Persistir métricas analíticas (histórico por ventana temporal) para análisis de tendencias y auditoría operacional.
2. `TASK-FULL-18` — Definir alertas automáticas sobre degradación (`ErrorRate`, `AverageLatencyMs`) y umbrales por endpoint.
3. `TASK-FULL-19` — Exponer dashboard operativo de observabilidad (series temporales y heatmaps por tenant/stage/source).

## TASK-FULL-17: Persistencia histórica de métricas de observabilidad
**Estado:** ✅ Completada

Title: Persistir snapshots de telemetría analítica para tendencias y auditoría operacional

Context:
El módulo de observabilidad actual mantiene métricas en memoria (ConcurrentDictionary), lo que implica pérdida total de datos al reiniciar el proceso. Para operación SaaS continua, se requiere persistir snapshots periódicos en base de datos, permitir consulta histórica por ventana temporal y habilitar análisis de tendencias sobre ErrorRate y latencia por endpoint.

Steps:
1. Crear entidad de dominio `ObservabilitySnapshot` con campos: `EndpointName`, `RequestCount`, `SuccessCount`, `ErrorCount`, `AverageLatencyMs`, `RecordedAtUtc`, `TenantId`.
2. Definir contrato de respuesta `ObservabilityHistoryResponse` y query de filtro por ventana temporal (`StartUtc`, `EndUtc`, `EndpointName`).
3. Implementar `IObservabilitySnapshotRepository` con operación de inserción y consulta por rango temporal.
4. Crear `ObservabilityPersistenceService` que tome el snapshot en memoria actual vía `IAnalyticsObservabilityService.GetSnapshot()` y persista cada entrada en la tabla.
5. Integrar llamada a persistencia como job programado (Hangfire o background service) cada 5 minutos.
6. Exponer endpoint `GET /api/analytics/advanced/metrics/history` con filtros de ventana y nombre de endpoint.
7. Cubrir con pruebas TDD (RED→GREEN): escritura de snapshot, consulta histórica, job de persistencia.
8. Validar build/test Release y actualizar documentación.

Expected Output:
Snapshots de métricas de observabilidad persistidos automáticamente en BD, consultables por API con filtros temporales, con cobertura de pruebas enterprise y sin regresiones en suite existente.

Dependencies:
TASK-FULL-16.

Entregables implementados:
- Entidad de dominio implementada en:
	- `backend/src/Api/Domain/Observability/ObservabilityMetricRecord.cs`.
- Contrato de respuesta implementado en:
	- `backend/src/Api/Contracts/Analytics/ObservabilityHistoryResponse.cs` (`ObservabilityHistoryResponse`, `ObservabilityMetricRecordResponse`).
- Interfaces y servicios de Application implementados en:
	- `backend/src/Api/Application/Observability/IObservabilitySnapshotRepository.cs`.
	- `backend/src/Api/Application/Observability/IObservabilityPersistenceService.cs`.
	- `backend/src/Api/Application/Observability/ObservabilityPersistenceService.cs`.
- Repositorio EF Core implementado en:
	- `backend/src/Api/Infrastructure/Observability/ObservabilitySnapshotRepository.cs`.
	- Soporta filtros por ventana temporal (`startUtc`, `endUtc`) y por nombre de endpoint. `AsNoTracking` en lecturas. Top 1000 registros.
- Background service implementado en:
	- `backend/src/Api/Infrastructure/Observability/ObservabilityPersistenceBackgroundService.cs`.
	- Persiste snapshot cada 5 minutos vía `IServiceScopeFactory` para correcta gestión de ciclo de vida.
- Endpoints nuevos en `backend/src/Api/Controllers/AnalyticsAdvancedController.cs`:
	- `GET /api/analytics/advanced/metrics/history` — consulta histórica con filtros `startUtc`, `endUtc`, `endpointName`.
	- `POST /api/analytics/advanced/metrics/history/snapshot` — captura y persiste snapshot inmediato (utilidad operativa + testable).
- `LeadsDbContext` actualizado con `DbSet<ObservabilityMetricRecord>` y mapping EF Core sin query filter de tenant (tabla de sistema).
- Bootstrap SQL en `Program.cs`: tabla `ObservabilityMetricRecords` + índices `IX_ObservabilityMetricRecords_RecordedAtUtc` e `IX_ObservabilityMetricRecords_EndpointName`.
- DI registrado en `Program.cs`: `IObservabilitySnapshotRepository` (scoped), `IObservabilityPersistenceService` (scoped), `ObservabilityPersistenceBackgroundService` (hosted service).
- Pruebas TDD nuevas en:
	- `backend/tests/Api.Tests/ObservabilityHistoryEndpointTests.cs` (4 pruebas RED→GREEN).
	- Cobertura: lista vacía inicial, flush persiste y aparece en historial, filtro por endpoint, filtro por rango de fechas.

Evidencia de validacion:
- `dotnet test -c Release --filter "FullyQualifiedName~ObservabilityHistoryEndpointTests"` → 4/4 tests GREEN.
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --no-build` → 62/62 tests GREEN.

## TASK-FULL-18: Alertas automáticas por degradación de KPIs analytics
**Estado:** ✅ Completada

Title: Detectar y notificar degradación de métricas operativas analíticas en tiempo cuasi-real

Context:
Con telemetría persistida, el siguiente nivel operativo es definir umbrales por endpoint y disparar alertas automáticas cuando `ErrorRate` supere el límite configurado o `AverageLatencyMs` exceda el umbral de SLA. Las alertas deben registrarse en BD y notificarse por email a destinatarios configurados por tenant.

Steps:
1. Crear entidad `AlertThreshold` con campos: `EndpointName`, `MaxErrorRatePercent`, `MaxAverageLatencyMs`, `TenantId`, `IsActive`.
2. Crear entidad `AlertEvent` con campos: `ThresholdId`, `EndpointName`, `MetricName`, `ObservedValue`, `ThresholdValue`, `TriggeredAtUtc`, `TenantId`, `NotificationSent`.
3. Definir contratos API: `AlertThresholdCreateRequest`, `AlertThresholdResponse`, `AlertEventResponse`.
4. Implementar `IAlertThresholdRepository` e `IAlertEventRepository` con persistencia EF Core.
5. Implementar `AlertEvaluationService` que compare el snapshot actual contra todos los umbrales activos y genere `AlertEvent` cuando se exceda algún umbral.
6. Integrar evaluación de alertas en el job de persistencia de métricas (TASK-FULL-17) o como job independiente.
7. Enviar notificación email por `IEmailService` al detectar nuevo evento de alerta (template `alert.analytics.degradation`).
8. Exponer endpoints CRUD de umbrales (`POST/GET/PUT/DELETE /api/analytics/advanced/alert-thresholds`) y consulta de eventos (`GET /api/analytics/advanced/alert-events`).
9. Cubrir con pruebas TDD (RED→GREEN): evaluación de umbral excedido, no excedido, notificación email y consulta de eventos.
10. Validar build/test Release y actualizar documentación.

Expected Output:
Sistema de alertas operativas para analytics avanzado con umbrales configurables por tenant, registro auditable de eventos y notificación automática por email, con cobertura de pruebas enterprise.

Dependencies:
TASK-FULL-17.

Entregables implementados:
- Dominio de alertas agregado en:
	- `backend/src/Api/Domain/Observability/AlertThreshold.cs`.
	- `backend/src/Api/Domain/Observability/AlertEvent.cs`.
- Contratos API de alertas implementados en:
	- `backend/src/Api/Contracts/Analytics/AlertThresholdContracts.cs`.
	- `backend/src/Api/Contracts/Analytics/AlertEventContracts.cs`.
- Capa Application de alertas implementada en:
	- `backend/src/Api/Application/Observability/IAlertThresholdRepository.cs`.
	- `backend/src/Api/Application/Observability/IAlertEventRepository.cs`.
	- `backend/src/Api/Application/Observability/IAlertEvaluationService.cs`.
	- `backend/src/Api/Application/Observability/AlertEvaluationService.cs`.
- Persistencia EF Core implementada en:
	- `backend/src/Api/Infrastructure/Observability/AlertThresholdRepository.cs`.
	- `backend/src/Api/Infrastructure/Observability/AlertEventRepository.cs`.
- Evaluación de degradación integrada en flujo de flush (`TASK-FULL-17`):
	- `backend/src/Api/Application/Observability/ObservabilityPersistenceService.cs` ahora persiste snapshot y ejecuta `AlertEvaluationService`.
	- Reglas activas: evento por `ErrorRatePercent` y/o por `AverageLatencyMs` cuando se exceden umbrales configurados.
- Notificación por email integrada:
	- `backend/src/Api/Application/Email/IEmailService.cs` + `EmailService.cs` con método `SendAnalyticsDegradationAlertAsync`.
	- Template nuevo `alert.analytics.degradation` seeded en `Program.cs`.
	- Registro en `EmailLogs` con estado `Sent` o `Skipped` según configuración SMTP.
- API de alertas implementada en:
	- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
	- Endpoints:
		- `POST /api/analytics/advanced/alert-thresholds`.
		- `GET /api/analytics/advanced/alert-thresholds`.
		- `PUT /api/analytics/advanced/alert-thresholds/{id}`.
		- `DELETE /api/analytics/advanced/alert-thresholds/{id}`.
		- `GET /api/analytics/advanced/alert-events`.
- Infraestructura y wiring:
	- `LeadsDbContext` actualizado con `DbSet<AlertThreshold>` y `DbSet<AlertEvent>` + mappings con tenant isolation.
	- `Program.cs` actualizado con DI (`IAlertThresholdRepository`, `IAlertEventRepository`, `IAlertEvaluationService`) y bootstrap SQL de tablas `AlertThresholds` / `AlertEvents` + índices.

Evidencia de validacion:
- `dotnet test -c Release --filter "FullyQualifiedName~AnalyticsAdvancedAlertsEndpointTests"` → 4/4 tests GREEN.
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --no-build` → 66/66 tests GREEN.

## TASK-FULL-19: Dashboard operativo de observabilidad analytics
**Estado:** ✅ Completada

Title: Exponer vista operativa de salud del sistema analytics con series temporales y estado de alertas

Context:
Con persistencia y alertas operativas, el último bloque de madurez es una UI dedicada a operadores del sistema que muestre: evolución temporal de métricas por endpoint (series de ErrorRate y latencia), estado actual de umbrales activos y log de alertas recientes. La vista debe consumir los endpoints de TASK-FULL-17 y TASK-FULL-18 y ser coherente con el design system del proyecto.

Steps:
1. Crear `backend/src/Api/wwwroot/observability.html` con estructura de tabs: `Métricas en tiempo real`, `Historial`, `Alertas`.
2. Implementar tab `Métricas en tiempo real` consumiendo `GET /api/analytics/advanced/metrics` (snapshot actual) con auto-refresh configurable.
3. Implementar tab `Historial` consumiendo `GET /api/analytics/advanced/metrics/history` con selector de ventana temporal y nombre de endpoint; renderizar tabla de series.
4. Implementar tab `Alertas` con sub-secciones: gestión de umbrales (`AlertThreshold` CRUD) y log de eventos (`AlertEvent`) con indicador visual de severidad.
5. Agregar navegación desde `analytics-advanced.html` y `dashboard.html` hacia `/observability.html`.
6. Implementar service layer frontend `observabilityService` para consumo de todos los endpoints de observabilidad con manejo de estados `loading`, `error` y `empty`.
7. Cubrir con pruebas de integración backend: serving del HTML, filtros de historial, consulta de alertas activas.
8. Validar build/test Release y actualizar documentación de cierre.

Expected Output:
Dashboard operativo de observabilidad analytics disponible en `/observability.html`, con tres tabs funcionales, service layer frontend, navegación integrada al sistema y cobertura de pruebas enterprise sin regresiones.

Dependencies:
TASK-FULL-17, TASK-FULL-18.

Entregables implementados:
- Dashboard operativo implementado en:
	- `backend/src/Api/wwwroot/observability.html`.
	- Estructura con tabs funcionales: `Metricas en tiempo real`, `Historial`, `Alertas`.
- Service layer frontend `observabilityService` implementado para consumo de:
	- `GET /api/analytics/advanced/metrics`.
	- `GET /api/analytics/advanced/metrics/history`.
	- `GET /api/analytics/advanced/alert-thresholds`.
	- `POST /api/analytics/advanced/alert-thresholds`.
	- `DELETE /api/analytics/advanced/alert-thresholds/{id}`.
	- `GET /api/analytics/advanced/alert-events`.
- UX operativa incluida:
	- Auto-refresh configurable para snapshot en tiempo real.
	- Filtros de historial por `startUtc`, `endUtc`, `endpointName`.
	- Gestión de umbrales con creación y eliminación.
	- Log de eventos con indicador visual de severidad y estado de notificación.
- Navegación integrada hacia observabilidad desde:
	- `backend/src/Api/wwwroot/analytics-advanced.html`.
	- `backend/src/Api/wwwroot/dashboard.html`.
- Endpoint backend reforzado para consulta de alertas activas:
	- `GET /api/analytics/advanced/alert-thresholds?isActive=true` en `AnalyticsAdvancedAlertsController`.
- Pruebas TDD nuevas en:
	- `backend/tests/Api.Tests/ObservabilityDashboardFrontendEndpointTests.cs` (3 pruebas RED→GREEN).
	- Cobertura: serving de `/observability.html`, navegación desde vistas existentes, filtro backend de umbrales activos.

Evidencia de validacion:
- `dotnet test -c Release --filter "FullyQualifiedName~ObservabilityDashboardFrontendEndpointTests"` → 3/3 tests GREEN.
- `dotnet build -c Release` → 0 errores, 0 advertencias.
- `dotnet test -c Release --no-build` → 69/69 tests GREEN.

## TASK-ARC-20: ARC-01..ARC-15 (bloque de gobernanza API y resiliencia)
**Estado:** ✅ Parcialmente completado (13/15)

Title: Ejecutar endurecimiento transversal de arquitectura/API para ARC-01..ARC-15 sin regresiones

Context:
Luego de completar TASK-FULL-19 se priorizo cerrar brechas transversales de gobernanza: versionado, manejo uniforme de errores, validacion, idempotencia, paginacion operativa, resiliencia outbound y health checks. Se mantuvo compatibilidad con contratos existentes para no interrumpir flujos ya productivos.

Steps:
1. Implementar versionado por header para endpoints API.
2. Estandarizar contrato de error y middleware global de excepciones.
3. Aplicar validacion transversal de ModelState.
4. Agregar idempotencia en intake critico de leads.
5. Incorporar paginacion opcional en endpoints de listado de alto uso.
6. Fortalecer politica de timeout/reintento outbound SMTP.
7. Exponer health checks live/ready con chequeo de DB.
8. Actualizar auditoria ARC y validar regresion completa de pruebas.

Expected Output:
Backend con guardrails enterprise transversales activos y evidencias de no regresion.

Dependencies:
TASK-FULL-19.

Entregables implementados:
- Versionado por header implementado:
	- `backend/src/Api/Middleware/ApiVersioningMiddleware.cs`.
- Contrato de error estandarizado y middleware global:
	- `backend/src/Api/Contracts/ApiErrorResponse.cs`.
	- `backend/src/Api/Middleware/GlobalExceptionHandlingMiddleware.cs`.
- Validacion transversal de payloads:
	- `backend/src/Api/Program.cs` con `InvalidModelStateResponseFactory`.
- Idempotencia para intake:
	- `backend/src/Api/Application/Common/Interfaces/IIdempotencyStore.cs`.
	- `backend/src/Api/Infrastructure/Tenancy/InMemoryIdempotencyStore.cs`.
	- `backend/src/Api/Controllers/LeadsController.cs` (`Idempotency-Key`).
- Paginacion opcional (`page`, `pageSize`) aplicada en listados:
	- `backend/src/Api/Controllers/AssignmentsController.cs`.
	- `backend/src/Api/Controllers/EmailController.cs`.
	- `backend/src/Api/Controllers/RulesController.cs`.
	- `backend/src/Api/Controllers/ProposalsController.cs`.
	- `backend/src/Api/Controllers/OnboardingController.cs`.
	- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
- Resiliencia outbound SMTP:
	- `backend/src/Api/Infrastructure/Email/SmtpEmailSender.cs` (timeout + retries).
- Health checks:
	- `backend/src/Api/Program.cs` (`/health/live`, `/health/ready`).
	- `backend/src/Api/Api.csproj` (paquete EF health checks).
- Pruebas nuevas ARC:
	- `backend/tests/Api.Tests/ApiGovernanceEndpointTests.cs`.

Pendientes formales del bloque ARC:
- ARC-01: migracion completa de bootstrap SQL a migraciones versionadas EF.
- ARC-13: PoC comparativa y benchmark de provider multiusuario de produccion.

Evidencia de validacion:
- `dotnet test -c Release` → 72/72 tests GREEN.

## TASK-SEC-21: Cierre SEC-01..SEC-20 (seguridad, identidad y cumplimiento)
**Estado:** ✅ Completada

Title: Ejecutar endurecimiento de seguridad enterprise para SEC-01..SEC-20 con validacion integral

Context:
Se priorizo cerrar la brecha de seguridad transversal del backend: autenticacion/autorizacion, controles anti-abuso, cifrado de secretos, hardening HTTP, auditoria administrativa y artefactos de cumplimiento (ASVS, threat model, runbook de incidentes).

Steps:
1. Implementar stack de seguridad en runtime (auth, CORS, rate limiting, headers, CSP, API key intake).
2. Reforzar integridad tenant/role y RBAC operativo en middlewares.
3. Cifrar secretos SMTP en reposo y endurecer envio de adjuntos.
4. Incorporar auditoria administrativa y politica de retencion de datos sensibles.
5. Agregar pruebas de hardening para 401/403, spoofing, brute-force, headers y cifrado en reposo.
6. Crear artefactos de cumplimiento (ASVS baseline, threat model, incident response runbook) y gate SAST/DAST.

Expected Output:
Bloque SEC-01..SEC-20 implementado con evidencia de pruebas y documentacion operativa/compliance.

Dependencies:
TASK-ARC-20.

Entregables implementados:
- Seguridad runtime y middleware:
	- `backend/src/Api/Program.cs` (Auth/JWT, Authorization, CORS, RateLimiter, DataProtection, pipeline hardening).
	- `backend/src/Api/Middleware/TenantMiddleware.cs`.
	- `backend/src/Api/Middleware/RoleAuthorizationMiddleware.cs`.
	- `backend/src/Api/Middleware/SecurityHeadersMiddleware.cs`.
	- `backend/src/Api/Middleware/BruteForceProtectionMiddleware.cs`.
	- `backend/src/Api/Middleware/LeadIntakeApiKeyMiddleware.cs`.
- Seguridad de secretos y adjuntos:
	- `backend/src/Api/Infrastructure/Email/SmtpSettingsRepository.cs` (cifrado en reposo).
	- `backend/src/Api/Domain/Email/SmtpSettings.cs`.
	- `backend/src/Api/Infrastructure/Email/SmtpEmailSender.cs` (validacion de tipo/tamano adjuntos + resiliencia).
- Auditoria y retencion:
	- `backend/src/Api/Domain/Security/AdminAuditLog.cs`.
	- `backend/src/Api/Application/Security/IAdminAuditService.cs`.
	- `backend/src/Api/Infrastructure/Security/AdminAuditService.cs`.
	- `backend/src/Api/Infrastructure/Security/SensitiveDataRetentionService.cs`.
	- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs` (`AdminAuditLogs`).
	- `backend/src/Api/Controllers/EmailController.cs`, `RulesController.cs`, `AnalyticsAdvancedAlertsController.cs` (audit trail).
- CI/CD security gate y cumplimiento:
	- `.github/workflows/security-sast-dast.yml`.
	- `ia/security/asvs-baseline.md`.
	- `ia/security/threat-model-sec19.md`.
	- `ia/security/incident-response-sec20.md`.
- Pruebas nuevas:
	- `backend/tests/Api.Tests/SecurityHardeningEndpointTests.cs`.

Evidencia de validacion:
- `dotnet test -c Release` → 78/78 tests GREEN.

## TASK-DAT-22: Ejecucion parcial DAT-01..DAT-18 (gobernanza de datos v2)
**Estado:** ✅ Completada (wave 1)

Title: Implementar primera ola enterprise de gobernanza de datos (dedup/scoring metadata/quality/pipeline integrity)

Context:
Se inicio la ejecucion del backlog DAT con foco en capacidades de alto impacto operacional y bajo riesgo de regresion: versionado de scoring, recalculo historico controlado, metadata estandarizada en intake, validaciones de calidad de contacto y KPIs de calidad en dashboard.

Steps:
1. Extender modelo `Lead` y contratos API con metadata de canal/campana/country y linaje de scoring.
2. Introducir deduplicacion fuzzy v2 configurable por `DataGovernanceOptions` (modo enforcement opt-in).
3. Exponer recalculo historico de scoring por ventana temporal en `POST /api/scoring/recalculate`.
4. Endurecer integridad de transiciones de pipeline (retrocesos con razon y control de saltos).
5. Agregar endpoint de data quality (`GET /api/dashboard/data-quality`) con completitud y duplicidad candidata.
6. Centralizar catalogo inicial de codigos de error de dominio.

Expected Output:
Backlog DAT avanzado con endpoints y modelo de datos listos para evolucion por tenant sin romper contratos existentes.

Dependencies:
TASK-SEC-21.

Entregables implementados:
- Gobernanza y deduplicacion:
	- `backend/src/Api/Application/DataGovernance/DataGovernanceOptions.cs`.
	- `backend/src/Api/Application/Leads/LeadIntakeService.cs`.
	- `backend/src/Api/Application/Leads/LeadSourceCatalog.cs`.
- Versionado/recalculo de scoring:
	- `backend/src/Api/Application/Scoring/ILeadScoringService.cs`.
	- `backend/src/Api/Application/Scoring/LeadScoringService.cs`.
	- `backend/src/Api/Controllers/ScoringController.cs`.
	- `backend/src/Api/Contracts/LeadScoreResponse.cs`.
	- `backend/src/Api/Contracts/ScoreRecalculationRequest.cs`.
	- `backend/src/Api/Contracts/ScoreRecalculationResponse.cs`.
- Metadata de lead + persistencia:
	- `backend/src/Api/Domain/Leads/Lead.cs`.
	- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs`.
	- `backend/src/Api/Program.cs` (bootstrap idempotente de columnas e indices).
	- `backend/src/Api/Contracts/LeadIntakeRequest.cs`.
	- `backend/src/Api/Contracts/LeadIntakeResponse.cs`.
- Integridad de pipeline y calidad de datos:
	- `backend/src/Api/Application/Pipeline/PipelineService.cs`.
	- `backend/src/Api/Application/Contacts/ContactService.cs`.
	- `backend/src/Api/Application/Dashboard/IDashboardService.cs`.
	- `backend/src/Api/Application/Dashboard/DashboardService.cs`.
	- `backend/src/Api/Controllers/DashboardController.cs`.
	- `backend/src/Api/Contracts/DataQualityOverviewResponse.cs`.
- Catalogo de errores de dominio:
	- `backend/src/Api/Contracts/DomainErrorCodes.cs`.
	- `backend/src/Api/Middleware/GlobalExceptionHandlingMiddleware.cs`.

Evidencia de validacion:
- `dotnet test` → 78/78 tests GREEN (runTests).

## TASK-DAT-23: Ejecucion DAT wave 2 (DAT-04/05/06/08/11/13/15/17/18)
**Estado:** ✅ Completada

Title: Completar gobernanza de datos wave 2 con controles de entorno, auditoria temporal, soft delete y continuidad operacional

Context:
Se completo la segunda ola DAT para cerrar controles operativos pendientes sin romper compatibilidad funcional del backend.

Steps:
1. Implementar gobernanza de reglas por entorno con versionado y promocion.
2. Reforzar constraints/indexes de referencialidad en propuestas y onboarding.
3. Implementar auditoria temporal de cambios en lead.
4. Activar soft delete en entidades operativas seleccionadas.
5. Forzar consistencia UTC en serializacion de API.
6. Registrar ejecuciones del scheduler de retencion para evidencia operativa.
7. Documentar backup/restore drill y estrategia de migracion de base de datos productiva.

Expected Output:
Backlog DAT-04..DAT-18 cerrado con evidencia tecnica y documental.

Dependencies:
TASK-DAT-22.

Entregables implementados:
- Gobernanza de reglas:
	- `backend/src/Api/Domain/Rules/Rule.cs`.
	- `backend/src/Api/Application/RulesEngine/RuleEngineService.cs`.
	- `backend/src/Api/Controllers/RulesController.cs`.
	- `backend/src/Api/Contracts/RuleCreateRequest.cs`.
	- `backend/src/Api/Contracts/RuleUpdateRequest.cs`.
	- `backend/src/Api/Contracts/RulePromotionRequest.cs`.
	- `backend/src/Api/Contracts/RuleResponse.cs`.
- Auditoria temporal + retention evidence:
	- `backend/src/Api/Domain/Leads/LeadAuditSnapshot.cs`.
	- `backend/src/Api/Application/Common/Interfaces/ILeadAuditSnapshotRepository.cs`.
	- `backend/src/Api/Infrastructure/Persistence/LeadAuditSnapshotRepository.cs`.
	- `backend/src/Api/Controllers/LeadsController.cs` (`GET /api/leads/{id}/audits`).
	- `backend/src/Api/Domain/Security/DataRetentionRun.cs`.
	- `backend/src/Api/Infrastructure/Security/SensitiveDataRetentionService.cs`.
- Soft delete y consistencia UTC:
	- `backend/src/Api/Domain/Contacts/Contact.cs`.
	- `backend/src/Api/Domain/Companies/Company.cs`.
	- `backend/src/Api/Infrastructure/Persistence/ContactRepository.cs`.
	- `backend/src/Api/Infrastructure/Persistence/CompanyRepository.cs`.
	- `backend/src/Api/Infrastructure/Serialization/UtcDateTimeJsonConverter.cs`.
	- `backend/src/Api/Infrastructure/Serialization/NullableUtcDateTimeJsonConverter.cs`.
	- `backend/src/Api/Program.cs`.
	- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs`.
- Runbooks operativos:
	- `ia/ops/dat17-backup-restore-drill.md`.
	- `ia/ops/dat18-production-db-migration.md`.

Evidencia de validacion:
- TDD: test de promocion de reglas agregado y ejecutado (`PromoteRule_ToProduction_UpdatesGovernanceMetadata`).
- `runTests` → 79/79 tests GREEN.

## TASK-DAT-24: Cierre DAT residual (masking PII, eventos de anomalía, dead-letter remediation)
**Estado:** ✅ Completada

Title: Completar endurecimiento residual de gobernanza de datos y operacion diferida

Context:
Quedaban tres capacidades DAT transversales sin cerrar tras wave 2: politica de masking PII en endpoints operativos, tracking estructurado de anomalías de datos y workflow de remediacion para follow-up jobs fallidos.

Steps:
1. Introducir utilidad reusable de masking para email, telefono y payload JSON auditado.
2. Aplicar masking sobre respuestas operativas de email logs, follow-up jobs y lead audits.
3. Registrar eventos de anomalía de datos durante intake sobre snapshots auditables.
4. Exponer conteo de anomalías desde `GET /api/dashboard/data-quality`.
5. Exponer cola dead-letter de follow-up fallido y operacion de requeue.
6. Ajustar y ampliar pruebas de integracion para nuevo contrato.

Expected Output:
Controles residuales DAT cerrados con contrato operativo seguro, trazabilidad de anomalías y remediación de jobs fallidos.

Dependencies:
TASK-DAT-23.

Entregables implementados:
- Politica de masking:
	- `backend/src/Api/Application/Common/Security/PiiMasking.cs`.
	- `backend/src/Api/Controllers/EmailController.cs`.
	- `backend/src/Api/Controllers/FollowUpController.cs`.
	- `backend/src/Api/Controllers/LeadsController.cs`.
- Tracking de anomalías:
	- `backend/src/Api/Application/Leads/LeadIntakeService.cs`.
	- `backend/src/Api/Application/Common/Interfaces/ILeadAuditSnapshotRepository.cs`.
	- `backend/src/Api/Infrastructure/Persistence/LeadAuditSnapshotRepository.cs`.
	- `backend/src/Api/Application/Dashboard/DashboardService.cs`.
	- `backend/src/Api/Contracts/DataQualityOverviewResponse.cs`.
- Dead-letter remediation:
	- `backend/src/Api/Application/FollowUp/IFollowUpService.cs`.
	- `backend/src/Api/Application/FollowUp/IFollowUpJobRepository.cs`.
	- `backend/src/Api/Application/FollowUp/FollowUpService.cs`.
	- `backend/src/Api/Infrastructure/FollowUp/FollowUpJobRepository.cs`.
	- `backend/src/Api/Controllers/FollowUpController.cs` (`GET /api/followup/dead-letter`, `POST /api/followup/jobs/{id}/requeue`).
- Pruebas:
	- `backend/tests/Api.Tests/EmailEndpointTests.cs`.
	- `backend/tests/Api.Tests/DashboardEndpointTests.cs`.
	- `backend/tests/Api.Tests/FollowUpEndpointTests.cs`.

Evidencia de validacion:
- TDD focalizado: masking operativo, conteo de anomalías y requeue de dead-letter validados en tests dedicados.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 82/82 tests GREEN.

## TASK-DAT-25: Extender dead-letter remediation a proposal reminders
**Estado:** ✅ Completada

Title: Unificar remediacion operativa de jobs fallidos sobre recordatorios de propuestas

Context:
El patron dead-letter ya existia para follow-up jobs. Faltaba extenderlo al reminder diferido de propuestas para evitar correcciones manuales en base de datos cuando un recordatorio entraba en estado `Failed`.

Steps:
1. Exponer consulta de proposal reminders fallidos.
2. Permitir requeue controlado del reminder fallido sin romper integridad por propuesta.
3. Aplicar masking PII al destinatario en la vista operativa.
4. Validar el ciclo fail -> dead-letter -> requeue con prueba de integracion.

Expected Output:
Reminder de propuesta fallido visible y reencolable desde API operativa.

Dependencies:
TASK-DAT-24.

Entregables implementados:
- `backend/src/Api/Domain/Proposals/ProposalReminderJob.cs`.
- `backend/src/Api/Application/Proposals/IProposalReminderJobRepository.cs`.
- `backend/src/Api/Infrastructure/Proposals/ProposalReminderJobRepository.cs`.
- `backend/src/Api/Application/Proposals/IProposalService.cs`.
- `backend/src/Api/Application/Proposals/ProposalService.cs`.
- `backend/src/Api/Controllers/ProposalsController.cs`.
- `backend/src/Api/Contracts/ProposalReminderJobResponse.cs`.
- `backend/tests/Api.Tests/ProposalAutomationEndpointTests.cs`.

Evidencia de validacion:
- `FailedProposalReminder_IsListedInDeadLetter_AndCanBeRequeued` GREEN.

## TASK-DAT-26: Exponer historial de anomalías de datos
**Estado:** ✅ Completada

Title: Habilitar consulta histórica de eventos de anomalía por ventana y tipo

Context:
El dashboard ya reportaba el conteo agregado de anomalías. Faltaba la superficie de consulta para auditar eventos concretos, filtrar por tipo y revisar evidencia dentro de una ventana temporal.

Steps:
1. Extender repositorio de `LeadAuditSnapshots` con query por prefijo y rango temporal.
2. Exponer endpoint de dashboard para anomalías históricas.
3. Maskear payload auditado antes de devolverlo al cliente.
4. Validar filtro por tipo y rango temporal con test de integración.

Expected Output:
Endpoint `GET /api/dashboard/data-quality` con trazabilidad histórica usable desde operación/soporte.

Dependencies:
TASK-DAT-24.

Entregables implementados:
- `backend/src/Api/Application/Common/Interfaces/ILeadAuditSnapshotRepository.cs`.
- `backend/src/Api/Infrastructure/Persistence/LeadAuditSnapshotRepository.cs`.
- `backend/src/Api/Application/Dashboard/IDashboardService.cs`.
- `backend/src/Api/Application/Dashboard/DashboardService.cs`.
- `backend/src/Api/Controllers/DashboardController.cs`.
- `backend/src/Api/Contracts/DataAnomalyEventResponse.cs`.
- `backend/tests/Api.Tests/DashboardEndpointTests.cs`.

Evidencia de validacion:
- `GetDataQualityAnomalies_FiltersByWindowAndType` GREEN.

## TASK-DAT-27: Observabilidad de drift de reglas y hardening de consulta
**Estado:** ✅ Completada

Title: Añadir visibilidad operacional de drift de reglas con query hardening en repositorio

Context:
La gobernanza por entorno ya permitia versionar y promocionar reglas, pero faltaba una vista rápida para detectar drift operativo y endurecer las consultas de reglas con includes multiples.

Steps:
1. Exponer summary de drift de reglas desde API.
2. Incluir breakdown por entorno y estados de aprobación.
3. Marcar reglas activas fuera de producción como señal operativa.
4. Endurecer queries del repositorio con `AsSplitQuery`/`AsNoTracking` donde aplica.
5. Validar el summary con prueba de integración.

Expected Output:
Endpoint `GET /api/rules/drift-summary` con señal operativa de drift y menor riesgo de degradación por includes multiples.

Dependencies:
TASK-DAT-23.

Entregables implementados:
- `backend/src/Api/Application/RulesEngine/IRuleService.cs`.
- `backend/src/Api/Application/RulesEngine/RuleEngineService.cs`.
- `backend/src/Api/Controllers/RulesController.cs`.
- `backend/src/Api/Infrastructure/RulesEngine/RuleRepository.cs`.
- `backend/src/Api/Contracts/RuleDriftSummaryResponse.cs`.
- `backend/tests/Api.Tests/RulesEngineEndpointTests.cs`.

Evidencia de validacion:
- `GetDriftSummary_ReturnsEnvironmentAndApprovalBreakdown` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 85/85 tests GREEN.

## TASK-DAT-28: Retry policy y poison queue para automatizaciones diferidas
**Estado:** ✅ Completada

Title: Extender la remediación operativa con reintentos automáticos y cola poison para jobs diferidos

Context:
El patrón operativo ya cubría dead-letter manual para follow-up y proposals. Faltaba un comportamiento más robusto ante fallos repetidos: reintentos automáticos acotados y una cola terminal explícita para los casos que no deben seguir reprocesándose de forma indefinida.

Steps:
1. Añadir estado terminal `Poisoned` para jobs de follow-up y proposal reminders.
2. Implementar retry policy acotada con reprogramación automática tras fallo de entrega.
3. Exponer consultas operativas de poison queue.
4. Permitir requeue manual también desde estado `Poisoned`.
5. Ajustar contratos API para exponer número de intento actual.
6. Validar follow-up y proposals con pruebas de integración dedicadas.

Expected Output:
Automatizaciones diferidas con retry policy determinista, poison queue visible por API y remediación manual segura al agotarse los intentos.

Dependencies:
TASK-DAT-24.
TASK-DAT-25.

Entregables implementados:
- `backend/src/Api/Domain/FollowUp/FollowUpJob.cs`.
- `backend/src/Api/Domain/FollowUp/FollowUpJobStatus.cs`.
- `backend/src/Api/Application/FollowUp/IFollowUpJobRepository.cs`.
- `backend/src/Api/Application/FollowUp/FollowUpService.cs`.
- `backend/src/Api/Infrastructure/FollowUp/FollowUpJobRepository.cs`.
- `backend/src/Api/Controllers/FollowUpController.cs`.
- `backend/src/Api/Domain/Proposals/ProposalReminderJob.cs`.
- `backend/src/Api/Domain/Proposals/ProposalReminderStatus.cs`.
- `backend/src/Api/Application/Proposals/IProposalReminderJobRepository.cs`.
- `backend/src/Api/Application/Proposals/IProposalService.cs`.
- `backend/src/Api/Application/Proposals/ProposalService.cs`.
- `backend/src/Api/Infrastructure/Proposals/ProposalReminderJobRepository.cs`.
- `backend/src/Api/Controllers/ProposalsController.cs`.
- `backend/src/Api/Contracts/ProposalResponse.cs`.
- `backend/tests/Api.Tests/FollowUpEndpointTests.cs`.
- `backend/tests/Api.Tests/ProposalAutomationEndpointTests.cs`.

Evidencia de validacion:
- `ExecuteDueFollowUp_RetriesUntilPoisonQueue_WhenDeliveryKeepsFailing` GREEN.
- `ExecuteDueProposalReminders_WhenDeliveryFails_SchedulesRetryAttempt` GREEN.
- `PoisonedProposalReminder_IsListedInPoisonQueue_AndCanBeRequeued` GREEN.
- `ExecuteDueProposalReminders_RetriesUntilPoisonQueue_WhenDeliveryKeepsFailing` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 87/87 tests GREEN.

## TASK-DAT-29: Extender retry/poison queue a onboarding diferido
**Estado:** ✅ Completada

Title: Incorporar onboarding welcome jobs al patrón enterprise de retries y poison queue

Context:
El patrón de remediación diferida ya cubría follow-up y proposal reminders. Faltaba extenderlo a onboarding welcome para homogeneizar la operación de automatizaciones post-venta bajo el mismo contrato de retries acotados y cola terminal.

Steps:
1. Crear agregado de onboarding welcome job con estado y número de intento.
2. Programar job de bienvenida al crear customer `Won`.
3. Ejecutar due jobs con retry policy acotada y transición a `Poisoned`.
4. Exponer endpoints operativos de ejecución, poison queue, dead-letter y requeue.
5. Validar flujo de agotamiento y recuperación manual con integración.

Expected Output:
Automatización de onboarding con semántica homogénea de retry/poison queue y APIs operativas para soporte.

Dependencies:
TASK-DAT-28.

Entregables implementados:
- `backend/src/Api/Domain/Onboarding/OnboardingWelcomeJob.cs`.
- `backend/src/Api/Domain/Onboarding/OnboardingWelcomeJobStatus.cs`.
- `backend/src/Api/Application/Onboarding/IOnboardingWelcomeJobRepository.cs`.
- `backend/src/Api/Infrastructure/Onboarding/OnboardingWelcomeJobRepository.cs`.
- `backend/src/Api/Application/Onboarding/IOnboardingService.cs`.
- `backend/src/Api/Application/Onboarding/OnboardingService.cs`.
- `backend/src/Api/Controllers/OnboardingController.cs`.
- `backend/src/Api/Contracts/OnboardingWelcomeJobResponse.cs`.
- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs`.
- `backend/src/Api/Program.cs`.
- `backend/tests/Api.Tests/OnboardingAutomationEndpointTests.cs`.

Evidencia de validacion:
- `MoveOpportunityToWon_WithWelcomeFailures_MovesJobToPoisonQueue_AndAllowsRequeue` GREEN.

## TASK-DAT-30: Alertas operativas por crecimiento de poison queue
**Estado:** ✅ Completada

Title: Disparar alertas operativas por tipo de job cuando crece la poison queue

Context:
La observabilidad de poison queue estaba disponible por consulta operativa, pero faltaba alertado proactivo por crecimiento para reacción temprana de soporte y operación por tenant/tipo de job.

Steps:
1. Crear servicio de alertado operativo para poison queue.
2. Integrar notificación en transiciones a `Poisoned` de follow-up, proposals y onboarding.
3. Evaluar umbral por endpoint lógico (`poison-queue/{jobType}`).
4. Persistir `AlertEvent` con métrica `PoisonQueueDepth` y notificación por email.
5. Validar generación de evento al superar umbral configurado.

Expected Output:
Alertas operativas automáticas al crecer poison queue por tipo de job usando infraestructura existente de thresholds/events.

Dependencies:
TASK-DAT-28.
TASK-DAT-29.

Entregables implementados:
- `backend/src/Api/Application/Observability/IPoisonQueueAlertService.cs`.
- `backend/src/Api/Application/Observability/PoisonQueueAlertService.cs`.
- `backend/src/Api/Application/FollowUp/FollowUpService.cs`.
- `backend/src/Api/Application/Proposals/ProposalService.cs`.
- `backend/src/Api/Application/Onboarding/OnboardingService.cs`.
- `backend/src/Api/Application/FollowUp/IFollowUpJobRepository.cs`.
- `backend/src/Api/Infrastructure/FollowUp/FollowUpJobRepository.cs`.
- `backend/src/Api/Application/Proposals/IProposalReminderJobRepository.cs`.
- `backend/src/Api/Infrastructure/Proposals/ProposalReminderJobRepository.cs`.
- `backend/src/Api/Program.cs`.
- `backend/tests/Api.Tests/ProposalAutomationEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueueGrowth_CreatesOperationalAlertEvent` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 89/89 tests GREEN.

## TASK-DAT-31: Supresión de duplicados y cooldown para alertas de poison queue
**Estado:** ✅ Completada

Title: Endurecer el alertado operativo de poison queue con ventana de enfriamiento y deduplicación por crecimiento significativo

Context:
Tras introducir alertas por crecimiento de poison queue, faltaba reducir ruido operacional cuando ocurren incrementos consecutivos de baja magnitud en periodos muy cortos.

Steps:
1. Incorporar consulta del último `AlertEvent` por endpoint/métrica.
2. Definir ventana de cooldown para no repetir alertas de bajo delta.
3. Permitir nueva alerta dentro de cooldown solo si el crecimiento es significativo.
4. Añadir pruebas de integración para supresión por duplicado y bypass por salto significativo.

Expected Output:
Alertado operativo menos ruidoso, conservando señal temprana en crecimientos relevantes de poison queue.

Dependencies:
TASK-DAT-30.

Entregables implementados:
- `backend/src/Api/Application/Observability/IAlertEventRepository.cs`.
- `backend/src/Api/Infrastructure/Observability/AlertEventRepository.cs`.
- `backend/src/Api/Application/Observability/PoisonQueueAlertService.cs`.
- `backend/tests/Api.Tests/ProposalAutomationEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueueGrowth_WithinCooldown_DeduplicatesSmallIncrease` GREEN.
- `PoisonQueueGrowth_WithinCooldown_AllowsSignificantJump` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 91/91 tests GREEN.

## TASK-DAT-32: Panel de tendencia histórica de poison queue por job type
**Estado:** ✅ Completada

Title: Exponer y visualizar tendencia histórica de poison queue para operación continua por tenant

Context:
Tras reducir ruido con cooldown/deduplicación, faltaba visibilidad histórica agregada para observar evolución de backlog poison por tipo de job y detectar patrones de degradación operativa.

Steps:
1. Agregar endpoint agregado de tendencia sobre `AlertEvent` filtrando `PoisonQueueDepth`.
2. Soportar filtros por ventana temporal, `jobType` y bucket (`day`/`hour`).
3. Integrar sección visual en `observability.html` para consulta operativa de tendencia.
4. Añadir pruebas de integración para endpoint agregado y presencia de la nueva sección en UI.

Expected Output:
Capacidad operacional para analizar tendencia de poison queue por tipo de job sin depender de consultas manuales ad-hoc.

Dependencies:
TASK-DAT-31.

Entregables implementados:
- `backend/src/Api/Contracts/Analytics/AlertEventContracts.cs`.
- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
- `backend/src/Api/wwwroot/observability.html`.
- `backend/tests/Api.Tests/AnalyticsAdvancedAlertsEndpointTests.cs`.
- `backend/tests/Api.Tests/ObservabilityDashboardFrontendEndpointTests.cs`.
- `backend/tests/Api.Tests/DashboardEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueueTrend_ReturnsGroupedSeriesByBucketAndEndpoint` GREEN.
- `PoisonQueueTrend_WithJobTypeFilter_ReturnsOnlyRequestedJobType` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 93/93 tests GREEN.

## TASK-DAT-33: Priorización operativa de poison queue por severidad y variación
**Estado:** ✅ Completada

Title: Convertir tendencia histórica de poison queue en ranking operativo accionable

Context:
Con la tendencia histórica disponible, faltaba una señal de priorización para identificar automáticamente qué tipo de job requiere atención inmediata sin análisis manual de tablas.

Steps:
1. Agregar endpoint de prioridad sobre eventos `PoisonQueueDepth`.
2. Calcular severidad con reglas operativas (`low|medium|high|critical`).
3. Calcular variación entre bucket actual y bucket previo (`deltaDepth`, `deltaPercent`).
4. Permitir filtros operativos por `jobType`, `bucket`, ventana y tamaño de ranking (`top`).
5. Integrar panel de prioridad en observability UI.
6. Validar endpoint y UI con pruebas de integración.

Expected Output:
Ranking operativo de poison queue que prioriza automáticamente degradaciones por tipo de job en ventana reciente.

Dependencies:
TASK-DAT-32.

Entregables implementados:
- `backend/src/Api/Contracts/Analytics/AlertEventContracts.cs`.
- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
- `backend/src/Api/wwwroot/observability.html`.
- `backend/tests/Api.Tests/AnalyticsAdvancedAlertsEndpointTests.cs`.
- `backend/tests/Api.Tests/ObservabilityDashboardFrontendEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueuePriority_ReturnsRankedItemsWithSeverityAndVariation` GREEN.
- `PoisonQueuePriority_WithJobTypeFilter_ReturnsOnlyRequestedJobType` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 95/95 tests GREEN.

## TASK-DAT-34: Acciones recomendadas y atajos de remediación en ranking poison
**Estado:** ✅ Completada

Title: Añadir guía operativa contextual (runbook hints) y shortcut de remediación al ranking de poison queue

Context:
El ranking por severidad/variación ya priorizaba qué atender primero, pero faltaba orientar el siguiente paso operativo para reducir tiempo de respuesta de soporte.

Steps:
1. Extender contrato de prioridad con acción recomendada, hint de runbook y ruta de remediación.
2. Generar recomendaciones dinámicas por severidad y tipo de job.
3. Publicar shortcut de remediación por tipo de cola poison.
4. Mostrar en observability UI columnas de guía operativa y botón "Open remediation".
5. Validar contratos y visualización con pruebas de integración.

Expected Output:
Ranking de poison queue con priorización + guía accionable inmediata para operación.

Dependencies:
TASK-DAT-33.

Entregables implementados:
- `backend/src/Api/Contracts/Analytics/AlertEventContracts.cs`.
- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
- `backend/src/Api/wwwroot/observability.html`.
- `backend/tests/Api.Tests/AnalyticsAdvancedAlertsEndpointTests.cs`.
- `backend/tests/Api.Tests/ObservabilityDashboardFrontendEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueuePriority_ReturnsRankedItemsWithSeverityAndVariation` GREEN (incluye `RecommendedAction`, `RunbookHint`, `RemediationPath`).
- `PoisonQueuePriority_WithJobTypeFilter_ReturnsOnlyRequestedJobType` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 95/95 tests GREEN.

## TASK-DAT-35: Telemetría de ejecución de remediación y feedback loop de efectividad
**Estado:** ✅ Completada

Title: Medir efectividad de remediaciones de poison queue con trazabilidad operacional end-to-end

Context:
Con priorización y shortcuts activos, faltaba cerrar el ciclo de aprendizaje operativo: capturar cada ejecución de remediación (quién/cuándo/resultado) y exponer un resumen de efectividad para ajustar recomendaciones y priorización en ventanas recientes.

Steps:
1. Crear entidad persistente para runs de remediación de poison queue.
2. Implementar repositorio y wiring en DI + bootstrap SQL multitenant.
3. Exponer endpoints para registrar run, listar runs filtrados y calcular efectividad agregada.
4. Integrar telemetría en UI al accionar shortcut `Open remediation`.
5. Agregar panel de efectividad en observability.
6. Validar con pruebas de integración backend + frontend smoke.

Expected Output:
Feedback loop operativo para poison queue con evidencia de ejecución y métricas de efectividad (success rate y latencias) en la misma consola de observabilidad.

Dependencies:
TASK-DAT-34.

Entregables implementados:
- `backend/src/Api/Domain/Observability/PoisonQueueRemediationRun.cs`.
- `backend/src/Api/Application/Observability/IPoisonQueueRemediationRunRepository.cs`.
- `backend/src/Api/Infrastructure/Observability/PoisonQueueRemediationRunRepository.cs`.
- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs`.
- `backend/src/Api/Program.cs`.
- `backend/src/Api/Contracts/Analytics/AlertEventContracts.cs`.
- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
- `backend/src/Api/wwwroot/observability.html`.
- `backend/tests/Api.Tests/AnalyticsAdvancedAlertsEndpointTests.cs`.
- `backend/tests/Api.Tests/ObservabilityDashboardFrontendEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueueRemediationTelemetry_RecordAndEffectivenessSummary_Works` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "PoisonQueueRemediationTelemetry_RecordAndEffectivenessSummary_Works|PoisonQueuePriority_ReturnsRankedItemsWithSeverityAndVariation|ObservabilityHtml_IsServedSuccessfully"` → 3/3 tests GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 96/96 tests GREEN.

## TASK-DAT-36: Ciclo de estado de remediación (opened → in_progress → resolved/partial/failed)
**Estado:** ✅ Completada

Title: Convertir la telemetría de remediación en un ciclo de estado operativo completo

Context:
Con DAT-35 se capturaba ejecución inicial de remediación, pero faltaba modelar explícitamente la evolución del run para reflejar progreso real y resultados finales sin crear registros duplicados por cada cambio.

Steps:
1. Exponer endpoint de actualización por `runId` para outcome de remediación.
2. Agregar soporte en dominio/repositorio para actualizar run existente.
3. Restringir outcomes válidos (`opened`, `in_progress`, `resolved`, `partial`, `failed`).
4. Integrar controles en observability UI para cambiar outcome desde el ranking de prioridad.
5. Actualizar pruebas de integración backend y smoke frontend.

Expected Output:
Ciclo de vida de remediación trazable por run único, con transición de estado operacional y efecto directo en métricas de efectividad.

Dependencies:
TASK-DAT-35.

Entregables implementados:
- `backend/src/Api/Domain/Observability/PoisonQueueRemediationRun.cs`.
- `backend/src/Api/Application/Observability/IPoisonQueueRemediationRunRepository.cs`.
- `backend/src/Api/Infrastructure/Observability/PoisonQueueRemediationRunRepository.cs`.
- `backend/src/Api/Contracts/Analytics/AlertEventContracts.cs`.
- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
- `backend/src/Api/wwwroot/observability.html`.
- `backend/tests/Api.Tests/AnalyticsAdvancedAlertsEndpointTests.cs`.
- `backend/tests/Api.Tests/ObservabilityDashboardFrontendEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueueRemediationTelemetry_UpdateOutcome_ChangesEffectivenessSummary` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "PoisonQueueRemediationTelemetry_UpdateOutcome_ChangesEffectivenessSummary|PoisonQueueRemediationTelemetry_RecordAndEffectivenessSummary_Works|ObservabilityHtml_IsServedSuccessfully"` → 3/3 tests GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 97/97 tests GREEN.

## TASK-DAT-37: Correlación de impacto de remediación sobre PoisonQueueDepth
**Estado:** ✅ Completada

Title: Medir impacto causal aproximado de remediaciones comparando profundidad poison antes y después de la ejecución

Context:
Tras cerrar el ciclo de estado en DAT-36, faltaba un indicador operacional de efectividad real: no solo saber que un run terminó en `resolved`, sino verificar si la profundidad de poison queue disminuyó en la ventana posterior a la remediación.

Steps:
1. Exponer endpoint de correlación de impacto sobre runs cerrados (`resolved|partial|failed`).
2. Calcular `preDepth` (último valor previo) y `postDepth` (mínimo observado posterior en ventana de observación).
3. Publicar métricas derivadas (`DepthDelta`, `ReductionPercent`, `IsPositiveImpact`) por run.
4. Agregar resumen agregado (`PositiveImpactRatePercent`, `AverageDepthDelta`).
5. Integrar panel de impacto en observability UI.
6. Validar con pruebas de integración backend, smoke frontend y regresión completa.

Expected Output:
Indicador operativo accionable que cuantifica si una remediación cerrada estuvo asociada a reducción de backlog poison.

Dependencies:
TASK-DAT-36.

Entregables implementados:
- `backend/src/Api/Contracts/Analytics/AlertEventContracts.cs`.
- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
- `backend/src/Api/wwwroot/observability.html`.
- `backend/tests/Api.Tests/AnalyticsAdvancedAlertsEndpointTests.cs`.
- `backend/tests/Api.Tests/ObservabilityDashboardFrontendEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueueRemediationImpact_ReturnsPositiveDepthReductionAfterResolvedRun` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj --filter "PoisonQueueRemediationImpact_ReturnsPositiveDepthReductionAfterResolvedRun|PoisonQueueRemediationTelemetry_UpdateOutcome_ChangesEffectivenessSummary|ObservabilityHtml_IsServedSuccessfully"` → 3/3 tests GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 98/98 tests GREEN.

---

## TASK-DAT-38: Segmentación de impacto por JobType y Severity
**Estado:** ✅ Completada

Title: Desglosar el impacto de remediación por segmentos de JobType y Severity para priorización automática más fina.

Context:
Tras tener correlación causal de impacto por run individual (DAT-37), el siguiente paso operacional es la vista agregada: ¿cuál tipo de job o severidad tiene mayor tasa de éxito real en remediaciones? Esto permite que los operadores prioricen esfuerzos y el motor de scoring pueda usar métricas de efectividad históricas por segmento.

Steps:
1. Añadir contratos `PoisonQueueImpactSegmentItemResponse` y `PoisonQueueRemediationSegmentResponse`.
2. Implementar endpoint `GET alert-events/poison-queue-remediation-impact/by-segment` que agrupa runs por jobType y severity.
3. Calcular por segmento: `TotalRuns`, `PositiveImpactRuns`, `PositiveImpactRatePercent`, `AverageDepthDelta`.
4. Añadir sección "Impact Segmentation" con dos tablas (By Job Type / By Severity) en observability UI.
5. Registrar función JS `loadRemediationSegment()` + service `getPoisonQueueRemediationImpactBySegment()`.
6. Cubrir con test de integración TDD (RED → GREEN). Extender smoke test frontend.

Expected Output:
Vista segmentada de efectividad de remediación que permite priorización operativa por tipo de job y severidad.

Dependencies:
TASK-DAT-37.

Entregables implementados:
- `backend/src/Api/Contracts/Analytics/AlertEventContracts.cs`.
- `backend/src/Api/Controllers/AnalyticsAdvancedAlertsController.cs`.
- `backend/src/Api/wwwroot/observability.html`.
- `backend/tests/Api.Tests/AnalyticsAdvancedAlertsEndpointTests.cs`.
- `backend/tests/Api.Tests/ObservabilityDashboardFrontendEndpointTests.cs`.

Evidencia de validacion:
- `PoisonQueueRemediationImpactBySegment_ReturnsBreakdownByJobTypeAndSeverity` GREEN.
- `dotnet test tests/Api.Tests/Api.Tests.csproj` → 99/99 tests GREEN.

## TASK-EMF-39: Cierre EMF-01..EMF-14 (email y follow-up enterprise)
**Estado:** ✅ Completada

Title: Endurecer el módulo de email y follow-up con semántica enterprise operativa de punta a punta

Context:
El backlog EMF exigía cerrar capacidades que habían quedado en nivel MVP: provider desacoplado, cola persistente de dispatch, templates versionados, stop-list, políticas de follow-up, retry/KPIs/alerting y superficie admin coherente en frontend.

Steps:
1. Introducir metadata de provider y validación condicional para `smtp` y `webhook`.
2. Convertir welcome/proposal/customer welcome/analytics alert en dispatches persistentes con correlación.
3. Versionar templates con preview, rollback y allowlist estricta de variables.
4. Agregar stop-list persistente y aplicar supresión a email y follow-up.
5. Implementar policy de follow-up por tenant con quiet hours y delay basado en score.
6. Exponer ejecución de cola, retry manual, KPIs de entrega y alertado por degradación.
7. Actualizar la UI admin existente de email para operar providers y templates sin crear una superficie paralela.
8. Validar con tests dirigidos de backend y chequeo de errores del frontend tocado.

Expected Output:
Módulo de email/follow-up listo para operación enterprise: desacoplado del request síncrono, observable, administrable y documentado.

Dependencies:
TASK-MVP-04.
TASK-MVP-05.
TASK-FULL-11.
TASK-FULL-18.
TASK-DAT-28.

Entregables implementados:
- Backend email:
	- `backend/src/Api/Application/Email/EmailService.cs`.
	- `backend/src/Api/Application/Email/EmailDispatchService.cs`.
	- `backend/src/Api/Application/Email/IEmailDispatchService.cs`.
	- `backend/src/Api/Application/Common/Interfaces/IEmailDispatchJobRepository.cs`.
	- `backend/src/Api/Application/Common/Interfaces/IEmailStopListRepository.cs`.
	- `backend/src/Api/Infrastructure/Persistence/EmailDispatchJobRepository.cs`.
	- `backend/src/Api/Infrastructure/Persistence/EmailStopListRepository.cs`.
- Dominio y persistencia:
	- `backend/src/Api/Domain/Email/EmailDispatchJob.cs`.
	- `backend/src/Api/Domain/Email/EmailStopListEntry.cs`.
	- `backend/src/Api/Domain/Email/EmailLog.cs`.
	- `backend/src/Api/Domain/Email/EmailTemplate.cs`.
	- `backend/src/Api/Domain/Email/SmtpSettings.cs`.
	- `backend/src/Api/Domain/FollowUp/FollowUpPolicySettings.cs`.
	- `backend/src/Api/Infrastructure/Persistence/LeadsDbContext.cs`.
	- `backend/src/Api/Program.cs`.
- API operativa:
	- `backend/src/Api/Controllers/EmailController.cs` con endpoints de template versioning/preview/rollback, stop-list, execute-due, retry y KPIs.
	- `backend/src/Api/Controllers/FollowUpController.cs` con `GET/PUT /api/followup/policy`.
- Orquestación follow-up:
	- `backend/src/Api/Application/FollowUp/FollowUpService.cs`.
	- `backend/src/Api/Application/Leads/LeadIntakeService.cs`.
- Frontend admin:
	- `frontend/components/email/SmtpForm.tsx`.
	- `frontend/app/email/templates/page.tsx`.
	- `frontend/services/email.service.ts`.
	- `frontend/types/email.ts`.
	- `frontend/app/globals.css`.
- Pruebas dirigidas:
	- `backend/tests/Api.Tests/EmailEndpointTests.cs`.
	- `backend/tests/Api.Tests/FollowUpEndpointTests.cs`.
	- `backend/tests/LoadTests/email-followup-load.js`.
	- `backend/tests/LoadTests/run-email-followup-load-tests.ps1`.

Evidencia de validacion:
- `dotnet test backend/tests/Api.Tests/Api.Tests.csproj --filter EmailEndpointTests` → 13/13 tests GREEN.
- `dotnet test backend/tests/Api.Tests/Api.Tests.csproj --filter FollowUpEndpointTests` → 9/9 tests GREEN.
- Validación del workspace sin errores en archivos frontend tocados (`SmtpForm`, `templates/page`, `email.service`, `email.ts`, `globals.css`).
- `RUN_MODE=smoke ./run-email-followup-load-tests.ps1 -StartApi` → `backend/tests/LoadTests/results/email-followup-load-20260503-093837.json` (`failRate=0%`, `checks=100%`, `p95=84.89ms`).