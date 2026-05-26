# Tarif Subsystem — Phase-2 Architecture Audit Report

**Date:** 2026-05-26  
**Scope:** Post–Phase-1-alignment verification of **current** Phase-2 implementation (AS-IS).  
**Not in scope:** feature implementation, persistence redesign, publish engine delivery.

**Primary references audited against:**

- `docs/contexts/tarif/TARIF_IMPLEMENTATION_PLAN.md`
- `docs/contexts/tarif/tarif-01-context.md` … `tarif-05-runbook.md`
- `docs/ARTIFACTS.md`, `docs/ENGINEERING.md`, `docs/DATABASE.md`, `docs/NAMING.md`
- `docs/skills/feature-model-generation.md`, `docs/skills/feature-persistence-generation.md`

**Verification run:** `dotnet test Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~TarifFeature` — **126 passed**, 0 failed.

---

# Executive Summary

## Overall Phase-2 health

Phase-2 persistence **matches the intended architecture** at a structural level: policy aggregate tables/repos are separate from operational `BILRG_NilaiTarif*`, publish log is append-only persistence, and projection upsert is isolated in `INilaiTarifProjectionWriter`. Retroactive Phase-1 domain behaviours are present and tested. **No Phase-3 orchestration** (publish handler, workflow, API) exists in code — scope boundary is respected.

## Readiness assessment

The codebase is a **sound foundation for Phase-3** if publish work is introduced with explicit application-layer orchestration (`TransHelper.NewScope`), domain guards before saves, and a variant→projection mapper. Phase-2 itself does not block Phase-3, but several **guardrail gaps** must be addressed in Phase-3 handlers (not optional “nice to have”).

## Major risks

| Risk | Severity | Why it matters |
| ---- | -------- | -------------- |
| `TarifPolicyRepo.SaveChanges` does not enforce `EnsureEditable()` or validate variant uniqueness | **High** (Phase-3) | Any handler can persist edits to `Published` policies or duplicate composite keys by bypassing `AddVariant` |
| Policy save / projection upsert are **not transactional** today | **High** (Phase-3 publish) | Partial writes possible without handler-owned scope |
| No application mapper `TarifVariantType` → `NilaiTarifType` | **Medium** (Phase-3) | Publish handler may duplicate mapping logic or leak DTO concerns |
| `PublishedNilaiTarifId` never written by projection writer | **Medium** (Phase-3) | Historical variant↔projection link incomplete until handler updates policy rows |
| `tarif-01-context.md` terminology table still labels policy concepts **Planned** | **Low** (docs) | Integrators may misread LIVE vs PLANNED |

## Recommended action

Proceed to Phase-3 publish engine **after** baking in: (1) handler-level `EnsureEditable` / `ValidateForPublish` / `MarkPublished` sequence, (2) single transaction wrapping log + N×`Upsert` + policy save, (3) dedicated mapper + `ToPublishedSnapshot` updates. **Do not** expand `TarifPolicyRepo` into a smart orchestration repository.

---

# Detailed Findings

## 1. Aggregate boundary audit

### Findings

- **TarifPolicy → TarifVariant → TarifVariantKomponen** is reflected in persistence: `TarifPolicyRepo` owns header + child replace on `BILRG_TarifVariant` / `BILRG_TarifVariantKomponen` only.
- **NilaiTarif** (`NilaiTarifType`, `BILRG_NilaiTarif*`) is **not** loaded or mutated inside `TarifPolicyRepo`.
- **PublishLog** is a separate persistence slice: `TarifPublishLogRepo` (insert/load/list); no coupling to policy save.
- **Projection refresh** is isolated in `NilaiTarifProjectionWriter`; no policy workflow inside that type.
- `TarifVariant` is a **child** of the policy aggregate (composite PK under `TarifPolicyId`), not a separate aggregate root — consistent with `tarif-02-domain.md` and `TARIF_IMPLEMENTATION_PLAN.md` §3.1.

### Evidence

