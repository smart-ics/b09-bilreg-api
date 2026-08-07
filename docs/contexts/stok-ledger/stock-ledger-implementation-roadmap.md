# Stock Ledger — Phased Implementation Roadmap

**Artifact status:** Executable implementation roadmap  
**Basis:** Actual codebase state (August 2026), not aspirational docs alone  
**Inputs:** [`stock-ledger-feasibility-review.md`](./stock-ledger-feasibility-review.md), [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md), [`stok-ledger-domain.md`](./stok-ledger-domain.md), [`clbGenStokX1.cls`](./clbGenStokX1.cls)  
**Standards:** [`docs/ENGINEERING.md`](../../ENGINEERING.md), [`docs/DATABASE.md`](../../DATABASE.md), [`docs/NAMING.md`](../../NAMING.md)

---

## Guiding constraints (do not violate)

1. **Do not port** `clbGenStokX1` Transaction Script into Domain/Application. Extract rules; implement as DDD.
2. **Do not** make Stock Ledger delete depleted layers. Legacy projection may still delete `tb_stok` rows at qty 0.
3. **Do not** require mass replay of 20-year history. Lazy bootstrap per `Barang + DO` only.
4. **Do not** extend `StokModel(BrgId, LayananId)` as the Stock Position aggregate root.
5. Prefer **one FO consequence type end-to-end** before broadening coverage.
6. Keep VB6 / legacy writers authoritative for types not yet dual-written.

---

## Already implemented (start from here)

| Asset | Keep / Refactor / Replace |
|---|---|
| Domain docs bilingual | **Keep** as business authority |
| `StokLayerModel` / `StokBukuType` / `StokLotType` / `TrsReffType` ideas | **Refactor** into Position/Movement-aligned types |
| Depleted-layer retention in memory | **Keep** |
| `JenisLokasi` + `Layanan` persistence | **Keep**; extend later for Virtual |
| `tb_stok_dal` / `tb_buku_dal` | **Refactor** queries; use as legacy adapters |
| FARIN SQL files | **Refactor** schema before re-wiring |
| `StokModelTest` FEFO assertions | **Replace** with ED+FIFO policy tests |
| Commented `StokRepoTest` reconstruction | **Replace** with correct Barang+DO scope tests |
| Deleted FARIN DAL/Repo in working tree | **Replace** deliberately (do not blindly restore wrong design) |
| VB6 `Generate` | **Keep running** externally until dual-write coverage exists |

---

## Phase 0 — Alignment & freeze wrong direction

### Objective
Stop further investment in the location-scoped kartu-stok AR as Stock Ledger authority; document decisions engineers must not reinvent.

### Scope
Documentation + ADR-style decisions inside this folder; no production feature work.

### Implementation tasks
1. Treat this roadmap + gap analysis as mandatory reading for StokFeature work.
2. Record explicit decisions:
   * Position key = `BrgId + ReceiveId (DO)` across locations.
   * Allocation = Explicit ED (optional) then FIFO by Effective Receipt Time + LayerId.
   * Batch: either filter when supplied or ignore with documented reason (legacy ignored batch — decide in ADR).
   * Migration status table columns (minimum set below).
3. Inventory working-tree deletions; decide restore-vs-rewrite for FARIN Infra (recommend **rewrite** to new aggregates).

### Validation criteria
* Engineers can answer: what is AR, what is Receipt Source, what must not be ported from VB6.

### Exit criteria
* Phase 0 checklist signed off (team/PR review of docs).
* No new PR merges that add write logic onto `StokModel` as Position AR.

### Risks
* Team continues FEFO “because tests say so” → **Critical** if ignored.

### Minimum migration status fields (decision locked here)

