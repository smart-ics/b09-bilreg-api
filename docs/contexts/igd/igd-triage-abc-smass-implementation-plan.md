---
Title: IGD Triage ABC to SMASS Implementation Plan
Code: IGD-TRIAGE-ABC-SMASS
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-09-19
Status: NOT-STARTED
Execution Approval: PENDING
---

# 1. Objective

Implement the approved ARCHITECTURE
`b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-architecture.md` (V2.0):
every IGD Visit Triage ABC submission (first triage and re-triage) synchronously
produces one immutable structured medical assessment in SMASS, correlated by
(`IgdVisitId`, `NoTriage`), created before administrative registration exists,
and linked to the registration when `AssignRegister` / `ReplaceRegister`
succeeds.

Referenced artifacts:

- FEATURE: `b09-bilreg-api/docs/contexts/igd/igd-01-context.md` (IGD Visit)
- DOMAIN: `b09-bilreg-api/docs/contexts/igd/igd-02-domain.md`
- ARCHITECTURE: `b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-architecture.md` (V2.0)
- FEASIBILITY-ASSESSMENT (READY-FOR-PLANNING):
  `b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-feasibility-assessment.md` (V2.0)
- IGD Design / API Contract: `b09-bilreg-api/docs/contexts/igd/igd-03-design.md`,
  `b09-bilreg-api/docs/contexts/igd/igd-04-api-contract.md`

The architecture closes every planning-level input (AR-01…AR-16) and realizes
approved decisions D-01…D-15. This plan does not re-decide any WHAT; it orders
the implementation of the approved target state.

---

# 2. Planning Scope

Target repositories (one repository per slice):

| Alias | Repository | Contents in scope |
|---|---|---|
| SMASS | `a043_smass_structuredmedicalassesment_api` | `SMASS_Assesment` alteration, `SMASS_TriageConceptMap`, IGD Triage Paper + mapping seeds, assessment provenance DAL, generate/link commands, by-visit and pending-registration reads, controller actions |
| BILREG | `b09-bilreg-api` | `BILRG_IgdVisitSmassTask` table, task domain/model/repo, SMASS gateway + token service + options, four handler hooks, retry/process commands, task queries, task controller |
| WEB | `c012_myhospital_web` | Emergency contracts/hooks/query keys, SCR-02 SMASS status, SCR-03 SMASS Integration Panel + retry |

In scope (architecture sections realized):

- SMASS §5.3, §5.4, §6.5, §8.1–§8.4, §9.1.
- BILREG §5.2, §6.2–§6.6, §8.1, §8.5–§8.7, §9.1, §9.6.
- WEB §5.5, §6.5.

Out of scope (architecture §3 Excluded):

- The SMASS-first 30-item form and reverse-direction flow.
- Rework of unrelated SMASS validation, catalogs, reports, formulas, or existing IGD Papers.
- Any SMASS clinical display application (`Smass.Winform`, `Smass.Api` are out of scope for screens); the required display fields are exposed as API view models only (§5.5, R-06).
- Automatic background retry workers, outbox, or async synchronization (D-07).
- Backfilling `IgdVisitId` into historical assessments (BR-21).
- New authentication scheme, authorization policy, role, or permission.

Planning-level decisions carried into the plan (from architecture §2.3, §4.2, §9.6):

- `IgdVisit:EnableSmassIntegration` default `false` (AR-01); with the toggle off no task row and no HTTP call are produced.
- `IgdVisit:SmassLayananId` and `IgdVisit:SmassTriagePaperId` validated at call time; invalid → task Failed without HTTP (AR-02), fail-closed (P-08).
- SMASS owns mapping master data; revision is a seed-script change only (AR-07).
- Mapping and deployment order: DB → SMASS application → verify legacy → enable toggle → monitor (§8.6).
- No `PayloadJson`; retry rebuilds from `BILRG_IgdVisitTriage` (AR-12).
- No `AuditLog` / `BILRG_IgdVisitEvent` entry for task transitions (AR-16).
- No screen is added in the SMASS repository; WEB covers BILREG display only (R-06).

---

# 3. Dependencies

External dependencies:

- SMASS and BILREG run against separate databases (respectively `HOSPITAL_PKL`
  and `HOSPITAL_HPL` in current configuration). There is no cross-database
  foreign key or link table (BR-04); integration is HTTP only.
- SMASS IGD Triage Paper and `SMASS_TriageConceptMap` must be seeded before IGD
  traffic is enabled (architecture §8.6, §11.3).
- SMASS new endpoints must be deployed before the BILREG toggle is switched on.
- BILREG requires `Smass:BaseApiUrl`, `Smass:TokenEmail`, `Smass:TokenPass`,
  `IgdVisit:SmassLayananId`, `IgdVisit:SmassTriagePaperId` in configuration
  (§9.6). Missing values degrade the integration to a Failed task, not a
  startup failure (no `ValidateOnStart`).
- Reused existing infrastructure: BILREG `IRestClientFactory` (RestSharp),
  options pattern, Nuna `TransHelper`, `IMemoryCache`, existing Scrutor DI scan;
  SMASS `AssesmentBuilder`/`AssesmentWriter`, Nuna `TransHelper`, Dawn `Guard`.

For slice dependencies:

- `Depends On` declares implementation prerequisites.
- Dependencies reference Slice IDs only.
- Dependency satisfaction requires the referenced slice to have implementation
  status IMPLEMENTED.
- Dependency satisfaction does not require review status GO.
- Dependencies must represent real implementation prerequisites.

