Novamind Global — Sistema de Ventas Automatizadas 

Documento Maestro (Versión FULL – Integrado) 

 

1. VISIÓN 

El Sistema de Ventas Automatizadas (SVA) es un motor diseñado para: 

Capturar leads automáticamente 

Calificarlos con inteligencia 

Ejecutar el proceso de ventas sin intervención manual 

Convertir leads en clientes de forma sistemática 

No es un CRM tradicional. Es un sistema orientado a generar ingresos, altamente automatizado. 

 

2. PRINCIPIOS DE DISEÑO 

Event-driven 

Automated-first (human-in-the-loop solo para control) 

Multi-tenant (SaaS ready) 

Configurable mediante reglas 

Escalable 

UI moderna, elegante, minimalista y estratégica 

 

3. ARQUITECTURA GENERAL 

Lead Intake → Procesamiento → Deduplicación → Scoring → Asignación → Pipeline → Automatización → Cierre → Post-venta 

 

4. MÓDULOS DEL SISTEMA 

4.1 Lead Intake (API) 

Función 

Centralizar la entrada de leads desde cualquier canal. 

Características 

Endpoint POST /api/leads/intake 

Validación y normalización 

Identificación de fuente 

Logging 

❌ No incluye UI (entrada machine-to-machine). 

 

4.2 Deduplicación 

Comparación por email 

Comparación por teléfono 

Fuzzy matching 

Merge automático / manual 

 

4.3 Scoring Engine 

Reglas de scoring 

Cálculo automático por eventos 

Persistencia del score 

 

4.4 Assignment Engine 

Round robin 

Asignación por reglas (industria, país, score) 

SLA de contacto 

 

4.5 Pipeline (UI) 

UI tipo Kanban 

Etapas configurables 

Movimiento de oportunidades 

Historial de cambios 

✅ UI operativa principal del sistema. 

 

4.6 Rules Engine (CORE DEL SISTEMA – UI) 

Modelo: Trigger → Condition → Action 

UI de configuración 

CRUD de reglas 

Activar / desactivar reglas 

Visualización clara de lógica 

✅ Permite programar el negocio sin código. 

 

4.7 Automatización (Background Jobs) 

Jobs diferidos y recurrentes 

Hangfire u orquestador similar 

Logs técnicos 

❌ No incluye UI operativa. 

 

4.8 Gestión de Email (UI de Administración) 

Esta sección define la UI de configuración de correo, no un cliente de email. 

4.8.1 SMTP Configuration UI 

Función 

Configurar el proveedor SMTP por tenant. 

Campos 

SMTP Host 

Port 

Encryption (None / SSL / TLS) 

Username 

Password (almacenado cifrado) 

From Name 

From Email 

Acciones 

Guardar configuración 

Probar conexión (send test email) 

Activar / desactivar SMTP 

 

4.8.2 Email Templates UI 

Función 

Administrar los templates utilizados por las automatizaciones. 

Estructura 

Nombre 

Tipo (Welcome, Follow-up, Reminder, Proposal, etc.) 

Subject 

Body (HTML / editor simple) 

Variables dinámicas ({{lead.name}}, {{company.name}}) 

Estado (Active / Inactive) 

❗ Los templates solo se ejecutan vía reglas, no manualmente. 

 

4.8.3 Email Logs (Read-only UI) 

Fecha 

Lead asociado 

Template utilizado 

Resultado (Sent / Failed) 

Error (si aplica) 

 

4.9 Propuestas 

Templates 

Generación de PDF 

Envío automático 

Versionado 

 

4.10 Onboarding 

Conversión Won → Customer 

Tareas internas 

Secuencia de bienvenida 

 

4.11 Analytics (UI) 

Leads por día 

Conversión 

Valor de pipeline 

Tiempo promedio por etapa 

 

5. AUTOMATIZACIONES CLAVE 

Email inmediato al intake 

Follow-ups automáticos (24h, 48h) 

Alertas de estancamiento 

Recordatorios de propuestas 

Creación automática de cliente 

 

6. UI SCOPE – RESUMEN 

Módulo 

UI 

Lead Intake 

❌ No 

Automatizaciones 

❌ No 

Pipeline 

✅ Sí 

Rules Engine 

✅ Sí 

Email Configuración 

✅ Sí 

Dashboard 

✅ Sí 

 

7. STACK 

Backend 

.NET Core 

SQL Server 

Hangfire 

Frontend 

Next.js 

Infra 

Azure 

 

8. CONCLUSIÓN 

El sistema combina: 

API-first 

Automatización real 

UI poderosa 

Resultado: 

✅ Menos operación manual 

✅ Más conversión 

✅ Producto SaaS escalable 