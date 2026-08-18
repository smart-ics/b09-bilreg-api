# Apotek Invoice Mutability — Artifact Update Report

**PD-07 supersession (2026-08-18):** Section 8 below recorded Credit Note as an Apotek append-only document and `BillingCredit` as its Integration Task. That is **no longer canonical**. See [`APOTEK-CREDIT-NOTE-OWNERSHIP-RESOLUTION.md`](APOTEK-CREDIT-NOTE-OWNERSHIP-RESOLUTION.md) and persistence **PD-07**. Credit Note is Tata Rekening-owned. `BILRG_AptCreditNote` does not exist.

**Date:** 2026-08-18  
**Task type:** Artifact refactoring. No source code, schema, or API contract was changed.  
**Decision recorded in:** [`ADR-APT-003`](../adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md)  
**Pre-decision impact map:** [`APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md`](APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md)

---

## 1. Executive Summary

Apotek artifacts previously froze Invoice content at Issue. **BR-APT-027** required Credit Note, Refund, or Financial Adjustment for any issued or financially settled Invoice. **PD-05** allowed rewrite only while `InvoiceStatus = Established`. Those rules made Issue an Apotek-owned immutability gate and treated Credit Note as the normal post-Issue correction path.

The project now adopts a different principle:

> Invoice SHALL be considered mutable while modification is still permitted by external financial processes owned by Tata Rekening.

Canonical artifacts were updated so that:

- Invoice is a commercial/charge document representing medication sale information that serves as source information for Tata Rekening.
- Invoice is **not** automatically immutable after Issue.
- `Issued` and `Financially Cleared` remain commercial and payment/coverage facts. They are not mutability locks and are not Tata Rekening Close / Finalize / Lunas.
- Normal correction is Invoice revision of the same Invoice while Tata Rekening still permits modification.
- Credit Note, Refund, and Financial Adjustment are exception mechanisms used when direct Invoice revision is no longer permitted.
- Apotek consumes Tata Rekening financial permission as an external business fact. Apotek does not define, calculate, or own Financial Clearance rules.

No new Apotek business capability was added beyond what this decision requires for internal consistency. Tata Rekening APIs, permission-contract shapes, and charge-update task types were not invented.

---

## 2. Artifacts Reviewed

| Artifact | Role | Outcome |
|----------|------|---------|
| [`apotek-domain.md`](../apotek-domain.md) / [`apotek-domain-id.md`](../apotek-domain-id.md) | Canonical domain | Updated |
| [`outpatient-apotek-workflow.md`](../outpatient-apotek-workflow.md) / [`outpatient-apotek-workflow-id.md`](../outpatient-apotek-workflow-id.md) | Canonical workflow | Updated |
| [`outpatient-apotek-screen-and-aggregate-design.md`](../outpatient-apotek-screen-and-aggregate-design.md) | Screens and aggregates | Updated |
| [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) | Persistence / PD-05 | Updated |
| [`adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md`](../adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md) | Queue vs pharmacy state | No change required |
| [`adr/ADR-APT-002-pharmacy-stock-ledger-boundary.md`](../adr/ADR-APT-002-pharmacy-stock-ledger-boundary.md) | Stock Ledger boundary | No change required |
| [`adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md`](../adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md) | Invoice mutability | **Created and accepted** |
| SOP-003 / 004 / 005 / 007 EN+ID | Operator procedures | Updated |
| [`sop/DAFTAR-SOP-APT-RJ.md`](../sop/DAFTAR-SOP-APT-RJ.md) | SOP index / glossary | No Credit Note definition to change |
| SOP-001 / 002 / 006 EN+ID | Queue, intake, coordination | No Invoice freeze language |
| [`APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md`](APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md) | Pre-decision analysis | Status banner: decision accepted |
| [`APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md`](APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md) | Clearance ownership | Banner; §6 marked historical freeze |
| [`APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md`](APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md) | Unfulfilled vs Credit Note | Note added; KEEP conclusion unchanged |
| ALN-001–007 resolution reports | Historical alignment | Left as historical; no freeze decision to reopen |
| [`../working/outpatient-apotek-repository-gap-analysis-report.md`](../working/outpatient-apotek-repository-gap-analysis-report.md) | BA-07 / BA-08 | No PD-05 freeze text; BA-08 remains |
| [`docs/ARTIFACTS.md`](../../ARTIFACTS.md) | Documentation index | ADR-APT-003, this report, and financial-clearance analysis indexed |

