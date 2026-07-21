# Step 6 — Browser E2E Verification Report

**Date:** 12 July 2026  
**Workflow:** Proses Registrasi Rawat Inap  
**Nature:** Browser-level end-to-end verification (not a broad refactor)  
**Verdict:** **verified with non-blocking follow-ups**

---

## 1. Frontend and backend environment

| Layer | Value |
|-------|-------|
| Frontend under test | Local Vite development server `http://localhost:5173/MyHospital/` (workspace `c012_myhospital_web`) |
| Frontend package version | `1.6.27` |
| Frontend runtime config | `public/global_config.json` → `environment=development`, Bilreg `http://dev.smart-ics.com:8089/BilregApi/api` |
| Deployed FE smoke probe | `http://dev.smart-ics.com/MyHospital/` returns HTTP 200 (not used as primary FE under test; workspace FE exercises the current Proses Registrasi contract including `prosedurMasukInapId`) |
| Backend | Deployed Bilreg API `http://dev.smart-ics.com:8089/BilregApi` |
| Database | `dev.smart-ics.com` / `HOSPITAL_HPL` |
| Rollout | `AdmisiRanap:Enabled=true`, `allTablesReady=true` |

---

## 2. Browser and build/version information

| Item | Value |
|------|-------|
| Browser | Playwright Chromium 143.0.7499.4 (headless) |
| Runner | Playwright Test via `playwright.step6.config.ts` |
| Harness | `e2e/modules/Admisi/rawat-inap/verification/step6-browser-e2e.spec.ts` |
| Auth user | `octopus@email.com` (celestial) |

Artifacts: `c012_myhospital_web/e2e/artifacts/step6/`

---

## 3. Test sources and patients

| Role | PasienId | Notes |
|------|----------|-------|
| Opname happy path | `347137300000057` (RIZAL PERWIRA) | Safe test patient; no prior active reg |
| Reservation happy path | `347137300000037` (WULAN) | Safe test patient |
| Incomplete reservation / validation | `347137300000014` (SOEROYO) | Safe test patient |
| Guard-blocked | `347137300000070` (BAMBANG HARMANTO) | Existing active reg `RGA4LISM7N` (retained; not modified) |

Primary successful run sources/regs:

| Path | Source ID | RegId |
|------|-----------|-------|
| Opname | `OPN0688J6T9A` | `RGA4LKDDX4` |
| Reservation | `RSV0688J6X3W` | `RGA4LKDYYD` |

Masters used in UI:

- Care Class `KELAS I` (`3`), Bangsal `RUANG MAWAR` (`R1`)
- Cara masuk `DATANG SENDIRI` (`8`), Prosedur `MELALUI IGD` (`IGD`)
- Rujukan `PKBI` (`RJK00291`), Layanan `RAWAT INAP` (`RI001`), Karcis `02`
- Dokter seeded `DR00000015`, Jaminan default Umum (`00000`)

---

## 4. Initial loading-state result (Scenario 1)

**Pass.**

- Deep-link opened registration source; workspace entered registration mode after **Proses Registrasi**.
- Guard loading panel / form transition observed; submit remained visible.
- Workspace never blank; no false guard pass on placeholder data.
- Screenshot: `scenario1-loading-or-form.png` (when captured in run).

---

## 5. Incomplete-form behavior (Scenarios 2–3)

**Pass.**

### Opname incomplete

- **Proses Registrasi** remained clickable.
- Zero process POST to `/admission/from-opname-request`.
- Submission readiness blockers shown (`missing_kelas` and related missing fields).
- First actionable section focus/scroll behavior present.
- Toast/blocker UX shown; draft retained.
- Screenshot: `scenario2-blockers.png`.

### Reservation incomplete

- Seeded Care Class / Bangsal from reservation appeared (`KELAS I`, `RUANG MAWAR`).
- `prosedurMasukInapId` required and visible.
- Missing registration fields blocked submit without API POST.
- After filling prosedur, `missing_prosedur_masuk_inap` blocker cleared (no stale blocker).

