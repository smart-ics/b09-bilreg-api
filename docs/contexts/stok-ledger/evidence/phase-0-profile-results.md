# Stock Ledger Phase 0 — Profile Results (sanitized)

**Artifact status:** Phase-0 evidence — frozen baseline (2026-08-07)  
**Source:** `HOSPITAL_HPL` on `dev.smart-ics.com` (production snapshot)  
**Captured:** 2026-08-07  
**Mode:** Read-only `SELECT` / metadata only  
**Query pack:** [`phase-0-profile-queries.sql`](./phase-0-profile-queries.sql)
**Governing exit:** [`../stock-ledger-phase-0-exit-review.md`](../stock-ledger-phase-0-exit-review.md)

No credentials, patient identifiers, or full row dumps are included.

---

## 1. Volumes (FQ-05)

| Table | Row count |
|---|---|
| `tb_stok` | 11,280 |
| `tb_buku` | 4,689,901 |

| Metric | Value |
|---|---|
| `tb_stok` rows with `fn_qty = 0` | **0** (supports zero-row deletion) |
| Distinct Item+DO in `tb_buku` | 127,187 |
| Distinct Item+DO in `tb_stok` | 4,319 |
| Hottest Item+DO journal depth | ~4,509 `tb_buku` rows (16 locations) |
| Top hot scopes | Mostly FAR/ALK items on `DM*` receipt sources; one AJ-sourced scope in top 20 |

**Implication:** Full-table reconstruct-every-request is undesirable. Bounded per Item+Receipt Source replay of a few thousand journal rows is feasible for the hottest observed scopes.

---

## 2. Schema vs repository SQL (FQ-04)

### Present in snapshot beyond repo `tb_*.sql`

`tb_buku` / `tb_stok` include legacy audit columns (`CRTTGL`, `CRTJAM`, `CRTUSR`, …), plus `tb_buku.fs_closed`, `tb_buku.fb_gen_infstok`, and `tb_buku.fd_tgl_jam_mutasi`.  
`fs_jam_mutasi` on `tb_buku` is `varchar(10)` (repo script: `varchar(8)`).

### Indexes (live)

| Table | Index | Keys | Notes |
|---|---|---|---|
| `TB_BUKU` | `CX_tb_buku_fs_kd_trs` | `fs_kd_trs` | Clustered, **not unique**, **not PK** |
| `TB_BUKU` | `IX_tb_buku_fd_tgl_mutasi` | `fd_tgl_mutasi` | Date only — not `fd_tgl_jam_mutasi` |
| `TB_BUKU` | `IX_tb_buku_fs_kd_mutasi` | `fs_kd_mutasi` | FO transaction id |
| `TB_STOK` | `CX_tb_stok_fs_kd_trs` | `fs_kd_trs` | Unique clustered; `is_primary_key=0` |
| `TB_STOK` | `IX_tb_stok_fs_kd_brg_lyn` | `fs_kd_barang, fs_kd_layanan, fd_tgl_ed` | Availability-shaped |
| `TB_STOK` | `IX_tb_stok_fs_kd_layanan` | `fs_kd_layanan, fs_kd_barang, fs_kd_trs` | |

**Absent:** index on `(fs_kd_barang, fs_kd_do)`, index on `fd_tgl_jam_mutasi`, FK constraints, triggers on either table.

### Change features (FQ-07 input)

| Feature | Result |
|---|---|
| Database Change Tracking | Not evidenced as on (`IsChangeTrackingOn` null/empty; 0 CT tables for stock) |
| CDC on `tb_stok` / `tb_buku` | 0 |
| `FARIN_Stok` / `FARIN_StokLayer` / `FARIN_StokBuku` | Present, **0 rows** |

Related tables observed (not Stock Ledger authority): `TB_STOK_CLOSED`, `TB_BUKU_CLOSED`, `TB_REGEN_STOK`, `TB_REGEN_BUKU`, empty FARIN tables, plus unrelated `*_BUKU*` accounting names.

### Proposed additive indexes (evidence only — do not apply in Phase 0)

For Phase 1+ reconstruction/sync reads, candidates to validate with DBA:

1. `tb_buku (fs_kd_barang, fs_kd_do, fd_tgl_jam_mutasi, fs_kd_trs)` covering scoped history.
2. Optionally `tb_stok (fs_kd_barang, fs_kd_do, fs_kd_layanan)` for current-position reconciliation.

