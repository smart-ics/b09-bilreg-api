# Outpatient Pharmacy Domain

**Artifact status:** Canonical business specification

**Bounded context:** Outpatient Pharmacy (`Apotek Rajal`)

**Version scope:** Target business flow

**Bahasa Indonesia companion:** [apotek-rajal-domain-id.md](./apotek-rajal-domain-id.md)

**Related business contexts:** [Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN.md), [Tata Rekening](../../contexts/TataRekening/02-domain.md), [CPOE](../../contexts/cpoe/CPOE-DOMAIN.md), [Medication Fulfillment](./medication-fulfillment-domain.md)

## 1. Business Overview

### 1.1 Purpose and value

Outpatient Pharmacy turns an eligible medicine request into prepared medicine, an accountable Sale, and a safe Medicine Handover. It coordinates prescription review, queue-to-request mapping, stock allocation, payer-specific commercial clearance, physical dispensing, final medicine review, Patient education, and no-show resolution.

The business must ensure that:

- only clinically acceptable and available medicine is fulfilled;
- every Sale is derived from the medicine actually eligible for dispensing;
- no user manually enters a Sale or legacy `DU`;
- General Patients pay before physical dispensing;
- BPJS Patients are not asked to confirm a purchase or make a Patient payment; and
- medicine is not recognized as handed over unless the Patient is present and the handover is accountable.

### 1.2 Scope

This specification covers outpatient pharmacy service originating from:

1. an Electronic Prescription created by a clinic or another care unit;
2. a Physical Prescription recorded by Pharmacy Staff; or
3. a Direct Sale Request for medicine sold without a Prescription.

It covers the business flow from prescription or request availability through Order Dispensing, Sale formation, dispensing, Medicine Handover, and no-show resolution.

### 1.3 Business boundaries and related contexts

Outpatient Pharmacy owns the Prescription Review outcome, Order Dispensing, payer-specific Sale formation timing, dispensing outcome, Medicine Review, Medicine Handover, and pharmacy no-show resolution.

It relies on other business contexts without taking over their authority:

- the prescribing or clinical-order context owns the clinician's original Electronic Prescription intent;
- Patient Tracker owns pharmacy queue identity, Queue Session, Queue Number, and the operational queue lifecycle;
- Outpatient Pharmacy owns the business decision that maps a pharmacy Queue Entry to the applicable Prescription or Direct Sale Request;
- inventory ownership remains authoritative for physical stock balances and records the reserve, In-Transit, issue, and return outcomes requested by this flow;
- cashier/payment activity owns receipt of General Patient payment;
- Tata Rekening owns broader Patient financial responsibility, payer allocation, finalization, and settlement; and
- BPJS eligibility and claim policy remain authoritative outside this bounded context.

**Known alignment requirement:** Patient Tracker rule `BR-TRK-045` currently treats confirmed Sale formation as pharmacy service-start evidence. That interpretation remains valid for the General flow but cannot be applied unchanged to BPJS, because a BPJS Sale is formed only at successful Medicine Handover. Cross-context milestone semantics must be reconciled with `BR-APR-045` before implementation changes are treated as complete.

### 1.4 Required domain emphasis

Four facts are central to this domain:

