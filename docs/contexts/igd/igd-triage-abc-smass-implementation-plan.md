---
Title: IGD Triage ABC to SMASS Implementation Plan
Code: IGD-TRIAGE-ABC-SMASS
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-09-19
Status: COMPLETED
Execution Approval: APPROVED
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
| P1 — SMASS Persistence and Master Data | IN-PROGRESS | GO | 4/4 |
| P2 — SMASS Application | IN-PROGRESS | GO | 4/4 |
| P3 — BILREG Persistence | IN-PROGRESS | GO | 2/2 |
| P4 — BILREG Integration | IN-PROGRESS | GO | 5/5 |
| P5 — Web Client | IN-PROGRESS | GO | 3/3 |

---

# 5. Phases

## P1 - SMASS Persistence and Master Data

Implementation Status: IN-PROGRESS
Review Status: GO

Repository for all P1 slices: `a043_smass_structuredmedicalassesment_api`.

### P1-S01

Title: SMASS schema — assessment provenance columns and TriageConceptMap table

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- DAL shape chosen: `ITriageConceptMapDal : IListData<TriageConceptMapModel>` only.
  The full §5.3 marker set (`IInsert`/`IUpdate`/`IDelete`) is deliberately not
  declared because the codebase registers DALs by marker interface and a declared
  `IDelete<>` would expose a delete/purge path; the read-only
  `IListData<>`/`IGetData<,>` form mirrors existing mastery read DALs
  (`IKeadaanKeluarDkDal`). No insert/update/delete path exists.
- `ITriageConceptMapKey` declares `TriageFieldCode` and `TriageValue` (the INV-M1
  composite key) and is placed in its own file, matching the dominant `I*Key`
  convention (`ISmfKey`, `IKeadaanKeluarDkKey`).
- Changed files: `Smass.Domain/AssesmentContext/TriageConceptMapAgg/TriageConceptMapModel.cs`,
  `Smass.Domain/AssesmentContext/TriageConceptMapAgg/ITriageConceptMapKey.cs`,
  `Smass.Application/AssesmentContext/TriageConceptMapAgg/ITriageConceptMapDal.cs`,
  `Smass.Infrastructure/AssesmentContext/TriageConceptMapAgg/TriageConceptMapDal.cs`.
- Builds verified: `dotnet build` on `Smass.Infrastructure` and `Smass.Api`
  (0 errors); VS `MSBuild.exe` on `A043_Smass_StructuredMedicalAssesment_Api.sln`
  (Build succeeded, 0 errors, exit 0, including the `Smass.Db` SSDT dacpac).

---

## P2 - SMASS Application

Implementation Status: IN-PROGRESS
Review Status: GO

Repository for all P2 slices: `a043_smass_structuredmedicalassesment_api`.

### P2-S05

Title: GenerateIgdTriageAssesmentCommand

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Changed files: `Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/GenerateIgdTriageAssesmentCommand.cs`
  (new: request + response + handler), `Smass.Application/AssesmentContext/AssesmentAgg/Workers/Creators/AssesmentBuilder.cs`
  (interface + implementation: `LayananDirect`, `AssesmentDateDirect`, `PendingIgdTriage`),
  `Smass.Application/AssesmentContext/AssesmentAgg/Workers/Creators/AssesmentConceptBuilder.cs`
  (interface + implementation: explicit-value `SetAssValue(assValue, valueSnomedCtId, valueTaxonomy, qualifierValue)`).
- Route `POST api/Assesment/generateIgdTriage` is recorded as an XML-doc contract on
  the command type; controller wiring remains P2-S08.
- Mapping resolution: `ITriageConceptMapDal.ListData()` is loaded once per request
  and indexed by (`TriageFieldCode`, `TriageValue`), case-insensitive. The ten
  pairs are built in closed-set order (`AIRWAYS`, `BREATHING`, `CIRCULATION`,
  `GCS_EYE`, `GCS_MOTOR`, `GCS_VOICE`, `GCS_TOTAL` = eye + motor + voice,
  `ATS_LEVEL`, `TRIAGE_COLOR`, `MANUAL_OVERRIDE_BLACK`); scores use invariant
  numeric strings, `IsManualOverrideBlack` maps to lowercase `"true"`/`"false"`.
  Any unresolved or duplicated pair throws `ArgumentException` before any write
  (fail closed, no partial snapshot).
- Section grouping mirrors `SyncChartVitalSignCommand.BuildConceptSectionIndex`:
  `IPaperSectionDal.ListData(PaperKey)` → `SectionType = CustomSection` →
  `ICustomSectionConceptDal.ListData(sectionKeys)` → `conceptId → sectionId`.
  A mapped `ConceptId` absent from the Paper throws `ArgumentException`; sections
  are added in `SMASS_PaperSection.NoUrut` order and concepts inside a section in
  mapping `NoUrut` order.
- Registration/service bypass (AR-08, AR-10): `LayananDirect` stamps `LayananId`
  from the request with `LayananName = ''`; `AssesmentDateDirect` sets
  `AssesmentDate` without the JenisRawat window; `PendingIgdTriage` clears
  `RegId`/`PasienId`/`PasienName` and stamps `IgdVisitId`, `NoTriage`,
  `RegistrationLinkStatus = PendingRegistration`. `IGetRegService` /
  `IGetLayananService` are never invoked. `AssesmentValidator` is not re-enabled
  and `CreateFixAssesmentCommand` is not modified.
