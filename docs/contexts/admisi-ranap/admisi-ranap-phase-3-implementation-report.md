# Admisi Ranap Phase 3 — Implementation Report

**Status:** LIVE  
**Date:** 2026-07-07  
**Scope:** Application layer — 19 MediatR use-case handlers (11 commands + 8 queries), shared orchestration support, handler unit tests.

**Out of scope:** REST controllers, integration gateways, permission policies.

---

## Summary

Phase 3 delivers **business orchestration** for Rawat Inap Admission under `AdmisiRanapContext`. All architecture use cases are implemented as MediatR handlers with the **`Adm`** prefix (e.g. `AdmProcessOpnameRequestCmd`, `AdmGetAdmissionQry`). Admission from Opname Request and from Reservation are **independent single-source commands** — each runs its own linear path in a `TransHelper` transaction; there is no combined fulfill + realize workflow. `AdmCreateWaitingListCmd` enforces BR-RI-008 without mutating Admission (BR-RI-009).

V1 master-data resolution uses existing `IPasienRepo`, `IPpaRepo`, `IKelasRepo`, and `IBangsalRepo` directly — integration gateway adapters are deferred to Phase 5.

---

## Files Created

### Bilreg.Application

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/Shared/AdmisiRanapSupport.cs` | Load helpers, `EnsureNoActiveAdmission`, pasien-match guard |
| **OpnameRequestFeature/UseCases/** | |
| `AdmCreateOpnameRequestCmd.cs` | Create clinical opname request |
| `AdmCancelOpnameRequestCmd.cs` | Cancel opname request |
| `AdmGetOpnameRequestQry.cs` | Get single opname request |
| `AdmListOpnameRequestQry.cs` | List opname requests (status filter) |
| **ReservationFeature/UseCases/** | |
| `AdmCreateReservationCmd.cs` | Create reservation (independent from opname) |
| `AdmMaintainReservationCmd.cs` | Maintain reservation |
| `AdmGetReservationQry.cs` | Get single reservation |
| `AdmListReservationQry.cs` | List reservations |
| **AdmissionFeature/UseCases/** | |
| `AdmProcessAdmissionResponse.cs` | Shared response (`RegId`, `AdmissionStatus`) |
| `AdmProcessOpnameRequestCmd.cs` | Admit from Opname Request + fulfill opname |
| `AdmProcessReservationCmd.cs` | Admit from Reservation + realize reservation |
| `AdmUpdateAdmissionCmd.cs` | Update admission kelas/bangsal |
| `AdmCancelAdmissionCmd.cs` | Cancel admission |
| `AdmGetAdmissionQry.cs` | Get single admission |
| `AdmLookupAdmissionQry.cs` | Lookup admissions by status/pasien |
| **WaitingListFeature/UseCases/** | |
| `AdmCreateWaitingListCmd.cs` | Create waiting list (BR-RI-008) |
| `AdmUpdateWaitingListCmd.cs` | Update waiting list priority/kelas/bangsal |
| `AdmCloseWaitingListCmd.cs` | Close waiting list (no admission mutation) |
| `AdmGetWaitingListQry.cs` | Get single waiting list |
| `AdmListWaitingListQry.cs` | Ward worklist via `IWaitingListWorklistDal` |

MediatR auto-registers handlers via existing `Bilreg.Application` assembly scan.

### Bilreg.Test

| Path | Tests |
|------|-------|
| `AdmisiRanapContext/OpnameRequestFeature/AdmOpnameRequestHandlerTest.cs` | UT01, UT02 |
| `AdmisiRanapContext/ReservationFeature/AdmReservationHandlerTest.cs` | UT01, UT02 |
| `AdmisiRanapContext/AdmissionFeature/AdmProcessOpnameRequestHandlerTest.cs` | UT01, UT02 |
| `AdmisiRanapContext/AdmissionFeature/AdmProcessReservationHandlerTest.cs` | UT01, UT02 |
| `AdmisiRanapContext/WaitingListFeature/AdmWaitingListHandlerTest.cs` | UT01, UT02 |

Test method naming: `UTxx` serial per class only (not globally unique).

---

## Use Cases Implemented

| Use case | Handler | Type |
|----------|---------|------|
| Create Opname Request | `AdmCreateOpnameRequestCmd` | Command |
| Cancel Opname Request | `AdmCancelOpnameRequestCmd` | Command |
| Get Opname Request | `AdmGetOpnameRequestQry` | Query |
| List Opname Requests | `AdmListOpnameRequestQry` | Query |
| Create Reservation | `AdmCreateReservationCmd` | Command |
| Maintain Reservation | `AdmMaintainReservationCmd` | Command |
| Get Reservation | `AdmGetReservationQry` | Query |
| List Reservations | `AdmListReservationQry` | Query |
| Process Opname Request Admission | `AdmProcessOpnameRequestCmd` | Command |
| Process Reservation Admission | `AdmProcessReservationCmd` | Command |
| Update Admission | `AdmUpdateAdmissionCmd` | Command |
| Cancel Admission | `AdmCancelAdmissionCmd` | Command |
| Get Admission | `AdmGetAdmissionQry` | Query |
| Admission lookup | `AdmLookupAdmissionQry` | Query |
| Create Waiting List | `AdmCreateWaitingListCmd` | Command |
| Update Waiting List | `AdmUpdateWaitingListCmd` | Command |
| Close Waiting List | `AdmCloseWaitingListCmd` | Command |
| Get Waiting List | `AdmGetWaitingListQry` | Query |
| List active Waiting List | `AdmListWaitingListQry` | Query |

---

## Design Decisions

| Topic | Decision |
|-------|----------|
| Use-case prefix | `Adm` (supersedes planned `Rir` in implementation plan §4) |
| Shared support | `AdmisiRanapSupport` — internal static helpers (TarifPolicySupport pattern) |
| Master data (V1) | Direct repo deps; Phase 5 gateways not required for handler logic |
| Admission sources | Two independent commands; `AdmissionModel` records `OpnameRequestId` or `ReservationId` (one per path) |
| Process Opname Request | `TransHelper.NewScope()` when saving Admission + Opname Request |
| Process Reservation | `TransHelper.NewScope()` when saving Admission + Reservation; auto-`Maintain` when status is `Reserved` |
| Reservation decoupling | `ReservationModel` has no `OpnameRequestId`; `BILRG_AdmReservation` column dropped via M2 migration |
| Active admission check | `EnsureNoActiveAdmission` via `IAdmissionRepo.ListData(PasienId)` (BR-RI-004) |
| Create Waiting List | `HasActiveByRegId` guard; **no** `admission.MarkWaiting()` (BR-RI-009) |
| Close Waiting List | Requires `Accepted` status; admission repo never touched |
| List Waiting List | `IWaitingListWorklistDal` only — no Admission table scan (ADR-003) |
| Permissions | BR-RI-001 deferred to Phase 6 |

---

## BR-RI Coverage (Phase 3)

| Rule | Enforcement |
|------|-------------|
| BR-RI-004 | `EnsureNoActiveAdmission` in both admission process handlers |
| BR-RI-005 | `OpnameRequestModel.Fulfill` in `AdmProcessOpnameRequestCmd` |
| BR-RI-006 | `ReservationModel.Realize` (after auto-Maintain) in `AdmProcessReservationCmd` |
| BR-RI-007 | `WaitingListModel.Create` + `AdmissionStatusGuard` |
| BR-RI-008 | `IWaitingListRepo.HasActiveByRegId` before create WL |
| BR-RI-009 | Create/Close WL handlers never call `IAdmissionRepo.SaveChanges` |
| BR-RI-001 | Deferred — Phase 6 permission policies |

---

## Workflow Coverage

| Workflow | Handler chain |
|----------|---------------|
| Direct Admission | `AdmCreateOpnameRequestCmd` → `AdmProcessOpnameRequestCmd` → (optional) `AdmCreateWaitingListCmd` |
| Planned Admission | Create Opname + `AdmCreateReservationCmd` (independent) → `AdmMaintainReservationCmd` → `AdmProcessReservationCmd` → (optional) Create WL; opname fulfilled only via `AdmProcessOpnameRequestCmd` if taken separately |
| Elective Admission | `AdmCreateReservationCmd` → `AdmMaintainReservationCmd` → `AdmProcessReservationCmd` → (optional) Create WL |
| Patient Transfer | (Ward external) → `AdmCreateWaitingListCmd` → (Ward external) |

---

## Test Coverage

| Class | UT01 | UT02 |
|-------|------|------|
| `AdmOpnameRequestHandlerTest` | Create saves opname request | Cancel transitions status |
| `AdmReservationHandlerTest` | Create saves reservation | Maintain updates aggregate |
| `AdmProcessOpnameRequestHandlerTest` | Process fulfills Opname (BR-RI-005) | Fulfilled Opname rejected |
| `AdmProcessReservationHandlerTest` | Reserved → auto-maintain, realize (BR-RI-006) | Realized reservation rejected |
| `AdmWaitingListHandlerTest` | Cancelled admission rejects Create WL (BR-RI-007) | Close WL does not save Admission (BR-RI-009) |

Prior phases (domain DT-*, repo UT-RP-*, integration IT-DL-01) remain passing.

---

## Verification

```
dotnet build src/bilreg/Bilreg.Application/Bilreg.Application.csproj  → succeeded
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmisiRanapContext" → Passed: 44, Failed: 0
```

**Acceptance criteria met:**

- All architecture command use cases + 8 query handlers implemented
- Handler tests pass; BR-RI-005/006/008 enforced in orchestration
- Each admission command coordinates exactly one source aggregate in its transaction scope
- No references to `AdmProcessAdmissionCmd` remain
- No changes to legacy `RegFeature` or existing API routes

---

## Amendment — Split Admission + Reservation Decoupling (2026-07-07)

Post Phase 3 delivery, admission orchestration was refactored to treat Opname Request and Reservation as **independent admission sources**.

### Motivation

- A single `AdmProcessAdmissionCmd` with optional source IDs encouraged a combined fulfill + realize transaction that does not match business workflows.
- `ReservationModel.OpnameRequestId` created a hidden FK-style coupling; Reservation and Opname Request should be separate planning/clinical paths.

### Changes

| Layer | Change |
|-------|--------|
| Domain | Removed `OpnameRequestId` from `ReservationModel`; `Create(pasien, plannedDate, kelas, bangsal, auditUserId)` only |
| Persistence | Removed column from `BILRG_AdmReservation` CREATE; added `BILRG_AdmReservation_M2_DropOpnameRequestId_Alter.sql`; updated DTO/DAL |
| Application | Deleted `AdmProcessAdmissionCmd`; added `AdmProcessOpnameRequestCmd`, `AdmProcessReservationCmd`, `AdmProcessAdmissionResponse`; simplified `AdmCreateReservationCmd` / `AdmGetReservationQry` |
| Tests | Split `AdmAdmissionHandlerTest` into `AdmProcessOpnameRequestHandlerTest` and `AdmProcessReservationHandlerTest` |
| Docs | Domain, architecture, implementation plan §8/§10/Phase 3, this report |

### Unchanged

- `AdmissionModel` still stores optional `OpnameRequestId` / `ReservationId` — one populated per admission path.
- Opname Request, Waiting List, and other handlers unchanged.

---

## Explicitly Out of Scope (Phase 4+)

| Item | Phase |
|------|-------|
| REST controllers (`api/admisi-ranap/...`) | 4 |
| Integration gateways (`IDoctorServiceGateway`, etc.) | 5 |
| Permission-based authorization | 6 |
| E2E / rollout hardening | 7 |

---

## Next Phase

**Phase 4 — REST API:** Expose use-case-shaped endpoints with CQS separation; four controllers mapping 1:1 to handlers; `JSendOk` responses; baseline `[Authorize]`.

---

## Related Artifacts

- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
- `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-0-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-1-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-2-implementation-report.md`
