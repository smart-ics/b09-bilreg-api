# Patient Tracker Domain

**Artifact status:** Canonical business specification

**Bounded context:** Patient Tracker

**Version scope:** Pragmatic V1

**Admission queue feature specification:** [Admission Queue Operations Domain](./TRACKER-ADMISSION-QUEUE-DOMAIN.md)

## 1. Business Overview

### 1.1 Purpose

Patient Tracker preserves the continuity of one logical Patient Journey across booking, registration, consultation, pharmacy, and other Service Points that contribute operational evidence.

It records what the organization can prove from business interactions. It does not continuously observe the Patient and does not claim the Patient's physical position between interactions.

### 1.2 Business value

Patient Tracker provides:

- one stable `TrackerId` for one logical Patient Journey;
- one cumulative timeline of operational evidence contributed by other business activities;
- continuity between anonymous queue intake and an identified Patient Journey;
- Service Point queues with explicit waiting, service-start, and completion milestones;
- candidate journeys for an operator to resolve when demographic evidence is not unique; and
- waiting-time interpretations that do not misrepresent booking time or unobserved physical movement.

### 1.3 Scope

V1 owns five business capabilities:

1. Logical Patient Journey Tracking.
2. Operational Evidence Timeline.
3. Service Point Queue Coordination.
4. Journey Candidate Resolution.
5. Operational Time Interpretation.

The V1 journey foundation contains two aggregates:

1. `Patient Tracker Aggregate`.
2. `Queue Session Aggregate`.

The Admission Queue Operations feature elaborates Queue Session behavior and defines the supporting stable Service Point, Loket, and Kiosk business identities in its feature domain specification.

### 1.4 Business boundaries

Patient Tracker owns the logical journey identity, tracking period, cumulative evidence timeline, queue sessions, queue entries, and queue-service milestones.

Patient Tracker does not own:

- Patient master identity;
- Booking, Registration, Medical Chart, prescription, drug-sale, or other source transactions;
- clinical care or pharmacy dispensing;
- the operational truth contained in a referenced source transaction;
- the Patient's physical location or travel time;
- GPS, beacon, wearable, or other continuous physical tracking;
- a canonical catalogue of Tracker Event descriptions;
- compliance audit history; or
- event sourcing or a generic cross-context event framework.

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Patient Journey | One logical continuity of a Patient's operational interactions for a planned or actual visit. |
| Patient Tracker | The authoritative business record that identifies one Patient Journey and owns its cumulative Tracker Events. |
| TrackerId | The stable logical identity of one Patient Journey. It is not a physical token or tracking device. |
| Person Identity Snapshot | The Patient name and date of birth retained to support journey recognition; it is not the Patient master record. |
| StartPeriod | The date of the first evidence recorded for a Patient Journey. |
| LastPeriod | The inclusive upper date used to find a Patient Journey. For a booking, it begins at the VisitDate and may later extend to a later evidence date. |
| Tracking Period | The inclusive date interval from StartPeriod through LastPeriod used to limit journey candidates. |
| VisitDate | The planned date on which the Patient is expected to receive service. For a booking-created journey, it establishes the initial LastPeriod. |
| Tracker Event | One append-only item of operational evidence contributed to a Patient Tracker by a source business activity. It is not a physical-location observation. |
| Event Description | Free-text wording that helps a human understand a Tracker Event. It is display evidence and is not a V1 search key or behavioral discriminator. |
| Evidence Reference | The stable reference to the strongest operational evidence available when a Tracker Event is recorded. It is represented by ReffId. |
| Source Transaction Reference | An Evidence Reference produced by the primary business activity, such as a Booking, Registration, Medical Chart, or drug-sale transaction. |
| Queue Evidence Reference | The combination of Queue Session identity and Queue Number used as Evidence Reference when no primary source transaction yet exists. |
| OccurredAt | The business time at which the evidenced interaction occurred. It may differ from the later time at which the evidence is associated with a Patient Tracker. |
| Journey Candidate | A Patient Tracker returned because its Person Identity Snapshot and Tracking Period match the available search evidence. |
| Journey Resolution | The accountable human decision to associate a Queue Entry with an applicable existing Patient Tracker or, when an owning source activity establishes a new journey, with the newly established Patient Tracker. |
| Queue Session | One time-bounded queue for one Service Point on one Session Date. |
| Service Point | The operational place or responsibility at which Patients wait for service, such as an admission counter, physician, or outpatient pharmacy. |
| Queue Entry | One Patient's or anonymous visitor's participation in one Queue Session. |
| Queue Number | A number unique within one Queue Session. The same number may exist in another Queue Session. |
| Anonymous Queue Entry | A Queue Entry created before a Patient Tracker has been identified. |
| Identified Queue Entry | A Queue Entry associated with exactly one Patient Tracker. |
| CreatedAt | The time a Queue Entry joined or was reserved in a queue. It does not universally mean physical arrival. |
| ServedAt | The time the Service Point recognizes that operational service has started. |
| DoneAt | The time the Service Point recognizes that operational service has completed. |
| Waiting | The Queue Entry state after creation and before service starts. |
| In Service | The Queue Entry state after service starts and before it completes. |
| Done | The final Queue Entry state after service completes. |
| Withdrawn | The final Queue Entry state when participation ends before service starts under an applicable feature policy. |
| Registration Waiting Time | The interval from admission-counter CreatedAt to admission-counter ServedAt. |
| Post-Registration Consultation Waiting Time | The interval from registration DoneAt to physician ServedAt, regardless of when the physician Queue Entry was created. |
| Service Duration | The interval from ServedAt to DoneAt for one Queue Entry. |

