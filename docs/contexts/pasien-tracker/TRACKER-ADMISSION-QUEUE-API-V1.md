# Tracker–Admission Queue API v1 and Compatibility Contract

## Consumer inventory

Repository search on 2026-07-23 confirmed no caller of `api/Antrian/anonymous-intake` or
`api/Antrian/start` in `c012_myhospital_web`, and no Bilreg Queue Display/SignalR consumer. The only
adjacent SignalR client belongs to Taksaka operations and is unrelated. Backend tests construct the
legacy direct-start command. Consumers outside the checked repositories, deployed kiosk binaries,
scripts, and third-party integrations cannot be verified; absence from source is not evidence of no
production use.

## Common contract

> **Admission Officer workspace Phase 0 baseline — 2026-07-27.** This contract records backend
> authority only. Ready, Calling, Queued Registration, and Direct Registration are prospective
> frontend workspace projections; they are not Queue Entry states and must not be persisted,
> placed in request payloads, or sent over SignalR. The source plans are maintained at the shared
> workspace root: `admisi-rajal-officer-workspace-mode-feasibility-analysis.md` and
> `admisi-rajal-officer-workspace-refactoring-implementation-master-plan.md`.

Base route: `/api/v1/admission-queue`. All v1 endpoints use the existing JWT authentication
mechanism. Current platform authorization has no approved queue role policies, so v1 uses the
existing authenticated-access boundary; Officer/Supervisor/Administrator/Kiosk/Display policy
mapping is an unresolved security/product decision. `UserId` remains in mutation payloads under the
R-02 deferral. No new claims or actor model is introduced.

Success uses the existing JSend envelope. Errors expose a stable code and no DAL detail:

| HTTP | Code | Meaning |
|---:|---|---|
| 400 | `AQ_INVALID_REQUEST` / `AQ_OPERATION_NOT_ALLOWED` | malformed payload or invalid business transition |
| 401 | `AQ_UNAUTHENTICATED` | missing/invalid authentication |
| 403 | platform authorization response | authenticated but forbidden once approved policies are configured |
| 404 | `AQ_RESOURCE_NOT_FOUND` | unknown queue, entry, Service Point, or Registration |
| 409 | `AQ_CONCURRENCY_CONFLICT` | stale RowVersion, expected-state miss, duplicate outcome, or claim conflict |
| 503 | `AQ_SEQUENCE_EXHAUSTED` | Service Point/Business Date sequence reached 9999 |

`ExpectedRowVersion` is the Base64 representation returned by the current-display snapshot.
Loket mutations require `X-Loket-Key` and `X-Workstation-Key`; the Loket header must match the
payload and the configured static workstation-to-Loket mapping. The configuration rejects duplicate
workstation keys and duplicate Loket keys at startup. An unmapped or mismatched workstation is a
400 request error. A second Call on an already-active Loket remains an R-04 claim conflict and is
409. This does not resolve user identity from claims. The deployment edge must protect these headers
from spoofing; a physical workstation configured inconsistently in a separate installation cannot
be detected without the intentionally deferred cross-node coordination capability.

## Routes

