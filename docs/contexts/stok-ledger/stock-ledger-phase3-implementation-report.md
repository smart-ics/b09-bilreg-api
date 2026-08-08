# Stock Ledger — Phase 3 Implementation Report

**Status:** COMPLETE (with deferred G-25 / FQ-06 debt; does not claim Phase 4 or production mixed-writer safety)  
**Date:** 2026-08-08  
**Phase:** 3 — Incremental Legacy Synchronization and Freshness Gate  
**Plan:** [`stock-ledger-phase3-implementation-plan.md`](./stock-ledger-phase3-implementation-plan.md)  
**Governing baseline:** Phase 0 Exit Review **PASS WITH RISKS** — [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md)  
**Phase 2 foundation:** COMPLETE — [`stock-ledger-phase2-implementation-report.md`](./stock-ledger-phase2-implementation-report.md)  
**Latest slice:** [`stock-ledger-P3-S8-implementation-summary.md`](./stock-ledger-P3-S8-implementation-summary.md)  
**Review backlog:** [`stock-ledger-phase3-review-backlog.md`](./stock-ledger-phase3-review-backlog.md) (R-001–R-007 Resolved)

---

## Implementation outcome

Phase 3 delivered **deletion-aware Legacy Change Discovery**, pure sync delta interpretation, material reconciliation (classify only), incremental catch-up with Synchronization Position advancement, Legacy Freshness Gate, sync-specific retry/serialization, G-23 sync harness activation, and **initial G-24 explainability** — **without** transferring runtime authority away from `tb_stok` + `tb_buku`, **without** FO/native writers, and **without** claiming production mixed-writer correctness (FQ-06 / G-17).

| Slice | Outcome |
|---|---|
| P3-S1 | Live G-13 discovery adapter + set-diff classifier; **gate PASS** |
| P3-S2 | Pure sync delta interpreter (compensatory JournalUpdate / R-005) |
| P3-S3 | Live G-16 P0 reconcile adapter; `AllowsMaterialSynchronizationAdvance` (R-006) |
| P3-S4 | Incremental catch-up + position advancement; R-001–R-004 / R-007 Resolved |
| P3-S5 | Legacy Freshness Gate (G-12); discovery fast-path + at-most-once catch-up |
| P3-S6 | Sync-specific bounded retry + claim serialization; FQ-06 non-claim documented |
| P3-S7 | Coexistence sync harness (G-23 sync portion); 4 later-phase skips retained |
| P3-S8 | Initial G-24 explainability + Phase 3 exit report |

**Success meaning (locked):** Ledger representation can be kept current with legacy authority ≠ authority transferred ≠ VB6 blocked ≠ production mixed-writer proven.

---

## Major decisions

1. **Runtime authority stays on legacy** for Stage B; sync success does not transfer authority.
2. **Single fingerprint algorithm:** `LegacyReconstructionBasisCalculator` / `fingerprint-v1` shared by reconstruction, discovery, and position advancement.
3. **Position advances only** after committed catch-up + material reconciliation that permits advance.
4. **History retention:** voids/deletes ⇒ correction/reversal movements; never erase Stock Ledger history.
5. **Freshness Gate replaces Authority Gate** — never blocks VB6; never rejects legacy writes because origin is Native/Reconstructed.
6. **Thin orchestration (P3-S4):** handler composes P3-S1…S3; no god-handler business rules; one sync boundary per execution.
7. **Sync-specific retry only (P3-S6):** no generic retry frameworks; .NET-side serialization only — not live VB6 FQ-06.
8. **Initial G-24 (P3-S8):** structured explainability on sync/gate results + existing Scope fields; **no new DB columns**; no ops dashboard.
9. **No production DI / public stock write endpoints** through Phase 3.

---

## Deviations from the Phase 3 plan

| Plan suggestion | Actual | Why |
|---|---|---|
| Illustrative class names | `LegacyChangeDiscoveryPort`, `LegacySyncDeltaInterpreter`, `SynchronizeStockLedgerScopeHandler`, `LegacyStockFreshnessGate`, `StockLedgerSyncExplainability` | Repository MediatR / Inventory UseCase style |
| Optional last-sync explanation column | Zero new SqlDb columns | Existing Scope + result DTOs suffice for initial G-24 |
| Optional structured logs | Nullable `ILogger` on sync handler only | Minimal; test-friendly; no cross-cutting logging platform |
| Coexistence harness “or successor” | Kept `StockLedgerCoexistenceHarnessPlaceholderTest` | Extend in place (P3-S7) |

No deviation reopens Stage B authority, origin labels, fingerprint continuity, or sync ADR selection.

---

## Fingerprint continuity (mandatory)

Confirmed across Phase 3:

| Call site | Uses `LegacyReconstructionBasisCalculator` / `fingerprint-v1` |
|---|---|
| Reconstruction Phase C position init | Yes (Phase 2) |
| Discovery `ComputeCurrentFingerprint` / compare | Yes (P3-S1) |
| Catch-up position advancement | Yes (P3-S4) |
| Freshness Gate algorithm-version precondition | Yes (P3-S5) |
| Explainability `AlgorithmVersion` | Reads stored position / calculator constant (P3-S8) |

**No second hash implementation was introduced.**

---

## Test / build status