1. Prescription Review and pharmacy queue intake are independent flows.
2. Order Dispensing is the sole business basis of every Sale.
3. General and BPJS Sales are formed at different business moments.
4. A BPJS no-show before Medicine Handover returns stock without cancelling a Sale, because no Sale exists yet.

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Outpatient Pharmacy Service | The complete outpatient medicine-fulfilment responsibility from an eligible request through Medicine Handover or an accountable non-fulfilment outcome. |
| Electronic Prescription | A Prescription formed by a clinic or another care unit and available for pharmacy work without Patient arrival. |
| Physical Prescription | A paper Prescription brought by a Patient and recorded as a Prescription by Pharmacy Staff. |
| Direct Sale Request | A request for medicine sold without a Prescription; it still requires Order Dispensing and Sale. |
| Prescription Review | The Pharmacist's professional assessment of a Prescription before its eligible medicine lines may enter Order Dispensing. |
| Eligible Medicine Line | A requested medicine and quantity that passed the required Prescription Review, when applicable, and can be supplied from available stock. |
| Order Dispensing | The authoritative fulfilment instruction containing only Eligible Medicine Lines. It is the sole source of every Sale. |
| Pharmacy Queue Entry | A Patient's participation in the outpatient pharmacy queue, whose identity and lifecycle are owned by Patient Tracker. |
| Queue Mapping | The accountable association of a Pharmacy Queue Entry with the applicable Prescription, Order Dispensing, or Direct Sale Request. |
| Tracker Mapping | Automatic Queue Mapping using Patient Tracker or registration evidence presented by the Patient. |
| Manual Mapping | Queue Mapping performed by Pharmacy Staff after a Queue Number is called and the Patient or request is identified. |
| Stock Reservation | Stock allocated to an Order Dispensing so it cannot be promised again while fulfilment remains active. |
| In-Transit Stock | Prepared medicine removed from general pharmacy availability but not yet handed to the Patient. |
| Sale | The commercial transaction derived from one Order Dispensing. `DU` and `Trs.DU (DO-Bill Umum)` are legacy names for the same business concept. |
| General Sale | A Sale for which the Patient must confirm the purchase and pay before physical dispensing. |
| BPJS Sale | A Sale covered under the applicable BPJS arrangement, with no Patient purchase confirmation and no Patient payment. |
| Purchase Confirmation | The General Patient's decision to proceed after Pharmacy Staff communicates the final Sale value. |
| Payment Clearance | The business condition that permits General Patient dispensing after the Sale is paid. |
| BPJS Clearance | The business condition that permits BPJS dispensing after queue mapping, Order Dispensing readiness, and applicable coverage validation, without a Sale yet. |
| Physical Dispensing | The TTK's preparation, labelling, compounding when required, and packaging of medicine according to Order Dispensing. |
| Medicine Review | The Pharmacist's final review of prepared medicine before the Patient is called for pickup. |
| Patient Education | The Pharmacist's explanation of medicine use and relevant precautions during Medicine Handover. |
| Medicine Handover | The accountable transfer of reviewed medicine to a verified Patient or authorized recipient. |
| No-Show | The outcome when a Patient does not attend Medicine Handover within the applicable service limit. |
| Copy Prescription | The accountable record of prescribed items or quantities that could not be fulfilled. |
| Pharmacy Service Start Evidence | The accountable business evidence that active pharmacy service has begun; it is not universally identical to Sale formation. |

## 3. Business Capabilities

### 3.1 Service Demand Intake

Accept an Electronic Prescription, record a Physical Prescription, or recognize a Direct Sale Request as the source of outpatient pharmacy demand.

### 3.2 Prescription Review

Allow Pharmacists to review every available Prescription without waiting for Patient arrival or Queue Mapping.

### 3.3 Queue Mapping

Associate the Patient's Pharmacy Queue Entry with the applicable Prescription or Direct Sale Request through Tracker Mapping or Manual Mapping.

### 3.4 Dispensing Eligibility Determination

Determine which requested medicine lines pass the required professional review and can be supplied from available stock.

### 3.5 Order Dispensing Management

Establish and maintain the fulfilment instruction that becomes the common basis for stock allocation, Sale formation, dispensing, and handover.

### 3.6 Stock Allocation

Reserve eligible medicine, recognize prepared medicine as In-Transit, and return unhanded medicine to pharmacy availability when fulfilment ends without handover.

### 3.7 Payer-Specific Sale Formation

Form every Sale automatically from Order Dispensing at the business moment appropriate to General or BPJS coverage.

### 3.8 Commercial Clearance

Obtain Purchase Confirmation and payment for General Patients while allowing BPJS Patients to proceed without Patient payment.

### 3.9 Physical Dispensing and Final Assurance

Prepare medicine through TTK work and complete the Pharmacist's Medicine Review before pickup.

### 3.10 Handover and No-Show Resolution

Call the Patient, verify identity, provide Patient Education, complete Medicine Handover, or resolve a No-Show without leaving unsupported sales or stock outcomes.

## 4. Actors & Roles

### 4.1 Patient or Authorized Recipient

Presents queue or tracker evidence, supplies a Physical Prescription when applicable, confirms and pays for a General Sale, receives education, and accepts medicine.

### 4.2 Pharmacy Staff

