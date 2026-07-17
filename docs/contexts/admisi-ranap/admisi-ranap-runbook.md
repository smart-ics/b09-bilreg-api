# admisi-ranap-runbook.md — Rawat Inap Admission Operations

> **Canonical location:** `docs/contexts/admisi-ranap/admisi-ranap-runbook.md`  
> **Related:** [`admisi-ranap-rollout-checklist.md`](admisi-ranap-rollout-checklist.md), [`admisi-ranap-implementation-plan.md`](admisi-ranap-implementation-plan.md), [`admisi-ranap-domain.md`](admisi-ranap-domain.md)

---

## PURPOSE

Operational guide for DBA, release engineer, Admisi admin, and Ward operators deploying and validating **Rawat Inap Admission** (`AdmisiRanapContext`).

Does not describe internal architecture. For design, see [`admisi-ranap-architecture.md`](admisi-ranap-architecture.md).

**Authorization:** JWT authentication only (`[Authorize]`). Permission policies (Phase 6) are **not** required for rollout.

---

## PREREQUISITES

| Item | Requirement |
|------|-------------|
| Audit log table | `BILRG_AuditLog` deployed (`Bilreg.SqlDb/Shared/AuditLogFeature/BILRG_AuditLog.sql`) |
| Master data | Valid `Pasien`, `Ppa` (dokter), `Kelas`, `Bangsal` rows for integration gateways |
| Application build | `Bilreg.Api` includes AdmisiRanap controllers and gateways |
| SQL access | DBA can run scripts under `Bilreg.SqlDb/AdmisiRanapContext/` |
| Config | `AdmisiRanap:Enabled` in `appsettings` (default `true`) |

---

## DATABASE MIGRATION ORDER

Run scripts **in order** on the target database. Create scripts are idempotent (`IF OBJECT_ID ... IS NULL`).

### Greenfield (recommended)

| Order | Script |
|-------|--------|
| 1 | `Bilreg.SqlDb/AdmisiRanapContext/OpnameRequestFeature/BILRG_AdmOpnameRequest.sql` |
| 2 | `Bilreg.SqlDb/AdmisiRanapContext/ReservationFeature/BILRG_AdmReservation.sql` |
| 3 | `Bilreg.SqlDb/AdmisiRanapContext/AdmissionFeature/BILRG_AdmAdmission.sql` |
| 4 | `Bilreg.SqlDb/AdmisiRanapContext/WaitingListFeature/BILRG_BedWaitingList.sql` |

### Brownfield only

If tables were created from an early Phase-2 snapshot with deprecated columns, run alter scripts **after** the matching create script:

| Script | Purpose |
|--------|---------|
| `BILRG_AdmOpnameRequest_M1_DropPasienSnapshot_Alter.sql` | Drop patient snapshot columns |
| `BILRG_AdmReservation_M2_DropOpnameRequestId_Alter.sql` | Drop `OpnameRequestId` column |
| `BILRG_AdmAdmission_M1_DropPasienSnapshot_Alter.sql` | Drop patient snapshot columns |
| `BILRG_BedWaitingList_M1_DropPasienSnapshot_Alter.sql` | Drop patient snapshot columns |

### Post-migration verification (SQL)

```sql
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
 WHERE TABLE_NAME IN (
   'BILRG_AdmOpnameRequest',
   'BILRG_AdmReservation',
   'BILRG_AdmAdmission',
   'BILRG_BedWaitingList')
 ORDER BY TABLE_NAME;
-- Expect 4 rows
```

### Rollback (destructive)

`Bilreg.SqlDb/AdmisiRanapContext/BILRG_AdmisiRanap_Phase7_Rollback.sql`

**Warning:** Drops all four AdmisiRanap tables and indexes. Use only when no production data exists or after full backup.

---

## APPLICATION DEPLOYMENT

1. Deploy SQL scripts (above).
2. Deploy `Bilreg.Api` binary.
3. Verify `GET /api/admisi-ranap/rollout/status` — all tables `ready: true`.
4. Confirm `AdmisiRanap:Enabled` is `true` (or set deliberately after smoke tests).
5. Run automated tests (see [`admisi-ranap-rollout-checklist.md`](admisi-ranap-rollout-checklist.md)).

**Safe default:** `AdmisiRanap:Enabled` is `true` after deploy. Set to `false` to disable write/read business endpoints without binary rollback. Rollout status endpoint remains available.

---

## OPERATIONAL VALIDATION

After deploy or incident:

| Check | How |
|-------|-----|
| Tables deployed | `GET /api/admisi-ranap/rollout/status` → `allTablesReady: true` |
| Module enabled | Same response → `enabled: true` |
| Ward queue | `GET /api/admisi-ranap/waiting-list` returns array (may be empty) |
| Opname list | `GET /api/admisi-ranap/opname-request` returns 200 |
| Admission lookup | `GET /api/admisi-ranap/admission` returns 200 |
| Audit trail | Write action produces row in `BILRG_AuditLog` with `EntityName` matching aggregate |

---

## E2E WORKFLOW CHECKLIST

Manual smoke after deploy. Automated equivalents: `AdmisiRanapWorkflowTest` (WF-01 … WF-04).

### WF-01 — Direct Admission

| Step | HTTP | Expected |
|------|------|----------|
| 1 | `POST /api/admisi-ranap/opname-request` | `OpnameRequestId` returned; status Requested |
| 2 | `POST /api/admisi-ranap/admission/from-opname-request` | `RegId` returned; opname Fulfilled |
| 3 (optional) | `POST /api/admisi-ranap/waiting-list` | `WaitingListId` returned; status Waiting |

### WF-02 — Planned Admission

| Step | HTTP | Expected |
|------|------|----------|
| 1a | `POST /api/admisi-ranap/opname-request` | Opname created |
| 1b | `POST /api/admisi-ranap/reservation` | Reservation created (independent of opname) |
| 2 | `PUT /api/admisi-ranap/reservation/{id}` | Reservation Maintained |
| 3 | `POST /api/admisi-ranap/admission/from-reservation` | Admission created; reservation Realized |
| 4 (optional) | `POST /api/admisi-ranap/waiting-list` | Waiting list created |

### WF-03 — Elective Admission

| Step | HTTP | Expected |
|------|------|----------|
| 1 | `POST /api/admisi-ranap/reservation` | Reservation Reserved |
| 2 | `PUT /api/admisi-ranap/reservation/{id}` | Reservation Maintained |
| 3 | `POST /api/admisi-ranap/admission/from-reservation` | Admission Admitted |
| 4 (optional) | `POST /api/admisi-ranap/waiting-list` | Waiting list Waiting |

### WF-04 — Patient Transfer

Ward release and re-accommodation are **external** to this module.

| Step | HTTP | Expected |
|------|------|----------|
| 1 | Ensure existing Admission in Admitted/Updated status | `GET /api/admisi-ranap/admission/{regId}` |
| 2 | `POST /api/admisi-ranap/waiting-list` | New WL for same `RegId`; Ward consumes via list API |

---

## TROUBLESHOOTING

| Symptom | Likely cause | Action |
|---------|--------------|--------|
| 503 on all admisi-ranap routes | `AdmisiRanap:Enabled` is `false` | Set `Enabled: true` or use rollout status only |
| 401 Unauthorized | Missing/invalid JWT | Obtain valid token; unchanged from other modules |
| Active admission exists | BR-RI-004 | Complete or cancel existing admission first |
| Opname must be Requested | Wrong opname status | Cancel and recreate, or use different opname |
| Reservation must be Maintained | Skipped maintain step | `PUT reservation/{id}` before process admission |
| Waiting list already active | BR-RI-008 | Close existing WL before creating new one |
| Patient/doctor not found | Master data missing | Verify Pasien/Ppa in master tables |
| Invalid object name (SQL) | Tables not deployed | Run create scripts; check rollout status |
| Module disabled but status works | By design | Rollout endpoint bypasses enabled filter |

---

## ROLLBACK

| Situation | Action |
|-----------|--------|
| Incident during rollout | Set `AdmisiRanap:Enabled: false` in appsettings; recycle app |
| Bad application build | Redeploy previous `Bilreg.Api` binary |
| Need to remove module entirely | Backup data → run `BILRG_AdmisiRanap_Phase7_Rollback.sql` |
| Legacy RegFeature affected | Should not occur (additive module); run `dotnet test --filter FullyQualifiedName~RegFeature` |

**Database forward-only:** Production rollback typically leaves tables in place. SQL drop is optional and destructive.

---

## RELATED ARTIFACTS

| Path | Role |
|------|------|
| [`admisi-ranap-rollout-checklist.md`](admisi-ranap-rollout-checklist.md) | Pre/post deploy gates |
| [`admisi-ranap-phase-7-implementation-report.md`](admisi-ranap-phase-7-implementation-report.md) | What shipped in Phase 7 |
| [`docs/shared/audit-log.md`](../../shared/audit-log.md) | Audit logging developer guide |
