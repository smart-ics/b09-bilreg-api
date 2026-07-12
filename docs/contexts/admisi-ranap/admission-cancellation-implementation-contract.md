# Admission Cancellation — Implementation Contract

**Status:** Frozen implementation baseline  
**Date:** 12 July 2026  
**Scope:** Coordinated cancellation of an eligible Rawat Inap Admission and its shared Registration. This document resolves implementation mechanics only; the business boundary and state rules remain those in [docs/contexts/admisi-ranap/admisi-ranap-coordinated-cancellation-design.md](admisi-ranap-coordinated-cancellation-design.md).

## 1. Non-negotiable scope

The command implements only design stages A and B. It is a registration-wide administrative void, performed atomically. It retains Registration, RegInap, guarantor, Waiting List, doctor history, and source records; it deletes only the `BILRG_RegAktif` current-state projection.

Stage C (bed held/assigned/occupied), Stage D (downstream activity), Stage E (discharged/finalized), legacy repair, and any compensation workflow are out of scope. Unknown dependency authority is a blocker, never permission to proceed.

The source design's state transitions, source restoration outcomes, and prohibition on physical deletion of transactional history are **Frozen**.

## 2. Command and HTTP API contract

### 2.1 Application command

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

`AdmCancelAdmissionCmd` and its handler are superseded by this command; they must not remain reachable through the cancellation endpoint. The command is handled by one explicit application orchestrator. Domain models own their own transitions; repositories and DALs do not coordinate the workflow.

### 2.2 Endpoint

```http
POST /api/admisi-ranap/admission/{regId}/cancel
Content-Type: application/json
```

```json
{
  "reason": "Pasien membatalkan rencana rawat inap",
  "userId": "SPR001",
  "expectedAdmissionStatus": 0,
  "expectedUpdatedAt": "2026-07-12T03:15:30.1234567Z",
  "requestId": "6b81e8c8-d3d2-4bdf-b3b0-4d7a5d02d7c0"
}
```

The route value is `RegId`. `expectedAdmissionStatus` uses the current integer serialization of `AdmissionStatusEnum`. `expectedUpdatedAt`, when supplied, is an ISO-8601 UTC instant and must be compared exactly to the persisted admission update timestamp; it is not rounded or converted to local time.

| Input | Contract |
| --- | --- |
| `RegId` | Required route identity; max 50 characters. |
| `Reason` | Required after trimming; 1–500 characters. Persist the trimmed value. |
| `UserId` | Required; max 50 characters. This remains the actor until claims-based identity is introduced. |
| `ExpectedAdmissionStatus` | Required. Must be an allowed pre-cancellation status and equal the locked current status. |
| `ExpectedUpdatedAt` | Optional compatibility token. If present, it participates in the admission compare-and-set predicate. |
| `RequestId` | Required client-generated idempotency and correlation key; 1–50 characters, trimmed. |

Success is HTTP 200 using the existing JSend success envelope:

```json
{
  "status": "success",
  "data": {
    "regId": "RG...",
    "admissionStatus": "Cancelled",
    "registrationVoided": true,
    "alreadyCancelled": false,
    "waitingListId": "WL...",
    "restoredSource": { "type": "OpnameRequest", "id": "OPN..." },
    "correlationId": "6b81e8c8-d3d2-4bdf-b3b0-4d7a5d02d7c0"
  }
}
```

`waitingListId` and `restoredSource` are `null` when absent. `alreadyCancelled` is `true` only for the coherent-final-state idempotency case in section 8.

### 2.3 Error model

All expected failures return the API's JSend failure/error envelope with this machine-readable body inside `data`; exception messages are not the contract.

```json
{
  "code": "ADMISSION_HAS_BILLING",
  "message": "Admission cancellation is not eligible.",
  "blockers": ["ADMISSION_HAS_BILLING"],
  "correlationId": "6b81e8c8-d3d2-4bdf-b3b0-4d7a5d02d7c0"
}
```

| HTTP | Category | Codes |
| --- | --- | --- |
| 422 | malformed command | `VALIDATION_FAILED` (with field errors) |
| 404 | required root absent | `ADMISSION_NOT_FOUND`, `REGISTRATION_NOT_FOUND` |
| 400 | eligibility/business rejection | all `ADMISSION_*`, `REGISTRATION_*`, and `WAITING_LIST_*` blocker codes below |
| 409 | stale, concurrent, inconsistent, or idempotency conflict | `CONCURRENCY_CONFLICT`, `SOURCE_STATE_MISMATCH`, `COORDINATED_STATE_INCONSISTENT`, `REGINAP_HEADER_MISSING`, `REGAKTIF_MISSING`, `REQUEST_ID_REUSED` |
| 500 | unexpected persistence/infrastructure failure | no partially committed cancellation is permitted |

