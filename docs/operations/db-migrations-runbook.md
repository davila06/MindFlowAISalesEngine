# DB Migrations Runbook

Owner: Backend Platform
Scope: ARC-01 formal migration strategy for MindFlow backend.

## Purpose

Define how to apply, verify, and rollback EF Core migrations in a controlled and reproducible way.

## Current Baseline

- Baseline migration: `20260504145649_M0001_Baseline`
- DbContext: `LeadsDbContext`
- Provider: SQLite (current runtime)
- Startup strategy: `Migrate()` first, legacy SQL bootstrap fallback for migration-unaware environments.

## Apply Migrations (Local)

```powershell
cd backend/src/Api
dotnet ef database update --context LeadsDbContext
```

## Create New Migration

```powershell
cd backend/src/Api
dotnet ef migrations add <MigrationName> --context LeadsDbContext --output-dir Migrations
```

## Generate SQL Script

```powershell
cd backend/src/Api
dotnet ef migrations script --context LeadsDbContext --idempotent -o ../../scripts/migrations.sql
```

## Rollback Last Migration (Local)

```powershell
cd backend/src/Api
dotnet ef database update <PreviousMigrationId> --context LeadsDbContext
```

## Startup Verification

1. Start API.
2. Verify `/health/ready` returns 200.
3. Validate key endpoints (`/api/leads/intake`, `/api/pipeline/stages`, `/api/email/smtp-settings`).

## Safety Controls

1. Every schema change must be represented as an EF migration.
2. Do not add new inline DDL in `Program.cs`.
3. Keep legacy fallback only for migration baseline compatibility.
4. Include migration evidence in changelog and IA progress.

## CI Recommendation

Add a dedicated migration check job:
- build
- `dotnet ef migrations list`
- startup smoke test against clean DB
