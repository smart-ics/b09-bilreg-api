# Architecture Violations Report

## Resolved Violations (Phases 1–5)

| Violation | Former Location | Status |
|-----------|-----------------|--------|
| Worker DTO in Abstractions | `AntrianConsistencyItem` | Resolved — moved to plugin |
| Worker options in Abstractions | `AntrianConsistencyRepairOptions` | Resolved — moved to plugin |
| Worker repository in Abstractions | `IAntrianConsistencyRepairRepository` | Resolved — moved to plugin |
| Worker SQL in Infrastructure | `AntrianConsistencyRepairQueries` | Resolved — deleted from platform |
| Worker repository in Infrastructure | `AntrianConsistencyRepairRepository` | Resolved — deleted from platform |
| Manual worker DI | `AddTaksakaMaintenanceWorkers()` | Resolved — deleted |
| Server compiles worker type | Direct Maintenance project reference | Resolved — `ReferenceOutputAssembly=false` |
| Worker config in platform appsettings | `Workers:AntrianConsistencyRepair` | Resolved — removed |
| Plugin discovery stub | Log-only `PluginLoaderHostedService` in Server | Resolved — full discovery in Hosting |

## Remaining Gaps (Not Auto-Refactored)

| Issue | Description | Severity |
|-------|-------------|----------|
| Dispatcher stub | `IDispatcher.DispatchNextAsync` is a no-op; discovered workers are not executed by Engine | Medium |
| `Taksaka.Persistence` missing | Platform persistence remains in `Taksaka.Infrastructure` instead of dedicated project | Low |
| Missing `IPlugin` / `IJobContext` | Documented in architecture but not implemented in Abstractions | Low |
| Engine doc vs Hosting | Architecture doc lists Worker Discovery under Engine; implemented in Hosting | Documentation |
| Plugin logger injection | Workers use `NullLogger` in composition root unless host provides scope later | Low |

## Enforcement

Automated rules live in `Taksaka.Architecture.Tests` (NetArchTest). Run:

```powershell
dotnet test src\taksaka.backend\Taksaka.Architecture.Tests --filter FullyQualifiedName~Architecture
```

## Success Criterion

> A new Worker can be added by copying its plugin into the `plugins/` directory without modifying any existing platform project.

This is now satisfied for the reference `AntrianConsistencyRepairWorker` and sample workers.
