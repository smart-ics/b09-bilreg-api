# Taksaka — Operations Runbook

Operational procedures for Hospital IT (EDP). Adjust schedules to hospital policy. Items marked **(future)** apply when engine persistence and APIs are implemented.

---

## Scope

| System | Responsibility |
|--------|----------------|
| Taksaka.Server | Background processing host |
| SQL Server `Taksaka` | Platform persistence (schema pending) |
| Taksaka.Web | Operator console (optional) |

---

## Daily Checks

**Time:** Start of business day (or automated monitoring equivalent)

| # | Check | How | Expected |
|---|-------|-----|----------|
| 1 | Service running | Services MMC / `Get-Service Taksaka` | Running |
| 2 | Health endpoint | `GET /health` | HTTP 200 |
| 3 | API health | `GET /api/health` | `status: Healthy` |
| 4 | Error log scan | Review last 24h logs for `Error` / `Fatal` | No recurring errors |
| 5 | Disk space | Log volume | > 20% free |
| 6 | Plugin folder | `plugins\*.dll` count matches manifest | All expected DLLs present |
| 7 | **(future)** Queue depth | Operator console → Queue | Below alert threshold |
| 8 | **(future)** Dead letter count | Operator console / API | Stable or zero |

**Escalation:** If service down > 15 minutes, follow [Incident Response](#incident-response).

---

## Weekly Checks

**Time:** Weekly maintenance window

| # | Task | Procedure |
|---|------|-----------|
| 1 | Log rotation | Verify Serilog file sink retention; archive old logs |
| 2 | Plugin inventory | Compare `plugins/` with deployment manifest |
| 3 | Version record | Note server + plugin DLL file versions in change log |
| 4 | Health trend | Spot-check `/api/health` response times |
| 5 | **(future)** Failed job review | Review failed/retry jobs; ticket recurring failures |
| 6 | **(future)** Worker utilization | Identify workers at concurrency limit |
| 7 | Security patch | Check .NET runtime security advisories |
| 8 | Console smoke test | Open operator console; confirm SignalR connects |

---

## Monthly Maintenance

| # | Task | Procedure |
|---|------|-----------|
| 1 | Windows Update | Apply OS patches in approved window; restart if required |
| 2 | .NET runtime | Update ASP.NET Core runtime if security release |
| 3 | Certificate expiry | Check TLS cert on reverse proxy |
| 4 | Access review | Confirm only authorized staff have server RDP/admin |
| 5 | **(future)** Database index maintenance | Rebuild indexes on queue/history tables |
| 6 | **(future)** Purge old execution history | Per retention policy (e.g. > 90 days) |
| 7 | DR drill element | Restore test on non-prod (see [Disaster Recovery](#disaster-recovery)) |
| 8 | Documentation | Update runbook if configuration changed |

---

## Backup Verification

### Current state

`TaksakaDbContext` has no tables — database backup is preparatory.

| Asset | Backup method | Verification |
|-------|---------------|--------------|
| SQL `Taksaka` DB | SQL Agent full backup **(when schema exists)** | Monthly restore to test instance |
| `appsettings.Production.json` | Secure config backup | Diff against production quarterly |
| `plugins/` | File copy or artifact repository | Restore to test folder; file hash compare |
| Application binaries | Release artifact retention | Redeploy to test server |

**Monthly test:** Restore latest backup to `Taksaka_TEST` and start server against test DB.

---

## Database Maintenance

### Now

1. Ensure `Taksaka` database exists.
2. Verify service account connectivity monthly.

### When schema ships

1. **Statistics update** — weekly on high-churn tables.
2. **Index rebuild** — monthly during window.
3. **Growth monitoring** — alert if DB size exceeds forecast.
4. **History purge** — scheduled job or manual script per retention policy.

Coordinate with DBA before any manual `DELETE` on queue or history tables.

---

## Queue Cleanup

**Status: NOT IMPLEMENTED** — no persistent queue.

### Future procedure

1. Open operator console → Queue.
2. Identify stuck jobs (status `Running` > timeout threshold).
3. **Do not** delete without developer approval unless runbook defines safe cancel API.
4. Use admin API to cancel or move to dead letter per incident ticket.
5. Document job IDs affected.

**Emergency:** Stop service only if runaway job causes resource exhaustion; coordinate with development.

---

## Worker Verification

### Weekly

```powershell
# List plugin DLLs
Get-ChildItem C:\Apps\Taksaka\plugins\*.dll | Select Name, LastWriteTime, Length

# Confirm discovery in log after restart
Select-String -Path C:\Apps\Taksaka\logs\*.log -Pattern "Discovered plugin assembly"
```

Expected DLLs (baseline deployment):

- `Taksaka.Workers.Integration.dll`
- `Taksaka.Workers.Projection.dll`

Add hospital-specific workers to manifest.

### After plugin deployment

1. Restart service.
2. Verify one log line per new DLL.
3. **(future)** Submit test job; confirm completion in history.

---

## Health Verification

```powershell
# Liveness
Invoke-RestMethod https://taksaka.hospital.local/health

# Platform health
Invoke-RestMethod https://taksaka.hospital.local/api/health
```

Record `evaluatedAt` timestamp — stale values **(future)** may indicate monitor loop failure.

SignalR: open browser console on operator UI — expect `[SignalR] Connected to operations hub`.

---

## Incident Response

### Severity 1 — Service down

1. Acknowledge alert within 15 minutes.
2. Check service status and recent Windows Event Log / application log.
3. Attempt restart: `Restart-Service Taksaka`
4. If fails, run interactively: `dotnet Taksaka.Server.dll` to capture exception.
5. Escalate to development if configuration or code error.
6. Communicate to stakeholders if HIS background processing affected **(when jobs are live)**.

### Severity 2 — Degraded (future: queue backlog)

1. Identify failing worker or external dependency.
2. Pause non-critical job producers if possible.
3. Scale resources or increase concurrency per policy.
4. Post-incident review within 5 business days.

### Severity 3 — Console / monitoring only

1. SignalR disconnect — check CORS and API availability.
2. Non-blocking; fix during business hours.

---

## Disaster Recovery

### RTO / RPO targets (set by hospital)

| Metric | Suggested starting point |
|--------|--------------------------|
| RTO | 4 hours |
| RPO | 24 hours (SQL full backup daily when live) |

### Recovery steps

1. Provision replacement server with .NET 8 runtime.
2. Restore `C:\Apps\Taksaka` from artifact or backup.
3. Restore `plugins/` folder.
4. Restore SQL `Taksaka` database from latest backup.
5. Apply `appsettings.Production.json` from secure store.
6. Update DNS / load balancer to new host if needed.
7. Start service; run health checks.
8. **(future)** Verify queue integrity and replay failed jobs if needed.

### Partial failure

| Failed component | Action |
|------------------|--------|
| SQL Server | Failover to AG secondary or restore DB |
| App server only | Redeploy binaries; reattach to existing DB |
| Single plugin | Remove bad DLL; restart; redeploy fixed version |

---

## Upgrade Procedure

### Pre-upgrade

1. Read release notes.
2. Complete [07-production-checklist.md](07-production-checklist.md) validation on staging.
3. Backup database **(when schema exists)**, `plugins/`, and config.
4. Schedule maintenance window.
5. Notify users if operator console unavailable.

### Upgrade steps

1. Stop `Taksaka` service.
2. Backup current `C:\Apps\Taksaka` to `C:\Apps\Taksaka_backup_{date}`.
3. Deploy new publish output (preserve `appsettings.Production.json`).
4. Deploy updated plugin DLLs if included in release.
5. **(future)** Run `dotnet ef database update` or supplied SQL scripts.
6. Start service.
7. Verify health endpoints and plugin discovery logs.
8. Smoke test operator console.
9. Monitor logs for 30 minutes.

### Post-upgrade

1. Record versions in change log.
2. Remove old backup after stability period (e.g. 7 days).

---

## Rollback Procedure

1. Stop service.
2. Restore previous application folder from `Taksaka_backup_{date}`.
3. Restore previous `plugins/` if changed.
4. **(future)** Restore SQL database to pre-upgrade snapshot if migration ran.
5. Start service.
6. Verify health.
7. Log incident and root cause.

**Do not** rollback database alone without matching application version — schema mismatch risk when migrations exist.

---

## Contacts

| Role | Contact | When |
|------|---------|------|
| EDP on-call | _fill in_ | Service down |
| DBA | _fill in_ | Database restore |
| Development team | _fill in_ | Code/plugin defects |
| Vendor / integrator | _fill in_ | Third-party worker issues |

---

## Related Documents

- [03-operator-manual.md](03-operator-manual.md)
- [04-troubleshooting.md](04-troubleshooting.md)
- [09-administrator-guide.md](09-administrator-guide.md)
