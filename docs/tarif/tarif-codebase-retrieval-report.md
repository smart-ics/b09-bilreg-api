# Tarif Subsystem — Codebase Retrieval Report

**Agent role:** Feature Knowledge Retrieval (read-only)  
**Stewardship reference:** `docs/agents/feature-knowledge-steward.md`  
**Scope date:** 2026-05-26  
**Code focus:** `Bilreg.Domain/ChargeContext`, `Bilreg.Application/ChargeContext`, `Bilreg.Infrastructure/ChargeContext` (Tarif-related), plus cross-context types explicitly referenced by Tarif code.

**In scope (domain types):** `TarifType`, `NilaiTarifType`, `KomponenType`, `GroupKomponenType`, `Kelas`, `TipeTarif`, `SatTugas`  
**Explicitly out of scope (not in codebase):** `TarifPolicy`, `TarifVariant`, `BillingTransaction` (as a named subsystem)  
**Boundary:** Billing workflow is not inferred; only **documented call-site dependencies** are listed.

---

## 1. Current Codebase Overview

The Tarif area is implemented as a **pragmatic tactical DDD slice** inside the **ChargeContext** bounded context, using:

- **Rich-ish domain records** (`record` types, key interfaces, `Reff` projections)
- **Explicit DTO ↔ domain mapping** in Infrastructure
- **SQL-first persistence** via **Dapper** and **SqlBulkCopy**
- **Repository + DAL** split (`*Repo` orchestrates, `*Dal` executes SQL)
- **MediatR** for a small set of Tarif-specific use cases; broader consumption happens from **Reg** and **Tindakan** flows

### Layer map (Tarif-centric)

| Layer | Location | Tarif-related contents |
|-------|----------|------------------------|
| Domain | `Bilreg.Domain/ChargeContext/TarifFeature/` | Core types: Tarif, NilaiTarif, Komponen, GroupKomponen, TipeTarif, JenisTarif, GroupTarif*, TipeRek |
| Domain (cross-ref) | `Bilreg.Domain/BedUsageContext/WardFeature/KelasModel.cs` | `KelasType`, `KelasReff`, `IKelasKey` |
| Domain (cross-ref) | `Bilreg.Domain/AdmisiContext/PpaFeature/SatTugasType.cs` | `SatTugasType`, `ISatTugasKey` |
| Domain (cross-ref) | `Bilreg.Domain/PaymentContext/TrsBillingFeature/CoaType.cs` | `CoaType` used by `KomponenType` |
| Application | `Bilreg.Application/ChargeContext/TarifFeature/` | `INilaiTarifRepo`, `ITarifRepo`, 3 MediatR use cases |
| Application | `Bilreg.Application/ChargeContext/TindakanFeature/` | Repo abstractions for Komponen, TipeTarif, GroupKomponen, GroupTarif*, JenisTarif, TipeRek |
| Infrastructure | `Bilreg.Infrastructure/ChargeContext/TarifFeature/` | DALs, DTOs, Repos |
| Infrastructure | `Bilreg.Infrastructure/BedUsageContext/WardFeature/KelasDal.cs` | Kelas persistence |
| SQL | `Bilreg.SqlDb/ChargeContext/TarifFeature/` | Legacy `ta_*` + new `BILRG_*` tables |
| API | `Bilreg.Api/Controllers/ChargeContext/NilaiTarifController.cs`, `BillContext/TindakanSub/TarifController.cs` | 3 live Tarif/NilaiTarif endpoints |

### DI registration

- **DALs:** Scrutor scan in `Bilreg.Api/Configurations/InfrastructureService.cs` registers `IInsert<>`, `IUpdate<>`, `IDelete<>`, `IGetData<,>`, `IListData*` implementations from Infrastructure assembly.
- **Repos:** Same scan registers `ILoadEntity<,>`, `ISaveChange<>`, `IDeleteEntity<>` implementors (e.g. `NilaiTarifRepo`, `KomponenRepo`, `TipeTarifRepo`).
- **MediatR:** `ApplicationService.cs` registers handlers from `Bilreg.Application`.

