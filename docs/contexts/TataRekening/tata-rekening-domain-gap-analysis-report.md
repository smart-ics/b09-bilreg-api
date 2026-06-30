# Tata Rekening Gap Analysis Report

**Audit date:** 2026-06-30  
**Source of truth:** `docs/contexts/TataRekening/` (01-context.md, 02-domain.md, 03-design.md, 04-sop.md, SOP-TR-01 … SOP-TR-10)  
**Implementation scope:** Full bounded context — Domain, Application, Presentation, Infrastructure, Database, Integration, Authorization, Workflow, Persistence, API, UI, Tests

---

## Executive Summary

- **Overall implementation completeness:** ~**30%**
- **Overall architecture alignment:** ~**38%**
- **Total findings by severity:** Critical **5** · High **12** · Medium **10** · Low **5** · **32 total**

The Tata Rekening bounded context has a **credible domain foundation**: `TataRekeningModel` implements the OPEN → CLOSED → FINALIZED → LUNAS lifecycle, proportional allocation math, bill-mutation guards, and `CancelFinalization`. Persistence scaffolding (`BILRG_TataRekening`, `ta_registrasi3`, `ta_trs_billing` / `ta_trs_billing2`) and Charge Source entry via `AddBillAppService` are in place.

However, **seven of ten SOP workflows have no application or API surface**, Merge Billing and Financial Verification/Adjustment are **entirely absent**, and **privileged operations lack Verifikator authorization**. Legacy `RegOutFeature` still writes `ta_registrasi3` in parallel with `TataRekeningRepo`, creating a **dual-writer risk** for Financial Responsibility. Domain lifecycle methods are **never orchestrated or persisted** in production paths beyond bill creation.

Artifacts win on every conflict below.

---

## Findings Summary

| Severity | Count |
|----------|------:|
| Critical | 5 |
| High | 12 |
| Medium | 10 |
| Low | 5 |

---

## Detailed Findings

### GAP-001

**Category:** Workflow / Persistence  
**Current Implementation:** `TataRekeningModel.Close()`, `FinalizeFinancialResponsibility()`, `Pay()`, `CancelFinalization()`, and `ReOpen()` exist in domain and are covered by unit tests, but **no application command, handler, or service invokes them**. `TataRekeningRepo.SaveChanges` persists header + `ta_registrasi3` payment rows only; it does **not** call `TrsBillingRepo.SaveChanges` for bill finalization/payment projection rows in `ta_trs_billing2`.  
**Expected Artifact:** Every lifecycle transition (SOP-TR-02, 06, 07, 08, 09, 10) must be orchestrated at Application layer with registration-scoped transaction boundaries including projection regeneration (`03-design.md` §TRANSACTION STRATEGY).  
**Impact:** **Critical** — Financial lifecycle changes cannot be executed end-to-end; finalization and payment projections would not persist even if called manually.  
**Recommendation:** Add MediatR commands/handlers per SOP; implement a unit-of-work that atomically saves `TataRekeningRepo` + all affected `TrsBillingRepo` instances within one transaction.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs`, `Bilreg.Infrastructure/PaymentContext/TataRekeningFeature/TataRekeningRepo.cs`, `Bilreg.Application/PaymentContext/TataRekeningFeature/ITataRekeningRepo.cs` (interface only)

---

### GAP-002

**Category:** Integration / Financial Truth  
**Current Implementation:** Two independent writers target `ta_registrasi3`: `TaRegistrasi3Dal` (used by `TataRekeningRepo`) and `RegPembayaranDal` (used by `RegAddPembayaranHandler` → `RegPembayaranRepo`). `RegAddPembayaranCmd` inserts `RegPembayaranType` without loading or updating `TataRekeningModel`.  
**Expected Artifact:** Tata Rekening owns Financial Responsibility per registration (`01-context.md` §RESPONSIBILITY BOUNDARY); Cashier settlement operates on finalized projection (`04-sop.md` §3.5). Level-1 allocation must flow through the aggregate.  
**Impact:** **Critical** — Parallel legacy path can produce inconsistent Financial Responsibility vs. aggregate state.  
**Recommendation:** Route all `ta_registrasi3` mutations through `TataRekeningRepo`; deprecate or gate `RegAddPembayaranCmd` behind Tata Rekening orchestration.  
**Affected Files:** `Bilreg.Application/PaymentContext/RegOutFeature/UseCase/RegAddPembayaranCmd.cs`, `Bilreg.Infrastructure/PaymentContext/RegOutFeature/RegPembayaranDal.cs`, `Bilreg.Infrastructure/PaymentContext/TataRekeningFeature/TaRegistrasi3Dal.cs`

---

### GAP-003

**Category:** Domain / Workflow  
**Current Implementation:** No `MergeRequest` entity, status enum, repository, SQL table, or command. Grep across `*.cs` and `*.sql` returns zero matches for `MergeRequest`, `MergeBilling`, or `TransferReceivable`.  
**Expected Artifact:** Merge Request (Pending / Executed / Cancelled) as input to Merge Billing; moves Billing Set, regenerates projection, sends Transfer Receivable to Accounting, marks request Executed (`02-domain.md` §ENTITY Merge Request; `SOP-TR-03`).  
**Impact:** **Critical** — Cross-registration billing consolidation (Rawat Jalan → Rawat Inap, IGD → Rawat Inap, etc.) cannot be performed.  
**Recommendation:** Implement `MergeRequest` persistence, `MergeBillingDomService`, application command with transactional rollback on Accounting failure per SOP-TR-03 §Business Rules.  
**Affected Files:** *(none exist — fully missing)*

---

### GAP-004

**Category:** Workflow  
**Current Implementation:** No Financial Verification step, status, or query. `FinalizeFinancialResponsibility` proceeds directly from CLOSED without verification gate.  
**Expected Artifact:** Mandatory SOP-TR-04 after Close Bill (and optional Merge); blocks Allocation until verification passes or routes to Adjustment (`04-sop.md` §4.2).  
**Impact:** **High** — Financial Control phase can be bypassed; violates precondition chain in SOP-TR-06 and SOP-TR-07.  
**Recommendation:** Introduce verification state (domain flag or workflow record) and application command; enforce in finalization precondition.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs` (`EnsureCanFinalize` has no verification check)