Return every independently observed eligibility blocker in a stable order, rather than only the first one. State/CAS failures discovered after the eligibility read return only their specific 409 code because the facts are no longer trustworthy.

## 3. Eligibility query contract

### 3.1 Application-facing port

```csharp
public interface IAdmissionCancellationEligibilityRepo
{
    AdmissionCancellationFacts LoadForCancellation(string regId);
}

public sealed record AdmissionCancellationFacts(
    bool HasActiveBedAssignment,
    bool HasAnyBedOrWardActivity,
    bool HasBilling,
    bool HasTataRekeningOrFinancialFinalization,
    bool HasTindakan,
    bool HasMedicationOrPharmacyActivity,
    bool HasClinicalDocumentation,
    bool HasMedicalOrServiceOrders,
    bool HasLabOrders,
    bool HasTransfer,
    bool IsDischarged,
    bool IsRegistrationVoided,
    bool IsRegistrationFinalized,
    IReadOnlyList<CancellationDependencyAuthority> Authorities);
```

`CancellationDependencyAuthority` is a named authority and one of `VerifiedClear`, `Blocked`, or `Unavailable`. It covers inpatient bed/ward, medication/pharmacy, clinical documentation, transfer, and every external/legacy dependency in the facts. `Unavailable` produces `DEPENDENCY_AUTHORITY_UNAVAILABLE` with the authority name. This is the design's enablement gate expressed as a query result.

The repository uses explicit existence queries by `RegId`; it must not reconstruct downstream aggregates or become a generic rules engine. `AdmissionCancellationPolicy` maps these facts and the locked aggregate states to the following stable blocker codes:

| Fact or state | Blocker code |
| --- | --- |
| bed assigned, held, or occupied | `ADMISSION_HAS_ACTIVE_BED_ASSIGNMENT` |
| any ward/bed activity | `ADMISSION_HAS_BED_OR_WARD_ACTIVITY` |
| billing | `ADMISSION_HAS_BILLING` |
| Tata Rekening or financial finalization | `ADMISSION_HAS_FINANCIAL_ACTIVITY` |
| tindakan | `ADMISSION_HAS_TINDAKAN` |
| medication/pharmacy | `ADMISSION_HAS_MEDICATION_ACTIVITY` |
| clinical documentation | `ADMISSION_HAS_CLINICAL_DOCUMENTATION` |
| medical/service order | `ADMISSION_HAS_MEDICAL_OR_SERVICE_ORDER` |
| lab order | `ADMISSION_HAS_LAB_ORDER` |
| transfer | `ADMISSION_HAS_TRANSFER` |
| discharged | `REGISTRATION_DISCHARGED` |
| registration already voided | `REGISTRATION_ALREADY_VOIDED` |
| registration finalized | `REGISTRATION_FINALIZED` |
| unverified external/legacy authority | `DEPENDENCY_AUTHORITY_UNAVAILABLE` |
| non-RegInap registration | `REGISTRATION_NOT_INPATIENT` |
| Admission not `Admitted`, `Updated`, or `Waiting` | `ADMISSION_STATUS_NOT_CANCELLABLE` |
| Waiting List not `Waiting` or `Accepted` | `WAITING_LIST_STATUS_NOT_CANCELLABLE` |

No source state mismatch is treated as a blocker: it is stale/concurrent state and returns `SOURCE_STATE_MISMATCH` (409).

### 3.2 Required invariants before mutation

The orchestrator must load and lock Admission, Registration, active Waiting List (if any), RegInap, RegAktif, and source (if any). It then verifies:

- exactly the one shared `RegId` is present on all applicable records;
- Registration is active `RegInap`, not voided, discharged, or finalized;
- `ta_reg_inap` exists for a normal new-system cancellation; absence is `REGINAP_HEADER_MISSING` and is never repaired or deleted here;
- `BILRG_RegAktif` exists; absence is `REGAKTIF_MISSING` unless the request is handled as coherent idempotent completion;
- an Opname source is `Fulfilled` by this `RegId`, and a Reservation source is `Realized` by this `RegId`;
- a legacy-sourced Admission has no source to restore.

These checks and the dependency facts are re-read/revalidated inside the write transaction. A dry-run query, if later exposed, is advisory only and must use the same facts but cannot authorize the write.

