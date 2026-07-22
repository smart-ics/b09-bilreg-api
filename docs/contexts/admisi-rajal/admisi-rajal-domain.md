# Admisi Rajal Domain

**Artifact status:** Initial canonical business specification  
**Bounded context:** Admisi Rajal  
**Version scope:** Foundation for progressive feature elaboration  
**Bahasa Indonesia companion:** [admisi-rajal-domain-id.md](admisi-rajal-domain-id.md)

## 1. Business Overview

### 1.1 Purpose

Admisi Rajal is the business capability responsible for preparing and establishing a Patient's administrative access to outpatient care.

It connects planned or unplanned demand for care with an identified Patient, an eligible outpatient destination, an applicable payment or guarantor arrangement, and an authoritative Outpatient Registration.

### 1.2 Business value

Admisi Rajal provides:

- a consistent administrative entry into outpatient care;
- continuity between Booking, assisted or self-registration, and the intended outpatient destination;
- visible unresolved registration work for Admission Officers;
- an authoritative Outpatient Registration outcome;
- a foundation for Patient identity intake, coverage determination, referral qualification, destination assignment, and Initial Charge establishment; and
- accountable business evidence for the wider Patient Journey.

### 1.3 Scope and progressive elaboration

This initial specification establishes the stable context boundary and two elaborated capabilities: Booking Management and Registration Intake and Work Coordination.

The following capabilities are recognized as part of the Admisi Rajal business but require later feature specifications before their detailed policies are canonical:

- New Patient Recording;
- Insurance and Guarantor Determination;
- Referral Qualification;
- Outpatient Destination Assignment;
- Initial Charge establishment, including `Karcis`;
- advanced Practice Schedule Management policies beyond the existing schedule artifacts;
- advanced Booking amendment and exceptional-handling policies beyond the foundational rules in this document; and
- registration amendment, cancellation, and exceptional handling beyond the foundational rules in this document.

Recognition in this inventory does not settle every rule, aggregate boundary, eligibility condition, or lifecycle for those capabilities. Future domain artifacts shall refine this document without contradicting its ownership boundaries and Ubiquitous Language.

### 1.4 Business boundaries

Admisi Rajal owns:

- outpatient Booking business truth;
- outpatient Registration business truth and outcome;
- the business determination that registration assistance is required;
- the registration context shown as active work to Admission Officers;
- selection of the intended outpatient destination within applicable policy;
- the registration's payment, guarantor, referral, and Initial Charge context; and
- outpatient practice-schedule business truth currently maintained within this context.

Admisi Rajal does not own:

- the canonical Patient master identity, which belongs to the Patient context;
- Patient Journey identity, `TrackerId`, Queue Sessions, Queue Entries, Queue Numbers, or queue-service milestones, which belong to Patient Tracker;
- clinical consultation or the Medical Chart;
- the guarantor organization's external eligibility truth;
- tariff policy or the authoritative definition of charge prices;
- payment settlement or the complete Patient financial-account lifecycle;
- the clinical definition of an outpatient unit or physician profession; or
- the Patient's physical location or movement.