Assumptions searched for and found in canonical artifacts before this update:

- Issued Invoice is immutable
- Invoice cannot be revised after Issue
- Invoice rewrite is allowed only in `Established`
- Credit Note is mandatory for every Invoice correction after Issue
- Apotek owns an Issue-time freeze (stricter than Tata Rekening Charge Source rights)

Assumptions **not** found as current Apotek policy (already withdrawn by BA-08):

- Financial Clearance as an Apotek aggregate
- Apotek-owned Financial Clearance calculation

`Financially Cleared` was **not** the freeze trigger in PD-05 (Issue / leave-`Established` was). Informal “financial clearance” in **BR-APT-046** / **BR-APT-118** meant payer evidence for Dispense Authorized, not a freeze flag. Those rules were still revised because they forced Credit Note after that evidence existed.

---

## 3. Changes Applied

### 3.1 Domain

| Location | What changed | Why |
|----------|----------------|-----|
| §1.3 Tata Rekening boundary | Apotek consumes TR financial permission; does not own Financial Clearance rules | Ownership must be explicit |
| Glossary **Invoice** | Commercial/charge document that is source information for Tata Rekening | Matches the adopted Invoice meaning |
| Glossary **Credit Note** | Exception document when direct revision is no longer permitted | Credit Note is no longer the normal post-Issue path |
| §6.3 Invoice aggregate | Mutability is not InvoiceStatus; revision vs Credit Note split | Aggregate responsibilities must match BR-APT-027 |
| **BR-APT-027**, **046**, **051**, **060**, **071**, **080**, **118** | Two-path correction; Unfulfilled Outcome remains independent | Remove Issue-equals-freeze and Credit-Note-always |
| §8.3 Invoice lifecycle notes | `Issued` / `Financially Cleared` are facts, not freeze flags; `Adjusted or Credited` is the compensating-document path | Lifecycle must not re-encode mutability as a state |
| Events `Invoice Issued`, `Invoice Revised`, `Invoice Credited` | Issue does not freeze; revision is a distinct fact; credit is the exception path | Event meaning must not imply immutability |

English and Indonesian domain documents were kept in semantic parity.

### 3.2 Workflow

| Location | What changed | Why |
|----------|----------------|-----|
| Tata Rekening participant | Owns permission for Invoice revision; exception outcomes when not permitted | Stop treating TR as Credit Note supplier only |
| WF-003 exceptions | Revise while permitted; Credit Note / Adjustment / Refund when not | Remove “issued or financially cleared → Credit Note” |
| WF-004 shortage | If a BPJS Invoice already exists, follow BR-APT-027 | Consistency after handover-time Invoice |
| WF-005 | Established General Patient Invoice follows BR-APT-027 | Same two-path rule |
| WF-007 paid path | BR-APT-027; Sales Order stays `Active` until commercial consequence is resolved | Paid No-Show must not force Credit Note solely because Issue occurred |
| Events | `Invoice Revised` added where post-Issue correction can occur | Distinct from `Invoice Credited` |
| Cross-context handoff | Financial Charge, permitted revision, or Credit Note / Refund | Charge Source editor vs requester |

### 3.3 Screen and aggregate design

| Location | What changed | Why |
|----------|----------------|-----|
| Exception Worklist **Correction Request** | Accountable correction; revision when permitted | “Must not silently replace history” no longer means “never edit an Issued Invoice” |
| **Financial Consequence Pending** | Exception outcomes when revision is not permitted | Keep the worklist category for blocked / Cashier paths |
| Pelayanan Penjualan correction | Workbench may revise the same Invoice when permitted | Operator path matches BR-APT-027 |
| Invoice aggregate row | Mutable while TR permits; does not own TR permission rules or Financial Clearance | Screen design matches domain ownership |
| Tata Rekening collaborator | Permission plus exception outcomes | Stop listing only credit/refund |

### 3.4 Persistence

| Location | What changed | Why |
|----------|----------------|-----|
| §4.2 Invoice consistency boundary | Rewrite while Established, and after Issue while TR still permits | Remove “rewriteable until Issued” |
| §8.5 save semantics | Same two-window rewrite; Credit Note append-only when revision is not permitted | Align save policy with BR-APT-027 |
| §9.2 reconstruction exceptions | PD-05 rewrite window expanded | Table matched the old freeze |
| §11 `BillingCredit` | Compensating charge when revision is no longer permitted | Credit task is exception, not default |
| **PD-05** | Supersedes Established-only freeze; fixes `Paid` / `Closed` state-name drift | Persistence must not own an earlier freeze than Tata Rekening |

