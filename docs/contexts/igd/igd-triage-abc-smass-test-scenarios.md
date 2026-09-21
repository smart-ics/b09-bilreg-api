---
Title: IGD Triage ABC to SMASS Test Package
Code: IGD-TRIAGE-ABC-SMASS
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-09-21
Status: READY-FOR-EXECUTION
Source Plan: b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-implementation-plan.md (COMPLETED)
Source Architecture: b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-architecture.md (V2.0)
Source Feature: b09-bilreg-api/docs/contexts/igd/igd-01-context.md (IGD Visit)
---

# IGD Triage ABC → SMASS — Test Package

This package validates the finished capability **as a whole**: every IGD Visit triage
(first triage and re-triage) produces one immutable structured assessment in SMASS,
created before administrative registration exists, and linked to the registration when
the visit is registered. It also validates BILREG-side status display, manual retry and
monitoring.

It describes **what** to test and the expected observable outcome. It does not say who
must run it. Any qualified person (Programmer, Trainer, Reviewer, Customer, operator)
can execute it.

---

# 1. How to use this document

1. Read sections 2–5 to prepare the environment and test data.
2. Execute the test cases in section 7 in the recommended order (section 6).
3. For every case, record the observed result and evidence. A case is only complete
   when a result has been recorded through the Tester role as `PASS`, `FAIL`, or
   `BLOCKED`.
4. A `FAIL` must be recorded with enough evidence to reproduce it. Defects do not go
   back to the programmer directly: they enter the SDLC through Issue Creation.

Expected-result wording that includes exact field names, status words and labels is
intentional — record what you actually observe, verbatim.

---

# 2. Scope of testing

## In scope

- SMASS assessment generation from IGD triage (first triage and re-triage), including
  concept mapping, immutability and idempotency.
- Administrative registration linking on `AssignRegister` and `ReplaceRegister`.
- Quarantine of pre-registration assessments from registration/patient-based surfaces.
- Error handling and fail-closed behaviour when SMASS or the configuration is
  unavailable or incomplete.
- BILREG operational task store, status response fields, manual retry, batch process
  and monitoring endpoints.
- Web client: triage history SMASS status (SCR-02) and SMASS Integration Panel with
  manual retry (SCR-03).
- Regression safety of existing IGD and existing SMASS behaviour.

## Out of scope (do not report findings here)

- The SMASS-first 30-item form and any reverse-direction flow.
- SMASS clinical display screens (none exist in this workspace; SMASS exposes API
  fields only).
- Historical backfill of `IgdVisitId` into older assessments.
- Automatic background retry workers, outbox or asynchronous synchronisation — these
  are deliberately not implemented.
- Any new authentication scheme, role or permission.

## Known wording differences (not defects)

- The triage response field `smassGenerationStatus` is emitted on the wire in
  **PascalCase**: `"Disabled" | "Pending" | "Generated" | "Failed"`. Architecture §6.5
  prints these uppercase (`"DISABLED"` etc.); the implemented server and web schemas
  use PascalCase. Test against the **implemented** PascalCase values.
- In the triage history panel (SCR-02) the state `Disabled` is represented by **no
  SMASS slot at all** (no badge), because when integration is off no Generate task
  exists. Do not expect a "Disabled" badge.

---

# 3. Test environment

| Item | Value / note |
|---|---|
| BILREG API base URL | `________________` (the IGD backend under test) |
| SMASS API base URL | `________________` (must match `Smass:BaseApiUrl` in BILREG) |
| Web client URL | `________________` (`/app/igd/triase_igd`) |
| BILREG database | `HOSPITAL_HPL` (current configuration) |
| SMASS database | `HOSPITAL_PKL` (current configuration) |
| BILREG auth | A valid user token for the IGD roles used by the existing screens |
| SMASS auth | `POST {Smass:BaseApiUrl}/Token` with body `{ "email": "<Smass:TokenEmail>", "pass": "<Smass:TokenPass>" }` returns a raw JWT |
| BILREG config keys | `Smass:BaseApiUrl`, `Smass:TokenEmail`, `Smass:TokenPass`, `Smass:TimeoutSeconds` (default 10), `IgdVisit:EnableSmassIntegration` (default `false`), `IgdVisit:SmassLayananId`, `IgdVisit:SmassTriagePaperId` |
| SMASS config keys | `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` |

Access needed to execute this package:

- Ability to call the BILREG API and the SMASS API (a REST client or the web client).
- Ability to read the BILREG and SMASS databases (queries in section 7).
- Ability to change BILREG configuration values listed above and restart/reconfigure the
  BILREG API (only for the configuration cases, TC-IGD-SMASS-002, -003, -040).
- At least two available IGD beds and one valid `RegId` that exists in Admisi.

## Deployment precondition

The following must already be deployed before testing starts (architecture §8.6):

1. BILREG database script for `BILRG_IgdVisitSmassTask`, plus the `SMASS_Assesment`
   alteration and `SMASS_TriageConceptMap`.
2. SMASS application (four new endpoints).
3. SMASS IGD Triage Paper seed and the ATS→SMASS mapping seed.
4. The SMASS `PaperId` mirrored into `IgdVisit:SmassTriagePaperId`, and the configured
   `IgdVisit:SmassLayananId`.
5. `IgdVisit:EnableSmassIntegration = true` for the main (happy path) cases.

---

# 4. Test data

Prepare the following before execution. Record any ids you create; they are evidence.

