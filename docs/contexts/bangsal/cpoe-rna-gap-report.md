# CPOE–RNA Alignment Gap Report

**Assessment date:** 2026-07-22  
**Canonical CPOE source:** `docs/contexts/cpoe/CPOE-DOMAIN.md`  
**Assessed scope:** all artifacts under `docs/contexts/bangsal/`

## 1. Executive conclusion

The Bangsal artifacts are **partially aligned** with the simplified CPOE domain.

The central semantic artifacts are already substantially aligned:

- `docs/contexts/bangsal/CPOE-RNA-INTEGRATION.md` uses `ClinicalOrderId` plus `OrderOccurrenceId`, finite occurrences, occurrence-level fulfilment, explicit cancellation, and assisted inter-ward destination reconciliation.
- `docs/contexts/bangsal/RNA-DOMAIN.md` and `RNA-DOMAIN-ID.md` consistently treat RNA execution as Destination-owned truth and CPOE status as occurrence-owned truth.

However, the complete Bangsal artifact set is not yet consistent. The highest-risk problems are:

1. `SOP-RNA-S01` still completes an entire Clinical Order by `ClinicalOrderId`, rather than one `OrderOccurrenceId`.
2. `SOP-RNA-S03` and the SOP index depend on CPOE concepts that no longer exist: `Fulfilment Outcome Validity`, `Corrected`, `Invalidated`, and `Outcome Review`.
3. Architecture, implementation-plan, UI, and implementation-baseline documents still contain the earlier contract's `discontinuation`, integrated clarification, `FulfilmentObligationId`, recurrence, and multiple-execution assumptions.

Therefore `GAP-RNA-ARCH-016` may remain **closed at design level only after this documentation realignment is completed**. Native implementation remains a separate dependency.

## 2. Canonical alignment baseline

The simplified CPOE domain establishes these constraints:

- One Clinical Order owns one or more finite, explicit Order Occurrences.
- Every occurrence has its own `OrderOccurrenceId`, planned time, Destination, and lifecycle.
- Completion and Not Performed apply to an **Active Order Occurrence**, not directly to the whole order.
- The parent order status is derived from all occurrence outcomes or explicit cancellation.
- One occurrence may retain at most one immutable Fulfilment Evidence record.
- A final occurrence never returns to Active.
- CPOE does not own acceptance, clarification, hold, discontinuation, execution workflow, destination-specific execution details, or advanced/infinite recurrence.
- An inter-ward transfer does not automatically change Destination. CPOE performs assisted reconciliation and explicitly retains or changes each affected Active occurrence.

These rules are the test used in this report.

## 3. Alignment matrix

| Artifact | Result | Summary |
|---|---|---|
| `CPOE-RNA-INTEGRATION.md` | Mostly aligned | Correct occurrence identity and lifecycle. Needs narrower Fulfilment Evidence payload/ownership wording and authoritative-worklist clarification. |
| `RNA-DOMAIN.md` | Mostly aligned | Correct occurrence-level rules, finite cardinality, reconciliation, and ownership. Minor stale use of `discontinuation`. |
| `RNA-DOMAIN-ID.md` | Mostly aligned | Semantically follows the English domain artifact; update together with `RNA-DOMAIN.md`. |
| `SOP-RNA-S01-*` | Not aligned | Operates by Clinical Order and finalizes/removes the whole order after one execution. Scheduled orders would be handled incorrectly. |
| `SOP-RNA-S03-*` | Not aligned | Requires CPOE validity/review concepts absent from the simplified domain and approved integration contract. |
| `RNA-SOP-INDEX.md` | Not aligned | Repeats the obsolete order-level S01 and outcome-validity S03 semantics. |
| `RNA-SERVICE-EXECUTION-MODEL.md` | Partially aligned | Has `OrderOccurrenceId`, but retains obsolete obligation, discontinuation, recurrence, and potentially plural-current-execution language. |
| `RNA-ARCHITECTURE.md` | Partially aligned | Core boundary is sound; several use cases and model descriptions still reflect the earlier source-revision/discontinuation contract. |
| `RNA-IMPLEMENTATION-PLAN.md` | Not aligned for CPOE slice | Still plans clarification contracts, obligation/revision payload, subsequent authorization, termination, and multiple executions per occurrence. |
| `RNA-UIUX-DESIGN.md` | Partially aligned | Worklist and execution concepts fit RNA, but discontinuation and integrated hold/clarification language exceed the simplified CPOE boundary. |
| `RNA-TATA-REKENING-INTEGRATION.md` | No direct CPOE conflict found | Financial delivery remains downstream of RNA execution and does not determine CPOE fulfilment. |
| `ADMISI-RNA-INTEGRATION.md` | Aligned for relevant boundary | Inter-ward accommodation changes do not themselves alter CPOE Destination or occurrence state. |
| Accommodation SOPs A01–A07 | No material CPOE conflict found | Their main concerns are accommodation and readiness; CPOE impact is mediated by explicit reconciliation. |
| SOP-RNA-S02 and S04 | Mostly aligned | Ad Hoc/Independent work does not fabricate CPOE orders; financial publication remains independent of CPOE fulfilment. |

