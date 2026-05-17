# APIs disponibles — MindFlow AI Sales Engine

> Referencia rápida de todos los endpoints REST. Para el contrato completo (schemas, validaciones, ejemplos), ver [`v1/openapi.json`](v1/openapi.json).  
> Base URL local: `http://localhost:5165` | Producción: `https://api.novamind.ai`  
> Cabeceras requeridas en toda petición: `X-Tenant-Id`, `Content-Type: application/json`

---

## Leads — `/api/leads`

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/leads/intake` | Ingresa un nuevo lead |
| `POST` | `/api/leads/intake/bulk` | Ingesta masiva de leads |
| `GET` | `/api/leads/intake/failed` | Lista leads con ingesta fallida |
| `POST` | `/api/leads/intake/failed/{id}/reprocess` | Reprocesa un lead fallido |
| `PUT` | `/api/leads/intake/dedup-settings` | Actualiza configuración de deduplicación |
| `POST` | `/api/leads/merge` | Fusiona dos leads duplicados |

### Payload `POST /api/leads/intake`
```json
{
  "email": "cliente@ejemplo.com",
  "phone": "+506 8888-0000",
  "source": "web",
  "channel": "organic",
  "campaign": "mayo-2026",
  "country": "CR"
}
```
> `email` o `phone` son obligatorios. El resto es opcional.  
> Responde `201 Created` con el objeto `Lead` (incluye `id` y `score` calculado).  
> Soporta cabecera `Idempotency-Key` para evitar duplicados en reintentos.

---

## Pipeline — `/api/pipeline`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/pipeline/stages` | Lista todas las etapas |
| `GET` | `/api/pipeline/board` | Tablero completo: etapas + oportunidades |
| `GET` | `/api/pipeline/board/export` | Exporta tablero a CSV |
| `POST` | `/api/pipeline/opportunities` | Crea una oportunidad |
| `PATCH` | `/api/pipeline/opportunities/{id}/stage` | Mueve oportunidad a otra etapa |
| `GET` | `/api/pipeline/throughput` | Métricas de flujo del pipeline |
| `GET` | `/api/pipeline/stage-sla-alerts` | Alertas de SLA por etapa |
| `GET` | `/api/pipeline/wip-limits` | Lista límites WIP por etapa |
| `PUT` | `/api/pipeline/wip-limits/{stageId}` | Actualiza límite WIP de una etapa |

### Payload `POST /api/pipeline/opportunities`
```json
{
  "leadId": "uuid-del-lead",
  "title": "Propuesta Empresa ABC",
  "value": 5000,
  "stageId": "uuid-etapa"
}
```

### Payload `PATCH /api/pipeline/opportunities/{id}/stage`
```json
{
  "targetStageId": "uuid-etapa-destino",
  "reason": "cliente confirmó interés"
}
```

---

## Contactos — `/api/contacts`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/contacts` | Lista contactos |
| `POST` | `/api/contacts` | Crea un contacto |
| `GET` | `/api/contacts/{id}` | Obtiene un contacto |
| `PUT` | `/api/contacts/{id}` | Actualiza un contacto |
| `DELETE` | `/api/contacts/{id}` | Elimina un contacto |

### Payload `POST /api/contacts`
```json
{
  "firstName": "Carlos",
  "lastName": "García",
  "email": "carlos@empresa.com",
  "phone": "+506 8888-0000",
  "companyId": "uuid-empresa"
}
```

---

## Empresas — `/api/companies`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/companies` | Lista empresas |
| `POST` | `/api/companies` | Crea una empresa |
| `GET` | `/api/companies/{id}` | Obtiene una empresa |
| `PUT` | `/api/companies/{id}` | Actualiza una empresa |
| `DELETE` | `/api/companies/{id}` | Elimina una empresa |

---

