# Taksaka — Operations Documentation

Operational and deployment documentation for the **Taksaka** background processing platform, derived from the implemented codebase in `src/taksaka.backend` and `src/taksaka.frontend`.

**Implementation snapshot (2026-07-01):** Solution builds on **.NET 8**. Host starts, health endpoints respond, plugins are **discovered** (logged). Engine queue, dispatch, scheduler, retry, dead letter, and worker execution are **stub implementations**. Operator console UI is a **navigation shell** with placeholder pages.

For domain vision and target architecture, see [docs/contexts/taksaka/](../contexts/taksaka/).

---

## Documentation Index

| # | Document | Description |
|---|----------|-------------|
| 01 | [01-deployment-guide.md](01-deployment-guide.md) | System requirements, build, publish, install, production tips |
| 02 | [02-configuration-reference.md](02-configuration-reference.md) | Every configuration key from code and appsettings |
| 03 | [03-operator-manual.md](03-operator-manual.md) | Start/stop, logs, monitoring, plugins (operator view) |
| 04 | [04-troubleshooting.md](04-troubleshooting.md) | Symptoms, causes, investigation, resolution |
| 05 | [05-architecture-runtime.md](05-architecture-runtime.md) | Startup, DI, lifecycle, Mermaid diagrams |
| 06 | [06-plugin-development-guide.md](06-plugin-development-guide.md) | Build workers, example plugin, packaging |
| 07 | [07-production-checklist.md](07-production-checklist.md) | Pre-production checkbox list |
| 08 | [08-operations-runbook.md](08-operations-runbook.md) | Daily/weekly/monthly procedures, DR, upgrade |
| 09 | [09-administrator-guide.md](09-administrator-guide.md) | **Practical guide for hospital EDP** (Bahasa Indonesia) |

---

## Reading Order

### Hospital EDP / Administrator (first deployment)

1. [09-administrator-guide.md](09-administrator-guide.md) — practical steps
2. [01-deployment-guide.md](01-deployment-guide.md) — technical detail
3. [07-production-checklist.md](07-production-checklist.md) — go-live validation
4. [08-operations-runbook.md](08-operations-runbook.md) — ongoing operations
5. [04-troubleshooting.md](04-troubleshooting.md) — when things go wrong

### DevOps / Platform Engineer

1. [01-deployment-guide.md](01-deployment-guide.md)
2. [02-configuration-reference.md](02-configuration-reference.md)
3. [05-architecture-runtime.md](05-architecture-runtime.md)
4. [07-production-checklist.md](07-production-checklist.md)
5. [08-operations-runbook.md](08-operations-runbook.md)

### Developer (new worker plugin)

1. [05-architecture-runtime.md](05-architecture-runtime.md)
2. [06-plugin-development-guide.md](06-plugin-development-guide.md)
3. [02-configuration-reference.md](02-configuration-reference.md)
4. [docs/contexts/taksaka/taksaka-02-architecture.md](../contexts/taksaka/taksaka-02-architecture.md) — vision

### On-call Operator

1. [03-operator-manual.md](03-operator-manual.md)
2. [04-troubleshooting.md](04-troubleshooting.md)
3. [08-operations-runbook.md](08-operations-runbook.md) — incident section

---

## Intended Audience

| Audience | Primary documents |
|----------|-------------------|
| Hospital EDP / IT administrator | 09, 01, 08, 04 |
| DevOps / infrastructure | 01, 02, 07, 08 |
| Platform developer | 05, 06, 02 |
| On-call operator | 03, 04, 08 |
| Architect / tech lead | 05, [taksaka-02-architecture.md](../contexts/taksaka/taksaka-02-architecture.md) |

---

## Quick Start

### Run backend (development)

```powershell
cd src\taksaka.backend\Taksaka.Server
dotnet run --launch-profile http
```

Verify:

```powershell
curl http://localhost:5000/health
curl http://localhost:5000/api/health
```

### Run operator console (development)

```powershell
cd src\taksaka.frontend\Taksaka.Web
npm install
npm run dev
```

Open `http://localhost:5173`

### Build release

```powershell
cd src\taksaka.backend
dotnet build Taksaka.sln -c Release
dotnet test Taksaka.sln -c Release
```

Plugin DLLs appear in `Taksaka.Server\bin\Release\net8.0\plugins\`.

---

## Key Paths

| Path | Purpose |
|------|---------|
| `src/taksaka.backend/Taksaka.sln` | Backend solution |
| `src/taksaka.backend/Taksaka.Server/` | Web host entry point |
| `src/taksaka.backend/Taksaka.Server/appsettings.json` | Default configuration |
| `src/taksaka.frontend/Taksaka.Web/` | Operator console (Vue 3) |
| `docs/contexts/taksaka/` | Domain and architecture vision |

---

## What Works Today vs Planned

| Feature | Status |
|---------|--------|
| HTTP host + Serilog | Live |
| `GET /health`, `GET /api/health` | Live |
| SignalR hub `/hubs/operations` | Live (minimal) |
| Plugin DLL discovery (log only) | Live |
| Swagger (Development) | Live |
| Job queue / dispatch / scheduler | Stub |
| Worker load and execute | Not implemented |
| Database schema / migrations | Not implemented |
| Operator dashboards | UI placeholders |
| Authentication | Not implemented |

---

## Related Repository Documentation

- [docs/ARTIFACTS.md](../ARTIFACTS.md) — global documentation index
- [docs/contexts/taksaka/taksaka-01-domain.md](../contexts/taksaka/taksaka-01-domain.md) — domain model
- [docs/contexts/taksaka/taksaka-02-architecture.md](../contexts/taksaka/taksaka-02-architecture.md) — architecture vision
