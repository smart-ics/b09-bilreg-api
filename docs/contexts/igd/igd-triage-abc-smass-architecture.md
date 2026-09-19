---
Title: IGD Triage ABC to SMASS Architecture
Code: IGD-TRIAGE-ABC-SMASS
Artifact: ARCHITECTURE
Version: 2.0
LastUpdated: 2026-09-19
---

# 1. Overview

## Realized Capability

This architecture realizes one FEATURE: every IGD Visit Triage ABC submission (first triage and
re-triage) synchronously produces one immutable structured medical assessment in SMASS, built
against a dedicated IGD Triage Paper, correlated by (`IgdVisitId`, `NoTriage`), created before
registration exists, and linked to the administrative registration when `AssignRegister` /
`ReplaceRegister` succeeds.

The architecture defines:

- the BILREG-side integration gateway, operational task store, configuration and retry surface;
- the SMASS-side pending-registration creation path, registration-link path, by-visit query,
  master-data mapping table, and persistence changes;
- the frontend realization on the existing IGD Triage workspace (`c012_myhospital_web`).

## Referenced Artifacts

- FEATURE: `b09-bilreg-api/docs/contexts/igd/igd-01-context.md` (IGD Visit)
- DOMAIN: `b09-bilreg-api/docs/contexts/igd/igd-02-domain.md` (DR-01…DR-11)
- Analysis input (FEASIBILITY-ASSESSMENT):
  `b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-feasibility-assessment.md`
- Downstream artifact: Implementation Plan (Planner Agent)

## Owner Contexts

- IGD Visit — `b09-bilreg-api` / `IgdContext`
- Structured Medical Assessment — `a043_smass_structuredmedicalassesment_api` / `AssesmentContext`
- Frontend — `c012_myhospital_web` (`src/modules/Emergency`)

## Status and Readiness

```text
Status: DRAFT (canonical architecture artifact)
Ready For Planning: YES   (see §11)
```

Blocking issues: none. Every planning-level input listed in the feasibility assessment is closed in
this architecture by an explicit architecture decision (AR-01…AR-16).

## Solution Shape (approved, restated)

```text
BILREG IGD Visit
  ├─ triage persistence and history (unchanged, legal record)
  ├─ IgdVisitSmassTask  (operational generation / link task store)
  ├─ ISmassAssessmentGateway → POST  {Smass}/api/Assesment/generateIgdTriage
  └─ ISmassAssessmentGateway → PATCH {Smass}/api/Assesment/linkIgdVisit

SMASS
  ├─ dedicated IGD Triage Paper + CustomSection (+ concepts)
  ├─ SMASS_Assesment: IgdVisitId, NoTriage, RegistrationLinkStatus
  ├─ SMASS_TriageConceptMap (SMASS-owned ATS→SMASS mapping master data)
  ├─ GET api/Assesment/igdVisit/{igdVisitId}      (by-visit surface)
  └─ GET api/Assesment/pendingRegistration        (monitoring surface)
```

## Inputs

| Artifact | Path | Role |
|---|---|---|
| FEASIBILITY-ASSESSMENT | `b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-feasibility-assessment.md` | GAP-001…GAP-012, OQ-01…OQ-12, approved decisions D-01…D-15 |
| IGD Context (FEATURE) | `b09-bilreg-api/docs/contexts/igd/igd-01-context.md` | Scope, business gates, external dependencies |
| IGD Domain | `b09-bilreg-api/docs/contexts/igd/igd-02-domain.md` | DR-01…DR-11, aggregates, value objects |
| IGD Design | `b09-bilreg-api/docs/contexts/igd/igd-03-design.md` | Layering, persistence, transaction, error strategy |
| IGD API Contract | `b09-bilreg-api/docs/contexts/igd/igd-04-api-contract.md` | Existing IGD surface to be extended |
| Global standards | `b09-bilreg-api/docs/ENGINEERING.md`, `DATABASE.md`, `NAMING.md`, `WORKFLOW.md` | Conventions |
| Source — BILREG | `b09-bilreg-api/src/bilreg/**` | Existing implementation patterns |
| Source — SMASS | `a043_smass_structuredmedicalassesment_api/**` | Existing implementation patterns |
| Source — Web | `c012_myhospital_web/src/modules/Emergency/**` | Existing IGD screens |

---

# 2. Architectural Basis

## 2.1 Business Context

The business knowledge, business outcome, operational flow, and domain orchestration are owned by
the FEATURE and DOMAIN artifacts:

- FEATURE `b09-bilreg-api/docs/contexts/igd/igd-01-context.md` — IGD Visit scope, business gates,
  operational flow, external dependencies, non-functional requirements.
- DOMAIN `b09-bilreg-api/docs/contexts/igd/igd-02-domain.md` — DR-01…DR-11, aggregates, value
  objects.

Architecture consumes these; it does not restate business flow. This architecture realizes exactly
one FEATURE (IGD Visit).

Ownership note: the business-rule identifiers `BR-01`…`BR-33` used for traceability in this
document are FEATURE-owned. Per the feasibility assessment's FND-001, that register is pending
relocation into the owning FEATURE artifact. This architecture only maps those rules to enforcement
points; it does not define them.

## 2.2 Analysis Input

The analysis input is the FEASIBILITY-ASSESSMENT
`b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-feasibility-assessment.md`. It provides:

- Current State
- Analysis findings (GAP-001…GAP-012, OQ-01…OQ-12)
- Impact / Architecture Impact
- Assumptions (ASM-001…ASM-006)
- Risks (RISK-001…RISK-009)
- Approved decisions D-01…D-15

Derivation:

```text
DOMAIN + FEATURE
        +
FEASIBILITY-ASSESSMENT (D-01…D-15)
        ↓
    ARCHITECTURE
```

This architecture does not repeat the investigation, gap analysis, alternative evaluation, or
decision resolution already completed upstream. It realizes D-01…D-15 as target technical state.

## 2.3 Decision Traceability

### Approved decisions (D-01…D-15) → architecture elements

| Source | Decision (approved in feasibility) | Realized by |
|---|---|---|
| D-01 | Pre-registration creation keyed by `IgdVisitId`; administrative keys optional until link | §5.3 `AssesmentModel` extension; §5.3 `GenerateIgdTriageAssesmentCommand`; §8.2 `SMASS_Assesment` columns; AR-08 (pending-aware validation bypass) |
| D-02 | BILREG synchronously calls SMASS after every successful triage | §5.2 `IgdVisitSmassTask`; §6.2 hook in both triage handlers; §6.2 generate integration |
| D-03 | Configurable ATS→SMASS mapping table (provisional seed values) | §5.3 `TriageConceptMap`; §8.1 `SMASS_TriageConceptMap`; §5.3 mapping resolution algorithm |
| D-04 | One new immutable snapshot per triage, correlated by (`IgdVisitId`, `NoTriage`) | §8.2 filtered unique index; §5.3 idempotency step; §5.3 invariants |
| D-05 | Dedicated IGD Triage Paper; no triage concepts in existing Papers | §8.4 Paper master data; `IgdVisit:SmassTriagePaperId` config |
| D-06 | Configured IGD `LayananId` at creation; triage operator → `UserrId`; `RegId`/`PasienId` nullable until link | §5.3 identity mapping; §6.2 no service lookups; §9.7 config validation |
| D-07 | No rollback; idempotent generation; Pending Generation item; manual MVP retry | §5.2 task aggregate; §8.1 `BILRG_IgdVisitSmassTask`; §5.2 retry command / §6.2 retry strategy; §5.5 SCR-03 |
| D-08 | Separate `RegistrationLinkStatus`; `AggStateEnum` unchanged | §5.3 `RegistrationLinkStatusEnum`; §8.2 column; no change to `AggStateEnum` |
| D-09 | Synchronous `LinkAssessmentByIgdVisitId` after saved registration; link all; idempotent; replace-latest; preserve on void | §5.3 `LinkAssesmentByIgdVisitIdCommand`; §6.3 hook in `IgdVisitAssignRegisterCmd` and `IgdVisitReplaceRegisterCmd`; §5.3 invariants |
| D-10 | `ListByIgdVisitId` is the pending surface; RegId surfaces exclude pending | §8.2 DAL predicate (AR-11); §5.3 `ListAssesmentByIgdVisitIdQuery` and `ListPendingRegistrationAssesmentQuery` |
| D-11 | No historical migration; ordered rollout behind `IgdVisit:EnableSmassIntegration` | §8.5, §8.6, §9.7; toggle default `false` (AR-01) |
| D-12 | SMASS master data owns the mapping; provisional values are approved truth | §8.1 table + §8.4 seed script; §5.3 mapping resolution; §9.8 R-05 |
| D-13 | Existing JWT Bearer for both operations | §9.1 token acquisition; §9.1 `[Authorize]` on the three new endpoints |
| D-14 | BILREG triage = legal record; SMASS = derived document | §5.5 BILREG display; §6.5 SMASS view-model source fields; §5.2 no re-derivation |
| D-15 | Never auto-delete/purge/archive never-registered assessments; provide monitoring | §5.3 `ListPendingRegistrationAssesmentQuery`; §8.5; architecture contains **no** delete/purge path |

### Business-rule enforcement mapping

Business rules are FEATURE-owned (see §2.1). This table maps them to their single enforcement point
in this architecture.

| BR | Enforcement location |
|---|---|
| BR-01 | `GenerateIgdTriageAssesmentCommand` + `AssesmentModel` invariant (§5.3) |
| BR-02, BR-09 | `LinkAssesmentByIgdVisitIdCommand` all-or-nothing key population (§5.3) |
| BR-03 | Only `IgdVisitAssignRegisterCmd` / `IgdVisitReplaceRegisterCmd` invoke the link (§6.3) |
| BR-04 | No link table; `SMASS_Assesment.IgdVisitId` + `NoTriage` only (§8.2) |
| BR-05, BR-19, BR-20 | DAL-level quarantine predicate (§8.2, AR-11) |
| BR-06 | No update path to sections/concepts; only link command mutates (§5.3) |
| BR-07, BR-11 | Filtered unique index + idempotency pre-check (§8.2, §5.3) |
| BR-08 | Identity mapping in generate command (§5.3) |
| BR-10 | Gateway invoked after `trans.Complete()`; exceptions swallowed into task row (§6.6) |
| BR-12 | `IgdVisitSmassTask` Failed state + manual retry command (§5.2) |
| BR-13, BR-14 | `AggStateEnum` never written by the new commands (§5.3, AR-13) |
| BR-15, BR-16, BR-17 | Link command semantics (§5.3) and register handler hook (§6.3) |
| BR-18, BR-32 | Void handler is not modified; no delete/unlink path exists (§5.2) |
| BR-21, BR-22, BR-23 | §8.5, §8.6 |
| BR-24, BR-25, BR-26 | §8.1, §8.4 |
| BR-27 | §9.1 |
| BR-28, BR-29 | §5.2 (BILREG first), §6.5 |
| BR-30 | §5.5 SCR-02/SCR-03, §6.5 view models |
| BR-31, BR-33 | §5.3 monitoring query, §8.5 |

### Planning inputs closed by this architecture

The feasibility assessment left the following as planning inputs. They are technical, not business,
decisions and are closed here so the Planner does not need to decide them.

| Planning input | Closed by |
|---|---|
| Toggle ownership and default state | AR-01 — `IgdVisit:EnableSmassIntegration`, default `false`, owned by BILREG deployment config |
| `LayananId` configuration ownership, validation, invalid-value behaviour | AR-02 — `IgdVisit:SmassLayananId`, validated at call time, invalid → task Failed |
| Pending Generation store mechanism and ownership | AR-03 — new BILREG table `BILRG_IgdVisitSmassTask` (not an outbox) |
| Gateway token acquisition | AR-04 — `Smass:TokenEmail` / `Smass:TokenPass` → `POST {Smass:BaseApiUrl}/Token`, cached |
| Link contract mechanics | §6.3 / §5.3 |
| By-visit query and indexing | §8.2 / §5.3 |
| Authorization wiring | AR-05 — `[Authorize]` on the three new SMASS endpoints only |
| Display correlation and screen placement | §5.5, §6.5 |
| Monitoring ownership | AR-06 — query endpoints only, no new screen; precedent `BedIgd/pakaiBedIgd/orphan` |
| Mapping revision governance | AR-07 — seed-script change; no code change, no Paper change |
| Paper identity value | AR-09 — seeded master data; BILREG config `IgdVisit:SmassTriagePaperId` |

