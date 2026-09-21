# IGD Visit Artifact-to-Code Gap Analysis Report

**Audit date:** 2026-05-26  
**Artifacts compared:** `igd-01-context.md`, `igd-02-domain.md`, `igd-03-design.md`, `igd-04-api-contract.md`, `igd-05-runbook.md`  
**Implementation scope:** `Bilreg.Domain/IgdContext`, `Bilreg.Application/IgdContext`, `Bilreg.Infrastructure/IgdContext`, `Bilreg.Api/Controllers/IgdContext`, `Bilreg.SqlDb/IgdContext`, `Bilreg.Test/IgdContext`

---

## Executive Summary

| Metric | Assessment |
|--------|------------|
| **Overall alignment score** | **~78%** — core IGD Visit lifecycle is implemented end-to-end with Clean Architecture layering, matching routes, SQL tables, and most domain gates (DR-05–DR-09) on `IgdVisitModel`. |
| **Critical risks** | Terminal-state bypass on **Discharge** / **Void** for `REDIRECTED` visits; **API date format drift** on Daftar; **void compliance audit** written outside the same DB transaction as visit/bed writes. |
| **Missing implementations** | Bed lifecycle use-cases/API (`MarkClean`, `MarkMaintenance`); DR-01 triage role enforcement; optional ESI/CTAS/MTS engines (documented as future). |
| **Documentation drift** | `igd-04-api-contract.md` states `TglLahirYmd` as `yyyyMMdd` while code/tests use `yyyy-MM-dd`; runbook bed-cleaning references application use-cases that are domain-only today. |

**Summary:** The feature is **operationally usable** for the standard clinical-first flow (daftar → dokter → triage → bed/redirect → tindakan/BHP → register → discharge) with reconciliation support (`GET /api/BedIgd/pakaiBedIgd/orphan`). Gaps concentrate on **terminal-state integrity**, **contract accuracy**, **persistence semantics for append-only triage**, and **test coverage** for handlers and triage engine.

---

## Alignment Matrix

| Artifact Area | Implementation Status | Risk | Notes |
|---------------|----------------------|------|-------|
| 01-context — operational flow | **Aligned** | Low | All main steps have use-cases/controllers. |
| 01-context — clinical flow first | **Aligned** | Low | Triage/bed/tindakan before `RegId` supported. |
| 01-context — user roles | **Partial** | Medium | DR-01 (triage by dokter jaga) not enforced in assess handlers. |
| 02-domain — aggregates | **Aligned** | Low | `IgdVisitModel`, `BedIgdModel`, transaction entities present. |
| 02-domain — DR-05 triage before bed | **Aligned** | Low | `IgdVisitModel.AssignBed` + model test. |
| 02-domain — DR-06 bed occupancy | **Aligned** | Low | `BedIgdModel.Occupy`, `UQ_BILRG_BedIgd_VisitActive`, concurrency test. |
| 02-domain — DR-07 redirect constraint | **Aligned** | Low | Domain + model test. |
| 02-domain — DR-08 discharge gate | **Mostly aligned** | Medium | Cascade release implemented; terminal `REDIRECTED` not blocked (GAP-002). |
| 02-domain — DR-09 void gate | **Mostly aligned** | Medium | Repo checks tindakan/BHP; `REDIRECTED` not blocked (GAP-002). |
| 02-domain — DR-02 append-only triage | **Partial** | High | Domain appends; repo **delete-all + re-insert** on save (GAP-006). |
| 02-domain — DR-03 Black override | **Partial** | Medium | Color forced Black; level still ATS-scored; no override reason gate (GAP-004). |
| 02-domain — DR-04 re-assessment intervals | **Aligned** | Low | `AtsTriageEngine` + `ToReAssessmentInterval`. |
| 02-domain — DR-10 billing | **N/A (by design)** | Low | Out of process per artifacts. |
| 03-design — layering | **Aligned** | Low | Api → Application → Domain; Infra repos/DAL. |
| 03-design — `TransHelper.NewScope` | **Mostly aligned** | Medium | Void audit log outside scope (GAP-005). |
| 03-design — bed dual-write | **Aligned** | Low | Assign/checkout/discharge/void handlers coordinate 3 aggregates. |
| 03-design — concurrency | **Aligned** | Low | `BedIgdRepo` conditional update + integration test. |
| 04-api-contract — routes | **Aligned** | Low | All documented routes exist on controllers. |
| 04-api-contract — request shapes | **Partial** | **High** | `TglLahirYmd` format mismatch (GAP-001). |
| 04-api-contract — responses | **Mostly aligned** | Low | Empty bed may appear as `"-"` not empty string (GAP-013). |
| 05-runbook — orphan sweep | **Aligned** | Low | `PakaiBedIgdDal.ListOrphans` matches runbook reasons. |
| 05-runbook — bed Dirty → Active | **Missing API** | Medium | Domain `MarkClean` only; no handler/route (GAP-007). |
| Persistence / SQL | **Aligned** | Low | Tables, PKs, filtered unique index, sentinel dates present. |
| Tests — domain invariants | **Good** | Low | `IgdVisitModelTest` covers DR-05,07,08,09. |
| Tests — handlers / engine | **Gaps** | Medium | No `AtsTriageEngine` tests; limited handler coverage (GAP-010–011). |

