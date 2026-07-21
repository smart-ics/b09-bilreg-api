# Task 10F — Coordinated Admission Cancellation Verification Report

**Date:** 13 July 2026  
**Verdict:** **not ready**

## 13 July 2026 follow-up — owned-fixture evidence

### Fixture strategy

`AdmissionRegistrationStep4CDbTest` now creates a unique patient via the API
for every Step4C and coordinated-cancellation scenario.  It creates the
source, processes it through the real admission API, and deletes registration
children, source, correlated audit rows, ledger rows, billing rows, and the
patient in `finally`.  The test never uses a representative patient ID.

The passing targeted run was:

```text
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj \
  --filter "FullyQualifiedName~AdmissionRegistrationStep4CDbTest" --no-build --no-restore

Passed: 12, Failed: 0
```

The real-SQL cases establish these facts:

| Scenario | SQL evidence asserted |
|---|---|
| Opname / no Waiting List / zero items | Admission `Cancelled`; Registration void date/time/user populated; discharge and cancel-discharge sentinel values unchanged; RegInap, guarantor, and doctor history retained; all active doctor assignments ended; only `BILRG_RegAktif` deleted; Opname restored to `Requested`; five correlated audit rows written. |
| Waiting List `Waiting` and `Accepted` | The retained Waiting List row becomes status `Cancelled`, has void metadata, and has its own correlated pre-change snapshot. |
| One billing item | `REGISTRATION_HAS_BILLING_ITEMS`; Admission remains admitted; zero cancellation audits and zero ledger rows. |
| Legacy source | No restore audit is emitted; Admission and Registration are cancelled coherently. |
| Idempotency | Same request/fingerprint replays without audits; same ID/different fingerprint returns `REQUEST_ID_REUSED`; a different request against final coherent state completes as already-cancelled. |
| Two independent concurrent requests | Both calls finish safely, but SQL contains one Admission void transition, one Admission void audit, and no `RegAktif` row. |

Audit snapshots were repaired before this run: `LockState` now captures
Admission, Registration, RegInap plus doctor-history, RegAktif, Waiting List,
and source snapshots under the corresponding locks.  The SQL suite asserts
the RegInap snapshot contains doctor history and the RegAktif audit contains
the active-projection pre-change image.

### Defects fixed

- Replaced the five fixed shared Step4C patient fixtures with owned, cleaned
  fixtures.
- Added real SQL coverage for Waiting/Accepted list cancellation, billing
  rejection without residue, legacy cancellation, durable replay/request-ID
  reuse, and concurrent requests.
- Preserved entity-specific audit snapshots rather than serializing one
  aggregate state image for all audit rows.

### Still blocking deployment verification

The following required matrix cells are still absent: Reservation cancellation
and its source restore, multiple billing rows, empty Tata Rekening header,
all individual injected write/audit/ledger rollback boundaries, explicit
Admission/Waiting-List/source conditional-write races, inconsistent-partial
state, and voided-registration discharge/search endpoint regression tests.
They must be added before Task 10G.

### Full-suite classification

The full run after this change reported **96 failed, 1,228 passed, 1 skipped**
of 1,325.  The 12 cancellation SQL tests pass when run in their isolated
collection.  The full parallel run failed the new owned-patient tests because
the shared SQL environment intermittently reported missing
`sp_tz_parameter_no_getnextvalue`; this is schema/environment drift, not a
cancellation persistence failure.

The pre-existing failures remain classified as follows:

| Classification | Evidence |
|---|---|
| Caused by this feature | None demonstrated by the isolated 12/12 SQL run. |
| Stale shared test data / concurrent shared DB | Step4C failures in the parallel full run and fixture-dependent database tests. |
| Schema/environment drift | Lab tests report missing `EmrOrderId` and billing-release columns; owned-patient creation reports missing `sp_tz_parameter_no_getnextvalue`. |
| Unrelated pre-existing failures | Payment `TrsBilling2DtoTest` date expectation and other non-Admisi failures in the full output. |

### Final verdict

**not ready.** The repaired fixtures and the listed real-SQL evidence are
valid in isolation, but the mandatory rollback, race, Reservation, billing
matrix, and voided-registration regression coverage remains incomplete.  The
parallel full-suite SQL environment is also not stable.  Do not proceed to
Task 10G.

