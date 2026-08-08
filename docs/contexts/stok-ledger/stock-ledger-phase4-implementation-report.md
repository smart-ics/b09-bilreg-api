# Stock Ledger — Phase 4 Implementation Report

**Status:** COMPLETE (technical capability behind disabled-by-default flag; does not claim production mixed-writer safety)  
**Date:** 2026-08-08  
**Phase:** 4 — First Native Stock Consequence: DO Receipt  
**Plan:** [`stock-ledger-phase4-implementation-plan.md`](./stock-ledger-phase4-implementation-plan.md)  
**Governing baseline:** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Phase 3 foundation:** COMPLETE — [`stock-ledger-phase3-implementation-report.md`](./stock-ledger-phase3-implementation-report.md)  
**Latest slice:** [`stock-ledger-P4-S5-implementation-summary.md`](./stock-ledger-P4-S5-implementation-summary.md)

---

## Implementation outcome

Phase 4 delivered a **disabled-by-default** technical capability for Native DO Receipt (post + void) that commits Stock Ledger semantics together with authoritative legacy-compatible `tb_stok` / `tb_buku` consequences, proven atomic with the live writer, and proven to catch up later fixture-simulated VB6 activity via Phase 3 Freshness Gate / sync — **without** transferring runtime authority, **without** production HTTP enablement, and **without** claiming FQ-06 / production G-17.

| Slice | Outcome |
|---|---|
| P4-S1 | Live DM Legacy Compatibility Writer (receipt post); **gate PASS** |
| P4-S2 | Native DO Receipt UseCase + Scope `EstablishFromNativeReceipt` + capability flag + greenfield prior-history fail-closed |
| P4-S3 | Live G-18 atomicity / failure injection + G-23 New→Legacy / PartialFailure harness |
| P4-S4 | Receipt void — accountable `Reverse` + compensating `DO_V`; fail closed when unsafe; `xVoidDelete=True` deferred |
| P4-S5 | Native → VB6-shaped change → Phase 3 sync catch-up; AlternatingWriters activated (fixture extent); Phase 4 exit |

**Success meaning (locked):**

```text
Technical capability complete ≠ safe for unrestricted production use
```

---

## Major decisions

1. **Runtime authority stays on `tb_stok` + `tb_buku`** for Stage B; `Native` is origin only.
2. **Single fingerprint algorithm:** `LegacyReconstructionBasisCalculator` / `fingerprint-v1` — no second hasher.
3. **Single consequence TX:** `IStockConsequenceUnitOfWork` commits idempotency → Movement → Position → Scope → legacy Apply.
4. **Capability default false:** `StockLedgerDoReceiptOptions.Enabled`; no production appsettings enablement; no public write HTTP.
5. **Greenfield Native post only (P4-S2):** already-`Reconstructed` or prior history → `ScopeNotEligible`.
6. **Void prefers accountable Reversal + `DO_V`** (not Ledger history erase; not `xVoidDelete=True`).
7. **Phase 3 sync consumed, not reimplemented** for later VB6 activity on Native-origin scopes.
8. **AlternatingWriters (P4-S5)** = Native establish once + multiple fixture-simulated VB6 mutations — not live VB6 binary and not a second Native post on the same DO.

---

## Coexistence scenarios proven

| Scenario | Evidence |
|---|---|
| New→Legacy | `NewToLegacy_ReceiptVisibleInLegacyAuthority` (P4-S3) |
| PartialFailure (live writer) | `StockConsequenceUnitOfWorkLiveAtomicityTest` + harness marker (P4-S3) |
| Native → VB6 → sync → continue | `NativeReceipt_Vb6ShapedChange_SyncCatchUp_ContinuesProcessing` (P4-S5) |
| AlternatingWriters (fixture) | `AlternatingWriters_RemainReconcileable` (P4-S5) |
| Capability off continuity | Handler tests + `CapabilityDisabled_NoNativeWrites_LegacySimulationUnaffected` (P4-S5) |
| Legacy→New (Phase 3) | Unchanged; still active in harness |

---

## Intentionally disabled / deferred

| Item | Owner |
|---|---|
| Production `StockLedgerDoReceipt:Enabled = true` | Phase 9 |
| Public production stock-write HTTP endpoints | Later / not Phase 4 |
| Live VB6 concurrent session proof (FQ-06 / production G-17) | Phase 9 |
| `ConcurrentOutbound` harness | Phase 5 |
| `xVoidDelete=True` physical journal delete void mode | Deferred (fail-closed) |
| Outbound / MT / FIFO allocation orchestration | Phase 5 |
| Reservation / Virtual Stock Location | Phase 6 |
| Other FO families (DU/DT/PK/…) | Phase 7 |
| Stage C / `IsAuthoritative` / ownership cutover | Forbidden in this roadmap stage |
| Porting `clbGenStokX1` control flow | Forbidden |