Coordinates the pharmacy counter, calls Queue Numbers, performs Manual Mapping, records Physical Prescriptions, records Direct Sale Requests, communicates General Sale values, and verifies identity at handover. Pharmacy Staff does not perform the Pharmacist's professional reviews.

### 4.3 Pharmacist

Performs Prescription Review, performs Medicine Review, and provides Patient Education. The Pharmacist does not perform administrative queue calling.

### 4.4 Pharmacy Technician (`TTK`)

Performs Physical Dispensing according to Order Dispensing and corrects prepared medicine when the Medicine Review finds a discrepancy.

### 4.5 Cashier

Receives General Patient payment and establishes Payment Clearance. The Cashier is not involved in BPJS Patient payment.

### 4.6 Pharmacy Supervisor

Owns accountable exceptional decisions such as fulfilment expiry, prepared-stock return, and corrections that exceed ordinary staff authority.

## 5. Domain Objects

### 5.1 Prescription

Represents prescribed medicine intent available to the pharmacy. It may be electronic or recorded from a Physical Prescription. Its source does not change the requirement for Prescription Review.

### 5.2 Prescription Review Outcome

Records which Prescription lines are approved, partially approved, rejected, or require clarification. It is independent of Pharmacy Queue Entry existence.

### 5.3 Direct Sale Request

Represents non-prescription demand recorded by Pharmacy Staff. It bypasses Prescription creation but never bypasses Order Dispensing or Sale.

### 5.4 Queue Mapping

Relates the externally owned Pharmacy Queue Entry to the applicable pharmacy demand. It may be established automatically or by Pharmacy Staff.

### 5.5 Order Dispensing

Contains only Eligible Medicine Lines and their fulfilment quantities. It retains the source relationship, applicable value basis, and stock allocation needed to complete fulfilment.

### 5.6 Stock Allocation

Represents medicine reserved or placed In-Transit for one Order Dispensing and its final issued or returned outcome.

### 5.7 Sale

Represents the commercial value of one Order Dispensing. Its lines and quantities must be derived from Order Dispensing, never entered independently.

### 5.8 Medicine Review Outcome

Records whether prepared medicine corresponds to Order Dispensing and is safe to proceed to pickup, or requires correction.

### 5.9 Medicine Handover

Records the accountable delivery outcome, verified recipient, education responsibility, and the Sale and Order Dispensing being completed.

## 6. Aggregates

### 6.1 Pharmacy Prescription Aggregate

**Aggregate Root:** `Pharmacy Prescription`

The aggregate keeps the received Prescription, its source, Prescription Review Outcome, and per-line fulfilment eligibility mutually consistent. It does not rewrite the clinician's original prescribing intent.

### 6.2 Order Dispensing Aggregate

**Aggregate Root:** `Order Dispensing`

The aggregate owns Eligible Medicine Lines, fulfilment quantities, source traceability, value snapshot, Stock Reservation, dispensing progress, prepared outcome, and expiry or delivery outcome.

### 6.3 Sale Aggregate

**Aggregate Root:** `Sale`

The aggregate keeps payer classification, Sale lines, values, Purchase Confirmation when applicable, payment disposition, cancellation, and completion mutually consistent. It cannot contain lines that are absent from its Order Dispensing.

### 6.4 Medicine Handover Aggregate

**Aggregate Root:** `Medicine Handover`

The aggregate keeps recipient verification, Medicine Review completion, Patient Education, Sale reference, Order Dispensing reference, and delivery outcome mutually consistent.

For BPJS, Sale establishment and Medicine Handover completion form one indivisible business outcome: the business must not recognize one without the other.

### 6.5 Explicit external ownership

`Pharmacy Queue Entry`, `Queue Session`, Patient identity, cashier payment receipt, BPJS claim, and the organization-wide financial account are not Aggregate Roots of Outpatient Pharmacy.

## 7. Business Rules

### 7.1 Intake and professional responsibility