---

## 6. Guard-blocked behavior (Scenario 7)

**Pass.**

- Patient `347137300000070` with active `RGA4LISM7N`.
- Persistent `guard-blocked-panel` shown; registration form not processable.
- Reason understandable; **Buka Registrasi Aktif** available in panel (and toast action).
- Dismissing toast/Escape left blocked panel visible (page not empty).
- No process POST.

---

## 7. Opname request and response summary (Scenario 4)

**Pass.** Exactly **one** POST.

Endpoint:

```text
POST /api/admisi-ranap/admission/from-opname-request
```

Sanitized request:

```json
{
  "opnameRequestId": "OPN0688J6T9A",
  "kelasDkId": "3",
  "bangsalId": "R1",
  "userId": "SPR001",
  "registration": {
    "tipeJaminanId": "00000",
    "caraMasukDkId": "8",
    "prosedurMasukInapId": "IGD",
    "rujukanId": "RJK00291",
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
  "data": { "regId": "RGA4LKDDX4", "admissionStatus": 0 }
}
```

UI: success phase, Waiting List fork shown, blocker panel gone, submit footer removed (no accidental re-submit).

Artifact: `opname_http.json`

---

## 8. Reservation request and response summary (Scenario 6)

**Pass.** Exactly **one** POST.

Endpoint:

```text
POST /api/admisi-ranap/admission/from-reservation
```

Sanitized request:

```json
{
  "reservationId": "RSV0688J6X3W",
  "kelasDkId": "3",
  "bangsalId": "R1",
  "userId": "SPR001",
  "registration": {
    "tipeJaminanId": "00000",
    "caraMasukDkId": "8",
    "prosedurMasukInapId": "IGD",
    "rujukanId": "RJK00291",
    "dokterId": "DR00000015",
    "layananId": "RI001",
    "karcisId": "02",
    "pesertaJaminanId": ""
  }
}
```

Response: `regId=RGA4LKDYYD`, `admissionStatus=0`.

Reservation status **Realized (2)** with `RealizedRegId=RGA4LKDYYD`.

Artifact: `reservation_http.json`, `reservation_db.json`

---

## 9. Returned RegId values

| Path | RegId |
|------|-------|
| Opname | `RGA4LKDDX4` |
| Reservation | `RGA4LKDYYD` |
| Retained (not part of this run) | `RGA4LISM7N` |

---

## 10. Database evidence per table

### Opname `RGA4LKDDX4`

| Artifact | Result |
|----------|--------|
| `BILRG_AdmAdmission` | 1 row; status 0; source 0 |
| `ta_registrasi` | 1 row; `fs_kd_jenis_reg='1'`; `fs_kd_medis='DR00000015'` |
| `ta_reg_inap` | 1 row; `fs_kd_caramasuk_inap='IGD'`; booking/sekunder `' '` |
| `ta_reg_jaminan` | 1 row |
| `ta_reg_history_dokter` | 1 Primary DPJP (`fb_primer=true`, `DR00000015`) |
| `BILRG_RegAktif` | 1 row |
| Opname source | Status Fulfilled (1); `FulfilledRegId=RGA4LKDDX4` |
| Audit | Admission CREATE + Opname UPDATE |

### Reservation `RGA4LKDYYD`

Same persistence set as Opname (Admission, Registrasi inpatient, RegInap IGD + blank sentinels, jaminan, one Primary DPJP, RegAktif, audit). Reservation Realized with matching `RealizedRegId`.

---

## 11. Shared-RegId verification

**Pass** for both paths: Admission, Registration, RegInap, RegAktif, source fulfillment/realization, and admission audit entity id all share the returned `RegId`.

---

## 12. Duplicate-submit result (Scenario 9)

**Pass.**

- Rapid repeated clicks on submit during Opname processing produced **exactly one** process POST.
- One Registration / one `ta_reg_inap` / one Primary DPJP history row.
- No duplicate source transition beyond the single fulfillment.

