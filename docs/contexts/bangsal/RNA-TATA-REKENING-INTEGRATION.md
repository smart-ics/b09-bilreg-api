# RNA ↔ Tata Rekening Integration

**Status:** Approved target semantic contract; closes `GAP-RNA-012` and `GAP-RNA-ARCH-017` at design level; implementation pending  
**Primary gaps addressed:** `GAP-RNA-012`, `GAP-RNA-ARCH-017`  
**Audience:** AI implementation agents, architecture owners, application developers, testers  
**Transport:** Hybrid durable push plus authoritative reconciliation query

---

## 1. Purpose and Scope

This integration lets RNA publish billable RUANG RANAP Service Execution Facts and append-only corrections to the Tindakan/Tata Rekening boundary. The Tindakan-owning application creates at most one linked `Tindakan` per billable `ServiceExecutionFactId`; non-billable execution is not published to this boundary and creates no `Tindakan`.

It also lets RNA query Tata Rekening's authoritative lifecycle status before an ordinary Accommodation Correction, because RNA prohibits that workflow when Tata Rekening is `FINALIZED`.

This integration does **not**:

- transfer Service Execution, Performer, or Performed At ownership to Tata Rekening;
- let RNA calculate tariff, package, coverage, charge amount, payer, adjustment, journal, payment, or settlement; the RNA user supplies only the approved billable/non-billable execution classification;
- let Tata Rekening rewrite RNA execution or accommodation history;
- let an RNA repository write `Tindakan`, billing, or journal tables directly;
- define accommodation-assignment, retention, release, rooming-in, bed-day, or room-charge contracts;
- treat receipt acknowledgement as proof that a Financial Charge was created;
- define UI, HTTP, broker, serialization, or database contracts.

---

## 2. Context Ownership

| Business concern | Authoritative owner | Consumer | Boundary rule |
|---|---|---|---|
| RUANG RANAP Service Execution | RNA | Tata Rekening, CPOE | RNA alone states that a referenced Service was performed. |
| Service Execution source, Performer, Performed At, Responsible Ward, and correction history | RNA | Tata Rekening | Tata Rekening may retain references/snapshots but never edits RNA history. |
| Service identity and Service Definition | Tarif | RNA, Tata Rekening | RNA sends only the stable Service reference; neither context redefines it here. |
| Clinical Order intent and occurrence lifecycle | CPOE | RNA, Tata Rekening may reference | An order is not proof of execution or financial eligibility. |
| Billable/non-billable execution classification | RNA execution user under the approved dispatch contract | Tindakan/Tata Rekening boundary | Billable requires an eligible `ServiceId`; non-billable uses description and does not cross this boundary. |
| `Tindakan` formation | Tindakan-owning application component | Tata Rekening | It creates at most one `Tindakan` per billable `ServiceExecutionFactId` and returns the existing `TindakanId` on replay. |
| Billing Set, TrsBill ownership, Financial Verification, Adjustment, Allocation, Projection, Finalization | Tata Rekening | RNA may query lifecycle status only | RNA never writes Tata Rekening persistence or applies financial transitions. |
| Accommodation Allocation and Accommodation Correction | RNA | Authorized downstream contexts | This contract defines only the Tata Rekening lifecycle query used by RNA's correction gate; accommodation fact delivery is not yet approved. |
| Tata Rekening lifecycle status | Tata Rekening | RNA | A returned snapshot is authoritative only for the stated registration and observed revision/time. |
| Integration delivery state | Sender of each outbound obligation | Counterparty | Delivery state is neither execution state nor financial state. |
| Receiver processing outcome | Receiving context | Sender may observe | It reports receipt/application status, not ownership of the source fact. |

### Ownership invariants

1. Neither context directly mutates the other context's repositories, DALs, or tables.
2. RNA commits operational truth without a financial interpretation.
3. Tata Rekening applies only Tata Rekening-owned policy and transitions.
4. A committed RNA fact remains valid when delivery or financial processing fails.
5. Tata Rekening `CLOSED`, `FINALIZED`, or `LUNAS` state never erases a received RNA fact.
6. Corrections append history; they never overwrite the original fact.
7. A query snapshot or cached projection does not transfer lifecycle authority.

---

## 3. Collaboration Overview

### 3.1 Execution fact

```text
RNA records that Service X was performed by Performer Y at Performed At Z
→ RNA atomically commits the execution fact and Tata Rekening delivery obligation
→ Tata Rekening receives and deduplicates the fact
→ Tata Rekening independently evaluates Charge Eligibility
→ Tata Rekening applies only an approved local/adjacent financial workflow
→ Tata Rekening acknowledges receipt and reports its processing outcome
→ RNA updates delivery metadata only
```

No Service Execution Fact is sent when work was not performed.

### 3.2 Execution correction

```text
RNA preserves the original execution fact
→ RNA commits a new correction or Entered-in-Error fact
→ Tata Rekening receives the correction after, or reconciles it with, the original
→ Tata Rekening determines its own financial adjustment/recovery path
→ acknowledgement updates RNA delivery metadata only
```