No Tarif-specific manual registrations beyond assembly scanning.

---

## 2. Existing Domain Model

### 2.1 Classification summary

| Concept | Domain type | Aggregate? | Notes |
|---------|-------------|------------|-------|
| Tarif (master) | `TarifType` | **Partial / catalog entity** | Immutable-style record; no `Save` on `ITarifRepo` |
| Nilai tarif | `NilaiTarifType` | **Composite value cluster** | Header + child `NilaiTarifKomponenType` list |
| Komponen | `KomponenType` | **Master entity** | COA + SatTugas + GroupKomponen |
| Group komponen | `GroupKomponenType` | **Lookup** | Simple id/name |
| Tipe tarif | `TipeTarifType` | **Lookup** | In `TipeTarifModel.cs` file |
| Kelas | `KelasType` | **Ward context** | Referenced via `KelasReff` in NilaiTarif |
| Satuan tugas | `SatTugasType` | **Admisi/PPA context** | Child collection on Komponen |
| Jenis / Group tarif / Group tarif DK / Tipe rek | `JenisTarifType`, `GroupTarifType`, `GroupTarifDkType`, `TipeRekType` | **Lookups on Tarif** | Attached to `TarifType`; repos exist, limited API |

There are **no dedicated domain service classes** under `TarifFeature`. Behavior lives on types (`KomponenType.IsValidPpa`, `TindakanKomponenWithPpaType.Create`) and in application handlers.

### 2.2 `TarifType` — `Bilreg.Domain/ChargeContext/TarifFeature/TarifType.cs`

- **Identity:** `TarifId`, `TarifName`
- **Associations:** `GroupTarifType`, `GroupTarifDkType`, `JenisTarifType`, `RekapCetakReff` (PaymentContext)
- **Patterns:** `TarifType.Default`, `TarifType.Key(id)`, `TarifReff`, `ITarifKey`
- **Validation:** Constructor has no Guard clauses (unlike Komponen/TipeTarif)
- **Persistence capability:** Read-only at application boundary (`ITarifRepo` has load/list only)

### 2.3 `NilaiTarifType` — `NilaiTarifType.cs`

- **Identity:** `NilaiTarifId` (ULID on import)
- **Variant dimensions:** `TipeTarifReff`, `KelasReff` (implements `INilaiTarifVariant`)
- **Composite key:** `INilaiTarifCompositKey` = Tarif + TipeTarif + Kelas (`KeyComposite` factory)
- **Amount:** `Nilai` (header total)
- **Children:** private `List<NilaiTarifKomponenType>` exposed as `ListKomponen`
- **Child shape:** `NilaiTarifKomponenType(int NoUrut, KomponenReff Komponen, decimal Nilai)`

No domain methods enforce `Sum(ListKomponen) == Nilai` or uniqueness of (TarifId, TipeTarifId, KelasId).

### 2.4 `KomponenType` — `KomponenType.cs`

- **Identity:** `KomponenId`, `KomponenName`
- **Group:** `GroupKomponenType`
- **Accounting:** `CoaType RekPdpt`, `CoaType RekDiskon` (maps to `t_rek` via DAL)
- **Operational:** `IEnumerable<SatTugasType> ListSatTugas`
- **Invariant (implemented):** `Guard` on id/name/group in constructor
- **Behavior (implemented):** `IsValidPpa(PpaType)` — true when Komponen has SatTugas entries **and** intersects PPA’s SatTugas list; false if Komponen has **no** SatTugas

### 2.5 `GroupKomponenType` — `GrupKomponenType.cs` (file name) / type `GroupKomponenType`

- `Create` with Guard; properties `GroupKomponenId`, `GroupKomponenName`

### 2.6 `TipeTarifType` — `TipeTarifModel.cs`

- Properties: `TipeTarifId`, `TipeTarifName`, `IsAktif`, `NoUrut`
- `Create` validates non-empty ids/names, non-negative `NoUrut`

### 2.7 Cross-context types used by Tarif

