# Guía Completa de Conexión y Consumo — MindFlow AI Sales Engine

> **Versión:** 1.0 — Mayo 2026  
> **Aplica a:** Frontend (Next.js 14+) y cualquier cliente HTTP externo que consuma la API REST del backend .NET

---

## Tabla de Contenidos

1. [Arquitectura general de comunicación](#1-arquitectura-general-de-comunicación)
2. [Configuración de entorno (variables de entorno)](#2-configuración-de-entorno-variables-de-entorno)
3. [CORS — Permitir orígenes en el backend](#3-cors--permitir-orígenes-en-el-backend)
4. [El cliente HTTP central (`apiClient`)](#4-el-cliente-http-central-apiclient)
5. [Cabeceras obligatorias en cada petición](#5-cabeceras-obligatorias-en-cada-petición)
6. [Autenticación y contexto de usuario](#6-autenticación-y-contexto-de-usuario)
7. [Multi-tenancy — cómo funciona el `X-Tenant-Id`](#7-multi-tenancy--cómo-funciona-el-x-tenant-id)
8. [Rate Limiting](#8-rate-limiting)
9. [Manejo de errores y respuestas](#9-manejo-de-errores-y-respuestas)
10. [Caso 1 — Leads (Ingreso de prospectos)](#10-caso-1--leads-ingreso-de-prospectos)
11. [Caso 2 — Pipeline (Tablero Kanban de oportunidades)](#11-caso-2--pipeline-tablero-kanban-de-oportunidades)
12. [Caso 3 — Rules Engine (Motor de reglas de automatización)](#12-caso-3--rules-engine-motor-de-reglas-de-automatización)
13. [Caso 4 — Email (SMTP, plantillas y logs)](#13-caso-4--email-smtp-plantillas-y-logs)
14. [Caso 5 — Dashboard y Analytics](#14-caso-5--dashboard-y-analytics)
15. [Caso 6 — Scoring de leads](#15-caso-6--scoring-de-leads)
16. [Caso 7 — Asignación de leads](#16-caso-7--asignación-de-leads)
17. [Caso 8 — Contactos y Empresas](#17-caso-8--contactos-y-empresas)
18. [Caso 9 — Propuestas](#18-caso-9--propuestas)
19. [Integración desde una página web externa (vanilla JS / cualquier framework)](#19-integración-desde-una-página-web-externa-vanilla-js--cualquier-framework)
20. [Patrones de query con React Query (`useQuery` y `useMutation`)](#20-patrones-de-query-con-react-query-usequery-y-usemutation)
21. [Ejecución local — levantar backend y frontend juntos](#21-ejecución-local--levantar-backend-y-frontend-juntos)
22. [Checklist de verificación de integración](#22-checklist-de-verificación-de-integración)

---

## 1. Arquitectura general de comunicación

```
┌─────────────────────────────────────────────────────────┐
│  Cliente (Página web / App Next.js / Script externo)    │
│                                                         │
│  browser → fetch() con cabeceras X-Tenant-Id,           │
│            X-Correlation-Id, Content-Type, Bearer       │
└──────────────────────┬──────────────────────────────────┘
                       │  HTTPS/HTTP   (REST JSON)
                       ▼
┌─────────────────────────────────────────────────────────┐
│  Backend .NET API  (puerto 5165 en local)                │
│  Route prefix: /api/...                                  │
│                                                         │
│  Middleware:                                            │
│   1. TenantMiddleware   → lee X-Tenant-Id               │
│   2. Rate Limiter       → 120 req/min por tenant/IP     │
│   3. JWT Auth (opcional en dev)                         │
│   4. Controllers        → lógica de negocio             │
└─────────────────────────────────────────────────────────┘
```

**Regla de oro:** Todo cliente (interno o externo) DEBE enviar siempre la cabecera `X-Tenant-Id`. Sin ella, el backend asigna el tenant `"default"` pero los datos quedarán mezclados en producción.

---

## 2. Configuración de entorno (variables de entorno)

### 2.1 Frontend Next.js

Crea o edita el archivo `.env.local` en la raíz de `frontend/`:

```env
# URL base del backend (sin barra final)
NEXT_PUBLIC_API_URL=http://localhost:5165

# Identificador del tenant para esta instancia del frontend
NEXT_PUBLIC_TENANT_ID=mi-empresa
```

- Las variables con prefijo `NEXT_PUBLIC_` son **expuestas al navegador** (cliente).
- En producción, asigna `NEXT_PUBLIC_API_URL` a la URL pública de tu backend, p. ej. `https://api.novamind.ai`.
- En producción, `NEXT_PUBLIC_TENANT_ID` debe ser el slug o UUID único de tu organización.

### 2.2 Backend .NET

Las URLs permitidas de CORS se configuran en `appsettings.json` (o sus variantes por entorno):

**`appsettings.Development.json`** (para local):
```json
{
  "Security": {
    "AllowedCorsOrigins": [
      "http://localhost:3000",
      "http://127.0.0.1:3000"
    ]
  }
}
```

**`appsettings.Production.json`** (para producción):
```json
{
  "Security": {
    "AllowedCorsOrigins": [
      "https://app.tudominio.com"
    ],
    "JwtIssuer": "novamind-production",
    "JwtAudience": "novamind-production-clients"
  }
}
```

---

## 3. CORS — Permitir orígenes en el backend

El backend configura CORS usando la política `"NovamindCors"`. Esta política permite **cualquier cabecera y método** pero **solo** los orígenes listados en `Security.AllowedCorsOrigins`.

Si al hacer una petición desde tu página ves el error:

```
Access to fetch at 'http://localhost:5165/api/...' from origin 'http://localhost:3000'
has been blocked by CORS policy
```

**Solución:** Agrega tu origen (`http://localhost:3000`) al array `AllowedCorsOrigins` en `appsettings.Development.json` del backend y reinicia el servidor.

---

## 4. El cliente HTTP central (`apiClient`)

Archivo: `frontend/services/apiClient.ts`

Este es el único punto de salida HTTP del frontend. Todos los servicios lo usan internamente. Sus responsabilidades son:

| Responsabilidad | Detalle |
|---|---|
| Base URL | Lee `NEXT_PUBLIC_API_URL` o usa `http://localhost:5165` como fallback |
| Timeout | 12 segundos por defecto (configurable con `timeoutMs`) |
| Cabeceras automáticas | `Content-Type: application/json`, `X-Tenant-Id`, `X-Correlation-Id` |
| Cancelación | Soporta `AbortSignal` para cancelar peticiones (útil con React Query) |
| Errores HTTP | Lanza `Error` con el código de estado y cuerpo de la respuesta |
| 204 No Content | Devuelve `undefined` sin intentar parsear JSON |

### Métodos disponibles

```typescript
import { apiClient } from "@/services/apiClient";

// GET
const data = await apiClient.get<TipoRespuesta>("/api/ruta");

// POST
const created = await apiClient.post<TipoRespuesta>("/api/ruta", { campo: "valor" });

// PUT
const updated = await apiClient.put<TipoRespuesta>("/api/ruta", { campo: "valor" });

// PATCH
const patched = await apiClient.patch<TipoRespuesta>("/api/ruta", { campo: "valor" });

// DELETE
await apiClient.delete("/api/ruta");
```

### Uso con timeout personalizado

```typescript
const data = await apiClient.get<TipoRespuesta>("/api/ruta-lenta", {
  timeoutMs: 30000 // 30 segundos
});
```

### Uso con AbortSignal (cancelación desde React Query)

```typescript
const data = await apiClient.get<TipoRespuesta>("/api/ruta", { signal });
// El signal viene automáticamente de React Query en la queryFn
```

---

## 5. Cabeceras obligatorias en cada petición

Cuando uses `apiClient`, estas cabeceras se agregan automáticamente. Si consumes la API desde un cliente externo (Postman, script, otra app), debes agregarlas manualmente:

| Cabecera | Valor | Obligatoria |
|---|---|---|
| `Content-Type` | `application/json` | Sí (para POST/PUT/PATCH) |
| `X-Tenant-Id` | ID o slug de tu organización | Sí |
| `X-Correlation-Id` | UUID único por petición (para trazabilidad) | Recomendada |
| `Authorization` | `Bearer <token_jwt>` | Requerida en producción |
| `Idempotency-Key` | UUID único (solo para `/api/leads/intake`) | Opcional pero recomendada |

### Ejemplo con fetch nativo (sin `apiClient`):

```javascript
const response = await fetch("http://localhost:5165/api/leads/intake", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "X-Tenant-Id": "mi-empresa",
    "X-Correlation-Id": crypto.randomUUID(),
    "Idempotency-Key": crypto.randomUUID()
  },
  body: JSON.stringify({
    email: "cliente@ejemplo.com",
    source: "web"
  })
});

const lead = await response.json();
console.log(lead);
```

---

## 6. Autenticación y contexto de usuario

### Estado actual (desarrollo local)

En desarrollo, `useAuth` (`frontend/hooks/useAuth.ts`) devuelve un usuario hardcodeado con rol `Admin`. El JWT no se requiere para las peticiones en entornos locales si el backend no tiene `[Authorize]` forzado.

```typescript
// hooks/useAuth.ts — comportamiento actual
const user = {
  id: "local-user",
  fullName: "Local Admin",
  email: "admin@mindflow.local",
  tenantId: process.env.NEXT_PUBLIC_TENANT_ID ?? "default",
  roles: ["Admin"],
  plan: "pro"
};
```

### En producción (JWT Bearer)

1. El cliente obtiene un JWT token del endpoint de autenticación.
2. Almacena el token de forma segura (memoria o `HttpOnly` cookie).
3. Lo agrega a cada petición como `Authorization: Bearer <token>`.
4. El backend valida el token usando los parámetros en `appsettings.Production.json`:
   - `JwtIssuer`: `"novamind-production"`
   - `JwtAudience`: `"novamind-production-clients"`
   - La clave de firma se almacena en `Security.JwtSigningKey` (nunca en el repositorio).

### Permisos por rol

Los permisos del frontend se calculan en `usePermissions`:

```typescript
import { usePermissions } from "@/hooks/usePermissions";

const { canManageRules, canConfigureSmtp, canViewLogs } = usePermissions();
```

| Permiso | Roles con acceso |
|---|---|
| `canManageRules` | Admin |
| `canConfigureSmtp` | Admin |
| `canViewLogs` | Admin, Sales |

---

## 7. Multi-tenancy — cómo funciona el `X-Tenant-Id`

Cada petición al backend incluye la cabecera `X-Tenant-Id`. El middleware de tenancy en el backend extrae este valor y lo inyecta en `TenantContext`, que se usa para filtrar todos los datos por tenant.

**Flujo:**
```
Request → TenantMiddleware (lee X-Tenant-Id) → TenantContext.TenantId
       → Controller → Service → Repository (filtra por TenantId)
```

Para obtener el `tenantId` en cualquier componente React:

```typescript
import { useTenant } from "@/hooks/useTenant";

const { tenantId, plan } = useTenant();
// tenantId viene de useAuth().user.tenantId
// que a su vez lee NEXT_PUBLIC_TENANT_ID
```

---

## 8. Rate Limiting

El backend aplica un límite de **120 peticiones por minuto** por `X-Tenant-Id` (o por IP si no hay tenant). Si superas este límite, recibirás:

```
HTTP 429 Too Many Requests
```

En ese caso, implementa un backoff exponencial en tu cliente:

```javascript
async function fetchWithRetry(url, options, maxRetries = 3) {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    const response = await fetch(url, options);
    if (response.status !== 429) return response;
    const delay = Math.pow(2, attempt) * 500; // 1s, 2s, 4s
    await new Promise(resolve => setTimeout(resolve, delay));
  }
  throw new Error("Demasiadas peticiones. Intenta más tarde.");
}
```

---

## 9. Manejo de errores y respuestas

### Estructura de error estándar

El backend devuelve errores con este esquema JSON:

```json
{
  "code": "VALIDATION_ERROR",
  "message": "One or more validation errors occurred.",
  "traceId": "00-abc123...",
  "validationErrors": {
    "email": ["El campo email no es válido."]
  }
}
```

### Cómo capturar errores en el frontend

El `apiClient` lanza un `Error` estándar cuyo `message` contiene `"<código> <statusText>: <cuerpo>"`.

```typescript
try {
  const lead = await leadsService.intake({ email: "test@ejemplo.com", source: "web" });
  console.log("Lead creado:", lead);
} catch (error) {
  if (error instanceof Error) {
    // Ejemplo: "400 Bad Request: {"code":"VALIDATION_ERROR",...}"
    console.error("Error al crear lead:", error.message);
  }
}
```

### Con React Query (manejo automático)

```typescript
const { data, isLoading, isError, error } = usePipelineBoardQuery();

if (isError) {
  // error.message contiene el detalle del HTTP error
  return <div>Error: {error.message}</div>;
}
```

---

## 10. Caso 1 — Leads (Ingreso de prospectos)

### Endpoint

```
POST /api/leads/intake
```

### Payload

```typescript
interface IntakeLeadPayload {
  email?: string;    // al menos email o phone son requeridos
  phone?: string;
  source: string;    // origen del lead: "web", "form", "landing", etc.
}
```

### Respuesta (201 Created)

```typescript
interface Lead {
  id: string;
  email?: string;
  phone?: string;
  source: string;
  score?: number;
}
```

### Ejemplo completo desde el frontend

```typescript
import { leadsService } from "@/services/leads.service";

// En un componente o handler:
const handleSubmit = async (formData: { email: string }) => {
  try {
    const lead = await leadsService.intake({
      email: formData.email,
      source: "landing-page"
    });
    console.log("Lead registrado con ID:", lead.id);
  } catch (error) {
    console.error("Falló el registro del lead:", error);
  }
};
```

### Idempotencia (evitar duplicados)

Para evitar crear el mismo lead dos veces si el usuario hace doble clic o hay un reintento de red, usa la cabecera `Idempotency-Key`:

```typescript
// Desde un cliente externo
await fetch("/api/leads/intake", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "X-Tenant-Id": "mi-empresa",
    "Idempotency-Key": "key-unica-por-envio-de-formulario"
  },
  body: JSON.stringify({ email: "cliente@ejemplo.com", source: "web" })
});
// Si repites la petición con la misma Idempotency-Key,
// el backend responde con el mismo Lead sin duplicarlo
// y agrega la cabecera X-Idempotent-Replay: true
```

---

## 11. Caso 2 — Pipeline (Tablero Kanban de oportunidades)

### Endpoints disponibles

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/pipeline/board` | Devuelve etapas + oportunidades |
| `GET` | `/api/pipeline/stages` | Solo las etapas del pipeline |
| `POST` | `/api/pipeline/opportunities` | Crea una nueva oportunidad |
| `PATCH` | `/api/pipeline/opportunities/{id}/stage` | Mueve una oportunidad a otra etapa |
| `GET` | `/api/pipeline/throughput` | Métricas de flujo |
| `GET` | `/api/pipeline/stage-sla-alerts` | Alertas de SLA por etapa |
| `GET` | `/api/pipeline/wip-limits` | Límites de trabajo en curso |
| `PUT` | `/api/pipeline/wip-limits/{stageId}` | Actualiza límite WIP de una etapa |

### Obtener el tablero completo

```typescript
import { pipelineService } from "@/services/pipeline.service";

const board = await pipelineService.getBoard();
// board.stages   → Array<{ id, name, order, color }>
// board.opportunities → Array<{ id, leadId, stageId, title, value }>
```

### Crear una oportunidad

```typescript
const opportunity = await pipelineService.createOpportunity({
  leadId: "uuid-del-lead",
  title: "Propuesta CRM Empresa ABC",
  value: 5000,
  stageId: "uuid-etapa-prospecto"
});
```

### Mover una oportunidad entre etapas (drag & drop)

```typescript
await pipelineService.moveOpportunity(
  "uuid-de-la-oportunidad",
  "uuid-etapa-destino"
);
```

### Uso con React Query (recomendado en componentes React)

```typescript
import { usePipelineBoardQuery, useMoveOpportunityMutation } from "@/hooks/queries/usePipelineQueries";

function PipelineBoard() {
  const { data: board, isLoading } = usePipelineBoardQuery();
  const moveOpportunity = useMoveOpportunityMutation();

  const handleDrop = (opportunityId: string, targetStageId: string) => {
    // Optimistic update incluido: la UI se actualiza antes de que responda el servidor
    moveOpportunity.mutate({ opportunityId, targetStageId });
  };

  if (isLoading) return <Spinner />;

  return (
    <KanbanBoard
      stages={board.stages}
      opportunities={board.opportunities}
      onDrop={handleDrop}
    />
  );
}
```

> **Nota sobre Optimistic Updates:** `useMoveOpportunityMutation` implementa actualización optimista. Cuando el usuario arrastra una tarjeta, la UI se actualiza inmediatamente. Si el servidor falla, la tarjeta vuelve a su posición original automáticamente.

---

## 12. Caso 3 — Rules Engine (Motor de reglas de automatización)

### Endpoints disponibles

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/rules` | Lista todas las reglas |
| `GET` | `/api/rules/{id}` | Obtiene una regla por ID |
| `POST` | `/api/rules` | Crea una nueva regla |
| `PUT` | `/api/rules/{id}` | Actualiza una regla existente |
| `POST` | `/api/rules/{id}/activate` | Activa una regla |
| `POST` | `/api/rules/{id}/deactivate` | Desactiva una regla |
| `GET` | `/api/rules/templates` | Plantillas de reglas predefinidas |
| `POST` | `/api/rules/test-fixture` | Simula una regla sobre un lead de prueba |
| `POST` | `/api/rules/{id}/rollback` | Revierte una regla a una versión anterior |

### Estructura de una regla

```typescript
interface Rule {
  id: string;
  name: string;
  trigger: string;          // "lead.created", "lead.stage_changed", etc.
  isActive: boolean;
  version?: number;
  priority?: number;        // menor número = mayor prioridad
  conflictPolicy?: string;  // "first-wins" | "highest-priority"
  cooldownMinutes?: number; // evita disparos repetidos en X minutos
  allowDestructiveActions?: boolean;
  conditions: Array<{
    field: string;          // "source", "score", "priority", "hasEmail"
    operator: "eq" | "gt" | "lt" | "contains";
    value: string;
  }>;
  actions: Array<{
    type: string;           // "assign_to_user", "send_email", "change_stage"
    value: string;
  }>;
}
```

### Crear una regla

```typescript
import { rulesService } from "@/services/rules.service";

const nuevaRegla = await rulesService.create({
  name: "Asignar leads de alto valor",
  trigger: "lead.created",
  isActive: true,
  priority: 1,
  conflictPolicy: "first-wins",
  cooldownMinutes: 60,
  allowDestructiveActions: false,
  conditions: [
    { field: "score", operator: "gt", value: "80" }
  ],
  actions: [
    { type: "assign_to_user", value: "uuid-del-vendedor-senior" }
  ]
});
```

### Probar una regla antes de activarla (test-fixture)

```typescript
const resultado = await rulesService.testFixture({
  ruleId: "uuid-de-la-regla",
  trigger: "lead.created",
  lead: {
    source: "web",
    priority: "high",
    score: 90,
    hasEmail: true,
    hasPhone: false
  }
});

// resultado.matched        → true si el lead cumple las condiciones
// resultado.applied        → true si las acciones se habrían ejecutado
// resultado.actionsApplied → ["assign_to_user: uuid-vendedor"]
// resultado.skippedReasons → [] (vacío si todo OK)
```

### Usando React Query en componentes

```typescript
import { useRulesQuery, useActivateRuleMutation } from "@/hooks/queries/useRulesQueries";

function RulesList() {
  const { data: rules, isLoading } = useRulesQuery();
  const activateRule = useActivateRuleMutation();

  return rules?.map(rule => (
    <RuleCard
      key={rule.id}
      rule={rule}
      onActivate={() => activateRule.mutate(rule.id)}
    />
  ));
}
```

---

## 13. Caso 4 — Email (SMTP, plantillas y logs)

### Endpoints disponibles

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/email/smtp-settings` | Obtiene la configuración SMTP activa |
| `PUT` | `/api/email/smtp-settings` | Guarda/actualiza la configuración SMTP |
| `GET` | `/api/email/logs` | Lista logs de correos enviados (paginado) |
| `POST` | `/api/email/templates/{key}/versions` | Crea nueva versión de una plantilla |
| `POST` | `/api/email/templates/{key}/preview` | Previsualiza una plantilla con variables |
| `POST` | `/api/email/templates/{key}/rollback` | Revierte plantilla a versión anterior |

### Configurar SMTP

```typescript
import { emailService } from "@/services/email.service";

await emailService.saveSmtp({
  providerType: "smtp",
  host: "smtp.gmail.com",
  port: 587,
  username: "notificaciones@miempresa.com",
  password: "mi-contraseña-segura",
  fromEmail: "notificaciones@miempresa.com",
  fromName: "MindFlow Notificaciones",
  enableSsl: true
});
```

> **Seguridad:** Nunca almacenes la contraseña SMTP en el código fuente. Usa variables de entorno en el servidor o Azure Key Vault en producción.

### Leer la configuración SMTP activa

```typescript
const config = await emailService.getSmtp();
// config.host, config.port, config.username, etc.
// config.password NO se devuelve por seguridad (campo omitido en la respuesta)
```

### Consultar logs de email con paginación

```typescript
const logsPage = await emailService.getLogs({
  page: 1,
  pageSize: 20,
  search: "cliente@ejemplo.com"  // filtra por email del destinatario
});

// logsPage.items  → EmailLog[]
// logsPage.hasMore → true si hay más páginas
```

### Variables de plantilla permitidas

Solo se aceptan las siguientes variables en las plantillas:

```
lead.name         lead.email        company.name
pipeline.stage    recipient_name    proposal_title
amount            currency          tracking_url
endpoint_name     metric_name       observed_value
threshold_value   triggered_at_utc
```

### Previsualizar una plantilla con variables

```typescript
const preview = await emailService.previewTemplate("welcome-lead", {
  variables: {
    "lead.name": "Carlos García",
    "lead.email": "carlos@empresa.com",
    "company.name": "Empresa XYZ"
  }
});

// preview.subject  → "Bienvenido Carlos García a MindFlow"
// preview.bodyHtml → HTML completo del correo renderizado
```

### Usando React Query para logs

```typescript
import { useEmailLogsQuery } from "@/hooks/queries/useEmailLogsQuery";

function EmailLogsTable() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");

  const { data, isLoading } = useEmailLogsQuery({ page, pageSize: 20, search });

  return (
    <Table>
      {data?.items.map(log => (
        <TableRow key={log.id}>
          <td>{log.toEmail}</td>
          <td>{log.status}</td>
          <td>{log.sentAtUtc}</td>
          <td>{log.errorMessage}</td>
        </TableRow>
      ))}
    </Table>
  );
}
```

---

## 14. Caso 5 — Dashboard y Analytics

### Endpoints disponibles

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/dashboard/overview?days=7` | Resumen del período indicado |
| `GET` | `/api/dashboard/overview/csv?days=7` | Exporta el resumen en CSV |
| `GET` | `/api/dashboard/data-quality` | Calidad de datos general |
| `GET` | `/api/dashboard/data-quality/anomalies` | Eventos anómalos detectados |

### Respuesta de overview

```typescript
interface DashboardOverview {
  totalLeads: number;
  conversionRate: number;     // porcentaje, ej. 0.23 = 23%
  pipelineValue: number;      // suma de valores de oportunidades activas
  leadsPerDay: Array<{
    date: string;             // ISO 8601, ej. "2026-05-10"
    count: number;
  }>;
}
```

### Consumo en componente React

```typescript
import { useDashboardOverviewQuery } from "@/hooks/queries/useDashboardOverviewQuery";

function DashboardKPIs() {
  const { data: overview, isLoading } = useDashboardOverviewQuery(30); // últimos 30 días

  if (isLoading) return <Skeleton />;

  return (
    <div>
      <KPICard label="Total Leads" value={overview.totalLeads} />
      <KPICard label="Tasa Conversión" value={`${(overview.conversionRate * 100).toFixed(1)}%`} />
      <KPICard label="Valor Pipeline" value={`$${overview.pipelineValue.toLocaleString()}`} />
    </div>
  );
}
```

### Consumo directo (sin React Query)

```typescript
const overview = await apiClient.get<DashboardOverview>(
  "/api/dashboard/overview?days=14"
);
```

---

## 15. Caso 6 — Scoring de leads

### Endpoint

```
GET /api/scoring/formula      → lee la fórmula activa de scoring
PUT /api/scoring/formula      → actualiza la fórmula de scoring
```

El scoring se aplica automáticamente cuando se ingesta un lead. El campo `score` del lead (`0-100`) refleja su calificación calculada según la fórmula configurada.

### Ver el score de un lead

El `score` viene incluido en el objeto `Lead` retornado por `/api/leads/intake`:

```typescript
const lead = await leadsService.intake({ email: "test@test.com", source: "web" });
console.log(`Score calculado: ${lead.score}`); // ej. 75
```

---

## 16. Caso 7 — Asignación de leads

Los leads se asignan a usuarios de ventas según las reglas del motor de asignación.

### Endpoints

```
GET  /api/assignments                   → lista asignaciones activas
POST /api/assignments                   → asigna manualmente un lead
GET  /api/assignments/users             → lista usuarios disponibles para asignación
```

### Ejemplo de asignación manual

```typescript
await apiClient.post("/api/assignments", {
  leadId: "uuid-del-lead",
  assignedToUserId: "uuid-del-vendedor"
});
```

---

## 17. Caso 8 — Contactos y Empresas

### Contactos

```
GET    /api/contacts          → lista contactos
POST   /api/contacts          → crea un contacto
GET    /api/contacts/{id}     → obtiene un contacto
PUT    /api/contacts/{id}     → actualiza un contacto
DELETE /api/contacts/{id}     → elimina un contacto
```

### Empresas

```
GET    /api/companies         → lista empresas
POST   /api/companies         → crea una empresa
GET    /api/companies/{id}    → obtiene una empresa
PUT    /api/companies/{id}    → actualiza una empresa
```

### Ejemplo de creación de contacto

```typescript
await apiClient.post("/api/contacts", {
  firstName: "Carlos",
  lastName: "García",
  email: "carlos@empresa.com",
  phone: "+506 8888-0000",
  companyId: "uuid-empresa"  // opcional
});
```

---

## 18. Caso 9 — Propuestas

### Endpoints

```
GET  /api/proposals           → lista propuestas
POST /api/proposals           → crea una propuesta
GET  /api/proposals/{id}      → obtiene una propuesta
GET  /api/proposals/{id}/pdf  → descarga PDF de la propuesta
```

### Descargar propuesta como PDF

```typescript
const response = await fetch(`${API_BASE_URL}/api/proposals/${proposalId}/pdf`, {
  headers: {
    "X-Tenant-Id": tenantId,
    "Authorization": `Bearer ${token}`
  }
});

const blob = await response.blob();
const url = URL.createObjectURL(blob);
const link = document.createElement("a");
link.href = url;
link.download = `propuesta-${proposalId}.pdf`;
link.click();
```

---

## 19. Integración desde una página web externa (vanilla JS / cualquier framework)

Si tienes una página de marketing, landing page, formulario externo u otro sistema que no es parte de este proyecto Next.js, puedes consumir la API directamente.

### Requisito previo

1. Agrega el origen de tu página a `AllowedCorsOrigins` en el backend.
2. Asegúrate de conocer tu `tenantId`.

### Ejemplo: Formulario de captación de lead en HTML puro

```html
<form id="leadForm">
  <input type="email" id="email" placeholder="Tu email" required />
  <button type="submit">Quiero una demo</button>
</form>

<script>
  const API_URL = "https://api.tudominio.com"; // URL de tu backend en producción
  const TENANT_ID = "mi-empresa";

  document.getElementById("leadForm").addEventListener("submit", async (e) => {
    e.preventDefault();

    const email = document.getElementById("email").value;

    try {
      const response = await fetch(`${API_URL}/api/leads/intake`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Tenant-Id": TENANT_ID,
          "X-Correlation-Id": crypto.randomUUID(),
          "Idempotency-Key": crypto.randomUUID() // evita duplicados en reenvíos
        },
        body: JSON.stringify({
          email: email,
          source: "landing-page-principal"
        })
      });

      if (!response.ok) {
        const error = await response.json();
        alert("Error: " + error.message);
        return;
      }

      const lead = await response.json();
      // Redirigir o mostrar confirmación
      alert("¡Gracias! Te contactaremos pronto. ID: " + lead.id);

    } catch (error) {
      console.error("Error de red:", error);
      alert("No pudimos procesar tu solicitud. Intenta de nuevo.");
    }
  });
</script>
```

### Ejemplo: Integración desde React (sin Next.js, app CRA/Vite)

```typescript
// config/api.ts
const API_BASE = import.meta.env.VITE_API_URL ?? "http://localhost:5165";
const TENANT_ID = import.meta.env.VITE_TENANT_ID ?? "default";

export async function ingestLead(email: string, source: string) {
  const response = await fetch(`${API_BASE}/api/leads/intake`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Tenant-Id": TENANT_ID,
      "X-Correlation-Id": crypto.randomUUID()
    },
    body: JSON.stringify({ email, source })
  });

  if (!response.ok) {
    const errorBody = await response.text();
    throw new Error(`Error ${response.status}: ${errorBody}`);
  }

  return response.json();
}
```

### Ejemplo: Integración desde Python (backend a backend / webhook)

```python
import requests
import uuid

API_URL = "https://api.tudominio.com"
TENANT_ID = "mi-empresa"
JWT_TOKEN = "tu-token-jwt"  # obtener del endpoint de auth

def crear_lead(email: str, fuente: str) -> dict:
    response = requests.post(
        f"{API_URL}/api/leads/intake",
        json={"email": email, "source": fuente},
        headers={
            "Content-Type": "application/json",
            "X-Tenant-Id": TENANT_ID,
            "X-Correlation-Id": str(uuid.uuid4()),
            "Authorization": f"Bearer {JWT_TOKEN}"
        },
        timeout=12
    )
    response.raise_for_status()
    return response.json()

lead = crear_lead("cliente@empresa.com", "importacion-crm")
print(f"Lead creado: {lead['id']}, Score: {lead.get('score', 'N/A')}")
```

---

## 20. Patrones de query con React Query (`useQuery` y `useMutation`)

El frontend usa `@tanstack/react-query`. El `QueryProvider` se registra en el layout raíz (`app/layout.tsx`), por lo que cualquier componente dentro de `app/` puede usar los hooks sin configuración adicional.

### Patrón para lecturas (`useQuery`)

```typescript
"use client";

import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/services/apiClient";

// 1. Define la query key (para invalidación y cache)
const MIS_DATOS_KEY = ["mis-datos", "lista"];

// 2. Crea el hook
export function useMisDatosQuery() {
  return useQuery({
    queryKey: MIS_DATOS_KEY,
    queryFn: ({ signal }) =>
      apiClient.get<MisDatos[]>("/api/mi-recurso", { signal }),
    staleTime: 30_000  // datos frescos por 30 segundos
  });
}

// 3. Úsalo en el componente
function MiComponente() {
  const { data, isLoading, isError, error } = useMisDatosQuery();

  if (isLoading) return <div>Cargando...</div>;
  if (isError) return <div>Error: {error.message}</div>;

  return <ul>{data.map(item => <li key={item.id}>{item.nombre}</li>)}</ul>;
}
```

### Patrón para escrituras (`useMutation`)

```typescript
"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/services/apiClient";

export function useCrearRecursoMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (payload: NuevoRecurso) =>
      apiClient.post<Recurso>("/api/mi-recurso", payload),
    onSuccess: () => {
      // Invalida la lista para que se recargue automáticamente
      queryClient.invalidateQueries({ queryKey: ["mis-datos", "lista"] });
    }
  });
}

// Uso en componente:
function FormularioCrear() {
  const crearRecurso = useCrearRecursoMutation();

  return (
    <button
      onClick={() => crearRecurso.mutate({ nombre: "Nuevo" })}
      disabled={crearRecurso.isPending}
    >
      {crearRecurso.isPending ? "Guardando..." : "Crear"}
    </button>
  );
}
```

---

## 21. Ejecución local — levantar backend y frontend juntos

### Paso 1: Levantar el backend

```powershell
# Desde la raíz del repositorio
cd "backend/src/Api"
dotnet run
# El backend escucha en http://localhost:5165
```

Verifica que esté activo:
```
curl http://localhost:5165/health
# → { "status": "Healthy" }
```

### Paso 2: Configurar el frontend

```powershell
cd "frontend"

# Crea el archivo de entorno local si no existe
echo "NEXT_PUBLIC_API_URL=http://localhost:5165" > .env.local
echo "NEXT_PUBLIC_TENANT_ID=default" >> .env.local
```

### Paso 3: Levantar el frontend

```powershell
npm install --legacy-peer-deps
npm run dev
# El frontend escucha en http://localhost:3000
```

### Paso 4: Verificar la conexión

Abre `http://localhost:3000` en el navegador. El dashboard debería cargar sin errores de red. Si ves errores CORS, verifica que `http://localhost:3000` esté en `AllowedCorsOrigins` del archivo `appsettings.Development.json` del backend.

---

## 22. Checklist de verificación de integración

Usa esta lista antes de declarar la integración como funcional:

### Configuración básica
- [ ] `NEXT_PUBLIC_API_URL` apunta al backend correcto en `.env.local`
- [ ] `NEXT_PUBLIC_TENANT_ID` está configurado con un valor válido
- [ ] El origen del frontend (`http://localhost:3000` o URL de producción) está en `AllowedCorsOrigins` del backend

### Cabeceras
- [ ] Todas las peticiones envían `X-Tenant-Id`
- [ ] Las peticiones POST/PUT/PATCH envían `Content-Type: application/json`
- [ ] Las peticiones críticas usan `Idempotency-Key` (especialmente `/api/leads/intake`)

### Funcionalidades por módulo
- [ ] **Leads:** Se puede registrar un lead y obtener un ID de respuesta (201)
- [ ] **Pipeline:** El tablero carga etapas y oportunidades correctamente
- [ ] **Rules:** Se pueden listar, crear y activar/desactivar reglas
- [ ] **Email:** La configuración SMTP se puede guardar y los logs se muestran
- [ ] **Dashboard:** El overview carga métricas sin errores

### Seguridad
- [ ] En producción, el JWT Bearer token se incluye en todas las peticiones
- [ ] Las contraseñas SMTP no están hardcodeadas en el código fuente
- [ ] La clave de firma JWT (`JwtSigningKey`) está en variables de entorno / Key Vault, no en `appsettings.json`

### Errores y resiliencia
- [ ] Los errores HTTP (400, 404, 429, 500) se muestran al usuario de forma amigable
- [ ] Las peticiones largas tienen timeout configurado
- [ ] Los formularios críticos usan `Idempotency-Key` para evitar duplicados en reintentos

---

*Documento mantenido por el equipo NovaMind. Para cambios en la API, actualizar este documento junto con el archivo `docs/api/` correspondiente.*
