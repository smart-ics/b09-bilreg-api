# Apotek Invoice Mutability — Impact Analysis

**PD-07 supersession (2026-08-18):** Canonical artifacts no longer persist `BILRG_AptCreditNote`. Credit Note, Refund, and Financial Adjustment are Tata Rekening-owned. This report remains the pre-ADR-APT-003 impact map. Recommendations that keep an Apotek Credit Note table or Integration Task `BillingCredit` `{InvoiceId}:CN{n}` are superseded by [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) **PD-07**. Current rules are in [`apotek-domain.md`](../apotek-domain.md) **BR-APT-027**, PD-05, PD-07, and [`APOTEK-CREDIT-NOTE-OWNERSHIP-RESOLUTION.md`](APOTEK-CREDIT-NOTE-OWNERSHIP-RESOLUTION.md).

**Status:** Decision accepted 2026-08-18. Canonical artifacts were updated under [`ADR-APT-003`](../adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md). This report remains the pre-decision impact map.

**Scope:** Outpatient Apotek (`Pelayanan Obat Pasien`) — impact of changing Invoice mutability from **immutable after leaving `Established` (Issued / financially settled)** to **mutable while Tata Rekening financial status permits**.

**Task type:** Analysis only. No code, schema, workflow, or canonical-rule change is produced by this report.

**Ownership constraint (binding for this analysis):** Tata Rekening remains the owner of financial decisions. Apotek only **consumes** financial status / mutation permission from Tata Rekening. Apotek does **not** own Financial Clearance as an object, does **not** own Financial Verification / Close / Finalize / Reopen rules, and must **not** invent a local substitute for those rules.

**Repository state:** Analyzed from canonical Apotek artifacts. Target `ApotekContext` implementation is absent; legacy `PenjualanModel` / DU-Bill remains.

**Related reports:** [`APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md`](APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md) (terminology and Invoice vs registration financial state); [`APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md`](APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md) (Credit Note is collaborator, not fulfillment ledger); [`docs/contexts/TataRekening/04-sop.md`](../../TataRekening/04-sop.md) (Charge Source mutability while Billing is `OPEN`).

---

## 1. Executive Summary

Current Apotek artifacts make **Invoice content immutable after it leaves `Established`**. That gate is owned by the **Apotek Invoice lifecycle** (`Issued`, `Financially Cleared`, `Cancelled`, `Adjusted or Credited`, `Resolved`). Corrections after that point are **not** in-place rewrites. They are **Credit Note**, **Refund**, **Financial Adjustment**, or another accountable commercial outcome, plus Integration Task `BillingCredit`. Persistence decision **PD-05** hardens the same rule at the table level.

That Apotek-owned freeze is **stricter and earlier** than Tata Rekening Charge Source policy. Tata Rekening allows Charge Sources (including Farmasi) to **form, change, or cancel** Financial Charge while registration Billing is **`OPEN`**, including after **Reopen Billing** (`SOP-TR-09`). After **`CLOSED`**, Charge Sources must not produce new charges until Reopen. **`FINALIZED` / `LUNAS`** require Cancel Finalization / Reopen / Cashier paths that Apotek must not own.

The proposed change therefore does **not** mainly “unlock invoices after payment.” It **relocates the mutability gate** from Apotek `InvoiceStatus` to a **permission consumed from Tata Rekening**. That is a different pattern from today’s artifacts, which evaluate Dispense Authorized locally from snapshotted Payment/Coverage evidence and post charges **asynchronously** (BA-07). It is closer to Lab’s “ask billing before acting” shape, applied to **Invoice rewrite**, not to result release.

Credit Note does **not** disappear. Payment Clearance (Cashier) and Tata Rekening Billing status are **independent**. A General Patient Invoice can be **Issued and paid** while registration Tata Rekening is still **`OPEN`**. Line rewrite may then be financially permitted by Tata Rekening and still require **Refund** because cashier settlement already exists. Unfulfilled Medication Outcome, Sales Order item identity, Dispensing history, and Final Dispense Review records remain independent of Invoice rewrite.

**Recommendation:** Treat the change as a **bounded-context contract + ADR** before rewriting `BR-APT-027` / PD-05. Define an explicit **consumed permission** (not a Financial Clearance aggregate). Keep Credit Note / Refund for **settled-payment** and **TR-forbidden** cases. Do not infer mutation permission from Invoice `Financially Cleared` or from Payment Clearance.

---

## 2. Current Invoice Mutability Assumptions Found in Artifacts

### 2.1 Canonical freeze rule (Apotek-owned)

| Source | Assumption |
|--------|------------|
| **BR-APT-027** — [`apotek-domain.md`](../apotek-domain.md) §7.3 | An **issued or financially settled** Invoice shall be corrected through accountable **Financial Adjustment, Credit Note, or Refund**, **not silent replacement**. |
| **PD-05** — [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) §14.1 | Rewrite (delete+insert of items/charges) **only while `InvoiceStatus = Established`**. After leaving `Established` (wording includes `Issued`, `Paid`, `Cancelled`, `Closed`, or any later state), **content is immutable**. Post-Established correction shall **not modify original invoice rows**. |
| Persistence §8.5 / §4.2 / §9.2 | Same: rewrite while `Established`; after that, header commercial fields and item content immutable; `BILRG_AptCreditNote` insert-only. §4.2 also says “rewriteable **until Issued**,” which is consistent with freeze at Issue, not at Financially Cleared. |
| Invoice lifecycle §8.3 | `Established` → `Issued` → `Financially Cleared` → `Resolved`; `Established` or `Issued` → `Cancelled`; `Issued` or `Financially Cleared` → `Adjusted or Credited` → `Resolved`. **Adjusted or Credited is a lifecycle branch, not an in-place edit.** |
| Glossary **Pricing Snapshot** | **Immutable** commercial basis **when the Invoice is established**. **BR-APT-025** requires retaining Pricing Snapshot and Payer at establishment. |
| Glossary **Credit Note** | Commercial document **reducing or reversing an issued Invoice amount**. |
| Domain event **Invoice Issued** | Invoice became an **authoritative** commercial document. |
| Domain event **Invoice Credited** | Credit Note reduced or reversed an Invoice consequence. |

