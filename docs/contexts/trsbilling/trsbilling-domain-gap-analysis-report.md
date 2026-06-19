# TRSBILLING Domain Gap Analysis Report

**Audit date:** 2026-06-16  
**Source of truth:** [`trsbilling-02-domain.md`](trsbilling-02-domain.md)  
**Implementation scope (domain layer only):** `Bilreg.Domain/PaymentContext/TataRekeningFeature`, `Bilreg.Domain/PaymentContext/TrsBillFeature`, `Bilreg.Domain/PaymentContext/CoaFeature`  
**Out of scope:** Infrastructure, repositories, application orchestration, API, persistence, legacy migration

---

# Executive Summary

**Overall assessment: Moderate Alignment**

The TRSBILLING domain layer has the correct **core vocabulary** (`TataRekening`, `TrsBill`, three component event types, Level-1 payment shape with JASA/OBAT split) and a **partially correct lifecycle skeleton** on `TataRekeningModel` (OPEN → CLOSED → FINALIZED). `CreateBillService` and `TrsBillFactory` establish a credible financial charge snapshot at bill creation.

However, several **authoritative responsibilities are inverted or missing**:

- TataRekening does not own payment orchestration, cancel-discharge coordination, bill deletion, or the LUNAS terminal transition.
- `TrsBillType` exposes public `Pay()` and `CancelDischarge()` even though the artifact assigns those operations to TataRekening scope over the Billing Set.
- Payment decomposition uses **transaction components** instead of **discharge components**.
- The Billing Set is represented as `_listTrsBill` but is never populated by bill creation, so lifecycle operations cannot reliably operate on the complete set.
- Module partition (JASA/OBAT) logic exists in discharge math but `Modul` is always `0` at creation, making the invariant unenforceable in practice.

The implementation is **not artifact compliant**, but it is **not a greenfield miss** either: the charge aggregate structure and discharge allocation math are a usable foundation once aggregate boundaries and missing lifecycle behaviors are corrected.

---

# Gap Findings

## GAP-001

### Artifact Expectation

> **TataRekening** owns: Close Billing, ReOpen, Discharge, Cancel Discharge, **Payment Allocation**, Lifecycle Validation.  
> CloseBilling, ReOpen, Discharge, CancelDischarge, Payment always operate against the **complete Billing Set** belonging to the TataRekening.

### Current Implementation

`TataRekeningModel` implements `Close()`, `ReOpen()`, and `Discharge()` only. There is no `Pay()`, `CancelDischarge()`, or transition to LUNAS. Payment settlement is implemented as `TrsBillType.Pay()` — a public method on the charge aggregate with no TataRekening lifecycle guard.

```143:152:Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillType.cs
    public void Pay(PaymentType payment, decimal nilai, string trsBayarId, DateTime tglBayar)
    {
        // ...
        if (Status is not (TrsBillStatusEnum.Discharged or TrsBillStatusEnum.Paid))
            throw new InvalidOperationException("Cannot pay bill unless status is Discharged or Paid.");
```

### Impact

**High** — Payment can be invoked per bill without TataRekening FINALIZED authority or Billing Set orchestration. Violates financial authority matrix and payment model (§13).

### Recommendation

Introduce TataRekening-scoped payment orchestration (domain service or aggregate method) that: (1) guards `Status == Finalized`, (2) accepts Level-1 payment input per provider/modul group, (3) distributes across the Billing Set, (4) delegates Level-2 decomposition to `TrsBill` internal methods, and (5) transitions TataRekening to LUNAS when all outstanding responsibility is settled.

---

## GAP-002

### Artifact Expectation

> Cancel Discharge: allowed only when **No Payment Exists**. Process: Remove Discharge Allocation, Regenerate Discharge Components.  
> Always operates at TataRekening scope against the Billing Set.

### Current Implementation

`TrsBillType.CancelDischarge()` exists as a **public** per-bill operation. It clears discharge events when no payment events exist on that bill. `TataRekeningModel` has no `CancelDischarge()` and does not revert `Status` from FINALIZED to CLOSED.

