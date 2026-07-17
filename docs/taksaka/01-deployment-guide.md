# Taksaka — Deployment Guide

**Based on:** `src/taksaka.backend/` and `src/taksaka.frontend/` as implemented in this repository.  
**Last reviewed:** 2026-07-01

---

## Overview

Taksaka is deployed as an ASP.NET Core 8 web host (`Taksaka.Server`) with optional Vue 3 operator console (`Taksaka.Web`). Worker plugins are shipped as separate DLL files in a `plugins/` folder adjacent to the server executable.

> **Implementation status:** The solution builds and starts successfully with **SQLite by default** (`taksaka.db`). Persistence uses **Dapper** with explicit SQL repositories. Engine queue, retry, dead letter, and scheduler are **repository-backed**. Dispatcher and worker execution remain stubs.

---

## System Requirements

| Component | Requirement |
|-----------|-------------|
| Operating system | Windows Server 2019+ or Windows 10/11 (development); Linux is supported by .NET but not validated in this repo |
| CPU | 2+ cores recommended |
| Memory | 2 GB minimum for server; add capacity per worker load when engine is fully implemented |
| Disk | 500 MB for application + logs; SQLite file (`taksaka.db`) or SQL Server data files |
| Network | Inbound HTTP(S) for API, SignalR, and operator console; SQL Server network access only when `Database:Provider` is `SqlServer` |

---

## .NET Version

All backend projects target **`net8.0`**.

```xml
<TargetFramework>net8.0</TargetFramework>
```

Source: `Taksaka.Server/Taksaka.Server.csproj` and sibling projects.

---

## Required SDKs

| SDK / Tool | Version (from project) | Purpose |
|------------|------------------------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | **8.0** | Build and publish `Taksaka.Server` |
| Node.js | 18+ (LTS recommended) | Build operator console (`Taksaka.Web`) |
| SQL Server | 2019+ or Azure SQL | Optional production persistence (`Database:Provider` = `SqlServer`) |
| SQLite | Built-in (.NET) | Default persistence — no install required |

No `global.json` pins SDK version in this repository. Install .NET 8 SDK matching the target framework.

---

## Required Runtime

| Runtime | Notes |
|---------|-------|
| **ASP.NET Core Runtime 8.0** | Required on deployment server if not using self-contained publish |
| **.NET Runtime 8.0** | Required for `dotnet Taksaka.Server.dll` hosting model |

Worker plugin DLLs must target **`net8.0`** to be compatible with the host.

---

## Database Requirements

| Item | Status |
|------|--------|
| Default provider | **SQLite** via `Microsoft.Data.Sqlite` — `Data Source=taksaka.db` |
| Production provider | **SQL Server** via `Microsoft.Data.SqlClient` when `Database:Provider` = `SqlServer` |
| ORM | **None** — Dapper + explicit SQL in `Taksaka.Infrastructure/Persistence/Queries/` |
| Schema initialization | **Automatic** — `DatabaseInitializerHostedService` on startup (idempotent) |
| Tables | `TAKS_Job`, `TAKS_ExecutionHistory`, `TAKS_Schedule`, `TAKS_Alert`, `TAKS_Configuration` |

Default `appsettings.json`:

```json
"Database": {
  "Provider": "SQLite",
  "ConnectionString": "Data Source=taksaka.db"
}
```

**SQL Server production example:**

```json
"Database": {
  "Provider": "SqlServer",
  "ConnectionString": "Server=PROD-SQL;Database=Taksaka;User Id=taksaka_svc;Password=***;Encrypt=True"
}
```

**Backward compatibility:** If `Database:Provider` is unset and `ConnectionStrings:Taksaka` is present, SQL Server is selected automatically.

**Development:** `git clone` + `dotnet run` from `Taksaka.Server` — no SQL Server install required.

**Production (SQL Server):** Grant the service account `db_datareader`, `db_datawriter`, and `ddladmin` (schema is created on first run).

---

## Required Services

| Service | Required | Notes |
|---------|----------|-------|
| SQL Server | Optional | When `Database:Provider` = `SqlServer` |
| SQLite | Default | File `taksaka.db` created in working directory |
| Taksaka.Server (Kestrel) | Yes | Single process hosts API + SignalR + hosted services |
| Taksaka.Web (static/Vite dev) | Optional | Operator UI shell; pages are placeholders |
| Redis / RabbitMQ | No | Not used |
| Windows Service wrapper | No | Not included; use IIS, NSSM, or `sc create` externally |

---

## Environment Variables

Standard ASP.NET Core variables apply:

| Variable | Required | Default (dev) | Description |
|----------|----------|---------------|-------------|
| `ASPNETCORE_ENVIRONMENT` | No | `Development` (launch profile) | Controls Swagger, logging verbosity |
| `ASPNETCORE_URLS` | No | `http://localhost:5000` (launch profile) | Bind addresses |
| `ConnectionStrings__Taksaka` | No* | See appsettings | Overrides SQL connection (*required in production) |
| `Database__ConnectionString` | No | — | Alternate connection override |
| `PluginLoader__PluginsPath` | No | `./plugins` | Plugin directory path |
| `VITE_API_BASE_URL` | No | `http://localhost:5000` | Frontend API base (build-time for production bundle) |

