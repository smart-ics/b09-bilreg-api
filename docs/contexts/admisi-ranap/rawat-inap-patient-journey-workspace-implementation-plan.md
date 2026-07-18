# Rawat Inap Patient Journey Workspace Implementation Plan

## 1. Outcome and architectural decision

The Rawat Inap console will use one **journey projection** as its user-facing work unit. It
will no longer expose Opname Request, Reservation, Admission/Registration, or Waiting List records
as independent primary results.

The target design is a read-side composition over the existing write models:

- Before registration, each Opname Request and each Reservation is an independent prospective
  journey.
- After registration, `RegId` is the stable episode join key across Admission, legacy Registration,
  and every Waiting List record for the episode.
- The route-level `JourneyId` is a deterministic, opaque read-model identifier derived from the
  original source when the data is valid. This keeps a journey URL stable when a prospective
  journey becomes a registered episode.
- No new write aggregate is introduced. Existing Admisi commands retain their current ownership.
- Worklist, counters, search results, and the detail workspace come from journey-oriented backend
  APIs. The frontend does not deduplicate raw records or assemble a journey from unrelated detail
  calls.

Delivery is split into two releases:

- **Release 1 — Journey through accommodation handover:** ships the consolidated journey using only
  Opname Request, Reservation, Admission/Registration, and Waiting List. It does not wait for Ward
  Management to exist.
- **Release 2 — Ward Management integration:** is a future extension after an authoritative Ward
  placement domain exists. It can add room/bed placement, occupancy, release, and transfer facts by
  `RegId` without changing Release 1 journey identity.

The design reference at
`C:\Users\drury\OneDrive\Desktop\rawat-inap-patient-journey-workspace.html` determines the
information hierarchy and interaction model, but not domain relationships or write ownership.

## 2. Constraints preserved by this plan

1. A patient can have multiple journeys. Patient name, `PasienId`, and medical-record number are
   search attributes, never join keys.
2. Reservation remains independent from Opname Request. No `OpnameRequestId` is added to
   Reservation.
3. Waiting List represents an accommodation handover. Release 1 can show its recorded state but
   cannot prove placement, occupancy, release, or ward-to-ward transfer.
4. Ward/Bed Management does not currently exist. Release 1 must neither invent it nor wait for it.
5. Room and bed assignment remain future Ward responsibilities. Admisi must not perform or simulate
   bed assignment.
6. Existing write aggregates and commands are retained. The new model is a query projection and
   action-policy view.
7. Domain identifiers are hidden from the normal workspace and shown only in **Detail Sistem &
   Audit**.

## 3. Evidence from the current implementation

### 3.1 Frontend evidence

| Concern                 | Concrete code path                                                                                         | Current behavior and implication                                                                                                                                                            |
| ----------------------- | ---------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Worklist unit           | `src/modules/Admisi/types/ranapOperationalWorklist.ts:3-13`                                                | A row has `itemId`, `jenis`, and `aggregateStatus`. The public unit is an aggregate record, not a journey.                                                                                  |
| Active dataset          | `src/modules/Admisi/composables/useActiveOperationalDataset.ts:5-30`                                       | Loads `operational-worklist` with `includeTerminal: false`.                                                                                                                                 |
| Active filtering        | `src/modules/Admisi/composables/useRanapWorkList.ts:17-18`                                                 | Applies the omni filter to the raw aggregate rows in the browser.                                                                                                                           |
| Historical search       | `src/modules/Admisi/composables/useHistoricalSearch.ts:16-31`                                              | Calls the same aggregate worklist with `includeTerminal: true`, then filters those records in the browser.                                                                                  |
| Search semantics        | `src/modules/Admisi/utils/omniFilterEngine.ts` and `src/modules/Admisi/utils/omniFilterToServerFilters.ts` | The UI exposes record `jenis`; only `jenis` and free text are sent to the server, while several filters are client-only.                                                                    |
| Selection and deep link | `src/modules/Admisi/composables/useAdmisiRanapConsole.ts:21-88`                                            | Selection and URL state are `{ type, id }`, so a Waiting List and its Admission have different deep links.                                                                                  |
| Detail loading          | `src/modules/Admisi/components/ranap/RanapAdmissionWorkspace.vue:83-164`                                   | Four aggregate detail hooks are registered, then one is chosen by selected aggregate type. There is no complete journey query.                                                              |
| Timeline                | `src/modules/Admisi/composables/useAdmissionBranchTimeline.ts`                                             | Timeline content is inferred from whichever aggregate detail is open. Release 1 needs one authoritative source/registration/handover timeline; placement and transfer history are deferred. |
| Primary detail cards    | `src/modules/Admisi/components/ranap/SummaryWorkspace.vue`                                                 | Opname, Reservation, Registration, and Waiting List are rendered as separate primary cards with links between records.                                                                      |
| Worklist card           | `src/modules/Admisi/components/ranap/RanapWorkListCard.vue:34-104`                                         | The card shows the raw `jenis` badge and record-specific actions.                                                                                                                           |
| Counters                | `src/modules/Admisi/composables/useRanapOperationalSummary.ts:22-81`                                       | Counts raw records by type/status and separately counts attention conditions. One episode can contribute to multiple totals.                                                                |
| Status mapping          | `src/modules/Admisi/utils/ranapOperationalStatus.ts`                                                       | Client code translates each aggregate status independently. It can treat a closed Waiting List as a completed result even though Waiting List closure is not episode completion.            |
| Router boundary         | `src/router/routes.ts` and `src/modules/Admisi/configs/tabs.ts`                                            | Rawat Inap already lives under `/app/:screen?/:tab?`; the journey can remain query-selected without adding a new top-level route.                                                           |

The current tests also encode record-level behavior. Examples include raw `OP1` and `WL1` fixtures,
deep links such as `?type=admission&id=REG001`, and assertions for a separate “Lihat Registrasi”
link in:

- `src/modules/Admisi/components/ranap/__tests__/RanapAdmissionWorkspace.spec.ts`
- `src/modules/Admisi/views/__tests__/AdmisiRanapConsole.spec.ts`
- the colocated worklist/composable tests under `src/modules/Admisi`

These tests must be replaced or adapted to assert journey identity and a unified workspace.

### 3.2 Backend relationship evidence

Backend paths in this section are relative to
`D:\Project.Aktif\MyHospitalWeb\b09-bilreg-api`.

