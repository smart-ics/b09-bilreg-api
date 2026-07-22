# Patient Tracker — Admission Queue Operations Domain

**Artifact status:** Canonical feature business specification

**Bounded context:** Patient Tracker

**Parent domain:** [Patient Tracker Domain](./TRACKER-DOMAIN.md)

**Bahasa Indonesia companion:** [Domain Operasi Antrean Admisi](./TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md)

**Operational specification:** [Admission Queue Operations SOP](./TRACKER-ADMISSION-QUEUE-SOP.md)

**Technical specification:** [Admission Queue Operations Architecture](./TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md)

## 1. Business Overview

### 1.1 Purpose

Admission Queue Operations coordinates how Patients or Visitors obtain, wait with, are called by, and receive Registration Assistance through an outpatient admission queue.

It distinguishes the queue category represented by a `Service Point` from the physical service desk represented by a `Loket`. It preserves queue identity and operational milestones without creating a Patient Journey merely because a Queue Number was issued.

### 1.2 Business value

Admission Queue Operations provides:

- independently managed queues for BPJS, general, building-specific, or other admission services;
- recognizable Queue Labels such as `A001` and `B001`;
- controlled relationships between Kiosks, Service Points, and Loket;
- accountable calling of a Queue Entry to a physical Loket;
- separation between a queue call and actual service start;
- continuity from anonymous queue intake to later Patient Journey identification; and
- reliable waiting-time and service-duration interpretation.

### 1.3 Scope

This feature owns the admission specialization of these business capabilities:

1. Admission Service Point Management.
2. Admission Queue Intake.
3. Queue Number Allocation and Labelling.
4. Loket Service Authorization.
5. Queue Call Coordination.
6. Admission Queue Exception Resolution.

It elaborates the Patient Tracker `Queue Session Aggregate` and introduces stable business identities for `Service Point`, `Loket`, and `Kiosk`.

### 1.4 Business boundaries

Patient Tracker owns Service Point identity, Queue Prefix, Queue Session, Queue Entry, Queue Number, Queue Label, Queue Call, Loket service authorization, Kiosk service offering, and queue-service milestones.

Admisi Rajal owns Registration Assistance decisions and the Outpatient Registration outcome. The Patient context owns canonical Patient identity. A guarantor or its accountable integration owns external coverage eligibility truth.

Admission Queue Operations does not define:

- how a Patient proves BPJS, general, or other service eligibility;
- the contents or outcome of Outpatient Registration;
- the physical implementation of a Kiosk, Queue Display, or Admission Module;
- screen layout, button behavior, printing technology, audio technology, or operator procedures;
- application interfaces, message delivery, persistence schema, or concurrency mechanisms; or
- the Patient's physical position before an accountable service interaction occurs.

## 2. Ubiquitous Language

| Term | Definition |
|---|---|
| Admission Queue Operations | The coordination of queue intake, calling, service start, and completion for outpatient Registration Assistance. |
| Service Point | A recognized queue category representing one admission responsibility, such as BPJS Admission or General Admission. |
| ServicePointId | The stable business identity of one Service Point. |
| Service Point Name | The human-recognizable name of a Service Point. |
| Queue Prefix | The character sequence assigned to a Service Point and used when forming a Queue Label. |
| Loket | A recognized physical service desk at which a Service Point Operator provides Registration Assistance. |
| LoketId | The stable business identity of one physical Loket. |
| Loket Service Authorization | The active permission for one Loket to serve one Service Point. |
| Kiosk | A recognized self-service queue-intake facility through which a Patient or Visitor may request an Admission Queue Entry. |
| Kiosk Service Offering | The active availability of one Service Point through one Kiosk. |
| Queue Display | An operational communication channel that presents current Queue Calls and their destination Loket. |
| Queue Session | One time-bounded queue for one Service Point on one Session Date. |
| Queue Entry | One Patient's or anonymous Visitor's participation in one Queue Session. |
| Queue Number | The numeric ordinal assigned to one Queue Entry and unique within its Queue Session. |
| Queue Label | The public-facing identifier formed from the Queue Prefix applicable at issuance and the formatted Queue Number, such as `A001`. |
| Queue Prefix Snapshot | The Queue Prefix preserved by a Queue Session or Queue Entry so an issued Queue Label retains its original meaning after later Service Point changes. |
| Queue Call | The accountable request for the holder of one Waiting Queue Entry to attend one Loket. A Queue Call is not proof that service started. |
| Call Attempt | One occurrence of calling or recalling a Queue Entry to a Loket. |
| Recall | A later Call Attempt for the same Queue Entry without allocating another Queue Number. |
| Registration Assistance | Human administrative work required to establish or resolve an Outpatient Registration. |
| No-Show | The operational conclusion that the holder of a called Queue Entry did not present for service under the applicable policy. |
| Service Point Transfer | The accountable redirection of unresolved queue demand from one Service Point to another. |
| Withdrawn | The terminal Queue Entry state used when service will not start, including an approved transfer to another Service Point. |

