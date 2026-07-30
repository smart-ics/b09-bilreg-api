# Admission Officer Workspace — Phase 0 Implementation Summary

**Implemented:** 2026-07-27  
**Phase:** Baseline stabilization and contract lock

## Outcome

Phase 0 stabilizes and documents the existing Admission Officer queue workflow. It activates no
workspace mode, adds no endpoint, migration, persisted state, or Queue Display behavior.

The existing frontend current-Loket guard is retained: a new Call is unavailable until the
authoritative current-Loket query succeeds, and remains unavailable while that Loket has any active
Outstanding or InService session. The pre-mutation guard continues to prevent a stale UI action from
issuing a second Call.

## Changes by repository

### `c012_myhospital_web`

- Preserved the pre-existing dirty current-Loket guard and workstation retry/configuration work;
  it was not reset, overwritten, or separated into a Queue Display change.
- Extended `admissionQueueActionRules` characterization coverage to prove that an active claim
  blocks a second Call but still permits the active entry's Recall, `Tidak Hadir` (Return to
  Waiting), and `Hadir` (Start Service) actions.
- Added the approved authority and terminology baseline to `docs/modules/admisi/README.md`.
  Earlier operator-facing “No Show” wording is replaced with `Tidak Hadir` and terminal
  `Tidak Datang`.

### `b09-bilreg-api`

- Added characterization coverage that whitespace-only/no queue-context fields remain on the
  legacy no-context compatibility path.
- Updated the canonical Admission Queue API contract with the frozen terminology table, the
  non-persisted workspace-mode rule, and the existing Registration compatibility fixture:
  complete queue context is workstation-resolved, partial context is rejected as
  `AQ_INVALID_REQUEST`, and no meaningful context retains legacy synthetic completion behavior.
- Updated architecture reconciliation to record backend authority and the separation between
  non-terminal `Tidak Hadir` and terminal `Tidak Datang`.
- No handler, controller, route, persistence, migration, or authorization implementation changed.

### `c013-kiosk-queue-display-web`

- Reviewed only. Its pre-existing dirty SignalR/configuration changes remain untouched and are not
  part of Phase 0.

## Validation

| Check | Result |
|---|---|
| Frontend focused Vitest + type-check | Not runnable: `c012_myhospital_web/node_modules/.bin` is absent. Both `pnpm exec vitest run` attempts timed out after 60 seconds without test output; no dependencies were installed or changed. |
| Backend focused test filter | 39 passed, 1 failed. The changed context test compiled and passed. |
| Backend failure | Existing `AdmissionQueueApiContractTest.RefreshHub_IsAuthorizedAndExposesStablePath` expects `[Authorize]` on `AdmissionQueueRefreshHub`, but the source currently has none. This is SignalR/display-adjacent and out of Phase 0 scope, so it was not changed. |
| Diff whitespace check | Passed for frontend and backend changes; Git reports only normal CRLF conversion warnings in backend files. |
| Manual two-browser smoke check | Not executed: no authenticated running browser/API environment was available in this workspace. Required scenario remains browser A acquires a Loket claim, then browser B cannot offer or invoke Call until release/completion. |

## Intentional non-changes

- No Ready/Calling/Queued Registration/Direct Registration UI mode was implemented.
- No Direct Registration endpoint or queue-less persistence behavior was introduced.
- No database change, API addition, workspace-mode storage, SignalR workspace message, or Queue
  Display modification was made.

## Suggested commits

- `c012_myhospital_web`: `fix(admisi): fail closed while current loket state is unresolved`
- `b09-bilreg-api`: `test(admission-queue): lock legacy registration and queue action contracts`
- Optional documentation-only splits:
  - `docs(admisi): lock officer workspace phase-0 terminology and baseline`
  - `docs(admission-queue): record phase-0 authoritative API baseline`

No Phase 0 commit is planned for `c013-kiosk-queue-display-web`.
