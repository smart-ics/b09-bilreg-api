# Tata Rekening Phase 4 — Implementation Report

**Date:** 2026-06-30  
**Scope:** Infrastructure & Integration only (Phase 4)

---

## Summary

Phase 4 replaces temporary Phase 3 infrastructure with SQL-backed persistence, wires audit and accounting into existing handlers, completes projection round-trips, adds optimistic concurrency on the Tata Rekening header, and routes RegOut payment writes through a single `ITataRekeningRepo` path. **109** Tata Rekening tests pass (96 prior + 13 Phase 4).

---

## Files Added

| File | Purpose |
|------|---------|
| `Bilreg.SqlDb/PaymentContext/TataRekeningFeature/BILRG_MergeRequest.sql` | MergeRequest table + indexes |
| `Bilreg.SqlDb/PaymentContext/TataRekeningFeature/BILRG_TataRekening_M1_Phase1State_Alter.sql` | ALTER for phase-1 state columns + Version |
| `Bilreg.Application/.../ITransferReceivableService.cs` | Accounting integration contract |
| `Bilreg.Infrastructure/.../BilrgMergeRequestDto.cs` | MergeRequest row mapping |
| `Bilreg.Infrastructure/.../BilrgMergeRequestDal.cs` | Dapper DAL for `BILRG_MergeRequest` |
| `Bilreg.Infrastructure/.../MergeRequestRepo.cs` | SQL `IMergeRequestRepo` implementation |
| `Bilreg.Infrastructure/.../TransferReceivableService.cs` | Transfer Receivable on `t_bp_piutang_hdr` |
| `Bilreg.Infrastructure/.../TataRekeningModelRebuilder.cs` | Infrastructure helper for RegOut → TR routing |
| `Bilreg.Test/.../MergeRequestRepoTest.cs` | MergeRequest repo unit tests |
| `Bilreg.Test/.../TataRekeningDtoTestHelper.cs` | Shared DTO test factory |
| `Bilreg.Test/.../TataRekeningPhase3ApplicationTestHarness.cs` | Shared merge-billing test harness |
| `Bilreg.Test/.../TataRekeningPhase4ApplicationTest.cs` | Audit/accounting/rollback application tests |
| `Bilreg.Test/.../TataRekeningPhase4IntegrationTest.cs` | DAL/integration tests (graceful skip if DB not deployed) |

---

## Files Modified

| File | Change |
|------|--------|
| `Bilreg.SqlDb/.../BILRG_TataRekening.sql` | Canonical create script includes phase-1 + Version columns |
| `Bilreg.Domain/.../TataRekeningModel.cs` | `Version` property + `CommitVersionIncrement()` |
| `Bilreg.Infrastructure/.../BilrgTataRekeningDto.cs` | Phase-1 state + Version mapping |
| `Bilreg.Infrastructure/.../BilrgTataRekeningDal.cs` | Extended SQL; `UpdateConditional` for concurrency |
| `Bilreg.Infrastructure/.../TataRekeningRepo.cs` | Load/save phase-1 states; conditional update |
| `Bilreg.Infrastructure/.../RegPembayaranRepo.cs` | Routes writes through `ITataRekeningRepo` (single writer) |
| `Bilreg.Application/.../MergeBillingCommand.cs` | Transfer Receivable + audit before `Complete()` |
| `Bilreg.Application/.../AllocateFinancialResponsibilityCommand.cs` | Persist bill projections |
| `Bilreg.Application/.../FinalizeFinancialResponsibilityCommand.cs` | Persist bill projections |
| `Bilreg.Application/.../CancelFinalizationCommand.cs` | Persist bills + audit |
| `Bilreg.Application/.../FinancialAdjustmentCommand.cs` | Audit on persisted adjustments |
| `Bilreg.Application/.../ReopenBillingCommand.cs` | Audit with reason |
| `Bilreg.Application/.../SettlementInitiationCommand.cs` | Audit on settlement initiation |
| `Bilreg.Api/Configurations/InfrastructureService.cs` | Remove in-memory repo; register `ITransferReceivableService` |
| `Bilreg.Test/.../TataRekeningPhase3ApplicationTest.cs` | Updated handler constructors/mocks |
| `Bilreg.Test/.../TataRekeningRepoTest.cs` | Concurrency + phase-1 load tests |
| `Bilreg.Test/.../TataRekeningDtoTest.cs` | Extended DTO mapping tests |

