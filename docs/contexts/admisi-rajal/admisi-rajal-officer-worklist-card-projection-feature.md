# Admisi Rajal Officer Worklist Card Projection

## 1. Document purpose

This document is the canonical implementation context for the Admisi Rajal Officer Worklist Card
Projection feature.

It defines:

- the flattened card projection consumed by the officer worklist;
- field-specific source authority and fallback rules;
- server-side sorting and filtering semantics;
- paging, null, and deterministic-ordering rules;
- architecture boundaries between Patient Tracker queue truth and Admisi read composition;
- API compatibility requirements;
- implementation slices and acceptance criteria.

This document is optimized for AI coding agents. Treat statements marked **MUST**, **MUST NOT**,
**SHOULD**, and **MAY** as normative requirements.

The Bahasa Indonesia programmer companion is:
[`docs/contexts/admisi-rajal/admisi-rajal-officer-worklist-card-projection-feature-id.md`](admisi-rajal-officer-worklist-card-projection-feature-id.md).

## 2. Problem statement

The officer worklist currently returns three nested sources:

- queue data;
- optional Booking data;
- optional Registration data.

The frontend independently chooses which nested value to display. This has three problems:

1. field precedence is inconsistent;
2. a value displayed by the card can differ from the value used for sorting or filtering;
3. server paging occurs before Booking and Registration enrichment, so sorting or filtering
   enriched fields on the client only affects the loaded page.

The feature introduces a server-owned, flattened `card` projection. The same effective values MUST
drive:

- card display;
- server-side filtering;
- server-side sorting;
- filter option labels where applicable.

## 3. Scope

### 3.1 In scope

- Additive officer-worklist response projection for:
  - Queue Number;
  - Patient Name;
  - Pasien ID;
  - Booking ID;
  - Registration ID;
  - Service Point;
  - Source;
  - Arrival Time;
  - Layanan;
  - Dokter;
  - Jam Praktek.
- Explicit source authority and fallback rules.
- Server-side sort and filter contracts for the resolved fields.
- Correct sorting, filtering, counting, and paging over the full candidate set.
- Backward-compatible retention of existing `queue`, `identity`, `booking`, and `registration`
  sections.
- An explicit and durable intake-source projection.
- Null-safe card presentation.
- Unit, contract, persistence/integration, and frontend tests.

### 3.2 Out of scope

- Changing Queue lifecycle or claim behavior.
- Changing Registration or Booking aggregates.
- Replacing the Patient Tracker queue-only operational projection.
- Using Booking physician-queue numbers as Admission Queue numbers.
- Deriving a new clinical workflow state from nullable fields.
- Defining the completed/registered-history tab. The card projection MUST remain reusable by that
  future view.
- Making every field visible in every card layout.
- Creating a second mutable queue ledger.

## 4. Current implementation constraints

### 4.1 Queue truth is intentionally queue-owned

`AdmissionQueueOperationalProjection` returns queue-owned fields and applies the operational
default order:

```text
Priority DESC
CreatedAt ASC
NoUrut ASC
AntrianId ASC
```

This boundary MUST remain queue-only. Do not add Booking, Registration, patient, doctor, or
eligibility ownership to `AdmissionQueueWorklistItem`.

### 4.2 Enrichment currently occurs after paging

The current flow is:

```text
queue WHERE/ORDER/OFFSET/FETCH
    -> page of queue rows
    -> per-item Tracker/assistance/Booking/Registration composition
    -> API response
```

This means a client-side or post-page sort/filter is incomplete for enriched fields.

The target flow is:

```text
candidate queue rows
    -> resolve source references and effective card values
    -> apply effective filters
    -> apply effective stable sort
    -> count
    -> page
    -> API response
```

### 4.3 Existing frontend inference is not authoritative

The current frontend infers Source with:

```ts
item.booking ? 'Booking' : 'Walk-In'
```

This MUST NOT remain the authoritative rule. Missing enrichment does not prove Walk-In origin.

