# Coordinated Cancellation Design — Rawat Inap Admission and Registration

**Date:** 12 July 2026  
**Scope:** Design and codebase assessment only; no production code or data changes  
**Decision status:** Ready with specified business decisions

Related artifacts:

- `docs/contexts/admisi-ranap/admisi-ranap-domain.md`
- `docs/contexts/admisi-ranap/admisi-ranap-architecture.md`
- `docs/contexts/admisi-ranap/admisi-ranap-registration-orchestration.md`
- `docs/contexts/admisi-ranap/ta-reg-inap-persistence-contract.md`
- `docs/concepts/operational-events.md`
- `docs/shared/audit-log.md`

## Executive decision

Admission cancellation is a registration-wide administrative void, not an Admission-only status update and not a physical purge. It is allowed only before bed occupancy and before any clinical, billing, medication, service, transfer, discharge, or finalization activity exists.

For an eligible registration, one application orchestration must atomically:

1. cancel the Admission;
2. void the legacy Registration using its existing void columns;
3. end every active inpatient doctor assignment on the cancellation date while retaining `ta_reg_inap` and doctor history;
4. remove the row from the active-registration projection (`BILRG_RegAktif`);
5. cancel any active Waiting List while retaining its row;
6. restore the originating Opname Request or Reservation for deliberate reuse;
7. append correlated audit records for every transition.

Once bed occupancy or downstream activity exists, this command must reject. Bed release, administrative discharge, financial/clinical correction, and reopening remain separate workflows.

## 1. Current cancellation flow

### HTTP and application path

Current endpoint:

```text
POST /api/admisi-ranap/admission/{regId}/cancel
body: { userId }
  -> AdmCancelAdmissionCmd(regId, userId)
  -> AdmCancelAdmissionHandler
```

The handler currently:

1. validates `RegId` and `UserId`;
2. loads only `AdmissionModel`;
3. serializes its pre-change snapshot;
4. calls `AdmissionModel.Cancel(userId)`;
5. saves only the Admission;
6. appends one `VOID` audit for `AdmissionModel` using `AuditTrail.Modified`;
7. does not open `TransHelper.NewScope()`.

`AdmissionModel.Cancel` accepts any mutable status (`Admitted`, `Updated`, or `Waiting`), changes `AdmissionStatus` to `Cancelled`, and records a modified actor/time. It rejects only `Completed` and already `Cancelled`. The model has no cancellation reason, dedicated cancellation timestamp, or concurrency version.

### Records changed today

| Record | Current result |
| --- | --- |
| `BILRG_AdmAdmission` | Status becomes `Cancelled`; `UpdUser`/`UpdDate` change |
| `BILRG_AuditLog` | One `AdmissionModel` / `VOID` row with original Admission snapshot |

### Records untouched today

| Record | Current residual state |
| --- | --- |
| `ta_registrasi` | Remains active; no `fd_tgl_void`, `fs_jam_void`, or `fs_kd_petugas_void` |
| `ta_reg_inap` | Remains present |
| `ta_reg_history_dokter` | Active assignments retain an empty finish date |
| `ta_reg_jaminan` | Remains present, which is appropriate for history |
| `BILRG_RegAktif` | Remains searchable/dischargeable as active |
| `BILRG_BedWaitingList` | `Waiting` or `Accepted` remains actionable |
| `BILRG_AdmOpnameRequest` | Remains `Fulfilled` with `FulfilledRegId` |
| `BILRG_AdmReservation` | Remains `Realized` with `RealizedRegId` |

There is no atomicity between the Admission update and its audit insert. An audit failure can therefore occur after the business update, depending on ambient context.

## 2. Current consistency gaps

1. A cancelled Admission can coexist with an active `ta_registrasi`.
2. `BILRG_RegAktif` continues to block or misrepresent future registration and appears in payment/discharge searches.
3. An active Waiting List can still be accepted or assigned after cancellation.
4. The source remains consumed, preventing deliberate retry from Opname/Reservation.
5. DPJP and other doctor assignments remain active indefinitely.
6. The endpoint has no reason, correlation ID, expected state/version, or dependency evidence.
7. No transaction protects cross-aggregate consistency.
8. The handler checks neither bed occupancy nor downstream activity.
9. A repeated call is rejected by the domain rather than returning an explicit idempotent result.
10. `AdmissionModel` records cancellation as modification while the handler labels it `VOID`; lifecycle semantics are inconsistent.