- Paper existence is validated through the existing builder `.Paper(request)`
  path (`IPaperDal`); persistence is `IAssesmentWriter.Save` only. `Finish()` is
  never called (post-condition `Drafting` from `AddSection`) and no
  `CreatedAssesmentEvent` / `AddedSectionAssesmentEvent` is published (AR-13,
  AR-14).
- Response is emitted with `RegistrationLinkStatus` as `int` and `AssesmentState`
  as string (`Drafting`) to match the BILREG gateway DTO
  (`SmassAssessmentGateway.GenerateResponse`) on the wire.
- Idempotency: `_assesmentDal.GetData(request, request.NoTriage)` returns the
  existing snapshot idempotently without mutation when present.
- Builds verified: `dotnet build` on `Smass.Application`, `Smass.Infrastructure`,
  `Smass.Api` (0 errors); VS `MSBuild.exe` `/t:Rebuild` on
  `A043_Smass_StructuredMedicalAssesment_Api.sln` (Build succeeded, 0 errors,
  exit 0, including the `Smass.Db` SSDT dacpac); `dotnet test --filter
  FullyQualifiedName~SyncChartVitalSign` (27 passed, 0 failed).

---

### P2-S06

Title: LinkAssesmentByIgdVisitIdCommand

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Changed files: `Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Commands/LinkAssesmentByIgdVisitIdCommand.cs`
  (new: request + response + handler). No other source file changed; the
  command is picked up by the existing Scrutor/MediatR registration scan.
- Route `PATCH api/Assesment/linkIgdVisit` is recorded as an XML-doc contract on
  the command type; controller wiring remains P2-S08.
- Guard: Dawn `Guard.Argument(() => request).NotNull()` then `.Member(...)
  .NotEmpty()` for all six fields (`IgdVisitId`, `RegId`, `PasienId`,
  `PasienName`, `LayananId`, `LayananName`), mirroring `CreateFixAssesmentCommand`.
- Selection: the command implements `IIgdVisitKey`, so
  `_assesmentDal.ListData(request)` binds to the by-visit overload and returns
  every assessment of the visit (pending and registered) in one query — the
  single behaviour that satisfies BR-15, BR-17 and AR-15. An empty result
  returns `LinkedCount = 0` with `ListAssesmentId = []` and performs no write.
- Per-assessment atomicity: the loop opens `TransHelper.NewScope()` per
  assessment, so each assessment's key population commits (or rolls back)
  independently. Inside the scope only `RegId`, `PasienId`, `PasienName`,
  `LayananId`, `LayananName` and `RegistrationLinkStatus = Registered` are set;
  `_assesmentDal.Update(model)` is called directly (the `AssesmentWriter` is not
  used, so its section/concept delete+reinsert path never runs).
  `AssesmentState`, sections and concepts are never read or written, `Finish()`
  is never called, and `IGetRegService` / `IGetLayananService` are not injected
  or called (AR-10, AR-13, BR-13/BR-14, INV-A5/A6).
- Failure semantics: per-assessment exceptions are collected in the loop; after
  the loop, any collected failure throws `AggregateException`, so partial
  success persists and BILREG records a Failed `Link` task and retries (BR-16).
  Successful assessments on the wire are reported as `LinkedCount` plus the
  matching `ListAssesmentId[]`.
- No event publish: neither `CreatedAssesmentEvent` nor
  `AddedSectionAssesmentEvent` is referenced by the command (AR-14). No
  delete/purge/unlink path is introduced (BR-18, BR-31/32).
- Builds verified: `dotnet build` on `Smass.Application`,
  `Smass.Infrastructure`, `Smass.Api` (all succeeded, 0 errors; pre-existing
  unrelated warnings only); VS `MSBuild.exe` `/t:Build` on
  `A043_Smass_StructuredMedicalAssesment_Api.sln` (Build succeeded, exit 0,
  including the `Smass.Db` SSDT dacpac).

---

### P2-S07

Title: SMASS read surfaces — by-visit, pending-registration monitoring, provenance view fields

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Changed/new files:
  `Smass.Application/AssesmentContext/AssesmentAgg/UseCases/Queries/AssesmentIgdView.cs`
  (new: shared IGD provenance/link view-model base + label helpers),
  `.../Queries/ListAssesmentByIgdVisitIdQuery.cs` (new: query + response + handler),
  `.../Queries/ListPendingRegistrationAssesmentQuery.cs`
  (new: query + `PendingRegistrationView` + handler),
  `.../Queries/ListCatalogQuery.cs` (response now inherits `AssesmentIgdView`;
  projection fills the provenance fields),
  `.../Queries/ListAssesmentByRegIdQuery.cs` (response now inherits
  `AssesmentIgdView`; provenance fields applied after the Mapster adapt).
  No DAL, controller, domain, SSDT, or DI file changed.