## 13 July 2026 repair attempt — current evidence

This update replaces the stale-fixture conclusion in the original baseline with
the result of a fresh execution against the configured SQL Server.

| Verification run | Result | Evidence |
|---|---:|---|
| Existing Step4C real-SQL suite | PASS | `AdmissionRegistrationStep4CDbTest`: 5 passed, 0 failed. The previously reported absent representative-patient condition was not reproduced on 13 July. |
| Coordinated handler + audit snapshot tests | PASS | `AdmCoordinatedCancelHandlerTest`: 7 passed, 0 failed. |
| Owned-patient coordinated-cancellation SQL fixture | BLOCKED | The fixture creates a patient through `POST /api/pasien`, creates and consumes an Opname Request, then invokes the real handler. The first ledger lock fails with `Invalid object name 'BILRG_AdmCoordinatedCancellationRequest'`. |

### Defects fixed

`CoordinatedCancellationState` now carries immutable entity-scoped audit
snapshots. `CoordinatedCancellationRepo.LockState` captures separate snapshots
for Admission, Registration, RegInap plus active doctor history, RegAktif,
Waiting List, and source while the corresponding locks are held. The handler
now serializes the snapshot for the audited entity rather than serializing one
shared state object for every audit. The new handler test proves that a
RegInap/doctor-history audit and a RegAktif delete audit carry distinct
pre-change images.

### Fixture strategy (implemented starting point)

`AdmissionRegistrationStep4CDbTest` now has an owned-patient helper. It creates
a unique patient and NIK via the API, creates the Opname source and shared
registration through the real admission APIs, and cleans the source,
registration, child rows, correlated audit rows, and patient rows in `finally`.
The cancellation fixture has no fixed patient ID and no dependency on mutable
representative patient data.

### SQL evidence and blocking prerequisite

The cancellation fixture cannot acquire its first idempotency lock because the
verification database lacks the required table. The source-controlled DDL is:

`src/bilreg/Bilreg.SqlDb/AdmisiRanapContext/AdmissionFeature/BILRG_AdmCoordinatedCancellationRequest.sql`

No cancellation write, audit row, or completed ledger record was created by
this failure: the exception occurs at `LockLedger` before any mutation.

### Remaining required verification

The following remains unverified and must be completed after the ledger DDL is
deployed to an isolated verification database:

- all source, Waiting List, billing-item, and empty-header fixture variants;
- exact persisted cancellation/retention checks for every successful variant;
- failure injection at each write, audit, and ledger boundary, observed from a
  second SQL connection;
- durable idempotency and two-connection concurrency cases;
- voided-registration active/search/discharge regression coverage;
- full-suite failure classification.

### Current final verdict

**not ready**. The audit-snapshot defect is fixed and real SQL connectivity is
confirmed, but the required idempotency-ledger schema is absent from the live
verification database. It is therefore not possible to produce valid
coordinated-cancellation persistence, rollback, idempotency, or concurrency
evidence, and Task 10G must not begin.

## Scope and execution

Verification used the frozen contract at `docs/contexts/admisi-ranap/admission-cancellation-implementation-contract.md` and the completed coordinated-cancellation handler, API controller, and SQL repository.

| Verification run | Result | Evidence |
|---|---:|---|
| Targeted coordinated-cancellation unit, HTTP-host, and eligibility tests | PASS | `dotnet test ... --filter "FullyQualifiedName~AdmissionCancellation|FullyQualifiedName~AdmCoordinatedCancel|FullyQualifiedName~RegistrationCancellationEligibility" --no-build --no-restore`: 22 passed, 0 failed. |
| Admission, Registration, RegInap, Waiting List, Opname Request, and Reservation transition suite | PARTIAL | 108 passed; 3 real-SQL Step4C tests failed before setup because their fixed representative patients no longer exist. |
| Full regression suite | FAIL | 1,227 passed, 89 failed, 1 skipped of 1,317. Failures include stale shared-DB fixtures/schema and unrelated domain regressions. |
| Real SQL Server access | REACHED, not sufficient | Step4C tests connected to the configured SQL Server but received HTTP 404 for each absent patient fixture. No cancellation test record was created, so no cleanup was necessary. |

## Contract matrix

