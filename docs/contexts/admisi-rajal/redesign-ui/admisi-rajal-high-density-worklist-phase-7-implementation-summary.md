# Admisi Rajal High-Density Worklist — Phase 7 Implementation Summary

Date: 2026-07-25

## Scope and outcome

Phase 7 adds an Admisi-owned patient-context resolver for an authoritative In Service Loket
session. Booking, active Registration, and Patient candidates are returned through one ranked,
masked contract. Result selection remains Preview-only; an entity-specific confirmation refetches
the chosen context before it seeds the existing Registration editor.

## Pre-implementation audit

### Already implemented

- Phases 1–6 supplied the dense worklist, Registration Assistance boundary, Preview/Processing
  modes, current-Loket authority, dirty guards, and atomic queue-aware completion.
- Existing write paths already rejected an active Registration for the same Patient.
- Legacy Patient-only deep search, date-bound Booking search, and active Registration repository
  search supplied partial source capabilities.

### Partial or conflicting implementation

- Legacy deep search could directly seed Walk-In work without cross-entity relationship
  revalidation.
- Booking search lacked Booking-ID/name matching and a relationship-complete response.
- The high-density worklist still exposed a patient/name filter even though patient-context
  resolution belongs inside Processing.
- Patient creation had no exact NIK or demographic duplicate guard, and Booking Registration did
  not explicitly reject a Booking linked after Preview.

## Completed work

### Backend

- Added authenticated `POST /api/v1/admisi-rajal/patient-context-search` and confirmation-time
  `GET /api/v1/admisi-rajal/patient-context/{kind}/{id}` endpoints.
- Added discriminated Booking, Registration, and Patient results with deterministic ranking,
  relationship IDs/warnings, per-group counts/truncation, completeness metadata, and masked NIK
  and phone values.
- Current-date Booking is the default. Explicit all-date expansion searches the operational
  Booking retention window.
- Search logs contain only scope, exact-match category, counts, partial status, and duration.
- Booking Registration now rejects a Booking that already has a Registration.
- Patient creation now rejects exact-NIK and exact name/birth-date/phone duplicates.

### Frontend

- Added Zod contracts, a TanStack Query service, debounced universal-search orchestration, and
  confirmation-time revalidation.
- Added an accessible combobox/grouped-listbox resolver with Preview, entity-specific actions,
  masking, relationship warnings, all-date expansion, partial-source messaging, live regions, and
  keyboard navigation.
- Confirmed context is retained as `{ kind, id }`; current server data is mapped into the existing
  editor without storing search result objects as durable Registration state.
- Context is cleared when the queue selection or authoritative Processing session changes.
- `Ctrl+K` focuses the resolver only during an authoritative In Service session.
- Removed patient filtering from the high-density queue while retaining the legacy fallback.
- Updated the Admisi workflow guide and changelog.

## Architectural decisions and deviations

- Search uses POST rather than query-string GET so raw NIK, phone, or other search text does not
  enter normal URL/access-log fields.
- The resolver uses the existing Admission Queue enablement and legacy-fallback switches; no
  additional rollout flag was introduced.
- Existing legacy search routes remain unchanged.
- The current repository exposes active Registration search but no bounded historical
  Registration projection. All-date responses therefore mark completeness as partial instead of
  claiming that unavailable history was searched. A dedicated historical projection remains a
  Phase 8 measurement/design item.
- All-date Booking expansion is bounded to records from 2000 through one year beyond the business
  date to prevent an unbounded operational query.

## Verification

- Backend API project build: passed, with pre-existing warnings.
- Backend focused universal-search tests: 5 passed.
- Frontend Vue TypeScript check: passed using the installed project-local `vue-tsc`.
- Frontend resolver component tests: 3 passed.
- Existing focused Registration Assistance, session, and route Preview tests: 21 passed.
- Frontend targeted Prettier, Oxlint, and ESLint: passed with zero errors.
- Frontend production Vite build: passed; existing sourcemap, chunk-size, and browsers-data
  warnings remain.
- The repository `pnpm` wrapper still rejects its configured release signature offline, so the
  installed project-local binaries were invoked through `npx`.

## Remaining work

- Add a measured historical Registration projection if Operations requires complete final-history
  results in Search all dates; the current response explicitly reports this source as partial.
- Run authenticated browser journeys and representative real-SQL query-plan/load gates in their
  configured environments.

## Recommended commits

### `b09-bilreg-api`

```text
feat(admisi-rajal): add universal patient-context resolution

- compose ranked Booking, Registration, and Patient search results
- add confirmation-time relationship revalidation and masked contracts
- prevent duplicate Booking Registration and Patient creation
- sanitize search telemetry and expose completeness metadata
- add focused ranking, masking, relationship, and logging tests
- document Phase 7 implementation and remaining historical-search work
```

### `c012_myhospital_web`

```text
feat(admisi): add universal patient-context search

- add typed TanStack Query integration for patient-context resolution
- add accessible grouped Preview and entity-specific confirmation actions
- integrate confirmed stable context with the Processing session and editor
- gate Create Patient behind sufficient duplicate search
- remove patient filtering from the high-density queue
- update Admisi workflow documentation and changelog
```