- `AssesmentIgdView` (architecture §6.5) holds the six contract fields
  `AssesmentId`, `IgdVisitId`, `NoTriage`, `RegistrationLinkStatus` (`int`),
  `RegistrationLinkStatusLabel`, `SourceLabel`, implements `IAssesmentKey`, and
  exposes `GetRegistrationLinkStatusLabel` / `GetSourceLabel` /
  `ApplyProvenance` so every surface computes the labels identically. Labels are
  exactly `"Pending Registration"` / `"Registered"` and
  `"Generated From IGD Triage"` when `IgdVisitId` is non-empty, else `""`.
  Each concrete response keeps its existing display fields additively; nothing
  was renamed or removed.
- `ListAssesmentByIgdVisitIdQuery` is a positional record implementing
  `IIgdVisitKey`, so the handler's single `_assesmentDal.ListData(request)` binds
  to the P1-S03 by-visit overload (`IX_SMASS_Assesment_IgdVisitId`) and returns
  pending and registered rows together. This is the only surface that can return
  a pending row; it orders by `NoTriage` then `AssesmentId`. Route
  `GET api/Assesment/igdVisit/{igdVisitId}` is recorded as an XML-doc contract on
  the query type; controller wiring remains P2-S08.
- `ListPendingRegistrationAssesmentQuery` reuses
  `IAssesmentDal.ListPendingRegistration()` and re-applies
  `OrderBy(CreateDate)` so "oldest first" is explicit. `PendingRegistrationView`
  (architecture §6.5) adds `AssesmentDate`, `CreateDate`, `PaperId`, `PaperName`,
  `UserrId` and computed `AgeDays` (whole days since `CreateDate`, clamped at 0).
  Route `GET api/Assesment/pendingRegistration` is likewise xml-doc only.
- Additive-only changes: `ListCatalogResponse` and `ListAssesmentByRegIdResponse`
  now inherit the six provenance fields from `AssesmentIgdView`; their existing
  JSON/C# fields (`assesmentId`, `assesmentDate`, `paperId`, `paperName`,
  `layananId`, `layananName`, `userId`/`userName`, `userrName`,
  `assesmentState`, `oftaDocId`, `oftaDocUrl`, `isSigned`, `verificators`,
  `regId`/`pasienId`/`pasienName`, `listSection`) are unchanged, so existing
  consumers keep their fields. `ListCatalogResponse` still satisfies
  `IAssesmentKey` via the inherited `AssesmentId` (verificator lookup intact).
- Quarantine unchanged and untouched: catalog and RegId handlers still call
  `ListData(IRegKey)`, which carries the P1-S03 predicate
  `RegistrationLinkStatus = Registered`; `ListAssesmentByPasienIdQuery` and the
  OFTA/report flows inherit it. Pending rows therefore remain unreachable from
  every RegId/PasienId surface. `ListAssesmentByPasienIdQuery` was not modified.
- Read-only: no delete/purge/archive/unlink action or DAL method was added or
  invoked on any surface (BR-31). Soft-deleted rows (`AssesmentState = Deleted`)
  are excluded from both new surfaces, matching every existing read surface.
- Verified dependency P1-S03 (IMPLEMENTED): `IAssesmentDal.ListData(IIgdVisitKey)`,
  `ListPendingRegistration()`, `AssesmentModel.IgdVisitId`/`NoTriage`/
  `RegistrationLinkStatus`, and the `SMASS_Assesment` provenance columns all
  present.
- Builds verified: `dotnet build` on `Smass.Application`, `Smass.Infrastructure`,
  `Smass.Api` (0 errors; pre-existing unrelated warnings only); VS `MSBuild.exe`
  `/t:Rebuild` on `A043_Smass_StructuredMedicalAssesment_Api.sln` (Build
  succeeded, 34 pre-existing warnings, 0 errors, exit 0, including the `Smass.Db`
  SSDT dacpac).
- Deviation/decision (recorded, not contradicting architecture): `AssesmentIgdView`
  carries the six IGD provenance/link fields; the per-surface existing display
  fields stay on the concrete responses instead of being hoisted into the base,
  avoiding duplicate members on the two existing responses. All four surfaces
  expose the full §6.5 field set. No plan or architecture change is implied.

---

### P2-S08

Title: SMASS API endpoints and authorization

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Changed file:
  `Smass.Api/Controllers/AssesmentContext/AssesmentController.cs` (four new actions
  added after the existing `FinishSign` action; + `using
  Microsoft.AspNetCore.Authorization;`). No other file changed.
- Actions and dispatch (all use the existing `ControllerBase`, injected `IMediator`,
  and `Ok(new JSendOk(result))`):
  - `POST api/Assesment/generateIgdTriage` → `GenerateIgdTriageAssesmentCommand`
    (`[HttpPost][Authorize][Route("generateIgdTriage")]`).
  - `PATCH api/Assesment/linkIgdVisit` → `LinkAssesmentByIgdVisitIdCommand`
    (`[HttpPatch][Authorize][Route("linkIgdVisit")]`).
  - `GET api/Assesment/igdVisit/{igdVisitId}` → `ListAssesmentByIgdVisitIdQuery(igdVisitId)`
    (`[HttpGet][Authorize][Route("igdVisit/{igdVisitId}")]`).
  - `GET api/Assesment/pendingRegistration` → `ListPendingRegistrationAssesmentQuery()`
    (`[HttpGet][Authorize][Route("pendingRegistration")]`).
  Action method names (`GenerateIgdTriage`, `LinkIgdVisit`, `ListByIgdVisitId`,
  `ListPendingRegistration`) are unique within the controller; none of the new routes
  collides with an existing route.
