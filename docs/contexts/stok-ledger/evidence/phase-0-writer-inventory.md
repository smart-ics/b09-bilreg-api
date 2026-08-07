# Stock Ledger Phase 0 — Writer Inventory and FO Coverage Matrix

**Artifact status:** Phase-0 evidence — frozen baseline (2026-08-07)  
**Basis:** [`clbGenStokX1.cls`](../clbGenStokX1.cls) + [`phase-0-profile-results.md`](./phase-0-profile-results.md)  
**Date:** 2026-08-07  
**Authority during coexistence:** `tb_stok` + `tb_buku` remain Stage B source of truth.
**Governing exit:** [`../stock-ledger-phase-0-exit-review.md`](../stock-ledger-phase-0-exit-review.md)

---

## 1. Entry point

`Public Function Generate(xKodeTrs, xVoid, xTglVoid, xJamVoid, xVoidDelete)` routes by `Left(xKodeTrs, 2)`.

| FO prefix | Post handler | Void handler | Snapshot presence |
|---|---|---|---|
| `DM` | `GenStokDO` | `GenStokDOVoid` | Yes (`DO` / `DO_V`) |
| `MT` | `GenStokMutasi` | `GenStokMutasiVoid` | Yes (`MT_IN`/`MT_OUT` + voids) |
| `PK` | `GenStokPakai` | `GenStokPakaiVoid` | Yes |
| `DU` | `GenStokDOBillUmum` | `GenStokDOBillUmumVoid` | Yes (dominant) |
| `DR` | rewrites to `MT`+suffix → `GenStokDOBillUmumReserved` | Reserved void | **No** `DU-R_*` rows in snapshot |
| `DS` | rewrites to `DU`+suffix → `GenStokDOBillUmumSerah` | Serah void | **No** `DU-S*` rows in snapshot |
| `DT` | `GenStokDOBillTipe` | `GenStokDOBillTipeVoid` | **No** `DT` / `DT_V` rows; no `DT` mutasi prefix |
| `MN` | `GenStokMusnah` | `GenStokMusnahVoid` | Post yes; `MN_V` not observed |
| `RB` | `GenStokReturBeli` | Void | Yes |
| `RU` | `GenStokReturJualUmum` | Void | Yes |
| `RT` | `GenStokReturJualTipe` | Void | **No** rows |
| `AJ` | `GenStokAdjust` | Void | Yes (`AJ_*`) |
| `RP` | `GenStokRepack` | Void | Yes |

Constants `DB` / `RJ` exist but have **no `Generate` route** in this extract. Snapshot also has **no** `DB`/`RJ` jenis rows.

---

## 2. `xVoidDelete` behavior (FQ-03)

From `AddStok` / `RemoveStok`:

| `xVoidDelete` | Journal (`tb_buku`) | Current stock (`tb_stok`) |
|---|---|---|
| `False` | Insert compensating void jenis (e.g. `*_V`) | Insert/update/delete per qty rules |
| `True` | **Physical DELETE** of original journal rows filtered by mutasi/barang/(layanan)/ED | Still mutates current stock |

**Synchronization consequence:** Any append-only cursor over surviving `tb_buku` rows **misses** void-delete events. Deletion-aware discovery is mandatory.

**Caller inventory residual risk:** This repository contains the Transaction Script extract only. Deployed front-office callers that pass `xVoidDelete=True` are **not fully enumerated** from the snapshot. Phase 0 treats every void path as **capable** of delete mode until ops signs off the caller matrix.

---

## 3. Characterized anomalies

### 3.1 Typed-sale void mutation-type anomaly (script)

`GenStokDOBillTipeVoid` reads `MUTASI_JUAL_TIPE` (`DT`) posts but writes void jenis `MUTASI_JUAL_UMUM_VOID` (`DU_V`) instead of `DT_V` (see ~line 709).

Snapshot: no `DT` posts and no `DU_V` rows with `fs_kd_mutasi` prefix `DT` — anomaly is **script-proven**, not exercised in this hospital’s retained journal.

### 3.2 MT void leg imbalance (data)

`MT_IN`/`MT_OUT` counts match; `MT_IN_V` (1192) ≠ `MT_OUT_V` (1172). Sample FO ids have unequal void-leg counts. Mapping for sync must tolerate incomplete paired voids and reconcile against `tb_stok`.

### 3.3 Extra writer jenis `AJX_*`

`AJX_MIN` / `AJX_PLUS` appear in snapshot but not in current script constants → **hidden or superseded writer**. Inventory incomplete until legacy owners confirm.

### 3.4 Synthetic buku ids

`SYS` / `SYS-01` rows (~4,600) with blank jenis — treat as non-FO noise for synchronization identity design.

---

## 4. FO coverage matrix — Phase 0 characterization approval

“Approved” here means **characterized enough to plan coexistence**, not “new-system supported.” Implementation phases remain as in the roadmap.

| Transaction | Legacy writer today | Snapshot evidence | Sync implication | Recommended phase | Phase 0 status |
|---|---|---|---|---|---|
| DO Receipt `DM` | `GenStokDO` / Void | Strong | Later VB6 activity on DO must sync | 4 | Characterized |
| Transfer `MT` | `GenStokMutasi` / Void | Strong; void-leg anomaly | Sync both legs; tolerate imbalance | 5 | Characterized |
| Usage `PK` | `GenStokPakai` / Void | Strong | Catch-up before Ledger allocation | 5 or 7 | Characterized |
| Sale `DU` | `GenStokDOBillUmum` / Void | Dominant | High volume; deletion-aware voids | 7 | Characterized |
| Reserved `DR` | Reserved handlers via MT rewrite | **Unused in snapshot** | Confirm ops before Phase 6 | 6 | Deferred pending ops |
| Handover `DS` | Serah handlers via DU rewrite | **Unused in snapshot** | Confirm ops before Phase 6 | 6 | Deferred pending ops |
| Typed sale `DT` | `GenStokDOBillTipe` / Void (`DU_V` anomaly) | **Unused in snapshot** | Map void jenis carefully if enabled | 7 | Characterized (script); unused locally |
| Destruction `MN` | `GenStokMusnah` / Void | Low volume post | Sync disposition | 5 or 7 | Characterized |
| Purchase return `RB` | Retur beli | Present | Provenance by DO | 7 | Characterized |
| Sales return `RU` | Retur jual umum | Present | Provenance discovery | 7 | Characterized |
| Typed return `RT` | Retur jual tipe | Unused | Synthetic DO risk if enabled | 7 | Deferred pending use |
| Adjustment `AJ` | Adjust ± / void | Present + **AJX_*** extra | Include AJX in discovery vocabulary | 7 | Characterized; AJX owner TBD |
| Repack `RP` | Repack asal/hasil | Present | Treat as paired consequence | 7 | Characterized |
| `DB` / `RJ` | Constants only | Absent | No route until proven | 0 / defer | Confirmed unrouted |

---

## 5. Persistence mutations every sync design must cover

1. Insert `tb_buku` (+ usually insert/update `tb_stok`).
2. Update `tb_stok.fn_qty` (partial outbound).
3. Delete `tb_stok` at depletion (`fn_qty` reaches 0).
4. Insert compensating `*_V` journal when `xVoidDelete=False`.
5. Delete original `tb_buku` rows when `xVoidDelete=True`.
6. Multi-location transfer OUT+IN pairs (and possible incomplete void pairs).

FIFO selection in `RemoveStok` orders by `fd_tgl_mutasi, fs_jam_mutasi` (not Batch; ED filter applied in WHERE when provided).
