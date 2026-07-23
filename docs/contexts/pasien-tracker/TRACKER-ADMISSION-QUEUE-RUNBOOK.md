# Tracker–Admission Queue Runbook

**Audience:** Engineering, DBA, integration ops  
**Scope:** Backend V1 Admission Queue deploy to an **integration** environment  
**Not covered:** Production cutover execution, Officer/Kiosk/Display client packaging, SignalR multi-node scale-out

**Related**

| Artifact | Role |
|----------|------|
| [BILRG_AdmissionQueue_MigrationManifest.md](../../../src/bilreg/Bilreg.SqlDb/AdmisiContext/BILRG_AdmissionQueue_MigrationManifest.md) | Script order, guards, indexes, rollback boundary |
| [TRACKER-ADMISSION-QUEUE-ROLLOUT-CHECKLIST.md](./TRACKER-ADMISSION-QUEUE-ROLLOUT-CHECKLIST.md) | Go / No-Go gates |
| [TRACKER-ADMISSION-QUEUE-API-V1.md](./TRACKER-ADMISSION-QUEUE-API-V1.md) | Versioned API / security boundary |
| Phase 1 verification report | Concurrency / migration proof of record |

---

## 1. Prerequisites

- Disposable or installation-managed **integration** SQL Server (never production for first apply rehearsal)
- Bilreg.Api build that includes Phases 1–4 backend exits
- JWT auth unchanged (no new queue roles; R-02 deferred)
- Approved Service Point seed values (do not use example IDs in production)

---

## 2. Database migrate

Scripts root: `src/bilreg/Bilreg.SqlDb/AdmisiContext/`

Apply in the order documented in the migration manifest (same as `AdmissionQueueMigrationManifest.Scripts`). Greenfield `CREATE TABLE` scripts are not `IF NOT EXISTS`; prefer a fresh empty database for first apply. Guarded scripts may be skipped when the guard table already exists (fixture behavior).

After migrate, confirm critical indexes:

- `UX_BILRG_Antrian_SequenceTag`
- `UX_BILRG_AdmLoketCurrentCall_ActiveEntry`
- `IX_BILRG_AdmLoketCurrentCall_ActiveDisplay`
- `IX_BILRG_AntrianEntry_OperationalWorklist`

Or call authenticated `GET /api/v1/admission-queue/rollout/status` and require `allSchemaReady: true`.

### Seed Service Points

Apply an Ops-approved seed derived from:

`AntrianFeature/BILRG_AdmissionQueue_Seed_ServicePoints.example.sql`

Active `QueuePrefix` values must be unique. Seed at least one active Service Point before Kiosk intake smoke.

---

## 3. Application configuration

`appsettings` section `AdmissionQueueApi`:

```json
{
  "AdmissionQueueApi": {
    "LegacyEndpointsEnabled": true,
    "SignalRRefreshEnabled": true,
    "Workstations": [
      { "WorkstationKey": "ADM-01", "LoketKey": "L1" },
      { "WorkstationKey": "ADM-02", "LoketKey": "L2" }
    ]
  }
}
```

| Setting | Notes |
|---------|--------|
| `Workstations` | Unique `WorkstationKey` and unique `LoketKey` required at startup (`ValidateOnStart`) |
| `LegacyEndpointsEnabled` | Default `true`; set `false` only after consumer inventory go decision |
| `SignalRRefreshEnabled` | Default `true`; `false` binds no-op publisher; queue write truth unchanged |

### Trusted edge (required for Call accountability)

Reverse proxy / API edge **must** strip or overwrite client-supplied `X-Workstation-Key` and `X-Loket-Key`. The API validates mapping consistency but cannot alone prevent spoofing from an untrusted network path.

Officer Loket mutations require both headers; payload `loketKey` must match `X-Loket-Key`; workstation must map to that Loket.

---

## 4. Preflight health

```http
GET /api/v1/admission-queue/rollout/status
Authorization: Bearer <token>
```