```23:43:Bilreg.Infrastructure/ChargeContext/TarifFeature/TarifPolicyRepo.cs
    public void SaveChanges(TarifPolicyType model)
    {
        LoadEntity(model)
            .Match(
                onSome: _ => _tarifPolicyDal.Update(TarifPolicyDto.FromModel(model)),
                onNone: () => _tarifPolicyDal.Insert(TarifPolicyDto.FromModel(model)));

        _tarifVariantKomponenDal.Delete(model);
        _tarifVariantDal.Delete(model);
        // ... bulk re-insert variants + komponen
    }
```

```24:53:Bilreg.Infrastructure/ChargeContext/TarifFeature/NilaiTarifProjectionWriter.cs
    public string Upsert(NilaiTarifType projection, string sourcePolicyId)
    {
        // composite resolve → preserve NilaiTarifId → upsert BILRG_NilaiTarif* only
    }
```

```20:29:Bilreg.Infrastructure/ChargeContext/TarifFeature/TarifPublishLogRepo.cs
    public void Insert(TarifPublishLogType log)
    {
        _tarifPublishLogDal.Insert(TarifPublishLogDto.FromModel(log));
        // detail bulk insert
    }
```

SQL tables: `Bilreg.SqlDb/ChargeContext/TarifFeature/BILRG_TarifPolicy.sql`, `BILRG_TarifVariant.sql`, `BILRG_TarifVariantKomponen.sql`, `BILRG_TarifPublishLog.sql` — distinct from `BILRG_NilaiTarif*`.

### Classification

**OK**

### Risk level

**Low** (structural boundaries)

### Recommendation

Keep aggregate split in Phase-3; implement publish orchestration in Application handler, not in `TarifPolicyRepo`.

---

## 2. Domain authority audit

### Findings

- **Domain owns behaviour:** `TarifPolicyType` / `TarifVariantType` implement lifecycle, duplicate-variant check on `AddVariant`, komponen sum, publish validation (`ValidateForPublish`, `MarkPublished`).
- **Repositories are persistence-oriented:** DTO mapping, insert/update, child delete+bulk insert — no publish workflow in repos.
- **Application orchestration for import only:** `TrfImportNilaiTarifHandler` owns transaction for legacy import path.
- **Gaps:** Persistence does **not** re-invoke domain rules. `TarifPolicyRepo.SaveChanges` accepts any `TarifPolicyType` graph, including `Published` status or duplicate variants if built without `AddVariant`. `TarifPublishLogType` is structural (no factory/validation) — acceptable for audit log if handler validates.
- **Projection writer** uses `INilaiTarifRepo` for composite load but **DAL directly** for insert/update — bypasses `NilaiTarifRepo.SaveChanges` (intentional for `SourcePolicyId`, but mixed abstraction).

### Evidence

```80:85:Bilreg.Domain/ChargeContext/TarifFeature/TarifPolicyType.cs
    public void EnsureEditable()
    {
        if (PolicyStatus is TarifPolicyStatus.Published or TarifPolicyStatus.Archived)
            throw new InvalidOperationException(/* ... */);
    }
```

```23:43:Bilreg.Infrastructure/ChargeContext/TarifFeature/TarifPolicyRepo.cs
    // No call to EnsureEditable() or ValidateForPublish()
```

`grep` across `Bilreg.Application`: **no** `TrfPublish*` / `TarifPolicy` handlers — only `ITarifPolicyRepo` interface definition.

### Classification

**PARTIAL**

### Risk level

**Medium** — authority is in domain, but **not enforced at persistence boundary**

### Recommendation

Phase-3 handlers **must** call domain methods before `SaveChanges`; optionally add a thin `ITarifPolicyPersistence` guard in Application (not repo) if repeated. Do not move publish validation into DAL.

---

## 3. Phase-2 scope compliance audit

### Findings

**Present (in scope):**

| Deliverable | Evidence |
| ----------- | -------- |
| Schema `BILRG_Tarif*` | `Bilreg.SqlDb/ChargeContext/TarifFeature/BILRG_Tarif*.sql` |
| DTO/DAL/Repo policy aggregate | `TarifPolicyDto`, `TarifPolicyDal`, `TarifPolicyRepo`, `ITarifPolicyRepo` |
| Publish log persistence | `TarifPublishLogRepo`, `ITarifPublishLogRepo` |
| Projection upsert helper | `INilaiTarifProjectionWriter`, `NilaiTarifProjectionWriter` |
| Tests | `TarifPolicyRepoTest`, `NilaiTarifProjectionWriterTest`, `TarifPublishLogRepoTest`, `TarifPolicyDalIntegrationTest` |