**Net current model:** `Established` is draft-like and rewriteable. **Issue is the Apotek freeze.** `Financially Cleared` is a later Invoice state fed by **Payment Clearance or Coverage Clearance**; it is **not** the freeze trigger, and it is **not** Tata Rekening `CLOSED` / `FINALIZED` / `LUNAS`.

### 2.2 Informal “financial clearance” vs freeze

[`APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md`](APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md) already records that **Financial Clearance is not an Apotek object** (BA-08). Phrases in **BR-APT-046**, **BR-APT-118**, and SOP-003 §5.4 mean *payer evidence sufficient for Dispense Authorized / after payment*, **not** a persisted clearance entity.

Those rules still **couple non-fulfillment to Credit Note/Refund**. They do **not** say “rewrite the Issued Invoice.” The proposed mutability change would split that coupling: **TR-permitted rewrite** vs **Credit Note when rewrite is forbidden or payment is already settled**.

### 2.3 Workflow and SOP freeze language

| Artifact | Assumption |
|----------|------------|
| [`outpatient-apotek-workflow.md`](../outpatient-apotek-workflow.md) WF-003 | If an **issued or financially cleared** Invoice needs correction: Financial Adjustment, Credit Note, or Refund **under Tata Rekening authority**; **do not silently replace**. Incomplete payment: preparation blocked; **cancel only while lifecycle permits**. |
| WF-003 / WF-007 | Paid shortage / paid No-Show: Credit Note or Refund; **do not silently erase** a paid commercial consequence or treat it as the uninvoiced BPJS path. |
| WF-007 | General Patient Invoice **paid** → Tata Rekening supplies Credit Note/Refund; Sales Order stays `Active` until then. BPJS No-Show: **no Invoice** to cancel. |
| [`SOP-APT-RJ-003`](../sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md) §5.3 | Cancel only when displayed Invoice lifecycle permits; otherwise Tata Rekening supplies Adjustment / Credit Note / Refund. |
| SOP-003 §5.4, SOP-004 shortage, SOP-007 | Commercial correction is **supplied by Tata Rekening**, displayed by Pharmacy System; Pharmacy does not rebuild the Sales Order. |
| Screen design §3.2 | **Correction Request:** post-payment / post-handover correction **must not silently replace history**. **Financial Consequence Pending:** Credit Note, Refund, or other Tata Rekening outcome outstanding. Return/correction is a **request**, not a free reverse. |

Indonesian companions (`apotek-domain-id.md`, `outpatient-apotek-workflow-id.md`, SOP-ID files) repeat the same freeze and Credit Note path.

### 2.4 Integration-contract freeze language

| Artifact | Assumption |
|----------|------------|
| Persistence §11 `BillingCharge` | Fired on **Invoice Issued** (General) or **BPJS invoice at handover**. Idempotency `{InvoiceId}:CHARGE`. One create-oriented charge task per Invoice. |
| Persistence §11 `BillingCredit` | Fired when **Credit Note recorded**. Idempotency `{InvoiceId}:CN{n}`. Compensating charge — **implies original charge rows stay**. |
| BA-07 | Async Integration Task Table; **no** distributed transaction; Apotek does **not** currently define a synchronous “may I mutate this charge?” call to Tata Rekening. |
| Financial Clearance analysis §3.4 | Apotek evaluates **snapshotted** evidence locally; Tata Rekening posting is async. **Lab’s synchronous billing-release validation is not adopted** for dispensing. |

### 2.5 What current artifacts do **not** freeze (must not be confused with Invoice immutability)

| Concern | Mutability today | Owner |
|---------|------------------|--------|
| Invoice items/charges while `Established` | Rewrite allowed (PD-05) | Apotek |
| `BILRG_AptCreditNote` | Append-only | Apotek document; TR/Cashier for financial effect |
| Payment Clearance snapshot on Invoice | Evidence fields filled when known | Cashier owns the evidence |
| Dispensing / Final Dispense Review | Reviews append-only; failed review does not erase prior records | Apotek |
| Sales Order items after establishment | No medication-identity rewrite; Unfulfilled Outcome append-only | Apotek |
| Outpatient Queue Mapping | Update in place | Apotek association |
| Tata Rekening Billing Set | Mutable for Charge Source while **`OPEN`**; frozen after **`CLOSED`** until Reopen | Tata Rekening |

**ADR-APT-001** and **ADR-APT-002** do **not** decide Invoice mutability. They own queue vs pharmacy state, and Stock Ledger vs Dispensing. Invoice freeze lives in domain + PD-05, not in those ADRs.

### 2.6 Existing wording inconsistencies (already in artifacts)

These are not caused by the proposed change, but they will confuse a mutability rewrite if left unfixed:

1. **PD-05** lists later states `Paid` / `Closed`; domain Invoice states are `FinanciallyCleared` / `AdjustedOrCredited` / `Resolved` / `Cancelled`.
2. Persistence §4.2 “until Issued” vs PD-05 “only while Established” — same freeze if Issue is the first non-Established state; **Cancelled-from-Established** is a PD-05 later state that would also freeze.
3. **BR-APT-027** says issued **or financially settled**; PD-05 freezes at **any** leave of `Established`, so **Issued-but-unpaid** is already immutable.
4. Informal *financial clearance* vs Invoice `Financially Cleared` vs Tata Rekening statuses — three different things ([`APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md`](APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md)).

---

## 3. Proposed Model (analysis framing only)

**From:** Apotek decides mutability from **InvoiceStatus** (`Established` rewriteable; after Issue / financial settlement, Credit Note only).

**To:** Apotek **asks Tata Rekening** whether the registration Billing (and the existing Financial Charge, if any) **currently permits Charge Source mutation**. If permitted, Apotek may change Invoice commercial content (and emit the corresponding charge change). If not permitted, Apotek must not rewrite rows; it uses Credit Note / wait / display pending TR outcome as today.

**Apotek still owns:** Invoice identity, Sales Order traceability, Pricing Snapshot retention rules (unless explicitly revised), payer-path timing (General vs BPJS), Dispense Authorized evaluation, Dispensing, Unfulfilled Medication Outcome.

**Apotek still does not own:** Close Bill, Financial Verification, Financial Adjustment policy, Finalize, Cancel Finalization, Reopen Billing, Settlement Initiation, Payment Settlement, or any object named Financial Clearance.

**Tata Rekening still owns:** whether Charge Source may form / change / cancel Financial Charge (`OPEN` vs `CLOSED` vs later states, Reopen, Cancel Finalization).

---

## 4. Rules and Definitions That Would Require Revision

### 4.1 Must revise (directly encode Apotek-owned freeze)

| ID / name | Change needed |
|-----------|----------------|
| **BR-APT-027** | Replace “issued or financially settled → Credit Note / Adjustment / Refund, never silent replacement” with a **two-path** rule: (1) if Tata Rekening **permits Charge Source mutation**, Apotek may change Invoice commercial content under that permission and retain accountability (actor, effective time, correlation); (2) if Tata Rekening **does not permit** mutation, keep Credit Note / Refund / wait-for-TR. **Do not** key the rule on Invoice `Issued` or `Financially Cleared` alone. |
| **PD-05** | Rewrite policy must no longer be “only while `Established`.” Either: rewrite while TR permission is granted (including after Issue), **or** a new persistence decision (PD-0x) supersedes PD-05. Specify whether post-Issue rewrite is still delete+insert or in-place update + `Version`. |
| Persistence §4.2, §8.5, §9.2 | Consistency boundary text “rewriteable until Issued / while Established; then immutable” and the reconstruction exception table. |
| **Credit Note** glossary | Today: the normal way to change an **issued** amount. After change: residual instrument when mutation is **not** permitted, and/or when **cashier settlement** must be reversed even if TR is `OPEN`. |
| **Invoice Issued** event meaning | Today implies **authoritative frozen** commercial document. After change: Issue still means “posted / collectable / charge-emitted,” **not** “rows frozen.” |
| Lifecycle **Adjusted or Credited** | Today the only post-Issue correction branch. After change: need a distinct **Invoice Revised** (in-place / rewrite under TR permission) vs **Credited** (compensating document). |
| **BR-APT-071** | Issued/cleared cancel currently follows BR-APT-027. Cancel-in-place vs Credit Note must follow **TR permission**, not InvoiceStatus alone. |
| **BR-APT-046**, **BR-APT-118**, **BR-APT-080** | Today always pair non-fulfillment after payment/clearance with Credit Note/Refund. After change: Credit Note/Refund remain when money was taken or TR forbids mutation; **quantity/value correction of the Invoice** may be a TR-permitted rewrite instead of a Credit Note **if and only if** that is the Charge Source change TR expects. Unfulfilled Medication Outcome is **unchanged** (still required; Credit Note is not the fulfillment ledger). |
| **BR-APT-051** | “Shortage shall not **erase** an existing Invoice” remains valid. Clarify that **erase** ≠ **revise lines under TR permission**. Invoice identity stays. |
| **BR-APT-025** / Pricing Snapshot | Today snapshot is immutable at establishment. Tata Rekening **SOP-TR-09** says Reopen **does not change Pricing Snapshot of existing Financial Charge**. If Apotek rewrites items after Issue, decide whether snapshot stays, is versioned, or is re-taken — **without Apotek inventing TR pricing rules**. Align with TR, do not override them. |
| Screen **Correction Request** / “must not silently replace history” | “Silent” must be redefined: operator-visible, authorized, TR-permitted rewrite may be allowed; silent SQL overwrite without permission/audit remains forbidden. |

### 4.2 Likely revise (wording assumes Credit Note is the only post-Issue path)