## 3. Business Capabilities

### 3.1 Admission Service Point Management

Patient Tracker can establish and retain stable admission Service Points with recognizable names and Queue Prefixes, and can withdraw a Service Point from future use without rewriting historical Queue Labels.

### 3.2 Admission Queue Intake

Patient Tracker can offer one or more applicable Service Points through a Kiosk and create an Anonymous Admission Queue Entry for the Service Point chosen by a Patient or Visitor.

Queue intake does not establish or select a Patient Tracker.

### 3.3 Queue Number Allocation and Labelling

Patient Tracker can allocate a numeric Queue Number within one Queue Session and combine it with the applicable Queue Prefix to produce a stable public Queue Label.

### 3.4 Loket Service Authorization

Patient Tracker can identify physical Loket and determine which Service Points each Loket may serve. A Loket may serve one or more Service Points, and one Service Point may be served by one or more Loket.

### 3.5 Queue Call Coordination

Patient Tracker can record that a Waiting Queue Entry was called or recalled to one authorized Loket while preserving the distinction between the call, the Patient's presentation, and actual service start.

### 3.6 Admission Queue Exception Resolution

Patient Tracker can preserve accountable outcomes when a Patient does not present, chooses an inapplicable Service Point, or must be redirected, without falsely recording Registration Assistance as completed.

## 4. Actors & Roles

### 4.1 Patient or Visitor

The Patient or Visitor:

- chooses an applicable offered Service Point;
- receives and retains a Queue Label;
- waits for a Queue Call; and
- presents available identity, booking, or coverage evidence when requested.

### 4.2 Admission Officer

The Admission Officer acts as the Service Point Operator and Journey Resolution Operator for Registration Assistance. The Admission Officer:

- operates from an assigned Loket;
- serves only authorized Service Points;
- calls or recalls a Queue Entry;
- recognizes when Registration Assistance actually starts and completes; and
- accountably resolves the applicable Patient Journey from available evidence.

### 4.3 Queue Operations Administrator

The Queue Operations Administrator:

- establishes and retires Service Points, Kiosks, and Loket;
- assigns Queue Prefixes;
- maintains Kiosk Service Offerings and Loket Service Authorizations; and
- prevents operational ambiguity among concurrently used Queue Labels.

### 4.4 Queue Operations Supervisor

The Queue Operations Supervisor resolves authorized exceptions such as no-show disposition, service redirection, or temporary changes in which Loket serve a Service Point.

## 5. Domain Objects

### 5.1 Service Point

Purpose: represents one recognized category of admission service demand.

Responsibilities:

- retain a stable ServicePointId;
- retain its Service Point Name;
- retain the Queue Prefix applicable to new Queue Sessions; and
- determine whether it remains available for future queue intake.

A Service Point is not a physical Loket. Examples are `ADM-BPJS` and `ADM-UMUM`.

### 5.2 Loket

Purpose: represents one recognized physical desk where Registration Assistance may be delivered.

Responsibilities:

- retain a stable LoketId;
- remain distinguishable to Patients and Admission Officers; and
- retain its active Loket Service Authorizations.

### 5.3 Kiosk

Purpose: represents one recognized self-service queue-intake facility.

Responsibilities:

- retain a stable Kiosk identity;
- retain its active Kiosk Service Offerings; and
- expose only Service Points available through that Kiosk.

### 5.4 Queue Session

Purpose: represents one admission queue for one Service Point and operating interval.