No Financial Clearance table was added. Any last-consumed permission stored on Invoice is audit trace only. Charge-update task types and Tata Rekening APIs were **not** specified.

### 3.5 SOPs

SOP-003, SOP-004, SOP-005, and SOP-007 (EN+ID) now use Invoice revision when Tata Rekening still permits modification, and Credit Note / Refund / Financial Adjustment only when it does not. Unfulfilled Medication Outcome, Sales Order non-rewrite after establishment, and Final Dispense Review append-only records are unchanged.

### 3.6 Analysis and index

Pre-decision analyses were **not** rewritten as if they had always stated the new rule. They received status banners so they cannot be read as current canonical freeze policy.

---

## 4. Rules Revised

| Rule | Previous assumption | Revised meaning |
|------|---------------------|-----------------|
| **BR-APT-027** | Issued or financially settled Invoice → Credit Note / Adjustment / Refund, never silent replacement | Mutability follows consumed Tata Rekening permission. Revise the same Invoice while permitted. Credit Note / Refund / Financial Adjustment only when revision is no longer permitted. Apotek does not own TR permission rules or Financial Clearance. |
| **BR-APT-046** | Financial clearance then non-fulfillment → Unfulfilled Outcome **and required Credit Note/Refund** | Unfulfilled Outcome remains required. Invoice commercial correction follows **BR-APT-027**. Informal “financial clearance” wording replaced with Payment Clearance / Coverage Clearance sufficient for Dispense Authorized. |
| **BR-APT-118** | Post-SO shortage → Unfulfilled Outcome plus Credit Note/Refund when commercial consequences exist | Unfulfilled Outcome and Copy Resep unchanged. Commercial consequences follow **BR-APT-027**. |
| **BR-APT-051** | Shortage shall not erase an existing Invoice | Unchanged identity rule, plus: revision under **BR-APT-027** is not erasure. |
| **BR-APT-060** | Completed/cancelled outcomes are not erased; later correction adds a correcting fact | Invoice revision of the same Invoice is an accountable correction, not identity erasure. When revision is not permitted, the correcting fact is Credit Note / Refund / Financial Adjustment. |
| **BR-APT-071** | Cancel while lifecycle permits; issued or financially cleared follows BR-APT-027 | Cancel only while lifecycle **and** Tata Rekening permission both permit; otherwise **BR-APT-027**. |
| **BR-APT-080** | Paid No-Show: Sales Order stays `Active` until Credit Note / Refund / other TR outcome | Sales Order stays `Active` until the commercial consequence is resolved under **BR-APT-027**. |
| **PD-05** | Rewrite only while `Established`; then immutable | Rewrite while `Established`, and after Issue while Tata Rekening still permits modification. |

Rules **not** revised as Invoice-mutability rules:

| Rule | Why left as-is |
|------|----------------|
| **BR-APT-025** | Pricing Snapshot is still retained at establishment. Whether a later revision re-takes snapshot is not decided here. |
| **BR-APT-028** | Source Traceability Invoice → Tata Rekening still required, including after revision. |
| **BR-APT-041** / **BR-APT-043** / **BR-APT-122** | Payment Clearance and Dispense Authorized remain evidence evaluation, not Financial Clearance. |
| **BR-APT-045** | Paid / financially cleared Invoice still does not guarantee fulfillment. |
| **BR-APT-056** / **057** | Commercial and fulfillment independence unchanged. |
| **BR-APT-018** / Unfulfilled Outcome | Fulfillment quantity ledger unchanged. |
| **BR-APT-096** | Final Dispense Review records remain immutable. |
| **BR-APT-083** | Users still must not key in free-form Invoice Items. |

---

## 5. Definitions Revised