Response (JSend `data`) includes:

- `allSchemaReady`, `tables[]`, `indexes[]` — schema preflight
- `legacyEndpointsEnabled`, `signalRRefreshEnabled` — feature flags
- `workstationMappingsUnique`, `workstationMappingCount` — **no keys leaked**

Integration go requires `allSchemaReady: true` and `workstationMappingsUnique: true` with a non-zero mapping count when officers will Call.

---

## 5. Smoke tests

### Focused regression (always)

```powershell
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj `
  --filter "FullyQualifiedName~AdmissionQueueApiContractTest|FullyQualifiedName~AdmissionQueueGetRolloutStatusHandlerTest|FullyQualifiedName~AdmissionQueueMigrationManifestTest|FullyQualifiedName~AdmissionQueueOperationalCommandsTest|FullyQualifiedName~RegistrationOutcomeTest|FullyQualifiedName~BookingAssistanceIntakeTest|FullyQualifiedName~SignalRAdmissionQueueRefreshPublisherTest"
```

### Real-SQL gate (integration SQL configured)

```powershell
$env:BILREG_AQ_IT_SERVER="(local)"
$env:BILREG_AQ_IT_DATABASE="bilreg_aq_it"
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj `
  --filter "FullyQualifiedName~AdmissionQueue|FullyQualifiedName~QueAnonymous|FullyQualifiedName~BookingAssistance|FullyQualifiedName~RegistrationOutcome|FullyQualifiedName~SequencerAdmission"
```

Fail-closed when env unset. Refuse database names containing `prod` / `production` / `hospital_hpl`.

### API smoke (authenticated, migrated DB)

1. `GET .../service-points?activeOnly=true` — seeded points present  
2. `POST .../intake` — returns QueueLabel  
3. `GET .../worklist?businessDate=yyyy-MM-dd` — includes entry  
4. `POST .../entries/{q}/{n}/call` with valid workstation headers — Outstanding  
5. `GET .../displays/current` — persisted snapshot matches claim  
6. Optional: connect SignalR `/hubs/admission-queue`, confirm non-authoritative `RefreshHint`

### Security-edge smoke

| Case | Expect |
|------|--------|
| Missing `X-Workstation-Key` / `X-Loket-Key` | 400 |
| Payload Loket ≠ header | 400 |
| Unmapped workstation | 400 |
| Workstation mapped to another Loket | 400 |
| Duplicate workstation/Loket in config | App fails `ValidateOnStart` |

### Restart / recovery

1. Call an entry; note `AnnouncementVersion` from `GET displays/current`  
2. Recycle Bilreg.Api  
3. Re-read `GET displays/current` — snapshot must match pre-restart persisted claim (SignalR not required)

### Load smoke

Phase 1 volume evidence (400 entries + 20 Loket claims; worklist page 100 and current-display under 5s) remains the proof of record. Re-run `AdmissionQueueRealSqlGateTest` volume scenario on the target integration SQL before promoting.

---

## 6. Rollback

| Situation | Action |
|-----------|--------|
| App defect / bad binary | Redeploy previous Bilreg.Api |
| Need to quiet legacy AQ routes | `LegacyEndpointsEnabled: false` + recycle |
| Need to quiet SignalR | `SignalRRefreshEnabled: false` + recycle (writes unchanged) |
| Disposable DB full removal | Backup → `BILRG_AdmissionQueue_Rollback.sql` |
| Production with retained data | **Never** DROP TABLE; disable flags + previous binary |

Rollback SQL does **not** drop `ta_*` physician maps or `BILRG_PasienTracker*`.

---

## 7. Legacy observation

Warning logs exist on legacy `POST /api/Antrian/anonymous-intake`, `POST /api/Antrian/start`, and physician `mulaiPeriksa` / `selesaiPeriksa`. Inventory disposition is decided on the go/no-go checklist — Phase 5 does not invent a deprecation date.