| Ref | Data | Value / how to obtain |
|---|---|---|
| DATA-01 | IGD bed 1 and bed 2 | `GET /api/BedIgd/available`; both must be `Active` |
| DATA-02 | Valid dokter PPA id | Existing `DokterId` used by the IGD screens |
| DATA-03 | Valid `RegId` in Admisi | An existing registration that can be linked to a visit |
| DATA-04 | BILREG user id for `UserId` | Existing IGD operator id, e.g. `USR001` |
| DATA-05 | Visit V1 — first triage only | `POST /api/IgdVisit` then `POST /api/IgdVisit/{id}/triage` |
| DATA-06 | Visit V2 — first triage + re-triage | Same, then `POST /api/IgdVisit/{id}/re-triage` |
| DATA-07 | Visit V3 — manual override black | First triage with `isManualOverrideBlack: true` and a non-empty `overrideReason` |
| DATA-08 | Visit V4 — registration | A visit with at least one triage, then `PATCH /api/IgdVisit/{id}/register` |
| DATA-09 | Visit V5 — toggle-off control | Any visit triaged while `IgdVisit:EnableSmassIntegration = false` |
| DATA-10 | Visit V6 — fail-closed control | A visit triaged while a configuration value is invalid or a mapping row is removed |
| DATA-11 | SMASS assessment id prefix | Generated assessments are new SMASS documents; note the `AssesmentId` returned |

## Data-05/06/07 triage body example

```json
{
  "airwaysScore": 0,
  "breathingScore": 2,
  "bloodCirculationScore": 3,
  "gcsEyeScore": 4,
  "gcsMotorScore": 6,
  "gcsVoiceScore": 5,
  "isManualOverrideBlack": false,
  "overrideReason": "",
  "notes": "SMASS test package",
  "userId": "USR001"
}
```

- With the values above, the computed GCS total is **15**.
- `TriageLevel` and `TriageColor` are computed by the ATS engine. Record what the
  response actually returns; the test then verifies that SMASS received the **same**
  level, colour and scores.
- For DATA-07 set `isManualOverrideBlack: true` and `overrideReason` non-empty; the ATS
  engine then forces `TriageColor = BLACK`.

## Useful verification queries

SMASS database (`HOSPITAL_PKL`):

```sql
-- one snapshot per visit/triage?
SELECT AssesmentId, IgdVisitId, NoTriage, RegistrationLinkStatus, RegId, PasienId, LayananId
FROM SMASS_Assesment
WHERE IgdVisitId = '<IGV...>'
ORDER BY NoTriage;

-- mapping master data present?
SELECT TriageFieldCode, TriageValue, ConceptId
FROM SMASS_TriageConceptMap
ORDER BY TriageFieldCode, TriageValue;
```

BILREG database (`HOSPITAL_HPL`):

```sql
SELECT IgdVisitSmassTaskId, IgdVisitId, NoTriage, TaskType, TaskStatus,
       AssessmentId, RetryCount, LastRetryDate, ProcessedDate, LastError, CrtDate
FROM BILRG_IgdVisitSmassTask
WHERE IgdVisitId = '<IGV...>'
ORDER BY TaskType, NoTriage;
```

Interpretation: `TaskType` `0 = GENERATE`, `1 = LINK`; `TaskStatus` `0 = PENDING`,
`1 = SUCCEEDED`, `2 = FAILED`. The sentinel date `3000-01-01` means "not set".

---

# 5. Coverage summary

| Area | Test cases |
|---|---|
| A. Readiness and configuration | TC-IGD-SMASS-001 … 003 |
| B. Assessment generation | TC-IGD-SMASS-010 … 014 |
| C. Registration linking | TC-IGD-SMASS-020 … 023 |
| D. Quarantine and read surfaces | TC-IGD-SMASS-030 … 033 |
| E. Error handling and fail-closed | TC-IGD-SMASS-040 … 045 |
| F. Manual retry and monitoring | TC-IGD-SMASS-050 … 055 |
| G. Web client (SCR-02, SCR-03) | TC-IGD-SMASS-060 … 065 |
| H. Regression and non-goals | TC-IGD-SMASS-070 … 075 |

Expected outcome types covered: happy path, business rules, validation rules, error
handling, and regression risk.

---

# 6. Recommended execution sequence

1. **A** — confirm the deployment and configuration are correct.
2. **B** — prove generation works (happy path), then re-triage, then edge/concurrency.
3. **C** — prove linking works and is idempotent.
4. **D** — prove quarantine and read surfaces.
5. **E** — prove fail-closed and error handling (some of these need a configuration
   change, so do them together with a single reconfigure/restore).
6. **F** — prove retry and monitoring (reuse a visit left `FAILED` in step 5).
7. **G** — prove the web client behaviour.
8. **H** — prove regression safety and non-goals.

Do not start G before B–F, because the panel and badges depend on real task rows.

---

# 7. Test cases

## A. Readiness and configuration

### TC-IGD-SMASS-001 — Schema and master data are present

**Objective:** Confirm the database and master data the feature depends on exist before
behaviour is tested.

**Preconditions:** Database scripts and SMASS seeds have been deployed.

**Steps:**

1. In the SMASS database, confirm `SMASS_Assesment` has columns `IgdVisitId`,
   `NoTriage`, `RegistrationLinkStatus`.
2. Confirm the filtered unique constraint `UX_SMASS_Assesment_IgdVisitTriage` on
   (`IgdVisitId`, `NoTriage`) exists with the filter `IgdVisitId <> ''`.
3. Confirm the index `IX_SMASS_Assesment_IgdVisitId` exists.
4. Confirm table `SMASS_TriageConceptMap` exists with primary key
   (`TriageFieldCode`, `TriageValue`) and an index on `ConceptId`.
5. Run the mapping query from section 4 and confirm one row exists for every value in
   the closed set: `AIRWAYS` 0–2, `BREATHING` 0–5, `CIRCULATION` 0–4, `GCS_EYE` 1–4,
   `GCS_MOTOR` 1–6, `GCS_VOICE` 1–5, `GCS_TOTAL` 3–15, `ATS_LEVEL` `ATS1`–`ATS5`,
   `TRIAGE_COLOR` `RED`/`YELLOW`/`GREEN`/`BLACK`, `MANUAL_OVERRIDE_BLACK`
   `true`/`false`.