| Requirement | Result | Evidence / limitation |
|---|---|---|
| Domain transitions | PARTIAL | Existing domain tests passed in the transition suite; no complete coordinated transition test covers every aggregate together. |
| Zero / one / many billing rows; empty Tata Rekening header | PARTIAL | Eligibility port and SQL-shape tests pass. The query reads only `ta_trs_billing`; it does not read headers or `ta_trs_billing2`. Only mocked handler/API execution proves the resulting rejection; no real-SQL cancellation scenario exists. |
| Opname, Reservation, legacy source; no WL, Waiting WL, Accepted WL | PARTIAL | Mocked HTTP source variants pass. Only a `Waiting` waiting-list fixture is used. No real-SQL evidence for any variant, no-WL, or Accepted WL. |
| Exact persistence retention/deletion | NOT VERIFIED | No real-SQL cancellation test asserts Admission cancellation, Registration void fields, untouched discharge fields, doctor end dates, retained RegInap/guarantor/WL/source/history, or sole `BILRG_RegAktif` deletion. |
| Correlated audits | PARTIAL | Mocked tests verify correlated audit calls and HTTP metadata. No database audit rows were inspected. |
| Idempotency and request-ID reuse | PARTIAL | Mocked handler/API replay and reuse-conflict tests pass. No durable-ledger replay or different-key coherent-state test uses SQL Server. |
| Stale state and source conflict | PARTIAL | Mocked API tests pass. No SQL conditional-write race is exercised. |
| Rollback for persistence, audit, ledger failure | NOT VERIFIED | The API mock test covers one persistence exception only. No real transaction rollback test injects failures at every listed boundary. |
| HTTP/JSend/error/correlation | PASS (mock host) | HTTP tests prove 200, 400 billing rejection, 404 missing root, 409 conflicts, 422 validation, and correlation echoing. |
| Two independent SQL connections / one winner / no partial state | NOT VERIFIED | No concurrency test exists. |
| Voided registration not active/searchable/dischargeable | PARTIAL | Cancellation deletes the sole `BILRG_RegAktif` projection and ordinary Registration list queries filter `fd_tgl_void`. There is no cancellation regression test proving all search and discharge entry points reject the voided registration. |

## Database evidence

No cancellation-state database evidence can be claimed. The only attempted live setup failed before any source/admission/registration mutation:

| Attempt | Result |
|---|---|
| Opname Step4C fixture | HTTP 404: configured patient fixture absent. |
| Reservation Step4C fixture | HTTP 404: configured patient fixture absent. |
| RegInap rollback fixture | HTTP 404: configured patient fixture absent. |

The Step4C test cleanup method was not reached with a generated registration, and no cancellation fixture data remains from this verification.

## Defect found and fixed

**Fixed — missing Admission was incorrectly classified as a concurrency conflict.**

`CoordinatedCancellationRepo.LockState` returned a synthetic cancelled state when `BILRG_AdmAdmission` was absent. With a present Registration/RegInap, the handler then returned `CONCURRENCY_CONFLICT` (409), violating the frozen missing-root 404 contract.

The state now explicitly carries `AdmissionExists`; handler validation treats a missing Admission like the other missing roots. `MissingAdmission_ReturnsNotFoundBeforeEligibilityOrMutation` covers the behavior. The targeted suite passes 22/22 after this fix.

## Remaining deployment blockers and risks

1. Add isolated, transactionally cleaned SQL Server fixtures and tests for all mandatory source/Waiting List/billing variants, exact retained/deleted rows, and correlated audit rows.
2. Add failure-injection integration tests for each persistence mutation, each audit insertion, and ledger completion; verify rollback through a second SQL connection.
3. Add a two-connection concurrency test proving exactly one winner and no partial state.
4. Add direct regression tests for voided-registration active lookup, search, and every discharge command/endpoint.
5. Repair or isolate the stale shared Step4C test data before treating its SQL test result as a release gate. The full suite also has 89 failures, so it is not a clean regression baseline.
6. Audit snapshots are one shared state object and do not visibly contain separate pre-change RegInap/doctor-history or RegAktif snapshots; validate this against the contract before deployment verification.

## Final verdict

**not ready**. The mocked unit/API contract is substantially covered and the discovered 404 mismatch was fixed, but the required real SQL Server persistence, rollback, audit, idempotency, and concurrent-request evidence is absent.