- **BR-APR-001** — Outpatient Pharmacy demand shall originate from exactly one of Electronic Prescription, Physical Prescription, or Direct Sale Request.
- **BR-APR-002** — An Electronic Prescription and a recorded Physical Prescription shall be treated as Prescription sources with the same professional review obligation.
- **BR-APR-003** — A Pharmacist may and should perform Prescription Review as soon as a Prescription is available; Queue Mapping and Patient arrival shall not be prerequisites.
- **BR-APR-004** — A Physical Prescription shall become available for Prescription Review only after Pharmacy Staff records it as a Prescription.
- **BR-APR-005** — A Direct Sale Request shall not create a Prescription but shall create Order Dispensing before Sale.
- **BR-APR-006** — Only a Pharmacist shall own Prescription Review, Medicine Review, and Patient Education outcomes.
- **BR-APR-007** — Pharmacy Staff shall own administrative queue calling and shall not transfer that responsibility to the Pharmacist.
- **BR-APR-008** — TTK shall prepare medicine only from Order Dispensing.

### 7.2 Queue mapping and calls

- **BR-APR-009** — Queue Mapping shall be automatic when valid tracker or registration evidence resolves the applicable pharmacy demand.
- **BR-APR-010** — A directly issued Queue Number shall remain unmapped until Pharmacy Staff identifies and associates its pharmacy demand.
- **BR-APR-011** — A failed Tracker Mapping shall fall back to Manual Mapping.
- **BR-APR-012** — Pharmacy Staff shall call an unmapped Queue Number before performing Manual Mapping.
- **BR-APR-013** — Queue Mapping shall associate existing business records and shall not be treated as the cause of an existing Electronic Prescription or completed Prescription Review.
- **BR-APR-014** — A BPJS Patient with an Electronic Prescription and successful Tracker Mapping shall require exactly one pharmacy call in the normal flow: the pickup call after Medicine Review.
- **BR-APR-015** — Mapping and General Purchase Confirmation may be completed in one counter interaction when Order Dispensing and the final Sale value are already available.
- **BR-APR-016** — Patient Tracker shall remain authoritative for Queue Number and queue lifecycle even when Outpatient Pharmacy decides the mapping and call purpose.

### 7.3 Order Dispensing and stock

- **BR-APR-017** — Order Dispensing shall contain only Eligible Medicine Lines.
- **BR-APR-018** — A Prescription line is eligible only when it passes Prescription Review and the fulfilment quantity can be supplied from available stock.
- **BR-APR-019** — Partial fulfilment shall preserve the unfulfilled Prescription lines or quantities as an accountable non-fulfilment outcome, including Copy Prescription when applicable.
- **BR-APR-020** — A Direct Sale Request shall produce Order Dispensing from its accepted and available medicine lines.
- **BR-APR-021** — Order Dispensing may become Ready before Patient arrival or Queue Mapping.
- **BR-APR-022** — Ready Order Dispensing shall secure its fulfilment quantity through Stock Reservation.
- **BR-APR-023** — Prepared medicine shall be recognized as In-Transit Stock until handover or return.
- **BR-APR-024** — A Sale shall never be used as the source of medicine eligibility; eligibility belongs to Prescription Review and Order Dispensing.

### 7.4 Sale and payer policy

- **BR-APR-025** — Every Sale shall be derived automatically from exactly one Order Dispensing.
- **BR-APR-026** — No user shall manually enter a Sale, `DU`, or `Trs.DU`; a user action may trigger automatic Sale formation but shall not supply independent Sale lines.
- **BR-APR-027** — Sale lines and quantities shall not exceed or differ from their source Order Dispensing lines and quantities.
- **BR-APR-028** — No Order Dispensing shall have more than one active Sale.
- **BR-APR-029** — A General Sale shall be formed after Queue Mapping and Ready Order Dispensing, before Purchase Confirmation and payment.
- **BR-APR-030** — The final value communicated to a General Patient shall come from the General Sale derived from Order Dispensing.
- **BR-APR-031** — A General Patient who declines Purchase Confirmation shall cause the General Sale to be cancelled and the unused Stock Reservation to be released.
- **BR-APR-032** — A General Sale shall be paid before Physical Dispensing begins.
- **BR-APR-033** — A BPJS Patient shall not be asked for Purchase Confirmation or Patient payment.
- **BR-APR-034** — BPJS Physical Dispensing may begin when Queue Mapping, Ready Order Dispensing, and BPJS Clearance are present; a BPJS Sale shall not be required at that point.
- **BR-APR-035** — A BPJS Sale shall be formed only when Medicine Handover is successfully confirmed, not merely when the Patient arrives or is called.
- **BR-APR-036** — A BPJS Sale shall have no Patient payable amount and shall have payment disposition `Not Required`; its gross or covered value need not be zero.
- **BR-APR-037** — BPJS Sale formation and Medicine Handover shall be one indivisible business completion; partial recognition is forbidden.
- **BR-APR-038** — The legacy names `DU` and `Trs.DU (DO-Bill Umum)` shall not change the business meaning or formation rules of Sale.

