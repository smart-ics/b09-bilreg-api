# Stock Ledger — Phase 0 Exit Review

**Review date:** 2026-08-07  
**Artifact status:** Governing Phase-0 conclusion — **frozen baseline** (PASS WITH RISKS)  
**Reviewer role:** Critical exit gate (no Phase 1 implementation)  
**Phase 1 plan:** [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md)

**Freeze note:** This Exit Review is the authoritative Phase 0 conclusion. Recommended documentation updates from §“Recommended document updates” are applied as of the Phase 0 closure pass. Architecture decisions remain LOCKED; residual risks convert to G-13 / G-17 / G-28 work, not a second Phase 0.

**Inputs reviewed:**

- [`stock-ledger-implementation-roadmap.md`](./stock-ledger-implementation-roadmap.md) Phase 0
- [`stock-ledger-feasibility-review.md`](./stock-ledger-feasibility-review.md) (FQ-01–FQ-07, D1–D12)
- [`stock-ledger-gap-analysis.md`](./stock-ledger-gap-analysis.md) (G-01–G-28)
- [`stock-ledger-phase-0-implementation-report.md`](./stock-ledger-phase-0-implementation-report.md)
- [`adr/ADR-stock-ledger-legacy-change-discovery.md`](./adr/ADR-stock-ledger-legacy-change-discovery.md)
- [`adr/ADR-stock-ledger-mixed-writer-concurrency.md`](./adr/ADR-stock-ledger-mixed-writer-concurrency.md)
- [`evidence/phase-0-profile-results.md`](./evidence/phase-0-profile-results.md)
- [`evidence/phase-0-writer-inventory.md`](./evidence/phase-0-writer-inventory.md)

**Not modified at review time:** application/infrastructure code, Cursor plan file.

**Closure pass (2026-08-07):** Documentation alignment + Phase 1 plan produced; no production code.

---

## Executive verdict

### **PASS WITH RISKS**

Phase 0 meets the **formal exit criteria** (FQ resolved or interim-assigned; sync + concurrency ADRs present; no authority-cutover language; evidence pack exists). It does **not** fully meet the roadmap’s Phase 0 **Testing / Validation** bar: no empirical change-kind detection experiment and no controlled VB6 concurrency sessions were executed.

That shortfall is acceptable for **Phase 1 scaffolding** (mechanism-neutral Synchronization Position, domain/persistence foundation) if the risks below are carried explicitly into G-13 / G-17 / Phase 3–9 gates. It is **not** acceptable to treat Phase 0 as having *proven* that the selected sync mechanism detects all change kinds.

---

## 1. FQ-01 … FQ-07 audit

| ID | Phase 0 claim | Exit-review classification | Evidence quality | Notes |
|---|---|---|---|---|
| **FQ-01** | Resolved | **Resolved by evidence** | Strong | 100% populated/parseable; matches parts; ties proven; reject sole watermark justified. Writer path for the combined column still unknown (does not overturn population finding). |
| **FQ-02** | “Resolved (limited)” | **Partial / intentionally deferred** (relabel) | Supported for identity shape; **unresolved** for multi-instance transactional allocation | Snapshot cannot prove concurrent VB6 counter safety. Recommend renaming away from “Resolved”. |
| **FQ-03** | Interim | **Intentionally deferred with justification** | Supported for script behavior; Weak for complete caller set | `xVoidDelete` physical delete proven in script; callers not enumerated; `AJX_*` proves incomplete writer inventory. Safe interim (“assume delete mode possible”) is correct. |
| **FQ-04** | Resolved | **Resolved by evidence** | Strong | Indexes/triggers/FKs/schema deltas documented; CT/CDC absent; FARIN empty. Repo SQL ≠ live schema noted. |
| **FQ-05** | Resolved | **Resolved by evidence** | Strong | Volumes + hottest scopes support bounded replay; reject org-wide reconstruct-every-request as steady state. |
| **FQ-06** | Unresolved + interim | **Explicitly unresolved** + safe interim | Strong for “unknown”; interim policy coherent | Correctly blocks Phase 9, not Phase 1 scaffolding. |
| **FQ-07** | Interim deferred | **Intentionally deferred with justification** | Supported | No CT/CDC; cannot invent VB6 change-log coverage. Fingerprint primary is consistent. |

**Formal exit criterion (FQ table):** Met.  
**Honesty gap:** FQ-02 should not be labeled “Resolved”.

---

## 2. ADR consistency review

### 2.1 Authority model — **Consistent**

