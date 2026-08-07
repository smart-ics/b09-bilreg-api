# ADR — Legacy Change Discovery and Synchronization Position

**Status:** Accepted (Phase 0 interim; revisit if ops enables CT/CDC or a universal change log)  
**Date:** 2026-08-07  
**Context:** Stock Ledger Stage B coexistence  
**Related:** [`stock-ledger-feasibility-review.md`](../stock-ledger-feasibility-review.md) §3, [`phase-0-profile-results.md`](../evidence/phase-0-profile-results.md), FQ-01 / FQ-02 / FQ-03 / FQ-07

---

## Decision

**Primary mechanism:** Per Item + Receipt Source **deterministic history fingerprint** plus **bounded delta/replay** against authoritative `tb_buku` / `tb_stok`.

**Rejected as sole cursor:**

- `fs_kd_trs` alone (non-unique clustered index; synthetic `SYS*` ids; ID order ≠ time order on ~84% of hot-scope rows).
- `(fd_tgl_jam_mutasi, fs_kd_trs)` append watermark alone (void-delete removes rows; heavy timestamp ties; ID not a reliable tie-break for chronology).

**Optional hybrid hint (non-authoritative):** A stored max observed `fd_tgl_jam_mutasi` (or scoped row count / checksum) may trigger freshness checks, but **must not** advance Synchronization Position without successful material reconciliation.

**Deferred:** Additive change log written by all stock writers; SQL Server Change Tracking / CDC (not enabled on this snapshot; would require ops ownership and VB6 participation for FQ-07).

**Phase 1 persistence:** Store a **mechanism-neutral Synchronization Position** opaque value + algorithm version, so the concrete fingerprint format can evolve without authority semantics.

---

## Why

Evidence from `HOSPITAL_HPL`:

1. `fd_tgl_jam_mutasi` is **100% populated** and matches date/time parts — useful attribute, not a complete change stream.
2. `xVoidDelete=True` **physically deletes** `tb_buku` rows — append polling cannot see voids.
3. Zero-qty `tb_stok` rows are absent — depletion is absence, not a tombstone row.
4. Hottest Item+DO histories are thousands of rows, not millions per scope — bounded replay is realistic.
5. CT/CDC are not on; FARIN stock tables exist empty — no existing additive stream to adopt.

Full reconstruct on every request remains a last-resort fallback only, not the planned steady state.

---

## Fingerprint sketch (non-normative implementation detail)

For scope `Item + Receipt Source` across all locations, a candidate fingerprint includes ordered contribution of surviving journal identities and current stock snapshots, for example:

- count of `tb_buku` rows for the scope;
- checksum / hash over `(fs_kd_trs, fs_kd_jenis_mutasi, fs_kd_layanan, fn_stok_in, fn_stok_out, fd_tgl_jam_mutasi, …)` for surviving rows;
- checksum over current `tb_stok` quantities for the same Item+DO.

Any mismatch vs stored Synchronization Position ⇒ `SynchronizationRequired` (or `LegacyChangePending`) before Ledger-dependent allocation.

Deleted journal facts are detected as **missing identities / fingerprint drift**, then interpreted as accountable Ledger correction/reversal — never by erasing Ledger history.

---

## Consequences

- Phase 1 must not hard-code a timestamp cursor into domain APIs.
- Phase 2 reconstruction initializes Synchronization Position from the fingerprint at baseline commit.
- Phase 3 implements catch-up idempotency keys using durable source identities (mutasi + jenis + item + location + direction + qty + ED/batch as validated).
- Proposed supporting indexes (barang+do+time) remain additive DBA work before heavy sync load.
- FQ-07 remains **interim deferred**: if later evidence shows fingerprint cost too high, evaluate change log/CDC with ops — do not invent VB6 coverage.

---

## Explicit non-decisions

- No authority transfer; Native / Reconstructed / LegacySynchronized remain origin labels only.
- No requirement for broker or event sourcing.