## 4. Gaps and recommended resolutions

### GAP-CPOE-RNA-001 — SOP S01 uses order-level completion

**Priority:** P0 — design correctness blocker

`SOP-RNA-S01` states that one `ClinicalOrderId` represents one requested activity, records Completion and Not Performed by `ClinicalOrderId`, and removes the Clinical Order from active work after that outcome.

This contradicts finite Scheduled Orders. One Clinical Order may contain several independently active occurrences. Completing Monday morning's occurrence must not complete Tuesday morning's occurrence or remove it from the Destination Worklist.

**Decision:** Rewrite S01 around one `OrderOccurrenceId` while retaining `ClinicalOrderId` as the parent correlation.

Required changes:

- prerequisite and work selection use both identifiers;
- execution and Not Performed evidence finalize only the selected occurrence;
- RNA removes only that occurrence from pending work;
- the CPOE parent order may remain Active while another occurrence is Active;
- cancellation consumes the affected occurrence list supplied by `ClinicalOrderCancelled`;
- the SOP must show Single-Occurrence and Scheduled Order outcomes separately.

### GAP-CPOE-RNA-002 — Unsupported post-completion CPOE correction lifecycle

**Priority:** P0 — canonical-domain contradiction

`SOP-RNA-S03` and `RNA-SOP-INDEX.md` require CPOE to maintain:

- `Fulfilment Outcome Validity`;
- `Corrected` and `Invalidated` outcome values;
- mandatory `Outcome Review`; and
- a Responsible Clinician review lifecycle.

None of these exist in the simplified CPOE domain. They also exceed `CPOE-RNA-INTEGRATION.md`, which says that CPOE appends the correction notice and records a conflict or reconciliation need without rewriting the terminal occurrence status.

**Decision:** Preserve RNA's append-only correction governance, but reduce the CPOE consequence to the approved contract:

```text
RNA correction
  → ExecutionEvidenceCorrectionNotice
  → CPOE appends accountable history
  → conflict/reconciliation need is recorded
  → terminal occurrence status remains unchanged
```

Remove `Fulfilment Outcome Validity`, `Corrected`, `Invalidated`, and mandatory `Outcome Review` from all Bangsal claims about CPOE. Any human review may remain an RNA/Clinical Governance process, but it must not be presented as a CPOE lifecycle.

### GAP-CPOE-RNA-003 — Obsolete source-contract vocabulary in the implementation baseline

**Priority:** P1 — implementation-shape blocker

`RNA-SERVICE-EXECUTION-MODEL.md` retains:

- `FulfilmentObligationId`;
- cancellation/discontinuation source revisions;
- “recurring Clinical Order”; and
- plural execution facts in a way that can be read as allowing multiple performances for one occurrence.

The simplified contract requires `ClinicalOrderId` and `OrderOccurrenceId`; it has modification and cancellation but no discontinuation or fulfilment-obligation identity. Scheduled work is a finite explicit occurrence list, not recurrence generation.