Admisi Rajal may use referenced facts from those contexts, but it does not replace their authority.

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Admisi Rajal | The administrative business responsibility that prepares and establishes access to outpatient care. |
| Patient | The person seeking or receiving outpatient care. |
| Patient Representative | A person who provides administrative information or acts for the Patient when permitted. |
| Admission Officer | The accountable staff role that resolves assisted outpatient registration work. |
| Outpatient Registration | The authoritative administrative record that admits one Patient to one intended outpatient visit. |
| RegId | The stable business reference of an established Registration. It is not the Patient Journey identity. |
| Registration Intake Path | The origin of a registration attempt: `By Booking` or `By Walk-In`. |
| By Booking | A Registration Intake Path that begins from an existing Booking. |
| By Walk-In | A Registration Intake Path that begins without an applicable Booking. |
| Booking | A plan for a Patient or prospective Patient to visit a specified outpatient destination on a future or current Visit Date. |
| Booking Channel | The business channel through which a Booking request originates: `HiDok` or `Admission-Assisted`. |
| HiDok | The self-service digital Booking Channel provided through the organization's Android application. |
| Admission-Assisted | The Booking Channel in which an Admission Officer records a request received by phone, WhatsApp, or another approved communication method. |
| Practice Session | One doctor's scheduled availability at an Outpatient Destination on one Visit Date and operating interval. |
| Schedule Capacity | The planned maximum number of Patients for one Practice Session. It is a hard limit for HiDok and a planning limit for Admission-Assisted Booking. |
| Capacity Override | An accountable Admission Officer decision to accept an Admission-Assisted Booking after Schedule Capacity has been reached. |
| Unresolved Patient Identity | The condition in which a Booking retains a demographic identity snapshot but is not yet associated with a canonical Patient. |
| Expired Unfulfilled | The condition of a Booking whose Visit Date has passed without Registration, cancellation, or rescheduling. It is not proof of the Patient's physical absence. |
| Self-Registration | Registration performed by the Patient through an authorized self-service channel without Admission Officer assistance. |
| Self-Registration Requires Assistance | The business outcome that Self-Registration did not establish a Registration and assisted registration work is required. |
| Registration Assistance | Human administrative work required to establish or resolve an Outpatient Registration. |
| Admisi Rajal Work List | The collection of active Registration Assistance items represented by active Patient Tracker Queue Entries for the Admisi Rajal Service Point and enriched with Admisi Rajal context. |
| Work Item | The operational representation of one active Registration Assistance obligation. It is not a separate queue or Aggregate Root. |
| Patient Identity Intake | Collection and accountable resolution of identity information needed to reference an existing Patient or request New Patient Recording. |
| New Patient Recording | Establishment of a new canonical Patient identity when no applicable Patient record exists and governing policy permits creation. |
| Coverage Arrangement | The payment-responsibility context proposed or accepted for a Registration, including self-pay, insurance, or another guarantor. |
| Guarantor | The party expected to bear some or all financial responsibility under an applicable arrangement. |
| Referral | Evidence that the Patient was directed to outpatient care by an applicable referring party. |
| Outpatient Destination | The intended outpatient service unit and responsible care provider for a Registration or Booking. |
| Poli | Established operational language for an outpatient service unit used as an Outpatient Destination. |
| Practice Schedule | The planned availability of a care provider at an Outpatient Destination. |
| Visit Date | The date on which outpatient service is planned or registered. |
| Initial Charge | The initial charge obligation established because the Registration grants access to an outpatient destination. |
| Karcis | The established business term for the registration-linked Initial Charge arrangement applicable to a destination. |
| Patient Journey | One logical continuity of a Patient's operational interactions, owned by Patient Tracker. |
| TrackerId | The stable logical identity of one Patient Journey, owned by Patient Tracker. |
| Queue Entry | A Patient's or anonymous visitor's participation in a Queue Session, owned by Patient Tracker. |
| Registration Outcome | The authoritative result of a registration attempt, including Registration Established or Registration Not Established. |

## 3. Business Capabilities

### 3.1 Patient Identity Intake

Obtain sufficient identity information to reference an existing Patient or initiate New Patient Recording. Detailed matching, correction, duplication, and creation policies remain subject to later elaboration.

### 3.2 Practice Schedule Management

Maintain recurring and date-specific outpatient availability used by Booking and Registration. Existing schedule artifacts refine this capability.

### 3.3 Booking Management

Plan an outpatient visit through HiDok or Admission-Assisted Booking against an applicable Practice Session.

Booking Management can:

- accept a valid canonical Patient reference or retain an unresolved identity snapshot;
- apply hard Schedule Capacity to HiDok while allowing accountable Admission-Assisted Capacity Override;
- obtain a physician Queue Number from the Hospital's authoritative queue-number allocation capability;
- prevent duplicate active Bookings for a canonically identified Patient in the same Practice Session;
- reschedule a Booking without replacing its business identity;
- associate the Booking with a later Outpatient Registration; and
- retain an unfulfilled Booking as historical evidence after its Visit Date.

Queue-number allocation and Patient Journey identity remain governed by Patient Tracker.

### 3.4 Registration Intake and Work Coordination

Recognize registration demand from By Booking and By Walk-In paths, determine when Registration Assistance is required, and expose active work without taking ownership of Patient Tracker queues.

### 3.5 Outpatient Registration

Establish the authoritative administrative record that connects the Patient, Visit Date, Outpatient Destination, and applicable administrative context.

### 3.6 Insurance and Guarantor Determination

Determine and retain the Coverage Arrangement applicable to the Registration. Detailed eligibility, authorization, coordination-of-benefits, and fallback policies remain subject to later elaboration.

### 3.7 Referral Qualification

Determine whether Referral evidence is required and applicable to the Registration. Detailed referral rules remain subject to later elaboration.

