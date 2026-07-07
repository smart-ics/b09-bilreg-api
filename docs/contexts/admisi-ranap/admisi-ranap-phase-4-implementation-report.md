# Admisi Ranap Phase 4 — Implementation Report

**Status:** LIVE  
**Date:** 2026-07-07  
**Scope:** REST API layer — four thin MediatR controllers exposing all 19 Phase 3 use cases under `api/admisi-ranap/...`.

**Out of scope:** Domain/persistence changes, integration gateways, permission policies, HTTP integration tests.

---

## Summary

Phase 4 delivers **consumable HTTP endpoints** for Rawat Inap Admission. Four controllers map **1:1** to architecture use cases with CQS separation. All actions return `JSendOk`; controllers use class-level `[Authorize]` (authentication baseline — fine-grained permissions deferred to Phase 6).

The API follows the `TarifPolicyController` pattern: inject `IMediator`, bind Application-layer commands/queries, co-locate body `record` types when route `{id}` must be merged into the MediatR request. Create actions bind `AdmCreate*Cmd` directly from the request body.

`GET api/admisi-ranap/waiting-list` is the primary Ward consumer surface (ADR-004).

---

## Files Created

### Bilreg.Api

| Path | Endpoints |
|------|-----------|
| `Controllers/AdmisiRanapContext/OpnameRequestController.cs` | 4 |
| `Controllers/AdmisiRanapContext/ReservationController.cs` | 4 |
| `Controllers/AdmisiRanapContext/AdmissionController.cs` | 6 |
| `Controllers/AdmisiRanapContext/WaitingListController.cs` | 5 |

Removed placeholder `.gitKeep` from `Controllers/AdmisiRanapContext/`.

---

## Endpoint Mapping

### Opname Request — `api/admisi-ranap/opname-request`

| HTTP | Route | MediatR | Response |
|------|-------|---------|----------|
| POST | `/` | `AdmCreateOpnameRequestCmd` | `AdmCreateOpnameRequestResponse` |
| POST | `/{id}/cancel` | `AdmCancelOpnameRequestCmd` | `"Done"` |
| GET | `/{id}` | `AdmGetOpnameRequestQry` | `AdmGetOpnameRequestResponse` |
| GET | `/` | `AdmListOpnameRequestQry` | `AdmListOpnameRequestResponse` |

Query: `status?` (`OpnameRequestStatusEnum`).

### Reservation — `api/admisi-ranap/reservation`

| HTTP | Route | MediatR | Response |
|------|-------|---------|----------|
| POST | `/` | `AdmCreateReservationCmd` | `AdmCreateReservationResponse` |
| PUT | `/{id}` | `AdmMaintainReservationCmd` | `"Done"` |
| GET | `/{id}` | `AdmGetReservationQry` | `AdmGetReservationResponse` |
| GET | `/` | `AdmListReservationQry` | `AdmListReservationResponse` |

Query: `status?`, `plannedFrom?`, `plannedTo?`.

### Admission — `api/admisi-ranap/admission`

| HTTP | Route | MediatR | Response |
|------|-------|---------|----------|
| POST | `/from-opname-request` | `AdmProcessOpnameRequestCmd` | `AdmProcessAdmissionResponse` |
| POST | `/from-reservation` | `AdmProcessReservationCmd` | `AdmProcessAdmissionResponse` |
| PUT | `/{id}` | `AdmUpdateAdmissionCmd` | `"Done"` |
| POST | `/{id}/cancel` | `AdmCancelAdmissionCmd` | `"Done"` |
| GET | `/{id}` | `AdmGetAdmissionQry` | `AdmGetAdmissionResponse` |
| GET | `/` | `AdmLookupAdmissionQry` | `AdmLookupAdmissionResponse` |

Route `{id}` = `RegId`. Query: `status?`, `pasienId?`.

### Waiting List — `api/admisi-ranap/waiting-list`

