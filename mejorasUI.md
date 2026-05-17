# Mejoras UI - Plan Enterprise para MindFlow

Fecha: 2026-05-04
Alcance analizado: frontend/app, frontend/components, frontend/services, frontend/hooks, frontend/i18n, frontend/tests

## 1. Resumen Ejecutivo

La UI actual de MindFlow tiene una base funcional buena para operacion (dashboard, pipeline, reglas, email, admin), con avances claros en:
- estructura feature-based,
- estados base reutilizables (empty/error/skeleton),
- i18n basico EN/ES,
- accesibilidad inicial (skip-link, focus-visible, labels),
- telemetria UX y E2E smoke.

Para nivel enterprise, las mejoras clave no son cosmeticas: deben fortalecer consistencia, seguridad, escalabilidad, observabilidad, accesibilidad AA/AAA y performance en escenarios reales multi-tenant.

Este documento propone un plan de implementacion detallado, por frentes, con priorizacion y criterios de aceptacion.

## 2. Diagnostico del Estado Actual (As-Is)

## 2.1 Fortalezas actuales
- Shell de aplicacion consistente con navegacion lateral y i18n.
- Primitivos de UI reutilizables (Button, Field, EmptyState, ErrorState, Skeleton, KpiCard, TableContainer).
- Patrones de telemetria en eventos UX y Web Vitals.
- Tests E2E para flujos criticos.
- Responsive baseline y tablas con modo data-label en mobile.

## 2.2 Brechas enterprise detectadas

1. Autenticacion y permisos simulados en frontend
- `useAuth` actualmente retorna usuario fijo local y `isAuthenticated: true`.
- Impacto: no hay modelo real de sesion, expiracion, refresh token, ni RBAC robusto en UI.

2. Tenant/contexto en cliente limitado a env vars
- El `apiClient` inyecta tenant desde `NEXT_PUBLIC_TENANT_ID`.
- Impacto: acoplamiento de tenant a build/runtime local, baja flexibilidad multi-tenant real.

3. i18n inconsistente por feature
- Algunas vistas usan `t(...)`, pero en templates/email hay strings hardcodeadas en ingles.
- Impacto: UX inconsistente y deuda de localizacion.

4. Confirmaciones y modales no estandarizados
- Uso de `window.confirm` en reglas.
- Impacto: UX no uniforme, baja accesibilidad y poca trazabilidad de acciones destructivas.

5. Riesgo de seguridad en render HTML
- Uso de `dangerouslySetInnerHTML` para preview de templates sin sanitizacion explicita.
- Impacto: riesgo de XSS si no hay control estricto server/client.

6. Estrategia de datos sin cache de dominio avanzada
- Actualmente hay fetch manual por pantalla; no hay estandar de query invalidation, stale time, optimistic updates por dominio.
- Impacto: latencia percibida, recargas completas evitables, UX menos fluida.

7. Falta de boundaries nativos App Router por ruta
- No se observan `loading.tsx` / `error.tsx` por segmento.
- Impacto: menor resiliencia visual y recovery por ruta.

8. Sin sistema formal de design tokens versionados
- Hay variables CSS globales, pero falta gobernanza de tokens semanticos por componente/estado/tema.
- Impacto: riesgo de drift visual a medida que escale el equipo.

9. Accesibilidad no certificada
- Hay buenas practicas iniciales, pero falta compliance plan WCAG 2.2 AA integral (teclado, contraste, announced states, focus traps, reduced motion, etc.).

10. Observabilidad UX no cerrada end-to-end
- Se emiten eventos, pero falta asegurar pipeline de ingestion, dashboards y alertas UX (tiempo a insight, errores por pantalla, CLS/LCP por ruta).

11. Rendimiento funcional de tablas y board
- Sin paginacion server-first/virtualizacion para datasets grandes.
- Pipeline recarga board completo en acciones simples.

12. Testing de UI insuficiente para enterprise
- E2E smoke existe, pero falta cobertura profunda: accesibilidad automatizada, visual regression, contract testing FE-BE y performance budgets por ruta.

## 3. Vision Target (To-Be)

Una UI enterprise de MindFlow debe cumplir:
- UX operativa consistente por modulo.
- Seguridad UI-by-design (XSS-safe rendering, auth/session robusta, permisos por feature).
- Performance predecible bajo carga (p95 de interaccion y navegacion dentro de presupuesto).
- Accesibilidad WCAG 2.2 AA como baseline verificable.
- Observabilidad de experiencia (RUM + errores + eventos de negocio UI).
- Gobernanza de componentes y tokens para escalar sin degradar calidad.

## 4. Plan de Mejoras por Dominio

## 4.1 Fundacion UI y Design System

Objetivo:
Consolidar un design system enterprise con tokens semanticos, componentes versionados y patrones de estado.

