# Admisi Ranap Phase 7 — Implementation Report

**Status:** LIVE  
**Date:** 2026-07-07  
**Scope:** Production hardening — audit logging, rollout feature flag, operational validation endpoint, SQL rollback, runbook/checklist, workflow E2E tests.

**Out of scope:** Phase 6 permission policies; `admisi-ranap-api-contract.md`; historical backfill; HTTP Ward client.

---

## Summary

Phase 7 prepares **Rawat Inap Admission** for production rollout without changing the authentication model. All 11 write handlers now emit explicit `BILRG_AuditLog` rows. A rollout feature flag (`AdmisiRanap:Enabled`) allows disabling business endpoints without binary redeploy. A rollout status endpoint verifies SQL table readiness post-deploy. Operational runbook and rollout checklist document deployment, validation, and rollback. Four automated workflow tests validate Direct, Planned, Elective, and Patient Transfer chains.

**No authorization changes were introduced.** Class-level `[Authorize]` remains unchanged; Phase 6 is still PLANNED.

---

## Deliverables

| Deliverable | Path |
|-------------|------|
| Operational runbook | `docs/contexts/admisi-ranap/admisi-ranap-runbook.md` |
| Rollout checklist | `docs/contexts/admisi-ranap/admisi-ranap-rollout-checklist.md` |
| SQL rollback script | `src/bilreg/Bilreg.SqlDb/AdmisiRanapContext/BILRG_AdmisiRanap_Phase7_Rollback.sql` |
| Rollout options | `Bilreg.Application/AdmisiRanapContext/AdmisiRanapOptions.cs` |
| Enabled filter | `Bilreg.Api/Filters/AdmisiRanapEnabledFilter.cs` |
| Rollout status API | `GET api/admisi-ranap/rollout/status` |
| Workflow E2E tests | `Bilreg.Test/AdmisiRanapContext/AdmisiRanapWorkflowTest.cs` |
| Phase 7 report | This document |

---

## Files Created

### Application

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/AdmisiRanapOptions.cs` | `Enabled` feature flag (default `true`) |
| `AdmisiRanapContext/RolloutFeature/IAdmisiRanapRolloutDal.cs` | Table-exists port |
| `AdmisiRanapContext/RolloutFeature/UseCases/AdmGetRolloutStatusQry.cs` | Rollout status query + handler |

### Infrastructure

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/RolloutFeature/AdmisiRanapRolloutDal.cs` | `INFORMATION_SCHEMA.TABLES` check |

### API

| Path | Purpose |
|------|---------|
| `Filters/AdmisiRanapEnabledFilter.cs` | Returns 503 when module disabled |
| `Controllers/AdmisiRanapContext/AdmisiRanapRolloutController.cs` | Rollout status (no enabled filter) |