### 3.8 Outpatient Destination Assignment

Assign the intended Poli and responsible care provider consistently with applicable services and Practice Schedule policy.

### 3.9 Initial Charge Establishment

Determine the applicable `Karcis` and establish the registration-linked Initial Charge. Tarif authority, downstream billing, and settlement remain outside this capability.

## 4. Actors & Roles

### 4.1 Patient

The Patient supplies available identity and administrative evidence, chooses or accepts an outpatient destination, and participates in Self-Registration or assisted Registration.

### 4.2 Patient Representative

The Patient Representative supplies information or makes permitted administrative decisions on behalf of the Patient.

### 4.3 Admission Officer

The Admission Officer:

- records Admission-Assisted Booking requests regardless of whether they arrive by phone or WhatsApp;
- accepts or rejects requests beyond Schedule Capacity;
- resolves Registration Assistance;
- verifies available identity and administrative evidence;
- establishes or amends an Outpatient Registration within policy; and
- remains accountable for assisted registration decisions.

### 4.4 Schedule Administrator

The Schedule Administrator maintains Practice Schedule business truth and approved date-specific changes.

### 4.5 Care Provider

The Care Provider supplies availability and destination context for Booking and Registration but does not perform Admisi Rajal's administrative decision.

### 4.6 Guarantor Representative

The Guarantor Representative supplies or confirms coverage evidence on behalf of a guarantor. Admisi Rajal remains responsible for the Coverage Arrangement recorded on the Registration.

## 5. Domain Objects

### 5.1 Outpatient Registration

Represents one Patient's administrative admission to one intended outpatient visit.

It retains the Patient reference, Visit Date, Outpatient Destination, Coverage Arrangement, referral context, and Initial Charge context required by applicable policy.

### 5.2 Booking

Represents a plan for an outpatient visit before Registration is established.

A Booking retains its Booking Channel, Visit Date, Practice Session, Outpatient Destination, physician Queue Number, and either a canonical Patient reference or an unresolved identity snapshot. It may later be associated with one Outpatient Registration but remains distinct from that Registration.

Rescheduling changes the applicable Practice Session without replacing the Booking's business identity. An unfulfilled Booking remains historical evidence after its Visit Date.

### 5.3 Practice Schedule Template

Represents recurring provider availability used to plan outpatient service dates.

### 5.4 Daily Practice Schedule

Represents the effective provider availability or approved exception for one specific date.

### 5.5 Registration Assistance

Represents the business need for an Admission Officer to resolve a registration attempt. Its queue participation and service milestones are owned by Patient Tracker.

### 5.6 Admisi Rajal Work List

Represents the current collection of active Registration Assistance. It derives membership from Patient Tracker queue truth and combines it with Admisi Rajal registration context. It is not an independent business ledger.

### 5.7 Patient Identity Reference

Identifies the canonical Patient used by Booking or Outpatient Registration. Identity snapshots may support resolution but do not replace the Patient master.

### 5.8 Coverage Arrangement

Represents the payment-responsibility basis accepted for one Registration. Its detailed policy will be elaborated later.

### 5.9 Outpatient Destination

Represents the intended Poli and responsible care provider for the outpatient visit.

### 5.10 Initial Charge

Represents the initial financial obligation arising from administrative access to the selected Outpatient Destination. It references applicable tariff truth without owning tariff policy or settlement.

## 6. Aggregates

### 6.1 Outpatient Registration Aggregate

**Aggregate Root:** `Outpatient Registration`

The aggregate keeps the Patient reference, Visit Date, destination, Coverage Arrangement, referral context, Initial Charge context, and Registration status mutually consistent.

Detailed internal boundaries for coverage, referral, and charge components remain subject to later feature elaboration.

### 6.2 Booking Aggregate

**Aggregate Root:** `Booking`

The aggregate keeps Booking Channel, Patient identity information, Visit Date, Practice Session, Outpatient Destination, Schedule Capacity decision, physician queue reservation reference, and relationship to an established Registration mutually consistent.

The aggregate preserves Booking identity through rescheduling. It does not own Queue Number allocation or Patient Journey identity.

### 6.3 Practice Schedule Template Aggregate

**Aggregate Root:** `Practice Schedule Template`

The aggregate owns one recurring definition of provider availability, destination, operating interval, capacity, and queue pattern.

### 6.4 Daily Practice Schedule Aggregate

**Aggregate Root:** `Daily Practice Schedule`