## 2.4 Traceability Matrix

| Business rule (FEATURE-owned) | Workflow capability | Gap / OQ (feasibility) | Architecture element |
|---|---|---|---|
| BR-01 | WF-01/02 | GAP-002, OQ-01 | `AssesmentModel` INV-A1/A2; `GenerateIgdTriageAssesmentCommand` (§5.3) |
| BR-02, BR-09 | WF-03/04 | GAP-010, OQ-06 | `LinkAssesmentByIgdVisitIdCommand` all-key population (§5.3) |
| BR-03 | WF-03/04 | GAP-010 | Register handler hook only (§6.3) |
| BR-04 | WF-01…04 | GAP-005 | `SMASS_Assesment.IgdVisitId`/`NoTriage`; no link table (§8.2) |
| BR-05, BR-19, BR-20 | WF-08 | GAP-011, OQ-07 | DAL quarantine predicate (§8.2 AR-11); `ListAssesmentByIgdVisitIdQuery` |
| BR-06 | WF-01…04 | GAP-005 | INV-A5; only link mutates (§5.3) |
| BR-07, BR-11 | WF-01/02/06 | GAP-005, GAP-008 | `UX_SMASS_Assesment_IgdVisitTriage` + idempotency pre-check (§8.2, §5.3) |
| BR-08 | WF-01/02 | GAP-007, OQ-09 | Identity mapping (§5.3 step 4); `IgdVisit:SmassLayananId` (§9.7) |
| BR-10 | WF-01/02 | GAP-008, OQ-05 | Gateway after `trans.Complete()`, exceptions swallowed (§6.6) |
| BR-12 | WF-06 | GAP-008, OQ-05 | `BILRG_IgdVisitSmassTask`; `IgdVisitSmassTaskRetryCmd`; SCR-03 |
| BR-13, BR-14 | WF-03/04 | GAP-009, OQ-07 | `RegistrationLinkStatusEnum`; `AggStateEnum` untouched (AR-13) |
| BR-15, BR-16, BR-17 | WF-03/04 | GAP-010, OQ-06 | Link command loop + register handler (§5.3, §6.3) |
| BR-18, BR-32 | WF-05 | OQ-12 | Void handler unmodified; no delete path (§5.2, §8.5) |
| BR-21, BR-22, BR-23 | Deployment | GAP-012, OQ-08 | §8.5, §8.6, AR-01 |
| BR-24, BR-25, BR-26 | WF-01/02 | GAP-003, GAP-004, OQ-02 | `SMASS_TriageConceptMap` + seed (§8.1, §8.4, AR-07) |
| BR-27 | All | OQ-10 | §9.1 |
| BR-28, BR-29 | All | OQ-11 | §4.1 P-01; BILREG-first persistence (§6.6) |
| BR-30 | WF-08 | OQ-12 | SCR-02, SCR-03, §6.5 |
| BR-31, BR-33 | WF-07 | OQ-12 | `ListPendingRegistrationAssesmentQuery`; no delete path (§5.3, §8.5) |

---

# 3. Scope

## Included

- BILREG IGD Visit: outbound SMASS gateway, operational task store, configuration, retry surface.
- SMASS: pending-registration creation, registration linking, by-visit query, pending-registration
  monitoring query, mapping master data, persistence changes.
- SMASS master data: dedicated IGD Triage Paper, CustomSection, concepts, mapping seed.
- Frontend: SMASS integration status on the existing IGD Triage workspace (`c012_myhospital_web`).
- Deployment: ordered rollout, activation toggle, rollback posture, no historical backfill.

## Excluded

- The 30-item SMASS-first form and the reverse-direction flow (out of the realized FEATURE).
- Redesign of unrelated SMASS validation, catalogs, reports, formulas, or existing IGD Papers.
- Any SMASS clinical display application: `Smass.Winform` is master-data administration and
  `Smass.Api` is a service, so no screen is added in the SMASS repository (see §5.5, R-06).
- Automatic background retry workers, outbox, or async synchronization (D-07).
- Backfilling `IgdVisitId` into historical assessments (BR-21).
- New authentication scheme, new authorization policy, or new role/permission.

---

# 4. Technical Decisions

Technical Decisions describe how the approved feasibility decisions (D-01…D-15) are realized in the
target architecture. They do not re-evaluate upstream decisions.

## 4.1 Architecture Principles

| # | Principle | Consequence in this design |
|---|---|---|
| P-01 | **BILREG is the legal record; SMASS holds a derived document** (D-14, BR-28, BR-29) | Triage is never rolled back (BR-10). SMASS failures never surface as triage failures. No clinical data is re-derived from SMASS. |
| P-02 | **No distributed transaction, no outbox, no async worker for MVP** (D-02, D-07, BR-12) | Synchronous single-attempt gateway call placed **after** `TransHelper.Complete()`. Failures are rows, not messages. |
| P-03 | **One immutable snapshot per triage event** (D-04, BR-06, BR-07) | (`IgdVisitId`, `NoTriage`) is the correlation and idempotency key on both sides. No update path touches clinical content. |
| P-04 | **Registration linkage is a separate axis from completion state** (D-08, BR-13, BR-14) | `RegistrationLinkStatus` is a new field; `AggStateEnum` is untouched. |
| P-05 | **Pending assessments are quarantined by construction** (D-10, BR-05, BR-19, BR-20) | The quarantine predicate is applied in the SMASS DAL list methods, not per-query, so no existing RegId-based surface can leak a pending row. |
| P-06 | **Mapping is master data, not code** (D-03, D-12, BR-24, BR-25, BR-26) | Mapping lives in `SMASS_TriageConceptMap` + seed script; revision = seed change; the Paper and the gateway are stable. |
| P-07 | **Reuse existing infrastructure and patterns** | BILREG: `IRestClientFactory` (RestSharp), options pattern, Nuna `TransHelper`, `IgdVisitEvent` timeline, `IAuditRepo`. SMASS: MediatR command + Builder + Writer, `TransHelper` inside Writer, Dawn `Guard`. |
| P-08 | **Fail closed on misconfiguration** | Missing/invalid `IgdVisit:SmassLayananId` / `IgdVisit:SmassTriagePaperId`, or a missing mapping row, fails the *generation task* (recorded as Failed) — never the triage. |
| P-09 | **Existing surfaces keep existing behaviour** (BR-21, BR-22, D-11) | No backfill; legacy rows keep `IgdVisitId = ''`, `RegistrationLinkStatus = Registered`; all changes are additive. |
| P-10 | **Deterministic over clever** | No in-request retry, no background worker, no speculative name lookups, no event fan-out from the new commands (see AR-14). |

## 4.2 Architecture Decision Register

| ID | Decision |
|---|---|
| AR-01 | `IgdVisit:EnableSmassIntegration`, default `false`, BILREG-owned |
| AR-02 | `IgdVisit:SmassLayananId` validated at call time; invalid → task Failed, no HTTP call |
| AR-03 | Pending Generation store = new BILREG table `BILRG_IgdVisitSmassTask` (not an outbox) |
| AR-04 | Token from `POST {Smass:BaseApiUrl}/Token`, cached in `IMemoryCache`, attached as Bearer |
| AR-05 | `[Authorize]` added only to the three new SMASS endpoints and the new BILREG controller |
| AR-06 | Monitoring is endpoint-only; no new operator screen (precedent `BedIgd/pakaiBedIgd/orphan`) |
| AR-07 | Mapping revision = seed-script change; no code change, no Paper change |
| AR-08 | Pending creation bypasses `Reg()`, `Layanan()` service lookups and the `AssesmentDate` window check; missing mapping row or unmapped concept → `ArgumentException` (fail closed) |
| AR-09 | IGD Triage Paper is seeded master data; `PaperId` mirrored into `IgdVisit:SmassTriagePaperId`; existence validated via `IPaperDal` |
| AR-10 | No `IGetRegService` / `IGetLayananService` call on generate or link; names come from the link payload |
| AR-11 | Quarantine predicate applied inside `AssesmentDal.ListData(IRegKey)` and `ListData(IPasienKey)` |
| AR-12 | No payload persistence; retry rebuilds from the immutable `BILRG_IgdVisitTriage` row |
| AR-13 | New SMASS commands never call `Finish()`; completion state is untouched |
| AR-14 | New SMASS commands publish no `CreatedAssesmentEvent` / `AddedSectionAssesmentEvent` |
| AR-15 | Link command applies to all assessments of the visit (pending and registered), satisfying BR-15 and BR-17 with one idempotent behaviour |
| AR-16 | No new `IgdEventEnum` member and no `AuditLog` entry for SMASS task transitions; the task row is the operational record |

# 5. Component Responsibilities

Every implementation responsibility has exactly one owner.

## 5.1 Component Summary

| Component | Responsibility |
|---|---|
| IGD Visit (BILREG `IgdContext`) | Own triage record/history, visit lifecycle, outbound SMASS gateway, operational task store, manual retry, BILREG display |
| Structured Medical Assessment (SMASS `AssesmentContext`) | Own IGD-triage assessment creation, `RegistrationLinkStatus`, registration link, pending quarantine, by-visit and monitoring reads |
| SMASS Master Data (`StructureContext`) | Own the IGD Triage Paper and the ATS→SMASS mapping master data |
| BILREG Web Client (`c012_myhospital_web`) | Own display of SMASS generation/link status and manual retry interaction |
| SMASS Clinical UI (outside this workspace) | Own rendering of SMASS source/label fields exposed by API view models (see R-06) |

## 5.2 IGD Visit — BILREG (`IgdContext`)

### Responsibilities

- Own the triage clinical record, triage history, visit lifecycle, and bed/occupancy.
- Own the decision of *when* a SMASS assessment must exist (every triage event).
- Own the outbound SMASS gateway invocation.
- Own the operational record of generation/link outcomes and the manual-retry surface.
- Own BILREG-side display of generation status and `AssessmentId`.

### Owns

- `IgdVisitModel`, `IgdVisitTriageType`, `AtsAssessmentType`, `AtsTriageEngine` (unchanged).
- **New:** `IgdVisitSmassTaskModel`, `SmassTaskTypeEnum`, `SmassTaskStatusEnum`.
- **New:** `IgdVisitSmassTaskDal` / `IgdVisitSmassTaskRepo` / `BILRG_IgdVisitSmassTask`.
- **New:** `ISmassAssessmentGateway` (Application port) and `SmassAssessmentGateway` (Infrastructure adapter).
- **New:** `SmassOptions`, `IgdVisitOptions`, `ISmassTokenService`.

### Depends On

- `IRegRepo` (Admisi) — unchanged, already used by `IgdVisitAssignRegisterCmd`.
- SMASS HTTP API (new, outbound only).

### Exposes

- Extended `POST /api/IgdVisit/{id}/triage` and `POST /api/IgdVisit/{id}/re-triage` responses.
- `GET /api/IgdVisitSmassTask/{igdVisitId}`
- `GET /api/IgdVisitSmassTask/worklist`
- `PATCH /api/IgdVisitSmassTask/retry`

### Explicit Non-Responsibilities

- Does **not** store clinical assessment content.
- Does **not** store `RegistrationLinkStatus` (SMASS-owned, D-08).
- Does **not** maintain a link table (BR-04).

### Domain Model Realization — `IgdVisitSmassTask`

Purpose: operational record of one outbound SMASS operation for one IGD triage event or one
visit-level link operation. It is a **worklist**, not a synchronization mechanism (D-07).

