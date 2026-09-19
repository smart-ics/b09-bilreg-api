---
Title: IGD Triage ABC to SMASS Feasibility Assessment
Code: IGD-TRIAGE-ABC-SMASS
Artifact: FEASIBILITY-ASSESSMENT
Version: 2.0
LastUpdated: 2026-09-19
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Feature being assessed:

Each IGD Visit Triage ABC submission (first triage and re-triage) synchronously generates one
structured medical assessment in SMASS, built against a dedicated custom Paper, while preserving
the existing visit-first IGD flow. The frontend does not orchestrate the integration.

Referenced artifacts:

- DOMAIN: `b09-bilreg-api/docs/contexts/igd/igd-02-domain.md`
- FEATURE: `b09-bilreg-api/docs/contexts/igd/igd-01-context.md` (IGD Visit)
- ARCHITECTURE (realization consumer): `b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-architecture.md`

## Objective

Determine whether the requested capability is feasible against the current IGD Visit
(`b09-bilreg-api` / `IgdContext`) and Structured Medical Assessment
(`a043_smass_structuredmedicalassesment_api` / `AssesmentContext`) implementation, identify gaps,
open questions, assumptions and risks, and establish whether the target architecture can be
finalized.

In scope:

- BILREG triage submission and re-triage flows.
- Synchronous BILREG-to-SMASS assessment generation.
- Pre-registration assessment identity and registration linking.
- Triage-to-concept mapping, Paper ownership, versioning, visibility, and failure handling.
- SMASS data model, API, integration, rollout, and display implications.

Out of scope:

- The 30-item SMASS-first form and the reverse-direction assessment mentioned in the superseded
  related analysis.
- Redesign of unrelated SMASS validation, catalogs, reports, formulas, or existing IGD Papers.

---

# 2. Current State

This section contains facts only and does not propose solutions.

## Existing Behavior

### Existing IGD Flow

1. BILREG creates an `IgdVisit` visit-first with visitor data; `RegId` is not required at creation.
2. Triage is submitted through `POST /api/IgdVisit/{id}/triage` or re-triage through
   `POST /api/IgdVisit/{id}/re-triage`.
3. The payload contains six integer scores: Airways (0–2), Breathing (0–5), BloodCirculation (0–4),
   GCS Eye (1–4), GCS Motor (1–6), and GCS Voice (1–5), plus manual-override-black, override reason,
   notes, and user.
4. `AtsTriageEngine` computes ATS1–5 and Red/Yellow/Green. The visit stores the result and appends
   triage history.
5. Triage is required for bed assignment. Discharge requires a linked `RegId`.
6. `AssignRegister` currently loads the visit and registration, calls `visit.AssignRegister`, and
   saves without an outbound SMASS call.

Relevant components:

- BILREG persistence in `BILRG_IgdVisitTriage`, with primary key (`IgdVisitId`, `NoTriage`).
- `IgdVisitAssessTriageCmd`, `IgdVisitReAssessTriageCmd`, `IgdVisitTriageDal`, and
  `IgdVisitAssignRegisterCmd`.
- No existing outbound SMASS call exists in the BILREG triage path.

### Existing SMASS Flow

- `CreateFixAssesmentCommand` requires non-empty `PasienId`, `RegId`, `LayananId`, `PaperId`,
  `UserrId`, and date/time.
- `AssesmentBuilder` resolves Paper and registration/service data and stamps the caller as the user.
- SMASS supports custom sections through `AddCustomSectionAssesmentCommand` and Paper
  `addCustomSection`.
- `SyncChartVitalSignCommand` is an existing precedent for programmatic assessment generation
  against a dedicated Paper and custom-section concepts.
- `AggStateEnum` contains `Created`, `Drafting`, `Finished`, and `Deleted` only.
- `SMASS_Assesment` has no `IgdVisitId`; no triage-to-assessment link table exists.

## Existing Constraints

- The requested direction is BILREG to SMASS.
- Integration is synchronous; no async/outbox synchronization mechanism exists for this path.
- IGD triage is valid before registration, so the SMASS path must support a first-class
  pre-registration identity rather than a placeholder `RegId` or a registration-first flow.