## 3. Business Capabilities

### 3.1 Logical Patient Journey Tracking

Patient Tracker can:

- establish one stable TrackerId before a RegId exists;
- retain the same TrackerId across all evidenced stages of one journey;
- maintain the Person Identity Snapshot and Tracking Period; and
- distinguish separate journeys without treating name and date of birth as a unique identity.

### 3.2 Operational Evidence Timeline

Patient Tracker can:

- append evidence received from Booking, Registration, clinical, pharmacy, queue, and other source activities;
- preserve the source's Evidence Reference and OccurredAt;
- show all Tracker Events cumulatively in chronological order; and
- retain different kinds of Evidence Reference within one journey.

The timeline reports evidence. It does not take ownership of the source activity's operational truth.

### 3.3 Service Point Queue Coordination

Patient Tracker can:

- establish one Queue Session for a Service Point and operating interval;
- assign Queue Numbers unique within that Queue Session;
- admit anonymous or identified Queue Entries;
- associate an anonymous Queue Entry with a resolved Patient Tracker; and
- track each Queue Entry from Waiting through In Service to Done, or to Withdrawn when an applicable feature policy ends participation before service starts.

### 3.4 Journey Candidate Resolution

Patient Tracker can:

- find Journey Candidates from name, date of birth, and a relevant business date;
- expose each candidate's operational evidence;
- preserve multiple valid candidates instead of guessing; and
- let an accountable operator select the applicable existing journey; when no applicable journey exists, the owning source activity may establish a new Patient Tracker from its authoritative evidence.

### 3.5 Operational Time Interpretation

Patient Tracker can distinguish:

- advance reservation time from actual service waiting;
- anonymous queue intake time from later identity resolution;
- registration completion from physician service start;
- pharmacy queue creation from physical arrival; and
- observed system interactions from unobserved movement.

## 4. Actors & Roles

### 4.1 Patient or Visitor

The Patient or Visitor:

- participates in one or more Service Point queues;
- supplies available identity evidence when requested; and
- may present a Queue Number or booking evidence.

The Patient does not need to carry the internal TrackerId as a physical token.

### 4.2 Journey Resolution Operator

The Journey Resolution Operator:

- searches for Journey Candidates using available evidence;
- reviews the evidence shown for each candidate;
- selects the applicable Patient Journey or establishes a new one; and
- associates the selected journey with an Anonymous Queue Entry.

### 4.3 Service Point Operator

The Service Point Operator:

- coordinates Queue Entries for one Service Point;
- recognizes when service starts and completes; and
- contributes the appropriate queue milestones to the Patient Journey.

### 4.4 Source Activity Owner

The Source Activity Owner is the accountable business role responsible for a primary activity such as Booking, Registration, consultation, or pharmacy sale. This role:

- performs or completes the primary business activity;
- supplies its business time and source reference as operational evidence; and
- remains responsible for the truth of the source transaction.

## 5. Domain Objects

### 5.1 Entities

#### Patient Tracker

Purpose: represents one logical Patient Journey.

Responsibilities:

- retain a stable TrackerId;
- retain the Person Identity Snapshot;
- establish and maintain StartPeriod and LastPeriod;
- own the complete collection of Tracker Events; and
- expose evidence for human Journey Resolution.

#### Tracker Event

Purpose: preserves one item of operational evidence within a Patient Journey.

Responsibilities:

- retain its Event Description;
- retain the Evidence Reference supplied by the source activity;
- retain OccurredAt as the source's business time; and
- remain append-only after it is recorded.

A Tracker Event is not a generic integration event, a compliance audit entry, or evidence of continuous physical location.

#### Queue Session

Purpose: represents one queue for one Service Point on one Session Date and operating interval.

Responsibilities:

- retain its stable Queue Session identity;
- identify the Service Point, Session Date, Start Time, and End Time;
- own its Queue Entries; and
- keep Queue Numbers unique within the session.

#### Queue Entry

Purpose: represents one anonymous visitor's or identified Patient Journey's participation in a Queue Session.

Responsibilities:

- retain its Queue Number;
- retain an optional Patient Tracker association and available name snapshot;
- retain CreatedAt, ServedAt, and DoneAt milestones; and
- transition from Waiting to In Service to Done, or from Waiting to Withdrawn under an applicable feature policy.

### 5.2 Value Objects

#### TrackerId

The immutable logical identifier of one Patient Journey. A TrackerId may be established before Registration produces a RegId and remains unchanged when later source transactions are created.

#### Person Identity Snapshot

The name and date of birth used for journey recognition. Equal snapshots may produce multiple Journey Candidates and shall not be treated as proof that two journeys are the same.

#### Tracking Period

The inclusive StartPeriod and LastPeriod boundaries used to limit Journey Candidates. The period supports cross-date booking without changing the first event's truth.

#### Evidence Reference

The reference to the strongest available operational evidence. It may identify a source transaction or a Queue Evidence Reference. Its purpose is provenance, not Start-to-Done correlation.

#### Queue Evidence Reference

The combination of Queue Session identity and Queue Number. It identifies one Queue Entry even when no primary transaction exists.

#### Service Point

The named operational responsibility that owns a Queue Session and recognizes service milestones.

## 6. Aggregates

### 6.1 Patient Tracker Aggregate

**Aggregate Root:** `Patient Tracker`

**Owned entities:**

- one or more `Tracker Event` entities.

**Consistency boundary:**

The aggregate keeps TrackerId, Person Identity Snapshot, Tracking Period, and the cumulative evidence timeline mutually consistent.

Only the Patient Tracker may:

- append a Tracker Event to its journey;
- establish StartPeriod from its first evidence;
- initialize LastPeriod from an applicable VisitDate; or
- extend LastPeriod when later evidence falls after the current boundary.

The aggregate does not validate or replace the business truth of the referenced Booking, Registration, Medical Chart, pharmacy sale, or Queue Entry.

### 6.2 Queue Session Aggregate

**Aggregate Root:** `Queue Session`

**Owned entities:**

- zero or more `Queue Entry` entities.

**Consistency boundary:**

The aggregate keeps Service Point identity, session timing, Queue Number uniqueness, Queue Entry identity association, and service milestones mutually consistent.

Only the Queue Session may:

- assign a Queue Number;
- add an Anonymous or Identified Queue Entry;
- associate an Anonymous Queue Entry with one resolved Patient Tracker;
- start service for a Waiting Queue Entry; or
- complete an In Service Queue Entry; or
- withdraw a Waiting Queue Entry under an applicable feature policy.

A Queue Entry references a Patient Tracker by TrackerId but does not own or modify the Patient Tracker Aggregate.

## 7. Business Rules

### 7.1 Patient Journey identity and period

