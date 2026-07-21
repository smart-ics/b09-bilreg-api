# Admisi Ranap Phase 5 — Implementation Report

**Status:** LIVE  
**Date:** 2026-07-07  
**Scope:** Integration gateways — synchronous Doctor, Patient Administration, and Ward ports with V1 repo adapters; handler refactor to depend on ports instead of cross-context repositories.

**Out of scope:** Permission policies (Phase 6), HTTP Ward client, domain/persistence/API controller changes, new aggregates or SQL.

---

## Summary

Phase 5 delivers **explicit integration boundaries** for Rawat Inap Admission. Three Application-layer gateway ports (`IDoctorServiceGateway`, `IPatientAdministrationGateway`, `IWardAccommodationGateway`) decouple AdmisiRanap handlers from direct `IPasienRepo`, `IPpaRepo`, `IKelasRepo`, and `IBangsalRepo` dependencies. V1 Infrastructure adapters wrap existing in-process repositories — no HTTP clients.

`AdmCreateWaitingListHandler` calls `IWardAccommodationGateway.NotifyHandOver` after persisting a Waiting List entry. V1 implementation is a **no-op**; Ward hand-over contract remains persistence + `GET api/admisi-ranap/waiting-list` (ADR-004). No room/bed allocation APIs were added (ADR-002).

Gateway methods return domain `*Reff` types (`PasienReff`, `PpaReff`, `KelasReff`, `BangsalReff`) — no new domain models or persistence artifacts per feature-model/persistence-generation skills.

---

## Files Created