6. Confirm the IGD Triage Paper exists, its sections are `CustomSection` only, and the
   mapping `ConceptId` values are attached to that Paper via its custom section
   concepts. Record the Paper id.
7. Confirm `IgdVisit:SmassTriagePaperId` equals the seeded Paper id.
8. In the BILREG database, confirm table `BILRG_IgdVisitSmassTask` exists with the
   unique constraint `UX_BILRG_IgdVisitSmassTask_BusinessKey`
   (`IgdVisitId`, `NoTriage`, `TaskType`) and indexes
   `IX_BILRG_IgdVisitSmassTask_Visit` and `IX_BILRG_IgdVisitSmassTask_Status`.
9. Run the seed scripts a second time and re-run the mapping count.

**Expected result:**

- All objects exist with the stated keys/indexes.
- Every closed-set mapping value has exactly one row.
- Re-running the seeds changes no row count (idempotent).

---

### TC-IGD-SMASS-002 — Toggle off produces no task and no call

**Objective:** With integration disabled, triage behaves exactly as it did before the
feature and touches SMASS not at all.

**Preconditions:** `IgdVisit:EnableSmassIntegration = false`.

**Test data:** DATA-09.

**Steps:**

1. Create a visit and submit a first triage.
2. Record the triage response.
3. Query `BILRG_IgdVisitSmassTask` for the visit id.
4. Query `SMASS_Assesment` for the visit id.
5. Open the visit in the web client triage workspace.

**Expected result:**

- The triage response is successful and its `smassGenerationStatus` is `"Disabled"` and
  `smassAssessmentId` is empty.
- No `BILRG_IgdVisitSmassTask` row exists for the visit.
- No SMASS assessment exists for the visit.
- No SMASS HTTP call is made (verify from SMASS request logs if available).
- The triage history row shows **no** SMASS badge, and no SMASS task appears in the
  integration panel.

---

### TC-IGD-SMASS-003 — Missing configuration fails closed, does not block startup

**Objective:** A half-configured deployment still serves IGD triage; the integration
degrades to a failed task instead of an exception.

**Preconditions:** `IgdVisit:EnableSmassIntegration = true`, but one of
`Smass:BaseApiUrl`, `Smass:TokenEmail`, `Smass:TokenPass`, `IgdVisit:SmassLayananId`,
`IgdVisit:SmassTriagePaperId` is empty (test one at a time; restore between runs).

**Test data:** DATA-10 (one visit per empty value, or reuse one visit per run).

**Steps:**

1. Restart/reconfigure BILREG with one configuration value empty.
2. Confirm the BILREG API starts and serves existing IGD endpoints.
3. Create a visit and submit a triage.
4. Query `BILRG_IgdVisitSmassTask` for the visit id.
5. Check the SMASS request log to see whether an HTTP call was attempted.

**Expected result:**

- BILREG starts normally (there is no startup validation failure).
- The triage succeeds and stays committed.
- Exactly one `GENERATE` task exists for the visit with `TaskStatus = FAILED` and a
  non-empty `LastError` explaining the missing configuration.
- **No** HTTP call reaches SMASS (fail closed).

---

## B. Assessment generation

### TC-IGD-SMASS-010 — First triage generates one pending assessment

**Objective:** A successful first triage synchronously creates exactly one immutable
SMASS assessment before registration exists, built from the dedicated IGD Triage Paper.

**Preconditions:** TC-IGD-SMASS-001 passes; `IgdVisit:EnableSmassIntegration = true`;
SMASS reachable.

**Test data:** DATA-05 with the section-4 triage body.

**Steps:**

1. Create a visit; record `IgdVisitId`.
2. Submit the first triage with section-4 body.
3. Record the triage response (`TriageLevel`, `TriageColor`, `NoTriage`).
4. Query `BILRG_IgdVisitSmassTask` for the visit.
5. Query `SMASS_Assesment` for the visit.
6. Open the by-visit SMASS read: `GET {Smass}/api/Assesment/igdVisit/{igdVisitId}`
   (authenticated).

**Expected result:**

- Triage response: successful; `noTriage` = 1; `smassGenerationStatus` = `"Generated"`;
  `smassAssessmentId` non-empty.
- BILREG: one `GENERATE` task, `NoTriage = 1`, `TaskStatus = SUCCEEDED`,
  `AssessmentId` equal to the returned `smassAssessmentId`, `LastError` empty.
- SMASS: exactly one `SMASS_Assesment` row for the visit with `NoTriage = 1`,
  `RegistrationLinkStatus = 1` (Pending Registration), empty `RegId`, `PasienId`,
  `PasienName`, and `LayananId` equal to `IgdVisit:SmassLayananId`.
- Its clinical content contains the ten mapped concepts in the IGD Triage Paper section,
  ordered by the mapping order, including `GCS_TOTAL` = 15 and the same ATS level and
  triage colour the ATS engine returned.
- The by-visit read returns the assessment with `registrationLinkStatusLabel` =
  `"Pending Registration"` and `sourceLabel` = `"Generated From IGD Triage"`.
- The assessment completion state is `Drafting`.

---

### TC-IGD-SMASS-011 — Re-triage adds a second immutable snapshot

**Objective:** Every triage event produces a new snapshot; the previous snapshot is not
modified.

**Preconditions:** TC-IGD-SMASS-010 passes.

**Test data:** DATA-06. Use a visibly different score set for the re-triage (for example
`gcsEyeScore: 3`) so the two snapshots can be distinguished.

**Steps:**

1. On a visit already triaged once, note the first `AssesmentId` and its concept values.
2. Submit `POST /api/IgdVisit/{id}/re-triage`.
3. Query `SMASS_Assesment` for the visit ordered by `NoTriage`.
4. Compare the concept values of snapshot 1 and snapshot 2.
5. Query `BILRG_IgdVisitSmassTask` for the visit.
6. Open `GET /api/IgdVisit/{id}/triage-history`.

**Expected result:**

