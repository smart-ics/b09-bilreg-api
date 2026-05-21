# lab-test-scenarios.md — Laboratory Workflow Feature

# 1. Overview

Document ini digunakan untuk:

* manual integration testing,
* operational workflow validation,
* dan maintainer onboarding.

Fokus utama:

* workflow state,
* integration behavior,
* dan business invariant.

---

# 2. Basic EMR Order Flow

## Scenario

Dokter membuat order lab dari EMR.

---

## Steps

1. Dokter membuat order di EMR
2. EMR hit API CreateOrder ke LWF
3. Lab Staff melakukan charge billing
4. Lab Staff collect specimen
5. Analyst input result
6. Pathologist verify result
7. Lab Staff release result

---

## Expected Result

| Step           | Expected           |
| -------------- | ------------------ |
| Create Order   | Status = Ordered   |
| Charge Success | Status = Charged   |
| Collection     | Status = Collected |
| Result Entry   | Status = Recorded  |
| Verification   | Status = Verified  |
| Release        | Status = Released  |

---

# 3. Deferred Workflow

## Scenario

Pasien rawat jalan harus puasa lebih dulu.

---

## Steps

1. Create order
2. Mark order as Deferred
3. Next day patient returns
4. Create new registration via REG
5. Continue workflow
6. Create billing charge
7. Continue normal process

---

## Expected Result

| Step              | Expected              |
| ----------------- | --------------------- |
| Deferred          | No collection allowed |
| Deferred          | No billing allowed    |
| Continue Deferred | New RegId attached    |
| Charge Success    | Status = Charged      |

---

# 4. External Patient Workflow

## Scenario

Pasien datang langsung ke laboratorium tanpa order dokter.

---

## Steps

1. Lab Staff create external patient order
2. LWF create registration to REG
3. REG returns RegId
4. LWF update patient snapshot
5. Lab Staff continue workflow

---

## Expected Result

| Step           | Expected              |
| -------------- | --------------------- |
| Initial Create | RegId = null          |
| REG Success    | RegId updated         |
| Workflow       | Can continue normally |

---

# 5. Billing Failure

## Scenario

Billing mapping tidak ditemukan.

---

## Steps

1. Create order
2. Request billing charge
3. BIL returns failure

---

## Expected Result

| Expected                      |
| ----------------------------- |
| Workflow blocked              |
| Status not changed to Charged |
| User receives error message   |
| User can retry after fix      |

---

# 6. REG Failure

## Scenario

REG gagal membuat registrasi.

---

## Steps

1. Create external patient order
2. Save workflow
3. REG returns failure

---

## Expected Result

| Expected                    |
| --------------------------- |
| Workflow blocked            |
| RegId remains null          |
| User receives error message |
| User can retry              |

---

# 7. OWR Async Send

## Scenario

LWF mengirim order ke OWR.

---

## Steps

1. Order reaches Charged/Collected state
2. Worker sends order to OWR
3. OWR returns success

---

## Expected Result

| Expected            |
| ------------------- |
| OwareStatus = Sent  |
| Retry queue cleared |

---

# 8. OWR Failure Retry

## Scenario

OWR unavailable.

---

## Steps

1. Worker sends order
2. OWR unavailable
3. Retry worker executes again

---

## Expected Result

| Expected             |
| -------------------- |
| OwareStatus = Failed |
| Retry still possible |
| Workflow not blocked |

---

# 9. Manual Result Entry

## Scenario

Analyst input result manually.

---

## Steps

1. Open Result Entry Mode
2. Input component result
3. Save result

---

## Expected Result

| Expected               |
| ---------------------- |
| ResultDocument created |
| ResultSource = Manual  |
| Status = Recorded      |
| Auto flagging executed |

---

# 10. Instrument Result Import

## Scenario

OWR sends analyzer result.

---

## Steps

1. OWR sends result
2. LWF match by OrderNo
3. Result imported

---

## Expected Result

| Expected                   |
| -------------------------- |
| ResultSource = Instrument  |
| Result recorded            |
| Component matching success |

---

# 11. Result Verification

## Scenario

Pathologist verifies result.

---

## Steps

1. Open ResultDocument
2. Verify result

---

## Expected Result

| Expected          |
| ----------------- |
| Status = Verified |
| Workflow locked   |
| Result immutable  |

---

# 12. Result Release

## Scenario

Verified result; BIL returns CLEAR on release attempt.

---

## Steps

1. `PATCH .../release` (release attempt)
2. Handler calls `ILabBillingIntegration.ValidateReleaseEligibility` (in-process)
3. BIL returns CLEAR → LWF executes release

---

## Expected Result

| Expected |
| -------- |
| HTTP 200; `released=true`, `billingStatus=CLEAR` |
| Status = Released |
| `LastBillingReleaseStatus` trace = Clear (audit only) |
| Result available to patient |

---

# 13. Release Blocked (operational outcome)

## Scenario

Verified result; BIL returns BLOCKED at release attempt.

---

## Steps

1. Verify result
2. `PATCH .../release`
3. BIL returns BLOCKED (not infrastructure failure)

---

## Expected Result

| Expected |
| -------- |
| HTTP 200; `released=false`, `billingStatus=BLOCKED`, `message` set |
| **No** exception UX for BLOCKED |
| Internal result still visible |
| Status remains Verified |
| Trace updated; stale trace cleared after amendment + re-verify |

---

# 14. Cancel Before Collection

## Scenario

Doctor cancels order before collection.

---

## Steps

1. Order created
2. Request cancellation

---

## Expected Result

| Expected           |
| ------------------ |
| Status = Cancelled |
| Workflow stopped   |

---

# 15. Cancel After Collection

## Scenario

Doctor tries to cancel after specimen collected.

---

## Steps

1. Order collected
2. Request cancellation

---

## Expected Result

| Expected              |
| --------------------- |
| Cancellation rejected |
| Workflow unchanged    |

---

# 16. Termination Workflow

## Scenario

Workflow terminated after collection.

---

## Steps

1. Order collected
2. Request termination
3. Input termination reason

---

## Expected Result

| Expected            |
| ------------------- |
| Status = Terminated |
| Reason stored       |
| Workflow stopped    |

---

# 17. Result Amendment

## Scenario

Verified result needs correction.

---

## Steps

1. Open verified result
2. Create amendment
3. Correct result
4. Verify corrected version

---

## Expected Result

| Expected                           |
| ---------------------------------- |
| New ResultDocument version created |
| Previous version unchanged         |
| Latest version becomes active      |