| Type | File | Role in Tarif |
|------|------|----------------|
| `KelasType` / `KelasReff` | `WardFeature/KelasModel.cs` | NilaiTarif variant dimension; Tindakan stores `KelasReff` |
| `SatTugasType` | `PpaFeature/SatTugasType.cs` | Komponen eligibility for PPA-assigned tindakan lines |
| `CoaType` | `TrsBillingFeature/CoaType.cs` | Komponen revenue/discount accounts |

### 2.8 Key / Reff pattern (ubiquitous)

Repeated across Tarif types:

- `I{Name}Key` — identity-only interface
- `{Name}Type.Default` — sentinel `"-"` ids
- `{Name}Type.Key(string id)` — key stub for loads
- `{Name}Reff` — id + display name for embedding in other aggregates (`TindakanModel`, `NilaiTarifType`)

### 2.9 Related Charge types (outside Tarif folder, Tarif-dependent)

- `TindakanModel` (`TindakanFeature/TindakanModel.cs`) — created from `NilaiTarifType` + `KomponenPpaView`; stores `TarifReff`, `TipeTarifReff`, `KelasReff`, komponen lines
- `TindakanKomponenWithPpaType.Create` — calls `KomponenType.IsValidPpa`

---

## 3. Existing Persistence Model

### 3.1 Strategy

- **ORM:** None for Tarif; **Dapper** for CRUD/read, **SqlBulkCopy** for bulk import of NilaiTarif and children
- **Mapping:** Explicit `*Dto` records with `FromModel` / `ToModel`; legacy column names preserved (`fs_kd_*`)
- **New vs legacy tables:** Application **reads/writes operational nilai** to `BILRG_*`; **imports** from legacy transactional tables `ta_trs_tarif2` / `ta_trs_tarif3`

### 3.2 Table / mapping matrix

| Domain concept | Primary table(s) | DAL | DTO |
|----------------|------------------|-----|-----|
| Tarif | `TA_TARIF` (+ joins) | `TarifDal` | `TarifDto` |
| NilaiTarif | `BILRG_NilaiTarif` | `NilaiTarifDal` | `NilaiTarifDto` |
| NilaiTarif komponen | `BILRG_NilaiTarifKomponen` | `NilaiTarifKompDal` | `NilaiTarifKompDto` |
| Komponen | `ta_detil_tarif` | `KomponenDal` | `KomponenDto` |
| Komponen ↔ SatTugas | `ta_detil_tarif2` | `KomponenSatTugasDal` | `KomponenSatTugasDto` |
| Group komponen | `ta_grup_detil_tarif` | `GroupKomponenDal` | `GroupKomponenDto` |
| Tipe tarif | `ta_tarif_tipe` | `TipeTarifDal` | `TipeTarifDto` (in `TipeTarifRepo.cs`) |
| Kelas | `ta_kelas` | `KelasDal` | `KelasDto` |
| Group/Jenis tarif, Group tarif DK, Tipe rek | `ta_grup_tarif`, `ta_jenis_tarif`, `ta_grup_tarif_dk`, `ta_rek_tipe` | respective `*Dal` | respective `*Dto` |
| Legacy nilai source | `ta_trs_tarif2`, `ta_trs_tarif3` | `NilaiTarifDal.ListData2/3` | `ta_trs_tarif2_dto`, `ta_trs_tarif3_dto` |

**SQL definitions (repo):**

- `Bilreg.SqlDb/ChargeContext/TarifFeature/BILRG_NilaiTarif.sql`
- `Bilreg.SqlDb/ChargeContext/TarifFeature/BILRG_NilaiTarifKomponen.sql`
- `Bilreg.SqlDb/ChargeContext/TarifFeature/ta_tarif.sql`, `ta_detil_tarif.sql`, `ta_detil_tarif2.sql`, `ta_tarif_tipe.sql`, `ta_kelas.sql`, `ta_grup_detil_tarif.sql`

### 3.3 Naming conventions

