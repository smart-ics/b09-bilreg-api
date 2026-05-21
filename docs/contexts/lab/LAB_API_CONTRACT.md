# LAB_API_CONTRACT.md — Laboratory Workflow API (pre-release)

> **Status:** Evolving implementation-aligned contract — **not** a frozen public specification.  
> **Canonical location:** `docs/contexts/lab/LAB_API_CONTRACT.md`  
> **Authority:** Backend owns shapes; update this file when handlers/DTOs change.  
> **Related:** [`LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md`](LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md), [`lab-implementation-plan.md`](lab-implementation-plan.md), [`LAB_MASTER_TEST_ALIGNMENT.md`](LAB_MASTER_TEST_ALIGNMENT.md)

**Pre-release rules:**

- LWF is **unreleased** — no backward compatibility, no `/v1`, no `fromEmrV2`, no parallel deprecated routes.
- Replace endpoint bodies **in place** when contracts change.
- Enum **numeric values** are stable once integrated; HTTP/DTO shapes may change until release.

---

## Base path

Controllers live under:

```text
/api/LabContext/{Feature}/...
```

Examples below use feature-relative routes (as implemented in `Bilreg.Api`).

---

## EMR integration (target contract)

Authoritative business rules: [`LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md`](LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md) §7.3.

### Create order from EMR

| | |
|--|--|
| **Route** | `POST .../LabOrderFeature/fromEmr` |
| **Request** | `EmrOrderId`, patient snapshot fields, `Items[]: { TarifId, TarifName? }` |
| **EMR must not send** | `TestId`, `TestCode`, `TestName`, tube/specimen/count, component lists |
| **Response** | `EmrOrderId`, `LabOrderStatus`, `OrderNo` — **no** internal `LabOrderId` |

LWF resolves each `TarifId` to `LabTestDefinition`, specimen/vacutainer, and immutable `LabOrderItem` + `LabOrderItemComponent` snapshots.

**Failure codes (order create):** `LAB_TEST_DEFINITION_NOT_FOUND`, `LAB_TEST_DEFINITION_INACTIVE`, `LAB_COMPONENT_INACTIVE`, `LAB_COMPONENT_NOT_FOUND`.

### EMR status lookup

| | |
|--|--|
| **Route** | `GET .../LabOrderFeature/byEmrOrderId/{emrOrderId}` |
| **Purpose** | Workflow status for EMR correlation (`EmrOrderId`, `LabOrderStatus`, `OrderNo`, billing/cancel fields) |

### Cancel from EMR

| | |
|--|--|
| **Route (current)** | `POST .../LabOrderFeature/emr/cancel` |

---

## Lab order (workflow — current surface)

See [`lab-implementation-plan.md`](lab-implementation-plan.md) §7 for full route list (`fromEmr`, `external`, `charge`, `collect`, worklist, etc.).

Internal lab UI may use `OrderId` / `OrderNo`; EMR integration uses **`EmrOrderId`** only (target).

---

## Lab result (current surface)

| Route | Notes |
|-------|-------|
| `GET .../LabResultFeature/{orderId}` | Read result document |
| Record / amend / verify | See `lab-implementation-plan.md` §7 |

**Target (Phase 4):** record body carries **values only** keyed to server scaffold lines — see master plan §8.

---

## Lab component master (vendor catalog — implemented)

Read-only vendor-owned catalog. **No** POST/PUT/DELETE. Operational identity is `ComponentCode`; `LoincCode` is optional metadata only.

### List / search components

| | |
|--|--|
| **Route** | `GET /api/LabContext/LabComponentMasterFeature/components` |
| **Query** | `activeOnly` (bool, default `true`) — when true, only `IsActive = 1` rows |
| **Query** | `search` (string, optional) — case-insensitive partial match on `ComponentCode` or `ComponentName` |
| **Response** | Array of `LabComponentMasterListResponse` |

**Response fields (list item):**

| Field | Type | Notes |
|-------|------|-------|
| `componentId` | string | `MLC` + 4 uppercase hex (e.g. `MLC0001`) |
| `loincCode` | string \| null | Optional metadata |
| `componentCode` | string | Operational code |
| `componentName` | string | Display name |
| `resultType` | int | `LabResultTypeEnum`: 1 Numeric, 2 Text, 3 Option, 4 Narrative |
| `defaultUnit` | string | |
| `isSystem` | bool | |
| `isActive` | bool | |

### Get component detail

| | |
|--|--|
| **Route** | `GET /api/LabContext/LabComponentMasterFeature/components/{componentId}` |
| **Path** | `componentId` — `MLCxxxx` |
| **Response** | `LabComponentMasterGetResponse` (same fields as list item) |
| **Not found** | Same pattern as other Lab GET handlers (e.g. `LabOrderGetQuery`) |

---

## Lab test definition (hospital admin — implemented)

Hospital-configurable operational **LabTest** template: Tarif mapping, specimen/vacutainer metadata, and `LabTestComponent` membership. **No** standalone component CRUD — children are managed only through the parent aggregate.

**ID:** `TestDefinitionId` = `LTD` + 4 uppercase hex (e.g. `LTD0001`). Allocated sequentially by the API on create (`SELECT MAX` + hex increment).

### List / search definitions