Planning note (documentation, not a code prerequisite): the README
`b09-bilreg-api/AGENTS.md` and `docs/ARTIFACTS.md` conventions apply to any
documentation touched inside `b09-bilreg-api`.

---

# 4. Progress Summary

Plan status values are:

- NOT-STARTED
- IN-PROGRESS
- BLOCKED
- COMPLETED

Execution Approval values are:

- PENDING
- APPROVED

Execution Approval is owned by the Architect. It is PENDING during Planning and
set to APPROVED when the plan is released for execution. Execution must not begin
while Execution Approval is PENDING.

COMPLETED is a plan-level status only. Set it only when every slice has
implementation status IMPLEMENTED and review status GO.

Testing and test-package creation must not begin until the plan is COMPLETED.
An individual slice with review status GO is not a testing entry condition.

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 — SMASS Persistence and Master Data | NOT-STARTED | NOT-REVIEWED | 0/4 |
| P2 — SMASS Application | NOT-STARTED | NOT-REVIEWED | 0/4 |
| P3 — BILREG Persistence | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P4 — BILREG Integration | NOT-STARTED | NOT-REVIEWED | 0/5 |
| P5 — Web Client | NOT-STARTED | NOT-REVIEWED | 0/3 |

---

# 5. Phases

## P1 - SMASS Persistence and Master Data

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Repository for all P1 slices: `a043_smass_structuredmedicalassesment_api`.

### P1-S01

Title: SMASS schema — assessment provenance columns and TriageConceptMap table

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Extend `SMASS_Assesment` additively and create `SMASS_TriageConceptMap` (AR-03,
D-03, D-04, D-08) in the SMASS database, and register the scripts in the SSDT
project.

Depends On: None

Repository: `a043_smass_structuredmedicalassesment_api`

Completion Criteria:

- `Smass.Db/AssesmentContext/SMASS_Assesment.sql` adds, additively:
  `IgdVisitId VARCHAR(12) NOT NULL DEFAULT('')`,
  `NoTriage INT NOT NULL DEFAULT(0)`,
  `RegistrationLinkStatus INT NOT NULL DEFAULT(0)`.
- Filtered unique constraint
  `UX_SMASS_Assesment_IgdVisitTriage (IgdVisitId, NoTriage) WHERE IgdVisitId <> ''`
  exists (INV-A4, BR-07, BR-11).
- Index `IX_SMASS_Assesment_IgdVisitId (IgdVisitId) INCLUDE (AssesmentId, NoTriage,
  RegistrationLinkStatus)` exists.
- Existing PK `AssesmentId` and existing `IX_SMASS_Assesment_RegId` unchanged; no
  foreign key added.
- `Smass.Db/Tables/SMASS_TriageConceptMap.sql` created with
  `TriageFieldCode VARCHAR(30)`, `TriageValue VARCHAR(20)`, `ConceptId VARCHAR(6)`,
  `AssValue VARCHAR(255)`, `ValueSnomedCtId VARCHAR(18)`,
  `ValueTaxonomy VARCHAR(30)`, `QualifierValue VARCHAR(128)`, `NoUrut INT`,
  PK `PK_SMASS_TriageConceptMap (TriageFieldCode, TriageValue)`, index on
  `ConceptId` (INV-M1).
- Both scripts registered in `Smass.Db/Smass.Db.sqlproj` (`<Build>` ItemGroup for
  the table; the altered assessment script is already registered).
- SSDT project builds without error.

Notes:

- Additive only; `DEFAULT` values make every pre-existing row `Registered` with
  empty provenance (BR-21, BR-22). No backfill, no new soft-delete or audit
  columns.
- `SMASS_TriageConceptMap` is pure master data: no soft delete, no audit columns.
- `ConceptId` is validated at generation, not enforced as a physical FK (INV-M2).

---

### P1-S02

Title: SMASS master data seeds — IGD Triage Paper and ATS-to-SMASS mapping

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Seed the dedicated IGD Triage Paper (D-05) and the closed-set `TriageFieldCode` /
`TriageValue` mapping rows (D-03, D-12, AR-07) as idempotent SSDT seed scripts.

Depends On: P1-S01

Repository: `a043_smass_structuredmedicalassesment_api`

Completion Criteria:

- `Smass.Db/DataSeeds/IgdTriagePaperDataSeed.sql` creates one Paper
  (`SMASS_Paper`), its `SMASS_PaperSection` rows (`SectionType = CustomSection`
  only), the `SMASS_CustomSection` row(s), and the
  `SMASS_CustomSectionConcept` rows required by the mapping. `DELETE` then
  `INSERT`, idempotent by PaperId.
- The seeded Paper id (suggested `PP-001-IGDTR`) is recorded so it can be mirrored
  into BILREG `IgdVisit:SmassTriagePaperId`. No existing IGD Paper is modified.
- `Smass.Db/DataSeeds/TriageConceptMapDataSeed.sql` inserts one row per
  (`TriageFieldCode`, `TriageValue`) covering the closed domain: `AIRWAYS` (0–2),
  `BREATHING` (0–5), `CIRCULATION` (0–4), `GCS_EYE` (1–4), `GCS_MOTOR` (1–6),
  `GCS_VOICE` (1–5), `GCS_TOTAL` (3–15), `ATS_LEVEL` (`ATS1`…`ATS5`),
  `TRIAGE_COLOR` (`RED`, `YELLOW`, `GREEN`, `BLACK`), `MANUAL_OVERRIDE_BLACK`
  (`true`, `false`).
