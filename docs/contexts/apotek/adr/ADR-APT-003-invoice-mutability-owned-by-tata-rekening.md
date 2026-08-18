# ADR-APT-003 Invoice Mutability Owned by Tata Rekening Permission

**Status:** Accepted  
**Date:** 2026-08-18  
**Owner:** Architecture Team

## Context

Earlier Apotek artifacts treated Invoice content as immutable after it left `Established`. **BR-APT-027** required Credit Note, Refund, or Financial Adjustment for any issued or financially settled Invoice. Persistence decision **PD-05** allowed rewrite only while `InvoiceStatus = Established`.

That freeze was owned by the Apotek Invoice lifecycle. It was stricter than the Charge Source role Apotek already has toward Tata Rekening: Invoice is source information for Financial Charge, and Tata Rekening owns whether that charge may still change.

**BA-08** already retired Financial Clearance as an Apotek object. Apotek evaluates **Dispense Authorized** over inbound evidence and does not own Tata Rekening Close, Finalize, Reopen, or related financial-control rules.

The project now adopts a different principle: Invoice mutability follows external financial permission owned by Tata Rekening, not automatic immutability after Issue.

Related analysis (archived, not normative): [`APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md`](../archive/APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md), [`APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md`](../archive/APOTEK-FINANCIAL-CLEARANCE-ANALYSIS.md).

## Decision

Invoice SHALL be considered mutable while modification is still permitted by external financial processes owned by Tata Rekening.

Apotek does not own Financial Clearance rules. Apotek only consumes external financial decisions and permissions provided by Tata Rekening.

Therefore:

1. Invoice is a commercial/charge document representing medication sale information that serves as source information for Tata Rekening.
2. Invoice is **not** automatically immutable immediately after Issue.
3. Invoice `Issued` and `Financially Cleared` remain commercial and payment/coverage facts. They are **not** mutability locks and are **not** Tata Rekening Close / Finalize / Lunas.
4. Invoice correction SHALL use normal Invoice revision of the same Invoice while Tata Rekening still permits modification.
5. Credit Note, Refund, and Financial Adjustment remain exception mechanisms **owned and persisted by Tata Rekening** when direct Invoice revision is no longer permitted. Apotek MUST NOT persist a Credit Note entity, own Credit Note lifecycle, or introduce a replacement financial-correction aggregate. Optional correlation on Invoice is allowed.
6. Apotek MUST NOT define, calculate, or own the internal business rules Tata Rekening uses to determine such permissions.
7. Apotek MUST NOT persist a Financial Clearance aggregate, status, or local substitute for Tata Rekening permission. Any last-consumed permission retained by Apotek is audit trace only; Tata Rekening remains the source of truth.
8. Mutability SHALL NOT be modeled as a new InvoiceStatus (`Mutable` / `Frozen`). Command guards consume Tata Rekening permission at the time of correction.
9. Unfulfilled Medication Outcome, Sales Order item identity after establishment, and Final Dispense Review append-only records remain independent of Invoice revision.

This decision supersedes the Established-only freeze previously recorded in **PD-05** and the issued-or-settled Credit Note mandate previously recorded in **BR-APT-027**.

## Consequences

### Canonical rule impact

- **BR-APT-027** becomes the two-path correction rule: revise the Invoice while permitted; delegate Credit Note / Refund / Financial Adjustment to Tata Rekening when not permitted. Apotek does not persist those exception documents.
- **PD-05** allows rewrite while `Established`, and after Issue while Tata Rekening still permits modification.
- **PD-07** removes `BILRG_AptCreditNote` and Integration Task `BillingCredit` `{InvoiceId}:CN{n}`.
- Workflows and SOPs that forced Credit Note merely because an Invoice had been issued are aligned to **BR-APT-027**.

### What this decision does not specify

This ADR does not invent Tata Rekening APIs, permission-contract shapes, Charge Source task types, or the internal conditions under which Tata Rekening grants or withholds modification.

Those integration details remain Tata Rekening-owned and are deferred to implementation planning.

### What remains unchanged

- **ADR-APT-001** and **ADR-APT-002** are unaffected.
- **BA-08** remains: no Financial Clearance or Fulfillment Clearance object in Apotek.
- Payment Clearance and Coverage Clearance remain inbound evidence. They do not determine Invoice mutability.
- Invoice identity is never erased by shortage or stock discrepancy (**BR-APT-051**).
- Silent replacement without Tata Rekening permission and without accountable actor and effective business time remains forbidden.

## References

- [`apotek-domain.md`](../apotek-domain.md) — Invoice definition, **BR-APT-027**, Invoice lifecycle
- [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) — PD-05, PD-07
- [`docs/contexts/TataRekening/01-context.md`](../../TataRekening/01-context.md) — Charge Source vs Financial Truth
- [`docs/contexts/TataRekening/04-sop.md`](../../TataRekening/04-sop.md) — Charge Source form / change / cancel authority
