# 06 — Decisiones Arquitectónicas (ADRs)

## ADR-01: MindFlow se diseña como motor de ventas automatizado, no CRM tradicional
**Decisión:** El producto se modela como Sales Automation Engine con flujo E2E orientado a conversión y automatización.
**Razón:** El objetivo de negocio es escalar ingresos y reducir operación manual, no administrar datos comerciales de forma pasiva.
**Alternativas descartadas:** Enfoque CRM genérico con automatización secundaria.

## ADR-02: Arquitectura backend Clean Architecture / Modular Monolith
**Decisión:** Separar API, Application, Domain e Infrastructure, manteniendo módulos de negocio claros.
**Razón:** Facilita mantenibilidad por equipos y evolución controlada sin sobrecosto inicial de microservicios.
**Alternativas descartadas:** Monolito anémico con lógica en controllers; microservicios tempranos.

## ADR-03: Rules Engine como núcleo configurable (Trigger -> Condition -> Action)
**Decisión:** Implementar Rules Engine como capacidad central, con CRUD y activación/desactivación en UI.
**Razón:** Permite programar comportamiento comercial sin cambios de código para cada variación operativa.
**Alternativas descartadas:** Reglas hardcodeadas en servicios backend.

## ADR-04: Lead Intake API-first sin UI
**Decisión:** La entrada de leads será exclusivamente por endpoint (`POST /api/leads/intake`) en el MVP.
**Razón:** El intake es integración machine-to-machine y no requiere capa operativa visual.
**Alternativas descartadas:** Crear UI de intake desde fase inicial.

## ADR-05: Automatización por eventos + jobs en background
**Decisión:** Usar eventos de dominio y jobs diferidos/recurrentes (Hangfire) para follow-ups y acciones automáticas.
**Razón:** Aísla procesos asíncronos, mejora resiliencia y evita acoplar operaciones lentas al request-response.
**Alternativas descartadas:** Ejecutar automatizaciones pesadas en línea durante solicitudes HTTP.

## ADR-06: Prioridad de entrega incremental por sprints
**Decisión:** Ejecutar en orden: Sprint 1 (intake/pipeline/email), Sprint 2 (follow-up/asignación/scoring), Sprint 3 (rules/dashboard), Sprint 4+ (full SaaS).
**Razón:** Permite obtener valor temprano y reducir riesgo técnico y de producto.
**Alternativas descartadas:** Construcción simultánea de todas las épicas.

## ADR-07: Multi-tenant como requisito estructural de evolución full
**Decisión:** Tenant isolation y roles se incluyen como fase dedicada, pero con preparación desde el diseño temprano.
**Razón:** Evita migraciones costosas y asegura viabilidad SaaS.
**Alternativas descartadas:** Postergar completamente tenancy hasta después del producto estable.

## ADR-08: Catálogo de skills exclusivo del workspace
**Decisión:** El proyecto usa skills locales en `.github/skills`, incluyendo skills NovaMind y externos vendorized aprobados.
**Razón:** Garantiza consistencia de prácticas y evita interferencia de skills globales no controlados.
**Alternativas descartadas:** Dependencia de skills de perfil de usuario para decisiones del repositorio.

## ADR-09: Persistencia inicial de Lead Intake con EF Core + SQLite local
**Decisión:** Implementar la primera versión de persistencia de leads usando EF Core con SQLite (`leads.db`) en la API.
**Razón:** Permite tener persistencia real, reproducible y compilable en entorno local sin depender de infraestructura externa durante el MVP inicial.
**Alternativas descartadas:** Persistencia en memoria (sin durabilidad), SQL Server obligatorio desde día 1 (mayor fricción inicial).

## ADR-10: Publicación de `lead.created` desacoplada por interfaz
**Decisión:** Publicar el evento `lead.created` mediante la interfaz `ILeadCreatedEventPublisher` con implementación inicial de logging.
**Razón:** Mantiene el contrato de evento desde el inicio y evita acoplar la lógica de intake a mecanismos específicos de mensajería en esta fase.
**Alternativas descartadas:** No emitir evento en MVP, o acoplar intake directamente a acciones posteriores.

## ADR-11: Modelo inicial de Contact y Company asociado por LeadId
**Decisión:** Implementar `Contact` y `Company` como entidades persistentes con asociación explícita a `Lead` mediante `LeadId` y CRUD backend dedicado.
**Razón:** Habilita evolución hacia pipeline y propuestas manteniendo trazabilidad directa del origen comercial por lead en el MVP.
**Alternativas descartadas:** Mantener datos embebidos en `Lead` sin entidades separadas, o postergar modelo relacional hasta fases posteriores.

## ADR-12: Deduplicación defensiva en capa de aplicación para Contact/Company
**Decisión:** Aplicar deduplicación por reglas de negocio antes de persistir:
- Contact: conflicto por email o teléfono normalizados.
- Company: conflicto por nombre normalizado.
**Razón:** Evita colisiones funcionales tempranas y prepara el sistema para estrategias posteriores de merge asistido.
**Alternativas descartadas:** Permitir duplicados y resolver solo en analítica/reportes, o deduplicación exclusiva por constraints sin mensajes de dominio.

## ADR-13: Pipeline base con etapas default y oportunidad como unidad operativa
**Decisión:** Implementar pipeline inicial con catálogo de etapas default (`new`, `qualified`, `proposal`, `won`) y tratar `Opportunity` como unidad principal de movimiento.
**Razón:** Alinea el modelo con la operación Kanban y permite extender reglas por etapa sin sobrecargar la entidad `Lead`.
**Alternativas descartadas:** Mover directamente `Lead` entre etapas sin entidad de oportunidad, o depender de configuración manual de etapas desde el día 1.

## ADR-14: Historial append-only para trazabilidad de cambios de etapa
**Decisión:** Persistir cada cambio de etapa en `OpportunityStageHistory` como registros append-only.
**Razón:** Garantiza auditabilidad y habilita métricas posteriores de tiempo por etapa, throughput y conversión.
**Alternativas descartadas:** Guardar solo el estado actual sin historial, o sobrescribir movimientos previos en la oportunidad.

## ADR-15: Email como first-class module con logs siempre persistidos
**Decisión:** Implementar el módulo de email en `Domain/Email`, `Application/Email`, `Infrastructure/Email`. El servicio de email siempre persiste un `EmailLog` independientemente del resultado (Skipped / Sent / Failed).
**Razón:** Garantiza trazabilidad operativa completa desde MVP. Si no hay SMTP configurado, el intake nunca falla — el email simplemente se registra como `Skipped`.
**Alternativas descartadas:** No registrar logs cuando no hay SMTP, o lanzar excepción de intake cuando el email falla (rompería el flujo principal).

## ADR-16: SMTP configurable vía API, sin hardcoding de credenciales
**Decisión:** La configuración SMTP se persiste en tabla `SmtpSettings` y se gestiona por `PUT /api/email/smtp-settings`. La respuesta `SmtpSettingsResponse` nunca expone la contraseña.
**Razón:** Sigue principios OWASP Top 10 (A02 — Cryptographic Failures). Para producción se reemplaza almacenamiento por Key Vault.
**Alternativas descartadas:** Credenciales en `appsettings.json`, variables de entorno fijas en código.

## ADR-17: Test isolation per-factory en EmailEndpointTests con SQLite único
**Decisión:** `EmailEndpointTests` usa `EmailTestFactory` que genera un archivo SQLite único por instancia de factory. Cada test crea su propia factory para aislamiento completo de estado.
**Razón:** Los tests de email requieren control preciso del estado SMTP (configurado / no configurado). xUnit no garantiza orden de ejecución dentro de una clase; sin aislamiento de DB los tests se contaminan entre sí.
**Alternativas descartadas:** Compartir fixture de clase con estado compartido, usar SQLite in-memory (requiere mantener conexión abierta durante toda la prueba).

## ADR-18: Follow-up scheduler in-process sin Hangfire en MVP
**Decisión:** El scheduler de follow-up se implementa in-process: `FollowUpService.ExecuteDueJobsAsync` es invocable por un hosted service, sin depender de Hangfire o Azure Functions en el MVP.
**Razón:** Hangfire requiere SQL Server o Redis como backing store, introduce complejidad de instalación y no es necesaria para el MVP. El diseño mantiene `IFollowUpService.ExecuteDueJobsAsync` como contrato limpio que puede ser orquestado por cualquier scheduler (Hangfire, Azure Functions Timer, hosted service) en fases posteriores.
**Alternativas descartadas:** Hangfire con SQLite (inestable en producción), Azure Functions Timer (requiere infraestructura adicional), ejecución síncrona en el request de intake (bloquea la respuesta).

