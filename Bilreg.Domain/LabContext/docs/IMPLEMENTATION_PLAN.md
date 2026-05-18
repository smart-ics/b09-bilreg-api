# IMPLEMENTATION_PLAN.md — Laboratory Workflow Feature (LWF)

> **Status:** Planning only — no implementation in this document.  
> **Canonical location:** `Bilreg.Domain/LabContext/docs/IMPLEMENTATION_PLAN.md`  
> **API project:** `Bilreg.Api` (briefs may say WebApi).  
> **Scope root:** `{Layer}/LabContext/` in each project.

---

## 1. Feature Overview

**LWF** is an **operational workflow orchestration** feature for hospital laboratory examinations. It is not a full LIS, financial system, analyzer middleware, or EMR projection.

| Owns | Does not own |
|------|----------------|
| Workflow lifecycle (`Ordered` → … → `Released`) | Patient registration authority (REG) |
| `LabOrderModel` + `LabResultDocumentModel` | Billing authority (BIL) — only requests `Tindakan` |
| Billing charge **request**; financial clearance **gate** for external release | Payment, refund, receivable |
| Result verification + administrative release | OWR/LIS as source-of-truth |
| OWR outbound queue (async) | Accessioning, QC, reagent, analyzer routing |

**Primary references (read before any slice):**

| Artifact | Use when |
|----------|----------|
| `Bilreg.Domain/LabContext/docs/DOMAIN.md` | Aggregates, states, boundaries |
| `Bilreg.Domain/LabContext/docs/AGENT.md` | Invariants, forbidden design |
| `docs/ENGINEERING.md` | Layers, repo/query philosophy |
| `docs/DATABASE.md` | Table/column/audit rules |
| `docs/NAMING.md` | Naming |
| `docs/WORKFLOW.md` | Queue/workspace API hints |
| `docs/INSTRUCTION.md` | Global stance |
| `docs/skills/feature-model-generation.md` | Domain models |
| `docs/skills/feature-persistence-generation.md` | DTO/DAL/Repo |
| `docs/skills/use-case-generation.md` | Cmd/Query/Handler |

**Feature folders:**

| Folder | Aggregate |
|--------|-----------|
| `LabOrderFeature` | `LabOrderModel` — workflow, billing orchestration, collection, release eligibility |
| `LabResultFeature` | `LabResultDocumentModel` — versions, components, verification |

---

## 2. Existing Architecture Analysis

```
Bilreg.Api            → HTTP, IMediator, JSendOk
Bilreg.Application    → Handlers, I*Repo, Integration interfaces
Bilreg.Domain         → Models, enums, behaviour
Bilreg.Infrastructure → Dto, Dal (Dapper), Repo, Integration implementations
Bilreg.SqlDb          → DDL per Context/Feature
Bilreg.Test           → Dal/Repo tests (TransHelper)
```

**Dependency flow:** `Api → Application → Domain`; `Infrastructure → Application → Domain`.

**DI:** MediatR scans `Bilreg.Application`. DAL/Repo auto-registered via Scrutor (`InfrastructureService.cs`). Integration services registered in `InfrastructureService.cs` or `DomainService.cs` as scoped implementations of Application interfaces.

**LabContext today:** Docs + this plan only — greenfield inside existing conventions.

### Approved reference features

| Pattern | Reference | Notes |
|---------|-----------|-------|
| Workflow aggregate + transitions | `IgdContext/IgdVisitFeature` (`IgdVisitModel`, handlers) | **Primary** — AI-generated, reviewed, approved |
| Handler orchestration + transaction | `IgdVisitDaftarCmd`, `IgdVisitAssignRegisterCmd` | Explicit, small scope |
| Child-table persistence | `BedUsageContext/KamarOperasiFeature` (`OpCaseRepo` delete+insert detail) | Detail DAL only |
| API surface | `Api/Controllers/.../ScheduleOpController.cs` | Thin MediatR |

### Do NOT use as reference

| File | Reason |
|------|--------|
| `RegJalanWalkInCommand.cs` | Messy, needs refactor, not approved for orchestration patterns |

Agents may design **cleaner** orchestration than legacy Reg code, as long as it stays pragmatic, explicit, readable, debuggable, low-magic.

---

## 3. Existing Engineering Pattern Discovery

