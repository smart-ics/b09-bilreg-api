# Patient Tracker — Admission Queue Operations Architecture

**Artifact status:** Canonical target architecture analysis

**Bounded context:** Patient Tracker

**Business specification:** [Admission Queue Operations Domain](./TRACKER-ADMISSION-QUEUE-DOMAIN.md)

**Operational specification:** [Admission Queue Operations SOP](./TRACKER-ADMISSION-QUEUE-SOP.md)

**Implementation truth date:** 2026-07-22

## 1. Architecture Overview

Admission Queue Operations extends the existing Bilreg modular monolith so that persisted Service Points, Loket, Kiosks, Queue Labels, Queue Calls, and operational projections can realize the approved domain and SOP.

The backend remains a .NET 8 Clean Architecture modular monolith with this dependency direction:

```text
Bilreg.Api             → Bilreg.Application → Bilreg.Domain
Bilreg.Infrastructure → Bilreg.Application → Bilreg.Domain
```

The current queue implementation is hosted in the `AdmisiContext/AntrianFeature` namespaces across the four projects. The target keeps those physical locations during incremental delivery to avoid an unrelated namespace migration, while treating Patient Tracker as the logical owner described by the domain artifacts.

The target is actor-facing and integration-facing but UI-agnostic:

- `Kiosk` is a new self-service client of Bilreg application interfaces.
- `Queue Display` is a new read-only client that recovers a snapshot and receives near-real-time call notifications.
- `Admission Module` remains a separate client that invokes authorized queue and registration use cases.
- Bilreg API is the composition and transport host; it is not the queue authority.
- Patient Tracker aggregates and application use cases are the write authority.

Major current constraints are the existing `BILRG_Antrian` / `BILRG_AntrianEntry` persistence shape, Dapper DALs, ambient `TransHelper` transactions, `ISequencer`, caller-supplied `UserId`, generic `[Authorize]`, and whole-aggregate queue saves. Major gaps are the absent resource catalogs, Queue Prefix, Queue Label, Queue Call, Loket/Kiosk assignment, display projection, real-time transport, durable notification delivery, contextual authorization, audit coverage, withdrawal, transfer, and intake idempotency.

## 2. Codebase Evidence and Constraints

| Evidence | Verified location | Architectural implication |
|---|---|---|
| Four Clean Architecture projects and inward project references | `src/bilreg/Bilreg.Domain`, `Bilreg.Application`, `Bilreg.Infrastructure`, `Bilreg.Api` project files | New behavior must follow the existing project boundaries; clients must not bypass Application. |
| Queue Session aggregate and entries exist | `Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs`, `AntrianEntryModel.cs` | Extend the existing aggregate instead of creating a competing admission queue model. |
| Queue Entry states are only `Waiting`, `InService`, and `Done` | `AntrianStatusEnum.cs` | `Withdrawn` and Queue Call disposition are target domain-realization gaps. |
| Service Point is only a two-field value snapshot | `ServicePointType.cs` | It cannot satisfy the new Service Point Aggregate, Queue Prefix, lifecycle, or catalog queries. |
| Unused Service Point persistence abstractions exist without an implementation | `ServicePointStatusEnum.cs`, Application-layer `IServicePointDal.cs` | Do not treat Service Point persistence as existing. Replace the Application DAL contract with an aggregate repository; keep DAL contracts and SQL mapping in Infrastructure. |
| Anonymous intake trusts caller-supplied Service Point code and name | `QueAnonymousIntakeCmd.cs` | Target intake must resolve the Service Point from the authenticated Kiosk offering; caller text is not authority. |
| One configured admission Service Point is authoritative | `AdmisiRajalOptions.cs`, `DomainService.cs`, API settings | Configuration is a transitional constraint. Target authority is the Service Point repository; configuration may only seed or select a compatibility default. |
| Admission `start` immediately serves an anonymous entry | `AdmissionQueueStartCmd.cs` | The current command conflates call and service start and carries no Loket. New clients must use separate Call and Start Service use cases. |
| Queue identity and entry persistence exist | `AntrianDto.cs`, `AntrianEntryDto.cs`, `AntrianDal.cs`, `AntrianEntryDal.cs` | Existing tables require additive migration and compatibility mapping, not replacement. |
| Existing queue transaction tables do not carry the full audit-column set required by `docs/DATABASE.md` | `BILRG_Antrian.sql`, `BILRG_AntrianEntry.sql` | Migration design must add accountable mutation evidence or provide the approved equivalent; new transaction tables must follow the current standard. |
| Queue header uniqueness uses `SequenceTag` | `BILRG_Antrian_M1_ServicePointCode_Alter.sql` | Queue Session lookup remains deterministic, but Queue Prefix must not be inferred from `SequenceTag`. |
| Queue numbers use `ISequencer` and `SequenceTag` | `AntrianModel.AddEntry`, `AntrianFactory.Create(ServicePointType, DateOnly)` | Preserve the sequencer initially, but bind its sequence key to the authoritative Queue Session and add intake idempotency. |
| Whole-aggregate repository save compares every entry | `AntrianRepo.SaveChanges` | Concurrent queue actions can overwrite each other. Target lifecycle commands require conditional single-transition persistence. |
| Whole-aggregate comparison physically deletes persisted entries missing from the in-memory list | `AntrianRepo.CompareCollections` and `AntrianEntryDal.Delete` | New admission operations must not use removal to represent withdrawal, no-show, or transfer; historical entries require explicit status/history. |
| Anonymous InService Tracker association has compare-and-set | `TrySaveAnonymousInServiceTransition`, `UpdateFromAnonymousInService` | Reuse this proven pattern for call, start, withdraw, transfer, and complete transitions. |
| Existing queue queries are physician/Registration oriented | `QueListAntrianHeaderQuery.cs`, `QuePasienListQuery.cs`, `QueGetAntrianQuery.cs` | They are not an admission Work List or Queue Display projection and must not be stretched into write authority. |
| Registration and queue association share an ambient transaction | `RegJalanWalkInCommand.cs`, `RegJalanByBookingCmd.cs`, `TrkJourneyResolveSelectCmd.cs` | Preserve local atomicity for Registration, Tracker evidence, Queue Entry association, and local outbox records. |
| HTTP controllers use MediatR and `[Authorize]` | `AntrianController.cs`, `PasienTrackerController.cs` | Expose new use cases through transport adapters; add policy/context authorization rather than controller business logic. |
| Authenticated actor resolution exists but queue commands still accept `UserId` | `HttpCurrentUserContext.cs`, queue command contracts | Target queue commands derive human actor identity from claims and device identity from authenticated client context. |
| Shared audit storage exists | `IAuditRepo`, `AuditLogRepo`, `BILRG_AuditLog.sql` | Use it for administrative and exceptional decisions; operational Queue Call history remains separate queue truth. |
| Durable outbound queue patterns exist | `EmrAntrianOutboundFeature`, `LabOwareFeature` | Reuse explicit pending/processing/succeeded/failed storage and retry ideas for display notification delivery. |
| No SignalR hub, registration, or mapping exists | API project and `Program.cs` | Real-time Queue Display delivery is a target capability, not an existing feature. |
| No Kiosk or Queue Display application exists in this repository | repository file inventory | Their UI and deployment artifacts are later deliverables; backend contracts must remain client-agnostic. |