| Domain concept             | Concrete artifact                                                                                                                                                                                        | Actual relationship                                                                                                                                                            |
| -------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Opname Request             | `src/bilreg/Bilreg.Domain/AdmisiRanapContext/OpnameRequestFeature/OpnameRequestModel.cs:75-81`                                                                                                           | Has its own `OpnameRequestId`; fulfillment stores `FulfilledRegId`.                                                                                                            |
| Reservation                | `src/bilreg/Bilreg.Domain/AdmisiRanapContext/ReservationFeature/ReservationModel.cs:77-83`                                                                                                               | Has its own `ReservationId`; realization stores `RealizedRegId`.                                                                                                               |
| Reservation independence   | `src/bilreg/Bilreg.SqlDb/AdmisiRanapContext/ReservationFeature/BILRG_AdmReservation_M2_DropOpnameRequestId_Alter.sql:1-4`                                                                                | Explicitly removes `OpnameRequestId` from Reservation.                                                                                                                         |
| Admission episode          | `src/bilreg/Bilreg.Domain/AdmisiRanapContext/AdmissionFeature/AdmissionModel.cs:120-125`                                                                                                                 | `RegId` identifies Admission and it retains either the source Opname Request ID or Reservation ID.                                                                             |
| Registration orchestration | `src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs:111-168`                                                                                | Admission, legacy Registration, `RegInap`, and `RegAktif` are written with the same `RegId`; the selected source is fulfilled/realized with that ID in the same orchestration. |
| Registration identity rule | `docs/contexts/admisi-ranap/admisi-ranap-registration-orchestration.md`                                                                                                                                  | States that `RegId` is reused across the registration artifacts and is not replaced by a second episode identity.                                                              |
| Waiting List               | `src/bilreg/Bilreg.Domain/AdmisiRanapContext/WaitingListFeature/WaitingListModel.cs:81-83`                                                                                                               | Every Waiting List has its own `WaitingListId` and an Admission `RegId`.                                                                                                       |
| Active Waiting List        | `src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/WaitingListFeature/WaitingListDal.cs` and `src/bilreg/Bilreg.Application/AdmisiRanapContext/WaitingListFeature/UseCases/AdmCreateWaitingListCmd.cs` | Reads the active handover by `RegId` and enforces at most one active Waiting List, while allowing historical Waiting Lists for the same episode.                               |
| Current worklist query     | `src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/OperationalWorklistFeature/OperationalWorklistDal.cs:33-209`                                                                                        | Executes four branches with `UNION ALL`. Admission and Waiting List for one `RegId` remain two rows.                                                                           |
| Current worklist DTO       | `src/bilreg/Bilreg.Application/AdmisiRanapContext/OperationalWorklistFeature/IOperationalWorklistDal.cs`                                                                                                 | Exposes `ItemId`, `Jenis`, and `AggregateStatus`, preserving aggregate-record semantics.                                                                                       |
| Current list API           | `src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/OperationalWorklistController.cs`                                                                                                                  | `GET /api/admisi-ranap/operational-worklist` returns aggregate rows and only requires authentication.                                                                          |
| Bed ownership boundary     | `docs/contexts/admisi-ranap/admisi-ranap-domain.md` and `src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmissionModelTest.cs`                                                               | Admission deliberately has no room/bed allocation. Ward owns accommodation after the handover.                                                                                 |

The SQL and domain model therefore already provide the core registered-episode relationship:

```text
OpnameRequest.FulfilledRegId ─┐
                              ├─ Admission.RegId ─ Registration.RegId
Reservation.RealizedRegId ────┘                  ├─ WaitingList.RegId (0..n)
```

The current worklist misses this relationship because it projects aggregate rows independently
instead of grouping facts around `RegId`.

### 3.3 Domain states found in code

| Aggregate      | States                                                                         |
| -------------- | ------------------------------------------------------------------------------ |
| Opname Request | `Requested (0)`, `Fulfilled (1)`, `Cancelled (2)`                              |
| Reservation    | `Reserved (0)`, `Maintained (1)`, `Realized (2)`, `Cancelled (3)`              |
| Admission      | `Admitted (0)`, `Updated (1)`, `Waiting (2)`, `Completed (3)`, `Cancelled (4)` |
| Waiting List   | `Waiting (0)`, `Accepted (1)`, `Closed (2)`, `Cancelled (3)`                   |

Important consequences:

- Creating a Waiting List does not update Admission status. Admission status alone cannot describe
  accommodation progress.
- `WaitingList.Accepted` means the target Ward accepted the handover. It does not prove room or bed
  assignment.
- `WaitingList.Closed` is not proof of placement, discharge, or completion. Release 1 must expose
  that placement status is unavailable after closure unless an existing authoritative completion
  process has completed or cancelled the Admission.
- `Admission.Waiting` is a legacy/future state and must not be interpreted as an active bed
  assignment.
- The frontend Waiting List schema currently accepts only `0 | 1 | 2`, while the backend also has
  `Cancelled (3)`.

### 3.4 Confirmed placement boundary and release decision

The inspected backend does not contain a general Rawat Inap Bed Assignment aggregate or endpoint
under Admisi Ranap. The Bed Usage area provides reference data, and the available `PakaiBed` flow is
IGD-specific. Current Admisi domain documentation also puts room/bed allocation outside its
boundary.

Phase B0 concludes that Ward placement authority is unavailable. There is no authoritative room,
bed, placement, occupancy, release, or transfer source. This is a known capability boundary, not a
blocker for consolidating the currently authoritative journey facts.

Release 1 therefore:

- derives journeys only from Opname Request, Reservation, Admission/Registration, and Waiting List;
- treats placement fields as optional and absent;
- maps Waiting List `Accepted` to handover accepted, not bed assigned;
- maps a previously closed handover with no active Waiting List to
  `PLACEMENT_STATUS_UNAVAILABLE`, unless an existing authoritative Admission process proves
  completion or cancellation;
- returns Ward-owned next tasks with `canExecute = false` for Admisi;
- shows the responsible target Ward and explains that room/bed status is not available; and
- ships without a Ward adapter, dummy Ward records, or inferred placement facts.

Release 2 may consume a future Ward-owned read contract keyed by `RegId`. That future contract is
expected to expose at least:

- `RegId`;
- assignment identity and normalized state (`ACTIVE` or `RELEASED`);
- ward, room, and bed identity/display name;
- assigned and released timestamps;
- optional source and target ward;
- the associated `WaitingListId` or an equally stable handover correlation;
- handover kind (`ADMISSION_TO_WARD` or `WARD_TO_WARD`) when it cannot be derived from authoritative
  Ward events.

This contract is documentation for future integration, not a Release 1 implementation task. When
Ward Management is eventually created, carrying `RegId` is a Ward-domain responsibility. Neither
release may infer placement from Waiting List status, ward name, patient identity, or timestamps.

## 4. Canonical journey identity

### 4.1 Join key versus public read-model identity

Two identifiers serve different purposes:

- **Episode join key:** `RegId`, available after registration. Release 1 uses it to join Admission,
  Registration, and every Waiting List record. Release 2 can extend the same join to authoritative
  Ward placement history.
- **JourneyId:** an opaque read-model and route identifier that can exist before registration and
  remain stable afterward.

For valid data, the backend derives `JourneyId` as follows:

| Journey origin                            | Canonical JourneyId     | Behavior after registration                                     |
| ----------------------------------------- | ----------------------- | --------------------------------------------------------------- |
| Opname Request                            | `opn:{OpnameRequestId}` | Admission retains `OpnameRequestId`, so the ID does not change. |
| Reservation                               | `rsv:{ReservationId}`   | Admission retains `ReservationId`, so the ID does not change.   |
| Direct or legacy Admission with no source | `reg:{RegId}`           | Uses the registered episode identity.                           |

The string format is an API implementation detail. Clients must treat it as opaque and URL-encode
it. Domain IDs embedded in it are not rendered in the normal UI.

### 4.2 Data-integrity rules

The projection must never silently collapse suspicious data:

- Exactly one non-empty source is expected for a sourced Admission.
- A source should resolve to at most one Admission.
- A fulfilled/realized source must resolve to the same `RegId` stored by the Admission.
- Waiting Lists joined to an episode must use that episode's `RegId`.
- At most one Waiting List may be active for one `RegId`.

When these rules fail, the backend uses `reg:{RegId}` for each conflicting Admission, returns a
`NEEDS_RECONCILIATION` stage, and reports structured audit issues. It must not merge multiple
Admissions just because they share a source or patient.

### 4.3 Legacy URL resolution

Add a resolver for existing `{type, id}` links:

- Opname Request ID resolves directly to `opn:{id}`.
- Reservation ID resolves directly to `rsv:{id}`.
- Admission/Registration `RegId` resolves through the Admission source, otherwise `reg:{RegId}`.
- Waiting List ID resolves to its `RegId`, then through Admission to the canonical JourneyId.

The frontend replaces the old URL with `?journey={JourneyId}` after resolution. The resolver is
also the compatibility path for bookmarks, notifications, and links from other modules.

## 5. Proposed backend read model

### 5.1 Release 1 projection shape

Create a read-only `RawatInapJourneyProjection` with these conceptual groups:

| Group                  | Required fields                                                                                                               |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Identity               | `journeyId`, `regId?`, origin kind, prospective/registered/terminal flags                                                     |
| Patient                | patient ID, medical-record number, display name, sex, birth date, age, contact summary                                        |
| Operational stage      | one normalized Release 1 stage, condition title, short explanation, `stageSince`, attention flags                             |
| Next task              | action code, label, responsible actor/unit, `canExecute`, blocked reason, required permission                                 |
| Care request           | requested/effective class, target Ward, priority, waiting start and duration                                                  |
| Guarantor              | guarantor/type ID and display name, policy/participant summary where available                                                |
| Clinical               | admission reason/diagnosis, procedure, DPJP/referring doctor, referral/source summary                                         |
| Placement availability | `UNAVAILABLE` and a user-facing reason; actual room, bed, occupancy, and release fields are omitted in Release 1              |
| Timeline               | ordered source, registration, Waiting List handover, authoritative completion, and cancellation events                        |
| Audit                  | source IDs/statuses/timestamps, Admission/Registration IDs, all Waiting List IDs, reconciliation issues, projection timestamp |
| Concurrency            | `asOf`, projection version, and action-specific expected versions/timestamps when needed                                      |

The list DTO is a deliberate subset of the same projection. The detail endpoint enriches it; it
does not change its identity, stage, owner, or next-action semantics.

### 5.2 Release 1 derivation strategy

Implement the first version as a query-time read model:

1. Select prospective active/terminal sources.
2. Select Admissions and join their originating source.
3. Join legacy Registration, `RegInap`, guarantor, DPJP/doctor, and other existing registration
   facts by `RegId`.
4. Fold all Waiting List records for each `RegId` into the active handover and handover history.
5. Run one pure Release 1 stage/owner/action resolver over those existing facts.
6. Apply journey-level filters and search.
7. Calculate stage facets from the final one-row-per-journey set.
8. Page and sort the final journey rows.

Release 1 performs no Ward lookup and defines no Ward adapter. This is a read projection, not a new
aggregate. If query volume later requires a materialized read store, it may be fed only by the
existing Release 1 authorities. That optimization still must not become a command model.

### 5.3 List and search API

Add:

```http
GET /api/admisi-ranap/journeys
```

Supported query parameters:

| Parameter                                           | Meaning                                                                                                                  |
| --------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `scope=active\|history`                             | `active` excludes terminal journeys; `history` searches the full corpus, including active and terminal journeys.         |
| `stage`                                             | Optional normalized journey stage.                                                                                       |
| `search`                                            | Patient name/MR, doctor, guarantor, ward, and optionally exact system IDs.                                               |
| `dokterId`, `bangsalId`, `kelasId`, `tipeJaminanId` | Journey-level filters.                                                                                                   |
| `priority`                                          | Normalized handover priority.                                                                                            |
| `dateFrom`, `dateTo`                                | Applies to a documented journey date field, preferably `lastOperationalAt`; do not switch date semantics by record type. |
| `cursor`, `pageSize`                                | Stable cursor pagination over `sortAt`, then `journeyId`.                                                                |

Example response:

```json
{
  "items": [
    {
      "journeyId": "opn:OPN-000123",
      "patient": {
        "patientId": "P0001",
        "medicalRecordNumber": "RM0001",
        "name": "Atiqa"
      },
      "stage": "WARD_ACCEPTANCE_REQUIRED",
      "currentCondition": {
        "title": "Menunggu penerimaan bangsal",
        "explanation": "Handover akomodasi telah dibuat dan menunggu respons bangsal tujuan.",
        "since": "2026-07-13T02:00:00Z"
      },
      "nextTask": {
        "code": "WARD_ACCEPT_HANDOVER",
        "label": "Terima handover akomodasi",
        "owner": {
          "domain": "WARD",
          "unitId": "WARD-01",
          "name": "Bangsal Melati"
        },
        "canExecute": false,
        "blockedReason": "Tugas ini merupakan tanggung jawab Bangsal Melati dan tidak dapat dijalankan dari Admisi."
      },
      "careClass": { "id": "KLS-2", "name": "Kelas 2" },
      "targetWard": { "id": "WARD-01", "name": "Bangsal Melati" },
      "priority": "NORMAL",
      "waitingSince": "2026-07-13T02:00:00Z",
      "waitingDurationSeconds": 3600,
      "attentionFlags": ["WAITING_OVER_THRESHOLD"],
      "sortAt": "2026-07-13T02:00:00Z"
    }
  ],
  "stageFacets": [
    { "stage": "REGISTRATION_REQUIRED", "count": 4 },
    { "stage": "HANDOVER_REQUIRED", "count": 2 },
    { "stage": "WARD_ACCEPTANCE_REQUIRED", "count": 3 },
    { "stage": "HANDOVER_ACCEPTED", "count": 1 },
    { "stage": "PLACEMENT_STATUS_UNAVAILABLE", "count": 8 },
    { "stage": "NEEDS_RECONCILIATION", "count": 1 }
  ],
  "totalMatches": 19,
  "nextCursor": null,
  "asOf": "2026-07-13T03:00:00Z",
  "projectionVersion": 1
}
```

