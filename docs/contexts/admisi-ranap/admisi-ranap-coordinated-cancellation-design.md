# Coordinated Cancellation Design — Rawat Inap Admission and Registration

**Date:** 12 July 2026  
**Decision status:** Revised business decision; orchestrator remains unimplemented

## Executive decision

Admission cancellation is a registration-wide administrative void, not an Admission-only status update or a physical purge. A coordinated cancellation atomically cancels Admission, voids Registration, ends active RegInap doctor assignments, removes `BILRG_RegAktif`, cancels an active Waiting List, restores the consumed Opname Request or Reservation where applicable, and appends correlated audit records.

The only downstream blocker is Tata Rekening billing activity. At least one billing item in `ta_trs_billing` for the `RegId` rejects cancellation using `REGISTRATION_HAS_BILLING_ITEMS`. No billing items permits cancellation to continue through the required Registration, Admission, Waiting List, source, and concurrency validations. An empty Tata Rekening header has no eligibility effect.

## Eligibility boundary

The eligibility read is a narrow application-facing port:

```csharp
public interface IRegistrationCancellationEligibilityRepo
{
    bool HasBillingItems(string regId);
}
```

The sole query is an existence check on `ta_trs_billing.fs_kd_reg = @RegId`. `ta_trs_billing` is Tata Rekening's Financial Charge Ledger and is authoritative for billing items. The header and `ta_trs_billing2` projection are not evidence of a blocker.

Bed occupancy, ward activity, medication, clinical documentation, transfer, tindakan, orders, laboratory, pharmacy, finalization headers, and unavailable external authorities are not cancellation eligibility dependencies. They generate neither facts, authority gates, nor blocker codes.

## State validation and transition contract

The state checks are unchanged and are not downstream eligibility checks: Admission must be cancellable and match its expected state; Registration must be active RegInap and not voided/discharged; a present Waiting List must be cancellable; RegInap and RegAktif must exist for a normal new-system cancellation; and any source must still be owned and consumed by this registration. Stale, missing, or inconsistent state uses the established conflict/not-found contracts rather than the billing blocker.

Within one ambient transaction, lock and compare-and-set the affected records in the established order. Cancel the Waiting List, end doctor assignments, void Registration, delete RegAktif, restore the source, cancel Admission, write audits, complete the idempotency record, and commit. A failed conditional write, audit, or ledger write rolls back the entire unit.

Registration, RegInap, doctor history, Waiting List, Opname Request, Reservation, and guarantor history are retained. Only the active-registration projection is physically removed. Source restoration and coherent idempotent replay remain required behavior.

## Domain-model boundary

Admission, Registration, RegInap, Waiting List, Opname Request, and Reservation models keep their existing transition behavior. Tata Rekening reads and billing eligibility do not belong in those models; the future application orchestrator invokes the read port before mutation.

## Implementation status

The current revision implements only the read-only Tata Rekening billing-item port and its DI registration. It deliberately does not implement the coordinated cancellation command, handler, endpoint, transaction, idempotency ledger, or mutation sequence.
