# Unfulfilled Medication Outcome — Persistence Simplification Analysis

**Artifact status:** Investigation only. No schema, domain, or SOP change in this document.  
**Bounded context:** Apotek (`Pelayanan Obat Pasien`) — Outpatient Pharmacy  
**Date:** 2026-08-18  
**Question:** Can Unfulfilled Medication Outcome be fully represented from existing Dispensing, Copy Resep, Invoice / Tata Rekening financial correction, and related aggregate facts, without the dedicated table `BILRG_AptUnfulfilledOutcome`?

**Invoice mutability note (2026-08-18):** Tata Rekening financial correction remains a **collaborator**, not the Unfulfilled quantity ledger. After [`ADR-APT-003`](../adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md) and **PD-07**, Credit Note is Tata Rekening-owned. There is no `BILRG_AptCreditNote`. Invoice revision under **BR-APT-027** still does not replace `BILRG_AptUnfulfilledOutcome`. The KEEP conclusion below is unchanged.

**Authoritative sources (not re-opened):**

- [`apotek-domain.md`](../apotek-domain.md) — especially §5.8, §6.2–§6.5, `BR-APT-018`–`019`, `BR-APT-046`, `BR-APT-054`, `BR-APT-056`–`060`, `BR-APT-071`, `BR-APT-079`–`080`, `BR-APT-094`, `BR-APT-104`, `BR-APT-108`–`118`
- [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) — §2.1, §6.1, §8.4–§8.9, §9.2
- [`outpatient-apotek-screen-and-aggregate-design.md`](../outpatient-apotek-screen-and-aggregate-design.md) — §5.1 Sales Order ownership
- [`outpatient-apotek-workflow.md`](../outpatient-apotek-workflow.md) — `WF-APT-RJ-002` / `003` / `004` / `007`
- [`sop/SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-EN.md`](../sop/SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-EN.md)
- [`ALN-006-RESOLUTION-REPORT.md`](./ALN-006-RESOLUTION-REPORT.md) — shortage timing cut at Sales Order establishment (BC-10)

**Codebase evidence:** No Apotek write model, DTO, DAL, or SQL script exists in `b09-bilreg-api` at analysis time. The table exists only as a proposed persistence shape. This report evaluates the design, not a live schema.

---

## 1. Recommendation

**KEEP** `BILRG_AptUnfulfilledOutcome`.

The table is the Sales Order aggregate’s append-only ledger that an **Accepted Quantity** received a final non-fulfillment outcome. Dispensing, Copy Resep, Invoice, and Tata Rekening financial correction already store related but **different** facts. Reconstructing Unfulfilled Medication Outcome from those neighbors is lossy: several required closures have no Dispensing, no Invoice, and no Copy Resep, and one Sales Order Item may close under more than one reason and quantity.

**REMOVE is not viable.** It would leave `BR-APT-018` / `BR-APT-019` without a durable, item-and-quantity fact, and would collapse independent commercial, physical, and accepted-demand dimensions that the domain keeps separate (`BR-APT-056`–`057`, `BR-APT-104`).

**REWORK is optional and must not delete the table.** The only useful rework is additive correlation (optional `DispensingId` / Tata Rekening correction identity when those documents exist) so operators can join neighbors without treating them as the source of truth. Folding the fact into mutable `SalesOrderItem` columns, or into Dispensing terminal status, is a rejected rework.

---

## 2. Current responsibilities of `BILRG_AptUnfulfilledOutcome`

Persistence design (§6.1, §8.4) places the table as an **append-only detail of `SalesOrder`**, PK `(SalesOrderId, OutcomeNo)`. Proposed columns: `OutcomeNo`, `SalesOrderItemNo`, `Qty`, `Reason`, `CopyResepId`, `ActorId`, `EffectiveAt`. Delete+insert is forbidden.

Domain ownership is explicit:

- Terminology: Unfulfilled Medication Outcome is “a final, accountable reason that an accepted medication quantity was not fulfilled” (`apotek-domain.md` §2).
- Object: it “identifies any Copy Resep, return, or financial correction required” and does not use backorder closure (§5.8).
- Aggregate: `SalesOrder` “owns Sales Order Items, accepted quantities, fulfilled quantities, **unfulfilled outcomes**, and overall resolution” (§6.2).
- Event: `Unfulfilled Medication Recorded` — “an accepted quantity received a final non-fulfillment outcome” (§9).

