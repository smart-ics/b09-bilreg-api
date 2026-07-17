# Plugin Manifest Example — AntrianConsistencyRepairWorker

Manifest file: `Taksaka.Workers.Maintenance.plugin.json` (deployed alongside `Taksaka.Workers.Maintenance.dll` in `plugins/`).

```json
{
  "name": "antrian-consistency-repair",
  "version": "1.0.0",
  "description": "Repairs BILRG_AntrianEntry rows still referencing bookings after registration.",
  "workerType": "Maintenance",
  "configuration": {
    "batchSize": 50,
    "workerEnabled": true,
    "executionTimeout": "00:10:00",
    "connectionString": ""
  }
}
```

## Field Notes

| Field | Value | Purpose |
|-------|-------|---------|
| `name` | `antrian-consistency-repair` | Must match `IWorker.Descriptor.Name` |
| `version` | `1.0.0` | Plugin semver for host logging |
| `workerType` | `Maintenance` | Maps to `WorkerCategory.Maintenance` |
| `description` | Human text | Operator documentation |
| `configuration` | Plugin-owned object | Loaded by plugin composition root; not bound in platform `appsettings.json` |

## Deployment Convention

For flat `plugins/` directories with multiple DLLs, manifest files are named `{AssemblyName}.plugin.json` (e.g. `Taksaka.Workers.Maintenance.plugin.json`).

Schema: [plugin-manifest-schema.json](../plugin-manifest-schema.json)
