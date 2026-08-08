# Stock Ledger Phase 2 / P2-S4 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P2-S4 — Baseline calculation + ambiguity classification  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Prior foundation:** [`stock-ledger-phase2-s3-implementation-summary.md`](./stock-ledger-phase2-s3-implementation-summary.md); G-05 snapshot DTOs (P2-S1); Domain reconstructed shapes (P2-S3)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS; BR-STL-065–068, BR-STL-080, BR-STL-102–105  

---

## WHAT

Pure Application calculation of a proposed reconstructed Stock Ledger baseline from a scoped P2-S1 legacy snapshot (balances + journals), classifying `Balanced` vs `Inconsistent` with an explicit reason. No claim TX, no persistence, no fingerprint computation beyond optional later reuse of P2-S2 by orchestration, no Freshness Gate, and no authority transfer.

| Area | Delivered |
|---|---|
| Application | `LegacyReconstructionBaselineCalculator.Calculate(scope, balances, journals)` |
| Result type | `ReconstructionBaselineCalculationResult` + `ReconstructionBaselineOutcomeEnum` |
| Balanced payload | Establishing `StockMovementModel` (Receipt, `Origin = Reconstructed`) + per-location `StockPositionModel` layers |
| Inconsistent payload | Explicit `InconsistencyReason`; no movement/positions |
| Tests | `LegacyReconstructionBaselineCalculatorTest` — multi-location, depleted retention, quantity mismatch, material ambiguity, determinism, empty/balances-only |
| DI / API | **No** production DI; **no** public endpoints |

Domain, Infrastructure, SqlDb, and P2-S1/S2/S3 behavior were **not** modified.

---

## Important implementation decisions (repository-first)

| Decision | Why |
|---|---|
| Application static helper (alongside P2-S2 fingerprint calculator) | Snapshot-in / proposed-baseline-out needs no SQL; Domain already expresses reconstructed shapes |
| Balance-anchored algorithm | Plan guidance: authority Remaining from `tb_stok`; `tb_buku` enriches provenance / depleted layers; avoid full journal-as-movement replay (Phase 3 sync duplication risk) |
| Provenance grouping key = Location + UnitCost + ExpirationDate | Matches material outcomes that matter for BR-STL-105; Batch remains informational only |
| Synthetic deterministic IDs (`RBL\|…`) | Equivalent inputs must produce identical output; ULID would break determinism; does not recreate vanished legacy layer ids (BR-STL-066/067) |
| One establishing Receipt across locations | Matches P2-S3 readiness shape; layer-forming movement links without inventing reconstruction-specific movement kinds |
| Multiple balance rows sharing provenance + journal history ⇒ Inconsistent | Material ambiguity — Initial/provenance cannot be assigned without inventing identities |
| Journal net ≠ balance Remaining ⇒ Inconsistent | Real quantity mismatch; never silent force-balance |
| Depleted when journal net = 0 and no surviving `tb_stok` row | Intentional representational difference (BR-STL-080); Ledger retains Remaining = 0 |
| Reuse existing Domain factories | `StockLayerModel.Create(..., remainingQuantity:)`, `CreateReceipt(..., Reconstructed)`, `StockPositionModel.Create` |

---

## Deviations from the Phase 2 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Application and/or Domain helpers | Application only | P2-S3 already closed Domain readiness; calculation is orchestration-facing |
| Illustrative calculator naming | `LegacyReconstructionBaselineCalculator` | Aligns with `LegacyReconstructionBasisCalculator` / `LegacyStockReadPort` naming |
| Full event-sourced journal replay | Not implemented | Explicit plan guidance and risk against Phase 3 sync duplication |

No planned P2-S4 behavior was deferred that blocks P2-S5.

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **107 passed**, **8 skipped** (G-23 placeholders), 0 failed |
| P2-S4 new tests | 9 (`LegacyReconstructionBaselineCalculatorTest`) |
| Prior suite | Preserved (98 prior executable + 9 new = 107) |
| Domain SQL / Infrastructure leakage | None |
| Claim / persist / Phase B–C orchestration | None |
| P2-S1 / P2-S2 / P2-S3 behavior | Unchanged |

---

## Remaining work handed to P2-S5

**P2-S5 — Phase A reconstruction claim** can proceed. It should:

1. Acquire reconstruction work for one Item + Receipt Source in a **short** transaction: coexistence state → `Reconstructing`, then commit.
2. Use existing Scope transitions (`NotReconstructed` → `ReconstructionRequired` → `Reconstructing`) with the **minimum** claim-safety mechanism (conditional status update); Phase 1 Scope lacks OCC.
3. **Not** read/calculate full history inside the claim TX; **not** persist Movements/Layers in TX-A.
4. Leave baseline calculation to P2-S4 (`LegacyReconstructionBaselineCalculator`) and Phase B/C orchestration to P2-S6.

P2-S6 (later) will: claim (P2-S5) → read via G-05 → fingerprint (P2-S2) → calculate (P2-S4) → revalidate → persist Balanced payload or `MarkReconstructionInconsistent(reason)`.
