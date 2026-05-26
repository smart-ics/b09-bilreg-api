# Tarif Phase 0 — Implementation Report

**Status:** LIVE (operational projection hardening)  
**Date:** 2026-05-26  
**Scope:** `BILRG_*` import safety, duplicate variant constraint, Komponen SatTugas persistence, `SourcePolicyId` traceability column.

**Out of scope:** TarifPolicy, publish API, Billing, tindakan snapshot, `INilaiTarifRepo` signature changes (`void Import()` retained).

---

## What changed

| Slice | Code / SQL | Outcome |
| ----- | ---------- | ------- |
| 0.1 Transactional import | `TrfImportNilaiTarifHandler`, `NilaiTarifRepo` (`BuildImportPayload` + `ImportWrite`) | Failed import rolls back `BILRG_*` writes |
| 0.2 Duplicate + UX index | Phase0 SQL scripts, `LoadEntity(composite)` | Deterministic winner = max `NilaiTarifId` |
| 0.3 Komponen SatTugas | `KomponenRepo.SaveChanges` / `DeleteEntity` | Child replace on `ta_detil_tarif2` |
| 0.4 SourcePolicyId | Alter script, `NilaiTarifDto` / `NilaiTarifDal` | Empty on import; reserved for future publish |

---

## Migration order (existing DB)

1. `BILRG_NilaiTarif_Phase0_DuplicateReport.sql` (read-only; archive)
2. `BILRG_NilaiTarif_Phase0_DuplicateCleanup.sql`
3. `BILRG_NilaiTarif_Phase0_SourcePolicyId_Alter.sql`
4. `BILRG_NilaiTarif_Phase0_UX_Variant_Index.sql`
5. Deploy application build
6. Smoke: `POST /api/NilaiTarif/import`

Greenfield: use updated `BILRG_NilaiTarif.sql` (includes column + unique index).

---

## Rollback

| Layer | Action |
| ----- | ------ |
| Application | Deploy previous build; txn wrapper is backward-compatible |
| Index | `DROP INDEX UX_BILRG_NilaiTarif_Variant ON BILRG_NilaiTarif` |
| Column | Drop `SourcePolicyId` if needed |
| Data | Restore `BILRG_*` backup or re-run import after cleanup |

---

## Risks / known gaps

- Import still assigns **new ULIDs** each run — stable ids deferred to publish (Phase 3).
- No import/publish mutex — concurrent import remains operator responsibility.
- `SourcePolicyId` requires alter on brownfield DB before bulk import with new DTO column.
- Composite load and cleanup both use **MAX(`NilaiTarifId`)** — document for ops sign-off.

---

## Tests

- `NilaiTarifRepoTest` — composite load picks max id; transaction rollback keeps seeded row
- `KomponenRepoTests` UT9/UT10 — SatTugas delete+insert on save
- Run: `dotnet test --filter "FullyQualifiedName~TarifFeature"`

---

## Related artifacts

- `docs/contexts/tarif/tarif-03-design.md`
- `docs/contexts/tarif/tarif-05-runbook.md`
- `docs/contexts/tarif/TARIF_IMPLEMENTATION_PLAN.md` (Phase 0 → LIVE)
