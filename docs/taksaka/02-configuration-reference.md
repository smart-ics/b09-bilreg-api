# Taksaka — Configuration Reference

Every configuration key discovered from binding code, `appsettings.json`, and launch profiles. Keys with **no implementation** are listed as *planned / not bound* based on domain architecture docs.

**Source of truth:** `src/taksaka.backend/Taksaka.Server/appsettings.json` and `*Options.cs` classes.

---

## Summary Table

| Section | Bound in code | Notes |
|---------|---------------|-------|
| `ConnectionStrings` | Legacy | SQL Server fallback when `Database:Provider` unset |
| `Database` | Yes | Provider (`SQLite` / `SqlServer`) + connection string |
| `PluginLoader` | Yes | Plugin directory path (`Taksaka.Hosting.Configuration.PluginLoaderOptions`) |
| `Engine` | Yes | Dispatch poll interval, health monitor interval |
| `Serilog` | Yes | Serilog pipeline |
| `Logging` | Yes | ASP.NET Core logging (secondary to Serilog host) |
| `AllowedHosts` | Yes | ASP.NET Core built-in |
| `Cors` | Yes | Operator console origins |
| Queue | Yes | `Queue:LockLeaseSeconds` in `TAKS_Configuration` (default 300) |
| Retry | Yes | `Retry:BaseDelaySeconds` in `TAKS_Configuration` (default 30) |
| Timeout | Yes | Per-worker `WorkerExecutionPolicy.Timeout` enforced by dispatcher |
| Scheduler | Yes | 15s tick; reads `TAKS_Schedule` |
| Health monitoring | Yes | Background service + `GET /api/health` |
| Alerting | Partial | Alerts written on health warnings/critical |
| Worker discovery | Yes | Full manifest validation + assembly load |

---

## Connection Strings

### `ConnectionStrings:Taksaka` (legacy)

| Property | Value |
|----------|-------|
| **Key** | `ConnectionStrings:Taksaka` |
| **Type** | `string` |
| **Default** | Not set in default `appsettings.json` |
| **Required** | No |
| **Description** | Legacy SQL Server connection string. Used only when `Database:Provider` is not set — forces `SqlServer` provider. Prefer `Database` section. |
| **Example** | `"Server=sql01;Database=Taksaka;User Id=taksaka;Password=***;Encrypt=True"` |

---

## Database

### `Database:Provider`

| Property | Value |
|----------|-------|
| **Key** | `Database:Provider` |
| **Type** | `string` |
| **Default** | `SQLite` |
| **Required** | No |
| **Description** | Database provider: `SQLite` (default) or `SqlServer`. |
| **Example** | `"SqlServer"` |

### `Database:ConnectionString`

| Property | Value |
|----------|-------|
| **Key** | `Database:ConnectionString` |
| **Type** | `string` |
| **Default** | `Data Source=taksaka.db` |
| **Required** | No |
| **Description** | Connection string for the selected provider. |
| **Example (SQLite)** | `"Data Source=taksaka.db"` |
| **Example (SQL Server)** | `"Server=localhost;Database=Taksaka;Trusted_Connection=True;TrustServerCertificate=True"` |

**Options class:** `Taksaka.Infrastructure.Configuration.DatabaseOptions`  
**Section name constant:** `DatabaseOptions.SectionName` → `"Database"`

### `TAKS_Configuration` keys (runtime, not appsettings)

| Key | Default | Description |
|-----|---------|-------------|
| `Queue:LockLeaseSeconds` | `300` | Dequeue lock lease duration |
| `Retry:BaseDelaySeconds` | `30` | Base delay for exponential retry backoff |

---

## Plugin Loading

### `PluginLoader:PluginsPath`

| Property | Value |
|----------|-------|
| **Key** | `PluginLoader:PluginsPath` |
| **Type** | `string` |
| **Default** | `./plugins` |
| **Required** | No |
| **Description** | Directory scanned for `*.dll` at startup. Resolved with `Path.GetFullPath`. Subdirectories are **not** scanned. Assemblies are logged but **not loaded** into the runtime. |
| **Example** | `"C:\\Apps\\Taksaka\\plugins"` |

**Options class:** `Taksaka.Server.Configuration.PluginLoaderOptions`  
**Section name:** `"PluginLoader"`

---

## Logging — Serilog

### `Serilog` (entire section)