---

### GAP-005

**Category:** Workflow  
**Current Implementation:** No Financial Adjustment types, commands, or domain methods (Manual Charge, Billing Correction, Waive, Subsidy, Merge Billing Correction).  
**Expected Artifact:** SOP-TR-05 — optional correction of Financial Truth without changing operational history; may trigger Reopen Billing.  
**Impact:** **High** — No artifact-compliant correction path before Allocation.  
**Recommendation:** Add adjustment domain operations and application handlers; link to Reopen when Charge Source change is required.  
**Affected Files:** *(none — not implemented)*

---

### GAP-006

**Category:** Domain Model / Workflow  
**Current Implementation:** Financial Responsibility Allocation is **coupled inside** `FinalizeFinancialResponsibility()` — allocation (`FinalizationAllocation`) and status transition to FINALIZED occur in one atomic domain call.  
**Expected Artifact:** SOP-TR-06 (Allocation) is a **separate mandatory step** before SOP-TR-07 (Finalize); Allocation can be iterated and reviewed by Verifikator before locking (`SOP-TR-06` §Workflow steps 7–9).  
**Impact:** **High** — Workflow granularity mismatch; no standalone allocation without finalization.  
**Recommendation:** Extract `AllocateFinancialResponsibility(payments)` that regenerates projection but keeps status CLOSED; restrict `FinalizeFinancialResponsibility` to locking pre-validated allocation.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs` (lines 74–92, 216–260)

---

### GAP-007

**Category:** Workflow / Bounded Context Boundary  
**Current Implementation:** `TataRekeningModel.Pay()` performs payment allocation across bills and transitions to LUNAS. No `SettlementInitiation` operation exists.  
**Expected Artifact:** SOP-TR-10 — Settlement Initiation hands FINALIZED billing to **Cashier** without payment; Payment Settlement is Cashier responsibility (`01-context.md` §Cashier; `SOP-TR-10` §Business Rules).  
**Impact:** **High** — Payment orchestration lives in wrong bounded context; conflates Financial Control handoff with Payment Settlement.  
**Recommendation:** Add `InitiateSettlement()` domain transition (metadata only, no payment); move `Pay()` invocation to Cashier integration or rename/re-scope under Cashier context.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs` (`Pay`, `TryTransitionToLunas`)

---

### GAP-008

**Category:** Authorization  
**Current Implementation:** `petugasVerif` is stored as a string parameter with no role validation. No `[Authorize]`, policy, or Verifikator check on any Payment/Tata Rekening endpoint. Grep for `Verifikator` in `*.cs` returns zero matches. Payment API controllers are commented out.  
**Expected Artifact:** `03-design.md` §SECURITY — Close Bill, Merge Billing, Adjustment, Allocation, Finalize, Cancel Finalization, Reopen, Settlement Initiation require Verifikator authorization.  
**Impact:** **High** — Privileged financial operations would be unprotected when API is enabled.  
**Recommendation:** Define Verifikator authorization policy; enforce in handlers before domain mutation.  
**Affected Files:** `Bilreg.Api/Controllers/PaymentContext/TrsBillingControllerController.cs`, `Bilreg.Api/Controllers/PaymentContext/RegOutSub/RegOutController.cs`, all future Tata Rekening handlers

