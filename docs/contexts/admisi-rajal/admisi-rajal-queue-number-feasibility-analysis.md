# Admisi Rajal Queue Number — Codebase Feasibility Analysis

**Status:** Feasibility assessment (no implementation)  
**Date:** 2026-07-22  
**Decision:** **High feasibility, incremental implementation**  
**Canonical domains:** [Admisi Rajal](admisi-rajal-domain.md) and [Patient Tracker](../pasien-tracker/TRACKER-DOMAIN.md)

## 1. Requested outcome

At the hospital kiosk:

1. A booked Patient scans the Booking QR.
2. If Self-Registration succeeds, an authoritative Outpatient Registration is established and the Patient proceeds to the physician waiting area. No Admisi Rajal Queue Number is issued.
3. If Self-Registration requires assistance, Patient Tracker issues an Admisi Rajal Queue Number and the Patient waits for assisted registration.
4. A Walk-In Patient without a Booking obtains an Admisi Rajal Queue Number and waits for assisted registration.

The requested number is an **Admisi Rajal Service Point Queue Number**, not the physician Queue Number already reserved by a Booking.

## 2. Domain alignment

The requested flow is already the canonical flow described by Admisi Rajal:

- successful Self-Registration must not create Registration Assistance (`BR-ARJ-006`);
- a failed attempt is `Self-Registration Requires Assistance`, not a successful Registration (`BR-ARJ-007`);
- Booking assistance must ultimately retain the Booking's applicable Patient Journey after accountable resolution (`BR-ARJ-008`);
- Walk-In assistance may start anonymously (`BR-ARJ-009`);
- the Work List derives from an active Patient Tracker Queue Entry (`BR-ARJ-010` and `BR-ARJ-011`);
- Admisi Rajal must not allocate numbers or own queue state (`BR-ARJ-012`);
- duplicate assistance for the same unresolved Booking attempt is forbidden (`BR-ARJ-015`);
- a booked Patient proceeds to the physician waiting area only after Registration is established (`BR-ARJ-043`).

Patient Tracker supplies the required mechanics:

- anonymous Queue Entries are allowed (`BR-TRK-029`);
- admission Queue Number allocation does not create or automatically select a Tracker (`BR-TRK-029`–`BR-TRK-031b`);
- anonymous entries can later be associated through Journey Resolution (`BR-TRK-031`);
- Queue Numbers are unique within a Queue Session (`BR-TRK-027`);
- Waiting, In Service, and Done are Queue Entry lifecycle states (`BR-TRK-035`–`BR-TRK-039`).

Therefore the use case must be an **application orchestration across Admisi Rajal and Patient Tracker**, while number generation remains behavior of the Patient Tracker Queue Session aggregate.

## 3. Existing capabilities

| Required capability | Current evidence | Assessment |
|---|---|---|
| Find Booking by scanned QR | `BookingSearchQuery` matches `ExtAppReff.CheckInQr`; `GET api/Booking/search/{date}/{keyword}` exposes it | Present, but it is a generic search API rather than a kiosk decision use case |
| Establish Registration from Booking | `RegJalanByBookingCmd` creates the Registration and reuses the Booking physician queue entry | Present |
| Establish Walk-In Registration after assistance | `RegJalanWalkInCommand` supports admission queue identity fields | Present |
| Create an anonymous Admisi queue entry | `QueAnonymousIntakeCmd` resolves/creates a Service Point Queue Session and calls `AntrianModel.AddEntry` | Present |
| Return an Admisi queue number | `QueAnonymousIntakeResponse` returns `AntrianId`, `NoUrut`, and `CreatedAt` | Present foundation |
| Identify an anonymous entry | `AdmissionQueueIdentify` assigns a Tracker, starts service, and records Check-In / Reg-Start evidence | Present through Journey Resolution flows |
| Complete admission queue service at Registration | `AdmissionQueueComplete` marks the queue entry Done and appends REGISTER evidence | Present and wired into both Rajal registration handlers |
| Queue persistence and uniqueness guard | `AntrianModel` owns allocation and rejects duplicate numbers; `BILRG_Antrian` / `BILRG_AntrianEntry` persist it | Present in source |
| Explicit Admisi Service Point | Generic `ServicePointType` exists; default code `ADM` / name `Loket Admisi` is used during completion | Partly present; no authoritative catalogue/configuration |
| Admisi Rajal Work List | Domain defines it as a projection from active Tracker entries | Not found as a dedicated Rajal query/API |

