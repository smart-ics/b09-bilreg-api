# Admission Officer Workspace — Phase 3 Implementation Summary

**Implemented:** 2026-07-27  
**Phase:** Workspace restoration and concurrency hardening

## Outcome

The Admission Officer workspace now treats `displays/current` as an independently refreshed
authoritative source. A missed refresh hint, reload, window focus change, stale same-workstation
tab, or indeterminate mutation cannot promote stale local selection over the server Loket claim.

## Changes

### `c012_myhospital_web`

- Current-Loket display now polls on the existing `admissionQueue.worklistPollMs` cadence and
  explicitly refetches on window focus and remount. The unresolved shell remains fail-closed until
  a successful current-Loket response exists.
- Successful Call, Panggil Lagi, Tidak Hadir, and Hadir flows refresh the main worklist,
  Loket-scoped worklist, and current-Loket display together.
- A non-concurrency mutation failure now reconciles those authoritative projections before showing
  an error. If the server already committed the intended Call, Recall, Return-to-Waiting, or
  Start-Service state, the operator receives a confirmation instead of a false failure.
- `AQ_CONCURRENCY_CONFLICT` remains non-retrying: the workspace refreshes once and asks the
  operator to review current server state.
- Added service-level coverage that locks the independent current-Loket polling, focus, and
  remount contract. Existing workspace-mode tests verify Outstanding/InService restoration and
  claim precedence over local intent.
- Corrected the workspace-mode template binding and the pinned Calling tile's duplicate class
  binding so the typed production build can compile the existing Phase 1/2 UI.

## Validation

| Check | Result |
|---|---|
| Targeted Prettier | Passed |
| Focused Vitest: Admission Queue service + workspace mode | Passed: 12 tests across 2 files |
| `vue-tsc --noEmit --project tsconfig.app.json` | Passed |
| `vite build` | Passed (existing chunk-size and sourcemap warnings only) |
| Targeted ESLint | Did not complete within 60 seconds and produced no diagnostic output |
| `git diff --check` | Passed |

## Environment-gated verification

No authenticated browser environment, Queue Display endpoint, active queue fixture, or disposable
Admission Queue SQL database was supplied locally; no live system was modified.

Run the real-SQL race and rollback gate from `b09-bilreg-api` only with its required
`BILREG_AQ_IT_*` disposable-database environment variables configured:

```powershell
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmissionQueueRealSqlGateTest"
```

Run authenticated Playwright against a non-production API, a mapped workstation/Loket, two browser
tabs for the same workstation, an active Waiting entry, and an available Queue Display subscription.
Verify: reload during Calling and InService restores the pinned/processing context; a stale
RowVersion receives conflict recovery; and a display refresh is observed after Call/Recall. Do not
use production patients or a shared operational Loket for this verification.

## Rollback

Frontend-only rollback: deploy the previous `c012_myhospital_web` build, or set
`admissionQueue.enabled=false` / `useLegacySidebarFallback=true` and refresh the client. No API,
database, queue transition, or Queue Display deployment rollback is required.

## Suggested commit

- `c012_myhospital_web`: `feat(admisi): harden officer workspace restoration and queue concurrency recovery`

No source change is required in `b09-bilreg-api` or `c013-kiosk-queue-display-web`.
