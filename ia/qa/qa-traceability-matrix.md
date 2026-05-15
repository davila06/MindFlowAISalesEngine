# QA Traceability Matrix — MindFlow AI Sales Engine

> QA-01: Requirements → Test traceability.  
> Last updated: 2026-05-04  
> Scope: All functional and non-functional requirements mapped to automated tests.

---

## How to read this matrix

Each row links a requirement (from product epics and architecture documents) to its implementing test file and test method. Coverage type indicates whether the requirement is validated by a unit test, integration test, load test, or E2E test.

---

## 1. Lead Intake & Deduplication

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Intake creates lead with score | ÉPICA 1 | `ScoringEndpointTests` | `IntakeLead_PersistsScoreAndPriority_InCreatedResponse` | Integration |
| Idempotency key prevents duplicates | ARC-06 | `ScoringEndpointTests` | `IntakeLead_WithIdempotencyKey_ReturnsSameLeadOnReplay` | Integration |
| Concurrent intake with same key is deduplicated | QA-05 | `QaConcurrencyTests` | `ConcurrentIntake_SameIdempotencyKey_ProducesSingleLead` | Integration |
| Bulk intake with partial validation | LCC-07 | `ContactCompanyEndpointTests` | Multiple | Integration |
| Lead intake response contract stable | QA-03 | `QaContractFirstApiTests` | `LeadIntake_201_HonoursFull_ContractShape` | Contract |
| Empty payload returns 400 | QA-15 | `QaControlledDegradationTests` | `EmptyLeadIntakePayload_Returns400_WithValidationErrors` | Integration |
| Malformed JSON returns 400 | QA-15 | `QaControlledDegradationTests` | `MalformedJson_Returns400_NotServerError` | Integration |
| Oversized field does not produce 5xx | QA-15 | `QaControlledDegradationTests` | `OversizedField_Returns400Or413` | Integration |
| Multi-tenant lead isolation | SEC-04 | `QaMultiTenantIsolationTests` | `Dashboard_TotalLeads_IsIsolated_PerTenant` | Integration |
| Concurrent unique-email intake creates distinct leads | QA-05 | `QaConcurrencyTests` | `ConcurrentIntake_NoIdempotencyKey_CreatesDistinctLeads` | Integration |
| Lead intake load (QA-06) | QA-06 | `intake-analytics-load.js` | VU: `lead_intake` group | Load |

## 2. Scoring & Rules Engine

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Referral+US source scores Medium/High | DAT-02 | `QaScoringRulesRegressionTests` | `F1_Referral_US_Campaign_ScoredMediumOrHigh` | Regression |
| Unknown source scores Low/Medium | DAT-02 | `QaScoringRulesRegressionTests` | `F2_UnknownSource_NoCountry_ScoredLow` | Regression |
| Active rule increases score | QA-10 | `QaScoringRulesRegressionTests` | `F3_ActiveRule_IncreasesScore` | Regression |
| Two rules accumulate additively | QA-10 | `QaScoringRulesRegressionTests` | `F4_TwoActiveRules_AccumulateScore` | Regression |
| Score version is persisted | DAT-02 | `QaScoringRulesRegressionTests` | `F5_ScoreVersion_IsPersisted_AndRetrievable` | Regression |
| Scoring recalculate accepts date range | DAT-03 | `QaScoringRulesRegressionTests` | `F6_ScoringRecalculate_AcceptsDateRange` | Integration |
| Rule with positive points increases score | QA-04 | `QaMutationRulesTests` | `AddScoreRule_PositivePoints_IncreasesScore` | Mutation |
| eq condition only matches exact source | QA-04 | `QaMutationRulesTests` | `ConditionEq_OnlyMatchesExactSource` | Mutation |
| Deactivated rule does not fire | QA-04 | `QaMutationRulesTests` | `DeactivatedRule_DoesNotAffectLeadScore` | Mutation |
| Rules from TenantA do not affect TenantB | QA-09 | `QaMultiTenantIsolationTests` | `RuleCrossedTenantBoundary_LeadScoreUnaffected` | Isolation |
| Rule create response includes id, trigger, isActive | QA-03 | `QaContractFirstApiTests` | `RuleCreate_201_IncludesRequiredFields` | Contract |
| Rules list returns JSON array | QA-03 | `QaContractFirstApiTests` | `RulesList_200_IsJsonArray` | Contract |

