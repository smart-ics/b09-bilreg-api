# IGD Triage ABC to SMASS Feasibility Assessment

> **Document status:** Canonical analytical artifact  
> **Date:** 2026-09-17 (updated: D-15 recorded)  
> **Target contexts:** IGD Visit (`b09-bilreg-api` / `IgdContext`) and Structured Medical Assessment (`a043_smass_structuredmedicalassesment_api`)  
> **Related artifact:** `b09-bilreg-api/docs/contexts/igd/igd-assessment-input-feasibility-analysis.md` (reverse direction; its D-01 is superseded for this track)  
> **Result:** **FEASIBLE** and ready for planning

## Executive Summary

### Objective

Assess whether each IGD Visit Triage ABC submission can synchronously generate a structured medical assessment in SMASS using a dedicated custom Paper, while preserving the existing visit-first IGD flow.

The generated assessment is populated from the Triage ABC form and follows `Smass.Domain/AssesmentContext/AssesmentAgg/AssesmentModel.cs`. The frontend does not orchestrate the integration.

### Scope

In scope:

- BILREG triage submission and re-triage flows.
- Synchronous BILREG-to-SMASS assessment generation.
- Pre-registration assessment identity and registration linking.
- Triage-to-concept mapping, Paper ownership, versioning, visibility, and failure handling.
- SMASS data model, API, integration, rollout, and display implications.

Out of scope:

- The 30-item SMASS-first form and the reverse-direction assessment analyzed in the related artifact.
- Redesign of unrelated SMASS validation, catalogs, reports, formulas, or existing IGD Papers.

### Assessment Outcome

Planning may proceed. GAP-001 through GAP-012 and OQ-01 through OQ-12 are resolved. D-01 through D-15 are approved, with D-12 superseding the former D-03 production approval gate. Remaining items are planning-level implementation inputs, not feasibility blockers.

The selected solution is:

- BILREG remains visit-first and saves triage before calling SMASS.
- Every triage event generates one new immutable assessment against a dedicated IGD Triage Paper.
- The assessment is correlated by (`IgdVisitId`, `NoTriage`) and begins with `RegistrationLinkStatus = PendingRegistration`.
- `AssignRegister` synchronously links all pending assessments for the visit.
- Triage and registration are never rolled back because of an integration failure.
- Pending assessments are visible only through IGD Visit workflows until linked.

## Current State Analysis

### Existing IGD Flow

1. BILREG creates an `IgdVisit` visit-first with visitor data; `RegId` is not required at creation.
2. Triage is submitted through `POST /api/IgdVisit/{id}/triage` or re-triage through `POST /api/IgdVisit/{id}/re-triage`.
3. The payload contains six integer scores: Airways (0–2), Breathing (0–5), BloodCirculation (0–4), GCS Eye (1–4), GCS Motor (1–6), and GCS Voice (1–5), plus manual-override-black, override reason, notes, and user.
4. `AtsTriageEngine` computes ATS1–5 and Red/Yellow/Green. The visit stores the result and appends triage history.
5. Triage is required for bed assignment. Discharge requires a linked `RegId`.
6. `AssignRegister` currently loads the visit and registration, calls `visit.AssignRegister`, and saves without an outbound SMASS call.

Relevant components include:

- BILREG persistence in `BILRG_IgdVisitTriage`, with primary key (`IgdVisitId`, `NoTriage`).
- `IgdVisitAssessTriageCmd`, `IgdVisitReAssessTriageCmd`, `IgdVisitTriageDal`, and `IgdVisitAssignRegisterCmd`.
- No existing outbound SMASS call in the BILREG triage path.

### Existing SMASS Flow

- `CreateFixAssesmentCommand` currently requires non-empty `PasienId`, `RegId`, `LayananId`, `PaperId`, `UserrId`, and date/time.
- `AssesmentBuilder` resolves Paper and registration/service data and stamps the caller as the user.
- SMASS supports custom sections through `AddCustomSectionAssesmentCommand` and Paper `addCustomSection`.
- `SyncChartVitalSignCommand` is an existing precedent for programmatic assessment generation against a dedicated Paper and custom-section concepts.
- `AggStateEnum` currently contains `Created`, `Drafting`, `Finished`, and `Deleted` only.
- `SMASS_Assesment` currently has no `IgdVisitId`; no triage-to-assessment link table exists.

### Integration Constraints

