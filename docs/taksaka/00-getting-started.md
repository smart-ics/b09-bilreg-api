# Taksaka — Getting Started

This guide walks a new developer through running Taksaka and executing a background job **without reading the source code**.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PowerShell or a terminal

---

## Step 1 — Clone and build

```powershell
git clone <repository-url>
cd b09-bilreg-api/src/taksaka.backend
dotnet build Taksaka.sln -c Release
```

The build copies sample worker plugins into:

```
Taksaka.Server/bin/Release/net8.0/plugins/
```

You should see three plugin pairs (DLL + `.plugin.json`):

| File | Worker name |
|------|-------------|
| `Taksaka.Workers.Projection.dll` | `sample-projection` |
| `Taksaka.Workers.Integration.dll` | `sample-integration` |
| `Taksaka.Workers.Maintenance.dll` | `antrian-consistency-repair` |

---

## Step 2 — Start the server

```powershell
cd Taksaka.Server
dotnet run --launch-profile http
```

The server listens on `http://localhost:5000` by default.

### What happens automatically

No manual initialization is required. On startup the server:

1. Creates the SQLite database (`taksaka.db`) if missing
2. Loads and validates plugins from `./plugins`
3. Registers workers in the runtime catalog
4. Starts the scheduler (cron-based job creation)
5. Starts queue polling (dispatcher)
6. Starts the health monitor

### Expected startup logs

```
Server starting
Initializing Taksaka database schema.
Taksaka database schema initialized.
Loading plugins from .../plugins...
Found 3 plugin assemblies
sample-integration loaded (v1.0.0, Integration)
antrian-consistency-repair loaded (v1.0.0, Maintenance)
sample-projection loaded (v1.0.0, Projection)
Registered 3 workers
Scheduler started.
Queue polling started (interval=2s)
Health monitor started (interval=60s)
Server ready
```

If plugin loading fails, the log explains **what failed, why, which assembly, and suggested fixes**.

---

## Step 3 — Verify the platform is healthy

```powershell
curl http://localhost:5000/api/health
curl http://localhost:5000/api/workers
```

`/api/workers` lists every registered worker with concurrency and retry settings.

---

## Step 4 — Create a job

```powershell
curl -X POST http://localhost:5000/api/jobs `
  -H "Content-Type: application/json" `
  -d '{"workerName":"sample-projection","payload":"{\"hello\":\"world\"}"}'
```

Response (201 Created):

```json
{
  "id": "…",
  "workerName": "sample-projection",
  "status": 1,
  "priority": 2,
  "createdAt": "…"
}
```

`status: 1` = Queued. The dispatcher picks up queued jobs within the configured poll interval (default 2 seconds).

---

## Step 5 — Verify execution

```powershell
curl http://localhost:5000/api/jobs/{job-id}
```

When complete:

```json
{
  "status": 3,
  "history": [
    {
      "outcome": "Sample projection worker completed."
    }
  ]
}
```

`status: 3` = Completed.

---

## Step 6 — Deploy your own worker plugin

See [06-plugin-development-guide.md](06-plugin-development-guide.md) for the full reference. Summary:

1. Create a class library implementing `IWorker` from `Taksaka.Abstractions`
2. Add a `{AssemblyName}.plugin.json` manifest beside the DLL
3. Copy both files into the server's `plugins/` folder
4. Restart the server

The sample projection worker (`Taksaka.Workers.Projection`) is the canonical reference implementation.

---

## Configuration

Default settings are in `Taksaka.Server/appsettings.json`:

| Section | Purpose |
|---------|---------|
| `Database` | SQLite (default) or SQL Server |
| `PluginLoader:PluginsPath` | Plugin directory (default `./plugins`) |
| `Engine:DispatchPollIntervalSeconds` | Queue poll interval (default 2) |
| `Engine:HealthMonitorIntervalSeconds` | Health check interval (default 60) |

See [02-configuration-reference.md](02-configuration-reference.md) for all keys.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `Registered 0 workers` | Empty `plugins/` folder | Build solution or copy DLLs manually |
| Job stays Queued | Worker name mismatch | Check `GET /api/workers` for exact names |
| Job → DeadLetter | Worker not found | Verify manifest `name` matches `Descriptor.Name` |
| Manifest validation error | Invalid JSON or missing fields | See [plugin-manifest-schema.json](plugin-manifest-schema.json) |

Full guide: [04-troubleshooting.md](04-troubleshooting.md)

---

## Next steps

- [05-architecture-runtime.md](05-architecture-runtime.md) — how the runtime works
- [06-plugin-development-guide.md](06-plugin-development-guide.md) — build a production worker
- [07-production-checklist.md](07-production-checklist.md) — go-live checklist