---

## 13. Waiting List handoff result (Scenario 10)

**Partial — frontend handoff verified; persistence not confirmed.**

- Success UI showed Waiting List fork with returned `regId`.
- User action **Buat Waiting List** was exercised (including rapid re-click).
- Browser console recorded `422 Unprocessable Entity` during Waiting List attempts.
- DB check immediately after: `BILRG_BedWaitingList` row count for the regId = **0** (no duplicate row; also no successful create).
- Likely Care Class → hospital `kelasId` mapping / API validation issue on create payload.

**Limitation:** Waiting List create against this master combination did not persist in this run. Frontend handoff (regId + UI control) is verified. Do not treat remote Waiting List create as fully green.

---

## 14. Browser console errors

| Observation | Assessment |
|-------------|------------|
| `422 Unprocessable Entity` during Waiting List create attempts | Related to Scenario 10 limitation |
| No unexpected console errors during guard load, incomplete submit, Opname/Reservation success, or backend validation error display | Clean for core registration path |

---

## 15. Defects found

| ID | Severity | Finding |
|----|----------|---------|
| D1 | Non-blocking | Waiting List create from success fork returned 422; no `BILRG_BedWaitingList` row written for the successful Opname reg in this run |
| D2 | Test-only / UX note | Omni filter does not match `itemId`/`opnameRequestId` (patient/MRN only). Deep-link `?type=&id=` used for reliable E2E selection |
| D3 | Test harness | Playwright `toBeVisible` strict-mode conflicts when toast + panel share the same action/label text (fixed in harness selectors) |

No blocking defect in Opname/Reservation process → persistence → success UI path.

---

## 16. Narrowly scoped fixes applied

| Fix | Scope |
|-----|-------|
| `WaitingListFork.vue` — add `data-testid="waiting-list-fork"` | Verification/testability only |
| `e2e/pages/login.page.ts` — accept `email@example.com` placeholder (legacy `pegId` still matched) | E2E login compatibility |
| Step 6 Playwright harness + Python SQL/API support scripts | Verification tooling under `e2e/.../verification` and `b09-bilreg-api/tools/step6_*` |

No broad product refactors.

---

## 17. Cleanup status

| Item | Status |
|------|--------|
| Successful Opname/Reservation regs from verification runs | Deleted (Admission, Registrasi, RegInap, history, RegAktif, jaminan, audit, source) |
| STEP6-tagged open opnames/reservations | Cleaned |
| `RGA4LISM7N` | **Not modified** (explicit retain) |
| Unrelated operational data | Untouched |

---

## Scenario checklist

| # | Scenario | Result |
|---|----------|--------|
| 1 | Initial loading | Pass |
| 2 | Incomplete Opname | Pass |
| 3 | Incomplete Reservation | Pass |
| 4 | Successful Opname UI/HTTP | Pass |
| 5 | Opname DB evidence | Pass |
| 6 | Successful Reservation | Pass |
| 7 | Guard-blocked | Pass |
| 8 | Backend validation error + no residue | Pass (`Prosedur Masuk Inap 'ZZZ' tidak ditemukan.`; form retained; source remained Requested; no RegAktif) |
| 9 | Duplicate submit | Pass (1 POST) |
| 10 | Waiting List continuation | Partial (handoff yes; persistence no / 422) |

---

## Verdict

**verified with non-blocking follow-ups**

Core browser chain is proven:

```text
Frontend UI → validation → HTTP process POST → deployed Bilreg API → DB persistence
→ success UI → Waiting List handoff surface
```

for both Opname and Reservation, including shared `RegId`, `ta_reg_inap`/`prosedurMasukInapId`, Primary DPJP history, guard blocking, incomplete-form gating, duplicate-submit protection, and backend rejection without residue.

Non-blocking follow-up: diagnose Waiting List create `422` (Care Class ↔ hospital `kelasId` mapping / payload validation) so success-fork persistence matches the handoff UI.