---

## Detailed Findings

### GAP-001 — Daftar `TglLahirYmd` format: contract vs implementation

#### Artifact Reference
- `docs/contexts/igd/igd-04-api-contract.md` — Daftar body: `TglLahirYmd` (`yyyyMMdd`)
- `docs/contexts/igd/igd-01-context.md` — Visitor identity at registration

#### Actual Implementation
- `Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitDaftarCmd.cs` — `DateOnly.ParseExact(request.TglLahirYmd, "yyyy-MM-dd", ...)`
- `Bilreg.Test/IgdContext/IgdVisitFeature/IgdVisitDaftarHandlerTest.cs` — uses `"1990-05-15"`

#### Gap Description
Clients following `igd-04-api-contract.md` with `yyyyMMdd` (e.g. `19900515`) will receive `FormatException`. Tests and handler implement **ISO date** `yyyy-MM-dd`, not compact `yyyyMMdd`.

#### Risk
**High** — front-end integration failure at first API call.

#### Recommendation
Either update `igd-04-api-contract.md` to `yyyy-MM-dd` (if intentional) or change handler to accept `yyyyMMdd` (and add contract test). Prefer single format with explicit validation message.

---

### GAP-002 — Discharge and Void do not honor `REDIRECTED` terminal state

#### Artifact Reference
- `docs/contexts/igd/igd-02-domain.md` — `IsTerminal` includes `Redirected`; state diagram: `REDIRECTED` → terminal
- `docs/contexts/igd/igd-02-domain.md` — UC09 Discharge, UC10 VoidVisit

#### Actual Implementation
- `Bilreg.Domain/IgdContext/IgdVisitFeature/IgdVisitModel.cs` — `Discharge()` checks `IsDischarged` and `HasReg` only; does **not** check `IsRedirected` / `IsTerminal`
- `IgdVisitModel.Void()` — blocks `IsDischarged` only, not `IsRedirected`
- Other mutations (`AssignDokter`, `AssignBed`, `AssignRegister`, `RecordTindakanEvent`, etc.) correctly use `IsTerminal`

#### Gap Description
A visit in `AdministrativeState = REDIRECTED` can still be **voided** (if no tindakan/BHP) or **discharged** (if `RegId` was linked earlier), contradicting domain terminal semantics and runbook path “Redirect → terminal”.

#### Risk
**Critical** — invalid administrative state transitions and billing/ops confusion.

#### Recommendation
In `Discharge()` and `Void()`, reject when `IsRedirected` (or use `IsTerminal` excluding already-handled void/discharge idempotency). Add model tests: `Discharge_WhenRedirected_Throws`, `Void_WhenRedirected_Throws`.

---

### GAP-003 — DR-01: Triage “oleh dokter jaga” not enforced

