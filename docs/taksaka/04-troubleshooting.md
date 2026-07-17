# Taksaka — Troubleshooting Guide

Structured diagnosis for common operational problems. Each item states whether the feature is **implemented** or **not yet implemented** in the current codebase.

---

## Service Won't Start

### Symptoms

- Process exits immediately
- HTTP connection refused on configured port
- Windows Service stops shortly after start

### Possible Causes

| Cause | Likelihood |
|-------|------------|
| .NET 8 runtime not installed | High |
| Port already in use | Medium |
| Invalid `ASPNETCORE_URLS` | Medium |
| Missing `appsettings.json` in working directory | Medium |
| Permission denied on `plugins/` or log path | Low |

### Investigation Steps

1. Run from command line to see exceptions:

   ```powershell
   cd C:\Apps\Taksaka
   dotnet Taksaka.Server.dll
   ```

2. Check Event Viewer / NSSM stderr log.
3. Verify runtime: `dotnet --list-runtimes` includes `Microsoft.AspNetCore.App 9.x`.
4. Test port: `netstat -ano | findstr :5000`

### Resolution

- Install ASP.NET Core 8 runtime.
- Change bind port via `ASPNETCORE_URLS`.
- Set service working directory to application folder.
- Fix folder permissions for service account.

---

## Worker Not Discovered

### Symptoms

- No `Discovered plugin assembly` log lines
- Expected worker DLL missing from logs

### Possible Causes

| Cause | Notes |
|-------|-------|
| DLL not in `plugins/` root | Subfolders not scanned |
| Wrong `PluginLoader:PluginsPath` | Relative path resolves from working directory |
| Build did not copy plugins | Missing `CopyPlugins` MSBuild output |
| Worker project not built | ReferenceOutputAssembly=false — separate build |

### Investigation Steps

1. List plugins folder:

   ```powershell
   Get-ChildItem C:\Apps\Taksaka\plugins\*.dll
   ```

2. Check configured path in logs: `Plugin directory not found at {path}`
3. Rebuild solution: `dotnet build -c Release` and republish.

### Resolution

- Place DLLs directly in `plugins/`.
- Set absolute `PluginLoader:PluginsPath` in production config.
- Restart after copying files.

> **Note:** Discovery only logs DLLs. Even when discovered, workers are **not loaded or executed** in current implementation.

---

## Worker Not Executing Jobs

### Symptoms

- Jobs submitted (future) never complete
- No worker activity in logs

### Possible Causes

**Expected behavior today** — entire execution pipeline is stub:

- `QueueManager` — no-op
- `Dispatcher` — no-op
- `PluginLoaderHostedService` — does not load `IWorker`
- `EngineHostedService` — does not start dispatch loop

### Investigation Steps

1. Confirm this is current code version (stubs).
2. Review [05-architecture-runtime.md](05-architecture-runtime.md) implementation status.

### Resolution

- No operator fix available until engine implementation ships.
- Track development milestone for queue + dispatcher + plugin loading.

---

## Database Connection Failure

### Symptoms

- Exception mentioning `SqlException` or EF Core
- Startup failure when DB validation is added

### Possible Causes

| Cause | Notes |
|-------|-------|
| SQL Server unreachable | Network/firewall |
| Wrong connection string | Typo in server/name/credentials |
| Database does not exist | Must create `Taksaka` database |
| Login failed | Service account permissions |

### Investigation Steps

1. Test connection:

   ```powershell
   sqlcmd -S SERVER -d Taksaka -E -Q "SELECT 1"
   ```

2. Verify effective connection string (environment override vs file).
3. Check SQL Server error log.

### Resolution

- Fix connection string in `ConnectionStrings:Taksaka`.
- Grant login and database access to service account.
- Create database if missing.

> **Current note:** Empty `TaksakaDbContext` may not open connections until first DB operation. Startup typically succeeds without SQL today.

---

## Queue Growing Continuously

### Status: NOT APPLICABLE (queue not persisted)

When implemented, investigate:

### Symptoms

- Rising pending job count
- Increasing oldest-job age

### Possible Causes

- Dispatcher stopped
- All workers disabled
- Resource manager blocking (concurrency exhausted)
- Downstream system slow

### Investigation Steps

1. Check dispatcher/hosted service health.
2. Review worker utilization dashboard.
3. Inspect running vs failed job ratio.

### Resolution

- Scale concurrency if safe.
- Fix failing workers causing retry backlog.
- Pause job producers temporarily.

---

## Dead Letter Increasing

### Status: NOT APPLICABLE (dead letter not implemented)

When implemented:

### Symptoms

- Dead letter count rises
- Alerts on failed permanent jobs

### Possible Causes

- Systematic worker bug
- External API down
- Invalid payload from producer
- Retry policy exhausted

### Investigation Steps

1. Sample recent dead letter messages and stack traces.
2. Correlate with external dependency status.
3. Check for deployment correlation.

### Resolution

- Fix root cause in worker or upstream data.
- Replay after fix (when replay API exists).