- The requested direction is BILREG to SMASS.
- Integration remains synchronous; no async/outbox synchronization mechanism is introduced.
- IGD triage is valid before registration, so the SMASS path must support a first-class pre-registration identity rather than a placeholder `RegId` or registration-first flow.
- Existing SMASS registration-keyed reads must remain unchanged for linked assessments.
- Existing JWT Bearer infrastructure is used for both gateway operations.
- BILREG is the legal record and source of truth for triage history and audit.

## Findings

### Identified Gaps

| Gap | Finding | Resolution |
| --- | --- | --- |
| GAP-001 | No BILREG-to-SMASS path exists in the triage flow. | **Resolved by D-02:** synchronous backend-to-backend generation after successful triage; BILREG owns the gateway. |
| GAP-002 | SMASS creation requires `RegId`/`PasienId`/`LayananId`, but triage may precede registration. | **Resolved by D-01:** create with `IgdVisitId` in `PendingRegistration`; keys are optional until automatic linking. Implementation is covered by GAP-009 and GAP-010. |
| GAP-003 | Integer ATS scores do not directly match seeded qualifier and derived SMASS concepts. | **Resolved by D-03 and D-12:** SMASS-owned configurable ATS-to-SMASS mapping table with approved provisional seed-derived values. |
| GAP-004 | BILREG captures GCS Eye/Motor/Voice separately while SMASS evidence includes a GCS-total concept. | **Resolved by D-03 and D-12:** per-component mappings are rows in the same mapping table. |
| GAP-005 | New-versus-append semantics and SMASS correlation for re-triage were undefined. | **Resolved by D-04:** one new immutable snapshot per triage, correlated by (`IgdVisitId`, `NoTriage`); all pending assessments link on `AssignRegister`. |
| GAP-006 | Ownership and versioning of the custom Paper were undefined. | **Resolved by D-05:** dedicated IGD Triage Paper; no triage concepts in existing IGD Papers. |
| GAP-007 | `LayananId` and `UserrId` sources were undefined. | **Resolved by D-06:** configurable IGD `LayananId` at creation, triage operator as `UserrId`, administrative keys nullable until link. |
| GAP-008 | Synchronous generation failure, retry, and idempotency were undefined. | **Resolved by D-07:** triage is never rolled back; generation is idempotent on (`IgdVisitId`, `NoTriage`); failures become Pending Generation items; MVP retry is manual. |
| GAP-009 | Registration status versus `AggStateEnum` modeling was undefined. | **Resolved by D-08:** separate `RegistrationLinkStatus` with `PendingRegistration` and `Registered`; `AggStateEnum` remains unchanged. |
| GAP-010 | Registration-link trigger, contract, multi-assessment behavior, retry, and edge cases were undefined. | **Resolved by D-09:** synchronous `LinkAssessmentByIgdVisitId` after saved `AssignRegister`; link all pending assessments, idempotently; no registration rollback; replace with latest on `ReplaceRegister`; preserve on void. |
| GAP-011 | Existing RegId-based reads cannot show pending assessments. | **Resolved by D-10:** `ListByIgdVisitId` is the pending-only surface; pending assessments are excluded from RegId-based surfaces and participate in them after linking. |
| GAP-012 | Migration, deployment ordering, and legacy coexistence were undefined. | **Resolved by D-11:** no historical migration; existing rows keep `IgdVisitId = NULL`; DB → SMASS app → verify legacy → enable integration → monitor. |

### Open Questions

All twelve questions are resolved. OQ-01 through OQ-08 are the former feasibility blockers; OQ-09 through OQ-12 are the former planning questions.

| Open Question | Resolution | Decision |
| --- | --- | --- |
| OQ-01: Assessment key availability | Pre-registration creation uses `IgdVisitId`; administrative keys are populated by `AssignRegister`. | D-01 |
| OQ-02: Clinical approval of provisional mappings | D-12 makes provisional values the approved source of truth for demo, UAT, and initial deployment; no MVP sign-off or production gate. | D-03, D-12 |
| OQ-03: Triage versioning | Every triage event creates one immutable assessment correlated by (`IgdVisitId`, `NoTriage`). | D-04 |
| OQ-04: Paper ownership | A dedicated IGD Triage Paper is canonical; triage changes are versioned within its lifecycle. | D-05 |
| OQ-05: Generation failure semantics | Triage remains committed; failures are recorded for manual MVP retry and generation is idempotent. | D-07 |
| OQ-06: Registration linking | `LinkAssessmentByIgdVisitId` runs after `AssignRegister` persistence and links all pending assessments. | D-09 |
| OQ-07: Status lifecycle | Registration linking uses a separate field; completion state is unchanged. | D-08 |
| OQ-08: Migration and rollout | No historical migration; legacy and pending paths coexist under the ordered rollout and feature toggle. | D-11 |
| OQ-09: Assessment identity values | Configured IGD `LayananId` is assigned at creation; triage operator identity becomes `UserrId`. | D-06 |
| OQ-10: Service authentication | Existing JWT Bearer is used for generation and linking; no new auth scheme. | D-13 |
| OQ-11: Legal record | BILREG triage is the legal record and audit source; SMASS assessments are derived documents. | D-14 |
| OQ-12: Never-registered assessments | Preserve them as valid long-lived pending records; do not auto-delete, purge, or archive; provide monitoring. | D-15 |

