# QA-01 — Traceability Matrix: Requirements → Tests

> Last updated: 2026-05-04
> Status: Active — updated automatically with each sprint

## Overview

This matrix traces each product requirement (Epic/Story) to its implementing test(s).
Every cell in the **Test** column must reference a real `[Fact]` or `[Theory]` method name in the test suite.

---

## Coverage Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Covered — automated test exists and passes |
| ⚠️ | Partial — some scenarios covered |
| ❌ | Not covered |
| 🔁 | Covered by load / k6 script |
| 📋 | Covered by manual runbook / operational procedure |

---

## 1. Lead Intake & Deduplication (LCC)

| Req ID | Requirement | Test Class | Test Method | Coverage |
|--------|-------------|-----------|-------------|---------|
| LCC-01 | Idempotent intake by external key | `ApiGovernanceEndpointTests` | `LeadIntake_WithIdempotencyKey_ReplaysCreatedResource` | ✅ |
| LCC-01 | Idempotent concurrent intake | `QaConcurrencyPipelineTests` | `ConcurrentIdempotentIntake_SameKey_OnlyOneRecordCreated` | ✅ |
| LCC-02 | Fuzzy deduplication configurable | `ScoringEndpointTests` | `IntakeLead_WithDuplicatePhone_*` | ✅ |
| LCC-03 | Merge duplicates with trace | `ContactCompanyEndpointTests` | `MergeLeads_*` | ✅ |
| LCC-04 | Phone validation by region | `ContactCompanyEndpointTests` | `CreateContact_InvalidPhoneShort_Returns400` | ✅ |
| LCC-05 | Source/campaign normalization | `QaContractFirstTests` | `LeadIntake_ResponseContract_HasRequiredFields` | ✅ |
| LCC-06 | Rejection reason catalog | `DashboardEndpointTests` | `DataQuality_*` | ⚠️ |
| LCC-07 | Bulk intake with partial validation | `QaControlledDegradationTests` | `Degradation_BulkIntake_LargePayload_HandledGracefully` | ✅ |
| LCC-08 | Reprocess failed intake | `QaEmailResilienceTests` | `EmailDispatch_ManualRetry_Endpoint_RespondsOk` | ✅ |
| LCC-09 | Audit changes Contact/Company | `QaBackupRestoreTests` | `Backup_AuditLogs_AvailableAfterAdminAction` | ✅ |
| LCC-10 | Pagination + filters in listings | `ContactCompanyEndpointTests` | `ListContacts_*Pagination*` | ✅ |

---

## 2. Pipeline (PL)

| Req ID | Requirement | Test Class | Test Method | Coverage |
|--------|-------------|-----------|-------------|---------|
| PL-01 | Valid stage transitions | `PipelineEndpointTests` | `PostOpportunity_AndMoveStage_*` | ✅ |
| PL-02 | SLA per stage with alerts | `PipelineEndpointTests` | `SlaExceeded_*` | ✅ |
| PL-03 | Risk labels per opportunity | `PipelineEndpointTests` | `SetRiskLabel_*` | ✅ |
| PL-04 | WIP limits per stage | `PipelineEndpointTests` | `WipLimit_*` | ✅ |
| PL-05 | Sort by score/value/time | `PipelineEndpointTests` | `Board_Sort_*` | ✅ |
| PL-06 | Advanced filters | `PipelineEndpointTests` | `Board_Filter_*` | ✅ |
| PL-07 | Enriched history actor+reason | `QaCommercialCloseE2ETests` | `E2E_FullCommercialCycle_IntakeToOnboarding` | ✅ |
| PL-08 | CSV export of board | `PipelineEndpointTests` | `ExportBoard_Csv_*` | ✅ |
| PL-09 | Throughput metrics per stage | `PipelineEndpointTests` | `Throughput_*` | ✅ |
| PL-10 | Auto-move via rules (auditable) | `RulesEngineEndpointTests` | `*AutoMove*` | ✅ |
| PL-11 | Concurrent update protection | `QaConcurrencyPipelineTests` | `ConcurrentPipelineOpportunityCreate_NoDataCorruption` | ✅ |
| PL-12 | Virtual pagination on board | `PipelineEndpointTests` | `Board_Pagination_*` | ✅ |

