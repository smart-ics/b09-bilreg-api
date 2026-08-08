# Stock Ledger Phase 5 / P5-S3 — Slice Review

**Review date:** 2026-08-09  
**Artifact status:** Slice gate re-review after destination OCC remediation — **NO-GO**  
**Slice:** P5-S3 — Native Stock Transfer UseCase + capability flag  
**Plan:** [`stock-ledger-phase5-implementation-plan.md`](./stock-ledger-phase5-implementation-plan.md)  
**Implementation summary:** [`stock-ledger-phase5-s3-implementation-summary.md`](./stock-ledger-phase5-s3-implementation-summary.md)

**Inputs reviewed:**

- [`stock-ledger-phase5-implementation-plan.md`](./stock-ledger-phase5-implementation-plan.md) § P5-S3 + §8 atomic boundary + §9 capability + reviewer checkpoints
- [`stock-ledger-phase5-s3-implementation-summary.md`](./stock-ledger-phase5-s3-implementation-summary.md) (including OCC remediation notes / reviewer checklist)
- Prior gate text in this artifact (2026-08-08 OCC **NO-GO**)
- Governing Stock Ledger Phase 0 / Phase 1–4 patterns (UoW, Freshness, fingerprint-v1, DO Receipt capability style)
- **Actual codebase** (not summary-only):
  - `PostStockTransferStockConsequenceCommand.cs` / Handler (`BuildDestinationPositions`, source consume)
  - `TransferConsequenceIdempotency.cs`
  - `TransferLegacyCompatibilityMapper.cs` (`TryResolveSourceRow`, exact-qty preference)
  - `StockLedgerStockTransferOptions.cs`
  - `IStockConsequenceUnitOfWork.cs` / `StockConsequenceUnitOfWork.cs` (`AdditionalScopeStates`)
  - `StockPositionRepo.cs` OCC contract
  - `PostStockTransferStockConsequenceHandlerTest.cs` (12 scenarios, including OCC increase)
  - Related Domain `CreateTransfer` / FIFO ordering / Position Version semantics

**Not claimed by this review:** P5-S4 void, P5-S5 concurrency, P5-S6 coexistence, FQ-06 / production enablement.

---

## Executive verdict

### **NO-GO**

Do **not** start P5-S4 until LegacyRowId resolution cannot silently OUT a different `tb_stok` row than the FIFO-selected Ledger layer, and that failure mode is covered by a disposable-DB test.

The prior destination Position OCC increase NO-GO is **cleared**: `BuildDestinationPositions` now applies one `Version + 1` after merging inbound layers (mirrors source consume), and `ExistingDestination_MultiLayerIncrease_PreservesOcc` asserts establish/increase under OCC. Focused suite: **12/12 passed**.

That remediation alone is not enough to authorize the next slice. Exact-qty disambiguation in `TransferLegacyCompatibilityMapper.TryResolveSourceRow` can still commit Ledger + legacy with divergent physical-row identity under a normal multi-MT_IN accumulation pattern — hidden coexistence instability for later outbound/void work.

---

## Findings

### [Major] Exact-qty LegacyRowId disambiguation can OUT the wrong row

**Where:** `TransferLegacyCompatibilityMapper.TryResolveSourceRow`  
(`TransferLegacyCompatibilityMapper.cs`)

**What works today:** Unique attribute matches (Item / DO / Location / HPP / ED / Batch, qty ≥ slice) resolve correctly. Multi-candidate cases with **no** unique exact qty return `Ambiguous` → handler `Inconsistent`.

**Defect:** On `candidates.Count > 1`, a single exact-qty match is accepted instead of failing closed. Quantity is not Ledger layer identity. FIFO orders by `EffectiveReceiptTime` then `StockLayerId`; the mapper never sees `StockLayerId` / ERT.

**Reachable scenario (this slice creates it):**