- Authorization (D-13, AR-05, §9.1): `[Authorize]` is **bare** on each of the four new
  actions — no policy, role, or permission is added. The existing JWT Bearer scheme is
  already configured in `Smass.Api/Configurations/PresentationService.cs`
  (`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`) and
  the pipeline already calls `UseAuthentication()` / `UseAuthorization()` in
  `Smass.Api/Program.cs`, so all four actions require a valid token while every
  pre-existing action is unchanged. Exactly four `[Authorize]` attributes exist in the
  file (one per new action); no existing action's authorization changed.
- Architecture wording note from the slice Notes is carried forward unchanged: §9.1 and
  AR-05 say "three new SMASS endpoints" while §5.3 and the §9.1 permission-boundary
  table enumerate four surfaces; consistent with this plan, all four new actions are
  authorized. No approved decision or existing consumer behaviour changed; flagged for
  later architecture wording alignment.
- Endpoint reachability with the BILREG service identity: the BILREG gateway
  (P4-S11) obtains a JWT from `POST /Token` and attaches
  `Authorization: Bearer <jwt>`; SMASS validates it with the existing `Jwt` config, so a
  valid token makes the four actions reachable. No new scheme or validation was added.
- Builds verified: `dotnet build Smass.Api\Smass.Api.csproj -v minimal` — Build
  succeeded, 0 errors (4 pre-existing `Smass.Api` nullability warnings only); VS
  `MSBuild.exe /t:Rebuild` on `A043_Smass_StructuredMedicalAssesment_Api.sln` — Build
  succeeded (pre-existing warnings only), 0 errors, exit 0, including the `Smass.Db`
  SSDT dacpac.

---

## P3 - BILREG Persistence

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Repository for all P3 slices: `b09-bilreg-api`.

### P3-S09

Title: BILRG_IgdVisitSmassTask table

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Status: IMPLEMENTED
Review Status: GO

Repository for all P4 slices: `b09-bilreg-api`.

### P4-S11

Title: SMASS gateway, token service, and configuration options

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Both triage handlers achieve the §6.6 ordering: guards + load → domain behaviour
  (`visit.AssessTriage`) → `using (var trans = TransHelper.NewScope())` →
  `_igdVisitRepo.SaveChanges(visit)` → `trans.Complete()` (scope disposed) → toggle
  check → task upsert (`FindByBusinessKey` / `CreatePending`) → one gateway call →
  `MarkSucceeded` / `MarkFailed` → `taskRepo.SaveChanges(task)` → response.
- A shared internal helper `IgdVisitSmassGenerationHook.RunAsync` holds the
  post-commit steps so the ordering is identical in both handlers; it swallows every
  gateway/task-persistence exception (BR-10) and the HTTP call is outside every
  `TransHelper` scope. A concurrent task-insert unique-index violation is handled by
  re-reading the business key and treating the persisted row as authoritative
  (§6.2); an already-`Succeeded` task returns `Generated` without a redundant HTTP
  call (INV-T4).
- `IgdVisitAssessTriageResponse` gained `SmassAssessmentId` (empty when not
  generated) and `SmassGenerationStatus`. Status mapping:
  `Disabled` = toggle off (no task, no HTTP); `Generated` = gateway success +
  `MarkSucceeded`; `Failed` = gateway failure/rejection or `MarkFailed`;
  `Pending` = the hook returns before a terminal task result can be recorded
  (task materialisation failure, or task save failure with no re-readable row).
  The controller returns the extended response via the existing `JSendOk(result)`
  with no controller change required.
- Changed files: `IgdVisitFeature/UseCases/IgdVisitSmassGenerationHook.cs` (new),
  `IgdVisitFeature/UseCases/IgdVisitAssessTriageCmd.cs`,
  `IgdVisitFeature/UseCases/IgdVisitReAssessTriageCmd.cs` (Bilreg.Application).
- Build verified: `dotnet build src/bilreg/b09-bilreg-api.sln -v minimal` —
  Build succeeded, 0 errors (pre-existing warnings only).

---

### P4-S13

Title: Registration link hooks

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Both register handlers achieve the §6.6 ordering: guards + load (`visit`/`igdVisit` and
  the register aggregate) → domain behaviour (`visit.AssignRegister` /
  `igdVisit.ReplaceRegister`) → `using (var trans = TransHelper.NewScope())` →
  `_igdVisitRepo.SaveChanges(...)` → `trans.Complete()` (scope disposed) → toggle check →
  task upsert (`FindByBusinessKey` / `CreatePending`, `NoTriage = 0`, `TaskType = Link`) →
  one gateway call (`LinkIgdVisit`) → `MarkSucceeded` / `MarkFailed` →
  `taskRepo.SaveChanges(task)` → return (unchanged `Task`, controller still returns
  `"Done"`). The `using var` form was changed to a `using (…) { … }` block so the
  `TransHelper` scope is disposed before the HTTP call.
