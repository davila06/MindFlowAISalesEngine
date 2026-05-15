# Matriz RBAC — MindFlow AI Sales Engine

> Última actualización: 2026-05-15
> Propósito: Definir permisos por rol, endpoint y acción para Admin, Sales y Viewer.

| Endpoint                        | Acción         | Admin | Sales | Viewer |
|---------------------------------|---------------|:-----:|:-----:|:------:|
| /api/leads/intake               | POST          |   ✔   |   ✔   |        |
| /api/pipeline                   | GET           |   ✔   |   ✔   |   ✔    |
| /api/email/send                 | POST          |   ✔   |   ✔   |        |
| /api/rules                      | GET           |   ✔   |   ✔   |   ✔    |
| /api/rules                      | POST/PUT/DEL  |   ✔   |       |        |
| /api/contacts                   | CRUD          |   ✔   |   ✔   |        |
| /api/companies                  | CRUD          |   ✔   |   ✔   |        |
| /api/analytics                  | GET           |   ✔   |   ✔   |   ✔    |
| /api/admin/*                    | ANY           |   ✔   |       |        |

**Notas:**
- Admin: Acceso total a configuración, reglas, usuarios y datos.
- Sales: Acceso operativo a leads, pipeline, contactos, empresas y envío de emails.
- Viewer: Solo lectura de pipeline, reglas y analytics.
- Los endpoints de configuración avanzada y administración solo están habilitados para Admin.
- Se recomienda revisar y ajustar esta matriz conforme se agreguen nuevos módulos/endpoints.
