# Patient Tracker — Admission Queue R-01 Implementation Report

**Roadmap item:** R-01 — Replace unsafe admission lifecycle persistence

**Status:** Complete

**Completed:** 2026-07-22

**Roadmap:** [TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-ROADMAP.md](./TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-ROADMAP.md)

**R-00 baseline:** `2bac008b1da15946e7aa2fd23429fefdfacc8835`

**Verified R-01 implementation commit:** `4d70915505268da4747144fbdf7a2dd075661fec`

## 1. Outcome

Admission lifecycle transitions no longer use whole-aggregate `AntrianRepo.SaveChanges` for entry mutation. Active transitions persist through expected-state compare-and-set updates; new entries persist through header upsert plus single-row insert. Failed CAS (zero affected rows) raises `AdmissionQueueConcurrencyException` (HTTP 409 already mapped). No schema change was required.

## 2. Persistence paths changed

| Path | Before (unsafe) | After |
|------|-----------------|-------|
| `AdmissionQueueStartHandler` Waiting → InService | `Serve` + `SaveChanges` | `Serve` + `TrySaveWaitingToInServiceTransition` |
| `RegJalanWalkInCommand` keyed completion | anonymous CAS then still `SaveChanges(admissionQueue)`; identified used `SaveChanges` only | anonymous → `TrySaveAnonymousInServiceTransition`; identified → `TrySaveInServiceToDoneTransition`; never `SaveChanges` on admission queue |
| `RegJalanWalkInCommand` legacy create-on-reg | `SaveChanges(admissionQueue)` | `SaveNewEntry` (header upsert + insert only) |
| `RegJalanByBookingCmd` keyed / legacy admission | `SaveChanges(admissionQueue)` | CAS Done or `SaveNewEntry` |
| `QueAnonymousIntakeHandler` | `AddEntry` + `SaveChanges` | `SaveNewEntry` |
| Journey associate (`TrkJourneyResolveSelectCmd`) | already CAS | unchanged |

Physician queue `SaveChanges(antrian)` in Walk-In/ByBooking is unchanged (out of R-01 admission scope).

## 3. Why the old paths were unsafe

`AntrianRepo.SaveChanges` sync-compares every entry against the database:

- changed rows use unconditional PK `UPDATE` → concurrent officers can overwrite each other (lost updates);
- rows present in DB but absent from the in-memory collection are **physically deleted** → queue history can vanish under concurrent intake/start/complete.

## 4. How the new implementation prevents lost updates and deletion

- **CAS updates** (`UpdateWaitingToInService`, `UpdateInServiceToDone`, existing `UpdateFromAnonymousInService`) set the decided DTO fields only when `AntrianId`/`NoUrut` plus expected status (and tracker identity where required) still match. Competing transitions yield one winner; the loser gets zero rows → `AdmissionQueueConcurrencyException`.
- **`SaveNewEntry`** upserts the session header and inserts one entry only. It never lists or deletes siblings.
- Domain methods `Serve` / `Done` / `AssignPasien` remain intention-revealing; no generic transition framework was added.

## 5. Verification

```powershell
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore --filter "FullyQualifiedName~AdmissionQueueStartHandlerTest|FullyQualifiedName~AdmissionQueueRegistrationResolverTest|FullyQualifiedName~TrkJourneyResolveHandlerTest|FullyQualifiedName~QueAnonymousIntakeHandlerTest|FullyQualifiedName~AdmissionQueueCompleteAndMulaiPeriksaTest|FullyQualifiedName~AntrianEntryDalTest|FullyQualifiedName~AntrianRepoAreEqualTest|FullyQualifiedName~AdmissionQueueLifecyclePersistenceTest"
```

Result:

```text
Passed:  27
Failed:  0
Skipped: 0
Total:   27
```

The focused suite was rerun successfully against implementation commit
`4d70915505268da4747144fbdf7a2dd075661fec` on 2026-07-22. The repository
working tree was clean before this verification/documentation update. Existing
nullable-reference and XML-documentation compiler warnings remain outside R-01;
there were no compilation or test failures.

Coverage includes SQL CAS affected-row tests (1 then 0), sibling-entry preservation, handler conflict → concurrency exception, and repo proofs that insert/CAS paths never call `Delete`.

## 6. Exit criteria

| Criterion | Result |
|---|---|
| Admission Waiting → InService uses CAS | Pass |
| Admission InService → Done uses CAS | Pass |
| Keyed registration never `SaveChanges` on admission queue | Pass |
| New entries use insert-only (no physical delete) | Pass |
| Zero affected rows → `AdmissionQueueConcurrencyException` | Pass |
| No schema change | Pass |
| Focused suite green | Pass — 27/27 |

## 7. Out of scope (deferred)

R-02 actor enforcement (deferred to the Security / Authorization phase and non-blocking); physician `QueMulaiPeriksa` / `QueSelesaiPeriksa`; cancel/`RemoveEntry` flows; Call/Withdraw/Redirect; `RowVersion` / additive schema (R-03).
