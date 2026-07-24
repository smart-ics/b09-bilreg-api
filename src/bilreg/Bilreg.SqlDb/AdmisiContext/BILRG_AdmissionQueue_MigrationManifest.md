# Admission Queue — Migration Manifest

**Audience:** DBA / integration ops  
**Authority:** `Bilreg.Application` type `AdmissionQueueMigrationManifest` (executable source of truth)  
**Scripts root:** `src/bilreg/Bilreg.SqlDb/AdmisiContext/`

## Apply order (greenfield / disposable)

| # | Relative path | Guard (skip if table exists) |
|---|---------------|------------------------------|
| 1 | `AntrianFeature/BILRG_Antrian.sql` | `BILRG_Antrian` |
| 2 | `AntrianFeature/BILRG_AntrianEntry.sql` | `BILRG_AntrianEntry` |
| 3 | `AntrianFeature/BILRG_Antrian_M1_ServicePointCode_Alter.sql` | — (idempotent alter) |
| 4 | `AntrianFeature/BILRG_Antrian_M2_QueuePrefixSnapshot_Alter.sql` | — |
| 5 | `AntrianFeature/BILRG_AntrianEntry_M2_QueueOperations_Alter.sql` | — |
| 6 | `AntrianFeature/BILRG_AdmServicePoint.sql` | `BILRG_AdmServicePoint` |
| 7 | `AntrianFeature/BILRG_AdmLoketCurrentCall.sql` | `BILRG_AdmLoketCurrentCall` |
| 8 | `RegFeature/BILRG_RegOutcome.sql` | `BILRG_RegOutcome` |
| 9 | `AntrianFeature/BILRG_AdmBookingAssistance.sql` | `BILRG_AdmBookingAssistance` |
| 10 | `AntrianFeature/BILRG_AdmissionQueue_M3_Audit_Alter.sql` | — |
| 11 | `AntrianFeature/BILRG_AdmWorkstation.sql` | `BILRG_AdmWorkstation` |
| 12 | `AntrianFeature/BILRG_AdmQueueDisplay.sql` | `BILRG_AdmQueueDisplay` |
| 13 | `AntrianFeature/BILRG_AdmDisplayLoket.sql` | `BILRG_AdmDisplayLoket` |

Greenfield `CREATE TABLE` scripts are not wrapped `IF NOT EXISTS`. Prefer a fresh empty database for first apply. The real-SQL fixture skips a guarded script when its guard table already exists.

## Required indexes (preflight)

| Index | Table |
|-------|-------|
| `UX_BILRG_Antrian_SequenceTag` | `BILRG_Antrian` |
| `UX_BILRG_AdmLoketCurrentCall_ActiveEntry` | `BILRG_AdmLoketCurrentCall` |
| `IX_BILRG_AdmLoketCurrentCall_ActiveDisplay` | `BILRG_AdmLoketCurrentCall` |
| `IX_BILRG_AntrianEntry_OperationalWorklist` | `BILRG_AntrianEntry` |

## Seed

After migrate, apply an approved Service Point seed (example only):

`AntrianFeature/BILRG_AdmissionQueue_Seed_ServicePoints.example.sql`

Do not use example IDs/prefixes in production without Ops approval.

## Rollback boundary

| Environment | Action |
|-------------|--------|
| Disposable / integration (no retained data) | `AntrianFeature/BILRG_AdmissionQueue_Rollback.sql` |
| Brownfield / production with data | Disable `AdmissionQueueApi` feature flags + redeploy previous binary; **do not** DROP TABLE |

Rollback drops **additive Admission Queue tables only**. It does **not** drop physician/legacy `ta_*` maps or unrelated Tracker tables (`BILRG_PasienTracker*`).

## Related

- Phase 1 verification: `docs/contexts/pasien-tracker/tracker-admission-queue-phase1-slice1-verification-report.md`
- Runbook: `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-RUNBOOK.md`
- Go/No-Go: `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ROLLOUT-CHECKLIST.md`