| ID / name | Why |
|-----------|-----|
| **BR-APT-045** | Paid/financially cleared Invoice still does not guarantee fulfillment — keep. Do not treat `Financially Cleared` as a mutability lock. |
| **BR-APT-028** | Source Traceability Invoice → Tata Rekening must survive **revision** of the same Invoice, not only original Issue + Credit Note. |
| **BR-APT-070** | Purchase Confirmation is evidenced by **establishing** the Invoice. If lines change after Issue, decide whether **re-confirmation** is required (workflow), independent of TR permission. |
| **BR-APT-041** | Payment Clearance still must not be inferred from Invoice existence. After mutation, snapshot `PaymentClearanceReff` / amounts may be **stale** relative to new totals. |
| **BR-APT-056 / 057** | Commercial vs fulfillment independence stays. Mutation of Invoice must not complete Dispensing or Unfulfilled Outcome. |
| **BR-APT-058 / 060** | Accountable actor/time and non-erasure of completed outcomes: if items are rewritten, **append-only commercial history** (or audit) is needed so completed Credit Notes / prior issued amounts are not lost. |
| **BR-APT-075 / 079 / 092** | BPJS Invoice-at-handover timing unchanged. Mutability window after handover may be short if registration is already approaching Close Bill. |
| **BR-APT-122** | Patient-Pay still needs Payment Clearance for Dispense Authorized — orthogonal to Invoice row freeze. |

### 4.3 Do not revise as Invoice-mutability rules (false friends)

| ID / name | Why leave as-is |
|-----------|-----------------|
| **BA-08 / BR-APT-043** | Financial Clearance / Fulfillment Clearance remain **retired objects**. Consuming TR **mutation permission** is not recreating Financial Clearance. |
| **BR-APT-096** and Final Dispense Review immutability | Different aggregate; append-only reviews stay. |
| **BR-APT-018 / Unfulfilled Outcome table** | Fulfillment quantity ledger; see unfulfilled-outcome analysis. |
| **ADR-APT-001 / ADR-APT-002** | Queue and Stock Ledger boundaries. |
| **BR-APT-083** | Users still must not key in free-form Invoice Items; mutation still from Sales Order Items. |
| Mapping rules **BR-APT-062** etc. | Mapping still must not target Invoice. |

### 4.4 Business assumptions that would break if freeze is dropped without TR consumption

1. **Issue = financial document freeze** (PD-05 rationale: Established is draft; after Issue, auditability via Credit Note).
2. **One `BillingCharge` per Invoice identity** is enough; later money movement is always **compensating Credit**.
3. **Pharmacy screens never edit Issued lines**; they raise Correction Request and wait for TR/Cashier.
4. **Sales Order stays Active until Credit Note/Refund** for paid No-Show (**BR-APT-080**) — may still be true for cashier, even if Invoice lines can change under `OPEN`.
5. **Apotek does not call Tata Rekening synchronously** for permission (current BA-07 + local snapshots).

---

## 5. Workflow Impacts

### 5.1 `WF-APT-RJ-003` — General Patient

| Step / exception | Current | If TR-permitted mutation |
|------------------|---------|---------------------------|
| Amount change **before** establishment | Re-confirm verbally; no Invoice yet | Unchanged |
| Invoice Established then unpaid | Cancel if lifecycle permits; preparation blocked | Cancel/revise still need **TR permission** once `BillingCharge` exists or Issue has occurred; unpaid `Established` may remain local rewrite |
| Issued / paid, amount or item error | Credit Note / Adjustment / Refund; **no silent replace** | If TR `OPEN` (or Reopened): Pharmacy **may** revise Invoice **as Charge Source**, then emit charge **update** (new task type — not specified today). If Payment Clearance already exists, **Cashier Refund** may still be required even when TR allows charge change |
| Shortage after payment | Unfulfilled Outcome + Credit Note/Refund | Unfulfilled Outcome **always**. Invoice: rewrite vs Credit Note **according to TR permission**, not according to “already Issued” |
| Completion criterion “Invoice visibly financially cleared” | Invoice state | Unchanged as **payment/coverage evidence**; not a freeze flag |

**Purchase Confirmation:** If Issued quantities/prices change, WF-003 does not currently re-open the confirmation conversation. That is a **workflow gap** the proposed model must close independently of TR.

### 5.2 `WF-APT-RJ-004` — BPJS

Invoice exists only at **successful handover**. Freeze today starts almost immediately after Issue + `BillingCharge`.

- No-Show **before** handover: still **no Invoice** — mutability change has **no effect**.
- Correction **after** handover: today Credit Note. After change: Charge Source rewrite only while TR permits. Outpatient BPJS often approaches registration Close sooner than a long General Patient stay; the **practical mutation window may be small**.
- Failed Final Dispense Review **before** Invoice: unchanged (no Invoice).

### 5.3 `WF-APT-RJ-005` — Mixed coverage

Two Invoices, independent freeze today. After change, **each charge** is permitted or blocked by the **same registration** Tata Rekening status, but **Payment Clearance applies only to Patient-Pay**. A TR `OPEN` rewrite of the General Invoice can proceed while the BPJS Invoice is still absent (or vice versa after handover). Mixed SOP language that the General Invoice is “financially cleared” and the BPJS Invoice is “handover-time” remains true as **timing**, not as freeze.

### 5.4 `WF-APT-RJ-006` — Coordinated queues

No Invoice freeze rule except “do not merge Invoices.” Unchanged.

### 5.5 `WF-APT-RJ-007` — Uncollected medication

| Path | Current | Impact |
|------|---------|--------|
| BPJS uninvoiced | No Invoice | None |
| General Patient paid | Wait for Credit Note/Refund; SO `Active` | If TR permits Charge Source cancel/change, Pharmacy might **revise or cancel Invoice lines** as the Charge Source step **after or during** Reopen — but **Refund** remains Cashier-owned. Do not treat TR `OPEN` as “money returned.” |
| “Must not silently erase paid consequence” | Protects against treating paid as BPJS-uninvoiced | Keep. Rewrite under TR permission is not erasure of payer path. |