## Reglas — `/api/rules`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/rules` | Lista todas las reglas |
| `POST` | `/api/rules` | Crea una regla |
| `GET` | `/api/rules/{id}` | Obtiene una regla por ID |
| `PUT` | `/api/rules/{id}` | Actualiza una regla |
| `DELETE` | `/api/rules/{id}` | Elimina una regla |
| `POST` | `/api/rules/{id}/activate` | Activa una regla |
| `POST` | `/api/rules/{id}/deactivate` | Desactiva una regla |
| `POST` | `/api/rules/{id}/promote` | Promueve la versión de una regla |
| `POST` | `/api/rules/{id}/rollback` | Revierte una regla a versión anterior |
| `POST` | `/api/rules/{id}/dry-run` | Simula la regla sin ejecutar acciones |
| `GET` | `/api/rules/templates` | Lista plantillas de reglas predefinidas |
| `POST` | `/api/rules/test-fixture` | Prueba una regla sobre un lead simulado |
| `POST` | `/api/rules/events/dispatch` | Dispara un evento de regla manualmente |
| `GET` | `/api/rules/drift-summary` | Resumen de deriva de reglas |

### Payload `POST /api/rules`
```json
{
  "name": "Asignar leads de alto score",
  "trigger": "lead.created",
  "isActive": true,
  "priority": 1,
  "conflictPolicy": "first-wins",
  "cooldownMinutes": 60,
  "allowDestructiveActions": false,
  "conditions": [
    { "field": "score", "operator": "gt", "value": "80" }
  ],
  "actions": [
    { "type": "assign_to_user", "value": "uuid-vendedor" }
  ]
}
```

**Operadores de condición:** `eq` | `gt` | `lt` | `contains`  
**Triggers disponibles:** `lead.created` | `lead.stage_changed` | `lead.scored`

---

## Email — `/api/email`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/email/smtp-settings` | Obtiene configuración SMTP activa |
| `PUT` | `/api/email/smtp-settings` | Guarda/actualiza configuración SMTP |
| `GET` | `/api/email/logs` | Lista logs de emails (`?page=1&pageSize=20&search=`) |
| `POST` | `/api/email/logs/{logId}/retry` | Reintenta un email fallido |
| `POST` | `/api/email/dispatch/execute-due` | Ejecuta despacho de emails pendientes |
| `POST` | `/api/email/templates/{key}/versions` | Crea nueva versión de una plantilla |
| `POST` | `/api/email/templates/{key}/preview` | Previsualiza plantilla con variables |
| `POST` | `/api/email/templates/{key}/rollback` | Revierte plantilla a versión anterior |
| `POST` | `/api/email/stop-list` | Agrega email a lista de bloqueo |

### Payload `PUT /api/email/smtp-settings`
```json
{
  "providerType": "smtp",
  "host": "smtp.gmail.com",
  "port": 587,
  "username": "notif@empresa.com",
  "password": "contraseña-segura",
  "fromEmail": "notif@empresa.com",
  "fromName": "MindFlow",
  "enableSsl": true
}
```

### Variables permitidas en plantillas de email
```
lead.name  lead.email  company.name  pipeline.stage
recipient_name  proposal_title  amount  currency
tracking_url  endpoint_name  metric_name
observed_value  threshold_value  triggered_at_utc
```

---

## Dashboard — `/api/dashboard`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/dashboard/overview` | KPIs del período (`?days=7`) |
| `GET` | `/api/dashboard/overview/csv` | Exporta KPIs a CSV |
| `GET` | `/api/dashboard/data-quality` | Resumen de calidad de datos |
| `GET` | `/api/dashboard/data-quality/anomalies` | Eventos anómalos detectados |
| `GET` | `/api/dashboard/qa-health-report` | Reporte de salud QA (`?windowDays=7`) |

---

## Propuestas — `/api/proposals`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/proposals` | Lista propuestas |
| `POST` | `/api/proposals` | Crea una propuesta |
| `GET` | `/api/proposals/{id}` | Obtiene una propuesta |
| `GET` | `/api/proposals/{id}/pdf` | Descarga propuesta como PDF |
| `POST` | `/api/proposals/templates` | Crea una plantilla de propuesta |
| `POST` | `/api/proposals/{id}/sign` | Marca propuesta como firmada |
| `POST` | `/api/proposals/{id}/expire` | Expira una propuesta |
| `POST` | `/api/proposals/{id}/renew` | Renueva una propuesta expirada |
| `POST` | `/api/proposals/{id}/reminders/force-due` | Fuerza recordatorio de propuesta |
| `POST` | `/api/proposals/{id}/reminders/requeue` | Vuelve a encolar recordatorio |
| `POST` | `/api/proposals/reminders/execute-due` | Ejecuta recordatorios pendientes |