### 3.3 Accommodation correction gate

```text
Head Nurse requests an ordinary Accommodation Correction
→ RNA queries Tata Rekening lifecycle status by RegistrationId
→ Tata Rekening returns an authoritative current status snapshot
→ RNA applies its own same-Ward, actor-authority, and pre-FINALIZED rules
→ query and response create no financial transition
```

### 3.4 Separation of states

```text
RNA business fact
≠ RNA delivery state
≠ Tata Rekening receipt acknowledgement
≠ Tata Rekening eligibility decision
≠ Financial Charge state
```

---

## 4. Business Contracts

## 4.1 RNA → Tata Rekening

| Contract | Type | Trigger | Commit point and business meaning | Receiver effect |
|---|---|---|---|---|
| `RnaServiceExecutionRecorded` | Business Fact | RNA commits an ordered, Ad Hoc, or Independent execution | The RNA fact and outbound obligation have already committed. Service X was performed by Performer Y at Performed At Z. | Deduplicate, preserve source correlation, and evaluate Charge Eligibility under Tata Rekening authority. Do not assume a charge must be created. |
| `RnaServiceExecutionCorrected` | Correction Fact | An authorized RNA correction workflow commits a correction or `EnteredInError` statement | The original remains immutable; this is a new revision referencing it. | Preserve both facts and reconcile Tata Rekening-owned financial state. Automatic adjustment, reopen, cancel-finalization, or reversal is not implied. |
| `GetTataRekeningLifecycleStatus` | Query Contract | RNA must validate the pre-`FINALIZED` Accommodation Correction gate or reconcile a prior observation | Requests the current Tata Rekening lifecycle for one `RegistrationId`; no RNA mutation has yet committed from this query alone. | Return current status and revision/observation metadata. Do not decide whether RNA may correct. |
| `GetExecutionFactProcessingOutcome` | Query Contract | RNA recovery cannot establish the outcome of a prior delivery | Requests Tata Rekening's recorded receipt/application outcome for a stable RNA fact revision. | Return the prior outcome or `Unknown`; do not re-run financial behavior merely because it was queried. |

### RNA sender guarantees

- Business facts and corrections have committed locally before publication.
- Retries reuse the same source fact identity and revision.
- Payloads carry no RNA decision about eligibility or money.
- RNA publishes no fact for ordered work that was not executed.
- A correction references its immutable original and carries a new identity/revision.

### Tata Rekening receiver prohibitions

Tata Rekening must not infer:

- that a Clinical Order proves execution;
- that an RNA delivery retry is a second performance;
- that Service identity alone determines price, coverage, package, or payer;
- that `RecordedAt` is the performance time;
- that a correction permits destructive deletion of either context's history;
- that an execution fact authorizes Tata Rekening to alter RNA records.

## 4.2 Tata Rekening → RNA

| Contract | Type | Trigger | Commit point and business meaning | Receiver effect |
|---|---|---|---|---|
| `RnaExecutionFactAcknowledged` | Acknowledgement | The receiver durably records/deduplicates a billable fact and its Tindakan outcome | Confirms `Created`, `AlreadyExists`, `RejectedTechnical`, or `ReconciliationRequired`, and includes `TindakanId` for created/existing outcomes. | RNA updates delivery metadata and linked Tindakan reference only; execution remains unchanged. |
| `RnaExecutionCorrectionAcknowledged` | Acknowledgement | Tata Rekening durably records a correction and its processing outcome | Confirms correction receipt/deduplication or a reconciliation condition. | RNA updates correction delivery metadata only. |
| `TataRekeningLifecycleStatusSnapshot` | Query Result | Tata Rekening answers `GetTataRekeningLifecycleStatus` | Current authoritative lifecycle observation for one registration at a stated revision/time. | RNA evaluates its own Accommodation Correction rule. It must not cache the response as permanent authority. |
| `ExecutionFactProcessingOutcomeSnapshot` | Query Result / reconciliation fact | Tata Rekening answers a processing-outcome query | Tata Rekening's recorded outcome for one RNA source fact revision. | RNA reconciles delivery state without replaying the human execution action. |

### No approved cross-context Command Requests

No business `Command Request` is approved in either direction for version 1. This is an explicit contract classification, not an omitted design.

- RNA must not send `CreateBill`, `ApplyTariff`, `ReopenBilling`, or `CancelFinalization`; those are foreign financial decisions.
- Tata Rekening must not send `CorrectExecution`, `VoidExecution`, or `CorrectAccommodation`; RNA actors and policies own those decisions.
- A future request to review operational truth, if approved, must be a rejectable command request and must not itself change RNA truth. It remains an open decision.

---

## 5. Contract Payload Semantics

## 5.1 Common envelope

Every retriable fact, correction, acknowledgement, and reconciliation result carries:

| Field | Meaning |
|---|---|
| `ContractId` | Stable identity of this contract occurrence; retries reuse it. |
| `ContractType` | Business contract name from this document. |
| `ContractVersion` | Version of payload meaning and state effect. |
| `SourceContext` | `RNA` or `TATA_REKENING`. |
| `SourceFactId` | Stable identity of the authoritative fact or observation. |
| `SourceRevision` | Monotonic revision within that source identity/chain. |
| `OccurredAt` | Authoritative UTC business time. For execution this is Performed At. |
| `RecordedAt` | UTC persistence time for audit/technical tracing. |
| `CorrelationId` | Cross-context workflow correlation; normally rooted in `RegistrationId` and the source fact chain. |
| `CausationId` | Contract/fact that directly caused this occurrence, when available. |
| `FacilityId` | Facility/tenant scope. |
| `SenderIdentity` | Authenticated application/service identity. |

Business order uses `OccurredAt`; delivery order uses neither `RecordedAt` nor arrival time as a substitute. When actual business time is unknown, RNA sets `OccurredAt = RecordedAt` and does not invent an earlier time.

## 5.2 `RnaServiceExecutionRecorded`

| Field | Authority / semantics |
|---|---|
| `ServiceExecutionFactId` | Stable RNA identity; the primary idempotency source identity. |
| `ExecutionRevision` | Monotonic RNA revision, initially the original fact revision. |
| `RegistrationId` | Stable registration correlation; not a patient-demographic substitute. |
| `TarifServiceId` | Stable Tarif-owned Service reference. |
| `ResponsibleWardId` | RNA-owned Ward responsibility reference at execution time. |
| `PerformerId` | RNA-authoritative actual accountable Performer identity. |
| `PerformedAt` | Actual performance time; must equal envelope `OccurredAt`. |
| `ExecutionSource` | Ordered, Ad Hoc, or Independent, using an approved RNA value. |
| `SourceOrderId` | Optional CPOE/legacy order reference when the execution was ordered. |
| `SourceOccurrenceId` | Optional stable planned-occurrence reference when one exists. |
| `AuthorityBasisReference` | Optional minimal reference for Ad Hoc/Independent accountability; no clinical narrative. |

Explicitly excluded: eligibility, billable flag, price, amount, quantity/unit billing rules, package, coverage, payer, diagnosis, clinical notes, full patient demographics, journal, and payment fields.

Display labels may be optional snapshots; stable IDs remain authoritative.

## 5.3 `RnaServiceExecutionCorrected`

| Field | Authority / semantics |
|---|---|
| `CorrectionFactId` | Stable RNA correction identity; retries reuse it. |
| `OriginalServiceExecutionFactId` | Immutable original fact reference. |
| `PreviousRevision` | Revision directly corrected. |
| `CorrectionRevision` | New monotonic revision in the execution chain. |
| `RegistrationId` | Registration correlation. |
| `CorrectionKind` | `Corrected` or `EnteredInError`, as already defined by RNA. |
| `CorrectedFields` | Only cross-boundary fields whose RNA truth changed. |
| `CorrectedValues` | Replacement values needed for Tata Rekening reconciliation. |
| `CorrectionReason` | Accountable, sanitized reason. |
| `CorrectedBy` | Authorized RNA correction actor. |
| `CorrectedAt` | Business time of the correction; envelope `OccurredAt`. |

The payload must not carry a requested financial action. Tata Rekening determines the financial response.

## 5.4 `GetTataRekeningLifecycleStatus` and snapshot

Query minimum:

- `QueryId`
- `RegistrationId`
- `FacilityId`
- `RequestedAt`
- authenticated requester identity and correlation

Result minimum:

| Field | Authority / semantics |
|---|---|
| `QueryId` | Correlates the response to the request. |
| `RegistrationId` | Exact requested registration. |
| `LifecycleStatus` | Tata Rekening-owned `OPEN`, `CLOSED`, `FINALIZED`, or `LUNAS` semantic value. |
| `LifecycleRevision` | Tata Rekening concurrency/version value when available. |
| `StatusOccurredAt` | Business time of the lifecycle transition when available. |
| `ObservedAt` | Time Tata Rekening read the authoritative state. |

The response must not contain `CorrectionAllowed`; that decision belongs to RNA. If status, registration, or authority cannot be established, the result is not safely usable for mutation.

## 5.5 Acknowledgements and processing outcomes

| Field | Meaning |
|---|---|
| `AcknowledgedContractId` | Contract occurrence being acknowledged. |
| `AcknowledgedSourceFactId` | RNA fact/correction identity. |
| `AcknowledgedSourceRevision` | Exact revision processed. |
| `ReceiptOutcome` | `AcceptedForProcessing`, `AlreadyProcessed`, `RejectedTechnical`, `RejectedStale`, or `ReconciliationRequired`. |
| `BusinessApplicationOutcome` | `Recorded`, `Applied`, `DeferredByLifecycle`, `NoFinancialEffect`, `ReconciliationRequired`, or `Unknown`; exact v1 values require approval. |
| `ReceiverReference` | Optional Tata Rekening-owned inbox/evaluation reference; not necessarily a TrsBill ID. |
| `ReceiverRevision` | Tata Rekening revision after local application, when applicable. |
| `AcknowledgedAt` | Receiver processing time. |
| `FailureCode` | Stable failure/reconciliation code when not successful. |
| `FailureMessage` | Sanitized explanation. |