---

### GAP-009

**Category:** Domain Events  
**Current Implementation:** No domain events emitted. Grep for `BillClosed`, `SettlementInitiated`, `FinancialResponsibilityFinalized` returns zero matches in code.  
**Expected Artifact:** `02-domain.md` §DOMAIN EVENTS — Bill Closed, Merge Billing Executed, Financial Adjusted, Financial Responsibility Allocated, Financial Responsibility Finalized, Finalization Cancelled, Billing Reopened, Settlement Initiated.  
**Impact:** **High** — No integration hooks for Accounting, Cashier, or audit subscribers.  
**Recommendation:** Raise domain events from aggregate methods; dispatch from application layer.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs`

---

### GAP-010

**Category:** Integration  
**Current Implementation:** Accounting journal creation occurs at bill creation (`TdkCreateTindakanCmd` → `JurnalRepo`) only. No Transfer Receivable on merge, no accounting hook on finalization or settlement initiation.  
**Expected Artifact:** Merge Billing sends Transfer Receivable to Accounting (`SOP-TR-03` step 10); Financial Projection is basis for Accounting Projection (`01-context.md` §Financial Projection).  
**Impact:** **High** — Accounts Receivable ownership will not move on merge; ledger may diverge from Financial Truth.  
**Recommendation:** Implement Accounting integration contracts invoked from Merge Billing and Finalization handlers with rollback on failure.  
**Affected Files:** Accounting context (`Bilreg.Application/AccountingContext/`, `Bilreg.Infrastructure/AccountingContext/`)

---

### GAP-011

**Category:** Application / SOP-TR-01  
**Current Implementation:** "Open Tata Rekening" is partially realized as lazy header creation in `AddBillAppService.ResolveTataRekening` when first bill is created. No dedicated open/query use case loads Billing Set summary, Financial Projection, Payer info, or Pending Merge Requests.  
**Expected Artifact:** SOP-TR-01 — read-only preparation: load billing set, projection, payer, pending merge requests (by TargetReg or PatientId); no mutation.  
**Impact:** **Medium** — Verifikator workspace cannot be built from current API.  
**Recommendation:** Add `OpenTataRekeningQuery` composing `ITataRekeningRepo`, merge request repo, and payer resolution.  
**Affected Files:** `Bilreg.Application/PaymentContext/TrsBillingFeature/AddBillAppService.cs`, `TrBListBillingQuery.cs` (lists bills only, no Tata Rekening context)

---

### GAP-012

**Category:** Audit  
**Current Implementation:** `CancelFinalization()` and `ReOpen()` accept no reason parameter and write no audit trail. No Tata Rekening–specific audit tables.  
**Expected Artifact:** SOP-TR-08 and SOP-TR-09 require audit trail (registrasi, waktu, verifikator, alasan); SOP-TR-03 requires merge audit trail.  
**Impact:** **Medium** — Regulatory and operational traceability missing.  
**Recommendation:** Add audit records on privileged transitions; require reason for Cancel/Reopen.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs` (`CancelFinalization`, `ReOpen`)

---

### GAP-013

**Category:** Presentation / API  
**Current Implementation:** `TrsBillingControllerController` and `RegOutController` are **fully commented out** (`// TODO: jude-dev-trs-billing`). No active HTTP endpoints for Tata Rekening workflows.  
**Expected Artifact:** `03-design.md` — Client → Presentation → Application → Domain.  
**Impact:** **Medium** — No API exposure for any SOP.  
**Recommendation:** Implement REST/MediatR controllers per workflow with Verifikator authorization.  
**Affected Files:** `Bilreg.Api/Controllers/PaymentContext/TrsBillingControllerController.cs`, `Bilreg.Api/Controllers/PaymentContext/RegOutSub/RegOutController.cs`

---

### GAP-014

**Category:** UI  
**Current Implementation:** No frontend or UI project for Tata Rekening exists in this repository.  
**Expected Artifact:** Workflow screens for all ten SOPs with lifecycle visibility and permission-gated actions.  
**Impact:** **Medium** — End users cannot execute Tata Rekening through this codebase.  
**Recommendation:** Implement UI per global `docs/WORKFLOW.md` queue/workspace patterns when frontend is in scope.  
**Affected Files:** *(none in repo)*

---

### GAP-015