## 4. Main gaps

### 4.1 No single kiosk decision use case

The codebase has separate Booking search, anonymous intake, Journey Resolution, and Registration commands. It does not have one use case that accepts the kiosk attempt and returns one explicit outcome:

```text
RegistrationEstablished | AssistanceQueueIssued | AlreadyQueued | Rejected
```

Without that boundary, kiosk clients must reproduce domain decisions and can accidentally issue an admission number after successful Registration.

### 4.2 Booking failure starts anonymously and needs later accountable association

`QueAnonymousIntakeCmd` deliberately creates an entry with the sentinel Tracker ID. That is correct for both Walk-In intake and Booking Self-Registration that requires assistance: getting the Admisi Queue Number does not create or select a Tracker. When the number is called, the Admission Officer asks for evidence and accountably associates the entry. For Booking, the officer selects the applicable existing Booking Tracker; for a Walk-In without an applicable Tracker, Registration establishes a new Tracker and the existing queue entry is attached to it.

### 4.3 Duplicate Booking assistance is not prevented

The anonymous intake command always adds a new entry. No persisted assistance-attempt identity or query currently enforces `BR-ARJ-015`. A repeated QR scan, kiosk retry, or network retry could print multiple active numbers.

A minimal solution should use an idempotency identity tied to the unresolved Booking attempt and, inside the transaction, return the existing active Admisi Queue Entry when present. Do not create a second Admisi-owned assistance ledger merely to enforce this rule.

### 4.4 Kiosk authorization and trust boundary are missing

`AntrianController` and `BookingController` are controller-level `[Authorize]`. The kiosk needs a deliberately scoped machine/device credential or a narrowly anonymous endpoint with compensating controls. Reusing broad staff authorization or exposing general Booking search would reveal more data than the kiosk needs.

The response should contain only the fields needed for routing/printing. Raw QR values should not be logged.

### 4.5 Service Point master/configuration is absent

`QueAnonymousIntakeCmd` trusts caller-provided `ServicePointCode` and `ServicePointName`; `IServicePointDal` has no implementation. A public kiosk must not select an arbitrary Service Point. Resolve the configured Admisi Rajal Service Point server-side (facility-aware if required).

### 4.6 Work List projection is absent

Queue truth exists, but no dedicated Admisi Rajal Work List query was found that filters active (`Waiting` / `InService`) entries for the Admisi Service Point and enriches them with Booking/registration context. This is needed for the assisted-registration desk and must remain a projection, not another queue table.

### 4.7 Session creation and concurrent intake need hardening

Anonymous intake performs list-then-create and then allocates a number. Source has uniqueness for `SequenceTag`, but concurrent first requests may race and require retry/reload handling. Number allocation also relies on the configured sequencer and should be proven atomic under kiosk concurrency. The natural Service Point/date/time uniqueness policy remains deferred in Tracker documentation.

### 4.8 Successful Self-Registration is not a kiosk-specific contract

`RegJalanByBookingCmd` can establish Registration, but its request requires administrative details and is a staff-oriented API. A kiosk-specific Self-Registration use case must define what evidence is already available from Booking, which facts the Patient may confirm, and which missing/invalid facts result in Assistance. Expected business failures should be returned explicitly rather than collapsed into system exceptions.

## 5. Recommended application shape

Keep the orchestration in Admisi Rajal Application and reuse Patient Tracker ports/models.

### 5.1 Kiosk attempt

Suggested command name:

```text
AdmisiRajalKioskAttemptCmd
```

Suggested input:

```text
IntakePath: ByBooking | ByWalkIn
BookingQr: required only for ByBooking
KioskId / FacilityId: trusted request context
IdempotencyKey: required per kiosk attempt
```

Suggested result union/discriminator:

```text
RegistrationEstablished(RegId, PhysicianQueueNumber, Destination)
AssistanceQueueIssued(AntrianId, QueueNumber, ServicePoint, CreatedAt)
AlreadyQueued(AntrianId, QueueNumber, ServicePoint, CreatedAt)
Rejected(Code, SafeMessage)
```

`RegistrationEstablished` must not contain an Admisi queue number. `AssistanceQueueIssued` must not imply Registration success.

### 5.2 Booking path

```text
Validate QR and Visit Date
  -> resolve Booking and existing Tracker
  -> attempt Self-Registration
      -> success: persist Registration; return RegistrationEstablished
      -> expected assistance reason:
           resolve configured Admisi Queue Session
           find active assistance for this Booking attempt
             -> found: return AlreadyQueued
             -> absent: add Anonymous Queue Entry without creating or selecting a Tracker
           persist atomically; return AssistanceQueueIssued
```

The physician Booking Queue Number remains unchanged and separate from the new Admisi Queue Number.

### 5.3 Walk-In path

```text
Resolve configured Admisi Queue Session
  -> add anonymous Queue Entry
  -> persist atomically
  -> return AssistanceQueueIssued
```

Later, the Admission Officer uses the existing Journey Resolution / identification flow, starts service, establishes Registration, and the registration handler completes the same Admisi Queue Entry.

## 6. Implementation slices

| Slice | Deliverable | Risk |
|---|---|---|
| 0 — policy decisions | Define Self-Registration success/assistance reasons, Service Point configuration, kiosk authentication, printable number format, and retry window | Domain/product decision |
| 1 — queue issuance | Add a server-resolved Admisi queue issuance service supporting anonymous admission intake for both Walk-In and Booking-assistance paths | Low–medium |
| 2 — Booking idempotency | Query/reuse an active Admisi entry for the same Booking attempt; add persistence support/index if required | Medium |
| 3 — kiosk orchestration/API | Add the narrow kiosk command and outcome contract; keep Booking lookup details private | Medium |
| 4 — Work List | Add a projection of active Admisi Queue Entries enriched with Booking/registration context | Medium |
| 5 — hardening | Concurrency tests, retry behavior, authorization tests, audit/metrics, deployment verification | Medium |

Do not route this number through `IQueueNumberCompatibilityAdapter`: that adapter exists for legacy **physician** number allocation. The Admisi Service Point number should come directly from `AntrianModel.AddEntry` / the Queue Session's sequencer.

## 7. Minimum verification matrix

1. Valid Booking + successful Self-Registration creates Registration and no Admisi Queue Entry.
2. Valid Booking + expected assistance creates one anonymous Admisi Queue Entry and does not create or automatically select a Tracker.
3. Repeated scan/retry for the same unresolved Booking returns the same active entry and number.
4. Invalid, expired, cancelled, already-registered, and ambiguous Booking QR return distinct safe outcomes.
5. Walk-In creates an anonymous Waiting entry.
6. For Booking, Admission Officer evidence resolution attaches the applicable existing Booking Tracker; for Walk-In without an applicable journey, successful Registration creates the Tracker and attaches the existing entry.
7. Successful assisted Registration marks that same Admisi entry Done; failed Registration does not falsely establish Registration.
8. Physician Queue Number and Admisi Queue Number remain independent.
9. Concurrent first requests create one session and unique queue numbers without partial writes.
10. Kiosk credentials cannot call staff Booking search or unrelated endpoints; sensitive QR values are absent from logs.

## 8. Feasibility conclusion

**High feasibility.** The codebase already contains the central Patient Tracker mechanics and downstream registration integration. No new queue aggregate or independent Admisi Rajal queue ledger is warranted.

The viable change is an incremental orchestration layer around existing capabilities. The critical design work is to keep queue issuance independent from Tracker creation/selection, associate the applicable Booking Tracker only after accountable evidence resolution, create a Walk-In Tracker only from Registration evidence, make Booking assistance idempotent, resolve the Service Point server-side, expose a kiosk-safe contract, and add the derived Admisi Rajal Work List. With those constraints, the use case respects both canonical domain documents and fits the existing Clean Architecture structure.