- Existing SMASS registration-keyed reads must remain unchanged for linked assessments.
- Existing JWT Bearer infrastructure is used by both systems.
- BILREG is the legal record and source of truth for triage history and audit.
- The related reverse-direction artifact referenced by the prior revision
  (`igd-assessment-input-feasibility-analysis.md`) does not exist in the active docs tree; only a
  superseded copy remains under `docs/contexts/igd/_unused/`.

---

# 3. Gap Analysis

Gaps identified between the requested capability and the current system. All listed gaps are
CLOSED; closure decisions are recorded in section 8.

| ID | Severity | Gap |
| --- | --- | --- |
| GAP-001 | CRITICAL | No BILREG-to-SMASS path exists in the triage flow. |
| GAP-002 | CRITICAL | SMASS creation requires `RegId`/`PasienId`/`LayananId`, but triage may precede registration. |
| GAP-003 | MAJOR | Integer ATS scores do not directly match seeded qualifier and derived SMASS concepts. |
| GAP-004 | MAJOR | BILREG captures GCS Eye/Motor/Voice separately while SMASS evidence includes a GCS-total concept. |
| GAP-005 | MAJOR | New-versus-append semantics and SMASS correlation for re-triage were undefined. |
| GAP-006 | MAJOR | Ownership and versioning of the custom Paper were undefined. |
| GAP-007 | MAJOR | `LayananId` and `UserrId` sources were undefined. |
| GAP-008 | CRITICAL | Synchronous generation failure, retry, and idempotency were undefined. |
| GAP-009 | MAJOR | Registration status versus `AggStateEnum` modeling was undefined. |
| GAP-010 | CRITICAL | Registration-link trigger, contract, multi-assessment behavior, retry, and edge cases were undefined. |
| GAP-011 | MAJOR | Existing RegId-based reads cannot show pending assessments. |
| GAP-012 | MAJOR | Migration, deployment ordering, and legacy coexistence were undefined. |

Severity:

- CRITICAL
- MAJOR
- MINOR

---

# 4. Open Questions

Unresolved questions that prevented confident architecture decisions. All listed questions are
resolved; resolutions are recorded in section 8. OQ-01 through OQ-08 were the former feasibility
blockers; OQ-09 through OQ-12 were the former planning questions.

| ID | Question | Impact |
| --- | --- | --- |
| OQ-01 | Which assessment key is available before registration? | Creation identity and persistence shape. |
| OQ-02 | Is clinical approval required for provisional triage-to-concept mappings? | Mapping ownership and delivery gate. |
| OQ-03 | How is triage versioning modeled on re-triage? | Snapshot semantics and correlation. |
| OQ-04 | Which Paper owns triage concepts, and how is it versioned? | Paper ownership and change isolation. |
| OQ-05 | What are the generation-failure and retry semantics? | Triage integrity, idempotency, operations. |
| OQ-06 | How and when are assessments linked to registration? | Registration-link trigger and contract. |
| OQ-07 | How does registration status relate to assessment completion state? | State model and ripple effects. |
| OQ-08 | What are the migration and rollout constraints? | Data safety and legacy coexistence. |
| OQ-09 | Where do `LayananId` and operator identity come from? | Identity mapping at creation and link. |
| OQ-10 | How is the service-to-service call authenticated? | Security scheme and wiring. |
| OQ-11 | Which record is the legal record of triage? | Ownership and audit boundary. |
| OQ-12 | What happens to never-registered assessments? | Retention, monitoring, and data loss risk. |

---

# 5. Assumptions