| Method and route | Request/query | Result and access context |
|---|---|---|
| `GET service-points?activeOnly=true` | none | ServicePointId, name, prefix, status; authenticated kiosk/officer/display |
| `PUT service-points/{id}` | `{displayName,queuePrefix,active}` | managed Service Point; authenticated administrator pending approved policy |
| `POST intake` | `{servicePointId}` | AntrianId, NoUrut, QueueLabel, CreatedAt; authenticated kiosk; no Loket |
| `POST booking-assistance` | `{bookingId,servicePointId,failureCode?,kioskId,userId}` | ensure one unresolved Booking assistance entry; returns QueueLabel and Existing |
| `GET worklist` | businessDate, optional servicePointId/status/loketKey, offset/limit | queue-only officer projection; authenticated officer |
| `GET displays/current` | optional loketKey | authoritative active display snapshot and AnnouncementVersion; authenticated display |
| `GET rollout/status` | none | schema/index preflight + feature-flag/workstation uniqueness summary (no keys); authenticated ops/engineering |
| `POST entries/{q}/{n}/call` | `{loketKey,userId}` | Outstanding; officer-selected entry only |
| `POST entries/{q}/{n}/recall` | `{loketKey,expectedRowVersion,userId}` | retained Outstanding |
| `POST entries/{q}/{n}/return-to-waiting` | versioned Loket payload | claim Released; Queue Entry remains Waiting |
| `POST entries/{q}/{n}/cancel-registration` | versioned Loket payload | In Service Queue Entry returns to Waiting; claim Released |
| `POST entries/{q}/{n}/start-service` | versioned Loket payload | InService |
| `POST entries/{q}/{n}/withdraw` | `{reason,loketKey?,expectedRowVersion?,userId}` | Withdrawn; Loket/version required when actively called |
| `POST entries/{q}/{n}/no-show` | versioned Loket payload | Withdrawn with `NoShow` |
| `POST entries/{q}/{n}/redirect` | `{targetServicePointId,loketKey?,expectedRowVersion?,userId}` | new Priority Waiting entry and label |
| `POST entries/{q}/{n}/outcomes/established` | `{loketKey,expectedRowVersion,regId,userId}` | immutable Established outcome and Done entry |
| `POST entries/{q}/{n}/outcomes/not-established` | `{loketKey,expectedRowVersion,reasonCode,userId}` | immutable NotEstablished outcome and Done entry |
| `GET closing-preview` | `businessDate=yyyy-MM-dd&servicePointId` | supervisor-only remaining Waiting-entry review; preview is not a reservation |
| `POST close` | `{businessDate,servicePointId,userId,decisions[]}` | supervisor-only atomic terminal disposition of the exact remaining Waiting set |

`GET worklist` remains **queue-only**. It must not return Booking, patient identity, Registration,
eligibility, or physician enrichment.

Return to Waiting is the non-final disposition for an unanswered Outstanding call. It conditionally
releases only the configured current Loket's matching claim, preserves Queue Entry state and call
history, emits no announcement, and records `RETURN_TO_WAITING` / `UnansweredCall` in the shared
append-only audit log. It is not No-Show, Withdraw, or rollback from In Service.

### Frozen officer terminology

| Backend action/outcome | Operator label | Contract distinction |
|---|---|---|
| Call | `Panggil` | Acquires the Outstanding Loket claim. |
| Recall | `Panggil Lagi` | Retains the same Outstanding claim. |
| Return to Waiting | `Tidak Hadir` | Non-terminal; releases the claim and leaves the entry Waiting. |
| Start Service | `Hadir` | Moves the claim and Queue Entry to In Service. |
| No-Show | `Tidak Datang` | Terminal supervisor disposition; stored as `Withdrawn` with reason `NoShow`. |
| NotEstablished outcome | `Registrasi Tidak Terbentuk` | Queue completion with an externally owned reason code. |

“No Show” is a backend/API reason name only and is not operator-facing wording. In particular,
`Tidak Hadir` must never invoke the terminal No-Show endpoint or create a Registration Outcome.

### Supervisor Queue Closing

Queue Closing is enabled by default and protected by the `AdmissionQueueSupervisorOperations`
policy. It accepts either the `AdmissionQueue.SupervisorOperations` permission claim or a role listed
in `AdmissionQueueApi:SupervisorOperationAllowedRoles` (default: `ADM-SPV`); an absent permission
and absent matching role fail closed. The existing `withdraw` and `no-show` routes use the same policy.

`GET closing-preview` returns every Waiting entry for the exact business date and Service Point,
including queue/patient-reference summaries, call metadata, active claim state, active claim Loket,
and Base64 claim RowVersion. Each result permits exactly `NoShow` or `Withdraw`.