## ADR-19: FollowUpJob como agregado con ciclo de vida explícito
**Decisión:** `FollowUpJob` es un agregado de dominio con estado `Scheduled → Sent | Failed | Cancelled` gestionado por métodos de dominio (`MarkSent`, `MarkFailed`, `Cancel`). El servicio de aplicación consulta `IsDue(DateTime)` para determinar qué jobs ejecutar.
**Razón:** Centraliza invariantes del ciclo de vida en el dominio y evita lógica de estado dispersa en capas superiores. El patrón es consistente con los demás agregados del sistema (Lead, Opportunity).
**Alternativas descartadas:** Estado como enum libre en tabla sin validaciones de dominio, gestionar ciclo de vida exclusivamente en la capa de aplicación.

## ADR-20: Asignación automática MVP con round-robin determinístico
**Decisión:** Implementar asignación inicial mediante round-robin determinístico sobre `AssignmentUsers` activos, ordenados por `CreatedAtUtc` e `Id` como desempate.
**Razón:** Provee distribución equitativa y predecible sin introducir complejidad prematura de reglas dinámicas. El desempate por `Id` evita no determinismo entre ejecuciones.
**Alternativas descartadas:** Asignación aleatoria (no auditable), asignación fija al primer usuario (sesgo operativo), motor de reglas completo en Sprint 1 (sobre-ingeniería para MVP).

## ADR-21: Auditoría explícita de asignaciones con `LeadAssignments`
**Decisión:** Registrar cada asignación en tabla append-only `LeadAssignments` con `LeadId`, `UserId`, `Strategy`, `RuleKey` y `AssignedAtUtc`.
**Razón:** Garantiza trazabilidad de operación comercial y deja el contrato preparado para evolución a reglas avanzadas (campo `RuleKey`) sin romper esquema ni API.
**Alternativas descartadas:** Guardar solo `AssignedUserId` en `Lead` (sin historial), logs no estructurados sin capacidad de consulta analítica.

## ADR-22: Scoring básico con reglas determinísticas y thresholds explícitos
**Decisión:** Implementar scoring MVP con catálogo de reglas determinístico (`ScoreRule`) y thresholds fijos de prioridad: `Low < 50`, `Medium >= 50`, `High >= 80`.
**Razón:** Permite priorización inmediata y reproducible en operación comercial sin depender todavía de un motor de reglas configurable.
**Alternativas descartadas:** Modelo estadístico/ML en MVP (sobre-coste y baja explicabilidad), priorización manual sin score persistido.

## ADR-23: Persistencia del score en `Lead` y exposición por API dedicada
**Decisión:** Persistir `Score` y `Priority` dentro de `Lead`, recalcular durante el flujo de intake y exponer consulta por `ScoringController` (`/api/scoring/rules`, `/api/scoring/leads/{leadId}`).
**Razón:** Mantiene bajo acoplamiento, evita joins innecesarios para ordenamiento operativo y deja el dato disponible para Rules Engine y Dashboard.
**Alternativas descartadas:** Guardar score solo en memoria de request, tabla separada sin necesidad de histórico en MVP, cálculo on-demand en cada lectura.

## ADR-24: Rules Engine MVP con trigger único `lead.created`
**Decisión:** Implementar el Rules Engine MVP con un trigger operativo inicial `lead.created`, evaluando reglas activas configurables por API (CRUD + activar/desactivar).
**Razón:** Permite validar la arquitectura Trigger -> Condition -> Action de forma incremental, con bajo riesgo y cobertura de casos reales inmediatos.
**Alternativas descartadas:** Implementar múltiples triggers complejos en la primera iteración, o postergar completamente el motor hasta fases posteriores.

## ADR-25: Acciones MVP del motor de reglas limitadas a `add_score` y `set_priority`
**Decisión:** En la primera versión, el dispatcher soporta acciones determinísticas sobre lead (`add_score`, `set_priority`), manteniendo extensión futura para acciones cross-module.
**Razón:** Garantiza comportamiento predecible, fácilmente testeable y alineado con Scoring/Assignment ya implementados.
**Alternativas descartadas:** Permitir acciones arbitrarias no validadas (riesgo operativo), integrar todas las acciones avanzadas en una sola entrega.

## ADR-26: Definición explícita de métricas del Dashboard MVP
**Decisión:** El Dashboard MVP expone tres métricas operativas mínimas:
- `TotalLeads`.
- `ConversionRate = WonOpportunities / TotalOpportunities * 100`.
- `PipelineValue = SUM(Value)` de oportunidades persistidas.
Además publica serie temporal `LeadsPerDay` para una ventana configurable por `days` (default 7).
**Razón:** Provee visibilidad inmediata y objetiva con costo de implementación bajo, reutilizando datos ya disponibles en pipeline/scoring.
**Alternativas descartadas:** KPIs avanzados desde el inicio (CAC/LTV), conversión basada en múltiples embudos sin suficiente histórico.

## ADR-27: Dashboard MVP servido como página estática + endpoint de overview
**Decisión:** Implementar la primera pantalla en `wwwroot/dashboard.html` consumiendo `GET /api/dashboard/overview` en lugar de introducir un frontend separado en esta etapa.
**Razón:** Reduce complejidad y acelera time-to-value del MVP intermedio, manteniendo una base simple para futura migración al frontend feature-based.
**Alternativas descartadas:** Posponer UI hasta integrar Next.js completa, o exponer solo endpoint sin visualización operativa inicial.

## ADR-28: Multi-tenant por filtro global EF + contexto de request y roles mínimos por middleware
**Decisión:** Implementar aislamiento tenant en backend con:
- Contexto scoped (`ITenantContext`/`TenantContext`) poblado por request desde `X-Tenant-Id` y `X-User-Role`.
- Filtros globales por `TenantId` en `LeadsDbContext` para entidades de negocio.
- Shadow property `TenantId` e inicialización automática en `SaveChanges`.
- Middleware de autorización por rol que deniega escrituras en `/api/*` cuando el rol es `Viewer`.
**Razón:** Permite una transición incremental del MVP a SaaS sin rehacer módulos existentes, manteniendo compatibilidad hacia atrás por defaults (`tenant=default`, `role=Admin`) y agregando barreras mínimas de seguridad operativa.
**Alternativas descartadas:**
- Requerir autenticación/claims completa antes de habilitar tenant (incrementa alcance y tiempo del hito).
- Implementar aislamiento solo en repositorios manuales sin filtro global (mayor riesgo de fuga por omisión).

## ADR-29: Automatización de propuestas con PDF interno + tracking token + reminder diferido
**Decisión:** Implementar `TASK-FULL-11` con un módulo dedicado de propuestas:
- Agregados `Proposal` y `ProposalReminderJob` en dominio.
- Generación de PDF con componente interno (`SimpleProposalPdfGenerator`) para evitar dependencia externa temprana.
- Envío inicial y reminder a través de `EmailService` usando templates `proposal.standard` y `proposal.reminder`.
- Tracking por token (`/api/proposals/track/{trackingToken}`) persistiendo `ViewCount` y `LastViewedAtUtc`.
- Reminder diferido (+72h) ejecutable por endpoint operativo (`/api/proposals/reminders/execute-due`) para futura orquestación por scheduler.
**Razón:** Permite acelerar cierre comercial con trazabilidad completa (documento + envío + seguimiento + recordatorio) manteniendo compatibilidad con arquitectura modular existente y enfoque incremental del MVP extendido.
**Alternativas descartadas:**
- Integrar librería pesada de generación PDF en esta fase (mayor complejidad y superficie de fallos).
- Delegar tracking a sistemas externos de marketing desde el inicio (acoplamiento prematuro).
- Ejecutar reminders síncronamente en requests de negocio (impacto en latencia y resiliencia).

## ADR-30: Onboarding post-venta automático acoplado a transición de pipeline `won`
**Decisión:** Implementar `TASK-FULL-12` disparando onboarding desde `PipelineService` cuando una oportunidad cambia a etapa `won`.
- Crear `Customer` de forma idempotente por `LeadId`.
- Generar tareas iniciales de onboarding (`kickoff-call`, `requirements-checklist`, `workspace-setup`).
- Enviar email de bienvenida con template `customer.welcome`.
- Exponer tracking por token (`/api/onboarding/track/{trackingToken}`) registrando activaciones.
**Razón:** Centraliza la automatización post-venta en el punto de verdad del estado comercial (pipeline), reduce operación manual y asegura trazabilidad completa cliente-onboarding desde el primer cierre ganado.
**Alternativas descartadas:**
- Ejecutar onboarding mediante endpoint manual separado (riesgo de omisiones operativas).
- Disparar onboarding desde tareas batch no determinísticas (latencia y complejidad innecesaria en esta fase).
- Crear customer en intake o propuesta en lugar de `won` (no respeta el hito real de cierre).