### 7.5 Dispensing, handover, no-show, and cross-context evidence

- **BR-APR-039** — Medicine Review shall be completed successfully before Pharmacy Staff calls the Patient for pickup.
- **BR-APR-040** — Pharmacy Staff, not the Pharmacist, shall perform the pickup call.
- **BR-APR-041** — Recipient identity shall be verified and the Pharmacist shall provide Patient Education before Medicine Handover completes.
- **BR-APR-042** — Successful Medicine Handover shall complete Order Dispensing and consume the corresponding In-Transit Stock.
- **BR-APR-043** — A BPJS No-Show before Medicine Handover shall not form or cancel a Sale; reserved or In-Transit medicine shall be returned and Order Dispensing shall expire or end without delivery.
- **BR-APR-044** — A General No-Show after payment shall follow the accountable policy for a paid but uncollected Sale; it shall not be treated as the BPJS no-Sale path.
- **BR-APR-045** — Pharmacy Service Start Evidence shall come from an accountable service activity and shall not universally depend on Sale formation. General Sale confirmation may evidence service start, while BPJS service start must be evidenced before its Sale is formed at handover.
- **BR-APR-046** — Source traceability from Prescription or Direct Sale Request through Order Dispensing, Sale, and Medicine Handover shall be preserved.

## 8. State Machines & Lifecycles

### 8.1 Prescription Review lifecycle

```text
Prescription Recorded
  → Under Review
      → Approved
      → Partially Approved
      → Rejected
      → Clarification Required
```

Electronic and Physical Prescription sources use the same lifecycle. Queue Mapping is not a transition in this lifecycle.

### 8.2 Queue Mapping relationship

```text
Unmapped
  → Mapped
```

This is the pharmacy demand-association relationship, not a replacement for the Patient Tracker queue lifecycle.

### 8.3 Order Dispensing lifecycle

```text
Established
  → Ready
      → Dispensing
          → Prepared
              → Delivered

Ready or Prepared
  → Cancelled or Expired
```

`Delivered` requires successful Medicine Handover. A BPJS No-Show ends through `Expired` or another approved non-delivery outcome, not through Sale cancellation.

### 8.4 Stock Allocation lifecycle

```text
Available
  → Reserved
      → In-Transit
          → Issued to Patient
          → Returned to Pharmacy

Reserved
  → Released to Available
```

### 8.5 General Sale lifecycle

```text
Established
  → Awaiting Confirmation
      → Awaiting Payment
          → Paid
              → Completed

Awaiting Confirmation
  → Cancelled
```

### 8.6 BPJS Sale lifecycle and formation

A BPJS Sale does not exist while Order Dispensing is being reviewed, reserved, dispensed, prepared, or waiting for pickup.

```text
Successful BPJS Medicine Handover
  → BPJS Sale Established with Payment Not Required
  → BPJS Sale Completed
```

Sale establishment and completion occur within the same accountable handover outcome. “Not yet established” is absence of a Sale, not a Sale state.

### 8.7 Medicine Handover lifecycle

```text
Pending Final Review
  → Ready for Pickup
      → Recipient Verified
          → Education Provided
              → Delivered

Ready for Pickup
  → No-Show
```

## 9. Domain Events

