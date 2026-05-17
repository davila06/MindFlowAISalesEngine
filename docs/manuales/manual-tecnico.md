# Manual Técnico – NovaMind MindFlow AI Sales Engine

## Introducción
Este manual está dirigido a administradores, desarrolladores y personal técnico encargado de la instalación, configuración, mantenimiento y soporte del sistema NovaMind MindFlow AI Sales Engine.

## Índice
1. Arquitectura general
2. Requisitos del sistema
3. Instalación y despliegue
4. Configuración inicial
5. Integraciones
6. Seguridad y backups
7. Mantenimiento y actualización
8. Resolución de problemas

---

## 1. Arquitectura general
- Backend: .NET Modular Monolith (Clean Architecture)
- Frontend: Next.js (feature-based)
- Infraestructura: Azure (Bicep, pipelines)
- Base de datos: SQL Server

## 2. Requisitos del sistema
- Servidor: Windows Server 2019+ o Linux equivalente
- .NET 8.0+, Node.js 20+, SQL Server 2019+, Azure CLI
- Acceso a Azure y permisos de despliegue

## 3. Instalación y despliegue
- Clonar el repositorio desde GitHub.
- Backend: compilar y publicar desde `backend/MindFlow.Backend.sln`.
- Frontend: instalar dependencias y ejecutar `npm run build` en `frontend/`.
- Infra: desplegar recursos con Bicep desde `infra/`.
- Configurar variables de entorno y secretos en Azure Key Vault.

## 4. Configuración inicial
- Crear usuarios y roles desde el módulo de administración.
- Configurar SMTP y plantillas de email.
- Definir reglas de negocio y pipeline de ventas.

## 5. Integraciones
- Email (SMTP), autenticación (Azure AD), almacenamiento (Blob Storage), telemetría (App Insights).
- Consultar `docs/architecture/` y `docs/operations/` para detalles.

## 6. Seguridad y backups
- Revisar el `security-guide.md`.
- Configurar backups automáticos de base de datos.
- Habilitar logs y alertas en Azure Monitor.

## 7. Mantenimiento y actualización
- Seguir el `patching-cadence.md` para actualizaciones.
- Monitorear logs y métricas de salud.
- Probar cambios en entornos de staging antes de producción.

## 8. Resolución de problemas
- Consultar el `incident-severity-playbook.md` y `runbooks-by-module.md`.
- Revisar logs de aplicación y Azure.
- Contactar soporte técnico si es necesario.

---

*Este manual debe mantenerse actualizado con cada release importante del sistema.*