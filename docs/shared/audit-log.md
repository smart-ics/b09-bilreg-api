# audit-log.md — Audit Log Developer Guide

This document explains how to record **operational audit entries** from use-cases and handlers. Audit logging here is **explicit infrastructure**: you create a row, then persist it. Nothing is intercepted or injected automatically.

---

## What this is (and is not)

| Concept | Role |
|--------|------|
| **`AuditLog`** | Append-only row in `BILRG_AuditLog` — who did what, to which entity, when, with optional snapshot and trace metadata. |
| **`AuditTrailType` / `AuditInfoType`** | Embedded lifecycle on aggregates (`Created` / `Modified` / `Voided`). Stays on the entity; not replaced by `AuditLog`. |
| **`IAuditRepo`** | Persists a single `AuditLog` via `SaveChanges(audit)`. Insert-only. |
| **`AuditLogSnapshotJson`** | Optional helper to serialize a **full entity snapshot** before update/delete. |

**Use `AuditLog` when** you need a searchable, cross-cutting audit history (compliance, investigations, support).

**Do not expect** middleware, DbContext hooks, repository decorators, or domain events to write audit rows for you.

---

## Prerequisites

1. **Database** — Table `BILRG_AuditLog` must exist. Deploy:

   `Bilreg.SqlDb/Shared/AuditLogFeature/BILRG_AuditLog.sql`

2. **Dependency injection** — Register `IAuditRepo` in the handler/use-case constructor. It is resolved via existing Scrutor `ISaveChange<>` scanning (no extra API registration).

---

## Standard workflow

Every audited operation should follow this pattern in the use-case/handler:

```text
1. Capture state before change (if UPDATE / DELETE / VOID)
2. Execute domain behaviour + persist business data
3. AuditLog.Create(...)
4. _auditRepo.SaveChanges(audit)
```

Both steps 3 and 4 must be **visible in code** — that is intentional.

```csharp
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.AuditLogFeature;

public class MyUpdateHandler
{
    private readonly IMyEntityRepo _entityRepo;
    private readonly IAuditRepo _auditRepo;

    public MyUpdateHandler(IMyEntityRepo entityRepo, IAuditRepo auditRepo)
    {
        _entityRepo = entityRepo;
        _auditRepo = auditRepo;
    }

    public void Handle(MyUpdateCmd cmd)
    {
        var entity = _entityRepo.LoadEntity(MyEntityModel.Key(cmd.EntityId))
            .ValueOrThrow();

        var snapshotJson = AuditLogSnapshotJson.Serialize(entity);

        entity.UpdateSomething(cmd);
        entity.Modif(cmd.UserId, cmd.Timestamp);
        _entityRepo.SaveChanges(entity);

        var audit = AuditLog.Create(
            entity.AuditTrail.Modified,
            actionType: "UPDATE",
            entityName: nameof(MyEntityModel),
            entityId: entity.EntityId,
            reason: cmd.Reason,
            originalDataJson: snapshotJson,
            correlationId: cmd.CorrelationId);

        _auditRepo.SaveChanges(audit);
    }
}
```