| ID | Assumption |
| --- | --- |
| ASM-001 | The existing SMASS custom-section and programmatic-generation mechanisms remain available. |
| ASM-002 | Existing JWT Bearer infrastructure is used for both operations without a new authentication scheme. |
| ASM-003 | The dedicated IGD Triage Paper is created and seeded before IGD integration traffic is enabled. |
| ASM-004 | SMASS owns the mapping master data and its provisional seed-derived values for the initial deployment. |
| ASM-005 | BILREG owns the gateway; the failed-generation store mechanism and its ownership are implementation inputs. |
| ASM-006 | The approved closure decisions D-01 through D-15 are the decision boundary; implementation must not add new business semantics. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
| --- | --- | --- | --- |
| RISK-001 | Never-registered or voided visits leave pending assessments. | Long-lived orphan pending records. | Accepted as valid long-lived state (D-15); implement Pending Registration monitoring; future retention is governance-owned. |
| RISK-002 | Pending assessments are absent from RegId-based surfaces. | Incomplete operational visibility if not surfaced deliberately. | Intentional (D-10); implement and index the by-visit query surface. |
| RISK-003 | Link fails after registration is saved. | Assessments remain pending while registration exists. | Accepted (D-09); registration is not rolled back; retry through the D-07 mechanism. |
| RISK-004 | Generation fails after triage is saved. | Missing derived assessment for a triage event. | Accepted (D-07); triage is not rolled back; failed-generation store and manual retry. |
| RISK-005 | Mapping values require later clinical revision. | Clinical content drift over time. | Accepted (D-12); provisional values are approved initial truth; define revision governance. |
| RISK-006 | Invalid or missing configured `LayananId`. | Failed generation due to configuration. | Define configuration ownership, validation, and failure behavior. |
| RISK-007 | Authorization is not currently enforced on the relevant SMASS controller. | Unauthenticated access to new surfaces. | Enforce existing JWT authorization on the generate/link surfaces. |
| RISK-008 | Service token acquisition and audit labeling are not fully specified. | Security and audit traceability gap. | Define service identity/token details under existing JWT infrastructure and audit generation/link events. |
| RISK-009 | AssessmentId display correlation and screen placement are unspecified. | Status not visible to users. | Persist the returned id or look it up by business key; select host screens. |

Former risks retired by decision: missing `RegId` at triage (D-01), status-model ripple (D-08),
duplicate re-triage snapshots and retry duplicates (D-04/D-07), and a production gate for
provisional mappings (D-12).

---

# 7. Recommendations

Possible approaches for addressing the major gaps. No final decision is recorded here; approved
decisions are recorded in section 8.

## Option A — Backend synchronous orchestration (selected through D-02)

BILREG persists triage, then synchronously calls SMASS to generate one assessment per triage event;
BILREG owns the gateway and the operational failure record.

### Advantages

- Keeps orchestration in the backend and hides SMASS complexity from the frontend.
- Deterministic, single-attempt behavior consistent with existing triage persistence.
- Avoids distributed transactions, outbox, and async workers for the MVP.
- Preserves the visit-first IGD flow because generation occurs after triage is committed.

### Disadvantages

- Adds one synchronous outbound HTTP round trip per triage and per registration link.
- Requires an operational failed-generation store and manual retry surface.
- Couples triage latency (bounded) to SMASS availability at request time.

## Option B — Frontend dual-dispatch

The frontend calls BILREG triage and SMASS generation separately.

### Advantages

- No BILREG outbound gateway or failure store required.
- SMASS failure does not affect the triage request timing.

### Disadvantages

- Exposes SMASS complexity and integration semantics to the frontend.
- Non-deterministic orchestration; a client failure can silently drop assessment generation.
- Contradicts D-02 (backend orchestration) and the legal-record boundary (D-14).

---

# 8. Gap Closure

All gaps and open questions below are CLOSED. Decisions D-01 through D-15 are the approved
gap-closure decisions (WHAT is approved). Target-state realization is owned by the ARCHITECTURE
artifact and is not defined here.

## GAP-001

- **Status:** CLOSED
- **Decision:** D-02 — BILREG synchronously calls SMASS immediately after successful triage
  submission; every triage generates an assessment. BILREG owns the gateway.
- **Rationale:** Keeps orchestration in the backend, hides SMASS complexity from the frontend,
  produces deterministic behavior, and avoids async synchronization complexity.
- **Impact:** BILREG owns an outbound SMASS gateway and invokes generation for both triage and
  re-triage.
- **Architecture Impact:** BILREG application hooks and integration gateway; SMASS creation
  endpoint. Affected areas realized under `igd-triage-abc-smass-architecture.md`.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-002

- **Status:** CLOSED
- **Decision:** D-01 — IGD-originated assessments may be created before registration, with
  `IgdVisitId` as the operational reference. `RegId`, `PasienId`, and `LayananId` are optional while
  `RegistrationLinkStatus = PendingRegistration`; `AssignRegister` populates the administrative keys.