```135:141:Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillType.cs
    public void CancelDischarge()
    {
        if (_listTrsBill2PaymentEvent.Count > 0)
            throw new InvalidOperationException("Cannot cancel discharge with existing payments.");

        _listTrsBill2DischargeEvent.Clear();
    }
```

### Impact

**High** — Cancel discharge is not coordinated across the Billing Set; TataRekening lifecycle state can remain FINALIZED while individual bills are partially cleared.

### Recommendation

Add `TataRekeningModel.CancelDischarge(IEnumerable<TrsBillType> billingSet)` (or equivalent domain service) that validates no payment exists on any bill, clears discharge components on all bills, clears Level-1 allocation records, and transitions FINALIZED → CLOSED.

---

## GAP-003

### Artifact Expectation

> TataRekening contains the **Billing Set** — all TrsBill participating in the patient financial lifecycle.  
> Bill creation may occur independently without loading the entire Billing Set, but lifecycle operations require the complete set.

### Current Implementation

`TataRekeningModel` holds `_listTrsBill` and exposes `ListTrsBill`, but `CreateBillService` creates a `TrsBillType` without registering it on the aggregate. No `AddBill`, `RemoveBill`, or Billing Set management API exists.

```29:41:Bilreg.Domain/PaymentContext/TrsBillFeature/CreateBillService.cs
public sealed class CreateBillService : ICreateBillService
{
    public TrsBillType FromReg(...)
    {
        ValidateReg(tataRekening, reg.RegId);
        return TrsBillFactory.CreateFromReg(...);
    }
```

`Discharge()` operates only on bills already present in `_listTrsBill` at construction/load time.

### Impact

**High** — Lifecycle operations cannot guarantee completeness of the Billing Set. Discharge invariant (Σ Allocation = Σ Outstanding Bill) may pass on a subset while omitting bills created but not loaded.

### Recommendation

Model Billing Set ownership explicitly: either (a) add aggregate methods to register/deregister bills while preserving independent creation scope, or (b) require lifecycle domain services to receive the full Billing Set as an argument with TataRekening validating completeness. Align with artifact: creation scope ≠ lifecycle scope, but lifecycle scope = full Billing Set.

---

## GAP-004

### Artifact Expectation

> Bill deletion is allowed only while **TataRekening = OPEN**. Deletion is physical removal.

### Current Implementation

No delete-bill behavior exists in `TataRekeningModel`, `TrsBillType`, or any domain service.

### Impact

**Medium** — OPEN-state bill control is incomplete; callers cannot enforce the deletion invariant through the domain model.

### Recommendation

Add `DeleteBill` control on TataRekening (or `DeleteBillService`) that guards `Status == Opened` and removes the bill from the Billing Set / authorizes physical removal.

---

## GAP-005

### Artifact Expectation

> Lifecycle: OPEN → CLOSED → FINALIZED → **LUNAS**.  
> LUNAS: all financial responsibility converted to cash settlement; no further modification allowed.

### Current Implementation

`TataRekeningStatusEnum` defines `Paid = 3` instead of `Lunas`. No code path sets `Status` to `Paid`. `TrsBillStatusEnum.Paid` is derived from payment events on individual bills, not from TataRekening lifecycle authority.

```3:9:Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningStatusEnum.cs
public enum TataRekeningStatusEnum
{
    Opened = 0,
    Closed = 1,
    Finalized = 2,
    Paid = 3
}
```

### Impact

**High** — Terminal settlement state is undefined at the financial authority aggregate. No domain rule prevents mutation after full payment.

### Recommendation

Rename `Paid` → `Lunas` (or alias with ubiquitous language alignment). Implement transition to LUNAS when Billing Set settlement is complete. Guard all mutating operations when `Status == Lunas`.

---

## GAP-006

### Artifact Expectation

> **Payment Component** is generated from **Discharge Component** using Payment Allocation.  
> Each payment component is a copy of discharge component with payment ownership and proportional settlement value.

### Current Implementation

`TrsBillType.Pay()` iterates `_listTrsBill2TransEvent` (transaction components), not `_listTrsBill2DischargeEvent` (discharge components).

