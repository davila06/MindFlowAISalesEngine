# MindFlow CC - Documento Integral del Sistema

> Fecha: 2026-05-04
> Proyecto: NovaMind - MindFlow AI Sales Engine
> Version del documento: 1.0

## 1. Resumen Ejecutivo

MindFlow es un motor de ventas automatizadas orientado a ingresos. No se plantea como un CRM tradicional centrado solo en registro de datos, sino como un sistema que ejecuta el ciclo comercial completo con minima intervencion manual.

Su objetivo es convertir mas oportunidades en clientes mediante:
- Captura estructurada de leads.
- Calificacion automatica.
- Asignacion inteligente.
- Orquestacion del pipeline.
- Automatizaciones por reglas y jobs.
- Cierre comercial y onboarding post-venta.
- Observabilidad operativa y gobierno del producto.

## 2. Que Hace el Sistema

MindFlow cubre de forma integrada el flujo comercial de punta a punta:

1. Recibe leads desde integraciones externas por API.
2. Valida, normaliza y deduplica informacion.
3. Enriquece el lead con score y prioridad.
4. Asigna automaticamente a un owner comercial.
5. Crea/mueve oportunidades en pipeline (modelo Kanban operativo).
6. Ejecuta reglas de negocio configurables (trigger -> condition -> action).
7. Automatiza comunicaciones (email, follow-ups, recordatorios, supresion por stop-list).
8. Gestiona propuestas y su ciclo de vida (versiones, seguimiento, renovacion).
9. Convierte oportunidades ganadas en clientes y dispara onboarding.
10. Expone analitica operacional y avanzada para decision comercial y SRE.

## 3. Para Que Sirve

MindFlow sirve para escalar una operacion comercial con disciplina operativa y automatizacion.

### Beneficios principales
- Incrementar conversion comercial reduciendo tiempos muertos.
- Disminuir carga manual en seguimiento y tareas repetitivas.
- Asegurar trazabilidad de decisiones y acciones automaticas.
- Estandarizar playbooks comerciales entre equipos y tenants.
- Habilitar operacion SaaS multi-tenant con aislamiento por tenant y control por rol.
- Mejorar visibilidad con KPIs de funnel, revenue, velocity, SLA y onboarding.

### Casos de uso tipicos
- Equipos de ventas B2B con alto volumen de leads.
- Operaciones multi-equipo o multi-sede con reglas diferenciadas.
- Empresas que necesitan pasar de un CRM pasivo a una maquina de revenue.
- Operaciones que requieren gobernanza documental, auditoria y CI/CD robusto.

## 4. Como Funciona (Flujo Operativo)

## 4.1 Flujo E2E Base

Lead Source -> Intake API -> Validacion/Normalizacion -> Deduplicacion -> Persistencia -> Eventos -> Score -> Follow-up policy -> Assignment -> Opportunity/Pipeline -> Rules Engine -> Proposal -> Won -> Customer + Onboarding

## 4.2 Paso a paso

### Paso 1: Ingreso de lead
- Endpoint canonico: POST /api/leads/intake.
- El sistema valida payload y normaliza campos clave (email, telefono, metadata fuente).
- Se aplican controles de duplicidad y politicas de merge/reprocess.

### Paso 2: Persistencia y contexto comercial
- Se persiste informacion de lead y su relacion con Contact y Company.
- El modelo conserva trazabilidad de origen, cambios y ownership.

### Paso 3: Calculo de valor comercial
- Se ejecuta scoring por reglas (basico y evolucionado).
- El lead se clasifica por prioridad para acelerar foco comercial.

### Paso 4: Asignacion y SLA
- Se aplica estrategia de asignacion (round-robin y reglas por atributos).
- Se audita asignacion para trazabilidad y metricas.

### Paso 5: Pipeline operativo
- Se crea o actualiza oportunidad en etapas del pipeline.
- La UI de pipeline es el centro operativo para ventas.
- Cada cambio de etapa deja historial append-only auditable.

### Paso 6: Automatizacion por reglas
- Rules Engine evalua reglas activas por trigger, condiciones y acciones.
- Puede disparar acciones como scoring adicional, prioridad, movimiento de etapa o comunicaciones.

### Paso 7: Email y follow-up
- Los envios se gestionan por cola de dispatch para resiliencia.
- Se soporta provider SMTP o webhook.
- Se aplican retry, quiet hours, stop-list y politicas por tenant.

### Paso 8: Cierre y post-venta
- Al llegar a Won, se activa conversion a Customer.
- Se crean tareas de onboarding y se activa secuencia de bienvenida.

### Paso 9: Medicion y operacion
- Dashboard y analytics avanzados exponen KPIs clave.
- Observabilidad operativa detecta degradacion, alerts y salud de jobs.

## 5. Principios de Diseno

- Event-driven architecture.
- Automated-first (human-in-the-loop solo para control/override).
- Multi-tenant SaaS readiness.
- Behavior configurable por reglas, no hardcodeado.
- Escalabilidad y mantenibilidad como baseline.
- UI estrategica: solo donde mejora control operativo.

## 6. Arquitectura Tecnica

## 6.1 Vista de alto nivel

La plataforma esta organizada como Modular Monolith con Clean Architecture en backend, frontend feature-based en Next.js, y una capa de infraestructura/devops en Azure con IaC y pipelines.

## 6.2 Backend (Clean Architecture / Modular Monolith)