- Exactly two `GENERATE` tasks: `NoTriage = 1` and `NoTriage = 2`, both `SUCCEEDED`,
  each with its own `AssessmentId`.
- SMASS has exactly two assessments for the visit; snapshot 1 is byte-identical to what
  it was before the re-triage; snapshot 2 reflects the new scores.
- Triage history contains both events in order (newest first); the older event's values
  are unchanged.

---

### TC-IGD-SMASS-012 — Manual override Black is mapped correctly

**Objective:** A manual override (not an ATS score) is carried through as the `BLACK`
colour.

**Preconditions:** TC-IGD-SMASS-001 passes; integration enabled.

**Test data:** DATA-07 (override black, `overrideReason` non-empty).

**Steps:**

1. Submit a first triage with `isManualOverrideBlack: true`.
2. Record the returned `TriageColor`.
3. Query the generated assessment's mapped concepts.

**Expected result:**

- `TriageColor` is `BLACK`.
- The mapped `TRIAGE_COLOR` concept value is `BLACK`, and
  `MANUAL_OVERRIDE_BLACK` concept value is `true`.
- The `GENERATE` task is `SUCCEEDED`.

---

### TC-IGD-SMASS-013 — Idempotency of generation

**Objective:** Repeating generation for the same (`IgdVisitId`, `NoTriage`) does not
create a second assessment or a duplicate task.

**Preconditions:** TC-IGD-SMASS-010 passes.

**Steps:**

1. Note the `smassAssessmentId` returned for a visit/triage.
2. Re-submit the generation call directly:
   `POST {Smass}/api/Assesment/generateIgdTriage` with the same
   `IgdVisitId`/`NoTriage` and payload (use the SMASS service token).
3. Query `SMASS_Assesment` for that visit/triage.
4. Query `BILRG_IgdVisitSmassTask` for that visit.

**Expected result:**

- The second call returns the **same** `AssesmentId`; no new row is inserted.
- Exactly one `SMASS_Assesment` row exists for (`IgdVisitId`, `NoTriage`).
- Exactly one `GENERATE` task exists for that key.

---

### TC-IGD-SMASS-014 — Concurrent duplicate generation creates one snapshot

**Objective:** Two near-simultaneous attempts for the same key cannot create two
assessments.

**Preconditions:** TC-IGD-SMASS-010 passes; a REST client able to fire two requests in
parallel.

**Steps:**

1. Prepare two identical `generateIgdTriage` calls for the same
   `IgdVisitId`/`NoTriage`/payload.
2. Send both as close together as possible.
3. Inspect both responses.
4. Query `SMASS_Assesment` for the key.

**Expected result:**

- Exactly one assessment row exists for the key.
- Both responses are successful and refer to the same `AssesmentId` (a unique-constraint
  collision is re-read and treated as success).
- No duplicate `GENERATE` task exists.

---

## C. Registration linking

### TC-IGD-SMASS-020 — AssignRegister links all pending assessments

**Objective:** Linking a registration populates all administrative keys on every
assessment of the visit and marks it `Registered`.

**Preconditions:** DATA-08 visit has at least one generated assessment
(`RegistrationLinkStatus = PendingRegistration`); a valid `RegId` exists.

**Steps:**

1. Note the visit's assessment count and ids before linking.
2. Link the registration: `PATCH /api/IgdVisit/{id}/register`
   with body `{ "regId": "<RegId>", "userId": "USR001" }`.
3. Query `SMASS_Assesment` for the visit.
4. Query `BILRG_IgdVisitSmassTask` for the visit.

**Expected result:**

- The register call succeeds and the visit shows the `RegId`.
- Every assessment of the visit now has `RegistrationLinkStatus = Registered`,
  `RegId`, `PasienId`, `PasienName`, `LayananId`, `LayananName` populated with the
  values from the Admisi registration.
- `AssesmentState` and all clinical content are unchanged.
- Exactly one `LINK` task exists (`NoTriage = 0`), `TaskStatus = SUCCEEDED`.

---

### TC-IGD-SMASS-021 — ReplaceRegister replaces latest idempotently

**Objective:** Replace-registration re-applies the latest administrative values to every
assessment, repeatably, without duplicates.

**Preconditions:** TC-IGD-SMASS-020 passes.

**Steps:**

1. Link a different registration to the same visit (replace variant of
   `PATCH /api/IgdVisit/{id}/register`).
2. Query `SMASS_Assesment` for the visit.
3. Repeat the same replace call once more.
4. Query `BILRG_IgdVisitSmassTask` for the visit.

**Expected result:**

- Every assessment carries the latest `RegId`/`PasienId`/`PasienName`/`LayananId`/
  `LayananName`; the earlier values are overwritten.
- Repeating the call leaves the same result (idempotent) with no extra rows and no error.
- Still exactly one `LINK` task per visit; its status is terminal `SUCCEEDED` and is not
  transitioned again.

---

### TC-IGD-SMASS-022 — Linking a visit with no assessment is a no-op success

**Objective:** Linking before any assessment exists is harmless.

**Preconditions:** A visit with no triage / no SMASS assessment, but that can be
registered; integration enabled.

**Steps:**

1. `PATCH /api/IgdVisit/{id}/register` for the visit.
2. Query `BILRG_IgdVisitSmassTask` for the visit.
3. Call the SMASS link surface directly, if reachable, and inspect the result.

**Expected result:**

- The register call succeeds.
- The link operation reports `linkedCount = 0` and an empty list of assessment ids, with
  no error.
- One `LINK` task may exist and is `SUCCEEDED`.

---

### TC-IGD-SMASS-023 — Linking never mutates clinical content or completion state

**Objective:** Prove the only sanctioned post-creation change is the administrative keys
and link status.

**Preconditions:** TC-IGD-SMASS-020 passes.

**Steps:**

1. Before linking, capture the assessment's sections, concepts and `AssesmentState`.
2. Link the registration.
3. Re-read the assessment's sections, concepts and state.

