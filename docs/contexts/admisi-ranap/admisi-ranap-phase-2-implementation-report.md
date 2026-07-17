# Admisi Ranap Phase 2 — Implementation Report

**Status:** LIVE  
**Date:** 2026-07-07  
**Scope:** Persistence layer — SQL scripts, DTO/DAL, repositories, worklist projection DAL, repo unit tests, optional DAL integration test.

**Out of scope:** MediatR handlers, REST controllers, integration gateways, permission policies, cross-aggregate orchestration.

---

## Summary

Phase 2 delivers **durable storage** for the four AdmisiRanap aggregate roots with **one repository per root** and **independent Waiting List persistence** (ADR-003). Each aggregate has a `BILRG_*` table, DTO with `FromModel`/`ToModel`, Dapper DAL, and repo implementing `SaveChanges`/`LoadEntity` via the Nuna `MayBe` pattern. `IWaitingListWorklistDal` provides a ward queue projection that queries **only** `BILRG_BedWaitingList` — not Admission history.

Repos contain **no workflow orchestration** (no fulfill/realize/admit logic).

---

## Files Created

### Bilreg.SqlDb

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/OpnameRequestFeature/BILRG_AdmOpnameRequest.sql` | Opname Request root table + index |
| `AdmisiRanapContext/OpnameRequestFeature/BILRG_AdmOpnameRequest_M1_DropPasienSnapshot_Alter.sql` | Migration — drop patient snapshot columns |
| `AdmisiRanapContext/ReservationFeature/BILRG_AdmReservation.sql` | Reservation root table + index |
| `AdmisiRanapContext/AdmissionFeature/BILRG_AdmAdmission.sql` | Admission root table + indexes |
| `AdmisiRanapContext/AdmissionFeature/BILRG_AdmAdmission_M1_DropPasienSnapshot_Alter.sql` | Migration — drop patient snapshot columns |
| `AdmisiRanapContext/WaitingListFeature/BILRG_BedWaitingList.sql` | Waiting List root table + indexes |
| `AdmisiRanapContext/WaitingListFeature/BILRG_BedWaitingList_M1_DropPasienSnapshot_Alter.sql` | Migration — drop patient snapshot columns |

Registered in `Bilreg.SqlDb.sqlproj`.

### Bilreg.Application

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/OpnameRequestFeature/IOpnameRequestRepo.cs` | Repo contract + `OpnameRequestListFilter` |
| `AdmisiRanapContext/ReservationFeature/IReservationRepo.cs` | Repo contract + `ReservationListFilter` |
| `AdmisiRanapContext/AdmissionFeature/IAdmissionRepo.cs` | Repo contract + `AdmissionListFilter` |
| `AdmisiRanapContext/WaitingListFeature/IWaitingListRepo.cs` | Repo contract + `HasActiveByRegId` |
| `AdmisiRanapContext/WaitingListFeature/IWaitingListWorklistDal.cs` | Ward queue projection contract |