**Absent (correctly out of scope):**

- No `TrfPublishTarifPolicyHandler` or `ITarifPublishService`
- No policy HTTP controllers (Api `grep`: only DI registration for log + projection writer)
- No effective-date activation, approval engine, billing side effects
- No smart orchestration repo combining log + projection + policy status

**Consumer contract frozen:** `INilaiTarifRepo` unchanged; import still `TrfImportNilaiTarifCmd` + `TransHelper.NewScope()`.

### Classification

**OK**

### Risk level

**Low**

### Recommendation

Maintain scope discipline in Phase-3 PRs (handler + validator only; no consumer signature changes).

---

## 4. Projection separation audit

### Findings

- `BILRG_NilaiTarif*` remains operational projection: populated by **import** (full replace) and **optional** per-row `Upsert` (publish path).
- Policy historical data lives in `BILRG_TarifVariant*` — not mixed into projection tables.
- **Stable `NilaiTarifId`:** `NilaiTarifProjectionWriter` resolves composite `(TarifId, TipeTarifId, KelasId)` via `INilaiTarifRepo.LoadEntity`, reuses id when found, new ULID otherwise.
- **`SourcePolicyId`** set on upsert via `NilaiTarifDto.FromModel(projection, sourcePolicyId)` — import leaves empty (Phase 0).
- Projection upsert does **not** touch policy tables or publish log.
- **Gap:** No coupling from upsert back to `PublishedNilaiTarifId` on `BILRG_TarifVariant` (documented Phase-3 handler duty in `TARIF_PHASE2_REPORT.md`).

### Evidence

```24:52:Bilreg.Infrastructure/ChargeContext/TarifFeature/NilaiTarifProjectionWriter.cs
        var nilaiTarifId = _nilaiTarifRepo.LoadEntity(compositeKey)
            .Match(onSome: existing => existing.NilaiTarifId, onNone: () => Ulid.NewUlid().ToString());
        var dto = NilaiTarifDto.FromModel(projection, sourcePolicyId) with { NilaiTarifId = nilaiTarifId };
        // delete komponen children + bulk insert
```

`NilaiTarifRepo.Import()` still clears and bulk-reloads entire projection (global replace) — separate path, not invoked from policy repo.

### Classification

**OK**

### Risk level

**Low** for Phase-2; **Medium** for Phase-3 until `PublishedNilaiTarifId` write path exists

### Recommendation

Phase-3 publish handler: after each `Upsert`, call `variant.ToPublishedSnapshot(nilaiTarifId)` and persist via `TarifPolicyRepo.SaveChanges` (or targeted variant update if added later).

---

## 5. Repository pattern audit

### Findings

| Convention | Tarif policy stack | ChargeContext reference |
| ---------- | ------------------ | ----------------------- |
| Dapper + explicit SQL | `TarifPolicyDal`, etc. | `KomponenDal`, `NilaiTarifDal` |
| DTO `FromModel` / `ToModel` | `TarifPolicyDto`, `TarifVariantDto` | `KomponenDto` |
| Child replace on save | Delete all children → `SqlBulkCopy` insert | `KomponenRepo`, `NilaiTarifRepo.SaveChanges` |
| `MayBe` load | `TarifPolicyRepo.LoadEntity` | `NilaiTarifRepo` |
| Scrutor scan for DAL | Policy DALs registered via scan | Same pattern |
| Explicit DI | `ITarifPublishLogRepo`, `INilaiTarifProjectionWriter` in `InfrastructureService` | — |

**Deviations / notes:**

- `TarifPolicyRepo.SaveChanges` uses **multiple connections** (header Dapper + two BCP passes) with **no** `TransHelper` — acceptable for Phase-2 draft saves if handlers add scope; **required** for publish.
- `TarifPolicyRepo.ListData` performs **N+1** variant count queries — performance only, not boundary violation.
- `NilaiTarifProjectionWriter` splits repo (read) vs DAL (write) — differs from `NilaiTarifRepo.SaveChanges` but aligned with Phase-2 report intent.