| Property | Value |
|----------|-------|
| **Key** | `Serilog` |
| **Type** | object (Serilog.Settings.Configuration schema) |
| **Default** | Console sink, Information default |
| **Required** | No |
| **Description** | Drives `LoggerConfiguration.ReadFrom.Configuration`. `SerilogConfiguration` also calls `.WriteTo.Console()` explicitly. |
| **Example** | See [01-deployment-guide.md](01-deployment-guide.md) |

### `Serilog:MinimumLevel:Default`

| Property | Value |
|----------|-------|
| **Key** | `Serilog:MinimumLevel:Default` |
| **Type** | `string` |
| **Default** | `Information` |
| **Required** | No |
| **Description** | Minimum log level for application code. |
| **Example** | `"Warning"` |

### `Serilog:MinimumLevel:Override:*`

| Property | Value |
|----------|-------|
| **Key** | `Serilog:MinimumLevel:Override:{Source}` |
| **Type** | `string` |
| **Default** | `Microsoft` → `Warning` |
| **Required** | No |
| **Description** | Per-namespace level overrides. |
| **Example** | `"Override": { "Taksaka.Engine": "Debug" }` |

### `Serilog:WriteTo`

| Property | Value |
|----------|-------|
| **Key** | `Serilog:WriteTo` |
| **Type** | array |
| **Default** | `[{ "Name": "Console" }]` |
| **Required** | No |
| **Description** | Output sinks. Requires matching Serilog sink packages. |
| **Example** | File, Seq, Elasticsearch sinks |

### `SerilogOptions.MinimumLevel` (class property)

| Property | Value |
|----------|-------|
| **Key** | N/A — property exists on `SerilogOptions` but is **not read** by `SerilogConfiguration` |
| **Type** | `string` |
| **Default** | `Information` |
| **Required** | No |
| **Description** | Registered via `Configure<SerilogOptions>` but unused at runtime. Use `Serilog:MinimumLevel` in JSON instead. |

---

## Logging — ASP.NET Core

### `Logging:LogLevel:Default`

| Property | Value |
|----------|-------|
| **Key** | `Logging:LogLevel:Default` |
| **Type** | `string` |
| **Default** | `Information` |
| **Required** | No |
| **Description** | Microsoft.Extensions.Logging default (host uses Serilog as primary). |
| **Example** | `"Warning"` |

### `Logging:LogLevel:Microsoft.AspNetCore`

| Property | Value |
|----------|-------|
| **Key** | `Logging:LogLevel:Microsoft.AspNetCore` |
| **Type** | `string` |
| **Default** | `Warning` |
| **Required** | No |
| **Description** | Reduces ASP.NET Core framework noise. |
| **Example** | `"Error"` |

---

## Hosting

### `AllowedHosts`

| Property | Value |
|----------|-------|
| **Key** | `AllowedHosts` |
| **Type** | `string` |
| **Default** | `*` |
| **Required** | No |
| **Description** | ASP.NET Core host filtering. Set to explicit hostnames in production. |
| **Example** | `"taksaka.hospital.local"` |

---

## Environment Variables (ASP.NET Core)

### `ASPNETCORE_ENVIRONMENT`

| Property | Value |
|----------|-------|
| **Key** | `ASPNETCORE_ENVIRONMENT` |
| **Type** | `string` |
| **Default** | `Production` (if unset) |
| **Required** | No |
| **Description** | `Development` enables Swagger/SwaggerUI. |
| **Example** | `Production` |

### `ASPNETCORE_URLS`

| Property | Value |
|----------|-------|
| **Key** | `ASPNETCORE_URLS` |
| **Type** | `string` |
| **Default** | `http://localhost:5000` (launch profile) |
| **Required** | No |
| **Description** | Kestrel bind URLs. |
| **Example** | `http://0.0.0.0:8080` |

---

## Frontend (Operator Console)

### `VITE_API_BASE_URL`

| Property | Value |
|----------|-------|
| **Key** | `VITE_API_BASE_URL` |
| **Type** | `string` |
| **Default** | `http://localhost:5000` |
| **Required** | No |
| **Description** | Base URL for REST and SignalR in `Taksaka.Web`. Set at **build time** for production bundles. |
| **Example** | `https://taksaka-api.hospital.local` |

**Source:** `src/taksaka.frontend/Taksaka.Web/src/shared/api/httpClient.ts`, `signalrClient.ts`

---

## CORS (code-only — not in appsettings)

CORS is configured in code, not via configuration file:

| Setting | Value |
|---------|-------|
| Policy name | `TaksakaCors` |
| Allowed origins | `http://localhost:5173` only |
| Methods | Any |
| Headers | Any |
| Credentials | Allowed |