| | |
|--|--|
| **Route** | `GET /api/LabContext/LabTestDefinitionFeature/definitions` |
| **Query** | `activeOnly` (bool, default `false`) |
| **Query** | `search` (string, optional) — partial match on `LabTestCode`, `LabTestName`, `TarifCode`, `TarifId` |
| **Query** | `tarifId` (string, optional) — exact filter |
| **Response** | Array of `LabTestDefinitionListResponse` |

**List item fields:**

| Field | Type | Notes |
|-------|------|-------|
| `testDefinitionId` | string | `LTDxxxx` |
| `tarifId` | string | BIL Tarif reference |
| `tarifCode` | string | Cached label |
| `tarifName` | string | Cached label |
| `labTestCode` | string | Operational code |
| `labTestName` | string | Operational name |
| `specimenType` | string | |
| `vacutainerType` | int | `VacutainerTypeEnum`: 1 Edta, 2 Serum, 3 Citrate, 4 Heparin |
| `isActive` | bool | |
| `componentCount` | int | Child row count |

### Get definition detail

| | |
|--|--|
| **Route** | `GET /api/LabContext/LabTestDefinitionFeature/definitions/{testDefinitionId}` |
| **Response** | `LabTestDefinitionDetailResponse` |

**Detail fields:** list item fields plus `components[]`:

| Field | Type | Notes |
|-------|------|-------|
| `sequenceNo` | int | Display order (unique per definition) |
| `componentId` | string | `MLCxxxx` → `LabComponentMaster` |
| `referenceRangeOverride` | string | Hospital text override |
| `requiredFlagging` | bool | |
| `isMandatory` | bool | |

### Preview active definition by Tarif

| | |
|--|--|
| **Route** | `GET /api/LabContext/LabTestDefinitionFeature/byTarif/{tarifId}` |
| **Purpose** | Resolve the **active** `LabTestDefinition` for a Tarif (future order-resolution preview) |
| **Response** | `LabTestDefinitionDetailResponse` |
| **Not found** | `LAB_TEST_DEFINITION_NOT_FOUND` |

### Create definition

| | |
|--|--|
| **Route** | `POST /api/LabContext/LabTestDefinitionFeature/definitions` |
| **Body** | `LabTestDefinitionCreateCmd` |
| **Response** | `{ testDefinitionId }` — server-allocated `LTDxxxx` |

**Create body fields:**

| Field | Type | Required |
|-------|------|----------|
| `tarifId` | string | yes |
| `tarifCode` | string | |
| `tarifName` | string | |
| `labTestCode` | string | yes |
| `labTestName` | string | yes |
| `specimenType` | string | |
| `vacutainerType` | int | yes |
| `isActive` | bool | |
| `userId` | string | yes |
| `components` | `LabTestComponentInput[]` | yes (may be empty when `isActive` is false) |

**Component input:** `sequenceNo`, `componentId` (`MLCxxxx`), `referenceRangeOverride`, `requiredFlagging`, `isMandatory`.

### Update definition

| | |
|--|--|
| **Route** | `PUT /api/LabContext/LabTestDefinitionFeature/definitions/{testDefinitionId}` |
| **Body** | Same shape as create (without path id) — **replaces** all child rows (delete + bulk insert) |

### Activate / deactivate

| | |
|--|--|
| **Route** | `POST .../definitions/{testDefinitionId}/activate` |
| **Route** | `POST .../definitions/{testDefinitionId}/deactivate` |
| **Body** | `{ userId }` |

### Validation / error codes (domain)

| Code | When |
|------|------|
| `LAB_TEST_DEFINITION_NOT_FOUND` | No active definition for Tarif (byTarif) |
| `LAB_TEST_DEFINITION_INACTIVE` | Inactive definition (byTarif guard) |
| `LAB_TEST_DEFINITION_TARIF_CONFLICT` | Another active definition already uses this `TarifId` |
| `LAB_TEST_DEFINITION_EMPTY_COMPONENTS` | Active definition with zero components |
| `LAB_COMPONENT_INACTIVE` | Child references inactive `MLCxxxx` |
| `LAB_COMPONENT_NOT_FOUND` | Unknown `MLCxxxx` |
| `LAB_DUPLICATE_COMPONENT` | Same `ComponentId` twice on one definition |
| `LAB_DUPLICATE_SEQUENCE` | Same `SequenceNo` twice on one definition |
| `LAB_INVALID_LTD_FORMAT` | Invalid `TestDefinitionId` format |
| `LAB_INVALID_MLC_FORMAT` | Invalid `ComponentId` format |

**Out of scope for this feature:** standalone `LabTestComponent` APIs, order resolution, result scaffold, API versioning.

---

## Billing integration (application boundary)

Not exposed as LWF HTTP. Target: **one BIL Tindakan per LWF order** containing **all Tarif lines** — see master plan §6.5.

Stub: `ILabBillingIntegration.CreateTindakan(LabBillingChargeRequest)` with `OrderId`, `UserId`, and **`TarifLines[]`** (`TarifId`, `TarifCode`, `TarifName`) collected from resolved order items — prepared for one Tindakan / many Tarif lines. Real BIL orchestration remains out of scope.

---

## Maintenance

When implementing a slice:

1. Change handler/DTO in code.  
2. Update the matching section in this file.  
3. Do **not** add versioned duplicate routes.
