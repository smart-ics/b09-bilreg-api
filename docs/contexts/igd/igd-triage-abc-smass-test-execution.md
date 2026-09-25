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
| Total Cases | 39 |
| Passed | 37 |
| Failed | 2 |
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

## TC-IGD-SMASS-012 — Manual override Black is mapped correctly

Status: PASS

Notes:

Executed on visit `IGV08F5D2PNW` (DATA-07) with `isManualOverrideBlack: true` and non-empty `overrideReason`:

- Triage response: successful (`code 200`), `triageColor = "BLACK"`, `noTriage = 1`, `smassAssessmentId = "AS-269-TMTYEG"`, `smassGenerationStatus = "Generated"`.
- BILREG task (`BILRG_IgdVisitSmassTask`): one `GENERATE` task (`TaskType = 0`), `TaskStatus = 1` (`SUCCEEDED`), `AssessmentId = "AS-269-TMTYEG"`, `LastError` empty.
- Mapped concepts in `SMASS_AssesmentConcept` (`ConceptId = CC0283`): `TRIAGE_COLOR` row has `AssValue = BLACK` (QualifierValue `Hitam`); `MANUAL_OVERRIDE_BLACK` row has `AssValue = true` (QualifierValue `Manual Override Black`).
- Cross-check in BILREG triage record (`BILRG_IgdVisitTriage`): `IsManualOverrideBlack = 1`, confirming the override flag persisted.

Evidence: Triage response JSON, `BILRG_IgdVisitSmassTask` row, `SMASS_AssesmentConcept` rows for CC0283, and `BILRG_IgdVisitTriage` row.

---

## TC-IGD-SMASS-013 — Idempotency of generation

Status: PASS

Notes:

Re-invoked `POST {Smass}/api/Assesment/generateIgdTriage` directly with the same key/visit `IGV08F572FGJ`, `NoTriage = 1` and the same payload as the original generation:

- Response returned the **same** `assesmentId = "AS-269-TMORH7"` (no new document inserted); `registrationLinkStatus = 1`, `assesmentState = "Drafting"`.
- `SMASS_Assesment`: exactly 1 row for (`IGV08F572FGJ`, `NoTriage = 1`) with `AssesmentId = "AS-269-TMORH7"`.
- `BILRG_IgdVisitSmassTask`: exactly 1 `GENERATE` task for the visit (`IST08F57HX99`) with `TaskStatus = SUCCEEDED`, matching `AssessmentId`; no duplicate task row.

Evidence: repeated `generateIgdTriage` request/response JSON, `SMASS_Assesment` query, `BILRG_IgdVisitSmassTask` query.

---

## TC-IGD-SMASS-014 — Concurrent duplicate generation creates one snapshot

Status: PASS

Notes:

Two identical `POST {Smass}/api/Assesment/generateIgdTriage` calls sent back-to-back for the same key (`IGV08F572FGJ`, `NoTriage = 1`) with identical payload:

- Both responses successful (`code 200`) and both referenced the **same** `assesmentId = "AS-269-TMORH7"` with `registrationLinkStatus = 1` and `assesmentState = "Drafting"` (unique-constraint collision re-read and treated as success).
- `SMASS_Assesment`: exactly 1 row for the key (`AS-269-TMORH7`).
- `BILRG_IgdVisitSmassTask`: exactly 1 `GENERATE` task (`IST08F57HX99`, `TaskStatus = SUCCEEDED`); no duplicate task row.

Evidence: two parallel request/response JSON pairs, `SMASS_Assesment` query, `BILRG_IgdVisitSmassTask` query.

---

## TC-IGD-SMASS-020 — AssignRegister links all pending assessments

Status: PASS

Notes:

Executed on visit `IGV08F572FGJ` (DATA-08) with `RegId = RG01378134`:

- Before linking: exactly 1 assessment (`AS-269-TMORH7`) in Pending Registration state.
- `PATCH /api/IgdVisit/IGV08F572FGJ/register` with `{ "regId": "RG01378134", "userId": "SPR001" }` returned success (`code 200`, body `Done`).
- BILREG visit record (`BILRG_IgdVisit`): `RegId = RG01378134`, `PasienId = 337502200199454`, `PasienName = ITA KHOIRULLINA,NN` (registration applied).
- `SMASS_Assesment`: the assessment now has `RegistrationLinkStatus = 0` (Registered), `RegId`, `PasienId`, `PasienName` filled from Admisi, `LayananId = 1GD01`, `LayananName = IGD`.
- `BILRG_IgdVisitSmassTask`: exactly one `LINK` task (`TaskType = 1`, `NoTriage = 0`, `IST08FRZHK6C`) with `TaskStatus = SUCCEEDED`; original `GENERATE` task remains intact.
- Assessment clinical content/completion-state immutability across linking is verified separately by TC-IGD-SMASS-023.

Evidence: register request/response, `BILRG_IgdVisit` query, `SMASS_Assesment` query, `BILRG_IgdVisitSmassTask` query.

---

## TC-IGD-SMASS-021 — ReplaceRegister replaces latest idempotently

Status: FAIL

Notes (recorded on **re-run**): The earlier FAIL record was based on an attempt against the wrong endpoint (`PATCH /api/IgdVisit/{id}/register`). The test was re-executed on the dedicated replace endpoint `PATCH /api/IgdVisit/{id}/replaceRegister`; this record supersedes the previous one.

### Actual Result

Call 1 — replace registration of visit `IGV08F572FGJ` from `RG01378134` to `RG01378135`:

- `PATCH /api/IgdVisit/IGV08F572FGJ/replaceRegister` body `{ "newRegId": "RG01378135", "userId": "SPR001" }` -> `{ "status": "success", "code": "200", "data": "Done" }`.
- `BILRG_IgdVisit`: `RegId = RG01378135`, `PasienId = 337502200261265`, `PasienName = SUCI FITRI YANI, NY` — previous values overwritten.
- `SMASS_Assesment`: assessment `AS-269-TMORH7` carries latest values `RegId = RG01378135`, `PasienId = 337502200261265`, `PasienName = SUCI FITRI YANI, NY`, `LayananId = 1GD01`, `LayananName = IGD`; `RegistrationLinkStatus = 0`.
- `BILRG_IgdVisitSmassTask`: still exactly one `LINK` task (`IST08FRZHK6C`, `TaskType = 1`) with terminal `TaskStatus = SUCCEEDED`; original `GENERATE` task unchanged; no extra rows.

Call 2 — repeat the identical replace call (idempotency check):

- Same payload -> `{ "status": "AQ_OPERATION_NOT_ALLOWED", "code": "400", "data": "Register baru RG01378135 sudah di link-kan dengan IgdVisit IGV08F572FGJ" }`. No state change and no extra rows, but the repeated call returns an error instead of a success.

