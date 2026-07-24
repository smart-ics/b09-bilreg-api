# R-06 — Service Point Authority and Immutable Queue Labels

## Outcome

R-06 introduces an Admission-specific Service Point master without replacing the existing
`ServicePointType` session reference used by physician queues. Anonymous intake now accepts only
`ServicePointId`, resolves the authoritative master server-side, rejects retired or unknown rows,
and derives Business Date from the server clock.

## Implemented contract

- `AdmissionServicePointModel` owns stable identity, display name, normalized one-character ASCII
  prefix, and Active/Retired state.
- `BILRG_AdmServicePoint` persists the master. A filtered unique index prevents two active Service
  Points from owning the same prefix; database checks enforce prefix and status values.
- New Admission Queue Sessions snapshot the master prefix into
  `BILRG_Antrian.QueuePrefixSnapshot`.
- Queue-header updates do not update `QueuePrefixSnapshot`; a later master prefix change therefore
  cannot rewrite an existing session label.
- The Domain formats labels as prefix plus four-digit `NoUrut`, validates `1-9999`, and returns no
  label when a historical session has no snapshot.
- `(AntrianId, NoUrut)` remains Queue Entry identity.
- No historical prefix values are fabricated and no backfill is included.

## Compatibility boundary

`ServicePointType` remains the lightweight snapshot/reference for existing non-Admission queues.
Only the anonymous Admission intake contract changed from caller-supplied code/name/date to
`ServicePointId`. Legacy registration configuration paths remain compatibility paths and must be
migrated to the master in a later explicit slice rather than silently changing unrelated APIs.

## Concurrency

The existing `UX_BILRG_Antrian_SequenceTag` database index remains the authority preventing two
daily sessions for the same Service Point and Business Date. R-06 does not add an in-memory lock.
The current handler has one-winner conflict behavior during simultaneous first intake; transparent
reload/retry remains part of the broader session-persistence hardening slice.

## Verification

- `dotnet build src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore`: passed (existing warnings).
- Focused R-06 tests: 10 passed, 0 failed.
- Covered: prefix normalization and invalid inputs, retirement rejection, authoritative intake
  resolution, immutable snapshot behavior, historical label unavailability, and label bounds.
- Active-prefix uniqueness and daily-session uniqueness are database constraints. Database-level
  race tests require applying the included migrations to the integration-test database.

## Deployment order

1. Apply `BILRG_AdmServicePoint.sql`.
2. Apply `BILRG_Antrian_M2_QueuePrefixSnapshot_Alter.sql`.
3. Seed operations-approved active Service Points and prefixes.
4. Deploy API/application binaries.

Do not enable anonymous intake before step 3; unknown Service Points are rejected by design.