```169:180:Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillType.cs
        for (var i = 0; i < _listTrsBill2TransEvent.Count; i++)
        {
            var trans = _listTrsBill2TransEvent[i];
            var share = i == _listTrsBill2TransEvent.Count - 1
                ? nilai - allocated
                : nilai * trans.Nilai / totalBase;
            // creates payment event from transaction component
```

### Impact

**High** — Payment decomposition ignores discharge responsibility structure. Breaks the three-stage component pipeline (Transaction → Discharge → Payment) defined in §9.

### Recommendation

Refactor `Pay()` to iterate matching discharge components, allocate proportionally within each payer line, and copy discharge component identity (komponen, jenis bayar, petugas) into payment events.

---

## GAP-007

### Artifact Expectation

> Allocation Partition Rule: every TrsBill belongs to module group **JASA** or **OBAT**. BPJS JASA may only distribute to JASA bills; BPJS OBAT only to OBAT bills. Domain invariant.

### Current Implementation

`TataRekeningModel.Discharge()` filters bills by `Modul == 0` (JASA) and `Modul == 1` (OBAT). However `TrsBillFactory` always sets `Modul = 0` for both registration and tindakan bills.

```57:58:Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillFactory.cs
        var result = new TrsBillType(reg.RegId, 0, reg.RegDate.ToDateTime(...),
```

```115:116:Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillFactory.cs
        var result = new TrsBillType(tindakan.TindakanId, 0, tindakan.TindakanDate,
```

### Impact

**High** — Partition rule is structurally present but **functionally unimplemented**. All bills are JASA; OBAT allocation path is never exercised.

### Recommendation

Introduce `BillModulGroup` (or equivalent) value object with `Jasa` / `Obat` semantics. Set `Modul` correctly at bill creation based on charge source. Add domain tests enforcing cross-partition rejection.

---

## GAP-008

### Artifact Expectation

> A TrsBill cannot discharge itself, pay itself, finalize itself, reopen itself. These operations belong exclusively to TataRekening.

### Current Implementation

`Discharge()` on `TrsBillType` is `internal` (correct for Level-2 decomposition). `Pay()` and `CancelDischarge()` are **public** (incorrect). External callers can invoke bill-level lifecycle behavior without TataRekening authority.

### Impact

**High** — Aggregate boundary violation; responsibility inversion between financial authority and financial charge.

### Recommendation

Make `Pay()` and `CancelDischarge()` internal. Expose lifecycle only through TataRekening or dedicated domain services (`DischargeBillService`, `PaymentBillService`) that coordinate the Billing Set.

---

## GAP-009

### Artifact Expectation

> **OPEN**: create bill, delete bill, modify operational billing source.  
> **CLOSED**: no new bill may be created.  
> **FINALIZED** / **LUNAS**: no bill creation.

### Current Implementation

`EnsureCanCreateTrsBill()` blocks creation only when `Status == Closed`. Creation is allowed when `Opened`, `Finalized`, and `Paid`.

```114:119:Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs
    public void EnsureCanCreateTrsBill()
    {
        if (Status == TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(...);
    }
```

### Impact

**Medium** — Bills can be created after discharge (FINALIZED) or settlement (PAID), violating lifecycle freeze semantics.

### Recommendation

Guard bill creation with `Status == Opened` only. Reject create/delete for Closed, Finalized, and Lunas.

---

## GAP-010

### Artifact Expectation

> Discharge must allocate **100%** of outstanding receivable.  
> Rule: Σ Allocation = Σ Outstanding Bill — always. Partial discharge is not allowed.

### Current Implementation

`TataRekeningModel.Discharge()` validates that total JASA and OBAT payment inputs match total JASA and OBAT bill totals separately. This is a **partial** implementation of the invariant at Level-1. Per-bill discharge caps exist (`totalDischarged + nilai > totalBase`), but there is no explicit assertion that after discharge every bill's outstanding equals zero across all payers.

### Impact

**Medium** — Level-1 totals are checked; Level-2 completeness and rounding residue across bills/components are not explicitly validated.

### Recommendation

After distributing allocation across the Billing Set, assert per modul group and per bill that Σ discharge components equals bill outstanding. Fail discharge if any bill remains partially allocated.