**Production action required:** Extend `ServerServiceCollectionExtensions` to read origins from configuration (not yet implemented).

---

## Queue — NOT IMPLEMENTED

No configuration section exists. `IQueue` / `QueueManager` are stubs.

| Planned key (recommendation) | Type | Description |
|------------------------------|------|-------------|
| `Queue:MaxDepth` | int | Alert threshold |
| `Queue:PersistenceEnabled` | bool | SQL-backed queue |
| `Queue:PollIntervalMs` | int | Dispatcher poll interval |

**Current behavior:** `EnqueueAsync` and `DequeueAsync` complete immediately without storing jobs.

---

## Retry — NOT IMPLEMENTED

No configuration section. `IRetryManager` / `RetryManager` are stubs.

Per-worker retry limits exist in **code** via `WorkerExecutionPolicy.MaxRetryCount` on each `IWorker.Descriptor.Policy`, not in appsettings.

| Policy property | Type | Default | Description |
|-----------------|------|---------|-------------|
| `MaxRetryCount` | `int?` | `null` = unlimited (sample projection worker) | Max retries before dead letter |
| | | `5` (sample integration worker) | |

**Engine `ScheduleRetryAsync`:** no-op.

---

## Timeout — PARTIAL (worker policy only)

No global timeout configuration. Per-worker:

| Policy property | Type | Example |
|-----------------|------|---------|
| `WorkerExecutionPolicy.Timeout` | `TimeSpan?` | `00:05:00` on `SampleIntegrationWorker`; `null` on `SampleProjectionWorker` |

Dispatcher does not enforce timeouts yet (stub).

---

## Scheduler — NOT IMPLEMENTED

No configuration section. `IScheduler` / `Scheduler` stubs return completed tasks.

| Planned key (recommendation) | Type | Description |
|------------------------------|------|-------------|
| `Scheduler:Enabled` | bool | Master switch |
| `Scheduler:Jobs[]` | array | Cron expressions and worker targets |

---

## Worker Discovery — PARTIAL

Only `PluginLoader:PluginsPath` is configurable. No settings for:

- Assembly load context
- Worker enable/disable flags
- Shadow copying
- Plugin dependency resolution

Workers are **not** registered in DI by the host today.

---

## Health Monitoring — NOT IMPLEMENTED

`IHealthMonitor.EvaluateAsync` returns a static snapshot:

```json
{
  "Dimension": "Platform",
  "State": "Healthy",
  "EvaluatedAt": "<utc-now>"
}
```

No configuration for thresholds, probe intervals, or dimensions.

**HTTP endpoints:**

| Endpoint | Behavior |
|----------|----------|
| `GET /health` | Static `{ "status": "Healthy" }` — does not call `IHealthMonitor` |
| `GET /api/health` | Calls `IHealthMonitor` |

---

## Alerting — NOT IMPLEMENTED

`Taksaka.Core.Entities.Alert` and `AlertSeverity` enum exist. No alert service, configuration, or API.

| Planned key (recommendation) | Type | Description |
|------------------------------|------|-------------|
| `Alerting:Enabled` | bool | Master switch |
| `Alerting:Channels` | array | Email, webhook, etc. |

---

## Worker Execution Policy (code defaults)

Declared on each `IWorker` implementation, not in appsettings:

| Property | Type | Default (class) | Description |
|----------|------|-----------------|-------------|
| `MaxConcurrency` | `int` | `1` | Max parallel jobs per worker type |
| `MaxRetryCount` | `int?` | `null` | Retry limit |
| `Timeout` | `TimeSpan?` | `null` | Execution timeout |
| `CircuitBreakerEnabled` | `bool` | `false` | Circuit breaker (not enforced) |

---

## Job Model (runtime — not configuration)

| Enum / field | Values |
|--------------|--------|
| `JobPriority` | `Critical`, `High`, `Normal`, `Low`, `Background` |
| `JobStatus` | `Created`, `Queued`, `Running`, `Completed`, `Failed`, `RetryWaiting`, `DeadLetter` |

---

## Configuration Precedence

```
appsettings.json
  → appsettings.{Environment}.json
    → User secrets (Development only)
      → Environment variables (double underscore nesting)
        → Command-line arguments
```

Connection string resolution:

```
ConnectionStrings:Taksaka
  → Database:ConnectionString
    → Hard-coded localhost fallback
```

---

## Related Documents

- [01-deployment-guide.md](01-deployment-guide.md)
- [05-architecture-runtime.md](05-architecture-runtime.md)
