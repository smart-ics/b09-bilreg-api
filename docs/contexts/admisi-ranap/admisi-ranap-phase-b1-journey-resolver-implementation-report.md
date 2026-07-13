# Admisi Ranap Release 1 Phase B1 — Journey Contracts & Pure Resolver

**Status:** LIVE  
**Date:** 2026-07-13  
**Branch:** `jude-pasien-journey`  
**Scope:** Release 1 journey contracts, canonical `JourneyId`, integrity validation, and pure operational-stage resolver.

**Plan reference:** `docs/contexts/admisi-ranap/rawat-inap-patient-journey-workspace-implementation-plan.md` (Phase B1)  
**Also mirrored at:** `c012_myhospital_web/rawat-inap-patient-journey-workspace-implementation-plan.md`

**Out of scope:** SQL/DAL projection, Journey API controllers, frontend, database migrations, Ward/Bed Management, Ward adapters, room/bed/occupancy/release/transfer facts, `IN_WARD`, new write aggregates/commands, permission-based authorization, generic support/gateway/orchestration abstractions.

---

## Summary

Phase B1 adds a read-side journey contract and a **pure** Release 1 stage resolver under `AdmisiRanapContext/JourneyFeature`. The resolver receives normalized facts only (no persistence). It returns exactly one operational stage, current condition, next task, owner-boundary executability, placement-unavailable metadata, timeline stubs, system/audit references, and structured reconciliation issues.

`JourneyId` is deterministic and opaque:

| Origin | Canonical JourneyId |
|--------|---------------------|
| Opname Request | `opn:{OpnameRequestId}` |
| Reservation | `rsv:{ReservationId}` |
| Direct / legacy Admission (no valid source) | `reg:{RegId}` |

On integrity failure involving a conflicting Admission, derivation falls back to `reg:{RegId}` so journeys are not silently merged. Clients must treat `JourneyId` as opaque.

Projection contract version: **`1`** (`JourneyProjectionVersions.Release1`).

---

## Completion-authority finding

| Question | Evidence | Decision |
|----------|----------|----------|
| Does an authoritative process own `Admission.Completed`? | `AdmissionModel.Complete(string)` exists on the domain aggregate and is exercised only by domain unit tests (`AdmissionModelTest`). No MediatR command, orchestrator, or controller in `AdmisiRanapContext` calls it. Cancellation **is** owned (`AdmCoordinatedCancelCmd`). | **`COMPLETED` is unreachable in Release 1.** |

Consequences:

- `JourneyOperationalStage.Completed` remains in the enum for forward compatibility.
- Facts with `AdmissionStatusEnum.Completed` produce integrity code `ADMISSION_COMPLETED_WITHOUT_AUTHORITATIVE_PROCESS` and stage `NEEDS_RECONCILIATION`.
- Waiting List `Closed` never maps to `COMPLETED` or `IN_WARD`.
- `IN_WARD` remains deferred to Release 2 (Ward Management).

---

## Files created / changed

