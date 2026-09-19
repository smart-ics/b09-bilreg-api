# FEASIBILITY ASSESSMENT: Generate SMASS Assessment from IGD Visit Triage ABC via Custom Paper

> **Document Status:** CANONICAL ANALYTICAL ARTIFACT
> **Date:** 2026-09-17 (updated: D-15 recorded)
> **Target Contexts:** IGD Visit (`b09-bilreg-api` / `IgdContext`) & Structured Medical Assessment (`a043_smass_structuredmedicalassesment_api`)
> **Skill Standard:** `b09-bilreg-api/docs/skills/feasibility-creation-skill.md`
> **Related Prior Artifact:** `b09-bilreg-api/docs/contexts/igd/igd-assessment-input-feasibility-analysis.md` (reverse direction; its open D-01 is superseded for this track by Decision D-01 below)
> **Decisions recorded here:** D-01 (APPROVED — assessment key-availability strategy, see §6) · D-02 (APPROVED — synchronous BILREG→SMASS generation, see §6) · D-03 (APPROVED Temporary — configurable ATS-to-SMASS mapping table with provisional seed-derived values, see §6) · D-04 (APPROVED — new assessment per triage event, immutable snapshots correlated by IgdVisitId + NoTriage, see §6) · D-05 (APPROVED — dedicated IGD Triage Paper, see §6) · D-06 (APPROVED — identity mapping: configurable IGD LayananId at creation, UserrId inherited from triage operator, RegId/PasienId nullable until link, see §6) · D-07 (APPROVED — generation failure handling: triage never rolled back, idempotent generate on (IgdVisitId, NoTriage), failures recorded as Pending Generation items, manual retry for MVP, see §6) · D-08 (APPROVED — registration-link lifecycle as a separate RegistrationLinkStatus field, AggStateEnum untouched; PendingRegistration assessments may be Drafting or Finished, see §6) · D-09 (APPROVED — link via LinkAssessmentByIgdVisitId after AssignRegister persistence: populate RegId/PasienId/LayananId, set Registered, idempotent, no rollback, retry via D-07 mechanism; ReplaceRegister replaces with latest; VoidVisit preserves records, see §6) · D-10 (APPROVED — PendingRegistration assessments visible only through IGD Visit workflows via ListByIgdVisitId; excluded from all RegId-based surfaces; full RegId-based participation after link, see §6) · D-11 (APPROVED — no historical migration, existing rows keep IgdVisitId NULL, legacy and pending paths coexist; deploy DB → SMASS app → verify legacy → enable IGD integration → monitor; feature toggle IgdVisit:EnableSmassIntegration, see §6) · D-12 (APPROVED — ATS-to-SMASS mapping table owned by SMASS master data; provisional seed-derived values are the approved source of truth for demo/UAT/initial deployment; no clinical sign-off for MVP, no production gate; later clinical revisions are configuration changes, Paper stays stable, see §6) · D-13 (APPROVED — BILREG gateway authenticates to SMASS with existing JWT Bearer, no new auth scheme; same token validation config; scope covers GenerateAssessment + LinkAssessmentByIgdVisitId, see §6) · D-14 (APPROVED — BILREG triage record is the legal record; SMASS assessments are derived documents; BILREG is triage history/audit source of truth; BILREG shows generation status + linked AssessmentId; SMASS shows source info; pending labelled "Pending Registration", see §6) · D-15 (APPROVED — no auto delete/purge/archive for never-registered assessments; PendingRegistration is valid long-lived; void preserves records; Pending Registration monitoring query/dashboard; future retention via governance without semantic change, see §6)

---

## 1. Executive Summary **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

### Request

On IGD Visit triage submit (`POST /api/IgdVisit/{id}/triage`, "Triage ABC" form), automatically generate an assessment document in `a043_smass_structuredmedicalassesment_api` conforming to `Smass.Domain/AssesmentContext/AssesmentAgg/AssesmentModel.cs`, using a custom paper if possible (requester notes assessment creation currently requires a Paper).

### Recommendation

Planning may proceed. All twelve gaps are resolved, all approval gates are cleared, and Open Question No.10 (service auth) is closed, and Open Question No.11 (legal record) is closed, and Open Question No.12 (orphan policy) is closed — D-12 removes the former D-03 production gate by approving the SMASS-owned mapping table's provisional values as the source of truth for demo, UAT, and initial deployment, with later clinical revisions handled as configuration changes, and D-13 fixes gateway authentication to the existing JWT Bearer mechanism with no new auth scheme. Fifteen decisions are now fixed — D-02 (synchronous backend-to-backend BILREG→SMASS, every triage generates), D-01 (IGD-originated assessments may be created pre-registration keyed by `IgdVisitId` in `PENDING_REGISTRATION` status, auto-linked on `AssignRegister`), D-03 (configurable ATS-to-SMASS mapping table; seed-derived provisional mappings for demo/dev; clinical approval gates production), D-04 (each triage event generates a new assessment as an immutable snapshot, correlated by `IgdVisitId` + `NoTriage`; all pending assessments link on `AssignRegister`), D-05 (a dedicated IGD Triage Paper is created; triage concepts are not embedded in any existing IGD Paper), D-06 (generated assessments use a configurable IGD `LayananId` assigned at creation, inherit `UserrId` from the triage operator, and keep `RegId`/`PasienId` nullable until link), D-07 (triage persistence is primary and never rolled back for generation failure; generation is idempotent on (`IgdVisitId`, `NoTriage`); failures are recorded as Pending Generation items with manual retry for MVP), D-08 (registration-link lifecycle lives in a separate `RegistrationLinkStatus` field — `PendingRegistration`/`Registered` — with `AggStateEnum` untouched; a `PendingRegistration` assessment may be `Drafting` or `Finished`; linkage never affects completion status), and D-09 (`LinkAssessmentByIgdVisitId` triggered synchronously after successful `AssignRegister` persistence populates `RegId`/`PasienId`/`LayananId` and sets `Registered` on all pending assessments of the visit, idempotently; registration is never rolled back for link failure, with retry via the D-07 mechanism; `ReplaceRegister` replaces linked values with the latest registration; voiding a visit preserves all assessments), and D-10 (`PendingRegistration` assessments are visible only through IGD Visit workflows via `ListByIgdVisitId` — never through `ListByRegId`, `Catalog/{regId}`, OFTA integrations, or RegId-based reporting — and participate in all existing RegId-based SMASS queries once `Registered`), and D-11 (no historical data migration with existing rows keeping `IgdVisitId = NULL`; legacy and pending paths coexist; deployment ordered DB → SMASS app → verify legacy → enable IGD integration → monitor, gated by the `IgdVisit:EnableSmassIntegration` feature toggle, and D-12 (the mapping table is owned by SMASS master data; its provisional seed-derived values are the approved source of truth for demo, UAT, and initial deployment, with no clinical sign-off required for MVP and no production gate — later clinical revisions are configuration changes and the triage Paper stays stable), and D-13 (the BILREG-owned gateway authenticates to SMASS with the existing JWT Bearer mechanism — no API key, shared secret, or custom scheme — validated with the same token configuration as existing internal APIs, covering both operations), and D-14 (the BILREG triage record is the legal record of triage; generated SMASS assessments are derived clinical documents; BILREG remains the source of truth for triage history and audit; BILREG displays generation status + linked AssessmentId; SMASS displays source info and labels pending assessments "Pending Registration"), and D-15 (never-registered assessments are never auto deleted/purged/archived; PendingRegistration is valid long-lived; void preserves records; Pending Registration monitoring query/dashboard; future retention via governance without semantic change). Together D-01 and D-02 close the former key-availability blocker GAP-002 at decision level, D-03 closes the mapping blockers GAP-003/GAP-004 provisionally, D-04 closes the versioning blocker GAP-005, D-05 closes the paper-ownership blocker GAP-006, D-06 closes the identity-mapping blocker GAP-007, D-07 closes the generation-failure blocker GAP-008, D-08 closes the lifecycle-modeling blocker GAP-009, D-09 closes the linking blocker GAP-010, D-10 closes the visibility blocker GAP-011, D-11 closes the migration/rollout blocker GAP-012, D-12 clears the clinical-approval production gate (mapping table approved as source of truth; later revisions are config changes), D-13 closes service auth with the existing JWT Bearer (no new scheme), D-14 fixes legal-record ownership on the BILREG triage record with derived SMASS documents plus both sides' display duties, D-15 fixes orphan policy (preserve, long-lived pending, monitoring surface, governance-owned future retention), and the custom-paper mechanism is confirmed viable (proven `SyncChartVitalSignCommand` / `AddCustomSectionAssesmentCommand` precedent). Remaining before production slicing are planning residuals only: triage Paper creation/seed mechanics, config-key ownership/validation, Pending Generation store + manual-retry surface, link payload/contract mechanics + retry-surface wiring, `ListByIgdVisitId` implementation + index, and toggle ownership/default state, plus authorization enforcement wiring + gateway token acquisition (D-13 residuals), plus BILREG generation-status display + linked-AssessmentId correlation and SMASS source-info display + pending labelling (D-14 residuals), plus Pending Registration monitoring implementation + host/ownership (D-15 residuals).

### Feasibility Result

```text
FEASIBLE
```

All twelve gaps resolved; all approval gates cleared (D-12 removes the former D-03 production gate). What remains are planning-level residuals, not discovery blockers.

---

## 2. Request Understanding **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

- **Requested capability:** each Triage ABC input in the IGD Visit feature produces (generates) an assessment in SMASS.
- **Business objective (as stated):** triage data entered once in IGD should also exist as a structured medical assessment.
- **Expected outcome (as stated):** a SMASS `AssesmentModel` populated from the triage form, created under a custom paper rather than forcing the triage flow through an existing clinical paper.
- **Decided (D-02, APPROVED):** generation is synchronous backend-to-backend (BILREG→SMASS) triggered immediately after successful triage submission, and every triage generates an assessment. Frontend is not involved in orchestration.
- **Decided (D-01, APPROVED):** an assessment originating from IGD Visit may be created before administrative registration is completed, keyed by `IgdVisitId` as operational reference with `RegId`/`PasienId`/`LayananId` optional while in `PENDING_REGISTRATION` status; on `AssignRegister`, the system automatically links and populates the three keys. See §6 for the full decision record and §12 for its consequences.
- **Decided (D-03, APPROVED Temporary):** a configurable ATS-to-SMASS mapping table carries the score→concept mappings; initial values are derived from existing SMASS Concept/ConceptPreference seed data and are provisional for demo and development; production deployment requires clinical review and approval of all mappings. See §6.
- **Decided (D-04, APPROVED):** each triage event generates a new assessment; assessment records are immutable snapshots of the patient's condition at triage time, correlated by `IgdVisitId` + `NoTriage`; all pending assessments of a visit link when `AssignRegister` occurs. See §6.
- **Decided (D-05, APPROVED):** a dedicated IGD Triage Paper is created for all assessments generated from IGD Triage; triage concepts are not embedded in any existing IGD Paper; future triage changes are versioned within the triage paper lifecycle. See §6.
- **Decided (D-06, APPROVED):** generated assessments use a configurable IGD `LayananId` (e.g. `IgdVisit:SmassLayananId`) assigned at creation independent of Registration; `UserrId` is inherited from the user who performed the triage (operator becomes assessment creator); `RegId`/`PasienId` stay nullable during `PENDING_REGISTRATION` and are populated at `AssignRegister`. See §6.
- **Decided (D-07, APPROVED):** triage persistence is the primary operation and is never rolled back for generation failure; generation is idempotent on (`IgdVisitId`, `NoTriage`); failed attempts are recorded as Pending Generation items for later retry; MVP retry is manual, with automatic workers possible later. See §6.
- **Decided (D-08, APPROVED):** the registration-link lifecycle is a separate `RegistrationLinkStatus` field (`PendingRegistration`/`Registered`); `AggStateEnum` is not extended; a `PendingRegistration` assessment may be `Drafting` or `Finished`; linkage never affects completion status. See §6.
- **Decided (D-09, APPROVED):** linking is `LinkAssessmentByIgdVisitId`, triggered synchronously after successful `AssignRegister` persistence; it populates `RegId`/`PasienId`/`LayananId` and sets `Registered` on all pending assessments of the visit, idempotently; registration is never rolled back for link failure (retry via the D-07 mechanism); `ReplaceRegister` replaces linked values with the latest registration; voiding a visit preserves all assessments. See §6 (including the noted D-06/BR-09 interaction).
- **Decided (D-10, APPROVED):** `PendingRegistration` assessments are visible only through IGD Visit workflows via `ListByIgdVisitId`; never through `ListByRegId`, `Catalog/{regId}`, OFTA integrations, or RegId-based reporting; once `Registered`, they participate in all existing RegId-based SMASS queries. See §6.
- **Decided (D-11, APPROVED):** no historical data migration — existing assessments keep `IgdVisitId = NULL`; legacy creation and the pending-registration path coexist during rollout; deployment ordered DB → SMASS app → verify legacy → enable IGD integration → monitor; activation controlled by the `IgdVisit:EnableSmassIntegration` feature toggle. See §6.
- **Decided (D-12, APPROVED):** the ATS-to-SMASS mapping table is owned by SMASS master data; its provisional seed-derived values are the approved source of truth for demo, UAT, and initial deployment; no clinical sign-off is required for MVP and no production gate is imposed; later clinical revisions are configuration changes and the dedicated triage Paper stays stable. See §6.
- **Decided (D-13, APPROVED):** the BILREG-owned gateway authenticates to SMASS with the existing JWT Bearer mechanism — no API key, shared secret, or custom scheme; SMASS validates with the same token configuration as existing internal APIs; scope covers both gateway operations (generate + link). See §6.
- **Decided (D-14, APPROVED):** the BILREG triage record is the legal record of triage; generated SMASS assessments are derived clinical documents; BILREG remains the source of truth for triage history and audit. BILREG displays assessment-generation status + linked AssessmentId; SMASS displays source info (`IgdVisitId`, `NoTriage`, "Generated From IGD Triage"); pending assessments are labelled "Pending Registration". See §6.
- **Decided (D-15, APPROVED):** never-registered assessments are never auto deleted/purged/archived; `PendingRegistration` is a valid long-lived state; voiding preserves records; a Pending Registration monitoring query/dashboard provides operational visibility; future retention policies come through operational governance without changing assessment semantics. See §6.
- **What remains unspecified:** nothing — every numbered question (No.10 service auth, No.11 legal record, No.12 orphan policy) and every gap (GAP-001–GAP-012) is decided (D-01–D-15). What remains are the planning-level residuals enumerated in §11.

