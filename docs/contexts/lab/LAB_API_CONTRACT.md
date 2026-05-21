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
| **Route (current)** | `POST .../LabOrderFeature/fromEmr` |
| **Target request** | `EmrOrderId`, patient snapshot fields, `Items[]: { TarifId, TarifName? }` |
| **EMR must not send** | `TestId`, `TestCode`, `TestName`, tube/specimen/count, component lists |
| **Target response** | Success + `EmrOrderId` + workflow status fields — **no** dependency on internal `LabOrderId` in EMR systems |

**Legacy (current code):** `LabOrderCreateFromEmrCmd` still accepts EMR-supplied test/tube fields and returns `OrderId` + `OrderNo`. Replace in place during master-test Phase 3.

### EMR status lookup (planned)

| | |
|--|--|
| **Route (planned)** | `GET .../LabOrderFeature/byEmrOrderId/{emrOrderId}` |
| **Purpose** | Workflow/result summary for EMR correlation |

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

## Master catalog APIs (planned)

| Feature | Routes (planned) |
|---------|------------------|
| `LabComponentMasterFeature` | `GET .../components`, `GET .../components/{componentId}` (`MLCxxxx`) — read-only |
| `LabTestDefinitionFeature` | CRUD + `GET byTarif/{tarifId}` — hospital admin |

Document request/response JSON when each phase lands.

---

## Billing integration (application boundary)

Not exposed as LWF HTTP. Target: **one BIL Tindakan per LWF order** containing **all Tarif lines** — see master plan §6.5.

Current stub: `ILabBillingIntegration.CreateTindakan(LabBillingChargeRequest)` with `OrderId` + `UserId` only.

---

## Maintenance

When implementing a slice:

1. Change handler/DTO in code.  
2. Update the matching section in this file.  
3. Do **not** add versioned duplicate routes.
