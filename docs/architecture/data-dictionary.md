# Data Dictionary By Entity

Owner: Data Governance

## Purpose

Define core entities, key fields, and governance constraints for operational consistency.

## Leads

| Field | Type | Required | Description |
|---|---|---|---|
| `Id` | Guid | Yes | Lead primary key |
| `TenantId` | string (shadow column) | Yes | Tenant boundary identifier |
| `Email` | string | Yes | Main contact email |
| `Phone` | string | No | Contact phone normalized by rules |
| `Source` | string | Yes | Lead source origin |
| `Score` | int | Yes | Current scoring output |
| `Priority` | string | Yes | Business priority (`hot`,`warm`,`cold`) |

## Contacts

| Field | Type | Required | Description |
|---|---|---|---|
| `Id` | Guid | Yes | Contact identifier |
| `LeadId` | Guid | No | Optional relation to lead |
| `Email` | string | Yes | Contact email |
| `Phone` | string | No | Contact phone |
| `IsDeleted` | bool | Yes | Soft-delete flag |

## Companies

| Field | Type | Required | Description |
|---|---|---|---|
| `Id` | Guid | Yes | Company identifier |
| `Name` | string | Yes | Company legal/commercial name |
| `Industry` | string | Yes | Industry classification |
| `IsDeleted` | bool | Yes | Soft-delete flag |

## Opportunities

| Field | Type | Required | Description |
|---|---|---|---|
| `Id` | Guid | Yes | Opportunity identifier |
| `LeadId` | Guid | Yes | Related lead |
| `StageId` | Guid | Yes | Current pipeline stage |
| `Value` | decimal | Yes | Commercial value |
| `VersionToken` | string | Yes | Optimistic concurrency token |

## Rules

| Field | Type | Required | Description |
|---|---|---|---|
| `Id` | Guid | Yes | Rule identifier |
| `Name` | string | Yes | Rule name |
| `IsActive` | bool | Yes | Activation state |
| `Priority` | int | Yes | Rule precedence |
| `ConflictPolicy` | string | Yes | Conflict resolution policy |
| `Version` | int | Yes | Rule version |

## Email Logs

| Field | Type | Required | Description |
|---|---|---|---|
| `Id` | Guid | Yes | Log identifier |
| `CorrelationId` | string | No | End-to-end trace identifier |
| `Status` | string | Yes | Delivery status |
| `Provider` | string | Yes | Email provider channel |

## Governance Notes

1. `TenantId` is implemented as a database shadow column in multiple entities.
2. Timestamps must be stored in UTC.
3. Any new entity requires dictionary update in this file.
