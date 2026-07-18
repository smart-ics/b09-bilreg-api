# Admisi Ranap Release 1 Phase B2 — Journey Read DAL / Projection

**Status:** LIVE  
**Date:** 2026-07-13  
**Scope:** Release 1 journey read DAL, SQL journey-root projection, stage-parity CASE for facets/filters/cursor paging, detail workspace hydration via Phase B1 resolver, integration + parity tests, data-proof + performance notes.

**Plan reference:** `docs/contexts/admisi-ranap/rawat-inap-patient-journey-workspace-implementation-plan.md` (Phase B2)  
**Depends on:** `docs/contexts/admisi-ranap/admisi-ranap-phase-b1-journey-resolver-implementation-report.md`

**Out of scope (unchanged):** API controllers, frontend, removal of OperationalWorklist, Ward/Bed Management, room/bed/placement/`IN_WARD`, write aggregates/commands, permission infrastructure, production data repair, in-memory historical pagination.

---

## Summary

Phase B2 adds a **journey-oriented read projection** beside the existing aggregate OperationalWorklist. SQL builds one row per journey (prospective Opname, prospective Reservation, or Admission episode with Waiting Lists folded by `RegId`), derives an operational stage with the same precedence as B1 for filter/facet/cursor work, then enriches each page/detail result through `JourneyNormalizedFacts` → `Release1JourneyStageResolver`.

Projection contract version remains **`1`**.

---

## 1. Data-proof findings

Profiled against **`HOSPITAL_HPL`** on `dev.smart-ics.com` (read-only).  
`devTest` does **not** contain Admisi Ranap tables (`BILRG_AdmAdmission` missing) — soft-skip pattern retained for environments without the schema.

### Counts (non-void rows, `VodDate = 3000-01-01`)

| Metric | Count |
|--------|------:|
| Admission by status — Admitted (0) | 6 |
| Admission by status — Updated (1) | 1 |
| Admission by status — Waiting (2) | 0 |
| Admission by status — Completed (3) | **0** |
| Admission by status — Cancelled (4) | 0 |
| Admissions with Opname Request source only | 5 |
| Admissions with Reservation source only | 2 |
| Direct/legacy Admissions (neither source) | 0 |
| Admissions with both sources | 0 |
| Fulfilled Opname Requests without expected Admission | 0 |
| Realized Reservations without expected Admission | 0 |
| Multiple Admissions referencing same Opname source | 0 |
| Multiple Admissions referencing same Reservation source | 0 |
| Waiting List — Waiting (0) | 2 |
| Waiting List — Accepted / Closed / Cancelled | 0 / 0 / 0 |
| Admissions with multiple active Waiting Lists | 0 |
| Orphan Waiting Lists | 0 |
| Closed Waiting Lists without authoritative Admission cancellation | 0 |
| Opname Requests (all statuses) | 15 (Requested 10, Fulfilled 5) |
| Reservations (all statuses) | 2 (both Realized) |
| Admissions | 7 |
| Waiting Lists | 2 |

### `Admission.Completed`

**Quantity: 0.** No observable create/update history for Completed rows on this database.  
B1 behavior retained: if Completed rows appear later, integrity code `ADMISSION_COMPLETED_WITHOUT_AUTHORITATIVE_PROCESS` forces `NEEDS_RECONCILIATION`. Their existence would still **not** prove an authoritative completion workflow.

### Reconciliation inventory (live profile)

No structural reconciliation cases observed in the profiled corpus (no both-sources, no multi-admission-per-source, no fulfilled-without-admission, no multi-active WL, no orphans). Seeded integration tests deliberately create and clear contradiction cases.

### Plan vs database relationships

| Plan expectation | Observed |
|------------------|----------|
| Opname ⟂ Reservation | Confirmed — no Opname↔Reservation join; Reservation has no `OpnameRequestId` |
| Episode join key = `RegId` | Confirmed for Admission + Waiting List |
| Fulfilled/Realized absorbed into Admission journey | Confirmed (5 fulfilled Opnames + 2 realized Reservations linked via Admission source IDs) |
| Rawat Inap does not require `ta_registrasi2` | Confirmed — detail/list succeed with `ta_registrasi2` count = 0 for seeded admissions |
| `IX_(RegId, WaitingListStatus, …)` on Waiting List | **Script exists** (`BILRG_BedWaitingList_RegId_Status_Index.sql`) but **not deployed** on `HOSPITAL_HPL` |

No production data was changed or repaired.

---

## 2. Changed / added files

