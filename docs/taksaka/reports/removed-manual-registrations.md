# Removed Manual Worker Registrations

## Deleted Registrations

| Location | Removed Code |
|----------|--------------|
| `Taksaka.Workers.Maintenance/DependencyInjection/MaintenanceWorkerServiceCollectionExtensions.cs` | Entire file deleted |
| Same file | `services.AddSingleton<AntrianConsistencyRepairWorker>()` |
| Same file | `public static IServiceCollection AddTaksakaMaintenanceWorkers(...)` |
| `Taksaka.Server/DependencyInjection/ServerServiceCollectionExtensions.cs` | `using Taksaka.Workers.Maintenance.DependencyInjection` |
| `Taksaka.Server/DependencyInjection/ServerServiceCollectionExtensions.cs` | `services.AddTaksakaMaintenanceWorkers()` |
| `Taksaka.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `services.AddSingleton<IAntrianConsistencyRepairRepository, AntrianConsistencyRepairRepository>()` |
| `Taksaka.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` | `services.AddOptions<AntrianConsistencyRepairOptions>().Bind(...)` |

## Removed Platform Worker Configuration

| Location | Removed |
|----------|---------|
| `Taksaka.Server/appsettings.json` | `Workers:AntrianConsistencyRepair` section |

## Project Reference Changes

| Location | Change |
|----------|--------|
| `Taksaka.Server.csproj` | `Taksaka.Workers.Maintenance` changed to `ReferenceOutputAssembly=false` (DLL copy only) |

## Replacement

Worker registration now occurs exclusively via `Taksaka.Hosting` automatic plugin discovery (`WorkerPluginDiscoverer` + `WorkerPluginCatalog`).