## ADR-31: Analytics avanzado definido por contratos explícitos y KPIs normalizados antes de implementación
**Decisión:** Completar `TASK-FULL-13` definiendo primero el marco analítico (KPIs, fórmulas, filtros y contratos) antes de construir endpoints/servicios de ejecución.
- KPIs normalizados en cinco dominios: funnel, revenue, velocity, SLA y activación onboarding.
- Filtros estándar unificados: `StartDateUtc`, `EndDateUtc`, `GroupBy`, `Stage`, `Source`, `Tenant`.
- Contratos backend explícitos en `backend/src/Api/Contracts/Analytics/` para desacoplar diseño de API de la implementación de queries.
- Priorización explícita de endpoints para ejecución incremental en `TASK-FULL-14`.
**Razón:** Reduce retrabajo y divergencia semántica, alinear equipos backend/frontend y asegurar trazabilidad de métricas de negocio antes de optimizar consultas o materializaciones.
**Alternativas descartadas:**
- Implementar endpoints ad-hoc sin modelo de KPI unificado (riesgo de inconsistencias).
- Diseñar contratos después de codificar servicios (mayor costo de refactor).
- Definir analytics sólo en UI sin contratos backend canónicos (acoplamiento y deuda técnica).

## ADR-32: Analytics avanzado implementado con servicio agregado + snapshot repository
**Decisión:** Implementar `TASK-FULL-14` con un servicio de aplicación central (`AnalyticsAdvancedService`) y un repositorio analítico de snapshot (`AnalyticsAdvancedDataRepository`) que carga de forma consistente leads, oportunidades, historial de etapas, asignaciones y clientes onboarding para calcular KPIs.
- Exponer endpoints separados por KPI y uno de overview agregado en `AnalyticsAdvancedController`.
- Reutilizar filtros estándar de `AnalyticsAdvancedQuery` para mantener contratos estables.
- Calcular KPIs de funnel/revenue/velocity/SLA/onboarding en capa de aplicación con reglas determinísticas y redondeo explícito.
- Validar implementación con TDD de integración (`AnalyticsAdvancedEndpointTests`) y gates de Release.
**Razón:** Mantiene separación de responsabilidades (query/data loading vs. cálculo de negocio), reduce duplicación entre endpoints y habilita evolución futura a materialized views o queries optimizadas sin romper contratos API.
**Alternativas descartadas:**
- Implementar lógica KPI directamente en controller (acoplamiento y baja testeabilidad).
- Crear un endpoint monolítico sin KPIs individuales (menor flexibilidad para consumo frontend).
- Acoplar consultas analíticas directamente a repositorios transaccionales por cada KPI (duplicación y riesgo de inconsistencias).

## ADR-33: Frontend analytics avanzado desacoplado por service layer + hardening de consultas
**Decisión:** Completar `TASK-FULL-15` implementando una pantalla dedicada (`analytics-advanced.html`) con consumo desacoplado vía `analyticsService` y validaciones de filtros tanto en cliente como en API.
- UI con tabs por KPI y filtros estándar para mantener consistencia de uso sobre los endpoints avanzados.
- Validación server-side en `AnalyticsAdvancedController` para `groupBy` y ventanas temporales acotadas (máximo 366 días).
- Optimización de lectura en repositorio analítico con `AsNoTracking` y fortalecimiento de índices SQL para columnas de alta cardinalidad temporal/relacional.
- Cobertura TDD específica en `AnalyticsAdvancedFrontendEndpointTests` para asegurar serving de UI y comportamiento de validaciones.
**Razón:** Mejora confiabilidad operativa y performance sin romper contratos existentes, habilitando crecimiento de volumen y uso productivo inmediato del módulo analítico desde interfaz.
**Alternativas descartadas:**
- Integrar la UI avanzada dentro del dashboard básico existente sin separación de responsabilidades (riesgo de complejidad incremental).
- Delegar validaciones de filtros solo al frontend (riesgo de bypass y degradación por consultas amplias).
- Posponer tuning de índices hasta detectar degradación en producción (riesgo de deuda técnica y latencia evitables).

## ADR-34: Observabilidad analytics con collector in-memory y endpoint operativo dedicado
**Decisión:** Completar `TASK-FULL-16` agregando observabilidad explícita sobre endpoints de analytics avanzado mediante un servicio singleton in-memory (`IAnalyticsObservabilityService`) y un endpoint operativo `GET /api/analytics/advanced/metrics`.
- Métricas registradas por endpoint: `RequestCount`, `SuccessCount`, `ErrorCount`, `AverageLatencyMs`.
- Integración de tracking en cada acción del controller, incluyendo errores por validación de filtros.
- Contrato de snapshot tipado para consumo operacional interno.

## ADR-35: Pipeline enterprise con seed tenant-safe y board query unificado
**Decisión:** Consolidar la evolución PL-03..PL-12 en un único slice de pipeline que expone board query, export, throughput, WIP limits, historial enriquecido, auto-move y concurrencia optimista, manteniendo `PipelineService` como punto de orquestación.
**Razón:** Evita dispersar reglas operativas entre controller, repositorios y rules engine; además corrige el defecto multi-tenant del catálogo de etapas mediante seed por tenant con IDs propios e índices únicos compuestos (`TenantId + Name`, `TenantId + Order`).
**Alternativas descartadas:**
- Resolver filtros/ordenamiento exclusivamente en frontend y mantener board sin semántica operativa en backend.
- Reutilizar GUIDs globales de etapas por tenant y sostener unicidad global de `Name`/`Order`.
- Implementar concurrencia optimista y auto-move como handlers aislados fuera de `PipelineService`.
- Validación con TDD de integración para disponibilidad del endpoint y acumulación de contadores.
**Razón:** Aporta trazabilidad operacional inmediata y diagnóstico rápido de degradación sin introducir complejidad prematura de almacenamiento externo o stack de monitoreo adicional.
**Alternativas descartadas:**
- Instrumentar únicamente logs sin endpoint de consulta (menor observabilidad operativa en tiempo real).
- Introducir desde esta fase persistencia completa de telemetría en base de datos (alcance mayor y costo operativo inicial).
- Delegar observabilidad a tooling externo sin contrato interno de métricas (acoplamiento y menor portabilidad).

## ADR-35: Persistencia histórica de métricas de observabilidad con BackgroundService y tabla de sistema sin tenant isolation
**Decisión:** Persistir snapshots periódicos del colector in-memory en tabla `ObservabilityMetricRecords` sin filtros de tenant (tabla de sistema), usando un `BackgroundService` (cada 5 min) y un endpoint de flush manual (`POST /metrics/history/snapshot`).
- Entidad `ObservabilityMetricRecord` sin shadow property `TenantId` — tabla de sistema, no de negocio.
- `ObservabilityPersistenceService` (scoped) encapsula la lógica de flush y es reutilizable tanto por el job como por el endpoint.
- `ObservabilityPersistenceBackgroundService` usa `IServiceScopeFactory` para crear un scope scoped por ciclo de flush.
- Endpoint `POST /metrics/history/snapshot` expuesto para operaciones manuales y testabilidad sin necesidad de manipular timers.
- Consulta histórica con filtros `startUtc`, `endUtc`, `endpointName` y límite de 1000 registros para proteger rendimiento de consultas largas.
**Razón:** La tabla de sistema de métricas no debe filtrarse por tenant porque los datos son de infraestructura transversal. La separación entre servicio de flush y background service permite testabilidad directa sin acoplamiento a timer.
**Alternativas descartadas:**
- Persistir en tabla con tenant isolation (semánticamente incorrecto para datos de sistema).
- Usar Hangfire para el job periódico (introduce dependencia innecesaria cuando `BackgroundService` es suficiente).
- Exponer solo polling del colector in-memory sin historial (no satisface requerimiento de auditoría y tendencias).

