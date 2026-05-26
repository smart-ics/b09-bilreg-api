# TARIF_PHASE2_REPORT.md — Policy persistence & projection writer

**Date:** 2026-05-26  
**Scope:** Phase 2 only — schema, DTO/DAL/Repo, projection upsert helper, tests.  
**Out of scope:** publish orchestration, policy workflow/API, billing/tindakan changes, `INilaiTarifRepo` contract changes.

---

## Implemented persistence structures

| SQL table | Purpose |
| --------- | ------- |
| `BILRG_TarifPolicy` | Policy header (`PolicyNo`, `PolicyName`, `EffectiveDateInfo`, `PolicyStatus`, audit) |
| `BILRG_TarifVariant` | Variant lines PK `(TarifPolicyId, ItemNo)` + `PublishedNilaiTarifId` placeholder |
| `BILRG_TarifVariantKomponen` | Komponen lines PK `(TarifPolicyId, ItemNo, NoUrut)` |
| `BILRG_TarifPublishLog` | Publish audit header |
| `BILRG_TarifPublishLogDetail` | Per-variant publish snapshot (optional detail) |

Scripts (idempotent `IF OBJECT_ID` guards): `Bilreg.SqlDb/ChargeContext/TarifFeature/BILRG_Tarif*.sql`

**Domain (structural, for persistence mapping):** `TarifPolicyType`, `TarifVariantType`, `TarifVariantKomponenType`, `TarifPublishLogType`, `TarifPublishLogDetailType`, `TarifPolicyStatus`.

**Application contracts:** `ITarifPolicyRepo`, `ITarifPublishLogRepo`, `INilaiTarifProjectionWriter`.

**Infrastructure:** matching `*Dto`, `*Dal`, `TarifPolicyRepo`, `TarifPublishLogRepo`, `NilaiTarifProjectionWriter`.

---

## Migration notes (deploy order)

1. Deploy Phase-2 scripts on each environment (safe to re-run):
   - `BILRG_TarifPolicy.sql`
   - `BILRG_TarifVariant.sql`
   - `BILRG_TarifVariantKomponen.sql`
   - `BILRG_TarifPublishLog.sql`
   - `BILRG_TarifPublishLogDetail.sql`
2. No change required to existing `BILRG_NilaiTarif*` for Phase 2 (Phase 0 `SourcePolicyId` already present).
3. Integration tests skip gracefully when tables are missing (`Invalid object name`).

**DI:** `ITarifPublishLogRepo` and `INilaiTarifProjectionWriter` registered explicitly in `InfrastructureService`; policy repo/DALs picked up by Scrutor scan.

---

## Projection upsert behavior (Phase 3 dependency)

`INilaiTarifProjectionWriter.Upsert(projection, sourcePolicyId)`:

1. Resolve composite `(TarifId, TipeTarifId, KelasId)` via `INilaiTarifRepo.LoadEntity(composite)`.
2. **Existing row:** reuse `NilaiTarifId`, `UPDATE` header (including `SourcePolicyId`), delete + bulk insert komponen children.
3. **No row:** new ULID `NilaiTarifId`, `INSERT` header + komponen children.

`NilaiTarifRepo.Import()` and consumer load paths are **unchanged**.

---

## Tests

`dotnet test --filter FullyQualifiedName~TarifFeature` — 112 passed (includes Phase-2 unit tests + existing Tarif tests).

| Test area | Coverage |
| --------- | -------- |
| `TarifPolicyRepoTest` | Save insert/update, load reconstruction, child replace |
| `NilaiTarifProjectionWriterTest` | Preserve id vs new ULID |
| `TarifPublishLogRepoTest` | Insert + load with details |
| `TarifPolicyDalIntegrationTest` | DB round-trip when tables exist |

---

## Risks

| Risk | Mitigation |
| ---- | ---------- |
| Phase-2 tables not deployed | Integration tests no-op on missing tables; runbook deploy list above |
| Policy save replaces all variant lines | By design; callers must send full variant set |
| Concurrent publish (Phase 3) | Last-wins per variant; requires transactional publish handler |
| Domain workflow not in Phase 2 | Phase 3+ must enforce draft/published rules in handlers |

---

## Remaining gaps before Phase 3

- `TrfPublishTarifPolicyHandler` + `TransHelper` transaction (log → projection upsert → policy status).
- Publish validator (komponen sum, duplicate variant, master refs).
- Update `PublishedNilaiTarifId` on `BILRG_TarifVariant` after successful upsert.
- Policy CRUD/review HTTP (Phase 4).
- ~~Full `TarifPolicyType` domain behaviours~~ — completed in Phase 1 retroactive alignment (`TARIF_PHASE1_ALIGNMENT_REPORT.md`).

---

## Verification

- `dotnet build Bilreg.Test/Bilreg.Test.csproj` — success.
- No changes to `INilaiTarifRepo` public methods or consumer handlers.
- Operational projection remains authoritative for runtime lookup.
