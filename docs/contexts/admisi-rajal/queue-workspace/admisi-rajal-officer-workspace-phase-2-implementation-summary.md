# Admission Officer Workspace — Phase 2 Implementation Summary

**Implemented:** 2026-07-27  
**Phase:** Pinned Calling tile and interaction alignment

## Outcome

Calling Mode now has one pinned active queue tile. It is removed from the virtual worklist and
contains the only Calling action surface: `Panggil Lagi`, `Tidak Hadir`, and `Hadir`.

`Tidak Hadir` continues to invoke the existing Return-to-Waiting command. It releases the
Outstanding claim and keeps the patient in the queue; it does not invoke terminal No Show.

## Changes by repository

### `c012_myhospital_web`

- Added tile presentation states for Ready, active Calling, and locked worklist entries.
- Ready tiles offer read-only preview plus `Panggil`; Calling actions appear only on the pinned
  active tile, using the authoritative current-Loket session.
- Filtered the active Calling item from the virtual worklist without changing its scroll reset
  trigger. Other entries remain visible but their preview and action controls are truly disabled.
- Locked the worklist in Calling and Queued Registration modes, leaving the registration panel as
  the only Queued Registration work surface.
- Reduced `CurrentLoketSession` to an identity/status summary for Outstanding claims. The existing
  In Service `Resume Processing` path remains available.
- Added a current-session Start Service command route so `Hadir` uses the authoritative active
  claim rather than a stale preview selection.
- Updated focused tile and current-session tests for Indonesian labels and the single action
  surface.

## Intentional non-changes

- No backend endpoint, database, domain-model, persistence, or Queue Display change.
- No Direct Registration behavior, Registration Not Established action, or Queue Closing behavior.
- Existing queue mutation clients remain authoritative; this phase changes presentation and event
  routing only.

## Validation

| Check | Result |
|---|---|
| Targeted Prettier | Passed for modified Vue, TypeScript, and test files. |
| `pnpm tc:app` | Blocked before type-checking: pnpm refused its configured version because the registry signature could not be verified in this environment. No dependency/configuration change was made to bypass it. |
| Focused Vitest | Attempted directly with the two changed component specs; it produced no output and remained stalled for more than 80 seconds, then was stopped. |
| Lint | Not run because the pnpm runtime gate prevents the project script from starting; required in a healthy frontend toolchain. |
| `git diff --check` | Passed for the frontend repository. |

## Suggested commit

- `c012_myhospital_web`: `feat(admisi): pin calling queue tile and align officer actions`

No commit is required for `b09-bilreg-api` or `c013-kiosk-queue-display-web`.
