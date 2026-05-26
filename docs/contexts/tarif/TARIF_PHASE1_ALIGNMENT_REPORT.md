# TARIF_PHASE1_ALIGNMENT_REPORT.md — Retroactive domain alignment

**Date:** 2026-05-26  
**Scope:** Phase 1 domain finalization after Phase 2 persistence (no SQL/repo/projection/API changes).  
**Verification:** `dotnet test --filter FullyQualifiedName~TarifFeature` — 126 passed.

---

## 1. Already correct (unchanged)

| Area | Notes |
| ---- | ----- |
| Aggregate boundaries | `TarifPolicy` owns variants/komponen; `NilaiTarifType` separate projection aggregate |
| PublishLog | Audit-only; append-only repo |
| Persistence layering | Thin DALs; `TarifPolicyRepo` child replace; no publish workflow in repos |
| Projection writer | `NilaiTarifProjectionWriter.Upsert` preserves `NilaiTarifId` by composite; sets `SourcePolicyId` |
| Publish semantics in design | Manual publish; no effective-date engine; no policy lineage |
| Consumer contracts | `INilaiTarifRepo` unchanged |
| Schema | `TarifPolicyId VARCHAR(12)`; variant PK `(TarifPolicyId, ItemNo)` |

---

## 2. Was misaligned

| Gap | Impact |
| --- | ------ |
| Anemic `TarifPolicyType` / `TarifVariantType` | Persistence DTOs dictated shape; no lifecycle or invariants |
| No domain tests for policy | Phase 1 gate missing |
| Artifact drift | Docs marked policy as wholly **Planned** while Phase 2 tables/repos were **LIVE** |
| `PublishedNilaiTarifId` | Risk of treating as domain orchestration vs post-publish snapshot |

---

## 3. Corrected

### Code (`Bilreg.Domain/ChargeContext/TarifFeature/`)

**`TarifPolicyType`:** `Create`, `AddVariant`, `CopyFrom`, `MassAdjust`, `MarkReviewed`, `ValidateForPublish`, `MarkPublished`, `EnsureEditable`.

**`TarifVariantType`:** `Create`, `SetKomponenLines`, `VariantCompositeKey`, `ToPublishedSnapshot`, `WithMassAdjustedNilai`, komponen sum/uniqueness validation (±0.01 tolerance).

**Tests:** `Bilreg.Test/ChargeContext/TarifFeature/TarifPolicyTypeTest.cs` (14 scenarios).

### Artifacts

- `tarif-01-context.md` — policy persistence **Implemented**; publish orchestration **Planned**
- `tarif-02-domain.md` — policy invariants in **Implemented** table; behaviour documented
- `tarif-03-design.md` — removed stale “TarifPolicy absent” gap
- `TARIF_IMPLEMENTATION_PLAN.md` — Phase 1 **LIVE** (retroactive); enum name `TarifPolicyStatus`
- `ARTIFACTS.md` — index entry for this report

### Explicitly not changed

- SQL scripts, DALs, repos, `NilaiTarifProjectionWriter`, `INilaiTarifRepo`, import path, billing/tindakan

---

## 4. Remaining risks before Phase 3

| Risk | Mitigation |
| ---- | ---------- |
| `TarifPolicyRepo.SaveChanges` accepts any status | Handlers must call `EnsureEditable()` before draft saves |
| Full-graph child replace | Callers must send complete variant set on save |
| Master-ref validation absent | Phase 3 publish validator (`Tarif`, `Kelas`, `Komponen`) |
| `PublishedNilaiTarifId` not written by projection writer | Phase 3 handler updates variant rows after upsert |
| Import vs publish concurrency | Ops/runbook lock until Phase 5 mutex |
| `NilaiTarifType` projection invariants | Enforced at publish mapping, not on projection aggregate |

---

## 5. Recommended next steps

1. **Phase 3.1** — `TrfPublishTarifPolicyHandler` + `TransHelper.NewScope`: log → per-variant `INilaiTarifProjectionWriter.Upsert` → `MarkPublished` → `SaveChanges` with `PublishedNilaiTarifId` on variants.
2. **Phase 3.2** — Application publish validator (master refs, overlap policy, structured errors).
3. **Phase 4** — Policy/variant HTTP; handlers call domain factories before repos.
4. Optional — `Archive` domain transition; component-scoped mass adjust.

---

## Document control

| Field | Value |
| ----- | ----- |
| Steward | Tarif bounded context |
| Related | `TARIF_PHASE2_REPORT.md`, `tarif-02-domain.md` |