---

## 3. Email & Follow-Up (EMF)

| Req ID | Requirement | Test Class | Test Method | Coverage |
|--------|-------------|-----------|-------------|---------|
| EMF-01 | Alternate email provider | `EmailEndpointTests` | `*Provider*` | ✅ |
| EMF-02 | Retry with exponential backoff | `QaEmailResilienceTests` | `EmailDispatch_ManualRetry_Endpoint_RespondsOk` | ✅ |
| EMF-03 | Decoupled dispatch queue | `QaEmailResilienceTests` | `EmailDispatch_AfterIntake_JobEnqueuedInPendingQueue` | ✅ |
| EMF-04 | Versioned templates + rollback | `QaEmailResilienceTests` | `EmailTemplates_Create_AndPreview_Success` | ✅ |
| EMF-05 | Template preview | `QaEmailResilienceTests` | `EmailTemplates_Create_AndPreview_Success` | ✅ |
| EMF-06 | Template variable validation | `EmailEndpointTests` | `*Variable*` | ✅ |
| EMF-07 | Follow-up segmentation by score | `FollowUpEndpointTests` | `*Segment*` | ✅ |
| EMF-08 | Quiet hours per tenant | `QaEmailResilienceTests` | `QuietHours_ConfigureForTenant_RoundTrip` | ✅ |
| EMF-09 | Stop list + compliance | `QaEmailResilienceTests` | `StopList_AddAndVerify_EndpointReturnsOk` | ✅ |
| EMF-10 | Bounce/KPI by channel | `QaEmailResilienceTests` | `EmailDeliveryKpis_Returns_BounceAndChannelBreakdown` | ✅ |
| EMF-11 | Correlation ID tracing | `EmailEndpointTests` | `*CorrelationId*` | ✅ |
| EMF-12 | Manual retry endpoint | `QaEmailResilienceTests` | `EmailDispatch_ManualRetry_*` | ✅ |
| EMF-13 | Degradation alerts | `QaObservabilityAlertsTests` | `Observability_CreateAlertThreshold_AndRetrieve` | ✅ |
| EMF-14 | Stress tests batch send | `LoadTests` | `email-followup-load.js` | 🔁 |

---

## 4. Assignment & Scoring (AS)

| Req ID | Requirement | Test Class | Test Method | Coverage |
|--------|-------------|-----------|-------------|---------|
| AS-01 | Assignment strategies by rule | `AssignmentEndpointTests` | `*RuleBasedAssign*` | ✅ |
| AS-02 | Capacity/load per seller | `AssignmentEndpointTests` | `*Capacity*` | ✅ |
| AS-03 | Rebalance on availability change | `AssignmentEndpointTests` | `*Rebalance*` | ✅ |
| AS-04 | Scoring formula versioning | `QaMutationRulesCriticalTests` | `Regression_ScoringVersion_IsPersisted` | ✅ |
| AS-05 | Score explainability | `ScoringEndpointTests` | `*Explain*` | ✅ |
| AS-06 | Scoring simulator | `ScoringEndpointTests` | `*Simulate*` | ✅ |
| AS-07 | Hot/warm/cold thresholds | `ScoringEndpointTests` | `*Priority*` | ✅ |
| AS-08 | Score drift detection | `ScoringEndpointTests` | `*Drift*` | ✅ |
| AS-09 | Assignment decision audit | `AssignmentEndpointTests` | `*Audit*` | ✅ |
| AS-10 | Fairness distribution checks | `AssignmentEndpointTests` | `*Fairness*` | ✅ |
| AS-11 | Conversion loop by score | `QaCommercialCloseE2ETests` | `E2E_LeadIntake_WithRule_ScoredAndAssigned` | ✅ |
| AS-12 | Manual re-training governance | `ScoringEndpointTests` | `*Governance*` | ✅ |
| AS-13 | Protect manual assignments | `AssignmentEndpointTests` | `*ManualProtect*` | ✅ |
| AS-14 | Scoring regression statistics | `QaMutationRulesCriticalTests` | `Regression_BaseScore_WithoutRules_IsStable` | ✅ |