| Concern | Convention |
|---------|------------|
| Domain type | `{Name}Model`; value objects: `{Name}Type` |
| Key | `I{Name}Key` |
| Command / Query | `{Feature}{Action}Cmd`, `{Feature}{Action}Query` |
| Handler | `{Feature}{Action}Handler` |
| Repo | `Application/.../I{Name}Repo` → `Infrastructure/.../{Name}Repo` |
| DAL | Dapper, Nuna `IInsert`/`IUpdate`/`IGetData`/`IListData` |
| Validation | `Guard` in handler; invariants in model |
| API | `JSendOk`; `POST` / `PATCH` / `GET` |
| Transaction | `TransHelper.NewScope()` in handler when needed |
| **LWF tables** | Prefix **`BILRG_`** (all LWF tables) |

**Model naming:** Use `{Name}Model` per NAMING.md (not skill doc `{Name}Type` for aggregates).

---

## 4. Recommended Folder Structure

```
Bilreg.Domain/LabContext/
  LabOrderFeature/
    ILabOrderKey.cs
    LabOrderModel.cs
    LabOrderItemModel.cs
    LabOrderStatusEnum.cs
    LabOrderSourceEnum.cs
    FinancialClearanceEnum.cs
    OwareStatusEnum.cs
    PatientSnapshotType.cs          # 6 fields — no MRNumber
    CollectionInfoType.cs
    DeferredInfoType.cs
    SpecimenRequirementType.cs
    VacutainerTypeEnum.cs           # EDTA, Serum, Citrate, Heparin
  LabResultFeature/
    ILabResultDocumentKey.cs
    LabResultDocumentModel.cs
    LabResultVersionModel.cs
    LabResultComponentModel.cs
    ...

Bilreg.Application/LabContext/
  LabOrderFeature/
    ILabOrderRepo.cs
    ILabOrderWorklistDal.cs
    Integration/
      ILabRegIntegration.cs
      ILabBillingIntegration.cs
    UseCases/...
  LabResultFeature/
    ILabResultDocumentRepo.cs
    UseCases/...

Bilreg.Infrastructure/LabContext/
  LabOrderFeature/          # Dto, Dal, Repo
  LabResultFeature/
  Integration/
    LabRegIntegration.cs          # V1: wraps IRegRepo when ready; placeholder fake RegId
    LabBillingIntegration.cs      # V1: placeholder fake TindakanId
    LabOwareQueueDal.cs

Bilreg.Api/Controllers/LabContext/
  LabOrderFeature/LabOrderController.cs
  LabResultFeature/LabResultController.cs

Bilreg.SqlDb/LabContext/
  LabOrderFeature/BILRG_LabOrder.sql
  LabOrderFeature/BILRG_LabOrderItem.sql
  LabOrderFeature/BILRG_LabOwareOutboundQueue.sql
  LabResultFeature/BILRG_LabResultDocument.sql
  LabResultFeature/BILRG_LabResultVersion.sql
  LabResultFeature/BILRG_LabResultComponent.sql

Bilreg.Test/LabContext/...
```

**V1 explicitly excluded:** `BILRG_LabOrderStateHist` (no state history table unless audit requirement appears later).

---

## 5. Aggregate Implementation Plan

### 5.1 Patient snapshot (corrected)

`PatientId` **is** the MR number. Do **not** add `MRNumber` anywhere (model, SQL, DTO, views, API).

**Required snapshot fields:**

```text
RegId
PatientId
PatientName
BirthDate
Gender
AgeAtOrder
```

Persist on `BILRG_LabOrder`. External patient may start with `RegId` / `PatientId` empty until REG attaches registration.

### 5.2 `LabOrderModel`

**Identity:** `OrderId` `VARCHAR(12)` (app-generated).

**Header fields (conceptual):** `OrderNo`, `OrderSource`, `LabOrderStatus`, `FinancialClearance`, `OwareStatus`, patient snapshot, `DeferredInfo`, `CollectionInfo`, `BillingTindakanId`, `BillingLastError`, `ExecutionRegId`, audit columns.

**Children:** `LabOrderItemModel` — PK `(OrderId, ItemNo)`; snapshot test name, tarif refs, `SpecimenRequirementType` (tube/specimen/count).

**Vacutainer (V1):** Pragmatic grouping in handler or small private helper — group items by specimen/tube type (`EDTA`, `Serum`, `Citrate`, `Heparin`), return counts. **No** `IVacutainerGroupResolver` in V1.

**Behaviour methods (examples):**

