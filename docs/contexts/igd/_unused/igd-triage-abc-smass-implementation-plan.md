# IGD Triage ABC → SMASS — Implementation Plan

> **Document status:** PLANNING (implementation plan only — no implementation in this document)
> **Date:** 2026-09-17
> **Canonical location:** `docs/contexts/igd/igd-triage-abc-smass-implementation-plan.md`
> **Planning authority:** Architecture (`docs/contexts/igd/igd-triage-abc-smass-architecture.md`)
> **Source of truth for decisions:** `docs/contexts/igd/igd-triage-abc-smass-architecture.md` (D-01…D-15, BR-01…BR-33, AR-01…AR-16)

---

## 1. Planning Authority

```text
ARCHITECTURE
```

This plan realizes the approved architecture and does **not** introduce, reinterpret, or override any business or architecture decision. Where the architecture closes a planning input with an explicit decision register entry (AR-01…AR-16), that decision is binding and is cited rather than re-decided.

---

## 2. Scope Summary

Realize, in software, the approved capability: **every IGD Visit Triage ABC submission (first triage and re-triage) synchronously produces one immutable structured medical assessment in SMASS**, built against a dedicated IGD Triage Paper, correlated by (`IgdVisitId`, `NoTriage`), created before registration exists (`PendingRegistration`), and linked to the administrative registration when `AssignRegister` / `ReplaceRegister` succeeds.

The implementation spans three repositories:

| Repository | Scope |
|---|---|
| `b09-bilreg-api` (BILREG, `IgdContext`) | `IgdVisitSmassTask` operational task store, `ISmassAssessmentGateway`, configuration/retry surface, four handler hooks, list/retry/worklist API |
| `a043_smass_structuredmedicalassesment_api` (SMASS, `AssesmentContext` + `StructureContext`) | Pending-registration creation path, registration-link path, by-visit query, mapping master data, persistence changes, seed data |
| `c012_myhospital_web` (Web) | `IgdSmassIntegrationPanel` (SCR-03), `TriageHistoryPanel` badge (SCR-02), hooks/types/query-keys |

All work is **additive**; `IgdVisitVoidCmd`, `CreateFixAssesmentCommand`, `AssesmentValidator`, and legacy `SMASS_Assesment` rows are untouched (BR-18, BR-21, BR-22).

---

## 3. Impact Inventory

### 3.1 Database

| Component | Kind | Reference |
|---|---|---|
| `SMASS_Assesment` | Alter (additive: `IgdVisitId`, `NoTriage`, `RegistrationLinkStatus`; filtered unique index `UX_SMASS_Assesment_IgdVisitTriage`; index `IX_SMASS_Assesment_IgdVisitId`) | §6.2 |
| `SMASS_TriageConceptMap` | New table + `IX_SMASS_TriageConceptMap_ConceptId` | §6.3 |
| IGD Triage Paper master data (`SMASS_Paper`, `SMASS_PaperSection`, `SMASS_CustomSection`, `SMASS_CustomSectionConcept`) | New seed data | §6.4 |
| `IgdTriagePaperDataSeed.sql`, `TriageConceptMapDataSeed.sql` | New seed scripts | §6.5 |
| `BILRG_IgdVisitSmassTask` | New table + `UX_..._BusinessKey`, `IX_..._Visit`, `IX_..._Status` | §6.1 |

### 3.2 Backend

| Component | Kind | Reference |
|---|---|---|
| SMASS `AssesmentModel` | Extend (3 fields, `IIgdVisitKey`, INV-A1…A6) | §5.2 |
| SMASS `RegistrationLinkStatusEnum`, `IIgdVisitKey` | New value object / enum | §5.2 |
| SMASS `TriageConceptMapModel`, `ITriageConceptMapKey` | New aggregate | §5.3 |
| SMASS `IAssesmentDal` | Extend (`GetData`, `ListData(IIgdVisitKey)`, `ListPendingRegistration`, quarantine predicate, column projection) | §5.2, AR-11 |
| SMASS `TriageConceptMapDal` | New DAL | §5.3 |
| SMASS `GenerateIgdTriageAssesmentCommand` | New command | §7.2 |
| SMASS `LinkAssesmentByIgdVisitIdCommand` | New command | §7.2 |
| SMASS `ListAssesmentByIgdVisitIdQuery`, `ListPendingRegistrationAssesmentQuery` | New queries | §7.2 |
| SMASS `AssesmentController` | Extend (3 endpoints + `[Authorize]`) | §9.2 |
| BILREG `IgdVisitSmassTaskModel`, `SmassTaskTypeEnum`, `SmassTaskStatusEnum`, `IIgdVisitSmassTaskKey` | New aggregate + enums | §5.1 |
| BILREG `IgdVisitSmassTaskDal` / `IgdVisitSmassTaskRepo` | New DAL / repo | §5.1 |
| BILREG `SmassOptions`, `IgdVisitOptions`, `ISmassTokenService` | New configuration + service | §9.1, §10.6 |
| BILREG `ISmassAssessmentGateway` / `SmassAssessmentGateway` | New port / adapter | §4.1, §8 |
| BILREG handler hooks (`IgdVisitAssessTriageCmd`, `IgdVisitReAssessTriageCmd`, `IgdVisitAssignRegisterCmd`, `IgdVisitReplaceRegisterCmd`) | Modify (post-commit hook) | §7.1 |
| BILREG `IgdVisitSmassTaskRetryCmd`, `IgdVisitSmassTaskProcessCmd` | New commands | §7.1 |
| BILREG `IgdVisitListSmassTaskQuery`, `IgdVisitSmassWorklistQuery` | New queries | §7.1 |
| BILREG `IgdVisitSmassTaskController` | New controller | §4.1 |
| BILREG `IgdVisitSmassTaskView`, `IgdVisitAssessTriageResponse` (extended) | New / extended DTO | §7.1, §19.1 |