**Expected result:**

- Sections and concepts are identical before and after linking.
- `AssesmentState` remains `Drafting` (unchanged).
- No completion ("Finish") transition occurs, and no assessment-created event is
  produced for the linked assessment.

---

## D. Quarantine and read surfaces

### TC-IGD-SMASS-030 — Pending assessment is hidden from RegId/PasienId surfaces

**Objective:** Before registration, a pre-registration assessment must not leak into any
surface that is keyed by registration or patient.

**Preconditions:** A visit with a generated assessment in `PendingRegistration` state.

**Steps:**

1. Query the SMASS catalog for an arbitrary registration:
   `GET {Smass}/api/Assesment/Catalog/{regId}`.
2. Query the by-registration surface for that registration.
3. Query the by-patient surface for the visitor's patient id if one exists.
4. Search the returned data for the pending `AssesmentId`.

**Expected result:**

- The pending assessment does **not** appear in any catalog, by-registration, by-patient
  or OFTA/report response.
- The surfaces return their normal content and no error.
- The quarantine holds because it is applied at the data-access level, not per query.

---

### TC-IGD-SMASS-031 — By-visit surface returns pending and registered

**Objective:** The by-visit surface is the only place a pending assessment is visible.

**Preconditions:** At least one visit with a pending assessment, and one with a linked
assessment.

**Steps:**

1. `GET {Smass}/api/Assesment/igdVisit/{igdVisitId}` for the pending visit.
2. `GET {Smass}/api/Assesment/igdVisit/{igdVisitId}` for the linked visit.
3. Inspect `registrationLinkStatus`, `registrationLinkStatusLabel` and `sourceLabel` on
   each row.

**Expected result:**

- Pending visit: the assessment is returned with `registrationLinkStatus` = `1` and
  `registrationLinkStatusLabel` = `"Pending Registration"`.
- Linked visit: the assessment is returned with `registrationLinkStatus` = `0` and
  `registrationLinkStatusLabel` = `"Registered"`.
- `sourceLabel` is `"Generated From IGD Triage"` for assessments with a non-empty
  `igdVisitId`, and empty otherwise.
- Rows are ordered by `NoTriage`.

---

### TC-IGD-SMASS-032 — Pending-registration monitoring

**Objective:** Operators can see never-registered assessments, oldest first.

**Preconditions:** At least two pending assessments with different creation dates.

**Steps:**

1. `GET {Smass}/api/Assesment/pendingRegistration` (authenticated).
2. Inspect the ordering and fields.
3. Attempt to find any delete, purge or archive action on the surface.

**Expected result:**

- Only `PendingRegistration` assessments are returned.
- They are ordered **oldest first** (by creation date).
- Each row exposes at least `assesmentId`, `igdVisitId`, `noTriage`, `assesmentDate`,
  `createDate`, `paperId`, `userrId`.
- No delete/purge/archive action exists anywhere on the surface.

---

### TC-IGD-SMASS-033 — Legacy assessments and rows are untouched

**Objective:** Additive change with no backfill; existing data behaves exactly as before.

**Preconditions:** Pre-existing assessments created before this feature.

**Steps:**

1. Pick a pre-existing assessment and note its `IgdVisitId`, `NoTriage`,
   `RegistrationLinkStatus`, and its appearance on the catalog/by-registration surface.
2. Confirm it still appears where it did before.
3. Confirm its `IgdVisitId` is empty and `NoTriage` is 0.
4. Run the SMASS schema/default check on a sample of old rows.

**Expected result:**

- Pre-existing assessments are unchanged.
- Pre-existing rows default to `RegistrationLinkStatus = Registered` and empty
  `IgdVisitId`/`NoTriage`; no `IgdVisitId` is backfilled.
- Catalog, by-registration, by-patient and OFTA/report results for these rows are as
  before the feature.

---

## E. Error handling and fail-closed

### TC-IGD-SMASS-040 — Missing mapping fails closed without a partial snapshot

**Objective:** An incomplete ATS→SMASS mapping must fail the whole generation, never
create a partial clinical document, and never fail the triage.

**Preconditions:** Integration enabled; SMASS reachable. Remove (or temporarily
de-activate) one mapping row, for example the `GCS_TOTAL` row for the score that the
triage will produce.

**Test data:** DATA-10.

**Steps:**

1. Remove one required mapping row from `SMASS_TriageConceptMap`.
2. Submit a first triage that requires that value.
3. Query `SMASS_Assesment` for the visit.
4. Query `BILRG_IgdVisitSmassTask` for the visit.
5. Restore the mapping row.

**Expected result:**

- The triage call succeeds and the triage record is committed.
- **No** SMASS assessment is created for the visit (no partial snapshot).
- The `GENERATE` task is `FAILED` with a non-empty `LastError`.

---

### TC-IGD-SMASS-041 — SMASS unavailable: triage stays committed, task fails

**Objective:** A transport failure, timeout or server error is recorded, not propagated.

**Preconditions:** Integration enabled; you can make SMASS unreachable (stop the SMASS
API, block the URL, or point `Smass:BaseApiUrl` at a dead port).

**Steps:**

1. Make SMASS unreachable (or set an unroutable base URL and reconfigure BILREG).
2. Submit a triage.
3. Observe the triage response time and content.
4. Query `BILRG_IgdVisitSmassTask` for the visit.
5. Restore SMASS.

**Expected result:**

- The triage response is successful and the visit/triage data is committed.
- The call does not exceed roughly `Smass:TimeoutSeconds` per attempt.
- Exactly one `GENERATE` task exists, `TaskStatus = FAILED`, with a non-empty
  `LastError` describing the failure.
- No exception surfaces to the triage caller.

---

### TC-IGD-SMASS-042 — SMASS rejects the call (4xx): task fails, triage stays

**Objective:** A business rejection from SMASS does not roll back triage.