---

## 3. Current State Analysis **[Changed by D-08]**

### Existing Business Flow

1. BILREG creates `IgdVisit` visit-first (visitor data, no `RegId` required at creation).
2. Triage is submitted via `POST /api/IgdVisit/{id}/triage` (`src/bilreg/Bilreg.Api/Controllers/IgdContext/IgdVisitController.cs:39`) or `POST /api/IgdVisit/{id}/re-triage` (`IgdVisitController.cs:58`), carrying 6 integer scores: Airways (0–2), Breathing (0–5), BloodCirculation (0–4), GCS Eye (1–4), GCS Motor (1–6), GCS Voice (1–5), plus manual-override-black flag, override reason, notes, user (`src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitAssessTriageCmd.cs:11-23`, guards at `IgdVisitAssessTriageCmd.cs:52-57`).
3. `AtsTriageEngine` computes level ATS1–5 and color Red/Yellow/Green (`src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/TriageEngine/TriageEngine.cs:55-70`); the visit aggregate stores the result and appends history (`src/bilreg/Bilreg.Domain/IgdContext/IgdVisitFeature/IgdVisitModel.cs:226-279`). Triage is a precondition for bed assignment (`IgdVisitModel.cs:290-292`), while discharge requires a linked `RegId` (`IgdVisitModel.cs:437-451`). Registration linking itself happens in `IgdVisitAssignRegisterHandler`, which today only loads visit + reg, calls `visit.AssignRegister`, and saves — with no outbound call (`src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitAssignRegisterCmd.cs:32-48`).

### Existing Components

- **BILREG triage persistence:** `BILRG_IgdVisitTriage` table (`src/bilreg/Bilreg.SqlDb/IgdContext/IgdVisitFeature/BILRG_IgdVisitTriage.sql:1-23`) + `src/bilreg/Bilreg.Infrastructure/IgdContext/IgdVisitFeature/IgdVisitTriageDal.cs:29-66`. No outbound call to SMASS exists in the bilreg triage path (no SMASS client/reference found in the triage use cases; triage handler only loads/saves the visit via `IIgdVisitRepo`). Per D-02, BILREG will introduce an outbound SMASS gateway and becomes responsible for invoking assessment generation after triage completion.
- **SMASS assessment creation (Paper-mandatory, confirmed):** `CreateFixAssesmentCommand` guards `PasienId`, `RegId`, `LayananId`, `PaperId`, `UserrId`, date/time all non-empty (`Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/CreateFixAssesmentCommand.cs:62-69`); the builder resolves Paper and throws when missing (`Smass.Application/AssesmentContext/AssesmentAgg/Workers/Creators/AssesmentBuilder.cs:250-261`), resolves Reg/Layanan through external services (`AssesmentBuilder.cs:181-235`), and stamps the caller id as user (`AssesmentBuilder.cs:237-248`). **D-01 explicitly overrides the Reg/Pasien/Layanan key guards for the IGD-originated path** — the guard relaxation (or a dedicated triage-create command) is now a required design change, not an open strategy question. See §12.
- **SMASS custom-section mechanism (the "custom paper" hook the request asks about):** assessments accept custom sections via `PATCH /api/Assesment/AddSection` (`Smass.Api/Controllers/AssesmentContext/AssesmentController.cs:109-115` → `Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/AddCustomSectionAssesmentCommand.cs:15-27`, handler `AddCustomSectionAssesmentCommand.cs:82-102`); Paper supports `addCustomSection` (`Smass.Api/Controllers/StructureContext/PaperController.cs:35-41`); the builder's `AddSection` explicitly handles `SectionType.CustomSection` (`AssesmentBuilder.cs:263-285`).
- **Precedent for programmatic generation:** `SyncChartVitalSignCommand` creates an assessment against a dedicated `DefaultPaperId = "PP-001-CHRT"` (`Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/SyncChartVitalSignCommand.cs:39`), then writes monitored values into custom-section concepts via `CustomSectionDal`/`ConceptBuilder` (`SyncChartVitalSignCommand.cs:169-188`, `365-425`). This is the closest existing analogue to "triage input → generated assessment."
- **SMASS status model (verified for D-01 design; modeling decided by D-08):** `AggStateEnum` today admits only `Created`, `Drafting`, `Finished`, `Deleted` (`Smass.Domain/AssesmentContext/AssesmentAgg/AggState.cs:3-9`). Neither `PENDING_REGISTRATION` nor `REGISTERED` exists anywhere in the SMASS codebase (verified: no `IgdVisitId` / `PENDING_REGISTRATION` references found). Per D-08 the enum stays untouched: the D-01 lifecycle lives in a new separate `RegistrationLinkStatus` field, so no existing `AssesmentState` consumer changes behavior — see GAP-009 and §12.
- **SMASS persistence surface (verified for D-01 design):** `AssesmentDal.Insert`/`Update` address a fixed `SMASS_Assesment` column set with no `IgdVisitId` (`Smass.Infrastructure/AssesmentContext/FixAssesmentAgg/Repos/AssesmentDal.cs:24-57`, `:59-102`). The D-01 field is a confirmed schema + DAL + mapper change. See §12.
- **Related prior analysis:** `b09-bilreg-api/docs/contexts/igd/igd-assessment-input-feasibility-analysis.md` assessed the reverse direction (30-item SMASS-first form → BILREG sync) with its own D-01 open (relax-guard vs. hybrid). The D-01 recorded in §6 of this document supersedes that framing for this track: it is neither "guard relaxation with `RegId=''`" nor "registration-first" — it is a first-class pre-registration identity (`IgdVisitId`) with an explicit lifecycle and auto-linking.

### Existing Database

- BILREG: `BILRG_IgdVisitTriage` (PK `IgdVisitId, NoTriage`), `BILRG_IgdVisit`.
- SMASS: `SMASS_Assesment` (no `IgdVisitId` column; `RegId` treated as required key — both now change per D-01), `SMASS_AssesmentSection`, `SMASS_AssesmentConcept`; structure tables for Paper / CustomSection / Concept / ConceptPreference.
- No shared triage↔assessment link table exists in either database — and per D-01 none is needed: `IgdVisitId` stored on the assessment is itself the correlation.

### Existing Integrations

- None between BILREG triage and SMASS assessments today.
- SMASS already calls outward to Billing for Reg resolution (`Smass.Infrastructure/BillingContext/Services/GetRegService.cs:26-58`); per D-02 the generation dependency runs in the opposite direction (BILREG→SMASS, synchronous), and per D-01 a second synchronous call (link-on-`AssignRegister`) is added in the same direction.

---

## 4. Impact Analysis **[Changed by D-01]**

### Backend Impact **[Changed by D-08, D-09]**

- **BILREG (`IgdVisitFeature`):** `IgdVisitAssessTriageCmd`/`IgdVisitReAssessTriageCmd` handlers (generation trigger, decided per D-02: after successful triage completion — the gateway now sends `IgdVisitId` as the operational reference instead of requiring keys, per D-01); `IgdVisitAssignRegisterCmd` handler (trigger decided by D-09: synchronously after successful `AssignRegister` persistence, invoking SMASS `LinkAssessmentByIgdVisitId` with the assigned registration data; registration never rolled back for link failure); `IgdVisitModel.AssessTriage`, `IgdVisitTriageDal`; plus the outbound SMASS gateway client (generation + linking operations) owned by BILREG per D-02. No domain scoring change needed — engine output is reusable as-is.
- **SMASS (`AssesmentAgg`, `StructureContext`):** `AssesmentModel` gains `IgdVisitId` (new `IIgdVisitKey`-style reference); assessment creation path gains a pre-registration variant (relaxed `CreateFixAssesmentCommand` guards or a dedicated triage-create command — design choice, see GAP-009/GAP-010); `AssesmentBuilder.Reg` resolution is skipped/deferred on the pending path (it currently throws on missing reg, `AssesmentBuilder.cs:181-194`); new registration-linking operation (load-by-`IgdVisitId` → populate keys → flip lifecycle); status handling via the new separate `RegistrationLinkStatus` field with `AggStateEnum` untouched per D-08 (a pending assessment may be `Drafting` or `Finished`; linkage never touches completion status); new list-by-`IgdVisitId` query; `AssesmentValidator`/`AssesmentWriter`, section/concept builders, `CustomSection` + `Concept` master data, `Paper` master data (unchanged in kind, extended in content).

### Database Impact **[Changed by D-01, D-11]**

- **BILREG:** no schema change. Correlation is resolved by D-01 itself: the assessment carries `IgdVisitId`, so no BILREG-side link table or reference column is required.
- **SMASS:** schema change now required (this supersedes the earlier "no schema redesign" finding for this track): add `IgdVisitId` column to `SMASS_Assesment` (nullable, indexed — pending assessments are looked up by it at link time); add a separate `RegistrationLinkStatus` column (`PendingRegistration`/`Registered`) per D-08 — `AggStateEnum` is not extended, so no existing state consumer changes; `RegId`/`PasienId`/`LayananId` must accept empty/placeholder on the pending path (column defaults already tolerate `''`; the enforcement point moves from DDL to application validation); plus master-data rows (Paper, CustomSection, CustomSectionConcept, preferences as needed) and transactional rows in existing `SMASS_Assesment*` tables. Migration per D-11 **[Changed by D-11]:** no historical data migration — existing rows keep `IgdVisitId = NULL`; legacy creation and the pending path coexist during rollout; deployment ordered DB → SMASS app → verify legacy → enable IGD integration via the `IgdVisit:EnableSmassIntegration` toggle → monitor (this replaces the provisional backfill assumption stated here earlier).