### 3.3 Frontend

| Component | Kind | Reference |
|---|---|---|
| `src/modules/Emergency/types/contract.ts` | Extend (Zod schemas: `IgdSmassTaskView`, `AssignTriaseResponse`, `TriageHistoryItem`) | §19, §23 |
| `src/core/api/queryConfigs.ts` | Extend (`queryKeys.emergency`) | §23 |
| `src/modules/Emergency/queries/EmergencyService.ts` | Extend (`useIgdSmassTask`, `useRetryIgdSmassTask`) | §17, §23 |
| `TriageHistoryPanel.vue` (SCR-02) | Modify (SMASS status badge + `AssessmentId`) | §11, §12, §19.3 |
| `IgdSmassIntegrationPanel.vue` (SCR-03) | New component | §11, §12, §16 |

### 3.4 Integration

| Component | Kind | Reference |
|---|---|---|
| `POST {Smass}/api/Assesment/generateIgdTriage` | BILREG → SMASS outbound | §8.1 |
| `PATCH {Smass}/api/Assesment/linkIgdVisit` | BILREG → SMASS outbound | §8.2 |
| `POST {Smass}/Token` (JWT, cached) | BILREG → SMASS auth | §9.1, AR-04 |
| `GET api/IgdVisitSmassTask/{igdVisitId}`, `GET api/IgdVisitSmassTask/worklist`, `PATCH api/IgdVisitSmassTask/retry` | BILREG read/retry surfaces | §4.1, §8.3 |
| `GET api/Assesment/igdVisit/{igdVisitId}`, `GET api/Assesment/pendingRegistration` | SMASS read surfaces | §8.3 |

---

## 4. Phases

| Phase | Objective | Result |
|---|---|---|
| Phase 1 | SMASS Persistence & Master Data | SMASS schema + seed data in place, verifiable in isolation (BR-23 step 1–2) |
| Phase 2 | SMASS Domain & Application | SMASS generate/link/query surfaces complete and independently testable (BR-23 step 2–3) |
| Phase 3 | BILREG Persistence & Domain | `IgdVisitSmassTask` aggregate + persistence in place |
| Phase 4 | BILREG Integration & Application | Gateway, options, handler hooks, retry/worklist API; end-to-end backend slice |
| Phase 5 | Frontend | SCR-02 badge + SCR-03 panel (display-only; cannot change backend outcomes) |
| Phase 6 | End-to-End Verification & Rollout | Ordered rollout + rollback verified against BR-23 |

Sequencing rationale (from architecture §23): SMASS persistence + master data must exist before the BILREG toggle is enabled; SMASS read/write surfaces must be complete before BILREG records any task row; frontend is last; manual-retry and monitoring are delivered with the first end-to-end slice (Phase 4).

---

## 5. Slices

### Phase 1 — SMASS Persistence & Master Data

#### P1S1 — `SMASS_Assesment` additive alteration

- **Objective:** Add `IgdVisitId`, `NoTriage`, `RegistrationLinkStatus` columns; add filtered unique index `UX_SMASS_Assesment_IgdVisitTriage` and index `IX_SMASS_Assesment_IgdVisitId`.
- **Dependencies:** None.
- **Acceptance Criteria:**
  - `Smass.Db/AssesmentContext/SMASS_Assesment.sql` is altered additively only (`NOT NULL DEFAULT('')` / `DEFAULT(0)`), no drop/rewrite of existing columns.
  - `UX_SMASS_Assesment_IgdVisitTriage (IgdVisitId, NoTriage) WHERE IgdVisitId <> ''` enforces INV-A4 / BR-07 / BR-11.
  - `IX_SMASS_Assesment_IgdVisitId (IgdVisitId) INCLUDE (AssesmentId, NoTriage, RegistrationLinkStatus)` exists (D-10).
  - Existing rows default to `IgdVisitId = ''`, `NoTriage = 0`, `RegistrationLinkStatus = 0` (`Registered`) — no backfill (BR-21).