### 2.1 Constraints

- Preserve existing physician queue allocation and `IQueueNumberCompatibilityAdapter` behavior.
- Preserve both anonymous Tracker sentinels (`""` and `"-"`) until a separate migration removes them.
- Preserve no-kiosk Registration compatibility while it remains used, but do not make it the primary target workflow.
- Use SQL Server, explicit SQL, DTO mapping, repositories, and application-owned transactions.
- Do not require Kiosk or Queue Display availability to commit Registration truth.
- Do not let a client allocate queue numbers or construct an authoritative Queue Label.

### 2.2 Gaps

- No maintained Service Point, Loket, or Kiosk aggregate repository exists.
- No Queue Prefix Snapshot or Queue Label exists.
- No Queue Call or Call Attempt model exists.
- No atomic claim prevents two Loket calling the same Queue Entry.
- No invariant prevents one Loket having multiple active entries.
- No `Withdrawn`, No-Show, Recall, or transfer realization exists.
- No admission-specific worklist or display snapshot projection exists.
- No durable real-time notification mechanism exists.
- No device-scoped authentication or assignment authorization exists.
- No admission queue audit policy is implemented.
- Existing queue tables and mutations do not satisfy the target accountable history and current audit-column standard.

## 3. Module and Bounded-Context Boundaries

| Module | Responsibility | Owns | Depends on | Must not own |
|---|---|---|---|---|
| Queue Resource Catalog | Maintain queue resources and permitted relationships | Service Point, Loket, Kiosk aggregates and assignments | Authenticated administration context, business clock | Registration outcome, Patient identity, UI configuration |
| Queue Execution | Allocate and transition admission queue participation | Queue Session, Queue Entry, Queue Call, Call Attempt, Queue Label snapshot | Resource identities, sequencer, business clock | Coverage eligibility or Registration outcome |
| Queue Operational Projections | Provide Kiosk offerings, Loket worklists, display state, history, and recovery views | Read-only projections | Queue Resource Catalog and Queue Execution persistence | Lifecycle decisions or write authority |
| Queue Notification Delivery | Deliver committed queue changes to subscribed clients | Durable notification delivery state | Queue Execution facts and real-time transport | Queue state or display interpretation policy |
| Journey Association | Associate an anonymous entry with one applicable Patient Tracker | Queue Entry association and Tracker queue evidence within their aggregates | Patient Tracker repository, Journey Resolution application contracts | Canonical Patient master or Registration truth |
| Admisi Rajal Registration | Establish or reject outpatient Registration | Outpatient Registration and registration outcome | Patient Tracker application contracts | Queue numbering, Queue Call, Queue Label, or queue lifecycle authority |
| Kiosk client | Initiate intake and present the issued result | Client session and local presentation only | Bilreg application interface, local printer | Service Point authority, sequence generation, Queue Entry state |
| Queue Display client | Present committed display projection and announcements | Client presentation state only | Snapshot query and notification stream | Call selection, Queue Entry mutation, authoritative history |
| Admission Module client | Compose queue and Registration use cases for an officer | Client interaction state only | Bilreg application interfaces | Queue, Tracker, or Registration business authority |

Kiosk and Queue Display are actor-facing hosts, not new bounded contexts. They may be separate deployables or repositories later, but all authoritative changes flow through Patient Tracker Application use cases.

## 4. Clean Architecture Layer Responsibilities

| Layer | Feature responsibilities | Permitted dependencies | Prohibited dependencies |
|---|---|---|---|
| Domain — `Bilreg.Domain/AdmisiContext/AntrianFeature` | Service Point, Loket, Kiosk, Queue Session, Queue Entry, Queue Call, Call Attempt behavior; Queue Label formation; lifecycle invariants | Domain value objects and shared pure utilities | MediatR, Dapper, SQL, HTTP, SignalR, configuration, DALs |
| Application — `Bilreg.Application/AdmisiContext/AntrianFeature` | Commands, queries, orchestration, repository ports, authorization ports, transactions, notification-outbox port, cross-context contracts | Domain and application abstractions | SQL, Dapper, ASP.NET types, Hub contexts, database DTOs |
| Infrastructure — `Bilreg.Infrastructure/AdmisiContext/AntrianFeature` | Repository implementations, DTO mapping, Dapper DALs, projection DALs, conditional transition persistence, outbox storage and claiming | Application ports, Domain models, SQL and external SDKs | Business decisions, actor authorization policy, UI behavior |
| API — `Bilreg.Api` | Authentication, device/actor context mapping, transport adaptation, MediatR dispatch, SignalR hub transport, runtime registration | Application and Infrastructure composition | Aggregate mutation, queue sequencing, lifecycle decisions |
| External clients | Kiosk intake, Queue Display presentation, Admission Module composition | Published application interfaces | Direct database access, caller-generated authoritative identities or state |

Domain methods decide whether a transition is valid. Application handlers authorize and orchestrate. Repository implementations persist the decided transition with expected-state conditions. Projection DALs may query optimized joins without reconstructing aggregates but must never decide or perform lifecycle transitions.

## 5. Application Use Cases