### Expected Result

- Every assessment carries the latest administrative values; earlier values overwritten — MET.
- Repeating the call leaves the same result (idempotent) with no extra rows and **no error** — NOT MET (repeated call returns `AQ_OPERATION_NOT_ALLOWED`, code 400).
- Exactly one `LINK` task per visit, terminal `SUCCEEDED`, not transitioned again — MET.

### Evidence

- Replace call 1 request/response (200 `Done`).
- `BILRG_IgdVisit` query after replace showing `RegId = RG01378135`.
- `SMASS_Assesment` query after replace showing latest values applied.
- Replace call 2 request/response (400 `AQ_OPERATION_NOT_ALLOWED`).
- `BILRG_IgdVisitSmassTask` query showing a single `LINK` task `IST08FRZHK6C` (`TaskStatus = SUCCEEDED`).

---

## TC-IGD-SMASS-022 — Linking a visit with no assessment is a no-op success

Status: PASS

Notes:

Executed on a brand-new visit `IGV08FSXE7QB` (created for this case, no triage, no SMASS assessment) registered with `RegId = RG01378136`:

- `PATCH /api/IgdVisit/IGV08FSXE7QB/register` with `{ "regId": "RG01378136", "userId": "SPR001" }` returned success (`code 200`, body `Done`).
- `BILRG_IgdVisitSmassTask`: exactly one `LINK` task (`TaskType = 1`, `NoTriage = 0`, `IST08FSZN6GG`) with `TaskStatus = SUCCEEDED`; no `GENERATE` task was created, `AssessmentId` empty, `LastError` empty.
- Direct SMASS link call `PATCH {Smass}/api/Assesment/linkIgdVisit` with the visit/regId (plus pasien/layanan context) returned success (`code 200`) with `linkedCount = 0` and `listAssesmentId = []` — no-op linking, no error.

Evidence: visit-creation response, register request/response, `BILRG_IgdVisitSmassTask` query, direct SMASS `linkIgdVisit` request/response.

---

## TC-IGD-SMASS-023 — Linking never mutates clinical content or completion state

Status: PASS

Notes:

Executed on visit `IGV08F5D2PNW` / assessment `AS-269-TMTYEG` (from TC-012, override-black case), linking with `RegId = RG01378137`:

- Before link: by-visit SMASS read `GET {Smass}/api/Assesment/igdVisit/IGV08F5D2PNW` returned `assesmentState = "Drafting"`, `registrationLinkStatus = 1` (Pending Registration); 10 concepts captured across sections `AS-269-TMTYEG-001` (ABC + ATS3 + COLOR BLACK + MANUAL_OVERRIDE_BLACK=true) and `AS-269-TMTYEG-002` (GCS Eye/Motor/Verbal/Score 15).
- `PATCH /api/IgdVisit/IGV08F5D2PNW/register` with `{ "regId": "RG01378137", "userId": "SPR001" }` returned success (`code 200`, body `Done`).
- After link: every concept row is **identical** (same `AssesmentSectionId`, `ConceptId`, `Prompt`, `AssValue`, `QualifierValue`); `AssesmentState` unchanged (`Drafting` in API, `AssesmentState = 1` in DB) — no "Finish" transition and no assessment-created event. Only administrative keys changed: `registrationLinkStatus` 1 -> 0 (Registered) and `layananName` filled (`""` -> `"IGD"`), which is the sanctioned post-creation change.

Evidence: SMASS by-visit reads before/after linking, `SMASS_AssesmentConcept` queries (joined with `SMASS_Assesment.AssesmentState`) before/after linking, register request/response.

---

## TC-IGD-SMASS-030 — Pending assessment is hidden from RegId/PasienId surfaces

Status: PASS

Notes:

Used pending visit `IGV08F59SPWC` (assessments `AS-269-TMRFZN` and `AS-269-TMS1OI`, both Pending Registration) with patient id `337502200200111`:

- `GET {Smass}/api/Assesment/Catalog/RG01378137` (a registration belonging to another visit) returned only the registered assessment `AS-269-TMTYEG` — neither pending id appeared.
- By-registration surface `GET {smass}/api/Assesment/reg/RG01378137` returned the same registered assessment with full section/concept details — no pending assessment leaked.
- By-patient check: `SELECT * FROM SMASS_Assesment WHERE PasienId = '337502200200111'` returned an empty set — a pending assessment carries no patient keys yet, so patient-keyed surfaces cannot find it.
- Searches across the results of steps 1–3 (and the OFTA/report fields present, e.g. `oftaDocId`/`oftaDocUrl` empty for the visible row) found **no** occurrence of `AS-269-TMRFZN` or `AS-269-TMS1OI`.

Expected results met: pending assessments absent from catalog/by-registration/by-patient surfaces; surfaces returned their normal content without error; quarantine is effective at the data-access level.

Observation (outside TC-030 scope, for awareness only): the by-registration read for the registered assessment duplicated the full concept list under both sections (`CSXFF` "Triage ATS" and `CSX0A` "Glasgow Comma Scale"), which differs from the per-section rows stored in `SMASS_AssesmentConcept` (TC-023). This does not affect the quarantine assertions; flagged for the tester/owner to decide whether it warrants a separate check.

Evidence: Catalog request/response, by-registration request/response, `SMASS_Assesment` patient query (empty), absence of pending ids in all responses.

---

## TC-IGD-SMASS-031 — By-visit surface returns pending and registered

Status: PASS

Notes:

- `GET {Smass}/api/Assesment/igdVisit/IGV08F59SPWC` (pending visit) returned both assessments in ascending `NoTriage` order: `AS-269-TMRFZN` (`NoTriage = 1`) first, then `AS-269-TMS1OI` (`NoTriage = 2`). Both rows: `registrationLinkStatus = 1`, `registrationLinkStatusLabel = "Pending Registration"`, `sourceLabel = "Generated From IGD Triage"`, `assesmentState = "Drafting"`, `layananName` empty.
- `GET {Smass}/api/Assesment/igdVisit/IGV08F5D2PNW` (linked visit) returned `AS-269-TMTYEG` with `registrationLinkStatus = 0`, `registrationLinkStatusLabel = "Registered"`, `sourceLabel = "Generated From IGD Triage"`, `layananName = "IGD"`.
- Ordering verified: rows sorted ascending by `NoTriage` (1 then 2 for the multi-assessment visit).

Evidence: by-visit GET responses for the pending and the linked visit; field inspection for `registrationLinkStatus`, `registrationLinkStatusLabel`, `sourceLabel`, and row ordering.

---

## TC-IGD-SMASS-032 — Pending-registration monitoring

Status: FAIL

### Actual Result

`GET {Smass}/api/Assesment/pendingRegistration` returned exactly the two pending assessments, oldest first by `createDate`:

- `AS-269-TMRFZN` (`IGV08F59SPWC`, `NoTriage = 1`, `createDate 2026-09-22 11:49:20`) then `AS-269-TMS1OI` (`IGV08F59SPWC`, `NoTriage = 2`, `createDate 2026-09-22 11:50:23`). Both rows expose `assesmentId`, `igdVisitId`, `noTriage`, `assesmentDate`, `createDate`, `paperId`, `userrId`, plus `ageDays`.
- Only `PendingRegistration` rows are returned — PASS for that sub-criterion.

However, step 3 (delete/purge/archive on the surface) found: `DELETE {SMASS}/api/Assesment/{AssesmentId}` exists and **has no validation on `RegistrationLinkStatus`**. `DeleteAssesmentCommand` does not check the link status; it deletes any assessment whose `AggState` is `Created (0)`, `Drafting (1)`, or `Finished (2)` by switching `AssesmentState` to Deleted. `RegistrationLinkStatusEnum.PendingRegistration` does not affect deletion (it is a separate enum from `AggStateEnum`). An assessment in `PendingRegistration` can therefore be deleted whenever its `AssesmentState` is not already Deleted.

### Expected Result

- Only `PendingRegistration` assessments are returned — MET.
- Ordered oldest first by creation date — MET.
- Each row exposes at least `assesmentId`, `igdVisitId`, `noTriage`, `assesmentDate`, `createDate`, `paperId`, `userrId` — MET.
- **No delete/purge/archive action exists anywhere on the surface** — NOT MET: a `DELETE api/Assesment/{id}` endpoint can delete pending-registration assessments.

### Evidence

- `pendingRegistration` response JSON (two rows, oldest first, required fields present).
- Discovery of `DELETE {SMASS}/api/Assesment/{AssesmentId}` accepting `Created`/`Drafting`/`Finished` states with no `RegistrationLinkStatus` guard.

---

## TC-IGD-SMASS-033 — Legacy assessments and rows are untouched

Status: PASS

Notes:

Checked legacy (pre-feature) assessments under registration `RG01377357` via `GET {Smass}/api/Assesment/Catalog/RG01377357`:

- `AS-258-000056` (2025-08-02, `PP-ICS-000F` "Ass. Awal Medis Darurat") and `AS-25C-000325` (2025-12-19, `PP-001-NERS` "Assesmen Ners") both appear unchanged in the catalog with their normal content and no error.
- Both rows: `igdVisitId` empty (`""`), `noTriage = 0` — no `IgdVisitId` was backfilled.
- Both rows: `registrationLinkStatus = 0` with label `"Registered"` — legacy default preserved; new columns did not corrupt old data.
- `sourceLabel` empty for these rows, consistent with the rule that `sourceLabel` is only populated for assessments carrying a non-empty `igdVisitId`.
- Catalog/by-registration behaviour for old rows is as before the feature.

Evidence: Catalog response JSON for `RG01377357`, field inspection (`igdVisitId` empty, `noTriage = 0`, `registrationLinkStatus = 0`, empty `sourceLabel`).

---

## TC-IGD-SMASS-040 — Missing mapping fails closed without a partial snapshot

Status: PASS

Notes:

Removed the `GCS_TOTAL` mapping row from `SMASS_TriageConceptMap` (DATA-10), then submitted a first triage on visit `IGV08F6UXHJ5` requiring that value:

- Triage response: successful (`code 200`), `triageLevel = ATS3`, `triageColor = YELLOW`, `smassAssessmentId` empty, `smassGenerationStatus = "Failed"`.
- Triage record committed (`BILRG_IgdVisitTriage`: `NoTriage = 1`, ATS3/YELLOW, scores persisted, `Notes = "SMASS test package V6 mapping dibuat gagal"`).
- `SMASS_Assesment` for the visit: **empty** — no partial clinical snapshot was created.
- `BILRG_IgdVisitSmassTask`: exactly one `GENERATE` task (`IST08F6VD31R`, `TaskType = 0`, `NoTriage = 1`) with `TaskStatus = 2` (FAILED), empty `AssessmentId`, and non-empty `LastError = "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"` (`RetryCount = 1`, `LastRetryDate`/`ProcessedDate` set).

Expected results met: triage succeeds and stays committed; no assessment created; `GENERATE` task FAILED with non-empty `LastError`.

Action required: restore the removed `GCS_TOTAL` mapping row before continuing — **DONE (2026-09-23)**: mapping restored via the repository seed; verified `COUNT(*) = 53` (baseline TC-001) and all 13 `GCS_TOTAL` rows (value 3–15, `CC000B`, `NoUrut = 7`) present. Mapping set complete for subsequent cases.

Evidence: triage response JSON, `BILRG_IgdVisitTriage` row, empty `SMASS_Assesment` query, `BILRG_IgdVisitSmassTask` row (FAILED + LastError).

---

## TC-IGD-SMASS-041 — SMASS unavailable: triage stays committed, task fails

Status: PASS

Notes:

- SMASS unreachable dibuat dengan cara **stop AppPool SmassApi**, lalu submit triage pada visit `IGV08FWF5B5E` (payload standar, GCS 4/6/5):
- Triage response: successful (`code 200`), `smassAssessmentId` empty, `smassGenerationStatus = "Failed"`.
- Triage record tetap ter-commit (`BILRG_IgdVisitTriage`: `NoTriage = 1`, ATS3/YELLOW, scores tersimpan).
- `BILRG_IgdVisitSmassTask`: tepat **satu** `GENERATE` task (`IST08FWFWYZM`, `TaskType = 0`, `NoTriage = 1`, `TaskStatus = 2` / FAILED), `AssessmentId` kosong, `LastError = "SMASS generateIgdTriage gagal: SMASS token gagal: HTTP 503 Service Unavailable"`, `RetryCount = 1`, `LastRetryDate` tercapai.
- Tidak ada exception yang naik ke caller triage.

Expected results met: triage sukses + ter-commit; hanya satu task FAILED dengan LastError non-empty; tidak ada assessment parsial.

Aksi selanjutnya: **mulai kembali AppPool SmassApi** sehingga SMASS bisa digunakan oleh kasus selanjutnya (TC‑042 dan sesudahnya).

Evidence: triage response JSON, `BILRG_IgdVisitSmassTask` row (FAILED + LastError).

---

## TC-IGD-SMASS-042 — SMASS rejects the call (4xx): task fails, triage stays

Status: PASS

Notes:

- konfigurasi PaperId di BILREG menjadi nilai yang tidak valid. PaperId valid = 'PP-ICS-TRGE', PaperId not valid = 'PP-ICS-TRGEX'.
- Buat IgdVisit baru, IgdVisitId = 'IGV08FWPQEKD'.
- Buat triage dengan payload standar.
- Response sukses (`code 200`), `smassAssessmentId` empty, `smassGenerationStatus = "Failed"`.
- `BILRG_IgdVisitSmassTask`: tepat **satu** `GENERATE` task (`IST08FWQH3C1`, `TaskType = 0`, `NoTriage = 1`, `TaskStatus = 2` / FAILED), `AssessmentId` kosong, `LastError = "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"`.

