PRINCIPIOS DE LA ESTRUCTURA
La estructura debe cumplir:

✅ Separación clara de responsabilidades
✅ Escalable a multi‑tenant SaaS
✅ Fácil de mantener por equipos
✅ Compatible con .NET + Next.js + Azure
✅ Clara distinción entre core business y UI


1️⃣ BACKEND (.NET – API + Core)
Arquitectura recomendada: Clean Architecture / Modular Monolith
/backend
│
├── src
│   ├── Api
│   │   ├── Controllers
│   │   │   ├── LeadsController.cs
│   │   │   ├── PipelineController.cs
│   │   │   ├── RulesController.cs
│   │   │   ├── EmailController.cs          # SMTP + Templates
│   │   │   └── AdminController.cs
│   │   │
│   │   ├── Middleware
│   │   │   ├── TenantMiddleware.cs
│   │   │   ├── AuthMiddleware.cs
│   │   │   └── ErrorHandlingMiddleware.cs
│   │   │
│   │   ├── Filters
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── Application
│   │   ├── Common
│   │   │   ├── Interfaces
│   │   │   ├── DTOs
│   │   │   └── Exceptions
│   │   │
│   │   ├── Leads
│   │   │   ├── Commands
│   │   │   ├── Queries
│   │   │   └── Handlers
│   │   │
│   │   ├── Pipeline
│   │   ├── RulesEngine
│   │   ├── Email
│   │   ├── Analytics
│   │   └── Users
│   │
│   ├── Domain
│   │   ├── Leads
│   │   │   ├── Lead.cs
│   │   │   └── Events
│   │   │
│   │   ├── Pipeline
│   │   ├── Rules
│   │   ├── Email
│   │   │   ├── EmailTemplate.cs
│   │   │   ├── SmtpSettings.cs
│   │   │   └── EmailLog.cs
│   │   │
│   │   ├── Users
│   │   └── Tenancy
│   │
│   ├── Infrastructure
│   │   ├── Persistence
│   │   │   ├── DbContext
│   │   │   ├── Migrations
│   │   │   └── Repositories
│   │   │
│   │   ├── Email
│   │   │   ├── SmtpClientFactory.cs
│   │   │   └── EmailSender.cs
│   │   │
│   │   ├── Jobs
│   │   │   ├── FollowUpJob.cs
│   │   │   └── RuleExecutionJob.cs
│   │   │
│   │   ├── Security
│   │   └── Observability
│   │
│   └── BackgroundJobs
│       └── Hangfire
│
├── tests
│   ├── Application.Tests
│   ├── Domain.Tests
│   └── Api.Tests
│
└── README.md

💡 Clave aquí

Email es un módulo formal (no util)
Rules Engine vive en Application + Domain
No hay lógica en Controllers
Multi‑tenant se maneja como cross‑cutting concern


2️⃣ FRONTEND (Next.js – UI)
Arquitectura: feature‑based, no “pages‑only”.
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


3️⃣ INFRAESTRUCTURA / DEVOPS
/infra
│
├── bicep
│   ├── appservice.bicep
│   ├── sql.bicep
│   ├── storage.bicep
│   └── keyvault.bicep
│
├── pipelines
│   ├── backend-ci.yml
│   ├── frontend-ci.yml
│   └── release.yml
│
├── scripts
│   ├── seed-db.sql
│   └── migrate.sh
│
└── README.md


4️⃣ DOCUMENTACIÓN
/docs
│
├── architecture
│   ├── system-overview.md
│   ├── rules-engine.md
│   └── email-architecture.md
│
├── api
│   └── openapi.yaml
│
├── ui
│   ├── pipeline.md
│   ├── rules-ui.md
│   └── email-ui.md
│
└── product
    └── roadmap.md


✅ RESUMEN EJECUTIVO

Backend: Clean Architecture + módulos claros
Frontend: Feature‑based, no caótico
Email: primer nivel (no parche)
UI: solo donde agrega control y valor
Listo para SaaS real, no “app interna”