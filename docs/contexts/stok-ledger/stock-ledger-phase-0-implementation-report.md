# Stock Ledger Phase 0 — Implementation Report

**Status:** COMPLETE — approved Phase-0 baseline (frozen 2026-08-07)  
**Exit review:** [`stock-ledger-phase-0-exit-review.md`](./stock-ledger-phase-0-exit-review.md) — **PASS WITH RISKS**  
**Date:** 2026-08-07  
**Scope:** Revalidate legacy behavior and coexistence baseline — **no Stock Ledger application code**  
**Snapshot:** `HOSPITAL_HPL` @ `dev.smart-ics.com` (read-only)  
**Standards:** `docs/ENGINEERING.md`, `docs/DATABASE.md`, `docs/NAMING.md`

**Baseline freeze:** Architecture decisions in this report and linked ADRs are LOCKED. Do not reopen authority, reconstruction scope, sync concept, Freshness Gate, origin labels, or coexistence strategy without an explicit change request. Residual testing debt is tracked below and does not reopen those decisions.

---

## What was implemented

Documentation and evidence only:

| Artifact | Purpose |
|---|---|
| [`evidence/phase-0-profile-queries.sql`](./evidence/phase-0-profile-queries.sql) | Reproducible read-only profiling pack |
| [`evidence/phase-0-profile-results.md`](./evidence/phase-0-profile-results.md) | Sanitized volumes, schema, FQ-01/02/04/05 findings |
| [`evidence/phase-0-writer-inventory.md`](./evidence/phase-0-writer-inventory.md) | FO routes, `xVoidDelete`, anomalies, coverage matrix |
| [`adr/ADR-stock-ledger-legacy-change-discovery.md`](./adr/ADR-stock-ledger-legacy-change-discovery.md) | Sync mechanism decision |
| [`adr/ADR-stock-ledger-mixed-writer-concurrency.md`](./adr/ADR-stock-ledger-mixed-writer-concurrency.md) | Lock order / conflict interim policy |
| This report | Handoff for Phase 1 agents |

No Domain/Application/Infrastructure Stock Ledger code, no FARIN wiring, no DDL on `HOSPITAL_HPL`, no `DEVTEST` deploy (snapshot evidence was sufficient).

---

## Why this shape

Phase 0 exists to prevent premature schema/cursor commitments. Snapshot profiling showed that the most tempting cursor (`fd_tgl_jam_mutasi` + id) is populated but **unsafe alone** because of void deletes, ties, and ID/time inversions. An evidence-backed fingerprint + bounded replay **decision** unblocks Phase 1’s mechanism-neutral Synchronization Position without inventing CDC. That decision is architectural selection — **not** empirical proof that every change kind is already detected in tests (see residual debt).

---

## FQ-01–FQ-07 resolutions

| ID | Status | Decision |
|---|---|---|
| **FQ-01** | **Resolved** | `fd_tgl_jam_mutasi` is 100% populated and parseable; matches date+time parts. Usable as attribute/hint. **Not** a sole append cursor (ties + deletes). |
| **FQ-02** | **Partial / deferred** | Surviving `BK*` ids dominate; non-unique index; synthetic `SYS*`; ID order ≠ time on hot scopes. IDs are identities, not a sync cursor. Live multi-instance counter atomicity **not** proven from snapshot (remains open for concurrency evidence). |
| **FQ-03** | **Interim** | Script proves `xVoidDelete` physical deletes; all void routes accept the flag. Deployed caller list outside this extract is **not** complete — treat delete mode as always possible. Extra `AJX_*` jenis implies another writer. |
| **FQ-04** | **Resolved** | Live indexes/triggers/FKs documented; schema richer than repo SQL; no triggers/FKs on stock tables; no index on `(barang, do)` or `fd_tgl_jam_mutasi`. Proposed additive indexes recorded for Phase 1+ DBA review. |
| **FQ-05** | **Resolved** | ~4.7M buku / ~11k stok; hottest Item+DO ~4.5k journal rows; zero-qty stok absent. Bounded per-scope replay is feasible; org-wide reconstruct-every-request rejected as steady state. |
| **FQ-06** | **Unresolved + interim** | VB6 RMW without evidenced TX/isolation. Interim: short TX, deterministic order, revalidate, conditional update, Ledger OCC, `UPDLOCK/HOLDLOCK` as .NET candidate only. Mixed-writer proof deferred (blocks Phase 9). |
| **FQ-07** | **Interim deferred** | CT/CDC not enabled; no universal change log. Do not assume all writers can emit additive records. Prefer fingerprint mechanism; revisit change log/CDC only with ops mandate. |

