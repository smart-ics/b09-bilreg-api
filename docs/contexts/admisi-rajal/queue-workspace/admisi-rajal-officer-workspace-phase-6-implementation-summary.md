# Admission Officer Workspace — Phase 6 Implementation Summary

**Implemented:** 2026-07-27  
**Phase:** Registration Not Established activation

## Outcome

`c012_myhospital_web` now activates **Registrasi Tidak Terbentuk** for an authoritative In Service
Admission Queue session. The officer selects an externally configured runtime reason and submits the
existing Not Established outcome command directly. A successful outcome completes the queue entry,
releases the Loket claim, and restores the workspace to Ready without creating a Registration.

## Changes

### `c012_myhospital_web`

- Passed the runtime `admissionQueue.reasonCodes` catalog from the officer workspace to the queued
  Registration editor only.
- Added the **Registrasi Tidak Terbentuk** terminal action, reason selector, required-reason guard,
  pending-state protection, and dedicated success event.
- Reused the existing Not Established mutation and authoritative completion invalidation. Concurrency
  conflicts continue to enter the existing fail-closed refresh path; indeterminate failures invalidate
  and ask the operator to reload/review before retrying.
- Kept the action unavailable when the runtime catalog is empty and show a configuration-dependency
  message. No fallback, local, or `Other` reason code was added.
- Updated the Admisi operator guide and focused component/service coverage.

## Validation

| Check                                                            | Result                                                                                |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| Targeted Prettier                                                | Passed                                                                                |
| Focused Vitest: Registration workspace + Admission Queue service | Passed: 17 tests across 2 files                                                       |
| `npx vue-tsc --noEmit --project tsconfig.app.json`               | Passed                                                                                |
| `pnpm tc:app`                                                    | Timed out after 60 seconds without diagnostics; direct project-local `vue-tsc` passed |
| Targeted ESLint: production files + service test                 | Passed                                                                                |
| Legacy workspace test-file ESLint                                | Blocked by 30 pre-existing `any` / commented-test violations                          |
| `git diff --check`                                               | Passed                                                                                |
| Authenticated browser verification                               | Environment-gated; not run locally                                                    |

The focused legacy workspace suite emits pre-existing Vue/mock query warnings while passing; no
production error was reported.

## Deployment and rollback

Deployment must provide the approved external `admissionQueue.reasonCodes` values. The repository
default remains empty, so the terminal action stays unavailable until configuration is supplied.

Rollback is frontend-only: deploy the prior `c012_myhospital_web` build or enable the existing legacy
sidebar fallback. Do not replace this action with a local reason code or alter existing outcomes.

## Suggested commit

- `c012_myhospital_web`: `feat(admisi): activate registration not established outcome`

No source change is required in `b09-bilreg-api` or `c013-kiosk-queue-display-web`.