## ADR-36: Evaluación de alertas desacoplada sobre snapshot de observabilidad con umbrales multi-tenant
**Decisión:** Implementar `AlertEvaluationService` como servicio de aplicación desacoplado, ejecutado dentro del flujo de flush de observabilidad. El servicio evalúa umbrales activos por endpoint y genera eventos separados por métrica (`ErrorRatePercent`, `AverageLatencyMs`) con notificación por email.
- Umbrales (`AlertThresholds`) y eventos (`AlertEvents`) se modelan como tablas de negocio con `TenantId` y filtros globales de tenant.
- La evaluación se realiza sobre el snapshot actual del colector in-memory; no se recalcula desde histórico para minimizar costo en ciclo operativo.
- Cada breach produce un `AlertEvent` auditable; el estado de notificación se persiste en el evento (`NotificationSent`).
- Notificación email encapsulada en `IEmailService.SendAnalyticsDegradationAlertAsync` con template dedicado `alert.analytics.degradation`.
**Razón:** Mantiene cohesión arquitectónica (persistir + evaluar en un mismo ciclo controlado), evita lógica de alertado en controllers y habilita extensibilidad futura de métricas/umbrales sin romper contratos API.
**Alternativas descartadas:**
- Evaluar alertas en controller HTTP de métricas (acopla alerting a tráfico manual y no garantiza ejecución periódica).
- Evaluar desde job separado sin compartir ciclo de flush (duplica orquestación y complica consistencia temporal de snapshot evaluado).
- Persistir solo un evento agregado por endpoint sin granularidad de métrica (reduce trazabilidad operativa y depuración).

## ADR-37: Observabilidad robusta con agregación incremental por lotes y control de cardinalidad en origen
**Decisión:** Cerrar AO-07 y AO-08 con dos mecanismos complementarios:
- AO-07: pipeline incremental de agregación sobre `ObservabilityMetricRecords`, persistiendo lotes por ventana temporal (`windowMinutes`) y endpoint en `ObservabilityAggregateBatches`, con checkpoint por ventana (`ObservabilityAggregationCheckpoints`) y estado acumulado por endpoint (`ObservabilityEndpointAggregationStates`).
- AO-08: control de cardinalidad en el colector in-memory con normalización de rutas dinámicas (GUID/números/tokens largos), límite de series distintas y bucket de overflow (`__overflow__`), exponiendo metadata de cardinalidad en snapshot operativo.

**Razón:**
- Evita recalcular histórico completo en cada lectura y permite rollups incrementales estables para ventanas operativas.
- Reduce riesgo de explosión de cardinalidad que degrada memoria y ruido de métricas cuando aparecen rutas parametrizadas de alta variación.
- Mantiene compatibilidad con endpoints existentes (`metrics`, `metrics/history`, `metrics/history/snapshot`) mientras añade capacidades enterprise sin romper contratos previos.

**Alternativas descartadas:**
- Reagregar histórico completo en cada consulta (coste creciente y latencia no acotada).
- Mover control de cardinalidad a una capa posterior (DB/consulta) en lugar de origen (no evita presión en memoria y en snapshot base).
- Introducir stack externo de observabilidad para resolver AO-07/AO-08 en esta fase (alcance mayor al objetivo del backlog actual).

## ADR-37: Email transaccional operado como cola persistente con provider desacoplado y trazabilidad por correlación
**Decisión:** Evolucionar el módulo de email para que welcome/proposal/customer welcome/analytics alert no dependan del request-response ni del transporte SMTP directo. Los envíos se modelan como `EmailDispatchJob` persistente y `EmailLog` append-only enlazado por `CorrelationId`; el transporte se abstrae por provider (`smtp` o `webhook`) definido en `SmtpSettings`.
**Razón:** Permite resiliencia operacional, retry controlado, KPIs de entrega y alertado de degradación sin perder evidencia histórica cuando un envío falla o se reintenta manualmente.
**Alternativas descartadas:** Mantener envíos síncronos acoplados al intake/propuesta/onboarding; sobreescribir logs fallidos al reintentar; introducir un broker externo antes de estabilizar la semántica del módulo.

## ADR-38: Follow-up gobernado por política tenant-aware, quiet hours y stop-list
**Decisión:** Mantener `SendLeadFollowUpAsync` como ejecución directa para preservar el patrón existente de retry/poison queue, pero mover la inteligencia de scheduling a `FollowUpService` usando `FollowUpPolicySettings`, score persistido, quiet hours y supresión por `EmailStopListEntry`.
**Razón:** Separa claramente la decisión de cuándo contactar del transporte de email, evita follow-ups fuera de ventana operativa y hace reusable la política por tenant sin romper la remediación ya existente del runtime diferido.
**Alternativas descartadas:** Resolver delays/quiet hours en controllers o frontend; aplicar políticas hardcodeadas globales; convertir follow-up al nuevo dispatcher genérico antes de cerrar la remediación enterprise existente.

## ADR-37: Dashboard de observabilidad como frontend estático con service layer y pestañas operativas
**Decisión:** Implementar `observability.html` como artefacto estático en `wwwroot` con service layer JavaScript (`observabilityService`) y separación funcional por tabs (`realtime`, `history`, `alerts`).
- Navegación integrada desde `analytics-advanced.html` y `dashboard.html` hacia `/observability.html`.
- Auto-refresh configurable sólo para tab realtime para minimizar tráfico innecesario en tabs de historial/alertas.
- Gestión de umbrales embebida en la vista operativa (crear/eliminar) y consumo de eventos de alerta para trazabilidad.
- Endpoint `alert-thresholds` extendido con filtro `isActive` para soportar consultas operativas de alertas activas sin post-filtrado costoso en cliente.
**Razón:** Entrega rápida y robusta para operación diaria sin introducir framework frontend adicional, manteniendo consistencia con el patrón ya adoptado en `dashboard.html` y `analytics-advanced.html`.
**Alternativas descartadas:**
- Crear SPA separada para observabilidad (mayor complejidad de build/deploy en esta fase).
- Renderizado server-side específico para observabilidad (acopla más backend y reduce iteración de UI operativa).
- Dejar gestión de umbrales fuera de UI y operar sólo por API (menor usabilidad para operadores).

## ADR-38: Versionado API por header con compatibilidad progresiva
**Decisión:** Adoptar versionado por header `X-Api-Version` para endpoints bajo `/api`, aceptando `1` y `v1` como versión estable actual.
- Requests sin header mantienen comportamiento backward-compatible y reciben encabezado de respuesta con versión efectiva (`X-Api-Version: 1`).
- Requests con versiones no soportadas retornan `400` con contrato uniforme (`ApiErrorResponse`).
**Razón:** Permite gobernar evolución de contratos sin romper rutas existentes ni exigir refactor masivo de consumers actuales.
**Alternativas descartadas:**
- Versionado por path inmediato (`/api/v1/...`) en todo el backend (alto costo de migración en una iteración).
- Mantener API sin política explícita de versión (riesgo alto de breaking changes silenciosos).

## ADR-39: Contrato unificado de error y middleware global como capa de seguridad operativa
**Decisión:** Estandarizar errores transversales con `ApiErrorResponse` y centralizar excepciones no controladas en `GlobalExceptionHandlingMiddleware`.
- Validación transversal de payloads configurada por `InvalidModelStateResponseFactory`.
- Error 500 devuelve payload seguro con `traceId` y sin exponer stack traces al cliente.
**Razón:** Mejora consistencia de integración, simplifica diagnóstico y reduce exposición de detalles internos.
**Alternativas descartadas:**
- Manejo de errores por controlador (duplicación, divergencia de contratos y mayor deuda).
- Dejar excepciones por defecto de ASP.NET sin capa de homogenización.

## ADR-40: Idempotencia en intake crítico mediante header y store de replay
**Decisión:** Implementar idempotencia para `POST /api/leads/intake` usando header `Idempotency-Key` y store en memoria por ámbito (`tenant + endpoint`).
- Reenvíos con misma clave retornan el mismo recurso (`Created`) y marcan replay por header `X-Idempotent-Replay: true`.
- La estrategia se diseñó para extensión posterior a almacenamiento distribuido.
**Razón:** Evita duplicados por reintentos de red en el flujo más crítico de entrada comercial, con bajo costo de implementación.
**Alternativas descartadas:**
- No aplicar idempotencia en intake (duplicación de leads y ruido analítico).
- Exigir token transaccional externo desde el cliente como prerequisito duro en esta fase.

## ADR-41: Paginación opcional en listados para escala sin ruptura de contratos
**Decisión:** Agregar `page` y `pageSize` como parámetros opcionales en endpoints de listado de mayor uso, manteniendo respuesta compatible cuando no se envían.
- Aplicado en listados de assignment users/assignments, email logs, rules, proposals, onboarding customers y alertas avanzadas.
**Razón:** Introduce control de volumen y latencia en consumo incremental sin forzar cambios inmediatos en clientes existentes.
**Alternativas descartadas:**
- Migrar de inmediato a contratos paginados estrictos en todos los endpoints (alto impacto de compatibilidad).
- Mantener listados ilimitados (riesgo de degradación progresiva).