```text
CreateFromEmr / CreateExternal
Defer(reason, until, userId)
ActivateFromDeferred(executionRegId, snapshot, userId)
Charge()                    # preconditions only; actual Tindakan via integration
MarkCharged(tindakanId, userId)
CollectSpecimen(...)
MarkRecorded(userId)
Cancel(userId, reason)      # Ordered | Deferred | Charged only
Terminate(userId, reason)   # Collected | Recorded only
UpdateFinancialClearance(status)
Release(userId)             # requires clearance Approved
AttachPatientSnapshot(...)  # after REG
```

**Deferred rules:**

- Meaning: waiting for patient preparation (fasting, next-day collection, etc.).
- While `Deferred`: **no** billing, **no** collection, **no** result entry.
- On execution: new REG registration → set `ExecutionRegId`; **do not** replace original order context.

Specimen collection requires order status = Charged.

Result recording is operationally allowed before billing charge completion.
Charging failure blocks administrative progression but does not necessarily block laboratory execution.

**Immutable states:** `Verified`, `Released` on order workflow where applicable; verified **result versions** never mutated.

Verified result document is immutable.
Released order workflow is operationally final.

### 5.3 `LabResultDocumentModel`

- 1 order : 1 result document.
- Versions: full immutable snapshot per version; amendment = new version only.
- `Verify(pathologistId, userId)` on document — medical validation, **not** release.

### 5.4 Domain implementation order

1. Enums + keys + `PatientSnapshotType`  
2. `LabOrderModel` + items + transitions + tests  
3. `LabResultDocumentModel` + versions + components + tests  

**Skill:** `docs/skills/feature-model-generation.md`

---

## 6. Database Implementation Plan

### 6.1 Table

**Prefix:** `BILRG_` for **all** LWF tables.

| Table | Purpose |
|-------|---------|
| `BILRG_LabOrder` | Root workflow + patient snapshot (6 fields) + deferred/collection/billing refs |
| `BILRG_LabOrderItem` | PK `(OrderId, ItemNo)` |
| `BILRG_LabResultDocument` | PK `OrderId` |
| `BILRG_LabResultVersion` | PK `(OrderId, VersionNo)` |
| `BILRG_LabResultComponent` | PK `(OrderId, VersionNo, ComponentNo)` |
| `BILRG_LabOwareOutboundQueue` | Async OWR outbound |

**Not in V1:** `BILRG_LabOrderStateHist`.

**Standards:** `DATABASE.md` — `VARCHAR(12)` PK on roots, no FK constraints, audit + void columns, `INT` enums, `'3000-01-01'` empty dates, PascalCase columns.

### 6.2 Index

**Indexes (initial):**

| Table | Index |
|-------|-------|
| `BILRG_LabOrder` | `(LabOrderStatus, CrtDate)` |
| `BILRG_LabOrder` | `(RegId, LabOrderStatus)` |
| `BILRG_LabOrder` | `(OrderNo)` |
| `BILRG_LabOwareOutboundQueue` | `(Status, CrtDate)` |

### 6.3 Identifier Strategy

- OrderId -> VARCHAR(12), generated using INunaCounterBL
- OrderNo -> Human-readable LAB transaction number
- VersionNo -> Incremental integer per ResultDocument
- QueueId -> VARCHAR(12), generated using INunaCounterBL
- ItemNo -> Incremental integer inside aggregate

### 6.4 Result Persistence Strategy

M6 intentionally uses simplified two-table result persistence:

- BILRG_LabResultDocument
- BILRG_LabResultItem

Additional normalization is intentionally deferred until operational complexity justifies it.

The current structure prioritizes:

- operational simplicity,
- query simplicity,
- AI-agent readability,
- pragmatic workflow implementation.

---

## 7. API Implementation Plan

Controllers: `LabOrderController`, `LabResultController` under `Bilreg.Api/Controllers/LabContext/`.

### LabOrder

| Method | Route | Cmd/Query |
|--------|-------|-----------|
| POST | `fromEmr` | `LabOrderCreateFromEmrCmd` |
| POST | `external` | `LabOrderCreateExternalPatientCmd` |
| GET | `{orderId}` | `LabOrderGetQuery` |
| GET | `worklist` | `LabOrderWorklistQuery` |
| PATCH | `defer` | `LabOrderDeferCmd` |
| PATCH | `activateDeferred` | `LabOrderActivateDeferredCmd` |
| PATCH | `charge` | `LabOrderChargeCmd` |
| PATCH | `collect` | `LabOrderCollectSpecimenCmd` |
| PATCH | `cancel` | `LabOrderCancelCmd` |
| PATCH | `terminate` | `LabOrderTerminateCmd` |
| PATCH | `release` | `LabOrderReleaseCmd` |
| POST | `emr/cancel` | `LabOrderCancelFromEmrCmd` |