- **Review Focus:** Persistence Compliance; Backward Compatibility (BR-22).

#### P1S2 — `SMASS_TriageConceptMap` table

- **Objective:** Create `SMASS_TriageConceptMap` with PK `(TriageFieldCode, TriageValue)` and `IX_SMASS_TriageConceptMap_ConceptId`, registered in `Smass.Db.sqlproj`.
- **Dependencies:** None.
- **Acceptance Criteria:**
  - Table columns match §6.3 exactly; PK enforces INV-M1.
  - Script registered under `<Build>` ItemGroup (precedent `SMASS_LayananSmf.sql`).
  - No soft-delete/audit columns (pure master data).
- **Review Focus:** Persistence Compliance; Master-Data Ownership (D-12).

#### P1S3 — IGD Triage Paper master data seed

- **Objective:** Seed the dedicated IGD Triage Paper (`PP-001-IGDTR`), its `CustomSection` PaperSections, `SMASS_CustomSection`, and `SMASS_CustomSectionConcept` rows.
- **Dependencies:** None (SMASS master data).
- **Acceptance Criteria:**
  - `Smass.Db/DataSeeds/IgdTriagePaperDataSeed.sql` registered `<None>` (precedent `PaperDataSeed.sql`), idempotent (`DELETE` then `INSERT`).
  - Exactly one new Paper; no existing IGD Paper modified (D-05).
  - Every concept referenced by §5.3 `TriageFieldCode` mapping is present in the Paper's custom sections.
- **Review Focus:** Master-Data Compliance (D-05, AR-09).

#### P1S4 — `TriageConceptMapDataSeed.sql` mapping seed

- **Objective:** Seed one `SMASS_TriageConceptMap` row per (`TriageFieldCode`, `TriageValue`) from §5.3, using `ConceptId` from existing `SMASS_Concept` seeds.
- **Dependencies:** P1S2, P1S3.
- **Acceptance Criteria:**
  - Covers all ten `TriageFieldCode` domains (AIRWAYS, BREATHING, CIRCULATION, GCS_EYE/MOTOR/VOICE/TOTAL, ATS_LEVEL, TRIAGE_COLOR incl. `BLACK`, MANUAL_OVERRIDE_BLACK).
  - Idempotent (`DELETE` then `INSERT`), registered `<None>` (precedent `PreferenceDataSeed.sql`).
  - Every `ConceptId` resolves to an existing `SMASS_Concept`; values derived from existing concept/preference seeds (D-03, D-12).
- **Review Focus:** Mapping Master-Data Compliance (BR-24, BR-25, BR-26, AR-07).

---

### Phase 2 — SMASS Domain & Application

#### P2S1 — `AssesmentModel` extension + `RegistrationLinkStatusEnum` + `IIgdVisitKey`

- **Objective:** Add `IgdVisitId`, `NoTriage`, `RegistrationLinkStatus` to `AssesmentModel`; add `IIgdVisitKey`; add `RegistrationLinkStatusEnum`; encode INV-A1…A6.
- **Dependencies:** None (domain only).
- **Acceptance Criteria:**
  - `RegistrationLinkStatusEnum { Registered = 0, PendingRegistration = 1 }` in `Smass.Domain/AssesmentContext/AssesmentAgg/RegistrationLinkStatus.cs`.
  - `IIgdVisitKey { string IgdVisitId }` in `Smass.Domain/ExternalContext/IgdVisitAgg/IIgdVisitKey.cs`.
  - INV-A1…A6 hold (pending ⇒ `IgdVisitId` non-empty and `RegId`/`PasienId`/names empty; `AggStateEnum` never written by feature).
  - Domain tests cover INV-A1…A6.
- **Review Focus:** Architecture Compliance; Domain Invariant Compliance (BR-01, BR-02, BR-06, BR-13, BR-14).

#### P2S2 — `TriageConceptMapModel` + `IAssesmentDal` extension + `TriageConceptMapDal`