## 5. Ubiquitous language

| Term | Definition |
|---|---|
| Queue Entry | Patient Tracker-owned Admission Queue entry identified by `AntrianId + NoUrut`. |
| Raw Source | One of Queue, Booking, or Registration data used during composition. |
| Effective Value | The single resolved value exposed in the flattened card projection. |
| Card Projection | Admisi-owned read model containing effective values for display, sort, and filter. |
| Intake Source | Immutable operational origin of the queue entry: Booking, Walk-In, or Unknown. |
| Service Point | Admission Queue service lane selected at intake and snapshotted by the Queue. |
| Payer Class | Payment/guarantor classification such as UMUM or BPJS. It is not automatically equivalent to Service Point. |
| Queue Order | Priority-aware FIFO operational ordering. |

## 6. Core invariants

1. Queue identity MUST remain `AntrianId + NoUrut`.
2. `queueNumber` MUST always represent the Admission Queue number.
3. Booking `NoAntrian` MUST NOT override the Admission Queue number.
4. `pasienId`, `bookingId`, and `regId` MUST remain distinct identifier types and MUST NOT be used
   as fallbacks for one another.
5. Queue `CreatedAt` MUST remain the Arrival Time.
6. Queue Service Point MUST remain authoritative for the queue lane.
7. Registration values override Booking values only for fields where both values have the same
   business meaning.
8. Missing, blank, whitespace-only, sentinel, and invalid-reference values MUST be normalized to
   `null` before fallback.
9. Display, filter, and sort MUST use the same effective-value resolver.
10. Filtering and sorting MUST occur before paging.
11. Every sort MUST end with deterministic queue-identity tie-breakers.
12. Null effective values MUST remain valid worklist records.
13. Failure to enrich one optional source MUST NOT remove a Queue Entry from the worklist.
14. The API change MUST be additive until all consumers explicitly migrate.
15. Default operational sorting MUST continue to honor priority.

## 7. Effective field authority matrix

The general phrase “Registration, then Booking, then Queue” is not applied blindly. Authority is
defined separately for every field.

| Effective field | Authority/fallback | Required notes |
|---|---|---|
| `queueNumber` | Queue `QueueLabel`; fallback Queue `NoUrut` | Booking `NoAntrian` is a different sequence and is forbidden as fallback. |
| `patientName` | Registration `PasienName` -> Booking `PersonName` -> Queue/tracker `PersonName` | Normalize blank and sentinel values first. |
| `pasienId` | Registration `PasienId` -> Booking `PasienId` -> resolved Tracker patient reference when available -> `null` | A `PasienTrackerId` is not a `PasienId` and MUST NOT be returned as one. |
| `bookingId` | Queue-linked Booking/Booking Assistance evidence -> Booking `BookingId` -> `null` | Preserve the Booking identity even when detailed Booking enrichment is unavailable. |
| `regId` | Registration outcome/Queue Registration reference -> Registration `RegId` -> `null` | Preserve the Registration identity even when detailed Registration enrichment is unavailable. |
| `servicePoint` | Queue `ServicePointId + ServicePointName` only | This is the intake lane, not automatically the payer class. |
| `source` | Durable intake evidence -> `Unknown` | Do not derive only from the presence of an enriched Booking object. |
| `arrivalTime` | Queue `CreatedAt` only | Use business time consistently. |
| `layanan` | Registration `Layanan` -> Booking `Layanan` -> `null` | Resolve ID and name as one atomic reference. |
| `dokter` | Registration `Dokter` -> Booking `Dokter` -> `null` | Resolve ID and name as one atomic reference. |
| `jamPraktek` | Booking `JamPraktek` -> `null` | Current Registration has no equivalent authoritative property. |

### 7.1 Reference atomicity

For `layanan`, `dokter`, and `servicePoint`, an ID and name form one reference. The resolver MUST
NOT combine an ID from one source with a name from another source.

Example:

```text
Registration LayananId is valid, Registration LayananName is valid
=> use the complete Registration Layanan reference.
```