## Approved Decisions

### D-01 Assessment Key Availability Strategy

- **Status:** APPROVED.
- **Decision:** IGD-originated assessments may be created before registration, with `IgdVisitId` as the operational reference. `RegId`, `PasienId`, and `LayananId` are optional while `RegistrationLinkStatus = PendingRegistration`; `AssignRegister` automatically populates the administrative keys.
- **Rationale:** Preserves the visit-first flow and avoids both placeholder-`RegId` and registration-first designs.
- **Consequences:** SMASS requires a pre-registration creation path, `IgdVisitId` persistence, pending-aware validation, and registration linking.

### D-02 Synchronous Assessment Generation

- **Status:** APPROVED.
- **Decision:** BILREG synchronously calls SMASS immediately after successful triage submission; every triage generates an assessment.
- **Rationale:** Keeps orchestration in the backend, hides SMASS complexity from the frontend, produces deterministic behavior, and avoids async synchronization complexity.
- **Consequences:** BILREG owns an outbound SMASS gateway and invokes generation for both triage and re-triage.

### D-03 Temporary ATS-to-SMASS Mapping Strategy

- **Status:** APPROVED (Temporary).
- **Decision:** Use a configurable ATS-to-SMASS mapping table. Initial values derive from SMASS Concept and ConceptPreference seed data and are provisional.
- **Rationale:** Provides the required score-to-concept translation without embedding mappings in the gateway or Paper.
- **Consequences:** The table supplies demo/development mappings. D-12 subsequently makes the provisional values the approved source of truth for demo, UAT, and initial deployment and removes the former production gate.

### D-04 Triage Assessment Versioning Strategy

- **Status:** APPROVED.
- **Decision:** Each triage event creates a new immutable assessment snapshot, correlated by (`IgdVisitId`, `NoTriage`).
- **Rationale:** Preserves clinical history, aligns with BILREG triage history and the every-triage requirement, and simplifies linking.
- **Consequences:** Multiple assessments may exist for one visit; all pending assessments for the visit link on `AssignRegister`. Clinical content is not changed after creation.

### D-05 Dedicated IGD Triage Paper

- **Status:** APPROVED.
- **Decision:** Create a dedicated IGD Triage Paper. Do not embed triage concepts in an existing IGD Paper.
- **Rationale:** Triage is a distinct workflow whose versioning must be independent, with reduced impact on existing assessments.
- **Consequences:** The Paper is canonical for all triage-generated assessments; future triage changes remain within its lifecycle.

### D-06 Identity Mapping Strategy

- **Status:** APPROVED.
- **Decision:** Assign a configurable IGD `LayananId` (for example, `IgdVisit:SmassLayananId`) at creation, independently of registration. Copy the triage operator identity to `UserrId`. Keep `RegId` and `PasienId` nullable until linking.
- **Rationale:** Establishes stable service and user identity at generation while preserving pre-registration creation.
- **Consequences:** Configuration ownership, validation, invalid-value behavior, and audit labeling remain planning inputs. D-09 later requires `LayananId` to be populated from registration data during linking; the configured value remains the pre-link value.

### D-07 Assessment Generation Failure Handling

- **Status:** APPROVED.
- **Decision:** Triage persistence is primary and is never rolled back for generation failure. Generation is idempotent on (`IgdVisitId`, `NoTriage`). Failed attempts become Pending Generation items; MVP retry is manual, with automatic workers deferred unless later required.
- **Rationale:** Preserves the legal triage record and prevents duplicate snapshots without introducing distributed transactions.
- **Consequences:** BILREG needs an operational Pending Generation store and manual-retry surface. The failure record is a worklist, not an asynchronous synchronization mechanism.

