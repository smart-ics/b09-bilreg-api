# Admisi Ranap Release 1 Phase B2.1 — Journey Projection Hardening

**Status:** LIVE  
**Date:** 2026-07-13  
**Scope:** Remove N+1 list enrichment, preserve SQL/B1 stage parity, transaction-scoped IT isolation, CancelAdmission allowed-action gate, representative-volume evidence.  
**Out of scope:** API controllers (B3), frontend, indexes without plan evidence, role/permission infrastructure.

**Depends on:** [admisi-ranap-phase-b2-journey-projection-implementation-report.md](admisi-ranap-phase-b2-journey-projection-implementation-report.md)

---

## Summary

Phase B2.1 hardens the journey read projection. List enrichment no longer issues one fact-load per page item: after facet + page SQL, facts are loaded in two fixed `QueryMultiple` batches, folded in memory, then resolved once per bag by `Release1JourneyStageResolver`. Detail exposes both B1 **candidate** actions and **allowed** actions after the existing Tata Rekening billing precondition. Integration tests no longer seed/void-clean `HOSPITAL_HPL`; they require env configuration, fail loudly on missing schema, and always roll back via `TransHelper.NewScope()`.

---

## 1. Changed files

### Bilreg.Application

| Path | Change |
|------|--------|
| `JourneyFeature/JourneyContracts.cs` | `AllowedAdmisiActions` → `CandidateAdmisiActions` on `Release1JourneyResolution` |
| `JourneyFeature/IJourneyDal.cs` | Detail: `CandidateAdmisiActions` + `AllowedAdmisiActions`; `LastListDiagnostics`; `JourneyListDiagnostics` |
| `JourneyFeature/Release1JourneyStageResolver.cs` | Emits candidates only (`BuildCandidateActions`) |
| `JourneyFeature/JourneyAllowedActionEvaluator.cs` | **New** — applies CancelAdmission billing gate |

### Bilreg.Infrastructure

| Path | Change |
|------|--------|
| `JourneyFeature/JourneyDal.cs` | Batch list path, diagnostics, same-connection billing check on detail |
| `JourneyFeature/JourneyDal.Batch.cs` | **New** — fixed-batch `QueryMultiple` hydration + in-memory fold |
| `Bilreg.Infrastructure.csproj` | `InternalsVisibleTo` for `Bilreg.Test` |

### Bilreg.Test

| Path | Change |
|------|--------|
| `JourneyFeature/JourneyDalIntegrationTest.cs` | Env + schema gate, TransHelper rollback, parity / RT / cancel tests |
| `JourneyFeature/JourneyDalVolumeTest.cs` | **New** — 500-journey pagination + timings |
| `JourneyFeature/JourneyDalTestEnv.cs` | **New** — shared env/schema requirements |
| `JourneyFeature/JourneyDalTestEnvGateTest.cs` | **New** — missing-env fails clearly |
| `JourneyFeature/JourneyAllowedActionEvaluatorTest.cs` | **New** — billing / no phantom blockers |
| `JourneyFeature/Release1JourneyStageResolverTest.cs` | Candidate property rename |

### Docs

| Path | Change |
|------|--------|
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b2.1-journey-projection-hardening-report.md` | This report |
| `docs/ARTIFACTS.md` | Index entry |

---

## 2. Final query architecture

```text
List:
  RT1  Facet SQL (journey-root CTE + GROUP BY SqlStage)
  RT2  Page SQL (keyset TOP pageSize+1)
  RT3  QueryMultiple: Opname + Reservation + Admission (page IDs)
  RT4  QueryMultiple: expanded Opname/Reservation/Admission + WaitingList + Reg extras
       → fold JourneyNormalizedFacts in memory
       → Release1JourneyStageResolver.Resolve once per page item

Empty page: RT1 + RT2 only (FixedListRoundTripsEmptyPage = 2)
Non-empty page: FixedListRoundTripsWithPage = 4 (constant vs page size)

Detail:
  Single-journey LoadFactBag (unchanged shape) → B1 Resolve
  → CancelAdmission billing EXISTS on same SqlConnection
  → CandidateAdmisiActions + AllowedAdmisiActions
```

SQL stage CASE remains filter/facet/sort/page only. B1 remains semantic authority for enrichment.

---

## 3. Database round-trip count

| Scenario | Round-trips |
|----------|------------:|
| List, empty page | 2 |
| List, page size 1 / 10 / 50 | **4** (proven equal) |
| Detail (with CancelAdmission candidate) | fact-load queries + 1 billing check on same connection |

Instrumentation: `IJourneyDal.LastListDiagnostics` (`DatabaseRoundTrips`, facet/page ms, batch ms, resolver ms, total ms).

---

## 4. Test-isolation strategy

**Chosen:** transaction-scoped seed with unique IDs; dispose without `Complete()` (always roll back).

- Env: `BILREG_JOURNEY_IT_SERVER`, `BILREG_JOURNEY_IT_DATABASE` (optional `BILREG_JOURNEY_IT_USER` / `_PASSWORD`; defaults `bilregLogin` / `bilreg123!`).
- Traits: `Category=JourneyDalIntegration`, `Category=JourneyDalVolume`.
- Missing env or missing `BILRG_AdmAdmission` / Opname / Reservation / WaitingList → **fail** with `InvalidOperationException` (no soft-skip).
- `HOSPITAL_HPL` may be pointed at for schema-complete runs; tests do **not** persist, void, repair, or clean operational rows.
- Isolation proof: `TransactionRollback_Leaves_No_Seeded_Records`.

### Commands

```text
# Unit (no DB)
dotnet test --filter "FullyQualifiedName~AdmisiRanapContext.JourneyFeature&Category!=JourneyDalIntegration&Category!=JourneyDalVolume"