- **Objective:** Add `TriageConceptMapModel`/`ITriageConceptMapKey`; extend `IAssesmentDal` with `GetData(IIgdVisitKey,int)`, `ListData(IIgdVisitKey)`, `ListPendingRegistration()`, column projection; add quarantine predicate to `ListData(IRegKey)`/`ListData(IPasienKey)`; add `TriageConceptMapDal`.
- **Dependencies:** P2S1, P1S1, P1S2.
- **Acceptance Criteria:**
  - `AssesmentDal.SelectClause()`/`Insert`/`Update` include the three new columns.
  - Quarantine predicate `RegistrationLinkStatus = 0` is applied inside `ListData(IRegKey)`/`ListData(IPasienKey)` — not per-query (AR-11).
  - `TriageConceptMapDal` exposes `ListData()` loading the full small table (read-only usage).
  - INV-M2 (concept exists) and INV-M3 (closed `TriageFieldCode` set) are enforced at generation time, not seed.
- **Review Focus:** Persistence Compliance; Quarantine Compliance (BR-05, BR-19, BR-20, AR-11).

#### P2S3 — `GenerateIgdTriageAssesmentCommand`

- **Objective:** Implement `POST api/Assesment/generateIgdTriage` per the §7.2 normative algorithm.
- **Dependencies:** P2S1, P2S2, P1S3, P1S4.
- **Acceptance Criteria:**
  - Guard validates all request fields; `NoTriage > 0`; date/time formats.
  - Idempotency: pre-check by (`IgdVisitId`, `NoTriage`) returns existing `AssesmentId` without mutation (BR-07, BR-11).
  - Resolves all ten mapping pairs (including computed `GCS_TOTAL`); any unresolved pair or unmapped concept → `ArgumentException` (AR-08, fail closed).
  - Bypasses `IGetRegService`/`IGetLayananService` (AR-10), date-window validation (AR-08); sets `RegId = PasienId = PasienName = ''`; `RegistrationLinkStatus = PendingRegistration`.
  - Persists via existing `AssesmentWriter` (owns `TransHelper.NewScope()`); never calls `Finish()`; publishes no event (AR-13, AR-14).
  - Handler tests cover: success build, idempotent return, fail-closed on missing mapping, fail-closed on Paper absent.
- **Review Focus:** Architecture Compliance; Idempotency Compliance; Fail-Closed Compliance (AR-08); Workflow Compliance (WF-01/02).

#### P2S4 — `LinkAssesmentByIgdVisitIdCommand`

- **Objective:** Implement `PATCH api/Assesment/linkIgdVisit` per §7.2.
- **Dependencies:** P2S2, P2S1.
- **Acceptance Criteria:**
  - Guard requires all six fields non-empty.
  - Selects **all** assessments by `IgdVisitId` (pending + registered) — satisfies BR-15, BR-17, idempotent (AR-15).
  - Empty list → `LinkedCount = 0` (success, nothing to link).
  - Per-assessment `TransHelper.NewScope()`; sets `RegId`, `PasienId`, `PasienName`, `LayananId`, `LayananName`, `RegistrationLinkStatus = Registered`; never touches `AssesmentState`/sections/concepts (BR-13, BR-14).
  - Partial failure persists; command throws after loop (BR-16); no event publish (AR-14).
  - No `IGetRegService`/`IGetLayananService` call (AR-10).
- **Review Focus:** Architecture Compliance; All-or-Nothing Key Population (BR-02, BR-09); Idempotency (D-09).

#### P2S5 — `ListAssesmentByIgdVisitIdQuery` + `ListPendingRegistrationAssesmentQuery` + view models

- **Objective:** Implement `GET api/Assesment/igdVisit/{igdVisitId}` and `GET api/Assesment/pendingRegistration` with `AssesmentIgdView`/`PendingRegistrationView` projections (§19.4, §19.5).
- **Dependencies:** P2S2, P2S1.
- **Acceptance Criteria:**
  - By-visit query returns every assessment (pending + registered) with `RegistrationLinkStatus`, backed by `IX_SMASS_Assesment_IgdVisitId`.
  - Pending query returns `PendingRegistration` rows oldest-first (BR-33); read-only; no delete/purge/archive action (BR-31).
  - View models include `registrationLinkStatusLabel`, `sourceLabel = "Generated From IGD Triage"` when `IgdVisitId` non-empty (D-14).
  - By-visit query is the **only** surface that can return a pending row (BR-19).
- **Review Focus:** Quarantine Compliance (BR-19); Monitoring Surface (D-15, BR-33); View-Model Compliance.

#### P2S6 — `AssesmentController` actions + `[Authorize]`

- **Objective:** Wire the three new endpoints on `AssesmentController` with `[Authorize]` (bare).
- **Dependencies:** P2S3, P2S4, P2S5.
- **Acceptance Criteria:**
  - `POST generateIgdTriage`, `PATCH linkIgdVisit`, `GET igdVisit/{igdVisitId}`, `GET pendingRegistration` routed 1:1 to their use cases.
  - `[Authorize]` present on the three new endpoints only; existing SMASS actions keep current behaviour (BR-22).
  - `JSend` responses per SMASS convention.