### Frontend Impact **[Changed by D-14]**

- Per D-02, orchestration stays in the backend: no triage-form orchestration change. D-14 introduces the first frontend obligations, display-only: BILREG surfaces must show per-triage assessment-generation status plus the linked AssessmentId; SMASS surfaces must show source info (`IgdVisitId`, `NoTriage`, "Generated From IGD Triage") and label pending assessments "Pending Registration". Planning inputs: how BILREG obtains the linked AssessmentId for display — persisting the gateway-returned id against the triage row (new BILREG-side persistence, mechanism TBD) vs. looking it up from SMASS by (`IgdVisitId`, `NoTriage`) — and which IGD/SMASS screens host each element. D-01 adds no further frontend obligation beyond the pending label (lifecycle itself remains a backend concern).

### Integration Impact **[Changed by D-01, D-07, D-09]**

- Two synchronous BILREG→SMASS operations (both owned by BILREG):
  1. **Generate** (D-02, reshaped by D-01): after successful triage submission, create assessment with `IgdVisitId` (+ assessor, timestamps, triage concepts) in `PENDING_REGISTRATION`; keys optional.
  2. **Link** (D-01, contract decided by D-09): after successful `AssignRegister` persistence, `LinkAssessmentByIgdVisitId` populates `RegId`/`PasienId`/`LayananId` and sets `Registered` on all pending assessments of the visit, idempotently; registration is never rolled back for link failure (retry via the D-07 mechanism); `ReplaceRegister` re-links with the latest registration data; voiding a visit preserves all assessments (no delete/unlink).
- Per constraints, both stay synchronous; no async/outbox mechanism is introduced. Generate-call failure behavior is decided by D-07 (no rollback of triage; failure recorded as a Pending Generation item; idempotent retry on (`IgdVisitId`, `NoTriage`); manual retry for MVP). Link-call failure behavior is decided by D-09 (no rollback of registration; retry via the D-07 mechanism).

### Security Impact **[Changed by D-13]**

- Service-to-service authentication between BILREG and SMASS is decided by D-13: the BILREG-owned gateway uses the existing JWT Bearer mechanism for both operations (generate + link), with SMASS validating under its existing token configuration — no API key, shared secret, or custom scheme. Verified present on both sides today (`Smass.Api/Configurations/PresentationService.cs:22`, `Bilreg.Api/Configurations/PresentationService.cs:76`). Enforcement residual: SMASS's `AssesmentController` currently carries no `[Authorize]` (consistent with neighboring SMASS controllers where it is commented out), so the generate/link surfaces must actually enforce authorization under the existing scheme — a planning-level wiring item, not a new mechanism. User-identity mapping (BILREG `AssessorUserId` → SMASS `UserrId` per D-06) and auditability of generated and auto-linked clinical content (the link event must itself be auditable) remain as stated.

---

## 5. Gap Analysis **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12]**

| Gap ID | Type | Description | Status |
| --- | --- | --- | --- |
| GAP-001 | Integration | No BILREG→SMASS path exists on the triage flow. | **RESOLVED by D-02** — synchronous backend-to-backend (BILREG→SMASS) after successful triage; BILREG owns the outbound gateway. |
| GAP-002 | Data | `CreateFixAssesmentCommand` mandates non-empty `RegId`/`PasienId`/`LayananId`, but IGD triage is valid pre-`AssignRegister` when `RegId` is typically absent. | **RESOLVED by D-01 (decision level)** — IGD-originated assessments are created with `IgdVisitId` in `PENDING_REGISTRATION` with the three keys optional; auto-linked on `AssignRegister`. Implementation of the relaxed validation + linking is now tracked under GAP-009/GAP-010 and §12 (was: OPEN key-availability strategy). |
| GAP-003 | Data | Score→concept value-domain mismatch: BILREG triage fields are integers (Airways 0–2, Breathing 0–5, Circulation 0–4), while the seeded SMASS triage concepts are qualifier-type (e.g. `CC052B` Jalan Nafas, `CC052C` Pernafasan, `CC052D` Sirkulasi with text preferences; `Smass.Db/DataSeeds/ConceptDataSeed.sql:28475-28566`) plus derived score/category concepts (`CC0364-CC0367`, `CC0282`, `CC0656`, `CC0283`). | **RESOLVED by D-03 + D-12** — configurable ATS-to-SMASS mapping table owned by SMASS master data; seed-derived provisional values are the approved source of truth for demo/UAT/initial deployment with no clinical sign-off for MVP and no production gate; later clinical revisions are configuration changes. |
| GAP-004 | Data | GCS breakdown mapping: BILREG captures GCS Eye/Motor/Voice separately; SMASS evidence shows GCS-total concept `CC000B` referenced by triage formulas, with per-component mapping covered as rows of the same table. | **RESOLVED by D-03 + D-12** — per-component mappings are rows of the SMASS-owned mapping table under the same approved-provisional terms as GAP-003. |
| GAP-005 | Functional | Versioning semantics: D-02 establishes that every triage generates an assessment; the new-vs-append choice was undecided, with BILREG-side history (`NoTriage`) lacking a SMASS-side counterpart. | **RESOLVED by D-04** — each triage event generates a new assessment as an immutable snapshot, correlated by `IgdVisitId` + `NoTriage` (the SMASS side now carries the BILREG history key); multiple assessments per `IgdVisitId` are expected and all pending ones link on `AssignRegister`. Residual: generate-call idempotency (a retried failed call must not double-create for the same `NoTriage`) stays under GAP-008. |
| GAP-006 | Functional | Custom-paper ownership: new dedicated triage Paper vs. adding a triage CustomSection to an existing IGD Paper; who versions it and how existing assessments resolve it. | **RESOLVED by D-05** — a dedicated IGD Triage Paper is created; no triage concepts go into any existing IGD Paper; the triage Paper is the canonical target for all triage-generated assessments and versions triage changes within its own lifecycle, isolated from unrelated IGD papers. Residual: the Paper's id, creation/seed mechanics, and lifecycle governance are planning inputs, not new gaps. |
| GAP-007 | Technical | Identity mapping: which `LayananId` and `UserrId` the generated assessment carries (`Userr()` currently stamps caller id — `AssesmentBuilder.cs:237-248`); D-01 made `LayananId` optional at creation and populated at link time, leaving the value sources undecided. | **RESOLVED by D-06** — `LayananId` comes from application configuration (configurable IGD value, e.g. `IgdVisit:SmassLayananId`), assigned at creation independent of Registration (no link-time resolution needed); `UserrId` is inherited from the triage operator; `RegId`/`PasienId` stay nullable until link (as per D-01). Residual planning input: the config key's ownership/validation (missing-or-invalid value behavior) and audit labelling of operator-stamped generated content. |
| GAP-008 | Technical | Failure semantics for the decided synchronous generate call undefined: triage is saved before generation is invoked (D-02 trigger), so SMASS-call failure leaves triage-saved-but-assessment-missing; retry/compensation behavior and idempotency are not defined. No distributed transaction exists today. | **RESOLVED by D-07** — triage is primary and never rolled back; generation is idempotent on (`IgdVisitId`, `NoTriage`); failures are recorded as Pending Generation items for later retry; MVP retry is manual (auto workers later if needed). Residual planning inputs: the Pending Generation record store (mechanism/location) and the manual-retry surface. |
| GAP-009 | Technical/Data | Status-lifecycle modeling for `PENDING_REGISTRATION`/`REGISTERED` was undecided: extend `AggStateEnum` (currently `Created, Drafting, Finished, Deleted` — `AggState.cs:3-9`) vs. a separate registration-link status field. | **RESOLVED by D-08** — separate `RegistrationLinkStatus` field (`PendingRegistration`/`Registered`); `AggStateEnum` untouched, so no existing `AssesmentState` consumer changes behavior; linkage is orthogonal to completion (a pending assessment may be `Drafting` or `Finished`). Residual planning inputs: the new column + DAL/mapper handling and the link operation's flip mechanics (under GAP-010). |
| GAP-010 | Integration/Functional | Registration-linking mechanism: trigger position in `IgdVisitAssignRegisterHandler`, the SMASS linking operation contract (match by `IgdVisitId`, populate keys atomically, flip lifecycle), multi-assessment handling, link-call failure semantics, retry/idempotency, and the `ReplaceRegister` + void-visit edges. | **RESOLVED by D-09** — `LinkAssessmentByIgdVisitId` triggered synchronously after successful `AssignRegister` persistence; populates `RegId`/`PasienId`/`LayananId` and sets `Registered` on all pending assessments of the visit (link-all per D-04), idempotently; no rollback of registration on link failure, retry via the D-07 mechanism; `ReplaceRegister` replaces linked values with the latest registration; voiding a visit preserves all assessments. Residual planning inputs: operation payload/contract mechanics and the retry-surface wiring into the D-07 mechanism. |
| GAP-011 | Data/UX | Pending-assessment visibility: existing reg-keyed reads (`ListByRegId`, `Catalog/{regId}`) cannot return `PENDING` assessments (no `RegId` yet); report/OftaDoc joins on `RegId` exclude them; which surfaces show pending assessments and the list-by-`IgdVisitId` query (+ index) were undefined. | **RESOLVED by D-10** — `PendingRegistration` assessments are visible only through IGD Visit workflows via `ListByIgdVisitId`; excluded from `ListByRegId`, `Catalog/{regId}`, OFTA integrations, and RegId-based reporting; once `Registered`, full participation in all existing RegId-based queries (no changes to those reads). Residual planning inputs: `ListByIgdVisitId` implementation + index. |
| GAP-012 | Technical | Migration and rollout: `IgdVisitId` column backfill for existing `SMASS_Assesment` rows, deploy ordering (column + DAL before gateway traffic), and coexistence of the legacy fully-keyed path with the pending path during rollout. | **RESOLVED by D-11** — no historical data migration (existing rows keep `IgdVisitId = NULL`); legacy and pending paths coexist; deployment ordered DB → SMASS app → verify legacy → enable IGD integration → monitor; activation via the `IgdVisit:EnableSmassIntegration` feature toggle. (This replaces the provisional backfill assumption stated in §4 earlier.) |

---

## 6. Solution Options **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

### Decision D-01 — Assessment Key Availability Strategy (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - Assessment originating from IGD Visit may be created before administrative registration is completed.
  - SMASS shall be extended with `IgdVisitId` as an operational reference.
  - `RegId`, `PasienId`, and `LayananId` become optional while the assessment is in `PENDING_REGISTRATION` status.
  - When `AssignRegister` occurs in IGD Visit, the system shall automatically link and populate `RegId`, `PasienId`, and `LayananId` into the corresponding assessment.
- **Assessment notes (analysis, not new decisions):** this supersedes the prior doc's open D-01 framing for this track — it is neither "`RegId=''` relaxation" nor "registration-first", but a first-class pre-registration identity with lifecycle + auto-linking. It resolves GAP-002 at decision level and converts it into bounded design work (GAP-009–GAP-012, §12). It does not contradict D-02; it reshapes the generate call's payload (keys optional, `IgdVisitId` mandatory) and adds the link call in the same synchronous style.

### Decision D-03 — Temporary Triage Concept Mapping Strategy (APPROVED Temporary) **[New]**

- **Status:** APPROVED (Temporary).
- **Decision (as given):**
  - Implement a configurable ATS-to-SMASS mapping table.
  - Initial mappings will be derived from the existing SMASS Concept and ConceptPreference seed data and treated as provisional mappings for demo and development purposes.
  - Production deployment requires clinical review and approval of all mappings.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-003 at strategy level and GAP-004 as rows of the same table, provisionally — demo/dev planning may proceed against seed-derived values, while production readiness now depends on a clinical-approval gate whose scope, approvers, and evidence bar are still undefined (tracked in §9). It does not contradict D-01/D-02; it supplies the value-translation the generate call was missing. Table ownership (which service owns the table — SMASS master data vs. gateway configuration) and storage mechanism are design choices left to planning, flagged here so they are not silently assumed.