**Preconditions:** Integration enabled; SMASS reachable. Cause a 4xx, e.g. submit a
triage timestamp that the generation guard rejects, or use a bad Paper id via
configuration.

**Steps:**

1. Cause SMASS to return 4xx for the generation call.
2. Submit the triage.
3. Query `BILRG_IgdVisitSmassTask` and `SMASS_Assesment`.

**Expected result:**

- Triage succeeds and is committed.
- No SMASS assessment is created.
- One `GENERATE` task is `FAILED` with the SMASS error message recorded (truncated at
  500 characters if longer).

---

### TC-IGD-SMASS-043 — Paper / concept mismatch fails closed

**Objective:** If the configured Paper is missing, or a mapped concept is not attached
to the Paper, generation fails cleanly.

**Preconditions:** Integration enabled. Temporarily set `IgdVisit:SmassTriagePaperId` to
a non-existent Paper, then (separately) detach one mapped concept from the Paper's
custom section.

**Steps:**

1. Point `SmassTriagePaperId` at a non-existent Paper; submit a triage; inspect results.
2. Restore the Paper id; detach one mapped concept from the Paper; submit a triage.
3. Inspect both outcomes; restore.

**Expected result:**

- In both runs, no partial assessment is persisted.
- The triage succeeds and stays committed.
- The `GENERATE` task is `FAILED` with a descriptive error.

---

### TC-IGD-SMASS-044 — Authorization on the new surfaces

**Objective:** New endpoints require authentication; existing endpoints keep their
current behaviour.

**Preconditions:** Integration enabled.

**Steps:**

1. Call `POST {Smass}/api/Assesment/generateIgdTriage` with no token, then with a valid
   token.
2. Repeat for `PATCH {Smass}/api/Assesment/linkIgdVisit`,
   `GET {Smass}/api/Assesment/igdVisit/{id}` and
   `GET {Smass}/api/Assesment/pendingRegistration`.
3. Call an existing SMASS action that had no authorization before and confirm it still
   behaves as before.
4. Call the BILREG task endpoints `GET /api/IgdVisitSmassTask/{igdVisitId}`,
   `GET /api/IgdVisitSmassTask/worklist`, `PATCH /api/IgdVisitSmassTask/retry`,
   `POST /api/IgdVisitSmassTask/process` with and without a valid BILREG token.

**Expected result:**

- All four new SMASS actions reject a missing/invalid token (401/403) and accept a valid
  token.
- Existing SMASS actions keep their prior authentication behaviour.
- All four BILREG task endpoints require a valid BILREG token, like the other IGD
  controllers.

---

### TC-IGD-SMASS-045 — Link failure does not roll back registration

**Objective:** A failed link leaves the registration committed and recoverable.

**Preconditions:** Integration enabled; make the SMASS link call fail (SMASS down or a
bad configuration value).

**Steps:**

1. Make the link operation fail.
2. Register a visit that has a pending assessment.
3. Confirm the visit's administrative state and `RegId`.
4. Query `SMASS_Assesment` for the visit.
5. Query `BILRG_IgdVisitSmassTask` for the visit.

**Expected result:**

- The registration succeeds and stays committed.
- The assessment remains `PendingRegistration` in SMASS (link did not apply).
- Exactly one `LINK` task exists and is `FAILED` with an error message.
- The failure is recoverable later by manual retry (covered in section F).

---

## F. Manual retry and monitoring

### TC-IGD-SMASS-050 — Manual retry of a failed Generate task

**Objective:** An operator can recover a failed generation after the cause is fixed; the
payload is rebuilt from the immutable triage record.

**Preconditions:** A visit with a `GENERATE` task in `FAILED` (from section E). The
underlying cause has been fixed and integration is enabled.

**Steps:**

1. Call `PATCH /api/IgdVisitSmassTask/retry` with body
   `{ "igdVisitSmassTaskId": "<id>" }`.
2. Inspect the response.
3. Query `BILRG_IgdVisitSmassTask` for the task.
4. Query `SMASS_Assesment` for the visit.
5. Confirm there is no `PayloadJson` column on the task table and that the SMASS request
   contains the same scores as the original triage record.

**Expected result:**

- The retry succeeds; the task becomes `SUCCEEDED` with a non-empty `AssessmentId` and
  `RetryCount` increased.
- Exactly one assessment exists for the visit/triage, with the original clinical values
  (rebuilt from the triage record, not from a stored payload).
- No `PayloadJson` exists on the task table.

---

### TC-IGD-SMASS-051 — Manual retry of a failed Link task

**Objective:** A failed link can be retried and applies the latest registration values.

**Preconditions:** A visit with a `LINK` task in `FAILED`; the assessment is still
pending; integration enabled.

**Steps:**

1. Call `PATCH /api/IgdVisitSmassTask/retry` with the link task id.
2. Query `SMASS_Assesment` for the visit.
3. Query `BILRG_IgdVisitSmassTask`.

**Expected result:**

- The task becomes `SUCCEEDED`; `RetryCount` increased.
- All assessments of the visit are now `Registered` with the latest
  `RegId`/`PasienId`/`PasienName`/`LayananId`/`LayananName`.
- The retry rebuilds from the visit/registration data; no stored payload is replayed.

---

### TC-IGD-SMASS-052 — Retry is only available for Failed tasks

**Objective:** The retry guard in the server rejects tasks that are not failed.

**Preconditions:** One visit with a `SUCCEEDED` task and one with a `PENDING` task if
obtainable.

**Steps:**

1. Call `PATCH /api/IgdVisitSmassTask/retry` with a `SUCCEEDED` task id.
2. Call it again with a `PENDING` task id if one can be produced.
3. Inspect status codes and messages.

**Expected result:**

- The call is rejected for a non-failed task (`Pending` or `Succeeded`) with a clear
  message; the task state is unchanged.
- No duplicate SMASS assessment is produced.

---

### TC-IGD-SMASS-053 — Batch process loops failed tasks without aborting