```text
StockLedgerMigration
- BrgId
- ReceiveId              -- KodeDO / Receipt Source
- BootstrapVersion
- Status                 -- NotReconstructed | Reconstructing | Reconstructed | Inconsistent
- IsValidated
- LastLegacyBookCursor   -- NOT raw fs_kd_trs alone; use (fd_tgl_jam_mutasi, fs_kd_trs) or hash watermark
- BootstrapStartedAt
- BootstrapCompletedAt
- InconsistencyReason    -- nullable
```

---

## Phase 1 — Domain realignment (no FO cutover)

### Objective
Implement domain types and policies that match `stok-ledger-domain.md` for Position, Layer, Movement, Allocation, Reconstruction status.

### Scope
`Bilreg.Domain` (+ unit tests). Application interfaces sketched. No requirement to finish Infra.

### Implementation tasks
1. **G-01/G-02/G-03/G-18:** Introduce:
   * `StockPosition` (Item + Receipt Source)
   * `StockLayer` (location, ED, batch, HPP, initial/remaining, forming movement ref, origin Native|Reconstructed)
   * `StockMovement` + `StockMovementLine`
   * `FifoAllocationService` (or equivalent domain service)
   * `LegacyStockReconstruction` status model
2. Mark obsolete path: `StokModel.AddStok/RemoveStok` either deleted or wrapped as temporary adapter with obsolete attributes — **do not** keep FEFO as public policy.
3. Unit tests replacing `StokModelTest` FEFO cases with domain policy cases (legacy narrative from user + VB6 ED+FIFO).
4. Sketch Application ports only (`IStockPositionRepo`, `IStockMovementRepo`, `IStockLedgerMigrationStore`, `ILegacyStockReadAdapter`).

### Validation criteria
* Domain tests cover: multi-location layers per DO; ED filter; FIFO order; retain qty 0; transfer conservation; reject negative.
* No SQL references in Domain.

### Exit criteria
* Domain project builds with new types.
* Gap G-01/G-02/G-03 marked done in PR description referencing this roadmap.

### Risks
* Over-modeling Movement lifecycle before first use case — mitigate by implementing only states needed for Recorded + Reversed first.

---

## Phase 2 — Persistence foundation

### Objective
Persist Position/Layer/Movement/Migration with correct keys and concurrency.

### Scope
`Bilreg.SqlDb` + `Bilreg.Infrastructure` + DAL/repo tests.

### Implementation tasks
1. **G-06/G-07/G-04:** Design/migrate tables (names per `docs/NAMING.md` / existing FARIN conventions):
   * Layers (keep zero)
   * Movements + lines
   * Migration status
   * Optional header balance projection by location **as read model**, not AR
2. Indexes:
   * Layer: `(BrgId, ReceiveId)`, `(BrgId, LayananId, ExpDate, EffectiveReceiptTime, StokLayerId)`
   * Movement: unique source consequence key
   * Legacy: `(fs_kd_barang, fs_kd_do)` on `tb_buku` / `tb_stok` if missing
3. **G-05:** Fix/implement legacy list-by-Barang+DO across locations.
4. Repositories implement ports from Phase 1.
5. Optimistic concurrency on layer Remaining Qty (version column).

### Validation criteria
* Round-trip tests: save reconstructed DO with a zero location layer; reload identical qty map.
* Unique constraint blocks duplicate source consequence.

### Exit criteria
* Infra tests green in CI for new tables.
* Old `IStokRepo` either removed, narrowed to read-model, or clearly obsolete.

### Risks
* Blind restore of deleted `StokRepo` reintroduces wrong Brg+Layanan AR — **High**; rewrite instead.

---

## Phase 3 — Lazy reconstruction (bootstrap)

### Objective
Make first touch of `Barang + DO` establish Stock Ledger authority without mass migration.

### Scope
Application use case + domain reconstruction rules + legacy read adapter.