### Decision D-04 — Triage Assessment Versioning Strategy (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - Each triage event generates a new assessment.
  - Assessment records are immutable snapshots of the patient's condition at the time of triage.
  - Correlation is maintained through `IgdVisitId` + `NoTriage`.
- **Rationale (as given):**
  - Preserves complete clinical history.
  - Aligns with existing BILREG triage history.
  - Aligns with D-02 requirement that every triage generates an assessment.
  - Simplifies D-01 registration-linking behavior.
- **Consequence (as given):** Multiple assessments may exist for a single `IgdVisitId` and all pending assessments are linked when `AssignRegister` occurs.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-005 fully — no versioning alternative remains. It interacts with three existing items, all consistently: (a) D-01 linking now has a fixed multi-match rule (link all pending assessments of the visit — the GAP-010 "multi-assessment handling" sub-question is decided in-rule; only contract mechanics and failure/edge behavior stay open); (b) the SMASS `IgdVisitId` field design (§12.3 item 1) extends to carrying `NoTriage` as the snapshot key (BILREG's `BILRG_IgdVisitTriage` PK is `IgdVisitId, NoTriage`, so the correlation mirrors an existing key rather than inventing one); (c) "immutable snapshot" constrains the GAP-009 lifecycle design — post-creation mutation of clinical content is out, while the D-01 key-population + lifecycle flip remains the sole sanctioned mutation (plus the still-open finish/sign rules for pending assessments). Generate-call idempotency (same `NoTriage` retried) remains required under GAP-008.

### Decision D-05 — Dedicated IGD Triage Paper (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - Create a dedicated IGD Triage Paper.
  - Do not embed triage concepts into an existing IGD Paper.
- **Rationale (as given):**
  - Triage is a distinct clinical workflow.
  - Triage versioning should be independent from other IGD assessment content.
  - Simplifies deployment and future changes.
  - Avoids unintended impact on existing IGD assessments and papers.
  - Aligns with D-04 (1 Triage = 1 Assessment).
- **Consequence (as given):**
  - A new Paper will be created specifically for IGD Triage assessments.
  - The Paper becomes the canonical target for all assessments generated from IGD Triage.
  - Future triage changes are versioned within the triage paper lifecycle and do not affect unrelated IGD papers.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-006 fully — no paper alternative remains. It is consistent with every prior decision: the generate call's `PaperId` now has a fixed canonical target; D-04 snapshots each reference the triage Paper, so per-snapshot Paper versioning questions reduce to the triage paper lifecycle (existing assessments keep resolving against the version they were created with — standard Paper semantics, no new rule invented here); D-03's clinical-approval gate now explicitly covers the triage Paper's concept set. It does not contradict D-01/D-02/D-03/D-04.

### Decision D-06 — Identity Mapping Strategy (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - Generated IGD Triage Assessments shall use a configurable IGD `LayananId` defined in application configuration (example: `IgdVisit:SmassLayananId`). The value is assigned at assessment creation time and does not depend on Registration.
  - Generated assessments shall inherit the user identity that performed the triage operation. The triage operator becomes the assessment creator (`UserrId`).
  - `RegId` / `PasienId` remain nullable during `PENDING_REGISTRATION` and are populated during `AssignRegister`.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-007 fully — no identity alternative remains. It interacts with two prior items, both simplifications: (a) the D-01 link operation no longer needs to resolve or populate `LayananId` (only `RegId`/`PasienId` + names are linked; the `LayananName` paired with the configured id should be resolved once from the configured value, a planning detail); (b) the `AssesmentBuilder.Userr` behavior needs no cross-service user-directory mapping — the BILREG operator id is stamped directly, consistent with the builder's current caller-id stamping. Residual planning inputs (not new gaps): config-key ownership/validation and what happens when the configured value is missing or invalid at creation time; audit labelling of operator-attributed generated content stays under the existing security items.

### Decision D-07 — Assessment Generation Failure Handling (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - Triage persistence is the primary operation and must not be rolled back because of assessment generation failure.
  - Assessment generation shall be idempotent using (`IgdVisitId`, `NoTriage`) as the business key.
  - Failed generation attempts shall be recorded as Pending Generation items for later retry.
  - For MVP, retry may be manual. Automatic retry workers may be introduced later if operationally required.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-008 fully — the generate side's failure semantics (no-rollback, idempotency key, failure record, MVP retry mode) are all fixed. It interacts with three prior items, all consistently: (a) D-04's BR-07 (one event, one assessment) is now enforceable — the (`IgdVisitId`, `NoTriage`) business key doubles as the idempotency key, so a retried call resolves to the single snapshot; (b) the SMASS uniqueness enforcement on (`IgdVisitId`, `NoTriage`) already flagged in §12.3 item 1 becomes the idempotency mechanism on the SMASS side; (c) D-02's no-async constraint is preserved — "recorded for later retry" with manual MVP retry is a worklist, not an async synchronization mechanism; introducing auto workers later would be a deliberate, separately justified change. Scope boundary: D-07 decides the generate call only — link-call failure stays open under GAP-010. New required design inputs (not new gaps): the Pending Generation record store (mechanism/location — BILREG-side store is the natural home since BILREG owns the gateway, but this is a planning choice) and the manual-retry surface (operator-triggered re-invocation path).

### Decision D-08 — Registration Link Lifecycle Strategy (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - Do not extend `AggStateEnum`.
  - Introduce a separate `RegistrationLinkStatus` field: `PendingRegistration`, `Registered`.
- **Rationale (as given):**
  - Assessment lifecycle and registration-link lifecycle represent different concerns.
  - Avoids ripple effects across existing `AggState` consumers.
  - Preserves current assessment behavior.
  - Supports D-01 without changing core assessment state semantics.
- **Business Rule (as given):** A `PendingRegistration` assessment may be `Drafting` or `Finished`. Registration linkage shall not affect assessment completion status.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-009 fully — the enum-extension alternative is rejected, so the ripple risk it carried retires with it. It interacts with three prior items, all simplifications: (a) the D-01 link operation's flip targets the new field only — finish/reopen guards, `Load` checks, and collectors keep current behavior untouched; (b) the D-04 immutability constraint is unaffected — the sanctioned post-creation mutations remain key-population + link-status flip (completion status was never part of the link, which D-08 now states explicitly); (c) the GAP-009-era question "may a pending assessment be finished/signed?" is answered yes for completion (may be `Drafting` or `Finished`), with sign/date/`JenisRawat` specifics remaining under the link-operation and validator design (GAP-010). Residual planning inputs (not new gaps): the new column + DAL/mapper/query handling.

### Decision D-09 — Registration Linking Strategy (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - Registration linking is triggered synchronously after successful `AssignRegister` persistence.
  - SMASS shall provide `LinkAssessmentByIgdVisitId`, which links all pending assessments for the specified `IgdVisitId`.
  - The operation shall populate `RegId`, populate `PasienId`, populate `LayananId`, and set `RegistrationLinkStatus=Registered`.
  - Idempotency: repeated link requests are allowed.
  - Failure handling: registration persistence shall not be rolled back because of linking failure. Failed links shall be retried using the existing integration retry mechanism.
  - `ReplaceRegister`: latest registration data replaces previous linked values.
  - `VoidVisit`: does not delete or unlink assessments. Clinical records remain preserved.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-010 fully — trigger, operation, link-all scope, idempotency, no-rollback, retry vehicle, and both edges are all fixed. Three interactions require explicit recording:
  - (a) **Supersession of BR-09:** D-06 established the creation-time configured `LayananId`, and BR-09 derived from it that the link populates only `RegId`/`PasienId` ("`LayananId` is never linked"). D-09 explicitly lists populating `LayananId` at link time. As the later, explicit decision on the same point, D-09 supersedes BR-09 to the extent of conflict: the link populates all three keys from the registration data in the link call (consistent with D-01's original "populate `RegId`, `PasienId`, and `LayananId`" wording, which D-06 had narrowed). BR-09 is therefore marked SUPERSEDED below, not deleted, so the change history stays auditable. The creation-time configured value remains the pre-link value (D-06 stands for creation).
  - (b) **"Existing integration retry mechanism":** the only retry mechanism established in this track is D-07's (Pending Generation record + manual MVP retry). D-09's retry is therefore read as reusing that same mechanism for failed links — recorded here as an interpretation, flagged for planning confirmation rather than assumed silently.
  - (c) **Void-preservation vs. orphan policy:** voiding preserves assessments (possibly `PendingRegistration` forever for never-registered visits). Per D-15 this is valid long-lived state with no auto-delete/purge/archive — retention/visibility treatment is monitoring via the Pending Registration surface, with any future retention rule coming through operational governance. **[Changed by D-15]**

### Decision D-10 — Pending Assessment Visibility (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - `PENDING_REGISTRATION` assessments are visible only through IGD Visit workflows.
  - Visibility method: `ListByIgdVisitId`.
  - Not visible through: `ListByRegId`, `Catalog/{regId}`, OFTA Document integrations, RegId-based reporting.
  - After `AssignRegister`: assessment becomes `REGISTERED` and participates in all existing RegId-based SMASS queries.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-011 fully — the visibility rule, the query method, the exclusion list, and the post-link participation are all fixed. It interacts with three prior items, all confirmations: (a) BR-05 (pending quarantine) is confirmed and now stated operationally — no reg-keyed read changes, so OFTA/report/catalog behavior is preserved by construction; (b) the §12.3 item 5 query surface is decided (implement `ListByIgdVisitId` + index; leave all reg-keyed reads untouched); (c) the D-03 approval gate and D-09 void-preservation are unaffected — voided-visit assessments remain visible through the same IGD-visit surface, with retention governed operationally per D-15 (no auto-delete; monitoring surface; future retention via governance). Residual planning inputs (not new gaps): `ListByIgdVisitId` implementation + index.

### Decision D-11 — Migration and Rollout Strategy (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - No historical data migration is required. Existing assessments shall retain `IgdVisitId = NULL`.
  - Legacy assessment creation and the new IGD pending-registration path shall coexist during rollout.
  - Deployment order: (1) deploy database changes; (2) deploy SMASS application changes; (3) verify legacy assessment behavior; (4) enable IGD integration; (5) monitor production rollout.
  - Feature toggle `IgdVisit:EnableSmassIntegration` may be used to control activation.
- **Rationale (as given):**
  - Minimizes deployment risk.
  - Avoids unnecessary data migration.
  - Supports incremental rollout.
  - Preserves backward compatibility.
- **Assessment notes (analysis, not new decisions):** this resolves GAP-012 fully — migration scope (none), existing-row treatment (`NULL`), coexistence, deploy order, and activation control are all fixed. Two points recorded for planning precision: (a) the `NULL` treatment replaces the provisional "backfill with empty default" assumption stated in §4 earlier — no audit concern arises since no clinical data moves, only the new column defaults; (b) the toggle name (`IgdVisit:EnableSmassIntegration`) reads as the activation gate for the IGD-integration traffic (gateway calls + enable step 4); which service evaluates it and its default state are planning details, flagged here rather than assumed.

### Decision D-12 — Temporary Clinical Mapping Ownership (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - The ATS-to-SMASS mapping table is owned by SMASS master data.
  - Initial values are derived from existing SMASS seed data and are considered provisional mappings.
  - For demo, UAT, and initial deployment, the mapping table itself is the approved source of truth.
  - Clinical review may revise mappings later without requiring code changes.
- **Consequences (as given):**
  - No clinical sign-off is required for MVP.
  - No production gate is imposed.
  - Mapping changes are configuration changes.
  - Dedicated IGD Triage Paper remains stable.
- **Assessment notes (analysis, not new decisions):** this closes the D-03 production gate — the former blocking question 2 (approvers, evidence bar, per-concept sign-off as preconditions) is retired and replaced with a revision-after-deployment model: provisional values ship as the approved truth, clinical review revises them later as config, and the Paper never moves for mapping edits. It interacts with two prior items: (a) the D-03 "table ownership / storage mechanism" residual is now decided in favor of SMASS master data (storage mechanics remain a planning detail); (b) the §8 "provisional mappings reach production" risk is retired by design — reaching production provisional is now the approved behavior, not a failure mode; the residual risk shifts to revision governance (who may change mappings post-deployment and with what review), flagged as a planning input rather than a gate.

### Decision D-13 — BILREG to SMASS Service Authentication (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - The BILREG-owned gateway shall authenticate to SMASS using the existing JWT Bearer authentication mechanism.
  - No dedicated API key, shared secret, or custom authentication scheme shall be introduced.
  - SMASS shall validate the JWT using the same token validation configuration already used by existing internal APIs.
  - Scope: `GenerateAssessment`, `LinkAssessmentByIgdVisitId`.
- **Rationale (as given):**
  - Reuses existing security infrastructure.
  - Avoids introducing a second auth model.
  - Minimizes implementation effort.
  - Consistent with current MyHospital API authentication practices.
- **Assessment notes (analysis, not new decisions):** this closes Open Question No.10 with no new mechanism — verified feasible without new infrastructure since both APIs already wire JWT Bearer (`Smass.Api/Configurations/PresentationService.cs:22`, `Bilreg.Api/Configurations/PresentationService.cs:76`). One evidence-based residual, flagged for planning rather than assumed: SMASS's `AssesmentController` currently exposes its endpoints without `[Authorize]` (neighboring SMASS controllers carry it commented out), so planning must wire authorization enforcement onto the generate/link surfaces under the existing scheme. No token-issuance design is invented here — how the gateway obtains a valid bearer token (service identity, audience, lifetime, rotation) is a planning detail under the existing infrastructure.

### Decision D-14 — Triage Legal Record Ownership (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - The BILREG triage record is the legal record of triage.
  - Generated SMASS assessments are derived clinical documents created from the triage record.
  - BILREG remains the source of truth for triage history and audit.
- **UI Requirements (as given):**
  - BILREG shall display assessment-generation status and linked AssessmentId.
  - SMASS shall display source information: `IgdVisitId`, `NoTriage`, "Generated From IGD Triage".
  - Pending assessments shall be labelled "Pending Registration".
- **Rationale (as given):**
  - Preserves clear ownership boundaries.
  - Aligns with D-04 (1 Triage = 1 Assessment).
  - Avoids legal ambiguity if assessment generation fails.
  - Keeps BILREG as the authoritative triage system.
- **Assessment notes (analysis, not new decisions):** this closes Open Question No.11 with BILREG authoritative — which also settles the D-07 failure case legally: a missing generated assessment never creates ambiguity because the BILREG triage record stands as the record regardless. Two planning inputs arise (not new gaps): (a) BILREG must be able to show the linked AssessmentId per triage, so planning must choose between persisting the gateway-returned id against the triage row (new BILREG-side persistence — the second such addition after the D-07 Pending Generation store) and looking it up live from SMASS by (`IgdVisitId`, `NoTriage`); D-01's "no BILREG link table" finding covered correlation, not display — display needs a read path either way; (b) screen placement for each display element on both sides. No triage-form behavior change is implied — orchestration stays backend per D-02.

### Decision D-15 — Orphan Pending Assessment Policy (APPROVED) **[New]**

- **Status:** APPROVED.
- **Decision (as given):**
  - Assessments generated from IGD Triage shall not be automatically deleted, purged, or archived solely because registration never occurred.
  - `PendingRegistration` is a valid long-lived state.
  - Voiding an IGD Visit does not remove associated assessments.
  - Operational visibility shall be provided through a Pending Registration monitoring query/dashboard.
  - Future retention policies may be introduced through operational governance without changing assessment semantics.
- **Rationale (as given):**
  - Preserves clinical history.
  - Consistent with D-09 and D-14.
  - Avoids accidental loss of clinical data.
  - Keeps MVP implementation simple.
- **Assessment notes (analysis, not new decisions):** this closes Open Question No.12 — the last numbered open question in this assessment. It reaffirms (not changes) D-09's void-preservation and D-14's derived-document standing: preservation was already decided, D-15 adds the long-lived-validity standing plus the monitoring surface and the governance-owned future-retention path. One residual planning input (not a new gap): the monitoring query/dashboard's implementation + host/ownership — D-10 confines pending visibility to IGD Visit workflows, so the natural home is alongside the IGD-visit surfaces, but this is a planning choice, flagged here rather than assumed.

### Decision D-02 — Triage Assessment Generation Integration (APPROVED)

- **Status:** APPROVED (preserved; not contradicted by D-01).
- **Decision:** Use synchronous backend-to-backend integration (BILREG → SMASS) triggered immediately after successful triage submission.
- **Rationale (as decided):**
  - Keeps orchestration in backend.
  - Hides SMASS complexity from frontend.
  - Produces deterministic behavior.
  - Aligns with requirement that every triage generates an assessment.
  - Avoids operational complexity of async synchronization mechanisms.
- **Consequence (as decided):** BILREG introduces an outbound SMASS gateway and becomes responsible for invoking assessment generation after triage completion.
- **D-01 interaction:** the gateway's generate operation now carries `IgdVisitId` with optional keys (pending path); the gateway gains a second operation (link on `AssignRegister`). Both remain synchronous per constraints.

### Option — Dedicated triage custom Paper + server-side generation on triage submit (selected by D-02, enabled by D-01, mapped by D-03, versioned by D-04, papered by D-05, identified by D-06, hardened by D-07, linked by D-09)

- **Description:** a new dedicated IGD Triage Paper carrying the triage CustomSection (triage concepts) is created and becomes the canonical target; each triage event causes BILREG to synchronously invoke SMASS generation with `IgdVisitId` + `NoTriage` against that PaperId, stamped with the configured IGD `LayananId` and the triage operator's `UserrId`, creating a new SMASS document as an immutable snapshot in `PendingRegistration` through the existing create-then-add-section capability (the `SyncChartVitalSignCommand` precedent), populated from the triage payload via the D-03 mapping table — triage never rolled back on failure, failures recorded as Pending Generation items with idempotent manual retry for MVP; after successful `AssignRegister` persistence, BILREG synchronously invokes `LinkAssessmentByIgdVisitId`, which populates `RegId`/`PasienId`/`LayananId` and sets `Registered` on all pending assessments of the visit, idempotently and without touching completion status — registration never rolled back on link failure, failed links retried via the D-07 mechanism; `ReplaceRegister` re-links with the latest registration data; voiding a visit preserves all assessments; future triage changes version within the triage paper lifecycle without touching unrelated IGD papers.
- **Advantages:** directly answers "custom paper if possible" — yes, the Paper/CustomSection/AddSection machinery and a programmatic precedent already exist; single user action; no clinical table duplication in BILREG; deterministic per-triage behavior per D-02; visit-first IGD flow preserved per D-01 (no registration-first reordering, no placeholder-`RegId` hack — a real operational key instead); complete per-triage clinical history with a correlation key that mirrors BILREG's own triage PK per D-04.
- **Disadvantages:** introduces BILREG→SMASS runtime coupling over two operations (accepted by D-02/D-01); SMASS domain, persistence, and API all change (new fields, lifecycle, validation, linking operation, query — see §12); assessment volume scales with triage/re-triage count by design (accepted by D-04).
- **Risk:** low; concentrated in the `LinkAssessmentByIgdVisitId` payload/contract mechanics and the retry-surface wiring into the D-07 mechanism, the new `RegistrationLinkStatus` column handling, the Pending Generation store + manual-retry surface design, and mapping-revision governance (D-12 residual). All former design-decision risks are retired by D-01–D-12.

### Option — Frontend dual-dispatch (SMASS create, then BILREG triage update) (rejected by D-02)

- **Description:** the triage form writes the assessment to SMASS first, then submits triage to BILREG (the direction already recommended by the prior 30-item assessment).
- **Advantages:** no backend-to-backend coupling.
- **Disadvantages:** contradicts D-02 (orchestration must stay in backend; frontend must not carry SMASS complexity); inverts this request's stated direction; D-01's pending/link design assumes backend orchestration of both calls.
- **Risk:** rejected; retained here only as a record of the considered alternative.

### Recommendation **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

Proceed with the **D-01–D-15-selected option (new dedicated IGD Triage Paper as canonical target + synchronous server-side generate of one immutable assessment per triage event, identity-stamped per D-06, never rolling back triage, idempotent on (`IgdVisitId`, `NoTriage`) with Pending Generation record + manual MVP retry per D-07, registration-link lifecycle in a separate `RegistrationLinkStatus` field per D-08 + `LinkAssessmentByIgdVisitId` after `AssignRegister` persistence populating all three keys idempotently with no rollback and D-07-mechanism retry, replace-latest on `ReplaceRegister`, preserve-on-void per D-09, pending assessments visible only via `ListByIgdVisitId` with full RegId-based participation after link per D-10, no historical migration with `NULL` carryover and coexistence plus ordered rollout behind the `IgdVisit:EnableSmassIntegration` toggle per D-11 + SMASS-owned mapping table with approved provisional values, later revisions as config changes, per D-12 + existing JWT Bearer gateway auth for both operations per D-13 + BILREG triage record as legal record with derived SMASS documents, generation-status + linked-AssessmentId display on BILREG and source-info display + "Pending Registration" labelling on SMASS per D-14)**. All gates are cleared; the remaining items are planning-level residuals, not conditions: the triage Paper's creation/seed mechanics, the D-06 residual (config-key ownership/validation + missing-or-invalid behavior), the D-07 residuals (Pending Generation store mechanism/location + manual-retry surface), the D-09 residuals (link payload/contract mechanics + retry-surface wiring), the D-10 residual (`ListByIgdVisitId` implementation + index), the D-11 residual (toggle ownership/default state), the D-12 residual (mapping-revision governance: who may revise post-deployment and with what review), the D-13 residuals (authorization enforcement wiring + token acquisition), and the D-14 residuals (linked-AssessmentId correlation for BILREG display — persist vs. lookup — + screen placement on both sides), and the D-15 residual (Pending Registration monitoring implementation + host/ownership).

---

## 7. Recommended Approach **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

Prefer a solution in which a new dedicated IGD Triage Paper (D-05, canonical target, versioning triage changes within its own lifecycle) carrying the triage CustomSection is the sole SMASS home for generated triage content, written through the existing assessment create + custom-section capabilities following the `SyncChartVitalSign` precedent — each triage event synchronously generating one new immutable assessment carrying `IgdVisitId` + `NoTriage` (D-02 + D-04, `RegistrationLinkStatus = PendingRegistration`), stamped with the configured IGD `LayananId` and the triage operator's `UserrId` (D-06), translated through the SMASS-owned ATS-to-SMASS mapping table carrying approved provisional seed-derived values for demo/UAT/initial deployment, revisable later as configuration changes without touching the Paper (D-03 + D-12), never rolling back triage on generation failure but recording Pending Generation items with idempotent retry on (`IgdVisitId`, `NoTriage`), manual for MVP (D-07), with all pending assessments of the visit linked via `LinkAssessmentByIgdVisitId` after successful `AssignRegister` persistence (D-01 + D-04 + D-09, → `Registered`, populating all three keys idempotently, completion status untouched per D-08, registration never rolled back, failed links retried via the D-07 mechanism, replace-latest on `ReplaceRegister`, preserve-on-void) and visible only through IGD Visit workflows via `ListByIgdVisitId` until linked, with full RegId-based participation after link (D-10) — with BILREG triage remaining the legal record of triage and source of truth for triage history and audit, SMASS assessments standing as derived documents (D-14), BILREG displaying generation status + linked AssessmentId and SMASS displaying source info with "Pending Registration" labelling. This stays implementation-neutral: it names *what* must change (triage Paper creation, new `IgdVisitId` + `NoTriage` correlation fields, one separate link-status field per D-08, one link operation per D-09, one mapping table owned by SMASS master data per D-12 (approved provisional values; later revisions are config changes), one identity rule per D-06, one generate-failure rule per D-07 plus Pending Generation store + retry surface, one visibility rule per D-10 plus `ListByIgdVisitId` + index, one migration/rollout rule per D-11 (no historical migration, `NULL` carryover, coexistence, ordered deploy behind the toggle), one monitoring surface per D-15 (Pending Registration query/dashboard; host/ownership TBD)) without specifying handlers, endpoints, phases, or code.

---

## 8. Risks **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14]**

| Risk | Impact | Probability | Mitigation |
| ---- | ------ | ----------- | ---------- |
| ~~`RegId` unavailable at triage time blocks generation~~ — retired by D-01; replaced by the pending-governance risks below. | — | — | — |
| Orphan `PENDING` assessments for visits never registered (no `AssignRegister` ever fires) — decided by D-15 (valid long-lived state; no auto-delete/purge/archive; monitoring surface; future retention via governance) | — | — | Residual planning input: Pending Registration monitoring implementation + host/ownership. **[Changed by D-15]** |
| Pending assessments invisible to reg-keyed catalogs/reports/OftaDoc — decided by D-10 (visit-only visibility via `ListByIgdVisitId`; exclusion by construction; full participation after link) | — | — | Residual: `ListByIgdVisitId` implementation + index. **[Changed by D-10]** |
| Status-model ripple (if `AggStateEnum` is extended, all `AssesmentState` consumers are affected; if separate, illegal state combinations are possible) — retired by D-08 (separate field chosen; enum untouched, no consumer changes) | — | — | — **[Changed by D-08]** |
| Link-call failure after `AssignRegister` saved — decided by D-09 (registration never rolled back; failed links retried via the D-07 mechanism) | — | — | Residual: link payload/contract mechanics + retry-surface wiring. **[Changed by D-09]** |
| `ReplaceRegister`/void-visit edges — decided by D-09 (replace with latest; void preserves all records) | — | — | Residual: none on behavior; voided/never-registered visits feed the open orphan-visibility item (§9 question 12). **[Changed by D-09]** |
| Score→concept mis-mapping in shipped values | HIGH | LOW (provisional values are the approved truth per D-12; later clinical revisions are config changes) | D-12: table owned by SMASS master data; provisional values approved for demo/UAT/initial deployment; no sign-off gate; revisions need no code changes and leave the Paper stable. Residual planning input: mapping-revision governance (who may revise post-deployment, with what review). **[Changed by D-12]** |
| ~~Provisional mappings reach production without clinical approval~~ — retired by D-12 (reaching production provisional is the approved behavior, not a failure mode) | — | — | — **[Changed by D-12]** |
| Duplicate assessments on re-triage — retired as a versioning risk by D-04 and idempotency decided by D-07 (retries resolve to the single snapshot via the business key) | — | — | — **[Changed by D-07]** |
| Synchronous SMASS generate-call failure after triage saved — decided by D-07 (triage never rolled back; failure recorded as Pending Generation; idempotent manual retry for MVP) | — | — | Residual planning inputs: Pending Generation store mechanism/location + manual-retry surface; auto workers only later if operationally required. **[Changed by D-07]** |
| Identity/audit gaps — narrowed by D-06 (`LayananId` from config, `UserrId` from operator are decided). Residual: missing-or-invalid configured `LayananId` at creation time, and audit labelling of operator-attributed auto-linked content | LOW–MEDIUM | LOW | Fix config-key ownership/validation + missing-or-invalid behavior and the link-event audit rule during planning input. **[Changed by D-06]** |

---

## 9. Open Questions **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

Blocking (planning cannot begin without these):

1. ~~Key-availability strategy (D-01)~~ — **decided by D-01** (pending-via-`IgdVisitId` + auto-link). Remaining detail was tracked in GAP-009–GAP-012, now all decided (D-08–D-11). See §6/§12.
2. ~~Clinical approval of provisional mappings (former D-03 production gate)~~ — **cleared by D-12**: mapping table owned by SMASS master data; provisional values are the approved source of truth for demo/UAT/initial deployment; no sign-off for MVP, no production gate; later revisions are config changes with a stable Paper. Residual planning input: mapping-revision governance (who may revise post-deployment, with what review).
3. ~~Versioning rule~~ — **decided by D-04**: one new immutable assessment per triage event, correlated by `IgdVisitId` + `NoTriage`; all pending assessments link on `AssignRegister`. Residual idempotency requirement stays under GAP-008.
4. ~~Paper ownership~~ — **decided by D-05**: new dedicated IGD Triage Paper as canonical target; no triage concepts in any existing IGD Paper; triage changes version within the triage paper lifecycle. Residual planning input: the Paper's id, creation/seed mechanics, and lifecycle governance. (D-04 notes each assessment is a standalone snapshot — the Paper choice is unaffected in kind, but the chosen Paper serves every snapshot.)
5. ~~Sync failure semantics, generate call (former GAP-008)~~ — **decided by D-07**: triage never rolled back; idempotent generation on (`IgdVisitId`, `NoTriage`); failures recorded as Pending Generation items; manual retry for MVP. Residual planning inputs: Pending Generation store mechanism/location + manual-retry surface.
6. ~~Linking mechanism detail (former GAP-010)~~ — **decided by D-09**: `LinkAssessmentByIgdVisitId` after successful `AssignRegister` persistence; populates `RegId`/`PasienId`/`LayananId`, sets `Registered` on all pending assessments, idempotently; no rollback of registration; retry via the D-07 mechanism; replace-latest on `ReplaceRegister`; preserve-on-void. Residual planning inputs: link payload/contract mechanics + retry-surface wiring.
7. ~~Status-lifecycle modeling (former GAP-009)~~ — **decided by D-08**: separate `RegistrationLinkStatus` field; `AggStateEnum` untouched; pending may be `Drafting` or `Finished`; linkage never affects completion. Residual planning inputs: new column + DAL/mapper handling.
8. ~~Migration/rollout (former GAP-012)~~ — **decided by D-11**: no historical migration (`IgdVisitId = NULL` carryover); legacy and pending paths coexist; deploy ordered DB → SMASS app → verify legacy → enable IGD integration → monitor; activation via the `IgdVisit:EnableSmassIntegration` toggle. Residual planning input: toggle ownership/default state.

Non-blocking (needed during planning):

9. ~~Assessment identity values~~ — **decided by D-06**: configured IGD `LayananId` at creation, operator-inherited `UserrId`, `RegId`/`PasienId` nullable until link. Residual planning input: config-key ownership/validation, missing-or-invalid behavior, and link-event audit labelling.
10. ~~Service-to-service auth mechanism~~ — **decided by D-13**: existing JWT Bearer for both gateway operations, same SMASS token validation config, no new scheme. Residual planning details: authorization enforcement wiring on the generate/link surfaces; gateway token acquisition (identity, audience, lifetime, rotation) under the existing infrastructure.
11. ~~Legal record of triage~~ — **decided by D-14**: the BILREG triage record is the legal record; SMASS assessments are derived documents; BILREG is the history/audit source of truth. Display duties decided: BILREG shows generation status + linked AssessmentId; SMASS shows source info; pending labelled "Pending Registration". Residual planning inputs: linked-AssessmentId correlation for BILREG display (persist vs. lookup) + screen placement on both sides.
12. ~~Orphan-pending policy (former Question No.12)~~ — **decided by D-15**: no auto delete/purge/archive for never-registered assessments; `PendingRegistration` is valid long-lived; void preserves records; Pending Registration monitoring query/dashboard; future retention via governance without semantic change. Residual planning input: monitoring implementation + host/ownership.

Decided (no longer open):

- ~~Integration shape and trigger (former GAP-001)~~ — resolved by **D-02**: synchronous BILREG→SMASS after successful triage; every triage generates; BILREG owns the gateway. See §6.
- ~~Generate-call failure semantics (former GAP-008)~~ — resolved by **D-07**: triage primary, never rolled back; idempotent generation on (`IgdVisitId`, `NoTriage`); failures recorded as Pending Generation items; manual retry for MVP. Residual planning inputs: Pending Generation store mechanism/location + manual-retry surface. See §6.
- ~~Key-availability strategy (former GAP-002)~~ — resolved by **D-01**: pre-registration creation via `IgdVisitId` + `PENDING_REGISTRATION` with optional keys; auto-link/populate on `AssignRegister`. See §6/§12.
- ~~Status-lifecycle modeling (former GAP-009)~~ — resolved by **D-08**: separate `RegistrationLinkStatus` field; `AggStateEnum` untouched; pending may be `Drafting` or `Finished`; linkage never affects completion. Residual planning inputs: new column + DAL/mapper handling. See §6.
- ~~Clinical approval gate (former D-03 gate)~~ — cleared by **D-12**: SMASS-owned mapping table; provisional values approved for demo/UAT/initial deployment; no sign-off, no gate; later revisions are config changes. Residual planning input: mapping-revision governance. See §6.
- ~~Pending visibility (former GAP-011)~~ — resolved by **D-10**: visit-only visibility via `ListByIgdVisitId`; excluded from all RegId-based surfaces; full participation after link. Residual planning inputs: `ListByIgdVisitId` implementation + index. See §6.
- ~~Migration/rollout (former GAP-012)~~ — resolved by **D-11**: no historical migration (`NULL` carryover); coexistence; ordered deploy DB → app → verify → enable → monitor; `IgdVisit:EnableSmassIntegration` toggle. Residual planning input: toggle ownership/default state. See §6.
- ~~Service authentication (former Question No.10)~~ — resolved by **D-13**: existing JWT Bearer, same validation config, both operations in scope, no new scheme. Residual planning details: enforcement wiring + token acquisition. See §6.
- ~~Legal record ownership (former Question No.11)~~ — resolved by **D-14**: BILREG triage record is the legal record; SMASS documents are derived; BILREG is history/audit source of truth; display duties on both sides decided. Residual planning inputs: linked-AssessmentId correlation (persist vs. lookup) + screen placement. See §6.
- ~~Orphan policy (former Question No.12)~~ — resolved by **D-15**: preserve (no auto delete/purge/archive); long-lived pending is valid; monitoring query/dashboard; future retention via governance. Residual planning input: monitoring implementation + host/ownership. See §6.
- ~~Linking strategy (former GAP-010)~~ — resolved by **D-09**: `LinkAssessmentByIgdVisitId` after `AssignRegister` persistence; populate all three keys + `Registered` on all pending assessments, idempotently; no rollback; retry via D-07 mechanism; replace-latest; preserve-on-void. Residual planning inputs: payload/contract mechanics + retry-surface wiring. See §6.
- ~~Mapping strategy and provisional values (former GAP-003/GAP-004)~~ — resolved temporarily by **D-03**: configurable ATS-to-SMASS mapping table; seed-derived provisional values for demo/dev. Production approval remains open (see blocking question 2). See §6.
- ~~Versioning strategy (former GAP-005)~~ — resolved by **D-04**: one new immutable assessment per triage event, correlated by `IgdVisitId` + `NoTriage`; link-all on `AssignRegister`. See §6.
- ~~Paper ownership (former GAP-006)~~ — resolved by **D-05**: new dedicated IGD Triage Paper as canonical target; no triage concepts in any existing IGD Paper; triage changes version within the triage paper lifecycle. Residual planning input: Paper id, creation/seed mechanics, lifecycle governance. See §6.
- ~~Identity mapping (former GAP-007)~~ — resolved by **D-06**: configured IGD `LayananId` at creation, operator-inherited `UserrId`, `RegId`/`PasienId` nullable until link. Residual planning input: config-key ownership/validation, missing-or-invalid behavior. See §6.

---

## 10. Implementation Impact Inventory **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

### Backend

- BILREG: `IgdVisitAssessTriageCmd` (+ `IgdVisitReAssessTriageCmd` — every triage generates exactly one new assessment per D-02/D-04; gateway payload carries `IgdVisitId` + `NoTriage`, the triage operator id as `UserrId`, and the configured IGD `LayananId` per D-06, with optional registration keys per D-01), `IgdVisitAssignRegisterCmd` handler (synchronous `LinkAssessmentByIgdVisitId` invocation post-save per D-01/D-09, linking all pending assessments of the visit per D-04 with `RegId`/`PasienId`/`LayananId` from the assigned registration per D-09 — superseding the earlier D-06-derived "no link-time `LayananId`" note), `IgdVisitModel`, `IgdVisitTriageDal`/repo, outbound SMASS gateway client with two operations — generate (idempotent on (`IgdVisitId`, `NoTriage`) per D-04/D-07; triage save never rolled back on generate failure per D-07) + link — owned by BILREG (decided, D-02/D-01), plus ownership/validation of the `IgdVisit:SmassLayananId`-style config value (D-06 residual), plus the Pending Generation record store for failed attempts with a manual-retry surface for MVP (D-07; mechanism/location is a planning choice — first BILREG-side persistence this track requires, revising the earlier "no BILREG schema change" note, which now holds only for clinical data), plus BILREG display of per-triage generation status + linked AssessmentId (D-14; linked-AssessmentId correlation mechanism — persist returned id vs. live lookup by business key — is a planning choice).
- SMASS: `AssesmentModel` (+ `IgdVisitId` + `NoTriage` snapshot key per D-04, + `RegistrationLinkStatus` per D-08), assessment creation path (pending variant with relaxed key validation, always against the dedicated triage `PaperId` per D-05; immutability guard on clinical content post-creation per D-04, with the D-01 key-population + link-status flip as the sanctioned mutation, completion status untouched per D-08), new registration-linking operation `LinkAssessmentByIgdVisitId` (link-all match on `IgdVisitId`, populating `RegId`/`PasienId`/`LayananId` + names and flipping only the link-status field per D-04/D-08/D-09, idempotent, replace-latest on re-link), list-by-`IgdVisitId` query (decided by D-10 as the sole pending-visibility surface, backed by the new index; all reg-keyed reads unchanged by design), `AssesmentBuilder` (deferred `Reg`/`Layanan` resolution on pending path), `AssesmentValidator` (pending-aware rules — completion guards unchanged per D-08), `AssesmentWriter`/DALs (new columns), section/concept builders, and the new dedicated IGD Triage Paper itself (Paper + CustomSection + CustomSectionConcept master data + seed mechanics + lifecycle governance per D-05 — no changes to any existing IGD Paper), plus the ATS-to-SMASS mapping table owned by SMASS master data with approved provisional seed-derived values (D-03 + D-12; later clinical revisions are configuration changes needing no code changes and leaving the triage Paper stable; residual: revision governance).

### Database

- BILREG: no clinical-data schema change (D-01 correlation lives on the SMASS side via `IgdVisitId`; D-04 extends it with `NoTriage`, mirroring BILREG's own `BILRG_IgdVisitTriage` PK). New operational store required by D-07: Pending Generation records for failed attempts (+ manual-retry surface); mechanism/location is a planning choice.
- SMASS: `SMASS_Assesment` gains `IgdVisitId` (nullable, indexed) + a separate `RegistrationLinkStatus` column (`PendingRegistration`/`Registered`) per D-08 — `AggStateEnum` is not extended and no existing state consumer changes; key columns accept empty on the pending path (application-validated, not DDL-relaxed beyond what defaults already allow); backfill of `IgdVisitId` for existing rows (GAP-012); master-data rows (Paper, CustomSection, CustomSectionConcept, preferences); transactional rows in existing assessment tables.

### Frontend **[Changed by D-14]**

- Per D-02/D-14: no triage-form orchestration change. Decided display-only changes: BILREG screens show per-triage generation status + linked AssessmentId (correlation mechanism is a planning choice — persist returned id vs. live lookup by business key, see §4); SMASS screens show source info (`IgdVisitId`, `NoTriage`, "Generated From IGD Triage") and label pending assessments "Pending Registration". Screen placement on both sides is a planning input. Per D-15, a Pending Registration monitoring query/dashboard provides operational visibility over long-lived pending (incl. voided-visit) assessments — implementation + host/ownership is a planning input, with the natural home alongside the IGD-visit surfaces per the D-10 visit-only rule.

### Integration **[Changed by D-09, D-10, D-11, D-13]**

- Decided (D-02/D-01/D-04/D-09/D-10/D-11/D-13): two synchronous BILREG→SMASS operations — (1) generate after each successful triage submission, creating one new immutable assessment keyed by (`IgdVisitId`, `NoTriage`) in pending state, (2) link via `LinkAssessmentByIgdVisitId` after successful `AssignRegister` (populate keys, flip lifecycle on all pending assessments). Both operations authenticate with the existing JWT Bearer mechanism (D-13) — no new auth scheme; SMASS validates under its existing token configuration. Timeout/retry/idempotency per D-07 (generate) and D-09 (link). SMASS→Billing Reg lookup is bypassed (not removed) on the pending path and applies normally at/after linking. Rollout ordered per D-11 behind the `IgdVisit:EnableSmassIntegration` toggle.

### Security **[Changed by D-06, D-13]**

- Cross-service auth decided (D-13): existing JWT Bearer over both gateway operations; residual planning details are enforcement wiring on the generate/link surfaces and gateway token acquisition. Identity decided (D-06): BILREG-assessor→SMASS-userr direct stamping plus configured `LayananId`. Audit labelling of system-generated assessment content and of the auto-link mutation (link event must record actor, source, timestamp, and key values applied).

---

## 11. Planning Readiness **[Changed by D-01, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

### Status

```text
READY
```

### Status

```text
READY
```

### Blocking Issues

```text
None.
```

All twelve gaps GAP-001–GAP-012 are resolved, the former D-03 production gate is cleared by D-12, Open Question No.10 (service auth) is closed by D-13, and Open Question No.11 (legal record) is closed by D-14. What remains are planning-level residuals to be settled during planning (not discovery blockers): triage Paper creation/seed mechanics + lifecycle governance; config-key ownership/validation + missing-or-invalid behavior; Pending Generation store + manual-retry surface; link payload/contract mechanics + retry-surface wiring; `ListByIgdVisitId` implementation + index; toggle ownership/default state; mapping-revision governance; authorization enforcement wiring + gateway token acquisition (D-13 residuals); linked-AssessmentId correlation + screen placement (D-14 residuals); Pending Registration monitoring implementation + host/ownership (D-15 residual). No open questions remain — every numbered question (No.10–No.12) and every gap (GAP-001–GAP-012) is decided.

### Planner Guidance

- **Scope:** two synchronous operations (D-02 generate-one-per-triage against the dedicated triage Paper + D-01/D-04/D-09 link-all via `LinkAssessmentByIgdVisitId`) from each triage event to the SMASS triage-paper home keyed by (`IgdVisitId`, `NoTriage`); pending assessments surface only via `ListByIgdVisitId` (D-10), joining all RegId-based queries after link; BILREG keeps visit-flow state, SMASS keeps the clinical documents as immutable snapshots (pending until linked). Do not expand into the 30-item form or full IGD assessment redesign — those belong to the prior assessment's track. Do not redesign unrelated SMASS validation, catalog, or report behavior beyond what the pending path strictly requires, and do not touch any existing IGD Paper (D-05).
- **Dependencies:** none block planning. Planning-level residuals to settle during planning: SMASS-owned mapping table mechanics + revision governance (D-03/D-12); triage Paper creation/seed mechanics + lifecycle governance; link payload/contract mechanics + retry-surface wiring (D-09 residuals); Pending Generation store + manual-retry surface; `ListByIgdVisitId` implementation + index; toggle ownership/default state; authorization enforcement wiring + gateway token acquisition (D-13 residuals); linked-AssessmentId correlation + screen placement (D-14 residuals); Pending Registration monitoring implementation + host/ownership (D-15 residual).
- **Sequencing concern:** D-11 fixes rollout order (DB → SMASS app → verify legacy → enable IGD integration via toggle → monitor); the enable step gates all gateway traffic, so no integration sequencing inside planning is needed beyond respecting the toggle.
- **Review concern:** verify any proposal against the skill constraints — no silently invented mappings (D-03/D-12 provisional values are a given decision input and the approved source of truth; later revisions are config changes under revision governance), no invented auth schemes (D-13 mandates the existing JWT Bearer — planning wires enforcement and token acquisition under it, nothing new), no invented legal semantics (D-14 fixes BILREG as the legal record with derived SMASS documents — planning implements the display duties, nothing more), no invented retention semantics (D-15 fixes preserve + monitor + governance-owned future retention — planning implements the monitoring surface, nothing more), no new business rules smuggled in as technical defaults (D-01–D-15 are recorded as given decisions, not invented here; §12 business rules derive strictly from them — with BR-09 explicitly marked superseded by D-09), and the assessment/plan/implementation boundary preserved.

---

## 12. D-01 Update Pack — Assessment Key Availability Strategy **[New: all subsections introduced by D-01]**

### 12.1 GAP-002 Re-evaluation

GAP-002 asked how an assessment could be created when triage validly precedes `AssignRegister` while `CreateFixAssesmentCommand` mandates non-empty `RegId`/`PasienId`/`LayananId`. D-01 answers it by changing the premise rather than working around it: IGD-originated assessments carry `IgdVisitId` as a first-class operational reference, the three administrative keys become optional while the assessment is `PENDING_REGISTRATION`, and `AssignRegister` auto-links them. GAP-002 is therefore **closed at decision level**. What remains is bounded implementation design, re-tracked as GAP-009 (lifecycle modeling), GAP-010 (linking mechanism + edges), GAP-011 (pending visibility), and GAP-012 (migration/rollout). No async mechanism is needed or introduced: both new behaviors (create-pending, link-on-register) execute synchronously in backend flows per D-02 and the constraints.

### 12.2 Updated Target Architecture & Integration Flow **[New by D-01]**

Pre-registration generation (D-01 + D-02 combined):

```text
Triage submit (BILREG)
  → save BILRG_IgdVisitTriage + visit state (existing)
  → [NEW, sync] SMASS generate(IgdVisitId, NoTriage, assessor, timestamps, triage concepts, PaperId)
  → SMASS creates new AssesmentModel { IgdVisitId, NoTriage, RegistrationLinkStatus = PendingRegistration, keys empty } + triage section (immutable snapshot per D-04)
```

Registration linking (D-01 + D-04 + D-08 + D-09):

```text
AssignRegister (BILREG)
  → load visit + reg, visit.AssignRegister, save (existing)
  → [NEW, sync] SMASS LinkAssessmentByIgdVisitId(IgdVisitId, RegId, PasienId, LayananId)
  → SMASS populates all three keys on ALL matching pending assessments, sets RegistrationLinkStatus = Registered (completion untouched)
  → on link failure: registration stands (no rollback); failed link retried via the D-07 mechanism
```

Edges (D-09): `ReplaceRegister` re-invokes the link with the latest registration data (latest replaces previous linked values); voiding a visit neither deletes nor unlinks assessments.

Re-triage follows the generate flow per D-02/D-04 (every triage, including every re-triage, generates exactly one new assessment). A failed generate call records a Pending Generation item keyed by (`IgdVisitId`, `NoTriage`) without rolling back triage (D-07); MVP retry is manual re-invocation resolving idempotently to the single snapshot.

### 12.3 Required SMASS Design Changes **[New by D-01]**

1. **New `IgdVisitId` + `NoTriage` fields:** on `AssesmentModel` (both verified absent today), through DAL insert/update/select (`AssesmentDal`), model mappers, and any assessment read contracts that must expose them. Non-IGD assessments leave them empty; both are mandatory on the IGD-originated pending path, jointly forming the D-04 snapshot correlation key that mirrors BILREG's `BILRG_IgdVisitTriage` PK. Uniqueness enforcement on (`IgdVisitId`, `NoTriage`) underpins generate idempotency (GAP-008).
2. **Status lifecycle (`PendingRegistration`, `Registered`) [Decided by D-08]:** neither value exists today (`AggStateEnum`: `Created, Drafting, Finished, Deleted`). Per D-08 the enum stays untouched and the lifecycle lives in a new separate `RegistrationLinkStatus` field (column + DAL/mapper/query handling, defaulted for existing rows). Consequences: no existing `AssesmentState` consumer changes; completion guards keep current behavior; pending assessments may be `Drafting` or `Finished` and linkage never touches completion status. D-04's constraint stands alongside: clinical content is immutable post-creation, so key-population + link-status flip is the sole sanctioned post-creation mutation. Pending-state sign/date/`JenisRawat` specifics remain under the link-operation and validator design (GAP-010).
3. **Registration-linking operation `LinkAssessmentByIgdVisitId` [Decided by D-09]:** matches by `IgdVisitId`, populates `RegId`/`PasienId`/`PasienName`/`LayananId`/`LayananName` atomically (all-or-none) on all matching pending assessments (link-all per D-04 — the multi-match rule is decided, only mechanics remain), sets `RegistrationLinkStatus = Registered` (completion untouched per D-08), idempotently; invoked synchronously by BILREG after `AssignRegister` persistence. No-match (link with nothing pending) is a no-op success condition to define in the contract; link failure never rolls back registration and retries via the D-07 mechanism; re-link (e.g. `ReplaceRegister`) replaces values with the latest registration data.
4. **Validation rule changes:** the creation path admits a pending variant in which `RegId`/`PasienId`/`LayananId` guards are bypassed iff `IgdVisitId` is present (dedicated triage-create command vs. relaxed `CreateFixAssesmentCommand` is a design choice); `Reg`/`Layanan` external-service resolution is deferred to link time; pending-state transition rules follow the decided lifecycle (separate field per D-08) and visibility (visit-only per D-10).
5. **Query surface [Decided by D-10]:** implement list-by-`IgdVisitId` (backed by the new index) as the sole pending-visibility surface for generation-idempotency checks, linking, and IGD-side display; reg-keyed catalogs keep current behavior for linked assessments and exclude pending ones by construction (no changes to those reads).

### 12.4 New Business Rules (derived strictly from D-01, D-04, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, and D-15 — with BR-09 superseded, see below) **[Changed by D-04, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

- **BR-01 (pending creation):** an IGD-originated assessment may exist with empty `RegId`/`PasienId`/`LayananId` if and only if it carries a non-empty `IgdVisitId` and is in `PENDING_REGISTRATION`.
- **BR-02 (atomic linking):** the `PENDING_REGISTRATION → REGISTERED` transition populates all three keys together or not at all; partial key population is invalid.
- **BR-03 (link trigger):** linking is driven exclusively by `AssignRegister` (and any explicitly decided edge flows — none assumed); nothing else may flip the lifecycle.
- **BR-04 (correlation):** (`IgdVisitId`, `NoTriage`) on the assessment is the system of record for triage↔assessment correlation, mirroring BILREG's triage PK; no parallel link table is maintained. **[Changed by D-04: `NoTriage` added to the key]**
- **BR-05 (pending quarantine):** while `PENDING`, the assessment is excluded from reg-keyed catalogs, reports, and document workflows until linked, except where the pending-visibility rule (GAP-011) explicitly includes it.
- **BR-06 (snapshot immutability) [New by D-04]:** each assessment is an immutable snapshot of the triage event; clinical content (sections/concepts) is not modified after creation. The D-01 key-population + lifecycle flip is the sole sanctioned post-creation mutation.
- **BR-07 (one event, one assessment) [New by D-04]:** each triage event (first or re-triage) yields exactly one new assessment; retries of a failed generate call resolve to the single snapshot for that (`IgdVisitId`, `NoTriage`), never a second one.
- **BR-08 (creation identity) [New by D-06]:** the generated assessment's `LayananId` is the configured IGD value (e.g. `IgdVisit:SmassLayananId`), assigned at creation independent of Registration; its `UserrId` is the triage operator's identity.
- **BR-09 (link scope) [SUPERSEDED by D-09 — retained for audit trail]:** formerly stated the `AssignRegister` link populates `RegId`/`PasienId` (+ names) only and never touches `LayananId` (derived from D-06). D-09 explicitly requires populating `LayananId` at link time from the registration data in the link call; as the later explicit decision on the same point, D-09 supersedes BR-09. D-06 still stands for creation (configured value is the pre-link value).
- **BR-15 (link-all keys) [New by D-09]:** `LinkAssessmentByIgdVisitId` populates `RegId`/`PasienId`/`LayananId` (+ names) and sets `Registered` on all pending assessments of the visit, atomically per assessment and idempotently across repeats.
- **BR-16 (link failure) [New by D-09]:** registration persistence is never rolled back for link failure; failed links retry via the D-07 integration retry mechanism.
- **BR-17 (replace-latest) [New by D-09]:** `ReplaceRegister` re-links with the latest registration data; latest values replace previous linked values.
- **BR-18 (void preservation) [New by D-09, reaffirmed by D-15]:** voiding a visit neither deletes nor unlinks assessments; clinical records remain preserved as valid long-lived pending state with monitoring visibility (see BR-31–BR-33).
- **BR-19 (visit-only pending visibility) [New by D-10]:** a `PendingRegistration` assessment is visible only through IGD Visit workflows via `ListByIgdVisitId`; it is excluded from `ListByRegId`, `Catalog/{regId}`, OFTA integrations, and RegId-based reporting. This confirms BR-05 operationally.
- **BR-20 (post-link participation) [New by D-10]:** once `Registered`, the assessment participates in all existing RegId-based SMASS queries with no changes to those reads.
- **BR-21 (no historical migration) [New by D-11]:** existing assessments retain `IgdVisitId = NULL`; no clinical or key data is backfilled.
- **BR-22 (coexistence) [New by D-11]:** legacy assessment creation and the pending-registration path coexist during rollout; legacy behavior is verified before IGD integration is enabled.
- **BR-23 (ordered rollout behind toggle) [New by D-11]:** deployment follows DB → SMASS app → verify legacy → enable IGD integration → monitor; IGD-integration activation is controlled by the `IgdVisit:EnableSmassIntegration` toggle.
- **BR-24 (mapping ownership) [New by D-12]:** the ATS-to-SMASS mapping table is owned by SMASS master data.
- **BR-25 (approved provisional truth) [New by D-12]:** the table's provisional seed-derived values are the approved source of truth for demo, UAT, and initial deployment; no clinical sign-off is required and no production gate is imposed.
- **BR-26 (config-change revisions) [New by D-12]:** later clinical revisions to mappings are configuration changes requiring no code changes and leaving the dedicated triage Paper stable.
- **BR-27 (gateway authentication) [New by D-13]:** the BILREG-owned gateway authenticates to SMASS with the existing JWT Bearer mechanism for both operations; no API key, shared secret, or custom scheme exists on this integration.
- **BR-28 (legal record) [New by D-14]:** the BILREG triage record is the legal record of triage; BILREG remains the source of truth for triage history and audit, including when generation fails.
- **BR-29 (derived documents) [New by D-14]:** generated SMASS assessments are derived clinical documents created from the triage record — never the record itself.
- **BR-30 (display duties) [New by D-14]:** BILREG displays assessment-generation status + linked AssessmentId; SMASS displays source info (`IgdVisitId`, `NoTriage`, "Generated From IGD Triage"); pending assessments are labelled "Pending Registration".
- **BR-31 (no auto-deletion) [New by D-15]:** assessments generated from IGD Triage are never automatically deleted, purged, or archived solely because registration never occurred.
- **BR-32 (long-lived pending) [New by D-15]:** `PendingRegistration` is a valid long-lived state, including for voided visits' assessments (reaffirming BR-18).
- **BR-33 (monitoring + governed retention) [New by D-15]:** operational visibility over pending assessments is provided through a Pending Registration monitoring query/dashboard; future retention policies come through operational governance without changing assessment semantics.
- **BR-10 (triage primacy) [New by D-07]:** triage persistence is the primary operation; a generation failure never rolls back saved triage.
- **BR-11 (idempotent generation) [New by D-07]:** generation is idempotent on the (`IgdVisitId`, `NoTriage`) business key; a retry resolves to the single snapshot for that key.
- **BR-12 (failure record + MVP retry) [New by D-07]:** every failed generation attempt is recorded as a Pending Generation item for later retry; MVP retry is manual (automatic workers only later, if operationally required).
- **BR-13 (separate link lifecycle) [New by D-08]:** registration-link state lives in a separate `RegistrationLinkStatus` field (`PendingRegistration`/`Registered`); assessment completion state (`AggStateEnum`) is a different concern and is never modified by linkage.
- **BR-14 (completion independence) [New by D-08]:** a `PendingRegistration` assessment may be `Drafting` or `Finished`; registration linkage shall not affect assessment completion status.

### 12.5 Implementation Implications (solution level, no slices) **[New by D-01]**

- **SMASS:** domain (+field, lifecycle, pending-aware guards), application (pending create variant, link operation, deferred external resolution, new query), infrastructure (DAL column handling, index), API (pending-create surface + link surface + by-visit query), DB migration (column backfill, deploy-before-traffic ordering). Unrelated SMASS behavior (formulas, non-IGD papers, existing catalogs for linked assessments) is out of scope by constraint.
- **BILREG:** gateway client gains the link operation; `AssignRegister` flow gains the post-save synchronous link call; triage generate call switches to the `IgdVisitId`-keyed pending payload. No BILREG clinical-data schema change; no IGD flow reordering (visit-first preserved — that is the point of D-01). The D-07 Pending Generation store is the sole BILREG-side persistence addition (operational worklist, not clinical data).
- **Cross-cutting:** two-operation auth scope, audit records for generation and for the key-mutating link event, orphan-pending policy, and rollout gating (SMASS migration + link/generate surfaces live before BILREG gateway traffic).
- **[Changed by D-07]:** generate path gains the no-rollback guarantee (triage save is never undone), the (`IgdVisitId`, `NoTriage`) idempotency enforcement on both sides of the gateway call, the Pending Generation record store + manual-retry surface (first BILREG-side persistence this track — operational, not clinical), with automatic retry workers explicitly deferred unless operationally required.

### 12.6 Invalidated Pre-existing Assumptions **[New by D-01]**

- "SMASS assessment creation always requires `RegId`/`PasienId`/`LayananId`" — invalid on the IGD-originated path (guards become conditional).
- "`RegId` is the near-universal assessment key" — invalid while pending; `IgdVisitId` is the operational key until linking.
- "No SMASS schema change is needed for this track" (§4 earlier finding) — invalid; `IgdVisitId` + lifecycle storage are required.
- "`AssesmentBuilder.Reg` external resolution always runs at creation" — invalid on the pending path (deferred to link time).
- "Reg-keyed catalogs surface all assessments" — invalid for pending assessments (quarantined per BR-05 pending GAP-011).
- "The key-availability choice is between placeholder-`RegId` and registration-first" (prior doc's framing) — superseded by D-01's first-class pre-registration identity for this track.

### 12.7 Remaining Open Questions After D-01 **[Changed by D-04, D-05, D-06, D-07, D-08, D-09, D-10, D-11, D-12, D-13, D-14, D-15]**

No open questions remain. Every numbered question (No.10 service auth, No.11 legal record, No.12 orphan policy) and every gap (GAP-001–GAP-012) is decided (D-01–D-15). For the record, the full closure chain: the mapping blockers (GAP-003/GAP-004) by D-03/D-12 (SMASS-owned table, approved provisional truth, revisions as config changes); the versioning blocker (GAP-005) by D-04; the paper-ownership blocker (GAP-006) by D-05; the identity blocker (GAP-007) by D-06; the generation-failure blocker (GAP-008) by D-07 (Pending Generation store + manual-retry surface remain as planning inputs); the lifecycle-modeling blocker (GAP-009) by D-08; the linking blocker (GAP-010) by D-09 (payload/contract mechanics + retry-surface wiring remain as planning inputs); the visibility blocker (GAP-011) by D-10 (`ListByIgdVisitId` implementation + index remain as planning inputs); the migration/rollout blocker (GAP-012) by D-11 (toggle ownership/default state remains as planning input); the orphan-policy question (No.12) by D-15 (monitoring implementation + host/ownership remains as planning input). Note the recorded supersession: D-09's link-populates-`LayananId` supersedes BR-09 (see §12.4).