`POST close` requires a decision for exactly every previewed entry at transaction time. `Withdraw`
requires a nonblank reason; `NoShow` is persisted as withdrawal reason `NoShow` and ignores a
submitted reason. An Outstanding claim requires its exact preview RowVersion; an InService claim
blocks the whole operation. Missing/extra/stale/concurrently-created or changed entries return
`409 AQ_CONCURRENCY_CONFLICT`; malformed decisions return `400 AQ_INVALID_REQUEST`; InService
blockers return `400 AQ_OPERATION_NOT_ALLOWED`. All entries, their applicable Outstanding-claim
releases, per-entry audits, and one `ADMISSION_QUEUE_CLOSE` scope summary audit commit together.
Queue Closing never writes a Registration Outcome or Queue Session state.

Operations must stop intake before requesting the preview. A conflict always requires a fresh human
review, and the close runbook must verify an empty scoped preview after a successful close.

### Existing Registration compatibility fixture

The established Registration create routes remain unchanged during Phase 0:

| Request shape | Existing behavior locked by characterization tests |
|---|---|
| Complete `AdmissionAntrianId`, `AdmissionNoUrut`, and `AdmissionExpectedRowVersion` | The controller resolves the configured workstation/Loket; the handler requires the matching In Service claim and completes that admission entry atomically. |
| Partial queue context | Rejected through the existing `ArgumentException` path as `400 AQ_INVALID_REQUEST`. |
| No meaningful queue context | Preserves legacy create-on-registration compatibility, including its synthetic completed Admission Queue entry and REGISTER evidence. This is not the future queue-less Direct Registration contract. |

### Direct Registration create routes (Phase 4)

The following additive routes create a normal outpatient Registration without Admission Queue
participation. They use the same request and JSend response contracts as their corresponding
existing create routes:

| Method and route | Admission Queue behavior |
|---|---|
| `POST /api/Reg/rajalWalkIn/direct` | Queue-less walk-in Registration |
| `POST /api/Reg/rajalByBooking/direct` | Queue-less booking Registration |

Direct requests must not include `AdmissionAntrianId`, `AdmissionNoUrut`, or
`AdmissionExpectedRowVersion`. Supplying any of those fields is rejected as `400
AQ_INVALID_REQUEST`. Direct routes do not resolve or require Admission Queue workstation/Loket
headers.

Registration creation internally selects one transient, JSON-ignored application instruction:

| Behavior | Selected by | Result |
|---|---|---|
| `QueueLinked` | Existing route with complete queue context | Retains atomic Admission Queue completion, Loket claim release, and Established outcome. |
| `LegacyAutoComplete` | Existing route without queue context | Retains legacy synthetic Admission Queue completion for compatibility. |
| `None` | Direct route | Saves Registration and existing side effects, including exactly-once REGISTER tracker evidence, without an Admission Queue entry, Loket claim interaction, or Registration Outcome. |

`None` is an application-layer instruction, not persisted domain state or a workspace mode. Direct
behavior is prospective only: historical synthetic queue artifacts are unchanged, and no database
migration or backfill is required.

### Admisi Rajal composed officer worklist (read-only)

Base route: `/api/v1/admisi-rajal`. Authenticated. Same JSend success envelope.

| Method and route | Request/query | Result and access context |
|---|---|---|
| `GET officer-worklist` | businessDate, optional servicePointId/queueStatus/loketKey/activeOnly/includePagingMetadata, offset/limit | Composes the queue-only projection with Booking, identity, and Registration summaries for officer display |

Composition rules:

- Queue membership, state, call state, LoketKey, timestamps, Queue Label, Priority, and optional
  TrackerId remain Patient Tracker truth (copied from the queue projection).
- Enrichment is best-effort and nullable for anonymous or unresolved entries.
- This route does **not** persist another worklist, duplicate queue state, or become a second ledger.
- Loket workstation headers are not required for this read (same stance as queue `GET worklist`).
- `activeOnly=true` includes only Waiting and In Service and cannot be combined with `queueStatus`.
- The default JSend `data` remains the legacy item array. `includePagingMetadata=true` changes
  `data` to `{items,hasMore,nextOffset}`; `nextOffset` is null when complete.