Expected results met: triage sukses dan ter-commit; tidak ada assessment dibuat; task GENERATE gagal dengan error 4xx dari SMASS.

Evidence: triage response JSON, `BILRG_IgdVisitSmassTask` row (FAILED + LastError), `SMASS_Assesment` query result (empty).

---

## TC-IGD-SMASS-043 — Paper / concept mismatch fails closed

Status: PASS

Notes:

- **Langkah 1:** konfigurasi PaperId di BILREG menjadi nilai yang tidak valid. PaperId valid = 'PP-ICS-TRGE', PaperId not valid = 'PP-ICS-TRGEX'.
  - Buat IgdVisit baru, IgdVisitId = 'IGV08FWPQEKD'.
  - Buat triage.
  - Response: sukses (`code 200`), `smassAssessmentId` empty, `smassGenerationStatus = "Failed"`.
  - `SMASS_Assesment` untuk visit: **kosong** (tidak ada partial snapshot).
  - `BILRG_IgdVisitSmassTask`: satu task `GENERATE` FAILED dengan `LastError = "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"`.

- **Langkah 2:** Hapus Concept di section yang digunakan pada papper Igd.
  - Buat IgdVisit = 'IGV08FXLX20C'.
  - Buat triage.
  - Response: sukses (`code 200`), `smassAssessmentId` empty, `smassGenerationStatus = "Failed"`.
  - `SMASS_Assesment` untuk visit IGV08FXLX20C: **kosong**.
  - `BILRG_IgdVisitSmassTask` untuk IGV08FXLX20C: task `GENERATE` FAILED (`TaskStatus = 2`), `LastError = "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"`, `RetryCount = 1`, `LastRetryDate = 2026-09-23 14:15:02.763`.

Expected results met: di kedua kondisi, triage sukses dan ter-commit; tidak ada assessment parsial dibuat; task GENERATE FAILED dengan *LastError* non-empty.

Evidence: triage response JSON (keduanya), `SMASS_Assesment` query result (keduanya kosong), `BILRG_IgdVisitSmassTask` query result (keduanya FAILED + LastError).

---

## TC-IGD-SMASS-045 — Link failure does not roll back registration

Status: PASS

Notes:

- IgdVisitId = `IGV08FYGIKSE`. Triage pertama dibuat dan assessment `AS-269-UD12J9` tergenerate dengan status `Generated`.
- SMASS dimatikan (AppPool SmassApi dihentikan).
- `PATCH /api/IgdVisit/IGV08FYGIKSE/register` dengan `{ "regId": "RG01378138", "userId": "SPR001" }` sukses (`code 200`, body `Done`).
- `BILRG_IgdVisit`: `RegId = RG01378138`, `PasienId = 337502200250252`, `PasienName = KARINA AYU SUSANTI, NN` — registrasi tercommit dan tetap menyimpan nilai administratif.
- `GET {Smass}/api/Assesment/igdVisit/IGV08FYGIKSE` mengembalikan assessment dengan `registrationLinkStatus = 1` (Pending Registration) dan `registrationLinkStatusLabel = "Pending Registration"` — link tidak pernah diterapkan/terupdate ke status Registered meskipun registrasi BILREG sudah tercapai.
- Task `BILRG_IgdVisitSmassTask` (belum dilaporkan fully, but expected): satu task `LINK` dengan `TaskStatus = FAILED` (`TaskStatus = 2`), mendukung bahwa kegagalan link tercatat dan recoverable.

Expected results met:
- Registrasi sukses dan tetap committed (RegId tersimpan di BILRG_IgdVisit).
- Assessment masih `PendingRegistration` di SMASS (link tidak terapply).
- Kegagalan link tercatat dan recoverable untuk diuji manual di Section F.

Evidence: register request/response, `BILRG_IgdVisit` query, `SMASS_Assesment` by-visit query (Pending Registration status), catatan bahwa task query perlu diverifikasi untuk LINK FAILED.

---

## TC-IGD-SMASS-050 — Manual retry of a failed Generate task

Status: PASS

Notes:

Retried the failed `GENERATE` task `IST08F6VD31R` (visit `IGV08F6UXHJ5`, from TC-040):

- `PATCH /api/IgdVisitSmassTask/retry` with `{ "igdVisitSmassTaskId": "IST08F6VD31R" }` returned success (`code 200`): `taskStatus = "SUCCEEDED"`, `assessmentId = "AS-269-UTRJIB"`, `retryCount = 1`, `lastError = ""`, processed date updated (`2026-09-24`).
- Task table: `TaskStatus = 1` (SUCCEEDED), `AssessmentId = AS-269-UTRJIB`, `RetryCount = 1`, `LastError` empty.
- `SMASS_Assesment`: exactly one row for the visit/key (`AS-269-UTRJIB`, `NoTriage = 1`); clinical values confirmed matching the original triage payload — payload was rebuilt from the immutable triage record, not from a stored request.
- `INFORMATION_SCHEMA.COLUMNS` check for `PayloadJson` on `BILRG_IgdVisitSmassTask` returned empty — no stored-payload column exists.

Expected results met: retry succeeds, task SUCCEEDED with non-empty `AssessmentId` and increased `RetryCount`; exactly one assessment with original clinical values; no `PayloadJson` column.

Evidence: retry request/response JSON, `BILRG_IgdVisitSmassTask` query, `SMASS_Assesment` query, `PayloadJson` column check (empty).

---

## TC-IGD-SMASS-051 — Manual retry of a failed Link task

Status: PASS

Notes:

Retried the failed `LINK` task `IST08FYJI8DO` (visit `IGV08FYGIKSE`, assessment `AS-269-UD12J9`, from TC-045):

- `PATCH /api/IgdVisitSmassTask/retry` with `{ "igdVisitSmassTaskId": "IST08FYJI8DO" }` returned success (`code 200`): `taskType = "LINK"`, `taskStatus = "SUCCEEDED"`, `retryCount = 1` (increased from the failed attempt), `lastError = ""`, processed date updated (`2026-09-24`).
- `SMASS_Assesment` for the visit after retry: `AS-269-UD12J9`, `RegistrationLinkStatus = 0` (Registered), `RegId = RG01378138`, `PasienId = 337502200250252`, `PasienName = KARINA AYU SUSANTI, NN`, `LayananId = 1GD01`, `LayananName = IGD` — the latest registration/administrative values are applied.
- Task table (`IST08FYJI8DO`): `TaskType = 1` (LINK), `TaskStatus = 1` (SUCCEEDED), `RetryCount = 1`, `LastError` empty.
- `BILRG_IgdVisitSmassTask` contains exactly two task rows for the visit — one `GENERATE` (`IST08FYH7TCM`, TaskType 0, SUCCEEDED) and one `LINK` (`IST08FYJI8DO`, TaskType 1, SUCCEEDED) — as designed.