If a reference is invalid, normalize the entire reference to `null` and continue to the next
allowed source.

### 7.2 Registration precedence

Registration precedence represents the final/effective registered visit context. It applies to:

- Patient Name;
- Pasien ID;
- Layanan;
- Dokter.

It does not apply to:

- Admission Queue number;
- Queue Arrival Time;
- Queue Service Point;
- Intake Source;
- Booking ID;
- Registration ID;
- Jam Praktek when Registration has no equivalent field.

### 7.4 Identifier semantics

The three projected identifiers have different meanings:

| Identifier | Meaning |
|---|---|
| `pasienId` | Hospital patient/master-record identity. |
| `bookingId` | Booking aggregate identity associated with the Queue Entry. |
| `regId` | Registration aggregate identity associated with the Queue Entry. |

They MUST NOT substitute for one another.

An identifier MAY be known even when its detailed object cannot be enriched. For example, a durable
Registration outcome can provide `regId` while the Registration detail read temporarily fails. In
that case:

```text
card.regId = known Registration identity
registration = null
```

The same rule applies to a durable Queue-to-Booking link and `bookingId`.

### 7.3 Service Point versus payer class

The implementation MUST confirm the product meaning of “UMUM/BPJS.”

If UMUM and BPJS are configured Admission Service Point lanes:

```text
card.servicePoint <- Queue service point
```

If UMUM and BPJS mean guarantor/payment category, add a separate future field:

```ts
type PayerClass = 'UMUM' | 'BPJS' | 'OTHER' | 'UNKNOWN'
```

Suggested payer resolution:

```text
Registration TipeJaminan -> Booking coverage/Asuransi -> Unknown
```

The implementation MUST NOT merge payer semantics into `servicePoint`.

## 8. Source resolution contract

### 8.1 Values

```text
Booking
WalkIn
Unknown
```

Use `WalkIn` in API/code contracts and “Walk-in” in UI labels.

### 8.2 Required behavior

Source describes how the Queue Entry entered the admission workflow. It MUST NOT change because a
Registration later exists.

The resolver SHOULD use durable evidence, for example:

- explicit persisted intake-source data;
- a durable Queue Entry reference such as Booking-assistance evidence;
- a Booking-assistance record linked by `AntrianId + NoUrut`;
- other approved immutable intake evidence.

An explicit persisted source discriminator is preferred if legacy evidence is ambiguous.

### 8.3 Forbidden behavior

The following is forbidden as the final source contract:

```text
Booking object exists => Booking
Booking object absent => WalkIn
```

An enrichment failure or missing historical Booking link MUST resolve to `Unknown`, not WalkIn.

## 9. Proposed API contract

### 9.1 Additive response shape

Keep the existing raw sections and add `card`:

```json
{
  "queue": {},
  "identity": {},
  "booking": {},
  "registration": {},
  "card": {
    "queueNumber": {
      "label": "A0042",
      "sequence": 42
    },
    "patientName": "SITI AMINAH",
    "pasienId": "P000123",
    "bookingId": "BKG-01JXYZ",
    "regId": "REG-20260730-001",
    "servicePoint": {
      "id": "ADM-BPJS",
      "name": "BPJS"
    },
    "source": "Booking",
    "arrivalTime": "2026-07-30T08:14:30",
    "layanan": {
      "id": "LYN-001",
      "name": "POLI PENYAKIT DALAM"
    },
    "dokter": {
      "id": "DR-001",
      "name": "dr. Budi"
    },
    "jamPraktek": "08:00"
  }
}
```

### 9.2 Suggested backend types