### 5.6 `WF-APT-RJ-002` — Accept demand

Invoice formation independent of Dispensing (**BR-APT-015**) stays. Mutability of a later Invoice does not allow rebuilding Sales Order items after establishment (SOP-003 §5.4 / BC-10). **Do not** smuggle Sales Order rewrite through Invoice mutation.

### 5.7 Compensations and handoffs

Workflow already hands commercial correction to **Tata Rekening**. The proposed model **narrows** when Pharmacy is the Charge Source **editor** (while TR says `OPEN`) vs when Pharmacy is only a **requester** (while `CLOSED` / `FINALIZED`). SOPs that always say “Tata Rekening supplies Credit Note” would be **too coarse**: Verifikator Reopen (**SOP-TR-09**) is what **re-enables** Charge Source edits; Credit Note/`BillingCredit` remains the path when Charge Source must **not** rewrite.

---

## 6. Aggregate and Persistence Impacts

### 6.1 `Invoice` aggregate

Today the aggregate keeps items, charges, snapshot, payer, Credit Notes, refunds, and Financial Charge outcome consistent, with **post-Established item immutability**.

Required if mutation is TR-gated:

- A **consumed permission snapshot** at command time (status, correlation, denied reason) — **audit/trace only**, not a Financial Clearance aggregate, not a source of truth (same discipline as BA-08 / Lab `LastBillingRelease*`).
- **Concurrency:** `Version` already exists on `BILRG_AptInvoice`. Post-Issue rewrite needs optimistic concurrency against both Apotek rows and the posted `TataRekeningChargeId`.
- **History:** PD-05 currently allows **delete+insert** only in `Established` with **no item history table**. Post-Issue rewrite without history **violates** the audit rationale of PD-05 unless **audit log** or **revision/credit** rows remain.
- **Cancellation:** `Cancelled` from `Issued` today is a lifecycle transition without describing row rewrite. Under TR `OPEN`, cancel may mean charge cancel in Tata Rekening plus Apotek status change — still not “delete header.”

### 6.2 Persistence objects

| Object | Current | Impact |
|--------|---------|--------|
| `BILRG_AptInvoice` | Status, snapshot time, payment evidence, `TataRekeningChargeId` | Status machine may need **Revised** or reuse `Issued` after rewrite. Charge id may stay (in-place bill update) or rotate (forbidden by current idempotency `{InvoiceId}:CHARGE`). |
| `BILRG_AptInvoiceItem` / `ItemCharge` / `InvoiceCharge` | Rewrite only in `Established` | PD-05 exception table must change. Decide insert-only revisions vs delete+insert. |
| `BILRG_AptCreditNote` | Append-only, `BR-APT-027` | Remain for TR-forbidden and/or refund-linked corrections. Must not be the **only** post-Issue path. |
| `BILRG_AptIntegrationTask` | `BillingCharge`, `BillingCredit` | Need **charge update / cancel** task types, new idempotency keys, reconciliation of **mutated** totals vs `ta_trs_billing`. |
| `BILRG_AptUnfulfilledOutcome` | Append-only on Sales Order | **Keep.** Invoice mutation does not replace it. |
| `BILRG_AptFinalReview` | Insert-only | Unrelated. |

### 6.3 Neighbor persistence

- **`ta_trs_billing`:** today assumed created once per Invoice Issue. In-place Charge Source change while `OPEN` is **already** Tata Rekening policy; Apotek simply never used it after Issue.
- **Cashier payment rows:** not owned by Apotek; mutation of Invoice totals after Payment Clearance can **desynchronize** `PaymentClearanceReff` vs `GrandTotal`.
- **Legacy DU (BA-05):** still independent; do not dual-write. Mutability change applies only to the **new Invoice**.

### 6.4 Repository / reconstruction

`IInvoiceRepo` save semantics today: rewrite children only in `Established`. Command handlers would need **pre-save TR permission** (application port). That is an **inbound** call, unlike today’s outbound-only billing tasks. BA-07 does not forbid inbound queries; it forbids treating async tasks as a two-phase commit.

---

## 7. Credit Note Implications

### 7.1 Current role

Credit Note is the **Apotek commercial compensating document** for issued/settled Invoices. It is:

- Required (with Unfulfilled Outcome) after financial clearance then non-fulfillment (**BR-APT-046**).
- Required (with Refund) for paid General Patient No-Show until SO can `Resolve` (**BR-APT-080**).
- The persistence expression of **BR-APT-027** (`BILRG_AptCreditNote` insert-only).
- The trigger for Integration Task **`BillingCredit`**.
- A **collaborator**, not a substitute, for Unfulfilled Medication Outcome ([`APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md`](APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md)).

### 7.2 What must not happen

- **Do not** drop Credit Note because TR `OPEN` allows Charge Source edits. Settled cashier payments still need Refund/Credit Note semantics.
- **Do not** use Credit Note as the Unfulfilled quantity ledger.
- **Do not** let Pharmacy invent Credit Note **approval** rules that belong to Tata Rekening / Cashier.

### 7.3 Revised role (recommended split)

| Situation | Invoice commercial document | Money already taken (Payment Clearance) | Suggested instrument |
|-----------|----------------------------|------------------------------------------|----------------------|
| TR permits Charge Source mutation; unpaid or charge-only | May rewrite Invoice; update Financial Charge | No | Charge update; Credit Note optional/unused |
| TR permits mutation; **paid** | May rewrite Invoice **and/or** Credit Note — **open question** (see §11) | Yes | Refund still required via Cashier; Credit Note may remain the pharmacy-visible commercial offset |
| TR **forbids** mutation (`CLOSED` / `FINALIZED` / `LUNAS` without Reopen/Cancel) | **No** row rewrite | Either | Credit Note request / Financial Adjustment / wait Exception Worklist — **current SOP path** |
| BPJS never invoiced | No Invoice | N/A | No Credit Note |

