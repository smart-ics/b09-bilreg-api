# Stock Ledger Phase 2 / P2-S1 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-07  
**Slice:** P2-S1 — Live legacy reconstruction read adapter (G-05)  
**Plan:** [`stock-ledger-phase2-implementation-plan.md`](./stock-ledger-phase2-implementation-plan.md)  
**Prior foundation:** Phase 1 complete (P1-S1…P1-S8); port contract from [`stock-ledger-phase1-s7-implementation-summary.md`](./stock-ledger-phase1-s7-implementation-summary.md)  
**Governing baseline:** Phase 0 Exit Review PASS WITH RISKS  

---

## WHAT

Live **read-only** Infrastructure adapter for G-05: parameterized `tb_stok` / `tb_buku` reads by Item + Receipt Source **across all Stock Locations**, with deterministic ordering. No reconstruction orchestration, fingerprint, Freshness Gate, Availability/Provenance Discovery, FO transactions, or legacy writes.

| Area | Delivered |
|---|---|
| Infrastructure | `LegacyStockReadPort` implementing existing `ILegacyStockReadPort` |
| Queries | `ListCurrentBalances` → `tb_stok`; `ListJournalEntries` → `tb_buku`; filter `fs_kd_barang` + `fs_kd_do` only |
| Mapping | Rows → existing Application `LegacyStockBalanceType` / `LegacyStockJournalEntryType` |
| Test fixture helper | `StockLedgerSchemaFixture.EnsureLegacyStockTables()` applies repo `tb_stok.sql` / `tb_buku.sql` on disposable `devTest` when missing |
| Tests | `LegacyStockReadPortTest` — multi-location, empty scope, stable ordering, no write side effects |
| DI / API | **No** production DI registration; **no** public endpoints |

Legacy `StokFeature` DALs were **not** modified. `FakeLegacyStockReadPort` remains for unit/orchestration doubles.

---

## Important implementation decisions (repository-first)

| Decision | Why |
|---|---|
| New `LegacyStockReadPort` under `StockLedgerFeature/` | Plan + G-05: StokFeature DALs are location-scoped; `tb_buku_dal` enumerable DO binding is defective — not the reconstruction API |
| Reuse Application port DTOs | Avoid parallel projections; Phase 1 contract is the source of truth |
| Balance `ORDER BY fs_kd_layanan, fs_kd_trs` | Deterministic surviving-row input order |
| Journal `ORDER BY fd_tgl_jam_mutasi, fs_kd_trs, fs_kd_layanan` | Deterministic fallback input order (BR-STL-104); **not** a Synchronization Position / watermark (Phase 0 FQ-01) |
| Null-coalesce `conn.Read` → empty list | Nuna `Read` returns null when no rows; port contract requires empty collections |
| No production DI | Same Phase 1 stance; tests construct adapter manually; register when P2-S6+ orchestration needs it |
| Ensure legacy tables on `devTest` | `tb_stok`/`tb_buku` were absent on disposable DB; apply existing SqlDb scripts outside ambient TX (never `HOSPITAL_HPL`) |
| Skip optional index SQL scripts | Plan marks them optional; DBA-owned apply (G-25). Documented below for handoff |

---

## Query / SLO notes (G-05 acceptance)

From Phase 0 evidence ([`evidence/phase-0-profile-results.md`](./evidence/phase-0-profile-results.md)):

| Fact | Value |
|---|---|
| Hottest Item+DO journal depth | ~4,509 `tb_buku` rows across 16 locations |
| Live indexes | No `(fs_kd_barang, fs_kd_do)` index on either table |
| Proposed (DBA-owned) | `tb_buku (fs_kd_barang, fs_kd_do, fd_tgl_jam_mutasi, fs_kd_trs)`; optional `tb_stok (fs_kd_barang, fs_kd_do, fs_kd_layanan)` |

Adapter queries are **bounded** by Item + Receipt Source parameters. Full-table scans are avoided by design; production performance for hot scopes still depends on the proposed indexes (not applied in this slice).

---

## Deviations from the Phase 2 plan

| Plan suggestion | Actual choice | Why |
|---|---|---|
| Optionally check in proposed-index SqlDb scripts | Deferred | Optional for G-05; DBA rollout is G-25; notes recorded here instead |
| Update Phase 2 plan slice-progress table | **Not done** | Explicit deliverable instruction: do not modify the Phase 2 plan; progress recorded in this summary |
| Illustrative adapter naming | `LegacyStockReadPort` | Matches port name and Phase 1 naming style |

No planned P2-S1 behavior was deferred that blocks P2-S2.

---

## Tests / build results

| Check | Result |
|---|---|
| `dotnet test --filter FullyQualifiedName~StockLedgerFeature` | **82 passed**, **8 skipped** (G-23 placeholders), 0 failed |
| P2-S1 new tests | 5 (multi-location balances, multi-location journals, empty scope, stable ordering, no write side effects) |
| Prior Phase 1 suite | Preserved (77 prior executable + 5 new = 82) |
| Disposable DB | `ConnStringHelper.GetTestEnv()` → `devTest`; legacy schema via `EnsureLegacyStockTables` |
| `tb_stok` / `tb_buku` writes via adapter | None (SELECT only) |
| `HOSPITAL_HPL` | Rejected by fixture guard |

---

## Remaining work handed to P2-S2

**P2-S2 — Reconstruction basis / fingerprint capture (init-only)** can proceed. It should:

1. Consume scoped snapshots from `ILegacyStockReadPort` (`ListCurrentBalances` + `ListJournalEntries`).
2. Compute a mechanism-neutral opaque basis fingerprint + algorithm version for Phase C revalidation and Synchronization Position **initialization**.
3. **Not** implement change discovery, set-diff, catch-up, or Freshness Gate.

P2-S1 does **not** deliver: reconstruction claim/calculate/persist (P2-S3…S6), Availability Discovery (P2-S7), or production index apply.
