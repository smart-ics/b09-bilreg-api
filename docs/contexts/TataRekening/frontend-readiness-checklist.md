# Tata Rekening — Frontend Readiness Checklist

Use this checklist before starting frontend implementation. All items should be **checked** for Phase 5 completion.

---

## REST API

- [x] All 10 SOP endpoints implemented under `/api/tatarekening`
- [x] GET Open (`/api/tatarekening/{regId}`)
- [x] POST Close, Merge, Verify, Adjust, Allocate, Finalize, Cancel Finalization, Reopen, Settlement Initiation
- [x] Controllers delegate to MediatR only (no business logic in API layer)
- [x] 125 automated tests passing (including 16 API integration tests)

---

## OpenAPI / Swagger

- [x] `docs/contexts/TataRekening/swagger.json` generated from live Swashbuckle output
- [x] All Tata Rekening paths included
- [x] Request schemas documented (MergeBillingRequest, FinancialVerificationRequest, etc.)
- [x] Response DTO schemas included (OpenTataRekeningApiResponse, MergeBillingApiResponse, etc.)
- [x] Enum definitions with integer values and descriptions
- [x] Bearer JWT security scheme defined
- [x] Per-operation security (GET anonymous, POST requires Bearer)
- [x] HTTP status codes on operations (200, 400, 401, 403, 404, 409, 422)
- [x] Live API also exposes full spec at `/openapi/v1.json` and Scalar at `/scalar/v1`

---

## DTO documentation

- [x] `TataRekeningSummaryDto` documented
- [x] `TrsBillSummaryDto` documented
- [x] `PaymentProjectionDto` documented
- [x] `MergeRequestSummaryDto` documented
- [x] `PaymentAllocationInputDto` documented
- [x] `FinancialAdjustmentInputDto` documented
- [x] All API request records documented
- [x] All API response wrappers documented
- [x] JSend success/error envelope documented

---

## Enum documentation

- [x] `TataRekeningStatusEnum` (Opened, Closed, Finalized, Lunas)
- [x] `FinancialVerificationStatusEnum`
- [x] `MergeRequestStatusEnum`
- [x] `FinancialAdjustmentTypeEnum`
- [x] `FinancialVerificationAction`
- [x] `BillModulGroup`
- [x] Integer JSON serialization noted for frontend

---

## Authentication

- [x] JWT Bearer mechanism documented
- [x] `Authorization: Bearer {token}` header documented
- [x] Verifikator roles `VERIF-SPV` / `VERIF-USR` documented
- [x] Permission `TATA-REKENING-WRITE` documented
- [x] GET Open anonymous; POST requires Verifikator
- [x] Actor identity from JWT (not request body) documented

---

## Error codes

- [x] 400 — business rule violation
- [x] 401 — unauthenticated
- [x] 403 — forbidden (non-Verifikator)
- [x] 404 — not found
- [x] 409 — concurrency conflict
- [x] 422 — validation error
- [x] 500 — unexpected error
- [x] Recommended frontend behaviour per status documented

---

## Workflow

- [x] SOP order mapped to API calls in integration guide
- [x] Adjustment → requiresReopen → Reopen branch documented
- [x] Cancel Finalization → re-allocate → re-finalize branch documented
- [x] Status-driven UI guard table provided
- [x] Mermaid sequence diagrams for Close, Merge, Verify, Adjust, Allocate, Finalize, Settlement

---

## Backend assumptions

- [x] Merge request creation not exposed via API
- [x] Payment/cashier out of scope for Tata Rekening API
- [x] Optimistic concurrency (409 + reload) documented
- [x] Route typo `settlement-initiation` documented
- [x] Enum integer serialization documented

---

## Handover artifacts

| File | Location |
|------|----------|
| OpenAPI spec | [`swagger.json`](swagger.json) |
| Integration guide | [`frontend-integration-guide.md`](frontend-integration-guide.md) |
| API contract (concise) | [`tata-rekening-api-contract.md`](tata-rekening-api-contract.md) |
| Phase 5 report | [`tata-rekening-phase-5-implementation-report.md`](tata-rekening-phase-5-implementation-report.md) |
| HTTP smoke file | `Bilreg.Api/Controllers/PaymentContext/TataRekeningFeature/TataRekening.http` |

---

## Frontend can start when

All boxes above are checked **and** the team has:

1. A valid JWT with Verifikator role for write operations
2. API base URL for the target environment
3. A test `regId` with Tata Rekening data in the database

**Status:** Ready for frontend implementation (Phase 6).
