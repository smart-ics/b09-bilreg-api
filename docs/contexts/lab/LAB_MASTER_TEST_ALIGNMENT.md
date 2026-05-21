# LAB_MASTER_TEST_ALIGNMENT.md — Phase-0 Alignment Report

> **Status:** Alignment review only — no implementation.  
> **Canonical location:** `docs/contexts/lab/LAB_MASTER_TEST_ALIGNMENT.md`  
> **Source plan:** [`LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md`](LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md) (v1.2)  
> **Branch context:** `dev-jude-master-lab-test`  
> **Date:** 2026-05-21

Review completed against `docs/contexts/lab/LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md` (v1.2), `docs/ARTIFACTS.md`, sibling lab docs, and current `LabContext` code/SQL. **No implementation was performed.**

---

## 1. Alignment Summary

| Area | Status | Notes |
|------|--------|-------|
| **Master plan (canonical)** | **Aligned** | Finalized principles (workflow-first, snapshots, EMR Tarif-only, server-owned result structure, billing 1:1:N, no compat/V2, naming, seed DELETE+INSERT) are internally consistent in `LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md`. |
| **Referenced lab docs** | **Partially aligned** | `lab-agent.md` / `lab-workflow.md` do not contradict masters; they are silent on EMR/master specifics. `lab-domain.md`, `lab-integration.md`, `lab-implementation-plan.md` still describe **pre-master** shapes or generic integration. |
| **API contract artifact** | **Gap** | `LAB_API_CONTRACT.md` is indexed in `ARTIFACTS.md` but **does not exist** in the repo. |
| **Repository code** | **Expected baseline drift** | Implemented LWF matches **old** EMR/free-form result model; master tables/features are absent (correct for pre-Phase-1). |

**Overall:** Architecture direction in the master plan is **ready to drive implementation**. Cross-artifact and code alignment need **documentation cleanup** (and later in-place contract replacement per plan) before agents rely on sibling docs alone.

---

## 2. Inconsistencies Found

### Master plan — minor internal items

| Item | Location | Issue |
|------|----------|-------|
| Typo | §3 table | `SOt` → should read **source of truth** |
| Phase 0 checklist | §12 | Items still unchecked (`MLC`/`LTD` allocator rules, EMR payload, BIL target) — content exists in body but **allocator algorithm** is not fully specified (only “sequential hex in seed”) |
| “reconciles” | Executive summary | Used only as **anti-pattern** (`NOT: client sends structure → server reconciles`) — acceptable |

No `fromEmrV2`, API versioning, dual-contract, or `IF NOT EXISTS` seed merge language found in the master plan.

### Sibling documentation vs master plan

| Artifact | Contradiction |
|----------|----------------|
| `lab-implementation-plan.md` §19 #6 | **“Test catalog at order create → Snapshot from EMR payload”** conflicts with Tarif-only + LWF resolution. |
| `lab-implementation-plan.md` §5.2, §6 | Order items: `TestId` / `TestCode` / `TestName` / tube / specimen from EMR-shaped snapshots — pre-target state, not labeled as legacy. |
| `lab-implementation-plan.md` §6 / folder tree | References `BILRG_LabResultVersion`, `BILRG_LabResultComponent`, `LabResultVersionModel` — **implemented** model is `LabResultDocument` + `BILRG_LabResultItem` (doc drift, not master-plan conflict). |
| `lab-implementation-plan.md` Appendix B | **`LabRegIntegration` V2 / `LabBillingIntegration` V2** — integration implementation generations; risk of confusion with forbidden **API** V2 (master plan §10.6 is clear). |
| `lab-integration.md` §3 | Generic `POST /api/lwf/orders` — no `EmrOrderId`, Tarif-only payload, or `GET byEmrOrderId`. |
| `lab-domain.md` §20, §26 | Bare **“test”** (e.g. vacutainer grouping, “1 test → many component”) — ambiguous vs **LabTest** / `LabTestDefinition`. |
| `ARTIFACTS.md` | Lists `LAB_API_CONTRACT.md` — **file missing**. |

