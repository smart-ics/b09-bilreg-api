# Admission Cancellation — Implementation Contract

**Status:** Revised implementation baseline
**Date:** 12 July 2026  
**Scope:** Coordinated cancellation of an eligible Rawat Inap Admission and its shared Registration. This contract supersedes the prior multi-dependency eligibility design.

## 1. Scope and decision

Coordinated cancellation is a registration-wide administrative void, performed atomically. It retains Registration, RegInap, guarantor, Waiting List, doctor history, and source records; it deletes only the `BILRG_RegAktif` current-state projection.

The sole downstream eligibility blocker is Tata Rekening billing activity:

- one or more billing items for `RegId` reject cancellation with `REGISTRATION_HAS_BILLING_ITEMS`;
- zero billing items permit eligibility to continue;
- an empty Tata Rekening header is not a blocker and is not queried.

All required Registration, Admission, Waiting List, source, and concurrency validations remain separate orchestration-state validations. This change does not authorize a coordinated-cancellation orchestrator or endpoint implementation.

## 2. API and command contract

The planned command and endpoint remain unchanged:

```csharp
public record AdmCoordinatedCancelCmd(
    string RegId,
    string Reason,
    string UserId,
    AdmissionStatusEnum ExpectedAdmissionStatus,
    DateTime? ExpectedUpdatedAt,
    string RequestId,
    string? ClientIpAddress,
    string? UserAgent) : IRequest<AdmCoordinatedCancelResponse>, IRegKey;
```

```text
POST /api/admisi-ranap/admission/{regId}/cancel
```

`RegId` is required and has a maximum length of 50. `Reason` is trimmed and required (1–500 characters), `UserId` is required (maximum 50), and `RequestId` is a trimmed required client-generated idempotency/correlation key (1–50). `ExpectedAdmissionStatus` is required and must equal the locked cancellable status. `ExpectedUpdatedAt`, when supplied, is an exact ISO-8601 UTC compatibility token in the Admission compare-and-set predicate.

Success remains HTTP 200 in the JSend success envelope with `regId`, `admissionStatus`, `registrationVoided`, `alreadyCancelled`, optional `waitingListId`, optional restored source, and `correlationId`. Missing roots remain 404, malformed commands 422, and stale/concurrent/inconsistent/idempotency conflicts 409. A billing rejection is the sole downstream HTTP 400 business rejection and has this payload shape:

```json
{
  "code": "REGISTRATION_HAS_BILLING_ITEMS",
  "message": "Registration cancellation is not eligible.",
  "blockers": ["REGISTRATION_HAS_BILLING_ITEMS"],
  "correlationId": "6b81e8c8-d3d2-4bdf-b3b0-4d7a5d02d7c0"
}
```

No other downstream blocker code or dependency-authority result exists in this contract.

## 3. Eligibility read contract

```csharp
public interface IRegistrationCancellationEligibilityRepo
{
    bool HasBillingItems(string regId);
}
```

The implementation performs one read-only existence check against Tata Rekening's authoritative Financial Charge Ledger:

```sql
SELECT
    CAST(CASE WHEN EXISTS (
        SELECT 1
        FROM ta_trs_billing aa
        WHERE aa.fs_kd_reg = @RegId
    ) THEN 1 ELSE 0 END AS bit)
```

`ta_trs_billing` rows are billing items. `BILRG_TataRekening` (or another Tata Rekening header) and `ta_trs_billing2` financial projection rows are deliberately not queried. The check is scoped exclusively to the requested `RegId` and does not reconstruct a Tata Rekening aggregate.

The application maps `true` to `REGISTRATION_HAS_BILLING_ITEMS`; `false` permits the existing non-eligibility state validations to proceed. There is no facts object, dependency enum, authority availability state, generic cancellation policy, or blocker mapping table.

## 4. Preserved coordinated-cancellation contract

The future orchestrator must still load and lock Admission, Registration, active Waiting List (if any), RegInap, RegAktif, and source (if any), then validate shared identity, Registration type/active state, RegInap and RegAktif presence, source ownership/state, and expected Admission/Waiting List states.

The complete operation remains one `TransHelper.NewScope()` transaction. Acquire locks in this order: Registration, Admission, RegInap/doctor history, active Waiting List, RegAktif, then source. The billing-item existence check is re-read within that same transaction before mutation. SQL must use update locks/serializable protection where an expected row can be absent, plus conditional writes.

Every mutable write retains its expected-state predicate: Admission uses `RegId`, expected/current cancellable status, and optional exact `UpdDate`; Waiting List uses active `RegId` and a cancellable status; Registration uses `RegId`, `JenisReg = RegInap`, and no void/discharge/finalization marker; doctor history updates only active assignments for `RegId`; RegAktif has exactly one row for `RegId`; and a source must be `Fulfilled`/`Realized` by this `RegId`. A zero-row conditional mutation rolls back with `CONCURRENCY_CONFLICT`, except source state/ownership, which returns `SOURCE_STATE_MISMATCH`.

The effective transition order remains: claim/read the idempotency record; lock/revalidate state and billing; cancel Waiting List; end active RegInap doctor assignments; void Registration; remove RegAktif; restore source; cancel Admission; append audits; complete the idempotency record; commit. A failed conditional write, audit insert, ledger write, or persistence operation rolls back the entire unit.

Capture pre-change snapshots with `AuditLogSnapshotJson.Serialize`; append `VOID` audits for Admission, Registration, RegInap/doctor-history, and Waiting List where changed, a `DELETE` audit for RegAktif, and a `RESTORE` audit for a restored source. Every audit uses the request's correlation ID, actor, reason, effective timestamp, and optional request metadata. Rejected commands write no audit.

The durable idempotency ledger remains keyed by globally unique `RequestId` and holds the request fingerprint, `RegId`, `InProgress`/`Completed` lifecycle, completed response payload, timestamps, and correlation ID in the same transaction. Same key plus same completed fingerprint replays the stored response; a different fingerprint returns `REQUEST_ID_REUSED`; a different key with a coherent completed state returns the idempotent completed response without duplicate audits; partial state returns `COORDINATED_STATE_INCONSISTENT` without repair.

No physical deletion is allowed for transactional history other than the active `BILRG_RegAktif` projection. Tata Rekening is not queried by, and must not be represented in, any domain model.

## 5. Required tests

- zero billing items permits eligibility;
- one billing item blocks cancellation;
- multiple billing items also block cancellation;
- an empty Tata Rekening header does not block cancellation;
- the billing query is scoped to the requested `RegId` and reads `ta_trs_billing` only;
- no discarded downstream dependency affects eligibility;
- domain transitions, transaction, concurrency, audit, idempotency, source restoration, and rollback tests remain valid when the orchestrator is implemented.

The current code adds only the narrow read port and its tests. It does not add the orchestrator.