Location: `Bilreg.Domain/IgdContext/IgdVisitSmassTaskFeature/IgdVisitSmassTaskModel.cs`

Aggregate root `IgdVisitSmassTaskModel`:

| Member | Type | Notes |
|---|---|---|
| `IgdVisitSmassTaskId` | `string` | PK, `NunaId.New("IST")` |
| `IgdVisitId` | `string` | Part of business key |
| `NoTriage` | `int` | `> 0` for `Generate`; `0` for `Link` |
| `TaskType` | `SmassTaskTypeEnum` | `Generate = 0`, `Link = 1` |
| `TaskStatus` | `SmassTaskStatusEnum` | `Pending = 0`, `Succeeded = 1`, `Failed = 2` |
| `AssessmentId` | `string` | Empty unless `TaskType = Generate` and `TaskStatus = Succeeded` |
| `RetryCount` | `int` | |
| `LastRetryDate` | `DateTime` | Sentinel `3000-01-01` |
| `ProcessedDate` | `DateTime` | Sentinel `3000-01-01` |
| `LastError` | `string` | Truncated to 500 chars (precedent: `EmrAntrianOutboundQueueModel`) |
| `CrtDate` | `DateTime` | |

Value objects / enums:

- `SmassTaskTypeEnum { Generate, Link }` + `ToCode()` returning `"GENERATE"` / `"LINK"`.
- `SmassTaskStatusEnum { Pending, Succeeded, Failed }` + `ToCode()`.
- `IIgdVisitSmassTaskKey` (`IgdVisitSmassTaskId`).

Invariants:

- INV-T1: (`IgdVisitId`, `NoTriage`, `TaskType`) is unique — one task per operation per event.
- INV-T2: `TaskType = Link` ⇒ `NoTriage = 0`.
- INV-T3: `TaskType = Generate` ⇒ `NoTriage > 0`.
- INV-T4: `MarkSucceeded` is legal only from `Pending` or `Failed`.
- INV-T5: `MarkFailed` is legal only from `Pending` or `Failed`.
- INV-T6: `AssertCanManualRetry()` requires `TaskStatus = Failed`.
- INV-T7: `TaskStatus = Succeeded ∧ TaskType = Generate` ⇒ `AssessmentId` non-empty.
- INV-T8: no state transition deletes the row (D-15/BR-31: retention is governance-owned, never automatic).

Behaviours: `CreatePending`, `MarkSucceeded(assessmentId, processedAt)`,
`MarkFailed(error, failedAt)`, `Rehydrate(...)` — mirroring `EmrAntrianOutboundQueueModel`.

Repository `IIgdVisitSmassTaskRepo` (Application) / `IgdVisitSmassTaskRepo` (Infrastructure):

| Member | Purpose |
|---|---|
| `LoadEntity(IIgdVisitSmassTaskKey)` | Retry command |
| `FindByBusinessKey(igdVisitId, noTriage, taskType)` | Idempotent upsert on triage/link |
| `ListByVisit(igdVisitId)` | SCR-03 + BILREG display |
| `ListProcessable()` | Operator worklist (`TaskStatus = Failed`), ordered by `CrtDate` ascending |
| `SaveChanges(model)` | Upsert |

Auto-registered by the existing Scrutor scan if it implements `ISaveChange<>` / `ILoadEntity<,>`
(precedent: `EmrAntrianOutboundQueueRepo`).

### Application Layer Realization — BILREG

Modified commands:

| Command | Modification |
|---|---|
| `IgdVisitAssessTriageCmd` | After `trans.Complete()`, if toggle enabled: upsert `Generate` task (`Pending`) → invoke gateway → `MarkSucceeded` / `MarkFailed`. Response extended. |
| `IgdVisitReAssessTriageCmd` | Identical modification. |
| `IgdVisitAssignRegisterCmd` | After `trans.Complete()`, if toggle enabled: upsert `Link` task (`NoTriage = 0`, `Pending`) → invoke gateway → `MarkSucceeded` / `MarkFailed`. Response unchanged (`"Done"`). |
| `IgdVisitReplaceRegisterCmd` | Identical modification (BR-17 replace-latest). |
| `IgdVisitVoidCmd` | **Not modified** (BR-18, BR-32). |

New commands:

| Command | Responsibility |
|---|---|
| `IgdVisitSmassTaskRetryCmd` | Load task, `AssertCanManualRetry()`, rebuild payload from the immutable triage row (Generate) or from visit/Reg (Link), invoke gateway, mark result. |
| `IgdVisitSmassTaskProcessCmd` | Optional batch variant of the retry command for the operator worklist. Same semantics, loops failed tasks. (`POST` on the same controller.) |

New queries:

| Query | Responsibility |
|---|---|
| `IgdVisitListSmassTaskQuery` | Tasks for one visit → SCR-03 and BILREG display (BR-30). |
| `IgdVisitSmassWorklistQuery` | Failed tasks across visits, oldest first → operator monitoring (BR-12, BR-33). |

DTO / projection:

- `IgdVisitSmassTaskView { IgdVisitSmassTaskId, IgdVisitId, NoTriage, TaskType, TaskStatus, AssessmentId, RetryCount, LastRetryDate, ProcessedDate, LastError, CrtDate }`
- `IgdVisitAssessTriageResponse` gains `SmassAssessmentId` (`string`, empty when not generated) and `SmassGenerationStatus` (`"Disabled" | "Pending" | "Generated" | "Failed"`).

Payload construction (no payload persistence — AR-12): on retry, the payload is rebuilt by
re-reading `BILRG_IgdVisitTriage` by (`IgdVisitId`, `NoTriage`). That row is immutable (DR-02) and
already contains every required value: six scores, `TriageMethod`, `TriageLevel`, `TriageColor`,
`IsManualOverrideBlack`, `OverrideReason`, `AssessmentDateTime`, `AssessorUserId`. No `PayloadJson`
column is added.

## 5.3 Structured Medical Assessment — SMASS (`AssesmentContext`)

### Responsibilities

- Create and persist IGD-triage-originated assessments in `PendingRegistration` state.
- Own the ATS→SMASS mapping master data.
- Own `RegistrationLinkStatus` and the registration-link operation.
- Own quarantine of pending assessments from all RegId/PasienId surfaces.
- Own the by-visit and pending-registration read surfaces.

### Owns

- `AssesmentModel` (+ 3 new fields), `AssesmentSectionModel`, `AssesmentConceptModel` (unchanged).
- **New:** `RegistrationLinkStatusEnum`, `IIgdVisitKey`.
- **New:** `TriageConceptMapModel`, `ITriageConceptMapKey`.
- **New:** `GenerateIgdTriageAssesmentCommand`, `LinkAssesmentByIgdVisitIdCommand`, `ListAssesmentByIgdVisitIdQuery`, `ListPendingRegistrationAssesmentQuery`.
- **New:** `SMASS_TriageConceptMap` table + `TriageConceptMapDal`.
- **New:** IGD Triage Paper master data.

### Depends On

- `IPaperDal`, `IPaperSectionDal`, `ICustomSectionDal`, `ICustomSectionConceptDal`, `IConceptDal` (existing master data).
- **Not** `IGetRegService` / `IGetLayananService` (see AR-08/AR-10).

### Exposes

- `POST api/Assesment/generateIgdTriage`
- `PATCH api/Assesment/linkIgdVisit`
- `GET api/Assesment/igdVisit/{igdVisitId}`
- `GET api/Assesment/pendingRegistration`

### Domain Model Realization — `Assesment` (extended)

Purpose: a structured clinical assessment document that also carries IGD provenance and
registration-link state.

Location: `Smass.Domain/AssesmentContext/AssesmentAgg/AssesmentModel.cs`

New members:

| Member | Type | Meaning |
|---|---|---|
| `IgdVisitId` | `string` | Empty for legacy / non-IGD rows (BR-21) |
| `NoTriage` | `int` | `0` when `IgdVisitId` is empty |
| `RegistrationLinkStatus` | `RegistrationLinkStatusEnum` | `Registered = 0` (default for all existing rows), `PendingRegistration = 1` |

The model also gains `IIgdVisitKey` implementation.

New value object / enum:

- `RegistrationLinkStatusEnum { Registered = 0, PendingRegistration = 1 }` — file `Smass.Domain/AssesmentContext/AssesmentAgg/RegistrationLinkStatus.cs`.
- `IIgdVisitKey { string IgdVisitId { get; } }` — file `Smass.Domain/ExternalContext/IgdVisitAgg/IIgdVisitKey.cs`.

New invariants:

- INV-A1: `RegistrationLinkStatus = PendingRegistration` ⇒ `IgdVisitId` non-empty.
- INV-A2: `RegistrationLinkStatus = PendingRegistration` ⇒ `RegId`, `PasienId`, `PasienName`, `LayananName` are empty (BR-01).
- INV-A3: `RegistrationLinkStatus = Registered` ⇒ `RegId`, `PasienId`, `LayananId` are all non-empty (BR-02) — **except** legacy rows that predate this feature, which are `Registered` by default and already fully keyed.
- INV-A4: (`IgdVisitId`, `NoTriage`) is unique for all rows where `IgdVisitId <> ''` (BR-07, BR-11).
- INV-A5: clinical content (`ListSection`, concepts, `AssValue`) is never mutated after creation by any command in this feature (BR-06). The **only** sanctioned post-creation mutation is: `RegId`, `PasienId`, `PasienName`, `LayananId`, `LayananName`, `RegistrationLinkStatus`.
- INV-A6: `AggStateEnum` is never written by `GenerateIgdTriageAssesmentCommand` or `LinkAssesmentByIgdVisitIdCommand` (BR-13, BR-14).

Repository / DAL — `IAssesmentDal` gains:

| Member | Purpose |
|---|---|
| `GetData(IIgdVisitKey, int noTriage)` | Idempotency pre-check for generation |
| `ListData(IIgdVisitKey)` | By-visit surface + link selection |
| `ListPendingRegistration()` | Monitoring (BR-33) |

`AssesmentDal.SelectClause()`, `Insert`, `Update` extended with the three new columns. **Quarantine
predicate** added to `ListData(IRegKey)` and `ListData(IPasienKey)` (see AR-11).

### Domain Model Realization — `TriageConceptMap` (new)

Purpose: SMASS-owned master data translating one BILREG triage field/value into one SMASS concept
value (D-03, D-12, GAP-003, GAP-004).

Location: `Smass.Domain/AssesmentContext/TriageConceptMapAgg/TriageConceptMapModel.cs`

Members:

| Member | Type | Meaning |
|---|---|---|
| `TriageFieldCode` | `string` | Source field code (enumeration below) |
| `TriageValue` | `string` | Source value as text (score, level code, colour code, boolean) |
| `ConceptId` | `string` | Target SMASS concept |
| `AssValue` | `string` | Value written to `SMASS_AssesmentConcept.AssValue` |
| `ValueSnomedCtId` | `string` | Optional SNOMED value id |
| `ValueTaxonomy` | `string` | Optional value taxonomy |
| `QualifierValue` | `string` | Optional qualifier |
| `NoUrut` | `int` | Deterministic concept ordering inside a section |

Enumerated `TriageFieldCode` domain (closed set):

| Code | Source in BILREG payload | `TriageValue` domain |
|---|---|---|
| `AIRWAYS` | `AirwaysScore` | `0`, `1`, `2` |
| `BREATHING` | `BreathingScore` | `0`…`5` |
| `CIRCULATION` | `BloodCirculationScore` | `0`…`4` |
| `GCS_EYE` | `GcsEyeScore` | `1`…`4` |
| `GCS_MOTOR` | `GcsMotorScore` | `1`…`6` |
| `GCS_VOICE` | `GcsVoiceScore` | `1`…`5` |
| `GCS_TOTAL` | `GcsEyeScore + GcsMotorScore + GcsVoiceScore` (computed by SMASS) | `3`…`15` |
| `ATS_LEVEL` | `AtsLevel` | `ATS1`…`ATS5` |
| `TRIAGE_COLOR` | `TriageColor` | `RED`, `YELLOW`, `GREEN`, `BLACK` |
| `MANUAL_OVERRIDE_BLACK` | `IsManualOverrideBlack` | `true`, `false` |