---

## Deviations from the Phase 4 plan (aggregate)

| Plan suggestion | Actual | Why |
|---|---|---|
| Pre-commit fingerprint only for void | P4-S4 also reconciles live fingerprint post-commit via `…\|FP` SyncBatch when needed | Safer discovery continuity |
| Optional Application glue in P4-S5 | Not required | P4-S2 SYNC bootstrap sufficient |
| AlternatingWriters as full bidirectional FO writers | Fixture: Native once + multiple VB6-shaped changes | Honors P4-S2 greenfield constraint |

No deviation reopens Stage B authority, origin labels, fingerprint continuity, or FQ-06 non-claim.

---

## Test / build status

| Check | Result |
|---|---|
| Solution build | Succeeds (existing unrelated warnings only) |
| P4-S5 focused filter | **3 passed**, 0 skipped |
| Coexistence harness | **10 passed**, **1 skipped** (`ConcurrentOutbound` → Phase 5) |
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` (single-threaded) | **284 passed**, **1 skipped**, 0 failed |
| FQ-06 / production G-17 claimed? | **No** |
| Capability default | **false** |

Parallel full-suite runs on shared `devTest` may still show intermittent deadlocks (documented since P4-S1). Gate evidence is focused and single-threaded filters.

---

## Phase 4 exit checklist (plan §12)

- [x] **P4-S1 gate passed:** live DM post compatibility writer accepted before Native UseCase enablement work proceeded.
- [x] **G-11 accepted for DM post and void:** legacy readers can consume new-system receipt/void consequences; delete-on-zero remains compatible; compatibility failure rolls back the complete consequence.
- [x] **G-18 accepted with live writer:** failure injection at persistence boundaries rolls back Ledger + legacy together; retry commits once.
- [x] **G-19 accepted as technical capability:** idempotent Native DO Receipt; Scope baseline + `fingerprint-v1` position; no authority/cutover fields.
- [x] **Void:** accountable Ledger reversal retained; `xVoidDelete=True` explicitly deferred with fail-closed rationale.
- [x] **Phase 3 integration:** Native → later legacy change → Freshness Gate / sync catch-up → continue; Native-origin is not treated as VB6 prohibition.
- [x] **G-23 Phase-4 markers:** New→Legacy activated; PartialFailure covered; AlternatingWriters activated to fixture-simulated extent.
- [x] **Capability flag default false;** no public production write endpoint required.
- [x] **Explicit non-claim:** FQ-06 / production G-17 **not** done.
- [x] **No** `IsAuthoritative`, Stage C, generic stock framework, or `clbGenStokX1` port.
- [x] Solution builds; StockLedgerFeature tests green for Phase 4 slices (single-threaded); Phase 1–3 tests remain green.
- [x] Phase 4 implementation report published; ARTIFACTS updated.

**Exit verdict:** Phase 4 exit criteria are **satisfied** as a **technical capability**. Production enablement remains **Phase 9**. Outbound Native paths remain **Phase 5+**.

---

## Residual risks / handoff

| Residual | Owner | Blocks Phase 5 coding start? |
|---|---|---|
| FQ-06 live VB6 concurrency / production G-17 | Phase 9 | **No** |
| G-25 proposed legacy indexes / p95 SLOs | Phase 8 / DBA | **No** |
| Legacy stock id counter multi-instance atomicity (FQ-02) | Production hardening may continue | **No** |
| Full FO family matrix | Phases 5–7 | **No** |
| Production capability enablement | Phase 9 | **No** (must remain off) |
| Shared disposable-DB deadlock under parallel tests | Test hygiene | **No** |

### Phase 5 may consume

| From Phase 4 | Use |
|---|---|
| Live `LegacyCompatibilityWriterPort` (post + void) | Pattern for outbound compatibility shapes |
| `PostDoReceipt` / `VoidDoReceipt` UseCase + capability options | Pattern for FO capability gating |
| Freshness Gate caller contract (P3-S6 / P4 greenfield nuance) | Required before allocate-from-layers |
| Coexistence harness (Native → VB6 → sync) | Extend for outbound scenarios; keep ConcurrentOutbound for Phase 5 |
| `fingerprint-v1` continuity | Do not fork hashing |

**Do not** claim FQ-06 / production G-17 complete when enabling FO paths. Live VB6/.NET mixed-writer proof remains **Phase 9**.
