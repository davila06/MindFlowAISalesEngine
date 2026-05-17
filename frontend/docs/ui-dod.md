# Definition of Done (DoD) — UI Enterprise

Una mejora o feature UI se considera terminada solo si cumple TODOS los siguientes criterios:

## Criterios funcionales
- [x] UX validada con usuario objetivo o QA.
- [x] Accesibilidad WCAG 2.2 AA verificada (tests + checklist manual).
- [x] i18n completa EN/ES, sin strings hardcodeados.
- [x] Telemetría de evento y manejo de error instrumentados.
- [x] Pruebas unitarias/E2E/contrato actualizadas y en verde.
- [x] Sin regresión de performance vs baseline (Web Vitals, bundle).
- [x] Documentación de componente/patrón actualizada (Storybook, docs/).

## Criterios de gobernanza
- [x] Evidencia de validación en PR (screenshots, videos, logs de test).
- [x] Checklist DoD marcado en PR y revisado por peer.
- [x] Deuda técnica documentada con owner y SLA.
- [x] Cumplimiento de políticas de seguridad y privacidad.

## Flujo de PR
- [x] PRs deben enlazar tarea/issue y checklist DoD.
- [x] No se permite merge con checks rojos o DoD incompleto.
- [x] Cambios de UI deben incluir story y evidencia visual.

> **Nota:** El DoD UI debe revisarse y actualizarse trimestralmente para reflejar nuevos riesgos, estándares y aprendizajes del equipo.