---

## Important design decisions

1. **Deletion-aware sync = fingerprint + bounded delta/replay** (ADR change discovery). Selected; detection experiments remain residual debt for G-13.
2. **Reject** `fs_kd_trs`-alone and watermark-alone cursors.
3. **Concurrency ADR** is explicitly interim; production coexistence still requires VB6 race tests.
4. **FO matrix** characterization-approved for planning; DR/DS/DT/RT unused in this snapshot; `AJX_*` flagged.
5. **DT void script anomaly** (`DU_V` instead of `DT_V`) recorded for Phase 7 mapping.
6. **MT void leg imbalance** recorded for sync tolerance.
7. **No authority-cutover language**; Stage B authority remains `tb_stok` + `tb_buku`.

---

## Assumptions

- `HOSPITAL_HPL` is a faithful production snapshot for schema and historical journal shape.
- `clbGenStokX1.cls` in-repo is the operative Transaction Script for FO stock generation.
- Empty FARIN tables are unused spikes, not a sync feed.
- Ops will confirm unused FO families (DR/DS/DT/RT) and `AJX_*` ownership before those Phase 6/7 enables.

---

## Residual Phase 0 testing debt

These items were in Phase 0 Testing/Validation but were **not** executed. They do **not** reopen locked architecture decisions. They are carried forward:

| Residual item | Carries into | Blocks |
|---|---|---|
| Insert/update/`tb_stok` delete/`tb_buku` void-delete/backdate/repost **detection experiment** for the selected mechanism | **G-13** (Phase 3) | Claiming Freshness Gate / sync production-ready |
| Characterization fixtures for all routed FO families | G-22 / G-23 | Per-family enablement |
| Controlled concurrent VB6 sessions (FQ-06) | **G-17** (Phase 9 gate) | Mixed-writer production enablement |
| Complete deployed `xVoidDelete` caller signoff | G-13 / G-28 | Hardening void sync claims |
| `AJX_*` / hidden writer owner confirmation | **G-28** | Adjustment sync vocabulary completeness |

**Do not** interpret Phase 0 completion as proof that fingerprint discovery already detects all change kinds in automated tests.

---

## Remaining work / known limitations before Phase 1

Phase 1 **may start** on additive domain/persistence with mechanism-neutral Synchronization Position (Exit Review GO conditions).

Still open (do not block Phase 1 scaffolding; block later production gates):

| Item | Blocks |
|---|---|
| Live VB6 transaction/isolation/lock proof (FQ-06) | Phase 9 / mixed-writer enablement |
| Empirical change-detection experiment | G-13 acceptance / Phase 3 |
| Complete deployed `xVoidDelete` caller signoff | Hardening of void sync tests |
| `AJX_*` owner confirmation | Adjustment family completeness |
| DR/DS/DT/RT operational use confirmation | Phases 6–7 for those families |
| DBA approval of proposed indexes | Sync/reconstruction performance |
| Optional CT/CDC/change-log program | Only if fingerprint cost fails SLOs |

**Explicitly out of Phase 0:** Movement/Layer domain, ED+FIFO replacement of FEFO spike, Legacy Compatibility Writer, reconstruction jobs, capability flags.

---

## Engineer checklist (Phase 0 increment)

- [x] Canonical domain rules and gap/FQ IDs referenced
- [x] `tb_stok + tb_buku` declared Stage B source of truth
- [x] No `IsAuthoritative` / per-DO ownership / VB6 prohibition introduced
- [x] Reconstruction scope, write boundary, lock boundary, authority kept distinct (ADRs)
- [x] Origin labels not treated as authority
- [x] Deletion-aware sync mechanism selected
- [x] No stock application code; no final cutover work
- [x] Exit Review filed (PASS WITH RISKS)