### LabResult

| Method | Route | Cmd/Query |
|--------|-------|-----------|
| GET | `{orderId}` | `LabResultGetQuery` |
| GET | `worklist/verification` | `LabResultVerificationWorklistQuery` |
| POST | `record` | `LabResultRecordCmd` |
| PATCH | `verify` | `LabResultVerifyCmd` |
| PATCH | `amend` | `LabResultAmendCmd` |
| GET | `{orderId}/pdf` | `LabResultPdfQuery` (on-demand, not persisted) |

### OWARE (async)

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `oware/dispatch` | Worker/cron: process queue batch |
| POST | `oware/retry/{queueId}` | Manual retry |

External EMR/OWR clients use HTTP to **LWF API only** — not to REG/BIL via HTTP from LWF internals.

---

## 8. Use-Case Implementation Order

Vertical slices: SQL → Dto/Dal → Repo → Model → Handler → Controller → Test.

| Phase | Slice |
|-------|--------|
| 0 | Enums, `BILRG_LabOrder` + `BILRG_LabOrderItem`, repo load/save smoke test |
| 1 | Create order (EMR + external), `LabOrderGetQuery` |
| 2 | Worklist projection |
| 3 | Deferred + activate (`ILabRegIntegration`) |
| 4 | Charge (`ILabBillingIntegration` placeholder → real) |
| 5 | Collect specimen + vacutainer grouping (simple) |
| 6 | Record result + `MarkRecorded` |
| 7 | Verify |
| 8 | Financial clearance + release |
| 9 | Cancel / terminate / EMR cancel |
| 10 | Amendment |
| 11 | OWARE queue + worker + retry |
| 12 | PDF render (current operational version) |

**Handler template (IgdVisit-style):**

1. Guard request  
2. `LoadEntity`  
3. Model behaviour  
4. Call integration interfaces (not foreign repos directly from handler)  
5. `TransHelper.NewScope` when multiple repos  
6. `SaveChanges`  
7. Return response  

---

## 9. Integration Plan

### Architecture decision

LWF, REG, and BIL live in the **same solution**.

| Forbidden | Required |
|-----------|----------|
| HTTP/API between LWF and REG/BIL | Application Integration Service |
| Controller-to-controller | Handler → integration interface → concrete service |
| Direct cross-context DAL/SQL | Service calls REG/BIL **repositories / application logic** |
| Domain Service for integration | Integration in Application + Infrastructure |

**Flow:**

```text
UseCase Handler
    → ILabRegIntegration / ILabBillingIntegration
        → LabRegIntegration / LabBillingIntegration (Infrastructure)
            → IRegRepo / ITindakanRepo / existing REG-BIL use cases
```

**Example interfaces (Application/LabContext/.../Integration/):**

```csharp
public interface ILabRegIntegration
{
    // Returns RegId after registration attach / deferred execution
    string CreateOrAttachRegistration(LabRegAttachRequest request);
}

public interface ILabBillingIntegration
{
    // Billing charge = create Tindakan; success returns TindakanId
    string CreateTindakan(LabBillingChargeRequest request);
}
```

Register implementations in `InfrastructureService.cs` (scoped).

**Why:** Explicit dependency, easy debugging, migration-friendly, no hidden HTTP.

### REG (current)

- Already exists; reuse `IRegRepo` and related application logic **inside** `LabRegIntegration`.
- V1 placeholder: return fake `RegId` until wiring is complete.

### BIL (current)

- Under development; `ITindakanRepo` / charge flow not ready.
- V1 placeholder: `LabBillingIntegration` returns fake `TindakanId`.
- **Terminology:** “Billing charge” = create **`Tindakan`**; success always yields **`TindakanId`** stored on order (`BillingTindakanId`).

### OWARE (async)

- **Not** synchronous integration.
- `BILRG_LabOwareOutboundQueue` + worker/cron + manual retry endpoint.
- Update `OwareStatus` on `LabOrder`: `Pending`, `Sent`, `Failed`.
- OWR is middleware only — LWF remains operational truth.

**Post-transaction:** OWR enqueue may run after order save; queue failure does not roll back order state.

---

## 10. SQL Script Plan

**Path:** `Bilreg.SqlDb/LabContext/{Feature}/`

