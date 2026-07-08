# Admisi Ranap Phase 1 — Implementation Report

**Status:** LIVE  
**Date:** 2026-07-07  
**Scope:** Domain layer — four aggregate roots, state machines, BR-RI invariants, domain unit tests.

**Out of scope:** SQL scripts, DTO/DAL/repositories, MediatR handlers, REST controllers, integration gateways, DI registration.

---

## Summary

Phase 1 delivers the **Rawat Inap Admission** domain module as four immutable `record` aggregate roots under `AdmisiRanapContext`, with behaviour methods enforcing lifecycles from `admisi-ranap-domain.md` §8. **Admission** reuses `IRegKey` / `RegId` (registration id space). **Kelas Rawat** and **Bangsal** use existing `KelasReff` and `BangsalReff` from `BedUsageContext.WardFeature`. **Waiting List** is structurally independent from Admission (ADR-001/003); Admission has no room/bed allocation surface (ADR-002 / BR-RI-011–012).

Canonical docs (`admisi-ranap-domain.md`, `admisi-ranap-architecture.md`, `admisi-ranap-implementation-plan.md`) were aligned to KelasRawat/Bangsal terminology and `RegId` identity.

---

## Files Created

### Bilreg.Domain

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/Shared/AdmissionStatusGuard.cs` | BR-RI-007 guard for Waiting List entry |
| `AdmisiRanapContext/Shared/AdmisiRanapIdHelper.cs` | `RG` + 8-char compact `RegId` generator (Phase 3 may replace with sequencer) |
| `AdmisiRanapContext/OpnameRequestFeature/IOpnameRequestKey.cs` | Key interface |
| `AdmisiRanapContext/OpnameRequestFeature/OpnameRequestStatusEnum.cs` | Requested → Fulfilled \| Cancelled |
| `AdmisiRanapContext/OpnameRequestFeature/OpnameRequestModel.cs` | Aggregate root |
| `AdmisiRanapContext/ReservationFeature/IReservationKey.cs` | Key interface |
| `AdmisiRanapContext/ReservationFeature/ReservationStatusEnum.cs` | Reserved → Maintained → Realized \| Cancelled |
| `AdmisiRanapContext/ReservationFeature/ReservationModel.cs` | Aggregate root |
| `AdmisiRanapContext/AdmissionFeature/AdmissionStatusEnum.cs` | Admitted → Updated → Waiting → Completed \| Cancelled |
| `AdmisiRanapContext/AdmissionFeature/AdmissionModel.cs` | Aggregate root (`IRegKey`) |
| `AdmisiRanapContext/WaitingListFeature/IWaitingListKey.cs` | Key interface |
| `AdmisiRanapContext/WaitingListFeature/WaitingListStatusEnum.cs` | Waiting → Accepted → Closed |
| `AdmisiRanapContext/WaitingListFeature/WaitingListModel.cs` | Independent aggregate root |

### Bilreg.Test

| Path | Tests |
|------|-------|
| `AdmisiRanapContext/OpnameRequestFeature/OpnameRequestModelTest.cs` | DT-OR-01, DT-OR-02 |
| `AdmisiRanapContext/ReservationFeature/ReservationModelTest.cs` | DT-RS-01 |
| `AdmisiRanapContext/AdmissionFeature/AdmissionModelTest.cs` | DT-AD-01, DT-AD-02 |
| `AdmisiRanapContext/WaitingListFeature/WaitingListModelTest.cs` | DT-WL-01, DT-WL-02, DT-WL-03 |

---

## State Machines (implemented)

| Aggregate | Transitions |
|-----------|-------------|
| Opname Request | `Requested` → `Fulfilled` \| `Cancelled` |
| Reservation | `Reserved` → `Maintained` → `Realized` \| `Cancelled` |
| Admission | `Admitted` → `Updated` → `Waiting` → `Completed` \| `Cancelled` |
| Waiting List | `Waiting` → `Accepted` → `Closed` |

---

## BR-RI Coverage (Phase 1)

| Rule | Domain enforcement | Notes |
|------|-------------------|-------|
| BR-RI-001 | Deferred | Doctor-only create — handler + permission (Phase 3/6) |
| BR-RI-002 | Structural | Opname Request is not Admission |
| BR-RI-003 | `ReservationModel.Create` without Opname | Elective path (DT-RS-01) |
| BR-RI-004 | One episode per `RegId` | Identity at creation |
| BR-RI-005 | `Fulfill` throws if already fulfilled | Cross-aggregate uniqueness in handler (Phase 3) |
| BR-RI-006 | `Realize` throws if already realized | Handler duplicate check (Phase 3) |
| BR-RI-007 | `AdmissionStatusGuard` + `WaitingListModel.Create` | DT-WL-01 |
| BR-RI-008 | `IsActive` property | ≤1 active WL per Admission — handler + repo (Phase 3) |
| BR-RI-009 | Structural | WL methods do not mutate Admission (DT-WL-03) |
| BR-RI-010 | WL owns KelasRawat + Bangsal | Hand-over to Ward |
| BR-RI-011 | Structural + DT-AD-02 | No Bed/Kamar/Room/Allocate on Admission |
| BR-RI-012 | Structural | Accommodation belongs to Ward |

---

## Design Decisions

| Topic | Decision |
|-------|----------|
| Admission identity | `RegId` via existing `IRegKey`; no `IAdmissionKey` |
| Accommodation attributes | `KelasRawat` (`KelasReff`), `Bangsal` (`BangsalReff`) — no CareClass/CareLevel |
| Mutability | Immutable `record`; behaviour returns new instance |
| RegId generation | `AdmisiRanapIdHelper.NewRegId()` → `RG` + 8 chars (aligns with `RegFactory`); Phase 3 may wire `ISequencerManual` |
| Other IDs | `NunaId.New("OPN" \| "RES" \| "WTL")` |

---

## Verification

```
dotnet build src/bilreg/Bilreg.Domain/Bilreg.Domain.csproj       → succeeded
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmisiRanapContext" → Passed: 20, Failed: 0
```

**Acceptance criteria met:**

- All domain tests pass
- Domain has zero Infrastructure references
- Waiting List structurally independent from Admission
- No changes to existing API routes or `RegModel` behaviour

---

## Explicitly Out of Scope (Phase 2+)

| Item | Phase |
|------|-------|
| `BILRG_Adm*` / `BILRG_BedWaitingList` SQL, DTO, DAL, repos | 2 |
| MediatR commands/queries (10 use cases) | 3 |
| REST controllers | 4 |
| Integration gateways | 5 |
| Permission policies | 6 |

---

## Next Phase

**Phase 2 — Persistence & repositories:** SQL scripts, DTO/DAL, one repo per aggregate; `RegId` column on `BILRG_AdmAdmission` and `BILRG_BedWaitingList`; snapshot columns for `KelasId`/`KelasName`, `BangsalId`/`BangsalName`.

**Skill:** `docs/skills/feature-persistence-generation.md`

---

## Related Artifacts

- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
- `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-0-implementation-report.md`