`GCS_TOTAL` is derived inside SMASS so the mapping table holds one row per possible total (GAP-004).
`TRIAGE_COLOR` includes `BLACK` because manual-override-black forces `TriageColorEnum.Black` in
`IgdVisitModel.AssessTriage`.

Invariants:

- INV-M1: (`TriageFieldCode`, `TriageValue`) is unique.
- INV-M2: `ConceptId` must exist in `SMASS_Concept` (enforced at generation, not at seed).
- INV-M3: `TriageFieldCode` must belong to the closed set above (enforced by seed + generation-time lookup).

Persistence: `ITriageConceptMapDal : IInsert<>, IUpdate<>, IDelete<>, IGetData<,>, IListData<>` —
read-only usage in this feature (`ListData()` loads the whole table; it is small master data and is
cached per request).

### Application Layer Realization — `GenerateIgdTriageAssesmentCommand`

File: `Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/GenerateIgdTriageAssesmentCommand.cs`
Route: `POST api/Assesment/generateIgdTriage` (feasibility alias: `GenerateAssessment`)

Request fields:

| Field | Required | Notes |
|---|---|---|
| `IgdVisitId` | yes | Correlation key |
| `NoTriage` | yes | `> 0` |
| `PaperId` | yes | Dedicated IGD Triage Paper |
| `LayananId` | yes | Configured IGD value (D-06) |
| `UserrId` | yes | Triage operator (D-06) |
| `AssesmentDate` | yes | `yyyy-MM-dd` |
| `AssesmentTime` | yes | `HH:mm:ss` |
| `AirwaysScore`, `BreathingScore`, `BloodCirculationScore`, `GcsEyeScore`, `GcsMotorScore`, `GcsVoiceScore` | yes | Integer scores |
| `AtsLevel` | yes | `ATS1`…`ATS5` |
| `TriageColor` | yes | `RED`/`YELLOW`/`GREEN`/`BLACK` |
| `IsManualOverrideBlack` | yes | Boolean |

`RegId` and `PasienId` are intentionally absent (D-01, GAP-002).

Response: `{ AssesmentId, IgdVisitId, NoTriage, RegistrationLinkStatus, AssesmentState }`

Algorithm (normative):

1. **Guard** (Dawn `Guard.Argument(...).Member(...)`, precedent `CreateFixAssesmentCommand`): all fields above; `NoTriage > 0`; date/time format.
2. **Idempotency**: `existing = _assesmentDal.GetData(request, request.NoTriage)`. If found → return `existing.AssesmentId` without mutation (BR-07, BR-11).
3. **Resolve mappings**: build the ten (`TriageFieldCode`, `TriageValue`) pairs (§5.3) — `GCS_TOTAL` computed as `GcsEye + GcsMotor + GcsVoice`. For each pair require exactly one `SMASS_TriageConceptMap` row; **if any pair is unresolved → `ArgumentException`** (AR-08: fail closed, no partial clinical snapshot).
4. **Build the assessment**:
   - `_builder.CreateNew()`
   - `.Paper(request)` — validates the Paper exists via `IPaperDal` (AR-09)
   - `.Userr(request)` — `UserrId`/`UserrName` stamped from the request (existing behaviour, no external lookup)
   - Set `LayananId` from request; `LayananName` = `''` — **bypass `IGetLayananService`** (AR-10)
   - Set `AssesmentDate` directly — **bypass the JenisRawat date-window validation** (AR-08)
   - Set `RegId = PasienId = PasienName = ''`
   - Set `IgdVisitId`, `NoTriage`, `RegistrationLinkStatus = PendingRegistration`
5. **Group concepts into Paper sections**: reuse the `SyncChartVitalSignCommand.BuildConceptSectionIndex(paperId)` approach — `IPaperSectionDal.ListData(paperKey)` → filter `SectionType = CustomSection` → `ICustomSectionConceptDal.ListData(...)` → `conceptId → sectionId`. A mapped `ConceptId` absent from the Paper → `ArgumentException` (AR-08).
6. **Build sections** with `AssesmentSectionBuilder.CreateNew` + `AssesmentConceptBuilder.CreateNew(...).SetAssValue(map.AssValue, map.ValueSnomedCtId, map.ValueTaxonomy, map.QualifierValue)`, ordered by `NoUrut`, then `_builder.AddSection(section)` per section.
7. **Persist**: `_writer.Save(ref aggRoot)` — the Writer owns `TransHelper.NewScope()` (existing convention).
8. **No event publish** (AR-14).
9. Return the response.

Post-condition on completion state: whatever `AddSection` produces — i.e. `Drafting`. The command
never calls `Finish()` (AR-13; BR-13, BR-14).

### Application Layer Realization — `LinkAssesmentByIgdVisitIdCommand`

File: `Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/LinkAssesmentByIgdVisitIdCommand.cs`
Route: `PATCH api/Assesment/linkIgdVisit` (feasibility alias: `LinkAssessmentByIgdVisitId`)

Request fields:

| Field | Required | Notes |
|---|---|---|
| `IgdVisitId` | yes | |
| `RegId` | yes | Administrative key 1 (BR-02) |
| `PasienId` | yes | Administrative key 2 |
| `PasienName` | yes | Display-only; BILREG already resolved the `Reg` aggregate |
| `LayananId` | yes | Administrative key 3 — **D-09 supersedes BR-09** |
| `LayananName` | yes | Display-only |

Response: `{ IgdVisitId, LinkedCount, ListAssesmentId[] }`

Algorithm (normative):

1. **Guard**: all six fields non-empty.
2. `list = _assesmentDal.ListData(request /* IIgdVisitKey */)` — **all** assessments correlated to the visit, pending **and** already-registered. This single behaviour satisfies BR-15 (all pending become Registered) and BR-17 (replace-latest over previously linked rows) and is naturally idempotent (D-09). (Decision AR-15.)
3. If `list` is empty → return `LinkedCount = 0` (success; nothing to link).
4. For each assessment, inside **its own** `TransHelper.NewScope()` (D-09 "atomic per-assessment updates"):
   - set `RegId`, `PasienId`, `PasienName`, `LayananId`, `LayananName`
   - set `RegistrationLinkStatus = Registered`
   - do **not** touch `AssesmentState`, sections, or concepts (BR-13, BR-14)
   - `_assesmentDal.Update(model)` — **no** `IGetRegService` / `IGetLayananService` call (AR-10)
5. Collect per-assessment failures. If any assessment fails, the command throws after the loop — partial success persists, and BILREG records a Failed link task and retries (BR-16).
6. No event publish (AR-14).

### Application Layer Realization — Read Queries

`ListAssesmentByIgdVisitIdQuery` — route `GET api/Assesment/igdVisit/{igdVisitId}`

- Returns **every** assessment correlated to the `IgdVisitId` — pending and registered — each carrying `RegistrationLinkStatus`.
- This is the **only** surface on which a pending assessment can be returned (BR-19).
- Backed by `IX_SMASS_Assesment_IgdVisitId`.

`ListPendingRegistrationAssesmentQuery` — route `GET api/Assesment/pendingRegistration`

- Returns assessments with `RegistrationLinkStatus = PendingRegistration`, oldest first, including `IgdVisitId`, `NoTriage`, `AssesmentDate`, `CreateDate`, `PaperId`, `UserrId`.
- Satisfies D-15 / BR-33 monitoring. Read-only; no delete/purge/archive action exists (BR-31).

Unchanged commands: `CreateFixAssesmentCommand` and its Guard are **not** modified. The pending path
is a separate command (BR-22: legacy fully-keyed creation continues to coexist). `AssesmentValidator`
(currently unused) is **not** re-enabled — it rejects empty `RegId`/`PasienId` and would break BR-01.

## 5.4 SMASS Master Data (`StructureContext`)

- Owns the dedicated IGD Triage Paper, its CustomSection and its concepts (D-05).
- Owns `SMASS_TriageConceptMap` content (D-03, D-12, BR-24).
- Seeded via SSDT seed scripts, executed before IGD integration traffic is enabled (BR-23 step 2/3).

## 5.5 BILREG Web Client (`c012_myhospital_web`)

### Screen Inventory

SCR-01 — IGD Triage Workspace:

| | |
|---|---|
| Screen ID | SCR-01 |
| Name | IGD Triage Workspace |
| Purpose | Existing operational workspace for IGD triage, bed pairing and visit lifecycle; becomes the host for SMASS integration status |
| Primary actor | Perawat IGD, Dokter jaga IGD, Admin IGD |
| Workflow | WF-01, WF-02, WF-03, WF-08 |
| Domain | IGD Visit (DR-01…DR-05), BR-30 |
| Implementation | Existing `c012_myhospital_web/src/modules/Emergency/views/TriaseIGD.vue` — **modified** |
| Route | `/app/igd/triase_igd` |

SCR-02 — Triage History Panel:

| | |
|---|---|
| Screen ID | SCR-02 |
| Name | Triage History Panel |
| Purpose | Show every triage event of a visit; per event show the SMASS generation status and the linked `AssessmentId` |
| Primary actor | Perawat IGD, Dokter jaga IGD |
| Workflow | WF-08 |
| Domain | DR-02 (append-only history), BR-30 |
| Implementation | Existing `.../Emergency/components/TriageHistoryPanel.vue` — **modified** |
| Route | Inline inside SCR-01 (desktop right column, tablet/mobile drawer) |

SCR-03 — SMASS Integration Panel (new):

| | |
|---|---|
| Screen ID | SCR-03 |
| Name | SMASS Integration Panel (Integrasi SMASS) |
| Purpose | Show the SMASS generation/link tasks of the selected visit with their status, error and `AssessmentId`, and expose manual retry for failed tasks (D-07, BR-12) |
| Primary actor | Admin IGD, Perawat IGD |
| Workflow | WF-06, WF-08 |
| Domain | BR-10, BR-12, BR-30 |
| Implementation | **New** `.../Emergency/components/IgdSmassIntegrationPanel.vue`, mounted inside SCR-01 |
| Route | Inline inside SCR-01 |

SMASS-side display — scope statement: D-14/BR-30 require SMASS to display `IgdVisitId`,
`NoTriage`, `Generated From IGD Triage` and a `Pending Registration` label. In this workspace SMASS
has **no clinical display application**: `Smass.Winform` is a master-data administration tool
(Paper / Concept / CustomSection) and `Smass.Api` is a service. Therefore:

- No screen is added in the SMASS repository by this architecture.
- The required fields are exposed as API view-model fields (§6.5) on `ListAssesmentByIgdVisitIdQuery`, `ListPendingRegistrationAssesmentQuery`, `ListCatalogQuery` and `ListAssesmentByRegIdQuery`.
- Rendering those fields in the SMASS-consuming clinical UI is out of scope of this artifact and requires no change to any decision in D-01…D-15.
- Consequently **no** frontend screen in this document is attributed to the SMASS repository; this component covers the BILREG web client only. (Recorded as risk R-06.)

### Screen Layout

SCR-01 regions:

```text
Toolbar / TriageChipBar            (existing)
├─ Left: IgdBedPanel                (existing — visit queue + bed board)
└─ Right column (context dependent):
   ├─ Empty state                   (existing)
   ├─ New-visit form                (existing IgdVisitForm + TriaseForm)
   ├─ Triaged pending visit:
   │   ├─ TriageHistoryPanel        (SCR-02 — modified)
   │   └─ IgdSmassIntegrationPanel  (SCR-03 — NEW, below history)
   └─ Paired visit:
       ├─ IgdVisitDetailCard        (existing)
       ├─ TriageHistoryPanel        (SCR-02 — modified)
       ├─ IgdSmassIntegrationPanel  (SCR-03 — NEW)
       └─ BillingPanel              (existing)
Dialogs: discharge, void, register-required, IgdVisitActions, BedMasterSheet (existing)
```