**Screen:** Exception category **Financial Consequence Pending** stays valid. **Correction Request** should show **why** rewrite is blocked (consumed TR status), not a pharmacy-owned clearance flag.

---

## 8. Invoice Lifecycle Implications

Current machine (§8.3):

```text
Established -> Issued -> Financially Cleared -> Resolved
Established or Issued -> Cancelled
Issued or Financially Cleared -> Adjusted or Credited -> Resolved
```

### 8.1 States that stay (meaning)

| State | Keep as |
|-------|---------|
| `Established` | Medication Sale formed; still the natural rewrite window **before** charge emission |
| `Issued` | Authoritative for collection / `BillingCharge`; **no longer means frozen rows** |
| `Financially Cleared` | Payment Clearance and/or Coverage Clearance evidence reflected — **not** TR Close/Finalize, **not** mutation lock |
| `Cancelled` | Document ended while still allowed |
| `Resolved` | Commercial consequences final **including** credits/refunds/revisions |
| `Adjusted or Credited` | Compensating-document path |

### 8.2 Gaps the current machine does not express

1. **`Issued` then revised under TR permission** — still `Issued`? Needs `RevisedAt` / revision count? Or always jump to `Adjusted or Credited` even without a Credit Note row? The last option **collapses** two different facts (rewrite vs credit) and should be avoided.
2. **`Financially Cleared` then line rewrite** — payment evidence may be invalid; state might need to **leave** `Financially Cleared` until Cashier re-clears. Artifacts do not define that demotion.
3. **`Cancelled` after Issue** while TR `OPEN` vs Credit Note while TR `CLOSED` — two implementations of “no remaining charge,” one lifecycle name.

Issue and Financially Cleared should remain **commercial/payment facts**. **Mutability is not a new InvoiceStatus.** Putting `Mutable` / `Frozen` on the Invoice would **re-create a local financial gate** and violate the ownership constraint.

---

## 9. Tata Rekening Integration Implications

### 9.1 What Tata Rekening already allows (Charge Source)

From [`04-sop.md`](../../TataRekening/04-sop.md) §3.3 and [`SOP-TR-09`](../../TataRekening/SOP-TR-09%20—%20Reopen%20Billing.md):

- **`OPEN`:** Charge Sources (including Farmasi) may **form, change, or cancel** Financial Charge.
- **`CLOSED`:** no **new** Financial Charge; Financial Control (verify/adjust/allocate/finalize).
- **Financial Adjustment (SOP-TR-05):** correct Financial Truth **without changing Charge Source operational history**, unless Reopen is required.
- **Reopen:** Verifikator returns Billing to `OPEN` so Charge Source can change charges; then Close and Financial Control repeat.
- **`FINALIZED` / `LUNAS`:** Charge Source must not treat these as editable; Cancel Finalization / settlement rules are TR/Cashier.

**Alignment insight:** Proposed Apotek mutability **matches Charge Source rights Tata Rekening already documents**. Current Apotek PD-05 **refuses** those rights after Issue even while Billing is `OPEN`.

### 9.2 What Apotek must consume (not own)

Apotek needs a **query/port**, for example: “For this registration / this `TataRekeningChargeId`, may Charge Source **mutate** the Financial Charge?”

- Owner of the **answer:** Tata Rekening.
- Owner of **when** Close/Reopen happens: Verifikator / TR SOPs.
- Apotek **stores** at most last-checked status for audit, then **re-queries** at the next command (same pattern as Lab release eligibility: truth stays in billing).

Apotek must **not** implement Close/Open heuristics locally (e.g. “if Invoice is not Financially Cleared, allow rewrite”).

### 9.3 Integration Task contract today vs needed

| Task | Today | Gap under mutation |
|------|-------|--------------------|
| `BillingCharge` `{InvoiceId}:CHARGE` | Create once at Issue / BPJS handover | Idempotency **blocks** a second create. Mutation cannot reuse this key as a new insert. Need **update** semantics or a new key scheme **owned with TR** |
| `BillingCredit` `{InvoiceId}:CN{n}` | Compensating charge | Still required when Credit Note is used; **must not** double-apply with an in-place charge update |
| *(missing)* Charge update / cancel | — | Required for TR-permitted Invoice rewrite/cancel **without** Credit Note |

BA-07 still applies: Apotek transaction commits Invoice change **and** task atomically; worker updates `ta_trs_billing` asynchronously. **Permission check must be synchronous and prior** to that commit, or Apotek will persist a rewrite Tata Rekening will reject (task `Failed` / `Dead`). Reconciliation must include **stale permission** (closed between check and worker).

### 9.4 Scope mismatch (Invoice vs registration)

A pharmacy Invoice is **one Charge Source document**. Tata Rekening status is **registration Billing Set**. Permission is therefore **registration-scoped**:

- Mutating one Apotek Invoice while `OPEN` is allowed by TR even if **other** charges exist.
- `CLOSED` blocks **all** Charge Sources, not only pharmacy.
- Pharmacy Invoice `Financially Cleared` can coexist with TR `OPEN` ([`APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md`](APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md) §3.3).

### 9.5 SOP-TR-05 vs pharmacy rewrite