# Integration
set BILREG_JOURNEY_IT_SERVER=dev.smart-ics.com
set BILREG_JOURNEY_IT_DATABASE=<schema-complete-db>
dotnet test --filter "Category=JourneyDalIntegration"

# Volume
dotnet test --filter "Category=JourneyDalVolume"
```

---

## 5. Performance — before and after

### Before (B2 report, Release, remote `HOSPITAL_HPL`, n≈17)

| Query | Duration |
|-------|----------|
| Active list page 50 (N+1 enrich) | ~1856 ms |
| Detail | ~235 ms |

Dominant cost: per-item `LoadFactBag` (3–6 queries × page size).

### After (B2.1, Release, remote DB via IT env, 500 seeded journeys under ambient `TransHelper` scope)

| Metric | page10 | page50 | filtered50 | history50 |
|--------|-------:|-------:|-----------:|----------:|
| Total list ms | ~2930 | ~10821 | ~11298 | ~11081 |
| Facet+page SQL ms | — | ~107 | — | — |
| Batch hydration ms | — | ~10712 | — | — |
| Resolver ms | — | ~0 | — | — |
| Round-trips | 4 | 4 | 4 | 4 |
| Detail ms (sample) | ~1772 | | | |

**Target:** page-size-50 &lt; 750 ms (prefer &lt; 500 ms) — **not met** on this remote IT environment.

**What is proven:** round-trips are fixed at 4 and do not grow with page size.  
**Bottleneck:** batch-hydration wall time under ambient TransactionScope + remote SQL (facet/page ~100 ms; resolver negligible). No indexes were added (no execution-plan evidence justifying a migration in this pass). Re-measure outside ambient TX / closer to SQL for API-path latency before B3 SLO claims.

---

## 6. Actual SQL / B1 parity evidence

| Evidence | Result |
|----------|--------|
| Unit `Release1JourneySqlStageParityTest` (C# Derive ↔ B1) | Passed |
| IT `SqlPageStage_Matches_B1_EnrichedStage_ForRelease1Matrix` (SQL stage filter ↔ enriched stage for RegistrationRequired / WardAcceptanceRequired / HandoverAccepted / Cancelled) | Passed |
| IT `BatchHydration_Matches_SingleJourneyLoad_Contract` (JourneyId, stage, owner/next-task, timeline) | Passed |
| IT multi-WL chronological fold | Passed |
| IT facet sum = totalMatches | Passed |
| IT equal-timestamp cursor stability | Passed |
| Volume 500-journey cursor: no duplicate/omit | Passed |

---

## 7. Action-candidate versus allowed-action decision

| Layer | Contract | Meaning |
|-------|----------|---------|
| B1 resolver | `CandidateAdmisiActions` | Stage + ownership boundary only (Ward tasks remain non-executable) |
| Detail projection | `AllowedAdmisiActions` | Candidates after **implemented** command dependencies |

**CancelAdmission:** evaluated with the same `ta_trs_billing` EXISTS used by coordinated cancellation (`RegistrationCancellationEligibilityDal.HasBillingItemsSql`) on the detail connection. Billing present → `CanExecute=false` with Tata Rekening blocked reason. No Bed Occupancy / Medication / Clinical Documentation / Ward Transfer blockers. List items remain action-free. No permission infrastructure.

---

## 8. Test results

| Suite | Result |
|-------|--------|
| JourneyFeature unit (excl. IT/Volume) | **53 passed** |
| JourneyDalIntegration | **19 passed** |
| JourneyDalVolume | **3 passed** |
| Wider `AdmisiRanapContext` excl. Journey IT/Volume | 178 passed; **12 failed** in pre-existing `AdmissionRegistrationStep4CDbTest` (hardcoded DB fixtures; unrelated to journey projection) |

Hardening gate for JourneyFeature B2.1: **passed**.

---

## 9. Remaining blockers for B3

1. Journey list/detail **API controllers** and auth policies (not started).
2. Page-50 latency SLO (&lt;750 ms) not demonstrated on remote IT-with-ambient-TX; re-profile on the production API path before advertising list SLO.
3. Optional: further batch SQL tuning with execution plans if API-path latency remains batch-bound (still no speculative indexes).
4. Frontend must consume `candidateActions` vs `allowedActions` correctly and must not invent cancellation rules.
5. Do not remove OperationalWorklist until B3/B4 rollout plan says so.

**Do not implement B3 until this hardening report is accepted as the gate.**