Receipt and business application are separate. `AcceptedForProcessing` does not mean eligible, billed, finalized, or paid.

---

## 6. State and Responsibility Effects

| Current owner/state | Received contract or observation | Result | Context performing transition |
|---|---|---|---|
| RNA execution not yet recorded | Order/work exists | No execution fact and no financial effect from this contract | None |
| RNA execution committed; delivery pending | None | Execution remains valid; financial processing may lag | RNA fact already committed |
| Tata Rekening has not seen fact revision | `RnaServiceExecutionRecorded` | Receipt is recorded once; eligibility evaluation may begin | Tata Rekening |
| Tata Rekening `OPEN` | New execution fact | Tata Rekening evaluates eligibility; exact eligible-to-charge workflow is unresolved | Tata Rekening plus approved Charge Source boundary |
| Tata Rekening `CLOSED` | New/late execution fact | Preserve receipt; do not silently mutate frozen charges. Defer or reconcile under Tata Rekening policy | Tata Rekening |
| Tata Rekening `FINALIZED` or `LUNAS` | New/late execution fact | Preserve receipt and escalate/reconcile; no automatic ordinary mutation is defined | Tata Rekening |
| Original fact processed | `RnaServiceExecutionCorrected` | Preserve original and correction; determine receiver-owned financial response | Tata Rekening |
| Original fact unknown | Correction received | Hold/quarantine and retrieve/replay original; do not apply as unrelated execution | Tata Rekening |
| Any RNA execution state | Acknowledgement received | Delivery metadata changes only | RNA |
| RNA Accommodation Correction requested | Lifecycle snapshot is `FINALIZED` | Ordinary correction is rejected by RNA | RNA |
| RNA Accommodation Correction requested | Lifecycle snapshot is `OPEN` or `CLOSED` | Lifecycle gate alone passes; RNA still enforces actor, same-Ward, and fact rules | RNA |
| RNA Accommodation Correction requested | Status unknown/unavailable/stale | Mutation is blocked pending authoritative recheck | RNA |

`LUNAS` behavior for the RNA Accommodation Correction gate is not stated by the source SOP and remains an open decision. The safe interim behavior is deny-by-default, not an assertion of a new business rule.

No responsibility moves between contexts because of these contracts. RNA remains responsible for truthful execution history; Tata Rekening remains responsible for financial truth.

---

## 7. Reliability and Consistency

## 7.1 Delivery model

**Approved hybrid model:**

- durable at-least-once delivery for execution facts, corrections, and acknowledgements;
- read-on-demand for lifecycle and processing-outcome queries;
- no distributed cross-context atomic transaction.

The current code does not implement this target. A synchronous in-process adapter may optimize delivery, but it must preserve the same outbox, inbox, acknowledgement, retry, idempotency, and reconciliation semantics.

## 7.2 Idempotency

For facts and corrections, the receiver deduplication key is:

```text
SourceContext + SourceFactId + SourceRevision + ContractVersionMajor
```

Rules:

1. Same key and semantically identical payload returns the prior outcome without repeating financial behavior.
2. Same key with conflicting payload is quarantined as `ReconciliationRequired`; last-write-wins is prohibited.
3. A new correction uses a new `CorrectionFactId` and revision; it is not a replay of the original.
4. Query retries reuse `QueryId` when asking for the same observation attempt, but queries create no business mutation.
5. A timeout permits semantic replay with the same identity.

## 7.3 Ordering

Ordering is required per Service Execution correction chain:

- revisions are applied monotonically per `ServiceExecutionFactId`;
- a correction cannot be applied before its original is known;
- missing predecessors are held for bounded reordering or authoritative reconciliation;
- equal/lower processed revisions are duplicate or stale.

No global ordering is required across executions or registrations. `OccurredAt` preserves business chronology, including late entry. Arrival order and `RecordedAt` do not prove business order. Tata Rekening lifecycle concurrency is registration-scoped and must be rechecked when a received fact would cause local financial work.

## 7.4 Transaction boundaries

### RNA local transaction

For an original execution, RNA atomically commits:

- the Service Execution Fact;
- execution/authority history;
- audit;
- stable outbound obligation(s), including Tata Rekening.

For a correction, RNA atomically commits:

- the immutable correction/Entered-in-Error fact;
- audit;
- new outbound correction obligation(s).

Retry workers update delivery metadata only; they never repeat the execution or correction command.

### Tata Rekening local transaction

On receipt, Tata Rekening must atomically commit:

- idempotent inbox/processing outcome;
- any Tata Rekening-owned state effect that is approved for that lifecycle state;
- audit;
- acknowledgement obligation.