---

## GAP-011

### Artifact Expectation

> `PaymentAllocation` on TataRekening distributes payment provider responsibility to TrsBill (Level 1).  
> TrsBill distributes bill share to components (Level 2).

### Current Implementation

`TataRekeningModel` has a private method named `PaymentAllocation()` that is invoked from `Discharge()` and performs **discharge** distribution to bills — not payment settlement.

```91:112:Bilreg.Domain/PaymentContext/TataRekeningFeature/TataRekeningModel.cs
    private void PaymentAllocation()
    {
        // distributes TataRekeningPaymentType to bills via item2.Discharge(...)
    }
```

### Impact

**Medium** — Misleading ubiquitous language conflates discharge allocation with payment allocation. Increases risk of incorrect future changes.

### Recommendation

Rename to `DischargeAllocation()` or extract to `DischargeAllocationCalculator`. Introduce separate payment allocation logic when payment orchestration is implemented.

---

## GAP-012

### Artifact Expectation

> TrsBill stores pricing snapshot and accounting snapshot as **immutable**.  
> Component snapshot resolved at transaction time; immutable.

### Current Implementation

`TrsBillType` uses mutable `List<>` collections for all three event types. No immutability enforcement after creation. `TrsBillNilaiType` and `TrsBill2CoaType` are records but can be replaced if the aggregate is reconstructed.

### Impact

**Low** — Conceptual model allows mutation paths that contradict financial truth immutability.

### Recommendation

Freeze transaction components after creation. Restrict mutation to append-only discharge/payment event lists via controlled internal methods. Consider read-only exposure of transaction components.

---

## GAP-013

### Artifact Expectation

> Ubiquitous language: **TrsBill**, **Billing Set**, **LUNAS**, **OPEN** / **CLOSED** / **FINALIZED**.

### Current Implementation

| Artifact term | Code term | Notes |
|---------------|-----------|-------|
| TrsBill | `TrsBillType`, `TrsBillingId` | Mixed TrsBill / TrsBilling naming |
| Billing Set | `_listTrsBill` / `ListTrsBill` | No explicit Billing Set type or term |
| LUNAS | `Paid` | Terminology mismatch |
| OPEN | `Opened` | Minor naming drift |
| Financial Charge | `TrsBillType` | Acceptable |
| Transaction Component | `TrsBill2TransEventType` | Persistence-oriented `TrsBill2` prefix |
| Discharge Component | `TrsBill2DischargeEventType` | Same |
| Payment Component | `TrsBill2PaymentEventType` | Same |

### Impact

**Low** — Readability and onboarding friction; `TrsBill2*` naming signals table-driven modeling.

### Recommendation

Introduce type aliases or rename toward domain language (`TransactionComponent`, `DischargeComponent`, `PaymentComponent`) when refactoring. Align status enum names with artifact states.

---

## GAP-014

### Artifact Expectation

> Domain services where cross-aggregate orchestration is required. Examples: CreateBillService, allocation logic, payment allocation.

### Current Implementation

| Expected service | Status |
|------------------|--------|
| `CreateBillService` | Present and aligned for creation guards |
| Discharge allocation orchestration | Embedded in `TataRekeningModel.Discharge()` |
| Payment allocation orchestration | Missing |
| `DeleteBillService` | Missing |
| Dedicated allocation calculators | Missing |

### Impact

**Medium** — Orchestration logic is split between aggregate private methods and public TrsBill APIs without a consistent domain service pattern.

### Recommendation

Extract `DischargeBillService` and `PaymentBillService` with calculators for Level-1 distribution. Keep `CreateBillService`. Keep TataRekening as lifecycle authority with guards and state transitions.

---

## GAP-015

### Artifact Expectation

> TRSBILLING owns financial truth. Operational subsystems own operational truth. Domain model reflects business concepts, not table structure.

### Current Implementation