Those statements define **quantity closure on accepted demand**, not a document type of its own aggregate.

### 2.1 Responsibilities the table is designed to carry

| # | Responsibility | Why it is on Sales Order, not a neighbor |
|---|---|---|
| R1 | Close a positive **Accepted Quantity** that will not be fulfilled (`BR-APT-018`) | Accepted Quantity lives on `SalesOrderItem`. Dispensing Qty and Invoice Qty are independent branches (§8.5). |
| R2 | Record **reason, actor, effective business time** for that closure (`BR-APT-058`) | Header `ResolvedReason` is order-level. Dispensing `CancelReason` / `ExpiredAt` exist only if a Dispensing exists. |
| R3 | Support **split quantity and split reason** on one established Sales Order Item | Item identity is frozen after establishment (`BR-APT-050`). One line may be partly handed over and partly closed for shortage, expiry, or decline. `OutcomeNo` is the only proposed append-only split. |
| R4 | Remain valid when **no Dispensing** exists for that quantity (`BR-APT-030`, §8.5 “Not Yet Planned for Fulfillment”) | Dispensing is a physical fulfillment instruction (§5.4), not a mandatory wrapper for every accepted qty. |
| R5 | Remain valid when **no Invoice / Tata Rekening correction** exists (`BR-APT-022`, `BR-APT-079`, `BR-APT-094`) | Commercial resolution is a separate dimension (`BR-APT-056`–`057`). |
| R6 | Optionally correlate **Copy Resep** issued for post-establishment unfulfillment (`BR-APT-054`, `BR-APT-118`) without making Copy Resep the outcome | Copy Resep is a supporting document; it is not required, and it also covers pre-SO exclusions that never became Accepted Quantity. |
| R7 | Feed **reconciled projections** `UnfulfilledQty` / `ItemStatus` on `SalesOrderItem` | Persistence §8.4: those columns are maintained by Sales Order behavior. They are summaries, not the audit ledger. |
| R8 | Obey **append-only correction** (`BR-APT-060`) | Same persistence class as `BILRG_AptFinalReview`. Not the same class as Tata Rekening Credit Note. |
| R9 | Distinguish **Dispensing cancelled** (instruction ended; accepted qty may still be open) from **accepted qty closed** (`BR-APT-104`) | Multiple Dispensings per Sales Order are execution. Cancelling one Dispensing does not by itself fulfill `BR-APT-018`. |

### 2.2 Responsibilities the table is **not** designed to carry

These are often named “unfulfilled” in SOP language but are **out of this table’s grain**:

| Fact | Where it already lives |
|---|---|
| Prescription items **never accepted** (Telaah `Rejected`) | `BILRG_AptTelaahResepItem` |
| Prescription items **excluded before Sales Order** (Patient Request, pre-SO Stock Shortage, Fornas split) | Resep Kerja items minus Sales Order items; SO header `PartialReason`; Copy Resep when issued (`BR-APT-108`–`116`, ALN-006) |
| Physical instruction terminal state | `BILRG_AptDispensing.DispensingStatus` (`Cancelled` / `Expired` / `Unfulfilled`) |
| Stock return / Remove Stock | Dispensing item mutasi correlation ids + Stock Ledger |
| Commercial reversal | Tata Rekening Credit Note / Refund / Financial Adjustment (+ optional Invoice `TataRekeningCorrectionReff`) |
| Order-level resolution | `SalesOrderStatus` / `ResolvedReason` (including `CollectionWindowExpired`) |

---

## 3. What existing aggregates already cover

Coverage below means “this neighbor already stores a **related** operational fact.” It does **not** mean the neighbor can stand in for R1–R9.

### 3.1 Sales Order header and items (without the outcome table)