| ID | Status | Use case | Kind | Purpose | Authority / initiator | Primary model | Dependencies | Transaction outcome |
|---|---|---|---|---|---|---|---|---|
| UC-AQO-001 | Target | Establish Admission Service Point | Command | Create one active Service Point | Queue Operations Administrator | Service Point | Service Point repo, actor context, clock, audit | Service Point and audit committed atomically |
| UC-AQO-002 | Target | Amend Admission Service Point | Command | Change its recognized name or future Queue Prefix | Queue Operations Administrator | Service Point | Service Point repo, actor context, clock, audit | New catalog state committed; snapshots unchanged |
| UC-AQO-003 | Target | Retire Admission Service Point | Command | Stop future intake without erasing history | Queue Operations Administrator | Service Point | Service Point repo, active-session check, audit | Retired state and audit committed |
| UC-AQO-004 | Target | Maintain Loket Authorizations | Command | Establish/retire Loket and its permitted Service Points | Queue Operations Administrator | Loket | Loket and Service Point repos, audit | Loket authorization state and audit committed |
| UC-AQO-005 | Target | Maintain Kiosk Offerings | Command | Establish/retire Kiosk and its offered Service Points | Queue Operations Administrator | Kiosk | Kiosk and Service Point repos, audit | Kiosk offering state and audit committed |
| UC-AQO-006 | Target | List Kiosk Service Offerings | Query | Return active choices authorized for one Kiosk | Authenticated Kiosk | Kiosk offering projection | Kiosk identity, projection DAL | Read-only result |
| UC-AQO-007 | Existing partial / Target replacement | Issue Admission Queue Entry | Command | Resolve offered Service Point, find/create session, allocate one stable Queue Label | Authenticated Kiosk | Queue Session | Kiosk projection, Service Point repo, queue repo, sequencer, clock | Intake request, entry, and local notification committed once |
| UC-AQO-008 | Target | Get Intake Attempt Result | Query | Recover the Queue Label for a retried or uncertain Kiosk attempt | Same authenticated Kiosk | Intake-result projection | Kiosk identity, request identity | Read-only result for reprint/recovery |
| UC-AQO-009 | Target | List Loket Service Points | Query | Return active Service Points permitted at one Loket | Admission Officer | Loket authorization projection | Actor/Loket context, projection DAL | Read-only result |
| UC-AQO-010 | Target | List Admission Worklist | Query | Return scoped Waiting, outstanding, and In Service entries | Admission Officer | Admission worklist projection | Actor/Loket context, projection DAL | Read-only paged result |
| UC-AQO-011 | Target | Call Admission Queue Entry | Command | Claim one Waiting entry for one authorized Loket | Admission Officer | Queue Session / Queue Call | queue repo, Loket auth query, actor context, clock, outbox | Call, first attempt, and notification committed atomically |
| UC-AQO-012 | Target | Recall Admission Queue Entry | Command | Add a Call Attempt to the outstanding call | Admission Officer | Queue Session / Queue Call | queue repo, actor context, clock, outbox | Attempt and notification committed atomically |
| UC-AQO-013 | Existing partial / Target split | Start Admission Queue Service | Command | Acknowledge the call and move Waiting to In Service | Admission Officer | Queue Session / Queue Entry | queue repo, actor/Loket context, clock, outbox | Call acknowledgement, entry transition, and notification committed atomically |
| UC-AQO-014 | Target | Conclude No-Show or Withdraw Entry | Command | End or retain waiting participation under approved disposition | Queue Operations Supervisor | Queue Session / Queue Call | queue repo, supervisor authorization, clock, audit, outbox | Disposition, audit, and notification committed atomically |
| UC-AQO-015 | Target | Transfer Admission Queue Entry | Command | Withdraw original entry and create linked replacement participation | Queue Operations Supervisor | Two Queue Sessions | source/target queue repos, Service Point repo, sequencer, audit, outbox | Original and replacement state committed in one local transaction |
| UC-AQO-016 | Target | Get Queue Display Snapshot | Query | Recover current and recent committed calls for an announcement scope | Authenticated Queue Display | Queue display projection | display identity/scope, projection DAL | Read-only snapshot |
| UC-AQO-017 | Target | Get Queue Call History | Query | Support investigation and approved operational review | Authorized officer/supervisor | Call-history projection | scoped projection DAL | Read-only paged history |
| UC-AQO-018 | Existing | Select Existing Journey for Admission Entry | Command | Associate anonymous InService entry with selected existing Tracker | Admission Officer | Queue Session and Patient Tracker | queue repo, Tracker repo, admission Service Point authority | CAS association and Tracker evidence committed atomically |
| UC-AQO-019 | Existing partial / Target boundary | Complete Queue through Registration | Integration Inbound | Reuse or establish the applicable Tracker and complete the same entry with Registration | Admisi Rajal Registration | Queue Session and Patient Tracker | Patient Tracker application contract, Registration authority, outbox | Registration, association, completion, evidence, and outbox commit locally or all roll back |
| UC-AQO-020 | Target | Dispatch Queue Notifications | Background Process | Deliver committed projection-change notifications to active display/worklist subscribers | Queue notification worker | Notification outbox | outbox repo, SignalR transport, clock | Delivery attempt recorded; queue truth unchanged |
| UC-AQO-021 | Target | List Queue Resource Catalog | Query | Support authorized catalog administration and selection | Queue Operations Administrator | Resource projections | scoped projection DALs | Read-only paged result |
| UC-AQO-022 | Existing compatibility / Target constrained | Complete Registration without Prior Intake | Command collaboration | Preserve legacy callers that have no admission Queue Entry | Admisi Rajal Registration | Queue Session / Queue Entry | server-resolved compatibility Service Point, queue repo | Identified create/serve/complete remains one local transaction |

Exact transport routes, payloads, UI views, and visible labels belong to later API and UI artifacts.

## 6. Use-Case Traceability

