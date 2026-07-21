# Admisi Ranap — Rollout Checklist

**Artifact role:** OPERATION — production rollout gates  
**Audience:** Engineering, DBA, Admisi operations

---

## Pre-requisites

- [ ] Phases 0–5 deployed (domain, persistence, handlers, API, gateways)
- [ ] Phase 7 deployed (audit logging, rollout flag, rollout status, rollback SQL)
- [ ] `BILRG_AuditLog` table exists
- [ ] `appsettings` section `AdmisiRanap` present
- [ ] `dotnet test --filter FullyQualifiedName~AdmisiRanapContext` green
- [ ] `dotnet test --filter FullyQualifiedName~RegFeature` green (legacy regression)

---

## R1 — Database

| Engineering | Operations |
|-------------|------------|
| [ ] Run 4 create scripts in order (see runbook) | [ ] Confirm 4 tables exist via SQL or rollout status |
| [ ] Brownfield: run M1/M2 alters only if needed | [ ] Archive migration log |
| [ ] Rollback script available (`BILRG_AdmisiRanap_Phase7_Rollback.sql`) | [ ] Backup taken before first production write |

---

## R2 — Application deploy

| Engineering | Operations |
|-------------|------------|
| [ ] Deploy `Bilreg.Api` with Phase 7 build | [ ] JWT auth unchanged (no new permissions) |
| [ ] `AdmisiRanap:Enabled` set per environment policy | [ ] Ward team notified of waiting-list API |
| [ ] `GET /api/admisi-ranap/rollout/status` → `allTablesReady: true` | [ ] Smoke WF-01 on staging |

---

## R3 — Workflow validation

| ID | Workflow | Manual E2E | Automated (`AdmisiRanapWorkflowTest`) |
|----|----------|------------|----------------------------------------|
| WF-01 | Direct Admission | [ ] | [ ] |
| WF-02 | Planned Admission | [ ] | [ ] |
| WF-03 | Elective Admission | [ ] | [ ] |
| WF-04 | Patient Transfer | [ ] | [ ] |

---

## R4 — Production enablement

| Engineering | Operations |
|-------------|------------|
| [ ] Set `AdmisiRanap:Enabled: true` in production | [ ] Admisi staff trained on new routes |
| [ ] Monitor `BILRG_AuditLog` for write actions | [ ] Ward consumes `GET /api/admisi-ranap/waiting-list` |
| [ ] No changes to legacy `RegFeature` endpoints verified | [ ] Support runbook link distributed |

---

## Rollback

| Situation | Action |
|-----------|--------|
| Module misbehaviour | `AdmisiRanap:Enabled: false` + app recycle |
| Application defect | Redeploy previous binary |
| Full removal (no data retention) | Backup → `BILRG_AdmisiRanap_Phase7_Rollback.sql` |
| Legacy regression | AdmisiRanap is additive; disable flag; investigate separately |

---

## Related

| Path | Role |
|------|------|
| [`admisi-ranap-runbook.md`](admisi-ranap-runbook.md) | Procedures and troubleshooting |
| [`admisi-ranap-implementation-plan.md`](admisi-ranap-implementation-plan.md) | Phase ledger |
