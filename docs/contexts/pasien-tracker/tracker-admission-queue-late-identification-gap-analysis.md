# Patient Tracker — Admission Queue Late-Identification Gap Analysis

**Artifact status:** Codebase investigation and remediation design  
**Date:** 2026-07-22  
**Implementation status:** Closed in source on 2026-07-22  
**Authoritative business definition:** `docs/contexts/pasien-tracker/TRACKER-DOMAIN.md`  
**Related context:** `docs/contexts/admisi-rajal/admisi-rajal-domain.md`

> **Close-out:** Walk-In Registration now supports both anonymous and already-identified `InService` entries; anonymous association uses an atomic compare-and-set update; admission start, selection, and completion validate the server-configured `AdmisiRajal:AdmissionServicePoint`. Concurrent association failure maps to HTTP 409. No schema migration was required.

## 1. Executive conclusion

The implementation is **not conformant** with the revised admission-queue identity rules:

- obtaining an Admisi Queue Number correctly creates no Tracker;
- selecting an existing journey correctly associates an existing Tracker, but it also couples association with queue service start;
- selecting “new journey” incorrectly creates a Patient Tracker from Queue Evidence before Registration;
- Walk-In Registration with an existing anonymous admission Queue Entry is rejected because the handler requires that entry to have been identified beforehand.

The gap is primarily in the Application layer. The existing Queue Session aggregate and SQL shape can already represent the required intermediate state:

```text
Anonymous Queue Entry + InService + PasienTrackerId "-"
```

No database migration is required for that state. The required change is to separate three operations that are currently coupled:

1. start service for an anonymous admission Queue Entry;
2. associate an applicable existing Patient Tracker when evidence supports it; and
3. for a new Walk-In journey, create the Tracker from authoritative Registration evidence and attach the existing Queue Entry inside the Registration transaction.

**Overall severity:** High. The current path can establish a durable Tracker without Registration, contrary to `BR-TRK-025` and `BR-TRK-031a`, and the conforming Walk-In path cannot complete through the current registration command.

## 2. Revised domain requirements assessed

The investigation used these rules from `docs/contexts/pasien-tracker/TRACKER-DOMAIN.md`:

| Rule | Required behavior |
|---|---|
| `BR-TRK-025` | Queue Number allocation alone shall not establish a Patient Tracker; a new Tracker comes from authoritative evidence of the owning source activity. |
| `BR-TRK-029` | Queue Number allocation may create an Anonymous Queue Entry without creating a Tracker. |
| `BR-TRK-030` | A physician Queue Entry directly reserved by Booking is identified; a separate admission Queue Entry issued after failed Self-Registration may remain anonymous. |
| `BR-TRK-031` | Anonymous-to-identified association requires accountable selection of an existing Tracker or creation by an owning source activity. |
| `BR-TRK-031a` | A Walk-In admission entry remains anonymous until Registration establishes a new Tracker or an applicable existing Tracker is selected. |
| `BR-TRK-031b` | Booking QR alone does not automatically identify the admission entry; the officer resolves the applicable Booking Tracker from Patient-supplied evidence. |
| `BR-TRK-035`–`039` | Queue service lifecycle remains Waiting → InService → Done and is independent from Tracker ownership. |

The corresponding Admisi Rajal contract requires the Booking-assistance flow to attach the applicable existing Booking journey only after accountable evidence resolution and forbids a duplicate Patient Journey (`BR-ARJ-008`).

## 3. Investigation scope

The investigation traced:

- anonymous queue issuance;
- Journey Candidate listing;
- existing-Tracker selection;
- new-Tracker resolution;
- Queue Entry identity and lifecycle behavior;
- Walk-In and Booking registration orchestration;
- queue/tracker persistence mapping;
- HTTP routes; and
- focused unit tests.

Primary implementation paths:

- `src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/QueAnonymousIntakeCmd.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/TrkJourneyResolveSelectCmd.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/TrkJourneyResolveNewCmd.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/AdmissionQueueIdentify.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/AdmissionQueueComplete.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs`
- `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs`
- `src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianEntryModel.cs`
- `src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/PasienTrackerModel.cs`
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianEntryDto.cs`
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianEntryDal.cs`
- `src/bilreg/Bilreg.SqlDb/AdmisiContext/AntrianFeature/BILRG_AntrianEntry.sql`