- **Review Focus:** Security Compliance (AR-05); API Contract Compliance.

---

### Phase 3 — BILREG Persistence & Domain

#### P3S1 — `BILRG_IgdVisitSmassTask` table

- **Objective:** Create `BILRG_IgdVisitSmassTask` + `UX_..._BusinessKey`, `IX_..._Visit`, `IX_..._Status`.
- **Dependencies:** None.
- **Acceptance Criteria:**
  - Columns match §6.1; PK `IgdVisitSmassTaskId VARCHAR(14)`.
  - `UX_BILRG_IgdVisitSmassTask_BusinessKey (IgdVisitId, NoTriage, TaskType)` enforces INV-T1 / BR-11.
  - `IX_..._Status (TaskStatus, CrtDate)` supports `ListProcessable()`.
  - No soft-delete; no audit columns (AR-16); no backfill.
- **Review Focus:** Persistence Compliance; Operational-Store Compliance (AR-03, D-07).

#### P3S2 — `IgdVisitSmassTaskModel` + enums + key

- **Objective:** Implement `IgdVisitSmassTaskModel` with `SmassTaskTypeEnum`, `SmassTaskStatusEnum`, `IIgdVisitSmassTaskKey`, and INV-T1…T8 behaviours (`CreatePending`, `MarkSucceeded`, `MarkFailed`, `Rehydrate`, `AssertCanManualRetry`).
- **Dependencies:** None (domain only).
- **Acceptance Criteria:**
  - Enum code values as §5.1 (`ToCode()` → `"GENERATE"`/`"LINK"`, `"PENDING"`/`"SUCCEEDED"`/`"FAILED"`).
  - INV-T1…T8 enforced; `LastError` truncated to 500 chars.
  - Domain tests cover state transitions (INV-T4/T5/T6/T7), `NoTriage`/`TaskType` coupling (INV-T2/T3), non-deletion (INV-T8).
- **Review Focus:** Architecture Compliance; Domain Invariant Compliance.

#### P3S3 — `IgdVisitSmassTaskDal` + `IgdVisitSmassTaskRepo`

- **Objective:** Implement `IIgdVisitSmassTaskRepo`/`IgdVisitSmassTaskRepo` + `IgdVisitSmassTaskDal` with `LoadEntity`, `FindByBusinessKey`, `ListByVisit`, `ListProcessable`, `SaveChanges` (upsert).
- **Dependencies:** P3S1, P3S2.
- **Acceptance Criteria:**
  - Auto-registered by Scrutor scan (implements `ISaveChange<>`/`ILoadEntity<,>`, precedent `EmrAntrianOutboundQueueRepo`).
  - `FindByBusinessKey` supports idempotent upsert (BR-11); `ListProcessable` returns `Failed` ordered by `CrtDate` ascending.
  - Repo round-trip test (insert/update/load) passes.
- **Review Focus:** Persistence Compliance; Repository Boundary Compliance.

---

### Phase 4 — BILREG Integration & Application

#### P4S1 — `SmassOptions` + `IgdVisitOptions`

- **Objective:** Add `SmassOptions` and `IgdVisitOptions`, registered via `.Configure<T>(...)` in `InfrastructureService.cs`.
- **Dependencies:** None.
- **Acceptance Criteria:**
  - `SmassOptions.SECTION_NAME = "Smass"`; `IgdVisitOptions.SECTION_NAME = "IgdVisit"`; keys per §10.6.
  - `IgdVisit:EnableSmassIntegration` default `false` (AR-01); `Smass:TimeoutSeconds` default `10` with range 1–120.
  - No `ValidateOnStart()` for either (misconfig must not prevent API start).
- **Review Focus:** Configuration Compliance (AR-01, AR-02, §10.6).

#### P4S2 — `ISmassTokenService`

- **Objective:** Implement token acquisition from `POST {Smass:BaseApiUrl}/Token`, cached in `IMemoryCache` (key `SmassToken`), expiry = `exp` − 60 s.
- **Dependencies:** P4S1.
- **Acceptance Criteria:**
  - Uses existing `JSend<T>`/`DeserializeOrThrow` helper pattern (precedent `UsmanGetTokenService`).
  - Returns Bearer token; caches; empty `TokenEmail`/`TokenPass` handled at call time (task Failed, no HTTP call).
  - Unit test for cache hit/miss and token parse.
- **Review Focus:** Security Compliance (AR-04, BR-27).

#### P4S3 — `ISmassAssessmentGateway` / `SmassAssessmentGateway`