## 3. Pipeline

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Pipeline stages are ordered | PL-01 | `PipelineEndpointTests` | `GetStages_ReturnsDefaultOrderedStages` | Integration |
| Stage move persists history | PL-07 | `PipelineEndpointTests` | `PostOpportunity_AndMoveStage_PersistsHistory` | Integration |
| Concurrent stage move: at least one wins | QA-05 | `QaConcurrencyTests` | `ConcurrentStageMoves_AtLeastOneSucceeds` | Concurrency |
| Pipeline opportunities are tenant-isolated | QA-09 | `QaMultiTenantIsolationTests` | `PipelineOpportunities_AreIsolated_CrossTenant` | Isolation |
| Pipeline stages contract has id and name | QA-03 | `QaContractFirstApiTests` | `PipelineStages_200_ArrayHasIdAndName` | Contract |
| Full pipeline flow: new→qualified→proposal→won | QA-11 | `QaCommercialCloseE2ETests` | `E2E_FullCommercialCycle_IntakeToOnboarding` | E2E |

## 4. Email & Follow-up

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Email dispatch queue is persistent | EMF-03 | `QaEmailResilienceTests` | `EmailDispatch_Queued_EvenWithoutSmtpConfiguration` | Resilience |
| Stop-list blocks dispatch | EMF-09 | `QaEmailResilienceTests` | `StopListedEmail_IsNotDispatched` | Resilience |
| Manual retry on empty queue does not error | EMF-12 | `QaEmailResilienceTests` | `ManualRetry_EmptyQueue_Returns2xx` | Resilience |
| Email KPI endpoint returns correct shape | EMF-10 | `QaEmailResilienceTests` | `EmailKpi_Returns_CorrectShape` | Integration |
| Email logs return 200 | EMF-11 | `QaEmailResilienceTests` | `EmailLogs_Returns200_WithList` | Integration |
| Template preview does not dispatch | EMF-05 | `QaEmailResilienceTests` | `TemplatePreview_DoesNotTriggerDispatch` | Integration |
| Email stress load | EMF-14 | `email-followup-load.js` | VU scenarios | Load |

## 5. Assignment

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Assignment user create 201 includes id, fullName, isActive | QA-03 | `QaContractFirstApiTests` | `AssignmentUserCreate_201_IncludesRequiredFields` | Contract |
| Concurrent assignment user creation no errors | QA-05 | `QaConcurrencyTests` | `ConcurrentAssignmentUserCreation_NoInternalErrors` | Concurrency |
| Assignment users are tenant-scoped | QA-09 | `QaMultiTenantIsolationTests` | `AssignmentUsers_AreScoped_ToTenant` | Isolation |
| Assignment fairness endpoint returns 200 | QA-10 | `QaScoringRulesRegressionTests` | `F9_AssignmentFairness_Returns200` | Integration |

## 6. Security & Authorization

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Unauthenticated write returns 401 | SEC-01 | `SecurityHardeningEndpointTests`, `QaAuthorizationSecurityTests` | Multiple | Security |
| Viewer role cannot write | SEC-03 | `QaAuthorizationSecurityTests` | `ViewerRole_WriteToRules_Returns403` | Security |
| Sales role blocked from admin endpoints | SEC-03 | `QaAuthorizationSecurityTests` | `SalesRole_OperationalSnapshot_Returns403` | Security |
| Admin role has authorized access | SEC-03 | `QaAuthorizationSecurityTests` | `AdminRole_DashboardOverview_Returns200` | Security |
| Tenant claim mismatch returns 400 | SEC-18 | `QaAuthorizationSecurityTests` | `CrossTenant_ClaimMismatch_Returns400` | Security |
| Security headers on all responses | SEC-12 | `QaAuthorizationSecurityTests` | `ApiResponse_IncludesSecurityHeaders` | Security |
| Brute-force protection on SMTP settings | SEC-07 | `SecurityHardeningEndpointTests` | `SmtpSettings_FailedAttempts_AreBlocked` | Security |