- Paging fetches one extra row and does not compute `totalActive`. Stable ordering is Priority
  descending, CreatedAt ascending, NoUrut ascending, then AntrianId ascending.

### Journey association (existing officer contract)

Journey resolve remains on `/api/PasienTracker` (not under admission-queue v1):

- `GET /api/PasienTracker/{pasienTrackerId}`
- `GET /api/PasienTracker/candidates`
- `POST /api/PasienTracker/resolve/select`

### NotEstablished ReasonCode boundary

`reasonCode` is validated through `IRegistrationOutcomeReasonCatalog`. Until Operations publishes the
approved catalog, the default pass-through implementation requires a non-empty code only and invents
no business reason values. Rejection maps to `AQ_INVALID_REQUEST`.

Every response containing a queue number exposes QueueLabel when its immutable session prefix exists;
historical sessions may return null/unavailable.

## Booking assistance / HiDok consumer contract (Phase 3)

Receive-side ensure API only. Bilreg does **not** own Self-Registration decision branching.
The external HiDok/Admisi Self-Registration caller owns when (and whether) to invoke this route.

### When to call

| Self-Registration outcome | Caller action |
|---|---|
| Registration established (success) | **Do not** call `POST booking-assistance` |
| Definitive `AssistanceRequired` | Call `POST /api/v1/admission-queue/booking-assistance` once |
| Transient / uncertain failure | **Do not** call; retry Self-Registration per caller policy |

### Request and response

- **Route:** `POST /api/v1/admission-queue/booking-assistance`
- **Auth:** JWT `[Authorize]`; no `X-Loket-Key` / `X-Workstation-Key`
- **Body:** `{ bookingId, servicePointId, failureCode?, kioskId, userId }`
- **Correlation:** server-derived `BOOKING-ASSISTANCE:{BookingId}` (caller does not send it)
- **Success data:** `{ antrianId, noUrut, queueLabel, existing }`
  - `existing=false` — new unresolved assistance entry created
  - `existing=true` — active assistance already exists; business-duplicate success (print/display
    `queueLabel` the same as a new entry)
- **`failureCode`:** optional opaque audit string from the caller; Bilreg stores it and invents no
  business reason catalog values

### Errors (ensure path)

| Condition | HTTP / code |
|---|---|
| Missing/blank `bookingId` | 400 `AQ_INVALID_REQUEST` |
| Unknown Booking or Service Point | 404 `AQ_RESOURCE_NOT_FOUND` |
| Retired / inactive Service Point | 400 `AQ_OPERATION_NOT_ALLOWED` |
| Sequence exhausted (9999) | 503 `AQ_SEQUENCE_EXHAUSTED` |
| Concurrent create with no winner reloadable | 409 `AQ_CONCURRENCY_CONFLICT` |

### Required consumer-contract tests (HiDok / Admisi — outside this repository)

1. Successful Self-Registration creates Registration and **never** invokes booking-assistance
   (`success/no-entry`).
2. Transient / uncertain Self-Registration failure **never** invokes booking-assistance.
3. Definitive `AssistanceRequired` invokes booking-assistance and handles `existing=false`.
4. Retry of the same unresolved Booking treats `existing=true` as success and reuses `queueLabel`.
5. Caller surfaces inactive Service Point and sequence-exhaustion errors without inventing a second
   assistance obligation.

## Kiosk V1

Intake is non-idempotent. There is no required `ClientRequestId`; repeated submissions may create
different entries. The kiosk must disable its button while the request is pending and require a
deliberate retry after uncertainty. Retry identity remains deferred to R-05B.

Walk-in anonymous intake (`POST intake`) remains separate from Booking assistance. Booking
assistance uses the ensure route above and business deduplication by BookingId.

## Display recovery

Persisted snapshots are authoritative. SignalR delivers a best-effort reload hint only. The client
must reload after a hint and reconnect, poll at its configured interval, and play audio only when the
reloaded AnnouncementVersion is greater than its last processed version. No Bilreg display frontend
was found to migrate in the available source tree.

### SignalR refresh-hint contract (Phase 4)