## ADR-42: Health checks y resiliencia SMTP como guardrails operativos mínimos
**Decisión:** Exponer `/health/live` y `/health/ready` con chequeo de `LeadsDbContext`, e incorporar política básica de resiliencia SMTP (timeout y reintentos acotados).
**Razón:** Mejora detectabilidad de incidentes y recuperación ante fallos transitorios outbound sin introducir aún un framework de resiliencia más pesado.
**Alternativas descartadas:**
- Operar sin endpoints de health estándar (menor observabilidad para despliegue/monitoring).
- Reintentos manuales dispersos en capa de aplicación sin encapsulación en sender.

## ADR-43: Security strict-mode progresivo con compatibilidad operativa
**Decisión:** Implementar un `StrictMode` de seguridad configurable que activa enforcement estricto de autenticación/integridad de contexto, manteniendo compatibilidad hacia atrás en entornos que aún dependen de headers legacy.
- Runtime de seguridad centralizado en `SecurityRuntimeOptions`.
- JWT bearer wiring activo para transición a identidad firmada.
- Enforcement estricto por middleware para rutas API y intake con API key cuando la política lo exige.
**Razón:** Permite elevar seguridad sin romper de forma abrupta consumidores existentes durante la transición.
**Alternativas descartadas:**
- Romper de inmediato todos los clientes legacy sin ventana de migración.
- Mantener modelo legacy indefinidamente sin ruta a identidad verificable.

## ADR-44: Tenant-context integrity checks con fuente autenticada prioritaria
**Decisión:** `TenantMiddleware` prioriza claims/contexto autenticado (`tenant_id`, `role` o headers de gateway autenticado) y valida mismatch contra headers de request en modo estricto.
**Razón:** Mitiga spoofing de headers y fortalece aislamiento multi-tenant en escenarios con proxies/gateways.
**Alternativas descartadas:**
- Confiar ciegamente en headers de cliente como única fuente de tenant/rol.
- Validar coherencia solo en controladores (duplicación y cobertura inconsistente).

## ADR-45: Secretos SMTP cifrados en reposo con Data Protection
**Decisión:** Cifrar contraseña SMTP al persistir (`SmtpSettingsRepository`) y descifrar en lectura runtime, manteniendo contrato API sin exposición de secretos.
**Razón:** Reduce riesgo de exposición de credenciales en base de datos y cumple baseline de seguridad en reposo sin introducir complejidad externa inmediata.
**Alternativas descartadas:**
- Persistencia plaintext en DB.
- Exigir Key Vault completo en esta iteración sin preparar transición incremental.

## ADR-46: Seguridad operativa por capas (rate limit + brute-force + admin-only)
**Decisión:** Aplicar defensa en profundidad para abuso operativo:
**Razón:** Disminuye superficie de abuso y operaciones no autorizadas en endpoints de alto impacto.
**Alternativas descartadas:**

## ADR-47: Seguridad y cumplimiento como artefactos versionados del repositorio
**Decisión:** Versionar baseline ASVS, threat model y runbook de incidentes en `ia/security/`, junto con un gate CI (`security-sast-dast.yml`).
**Razón:** Asegura trazabilidad auditable y repetibilidad del proceso de cumplimiento en el ciclo de desarrollo.
**Alternativas descartadas:**
- Mantener documentos de seguridad fuera del repositorio (baja trazabilidad).
- Ejecutar revisiones ad-hoc sin evidencia versionada.

## ADR-48: Scoring con linaje explícito y recalculo controlado por ventana
**Decisión:** Persistir `ScoringVersion` y `ScoredAtUtc` en `Lead`, y habilitar recalculo historico acotado por fecha en `POST /api/scoring/recalculate`.
**Razón:** Permite trazabilidad de cambios en la formula de score, re-procesamiento auditable y comparabilidad temporal sin romper contratos existentes.
**Alternativas descartadas:**
- Mantener scoring opaco sin versionado.
- Recalculos globales sin ventana controlada.

## ADR-49: Gobernanza de calidad de dato operativa en capa de aplicación
**Decisión:** Introducir metadata estandarizada de lead (`Channel`, `Campaign`, `Country`), deduplicacion fuzzy configurable (enforcement opt-in), validacion reforzada de telefonia y endpoint dedicado de KPIs de calidad (`GET /api/dashboard/data-quality`).
**Razón:** Eleva consistencia de captura y observabilidad de calidad sin forzar cambios disruptivos en flujos existentes.
**Alternativas descartadas:**
- Forzar bloqueo de duplicados por defecto (alto riesgo de regresion).
- Posponer medicion de calidad para etapas posteriores de BI.

## ADR-50: Gobernanza de reglas por entorno con promocion versionada
**Decisión:** Extender `Rule` con metadatos de gobierno (`Environment`, `ApprovalStatus`, `Version`, `ApprovedBy`, `ApprovedAtUtc`) y exponer promocion formal `POST /api/rules/{id}/promote`.
**Razón:** Permite trazabilidad de cambios y control de ciclo dev/stg/prod sin acoplar la operacion a scripts manuales no auditables.
**Alternativas descartadas:**
- Mantener reglas sin version ni estado de aprobacion.
- Depender exclusivamente de convenciones externas al sistema.

## ADR-51: Auditoria temporal y soft delete como base de recuperabilidad operacional
**Decisión:** Implementar snapshots temporales para lead (`LeadAuditSnapshots`) y soft delete en entidades operativas (`Contact`, `Company`) mediante `IsDeleted/DeletedAtUtc` + query filters.
**Razón:** Mejora trazabilidad de cambios y recuperabilidad sin introducir cascadas destructivas ni ampliar downtime de soporte.
**Alternativas descartadas:**
- Borrado fisico irreversible.
- Auditoria solo en logs de infraestructura sin endpoint de consulta funcional.

## ADR-52: Consistencia UTC y evidencias de retencion como controles transversales DAT
**Decisión:** Forzar serializacion UTC en API con convertidores JSON globales y registrar cada ejecucion del cleanup sensible en `DataRetentionRuns`.
**Razón:** Elimina desfaces de timezone en contratos y aporta evidencia auditable de mantenimiento de datos.
**Alternativas descartadas:**
- Dejar timezone al comportamiento por defecto del serializer.
- Mantener limpieza sin bitacora estructurada.

## ADR-53: Controles residuales DAT sobre endpoints operativos y jobs fallidos
**Decisión:** Centralizar masking PII en una utilidad reusable (`PiiMasking`), registrar anomalías de intake como eventos auditables `lead.data_anomaly.*` y tratar follow-up fallido como dead-letter reencolable mediante endpoints operativos dedicados.
**Razón:** Cierra la brecha entre gobernanza de datos y operación diaria: reduce exposición de PII en superficies de soporte, agrega trazabilidad estructurada de calidad de dato y evita remediación manual ad-hoc de automatizaciones fallidas.
**Alternativas descartadas:**
- Mantener masking disperso en cada controller sin política común.
- Medir anomalías solo como KPIs calculados sin evidencia evento-a-evento.
- Resolver follow-up fallido únicamente por manipulación directa de base de datos.

## ADR-54: Estabilización post-DAT con remediación de proposals, historial de anomalías y summary de drift
**Decisión:** Extender el patrón operativo post-DAT en tres frentes: dead-letter reencolable para `ProposalReminderJob`, historial filtrable de `lead.data_anomaly.*` desde dashboard y summary de drift de reglas con query hardening (`AsSplitQuery`/`AsNoTracking`) en repositorio.
**Razón:** Homologa la operación de automatizaciones diferidas, vuelve auditable la calidad histórica del dato y aporta una vista rápida de gobernanza de reglas sin requerir exploración manual de catálogos completos.
**Alternativas descartadas:**
- Mantener proposals como flujo excepcional sin remediación dedicada.
- Consultar anomalías solo por lead individual en lugar de vista histórica agregable.
- Detectar drift revisando manualmente el catálogo de reglas sin endpoint summary ni optimización de consultas.