### Bilreg.Infrastructure

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/OpnameRequestFeature/OpnameRequestDto.cs` | DTO mapping |
| `AdmisiRanapContext/OpnameRequestFeature/OpnameRequestDal.cs` | DAL (Insert/Update/GetData/ListData) |
| `AdmisiRanapContext/OpnameRequestFeature/OpnameRequestRepo.cs` | Repo implementation |
| `AdmisiRanapContext/ReservationFeature/ReservationDto.cs` | DTO mapping |
| `AdmisiRanapContext/ReservationFeature/ReservationDal.cs` | DAL |
| `AdmisiRanapContext/ReservationFeature/ReservationRepo.cs` | Repo implementation |
| `AdmisiRanapContext/AdmissionFeature/AdmissionDto.cs` | DTO mapping |
| `AdmisiRanapContext/AdmissionFeature/AdmissionDal.cs` | DAL |
| `AdmisiRanapContext/AdmissionFeature/AdmissionRepo.cs` | Repo implementation |
| `AdmisiRanapContext/WaitingListFeature/WaitingListDto.cs` | DTO mapping |
| `AdmisiRanapContext/WaitingListFeature/WaitingListDal.cs` | DAL + `HasActiveByRegId` |
| `AdmisiRanapContext/WaitingListFeature/WaitingListRepo.cs` | Repo implementation |
| `AdmisiRanapContext/WaitingListFeature/WaitingListWorklistDal.cs` | Active queue projection DAL |

### Bilreg.Api

| Path | Change |
|------|--------|
| `Configurations/InfrastructureService.cs` | Manual `AddScoped<IWaitingListWorklistDal, WaitingListWorklistDal>()` |

DALs and repos auto-registered via existing Scrutor scan.

### Bilreg.Test

| Path | Tests |
|------|-------|
| `AdmisiRanapContext/OpnameRequestFeature/OpnameRequestRepoTest.cs` | UT-RP-01..03 |
| `AdmisiRanapContext/ReservationFeature/ReservationRepoTest.cs` | UT-RP-01..03 |
| `AdmisiRanapContext/AdmissionFeature/AdmissionRepoTest.cs` | UT-RP-01..03 |
| `AdmisiRanapContext/WaitingListFeature/WaitingListRepoTest.cs` | UT-RP-01..03, HasActiveByRegId |
| `AdmisiRanapContext/AdmisiRanapPersistenceIntegrationTest.cs` | IT-DL-01 (graceful skip) |

---

## Tables & Indexes

| Table | PK | Indexes |
|-------|-----|---------|
| `BILRG_AdmOpnameRequest` | `OpnameRequestId VARCHAR(12)` | `(OpnameRequestStatus, CrtDate)` |
| `BILRG_AdmReservation` | `ReservationId VARCHAR(12)` | `(ReservationStatus, PlannedDate)` |
| `BILRG_AdmAdmission` | `RegId VARCHAR(10)` | `(AdmissionStatus, CrtDate)`, `(PasienId, AdmissionStatus)` |
| `BILRG_BedWaitingList` | `WaitingListId VARCHAR(12)` | `(WaitingListStatus, CrtDate)`, `(BangsalId, WaitingListStatus)` |

All tables include standard audit columns (`CrtUser`, `CrtDate`, `UpdUser`, `UpdDate`, `VodUser`, `VodDate`).

**Patient demographics strategy:**

| Table | Patient storage |
|-------|-----------------|
| `BILRG_AdmOpnameRequest` | `PasienId` only — hydrated from `tc_mr` on read |
| `BILRG_AdmAdmission` | `PasienId` only — hydrated from `tc_mr` on read |
| `BILRG_AdmReservation` | Snapshot columns (`PasienName`, `TglLahir`, `Gender`) |
| `BILRG_BedWaitingList` | `PasienId` only — hydrated from `tc_mr` on read |

Opname Request, Admission, and Waiting List DAL `GetData`/`ListData` (and `WaitingListWorklistDal.List`) use `LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr` to populate `PasienReff` via `fs_nm_pasien`, `fd_tgl_lahir`, `fs_jns_kelamin`.

Doctor (Opname Request), kelas, and bangsal snapshots remain on their respective tables.

---

## Design Decisions

| Topic | Decision |
|-------|----------|
| Aggregate shape | Flat roots — no detail tables (unlike TarifPolicy) |
| Repo pattern | `LoadEntity` → `Match` → Insert/Update (JenisTarif reference) |
| Admission key | `ILoadEntity<AdmissionModel, IRegKey>` — reuses registration id space |
| Reservation ID prefix | `RSV` (as implemented in Phase 1 domain; plan doc says `RES`) |
| OpnameRequest / Admission / WaitingList patient | Persist `PasienId` only; hydrate `PasienReff` from `tc_mr` on read (`LEFT JOIN fs_mr`) |
| Reservation patient | Snapshot columns persisted (only aggregate with patient snapshots) |
| TglLahir (Reservation) | `DATETIME` in SQL; `DateOnly` ↔ `DateTime` in DTO |
| TglLahir (OpnameRequest/Admission/WaitingList read) | `fd_tgl_lahir VARCHAR(10)` from `tc_mr`; parsed to `DateOnly` in DTO `ToModel()` |
| Active WL check | `IWaitingListDal.HasActiveByRegId` queries `BILRG_BedWaitingList` only (BR-RI-008 support for Phase 3) |
| Ward worklist | `IWaitingListWorklistDal` — queries `BILRG_BedWaitingList` with `tc_mr` join; no join to `BILRG_AdmAdmission`; default filter active statuses (Waiting, Accepted) |
| Void filter | List queries use `VodDate = '3000-01-01'` sentinel |
| DI | Worklist DAL manually registered; other DALs/repos via Scrutor |

---

## Test Coverage

| ID | Scenario | Status |
|----|----------|--------|
| UT-RP-01 | New aggregate insert | Pass (×4 repos) |
| UT-RP-02 | Existing aggregate update | Pass (×4 repos) |
| UT-RP-03 | Load reconstructs full aggregate | Pass (×4 repos) |
| UT-RP-04 | `HasActiveByRegId` delegates to DAL | Pass |
| IT-DL-01 | SQL round-trip + worklist (graceful skip if tables missing) | Pass |
| DT-* (Phase 1) | Domain invariant tests | Pass (20 tests) |

---

## Verification

```
dotnet build src/bilreg/Bilreg.Infrastructure/Bilreg.Infrastructure.csproj  → succeeded
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmisiRanapContext" → Passed: 34, Failed: 0
```

**Acceptance criteria met:**

- Aggregate round-trip save/load via repos
- Active Waiting List query (`IWaitingListWorklistDal`) does not scan Admission history
- Repos contain no workflow orchestration
- Waiting List persisted independently (`BILRG_BedWaitingList` separate from `BILRG_AdmAdmission`)
- No changes to legacy `RegFeature` or existing API routes

---

## Explicitly Out of Scope (Phase 3+)

| Item | Phase |
|------|-------|
| MediatR commands/queries (10 use cases) | 3 |
| REST controllers | 4 |
| Integration gateways (Doctor, Pasien, Ward) | 5 |
| Permission policies | 6 |
| Cross-aggregate orchestration (fulfill Opname, realize Reservation, Process Admission) | 3 |

---

## Next Phase

**Phase 3 — Application use cases:** Implement all 10 architecture use cases with cross-aggregate orchestration in MediatR handlers; enforce BR-RI-005/006/008 in application layer.

**Skill:** `docs/skills/use-case-generation.md`

---

## Related Artifacts

- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
- `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-0-implementation-report.md`
- `docs/contexts/admisi-ranap/admisi-ranap-phase-1-implementation-report.md`
