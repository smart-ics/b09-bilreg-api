# Step 4C — End-to-End and Database Verification Report

**Date:** 12 July 2026  
**Scope:** Inpatient admission-registration workflow (Opname Request + Reservation)  
**Code under test:** local `b09-bilreg-api` (current workspace) against `HOSPITAL_HPL`  
**Verdict:** **verified with non-blocking follow-ups**

---

## 1. Environment used

| Layer | Value |
|-------|-------|
| Database | `dev.smart-ics.com` / `HOSPITAL_HPL` (same as `Bilreg.Api` appsettings) |
| Local API under test | `WebApplicationFactory<Program>` with real persistence (`AdmissionRegistrationStep4CDbTest`) |
| Remote deployed API (smoke only) | `http://dev.smart-ics.com:8089/BilregApi/api` |
| Frontend | Unit tests for Proses Admisi success / double-submit gates |
| Auth | JWT signed with Bilreg `Jwt:Key` (tokens not retained in this report) |
| Feature flag | `AdmisiRanap:Enabled=true`; rollout status `allTablesReady=true` |

**Schema fix applied during preflight (blocking for live process):**

- `BILRG_AdmAdmission.AdmissionSource` was missing on `HOSPITAL_HPL`.
- Applied idempotent alter from [`BILRG_AdmAdmission.sql`](../../src/bilreg/Bilreg.SqlDb/AdmisiRanapContext/AdmissionFeature/BILRG_AdmAdmission.sql).
- Without this column, admission lookup/process fails with `Invalid column name 'AdmissionSource'`.

---

## 2. Test records and source paths exercised

| Path | Mechanism | Patients / notes |
|------|-----------|------------------|
| Opname Request happy path | Local real-DB HTTP integration | `347137300000057` — created, processed, cleaned |
| Reservation happy path | Local real-DB HTTP (create → maintain → process) | `347137300000037` — cleaned |
| Invalid prosedur | Local real-DB HTTP | `347137300000014` — source left Requested, cleaned |
| Forced RegInap rollback | Local real-DB HTTP + `FailingRegInapRepo` | `347137300000057` — cleaned |
| Reprocess fulfilled Opname | Local real-DB HTTP | `347137300000014` — cleaned |
| Remote smoke (stale binary) | Direct POST to deployed API | Opname `OPN065SPTG0Y` → `RegId=RGA4LISM7N` (**retained**; see §12) |

Masters used consistently:

- `kelasDkId=3`, `bangsalId=R1`/`R2`, `prosedurMasukInapId=IGD`
- `caraMasukDkId=8`, `rujukanId=00084`, `layananId=RI001`, `karcisId=02`, `tipeJaminanId=00000`

Automated suite:

```text
dotnet test --filter FullyQualifiedName~AdmissionRegistrationStep4CDbTest
Passed!  Failed: 0, Passed: 5, Skipped: 0
```

---

## 3. Actual HTTP request and response summaries

### Process from Opname Request

`POST /api/admisi-ranap/admission/from-opname-request`

Request (sanitized):

```json
{
  "opnameRequestId": "<generated>",
  "kelasDkId": "3",
  "bangsalId": "R1",
  "userId": "step4c",
  "registration": {
    "tipeJaminanId": "00000",
    "caraMasukDkId": "8",
    "prosedurMasukInapId": "IGD",
    "rujukanId": "00084",
    "dokterId": "DR00000015",
    "layananId": "RI001",
    "karcisId": "02",
    "pesertaJaminanId": ""
  }
}
```

Response:

```json
{
  "status": "success",
  "code": "200",
  "data": { "regId": "<shared RegId>", "admissionStatus": 0 }
}
```

Confirmed: authentication succeeds; single successful POST per workflow; body includes source id, `kelasDkId`, `bangsalId`, `userId`, registration fields, and `prosedurMasukInapId`.

### Process from Reservation

`POST /api/admisi-ranap/admission/from-reservation` — same nested `registration` shape with `reservationId`; response `{ regId, admissionStatus: 0 }`.

### Remote deployed smoke (contrast)

Same request shape against remote API returned `regId=RGA4LISM7N` successfully, but **did not** write `ta_reg_inap` / doctor history (remote binary predates Step 4B RegInap persistence). Local code under `WebApplicationFactory` **does** persist RegInap.

---

## 4. Database evidence for each persisted artifact

Verified by assertions in `Step4C_OpnamePath_PersistsSharedRegIdIncludingRegInapAndHistory` and reservation counterpart (exact counts = 1 unless noted):

| Artifact | Result |
|----------|--------|
| `BILRG_AdmAdmission` | Present; `AdmissionStatus=0`; `AdmissionSource=0` (Admission) |
| `ta_registrasi` | Present; `fs_kd_jenis_reg='1'` (inpatient) |
| `ta_reg_inap` | Present; `fs_kd_caramasuk_inap='IGD'`; booking/secondary = `' '` |
| `ta_reg_jaminan` | Present |
| `ta_reg_history_dokter` | One Primary DPJP row for selected doctor |
| `BILRG_RegAktif` | Present; same `RegId` |
| Opname source | `OpnameRequestStatus=1` (Fulfilled) + `FulfilledRegId` |
| Reservation source | `ReservationStatus=2` (Realized) + `RealizedRegId` |
| `BILRG_AuditLog` | Admission `CREATE` + source `UPDATE` |