## ADR-55: Retry policy acotada y poison queue explícita para jobs diferidos
**Decisión:** Aplicar retry policy acotada a `FollowUpJob` y `ProposalReminderJob`, promoviendo a estado `Poisoned` al agotar intentos y exponiendo consultas operativas separadas de poison queue junto con requeue manual controlado.
**Razón:** Evita reprocesamiento infinito, reduce intervención manual temprana y deja visible una cola terminal auditable para soporte/operación cuando la falla persiste más allá del umbral permitido.
**Alternativas descartadas:**
- Mantener solo dead-letter manual sin retry automático.
- Reintentar indefinidamente cada job fallido sin cola terminal explícita.
- Ocultar el intento vigente y el estado terminal al consumidor de la API operativa.

## ADR-56: Onboarding welcome jobs bajo el mismo contrato de retries y alertado de crecimiento poison
**Decisión:** Introducir `OnboardingWelcomeJob` como automatización diferida de onboarding con retry policy acotada, transición a `Poisoned`, APIs operativas de ejecución/requeue y emisión de `AlertEvent` (`PoisonQueueDepth`) al crecer la cola poison por tipo de job.
**Razón:** El onboarding quedaba fuera del patrón de resiliencia operativa aplicado a follow-up/proposals. Esta unificación reduce asimetrías de soporte y habilita alertado temprano sobre degradación operacional en jobs diferidos.
**Alternativas descartadas:**
- Mantener welcome onboarding en envío directo sin cola operativa.
- Resolver fallos repetidos solo con inspección manual de email logs.
- Alertar únicamente por KPIs analytics sin señal específica de acumulación en poison queue.

## ADR-57: Alertado de poison queue con supresión por cooldown y delta significativo
**Decisión:** Endurecer `PoisonQueueAlertService` con una política de supresión de ruido basada en el último evento emitido por endpoint/métrica: aplicar ventana de cooldown y omitir alertas de incrementos marginales, permitiendo nuevas alertas dentro de la ventana solo cuando el crecimiento de `PoisonQueueDepth` alcanza un delta significativo.
**Razón:** Sin esta política, los incrementos consecutivos pequeños generan tormenta de alertas operativas y reducen señal/ruido para soporte. La deduplicación conserva detección temprana en saltos relevantes sin perder trazabilidad histórica.
**Alternativas descartadas:**
- Mantener emisión por cada transición a `Poisoned` sin supresión.
- Aplicar solo throttling temporal sin considerar magnitud de crecimiento.
- Desactivar alertas de poison queue y depender de consulta manual periódica.

## ADR-58: Tendencia histórica de poison queue como agregado de AlertEvents
**Decisión:** Implementar un endpoint agregado dedicado (`/api/analytics/advanced/alert-events/poison-queue-trend`) sobre `AlertEvent` filtrando métrica `PoisonQueueDepth`, con agrupación temporal configurable (`day|hour`) y filtro opcional por `jobType`, y exponerlo en el panel operativo de observabilidad.
**Razón:** La lista de eventos aislados no permite detectar fácilmente patrones de acumulación/reducción. El agregado por bucket mejora lectura operacional y acelera diagnóstico por tipo de automatización diferida sin introducir nueva persistencia.
**Alternativas descartadas:**
- Construir tendencia en cliente consumiendo toda la lista de eventos crudos.
- Persistir una tabla nueva de series temporales para poison queue en esta fase.
- Reutilizar únicamente historial de métricas generales sin separar el dominio `PoisonQueueDepth`.

## ADR-59: Priorización operativa de poison queue por severidad y variación inter-bucket
**Decisión:** Introducir un endpoint de prioridad (`/api/analytics/advanced/alert-events/poison-queue-priority`) que rankea tipos de job en función de severidad y variación respecto al bucket previo (`deltaDepth`, `deltaPercent`), con filtros de ventana, bucket, job type y tamaño máximo de ranking.
**Razón:** La tendencia por sí sola aporta visibilidad, pero no priorización inmediata. El ranking orienta respuesta operativa hacia degradaciones más críticas sin que soporte tenga que inspeccionar manualmente todos los puntos de serie.
**Alternativas descartadas:**
- Mantener priorización manual sobre tablas de trend.
- Ordenar únicamente por profundidad actual sin considerar variación.
- Resolver prioridad únicamente en frontend sin contrato backend estable.

## ADR-60: Guía operativa embebida en ranking poison (runbook hints + remediation path)
**Decisión:** Extender cada ítem priorizado de poison queue con metadatos accionables (`RecommendedAction`, `RunbookHint`, `RemediationPath`) y exponerlos directamente en observability UI con shortcut de remediación.
**Razón:** Priorizar qué atender no era suficiente; soporte necesitaba orientación inmediata de cómo actuar. Embebiendo guía y atajos en el mismo contrato se reduce fricción y MTTR sin depender de consultas externas.
**Alternativas descartadas:**
- Mantener runbooks solo en documentación externa sin contextualización por ítem.
- Resolver atajos de remediación únicamente del lado frontend sin metadata backend.
- Añadir guía operativa como texto estático global sin considerar severidad/job type.

## ADR-61: Feedback loop de remediación poison con telemetría persistente y summary agregado
**Decisión:** Persistir cada ejecución de remediación de poison queue en una entidad dedicada (`PoisonQueueRemediationRun`) y exponer tres capacidades operativas: registro (`POST`), historial filtrable (`GET`) y resumen de efectividad (`GET`) con success rate y latencias promedio.
- La UI de observabilidad registra automáticamente un run con outcome `opened` al usar el shortcut `Open remediation`.
- El panel operativo incorpora "Remediation Effectiveness" para cerrar el loop entre priorización, acción y resultado medible.
**Razón:** El sistema ya priorizaba y sugería acciones, pero carecía de evidencia estructurada sobre ejecución y efectividad real. Sin este loop, no era posible ajustar recomendaciones con datos operativos ni medir impacto en tiempos de resolución.
**Alternativas descartadas:**
- Registrar ejecuciones solo en logs no estructurados sin endpoint consultable.
- Calcular efectividad únicamente desde `AlertEvent` sin evidencia de acción de remediación.
- Instrumentar telemetría solo en frontend sin persistencia backend multi-tenant.

## ADR-62: Transición de estado por runId para remediación poison
**Decisión:** Modelar la evolución operacional de cada remediación como transiciones sobre un run existente (`PUT /poison-queue-remediation-runs/{id}`) en lugar de crear un nuevo registro por cada cambio de estado.
- Estados válidos: `opened`, `in_progress`, `resolved`, `partial`, `failed`.
- El cálculo de efectividad consume el estado final persistido del mismo run, preservando trazabilidad temporal (`ExecutedAtUtc`) y latencia recalculada.
- La UI de observabilidad expone controles directos por fila priorizada para actualizar el estado operativo.
**Razón:** Crear múltiples runs para una sola remediación distorsiona métricas de éxito y latencia. La transición por `runId` conserva granularidad operacional y evita inflar artificialmente el denominador de efectividad.
**Alternativas descartadas:**
- Insertar un run nuevo por cada estado (`opened`, `in_progress`, `resolved`) y deduplicar en reportes.
- Mantener solo `opened` sin registrar progreso/finalización.
- Resolver transición de estado exclusivamente en frontend sin endpoint explícito de actualización.

## ADR-63: Correlación de impacto de remediación con señal before/after en ventana observacional
**Decisión:** Introducir un endpoint dedicado de impacto (`GET /poison-queue-remediation-impact`) que correlaciona runs cerrados con eventos `PoisonQueueDepth` antes y después de la ejecución para derivar señal de reducción operacional.
- `PreDepth`: último valor observado previo al run (ventana de lookback).
- `PostDepth`: mínimo valor observado posterior (ventana de observación).
- Métricas por run: `DepthDelta`, `ReductionPercent`, `IsPositiveImpact`.
- Métricas agregadas de ventana: `PositiveImpactRatePercent` y `AverageDepthDelta`.
**Razón:** El outcome declarado (`resolved`) no garantiza por sí solo mejora observable en backlog. La correlación before/after añade una capa de verificación operacional basada en evidencia de profundidad real.
**Alternativas descartadas:**
- Considerar impacto únicamente por outcome manual sin contraste con telemetría.
- Medir impacto solo con promedio global diario sin granularidad por run.
- Requerir modelado causal complejo en esta fase en lugar de señal pragmática de correlación operativa.