| Use case ID | Domain capabilities / rules | SOP references | Owning module | Authorization concern | Required tests |
|---|---|---|---|---|---|
| UC-AQO-001–003 | Admission Service Point Management; BR-AQO-001–002, 006–011 | §3.2–3.3 | Queue Resource Catalog | Queue administrator role | Aggregate lifecycle, prefix conflict, audit, persistence |
| UC-AQO-004 | Loket Service Authorization; BR-AQO-003–004, 006, 018, 020 | §3.8–3.9; §4.2 | Queue Resource Catalog | Administrator plus active resource scope | Authorization invariant, projection, audit |
| UC-AQO-005–008 | Admission Queue Intake; Queue Number Allocation and Labelling; BR-AQO-005, 007–016 | §4.1; §5.1–5.3 | Resource Catalog / Queue Execution | Device identity and offering scope | Idempotency, sequence concurrency, label snapshot, recovery |
| UC-AQO-009–010 | Loket Service Authorization; Queue Call Coordination | §4.2 | Queue Operational Projections | Officer may see only assigned/authorized scope | Query scoping, ordering, paging, stale-state behavior |
| UC-AQO-011–013 | Queue Call Coordination; BR-AQO-017–024 | §4.3–4.4; §5.5–5.6 | Queue Execution | Officer/Loket/Service Point contextual authorization | Domain transitions, CAS conflicts, outbox atomicity |
| UC-AQO-014–015 | Admission Queue Exception Resolution; BR-AQO-026–030 | §5.6–5.8 | Queue Execution | Supervisor decision and reason | No-show alternatives, transfer rollback, audit, concurrency |
| UC-AQO-016–017, 020 | Queue Call Coordination; BR-AQO-028–030 | §4.3–4.4; §5.8; §6.2, §6.7 | Projections / Notification Delivery | Display scope or authorized operational history | Snapshot recovery, duplicate delivery, reconnect, data scoping |
| UC-AQO-018 | BR-AQO-013–016 | §4.5 steps 24–29; §5.9–5.10 | Journey Association | Authenticated Admission Officer | Existing CAS success/conflict and no losing evidence |
| UC-AQO-019 | BR-AQO-014–016, 023–025 | §4.5–4.6; §5.11–5.12 | Journey Association / Admisi Rajal | Officer authorized for current InService entry | Walk-In branches, Booking reuse, rollback across all local writes |
| UC-AQO-021 | Admission Service Point Management; Loket/Kiosk administration | §3.2–3.9 | Queue Resource Catalog | Administrator-scoped query | Projection correctness and sensitive-field exclusion |
| UC-AQO-022 | Compatibility constraint; parent BR-TRK-025, 029 | Not primary SOP path | Queue Execution / Admisi Rajal | Authenticated registration workflow only | Regression of no-kiosk registration behavior |

## 7. Aggregate and Domain Model Realization

### 7.1 Service Point Aggregate

- **Existing:** `ServicePointType` is a two-field value snapshot; `ServicePointStatusEnum` is unused; no aggregate or repository implementation exists.
- **Target root:** `ServicePointModel`, following repository naming standards.
- **Target value objects:** Service Point identity/reference and Queue Prefix value.
- **Protected rules:** BR-AQO-001–002, 006–011.
- **Consistency boundary:** stable identity, current name, prefix for future sessions, Active/Retired lifecycle.
- **Mutations:** establish, rename, change future prefix, retire. Historical session snapshots are never mutated.
- **Prohibited:** queue allocation, Loket assignment ownership, coverage eligibility decisions.

### 7.2 Loket Aggregate

- **Existing:** absent.
- **Target root:** `LoketModel`.
- **Owned children:** `LoketServiceAuthorizationModel` records.
- **Protected rules:** BR-AQO-003–004, 006, 018, 020.
- **Consistency boundary:** stable Loket identity, active state, authorized Service Point identities.
- **References:** Service Points by identity only; resource existence is checked by Application before mutation.
- **Prohibited:** owning Queue Calls or deciding which Queue Entry is next.

### 7.3 Kiosk Aggregate

- **Existing:** absent.
- **Target root:** `KioskModel`.
- **Owned children:** `KioskServiceOfferingModel` records.
- **Protected rules:** BR-AQO-005–006.
- **Consistency boundary:** stable Kiosk identity, active state, offered Service Point identities.
- **Prohibited:** queue-number sequencing and client-supplied Service Point authority.

### 7.4 Queue Session Aggregate

- **Existing root:** `AntrianModel` with `AntrianEntryModel` children.
- **Target extensions:** immutable Queue Prefix Snapshot on an admission session; Queue Label behavior; `Withdrawn` entry transition; `QueueCallModel` and append-only `CallAttemptModel` children; link between transferred entries.
- **Protected rules:** BR-AQO-007–030 plus parent BR-TRK-026–039a.
- **Consistency boundary:** Queue Number uniqueness, label stability, entry lifecycle, singular Tracker association, one outstanding call per entry, call disposition, and destination Loket identity.
- **Creation/mutation entry points:** issue entry, call, recall, acknowledge/start, complete, withdraw, and transfer orchestration.
- **Events/facts:** stable domain facts listed in Domain §9 may drive local projection notifications; they do not require a generic event bus.
- **References:** ServicePointId, LoketId, KioskId/intake request provenance, optional TrackerId, and transfer counterpart identity.
- **Prohibited:** Registration outcome, Patient master mutation, eligibility decisions, SignalR delivery.

The aggregate may contain many entries. Operational queries must use projections. Lifecycle persistence may use repository-owned expected-state operations for one entry/call rather than rewriting every child, provided the Domain model first authorizes the transition and the conditional persistence protects the same invariant.

### 7.5 Patient Tracker Aggregate

`PasienTrackerModel` remains the existing root for Patient Journey evidence. Queue issuance and calling do not create it. UC-AQO-018 and UC-AQO-019 append evidence only after accountable association or Registration-owned creation. It must not absorb Queue Call or resource-catalog behavior.

### 7.6 Cross-aggregate orchestration

- Transfer coordinates two Queue Session aggregates in Application and uses one local SQL transaction because original withdrawal and replacement creation are one operational outcome.
- Registration completion coordinates Admisi Rajal Registration, Patient Tracker, Queue Session, billing/outbox behavior already present in the registration use case. Each authority remains explicit even when the same local transaction provides atomicity.
- Resource validation reads Service Point/Loket/Kiosk authority before mutating Queue Session; Queue Session stores stable identity and snapshots rather than owning those aggregates.

## 8. Persistence and Repository Strategy

### 8.1 Existing persistence

- `BILRG_Antrian` stores Queue Session identity, date/time, sequence tag, description, and Service Point code.
- `BILRG_AntrianEntry` stores `(AntrianId, NoUrut)`, anonymous/Tracker association, status, timestamps, and references.
- `AntrianRepo` reconstructs the aggregate and currently performs collection comparison for saves.
- `AntrianEntryDal.UpdateFromAnonymousInService` is the existing conditional transition anchor.
- `ISequencer` provides the next number using a sequence tag.

### 8.2 Target repositories

Application owns one repository contract per aggregate root:

- `IServicePointRepo`
- `ILoketRepo`
- `IKioskRepo`
- extended `IAntrianRepo`
- existing `IPasienTrackerRepo`

Infrastructure owns repository implementations, DTOs, DAL contracts/implementations, and explicit SQL. The unused Application `IServicePointDal` must not become the target contract.

### 8.3 Target persistence additions

Use additive, module-prefixed SQL artifacts for:

- Service Point aggregate state;
- Loket and its Service Point authorizations;
- Kiosk and its Service Point offerings;
- Queue Call and append-only Call Attempt history;
- intake request idempotency;
- transfer linkage;
- durable queue-notification outbox.