- A shared internal helper `IgdVisitSmassLinkHook.RunAsync` holds the post-commit steps so
  the ordering is identical in both handlers; it swallows every gateway/task-persistence
  exception (BR-16) and the HTTP call is outside every `TransHelper` scope. No
  `IgdEventEnum`/`AuditLog` entry is produced (AR-16).
- The link payload is built from the already-loaded `RegModel` (not `IgdVisitModel.Reg`):
  `reg.RegId`, `reg.Pasien.PasienId`, `reg.Pasien.PasienName`, `reg.Layanan.LayananId`,
  `reg.Layanan.LayananName`, plus `IgdVisitId`. `IgdVisitModel.Reg` is only a
  `RegReff(RegId, PasienId, PasienName)` and carries neither `LayananId` nor `LayananName`.
- Unlike the generation hook, the visit-level link is **always** re-invoked when the toggle
  is enabled, because `ReplaceRegister` must replace the latest registration values on every
  assessment (BR-17 replace-latest / AR-15); it is not short-circuited for an
  already-`Succeeded` task. INV-T4/INV-T5 make a terminal `Succeeded` task
  non-transitionable, so when the existing task is already `Succeeded` the gateway call
  still runs but the terminal state is not written again (no second
  `MarkSucceeded`/`MarkFailed`).
- `IgdVisitVoidCmd` is not modified (BR-18, BR-32). No unlink/delete path is introduced.
- Changed files: `IgdVisitFeature/UseCases/IgdVisitSmassLinkHook.cs` (new),
  `IgdVisitFeature/UseCases/IgdVisitAssignRegisterCmd.cs`,
  `IgdVisitFeature/UseCases/IgdVisitReplaceRegisterCmd.cs` (Bilreg.Application).
- Build verified: `dotnet build "src/bilreg/b09-bilreg-api.sln" -v minimal` —
  Build succeeded, 0 errors (pre-existing warnings only).

---

### P4-S14

Title: Manual retry, batch process, and task queries

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- `IgdVisitSmassTaskRetryCmd` (new) loads the task with
  `IIgdVisitSmassTaskRepo.LoadEntity`, calls `AssertCanManualRetry()` (INV-T6/IR-01),
  invokes exactly one gateway attempt, marks the task result and saves it, then
  returns the updated `IgdVisitSmassTaskView`.
  `IgdVisitSmassTaskProcessCmd` (new) loops `ListProcessable()` (Failed tasks, oldest
  first) and runs the identical executor per task; a single-task failure is counted and
  never aborts the batch. Summary is `{ Total, Succeeded, Failed }`.
- New `IgdVisitSmassTaskRetryExecutor` (new file) holds the single retry semantics,
  mirroring the P4-S12/P4-S13 hook pattern. Generate rebuild: visit aggregate via
  `IIgdVisitRepo.LoadEntity(IgdVisitModel.Key(...))` → the immutable
  `BILRG_IgdVisitTriage` row selected by `NoTriage`. Link rebuild: the visit's
  `Reg.RegId` → `IRegRepo.LoadEntity(RegModel.Key(...))` for
  `LayananId`/`LayananName` (the visit aggregate only carries
  `RegReff(RegId, PasienId, PasienName)`). No `PayloadJson` exists, is written, or is
  replayed (AR-12). The HTTP call is outside every `TransHelper` scope; the task is
  saved in a separate write.
- Payload construction is reused from P4-S12/P4-S13:
  `IgdVisitSmassGenerationHook.BuildPayload` and `IgdVisitSmassLinkHook.BuildPayload`
  were changed from `private` to `internal` so Generate and Link retries rebuild the
  exact same request DTO as the live hooks (single source of truth, no duplicated
  payload logic). Paper/Layanan still resolve from `IgdVisitOptions` in the gateway.
- `IgdVisitListSmassTaskQuery` (new) returns `IgdVisitSmassTaskView` for one visit via
  `ListByVisit`; `IgdVisitSmassWorklistQuery` (new) returns `ListProcessable()`.
  `TaskType`/`TaskStatus` are projected to the approved wire codes
  (`GENERATE`/`LINK`, `PENDING`/`SUCCEEDED`/`FAILED`) by
  `IgdVisitSmassTaskView.FromModel`, shared by both queries and the retry response.
  The sentinel `3000-01-01` dates are surfaced verbatim (client conversion is P5-S16).
- No delete/purge/archive path is introduced anywhere (BR-31); no automatic worker or
  polling is introduced (D-07). No `IgdEventEnum`/`AuditLog` entry is produced (AR-16).
- Decision recorded (not contradicted by the architecture): manual retry is **not**
  gated by `IgdVisit:EnableSmassIntegration`. The task row already exists and an
  operator explicitly requests the attempt, matching D-15/BR-18/IR-07 (retry remains
  available for terminal/voided visits). Missing configuration still fails closed in
  the gateway without an HTTP call (AR-02).
- Changed files:
  `Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskRetryCmd.cs`
  (new),
  `Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskProcessCmd.cs`
  (new),
  `Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitListSmassTaskQuery.cs`
  (new),
  `Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassWorklistQuery.cs`
  (new),
  `Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskRetryExecutor.cs`
  (new),
  `Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitSmassGenerationHook.cs`
  (payload builder visibility: `private` → `internal`),
  `Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitSmassLinkHook.cs`
  (payload builder visibility: `private` → `internal`).