This was a source investigation. No staging or production database was queried.

## 4. Current implementation behavior

### 4.1 Anonymous queue issuance is conformant

`QueAnonymousIntakeHandler`:

1. resolves or creates a Service Point Queue Session;
2. calls `AntrianModel.AddEntry(occurredAt)`;
3. persists only the queue; and
4. returns `AntrianId`, `NoUrut`, and `CreatedAt`.

The added entry uses the sentinel Tracker key `"-"`. No `PasienTrackerModel` is created and no Tracker Event is appended. This is conformant for both Walk-In intake and Booking Self-Registration requiring assistance.

### 4.2 Existing-Tracker selection is partly conformant

`TrkJourneyResolveSelectHandler` loads an existing Tracker and anonymous Waiting entry. In one transaction, `AdmissionQueueIdentify.IdentifyAndRecordEvidence`:

- assigns the selected Tracker;
- starts queue service;
- appends `Check In` at the entry's original `CreatedAt`; and
- appends `Reg-Start` at the current time.

This correctly reuses an existing Tracker and preserves evidence timestamps. It can support:

- Booking assistance after the officer verifies evidence and selects the applicable Booking Tracker; and
- Walk-In assistance when an applicable existing journey is found.

However, it couples **identity association** and **service start**. This coupling prevents an anonymous entry from entering InService before the identity decision and makes the new Walk-In path depend on premature Tracker creation.

### 4.3 New-Tracker resolution is nonconformant

`TrkJourneyResolveNewHandler` currently:

1. accepts a Person snapshot, Visit Date, Queue Session identity, Queue Number, and User ID;
2. creates `PasienTrackerModel` using `Check In` queue evidence as its first evidence;
3. assigns the new Tracker to the Queue Entry;
4. moves the entry to InService;
5. appends `Reg-Start`; and
6. saves queue and Tracker before any Registration exists.

This directly violates the revised meaning of `BR-TRK-025` and `BR-TRK-031a`. Queue evidence proves that a visitor is waiting or being served; it is not the authoritative source activity that establishes a new Walk-In Patient Journey.

The endpoint is publicly exposed to authorized clients as:

```text
POST api/PasienTracker/resolve/new
```

The response exposes the prematurely created `PasienTrackerId`, making the stale behavior part of the HTTP contract.

### 4.4 Walk-In Registration rejects the required intermediate state

`RegJalanWalkInCommand` accepts `AdmissionAntrianId` and `AdmissionNoUrut`, but `ResolveTrackerForWalkIn` requires that entry to already contain a real Tracker ID. If it is still anonymous, it throws:

```text
Admission queue entry '{AntrianId}' / {NoUrut} is not identified.
```

Therefore the revised Walk-In flow cannot pass:

```text
Anonymous admission entry
  → service started
  → successful Registration creates Tracker
  → existing entry attached and completed
```

The only working options today are:

- call the nonconformant `resolve/new` first; or
- omit the admission queue keys, causing Registration to create a Tracker and synthesize a new admission Queue Entry that is immediately Served and Done.

The second option loses the real kiosk queue identity and waiting/service milestones.

### 4.5 Booking Registration is compatible after existing-Tracker selection

`RegJalanByBookingHandler` loads the Tracker from the Booking-created physician Queue Entry. When admission queue keys are supplied, `AdmissionQueueComplete` requires the admission entry to be:

- identified;
- associated with that same Booking Tracker; and
- InService.

This is compatible with the revised domain after the Admission Officer has selected the applicable Booking Tracker through accountable evidence resolution. It usefully prevents the admission entry from being completed against a different journey.

The Booking handler does not need to auto-associate the admission entry from QR evidence and must continue not doing so.

## 5. Persistence feasibility

### 5.1 Required state is already representable

`BILRG_AntrianEntry` stores Queue Entry identity and lifecycle independently:

| Column | Anonymous InService value |
|---|---|
| `PasienTrackerId` | `"-"` under the existing sentinel convention |
| `AntrianStatus` | `InService` integer value |
| `CreatedAt` | kiosk issuance time |
| `ServedAt` | officer service-start time |
| `DoneAt` | `3000-01-01` sentinel until completion |