### Classification

**OK**

### Risk level

**Low** (pattern); **Medium** (missing transaction on multi-step saves)

### Recommendation

Wrap Phase-3 publish and optionally policy draft saves in `TransHelper.NewScope()` at handler level; do not embed transactions inside projection writer.

---

## 6. Domain invariant audit

| Invariant | Expected layer | Status | Evidence |
| --------- | -------------- | ------ | -------- |
| Unique variant per policy `(Tarif, Kelas, TipeTarif)` | Domain | **Enforced** on `AddVariant` only | `TarifPolicyType.AddVariant` + `TarifPolicyTypeTest.DT2` |
| Cannot edit published/archived policy | Domain | **Enforced** on mutating methods | `EnsureEditable()` |
| Copy → independent draft | Domain | **Enforced** | `CopyFrom` + `DT` tests |
| ≥1 komponen; Σ komponen ≈ header | Domain | **Enforced** on variant ctor / publish | `TarifVariantType.ValidateKomponenInvariants` |
| Cannot publish empty / wrong status | Domain | **Enforced** | `ValidateForPublish`, `MarkPublished` |
| Unique variant on **reconstituted** graph | Domain | **Missing** | Constructor accepts arbitrary `IEnumerable<TarifVariantType>` without duplicate scan |
| Cannot save published policy via repo | Persistence/Handler | **Missing** | `TarifPolicyRepo.SaveChanges` |
| Master ref existence at publish | Application | **Planned** Phase-3 | Not in codebase |
| Projection header = Σ komponen | Domain on `NilaiTarifType` | **Not enforced** | By design per `tarif-02-domain.md` |
| Unique projection variant | DB | **LIVE** Phase-0 | `UX_BILRG_NilaiTarif_Variant` |

### Classification

**PARTIAL**

### Risk level

**Medium**

### Recommendation

- Phase-3 publish validator: master refs + optional cross-policy overlap rules.
- Phase-4 draft save handlers: always mutate via `AddVariant` / domain methods; consider `TarifPolicyType` factory validating full graph on load if DB corruption is a concern.
- Do not duplicate komponen-sum logic in DAL.

---

## 7. Phase-3 readiness audit

### Ready

- Separate repos: policy, publish log, projection writer.
- Domain publish validation (`ValidateForPublish`, `MarkPublished`, `ToPublishedSnapshot`).
- Projection upsert reusable inside a transaction.
- Import path transactional (reference pattern for publish).
- Tests cover repo reconstruction, projection id preservation, publish log insert.

### Gaps (expected Phase-3 work, not Phase-2 defects)

| Gap | Impact |
| --- | ------ |
| No `TrfPublishTarifPolicyHandler` | No end-to-end publish |
| No variant→`NilaiTarifType` mapper in Application | Mapping duplication risk |
| `PublishedNilaiTarifId` not persisted after upsert | Weak audit trail variant↔projection |
| No idempotent re-publish policy | Spec in plan; implement in handler |
| Import vs publish concurrency | Ops/runbook; no app mutex |
| `TarifPolicyRepo` multi-step save without transaction | Publish partial-failure risk |

### Architectural debt / coupling risks

- If Phase-3 puts orchestration in `TarifPolicyRepo` or `NilaiTarifRepo`, boundaries erode — **forbidden** per plan and `ENGINEERING.md` explicit orchestration stance.
- If handlers skip `EnsureEditable`, published policies can be overwritten in DB while domain types would have thrown.

### Classification

**PARTIAL** (foundation ready; guardrails pending)

### Risk level

**Medium**

### Recommendation

Follow `TARIF_IMPLEMENTATION_PLAN.md` §5.2 algorithm in a single Application handler; inject `ITarifPolicyRepo`, `ITarifPublishLogRepo`, `INilaiTarifProjectionWriter` — do not create `TarifPublishRepo` that merges concerns unless it is a thin façade over existing three.

---

## 8. Artifact alignment audit

### Aligned