| Domain Event | Business meaning |
|---|---|
| Prescription Recorded | A Prescription has become available for pharmacy review. |
| Prescription Review Completed | The Pharmacist has established the professional disposition of the Prescription lines. |
| Pharmacy Queue Mapped | A Pharmacy Queue Entry has been associated with the applicable pharmacy demand. |
| Order Dispensing Established | Eligible Medicine Lines have formed the authoritative fulfilment instruction. |
| Stock Reserved | Stock has been allocated to an Order Dispensing. |
| General Sale Established | A General Sale has been derived from Ready Order Dispensing. |
| Purchase Confirmed | A General Patient has agreed to proceed at the communicated Sale value. |
| General Sale Paid | Payment Clearance has been established for a General Sale. |
| BPJS Clearance Established | BPJS fulfilment may proceed without Patient payment and without an existing Sale. |
| Dispensing Started | TTK has begun physical preparation from Order Dispensing. |
| Medicine Prepared | Medicine has entered the prepared In-Transit condition. |
| Medicine Review Completed | The Pharmacist has confirmed that prepared medicine may proceed to pickup. |
| Patient Called for Pickup | Pharmacy Staff has called the Patient for Medicine Handover. |
| BPJS Sale Established | A BPJS Sale has been derived from Order Dispensing as part of successful Medicine Handover. |
| Medicine Handed Over | Reviewed medicine has been transferred to a verified recipient with Patient Education. |
| Order Dispensing Expired | Fulfilment ended without delivery within the applicable service limit. |
| Pharmacy Stock Returned | Reserved or In-Transit medicine has returned to pharmacy availability. |

## 10. Business Workflows

### 10.1 Review an Electronic Prescription before Patient arrival

```text
Electronic Prescription becomes available
  → Pharmacist performs Prescription Review
  → Eligible Medicine Lines are determined
  → Order Dispensing is established
  → Stock is reserved
  → Order Dispensing waits independently for Queue Mapping
```

### 10.2 BPJS with Electronic Prescription and successful Tracker Mapping

```text
Electronic Prescription has Ready Order Dispensing
  → Patient scans tracker evidence
  → Queue Mapping succeeds automatically
  → BPJS Clearance is established
  → TTK performs Physical Dispensing
  → Pharmacist completes Medicine Review
  → Pharmacy Staff calls the Patient once for pickup
  → Patient is verified and educated
  → BPJS Sale and Medicine Handover complete together
```

### 10.3 Manual Queue Mapping

```text
Patient takes a directly issued Queue Number or Tracker Mapping fails
  → Pharmacy Staff calls the Queue Number
  → Pharmacy Staff identifies the service source
      → existing Electronic Prescription is associated
      → Physical Prescription is recorded
      → Direct Sale Request is recorded
  → Pharmacy Queue Entry becomes Mapped
```

### 10.4 Physical Prescription fulfilment

```text
Pharmacy Staff records the Physical Prescription
  → Pharmacist performs Prescription Review
  → Eligible Medicine Lines are determined
  → Order Dispensing is established
  → payer-specific clearance proceeds
```

### 10.5 Direct Sale fulfilment

```text
Pharmacy Staff records Direct Sale Request
  → accepted and available medicine lines are determined
  → Order Dispensing is established
  → General Sale is derived
  → Purchase Confirmation and payment proceed
```

### 10.6 General Patient fulfilment

```text
Queue Mapping and Ready Order Dispensing are present
  → General Sale is derived from Order Dispensing
  → Pharmacy Staff communicates the final Sale value
  → Patient confirms purchase
  → Cashier receives payment
  → TTK performs Physical Dispensing
  → Pharmacist completes Medicine Review
  → Pharmacy Staff calls the Patient for pickup
  → Patient is verified and educated
  → Medicine Handover completes the Sale and Order Dispensing
```

If the Patient declines before payment, the General Sale is cancelled and unused stock is released.

### 10.7 Successful BPJS Medicine Handover

```text
Prepared BPJS medicine passes Medicine Review
  → Pharmacy Staff calls the Patient
  → Patient or authorized recipient is present and verified
  → Pharmacist provides Patient Education
  → BPJS Sale is derived from Order Dispensing
  → Medicine Handover is recorded
  → In-Transit Stock is issued
  → Order Dispensing and pharmacy queue service complete
```

BPJS Sale and Medicine Handover are one accountable outcome even though Sale is listed first for source traceability.

### 10.8 BPJS No-Show

```text
Prepared BPJS medicine passes Medicine Review
  → Pharmacy Staff calls the Patient
  → Patient does not attend within the applicable service limit
  → no BPJS Sale is formed
  → Reserved or In-Transit Stock returns to pharmacy availability
  → Order Dispensing expires without delivery
  → pharmacy queue participation closes under the applicable Patient Tracker policy
```

### 10.9 No eligible medicine

```text
Prescription Review and stock assessment complete
  → no Eligible Medicine Line remains
  → no Order Dispensing is established
  → no Sale is formed
  → non-fulfilment and Copy Prescription are recorded when applicable
```