The receiver atomically creates or resolves the unique linked `Tindakan` with its inbox outcome when the financial lifecycle permits. After `FINALIZED` or `LUNAS`, it preserves receipt and returns `ReconciliationRequired` without automatically reopening billing, reversing payment, or deleting `Tindakan`.

### Cross-context boundary

```text
RNA execution commit is final for RNA even if Tata Rekening is unavailable.

Tata Rekening financial processing is eventual relative to RNA execution.

Failure in Tata Rekening never rolls back or deletes the RNA execution fact.
```

---

## 8. Failure and Reconciliation

| Failure case | Required behavior | Recovery owner | Prohibited behavior |
|---|---|---|---|
| Tata Rekening unavailable | Keep RNA fact committed; persist and retry same identity | RNA integration support | Re-record execution or invent a billable decision |
| RNA unavailable for acknowledgement | Retain Tata Rekening outcome and retry acknowledgement | Tata Rekening integration support | Reapply financial behavior |
| Duplicate delivery | Return prior receipt/application outcome | Tata Rekening | Create duplicate charge/evaluation |
| Unknown `RegistrationId` | Reject technically; preserve RNA fact/obligation for reconciliation | Both integration teams | Match by patient name/demographics |
| Unknown `TarifServiceId` | Reject/quarantine as dependency failure | Tata Rekening with Tarif owner | Create a permissive local Service definition |
| Stale revision | Return prior outcome or reject stale; do not overwrite newer revision | Tata Rekening | Last-write-wins |
| Future revision/missing original | Hold/quarantine; retrieve original or reconcile chain | Tata Rekening | Treat correction as unrelated execution |
| Same identity, conflicting payload | Quarantine and investigate | Both architecture/data owners | Generate a new identity to bypass conflict |
| Financial lifecycle blocks application | Preserve receipt; report `DeferredByLifecycle` or reconciliation outcome | Tata Rekening operations | Reject the operational truth as if it never happened |
| Correction delivery failure | Retry correction independently using same correction identity | RNA integration support | Modify and resend the original fact |
| Partial downstream success | Track Tata Rekening separately from CPOE and other destinations | RNA | Mark all destinations successful |
| Lifecycle query unavailable/unknown | Block ordinary Accommodation Correction and expose dependency failure | RNA operations | Use stale/default `OPEN` status |
| Permanent rejection | Move to visible reconciliation with reason and stop infinite silent retry | Sender first, then joint support | Delete source fact or silently discard obligation |

### Reconciliation capabilities

RNA must support authoritative lookup by `ServiceExecutionFactId`, revision, correction chain, `RegistrationId`, and delivery state.

Tata Rekening must support authoritative lookup by:

- `RegistrationId` for current lifecycle status/revision;
- RNA `SourceFactId` and revision for receipt/application outcome;
- receiver reference for any resulting Tata Rekening state.

### Reconciliation rules

1. RNA source truth wins for execution and correction facts.
2. Tata Rekening truth wins for lifecycle, eligibility, Billing Set, and financial effects.
3. Recovery replays delivery/application, never the original human performance.
4. Manual recovery may retry a stable obligation, query both owners, link an orphan after evidence, or mark permanent failure.
5. Manual recovery must not edit the other context, fabricate a new fact ID, or silently create/reverse financial or operational truth.

Observable integration states should be limited to `Pending`, `InProgress`, `Acknowledged`, `FailedRetryable`, `RejectedTechnical`, and `ReconciliationRequired`. They are not business lifecycle states.

---

## 9. Security and Audit

## 9.1 Authentication

Allowed deployment mappings are:

- trusted authenticated in-process application identity; or
- authenticated service principals across a process boundary.

Actor-facing RNA execution/correction commands additionally require authenticated user context. A caller-supplied actor ID is insufficient.

## 9.2 Authorization

**Phase boundary (ARCH-020).** The current implementation assumes only baseline authentication and coarse-grained application access. Fine-grained contextual authorization, including Ward/role/professional scope, legitimate relationship scoping, and service-principal enforcement, is intentionally out of scope for the current phase and deferred to Phase-99. The rules below preserve business ownership and integration semantics; their enforcement remains deferred.

- Only RNA's authoritative application boundary may publish RNA facts and corrections.
- Only Tata Rekening's authoritative application boundary may publish lifecycle snapshots and processing outcomes.
- RNA execution and correction authority remains Ward/role/professional-scope aware.
- Lifecycle queries must be restricted to a legitimate facility, registration, care, or recovery relationship.
- Integration-recovery authority does not grant execution-correction or financial-adjustment authority.
- Tata Rekening Verifikator authority does not grant authority to change RNA history.

## 9.3 Minimum-data and audit rules

Do not log full patient demographics, diagnosis, clinical narrative, price/coverage detail, credentials, or tokens.

Record at minimum:

- source and destination context;
- contract ID, type, version;
- source fact ID and revision;
- original fact ID for corrections;
- Registration, Service, facility, and Ward reference where applicable;
- business time, recorded time, delivery time;
- sender, receiver, and accountable human actor where applicable;
- correlation and causation IDs;
- attempt count;
- receipt and business-application outcomes;
- receiver reference/revision;
- failure code and sanitized reason.