**Objective:** The operator worklist process handles many failed tasks and isolates
failures.

**Preconditions:** At least two `FAILED` tasks exist (one of which will fail again, one
will succeed — for example because only one has a fixable cause).

**Steps:**

1. Call `POST /api/IgdVisitSmassTask/process`.
2. Inspect the summary returned (`Total`, `Succeeded`, `Failed`).
3. Query the task rows before and after.

**Expected result:**

- The batch runs every failed task (oldest first).
- A single task failure does not abort the batch.
- The summary totals match the actual per-task outcomes; `RetryCount` is increased for
  every attempted task.

---

### TC-IGD-SMASS-054 — Worklist lists failed tasks oldest first

**Objective:** Operators can find outstanding failures across all visits.

**Preconditions:** At least two `FAILED` tasks on different visits created at different
times.

**Steps:**

1. `GET /api/IgdVisitSmassTask/worklist`.
2. Inspect ordering and fields.

**Expected result:**

- Only `FAILED` tasks are returned, oldest first.
- Each row exposes at least `igdVisitSmassTaskId`, `igdVisitId`, `noTriage`,
  `taskType`, `taskStatus`, `assessmentId`, `retryCount`, `lastRetryDate`,
  `processedDate`, `lastError`, `crtDate`.
- `taskType` is `"GENERATE"` or `"LINK"`; `taskStatus` is `"FAILED"`.
- Sentinel `3000-01-01` dates are surfaced as-is by the API (the client converts them).

---

### TC-IGD-SMASS-055 — Retry remains available for terminal/voided visits

**Objective:** Records and retry survive visit completion.

**Preconditions:** A visit with a `FAILED` task that has since been discharged,
redirected or voided. Integration enabled (retry is not gated by the toggle).

**Steps:**

1. Confirm the visit is in a terminal state.
2. Retry its failed task via `PATCH /api/IgdVisitSmassTask/retry`.
3. Open the visit in the web client.

**Expected result:**

- The retry call is accepted (the task row already exists and an operator explicitly
  requested it).
- The task is retried and its state updated.
- Nothing about the retry deletes, cancels, archives or unlinks the task or assessment.

---

## G. Web client

### TC-IGD-SMASS-060 — Triage history shows SMASS status and assessment id

**Objective:** Each triage event shows its SMASS generation status and the linked
assessment id.

**Preconditions:** A visit with at least one generated assessment; integration enabled.

**Steps:**

1. Open `/app/igd/triase_igd`, select the visit.
2. Open the triage history panel.
3. Inspect each triage row.

**Expected result:**

- Each row shows a SMASS status badge next to the timestamp: `Generated` (green),
  `Pending` (amber), or `Failed` (red), matched to that row's `NoTriage`.
- The `AssessmentId` is shown as **plain text** for generated rows; there is no link,
  navigation or copy action.
- No new table column is added and the narrow layout is preserved.

---

### TC-IGD-SMASS-061 — No SMASS slot when there is no Generate task

**Objective:** Backward compatibility for visits created before rollout or with the
toggle off.

**Preconditions:** A visit whose triage happened while integration was off, or before
rollout (no `GENERATE` task).

**Steps:**

1. Open the visit's triage history.
2. Inspect the rows.

**Expected result:**

- The rows render exactly as before, with **no** SMASS badge and no empty slot.
- No error is shown and no extra request is made for SMASS data.

---

### TC-IGD-SMASS-062 — SMASS Integration Panel content

**Objective:** The panel shows one row per task with the right fields.

**Preconditions:** A visit with at least one `GENERATE` and one `LINK` task.

**Steps:**

1. Open the visit and locate the "Integrasi SMASS" panel.
2. Inspect the header and each row.

**Expected result:**

- Header reads "Integrasi SMASS" and shows a status summary chip when failures exist
  (for example "1 gagal").
- One row per task showing: task type label (`Generate` / `Link`), `#n` for a triage
  task or `Visit` for a link task, a status badge (`Succeeded` green / `Pending` amber /
  `Failed` red), and the `AssessmentId` **only for Generate rows**.
- `LastError` is shown when non-empty.
- No SMASS endpoint is called by the client; only BILREG `IgdVisitSmassTask/*`.
- The panel renders for every selected visit.

---

### TC-IGD-SMASS-063 — Retry interaction rules

**Objective:** Retry is available exactly when it should be, and only one retry runs at a
time.

**Preconditions:** A visit with at least one `FAILED` task and one `SUCCEEDED` task.

**Steps:**

1. Inspect the retry buttons on the failed and succeeded rows.
2. Start a retry on the failed row and, while it is running, try to click any other retry
   button.
3. Wait for the retry to settle.
4. Observe the toast and the refreshed rows.

**Expected result:**

- "Coba Ulang" is enabled only on `FAILED` rows.
- It is **hidden** (not merely disabled) on `Succeeded` rows and disabled on `Pending`
  rows.
- While one retry is in progress, every retry button on the panel is disabled.
- On settle, the task list refreshes and exactly one toast reports the outcome
  (success or error).

---

### TC-IGD-SMASS-064 — Panel expand behaviour

**Objective:** Failures are visible even while collapsed.

**Preconditions:** A visit with at least one `FAILED` task, and another visit with no
failures.

**Steps:**

1. Open the visit with a failed task.
2. Observe the panel's open state and the header chip.
3. Open the visit with no failed tasks.

**Expected result:**

- The panel auto-expands when at least one task is `Failed`, and the header always shows
  the "n gagal" chip while failures exist.
- With no failed task the panel is collapsed by default and no chip is shown.
- The panel is mounted directly below the triage history panel on desktop and as a
  section in the tablet/mobile triage drawer.

---

### TC-IGD-SMASS-065 — No destructive actions and no local store

**Objective:** The UI cannot delete or silently change server state.

**Steps:**

1. Inspect every control in the triage history panel and the integration panel.
2. Confirm there is no delete, cancel, archive or unlink action.
3. Confirm there is no optimistic update: while a retry is in flight the row does not
   change until the server responds.