1. Two `MT_IN` into the same destination write-scope with the same DO / HPP / ED / batch (split transfers from the same source receipt → same ERT on both dest layers).
2. Rows e.g. ST1=5, ST2=3; layers L1 rem=5, L2 rem=3; equal ERT → FIFO uses `StockLayerId` order.
3. Outbound qty 3: FIFO may consume from L1; exact-qty picks ST2.

Ledger and legacy then diverge on which physical row moved, while the UoW still commits both sides. That breaks allocation-explicit / coexistence assumptions and makes P5-S4 void on later hops unsafe.

**Direction (for fixing agent; do not prescribe exact patch here):** Prefer fail-closed on any multi-candidate match (align with the summary’s Ambiguous → `Inconsistent` contract), or bind a durable LegacyRowId into the plan→mapper path so resolution does not re-guess by qty. Add a disposable-`devTest` case: two same-attribute dest balances, transfer qty equal to the non-FIFO-first row.

---

### [Minor] Same-`BrgId` multi-line plans are independent

Per-line `TrustedStockAllocationOrchestrator.PlanAsync` does not reserve prior lines’ allocations. Duplicate Item lines can plan overlapping layers, then throw in `Consume` instead of typed `InsufficientStock`. Typical mutasi is one line per barang; behavior is fail-closed but contract-rough. Carry forward: enforce unique `BrgId` per request, or accumulate reserved qty across lines.

---

### [Minor] `SmallestUnitId` not propagated on MT legs

Handler passes `SmallestUnitId: null`; `LegacyStockBalanceType` has no satuan field, so the mapper cannot copy from live balances. Writer persists empty `fs_kd_satuan`. Qty / DO / HPP / ED / batch / PO conservation still holds; satuan continuity is a carry-forward compatibility gap (read-port / mapper), not an authority or partial-write defect.

---

### [Minor] No dedicated Ambiguous → `Inconsistent` test

Mapper/handler path exists; the suite does not force a multi-candidate Ambiguous outcome. Related to the Major above; carry forward with the remediation test.

---

## Prior NO-GO (destination OCC) — cleared

| Check | Result |
|---|---|
| `BuildDestinationPositions` single OCC bump via merge + `Create(..., baseVersion + 1)` | **Pass** |
| Existing dest + ≥2 inbound layers → `Version == prior + 1` | **Pass** — `ExistingDestination_MultiLayerIncrease_PreservesOcc` |
| Source single-bump unchanged | **Pass** |
| Insert path for empty dest still OK | **Pass** |
| Focused handler suite | **Pass** — 12/12 |

---

## Prior NO-GO (idempotency) — still cleared

| Check | Result |
|---|---|
| SourceConsequence key `MT\|{SourceTransactionId}` (no BrgId) | **Pass** — code + happy-path key asserts |
| Multi-item reorder / partial retry quantity-neutral | **Pass** — covered by tests |
| Capability default **false**, independent of DO Receipt | **Pass** — `StockLedgerStockTransferOptions`; separate section name; not silently enabled |
| `CreateTransfer` conservation; no new Receipt Source at destination | **Pass** — same Receipt Source + source ERT / valuation / batch |
| Trusted allocation before write; stale / insufficient fail closed | **Pass** — P5-S1 orchestrator; typed outcomes; no UoW on failure |
| Legacy OUT/IN + `fingerprint-v1` + `AdditionalScopeStates` wiring | **Pass** for UoW/fingerprint; **undermined** when mapper wrong-rows (see Major) |
| Ulid-tail compact BK/ST hygiene (prior compact-id Minor) | **Pass** — seed + mapper no longer rely on `NewLegacyCompact` for this path |
| No void / ConcurrentOutbound / production HTTP / FQ-06 claim | **Pass** |

---

## Acceptance / architecture assessment

### Slice problem / question

> Can an authorized Stock Transfer be represented as a Native conserved Transfer movement, update source/destination layers/positions, refresh coexistence Synchronization Position, and commit with the P5-S2 legacy consequence through the existing UoW — behind a disabled-by-default capability flag?