### D-08 Registration-Link Lifecycle Strategy

- **Status:** APPROVED.
- **Decision:** Add a separate `RegistrationLinkStatus` field with `PendingRegistration` and `Registered`; do not extend `AggStateEnum`.
- **Rationale:** Registration linkage and assessment completion are different concerns, avoiding ripple effects across existing state consumers.
- **Consequences:** A pending assessment may be `Drafting` or `Finished`; linking never changes completion status. Existing `AggStateEnum` consumers remain unchanged.

### D-09 Registration Linking Strategy

- **Status:** APPROVED.
- **Decision:** After successful `AssignRegister` persistence, synchronously call `LinkAssessmentByIgdVisitId`. Populate `RegId`, `PasienId`, and `LayananId` for all pending assessments and set `RegistrationLinkStatus = Registered`; repeated calls are allowed. Registration is not rolled back on link failure. `ReplaceRegister` uses the latest values; `VoidVisit` preserves records.
- **Rationale:** Links all snapshots for a visit deterministically without coupling registration persistence to a distributed transaction.
- **Consequences:** The link operation requires atomic per-assessment updates, idempotency, retry through the D-07 mechanism, and an auditable link event. D-09 supersedes BR-09's earlier statement that `LayananId` was never linked.

### D-10 Pending Assessment Visibility

- **Status:** APPROVED.
- **Decision:** Pending assessments are visible only through IGD Visit workflows using `ListByIgdVisitId`. They are excluded from `ListByRegId`, `Catalog/{regId}`, OFTA integrations, and RegId-based reporting. After linking, they participate in existing RegId-based SMASS queries.
- **Rationale:** Prevents incomplete administrative records from entering registration-keyed workflows while providing an explicit IGD view.
- **Consequences:** SMASS needs the by-visit query and index; existing RegId-based reads remain unchanged.

### D-11 Migration and Rollout Strategy

- **Status:** APPROVED.
- **Decision:** Do not migrate historical data; existing assessments retain `IgdVisitId = NULL`. Legacy and pending-registration creation coexist. Deploy in this order: database, SMASS application, verify legacy behavior, enable IGD integration, monitor production. Use `IgdVisit:EnableSmassIntegration` as the activation toggle.
- **Rationale:** Minimizes deployment risk, avoids unnecessary data migration, supports incremental rollout, and preserves backward compatibility.
- **Consequences:** Toggle ownership and default state are planning inputs. No clinical data is backfilled.

### D-12 Temporary Clinical Mapping Ownership

- **Status:** APPROVED.
- **Decision:** SMASS master data owns the ATS-to-SMASS mapping table. Seed-derived provisional values are the approved source of truth for demo, UAT, and initial deployment. Later clinical revisions are configuration changes.
- **Rationale:** Allows delivery without a clinical sign-off gate while keeping mapping ownership in the clinical assessment system.
- **Consequences:** No clinical sign-off is required for MVP and no production gate is imposed. Mapping changes require no code changes and the dedicated Paper remains stable. Revision governance is a planning input.

### D-13 BILREG-to-SMASS Service Authentication

- **Status:** APPROVED.
- **Decision:** The BILREG gateway uses existing JWT Bearer authentication for `GenerateAssessment` and `LinkAssessmentByIgdVisitId`. SMASS uses its existing token validation configuration. No API key, shared secret, or custom scheme is introduced.
- **Rationale:** Reuses current infrastructure, avoids a second authentication model, and is consistent with existing MyHospital API practices.
- **Consequences:** Authorization enforcement must be wired onto the generate/link surfaces. Gateway token acquisition details remain within the existing mechanism.

### D-14 Triage Legal Record Ownership

- **Status:** APPROVED.
- **Decision:** The BILREG triage record is the legal record and source of truth for triage history and audit. SMASS assessments are derived clinical documents.
- **Rationale:** Preserves ownership boundaries and avoids legal ambiguity when generation fails.
- **Consequences:** BILREG displays generation status and linked AssessmentId. SMASS displays `IgdVisitId`, `NoTriage`, and `Generated From IGD Triage`; pending assessments are labeled `Pending Registration`. Display correlation and screen placement are planning inputs.

### D-15 Orphan Pending Assessment Policy

- **Status:** APPROVED.
- **Decision:** Never auto-delete, purge, or archive an assessment solely because registration never occurred. `PendingRegistration` is valid long-lived state. Voiding a visit preserves assessments. Provide a Pending Registration monitoring query/dashboard; future retention policies are operational governance changes that do not change assessment semantics.
- **Rationale:** Preserves clinical history, is consistent with D-09 and D-14, avoids accidental data loss, and keeps MVP implementation simple.
- **Consequences:** Monitoring implementation and host/ownership are planning inputs. Pending visibility remains governed by D-10.