- **Rationale:** Preserves the visit-first flow and avoids both placeholder-`RegId` and
  registration-first designs.
- **Impact:** SMASS requires a pre-registration creation path, `IgdVisitId` persistence,
  pending-aware validation, and registration linking.
- **Architecture Impact:** SMASS assessment creation path, persistence, and validation.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-003

- **Status:** CLOSED
- **Decision:** D-03 and D-12 — use a configurable ATS-to-SMASS mapping table owned by SMASS; initial
  seed-derived provisional values are the approved source of truth for demo, UAT, and initial
  deployment.
- **Rationale:** Provides score-to-concept translation without embedding mappings in the gateway or
  Paper, and without a clinical sign-off gate.
- **Impact:** The mapping table supplies initial mappings; mapping changes are configuration
  changes; the dedicated Paper remains stable.
- **Architecture Impact:** SMASS mapping master data and generation-time resolution.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-004

- **Status:** CLOSED
- **Decision:** D-03 and D-12 — GCS Eye/Motor/Voice per-component mappings are rows in the same
  mapping table; the GCS total is derived for mapping purposes.
- **Rationale:** Keeps all triage-to-concept translation in one owned master-data table.
- **Impact:** Per-component and derived mappings are configuration, not code or Paper content.
- **Architecture Impact:** SMASS mapping master data and generation-time resolution.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-005

- **Status:** CLOSED
- **Decision:** D-04 — each triage event creates a new immutable assessment snapshot, correlated by
  (`IgdVisitId`, `NoTriage`); all pending assessments for a visit link on `AssignRegister`.
- **Rationale:** Preserves clinical history, aligns with BILREG triage history and the
  every-triage requirement, and simplifies linking.
- **Impact:** Multiple assessments may exist for one visit; clinical content is not changed after
  creation.
- **Architecture Impact:** SMASS correlation key, idempotency, and linking selection.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-006

- **Status:** CLOSED
- **Decision:** D-05 — create a dedicated IGD Triage Paper; do not embed triage concepts in an
  existing IGD Paper.
- **Rationale:** Triage is a distinct workflow whose versioning must be independent, with reduced
  impact on existing assessments.
- **Impact:** The Paper is canonical for all triage-generated assessments; future triage changes
  remain within its lifecycle.
- **Architecture Impact:** SMASS master data (Paper, sections, concepts) and Paper configuration.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-007

- **Status:** CLOSED
- **Decision:** D-06 — assign a configurable IGD `LayananId` at creation independently of
  registration; copy the triage operator identity to `UserrId`; keep `RegId` and `PasienId` nullable
  until linking. D-09 later requires `LayananId` to be populated from registration data during
  linking; the configured value remains the pre-link value.
- **Rationale:** Establishes stable service and user identity at generation while preserving
  pre-registration creation.
- **Impact:** Configuration ownership, validation, invalid-value behavior, and audit labeling remain
  implementation inputs.
- **Architecture Impact:** SMASS identity mapping and BILREG configuration.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-008

- **Status:** CLOSED
- **Decision:** D-07 — triage persistence is primary and is never rolled back for generation
  failure; generation is idempotent on (`IgdVisitId`, `NoTriage`); failed attempts become Pending
  Generation items; MVP retry is manual.
- **Rationale:** Preserves the legal triage record and prevents duplicate snapshots without
  introducing distributed transactions.
- **Impact:** BILREG needs an operational failed-generation store and manual-retry surface. The
  failure record is a worklist, not an asynchronous synchronization mechanism.
- **Architecture Impact:** BILREG operational task store, retry surface, idempotency enforcement.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-009

- **Status:** CLOSED
- **Decision:** D-08 — add a separate `RegistrationLinkStatus` field with `PendingRegistration` and
  `Registered`; do not extend `AggStateEnum`.
- **Rationale:** Registration linkage and assessment completion are different concerns, avoiding
  ripple effects across existing state consumers.
- **Impact:** A pending assessment may be `Drafting` or `Finished`; linking never changes completion
  status. Existing `AggStateEnum` consumers remain unchanged.
