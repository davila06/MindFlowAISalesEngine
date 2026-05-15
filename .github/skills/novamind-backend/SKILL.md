---
name: novamind-backend
description: Architecture and coding conventions for the NovaMind MindFlow AI Sales Engine .NET backend. Enforces Clean Architecture / Modular Monolith, multi-tenant design, and domain module boundaries. WHEN: create backend feature, add endpoint, create module, add lead logic, implement pipeline, build rules engine, add email module, configure SMTP, write background job, add Hangfire job, multi-tenant middleware, create domain entity, write repository, add application handler, implement scoring, assignment engine, deduplication, analytics, NovaMind backend, MindFlow backend.
invocable: false
---

# NovaMind Backend — Architecture & Conventions

## Project Context

NovaMind MindFlow is an **automated sales engine** — not a traditional CRM.  
Goal: capture leads → qualify with intelligence → execute sales without manual intervention.

Stack: **.NET Core** · **SQL Server / EF Core** · **Hangfire** · **Azure App Service** · **Azure Key Vault**

---

## Non-Negotiable Principles

1. **No logic in Controllers** — Controllers are thin HTTP adapters only (validate → dispatch → return).
2. **Email is a first-class module** — lives in `Domain/Email`, `Application/Email`, `Infrastructure/Email`. Never a utility.
3. **Rules Engine lives in `Application/RulesEngine` + `Domain/Rules`** — it is the core of the system.
4. **Multi-tenancy is a cross-cutting concern** — resolved in `TenantMiddleware`, injected via `ITenantContext`, never hardcoded in business logic.
5. **Event-driven inside the domain** — use domain events (`Lead.Created`, `Lead.StageChanged`, etc.) to trigger side effects.
6. **Background jobs are infrastructure** — Hangfire lives in `Infrastructure/Jobs` + `BackgroundJobs/Hangfire`. Never call SMTP directly from a domain handler synchronously when a job can own it.

---

## Canonical Folder Structure

```
/backend/src/
├── Api/
│   ├── Controllers/          # Thin. Dispatch commands/queries only.
│   │   ├── LeadsController.cs
│   │   ├── PipelineController.cs
│   │   ├── RulesController.cs
│   │   ├── EmailController.cs      # SMTP config + templates
│   │   └── AdminController.cs
│   ├── Middleware/
│   │   ├── TenantMiddleware.cs     # Resolves ITenantContext per request
│   │   ├── AuthMiddleware.cs
│   │   └── ErrorHandlingMiddleware.cs
│   ├── Filters/
│   ├── Program.cs
│   └── appsettings.json            # No secrets here — use Key Vault
│
├── Application/
│   ├── Common/
│   │   ├── Interfaces/             # IRepository<T>, IEmailSender, ITenantContext …
│   │   ├── DTOs/
│   │   └── Exceptions/             # DomainException, NotFoundException, ConflictException
│   ├── Leads/
│   │   ├── Commands/               # CreateLeadCommand, UpdateLeadCommand …
│   │   ├── Queries/                # GetLeadByIdQuery, ListLeadsQuery …
│   │   └── Handlers/               # MediatR handlers
│   ├── Pipeline/
│   ├── RulesEngine/                # Rule evaluation service lives here
│   ├── Email/
│   ├── Analytics/
│   └── Users/
│
├── Domain/
│   ├── Leads/
│   │   ├── Lead.cs                 # Aggregate root
│   │   └── Events/                 # LeadCreatedEvent, LeadScoredEvent …
│   ├── Pipeline/
│   │   ├── PipelineStage.cs
│   │   └── Opportunity.cs
│   ├── Rules/
│   │   ├── Rule.cs                 # Trigger + Conditions + Actions
│   │   └── RuleExecution.cs
│   ├── Email/
│   │   ├── EmailTemplate.cs
│   │   ├── SmtpSettings.cs         # Per-tenant
│   │   └── EmailLog.cs
│   ├── Users/
│   └── Tenancy/
│       └── Tenant.cs
│
├── Infrastructure/
│   ├── Persistence/
│   │   ├── DbContext/              # AppDbContext with multi-tenant query filters
│   │   ├── Migrations/
│   │   └── Repositories/
│   ├── Email/
│   │   ├── SmtpClientFactory.cs    # Builds SMTP client from tenant SmtpSettings
│   │   └── EmailSender.cs          # Implements IEmailSender
│   ├── Jobs/
│   │   ├── FollowUpJob.cs
│   │   └── RuleExecutionJob.cs
│   ├── Security/
│   └── Observability/
│
└── BackgroundJobs/
    └── Hangfire/                   # Dashboard config, recurring job registration
```