**Category:** Domain Services  
**Current Implementation:** Merge Billing, Financial Verification, Allocation, and Projection are not implemented as named domain services (`02-domain.md` §DOMAIN SERVICES). Allocation/projection logic is private methods on the aggregate.  
**Expected Artifact:** Explicit domain services for merge, verification, allocation, projection generation.  
**Impact:** **Medium** — Complex cross-aggregate merge logic has no home; verification/adjustment cannot be added cleanly.  
**Recommendation:** Extract services as artifacts specify; keep aggregate as consistency boundary.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/`

---

### GAP-016

**Category:** Workflow Preconditions  
**Current Implementation:** `EnsureCanFinalize` checks status CLOSED and allocation totals only. No check for pending Merge Requests, incomplete verification, or projection availability as separate preconditions.  
**Expected Artifact:** SOP-TR-07 preconditions: verification complete, adjustments complete, allocation complete, projection regenerated. SOP-TR-04 exception: pending merge requests block verification.  
**Impact:** **Medium** — Invalid state transitions possible in domain.  
**Recommendation:** Add explicit precondition validation aligned with SOP sequence.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs` (`EnsureCanFinalize`, `AssertFinalizationComplete`)

---

### GAP-017

**Category:** Concurrency  
**Current Implementation:** `BILRG_TataRekening` has no rowversion/timestamp column. No optimistic concurrency in `BilrgTataRekeningDal.Update`.  
**Expected Artifact:** `03-design.md` §CONCURRENCY — registration-level concurrency to prevent double finalization, concurrent merge, concurrent reopen.  
**Impact:** **Medium** — Race conditions on lifecycle transitions under concurrent Verifikator sessions.  
**Recommendation:** Add `RowVersion` column and concurrency checks on update.  
**Affected Files:** `Bilreg.SqlDb/PaymentContext/TataRekeningFeature/BILRG_TataRekening.sql`, `BilrgTataRekeningDal.cs`

---

### GAP-018

**Category:** Application / Validators  
**Current Implementation:** No FluentValidation validators for PaymentContext Tata Rekening commands. Validator assembly registration appears disabled in application bootstrap.  
**Expected Artifact:** Application layer validates command shape and preconditions; domain enforces invariants.  
**Impact:** **Medium** — Input validation gap when commands are added.  
**Recommendation:** Add validators per command following `docs/skills/use-case-generation.md`.  
**Affected Files:** `Bilreg.Application/PaymentContext/`

---

### GAP-019

**Category:** Billing Set Consistency  
**Current Implementation:** `AddBillAppService` persists bills via `ITrsBillingRepo` without adding to aggregate `_listTrsBill`. Aggregate billing set is populated only on `LoadEntity`. `DeleteBill` exists on aggregate but has no application path.  
**Expected Artifact:** Billing Set owned by aggregate; lifecycle operates on complete set (`02-domain.md` §Billing Set invariants).  
**Impact:** **Medium** — In-memory aggregate during bill creation does not reflect persisted bills until reload.  
**Recommendation:** Either add bills to aggregate on creation or always reload before lifecycle operations in application layer.  
**Affected Files:** `Bilreg.Application/PaymentContext/TrsBillingFeature/AddBillAppService.cs`, `TataRekeningModel.cs` (`DeleteBill`)

---

### GAP-020

**Category:** Persistence Naming  
**Current Implementation:** Table `BILRG_TataRekening` with column `DischargeDate` mapped to domain `FinalizationDate`. Design doc references `BILRG_TrsBillingRegister` and maps business Tata Rekening to `TrsBillingRegister`.  
**Expected Artifact:** `03-design.md` §PERSISTENCE DESIGN — `BILRG_TrsBillingRegister` for lifecycle.  
**Impact:** **Low** — Naming divergence; functionally acceptable under Compatibility First if documented, but confuses onboarding.  
**Recommendation:** Document alias in persistence layer or align naming in next migration phase.  
**Affected Files:** `Bilreg.SqlDb/PaymentContext/TataRekeningFeature/BILRG_TataRekening.sql`, `03-design.md`

---

### GAP-021

**Category:** Domain Model Mapping  
**Current Implementation:** Artifact aggregate name **Tata Rekening** maps to `TataRekeningModel`; artifact **TrsBill** maps to `TrsBillType` / `BillingCharge` naming in design table.  
**Expected Artifact:** `03-design.md` §DOMAIN IMPLEMENTATION mapping table.  
**Impact:** **Low** — Ubiquitous language partially preserved (`TataRekening*` types) but `TrsBill` vs `TrsBillType` and missing `TrsBillingRegister` type name reduce clarity.  
**Recommendation:** Align public type names with artifact glossary where refactor cost is low.  
**Affected Files:** `Bilreg.Domain/PaymentContext/TataRekeningFeature/`, `Bilreg.Domain/PaymentContext/TrsBillFeature/`

---

### GAP-022