### Bilreg.Application

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/JourneyFeature/IJourneyDal.cs` | List/detail contracts, filter, facets, workspace DTOs |
| `AdmisiRanapContext/JourneyFeature/JourneyListCursor.cs` | Opaque Base64Url cursor `(SortAt, JourneyId)` |
| `AdmisiRanapContext/JourneyFeature/Release1JourneySqlStageExpression.cs` | Isolated SQL-parity stage inputs + CASE documentation |
| `AdmisiRanapContext/JourneyFeature/UseCases/AdmListJourneyQry.cs` | MediatR list + get handlers (no controllers) |
| `Bilreg.Application.csproj` | JourneyFeature UseCases folder |

### Bilreg.Infrastructure

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/JourneyFeature/JourneyDal.cs` | Journey-root SQL, facets, cursor page, fact load, B1 enrichment |
| `Bilreg.Infrastructure.csproj` | JourneyFeature folder |

### Bilreg.Api

| Path | Purpose |
|------|---------|
| `Configurations/InfrastructureService.cs` | `AddScoped<IJourneyDal, JourneyDal>()` |

### Bilreg.Test

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/JourneyFeature/Release1JourneySqlStageParityTest.cs` | SQL stage ↔ B1 resolver parity matrix + cursor stability |
| `AdmisiRanapContext/JourneyFeature/JourneyDalIntegrationTest.cs` | DB integration coverage on `HOSPITAL_HPL` |

### Docs / tools

| Path | Purpose |
|------|---------|
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b2-journey-projection-implementation-report.md` | This report |
| `docs/ARTIFACTS.md` | Index entry |
| `tools/journey-perf/` | One-off list/detail timing harness (not product runtime) |

**Unchanged:** `OperationalWorklistDal` / controller / frontend.

---

## 3. Projection / query architecture

```text
Prospective Opname roots (no Admission.OpnameRequestId match)
Prospective Reservation roots (no Admission.ReservationId match)
Admission roots + LEFT JOIN source + WL agg + ta_registrasi/guarantor + RegInap/DPJP
        │
        ▼
JourneyProjected (one row / journey) + SqlStage CASE (B1 precedence)
        │
        ├─► Facets + totalMatches  (scope + non-stage filters; before stage filter/page)
        └─► Keyset page (SortAt DESC, JourneyId DESC)
                    │
                    ▼
            Per-page Enrichment: LoadFactBag → Release1JourneyStageResolver
```

Detail: parse `JourneyId` → load normalized facts (source, admissions, **all** Waiting Lists by `RegId` ordered chronologically, patient, guarantor, doctor, bangsal/kelas) → B1 resolver → `JourneyDetailWorkspace`.

**Do not** join Reservation to Opname Request.  
**Do not** use patient/MR/name/date proximity as join keys.  
**Do not** require `ta_registrasi2` (`HasOrphanRegistration` is never set from its absence).

---

## 4. Join and JourneyId rules

| Root | Join keys | Canonical JourneyId (valid data) |
|------|-----------|----------------------------------|
| Prospective Opname | Independent | `opn:{OpnameRequestId}` |
| Prospective Reservation | Independent | `rsv:{ReservationId}` |
| Registered Opname-sourced Admission | `Admission.OpnameRequestId` / `FulfilledRegId` ↔ `RegId`; WLs by `RegId` | `opn:{OpnameRequestId}` |
| Registered Reservation-sourced Admission | `Admission.ReservationId` / `RealizedRegId` ↔ `RegId`; WLs by `RegId` | `rsv:{ReservationId}` |
| Direct/legacy Admission | `RegId` only | `reg:{RegId}` |
| Integrity conflict involving Admission | Same facts; no silent merge | Fallback `reg:{RegId}` + `NEEDS_RECONCILIATION` |

Soft-delete filter: `VodDate = 3000-01-01`.

---

## 5. Stage-resolver parity approach

1. **Authority:** `Release1JourneyStageResolver` remains the semantic authority for list enrichment and detail.
2. **SQL CASE:** `Release1JourneySqlStageExpression.SqlCaseExpression` mirrors DecideStage precedence for filter, facets, and pagination only.
3. **C# mirror:** `Release1JourneySqlStageExpression.Derive` + `FromNormalizedFacts` used in parity unit tests.
4. **Tests fail** on any B1 ↔ SQL-stage disagreement (`Release1JourneySqlStageParityTest`).
5. Pagination is **not** implemented by loading the full historical corpus into memory.

Active scope excludes SQL stages `COMPLETED` (5) and `CANCELLED` (6). History includes the full corpus with the same DTO shape.

---

## 6. Integration-test results

Command:

```text
dotnet test --filter "FullyQualifiedName~AdmisiRanapContext.JourneyFeature"
```