```csharp
public sealed record AdmisiRajalOfficerCardReference(
    string Id,
    string Name);

public sealed record AdmisiRajalOfficerQueueNumber(
    string Label,
    int Sequence);

public enum AdmisiRajalOfficerIntakeSource
{
    Unknown = 0,
    Booking = 1,
    WalkIn = 2
}

public sealed record AdmisiRajalOfficerWorklistCard(
    AdmisiRajalOfficerQueueNumber QueueNumber,
    string? PatientName,
    string? PasienId,
    string? BookingId,
    string? RegId,
    AdmisiRajalOfficerCardReference ServicePoint,
    AdmisiRajalOfficerIntakeSource Source,
    DateTime ArrivalTime,
    AdmisiRajalOfficerCardReference? Layanan,
    AdmisiRajalOfficerCardReference? Dokter,
    TimeOnly? JamPraktek);
```

The actual names MAY be adjusted to repository conventions, but field semantics MUST remain
unchanged.

### 9.3 Time serialization

- `arrivalTime` MUST use the established API business-time convention.
- `jamPraktek` MUST serialize as `HH:mm`.
- Do not synthesize `jamPraktek` from Arrival Time.

## 10. Sort contract

### 10.1 Sort keys

```text
queueOrder
queueNumber
patientName
pasienId
bookingId
regId
servicePoint
source
arrivalTime
layanan
dokter
jamPraktek
```

### 10.2 Direction

```text
asc
desc
```

The API MUST parse sort keys and directions through an enum or explicit allow-list. Arbitrary SQL
column interpolation is forbidden.

### 10.3 Default sort

The default is the synthetic operational key `queueOrder`:

```text
Priority DESC
ArrivalTime ASC
QueueNumber.Sequence ASC
AntrianId ASC
```

`queueOrder` is allowed to remain hidden from the card. It preserves priority-aware FIFO.

### 10.4 Stable secondary ordering

Every non-default sort MUST append stable operational tie-breakers:

```text
selected effective field and direction
Priority DESC
ArrivalTime ASC
QueueNumber.Sequence ASC
AntrianId ASC
```

Where the Queue Number label can cross Service Points, include the stable Service Point ID or
Queue Label as needed before `AntrianId`.

### 10.5 Null ordering

- Non-null values MUST sort before null values.
- Null values MUST remain last for both ascending and descending user directions.
- Text values MUST be compared using a documented case-insensitive, trimmed strategy.
- Duplicate display names MUST be resolved with stable IDs and Queue identity tie-breakers.

## 11. Filter contract

### 11.1 Supported filters

| Filter | Suggested behavior |
|---|---|
| `queueNumber` | Exact or prefix match against effective Queue Number label; numeric exact match against sequence where supported. |
| `patientName` | Case-insensitive contains match. |
| `pasienId` | Exact match by default; optional prefix search for an explicit identifier-search control. |
| `bookingId` | Exact match by default; optional prefix search for an explicit identifier-search control. |
| `regId` | Exact match by default; optional prefix search for an explicit identifier-search control. |
| `servicePointId` | Exact or multi-select stable ID match. |
| `source` | Multi-select `Booking`, `WalkIn`, `Unknown`. |
| `arrivalFrom`, `arrivalTo` | Inclusive business-time range with documented bounds. |
| `layananId` | Exact or multi-select stable ID match. |
| `dokterId` | Exact or multi-select stable ID match. |
| `jamPraktekFrom`, `jamPraktekTo` | Inclusive time range. |

### 11.2 Effective-value filtering

Filters MUST target resolved effective fields.

Example:

```text
Registration Layanan = Cardiology
Booking Layanan = Internal Medicine
filter Layanan = Cardiology
=> item matches
```

The raw Booking value MUST NOT cause the item to match Internal Medicine after Registration has
become authoritative.

### 11.3 Unknown values

Where operationally useful, nullable filters SHOULD support an explicit `Unknown` option. A null
enrichment value MUST NOT silently remove the Queue Entry unless the user applies a filter that
excludes null.

### 11.4 Count and paging

`totalCount`, `hasMore`, and `nextOffset` MUST describe the filtered candidate set, not the
unfiltered queue or a client-filtered page.

## 12. Paging and mutation safety

### 12.1 Processing order

The required logical order is:

```text
scope by Business Date and operational status
resolve effective projection
apply effective filters
count filtered rows
apply stable effective sort
apply offset/limit
return page
```