Persist the **business aggregate first**, then the audit row, unless your transaction strategy wraps both (see [Transactions](#transactions)).

---

## `AuditLog.Create` overloads

Choose the overload that matches what you already have at the call site.

### A — Primitive (`userId` + `UtcNow`)

Use when there is **no** `AuditTrailType` on the entity (external events, integration callbacks, admin scripts).

```csharp
var audit = AuditLog.Create(
    userId: cmd.UserId,
    actionType: "IMPORT",
    entityName: "LabOrder",
    entityId: orderId,
    correlationId: cmd.CorrelationId);
```

- `EventTime` = `DateTime.UtcNow` at creation.

### B — `AuditInfoType` (recommended after domain `Modif` / `Batal`)

Use when the aggregate trail already has the actor and timestamp for this event.

```csharp
entity.Modif(cmd.UserId, cmd.Timestamp);

var audit = AuditLog.Create(
    entity.AuditTrail.Modified,
    actionType: "UPDATE",
    entityName: nameof(MyEntityModel),
    entityId: entity.EntityId,
    originalDataJson: snapshotJson);
```

- `UserId` and `EventTime` come from the selected `AuditInfoType`.
- **`AuditInfoType.Default` is rejected** (empty user or sentinel date `3000-01-01`). Call `Modif` / `Batal` / `Create` on the aggregate **before** building the audit log.

### C — `AuditTrailType` + `AuditLogEventSource`

Use when passing the whole trail reads clearer than picking a slot.

```csharp
var audit = AuditLog.Create(
    entity.AuditTrail,
    AuditLogEventSource.Voided,
    actionType: "VOID",
    entityName: nameof(MyEntityModel),
    entityId: entity.EntityId,
    reason: cmd.VoidReason,
    originalDataJson: snapshotJson);
```

| `AuditLogEventSource` | Slot used |
|----------------------|-----------|
| `Created` | `auditTrail.Created` |
| `Modified` | `auditTrail.Modified` |
| `Voided` | `auditTrail.Voided` |

`actionType` is **always explicit** (e.g. `"VOID"`). It is **not** inferred from `eventSource`.

---

## Field reference

| Property | Required | Notes |
|----------|----------|--------|
| `AuditId` | auto | `NunaId.New("AUD")` — do not set manually. |
| `EventTime` | auto | From `AuditInfoType` (B/C) or `UtcNow` (A). |
| `UserId` | yes | Operator performing the action. |
| `ActionType` | yes | Stable verb, e.g. `CREATE`, `UPDATE`, `VOID`, `DELETE`, `APPROVE`. Max 100 chars in DB. |
| `EntityName` | yes | Prefer `nameof(MyEntityModel)` for consistency. Max 100 chars. |
| `EntityId` | yes | Primary key of affected entity. Max 50 chars. |
| `Reason` | optional | Business justification (void reason, rejection note). Max 500 chars. |
| `OriginalDataJson` | optional | Full JSON snapshot **before** change. Use `AuditLogSnapshotJson`. |
| `CorrelationId` | optional | Request/workflow id for tracing related audits. Max 50 chars. |
| `ClientIpAddress` | optional | Pass from API layer; do not read `HttpContext` in Domain. Max 45 chars. |
| `UserAgent` | optional | Same as IP. Max 500 chars. |

---

## JSON snapshots (`OriginalDataJson`)

Serialize the **entire entity (or DTO) as it existed before the mutation**, not a diff.

```csharp
var before = _repo.LoadEntity(key).ValueOrThrow();
var snapshotJson = AuditLogSnapshotJson.Serialize(before);

// ... apply changes and SaveChanges on aggregate ...

var audit = AuditLog.Create(..., originalDataJson: snapshotJson);
```

**Rules:**

- Pass a **concrete type** at the call site (`Serialize(myModel)`), not a polymorphic base without a known shape.
- Options are fixed: camelCase, indented, nulls omitted — do not hand-roll JSON for audit rows.
- For CREATE actions, `originalDataJson` is usually omitted (nothing to snapshot).

---

## Correlation ID

Use one correlation value per HTTP request or long-running workflow so support can find all related audit rows:

```sql
SELECT * FROM BILRG_AuditLog WHERE CorrelationId = @id ORDER BY EventTime;
```

Pass the same `correlationId` on every `AuditLog.Create` inside that request (from command, middleware value, or activity id — wired explicitly in the API/handler).

---

## HTTP metadata (IP, User-Agent)

Optional. Capture in the **API or application layer** and pass as strings into `Create`:

```csharp
var audit = AuditLog.Create(
  auditInfo,
  "UPDATE",
  nameof(MyEntityModel),
  entityId,
  clientIpAddress: cmd.ClientIp,
  userAgent: cmd.UserAgent);
```

Do not reference ASP.NET types from Domain or Infrastructure audit code.

---

## `actionType` conventions

Use short, uppercase, stable tokens agreed within the feature team:

| `actionType` | Typical use |
|--------------|-------------|
| `CREATE` | New entity persisted |
| `UPDATE` | Field or state change |
| `VOID` | Void / batal lifecycle |
| `DELETE` | Hard removal (rare in this codebase) |
| `APPROVE` / `REJECT` | Workflow decisions |

Keep names consistent across handlers for the same entity so operational queries stay simple.

---

## Common scenarios

### Update with snapshot

```csharp
var entity = _repo.LoadEntity(key).ValueOrThrow();
var snapshot = AuditLogSnapshotJson.Serialize(entity);

entity.Apply(cmd);
entity.Modif(cmd.UserId, now);
_repo.SaveChanges(entity);

_auditRepo.SaveChanges(AuditLog.Create(
    entity.AuditTrail.Modified,
    "UPDATE",
    nameof(MyEntityModel),
    entity.EntityId,
    reason: cmd.Reason,
    originalDataJson: snapshot,
    correlationId: cmd.CorrelationId));
```

### Void

```csharp
var entity = _repo.LoadEntity(key).ValueOrThrow();
var snapshot = AuditLogSnapshotJson.Serialize(entity);

entity.Batal(cmd.UserId, now);
_repo.SaveChanges(entity);

_auditRepo.SaveChanges(AuditLog.Create(
    entity.AuditTrail,
    AuditLogEventSource.Voided,
    "VOID",
    nameof(MyEntityModel),
    entity.EntityId,
    reason: cmd.VoidReason,
    originalDataJson: snapshot,
    correlationId: cmd.CorrelationId));
```

### Create (no snapshot)

```csharp
var entity = MyEntityModel.Create(cmd);
_repo.SaveChanges(entity);

_auditRepo.SaveChanges(AuditLog.Create(
    entity.AuditTrail.Created,
    "CREATE",
    nameof(MyEntityModel),
    entity.EntityId,
    correlationId: cmd.CorrelationId));
```

### Event without `AuditTrailType`

```csharp
_auditRepo.SaveChanges(AuditLog.Create(
    cmd.UserId,
    "SYNC",
    "ExternalFeed",
    cmd.FeedItemId,
    reason: cmd.Note,
    correlationId: cmd.CorrelationId));
```

---

## Transactions

- If the use-case already runs inside a **unit of work / transaction scope**, call `_auditRepo.SaveChanges(audit)` in the same scope so business data and audit row commit together.
- If audit insert fails after business commit, you get a business change without audit — handle errors according to your feature’s consistency requirements (retry audit write, compensating action, or shared transaction).

There is **no** automatic enlistment; orchestration is the use-case author’s responsibility.

---

## What not to do

- Do not add global filters, middleware, or repository base classes that write audit rows silently.
- Do not store partial diffs in `OriginalDataJson` unless you have a documented exception — default is **full snapshot**.
- Do not use `AuditLog` to replace `AuditTrailType` on aggregates; both can coexist.
- Do not call `AuditLog.Create` with `AuditTrail.Modified` before calling `Modif` — you will get stale user/time or `Default` validation errors.
- Do not implement delete/update APIs on `IAuditLog` / `IAuditRepo` — the table is append-only.

---

## Project layout (reference)

| Layer | Type | Location |
|-------|------|----------|
| Domain | `AuditLog`, `AuditLogEventSource` | `Bilreg.Domain/Shared/AuditLogFeature/` |
| Application | `IAuditRepo`, `AuditLogSnapshotJson` | `Bilreg.Application/Shared/AuditLogFeature/` |
| Infrastructure | `AuditLogRepo`, `AuditLogDal`, `AuditLogDto` | `Bilreg.Infrastructure/Shared/AuditLogFeature/` |
| SqlDb | `BILRG_AuditLog.sql` | `Bilreg.SqlDb/Shared/AuditLogFeature/` |

---

## Operational queries (examples)

By entity:

```sql
SELECT AuditId, EventTime, UserId, ActionType, Reason
FROM BILRG_AuditLog
WHERE EntityName = 'MyEntityModel' AND EntityId = @entityId
ORDER BY EventTime DESC;
```

By user and date range:

```sql
SELECT *
FROM BILRG_AuditLog
WHERE UserId = @userId
  AND EventTime >= @from AND EventTime < @to
ORDER BY EventTime;
```

By correlation:

```sql
SELECT *
FROM BILRG_AuditLog
WHERE CorrelationId = @correlationId
ORDER BY EventTime;
```

---

## Checklist for new use-cases

- [ ] Inject `IAuditRepo` in constructor.
- [ ] Decide `actionType` and `entityName` / `entityId` values.
- [ ] For updates/voids: snapshot **before** mutation via `AuditLogSnapshotJson.Serialize`.
- [ ] Update aggregate audit trail (`Modif` / `Batal`) before using overload B or C.
- [ ] Call `AuditLog.Create(...)` then `_auditRepo.SaveChanges(audit)` in clear view of the workflow.
- [ ] Pass `correlationId` (and IP/User-Agent if available) from the command/API layer.
- [ ] Confirm `BILRG_AuditLog` exists in target environment.