- Every mapping `ConceptId` exists in `SMASS_Concept`, and the same `ConceptId` is
  attached to the IGD Triage Paper's CustomSection via
  `SMASS_CustomSectionConcept` (so generation-time section grouping resolves).
- Both scripts registered in `Smass.Db.sqlproj` as `<None>` (manual execution,
  precedent `PaperDataSeed.sql`).
- Re-running both scripts is idempotent.

Notes:

- Mapping values are derived from existing `SMASS_Concept` /
  `SMASS_ConceptPreference` seed data; they are approved provisional truth (D-12).
- `GCS_TOTAL` is derived inside SMASS at generation time, so the table needs one
  row per possible total (GAP-004).
- Mapping revision after deployment = edit and re-run this seed script; no code
  change, no Paper change (AR-07, BR-26).
- This slice is a deployment prerequisite for end-to-end verification of P2-S05
  and for enabling the BILREG toggle (§8.6 step 2/4).

---

### P1-S03

Title: SMASS assessment provenance persistence — model, enum, key, DAL, quarantine

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Extend `AssesmentModel` with IGD provenance and `RegistrationLinkStatus`, add the
`IIgdVisitKey` and `RegistrationLinkStatusEnum` contracts, extend the assessment
DAL for the three new columns and the IGD read methods, and apply the
construction-level quarantine predicate (D-01, D-08, D-10, AR-11, §5.3, §8.2).

Depends On: P1-S01

Repository: `a043_smass_structuredmedicalassesment_api`

Completion Criteria:

- `AssesmentModel` gains `IgdVisitId` (`string`, default `''`), `NoTriage`
  (`int`, default `0`), `RegistrationLinkStatus`
  (`RegistrationLinkStatusEnum`), and implements `IIgdVisitKey`.
- `Smass.Domain/ExternalContext/IgdVisitAgg/IIgdVisitKey.cs` declares
  `string IgdVisitId { get; }`.
- `RegistrationLinkStatusEnum { Registered = 0, PendingRegistration = 1 }` exists
  at `Smass.Domain/AssesmentContext/AssesmentAgg/RegistrationLinkStatus.cs`.
- `AssesmentDal.SelectClause()`, `Insert`, and `Update` include the three new
  columns; default `RegistrationLinkStatus = Registered` for new non-IGD rows.
- `AssesmentDal.ListData(IRegKey)` and `AssesmentDal.ListData(IPasienKey)` apply the
  predicate `RegistrationLinkStatus = 0 /* Registered */` (AR-11, BR-05/19/20).
- `IAssesmentDal` gains and `AssesmentDal` implements:
  `GetData(IIgdVisitKey key, int noTriage)` (idempotency pre-check),
  `ListData(IIgdVisitKey key)` (by-visit surface + link selection),
  `ListPendingRegistration()` (monitoring, oldest first).
- By-visit query is backed by `IX_SMASS_Assesment_IgdVisitId`.
- SMASS solution builds; existing consumers of `ListData(IRegKey)` /
  `ListData(IPasienKey)` see byte-identical results for pre-existing rows.

Notes:

- INV-A1/A2/A3 are realized by the generation and link commands (P2-S05, P2-S06);
  this slice provides the shape they enforce.
- `AggStateEnum` is not modified (D-08, AR-13).
- No audit columns are added; IGD provenance is carried by `IgdVisitId`,
  `NoTriage`, `UserrId` (§8.2).

---

### P1-S04

Title: SMASS TriageConceptMap domain model and DAL

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Add the SMASS-owned ATS-to-SMASS mapping master data model and read DAL used by the
generation command (D-03, D-12, §5.3).

Depends On: P1-S01

Repository: `a043_smass_structuredmedicalassesment_api`

Completion Criteria:

- `Smass.Domain/AssesmentContext/TriageConceptMapAgg/TriageConceptMapModel.cs` with
  members `TriageFieldCode`, `TriageValue`, `ConceptId`, `AssValue`,
  `ValueSnomedCtId`, `ValueTaxonomy`, `QualifierValue`, `NoUrut`, and
  `ITriageConceptMapKey`.
- `ITriageConceptMapDal` in `Smass.Application` declares read access
  (`ListData()` returning the whole small master table); implementation
  `TriageConceptMapDal` in `Smass.Infrastructure` reads
  `SMASS_TriageConceptMap`.
- DAL is discoverable by the existing DI registration (Nuna data-access markers /
  Scrutor scan); no manual registration required beyond the existing pattern.
- SMASS solution builds.

Notes:

- Table is small master data; loading the whole table per generate request is
  acceptable (§9.5).
- Interface declares insert/update/delete per §5.3 but this feature uses read only;
  do not introduce any delete/purge path.

---

## P2 - SMASS Application

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Repository for all P2 slices: `a043_smass_structuredmedicalassesment_api`.

### P2-S05

Title: GenerateIgdTriageAssesmentCommand

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Implement the pending-registration generation command that creates one immutable
assessment snapshot per triage event, resolving concepts through
`SMASS_TriageConceptMap` and bypassing registration/service dependencies
(D-01, D-02, D-03, D-04, D-06, AR-08, AR-10, AR-13, AR-14, §5.3, §6.2).

Depends On: P1-S03, P1-S04

Repository: `a043_smass_structuredmedicalassesment_api`

Completion Criteria:

- `Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/GenerateIgdTriageAssesmentCommand.cs`
  contains request, response, and handler; route `POST api/Assesment/generateIgdTriage`.