## Target Solution

### High-Level Architecture

```text
BILREG IGD Visit
  ├─ triage persistence and history
  ├─ synchronous GenerateAssessment gateway call
  └─ synchronous LinkAssessmentByIgdVisitId after AssignRegister

SMASS
  ├─ dedicated IGD Triage Paper and CustomSection
  ├─ assessment correlation: IgdVisitId + NoTriage
  ├─ RegistrationLinkStatus: PendingRegistration | Registered
  ├─ SMASS-owned ATS-to-SMASS mapping table
  └─ pending visibility through ListByIgdVisitId
```

### Integration Flow

```text
Triage submit
  → save BILRG_IgdVisitTriage and visit state
  → GenerateAssessment(IgdVisitId, NoTriage, assessor, timestamps, concepts, PaperId)
  → create one immutable SMASS snapshot with PendingRegistration

AssignRegister
  → persist registration
  → LinkAssessmentByIgdVisitId(IgdVisitId, RegId, PasienId, LayananId)
  → populate all matching pending assessments and set Registered
```

On generation failure, triage remains saved and a Pending Generation item is recorded. On link failure, registration remains saved and the link is retried through the D-07 mechanism. `ReplaceRegister` links with the latest data; voiding preserves all assessments.

### Assessment Lifecycle

- Triage submission creates exactly one assessment for the (`IgdVisitId`, `NoTriage`) event.
- The assessment is an immutable snapshot of clinical sections and concepts.
- It starts with `RegistrationLinkStatus = PendingRegistration` and may have `AggStateEnum = Drafting` or `Finished`.
- `AssignRegister` changes only registration-link state and administrative keys.
- A retry for the same business key resolves to the existing snapshot.

### Registration Linking Lifecycle

- `AssignRegister` is persisted before linking is attempted.
- `LinkAssessmentByIgdVisitId` links all pending assessments for the visit.
- Administrative keys are populated together; partial key population is invalid.
- Link calls are idempotent. `ReplaceRegister` replaces prior linked values with the latest registration data.
- Link failure does not roll back registration. `VoidVisit` does not delete or unlink records.

### Triage Versioning Strategy

Every first triage and re-triage event creates a new snapshot. `IgdVisitId` and `NoTriage` jointly mirror the BILREG triage primary key and enforce one assessment per event.

### Identity Mapping Strategy

- Creation-time `LayananId`: configured IGD value.
- Creation-time `UserrId`: user who performed triage.
- Pre-link `RegId` and `PasienId`: nullable.
- Link-time values: `RegId`, `PasienId`, and `LayananId` from the registration-link payload, as required by D-09.

### Failure Handling Strategy

- Triage and registration are primary operations and are not rolled back for downstream failures.
- Generation idempotency uses (`IgdVisitId`, `NoTriage`).
- Failed generation is recorded as Pending Generation for manual MVP retry.
- Failed links reuse the D-07 retry mechanism.
- No distributed transaction or async/outbox mechanism is introduced.

### Visibility Strategy

- Pending assessments are available through `ListByIgdVisitId` and IGD Visit workflows.
- They are excluded from `ListByRegId`, `Catalog/{regId}`, OFTA integrations, and RegId-based reports.
- Registered assessments participate in existing RegId-based SMASS queries.
- SMASS displays source information and the Pending Registration label; BILREG displays generation status and linked AssessmentId.
- A Pending Registration monitoring query/dashboard provides operational visibility for long-lived and voided-visit pending records.

## Data Model Changes

### Assessment

Add to `AssesmentModel` and corresponding contracts, mappers, DAL operations, and queries:

- `IgdVisitId`, nullable for non-IGD assessments and mandatory for IGD pending assessments.
- `NoTriage`, mandatory with `IgdVisitId` for IGD snapshots.
- `RegistrationLinkStatus`, with `PendingRegistration` and `Registered`.

Enforce uniqueness on (`IgdVisitId`, `NoTriage`) for IGD-originated assessments to support idempotent generation. Existing non-IGD assessments retain empty/null correlation fields and legacy behavior.

### IGD Visit

BILREG remains the clinical source of triage data and requires no clinical-data correlation column or link table. It requires gateway integration from triage and `AssignRegister`, plus an operational Pending Generation store and a display read path for generation status and AssessmentId.