Responsibilities:

- retain the applicable Service Point and Queue Prefix Snapshot;
- allocate Queue Numbers unique within the session;
- own Queue Entries and their Call Attempts; and
- preserve queue-service milestones.

### 5.5 Queue Entry

Purpose: represents one anonymous Visitor's or identified Patient Journey's participation in an admission Queue Session.

Responsibilities:

- retain its numeric Queue Number and stable Queue Label;
- retain its anonymous or identified condition;
- retain CreatedAt, ServedAt, and DoneAt; and
- retain its Waiting, In Service, Done, or Withdrawn service state.

### 5.6 Queue Call

Purpose: represents the current calling obligation for one Waiting Queue Entry and preserves its Call Attempts.

Responsibilities:

- identify the Queue Entry and destination Loket;
- retain each call or recall occurrence;
- distinguish an outstanding call from an acknowledged or concluded call; and
- avoid representing a call as service start.

### 5.7 Queue Label

Purpose: provides the public identifier used by a Patient, Admission Officer, and Queue Display.

The Queue Label combines the Queue Prefix Snapshot and formatted Queue Number. It remains unchanged after issuance.

### 5.8 Loket Service Authorization

Purpose: represents the business permission for one Loket to serve one Service Point during its active period.

### 5.9 Kiosk Service Offering

Purpose: represents the business availability of one Service Point through one Kiosk during its active period.

## 6. Aggregates

### 6.1 Service Point Aggregate

**Aggregate Root:** `Service Point`

**Consistency boundary:**

The aggregate keeps ServicePointId, Service Point Name, Queue Prefix, and availability for future intake mutually consistent. Historical Queue Prefix Snapshots are outside this aggregate and are not rewritten when the Service Point changes.

### 6.2 Loket Aggregate

**Aggregate Root:** `Loket`

**Owned entities:**

- zero or more `Loket Service Authorization` entities.

**Consistency boundary:**

The aggregate keeps Loket identity, operational availability, and its authorized Service Points mutually consistent.

### 6.3 Kiosk Aggregate

**Aggregate Root:** `Kiosk`

**Owned entities:**

- zero or more `Kiosk Service Offering` entities.

**Consistency boundary:**

The aggregate keeps Kiosk identity, operational availability, and its offered Service Points mutually consistent.

### 6.4 Queue Session Aggregate

**Aggregate Root:** `Queue Session`

**Owned entities:**

- zero or more `Queue Entry` entities;
- zero or more `Queue Call` entities; and
- zero or more `Call Attempt` records within each Queue Call.

**Consistency boundary:**

The aggregate keeps Service Point identity, Queue Prefix Snapshot, Queue Number uniqueness, Queue Entry identity association, Queue Call disposition, destination Loket, and service milestones mutually consistent.

A Queue Entry references a Patient Tracker by TrackerId after identification but does not own or modify the Patient Tracker Aggregate.

## 7. Business Rules

### 7.1 Service Point, Loket, and Kiosk identity

- **BR-AQO-001** — Every Service Point shall have one stable ServicePointId, one Service Point Name, and one Queue Prefix.
- **BR-AQO-002** — ServicePointId shall identify a queue category and shall not identify a physical Loket.
- **BR-AQO-003** — Every Loket shall have one stable LoketId and shall represent exactly one physical service desk.
- **BR-AQO-004** — A Loket may serve multiple Service Points, and a Service Point may be served by multiple Loket, only through active Loket Service Authorizations.
- **BR-AQO-005** — A Kiosk may offer one or more Service Points only through active Kiosk Service Offerings.
- **BR-AQO-006** — A Service Point, Loket, or Kiosk that has historical queue activity shall be retired from future use rather than erased from business history.

### 7.2 Queue Number and Queue Label

- **BR-AQO-007** — Every admission Queue Session shall identify exactly one Service Point and retain the Queue Prefix applicable when that session was established.
- **BR-AQO-008** — Every Queue Entry shall receive exactly one numeric Queue Number unique within its Queue Session.
- **BR-AQO-009** — Every Queue Entry shall have one Queue Label formed from its Queue Prefix Snapshot and formatted Queue Number.
- **BR-AQO-010** — A Queue Label shall remain unchanged after issuance even when the Service Point Name or Queue Prefix changes later.
- **BR-AQO-011** — Queue Prefixes shall not create ambiguous active Queue Labels among Service Points announced to the same Patient audience.
- **BR-AQO-012** — Reissuing or re-presenting evidence of the same Queue Entry shall not allocate another Queue Number.