**Deploy order:**

1. `BILRG_LabOrder.sql`  
2. `BILRG_LabOrderItem.sql`  
3. `BILRG_LabResultDocument.sql`  
4. `BILRG_LabResultVersion.sql`  
5. `BILRG_LabResultComponent.sql`  
6. `BILRG_LabOwareOutboundQueue.sql`  

Follow `DATABASE.md` formatting (`aa` alias in queries, `GO` separators).

---

## 11. Query/Read Model Plan

- **Writes:** `ILabOrderRepo`, `ILabResultDocumentRepo` only.  
- **Worklists:** Dedicated DAL → `LabOrderWorklistView`, `LabResultVerificationWorklistView` — no full aggregate load.  
- **Stable SQL** per worklist type (ENGINEERING.md §8).  
- **PDF** render (current operational version); render on demand.

**Patient fields in views:** `PatientId` (not `MRNumber`).

---

## 12. Transaction Boundary Plan

Follow **IgdVisit** patterns: small, explicit scopes.

| Operation | Scope |
|-----------|--------|
| Single repo save | Optional `TransHelper` |
| Order + result first record | One transaction: both repos |
| Charge | Call `ILabBillingIntegration` then save order with `TindakanId` (integration failure → no state advance) |
| Activate deferred | `ILabRegIntegration` then save order with `ExecutionRegId` + snapshot |
| OWARE dispatch | Queue row update separate from order (eventual consistency) |

**Avoid:** Distributed transaction abstractions, saga frameworks, mediator chaining.

**Multi-repo example (IgdVisit-style):**

```csharp
using var trans = TransHelper.NewScope();
_labOrderRepo.SaveChanges(order);
_labResultRepo.SaveChanges(result);
trans.Complete();
```

---

## 13. Frontend Dependency Consideration

| UI need | API |
|---------|-----|
| Left queue | `GET worklist?status=` |
| Workspace | `GET {orderId}` |
| Actions | Status + `FinancialClearance` + allowed transitions |
| Collection prep | Grouped vacutainer counts (dedicated query or included in get) |
| Release gate | Clearance must be `Approved` for external release |

Enum numeric values fixed once published. Snapshot avoids live REG joins for display.

---

## 14. Risk & Complexity Analysis

| Risk | Mitigation |
|------|------------|
| BIL not ready | Placeholder `TindakanId`; swap `LabBillingIntegration` impl |
| REG attach errors | Explicit integration result; order stays Deferred/Ordered |
| Confusing MR vs PatientId | Single field `PatientId` only |
| OWR payload churn | Opaque JSON in queue |
| Verified result mutation | Version-only amendments |
| Clearance vs payment | Separate enum; document in API |

---

## 15. Incremental Delivery Strategy

| Milestone | Capability |
|-----------|------------|
| M1 | Create + get order |
| M2 | Worklist |
| M3 | Deferred + activate (placeholder REG) |
| M4 | Charge (placeholder TindakanId) |
| M5 | Collect + vacutainer grouping |
| M6 | Record result |
| M7 | Verify (internal visibility) |
| M8 | Clearance + release |
| M9 | Cancel / terminate |
| M10 | Amendment + PDF |
| M11 | OWARE queue + retry |

Each milestone: domain tests for transitions + Dal/Repo tests.

---

## 16. Recommended Development Sequence

```text
1. Read DOMAIN.md + AGENT.md
2. SQL BILRG_LabOrder + BILRG_LabOrderItem
3. LabOrderModel + domain tests
4. Dto/Dal/Repo
5. Integration placeholders (Reg + Billing)
6. LabOrderCreateFromEmrCmd + API + tests
7. Continue phases §8 in order
8. LabResult tables + aggregate
9. Replace placeholders with real REG/BIL wiring when ready
10. OWARE queue + worker
```

**Parallel safe:** SQL/Dal tests vs domain tests (after schema frozen).  
**Serialize:** Enum values, `OrderId` format, table schemas.

---

## 17. AI-Agent Implementation Notes

