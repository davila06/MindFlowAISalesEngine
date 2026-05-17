
# Product KPI Dashboard Definition

Owner: Product + Analytics
Refresh cadence: Daily operational view, weekly executive summary.

## Objectives

Track conversion, velocity, reliability, and quality indicators aligned with revenue outcomes.

## KPI Catalog

### Commercial Funnel KPIs

- Lead intake volume
- Qualification rate
- Proposal conversion rate
- Win rate
- Revenue velocity (time to close)

### Operations KPIs

- Deployment frequency
- Change failure rate
- MTTR
- Background job failure rate
- Email delivery success rate

### Data And Quality KPIs

- Duplicate lead rate
- Contact completeness score
- Alert false-positive ratio
- Test flakiness rate

## Dashboard Views

1. Executive view (weekly trend lines and targets)
2. Operations view (SLO and incident signals)
3. Sales flow view (stage throughput, stuck opportunities)
4. Data quality view (completeness and duplicate trends)
5. UX observability & alerting dashboard (telemetría de eventos UX, errores, web vitals, alertas de experiencia)

## Ownership

| KPI Group | Owner |
|---|---|
| Commercial | Product + Sales Ops |
| Operations | Platform Engineering |
| Data Quality | Data Governance |
| Testing Quality | QA Lead |

## Targets (Initial)

- Proposal conversion: >= 25%
- Change failure rate: <= 15%
- MTTR: <= 60 minutes
- Email delivery success: >= 98%
- Duplicate lead rate: <= 3%