## 7. Analytics & Observability

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Metric recording endpoint accepts telemetry | AO-08 | `QaObservabilityTests` | `MetricRecording_Accepts_And_Returns200` | Integration |
| Alert threshold CRUD | AO-10 | `QaObservabilityTests` | `AlertThreshold_CreatedAndRetrievable` | Integration |
| SLO status returns compliance data | AO-09 | `QaObservabilityTests` | `SloStatus_Returns200_WithComplianceData` | Integration |
| Heatmap returns hourly density | AO-15 | `QaObservabilityTests` | `Heatmap_Returns200_WithHourlyData` | Integration |
| Trends endpoint returns percentiles | AO-17 | `QaObservabilityTests` | `Trends_Returns200_WithPercentiles` | Integration |
| Alert event status lifecycle (ack/snooze/resolve) | AO-12 | `QaObservabilityTests` | `AlertEvent_StatusTransitions_AreAtomic` | Integration |
| Purge runs without server error | AO-16 | `QaObservabilityTests` | `Purge_RunsWithout_ServerError` | Integration |
| Runbook lookup returns structured steps | AO-13 | `QaObservabilityTests` | `RunbookLookup_Returns_StructuredSteps` | Integration |
| Tenant summary returns scoped metrics | AO-14 | `QaObservabilityTests` | `TenantSummary_Returns_ScopedMetrics` | Integration |
| Analytics load test | AO-18 | `analytics-advanced-load.js` | VU scenarios | Load |
| Alert list deterministically stable | QA-18 | `QaObservabilityTests` | `AlertList_IsDeterministicallyStable` | Flakiness |

## 8. Data Persistence & Backup

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Health/ready confirms DB connectivity | DAT-17 | `QaBackupRestoreTests` | `HealthReady_ConfirmsDbConnectivity` | Operational |
| Lead data round-trips faithfully | DAT-17 | `QaBackupRestoreTests` | `LeadData_PersistsAndIsRetrievable` | Integration |
| Dashboard returns 200 after startup | DAT-17 | `QaBackupRestoreTests` | `DashboardOverview_Returns200_AfterStartup` | Smoke |
| Data retention does not remove recent leads | SEC-13 | `QaBackupRestoreTests` | `DataRetention_DoesNotRemove_RecentLeads` | Integration |
| Admin audit log is persistent | SEC-14 | `QaBackupRestoreTests` | `AdminAuditLog_IsPersistent` | Operational |

## 9. Multi-tenant Isolation (Massive Scale)

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| Dashboard totals are independent per tenant | SEC-04 | `QaMultiTenantIsolationTests` | `Dashboard_TotalLeads_IsIsolated_PerTenant` | Isolation |
| Rules from TenantA don't affect TenantB | SEC-04 | `QaMultiTenantIsolationTests` | `RuleCrossedTenantBoundary_LeadScoreUnaffected` | Isolation |
| Pipeline opportunities are tenant-scoped | SEC-04 | `QaMultiTenantIsolationTests` | `PipelineOpportunities_AreIsolated_CrossTenant` | Isolation |
| Assignment users are tenant-scoped | SEC-04 | `QaMultiTenantIsolationTests` | `AssignmentUsers_AreScoped_ToTenant` | Isolation |
| Alert thresholds are tenant-scoped | SEC-04 | `QaMultiTenantIsolationTests` | `AlertThreshold_IsScopedToTenant` | Isolation |
| Concurrent intake across 5 tenants accurate | QA-09 | `QaMultiTenantIsolationTests` | `MassiveConcurrentIntake_KeepsEachTenantCountAccurate` | Isolation |

## 10. API Contract Compatibility (QA-12)

| Requirement | Source Doc | Test File | Test Method | Type |
|-------------|-----------|-----------|-------------|------|
| v1 API header accepted | ARC-02 | `QaContractFirstApiTests` | `ApiVersion_v1Header_IsAccepted` | Contract |
| Unsupported API version returns 400 | ARC-02 | `QaContractFirstApiTests` | `ApiVersion_UnsupportedHeader_Returns400` | Contract |
| Error envelope has code + message | ARC-03 | `QaContractFirstApiTests` | `ErrorEnvelope_AlwaysHas_CodeAndMessage` | Contract |
| Scoring response schema is stable | QA-12 | `QaContractFirstApiTests` | `ScoringResponse_SchemaIsStable` | Contract |
| Lead intake backward-compatible fields | QA-12 | `QaCommercialCloseE2ETests` | `Compat_LeadIntakeResponse_BackwardCompatibleFields` | Compatibility |

---

## Coverage Summary

| Module | Tests | Type Coverage |
|--------|-------|---------------|
| Lead Intake | 11 | Unit, Integration, Load, E2E |
| Scoring & Rules | 14 | Regression, Mutation, Isolation |
| Pipeline | 6 | Integration, Concurrency, E2E |
| Email & Follow-up | 7 | Resilience, Load |
| Assignment | 4 | Contract, Concurrency, Isolation |
| Security & Auth | 8 | Security |
| Analytics & Observability | 11 | Integration, Load, Flakiness |
| Data Persistence & Backup | 5 | Operational |
| Multi-tenant Isolation | 6 | Isolation |
| API Contracts | 5 | Contract, Compatibility |
| **Total** | **77** | **Full spectrum** |