- Request accepts `IgdVisitId`, `NoTriage (>0)`, `PaperId`, `LayananId`, `UserrId`,
  `AssesmentDate` (`yyyy-MM-dd`), `AssesmentTime` (`HH:mm:ss`), the six scores,
  `AtsLevel`, `TriageColor`, `IsManualOverrideBlack`; `RegId`/`PasienId` are absent.
- Response is `{ AssesmentId, IgdVisitId, NoTriage, RegistrationLinkStatus, AssesmentState }`.
- Dawn `Guard` validates every field; `NoTriage > 0`; date/time formats.
- Idempotency: `_assesmentDal.GetData(request, request.NoTriage)`; when found,
  return the existing `AssesmentId` without mutation.
- Mapping resolution: builds the ten (`TriageFieldCode`, `TriageValue`) pairs,
  computing `GCS_TOTAL = GcsEye + GcsMotor + GcsVoice`; any unresolved pair throws
  `ArgumentException` (fail closed, no partial snapshot).
- Creation bypasses `IGetRegService` / `IGetLayananService` and the JenisRawat
  `AssesmentDate` window; `LayananId` comes from the request and `LayananName = ''`;
  `RegId = PasienId = PasienName = ''`; `IgdVisitId`, `NoTriage`,
  `RegistrationLinkStatus = PendingRegistration` are set.
- The builder supports pending creation without external lookups (a new builder
  path or equivalent), and the concept builder accepts explicit
  `assValue`/`ValueSnomedCtId`/`ValueTaxonomy`/`QualifierValue` from the mapping row.
- Paper existence is validated via `IPaperDal`; concepts are grouped into the
  Paper's CustomSection via `IPaperSectionDal` + `ICustomSectionConceptDal`; a
  mapped `ConceptId` absent from the Paper throws `ArgumentException`.
- Sections are added ordered by `NoUrut`; persistence uses `IAssesmentWriter.Save`;
  no `Finish()`; no `CreatedAssesmentEvent` / `AddedSectionAssesmentEvent` publish.
- SMASS solution builds.

Notes:

- Post-condition state is whatever `AddSection` produces (`Drafting`); the command
  never calls `Finish()` (R-02, BR-13/BR-14).
- `AssesmentValidator` (currently unused) must not be re-enabled; it rejects empty
  `RegId`/`PasienId`.
- `CreateFixAssesmentCommand` must not be modified.

---

### P2-S06

Title: LinkAssesmentByIgdVisitIdCommand

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Implement the registration-link command that populates administrative keys on all
assessments of a visit, all-or-nothing, idempotently, without touching completion
state (D-09, AR-10, AR-13, AR-14, AR-15, §5.3, §6.3).

Depends On: P1-S03

Repository: `a043_smass_structuredmedicalassesment_api`

Completion Criteria:

- `Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/LinkAssesmentByIgdVisitIdCommand.cs`
  contains request, response, and handler; route `PATCH api/Assesment/linkIgdVisit`.
- Request accepts `IgdVisitId`, `RegId`, `PasienId`, `PasienName`, `LayananId`,
  `LayananName`; all six are guarded non-empty.
- `_assesmentDal.ListData(request /* IIgdVisitKey */)` selects all assessments of the
  visit (pending and registered); empty list returns `LinkedCount = 0` without error.
- For each assessment, inside its own `TransHelper.NewScope()`, the command sets
  `RegId`, `PasienId`, `PasienName`, `LayananId`, `LayananName` and
  `RegistrationLinkStatus = Registered`, then calls `_assesmentDal.Update(model)`.
- `AssesmentState`, sections, and concepts are never modified; `Finish()` is not
  called; `IGetRegService` / `IGetLayananService` are not called.
- No `CreatedAssesmentEvent` / `AddedSectionAssesmentEvent` publish.
- Response is `{ IgdVisitId, LinkedCount, ListAssesmentId[] }`; partial failures
  throw after the loop so BILREG records a Failed link task (BR-16).
- SMASS solution builds.

Notes:

- One repeatable behaviour satisfies BR-15 (all pending become `Registered`) and
  BR-17 (replace-latest over previously linked rows); naturally idempotent (AR-15).
- No unlink/delete path exists (BR-18, BR-31/32).

---

### P2-S07

Title: SMASS read surfaces — by-visit, pending-registration monitoring, provenance view fields

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Add the by-visit read surface and the pending-registration monitoring query, and
expose IGD provenance/link view-model fields on the existing catalog and RegId
surfaces (D-10, D-14, D-15, §5.3, §6.5).

Depends On: P1-S03

Repository: `a043_smass_structuredmedicalassesment_api`

Completion Criteria:

- `ListAssesmentByIgdVisitIdQuery` (route `GET api/Assesment/igdVisit/{igdVisitId}`)
  returns every assessment correlated to the `IgdVisitId` (pending and registered),
  each carrying `RegistrationLinkStatus`; it is the only surface that returns a
  pending assessment.
- `ListPendingRegistrationAssesmentQuery` (route
  `GET api/Assesment/pendingRegistration`) returns `PendingRegistration`
  assessments, oldest first, including `IgdVisitId`, `NoTriage`, `AssesmentDate`,
  `CreateDate`, `PaperId`, `UserrId`.
- `AssesmentIgdView` fields are surfaced on `ListAssesmentByIgdVisitIdQuery`,
  `ListPendingRegistrationAssesmentQuery`, `ListCatalogQuery`, and
  `ListAssesmentByRegIdQuery`: `assesmentId`, `igdVisitId`, `noTriage`,
  `registrationLinkStatus`, `registrationLinkStatusLabel`
  (`"Pending Registration"` / `"Registered"`), `sourceLabel`
  (`"Generated From IGD Triage"` when `igdVisitId` is non-empty, else `""`), plus
  existing display fields.
