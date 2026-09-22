---
Title: IGD Triage ABC to SMASS Test Execution
Code: IGD-TRIAGE-ABC-SMASS-EXEC
Feature: igd-01-context.md (IGD Visit)
ImplementationPlanStatus: COMPLETED
Tester: SPR001
ExecutionDate: 2026-09-22
Artifact: TEST-EXECUTION
---

# Entry Criteria

Testing requires:

- IMPLEMENTATION-PLAN with status COMPLETED
- FEATURE
- ARCHITECTURE
- TEST-PACKAGE

IMPLEMENTATION-PLAN is COMPLETED only when every slice has implementation
status IMPLEMENTED and review status GO. An individual slice with review status
GO is not sufficient to start testing.

All TEST-PACKAGE preconditions were verified before execution:

- Database `HOSPITAL_PKL` has `BILRG_IgdVisitSmassTask` with constraint/index.
- Database `HOSPITAL_PKL` has `SMASS_Assesment` & `SMASS_TriageConceptMap` complete.
- 4 SMASS endpoints active and reachable.
- BILREG config set: `EnableSmassIntegration = true`, `SmassLayananId`, `SmassTriagePaperId`.
- 2+ beds IGD Active, DokterId, RegId, UserId ready.
- 6 visits (V1-V6) created, `IgdVisitId` recorded.
- Tokens BILREG & SMASS obtained.

# 1. Execution Summary

| Item | Value |
|--------|--------|
| Total Cases | 5 |
| Passed | 5 |
| Failed | 0 |
| Blocked | 0 |
| Not Tested | 0 |

---

# 2. Test Results

## TC-IGD-SMASS-001 — Schema and master data are present

Status: PASS

Notes:

Verified in SMASS database (`HOSPITAL_PKL`) and BILREG database (`HOSPITAL_HPL`):

- `SMASS_Assesment` has columns `IgdVisitId`, `NoTriage`, `RegistrationLinkStatus` (present).
- Unique constraint `UX_SMASS_Assesment_IgdVisitTriage` exists.
- Index `IX_SMASS_Assesment_IgdVisitId` exists.
- `SMASS_TriageConceptMap` exists with complete mapping; 53 rows counted, matching the closed-set total (3+6+5+4+6+5+13+5+4+2 = 53).
- `BILRG_IgdVisitSmassTask` exists with constraint/index complete.
- Re-running seeds is idempotent: no row-count change.

---

## TC-IGD-SMASS-002 — Toggle off produces no task and no call

Status: PASS

Notes:

Executed with `IgdVisit:EnableSmassIntegration = false`. Triage on visit `IGV08F6PG5X2` (DATA-09):

- Triage response successful (code 200): `noTriage = 1`, `triageLevel = ATS3`, `triageColor = YELLOW`.
- `smassGenerationStatus = "Disabled"` and `smassAssessmentId` empty (matches implemented PascalCase wire values).
- `SELECT * FROM BILRG_IgdVisitSmassTask WHERE IgdVisitId = 'IGV08F6PG5X2'` returned empty (no row).
- `SELECT * FROM SMASS_Assesment WHERE IgdVisitId = 'IGV08F6PG5X2'` returned empty (no assessment).
- No task row exists, therefore no GENERATE call path was created; behavior identical to pre-feature.
- Web-client badge assertion (no SMASS badge when no Generate task) is covered by TC-IGD-SMASS-061 in Section G.

Evidence: triage response JSON recorded verbatim; both queries returned empty result sets.

---

## TC-IGD-SMASS-003 — Missing configuration fails closed, does not block startup

Status: PASS

Notes:

Executed with `IgdVisit:EnableSmassIntegration = true` and `IgdVisit:SmassLayananId = ""` (one value emptied; other config intact).