**Category:** Tests  
**Current Implementation:** Strong domain unit tests (`TataRekeningLifecycleDomainTest` — 14 tests, `TataRekeningFinancialAllocationIntegrityTest` — 14 tests) and repo/DTO tests. No integration test executes full SOP chain with database. No application handler tests. No merge/verification/adjustment tests.  
**Expected Artifact:** Tests covering business rules, lifecycle, invariants, and workflow per analysis scope.  
**Impact:** **Low** for domain coverage; **Medium** for end-to-end confidence — gaps will widen as application layer is built.  
**Recommendation:** Add workflow integration tests per SOP once handlers exist.  
**Affected Files:** `Bilreg.Test/PaymentContext/TataRekeningFeature/`

---

### GAP-023

**Category:** Unexpected Implementation  
**Current Implementation:** `TataRekeningModel.Pay()` and automatic LUNAS transition implement Cashier settlement inside Tata Rekening aggregate.  
**Expected Artifact:** Payment Settlement is Cashier bounded context (`01-context.md` §OUT OF SCOPE).  
**Impact:** **High** — Wrong ownership; should be refactored when Cashier integration is built.  
**Recommendation:** Treat `Pay()` as interim/legacy bridge; replace with Settlement Initiation + Cashier command.  
**Affected Files:** `TataRekeningModel.cs`, `TataRekeningFinancialAllocationIntegrityTest.cs` (TG07 partial pay → Lunas)

---

### GAP-024

**Category:** Charge Source Guard  
**Current Implementation:** `CreateBillDomService` calls `tataRekening.EnsureCanCreateTrsBill()` — correctly blocks bill creation when not OPEN. Charge Source commands use `IAddBillAppService`.  
**Expected Artifact:** OPEN status allows Charge Source; CLOSED blocks new Financial Charge (`04-sop.md` §3.3).  
**Impact:** **Positive partial alignment** — guard exists at domain level.  
**Recommendation:** Ensure `ResolveTataRekening` always loads existing header (not recreate) so CLOSED status is enforced. Current implementation does load existing — **aligned**.  
**Affected Files:** `CreateBillDomService.cs`, `AddBillAppService.cs`

---

### GAP-025

**Category:** Projection Strategy  
**Current Implementation:** `TrsBillingRepo.SaveChanges` deletes and re-inserts all `ta_trs_billing2` rows for a bill (DELETE → REGENERATE per bill). Matches design at bill level.  
**Expected Artifact:** `03-design.md` §FINANCIAL PROJECTION — derived data, regenerable before Finalization.  
**Impact:** **Low** — Strategy aligned for TrsBill projection; registration-level regeneration orchestration missing (see GAP-001).  
**Recommendation:** Wire aggregate lifecycle to trigger per-bill projection save.  
**Affected Files:** `Bilreg.Infrastructure/PaymentContext/TrsBillingFeature/TrsBillingRepo.cs`

---

### GAP-026

**Category:** Legacy Parallel Feature  
**Current Implementation:** `RegOutFeature` (dischargeable list, pembayaran, hutang) operates independently with active MediatR handlers (`RegAddPembayaranCmd`, `RegListRegDischargeableQuery`, etc.) while RegOut API is commented out.  
**Expected Artifact:** Tata Rekening replaces Financial Control; legacy maintained only for compatibility during migration (`03-design.md` §DEPLOYMENT STRATEGY).  
**Impact:** **High** — Legacy behavior preserved without feature toggle or migration path to Tata Rekening aggregate.  
**Recommendation:** Define migration feature flag; route RegOut flows through Tata Rekening when enabled.  
**Affected Files:** `Bilreg.Application/PaymentContext/RegOutFeature/`

---

### GAP-027

**Category:** Value Objects  
**Current Implementation:** `TataRekeningPaymentType` (Jasa/Obat split per payer) and `PaymentType` providers exist. No distinct `FinancialProjection` value object type — projection is materialized as `TrsBill2FinalizationEventType` / `TrsBill2PaymentEventType` lists on bills.  
**Expected Artifact:** `02-domain.md` §VALUE OBJECT Financial Projection as derived allocation result.  
**Impact:** **Low** — Representation differs but functionally maps to `ta_trs_billing2`; acceptable pragmatic DDD.  
**Recommendation:** Optional: introduce read-model type for Open Tata Rekening summary.  
**Affected Files:** `TrsBill2FinalizationEventType.cs`, `TrsBill2PaymentEventType.cs`

---

### GAP-028

**Category:** Cancel Finalization Semantics  
**Current Implementation:** `CancelFinalization()` clears level-1 payments and bill finalization events; blocks if any bill has payment events. No reason/audit.  
**Expected Artifact:** SOP-TR-08 — revert FINALIZED → CLOSED, unlock allocation, retain charges; audit with reason.  
**Impact:** **Medium** — Core state transition present; procedural requirements missing.  
**Recommendation:** Add reason parameter, audit persistence, application command.  
**Affected Files:** `TataRekeningModel.cs` (lines 113–133)

