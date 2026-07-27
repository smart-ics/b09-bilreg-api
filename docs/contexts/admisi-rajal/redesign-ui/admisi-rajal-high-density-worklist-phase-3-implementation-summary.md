# Admisi Rajal High-Density Worklist — Phase 3 Implementation Summary

**Status:** Frontend implementation complete; live browser verification remains environment-gated  
**Date:** 2026-07-25  
**Source roadmap:** `c012_myhospital_web/docs/_notes/admisi/admisi-rajal-high-density-worklist-implementation-roadmap.md`  
**Scope:** Registration panel extraction, safe Empty/Preview modes, context resolution, mutation
capabilities, legacy dirty protection, and navigation ordering. No backend runtime, API, database,
queue transition, or Registration payload change.

## 1. Shipped boundary

`RegistrasiRajal.vue` is now the route-level composition owner. With the redesigned Admission Queue
enabled, it composes the dense worklist, workstation setup, mobile worklist handoff, and a
module-local `RegistrationAssistancePanel`. The oversized editable workflow was moved into
`LegacyRegistrationWorkspace`, with separate context-header, visit, guarantee, administration,
action-bar, and mutation-dialog presentation boundaries.

The panel has three Phase 3 modes:

| Mode              | Entry condition                                                | Presentation                   | Mutation authority |
| ----------------- | -------------------------------------------------------------- | ------------------------------ | ------------------ |
| `legacy-editable` | Queue UI disabled or legacy fallback enabled                   | Existing Registration workflow | Existing behavior  |
| `empty`           | Redesigned queue UI enabled and no stable selection            | Selection guidance             | None               |
| `preview`         | Redesigned queue UI enabled and a stable queue key is retained | Dedicated read-only context    | None               |

Empty and Preview never mount the editable VeeValidate form or Registration mutation dialogs. Queue
selection remains separate from Call/Recall and does not call Start Processing, Return to Waiting,
an outcome command, or any Registration/Booking/Patient write.

## 2. Preview context resolution

The assistance session retains only `antrianId:noUrut`. It resolves that key against the latest
unfiltered server page, while the visible grid may apply a local text filter independently.
Consequently, filtering a tile out of view does not clear its Preview, and polling replaces the
resolved object with the latest query result rather than retaining a stale copy.

Referenced details are loaded independently through existing frontend services:

1. a non-placeholder Booking ID enables the existing Booking detail query;
2. a non-placeholder Registration ID enables the existing Registration detail query;
3. Patient candidates are collected from the worklist identity and the returned Booking and
   Registration details;
4. the Patient detail query runs only when those sources resolve to exactly one unique MR.

Blank values and `"-"` are treated as absent. Zero candidates produce an unresolved warning.
Multiple distinct patient IDs produce a conflict warning and no Patient query. A selected key that
no longer exists in the unfiltered page produces a stale-selection state. Booking, Registration,
and Patient errors remain independent, preserve any successfully loaded context, and expose
resource-specific retry controls.

No Preview query result is copied into the editable legacy VeeValidate form. Polling can refresh
server state without overwriting a local draft.

## 3. Mutation capability matrix

The centralized `RegistrationMutationCapabilities` contract covers all Phase 3 mutation families.

| Capability              | Legacy editable | Empty | Preview |
| ----------------------- | :-------------: | :---: | :-----: |
| Edit patient            |       Yes       |  No   |   No    |
| Resolve patient         |       Yes       |  No   |   No    |
| Create patient          |       Yes       |  No   |   No    |
| Change visit            |       Yes       |  No   |   No    |
| Manage guarantee/policy |       Yes       |  No   |   No    |
| Delete SEP              |       Yes       |  No   |   No    |
| Reset                   |       Yes       |  No   |   No    |
| Void                    |       Yes       |  No   |   No    |
| Save                    |       Yes       |  No   |   No    |
| Open mutating dialogs   |       Yes       |  No   |   No    |