- **Architecture Impact:** SMASS state model and persistence; `AggStateEnum` unchanged.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-010

- **Status:** CLOSED
- **Decision:** D-09 — after successful `AssignRegister` persistence, synchronously call
  `LinkAssessmentByIgdVisitId`; populate `RegId`, `PasienId`, and `LayananId` for all pending
  assessments and set `RegistrationLinkStatus = Registered`; repeated calls are allowed;
  registration is not rolled back on link failure; `ReplaceRegister` uses the latest values;
  `VoidVisit` preserves records. D-09 supersedes the earlier BR-09 statement that `LayananId` was
  never linked.
- **Rationale:** Links all snapshots for a visit deterministically without coupling registration
  persistence to a distributed transaction.
- **Impact:** The link operation requires atomic per-assessment updates, idempotency, retry through
  the D-07 mechanism, and an auditable link event.
- **Architecture Impact:** BILREG registration handlers and link gateway; SMASS link command.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-011

- **Status:** CLOSED
- **Decision:** D-10 — pending assessments are visible only through IGD Visit workflows using
  `ListByIgdVisitId`; they are excluded from `ListByRegId`, `Catalog/{regId}`, OFTA integrations,
  and RegId-based reporting; after linking they participate in existing RegId-based SMASS queries.
- **Rationale:** Prevents incomplete administrative records from entering registration-keyed
  workflows while providing an explicit IGD view.
- **Impact:** SMASS needs the by-visit query and index; existing RegId-based reads remain unchanged.
- **Architecture Impact:** SMASS read surfaces and DAL quarantine predicate.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## GAP-012

- **Status:** CLOSED
- **Decision:** D-11 — do not migrate historical data; existing assessments retain
  `IgdVisitId = NULL`; legacy and pending-registration creation coexist; deploy in order database →
  SMASS application → verify legacy → enable IGD integration → monitor, using the activation
  toggle.
- **Rationale:** Minimizes deployment risk, avoids unnecessary data migration, supports incremental
  rollout, and preserves backward compatibility.
- **Impact:** Toggle ownership and default state are implementation inputs; no clinical data is
  backfilled.
- **Architecture Impact:** SMASS schema additions, BILREG task store, and rollout configuration.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-01

- **Status:** CLOSED
- **Decision:** D-01 — pre-registration creation uses `IgdVisitId`; administrative keys are
  populated by `AssignRegister`.
- **Rationale:** The visit-first flow makes `IgdVisitId` the only reliable pre-registration
  identity.
- **Impact:** Creation and linking are separated; keys are nullable until link.
- **Architecture Impact:** SMASS creation path and link operation.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-02

- **Status:** CLOSED
- **Decision:** D-03, D-12 — provisional seed-derived mappings are the approved source of truth for
  demo, UAT, and initial deployment; no MVP sign-off or production gate.
- **Rationale:** Allows delivery without a clinical sign-off gate while keeping mapping ownership in
  SMASS master data.
- **Impact:** Mapping revisions are configuration changes; the Paper remains stable.
- **Architecture Impact:** SMASS mapping master data and revision governance.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-03

- **Status:** CLOSED
- **Decision:** D-04 — every triage event creates one immutable assessment correlated by
  (`IgdVisitId`, `NoTriage`).
- **Rationale:** Mirrors the BILREG triage primary key and preserves history.
- **Impact:** Multiple assessments per visit; clinical content immutable after creation.
- **Architecture Impact:** SMASS correlation key and idempotency.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-04

- **Status:** CLOSED
- **Decision:** D-05 — a dedicated IGD Triage Paper is canonical; triage changes are versioned
  within its lifecycle.
- **Rationale:** Independent versioning with reduced impact on existing assessments.
- **Impact:** No triage concepts are added to existing IGD Papers.
- **Architecture Impact:** SMASS master data.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-05

- **Status:** CLOSED
- **Decision:** D-07 — triage remains committed; failures are recorded as Pending Generation for
  manual MVP retry; generation is idempotent.
- **Rationale:** Protects the legal triage record and avoids duplicate snapshots.
- **Impact:** Requires a failed-generation store and retry surface.
- **Architecture Impact:** BILREG operational store and retry command.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-06

