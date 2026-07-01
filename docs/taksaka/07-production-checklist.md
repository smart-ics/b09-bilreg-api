# Taksaka — Production Deployment Checklist

Use before first production deployment and before each major upgrade. Items reflect **current implementation**; deferred items are marked.

---

## Infrastructure

- [ ] Windows Server or Linux host provisioned with 2+ CPU, 2+ GB RAM
- [ ] .NET 8 ASP.NET Core Runtime installed (`dotnet --list-runtimes`)
- [ ] Dedicated service account created (least privilege)
- [ ] Install path created (e.g. `C:\Apps\Taksaka`) with `plugins\` and `logs\` subfolders
- [ ] Firewall rules allow internal access to Kestrel port only
- [ ] Reverse proxy / load balancer configured (IIS, nginx, YARP) if required
- [ ] TLS certificate installed and HTTPS enforced externally
- [ ] Process manager configured (Windows Service, NSSM, systemd)
- [ ] Service recovery policy: restart on failure

---

## Database

- [ ] SQL Server instance available (2019+ or Azure SQL)
- [ ] Database `Taksaka` created
- [ ] Service account granted appropriate permissions
- [ ] Connection string tested with `sqlcmd` or SSMS
- [ ] Connection string stored securely (not plain text in repo)
- [ ] **Deferred:** EF migrations applied (`TaksakaDbContext` has no schema yet)
- [ ] **Deferred:** Backup job scheduled for Taksaka database

---

## Configuration

- [ ] `appsettings.Production.json` deployed (not Development)
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] `ConnectionStrings:Taksaka` set via secure channel
- [ ] `PluginLoader:PluginsPath` set to absolute path
- [ ] `AllowedHosts` restricted to production hostnames
- [ ] `ASPNETCORE_URLS` set for production bind address
- [ ] Duplicate `Database:ConnectionString` removed or aligned with ConnectionStrings
- [ ] **Action required:** CORS origins updated from `localhost:5173` to production console URL

---

## Security

- [ ] Service account is not Local System / Administrator
- [ ] `TrustServerCertificate=False` and `Encrypt=True` for SQL (unless documented exception)
- [ ] Taksaka API not exposed to public internet without authentication layer
- [ ] **Deferred:** Authentication/authorization on API and SignalR
- [ ] Secrets rotation procedure documented
- [ ] Antivirus exclusion reviewed (avoid blind exclude of `plugins/`)

---

## Logging

- [ ] Serilog minimum level set to `Information` or `Warning` for production
- [ ] Durable log sink configured (file or centralized)
- [ ] Log retention policy defined (e.g. 30 days)
- [ ] Log directory writable by service account
- [ ] NSSM or equivalent stdout/stderr capture configured if no file sink

---

## Monitoring

- [ ] Load balancer health probe → `GET /health` or `GET /api/health`
- [ ] Process monitoring alert (service down)
- [ ] HTTP 5xx alert on reverse proxy
- [ ] **Deferred:** Queue depth monitoring
- [ ] **Deferred:** Dead letter count alert
- [ ] **Deferred:** Worker failure rate alert
- [ ] Disk space alert on log volume

---

## Backup

- [ ] **Deferred:** SQL backup schedule (when schema exists)
- [ ] `appsettings.Production.json` backed up in secure config store
- [ ] `plugins\` folder backed up or reproducible from build pipeline
- [ ] Deployment package version recorded

---

## Alerting

- [ ] On-call contact defined for Taksaka service down
- [ ] **Deferred:** Platform alert integration (email/SMS/webhook)
- [ ] **Deferred:** `Alert` entity pipeline

---

## Performance

- [ ] Expected worker count and concurrency documented
- [ ] SQL Server sized for future queue/history workload
- [ ] **Deferred:** Dispatcher poll interval tuned
- [ ] **Deferred:** Load test with representative job volume

---

## Validation

- [ ] `dotnet build -c Release` succeeds
- [ ] `dotnet test` passes
- [ ] Publish output contains `plugins\*.dll`
- [ ] Service starts without exception
- [ ] `curl https://{host}/health` returns 200
- [ ] `curl https://{host}/api/health` returns 200 with `status: Healthy`
- [ ] Plugin discovery lines appear in log
- [ ] Operator console loads (if deployed) and SignalR connects
- [ ] Swagger **not** exposed in Production (expected 404)
- [ ] Rollback package from previous version retained

---

## Post-Deployment Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| EDP / Administrator | | | |
| Developer | | | |
| DBA | | | |

---

## Related Documents

- [01-deployment-guide.md](01-deployment-guide.md)
- [08-operations-runbook.md](08-operations-runbook.md)
- [09-administrator-guide.md](09-administrator-guide.md)
