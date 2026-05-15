BACKEND (.NET – API + Core)
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