There is no database constraint requiring a real `PasienTrackerId` for InService. `AntrianEntryDto`, `AntrianEntryDal.Update`, and `AntrianRepo.AreEqual` already persist status, milestones, Person name, and Tracker ID independently.

### 5.2 Domain model already permits anonymous service start

`AntrianEntryModel.Serve(DateTime)` validates only:

- current state is Waiting;
- the supplied time is a valid business time; and
- `ServedAt >= CreatedAt`.

It does not require an identified Tracker. This is suitable for the revised workflow and should be preserved.

### 5.3 No migration is required for the core fix

The core remediation needs application commands and orchestration changes only. Changing sentinel `"-"` to SQL `NULL` remains a separate deferred persistence decision and should not be bundled into this correction.

## 6. Detailed findings

| ID | Severity | Finding | Consequence |
|---|---|---|---|
| LID-01 | High | `TrkJourneyResolveNewHandler` creates and persists a Tracker from queue evidence before Registration | Durable journey identity exists without authoritative Registration evidence |
| LID-02 | High | `ResolveTrackerForWalkIn` rejects an anonymous admission entry | The canonical Walk-In late-identification flow cannot complete |
| LID-03 | High | `AdmissionQueueIdentify` couples AssignPasien, Serve, and evidence append | Queue service cannot start independently while identity remains unresolved |
| LID-04 | Medium | `POST resolve/new` and its response expose the stale behavior as an API contract | Clients can continue creating premature Trackers unless the endpoint is removed or changed |
| LID-05 | Medium | `AdmissionQueueComplete` only completes an already identified InService entry | It lacks an atomic Registration-owned attach-new-Tracker-and-complete path |
| LID-06 | Medium | `PasienTrackerModel.Create(RegModel)` adds `REGISTER` immediately | Reusing it unchanged before adding earlier queue evidence can give the wrong event insertion order and may not establish the desired first-evidence period semantics |
| LID-07 | Medium | Tests assert that `resolve/new` creates and persists a Tracker | The suite is green while protecting behavior that is now domain-invalid |
| LID-08 | Low | `UserId` is validated in resolve commands but not stored in queue/Tracker operational evidence or a dedicated audit record | Accountability is only partially observable; this is pre-existing and not a blocker for the core fix |

## 7. Recommended target workflow

### 7.1 Start admission service without resolving identity

Add an application use case such as:

```text
AdmissionQueueStartCmd(AntrianId, NoUrut, UserId)
```

Behavior:

```text
Load Queue Session
  → require Queue Entry is Waiting
  → call entry.Serve(occurredAt)
  → save Queue Session
  → do not create/load/save a Patient Tracker
  → return AntrianId, NoUrut, InService, ServedAt
```

This use case represents the Admission Officer calling the number and beginning assisted service. It works identically for Booking and Walk-In tickets.

### 7.2 Associate an applicable existing Tracker

Refactor `TrkJourneyResolveSelectCmd` so it associates the selected Tracker with an **anonymous InService** admission entry and appends:

- `Check In` with `OccurredAt = CreatedAt`; and
- `Reg-Start` with `OccurredAt = ServedAt`.

It should no longer call `Serve`. The application order becomes explicit:

```text
AdmissionQueueStartCmd
  → candidate search / evidence review
  → TrkJourneyResolveSelectCmd when an existing Tracker applies
```

The select operation must still save the Queue Session and existing Tracker atomically and reject an already identified entry or a mismatched/non-InService entry.

### 7.3 Do not create a new Tracker during journey resolution

Remove or retire `TrkJourneyResolveNewCmd` as an admission identity command. Possible compatibility handling:

- preferred: remove `POST api/PasienTracker/resolve/new` after coordinated client migration;
- transitional: return an explicit outcome such as `NoApplicableJourney` without creating a Tracker, but do not retain the misleading `PasienTrackerId` response;
- do not replace it with a temporary Tracker or an Admisi-owned identity record.

The anonymous Queue Entry itself is sufficient durable evidence that assistance is active.

### 7.4 Create and attach a new Walk-In Tracker during Registration