Both ADRs and Phase 0 report keep Stage B authority on `tb_stok + tb_buku`, origin labels as non-authority, and no per-DO ownership. Aligns with feasibility D1–D2 and gap steering rules.

### 2.2 Synchronization — **Mostly consistent; one underspecification**

| Claim | Status |
|---|---|
| Reject `fs_kd_trs`-alone and watermark-alone | Consistent with FQ-01/02/03 evidence |
| Fingerprint + bounded delta/replay as primary | Consistent with feasibility §3.1 candidate |
| Mechanism-neutral Synchronization Position in Phase 1 | Consistent with roadmap Phase 1 |
| “Missing identities / fingerprint drift” detect deletes | **Underspecified** |

**Issue:** Physical `tb_buku` deletes leave no tombstone. A stored **hash-only** Synchronization Position can detect *staleness*, but cannot by itself list *which* journal identities disappeared. Recovering delete semantics requires one of:

1. Set-diff of **Ledger-known legacy source identities** vs surviving `tb_buku` for the scope, or  
2. Scoped re-derive / bounded replay from Ledger baseline + current authority snapshot, or  
3. An additive change log / CT / CDC (deferred).

The sync ADR implies (1)/(2) via “missing identities” language but does not make that algorithm normative. This is **not** an authority contradiction; it is a **Phase 3 design debt** that must not be mistaken for a finished G-13 acceptance.

### 2.3 Write aggregate / boundaries — **Consistent**

Concurrency ADR correctly separates:

- reconstruction/reconciliation = Item + Receipt Source × all locations  
- write consistency candidate = Item + Receipt Source + Location  
- locking = technical, smallest safe range  
- authority = global legacy  

Matches feasibility D3 and G-01.

### 2.4 Reconstruction / bootstrap — **Consistent with caveat**

Sync ADR: Phase 2 initializes Synchronization Position from fingerprint at baseline commit. Concurrency ADR: Phase C basis/fingerprint mismatch → retry / not current. Aligns with roadmap reconstruction TX-A/B/C.

**Caveat:** No Phase 0 experiment proved fingerprint stability across insert/update/stok-delete/buku-delete/backdate/repost. Bootstrap design is **chosen**, not **validated**.

### 2.5 Concurrency — **Internally consistent; production-proof deferred**

Interim policy (short TX, lock order, revalidate, conditional update, Ledger OCC) is coherent and correctly refuses to claim mixed-writer safety. `UPDLOCK/HOLDLOCK` kept as .NET candidate only — good.

Minor wording: “implement Phase 1–4 behind capability flags” is fine for scaffolding, but **production** DO Receipt (Phase 4) still depends on Phase 3 sync per roadmap — do not read the concurrency ADR as waiving G-12/G-13 for enabling Phase 4 in production.

### 2.6 Deletion detection — **Decision sound; proof incomplete**

Decision to require deletion-aware discovery is mandatory given `xVoidDelete`. Empirical “detects all tested change kinds” validation from roadmap Phase 0 Testing was **not executed**. Carry into G-13.

### 2.7 Soft contradictions / overclaims to fix in docs

| Item | Severity |
|---|---|
| Roadmap Phase 0 Validation text vs actual work (no detection experiment) | Medium — checklist overclaims |
| FQ-02 labeled “Resolved (limited)” | Low — naming |
| Sync ADR “missing identities” without normative set-diff/re-derive | Medium — Phase 3 ambiguity |
| Gap G-19 dependencies omit G-12/G-13 while roadmap Phase 4 requires Phase 3 | Medium — backlog inconsistency (pre-existing; Phase 0 made it sharper) |
| Gap G-25 “live indexes/volumes unknown” now stale | Low — update evidence field |

No ADR contradiction reintroduces authority cutover or `IsAuthoritative`.

---

## 3. Assumption register