#### Artifact Reference
- `docs/contexts/igd/igd-02-domain.md` — DR-01
- `docs/contexts/igd/igd-04-api-contract.md` — Assign dokter gate references DR-01 context only on dokter endpoint

#### Actual Implementation
- `IgdVisitAssessTriageHandler` / `IgdVisitReAssessTriageHandler` — accept any `UserId`; no check that visit has assigned dokter or that assessor is dokter PPA
- `IgdVisitAssignDokterHandler` — validates dokter via `PpaRepo` and `IsDokter()`

#### Gap Description
Perawat/admin can record triage before dokter assignment; assessor is not validated as dokter jaga. Artifact allows input by nurse/admin but states clinical triage is doctor responsibility — **not encoded**.

#### Risk
**Medium** — medico-legal / workflow non-compliance.

#### Recommendation
Define explicit rule: (a) require `visit.Dokter` assigned before triage, and/or (b) validate `UserId` maps to dokter PPA for triage commands. Document chosen rule in `igd-02-domain.md` and enforce in assess handlers.

---

### GAP-004 — DR-03: Manual Black override incomplete

#### Artifact Reference
- `docs/contexts/igd/igd-02-domain.md` — DR-03 (Black manual dokter only)
- `docs/contexts/igd/igd-04-api-contract.md` — `IsManualOverrideBlack`, `OverrideReason`

#### Actual Implementation
- `AtsTriageEngine.Calculate` — ignores `IsManualOverrideBlack`; always computes ATS level/color
- `IgdVisitModel.AssessTriage(...)` — sets `Color = Black` when flag true but **level remains engine output**
- No guard requiring non-empty `OverrideReason` when `IsManualOverrideBlack`

#### Gap Description
Black is a **color override only**; level stays ATS1–5. Override reason optional. May be acceptable clinically but diverges from strict reading of “Black hanya manual override dokter” as a distinct triage category.

#### Risk
**Medium** — reporting/monitoring treats Black visits as ATS levels; audit trail may lack mandatory override justification.

#### Recommendation
When `IsManualOverrideBlack`, require `OverrideReason`, set level to a documented sentinel or `Unknown` with Black color, and skip ATS interval or set continuous monitoring per policy. Add engine/handler tests.

---

### GAP-005 — Void compliance `AuditLog` saved outside `TransHelper.NewScope`

#### Artifact Reference
- `docs/contexts/igd/igd-03-design.md` — Void: bed release + visit + compliance `AuditLog`
- `docs/contexts/igd/igd-04-api-contract.md` — Void captures IP/User-Agent

#### Actual Implementation
- `Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitVoidCmd.cs` — visit/bed/pakai saved inside `using (var trans = TransHelper.NewScope())`; `_auditRepo.SaveChanges(auditLog)` **after** scope completes

#### Gap Description
Visit void can commit while compliance audit insert fails (or vice versa if ordering inverted), breaking atomic void operation described in design.

#### Risk
**High** — audit gap on voided visits.

#### Recommendation
Include `_auditRepo.SaveChanges` inside the same `TransHelper.NewScope` as visit/bed/pakai, or use shared unit-of-work. Add integration test asserting rollback on audit failure.

---

### GAP-006 — Triage/event persistence: delete-all then re-insert (append-only drift)

#### Artifact Reference
- `docs/contexts/igd/igd-02-domain.md` — DR-02, medico-legal append-only triage
- `docs/contexts/igd/igd-03-design.md` — append-only triage/event inserts

#### Actual Implementation
- `Bilreg.Infrastructure/IgdContext/IgdVisitFeature/IgdVisitRepo.cs` — `SaveChanges`: `_triageDal.Delete(model)` then re-`Insert` all rows; same for events
- `IIgdVisitTriageDal` — no `Update`; only `Insert`/`Delete`

#### Gap Description
Domain appends in memory, but persistence **rewrites** full triage/event sets. Under transaction this is usually consistent, but (1) not true append-only at DB layer, (2) concurrent writers could lose rows, (3) failure after `Delete` loses history.

#### Risk
**High** — DR-02 / audit integrity under partial failure or concurrency.

