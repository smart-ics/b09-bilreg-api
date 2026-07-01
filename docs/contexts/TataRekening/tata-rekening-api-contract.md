# Tata Rekening — API Contract

**Artifact role:** INTEGRATION — HTTP surface for frontend clients  
**Base:** JWT authentication (`Program.cs`). Write endpoints require policy `TataRekeningVerifikator` (`TATA-REKENING-WRITE` permission). Open (read) is anonymous per project convention.

---

## Authentication

| Item | Detail |
|------|--------|
| Scheme | Bearer JWT |
| Write endpoints | `[Authorize(Policy = TataRekeningVerifikator)]` |
| Read endpoint | `GET /api/tatarekening/{regId}` — `[AllowAnonymous]` |
| Verifikator roles | `VERIF-SPV`, `VERIF-USR` (claim type `role` or `ClaimTypes.Role`) |
| Permissions | `TATA-REKENING-READ`, `TATA-REKENING-WRITE` via `RolePermissionDto` |
| Actor identity | `sub` / `NameIdentifier` claim → `PetugasVerif` and audit `userId` |

---

## Response envelope

All success responses use JSend:

```json
{
  "status": "success",
  "code": 200,
  "data": { }
}
```

Errors (middleware) use the same shape with appropriate HTTP status.

| HTTP | Meaning |
|------|---------|
| 400 | Business rule violation (`InvalidOperationException`) |
| 401 | Not authenticated |
| 403 | Authenticated but missing Verifikator permission |
| 404 | Registration / entity not found (`KeyNotFoundException`) |
| 409 | Optimistic concurrency conflict (stale version) |
| 422 | Validation error (`ArgumentException`, invalid model state) |
| 500 | Unexpected error |

---

## Endpoints

### SOP-TR-01 — Open Tata Rekening

```http
GET /api/tatarekening/{regId}
```

| Item | Detail |
|------|--------|
| Auth | None |
| Handler | `OpenTataRekeningQuery` |
| Purpose | Load Tata Rekening workspace: summary, bills, projection, pending merge requests |

**Response `data`:**

| Field | Type |
|-------|------|
| `summary` | `TataRekeningSummaryDto` |
| `bills` | `TrsBillSummaryDto[]` |
| `projection` | `PaymentProjectionDto[]` |
| `pendingMergeRequests` | `MergeRequestSummaryDto[]` |

---

### SOP-TR-02 — Close Bill

```http
POST /api/tatarekening/{regId}/close
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | _(empty)_ |
| Handler | `CloseBillCommand` |
| Purpose | Close billing set when operational charges are complete |

**Response `data`:** `{ "summary": TataRekeningSummaryDto }`

---

### SOP-TR-03 — Merge Billing

```http
POST /api/tatarekening/merge
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | `{ "mergeRequestId": "MR-00001" }` |
| Handler | `MergeBillingCommand` |
| Purpose | Execute pending merge request; move bills to target registration |

**Response `data`:** `{ "mergeRequest", "sourceSummary", "targetSummary" }`

---

### SOP-TR-04 — Financial Verification

```http
POST /api/tatarekening/{regId}/verify
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | `{ "action": "Verify" \| "RequireAdjustment", "verifiedAt": "optional ISO datetime" }` |
| Handler | `FinancialVerificationCommand` |
| Purpose | Mark verification valid or flag adjustment required |

**Response `data`:** `{ "summary": TataRekeningSummaryDto }`

---

### SOP-TR-05 — Financial Adjustment

```http
POST /api/tatarekening/{regId}/adjust
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | `{ "adjustment": FinancialAdjustmentInputDto, "appliedAt": "optional" }` |
| Handler | `FinancialAdjustmentCommand` |
| Purpose | Apply waive, subsidy, manual charge, or charge-source correction |

**Response `data`:** `{ "summary", "requiresReopen", "adjustmentType", "trsBillingId" }`

---

### SOP-TR-06 — Financial Responsibility Allocation

```http
POST /api/tatarekening/{regId}/allocate
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | `{ "payments": PaymentAllocationInputDto[] }` |
| Handler | `AllocateFinancialResponsibilityCommand` |
| Purpose | Allocate payer responsibility and regenerate projection |

**Response `data`:** `{ "summary", "projection" }`

---

### SOP-TR-07 — Finalize Financial Responsibility

```http
POST /api/tatarekening/{regId}/finalize
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | `{ "finalizationDate": "optional" }` |
| Handler | `FinalizeFinancialResponsibilityCommand` |
| Purpose | Lock financial responsibility after allocation review |

**Response `data`:** `{ "summary": TataRekeningSummaryDto }`

---

### SOP-TR-08 — Cancel Finalization

```http
POST /api/tatarekening/{regId}/cancel-finalization
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | `{ "reason": "required text" }` |
| Handler | `CancelFinalizationCommand` |
| Purpose | Revert FINALIZED → CLOSED for re-allocation |

**Response `data`:** `{ "summary": TataRekeningSummaryDto }`

---

### SOP-TR-09 — Reopen Billing

```http
POST /api/tatarekening/{regId}/reopen
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | `{ "reason": "required text" }` |
| Handler | `ReopenBillingCommand` |
| Purpose | Reopen CLOSED billing after charge-source adjustment |

**Response `data`:** `{ "summary": TataRekeningSummaryDto }`

---

### SOP-TR-10 — Settlement Initiation

```http
POST /api/tatarekening/{regId}/settlement-initiation
```

| Item | Detail |
|------|--------|
| Auth | Verifikator |
| Body | `{ "initiatedAt": "optional" }` |
| Handler | `SettlementInitiationCommand` |
| Purpose | Hand off finalized bill to cashier settlement |

**Response `data`:** `{ "summary": TataRekeningSummaryDto }`

---

## Application DTO reference

Reused from `Bilreg.Application/PaymentContext/TataRekeningFeature/Dtos/` — not domain types.

| DTO | Key fields |
|-----|------------|
| `TataRekeningSummaryDto` | `regId`, `status`, `financialVerificationStatus`, `isFinancialResponsibilityAllocated`, `settlementInitiated` |
| `PaymentAllocationInputDto` | `paymentId`, `paymentName`, `isTipeJaminan`, `nilaiJasa`, `nilaiObat`, `coaId`, `coaName` |
| `FinancialAdjustmentInputDto` | `type`, `amount`, `reason`, optional bill/subsidy fields |

---

## OpenAPI / Scalar

- OpenAPI JSON: `/openapi/v1.json`
- Scalar UI: `/scalar/v1`
- Bearer security scheme documented in Swagger