4. Confirm the client does not call SMASS and does not introduce a Pinia store for these
   screens.

**Expected result:**

- Only the approved manual retry sends a mutation.
- Server state stays authoritative; the task list is invalidated and re-read after
  settle.
- No SMASS call and no new Pinia store.

---

## H. Regression and non-goals

### TC-IGD-SMASS-070 — Legacy SMASS behaviour is unchanged

**Objective:** Existing SMASS create/finish/catalog/OFTA flows still work.

**Preconditions:** Integration off (or on — this test targets the pre-existing flows).

**Steps:**

1. Create an assessment through the existing SMASS path (standard creation).
2. Finish it, then view it in the catalog and via the OFTA/documents flow.
3. Repeat the same operations as before the feature.

**Expected result:**

- All existing behaviours and responses are unchanged and still carry their original
  fields.
- No new required field blocks the existing create path.

---

### TC-IGD-SMASS-071 — Existing IGD flows unchanged

**Objective:** Triage, bed, transfer, registration and discharge still work end to end
with integration enabled and SMASS available.

**Preconditions:** Integration enabled; SMASS reachable.

**Steps:**

1. Create a visit, assign dokter, submit triage.
2. Assign a bed, transfer bed (optional), then register and discharge.
3. Observe each response and the visits' state.

**Expected result:**

- Every existing step behaves as before; the extended triage response still contains the
  original fields plus `smassAssessmentId` and `smassGenerationStatus`.
- Bed occupancy and visit state remain consistent.

---

### TC-IGD-SMASS-072 — Void preserves SMASS records; no unlink path exists

**Objective:** Voiding a visit does not delete or unlink assessments.

**Preconditions:** A visit with at least one generated assessment, voidable (no
tindakan/BHP).

**Steps:**

1. Note the assessment and task rows for the visit.
2. Void the visit.
3. Re-query the assessment and task rows.
4. Inspect the API surface for any unlink/delete operation.

**Expected result:**

- Void succeeds as before.
- The assessment(s) and task(s) still exist with the same values; nothing is deleted,
  unlinked or archived.
- No unlink/delete endpoint or UI action exists.

---

### TC-IGD-SMASS-073 — Manual retry only; no automatic worker or polling

**Objective:** Confirm the approved operational model.

**Steps:**

1. Leave a `FAILED` task untouched and observe the system (config/workers/logs) for a
   reasonable period.
2. Observe the web client on a selected visit without interacting.

**Expected result:**

- No background worker, scheduler or outbox retries the task on its own; it stays
  `FAILED` until a manual retry.
- The client does not poll the task endpoint.

---

### TC-IGD-SMASS-074 — No new audit/event entries for task transitions

**Objective:** The task row is the operational record; the IGD timeline is not extended.

**Preconditions:** A visit with generated/linked (and optionally failed) tasks.

**Steps:**

1. Inspect `BILRG_IgdVisitEvent` for the visit.
2. Inspect the audit log for SMASS task transitions.

**Expected result:**

- No `IgdVisitEvent` row and no audit-log entry was added by the generation, link or
  retry operations.

---

### TC-IGD-SMASS-075 — No historical backfill

**Objective:** The change is additive with no data migration.

**Steps:**

1. Find assessments created before the feature.
2. Inspect their `IgdVisitId`, `NoTriage` and `RegistrationLinkStatus`.

**Expected result:**

- No historical assessment was updated; they keep empty `IgdVisitId`, `NoTriage = 0`
  and `RegistrationLinkStatus = Registered`.
- No script or process backfills historical data.

---

# 8. Acceptance-condition coverage

| Architecture acceptance condition (§11.2) | Covered by |
|---|---|
| 1. Exactly one assessment per triage, or a recorded Failed task | TC-010, -011, -013, -014, -041 |
| 2. Persistence committed before/independent of the SMASS call | TC-041, -042, -045 |
| 3. Register links all assessments, all keys together, idempotent | TC-020, -021, -023 |
| 4. Pending reachable only via the by-visit surface | TC-030, -031 |
| 5. `AggStateEnum` never written by the new commands | TC-023, -033, -070 |
| 6. Data-driven mapping; missing mapping fails closed | TC-001, -040, -043 |
| 7. JWT Bearer auth on both operations and new endpoints | TC-044 |
| 8. No historical modification/backfill; legacy unchanged | TC-033, -070, -075 |
| 9. No delete/purge/archive/unlink; monitoring available | TC-032, -065, -072 |
| 10. Frontend never calls SMASS directly | TC-062, -065 |

---

# 9. Result recording

Record every executed case with its identifier, observed result, timestamp, and
evidence (response snippets, screenshots, query output). Use only `PASS`, `FAIL`, or
`BLOCKED`.

For a `FAIL`, include in the evidence:

- what was expected and what was observed;
- the exact steps and test data that reproduce it;
- the environment (BILREG/SMASS version or deployment, configuration values relevant to
  the case);
- any captured ids (`IgdVisitId`, `IgdVisitSmassTaskId`, `AssesmentId`) and error text.

A `FAIL` is a defect record. It does not go back to the programmer directly; it is
handed to Issue Creation so the SDLC can open an ISSUE and start BUG-INVESTIGATION. Any
case affected by an open defect is retested as a new run after the fix; previous runs
are never overwritten.

The **TEST PASSED** gate is granted only when every test case in TEST-EXECUTION is
`PASS`, or every `FAIL` has been resolved through an ISSUE and retested as `PASS`.

---

# 10. Change log

- 2026-09-21 — v1.0 — Initial TEST-PACKAGE created from the COMPLETED
  IMPLEMENTATION-PLAN `igd-triage-abc-smass-implementation-plan.md` (18 slices,
  IMPLEMENTED/GO), ARCHITECTURE V2.0 and FEATURE `igd-01-context.md`.