- `TARIF_IMPLEMENTATION_PLAN.md` — Phase 2 **LIVE**, Phase 3 **PLANNED**; dependency graph matches code order.
- `tarif-02-domain.md` — ownership table, invariants, `TarifVariant` naming (no `TarifVersion` in code).
- `tarif-03-design.md` — policy persistence + projection writer documented as implemented.
- `TARIF_PHASE1_ALIGNMENT_REPORT.md`, `TARIF_PHASE2_REPORT.md` — accurate vs code.
- `ARTIFACTS.md` — index reflects Phase 1/2 reports.

### Drift (targeted doc updates recommended; do not rewrite now)

| Artifact | Issue |
| -------- | ----- |
| `tarif-01-context.md` | Status table says policy **Implemented**, but §Core terminology still marks `TarifPolicy`, `TarifVariant`, `PublishLog`, `Publish` as **Planned** |
| `tarif-03-design.md` | Mermaid subgraph label `Planned historical source` — tables are **LIVE** (wording only) |
| `TARIF_IMPLEMENTATION_PLAN.md` header | Still says “Planning only — no production code” at line 3 while body marks Phase 1–2 **LIVE** |

### Terminology / LIVE vs PLANNED

| Term | Code | Artifacts |
| ---- | ---- | --------- |
| `TarifVariant` (not `TarifVersion`) | Consistent | Consistent |
| Manual publish | Not implemented | Consistent (**Planned**) |
| `EffectiveDateInfo` | Column + domain field; no scheduler | Consistent |
| Publish orchestration | Absent | Consistent |

### Classification

**PARTIAL**

### Risk level

**Low**

### Recommendation

Small doc PR: fix `tarif-01-context.md` terminology table rows to **Implemented** / **Planned** per concept; clarify `TARIF_IMPLEMENTATION_PLAN.md` document header vs phase legend.

---

# Architectural Violations

No **structural** violations (collapsed aggregates, projection inside policy repo, publish workflow in DAL, billing side effects).

**Boundary soft violations** (fix in Phase-3, not retroactive Phase-2 rework):

1. **Persistence can bypass domain edit guards** — `TarifPolicyRepo.SaveChanges` has no status gate.
2. **Duplicate variant invariant bypass** — save path does not require `AddVariant` discipline.

These are **handler-contract** issues, not wrong table design.

---

# Refactor Recommendations Before Phase-3

## Critical

1. **Phase-3.1 publish handler** with `TransHelper.NewScope()` spanning: `TarifPublishLogRepo.Insert` → foreach variant `INilaiTarifProjectionWriter.Upsert` → domain `MarkPublished` → `TarifPolicyRepo.SaveChanges` with `PublishedNilaiTarifId` populated via `ToPublishedSnapshot`.
2. **Never save draft** without `policy.EnsureEditable()` (or equivalent) in every mutating handler before `SaveChanges`.
3. **Application-layer publish validator** (master refs, structured errors per `tarif-04-api-contract.md`) — do not push into repos.

## Important

4. Add **`ITarifPublishMapper`** (or static mapper in Application) `TarifVariantType` → `NilaiTarifType` (resolve display names via existing repos if needed).
5. Consider **transactional policy draft save** when variant count is large (same `TransHelper` pattern as import).
6. Document **idempotent re-publish** behaviour in Phase-3 slice artifact (`tarif-06-publish-engine.md` proposed in plan).

## Optional

7. `TarifPolicyType` constructor validation for duplicate composite keys when rehydrating from DB.
8. Reduce `ListData` N+1 variant counts (single SQL or denormalized count) when policy list API ships (Phase 4).
9. Artifact terminology cleanup (`tarif-01-context.md`, plan header).

---

# Final Verdict

## **READY WITH MINOR REFACTOR**

Phase-2 implementation **conforms** to intended aggregate separation, projection isolation, and scope boundaries. Domain authority is restored after Phase-1 alignment. Remaining gaps are **known, documented, and appropriate to Phase-3 handlers** — not evidence of wrong Phase-2 architecture — but Phase-3 must not proceed without explicit transactional orchestration and domain guard calls at the application layer.

---

## Document control

| Field | Value |
| ----- | ----- |
| Version | 1.0 |
| Auditor | Phase-2 audit (implementation-aligned) |
| Related | `TARIF_PHASE2_REPORT.md`, `TARIF_PHASE1_ALIGNMENT_REPORT.md`, `TARIF_IMPLEMENTATION_PLAN.md` |
