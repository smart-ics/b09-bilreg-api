# Authorization Simplification Report — Bilreg.Api

**Date:** 2026-07-01  
**Scope:** `src/bilreg/Bilreg.Api` and related auth infrastructure

---

## Executive Summary

Bilreg.Api authorization has been simplified from a mixed model (JWT authentication + one custom permission policy + 88% anonymous endpoints) to **authentication-only**:

- **JWT Bearer validation** is unchanged — issuer, audience, signing key, and middleware order are preserved.
- **All 44 active controllers** now declare `[Authorize]` at the class level.
- **5 endpoints** remain explicitly public via `[AllowAnonymous]`.
- **Permission-based authorization** (`TataRekeningVerifikator` policy, `PermissionAuthorizationHandler`, static `RolePermissionDto` mapping) has been removed.
- **`AddAuthorization()`** is now a plain registration with no custom policies or handlers.
- **No `FallbackPolicy`** was introduced — every controller explicitly declares its authorization intent.

This is a **breaking change** for clients that previously called ~195 endpoints without a JWT. After deployment, all non-anonymous routes require `Authorization: Bearer {token}`.

---

## Files Modified

### API layer

| File | Change |
|------|--------|
| `Bilreg.Api/Configurations/PresentationService.cs` | Replaced custom policy/handler registration with `services.AddAuthorization()` |
| `Bilreg.Api/Controllers/**/*.cs` (44 active controllers) | Added `[Authorize]`; 5 action-level `[AllowAnonymous]` |
| `Bilreg.Test/.../TataRekeningApiIntegrationTest.cs` | Updated API05 test: any authenticated user succeeds (no role gate) |

### Deleted files

| File | Layer |
|------|-------|
| `Bilreg.Api/Authorization/PermissionAuthorizationHandler.cs` | API |
| `Bilreg.Api/Authorization/PermissionRequirement.cs` | API |
| `Bilreg.Api/Authorization/TataRekeningPolicies.cs` | API |
| `Bilreg.Infrastructure/Shared/User/RolePermissionDto.cs` | Infrastructure |
| `Bilreg.Infrastructure/Shared/User/RolePermissionRepo.cs` | Infrastructure |
| `Bilreg.Application/Shared/User/IRolePermissionRepo.cs` | Application |
| `Bilreg.Domain/Shared/User/RolePermissionModel.cs` | Domain |
| `Bilreg.Domain/Shared/User/PermissionModel.cs` | Domain |

### Preserved (unchanged)

| File | Reason |
|------|--------|
| `Bilreg.Api/Authorization/HttpCurrentUserContext.cs` | Reads authenticated user ID from JWT for use cases |
| JWT `AddJwtBearer(...)` configuration | Authentication must continue working |
| Swagger Bearer security definition | API documentation unchanged |

---

## Removed Components

### Policies removed

| Policy name | Requirement | Previously used by |
|-------------|-------------|------------------|
| `TataRekeningVerifikator` | Authenticated + `TATA-REKENING-WRITE` permission | `TataRekeningController` (all actions except `Open`) |

### Permission handlers removed

- `PermissionAuthorizationHandler` — checked `"permission"` claims and role→permission mapping via `RolePermissionDto`

### Role/permission mapping removed

Static mappings in `RolePermissionDto.ListData()`:

| RoleId | RoleName | Permission IDs (sample) |
|--------|----------|-------------------------|
| `ADM-SPV` | ADMISI SPV | `REGJLNWLK-*`, `REGJLNBOOK-*`, `BOK-*` |
| `ADM-USR` | ADMISI USER | `REGJLNWLK-CREATE/EDIT`, `REGJLNBOOK-CREATE/EDIT`, `BOK-CREATE/EDIT` |
| `VERIF-SPV` | VERIFIKATOR SPV | `TATA-REKENING-READ`, `TATA-REKENING-WRITE` |
| `VERIF-USR` | VERIFIKATOR USER | `TATA-REKENING-READ`, `TATA-REKENING-WRITE` |

Supporting types removed: `PermissionRequirement`, `RolePermissionModel`, `PermissionModel`, `IRolePermissionRepo`, `RolePermissionRepo`.

**Kept:** `RoleModel` / `IRoleKey` in Domain — unrelated user-role domain, not wired to endpoint authorization.

---

## Controllers Updated

All **44 active controllers** now have class-level `[Authorize]`:

### Admisi (20)