- **Objective:** Implement the outbound gateway with `Generate` and `Link` operations, `SmassGatewayResult(bool Success, string? AssessmentId, string? ErrorMessage)`, catching **all** exceptions.
- **Dependencies:** P4S1, P4S2.
- **Acceptance Criteria:**
  - Port in `Bilreg.Application`, adapter in `Bilreg.Infrastructure` (precedent `IDoctorServiceGateway`); one explicit `AddScoped`.
  - Uses `IRestClientFactory` (RestSharp); no `IHttpClientFactory`/Polly.
  - Generate → `POST .../generateIgdTriage`; Link → `PATCH .../linkIgdVisit`; payloads per §8.1/§8.2; timeout from `Smass:TimeoutSeconds`.
  - No exception propagates (BR-10); missing config → `MarkFailed` without HTTP call; unique-index violation on concurrent retry → re-read then `MarkSucceeded`.
  - Gateway unit tests with mocked RestSharp client for 2xx/4xx/5xx/timeout/config-missing.
- **Review Focus:** Integration Compliance; Transaction-Boundary Compliance (BR-10, §7.1).

#### P4S4 — Triage handler hooks + response extension

- **Objective:** Modify `IgdVisitAssessTriageCmd` and `IgdVisitReAssessTriageCmd` per the §7.1 execution-ordering rule; extend `IgdVisitAssessTriageResponse` with `SmassAssessmentId` + `SmassGenerationStatus`.
- **Dependencies:** P4S3, P3S3.
- **Acceptance Criteria:**
  - Gateway call placed **after** `trans.Complete()`, outside every `TransHelper` scope, guarded by `IgdVisitOptions.EnableSmassIntegration`.
  - Task upserted (`FindByBusinessKey` ?? `CreatePending`), then `MarkSucceeded`/`MarkFailed`; exceptions never rethrow into the triage handler (BR-10).
  - Response carries `SmassAssessmentId` + `SmassGenerationStatus` (`"Disabled" | "Pending" | "Generated" | "Failed"`).
  - Handler tests: toggle-off (no task row, no HTTP), success, failure swallowed.
- **Review Focus:** Workflow Compliance (WF-01/02); Transaction-Boundary Compliance (BR-10); Toggle Compliance (AR-01).

#### P4S5 — Register handler hooks

- **Objective:** Modify `IgdVisitAssignRegisterCmd` and `IgdVisitReplaceRegisterCmd` to invoke the Link gateway after commit; `NoTriage = 0`, `TaskType = Link`; response unchanged.
- **Dependencies:** P4S3, P3S3.
- **Acceptance Criteria:**
  - Link payload built from loaded `RegModel` (`reg.RegId`, `reg.Pasien.*`, `reg.Layanan.*`) — not from `IgdVisitModel.Reg` (BR-16).
  - Failure → `Link` task Failed; registration never rolled back (BR-16); response stays `"Done"`.
  - Handler tests: success, gateway failure, toggle-off.
- **Review Focus:** Workflow Compliance (WF-03/04); Registration-Link Compliance (D-09, BR-03).

#### P4S6 — Retry + Process commands

- **Objective:** Implement `IgdVisitSmassTaskRetryCmd` and `IgdVisitSmassTaskProcessCmd` (batch variant).
- **Dependencies:** P4S3, P3S3, P4S4 (payload rebuild from immutable triage row).
- **Acceptance Criteria:**
  - `AssertCanManualRetry()` (INV-T6) gate; payload rebuilt by re-reading `BILRG_IgdVisitTriage` by (`IgdVisitId`, `NoTriage`) — no `PayloadJson` column (AR-12).
  - Generate retry re-uses `GenerateIgdTriageAssesmentCommand`; Link retry re-uses link path; `ProcessCmd` loops failed tasks.
  - Handler tests for retry success, retry-not-allowed on non-Failed, payload rebuild.
- **Review Focus:** Workflow Compliance (WF-06); Idempotency Compliance (AR-12, D-07).

#### P4S7 — List queries + worklist + DTOs

- **Objective:** Implement `IgdVisitListSmassTaskQuery`, `IgdVisitSmassWorklistQuery`, and `IgdVisitSmassTaskView` DTO.
- **Dependencies:** P3S3.
- **Acceptance Criteria:**
  - `IgdVisitSmassTaskView` fields per §19.1; sentinel `3000-01-01` mapped to `null`.
  - Worklist returns `Failed` tasks across visits oldest-first (BR-12, BR-33).
  - Query tests for projection + sentinel mapping.
- **Review Focus:** Read-Model Compliance; Monitoring Surface (BR-30, BR-33).

#### P4S8 — `IgdVisitSmassTaskController` + `[Authorize]`