### SqlDb

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/BILRG_AdmisiRanap_Phase7_Rollback.sql` | Destructive rollback (4 tables + indexes) |

### Test

| Path | Tests |
|------|-------|
| `AdmisiRanapContext/AdmisiRanapWorkflowTest.cs` | WF-01 … WF-04 |
| `AdmisiRanapContext/RolloutFeature/AdmGetRolloutStatusHandlerTest.cs` | UT01–UT02 |
| `AdmisiRanapContext/AdmissionFeature/AdmUpdateAdmissionHandlerTest.cs` | UT01–UT02 |
| `AdmisiRanapContext/AdmissionFeature/AdmCancelAdmissionHandlerTest.cs` | UT01–UT02 |

### Documentation

| Path | Purpose |
|------|---------|
| `docs/contexts/admisi-ranap/admisi-ranap-runbook.md` | Deployment, validation, troubleshooting, rollback |
| `docs/contexts/admisi-ranap/admisi-ranap-rollout-checklist.md` | Pre/post deploy gates |

---

## Files Modified

### Application (audit logging — 11 write handlers)

| Handler | Audit actions |
|---------|---------------|
| `AdmCreateOpnameRequestHandler` | CREATE |
| `AdmCancelOpnameRequestHandler` | VOID + snapshot |
| `AdmCreateReservationHandler` | CREATE |
| `AdmMaintainReservationHandler` | UPDATE + snapshot |
| `AdmProcessOpnameRequestHandler` | CREATE admission + UPDATE opname (in txn) |
| `AdmProcessReservationHandler` | CREATE admission + UPDATE reservation (in txn) |
| `AdmUpdateAdmissionHandler` | UPDATE + snapshot |
| `AdmCancelAdmissionHandler` | VOID + snapshot |
| `AdmCreateWaitingListHandler` | CREATE |
| `AdmUpdateWaitingListHandler` | UPDATE + snapshot |
| `AdmCloseWaitingListHandler` | UPDATE + snapshot |

### API

| Path | Change |
|------|--------|
| `Controllers/AdmisiRanapContext/AdmissionController.cs` | `[ServiceFilter(typeof(AdmisiRanapEnabledFilter))]` |
| `Controllers/AdmisiRanapContext/OpnameRequestController.cs` | Same |
| `Controllers/AdmisiRanapContext/ReservationController.cs` | Same |
| `Controllers/AdmisiRanapContext/WaitingListController.cs` | Same |
| `Configurations/InfrastructureService.cs` | `AdmisiRanapOptions`, `IAdmisiRanapRolloutDal` |
| `Configurations/PresentationService.cs` | Register `AdmisiRanapEnabledFilter` |
| `appsettings.json` | `"AdmisiRanap": { "Enabled": true }` |

### Test (handler tests updated for `IAuditRepo`)

| Path |
|------|
| `OpnameRequestFeature/AdmOpnameRequestHandlerTest.cs` |
| `ReservationFeature/AdmReservationHandlerTest.cs` |
| `AdmissionFeature/AdmProcessOpnameRequestHandlerTest.cs` |
| `AdmissionFeature/AdmProcessReservationHandlerTest.cs` |
| `WaitingListFeature/AdmWaitingListHandlerTest.cs` |

### Index / ledger

| Path | Change |
|------|--------|
| `docs/ARTIFACTS.md` | Phase 7 artifacts indexed |
| `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md` §16 | Phase 7 → LIVE |
| `Bilreg.SqlDb/Bilreg.SqlDb.sqlproj` | Rollback script registered |

---

## Rollout Controls

| Control | Behaviour |
|---------|-----------|
| `AdmisiRanap:Enabled` | `false` → 503 on 4 business controllers; rollout status still works |
| `GET /api/admisi-ranap/rollout/status` | Returns `enabled`, `allTablesReady`, per-table `ready` |
| SQL rollback | `BILRG_AdmisiRanap_Phase7_Rollback.sql` — destructive, backup first |

---

## Workflow Validation

| ID | Workflow | Automated test |
|----|----------|----------------|
| WF-01 | Direct Admission (+ optional WL) | `WF01_DirectAdmission_WithWaitingList_CompletesChain` |
| WF-02 | Planned Admission (reservation path) | `WF02_PlannedAdmission_ViaReservation_CompletesChain` |
| WF-03 | Elective Admission | `WF03_ElectiveAdmission_ViaReservation_CompletesChain` |
| WF-04 | Patient Transfer (WL for existing admission) | `WF04_PatientTransfer_CreateWaitingList_ForExistingAdmission` |

Manual HTTP checklist documented in [`admisi-ranap-runbook.md`](admisi-ranap-runbook.md).

---

## Authorization

**Unchanged.** No permission policies, roles, claims, or `IAdmisiRanapAuthorizationService`. Phase 6 remains independently PLANNED.

---

## Verification

```
dotnet build src/bilreg/Bilreg.Api/Bilreg.Api.csproj  → succeeded
dotnet test --filter FullyQualifiedName~AdmisiRanapContext → Passed: 62, Failed: 0
dotnet test --filter FullyQualifiedName~RegFeature       → 28 passed, 4 failed (pre-existing test DB schema)
```

**AdmisiRanapContext:** 52 → **62** tests (+10: 4 workflow, 2 rollout, 4 admission handler).

**RegFeature regression:** 4 failures in `RegDalTest` due to missing column `fs_kd_trs_sjp` on test database — **not introduced by Phase 7** (no `RegFeature` code changes). Resolve by aligning test DB schema before production sign-off.

---

## Acceptance Criteria

| Criterion | Status |
|-----------|--------|
| Operationally ready for deployment | Met — runbook, checklist, rollback SQL, status endpoint |
| Backward compatible | Met — additive only; legacy `RegFeature` code untouched |
| Four workflows validated E2E | Met — automated WF-01 … WF-04 |
| Deployment/rollback documented | Met — runbook + checklist |
| No authorization changes | Met — `[Authorize]` only |
| Independent of Phase 6 | Met |

---

## Related Artifacts

- [`admisi-ranap-runbook.md`](admisi-ranap-runbook.md)
- [`admisi-ranap-rollout-checklist.md`](admisi-ranap-rollout-checklist.md)
- [`admisi-ranap-implementation-plan.md`](admisi-ranap-implementation-plan.md)
- [`docs/shared/audit-log.md`](../../shared/audit-log.md)