### 12.2 Stable frontend identity

The frontend MUST continue to key and select items with:

```text
AntrianId + NoUrut
```

It MUST NOT use array index, Patient ID, Registration ID, or Queue Label as the sole identity.

### 12.3 Polling movement

Registration and Booking enrichment can appear while the worklist is polling. A card MAY move when
the selected sort field changes after refresh. The frontend MUST preserve selection by Queue
identity.

## 13. Target architecture

### 13.1 Required boundary

Keep:

```text
IAdmissionQueueOperationalProjection
    => queue-owned operational truth only
```

Introduce or evolve an Admisi-owned composed read projection:

```text
IAdmisiRajalOfficerWorklistProjection
    => Queue + Registration + Booking effective card read model
```

The exact interface name MAY vary, but the composed responsibility MUST live in Admisi Rajal, not
inside the Patient Tracker queue-owned contract.

### 13.2 Preferred query strategy

Preferred:

- one dedicated SQL read query/projection;
- set-based joins or set-based reference resolution;
- effective values computed before `WHERE`, `ORDER BY`, and paging;
- matching count query;
- query-plan verification at representative volume.

Acceptable when proven by performance tests:

- load candidate Queue identities;
- batch-load Registration, Booking, Tracker, and assistance data;
- resolve in memory;
- filter/sort/page in memory only when the candidate set is explicitly bounded and operationally
  safe.

Forbidden:

- per-row repository loading across the complete unpaged candidate set;
- frontend-only sorting/filtering of a partial page;
- filtering enriched fields after `OFFSET/FETCH`;
- duplicating mutable Queue state in a second ledger.

### 13.3 Consistency

The read projection is operationally refreshed and may be eventually consistent across sources.
Whenever practical, use a single SQL statement/read snapshot so Queue, Registration, and Booking
references are composed consistently for one response.

Enrichment failure policy:

- keep the Queue Entry;
- return null effective fields where needed;
- log safe diagnostic context;
- do not expose sensitive patient data in logs.

## 14. Database and performance requirements

1. Preserve the existing index path for default Queue Order.
2. Review execution plans for each supported sort/filter combination.
3. Avoid `%contains%` patient searches over unbounded historical scope.
4. Scope the officer view by Business Date before expensive enrichment.
5. Add indexes only from measured query-plan evidence.
6. Ensure the count query uses exactly the same effective filters as the page query.
7. Avoid N+1 Tracker, Booking, Registration, or assistance calls.
8. Verify performance with at least representative active-day volume and the maximum supported
   page limit.
9. Preserve the configured worklist polling interval; do not compensate for slow queries by
   silently increasing frontend polling.

## 15. Frontend requirements

### 15.1 Single projection consumer

`DenseWorklistTile.vue` and all future worklist representations SHOULD consume the server-resolved
`card` object for these eleven fields.

The frontend MUST NOT implement a second precedence resolver for production behavior.

### 15.2 Visibility

Fields may be omitted visually based on layout and available width. Missing values MUST not reserve
misleading blank labels.

Recommended density:

- always visible: Queue Number, Patient Name;
- compact metadata: Service Point, Source, Arrival Time;
- contextual line when available: Layanan, Dokter, Jam Praktek.
- identifiers when operationally useful: Pasien ID, Booking ID, and Registration ID; these MAY be
  hidden in compact layouts and exposed in list/detail/search contexts.

This is a presentation recommendation, not an API omission rule.

### 15.3 Sorting/filter controls

- Controls MUST send server query parameters.
- Changing sort or filters MUST reset paging to the first page.
- The virtual list SHOULD scroll to the first row after an explicit sort/filter change.
- Loading and empty states SHOULD distinguish “no queue entries” from “no entries match filters.”
- Active filter state MUST remain visible to the officer.

### 15.4 Backward compatibility

During migration:

- parse `card` as additive;
- retain existing nested fields;
- optionally use a temporary guarded fallback only for compatibility;
- remove duplicate frontend precedence logic after backend rollout is complete.

## 16. Validation and normalization rules

Treat the following as missing:

- `null`;
- empty string;
- whitespace-only string;
- known sentinel values such as `"-"` where the source contract uses them;
- invalid IDs paired with placeholder names;
- invalid or sentinel dates.

Do not treat valid zero-like values as missing without a field-specific rule.

Normalize names by trimming for comparison. Preserve the approved display casing returned by the
authoritative source.

## 17. Test requirements

### 17.1 Resolver unit tests

At minimum:

1. Registration Patient Name overrides Booking and Queue.
2. Booking Patient Name overrides Queue when Registration is absent.
3. Queue Patient Name is used when both enriched sources are absent.
4. Registration Layanan overrides Booking.
5. Registration Dokter overrides Booking.
6. Booking Jam Praktek is returned when Booking exists.
7. Jam Praktek is null when Booking is absent.
8. Queue Number never uses Booking `NoAntrian`.
9. Queue Service Point is not overwritten by payer data.
10. Blank/sentinel Registration values fall back correctly.
11. Invalid compound references fall back atomically.
12. Missing Booking enrichment does not automatically produce WalkIn.
13. Registration Pasien ID overrides Booking Pasien ID.
14. Booking Pasien ID is used when Registration Pasien ID is absent.
15. Pasien Tracker ID is never exposed as Pasien ID.
16. Booking ID remains available when Booking detail enrichment fails but durable linkage exists.
17. Registration ID remains available when Registration detail enrichment fails but durable
    linkage exists.
18. Booking ID and Registration ID never substitute for one another.

### 17.2 Source-resolution tests

- Booking assistance resolves to Booking.
- Anonymous intake resolves to WalkIn.
- Ambiguous legacy evidence resolves to Unknown.
- Source does not change after Registration is established.
- Source survives completed/registered-history projection.

### 17.3 Sort tests

- Default Queue Order preserves priority-aware FIFO.
- Every allowed key supports ascending and descending.
- Nulls remain last in both directions.
- Duplicate names produce stable ordering.
- Paging the same unchanged dataset repeatedly returns stable boundaries.
- Adding Registration enrichment changes the effective field and expected sort position.

### 17.4 Filter and paging tests

- Filters use Registration-over-Booking effective values.
- Pasien ID, Booking ID, and Registration ID filters target their own effective identifier fields.
- Null values remain when no excluding filter is selected.
- Unknown Source can be filtered explicitly.
- `totalCount` matches effective filters.
- `hasMore` and `nextOffset` match the filtered set.
- A match outside the original first Queue page can appear on the first filtered page.

### 17.5 API contract tests

- Legacy raw sections remain present and compatible.
- `card` is returned with nullable optional fields.
- sort/filter query parameters reject unknown keys and invalid values.
- default requests preserve operational ordering.
- page metadata remains correct.

### 17.6 Frontend tests

- Card renders server-effective fields.
- Identifier fields render or remain hidden according to layout without being semantically
  transformed.
- Missing optional fields are omitted safely.
- sort/filter changes reset paging and virtual scroll.
- selected Queue Entry remains selected after reorder.
- frontend does not reinterpret Source from `booking != null`.
- compact and card layouts remain accessible.

### 17.7 Performance tests

- representative active-day volume;
- maximum supported page size;
- polling-compatible response time;
- no per-row repository call growth;
- query-plan evidence for the default and highest-cost supported filters.

## 18. Suggested implementation slices

### Slice 0 — Contract and evidence

- Confirm Service Point versus payer-class semantics.
- Inventory durable Booking/Walk-In source evidence.
- Approve field authority matrix.
- Add resolver tests before changing runtime behavior.

### Slice 1 — Backend card projection

- Add card contract types.
- Implement normalization and field resolver.
- Return additive `card` data.
- Preserve existing raw response sections.

### Slice 2 — Set-based composed query