- Build verified: `dotnet build "src/bilreg/b09-bilreg-api.sln" -v minimal` —
  Build succeeded, 0 errors (pre-existing warnings only).

---

### P4-S15

Title: IgdVisitSmassTaskController

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- New `IgdVisitSmassTaskController` (BILREG API) at
  `Bilreg.Api/Controllers/IgdContext/IgdVisitSmassTaskController.cs`, mirroring the
  existing IGD controllers (`IgdVisitController`, `BedIgdController`):
  `[Route("api/[controller]")]`, `[ApiController]`, `[Authorize]`, `ControllerBase`,
  injected `IMediator`, and `Ok(new JSendOk(...))` for every response.
- Actions and dispatch:
  `GET api/IgdVisitSmassTask/{igdVisitId}` → `IgdVisitListSmassTaskQuery(igdVisitId)`;
  `GET api/IgdVisitSmassTask/worklist` → `IgdVisitSmassWorklistQuery`;
  `PATCH api/IgdVisitSmassTask/retry` → `IgdVisitSmassTaskRetryCmd(body.IgdVisitSmassTaskId)`
  (request body `IgdVisitSmassTaskRetryBody { IgdVisitSmassTaskId }`, carrying the task id
  per architecture §9.2);
  `POST api/IgdVisitSmassTask/process` → `IgdVisitSmassTaskProcessCmd` (no body).
  The literal `worklist` route wins over the `{igdVisitId}` parameter route by attribute
  routing precedence.
- `[Authorize]` is bare (no policy, role, or permission added — AR-05/§9.1); it matches
  every other IGD controller.
- Monitoring remains endpoint-only: the controller adds no screen and no
  delete/purge/archive path (AR-06, BR-31); no polling or background worker is introduced
  (D-07). P4-S15 only exposes the P4-S14 use cases; no business logic is added here.
- Changed files:
  `Bilreg.Api/Controllers/IgdContext/IgdVisitSmassTaskController.cs` (new).
- Build verified: `dotnet build "src/bilreg/b09-bilreg-api.sln" -v minimal` —
  Build succeeded, 0 errors (pre-existing warnings only).

---

## P5 - Web Client

Implementation Status: IN-PROGRESS
Review Status: NOT-REVIEWED

Repository for all P5 slices: `c012_myhospital_web`.

### P5-S16

Title: Emergency contracts, query keys, and SMASS task hooks

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Changed files:
  `src/modules/Emergency/types/contract.ts` (adds
  `smassGenerationStatusSchema`, `assignTriaseResponseSchema` extension,
  `igdSmassTaskTypeSchema`, `igdSmassTaskStatusSchema`, `igdSmassTaskViewSchema`,
  `igdSmassTaskListResponseSchema`, `retryIgdSmassTaskPayloadSchema`, and the
  matching inferred types),
  `src/core/api/queryConfigs.ts` (adds `queryKeys.emergency.igdSmassTask` with
  `all`/`lists`/`list(visitId)`/`details`/`detail(taskId)`),
  `src/modules/Emergency/queries/EmergencyService.ts` (adds
  `useIgdSmassTask`, `useRetryIgdSmassTask`, `API_ENDPOINT.IGD_SMASS_TASK =
  'IgdVisitSmassTask'`, `EMERGENCY_SERVICE_KEYS.RETRY_IGD_SMASS_TASK`, and both
  hooks in the returned service object),
  `src/modules/Emergency/types/domain.ts` (adds the `IgdSmassTask` view type with
  `lastRetryDate`/`processedDate` nullable),
  `src/modules/Emergency/utils/mappers.ts` (adds
  `mapIgdSmassTaskToDomain`, converting the sentinel dates to `null` with the
  existing `isSentinel()` helper per architecture §5.5).
- Hooks: `useIgdSmassTask(visitId)` issues `GET
  IgdVisitSmassTask/{igdVisitId}` (`createQueryFn` over
  `igdSmassTaskListResponseSchema`, `placeholderData: []`, enabled only when a
  visit id is present, no polling) and keys on
  `queryKeys.emergency.igdSmassTask.list(visitId)`. `useRetryIgdSmassTask()`
  issues `PATCH IgdVisitSmassTask/retry` with body `{ igdVisitSmassTaskId }`
  (`retryIgdSmassTaskPayloadSchema` → `igdSmassTaskViewSchema`) and invalidates
  `queryKeys.emergency.igdSmassTask.all()` in `onSettled`; there is no optimistic
  update (architecture §5.5).
- Casing deviation (recorded, flagged for the Architect): the plan P5-S16 and
  ARCHITECTURE §6.5 specify `smassGenerationStatus` as
  `"DISABLED" | "PENDING" | "GENERATED" | "FAILED"`, but the accepted BILREG
  implementation (P4-S12, GO — `IgdVisitSmassGenerationHook.SmassGenerationResult`
  and `IgdVisitAssessTriageCmd.SmassGenerationStatus`) emits PascalCase
  `"Disabled" | "Pending" | "Generated" | "Failed"`; ARCHITECTURE §5.2 already
  states those exact PascalCase values. The Zod schema uses the actual BILREG
  wire values `['Disabled', 'Pending', 'Generated', 'Failed']` so the triage and
  re-triage responses parse end-to-end, as instructed. This is an
  architecture/plan inconsistency (§5.2 vs §6.5; plan P4-S12 vs P5-S16), not a
  code defect; BILREG was not modified.
