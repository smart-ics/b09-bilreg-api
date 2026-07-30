# Admission Officer Workspace — Phase 1 Implementation Summary

**Implemented:** 2026-07-27  
**Phase:** Explicit frontend workspace-mode projection

## Outcome

Phase 1 adds the four frontend-only Admission Officer workspace modes without changing backend
contracts, queue transitions, persisted state, action placement, or Queue Display behavior.

`Ready`, `Calling`, and `Queued Registration` are now derived from authoritative current-Loket
truth. `Direct Registration` exists as a frontend projection but remains inactive: its entry action
and persistence behavior belong to later phases.

## Changes by repository

### `c012_myhospital_web`

- Added `admisiRajalWorkspaceMode.ts`, containing the `AdmisiRajalWorkspaceMode` type, a pure
  `deriveAdmisiRajalWorkspaceMode` projection, and the
  `useAdmisiRajalWorkspaceMode` controller composable.
- Exposed `isLoketAvailabilityKnown` from `useOfficerAdmissionQueue` so the route never derives
  `Ready` while current-Loket truth is loading or unavailable.
- Integrated the projection into `RegistrasiRajal.vue` without exposing a debug indicator.
  Authoritative active sessions now re-associate their queue item after reload/refetch, while the
  existing Registration Assistance state machine remains unchanged.
- Kept panel behavior subordinate to the workspace mode: Ready and Calling are read-only;
  Queued Registration is editable through the existing flow; Direct Registration has no active UI
  entry in this phase.
- Added projection tests for every stable result, unresolved truth, precedence over local Direct
  intent, and reactive claim updates.

## Projection precedence

| Current-Loket truth | Local Direct intent | Workspace mode |
|---|---:|---|
| Unknown/loading/error | Any | Neutral/unresolved shell (no mode) |
| Outstanding claim | Any | Calling |
| InService claim | Any | Queued Registration |
| No active claim | Active | Direct Registration |
| No active claim | Inactive | Ready |

The first two claim rows take precedence over local intent. No workspace mode is stored locally,
sent to an API, synchronized, or persisted.

## Intentional non-changes

- No backend, API, database, or queue-domain change.
- No Direct Registration command, button, local draft, or persistence implementation.
- No Calling tile relocation, new labels, or action-surface change; these remain Phase 2.
- No automatic transition into the legacy Registration editor during an InService restoration; the
  existing Resume Processing workflow remains intact.

## Validation

| Check | Result |
|---|---|
| Targeted Prettier | Passed for all changed frontend files. |
| `git diff --check` | Passed. |
| Focused Vitest | Not completed: `pnpm exec vitest run ...` produced no test output and timed out after 60 seconds. No dependencies were installed or changed. |
| Type-check and lint | Not run after the stalled Vitest invocation; they remain required in an environment where the frontend toolchain starts successfully. |

## Suggested commit

- `c012_myhospital_web`: `feat(admisi): derive officer workspace mode from current loket state`

No commit is required for `b09-bilreg-api` or `c013-kiosk-queue-display-web`: Phase 1 does not
modify either repository.