- **BR-TRK-001** — A Patient Tracker shall represent exactly one logical Patient Journey.
- **BR-TRK-002** — Every Patient Tracker shall have one stable TrackerId and one Person Identity Snapshot.
- **BR-TRK-003** — TrackerId shall identify a logical journey and shall not be represented as a physical tracker, Patient location, or physical token.
- **BR-TRK-004** — A Patient Tracker shall contain at least one Tracker Event when it is established.
- **BR-TRK-005** — StartPeriod shall equal the calendar date of the first Tracker Event and shall preserve that first-evidence truth.
- **BR-TRK-006** — For a Patient Tracker established from Booking, LastPeriod shall initially equal the later of StartPeriod and VisitDate.
- **BR-TRK-007** — For a Patient Tracker established without a future VisitDate, LastPeriod shall initially equal StartPeriod.
- **BR-TRK-008** — Recording later evidence shall set LastPeriod to the later of its current value and the Tracker Event's OccurredAt date; LastPeriod shall never move backward.
- **BR-TRK-009** — Creation of a RegId or another source transaction shall not replace TrackerId.
- **BR-TRK-009a** — TrackerId shall remain immutable across visit change (reschedule), registration cancellation, and booking deletion. Those workflows append Tracker Events and shall not create a replacement TrackerId or delete the Patient Tracker aggregate.
- **BR-TRK-009b** — Cancellation and visit-change evidence use the following Event Descriptions and Evidence References; prior events remain append-only history:

  | Workflow | Event Description | Evidence Reference |
  |---|---|---|
  | Booking deletion | `BOOKING_CANCELLED` | BookingId |
  | Registration cancellation | `REGISTER_CANCELLED` | RegId |
  | Visit change | `VISIT_CHANGED` | RegId |

- **BR-TRK-009c** — Releasing a legacy physician slot (`AntrianMap`) or removing a Queue Entry shall not require deleting Patient Tracker evidence. Slot and queue cleanup remain independent of Tracker retention.

### 7.2 Tracker evidence

- **BR-TRK-010** — Every Tracker Event shall identify one Patient Tracker, one Event Description, one Evidence Reference, and one OccurredAt.
- **BR-TRK-011** — A Tracker Event shall be contributed only from evidence produced by another accountable business activity or by a Queue Entry owned by this domain.
- **BR-TRK-012** — When the primary source transaction exists, its stable business identity shall be used as the Evidence Reference.
- **BR-TRK-013** — When the primary source transaction does not yet exist, the applicable Queue Evidence Reference may be used as the Evidence Reference.
- **BR-TRK-014** — Events for the Start and Done milestones of one service may use different kinds of Evidence Reference because each reference identifies the strongest evidence available at that milestone.
- **BR-TRK-015** — Evidence Reference shall express provenance and shall not be required to correlate all events in one service lifecycle.
- **BR-TRK-016** — Event Description shall remain free text for human display and shall not determine V1 search, identity resolution, state transition, or business behavior.
- **BR-TRK-017** — Tracker Events shall be cumulative and append-only; adding an event shall not remove or replace prior evidence.
- **BR-TRK-018** — The journey timeline shall be presented chronologically by OccurredAt while preserving a deterministic recorded order for equal timestamps.
- **BR-TRK-019** — OccurredAt shall preserve the source activity's business time even when the Tracker Event is associated with the journey later.

### 7.3 Journey Candidate Resolution

- **BR-TRK-020** — Journey Candidate search shall use the available name, date of birth, and a relevant business date.
- **BR-TRK-021** — A Patient Tracker is temporally eligible when the relevant business date falls inclusively between StartPeriod and LastPeriod.
- **BR-TRK-022** — Search shall return every matching Journey Candidate and shall not silently choose among equal Person Identity Snapshots.
- **BR-TRK-023** — When multiple candidates remain, an accountable Journey Resolution Operator shall select the applicable journey from the available evidence.
- **BR-TRK-024** — Equal name and date of birth shall not merge Patient Trackers or prove that two journeys are the same.
- **BR-TRK-025** — When no candidate is applicable, the accountable owning source activity may establish a new Patient Tracker from its authoritative identity and operational evidence; obtaining a Queue Number alone shall not establish a Patient Tracker.

### 7.4 Queue Session and Queue Entry identity

