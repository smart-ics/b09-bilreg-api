# Phase 3 — Booking Self-Registration assistance receive-side closure

**Status:** Implemented in source (2026-07-23)  
**Plan:** [TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md](./TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md) Phase 3  
**Decision:** Receive-side closure only (no in-repo Self-Registration decision command)  
**Prerequisite:** Phase 1 Slice 1 real-SQL gate; R-11 ensure API

## Outcome

Patient Tracker continues to expose `POST /api/v1/admission-queue/booking-assistance` as the narrow
ensure contract for an accountable external `AssistanceRequired` result. Duplicate business requests
converge on one active assistance row (`Existing=true`). Bilreg invents no business reason codes.
Self-Registration success and transient-failure paths remain the responsibility of the external
HiDok/Admisi caller and are documented as required consumer-contract tests outside this repository.

## What shipped

### 1. Ensure-path verification (no production defect found)

| Artifact | Role |
|----------|------|
| `BookingAssistanceIntakeHandler` | Ensure one active assistance; derived correlation |
| `BookingAssistanceRepo.TryCreate` | Local transaction create; unique race → `false` |
| `AdmissionQueueV1Controller.BookingAssistance` | Existing v1 receive-side route (unchanged) |

Audited behaviors already present:

- Inactive / retired Service Point rejected via `EnsureCanAcceptIntake` before allocate/create
- Sequence exhaustion propagates as `SequenceExhaustedException` before `TryCreate`
- Concurrent loser reloads winner and returns `Existing=true` without claiming a new entry
- `failureCode` stored as opaque caller audit text only

No production code change was required.

### 2. Receive-side test coverage

Extended [`BookingAssistanceIntakeTest.cs`](../../src/bilreg/Bilreg.Test/AdmisiContext/AntrianFeature/BookingAssistanceIntakeTest.cs):

| Scenario | Coverage |
|----------|----------|
| New assistance + derived correlation + QueueLabel | Existing unit |
| Existing assistance without allocate/`TryCreate` | Existing unit |
| Concurrent loser → `Existing=true` | Existing unit + real-SQL race |
| Inactive Service Point rejects before mutate | **Added** |
| Sequence exhaustion propagates; no `TryCreate` | **Added** |

Real-SQL unique race remains in `AdmissionQueueRealSqlGateTest.BookingAssistance_UniqueRace_OneActive_LoserExisting`.

### 3. External caller contract documentation

[`TRACKER-ADMISSION-QUEUE-API-V1.md`](./TRACKER-ADMISSION-QUEUE-API-V1.md) now includes a dedicated
**Booking assistance / HiDok consumer contract** section: when to call, request/response, error
taxonomy, and required consumer-contract tests owned by HiDok/Admisi.

## Explicitly not done (by design)

- In-repo Admisi Self-Registration decision command or new `/api/v1/admisi-rajal` mutation route
- In-repo `success/no-entry` / transient/no-entry orchestration tests (external consumer ownership)
- Wiring `RegJalanByBookingCmd` or `BookingCreateFromHidok` to assistance intake
- R-05B `ClientRequestId` / Kiosk transport idempotency
- Phase 4 SignalR publisher
- Phase 5 rollout package

## Revised Phase 3 exit checklist

| Criterion | Result |
|-----------|--------|
| Patient Tracker accepts accountable `AssistanceRequired` ensure requests | Yes (existing v1 route + handler) |
| Duplicate requests converge safely (`Existing=true`, one active) | Yes (unit + real-SQL race) |
| No business codes invented in Bilreg | Yes (`failureCode` opaque; no catalog) |
| External caller owns success/transient non-invocation | Yes (documented consumer contract) |
| Focused ensure-path tests green | Yes (see evidence below) |

## Test evidence

Focused filter (2026-07-23):

`BookingAssistanceIntakeTest|AdmissionQueueApiContractTest`

**Passed: 13, Failed: 0**

Coverage includes BookingAssistanceIntake unit scenarios (create, existing, concurrent
loser, inactive Service Point, sequence exhaustion) plus Admission Queue API contract
checks (including `BookingAssistance` route presence).

Real-SQL unique race remains available when the integration connection is configured:

`AdmissionQueueRealSqlGateTest.BookingAssistance_UniqueRace_OneActive_LoserExisting`

## External consumer-contract obligations (HiDok / Admisi)

1. Successful Self-Registration never calls booking-assistance.
2. Transient / uncertain failure never calls booking-assistance.
3. Definitive `AssistanceRequired` calls the ensure route and handles new vs `Existing=true`.
4. Inactive Service Point and sequence-exhaustion errors do not create a second obligation.

## Suggested commit message

```
feat(admission-queue): Phase 3 booking-assistance receive-side closure

Harden ensure-path test coverage for inactive Service Point and sequence
exhaustion; document external HiDok AssistanceRequired caller contract.
```
