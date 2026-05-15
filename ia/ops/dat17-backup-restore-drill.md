# DAT-17 Backup and Restore Drill

Fecha base: 2026-05-01

## Objetivo
Validar restauracion operativa con RPO <= 15 minutos y RTO <= 60 minutos para base transaccional.

## Alcance
- Backup full diario + incrementales cada 15 min.
- Restore en entorno de staging con verificacion funcional.

## Checklist de simulacro
1. Confirmar snapshot y log backup mas reciente.
2. Restaurar backup full en entorno aislado.
3. Aplicar incrementales/log hasta punto objetivo.
4. Ejecutar smoke checks API (`/health/live`, `/health/ready`).
5. Validar conteos criticos (Leads, Opportunities, Proposals, Customers).
6. Registrar RPO real y tiempo de recuperacion total (RTO).
7. Levantar hallazgos y acciones correctivas.

## Criterios de aceptacion
- RPO real <= 15 min.
- RTO real <= 60 min.
- Validaciones funcionales clave sin errores bloqueantes.

## Evidencia minima
- Timestamp de ultimo backup util.
- Tiempo inicio/fin restore.
- Resultado de health checks.
- Comparativa de conteos antes/despues.
