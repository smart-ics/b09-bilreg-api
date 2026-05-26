# Tarif — Migration Strategy (Import → Publish)

**Artifact role:** OPERATION / INTEGRATION — dual authority transition  
**Audience:** Engineering, Keuangan, support

---

## Goal

Transition operational authority from:

**Legacy import → `BILRG_*` projection**  
to  
**TarifPolicy publish → `BILRG_*` projection**

Without redesigning consumers (`INilaiTarifRepo`), billing snapshots, or the publish engine.

---

## Principles

| Principle | Implementation |
| --------- | -------------- |
| Explicit | Manual baseline, publish, import; no startup auto-sync |
| Reversible | Emergency import + DB backup; mode switch via config/DB |
| Observable | `/api/tarif-migration/status`, `/consistency`, publish log |
| Low risk | Staged M0–M3; import retained as fallback |

---

## Dual authority model

```text
M0:  ta_*  ──import──► BILRG_*  ◄── (publish blocked)
M1:  ta_*  ──import──► BILRG_*  ◄── policy publish (pilot)
M2:  ta_*  ──import──► BILRG_*  ◄── policy publish (primary)
M3:  ta_*  ──emergency import──► BILRG_*  ◄── policy publish (only routine path)
```

**Effective mode** = `BILRG_TarifOperationalState.MigrationMode` when set, else `appsettings` → `Tarif:Mode`.

---

## Baseline policy

One-time optional anchor: `POST /api/tarif-migration/baseline`

- Reads current `BILRG_NilaiTarif*`
- Creates `TarifPolicy` + variants + komponen (`PolicyNo` = `BASELINE-{date}`)
- Publishes in one transaction (stamps `SourcePolicyId`, preserves `NilaiTarifId`)
- Does **not** replace routine import or publish workflows

---

## Import vs publish control

| Control | Scope |
| ------- | ----- |
| `ITarifMigrationGuard` | Mode-based allow/deny |
| `TarifOperationalGate` | Single-instance mutex (import vs publish/baseline) |
| Ops procedure | Multi-instance: no parallel import/publish |

---

## Decommission (legacy import)

Phase 5 does **not** remove `POST /api/NilaiTarif/import`.

| Stage | Import role |
| ----- | ----------- |
| M0–M1 | Primary or parallel |
| M2 | Fallback / DR only (`AllowEmergencyImport`) |
| M3 | Operationally deprecated; endpoint kept |

Future removal is a separate decision after sustained M3 operation.

---

## What we explicitly did not build

- Distributed lock / orchestration framework
- Automatic reconciliation engine
- Runtime policy rule engine
- Consumer contract changes

---

## Related artifacts

| Path | Role |
| ---- | ---- |
| [`tarif-09-rollout-checklist.md`](tarif-09-rollout-checklist.md) | Stage gates |
| [`tarif-05-runbook.md`](tarif-05-runbook.md) | Run procedures |
| [`tarif-04-api-contract.md`](tarif-04-api-contract.md) | API |
| [`TARIF_IMPLEMENTATION_PLAN.md`](TARIF_IMPLEMENTATION_PLAN.md) | Phase ledger |