### Bilreg.Application

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/Integration/IDoctorServiceGateway.cs` | Doctor identity resolution port |
| `AdmisiRanapContext/Integration/IPatientAdministrationGateway.cs` | Patient demographic snapshot port |
| `AdmisiRanapContext/Integration/IWardAccommodationGateway.cs` | Ward master-data + hand-over port; `WardAccommodationHandOver` record |

### Bilreg.Infrastructure

| Path | Wraps |
|------|-------|
| `AdmisiRanapContext/Integration/DoctorServiceGateway.cs` | `IPpaRepo` |
| `AdmisiRanapContext/Integration/PatientAdministrationGateway.cs` | `IPasienRepo` |
| `AdmisiRanapContext/Integration/WardAccommodationGateway.cs` | `IKelasRepo`, `IBangsalRepo` |

### Bilreg.Test

| Path | Tests |
|------|-------|
| `AdmisiRanapContext/Integration/DoctorServiceGatewayTest.cs` | UT01–UT02 |
| `AdmisiRanapContext/Integration/PatientAdministrationGatewayTest.cs` | UT01–UT02 |
| `AdmisiRanapContext/Integration/WardAccommodationGatewayTest.cs` | UT01–UT03 |

---

## Files Modified

### Bilreg.Application

| Path | Change |
|------|--------|
| `AdmisiRanapContext/Shared/AdmisiRanapSupport.cs` | Gateway-based `LoadPasienReff`, `LoadDoctorReff`, `LoadKelasReff`, `LoadBangsalReff` |
| `OpnameRequestFeature/UseCases/AdmCreateOpnameRequestCmd.cs` | `IPatientAdministrationGateway`, `IDoctorServiceGateway` |
| `ReservationFeature/UseCases/AdmCreateReservationCmd.cs` | `IPatientAdministrationGateway`, `IWardAccommodationGateway` |
| `ReservationFeature/UseCases/AdmMaintainReservationCmd.cs` | `IWardAccommodationGateway` |
| `AdmissionFeature/UseCases/AdmProcessOpnameRequestCmd.cs` | `IWardAccommodationGateway` |
| `AdmissionFeature/UseCases/AdmProcessReservationCmd.cs` | `IWardAccommodationGateway` |
| `AdmissionFeature/UseCases/AdmUpdateAdmissionCmd.cs` | `IWardAccommodationGateway` |
| `WaitingListFeature/UseCases/AdmCreateWaitingListCmd.cs` | `IWardAccommodationGateway` + `NotifyHandOver` after save |
| `WaitingListFeature/UseCases/AdmUpdateWaitingListCmd.cs` | `IWardAccommodationGateway` |

### Bilreg.Api

| Path | Change |
|------|--------|
| `Configurations/InfrastructureService.cs` | Manual `AddScoped` for three gateways |

### Bilreg.Test (handler tests updated to mock gateways)

| Path |
|------|
| `AdmisiRanapContext/OpnameRequestFeature/AdmOpnameRequestHandlerTest.cs` |
| `AdmisiRanapContext/ReservationFeature/AdmReservationHandlerTest.cs` |
| `AdmisiRanapContext/AdmissionFeature/AdmProcessOpnameRequestHandlerTest.cs` |
| `AdmisiRanapContext/AdmissionFeature/AdmProcessReservationHandlerTest.cs` |
| `AdmisiRanapContext/WaitingListFeature/AdmWaitingListHandlerTest.cs` (+ UT03 hand-over notification) |

---

## Gateway Contracts

| Port | Method | Purpose | V1 adapter |
|------|--------|---------|------------|
| `IDoctorServiceGateway` | `ResolveDoctor(dokterId)` | Validate doctor for Opname Request (BR-RI-001) | `IPpaRepo.LoadEntity` → `PpaReff` |
| `IPatientAdministrationGateway` | `ResolvePatient(pasienId)` | Patient demographic snapshot | `IPasienRepo.LoadEntity` → `PasienReff` |
| `IWardAccommodationGateway` | `ResolveKelas(kelasId)` | Ward kelas master validation | `IKelasRepo.LoadEntity` → `KelasReff` |
| `IWardAccommodationGateway` | `ResolveBangsal(bangsalId)` | Ward bangsal master validation | `IBangsalRepo.LoadEntity` → `BangsalReff` |
| `IWardAccommodationGateway` | `NotifyHandOver(handOver)` | Outbound hand-over signal | V1 no-op; extension point for future HTTP/push |

---

## Handler Migration

| Handler | Gateways injected |
|---------|-------------------|
| `AdmCreateOpnameRequestHandler` | `IPatientAdministrationGateway`, `IDoctorServiceGateway` |
| `AdmCreateReservationHandler` | `IPatientAdministrationGateway`, `IWardAccommodationGateway` |
| `AdmMaintainReservationHandler` | `IWardAccommodationGateway` |
| `AdmProcessOpnameRequestHandler` | `IWardAccommodationGateway` |
| `AdmProcessReservationHandler` | `IWardAccommodationGateway` |
| `AdmUpdateAdmissionHandler` | `IWardAccommodationGateway` |
| `AdmCreateWaitingListHandler` | `IWardAccommodationGateway` (+ `NotifyHandOver`) |
| `AdmUpdateWaitingListHandler` | `IWardAccommodationGateway` |

Query handlers and cancel/close handlers unchanged — they do not resolve external master data.

---

## Design Decisions

| Topic | Decision |
|-------|----------|
| V1 strategy | Repo-wrapping adapters per implementation plan §9; replace with HTTP only when external boundaries require |
| Return types | Domain `*Reff` only — no DTO leakage into Application |
| Ward hand-over | `NotifyHandOver` called on Waiting List create; V1 body empty — contract is persisted queue + GET API (ADR-004) |
| Bed/room allocation | Not exposed — gateways resolve planning master data only (ADR-002) |
| DI registration | Explicit `AddScoped` in `InfrastructureService.cs` (not Scrutor-scanned) |
| Test naming | `UTxx` serial per test class; no global prefix codes |

---

## Verification

```
dotnet build src/bilreg/Bilreg.Api/Bilreg.Api.csproj  → succeeded
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmisiRanapContext" → Passed: 52, Failed: 0
```

**Acceptance criteria met:**

- Ward consumes Waiting List API/contract only — no bed allocation in Admisi Ranap
- Patient snapshot populated via `IPatientAdministrationGateway` on create paths; admission inherits from source aggregate
- Three gateway adapters with unit tests (7 new tests; 1 new handler test UT03)
- Handlers no longer depend directly on cross-context repos for master-data resolution
- No changes to Domain, SqlDb, API controllers, or legacy `RegFeature`

---

## Explicitly Out of Scope (Phase 6+)

| Item | Phase |
|------|-------|
| Permission-based authorization (`[Authorize(Policy)]`) | 6 |
| HTTP Ward client / push notification in `NotifyHandOver` | Future |
| E2E / rollout hardening | 7 |
| `admisi-ranap-api-contract.md` | When API stabilizes |

---

## Next Phase

**Phase 6 — Permission-based authorization:** Policy registration; `[Authorize(Policy)]` on endpoints; handler defense-in-depth via `IAdmisiRanapAuthorizationService` or equivalent.

---

## Related Artifacts

- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
- `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-0-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-1-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-2-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-3-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-4-implementation-report.md`