### Implementation tasks
1. **G-08:** `ReconstructStockFromLegacy` (internal) invoked before native consequence when status ≠ Reconstructed.
2. Algorithm (normative):
   1. Begin tx; set status `Reconstructing` (fail if already Reconstructing by other worker — wait/retry/reject).
   2. Load all `tb_buku` for Brg+DO (all layanan), ordered by `(fd_tgl_jam_mutasi, fs_kd_trs)`.
   3. Derive per-location remaining qty and layer splits using available ED/batch/HPP/mutasi facts; assign **new** layer ids (BR-STL-066/067).
   4. Cross-check surviving `tb_stok` rows; if conflict that changes qty/provenance → `Inconsistent` (BR-STL-073/103).
   5. Persist layers (including zeros that ever existed in buku net) + mark `Reconstructed` + `IsValidated`.
3. Never delete legacy rows as part of bootstrap success path except via separate explicit legacy-repair tooling.
4. Tests from the Amoxan narrative (DO-001 across Gudang/Apotek/IGD) and at least one inconsistent fixture.

### Validation criteria
* Bootstrap idempotent.
* Concurrent two requests → one reconstruction, no duplicate layers.
* Inconsistent DO blocks native write with clear error.

### Exit criteria
* G-04/G-05/G-08 done.
* Runbook note: how ops resolves `Inconsistent`.

### Risks
* Hot DO timeout — **High**; set command timeout + metrics; allow admin re-run after index fix.
* Ambiguous ordering — apply BR-STL-104/105.

---

## Phase 4 — First end-to-end consequence + dual-write

### Objective
Prove coexistence: one real FO type writes Stock Ledger authoritatively and projects legacy tables.

### Recommended first slice
**Stock Transfer (MT)** — exercises OUT+IN, provenance retention, dual locations, and maps cleanly from VB6 `GenStokMutasi`.

Alternative if transfer coupling is hard: **DO Receipt** (inbound only) as slice 4a, then MT as 4b.

### Scope
Application command(s), legacy projection, API or internal handler invoked from an orchestration point (may initially be a facaded service called beside FO post).

### Implementation tasks
1. **G-09/G-10/G-11 (subset):**
   * Ensure bootstrap (Phase 3) runs for each affected DO on the transfer lines.
   * Allocate OUT with ED+FIFO.
   * Record Movement(s); update Position layers; create destination layers.
   * Project `tb_buku` OUT/IN; update/delete `tb_stok` with **legacy** semantics.
2. Idempotency on MT code + consequence type.
3. Integration test: compare qty map Ledger vs projected legacy for happy path.
4. Feature flag: `StockLedger:DualWrite:Mutasi=true`.

### Validation criteria
* With flag on: MT path does not need VB6 for that transaction in the test environment.
* Legacy consumers reading `tb_stok` still see expected qty (including absences at zero).
* Ledger still has zero layers where applicable.
* Reconciliation (can be a test helper in this phase) balanced for DO.

### Exit criteria
* One FO type production-pilot capable behind flag.
* Rollback plan: disable flag → VB6 remains writer (Ledger may lag — document).

### Risks
* Dual path both VB6 and new writer enabled → double stock — **Critical**; flag must be mutually exclusive per type.

---

## Phase 5 — Reconciliation + read models

### Objective
Operational trust for bootstrapped DO without 20-year global replay.

### Scope
Reconciliation aggregate/use case; kartu/balance queries; repair tooling hooks.

### Implementation tasks
1. **G-12/G-16:** Reconcile per Brg+DO; persist Balanced / Difference Identified.
2. Replace `StokGetKartuStokQuery` with Ledger-backed read models.
3. Admin query: migration status, last bootstrap, inconsistency reason.

### Validation criteria
* Recon for bootstrapped DO completes using Ledger facts only (BR-STL-111).
* Difference Identified does not rewrite history.

### Exit criteria
* Controllers work; recon report usable by Inventory Controller role.

### Risks
* Ops treats Legacy Projection as truth after authority established — training/docs (**Medium**).

---

## Phase 6 — Expand consequence coverage