- **Status:** CLOSED
- **Decision:** D-09 — `LinkAssessmentByIgdVisitId` runs after `AssignRegister` persistence and
  links all pending assessments.
- **Rationale:** Deterministic, idempotent, and decoupled from registration persistence.
- **Impact:** Idempotent link-all behavior; no registration rollback.
- **Architecture Impact:** BILREG registration handler hooks and SMASS link command.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-07

- **Status:** CLOSED
- **Decision:** D-08 — registration linking uses a separate `RegistrationLinkStatus` field;
  completion state is unchanged.
- **Rationale:** Separates two independent concerns and avoids consumer ripple effects.
- **Impact:** Pending assessments may be `Drafting` or `Finished`.
- **Architecture Impact:** SMASS state model.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-08

- **Status:** CLOSED
- **Decision:** D-11 — no historical migration; legacy and pending paths coexist under the ordered
  rollout and activation toggle.
- **Rationale:** Backward compatibility with minimal deployment risk.
- **Impact:** Existing rows retain `IgdVisitId = NULL`.
- **Architecture Impact:** Schema additions and rollout configuration.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-09

- **Status:** CLOSED
- **Decision:** D-06 — a configured IGD `LayananId` is assigned at creation; the triage operator
  identity becomes `UserrId`; D-09 populates `LayananId` from registration data at link time.
- **Rationale:** Stable identity at generation while preserving pre-registration creation.
- **Impact:** Configuration ownership and validation remain implementation inputs.
- **Architecture Impact:** BILREG configuration and SMASS identity mapping.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-10

- **Status:** CLOSED
- **Decision:** D-13 — existing JWT Bearer authentication is used for generation and linking; no new
  auth scheme is introduced.
- **Rationale:** Reuses current infrastructure and avoids a second authentication model.
- **Impact:** Authorization enforcement must be wired onto the generate/link surfaces.
- **Architecture Impact:** SMASS endpoint authorization and BILREG token acquisition.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-11

- **Status:** CLOSED
- **Decision:** D-14 — BILREG triage is the legal record and audit source; SMASS assessments are
  derived clinical documents.
- **Rationale:** Preserves ownership boundaries and avoids legal ambiguity when generation fails.
- **Impact:** BILREG displays generation status and linked AssessmentId; SMASS displays
  `IgdVisitId`, `NoTriage`, `Generated From IGD Triage`, and labels pending assessments.
- **Architecture Impact:** Display surfaces and correlation fields.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

## OQ-12

- **Status:** CLOSED
- **Decision:** D-15 — never auto-delete, purge, or archive an assessment solely because
  registration never occurred; `PendingRegistration` is valid long-lived state; voiding a visit
  preserves assessments; provide a Pending Registration monitoring query/dashboard.
- **Rationale:** Preserves clinical history, is consistent with D-09 and D-14, avoids accidental
  data loss, and keeps MVP implementation simple.
- **Impact:** Monitoring implementation and host/ownership are implementation inputs; pending
  visibility remains governed by D-10.
- **Architecture Impact:** SMASS read surface for pending registration and retention policy.
- **Resolved By:** Analyst — Gap Closure
- **Resolved Date:** 2026-09-17

---

# 9. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [ ] Architecture can be finalized or updated

## Status

READY-FOR-PLANNING

## Notes

All GAP-001 through GAP-012 and OQ-01 through OQ-12 are CLOSED and decisions D-01 through D-15 are
recorded, so no blocking gap or open question remains. Status is intentionally left NOT-READY
because this activity does not grant the READY-FOR-PLANNING gate; the Architect sets that status
after confirming the target architecture can be finalized or updated from these approved decisions.

Items carried forward as architecture/implementation inputs (not feasibility blockers): Paper
seeding, mapping revision governance, configuration validation, failed-generation storage and
retry, link contract mechanics, by-visit query/indexing, authorization wiring, token acquisition,
display correlation, and monitoring ownership.

### FND-001 — Business-constraint ownership (out-of-artifact finding)