`stageFacets` are calculated after scope and non-stage filters, but before the optional `stage`
filter and pagination. Every matching journey contributes to exactly one facet. Therefore the
facet counts sum to `totalMatches` for the same scope and non-stage filters.

### 5.4 Detail workspace API

Add:

```http
GET /api/admisi-ranap/journeys/{journeyId}
```

The response contains one complete workspace projection:

```json
{
  "journeyId": "opn:OPN-000123",
  "patientEpisodeHeader": {},
  "stage": "WARD_ACCEPTANCE_REQUIRED",
  "currentCondition": {},
  "nextTask": {},
  "timeline": [],
  "operationalSummary": {},
  "information": {
    "patientAndContact": {},
    "guarantorAndCareClass": {},
    "clinical": {},
    "placementAvailability": {
      "status": "UNAVAILABLE",
      "reasonCode": "WARD_MANAGEMENT_NOT_IMPLEMENTED",
      "responsibleWard": { "id": "WARD-01", "name": "Bangsal Melati" },
      "explanation": "Status kamar dan tempat tidur belum tersedia di sistem."
    }
  },
  "allowedActions": [
    {
      "code": "CANCEL_ADMISSION",
      "label": "Batalkan admisi",
      "canExecute": true,
      "blockedReason": null,
      "requiredPermission": "admisi.ranap.cancel"
    }
  ],
  "systemAudit": {
    "origin": {},
    "admission": {},
    "registration": {},
    "waitingLists": [],
    "integrityIssues": []
  },
  "asOf": "2026-07-13T03:00:00Z",
  "projectionVersion": 1
}
```

The endpoint must not require the client to call Admission, Waiting List, Registration, or
guarantor endpoints to understand the episode. It makes no Ward call in Release 1. The UI receives
one coherent workspace contract and an explicit statement that placement details are unavailable.

Actual placement properties are optional extension fields. They are omitted from Release 1 rather
than populated with null-like dummy Ward, room, or bed records.

### 5.5 Release 2 Ward placement contract

Release 2 is not part of the current implementation. After Ward Management exists, define a
Ward-owned query contract keyed by `RegId` with this minimum semantic shape:

```json
{
  "regId": "REG-000123",
  "currentPlacement": {
    "placementId": "opaque-ward-id",
    "status": "ACTIVE",
    "ward": {},
    "room": {},
    "bed": {},
    "assignedAt": "2026-07-13T04:00:00Z"
  },
  "placementHistory": [
    {
      "placementId": "opaque-ward-id",
      "status": "RELEASED",
      "assignedAt": "2026-07-10T04:00:00Z",
      "releasedAt": "2026-07-13T03:00:00Z"
    }
  ],
  "handoverCorrelation": {
    "waitingListId": "WL-000123",
    "kind": "ADMISSION_TO_WARD"
  },
  "asOf": "2026-07-13T04:00:00Z",
  "version": 1
}
```

Ward Management will own the model and its commands. The journey projection will consume it as a
read-only authority by `RegId`. Release 2 may then add optional placement fields, `IN_WARD`, room
and bed information, released placements, transfer cycles, and a complete placement timeline.
This future contract must not be implemented as fake data or an Admisi write model.

### 5.6 Legacy-link resolver

Add either:

```http
GET /api/admisi-ranap/journeys/resolve?recordType={type}&recordId={id}
```

or an equivalent route that returns `{ journeyId }` or `404`. It is a compatibility endpoint, not
a new primary navigation model.

## 6. Operational stage mapping

### 6.1 Resolver precedence

The Release 1 backend owns one pure, exhaustively tested stage resolver. Apply rules in this order:

1. Detect contradictions and integrity failures that prevent a safe interpretation.
2. Resolve cancellation from an existing authoritative source or Admission process.
3. Resolve completion only when an existing authoritative process owns and records episode
   completion. Do not infer it from Waiting List closure.
4. Resolve an active Waiting List: `Waiting` before `Accepted`.
5. If any previous handover is `Closed`, its placement outcome remains unresolved, and no active
   Waiting List exists, return `PLACEMENT_STATUS_UNAVAILABLE`.
6. Resolve an active Admission that has not yet entered or must restart accommodation handover.
7. Resolve an unregistered prospective source.

Release 1 has no placement or transfer facts in its resolver input. It must not contain branches
that test room, bed, occupancy, release, Ward adapter availability, or transfer kind.

### 6.2 Release 1 stage table

| User-facing stage              | Actual facts                                                                                                                                                                                                                             | Current condition                                                                                          | Next task and owner                                                | Allowed Admisi behavior                                                       |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ | ----------------------------------------------------------------------------- |
| `REGISTRATION_REQUIRED`        | No Admission; Opname Request is `Requested`, or Reservation is `Reserved`/`Maintained`.                                                                                                                                                  | Prospective journey is ready or waiting for registration.                                                  | Complete registration — **Admisi**                                 | Open the existing registration flow when policy permits.                      |
| `HANDOVER_REQUIRED`            | Active Admission (`Admitted`, `Updated`, or legacy `Waiting`) has no Waiting List history, or its latest attempted handover is `Cancelled` without a prior closed-handover ambiguity.                                                    | Registration exists and accommodation handover must be created or recreated.                               | Create accommodation handover — **Admisi**                         | Create the existing Waiting List/handover only; never choose a bed.           |
| `WARD_ACCEPTANCE_REQUIRED`     | Active Waiting List is `Waiting`.                                                                                                                                                                                                        | Handover exists and the target Ward has not accepted it.                                                   | Review/accept handover — **target Ward**                           | Read-only monitoring; `canExecute = false` for Admisi.                        |
| `HANDOVER_ACCEPTED`            | Active Waiting List is `Accepted`.                                                                                                                                                                                                       | The target Ward accepted the handover; room and bed assignment are not proven or visible.                  | Continue placement process — **target Ward**                       | Show Ward responsibility and unavailable placement details; no bed action.    |
| `PLACEMENT_STATUS_UNAVAILABLE` | At least one Waiting List is `Closed`, its placement outcome remains unresolved, no Waiting List is active, Admission is not authoritatively completed/cancelled, and no placement authority exists.                                     | A handover record was closed, but the system cannot report room, bed, occupancy, release, or current ward. | Placement follow-up outside current system — **responsible Ward**  | Read-only explanation; `canExecute = false`; never infer `IN_WARD`.           |
| `COMPLETED`                    | An existing authoritative Admission/discharge process explicitly records episode completion. If no such process is confirmed, Release 1 does not synthesize this stage.                                                                  | Rawat Inap episode is authoritatively complete.                                                            | None/archive — **none**                                            | Historical read only.                                                         |
| `CANCELLED`                    | Unregistered source is cancelled, or Admission is authoritatively `Cancelled`, with no contradictory active facts.                                                                                                                       | Prospective or registered journey was cancelled.                                                           | None/archive — **none**                                            | Historical read only.                                                         |
| `NEEDS_RECONCILIATION`         | Missing/mismatched source link, multiple Admissions for one source, two sources on one Admission, multiple active Waiting Lists, orphan Registration/Waiting List, or an internally contradictory state among the Release 1 authorities. | The existing authorities cannot safely determine the handover-level operational state.                     | Investigate data/coordination issue — **configured support owner** | Show a safe read-only workspace and structured issue; do not guess an action. |