- **Objective:** Wire `GET /{igdVisitId}`, `GET /worklist`, `PATCH /retry` (and `POST` process) on `IgdVisitSmassTaskController`.
- **Dependencies:** P4S6, P4S7.
- **Acceptance Criteria:**
  - Routes per §4.1; `[Authorize]` matching other IGD controllers; `JSendOk` responses.
  - No new policy/role/permission (AR-05).
- **Review Focus:** Security Compliance; API Contract Compliance.

---

### Phase 5 — Frontend

#### P5S1 — Types + query keys + service hooks

- **Objective:** Add Zod schemas (`IgdSmassTaskView`, extended `AssignTriaseResponse`, extended `TriageHistoryItem`), `queryKeys.emergency` entries, and `useIgdSmassTask`/`useRetryIgdSmassTask` hooks.
- **Dependencies:** Phase 4 (backend contracts).
- **Acceptance Criteria:**
  - Schemas added to `src/modules/Emergency/types/contract.ts`; query keys in `src/core/api/queryConfigs.ts`; hooks in `queries/EmergencyService.ts`.
  - `useIgdSmassTask(visitId)` uses TanStack Query (no polling); `useRetryIgdSmassTask` invalidates cache on settle (`onSettled`), no optimistic update.
  - No Pinia store introduced; no direct SMASS call from the client (D-02).
- **Review Focus:** UI State Compliance; Contract Compliance.

#### P5S2 — SCR-02 `TriageHistoryPanel` badge

- **Objective:** Add an inline SMASS status badge + `AssessmentId` per triage event row.
- **Dependencies:** P5S1.
- **Acceptance Criteria:**
  - `smassGenerationStatus`/`smassAssessmentId` joined client-side in a `computed` (no new endpoint, §19.3).
  - Badge reflects `Generated | Failed | Pending | Disabled`; no slot rendered when `smassGenerationStatus = "Disabled"` (IR-04).
  - Backward compatible with visits created before rollout (no task → no slot).
- **Review Focus:** UI State Compliance (IR-04); Backward Compatibility.

#### P5S3 — SCR-03 `IgdSmassIntegrationPanel` + retry

- **Objective:** Implement new `IgdSmassIntegrationPanel.vue`, mounted inside SCR-01; render task list with status/error/`AssessmentId` and `Coba Ulang` retry.
- **Dependencies:** P5S1.
- **Acceptance Criteria:**
  - Rendered for `Triaged`/`Paired`/`Terminal` modes (read-only retry allowed in `Terminal`, IR-07); hidden in `Empty`/`NewVisit`.
  - `Coba Ulang` only on `Failed` (IR-01), hidden for `Succeeded`; single-flight retry (IR-02); auto-expand + "n gagal" chip when ≥1 Failed (IR-03).
  - `AssessmentId` shown only for `Generate` tasks (IR-05/IR-06); retry outcome via `vue-sonner` toast; no delete/cancel/archive action (BR-31).
- **Review Focus:** UI State Compliance (IR-01…IR-07); Workflow Compliance (WF-06, WF-08).

---

### Phase 6 — End-to-End Verification & Rollout

#### P6S1 — End-to-end scenario verification

- **Objective:** Verify the six workflow capabilities (WF-01…WF-08) end-to-end in staging, including failure/retry/pending paths.
- **Dependencies:** Phase 2, Phase 4 (all backend slices).
- **Acceptance Criteria:**
  - First triage → pending assessment created (BR-01); re-triage → second snapshot (BR-07).
  - Assign/Replace register → link populates keys all-or-nothing (BR-02, BR-09, BR-17).
  - Failed generation recorded as task row, recoverable via retry; pending rows unreachable from RegId/PasienId surfaces (BR-05, BR-19, BR-20).
  - Void unchanged (BR-18, BR-32); no assessment ever deleted (BR-31).
- **Review Focus:** Workflow Compliance; Critical Invariants (§23).

#### P6S2 — Rollout ordering + rollback verification

- **Objective:** Verify BR-23 rollout order and rollback per §10.2/§10.3.
- **Dependencies:** P6S1.
- **Acceptance Criteria:**
  - DB → SMASS app → legacy regression check → toggle enable → monitor, in order.
  - Toggle off stops new attempts immediately; existing rows/assessments untouched (BR-22); schema rollback not required.
  - Legacy assessment behaviour (create/finish/catalog/OFTA) unchanged with toggle off (BR-23 step 3).
- **Review Focus:** Rollout Compliance (BR-21, BR-22, BR-23); Rollback Safety.

---

## 6. Dependency Graph