| Layer | Convention | Example |
|-------|------------|---------|
| Legacy DB | `fs_kd_*`, `fs_nm_*`, `fn_*`, `fb_*` | `fs_kd_detil_tarif` → KomponenId |
| New BILRG tables | PascalCase columns | `NilaiTarifId`, `TarifId` |
| Domain (modern) | PascalCase English/Indonesian mix | `KomponenId`, `NilaiTarifId` |
| Legacy semantic rename in domain | detil_tarif → Komponen | DAL still uses `ta_detil_tarif` |

### 3.4 Entity relationships (as implemented in SQL joins)

```text
TA_TARIF (Tarif)
    │
    ├──< BILRG_NilaiTarif (NilaiTarif) ──> ta_tarif_tipe (TipeTarif)
    │         │                      └──> ta_kelas (Kelas)
    │         │
    │         └──< BILRG_NilaiTarifKomponen ──> ta_detil_tarif (Komponen)
    │
    └── (search) ta_tarif4.fs_kd_layanan — used in NilaiTarifDal.ListData(layanan, variant, keyword)

ta_detil_tarif (Komponen)
    ├──> ta_grup_detil_tarif (GroupKomponen)
    ├──> t_rek (RekPdpt, RekDiskon)
    └──< ta_detil_tarif2 >── td_sat_tugas (SatTugas)
```

**Legacy import path:**

```text
ta_trs_tarif2 (fs_kd_trs, fs_kd_tarif, fd_tgl_expired)
    └──< ta_trs_tarif3 (fs_kd_tarif, fs_kd_kelas, fs_kd_tipe, fs_kd_detil, fn_nilai)
              │
              Import() ──> BILRG_NilaiTarif + BILRG_NilaiTarifKomponen
```

Import filter (`NilaiTarifRepo.Import`): only rows where parent `ta_trs_tarif2.fd_tgl_expired = '3000-01-01'` and `fn_nilai > 0`. Header `Nilai` = **sum** of line `fn_nilai` per (tarif, kelas, tipe).

### 3.5 NilaiTarif: source of truth vs projection

| Store | Role in current code | Evidence |
|-------|----------------------|----------|
| `ta_trs_tarif2` / `ta_trs_tarif3` | **Legacy source for bulk import only** | `NilaiTarifRepo.Import()` reads via `ListData3`, clears BILRG tables, bulk inserts |
| `BILRG_NilaiTarif` (+ Komponen) | **Operational read model used by application** | All `LoadEntity`, `Search`, Tindakan/Reg flows read BILRG |
| `NilaiTarifRepo.SaveChanges` | **Implemented but unused** | No application caller of `SaveChanges` found (grep) |

**Conclusion:** **Hybrid**

- **Implemented:** legacy → import → BILRG projection
- **Runtime truth for app code:** `BILRG_*` tables
- **Not implemented:** continuous sync, policy-variant history, or policy-driven effective dating

### 3.6 Komponen: accounting vs fee distribution

| Role | Implemented? | Evidence |
|------|--------------|----------|
| **Accounting distribution unit** | **Yes** | `KomponenType.RekPdpt`, `RekDiskon` persisted to `ta_detil_tarif.fs_kd_rek`, `fs_kd_rek_diskon`; consumed in `TrsBillingType.CreateFromTindakan` (lines 66–68, 74–76) |
| **Fee / jasa distribution unit** | **Yes (operational)** | `ta_detil_tarif2` maps Komponen ↔ SatTugas; `IsValidPpa` gates PPA assignment; `TrsBilling2JasaType` created per tindakan komponen line |
| **Both** | **Yes** | Same master `KomponenType` feeds billing COA **and** PPA/jasa line construction |

Tarif subsystem **does not** implement billing posting logic; it **supplies** komponen master data and nilai breakdown consumed by `PaymentContext.TrsBillingFeature` at explicit call sites (documented below).

### 3.7 Known persistence gaps / risks (factual)