Source terminal states are interpreted as follows:

- `OpnameRequest.Fulfilled` and `Reservation.Realized` do not create their own stage. They follow the
  linked Admission journey.
- A fulfilled/realized source without the expected Admission is `NEEDS_RECONCILIATION`.
- `WaitingList.Cancelled` does not cancel the episode. If Admission remains active and there is no
  earlier unresolved closed handover, the episode returns to `HANDOVER_REQUIRED`; otherwise it
  remains `PLACEMENT_STATUS_UNAVAILABLE`.
- `WaitingList.Accepted` produces `HANDOVER_ACCEPTED`; it does not prove a bed assignment.
- `WaitingList.Closed` does not complete the episode and never produces `IN_WARD`. Without an
  authoritative completion/cancellation result, it produces `PLACEMENT_STATUS_UNAVAILABLE`.

### 6.3 Release 2 stage extensions

After Ward Management exists and its read contract is integrated, Release 2 may add placement facts
to the same resolver and introduce `IN_WARD`. It may also resolve repeated transfer cycles:

```text
IN_WARD
  → new WARD_TO_WARD handover
  → WARD_ACCEPTANCE_REQUIRED
  → HANDOVER_ACCEPTED
  → target placement active
  → IN_WARD
```

The `JourneyId` and `RegId` remain unchanged. Release 2 can append release, handover, acceptance,
and new-placement events to the complete timeline. These rules and their tests are explicitly
deferred and are not Release 1 exit criteria.

### 6.4 Attention indicators

Overdue reservations, excessive waiting duration, and other urgency signals are `attentionFlags`,
not stages. They may decorate or sort a row but may not create a second counter contribution.

The current HTML reference's “Semua Aktif” and “Perlu Perhatian” cards overlap other counters. In
the implementation:

- the top counter strip contains only mutually exclusive stage facets; and
- an overall total, if retained, is displayed outside the stage-counter set; attention is a badge
  or filter, not an overlapping counter.

## 7. Task ownership and allowed actions

### 7.1 Server-owned policy decision

The frontend must not infer ownership or authorization from stage, role names, or button visibility.
The workspace projection returns:

- owner domain: `ADMISI`, `WARD`, or `SYSTEM_SUPPORT`; `WARD` expresses operational
  responsibility in Release 1 and does not imply that a Ward application exists;
- owner unit ID/name where relevant;
- next-action code and user-facing label;
- `canExecute` for the authenticated user;
- `blockedReason` suitable for the UI;
- required permission/policy identifier;
- concurrency data required by the existing command.

Controllers currently use broad `[Authorize]` checks. Implementation must add command-specific
authorization policies for existing executable commands before treating `allowedActions` as
reliable. Release 1 creates no Ward command endpoint. UI hiding is not an authorization boundary.

### 7.2 Action mapping

| Stage                        | Action code                     | Responsible actor    | Admisi workspace behavior                                                                    |
| ---------------------------- | ------------------------------- | -------------------- | -------------------------------------------------------------------------------------------- |
| Registration required        | `COMPLETE_REGISTRATION`         | Admisi               | Execute through existing Opname/Reservation registration orchestration.                      |
| Handover required            | `CREATE_ACCOMMODATION_HANDOVER` | Admisi               | Execute through the existing Waiting List/handover command.                                  |
| Ward acceptance required     | `WARD_ACCEPT_HANDOVER`          | Target Ward          | Show owner/status with `canExecute = false`; no Ward UI or command is fabricated.            |
| Handover accepted            | `AWAIT_WARD_PLACEMENT`          | Target Ward          | Explain that handover was accepted and room/bed status is unavailable; no executable action. |
| Placement status unavailable | `AWAIT_PLACEMENT_VISIBILITY`    | Responsible Ward     | Read-only explanation with `canExecute = false`; never offer bed assignment.                 |
| Completed/cancelled          | none                            | none                 | Historical read only.                                                                        |
| Needs reconciliation         | configured investigation action | Support/domain owner | Read-only unless a separately authorized repair workflow exists.                             |

Existing Admisi cancellation/update commands remain independent allowed actions when their command
policy and current domain state allow them; they are not automatically the journey's next task.

`WARD_ACCEPT_HANDOVER`, `AWAIT_WARD_PLACEMENT`, and `AWAIT_PLACEMENT_VISIBILITY` are informational
task descriptors in the Admisi workspace, not write commands or discoverable Ward mutation
endpoints. Release 2 may map them to Ward-owned actions only after Ward Management and its
authorization model exist.

## 8. Filtering, search, sorting, and counters

### 8.1 Active mode

- Query `scope=active` and return one item per journey.
- Apply doctor, guarantor, class, target ward, priority, date, stage, and free-text filters to the
  composed journey facts, not to one selected record branch.
- Remove `jenis` as a user-facing primary filter. Origin can remain an advanced/audit filter only if
  an operational need is confirmed.
- Sort by stage priority, explicit priority, waiting age, and stable `journeyId` tie-breaker. The
  exact sort contract must be fixed and tested.
- Stage counters come from the same filtered journey relation before stage selection and pagination.

### 8.2 Historical/deep search

- Query `scope=history` using the same list DTO, `JourneyId`, stage resolver, and filters.
- Search the full corpus, including terminal journeys; do not switch back to raw aggregate results.
- Support patient name, medical-record number, patient ID, doctor, guarantor, **target Ward from the
  handover**, and exact domain ID lookup for support users. Release 1 does not search current room,
  bed, or placement ward.
- Paginate on the server. Do not download all historical domain records for browser-side filtering.
- Selecting a result opens the same workspace endpoint as active mode.

### 8.3 Frontend filtering boundary

The authoritative result set and counters are server-derived. A client may perform a transient,
instant filter over an already normalized active journey page for typing responsiveness, but it
must reconcile with the debounced backend response and must never:

- join Admission to Waiting List;
- deduplicate by patient or `RegId`;
- derive stage/owner/action;
- calculate global counters from a partial page; or
- change the identity/shape between active and historical modes.

## 9. Backend implementation tasks

### Release 1 Phase B0 — record the confirmed authority boundary

1. Record the completed conclusion: Ward/Bed Management and its placement authority do not exist.
2. Exclude room, bed, occupancy, release, transfer, Ward adapters, and Ward staleness semantics from
   the Release 1 implementation backlog.
3. Treat `PLACEMENT_STATUS_UNAVAILABLE` as an intentional Release 1 result, not a startup failure or
   integration outage.
4. Proceed with Registration/Waiting List journey consolidation without waiting for Ward
   Management.

