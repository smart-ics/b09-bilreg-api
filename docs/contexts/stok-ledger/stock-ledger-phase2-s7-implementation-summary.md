# Stock Ledger Phase 2 / P2-S7 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P2-S7 — Availability Discovery live adapter (G-08)  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Prior foundation:** [`stock-ledger-phase2-s6-implementation-summary.md`](./stock-ledger-phase2-s6-implementation-summary.md); P2-S1 legacy read patterns; Phase 1 `IAvailabilityDiscoveryPort`  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS; G-08 acceptance  

---

## WHAT

Live **read-only** Infrastructure adapter for G-08: provisional Availability Discovery from Legacy Stock Authority (`tb_stok`) by Item + Stock Location (+ optional Expiration Date), returning Receipt Source candidates. Discovery only — **not** final FIFO allocation.

| Area | Delivered |
|---|---|
| Infrastructure | `AvailabilityDiscoveryPort` implementing existing `IAvailabilityDiscoveryPort` |
| Query | Parameterized `tb_stok` SELECT by `fs_kd_barang` + `fs_kd_layanan` with `fn_qty > 0` |
| Filtering | Optional Exact Expiration Date match after sentinel-aware date parse |
| Mapping | Surviving rows → `AvailabilityCandidateType` (Receipt Source, location, qty, ED, batch) |
| Outcomes | `CandidatesFound` or `InsufficientAuthoritativeStock`; never invents candidates |
| Port docs | Contract note: live adapter + `StaleOrNotCurrent` reserved for Phase 3/5 Freshness Gate |
| Tests | `AvailabilityDiscoveryPortTest` — multi-DO, ED filter, empty/insufficient, depleted excluded, legacy≠Ledger authority, no FIFO selection, no write side effects |
| DI / API | **No** production DI registration; **no** public endpoints |

`FakeAvailabilityDiscoveryPort` remains for unit/orchestration doubles. Provenance Discovery was **not** implemented. Stock Ledger layers are **not** consulted.

---

## Important repository-driven decisions

| Decision | Why |
|---|---|
| New `AvailabilityDiscoveryPort` under `StockLedgerFeature/` | Matches P2-S1 pattern; Availability access shape (Item+Location across DOs) differs from G-05 reconstruction (Item+DO across locations) |
| Dedicated SQL, not `tb_stok_dal.ListData` | Avoid name joins / StokFeature coupling; keep qty filter and deterministic order in the adapter; do not modify legacy DALs |
| Separate from `ILegacyStockReadPort` | Availability ≠ Provenance ≠ Reconstruction read; G-05 port is wrong key shape for outbound discovery |
| One candidate per surviving `tb_stok` row | Candidate DTO carries ED + Batch; provisional input for later allocation — not aggregated FIFO pick |
| `ORDER BY fs_kd_do, fd_tgl_ed, fs_kd_trs` | Deterministic listing only; **not** FIFO selection |
| ED filter applied after parse | Reuses P2-S1 sentinel/`3000-01-01` handling; exact match when filter supplied (BR-STL-031 eligibility semantics) |
| Never return `StaleOrNotCurrent` | Plan freshness note: G-12 orchestration belongs to Phase 3/5 callers |
| No Stock Ledger reads | Coexistence authority remains legacy; reconstructed layers must not override discovery qty |
| No production DI | Same Phase 1/2 stance; tests construct adapter manually |

---

## Deviations from the P2-S7 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Optional Application wiring | None | No production consumer yet; DI deferred like P2-S1…S6 |
| Illustrative adapter naming | `AvailabilityDiscoveryPort` | Matches port name and Phase 1/2 naming style |

No planned P2-S7 behavior was deferred that blocks P2-S8.

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **132 passed**, **8 skipped** (G-23 placeholders), 0 failed |
| P2-S7 new tests | 7 (`AvailabilityDiscoveryPortTest`) |
| Prior suite | Preserved (125 prior executable + 7 new = 132) |
| Multi-DO candidates | All eligible Receipt Sources returned; other location excluded |
| Optional ED filter | Only matching Expiration Date candidates |
| Empty / insufficient | `InsufficientAuthoritativeStock` with empty candidate list |
| Depleted / qty ≤ 0 | Not offered as available stock |
| Legacy vs Ledger | Legacy qty returned even when Ledger Remaining differs |
| FIFO | No single-layer selection; all eligible candidates returned |
| Legacy writes | None (`SELECT` only) |
| P2-S1…S6 behavior | Unchanged |

---

## Remaining work handed to P2-S8

**P2-S8 — Reconstruction harness, recovery, Phase 2 exit hardening** can proceed. It should:

1. Cover remaining Phase 2 coexistence/reconstruction harness themes (concurrent reconstructors, legacy write during Phase B, retry, duplicate trigger, bounded-query failure as practical).
2. Un-skip or replace **only** reconstruction-relevant G-23 placeholders; leave Phase 3/4/5 skips in place.
3. Provide controlled recovery for incomplete additive reconstruction without touching legacy authority.
4. Record index/SLO residual debt for G-25; produce Phase 2 implementation report / exit checklist.

P2-S8 (and later phases) must treat P2-S7 discovery results as **provisional** input: reconstruct/synchronize + Freshness Gate before trusted Ledger-enriched FIFO allocation.

P2-S7 does **not** deliver: Freshness Gate, Legacy Change Discovery, FO/native stock transactions, final FIFO outbound, Provenance Discovery, or production coexistence enablement.