Implementacion:
1. Definir tokens semanticos v1:
- color.surface.default / hover / inverse
- color.text.primary / secondary / disabled
- color.action.primary / danger / warning / success
- spacing.scale.2..12
- radius.scale
- motion.duration / easing
2. Separar tokens de tema (light/dark/high-contrast) de tokens de componente.
3. Estandarizar naming y documentar contrato de componentes.
4. Crear catalogo vivo de componentes (UI guide + Storybook opcional).
5. Definir reglas de deprecacion/versionado de componentes.

Entregables:
- `frontend/styles/tokens.css` (o equivalente)
- Matriz de componentes aprobados
- Guia de uso por componente y anti-patrones

Criterio de aceptacion:
- 100% de componentes base consumen tokens semanticos.

## 4.2 Arquitectura de datos y estado (Enterprise Data UX)

Objetivo:
Reducir refetch innecesario y mejorar UX percibida con cache, invalidacion y optimistic updates.

Implementacion:
1. Adoptar capa unificada de data fetching por dominio (React Query recomendado).
2. Definir query keys canonicas:
- `dashboard.overview({days, tenant})`
- `pipeline.board({filters, tenant})`
- `rules.list({query, tenant})`
- `email.logs({filters, tenant})`
3. Configurar staleTime/cacheTime por criticidad.
4. Implementar optimistic updates en:
- move opportunity
- activate/deactivate rule
5. Mantener rollback automatico en error.
6. Evitar full reload de board despues de acciones puntuales.

Criterio de aceptacion:
- Reduccion >= 35% de llamadas redundantes en flujos clave.

## 4.3 Seguridad UI y hardening de rendering

Objetivo:
Eliminar vectores XSS y elevar postura de seguridad frontend.

Implementacion:
1. Sanitizar HTML de preview en templates:
- usar sanitizacion robusta antes de `dangerouslySetInnerHTML`.
2. Introducir capa de encoding para contenido dinamico.
3. Asegurar CSP compatible con Next.js y feature set actual.
4. Revisar exposicion de errores backend en UI (evitar leak de detalles internos).
5. Implementar manejo de sesion real (token lifecycle, logout por expiracion, refresh flow).

Criterio de aceptacion:
- 0 hallazgos High/Critical en escaneo de seguridad frontend.

## 4.4 Accesibilidad (A11y) enterprise

Objetivo:
Cumplir WCAG 2.2 AA de forma verificable.

Implementacion:
1. Auditoria completa por ruta:
- dashboard, pipeline, rules, email/smtp, email/templates, email/logs, admin.
2. Reemplazar `window.confirm` por modal accesible:
- focus trap,
- teclado completo,
- `aria-labelledby`, `aria-describedby`.
3. Mejorar anuncios ARIA en:
- banners de undo,
- errores async,
- estados de carga.
4. Soporte `prefers-reduced-motion` en animaciones.
5. Validar contraste en todos los estados (normal, hover, disabled, error).

Criterio de aceptacion:
- score AA >= 95% en auditoria automatizada + checklist manual.

## 4.5 Internacionalizacion completa

Objetivo:
Eliminar strings hardcodeadas y habilitar i18n consistente por feature.

Implementacion:
1. Extraer todos los textos hardcodeados a `messages.ts`.
2. Establecer regla lint para prohibir literals UI fuera de i18n (excepto test files).
3. Definir proceso de adicion de keys por PR.
4. Alinear fecha/numero/moneda a locale.

Criterio de aceptacion:
- 0 textos hardcodeados en `app/` y `components/`.

## 4.6 UX operacional por modulo

## 4.6.1 Dashboard
- Filtros avanzados (tenant slice, source, owner, stage).
- Drill-down desde KPI a tabla detallada.
- Estados vacios contextualizados con CTA.

## 4.6.2 Pipeline
- Drag-and-drop accesible y modo teclado.
- Bulk actions (asignar, mover etapa, etiquetar).
- WIP limit visual warnings y reason capture obligatorio al mover.
- Vistas guardadas por usuario (filtros/sort persistidos).

## 4.6.3 Rules
- Rule builder completo (trigger/condition/action guiado).
- Simulador “test fixture” en UI.
- Historial de cambios y rollback visual.

## 4.6.4 Email
- SMTP: test connection en vivo + feedback tecnico usable.
- Templates: editor con validacion de variables y preview segura.
- Logs: filtros avanzados, paginacion server-side y export CSV.

## 4.7 Performance y presupuesto

Objetivo:
Definir SLO de performance UI por ruta.

SLO sugeridos:
- LCP p75 < 2.5s en dashboard/pipeline
- INP p75 < 200ms
- CLS p75 < 0.1
- TTI en rutas criticas < 3.0s