### Objective
Grow FO coverage in dependency order while VB6 remains for the rest.

### Suggested order
1. DO Receipt  
2. Mutasi (if not done in Phase 4)  
3. Sale umum (DU) + void as reversal  
4. Retur jual umum (restore layer)  
5. Pakai / Musnah  
6. Retur beli  
7. Adjust (only after opname authority clarified)  
8. Reserved (DR) + Serah (DS) — may wait for Phase 7 virtual locations  
9. Repack  
10. Sale tipe / other variants  

### Implementation tasks
* For each type: map VB6 behavior → Preserve/Replace/Migrate/Remove (feasibility §4) → consequence handler → tests → flag.
* Maintain coverage matrix in this folder (`stock-ledger-fo-coverage.md` when started).

### Validation criteria
* Each enabled type has dual-write tests and recon samples.
* Unmapped types still exclusively VB6.

### Exit criteria
* Coverage matrix shows production-enabled types; no silent fallbacks.

### Risks
* Pressure to “enable all flags” before reserved/serah modeled — **High**.

---

## Phase 7 — Virtual locations, events, harden

### Objective
Complete reservation model and integration surface; operational hardening.

### Scope
**G-14/G-15/G-19/G-20**

### Implementation tasks
1. Virtual Stock Location strategy (dedicated layanan ids vs `JenisLokasi` Virtual).
2. Map DR/DS to reserve/consume/release.
3. Domain events for movement/reconstruction/recon.
4. Rename Charge stock list contract; restore if tarif needs it.
5. Bootstrap/recon SLOs, indexes, dashboards.

### Validation criteria
* Reserved qty invisible to ordinary-location allocation.
* Events observable in existing project mechanisms.

### Exit criteria
* Domain §3.6 and §3.9 capabilities met for enabled FO set.
* Feasibility risks R1–R8 mitigated or accepted with owners.

### Risks
* Virtual location identity colliding with real `ta_layanan` — design carefully.

---

## Phase summary table

| Phase | Objective | Primary gaps | Defer |
|---|---|---|---|
| 0 | Freeze wrong AR / lock decisions | Docs | Coding features |
| 1 | Domain realignment | G-01 G-02 G-03 G-18 | Infra, FO |
| 2 | Persistence | G-04 G-05 G-06 G-07 G-17 | Dual-write |
| 3 | Lazy bootstrap | G-08 G-20 (indexes) | FO cutover |
| 4 | First consequence + dual-write | G-09 G-10 G-11 | Full FO catalog |
| 5 | Recon + reads | G-12 G-16 | Virtual loc |
| 6 | Expand FO | G-13 | Events polish |
| 7 | Virtual + events + harden | G-14 G-15 G-19 G-20 | Mass migration (never) |

---

## Engineer checklist (per PR)

- [ ] References gap IDs touched
- [ ] Does not port VB6 control flow
- [ ] Does not delete Ledger depleted layers
- [ ] Dual-write flags mutually exclusive with VB6 for same FO type
- [ ] Tests updated for allocation policy (not FEFO-by-default)
- [ ] Migration/recon concurrency considered if touching bootstrap
- [ ] Updates FO coverage matrix when enabling a type

---

## Assumption log

| Assumption | Evidence status |
|---|---|
| VB6 `clbGenStokX1` is still the production stock poster for FO | Present in docs as legacy reference; **not verified** against deployment topology in this review — confirm with ops |
| FARIN tables may be empty / unused in production today | Schema in repo; runtime wiring absent — **assume unused** until DBA confirms |
| Working-tree deletion of FARIN DAL is intentional WIP | Git status deleted; treat as unclean slate for Phase 2 rewrite |
| Batch selection should become real when FO supplies batch | Legacy ignores batch — **decision required** in Phase 0 ADR |

When evidence is missing, do not invent production behavior; confirm with Inventory/ops before Phase 4 pilot.
)