```text
P1S1 ─┬─ P2S2 ─┬─ P2S3 ── P2S6
P1S2 ─┘        ├─ P2S4 ── P2S6
P1S3 ─┬─ P1S4 ─┴─ P2S5 ── P2S6
      └─ P2S3
P2S1 ─┬─ P2S2
      └─ P2S4, P2S5

P3S1 ─ P3S3 ─┬─ P4S4 ── P4S8
P3S2 ─┘       ├─ P4S5 ── P4S8
P4S1 ─ P4S2 ──┴─ P4S6 ── P4S8
P4S3 ─┬─ P4S4
      ├─ P4S5
      └─ P4S6
P4S7 ── P4S8

P5S1 ── P5S2
     └─ P5S3

Phase 2 + Phase 4 ── P6S1 ── P6S2
Phase 4 ── Phase 5
```

---

## 7. Progress Tracker

| Slice | Status | Implementation History | Review History | Remediation History |
|---|---|---|---|---|
| P1S1 | PLANNED | — | — | — |
| P1S2 | PLANNED | — | — | — |
| P1S3 | PLANNED | — | — | — |
| P1S4 | PLANNED | — | — | — |
| P2S1 | PLANNED | — | — | — |
| P2S2 | PLANNED | — | — | — |
| P2S3 | PLANNED | — | — | — |
| P2S4 | PLANNED | — | — | — |
| P2S5 | PLANNED | — | — | — |
| P2S6 | PLANNED | — | — | — |
| P3S1 | PLANNED | — | — | — |
| P3S2 | PLANNED | — | — | — |
| P3S3 | PLANNED | — | — | — |
| P4S1 | PLANNED | — | — | — |
| P4S2 | PLANNED | — | — | — |
| P4S3 | PLANNED | — | — | — |
| P4S4 | PLANNED | — | — | — |
| P4S5 | PLANNED | — | — | — |
| P4S6 | PLANNED | — | — | — |
| P4S7 | PLANNED | — | — | — |
| P4S8 | PLANNED | — | — | — |
| P5S1 | PLANNED | — | — | — |
| P5S2 | PLANNED | — | — | — |
| P5S3 | PLANNED | — | — | — |
| P6S1 | PLANNED | — | — | — |
| P6S2 | PLANNED | — | — | — |

Lifecycle: `PLANNED → IN IMPLEMENTATION → IMPLEMENTED → IN REVIEW → GO` (or `NO-GO → REMEDIATION → IN REVIEW → GO`).

---

## 8. Cross-Cutting Constraints (binding, from architecture §23)

- **Backend (BILREG):** gateway call after `trans.Complete()`, never inside a `TransHelper` scope; catch every exception, never rethrow (BR-10); do not modify `IgdVisitVoidCmd` (BR-18); use `IRestClientFactory`; do not add SMASS columns to `BILRG_IgdVisitTriage`/`BILRG_IgdVisit`.
- **Backend (SMASS):** MediatR one-file pattern; persist only via `AssesmentWriter`; no `IGetRegService`/`IGetLayananService` (AR-10); no event publish (AR-14); do not modify `CreateFixAssesmentCommand` or re-enable `AssesmentValidator`; additive DDL only; `[Authorize]` on the three new endpoints only.
- **Frontend:** no direct SMASS call (D-02); extend existing schemas/hooks/query-keys; SCR-03 presentational but owns `useIgdSmassTask` (precedent `TriageHistoryPanel.vue`); no Pinia store.
- **Critical invariants:** commit before outbound call; (`IgdVisitId`, `NoTriage`) → exactly one assessment; all-or-nothing keys; `AggStateEnum` never written; pending unreachable from RegId/PasienId surfaces; assessments never deleted/purged/archived/unlinked.

---

## 9. Prohibited Shortcuts (from architecture §23)

- Reusing `CreateFixAssesmentCommand` with empty keys.
- Gateway call inside the existing `TransHelper` scope.
- A `PayloadJson` column replaying stale snapshots.
- Filtering pending assessments per-query instead of at the DAL list level.
- Hard-coding the ATS→SMASS mapping in the gateway or the Paper.
- Frontend dual dispatch to SMASS.
- In-request retry or an automatic retry worker.
- Backfilling `IgdVisitId` into historical assessments.

---

## 10. Out of Scope (explicitly deferred, not planned here)

- Automatic retry worker / background worker (D-07).
- Event fan-out from the new SMASS commands (AR-14, R-03).
- SMASS clinical display surface (R-06 — fields exposed as API view-models only).
- Closing pending-assessment completion-state/editability (R-02) — requires a follow-up decision.
- Retention governance for never-registered assessments (D-15, R-08).

---

*End of implementation plan. Planning authority: `docs/contexts/igd/igd-triage-abc-smass-architecture.md`.*