- No delete/purge/archive action is introduced on any read surface.
- SMASS solution builds; existing `ListCatalogQuery` / `ListAssesmentByRegIdQuery`
  consumers keep their current fields.

Notes:

- Pending rows are excluded from every RegId/PasienId surface by the P1-S03
  quarantine predicate, so the catalog and RegId responses never expose them.
- `ListAssesmentByPasienIdQuery` and OFTA/report flows require no new code; they
  inherit the quarantine predicate.

---

### P2-S08

Title: SMASS API endpoints and authorization

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Expose the new SMASS operations on `AssesmentController` and enforce the existing
JWT Bearer authorization on the new actions only (D-13, AR-05, §5.3, §9.1).

Depends On: P2-S05, P2-S06, P2-S07

Repository: `a043_smass_structuredmedicalassesment_api`

Completion Criteria:

- `AssesmentController` exposes `POST api/Assesment/generateIgdTriage` (P2-S05),
  `PATCH api/Assesment/linkIgdVisit` (P2-S06),
  `GET api/Assesment/igdVisit/{igdVisitId}` and
  `GET api/Assesment/pendingRegistration` (P2-S07).
- `[Authorize]` is applied to each new action; no existing action changes its
  authorization and no policy/role/permission is added.
- Responses use the existing `JSendOk` convention.
- SMASS solution builds and the endpoints are reachable when the BILREG service
  identity presents a valid token.

Notes:

- Architecture §9.1 and AR-05 refer to "three new SMASS endpoints" while §5.3 and
  the §9.1 permission-boundary table enumerate four surfaces
  (`generateIgdTriage`, `linkIgdVisit`, `igdVisit/{id}`, `pendingRegistration`).
  This plan treats every new action as authorized; no approved decision or existing
  consumer behaviour is changed. Flagged for later architecture wording alignment;
  it does not block implementation.

---

## P3 - BILREG Persistence

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Repository for all P3 slices: `b09-bilreg-api`.

### P3-S09

Title: BILRG_IgdVisitSmassTask table

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Create the BILREG operational task store table that records one outbound SMASS
operation per IGD triage event or per visit-level link operation (D-07, AR-03,
INV-T1, §8.1).

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:

- `Bilreg.SqlDb/IgdContext/IgdVisitSmassTaskFeature/BILRG_IgdVisitSmassTask.sql`
  creates the table with columns `IgdVisitSmassTaskId VARCHAR(14)` (PK),
  `IgdVisitId VARCHAR(12)`, `NoTriage INT`, `TaskType INT`, `TaskStatus INT`,
  `AssessmentId VARCHAR(13) DEFAULT('')`, `RetryCount INT DEFAULT(0)`,
  `LastRetryDate DATETIME DEFAULT('3000-01-01')`,
  `ProcessedDate DATETIME DEFAULT('3000-01-01')`,
  `LastError VARCHAR(500) DEFAULT('')`, `CrtDate DATETIME`.
- Unique constraint `UX_BILRG_IgdVisitSmassTask_BusinessKey (IgdVisitId, NoTriage, TaskType)`
  exists (INV-T1, BR-11).
- Indexes `IX_BILRG_IgdVisitSmassTask_Visit (IgdVisitId) INCLUDE (NoTriage,
  TaskType, TaskStatus, AssessmentId)` and
  `IX_BILRG_IgdVisitSmassTask_Status (TaskStatus, CrtDate)` exist.
- No soft-delete column; no `AuditLog` and no `BILRG_IgdVisitEvent` change (AR-16,
  INV-T8).
- No backfill and no change to `BILRG_IgdVisitTriage` or `BILRG_IgdVisit`.

Notes:

- Retention is governance-owned and never automatic (D-15, BR-31).
- Precedent naming/DDL: `BILRG_EmrAntrianOutboundQueue`-style operational tables.

---

### P3-S10

Title: IgdVisitSmassTask domain model, enums, repository, and DAL

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Implement the task aggregate, its value objects, and its persistence so generation,
link, retry, and worklist operations have a single operational record (D-07, §5.2).

Depends On: P3-S09

Repository: `b09-bilreg-api`

Completion Criteria:

- `Bilreg.Domain/IgdContext/IgdVisitSmassTaskFeature/IgdVisitSmassTaskModel.cs`
  exposes `IgdVisitSmassTaskId` (`NunaId.New("IST")`), `IgdVisitId`, `NoTriage`,
  `TaskType`, `TaskStatus`, `AssessmentId`, `RetryCount`, `LastRetryDate`,
  `ProcessedDate`, `LastError`, `CrtDate`.
- `SmassTaskTypeEnum { Generate = 0, Link = 1 }` and
  `SmassTaskStatusEnum { Pending = 0, Succeeded = 1, Failed = 2 }` exist with
  `ToCode()` returning `"GENERATE"`/`"LINK"` and `"PENDING"`/`"SUCCEEDED"`/`"FAILED"`;
  `IIgdVisitSmassTaskKey` exists.
- Behaviours `CreatePending`, `MarkSucceeded(assessmentId, processedAt)`,
  `MarkFailed(error, failedAt)`, `Rehydrate(...)` are implemented; `LastError` is
  truncated to 500 chars.