| Check | Result |
|---|---|
| Solution build | Succeeds (existing unrelated warnings only) |
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` (single-threaded) | **241 passed**, **4 skipped**, 0 failed |
| Skipped | 4 G-23 later-phase placeholders (Phase 4/5/8 owners) |
| Legacy authority writes by sync path | None |
| FQ-06 / production G-17 claimed? | **No** |

---

## Unresolved risks / debt

| Debt | Owner | Blocks Phase 4 coding start? |
|---|---|---|
| G-25 proposed legacy indexes `(barang, do, …)` not applied | DBA / Phase 8 | **No** |
| G-25 production p95 SLOs | Ops / Phase 8 | **No** |
| FQ-06 live VB6 concurrency proof / production G-17 | Phase 9 | **No** (expected) |
| New→Legacy / FO partial-failure harness | Phase 4+ | **No** (expected) |
| Full G-24 metrics/alerts/runbook product | Phase 8 | **No** |
| G-28 unknown jenis vocabulary completeness | Before AJ sync-completeness claims | **No** |
| Multi-line aggregate void → precise journal→line Correct | Future (fail-closed Option A accepted for R-007) | **No** |

---

## Phase 3 exit checklist (plan §9)

Evaluated against repository evidence after P3-S8:

- [x] **P3-S1 gate passed:** live deletion-aware discovery accepted before catch-up was built.
- [x] **G-13 accepted:** live discovery detects insert, update, `tb_stok` delete, `tb_buku` void delete, backdated/tied movement, and repost on disposable fixtures; unclassifiable ⇒ `RequiresScopedReDerive` / `Undeterminable`.
- [x] **G-14 accepted:** opaque Synchronization Position (+ algorithm version) advances only after committed catch-up + successful material reconciliation; crash before commit retains prior position; advanced position uses `fingerprint-v1`.
- [x] **G-15 accepted:** catch-up applies `LegacySynchronized` facts with `SyncBatch` idempotency; duplicate batch quantity-neutral; voids become correction/reversal; success returns Scope to `Current`.
- [x] **G-16 P0 accepted:** reconciliation classifies only (no repair); intentional depleted-layer difference is not material inconsistency; real quantity mismatch surfaces `Inconsistent` / not-safe-to-advance.
- [x] **G-12 accepted:** Freshness Gate detects post-baseline legacy change, synchronizes or fails closed, and never acts as an Authority Gate against VB6.
- [x] **Sync duplicate protection / retry / crash recovery / .NET-side sync serialization covered;** explicit non-claim that FQ-06 / production G-17 is done.
- [x] **Synchronization portion of G-23 activated** for Legacy→New, duplicate sync, real mismatch, and .NET sync serialization race; Phase 4/5/8 harness markers remain skipped with owners listed.
- [x] **Initial G-24:** freshness/sync outcome is explainable from position + last discovery/reconcile/Scope state (`StockLedgerSyncExplainability` on sync/gate results; persisted Scope fields for failures). No ops dashboard.
- [x] **No `IsAuthoritative`;** no legacy row rewrites for sync convenience; depleted layers retained; no sync framework / workflow engine introduced.
- [x] **Solution builds;** StockLedgerFeature tests for Phase 3 slices pass; Phase 1/2 tests remain green.
- [x] **Phase 3 implementation report published;** plan slice progress table updated to COMPLETE.
- [x] **Explicit handoff notes** list Phase 4 residuals (below) and remaining G-17/G-25 debt.

**Exit verdict:** Phase 3 exit criteria are **satisfied**. Residual G-25 and FQ-06 work is **deferred** and does **not** block starting Phase 4 coding. Phase 4 FO behavior is **not** claimed complete. Production mixed-writer safety remains Phase 9.

---

## Readiness / handoff for Phase 4+

Phase 4 may consume:

| From Phase 3 | Use |
|---|---|
| `LegacyStockFreshnessGate` | Prove freshness before Ledger-dependent writes |
| `SynchronizeStockLedgerScopeHandler` | Catch-up for later VB6 activity |
| `StockLedgerSyncExplainability` | Seed for Phase 8 G-24 productization |
| Sync-serialization contract (P3-S6 summary) | Native consequence UoW must honor Freshness Gate + Scope claim |
| `fingerprint-v1` continuity | Do not fork hashing |

Phase 4 must still implement (not started in Phase 3):

1. Live `ILegacyCompatibilityWriterPort` / DO Receipt / FO post-void (G-11, G-19)  
2. New→Legacy coexistence harness scenarios  
3. Alternating writers requiring native FO writer  
4. Honor P3-S6 documented sync-serialization contract in native write UoW  

**Do not** claim FQ-06 / production G-17 complete when enabling FO paths. Live VB6/.NET mixed-writer proof remains **Phase 9**.

### Phase 4+ caller contract (from P3-S6 — document only)

Native consequence UoW / FO writers **must**:

1. Not allocate from Stock Ledger layers until Freshness Gate returns `Current` or `SynchronizedNow` (`IsSafeToTrustLedgerLayers == true`).
2. Serialize native write boundaries with synchronization claim / Scope synchronization state.
3. Honor `StaleOrNotCurrent` / `Inconsistent` as fail-closed for Ledger-dependent decisions.
4. Never treat provisional G-08 Availability Discovery as Ledger authority.