Extend `BILRG_Antrian` or its owned snapshot persistence with Queue Prefix Snapshot. Extend `BILRG_AntrianEntry` for `Withdrawn` and any stable provenance needed for intake/transfer. Do not derive historical Queue Labels from the current Service Point record.

Detailed columns and indexes belong to implementation persistence artifacts, but migrations must follow `docs/DATABASE.md`: explicit SQL, no workflow triggers, non-null defaults where appropriate, audit columns on transaction tables, append/status history, and operational indexes.

### 8.4 Concurrency

Whole-aggregate last-writer-wins updates are insufficient for active queues. Target repository operations must return affected-row counts and require exactly one expected transition:

- available Waiting → outstanding Call created;
- outstanding Call → another Call Attempt;
- Waiting with matching outstanding Call → InService and Call acknowledged;
- Waiting → Withdrawn;
- InService → Done;
- anonymous InService → identified InService or Done (existing pattern).

Zero affected rows is an explicit conflict. Do not automatically select another entry, Loket, or Tracker. The caller refreshes the relevant projection.

Database uniqueness must support, not replace, domain decisions: unique intake request identity per Kiosk, unique Queue Number per session, at most one outstanding call per entry, and at most one active entry per Loket unless later approved policy says otherwise.

### 8.5 Time and identity

- Use `ITglJamProvider` for business occurrence times.
- Use application-generated opaque identities consistent with the current ULID practice.
- Derive human actor identity through `ICurrentUserContext`; do not trust request `UserId` for new commands.
- Derive Kiosk and Queue Display identities from authenticated service/device context.

### 8.6 Compatibility

- Backfill a repository Service Point for the configured `ADM / Loket Admisi` value before switching authority.
- Backfill or deterministically preserve existing session descriptions and Service Point codes; do not fabricate historical prefixes without an approved migration value.
- Keep current physician queues and compatibility adapter untouched.
- Gate new admission behavior separately from physician queues sharing `AntrianModel`.

## 9. Read Models and Query Strategy

| Projection | Consumer purpose | Source authority | Freshness | Filters / scope | Must not decide |
|---|---|---|---|---|---|
| Kiosk Service Offering View | Show active Service Point choices | Kiosk and Service Point aggregates | Request-time current | Authenticated Kiosk, active offerings | Eligibility, queue allocation, prefix authority |
| Intake Attempt Result View | Recover/reprint one issued result | Queue Session/Entry and intake identity | Request-time current | Same Kiosk and request identity | Whether to allocate a second entry |
| Loket Authorization View | Show Service Points available at a Loket | Loket and Service Point aggregates | Request-time current | Authenticated officer and assigned Loket | Authorization mutation |
| Admission Worklist View | Scan Waiting, outstanding, and InService work | Queue Session/Entry/Call | Near-real-time; query is recovery truth | Session date, ServicePointId, Loket scope, state, paging | Claim, call, start, complete, identify |
| Queue Display Snapshot View | Recover current/recent display state | Queue Call, Call Attempt, Entry, Loket, Service Point | Near-real-time plus reconnect query | Authenticated display announcement scope | Which entry to call or whether service started |
| Queue Call History View | Investigate calls, recalls, dispositions | Queue Call and attempts | Request-time current | Date, Service Point, Loket, Queue Label, authorized scope | Correct or mutate history |
| Queue Resource Catalog View | Administer/search active and retired resources | Resource aggregates | Request-time current | Resource type, status, location/scope when defined | Lifecycle decisions |
| Notification Recovery View | Operate delivery retries and reconciliation | Notification outbox | Operationally current | status, age, retry eligibility | Queue state or domain outcome |

Projection DALs should use stable SQL shapes and indexes optimized for queue scanning. They may denormalize names and Queue Labels for display but must retain source identities and must never become a second write model.

## 10. Application Interfaces and API Philosophy

- Application commands and queries remain transport-independent MediatR requests or equivalent application contracts.
- HTTP controllers and the real-time hub adapt authentication and transport only.
- Exact routes, request/response fields, UI-specific view models, and labels are deferred to an API contract after UI design.
- Commands return deterministic success identities and expected error categories: validation, not found, forbidden, conflict, and unavailable dependency.
- Conflict remains HTTP 409 at HTTP transport; authorization distinguishes unauthenticated from forbidden access.
- Kiosk intake requires a client-generated request identity scoped to an authenticated Kiosk. Repetition returns the original outcome rather than another Queue Number.
- Call, recall, start, withdraw, transfer, identify, and complete commands require an expected current identity/state or repository equivalent and never silently retry a different business choice.
- Worklist, history, and catalog queries require paging and server-side filters appropriate to their operational volume.
- Queue Display first requests a snapshot and then subscribes to scoped notifications. A notification is a refresh hint plus committed display data; it is not write authority.
- Application contracts must preserve caller compatibility during migration. The current direct admission `start` capability is deprecated for new clients once separate Call and Start Service are available.
- Controllers must not read another context's tables, construct Queue Labels, or choose fallback Service Points.

## 11. Integration and Cross-Context Collaboration

| Collaborator | Direction | Purpose | Owning authority | Contract style | Consistency / delivery | Failure handling |
|---|---|---|---|---|---|---|
| Kiosk | Inbound to Patient Tracker | Query offerings and issue/recover Queue Entry | Patient Tracker | Authenticated application command/query | Synchronous; intake idempotent | Return existing result on retry; no offline number allocation |
| Queue Ticket Printer | Local from Kiosk | Print the already issued Queue Label | Patient Tracker owns label; Kiosk owns presentation attempt | Client-local device integration | Not atomic with queue commit | Reprint same intake result; never allocate another number |
| Admission Module | Inbound to Patient Tracker and Admisi Rajal | Worklist, call, service, Journey Resolution, Registration | Respective owning context | Authenticated commands/queries | Synchronous local transactions | Explicit validation/conflict; refresh projections |
| Queue Display | Query inbound; notification outbound | Recover and present committed calls | Patient Tracker | Snapshot query plus SignalR target transport | At-least-once notifications; snapshot recovery | Deduplicate by notification/call-attempt identity; reconnect and reload |
| Admisi Rajal Registration | In-process collaboration | Establish Registration and coordinate queue completion | Admisi Rajal for Registration; Patient Tracker for queue/Tracker | Application contract, not table/DAL access | Same local SQL transaction where current registration orchestration already requires atomicity | Roll back local writes; return accountable registration result |
| Patient master context | Indirect through Registration/Journey Resolution | Supply canonical identity evidence | Patient context | Existing application/domain contracts | No new queue-owned mutation | Keep entry InService until accountable resolution outcome |
| Authentication authority | Inbound identity | Authenticate officers and device/service clients | Identity authority | JWT/service credential validation | Per connection/request | Reject unauthenticated or invalid scope |
| Queue notification transport | Outbound | Push committed projection changes | Patient Tracker notification delivery | SignalR adapter behind Application port | Durable outbox to at-least-once transport | Retry with backoff; clients recover snapshot |
| Existing EMR queue integration | Existing outbound, unchanged | Physician/registration queue publication | Existing integration feature | Existing durable outbound queue | Existing behavior | Must not be reused as display notification authority |