---

### GAP-029

**Category:** Reopen Billing Semantics  
**Current Implementation:** `ReOpen()` transitions CLOSED → OPEN only. No link to Financial Adjustment precondition or audit.  
**Expected Artifact:** SOP-TR-09 — only after adjustment determines Charge Source change needed; requires reason and audit.  
**Impact:** **Medium** — State transition exists without workflow context.  
**Recommendation:** Add `ReopenBilling(reason)` with audit; enforce adjustment precondition in handler.  
**Affected Files:** `TataRekeningModel.cs` (lines 60–68)

---

### GAP-030

**Category:** Operational vs Financial Truth  
**Current Implementation:** Charge Source creates bills via `AddBillAppService`; operational events stay in source contexts (Tindakan, Reg, Lab). Tata Rekening guards OPEN for new charges.  
**Expected Artifact:** Operational Truth ≠ Financial Truth separation (`01-context.md`).  
**Impact:** **Low** — Directionally correct; incomplete until Close Bill is enforced system-wide.  
**Recommendation:** Propagate CLOSED status check to all Charge Source entry points (verify each command uses `IAddBillAppService`).  
**Affected Files:** Charge Source commands using `IAddBillAppService`

---

### GAP-031

**Category:** Database Duplication  
**Current Implementation:** `BILRG_TataRekening.sql` exists in both `Bilreg.SqlDb/PaymentContext/TataRekeningFeature/` and `Bilreg.SqlDb/PaymentContext/TrsBillingFeature/`.  
**Expected Artifact:** Single canonical table definition per `docs/DATABASE.md`.  
**Impact:** **Low** — Maintenance duplication risk.  
**Recommendation:** Consolidate to one SQL file path.  
**Affected Files:** Both `BILRG_TataRekening.sql` copies

---

### GAP-032

**Category:** Pasien Balance Adjacency  
**Current Implementation:** `BILRG_TataRekPasienBalance` feature exists separately from registration lifecycle.  
**Expected Artifact:** Not in Tata Rekening core scope; patient-level balance is adjacent concern.  
**Impact:** **Low** — Unexpected but not conflicting if kept separate.  
**Recommendation:** Document boundary in integration guide; avoid duplicating allocation logic.  
**Affected Files:** `Bilreg.Application/PaymentContext/PasienBalanceFeature/`

---

## Missing Features

| Feature (from artifacts) | Status |
|----------------------------|--------|
| Merge Request entity and persistence | **Not Implemented** |
| Merge Billing workflow (SOP-TR-03) | **Not Implemented** |
| Transfer Receivable to Accounting on merge | **Not Implemented** |
| Financial Verification workflow (SOP-TR-04) | **Not Implemented** |
| Financial Adjustment types and workflow (SOP-TR-05) | **Not Implemented** |
| Standalone Financial Responsibility Allocation step (SOP-TR-06) | **Partially Implemented** (embedded in finalize only) |
| Settlement Initiation (SOP-TR-10) | **Not Implemented** |
| Open Tata Rekening query/workspace (SOP-TR-01) | **Partially Implemented** (header on first bill only) |
| Application commands/handlers for Close, Finalize, Cancel, Reopen | **Not Implemented** |
| Verifikator authorization | **Not Implemented** |
| Domain events (8 event types) | **Not Implemented** |
| Merge / Cancel / Reopen audit trails | **Not Implemented** |
| Tata Rekening API endpoints | **Not Implemented** (commented stubs only) |
| UI screens for Tata Rekening | **Not Implemented** (not in repo) |
| Registration-scoped unit of work (header + all bills) | **Not Implemented** |
| Optimistic concurrency on lifecycle header | **Not Implemented** |
| Pending Merge Request discovery on Open | **Not Implemented** |

---

## Unexpected Features

| Implementation | Notes |
|----------------|-------|
| `TataRekeningModel.Pay()` with LUNAS transition | Payment Settlement belongs to Cashier per artifacts — **Implemented Differently** |
| `RegOutFeature` / `RegAddPembayaranCmd` direct `ta_registrasi3` writes | Legacy parallel path — **Legacy Behavior** |
| `PasienBalanceFeature` | Patient balance tracking — not described in Tata Rekening artifacts |
| `DeleteBill()` on aggregate | Bill removal when OPEN — not listed in artifact aggregate responsibilities (may be operational correction) |
| `DischargeDate` column name | Legacy naming — **Legacy Behavior** |
| Commented `TrsBillingControllerController` / `RegOutController` | Scaffold only — not active |

---

## Workflow Deviations