- **BR-TRK-026** — Every Queue Session shall identify exactly one Service Point, one Session Date, one Start Time, and one End Time.
- **BR-TRK-027** — Queue Numbers shall be unique within a Queue Session but may repeat across different Queue Sessions.
- **BR-TRK-028** — Every Queue Entry shall belong to exactly one Queue Session and shall retain exactly one Queue Number within that session.
- **BR-TRK-029** — Obtaining a Queue Number may create an Anonymous Queue Entry when the applicable Patient Journey has not yet been accountably resolved; Queue Number allocation shall not by itself create a Patient Tracker.
- **BR-TRK-030** — A Queue Entry directly created or reserved by Booking or another identified upstream source activity shall reference exactly one Patient Tracker from creation. An admission Queue Entry issued after a Booking Self-Registration attempt requires assistance is not a Booking-created Queue Entry for this rule and may remain Anonymous until an Admission Officer resolves the journey from Patient-supplied evidence.
- **BR-TRK-031** — An Anonymous Queue Entry may become Identified only after an accountable resolution associates it with one existing Patient Tracker or an owning source activity establishes a new Patient Tracker from authoritative evidence.
- **BR-TRK-031a** — A Walk-In admission Queue Entry shall remain Anonymous until Registration establishes a new Patient Tracker or accountable Journey Resolution selects an applicable existing Patient Tracker.
- **BR-TRK-031b** — A Booking Patient's admission Queue Entry shall not be automatically associated from the presented Booking QR alone; an Admission Officer shall use Patient-supplied evidence to resolve and associate the applicable existing Booking Patient Tracker.
- **BR-TRK-032** — One Patient Tracker may participate in multiple Queue Sessions, but one Queue Entry shall reference at most one Patient Tracker.
- **BR-TRK-033** — CreatedAt shall record when the Queue Entry was created or reserved and shall not universally be interpreted as physical arrival.
- **BR-TRK-034** — A booking-created Queue Entry may have CreatedAt before its Queue Session Date or Start Time without changing the Booking occurrence time.

### 7.5 Queue Entry service lifecycle

- **BR-TRK-035** — A newly created Queue Entry shall be Waiting, with CreatedAt recorded and ServedAt and DoneAt absent.
- **BR-TRK-036** — Only a Waiting Queue Entry may enter In Service, and entering In Service shall record ServedAt.
- **BR-TRK-037** — Only an In Service Queue Entry may become Done, and becoming Done shall record DoneAt.
- **BR-TRK-038** — Queue lifecycle timestamps shall be valid business times. In Fixed Business Date simulation, ServedAt may precede CreatedAt and DoneAt may precede ServedAt; consumers that calculate durations must explicitly tolerate or exclude negative intervals.
- **BR-TRK-039** — A Done Queue Entry is final in V1 and shall not return to Waiting or In Service.
- **BR-TRK-039a** — An applicable feature policy may make a Waiting Queue Entry Withdrawn when participation ends before service starts; a Withdrawn Queue Entry is final and shall not be represented as completed service.
- **BR-TRK-052** — Outpatient pharmacy is an applicable feature policy under `BR-TRK-039a`. When Apotek records a Pharmacy Queue Close with a mandatory reason for a Waiting Pharmacy Queue Entry that has not entered In Service, Patient Tracker shall make that Queue Entry Withdrawn. TAKEN shall not be added as a Patient Tracker state. No additional queue state shall be introduced.

### 7.6 Operational time interpretation

- **BR-TRK-040** — Registration Waiting Time shall be measured from the admission Queue Entry's CreatedAt to its ServedAt.
- **BR-TRK-041** — Registration Service Duration shall be measured from the admission Queue Entry's ServedAt to its DoneAt.
- **BR-TRK-042** — Post-Registration Consultation Waiting Time shall be measured from registration DoneAt to physician ServedAt and shall not use a booking-created physician Queue Entry's CreatedAt.
- **BR-TRK-043** — Consultation Service Duration shall be measured from the physician Queue Entry's ServedAt to its DoneAt.
- **BR-TRK-044** — Pharmacy Queue Entry creation from a prescription or completed consultation shall not be treated as proof that the Patient physically arrived at the pharmacy.
- **BR-TRK-045** — For every V1 outpatient pharmacy payer path, `Medication Preparation Started` supplied by Apotek shall cause the Pharmacy Queue Entry to enter In Service, record ServedAt, and establish pharmacy service-start evidence.
- **BR-TRK-045a** — The coordinated outpatient pharmacy pickup call shall cause the Pharmacy Queue Entry to become Done and record DoneAt. Queue completion shall not assert that Medication Handover has occurred.
- **BR-TRK-046** — Pharmacy Service Duration shall be measured from pharmacy ServedAt to pharmacy DoneAt.
- **BR-TRK-051** — Patient Tracker `QueueEntry` is the sole canonical outpatient-pharmacy queue identity. Legacy Farinv queue identity is deprecated and shall not create active queue records. Historical Farinv queue data is read-only. No dual-active queue model is permitted. `Apotek-Start` and `Apotek-Done` evidence shall reference the canonical `QueueEntryId`.
- **BR-TRK-047** — Patient Tracker shall not infer physical position, travel start, travel completion, or waiting-room arrival when no accountable business interaction occurred.