- `smassAssessmentId` is typed `z.string()` (empty string when not generated),
  matching the BILREG wire and the plan's type.
- `igdSmassTaskViewSchema` mirrors the `IgdVisitSmassTaskView` wire exactly:
  `taskType` `"GENERATE" | "LINK"`, `taskStatus`
  `"PENDING" | "SUCCEEDED" | "FAILED"`; the sentinel `3000-01-01` dates are
  surfaced verbatim by BILREG and converted to `null` only in
  `mapIgdSmassTaskToDomain` (module mapper path).
- The web client calls only BILREG (`IgdVisitSmassTask/*`); no SMASS endpoint is
  referenced anywhere in the change (D-02, §10.3). No Pinia store was introduced;
  server state stays in TanStack Query.
- Verification: `npx prettier --write` on the five modified files (no unintended
  changes); `pnpm tc:app` → exit 0, no type errors. `pnpm lint` fails on
  pre-existing repo-wide findings (95 oxlint errors across 975 files, e.g.
  `useBedStore.ts`, `AuthService.spec.ts` merge markers, `queryConfigs.ts`
  laboratory `any`s) that are unrelated to this slice; `oxlint` on the five
  changed files reports only the pre-existing findings in
  `EmergencyService.ts` (unused `IgdRegisterMutationInput`, `AssignTriaseResponse`,
  `MarkBedCleanResponse` imports) and `mappers.ts` (unused `DateTime` import,
  `hasReg` local), and `eslint` on them reports only the pre-existing
  `queryConfigs.ts` `any` findings. No new lint finding was introduced.

---

### P5-S17

Title: SCR-02 — Triage History SMASS status

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Changed file: `src/modules/Emergency/components/TriageHistoryPanel.vue` (the only
  file changed). Verified dependency P5-S16 (IMPLEMENTED/GO): `useIgdSmassTask`,
  `igdSmassTaskViewSchema`, `queryKeys.emergency.igdSmassTask`, and
  `TriageHistoryPanel.vue` all exist. The panel now calls the existing
  `useIgdSmassTask(computed(() => props.visitId))` next to `useTriageHistory` and
  merges both already-cached query results in a single `historyRows` computed. No new
  endpoint, no extra request, no new table column, no Pinia store.
- Join: `historyRows` builds a `Map<noTriage, SmassRowStatus>` from tasks where
  `taskType = "GENERATE"` and `task.igdVisitId === props.visitId` (the
  (`igdVisitId`, `noTriage`, `GENERATE`) business key), then overlays an `smass` slot
  on each triage-history row; the task list is already visit-scoped and the
  `igdVisitId` equality is still asserted explicitly.
- Display: the row header gains a right-side cluster with the SMASS status badge
  immediately right of the timestamp (before the expand chevron) and the
  `assessmentId` rendered as plain text only when non-empty. `AssessmentId` is text
  only — no link, no router navigation, no copy action.
- Status mapping (task wire `PENDING | SUCCEEDED | FAILED` → displayed):
  `SUCCEEDED → Generated` (green), `FAILED → Failed` (red), `PENDING → Pending`
  (amber). Badge uses the existing inline convention
  (`rounded px-1.5 py-0.5 text-[10px] font-black uppercase`); `assessmentId` is shown
  only on `GENERATE` rows because the join is Generate-only (IR-05/IR-06).