| Existing fact | Covers | Does not cover |
|---|---|---|
| `UnfulfilledQty`, `ItemStatus` | Current remainder / status projection | Why, who, when; split reasons; correction history |
| `SalesOrderStatus`, `ResolvedReason` | Order-level completion (`Resolved` / `Cancelled`) | Per-item quantity closure; mixed reasons on one order |
| `PartialReason` | Why the **established** SO is a partial prescription (Patient Request / Stock Shortage / Fornas) | Post-SO shortage (`BR-APT-118`); no-show; Patient-Pay decline of an already established SO |

### 3.2 Dispensing

| Existing fact | Covers | Does not cover |
|---|---|---|
| `DispensingStatus` ∈ {`Cancelled`, `Expired`, `Unfulfilled`} | Terminal state of **that** physical instruction; No-Show path sets `Expired` (`BR-APT-079`, `WF-APT-RJ-007`) | Accepted qty with **zero** Dispensings; qty left unplanned; leftover qty after a **Completed** Dispensing of a smaller amount |
| `CancelledAt` / `ExpiredAt` / `CancelReason` | Actor-time-reason for the Dispensing header | Item-split qty; a second reason on the same SO item; distinction “cancel instruction vs close accepted qty” |
| `ItemOutcome` (`Returned` / `Unfulfilled`) | Line outcome **inside** a Dispensing | Lines that were never planned onto a Dispensing Item |
| Return mutasi correlation | Inventory disposition after No-Show or unused reserve | Fulfillment-completion arithmetic on Accepted Quantity |

Dispensing §8.4 allows `Unfulfilled` as a Dispensing lifecycle state with “a reason and resolution of allocated stock and financial consequences.” That is the **instruction’s** terminal label. Domain §6.4 even says the Dispensing keeps “non-fulfillment outcomes” consistent **for its items**. That overlap is real, and it is the main reason the dedicated table looks redundant on the No-Show path. It is still a different grain: one Sales Order may have zero or many Dispensings (`BR-APT-030`), and partial-fulfillment semantics belong to Sales Order (`BR-APT-104`).

### 3.3 Copy Resep

| Existing fact | Covers | Does not cover |
|---|---|---|
| Header `Reason` (PatientRequest / StockShortage / post-SO unfulfilled), `SalesOrderId` (empty if issued before SO) | Accountable **Copy Resep** document (`BR-APT-054`) | Closures that do **not** issue a copy (typical No-Show; Patient-Pay decline with no copy; cancellation without external fill) |
| Items (`ResepKerja` `ItemNo`, `BrgId`, `Qty`) | What was copied for external fulfillment | Binding to `SalesOrderItemNo` when the copy is issued before SO; guarantee that copy qty equals closed Accepted Quantity |

Copy Resep is issued “when applicable.” Using it as the Unfulfilled Outcome store would force a document for every accepted-qty closure, or would silently drop closures that have no copy. It would also mix **pre-SO exclusions** (never Accepted Quantity) with **post-SO closures** (Accepted Quantity).

### 3.4 Invoice and Tata Rekening financial correction

| Existing fact | Covers | Does not cover |
|---|---|---|
| Invoice Item qty / status | What was billed | What was physically unfulfilled; BPJS No-Show with **no** Invoice (`BR-APT-079`) |
| Tata Rekening Credit Note / Refund / Financial Adjustment (PD-07; not `BILRG_AptCreditNote`) | Accountable commercial correction (`BR-APT-027`, `BR-APT-046`, `BR-APT-080`) | Fulfillment closure; uninvoiced qty; decline before Invoice (`BR-APT-071`, `BR-APT-094`) |
| Invoice `Cancelled` while still permitted | Commercial document ended | Accepted demand still open until an Unfulfilled Outcome (or fulfillment) is recorded |

`BR-APT-046` requires **both** an Unfulfilled Medication Outcome **and** commercial correction under `BR-APT-027` when payment/coverage evidence is followed by non-fulfillment. That wording is evidence that Tata Rekening financial correction is a collaborator, not a substitute.

### 3.5 Related supporting facts

| Fact | Role relative to Unfulfilled Outcome |
|---|---|
| Telaah reject | Never becomes Accepted Quantity; out of scope |
| Jual Bebas decline | No Jual Bebas row, no Sales Order (`BR-APT-089`); out of scope |
| Pharmacy Queue Close | Ends queue participation before pharmacy workflow; no Sales Order closure |
| Integration Task / Stock Ledger | Neighbor effects after the Apotek fact exists; not the fact |