- Invariants INV-T1…INV-T8 are enforced (business-key uniqueness, `Link ⇒ NoTriage = 0`,
  `Generate ⇒ NoTriage > 0`, transition legality, `AssertCanManualRetry()` requires
  `Failed`, `Generate + Succeeded ⇒ AssessmentId` non-empty, no delete transition).
- `IIgdVisitSmassTaskRepo` (Application) declares `LoadEntity`, `FindByBusinessKey`,
  `ListByVisit`, `ListProcessable` (Failed, ordered by `CrtDate` ascending),
  `SaveChanges`.
- `IgdVisitSmassTaskRepo`, `IgdVisitSmassTaskDal`, and `IgdVisitSmassTaskDto` exist
  in Infrastructure and are auto-registered by the existing Scrutor/Nuna scan
  (precedent `EmrAntrianOutboundQueueRepo`).
- BILREG solution builds.

Notes:

- Sentinel date is `3000-01-01`.
- No state transition deletes the row (D-15); no `PayloadJson` column is added
  (AR-12).

---

## P4 - BILREG Integration

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Repository for all P4 slices: `b09-bilreg-api`.

### P4-S11

Title: SMASS gateway, token service, and configuration options

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Implement the BILREG outbound SMASS gateway (port + adapter), JWT token acquisition,
and the configuration options that drive the integration (D-02, D-13, AR-01, AR-02,
AR-04, §6.2, §6.3, §9.1, §9.6).

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:

- `SmassOptions` with `BaseApiUrl`, `TokenEmail`, `TokenPass`, `TimeoutSeconds`
  (default `10`), `SECTION_NAME = "Smass"`; `IgdVisitOptions` with
  `EnableSmassIntegration` (default `false`), `SmassLayananId`,
  `SmassTriagePaperId`, `SECTION_NAME = "IgdVisit"`.
- `ISmassAssessmentGateway` (Application port) and `SmassAssessmentGateway`
  (Infrastructure adapter) implement `GenerateIgdTriage` (POST
  `{Smass:BaseApiUrl}/api/Assesment/generateIgdTriage`) and `LinkIgdVisit` (PATCH
  `{Smass:BaseApiUrl}/api/Assesment/linkIgdVisit`) using `IRestClientFactory`;
  payloads are camelCase JSON; responses parse via `JSend<T>` with
  `PropertyNamingPolicy = CamelCase` and `PropertyNameCaseInsensitive = true`.
- The gateway catches all exceptions and returns
  `SmassGatewayResult(bool Success, string? AssessmentId, string? ErrorMessage)`;
  no exception escapes to the caller (BR-10). Missing `BaseApiUrl`, `TokenEmail`,
  `TokenPass`, `SmassLayananId`, or `SmassTriagePaperId` produces a failed result
  without performing an HTTP call (AR-02, P-08).
- `ISmassTokenService` / `SmassTokenService` obtains a JWT from
  `POST {Smass:BaseApiUrl}/Token` with `{ email, pass }` and caches it in
  `IMemoryCache` under `SmassToken` with absolute expiry = token `exp` − 60 s; the
  token is attached as `Authorization: Bearer <jwt>` via RestSharp `AddHeader`.
- Both options are registered in `Bilreg.Api/Configurations/InfrastructureService.cs`
  with `.Configure<T>(...)`; `ValidateOnStart()` is **not** used for them. The
  gateway and token service receive explicit `AddScoped` lines (they are not
  Scrutor-resolved).
- No `IHttpClientFactory`, no Polly.
- BILREG solution builds.

Notes:

- First outbound Bearer in BILREG; precedent `UsmanGetTokenService` for token
  parsing and `IRestClientFactory` usage.
- Exactly one attempt per request; no in-request retry and no background worker
  (D-07, P-02).

---

### P4-S12

Title: Triage generation hooks and response extension

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Invoke the generate operation synchronously after triage persistence commits, in
both triage handlers, recording the outcome as a task row and surfacing SMASS
status in the response (D-02, D-04, D-07, BR-10, §5.2, §6.2, §6.6).

Depends On: P3-S10, P4-S11

Repository: `b09-bilreg-api`

Completion Criteria:

- `IgdVisitAssessTriageCmd` and `IgdVisitReAssessTriageCmd` follow the §6.6
  ordering: domain behaviour → `TransHelper.NewScope()` → `SaveChanges(visit)` →
  `trans.Complete()` → toggle check → task upsert → gateway call → mark result →
  task save → response.
- With `IgdVisit:EnableSmassIntegration = false` no task row is created and no HTTP
  call is made.
- The `Generate` task (`NoTriage > 0`) is upserted via
  `FindByBusinessKey(igdVisitId, noTriage, Generate)` / `CreatePending`, then
  `MarkSucceeded(assessmentId)` or `MarkFailed(error)`.
- The generate payload contains the six scores, `AtsLevel` (from
  `TriageLevelEnum.ToCode()`), `TriageColor` (including `Black` for manual
  override), `IsManualOverrideBlack`, `AssesmentDate`/`AssesmentTime` from the
  committed triage `AssessmentDateTime`, `UserrId` from `AssessorUserId`, plus
  `PaperId` from `IgdVisit:SmassTriagePaperId` and `LayananId` from
  `IgdVisit:SmassLayananId`.
- No exception from the gateway or task persistence propagates into the triage
  handler; triage remains committed.
- The HTTP call is outside every `TransHelper` scope; the task is saved in a
  separate write with no transaction spanning the HTTP call.
- `IgdVisitAssessTriageResponse` gains `SmassAssessmentId` (`string`, empty when not
  generated) and `SmassGenerationStatus`
  (`"Disabled" | "Pending" | "Generated" | "Failed"`), consumed by the controller.