| Suite | Result |
|-------|--------|
| Phase B1 resolver unit tests | Passed (unchanged) |
| SQL/B1 parity + cursor unit tests | Passed |
| JourneyDal integration (`HOSPITAL_HPL`) | Passed |

**Total: 60 passed / 0 failed.**

Covered scenarios include: one journey for Admission+WL / multi-WL; two RegIds → two journeys; prospective Opname/Reservation independence; fulfilled/realized via Admission; `reg:{RegId}` direct; WL status mapping; Closed ≠ Completed/IN_WARD; both-sources → reconciliation; facet sum = totalMatches; stable cursor on equal timestamps; active/history same shape; chronological WL timeline; no Ward/`ta_registrasi2` dependency.

---

## 7. Performance and execution-plan results

### Row counts before / after consolidation (`HOSPITAL_HPL`)

| View | Rows |
|------|-----:|
| Active raw aggregate-style rows (worklist-like terminal exclusions) | 19 |
| History raw aggregate-style rows | 26 |
| Journey roots (history) | **17** |
| Journey roots (active approx.) | **17** |

Consolidation removes duplicate Admission+Waiting List rows and absorbs fulfilled/realized sources into Admission journeys.

### Observed durations (`tools/journey-perf`, Release build, remote SQL)

| Query | Duration | Notes |
|-------|----------|-------|
| Active list (page 50, full enrich) | ~1856 ms | 17 matches / 17 items |
| Historical list (page 50, full enrich) | ~1851 ms | 17 matches / 17 items |
| Detail (single JourneyId) | ~235 ms | |

Probe join `Admission ⟕ WaitingList`: ~0 ms elapsed, 2 + 15 logical reads — trivial at current volume.

### Generated SQL shape

- Shared CTE: `WlAgg`, source multi-admission detectors, `ProspectiveOpname`, `ProspectiveReservation`, `AdmissionRoots`, `JourneyUnion`, `JourneyProjected` (+ `SqlStage` CASE).
- Facet query: `GROUP BY SqlStage` after scope/non-stage filters.
- Page query: `TOP (@PageSize)` with keyset `(SortAt, JourneyId)` and optional stage filter.
- Detail/enrichment: point lookups by Opname/Reservation/RegId + WL by RegId + optional `ta_registrasi` / `ta_reg_inap` / `ta_reg_history_dokter`.

### List latency note

List wall time is dominated by **per-item fact hydration** after SQL paging (N+1 enrichment pattern), not by the facet/page SQL itself at this volume. Batch enrichment is a B4 candidate when active/historical volumes grow; it was not required to satisfy B2 correctness constraints.

### Command timeout / observability

`JourneyDal` uses `CommandTimeout = 60s` and logs list/detail duration plus placement-unavailable / reconciliation facet counts.

---

## 8. Proposed indexes

**No index migration added.** Current execution evidence does not justify speculative indexes.

| Candidate (for future review if volume grows) | Evidence today |
|-----------------------------------------------|----------------|
| `BILRG_BedWaitingList (RegId, WaitingListStatus)` incl. timestamp | Script already in repo; **not deployed** on `HOSPITAL_HPL`; 15 logical reads on full WL scan at n=2 |
| `BILRG_AdmAdmission (OpnameRequestId)` / `(ReservationId)` | Not evidenced; PK/status indexes sufficient at n=7 |
| `BILRG_AdmOpnameRequest (FulfilledRegId)` / Reservation `(RealizedRegId)` | Not evidenced at current volume |

Revisit with actual plans when historical journey counts leave the tens/hundreds range.

---

## 9. Reconciliation counts

| Source | Count |
|--------|------:|
| Live profile structural reconciliation cases | 0 |
| Seeded IT contradiction (`ADMISSION_HAS_BOTH_SOURCES`) | Exercised then void-cleaned |
| `Admission.Completed` without authoritative process | 0 live rows |

---

## 10. Conflicts / clarifications vs plan

1. **Data-proof DB:** Plan examples assume a populated Admisi DB; `ConnStringHelper.GetTestEnv()` → `devTest` lacks tables. Profiling and integration tests use **`HOSPITAL_HPL`**.
2. **Waiting List RegId index:** Plan suggests indexing when plans require it; script exists but is undeployed; **not** applied in B2 (no plan evidence of need).
3. **List enrichment N+1:** Plan preferred batch hydration; B2 ships correctness-first single-ID enrichment after SQL page. Documented for B4 if latency becomes material.
4. **Active vs history equality on profile:** Both scopes returned 17 journeys because no terminal Cancelled/Completed journeys were present in the live corpus at measurement time.

---

## Next step (Phase B3)

Add journey list/detail/legacy-resolve API controllers and authorization policies; keep write routes aggregate-scoped; do not expose bed-assignment commands.