### 7.7 Ownership and historical truth

- **BR-TRK-048** — A referenced source activity remains authoritative for its own transaction content and business outcome.
- **BR-TRK-049** — Patient Tracker shall preserve the supplied source time and Evidence Reference without rewriting the source transaction's history.
- **BR-TRK-050** — Queue milestones and Tracker Events shall represent operational evidence and shall not replace compliance audit obligations.

## 8. State Machines & Lifecycles

### 8.1 Patient Tracker lifecycle

Patient Tracker has no Open, Closed, or Completed status in V1. Its lifecycle is evidence growth within a bounded Tracking Period.

```text
First operational evidence
          |
          v
Patient Tracker established
          |
          +----> Tracker Event appended ----+
          |                                 |
          +----> LastPeriod extended -------+
          |                                 |
          +---------------------------------+
```

For a booking-created journey:

```text
StartPeriod = Booking OccurredAt date
LastPeriod  = max(StartPeriod, VisitDate)

Later Tracker Event
  → LastPeriod = max(current LastPeriod, event OccurredAt date)
```

### 8.2 Tracker Event lifecycle

```text
Source activity produces operational evidence
  → Evidence associated with one Patient Tracker
  → Tracker Event recorded
  → Tracker Event remains immutable
```

The Event Description may vary because it is free text. The Evidence Reference and OccurredAt preserve the source evidence used when the event was recorded.

### 8.3 Queue Entry service lifecycle

```text
Queue Entry created
        |
        v
     Waiting
        |
        | Service starts / ServedAt recorded
        v
    In Service
        |
        | Service completes / DoneAt recorded
        v
       Done

     Waiting
        |
        | Participation ends before service starts
        v
    Withdrawn
```

| State | Business meaning | Allowed next state |
|---|---|---|
| Waiting | The Queue Entry exists and service has not started. | In Service or Withdrawn under an applicable feature policy |
| In Service | The Service Point has recognized service start. | Done |
| Done | The Service Point has recognized service completion. | None |
| Withdrawn | Queue participation ended before service started. | None |

### 8.4 Queue Entry identification lifecycle

```text
Anonymous Queue Entry
          |
          | Accountable association with an existing Tracker,
          | or new Tracker established by an owning source activity
          v
Identified Queue Entry
```

A physician Queue Entry directly created by Booking, or an entry directly created by another identified source activity, begins as Identified and does not pass through the Anonymous condition. An admission Queue Entry issued because Booking Self-Registration requires assistance is a separate Queue Entry and may begin Anonymous.

## 9. Domain Events

The events in this section are stable business facts of this bounded context. They are distinct from the free-text Event Description stored inside a Tracker Event.

| Domain Event | Business meaning |
|---|---|
| Patient Tracker Established | A logical Patient Journey was established from its first evidence. |
| Tracker Evidence Recorded | One operational evidence item was appended to a Patient Tracker. |
| Tracking Period Extended | Later evidence moved LastPeriod beyond its prior boundary. |
| Journey Candidate Selected | An accountable operator selected one Patient Tracker from the available evidence. |
| Queue Session Established | A queue was established for one Service Point and operating interval. |
| Queue Entry Created | An Anonymous or Identified Queue Entry received a Queue Number. |
| Queue Entry Identified | An Anonymous Queue Entry was associated with one resolved Patient Tracker. |
| Queue Service Started | A Waiting Queue Entry entered In Service and received ServedAt. |
| Queue Service Completed | An In Service Queue Entry became Done and received DoneAt. |
| Queue Entry Withdrawn | A Waiting Queue Entry ended before service started under an applicable feature policy. |
| Booking Queue Number Assigned | A Queue Number reserved in a physician Queue Session was associated with its Booking. |

## 10. Business Workflows

### 10.1 Establish a journey from Booking

```text
Booking established with VisitDate
  → Patient Tracker established with Booking evidence
  → StartPeriod set from Booking OccurredAt date
  → LastPeriod set to max(StartPeriod, VisitDate)
  → Physician Queue Session found or established for VisitDate
  → Identified Queue Entry created with booking OccurredAt as CreatedAt
  → Queue Number associated with Booking
```

The physician Queue Entry's CreatedAt preserves booking truth. It is not the start of Post-Registration Consultation Waiting Time.

