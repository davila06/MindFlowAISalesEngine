# NovaMind - MindFlow AI Sales Engine

## Descripción
NovaMind es un sistema avanzado de automatización de ventas diseñado para optimizar el flujo de trabajo de ventas y mejorar la eficiencia del equipo. Este sistema combina un backend robusto basado en .NET con un frontend moderno construido en Next.js, ofreciendo una experiencia de usuario fluida y herramientas de análisis avanzadas.

## Estructura del Proyecto
El proyecto está dividido en las siguientes secciones principales:

### Backend
- **Tecnología:** .NET
- **Arquitectura:** Clean Architecture / Modular Monolith
- **Características:**
  - API para la gestión de leads, reglas de negocio y automatización de correos electrónicos.
  - Motor de deduplicación y asignación de leads.
  - Integración con sistemas de análisis y reportes.

### Frontend
- **Tecnología:** Next.js
- **Estructura:** Feature-based
- **Características:**
  - Interfaz de usuario para la gestión de pipelines de ventas.
  - Tableros de análisis y métricas.
  - Configuración de reglas y plantillas de correo electrónico.

### Infraestructura
- **Tecnología:** Azure
- **Características:**
  - Despliegue automatizado con Bicep y pipelines de CI/CD.
  - Integración con Azure Monitor y Application Insights para observabilidad.

## Requisitos
- **Node.js:** v16 o superior
- **.NET SDK:** v6.0 o superior
- **Azure CLI:** Instalado y configurado

## Instalación
1. Clonar el repositorio:
   ```bash
   git clone <URL_DEL_REPOSITORIO>
   ```
2. Navegar al directorio del proyecto:
   ```bash
   cd NovaMind - MindFlow AI sales engine
   ```
3. Instalar dependencias del frontend:
   ```bash
   cd frontend
   npm install
   ```
4. Configurar el backend:
   - Abrir el archivo `appsettings.json` en el directorio `backend/src/Api` y configurar las credenciales necesarias.

## Ejecución
### Backend
1. Navegar al directorio del backend:
   ```bash
   cd backend/src/Api
   ```
2. Ejecutar el servidor:
   ```bash
   dotnet run
   ```

### Frontend
1. Navegar al directorio del frontend:
   ```bash
   cd frontend
   ```
2. Ejecutar el servidor de desarrollo:
   ```bash
   npm run dev
   ```

## Contribución
Por favor, consulta el archivo [CONTRIBUTING.md](CONTRIBUTING.md) para obtener detalles sobre cómo contribuir al proyecto.

## Licencia
Este proyecto está licenciado bajo los términos de la licencia MIT. Consulta el archivo `LICENSE` para más detalles.