- `TrsBill2*` naming mirrors legacy `trs_billing_2` table decomposition.
- `CoaType` lives under `TrsBillingFeature` namespace in some imports (legacy namespace coupling).
- Parallel legacy models in `RegOutFeature` (`RegBiayaType`, `RegHutangType`, `RegPembayaranType`) represent overlapping registration discharge/payment concepts outside the TrsBill/TataRekening model.
- `Modul` as `int` magic numbers `0`/`1` instead of domain enum `Jasa`/`Obat`.
- `TrsBillStatusEnum` provides a derived per-bill status parallel to TataRekening lifecycle, which can confuse authority (bill appears Paid while TataRekening is not Lunas).

### Impact

**Medium** — Persistence and legacy shapes leak into domain language; dual models risk inconsistent financial truth.

### Recommendation

Domain refactor should treat `TrsBill2*EventType` as component value objects with domain names. Deprecate `RegOutFeature` financial types in favor of TRSBILLING aggregates (domain-only concern; no infra changes in this report).

---

# Missing Domain Concepts

| Concept (from artifact) | Status in code |
|-------------------------|----------------|
| **Billing Set** (explicit bounded collection concept) | Implicit list only; no type, invariant, or registration API |
| **LUNAS** terminal lifecycle state | Enum value `Paid` exists but never assigned; no behavior |
| **TataRekening Payment Allocation** (orchestrated payment settlement) | Absent |
| **TataRekening Cancel Discharge** (aggregate-wide) | Absent |
| **Delete Bill Control** | Absent |
| **BillModulGroup** (JASA / OBAT) | Magic `int Modul` without domain enum |
| **DischargeAllocationCalculator** | Logic inline in `TataRekeningModel` |
| **PaymentAllocationCalculator** | Absent |
| **Level-1 allocation state** (persisted discharge/payment responsibility records on TataRekening beyond input list) | `TataRekeningPaymentType` exists as input shape only |
| **Financial adjustment** as explicit bill category | Not modeled (may be acceptable as generic bill creation) |
| **Immutability enforcement** for pricing/accounting/component snapshots | Not enforced |

---

# Incorrect Domain Concepts

| Code concept | Artifact inconsistency |
|--------------|------------------------|
| `PaymentAllocation()` method performing discharge | Misnamed; performs discharge allocation, not payment |
| `TrsBillType.Pay()` as public API | Payment belongs to TataRekening orchestration |
| `TrsBillType.CancelDischarge()` as public API | Cancel discharge belongs to TataRekening scope |
| Payment decomposition from transaction components | Must decompose from discharge components |
| `TataRekeningStatusEnum.Paid` | Artifact term is **LUNAS** |
| `TrsBillStatusEnum` as lifecycle authority | Artifact: TrsBill cannot independently finalize/pay; status is projection only |
| `EnsureCanCreateTrsBill` blocking Closed only | Artifact: only OPEN allows create |
| `Modul = 0` for all factory-created bills | Violates JASA/OBAT partition model |
| `RegOutFeature` financial types | Parallel domain model outside TRSBILLING aggregate design |

---

# Aggregate Boundary Assessment

| Aspect | Assessment |
|--------|------------|
| TataRekening as aggregate root for lifecycle | **Partial** — Close, ReOpen, Discharge present; payment, cancel discharge, LUNAS, delete missing |
| TataRekening owns Billing Set | **Partial** — Collection exists but is not maintained on bill creation |
| TrsBill as financial charge aggregate | **Mostly correct** — charge snapshot, components, Level-2 decomposition structure |
| TrsBill excluded from independent discharge/payment | **Violated** — public Pay and CancelDischarge |
| Creation scope ≠ lifecycle scope | **Partial** — CreateBillService does not load full Billing Set (good) but also does not link bill to authority (gap) |
| Aggregate leakage | **Present** — lifecycle callable on TrsBill without TataRekening; payment ignores discharge component chain |

**Summary:** Aggregate boundaries are **conceptually drawn** but **not enforced**. TataRekening behaves as a lifecycle skeleton; TrsBill is over-scoped with public settlement APIs.

---

# Lifecycle Assessment