---

## 4. Scenario matrix — can neighbors reconstruct the outcome?

| Scenario | Domain / SOP | Dispensing | Copy Resep | Credit Note / Invoice | Reconstruct without outcome table? |
|---|---|---|---|---|---|
| Pre-SO Partial Prescription (Patient Request / Available Stock shortage) | `BR-APT-108`–`116`, ALN-006 | Often none for excluded lines | Yes, when issued | No commercial line on this SO | **N/A — not this table.** Excluded lines never have `SalesOrderItemNo`. |
| Post-SO shortage, qty never planned on a Dispensing | `BR-APT-118` | None for that qty | Optional | Only if already invoiced | **No.** Neighbors can all be empty. |
| Post-SO shortage after Dispensing established, before handover | `BR-APT-118`, SOP-003 5.4 / SOP-004 5.2 | May become `Cancelled` / `Unfulfilled` | Optional | Credit Note if paid | **Partial.** Dispensing can show the instruction died; it does not uniquely prove Accepted Qty closed vs re-planned on a later Dispensing. |
| Patient declines Purchase Confirmation / Patient-Pay SO before Invoice | `BR-APT-071`, `BR-APT-094`, `WF-APT-RJ-003` | May exist (unused reserve) or not | Optional | No Invoice | **No** as a quantity ledger. Header `Cancelled` can close a **whole** SO but not mixed item leftovers on a continuing SO. |
| Authorized Dispensing cancel; SO item still to be fulfilled later | `BR-APT-030`, `BR-APT-104` | `Cancelled` | No | No | **Must not** invent an Unfulfilled Outcome. Inferring outcome from Dispensing cancel would **falsely close** Accepted Quantity. |
| No-Show / Collection Window Expired | `WF-APT-RJ-007`, `BR-APT-079`–`080` | `Expired` + return mutasi | Typically no | Credit Note only if paid General Patient | **Partial.** Dispensing `Expired` is necessary and sufficient for the **instruction**. Workflow still records Unfulfilled Outcome **per affected quantity** (SOP-007 step 5; `WF-APT-RJ-007` step 6) so SO completion (`BR-APT-019`) does not depend on joining Dispensing. |
| Paid non-fulfillment (shortage or No-Show) | `BR-APT-046`, `BR-APT-080` | Terminal or absent | Optional | Credit Note required | **No** from Credit Note alone. Amount/reason are commercial; they do not name `SalesOrderItemNo` + qty remaining unfulfilled. |
| Split outcomes on one SO item (e.g. 6 handed over, 4 expired; or 3 shortage + 2 declined) | §8.5, `BR-APT-047` | One Completed Dispensing plus remainder with no/other Dispensing | Maybe for remainder | Maybe for billed remainder | **No.** Item projections are scalars. Dispensing rows do not encode a second reason on the same accepted line without an SO-owned split. |
| Correction of a prior unfulfillment | `BR-APT-060` | New Dispensing or status change would be a different fact | New copy would be a different document | New Credit Note is a different fact | **No** without an append-only SO ledger. Mutating `UnfulfilledQty` erases history. |

**Conclusion of the matrix:** neighbors cover **some** scenarios’ *symptoms*. They do not cover the **full set** of Unfulfilled Medication Outcome responsibilities. The dangerous reconstruction is Dispensing `Cancelled` → treat Accepted Quantity as closed.

---

## 5. Information that would be lost if the table is removed

If `BILRG_AptUnfulfilledOutcome` is omitted and only existing columns/documents remain:

1. **Authoritative per-item, per-quantity closure** independent of whether a Dispensing or Invoice exists.
2. **Reason at Accepted Quantity grain**, distinct from Dispensing `CancelReason`, Invoice Credit Note `Reason`, Copy Resep header `Reason`, and SO `ResolvedReason`.
3. **Actor and `EffectiveAt` of the quantity closure**, distinct from Dispensing `ExpiredAt` / Invoice credit time / Copy Resep `IssuedAt` / generic `UpdDate`.
4. **Split-reason history** on one frozen Sales Order Item (`OutcomeNo`).
5. **Optional Copy Resep correlation from the closed SO quantity** (`CopyResepId`) without requiring a copy for every closure.
6. **Deterministic input to `UnfulfilledQty` / Fulfillment Completion** (`BR-APT-018`–`019`). `UnfulfilledQty` would become an unaudited counter, or a brittle join across optional neighbors.
7. **Ability to cancel a Dispensing without closing the Sales Order item** without extra inferred flags.
8. **First-class evidence for domain event `Unfulfilled Medication Recorded`**, which SOPs and `WF-APT-RJ-007` treat as a produced fact, not a query.

`SalesOrderItem.UnfulfilledQty` would still exist as a number. That number without the ledger is not an accountable outcome (`BR-APT-058`, `BR-APT-060`).

---

## 6. Required artifact changes if the table were removed

This section is the impact list **if** architects overrode the recommendation. It is not an implementation plan.

| Artifact | Change required |
|---|---|
| [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) | Drop table from §6.1, ERD, §8.4, §9.2 insert-only list. Redefine how `UnfulfilledQty` is maintained. Likely force Dispensing creation for every post-SO closure (conflicts with `BR-APT-030` / §5.4). |
| [`apotek-domain.md`](../apotek-domain.md) / [`apotek-domain-id.md`](../apotek-domain-id.md) | Rewrite §5.8 from a Sales Order–owned fact to a derived projection — **or** keep §5.8 and admit persistence no longer matches the domain. Touch `BR-APT-018`, `BR-APT-046`, `BR-APT-118`. Clarify Dispensing `Unfulfilled` vs Accepted Quantity closure (`BR-APT-104`). |
| [`outpatient-apotek-screen-and-aggregate-design.md`](../outpatient-apotek-screen-and-aggregate-design.md) | Sales Order would no longer “own unfulfilled progress” as a write fact; Exception Worklist would join Dispensing + Copy Resep + Tata Rekening correction with gap cases. |
| [`outpatient-apotek-workflow.md`](../outpatient-apotek-workflow.md) / `-id.md` | Remove or redefine `Unfulfilled Medication Recorded`; rewrite `WF-APT-RJ-007` step 6; rewrite post-SO shortage exceptions in `WF-APT-RJ-003` / `004`. |
| SOP-003, SOP-004, SOP-007 (EN/ID) | Stop requiring a recorded Unfulfilled Medication Outcome as a completion criterion; substitute neighbor documents (incomplete for unpaid / unplanned qty). |
| ALN-006 / BC-10 narrative | Post-SO branch currently “record Unfulfilled Medication Outcome”; would need a replacement fact that still forbids stripping Sales Order lines. |
| Future domain model / repo | `SalesOrder` reconstruction would load no outcome details; Fulfillment Completion rules would move into application-level joins. |

Domain and SOP would have to **change meaning**, not merely drop a table name. That is a larger decision than persistence simplification.

---

## 7. Risks, gaps, and audit-trail implications

### 7.1 Risks of REMOVE

| Risk | Effect |
|---|---|
| Ambiguous Fulfillment Completion | `BR-APT-019` cannot be evaluated without a closed-world sum of Accepted Qty = Fulfilled + Unfulfilled (by reason) + still open. |
| False closure | Treating every `Dispensing Cancelled` as Unfulfilled Outcome blocks later Dispensings (`BR-APT-030`). |
| False openness | Post-SO shortage with no Dispensing and no Copy Resep leaves Accepted Qty “Active” forever, or operators strip SO lines (forbidden by ALN-006 / `BR-APT-118`). |
| Commercial/fulfillment collapse | Using Credit Note as the fulfillment outcome violates `BR-APT-056`–`057` and fails BPJS No-Show (no invoice). |
| Copy Resep over-use | Forcing a Copy Resep for No-Show or cancellation creates documents the Patient does not need; skipping Copy Resep loses the only remaining neighbor. |
| Query complexity | Exception worklist and Patient Medication Journey would encode policy in SQL joins instead of an explicit fact (against persistence clarity in `DATABASE.md` / `ENGINEERING.md`). |

### 7.2 Gaps that remain even if the table is KEPT