## 4. Transaction, locks, and execution order

**Frozen:** the complete operation runs in one `TransHelper.NewScope()`; audit and idempotency writes enlist in the same ambient transaction. There is no event bus, asynchronous compensation, or process-local lock.

Acquire locks in this fixed order to reduce deadlock risk: Registration by `RegId`, Admission by `RegId`, RegInap/doctor history by `RegId`, active Waiting List by `RegId`, RegAktif by `RegId`, source by its ID, then dependency/bed authority rows. SQL implementations use update locks/serializable key-range protection where a row may be absent, plus conditional updates. The concrete DAL may express those mechanics differently, but must preserve this observable contract.

Within the transaction:

1. claim/read the idempotency record (section 8) and return its completed response if it matches;
2. load and lock the records above, obtain eligibility facts, and evaluate the policy;
3. revalidate all expected states and source ownership under the locks;
4. cancel the active Waiting List, if present;
5. end every active RegInap doctor assignment on the one orchestration effective date and persist RegInap/history;
6. void Registration through `RegModel.BatalBerobat` using the same timestamp;
7. conditionally delete the one `BILRG_RegAktif` projection row;
8. conditionally restore the originating Opname Request or Reservation, if any;
9. conditionally cancel Admission using its expected status and optional exact update timestamp;
10. insert the complete audit set and mark the idempotency record completed with the response payload;
11. call `Complete()`.

Any zero-row conditional mutation, failed audit insert, or persistence exception aborts the scope. No catch-and-continue behavior is allowed.

## 5. Optimistic concurrency contract

The first implementation uses **lock + compare-and-set**, not a new row-version migration. This is **Frozen** for this slice because current tables have no row version. A later row-version migration is an explicit compatibility change, not a silent replacement of this contract.

Every mutable write has an expected-state predicate:

| Target | Required predicate |
| --- | --- |
| Admission | `RegId`, required expected status, current cancellable status, and `UpdDate` when `ExpectedUpdatedAt` is supplied |
| Waiting List | active row for `RegId`; status is `Waiting` or `Accepted` |
| Registration | `RegId`, `JenisReg = RegInap`, no void/discharge/finalization marker |
| doctor history | active assignments for `RegId` only; update only rows with no finish date |
| RegAktif | exactly one row for `RegId` |
| Opname | `Fulfilled` and `FulfilledRegId = RegId` |
| Reservation | `Realized` and `RealizedRegId = RegId` |

A predicate affecting zero rows rolls back and returns `CONCURRENCY_CONFLICT`, except source predicates, which return `SOURCE_STATE_MISMATCH`. Concurrent Waiting List acceptance, bed assignment, source edit, or Admission update has one winner only; the other operation must observe a conflict or blocker and leave no partial state.

## 6. Audit contract

Capture a full pre-change snapshot with `AuditLogSnapshotJson.Serialize` before each mutated aggregate/projection. Append these audit rows within the transaction, all with the same `RequestId` as `CorrelationId`, the trimmed reason, actor, one orchestration timestamp, and API-captured optional IP/User-Agent:

| Changed record | `EntityName` | action | event source |
| --- | --- | --- | --- |
| Admission | `AdmissionModel` | `VOID` | `Voided` |
| Registration | `RegModel` | `VOID` | `Voided` |
| RegInap and its doctor-history transition | `RegInapModel` | `VOID` | `Voided` |
| Waiting List, if changed | `WaitingListModel` | `VOID` | `Voided` |
| RegAktif projection removal | `RegAktifModel` | `DELETE` | primitive audit info using the orchestration actor/time |
| restored Opname or Reservation | concrete model name | `RESTORE` | its transition audit slot |

`BILRG_AuditLog` is append-only. The audit snapshot describes the record before the transition; no audit row is created for a rejected command. Audit is a compliance record, not a domain-event bus. The existing audit-log semantics are otherwise **Frozen**.

## 7. Correlation and request identity

`RequestId` is the correlation ID for the whole cancellation. The API passes it unchanged to the command and every audit row. It must be logged in structured request logging and returned on both success and expected failures. It is not regenerated by handlers or repositories.

## 8. Idempotency contract

The system needs a durable, transactionally enlisted cancellation-request ledger; audit lookup alone is not a safe idempotency implementation. Add a narrowly scoped persistence record keyed by `RequestId` with: request fingerprint, `RegId`, lifecycle (`InProgress`/`Completed`), completed response JSON, created/completed timestamp, and correlation ID. `RequestId` is globally unique for this endpoint; no database FK is introduced.