Preview is enforced structurally and at the composition boundary: it has no editable slot, submit
handler, mutation dialog, indirect Registration action, or keyboard submission path. The only
panel actions are clearing Preview and retrying read-only detail queries.

## 4. Ownership, partial state, and focus

Queue tiles and Preview show explicit Waiting, current-Loket, other-Loket, and unavailable cues
without relying only on color. Missing and conflicting identity states are named directly.
Successful context fragments remain visible if another detail query fails.

Selecting a desktop or mobile tile moves focus to the Preview heading. Clearing Preview restores
focus to the selected tile when it remains rendered. Local text filtering does not trigger a dirty
prompt because it does not clear the stable selected context.

## 5. Dirty legacy drafts and navigation

Existing editable legacy drafts use a Save/Discard/Cancel transition guard:

- **Save** runs VeeValidate, waits for Registration and any SEP persistence, waits for authoritative
  refetch, and continues only after success.
- **Discard** resets the draft before the pending transition.
- **Cancel** leaves form values, selection, dialog state, and focus in place.

The guard covers queue and patient handoff, IGD selection, desktop/mobile selection handoff,
patient-detail/create entry, browser unload, and route leave. In the redesigned branch, service
point and workstation changes cannot discard a draft because Empty/Preview mounts no editable
form; Phase 5 will reuse the guard when an explicit Processing mode introduces editing beside the
worklist.

Workspace tab and screen clicks now call the router first. The workspace store updates only from an
accepted URL transition, so a route guard rejection leaves the active screen, mounted component,
and draft unchanged.

## 6. Compatibility, deployment, and rollback

The existing environment-wide configuration remains the only enablement boundary:

```json
"admissionQueue": {
  "enabled": true,
  "useLegacySidebarFallback": false
}
```

Set `admissionQueue.enabled=false` or `useLegacySidebarFallback=true`, then refresh the client, to
restore the existing editable sidebar workflow. Rollback requires no database or server-state
reversal. This phase added no endpoint, request field, response field, queue lifecycle transition,
background job, or database object.

Deploy the frontend after the existing Booking, Registration, Patient, and Phase 1 worklist queries
are available. Backend deployment order is otherwise unchanged.

## 7. Verification evidence

Completed locally in `c012_myhospital_web`:

- targeted Prettier completed for all Phase 3 files;
- focused Vitest with one worker: **39 passed, 0 failed** across nine files;
- `npx --no-install vue-tsc --noEmit --project tsconfig.app.json`: passed;
- `npx --no-install vite build`: passed, 3,634 modules transformed;
- targeted Oxlint: 0 warnings and 0 errors;
- targeted ESLint for production Phase 3 code and new unit/navigation tests: passed;
- Playwright collected both new legacy-fallback and read-only Preview scenarios successfully.

The live Playwright journeys were not executed because they require an authenticated environment,
the corresponding runtime-flag state, and representative active queue data. The broad `pnpm`
commands could not start because the configured pnpm 10.11.1 registry signature could not be
verified. The locally installed underlying type-check, test, lint, and Vite build tools were used
without bypassing that package-manager integrity failure.

The Phase 1 representative disposable-SQL performance gate remains outstanding. Phase 3 does not
satisfy or weaken that production gate.

## 8. Explicitly deferred behavior

Phase 3 does not implement:

- Phase 4 Return to Waiting or any new claim transition;
- Phase 5 Start/Resume Processing, editable queue sessions, conflict recovery, or session strip
  integration;
- Phase 6 queue-linked Registration payloads, Complete Assistance, Established, or NotEstablished;
- Phase 7 universal patient-context search or duplicate-prevention confirmation;
- Phase 8 scale, accessibility, resilience, and representative production-load closure;
- Phase 9 rollout telemetry, controlled enablement, or legacy retirement.

The extracted panel, stable context keys, capability contract, and dirty-transition boundary are
the prerequisites for those later phases; they do not grant those capabilities early.