- Move effective resolution before filtering/paging.
- Eliminate N+1 enrichment for the page/candidate set.
- Preserve queue-only projection boundary.
- Add count and paging integration tests.

### Slice 3 — Server sorting

- Add allow-listed sort parameters.
- Preserve default Queue Order.
- Implement null and deterministic tie-breakers.
- Verify query plans.

### Slice 4 — Server filtering

- Add effective-value filters.
- Reset paging when filters change.
- Validate total-count parity.

### Slice 5 — Frontend consumption

- Add `card` schema/types.
- Render effective values.
- Add sort/filter controls.
- remove or retire duplicated frontend precedence inference.

### Slice 6 — Registered-history reuse and hardening

- Reuse the card projection for completed successful Registration entries.
- Keep `QueueStatus`, `DoneAt`, and outcome metadata available outside the eleven card fields;
  `RegId` is already part of the card projection.
- Load/performance test and document rollout evidence.

## 19. Acceptance criteria

The feature is complete when:

1. Every worklist item exposes the additive flattened `card` projection.
2. Effective fields follow the authority matrix.
3. Pasien ID, Booking ID, and Registration ID remain distinct and follow their identifier-specific
   authority rules.
4. Known Booking and Registration identities survive temporary detail-enrichment failure.
5. Source is explicit and durable; missing Booking enrichment is not treated as WalkIn.
6. Display, filter, and sort use the same effective values.
7. All supported sorting and filtering occurs before paging.
8. Default ordering remains priority-aware FIFO.
9. `totalCount`, `hasMore`, and `nextOffset` describe the filtered set.
10. Existing Queue, Booking, Registration, and identity response sections remain compatible.
11. The Patient Tracker operational projection remains queue-only.
12. No N+1 enrichment is introduced over the complete candidate set.
13. Selection remains stable by `AntrianId + NoUrut`.
14. Resolver, API, integration, frontend, and performance tests pass.

## 20. Non-goals and forbidden shortcuts

Do not:

- implement global sorting only in `VirtualWorklistGrid.vue`;
- filter only the currently loaded frontend page;
- use Booking `NoAntrian` as the Admission Queue number;
- use Pasien ID, Booking ID, Registration ID, or Pasien Tracker ID as substitutes for one another;
- infer WalkIn solely from a null Booking object;
- use Registration presence as the intake source;
- treat Service Point and payer class as interchangeable without an approved product decision;
- discard Queue Entries because enrichment is missing;
- move Registration/Booking ownership into the Patient Tracker queue-only projection;
- introduce a second mutable Queue worklist table as a shortcut;
- expose arbitrary SQL sort columns through request parameters.

## 21. Implementation references

Backend:

- `Bilreg.Application/AdmisiContext/RegFeature/AdmisiRajalOfficerWorklistQuery.cs`
- `Bilreg.Application/AdmisiContext/AntrianFeature/IAdmissionQueueOperationalProjection.cs`
- `Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AdmissionQueueOperationalProjection.cs`
- `Bilreg.Infrastructure/AdmisiContext/AntrianFeature/BookingAssistanceRepo.cs`
- `Bilreg.Infrastructure/AdmisiContext/RegFeature/RegistrationOutcomeOperationRepo.cs`

Frontend:

- `c012_myhospital_web/src/modules/Admisi/types/admissionQueue.ts`
- `c012_myhospital_web/src/modules/Admisi/queries/AdmissionQueueService.ts`
- `c012_myhospital_web/src/modules/Admisi/composables/useOfficerAdmissionQueue.ts`
- `c012_myhospital_web/src/modules/Admisi/composables/admissionQueueWorklistPresentation.ts`
- `c012_myhospital_web/src/modules/Admisi/components/admissionQueue/DenseWorklistTile.vue`
- `c012_myhospital_web/src/modules/Admisi/components/admissionQueue/VirtualWorklistGrid.vue`
- `c012_myhospital_web/src/modules/Admisi/components/admissionQueue/OfficerAdmissionQueueWorkspace.vue`