Integration audit supplements, but never replaces, RNA aggregate history or Tata Rekening financial audit.

---

## 10. Versioning and Compatibility

1. Every contract carries `ContractVersion`.
2. Additive optional fields may remain within the current major version.
3. A change to business meaning, identity, required fields, idempotency scope, correction semantics, or state effect requires a new major version.
4. Producers do not remove required fields within a supported major version.
5. Consumers ignore unknown optional fields unless explicitly marked critical.
6. Initial release supports only contract `v1`; no overlap is required because no previous native production contract exists.
7. Historical replay retains its original contract version.
8. A version/deprecation policy must be approved before `v2` or another breaking change.
9. Operations owns deployment, monitoring, rollback, and backlog verification; rollback never deletes committed v1 facts or Tindakan records.

---

## 11. Open Decisions

| ID | Decision | Owner | Blocks | Safe interim behavior |
|---|---|---|---|---|
| `RNA-TR-OD-001` — CLOSED | The Tindakan-owning application consumes billable RNA execution facts and idempotently creates/resolves one linked `Tindakan` per `ServiceExecutionFactId`. | Tata Rekening + Tindakan + RNA owners | No design block; consumer implementation remains | RNA repositories never write Tindakan/billing/journal persistence directly. |
| `RNA-TR-OD-002` — CLOSED | RNA's execution user explicitly classifies execution as billable or non-billable. Billable requires an eligible `ServiceId`; non-billable requires description and creates no `Tindakan`. | RNA + Tata Rekening business owners | No design block | This classification does not give RNA tariff, coverage, amount, journal, or payment authority. |
| `RNA-TR-OD-003` — CLOSED | Before `FINALIZED`, an accepted billable fact/correction follows the Tindakan owner's append-only lifecycle. At `FINALIZED` or `LUNAS`, preserve receipt and return `ReconciliationRequired`; never automatically reopen, reverse payment, or delete Tindakan. | Tata Rekening business owner | Lifecycle-aware consumer implementation | Preserve every fact and settled financial history. |
| `RNA-TR-OD-004` — CLOSED | Execution correction is append-only. Before `FINALIZED`, the Tindakan owner applies its correction lifecycle; after `FINALIZED`/`LUNAS`, reconciliation is required and no automatic financial reversal occurs. | Tata Rekening + Cashier + Accounting owners | Correction implementation | Preserve original execution and Tindakan history. |
| `RNA-TR-OD-005` — CLOSED | V1 acknowledgement outcomes are `Created`, `AlreadyExists`, `RejectedTechnical`, and `ReconciliationRequired`; created/existing outcomes include `TindakanId`. RNA exposes delivery/Tindakan linkage, not tariff or settlement outcomes. | Tata Rekening + RNA architecture owners | Acknowledgement implementation | Acknowledgement never changes RNA execution truth. |
| `RNA-TR-OD-006` — CLOSED | `FINALIZED` and `LUNAS` block ordinary automatic financial application. Changes follow reconciliation/administrative handling outside RNA; RNA never initiates reopen or cancel-finalization. | RNA operations + Tata Rekening business owner | Reconciliation implementation | Preserve correction and return `ReconciliationRequired`. |
| `RNA-TR-OD-007` — CLOSED FOR V1 | Accommodation Facts are excluded from this contract. Room/accommodation charging requires a separate future contract. | RNA + Tata Rekening + Charge Source owners | None for v1 | Publish only billable Service Execution Facts. |
| `RNA-TR-OD-008` — CLOSED | Approved execution correction authority, second review, dispute handling, and evidence requirements (`GAP-RNA-011`). | Clinical Governance + RNA Operations | RNA correction producer | Preserve the immutable original; ordinary corrections may be finalized by the owning-Ward Head Nurse without self-approval; material, identity, replacement, Entered in Error, and disputed corrections require an independent Clinical Governance-authorized second reviewer; hold when authority or evidence is unclear. |
| `RNA-TR-OD-009` — CLOSED | Use hybrid durable outbox/inbox push plus authoritative processing-outcome query. An in-process optimization must preserve identical durability and idempotency semantics. | RNA + Tata Rekening architecture owners | Implementation only | Retry the same stable identity; expose failed/stale/conflicting work. |
| `RNA-TR-OD-010` — CLOSED (DEFERRED TO PHASE-99) | Define service-principal identities, facility/Ward scopes, and recovery roles. Fine-grained contextual enforcement is deferred; v1 uses baseline authentication and coarse-grained application access. | Identity/Authorization owner | Phase-99 authorization enforcement only; no v1 design or acceptance-test block | Preserve the target authority semantics in the contract, but do not claim contextual enforcement exists in v1. |
| `RNA-TR-OD-011` — CLOSED FOR V1 | Initial release supports only v1 with no previous-version overlap. Define upgrade/deprecation policy before v2. Operations owns rollout, rollback, monitoring, and backlog verification. | RNA + Tata Rekening architecture owners + Operations | No v1 design block | Rollback stops new intake without deleting committed facts/Tindakan. |