---

## Module Rules

### Leads Module
- Entry point: `POST /api/leads/intake` — validate → normalize → deduplicate → save → publish `LeadCreatedEvent`.
- Deduplication: compare by email → phone → fuzzy match. Merge or flag.
- Scoring: triggered by domain events (not by direct calls). Result stored on `Lead.Score`.
- Assignment: resolved after scoring. Supports round-robin and rule-based (industry, country, score threshold).

### Pipeline Module
- `Opportunity` is the moving unit — not `Lead` directly.
- `PipelineStage` rows are tenant-configurable.
- Stage changes emit `OpportunityStageChangedEvent`.
- History stored in an `OpportunityHistory` table (append-only).

### Rules Engine Module (Core)
Model: **Trigger → Condition → Action**

```csharp
// Domain/Rules/Rule.cs
public class Rule
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public RuleTrigger Trigger { get; private set; }       // e.g. LeadCreated, StageChanged
    public IReadOnlyList<RuleCondition> Conditions { get; private set; }
    public IReadOnlyList<RuleAction> Actions { get; private set; }
}
```

- Rules are evaluated in `Application/RulesEngine/RuleEvaluationService.cs`.
- Actions dispatch commands (SendEmail, AssignLead, MoveStage, CreateTask …).
- Rules are CRUD-managed via `RulesController` + Application layer; never hardcoded.

### Email Module
- `SmtpSettings` is per-tenant and stored encrypted.
- `EmailTemplate` is linked to automation rules — templates are **not** sent manually.
- `EmailLog` is append-only, read-only from UI.
- Test-connection action is a dedicated command (`TestSmtpConnectionCommand`).

### Multi-Tenancy Pattern
```csharp
// Application/Common/Interfaces/ITenantContext.cs
public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantSlug { get; }
}
```
- EF Core global query filters applied per entity: `.HasQueryFilter(x => x.TenantId == _tenantContext.TenantId)`.
- `TenantMiddleware` resolves from JWT claim or subdomain header, sets `ITenantContext` in DI scope.
- **Never pass `tenantId` as a parameter to application handlers** — always read from `ITenantContext`.

---

## Controller Pattern

```csharp
// Thin controller — dispatch only
[ApiController]
[Route("api/leads")]
public class LeadsController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeadsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("intake")]
    public async Task<IActionResult> Intake(
        [FromBody] LeadIntakeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateLeadCommand(request.Email, request.Phone, request.Source);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
```

---

## Background Jobs Pattern

```csharp
// Infrastructure/Jobs/FollowUpJob.cs
public class FollowUpJob
{
    private readonly IMediator _mediator;
    public FollowUpJob(IMediator mediator) => _mediator = mediator;

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid leadId, Guid tenantId)
    {
        await _mediator.Send(new SendFollowUpEmailCommand(leadId, tenantId));
    }
}
```
- Jobs are scheduled from Application handlers via `IBackgroundJobClient`.
- Jobs are **cancelled** (deleted from Hangfire) when the triggering condition is resolved.
- All jobs produce logs consumable from `EmailLog` or an `JobExecutionLog` table.

---

## Security Rules

- Secrets (SMTP passwords, JWT secrets, connection strings) → **Azure Key Vault only**. Never in `appsettings.json`.
- SMTP password → stored encrypted in DB (AES-256), decrypted only in `SmtpClientFactory`.
- All endpoints require authentication except `POST /api/leads/intake` (which uses an API key per tenant).
- API key validated in `AuthMiddleware`, not in controllers.

---

## Testing Conventions

- `Application.Tests` → unit test handlers with mocked interfaces.
- `Domain.Tests` → pure domain logic, no mocks.
- `Api.Tests` → integration tests with `WebApplicationFactory`.
- One test class per handler/service. Name: `{HandlerName}Tests`.

---

## Anti-Patterns to Avoid

| ❌ Don't | ✅ Do instead |
|---|---|
| Logic in controllers | Dispatch MediatR command/query |
| SMTP call in a domain handler | Enqueue Hangfire job from handler |
| Hardcode `tenantId` | Inject `ITenantContext` |
| Direct DB access from Application | Use repository interface |
| Secrets in appsettings.json | Azure Key Vault reference |
| Email as a utility folder | Email module in Domain + Application + Infrastructure |
| Skip domain events | Publish domain event, let handlers react |