Implementacion:
1. Route-level code splitting por features pesadas.
2. Lazy loading de modulos no criticos.
3. Optimizar render de tablas y listas grandes (virtualizacion).
4. Medir y alertar Web Vitals por release.

## 4.8 Observabilidad UX

Objetivo:
Conectar eventos UX a analytics operacional accionable.

Implementacion:
1. Taxonomia de eventos v1:
- navigation_opened
- filter_applied
- action_submitted
- action_failed
- time_to_insight
- web_vital
2. Correlation IDs FE-BE por request.
3. Dashboard de experiencia:
- errores por pantalla,
- latencia por endpoint UI,
- funnel de tareas usuario.
4. Alertas de degradacion (p95 y error spikes).

## 4.9 QA UI enterprise

Implementacion:
1. E2E:
- ampliar cobertura a templates, logs, permisos, escenarios negativos.
2. Accessibility tests automatizados por PR.
3. Visual regression snapshots en rutas clave.
4. Contract tests FE-BE para payloads criticos.
5. Performance checks automáticos en CI.

Criterio de aceptacion:
- PR bloqueado si rompe accesibilidad/performance/contratos.

## 4.10 Operacion, governance y escalado de equipo

Implementacion:
1. Checklist UI Definition of Done:
- a11y,
- i18n,
- telemetry,
- tests,
- docs,
- performance.
2. RFC corto para nuevos patrones de componente.
3. Catalogo de deuda UI con SLA de cierre por severidad.

## 5. Backlog Priorizado (90 dias)

## Fase 0 (Semana 1-2) - Riesgo alto primero
- [x] Reemplazar confirmaciones nativas por modal accesible.
- [x] Sanitizar preview de templates HTML.
- [x] Eliminar strings hardcodeadas en email/templates.
- [x] Definir y publicar tokens semanticos v1.

## Fase 1 (Semana 3-5) - Estabilidad operativa
- [x] Implementar capa de queries con cache/invalidation/optimistic update.
- [x] Agregar loading.tsx/error.tsx por segmentos criticos.
- [x] Server-side pagination/filtering para logs.
- [x] Instrumentar correlation IDs FE-BE.

## Fase 2 (Semana 6-9) - Calidad enterprise
- [x] Suite a11y automatizada en CI.
- [x] Visual regression por rutas clave.
- [x] Contract tests FE-BE.
- [x] Dashboard de UX observability y alertas.

## Fase 3 (Semana 10-12) - Escala y productividad
- [ ] Rule builder avanzado + fixture testing UI.
- [ ] Pipeline advanced UX (bulk actions, saved views, keyboard flow).
- [ ] Storybook/catalogo oficial de componentes.
- [ ] Governance DoD UI formal en contribucion.

## 6. KPIs de Exito

Producto/UX:
- Tasa de exito de tareas criticas >= 98%.
- Reduccion de errores UI visibles >= 50%.
- Time-to-insight dashboard < 2s p75.

Tecnico:
- Cobertura E2E de flujos criticos >= 90%.
- 0 vulnerabilidades High/Critical frontend abiertas.
- Cumplimiento WCAG 2.2 AA >= 95%.
- Presupuesto de bundle y vitals cumplido por release.

Operacion:
- MTTR de incidencias UI < 4h.
- Alertas tempranas de degradacion (latencia/error) activas en todos los ambientes.

## 7. Riesgos y Mitigaciones

Riesgo: Sobrecarga de refactor en paralelo a nuevas features.
Mitigacion: migracion por modulo + feature flags + branch by abstraction.

Riesgo: deuda de i18n y accesibilidad reaparece.
Mitigacion: reglas de lint + checks CI obligatorios + DoD UI.

Riesgo: desacople FE-BE incompleto.
Mitigacion: contract tests y versionado de contratos.

## 8. Secuencia Recomendada de Implementacion

1. Seguridad y accesibilidad critica (modal accesible + sanitizacion).
2. i18n completa y tokens semanticos.
3. Data layer enterprise (cache/invalidation/optimistic).
4. Observabilidad UX y QA gates en CI.
5. Optimizacion avanzada de modulo pipeline/rules/email.

## 9. Definicion de Terminado (DoD UI Enterprise)

Una mejora UI se considera terminada solo si cumple:
- [ ] UX funcional validada con usuario objetivo.
- [ ] A11y WCAG 2.2 AA verificada.
- [ ] i18n completa EN/ES.
- [ ] Telemetria de evento + manejo de error instrumentados.
- [ ] Pruebas unitarias/E2E/contrato actualizadas.
- [ ] Sin regresion de performance vs baseline.
- [ ] Documentacion de componente/patron actualizada.

---

Este plan permite evolucionar la UI actual de MindFlow desde un estado operativo correcto a una experiencia enterprise robusta, segura, observable y escalable para equipos multi-tenant en produccion.