Extend `RegJalanWalkInCommand` orchestration for supplied admission queue keys:

```text
Load anonymous InService admission Queue Entry
  → establish Outpatient Registration
  → create Patient Tracker from Registration-owned identity and evidence
  → append queue evidence using original CreatedAt and ServedAt
  → assign the new Tracker to the existing Queue Entry
  → mark the entry Done at registration completion
  → append REGISTER evidence
  → save Registration + Tracker + Queue Session in one TransHelper scope
```

Recommended event insertion order for a new Walk-In Tracker:

1. `Check In` — Queue Evidence Reference, `OccurredAt = entry.CreatedAt`;
2. `Reg-Start` — Queue Evidence Reference, `OccurredAt = entry.ServedAt`;
3. `REGISTER` — `RegId`, `OccurredAt = registration completion time`.

Although Registration is the owning activity authorizing Tracker creation, inserting the preserved operational evidence chronologically avoids relying on later backdated inserts and keeps `StartPeriod` aligned with the earliest evidence.

This likely warrants an intention-revealing factory/orchestration method rather than calling `PasienTrackerModel.Create(RegModel)` and then appending older events.

### 7.5 Preserve existing paths

- Walk-In without prior kiosk intake may continue using create-on-registration compatibility behavior, but it should be explicitly classified as a legacy/no-intake path.
- Booking-created physician Queue Entries remain identified from Booking creation and are unaffected.
- Booking admission tickets remain anonymous until the officer selects the applicable existing Booking Tracker.
- `IQueueNumberCompatibilityAdapter` remains physician-queue-only and must not participate in admission ticket allocation or association.

## 8. Suggested code change map

| Area | Recommended change |
|---|---|
| `AdmissionQueueIdentify.cs` | Split service start, existing-Tracker association, and evidence append into intention-revealing operations; allow association to an anonymous InService entry |
| `TrkJourneyResolveSelectCmd.cs` | Associate existing Tracker only; derive Reg-Start time from persisted `ServedAt` |
| `TrkJourneyResolveNewCmd.cs` | Remove/retire admission Tracker creation behavior |
| New admission-start command | Start anonymous queue service without touching Tracker |
| `RegJalanWalkInCommand.cs` | Accept anonymous InService admission entry; create Tracker from Registration; attach, append evidence, and complete atomically |
| `AdmissionQueueComplete.cs` | Add an explicit attach-new-Tracker-and-complete path or move that orchestration into the registration handler |
| `PasienTrackerModel.cs` | Add a creation method suited to Registration-authorized journey creation with ordered admission evidence, if domain behavior belongs in the aggregate |
| `PasienTrackerController.cs` | Expose admission start; remove or transition `resolve/new` |
| Tests | Replace premature-creation assertions with absence-of-Tracker and atomic registration-association scenarios |

## 9. Transaction and consistency requirements

### 9.1 Queue service start

Queue service start changes only the Queue Session aggregate. It can be committed independently because the entry is allowed to remain anonymous while InService.

### 9.2 Existing Tracker selection

Assigning an existing Tracker and appending queue evidence must remain one transaction:

```text
Queue Entry association + existing Tracker evidence
```

Failure must leave the entry anonymous and the Tracker unchanged.

### 9.3 New Walk-In Registration

These writes must share one transaction:

```text
Outpatient Registration
+ Patient Tracker header/events
+ admission Queue Entry association/completion
+ physician queue reservation/projection
+ existing registration financial/outbound writes
```

If Registration fails, no new Tracker is persisted and the admission entry remains anonymous InService for accountable retry or exception handling.

### 9.4 Concurrency guards

The current repositories have no optimistic concurrency token. The implementation should at minimum reload and validate that the entry is still anonymous and InService inside the transaction before association. Concurrent select/registration attempts must result in one winner and an explicit conflict, not silent reassignment.

## 10. API compatibility impact

| Current API | Target disposition |
|---|---|
| `POST api/Antrian/anonymous-intake` | Keep; already conformant |
| `GET api/PasienTracker/candidates` | Keep; supports evidence review |
| `POST api/PasienTracker/resolve/select` | Keep with changed precondition: anonymous InService entry; no implicit Serve |
| `POST api/PasienTracker/resolve/new` | Retire or change to a no-creation outcome after client coordination |
| New admission-start endpoint | Add for call/start without Tracker |
| `POST api/Reg/rajalWalkIn` | Extend to atomically create/attach Tracker when the supplied admission entry is anonymous InService |
| `POST api/Reg/rajalByBooking` | Keep existing same-Tracker completion guard |

