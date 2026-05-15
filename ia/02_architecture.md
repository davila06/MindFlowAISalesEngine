# 02 — Arquitectura del Sistema

> **Última actualización:** 2026-05-03
> **Scope:** MindFlow AI Sales Engine

## Propósito

Describir la arquitectura técnica objetivo del MVP y la evolución posterior del sistema de ventas automatizadas, manteniendo alineación con el modelo event-driven, modular y SaaS-ready definido para NovaMind.

## Scope y non-scope

### Scope
- Backend modular para leads, pipeline, scoring, rules, email, analytics y tenancy.
- Frontend con UI para pipeline, rules engine, email config y dashboard.
- Automatización mediante jobs en background.
- Preparación para multi-tenant y roles.

### Non-scope del MVP inicial
- UI para Lead Intake.
- UI operativa para runtime de automatizaciones.
- Multi-tenant full y onboarding completo antes del Sprint 4.

## Pipeline principal

```text
Lead Source
  -> POST /api/leads/intake
  -> Validation + Normalization
  -> Deduplication
  -> Lead/Contact/Company Persistence
  -> Domain Events
    -> Automatic Email Queue
    -> Scoring
    -> Follow-up Scheduling Policy
    -> Assignment
      -> Opportunity Creation/Update
  -> Pipeline UI
  -> Rules Engine
      -> Actions (email, assign, move stage, create task)
  -> Closing
  -> Customer + Onboarding
```

## Componentes clave

| Componente | Responsabilidad | Ubicación objetivo |
|-----------|-----------------|--------------------|
| API | Exponer endpoints HTTP y middleware | `/backend/src/Api` |
| Leads Module | Intake, normalización, deduplicación, persistencia inicial | `/backend/src/Application/Leads`, `/backend/src/Domain/Leads` |
| Contacts/Companies | Entidades comerciales relacionadas | `/backend/src/Domain/Leads` o módulo dedicado |
| Pipeline Module | Oportunidades, etapas, historial y movimiento | `/backend/src/Application/Pipeline`, `/backend/src/Domain/Pipeline` |
| Email Module | Provider settings (`smtp|webhook`), templates versionados, stop-list, dispatch queue, logs y KPIs de entrega | `/backend/src/Application/Email`, `/backend/src/Domain/Email`, `/backend/src/Infrastructure/Email` |
| Scoring Engine | Cálculo y persistencia de score por eventos | `/backend/src/Application/Analytics` o módulo dedicado de scoring |
| Assignment Engine | Round robin y asignación por reglas | `/backend/src/Application/Users` o módulo dedicado |
| Rules Engine | Trigger, condition, action + dispatcher | `/backend/src/Application/RulesEngine`, `/backend/src/Domain/Rules` |
| Jobs Runtime | Follow-ups y ejecución diferida/recurrente | `/backend/src/Infrastructure/Jobs`, `/backend/src/BackgroundJobs/Hangfire` |
| Dashboard/Analytics | Métricas y reportes | `/frontend/app/dashboard`, servicios backend de analytics |
| Tenancy | Contexto y aislamiento por tenant | `/backend/src/Api/Middleware`, `/backend/src/Domain/Tenancy` |

## Arquitectura por capa

### Backend
- **Api**: controladores delgados, middleware de tenant, auth y errores.
- **Application**: commands, queries, handlers y servicios de coordinación.
- **Domain**: entidades, reglas del negocio y eventos del dominio.
- **Infrastructure**: EF Core, repositorios, SMTP, jobs y observabilidad.

### Frontend
- **App Router** para rutas principales.
- **Services** para acceso a API.
- **Hooks** para tenant, auth y permisos.
- **Components** segmentados por feature.

### Infraestructura
- Bicep modular para App Service, SQL, Storage y Key Vault.
- Pipelines separados para backend, frontend y release.

## Data Flow y Event Flow

### Eventos iniciales del dominio
- `lead.created`
- `lead.updated`
- `lead.responded`
- `lead.scored`
- `opportunity.stage_changed`
- `proposal.sent`
- `opportunity.won`

### Reacciones esperadas
- `lead.created` -> email automático, scoring, posible asignación, creación de oportunidad.
- `lead.created` -> queue de bienvenida, scoring, cálculo de policy/quiet hours para follow-up y posterior asignación.
- `lead.responded` -> cancelación de follow-up pendiente.
- `opportunity.stage_changed` -> reevaluación de reglas y métricas.
- `opportunity.won` -> conversión a cliente y onboarding.

## Modelo técnico mínimo

### Entidades base
- `Lead`
- `Contact`
- `Company`
- `Opportunity`
- `PipelineStage`
- `User`
- `ScoreRule`
- `Rule`
- `RuleCondition`
- `RuleAction`
- `EmailTemplate`
- `EmailLog`
- `EmailDispatchJob`
- `EmailStopListEntry`
- `SmtpSettings`
- `FollowUpPolicySettings`
- `Customer`

### Campos transversales esperados
- `Id`
- `TenantId` en entidades SaaS-ready
- `CreatedAt`
- `UpdatedAt`
- metadatos operativos según módulo

## Contratos críticos

