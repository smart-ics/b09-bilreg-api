# Admisi Rajal High-Density Worklist — Phase 9 Implementation Summary

**Date:** 2026-07-25  
**Phase:** 9 — Controlled rollout and cleanup

## Completed work

- Added the frontend runtime `admissionQueue.rolloutStage` contract with `pilot`, `expansion`, and `generalAvailability` values. Existing deployed configs that do not contain the new field resolve to `generalAvailability` to preserve their current enabled/fallback behavior.
- Set the checked-in development runtime configuration to `generalAvailability` and retained `enabled` as the immediate kill switch plus `useLegacySidebarFallback` as the reversible frontend fallback.
- Added non-PII support context to the officer toolbar: rollout stage, workstation, and Loket.
- Prevented embedded Registration Assistance from registering or responding to the legacy `Ctrl+K` deep-search shortcut. In the high-density workspace, `Ctrl+K` remains reserved for the universal resolver while an authoritative Processing session is active; the standalone legacy workspace preserves its shortcut.
- Updated the Admisi user documentation, Rajal SOP, changelog, and Admission Queue operations runbook with staged rollout, smoke, seven-day stabilization, conflict, unresolved-context, fallback, and legacy-retirement procedures.

## Architectural decisions

- Rollout remains global by approval. `rolloutStage` communicates the current operational stage; it intentionally does not introduce role, workstation, service-point, or percentage authorization logic.
- Stage is diagnostic/support context only. No patient, search, Booking, Registration, queue, or workstation value is sent to telemetry by this change.
- Legacy code and backend compatibility remain deployed after General Availability. The fallback remains the fast rollback mechanism while queue and Registration data stay authoritative on the backend.

## Deviations and remaining release-gate work

- No backend API, database migration, dashboard exporter wiring, or alert-rule implementation was added. Phase 8 identified these as environment-owned release gates, and Phase 9 only documents their required evidence.
- The checked-in development config is marked General Availability by the approved assumption. Production stage changes, pilot training, operational sign-off, authenticated browser checks, real-SQL measurements, and the seven-day evidence archive must be completed by Operations before production General Availability is asserted.
- Legacy component deletion, endpoint deprecation, and schema removal are intentionally deferred to a separately approved cleanup change after usage evidence and rollback assessment.

## Verification

- Targeted frontend unit tests cover rollout-stage parsing/default compatibility, toolbar support context, unchanged kill-switch/fallback rules, and standalone versus embedded legacy deep-search shortcut behavior.
- Run the repository TypeScript check, targeted lint, focused tests, and production build before merge; browser, accessibility, production telemetry, and SQL evidence remain external gates.

## Recommended commits

### `c012_myhospital_web`

```text
feat(admisi): add controlled admission-queue rollout support

- add global pilot, expansion, and general-availability runtime stages
- show non-PII rollout, workstation, and Loket support context
- reserve embedded Ctrl+K for universal patient-context resolution
- document officer workflow, fallback, and conflict recovery
```

### `b09-bilreg-api`

```text
docs(admission-queue): add phase 9 rollout and recovery runbook

- define pilot, expansion, and seven-day stabilization gates
- document stage smoke tests, conflict recovery, fallback, and legacy retirement criteria
- add the Phase 9 implementation summary and release-gate handoff
```