### Capas
- API: controladores HTTP, middleware, contratos de entrada/salida, health endpoints.
- Application: servicios, casos de uso, coordinacion de flujo y reglas de aplicacion.
- Domain: entidades, invariantes, eventos de dominio, logica de negocio central.
- Infrastructure: EF Core, repositorios, providers de email, jobs, persistencia y observabilidad.

### Modulos funcionales principales
- Leads intake y deduplicacion.
- Contacts y Companies.
- Pipeline y stage history.
- Assignment y SLA.
- Scoring.
- Rules Engine.
- Email + Follow-up.
- Proposals.
- Onboarding.
- Analytics y Observability.
- Ops (SRE summary, tenant capacity, config audit, job alerts).

### Persistencia
- EF Core como ORM.
- Evolucion formal con migraciones baseline y estrategia migration-aware.
- Compatibilidad de provider para PoC y operacion controlada: sqlite, sqlserver, postgres.
- Decision tecnica vigente para target productivo multiusuario: PostgreSQL.

## 6.3 Frontend (Next.js, feature-based)

### Principios
- UI solo para modulos operativos de alto impacto.
- Consumo por service layer desacoplado de vistas.
- Hooks de tenant/auth/permisos.
- Estado y comportamiento orientado a flujos operativos.

### Superficies UI clave
- Pipeline Kanban.
- Rules UI (CRUD, activacion, pruebas de reglas).
- Email Admin (providers, templates, stop-list, retries).
- Dashboard y Analytics avanzado.

### No-scope intencional de UI
- No se expone UI para intake machine-to-machine.
- No se expone runtime interno de jobs como consola operativa principal.

## 6.4 Arquitectura de eventos y automatizacion

### Eventos de dominio representativos
- lead.created
- lead.updated
- lead.responded
- lead.scored
- opportunity.stage_changed
- proposal.sent
- opportunity.won

### Runtime de automatizacion
- Ejecucion diferida/recurrente para follow-ups y dispatch.
- Politicas por tenant (quiet hours, supresion, thresholds).
- Logs tecnicos y de negocio para auditoria y post-mortem.

## 6.5 Infraestructura y DevOps

### Infra objetivo
- Azure App Service para backend/frontend.
- Base de datos relacional gestionada.
- Storage y Key Vault para secretos y activos operativos.
- IaC con Bicep.

### DevOps implementado
- CI fullstack con quality gates (tests, cobertura, type-check, budget).
- CD con staging/production, health gate y rollback.
- Workflows de DORA metrics, dependency review y backup verification.
- Scripts operativos de smoke, rollback, backup y restore.

## 6.6 Seguridad, tenancy y gobierno

### Seguridad y aislamiento
- Modelo de roles base: Admin, Sales, Viewer.
- Evolucion hacia aislamiento tenant completo.
- Controles de acceso por middleware y reglas de escritura/lectura.

### Gobierno tecnico
- ADRs para decisiones estructurales.
- Runbooks operativos por modulo.
- Politicas de incidentes, dependencias y patching.
- Auditoria de coherencia doc-codigo.

## 6.7 Observabilidad y calidad

### Observabilidad
- Health endpoints live/ready.
- KPIs de operacion y alertado.
- Metricas de latencia, error y ejecucion de jobs.
- Analitica avanzada para funnel/revenue/velocity/SLA/onboarding.

### Calidad y testing
- Cobertura amplia de pruebas backend por modulos.
- E2E frontend para flujos criticos.
- Load tests para endpoints sensibles de analytics y email/follow-up.
- Evidencia de release y verificacion en progreso tecnico.

## 7. Arquitectura Logica por Capacidades

1. Intake Layer
- Ingestion API-first con validacion, normalizacion y deduplicacion.

2. Orchestration Layer
- Coordinacion de scoring, assignment, pipeline y rules.

3. Automation Layer
- Jobs de follow-up y dispatch con politicas por tenant.

4. Commercial Execution Layer
- Pipeline, propuestas y cierre a customer/onboarding.

5. Intelligence Layer
- Analytics operativo y avanzado para decision comercial.

6. Platform Layer
- Observabilidad, seguridad, tenancy, CI/CD, backup y DR.

## 8. Estado Actual del Sistema

- El sistema se encuentra en Fase activa de expansion Full SaaS.
- Se han consolidado olas de arquitectura, reglas, propuestas/onboarding, observabilidad, devops y documentacion de gobierno.
- Se formalizo estrategia de migraciones y comparativa de provider DB.
- Se mantiene seleccion de PostgreSQL como provider objetivo productivo.
- Existen frentes de optimizacion en endpoints analytics pesados bajo carga extrema, ya identificados y documentados.

## 9. Roadmap Tecnico Inmediato Recomendado

1. Optimizar endpoints analytics de alta carga (consultas, indices, materializacion incremental).
2. Completar hardening de seguridad enterprise (authn/authz avanzada, secretos y auditoria ampliada).
3. Consolidar baseline productivo multi-tenant full con politicas por tenant mas granulares.
4. Continuar mejora de performance end-to-end en escenarios stress.
5. Mantener disciplina de evidencia: ADR + tasks + progress + runbooks por cada ola nueva.

## 10. Conclusiones

MindFlow es una plataforma de ventas automatizadas con enfoque operacional, diseno modular y capacidad real de evolucion SaaS. Su valor diferencial esta en convertir un flujo comercial complejo en un pipeline automatizado, medible y gobernable.

La arquitectura tecnica actual permite seguir escalando funcionalidad sin perder control de calidad, trazabilidad ni coherencia de producto, siempre que se mantenga el enfoque event-driven, automated-first y documentacion viva alineada a implementacion.