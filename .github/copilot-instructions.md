# Instrucciones del Proyecto NovaMind

Este repositorio utiliza personalizaciones de Copilot exclusivamente a nivel workspace.

## Alcance de skills permitido

Usar solo estos skills locales del proyecto:

- .github/skills/novamind-system-master/SKILL.md
- .github/skills/novamind-backend/SKILL.md
- .github/skills/novamind-frontend/SKILL.md
- .github/skills/novamind-infra-devops/SKILL.md
- .github/skills/novamind-documentation/SKILL.md
- .github/skills/external-frontend-design/SKILL.md
- .github/skills/external-vercel-react-best-practices/SKILL.md
- .github/skills/external-systematic-debugging/SKILL.md
- .github/skills/external-test-driven-development/SKILL.md
- .github/skills/external-appinsights-instrumentation/SKILL.md
- .github/skills/external-azure-observability/SKILL.md
- .github/skills/external-azure-diagnostics/SKILL.md

## Restricciones

- No usar skills personalizados de perfil de usuario (.agents/skills) para tareas de este repositorio.
- Los skills externos aprobados deben existir vendorized dentro de .github/skills y no deben cargarse desde sus ubicaciones globales originales.
- No introducir nuevas guias de arquitectura fuera de las definidas en estos skills sin aprobacion explicita.
- Mantener coherencia con la estructura oficial:
  - Backend: Clean Architecture / Modular Monolith
  - Frontend: feature-based
  - Infra: infra/bicep + infra/pipelines
  - Docs: docs/ + ia/

## Activacion esperada

Cuando la tarea corresponda a backend, frontend, infra/devops o documentacion, priorizar el skill NovaMind correspondiente antes de proponer o editar cambios.

Los skills externos se usan solo como complemento horizontal:
- external-frontend-design y external-vercel-react-best-practices para calidad y performance de UI.
- external-systematic-debugging y external-test-driven-development para disciplina de ejecucion.
- external-appinsights-instrumentation, external-azure-observability y external-azure-diagnostics para telemetria y operacion en Azure.
