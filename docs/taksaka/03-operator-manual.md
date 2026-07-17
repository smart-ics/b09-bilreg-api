# Taksaka — Operator Manual

Guide for platform operators and on-call engineers. Describes **what works today** in the codebase and what requires future implementation.

---

## Platform Components

| Component | Location | Role |
|-----------|----------|------|
| Taksaka.Server | `src/taksaka.backend/Taksaka.Server` | API host, SignalR, plugin scan, engine DI |
| Taksaka.Web | `src/taksaka.frontend/Taksaka.Web` | Operator console (UI shell) |
| plugins/ | Next to server binary | Worker DLL drop folder |

---

## Starting the Service

### Development

**Backend:**

```powershell
cd src\taksaka.backend\Taksaka.Server
dotnet run --launch-profile http
```

Listens on `http://localhost:5000` (see `Properties/launchSettings.json`).

**Operator console:**

```powershell
cd src\taksaka.frontend\Taksaka.Web
npm run dev
```

Opens `http://localhost:5173` with API proxy to port 5000.

### Production

Start the Windows service, IIS site, or process manager configured during deployment (see [01-deployment-guide.md](01-deployment-guide.md)).

Verify startup:

```powershell
curl http://localhost:5000/health
curl http://localhost:5000/api/health
```

---

## Stopping the Service

| Method | Command / Action |
|--------|------------------|
| Development | `Ctrl+C` in terminal |
| Windows Service | `Stop-Service Taksaka` or Services MMC |
| Graceful | SIGTERM / `nssm stop Taksaka` |

`EngineHostedService` and `PluginLoaderHostedService` log stop messages; no custom drain logic is implemented.

---

## Restarting

1. Stop the service.
2. Deploy updated binaries/plugins if applicable.
3. Start the service.
4. Confirm health endpoints return 200.
5. Refresh operator console browser tab (SignalR auto-reconnects when implemented).

> Plugin hot-reload is **not implemented**. Restart is required after plugin DLL changes.

---

## Viewing Logs

| Source | Location |
|--------|----------|
| Console | stdout when run interactively or via NSSM redirect |
| Serilog | Configured sinks in `Serilog:WriteTo` (default: Console only) |
| ASP.NET request log | Serilog request logging middleware |

**Useful log messages:**

| Message | Meaning |
|---------|---------|
| `Taksaka Engine hosted service started.` | Host registered engine hosted service |
| `Discovered plugin assembly: {path}` | Plugin scan found a DLL |
| `Plugin directory not found at {path}` | Created empty plugins folder |
| `[SignalR] Connected to operations hub` | Frontend connected (browser console) |

Increase verbosity:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Debug",
    "Override": { "Microsoft": "Information" }
  }
}
```

---

## Viewing Execution History

**Status: NOT IMPLEMENTED**

`ExecutionHistory` entity exists in `Taksaka.Core` but:

- No persistence mapping in `TaksakaDbContext`
- No REST API for history
- Operator console Dashboard shows "coming soon"

**Workaround:** Application logs only.

---

## Monitoring Queue

**Status: NOT IMPLEMENTED**

`QueuePage.vue` displays placeholder text. `QueueManager` does not store jobs.

**When implemented**, monitor via:

- Operator console → Queue
- SignalR event `QueueChanged` (defined in architecture; not published yet)

**Today:** No queue metrics available.

---

## Monitoring Workers

**Status: NOT IMPLEMENTED**

`WorkersPage.vue` is a placeholder. Plugins are discovered and logged at startup but not loaded or status-tracked.

**Today — manual check:**

1. Inspect server logs for `Discovered plugin assembly` lines.
2. Verify DLL files exist in `plugins/` folder.

Sample workers in repository:

| DLL | Worker name | Category |
|-----|-------------|----------|
| `Taksaka.Workers.Integration.dll` | `sample-integration` | Integration |
| `Taksaka.Workers.Projection.dll` | `sample-projection` | Projection |

---

## Replay Failed Jobs

**Status: NOT IMPLEMENTED**

No API, UI, or `IDeadLetterManager` implementation beyond stub.

---

## Retry Failed Jobs

**Status: NOT IMPLEMENTED**

`RetryManager.ScheduleRetryAsync` is a no-op. No operator action available.

---

## Dead Letter Handling

**Status: NOT IMPLEMENTED**

`IDeadLetterManager.MoveToDeadLetterAsync` is a no-op. `JobStatus.DeadLetter` enum exists for future use.

**Planned workflow** (from domain docs):

1. Job exhausts retries → Dead Letter
2. Operator reviews failure reason in console
3. Operator triggers replay → new job enqueued

---

## Health Monitoring

### HTTP probes

| Endpoint | Use |
|----------|-----|
| `GET /health` | Simple liveness — always returns Healthy |
| `GET /api/health` | Returns `IHealthMonitor` snapshot |

Example response (`/api/health`):

```json
{
  "status": "Healthy",
  "dimension": "Platform",
  "evaluatedAt": "2026-07-01T10:00:00Z",
  "platform": "Healthy"
}
```

`HealthPage.vue` in operator console is a placeholder.

### SignalR

Hub: `/hubs/operations`  
Client event: `HealthChanged` (server method `PublishHealthChanged` exists but is not called by engine)

Frontend subscribes on connect (`signalrClient.ts`).

---

## Alerts

**Status: NOT IMPLEMENTED**

`AlertsPage.vue` is a placeholder. `Alert` entity exists; no alert pipeline.

---

## Plugin Management

### Current behavior

1. Build copies worker DLLs to `bin/{Configuration}/net8.0/plugins/`.
2. At startup, `PluginLoaderHostedService` lists `*.dll` in configured path.
3. Assemblies are **not** loaded; workers **cannot** execute.

### Add a plugin (deployment)

1. Build worker project targeting `net8.0`.
2. Copy output DLL (and dependencies if any) to `plugins/`.
3. Restart Taksaka.Server.
4. Confirm discovery log line.

### Remove a plugin

1. Delete DLL from `plugins/`.
2. Restart server.

### Enable / disable worker

**Not implemented.** No runtime toggle. Removal + restart is the only option today.

---

## Backup Considerations

| Asset | Backup needed | Notes |
|-------|---------------|-------|
| SQL Server `Taksaka` DB | Yes (when schema exists) | Currently empty context — no tables |
| `appsettings.Production.json` | Yes | Connection strings, paths |
| `plugins/*.dll` | Yes | Or rebuild from source |
| Logs | Optional | Per retention policy |
| Operator console `dist/` | Optional | Rebuild from source |

---

## Operator Console Screens

| Route | Status |
|-------|--------|
| `/` Dashboard | Placeholder |
| `/queue` | Placeholder |
| `/workers` | Placeholder |
| `/scheduler` | Placeholder |
| `/alerts` | Placeholder |
| `/health` | Placeholder (API works) |
| `/settings` | Placeholder |

Screenshot placeholders: *[Dashboard — coming soon]*, *[Queue monitoring — coming soon]*, etc.

---

## SignalR Connection Troubleshooting

If browser console shows `Connection failed (backend may be offline)`:

1. Confirm backend is running on port 5000.
2. Check CORS allows origin `http://localhost:5173`.
3. For production, update CORS policy and `VITE_API_BASE_URL`.

---

## Related Documents

- [09-administrator-guide.md](09-administrator-guide.md) — hospital EDP procedures
- [04-troubleshooting.md](04-troubleshooting.md)
- [08-operations-runbook.md](08-operations-runbook.md)