Do not introduce a generic cross-context event bus solely because the domain lists events. Use direct orchestration for local consistency and a dedicated durable notification outbox for client delivery.

## 12. Authentication, Authorization, and Audit

### 12.1 Authentication

- Admission Officers and supervisors use the existing JWT authentication path.
- New human commands derive the actor from `ICurrentUserContext` rather than accepting authoritative `UserId` input.
- Kiosks and Queue Displays require non-human service/device identities distinct from officer identities.
- A long-lived display connection must be authenticated at connection establishment and revalidated on reconnect.

### 12.2 Contextual authorization

- Kiosk commands are scoped to that Kiosk's active offerings.
- Queue Display queries/subscriptions are scoped to the display's approved announcement scope.
- Admission Officer queries and commands are scoped to the assigned Loket and its active Service Point authorizations.
- Supervisor-only commands cover final no-show disposition, transfer approval, and exceptional overrides.
- Resource-catalog commands require Queue Operations Administrator authority.
- UI visibility is not authorization. Application handlers enforce scope before mutation or data return.

Current `[Authorize]` without contextual policies is a gap. Current caller-provided `UserId` must not be carried into new target commands as authority.

### 12.3 Audit and operational history

- Queue Call and Call Attempt records are operational queue truth, not substitutes for compliance audit.
- Use `IAuditRepo` for Service Point/Loket/Kiosk administration, authorization changes, no-show disposition, transfer, override, and other material exceptional decisions.
- Audit records include authenticated actor, occurrence time, reason where required, affected identity, and before/after snapshot where useful.
- Do not put Patient demographics or Booking QR contents into notification payloads, display logs, or general structured logs.
- Kiosk and display clients receive the minimum data needed for their function.

## 13. Transactions, Consistency, Concurrency, and Idempotency

| Use case | Local transaction boundary | Synchronous invariants | Eventual facts | Duplicate key / retry | Conflict and recovery |
|---|---|---|---|---|---|
| Issue entry | Intake identity, session create/load, sequence allocation, entry insert, local notification | Offering active, one result per request, number unique, prefix snapshot stable | Worklist refresh notification | KioskId + intake request identity | Return committed result; retry same request only |
| Call | Conditional entry claim, Queue Call, first attempt, outbox | Waiting, authorized Loket, no competing outstanding call/active Loket work | Display/worklist notification | Command/request identity plus entry identity | 409; refresh worklist |
| Recall | Conditional outstanding-call check, Call Attempt, outbox | Same call still outstanding, same authorized Loket | Display notification | Recall request identity | Return prior attempt on duplicate; 409 on concluded call |
| Start service | Conditional Waiting transition, call acknowledgement, outbox | Matching outstanding call and Loket, ServedAt ordering | Display/worklist notification | Start request identity | 409; refresh entry/call |
| Withdraw/no-show | Conditional Waiting transition or retained Waiting disposition, call conclusion, audit, outbox | Supervisor authority, no service start when withdrawing | Display/worklist notification | Disposition request identity | 409; supervisor reviews current state |
| Transfer | Source withdrawal, target session/entry allocation, transfer link, audit, outbox | Source Waiting, target active, one linked replacement | Two scoped projection notifications | Transfer request identity | Roll back all local writes; no partial replacement |
| Identify existing Tracker | Existing CAS entry association plus Tracker evidence | Anonymous InService and one selected Tracker | Optional worklist refresh | Entry identity + expected anonymous status | Existing 409; no losing Tracker evidence |
| Registration completion | Existing Registration transaction extended with conditional queue transition and local outbox | Registration outcome, Tracker association, entry completion | Display/worklist notifications and existing integrations | Registration/source identities | Roll back Registration, Tracker, queue, billing, local outbox on conflict |
| Dispatch notification | Claim outbox row, send, record result | One worker owns an attempt | SignalR delivery is at least once | NotificationId | Retry; dead-letter/failed state remains operable |

No transaction includes a Kiosk printer, browser, Queue Display, audio device, or remote client. Once queue intake commits, a print failure is recovered by retrieving the same result. Once a call commits, display delivery failure is recovered from the durable outbox and display snapshot.

If multiple API/worker instances are deployed, outbox claiming and active-call uniqueness must remain database-coordinated. SignalR scale-out requirements are an open deployment decision.

## 14. Infrastructure and Operational Concerns

### 14.1 Database and migrations

- Ship additive scripts under `Bilreg.SqlDb/AdmisiContext/AntrianFeature` in dependency order.
- Seed/backfill the compatibility admission Service Point before enabling repository authority.
- Validate duplicate active prefixes within the approved announcement scope before enforcing uniqueness.
- Preserve legacy rows and sentinels; do not physically delete historical queue participation.

### 14.2 Real-time delivery

- Add ASP.NET Core SignalR as the target transport adapter; no SignalR support exists today.
- Group subscribers by authenticated announcement scope, not by caller-supplied arbitrary group name.
- Persist a dedicated queue-notification outbox in the same transaction as queue mutation.
- Add a hosted/background processor that claims pending notifications, broadcasts them, and records transport-dispatch attempts.
- Clients deduplicate using stable notification and Call Attempt identities and reload the display snapshot after reconnect.
- Polling the snapshot may be a temporary fallback, but it must not become write authority.

A successful SignalR broadcast means the server transport accepted the dispatch; it does not prove that a particular Queue Display rendered the call or played audio. The authoritative recovery mechanism remains the persisted projection and snapshot query.

### 14.3 Observability

Structured logs and metrics should include correlation/request identity, Kiosk/Loket/display identity, ServicePointId, AntrianId, NoUrut, CallId, transition result, conflict category, and notification delivery result. Do not log unnecessary Patient evidence.