#### Recommendation
Persist only **new** triage/event rows (track `NoTriage`/`NoEvent` high-water mark). Remove bulk delete on routine saves. Add test: save failure after append does not delete existing triage rows.

---

### GAP-007 — Bed Dirty → Active maintenance: runbook vs missing application surface

#### Artifact Reference
- `docs/contexts/igd/igd-05-runbook.md` — “Occupied → Dirty → cleaning → Active (via application bed maintenance use-cases when used)”
- `docs/contexts/igd/igd-03-design.md` — future `MarkClean`, `MarkMaintenance`

#### Actual Implementation
- `BedIgdModel.MarkClean` / `MarkMaintenance` — domain only
- `BedIgdController` — only `available` and `pakaiBedIgd/orphan`; **no** maintenance endpoints
- `Bilreg.Test/IgdContext/BedIgdFeature/BedIgdModelTest.cs` — domain tests only

#### Gap Description
After checkout/discharge, beds become `Dirty` but operators cannot return them to `Active` via API without direct SQL (runbook forbids mutating `BedIgd` manually except incident).

#### Risk
**Medium** — operational dead-end: available bed list shrinks until manual DBA intervention.

#### Recommendation
Add `POST /api/BedIgd/{id}/markClean` (and optional maintenance) use-case with `TransHelper.NewScope`, document in `igd-04-api-contract.md`, smoke in runbook rollout.

---

### GAP-008 — `IgdAssignBedHandler` does not pre-check `bed.IsAvailable` explicitly

#### Artifact Reference
- `docs/contexts/igd/igd-03-design.md` — “validasi `bed.IsAvailable` dan visit belum observed sebelum `Occupy`”

#### Actual Implementation
- `IgdAssignBedHandler` — calls `bed.Occupy` directly
- `BedIgdModel.Occupy` — enforces `BedState == Active` and not occupied (equivalent to `IsAvailable`)

#### Gap Description
Behavior is **functionally equivalent** but design documents explicit handler-level check. No explicit `visit.IsTerminal` before load (domain throws on `AssignBed`).

#### Risk
**Low** — documentation/implementation nuance only.

#### Recommendation
Optional explicit `if (!bed.IsAvailable) throw ...` for clearer API errors; add handler unit test.

---

### GAP-009 — Assign bed handler: no dedicated tests (DR-05 at application layer)

#### Artifact Reference
- `docs/contexts/igd/igd-03-design.md` — Testing strategy: assign bed without triage fails
- `docs/contexts/igd/igd-02-domain.md` — DR-05

#### Actual Implementation
- `IgdVisitModelTest.AssignBed_BeforeTriage_Throws` — domain only
- **No** `IgdAssignBedHandler` test
- **No** integration test for assign-bed transaction

#### Gap Description
Regression risk if handler ordering or transaction boundaries change.

#### Risk
**Medium**

#### Recommendation
Add `IgdAssignBedHandlerTest` (mock repos): fails without triage, succeeds with triage + saves three repos in one scope.

---

### GAP-010 — No unit tests for `AtsTriageEngine` (DR-04)

#### Artifact Reference
- `docs/contexts/igd/igd-02-domain.md` — DR-04 intervals ATS1 continuous, ATS2 15m, …
- `docs/contexts/igd/igd-03-design.md` — TriageEngine / `AtsTriageEngine`

#### Actual Implementation
- `Bilreg.Application/IgdContext/IgdVisitFeature/TriageEngine/TriageEngine.cs` — scoring and intervals
- **Zero** test classes reference `AtsTriageEngine`

#### Gap Description
Score-to-level mapping and `NextReTriageAt` logic unverified by automated tests.

#### Risk
**Medium** — clinical SLA dashboard (`triage-monitoring`) depends on correct intervals.

#### Recommendation
Add `AtsTriageEngineTest` with boundary cases per ATS level and GCS branches; assert `NextReTriageAt` null for ATS1.

---

### GAP-011 — Missing handler/integration tests (checklist from audit brief)

#### Artifact Reference
- Audit dimensions §7; `igd-03-design.md` testing strategy

