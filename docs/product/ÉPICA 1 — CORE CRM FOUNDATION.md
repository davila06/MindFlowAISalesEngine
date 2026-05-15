**ÉPICA 1 — CORE CRM FOUNDATION**

**🎯 Objetivo**



**Tener una base funcional para capturar y gestionar leads.**



**📦 Feature 1.1 — Lead Management**

**User Story**



**Como sistema, quiero capturar leads desde múltiples fuentes para iniciar el proceso comercial.**



**Tasks**

&#x20;**Crear endpoint POST /api/leads/intake**

&#x20;**Validar payload (email, teléfono)**

&#x20;**Normalizar datos**

&#x20;**Guardar en DB**

&#x20;**Agregar logging**

&#x20;**Manejo de errores**

**📦 Feature 1.2 — Contact \& Company**

**User Story**



**Como sistema, quiero asociar leads a contactos y empresas.**



**Tasks**

&#x20;**Crear tablas Contacts, Companies**

&#x20;**Relación Lead → Contact → Company**

&#x20;**CRUD endpoints**

&#x20;**Validación de duplicados**

**📦 Feature 1.3 — Pipeline básico**

**User Story**



**Como usuario, quiero ver el estado del lead en un pipeline.**



**Tasks**

&#x20;**Crear tabla PipelineStages**

&#x20;**Crear tabla Opportunities**

&#x20;**Endpoint cambio de etapa**

&#x20;**UI tipo Kanban (básico)**

&#x20;**Historial de cambios**

**⚙️ ÉPICA 2 — AUTOMATIZACIÓN INICIAL**

**🎯 Objetivo**



**Eliminar seguimiento manual básico.**



**📦 Feature 2.1 — Email automático**

**User Story**



**Como sistema, quiero enviar un email al recibir un lead.**



**Tasks**

&#x20;**Integrar SMTP (ej: Google Workspace)**

&#x20;**Crear template base**

&#x20;**Trigger en lead.created**

&#x20;**Manejo de errores**

**📦 Feature 2.2 — Follow-up automático**

**User Story**



**Como sistema, quiero enviar seguimiento si el lead no responde.**



**Tasks**

&#x20;**Integrar Hangfire**

&#x20;**Crear job diferido (48h)**

&#x20;**Cancelar job si responde**

&#x20;**Logs de ejecución**

**📦 Feature 2.3 — Asignación automática**

**User Story**



**Como sistema, quiero asignar leads automáticamente.**



**Tasks**

&#x20;**Implementar round robin**

&#x20;**Tabla Users**

&#x20;**Lógica de asignación**

&#x20;**Registro de asignación**

**🧠 ÉPICA 3 — INTELIGENCIA (SCORING)**

**🎯 Objetivo**



**Priorizar leads automáticamente.**



**📦 Feature 3.1 — Scoring Engine**

**User Story**



**Como sistema, quiero calcular score de leads.**



**Tasks**

&#x20;**Crear tabla ScoreRules**

&#x20;**Implementar cálculo**

&#x20;**Trigger en eventos**

&#x20;**Guardar score**

**📦 Feature 3.2 — Prioridad automática**

**Tasks**

&#x20;**Definir thresholds**

&#x20;**Marcar leads calientes**

&#x20;**Ordenar pipeline por score**

**🔁 ÉPICA 4 — RULES ENGINE (CORE DEL SISTEMA)**

**🎯 Objetivo**



**Hacer el sistema configurable.**



**📦 Feature 4.1 — Modelo de reglas**

**Tasks**

&#x20;**Tabla Rules**

&#x20;**Tabla Conditions**

&#x20;**Tabla Actions**

&#x20;**JSON schema para reglas**

**📦 Feature 4.2 — Motor de ejecución**

**User Story**



**Como sistema, quiero ejecutar acciones según reglas.**



**Tasks**

&#x20;**Listener de eventos**

&#x20;**Evaluador de condiciones**

&#x20;**Dispatcher de acciones**

&#x20;**Logs de ejecución**

**📦 Feature 4.3 — UI de configuración**

**Tasks**

&#x20;**CRUD reglas**

&#x20;**Activar/desactivar reglas**

&#x20;**Visualización simple**

**📊 ÉPICA 5 — ANALYTICS**

**🎯 Objetivo**



**Medir lo que pasa.**



**📦 Feature 5.1 — Dashboard básico**

**Tasks**

&#x20;**Leads por día**

&#x20;**Conversión**

&#x20;**Pipeline value**

**📦 Feature 5.2 — Reportes**

**Tasks**

&#x20;**Export CSV**

&#x20;**Reporte semanal automático**

&#x20;**Métricas por vendedor**

**🧾 ÉPICA 6 — PROPUESTAS**

**🎯 Objetivo**



**Automatizar cierre.**



**📦 Feature 6.1 — Generación de propuestas**

**Tasks**

&#x20;**Templates**

&#x20;**Generación PDF**

&#x20;**Envío por email**

**📦 Feature 6.2 — Seguimiento**

**Tasks**

&#x20;**Recordatorio automático**

&#x20;**Tracking estado**

**🚀 ÉPICA 7 — ONBOARDING**

**🎯 Objetivo**



**Automatizar post-venta.**



**📦 Feature 7.1 — Cliente**

**Tasks**

&#x20;**Crear entidad Customer**

&#x20;**Conversión automática (Won → Customer)**

**📦 Feature 7.2 — Inicio automático**

**Tasks**

&#x20;**Crear tareas onboarding**

&#x20;**Email bienvenida**

&#x20;**Activar tracking**

**🧱 ÉPICA 8 — MULTI-TENANT (VERSIÓN FULL)**

**🎯 Objetivo**



**Convertirlo en producto SaaS.**



**📦 Feature 8.1 — Tenant isolation**

**Tasks**

&#x20;**TenantId en todas las tablas**

&#x20;**Middleware de contexto**

&#x20;**Seguridad por tenant**

**📦 Feature 8.2 — Roles**

**Tasks**

&#x20;**Admin / Sales / Viewer**

&#x20;**Permisos**

**⚠️ PRIORIDAD REAL (ORDEN CORRECTO)**



**No construyas todo a la vez.**



**Sprint 1 (SEMANA 1–2)**

**Leads intake**

**Pipeline básico**

**Email automático**

**Sprint 2 (SEMANA 3–4)**

**Follow-ups**

**Asignación**

**Scoring básico**

**Sprint 3 (SEMANA 5–6)**

**Rules engine básico**

**Dashboard**

**Sprint 4+ (FULL)**

**Multi-tenant**

**Propuestas**

**Onboarding**

**Analytics avanzado**