---

## Scheduler Not Firing

### Status: NOT APPLICABLE (scheduler stub)

### Symptoms

- Expected scheduled jobs never appear

### Possible Causes

- `Scheduler.StartAsync` not invoked by host loop
- No schedule definitions configured
- Clock/timezone misconfiguration

### Investigation Steps

1. Confirm `EngineHostedService` does not call `IScheduler.StartAsync` today.
2. Review scheduler configuration when added.

### Resolution

- Await scheduler implementation.
- For urgent needs, trigger jobs manually via API when job creation endpoint exists.

---

## Plugin Load Failure

### Symptoms

- DLL present but not logged
- Future: reflection/load exception at startup

### Possible Causes

| Cause | Notes |
|-------|-------|
| Wrong .NET target (`net9.0` vs `net8.0`) | Load failure when loading is implemented |
| Missing dependency DLL | Worker depends on packages not copied |
| Blocked by antivirus | Quarantined DLL |

### Investigation Steps

1. Verify assembly target framework with `dotnet tool` or ILSpy.
2. Copy all dependency DLLs alongside worker.
3. Check Windows Defender logs.

### Resolution

- Rebuild worker for `net8.0`.
- Deploy complete dependency set.
- Whitelist application folder.

---

## Configuration Errors

### Symptoms

- Wrong database targeted
- Plugins path points to wrong directory
- CORS blocks operator console

### Possible Causes

- Environment variable typo (`__` vs `:`)
- Relative path from wrong working directory
- Development `appsettings` deployed to production

### Investigation Steps

1. Dump effective config (temporary):

   ```csharp
   // Development only — not in production build
   ```

2. Compare `appsettings.Production.json` with environment overrides.
3. Test CORS from browser developer tools network tab.

### Resolution

- Use absolute paths for `PluginLoader:PluginsPath`.
- Standardize on `ConnectionStrings:Taksaka`.
- Update CORS origins in `ServerServiceCollectionExtensions` for production UI URL.

---

## Timeout Issues

### Status: NOT ENFORCED

`WorkerExecutionPolicy.Timeout` is declared on sample workers but dispatcher does not enforce it.

When implemented:

### Symptoms

- Jobs stuck in Running
- Timeout alerts

### Investigation

- Compare job duration vs worker policy timeout.
- Check for blocked I/O (HTTP, SQL).

### Resolution

- Fix slow external calls.
- Adjust worker policy if legitimately long-running.

---

## Retry Loop

### Status: NOT APPLICABLE

### Symptoms (future)

- Same job retries indefinitely
- Retry queue never drains

### Causes

- `MaxRetryCount = null` (unlimited) on worker
- Transient error that is actually permanent

### Resolution

- Set finite `MaxRetryCount`.
- Fix worker logic; move poison messages to dead letter.

---

## High CPU

### Symptoms

- Sustained high CPU on Taksaka process

### Possible Causes (current)

| Cause | Notes |
|-------|-------|
| Development hot reload | Dev only |
| Tight polling loop | Not present in stubs |
| Many SignalR connections | Possible if many consoles open |

### Investigation

1. `dotnet-counters` or Performance Monitor — process CPU.
2. Count active connections to `/hubs/operations`.
3. Profile when dispatch loop is implemented.

### Resolution

- Limit operator console instances.
- Throttle dispatcher poll interval when configurable.

---

## High Memory

### Symptoms

- Growing working set over days

### Possible Causes

- Memory leak in future queue/history retention
- Large log buffers
- Plugin assembly load contexts not unloaded

### Investigation

1. Monitor over 24–48 hours.
2. Capture dump if growth is linear.
3. Review Serilog sink buffering.

### Resolution

- Restart during maintenance window.
- Configure log sink flush settings.
- Implement history retention policy when persistence exists.

---

## Slow Execution

### Status: NOT MEASURABLE (no job execution)

When workers run:

### Investigation

- Compare execution history durations.
- Check SQL, network latency for integration workers.
- Review `MaxConcurrency` — may be queueing.

### Resolution

- Optimize worker code path.
- Increase concurrency if resources allow.

---

## SignalR / Operator Console Issues

### Symptoms

- `Connection failed (backend may be offline)`
- API calls fail from browser

### Causes

- Backend down
- Wrong `VITE_API_BASE_URL`
- CORS policy blocks origin
- Mixed HTTP/HTTPS content

### Resolution

- Start backend.
- Align frontend build URL with API.
- Update CORS policy.
- Use HTTPS consistently.

---

## Swagger Not Available

### Symptoms

- `/swagger` returns 404

### Cause

Swagger enabled only when `ASPNETCORE_ENVIRONMENT=Development`.

### Resolution

- Expected in production. Use `/health` and `/api/health` instead.

---

## Related Documents

- [01-deployment-guide.md](01-deployment-guide.md)
- [03-operator-manual.md](03-operator-manual.md)
- [08-operations-runbook.md](08-operations-runbook.md)