#### Actual Implementation (present)
| Test area | File |
|-----------|------|
| DR-05 domain | `IgdVisitModelTest` |
| DR-08 discharge | `IgdVisitDischargeHandlerTest` (incl. cascade) |
| DR-09 void | `IgdVisitVoidHandlerTest` |
| Concurrency | `BedIgdRepoConcurrencyTest` |
| DAL | `IgdVisitDalTest`, `PakaiBedIgdDalTest`, `IgdVisitTriageDalTest` |

#### Missing (evidence: no test files)
- `IgdVisitRedirectRawatJalanHandler` / DR-07 at handler layer
- `IgdCheckOutBedHandler`
- `IgdVisitAssessTriageHandler` / re-assess
- `IgdVisitAssignRegisterHandler`
- `PakaiBedIgdListOrphanQuery` / orphan SQL reasons
- Transaction rollback on multi-aggregate failure
- Redirect constraint integration

#### Risk
**Medium**

#### Recommendation
Prioritize handler tests for redirect, assign bed, and orphan query; one integration test per `TransHelper` use-case listed in `igd-03-design.md`.

---

### GAP-012 — Orphan reconciliation: no automated test coverage

#### Artifact Reference
- `docs/contexts/igd/igd-05-runbook.md` — Orphan PakaiBedIgd recovery, `OrphanReason` table

#### Actual Implementation
- `PakaiBedIgdDal.ListOrphans()` — SQL matches runbook reasons (`VISIT_NOT_FOUND`, `VISIT_VOIDED`, `VISIT_TERMINAL`, `BED_NOT_FOUND`, `BED_REASSIGNED`, `BED_NOT_OCCUPIED`)
- **No** test invokes `ListOrphans`

#### Gap Description
Reconciliation contract is implemented but unverified; regressions in JOIN/WHERE would block DBA playbook.

#### Risk
**Medium**

#### Recommendation
Add `PakaiBedIgdDalTest.ListOrphans_*` fixtures per reason (seed visit/bed/pakaiBed states).

---

### GAP-013 — Empty bed sentinel exposed as `"-"` in API

#### Artifact Reference
- `docs/contexts/igd/igd-04-api-contract.md` — `BedIgdId` empty when not observed
- `docs/contexts/igd/igd-02-domain.md` — `HasObserved` when `BedId` not empty/`"-"`

#### Actual Implementation
- Domain `EMPTY_BED = "-"`; `IgdVisitGetResponse.BedIgdId: visit.BedId` returns `"-"`
- `BedIgdDto` normalizes bed **master** `"-"` → `""` for SQL, but visit row stores `"-"` in `BedIgdId` column via `IgdVisitDto`

#### Gap Description
UI expecting empty string may show `-` as bed id.

#### Risk
**Low**

#### Recommendation
Map `"-"` → `""` in get/list DTOs or document `"-"` as contract sentinel in `igd-04-api-contract.md`.

---

### GAP-014 — Triage history `NextReTriageAt` recomputed, not historical

#### Artifact Reference
- `docs/contexts/igd/igd-04-api-contract.md` — `TriageHistoryItem[]`
- DR-02 immutable assessment record

#### Actual Implementation
- `IgdVisitGetTriageHistoryHandler` — computes `NextReTriageAt` from `level.ToReAssessmentInterval()` at read time, not stored per row

#### Gap Description
If interval rules change, historical displayed due times change. Stored assessments are immutable; derived field is not.

#### Risk
**Low** — spec drift for medico-legal display.

#### Recommendation
Persist per-triage `NextReTriageAt` on `IgdVisitTriageType`/table (optional column) or remove field from history response.

---

### GAP-015 — `IgdVisitRepo.SaveChanges` rewrites operational events each save

#### Artifact Reference
- `docs/concepts/operational-events.md` (referenced by domain doc) — timeline audit
- DR-02-style immutability spirit for events

#### Actual Implementation
- Same delete/insert pattern as triage for `IgdVisitEvent`

#### Gap Description
Same class of risk as GAP-006 for operational timeline integrity.