Layout notes:

- SCR-03 is placed directly under SCR-02 in the right column on desktop, and as a section inside the triage drawer on tablet/mobile.
- SCR-03 is **collapsible** and collapsed by default when there is no failed task (IR-03).
- SCR-02 rows gain an inline status slot on the right of the timestamp; no new column is added (keeps the narrow tablet layout intact).

SCR-02 regions:

```text
Row header:  [colour chip] [level] [timestamp]        [SMASS badge] [AssessmentId]
Row detail:  Metode / Level / Re-Triase / Override / Catatan      (existing)
```

SCR-03 regions:

```text
Header:  "Integrasi SMASS"  +  status summary chip (e.g. "1 gagal")
List:    one row per task
           [TaskType label] [NoTriage or "Visit"] [status badge] [AssessmentId] [LastError]
           action: "Coba Ulang"  (retry)
Empty:   "Tidak ada integrasi SMASS untuk kunjungan ini"
```

### Navigation Architecture

The web client uses a single workspace route (`/app/:screen?/:tab?`); there is no route change for
this feature.

| Source | Target | Condition |
|---|---|---|
| SCR-01 (visit selected) | SCR-02 | Existing: visit has at least one triage |
| SCR-01 (visit selected) | SCR-03 | Existing: visit selected; panel renders for every selected visit |
| SCR-03 row `AssessmentId` | none | MVP: `AssessmentId` is displayed as text only, no navigation (SMASS has no web UI in this workspace) |
| SCR-01 | `IgdVisitActions` modal (assign-register) | Existing |
| Operator worklist | none | `GET api/IgdVisitSmassTask/worklist` is an endpoint-only surface; no screen (AR-06) |

### UI State Architecture

SCR-02 row state (per triage event):

```text
Loading
   ↓ data available
History Loaded
   ↓ smass status present
Smass Status Shown        (Generated | Failed | Pending | Disabled)
```

Transition rules:

- `Loading → History Loaded` when `useTriageHistory` resolves.
- `History Loaded → Smass Status Shown` when `useIgdSmassTask(visitId)` resolves and a `Generate` task exists for that `NoTriage`; otherwise the row shows no SMASS slot (backward compatible with visits created before rollout).
- No user-initiated transition; the panel is read-only.

SCR-03 panel state:

```text
Idle
   ↓ visit selected
Loading
   ↓ query resolved
Empty            (no tasks)
   ↓
Ready            (≥1 task)
   ↓ user clicks "Coba Ulang" on a Failed row
Retrying
   ↓ success
Ready  (row now Succeeded)
   ↓ failure
Ready  (row still Failed, error refreshed, toast error)
```

Transition rules:

- `Retrying` is entered only from `Ready` and only for a row with `taskStatus = "Failed"` (IR-01).
- `Retrying` disables all other retry buttons on the panel (IR-02).
- On mutation settle, the `useIgdSmassTask` query cache is invalidated (existing `onSettled` pattern); no optimistic update.

SCR-01 global state additions:

| New state | Type | Source |
|---|---|---|
| `smassPanelOpen` | `boolean` | View-local; auto-opens when at least one task is `Failed` (IR-03) |

### Workspace Modes

SCR-01 already operates in these modes; SMASS visibility is defined per mode.

| Mode | Meaning | SMASS behaviour |
|---|---|---|
| Empty | No visit selected | SCR-02 and SCR-03 not rendered |
| NewVisit | Creating a visit + first triage | SCR-03 not rendered (no visit id yet) |
| Triaged | Visit exists, has ≥1 triage, not paired | SCR-02 + SCR-03 rendered; retry available |
| Paired | Visit paired to a bed | SCR-02 + SCR-03 rendered; retry available |
| Terminal | Discharged / redirected / voided | SCR-03 rendered read-only; **retry remains available** for failed tasks (D-15/BR-18: records persist and remain retryable) |

Restrictions:

- Retry is never available for a task whose `taskStatus` is `Pending` or `Succeeded` (IR-01).
- Nothing in the UI can delete, cancel or archive a task or an assessment (BR-31).

### Interaction Rules

| ID | Condition | Enabled | Disabled | Notes |
|---|---|---|---|---|
| IR-01 | `taskStatus = "Failed"` | "Coba Ulang" | — | Mirrors `IgdVisitSmassTaskModel.AssertCanManualRetry()` (INV-T6) |
| IR-01 | `taskStatus ∈ { "Pending", "Succeeded" }` | — | "Coba Ulang" | Button hidden, not merely disabled, for `Succeeded` |
| IR-02 | Any row is `Retrying` | — | All retry buttons in SCR-03 | Prevents duplicate concurrent retries |
| IR-03 | ≥1 task `Failed` for the selected visit | SCR-03 auto-expanded + summary chip "n gagal" | — | Otherwise collapsed by default |
| IR-04 | `smassGenerationStatus = "Disabled"` on triage response | — | — | No SMASS slot rendered anywhere; feature toggle off (BR-22) |
| IR-05 | `taskType = "Generate"` | Copy `AssessmentId` | — | `AssessmentId` shown only for Generate tasks |
| IR-06 | `taskType = "Link"` | — | `AssessmentId` display | Link is visit-level (`NoTriage = 0`) and has no single AssessmentId |
| IR-07 | Visit is terminal/voided | Retry | — | Allowed; BR-18/BR-32 preserve records, D-07 retry is manual |

### UI Commands

| Screen | UI Action | Command / API |
|---|---|---|
| SCR-01 / SCR-02 | Submit first triage | `POST IgdVisit/{id}/triage` (`useAssignTriase`) |
| SCR-01 / SCR-02 | Submit re-triage | `POST IgdVisit/{id}/re-triage` (`useReTriage`) |
| SCR-01 | Assign register | `PATCH IgdVisit/{id}/register` (`useIgdAssignRegister`) → triggers `LinkAssesmentByIgdVisitIdCommand` server-side |
| SCR-01 | Replace register | `PATCH IgdVisit/{id}/register` (replace variant) → triggers `LinkAssesmentByIgdVisitIdCommand` |
| SCR-03 | Load tasks of visit | `GET IgdVisitSmassTask/{igdVisitId}` (`useIgdSmassTask`) |
| SCR-03 | Retry failed task | `PATCH IgdVisitSmassTask/retry` (`useRetryIgdSmassTask`) |
| Operator (endpoint only) | List failed tasks | `GET IgdVisitSmassTask/worklist` |
| Operator (endpoint only) | List pending-registration assessments | `GET {Smass}/api/Assesment/pendingRegistration` |

The frontend never calls SMASS directly (D-02: backend orchestration; rejected frontend
dual-dispatch alternative).

### Frontend Performance Architecture

| Area | Decision |
|---|---|
| `useIgdSmassTask(visitId)` | TanStack Query; `staleTime` inherited from the module default; **no** polling. Invalidated on visit selection change and on retry settle. |
| Retry mutation | Single-flight via IR-02; no optimistic update (server state is authoritative). |
| SCR-02 join | Pure `computed` over two already-cached queries; no extra request. |
| Rendering | Task counts per visit are small (≤ a few dozen); no virtualization, no pagination. |
| Toasts | Existing `vue-sonner` pattern; one toast per retry outcome. |
| Sentinel handling | `3000-01-01` dates are converted to `null` using the module's existing `isSentinel()` helper in `utils/mappers.ts`. |

## 5.6 Workflow Capability Mapping

| Capability | BILREG | SMASS |
|---|---|---|
| WF-01 Submit first triage | `IgdVisitAssessTriageCmd` + generate hook | `GenerateIgdTriageAssesmentCommand` |
| WF-02 Re-triage | `IgdVisitReAssessTriageCmd` + generate hook | `GenerateIgdTriageAssesmentCommand` |
| WF-03 Assign register | `IgdVisitAssignRegisterCmd` + link hook | `LinkAssesmentByIgdVisitIdCommand` |
| WF-04 Replace register | `IgdVisitReplaceRegisterCmd` + link hook | `LinkAssesmentByIgdVisitIdCommand` |
| WF-05 Void visit | unchanged | unchanged (BR-18) |
| WF-06 Retry failed SMASS operation | `IgdVisitSmassTaskRetryCmd` / `IgdVisitSmassTaskProcessCmd` | same as WF-01/03 |
| WF-07 Monitor pending | `IgdVisitSmassWorklistQuery` | `ListPendingRegistrationAssesmentQuery` |
| WF-08 View assessments for a visit | `IgdVisitListSmassTaskQuery` | `ListAssesmentByIgdVisitIdQuery` |

---

# 6. Integration Design

## 6.1 Collaboration Summary

| Source | Target | Purpose |
|---|---|---|
| BILREG triage handler | SMASS `generateIgdTriage` | Create one immutable assessment snapshot per triage event (D-02, D-04) |
| BILREG register handler | SMASS `linkIgdVisit` | Populate administrative keys and set `Registered` on all assessments of the visit (D-09) |
| BILREG web client | BILREG `IgdVisitSmassTask` endpoints | Display task status, `AssessmentId`, errors; trigger manual retry |
| Operator | BILREG `worklist` + SMASS `pendingRegistration` | Monitoring (AR-06) |
| SMASS | — | SMASS makes **no** outbound call for this feature; `IGetRegService` / `IGetLayananService` are deliberately bypassed (AR-10) |

## 6.2 Integration — Generate SMASS Assessment (BILREG → SMASS)

Purpose: create one immutable SMASS assessment snapshot for one IGD triage event (D-02, D-04).

Trigger: synchronous, inside `IgdVisitAssessTriageCmd` / `IgdVisitReAssessTriageCmd`, after
`trans.Complete()`. Also from `IgdVisitSmassTaskRetryCmd`.

Transport / contract:

| Item | Value |
|---|---|
| Method | `POST` |
| URL | `{Smass:BaseApiUrl}/api/Assesment/generateIgdTriage` |
| Auth | `Authorization: Bearer <jwt>` (D-13) |
| Content | JSON, camelCase |
| Timeout | `Smass:TimeoutSeconds`, default `10` |
| Attempts | exactly 1 per request |

Payload: the request fields in §5.3, plus `PaperId` from `IgdVisit:SmassTriagePaperId` and
`LayananId` from `IgdVisit:SmassLayananId`.

Response parsed as `JSend<GenerateIgdTriageAssesmentResponse>` using the existing `JSend<T>` helper
with `JsonSerializerOptions { PropertyNamingPolicy = CamelCase, PropertyNameCaseInsensitive = true }`.

Error handling:

| Outcome | Action |
|---|---|
| HTTP 2xx + `status = success` | `task.MarkSucceeded(assessmentId)` |
| HTTP 4xx (incl. 400 mapping misconfiguration, 401/403 auth) | `task.MarkFailed(message)` — triage stays committed |
| HTTP 5xx | `task.MarkFailed(message)` |
| Transport exception / timeout | `task.MarkFailed(message)` |
| Config missing (`LayananId`, `PaperId`, `BaseApiUrl`) | `task.MarkFailed("...")` **without** performing an HTTP call |
| Unique-index violation on concurrent retry | Re-read by business key; if the assessment exists → `MarkSucceeded` |

The gateway catches **all** exceptions and returns a result record
(`SmassGatewayResult(bool Success, string? AssessmentId, string? ErrorMessage)`), precedent
`EmrAntrianSendResult`. No exception propagates into the triage handler (BR-10).

Retry strategy: none in-request. Manual retry through `IgdVisitSmassTaskRetryCmd`. Automatic workers
are explicitly deferred (D-07). Idempotency is provided by the SMASS pre-check + filtered unique
index.

## 6.3 Integration — Link Assessments by IGD Visit (BILREG → SMASS)

Purpose: populate the administrative keys and set `Registered` on every assessment correlated to a
visit (D-09).

Trigger: synchronous, inside `IgdVisitAssignRegisterCmd` / `IgdVisitReplaceRegisterCmd`, after
`trans.Complete()`. Also from `IgdVisitSmassTaskRetryCmd`.

