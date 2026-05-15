# DAT-18 Production DB Migration Strategy

Fecha base: 2026-05-01

## Objetivo
Migrar de SQLite operativa a motor de produccion sin perdida de datos y con rollback controlado.

## Enfoque
- Estrategia blue/green para capa de datos.
- Cutover en ventana controlada con freeze de escrituras.

## Plan de corte
1. Congelar escrituras en origen (maintenance mode).
2. Exportar dataset completo con checksum por tabla.
3. Importar en destino con validacion de integridad referencial.
4. Ejecutar script de validacion post-import (conteos + checksums + constraints).
5. Cambiar connection string de aplicacion a destino.
6. Ejecutar smoke tests funcionales y monitoreo intensivo 60 min.
7. Abrir trafico de escritura y cerrar ventana.

## Validaciones post-migracion
- Conteos por entidad principal y tenant.
- Integridad FK en pipeline, proposals y onboarding.
- Consultas de negocio criticas con tiempos esperados.
- Verificacion de timestamps UTC.

## Plan de rollback
- Condicion de rollback: error critico de integridad o indisponibilidad > 15 min.
- Accion: revertir connection string a origen y reabrir trafico.
- Preservar bitacora de incidencias y delta no aplicado.

## Criterios de exito
- Cero perdida de datos validada por checksum/conteo.
- Sin errores de integridad referencial post-cutover.
- Ventana de indisponibilidad dentro del objetivo acordado.
