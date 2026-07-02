# Taksaka — Operations Documentation

Operational and deployment documentation for the **Taksaka** background processing platform (`src/taksaka.backend` and `src/taksaka.frontend`).

**Implementation snapshot (2026-07-02):** Solution builds on **.NET 8**. The host starts all background services automatically, discovers and loads plugin workers, dispatches jobs end-to-end, and exposes health, job, and worker APIs. Operator console UI is a navigation shell with placeholder pages.

For domain vision and target architecture, see [docs/contexts/taksaka/](../contexts/taksaka/).

---

## Documentation Index

| # | Document | Description |
|---|----------|-------------|
| 00 | [00-getting-started.md](00-getting-started.md) | **Start here** — clone, build, run, create a job |
| 01 | [01-deployment-guide.md](01-deployment-guide.md) | System requirements, build, publish, install |
| 02 | [02-configuration-reference.md](02-configuration-reference.md) | Every configuration key |
| 03 | [03-operator-manual.md](03-operator-manual.md) | Start/stop, logs, monitoring, plugins |
| 04 | [04-troubleshooting.md](04-troubleshooting.md) | Symptoms, causes, resolution |
| 05 | [05-architecture-runtime.md](05-architecture-runtime.md) | Startup, DI, lifecycle, diagrams |
| 06 | [06-plugin-development-guide.md](06-plugin-development-guide.md) | Build workers, packaging, deployment |
| 07 | [07-production-checklist.md](07-production-checklist.md) | Pre-production checkbox list |
| 08 | [08-operations-runbook.md](08-operations-runbook.md) | Daily/weekly procedures, DR, upgrade |
| 09 | [09-administrator-guide.md](09-administrator-guide.md) | Practical guide for hospital EDP (Bahasa Indonesia) |
| — | [reports/production-readiness-report.md](reports/production-readiness-report.md) | V2 production readiness audit results |

---

## Quick Start

### 1. Build

```powershell
cd src\taksaka.backend
dotnet build Taksaka.sln -c Release
dotnet test Taksaka.sln -c Release
```

Plugin DLLs are copied automatically to `Taksaka.Server\bin\Release\net8.0\plugins\`.

### 2. Run the server

```powershell
cd src\taksaka.backend\Taksaka.Server
dotnet run --launch-profile http
```

On startup you should see:

```
Server starting
Initializing Taksaka database schema.
Loading plugins from .../plugins...
Found 3 plugin assemblies
sample-projection loaded (v1.0.0, Projection)
Registered 3 workers
Scheduler started.
Queue polling started (interval=2s)
Health monitor started (interval=60s)
Server ready
```

### 3. Verify health and workers

```powershell
curl http://localhost:5000/api/health
curl http://localhost:5000/api/workers
```

### 4. Create and execute a job

```powershell
curl -X POST http://localhost:5000/api/jobs `
  -H "Content-Type: application/json" `
  -d '{"workerName":"sample-projection","payload":"{}"}'
```

Within a few seconds the dispatcher picks up the job. Check status:

```powershell
curl http://localhost:5000/api/jobs/{job-id}
```

`status: 3` means **Completed**. Execution history is included in the response.

---

## What Works Today

| Feature | Status |
|---------|--------|
| HTTP host + Serilog | Live |
| Automatic database schema bootstrap | Live |
| Plugin discovery, manifest validation, worker registration | Live |
| Scheduler (cron schedules from DB) | Live |
| Queue polling / dispatcher | Live |
| Worker execution with timeout, retry, dead letter | Live |
| Health monitor background service | Live |
| `GET /api/health`, `GET /api/workers`, `POST/GET /api/jobs` | Live |
| SignalR job completion events | Live |
| Operator dashboards | UI placeholders |
| Authentication | Not implemented |

---

## Key Paths

| Path | Purpose |
|------|---------|
| `src/taksaka.backend/Taksaka.sln` | Backend solution |
| `src/taksaka.backend/Taksaka.Server/` | Web host entry point |
| `src/taksaka.backend/Taksaka.Server/appsettings.json` | Default configuration |
| `src/taksaka.backend/Taksaka.Server/bin/Release/net8.0/plugins/` | Deployed plugin DLLs |
| `src/taksaka.frontend/Taksaka.Web/` | Operator console (Vue 3) |
| `docs/contexts/taksaka/` | Domain and architecture vision |

---

## Related Documentation

- [docs/ARTIFACTS.md](../ARTIFACTS.md) — global documentation index
- [docs/contexts/taksaka/taksaka-01-domain.md](../contexts/taksaka/taksaka-01-domain.md) — domain model
- [docs/contexts/taksaka/taksaka-02-architecture.md](../contexts/taksaka/taksaka-02-architecture.md) — architecture vision