**Decision:** Use one RNA ordered-work aggregate per `OrderOccurrenceId`.

- Remove `FulfilmentObligationId` from the native CPOE path unless it is documented strictly as RNA delivery metadata rather than a CPOE domain identity.
- Replace discontinuation with the canonical modification/cancellation facts.
- Replace “recurring” with “finite Scheduled Order occurrences.”
- Clarify that replacement facts belong to one append-only correction chain and do not prove an additional performance.

### GAP-CPOE-RNA-004 — Architecture still models discontinuation and generic revisions

**Priority:** P1

`RNA-ARCHITECTURE.md` says CPOE owns cancellation/discontinuation, and `UC-RNA-020` applies amendment, cancellation, or discontinuation through a generic source-revision contract. The simplified CPOE domain has eligible modifications and explicit Clinical Order cancellation, but no discontinuation contract.

**Decision:** Align architecture use cases directly with the seven contracts in `CPOE-RNA-INTEGRATION.md`:

- route occurrence;
- modify Active occurrence;
- cancel remaining Active occurrences;
- apply confirmed reconciliation destination decision;
- report Completed evidence;
- report Not Performed evidence; and
- report execution-evidence correction notice.

Transport revisions may still exist for idempotency, but they must not introduce an additional business lifecycle.

### GAP-CPOE-RNA-005 — Implementation plan describes the superseded integration contract

**Priority:** P1

The CPOE slice in `RNA-IMPLEMENTATION-PLAN.md` still includes clarification request, fulfilment obligation/revision, termination, subsequent authorization, execution/exception outbound, and “one occurrence/multiple distinct executions.” These statements conflict with the canonical integration contract's explicit exclusions and one-current-evidence-chain rule.

**Decision:** Replace that slice with the seven approved contracts and acceptance tests derived from them. At minimum test:

- one routed occurrence creates one pending RNA responsibility;
- scheduled occurrences remain independently actionable;
- modification applies only while the referenced occurrence remains Active;
- cancellation closes only listed pending occurrences;
- duplicate evidence is idempotent;
- evidence for a final occurrence becomes conflict/reconciliation work;
- transfer never auto-reroutes work; and
- correction never reopens or rewrites a terminal occurrence.

### GAP-CPOE-RNA-006 — Clarification and hold are ambiguously presented as integration behavior

**Priority:** P1

The simplified CPOE domain explicitly excludes clarification and hold workflows. `CPOE-RNA-INTEGRATION.md` follows that rule, and S01 appropriately says staff contact the Ordering Clinician without creating a CPOE clarification status. However, the implementation plan calls for a clarification request, while UI/UX exposes “Hold and request clarification.”

**Decision:** RNA may keep an internal operational blocker/note, but:

- it must not become a CPOE state or CPOE–RNA business contract;
- it must not change occurrence status;
- the occurrence remains Active in CPOE until Completed, Not Performed, or Cancelled; and
- any actual order change must be made by CPOE under its modification rules.

Rename UI actions to make their RNA-local nature explicit, for example `Record Internal Blocker` and `Contact Ordering Clinician`.

### GAP-CPOE-RNA-007 — Fulfilment Evidence payload exceeds clear CPOE ownership

**Priority:** P1 — boundary clarification

The CPOE domain defines Fulfilment Evidence minimally as outcome, effective time, responsible Destination, evidence reference, and reason when Not Performed. It also states that CPOE does not own destination-specific execution details.

`OccurrenceCompletedEvidence` currently requires `Performer` and execution description/reference in addition to the canonical minimum. RNA certainly owns those details, but the contract does not say whether they are merely transported or persisted as CPOE business state.

**Decision:** Separate the delivery envelope from CPOE-owned Fulfilment Evidence.

- RNA retains Performer and detailed execution content.
- CPOE persists only its canonical Fulfilment Evidence fields.
- CPOE stores an `EvidenceReference` to RNA for authorized detail retrieval.
- Extra envelope data, if transported for validation/audit, must not become duplicated CPOE execution truth.

### GAP-CPOE-RNA-008 — Destination Worklist authority is ambiguous

**Priority:** P2 — terminology/ownership clarification