| Term | Previous | Revised |
|------|----------|---------|
| **Invoice** | Authoritative commercial document and Aggregate Root for one Medication Sale | A commercial/charge document representing medication sale information that serves as source information for Tata Rekening. Still the Aggregate Root for one Medication Sale. |
| **Credit Note** | Commercial document reducing or reversing an **issued** Invoice amount | Exception commercial document used when Tata Rekening no longer permits direct Invoice revision. |
| **Invoice Issued** (event) | Invoice became an authoritative commercial document (read as freeze) | Invoice became the posted commercial/charge document for collection and Financial Charge. Issue does not make content immutable. |
| **Invoice Revised** (event) | Not defined | Commercial content of the same Invoice was revised under Tata Rekening permission. Identity remains. |
| **Invoice Credited** (event) | Credit Note reduced or reversed an Invoice consequence | Same, and only because direct revision was no longer permitted. |
| **Financially Cleared** | Invoice state after payment/coverage evidence | Unchanged as a payment/coverage fact. Explicitly **not** a mutation lock and **not** Tata Rekening Close / Finalize / Lunas. |
| **Financial Clearance** | Already retired by BA-08 | Reaffirmed: not an Apotek object, status, or rule set. Consuming TR permission does not recreate it. |
| **Financial Adjustment** | Accountable correction to a Medication Sale or its financial consequences | Unchanged. Positioned with Credit Note / Refund as an exception mechanism when revision is not permitted. |
| **Pricing Snapshot** | Immutable commercial basis when Invoice is established | Unchanged in this update. |

---

## 6. ADR Impacts

| ADR | Impact |
|-----|--------|
| **ADR-APT-001** | None. Queue vs pharmacy workflow state ownership is independent of Invoice mutability. |
| **ADR-APT-002** | None. Stock Ledger vs Dispensing ownership is independent of Invoice mutability. |
| **ADR-APT-003** | **Accepted.** Invoice mutability is gated by consumed Tata Rekening permission. BA-08 Financial Clearance stays retired. PD-05 Established-only freeze is superseded. Credit Note is retained as the exception path. Mutability is not a new InvoiceStatus. Tata Rekening APIs are not specified by this ADR. |

---

## 7. Invoice Lifecycle Changes

The state machine names are unchanged:

```text
Established
  -> Issued
       -> Financially Cleared
            -> Resolved

Established or Issued
  -> Cancelled

Issued or Financially Cleared
  -> Adjusted or Credited
       -> Resolved
```

What changed is the **meaning of mutability relative to those states**:

| State | Still means | No longer means |
|-------|-------------|-----------------|
| `Established` | Medication Sale formed; local rewrite window before Issue | The only rewrite window |
| `Issued` | Posted commercial/charge document for collection and Financial Charge | Frozen rows |
| `Financially Cleared` | Payment Clearance and/or Coverage Clearance evidence reflected | Tata Rekening Close/Finalize, or a mutation lock |
| `Cancelled` | Document ended while still allowed | The only post-Issue “undo” |
| `Adjusted or Credited` | Compensating-document path | The only post-Issue correction branch |
| `Resolved` | Commercial consequences final, including revision, credit, or refund | Unchanged |

Invoice revision under Tata Rekening permission stays on the same Invoice and does **not** require a transition to `Adjusted or Credited`. Mutability is evaluated at command time from Tata Rekening. It is not stored as `Mutable` / `Frozen`.

---

## 8. Credit Note Changes

Credit Note remains an Apotek commercial document and an append-only persistence object. Its **role** changed.

| Situation | Previous path | Current path |
|-----------|---------------|--------------|
| Invoice `Established`, not yet Issued | Local rewrite (PD-05) | Unchanged |
| Invoice Issued, Tata Rekening still permits modification | Credit Note / Adjustment / Refund | **Invoice revision** of the same Invoice |
| Invoice Issued, Tata Rekening no longer permits modification | Credit Note / Adjustment / Refund | **Unchanged exception path** |
| Non-fulfillment after payment/coverage evidence | Unfulfilled Outcome **and** Credit Note/Refund | Unfulfilled Outcome **always**. Invoice follows BR-APT-027 |
| Paid General Patient No-Show | Wait for Credit Note/Refund; SO `Active` | SO stays `Active` until BR-APT-027 commercial consequence is resolved |
| BPJS never invoiced | No Credit Note | Unchanged |

Credit Note is **not**:

- the Unfulfilled Medication Outcome ledger
- mandatory merely because an Invoice has been issued
- an Apotek-owned Financial Clearance or approval workflow
- a replacement for Cashier Refund when settled funds must be returned (Refund remains Payment/Cashier-owned)

`BillingCredit` remains the compensating Integration Task when a Credit Note is recorded. It is not the default post-Issue charge path.