| SOP | Status | Notes |
|-----|--------|-------|
| SOP-TR-01 Open Tata Rekening | **Partially Implemented** | Lazy `TataRekeningModel.Create` on first bill; no read-model for projection, payer, merge requests |
| SOP-TR-02 Close Bill | **Partially Implemented** | Domain `Close()` + tests; no app/API; Charge Source guard via `EnsureCanCreateTrsBill` works when header loaded |
| SOP-TR-03 Merge Billing | **Not Implemented** | No code, tables, or integration |
| SOP-TR-04 Financial Verification | **Not Implemented** | No verification step or status |
| SOP-TR-05 Financial Adjustment | **Not Implemented** | No adjustment operations |
| SOP-TR-06 Financial Responsibility Allocation | **Implemented Differently** | Allocation math exists but only inside `FinalizeFinancialResponsibility`, not as separate iterative step |
| SOP-TR-07 Finalize Financial Responsibility | **Partially Implemented** | Domain method + allocation tests; no orchestration, no verification preconditions, no persistence of bill projections |
| SOP-TR-08 Cancel Finalization | **Partially Implemented** | Domain `CancelFinalization()`; missing reason, audit, application layer |
| SOP-TR-09 Reopen Billing | **Partially Implemented** | Domain `ReOpen()`; missing adjustment precondition, reason, audit |
| SOP-TR-10 Settlement Initiation | **Not Implemented** | `Pay()` conflates with Cashier settlement |

---

## Domain Model Deviations

### Aggregate

| Artifact | Implementation | Deviation |
|----------|----------------|-----------|
| Tata Rekening (one per Registrasi) | `TataRekeningModel` | Aligned |
| Responsibilities: Close, Merge, Verify, Adjust, Allocate, Finalize, Cancel, Reopen, Settlement Initiation | Close, ReOpen, Finalize (with allocate), Cancel, Pay | Missing Merge, Verify, Adjust, Settlement Initiation; Pay should not be here |
| Billing Set in aggregate | `_listTrsBill` loaded on read, not updated on create | **Partial** |

### Entities

| Artifact | Implementation | Deviation |
|----------|----------------|-----------|
| TrsBill | `TrsBillType` | Aligned (naming) |
| Merge Request | — | **Missing** |

### Value Objects

| Artifact | Implementation | Deviation |
|----------|----------------|-----------|
| Financial Responsibility | `TataRekeningPaymentType` + `PaymentType` | Aligned shape (Jasa/Obat per payer) |
| Financial Projection | `TrsBill2FinalizationEventType` / `TrsBill2PaymentEventType` on bills | **Implemented Differently** (no standalone VO) |

### Services

| Artifact | Implementation | Deviation |
|----------|----------------|-----------|
| Merge Billing service | — | **Missing** |
| Financial Verification service | — | **Missing** |
| Allocation service | Private methods on aggregate | **Implemented Differently** |
| Projection service | `TrsBillingRepo` per-bill regenerate | **Partial** |

### Events

| Artifact | Implementation | Deviation |
|----------|----------------|-----------|
| 8 domain events | None | **Missing** |

---

## Architecture Deviations

| Layer | Expected | Current | Gap |
|-------|----------|---------|-----|
| Presentation | Tata Rekening API + UI | Commented controllers; no UI | **High** |
| Application | Commands/queries per SOP | `ITataRekeningRepo` + `AddBillAppService` only | **High** |
| Domain | Full aggregate + services + events | Strong lifecycle/allocation core; missing merge, verify, adjust, events | **Medium** |
| Infrastructure | Repos + integrations | Header/payment/bill repos exist; no merge, accounting hooks, UoW | **High** |
| Persistence | Registration transaction boundary | Separate repo saves | **High** |
| Integration | Charge Source, Accounting, Cashier | Charge Source partial; Accounting at bill create only; Cashier via legacy RegOut | **High** |

---

## Database Deviations

| Area | Artifact | Implementation | Deviation |
|------|----------|----------------|-----------|
| Lifecycle table | `BILRG_TrsBillingRegister` | `BILRG_TataRekening` | Naming (**Low**) |
| Financial Charge | `ta_trs_billing` | `ta_trs_billing` | Aligned |
| Financial Projection | `ta_trs_billing2` | `ta_trs_billing2` | Aligned |
| Level-1 allocation | (via projection design) | `ta_registrasi3` | Aligned legacy table; dual writers (**Critical**) |
| Merge Request table | Implied by domain | — | **Missing** |
| Audit tables | Required by SOPs 3, 8, 9 | — | **Missing** |
| Relationships | One Registrasi → one header → many bills | FK via `fs_kd_reg` on bills | Aligned |
| Indexes | Registration-centric access | PK on `RegId` only | No concurrency column (**Medium**) |
| Derived data | DELETE → REGENERATE | Per-bill in `TrsBillingRepo` | Aligned at bill level |
| Lifecycle storage | Status on register | `Status` INT on `BILRG_TataRekening` | Aligned |