This is a behaviorally breaking API change for clients that call `resolve/new`. Deployment requires identifying and coordinating those clients. No in-repository frontend caller was found under the inspected `src/bilreg` API/test scope; external UI usage remains Unable to Verify.

## 11. Required verification

### 11.1 Domain tests

1. An anonymous Waiting entry can enter InService without a Tracker.
2. Starting service does not create or save a Tracker.
3. An anonymous InService entry can be assigned exactly one real Tracker.
4. Reassignment is rejected.
5. Queue timestamps remain unchanged during association.

### 11.2 Use-case tests

1. Anonymous intake persists only the queue.
2. Admission start persists InService with sentinel Tracker ID.
3. Existing selection attaches the selected Tracker and appends Check In / Reg-Start using persisted times.
4. No-applicable-candidate decision creates no Tracker.
5. Walk-In Registration with anonymous InService entry creates one Tracker, attaches that entry, appends three ordered evidence items, and marks it Done in one transaction.
6. Failed Walk-In Registration persists neither Tracker nor association/completion.
7. Booking selection attaches the applicable Booking Tracker; Booking Registration rejects a different Tracker.
8. Concurrent association attempts cannot overwrite the first association.

### 11.3 Persistence/integration tests

1. Round-trip `PasienTrackerId = "-"`, `AntrianStatus = InService`, and valid `ServedAt`.
2. Round-trip later association to a real Tracker without changing `CreatedAt`/`ServedAt`.
3. Transaction rollback covers Registration, Tracker events, admission entry, physician queue, billing, and outbox writes.

## 12. Baseline verification performed

Command:

```text
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore --filter "FullyQualifiedName~TrkJourneyResolveHandlerTest|FullyQualifiedName~QueAnonymousIntakeHandlerTest|FullyQualifiedName~AdmissionQueueCompleteTest|FullyQualifiedName~AntrianEntryAssignPasienTest"
```

Result:

```text
Passed: 11
Failed: 0
Skipped: 0
```

The green result confirms that current mechanics are internally consistent. It does **not** establish domain conformance because `TrkJourneyResolveHandlerTest.New_WhenValid_ThenCreatesTrackerIdentifiesAndAppendsEvidence` explicitly protects the now-invalid eager Tracker creation behavior.

Build emitted pre-existing nullable-reference and XML-documentation warnings outside this gap's implementation paths.

## 13. Recommended implementation sequence

1. Add tests for anonymous InService persistence and no-Tracker service start.
2. Add `AdmissionQueueStartCmd` and API.
3. Split `AdmissionQueueIdentify` so selection associates an already-InService entry without starting it.
4. Change existing-Tracker selection tests and API contract documentation.
5. Add the Walk-In Registration branch that creates the Tracker, attaches the existing anonymous entry, appends ordered evidence, and completes it atomically.
6. Add rollback and concurrency tests.
7. Deprecate/remove `resolve/new` after external-client inventory and migration.
8. Update `docs/contexts/pasien-tracker/tracker-f04-implementation-report.md`, `docs/contexts/pasien-tracker/tracker-f05-implementation-report.md`, and `docs/contexts/pasien-tracker/tracker-codebase-gap-report.md` so they no longer describe F-05 eager creation as closed canonical behavior.

## 14. Final assessment

The gap is feasible to close without changing the Queue Session persistence schema. The domain model already permits anonymous service start, and the repository already persists identity and lifecycle independently.

The main risk is transactional refactoring inside the large Walk-In registration handler and the breaking retirement of `resolve/new`. The implementation should avoid creating a temporary Tracker, avoid adding a second admission identity ledger, and preserve the original Queue Entry and timestamps throughout registration. The authoritative outcome is:

```text
Queue Number alone
  = Queue Entry only

Existing journey proven
  = attach selected existing Tracker

New Walk-In Registration established
  = create Tracker + attach existing Queue Entry atomically
```
