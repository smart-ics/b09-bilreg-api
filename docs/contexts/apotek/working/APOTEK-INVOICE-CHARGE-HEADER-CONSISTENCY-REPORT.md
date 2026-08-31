# Apotek — Invoice-Level Charge Header Consistency Report

```yaml
Artifact-Type: ConsistencyReport
Status: Working
Normative-Level: Historical
Superseded-By: outpatient-apotek-persistence-design.md (PD-10)
Archive-Eligible: Yes
```

**Date:** 2026-08-18  
**Decision:** PD-10 — Remove `BILRG_AptInvoiceCharge`; store invoice-level commercial amounts exclusively on `BILRG_AptInvoice` header fields.

---

## 1. Decision summary

| Aspect | Before | After |
|---|---|---|
| Invoice-level adjustments (`BR-APT-128`) | Child table `BILRG_AptInvoiceCharge` | Header fields on `BILRG_AptInvoice` (`Pembulatan`, `BiayaLain`, `DiskonLain`, `SubTotal`, `SumBiaya`, `SumTax`, `GrandTotal`, etc.) |
| Item-specific charges (`BR-APT-127`) | `BILRG_AptInvoiceItemCharge` | Unchanged — `BILRG_AptInvoiceItemCharge` |
| Invoice aggregate reconstruction | Header + items + item charges + invoice charges | Header (commercial totals) + items + item charges |

---

## 2. Modified artifacts

| Artifact | Classification | Changes |
|---|---|---|
| [`outpatient-apotek-persistence-design.md`](../outpatient-apotek-persistence-design.md) | Active | Removed `BILRG_AptInvoiceCharge` from §6.1 table catalog and §7 ERD. Added to §6.2 omitted/forbidden. Updated §2.1, §4.2, §8.5, §9.2, PD-05, PD-06, §15 checklist. Added closed decision **PD-10**. |
| [`apotek-domain.md`](../apotek-domain.md) | Active | Glossary, §6.3 aggregate narrative, `BR-APT-024`, `BR-APT-128` — invoice-level adjustments are header attributes, not child entities. |
| [`apotek-domain-id.md`](../apotek-domain-id.md) | Active | Parallel Indonesian updates to glossary, aggregate narrative, `BR-APT-024`, `BR-APT-128`. |
| [`outpatient-apotek-screen-and-aggregate-design.md`](../outpatient-apotek-screen-and-aggregate-design.md) | Active | §2 principle 3 and §5.2 `Invoice` aggregate row — transaction-wide adjustments are header commercial totals. |
| [`outpatient-apotek-repository-gap-analysis-report.md`](./outpatient-apotek-repository-gap-analysis-report.md) | Working | BC-08 charges section and §9.1 summary bullet — persistence shape aligned to PD-10. |

---

## 3. Verification — no remaining dependency on invoice-level charge detail records

### 3.1 Active and Working artifacts

| Search | Result |
|---|---|
| `BILRG_AptInvoiceCharge` in Active/Working docs | Only in forbidden/omitted context (PD-10) or this report |
| `AptInvoiceCharge` in source code | **None** — Apotek write model not yet implemented |
| `invoice-level charge` implying child records | **None** in Active artifacts after update |

### 3.2 Archived artifacts (not modified; historical only)

| Artifact | Note |
|---|---|
| `archive/APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md` | References `InvoiceCharge` in pre-PD-10 rewrite analysis. Superseded by PD-05 and PD-10. |
| `APOTEK-INVOICE-MUTABILITY-IMPACT-ANALYSIS.md` (root duplicate if present) | Same as archive copy. |

Per [`artifact-management-skill.md`](../../../skills/artifact-management-skill.md), archived artifacts are not authoritative and were not updated.

---

## 4. Cross-cutting consistency matrix

| Concern | Consistent? | Authority |
|---|---|---|
| Table catalog | Yes | `outpatient-apotek-persistence-design.md` §6.1 — no `BILRG_AptInvoiceCharge` |
| ERD | Yes | §7 — only `BILRG_AptInvoiceItemCharge` child under items |
| Aggregate ownership | Yes | `Invoice` owns header totals + items + item charges only |
| Repository save semantics | Yes | §8.5, §9.2, PD-05 — item charges delete+insert; header totals upsert in place |
| Reporting (PD-06) | Yes | Adapter reads header + items + item charges; no invoice charge detail join |
| Integration (`BillingCharge`) | Yes | §11 — charge from Invoice Issued; totals from header, not child table |
| Domain rules | Yes | `BR-APT-128` → header commercial totals; `BR-APT-127` → item charges unchanged |
| Screen / aggregate design | Yes | §2, §5.2 aligned with persistence shape |

---

## 5. Invoice header fields carrying `BR-APT-128` amounts

These fields on `BILRG_AptInvoice` (§8.5) are the sole persistence for transaction-wide commercial adjustments:

- `SubTotal`
- `SumBiaya`
- `SumTax`
- `DiskonLain`
- `BiayaLain`
- `Pembulatan`
- `GrandTotal`

No additional invoice-level charge detail table is required or permitted.

---

## 6. Traceability

```text
Decision absorbed into:
    PD-10 (outpatient-apotek-persistence-design.md)
    BR-APT-024, BR-APT-128 (apotek-domain.md)
    Invoice aggregate (outpatient-apotek-screen-and-aggregate-design.md)
    BC-08 persistence note (outpatient-apotek-repository-gap-analysis-report.md)
```

This report is an Archive Candidate once PD-10 is accepted in architect review.
