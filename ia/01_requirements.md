# 01 — Requisitos del Sistema

> **Última actualización:** 2026-04-28
> **Fuentes:** ÉPICA 1 — CORE CRM FOUNDATION.md; .github/skills/novamind-system-master/SKILL.md

## Propósito del sistema

MindFlow AI Sales Engine es un sistema de ventas automatizadas orientado a ingresos. Su objetivo es capturar leads, calificarlos, asignarlos, moverlos por un pipeline, automatizar seguimientos y convertir oportunidades en clientes con mínima intervención manual.

## Reglas fundamentales de negocio

- Lead Intake es API-first y no tiene UI operativa.
- Cada lead debe poder asociarse a contacto y empresa.
- La deduplicación debe considerar email, teléfono y coincidencia difusa antes de crear nuevos registros.
- El score del lead debe recalcularse por eventos relevantes del negocio.
- La asignación debe soportar round robin y reglas por atributos como industria, país y score.
- El pipeline es la UI operativa principal y debe mantener historial de cambios.
- El Rules Engine define automatizaciones mediante Trigger -> Condition -> Action.
- Los templates de email se ejecutan vía reglas; no se envían manualmente desde UI.
- Los jobs de automatización son diferidos o recurrentes y deben dejar trazabilidad técnica.
- El sistema debe evolucionar a aislamiento multi-tenant completo con permisos por rol.

## Requisitos funcionales

### RF-01 Lead Intake
- Exponer `POST /api/leads/intake` para recibir leads desde múltiples fuentes.
- Validar campos mínimos del payload, al menos email y/o teléfono según canal.
- Normalizar email, teléfono y metadatos de fuente antes de persistir.
- Registrar logging y errores de intake.

### RF-02 Contactos y Empresas
- Crear y mantener entidades `Contact` y `Company`.
- Permitir relación `Lead -> Contact -> Company`.
- Exponer operaciones CRUD para estas entidades según rol.
- Validar duplicados antes de crear contactos o empresas nuevos.

### RF-03 Pipeline Comercial
- Mantener catálogo de `PipelineStages` configurables.
- Crear `Opportunities` asociadas a leads.
- Permitir cambio de etapa desde endpoint y UI.
- Mantener historial de cambios por oportunidad.
- Exponer vista Kanban básica para usuarios de ventas.

### RF-04 Automatización Inicial de Email
- Enviar email automático al crearse un lead válido.
- Soportar configuración SMTP por tenant.
- Crear al menos un template base para automatizaciones.
- Registrar errores de entrega y logs de ejecución.

### RF-05 Follow-up Automático
- Programar seguimiento automático si el lead no responde.
- Crear job diferido inicial a 48 horas.
- Cancelar el job si el lead responde o cambia de estado relevante.
- Guardar logs de ejecución del job.

### RF-06 Asignación Automática
- Implementar round robin como estrategia base.
- Mantener tabla o catálogo de usuarios asignables.
- Guardar el registro de la asignación realizada.

### RF-07 Scoring Engine
- Permitir definir reglas de scoring.
- Calcular score automáticamente por eventos del sistema.
- Persistir el score actual del lead.
- Marcar prioridades o thresholds para leads calientes.

### RF-08 Rules Engine
- Mantener modelo de reglas con entidades de reglas, condiciones y acciones.
- Soportar esquema estructurado para representar reglas.
- Escuchar eventos del sistema, evaluar condiciones y despachar acciones.
- Registrar logs de ejecución por regla.
- Exponer UI para CRUD y activación/desactivación.

### RF-09 Analytics
- Mostrar leads por día, conversión y valor del pipeline en un dashboard básico.
- Permitir exportación CSV.
- Generar reportes semanales automáticos.
- Exponer métricas por vendedor.

### RF-10 Propuestas
- Gestionar templates de propuestas.
- Generar PDF.
- Enviar propuestas por email.
- Crear recordatorios automáticos y tracking de estado.

### RF-11 Onboarding
- Crear entidad `Customer`.
- Convertir automáticamente una oportunidad ganada en cliente.
- Crear tareas de onboarding.
- Enviar email de bienvenida.
- Activar tracking post-venta.

### RF-12 Multi-Tenant y Roles
- Agregar `TenantId` en todas las tablas de dominio necesarias.
- Resolver contexto de tenant por middleware.
- Aplicar seguridad y aislamiento por tenant.
- Soportar roles base `Admin`, `Sales` y `Viewer` con permisos diferenciados.

## Requisitos no funcionales

- Arquitectura modular y escalable para SaaS multi-tenant.
- Diseño event-driven para scoring, automatización y rules engine.
- Trazabilidad de errores, jobs y acciones automáticas.
- UI solo donde agrega control operativo o de configuración.
- Seguridad de credenciales mediante almacenamiento seguro y cifrado.
- Mantenibilidad por equipos separados de backend, frontend, infra y docs.

## Flujos principales

### Flujo F-01: Lead Intake a Pipeline
**Estado de entrada:** lead externo recibido
**Estado de salida:** oportunidad creada en etapa inicial del pipeline

1. El sistema recibe un lead desde `POST /api/leads/intake`.
2. Valida y normaliza payload.
3. Verifica duplicados.
4. Persiste lead, contacto y empresa según corresponda.
5. Dispara eventos para email automático, scoring y posible asignación.
6. Crea o actualiza oportunidad en el pipeline.

### Flujo F-02: Seguimiento Automático
**Estado de entrada:** lead sin respuesta
**Estado de salida:** follow-up enviado o job cancelado

1. Se programa job de seguimiento al cumplir condición temporal.
2. El sistema verifica si el lead sigue elegible.
3. Si aplica, envía template de seguimiento.
4. Si el lead respondió o cambió de estado, cancela el job.

### Flujo F-03: Ejecución de Reglas
**Estado de entrada:** evento de negocio emitido
**Estado de salida:** acciones ejecutadas y registradas

1. Un listener recibe un evento del dominio.
2. Se cargan reglas activas del tenant.
3. Se evalúan condiciones.
4. Se despachan acciones como email, asignación, cambio de etapa o tareas.
5. Se registran logs de ejecución.

## Criterios de aceptación iniciales

- Sprint 1 debe dejar funcional: intake, pipeline básico y email automático.
- Sprint 2 debe dejar funcional: follow-ups, asignación y scoring básico.
- Sprint 3 debe dejar funcional: rules engine básico y dashboard.
- Lo no cubierto por Sprint 3 queda fuera del MVP inicial.