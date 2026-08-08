# Stock Ledger Phase 4 / P4-S1 — Implementation Summary

**Status:** COMPLETE — **Gate PASS**  
**Date:** 2026-08-08  
**Slice:** P4-S1 — Live DM Legacy Compatibility Writer (receipt post shape)  
**Plan:** [`stock-ledger-phase4-implementation-plan.md`](./stock-ledger-phase4-implementation-plan.md)

---

## Gate verdict

**PASS.** Disposable `devTest` characterization proves the live adapter can insert consumer-dependent DM receipt post shapes (`tb_buku` inbound journal + `tb_stok` balance) through `ILegacyCompatibilityWriterPort`, readable by `LegacyStockReadPort`, with coherent HPP/PO/DO/mutasi/ED, ambient TX rollback, and `fingerprint-v1` hashing. Domain remains SQL-free. No production DI/HTTP enablement.

P4-S2 may proceed.

---

## Objective

Answer: can the new application reproduce the authoritative DM receipt consequence through the existing compatibility port without leaking VB6 control flow into Domain?

---

## §5 Compatibility contract (A/B)

### A — Behavior consumers depend on (implemented for post)

| Concern | Implementation |
|---|---|
| Mutation jenis | `fs_kd_jenis_mutasi = "DO"` |
| Mutation id | `fs_kd_mutasi` = DO / Receipt Source id |
| `tb_buku` | INSERT inbound: `fn_stok_in = qty`, `fn_stok_out = 0`, HPP, ED, batch, PO, DO, mutasi times + combined `fd_tgl_jam_mutasi` |
| `tb_stok` | Always INSERT new `ST*` row: `fn_qty = fn_qty_in = qty` (never merge) |
| Write order | `tb_buku` then `tb_stok` (VB6 `AddStok` order) |
| HPP / PO / DO / ED / batch / satuan | Mapped from port DTOs |
| IDs | `NunaId.NewLegacyCompact("BK"|"ST")` — 10-char legacy compact ids |
| `fd_tgl_do` / `fs_jam_do` | VB6 parity: schema defaults (`3000-01-01` / `00:00:00`) → `ReceiptTime = null`; `LastMutationTime` from mutasi |

### B — Incidental / deferred

| Concern | Treatment |
|---|---|
| Void / `DO_V` / balance Delete | Fail-closed → P4-S4 |
| Non-Receipt movement kinds | `NotSupportedException` |
| `clbGenStokX1` / `Generate` | Not ported |
| `SqlBulkCopy` buku path | Not used; parameterized INSERT |
| PO/DO header writebacks | Not evidenced in `GenStokDO`; not invented |
| Production DI registration | Explicitly omitted |

---

## What was implemented

| Area | Deliverable |
|---|---|
| Application ports | Optional `SmallestUnitId` on balance + journal DTOs (additive; existing call sites unchanged) |
| Infrastructure | `LegacyCompatibilityWriterPort` — live DM receipt post adapter |
| Tests | `LegacyCompatibilityWriterPortTest` — 7 characterization tests on `devTest` |
| Docs | This summary + plan progress + ARTIFACTS |

**Explicitly not implemented:** Native DO Receipt UseCase, capability flag, void path, Freshness Gate wiring, production DI/HTTP, UoW write-order change, failure-injection matrix (P4-S3).

---

## Repository decisions

| Decision | Rationale |
|---|---|
| `fd_tgl_do` VB6 parity (defaults) | Prefer VB6 shape; `LegacyStockReadPort` nulls sentinel dates; baseline calculator already falls back to journal/mutasi time |
| Populate `fd_tgl_jam_mutasi` | Intentional improvement over VB6 `AddStok` (which leaves combined default); read port prefers combined then falls back to split parts |
| `NunaId.NewLegacyCompact` for BK/ST | User-directed; repo pattern (`AdmissionModel` RG); no custom ParamNoDal wrapper |
| Always INSERT on receipt | Matches VB6; never merge existing balance |
| Require 1:1 journal↔balance pairing | Live writer needs both shapes; fail closed on mismatch |
| No production DI | Capability remains disabled; tests compose manually |

---

## Deviations from the Phase 4 plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Prefer `INunaCounterDal` / ParamNoDal for ST/BK decimal counters (`ST00001234`) | `NunaId.NewLegacyCompact("BK"|"ST")` — time-based Base-36 10-char ids (e.g. `BKA581UU9A`) | Still fits `VARCHAR(10)`; format differs from classic VB6 decimal padding; consumers key by id presence, not decimal pattern |
| Optional counter helper class | None — call `NunaId` inline in writer | Simpler; no parallel ID infrastructure |

---

## Validation evidence

Focused run (disposable `devTest` only):

```text
dotnet test --filter FullyQualifiedName~LegacyCompatibilityWriterPortTest
Passed!  - Failed: 0, Passed: 7, Skipped: 0
```

| Test | Result |
|---|---|
| Read-back qty/HPP/DO/location/ED | Pass |
| `ReceiptTime` null + `LastMutationTime` from mutasi | Pass |
| `fingerprint-v1` stable hash | Pass |
| Rollback without `Complete` | Pass |
| Unsupported movement kind throws | Pass |
| Unique BK/ST ids (2 lines → 4 distinct) | Pass |
| Mismatched journal/balance counts throw | Pass |

Full `StockLedgerFeature` parallel suite may show intermittent shared-DB deadlocks unrelated to this adapter (existing ambient risk on `devTest`). Gate evidence is the focused filter above.

---

## Residual risks / handoff to P4-S2

| Item | Note for next slice |
|---|---|
| **FQ-02 / ID generation** | `NewLegacyCompact` is clock/Base-36 — no ParamNoDal counter advance on rollback. Residual FQ-02 for decimal VB6 counters is **not** exercised by this path. P4-S3 still owns full UoW + live-writer atomicity proof. |
| **Journal entries required** | P4-S2 must populate **both** `BalanceMutations` and `JournalEntries` (1:1). Balance-only drafts (as in P1-S8 UoW tests) are insufficient for the live writer. |
| **Satuan** | Optional `SmallestUnitId` added; P4-S2 should populate when DO lines have terkecil unit. Empty string is schema-compatible. |
| **UseCase mapping** | Map Native receipt → `LegacyCompatibilityWriteRequest` with `MutationKindId = "DO"`, mutasi = DO id, `QuantityOut = 0`. |
| **Capability / DI** | Keep flag default-off; do not register live writer in production DI until intentionally enabled for tests/composition. |
| **Void** | Writer rejects Delete / `IsVoid` — do not extend in S2; wait for P4-S4. |
| **Gate dependency** | P4-S2 may start — this summary records **PASS**. |

---

## Rollback / containment

Remove or stop composing `LegacyCompatibilityWriterPort`; keep port + `FakeLegacyCompatibilityWriterPort`. No production registration was added. Disposable-DB test rows cleaned per test.