## 3. Eligibility matrix

| Stage | Cancellation allowed | Required preconditions | Result |
| --- | --- | --- | --- |
| A — Registration complete, no Waiting List | Yes | Admission mutable; Registration is RegInap and not void/discharged; RegAktif exists; no bed or downstream dependency | Coordinated administrative void; retain historical headers; remove active projection; restore source |
| B — Waiting List `Waiting` | Yes, with coordinated reversal | Stage A checks plus active Waiting List locked and still `Waiting` | Waiting List becomes `Cancelled`; remaining transitions same as A |
| B — Waiting List `Accepted`, no bed assignment | Yes, with coordinated reversal | Acceptance has not created/held/occupied a bed and no downstream work has started | Cancel acceptance through Waiting List `Cancelled`, then same result as A |
| C — Bed assigned/held/occupied | No through this command | None | Replace with explicit cancel-bed-assignment or administrative-discharge workflow. A business decision in that workflow may return the patient to Waiting List; it must not silently void the Registration |
| D — Any clinical, billing, medication, service, ward, transfer, or order activity | No | None | Reject coordinated cancellation; use domain-specific reversal plus administrative void/discharge only when every owning context permits it |
| E — Discharged, bill finalized, or Registration finalized | No | None | Use correction/reopening workflow; Admission cancellation is invalid |

The conservative rule is intentional: existence of downstream records is a blocker even when those records are themselves voided, until each owning context explicitly defines whether a fully reversed artifact is harmless. Phase 1 should favor false rejection over destructive reversal.

## 4. State transitions

| Concern | Eligible initial state | Final state | Persistence rule |
| --- | --- | --- | --- |
| Admission | `Admitted`, `Updated`, or `Waiting` | `Cancelled` | Retain row; add/use cancellation reason and actor/time; cancellation should populate the void lifecycle rather than only modified lifecycle |
| Registration | Active RegInap, not discharged/void | Voided/inactive | Retain `ta_registrasi`; populate existing `fd_tgl_void`, `fs_jam_void`, `fs_kd_petugas_void` through `RegModel.BatalBerobat`; do not populate discharge or cancel-discharge columns |
| RegInap header | Present, or documented legacy absence | Historical/inactive | Retain `ta_reg_inap`; do not delete it and do not invent a missing row during cancellation |
| Doctor history | One or more active assignments | All assignments ended on cancellation date | Set `fd_tgl_selesai` for every active assignment; do not create replacement assignments |
| RegAktif | One active projection row | No active projection row | Delete `BILRG_RegAktif` row. This table is a current-state projection without audit columns, so deletion is projection cleanup, not loss of registration history |
| Waiting List | None, `Waiting`, or `Accepted` | None or `Cancelled` | Retain row; add/use explicit cancellation transition, reason, actor/time; exclude `Cancelled` from active queries and prohibit accept/close/bed assignment |
| Guarantor | Present | Unchanged historical snapshot | Retain `ta_reg_jaminan` |
| Admission/source audit | Existing history | Append-only cancellation set | Never update/delete `BILRG_AuditLog` |

### Required model changes for implementation

- `AdmissionModel.Cancel(userId, reason, timestamp)` should use a cancellation/void audit slot and reject blank reasons.
- `RegModel.BatalBerobat` should reject an already voided or discharged Registration and accept the orchestration timestamp.
- `RegInapModel` needs a terminal behavior such as `EndAllDoctorAssignments(effectiveDate)` that intentionally relaxes the normal “exactly one active Primary DPJP” invariant only for a terminal registration. A terminal flag or validation mode may be needed during rehydration; otherwise the current repository cannot rehydrate a cancelled RegInap with zero active Primary DPJP.
- `WaitingListModel.Cancel(userId, reason, timestamp)` should accept both `Waiting` and `Accepted`, but only after the orchestration policy proves there is no bed assignment.

## 5. Source restoration rules

Source restoration is safe only inside the same cancellation transaction and only when the source still references the same `RegId`.

### Opname Request

Transition:

```text
Fulfilled(FulfilledRegId == cancelled RegId)
  -> Requested(FulfilledRegId = "-")
```

Decision: restore the original request, do not create a replacement source. The clinical request remains meaningful, preserves its identity/history, and becomes deliberately reusable. Append a `RESTORE` audit with the pre-restoration snapshot, cancellation reason, actor, timestamp, and shared correlation ID.