| Aspect | Assessment |
|---|---|
| Native conserved `CreateTransfer` + flag | Met for unique-row happy paths |
| Trusted allocation → short TX UoW → live MT OUT/IN | Met for unique-row happy paths |
| Scope `fingerprint-v1` refresh (multi-DO via `AdditionalScopeStates`) | Met |
| Destination establish (empty) | Met |
| Destination **increase** (existing Position, multi-layer) | **Met** after OCC remediation |
| Whole-mutasi idempotency | Met |
| Allocation-explicit legacy OUT | **Not reliably met** under same-attribute multi-ST accumulation |

### Reuse vs parallel stacks

Reuses P5-S1 orchestrator, Domain `CreateTransfer`, P5-S2 writer, existing consequence UoW; discovery / Freshness / reconstruction stay outside the short TX. `AdditionalScopeStates` is a minimal, backward-compatible multi-DO Scope extension. No Stage C / `IsAuthoritative` / production DI/HTTP / void / ConcurrentOutbound landed prematurely.

### Atomicity / fail-closed

Ledger + legacy commit through ambient `TransHelper` via existing UoW. Allocation / mapper typed failures return before `Complete`. Full multi-position failure-injection matrix correctly deferred to P5-S5. The Major wrong-row case is worse than fail-closed: it **commits** both authorities with inconsistent physical-row identity.

---

## Reviewer-note checklist (from S3 summary)

| # | Note | Result |
|---|---|---|
| 1 | Capability default false, independent of DO Receipt | Pass |
| 2 | `CreateTransfer` conservation; no new Receipt Source; ERT preserved | Pass (code + happy-path ERT assert) |
| 3 | Trusted allocation before writes; stale/insufficient fail closed | Pass |
| 4 | Legacy follows Ledger plan; `fingerprint-v1` per affected DO | Pass for wiring; undermined by Major wrong-row |
| 5 | Whole-mutasi key; multi-item reorder/partial retry quantity-neutral | Pass |
| 6 | Ambiguous LegacyRowId → `Inconsistent` | **Fail** — exact-qty escape hatch |
| 7 | No void / locks / ConcurrentOutbound / HTTP / FQ-06 | Pass |
| 8 | Destination establish/**increase** stable under Position OCC | **Pass** (prior Major cleared) |

---

## Artifact hygiene

| Artifact | Status at review |
|---|---|
| `stock-ledger-phase5-s3-implementation-summary.md` | Present; status synced to gate **NO-GO** (LegacyRowId); OCC remediation noted as accepted |
| Phase-5 Slice Progress | P5-S3 Gate **NO-GO** (LegacyRowId) |
| `docs/ARTIFACTS.md` | Linked S3 summary + this review (descriptions synced) |
| This review | Authoritative gate result for proceeding to P5-S4 |

Do not mark P5-S3 COMPLETE / PASS until mapper remediation lands and a further re-review flips this artifact to PASS / PASS WITH MINOR FINDINGS.

---

## Why NO-GO (concrete)

OCC increase is fixed and tested. The remaining Major is coexistence-critical: under repeated `MT_IN` with shared DO/HPP/ED/batch, exact-qty disambiguation can deplete a different `tb_stok` row than the FIFO-selected layer, while still committing Ledger + legacy. That is hidden instability for later outbound/void work.

**Not required for the fix:** void path, lock-order / ConcurrentOutbound, coexistence proof, or production enablement.

---

## Recommended next step

1. Fixing agent: remediate LegacyRowId resolution (fail-closed multi-candidate and/or durable row binding) + add the multi-balance wrong-row regression; keep capability default-off.  
2. Re-review against this artifact’s Major finding and the S3 summary reviewer notes.  
3. Only then mark P5-S3 gate accepted and authorize P5-S4.