If Verifikator can correct Financial Truth **without** Charge Source history change, Pharmacy **must not** rewrite Invoice. If Adjustment **requires** Charge Source change, **Reopen** comes first, then Pharmacy mutates. Apotek consumes that sequence; it does not decide it.

### 9.6 Lab contrast (do not copy blindly)

Lab asks BIL whether **results may be released**. The proposed Apotek call asks whether **charges may be mutated**. Different question, similar **ownership** (billing owns the answer; operational context stores trace only). Do **not** reuse Lab “Financial Clearance” vocabulary (retired in Apotek BA-08; obsolete in Lab domain).

---

## 10. Required Artifact Updates

Canonical documents to change **if** the proposal is accepted (not done in this report). English + Indonesian pairs must stay in semantic parity.

| Artifact | Update |
|----------|--------|
| [`apotek-domain.md`](../apotek-domain.md) / [`apotek-domain-id.md`](../apotek-domain-id.md) | Glossary (Credit Note, Pricing Snapshot, Invoice Issued); **BR-APT-027** and dependent rules (§4); §5.3 / §6 Invoice aggregate; §8.3 lifecycle notes; events (`Invoice Revised` or equivalent); §1.3 ownership: **consume TR mutation permission**, do not own it |
| [`outpatient-apotek-workflow.md`](../outpatient-apotek-workflow.md) / `-id.md` | WF-003 / 004 / 005 / 007 exceptions and compensations; cross-context table (Apotek ↔ Tata Rekening); event lists |
| [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) | §4.2, §8.5, §9.2, §11 task catalog, **PD-05 supersession** |
| [`outpatient-apotek-screen-and-aggregate-design.md`](../outpatient-apotek-screen-and-aggregate-design.md) | Pelayanan Penjualan workbench (edit Issued invoice only when permission displayed); Exception Worklist Correction Request / Financial Consequence Pending; §5 Invoice ownership row |
| SOP-003, SOP-004, SOP-005, SOP-007 (EN+ID) | Correction/shortage/No-Show: Charge Source rewrite vs wait-for-Reopen vs Credit Note/Refund |
| [`DAFTAR-SOP-APT-RJ.md`](../sop/DAFTAR-SOP-APT-RJ.md) | Glossary if Credit Note role changes |
| **New ADR** (recommended `ADR-APT-003`) | Invoice mutability gated by **consumed** Tata Rekening Charge Source permission; BA-08 Financial Clearance stays retired; PD-05 replaced; Credit Note retained for blocked/settled paths |
| [`../working/outpatient-apotek-repository-gap-analysis-report.md`](../working/outpatient-apotek-repository-gap-analysis-report.md) | Note on MI-03 / BA-07: inbound permission query; new billing task types — **as a follow-on decision**, not a silent edit of ratified BA-07 delivery style |
| [`APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md`](APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md) | §6 Invoice mutability implications become stale |
| [`APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md`](APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md) | Credit Note still collaborator; add rewrite-vs-credit distinction |
| [`APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md`](APOTEK-ARTIFACT-ALIGNMENT-REVIEW.md) | Would need a new ALN if documents diverge during rollout |
| Tata Rekening API / design (outside Apotek but **blocking**) | Stable **Charge Source mutation permission** contract Apotek can consume; charge **update/cancel** aligned with `AddBill` / reopen |
| [`docs/ARTIFACTS.md`](../../ARTIFACTS.md) | Index this report (and the ADR when written) |

**No change required** solely for this proposal: ADR-APT-001, ADR-APT-002, SOP-001 mapping, Available Stock / Stock Ledger reports, queue-mapping reports, ALN-001–007 resolution reports (historical), Unfulfilled table KEEP conclusion.

---

## 11. Risks, Open Questions, and Recommendations

### 11.1 Risks

| Risk | Severity | Note |
|------|----------|------|
| Recreating **Financial Clearance** as an Apotek status or table | High | Permission must be consumed and re-evaluated; not a source of truth |
| Inferring permission from Invoice `Financially Cleared` or Payment Clearance | High | Those are cashier/coverage facts; TR may still be `OPEN` or already `CLOSED` |
| In-place rewrite **after payment** without Refund | High | Patient paid amount ≠ new GrandTotal |
| Double application: Invoice rewrite **and** Credit Note **and** `BillingCredit` | High | Need exclusive correction paths |
| Idempotency `{InvoiceId}:CHARGE` vs later update | High | Worker may no-op or duplicate |
| Check-then-act race: TR Closes between permission and worker | Medium | Task failure + operator visibility; do not hide |
| Losing audit by delete+insert after Issue | High | Contradicts PD-05 rationale |
| Sales Order / Unfulfilled / Dispensing rewritten “to match” Invoice | High | Violates BC-10, BR-APT-018, independent lifecycles |
| BPJS Invoice rewrite after handover while registration already in Financial Control | Medium | Permission will usually be **deny**; UX must explain wait for Verifikator |
| Purchase Confirmation stale after Issued-line change | Medium | Patient agreed to a different amount |
| Indonesian/English SOP drift during a large wording change | Medium | Steward both languages together |

### 11.2 Open questions (must be decided before implementation)