| Item | Value |
|------|-------|
| Hub path | `/hubs/admission-queue` |
| Client event | `RefreshHint` |
| Payload | `{ "loketKey": string \| null }` — hint only; no display fields and no AnnouncementVersion |
| Scope | Broadcast to all connected clients (`Clients.All`); displays filter by `loketKey` |
| Authentication | Same JWT `[Authorize]` boundary as v1 REST (including `GET displays/current`). WebSocket/SSE clients may pass the token as `?access_token=` on the hub negotiate/connect URL. No Display-specific role is introduced (R-02 deferred). |
| Disable | `AdmissionQueueApi:SignalRRefreshEnabled` (default `true`). When `false`, DI binds the no-op publisher; queue write truth is unchanged. |
| Failure | Transport failures are logged and swallowed after commit; polling recovers. |

Do not treat SignalR messages as write authority. AnnouncementVersion semantics remain owned by claim/display persistence. Taksaka `/hubs/operations` is unrelated and must not be reused.

## Legacy compatibility

`POST /api/Antrian/anonymous-intake` retains its legacy payload shape; ServicePointCode is resolved
as the authoritative ServicePointId and caller name/date are no longer authoritative under R-06.
`POST /api/Antrian/start` retains direct Waiting-to-InService semantics. Both routes log warning-level
usage and are controlled by `AdmissionQueueApi:LegacyEndpointsEnabled` (default true); when disabled
they return 404. New clients must not use them. No removal or deprecation date is invented; product
ownership must supply it after consumer migration is verified.

### Legacy / compatibility mutation inventory (Phase 2)

| Route | Role | Gate / observation |
|---|---|---|
| `POST /api/Antrian/anonymous-intake` | Legacy AQ intake | `LegacyEndpointsEnabled`; warning log |
| `POST /api/Antrian/start` | Legacy direct Waiting→InService | `LegacyEndpointsEnabled`; warning log |
| `PATCH /api/Antrian/mulaiPeriksa/{id}/{n}` | Physician Serve (compat) | Ungated; warning log; not officer AQ v1 |
| `PATCH /api/Antrian/selesaiPeriksa/{id}/{n}` | Physician Done (compat) | Ungated; warning log; not officer AQ v1 |
| Registration create-on-queue paths | Legacy registration without prior intake | Compatibility; disposition recorded on [rollout checklist](./TRACKER-ADMISSION-QUEUE-ROLLOUT-CHECKLIST.md) |

Physician/compat mutators are intentionally not feature-gated in Phase 2; they remain inventoried for
Phase 5 compatibility closure. Phase 5 supplies the go/no-go checklist and runbook; it does not invent
a deprecation date. Integration evidence and workstation/edge requirements are in
[TRACKER-ADMISSION-QUEUE-RUNBOOK.md](./TRACKER-ADMISSION-QUEUE-RUNBOOK.md).

### Rollout preflight (Phase 5)

`GET /api/v1/admission-queue/rollout/status` returns `allSchemaReady`, required table/index readiness,
`legacyEndpointsEnabled`, `signalRRefreshEnabled`, `workstationMappingsUnique`, and
`workstationMappingCount`. It never returns workstation or Loket key values.

## Frontend migration contract

No confirmed frontend consumer exists in the available `c012_myhospital_web` tree, so no speculative
frontend files were changed. Any external officer client must split Call from Start Service, reload on
409, and use the worklist/current snapshot as recovery truth. Any display must implement the reload,
poll, and AnnouncementVersion rules above.

## Phase 9 observability contract

Successful and failed officer operational commands emit `AdmissionQueueOperationalEvent`. Its stable dimensions are `Operation`, `Result`, `FailureCategory`, `DurationMs`, `BusinessDate`, `ServicePointId`, `WorkstationKey`, and `LoketKey`. This is operational telemetry, not an API response field and not a persisted Workspace Mode. No request or response contract changes in Phase 9. The event excludes patient data, queue labels, Registration IDs, Booking IDs, user identity, reasons, row versions, and request bodies.
