# Tata Rekening Phase 2 — Implementation Report

**Date:** 2026-06-30  
**Scope:** Domain Feature Completion only (Phase 2)

---

## Summary

Phase 2 completes missing Tata Rekening domain capabilities: MergeRequest lifecycle, Merge Billing domain service, Projection Regeneration domain service, Financial Verification domain service, Financial Adjustment domain service, and supporting aggregate/TrsBill hooks. All **80** Tata Rekening unit tests pass (58 Phase 1 + 22 Phase 2).

---

## Files Added

| File | Purpose |
|------|---------|
| `Bilreg.Domain/.../MergeRequestStatusEnum.cs` | Pending / Executed / Cancelled |
| `Bilreg.Domain/.../MergeRequestModel.cs` | Merge Request entity with Create / Execute / Cancel / AssignTarget |
| `Bilreg.Domain/.../FinancialAdjustmentTypeEnum.cs` | ManualCharge, BillingCorrection, Waive, Subsidy, MergeBillingCorrection |
| `Bilreg.Domain/.../FinancialAdjustmentRequest.cs` | Adjustment input DTO |
| `Bilreg.Domain/.../FinancialAdjustmentResult.cs` | Adjustment result (RequiresReopen flag) |
| `Bilreg.Domain/.../MergeBillingResult.cs` | Merge execution result |
| `Bilreg.Domain/.../IProjectionRegenerationDomainService.cs` | Projection service contract |
| `Bilreg.Domain/.../ProjectionRegenerationDomainService.cs` | Clear / regenerate projection |
| `Bilreg.Domain/.../IMergeBillingDomainService.cs` | Merge Billing service contract |
| `Bilreg.Domain/.../MergeBillingDomainService.cs` | Cross-registration merge orchestration |
| `Bilreg.Domain/.../IFinancialVerificationDomainService.cs` | Verification service contract |
| `Bilreg.Domain/.../FinancialVerificationDomainService.cs` | Verify / RequireAdjustment / ResetVerification |
| `Bilreg.Domain/.../IFinancialAdjustmentDomainService.cs` | Adjustment service contract |
| `Bilreg.Domain/.../FinancialAdjustmentDomainService.cs` | Five adjustment types |
| `Bilreg.Domain/.../TrsBillFinancialAdjustmentRecord.cs` | Financial-layer adjustment audit on bill |
| `Bilreg.Test/.../TataRekeningPhase2DomainTest.cs` | 22 Phase 2 domain tests |

---

## Files Modified

| File | Change |
|------|--------|
| `Bilreg.Domain/.../TataRekeningModel.cs` | Public `ResetFinancialVerification`; internal merge/adjustment/projection hooks; `FinancialTotal` in allocation math |
| `Bilreg.Domain/.../TrsBillType.cs` | `TransferToRegistration`, `EnsureMergeable`, `ApplyFinancialAdjustment`, `FinancialTotal` offset |
| `Bilreg.Domain/.../TataRekeningDomainEvents.cs` | Stub records `MergeBillingExecuted`, `FinancialAdjusted` |
| `Bilreg.Test/.../TataRekeningDomainTestHelper.cs` | Shared domain service instances for tests |

---

## Design Decisions

1. **MergeRequest as standalone entity** — Not embedded in `TataRekeningModel` per `02-domain.md`; lifecycle encapsulated in `MergeRequestModel`.

2. **Cross-registration merge in domain service** — `MergeBillingDomainService` orchestrates two aggregates; aggregate exposes narrow `internal` hooks (`ReleaseBillingSetForMerge`, `AcceptMergedBillingSet`).

3. **Projection regeneration extracted** — `ProjectionRegenerationDomainService` wraps `ClearFinancialProjection` / `RegenerateFinancialProjection` internal methods; `AllocateFinancialResponsibility` delegates to regeneration after guards.

4. **Financial adjustments preserve operational history** — `TrsBillType` uses `_financialAdjustmentOffset` + `FinancialTotal`; original `Nilai` and transaction events unchanged; allocation uses `FinancialTotal`.

5. **Verification pending-merge guard** — `FinancialVerificationDomainService.Verify` accepts `IEnumerable<MergeRequestModel>` from caller (no repository in domain).

6. **RequiresReopen path** — `FinancialAdjustmentRequest.RequiresChargeSourceChange` returns result without mutation; Phase 3 app layer routes to `ReOpen()`.

7. **Domain events** — Record types added only; no raising/dispatch (deferred to Phase 3+).

8. **Service naming** — Follows `CreateBillDomService` pattern: `I*DomainService` + sealed implementation.

---

## Assumptions

- Post-merge, target verification resets to `NotVerified` (SOP chain: merge → verify → allocate).
- Source after merge may have an empty billing set while remaining CLOSED.
- `AssignTarget` on pending MergeRequest supports late target binding before execution.
- Accounting Transfer Receivable, audit trail, and transactional rollback are Phase 3/4 concerns.
- Allocation domain service (`02-domain.md`) not extracted in Phase 2; allocation remains on aggregate backed by projection service.

---

## Remaining Gaps (Phase 3+)

| Area | Phase |
|------|-------|
| Application commands/handlers per SOP | 3 |
| MediatR orchestration + unit of work | 3 |
| Domain event dispatch | 3+ |
| MergeRequest / verification persistence | 4 |
| Accounting Transfer Receivable on merge | 4 |
| Audit trail (merge, cancel, reopen, adjustment) | 4 |
| Verifikator authorization | 5 |
| REST API / DTO | 5 |
| Open Tata Rekening query with pending merge discovery | 3 |

---

## Test Results

```
dotnet test Bilreg.Test --filter "FullyQualifiedName~TataRekening"
Passed: 80, Failed: 0
```