### Endpoints mínimos del MVP
- `POST /api/leads/intake`
- `GET /api/pipeline/stages`
- `GET /api/pipeline/board`
- `GET /api/pipeline/board/export`
- `GET /api/pipeline/stage-sla-alerts`
- `GET /api/pipeline/throughput`
- `GET /api/pipeline/wip-limits`
- `PUT /api/pipeline/wip-limits/{stageId}`
- `PATCH /api/pipeline/opportunities/{id}/stage`
- `GET /api/pipeline/opportunities/{id}/history`
- endpoints CRUD de reglas
- endpoints de configuración SMTP y templates
- endpoints operativos de email (`dispatch/execute-due`, `logs/{id}/retry`, `kpis`, `stop-list`, `templates/{key}/versions|preview|rollback`)
- endpoints de policy de follow-up (`GET/PUT /api/followup/policy`)
- endpoints de dashboard básico

### Capacidades enterprise agregadas sobre Email y Follow-up
- Cola persistente `EmailDispatchJobs` con correlación `EmailLog.CorrelationId` para ejecución fuera del request original.
- Providers desacoplados por metadata (`ProviderType`, `ProviderBaseUrl`, `ApiKey`) con compatibilidad SMTP y webhook.
- Templates versionados por tenant (`Version`, `IsCurrent`, `RequiredVariables`) con preview y rollback.
- Stop-list persistente para supresión de comunicaciones salientes y follow-ups.
- Políticas de follow-up por tenant con reglas basadas en score y quiet hours.
- KPIs de entrega de email y alertado reutilizando infraestructura de `AlertThreshold`/`AlertEvent`.

### UI operativa de Email
- Ruta de configuración SMTP/email con selector de provider y campos webhook.
- Ruta de templates con consola admin para `publish version`, `preview` y `rollback`.
- La UI se mantiene sobria y utilitaria, integrada al módulo existente y alineada al service layer del frontend.

### Capacidades enterprise agregadas sobre Pipeline
- Board filtrable y ordenable por `owner`, `source`, `score`, `risk`, `value`, `createdAt`, `updatedAt`.
- Paginación virtual del board (`page`, `pageSize`, `totalCount`, `hasMore`) para alto volumen.
- Export CSV del board respetando el mismo slice de filtros operativos.
- WIP limits configurables por etapa y tenant con enforcement en creación y movimientos.
- Historial enriquecido append-only con `actor`, `reason`, `fromStageName`, `toStageName`, `isAutomated`.
- Integración `pipeline.stage.changed` en el Rules Engine para auto-move auditable.
- Concurrencia optimista en cambios de etapa mediante `ExpectedVersionToken`.

### Contrato conceptual de regla

```text
Rule
  - Trigger
  - Conditions[]
  - Actions[]
  - IsActive
  - TenantId
```

## Dependencias

- .NET backend con persistencia relacional.
- Next.js frontend.
- Hangfire u orquestador equivalente para jobs.
- Proveedor SMTP.
- Azure App Service, SQL, Storage y Key Vault.

## Analytics avanzado (TASK-FULL-13)

### Catálogo de KPIs avanzados
- Funnel: `NewCount`, `QualifiedCount`, `ProposalCount`, `WonCount`, `NewToQualifiedRate`, `QualifiedToProposalRate`, `ProposalToWonRate`.
- Revenue: `WonRevenue`, `PipelineRevenue`, `AverageDealSize`, `Currency`.
- Velocity: `AverageHoursToQualified`, `AverageHoursToProposal`, `AverageHoursToWon`.
- SLA operativo: `AssignmentWithinSlaRate`, `FirstResponseWithinSlaRate`, `SlaBreaches`.
- Activación onboarding: `NewCustomers`, `ActivatedCustomers`, `ActivationRate`, `AverageHoursToFirstActivation`.

### Fórmulas base
- `NewToQualifiedRate = QualifiedCount / max(NewCount, 1) * 100`.
- `QualifiedToProposalRate = ProposalCount / max(QualifiedCount, 1) * 100`.
- `ProposalToWonRate = WonCount / max(ProposalCount, 1) * 100`.
- `AverageDealSize = WonRevenue / max(WonCount, 1)`.
- `ActivationRate = ActivatedCustomers / max(NewCustomers, 1) * 100`.

### Filtros y agrupación estándar
- Filtros: `StartDateUtc`, `EndDateUtc`, `Stage`, `Source`, `Tenant`.
- Agrupación: `GroupBy` (`day`, `week`, `month`).

### Contratos backend definidos
- `Api.Contracts.Analytics.AnalyticsAdvancedQuery`.
- `Api.Contracts.Analytics.AnalyticsAdvancedOverviewResponse`.
- `Api.Contracts.Analytics.FunnelKpiResponse`.
- `Api.Contracts.Analytics.RevenueKpiResponse`.
- `Api.Contracts.Analytics.VelocityKpiResponse`.
- `Api.Contracts.Analytics.SlaKpiResponse`.
- `Api.Contracts.Analytics.OnboardingActivationKpiResponse`.
- `Api.Contracts.Analytics.AnalyticsBacklogItemResponse`.

### Priorización de endpoints (implementación incremental)
1. `GET /api/analytics/advanced/overview`.
2. `GET /api/analytics/advanced/funnel`.
3. `GET /api/analytics/advanced/revenue`.
4. `GET /api/analytics/advanced/velocity`.
5. `GET /api/analytics/advanced/sla`.
6. `GET /api/analytics/advanced/onboarding-activation`.

## Riesgos y restricciones

- Si deduplicación queda vaga, se crearán registros inconsistentes.
- Si Rules Engine se implementa sin modelo claro, el sistema perderá configurabilidad.
- Si tenant isolation se deja para muy tarde sin diseñarlo desde el inicio, el costo de migración crecerá.
- Si la UI excede el scope definido, se diluye la propuesta automated-first.