---

## Asignaciones — `/api/assignments`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/assignments/users` | Lista usuarios disponibles para asignación |
| `POST` | `/api/assignments/users` | Registra un usuario como asignable |
| `PUT` | `/api/assignments/users/{userId}/availability` | Actualiza disponibilidad del usuario |
| `POST` | `/api/assignments/leads/{leadId}/manual` | Asigna manualmente un lead a un usuario |

---

## Scoring — `/api/scoring`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/scoring/formula` | Lee la fórmula de scoring activa |
| `POST` | `/api/scoring/formula/proposals` | Propone una nueva fórmula |
| `POST` | `/api/scoring/formula/proposals/{id}/approve` | Aprueba una propuesta de fórmula |
| `GET` | `/api/scoring/priority-thresholds` | Lee umbrales de prioridad |
| `PUT` | `/api/scoring/priority-thresholds` | Actualiza umbrales de prioridad |
| `POST` | `/api/scoring/simulator` | Simula el score de un lead hipotético |
| `POST` | `/api/scoring/recalculate` | Recalcula scores de todos los leads |

### Payload `POST /api/scoring/simulator`
```json
{
  "source": "web",
  "hasEmail": true,
  "hasPhone": false,
  "country": "CR",
  "channel": "organic"
}
```

---

## Analytics Avanzado — `/api/analytics`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/analytics/alert-thresholds` | Lista umbrales de alerta |
| `POST` | `/api/analytics/alert-thresholds` | Crea un umbral de alerta |
| `PUT` | `/api/analytics/alert-thresholds/{id}` | Actualiza un umbral |
| `DELETE` | `/api/analytics/alert-thresholds/{id}` | Elimina un umbral |
| `GET` | `/api/analytics/alert-events` | Lista eventos de alerta |
| `PUT` | `/api/analytics/alert-events/{id}/status` | Actualiza estado de un evento |
| `POST` | `/api/analytics/alert-events/purge` | Purga eventos antiguos |
| `POST` | `/api/analytics/reports/weekly/run` | Ejecuta reporte semanal manualmente |
| `POST` | `/api/analytics/metrics/history/snapshot` | Genera snapshot de métricas |
| `POST` | `/api/analytics/metrics/history/aggregate-incremental` | Agrega métricas incrementalmente |

---

## Follow-Up — `/api/followup`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/followup/jobs` | Lista jobs de seguimiento |
| `POST` | `/api/followup/leads/{leadId}/cancel` | Cancela seguimientos de un lead |
| `POST` | `/api/followup/jobs/{jobId}/cancel` | Cancela un job específico |
| `POST` | `/api/followup/jobs/{jobId}/requeue` | Vuelve a encolar un job |
| `GET` | `/api/followup/policy` | Lee la política de seguimiento activa |
| `PUT` | `/api/followup/policy` | Actualiza la política de seguimiento |

---

## Onboarding — `/api/onboarding`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/onboarding/tasks` | Lista tareas de onboarding |
| `POST` | `/api/onboarding/tasks/{taskId}/complete` | Marca una tarea como completada |
| `POST` | `/api/onboarding/lifecycle/evaluate` | Evalúa el ciclo de vida del onboarding |
| `POST` | `/api/onboarding/welcome-jobs/{jobId}/requeue` | Reencola un job de bienvenida |
| `POST` | `/api/onboarding/welcome-jobs/execute-due` | Ejecuta jobs de bienvenida pendientes |

---

## Health — `/health`

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/health` | Estado del servicio y de la base de datos |

---

## Códigos de respuesta comunes

| Código | Significado |
|---|---|
| `200 OK` | Operación exitosa |
| `201 Created` | Recurso creado correctamente |
| `204 No Content` | Operación exitosa sin cuerpo de respuesta |
| `400 Bad Request` | Validación fallida (ver `validationErrors` en el body) |
| `404 Not Found` | Recurso no encontrado |
| `409 Conflict` | Conflicto (ej. lead duplicado) |
| `429 Too Many Requests` | Límite de 120 req/min excedido |
| `500 Internal Server Error` | Error inesperado del servidor |

---

*Para la especificación OpenAPI completa (schemas detallados, ejemplos, modelos): [`v1/openapi.json`](v1/openapi.json)*