The prior revision of this artifact defined a business-rule catalogue `BR-01` through `BR-33`.
Business rules/constraints are not owned by FEASIBILITY-ASSESSMENT; they belong to the FEATURE
artifact (`b09-bilreg-api/docs/contexts/igd/igd-01-context.md`) or DOMAIN as appropriate. Those
rules are fully represented by the closure decisions D-01 through D-15 (see the trace in
Appendix A); they have been removed from this artifact as a standalone catalogue to avoid
duplicating FEATURE ownership. Recommendation: the Feature Knowledge Steward relocates the
`BR-01`–`BR-33` register into the owning FEATURE artifact and updates the `BR` references in
`igd-triage-abc-smass-architecture.md`. Because this is a FEATURE change, it requires an
ARCHITECTURE review request from the owning role. This finding does not block feasibility.

---

# 10. References

Referenced artifacts:

- DOMAIN: `b09-bilreg-api/docs/contexts/igd/igd-02-domain.md`
- FEATURE: `b09-bilreg-api/docs/contexts/igd/igd-01-context.md`
- ARCHITECTURE: `b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-architecture.md`

Referenced codebase locations:

- BILREG: `src/bilreg/Bilreg.Domain/IgdContext/**` (`IgdVisitModel`, `AtsTriageEngine`,
  `IgdVisitAssessTriageCmd`, `IgdVisitReAssessTriageCmd`, `IgdVisitAssignRegisterCmd`).
- BILREG persistence: `Bilreg.SqlDb/IgdContext/**` (`BILRG_IgdVisitTriage`).
- SMASS: `a043_smass_structuredmedicalassesment_api/Smass.Domain/AssesmentContext/AssesmentAgg/AssesmentModel.cs`,
  `CreateFixAssesmentCommand`, `SyncChartVitalSignCommand`, `AssesmentBuilder`.
- Web: `c012_myhospital_web/src/modules/Emergency/views/TriaseIGD.vue`.

Referenced documents:

- `b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-architecture.md`
- `b09-bilreg-api/docs/contexts/igd/igd-03-design.md`
- `b09-bilreg-api/docs/contexts/igd/igd-04-api-contract.md`
- Superseded (do not use as current design): `docs/contexts/igd/_unused/igd-triage-abc-smass-feasibility-assessment - Copy.md`

---

# Appendix A — Business-Rule to Decision Trace

Traceability from the former business-rule identifiers to the approved closure decisions. Rule
ownership is FEATURE/DOMAIN (see FND-001); the identifiers are retained here only so that existing
references can be remapped.

| Former BR | Closure decision(s) |
| --- | --- |
| BR-01 | D-01 (GAP-002) |
| BR-02 | D-09 (GAP-010) |
| BR-03 | D-09 (GAP-010) |
| BR-04 | D-04, D-09 (GAP-005, GAP-010) |
| BR-05 | D-10 (GAP-011) |
| BR-06 | D-04 (GAP-005) |
| BR-07 | D-04, D-07 (GAP-005, GAP-008) |
| BR-08 | D-06 (GAP-007) |
| BR-09 | Superseded by D-09 (GAP-010) |
| BR-10 | D-07 (GAP-008) |
| BR-11 | D-07 (GAP-008) |
| BR-12 | D-07 (GAP-008) |
| BR-13 | D-08 (GAP-009) |
| BR-14 | D-08 (GAP-009) |
| BR-15 | D-09 (GAP-010) |
| BR-16 | D-09 (GAP-010) |
| BR-17 | D-09 (GAP-010) |
| BR-18 | D-09, D-15 (GAP-010, OQ-12) |
| BR-19 | D-10 (GAP-011) |
| BR-20 | D-10 (GAP-011) |
| BR-21 | D-11 (GAP-012) |
| BR-22 | D-11 (GAP-012) |
| BR-23 | D-11 (GAP-012) |
| BR-24 | D-12 (GAP-003, GAP-004) |
| BR-25 | D-12 (GAP-003) |
| BR-26 | D-12 (GAP-003) |
| BR-27 | D-13 (OQ-10) |
| BR-28 | D-14 (OQ-11) |
| BR-29 | D-14 (OQ-11) |
| BR-30 | D-14 (OQ-11) |
| BR-31 | D-15 (OQ-12) |
| BR-32 | D-15 (OQ-12) |
| BR-33 | D-15 (OQ-12) |