The CPOE domain says CPOE provides the Destination Worklist. The integration contract also instructs RNA to create or expose pending-work responsibility. Both can coexist if RNA's view is explicitly a projection of CPOE-routed occurrences, but the current wording can be read as two authoritative worklists.

**Decision:** State explicitly:

- CPOE owns occurrence routing and authoritative Active status;
- RNA owns internal coordination and its local projection;
- RNA's worklist cannot independently accept, reject, cancel, or finalize the occurrence; and
- reconciliation compares RNA's projection with CPOE's authoritative occurrence state.

### GAP-CPOE-RNA-009 — Finite scheduled-order behavior is not covered end to end

**Priority:** P2 — completeness gap

The domain and integration contract support finite Scheduled Orders, but SOP, UI, architecture tests, and implementation-plan acceptance criteria do not consistently demonstrate them.

**Decision:** Add one canonical scenario across the artifacts:

```text
ClinicalOrderId ORD-1
  OCC-1 — Monday 08:00 — Completed
  OCC-2 — Monday 20:00 — Active
  OCC-3 — Tuesday 08:00 — Cancelled by later order cancellation
```

The scenario must prove independent worklist visibility, evidence, progress calculation, and cancellation behavior. Do not use recurrence rules or occurrence generation after creation.

## 5. Confirmed alignments

The following important decisions already match the simplified domain and should be preserved:

- CPOE owns clinical intent, occurrence lifecycle, routing, cancellation, Completion Progress, and Order History.
- RNA owns actual ward execution, Performer, Performed At, and append-only RNA correction history.
- `ClinicalOrderId` and `OrderOccurrenceId` are both mandatory for native ordered RNA work.
- RNA reports Completed or Not Performed only for the referenced occurrence.
- Service/Tarif resolution and financial publication do not gate CPOE fulfilment.
- Ad Hoc and Independent execution does not fabricate a retrospective Clinical Order.
- Inter-ward transfer retains RegId and never automatically changes Destination.
- A confirmed CPOE reconciliation decision changes only still-Active pending work.
- Cancellation never erases execution evidence already recorded.
- Duplicate delivery is idempotent, and conflicting reuse is visible rather than last-write-wins.

## 6. Recommended correction order

1. Fix `SOP-RNA-S01` to be occurrence-based.
2. Replace the unsupported CPOE correction lifecycle in `SOP-RNA-S03` and `RNA-SOP-INDEX.md`.
3. Align `RNA-SERVICE-EXECUTION-MODEL.md` and `RNA-ARCHITECTURE.md` to the seven canonical contracts.
4. Rewrite the CPOE slice of `RNA-IMPLEMENTATION-PLAN.md`.
5. Clarify RNA-local blocker/clarification UI and remove discontinuation wording.
6. Narrow Fulfilment Evidence persistence ownership and clarify Destination Worklist authority.
7. Add an end-to-end finite Scheduled Order scenario and acceptance tests.
8. Run a final terminology search for the retired concepts before reaffirming ARCH-016 design closure.

Suggested final verification search:

```text
FulfilmentObligationId
Fulfilment Outcome Validity
Outcome Review
discontinuation
recurring Clinical Order
clarification request
one occurrence/multiple distinct executions
```

Any remaining occurrence should either be removed or explicitly identified as RNA-local/transport-only and not part of the CPOE business model.

## 7. Closure recommendation

Do not reopen the already approved business ownership decisions in `GAP-RNA-ARCH-016`. Instead, create a documentation-alignment action covering GAP-CPOE-RNA-001 through GAP-CPOE-RNA-009.

The alignment action may be closed when:

- every ordered RNA workflow uses `OrderOccurrenceId` as the unit of fulfilment;
- no Bangsal artifact claims unsupported CPOE states or correction lifecycles;
- architecture and implementation plans use only the approved seven semantic contracts;
- finite Scheduled Order behavior is covered by SOP/UI/test scenarios; and
- CPOE and RNA ownership of evidence and worklist projections is explicit.

At that point, `GAP-RNA-ARCH-016 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY` remains an accurate classification.