No custom Taksaka-specific environment variables beyond configuration key overrides.

---

## Configuration Files

| File | Location | Purpose |
|------|----------|---------|
| `appsettings.json` | `Taksaka.Server/` | Base configuration |
| `appsettings.Development.json` | `Taksaka.Server/` | Development overrides (logging only) |
| `appsettings.{Environment}.json` | Deploy alongside binary | Environment-specific overrides |
| `launchSettings.json` | `Properties/` | Development launch only (not deployed) |

Configuration is loaded by `WebApplication.CreateBuilder(args)` — standard ASP.NET Core layering (JSON → environment variables → command line).

---

## appsettings Structure

Current `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Taksaka": "Server=localhost;Database=Taksaka;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Database": {
    "ConnectionString": "Server=localhost;Database=Taksaka;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "PluginLoader": {
    "PluginsPath": "./plugins"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Sections **not present** in current implementation (no code binds them):

- Queue / Retry / Timeout / Scheduler / Health / Alerting engine options

See [02-configuration-reference.md](02-configuration-reference.md) for the full settings catalog.

---

## Connection Strings

Use either form (both are read):

```json
"ConnectionStrings": {
  "Taksaka": "Server=PROD-SQL;Database=Taksaka;User Id=taksaka_svc;Password=***;TrustServerCertificate=False;Encrypt=True"
}
```

or

```json
"Database": {
  "ConnectionString": "Server=PROD-SQL;Database=Taksaka;..."
}
```

`ConnectionStrings:Taksaka` takes precedence when both are set.

---

## Logging Configuration

Logging uses **Serilog** via `SerilogConfiguration.ConfigureSerilog`:

- Reads from `IConfiguration` (`Serilog` section)
- Enriches from log context
- Always writes to **Console** (additional sink configured in `Serilog:WriteTo`)

ASP.NET Core request logging is enabled: `app.UseSerilogRequestLogging()`.

**Production recommendation:** Add a rolling file or centralized sink (Seq, Elasticsearch) under `Serilog:WriteTo`. Example:

```json
"Serilog": {
  "WriteTo": [
    { "Name": "Console" },
    {
      "Name": "File",
      "Args": {
        "path": "logs/taksaka-.log",
        "rollingInterval": "Day",
        "retainedFileCountLimit": 30
      }
    }
  ]
}
```

Add the corresponding Serilog sink NuGet package before using non-Console sinks.

---

## Plugin Folder Structure

On build, `Taksaka.Server.csproj` copies worker DLLs into the output `plugins/` folder:

```
Taksaka.Server/
  bin/Release/net8.0/
    Taksaka.Server.dll
    Taksaka.Server.exe
    appsettings.json
    plugins/
      Taksaka.Workers.Projection.dll
      Taksaka.Workers.Integration.dll
```

Configuration key: `PluginLoader:PluginsPath` (default `./plugins`, resolved to full path at startup).

`PluginLoaderHostedService` behavior:

- Creates the directory if missing
- Lists `*.dll` files in the **top directory only** (no subfolders)
- Logs discovered assemblies
- Does **not** load assemblies or register `IWorker` implementations in DI

---

## Build Process

### Backend

```powershell
cd D:\Project.Aktif\MyHospitalWeb\b09-bilreg-api\src\taksaka.backend
dotnet restore Taksaka.sln
dotnet build Taksaka.sln -c Release
dotnet test Taksaka.sln -c Release
```

Solution projects:

| Project | Role |
|---------|------|
| `Taksaka.Core` | Domain entities, enums, policies |
| `Taksaka.Abstractions` | Engine and worker contracts |
| `Taksaka.Engine` | Runtime services (stubs) |
| `Taksaka.Infrastructure` | EF Core, Serilog, SignalR constants |
| `Taksaka.Server` | Web host |
| `Taksaka.Workers.*` | Sample worker plugins |
| `Taksaka.*.Tests` | Unit tests |

### Frontend (optional)

```powershell
cd D:\Project.Aktif\MyHospitalWeb\b09-bilreg-api\src\taksaka.frontend\Taksaka.Web
npm ci
npm run build
```

Output: `dist/` static files. Serve via IIS, nginx, or embed behind reverse proxy.

---

## Publish Process

### Framework-dependent (recommended for Windows Server with .NET 8 runtime)

```powershell
dotnet publish src\taksaka.backend\Taksaka.Server\Taksaka.Server.csproj `
  -c Release `
  -o C:\Deploy\Taksaka `
  --no-self-contained
```

Verify `plugins/` folder exists in publish output with worker DLLs.

### Self-contained (no shared runtime)

```powershell
dotnet publish src\taksaka.backend\Taksaka.Server\Taksaka.Server.csproj `
  -c Release `
  -o C:\Deploy\Taksaka `
  -r win-x64 `
  --self-contained true
```

No Dockerfile or container manifest exists in this repository.