| HTTP | Route | MediatR | Response |
|------|-------|---------|----------|
| POST | `/` | `AdmCreateWaitingListCmd` | `AdmCreateWaitingListResponse` |
| PUT | `/{id}` | `AdmUpdateWaitingListCmd` | `"Done"` |
| POST | `/{id}/close` | `AdmCloseWaitingListCmd` | `"Done"` |
| GET | `/{id}` | `AdmGetWaitingListQry` | `AdmGetWaitingListResponse` |
| GET | `/` | `AdmListWaitingListQry` | `AdmListWaitingListResponse` |

Query: `bangsalId?`, `waitingListStatus?` (int).

---

## API Body Records (co-located)

| Controller | Record | Purpose |
|------------|--------|---------|
| OpnameRequest | `AdmCancelOpnameRequestBody` | Cancel — `userId` only |
| Reservation | `AdmMaintainReservationBody` | Maintain — route carries `reservationId` |
| Admission | `AdmProcessOpnameRequestBody` | Process from opname |
| Admission | `AdmProcessReservationBody` | Process from reservation |
| Admission | `AdmUpdateAdmissionBody` | Update kelas/bangsal |
| Admission | `AdmCancelAdmissionBody` | Cancel — `userId` only |
| WaitingList | `AdmUpdateWaitingListBody` | Update priority/kelas/bangsal |
| WaitingList | `AdmCloseWaitingListBody` | Close — `userId` only |

Create commands (`AdmCreateOpnameRequestCmd`, `AdmCreateReservationCmd`, `AdmCreateWaitingListCmd`) bind directly from body — no extra API DTO layer.

---

## Design Decisions

| Topic | Decision |
|-------|----------|
| Reference pattern | `TarifPolicyController` — thin MediatR, `JSendOk`, co-located body records |
| Base class | `ControllerBase` |
| Route prefix | `api/admisi-ranap/{resource}` per implementation plan §8 |
| UserId | Request body on write actions (existing codebase convention) |
| Auth | Class-level `[Authorize]` only; permission policies deferred to Phase 6 |
| API DTO layer | None — Application response types returned as-is |
| Domain / persistence | Unchanged — no modifications to Domain, Infrastructure, or SqlDb |
| Legacy APIs | No changes to `RegFeature` or existing routes |

---

## Workflow Coverage (HTTP)

| Workflow | API chain |
|----------|-----------|
| Direct Admission | `POST opname-request` → `POST admission/from-opname-request` → (optional) `POST waiting-list` |
| Planned Admission | `POST opname-request` + `POST reservation` (independent) → `PUT reservation/{id}` → `POST admission/from-reservation` → (optional) `POST waiting-list` |
| Elective Admission | `POST reservation` → `PUT reservation/{id}` → `POST admission/from-reservation` → (optional) `POST waiting-list` |
| Patient Transfer | (Ward external) → `POST waiting-list` → (Ward external) |
| Ward queue | `GET waiting-list?bangsalId=...&waitingListStatus=...` |

---

## Verification

```
dotnet build src/bilreg/Bilreg.Api/Bilreg.Api.csproj  → succeeded
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmisiRanapContext" → Passed: 44, Failed: 0
```

Swagger/OpenAPI: 19 new routes discoverable under `api/admisi-ranap/...` via existing `AddSwaggerGen` configuration.

**Acceptance criteria met:**

- All 19 routes map 1:1 to use cases
- `JSendOk` on all success responses
- Class-level `[Authorize]` on all four controllers
- No changes to legacy `RegFeature` or existing API routes
- Swagger lists new routes

---

## Explicitly Out of Scope (Phase 5+)

| Item | Phase |
|------|-------|
| Integration gateways (`IDoctorServiceGateway`, etc.) | 5 |
| Permission-based authorization (`[Authorize(Policy)]`) | 6 |
| HTTP integration / policy smoke tests | 6 |
| E2E / rollout hardening | 7 |
| `admisi-ranap-api-contract.md` | When API stabilizes |

---

## Next Phase

**Phase 5 — Integration gateways:** Wire synchronous Doctor, Patient Administration, and Ward adapters; replace direct repo deps in handlers where appropriate.

---

## Related Artifacts

- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
- `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-0-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-1-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-2-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-3-implementation-report.md`
