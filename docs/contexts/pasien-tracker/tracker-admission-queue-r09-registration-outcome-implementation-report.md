# R-09 — Final Registration Outcomes and Queue Completion

## Outcome

Admisi Rajal now owns an immutable final Registration Outcome separate from Queue Entry completion.
`Done` means assistance ended; `BILRG_RegOutcome` records whether Registration was established.

## Model and constraints

- Stable ULID `OutcomeId` and exactly one row per `(AntrianId, NoUrut)`.
- `Established`: non-empty `RegId`, empty `ReasonCode`.
- `NotEstablished`: empty `RegId`, non-empty `ReasonCode`.
- Database checks mirror the Domain invariants.
- Records use the existing create/update/void audit columns but are insert-only in this slice.
- `ReasonCode` is an external contract. R-09 validates only that it is non-empty and invents no
  catalog or business reasons.

## Atomic finalization

Explicit Established and NotEstablished commands require an InService Queue Entry and matching
InService Loket claim/RowVersion. One repository operation inserts the outcome, conditionally marks
the entry Done, and releases the claim inside the existing local transaction. Unique-key conflicts,
stale entry state, and stale claim versions produce `AdmissionQueueConcurrencyException`.

Anonymous NotEstablished does not depend on or create Patient Tracker/Journey state. Recoverable
input validation failures occur before the transaction and do not create an outcome.

## Existing successful Registration compatibility

Walk-In and By-Booking handlers receive the new outcome repository through DI. Keyed admission
completion now records an Established outcome alongside the CAS entry completion and releases any
InService claim. The legacy create-on-registration compatibility path records Established after its
new already-Done entry is inserted, in the same outer transaction. Optional constructor fallback is
retained solely so existing unit-test/manual constructions continue to compile; production DI
always supplies the registered repository.

## Deployment

Apply `BILRG_RegOutcome.sql` after the R-07/R-08 queue-entry and current-Loket migrations. The
authoritative ReasonCode catalog remains an external dependency and can add validation later without
changing stored outcome semantics.

## Verification

- Build passes with existing warnings.
- Focused R-09 plus Registration compatibility suite: 18 passed, 0 failed.
- Combined focused R-05A through R-09 suite: 38 passed, 0 failed.
- `git diff --check` passes.
- Database constraint, concurrent winner, and forced intermediate-write rollback tests require the
  integration database to receive the R-06 through R-09 migrations.