Implementation agents must not close these decisions by convention.

---

## 12. Traceability

| Contract or rule | Domain source | SOP source | Architecture/code evidence |
|---|---|---|---|
| RNA owns Service + Performer + Performed At execution truth | `docs/contexts/bangsal/RNA-DOMAIN.md` §§1, 5.9, 10.7–10.8 | `docs/contexts/bangsal/rna-sop/SOP-RNA-S01-Pelaksanaan-RNA-Service-Berdasarkan-Clinical-Order.md`; `docs/contexts/bangsal/rna-sop/SOP-RNA-S02-Pelaksanaan-Tindakan-Ad-Hoc-atau-Independen.md` | `docs/contexts/bangsal/RNA-ARCHITECTURE.md` UC-RNA-024/026 |
| No fact when work was not executed | `docs/contexts/bangsal/RNA-DOMAIN.md` §10.9 | `docs/contexts/bangsal/rna-sop/SOP-RNA-S01-Pelaksanaan-RNA-Service-Berdasarkan-Clinical-Order.md` exception | `docs/contexts/bangsal/RNA-ARCHITECTURE.md` UC-RNA-025 |
| Tata Rekening alone evaluates Charge Eligibility and financial consequences | `docs/contexts/bangsal/RNA-DOMAIN.md` §§1, 3.10, 10.12; `docs/contexts/TataRekening/01-context.md` operational-vs-financial truth | `docs/contexts/bangsal/rna-sop/SOP-RNA-S04-Penetapan-dan-Penyerahan-Charge-Eligibility.md` | `docs/contexts/bangsal/RNA-ARCHITECTURE.md` ADR-RNA-007, UC-RNA-031 |
| Tindakan owner creates one billable-execution record per RNA fact | `docs/contexts/TataRekening/01-context.md`; `docs/contexts/TataRekening/02-domain.md` TrsBill | SOP-RNA-S04; RNA-TR-OD-001/002 CLOSED | Approved idempotent adapter keyed by `ServiceExecutionFactId`; implementation pending |
| Correction preserves original and receiver decides impact | `docs/contexts/bangsal/RNA-DOMAIN.md` §§3.9, 10.10 | `docs/contexts/bangsal/rna-sop/SOP-RNA-S03-Koreksi-RNA-Service-Execution.md` | `docs/contexts/bangsal/RNA-ARCHITECTURE.md` UC-RNA-028 |
| Stable identity, at-least-once retry, and per-destination delivery | `docs/contexts/bangsal/RNA-DOMAIN.md` §8.5 | `docs/contexts/bangsal/rna-sop/SOP-RNA-S04-Penetapan-dan-Penyerahan-Charge-Eligibility.md` | `docs/contexts/bangsal/RNA-ARCHITECTURE.md` UC-RNA-030–033, ADR-RNA-010 |
| Tata Rekening lifecycle owns OPEN/CLOSED/FINALIZED/LUNAS | `docs/contexts/TataRekening/01-context.md`, `02-domain.md` | `SOP-TR-02`, `SOP-TR-07`–`09` | `TataRekeningModel.cs`; `TataRekeningStatusEnum.cs` |
| Accommodation Correction checks Tata Rekening status and is rejected at `FINALIZED` | `docs/contexts/bangsal/RNA-DOMAIN.md` §§3.11, 10.6a | `docs/contexts/bangsal/rna-sop/SOP-RNA-A06-Pelepasan-Akomodasi.md` | `docs/contexts/bangsal/RNA-ARCHITECTURE.md` UC-RNA-015 |
| Financial verification/adjustment does not rewrite operational history | `docs/contexts/TataRekening/01-context.md`; `docs/contexts/TataRekening/02-domain.md` | `docs/contexts/TataRekening/SOP-TR-04 — Financial Verification.md`; `docs/contexts/TataRekening/SOP-TR-05 — Financial Adjustment.md` | `docs/contexts/TataRekening/tata-rekening-phase-3-implementation-report.md` through `docs/contexts/TataRekening/tata-rekening-phase-5-implementation-report.md` |
| Existing code has no RNA aggregate or Tata Rekening RNA consumer | — | — | `RNA-ARCHITECTURE.md` §§1–2; absence under `src/bilreg/*/RnaContext`; `AddBillAppService` accepts only Reg/Tindakan models |
| Tata Rekening current lifecycle API/use cases exist but are not this semantic integration | `docs/contexts/TataRekening/02-domain.md` | `docs/contexts/TataRekening/SOP-TR-01 — Open Tata Rekening.md` through `docs/contexts/TataRekening/SOP-TR-10 — Settlement Initiation.md` | `src/bilreg/Bilreg.Application/PaymentContext/TataRekeningFeature/UseCases/OpenTataRekeningQuery.cs`; `docs/contexts/TataRekening/tata-rekening-phase-5-implementation-report.md`; current anonymous read is not sufficient service authorization |
| Events here are integration business facts, not event sourcing | — | — | `docs/concepts/operational-events.md`; `docs/ENGINEERING.md` §14 |

