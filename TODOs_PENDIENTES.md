# TODOs Pendientes - Backend y Frontend

Fecha de auditoria: 2026-05-04
Alcance revisado:
- Backend: backend/src
- Frontend: frontend/app, frontend/components, frontend/services, frontend/tests, frontend/hooks, frontend/i18n, frontend/scripts, frontend/types

Exclusiones aplicadas:
- Archivos generados y de build: frontend/.next, frontend/playwright-report, frontend/node_modules, dist, coverage
- Logs y artefactos no fuente

## Resumen Ejecutivo

Se realizo una verificacion de codigo para detectar:
- Codigo comentado que parezca implementacion pendiente
- Marcadores TODO/FIXME/HACK/XXX
- Indicadores de workaround/hotfix/quick fix
- Texto de riesgo tipo "In production"
- Stubs tipo NotImplemented/NotSupported

Resultado general:
- No se detectaron TODO/FIXME/HACK/XXX en codigo fuente revisado.
- No se detecto codigo comentado ejecutable en backend ni frontend con los patrones de auditoria.
- No se detectaron stubs pendientes de implementacion en frontend ni backend (NotImplemented/NotSupported).
- Se detecto 1 pendiente real en backend relacionado a operacion productiva de feature flags.

## Hallazgos Detallados

### BE-FF-001 - Integracion productiva de feature flags
- Archivo: backend/src/Api/Infrastructure/FeatureFlags/ConfigurationFeatureFlagService.cs
- Linea: 15
- Evidencia: comentario tecnico "In production, bind the Features section to Azure App Configuration or ..."

Interpretacion:
- La implementacion actual usa configuracion local para feature flags.
- Para produccion, el comentario indica migrar la fuente de flags a un proveedor administrado (Azure App Configuration y/o Key Vault).

Riesgo si no se atiende:
- Riesgo de drift de configuracion entre ambientes.
- Cambios de flags no centralizados.
- Menor control operacional y trazabilidad en cambios de comportamiento.

Implementacion recomendada (paso a paso):
1. Definir proveedor canonico de flags en produccion:
   - Opcion A: Azure App Configuration como fuente de flags.
   - Opcion B: App Configuration + secretos sensibles en Key Vault.
2. Agregar configuracion de conexion por ambiente:
   - Variables de entorno/secretos: endpoint, credenciales administradas y llaves requeridas.
3. Integrar proveedor en bootstrap de configuracion de API (Program.cs):
   - Cargar Features desde App Configuration antes de registrar servicios.
   - Mantener fallback local para desarrollo.
4. Ajustar ConfigurationFeatureFlagService para leer desde la jerarquia consolidada (tenant override -> global -> default false), manteniendo contrato actual.
5. Crear validaciones operativas:
   - Endpoint de verificacion de feature flags por tenant.
   - Logs de lectura/override para auditoria.
6. Agregar pruebas:
   - Unit tests para resolucion por prioridad.
   - Integracion para fallback local y lectura remota.
7. Actualizar documentacion:
   - Runbook de operaciones de feature flags.
   - Instrucciones de secretos y rotacion.

Criterio de cierre sugerido:
- Ambiente de produccion leyendo flags desde proveedor administrado.
- Fallback local solo en Development.
- Evidencia de pruebas y runbook actualizado.

## Checklist de TODOs Pendientes

- [ ] BE-FF-001 Integrar feature flags de produccion con Azure App Configuration/Key Vault y mantener fallback local para Development.

## Notas

- Esta auditoria se enfoco en codigo fuente ejecutable de backend/frontend.
- No se incluyeron artefactos generados (.next, reportes Playwright, node_modules) para evitar falsos positivos.