### Release 1 Phase B1 — journey contracts and pure resolver

1. Add normalized journey, patient, task-owner, handover timeline, placement-availability,
   attention, action, and audit DTOs.
2. Add exhaustive enums for all current domain states, including `WaitingList.Cancelled`.
3. Confirm whether an existing process authoritatively owns `Admission.Completed`; enable
   `COMPLETED` only when that process is evidenced and tested.
4. Implement `JourneyId` derivation and legacy record resolution.
5. Implement the Release 1 stage/owner/next-task resolver with an explicit precedence table and no
   placement inputs.
6. Return structured reconciliation issue codes rather than silently discarding invalid facts.
7. Version the projection contract.

### Release 1 Phase B2 — read DAL/projection

1. Replace the four-branch presentation query with a journey-root query; leave the old query in
   place only for compatibility during rollout.
2. Join source, Admission, legacy Registration, `RegInap`, guarantor, doctor/DPJP, and all Waiting
   List records using stable relationships.
3. Ensure fulfilled/realized sources remain reachable through Admission even though they are not
   independently active.
4. Keep unmatched prospective Opname Requests and Reservations as separate roots.
5. Add indexes where execution plans require them, particularly source fulfillment/realization IDs,
   Admission source IDs, and `(RegId, WaitingListStatus, timestamp)` access.
6. Produce list rows, stage facets, total matches, and cursor paging from the same normalized query.
7. Produce the complete Release 1 detail workspace from one application query.
8. Add query timeouts, tracing, and metrics for query failures, reconciliation counts, and
   `PLACEMENT_STATUS_UNAVAILABLE` volume.

### Release 1 Phase B3 — API and authorization

1. Add list/search, detail, and legacy-resolution endpoints.
2. Validate cursor, scope, stage, filter, and exact-ID search inputs.
3. Add action-specific authorization policies and reuse them in both existing command endpoints and
   `allowedActions` resolution.
4. Return `canExecute = false` and an explanation for Ward-owned informational tasks.
5. Audit existing Waiting List transition endpoints so the Admisi workspace cannot accept/close a
   handover on the Ward's behalf merely because a broad `[Authorize]` policy exists.
6. Never add an Admisi bed-assignment command, proxy endpoint, fake Ward adapter, or dummy placement
   record.
7. Define `404` for unknown journeys and `409` for stale versions of existing executable Admisi
   actions.
8. Keep existing write routes and their aggregate IDs internal to action execution.

### Release 1 Phase B4 — consistency and performance

1. Test the query against realistic active and historical volumes.
2. Verify one-row-per-journey and facet invariants in SQL/integration tests.
3. If a materialized projection becomes necessary, feed it only from existing Release 1
   authorities and expose `asOf`; do not route commands through it.
4. Define cache invalidation for registration, Waiting List creation/update/acceptance/closure,
   cancellation, and any confirmed authoritative completion event.

### Release 2 — future backend integration tasks

These tasks are documented but are not implemented or required by this refactor:

1. After Ward Management exists, validate its `RegId`-keyed read contract and ownership semantics.
2. Add a read-only Ward integration to the journey projection; do not move Ward commands into
   Admisi.
3. Extend the resolver with active/released placement facts, `IN_WARD`, and transfer precedence.
4. Extend the detail projection with optional room, bed, occupancy, release, and placement-history
   fields.
5. Add placement-event invalidation, staleness semantics, and observability appropriate to the real
   Ward source.
6. Preserve Release 1 JourneyIds, routes, and worklist identity while versioning additive contract
   fields.

## 10. Frontend implementation tasks

### Release 1 Phase F1 — types, service, and query state

1. Add Zod schemas for list response, detail workspace, normalized stages, action policies,
   placement availability, and reconciliation issues. Actual placement/history fields remain
   optional and absent.
2. Add journey list/detail/resolver methods and journey-scoped TanStack Query keys.
3. Replace the raw aggregate active-data provider with one journey dataset provider.
4. Make active and historical modes share the same journey item schema.
5. Keep Vue Query hooks at top-level setup/composable scope; use direct fetchers only for explicit
   procedural resolution.

### Release 1 Phase F2 — selection and routing

1. Replace `{ type, id }` selection with one `journeyId` source state.
2. Store it as `?journey={encodedJourneyId}` under the existing Rawat Inap tab route.
3. Resolve old `?type=&id=` URLs once and `router.replace` them with the journey URL.
4. Use history-aware navigation for user selections so browser Back/Forward restores selection;
   reserve `replace` for migration, invalid-selection cleanup, and canonicalization.
5. After registration, invalidate/refetch the same source-derived journey instead of switching the
   URL to a raw `RegId` detail.

### Release 1 Phase F3 — one-journey worklist

1. Replace `itemId`/`jenis` cards with patient identity, stage, condition explanation, next action,
   task owner, care class, target ward, priority, and waiting duration.
2. Replace current type/status filters with stage and journey-level filters.
3. Render backend stage facets as mutually exclusive counters.
4. Preserve virtual scrolling for large lists and reset `scrollTo(0)` after filter/search changes.
5. Keep attention as badges/filter/sorting signals, not extra stage totals.
6. Use the same card component for active and historical results, with terminal styling as needed.

### Release 1 Phase F4 — unified workspace

1. Replace `RanapAdmissionWorkspace` with a journey workspace backed by one detail hook.
2. Remove aggregate-type switching and the four independent primary detail branches.
3. Replace `RegistrationCard`, `WaitingListCard`, and thread-link navigation as the primary
   information architecture with:
   - patient and episode header;
   - current condition;
   - next action and responsible actor;
   - Rawat Inap journey timeline;
   - stage-specific operational summary;
   - patient/contact information;
   - guarantor and care-class information;
   - clinical information;
   - responsible Ward and an explicit placement-status-unavailable explanation; and
   - collapsed Detail Sistem & Audit.
4. Render source, registration, and Waiting List handover events in one timeline. Do not label
   Waiting List history as placement or transfer history.
5. Render only server-returned allowed actions. For Ward-owned tasks, show the responsible Ward,
   `canExecute = false`, and the reason that room/bed status is unavailable.
6. Keep raw domain IDs exclusively inside Detail Sistem & Audit and support copy actions there.

### Release 1 Phase F5 — mutation integration

1. Keep existing Admisi write commands and map journey action codes to them internally.
2. On mutation success, invalidate the journey detail, active list, relevant stage facets, and any
   visible historical result.
3. Handle optimistic concurrency responses without locally guessing the new stage.
4. Refetch the projection after every command; the backend resolver remains the status authority.

### Release 2 — future frontend integration tasks

After a real Ward placement API exists, the frontend may add optional current placement, room/bed,
occupancy, release, transfer-cycle, and placement-timeline UI. Release 2 must preserve JourneyId,
selection, route, list-card identity, and all Release 1 behavior when optional placement data is
absent. These components and tests are not implemented in Release 1.

