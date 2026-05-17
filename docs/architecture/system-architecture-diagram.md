```mermaid
graph TD
    A[Frontend (Next.js)] -->|API Calls| B[Backend (.NET)]
    B -->|Database Queries| C[SQL Server]
    B -->|Telemetry| D[Application Insights]
    A -->|Static Assets| E[Azure CDN]
    B -->|Authentication| F[Azure AD B2C]
    F -->|Token Validation| B
    C -->|Backups| G[Azure Blob Storage]
    D -->|Monitoring| H[Azure Monitor]
    H -->|Alerts| I[Ops Team]
```