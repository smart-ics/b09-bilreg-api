# ADR — Mixed-Writer Concurrency and Conflict Policy

**Status:** Accepted interim — Phase-0 baseline (frozen 2026-08-07) — **not** mixed-writer production proof  
**Date:** 2026-08-07  
**Context:** Stock Ledger Stage B coexistence with VB6  
**Related:** [`stock-ledger-implementation-roadmap.md`](../stock-ledger-implementation-roadmap.md) §7, FQ-06, [`phase-0-writer-inventory.md`](../evidence/phase-0-writer-inventory.md), [`stock-ledger-phase-0-exit-review.md`](../stock-ledger-phase-0-exit-review.md)

**Freeze note:** Interim lock order and conflict outcomes below are the approved design baseline for Phase 1+. Live VB6 proof (FQ-06 / G-17) remains an open **production** gate; do not reopen the interim policy without ADR amendment.

---

## Decision

Until controlled concurrent VB6 sessions prove shared locking, adopt the following **interim** policy for new-system design (Phase 1+), without claiming production mixed-writer safety:

1. **Short SQL transactions** for each new-system stock consequence (legacy-compatible writes + Ledger writes together).
2. **Deterministic lock / touch order:** Item → Receipt Source → Stock Location → legacy row identity (`fs_kd_trs`).
3. **Revalidate** authoritative `tb_stok` quantity (and sync fingerprint/basis) inside the consequence transaction before commit.
4. **Conditional mutation** on authoritative legacy rows (update/delete only if expected quantity/version still holds); on conflict → retry or reject — never silent overwrite.
5. **Optimistic concurrency** on future Stock Ledger position rows (version/token).
6. **Idempotency keys** for source consequences and sync batches.
7. **Bounded deadlock / version retries**.

`UPDLOCK, HOLDLOCK` (used elsewhere in this repository) is a **candidate for .NET-side** locking only. It is **not** accepted as sufficient protection against VB6 read-modify-write until FQ-06 is proven.

---

## Why

Repository evidence:

- `RemoveStok` is classic read-modify-write: `SELECT … ORDER BY fd_tgl_mutasi, fs_jam_mutasi` then `UPDATE`/`DELETE`/`INSERT` without lock hints in the extract.
- `clbGenStokX1` has **no** `BeginTrans` / isolation statements; transaction enclosure is unknown (caller/runtime).
- Snapshot profiling cannot observe live lock/isolation behavior.

Therefore FQ-06 is **Unresolved with safest interim** above. Phase 9 coexistence gate remains blocked on mixed-writer negative-stock proof.

---

## Required conflict outcomes (normative for later tests)

| Conflict | Minimum handling |
|---|---|
| New reads, VB6 commits before new write | Revalidate; retry or reject |
| VB6 reads, new commits before VB6 update | Shared protocol TBD after FQ-06 tests; .NET-only locks insufficient |
| Reconstruction vs legacy write | Phase C basis/fingerprint mismatch → retry / not current |
| Sync vs native | Serialize or version-check write boundary |
| Two new requests | Ledger position version + deterministic order |
| FIFO selection stale | Reload/revalidate in consequence TX |
| Multi-location transfer | Lock both locations in deterministic order; OUT+IN one TX |

---

## Write vs lock vs authority boundaries

| Boundary | Scope | Meaning |
|---|---|---|
| Reconstruction / reconciliation | Item + Receipt Source, all locations | Baseline and material compare |
| Write consistency (candidate) | Item + Receipt Source + Location | Local remaining quantity |
| Database locking | Smallest rows/ranges evidenced safe | Technical only |
| Runtime authority | Global Stage B | Always `tb_stok` + `tb_buku` |

None of the above is a per-DO ownership or cutover marker.

---

## Open proof required before enabling coexistence production

1. Controlled concurrent VB6 sessions updating overlapping `tb_stok` rows.
2. VB6 vs .NET outbound race on the same Item+Location(+DO).
3. Whether VB6 transaction scope covers the full FO stock consequence.
4. Whether a minimal shared lock protocol needs a legacy compatibility change at stock persistence only.

Until then: implement Phase 1–4 behind capability flags; do not enable Phase 9 mixed processing.

---

## Consequences

- Phase 1 UoW and repository ports must expose revalidation hooks.
- Do not document `Native` as exclusive writer for a scope.
- Concurrency tests in the roadmap §8 remain mandatory gates, not optional polish.