#### Risk
**Medium** (combined with GAP-006)

#### Recommendation
Insert-only new events by `NoEvent` max.

---

### GAP-016 — Authorization: no IGD-specific policies (documented)

#### Artifact Reference
- `docs/contexts/igd/igd-04-api-contract.md` — Authorization section
- `docs/contexts/igd/igd-03-design.md` — global API auth

#### Actual Implementation
- Controllers have no `[Authorize]` role attributes; `UserId` body-only actor tracking

#### Gap Description
**Aligned with contract** but **partial vs context** role table (dokter/perawat/admin/DBA). DBA-only orphan endpoint is not restricted in code.

#### Risk
**Medium** — any authenticated client could call orphan sweep.

#### Recommendation
Add role policy for `pakaiBedIgd/orphan` (operator/DBA). Document in contract.

---

### GAP-017 — Documentation index does not reference this report

#### Artifact Reference
- `docs/ARTIFACTS.md` — IGD table lists 01–05 only

#### Actual Implementation
- This report is new; not linked from `ARTIFACTS.md`

#### Gap Description
Stewardship / audit artifact not discoverable from index.

#### Risk
**Low**

#### Recommendation
Add row under IGD in `docs/ARTIFACTS.md` when accepted.

---

### GAP-018 — Legacy domain doc stubs (redirect only)

#### Artifact Reference
- `Bilreg.Domain/IgdContext/docs/DOMAIN.md`, `OPERATIONAL_RECOVERY.md` — redirect stubs

#### Actual Implementation
- Stubs point to `docs/contexts/igd/igd-02-domain.md` and `igd-05-runbook.md`

#### Gap Description
No functional gap; agents using old paths still resolve. Git status shows in-repo `Bilreg.Domain` doc edits — ensure stubs remain accurate.

#### Risk
**Low**

#### Recommendation
Keep stubs during transition; no code change required.

---

## Missing Tests

| Required area (audit brief) | Status | Location / note |
|----------------------------|--------|-----------------|
| DR-05 triage-before-bed | **Domain yes** | `IgdVisitModelTest.AssignBed_BeforeTriage_Throws` |
| DR-08 discharge gate | **Handler yes** | `IgdVisitDischargeHandlerTest` |
| DR-09 void gate | **Handler yes** | `IgdVisitVoidHandlerTest` |
| Concurrency handling | **Yes** | `BedIgdRepoConcurrencyTest` |
| Orphan detection | **No** | GAP-012 |
| Triage immutability (persistence) | **No** | GAP-006 |
| Transaction rollback | **No** | — |
| Discharge cascade release | **Yes** | `IgdVisitDischargeHandlerTest.Handle_OnBed_ReleasesBedAndCascades` |
| Redirect constraints | **Domain yes** | `IgdVisitModelTest.RedirectToRawatJalan_WhenOnBed_Throws` |
| `AtsTriageEngine` / DR-04 | **No** | GAP-010 |
| Terminal `REDIRECTED` discharge/void block | **No** | GAP-002 |
| API Daftar date format | **No** | GAP-001 |

---

## Design Drift

| Topic | Artifact | Code |
|-------|----------|------|
| Triage persistence | Append-only inserts | Delete all triages/events on each `IgdVisitRepo.SaveChanges` |
| Void transaction | Visit + bed + audit in one transaction | Audit after scope |
| Assign bed validation | Handler checks `IsAvailable` | Relies on `BedIgdModel.Occupy` only |
| Bed maintenance | Runbook “application use-cases” | Domain methods only |
| Triage methods | Multiple engines planned | Only `AtsTriageEngine` registered (acceptable per scope) |

---

## Undocumented Behaviors

