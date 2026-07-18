# Abstractions Migration Report

## Removed Types

| Type | Former Location | Reason |
|------|-----------------|--------|
| `AntrianConsistencyItem` | `Taksaka.Abstractions/Maintenance/AntrianConsistencyItem.cs` | Worker-specific DTO |
| `AntrianConsistencyRepairOptions` | `Taksaka.Abstractions/Configuration/AntrianConsistencyRepairOptions.cs` | Worker-specific options |
| `IAntrianConsistencyRepairRepository` | `Taksaka.Abstractions/Persistence/IAntrianConsistencyRepairRepository.cs` | Worker-specific repository contract |

These types were moved into `Taksaka.Workers.Maintenance` (plugin-owned).

## Remaining Platform Contracts

### Engine interfaces (8)

- `IDispatcher`
- `IScheduler`
- `IQueue`
- `IRetryManager`
- `IDeadLetterManager`
- `IResourceManager`
- `IHealthMonitor`
- `IEventPublisher`

### Worker execution (3)

- `IWorker`
- `WorkerResult`
- `ResourceDecision`

### Platform persistence (8)

- `IJobRepository`
- `IQueueRepository`
- `IScheduleRepository`
- `IExecutionHistoryRepository`
- `IAlertRepository`
- `IConfigurationRepository`
- `IDatabaseInitializer`
- `IDbConnectionFactory`

**Total:** 19 contract files in `Taksaka.Abstractions`.

## Architectural Issues Found

1. **Documented but missing abstractions:** `IJobContext` and `IPlugin` are listed in `taksaka-02-architecture.md` but not implemented. No new abstractions were added per Phase 1 scope.
2. **Missing `Taksaka.Persistence` project:** Platform job/queue persistence still lives in `Taksaka.Infrastructure`.
3. **Worker discovery location:** Architecture doc lists "Worker Discovery" under Engine; implementation correctly lives in `Taksaka.Hosting` per platform decision.
4. **Dispatcher stub:** Engine does not yet resolve workers from `WorkerPluginCatalog` at execution time.