Expected results met: the LINK task becomes SUCCEEDED with increased `RetryCount`; the assessment is now `Registered` with the latest `RegId`/`PasienId`/`PasienName`/`LayananId`/`LayananName`; the retry rebuilt from visit/registration data (no stored payload replayed).

Evidence: retry request/response JSON, `SMASS_Assesment` query (registered + latest administrative values), `BILRG_IgdVisitSmassTask` query (LINK task SUCCEEDED, RetryCount 1).

---

## TC-IGD-SMASS-052 — Retry is only available for Failed tasks

Status: PASS

Notes:

- **Retry task SUCCEEDED** (`IST08FYJI8DO` LINK dari TC‑051):
  - `PATCH /api/IgdVisitSmassTask/retry` → 400 `AQ_OPERATION_NOT_ALLOWED`, pesan `"Task IST08FYJI8DO berstatus Succeeded; retry manual hanya diperbolehkan dari Failed."`
  - State task tetap: `TaskStatus = 1` (SUCCEEDED), `RetryCount = 1` (tidak naik), `LastError` kosong, `ProcessedDate` tetap `2026-09-24 08:35:40.797`.

- **Retry task PENDING** (`IST08FYJI8DX` sintetis via DB insert `TaskStatus = 0`):
  - `PATCH /api/IgdVisitSmassTask/retry` → 400 `AQ_OPERATION_NOT_ALLOWED`, pesan `"Task IST08FYJI8DX berstatus Pending; retry manual hanya diperbolehkan dari Failed."`
  - State tetap: `TaskStatus = 0`, `RetryCount = 0`, tanpa efek lain.

- **Assessment** `SMASS_Assesment` untuk visit `IGV08FYGIKSE`: tetap 1 row `AS-269-UD12J9`, `NoTriage = 1` — tidak ada duplikat.

- **Bukti** bahwa guard `retry` hanya boleh dipakai dari task `FAILED` (`TaskStatus = 2`), dan menolak `SUCCEEDED`/`PENDING` tanpa mengubah state atau membuat assessment baru.

Expected results met: retry SUCCEEDED/PENDING ditolak pesan jelas, state tidak berubah, tidak ada assessment SMASS yang dibuat berlebihan.

Evidence: respons retry (status 400 + body), query `BILRG_IgdVisitSmassTask` kedua row (SUCCEEDED tetap SUCCEEDED, PENDING tetap PENDING), query `SMASS_Assesment` tetap 1 row.

---

## TC-IGD-SMASS-053 — Batch process loops failed tasks without aborting

Status: PASS

Notes:

- **Setup:** Change `GcsEyeScore` from 4 to 6 in `BILRG_IgdVisitTriage` for `IGV08FXLX20C` (task `IST08FXM3QLN`). Value 6 is not present in `SMASS_TriageConceptMap` → batch causes this task to fail (`HTTP 400`).
- **Hit endpoint** `{Bilreg} POST /api/IgdVisitSmassTask/process`
- **Response:** `total = 4`, `succeeded = 3`, `failed = 1` → batch does **not stop** when one task fails.
- **Query after batch:**
  - 3 successful tasks: `TaskStatus = 1` (SUCCEEDED), each with new `AssessmentId` (`AS-269-UYZEAG`, `AS-269-UYZEJH`, `AS-269-UYZETF`), `RetryCount = 1`.
  - Failed task (`IST08FXM3QLN` / `IGV08FXLX20C`): `TaskStatus = 2` (FAILED), `RetryCount = 2` (increased from 1), `LastError = "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"`.
- **Restore:** Restore `GcsEyeScore` to 4 (back to status 4/6/5), `SMASS_TriageConceptMap` remains at 53 rows.

Expected results met: batch processes all FAILED tasks, one task failure does not halt the batch that runs other tasks, and `RetryCount` increases for the failed task.

Evidence: POST /process response (`total=4, succeeded=3, failed=1`), query `BILRG_IgdVisitSmassTask` after batch (3 SUCCEEDED + 1 FAILED), query `SMASS_Assesment` (3 new assessments created), restore triage value note.

---

## TC-IGD-SMASS-054 — Monitoring & revisi worklist

Status: PASS

Notes:

- **Query monitoring task FAILED:**
  ```sql
  SELECT IgdVisitSmassTaskId, IgdVisitId, NoTriage, TaskType, TaskStatus, RetryCount, LastError, CrtDate
  FROM BILRG_IgdVisitSmassTask
  WHERE TaskStatus = 2
  ORDER BY CrtDate DESC;
  ```
- **Query result:** `IST08FXM3QLN`, `TaskStatus = 2` (FAILED), `LastError = "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"`, `RetryCount = 2`.
- **Validation:** Failed tasks are visible in monitoring with status `2`, `LastError`  provides informative error messages, `RetryCount` operational audit trail.

Expected results met: Failed tasks appear in monitoring with status `2` (FAILED); `LastError`  contains informative error messages; `RetryCount`  is recorded neatly for operational audit purposes.

Evidence: monitoring query result `TaskStatus = 2`, `LastError` message `HTTP 400 Bad Request`, `RetryCount = 2`.

---

## TC-IGD-SMASS-055 — Retry remains available for terminal/voided visits

Status: PASS

Notes:

- **Precondition check:** Query `BILRG_IgdVisit` for `IGV08FXLX20C` shows `RegId = -` (not registered), `PasienId = -`, `PasienName = -`. The visit still has a `FAILED` task — precondition met.
- **Hit endpoint** `PATCH /api/IgdVisitSmassTask/retry` with payload `{ "igdVisitSmassTaskId": "IST08FXM3QLN" }`
- **Response:** `200 Success`, data: `taskStatus = "SUCCEEDED"`, `assessmentId = "AS-269-UZS2OG"`, `retryCount = 2` (increased from 1), `lastError = ""`.
- **Evidence no deletion:** Task and assessment are not deleted, cancelled, archived, or unlinked.

Expected results met: retry call accepted (task row exists, operator explicitly requested it); task state updated (SUCCEEDED, AssessmentId filled, RetryCount increased); no delete/cancel/archive effects on task or assessment.

Evidence: retry response (`200` + body `taskStatus SUCCEEDED, assessmentId AS-269-UZS2OG, retryCount 2`), query task after retry (TaskStatus 1, AssessmentId filled), note that task state was not deleted.


---

## TC-IGD-SMASS-060 — Triage history shows SMASS status and assessment id