Reject cancellation as stale if the request is not `Fulfilled` by this exact Registration. Never overwrite `Cancelled`, a different fulfilled `RegId`, or concurrently edited state.

### Reservation

Transition:

```text
Realized(RealizedRegId == cancelled RegId)
  -> Maintained(RealizedRegId = "-")
```

Decision: restore to `Maintained`, not `Reserved`, because realization already required a maintained reservation and the maintained planning data should remain authoritative. Append the same form of `RESTORE` audit.

If the planned date is no longer operationally valid, restoration still preserves the row as `Maintained`; the user must maintain/reschedule it before reuse. Reject stale/mismatched source state rather than overwriting it.

### Legacy-sourced Admission

`AdmissionSource == Legacy` with no Opname/Reservation reference has no source to restore. Cancellation may proceed if all other eligibility checks pass.

## 6. Downstream dependency checks

Introduce one concrete application-facing query, for example `IAdmissionCancellationEligibilityRepo`, returning named facts rather than a generic rules engine:

```text
AdmissionCancellationFacts
- HasActiveBedAssignment
- HasAnyBedOrWardActivity
- HasBilling
- HasTataRekeningOrFinancialFinalization
- HasTindakan
- HasMedicationOrPharmacyActivity
- HasClinicalDocumentation
- HasMedicalOrServiceOrders
- HasLabOrders
- HasTransfer
- IsDischarged
- IsRegistrationVoided
- IsRegistrationFinalized
```

Responsibilities:

- Repository/DAL queries answer whether concrete registration-linked data exists and, where required, lock the relevant rows.
- A small `AdmissionCancellationPolicy` evaluates those facts into allowed/rejected plus stable reason codes.
- The handler/orchestrator loads aggregates, invokes their behavior, and coordinates persistence.
- Do not place cross-context SQL or repository access in domain models.
- Do not introduce a generic dependency framework; the dependency list is operationally specific and should remain explicit.

Confirmed codebase checks available or directly queryable include `ta_trs_billing`, `BILRG_TataRekening`, `BILRG_Tindakan`, `BILRG_OrderTdk`, `BILRG_LabOrder`, and operating-room records carrying `RegId`. The current ward bed model is master-oriented and the new Admission scope previously deferred bed assignment; implementation must identify the production inpatient bed-occupancy authority before enabling Stage C detection. Medication, EMR documentation, ward transfer, and legacy external tables also require consumer-owner confirmation.

Use existence queries, not full aggregate reconstruction, and add indexes only after verifying production query plans and actual legacy table shapes.

## 7. Transaction boundary and write order

All eligibility rechecks, state transitions, projection cleanup, source restoration, and audit inserts must commit in one `TransHelper.NewScope()`.

Recommended order inside the transaction:

1. load/lock Admission and Registration by `RegId`;
2. load/lock active Waiting List and source row;
3. run dependency checks under the same isolation/locking strategy;
4. revalidate expected Admission, Registration, Waiting List, and source states;
5. cancel Waiting List, if present;
6. end RegInap doctor assignments and save history;
7. void Registration;
8. delete RegAktif projection;
9. restore source;
10. cancel Admission;
11. append all audit records with one correlation ID;
12. complete transaction.

The logical order makes actionability disappear early, but no partial order is externally visible before commit. Any failure—including audit persistence—must roll back every change. The implementation must not catch and continue after a failed sub-write.

Isolation must prevent a Waiting List acceptance or bed assignment from passing after the dependency check. Prefer conditional updates/compare-and-set predicates on current statuses and source `RegId`, combined with transaction locks on the Registration and active Waiting List. A process-local lock is insufficient in a multi-instance API.

## 8. Command and API contract

Retain one pre-activity cancellation endpoint because Stages A and B have the same business outcome:

```http
POST /api/admisi-ranap/admission/{regId}/cancel
```

```json
{
  "reason": "Pasien membatalkan rencana rawat inap",
  "userId": "SPR001",
  "expectedAdmissionStatus": 0,
  "expectedUpdatedAt": "2026-07-12T10:15:30+07:00",
  "requestId": "client-generated-idempotency-key"
}
```

Command fields:

| Field | Rule |
| --- | --- |
| `RegId` | Route identity; required |
| `Reason` | Required, trimmed, bounded to audit capacity (500 characters) |
| `UserId` | Required; preserve existing compatibility until authenticated identity replaces body actor |
| `ExpectedAdmissionStatus` | Required in first implementation for compare-and-set protection |
| `ExpectedUpdatedAt` | Optional only if a real row-version is unavailable; exact timestamp comparison is weaker than a version column |
| `RequestId` | Recommended required idempotency/correlation key, max 50 characters |

Response should return `RegId`, final status, `alreadyCancelled`, restored source type/id, cancelled Waiting List ID if any, and correlation ID. Map stale compare-and-set failure to HTTP 409; dependency/business rejection to 400; missing aggregate to 404; malformed input to 422.

Do not overload this endpoint for Stage C–E. Later endpoints should carry different semantics, for example `/bed-assignment/{id}/cancel`, `/registration/{regId}/administrative-discharge`, or a context-owned financial/clinical correction API.

## 9. Concurrency and idempotency

| Situation | Required behavior |
| --- | --- |
| Same `requestId` submitted twice | Return the stored/same successful outcome without new transitions or duplicate audits |
| Different request after Registration already coherently cancelled | Return success with `alreadyCancelled = true` only after verifying Admission cancelled, Registration voided, no RegAktif, no active Waiting List, and source restored; otherwise return a consistency-conflict response for repair |
| Waiting List accepted during cancellation | Conditional status update or lock permits only one winner; loser receives 409 and no partial writes |
| Bed assignment occurs concurrently | Bed authority must participate in the locking/CAS protocol; cancellation loses with 409 once assignment/hold exists |
| Source changes concurrently | Conditional restoration requires expected status and matching source RegId; zero rows updated causes rollback and 409 |
| Admission status changes concurrently | Conditional Admission update on expected status/version; zero rows causes rollback and 409 |

Do not infer idempotency merely from `AdmissionStatus == Cancelled`; coordinated final-state verification is mandatory.

## 10. Legacy compatibility risks

### Confirmed conventions

- Legacy Registration void is represented by `fd_tgl_void`, `fs_jam_void`, and `fs_kd_petugas_void`, mapped to `RegVoidAudit` and set by `RegModel.BatalBerobat`.
- Discharge uses separate `RegKeluarAudit` columns; cancellation must not masquerade as discharge.
- Cancel-discharge uses separate `RegCancelOutAudit` columns; it is not Registration cancellation.
- `BILRG_RegAktif` is deleted by existing registration flows and is consumed by registration search and dischargeable-registration queries. Removing it is necessary to make a voided registration inactive.
- Transaction rows should follow void lifecycle rather than physical deletion.
- `ta_reg_inap` and `ta_reg_history_dokter` are currently physically deletable through repository APIs, but that API exists for persistence/cleanup and must not be used for operational cancellation.

### Risks requiring validation before coding

1. `RegModel.IsAktif` currently checks only `RegKeluarAudit`; a voided but not discharged Registration still reports active. It should also consider `RegVoidAudit`, and all consumers need regression tests.
2. Legacy reports may filter only discharge date or `BILRG_RegAktif`; verify they also exclude Registration void fields.
3. `RegInapModel` currently requires exactly one active Primary DPJP on rehydrate. Ending all assignments needs an explicit terminal-state-compatible model contract.
4. Waiting List has no `Cancelled` status/behavior in the current workflow; confirm numeric enum compatibility before adding a value.
5. Admission/Waiting List/source tables contain audit columns but no row version. Timestamp CAS can be fragile; a version column is safer but requires a migration and consumer review.
6. The real inpatient bed-occupancy and transfer authority is not established in this bounded context. Cancellation must remain disabled unless that dependency check is authoritative.
7. Medication, clinical documentation, and some legacy integrations may live outside the repository. Their owners must confirm registration-link fields and void semantics.
8. Audit action naming should be standardized: `VOID` for Admission/Registration/Waiting List and `RESTORE` for source, all with the same correlation ID and reason.

## 11. Stale record `RGA4LISM7N`

Known evidence documents this as intentionally retained remote smoke data for patient `347137300000070`, source Opname `OPN065SPTG0Y`, with Admission, Registration, RegAktif, guarantor, and audit rows but no `ta_reg_inap`. The source remains fulfilled and the patient remains blocked for re-admission.

No data was queried or modified in this design task. Before remediation, operations must re-query every eligibility dependency and confirm the record is test data.

Recommended disposition:

- If confirmed test data with no downstream dependency: remove it only through an approved, audited cleanup script because it violates the new-system invariant and cannot be normally rehydrated as `RegInapModel`.
- If it represents a real attempted admission: repair the missing `ta_reg_inap` only from authoritative clinical/registration evidence, then run the coordinated cancellation workflow if eligible.
- Do not run the future normal cancellation blindly against the incomplete row, and do not invent procedure/DPJP values.

Based on current evidence, **confirmed test-data cleanup is preferred**, subject to operational approval and a fresh dependency scan.

## 12. Recommended first implementation scope

Implement only Stages A and B:

1. add required reason, timestamp/correlation, and expected-state contract;
2. add explicit dependency fact queries for every authoritative in-repository table;
3. block cancellation unless inpatient bed/ward, medication, EMR, and legacy dependency authorities are confirmed clear;
4. add Admission, Registration, RegInap doctor-release, Waiting List cancel, and source-restore domain behavior;
5. orchestrate all writes and audits in one transaction;
6. use conditional updates/locks for Admission, active Waiting List, RegAktif presence, and source status/reference;
7. preserve Registration/RegInap/guarantor/doctor history; delete only RegAktif projection;
8. expose a dry-run eligibility query only if operations need to explain blockers; it must always be rechecked by the write command.

## 13. Deferred phases

- Stage C bed-assignment cancellation, bed release, return-to-Waiting-List, and administrative discharge.
- Stage D coordinated reversal of billing, orders, services, medication, clinical records, or ward activity.
- Stage E correction/reopening after discharge or financial finalization.
- Automatic cleanup/repair of incomplete legacy records.
- Generic workflow/event infrastructure or asynchronous compensation.
- Replacing request body `UserId` with claims-based actor identity across all APIs.
- Broad status redesign of legacy Registration tables.

## 14. Required tests

### Domain tests

- Admission cancels from `Admitted`, `Updated`, and `Waiting` with reason and cancellation audit.
- Admission rejects `Completed`, already `Cancelled`, blank reason, and stale expected status.
- Registration void sets only void fields; does not set discharge/cancel-discharge fields; repeated void is deterministic.
- `IsAktif` is false for voided Registration.
- RegInap cancellation ends all active assignments on one effective date, creates no assignments, preserves previous history, and can rehydrate terminal state.
- Waiting List cancellation supports `Waiting` and `Accepted`, rejects `Closed`/`Cancelled`, and records reason/actor/time.
- Opname restores only from `Fulfilled` with matching `FulfilledRegId`.
- Reservation restores only from `Realized` with matching `RealizedRegId` to `Maintained`.

### Handler/orchestration tests

- Stage A happy path reaches every final state and writes correlated audits.
- Stage B `Waiting` and `Accepted` happy paths cancel the Waiting List.
- Each downstream fact independently rejects before mutation.
- Missing RegInap legacy row follows the documented repair/cleanup path and is not silently deleted.
- Source mismatch, missing RegAktif, inconsistent already-cancelled state, and wrong Registration type return explicit conflicts.
- Failure at every persistence step rolls back all earlier writes, including audit failure.
- Same request ID twice produces one audit set; coherent already-cancelled state returns idempotent success.
- Concurrent Waiting List acceptance, bed assignment, source edit, and Admission edit yield one winner and no partial state.

### Persistence and integration tests

- Verify exact legacy void-column mapping in `ta_registrasi`.
- Verify `ta_reg_inap`, `ta_reg_jaminan`, and historical doctor rows remain.
- Verify all active doctor rows get a finish date and no new row is inserted.
- Verify only `BILRG_RegAktif` is physically deleted.
- Verify Waiting List row remains `Cancelled` and is excluded from active lookup/worklist.
- Verify Opname/Reservation reference resets and status transition atomically.
- Verify reports/search/dischargeable lists exclude voided Registration.
- Verify dependency queries against real schema and representative legacy data.
- End-to-end HTTP/SQL tests for Opname and Reservation cancellation, rollback, 409 concurrency, 400 blockers, and idempotent replay.

## Verdict

**Ready with specified business decisions.**

The cancellation semantics, safe boundary, persistence outcomes, source restoration, transaction scope, API shape, and first implementation slice are defined. Implementation must not begin until the production authorities for inpatient bed occupancy, medication, clinical documentation, ward transfer, and relevant legacy consumers are confirmed; until then their unknown state is a cancellation blocker, not permission to proceed.