### Mapping Tables

SMASS master data owns the configurable ATS-to-SMASS table, including Airways, Breathing, Circulation, GCS Eye/Motor/Voice, and related derived score/category mappings. Initial values are seed-derived and approved for demo, UAT, and initial deployment. Later revisions are configuration changes.

### New Fields

| Field | Location | Meaning |
| --- | --- | --- |
| `IgdVisitId` | `SMASS_Assesment` | Operational IGD correlation key. Nullable for legacy/non-IGD rows. |
| `NoTriage` | `SMASS_Assesment` | Triage snapshot key paired with `IgdVisitId`. |
| `RegistrationLinkStatus` | `SMASS_Assesment` | `PendingRegistration` or `Registered`; separate from completion state. |

### New Statuses

`PendingRegistration` and `Registered` belong only to `RegistrationLinkStatus`. `AggStateEnum` remains `Created`, `Drafting`, `Finished`, and `Deleted`. A pending assessment may be `Drafting` or `Finished`, and registration linking never changes completion state.

### Approved Business Rules

- **BR-01:** Empty `RegId`/`PasienId`/`LayananId` is valid only for an IGD assessment with non-empty `IgdVisitId` and `RegistrationLinkStatus = PendingRegistration`.
- **BR-02:** Linking populates all three administrative keys together or not at all.
- **BR-03:** `AssignRegister` drives linking; no other lifecycle flip is assumed.
- **BR-04:** (`IgdVisitId`, `NoTriage`) is the system of record for correlation; no parallel link table is maintained.
- **BR-05:** Pending assessments are quarantined from RegId-based catalogs, reports, and document workflows except the explicitly approved by-visit surface.
- **BR-06:** Clinical sections and concepts are immutable after creation; key population and registration-link status are the sanctioned post-creation mutations.
- **BR-07:** One triage event produces exactly one assessment; retries resolve to that snapshot.
- **BR-08:** Creation uses configured IGD `LayananId` and the triage operator's `UserrId`.
- **BR-09:** **SUPERSEDED by D-09:** the earlier rule that linking never touches `LayananId` is retained only for audit; D-09 requires link-time population of all three keys.
- **BR-10:** Triage persistence is primary and generation failure never rolls it back.
- **BR-11:** Generation is idempotent on (`IgdVisitId`, `NoTriage`).
- **BR-12:** Failed generation creates a Pending Generation item; MVP retry is manual.
- **BR-13:** Registration-link lifecycle is separate from `AggStateEnum`.
- **BR-14:** Pending assessments may be `Drafting` or `Finished`; linking does not change completion.
- **BR-15:** Linking populates all keys and sets `Registered` on all pending assessments for the visit, idempotently.
- **BR-16:** Registration is never rolled back for link failure; failed links retry through D-07.
- **BR-17:** `ReplaceRegister` replaces linked values with the latest registration data.
- **BR-18:** Voiding neither deletes nor unlinks assessments; records remain valid long-lived pending state where applicable.
- **BR-19:** Pending visibility is limited to `ListByIgdVisitId` and IGD Visit workflows.
- **BR-20:** Registered assessments participate in all existing RegId-based queries.
- **BR-21:** Existing assessments retain `IgdVisitId = NULL`; no historical clinical data is backfilled.
- **BR-22:** Legacy and pending-registration creation coexist during rollout.
- **BR-23:** Rollout is DB → SMASS app → verify legacy → enable integration → monitor, controlled by `IgdVisit:EnableSmassIntegration`.
- **BR-24:** SMASS master data owns the ATS-to-SMASS mapping table.
- **BR-25:** Provisional seed-derived mappings are approved truth for demo, UAT, and initial deployment; no MVP sign-off or production gate applies.
- **BR-26:** Later mapping revisions are configuration changes and do not change the dedicated Paper.
- **BR-27:** The gateway uses existing JWT Bearer authentication for both operations.
- **BR-28:** BILREG triage is the legal record and audit source, including when generation fails.
- **BR-29:** SMASS assessments are derived clinical documents, not the triage record.
- **BR-30:** BILREG shows generation status and AssessmentId; SMASS shows source information and labels pending assessments.
- **BR-31:** Never-registered assessments are never automatically deleted, purged, or archived solely for lack of registration.
- **BR-32:** `PendingRegistration` is valid long-lived state, including for voided visits.
- **BR-33:** Pending Registration monitoring is required; future retention comes through operational governance without changing assessment semantics.

