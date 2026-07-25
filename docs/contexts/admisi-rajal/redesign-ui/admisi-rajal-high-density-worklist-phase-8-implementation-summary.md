# Admisi Rajal High-Density Worklist — Phase 8 Implementation Summary

**Date:** 2026-07-25  
**Phase:** 8 — Scale, resilience, and accessibility hardening

## Completed work

- Added authoritative `totalCount` to the opt-in officer-worklist paging response while preserving
  the legacy array response and existing endpoint semantics.
- Kept the approved offset ordering and enforced the existing 1–500 bounded page size in the
  composed query. Worklist and current-session SQL reads now have a 30-second command timeout.
- Added low-cardinality `System.Diagnostics.Metrics` instruments for worklist latency/result
  buckets and universal-search latency/request dimensions. The instruments contain no search text,
  patient, Booking, Registration, queue, workstation, or identity values.
- Added centralized low-cardinality API error/conflict counters and sanitized unexpected 500
  responses so database/provider details are not returned to clients.
- Replaced the frontend's silent first-page limitation with an explicit 100-row incremental load
  model capped at 500. Each increment queries offset zero for the complete loaded window, so polling
  replaces one authoritative ordered window instead of merging stale offset pages.
- Added accurate “loaded of total” status, accessible Load More feedback, retained last-successful
  data during transient refetch failures, last-success time, and manual retry.
- Hardened responsive virtual rows with stable row keys and Arrow/Home/End navigation between
  Preview triggers. Native Tab access to per-tile actions remains intact.
- Updated the user workflow documentation and changelog.

## Architectural decisions

- Offset paging remains the compatibility contract. Consistency under polling is achieved by
  refetching the complete loaded capacity from offset zero rather than appending independently
  refreshed pages.
- `totalCount` uses exactly the same business-date, service-point, queue-state, active-only, and
  Loket predicates as the page query.
- The initial page remains 100 and polling remains 15 seconds. Load More is bounded at 500 even
  though the server contract also permits 500.
- Full name/MR display remains available only in the approved operational context. Existing masking
  for NIK, phone, and membership values is unchanged; queue-number-only privacy mode was not added.
- No index or read projection was added without representative query-plan evidence.

## Deviations and justification

- A complete historical Registration projection was not introduced. The current all-date search
  continues to declare Registration completeness as partial; inventing history from the active
  repository would violate the Phase 7 contract.
- Broad all-date Booking persistence was not rewritten because the existing repository exposes no
  bounded server-side search interface. Replacing it safely requires a measured repository/DAL
  change and representative SQL evidence, not client-side inference.
- Automated browser viewport, axe, and screen-reader proof was not claimed. The relevant semantics
  and keyboard behavior were implemented and unit-tested, but authenticated browser infrastructure
  and manual NVDA execution are external release gates.

## Verification

- Backend focused worklist, API contract, operational query, and universal-search tests:
  **50 passed**.
- Frontend TypeScript application check: **passed**.
- Frontend focused contract, toolbar, tile, universal-search, and route Preview tests:
  **20 passed**.
- Frontend targeted Oxlint and ESLint: **passed with zero errors**.
- Frontend production Vite build: **passed** with existing sourcemap, chunk-size, and browser-data
  warnings.
- Backend API build: **passed with zero errors**; six existing warnings are unrelated to Phase 8.
- The unfiltered backend regression command was attempted, but the repository includes
  environment-dependent DAL/real-SQL suites and unrelated pre-existing `AntrianMapModel` failures;
  it is not a valid green local gate without the configured integration environment. The focused
  Phase 8 suite remained green.

## Remaining release-gate work

- Run `AdmissionQueueRealSqlGateTest` with configured disposable `BILREG_AQ_IT_*` SQL settings and
  retain p50/p95/max plus Statistics IO/query-plan evidence for 400 entries and 20 Loket.
- Keep the worklist p95 at or below 2 seconds, exact search p95 at or below 2 seconds, broad search
  p95 at or below 3 seconds, and timeout/error rate below 1% over 15 minutes.
- Connect the new `Bilreg.AdmisiRajal` and `Bilreg.Api` meters to the production
  exporter/dashboard and add alert rules.
- Add a database index, batch enrichment, or bounded historical projection only if that evidence
  identifies a concrete bottleneck; deploy any such migration separately with pre/post plans and
  rollback SQL.
- Execute authenticated Playwright checks at Full HD, 125% zoom, tablet, and mobile widths,
  including throttling/reconnect and automated accessibility scanning.
- Complete the manual keyboard and NVDA checklist for focus return, live regions, dialogs, masked
  values, and non-color state cues.

## Rollback

The frontend may revert to the prior 100-row surface or the legacy feature-flag path without queue
or Registration data changes. The additive `totalCount` field and server metrics may remain
deployed safely. No database migration requires rollback.