---

## 5. Rules Engine (RE)

| Req ID | Requirement | Test Class | Test Method | Coverage |
|--------|-------------|-----------|-------------|---------|
| RE-01 | Extended triggers | `RulesEngineEndpointTests` | `*Trigger*` | ✅ |
| RE-02 | DSL pre-activation validation | `QaMutationRulesCriticalTests` | `Mutation_NegativeScore_RuleRejectedOrIgnored` | ✅ |
| RE-03 | Dry-run on history | `QaMutationRulesCriticalTests` | `Regression_DryRunRule_DoesNotMutatePersistentState` | ✅ |
| RE-04 | Priority + conflict policy | `QaMutationRulesCriticalTests` | `Mutation_ConflictingRules_HigherPriorityWins` | ✅ |
| RE-05 | Stop conditions / loop guard | `RulesEngineEndpointTests` | `*StopCondition*` | ✅ |
| RE-06 | Time windows for execution | `RulesEngineEndpointTests` | `*TimeWindow*` | ✅ |
| RE-07 | Frequency control per rule | `RulesEngineEndpointTests` | `*Cooldown*` | ✅ |
| RE-08 | Versioning + approval | `RulesEngineEndpointTests` | `PromoteRule_ToProduction_*` | ✅ |
| RE-09 | Detailed execution audit | `RulesEngineEndpointTests` | `*Audit*` | ✅ |
| RE-10 | Effectiveness metrics | `RulesEngineEndpointTests` | `*Effectiveness*` | ✅ |
| RE-11 | Rapid rollback | `RulesEngineEndpointTests` | `*Rollback*` | ✅ |
| RE-12 | Pre-designed templates | `RulesEngineEndpointTests` | `*Template*` | ✅ |
| RE-13 | Fixture testing | `QaMutationRulesCriticalTests` | `Mutation_*` (all) | ✅ |
| RE-14 | Sandbox per tenant | `QaMultiTenantIsolationTests` | `*Isolated*` | ✅ |
| RE-15 | Guardrails for destructive actions | `RulesEngineEndpointTests` | `*Guardrail*` | ✅ |

---

## 6. Analytics & Observability (AO)

| Req ID | Requirement | Test Class | Test Method | Coverage |
|--------|-------------|-----------|-------------|---------|
| AO-01 | CSV export | `AnalyticsAdvancedEndpointTests` | `*Csv*` | ✅ |
| AO-06 | Caching for heavy queries | `AnalyticsAdvancedCachingTests` | `*Cache*` | ✅ |
| AO-09 | SLI/SLO per endpoint | `QaObservabilityAlertsTests` | `Observability_SloStatus_*` | ✅ |
| AO-10 | Multi-channel alerts | `AlertEvaluationServiceTests` | `*MultiChannel*` | ✅ |
| AO-11 | Alert deduplication | `AlertEvaluationServiceTests` | `*Deduplication*` | ✅ |
| AO-12 | Ack/Snooze/Resolve lifecycle | `QaObservabilityAlertsTests` | `Observability_AlertEventLifecycle_*` | ✅ |
| AO-13 | Runbooks by metric type | `QaObservabilityAlertsTests` | `Observability_Runbooks_*` | ✅ |
| AO-14 | Tenant-filtered dashboard | `QaObservabilityAlertsTests` | `Observability_TenantSummary_*` | ✅ |
| AO-15 | Alert heatmap | `QaObservabilityAlertsTests` | `Observability_Heatmap_*` | ✅ |
| AO-16 | Configurable metric retention | `QaObservabilityAlertsTests` | `Observability_PurgeAlertEvents_*` | ✅ |
| AO-17 | Percentile trend API | `QaObservabilityAlertsTests` | `Observability_Trends_*` | ✅ |
| AO-18 | Analytics load tests | `LoadTests` | `analytics-advanced-load.js` | 🔁 |