### 7.3 Anonymous intake and Patient Journey association

- **BR-AQO-013** — Obtaining an admission Queue Number shall create an Anonymous Queue Entry when no Patient Journey has been accountably resolved and shall not establish or select a Patient Tracker.
- **BR-AQO-014** — A Walk-In Queue Entry shall remain Anonymous until Registration establishes a new Patient Tracker or accountable Journey Resolution selects an applicable existing Patient Tracker.
- **BR-AQO-015** — A Booking Patient's admission Queue Entry shall not be associated with a Patient Tracker from Booking evidence alone; an Admission Officer shall resolve the applicable existing Booking Patient Tracker from Patient-supplied evidence.
- **BR-AQO-016** — Association of an Anonymous Queue Entry with a Patient Tracker shall be singular, accountable, and irreversible within that Queue Entry.

### 7.4 Queue calling and service

- **BR-AQO-017** — Only a Waiting Queue Entry may have an outstanding Queue Call.
- **BR-AQO-018** — Every Queue Call shall identify exactly one destination Loket that is actively authorized for the Queue Entry's Service Point.
- **BR-AQO-019** — One Queue Entry shall not have more than one outstanding Queue Call at the same time.
- **BR-AQO-020** — One Loket shall not have more than one outstanding or In Service Queue Entry unless an explicitly approved operating policy permits parallel service.
- **BR-AQO-021** — A Recall shall create another Call Attempt for the same Queue Entry and shall not allocate another Queue Number.
- **BR-AQO-022** — A Queue Call or Call Attempt shall not by itself establish ServedAt or prove that Registration Assistance started.
- **BR-AQO-023** — A Waiting Queue Entry may enter In Service only when an Admission Officer recognizes that Registration Assistance has started at the destination Loket.
- **BR-AQO-024** — An In Service Queue Entry may become Done only when Registration Assistance has reached an accountable outcome.
- **BR-AQO-025** — Registration outcome remains authoritative in Admisi Rajal; Queue Entry completion shall not independently establish an Outpatient Registration.

### 7.5 Exceptions and historical truth

- **BR-AQO-026** — A No-Show conclusion shall preserve all prior Call Attempts and shall not be represented as completed Registration Assistance; the applicable policy shall determine whether the Queue Entry remains Waiting or becomes Withdrawn.
- **BR-AQO-027** — Redirecting unresolved demand to another Service Point shall make the original Waiting Queue Entry Withdrawn, preserve the relationship between the original and replacement queue participation, and shall not silently relabel the original Queue Entry.
- **BR-AQO-028** — A Queue Display shall present recorded Queue Call truth and shall not own Queue Entry selection or lifecycle decisions.
- **BR-AQO-029** — Queue waiting and service time shall be derived from Queue Entry milestones and shall not be inferred from a Queue Call alone.
- **BR-AQO-030** — Admission Queue Operations shall not claim Patient physical presence before an accountable service interaction recognizes it.

## 8. State Machines & Lifecycles

### 8.1 Service Point, Loket, and Kiosk availability lifecycle

```text
Active
  → Retired
```

Retired means unavailable for new operational use. Historical relationships and Queue Labels remain valid.

### 8.2 Queue Entry service lifecycle

```text
Waiting
  → In Service
      → Done

Waiting
  → Withdrawn
```

The Queue Entry lifecycle extends the parent Patient Tracker lifecycle with `Withdrawn` for queue participation that ends before service starts. A Queue Call occurs while the Queue Entry is Waiting and does not add a second service state.

### 8.3 Queue Call lifecycle

```text
Outstanding
  → Acknowledged

Outstanding
  → No-Show

Outstanding
  → Withdrawn
```

`Acknowledged` means the call obligation ended because service was recognized as starting. `No-Show` means the applicable no-show policy concluded without service start. `Withdrawn` means the call was ended without asserting service or no-show. Recall adds another Call Attempt while the Queue Call remains Outstanding.