Transport / contract:

| Item | Value |
|---|---|
| Method | `PATCH` |
| URL | `{Smass:BaseApiUrl}/api/Assesment/linkIgdVisit` |
| Auth | `Authorization: Bearer <jwt>` |
| Timeout | `Smass:TimeoutSeconds` |
| Attempts | exactly 1 per request |

Payload: `IgdVisitId`, `RegId`, `PasienId`, `PasienName`, `LayananId`, `LayananName`.

Source of values in BILREG: the `RegModel` already loaded by `IgdVisitAssignRegisterCmd`
(`reg.RegId`, `reg.Pasien.PasienId`, `reg.Pasien.PasienName`, `reg.Layanan.LayananId`,
`reg.Layanan.LayananName`). Note `IgdVisitModel.Reg` is only a `RegReff(RegId, PasienId, PasienName)`
and does **not** carry `LayananId` or `LayananName`; these must be read from the loaded `RegModel`.

Error handling: identical to §6.2. Registration is never rolled back (BR-16).

Retry strategy: through the `IgdVisitSmassTask` `Link` task and the D-07 manual retry mechanism.

## 6.4 Integration — Read Surfaces

| Caller | Surface | Purpose |
|---|---|---|
| SMASS → (none) | — | SMASS makes **no** outbound call for this feature (AR-10) |
| BILREG UI | `GET api/IgdVisitSmassTask/{igdVisitId}` | SCR-03 |
| Operator | `GET api/IgdVisitSmassTask/worklist` | BR-33 |
| IGD visit view | `GET {Smass}/api/Assesment/igdVisit/{igdVisitId}` | optional deep link; not required for MVP (BILREG displays its own task rows) |

## 6.5 Interface Contracts — View Models

`IgdSmassTaskView` (BILREG → web):

| Field | Type | Source query |
|---|---|---|
| `igdVisitSmassTaskId` | string | `IgdVisitListSmassTaskQuery` |
| `igdVisitId` | string | " |
| `noTriage` | number | " |
| `taskType` | `"GENERATE" \| "LINK"` | " |
| `taskStatus` | `"PENDING" \| "SUCCEEDED" \| "FAILED"` | " |
| `assessmentId` | string | " |
| `retryCount` | number | " |
| `lastRetryDate` | ISO string \| null | " (sentinel `3000-01-01` → `null`) |
| `processedDate` | ISO string \| null | " |
| `lastError` | string | " |
| `crtDate` | ISO string | " |

Frontend Zod schema added to `src/modules/Emergency/types/contract.ts`.

`AssignTriaseResponse` (extended):

| Field | Type | Source |
|---|---|---|
| existing fields | — | `POST IgdVisit/{id}/triage`, `POST IgdVisit/{id}/re-triage` |
| `smassAssessmentId` | string \| null | " |
| `smassGenerationStatus` | `"DISABLED" \| "PENDING" \| "GENERATED" \| "FAILED"` | " |

`TriageHistoryItem` (extended for SCR-02):

| Field | Type | Source |
|---|---|---|
| existing fields | — | `GET IgdVisit/{id}/triage-history` |
| `smassAssessmentId` | string \| null | join with `IgdVisitListSmassTaskQuery` on (`igdVisitId`, `noTriage`, `GENERATE`) — performed client-side in a `computed`, no new endpoint |
| `smassGenerationStatus` | enum \| null | " |

`AssesmentIgdView` (SMASS):

| Field | Type | Source query |
|---|---|---|
| `assesmentId` | string | `ListAssesmentByIgdVisitIdQuery`, `ListPendingRegistrationAssesmentQuery`, `ListCatalogQuery`, `ListAssesmentByRegIdQuery` |
| `igdVisitId` | string | " |
| `noTriage` | number | " |
| `registrationLinkStatus` | number | " |
| `registrationLinkStatusLabel` | `"Pending Registration" \| "Registered"` | " |
| `sourceLabel` | `"Generated From IGD Triage"` when `igdVisitId` non-empty, else `""` | " |
| `assesmentDate`, `paperId`, `paperName`, `layananId`, `layananName`, `userrId`, `assesmentState` | — | " |

`PendingRegistrationView` (SMASS monitoring):

| Field | Source |
|---|---|
| `assesmentId`, `igdVisitId`, `noTriage`, `assesmentDate`, `createDate`, `paperId`, `paperName`, `userrId`, `ageDays` (computed) | `ListPendingRegistrationAssesmentQuery` |

## 6.6 BILREG Execution Ordering Rule (normative)

```text
1. guard + load aggregate
2. domain behaviour (AssessTriage / AssignRegister / ReplaceRegister)
3. using var trans = TransHelper.NewScope();
4.     repo.SaveChanges(visit);
5. trans.Complete();                       // ← legal record is committed here
6. if (!IgdVisitOptions.EnableSmassIntegration) return response;   // no task row
7. task = repo.FindByBusinessKey(...) ?? IgdVisitSmassTaskModel.CreatePending(...)
8. try { result = await gateway.<Operation>(payload); task.MarkSucceeded(...); }
   catch (Exception ex) { task.MarkFailed(ex.Message, now); }      // BR-10: never rethrow
9. repo.SaveChanges(task);                 // separate write, no transaction over HTTP
10. return response (with Smass status fields for triage commands)
```

The HTTP call is **outside** every `TransHelper` scope (precedent: `EmrAntrianOutboundProcessor`).
Exactly **one** attempt per request; no in-request retry (D-07).

---

# 7. Data Ownership

| Data | Owner |
|---|---|
| Triage clinical record, triage history, visit lifecycle, bed/occupancy | IGD Visit (BILREG `IgdContext`) |
| SMASS generation/link operational task rows (`BILRG_IgdVisitSmassTask`) | IGD Visit (BILREG) |
| SMASS assessment document, clinical content, IGD provenance fields, `RegistrationLinkStatus` | Structured Medical Assessment (SMASS `AssesmentContext`) |
| ATS→SMASS mapping master data (`SMASS_TriageConceptMap`) | SMASS Master Data (`StructureContext`) |
| IGD Triage Paper, sections, concepts | SMASS Master Data (`StructureContext`) |
| SMASS display rendering of source/label fields | SMASS clinical UI (outside this workspace) |
| SMASS service credentials custody (`Smass:TokenEmail` / `Smass:TokenPass`) | BILREG operations |
| SMASS token validation and endpoint authorization | SMASS |

Avoid shared ownership: BILREG does not store `RegistrationLinkStatus`; SMASS does not own the
triage record or the operational task store.

---

# 8. Database Design

## 8.1 New Tables

| Table | Purpose |
|---|---|
| `BILRG_IgdVisitSmassTask` | BILREG operational generation/link task store (D-07) |
| `SMASS_TriageConceptMap` | SMASS-owned ATS→SMASS mapping master data (D-03, D-12) |

### `BILRG_IgdVisitSmassTask` (BILREG — new table)

Script: `Bilreg.SqlDb/IgdContext/IgdVisitSmassTaskFeature/BILRG_IgdVisitSmassTask.sql`

| Column | Type | Notes |
|---|---|---|
| `IgdVisitSmassTaskId` | `VARCHAR(14)` | PK |
| `IgdVisitId` | `VARCHAR(12)` | NOT NULL |
| `NoTriage` | `INT` | NOT NULL, `0` for link tasks |
| `TaskType` | `INT` | NOT NULL |
| `TaskStatus` | `INT` | NOT NULL |
| `AssessmentId` | `VARCHAR(13)` | NOT NULL `DEFAULT('')` |
| `RetryCount` | `INT` | NOT NULL `DEFAULT(0)` |
| `LastRetryDate` | `DATETIME` | NOT NULL `DEFAULT('3000-01-01')` |
| `ProcessedDate` | `DATETIME` | NOT NULL `DEFAULT('3000-01-01')` |
| `LastError` | `VARCHAR(500)` | NOT NULL `DEFAULT('')` |
| `CrtDate` | `DATETIME` | NOT NULL |

Primary key: `PK_BILRG_IgdVisitSmassTask (IgdVisitSmassTaskId)`

Unique constraints:

- `UX_BILRG_IgdVisitSmassTask_BusinessKey (IgdVisitId, NoTriage, TaskType)` — enforces INV-T1 and BR-11.

Indexes:

- `IX_BILRG_IgdVisitSmassTask_Visit (IgdVisitId) INCLUDE (NoTriage, TaskType, TaskStatus, AssessmentId)`
- `IX_BILRG_IgdVisitSmassTask_Status (TaskStatus, CrtDate)`

Soft delete: **none** — rows are never deleted by the feature (INV-T8, D-15).

Audit strategy: no `AuditLog` entry per task (it is operational, not clinical). Correlation and
legality remain in `BILRG_IgdVisitTriage` (D-14). The IGD operational timeline
(`BILRG_IgdVisitEvent`) is **not** extended for this feature: the task row already records attempt,
error, retry count and timestamps; adding events would duplicate the operational record.
(Decision AR-16.)

Backfill: none (BR-21).

### `SMASS_TriageConceptMap` (SMASS — new table)

Script: `Smass.Db/Tables/SMASS_TriageConceptMap.sql` (registered in `Smass.Db.sqlproj` `<Build>`
ItemGroup, precedent `SMASS_LayananSmf.sql`)

| Column | Type | Notes |
|---|---|---|
| `TriageFieldCode` | `VARCHAR(30)` | NOT NULL `DEFAULT('')` |
| `TriageValue` | `VARCHAR(20)` | NOT NULL `DEFAULT('')` |
| `ConceptId` | `VARCHAR(6)` | NOT NULL `DEFAULT('')` |
| `AssValue` | `VARCHAR(255)` | NOT NULL `DEFAULT('')` |
| `ValueSnomedCtId` | `VARCHAR(18)` | NOT NULL `DEFAULT('')` |
| `ValueTaxonomy` | `VARCHAR(30)` | NOT NULL `DEFAULT('')` |
| `QualifierValue` | `VARCHAR(128)` | NOT NULL `DEFAULT('')` |
| `NoUrut` | `INT` | NOT NULL `DEFAULT(0)` |

Primary key: `PK_SMASS_TriageConceptMap (TriageFieldCode, TriageValue)`

Indexes: `IX_SMASS_TriageConceptMap_ConceptId (ConceptId)`

Soft delete / audit: none (pure master data).

Backfill: seed only (§8.4). No historical clinical migration (BR-21).

## 8.2 Modified Tables

| Table | Change |
|---|---|
| `SMASS_Assesment` | Additive columns `IgdVisitId`, `NoTriage`, `RegistrationLinkStatus`; filtered unique constraint; by-visit index; DAL quarantine predicate |

### `SMASS_Assesment` (SMASS — altered)

Script: `Smass.Db/AssesmentContext/SMASS_Assesment.sql` (altered; additive only)

| New column | Type | Notes |
|---|---|---|
| `IgdVisitId` | `VARCHAR(12)` | NOT NULL `DEFAULT('')` |
| `NoTriage` | `INT` | NOT NULL `DEFAULT(0)` |
| `RegistrationLinkStatus` | `INT` | NOT NULL `DEFAULT(0)` (`Registered`) |

Primary key: unchanged `AssesmentId`.

Foreign keys: none (IGD lives in another database; BR-04 forbids a link table).

Unique constraints:

- `UX_SMASS_Assesment_IgdVisitTriage (IgdVisitId, NoTriage)` **filtered**: `WHERE IgdVisitId <> ''` — enforces INV-A4 / BR-07 / BR-11.

Indexes:

- `IX_SMASS_Assesment_IgdVisitId (IgdVisitId) INCLUDE (AssesmentId, NoTriage, RegistrationLinkStatus)` — required by D-10 (by-visit query, idempotency check, link selection).

Soft delete strategy: unchanged — `AssesmentState = 3 (Deleted)`. No new column.

Audit strategy: unchanged — the table has no audit columns; `CreateDate` / `FinishDate` only. No
audit columns are added; IGD provenance is carried by `IgdVisitId`, `NoTriage` and `UserrId` (D-14).