1. **Exact permission contract:** registration status only (`OPEN` vs not), or per-bill “this `fs_kd_trs` may be updated”? SOP text is registration-scoped Charge Source rights.
2. **Paid + `OPEN`:** is Charge Source rewrite allowed, with a **separate** Cashier refund, or is Credit Note **mandatory** whenever Payment Clearance exists regardless of TR status?
3. **Pricing Snapshot** after rewrite: frozen at first establishment (aligned with SOP-TR-09 for existing charges) vs new snapshot — who decides, TR or Tarif/Apotek?
4. **Must Pharmacy wait for `BillingCharge` success** before a first mutation, or can `Established` still rewrite locally before Issue?
5. **History model:** audit log only vs Invoice revision table vs always Credit Note for after-Issue even when TR `OPEN` (that last option **abandons** the proposed change).
6. **Demotion of `Financially Cleared`** after amount change.
7. **Re-confirmation** of General Patient after Issued mutation.
8. Does Tata Rekening already expose a query suitable for Charge Source, or is a new API required? Apotek artifacts must not specify TR internals beyond the consumed contract.
9. Interaction with **SOP-TR-05** adjustments that change allocation **without** Charge Source edit — Pharmacy must show “no Invoice action.”
10. **Jual Bebas** vs Resep Kerja: same Invoice aggregate; any retail-specific freeze?

### 11.3 Recommendations

1. **Accept the direction as Charge Source alignment**, not as Apotek taking financial authority. Write **ADR-APT-003** with the ownership constraint in this report’s header.
2. **Keep three correction channels explicit in domain language:**
   - **Local rewrite** while Invoice is `Established` and no Financial Charge exists (today’s PD-05 window).
   - **Charge Source mutation** when Tata Rekening **permits**.
   - **Credit Note / Refund / wait** when Tata Rekening **forbids** mutation, or when **Payment Settlement** already happened (until question 2 is answered).
3. **Do not** add InvoiceStatus `Mutable`/`Frozen`. Put mutability on **command guards** that call Tata Rekening.
4. **Do not** weaken Unfulfilled Medication Outcome, Sales Order immutability after establishment, or Final Dispense Review append-only rules.
5. **Extend BA-07** with inbound permission + outbound charge update/cancel **without** introducing distributed transactions.
6. **Fix PD-05 state-name drift** in the same persistence edit that supersedes the freeze.
7. **Screen:** show consumed TR status on Pelayanan Penjualan; disable line edit with an explanatory block when denied; keep Exception Worklist for denied + paid-unfulfilled cases.
8. **Sequence work:** (a) TR permission + charge-update contract, (b) domain/SOP/ADR wording, (c) persistence PD-05 replacement, (d) screens. Do not implement Apotek rewrite against Issue-only freeze while TR contract is unspecified.
9. **Leave Lab Financial Clearance vocabulary out of Apotek.**

### 11.4 Decision this report does **not** make

Whether the hospital **should** allow pharmacy operators to edit Issued invoices whenever Billing is `OPEN` is an operational policy choice. Artifacts today **forbid** it. Tata Rekening **already allows** Charge Source change in that window. This report only maps the impact of moving Apotek to consume that existing TR rule.

---

## 12. Artifact Footprint Index (Invoice freeze / Credit Note / Issue)

| Artifact | Freeze / Credit Note / Issue dependency |
|----------|----------------------------------------|
| `apotek-domain.md` / `apotek-domain-id.md` | BR-APT-027, 025, 028, 045, 046, 051, 070, 071, 080, 118; glossary; §6 Invoice; §8.3; events Invoice Issued / Credited |
| `outpatient-apotek-workflow.md` / `-id.md` | WF-003, 004, 005, 007 exceptions; Tata Rekening handoff; BR lists |
| `outpatient-apotek-persistence-design.md` | PD-05; §4.2; §8.5 Credit Note; §9.2; §11 BillingCharge/Credit |
| `outpatient-apotek-screen-and-aggregate-design.md` | Correction Request; Financial Consequence Pending; Invoice aggregate row |
| SOP-003 / 004 / 005 / 007 EN+ID | Correction, shortage, No-Show, Credit Note supplied by TR |
| `../working/outpatient-apotek-repository-gap-analysis-report.md` | BA-07, BA-08, MI-03 Invoice/credit-note gap |
| `APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md` | §6 mutability |
| `APOTEK-UNFULFILLED-OUTCOME-SIMPLIFICATION-ANALYSIS.md` | Credit Note as collaborator |
| `ALN-003-RESOLUTION-REPORT.md` | Invoice Item from Sales Order Item (timing, not freeze) |
| `ALN-006-RESOLUTION-REPORT.md` | Credit Note/Refund after post-SO shortage |
| ADR-APT-001 / 002 | No Invoice freeze decision |
| Tata Rekening `04-sop.md`, SOP-TR-05, SOP-TR-09 | Charge Source form/change/cancel while `OPEN`; Reopen |

---

## 13. Conclusion

Current Apotek artifacts freeze Invoice **content** at **Issue** (PD-05 / BR-APT-027) and route later commercial change through **Credit Note, Refund, and Tata Rekening Adjustment**. That freeze is **Apotek-owned** and **stricter** than Tata Rekening Charge Source rights while Billing is **`OPEN`**.

Changing to **mutable while Tata Rekening permits** is a **gate relocation**: Apotek becomes a Charge Source that **consumes** mutation permission and **does not** own Financial Clearance or Financial Control. Credit Note remains necessary for **blocked** mutation and for **cashier-settled** money until a separate rule is ratified. Integration today has **create charge** and **credit charge** only; **update/cancel charge** and a **synchronous permission query** are missing contracts.

No implementation should start until ADR-level ownership, the paid-vs-`OPEN` question, Pricing Snapshot, and billing task idempotency are decided with Tata Rekening.