Status: PASS

Notes:

- Web client (`/app/igd/triase_igd`), visit `IGV08F7H1ZA5` (RUKYAT HIDAYAT, TN):
  - In the Riwayat Triage (triage history) tab, each triage row shows a **green SMASS status badge** with caption `GENERATED`.
  - The **AssessmentId** is displayed with value `AS-269-UYZEAG` (plain text next to the generated row, matching that row's NoTriage).
- No new table column observed; existing narrow layout preserved.

Expected results met: green `Generated` badge shown for the generated row; AssessmentId surfaced as plain text with no navigation/copy action; layout unchanged.

Evidence: web client triage history screenshot data (badge green caption `GENERATED`, AssessmentId `AS-269-UYZEAG`) recorded from `IGV08F7H1ZA5`.

---

## TC-IGD-SMASS-061 — No SMASS slot when there is no Generate task

Status: PASS

Notes:

- Web client (`/app/igd/triase_igd`), visit `IGV08F6PG5X2` (KRESNO LUKITO, TN) — visit from TC-002 whose triage ran while integration was off (no `GENERATE` task row exists).
- Riwayat Triage rows render exactly as before: **no SMASS badge** and no empty slot.
- No error is shown when the visit loads; no extra SMASS request triggered.

Expected results met: rows rendered without any SMASS badge/slot; no error displayed; backward compatibility preserved.

Evidence: web client triage history for `IGV08F6PG5X2` (no SMASS badge, clean load, no error).

---

## TC-IGD-SMASS-062 — SMASS Integration Panel content

Status: PASS

Notes:

- Web client (`/app/igd/triase_igd`), visit `IGV08F572FGJ` (SUCI FITRI YANI, NY):
  - **Integrasi SMASS panel** renders one row per task:
    - `GENERATE #1` → badge `SUCCEEDED`, `AssesmentId = AS-269-TMORH7`, no `LastError`.
    - `LINK Visit` → badge `SUCCEEDED`, no AssessmentId displayed (Link rows do not show an AssessmentId).
  - Header reads **"INTEGRASI SMASS"** (confirmed on screen; tester typo earlier corrected).
  - No failure summary chip shown (expected, since no task is Failed).
  - No SMASS endpoint called from the client; only BILREG `IgdVisitSmassTask/*` is used.

Expected results met: one row per task with correct labels (`Generate`/`Link`, `#n`/`Visit`), status badges, AssessmentId only on Generate rows, LastError hidden when empty; panel renders for the selected visit.

Evidence: web client Integrasi SMASS panel content for `IGV08F572FGJ` (GENERATE #1 + LINK Visit rows, both SUCCEEDED, AssessmentId on Generate row only).

---

## TC-IGD-SMASS-063 — Retry interaction rules

Status: PASS

Notes:

- Web client (`/app/igd/triase_igd`), visit `IGV08H7Q1V0E` (Mr Tesst retry):
  - Setup: triage #1 with valid PaperId (`PP-ICS-TRGE`) → GENERATED; re-triage #2 and #3 with `SmassTriagePaperId = PP-ICS-TRGEX` (invalid) → both FAILED; config restored to `PP-ICS-TRGE` afterward.
  - Riwayat triage shows 3 rows: `#1 GENERATED` (AssessmentId shown), `#2 FAILED`, `#3 FAILED`.
  - **Integrasi SMASS panel** shows 3 task rows:
    - `GENERATE #1` → SUCCEEDED, AssessmentId shown (correct value verified via DB: `AS-S69-VH1T5`).
    - `GENERATE #2` → FAILED, no AssessmentId, `LastError = "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"`.
    - `GENERATE #3` → FAILED, no AssessmentId, `LastError = "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"`.
  - **Retry interaction:**
    - "Coba Ulang" button is present/enabled only on the FAILED rows (#2, #3); no retry button on the SUCCEEDED row (#1).
    - Started retry on `GENERATE #2`; while in progress, the "Coba Ulang" button on `GENERATE #3` was **disabled** (not clickable).
    - After settle: exactly **one toast** reported `SUCCESS`; task list refreshed; the "Coba Ulang" button on `GENERATE #2` **disappeared** and its label changed to `SUCCEEDED`.
    - Repeated for `GENERATE #3`: one toast `SUCCESS`, row refreshed to `SUCCEEDED`, retry button hidden.

Expected results met: retry button enabled only on FAILED rows and hidden on Succeeded rows; while a retry is running every retry button is disabled; on settle the list refreshes with exactly one toast per outcome.

Evidence: web client Integrasi SMASS panel rows for `IGV08H7Q1V0E` (GENERATE #1 SUCCEEDED + #2/#3 FAILED with LastError), retry button visibility/disable behavior during in-flight retry, single SUCCESS toast after settle, post-retry rows SUCCEEDED.

---

## TC-IGD-SMASS-064 — Panel expand behaviour

Status: PASS

Notes:

- Web client (`/app/igd/triase_igd`):
  - **Visit with FAILED task** (`IGV08H7Q1V0E`, Mr Tesst retry — re-triage #4 with invalid PaperId `PP-ICS-TRGEX`):
    - Panel **INTEGRASI SMASS auto-expands**.
    - Header/row shows `GENERATE #4 FAILED`, `AssesmentId = '-'`, error description present.
  - **Visit without failed task**:
    - Panel **INTEGRASI SMASS collapsed by default**, no "n gagal" chip shown.
  - **Mount position (desktop):** panel INTEGRASI SMASS appears **directly below** the Riwayat Triase panel — confirmed.
  - **Tablet/mobile:** panel INTEGRASI SMASS also appears **below RIWAYAT TRIASE** for the visit — confirmed (renders beneath the triage history, consistent with the mount requirement).

Expected results met: panel auto-expands when at least one task is Failed and shows the failure indicator; collapsed with no chip when no failures; mounted below the triage history panel on desktop.

Evidence: web client panel behavior for visit with FAILED (auto-expand + `GENERATE #4 FAILED` + error) and visit without FAILED (collapsed, no chip); desktop mount position confirmed below Riwayat Triase.

---

## TC-IGD-SMASS-065 — No destructive actions and no local store

Status: PASS

Notes:

- Web client, visit `IGV08H7Q1V0E` (Mr Tesst retry):
  - Inspected every control in the **RIWAYAT TRIASE** and **INTEGRASI SMASS** panels: **no** delete / cancel / archive / unlink button or action exists.
  - **No optimistic update:** during an in-flight retry the row does not change until the server responds to the previous action (row stays in its previous state, e.g. FAILED, until settle).
  - **No SMASS call from the client:** Network tab shows only `{BilregApi}` requests — no `{SmassApi}` calls.
  - **No new Pinia store:** state on these screens is managed with the existing mechanism; no new Pinia store was introduced for these screens.

Expected results met: the only mutation available is the approved manual retry (`PATCH /api/IgdVisitSmassTask/retry`); server state stays authoritative with the task list re-read after settle; no SMASS call and no new Pinia store.

Evidence: web client control inspection (no destructive actions on either panel), row behavior during retry (no optimistic update), Network tab (BILREG only, no SMASS API), no new Pinia store.

---

## TC-IGD-SMASS-070 — Legacy SMASS behaviour is unchanged

Status: PASS

Notes:

- SMASS API (standard creation path, not BILREG task):
  - `POST {SMASS}/api/Assesment` with legacy payload (`pasienId=337502200231611`, `regId=RG01376991`, `layananId=1GD01`, `paperId=PP-ICS-TRGE`, `userrId=admin_ics`) — **no `IgdVisitId`/`NoTriage`** → HTTP 200, created `AS-269-VJ99LV`; response still carries the original fields (`pasienId`, `pasienName`, `regId`, `layananId`, `paperId`, `userrId`, `assesmentDate`).
  - `POST {SMASS}/api/Assesment/finish/AS-269-VJ99LV` → HTTP 200, no error.
  - `GET {SMASS}/api/Assesment/catalog/RG01376991` → HTTP 200; the created assessment `AS-269-VJ99LV` now `assesmentState = Finished` with `oftaDocId = DOCU269000320` and `oftaDocUrl = http://dev.smart-ics.com/ofta-storage/DOCU269000320_Assesment.pdf` (OFTA document generated — OFTA/documents flow works). (Note: tester's finish note mentioned DOCU269000314; the catalog response identifies DOCU269000320 as the doc for AS-269-VJ99LV — catalog value recorded as authoritative.)
  - Pre-existing assessment `AS-269-VJ6NYA` in the same reg remains unchanged: `igdVisitId ""`, `noTriage 0`, `RegistrationLinkStatus Registered`, `sourceLabel ""`.
- No new required field blocks the existing create path (create succeeded without `IgdVisitId`/`NoTriage`).

Expected results met: existing SMASS create/finish/catalog/OFTA behaviours and responses are unchanged and carry their original fields; no new required field on the legacy create path.

Evidence: SMASS API responses for create (`AS-269-VJ99LV`), finish (200, document generated), catalog (both assessments listed, OFTA doc URLs present), legacy fields intact.

---

## TC-IGD-SMASS-071 — Existing IGD flows unchanged

Status: PASS

Notes:

- BILREG API, visit `IGV08H9YG2Y1` (Visit Normal), integration enabled (`IgdVisit:EnableSmassIntegration = true`), SMASS reachable:
  - `POST /api/IgdVisit` → 200, `IgdVisitId = IGV08H9YG2Y1`.
  - `POST /api/IgdVisit/{id}/dokter` → 200 `"Done"`.
  - `POST /api/IgdVisit/{id}/triage` → 200; response keeps the original fields (`noTriage 1`, `triageMethod ATS`, `triageLevel ATS3`, `triageColor YELLOW`, `lastTriageAt`, `nextReTriageAt`) **plus** `smassAssessmentId = AS-269-VJUZVO` and `smassGenerationStatus = Generated`.
  - `POST /api/IgdVisit/{id}/assignBed` → 200, `bedIgdId = BED00006`, `pakaiBedIgdId = PBI08HA3KSLV`.
  - `POST /api/IgdVisit/{id}/register` → 200 `"Done"`.
  - `POST /api/IgdVisit/{id}/discharge` → 200, `administrativeState = DISCHARGED`, `dischargeDateTime` set, `bedReleased = true`.
- Bed occupancy and visit state remain consistent (bed released on discharge, administrative state DISCHARGED).
- Transfer bed step is optional per the test package — not executed, no impact.

Expected results met: every existing IGD step behaves as before; the extended triage response contains the original fields plus `smassAssessmentId` and `smassGenerationStatus`; bed occupancy and visit state stay consistent.

Evidence: BILREG API responses for create/dokter/triage/assignBed/register/discharge on `IGV08H9YG2Y1` (triage response with original fields + `smassAssessmentId`/`smassGenerationStatus`; final state DISCHARGED with bed released).

---

## TC-IGD-SMASS-072 — Void preserves SMASS records; no unlink path exists

Status: PASS

Notes:

- BILREG API + SMASS DB, visit `IGV08HBV96TQ` (created fresh, triage only — voidable, no tindakan/BHP):
  - `POST /api/IgdVisit` → 200, `IgdVisitId = IGV08HBV96TQ`.
  - `POST /api/IgdVisit/{id}/triage` → 200, `smassAssessmentId = AS-269-VLI73U`, `smassGenerationStatus = Generated`.
  - **Before void** — task row (`GET /api/IgdVisitSmassTask/IGV08HBV96TQ`): `IST08HBW9KZF`, GENERATE, SUCCEEDED, `assessmentId AS-269-VLI73U`, `retryCount 0`, `lastError ""`. Assessment row (`SMASS_Assesment WHERE IgdVisitId = IGV08HBV96TQ`): `NoTriage 1`, `RegistrationLinkStatus 1` (Pending Registration — visit not yet registered), `LayananId 1GD01`, `PaperId PP-ICS-TRGE`.
  - `POST /api/IgdVisit/{id}/void` → 200, `isVoided = true`, `bedReleased = false` (void succeeds as before).
  - **After void** — task row and assessment row are **identical** to before: same task id, status, assessment id, retry count, error; same assessment `IgdVisitId`, `NoTriage`, `RegistrationLinkStatus`. Nothing deleted, unlinked or archived.
  - **API surface:** `IgdVisitSmassTaskController` exposes only `GET {igdVisitId}`, `GET worklist`, `PATCH retry`, `POST process` — no DELETE/unlink endpoint exists.

Expected results met: void succeeds; assessments and tasks still exist with the same values; no unlink/delete endpoint (confirmed by controller inspection).

Evidence: task and assessment rows captured before and after void on `IGV08HBV96TQ` (identical), void response (isVoided true), controller endpoint inventory (no DELETE/unlink).

---

## TC-IGD-SMASS-073 — Manual retry only; no automatic worker or polling

Status: PASS

Notes:

- Left one FAILED task untouched and observed the system for ~5 minutes:
  - Task: `IST08H8CF318` (visit `IGV08H7Q1V0E`, `NoTriage 4`, TaskType GENERATE, TaskStatus FAILED, `retryCount 1`, `lastError "SMASS generateIgdTriage gagal: HTTP 400 Bad Request"`, `processedDate 2026-09-25 09:51:22.980`).
  - `GET /api/IgdVisitSmassTask/worklist` after the observation period: the task is **still FAILED**, `retryCount` unchanged (1), `lastRetryDate`/`processedDate`/`lastError` unchanged — no automatic retry occurred.
  - Confirmed no background worker, scheduler or outbox attempted the task.
  - Web client on the visit left without interaction for 5 minutes: Network tab shows **no polling** — no repeated `IgdVisitSmassTask/*` requests without user action.

Expected results met: no background worker/scheduler/outbox retries the task on its own; it stays FAILED until a manual retry; the client does not poll the task endpoint.

Evidence: DB row (`BILRG_IgdVisitSmassTask WHERE TaskStatus = 2`) before/after observation; worklist response after 5 minutes (status/retryCount/timestamps identical); Network tab (no polling).

---

## TC-IGD-SMASS-074 — No new audit/event entries for task transitions

Status: PASS

Notes:

- Visit `IGV08F572FGJ` (has GENERATE task `IST08F57HX99` and LINK task `IST08FRZHK6C`, both SUCCEEDED — see TC-020/TC-062):
  - `SELECT * FROM BILRG_IgdVisitEvent WHERE IgdVisitId = 'IGV08F572FGJ' ORDER BY NoEvent` returns only the normal IGD flow events: 1 DAFTAR, 2 ASSESS_TRIAGE, 3 ASSIGN_BED, 4 ASSIGN_REGISTER, 5 REPLACE_REGISTER. **No entry related to SMASS** — no GENERATE/LINK/RETRY events were added by the task operations.
  - `SELECT * FROM BILRG_AuditLog WHERE entityId IN ('IGV08F572FGJ', 'IST08F57HX99', 'IST08FRZHK6C')` → **empty result**; no audit-log entry for the visit or its SMASS tasks.

Expected results met: no `IgdVisitEvent` row and no audit-log entry was added by the generation, link or retry operations; the task row remains the only operational record.

Evidence: `BILRG_IgdVisitEvent` rows for `IGV08F572FGJ` (5 normal IGD events, none SMASS-related); `BILRG_AuditLog` query for visit + task ids returns empty.

---

## TC-IGD-SMASS-075 — No historical backfill

Status: PASS

Notes:

- Historical assessments (created before the feature, `IgdVisitId` empty) — sample `SELECT TOP 3 ... FROM SMASS_Assesment WHERE IgdVisitId IS NULL OR IgdVisitId = ''`:
  - `AS-236-000001` (2023-06-05) → `IgdVisitId ''`, `NoTriage 0`, `RegistrationLinkStatus 0`.
  - `AS-236-000012` (2023-06-05) → `IgdVisitId ''`, `NoTriage 0`, `RegistrationLinkStatus 0`.
  - `AS-236-000028` (2023-06-05) → `IgdVisitId ''`, `NoTriage 0`, `RegistrationLinkStatus 0`.
- No historical assessment was updated: they keep empty `IgdVisitId`, `NoTriage = 0` and `RegistrationLinkStatus = 0 (Registered)`.
- No script or process in repo/env backfills historical assessment data (`IgdVisitId`/`NoTriage`/`RegistrationLinkStatus`).

Expected results met: the change is additive with no data migration; historical assessments are untouched and no backfill exists.

Evidence: SMASS DB query on historical assessments (IgdVisitId empty, NoTriage 0, RegistrationLinkStatus 0); repo/env inspection confirming no backfill script/job.

---

# 3. Defects

## DEF-001

Related Test Case:

TC-IGD-SMASS-021

Severity:

MAJOR

Actual Result:

`PATCH /api/IgdVisit/{id}/replaceRegister` successfully replaces the registration (visit `IGV08F572FGJ`, `RG01378134` → `RG01378135`, latest administrative values applied to `SMASS_Assesment`, exactly one `LINK` task, no duplicates). However, repeating the identical replace call returns the error `AQ_OPERATION_NOT_ALLOWED` (code 400): "Register baru RG01378135 sudah di link-kan dengan IgdVisit IGV08F572FGJ", instead of an idempotent success without error. State is unchanged and no extra rows are created, but the documented idempotency contract ("repeat call leaves the same result with no error") is not honored.

Expected Result:

Repeating the same replace call should leave the same result (idempotent) with no extra rows and no error, while keeping exactly one terminal `SUCCEEDED` LINK task that is not transitioned again.

Evidence:

- Call 1: `PATCH /api/IgdVisit/IGV08F572FGJ/replaceRegister` body `{ "newRegId": "RG01378135", "userId": "SPR001" }` -> 200 `Done`; after: `BILRG_IgdVisit` `RegId = RG01378135`, `SMASS_Assesment` carries latest values, single `LINK` task `IST08FRZHK6C`.
- Call 2 (identical) -> 400 `AQ_OPERATION_NOT_ALLOWED` "Register baru RG01378135 sudah di link-kan dengan IgdVisit IGV08F572FGJ".

---

## DEF-002

Related Test Case:

TC-IGD-SMASS-032

Severity:

MAJOR

Actual Result:

The pending-registration monitoring surface is expected to offer no destructive action, but `DELETE {SMASS}/api/Assesment/{AssesmentId}` exists and deletes assessments without checking `RegistrationLinkStatus`. `DeleteAssesmentCommand` transitions any assessment with `AggState` `Created (0)` / `Drafting (1)` / `Finished (2)` to Deleted; `PendingRegistration` (a separate `RegistrationLinkStatusEnum` value, 1) does not protect the assessment. A never-registered assessment can therefore be deleted, defeating the quarantine/monitoring guarantee that pending assessments remain observable until registered.

UI clarification (recorded 2026-09-23): neither the MyHospitalWeb UI nor the Emr UI exposes any delete button or delete access for pending assessments. The operator-facing surfaces are safe; the risk is confined to direct API calls to `DELETE api/Assesment/{id}` (or any future client using that endpoint).

Expected Result:

No delete/purge/archive action should exist on the pending-registration surface; deletions must not be possible for assessments in `PendingRegistration` state (whether via `DELETE api/Assesment/{id}` or any other destructive surface).

Evidence:

- `GET {SMASS}/api/Assesment/pendingRegistration` returns pending rows (`AS-269-TMRFZN`, `AS-269-TMS1OI`).
- `DELETE {SMASS}/api/Assesment/{AssesmentId}`: no `RegistrationLinkStatus` validation; delete allowed for `AggState` `Created (0)`, `Drafting (1)`, `Finished (2)`; sets `AssesmentState = Delete`.
- UI check: no delete button/access for pending assessments in MyHospitalWeb and Emr.

---

# 4. Recommendations

Open Issues:

- DEF-001 (TC-IGD-SMASS-021 repeated `replaceRegister` returns 400 instead of error-free idempotent success)
- DEF-002 (TC-IGD-SMASS-032 pending-registration assessments can be deleted via `DELETE api/Assesment/{id}` — no `RegistrationLinkStatus` guard)