The aggregate owns one date-specific schedule occurrence or approved exception. It remains independent from later changes to a recurring template when established as a manual exception.

### 6.5 Explicit non-aggregates

`Admisi Rajal Work List`, `Work Item`, `Queue Entry`, `TrackerId`, and `Patient Identity Reference` are not Admisi Rajal Aggregate Roots.

Queue consistency belongs to Patient Tracker. Canonical Patient identity consistency belongs to the Patient context.

## 7. Business Rules

### 7.1 Context and identity

- **BR-ARJ-001** — Every established Outpatient Registration shall reference exactly one canonical Patient.
- **BR-ARJ-002** — `RegId` shall identify an Outpatient Registration and shall not replace `TrackerId` as Patient Journey identity.
- **BR-ARJ-003** — Admisi Rajal shall not treat an identity snapshot, equal name, or equal date of birth as conclusive proof of canonical Patient identity.
- **BR-ARJ-004** — A Booking and an Outpatient Registration shall remain distinct business records even when the Registration originates from that Booking.

### 7.2 Intake paths and Self-Registration

- **BR-ARJ-005** — Every registration attempt shall have one Registration Intake Path: `By Booking` or `By Walk-In`.
- **BR-ARJ-006** — Successful Self-Registration shall establish an authoritative Outpatient Registration without creating Registration Assistance.
- **BR-ARJ-007** — `Self-Registration Requires Assistance` shall not be represented as successful Registration.
- **BR-ARJ-008** — After accountable evidence resolution, a Booking-based assistance request shall be associated with the applicable Patient Journey already associated with the Booking and shall not establish a duplicate Patient Journey. Presenting the Booking QR alone shall not automatically identify the Admisi Rajal Queue Entry.
- **BR-ARJ-009** — A Walk-In may enter Registration Assistance before canonical Patient identity or Patient Journey identity is known.

### 7.3 Work List and Patient Tracker boundary

- **BR-ARJ-010** — The Admisi Rajal Work List shall contain only active Registration Assistance for the Admisi Rajal Service Point.
- **BR-ARJ-011** — Each Work Item shall correspond to one active Queue Entry owned by Patient Tracker and shall not establish a second queue identity.
- **BR-ARJ-012** — Admisi Rajal shall not independently assign Queue Numbers or redefine Queue Entry state.
- **BR-ARJ-013** — Registration completion shall be determined by the Outpatient Registration outcome, not inferred solely from queue completion.
- **BR-ARJ-014** — Queue completion shall be recorded through Patient Tracker and shall not be inferred solely from Registration establishment.
- **BR-ARJ-015** — Repeated requests for assistance for the same unresolved Booking attempt shall not create more than one active Registration Assistance obligation.

### 7.4 Registration foundation

- **BR-ARJ-016** — Every established Outpatient Registration shall identify one Visit Date and one Outpatient Destination.
- **BR-ARJ-017** — An Outpatient Destination shall be eligible for outpatient service under the applicable destination and schedule policy.
- **BR-ARJ-018** — The Registration shall retain the Coverage Arrangement, referral context, and Initial Charge context required by the policy applicable at establishment time.
- **BR-ARJ-019** — Admisi Rajal shall not claim external guarantor eligibility when the required evidence has not been established.
- **BR-ARJ-020** — Establishing an Initial Charge shall not transfer tariff-policy or settlement ownership into Admisi Rajal.

### 7.5 Booking Management

