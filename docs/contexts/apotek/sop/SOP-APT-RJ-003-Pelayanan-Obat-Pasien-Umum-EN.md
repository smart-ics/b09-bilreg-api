# SOP APT-RJ-003 — Fulfill Medication for a General Patient

**Artifact status:** Canonical target operational specification

**Bounded context:** Apotek

**Workflow:** `WF-APT-RJ-003`

**Bahasa Indonesia companion:** [SOP APT-RJ-003 — Memenuhi Obat untuk Pasien Umum](./SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-ID.md)

**Subsystem terminology status:** `Pharmacy System` and `Outpatient Pharmacy` are established subsystem terms. Other controls are described by operational action because approved target-workflow labels are not available.

## 1. Purpose

Provide a repeatable procedure for obtaining verbal Purchase Confirmation, establishing and clearing the General Patient Invoice, preparing medication, and completing accountable outpatient Medication Handover.

## 2. Actors and Responsibilities

| Actor | Type | Operational responsibility |
|---|---|---|
| Patient or Caregiver | Human | Confirms or declines the calculated purchase, pays when confirmed, presents for pickup, receives education, and accepts medication when authorized. |
| Pharmacy Staff | Human | Communicates the calculated amount, records the confirmed Invoice, prepares or compounds medication under a released Dispensing, coordinates readiness, and performs the pickup call. |
| Pharmacy Supervisor | Human | Authorizes the manual uncollected-medication resolution when the Patient does not collect prepared medication. Authorization is by an authorized pharmacist according to operational policy; no monetary approval threshold applies. |
| Cashier or Payment Authority | Human or Subsystem | Receives payment and supplies Payment Clearance. |
| Pharmacist | Human | Operationally verifies the recipient, completes Final Dispense Review, and records Patient Education Acknowledgement. Recipient verification is not system-enforced. Detailed counseling notes are optional. |
| Pharmacy System | Subsystem | Displays Sales Order Item amounts, records the Invoice and its Invoice Items, displays Payment Clearance, evaluates Dispense Authorized, tracks preparation, and records dispense and handover. |
| Patient Tracker | Subsystem | Records `ServedAt` at preparation start and `DoneAt` when queue completion occurs (pickup call, or No Show Resolution if the Queue Entry is still `In Service`). `DoneAt` is never reversed. |
| Inventory | Subsystem | Supplies Mutasi, Remove Stock, and return-disposition outcomes. |
| Tata Rekening | Subsystem | Receives Financial Charge and supplies financial permission for Invoice revision, or Credit Note / Refund / Financial Adjustment when revision is no longer permitted. Pharmacy System does not persist those exception documents. |

## 3. Preconditions

1. Participating staff are signed in with their required permissions.
2. Outpatient Queue Mapping, an active Sales Order, Patient-payable Sales Order Items, and a calculated Pricing Snapshot are displayed.
3. No Invoice exists for the proposed Patient-payable sale.
4. An applicable Dispensing exists or can be established from Sales Order Items through its Dispensing Items.

## 4. Operational Steps

1. **Pharmacy Staff** opens the mapped demand in `Apotek Rajal` and verifies the calculated Patient-payable amount from the applicable Sales Order Items.
2. **Pharmacy Staff** receives the Patient during Manual Mapping or performs an administrative Queue Number call after Tracker Mapping; **Patient Tracker** does not record `ServedAt` or `DoneAt` for this interaction.
3. **Pharmacy Staff** verbally communicates the calculated amount before an Invoice exists.
4. **Patient or Caregiver** verbally confirms the purchase.
5. **Pharmacy Staff** records the confirmed transaction; **Pharmacy System** establishes the Invoice and its Invoice Items only from the confirmed Sales Order Item quantities and displays its identifier and amount.
6. **Cashier or Payment Authority** receives payment and supplies Payment Clearance for that Invoice.
7. **Pharmacy System** displays Payment Clearance and evaluates financial and coverage evidence as Dispense Authorized for the applicable Dispensing quantities. Dispense Authorized is a policy evaluation result; it is not established as a persisted business object.
8. **Stock Ledger** records Stock Mutasi from Pharmacy Unit to Dispensing Temporary Unit when Pharmacy Reserve is required and not already in Dispensing Temporary Unit; **Pharmacy System** displays the Mutasi outcome.
9. **Pharmacy Staff** starts Medication Preparation only after the Dispensing is released.
10. **Pharmacy System** records `Medication Preparation Started`; **Patient Tracker** moves the Queue Entry to In Service and records `ServedAt`.
11. **Pharmacy Staff** completes preparation or compounding and records completion; **Pharmacy System** displays the Dispensing as `Prepared`.
12. **Pharmacy Staff** verifies that every Dispensing intended for the handover is `Prepared` or has an accountable exception outcome.
13. **Pharmacy Staff** performs one coordinated pickup call; **Patient Tracker** makes the Queue Entry `Done` and records `DoneAt`.
14. With the Patient or caregiver present, **Pharmacist** operationally verifies the recipient, completes Final Dispense Review, and records Patient Education Acknowledgement. When the review passes, **Pharmacy System** appends the review record and displays the Dispensing as `Reviewed`. **Pharmacy System** records education timestamp and responsible Pharmacist. Detailed counseling notes are optional. The Pharmacist may optionally record recipient phone number and relationship for reference.
15. **Pharmacy System** blocks handover until Final Dispense Review has passed and Patient Education Acknowledgement is recorded. Recipient identity is not a system gate. Detailed counseling notes are not required. If Pickup Expired, **Pharmacy System** also blocks handover until an authorized pharmacist records Collection Window Override with reason.
16. **Pharmacy Staff** completes the physical handover after Pharmacist authorization; **Pharmacy System** records Medication Dispense and Medication Handover for each applicable quantity.
17. **Inventory** records Remove Stock from Dispensing Temporary Unit; **Pharmacy System** displays the Dispensing as `Completed` when all required outcomes are present.
18. **Pharmacy System** displays the Sales Order as `Resolved` only when every accepted quantity and commercial consequence is final.

