# Manual de Pruebas – NovaMind MindFlow AI Sales Engine

## Introducción
Este manual describe el enfoque, los casos y procedimientos de prueba para asegurar la calidad del sistema NovaMind MindFlow AI Sales Engine.

## Índice
1. Estrategia de pruebas
2. Tipos de pruebas
3. Entorno de pruebas
4. Preparación de datos
5. Casos de prueba principales
6. Ejecución de pruebas
7. Registro y reporte de resultados
8. Gestión de incidencias

---

## 1. Estrategia de pruebas
- Pruebas automatizadas y manuales.
- Cobertura: backend, frontend, integraciones, seguridad y rendimiento.

## 2. Tipos de pruebas
- Unitarias: validan lógica de componentes individuales.
- Integración: validan interacción entre módulos.
- End-to-end (E2E): validan flujos completos de usuario.
- Pruebas de carga: validan desempeño bajo estrés.

## 3. Entorno de pruebas
- Usar entornos dedicados (staging, QA).
- Datos de prueba aislados de producción.

## 4. Preparación de datos
- Scripts para poblar base de datos con datos de prueba.
- Uso de mocks y fixtures para pruebas unitarias.

## 5. Casos de prueba principales
- Acceso y autenticación de usuarios.
- Creación, edición y eliminación de leads.
- Movimiento de leads en el pipeline.
- Envío y recepción de emails automáticos.
- Generación de reportes y analítica.

## 6. Ejecución de pruebas
- Backend: ejecutar `dotnet test` en `backend/src/`.
- Frontend: ejecutar `npm test` y `npx playwright test` en `frontend/`.
- Revisar resultados y logs generados.

## 7. Registro y reporte de resultados
- Documentar resultados en herramientas de seguimiento (Jira, Azure DevOps, etc.).
- Adjuntar evidencias (capturas, logs).

## 8. Gestión de incidencias
- Registrar bugs encontrados y asignar prioridad.
- Hacer seguimiento hasta su resolución.

---

*Este manual debe actualizarse con cada ciclo de pruebas o cambios relevantes en el sistema.*