| Item | Status |
|------|--------|
| Unique constraint on `(TarifId, TipeTarifId, KelasId)` in `BILRG_NilaiTarif` | **Missing** in SQL script; composite load filters in memory (`NilaiTarifRepo.LoadEntity(compositKey)`) |
| `KomponenRepo.SaveChanges` persists SatTugas | **No** — only `KomponenDal`; `IKomponenSatTugasDal` used on read and in tests |
| `ta_tarif.sql` vs `TarifDal` | DAL expects columns (`fs_kd_grup_tarif`, `FB_AKTIF`, etc.) **not** in minimal `ta_tarif.sql` in repo — likely DB drift / partial script |
| `KelasDal.Update` SQL | Parameter/column mismatch in UPDATE statement (`KelasDal.cs` lines 49–57) — **bug visible in code** |
| Duplicate SQL file for grup detil | `ta_grup_detil_tarif.sql` and `ta_grup_komponen.sql` both define `ta_grup_detil_tarif` with different widths |

---

## 4. Existing API / Use Cases

### 4.1 TarifFeature MediatR use cases (only 3)

| Use case | Type | Handler | Repos / services |
|----------|------|---------|------------------|
| `TrfImportNilaiTarifCmd` | Command | `TrfImportNilaiTarifHandler` | `INilaiTarifRepo.Import()` |
| `TrfGetNilaiTarifQuery` | Query | `NilaiTarifGetHandler` | `INilaiTarifRepo`, `IKomponenRepo` (enrich SatTugas ids) |
| `TrfListTarifBrgQuery` | Query | `TrfListTarifBrgHandler` | `INilaiTarifRepo.Search`, `IStokRepo` |

Files: `Bilreg.Application/ChargeContext/TarifFeature/UseCases/*.cs`

### 4.2 HTTP endpoints (live)

| Method | Route | Controller | Use case |
|--------|-------|------------|----------|
| POST | `/api/NilaiTarif/import` | `NilaiTarifController` | `TrfImportNilaiTarifCmd` |
| GET | `/api/NilaiTarif/tarif-brg?layananId&kelasId&tipeTarifId&keyword` | `NilaiTarifController` | `TrfListTarifBrgQuery` |
| GET | `/api/Tarif/nilai/{id}/{tipeTarifId}/{kelasId}` | `BillContext/TindakanSub/TarifController` | `TrfGetNilaiTarifQuery` |

**Commented / removed:** `NilaiTarifController` had duplicate route for `TrfGetNilaiTarifQuery` (commented).

### 4.3 Master-data APIs (Komponen, TipeTarif, GroupKomponen, etc.)

Legacy controllers under `Bilreg.Api/Controllers/BillContext/TindakanSub/` (e.g. `KomponenTarifController`, `GrupKomponenController`, `JenisTarifController`) are **fully commented out**. Repo + DAL layers exist; **no active MediatR use cases** found in Application for Tarif master CRUD in the new structure.

### 4.4 Consumption outside TarifFeature (explicit dependencies)

These flows **read** Tarif concepts but live in other features:

| Flow | NilaiTarif access | Komponen / Tarif |
|------|-------------------|------------------|
| `TdkCreateTindakanCmd`, `TdkSaveTindakanCmd` | `LoadEntity(INilaiTarifKey)` by id | `IKomponenRepo`, `ITarifRepo`; creates `TrsBillingType` |
| Reg walk-in / booking / ubah kunjungan / ubah jaminan / darurat | `LoadEntity(INilaiTarifCompositKey)` via `NilaiTarifType.KeyComposite` | Same pattern for karcis/tindakan billing |
| Lab test definition | `TarifId` on lab master | `LabTestDefinitionController` GET `byTarif/{tarifId}` |

**Billing boundary note:** `TrsBillingType.CreateFromTindakan` in `PaymentContext` validates tarif/jaminan alignment and maps komponen COA to billing lines. This is an **explicit downstream dependency**, not Tarif subsystem logic.

### 4.5 Validation behavior