| Behavior | Location | Note |
|----------|----------|------|
| Discharge idempotent when already discharged | `IgdVisitModel.Discharge`, `IgdVisitDischargeHandler` | Matches `igd-04-api-contract.md` idempotency note — **documented** |
| Void idempotent when already voided | `IgdVisitVoidHandler` | Not explicit in contract; returns success |
| `AssessTriage` simplified overload (level only, zero scores) | `IgdVisitModel.AssessTriage(TriageLevelEnum,...)` | Used in tests; not exposed via API |
| Visit `BedIgdId` stored as `"-"` | `IgdVisitDto` / SQL | API consumers may not expect |
| Orphan SQL `UNKNOWN` fallback | `PakaiBedIgdDal.ListOrphans` | Should not appear in steady state |
| `TriageHistoryItem` includes computed `NextReTriageAt` | `IgdVisitGetTriageHistoryQuery` | Contract lists fewer fields than implementation |

---

## Operational Risks

| Risk | Source | Mitigation |
|------|--------|------------|
| Beds stuck in `Dirty` | GAP-007 | Add mark-clean API or runbook SQL exception process |
| False-negative available beds if `CurrentIgdVisitId` not normalized | Mitigated by `BedIgdDto` mapping `""` | Monitor DB for `"-"` in bed master |
| Orphan sweep callable by any API user | GAP-016 | Restrict endpoint |
| Void without compliance audit | GAP-005 | Single transaction |
| Redirected visit later discharged/voided | GAP-002 | Domain guards |
| Partial triage history loss on failed save | GAP-006 | Insert-only persistence |

---

## Recommended Remediation Order

1. **Critical invariant violations** — GAP-002 (terminal `REDIRECTED`), GAP-001 (API date format).
2. **Data consistency risks** — GAP-006/015 (insert-only triage/events), GAP-005 (void audit in scope).
3. **Transaction/concurrency risks** — GAP-012 orphan tests; retain `BedIgdRepo` CAS pattern.
4. **API drift** — GAP-013 sentinel bed id; GAP-014 history field semantics.
5. **Documentation drift** — GAP-007 runbook vs bed API; GAP-017 ARTIFACTS index; GAP-004 DR-03 clarity.
6. **Missing tests** — GAP-009–011, GAP-010 engine tests, GAP-002 model tests.

---

## Positive Alignment (evidence)

The following are **fully aligned** with artifacts (representative evidence):

- **API routes** — `IgdVisitController`, `BedIgdController`, `TindakanIgdController`, `BhpIgdController` match `igd-04-api-contract.md` paths and HTTP verbs.
- **Use-case coverage** — UC01–UC10 mapped to handlers (`Daftar`, `AssignDokter`, assess/re-assess triage, assign bed, checkout, redirect, tindakan/BHP, register, discharge, void).
- **JSend envelope** — `Ok(new JSendOk(...))` on controllers.
- **SQL artifacts** — `BILRG_IgdVisit`, `BILRG_IgdVisitTriage`, `BILRG_IgdVisitEvent`, `BILRG_BedIgd`, `BILRG_PakaiBedIgd`, `BILRG_RedirectRajal`, `BILRG_TindakanIgd`, `BILRG_BhpIgd` under `Bilreg.SqlDb/IgdContext/`.
- **Filtered unique index** — `UQ_BILRG_BedIgd_VisitActive` in `BILRG_BedIgd.sql`.
- **Orphan playbook** — `PakaiBedIgdDal.ListOrphans` conditions align with `igd-05-runbook.md` table.
- **Dual-write transactions** — `IgdAssignBedCmd`, `IgdCheckOutBedCmd`, `IgdVisitDischargeCmd`, `IgdVisitVoidCmd`, `IgdVisitRedirectRawatJalanCmd` use `TransHelper.NewScope()` for multi-table writes.
- **List aktif** — `IgdVisitDal.ListAktif` filters `DAFTAR`/`REGISTERED` and non-void `VodDate` sentinel.

---

## Speculative / out-of-scope notes

- **DR-10 billing** — No in-process billing engine expected; alignment N/A.
- **ESI/CTAS/MTS triage** — Explicitly future in `igd-03-design.md`; not counted as gap.
- **Priority bed preemption** — Documented as clinical process, not auto domain rule — aligned with “no auto-preempt” note in domain doc.

---

*End of report. Evidence gathered from source and artifact files on 2026-05-26; re-run after material IGD Visit changes.*