---

## 9. Cross-Document Consistency Validation

Checked after the update:

| Question | Result |
|----------|--------|
| Do canonical EN/ID domain, workflow, and SOP pairs agree on two-path correction? | Yes |
| Does any canonical rule still freeze Invoice at Issue? | No. Remaining freeze wording is only in pre-decision analysis context and ADR-APT-003’s Context section. |
| Does any canonical rule still force Credit Note solely because Issue occurred? | No |
| Does PD-05 still say rewrite only while `Established`? | No. PD-05 was updated and state-name drift (`Paid` / `Closed`) was removed. |
| Does Apotek own Financial Clearance rules? | No. BA-08 plus ADR-APT-003 / BR-APT-027. |
| Is mutability modeled as InvoiceStatus? | No |
| Are Unfulfilled Outcome, Sales Order identity after establishment, and Final Dispense Review append-only preserved? | Yes |
| Were Tata Rekening APIs or permission-contract internals invented? | No |
| Are analysis reports prevented from being read as current freeze policy? | Yes, via status banners |
| Is the documentation index current? | Yes |

Historical ALN-001–007 reports were left unchanged. They do not encode the withdrawn freeze as a living rule.

---

## 10. Remaining Open Questions

These are **implementation-planning** questions. They are not Invoice immutability investigations. The freeze-vs-permission decision is closed.

1. **Permission consumption contract.** How Apotek obtains Tata Rekening’s current modification decision at command time is Tata Rekening-owned. Apotek artifacts consume the decision; they do not specify the API, payload, or internal TR statuses used to produce it.
2. **Charge-change delivery after a permitted Invoice revision.** Today’s Integration Task catalog has `BillingCharge` (create) and `BillingCredit` (compensating credit). The task type, idempotency, and reconciliation for a permitted in-place charge change remain an integration-planning item. BR-APT-028 still requires Source Traceability.
3. **Pricing Snapshot after revision.** **BR-APT-025** still retains the snapshot taken at establishment. Whether a later permitted revision keeps that snapshot, versions it, or re-takes it is not decided here and must not be invented as an Apotek pricing rule.
4. **Payment Clearance vs revised Invoice totals.** If Payment Clearance already exists and Invoice amounts change under permission, Cashier/Payment owns whether Refund or re-clearance is required. Apotek must not infer mutability from Payment Clearance, and must not treat Payment Clearance as Tata Rekening permission.
5. **Invoice `Financially Cleared` after a permitted amount change.** Artifacts do not demote that state automatically. Whether evidence must be re-established is a later lifecycle-detail question, not a freeze question.
6. **Purchase Confirmation after Issued revision.** WF-003 still requires verbal confirmation before first establishment. Whether a later permitted revision requires re-confirmation is a workflow-detail question for implementation planning.
7. **Post-Issue rewrite history.** PD-05 continues delete+insert of items/charges while rewrite is allowed, with accountable actor and effective business time. Whether an additional Invoice revision history table is required is a persistence-detail question, not a mutability-policy question.

---

## Previous assumptions removed

1. Issued Invoice is immutable.
2. Invoice rewrite is allowed only in `Established`.
3. Credit Note is the normal (or mandatory) correction for every Issued Invoice.
4. `Financially Cleared` or “financial clearance” locks Invoice content.
5. Apotek owns an Issue-time freeze that is stricter than Tata Rekening Charge Source rights.
6. Apotek may encode Financial Clearance or TR permission rules locally to decide mutability.

## Decisions preserved

1. BA-08: no Financial Clearance or Fulfillment Clearance object in Apotek.
2. Dispense Authorized remains a policy evaluation over inbound evidence.
3. Invoice and Dispensing remain independent lifecycles coordinated by Sales Order.
4. Unfulfilled Medication Outcome remains required and is not replaced by Invoice revision or Credit Note.
5. Sales Order item identity is not rewritten after establishment.
6. Final Dispense Review records remain append-only.
7. Silent replacement without Tata Rekening permission and without accountable actor and time remains forbidden.
8. ADR-APT-001 and ADR-APT-002 remain unchanged.

---

**End state:** Canonical Apotek artifacts are internally consistent with Invoice mutability governed by Tata Rekening permission. Implementation planning can proceed from ADR-APT-003, **BR-APT-027**, and updated **PD-05** without reopening whether Issue freezes the Invoice.
