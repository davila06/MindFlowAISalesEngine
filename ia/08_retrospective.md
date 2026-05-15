# 08 — Retrospectiva

## Fase Preparación de Documentación Base (2026-04-28)

### ✅ Qué funcionó bien
- Se transformó el documento épico en artefactos ejecutables (`01_requirements`, `02_architecture`, `03_plan`, `04_tasks`).
- Se definió una secuencia incremental de entrega (Sprint 1 a Sprint 4+) con alcance claro.
- Se alineó la documentación con los skills del proyecto para mantener consistencia arquitectónica.
- Se dejó base de trazabilidad inicial con contexto, progreso y decisiones.

### ⚠️ Qué mejorar
- Faltan especificaciones numéricas de negocio para deduplicación y scoring.
- Falta formalizar contratos OpenAPI tempranamente para evitar ambigüedad en backend/frontend.
- La matriz de permisos por rol no está definida y puede bloquear implementación segura.
- No hay aún evidencias de implementación en código; solo preparación documental.

### 💡 Decisiones que cambiaría
- Definir tenancy full recién en fase avanzada puede ser costoso.
Tenant-ready desde el diseño inicial reduce retrabajo.

- Dejar OpenAPI para después del primer desarrollo aumenta riesgo de divergencia contrato-código.
Conviene iniciar el contrato API antes de implementar endpoints críticos.

### Próximos ajustes de proceso
- Cerrar especificaciones de deduplicación y scoring antes de iniciar `TASK-MVP-01` y `TASK-MVP-07`.
- Crear baseline de OpenAPI para endpoints MVP en paralelo al desarrollo.
- Actualizar `05_progress.md` al finalizar cada tarea con evidencia verificable.
