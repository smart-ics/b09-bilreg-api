# Stock Ledger Phase 2 / P2-S2 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P2-S2 — Reconstruction basis / fingerprint capture (init-only)  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Prior foundation:** [`stock-ledger-phase2-s1-implementation-summary.md`](./stock-ledger-phase2-s1-implementation-summary.md); Phase 1 opaque `SynchronizationPositionType`  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS; sync ADR fingerprint + bounded replay  

---

## WHAT

SQL-free Application helper that turns a scoped P2-S1 legacy snapshot (balances + journals) into a mechanism-neutral opaque Synchronization Position suitable for Phase C revalidation and for initializing Synchronization Position after successful reconstruction. Freshness/basis token only — not authority, ownership, watermark cursor, change discovery, or Freshness Gate.

| Area | Delivered |
|---|---|
| Application | `LegacyReconstructionBasisCalculator` — `Compute(balances, journals)` → `SynchronizationPositionType` |
| Algorithm | SHA-256 over canonical material fields; version constant `"fingerprint-v1"` |
| Opaque payload | Raw 32-byte digest (fits existing `VARBINARY(512)` Scope column) |
| Tests | `LegacyReconstructionBasisCalculatorTest` — determinism, material change, order normalize, empty scope, domain handoff |
| DI / API | **No** production DI; **no** public endpoints; **no** live `ILegacyChangeDiscoveryPort` |

P2-S1 `LegacyStockReadPort` and Phase 1 Scope persistence were **not** modified. No legacy writes.

---

## Important implementation decisions (repository-first)

| Decision | Why |
|---|---|
| Application static helper (not Infrastructure) | Snapshot-in / hash-out needs no SQL; P2-S1 already owns live reads |
| Reuse `SynchronizationPositionType` | Phase 1 mechanism-neutral opaque + algorithm version; Scope repo already round-trips it |
| Snapshot-in API only | Caller (later P2-S6) reads via G-05 then fingerprints; Phase C compares two independent computes |
| Do **not** implement `ILegacyChangeDiscoveryPort` | Phase 3 owns discovery/set-diff; this slice must not claim G-13 |
| Normalize order to P2-S1 keys | Equivalent scoped sets produce the same basis even if list order differs |
| Exclude balance `ReceiptTime` / `LastMutationTime` from v1 | Avoid non-material column noise flipping the basis (P2-S2 plan risk) |
| Algorithm version `"fingerprint-v1"` | Matches existing Scope/position test convention; explicit and stored with the position |
| No Domain changes | Domain already accepts caller-supplied position in `CompleteReconstruction` |

### Material fields hashed (fingerprint-v1)

- **Journals:** `LegacyJournalId`, `MutationKindId`, `MutationTransactionId`, `BrgId`, `ReceiptSourceId`, `LayananId`, `QuantityIn`, `QuantityOut`, `UnitCost`, `ExpirationDate`, `Batch`, `MutationTime`, `PurchaseOrderId`
- **Balances:** `LegacyRowId`, `BrgId`, `ReceiptSourceId`, `LayananId`, `Quantity`, `UnitCost`, `ExpirationDate`, `Batch`, `PurchaseOrderId`

Canonical stream uses section markers (`J` / `B`), counts, invariant decimals, ISO date/time strings, and a null sentinel for nullable strings/dates.

---

## Deviations from the Phase 2 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Application and/or Infrastructure | Application only | SQL-free; Infrastructure read adapter already exists |
| Update Phase 2 plan slice-progress table | **Not done** | Explicit deliverable instruction: do not modify the Phase 2 plan |
| Illustrative helper naming | `LegacyReconstructionBasisCalculator` | Aligns with `LegacyStockReadPort` / reconstruction-basis language |

No planned P2-S2 behavior was deferred that blocks P2-S3.

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **89 passed**, **8 skipped** (G-23 placeholders), 0 failed |
| P2-S2 new tests | 7 (identical fixture, journal change, balance change, order normalize, empty scope, CompleteReconstruction handoff, non-material timestamps ignored) |
| Prior suite | Preserved (82 prior executable + 7 new = 89) |
| Domain SQL in calculator | None |
| `tb_stok` / `tb_buku` writes | None |
| Live change discovery / Freshness Gate | None |

---

## Remaining work handed to P2-S3

**P2-S3 — Domain readiness for reconstructed baseline facts** can proceed. It should:

1. Extend domain factories/kinds as needed for reconstructed Movement/Layer/Position facts (origin=`Reconstructed`).
2. **Not** implement baseline calculation, claim TX, or Phase B/C orchestration yet (P2-S4…S6).
3. Later orchestration (P2-S6) will: read via `ILegacyStockReadPort` → `LegacyReconstructionBasisCalculator.Compute` (Phase B) → re-read + recompute for Phase C equality → on success pass position into `CompleteReconstruction`.

P2-S2 does **not** deliver: reconstruction claim/calculate/persist, Availability Discovery, or Phase 3 discovery/catch-up.