- **BR-ARJ-023** — Every Booking shall originate from exactly one Booking Channel: `HiDok` or `Admission-Assisted`.
- **BR-ARJ-024** — Phone and WhatsApp shall be treated as communication methods of the `Admission-Assisted` Booking Channel and shall not create distinct Booking Channels.
- **BR-ARJ-025** — Every Booking shall identify one applicable, non-cancelled Practice Session, Visit Date, Outpatient Destination, and doctor.
- **BR-ARJ-026** — A supplied `PasienId` shall reference an existing canonical Patient; an invalid supplied `PasienId` shall cause the Booking request to be rejected.
- **BR-ARJ-027** — A Booking may be established without `PasienId` and shall then retain an Unresolved Patient Identity snapshot.
- **BR-ARJ-028** — Demographic similarity or ambiguity shall not by itself prevent Booking creation, establish canonical Patient identity, or authorize a Patient merge.
- **BR-ARJ-029** — HiDok shall not establish a Booking after Schedule Capacity for the Practice Session has been reached.
- **BR-ARJ-030** — An Admission Officer may reject an Admission-Assisted Booking after Schedule Capacity has been reached or accept it through an accountable Capacity Override.
- **BR-ARJ-031** — HiDok shall not exercise a Capacity Override.
- **BR-ARJ-032** — Every established Booking shall obtain its physician Queue Number from the Hospital's authoritative queue-number allocation capability; Admisi Rajal shall not define legacy or current allocation mechanics.
- **BR-ARJ-033** — One canonically identified Patient shall not hold more than one active Booking for the same doctor, Visit Date, and Practice Session.
- **BR-ARJ-034** — A Booking may be rescheduled and shall preserve its Booking identity and existing business data unless a changed fact requires replacement.
- **BR-ARJ-035** — Rescheduling shall require an applicable destination Practice Session and shall replace the old physician queue reservation with one for the new Practice Session through the owning queue capability.
- **BR-ARJ-036** — A Booking shall apply only to its Visit Date and shall not be used to obtain Registration on a later date.
- **BR-ARJ-037** — A Booking whose Visit Date passes without Registration, cancellation, or rescheduling shall remain as Expired Unfulfilled historical evidence.
- **BR-ARJ-038** — Expired Unfulfilled shall not be interpreted as proof that the Patient was physically absent.
- **BR-ARJ-039** — HiDok shall not provide Booking Cancellation; cancellation through another authorized hospital channel remains subject to its accountable policy.
- **BR-ARJ-040** — A Practice Schedule change or cancellation shall not silently move or invalidate an existing Booking.
- **BR-ARJ-041** — A Booking with Unresolved Patient Identity shall require identity validation through Admisi Rajal before Outpatient Registration can be established.
- **BR-ARJ-042** — Identity validation shall associate the Booking with one existing canonical Patient or complete New Patient Recording under the owning Patient policy.
- **BR-ARJ-043** — A Booking Patient may proceed to the physician waiting area only after Outpatient Registration has been established by Self-Registration or assisted Registration.
- **BR-ARJ-044** — Admisi Rajal Booking shall contribute Booking evidence to Patient Tracker but shall not define `TrackerId` creation, selection, reuse, or lifecycle rules.

### 7.6 Progressive elaboration

- **BR-ARJ-021** — A later feature artifact may refine a capability recognized by this document but shall not silently change the ownership boundaries or canonical terms defined here.
- **BR-ARJ-022** — When a later approved policy conflicts with this initial specification, both language companions and every affected feature artifact shall be updated together.

## 8. State Machines & Lifecycles

### 8.1 Outpatient Registration lifecycle

```text
Registration Not Established
  → Registration Established
      → Registration Cancelled
```

`Registration Established` means authoritative outpatient administrative access exists. `Registration Cancelled` means that access was subsequently invalidated under an accountable cancellation policy. Detailed visit-closing and correction states require later elaboration.

### 8.2 Booking lifecycle

```text
Booking Planned
  → Booking Rescheduled
      → Booking Planned

Booking Planned
  → Registration Established

Booking Planned
  → Booking Cancelled

Booking Planned
  → Expired Unfulfilled
```

A Booking does not itself become an Outpatient Registration. `Registration Established` means the Booking has been associated with the separately established Registration. Rescheduling preserves Booking identity. Expired Unfulfilled remains historical evidence and is not proof of physical absence.

### 8.3 Booking Patient identification lifecycle

```text
Unresolved Patient Identity
  → Canonical Patient Identified
```

A Booking may begin with either condition. Canonical identification is required before Outpatient Registration is established.

### 8.4 Daily Practice Schedule lifecycle

```text
Active
  → Cancelled

Active
  → Manually Overridden (Active)
```

A manual date-specific exception remains independent from later recurring-template changes, subject to the approved scheduling policies.

### 8.5 Registration Assistance interpretation

```text
Waiting
  → In Service
      → Done
```

These are Patient Tracker Queue Entry states interpreted by Admisi Rajal for Work List membership. They are not a second Admisi Rajal-owned state machine.

## 9. Domain Events