### 8.4 Queue Entry identification lifecycle

```text
Anonymous Queue Entry
  → Identified Queue Entry
```

Queue issuance and Queue Calling do not cause this transition. It occurs only through accountable association with an existing Patient Tracker or a new Patient Tracker established by an owning source activity.

## 9. Domain Events

| Domain Event | Business meaning |
|---|---|
| Admission Service Point Established | A new category of admission queue service became recognized. |
| Admission Service Point Retired | A Service Point ceased accepting future queue intake while retaining its history. |
| Loket Authorized for Service Point | A physical Loket became permitted to serve one Service Point. |
| Kiosk Service Point Offered | A Service Point became available for Patient selection through one Kiosk. |
| Admission Queue Session Established | An admission queue was established for one Service Point and operating interval. |
| Admission Queue Entry Created | An Anonymous or Identified Queue Entry received a Queue Number and Queue Label. |
| Admission Queue Entry Called | A Waiting Queue Entry was called to one authorized Loket. |
| Admission Queue Entry Recalled | Another Call Attempt was made for the same Queue Entry and Loket. |
| Admission Queue Call Acknowledged | The outstanding call ended because Registration Assistance was recognized as starting. |
| Admission Queue Entry Marked No-Show | The outstanding call concluded without the Patient or Visitor presenting under the applicable policy. |
| Admission Queue Entry Identified | An Anonymous Admission Queue Entry was associated with one Patient Tracker. |
| Admission Queue Service Started | Registration Assistance for a Waiting Queue Entry started at one Loket. |
| Admission Queue Service Completed | Registration Assistance for an In Service Queue Entry reached an accountable outcome. |
| Admission Queue Entry Withdrawn | A Waiting Queue Entry ended without service start. |
| Admission Queue Entry Transferred | Unresolved queue demand was redirected to another Service Point with preserved history. |

These events are stable business facts. Display, audio, integration, and delivery behavior belong to architecture artifacts.

## 10. Business Workflows

### 10.1 Obtain an admission Queue Number

```text
Patient or Visitor chooses an offered Service Point
  → Applicable Queue Session recognized
  → Anonymous Queue Entry created
  → Queue Number allocated
  → Queue Label issued
  → Patient or Visitor waits
```

No Patient Tracker is created or selected by this workflow.

### 10.2 Call and begin Registration Assistance

```text
Admission Officer operates from an authorized Loket
  → Admission Officer chooses an authorized Service Point
  → Waiting Queue Entry selected
  → Queue Entry called to the Loket
  → Patient or Visitor presents at the Loket
  → Queue Call acknowledged
  → Registration Assistance starts
  → Queue Entry enters In Service
```

Calling and service start are separate business facts.

### 10.3 Resolve a Booking Patient Journey

```text
Anonymous admission Queue Entry is In Service
  → Admission Officer asks for Patient-supplied evidence
  → Applicable existing Booking Patient Tracker resolved
  → Queue Entry associated with that Tracker
  → Registration Assistance continues
  → Registration outcome determined by Admisi Rajal
  → Queue service completed
```

### 10.4 Resolve a Walk-In Patient Journey

```text
Anonymous admission Queue Entry is In Service
  → Admission Officer asks for Patient-supplied evidence
  → Applicable existing Patient Tracker selected when one exists
     or Registration establishes a new Patient Tracker
  → Existing Queue Entry associated with that Tracker
  → Registration outcome determined by Admisi Rajal
  → Queue service completed
```

### 10.5 Recall or conclude a No-Show

```text
Queue Call remains Outstanding
  → Admission Officer recalls the same Queue Entry
     or applicable No-Show policy is satisfied
  → Another Call Attempt retained when recalled
     or Queue Call concludes as No-Show
```

No Recall allocates another Queue Number. No-Show does not mean Registration Assistance completed.

### 10.6 Redirect to another Service Point

```text
Chosen Service Point found inapplicable
  → Accountable redirection approved
  → Original Queue Entry becomes Withdrawn and is retained as history
  → Replacement Queue Entry established for applicable Service Point
  → Relationship between both queue participations preserved
```

The original Queue Label is not silently changed into a label belonging to another Service Point.