- Existing triage scores, history, and visit persistence are unchanged.
- BILREG solution builds.

Notes:

- Unique-index violation on a concurrent duplicate is handled by re-reading the
  business key and treating an existing assessment as success (§6.2).
- No `IgdEventEnum`/`AuditLog` entry is added (AR-16).

---

### P4-S13

Title: Registration link hooks

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Invoke the link operation synchronously after registration persistence commits, in
both register handlers, recording the outcome as a `Link` task (D-09, BR-16, BR-17,
§5.2, §6.3, §6.6).

Depends On: P3-S10, P4-S11

Repository: `b09-bilreg-api`

Completion Criteria:

- `IgdVisitAssignRegisterCmd` and `IgdVisitReplaceRegisterCmd` follow the §6.6
  ordering after `trans.Complete()`.
- With the toggle disabled, no task row and no HTTP call are produced.
- A `Link` task (`NoTriage = 0`) is upserted via `FindByBusinessKey` /
  `CreatePending`, then `MarkSucceeded` / `MarkFailed`.
- The link payload is built from the already-loaded `RegModel`:
  `reg.RegId`, `reg.Pasien.PasienId`, `reg.Pasien.PasienName`,
  `reg.Layanan.LayananId`, `reg.Layanan.LayananName`, plus `IgdVisitId`. Values are
  read from `RegModel` because `IgdVisitModel.Reg` is only a
  `RegReff(RegId, PasienId, PasienName)`.
- No exception propagates; registration is never rolled back (BR-16).
- Handler responses are unchanged (`"Done"`).
- `IgdVisitVoidCmd` is not modified (BR-18, BR-32).
- BILREG solution builds.

Notes:

- Link task is idempotent and replace-latest by construction (AR-15).
- The HTTP call is outside every `TransHelper` scope.

---

### P4-S14

Title: Manual retry, batch process, and task queries

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Provide the manual retry surface and the monitoring/list queries over the
operational task store (D-07, BR-12, BR-30, BR-33, §5.2, §6.2, §6.3).

Depends On: P3-S10, P4-S11

Repository: `b09-bilreg-api`

Completion Criteria:

- `IgdVisitSmassTaskRetryCmd` loads the task via `IIgdVisitSmassTaskRepo.LoadEntity`,
  calls `AssertCanManualRetry()`, rebuilds the payload (Generate: re-read
  `BILRG_IgdVisitTriage` by (`IgdVisitId`, `NoTriage`) through the visit aggregate
  and the immutable triage row; Link: rebuild from the visit/`Reg`), invokes the
  gateway, then marks the task result. No `PayloadJson` is stored or replayed.
- `IgdVisitSmassTaskProcessCmd` is a batch variant that loops failed tasks with the
  same semantics.
- `IgdVisitListSmassTaskQuery` returns tasks for one visit as
  `IgdVisitSmassTaskView { IgdVisitSmassTaskId, IgdVisitId, NoTriage, TaskType,
  TaskStatus, AssessmentId, RetryCount, LastRetryDate, ProcessedDate, LastError,
  CrtDate }` (BR-30).
- `IgdVisitSmassWorklistQuery` returns failed tasks across visits, oldest first
  (BR-12, BR-33).
- No delete/purge/archive path is introduced (BR-31).
- BILREG solution builds.

Notes:

- Retry rebuild relies on the immutability of `BILRG_IgdVisitTriage` (DR-02) and
  the absence of a payload column (AR-12).
- Automatic workers remain explicitly out of scope (D-07).

---

### P4-S15

Title: IgdVisitSmassTaskController

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Expose the task endpoints consumed by the web client and operators, consistent with
existing IGD controllers (BR-12, BR-30, AR-06, §5.2, §6.4).

Depends On: P4-S14

Repository: `b09-bilreg-api`

Completion Criteria:

- `IgdVisitSmassTaskController` (BILREG API) exposes:
  `GET api/IgdVisitSmassTask/{igdVisitId}`,
  `GET api/IgdVisitSmassTask/worklist`,
  `PATCH api/IgdVisitSmassTask/retry`,
  `POST api/IgdVisitSmassTask/process`.
- The controller carries `[Authorize]`, matching all other IGD controllers; no new
  policy, role, or permission is introduced.
- Responses use the existing `JSendOk` convention.
- BILREG solution builds.

Notes:

- Monitoring is endpoint-only; no new operator screen is added (AR-06; precedent
  `BedIgd/pakaiBedIgd/orphan`).

---

## P5 - Web Client

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Repository for all P5 slices: `c012_myhospital_web`.

### P5-S16

Title: Emergency contracts, query keys, and SMASS task hooks

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Add the frontend contracts and TanStack Query wiring for reading SMASS task status
and triggering manual retry through BILREG only (D-02, §5.5, §6.5, §10.3).

Depends On: P4-S12, P4-S15

Repository: `c012_myhospital_web`

Completion Criteria:

- `src/modules/Emergency/types/contract.ts` adds `igdSmassTaskViewSchema`
  (`igdVisitSmassTaskId`, `igdVisitId`, `noTriage`, `taskType`
  `"GENERATE" | "LINK"`, `taskStatus` `"PENDING" | "SUCCEEDED" | "FAILED"`,
  `assessmentId`, `retryCount`, `lastRetryDate`, `processedDate`, `lastError`,
  `crtDate`) and extends `assignTriaseResponseSchema` with `smassAssessmentId` and
  `smassGenerationStatus` (`"DISABLED" | "PENDING" | "GENERATED" | "FAILED"`).