| Location | Behavior |
|----------|----------|
| Domain `Guard` | Komponen, GroupKomponen, TipeTarif, JenisTarif, GroupTarif*, TipeRek |
| Application handlers | `KeyNotFoundException`, `ArgumentException` (e.g. inactive reg, invalid komponen/PPA) |
| `TrfGetNilaiTarifQuery` | Throws if composite not found |
| FluentValidation | Registered in Application but **commented out** (`AddValidatorsFromAssembly`) |

No FluentValidation validators found for Tarif use cases.

### 4.6 Authorization behavior

- JWT authentication configured in `PresentationService.cs`
- Tarif/NilaiTarif controllers have **no** `[Authorize]` / `[AllowAnonymous]` attributes
- Inherits global `UseAuthentication` / `UseAuthorization` from `Program.cs` — **endpoint-level policy not visible in Tarif controllers**

---

## 5. Existing Architectural Direction

### 5.1 Clean Architecture layering

```text
API (Controllers)
    → Application (MediatR, I*Repo interfaces)
        → Domain (records, key interfaces, minimal behavior)
    Infrastructure (Repo, Dal, Dto) → SQL Server
```

**Dependency rule:** Domain has no infrastructure references. `KomponenType` references `CoaType` (Payment) and `SatTugasType` (Admisi) — cross-context domain references exist.

### 5.2 Module boundaries

- **Primary bounded context:** `ChargeContext` / `TarifFeature`
- **Shared master data contexts:** `BedUsageContext` (Kelas), `AdmisiContext` (SatTugas, Layanan for search)
- **Consumer contexts:** `TindakanFeature`, `RegFeature`, `PaymentContext` (TrsBilling), `LabContext` (tarif linkage)

### 5.3 Transaction handling

| Operation | Strategy |
|-----------|----------|
| Tindakan create/save + billing | `TransHelper.NewScope()` — `TdkCreateTindakanCmd`, `TdkSaveTindakanCmd` |
| NilaiTarif import | **No** explicit transaction wrapper in `Import()` — clear + bulk insert (two tables) |
| Single-entity repo saves | Per-statement Dapper / BCP |

### 5.4 Architectural patterns in use

- **Repository + DAL** (Nuna.Lib `ISaveChange`, `ILoadEntity`, `IListData`)
- **MayBe** for missing entities
- **Record DTO mapping** (explicit, low-magic)
- **Feature folders** over technical layers inside each project
- **Legacy compatibility** via `ta_*` table names and import pipeline

### 5.5 What is NOT present (confirmed)

- No `TarifPolicy`, `TarifVariant`, or `BillingTransaction` types in `.cs` codebase (grep: zero matches)
- No domain events for tarif changes
- No EF Core entities for Tarif

---

## 6. Existing Naming / Ubiquitous Language

### 6.1 Domain vs legacy vs file naming

| Ubiquitous term (code) | Legacy table / column | Notes |
|------------------------|----------------------|-------|
| Tarif | `ta_tarif` / `fs_kd_tarif` | |
| Komponen | `ta_detil_tarif` / `fs_kd_detil_tarif` | Legacy “detil tarif” |
| GroupKomponen | `ta_grup_detil_tarif` | File named `GrupKomponenType.cs`; type `GroupKomponenType` |
| NilaiTarif | `BILRG_NilaiTarif` | New naming |
| TipeTarif | `ta_tarif_tipe` | File `TipeTarifModel.cs` holds `TipeTarifType` |
| Kelas | `ta_kelas` | Type in WardFeature, not TarifFeature |
| SatTugas | `td_sat_tugas` via `ta_detil_tarif2` | Type in PpaFeature |

### 6.2 Interface / repo naming split

- Tarif-specific: `INilaiTarifRepo`, `ITarifRepo` → `Application/ChargeContext/TarifFeature`
- Reused “tindakan” naming: `IKomponenRepo`, `ITipeTarifRepo`, `IGroupKomponenRepo` → `Application/ChargeContext/TindakanFeature` (historical `BillContext.TindakanSub` namespace on some infra repos)

### 6.3 Prefix conventions

- Use cases: `Trf*` (e.g. `TrfGetNilaiTarifQuery`)
- Tests: `*DalTest`, `*RepoTests`

