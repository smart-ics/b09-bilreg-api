# Phase 5 — Backend integration rollout and compatibility closure

**Status:** Implemented in source (2026-07-23)  
**Plan:** [TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md](./TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md) Phase 5  
**Prerequisite:** Phases 1–4 backend exits

## Outcome

Admission Queue V1 backend is packaged for reproducible **integration** deployment: migration
manifest, example seed, destructive disposable rollback, authenticated schema/config preflight,
workstation/edge guidance, API smoke/security-edge tests, and an explicit go/no-go checklist.
Production cutover execution, external clients, and SignalR scale-out remain out of scope.

## What shipped

### 1. Migration package

| Artifact | Role |
|----------|------|
| `AdmissionQueueMigrationManifest` | Single script-order / required-table / required-index source of truth |
| `AdmissionQueueMigrationFixture` | Consumes manifest (no drifted private script list) |
| `BILRG_AdmissionQueue_MigrationManifest.md` | Ops-facing order, guards, indexes, rollback boundary |
| `BILRG_AdmissionQueue_Seed_ServicePoints.example.sql` | Idempotent example Service Point seed |
| `BILRG_AdmissionQueue_Rollback.sql` | Destructive reverse drop for disposable DBs only |

### 2. Preflight / health surface

| Artifact | Role |
|----------|------|
| `IAdmissionQueueRolloutDal` / `AdmissionQueueRolloutDal` | Table + index existence probes |
| `IAdmissionQueueRolloutConfig` / `AdmissionQueueRolloutConfig` | Key-free flag + uniqueness summary |
| `AdmissionQueueGetRolloutStatusQry` | Composes schema + config response |
| `GET /api/v1/admission-queue/rollout/status` | Authenticated preflight on v1 controller |

### 3. Ops documentation

| Artifact | Role |
|----------|------|
| [TRACKER-ADMISSION-QUEUE-RUNBOOK.md](./TRACKER-ADMISSION-QUEUE-RUNBOOK.md) | Migrate, seed, config, edge, smoke, recovery, rollback |
| [TRACKER-ADMISSION-QUEUE-ROLLOUT-CHECKLIST.md](./TRACKER-ADMISSION-QUEUE-ROLLOUT-CHECKLIST.md) | Go/No-Go + legacy disposition |
| [TRACKER-ADMISSION-QUEUE-API-V1.md](./TRACKER-ADMISSION-QUEUE-API-V1.md) | Rollout route + Phase 5 compatibility notes |
| [ARTIFACTS.md](../../ARTIFACTS.md) | Index entries |

## Explicitly not done (by design)

- Production migration execution or signed production cutover
- Officer / Kiosk / Queue Display client packaging
- Changing default `LegacyEndpointsEnabled` / inventing deprecation dates
- ASP.NET `AddHealthChecks` middleware
- Redis / SignalR multi-node backplane (R-12B)
- R-02 claims-derived actor / ReasonCode allowlist

## Phase 5 exit checklist

| Criterion | Result |
|-----------|--------|
| Manifest + seed example + rollback SQL exist and match fixture order | Yes |
| Preflight status API returns schema/config readiness without secrets | Yes |
| Runbook + go/no-go cover migrate, workstation/edge, smoke, recovery, legacy | Yes |
| Focused tests green; Phase 1 remains concurrency proof of record | Yes (see evidence) |
| Implementation report + commit message suggestion | Yes |

## Test evidence

Focused filter (2026-07-23):

`AdmissionQueueGetRolloutStatusHandlerTest|AdmissionQueueMigrationManifestTest|AdmissionQueueRolloutConfigTest|AdmissionQueueApiContractTest|AdmissionQueueWorkstationOptionsValidatorTest`

**Passed: 23, Failed: 0**

Coverage includes:

- Manifest script order stability
- Rollout status all-ready / missing table / missing index
- Config uniqueness without exposing keys
- RolloutStatus route on authenticated v1 controller
- Unmapped workstation and missing `X-Workstation-Key` rejection
- Existing mismatch / duplicate-mapping validator cases

Real-SQL re-run remains optional when `BILREG_AQ_IT_*` is configured; Phase 1 report is still the
concurrency gate of record.

## Suggested commit message

```
feat(admission-queue): Phase 5 integration rollout and compatibility package

Codify migration/seed/rollback, add rollout preflight status API, and
document runbook plus go/no-go for integration deployment.
```
