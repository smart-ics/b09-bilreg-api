# Phase 2 — Officer-supporting backend contracts and Admisi enrichment

**Status:** Implemented in source (2026-07-23)  
**Plan:** [TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md](./TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md) Phase 2  
**Prerequisite:** Phase 1 Slice 1 real-SQL gate ([verification report](./tracker-admission-queue-phase1-slice1-verification-report.md))

## Outcome

Officer-facing backend contracts remain on `/api/v1/admission-queue`. Admisi Rajal now owns a
read-only enriched officer worklist composition route. NotEstablished ReasonCodes pass through an
explicit external catalog port without inventing business codes. Legacy AQ intake/start stay
feature-gated; physician/compat mutators are inventoried with observation logs.

## What shipped

### 1. ReasonCode catalog boundary

| Artifact | Role |
|----------|------|
| `IRegistrationOutcomeReasonCatalog` | External ownership port (`EnsureAccepted`) |
| `PassThroughRegistrationOutcomeReasonCatalog` | Default: non-empty only; no allowlist |
| `FinalizeRegistrationNotEstablishedHandler` | Calls catalog before InService check / finalize |
| DI in `ApplicationService` | Registers pass-through implementation |

Ops can replace the pass-through with an approved-catalog implementation later without changing
stored outcome semantics.

### 2. Admisi-owned enriched officer worklist

| Artifact | Role |
|----------|------|
| `AdmisiRajalOfficerWorklistQuery` / handler | Read composition over queue projection |
| `GET /api/v1/admisi-rajal/officer-worklist` | Authenticated HTTP surface |
| `IBookingAssistanceRepo.FindActiveByEntry` | Resolve BookingId for anonymous assistance rows |

Composition attaches nullable Identity / Booking / Registration summaries. Queue fields are copied
from `IAdmissionQueueOperationalProjection` and remain authoritative. No second ledger, no queue
writes.

`GET /api/v1/admission-queue/worklist` is unchanged and queue-only.

### 3. Legacy inventory and observation

| Route | Gate / observation |
|-------|-------------------|
| `POST /api/Antrian/anonymous-intake` | `LegacyEndpointsEnabled`; warning log |
| `POST /api/Antrian/start` | `LegacyEndpointsEnabled`; warning log |
| `PATCH .../mulaiPeriksa` | Ungated physician/compat; warning log |
| `PATCH .../selesaiPeriksa` | Ungated physician/compat; warning log |

Journey association remains on `/api/PasienTracker` (`candidates`, `resolve/select`, GET). Documented
in [TRACKER-ADMISSION-QUEUE-API-V1.md](./TRACKER-ADMISSION-QUEUE-API-V1.md).

## Explicitly not done (by design)

- Config allowlist for ReasonCodes
- Hard-coded business reason catalog values
- Officer / Kiosk / Display UI
- Phase 3 HiDok AssistanceRequired wiring
- Phase 4 SignalR publisher
- Phase 5 rollout package
- Feature-gating physician `mulaiPeriksa` / `selesaiPeriksa`

## Test evidence

Focused filter (2026-07-23):

`RegistrationOutcomeTest|AdmisiRajalOfficerWorklistQueryTest|AdmissionQueueApiContractTest|AdmissionQueueOperationalQueriesTest|BookingAssistanceIntakeTest`

**Passed: 28, Failed: 0**

Coverage includes:

- Pass-through catalog accepts arbitrary non-empty codes; rejects whitespace
- Catalog rejection short-circuits before `TryFinalize`
- Anonymous enrichment returns null sections; queue row intact
- Tracker BOOKING/REGISTER events compose Booking + Registration
- Assistance entry without Tracker resolves Booking
- Composition does not touch queue write ports
- Legacy gate 404 for intake and direct start when disabled
- New Admisi Rajal controller route/auth contract

## Phase 2 exit checklist

| Criterion | Result |
|-----------|--------|
| Officer v1 mutation/read contracts stable | Yes (unchanged surface; taxonomy/headers preserved) |
| NotEstablished uses `IRegistrationOutcomeReasonCatalog` | Yes (pass-through pending Ops catalog) |
| Enriched route is read-only composition | Yes |
| Queue-only worklist unchanged | Yes |
| Legacy intake/start gated; inventory documented | Yes |
| Focused tests green | Yes (28/28) |

## Suggested commit message

```
feat(admission-queue): Phase 2 officer contracts and Admisi enrichment

Wire ReasonCode catalog port (pass-through), add Admisi-owned
officer-worklist composition route, and inventory legacy AQ paths.
```