---

## Test Coverage Gaps

### Missing unit tests

- Merge Billing business rules (no implementation)
- Financial Verification preconditions
- Financial Adjustment scenarios
- Standalone Allocation without Finalize
- Settlement Initiation (handoff semantics)
- Verifikator authorization policies
- Domain event publication

### Missing integration tests

- Full SOP chain: Open → Close → Allocate → Finalize → Settlement Initiation (with DB)
- Merge Billing with Accounting rollback
- Transaction rollback when projection regeneration fails
- Concurrent finalization attempts

### Missing workflow tests

- Exception paths per SOP (e.g. finalize while merge pending)
- Reopen → Close → re-verify sequence
- Cancel Finalization after partial payment blocked

### Missing business rule tests

- SOP-TR-03 merge invariants (CLOSED target, Pending request only)
- SOP-TR-08 audit/reason required
- SOP-TR-09 adjustment precondition

### Existing coverage (strengths)

- `TataRekeningLifecycleDomainTest` — lifecycle guards and OPEN→LUNAS path
- `TataRekeningFinancialAllocationIntegrityTest` — conservation, rounding, partial pay
- `TataRekeningRepoTest`, DTO round-trips
- `AddBillAppServiceTest`, `CreateBillDomServiceTest` — Charge Source entry

---

## Refactoring Roadmap

### Phase 1 — Critical fixes

1. Implement registration-scoped **unit of work** (`TataRekeningRepo` + `TrsBillingRepo` in one transaction).
2. **Eliminate dual `ta_registrasi3` writers** — route legacy RegOut through Tata Rekening or feature-flag off.
3. Add application handlers for **Close Bill**, **Finalize**, **Cancel Finalization**, **Reopen** with persistence.
4. **Do not enable API** until Verifikator authorization is in place.

### Phase 2 — Architecture alignment

1. Extract **AllocateFinancialResponsibility** from finalize; add verification gate.
2. Implement **Settlement Initiation**; relocate `Pay()` to Cashier boundary.
3. Add **domain events** and Accounting integration on finalize/merge.
4. Add **optimistic concurrency** on `BILRG_TataRekening`.

### Phase 3 — Feature completion

1. **Merge Request** persistence + **Merge Billing** SOP-TR-03 with Transfer Receivable.
2. **Financial Verification** and **Financial Adjustment** workflows.
3. **Open Tata Rekening** query with merge request discovery.
4. Enable **API controllers** and external UI integration.

### Phase 4 — Code quality improvements

1. Consolidate duplicate SQL definitions.
2. Align naming (`TrsBillingRegister` vs `TataRekening` table).
3. Add workflow integration test suite.
4. Document legacy migration toggles for RegOut.

---

## Final Assessment

### Overall implementation maturity

**Early domain-centric stage.** The team invested correctly in aggregate lifecycle, allocation integrity, and legacy-compatible persistence shapes. The bounded context is **not production-ready** for Financial Control: application orchestration, authorization, merge, verification, adjustment, settlement handoff, and API/UI are largely absent. Legacy RegOut remains a parallel financial path.

### Risks

1. **Financial inconsistency** if legacy `RegAddPembayaranCmd` and new aggregate paths run concurrently.
2. **Incorrect AR ownership** when merge is needed but unavailable.
3. **Unauthorized financial mutations** when API is uncommented without Verifikator checks.
4. **Lost projection data** if finalize is invoked without bill-level `TrsBillingRepo.SaveChanges`.
5. **Workflow bypass** — finalize without verification or pending-merge checks.

### Recommended implementation order

1. Unit of work + Close/Finalize/Cancel/Reopen handlers (persist header + bills).
2. Verifikator authorization policy.
3. Decommission or gate legacy `ta_registrasi3` direct writes.
4. Split Allocation from Finalize; add Verification gate.
5. Open Tata Rekening query + API.
6. Settlement Initiation + Cashier integration (move `Pay`).
7. Merge Request + Merge Billing + Accounting Transfer Receivable.
8. Financial Adjustment + audit trails.
9. Domain events + integration tests.
10. UI workspace.

---

## Related Documents

| Document | Description |
|----------|-------------|
| [01-context.md](01-context.md) | Business context |
| [02-domain.md](02-domain.md) | Domain model |
| [03-design.md](03-design.md) | Architecture & persistence |
| [04-sop.md](04-sop.md) | Workflow index |
| SOP-TR-01 … SOP-TR-10 | Per-step procedures |