---

## 13. Existing, Target, and Deferred Behavior

| Classification | Behavior |
|---|---|
| Existing | Tata Rekening lifecycle, financial-control use cases, registration-scoped optimistic versioning, TrsBill creation from legacy Registration/Tindakan, Verifikator write policy, and API exposure exist. |
| Existing | No RNA aggregate, execution persistence, inbox/outbox, Tata Rekening RNA consumer, acknowledgement ledger, lifecycle-status integration port, or reconciliation view exists. |
| Target | The contracts, ownership, payload meaning, idempotency, transaction separation, retry, and reconciliation rules in this document. |
| Deferred | Accommodation Fact financial integration and Phase-99 fine-grained authorization only. |

---

## 14. AI Implementation Guardrails

Implementation agents must:

1. Treat these contracts as transport-agnostic semantics.
2. Create Application-boundary ports before adapters.
3. Never use another context's repository/table as the integration contract.
4. Preserve source identity, revision, `OccurredAt`, and `RecordedAt` unchanged through adapters.
5. Commit RNA facts/corrections and outbound obligations atomically.
6. Implement Tata Rekening idempotent receipt before enabling retries.
7. Separate receipt outcome from eligibility and financial-application outcome.
8. Preserve original facts and append corrections.
9. Recheck Tata Rekening lifecycle under registration concurrency before local financial mutation.
10. Keep CPOE, Tarif, RNA, Charge Source, Tata Rekening, Cashier, and Accounting ownership distinct.
11. Add visible recovery rather than silent data repair.
12. Stop at an open decision instead of inventing financial or clinical policy.

### Minimum acceptance tests

- Duplicate original delivery produces one receiver application outcome.
- Same identity with conflicting payload is quarantined.
- Retry after timeout does not repeat financial behavior.
- Execution remains committed while Tata Rekening is unavailable.
- No execution fact is published for unperformed work.
- `OccurredAt`/Performed At is not replaced by arrival or persistence time.
- Unknown registration or Service reference is rejected without fabricated matching.
- Correction preserves the original and cannot apply before it is known.
- Correction retry does not resend a modified original.
- Receipt acknowledgement does not imply eligible, billed, finalized, or paid.
- `CLOSED`/`FINALIZED`/`LUNAS` late facts remain visible for reconciliation and do not silently mutate frozen financial state.
- Lifecycle query creates no business transition.
- Unknown/unavailable lifecycle status blocks ordinary Accommodation Correction.
- `FINALIZED` snapshot blocks ordinary Accommodation Correction in RNA.
- Baseline authentication and coarse-grained application access are required for publishers, lifecycle queries, and recovery endpoints; contextual publisher/scope/recovery-role enforcement is a Phase-99 acceptance criterion.

---

# Final Summary

## 1. Authoritative ownership

- **RNA** owns accommodation history and the operational fact that a Tarif-referenced Service was performed by a Performer at Performed At, including append-only correction history.
- **RNA user** classifies an execution as billable/non-billable under the approved execution contract; **Tindakan owner** creates the unique billable-execution record; **Tata Rekening** owns all subsequent tariff, billing, adjustment, settlement, and lifecycle behavior through `LUNAS`.
- **Tarif** owns Service identity/definition and pricing policy; **CPOE** owns order intent; the Tata Rekening artifacts assign Financial Charge formation to **Charge Source**.
- Delivery and acknowledgement metadata never replaces business ownership.

## 2. Contracts in each direction

**RNA → Tata Rekening**

- `RnaServiceExecutionRecorded`
- `RnaServiceExecutionCorrected`
- `GetTataRekeningLifecycleStatus`
- `GetExecutionFactProcessingOutcome`

**Tata Rekening → RNA**

- `RnaExecutionFactAcknowledged`
- `RnaExecutionCorrectionAcknowledged`
- `TataRekeningLifecycleStatusSnapshot`
- `ExecutionFactProcessingOutcomeSnapshot`

No cross-context business command is approved in v1.

## 3. Implementation blockers

- RNA write/read models, execution/correction persistence, durable obligations, adapters, and recovery views do not exist.
- Tata Rekening has no RNA fact consumer, inbox/idempotency ledger, acknowledgement contract, or processing-outcome query.
- The idempotent Tindakan consumer, linkage columns/index, correction application, lifecycle reconciliation, and acknowledgement ledger are not implemented.
- Fine-grained contextual RNA authorization and service-principal/scope mapping are intentionally deferred to Phase-99 under ARCH-020; baseline authentication and coarse-grained application access remain the current assumption.

## 4. Deferred beyond v1

- No v1 business/design decision remains. Accommodation Fact charging requires a separate future contract; security-scope enforcement remains deferred to Phase-99 under ARCH-020.
