# Admission Queue Phase 1 / Slice 1 — Verification Report

**Status:** Complete (local disposable SQL gate executed)  
**Date:** 2026-07-23  
**Scope:** Migrated-database operational proof and daily-session race hardening  
**Governing plan:** [TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md](./TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md) Phase 1; [TRACKER-ADMISSION-QUEUE-ARCHITECTURE-RECONCILIATION.md](./TRACKER-ADMISSION-QUEUE-ARCHITECTURE-RECONCILIATION.md) §7 Slice 1

## 1. Database target

| Item | Value |
|------|-------|
| Server | `(local)` (SQL Server 2022 Developer Edition, ProductVersion `16.0.1190.2`) |
| Database | `bilreg_aq_it` (disposable; created for this gate) |
| Auth | `bilregLogin` (aligned with `ConnStringHelper`) |
| Env gate | `BILREG_AQ_IT_SERVER`, `BILREG_AQ_IT_DATABASE` (fail-closed; never soft-skip; refuses names containing `prod` / `production` / `hospital_hpl`) |
| Production | Not targeted |

## 2. Script apply order

Applied by `AdmissionQueueMigrationFixture` from `src/bilreg/Bilreg.SqlDb/AdmisiContext/`:

1. `AntrianFeature/BILRG_Antrian.sql`
2. `AntrianFeature/BILRG_AntrianEntry.sql`
3. `AntrianFeature/BILRG_Antrian_M1_ServicePointCode_Alter.sql` (creates `UX_BILRG_Antrian_SequenceTag`)
4. `AntrianFeature/BILRG_Antrian_M2_QueuePrefixSnapshot_Alter.sql`
5. `AntrianFeature/BILRG_AntrianEntry_M2_QueueOperations_Alter.sql`
6. `AntrianFeature/BILRG_AdmServicePoint.sql`
7. `AntrianFeature/BILRG_AdmLoketCurrentCall.sql`
8. `RegFeature/BILRG_RegOutcome.sql`
9. `AntrianFeature/BILRG_AdmBookingAssistance.sql`
10. `AntrianFeature/BILRG_AdmissionQueue_M3_Audit_Alter.sql`

Seed after migrate: active Service Points `AQIT-A` (prefix A), `AQIT-B` (prefix B).

Post-migrate indexes confirmed present: `UX_BILRG_Antrian_SequenceTag`, `UX_BILRG_AdmLoketCurrentCall_ActiveEntry`, `IX_BILRG_AdmLoketCurrentCall_ActiveDisplay`, `IX_BILRG_AntrianEntry_OperationalWorklist`.

## 3. Commands

```powershell
$env:BILREG_AQ_IT_SERVER="(local)"
$env:BILREG_AQ_IT_DATABASE="bilreg_aq_it"
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj `
  --filter "FullyQualifiedName~AdmissionQueue|FullyQualifiedName~QueAnonymous|FullyQualifiedName~BookingAssistance|FullyQualifiedName~RegistrationOutcome|FullyQualifiedName~SequencerAdmission"
```

Unit runs without env vars must exclude `Category=AdmissionQueueRealSql` (fixture throws if unset).

## 4. Results

| Scenario | Result |
|----------|--------|
| Clean migrate + `UX_BILRG_Antrian_SequenceTag` | Pass |
| Same Loket / two entries | Pass — one winner Call; loser `AdmissionQueueConcurrencyException`; one active claim |
| Two Loket / one entry | Pass — one owner; no double-active entry |
| Stale RowVersion Recall/Start | Pass — CAS fail; claim remains, no orphan |
| Release vs Recall after Withdraw | Pass — Recall rejected; claim stays Released |
| Concurrent first daily-session create (×2 via repeatability test) | Pass — one SequenceTag session; distinct `NoUrut` for all parallel intakes |
| Redirect rollback (stale claim) | Pass — origin remains Waiting; no replacement row; claim intact |
| Outcome rollback (stale claim) | Pass — entry stays InService; no `BILRG_RegOutcome` row |
| Booking-assistance unique race | Pass — one active assistance; losers `Existing=true` |
| 9999 exhaustion | Pass — allocate 9999 then `SequenceExhaustedException`; no second same-date session |
| Representative volume worklist/current-display | Pass — 400 entries + 20 Loket claims; worklist page 100 and current-display under 5s; `SET STATISTICS IO ON` evidence captured in test |
| Focused AQ regression (AdmissionQueue / QueAnonymous / BookingAssistance / RegistrationOutcome / SequencerAdmission) | **64 / 64 passed** |

### Production fix applied

Concurrent first-session unique `SequenceTag` race was demonstrated and fixed narrowly:

- `AdmissionQueueSessionRaceException` from `AntrianRepo.SaveNewEntry` on SQL 2601/2627
- `QueAnonymousIntakeHandler` one-winner reload by SequenceTag, then continue allocation
- Unit coverage: `QueAnonymousIntakeHandlerTest.Intake_WhenConcurrentSessionRace_ThenReloadsWinnerAndContinues`

No second allocator, lock table, or `QueueEntryId` introduced.

## 5. Exit gate checklist

1. Clean disposable database migrates reproducibly with recorded script order — **Yes**
2. Mandatory real-SQL race/rollback/exhaustion tests pass repeatedly — **Yes**
3. No double-active claim or orphan active entry in failure cases — **Yes**
4. Representative worklist/current-display query evidence acceptable — **Yes**
5. Existing focused Admission Queue regression suite passes — **Yes (64/64)**
6. Production-code fix limited to demonstrated defect (session one-winner reload) — **Yes**

## 6. Remaining deployment qualifications

- This gate used a **local disposable** database, not an installation-managed integration environment. Ops should re-run with their own `BILREG_AQ_IT_*` target before promoting migrations.
- Greenfield `CREATE TABLE` scripts are not wrapped `IF NOT EXISTS`; the fixture skips when the guard table already exists. Fresh empty DBs are the intended target.
- `ConnStringHelper` still hardcodes `bilregLogin` / `bilreg123!` for DAL connections; env user/password are for documentation/alignment and probe connection string.
- Shared `devTest` / `ConnStringHelper.GetTestEnv()` remains unsuitable as this gate.
- Phase 2–5 work (Admisi enrichment, AssistanceRequired wiring, SignalR, rollout packaging) is out of scope and not claimed here.
- Unrelated pre-existing `AntrianMapModelTest` failures under the broader `AntrianFeature` filter are outside this slice.

## 7. Artifacts

| Artifact | Path |
|----------|------|
| Env gate | `src/bilreg/Bilreg.Test/AdmisiContext/AntrianFeature/AdmissionQueueRealSqlTestEnv.cs` |
| Migration fixture | `src/bilreg/Bilreg.Test/AdmisiContext/AntrianFeature/AdmissionQueueMigrationFixture.cs` |
| Real-SQL suite | `src/bilreg/Bilreg.Test/AdmisiContext/AntrianFeature/AdmissionQueueRealSqlGateTest.cs` |
| Intake reload | `src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/QueAnonymousIntakeCmd.cs` |
| Session race signal | `src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/AdmissionQueueSessionRaceException.cs` |
| Repo catch | `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianRepo.cs` |