**Quarantine (AR-11):** the predicate `RegistrationLinkStatus = 0 /* Registered */` is added inside
`AssesmentDal.ListData(IRegKey)` and `AssesmentDal.ListData(IPasienKey)`. Because every pre-existing
row defaults to `Registered`, behaviour for existing data is byte-identical (BR-22), and **no**
RegId/PasienId surface — including `ListCatalogQuery` (`api/Assesment/Catalog/{regId}`),
`ListAssesmentByRegIdQuery`, `ListAssesmentByPasienIdQuery`, OFTA document flows and reports — can
ever return a pending row (BR-05, BR-19). Pending rows have empty `RegId`/`PasienId` anyway; the
predicate makes the guarantee explicit and construction-level.

## 8.3 Relationships

- `SMASS_Assesment.IgdVisitId` + `SMASS_Assesment.NoTriage` correlate an assessment to a BILREG
  triage event. These are **operational correlation keys only**: there is no cross-database foreign
  key and no link table (BR-04). IGD lives in another database.
- `SMASS_TriageConceptMap.ConceptId` references existing `SMASS_Concept` (validated at generation,
  not enforced as a physical FK — INV-M2).
- `SMASS_TriageConceptMap.TriageFieldCode` + `TriageValue` are the mapping key; the target concept
  is attached to the IGD Triage Paper's CustomSection via existing `SMASS_CustomSectionConcept`
  master data (D-05).
- `BILRG_IgdVisitSmassTask` has no physical relationship to the SMASS database; it references
  `BILRG_IgdVisitTriage` logically by (`IgdVisitId`, `NoTriage`).

## 8.4 Master Data and Seeds

IGD Triage Paper master data (SMASS — new data):

- `SMASS_Paper` — one new Paper (D-05). Suggested seeded id `PP-001-IGDTR`; the literal value is master data and must be mirrored into BILREG `IgdVisit:SmassTriagePaperId`.
- `SMASS_PaperSection` — `SectionType = CustomSection` rows only.
- `SMASS_CustomSection` + `SMASS_CustomSectionConcept` — one or more custom sections carrying the triage concepts referenced by `SMASS_TriageConceptMap`.
- No existing IGD Paper is modified (D-05).

Seed scripts:

| Script | Registration | Content |
|---|---|---|
| `Smass.Db/DataSeeds/IgdTriagePaperDataSeed.sql` | `<None>` (manual, precedent `PaperDataSeed.sql`) | Paper + PaperSection + CustomSection + CustomSectionConcept, idempotent `DELETE` then `INSERT` |
| `Smass.Db/DataSeeds/TriageConceptMapDataSeed.sql` | `<None>` (manual, precedent `PreferenceDataSeed.sql`) | One row per (`TriageFieldCode`, `TriageValue`) from §5.3, values derived from existing `SMASS_Concept` / `SMASS_ConceptPreference` seeds (D-03, D-12) |

Mapping revision = edit + re-run the seed script. No code change, no Paper change (BR-26, AR-07).

## 8.5 Migration Considerations

Migration strategy (BR-21, BR-23):

1. **DB first.** Add `BILRG_IgdVisitSmassTask` (+ indexes). Alter `SMASS_Assesment` additively (`DEFAULT` values mean existing rows need no update). Create `SMASS_TriageConceptMap`.
2. **Seed SMASS master data.** IGD Triage Paper (+ sections/concepts) and `SMASS_TriageConceptMap` rows.
3. **No backfill.** Existing `SMASS_Assesment` rows keep `IgdVisitId = ''`, `NoTriage = 0`, `RegistrationLinkStatus = Registered`. No clinical data is migrated.

Rollback strategy:

- Setting `IgdVisit:EnableSmassIntegration = false` stops all new generation/link attempts immediately; existing rows and assessments are untouched.
- The SMASS schema change is additive and forward-compatible; no rollback of `SMASS_Assesment` columns is required to disable the feature.
- Assessments already created remain valid documents (D-15); they are never deleted by rollback.

## 8.6 Rollout Strategy (normative order)

1. Deploy database changes (BILREG + SMASS).
2. Deploy SMASS application changes.
3. Verify legacy assessment behaviour (create/finish/catalog/OFTA unchanged).
4. Enable IGD integration by setting `IgdVisit:EnableSmassIntegration = true`.
5. Monitor: generation success/failure, link success/failure, `api/IgdVisitSmassTask/worklist`, `api/Assesment/pendingRegistration`.

Toggle default is **`false`** (AR-01). With the toggle off, no task row is created and no HTTP call
is made; triage and registration behave exactly as today (BR-22).

## 8.7 Concurrency and Idempotency

Concurrency considerations:

| Scenario | Handling |
|---|---|
| Two requests generate for the same (`IgdVisitId`, `NoTriage`) | SMASS pre-check plus `UX_SMASS_Assesment_IgdVisitTriage`; on violation the gateway re-reads and treats it as success |
| Retry while a previous request is still in flight | BILREG `UX_BILRG_IgdVisitSmassTask_BusinessKey` prevents duplicate task rows; `MarkSucceeded`/`MarkFailed` are guarded by status (INV-T4/T5) |
| Link racing with a new triage for the same visit | Link selects by `IgdVisitId` only; a triage arriving after the link creates a new pending assessment, which is linked by the next `AssignRegister`/`ReplaceRegister` invocation or by retry |
| Bed/visit concurrency | Unchanged (`BedIgd` CAS); the gateway call happens after the visit transaction commits |

Idempotency requirements:

| Operation | Key | Mechanism |
|---|---|---|
| Generate | (`IgdVisitId`, `NoTriage`) | SMASS pre-check returning the existing `AssesmentId` + filtered unique index |
| Link | `IgdVisitId` | Set-based, repeatable assignment of the same values; `Registered` is already-set tolerant |
| Task upsert | (`IgdVisitId`, `NoTriage`, `TaskType`) | Unique index + `FindByBusinessKey` |

---

# 9. Cross-Cutting Concerns

## 9.1 Security

Authentication (AR-04):

- BILREG obtains a JWT from SMASS's existing `POST {Smass:BaseApiUrl}/Token` with body `{ "email": <Smass:TokenEmail>, "pass": <Smass:TokenPass> }` (`UsmanLoginCommand` shape: `Email`, `Pass`).
- The endpoint returns a raw JWT string; BILREG wraps it with the existing `JSend<T>`/`DeserializeOrThrow` helper pattern used by `UsmanGetTokenService`.
- The token is cached in `IMemoryCache` (already registered) with absolute expiry = token `exp` − 60 s. Cache key `SmassToken`.
- The token is attached as `Authorization: Bearer <token>` (first outbound Bearer in BILREG; RestSharp `AddHeader`).
- SMASS validates with its existing `Jwt` configuration (`Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`) — no new validation scheme (D-13).

Authorization (AR-05):

- `[Authorize]` (bare, no policy) is added **only** to the three new SMASS endpoints in `AssesmentController`. Existing SMASS actions keep their current (unauthenticated) behaviour, and existing BILREG controllers keep their existing `[Authorize]`, so no existing consumer changes (BR-22).
- New BILREG endpoints on `IgdVisitSmassTaskController` carry `[Authorize]`, matching all other IGD controllers.
- No new policy, role, or permission is introduced.

Permission boundaries:

| Surface | Allowed caller |
|---|---|
| `api/Assesment/generateIgdTriage` | Authenticated BILREG service identity (IGD triage operator's context is carried as `UserrId`, not as the JWT identity) |
| `api/Assesment/linkIgdVisit` | Authenticated BILREG service identity |
| `api/Assesment/igdVisit/{id}` | Authenticated (IGD view) |
| `api/Assesment/pendingRegistration` | Authenticated (operator / monitoring) |
| `api/IgdVisitSmassTask/*` | Authenticated BILREG user (IGD roles as for other IGD endpoints) |

Security ownership:

- BILREG owns credential custody (`Smass:TokenEmail`, `Smass:TokenPass`) and the outbound token lifecycle.
- SMASS owns token validation and endpoint authorization.
- `UserrId` in the SMASS payload is the **triage operator** supplied by BILREG (D-06) and is stamped verbatim by `AssesmentBuilder.Userr`, consistent with existing SMASS behaviour.

## 9.2 Validation Ownership

Client validation (c012_myhospital_web):

- Triage score ranges and defaults — **already owned** by `useTriaseCalculator` and `contract.ts` Zod schemas; unchanged.
- Required `userId` — unchanged.
- Retry: `IgdVisitSmassTaskId` non-empty; no other client rule.
- Format-only rendering of `AssessmentId`, `LastError`, timestamps.

Server validation (BILREG):

- Triage score ranges and visit gates — unchanged (`IgdVisitAssessTriageCmd` guards, `IgdVisitModel`).
- `Smass` configuration validity at call time (AR-02).
- Task state machine (INV-T4…T7).
- Authorization (`[Authorize]`).

Server validation (SMASS):

- All BR rules: BR-01 (empty keys valid only for pending IGD), BR-02 (all-or-nothing keys), BR-07/BR-11 (idempotency), INV-A1…INV-A6.
- Mapping completeness (AR-08).
- Paper existence.

Prohibited duplication:

- The client must **not** recompute ATS level/colour for submission (`AtsTriageEngine` is authoritative; `triageRules.ts` is documentation only).
- The client must **not** decide whether an assessment is pending/registered; it renders `registrationLinkStatus` from the server.

## 9.3 Audit and Logging

- BILREG triage remains the legal record and audit source (D-14, BR-28); SMASS assessments are derived documents (BR-29).
- No `AuditLog` entry is added for SMASS task transitions; the task row is the operational record (AR-16).
- `BILRG_IgdVisitEvent` is not extended for this feature (AR-16).
- `SMASS_Assesment` keeps its existing audit strategy (`CreateDate` / `FinishDate`); provenance is carried by `IgdVisitId`, `NoTriage`, `UserrId`.

## 9.4 Observability and Monitoring

- BILREG: `GET api/IgdVisitSmassTask/worklist` lists failed tasks across visits, oldest first (BR-12, BR-33).
- SMASS: `GET api/Assesment/pendingRegistration` lists `PendingRegistration` assessments, oldest first, including `IgdVisitId`, `NoTriage`, `AssesmentDate`, `CreateDate`, `PaperId`, `UserrId` (D-15, BR-33).
- Monitoring is endpoint-only; no new operator screen (AR-06; precedent `BedIgd/pakaiBedIgd/orphan`).
- Rollout monitoring covers generation success/failure, link success/failure, the worklist, and pending registration (§8.6 step 5).

## 9.5 Performance

- One extra HTTP round trip per triage and per registration assignment, bounded by `Smass:TimeoutSeconds` (default 10 s). Occurring after commit, it does not hold a database transaction.
- `SMASS_TriageConceptMap` is small master data; loaded once per generate request.
- `IX_SMASS_Assesment_IgdVisitId` covers the by-visit query, the idempotency probe and the link selection.
- No new hot-path query on `SMASS_Assesment` beyond the existing `RegId` index usage.
- Frontend: see §5.5 frontend performance architecture.

## 9.6 Configuration Ownership

| Key | Section / class | Owner | Default | Validation |
|---|---|---|---|---|
| `Smass:BaseApiUrl` | `SmassOptions` (new) | BILREG ops | empty | Call-time: empty → task Failed, no HTTP call |
| `Smass:TokenEmail` | `SmassOptions` | BILREG ops | empty | Call-time: empty → task Failed |
| `Smass:TokenPass` | `SmassOptions` | BILREG ops | empty | Call-time: empty → task Failed |
| `Smass:TimeoutSeconds` | `SmassOptions` | BILREG ops | `10` | Range 1–120 |
| `IgdVisit:EnableSmassIntegration` | `IgdVisitOptions` (new) | BILREG ops | `false` (AR-01) | bool |
| `IgdVisit:SmassLayananId` | `IgdVisitOptions` | BILREG ops (D-06) | empty | Call-time: empty → task Failed (AR-02) |
| `IgdVisit:SmassTriagePaperId` | `IgdVisitOptions` | BILREG ops, mirrored from SMASS seed | empty | Call-time empty → task Failed; SMASS also validates existence via `IPaperDal` |

Options are registered with `.Configure<T>(configuration.GetSection(T.SECTION_NAME))` in
`Bilreg.Api/Configurations/InfrastructureService.cs`, following the `Emr`/`HiDok`/`Jkn`/`Usman`
precedent. `IgdVisitOptions` uses `SECTION_NAME = "IgdVisit"`; `SmassOptions` uses
`SECTION_NAME = "Smass"`.

**No** `ValidateOnStart()` is used for these options: a missing value must degrade the integration,
not prevent the API from starting (a half-configured deployment must still serve IGD triage).

## 9.7 Architectural Risks and Residuals

### R-01 — Synchronous gateway adds latency to triage

- **Impact:** Triage response time increases by one HTTP call (bounded by `Smass:TimeoutSeconds`, default 10 s).
- **Mitigation:** Call placed after commit, outside any transaction; single attempt; short timeout; feature toggle for immediate disable.
- **Residual:** Low — triage is not blocked on database resources, and a failure does not fail the triage.

### R-02 — Generated assessments remain `Drafting` and are editable in SMASS

- **Impact:** BR-06 declares clinical content immutable, but `AddSection` leaves `AssesmentState = Drafting`, which SMASS treats as editable.
- **Mitigation:** AR-13 forbids the new commands from calling `Finish()`; the generation path mirrors the existing `SyncChartVitalSignCommand` precedent (also `Drafting`). Minimizing new lifecycle behaviour protects BR-13/BR-14 and avoids triggering the Finished hook for a pre-registration record.
- **Residual:** Medium — a SMASS user could open and edit a pending assessment. Closing this requires a decision on completion state and hook behaviour that is outside D-01…D-15. Flagged for a follow-up decision; it does not block planning.

### R-03 — No event fan-out from the new commands (AR-14)

- **Impact:** Reflection, diagnosa-terpusat, time-table and EMR "assesment created" hooks do not run for IGD-generated assessments, even after linking.
- **Mitigation:** Deliberate: publishing `CreatedAssesmentEvent`/`AddedSectionAssesmentEvent` would push pending-registration records into RegId-keyed downstream workflows (violating BR-05/BR-19) and would fire repeatedly on idempotent link retries.
- **Residual:** Medium — downstream SMASS features that rely on those hooks will not see IGD assessments. Requires a separate decision if/when those integrations must include IGD triage.

### R-04 — Date-window validation bypassed for pending creation (AR-08)

- **Impact:** A heavily backdated triage timestamp is accepted for a pending assessment, whereas the standard path would reject it.
- **Mitigation:** The window depends on `JenisRawat`, which is unavailable before registration. Bypass applies only to the pending path; the created snapshot carries the BILREG legal timestamp.
- **Residual:** Low.

### R-05 — Provisional mapping values may need clinical revision

- **Impact:** Concept/value mapping may be clinically revised after deployment.
- **Mitigation:** D-12/BR-25/BR-26 — values are seed data; revision is a seed-script change requiring no code or Paper change (AR-07).
- **Residual:** Accepted by D-12.

### R-06 — SMASS has no clinical display surface in this workspace

- **Impact:** The "Generated From IGD Triage" and "Pending Registration" display required by D-14 cannot be delivered inside the SMASS repository.
- **Mitigation:** All required fields are exposed as API view-model fields (§6.5) for consuming UIs; BILREG-side display is fully specified (SCR-02, SCR-03).
- **Residual:** Medium — the consuming SMASS UI must be updated elsewhere; no approved decision is affected.

### R-07 — `LayananId` and `PasienName`/`LayananName` are not validated against SMASS master data

- **Impact:** A misconfigured `IgdVisit:SmassLayananId` is persisted without existence validation (AR-10 deliberately avoids the Billing HTTP hop).
- **Mitigation:** Configuration is ops-owned (§9.6); link-time values come from the validated BILREG `Reg` aggregate.
- **Residual:** Low — already an accepted residual risk in the feasibility assessment.

### R-08 — Never-registered assessments accumulate

- **Impact:** Long-lived `PendingRegistration` rows (D-15).
- **Mitigation:** `ListPendingRegistrationAssesmentQuery` provides monitoring (BR-33); no auto-delete path exists.
- **Residual:** Accepted by D-15; future retention is governance-owned.

---

# 10. Implementation Constraints

Rules implementers must follow.

## 10.1 Backend Constraints (BILREG)

- Place the gateway call **after** `trans.Complete()` in `IgdVisitAssessTriageCmd`, `IgdVisitReAssessTriageCmd`, `IgdVisitAssignRegisterCmd`, `IgdVisitReplaceRegisterCmd`. Never inside a `TransHelper` scope.
- Catch every exception; never rethrow into the triage handler (BR-10).
- Do **not** modify `IgdVisitVoidCmd` (BR-18).
- `ISmassAssessmentGateway` is declared in `Bilreg.Application` (port) and implemented in `Bilreg.Infrastructure` (adapter), following `IDoctorServiceGateway` / `IWardAccommodationGateway`. It needs one explicit `AddScoped` line; DAL/repo types are picked up by the existing Scrutor scan.
- Use `IRestClientFactory` (RestSharp). Do **not** introduce `IHttpClientFactory` or Polly.
- Register `SmassOptions` and `IgdVisitOptions` in `InfrastructureService.cs`; do not use `ValidateOnStart` for them.
- Do not add SMASS columns to `BILRG_IgdVisitTriage` or `BILRG_IgdVisit`.

## 10.2 Backend Constraints (SMASS)

- New commands follow the MediatR one-file pattern: request + response + handler in `UseCases/Commands/`.
- Persist only through the existing `AssesmentWriter` (it owns `TransHelper.NewScope()`).
- Do **not** call `IGetRegService` or `IGetLayananService` on the pending or link path (AR-10).
- Do **not** publish `CreatedAssesmentEvent` / `AddedSectionAssesmentEvent` (AR-14).
- Do **not** modify `CreateFixAssesmentCommand` or re-enable `AssesmentValidator`.
- Additive DDL only; register new scripts in `Smass.Db.sqlproj`.
- `[Authorize]` on the three new endpoints only.

## 10.3 Frontend Constraints

- No direct call from the web client to SMASS (D-02).
- Extend existing Zod schemas in `src/modules/Emergency/types/contract.ts`; add hooks to `queries/EmergencyService.ts`; add query keys to `src/core/api/queryConfigs.ts` under `queryKeys.emergency`.
- SCR-03 is a presentational component receiving props from `TriaseIGD.vue`, matching the module's container/presentational convention — except that it owns its own `useIgdSmassTask` query (precedent: `TriageHistoryPanel.vue`).
- No Pinia store is introduced; server state stays in TanStack Query, UI state stays local to the view.

## 10.4 Critical Invariants

1. Triage/register persistence commits before any outbound call (BR-10, BR-16).
2. (`IgdVisitId`, `NoTriage`) resolves to exactly one SMASS assessment (BR-07, BR-11).
3. Administrative keys are populated all-or-nothing (BR-02).
4. `AggStateEnum` is never written by this feature (BR-13, BR-14).
5. Pending assessments are unreachable from every RegId/PasienId surface (BR-05, BR-19).
6. Assessments are never deleted, purged, archived or unlinked by this feature (BR-18, BR-31, BR-32).

## 10.5 Prohibited Shortcuts

- Reusing `CreateFixAssesmentCommand` with empty keys instead of the dedicated pending command.
- Writing the gateway call inside the existing `TransHelper` scope.
- Adding a `PayloadJson` column and replaying stale snapshots instead of re-reading the immutable triage row.
- Filtering pending assessments in each query instead of at the DAL list level.
- Hard-coding the ATS→SMASS mapping in the gateway or the Paper.
- Frontend dual dispatch to SMASS.
- Retrying inside the request, or adding an automatic retry worker.
- Backfilling `IgdVisitId` into historical assessments.

---

# 11. Acceptance Conditions

Conditions that must be true for implementation to be considered architecturally complete.

## 11.1 Architectural Readiness

```text
Ready For Planning: YES
```

Blocking issues: none. GAP-001…GAP-012 and OQ-01…OQ-12 are resolved and D-01…D-15 are approved.
Every planning-level input listed in the feasibility assessment is closed in §2.3 by an explicit
architecture decision (AR-01…AR-16).

## 11.2 Acceptance Conditions

1. Every triage and re-triage event produces exactly one SMASS assessment correlated by (`IgdVisitId`, `NoTriage`), or a recorded `Failed` task when SMASS is unavailable (D-02, D-04, D-07).
2. Triage and registration persistence are committed before and independent of any SMASS call; no SMASS failure rolls either back (BR-10, BR-16).
3. `AssignRegister` and `ReplaceRegister` link all assessments of the visit, populating `RegId`, `PasienId`, `LayananId` together and setting `RegistrationLinkStatus = Registered` idempotently (D-09, BR-02, BR-15, BR-17).
4. Pending assessments are reachable only through the by-visit surface and are excluded from every RegId/PasienId-based surface (D-10, BR-05, BR-19, BR-20).
5. `AggStateEnum` is never written by the new commands; pending assessments may remain `Drafting` (D-08, BR-13, BR-14).
6. Mapping resolution is data-driven from `SMASS_TriageConceptMap`; a missing mapping row or unmapped concept fails closed without creating a partial clinical snapshot (D-03, D-12, AR-08).
7. Both gateway operations authenticate with existing JWT Bearer; the three new SMASS endpoints enforce `[Authorize]` (D-13, AR-05).
8. No historical assessment is modified or backfilled; legacy behaviour is unchanged with the feature disabled (D-11, BR-21, BR-22).
9. No delete, purge, archive or unlink path exists for tasks or assessments; pending-registration monitoring is available (D-15, BR-31, BR-33).
10. The frontend never calls SMASS directly and renders SMASS status only from BILREG surfaces (D-02).

## 11.3 Planning Handoff

Expected implementation areas:

1. **SMASS persistence + master data** — `SMASS_Assesment` alteration, `SMASS_TriageConceptMap`, IGD Triage Paper seed, mapping seed, DAL extensions, quarantine predicate.
2. **SMASS application** — `GenerateIgdTriageAssesmentCommand`, `LinkAssesmentByIgdVisitIdCommand`, `ListAssesmentByIgdVisitIdQuery`, `ListPendingRegistrationAssesmentQuery`, `TriageConceptMapDal`, controller actions, `[Authorize]`.
3. **BILREG persistence** — `BILRG_IgdVisitSmassTask`, DAL, DTO, repo, domain model + enums.
4. **BILREG integration** — `SmassOptions`, `IgdVisitOptions`, `ISmassTokenService`, `ISmassAssessmentGateway`, four handler hooks, retry/process commands, list queries, `IgdVisitSmassTaskController`, response DTO extension.
5. **Web (c012_myhospital_web)** — `EmergencyService` hooks, types, SCR-02 badge, SCR-03 panel, retry mutation.

Major dependencies:

- SMASS Paper + mapping seed must exist before IGD traffic is enabled (BR-23 step 2 → step 4).
- SMASS endpoints must be deployed before the BILREG toggle is switched on.
- BILREG needs SMASS base URL and service credentials in configuration.

Recommended sequencing considerations:

- SMASS-side read/write surfaces and master data must be complete and verifiable **before** the BILREG toggle is enabled, because BILREG records every failure as a task row that then requires manual retry.
- The persistence + master-data areas are independent of the BILREG gateway and can be verified in isolation against legacy behaviour (BR-23 step 3).
- Frontend work is last: it is display-only and cannot change any backend outcome.
- Manual-retry and monitoring surfaces should be delivered together with the first end-to-end slice, so that the first generation failure is recoverable without a database script.