## API and Integration Changes

### Generate Assessment

BILREG adds a synchronous gateway operation after successful triage and re-triage. The payload includes `IgdVisitId`, `NoTriage`, triage concepts, timestamps, triage operator identity, configured IGD `LayananId`, and the dedicated Paper identifier. `RegId` and `PasienId` may be absent on this path.

SMASS adds a pending-aware creation path, maps values through the SMASS-owned table, creates one snapshot, and returns the AssessmentId. The operation is idempotent on (`IgdVisitId`, `NoTriage`).

### Registration Linking

After successful `AssignRegister` persistence, BILREG calls `LinkAssessmentByIgdVisitId` with `IgdVisitId`, `RegId`, `PasienId`, and `LayananId`. SMASS updates all matching pending assessments atomically per assessment, sets `Registered`, and leaves completion status unchanged. The operation is idempotent and supports latest-value replacement.

### Query Requirements

Implement `ListByIgdVisitId`, backed by an index on `IgdVisitId`, for pending visibility, idempotency checks, linking, and IGD display. Existing RegId-based queries remain unchanged and exclude pending assessments by construction.

Both gateway operations use JWT Bearer authentication under existing token validation configuration. SMASS must enforce authorization on the generate/link surfaces.

## Deployment and Rollout

### Migration Strategy

- Add the SMASS columns, indexes, DAL, mapper, validator, and API support.
- Do not backfill historical rows; existing assessments retain `IgdVisitId = NULL`.
- Add the dedicated Paper, CustomSection, concepts, preferences, and mapping master data.
- Add BILREG's operational Pending Generation store and retry surface.

### Rollout Strategy

1. Deploy database changes.
2. Deploy SMASS application changes.
3. Verify legacy assessment behavior.
4. Enable IGD integration through `IgdVisit:EnableSmassIntegration`.
5. Monitor generation, linking, pending generation, and Pending Registration surfaces.

### Backward Compatibility

- Legacy fully keyed SMASS assessment creation continues to coexist with the IGD pending path.
- Existing `AggStateEnum` consumers, linked assessment catalogs, reports, OFTA integrations, formulas, and unrelated Papers remain unchanged.
- No historical clinical data is migrated.
- Pending assessments join existing RegId-based behavior only after successful linking.

## Risks and Assumptions

### Risks

| Risk | Status / Impact | Mitigation or remaining input |
| --- | --- | --- |
| Never-registered or voided visits leave pending assessments. | Accepted by D-15 as valid long-lived state. | Implement Pending Registration monitoring; future retention is governance-owned. |
| Pending assessments are absent from RegId-based surfaces. | Accepted by D-10 and intentional. | Implement and index `ListByIgdVisitId`. |
| Link fails after registration is saved. | Accepted by D-09; registration is not rolled back. | Define link contract and retry-surface wiring through D-07. |
| Generation fails after triage is saved. | Accepted by D-07; triage is not rolled back. | Implement Pending Generation storage and manual MVP retry. |
| Mapping values require later clinical revision. | Accepted by D-12; provisional values are approved initial truth. | Define post-deployment mapping revision governance. |
| Invalid or missing configured `LayananId`. | Residual identity/configuration risk. | Define configuration ownership, validation, and failure behavior. |
| Authorization is not currently enforced on the relevant SMASS controller. | Residual security wiring risk. | Enforce existing JWT authorization on generate/link surfaces. |
| Gateway token acquisition and audit labeling are not fully specified. | Planning-level security/audit input. | Define service identity/token details under existing JWT infrastructure and audit generation/link events. |
| AssessmentId display correlation and screen placement are unspecified. | Planning-level UI input. | Persist the returned id or look it up by business key; select host screens. |

The following former risks are retired by decision: missing `RegId` at triage (D-01), status-model ripple (D-08), duplicate re-triage snapshots and retry duplicates (D-04/D-07), and a production gate for provisional mappings (D-12).

### Assumptions

- The existing SMASS custom-section and programmatic-generation mechanisms remain available.
- Existing JWT Bearer infrastructure is used without a new authentication scheme.
- The dedicated IGD Triage Paper is created and seeded before IGD integration traffic is enabled.
- SMASS owns mapping master data and its provisional seed-derived values for the initial deployment.
- BILREG owns the gateway; the Pending Generation store mechanism and ownership remain planning inputs.
- D-01 through D-15 and BR-01 through BR-33 are the approved decision and business-rule boundary; implementation must not add new semantics.

