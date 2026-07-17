# AntrianConsistencyRepairWorker

Maintenance worker plugin that repairs deferred consistency between Registration and Antrian queue entries.

## Configuration

Configuration is owned by this plugin via `Taksaka.Workers.Maintenance.plugin.json` deployed alongside the DLL in `plugins/`.

| Key | Default | Description |
|-----|---------|-------------|
| `batchSize` | 50 | Max rows per execution |
| `workerEnabled` | true | Kill switch |
| `executionTimeout` | 00:10:00 | Per-run timeout |
| `connectionString` | *(required)* | Hospital SQL Server database |

## Deployment

Copy `Taksaka.Workers.Maintenance.dll` and `Taksaka.Workers.Maintenance.plugin.json` into the host `plugins/` directory.