---

## Installation Steps

### 1. Prepare server

1. Install .NET 8 ASP.NET Core Runtime (or SDK for framework-dependent `dotnet` hosting).
2. Install SQL Server (or provision Azure SQL).
3. Create service account (e.g. `DOMAIN\taksaka_svc`).
4. Create folder `C:\Apps\Taksaka` with subfolders `plugins`, `logs`.

### 2. Deploy binaries

1. Publish `Taksaka.Server` to `C:\Apps\Taksaka`.
2. Copy production `appsettings.Production.json` with connection string and paths.
3. Confirm `plugins\*.dll` are present.
4. Set folder ACLs: service account **Read & Execute** on app; **Modify** on `logs`.

### 3. Configure as Windows Service (example with NSSM)

```powershell
nssm install Taksaka "C:\Program Files\dotnet\dotnet.exe" "C:\Apps\Taksaka\Taksaka.Server.dll"
nssm set Taksaka AppDirectory C:\Apps\Taksaka
nssm set Taksaka AppEnvironmentExtra ASPNETCORE_ENVIRONMENT=Production
nssm set Taksaka AppStdout C:\Apps\Taksaka\logs\stdout.log
nssm set Taksaka AppStderr C:\Apps\Taksaka\logs\stderr.log
nssm start Taksaka
```

IIS reverse proxy is an alternative; no `web.config` is generated by default publish.

### 4. Deploy operator console (optional)

1. Build `Taksaka.Web` with `VITE_API_BASE_URL=https://taksaka.hospital.local`.
2. Copy `dist/` to IIS site or static file host.
3. Ensure CORS on server allows the console origin (currently hard-coded to `http://localhost:5173` — **must be updated for production**).

### 5. Firewall

Allow inbound TCP on the Kestrel port (default **5000** in development; configure `ASPNETCORE_URLS` in production).

---

## First Startup

1. Start the service or run:

   ```powershell
   cd C:\Apps\Taksaka
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   dotnet Taksaka.Server.dll
   ```

2. Expected log lines:

   - Serilog host starting
   - `Taksaka Engine hosted service started.`
   - `Discovered plugin assembly: ...` (one per DLL in `plugins/`)
   - `Now listening on: http://localhost:5000`

3. Verify endpoints:

   ```powershell
   curl http://localhost:5000/health
   curl http://localhost:5000/api/health
   ```

   Expected: HTTP 200 with JSON status.

4. Development only — open Swagger: `http://localhost:5000/swagger`

5. Operator console (dev):

   ```powershell
   cd src\taksaka.frontend\Taksaka.Web
   npm run dev
   ```

   Open `http://localhost:5173` — SignalR connects to `/hubs/operations`.

---

## Common Deployment Mistakes

| Mistake | Symptom | Fix |
|---------|---------|-----|
| Missing `plugins/` folder | Warning log; empty plugin discovery | Ensure publish output includes `plugins/*.dll` |
| Wrong working directory | Plugins not found (`./plugins` resolves incorrectly) | Set service `AppDirectory` to install root |
| CORS mismatch | Browser console fails API/SignalR from production UI | Update `TaksakaCors` policy origins in `ServerServiceCollectionExtensions` |
| Expecting job execution | Jobs never run | Engine stubs — feature not implemented yet |
| Expecting DB tables | EF errors when persistence is added without migration | Run migrations when available |
| Duplicate connection keys with different values | Unpredictable DB target | Use only `ConnectionStrings:Taksaka` in production |
| `TrustServerCertificate=True` in production | Security risk | Use proper TLS and certificate validation |
| Plugin in subfolder | Plugin not discovered | Place DLLs directly in `plugins/` root (current code limitation) |

---

## Production Recommendations

1. **Secrets:** Store connection strings in Windows DPAPI, Azure Key Vault, or environment variables — not plain text in `appsettings.json`.
2. **HTTPS:** Terminate TLS at reverse proxy or configure Kestrel certificates; do not expose plain HTTP externally.
3. **Authentication:** Not implemented — place Taksaka behind VPN, internal network, or add auth before exposing operator console.
4. **CORS:** Replace hard-coded `localhost:5173` with production console URL(s).
5. **Logging:** Add durable file or centralized logging sink; retain 30+ days.
6. **Health checks:** Use `GET /health` for load balancer probes; extend when `IHealthMonitor` evaluates real dimensions.
7. **Monitoring:** Alert on process down, HTTP 5xx, and (when implemented) queue depth and dead-letter growth.
8. **Plugins:** Version and test worker DLLs independently; deploy during maintenance window until hot-reload is implemented.
9. **Database:** When migrations ship, automate `dotnet ef database update` in deployment pipeline.
10. **Resource limits:** Set Windows service recovery actions (restart on failure).

---

## Related Documents

- [02-configuration-reference.md](02-configuration-reference.md)
- [09-administrator-guide.md](09-administrator-guide.md)
- [docs/contexts/taksaka/taksaka-02-architecture.md](../contexts/taksaka/taksaka-02-architecture.md) — target architecture vision