1. **Reference code:** `IgdVisitModel`, `IgdVisitDaftarCmd`, `IgdVisitAssignRegisterCmd` — **not** `RegJalanWalkInCommand`.  
2. **Integration:** Handlers inject `ILabRegIntegration` / `ILabBillingIntegration` only — never `IRegRepo` / `ITindakanRepo` directly in Lab handlers.  
3. **Tables:** `BILRG_*` only; queue = `BILRG_LabOwareOutboundQueue`.  
4. **No MRNumber** in any artifact.  
5. **No** `BILRG_LabOrderStateHist` in V1.  
6. One slice per session; copy `OpCaseRepo` for child persistence only.  
7. Skills order: model → persistence → use case.  
8. OWARE queue: store opaque JSON string; worker deserializes/sends/retries.  
9. Billing field on order: `BillingTindakanId` (not generic BillingTrsId).  
10. Stop and ask if EMR payload contract is undefined (§19).

---

## 18. Explicitly Avoided Design

- `RegJalanWalkInCommand` as orchestration template  
- HTTP between LWF and REG/BIL in-solution  
- Direct cross-context DAL access  
- Domain Service for REG/BIL integration  
- `MRNumber` field  
- `LAB_*` table prefix (use `BILRG_*`)  
- `BILRG_LabOrderStateHist` in V1  
- Full LIS, accessioning, barcode lifecycle  
- OWR as source-of-truth  
- EMR → LWF state sync  
- Payment/refund logic in workflow state  
- Mutating verified result rows  
- Event-sourced / generic workflow engine  
- Mediator chaining, hidden orchestration  
- Persisted PDF truth  
- `IVacutainerGroupResolver` in V1  
- `UpdateStatus()` anemic patterns  

---

## 19. Open Questions / Technical Unknowns

| # | Question | Default for V1 |
|---|----------|----------------|
| 1 | EMR create-order payload schema | Minimal DTO + manual mapping |
| 2 | Real `LabBillingIntegration` contract when BIL ready | Placeholder `TindakanId` |
| 3 | Real deferred REG flow | Placeholder `RegId` via `ILabRegIntegration` |
| 4 | Financial clearance source (BIL push vs manual) | Financial clearance is manually updated in V1 through administrative command/use-case. Future integration with BIL may automate clearance synchronization. |
| 5 | OWR endpoint + retry policy | Queue + opaque JSON + manual retry |
| 6 | Test catalog at order create | Snapshot from EMR payload |
| 7 | `OrderNo` counter key | `INunaCounterBL` prefix `LAB` |
| 8 | Pathologist auth | Role check deferred to API auth layer |
| 9 | State history audit | **Skipped** unless requirement added |

---

## Appendix A — Enum Draft

**`LabOrderStatusEnum`:** `Ordered=1`, `Deferred=2`, `Charged=3`, `Collected=4`, `Recorded=5`, `Verified=6`, `Released=7`, `Cancelled=8`, `Terminated=9`

**`FinancialClearanceEnum`:** `Pending=0`, `Approved=1`, `Blocked=2`  
— Release to patient/external requires `Approved`. Internal view of verified result allowed before approval.

**`OwareStatusEnum`:** `Pending=0`, `Sent=1`, `Failed=2`

**`VacutainerTypeEnum`:** `Edta=1`, `Serum=2`, `Citrate=3`, `Heparin=4` (extend only with team agreement)

**`LabOrderSourceEnum`:** `Emr=1`, `ExternalPatient=2`

**`ResultSourceEnum`:** `Manual=1`, `Instrument=2`, `ExternalLis=3`

Enum numeric values are considered stable contract once frontend/API integration begins.
Do not reorder or renumber existing enum values.
New values may only be appended.

---

## Appendix B — Integration wiring checklist

| Component | Layer | Registers |
|-----------|-------|-----------|
| `ILabRegIntegration` | Application | — |
| `LabRegIntegration` | Infrastructure | `AddScoped<ILabRegIntegration, LabRegIntegration>()` |
| `ILabBillingIntegration` | Application | — |
| `LabBillingIntegration` | Infrastructure | Placeholder V1 |

**`LabRegIntegration` V2:** Inject `IRegRepo`, call existing reg logic (same as IGD assign-register pattern, but behind interface).

**`LabBillingIntegration` V2:** Inject `ITindakanRepo` + charge use case when BIL ready; return real `TindakanId`.

---

## Appendix C — Agent file checklist

| Task | Open |
|------|------|
| Orchestration | `IgdVisitDaftarCmd.cs`, `IgdVisitAssignRegisterCmd.cs` |
| Model | `IgdVisitModel.cs` |
| Repo detail | `OpCaseRepo.cs` |
| DAL | `OpCaseDal.cs` |
| SQL detail PK | `BILRG_OpCasePpa.sql` |
| Controller | `ScheduleOpController.cs` |

---

*End of IMPLEMENTATION_PLAN.md*