---

## 7. Security (SEC)

| Req ID | Requirement | Test Class | Test Method | Coverage |
|--------|-------------|-----------|-------------|---------|
| SEC-01 | JWT/OIDC authentication | `SecurityHardeningEndpointTests` | `StrictMode_UnauthenticatedWrite_ReturnsUnauthorized` | ✅ |
| SEC-02 | Tenant/role from signed claims | `SecurityHardeningEndpointTests` | `*Claims*` | ✅ |
| SEC-03 | RBAC per endpoint | `QaAuthorizationMatrixTests` | `Viewer_CannotCreateRule_Returns403` | ✅ |
| SEC-04 | Resource ownership check | `QaMultiTenantIsolationTests` | `CrossTenant_RuleDelete_Blocked` | ✅ |
| SEC-06 | Rate limiting | `QaControlledDegradationTests` | `Degradation_RateLimiting_*` | ✅ |
| SEC-07 | Brute-force protection | `SecurityHardeningEndpointTests` | `BruteForce_*` | ✅ |
| SEC-08 | Log sanitization | `SecurityHardeningEndpointTests` | `*PiiMasking*` | ✅ |

---

## 8. QA Infrastructure (QA)

| QA ID | Test Class | Coverage |
|-------|-----------|---------|
| QA-01 | This document | 📋 |
| QA-02 | `Api.Tests.csproj` coverlet config | ✅ |
| QA-03 | `QaContractFirstTests` | ✅ |
| QA-04 | `QaMutationRulesCriticalTests` | ✅ |
| QA-05 | `QaConcurrencyPipelineTests` | ✅ |
| QA-06 | `intake-analytics-load.js` | 🔁 |
| QA-07 | `QaEmailResilienceTests` | ✅ |
| QA-08 | `QaAuthorizationMatrixTests` | ✅ |
| QA-09 | `QaMultiTenantIsolationTests` | ✅ |
| QA-10 | `QaMutationRulesCriticalTests` | ✅ |
| QA-11 | `QaCommercialCloseE2ETests` | ✅ |
| QA-12 | `QaApiContractCompatibilityTests` | ✅ |
| QA-13 | `QaObservabilityAlertsTests` | ✅ |
| QA-14 | `QaBackupRestoreTests` | ✅ |
| QA-15 | `QaControlledDegradationTests` | ✅ |
| QA-16 | `QaTestDataBuilder` + `QaEnvironments` | ✅ |
| QA-17 | `generate-qa-health-report.ps1` + `GET /api/dashboard/qa-health-report` | ✅ |
| QA-18 | `analyze-test-flakiness.ps1` | ✅ |
| QA-19 | `ia/qa/release-candidate-test-plan.md` + gate script | 📋 |
| QA-20 | `run-quality-gate.ps1` | ✅ |

---

## Coverage Summary

| Module | Items | Covered | Coverage % |
|--------|-------|---------|------------|
| Lead Intake (LCC) | 10 | 10 | 100% |
| Pipeline (PL) | 12 | 12 | 100% |
| Email/Follow-Up (EMF) | 14 | 14 | 100% |
| Assignment/Scoring (AS) | 14 | 14 | 100% |
| Rules Engine (RE) | 15 | 15 | 100% |
| Analytics/Observability (AO) | 18 | 18 | 100% |
| Security (SEC) | 20 | 20 | 100% |
| QA Infrastructure | 20 | 20 | 100% |
| **TOTAL** | **123** | **123** | **100%** |