| Transition | Artifact | Implementation | Status |
|------------|----------|----------------|--------|
| OPEN → CLOSED | `CloseBilling()` | `Close()` from `Opened` | Implemented |
| CLOSED → OPEN | `ReOpen()` | `ReOpen()` from `Closed` | Implemented |
| CLOSED → FINALIZED | `Discharge()` | `Discharge()` from `Closed` | Implemented |
| FINALIZED → CLOSED | `CancelDischarge()` (no payment) | Not on TataRekening | Missing |
| FINALIZED → LUNAS | Payment complete | No transition | Missing |
| OPEN: create/delete bill | Allowed | Create partial; delete missing | Partial |
| CLOSED: no create | Forbidden | Create blocked | Implemented |
| FINALIZED: payment allowed | Yes | Per-bill Pay without guard | Incorrect |
| LUNAS: terminal | No mutations | Not reachable | Missing |

**Invalid transitions prevented:**

- Discharge from OPEN — **yes** (`EnsureCanDischarge`)
- Discharge from FINALIZED/PAID — **yes**
- ReOpen from non-CLOSED — **yes**
- Close from non-OPEN — **yes** (throws)

**Missing guards:**

- Payment from OPEN/CLOSED — not prevented at TataRekening level
- Bill create from FINALIZED/PAID — not prevented
- Mutation after LUNAS — not applicable (state unreachable)

---

# Invariant Assessment

| Invariant | Coverage |
|-----------|----------|
| Billing Set scope for lifecycle ops | **Partial** — discharge uses in-memory list; completeness not guaranteed |
| Σ Allocation = Σ Outstanding (discharge 100%) | **Partial** — Level-1 JASA/OBAT totals enforced; per-bill completeness not asserted |
| Payment on discharged responsibility | **Partial** — bill must be Discharged/Paid; no TataRekening FINALIZED guard |
| Cancel discharge only when no payment | **Partial** — per-bill check only |
| Bill deletion only when OPEN | **Missing** |
| JASA allocation → JASA bills only | **Partial** — logic exists; all bills created as JASA |
| OBAT allocation → OBAT bills only | **Not implemented** — no OBAT bills created |
| Pricing/accounting snapshot immutable | **Missing** — mutable collections |
| TrsBill cannot self-discharge/pay/finalize | **Violated** — public Pay/CancelDischarge |
| Partial discharge forbidden | **Partial** — input totals must match; edge rounding not validated |

---

# Final Verdict

**Classification: Partially Compliant**

**Rationale:**

The domain layer demonstrates **intentional alignment** with the artifact's two-concept model (TataRekening + TrsBill), three component types, and the OPEN → CLOSED → FINALIZED lifecycle spine. Discharge allocation math with JASA/OBAT partitioning and proportional component decomposition shows the team understood the allocation model.

However, the implementation is **not yet artifact compliant** because:

1. **Financial authority is inverted** — payment and cancel discharge are exposed on TrsBill instead of TataRekening.
2. **Billing Set ownership is incomplete** — bills are created but not registered for lifecycle scope.
3. **Terminal settlement (LUNAS) is absent** — the lifecycle cannot complete.
4. **Component pipeline is broken at payment** — payment decomposes from transaction, not discharge components.
5. **Module partition is a dead invariant** — factory always assigns JASA.
6. **Bill lifecycle guards are incomplete** — create allowed beyond OPEN; delete not implemented.

**Recommended priority order (domain layer only):**

1. Enforce aggregate boundaries (internalize TrsBill lifecycle; TataRekening orchestration).
2. Complete Billing Set registration semantics for lifecycle operations.
3. Implement payment orchestration and LUNAS transition.
4. Implement cancel discharge and delete bill with correct guards.
5. Fix payment decomposition source (discharge → payment).
6. Implement BillModulGroup at creation.
7. Align ubiquitous language (LUNAS, Billing Set, component naming).

---

## Analysis metadata

| Item | Value |
|------|-------|
| Domain files reviewed | 18 types across `TataRekeningFeature`, `TrsBillFeature`, `CoaFeature` |
| Tests referenced | `TrsBillCreationDomainServiceTest` (creation guards only) |
| Related internal doc | `trsbilling-04-domain-refactor-plan.md` (implementation plan; BD-1 therein conflicts with artifact §3 on Billing Set ownership — **artifact wins** for this report) |
