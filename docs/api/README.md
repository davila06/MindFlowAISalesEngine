# API Documentation

This folder contains versioned OpenAPI artifacts published from the live API runtime.

## Current Version

- Active version: `v1`
- OpenAPI artifact: `docs/api/v1/openapi.json`
- Source endpoint: `/openapi/v1.json` (Development environment)
- SHA256: `FD7FA48646B5CE22F6A02ED10BC3B66B2519745A34D1AB4F15DBF24188641A70`

## Publication Process

1. Start API in Development mode.
2. Export `http://localhost:<port>/openapi/v1.json`.
3. Save artifact under `docs/api/v{major}/openapi.json`.
4. Update checksum and release notes.
5. Reference artifact in `CHANGELOG.md` release section.

## Compatibility Policy

- `v1` remains backward compatible for existing consumers.
- Breaking changes require a new major version folder (`v2`, `v3`, ...).
- Deprecated fields and routes must remain documented at least one release cycle.