## 5. Operational Exceptions

### 5.1 Patient declines before invoice establishment

- **Pharmacy Staff** records the decline and does not establish an Invoice.
- **Pharmacy System** records the affected Patient-payable Sales Order Item quantity as declined or commercially unallocated and requests Stock Mutasi of unused quantity from Dispensing Temporary Unit back to Pharmacy Unit.

### 5.2 Amount changes before invoice establishment

- **Pharmacy System** displays the revised calculated amount.
- **Pharmacy Staff** communicates it again and obtains a new verbal confirmation before recording the transaction.

### 5.3 Payment is incomplete or an established invoice requires correction

- **Pharmacy System** keeps Medication Preparation blocked while Payment Clearance is absent.
- **Pharmacy Staff** cancels only when the displayed Invoice lifecycle and Tata Rekening permission both permit.
- When Tata Rekening still permits modification, **Pharmacy System** revises the same Invoice with accountable actor and effective business time.
- When Tata Rekening no longer permits modification, **Tata Rekening** supplies Financial Adjustment, Credit Note, Refund, or another exception outcome. **Pharmacy System** does not create or persist a Credit Note entity.

### 5.4 Shortage after Sales Order establishment

This exception applies when the shortage is identified **after** the Sales Order is established, including after payment or financial clearance (`BR-APT-118`). Shortage **before** Sales Order establishment is `SOP-APT-RJ-002` exception 5.2.

- **Pharmacy Staff** does not create Backorder, select an alternate stock source, or substitute the medication.
- **Pharmacy Staff** does not remove items from the established Sales Order and does not rebuild it as a partial order.
- **Pharmacy System** records the applicable Unfulfilled Medication Outcome, supports Salinan Resep for unfulfilled items, and keeps required financial consequences visible.
- Commercial consequences of an existing Invoice follow `BR-APT-027`: **Pharmacy System** revises the Invoice when Tata Rekening still permits modification; otherwise **Tata Rekening** supplies Credit Note, Refund, or another exception commercial correction.

### 5.5 Final Dispense Review fails

- When Final Dispense Review fails, **Pharmacist** records the reason and affected quantity and does not authorize handover.
- **Pharmacy System** appends an immutable review record with the Pharmacist and effective business time, returns the Dispensing from `Prepared` to `Preparing`, and keeps handover blocked.
- **Pharmacy Staff** corrects and prepares the affected medication again; **Pharmacy System** returns the Dispensing to `Prepared`, and **Pharmacist** performs a new Final Dispense Review. Previous review records remain visible and unchanged.

### 5.6 Patient does not collect medication

- **Pharmacy Supervisor** applies `SOP-APT-RJ-007`. If the Queue Entry is already `Done`, `DoneAt` is not reversed. If the Queue Entry is still `In Service` because the pickup call did not occur, that resolution may complete it to `Done` and record `DoneAt`.

## 6. Completion Criteria

1. The Invoice is visibly financially cleared.
2. The Dispensing is `Completed`, and Medication Handover records effective time. Recipient phone number and relationship may be recorded optionally for reference.
3. Remove Stock from Dispensing Temporary Unit is displayed as the Stock Ledger outcome.
4. The Sales Order is `Resolved`, or remains `Active` with an explicitly displayed unresolved fulfillment or commercial consequence.
5. Queue `Done` is not used as proof of Medication Handover.
6. A shortage after Sales Order establishment left that Sales Order unmodified; unfulfillable quantity has an Unfulfilled Medication Outcome and any required financial correction, not dropped Sales Order items.

## 7. References

- [Apotek Domain](../apotek-domain.md), especially `BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, and `BR-APT-125`–`BR-APT-134`, `BR-APT-138`–`BR-APT-142`.
- [Outpatient Apotek Workflow](../outpatient-apotek-workflow.md), `WF-APT-RJ-003`.
- [Patient Tracker Domain](../../../contexts/pasien-tracker/TRACKER-DOMAIN.md), `BR-TRK-045`, `BR-TRK-045a`, and `BR-TRK-046`.
- [Tata Rekening Domain](../../../contexts/TataRekening/02-domain.md).
