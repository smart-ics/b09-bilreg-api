# Admission Officer Workspace — Phase 4 Implementation Summary

> **Historical behavior superseded — 2026-07-27:** `LegacyAutoComplete` and synthetic
> context-free Admission Queue completion described below have been removed. All Rajal
> registrations without queue context are now queue-less; historical records are unchanged.

**Implemented:** 2026-07-27  
**Phase:** Additive backend support for queue-less Direct Registration

## Outcome

`b09-bilreg-api` now provides explicit queue-less outpatient Registration creation routes for
walk-in and booking workflows. Direct Registration keeps normal Registration side effects but
creates no Admission Queue entry, Loket claim interaction, or Registration Outcome.

Existing Registration create routes remain compatible: complete Admission Queue context uses the
existing queue-linked atomic completion flow, while no meaningful context retains the historical
synthetic Admission Queue behavior.

## Changes

### `b09-bilreg-api`

- Added the transient, JSON-ignored `RegistrationAdmissionQueueBehavior` application instruction:
  `LegacyAutoComplete`, `QueueLinked`, and `None`. It is not a persisted domain state or
  workspace mode.
- Added `POST /api/Reg/rajalWalkIn/direct` and `POST /api/Reg/rajalByBooking/direct`.
  Both return the existing JSend success envelope, require no Admission Queue workstation/Loket
  header, and reject queue context through the existing `ArgumentException` /
  `400 AQ_INVALID_REQUEST` path.
- Centralized route mapping so existing routes explicitly choose queue-linked or legacy behavior;
  partial queue context is rejected before workstation resolution.
- Added the queue-less handler branches. They preserve Registration, active-registration,
  billing, physician-queue, booking, print, journal, EMR-outbox, and Patient Tracker behavior,
  append `REGISTER` tracker evidence once, and skip Admission Queue completion/creation,
  Registration Outcome persistence, and Queue refresh publishing.
- Updated the canonical Admission Queue API contract with Direct route restrictions, behavior
  mapping, prospective-only semantics, and the no-migration rule.
- Added behavior-resolution and Direct-route controller contract coverage, including the no
  workstation-resolution and queue-context-rejection contracts.

## Validation

| Check | Result |
|---|---|
| `dotnet test ... --filter "FullyQualifiedName~AdmissionRegistrationQueueContextTest\|FullyQualifiedName~DirectWalkIn_\|FullyQualifiedName~DirectBooking_"` | Passed: 17 tests |
| Broader context/controller focused filter | 35 passed; 1 existing unrelated failure: `AdmissionQueueApiContractTest.RefreshHub_IsAuthorizedAndExposesStablePath` expects `[Authorize]` on `AdmissionQueueRefreshHub`. This change did not modify the SignalR hub. |
| `git diff --check` | Passed |

## Environment-gated verification

The disposable Admission Queue SQL database was not configured, so direct persistence assertions
were not run against real SQL. With `BILREG_AQ_IT_*` configured for a disposable database, run:

```powershell
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmissionQueueRealSqlGateTest"
```

Verify separately for Direct walk-in and booking creation: Registration and one tracker `REGISTER`
event exist; no `BILRG_AdmAntrianEntry`, `BILRG_RegOutcome`, or Loket-current-call artifact is
created or changed; and legacy routes retain their synthetic/queue-linked behavior.

## Rollback

The routes are additive and initially unused. Rollback removes the Direct routes and their
transient handler branch without a schema migration. Registrations already created through Direct
remain valid and must not be retroactively assigned Admission Queue artifacts.

## Suggested commit

- `b09-bilreg-api`: `feat(admisi): add queue-less direct registration endpoints`

No change is required in `c012_myhospital_web` or `c013-kiosk-queue-display-web` for Phase 4.