Minimum operational metrics:

- queue intake successes, duplicates, and failures;
- sequence conflicts;
- calls, recalls, starts, completions, withdrawals, and transfers;
- CAS conflicts by transition;
- outstanding calls and age;
- notification outbox backlog, dispatch retry count, and oldest age;
- active display connections and snapshot-recovery failures.

### 14.4 Configuration and rollout

- Feature-gate repository-backed admission resources, split Call/Start behavior, and real-time notification independently from physician queues.
- Configuration may bootstrap default identities but must not remain runtime Service Point authority.
- Health checks should cover database access, outbox backlog, and real-time endpoint readiness without exposing sensitive data.
- Offline Kiosk number allocation is prohibited; a disconnected Kiosk cannot safely allocate authoritative numbers.

## 15. Implementation Guidance for AI Agents

| Increment | Included use cases | Required layers | External dependencies | Verification gate |
|---|---|---|---|---|
| 1. Resource authority | UC-AQO-001–006, 009, 021 | Domain, Application, Infrastructure, API | Existing auth/audit | Aggregate tests; repo/DAL integration; authorization/query tests; seed migration verification |
| 2. Idempotent labelled intake | UC-AQO-007–008 | Domain, Application, Infrastructure, API | `ISequencer`, authenticated Kiosk context | Concurrent intake tests; duplicate request returns same label; prefix snapshot; existing anonymous intake regression |
| 3. Operational projections | UC-AQO-010, 016–017 | Application, Infrastructure, API | Resource and queue persistence | Worklist/display/history query tests for scope, ordering, state, and volume |
| 4. Call and service transitions | UC-AQO-011–013 | Domain, Application, Infrastructure, API | Officer/Loket authorization | Domain lifecycle tests; CAS integration tests; competing calls; one-active-entry constraint; old direct-start compatibility test |
| 5. Durable display delivery | UC-AQO-020 plus notification production | Application, Infrastructure, API | SignalR and hosted worker | Transactional outbox test; duplicate delivery; reconnect snapshot; worker retry/recovery |
| 6. Journey and Registration alignment | UC-AQO-018–019, 022 | Application boundary refactor, existing Domain/Infrastructure/API | Admisi Rajal, Patient Tracker, existing billing/outbox | Booking reuse; both Walk-In branches; complete rollback suite; no-kiosk regression |
| 7. Exceptions | UC-AQO-014–015 | Domain, Application, Infrastructure, API | Supervisor authorization/audit | Recall/no-show/withdraw/transfer domain tests; atomic transfer rollback; audit tests |
| 8. Hardening and rollout | All | All layers | Kiosk, Queue Display, Admission Module contract tests | End-to-end contract tests, load/concurrency tests, observability and feature-gate verification |

Implementation agents must:

- preserve Domain → Application → Infrastructure/API dependency direction;
- implement behavior in models and orchestration in use cases;
- use repositories rather than Application-layer DALs;
- add purpose-built projection DALs only for reads;
- preserve physician queue and compatibility-adapter behavior;
- retain Registration/Tracker/queue rollback guarantees;
- distinguish committed queue truth from eventual client notification;
- classify every new class/table/transport as target until implemented and tested.

Implementation agents must not:

- let Kiosk, Queue Display, or Admission Module access queue tables directly;
- trust client Service Point name, Queue Prefix, Queue Label, Loket assignment, or UserId as authority;
- make SignalR delivery part of the queue transaction;
- mark service started merely because a call was displayed;
- create a Tracker when issuing or calling a Queue Number;
- reuse the EMR outbound queue as a display transport;
- put business decisions in controllers, DALs, projections, hubs, or background workers;
- delete historical Queue Entries, Queue Calls, attempts, or audit records;
- invent no-show thresholds, announcement scopes, or device-provisioning rules left open below.

## 16. Architectural Decisions

### ADR-AQO-001 — Persist queue resources under Patient Tracker authority

- **Decision:** Realize Service Point, Loket, and Kiosk as repository-backed Patient Tracker aggregates.
- **Status:** Accepted Target.
- **Context:** Configuration and caller-supplied snapshots cannot enforce stable identities, prefix history, or assignments.
- **Rationale:** The domain assigns these identities and queue relationships to Patient Tracker.
- **Consequences:** Add repositories and migrations; configuration becomes bootstrap/compatibility only.
- **Rejected alternatives:** Multiple configured Service Points as runtime authority; client-supplied Service Point catalog.
- **Evidence or governing references:** Domain §§5–7; current `ServicePointType` and `AdmisiRajalOptions` gaps.

### ADR-AQO-002 — Keep Kiosk and Queue Display as external clients

- **Decision:** Kiosk and Queue Display consume Bilreg application interfaces and do not become bounded-context authorities.
- **Status:** Accepted Target.
- **Context:** Neither client exists; both present Patient Tracker truth.
- **Rationale:** Central authority prevents split sequencing and lifecycle decisions.
- **Consequences:** Clients require authenticated contracts and cannot operate authoritative queues offline.
- **Rejected alternatives:** Local Kiosk database/sequence; Queue Display-owned current-call state.
- **Evidence or governing references:** Domain boundaries; SOP Actors and Responsibilities.

### ADR-AQO-003 — Model Queue Call separately from service state

- **Decision:** Queue Call and Call Attempt are Queue Session-owned records while Queue Entry remains Waiting until service actually starts.
- **Status:** Accepted Target.
- **Context:** Current `AdmissionQueueStartCmd` conflates call with service start.
- **Rationale:** Preserves correct ServedAt, waiting time, recall, and no-show evidence.
- **Consequences:** Split new Call and Start Service use cases; migrate new clients away from direct start semantics.
- **Rejected alternatives:** Add `Called` as the Queue Entry service state; treat display delivery as ServedAt.
- **Evidence or governing references:** BR-AQO-017–024; SOP §§4.3–4.4.

### ADR-AQO-004 — Snapshot Queue Prefix at Queue Session establishment

- **Decision:** Resolve Queue Prefix from the Service Point Aggregate and preserve it on the admission Queue Session; derive the Queue Label from that snapshot and NoUrut.
- **Status:** Accepted Target.
- **Context:** Historical labels must survive later catalog changes.
- **Rationale:** One immutable session snapshot avoids unnecessary duplication while retaining deterministic labels.
- **Consequences:** Migration adds prefix snapshot; existing sessions need an approved backfill strategy.
- **Rejected alternatives:** Resolve prefix from current Service Point on every read; encode prefix into `SequenceTag` as authority.
- **Evidence or governing references:** BR-AQO-007–012.