### Bilreg.Application

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/JourneyFeature/JourneyEnums.cs` | Stages, origin, owner domain, placement, attention, timeline, action codes, reconciliation code constants, projection version |
| `AdmisiRanapContext/JourneyFeature/JourneyContracts.cs` | Normalized input facts and `Release1JourneyResolution` contract records |
| `AdmisiRanapContext/JourneyFeature/JourneyIdFactory.cs` | Deterministic JourneyId derivation + legacy record helpers |
| `AdmisiRanapContext/JourneyFeature/JourneyIntegrityValidator.cs` | Relationship integrity → structured issues (no silent merge) |
| `AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolver.cs` | Pure stage / owner / next-task / allowed-action resolver |
| `Bilreg.Application.csproj` | Registered `JourneyFeature` folder |

### Bilreg.Test

| Path | Purpose |
|------|---------|
| `AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolverTest.cs` | Exhaustive unit coverage for Release 1 fact combinations |
| `Bilreg.Test.csproj` | Registered `JourneyFeature` folder |

---

## Final contract shape

`Release1JourneyResolution` groups:

| Group | Contents |
|-------|----------|
| Identity | `journeyId`, `regId?`, origin kind, prospective / registered / terminal flags |
| Operational stage | One `JourneyOperationalStage`, `JourneyCurrentCondition` (title, explanation, since) |
| Next task | Action code, label, owner (`ADMISI` / `WARD` / `SYSTEM_SUPPORT` / `NONE`), `canExecute`, blocked reason, optional required-permission slot |
| Allowed Admisi actions | Next task plus independent Admisi-owned cancel/update when domain state permits |
| Accommodation handover summary | Active WL id/status/ward, total / closed / cancelled counts |
| Placement availability | Always `UNAVAILABLE` + `WARD_MANAGEMENT_NOT_IMPLEMENTED` in Release 1 |
| Timeline | Ordered source / registration / Waiting List events derived from facts |
| Attention flags | Enum present; empty in B1 (no threshold inputs yet) |
| System / audit | Origin, source ids/statuses, `RegId`, admission status, Waiting List ids, projection timestamp |
| Reconciliation | Structured `JourneyReconciliationIssue` list (`Code`, `Message`, `RelatedRecordIds`) |
| Versioning | `ProjectionVersion = 1` |

Normalized resolver input: `JourneyNormalizedFacts` (origin, patient summary, optional Opname/Reservation facts, admissions, waiting lists, orphan-registration flag).

### Reconciliation codes

| Code | Meaning |
|------|---------|
| `ADMISSION_HAS_BOTH_SOURCES` | Admission references both Opname Request and Reservation |
| `MULTIPLE_ADMISSIONS_FOR_SOURCE` | More than one Admission in the journey facts / for one source |
| `FULFILLED_SOURCE_WITHOUT_ADMISSION` | Fulfilled/Realized source missing expected Admission |
| `SOURCE_FULFILLMENT_REGID_MISMATCH` | Fulfilled/Realized RegId or source link mismatch |
| `MULTIPLE_ACTIVE_WAITING_LISTS` | More than one Waiting/Accepted Waiting List |
| `WAITING_LIST_REGID_MISMATCH` | Waiting List RegId not in episode Admission set |
| `ORPHAN_WAITING_LIST` | Waiting List without Admission |
| `ORPHAN_REGISTRATION` | Registration present without coherent Admission episode |
| `ACTIVE_WAITING_LIST_ON_CANCELLED_ADMISSION` | Active WL while Admission is Cancelled |
| `ACTIVE_ADMISSION_ON_CANCELLED_SOURCE` | Active Admission while source is Cancelled |
| `ADMISSION_COMPLETED_WITHOUT_AUTHORITATIVE_PROCESS` | Admission.Completed with no owning Release 1 process |
| `INTERNALLY_CONTRADICTORY_STATE` | Fallback contradictory state |

---

## Resolver precedence table

Apply in this order (no placement / Ward-adapter / room / bed / transfer inputs):

| # | Rule | Stage | Next-task owner / executability |
|---|------|-------|----------------------------------|
| 1 | Blocking integrity issues | `NEEDS_RECONCILIATION` | System support; `canExecute = false` |
| 2 | Authoritative Admission cancel, or unregistered cancelled source | `CANCELLED` | None; read-only |
| 3 | Authoritative episode completion | *(skipped — unreachable)* | — |
| 4 | Active Waiting List `Waiting` | `WARD_ACCEPTANCE_REQUIRED` | Ward; `canExecute = false` |
| 5 | Active Waiting List `Accepted` | `HANDOVER_ACCEPTED` | Ward; `canExecute = false` |
| 6 | Any Waiting List `Closed`, no active WL, Admission still active | `PLACEMENT_STATUS_UNAVAILABLE` | Ward; `canExecute = false` |
| 7 | Active Admission (`Admitted` / `Updated` / legacy `Waiting`) without active handover (incl. WL `Cancelled` only) | `HANDOVER_REQUIRED` | Admisi; `canExecute = true` (boundary) |
| 8 | Unregistered Opname `Requested` or Reservation `Reserved`/`Maintained` | `REGISTRATION_REQUIRED` | Admisi; `canExecute = true` (boundary) |

Source terminal interpretation:

- `OpnameRequest.Fulfilled` / `Reservation.Realized` follow the linked Admission journey (same stable JourneyId).
- Fulfilled/Realized without Admission → `NEEDS_RECONCILIATION`.
- Waiting List `Cancelled` does not cancel the episode; returns `HANDOVER_REQUIRED` unless a prior unresolved `Closed` remains → `PLACEMENT_STATUS_UNAVAILABLE`.
- Waiting List `Closed` never produces `IN_WARD` or `COMPLETED`.

### Authorization boundary (B1)

`canExecute` describes **operational boundary only**:

- Admisi-owned action + valid domain state → `canExecute = true`
- Ward-owned task → `canExecute = false`
- Reconciliation / system-support → read-only
- Terminal (`CANCELLED`) → read-only

User-specific authorization remains outside B1 (existing auth + later B3 policies).

---

## Test coverage and results

**Command:**

```text
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~JourneyFeature
```

**Result:** Passed — **32** tests, **0** failed (2026-07-13).

Covered scenarios:

- Requested Opname Request
- Reserved and Maintained Reservation
- Admission with no Waiting List (including `Admitted` / `Updated` / `Waiting`)
- Waiting List `Waiting`, `Accepted`, `Closed`, `Cancelled`
- Closed then Cancelled → stays `PLACEMENT_STATUS_UNAVAILABLE`
- Admission cancellation; cancelled unregistered Opname
- Fulfilled/realized source linked to Admission (stable JourneyId)
- Fulfilled/realized source without Admission
- Multiple Admissions for one source (`reg:` fallback)
- Admission containing both source types
- Multiple active Waiting Lists
- Orphan Waiting List / orphan Registration
- Stable JourneyId before and after Registration
- Two episodes for the same patient remain distinct
- Waiting List closure never produces `IN_WARD` or `COMPLETED`
- Exhaustive matrix: every supported combination yields one deterministic stage/owner/`canExecute`
- Legacy record helpers for Opname / Reservation / Admission-with-known-source
- Placement always `UNAVAILABLE` in Release 1
- `Admission.Completed` → reconciliation (Completed unreachable)

---

## Plan vs actual domain model

| Topic | Plan expectation | Actual domain | B1 handling |
|-------|------------------|---------------|-------------|
| `Admission.Completed` | Enable `COMPLETED` only if an owning process exists | Domain method only; no application owner | Stage unreachable; integrity code when status appears |
| `Admission.Waiting` | Legacy/future; not bed assignment | Enum value `Waiting = 2` | Treated as active → `HANDOVER_REQUIRED` when no active WL |
| Waiting List `Cancelled` | Backend has state `3`; FE historically omitted it | `WaitingListStatusEnum.Cancelled` | Fully supported in contracts + resolver |
| Empty source refs | — | Domain uses `"-"` sentinel | Treated as absent by `JourneyIdFactory.HasValue` |
| Reservation independence | No `OpnameRequestId` on Reservation | Confirmed | Origin kinds remain separate; both-sources on Admission is reconciliation |
| Ward placement | Unavailable in Release 1 | No Ward placement authority | Always `PLACEMENT_STATUS_UNAVAILABLE` metadata; never invent bed facts |

---

## Next phase

**Release 1 Phase B2 — read DAL/projection:** build journey-root SQL/query composition that produces `JourneyNormalizedFacts`, then call `Release1JourneyStageResolver`. Keep the existing operational-worklist query for compatibility during rollout.
