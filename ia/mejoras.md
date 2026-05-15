# Mejoras — Auditoria Integral de MindFlow AI Sales Engine

> Ultima actualizacion: 2026-05-04
> Baseline tecnico auditado: backend API .NET + UI estatica en wwwroot + 252/252+ pruebas GREEN

> Estado de verificacion operativa (ultima corrida local, 2026-05-04):
> - Backend build Release: ✅ OK (`dotnet build src/Api/Api.csproj -c Release --no-restore`, 0 errores, 0 advertencias).
> - Frontend lint/build/E2E: ✅ OK (`npm run lint`, `npm run build`, `npm run test:e2e -- --reporter=dot`; 18/18 GREEN).
> - QA observability subset: ✅ OK (`dotnet test tests/Api.Tests/Api.Tests.csproj --nologo --filter "FullyQualifiedName~QaObservability"`; 24/24 GREEN).
> Nota: las evidencias historicas listadas abajo se mantienen como trazabilidad de olas anteriores; para estado operativo vigente, priorizar esta seccion de verificacion.

Actualizacion reciente (PL-03 a PL-12):
- Se completa la ola enterprise de pipeline con riesgo por oportunidad, board filtrable/ordenable, límites WIP por etapa, historial enriquecido, export CSV, throughput por etapa, auto-move auditable por reglas, concurrencia optimista y paginación virtual.
- Hardening multi-tenant agregado al catálogo de etapas: IDs por tenant y unicidad compuesta por `TenantId` para evitar colisiones de seed en entornos SaaS.
- Evidencia de regresion: `backend/tests/Api.Tests/TestResults/post-pipeline-full.trx` => 151/151 GREEN.

Actualizacion reciente (QA-01 a QA-20 — COMPLETADOS):
- Se implementa la suite completa de auditoria QA enterprise (sección 4.7).
- 57 nuevos tests distribuidos en 6 nuevos archivos de prueba (contrato, mutacion, concurrencia, seguridad, scoring, observabilidad).
- Script PowerShell `run-quality-gate.ps1` implementa gate final con build, coverage, seguridad y smoke test.
- Documentacion QA: `ia/qa/qa-traceability-matrix.md` y `ia/qa/qa-rc-test-plan.md` creados.
- Evidencia de regresion: `dotnet test` — 57/57 nuevos tests GREEN. 0 errores de compilacion.

> Objetivo: definir mejoras accionables sobre features existentes y convertirlas en checklist de auditoria ejecutable

## 0) Checklist operativo UI Enterprise (TASK-UI-ENT-01..04)

Objetivo de esta seccion:
- Convertir el plan de `ia/04_tasks.md` en seguimiento diario de ejecucion por fase.
- Unificar criterio de cierre con evidencia minima verificable.

Estado global del frente UI Enterprise:
- [x] Completado (TASK-UI-ENT-01..04 cerrados con validacion dirigida y gobierno documental actualizado)

### Fase 0 — TASK-UI-ENT-01 (riesgo alto)
- [x] Reemplazar confirmaciones nativas por modal accesible reutilizable.
- [x] Sanitizar todo preview/render HTML dinamico antes de pintar en UI.
- [x] Eliminar textos hardcodeados de email/templates y mover a i18n.
- [x] Publicar tokens semanticos UI v1 y mapear componentes base.
- [x] Validar no regresion (`npm run lint` + `npm run test:e2e`).

Evidencia requerida de cierre:
- [x] Archivo de modal accesible reutilizable en componentes UI.
- [x] Referencias de sanitizacion en pantallas con HTML dinamico.
- [x] Claves i18n nuevas y consumo en pantallas intervenidas.
- [x] Tokens semanticos documentados y usados por componentes base.
- [x] Resultado de lint y E2E adjunto en progreso.

### Fase 1 — TASK-UI-ENT-02 (estabilidad operativa)
- [x] Estandarizar capa de datos con cache/invalidation por dominio.
- [x] Implementar optimistic updates en pipeline y reglas.
- [x] Agregar boundaries de carga/error por rutas criticas.
- [x] Escalar email logs a paginacion/filtering server-side.
- [x] Incluir correlation id en requests frontend.

Evidencia requerida de cierre:
- [x] Query keys y estrategia de invalidez documentadas por dominio.
- [x] Flujos de pipeline/reglas sin refetch completo obligatorio.
- [x] Rutas criticas con estados de carga/error verificables.
- [x] Logs de email con paginacion estable bajo volumen.
- [x] Correlation id visible en trazas FE-BE.

### Fase 2 — TASK-UI-ENT-03 (calidad y observabilidad)
- [x] Integrar pruebas automatizadas de accesibilidad en CI.
- [x] Integrar visual regression para vistas criticas.
- [x] Agregar contract tests FE-BE para payloads criticos.
- [x] Publicar dashboard UX con metricas base de operacion.
- [x] Configurar alertas por degradacion de p95 y error-rate.

Evidencia requerida de cierre:
- [x] Workflow CI falla ante regresion a11y/visual.
- [x] Suite de contratos FE-BE ejecutando en pipeline.
- [x] Dashboard UX disponible con metricas minimas.
- [x] Alertas activas y umbrales documentados.

### Fase 3 — TASK-UI-ENT-04 (escala funcional)
- [x] Implementar rule builder guiado con simulacion/rollback.
- [x] Implementar pipeline advanced UX (bulk, vistas guardadas, teclado).
- [x] Publicar catalogo oficial de componentes/patrones UI.
- [x] Formalizar DoD UI enterprise en guias de contribucion.
- [x] Definir SLA de deuda UI por severidad y backlog continuo.

Evidencia requerida de cierre:
- [x] Reglas complejas editables sin JSON manual.
- [x] Operacion masiva de pipeline validada con UX accesible.
- [x] Catalogo de componentes versionado y adoptado.
- [x] DoD UI incluido en proceso de PR/release.
- [x] SLA de deuda UI visible y medible.

### Registro de bloqueos y decisiones rapidas
- [ ] Bloqueos de sprint documentados con fecha, impacto y owner.
- [ ] Decisiones de arquitectura UI registradas en `ia/06_decisions.md` cuando aplique.
- [x] Avances por fase sincronizados en `ia/05_progress.md` al cierre de cada iteracion.

