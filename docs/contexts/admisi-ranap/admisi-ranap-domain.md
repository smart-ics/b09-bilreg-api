# Rawat Inap Admission Domain

---

## 1. Business Overview

Rawat Inap Admission is the business capability responsible for administratively admitting patients into inpatient care following a clinical decision requiring hospitalization.

The domain ensures every inpatient admission is administratively prepared and handed over to the appropriate Ward for patient accommodation.

Its responsibilities include:

- receiving clinical hospitalization requests,
- planning inpatient admissions,
- performing administrative admission,
- managing admissions awaiting accommodation.

The business boundary ends when an Admission has either:

- been handed over to the Waiting List for accommodation, or
- is ready for direct accommodation by the responsible Ward.

Room allocation and Bed allocation are outside this business domain and belong to the Ward domain.

---

## 2. Ubiquitous Language

| Term | Definition |
|------|------------|
| Patient | Person receiving inpatient care. |
| Doctor | Physician responsible for deciding that a patient requires hospitalization. |
| Admisi | Administrative unit responsible for inpatient admission. |
| Ward | Inpatient unit responsible for accommodating and caring for admitted patients. |
| Opname Request | Clinical decision requesting inpatient treatment. |
| Reservation | Administrative plan for a future inpatient admission. |
| Admission | Administrative acceptance of a patient into an inpatient episode. |
| Waiting List | Represents an Admission awaiting suitable accommodation before the next Bed Assignment. |
| Kelas Rawat | Accommodation class (kelas perawatan) determined by insurance entitlement or patient preference; persisted as `KelasReff`. |
| Bangsal | Destination inpatient ward responsible for accommodating the patient; persisted as `BangsalReff`. |

---

## 3. Business Capabilities

### Clinical Admission Request

Manage clinical requests requiring inpatient admission.

---

### Admission Planning

Manage planned inpatient admissions before administrative admission.

---

### Administrative Admission

Administratively admit patients into inpatient care.

---

### Waiting List Management

Manage Admissions that are waiting to be accommodated by the appropriate Ward.

---

## 4. Actors & Roles

### Doctor

Responsible for clinical hospitalization decisions.

Responsibilities:

- Create Opname Request
- Cancel Opname Request

---

### Admisi

Responsible for administrative admission.

Responsibilities:

- Create Reservation
- Maintain Reservation
- Process Admission
- Update Admission
- Cancel Admission
- Manage Waiting List

---

### Ward

Responsible for accepting Waiting List entries and accommodating patients.

The Ward owns patient accommodation, including Room and Bed assignment.

---

### Patient / Family

Provide administrative information and confirm admission plans.

---

## 5. Domain Objects

### Opname Request

Represents a clinical decision requiring inpatient treatment.

---

### Reservation

Represents a planned inpatient admission before administrative admission occurs.

---

### Admission

Represents one administrative inpatient episode.

Admission is the central business object throughout the admission lifecycle.

---

### Waiting List

Represents an Admission awaiting accommodation by the responsible Ward.

Waiting List serves as the business hand-over between the Admission domain and the Ward domain.

---

## 6. Aggregates

### Opname Request

**Aggregate Root**

Business Responsibility:

Maintain the lifecycle of clinical hospitalization requests.

---

### Reservation

**Aggregate Root**

Business Responsibility:

Maintain planned inpatient admissions before administrative admission.

Reservation owns Kelas Rawat and destination Bangsal for planning.

---

### Admission

**Aggregate Root**

Business Responsibility:

Maintain the administrative lifecycle of an inpatient admission.

Admission owns:

- Admission information
- Administrative status
- Reservation realization
- Opname Request fulfillment
- Kelas Rawat and destination Bangsal (planning attributes; not room/bed allocation)

Admission does **not** own patient accommodation.

---

### Waiting List

**Aggregate Root**

Business Responsibility:

Maintain Admissions awaiting accommodation.

Waiting List owns:

- Waiting status
- Waiting priority
- Kelas Rawat (`KelasReff`)
- Destination Bangsal (`BangsalReff`)

Waiting List is independent from Ward operations.

---

## 7. Business Rules

### BR-RI-001

Only a Doctor may create an Opname Request.

---

### BR-RI-002

An Opname Request represents a clinical decision and does not constitute an Admission.

---

### BR-RI-003

Reservation is optional before Admission.

---

### BR-RI-004

An Admission represents one inpatient episode.

---

### BR-RI-005

One Opname Request may fulfill at most one Admission.

---

### BR-RI-006

One Reservation may realize at most one Admission.

---

### BR-RI-007

Only an admitted patient may enter the Waiting List.

---

### BR-RI-008

An Admission may have at most one active Waiting List.

---

### BR-RI-009

Waiting List does not change the Admission status.

---

### BR-RI-010

Waiting List represents an administrative hand-over to the Ward.

---

### BR-RI-011

Admission never allocates Rooms or Beds.

---

### BR-RI-012

Room and Bed allocation belong exclusively to the Ward domain.

---

## 8. State Machines & Lifecycles

### Opname Request

Requested

↓

Fulfilled

or

↓

Cancelled

---

### Reservation

Reserved

↓

Maintained

↓

Realized

or

↓

Cancelled

---

### Admission

Admitted

↓

Updated

↓

Waiting

↓

Completed

or

↓

Cancelled

---

### Waiting List

Waiting

↓

Accepted

↓

Closed

---

## 9. Domain Events

- Opname Requested
- Opname Request Cancelled
- Reservation Created
- Reservation Updated
- Reservation Realized
- Admission Processed
- Admission Updated
- Admission Cancelled
- Waiting List Created
- Waiting List Updated
- Waiting List Closed

---

## 10. Business Workflows

### Direct Admission

Opname Request

↓

Admission

↓

Waiting List (if accommodation unavailable)

or

↓

Ward Accommodation

---

### Planned Admission

Opname Request

↓

Reservation

↓

Admission

↓

Waiting List (if accommodation unavailable)

or

↓

Ward Accommodation

---

### Elective Admission

Reservation

↓

Admission

↓

Waiting List (if accommodation unavailable)

or

↓

Ward Accommodation

---

### Patient Transfer

Ward Release

↓

Waiting List

↓

Ward Accommodation