---

## 3. `fd_tgl_jam_mutasi` (FQ-01)

| Metric | Value |
|---|---|
| Total rows | 4,689,901 |
| Null/blank | 0 |
| Default sentinel `3000-01-01 00:00:00` | 0 |
| Parseable non-default | **4,689,901 (100%)** |
| Unparseable | 0 |
| Min / max | `2011-06-10 00:15:00` … `2026-05-09 13:51:28` |
| Matches `fd_tgl_mutasi` + `fs_jam_mutasi` | **100%** |

### Ties

Many timestamps share thousands of rows (e.g. `2020-12-31 23:59:59` → 12,471 rows / 12,155 distinct `fs_kd_trs`). Timestamp alone is not unique.

### ID vs time order (top 50 hottest Item+DO scopes)

| Metric | Value |
|---|---|
| Rows in hot scopes | 140,727 |
| Rows where `fs_kd_trs` order ≠ time order | **117,811 (~84%)** |

**Verdict:** Column is populated and usable as a **business time attribute** and lightweight change hint. It is **not** a safe standalone monotonic cursor. Composite `(fd_tgl_jam_mutasi, fs_kd_trs)` is also unsafe as a sole append watermark because void paths physically delete `tb_buku` rows and ID order frequently disagrees with time order.

---

## 4. Legacy IDs (FQ-02)

| Pattern | Count |
|---|---|
| Length 10 (`BK…`) | 4,685,301 |
| Length 3 (`SYS`) | 3,990 |
| Length 6 (`SYS-01`) | 610 |

| Prefix | Min sample | Max sample |
|---|---|---|
| `BK` | `BK00010115` | `BKX0012554` |
| `SY` | `SYS` | `SYS-01` |

Duplicate `fs_kd_trs` values exist (non-unique clustered index): synthetic `SYS` / `SYS-01` multiples, plus a small number of duplicated `BKX*` ids (count=2).

**Verdict:** IDs are useful **identities for surviving rows**, not proven globally monotonic or transactionally allocated across concurrent VB6 instances from this snapshot. Do not treat ID counters as a synchronization cursor.

---

## 5. Mutation-type distribution (writer evidence)

Top observed `fs_kd_jenis_mutasi` values:

| Jenis | Count | Notes |
|---|---|---|
| `DU` / `DU_V` | 2,849,745 / 433,426 | Dominant outbound + void-or-compensating |
| `PK` / `PK_V` | 647,935 / 56,817 | Usage |
| `MT_IN` / `MT_OUT` | 233,401 / 233,401 | Transfer legs balanced |
| `MT_IN_V` / `MT_OUT_V` | 1,192 / 1,172 | **Imbalance** (see anomalies) |
| `DO` / `DO_V` | 89,392 / 878 | Receipt |
| `AJ_*` / `AJX_*` | present | `AJX_*` not in current script constants |
| `RU`, `RB`, `RP_*`, `MN` | present | Lower volume |
| `DT`, `DT_V`, `RT`, `DU-R_*`, `DU-S*` | **0 rows** | Unused in this snapshot |

Blank jenis: 3,990 (aligns with `SYS` synthetic rows).

### Depletion signal

Scopes exist with balanced `tb_buku` in/out and **no** matching `tb_stok` row — consistent with delete-at-zero.

---

## 6. Anomalies corroborated in data

1. **MT void leg imbalance:** `MT_IN_V` (1192) ≠ `MT_OUT_V` (1172); sample FO mutasi ids show unequal void-leg counts.
2. **DT / DR / DS unused** in this hospital snapshot (no `DT*` mutasi prefix; no `DU-R*` / `DU-S*` jenis).
3. **`AJX_MIN` / `AJX_PLUS`** present → at least one stock writer path outside current `clbGenStokX1` constants.

---

## 7. What this snapshot cannot prove

- Live VB6 `BeginTrans` / isolation / lock timing (FQ-06).
- Every deployed caller of `Generate(..., xVoidDelete)` outside this repository extract.
- Whether `fd_tgl_jam_mutasi` is written by `SQLGenerator2`, a helper, or a backfill — `AddStok` in the extract lists date/time parts but not the combined column explicitly; snapshot values nonetheless match the parts 100%.