These are design gaps, not arguments for deletion:

- Persistence §8.4 lists `Reason` as examples (shortage after SO, Patient-Pay decline, expiry, cancellation) but does not freeze an enum. Implementation still needs a closed reason set aligned to SOP paths.
- No proposed column points at the **Dispensing** or **Tata Rekening correction** that accompanied the closure. Operators must join by `SalesOrderId` + time. Optional correlation is the only justified REWORK.
- Dispensing lifecycle also has `Unfulfilled` (§8.4). Without a written invariant (“Dispensing Unfulfilled does not close Accepted Qty until an Unfulfilled Outcome row exists”), implementers may dual-write inconsistently or skip one side. **KEEP plus an invariant** is cheaper than REMOVE.
- `UnfulfilledQty` is a projection. The invariant should be: sum of outcome `Qty` per item = `UnfulfilledQty`, and Fulfilled + Unfulfilled + still-open = `AcceptedQty`.

### 7.3 Audit trail

`BR-APT-058` and `BR-APT-060` require responsible party, effective business time, and non-erasure of completed outcomes. The proposed table matches the same insert-only pattern already accepted for Final Dispense Review. Tata Rekening Credit Note is a neighbor document, not an Apotek insert-only sibling.

Without it:

- `UpdUser` / `UpdDate` on `SalesOrderItem` overwrite prior closures.
- Dispensing and Tata Rekening correction timestamps answer “when did the instruction or commercial document change?”, not “when was this accepted quantity closed?”
- Reconstructing history after a later Dispensing or a second Tata Rekening correction is not deterministic.

No-Show (`WF-APT-RJ-007`) **intentionally dual-writes**: Dispensing `Expired` (physical instruction + stock return) **and** Unfulfilled Outcome (accepted qty). That is audit of two aggregates, not duplication of one table.

---

## 8. Rejected alternatives (why not REWORK into neighbors)

| Alternative | Why it fails |
|---|---|
| Store reason/actor/time only on `SalesOrderItem` | One mutable slot; cannot split qty/reason; corrections overwrite (`BR-APT-060`). |
| Require a Dispensing for every unfulfillment and use Dispensing `Unfulfilled` | Turns Dispensing into a quantity-closure document; contradicts “physical fulfillment instruction” and “zero Dispensings allowed”. Confuses Dispensing cancel with SO close. |
| Use Copy Resep as the ledger | Optional document; wrong grain (Resep Kerja items, including pre-SO exclusions). |
| Use Credit Note as the ledger | Commercial only; missing when uninvoiced; `BR-APT-046` requires outcome **plus** commercial correction. Credit Note is Tata Rekening-owned (PD-07). |
| Derive at read time from “whatever neighbor exists” | Non-deterministic when neighbors conflict or are absent; Exception Worklist becomes a rule engine in SQL. |

**Allowed REWORK (additive, still KEEP):** optional `DispensingId` and/or Tata Rekening correction identity on the outcome row when those documents exist; closed `Reason` enum; explicit invariant that Dispensing terminal state ≠ Sales Order quantity closure until an outcome row is inserted. None of that removes the table, and none of it requires `BILRG_AptCreditNote`.

---

## 9. Verdict

| Option | Verdict |
|---|---|
| **KEEP** | **Recommended.** `BILRG_AptUnfulfilledOutcome` is the Sales Order–owned, append-only Accepted Quantity closure required by `BR-APT-018`–`019`, `BR-APT-046`, `BR-APT-118`, and `WF-APT-RJ-007`. Neighbors cover overlapping symptoms on some paths only. |
| **REMOVE** | **Rejected.** Information loss on unplanned qty, split reasons, decline-without-invoice, and cancel-vs-close. Audit and Fulfillment Completion become inferred. Domain/SOP would have to change, not just schema. |
| **REWORK** | **Not as a substitute.** Additive correlation and a written Dispensing-vs-SO invariant may be done later. Collapsing into Dispensing, Copy Resep, Invoice, or item scalars is rejected. |

Unfulfilled Medication Outcome **cannot** be fully represented from existing Dispensing, Copy Resep, Invoice / Tata Rekening financial correction, and related aggregate facts without a dedicated persistence table.