## ADR-64: Segmentación de impacto de remediación por JobType y Severity
**Decisión:** Añadir endpoint `GET /poison-queue-remediation-impact/by-segment` que reutiliza `BuildRemediationImpactPointsAsync` y agrupa los resultados en dos dimensiones: `byJobType` y `bySeverity`. Cada ítem de segmento expone `TotalRuns`, `PositiveImpactRuns`, `PositiveImpactRatePercent`, `AverageDepthDelta`.
**Razón:** La vista individual por run (ADR-63) es útil para diagnóstico forense, pero los operadores necesitan una vista de tendencia agregada por tipo de job y criticidad para decidir dónde concentrar capacidad de remediación. La segmentación permite priorización basada en evidencia histórica sin requerir ML.
**Alternativas descartadas:**
- Añadir filtros `groupBy` al endpoint existente → rompe el contrato de respuesta actual y complica la serialización.
- Agregar segmentación en el cliente JS calculando desde Items → no escala con volúmenes grandes; mejor mantenerlo en servidor.
- Exponer solo segmentación por jobType y omitir severity → pierde dimensión de criticidad operacional.

## ADR-65: Rules Engine enterprise con controles de ejecución y auditoría estructurada
**Decisión:** Extender el motor de reglas para operar con gobernanza y seguridad operativa enterprise:
- triggers multi-evento (`lead.created`, `stage_changed`, `lead.responded`, `proposal.sent`);
- validación DSL obligatoria antes de activación/actualización;
- prioridad y política de conflicto (`first_wins` / `merge`);
- ventanas horarias UTC + cooldown por regla;
- auditoría por ejecución (`RuleExecutionLog`) y snapshots de revisión (`RuleRevision`);
- templates predefinidos y test por fixture;
- guardrails para acciones destructivas (`move_stage` hacia `won|lost` requiere opt-in explícito).
**Razón:** El crecimiento de automatizaciones exige evitar loops, reducir ruido operativo, asegurar trazabilidad de decisiones y preservar aislación por tenant sin sacrificar configurabilidad.
**Alternativas descartadas:**
- Reglas sin control de frecuencia/ventana (riesgo de re-disparo y saturación).
- Acciones arbitrarias sin DSL validada (alto riesgo de side effects cross-module).
- Auditoría basada solo en logs de infraestructura no estructurados (baja capacidad de diagnóstico/efectividad).

## ADR-66: Propuestas y onboarding enterprise sobre lifecycle explícito y playbooks segmentados
**Decisión:** Completar PO-01..PO-13 endureciendo ambos módulos sobre agregados ricos y endpoints operativos explícitos.
- Propuestas se modelan con template versionado persistido (`ProposalTemplate`), lifecycle `Draft|Sent|Viewed|Signed|Expired|Renewed`, firma electrónica liviana, renovación encadenada y KPIs de conversión `proposal->won`.
- Onboarding se modela con `Customer` segmentado (`Segment`, `PlaybookKey`, `HealthScore`) y tareas con dependencias/due dates para representar playbooks por tipo de cliente.
- La salud del onboarding se recalcula dentro del servicio de aplicación y gobierna la transición a `Active|AtRisk|ChurnRisk`.
**Razón:** La post-venta y el cierre comercial ya no pueden operar como flujos lineales mínimos; necesitan versionado contractual, trazabilidad del engagement y playbooks adaptativos que soporten segmentación SaaS sin mover la lógica fuera de Application/Domain.
**Alternativas descartadas:**
- Mantener proposals como documento plano sin versionado ni lifecycle granular.
- Resolver onboarding con listas de tareas estáticas sin dependencias ni health/lifecycle explícito.
- Delegar KPIs y churn risk a reportes offline en lugar de exponerlos como contratos operativos de la API.

## ADR-67: FE-15 determinista con orquestación E2E desde Playwright
**Decisión:** Endurecer la suite E2E de frontend para que se autoejecute sin precondiciones manuales:
- `playwright.config.ts` arranca backend y frontend automáticamente (`webServer`).
- El frontend E2E usa puerto dedicado `3100` para evitar colisiones locales.
- Se deshabilita `reuseExistingServer` para impedir reutilizar apps no relacionadas.
- El backend de pruebas se fuerza a `ASPNETCORE_ENVIRONMENT=Development`.
- CORS de desarrollo incluye `localhost/127.0.0.1:3100`.
- El cliente API frontend tolera respuestas 200 con body vacío para evitar fallos de parseo JSON en acciones operativas.
**Razón:** En este workspace coexistían runtimes en puertos por defecto, lo que hacía la suite no determinista y frágil por dependencia de estado externo. La orquestación declarativa en Playwright elimina ese acoplamiento y deja FE-15 reproducible en entorno limpio.
**Alternativas descartadas:**
- Mantener ejecución E2E dependiente de runtime manual.
- Reutilizar cualquier servidor existente (`reuseExistingServer: true`) en puertos compartidos.
- Resolver estabilidad solo con scripts ad-hoc externos sin encapsular reglas en configuración de pruebas.

## ADR-68: Feature Flags respaldados por IConfiguration con resolución por tenant
**Decisión:** Implementar feature flags mediante `IFeatureFlagService` / `ConfigurationFeatureFlagService` usando `IConfiguration` como fuente, con resolución en tres niveles: `Features:Tenants:{tenantId}:{flagName}` → `Features:{flagName}` → `false`.
**Razón:** Permite controlar features por entorno (staging vs producción) vía `appsettings.{Env}.json` sin dependencia de servicios externos ni infraestructura adicional. Los overrides por tenant permiten canary rollout sin cambios de código.
**Alternativas descartadas:**
- Feature flag service externo (LaunchDarkly, Azure App Configuration) — sobrecosto para la fase actual.
- Flags como constantes en código — sin capacidad de cambio en runtime por entorno.
- Flags en base de datos — complejidad operacional innecesaria para el volumen actual.
**Nota técnica:** Dentro de `Api.Infrastructure.FeatureFlags`, la clase `FeatureFlags` debe referenciarse con nombre completamente calificado `Application.Common.FeatureFlags.FeatureFlags.*` para evitar colisión con el namespace que la contiene.

## ADR-69: CI/CD con blue/green deployment, aprobaciones por entorno y auto-rollback
**Decisión:** Adoptar estrategia de blue/green deployment mediante GitHub Environments con:
- Staging: despliegue automático post-merge a main, con health gate + smoke tests.
- Production: aprobación manual requerida antes del slot swap; auto-rollback si smoke tests fallan post-swap.
- Rollback automatizado: workflow `rollback-health.yml` sondea `/health/ready` cada 10 min en horario laboral y ejecuta rollback + abre issue si se detecta degradación.
- DORA metrics: colecta automática tras cada despliegue con append a `docs/metrics/dora-history.csv`.
**Razón:** El modelo blue/green garantiza zero-downtime en production deployments. La aprobación manual en producción agrega un control humano explícito. El rollback automatizado reduce el MTTR cerrando el ciclo de detección → remediación sin intervención manual.
**Alternativas descartadas:**
- Rolling updates sin blue/green — introduce tiempo de indisponibilidad parcial durante el despliegue.
- Canary por porcentaje de tráfico — requiere infraestructura de balanceo más compleja que la disponible actualmente.
- Sin gate de smoke tests — deja despliegues sin verificación funcional post-swap.

## ADR-70: Gobernanza documental con contratos versionados y auditoria mensual doc-codigo
**Decisión:** Institucionalizar la documentacion de producto y operacion mediante un set minimo obligatorio de artefactos versionados y auditables:
- OpenAPI versionado por release (`docs/api/v{major}/openapi.json`).
- Runbooks por modulo y playbook de incidentes por severidad.
- Catalogo vivo de eventos de dominio y diccionario de datos.
- Matriz RBAC publicada y revisada periodicamente.
- Contribution guide, coding standards y changelog tecnico por release.
- Dashboard de KPIs y Definition of Done por tipo de feature.
- Auditoria mensual automatizada de coherencia doc-codigo.
**Razón:** El crecimiento del sistema y la ejecucion por olas funcionales incrementaron el riesgo de drift entre implementacion y conocimiento operativo. Un baseline documental obligatorio reduce tiempos de onboarding, acelera respuesta a incidentes y mejora trazabilidad de decisiones.
**Alternativas descartadas:**
- Mantener documentacion ad-hoc por modulo sin control transversal.
- Centralizar solo en archivos `ia/*` sin artefactos operativos en `docs/`.
- Auditoria manual esporadica sin automatizacion en CI.

