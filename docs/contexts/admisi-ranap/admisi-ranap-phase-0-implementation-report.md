# Admisi Ranap Phase 0 — Implementation Report

**Status:** LIVE  
**Date:** 2026-07-07  
**Scope:** `AdmisiRanapContext` folder scaffolding and module conventions (Phase 0 only).

**Out of scope:** Domain models, SQL scripts, repositories, use-case handlers, REST controllers, DI registration, integration gateways, permission policies.

**Amendment (2026-07-07):** Admission workflow table prefix renamed from `BILRG_RegIn*` to `BILRG_Adm*` (`BILRG_AdmOpnameRequest`, `BILRG_AdmReservation`, `BILRG_AdmAdmission`). Waiting List remains `BILRG_BedWaitingList`.

---

## Summary

Phase 0 establishes the greenfield **Rawat Inap Admission** module boundary as `AdmisiRanapContext` across all six solution projects. Empty feature folders (`OpnameRequest`, `Reservation`, `Admission`, `WaitingList`) and `Integration` placeholders are registered in `.csproj` / `sqlproj` so git, Visual Studio, and parallel Phase 1+ work have predictable locations. The solution builds with zero new behaviour; no existing endpoints or `AdmisiContext` flows were modified.

---

## Folders Created

| Project | Path |
|---------|------|
| Bilreg.Domain | `AdmisiRanapContext/`, `OpnameRequestFeature/`, `ReservationFeature/`, `AdmissionFeature/`, `WaitingListFeature/` |
| Bilreg.Application | `AdmisiRanapContext/`, four features + `UseCases/` each, `Integration/` |
| Bilreg.Infrastructure | `AdmisiRanapContext/`, four features, `Integration/` |
| Bilreg.Api | `Controllers/AdmisiRanapContext/` |
| Bilreg.SqlDb | `AdmisiRanapContext/`, four feature folders (SQL deferred to Phase 2) |
| Bilreg.Test | `AdmisiRanapContext/`, four feature folders |

Empty directories are tracked via `.gitKeep` files and `<Folder Include>` entries in project files.

---

## Project File Changes

| File | Change |
|------|--------|
| `src/bilreg/Bilreg.Domain/Bilreg.Domain.csproj` | Added 5 `AdmisiRanapContext` folder entries |
| `src/bilreg/Bilreg.Application/Bilreg.Application.csproj` | Added 11 folder entries (features, UseCases, Integration) |
| `src/bilreg/Bilreg.Infrastructure/Bilreg.Infrastructure.csproj` | Added 6 folder entries (first Folder group in this project) |
| `src/bilreg/Bilreg.Api/Bilreg.Api.csproj` | Added `Controllers\AdmisiRanapContext\` |
| `src/bilreg/Bilreg.Test/Bilreg.Test.csproj` | Added 5 folder entries |
| `src/bilreg/Bilreg.SqlDb/Bilreg.SqlDb.sqlproj` | Added 5 folder entries |

No changes to `InfrastructureService.cs`, `ApplicationService.cs`, or `DomainService.cs`.

---

## Conventions Established

Per `admisi-ranap-implementation-plan.md` §4:

| Concern | Convention |
|---------|------------|
| Context name | `AdmisiRanapContext` |
| Domain type | `{Name}Model` (e.g. `AdmissionModel`) |
| Value objects | `{Name}Type`, `{Name}Reff` |
| Key | `I{Name}Key` |
| Status enum | `{Name}StatusEnum` |
| Command / Query | `Rir{Name}{Action}Cmd`, `Rir{Name}{Action}Qry` (`Rir` = Rawat Inap Ranap) |
| Handler | `Rir{Name}{Action}Handler` |
| Repo interface | `Application/.../I{Name}Repo` |
| Repo implementation | `Infrastructure/.../{Name}Repo` |
| API route prefix | `api/admisi-ranap/...` |
| Tables (Phase 2) | `BILRG_AdmOpnameRequest`, `BILRG_AdmReservation`, `BILRG_AdmAdmission`, `BILRG_BedWaitingList` |

**Feature folders:**

| Folder | Aggregate Root |
|--------|----------------|
| `OpnameRequestFeature` | Opname Request |
| `ReservationFeature` | Reservation |
| `AdmissionFeature` | Admission |
| `WaitingListFeature` | Waiting List |

---

## Explicitly Out of Scope (Later Phases)

| Item | Phase |
|------|-------|
| Domain models, enums, state machines, BR-RI invariants | 1 |
| SQL DDL, DTOs, DALs, repositories | 2 |
| MediatR commands/queries and handlers (10 use cases) | 3 |
| REST controllers (`OpnameRequest`, `Reservation`, `Admission`, `WaitingList`) | 4 |
| Integration gateways (Doctor, Patient Administration, Ward) | 5 |
| Permission-based authorization | 6 |
| Hardening, migration, E2E validation | 7 |

---

## Verification

All C# projects build successfully (pre-existing warnings only; no new errors):

```
dotnet build src/bilreg/Bilreg.Domain/Bilreg.Domain.csproj       → succeeded
dotnet build src/bilreg/Bilreg.Application/Bilreg.Application.csproj → succeeded
dotnet build src/bilreg/Bilreg.Infrastructure/Bilreg.Infrastructure.csproj → succeeded
dotnet build src/bilreg/Bilreg.Api/Bilreg.Api.csproj             → succeeded
dotnet build src/bilreg/Bilreg.Test/Bilreg.Test.csproj           → succeeded
```

**Acceptance criteria met:**

- Build succeeds
- No new controllers, handlers, or SQL scripts
- No changes to existing API routes or `AdmisiContext` registration code
- Conventions documented in implementation plan §4 and this report

`Bilreg.SqlDb` SSDT build requires Visual Studio; folder entries are registered for Phase 2 SQL scripts.

---

## Next Phase

**Phase 1 — Domain layer:** Implement four aggregate roots (`OpnameRequestModel`, `ReservationModel`, `AdmissionModel`, `WaitingListModel`) with state machines, BR-RI-001…BR-RI-012 invariants, and domain unit tests (`*TypeTest.cs` or `*ModelTest.cs` per feature).

**Skill:** `docs/skills/feature-model-generation.md`

---

## Related Artifacts

- `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md`
- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