## Final Recommendation

Proceed with implementation planning for the D-01 through D-15 solution: a dedicated SMASS IGD Triage Paper, synchronous BILREG-to-SMASS generation of one immutable assessment per triage event, (`IgdVisitId`, `NoTriage`) correlation and idempotency, separate registration-link status, synchronous link-all after `AssignRegister`, no rollback on downstream failure, visit-only pending visibility, existing JWT Bearer authentication, no historical migration, and the ordered rollout behind `IgdVisit:EnableSmassIntegration`.

The solution is feasible. Remaining work is implementation planning for Paper seeding, mapping revision governance, configuration validation, Pending Generation storage and retry, link contract mechanics, by-visit query/indexing, authorization wiring, token acquisition, display correlation, and monitoring ownership. None is a discovery blocker or changes an approved decision.

## Appendix A - GAP Resolution Matrix

| GAP | Resolution | Decision |
| --- | --- | --- |
| GAP-001 | Synchronous BILREG-to-SMASS generation after successful triage. | D-02 |
| GAP-002 | Pre-registration assessment keyed by `IgdVisitId`; link on `AssignRegister`. | D-01 |
| GAP-003 | Configurable SMASS-owned ATS-to-SMASS mapping table with approved provisional values. | D-03, D-12 |
| GAP-004 | GCS component mappings are rows in the same table. | D-03, D-12 |
| GAP-005 | New immutable snapshot per triage, keyed by (`IgdVisitId`, `NoTriage`). | D-04 |
| GAP-006 | Dedicated IGD Triage Paper. | D-05 |
| GAP-007 | Configured creation-time IGD `LayananId`; operator `UserrId`; nullable administrative keys until link. | D-06 |
| GAP-008 | No rollback, idempotent generation, Pending Generation record, manual MVP retry. | D-07 |
| GAP-009 | Separate `RegistrationLinkStatus`; `AggStateEnum` unchanged. | D-08 |
| GAP-010 | Synchronous link-all operation after saved registration; idempotent, retryable, replace-latest, preserve-on-void. | D-09 |
| GAP-011 | `ListByIgdVisitId` is the pending-only visibility surface; full RegId participation after link. | D-10 |
| GAP-012 | No historical migration; legacy coexistence and ordered toggle-controlled rollout. | D-11 |

## Appendix B - Open Question Resolution Matrix

| Open Question | Resolution | Decision |
| ------------- | ---------- | -------- |
| OQ-01 | Pre-registration creation uses `IgdVisitId`; link populates administrative keys. | D-01 |
| OQ-02 | Provisional mappings are approved truth for demo, UAT, and initial deployment; no gate. | D-03, D-12 |
| OQ-03 | One immutable assessment per triage event. | D-04 |
| OQ-04 | Dedicated IGD Triage Paper is canonical. | D-05 |
| OQ-05 | No rollback; Pending Generation and manual MVP retry. | D-07 |
| OQ-06 | `LinkAssessmentByIgdVisitId` after registration persistence; link all pending records. | D-09 |
| OQ-07 | Separate registration-link status; completion status unchanged. | D-08 |
| OQ-08 | No migration; ordered rollout behind `IgdVisit:EnableSmassIntegration`. | D-11 |
| OQ-09 | Configured IGD `LayananId` and triage-operator `UserrId`. | D-06 |
| OQ-10 | Existing JWT Bearer for generation and linking. | D-13 |
| OQ-11 | BILREG triage is the legal record; SMASS is derived. | D-14 |
| OQ-12 | Preserve never-registered assessments and monitor them; governance owns future retention. | D-15 |

## Appendix C - Historical and Superseded Material

The following items are retained for traceability only:

- The related reverse-direction assessment's D-01 framing of guard relaxation versus registration-first is superseded for this track by D-01's first-class pre-registration identity.
- The earlier assumption that no SMASS schema change was required is superseded; `IgdVisitId`, `NoTriage`, and `RegistrationLinkStatus` are required.
- The earlier provisional assumption of historical backfill is superseded by D-11; existing rows retain `IgdVisitId = NULL`.
- The former D-03 production clinical-approval gate is superseded by D-12; provisional seed-derived mappings are approved truth for demo, UAT, and initial deployment.
- BR-09 is retained in the business-rule list as superseded by D-09 because D-09 requires link-time population of `LayananId`.
- The rejected frontend dual-dispatch alternative is not selected because D-02 requires backend orchestration and keeps SMASS complexity out of the frontend.