| Assumption | Category | Rationale |
|---|---|---|
| `HOSPITAL_HPL` mirrors production schema + journal shape for stock tables | **Supported** | Explicit snapshot; not proven identical to every live site/binary |
| `clbGenStokX1.cls` extract is the operative FO stock generator | **Supported** | Only evidenced script; deployed binary drift possible |
| Zero `fn_qty` rows ⇒ legacy deletes depleted `tb_stok` | **Proven** (for this snapshot) | Count = 0 + balanced buku without stok samples |
| `xVoidDelete=True` physically deletes `tb_buku` | **Proven** (script) | `AddStok`/`RemoveStok` DELETE paths |
| Every void path may use delete mode in production | **Supported** (conservative) | Flag accepted on all void routes; callers unsigned |
| `fd_tgl_jam_mutasi` remains populated for future writes | **Supported** | Historical 100%; writer of combined column not shown in AddStok field list |
| Hottest scopes ~4–5k buku rows bound sync/replay cost | **Supported** | Measured top scopes; future growth unknown |
| Fingerprint + bounded replay is sufficient without change log | **Weak evidence** | Chosen by elimination + reasoning; **not** empirically tested for all change kinds |
| Bounded replay latency meets future SLO with proposed indexes | **Weak evidence** | Volumes known; no measured plan/SLO; indexes not applied |
| CT/CDC unavailable / not operationally usable now | **Supported** | Snapshot metadata; ops policy not fully surveyed |
| Empty FARIN tables are unused spikes, not a sync feed | **Supported** | 0 rows |
| DR/DS/DT/RT unused at this hospital | **Supported** | 0 matching jenis/prefixes in snapshot; may differ elsewhere |
| `AJX_*` is a real additional stock writer path | **Supported** | Present in data; owner/path unknown |
| MT void-leg imbalance is real historical anomaly to tolerate | **Proven** (counts) | 1192 vs 1172 + sample FO ids |
| DT void writes `DU_V` | **Proven** (script); unused locally | Line ~709 |
| VB6 stock RMW lacks reliable shared locking with .NET | **Guess** → treat as **Weak / unresolved** | Absence of evidence in script ≠ proof of unsafe production TX; FQ-06 correctly open |
| New-system interim lock order prevents negative stock vs VB6 | **Guess** | Explicitly unproven; must not be assumed until G-17 |
| Mechanism-neutral sync position can absorb fingerprint v1→vN | **Supported** | Design choice; low risk for Phase 1 |
| Ops will confirm FO/`AJX` before Phase 6/7 enables | **Guess** (process) | Required gate, not proven commitment |

---

## 4. Roadmap impact

| Phase | Change needed? | Recommendation |
|---|---|---|
| **0** | Annotate residual debt | Mark Testing/Validation items incomplete; keep status “Complete with residual risks” rather than unqualified complete |
| **1** | No structural change | Proceed; enforce mechanism-neutral position; ports for Legacy Change Discovery remain stubs/contracts |
| **2** | Minor clarify | Basis = fingerprint (or algorithm versioned basis); Phase C must re-hash authority snapshot |
| **3** | **Strengthen** | Normative deletion algorithm (set-diff vs scoped re-derive); add acceptance tests for all six mutation kinds; treat Phase 0 detection experiment debt as G-13 entry criteria |
| **4** | No reorder | Keep behind Phase 3 for any production enable; scaffolding OK earlier behind flags |
| **5** | Annotate | MT void-leg imbalance tolerance in sync/reconciliation tests |
| **6** | Optional de-prioritize for *this* hospital | DR/DS unused in snapshot — confirm ops before investing; do not remove from roadmap globally |
| **7** | Annotate | DT→`DU_V` mapping; `AJX_*` vocabulary; RT unused locally |
| **8–9** | Unchanged | FQ-06 / G-17 remain hard gates |

**Do not** insert a new “Phase 0.5 discovery” before Phase 1 **if** Phase 1 stays additive and mechanism-neutral. **Do** schedule a **G-13 characterization harness** early in Phase 3 (or a thin spike at end of Phase 1 tests) rather than another full Phase 0.

---

## 5. Gap catalog recommendations

| Action | Gap | Reason |
|---|---|---|
| **Update evidence** | **G-25** | Live indexes/volumes now known; replace “unknown” with Phase 0 results; keep SLO/index *approval* open |
| **Update evidence** | **G-13** | Mechanism selected (fingerprint + bounded replay); acceptance still requires detection experiments |
| **Update evidence** | **G-17** | Interim lock policy chosen; proof still open |
| **Update evidence** | **G-22** | Point at Phase 0 FO matrix; note DR/DS/DT/RT unused locally; DB/RJ unrouted |
| **Fix dependencies** | **G-19** | Add dependencies on **G-12, G-13, G-15** (and practically G-17 for production enable) to match roadmap Phase 4 |
| **Add (recommended)** | **G-28 — Hidden / alternate legacy writer vocabulary** | Cover `AJX_*`, synthetic `SYS*` buku rows, and any non-`clbGenStokX1` jenis; feeds G-13 discovery vocabulary and G-22 matrix. Priority P1 (before AJ enable / sync completeness claims) |
| **Add (optional merge alternative)** | Fold G-28 into G-13 + G-22 notes | Acceptable if owners prefer not to mint IDs; must not be lost |
| **Do not remove** | G-21 | DR/DS unused locally ≠ globally absent from script |
| **Do not reorder** | G-01–G-07 before G-10/G-13 | Phase 1 foundation still correct |
| **Clarify** | G-14 | Position shape = opaque + algorithm version; fingerprint is first concrete payload, not a watermark |

