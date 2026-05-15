# 00 — Contexto del Proyecto

> **Última actualización:** 2026-04-28
> **Scope:** C:\NovaMind - MindFlow AI sales engine

## Identidad del proyecto
**Empresa:** NovaMind  
**Proyecto:** MindFlow AI Sales Engine  
**Propósito:** Construir un sistema de ventas automatizadas orientado a ingresos, con intake API-first, scoring, asignación, pipeline, automatización y evolución a SaaS multi-tenant.

## Stack tecnológico
| Capa | Tecnología |
|------|-----------|
| Backend | .NET Core (Clean Architecture / Modular Monolith) |
| Persistencia | SQL Server (EF Core) |
| Jobs | Hangfire |
| Frontend | Next.js (App Router) |
| Infra | Azure (App Service, SQL, Storage, Key Vault) |
| IaC/DevOps | Bicep + pipelines CI/CD |

## Principios de diseño
- Event-driven.
- Automated-first (human-in-the-loop solo para control).
- Multi-tenant SaaS-ready.
- Configurable por reglas (Trigger -> Condition -> Action).
- Escalable y mantenible por equipos.
- UI estratégica: solo donde agrega control operativo.

## Invariantes del sistema
- Lead Intake no tiene UI operativa.
- No hay lógica de negocio en controllers.
- Email es módulo formal, no utility.
- Rules Engine es core del producto.
- Background automation no expone UI operativa.
- Templates de email se ejecutan por reglas, no manualmente.
- El aislamiento de tenant es obligatorio en la evolución full.

## Flujo E2E objetivo
Lead Intake -> Processing -> Deduplication -> Scoring -> Assignment -> Pipeline -> Automation -> Closing -> Post-sale Onboarding.

## Artefactos fuente en /ia
- `01_requirements.md`: requisitos funcionales y no funcionales.
- `02_architecture.md`: arquitectura, módulos, eventos y contratos base.
- `03_plan.md`: fases y sprints.
- `04_tasks.md`: tareas accionables con dependencias.
- `05_progress.md`: avance cronológico.
- `06_decisions.md`: decisiones arquitectónicas (ADRs).
- `07_issues.md`: issues conocidos (pendiente de poblar).
- `08_retrospective.md`: retrospectiva por fase (pendiente de poblar).

## Estado actual
- Requisitos, arquitectura, plan y tareas base del MVP ya definidos.
- Siguiente foco: ejecución de Sprint 1 (Lead Intake, Contact/Company base, Pipeline básico, Email automático).