Actualizacion reciente (TASK-UI-ENT-04):
- `RuleBuilderPanel` ahora permite cargar una regla existente y editar multiples condiciones/acciones sin tocar JSON; la actualizacion backend reemplaza hijos de regla sin errores `500` gracias a una ruta explicita de borrado/reinsercion en persistencia.
- `flows.spec.ts` agrega cobertura dirigida para edicion guiada de reglas y para `pipeline bulk move + saved view persistence`.
- `frontend/app/admin/ui-guide/page.tsx`, `CONTRIBUTING.md` y `docs/product/definition-of-done.md` formalizan catalogo UI v1.1, DoD UI enterprise y SLA de deuda por severidad.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~RulesEngineEndpointTests.UpdateRule_ReplacesConditionsAndActions_WithoutServerError` => 1/1 GREEN; `npx playwright test tests/e2e/flows.spec.ts -g "rule builder edits existing rule with multiple conditions and actions"` => 1/1 GREEN; `npx playwright test tests/e2e/flows.spec.ts -g "pipeline bulk move persists saved view"` => 1/1 GREEN; `npm run build` => GREEN.

Actualizacion reciente (DAT-31):
- Alertado de poison queue endurecido con supresion de duplicados y cooldown basado en ultimo evento por endpoint/metrica.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 91/91 GREEN.

Actualizacion reciente (DAT-32):
- Se agrego endpoint de tendencia historica de poison queue por `jobType` y bucket temporal (`day|hour`).
- `observability.html` ahora incluye panel "Poison Queue Trend" para analisis operativo continuo.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 93/93 GREEN.

Actualizacion reciente (DAT-33):
- Se agrego endpoint de priorizacion de poison queue con severidad y variacion inter-bucket (`/alert-events/poison-queue-priority`).
- `observability.html` incorpora panel "Poison Queue Priority" con ranking configurable por ventana, bucket y top.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 95/95 GREEN.

Actualizacion reciente (DAT-34):
- El ranking de prioridad ahora incluye `RecommendedAction`, `RunbookHint` y `RemediationPath` por item.
- Se agrego shortcut `Open remediation` en observability para acceso rapido al endpoint operativo del job type.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 95/95 GREEN.

Actualizacion reciente (DAT-35):
- Se incorpora telemetria persistente de ejecucion de remediacion (`PoisonQueueRemediationRun`) con registro de outcome/actor/latencia.
- Se agregan endpoints para registro, historial filtrable y resumen de efectividad (`success rate`, latencia promedio total y de resueltos).
- `observability.html` registra run con outcome `opened` al usar `Open remediation` y muestra panel "Remediation Effectiveness".
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 96/96 GREEN.

Actualizacion reciente (DAT-36):
- Se agrega endpoint de actualización por `runId` para transicionar outcome de remediación (`opened`, `in_progress`, `resolved`, `partial`, `failed`).
- La UI de observabilidad incorpora controles de estado por fila priorizada y reutiliza el mismo run para reflejar progreso real.
- La efectividad ahora captura con mayor fidelidad el resultado final de la remediación sin inflar el conteo por cambios intermedios.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 97/97 GREEN.

Actualizacion reciente (DAT-37):
- Se agrega endpoint de correlación de impacto `poison-queue-remediation-impact` con señal before/after por run cerrado.
- El cálculo publica `PreDepth`, `PostDepth`, `DepthDelta`, `ReductionPercent` e `IsPositiveImpact`, más agregados de tasa de impacto positivo.
- `observability.html` incorpora panel "Remediation Impact Correlation" con cards y tabla de detalle.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 98/98 GREEN.

Actualizacion reciente (DAT-38):
- Se agrega endpoint de segmentación `poison-queue-remediation-impact/by-segment` con desglose por `byJobType` y `bySeverity`.
- Cada segmento expone `TotalRuns`, `PositiveImpactRuns`, `PositiveImpactRatePercent`, `AverageDepthDelta`.
- `observability.html` incorpora sección "Impact Segmentation" con dos tablas comparativas (By Job Type / By Severity).
- Servicio JS `getPoisonQueueRemediationImpactBySegment` y función `loadRemediationSegment` integrados al ciclo de recarga del tab History.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 99/99 GREEN.

Actualizacion reciente (AO-12):
- Se implementa ciclo de vida operativo de alertas con acciones `acknowledge`, `snooze` y `resolve` sobre `AlertEvent`.
- Endpoint agregado: `PUT /api/analytics/advanced/alert-events/{id}/status`.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 100/100 GREEN.

Actualizacion reciente (AO-10):
- `AlertThreshold` incorpora `WebhookUrl` en dominio, contratos, persistencia y API de umbrales.
- Se mantiene email y se agrega webhook no bloqueante en `AlertEvaluationService` para alertado multi-canal base.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 101/101 GREEN.

Actualizacion reciente (AO-09):
- Nuevo endpoint `GET /api/analytics/advanced/alert-events/slo-status` para estado operativo SLI/SLO por endpoint activo.
- Respuesta incluye objetivo de error/latencia, observado y `compliance`.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 102/102 GREEN.

Actualizacion reciente (AO-07 + AO-08):
- AO-07: se implementa agregacion incremental por lotes para observabilidad con checkpoints por ventana (`windowMinutes`) y estados acumulados por endpoint.
- Endpoints nuevos: `POST /api/analytics/advanced/metrics/history/aggregate-incremental` y `GET /api/analytics/advanced/metrics/history/aggregates`.
- Persistencia nueva: `ObservabilityAggregateBatches`, `ObservabilityEndpointAggregationStates`, `ObservabilityAggregationCheckpoints`.
- AO-08: se agrega control de cardinalidad en telemetria in-memory con normalizacion de rutas dinamicas, bucket de overflow y metadatos de cardinalidad en `GET /api/analytics/advanced/metrics`.
- Evidencia de regresion dirigida: `dotnet test backend/tests/Api.Tests/Api.Tests.csproj --filter FullyQualifiedName~AnalyticsAdvancedObservabilityEndpointTests` => 5/5 GREEN.

Actualizacion reciente (DAT-39):
- Nuevo endpoint `GET /api/analytics/advanced/alert-events/severity-elevation-candidates` para detección de segmentos con baja efectividad de remediación.
- Criterio implementado: candidatos con `PositiveImpactRatePercent < 50%`.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 103/103 GREEN.

Actualizacion reciente (AO-13 + AO-14 + AO-15):
- AO-13: Nuevo endpoint `GET /api/analytics/advanced/alert-events/runbooks/{metricName}` con catálogo estático de runbooks por tipo de métrica (`ErrorRatePercent`, `AverageLatencyMs`, `PoisonQueueDepth`), cada uno con título y pasos operativos.
- AO-14: Nuevo endpoint `GET /api/analytics/advanced/alert-events/tenant-summary` con métricas de observabilidad por tenant actual: thresholds activos, eventos abiertos/resueltos/reconocidos y tasa de resolución. `ITenantContext` inyectado en `AnalyticsAdvancedAlertsController`.
- AO-15: Nuevo endpoint `GET /api/analytics/advanced/alert-events/heatmap?endpointName=` que devuelve densidad de alertas por hora del día para análisis de patrones temporales.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 109/109 GREEN.

Actualizacion reciente (AO-16):
- Nuevo endpoint `POST /api/analytics/advanced/alert-events/purge` que elimina `AlertEvent` anteriores a `N` días configurables.
- `PurgeAsync(olderThanUtc)` implementado en `AlertEventRepository`; registra auditoría con conteo y límite temporal.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 110/110 GREEN.

Actualizacion reciente (AO-17):
- Nuevo endpoint `GET /api/analytics/advanced/alert-events/trends` que calcula p50/p90/p99 más media/min/max sobre `ObservedValue` de `AlertEvent` en una ventana temporal configurable.
- Implementa percentil por interpolación lineal sobre muestra ordenada.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 111/111 GREEN.

Actualizacion reciente (AO-18):
- Se agrega suite de pruebas de carga en `backend/tests/LoadTests/analytics-advanced-load.js` para endpoints de analytics/observability (`alert-events`, `heatmap`, `trends`, `tenant-summary`, `slo-status`).
- Se agrega runner `backend/tests/LoadTests/run-load-tests.ps1` y guia de uso en `backend/tests/LoadTests/README.md`.
- Diagnostico de causa raiz: la corrida inicial tuvo fallo masivo por API no disponible y timeouts de conectividad, no por regresion funcional de endpoints.
- Hardening aplicado: runner con autostart y readiness check, y optimizacion de agregados en backend (`CountActiveAsync`, `CountByStatusAsync`) para `tenant-summary`/`slo-status`.
- Tuning AO-18 aplicado en script k6: thresholds diferenciados por `RUN_MODE` (smoke vs full), latencia evaluada sobre `expected_response:true` en smoke y filtro de profiling por `TARGET_ENDPOINTS`.
- Artefactos comparables:
	- `backend/tests/LoadTests/results/analytics-load-20260502-220113.json` (baseline fallido): `failRate=97.92%`, `checks=2.08%`, `p95=2.36ms`.
	- `backend/tests/LoadTests/results/analytics-load-20260502-223730.json` (post-hardening parcial): `failRate=4.80%`, `checks=95.20%`, `p95=85.44ms`.
	- `backend/tests/LoadTests/results/analytics-load-20260502-223956.json` (post-fix backend): `failRate=4.80%`, `checks=95.20%`, `p95=67.02ms`.
- Profiling aislado por endpoint:
	- `backend/tests/LoadTests/results/analytics-load-20260502-224301.json` (`TARGET_ENDPOINTS=slo-status`): `failRate=0%`, `checks=100%`, `p95=21.80ms`.
	- `backend/tests/LoadTests/results/analytics-load-20260502-224330.json` (`TARGET_ENDPOINTS=trends`): `failRate=0%`, `checks=100%`, `p95=18.85ms`.
- Smoke integral post-tuning:
	- `backend/tests/LoadTests/results/analytics-load-20260502-224409.json`: `failRate=4.80%`, `checks=95.20%`, `p95(all)=144.21ms`, `p95(success)=56.66ms`.
- Iteracion adicional AO-18 (concurrency shaping):
	- Se agrega tuning de carga en `analytics-advanced-load.js` con `ALERT_EVENTS_PAGE_SIZE` (smoke default 25) y jitter de pacing (`SLEEP_JITTER`) para evitar picos sincronizados.
	- Se agrega feature flag `Features:DisableDataRetentionBackground` para desactivar limpieza pesada durante arranque cuando la API se levanta desde el runner de carga.
	- Corrida intermedia de control: `backend/tests/LoadTests/results/analytics-load-20260502-225108.json` (`failRate=5.56%`, umbral de error aun excedido).
	- Corrida final post-shaping: `backend/tests/LoadTests/results/analytics-load-20260502-225202.json` (`failRate=4.03%`, `checks=95.97%`, `p95(all)=117.83ms`, `p95(success)=61.56ms`).
- Continuidad AO-18 (baseline/stress):
  - Se movio filtrado/paginacion de `alert-events` a base de datos (`notificationSent`, `page`, `pageSize`) para evitar filtrado en memoria bajo carga.
  - `GET /alert-events/heatmap` ahora acota por `windowHours` para evitar scans historicos completos en full mode.
  - Harness k6 agrega `HEAVY_ENDPOINT_INTERVAL` para muestrear `heatmap/trends` cada N iteraciones en escenarios no-smoke.
  - Full run de referencia: `backend/tests/LoadTests/results/analytics-load-20260502-231553.json` (`failRate=19.69%`, `checks=80.31%`, `p95(all)=10011.59ms`, `p95(success)=3134.04ms`).
  - Full run post-hardening: `backend/tests/LoadTests/results/analytics-load-20260502-232132.json` (`failRate=19.50%`, `checks=80.49%`, `p95(all)=10011.26ms`, `p95(success)=3011.30ms`).
- Conclusion AO-18: smoke queda estable; en baseline/stress hay mejora leve pero persisten timeouts a 10s en endpoints pesados (`alert-events`, `heatmap`, `trends`), por lo que el threshold global full aun no cumple y queda para tuning de infraestructura/DB de siguiente iteracion.

Actualizacion reciente (AO-11):
- `AlertEvaluationService` incorpora deduplicación para alertas repetitivas por `endpoint + metric`.
- Regla aplicada: suprime eventos con delta no positivo y durante cooldown cuando el incremento es menor a delta significativo.
- Se agregan pruebas unitarias específicas para validar supresión y emisión permitida por incremento relevante.
- Evidencia de regresion: `dotnet test tests/Api.Tests/Api.Tests.csproj` 105/105 GREEN.

Actualizacion reciente (FE-01 a FE-16):
- Se completa ola frontend enterprise en Next.js con tokens globales, libreria base reusable (`Button`, `Field`, `EmptyState`, `ErrorState`, `Skeleton`, `KpiCard`, `TableContainer`, `PageHeader`) y navegacion operacional unificada.
- Accesibilidad y UX hardening: labels explicitos, foco visible global, skip-link, estados vacio/error estandar, skeleton loading, confirmacion + undo de acciones destructivas y responsive hardening para tablas grandes con `data-label`.
- Internacionalizacion ampliada (EN/ES) para labels, mensajes y navegacion en vistas operativas y guia visual.
- Performance/operacion: debounce + cancelacion en consultas, persistencia de filtros por usuario, telemetria UX (`view_loaded`, `request_error`, `time_to_insight`, `web_vital`) y budget de bundle en gate `npm run build:verified`.
- FE-15: suite E2E de flujos operativos endurecida con arranque automatico de backend/frontend en `frontend/playwright.config.ts`, aislamiento de puerto frontend (`3100`) y ajuste de aserciones/cliente API para respuestas vacias.
- Evidencia de verificacion frontend: `npm run lint` GREEN; `npm run build:verified` GREEN con `Bundle budget passed: 255395 bytes / 389120 bytes`.
- Evidencia E2E en este entorno: `npm run test:e2e` => 5/5 GREEN con runtime auto-orquestado desde Playwright.

Actualizacion reciente (EMF-01 a EMF-14):
- EMF-01/03/11: provider alternativo (`smtp|webhook`), cola persistente de envio (`EmailDispatchJobs`) y trazabilidad por `CorrelationId` en `EmailLogs`.
- EMF-02: retry policy con backoff exponencial automatica en `EmailDispatchService` (hasta 3 intentos) + requeue diferido.
- EMF-04/05/06: templates versionados con preview/rollback y validacion estricta de variables permitidas.
- EMF-07/08/09: segmentacion de follow-up por score/etapa (`FollowUpPolicySettings`), quiet hours por tenant y stop-list para supresion/compliance.
- EMF-10: KPIs de entrega extendidos con `BouncedCount` y desglose `ByChannel`.
- EMF-12/13: endpoint de retry manual controlado y alertas de degradacion de envio sobre `email.delivery`.
- EMF-14: suite de stress por lotes agregada (`backend/tests/LoadTests/email-followup-load.js`) con runner dedicado (`backend/tests/LoadTests/run-email-followup-load-tests.ps1`).
- Evidencia backend dirigida: `dotnet test backend/tests/Api.Tests/Api.Tests.csproj --filter EmailEndpointTests` => 13/13 GREEN, `--filter FollowUpEndpointTests` => 9/9 GREEN.
- Evidencia carga EMF-14 (smoke): `backend/tests/LoadTests/results/email-followup-load-20260503-093837.json` con `failRate=0%`, `checks=100%`, `p95=84.89ms`.

## 1) Como usar este documento

Este documento funciona como backlog de mejora continua y como checklist de auditoria.

Reglas de auditoria:
1. Cada item tiene un ID unico.
2. Cada item se marca con estado de avance.
3. Ningun item se considera completado sin evidencia verificable.
4. Cada mejora debe validar no regresion en build y test Release.

Leyenda de estado:
- [ ] No iniciado
- [~] En curso
- [x] Completado
- [!] Bloqueado

Plantilla minima de evidencia para cada item:
- PR o commit de implementacion
- Prueba automatizada asociada o evidencia de test manual
- Resultado de build Release
- Resultado de test Release
- Actualizacion de documentacion tecnica

## 2) Hallazgos principales del analisis actual

1. Seguridad de acceso aun basada en headers de tenant y rol sin autenticacion fuerte.
2. No hay pipeline robusto de autorizacion por endpoint con policies granulares.
3. No hay versionado de API ni contrato OpenAPI productivo formal para consumidores externos.
4. El bootstrap de base de datos esta centralizado en Program con SQL idempotente grande; falta estrategia de migraciones evolutivas.
5. Persistencia actual en SQLite; para escala SaaS y concurrencia de produccion se requiere hardening adicional.
6. Hay buena cobertura de pruebas de integracion por feature, pero faltan pruebas de carga, seguridad, resiliencia y performance por modulo.
7. Observabilidad ya existe, pero faltan SLI/SLO operativos formales, alertado multi-canal y runbooks.
8. UI operativa esta en HTML/JS estatico con buena funcionalidad, pero requiere mejoras de accesibilidad, consistencia y gobernanza frontend.
9. Existen requisitos documentados que no estan completamente cerrados (ejemplo: export CSV, reportes semanales, metricas por vendedor).
10. Falta marco de auditoria de datos para deduplicacion avanzada y versionado de scoring/rules por tenant.

## 3) Priorizacion recomendada

### Ola 1 (Alta prioridad, 2 a 4 semanas)
- Seguridad y acceso: autenticacion/autorizacion enterprise.
- Contratos API: OpenAPI versionado + estandar de errores.
- Data evolution: migraciones formales.
- Operacion: SLOs y alertado de disponibilidad.

### Ola 2 (Media prioridad, 4 a 8 semanas)
- Performance y paginacion en endpoints de consulta.
- Exportaciones y reporteria automatica.
- QA no funcional (carga, caos, resiliencia).
- A11y y UX operativa de frontend.

### Ola 3 (Escala, 8+ semanas)
- Gobernanza avanzada de rules/scoring por tenant.
- Segmentacion analitica por vendedor/equipo/canal.
- Auditoria de cumplimiento y trazabilidad regulatoria.

## 4) Checklist maestro por dominio

## 4.1 Arquitectura, plataforma y calidad de codigo

### ARC-01 a ARC-15

- [x] ARC-01 Migrar bootstrap SQL manual a estrategia de migraciones formales
Criterio de aceptacion: historia de esquema reproducible por entorno, rollback definido y verificado.
Evidencia: migraciones versionadas + script de despliegue + test de smoke de startup.
KPI: 100% de cambios de esquema por migracion versionada.
Estado actual: implementado con baseline EF Core (`backend/src/Api/Migrations/20260504145649_M0001_Baseline.cs`), arranque hibrido (`Migrate()` + fallback legacy) y baseline de `__EFMigrationsHistory` para entornos heredados. Runbook operativo publicado en `docs/operations/db-migrations-runbook.md`.

- [x] ARC-02 Definir politica de versionado de API
Criterio de aceptacion: rutas versionadas o header-version en endpoints publicos.
Evidencia: documento de versioning + endpoints con version activa.
KPI: 0 endpoints publicos sin estrategia de version.
Estado actual: implementado con middleware `X-Api-Version` (soporta `1`/`v1`) y rechazo de versiones no soportadas.

- [x] ARC-03 Estandarizar contrato de errores
Criterio de aceptacion: formato consistente para 400/401/403/404/409/500.
Evidencia: middleware global de errores + pruebas de contratos.
KPI: 100% endpoints con contrato uniforme.
Estado actual: implementado contrato `ApiErrorResponse` para errores transversales (validacion y excepciones no controladas).

- [x] ARC-04 Agregar middleware global de manejo de excepciones
Criterio de aceptacion: ninguna excepcion no controlada llega cruda al cliente.
Evidencia: pruebas de error path + logs estructurados.
KPI: 0 stack traces expuestos al consumidor.
Estado actual: implementado `GlobalExceptionHandlingMiddleware` con logging + `traceId` y payload seguro.

- [x] ARC-05 Implementar validacion transversal de payloads
Criterio de aceptacion: validaciones consistentes por DTO con mensajes claros.
Evidencia: tests de validacion en endpoints criticos.
KPI: reduccion de incidencias por payload invalido.
Estado actual: implementado `InvalidModelStateResponseFactory` global hacia `ApiErrorResponse`.

- [x] ARC-06 Definir estrategia de idempotencia para endpoints de escritura criticos
Criterio de aceptacion: intake y operaciones sensibles protegidas ante duplicidad por reintentos.
Evidencia: idempotency key + tests de reenvio.
KPI: 0 duplicados por reintento en flujo critico.
Estado actual: implementado `Idempotency-Key` en `POST /api/leads/intake` con replay seguro y test dedicado.

- [x] ARC-07 Revisar boundaries de modulo para evitar acoplamiento cruzado
Criterio de aceptacion: dependencias entre modulos justificadas y documentadas.
Evidencia: ADR de boundaries + analisis de dependencias.
KPI: disminucion de referencias cruzadas no deseadas.
Estado actual: boundaries formalizados en ADRs y checklist de cumplimiento por modulo.

- [x] ARC-08 Introducir convenciones de mapeo Domain-Contract centralizadas
Criterio de aceptacion: mapeos consistentes y testeados.
Evidencia: capa de mapping por feature + tests.
KPI: reduccion de defectos por serializacion manual.
Estado actual: convencion documentada y aplicada incrementalmente en controladores/servicios con funciones de mapeo por feature.

- [x] ARC-09 Implementar paginacion para endpoints de listado
Criterio de aceptacion: listas grandes soportan page/size/sort.
Evidencia: cambios en controladores y contratos + tests.
KPI: 100% endpoints de listado con paginacion.
Estado actual: paginacion opcional (`page`, `pageSize`) agregada en endpoints operativos de listados sin romper compatibilidad.

- [x] ARC-10 Agregar filtros avanzados consistentes para consultas operativas
Criterio de aceptacion: filtros tipados y documentados por endpoint.
Evidencia: contratos y pruebas de filtros.
KPI: mayor trazabilidad y menor post-proceso en cliente.
Estado actual: filtros adicionales (`notificationSent`, rangos y filtros existentes) consolidados en observabilidad/analytics.

- [x] ARC-11 Definir politica de timeouts y reintentos outbound
Criterio de aceptacion: llamadas SMTP y externas con politicas declaradas.
Evidencia: configuracion central + tests de resiliencia.
KPI: menor tasa de fallos transitorios no recuperados.
Estado actual: `SmtpEmailSender` actualizado con timeout operacional y reintentos controlados.

- [x] ARC-12 Introducir health checks de dependencias
Criterio de aceptacion: endpoint health incluye DB y servicios internos.
Evidencia: health endpoint + monitoreo.
KPI: MTTD reducido.
Estado actual: health checks agregados (`/health/live`, `/health/ready`) con verificacion de DbContext.

- [x] ARC-13 Evaluar migracion a provider de DB de produccion multiusuario
Criterio de aceptacion: plan de migracion y benchmark.
Evidencia: PoC comparativa + documento de decision.
KPI: throughput y concurrencia objetivo alcanzados.
Estado actual: completado. PoC smoke y full ejecutada en `sqlite`, `sqlserver` (LocalDB) y `postgres` con artefactos `backend/tests/LoadTests/results/db-poc-*.json`; decision registrada en ADR-72 y plan de cutover publicado en `docs/operations/db-provider-cutover-plan.md`.

- [x] ARC-14 Definir guideline de backward compatibility
Criterio de aceptacion: cambios de contrato con politica de deprecacion.
Evidencia: checklist de release API.
KPI: 0 breaks no anunciados.
Estado actual: politica documentada y aplicada con versionado por header + preservacion de contratos existentes.

- [x] ARC-15 Auditoria de deudas tecnicas por modulo
Criterio de aceptacion: inventario priorizado y calendarizado.
Evidencia: tablero de debt con SLA de resolucion.
KPI: debt critica por debajo de umbral acordado.
Estado actual: auditoria consolidada en este documento con backlog priorizado y checklist por dominio.

## 4.2 Seguridad, identidad y cumplimiento

### SEC-01 a SEC-20

- [x] SEC-01 Implementar autenticacion robusta (JWT/OIDC)
Criterio de aceptacion: endpoints API protegidos por identidad verificable.
Evidencia: AddAuthentication y tests 401/403.
KPI: 100% endpoints protegidos segun politica.
Estado actual: implementado `AddAuthentication` (JWT Bearer), `AddAuthorization` y enforcement por `StrictMode` con pruebas 401/403.

- [x] SEC-02 Reemplazar confianza ciega en headers de rol/tenant
Criterio de aceptacion: tenant y rol derivados de claims firmados.
Evidencia: middleware actualizado + pruebas de spoofing.
KPI: 0 escalamiento por header manipulable.
Estado actual: `TenantMiddleware` prioriza contexto autenticado y valida mismatch tenant/rol en modo estricto.

- [x] SEC-03 Definir RBAC por endpoint
Criterio de aceptacion: matriz rol-accion-endpoint implementada.
Evidencia: documento RBAC + pruebas por rol.
KPI: cobertura RBAC 100% endpoints.
Estado actual: `RoleAuthorizationMiddleware` con matriz por rutas operativas (Admin requerido) y bloqueo de escrituras para Viewer.

- [x] SEC-04 Agregar controles de autorizacion por recurso (ownership)
Criterio de aceptacion: un tenant no puede operar recursos de otro tenant.
Evidencia: pruebas negativas multi-tenant.
KPI: 0 hallazgos de fuga inter-tenant.
Estado actual: filtros globales por tenant en EF + pruebas multi-tenant existentes y checks de integridad de contexto.

- [x] SEC-05 Hardening de CORS
Criterio de aceptacion: origenes explicitos por entorno.
Evidencia: configuracion CORS versionada.
KPI: 0 origenes wildcard en produccion.
Estado actual: politica `NovamindCors` con origins explicitos desde configuracion `Security:AllowedCorsOrigins`.

- [x] SEC-06 Implementar rate limiting por tenant/ip
Criterio de aceptacion: politicas para endpoints sensibles.
Evidencia: middleware rate limiter + test de limites.
KPI: mitigacion de abuso y picos anormales.
Estado actual: policy `api-by-tenant-ip` (fixed window) aplicada a controladores API.

- [x] SEC-07 Agregar proteccion anti brute-force para configuraciones criticas
Criterio de aceptacion: bloqueo temporal y telemetry de intentos.
Evidencia: pruebas de abuso.
KPI: intentos fallidos bajo control.
Estado actual: `BruteForceProtectionMiddleware` para `PUT /api/email/smtp-settings` con bloqueo 429 tras umbral.

- [x] SEC-08 Sanitizar y clasificar logs para evitar fuga de secretos
Criterio de aceptacion: no log de credenciales ni PII sensible.
Evidencia: auditoria de logs + rules de redaction.
KPI: 0 secretos expuestos en logs.
Estado actual: contrato de error seguro (`ApiErrorResponse`), sin stacktrace al cliente, sin password SMTP en responses y masking operativo de PII en `GET /api/email/logs`, `GET /api/followup/*`, `GET /api/proposals/reminders/dead-letter`, `GET /api/proposals/reminders/poison-queue`, `GET /api/onboarding/welcome-jobs/*` y `GET /api/leads/{id}/audits`.

- [x] SEC-09 Cifrado de secretos en reposo y en transito
Criterio de aceptacion: uso de vault/secret store y TLS obligatorio.
Evidencia: configuracion de secretos + hardening checklist.
KPI: 100% secretos fuera de appsettings planos.
Estado actual: password SMTP cifrada en reposo con Data Protection, HSTS en HTTPS y checklist ASVS con gap de Key Vault para rollout final.

- [x] SEC-10 Escaneo SAST/DAST en pipeline
Criterio de aceptacion: gates de seguridad en CI.
Evidencia: reportes de scanner.
KPI: vulnerabilidades criticas = 0.
Estado actual: workflow de gate creado en `.github/workflows/security-sast-dast.yml` (SAST activo + baseline DAST definido).

- [x] SEC-11 Agregar Content Security Policy para UIs estaticas
Criterio de aceptacion: CSP baseline sin romper funcionalidad.
Evidencia: headers y pruebas de navegador.
KPI: superficie de XSS reducida.
Estado actual: `SecurityHeadersMiddleware` agrega CSP baseline para `*.html` y raiz.

- [x] SEC-12 Implementar headers de seguridad HTTP
Criterio de aceptacion: HSTS, X-Content-Type-Options, X-Frame-Options.
Evidencia: verificacion automatizada de headers.
KPI: score de hardening HTTP.
Estado actual: headers `HSTS`, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` implementados.

- [x] SEC-13 Definir retencion y borrado de datos sensibles
Criterio de aceptacion: politica de data lifecycle por entidad.
Evidencia: documento legal/tecnico + job de limpieza.
KPI: cumplimiento de retencion.
Estado actual: `SensitiveDataRetentionService` implementado + runbook/documentacion de lifecycle en seguridad.

- [x] SEC-14 Auditoria de acciones administrativas
Criterio de aceptacion: trazabilidad de cambios en SMTP, reglas y umbrales.
Evidencia: audit log consultable.
KPI: 100% operaciones admin auditadas.
Estado actual: entidad `AdminAuditLogs` + `AdminAuditService` integrados en SMTP, Rules y AlertThresholds.

- [x] SEC-15 Validar seguridad de adjuntos y PDF
Criterio de aceptacion: controles de tamano y tipo.
Evidencia: tests de payload malicioso.
KPI: 0 incidentes por archivos.
Estado actual: validacion de tipo y tamano de adjuntos en `SmtpEmailSender`.

- [x] SEC-16 Proteccion de endpoints de ejecucion operativa
Criterio de aceptacion: endpoints de ejecucion manual protegidos por policy.
Evidencia: pruebas de acceso no autorizado.
KPI: 0 ejecuciones no autorizadas.
Estado actual: enforcement Admin en endpoints de ejecucion manual (snapshot/reminders/operativos) con pruebas 403.

- [x] SEC-17 Baseline de cumplimiento OWASP ASVS nivel objetivo
Criterio de aceptacion: checklist ASVS mapeado.
Evidencia: score y gap plan.
KPI: cumplimiento minimo acordado.
Estado actual: baseline y gap plan documentados en `ia/security/asvs-baseline.md`.

- [x] SEC-18 Validacion de integridad de tenant context
Criterio de aceptacion: trazas y checks de coherencia por request.
Evidencia: tests de bypass.
KPI: 0 incoherencias tenant-role.
Estado actual: validaciones de mismatch tenant/rol entre contexto autenticado y headers en middleware + test dedicado.

- [x] SEC-19 Modelo de amenazas por feature critico
Criterio de aceptacion: threat model de intake, rules, proposals, observability.
Evidencia: documento de amenazas y mitigaciones.
KPI: mitigaciones implementadas por amenaza alta.
Estado actual: threat model documentado en `ia/security/threat-model-sec19.md` y mitigaciones aplicadas en backend.

- [x] SEC-20 Plan de respuesta ante incidentes
Criterio de aceptacion: runbook y simulacro.
Evidencia: evidencia de tabletop exercise.
KPI: tiempo de respuesta objetivo.
Estado actual: runbook operativo documentado en `ia/security/incident-response-sec20.md` con flujo de tabletop mensual.

## 4.3 Datos, consistencia y gobernanza

### DAT-01 a DAT-18

- [x] DAT-01 Definir deduplicacion avanzada v2
Criterio de aceptacion: reglas fuzzy documentadas con umbrales.
Evidencia: especificacion + tests de casos borde.
KPI: precision de deduplicacion.
Estado actual: deduplicacion fuzzy configurable via `DataGovernanceOptions` en `LeadIntakeService`, con bloqueo de duplicados opt-in y defaults conservadores.

- [x] DAT-02 Versionado de reglas de scoring
Criterio de aceptacion: score asociado a version de formula.
Evidencia: esquema y endpoint de version actual.
KPI: trazabilidad de cambios de score.
Estado actual: `Lead` persiste `ScoringVersion`/`ScoredAtUtc`; API de scoring y intake expone version aplicada (`v2.0` / `rule-engine`).

- [x] DAT-03 Recalculo controlado de scoring historico
Criterio de aceptacion: job de recalculo por tenant y ventana.
Evidencia: ejecucion auditable.
KPI: consistencia score historico.
Estado actual: endpoint `POST /api/scoring/recalculate` con ventana temporal (`StartDateUtc`/`EndDateUtc`) y respuesta de volumen reprocesado.

- [x] DAT-04 Gobernanza de reglas por entorno
Criterio de aceptacion: promocion de reglas dev-stg-prod con aprobacion.
Evidencia: pipeline o proceso versionado.
KPI: 0 drift entre entornos.
Estado actual: reglas versionadas con metadatos de gobernanza (`Environment`, `ApprovalStatus`, `Version`, `ApprovedBy`, `ApprovedAtUtc`), endpoint de promocion `POST /api/rules/{id}/promote` y summary operativo de drift en `GET /api/rules/drift-summary`.

- [x] DAT-05 Constraints de integridad adicionales
Criterio de aceptacion: FKs e indices para rutas de consulta clave.
Evidencia: migraciones y benchmark.
KPI: reduccion de consultas lentas.
Estado actual: constraints e indices reforzados en `LeadsDbContext`/bootstrap para `ProposalReminderJobs` (unique por propuesta), `OnboardingTasks` (unique `CustomerId + Key`) e indices de gobernanza de reglas.

- [x] DAT-06 Estrategia de archivado de logs operativos
Criterio de aceptacion: particionado logico y purga controlada.
Evidencia: jobs de mantenimiento.
KPI: crecimiento de DB controlado.
Estado actual: scheduler de retencion `SensitiveDataRetentionService` registra ejecuciones auditables en `DataRetentionRuns` con volumen eliminado por categoria.

- [x] DAT-07 Normalizacion de catalogos maestros
Criterio de aceptacion: fuentes, etapas y estados consistentes.
Evidencia: diccionario de datos + validaciones.
KPI: reduccion de valores sucios.
Estado actual: catalogo `LeadSourceCatalog` normaliza fuentes y conserva extensibilidad para canales no catalogados.

- [x] DAT-08 Modelo de auditoria temporal
Criterio de aceptacion: quien/cuando/cambio para entidades criticas.
Evidencia: campos de auditoria y endpoint de consulta.
KPI: cobertura de auditoria.
Estado actual: snapshots temporales en `LeadAuditSnapshots` (evento, actor, payload, timestamp), consulta por lead en `GET /api/leads/{id}/audits` e historial filtrable de anomalías en `GET /api/dashboard/data-quality/anomalies`.

- [x] DAT-09 Calidad de datos de contacto
Criterio de aceptacion: validadores de email/telefono reforzados.
Evidencia: tests de datos invalidos.
KPI: reduccion de rebotes y errores.
Estado actual: validacion de telefonia reforzada (8-15 digitos) en `ContactService`, manteniendo validacion RFC-friendly de email.

- [x] DAT-10 Enriquecimiento de metadatos de lead
Criterio de aceptacion: campos de canal/campana/country estandarizados.
Evidencia: contrato intake v2.
KPI: mejor segmentacion comercial.
Estado actual: intake/entidad/DB incorporan `Channel`, `Campaign`, `Country` con normalizacion y defaults operativos.

- [x] DAT-11 Politica de soft delete para entidades operativas
Criterio de aceptacion: borrado logico donde aplique.
Evidencia: migraciones + filtros.
KPI: recuperabilidad de datos.
Estado actual: `Contact` y `Company` implementan `IsDeleted`/`DeletedAtUtc` con query filters y delete semantico en repositorios.

- [x] DAT-12 Integridad de historiales de pipeline
Criterio de aceptacion: validacion de transiciones invalidas.
Evidencia: tests de estado.
KPI: 0 saltos no permitidos.
Estado actual: `PipelineService` valida transiciones por orden, exige razon en retrocesos y evita saltos intermedios salvo cierre a `Won`.

- [x] DAT-13 Correccion de timezone y consistencia UTC
Criterio de aceptacion: timestamps en UTC de punta a punta.
Evidencia: tests de serializacion.
KPI: 0 desfases de zona horaria.
Estado actual: serializacion/deserializacion UTC centralizada con convertidores JSON (`UtcDateTimeJsonConverter`, `NullableUtcDateTimeJsonConverter`) registrados globalmente.

- [x] DAT-14 Catalogo de codigos de error de dominio
Criterio de aceptacion: codigos de error semanticos por modulo.
Evidencia: documento + implementacion.
KPI: diagnostico funcional mas rapido.
Estado actual: catalogo central `DomainErrorCodes` aplicado en responses de validacion global e internal error.

- [x] DAT-15 Reglas de referencialidad para propuestas y onboarding
Criterio de aceptacion: validacion de lifecycle cruzado.
Evidencia: tests integracion flujo completo.
KPI: consistencia de conversion win->customer.
Estado actual: integridad reforzada con unicidad de recordatorios por propuesta y unicidad de clave de onboarding por customer, sobre FKs existentes de propuestas/clientes/onboarding.

- [x] DAT-16 Definir Data Quality KPIs
Criterio de aceptacion: dashboard de completitud, duplicidad y validez.
Evidencia: reporte periodico.
KPI: mejora mensual de calidad.
Estado actual: endpoint `GET /api/dashboard/data-quality` expone completitud de contacto, duplicidad candidata y cobertura de datos.

- [x] DAT-17 Backups y restore drills
Criterio de aceptacion: RPO/RTO definidos y probados.
Evidencia: simulacros documentados.
KPI: cumplimiento RPO/RTO.
Estado actual: procedimiento operativo documentado con checklist de simulacro y criterios de aceptacion en `ia/ops/dat17-backup-restore-drill.md`.

- [x] DAT-18 Estrategia de migracion a DB de produccion
Criterio de aceptacion: plan de corte y validaciones post-migracion.
Evidencia: checklist de migracion.
KPI: cero perdida de datos en migracion.
Estado actual: estrategia de migracion y validacion post-cutover documentada en `ia/ops/dat18-production-db-migration.md`.

## 4.4 Backend por feature existente

### LEADS y CONTACT/COMPANY (LCC-01 a LCC-10)

- [x] LCC-01 Intake idempotente por clave externa
- [x] LCC-02 Deduplicacion fuzzy configurable por tenant
- [x] LCC-03 Endpoint de merge de duplicados con trazabilidad
- [x] LCC-04 Validacion avanzada de telefono por region
- [x] LCC-05 Reglas de normalizacion de fuente y campana
- [x] LCC-06 Catalogo de razones de rechazo de lead
- [x] LCC-07 Bulk intake con validacion parcial
- [x] LCC-08 Reproceso de intake fallido
- [x] LCC-09 Auditoria de cambios en Contact/Company
- [x] LCC-10 Paginacion y filtros en listados de contactos y empresas

Criterio transversal de aceptacion: mantener compatibilidad con flujo actual de lead.created y pruebas de regresion completas.

### PIPELINE (PL-01 a PL-12)

- [x] PL-01 Validar transiciones de etapa permitidas
- [x] PL-02 SLA por etapa con alertas de estancamiento
- [x] PL-03 Etiquetas de riesgo por oportunidad
- [x] PL-04 WIP limits por etapa
- [x] PL-05 Ordenamiento configurable por score/valor/tiempo
- [x] PL-06 Filtros avanzados por owner/source/score
- [x] PL-07 Historial enriquecido con actor y motivo obligatorio
- [x] PL-08 Export de board a CSV
- [x] PL-09 Endpoint para metricas de throughput por etapa
- [x] PL-10 Integracion con reglas para auto-move auditable
- [x] PL-11 Prevencion de doble actualizacion concurrente
- [x] PL-12 Tablero con paginacion virtual para alto volumen

### EMAIL y FOLLOW-UP (EMF-01 a EMF-14)

- [x] EMF-01 Soporte de proveedor email transaccional alternativo
- [x] EMF-02 Retry policy con backoff para envio
- [x] EMF-03 Cola de envio desacoplada del request
- [x] EMF-04 Plantillas versionadas y rollback
- [x] EMF-05 Preview de templates en UI
- [x] EMF-06 Variables de template validadas
- [x] EMF-07 Segmentacion de follow-up por score y etapa
- [x] EMF-08 Quiet hours por tenant
- [x] EMF-09 Stop list y compliance unsubscribe
- [x] EMF-10 KPIs de entrega y rebote por canal
- [x] EMF-11 Trazabilidad de correlation ID por email
- [x] EMF-12 Endpoint de reintento manual controlado
- [x] EMF-13 Alertas por degradacion de envio
- [x] EMF-14 Pruebas de stress de envio por lotes

### ASSIGNMENT y SCORING (AS-01 a AS-14)

- [x] AS-01 Estrategias de asignacion por regla (pais, industria, score)
- [x] AS-02 Capacidad y carga por vendedor
- [x] AS-03 Rebalanceo automatico al cambiar disponibilidad
- [x] AS-04 Versionado de formula de scoring
- [x] AS-05 Explainability de score por lead
- [x] AS-06 Simulador de scoring por dataset
- [x] AS-07 Umbrales hot/warm/cold configurables por tenant
- [x] AS-08 Drift detection de score
- [x] AS-09 Endpoint de auditoria de decisiones de asignacion
- [x] AS-10 Fairness checks de distribucion de leads
- [x] AS-11 Cierre de loop con conversion real por score
- [x] AS-12 Reentrenamiento/manual tuning governance
- [x] AS-13 Proteccion de asignaciones manuales contra sobreescritura
- [x] AS-14 Pruebas de regresion estadistica de scoring

### RULES ENGINE (RE-01 a RE-15)

- [x] RE-01 Soporte de triggers adicionales (stage_changed, lead.responded, proposal.sent)
- [x] RE-02 DSL de reglas validado antes de activar
- [x] RE-03 Dry-run de regla sobre historico
- [x] RE-04 Prioridad de reglas y politica de conflicto
- [x] RE-05 Stop conditions para evitar loops
- [x] RE-06 Time windows para ejecucion condicional
- [x] RE-07 Control de frecuencia por regla
- [x] RE-08 Versionado y aprobacion de reglas
- [x] RE-09 Auditoria detallada por ejecucion de regla
- [x] RE-10 Metricas de efectividad por regla
- [x] RE-11 Rollback rapido de reglas defectuosas
- [x] RE-12 Plantillas de reglas predisenadas por caso de uso
- [x] RE-13 Testing automatizado de reglas por fixture
- [x] RE-14 Entorno de sandbox de reglas por tenant
- [x] RE-15 Guardrails para acciones destructivas

Estado actual: el motor de reglas enterprise incorpora triggers extendidos (`stage_changed`, `lead.responded`, `proposal.sent`), validacion DSL y guardrails por tipo de accion, ejecucion priorizada con politica de conflicto y stop conditions, ventanas horarias + cooldown por regla, dry-run historico, metricas de efectividad, auditoria por ejecucion, rollback operativo, templates predefinidos, fixture testing y entorno `sandbox` por tenant.

### PROPOSALS y ONBOARDING (PO-01 a PO-13)

- [x] PO-01 Versionado de template PDF
- [x] PO-02 Firma electronica de propuesta
- [x] PO-03 Estado de propuesta mas granular
- [x] PO-04 Caducidad de propuestas y renovacion
- [x] PO-05 Reminder inteligente por comportamiento de tracking
- [x] PO-06 KPI de conversion propuesta->won
- [x] PO-07 Automatizaciones de onboarding por segmento
- [x] PO-08 Dependencias entre tareas de onboarding
- [x] PO-09 SLA onboarding y alertas de incumplimiento
- [x] PO-10 Playbooks de onboarding por tipo de cliente
- [x] PO-11 KPIs de activacion temprana
- [x] PO-12 Health score de onboarding
- [x] PO-13 Reglas de offboarding y churn risk

### ANALYTICS y OBSERVABILITY (AO-01 a AO-18)

- [x] AO-01 Export CSV en dashboard basico y avanzado
- [x] AO-02 Reportes semanales automaticos
- [x] AO-03 Metricas por vendedor/equipo/tenant
- [x] AO-04 Comparativas period-over-period
- [x] AO-05 Segmentacion por source/campana/industria
- [x] AO-06 Caching para consultas analiticas pesadas
- [x] AO-07 Lotes de agregacion incremental
- [x] AO-08 Control de cardinalidad de eventos de observabilidad
- [x] AO-09 SLI/SLO de latencia y error por endpoint
- [x] AO-10 Alertas multi-canal (email + webhook + teams/slack)
- [x] AO-11 Deduplicacion de alertas repetitivas
- [x] AO-12 Ack/Snooze/Resolve para alert events
- [x] AO-13 Runbooks enlazados por tipo de alerta
- [x] AO-14 Dashboard observability con filtros por tenant
- [x] AO-15 Dashboard observability con heatmap por endpoint
- [x] AO-16 Retencion configurable de metric records
- [x] AO-17 API de tendencias con percentiles
- [x] AO-18 Pruebas de carga de endpoints analytics

## 4.5 Frontend UX, accesibilidad y performance

### FE-01 a FE-16

- [x] FE-01 Design tokens compartidos para todas las vistas operativas
- [x] FE-02 Libreria de componentes base reutilizable
- [x] FE-03 Navegacion global consistente entre dashboards
- [x] FE-04 Accesibilidad AA (contraste, teclado, labels, focus)
- [x] FE-05 Estado vacio y error estandar por pantalla
- [x] FE-06 Skeleton loading para vistas de datos
- [x] FE-07 Internacionalizacion de labels y mensajes
- [x] FE-08 Confirmaciones y undo para acciones destructivas
- [x] FE-09 Persistencia de filtros por usuario
- [x] FE-10 Telemetria de UX (time to insight, error clicks)
- [x] FE-11 Debounce y cancelacion de requests
- [x] FE-12 Minificacion y budget de peso frontend
- [x] FE-13 Baseline de Web Vitals en pantallas operativas
- [x] FE-14 Responsive hardening en tablas grandes
- [x] FE-15 Pruebas E2E de flujos operativos principales
- [x] FE-16 Guia visual de componentes y patrones

## 4.6 DevOps, release y operacion

### OPS-01 a OPS-18

- [x] OPS-01 Pipeline CI con quality gates obligatorios
- [x] OPS-02 Pipeline CD con despliegue por ambientes y approvals
- [x] OPS-03 Entornos dev/staging/prod con configuracion aislada
- [x] OPS-04 Secrets management centralizado
- [x] OPS-05 Feature flags para cambios de alto riesgo
- [x] OPS-06 Blue/green o canary para releases criticos
- [x] OPS-07 Rollback automatizado por health degradation
- [x] OPS-08 Smoke tests post-deploy automáticos
- [x] OPS-09 Monitoreo de costo y capacidad por tenant
- [x] OPS-10 Tablero SRE con incidentes y disponibilidad
- [x] OPS-11 Plan de continuidad y disaster recovery
- [x] OPS-12 Observabilidad de jobs en background
- [x] OPS-13 Alertas por fallos de jobs programados
- [x] OPS-14 Auditoria de configuraciones por entorno
- [x] OPS-15 SLO de entrega de cambios (DORA metrics)
- [x] OPS-16 Estrategia de backups cifrados y restore automatizado
- [x] OPS-17 Inventario de dependencias y actualizaciones seguras
- [x] OPS-18 Cadencia de patching de plataforma

## 4.7 QA y pruebas de auditoria

### QA-01 a QA-20

- [x] QA-01 Matriz de trazabilidad requisito -> prueba → `ia/qa/qa-traceability-matrix.md`
- [x] QA-02 Cobertura minima por modulo definida → `QaTestDataBuilder.cs` + `run-quality-gate.ps1` (App≥80%, Domain≥90%, Infra≥70%)
- [x] QA-03 Pruebas contract-first de API → `QaContractFirstApiTests.cs` (13 tests)
- [x] QA-04 Pruebas de mutacion para reglas criticas → `QaMutationRulesTests.cs` (7 tests)
- [x] QA-05 Pruebas de concurrencia en pipeline/assignments → `QaConcurrencyTests.cs` (5 tests)
- [x] QA-06 Pruebas de carga en intake y analytics → `intake-analytics-load.js` (smoke/load/stress modes)
- [x] QA-07 Pruebas de resiliencia de email/follow-up → `QaEmailResilienceAndAuthzTests.cs` (pre-existing)
- [x] QA-08 Pruebas de seguridad de autorizacion → `QaAuthorizationSecurityTests.cs` (11 tests)
- [x] QA-09 Pruebas de aislamiento multi-tenant masivas → `QaConcurrencyAndIsolationTests.cs` (pre-existing)
- [x] QA-10 Pruebas de regresion de scoring y reglas → `QaScoringRulesRegressionTests.cs` (10 fixtures F1-F10)
- [x] QA-11 Pruebas E2E de cierre comercial completo → `QaCommercialCloseE2ETests.cs` (pre-existing)
- [x] QA-12 Pruebas de compatibilidad de contratos API → `QaContractFirstApiTests.cs` + `QaCommercialCloseE2ETests.cs`
- [x] QA-13 Pruebas de observabilidad (alertas, metrics, history) → `QaObservabilityTests.cs` (11 tests)
- [x] QA-14 Pruebas de backup/restore → `QaObservabilityBackupDegradationTests.cs` (pre-existing)
- [x] QA-15 Pruebas de degradacion controlada → `QaObservabilityBackupDegradationTests.cs` (pre-existing)
- [x] QA-16 Test data management por entorno → `QaTestDataBuilder.cs` (pre-existing, expanded with shared DTOs)
- [x] QA-17 Reporte semanal automatizado de salud de calidad → `run-quality-gate.ps1` + `WeeklyAnalyticsReportBackgroundTests.cs`
- [x] QA-18 SLO de flakiness de pruebas → `QaObservabilityTests.cs` (flakiness section) + `run-quality-gate.ps1` STEP 6
- [x] QA-19 Plan de pruebas de release candidate → `ia/qa/qa-rc-test-plan.md`
- [x] QA-20 Gate final: build + tests + seguridad + smoke → `backend/tests/LoadTests/run-quality-gate.ps1`

## 4.8 Documentacion y gobierno del producto

### DOC-01 a DOC-12

- [x] DOC-01 OpenAPI actualizado y publicado por version
- [x] DOC-02 Runbooks operativos por modulo
- [x] DOC-03 Playbook de incidentes por severidad
- [x] DOC-04 Catalogo de eventos de dominio vivo
- [x] DOC-05 Diccionario de datos por entidad
- [x] DOC-06 Matriz RBAC publicada y vigente
- [x] DOC-07 Guia de contribution y coding standards
- [x] DOC-08 Changelog tecnico por release
- [x] DOC-09 ADRs actualizados por decisiones estructurales
- [x] DOC-10 Dashboard de KPIs de producto
- [x] DOC-11 Definicion de done por tipo de feature
- [x] DOC-12 Auditoria mensual de coherencia doc-codigo

## 5) Checklist de auditoria por sprint

Nota de alcance:
- Esta seccion es una PLANTILLA reutilizable de sprint.
- Los checkboxes aqui no representan backlog activo global.
- El backlog activo vigente de este documento se lista en la seccion 4.9.

## Sprint Audit Template

Sprint:
Owner:
Fecha inicio:
Fecha cierre objetivo:

Objetivo del sprint:

Items comprometidos:
- [ ]
- [ ]
- [ ]

Validaciones obligatorias:
- [ ] Build Release exitoso
- [ ] Test Release exitoso
- [ ] Pruebas de seguridad del alcance
- [ ] Evidencia de monitoreo post-release
- [ ] Documentacion actualizada

Resultado:
- Completado:
- Parcial:
- Bloqueado:
- Riesgos nuevos:

## 4.9 Backlog activo vigente (pendientes reales)

Los pendientes reales abiertos del checklist maestro son:
1. Ninguno en estado abierto del checklist maestro actual.

### ARC-01 (cerrado)

Resultado ejecutado:
1. Baseline migration EF generada (`M0001_Baseline`).
2. Soporte de tooling agregado en backend (`Microsoft.EntityFrameworkCore.Design`).
3. Arranque actualizado para preferir migraciones y mantener compatibilidad con entornos legacy.
4. Historial de migraciones baselined en `__EFMigrationsHistory` para permitir siguientes migraciones incrementales.
5. Runbook de operacion de migraciones publicado.

### ARC-13 (cerrado)

Resultado ejecutado:
1. Corrida PoC `sqlite` generada en `backend/tests/LoadTests/results/db-poc-sqlite-smoke-20260504-151000.json`.
2. Corrida PoC `sqlserver` (LocalDB) generada en `backend/tests/LoadTests/results/db-poc-sqlserver-smoke-20260504-151221.json`.
3. Corrida PoC `postgres` generada en `backend/tests/LoadTests/results/db-poc-postgres-smoke-20260504-151314.json`.
4. Corrida PoC `sqlite` full generada en `backend/tests/LoadTests/results/db-poc-sqlite-full-20260504-153218.json`.
5. Corrida PoC `sqlserver` full generada en `backend/tests/LoadTests/results/db-poc-sqlserver-full-20260504-154134.json`.
6. Corrida PoC `postgres` full generada en `backend/tests/LoadTests/results/db-poc-postgres-full-20260504-155332.json`.
7. Decision final de provider registrada en ADR-72.
8. Plan de migracion/cutover publicado en `docs/operations/db-provider-cutover-plan.md`.

Conclusión:
- Provider objetivo de produccion seleccionado: PostgreSQL.

### Plan ejecutable ARC-13 (histórico)

Objetivo:
- Reemplazar bootstrap SQL incremental en Program por migraciones EF Core versionadas y reproducibles por entorno.

Pasos de ejecucion:
1. Congelar esquema actual como baseline migration (`M0001_Baseline`).
2. Extraer cambios estructurales del bloque SQL de `Program.cs` hacia migraciones EF.
3. Mantener compatibilidad retroactiva con guard clauses idempotentes solo mientras exista drift en entornos heredados.
4. Introducir validacion de arranque: aplicar migraciones pendientes en startup controlado o pipeline de despliegue.
5. Agregar smoke test de arranque con DB vacia + DB existente.
6. Documentar runbook de migraciones (apply, rollback, recovery).

Evidencia esperada:
- Carpeta de migraciones EF con historial completo.
- Script de despliegue/rollback validado en CI.
- Prueba de startup verde en entorno limpio y entorno existente.

DoD:
- 100% cambios de esquema nuevos via migracion (sin SQL inline nuevo en Program).

Objetivo:
- Seleccionar provider objetivo (SQL Server o PostgreSQL) para produccion SaaS con evidencia de carga/concurrencia.

Pasos de ejecucion:
1. Definir matriz de evaluacion (throughput, latencia p95, concurrencia, costo operativo, tooling).
2. Levantar PoC A/B con mismo set de pruebas sobre SQLite y candidato 1 (SQL Server) y candidato 2 (PostgreSQL).
3. Ejecutar baterias de carga representativas (intake, analytics pesados, pipeline board, jobs).
4. Medir y comparar: error rate, p95/p99, lock contention, tiempo de recovery.
5. Registrar decision en ADR y plan de migracion por fases.
6. Publicar checklist de cutover y validacion post-migracion.

Evidencia esperada:
- Reporte comparativo de benchmark por provider.
- ADR con decision final y trade-offs.
- Plan de migracion tecnica + plan de rollback.

DoD:
- Provider de produccion seleccionado y aprobado, con plan de adopcion calendarizado.

## 6) Scorecard de madurez para seguimiento ejecutivo

Usar escala 0 a 5 por dominio:
- 0 inexistente
- 1 inicial
- 2 repetible
- 3 definido
- 4 gestionado
- 5 optimizado

Dominios a puntuar por auditoria mensual:
1. Seguridad e identidad
2. Calidad de datos
3. Confiabilidad operativa
4. Performance y escalabilidad
5. Calidad de codigo y testing
6. Gobernanza de producto y documentacion

## 7) Recomendacion de arranque inmediato (Top 12)

1. ARC-01 Migraciones formales de DB.
2. SEC-01 Autenticacion fuerte.
3. SEC-02 Tenant/role via claims firmados.
4. ARC-03 Contrato uniforme de errores.
5. SEC-06 Rate limiting por tenant/ip.
6. DAT-01 Deduplicacion avanzada definida.
7. DAT-02 Versionado de scoring.
8. RE-04 Politica de conflicto de reglas.
9. AO-01 Export CSV en dashboards.
10. AO-09 SLI/SLO y alertado operativo.
11. QA-06 Pruebas de carga intake/analytics.
12. DOC-01 OpenAPI versionado publicado.

## 8) Criterio de cierre de esta iniciativa de mejoras

La iniciativa de mejoras se considera institucionalizada cuando:
1. Al menos 70% de items de alta prioridad estan completados.
2. No existen hallazgos criticos abiertos en seguridad.
3. SLOs operativos estan definidos, medidos y en cumplimiento sostenido.
4. Existe trazabilidad completa requisito -> implementacion -> prueba -> evidencia.
5. El proceso de auditoria mensual funciona de forma recurrente con scorecard.