Aligns with mixed ID/EN style described in `docs/NAMING.md` (operational Indonesian terms preserved).

---

## 7. Detected Domain Invariants

### 7.1 Implemented in domain code

| Invariant | Where |
|-----------|-------|
| Komponen id/name required; group required | `KomponenType` constructor |
| GroupKomponen id/name required on create | `GroupKomponenType.Create` |
| TipeTarif id/name required; NoUrut ≥ 0 | `TipeTarifType.Create` |
| PPA valid for komponen only if SatTugas sets intersect | `KomponenType.IsValidPpa` |
| PPA-assigned tindakan line requires valid komponen–PPA pairing | `TindakanKomponenWithPpaType.Create` |
| Billing from tindakan: tarif on tindakan must match loaded `TarifType` | `TrsBillingType.CreateFromTindakan` |
| Billing from tindakan: jaminan id must match reg tipe jaminan prefix | same |

### 7.2 Implemented in application (not domain)

| Rule | Where |
|------|-------|
| Reg must be active for tindakan create | `TdkCreateTindakanCmd.LoadReg` |
| NilaiTarif / Tarif / Komponen / PPA must exist | Various `Load*` helpers |
| Stok search requires keyword length ≥ 3 | `StokRepo.ListData` |

### 7.3 NOT implemented (gaps)

| Expected rule (conceptual) | Code state |
|----------------------------|------------|
| Header nilai = sum(komponen nilai) | **Not enforced** |
| Unique (TarifId, TipeTarifId, KelasId) | **Not enforced** in DB or domain |
| TarifPolicy / effective date / variant selection | **Missing** (out of scope; not in code) |
| Komponen SatTugas persisted on save | **Incomplete** in `KomponenRepo.SaveChanges` |

---

## 8. Detected Technical Constraints

1. **Dual schema era:** Legacy `ta_*` masters + `ta_trs_tarif*` transactions coexist with `BILRG_NilaiTarif*`.
2. **Import is destructive:** `Import()` clears both BILRG tables before reload.
3. **IDs:** `NilaiTarifId` is `VARCHAR(26)` (ULID); legacy keys are shorter (`VARCHAR(2–10)`).
4. **Read-only Tarif master** in new API surface — mutations would require new use cases or uncommenting legacy controllers.
5. **Full-text search** on `ta_tarif.fs_nm_tarif` for `TarifDal.ListData(string keyword)` with `FB_AKTIF = 1`.
6. **Cross-context compile-time coupling:** Charge domain depends on Payment (`CoaType`) and Admisi (`SatTugasType`).
7. **SqlBulkCopy** requires DTO shapes to match table columns exactly (NilaiTarif import, komponen children).
8. **No Tarif artifact docs yet** under `docs/tarif/` except this retrieval report.

---

## 9. Gap Analysis

| Area | Intended model (scope list) | Codebase state |
|------|----------------------------|----------------|
| TarifPolicy | Not yet implemented | **Missing** — no types, tables, or handlers |
| TarifVariant | Not yet implemented | **Missing** — effective dating only visible in legacy `ta_trs_tarif2.fd_tgl_expired` import filter |
| BillingTransaction | Agnostic boundary | **No Tarif coupling to a “BillingTransaction” aggregate**; `TrsBillingType` is separate Payment aggregate fed at call sites |
| NilaiTarif CRUD API | — | **Partial** — import + read queries; `SaveChanges` unused |
| Tarif master API | — | **Partial** — DAL/repo read; search endpoint commented; write controllers commented |
| Komponen / TipeTarif / GroupKomponen admin API | — | **Partial** — persistence layer + tests; API commented |
| SatTugas admin | — | **Admisi context**; `SatTugasController` commented |
| Kelas admin | — | **Ward context** (`KelasRepo`, `KelasDal`) |
| Aggregate consistency NilaiTarif | — | **Partial** — repo replaces komponen children on save, but save path unused |
| Single source of truth | — | **Hybrid** (legacy import → BILRG) |
| Domain services | — | **Missing** for tarif selection/variant resolution |
| Authorization per endpoint | — | **Not specified** on Tarif controllers |