---

## Files Removed

| File | Reason |
|------|--------|
| `Bilreg.Infrastructure/.../InMemoryMergeRequestRepo.cs` | Replaced by SQL `MergeRequestRepo` |

---

## Database Changes

### New table: `BILRG_MergeRequest`

- Core: `MergeRequestId`, `SourceRegId`, `TargetRegId`, `PatientId`, `Status`, `Reason`
- Lifecycle audit: `ExecutedBy/Date`, `CancelledBy/Date`
- Standard: `CrtUser`, `CrtDate`, `UpdUser`, `UpdDate`, `VodUser`, `VodDate`
- Indexes on `SourceRegId`, `TargetRegId`, `(PatientId, Status)`

### Extended: `BILRG_TataRekening`

| Column | Purpose |
|--------|---------|
| `FinVerifStatus` | `FinancialVerificationStatusEnum` |
| `FinVerifPetugas` | Verification verifier |
| `FinVerifDate` | Verification timestamp |
| `IsAllocated` | `IsFinancialResponsibilityAllocated` |
| `SettlementInitiated` | Settlement flag |
| `Version` | Optimistic concurrency token |

Deploy: `BILRG_MergeRequest.sql` + `BILRG_TataRekening_M1_Phase1State_Alter.sql` on existing databases.

### Unchanged legacy tables (reused)

- `ta_registrasi3` — level-1 allocation (single writer via `TataRekeningRepo`)
- `ta_trs_billing` / `ta_trs_billing2` — charges + projection
- `BILRG_AuditLog` — cross-cutting audit
- `t_bp_piutang_hdr` — Transfer Receivable target

---

## Design Decisions

1. **MergeRequest persistence-only** — Domain model unchanged; extra SQL columns (`PatientId`, executed/cancelled metadata) support queries and audit without expanding `MergeRequestModel`.

2. **Phase-1 state on header** — Verification, allocation, and settlement flags persisted on `BILRG_TataRekening` and passed into the existing `TataRekeningModel` constructor, eliminating incorrect rehydration inference on reload.

3. **Optimistic concurrency** — `Version` INT with `UpdateConditional` (same pattern as `PasienBalanceRepo`). Stale updates throw `InvalidOperationException`; UoW not completed → rollback.

4. **Transfer Receivable** — `TransferReceivableService` updates `t_bp_piutang_hdr.fs_keterangan` prefix from source to target reg (`LEFT(fs_keterangan,10)` linkage per legacy `RegHutangDal`). Zero rows updated is acceptable when no piutang exists; exceptions propagate for rollback.

5. **Audit** — Explicit `IAuditRepo` calls in handlers per `docs/shared/audit-log.md`, inside UoW before `Complete()`. Actor is `SYSTEM` until Phase 5 authorization.

6. **Dual-writer elimination** — `RegPembayaranRepo` delegates all writes to `ITataRekeningRepo`; RegOut handlers unchanged. Rebuilds aggregate payment list via `TataRekeningModelRebuilder` without new domain methods.

7. **Projection persistence** — Allocate/finalize/cancel handlers call `ITrsBillingRepo.SaveChanges` per bill (same as merge/adjust). Regeneration algorithm unchanged.

8. **DI** — `MergeRequestRepo` auto-registered via Scrutor `ISaveChange<>` scan; explicit `InMemoryMergeRequestRepo` override removed.

---

## Test Results

```
dotnet test Bilreg.Test --filter "FullyQualifiedName~TataRekening"
Passed: 109, Failed: 0
```

New coverage: MergeRequest repo, header concurrency, phase-1 DTO mapping, transfer-receivable rollback, integration DAL round-trips (skip gracefully if SQL scripts not deployed).

---

## Remaining Gaps (Phase 5+)

| Area | Phase |
|------|-------|
| REST API / controllers / Swagger | 5 |
| Verifikator authorization | 5 |
| `CancelFinalizationCommand` reason parameter | 5 |
| Domain event dispatcher | 5+ |
| Consolidate duplicate `BILRG_TataRekening.sql` in TrsBillingFeature folder | Maintenance |
| User identity on audit rows (replace `SYSTEM`) | 5 |

---

## Build Verification

```
dotnet build Bilreg.Api
Build succeeded
```