### Implemented code vs target (documented as current state in master plan §1.1)

| Area | Current | Target (master plan) |
|------|---------|----------------------|
| `LabOrderCreateFromEmrCmd` | No `EmrOrderId`; returns `OrderId` + `OrderNo` | `EmrOrderId` in; EMR ack without `LabOrderId` dependency |
| `LabOrderItemInput` / `LabOrderItemModel` | `TestId`, `TestCode`, `TestName`, tube, specimen, `RequiredTubeCount` | Tarif (+ optional name); LWF resolves structure |
| `LabResultRecordCmd` | Client sends full structure (`ComponentCode`, `ComponentName`, etc.) | Values-only against server scaffold |
| `BILRG_LabOrder` | No `EmrOrderId` column | Phase 3 alter |
| `ILabBillingIntegration` | `LabBillingChargeRequest(OrderId, UserId)` only | Future: all Tarif lines → **one** Tindakan (stub does not yet encode line collection) |

This code drift is **expected** until Phases 1–4; it is not a master-plan defect.

---

## 3. Naming Issues Found

### Master plan — consistent with required strategy

| Required | Present in plan |
|----------|-----------------|
| `BILRG_LabComponentMaster` | §5.2 |
| `BILRG_LabTestDefinition` | §5.3 |
| `BILRG_LabTestComponent` | §5.4 |
| `BILRG_LabOrderItemComponent` | §5.7 (snapshot-only) |
| `MLCxxxx` / `LTDxxxx` `VARCHAR(7)` | §2.2, §2.3, §5 |
| `LabTestCode` / `LabTestName` on snapshots | §2.3, §5.6 (avoids bare `TestCode` in **new** code) |

`TarifId VARCHAR(12)` on `LabTestDefinition` is correct (BIL reference, not master PK).

### Repository / docs — inconsistencies

| Location | Issue |
|----------|-------|
| `BILRG_LabOrderItem` | Columns `TestId`, `TestCode`, `TestName` — legacy; plan targets `TestDefinitionId`, `LabTestCode`, `LabTestName` |
| `LabOrderItemInput`, DALs, OWR payload | Propagate `TestCode` / `TestName` |
| `lab-implementation-plan.md` | Planned `LabResultComponent*` vs actual **`LabResultItem*`** |
| `lab-domain.md` | Bare “test” without `Lab` prefix |
| `NAMING.md` example | `ResultComponentModel` — generic; lab master uses `LabTestComponent` / order `LabOrderItemComponent` |

No `LabMasterFeature`, `MasterLabComponent*`, or `ILabOrderItemComponentRepo` in code (matches plan §6.1).

---

## 4. Contract Ambiguities Found

| Topic | Ambiguity | Recommendation before Phase 1 |
|-------|-----------|--------------------------------|
| **`LAB_API_CONTRACT.md`** | Indexed but missing; master plan says co-evolve with code | Add minimal stub stating **pre-release, evolving**, or remove from index until Phase 1 creates it |
| **EMR create response** | Plan: omit `LabOrderId` from EMR integration DTO; code returns `OrderId` | Document in contract: internal `OrderId` for lab UI only; EMR uses `EmrOrderId` |
| **External patient orders** | Plan §7.4: same Tarif-only; code uses same `LabOrderItemInput` | Explicitly state external path shares resolver + payload shape |
| **`LabBillingChargeRequest`** | Single `OrderId` today | Document target shape: order id + **collection of Tarif lines** for one `CreateTindakan` |
| **`MLC`/`LTD` allocation** | Seed-file sequential hex only | Phase 0 closure: rule for admin-created `LTD` (counter prefix vs manual), max `FFFF`, validation |
| **Result value keys** | `ComponentNo` or `ComponentId` (§8) | Pick one primary key for API in Phase 4 appendix |
| **Enum stability** (`lab-implementation-plan.md` Appendix A) | “Stable once frontend integrates” vs pre-release API freedom | Clarify: enum **numeric values** stable; HTTP/DTO shapes may change in place |