## 11. Migration and compatibility concerns

1. **Feature rollout:** place the new read APIs and frontend journey UI behind a coordinated feature
   flag until Release 1 data checks and route migration are ready. Ward integration is not a rollout
   prerequisite.
2. **Old APIs:** keep `/operational-worklist` and raw detail endpoints temporarily for non-migrated
   consumers. Mark them deprecated and instrument usage before removal.
3. **Old links:** support `{type,id}` resolution for a defined deprecation window. Unknown or
   ambiguous links open a reconciliation/error state, not a guessed patient episode.
4. **No JourneyId backfill:** deterministic IDs avoid adding a column or rewriting existing write
   aggregates. Only a future materialized read store would persist them.
5. **Legacy/direct Admission:** an Admission without a valid source uses `reg:{RegId}`.
6. **Bad historical data:** do not hide or merge it. Surface `NEEDS_RECONCILIATION`, measure issue
   codes, and repair through an owner-approved process.
7. **Known placement boundary:** Release 1 has no Ward dependency or outage mode. It returns
   `PLACEMENT_STATUS_UNAVAILABLE` after a closed handover when completion/cancellation is not
   authoritatively known. Release 2 may define real availability/staleness semantics after Ward
   Management exists.
8. **Cancellation contract drift:** frontend `cancelAdmissionInputSchema` currently sends only
   `userId` and expects the older command-done response, while the current backend coordinated
   cancellation requires reason, expected status, optional expected timestamp, and request ID.
   Align this contract as a compatibility task during the refactor; do not reintroduce the old
   cancellation behavior.
9. **Waiting List enum drift:** update frontend contracts to include backend status `Cancelled (3)`.
10. **Contract evolution:** Release 2 adds placement fields as optional/versioned extensions. It must
    not invalidate Release 1 JourneyIds, routes, stages that remain semantically valid, or clients
    that do not consume placement data.
11. **Documentation drift:** current worklist reports describe aggregate rows, and at least one SOP
    still implies Admisi bed assignment. During implementation update the Admisi Rawat Inap module
    docs, API docs, workflows/SOP, implementation notes, and Indonesian changelog to explain the
    Release 1 placement boundary, Ward responsibility, and journey semantics.

## 12. Acceptance-scenario mapping

### 12.1 Release 1 acceptance scenarios

| Scenario                                               | Required test data                                                                            | Expected result                                                                                                                                                                                           |
| ------------------------------------------------------ | --------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Atiqa has Registration and Waiting List in one episode | One Admission/Registration `RegId`; Waiting List with the same `RegId`                        | Exactly one row and one workspace. Registration and Waiting List IDs appear only in Detail Sistem & Audit. Stage follows the authoritative Waiting List facts.                                            |
| Same patient has two episodes                          | Same `PasienId`; two different valid episode origins/`RegId` values                           | Two journey rows with distinct JourneyIds. No patient-based merge.                                                                                                                                        |
| Pre-registration Opname Request                        | `Requested` Opname Request with no Admission                                                  | One `opn:` prospective journey in `REGISTRATION_REQUIRED`.                                                                                                                                                |
| Independent Reservation                                | `Reserved` or `Maintained` Reservation with no Admission                                      | One `rsv:` prospective journey, independent of any Opname Request for that patient.                                                                                                                       |
| Waiting for Ward acceptance                            | Waiting List in `Waiting`                                                                     | One journey in `WARD_ACCEPTANCE_REQUIRED`; target Ward is responsible and Admisi receives no executable Ward action.                                                                                      |
| Handover accepted                                      | Waiting List in `Accepted`                                                                    | One journey in `HANDOVER_ACCEPTED`; the workspace says the Ward accepted handover, room/bed status is unavailable, and `canExecute = false` for Admisi.                                                   |
| Closed handover without placement authority            | Latest Waiting List is `Closed`; Admission is neither authoritatively completed nor cancelled | One journey in `PLACEMENT_STATUS_UNAVAILABLE`; it never becomes `IN_WARD` or `COMPLETED`, and no room/bed data is synthesized.                                                                            |
| Counters                                               | Dataset containing every Release 1 stage and multiple domain records within episodes          | Each journey contributes to one mutually exclusive facet only; facet sum equals total matches.                                                                                                            |
| Admisi ownership boundary                              | Any Ward-owned handover/placement-follow-up stage                                             | Responsible Ward and explanation are visible, `canExecute = false`, and no Admisi bed-assignment command/button/endpoint exists.                                                                          |
| Active and historical search                           | One active and one terminal journey; search by patient and exact system ID                    | Both modes return the same journey shape and stable JourneyId; historical mode includes terminal journeys without reverting to raw records.                                                               |
| Unified workspace                                      | Journey containing source, Registration, and Waiting List                                     | One detail request renders header, condition, next task/owner, handover timeline, summaries, grouped user information, and system/audit details without separate primary Registration/Waiting List cards. |

### 12.2 Release 2 deferred scenarios

The following scenarios are deliberately excluded from Release 1 acceptance and do not block its
release:

- authoritative active placement and `IN_WARD`;
- room and bed assignment/details;
- placement history and occupancy;
- bed/placement release;
- Ward-to-Ward transfer cycles and their timeline; and
- Ward adapter availability, staleness, and failure behavior.

They become Release 2 acceptance scenarios only after Ward Management exists and its `RegId`-keyed
read contract is authoritative.

## 13. Automated verification

Sections 13.1–13.4 are Release 1 verification. Placement, `IN_WARD`, release, transfer, and Ward
adapter tests remain deferred with Release 2.

### 13.1 Backend unit tests

- JourneyId remains stable from prospective source through registration.
- Two episodes for one patient remain distinct.
- Reservation never joins an Opname Request.
- Every Release 1 resolver combination maps to exactly one stage.
- Waiting List `Closed` never produces `COMPLETED` by itself.
- Waiting List `Cancelled` returns an active Admission with no earlier unresolved closed handover to
  `HANDOVER_REQUIRED`.
- Waiting List `Accepted` produces `HANDOVER_ACCEPTED`, never bed-assigned or `IN_WARD`.
- A closed handover without authoritative completion/cancellation produces
  `PLACEMENT_STATUS_UNAVAILABLE`.
- The Release 1 resolver has no placement, occupancy, release, transfer, or Ward-adapter input.
- Ownership/action policies prevent Admisi bed assignment.
- Invalid relationships produce deterministic issue codes and no silent merge.

### 13.2 Backend integration/contract tests

- SQL/read DAL returns one row for Admission plus one or more Waiting Lists sharing `RegId`.
- Two `RegId` values for the same patient return two rows.
- Prospective Opname and Reservation roots remain independent.
- Facet counts equal the unpaged journey total for the same filter context.
- Active and historical endpoints share the same item contract.
- Search matches patient/MR, doctor, guarantor, ward, and authorized exact IDs.
- Cursor paging is stable under equal timestamps.
- Detail returns all required Release 1 sections in one API response and omits actual placement
  fields.