### 10.2 Take an anonymous admission queue number

```text
Patient requests a number for an admission Service Point
  → Admission Queue Session found or established
  → Anonymous Queue Entry created
  → Queue Number and CreatedAt recorded
```

This flow applies to both a Walk-In Patient and a Booking Patient whose Self-Registration requires assistance. Presenting or scanning Booking evidence does not automatically identify the admission Queue Entry and does not create another Patient Tracker. No Patient Tracker evidence is added until the Queue Entry is accountably associated with a resolved journey.

### 10.3 Resolve the journey and perform Registration

```text
Admission Queue Number called
  → Admission Officer asks the Patient for identity and available visit evidence
  → Journey Candidates found from the available evidence and relevant date
  → Booking path: Operator selects the applicable existing Booking Tracker
  → Walk-In path with applicable existing journey: Operator selects that Tracker
  → Walk-In path without applicable journey: Registration establishes a new Tracker from Registration evidence
  → Anonymous Queue Entry becomes Identified with the selected or newly established Tracker
  → Queue service starts and ServedAt is recorded
  → Check-in evidence uses Queue Evidence Reference and queue CreatedAt
  → Registration-start evidence uses Queue Evidence Reference and queue ServedAt
  → Registration completes and DoneAt is recorded
  → Registration-done evidence uses the Registration transaction reference
```

The Registration transaction remains authoritative for the registration outcome. For a Walk-In without an applicable existing journey, obtaining or calling the Queue Number does not create a Tracker; the Tracker is established when Registration provides the authoritative source evidence, and the existing Queue Entry is then associated with it. OccurredAt on later-appended queue evidence preserves the earlier queue milestone times.

### 10.4 Perform physician consultation

```text
Registration completed
  → Patient remains represented by the booking-created physician Queue Entry
  → Physician service starts and ServedAt is recorded
  → Consultation-start evidence uses Queue Evidence Reference
  → Consultation completes and DoneAt is recorded
  → Consultation-done evidence uses the Medical Chart transaction reference
```

Post-Registration Consultation Waiting Time is registration DoneAt to physician ServedAt.

### 10.5 Establish downstream pharmacy work

```text
Consultation completes and prescription work is generated
  → Pharmacy Queue Session found or established on the Patient Tracker queue platform
  → Canonical Pharmacy Queue Entry (`QueueEntry`) created for the outpatient pharmacy Service Point
  → Pharmacy CreatedAt records queue creation
  → No physical pharmacy-arrival event is inferred
  → Legacy Farinv queue records are not created for new interactions
```

### 10.6 Perform and complete pharmacy service

```text
Apotek reports Medication Preparation Started
  → Pharmacy Queue Entry enters In Service
  → Pharmacy ServedAt uses the preparation-start time
  → Apotek later reports the coordinated pickup call
  → Pharmacy Queue Entry becomes Done
  → Pharmacy DoneAt uses the pickup-call time

Apotek records Pharmacy Queue Close with mandatory reason while Waiting
  → Pharmacy Queue Entry becomes Withdrawn
  → ServedAt and DoneAt remain absent
```

The queue lifecycle describes operational pharmacy queue progress. It does not prove Final Dispense Review, Patient Education, Medication Dispense, or Medication Handover. Detailed outpatient pharmacy sequencing is owned by the [Outpatient Apotek workflow](../apotek/outpatient-apotek-workflow.md).

### 10.7 Resolve multiple Journey Candidates

```text
Name, date of birth, and relevant date supplied
  → All temporally eligible Journey Candidates shown with evidence
  → No automatic demographic merge or selection
  → Accountable operator selects one journey
     or establishes a new Patient Tracker
  → Selected TrackerId associated with the current Queue Entry
```

### 10.8 Interpret an outpatient journey timeline

```text
Booking time
  → advance physician queue reservation

Admission CreatedAt → Admission ServedAt
  = Registration Waiting Time

Admission ServedAt → Admission DoneAt
  = Registration Service Duration

Admission DoneAt → Physician ServedAt
  = Post-Registration Consultation Waiting Time

Physician ServedAt → Physician DoneAt
  = Consultation Service Duration

Pharmacy ServedAt → Pharmacy DoneAt
  = Pharmacy Service Duration
```

No interval in this workflow is evidence of physical movement unless a separate accountable business interaction explicitly records it.