| Controller | Route prefix |
|------------|--------------|
| `AntrianController` | `api/Antrian` |
| `BookingController` | `api/Booking` |
| `JadwalPraktekController` | `api/JadwalPraktek` |
| `JadwalPraktekEffectiveController` | `api/JadwalPraktekEffective` |
| `JadwalPraktekHarianController` | `api/JadwalPraktekHarian` |
| `PraktekDokterController` | `api/PraktekDokter` |
| `RuangController` | `api/Ruang` |
| `GrupJaminanController` | `api/GrupJaminan` |
| `JaminanController` | `api/Jaminan` |
| `PolisController` | `api/Polis` |
| `TipeJaminanController` | `api/TipeJaminan` |
| `LayananController` | `api/Layanan` |
| `LayananDkController` | `api/LayananDk` |
| `PpaController` | `api/Ppa` |
| `RegController` | `api/Reg` |
| `JenisInapController` | `api/JenisInap` |
| `KarcisController` | `api/Karcis` |
| `ProsedurMasukInapController` | `api/ProsedurMasukInap` |
| `CaraMasukDkController` | `api/CaraMasukDk` |
| `RujukanController` | `api/Rujukan` |

### Bed Usage (5)

| Controller | Route prefix |
|------------|--------------|
| `OperationController` | `api/Operation` |
| `OrderOpController` | `api/OrderOp` |
| `ScheduleOpController` | `api/ScheduleOp` |
| `RoomRateController` | `api/RoomRate` |
| `WardController` | `api/Ward` |

### Bill (2)

| Controller | Route prefix |
|------------|--------------|
| `KelasController` | `api/Kelas` |
| `TarifController` | `api/Tarif` |

### Charge (4)

| Controller | Route prefix |
|------------|--------------|
| `NilaiTarifController` | `api/NilaiTarif` |
| `TarifMigrationController` | `api/TarifMigration` |
| `TarifPolicyController` | `api/TarifPolicy` |
| `TindakanController` | `api/Tindakan` |

### IGD (4)

| Controller | Route prefix |
|------------|--------------|
| `BedIgdController` | `api/BedIgd` |
| `BhpIgdController` | `api/BhpIgd` |
| `IgdVisitController` | `api/IgdVisit` |
| `TindakanIgdController` | `api/TindakanIgd` |

### Lab (5)

| Controller | Route prefix |
|------------|--------------|
| `LabComponentMasterController` | `api/LabContext/LabComponentMasterFeature` |
| `LabOrderController` | `api/LabContext/LabOrderFeature` |
| `LabOwareController` | `api/LabContext/LabOwareFeature` |
| `LabResultController` | `api/LabContext/LabResultFeature` |
| `LabTestDefinitionController` | `api/LabContext/LabTestDefinitionFeature` |

### Pasien (3)

| Controller | Route prefix |
|------------|--------------|
| `DemografiController` | `api/Demografi` |
| `PasienController` | `api/Pasien` |
| `StatusSosialController` | `api/StatusSosial` |

### Payment (1)

| Controller | Route prefix |
|------------|--------------|
| `TataRekeningController` | `api/tatarekening` |

**Note:** 29 commented-out controllers (mostly `BillContext` master data) were not modified — they do not register routes at runtime.

---

## Anonymous Endpoints

Five endpoints intentionally remain public via `[AllowAnonymous]`:

| Method | Endpoint | Controller action | Rationale |
|--------|----------|-------------------|-----------|
| `GET` | `/api/tatarekening/{regId}` | `TataRekeningController.Open` | Documented: read-only workspace open without auth ([frontend-integration-guide.md](docs/contexts/TataRekening/frontend-integration-guide.md)) |
| `GET` | `/api/JadwalPraktekEffective/{dokterId}/{tglYmd}` | `JadwalPraktekEffectiveController.List` | Documented: public schedule lookup for HiDok |
| `GET` | `/api/JadwalPraktekEffective/{dokterId}/{tglYmd}/{jamMulai}` | `JadwalPraktekEffectiveController.Get` | Same |
| `POST` | `/api/Booking/createFromHidok` | `BookingController.CreateFromHidok` | HiDok server-to-server booking callback |
| `DELETE` | `/api/Booking/{bookingIdHidok}/hidok` | `BookingController.DeleteFromHidok` | HiDok server-to-server cancellation callback |

All other ~217 active endpoints require a valid JWT.

**Not present in this API:** login/token issuance, health check, or dedicated webhook controllers.

---

## Potential Integration Risks

Endpoints that were previously anonymous and are **now JWT-protected**. Consumers must be updated to send `Authorization: Bearer {token}` or be granted `[AllowAnonymous]` in a follow-up change.