The fingerprint is SHA-256 over canonical UTF-8 JSON containing `RegId`, trimmed `Reason`, `UserId`, `ExpectedAdmissionStatus`, and `ExpectedUpdatedAt` in round-trip UTC format. It is an implementation integrity check, not a business value.

| Situation | Required result |
| --- | --- |
| same key + same fingerprint + completed | HTTP 200 with the stored response; no new writes or audits |
| same key + different fingerprint | HTTP 409 `REQUEST_ID_REUSED` |
| same key currently in progress | serialize on the ledger row; after lock, return the completed response or perform the command if the prior transaction rolled back |
| different key + coherently completed final state | HTTP 200, `alreadyCancelled: true`, after verifying cancelled Admission, voided Registration, no RegAktif, no active Waiting List, correct doctor end dates, and restored/no-source state; persist this response in the new ledger record without creating duplicate transition audits |
| different key + partially cancelled/inconsistent state | HTTP 409 `COORDINATED_STATE_INCONSISTENT`; no repair is attempted |

The idempotency record is created and completed in the same transaction as the cancellation. A rollback leaves no completed result and permits a safe retry.

## 9. Test matrix

| Area | Scenario | Expected assertion |
| --- | --- | --- |
| Success | Stage A with legacy source | Admission/Registration voided; doctors ended; RegAktif removed; no source restore; correlated audit set and ledger committed |
| Success | Stage B Waiting | Waiting List cancelled and all coordinated transitions commit |
| Success | Stage B Accepted without bed | same as Waiting, with no bed activity |
| Success | Opname and Reservation separately | exact source state/reference restoration (`Requested` / `Maintained`) and `RESTORE` audit |
| Rejection | each dependency fact | each fact independently yields its named blocker and no mutation/audit/ledger completion |
| Rejection | unavailable authority | `DEPENDENCY_AUTHORITY_UNAVAILABLE`; no mutation |
| Rejection | invalid Admission/Registration/Waiting List state | stable status blocker; no mutation |
| Rejection | missing RegInap or RegAktif, source mismatch, non-RegInap | stated 409 code; no repair/delete |
| Rollback | each write step, including audit and ledger completion, throws | all earlier data writes roll back; no audit or ledger residue |
| Persistence | legacy void mapping | only `fd_tgl_void`, `fs_jam_void`, `fs_kd_petugas_void` are set; discharge/cancel-discharge remain unchanged |
| Persistence | retained/deleted records | RegInap, guarantor, Waiting List, doctor history remain; only RegAktif is physically deleted |
| Idempotency | same key replay | one transition/audit set; byte-equivalent stored response |
| Idempotency | different-key coherent replay / inconsistent state | idempotent success / `COORDINATED_STATE_INCONSISTENT` respectively |
| Concurrency | Waiting acceptance, bed assignment, source update, Admission update | one winner; loser receives 409 or blocker; no partial state |
| HTTP | validation, 404, 400 blockers, 409 conflicts, 500 | documented status, JSend envelope, code, and correlation ID |
| Regression | queries consuming active Registration | voided Registration is not active/searchable/dischargeable; `RegModel.IsAktif` is false when voided |

Run domain, handler/orchestration, DAL/integration, and HTTP/SQL tests. Concurrency tests must use separate database connections/transactions; mock-only concurrency tests are insufficient.

## 10. Open implementation decisions and enablement gates

The following are not new business decisions. They are required confirmations before enabling the endpoint in an environment:

1. Identify and wire the authoritative production sources and lock/CAS mechanism for inpatient bed occupancy, ward activity/transfer, medication/pharmacy, clinical documentation, and external legacy records. Until each is `VerifiedClear`, the command returns `DEPENDENCY_AUTHORITY_UNAVAILABLE`.
2. Confirm actual SQL Server column precision for Admission `UpdDate`; if it cannot support exact `ExpectedUpdatedAt` comparison, omit that optional token from the API until a compatible row-version migration is separately approved. Status CAS and transactional locks remain mandatory.
3. Confirm Waiting List enum numeric compatibility before adding `Cancelled`, and add terminal-state-safe `RegInapModel` rehydration before persisting zero active Primary DPJP assignments.
4. Confirm the existing response/error middleware can emit the stated non-200 JSend envelopes. If it cannot, implement that mapping in this feature's API layer without changing the codes or statuses.

No coding may treat these gates as a reason to weaken dependency checks or to broaden the cancellation workflow.