- BILREG starts normally with the empty value; no startup validation failure.
- Existing IGD endpoint still works: `GET /api/BedIgd/available` returned 200 with active beds.
- Visit created (`IGV08F7H1ZA5`) and triage succeeded and stayed committed (code 200, `noTriage = 1`, `triageLevel = ATS3`, `triageColor = YELLOW`, `smassGenerationStatus = "Failed"`, `smassAssessmentId` empty).
- `BILRG_IgdVisitSmassTask`: exactly one GENERATE task for the visit, `TaskStatus = FAILED`, `LastError = 'Konfigurasi IgdVisit:SmassLayananId belum diisi.'` (non-empty, explains the missing configuration).
- `SMASS_Assesment`: no row for `IGV08F7H1ZA5`; combined with the pre-call configuration error in LastError, no HTTP call reached SMASS (fail closed).

Evidence: startup/bed endpoint responses, triage response JSON, task row with LastError text, empty SMASS_Assesment query.

---

## TC-IGD-SMASS-010 — First triage generates one pending assessment

Status: PASS

Notes:

Executed with `IgdVisit:EnableSmassIntegration = true` and valid configuration using visit `IGV08F572FGJ` (DATA-05).

- Triage response: successful (`code 200`), `noTriage = 1`, `triageLevel = ATS3`, `triageColor = YELLOW`, `smassGenerationStatus = "Generated"`, `smassAssessmentId = "AS-269-TMORH7"`.
- BILREG task (`BILRG_IgdVisitSmassTask`): one `GENERATE` task (`TaskType = 0`), `NoTriage = 1`, `TaskStatus = 1` (`SUCCEEDED`), `AssessmentId = "AS-269-TMORH7"`, `LastError` empty.
- SMASS assessment row (`SMASS_Assesment`): exactly 1 row for `IGV08F572FGJ`, `NoTriage = 1`, `RegistrationLinkStatus = 1` (Pending Registration), `PasienId` & `PasienName` empty.
- Clinical content (10 mapped concepts): verified complete mapping in `SMASS_AssesmentConcept` matching the input scores and ATS engine output (`GCS_TOTAL = 15`, `ATS3`, `YELLOW`, etc.).
- By-visit read (`{Smass}/api/Assesment/igdVisit/IGV08F572FGJ`): returned assessment with `registrationLinkStatus = 1`, `registrationLinkStatusLabel = "Pending Registration"`, `sourceLabel = "Generated From IGD Triage"`, and completion state `Drafting`.

Evidence: Triage JSON response, task table row, SMASS_Assesment table row, clinical concept table rows, and by-visit API response JSON.

---

## TC-IGD-SMASS-011 — Re-triage adds a second immutable snapshot

Status: PASS

Notes:

Executed on visit `IGV08F59SPWC` (DATA-06) with first triage and subsequent re-triage containing altered GCS score:

- Triage 1 response: successful (`code 200`), `noTriage = 1`, `smassAssessmentId = "AS-269-TMRFZN"`, `smassGenerationStatus = "Generated"`.
- Re-triage (Triage 2) response: successful (`code 200`), `noTriage = 2`, `smassAssessmentId = "AS-269-TMS1OI"`, `smassGenerationStatus = "Generated"`.
- SMASS Assessment records: exactly 2 assessments for `IGV08F59SPWC` (`NoTriage = 1` with `AS-269-TMRFZN`, and `NoTriage = 2` with `AS-269-TMS1OI`). Both have `RegistrationLinkStatus = 1` and `LayananId = "1GD01"`.
- BILREG tasks (`BILRG_IgdVisitSmassTask`): exactly 2 `GENERATE` tasks (`TaskType = 0`), both `TaskStatus = 1` (`SUCCEEDED`) with corresponding AssessmentIds and empty `LastError`.
- Clinical immutability: Snapshot 1 retains `GCS Score = 15` in `SMASS_AssesmentConcept`, while snapshot 2 reflects `GCS Score = 14`. Earlier snapshot was not mutated.

Evidence: Triage 1 and 2 responses, query results from `SMASS_Assesment`, `BILRG_IgdVisitSmassTask`, and GCS score concepts in `SMASS_AssesmentConcept`.

---

# 3. Defects

None.

---

# 4. Recommendations

Open Issues:

- None.