- Legacy Admission and Waiting List links resolve to canonical JourneyId.
- Unauthorized action endpoints return `403` even if a client fabricates a request.
- No journey query calls a Ward adapter or requires Ward configuration.

### 13.3 Frontend unit/component tests

- Zod schemas accept every Release 1 stage, placement-unavailable metadata, and Waiting List status
  3 in audit data.
- Selection state uses one JourneyId and migrates old links.
- Worklist cards never render raw aggregate type as the primary identity.
- Counters render backend facets and do not recompute from partial pages.
- Active and historical results use the same component and route behavior.
- Workspace makes one journey detail query per selection and renders no separate primary
  Registration/Waiting List cards.
- Domain IDs are absent from normal sections and present in the collapsed audit section.
- Ward-owned stages render the responsible Ward, unavailable room/bed explanation, and no
  executable bed button.
- Handover timelines contain only source, registration, Waiting List, authoritative completion, and
  cancellation events.
- Optional Release 2 placement properties may be absent without schema or rendering failure.
- Virtual-list scroll resets after filter/search changes.
- Mutation success preserves JourneyId and refreshes server-derived stage/action state.

### 13.4 End-to-end tests

Seed or mock all acceptance scenarios and verify:

1. Atiqa appears once despite Admission and Waiting List records.
2. Two Atiqa episodes appear separately.
3. Prospective Opname and independent Reservation appear separately.
4. Registration keeps the same selected JourneyId and changes the workspace stage after refetch.
5. Creating a handover moves the journey to `WARD_ACCEPTANCE_REQUIRED` without exposing a Ward
   mutation from Admisi.
6. Accepting the handover through existing authoritative data produces `HANDOVER_ACCEPTED` without
   room/bed claims.
7. A closed handover produces `PLACEMENT_STATUS_UNAVAILABLE`, never `IN_WARD`.
8. Reload, Back, Forward, copied links, and old-link migration preserve the correct journey.
9. Active filters and historical deep search produce the same row representation.

## 14. Browser and interaction verification

Perform manual browser verification against the integrated APIs at desktop and narrow viewport
widths, including at least 1440 px and 390 px:

- compare the information hierarchy with the HTML reference;
- confirm one selection produces one list request and one workspace request, without aggregate
  detail fan-out;
- verify stage-counter clicks, combined filters, search debounce, empty states, errors, and cursor
  loading;
- inspect the network log to confirm no frontend deduplication, Ward adapter call, or hidden
  bed-assignment mutation;
- verify long-list virtualization with at least 500 journeys and scroll reset after filtering;
- verify waiting-duration updates without changing identity or triggering duplicate requests;
- verify keyboard navigation, focus restoration after selection, accessible labels, contrast, and
  audit disclosure behavior;
- verify `HANDOVER_ACCEPTED` and `PLACEMENT_STATUS_UNAVAILABLE` explain that room/bed status is not
  available and do not show false placement data;
- verify raw IDs are visible only after opening Detail Sistem & Audit; and
- verify the Admisi user cannot reach a bed assignment control through direct navigation or a
  fabricated frontend state.

## 15. Explicit non-goals

- Creating a new Rawat Inap write aggregate or replacing the existing Admission aggregate.
- Adding an Opname Request relationship to Reservation.
- Merging records by patient identity, name, medical-record number, date proximity, or display text.
- Creating Ward/Bed Management as part of this refactor.
- Creating dummy Ward, room, bed, placement, occupancy, release, or transfer records.
- Implementing a fake Ward adapter or blocking Release 1 on a future Ward source.
- Implementing room/bed assignment in Admisi or proxying Ward commands through Admisi.
- Treating Waiting List `Accepted` as proof of bed assignment.
- Treating a closed Waiting List as proof of active placement, `IN_WARD`, discharge, or episode
  completion.
- Implementing `IN_WARD`, placement history, release, or Ward-to-Ward transfer in Release 1.
- Rewriting all existing owner-specific command handlers solely for the new UX.
- Persisting JourneyId in source aggregate tables unless a later, separately approved read-store
  optimization requires it.
- Redesigning the application's top-level `/app/:screen?/:tab?` routing model.
- Exposing system IDs as primary worklist/workspace information.
- Letting the frontend join, deduplicate, or infer stage, owner, authorization, or placement from
  ward name, timestamps, patient identity, or Waiting List status.
- Expanding this refactor into clinical inpatient documentation, billing, discharge, or pharmacy
  workflows beyond the facts needed to represent the journey.

## 16. Delivery sequence and exit criteria

### 16.1 Release 1 — Journey through accommodation handover

1. Record Phase B0's confirmed conclusion that placement authority is unavailable and freeze the
   Release 1 boundary.
2. Implement and test journey identity plus the Release 1 stage/ownership resolver.
3. Implement the Opname/Reservation/Admission/Registration/Waiting List read projection and new
   list/detail/resolver APIs.
4. Add authorization policies for existing executable Admisi commands and align known
   frontend/backend contract drift.
5. Implement the frontend journey state, worklist, routing migration, and unified workspace behind a
   feature flag.
6. Run the Release 1 automated, browser, performance, and accessibility verification.
7. Update module documentation/SOP/API docs/changelog and roll out with old-API/link telemetry.
8. Remove deprecated record-oriented UI/API paths only after all consumers and bookmarks have passed
   the defined migration window.

Release 1 is complete when:

- every visible row, search result, and counter is journey-based and mutually exclusive by stage;
- Atiqa consolidation, multiple episodes, both prospective source types, ownership, filtering,
  historical search, and unified workspace scenarios pass;
- the detail view is served by one coherent projection using only current authorities;
- Waiting List `Accepted` is shown only as accepted handover;
- closed handovers without authoritative terminal state show
  `PLACEMENT_STATUS_UNAVAILABLE`, never `IN_WARD`;
- Ward-owned tasks return `canExecute = false` for Admisi and no bed-assignment capability exists;
- placement fields are optional/absent and the workspace clearly explains their unavailability; and
- no normal UI requires the user to understand separate Registration and Waiting List records.

Release 1 does not wait for Ward Management, placement history, room/bed assignment, release,
transfer, or Ward-adapter verification.

### 16.2 Release 2 — Ward Management integration

Release 2 begins only after Ward Management exists independently with an authoritative
`RegId`-keyed placement read contract. Its future delivery sequence is:

1. Validate the real Ward contract, authorization boundary, availability, and event semantics.
2. Integrate Ward facts read-only into the journey projection.
3. Add active/released placement, `IN_WARD`, room/bed, transfer-cycle, and complete timeline support.
4. Add Release 2 backend, frontend, end-to-end, availability, staleness, and browser tests.
5. Roll out additive/versioned fields without changing existing JourneyIds or moving Ward commands
   into Admisi.

Release 2 is a separate future scope. Its absence does not make Release 1 incomplete.