### ADR-AQO-005 — Use expected-state persistence for active queue transitions

- **Decision:** Persist call and lifecycle transitions with conditional affected-row operations instead of unrestricted whole-entry updates.
- **Status:** Accepted Target.
- **Context:** Multiple officers and clients act concurrently on shared worklists.
- **Rationale:** The existing anonymous association CAS proves the repository pattern and conflict behavior.
- **Consequences:** Extend repository ports and return HTTP 409 through transport adapters on zero affected rows.
- **Rejected alternatives:** Automatic retry with another business choice; application-memory locks; last-writer-wins.
- **Evidence or governing references:** `TrySaveAnonymousInServiceTransition`; BR-AQO-016–024.

### ADR-AQO-006 — Use durable notification outbox plus snapshot recovery

- **Decision:** Commit a dedicated notification record with queue mutations, deliver it asynchronously over SignalR, and require snapshot recovery on connect/reconnect.
- **Status:** Proposed; accepted in principle, pending deployment topology decision GAP-AQO-005.
- **Context:** Queue Display requires near-real-time updates but does not yet exist, and transient delivery cannot be authoritative.
- **Rationale:** Separates queue consistency from client availability and reuses proven durable outbound patterns.
- **Consequences:** Add outbox persistence, worker, hub adapter, idempotent client handling, snapshot recovery, and operational monitoring; transport dispatch success is not client-render acknowledgement.
- **Rejected alternatives:** SignalR send inside the command transaction; database polling as the only permanent mechanism; reuse EMR outbound queue.
- **Evidence or governing references:** SOP §§4.3, 5.8; existing EMR/Lab outbound queues; absence of SignalR.

### ADR-AQO-007 — Authenticate and scope device clients independently

- **Decision:** Kiosk and Queue Display use authenticated service/device identities; human actors use JWT claims and contextual Loket authorization.
- **Status:** Accepted Target; credential provisioning remains open.
- **Context:** Current generic authorization and caller `UserId` cannot protect physical resource assignments.
- **Rationale:** Server-side identity is required to authorize offerings, announcement scope, and accountable actions.
- **Consequences:** Add client identity context and policies; remove caller identity authority from new commands.
- **Rejected alternatives:** Trust KioskId/LoketId/UserId in request bodies; authorize only by hidden UI controls.
- **Evidence or governing references:** Current `HttpCurrentUserContext`; SOP Preconditions; BR-AQO-004–005, 018.

### ADR-AQO-008 — Keep printing outside the backend transaction

- **Decision:** Backend commits and returns the authoritative Queue Label; Kiosk owns printing and recovers by retrieving the same intake result.
- **Status:** Accepted Target.
- **Context:** Printer failure cannot roll back a safely allocated queue number after an uncertain network outcome.
- **Rationale:** Idempotent recovery avoids duplicate numbers and distributed transactions with devices.
- **Consequences:** Kiosk must retain request identity until the result is confirmed and support same-label reprint.
- **Rejected alternatives:** Allocate only after printer acknowledgement; allow Kiosk to choose a replacement number.
- **Evidence or governing references:** SOP §§4.1, 5.2–5.3; BR-AQO-012.

## 17. Open Gaps and Deferred Decisions

| ID | Gap or decision | Why it matters | Owner / authority needed | Blocks | Safe interim position |
|---|---|---|---|---|---|
| GAP-AQO-001 | Exact no-show threshold and whether an entry returns to Waiting or becomes Withdrawn | Determines supervisor action, timers, projections, and tests | Hospital Queue Operations policy owner | Final no-show UI/API and automation | Require explicit supervisor disposition; do not automate a threshold |
| GAP-AQO-002 | Definition of Patient announcement scope/location and prefix uniqueness scope | Determines display subscriptions and whether prefixes may repeat across buildings | Hospital operations and facility/domain owner | Catalog validation, display grouping, multi-building rollout | Treat all active admission Service Points as one scope and require globally unambiguous prefixes |
| GAP-AQO-003 | Trusted Admission workstation-to-Loket assignment mechanism | Browser-local selection alone is not authoritative | Security/operations owner | Contextual officer authorization | Require supervisor-managed server assignment; do not trust arbitrary LoketId |
| GAP-AQO-004 | Kiosk and Queue Display credential provisioning/rotation | Device identity is required for offering and display scoping | Security/platform owner | Production device authentication | No anonymous device commands or subscriptions |
| GAP-AQO-005 | API instance topology and SignalR scale-out/backplane | Multi-instance delivery and connection routing affect hub and worker design | Deployment/platform owner | Final real-time infrastructure | Implement snapshot recovery and durable outbox; do not assume in-memory delivery is sufficient across instances |
| GAP-AQO-006 | Kiosk and Queue Display client technology, repository, and hosting | Determines later UI and deployment artifacts, not backend authority | Product/platform owner | Client implementation | Keep backend contracts transport/client agnostic |
| GAP-AQO-007 | Queue Call, Call Attempt, outbox, and audit retention periods | Affects storage volume and authorized history | Compliance and hospital operations | Retention jobs and capacity sizing | Retain records; do not delete automatically |
| GAP-AQO-008 | Exact API contract and visible UI labels beyond `Call` | Required for client implementation and contract tests | Product/UI/API owners | UI delivery and API endpoint implementation | Implement Application contracts first; do not invent routes or labels in architecture |
| GAP-AQO-009 | Operational evidence used to offer or transfer BPJS versus General service | Queue does not own eligibility truth | Admisi Rajal/guarantor policy owner | Automated routing suggestions | Let Patient choose an offered Service Point and require accountable transfer when wrong |
| GAP-AQO-010 | Prefix backfill for historical and currently active admission sessions | Historical rows have no authoritative prefix | Data migration and operations owner | Enabling Queue Label projection for legacy sessions | Preserve raw NoUrut and mark prefix migration unresolved; do not infer from arbitrary descriptions |
| GAP-AQO-011 | Deprecation date and consumers of current direct admission `start` | New Call/Start split cannot safely replace unknown clients immediately | API/product owner | Removal or semantic change of existing capability | Keep compatibility endpoint feature-gated; new clients use split commands |
| GAP-AQO-012 | Queue Display retention semantics after service starts | Determines whether display shows outstanding call only, current InService item, or recent calls | Hospital Queue Operations policy owner | Final display projection/UI contract | Persist all call facts; expose a recoverable snapshot without declaring final visual retention policy |
