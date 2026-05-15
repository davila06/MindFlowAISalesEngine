# 07 — Issues Conocidos

> **Última actualización:** 2026-04-28
> **Estado general:** No hay bugs de runtime reportados aún; existen riesgos y bloqueos de definición para implementación.

## ISSUE-01: Reglas de deduplicación no definidas con precisión
**Severidad:** high  
**Estado:** en investigación  
**Descripción:** El roadmap exige deduplicación por email, teléfono y fuzzy matching, pero faltan criterios exactos (normalización, umbrales, orden de decisión y política de merge).  
**Reproducción:** Al intentar implementar `RF-01` y `RF-02`, no existe especificación formal de algoritmo ni conflictos esperados.  
**Workaround:** Usar deduplicación determinística mínima (email exacto normalizado y teléfono normalizado) hasta definir fuzzy matching.  
**Fix propuesto:** Definir en `01_requirements.md` un anexo de reglas de deduplicación con ejemplos y casos límite.

## ISSUE-02: Scoring básico sin fórmula base ni pesos
**Severidad:** high  
**Estado:** abierto  
**Descripción:** Se requiere scoring event-driven, pero no están definidos inputs, pesos, thresholds ni reglas de recalculo incremental.  
**Reproducción:** Al intentar iniciar `TASK-MVP-07`, no hay contrato de cálculo verificable.  
**Workaround:** Implementar una versión temporal con score neutral por defecto y eventos auditados sin impacto comercial automático.  
**Fix propuesto:** Definir fórmula v1 y thresholds de prioridad (cold/warm/hot) con criterios de negocio.

## ISSUE-03: Contratos API incompletos para MVP
**Severidad:** medium  
**Estado:** abierto  
**Descripción:** Están listados endpoints mínimos, pero faltan request/response schemas y catálogo de errores por endpoint.  
**Reproducción:** Al preparar implementación o tests de API, no se puede cerrar contrato sin supuestos.  
**Workaround:** Definir DTOs internos y alinear luego.  
**Fix propuesto:** Crear OpenAPI inicial con schemas para intake, pipeline, rules y email config.

## ISSUE-04: Especificación de permisos por rol insuficiente
**Severidad:** medium  
**Estado:** abierto  
**Descripción:** Se definieron roles `Admin`, `Sales`, `Viewer`, pero no está detallada la matriz de permisos por módulo y acción.  
**Reproducción:** Al diseñar endpoints/UI protegidos no hay política completa de autorización.  
**Workaround:** Aplicar política conservadora: solo `Admin` en configuración y `Sales` en operación básica.  
**Fix propuesto:** Crear matriz RBAC explícita por endpoint, pantalla y acción.

## ISSUE-05: Riesgo de retrabajo por tenancy tardía
**Severidad:** high  
**Estado:** en investigación  
**Descripción:** Multi-tenant está en fase full, pero si no se considera temprano en esquema/eventos se incrementa costo de migración.  
**Reproducción:** Diseño inicial de entidades sin `TenantId` en campos clave puede bloquear evolución SaaS.  
**Workaround:** Diseñar desde MVP con compatibilidad tenancy (campos y filtros listos aunque no activados full).  
**Fix propuesto:** Incluir ADR adicional de estrategia "tenant-ready from day one" para modelo y repositorios.

## ISSUE-06: Ejemplos de errores de API no documentados en OpenAPI
**Severidad:** medium  
**Estado:** abierto  
**Descripción:** Faltan ejemplos concretos de errores de validación y negocio en los schemas de OpenAPI.  
**Reproducción:** Al consumir la API, los clientes no pueden anticipar el formato de errores específicos.  
**Workaround:** Usar estructura genérica de ErrorResponse.  
**Fix propuesto:** Agregar ejemplos de errores en OpenAPI antes del siguiente release.

## ISSUE-07: Casos límite de deduplicación y scoring no validados con negocio
**Severidad:** medium  
**Estado:** abierto  
**Descripción:** Los criterios y ejemplos de deduplicación y scoring requieren validación y ajuste con stakeholders de negocio.  
**Reproducción:** Al implementar reglas, pueden surgir discrepancias con expectativas reales.  
**Workaround:** Documentar supuestos y ajustar en la próxima revisión.  
**Fix propuesto:** Revisar y aprobar casos límite con negocio antes de cierre de sprint.