### NilaiTarif behavior classification (for artifacts)

| Label | Description |
|-------|-------------|
| **Implemented** | BILRG tables, repo, import, composite/id load, search by layanan+tipe+kelas |
| **Partially implemented** | SaveChanges, master CRUD APIs, SatTugas write on Komponen save |
| **Missing** | TarifPolicy, TarifVariant, non-import nilai maintenance API |
| **Inferred (do not document as built)** | Future billing workflow, policy resolution rules |

---

## 10. Recommendations For Artifact Authoring

When producing `tarif-01-context.md` … `tarif-05-runbook.md` per feature-knowledge-steward:

1. **Split “catalog” vs “nilai” clearly** in domain doc: `TarifType` (master) vs `NilaiTarifType` (priced instance by TipeTarif + Kelas + komponen breakdown).

2. **Document the hybrid persistence story** in design (not domain): legacy `ta_trs_tarif*` → `Import()` → `BILRG_*` as the **current** operational path; avoid implying real-time legacy sync.

3. **Place Kelas and SatTugas** in domain doc as **external masters** with references (`KelasReff`, `SatTugasType`), pointing to Ward and Ppa contexts for ownership.

4. **Komponen dual role** should be explicit in domain: COA fields (accounting) + SatTugas mapping (PPA/jasa eligibility), with billing consumption cited only as integration boundary in design (`TrsBillingType.CreateFromTindakan`).

5. **Do not invent TarifPolicy/TarifVariant** in artifacts until implemented; mark as planned with zero code references.

6. **API contract artifact** should list only the **3 live endpoints** plus `TarifController.GetNilai`; note commented legacy BillContext controllers as **deprecated path**.

7. **Runbook** should include import operation (`POST /api/NilaiTarif/import`) warnings: full table clear, dependency on legacy expired-date filter `'3000-01-01'`.

8. **Use code citation filenames** consistently: `NilaiTarifType.cs`, `NilaiTarifRepo.cs`, `KomponenType.cs`, `NilaiTarifDal.cs`.

9. **Register open gaps** for engineering backlog: unused `SaveChanges`, missing unique index on nilai variant, `KomponenRepo` not persisting SatTugas, `KelasDal.Update` SQL defect.

10. **Tindakan integration** belongs in design/integration section: how `NilaiTarifId` vs composite key is chosen (id in Tindakan commands vs composite in Reg karcis flows).

---

## Appendix A — Key file index

### Domain (`Bilreg.Domain/ChargeContext/TarifFeature/`)

- `TarifType.cs`, `NilaiTarifType.cs`, `KomponenType.cs`, `GrupKomponenType.cs` (`GroupKomponenType`)
- `TipeTarifModel.cs` (`TipeTarifType`), `JenisTarifType.cs`, `GroupTarifType.cs`, `GroupTarifDkType.cs`, `TipeRekType.cs`

### Application

- `TarifFeature/INilaiTarifRepo.cs`, `ITarifRepo.cs`, `IStokRepo.cs`
- `TarifFeature/UseCases/Trf*.cs`
- `TindakanFeature/IKomponenRepo.cs`, `ITipeTarifRepo.cs`, `IGroupKomponenRepo.cs`, …

### Infrastructure (`Bilreg.Infrastructure/ChargeContext/TarifFeature/`)

- `*Dal.cs`, `*Dto.cs`, `NilaiTarifRepo.cs`, `KomponenRepo.cs`, `TarifRepo.cs` (namespace `BillContext.TindakanSub.TarifAgg`), `TipeTarifRepo.cs`, `GroupKomponenRepo.cs`

### API

- `Controllers/ChargeContext/NilaiTarifController.cs`
- `Controllers/BillContext/TindakanSub/TarifController.cs`

### SQL

- `Bilreg.SqlDb/ChargeContext/TarifFeature/*.sql`

---

*End of retrieval report. No source code was modified. No feature artifacts (`tarif-01` … `tarif-05`) were created.*