## ADR-71: Estrategia híbrida de migraciones EF Core con compatibilidad legacy
**Decisión:** Adoptar migraciones EF Core como mecanismo oficial de evolución de esquema (baseline `M0001_Baseline`), manteniendo temporalmente un fallback de bootstrap legacy para bases históricas sin `__EFMigrationsHistory`.
**Razón:** El sistema acumuló cambios de esquema mediante SQL idempotente en `Program.cs`, lo que dificulta trazabilidad y rollback formal. La estrategia híbrida permite habilitar migraciones sin romper entornos existentes.
**Implementación:**
- Baseline migration generada en `backend/src/Api/Migrations/20260504145649_M0001_Baseline.cs`.
- Arranque actualizado para intentar `dbContext.Database.Migrate()` primero.
- Baselining de `__EFMigrationsHistory` para entornos heredados.
- Runbook operativo publicado en `docs/operations/db-migrations-runbook.md`.
**Alternativas descartadas:**
- Corte abrupto eliminando bootstrap legacy de inmediato (riesgo alto en entornos heredados).
- Mantener sólo SQL inline sin migraciones (sin gobierno de esquema versionado).

## ADR-72: Selección de PostgreSQL como provider objetivo de producción multiusuario
**Decisión:** Adoptar PostgreSQL como base de datos objetivo para producción SaaS de MindFlow.
**Razón:** En la PoC ejecutada (`sqlite`, `sqlserver`, `postgres`) PostgreSQL mostró el mejor p95 en respuestas esperadas dentro del mismo perfil de carga smoke, con throughput equivalente y mejor portabilidad operativa multi-plataforma.
**Evidencia de benchmark:**
- `backend/tests/LoadTests/results/db-poc-sqlite-smoke-20260504-151000.json`
- `backend/tests/LoadTests/results/db-poc-sqlserver-smoke-20260504-151221.json`
- `backend/tests/LoadTests/results/db-poc-postgres-smoke-20260504-151314.json`
**Plan de adopción:** Ejecutar cutover por fases según `docs/operations/db-provider-cutover-plan.md`.
**Alternativas descartadas:**
- Mantener SQLite en producción (limitaciones de concurrencia para escala SaaS).
- SQL Server como opción principal (resultado cercano, pero menor portabilidad y costo operativo esperado superior en el contexto actual).
**Nota operacional:** Los timeouts observados en endpoints analíticos pesados fueron comunes a los tres providers en smoke; se tratan como iniciativa de optimización de consultas/índices separada de esta decisión.

## ADR-73: Capa de datos frontend con React Query y claves tipadas por dominio
**Decisión:** Adoptar `@tanstack/react-query` v5 como capa de gestión de estado asíncrono del frontend, con un `QueryProvider` global en `layout.tsx`, claves tipadas centralizadas en `queryKeys.ts` y hooks por dominio (`usePipelineQueries`, `useRulesQueries`, `useEmailLogsQuery`, `useDashboardOverviewQuery`).
**Parámetros de configuración:** `staleTime: 15s`, `gcTime: 5min`, `retry: 1`, `refetchOnWindowFocus: false`.
**Razón:** Elimina llamadas `apiClient` inline en componentes, proporciona deduplicación automática, caché consistente y base para optimistic updates sin implementar reducers manuales. El tipado de claves por dominio previene colisiones de caché entre módulos.
**Alternativas descartadas:**
- Estado local con `useState` + `useEffect` por componente (repetición, sin deduplicación, sin gestión de stale data).
- Redux Toolkit Query (mayor configuración inicial; React Query es suficiente para el modelo feature-based).
- SWR (menor ecosistema y sin optimistic update nativo tan maduro en v2).

## ADR-74: Optimistic updates con rollback en pipeline y rules engine frontend
**Decisión:** Implementar optimistic UI en `useMoveOpportunityMutation` (mueve tarjeta en KanbanBoard sin esperar servidor) y `useToggleRuleMutation` (flip de `isActive` instantáneo), con rollback automático a snapshot previo via `onError` de React Query.
**Razón:** El flujo de pipeline es de alta frecuencia de interacción — el usuario necesita respuesta visual inmediata. Los optimistic updates con rollback son el patrón recomendado de React Query para mantener UX fluida sin comprometer consistencia eventual.
**Alternativas descartadas:**
- Esperar respuesta del servidor antes de actualizar UI (latencia perceptible en drag-and-drop Kanban).
- Actualizar sin rollback (inconsistencia de estado visible al usuario ante errores de red).

## ADR-75: ConfirmDialog accesible como reemplazo de `window.confirm`
**Decisión:** Reemplazar todas las llamadas a `window.confirm` por el componente `ConfirmDialog` (`frontend/components/ui/ConfirmDialog.tsx`), que implementa focus trap (Tab/Shift+Tab), cierra con Escape, confirma con Enter, y declara `role="dialog"`, `aria-modal`, `aria-labelledby`, `aria-describedby`.
**Razón:** `window.confirm` no es accesible por teclado/lector de pantalla, no puede estilizarse con el design system y bloquea el hilo del browser. WCAG 2.1 AA requiere que los diálogos sean completamente accesibles. `Button` fue convertido a `forwardRef` para permitir que `ConfirmDialog` gestione el foco en el botón de cancelación al abrir.
**Alternativas descartadas:**
- Librería de UI con diálogos (Radix, Headless UI) — overhead de dependencia no justificado para un único componente; la implementación manual cumple con todos los requisitos ARIA con ~80 líneas.
- Mantener `window.confirm` (falla gates de accesibilidad axe-core, no compatible con test E2E determinista).

## ADR-76: DOMPurify para sanitización de HTML en preview de templates de email
**Decisión:** Sanitizar todo HTML recibido del backend antes de `dangerouslySetInnerHTML` usando `sanitizeHtml()` (`frontend/services/htmlSanitizer.ts`) que envuelve DOMPurify con `USE_PROFILES: { html: true }` y bloqueo explícito de `FORBID_TAGS: ['script','style','iframe']` y `FORBID_ATTR: ['onerror','onload','onclick']`.
**Razón:** OWASP Top 10 A03 (Injection / XSS) — el backend puede persistir HTML de templates ingresados por usuarios con permisos de administración. Sin sanitización, cualquier template malicioso podría ejecutar JS en el contexto de la aplicación SaaS. DOMPurify es el estándar de facto para sanitización DOM-aware en browser.
**Alternativas descartadas:**
- No sanitizar y confiar en que el backend valide entradas (defensa en profundidad requiere sanitización en capa de renderizado también).
- Usar `innerText` en lugar de `innerHTML` (pierde toda la capacidad de preview de HTML del template).
- Sanitización con regex (frágil e insuficiente contra vectores DOM-based XSS).

## ADR-77: Paginación server-side para email logs con debounce en búsqueda
**Decisión:** Migrar el módulo de email logs de filtrado client-side (todos los registros cargados) a paginación server-side: `GET /api/email/logs?page={n}&pageSize={n}&search={q}`. Frontend usa `useEmailLogsQuery` con `placeholderData` (React Query) para transiciones suaves entre páginas y debounce de 300ms en el input de búsqueda.
**Razón:** Con volumen operativo real, cargar todos los logs en cliente no escala. La paginación server-side acota latencia de respuesta y tamaño de payload independientemente del crecimiento de datos. El `placeholderData` evita parpadeos de UI al cambiar de página.
**Backend:** `EmailController.GetEmailLogs` extendido con `[FromQuery] string? search` aplicado como filtro case-insensitive sobre `TemplateName`, `Status`, `ToEmail` antes de paginación SQL.
**Alternativas descartadas:**
- Mantener carga completa con filtrado en cliente (degradación lineal con volumen).
- Cursor-based pagination (complejidad innecesaria para este caso de uso; offset pagination es suficiente en esta etapa).

## ADR-78: Gates de calidad CI para a11y, contratos FE-BE y regresión visual
**Decisión:** Añadir tres gates secuenciales al pipeline `.github/workflows/quality-fullstack.yml` ejecutados post-smoke: `Accessibility gate` (axe-core, WCAG 2.1 AA), `FE-BE contract gate` (validación de shapes de respuesta contra interfaces TypeScript) y `Visual regression gate` (Playwright screenshot diff `maxDiffPixelRatio: 0.03`).
**Razón:** Los gates automatizan verificación de calidad en dimensiones que no se capturan con unit/integration tests: (1) accesibilidad real renderizada en browser, (2) drift entre contrato backend y consumo frontend, (3) regresiones visuales silenciosas en componentes. Todos los gates ejecutan en CI para bloquear merges regresivos.
**Scripts npm:** `test:e2e:a11y`, `test:e2e:contracts`, `test:e2e:visual`.
**Alternativas descartadas:**
- Revisión manual de accesibilidad (no escala, introduce variabilidad humana).
- Contratos FE-BE validados solo con OpenAPI sin gate E2E (no verifica consumo real desde el frontend).
- Regresión visual sin umbral definido (demasiado ruidoso o demasiado permisivo).