No gap should be removed solely because Phase 0 deferred VB6 concurrency — G-17 remains P0 for coexistence production.

---

## 6. Can Phase 1 begin without another discovery phase?

### **Yes — Go for Phase 1 scaffolding**, with conditions.

Phase 1 scope (G-01–G-07, G-18 foundation, ports, mechanism-neutral Synchronization Position) does **not** require resolving FQ-06, completing `xVoidDelete` caller signoff, or empirically proving fingerprint detection — provided Phase 1:

1. Does **not** hard-code `(fd_tgl_jam_mutasi, fs_kd_trs)` as Synchronization Position.  
2. Does **not** claim Freshness Gate / G-13 complete.  
3. Treats Legacy Change Discovery as a **port** with fingerprint-oriented contract notes from the ADR.  
4. Does **not** enable production stock consequences.  
5. Avoids restoring empty FARIN tables as authority or sync source.

**No-Go** would apply only if the team intended Phase 1 to implement real catch-up detection or production-ready mixed writes — that would illegally skip Phase 0 residual validation / Phase 3.

---

## Remaining risks (ordered)

1. **High — Sync delete semantics underspecified:** Hash drift ≠ recoverable delete identity without Ledger set-diff or scoped re-derive.  
2. **High — FQ-06 unproven:** VB6 vs .NET lost update / negative stock still possible until G-17.  
3. **Medium — Incomplete writer inventory:** `AJX_*` and unknown `xVoidDelete` callers can produce unmapped facts.  
4. **Medium — No empirical change-detection experiment:** G-13 acceptance not pre-validated.  
5. **Medium — Missing index on `(barang, do)`:** Reconstruction/sync scans may miss SLO without DBA work (G-25).  
6. **Low — Schema drift:** Live columns differ from repo SQL; adapters must target live shape.  
7. **Low — Single-hospital snapshot:** Other deployments may use DR/DS/DT/RT heavily.  
8. **Low — FQ-02 labeling / roadmap overclaim:** Process risk of false confidence.

---

## Recommended document updates (no code)

**Closure status (2026-08-07):** Applied during Phase 0 closure / baseline freeze. Phase 1 plan: [`stock-ledger-phase1-implementation-plan.md`](./stock-ledger-phase1-implementation-plan.md).

1. Phase 0 report: relabel FQ-02; add “Residual Phase 0 testing debt” section; soften any implication that sync detection is proven. — **Done**
2. Sync ADR: add normative note — on fingerprint mismatch, discovery MUST set-diff Ledger-known legacy identities against surviving `tb_buku` and/or perform scoped re-derive; hash alone is a freshness trigger. — **Done**
3. Roadmap Phase 0 checklist: mark detection experiment and VB6 concurrency as residual; keep Phase 1 Go explicit. — **Done**
4. Gap analysis: refresh G-13/G-17/G-22/G-25 evidence; fix G-19 dependencies; add G-28 (or equivalent notes). — **Done**
5. Feasibility §3.4: keep Phase 0 pointer; align FQ-02 wording with “partial”. — **Done**
6. ARTIFACTS.md: index this Exit Review when filed. — **Done** (plus Phase 1 plan)

---

## Go / No-Go for Phase 1

| Decision | Scope |
|---|---|
| **GO** | Phase 1 additive foundation: domain Movement/Layer, coexistence state, mechanism-neutral Synchronization Position, repositories, UoW skeleton, discovery **ports**, tests for domain invariants — disposable DB only |
| **NO-GO** | Implementing production catch-up, claiming G-13/G-17 done, enabling FO capability flags, or treating Phase 0 as mixed-writer proof |
| **HOLD (ops)** | Phase 6 DR/DS and Phase 7 DT/RT/AJX until owner confirmation |

### Final recommendation

**PASS WITH RISKS → Phase 1 may start under the GO conditions above.**  
Do **not** open a second full Phase 0. Convert residual discovery into **G-13/G-17/G-28 acceptance work** inside Phases 1–3 as specified.