| Domain Event | Business meaning |
|---|---|
| Booking Planned | An outpatient visit was planned against an applicable destination and Visit Date. |
| Booking Rescheduled | An existing Booking was moved to another applicable Practice Session without replacing its identity. |
| Booking Cancelled | A planned outpatient visit was cancelled before its plan remained applicable. |
| Booking Expired Unfulfilled | A Booking's Visit Date passed without Registration, cancellation, or rescheduling. |
| Booking Patient Identified | A Booking with Unresolved Patient Identity was associated with one canonical Patient. |
| Capacity Override Granted | An Admission Officer accepted an Admission-Assisted Booking beyond Schedule Capacity. |
| Self-Registration Completed | Self-Registration established an authoritative Outpatient Registration. |
| Self-Registration Requires Assistance | Self-Registration did not establish a Registration and human assistance became necessary. |
| Registration Assistance Requested | An active need for assisted outpatient registration was recognized. |
| Registration Assistance Started | An Admission Officer began resolving the assistance obligation; queue-service truth remains owned by Patient Tracker. |
| Outpatient Registration Established | An authoritative Outpatient Registration was established. |
| Outpatient Registration Cancelled | A previously established Outpatient Registration was invalidated under an accountable policy. |
| Coverage Arrangement Determined | The applicable payment or guarantor basis was determined for a Registration. |
| Outpatient Destination Assigned | The intended Poli and care provider were assigned to the Registration. |
| Initial Charge Established | The Registration-linked Initial Charge obligation was established. |

These events express business facts. Their publication and delivery mechanisms belong to architecture artifacts.

## 10. Business Workflows

### 10.1 Create a HiDok Booking

```text
HiDok Booking requested
  → Supplied PasienId validated when present
  → Applicable Practice Session resolved
  → Schedule Capacity available
  → Physician Queue Number obtained from the Hospital queue authority
  → Booking Planned
  → Booking evidence contributed to Patient Tracker
```

HiDok does not provide Capacity Override or Booking Cancellation.

### 10.2 Create an Admission-Assisted Booking

```text
Booking request received by phone or WhatsApp
  → Admission Officer records the request as Admission-Assisted
  → Supplied PasienId validated when present
  → Applicable Practice Session resolved
  → Schedule Capacity evaluated
      → Within capacity: request may be accepted
      → Beyond capacity: Admission Officer accepts or rejects
  → Physician Queue Number obtained from the Hospital queue authority when accepted
  → Booking Planned
  → Booking evidence contributed to Patient Tracker
```

### 10.3 Reschedule a Booking

```text
Existing Booking selected for rescheduling
  → New applicable Practice Session resolved
  → Channel capacity policy applied
  → New physician Queue Number obtained through the Hospital queue authority
  → Old physician queue reservation released through the owning queue capability
  → Booking Rescheduled
  → Same Booking identity retained
```

### 10.4 By Booking with successful Self-Registration

```text
Booking Planned
  → Self-Registration attempted
  → Canonical Patient identity and applicable registration context validated
  → Outpatient Registration Established
  → Self-Registration Completed
  → No Registration Assistance required
  → Patient continues toward the booked Outpatient Destination
```

### 10.5 By Booking requiring identity validation or other assistance

```text
Booking Planned
  → Self-Registration attempted
  → Self-Registration Requires Assistance
  → Anonymous Admisi Rajal Queue Entry provided by Patient Tracker without creating or selecting a Tracker
  → Registration Assistance appears in the Admisi Rajal Work List
  → Admission Officer asks for evidence and resolves the applicable existing Booking Patient Journey
  → Queue Entry is associated with that resolved Tracker
  → Admission Officer resolves other required registration context
  → Outpatient Registration Established or Registration Not Established
  → Queue service completed through Patient Tracker
```

An Unresolved Patient Identity is an explicit reason for Registration Assistance rather than proof of a technical Self-Registration failure.

### 10.6 By Walk-In

```text
Patient requests assisted outpatient registration
  → Anonymous Admisi Rajal Queue Entry may be provided by Patient Tracker
  → Registration Assistance appears in the Admisi Rajal Work List
  → Admission Officer starts assistance
  → Patient Journey and canonical Patient identity resolved or established through their owning contexts
  → Visit Date, Outpatient Destination, and applicable registration context determined
  → Outpatient Registration Established or Registration Not Established
  → Queue service completed through Patient Tracker
```

### 10.7 Progressive registration-context determination

```text
Registration demand recognized
  → Patient identity context determined
  → Coverage and guarantor context determined as applicable
  → Referral context determined as applicable
  → Outpatient Destination determined
  → Initial Charge context determined
  → Outpatient Registration Established
```

The detailed ordering, optionality, evidence requirements, exceptions, and correction policies in this workflow remain subjects for later feature specifications.