### HiDok (partially mitigated)

| Endpoint | Risk |
|----------|------|
| `POST /api/Booking/createFromHidok` | **Preserved** — `[AllowAnonymous]` |
| `DELETE /api/Booking/{bookingIdHidok}/hidok` | **Preserved** — `[AllowAnonymous]` |
| `GET /api/JadwalPraktekEffective/*` | **Preserved** — `[AllowAnonymous]` |
| All other `BookingController` actions | **Now requires JWT** — HiDok or other clients calling list/search/save without tokens will receive 401 |

### Lab / EMR

| Controller | Key endpoints now protected | Risk |
|------------|----------------------------|------|
| `LabOrderController` | `byEmrOrderId`, worklist, charge, collect, cancel, release | EMR/LIS bridges calling without JWT will fail |
| `LabOwareController` | `enqueue`, `process`, `retry`, `worklist` | Background workers or Oware integration may need service-account JWT |
| `LabResultController` | Result entry/release endpoints | Lab workstation clients need tokens |
| `LabTestDefinitionController` | Test definition CRUD | Admin tooling needs tokens |
| `LabComponentMasterController` | Component master CRUD | Admin tooling needs tokens |

### Admisi / Registration

| Controller | Risk |
|------------|------|
| `RegController` | Walk-in registration, inap registration — all internal UIs must send JWT |
| `AntrianController` | Queue generation and management — kiosk/display clients need tokens |
| `PasienController`, `DemografiController` | Patient lookup and demographics — all clients need tokens |
| `JadwalPraktekController`, `JadwalPraktekHarianController` | Schedule admin — internal UIs need tokens |

### IGD

| Controller | Risk |
|------------|------|
| `IgdVisitController` | Triage, bed assignment, discharge — IGD UI must send JWT |
| `BedIgdController`, `BhpIgdController`, `TindakanIgdController` | Supporting IGD operations — all need tokens |

### Tata Rekening

| Endpoint | Risk |
|----------|------|
| `GET /api/tatarekening/{regId}` | **Unchanged** — still anonymous |
| All POST/PATCH actions (close, verify, finalize, etc.) | Previously required Verifikator role; now any authenticated JWT suffices — **authorization loosened** for writes |

### Tarif

| Controller | Risk |
|------------|------|
| `TarifPolicyController`, `TarifMigrationController` | Already required JWT — no change |
| `NilaiTarifController` | **Now requires JWT** (was commented-out `[Authorize]`) |

### BPJS / third-party

No dedicated BPJS callback routes were identified. Standard API consumers are affected by the global JWT requirement.

### Deployment recommendation

1. Audit all API consumers (internal UIs, EMR, HiDok, Lab bridges, mobile apps).
2. Ensure token issuer provides JWTs accepted by Bilreg.Api (`Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`).
3. Coordinate rollout — this change will break unauthenticated callers immediately.
4. Monitor 401/403 rates after deployment.

---

## Remaining Work

When the application reaches a more mature stage, implement a full permission-based authorization model:

1. **Database-backed roles and permissions** — connect `BILRG_User` / `BILRG_UserRole` tables (referenced in investigation) to endpoint authorization instead of static DTOs.
2. **Policy registration** — `AddPolicy` per bounded context (Admisi, Lab, Tata Rekening, Tarif, IGD).
3. **Authorization handler or claims transformation** — map roles to permissions at token validation or via a custom handler.
4. **Tata Rekening write gates** — re-introduce Verifikator-only policy for POST actions (reads may stay anonymous).
5. **Tarif admin gates** — Keuangan/Supervisor role separation per [tarif-04-api-contract.md](docs/contexts/tarif/tarif-04-api-contract.md).
6. **Admisi permission granularity** — wire `REGJLNWLK-*`, `BOK-*` permissions from the removed `RolePermissionDto` seed data.
7. **Integration auth strategy** — decide whether external systems (HiDok, Lab workers) use service-account JWTs vs. `[AllowAnonymous]` + API keys.
8. **Optional:** global `FallbackPolicy` once all controllers are verified — not recommended during early development.

---

## Verification

- `dotnet build` — Bilreg.Api, Bilreg.Test, Bilreg.Infrastructure, Bilreg.Application, Bilreg.Domain compile successfully.
- `TataRekeningApiIntegrationTest` — 16/16 tests pass (including updated API05 authenticated-user test).
- Swagger Bearer JWT scheme — unchanged in `PresentationService.cs`.
