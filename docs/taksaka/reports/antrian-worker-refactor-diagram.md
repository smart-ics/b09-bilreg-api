# Antrian Worker Refactor — Dependency Diagram

## Before

```mermaid
flowchart LR
    subgraph before [Before]
        S1[Taksaka.Server]
        I1[Taksaka.Infrastructure]
        A1[Taksaka.Abstractions]
        M1[Taksaka.Workers.Maintenance]

        S1 -->|ProjectRef + AddTaksakaMaintenanceWorkers| M1
        S1 --> I1
        M1 --> A1
        I1 -->|AntrianConsistency repo| A1
    end
```

## After

```mermaid
flowchart LR
    subgraph after [After]
        S2[Taksaka.Server]
        H2[Taksaka.Hosting]
        I2[Taksaka.Infrastructure]
        A2[Taksaka.Abstractions]
        P2[Taksaka.Workers.Maintenance plugin]

        S2 -->|entry point| H2
        S2 -->|CopyPlugins DLL only| P2
        H2 -->|loads + registers| P2
        H2 --> A2
        I2 --> A2
        P2 -->|IWorker only| A2
        P2 -->|owns repo SQL DTO config| P2
    end
```

## Summary

- Platform projects no longer reference worker-specific types.
- `AntrianConsistencyRepairWorker` is self-contained with internal repository, SQL, models, configuration, and composition root.
- Host discovers and registers the plugin via manifest + assembly reflection.