**EMR contract in master plan:** Clear and consistent — `EmrOrderId`, `TarifId`, optional `TarifName`; EMR does not send lab structure. No `fromEmrV2` or migration wording in lab master docs.

**Result ownership in master plan:** Clear — server structure, client values; merge/reconciliation forbidden.

**`LabOrderItemComponent`:** Clear — persistence-only via `LabOrderRepo`; no feature/API/repo interface.

---

## 5. Simplifications Applied

**Documentation updates applied** (branch `dev-jude-master-lab-test`, post–Phase-0):

1. `lab-implementation-plan.md` — master plan in primary references; legacy vs target order/result shapes; `LabResultItem` naming; §19 EMR/catalog/billing rows; enum vs DTO stability; integration “next impl” wording (not API V2).
2. `LAB_API_CONTRACT.md` — pre-release stub created.
3. `lab-integration.md` — EMR §3 aligned to Tarif-only target and actual routes.
4. `LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md` — Phase 0 checkboxes completed; `SOt` typo fixed.

**Already reflected in master plan v1.2 (prior simplification, verified):**

- Single in-place `fromEmr` contract (no V2/versioning)
- Full DELETE+INSERT vendor seed (no upsert/merge)
- Lightweight `LabOrderItemComponent` (no feature layer)
- Server-owned result scaffold (no reconciliation engine)
- One BIL Tindakan per LWF order with many Tarif lines
- LOINC/analyzer/HL7/FHIR deferred

**Recommended doc simplifications before Phase 1** — **done** (see list under “Documentation updates applied” above).

---

## 6. Ready-for-Phase Assessment

| Phase | Ready? | Blockers / prerequisites |
|-------|--------|---------------------------|
| **Phase 1 — `LabComponentMaster`** | **Yes, with minor prep** | Close Phase 0 allocator note for **seed-only** `MLC`; add/fix `LAB_API_CONTRACT.md` entry; table/seed folder names already specified |
| **Phase 2 — `LabTestDefinition`** | **Yes, after Phase 1** | Needs `MLC` catalog; define `LTD` allocation for hospital admin (not only seed) |
| **Phase 3 — Order resolution** | **Not yet** | Requires Phases 1–2 + doc updates on EMR/billing adapter; `EmrOrderId` column; replace `LabOrderItemInput` / `fromEmr` response shape in place |

### Phase 0 checklist (from master plan §12)

| Item | Verdict |
|------|---------|
| Field lists + error codes (Appendix B) | **Sufficient** for Phase 1–2 start |
| `MLC`/`LTD` allocator rules | **Partial** — seed sequential hex OK; **admin `LTD` allocator** should be written down |
| EMR payload: `EmrOrderId` + Tarif only | **Documented** in master plan; **not propagated** to sibling docs/code |
| BIL: one Tindakan / many Tarif lines | **Documented** in master plan; **stub contract** still minimal |

**Conclusion:** Repository is **architecturally aligned** with finalized decisions in the master plan. Sibling docs and `LAB_API_CONTRACT.md` stub now reflect the target contracts; **code baseline** remains intentionally behind until Phases 1–4. **Phase 1 can start** (hospital `LTD` allocator detail may be finalized during Phase 2 admin work).

---

## Quick reference — master plan strengths (no redesign needed)

```text
Workflow:     LWF authority, BIL billing, EMR initiates only
EMR:          EmrOrderId + TarifId (+ optional TarifName)
Masters:      LabComponentMaster (vendor) → LabTestDefinition (hospital)
Snapshots:    LabOrderItem + LabOrderItemComponent + LabResultItem
Results:      Server structure → client values only
Billing:      1 Order → 1 Tindakan → N Tarif lines
Compat:       None (pre-release, replace in place)
```