- `Disabled` is intentionally NOT rendered. `igdSmassTaskViewSchema` can only express
  `PENDING | SUCCEEDED | FAILED`, and when `IgdVisit:EnableSmassIntegration` is off
  no `Generate` task row is created (P4-S12). The computed join therefore yields no
  `smass` slot and no badge is rendered — the exact architecture SCR-02 transition
  rule "otherwise the row shows no SMASS slot" and IR-04 ("Disabled → no SMASS slot
  rendered anywhere"). The achievable displayed states are Generated/Pending/Failed;
  `Disabled` is not fabricated from data that cannot express it and no request/field
  was added to obtain it. Recorded as a data-model limitation of the task read
  surface (finding), not a code defect: the "Disabled" criterion is satisfied through
  its specified no-slot behaviour.
- Backward compatibility: for visits created before rollout, or with the toggle off,
  the panel renders exactly as before minus the (absent) SMASS slot, because no
  `Generate` task exists to join.
- Read-only / toggles: `showReTriage` and `showTriageHistory` remain the
  source-of-truth toggles in `TriaseIGD.vue`, which was not modified. The panel adds
  no mutation, no retry, no navigation, and no delete/cancel/archive action; it only
  reads the two already-cached queries.
- Verification: `npx prettier --write` on the changed file → unchanged;
  `pnpm tc:app` → exit 0, no type errors; `npx oxlint
  src/modules/Emergency/components/TriageHistoryPanel.vue -D correctness` → 0
  warnings, 0 errors; `npx eslint
  src/modules/Emergency/components/TriageHistoryPanel.vue` → 0 findings. Scoped unit
  run `pnpm test:unit -- --run src/modules/Emergency`: `TriaseIGD.spec.ts` 14/14
  passed; the 5 failures are confined to the pre-existing `IgdVisitList.spec.ts`
  (Pinia "no active Pinia" in `useBusinessNow`), unrelated to this slice.

---

### P5-S18

Title: SCR-03 — SMASS Integration Panel and manual retry

Implementation Status: IMPLEMENTED
Review Status: GO

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

Implementation Notes (2026-09-19):

- Changed files: `src/modules/Emergency/components/IgdSmassIntegrationPanel.vue`
  (new) and `src/modules/Emergency/views/TriaseIGD.vue` (import + mounts). No other
  file was changed. Verified dependency P5-S16 (IMPLEMENTED/GO): `useIgdSmassTask`,
  `useRetryIgdSmassTask`, `igdSmassTaskViewSchema`, `queryKeys.emergency.igdSmassTask`,
  and `TriaseIGD.vue` all exist.
- New component is presentational and owns its query exactly like
  `TriageHistoryPanel.vue`: `useIgdSmassTask(computed(() => props.visitId))` plus
  `useRetryIgdSmassTask()`. The only prop is `visitId: string`. All UI state
  (`isOpen`, `retryingTaskId`) is local `ref` state; no Pinia store and no new
  contract/hook/query-key was added. The client calls only
  `IgdVisitSmassTask/*` (BILREG); SMASS is never referenced (D-02).
- Row rendering: one row per task with the `TaskType` label
  (`GENERATE → Generate`, `LINK → Link`), `NoTriage` (`#n`, or `Visit` when
  `noTriage = 0`), a status badge (`SUCCEEDED → Succeeded` green,
  `FAILED → Failed` red, `PENDING → Pending` amber), `AssessmentId` rendered only
  for `GENERATE` rows (IR-05/IR-06), and `LastError` rendered when non-empty.
- IR-01: "Coba Ulang" is enabled only for `taskStatus = "FAILED"`. It is hidden (not
  rendered) for `SUCCEEDED` and rendered disabled for `PENDING`, matching the
  architecture wording "hidden, not merely disabled, for `Succeeded`". The button
  calls `useRetryIgdSmassTask().mutate({ igdVisitSmassTaskId })`, which mirrors the
  server `AssertCanManualRetry()` guard (INV-T6).
- IR-02: `isRetrying` is `retryMutation.isPending`; every rendered retry button binds
  `:disabled="isRetrying"`, so all retry buttons are disabled while any row retries.
  On settle the hook's existing `onSettled` invalidates
  `queryKeys.emergency.igdSmassTask.all()` (no optimistic update); the panel adds
  exactly one toast per retry outcome (`toast.success` on success, `toast.error` on
  error) using the existing `vue-sonner` pattern, and clears `retryingTaskId`.
- IR-03: `isOpen` starts `false` and a `watch` on `failedCount` auto-expands the panel
  when the count transitions from 0 to > 0. A red summary chip `"n gagal"` renders in
  the always-visible header whenever `failedCount > 0` (so failures are visible even
  while collapsed). With no failed task the panel stays collapsed by default.
- Mounting: the panel is mounted directly below SCR-02 (`TriageHistoryPanel`) in the
  desktop triaged and paired contexts, and as a section in the tablet triage area and
  the mobile ANTRIAN (triaged) / BED (paired) triage areas. In the paired contexts it
  is mounted unconditionally for the selected visit, so it renders for every selected
  visit and retry stays available for terminal/voided visits (IR-07). The existing
  layout was preserved; the desktop triaged history wrapper became
  `flex min-h-0 flex-1 flex-col` so history keeps `flex-1` and the SMASS panel is
  `shrink-0`.
- BR-31: the component exposes no delete, cancel, archive, or unlink action; its only
  mutation is the manual retry already approved by D-07/BR-12. No Pinia store was
  introduced (architecture §10.3).
- Verification: `npx prettier --write` on both files (applied);
  `pnpm tc:app` → exit 0, no type errors; `npx oxlint
  src/modules/Emergency/components/IgdSmassIntegrationPanel.vue
  src/modules/Emergency/views/TriaseIGD.vue` → 0 warnings, 0 errors; `npx eslint` on
  both files → 0 findings in the new component and only 15 pre-existing
  `@typescript-eslint/no-unused-vars` findings in `TriaseIGD.vue` (unchanged
  declarations such as `nextTick`, `Bed`, `BedIgdAvailableItem`, `isLoadingBedIgd`,
  `isMobile`, `VisitModal`, `isVisitDesktop`, `triageHistory`, `unpairedVisits`,
  `occupiedVisits`, `bedId`, `visitId`, `dokterListQuery`,
  `openConvertToRawatInapModal`), none introduced by this slice.

---

# 6. Change Log

- 2026-09-19 — v1.0 — Initial IMPLEMENTATION-PLAN created from ARCHITECTURE
  `igd-triage-abc-smass-architecture.md` (V2.0). 18 slices across 5 phases and 3
  repositories.
- 2026-09-19 — v1.0 — `Execution Approval` set to `APPROVED` by the Architect.
  The plan structure is now immutable; execution may begin. Structural correction
  from this point requires a replacement IMPLEMENTATION-PLAN created through a new
  Planning cycle.

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