- `src/modules/Emergency/queries/EmergencyService.ts` adds
  `useIgdSmassTask(visitId)` (GET `IgdVisitSmassTask/{igdVisitId}`) and
  `useRetryIgdSmassTask` (PATCH `IgdVisitSmassTask/retry`), plus the corresponding
  `EMERGENCY_SERVICE_KEYS` entries.
- `src/core/api/queryConfigs.ts` adds `queryKeys.emergency.igdSmassTask` list/detail
  keys.
- Retry mutation invalidates the task query on settle; no optimistic update.
- The client never calls SMASS directly.
- `pnpm tc:app` (and type-check) passes.

Notes:

- Sentinel `3000-01-01` dates are converted to `null` using the existing
  `isSentinel()` helper; the architecture places the conversion in the module
  mapper/util path.
- No Pinia store is introduced (§10.3).

---

### P5-S17

Title: SCR-02 — Triage History SMASS status

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Show, per triage event, the SMASS generation status and linked `AssessmentId` in the
existing triage history panel (BR-30, §5.5 SCR-02, UI state architecture).

Depends On: P5-S16

Repository: `c012_myhospital_web`

Completion Criteria:

- `src/modules/Emergency/components/TriageHistoryPanel.vue` renders an inline SMASS
  status slot at the right of the timestamp for each row.
- The status is derived from a `computed` join of the already-cached
  `useTriageHistory` result and `useIgdSmassTask(visitId)` on
  (`igdVisitId`, `noTriage`, `GENERATE`); no new endpoint and no extra request.
- Rows show `Generated | Failed | Pending | Disabled`; when no `Generate` task
  exists, no SMASS slot is rendered (backward compatible with visits created before
  rollout).
- `AssessmentId` is displayed as text only (no navigation).
- `showReTriage` and `showTriageHistory` remain the source-of-truth toggles; the
  panel stays read-only.
- Type-check and lint pass.

Notes:

- No new table column is added; the narrow tablet layout is preserved
  (architecture SCR-02 UI state).

---

### P5-S18

Title: SCR-03 — SMASS Integration Panel and manual retry

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:

Add the SMASS Integration Panel to the IGD Triage workspace showing generation/link
tasks, status, error, and `AssessmentId`, with manual retry for failed tasks
(D-07, BR-12, BR-30, §5.5 SCR-03, interaction rules IR-01…IR-07).

Depends On: P5-S16

Repository: `c012_myhospital_web`

Completion Criteria:

- New `src/modules/Emergency/components/IgdSmassIntegrationPanel.vue`, presentational
  with its own `useIgdSmassTask` query (precedent `TriageHistoryPanel.vue`),
  receiving props from `TriaseIGD.vue`.
- Panel lists one row per task: `TaskType` label, `NoTriage` or `Visit`, status
  badge, `AssessmentId` (Generate only), and `LastError`.
- "Coba Ulang" is available only for `taskStatus = "FAILED"` (IR-01); it is hidden
  for `Succeeded` and disabled/hidden for `Pending`.
- While any row is retrying, all retry buttons are disabled (IR-02); on settle the
  task query cache is invalidated and a single toast reports the outcome.
- The panel auto-expands with a summary chip ("n gagal") when at least one task is
  `Failed`, otherwise it is collapsed by default (IR-03).
- The panel is mounted directly below SCR-02 in the desktop right column, and as a
  section inside the tablet/mobile triage drawer; it renders for every selected
  visit, including terminal/voided visits where retry remains available (IR-07).
- Nothing in the UI deletes, cancels, or archives a task or assessment (BR-31).
- No Pinia store is introduced; UI state is local to the view.
- Type-check and lint pass.

Notes:

- `taskType = "LINK"` rows do not display a single `AssessmentId` (IR-06).
- The web client calls only BILREG (`IgdVisitSmassTask/*`), never SMASS (D-02).

---

# 6. Change Log

- 2026-09-19 — v1.0 — Initial IMPLEMENTATION-PLAN created from ARCHITECTURE
  `igd-triage-abc-smass-architecture.md` (V2.0). 18 slices across 5 phases and 3
  repositories. `Execution Approval` remains `PENDING`; the plan is ready for
  Architect release.

Planning notes:

- Architecture wording inconsistency recorded in P2-S08: §9.1/AR-05 say "three new
  SMASS endpoints" while §5.3 and the §9.1 permission-boundary table enumerate
  four. The plan authorizes every new action; no approved decision is changed.
- Architecture §2.1 records that the FEATURE-owned `BR-01`…`BR-33` register is still
  pending relocation into the owning FEATURE artifact (feasibility FND-001). That
  is a FEATURE change owned by the Feature Knowledge Steward; it does not affect
  the implementation slices, which realize the architecture's enforcement mapping.
- Implementation delta discovered during planning and covered by slices: the
  existing `AssesmentBuilder` cannot create an assessment without
  `IGetRegService`/`IGetLayananService` lookups and only exposes a qualifier lookup
  via concept preferences, so P2-S05 must add a pending-creation builder path and
  an explicit-value concept path. This is required by AR-08/AR-10 and does not
  alter any decision.
- Deployment/rollout order (§8.6) is an operational sequence, not a slice
  dependency: DB → SMASS app → verify legacy → enable
  `IgdVisit:EnableSmassIntegration` → monitor. P1-S02 and P2-S08 must be deployed
  before the toggle is enabled.