---

## 5. Shared-`RegId` verification

Admission, Registration, RegInap, RegAktif, source fulfillment/realization, and audit entity ids all use the same generated `RegId` for each successful workflow (asserted in integration tests).

---

## 6. Repository round-trip result

After successful Opname process:

1. `RegInapRepo.LoadEntity` rehydrated same `RegId`, prosedur `IGD`, exactly one active Primary DPJP, no release date.
2. `SaveChanges` on the same model re-ran without creating a second `ta_reg_inap` or history row (`COUNT(*)=1` each).

---

## 7. Frontend success behavior

Covered by unit tests (executed 12 July 2026):

```text
pnpm exec vitest run useProsesAdmisi.spec.ts ProsesAdmisi.spec.ts
Test Files  2 passed (2)
Tests       44 passed (44)
```

Evidence in specs:

- Successful mutation sets `phase === 'success'` and retains `resultRegId`.
- Waiting List fork is shown on success (`renders waiting list fork on success phase`).
- Incomplete submit shows blockers/toast and skips POST.
- Double submission while pending executes **only one** mutation (`toHaveBeenCalledTimes(1)`).
- Submit disabled while `isSubmitting`; after success, form phase no longer re-posts.

Browser Network E2E against the **remote** deployed API was not used as the acceptance surface for RegInap because that binary is stale (see §11). Local HTTP+DB suite is the acceptance surface for persistence.

---

## 8. Invalid-request result

`prosedurMasukInapId=ZZZ`:

- HTTP non-success; body contains `Prosedur`.
- Opname remains `Requested`.
- No new `BILRG_AdmAdmission` for the patient (count unchanged).
- No RegAktif / RegInap residue for the attempt.

Client-side incomplete forms are gated before POST (unit-tested).

---

## 9. Actual transaction rollback evidence

Test: `Step4C_RegInapFailure_RollsBackAllCommittedWrites`

- Test-only `FailingRegInapRepo` throws `STEP4C_FORCED_REGINAP_FAILURE` inside ambient `TransHelper` after earlier writes would enlist.
- After failure:
  - `BILRG_AdmAdmission` for that opname id = **0**
  - Opname still `Requested`
  - No source `UPDATE` audit from `step4c`
  - `BILRG_RegAktif` for patient = **0**

This closes gap **G-07** with real SQL assertions (not mock call-order only).

---

## 10. Duplicate / retry result

| Scenario | Result |
|----------|--------|
| Frontend double-click while pending | One mutation (unit test) |
| Reprocess same fulfilled Opname | HTTP rejected with status/Requested/Fulfilled messaging; still exactly one `ta_reg_inap` for the first `regId` |
| Second active registration from same source | Not created |

---

## 11. Defects discovered and narrowly applied fixes

| Defect | Action |
|--------|--------|
| Missing `BILRG_AdmAdmission.AdmissionSource` on `HOSPITAL_HPL` | **Fixed** — applied existing SQL alter; required for any admission read/write |
| Remote deployed Bilreg API processes admission **without** writing `ta_reg_inap` / history | **Not a local code defect** — remote binary stale vs current workspace. Local code verified. **Follow-up: redeploy Bilreg.Api** |
| `ta_registrasi2` empty after new inpatient Reg | Known `RegRepo.SaveChanges` reloads komponen from DAL (empty for new id) then re-inserts empty list — **not fixed** (adjacent; does not block RegInap contract) |
| Cancel leaves RegAktif (G-01) | **Not fixed** — deferred; no cancelled rows observed in DB sample; code still cancels Admission only |

No broad refactors performed.

---

## 12. Remaining follow-up items

1. **Deploy** current Bilreg.Api (with RegInap orchestration + AdmissionSource schema) to `dev.smart-ics.com:8089` so UI E2E against remote matches local persistence.
2. **G-01** coordinated cancellation (Admission + Reg + RegAktif).
3. **`ta_registrasi2` / RegKomponen** write path in `RegRepo.SaveChanges` (use model `ListKomponen`, not DAL reload for new regs).
4. Intentionally retained remote smoke data (stale binary artifact):
   - `RegId=RGA4LISM7N`, pasien `347137300000070`, opname `OPN065SPTG0Y` Fulfilled
   - Has Admission/Reg/RegAktif/jaminan/audit; **missing** `ta_reg_inap`
   - Safe cleanup can delete these rows when ops approve; patient remains blocked for re-admission until cleaned or cancelled properly.

Integration test leftovers: cleaned automatically by `CleanupRegAsync` in `AdmissionRegistrationStep4CDbTest`.

---

## Verdict

**verified with non-blocking follow-ups**

Local application stack + `HOSPITAL_HPL` prove Opname and Reservation paths, shared `RegId`, RegInap/DPJP persistence, repository round-trip, validation rejection, real transactional rollback, and reprocess rejection. Non-blocking follow-ups: remote redeploy, `ta_registrasi2`, G-01 cancellation, and cleanup of retained smoke `RGA4LISM7N`.
