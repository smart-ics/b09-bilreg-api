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

Major current constraints are the existing `BILRG_Antrian` / `BILRG_AntrianEntry` persistence shape, Dapper DALs, ambient `TransHelper` transactions, legacy `ISequencer`, caller-supplied `UserId`, generic `[Authorize]`, and whole-aggregate queue saves. Major V1 gaps are hardening the legacy sequence to the 1–9999 admission range, Service Point catalog, Queue Prefix/Label, current call state/CallCount, trusted workstation configuration, shared current display state/version, SignalR/polling transport, audit coverage, Priority redirection, and conditional intake idempotency.

## 2. Codebase Evidence and Constraints

| Evidence | Verified location | Architectural implication |
|---|---|---|
| Four Clean Architecture projects and inward project references | `src/bilreg/Bilreg.Domain`, `Bilreg.Application`, `Bilreg.Infrastructure`, `Bilreg.Api` project files | New behavior must follow the existing project boundaries; clients must not bypass Application. |
| Queue Session aggregate and entries exist | `Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs`, `AntrianEntryModel.cs` | Extend the existing aggregate instead of creating a competing admission queue model. |
| Queue Entry states are only `Waiting`, `InService`, and `Done` | `AntrianStatusEnum.cs` | `Withdrawn` and Queue Call disposition are target domain-realization gaps. |
| Service Point is only a two-field value snapshot | `ServicePointType.cs` | It cannot satisfy the new Service Point Aggregate, Queue Prefix, lifecycle, or catalog queries. |
| Unused Service Point persistence abstractions exist without an implementation | `ServicePointStatusEnum.cs`, Application-layer `IServicePointDal.cs` | Do not treat Service Point persistence as existing. Replace the Application DAL contract with an aggregate repository; keep DAL contracts and SQL mapping in Infrastructure. |
| Anonymous intake trusts caller-supplied Service Point code/name and permits optional `TglYmd` | `QueAnonymousIntakeCmd.cs` | Target intake resolves Business Date server-side and validates submitted ServicePointId against active Service Point persistence; local Kiosk offerings are presentation only. |
| One configured admission Service Point is authoritative | `AdmisiRajalOptions.cs`, `DomainService.cs`, API settings | Configuration is a transitional constraint. Target authority is the Service Point repository; configuration may only seed or select a compatibility default. |
| Admission `start` immediately serves an anonymous entry | `AdmissionQueueStartCmd.cs` | The current command conflates call and service start and carries no Loket. New clients must use separate Call and Start Service use cases. |
| Queue identity and entry persistence exist | `AntrianDto.cs`, `AntrianEntryDto.cs`, `AntrianDal.cs`, `AntrianEntryDal.cs` | Existing tables require additive migration and compatibility mapping, not replacement. |
| Existing queue transaction tables do not carry the full audit-column set required by `docs/DATABASE.md` | `BILRG_Antrian.sql`, `BILRG_AntrianEntry.sql` | Migration design must add accountable mutation evidence or provide the approved equivalent; new transaction tables must follow the current standard. |
| Queue header uniqueness uses `SequenceTag` | `BILRG_Antrian_M1_ServicePointCode_Alter.sql` | Queue Session lookup remains deterministic, but Queue Prefix must not be inferred from `SequenceTag`. |
| Queue numbers currently use `ISequencer` and `SequenceTag` | `AntrianModel.AddEntry`, `AntrianFactory.Create(ServicePointType, DateOnly)`, `Sequencer` | Target V1 retains this legacy authority, constrains each admission sequence to 1–9999 with no cycle, and rejects exhaustion. |
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
| Queue Resource Catalog | Maintain managed queue resources | Service Point only in Pragmatic V1 | Authenticated administration context, business clock | Loket, Kiosk, Queue Display, Announcement Scope, Registration outcome, Patient identity |
| Queue Execution | Allocate and transition admission queue participation | Dedicated Queue Session with canonical SequenceTag; legacy `ISequencer`; Queue Entry with current call state, CallCount, Priority, CreationReason, and source-entry provenance | Service Point identity, workstation-configured LoketKey, business clock | Coverage eligibility or Registration outcome |
| Queue Operational Projections | Provide queue-only worklists, shared current-per-Loket display state, and recovery views | Read-only projections and current display state | Service Point and Queue Execution persistence | Lifecycle decisions, write authority, or Admisi-owned enrichment |
| Queue Notification Delivery | Publish best-effort refresh hints after commit | No durable delivery state in V1 | Current display state and SignalR transport | Queue truth, retries, or delivery guarantees |
| Journey Association | Associate an anonymous entry with one applicable Patient Tracker | Queue Entry association and Tracker queue evidence within their aggregates | Patient Tracker repository, Journey Resolution application contracts | Canonical Patient master or Registration truth |
| Admisi Rajal Registration | Establish Outpatient Registration or record an explicit final NotEstablished decision | Outpatient Registration and Registration Outcome aggregate | Patient Tracker application contracts | Queue numbering, Queue Call, Queue Label, or queue lifecycle authority |
| Kiosk client | Initiate intake and present the issued result | Client session and local presentation only | Bilreg application interface, local printer | Service Point authority, sequence generation, Queue Entry state |
| Queue Display client | Present committed display projection and announcements | Client presentation state only | Snapshot query and notification stream | Call selection, Queue Entry mutation, authoritative history |
| Admission Module client | Compose queue and Registration use cases for an officer | Client interaction state only | Bilreg application interfaces | Queue, Tracker, or Registration business authority |

Kiosk and Queue Display are actor-facing hosts, not new bounded contexts. They may be separate deployables or repositories later, but all authoritative changes flow through Patient Tracker Application use cases.

## 4. Clean Architecture Layer Responsibilities

| Layer | Feature responsibilities | Permitted dependencies | Prohibited dependencies |
|---|---|---|---|
| Domain — `Bilreg.Domain/AdmisiContext/AntrianFeature` | Service Point, Queue Session, Queue Entry/current call state/CallCount behavior; Queue Label formation; lifecycle invariants | Domain value objects and shared pure utilities | MediatR, Dapper, SQL, HTTP, SignalR, configuration, DALs |
| Application — `Bilreg.Application/AdmisiContext/AntrianFeature` | Commands, queries, orchestration, repository ports, transactions, post-commit refresh port, cross-context contracts | Domain and application abstractions | SQL, Dapper, ASP.NET types, Hub contexts, database DTOs |
| Infrastructure — `Bilreg.Infrastructure/AdmisiContext/AntrianFeature` | Repository implementations, DTO mapping, Dapper DALs, projection/current-display DALs, conditional transition persistence, SignalR adapter | Application ports, Domain models, SQL and external SDKs | Business decisions, UI behavior |
| API — `Bilreg.Api` | Authentication, device/actor context mapping, transport adaptation, MediatR dispatch, SignalR hub transport, runtime registration | Application and Infrastructure composition | Aggregate mutation, queue sequencing, lifecycle decisions |
| External clients | Kiosk intake, Queue Display presentation, Admission Module composition | Published application interfaces | Direct database access, caller-generated authoritative identities or state |

Domain methods decide whether a transition is valid. Application handlers authorize and orchestrate. Repository implementations persist the decided transition with expected-state conditions. Projection DALs may query optimized joins without reconstructing aggregates but must never decide or perform lifecycle transitions.

## 5. Application Use Cases

| ID | Status | Use case | Kind | Purpose | Authority / initiator | Primary model | Dependencies | Transaction outcome |
|---|---|---|---|---|---|---|---|---|
| UC-AQO-001 | Target | Establish Admission Service Point | Command | Create one active Service Point | Queue Operations Administrator | Service Point | Service Point repo, actor context, clock, audit | Service Point and audit committed atomically |
| UC-AQO-002 | Target | Amend Admission Service Point | Command | Change its recognized name or future Queue Prefix | Queue Operations Administrator | Service Point | Service Point repo, actor context, clock, audit | New catalog state committed; snapshots unchanged |
| UC-AQO-003 | Target | Retire Admission Service Point | Command | Stop future intake without erasing history | Queue Operations Administrator | Service Point | Service Point repo, active-session check, audit | Retired state and audit committed |
| UC-AQO-004–006 | Excluded from V1 | Managed Loket/Kiosk/Display resources and authorization | — | V1 uses deployment configuration and permits every configured Loket to serve every active Service Point | Deployment/operations | None | Local configuration | No business-resource persistence |
| UC-AQO-007 | Existing partial / Target hardening | Issue Admission Queue Entry | Command | Validate an active Service Point, resolve server Business Date, lazily find/create the single daily session, obtain the next 1–9999 value from legacy `ISequencer`, and allocate one stable Queue Label | Kiosk client | Queue Session | Service Point repo, queue repo, sequencer, Business Date/clock | Session/entry committed; current Kiosk V1 request is non-idempotent |
| UC-AQO-008 | Deferred (R-05B) | Get Intake Attempt Result | Query | Future recovery of an uncertain intake after the Kiosk supports persistent per-interaction identity and retry reuse | Same client/API access context | Future intake-result projection | ClientRequestId | No current behavior |
| UC-AQO-009 | Target simplified | List Active Admission Service Points | Query | Return active Service Points; no per-Loket authorization filter exists in V1 | Admission Officer | Service Point projection | actor context, projection DAL | Read-only result |
| UC-AQO-010 | Target | List Admission Queue Worklist | Query | Return the scoped queue-only Waiting, outstanding-call, and In Service projection | Admission Officer or composing Admisi query | Admission Queue Worklist Projection | Actor/Loket context, projection DAL | Read-only paged result |
| UC-AQO-011 | Target | Call Admission Queue Entry | Command | Claim one Waiting entry for a workstation-configured LoketKey, increment CallCount, update `BILRG_AdmLoketCurrentCall`, and increment AnnouncementVersion when audio is required | Authenticated Admission Officer | Queue Session / Queue Entry | queue repo, workstation configuration, clock, display-state DAL | Queue/current-display state committed; refresh published after commit |
| UC-AQO-012 | Target | Recall Admission Queue Entry | Command | Increment CallCount and, when audio is required, AnnouncementVersion for the same outstanding entry | Authenticated Admission Officer | Queue Session / Queue Entry | queue repo, workstation configuration, clock, display-state DAL | Current state committed; refresh published after commit |
| UC-AQO-013 | Existing partial / Target split | Start Admission Queue Service | Command | Acknowledge the call and move Waiting to In Service | Admission Officer | Queue Session / Queue Entry | queue repo, actor/configured-Loket context, clock, display-state DAL | Entry/current-display state committed atomically; refresh follows commit |
| UC-AQO-014 | Target | Conclude No-Show or Withdraw Entry | Command | End or retain waiting participation under approved disposition | Queue Operations Supervisor | Queue Session / Queue Entry | queue repo, supervisor authorization, clock, audit, display-state DAL | Disposition/current-display/audit committed; refresh follows commit |
| UC-AQO-015 | Target simplified | Redirect with Priority Replacement | Command orchestration | Give the origin an explicit non-active disposition and create a Priority destination entry with CreationReason `Redirected` and required composite `(SourceAntrianId, SourceNoUrut)` | Authenticated Admission Officer | Two Queue Sessions | source/target queue repos, Service Point repo, audit | Prefer one transaction; no QueueTransfer aggregate/history |
| UC-AQO-016 | Target | Get Current Queue Display State | Query | Reload shared current-per-Loket state and AnnouncementVersion | Passive Queue Display | Current display state | projection DAL | Read-only snapshot |
| UC-AQO-017 | Target | Get Queue Call History | Query | Support investigation and approved operational review | Authorized officer/supervisor | Call-history projection | scoped projection DAL | Read-only paged history |
| UC-AQO-018 | Existing | Select Existing Journey for Admission Entry | Command | Associate anonymous InService entry with selected existing Tracker | Admission Officer | Queue Session and Patient Tracker | queue repo, Tracker repo, admission Service Point authority | CAS association and Tracker evidence committed atomically |
| UC-AQO-019 | Existing partial / Target boundary | Complete Queue from Final Registration Outcome | Integration Inbound | Complete the matching InService entry from an Admisi-owned `Established` or `NotEstablished` outcome; reuse/create Tracker only when applicable | Admisi Rajal Registration | Queue Session and optional Patient Tracker | Patient Tracker application contract, Registration Outcome authority, outbox | Outcome and queue completion commit atomically; Established also coordinates Registration/Tracker writes; NotEstablished may leave entry anonymous |
| UC-AQO-020 | Target simplified | Publish Queue Refresh | Post-commit best effort | Publish a SignalR refresh hint after queue/current-display transaction commits | Application handler | None | SignalR transport | No durable delivery record; polling recovers missed hints |
| UC-AQO-021 | Target | List Queue Resource Catalog | Query | Support authorized catalog administration and selection | Queue Operations Administrator | Resource projections | scoped projection DALs | Read-only paged result |
| UC-AQO-022 | Existing compatibility / Target constrained | Complete Registration without Prior Intake | Command collaboration | Preserve legacy callers that have no admission Queue Entry | Admisi Rajal Registration | Queue Session / Queue Entry | server-resolved compatibility Service Point, queue repo | Identified create/serve/complete remains one local transaction |

Exact transport routes, payloads, UI views, and visible labels belong to later API and UI artifacts.

## 6. Use-Case Traceability

| Use case ID | Domain capabilities / rules | SOP references | Owning module | Authorization concern | Required tests |
|---|---|---|---|---|---|
| UC-AQO-001–003 | Admission Service Point Management; BR-AQO-001–002, 006–011 | §3.2–3.3 | Queue Resource Catalog | Queue administrator role | Aggregate lifecycle, prefix conflict, audit, persistence |
| UC-AQO-004–006 | V1 deployment configuration; BR-AQO-003–006d | §3.8–3.9; §4.2 | Outside managed resource catalog | Authenticated operator plus trusted workstation configuration | Configuration parsing, unknown/empty Loket rejection, every-Loket/every-Service-Point behavior |
| UC-AQO-007–008 | Admission Queue Intake; Queue Number Allocation and Labelling; BR-AQO-005, 007–016 | §4.1; §5.1–5.3 | Queue Execution | Active Service Point validation | Daily-session uniqueness, bounded legacy sequence allocation, label snapshot, explicit non-idempotent V1 behavior; UC-AQO-008 deferred |
| UC-AQO-009–010 | Configured Loket operation; Queue Call Coordination | §4.2 | Queue Operational Projections | Any authenticated officer; configured Loket scope | Query scoping, ordering, paging, stale-state behavior |
| UC-AQO-011–013 | Queue Call Coordination; BR-AQO-017–024 | §4.3–4.4; §5.5–5.6 | Queue Execution | Authenticated officer and configured Loket | Domain transitions, CallCount/version increments, CAS conflicts, post-commit notification failure |
| UC-AQO-014–015 | Admission Queue Exception Resolution; BR-AQO-026–030 | §5.6–5.8 | Queue Execution | Supervisor decision and reason | No-show limitations, origin disposition plus Priority replacement, partial-failure prevention, audit, concurrency |
| UC-AQO-016–017, 020 | Queue Call Coordination; BR-AQO-028–030 | §4.3–4.4; §5.8; §6.2, §6.7 | Projections / Notification Delivery | Passive display read access | Shared-state reload, AnnouncementVersion audio decision, missed SignalR recovery by polling |
| UC-AQO-018 | BR-AQO-013–016 | §4.5 steps 24–29; §5.9–5.10 | Journey Association | Authenticated Admission Officer | Existing CAS success/conflict and no losing evidence |
| UC-AQO-019 | BR-AQO-014–016, 023–025 | §4.5–4.6; §5.11–5.12 | Journey Association / Admisi Rajal | Accountable officer authorized for current InService entry | Established Walk-In/Booking branches; final NotEstablished with reason; correctable validation remains InService; anonymous Done; rollback across all local writes |
| UC-AQO-021 | Admission Service Point Management; Loket/Kiosk administration | §3.2–3.9 | Queue Resource Catalog | Administrator-scoped query | Projection correctness and sensitive-field exclusion |
| UC-AQO-022 | Compatibility constraint; parent BR-TRK-025, 029 | Not primary SOP path | Queue Execution / Admisi Rajal | Authenticated registration workflow only | Regression of no-kiosk registration behavior |

## 7. Aggregate and Domain Model Realization

### 7.1 Service Point Aggregate

- **Existing:** `ServicePointType` is a two-field value snapshot; `ServicePointStatusEnum` is unused; no aggregate or repository implementation exists.
- **Target root:** `ServicePointModel`, following repository naming standards.
- **Target value objects:** Service Point identity/reference and Queue Prefix value. Queue Prefix normalizes with `Trim` + `ToUpperInvariant`, accepts exactly one uppercase ASCII letter, and participates in active-prefix uniqueness validation.
- **Protected rules:** BR-AQO-001–002, 006–011.
- **Consistency boundary:** stable identity, current name, prefix for future sessions, Active/Retired lifecycle.
- **Mutations:** establish, rename, change future prefix, retire. Historical session snapshots are never mutated.
- **Prohibited:** queue allocation, Loket assignment ownership, coverage eligibility decisions.

### 7.2 Deployment-configured Loket and Kiosk

Loket and Kiosk are not aggregates or master resources in Pragmatic V1. The Kiosk submits a stable ServicePointId selected from local configuration; the server validates only that the Service Point is active. The Admission Module supplies LoketId from trusted workstation configuration, and any authenticated Admission Officer may use any configured Loket for any active Service Point. These choices deliberately move configuration integrity into deployment operations.

### 7.3 Queue Session Aggregate

- **Existing root:** `AntrianModel` with `AntrianEntryModel` children.
- **Target extensions:** reuse the dedicated session table with one row per Service Point/Business Date, fixed interval, immutable one-character Queue Prefix Snapshot, canonical SequenceTag, legacy `ISequencer`, five-character Queue Label behavior, and 1–9999 no-cycle limit; extend Queue Entry with current call state, CallCount, Priority, CreationReason, and nullable source composite identity. ClientRequestId is reserved/deferred under R-05B.
- **Protected rules:** BR-AQO-007–030 plus parent BR-TRK-026–039a.
- **Consistency boundary:** Queue Number uniqueness, label stability, entry lifecycle, singular Tracker association, one outstanding call per entry, call disposition, and destination Loket identity.
- **Creation/mutation entry points:** issue entry, call, recall, acknowledge/start, complete, withdraw, and transfer orchestration.
- **Events/facts:** stable domain facts listed in Domain §9 may drive local projection notifications; they do not require a generic event bus.
- **References:** ServicePointId, configured LoketId, optional TrackerId, and optional origin entry identity. Future ClientRequestId is outside current Kiosk V1.
- **Prohibited:** Registration outcome, Patient master mutation, eligibility decisions, SignalR delivery.

The aggregate may contain many entries. Operational queries must use projections. Lifecycle persistence may use repository-owned expected-state operations for one entry/call rather than rewriting every child, provided the Domain model first authorizes the transition and the conditional persistence protects the same invariant.

### 7.4 Patient Tracker Aggregate

`PasienTrackerModel` remains the existing root for Patient Journey evidence. Queue issuance and calling do not create it. UC-AQO-018 and UC-AQO-019 append evidence only after accountable association or Registration-owned creation. It must not absorb Queue Call or resource-catalog behavior.

### 7.5 Cross-aggregate orchestration

- Redirection coordinates explicit origin disposition and Priority destination issuance. No QueueTransfer aggregate/history is created; one local transaction is preferred to avoid an active origin with a separately issued replacement.
- Registration completion coordinates Admisi Rajal Registration, Patient Tracker, Queue Session, billing/outbox behavior already present in the registration use case. Each authority remains explicit even when the same local transaction provides atomicity.
- A final `NotEstablished` decision coordinates the Admisi Rajal Registration Outcome aggregate and Patient Tracker Queue Session in one local transaction. It does not create a Registration, Patient Tracker, billing, or Registration evidence. The Queue Entry may remain anonymous and becomes Done only after the outcome is valid and persisted.
- Resource validation reads Service Point/Loket/Kiosk authority before mutating Queue Session; Queue Session stores stable identity and snapshots rather than owning those aggregates.

## 8. Persistence and Repository Strategy

### 8.1 Existing persistence

- `BILRG_Antrian` stores Queue Session identity, date/time, sequence tag, description, and Service Point code.
- `BILRG_AntrianEntry` stores `(AntrianId, NoUrut)`, anonymous/Tracker association, status, timestamps, and references.
- `AntrianRepo` reconstructs the aggregate and currently performs collection comparison for saves.
- `AntrianEntryDal.UpdateFromAnonymousInService` is the existing conditional transition anchor.
- Current `ISequencer` provides the next number using a sequence tag and remains the approved sole allocation authority. Admission sequences must be bounded to `MAXVALUE 9999 NO CYCLE`; a returned/out-of-range value is rejected and no `LastQueueNumber` column is added.

### 8.2 Target repositories

Application owns one repository contract per aggregate root:

- `IServicePointRepo`
- extended `IAntrianRepo`
- existing `IPasienTrackerRepo`

Infrastructure owns repository implementations, DTOs, DAL contracts/implementations, and explicit SQL. The unused Application `IServicePointDal` must not become the target contract.

### 8.3 Target persistence additions

Use additive, module-prefixed SQL artifacts for:

- Service Point aggregate state;
- the existing `BILRG_Antrian` Queue Session, extended with an immutable one-character Queue Prefix Snapshot and canonical SequenceTag used by legacy `ISequencer`;
- Queue Entry current call state, CallCount, Priority, CreationReason (`Normal`, `Redirected`, `ManualPriority`), and nullable composite `(SourceAntrianId, SourceNoUrut)`; ClientRequestId persistence is deferred; and
- `BILRG_AdmLoketCurrentCall`, keyed by unique LoketKey, storing only the latest visible call, composite Queue Entry reference `(AntrianId, NoUrut)`, `RowVersion` for optimistic concurrency, and separate `AnnouncementVersion` for audio replay decisions.

Reuse `BILRG_Antrian` as the dedicated Queue Session and enforce one row per `(ServicePointId, BusinessDate)` directly or through one formally canonical constrained session key. Legacy `ISequencer` is the sole allocation authority: its admission sequence is keyed by canonical `AN{yyMMdd}0000_{ServicePointCode}`, bounded to 1–9999 with no cycle, and exhaustion is rejected. SQL sequence values may contain gaps because consumption is non-transactional; uniqueness and monotonic issuance, not contiguity, are required. Do not add `LastQueueNumber`. Persist the fixed V1 interval and immutable `VARCHAR(1)` Queue Prefix Snapshot. Queue Label is the uppercased one-letter prefix plus exactly four digits. Extend Queue Entry persistence for `Withdrawn`, current call state, CallCount, Priority, CreationReason, and nullable source composite identity. `Redirected` requires the source identity; `Normal` prohibits it. ClientRequestId persistence/uniqueness is deferred under R-05B. Do not derive historical Queue Labels from current Service Point data.

Registration Outcome persistence belongs to Admisi Rajal, not `AntrianFeature`. It must retain OutcomeId, QueueEntryId, Result, conditional RegId/ReasonCode, Explanation, DecidedAt, and DecidedBy. The persistence/API contract must resolve how the existing `(AntrianId, NoUrut)` Queue Entry identity is represented as QueueEntryId.

Detailed columns and indexes belong to implementation persistence artifacts, but migrations must follow `docs/DATABASE.md`: explicit SQL, no workflow triggers, non-null defaults where appropriate, audit columns on transaction tables, append/status history, and operational indexes.

### 8.4 Concurrency

Whole-aggregate last-writer-wins updates are insufficient for active queues. Target repository operations must return affected-row counts and require exactly one expected transition:

- available Waiting → outstanding Call created;
- outstanding Call → CallCount incremented;
- Waiting with matching outstanding Call → InService and Call acknowledged;
- Waiting → Withdrawn;
- InService → Done;
- anonymous InService → identified InService or Done (existing pattern).

Zero affected rows is an explicit conflict. Do not automatically select another entry, Loket, or Tracker. The caller refreshes the relevant projection.

Database uniqueness must support, not replace, domain decisions: one Queue Session per `(ServicePointId, BusinessDate)`, unique Queue Number per session, unique LoketKey/current-call row, at most one outstanding call per entry, and at most one Outstanding or InService entry per LoketKey. No ClientRequestId uniqueness is required in current Kiosk V1.

Admission allocation must reject a next sequence value outside 1–9999. Allocating 9999 exhausts that Queue Session. Because Pragmatic V1 permits only one admission session per Service Point per Business Date, the server must reject further same-date intake for that Service Point; the next session can be lazily established only on the next authoritative Business Date. The repository must not insert an out-of-range number, create a second same-date session, wrap the counter, or change formatting to accommodate a larger value.

### 8.5 Time and identity

- Use `ITglJamProvider` for business occurrence times.
- Derive the admission session Business Date from server-side `ITglJamProvider.Now` (which exposes business time) through the shared Business Date capability. Do not accept an authoritative date in Kiosk or client commands.
- Use application-generated opaque identities consistent with the current ULID practice.
- Derive human actor identity through `ICurrentUserContext`; do not trust request `UserId` for new commands.
- Validate submitted ServicePointId against active Service Point persistence; there is no Kiosk offering authority in V1.
- Obtain LoketKey from trusted workstation deployment configuration such as controlled PC name or configuration file. Do not accept operator-entered arbitrary values. Missing or detectably duplicate configuration blocks calling; workstation rename requires controlled update. This value is not backed by a Loket master or assignment table in V1.

### 8.6 Compatibility

- Backfill a repository Service Point for the configured `ADM / Loket Admisi` value before switching authority.
- Backfill or deterministically preserve existing session descriptions and Service Point codes; do not fabricate historical prefixes without an approved migration value.
- Keep current physician queues and compatibility adapter untouched.
- Gate new admission behavior separately from physician queues sharing `AntrianModel`.

## 9. Read Models and Query Strategy

| Projection | Consumer purpose | Source authority | Freshness | Filters / scope | Must not decide |
|---|---|---|---|---|---|
| Active Admission Service Point View | Support Admission Module choices and optionally central Kiosk choices; local Kiosk configuration remains V1 presentation source | Service Point aggregate | Request-time current | Active Service Points; authenticated officer where applicable | Kiosk-specific offering, eligibility, or per-Loket authorization |
| Intake Attempt Result View | Deferred recovery/reprint capability after persistent Kiosk request identity exists | Future Queue Session/Entry correlation | Not implemented | Future ClientRequestId contract | Any current retry deduplication |
| Admission Queue Worklist Projection | Scan queue-only Waiting, outstanding-call, and InService work; expose Queue Label, Service Point, state, call state, LoketKey, queue timestamps, Priority indicator, and optional TrackerId | Queue Session/Entry/Call and existing Tracker association | Near-real-time; query is recovery truth | Session date, ServicePointId, LoketKey, state, paging; Priority may influence sorting only | Claim, auto-select/bypass, call, start, complete, identify, or enrich with Booking/identity/Registration/administrative context |
| Current Queue Display State | Recover the latest visible call per LoketKey and AnnouncementVersion | `BILRG_AdmLoketCurrentCall` updated with queue mutation | Near-real-time plus polling | Configured LoketKey/all V1 display state per deployment contract | Call history, past activity, which entry to call, or whether service started |
| Queue Call Summary View | Inspect current call state and CallCount | Queue Entry | Request-time current | Date, Service Point, Loket, Queue Label | Reconstruct detailed attempt timestamps or no-show evidence |
| Service Point Catalog View | Administer/search active and retired Service Points | Service Point aggregate | Request-time current | status | Lifecycle decisions |

Projection DALs should use stable SQL shapes and indexes optimized for queue scanning. They may denormalize names and Queue Labels for display but must retain source identities and must never become a second write model.

The Admission Module or an Admisi Rajal application query composes `Admission Queue Worklist Projection` with Booking, identity, Registration, and administrative context to produce the enriched `Admisi Rajal Work List`. That composition remains a read concern: it neither creates another ledger nor moves Admisi-owned enrichment into Patient Tracker.

## 10. Application Interfaces and API Philosophy

- Application commands and queries remain transport-independent MediatR requests or equivalent application contracts.
- HTTP controllers and the real-time hub adapt authentication and transport only.
- Exact routes, request/response fields, UI-specific view models, and labels are deferred to an API contract after UI design.
- Commands return deterministic success identities and expected error categories: validation, not found, forbidden, conflict, and unavailable dependency.
- Conflict remains HTTP 409 at HTTP transport; authorization distinguishes unauthenticated from forbidden access.
- Current Kiosk V1 intake is non-idempotent. Repeated requests may allocate separate entries. ClientRequestId behavior is deferred until the client persists and reuses one identity for an uncertain interaction.
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
| Admission Module | Inbound to Patient Tracker and Admisi Rajal | Compose the queue-only projection into the enriched Admisi Rajal Work List; call, service, Journey Resolution, Registration | Respective owning context | Authenticated commands/queries | Read composition plus synchronous local command transactions | Explicit validation/conflict; refresh projections |
| Queue Display | Query inbound; notification outbound | Recover and present committed calls | Patient Tracker | Snapshot query plus SignalR target transport | At-least-once notifications; snapshot recovery | Deduplicate by notification/call-attempt identity; reconnect and reload |
| Admisi Rajal Registration | In-process collaboration | Establish Registration and coordinate queue completion | Admisi Rajal for Registration; Patient Tracker for queue/Tracker | Application contract, not table/DAL access | Same local SQL transaction where current registration orchestration already requires atomicity | Roll back local writes; return accountable registration result |
| Patient master context | Indirect through Registration/Journey Resolution | Supply canonical identity evidence | Patient context | Existing application/domain contracts | No new queue-owned mutation | Keep entry InService until accountable resolution outcome |
| Authentication authority | Inbound identity | Authenticate officers and device/service clients | Identity authority | JWT/service credential validation | Per connection/request | Reject unauthenticated or invalid scope |
| Queue refresh transport | Outbound best effort | Hint clients to reload committed current display state | Patient Tracker application | SignalR adapter behind Application port | Immediate publish after commit; no durable delivery guarantee | Periodic polling recovers missed hints |
| Existing EMR queue integration | Existing outbound, unchanged | Physician/registration queue publication | Existing integration feature | Existing durable outbound queue | Existing behavior | Must not be reused as display notification authority |

Do not introduce a generic cross-context event bus solely because the domain lists events. Use direct orchestration for local consistency. Pragmatic V1 intentionally accepts best-effort post-commit SignalR refresh plus periodic polling instead of a notification outbox.

## 12. Authentication, Authorization, and Audit

### 12.1 Authentication

- Admission Officers and supervisors use the existing JWT authentication path.
- New human commands derive the actor from `ICurrentUserContext` rather than accepting authoritative `UserId` input.
- `DecidedBy` for a final Registration Outcome is derived from the authenticated accountable operator; `DecidedAt` comes from the business clock.
- Kiosk and passive Queue Display access follows the deployment/API security contract; no Kiosk or QueueDisplay master identity is resolved in V1.

### 12.2 Contextual authorization

- Kiosk intake validates only that submitted ServicePointId is active; local Kiosk configuration is not an authorization boundary.
- Queue Display clients reload shared current-per-Loket state; no display-specific scope exists in V1.
- Any authenticated Admission Officer may operate any configured Loket and active Service Point. LoketId is obtained from trusted workstation configuration, not an application assignment table.
- Supervisor-only commands cover final no-show disposition, transfer approval, and exceptional overrides.
- Resource-catalog commands require Queue Operations Administrator authority.
- UI visibility is not authorization. Application handlers enforce scope before mutation or data return.

Current `[Authorize]` without contextual policies is a gap. Current caller-provided `UserId` must not be carried into new target commands as authority.

### 12.3 Audit and operational history

- Queue Entry current call state and CallCount are operational queue truth but are not detailed attempt history or substitutes for compliance audit.
- Use `IAuditRepo` for Service Point/Loket/Kiosk administration, authorization changes, no-show disposition, transfer, override, and other material exceptional decisions.
- Audit records include authenticated actor, occurrence time, reason where required, affected identity, and before/after snapshot where useful.
- Do not put Patient demographics or Booking QR contents into notification payloads, display logs, or general structured logs.
- Kiosk and display clients receive the minimum data needed for their function.

## 13. Transactions, Consistency, Concurrency, and Idempotency

| Use case | Local transaction boundary | Synchronous invariants | Eventual facts | Duplicate key / retry | Conflict and recovery |
|---|---|---|---|---|---|
| Issue entry | Lazy daily session create/load, legacy sequence allocation, range check, entry insert | Service Point active, server Business Date, one session per Service Point/date, number 1–9999 unique, prefix snapshot stable | Post-commit refresh hint if needed | None in current V1 | Sequence gaps are permitted; repeated/uncertain requests may create separate entries |
| Call | Conditional entry claim, CallCount increment, `BILRG_AdmLoketCurrentCall` update, AnnouncementVersion increment when audio is required | Waiting, valid unique LoketKey, no competing Outstanding/InService work | Best-effort refresh after commit | Entry/current-state identity | 409; polling/worklist refresh recovers state |
| Recall | Conditional outstanding-call check, CallCount increment, AnnouncementVersion increment when audio is required | Same call still outstanding, same valid LoketKey | Best-effort refresh after commit | Entry identity | 409 on concluded call; no detailed attempt history |
| Start service | Conditional Waiting transition, call acknowledgement, current display update without AnnouncementVersion increment unless audio is explicitly required | Matching outstanding call and LoketKey, ServedAt ordering | Best-effort refresh after commit | Start request identity | 409; refresh entry/call |
| Withdraw/no-show | Conditional Waiting transition or retained Waiting disposition, current display update, audit | Supervisor authority, no service start when withdrawing | Best-effort refresh after commit | Disposition request identity | 409; detailed prior calls cannot be reconstructed from CallCount |
| Priority redirection | Explicit origin disposition, target allocation, Priority indicator, CreationReason `Redirected`, composite source identity | Source non-active after operation, target active | Best-effort refresh after commit | Command request identity when provided | Prefer one transaction; otherwise compensate/reconcile partial completion |
| Identify existing Tracker | Existing CAS entry association plus Tracker evidence | Anonymous InService and one selected Tracker | Optional worklist refresh | Entry identity + expected anonymous status | Existing 409; no losing Tracker evidence |
| Final outcome — Established | Registration Outcome, existing Registration transaction, conditional queue transition, optional association, local outbox | OutcomeId/QueueEntryId match, RegId required, Tracker rules satisfied, entry InService | Display/worklist notifications and existing integrations | OutcomeId plus Registration/source identities | Roll back Outcome, Registration, Tracker, queue, billing, and local outbox on conflict |
| Final outcome — NotEstablished | Registration Outcome, conditional InService → Done transition, local outbox | Explicit operator decision, ReasonCode required, RegId absent, matching entry; Tracker optional | Display/worklist notification | OutcomeId | Roll back Outcome, queue, and local outbox on conflict; no Registration/Tracker manufactured |
| Publish refresh | No database mutation after queue commit | Queue/current-display transaction already committed | SignalR best effort | None | Ignore transport failure operationally; polling reloads persisted state |

No transaction includes a Kiosk printer, browser, Queue Display, audio device, or remote client. Current Kiosk V1 has no reliable server-side recovery identity after an uncertain intake response; the UI must prevent concurrent presses and require deliberate retry. Once a call commits, a missed SignalR refresh is recovered by polling current display state and comparing AnnouncementVersion.

If multiple API instances are deployed, active-call/current-display uniqueness remains database-coordinated. SignalR scale-out remains an open deployment decision; polling is the V1 delivery recovery path.

## 14. Infrastructure and Operational Concerns

### 14.1 Database and migrations

- Ship additive scripts under `Bilreg.SqlDb/AdmisiContext/AntrianFeature` in dependency order.
- Seed/backfill the compatibility admission Service Point before enabling repository authority.
- Enforce one admission Queue Session per Service Point and Business Date; use the fixed Pragmatic V1 interval `00:00:00`–`23:59:59.9999999` as stored session truth.
- Validate globally duplicate active prefixes before enforcing uniqueness.
- Preserve legacy rows and sentinels; do not physically delete historical queue participation.

### 14.2 Real-time delivery

- Add ASP.NET Core SignalR as the target transport adapter; no SignalR support exists today.
- Atomically update `BILRG_AdmLoketCurrentCall` with queue mutation. Increment AnnouncementVersion only for Call/Recall that requires audio; ordinary refreshes and service-state changes do not increment it unless audio is explicitly required.
- Publish a SignalR refresh hint immediately after commit; no notification outbox or hosted dispatcher is introduced in V1.
- Passive displays reload persisted state after a hint, reconnect, or periodic polling.
- A display plays audio only when the reloaded AnnouncementVersion is newer than its last observed version. The polling interval is configurable and does not change projection authority.
- Polling is the V1 recovery mechanism for missed refresh hints and must not become write authority.

A successful SignalR broadcast means the server transport accepted the dispatch; it does not prove that a particular Queue Display rendered the call or played audio. The authoritative recovery mechanism remains the persisted projection and snapshot query.

### 14.3 Observability

Structured logs and metrics should include correlation/ClientRequestId when present, configured LoketId, ServicePointId, AntrianId, NoUrut, CallCount, AnnouncementVersion, transition result, conflict category, and refresh-publish result. Do not log unnecessary Patient evidence.

Minimum operational metrics:

- queue intake successes, duplicates, and failures;
- sequence conflicts;
- calls, recalls, starts, completions, withdrawals, and transfers;
- CAS conflicts by transition;
- outstanding calls and age;
- SignalR publish failures, display polling freshness, and current-display state age;
- active display connections and snapshot-recovery failures.

### 14.4 Configuration and rollout

- Feature-gate repository-backed Service Point/session behavior, split Call/Start behavior, and real-time refresh independently from physician queues.
- Configuration may bootstrap default identities but must not remain runtime Service Point authority.
- Health checks should cover database access, current-display freshness, and real-time endpoint readiness without exposing sensitive data.
- Offline Kiosk number allocation is prohibited; a disconnected Kiosk cannot safely allocate authoritative numbers.

## 15. Implementation Guidance for AI Agents

| Increment | Included use cases | Required layers | External dependencies | Verification gate |
|---|---|---|---|---|
| 1. Service Point and deployment configuration | UC-AQO-001–006, 009, 021 | Domain, Application, Infrastructure, API/configuration | Existing auth/audit and trusted workstation configuration | Service Point tests; configuration validation; every-configured-Loket/every-active-Service-Point behavior; no resource tables |
| 2. Labelled intake | UC-AQO-007; UC-AQO-008 deferred | Domain, Application, Infrastructure, API | Dedicated Queue Session persistence, legacy `ISequencer`, shared Business Date | Concurrent first-intake creates one daily session; bounded no-cycle sequence allocation; active Service Point validation; explicit non-idempotent limitation; four-digit formatting/exhaustion/prefix tests |
| 3. Operational projections | UC-AQO-010, 016–017 | Application, Infrastructure, API | Queue/current-display persistence | Queue-worklist field-boundary tests; current-per-Loket state/version tests; polling recovery; CallCount summary limitations |
| 4. Call and service transitions | UC-AQO-011–013 | Domain, Application, Infrastructure, API | Authenticated officer and workstation Loket configuration | Domain lifecycle tests; CallCount/version increments; CAS integration; competing calls; one-active-entry constraint; old direct-start compatibility test |
| 5. Best-effort display refresh | UC-AQO-020 plus current display state | Application, Infrastructure, API | SignalR and client polling | Commit-before-publish; publish failure leaves committed truth; polling recovery; AnnouncementVersion/audio behavior |
| 6. Journey and Registration alignment | UC-AQO-018–019, 022 | Application boundary refactor, Admisi Rajal Registration Outcome aggregate/persistence, existing Domain/Infrastructure/API | Admisi Rajal, Patient Tracker, existing billing/outbox | Booking reuse; both Walk-In Established branches; explicit NotEstablished with reason; validation remains InService; anonymous Done; outcome/queue rollback; no-kiosk regression |
| 7. Exceptions | UC-AQO-014–015 | Domain, Application, Infrastructure, API | Supervisor authorization/audit | Recall/CallCount limitations; no-show/withdraw tests; origin disposition plus Priority replacement; partial-failure/rollback; audit tests |
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
- trust client Service Point name, Queue Prefix, Queue Label, or UserId as authority; LoketId is accepted only from the explicitly trusted workstation-configuration adapter;
- make SignalR delivery part of the queue transaction;
- mark service started merely because a call was displayed;
- create a Tracker when issuing or calling a Queue Number;
- infer `NotEstablished` from validation failure, timeout, exception, missing Registration data, or queue state;
- reuse the EMR outbound queue as a display transport;
- put business decisions in controllers, DALs, projections, hubs, or background workers;
- claim detailed call-attempt evidence from CallCount or durable notification delivery from SignalR;
- leave an origin Queue Entry active after issuing its Priority replacement; or
- invent no-show automation not supported by persisted V1 evidence.

## 16. Architectural Decisions

### ADR-AQO-001 — Persist Service Point under Patient Tracker authority

- **Decision:** Realize Service Point as a repository-backed Patient Tracker aggregate. Loket and Kiosk portions of the original decision are superseded by ADR-AQO-013.
- **Status:** Accepted for Service Point only; partially superseded.
- **Context:** Configuration and caller-supplied snapshots cannot enforce stable identities, prefix history, or assignments.
- **Rationale:** The domain assigns these identities and queue relationships to Patient Tracker.
- **Consequences:** Add Service Point repository/migration. Loket and Kiosk remain deployment configuration in V1.
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

- **Decision:** Current Queue Call state remains distinct from service state while Queue Entry remains Waiting until service actually starts. Detailed Call Attempt persistence is superseded by CallCount under ADR-AQO-013.
- **Status:** Accepted with simplified V1 persistence.
- **Context:** Current `AdmissionQueueStartCmd` conflates call with service start.
- **Rationale:** Preserves correct ServedAt, waiting time, recall, and no-show evidence.
- **Consequences:** Split new Call and Start Service use cases; migrate new clients away from direct start semantics.
- **Rejected alternatives:** Add `Called` as the Queue Entry service state; treat display delivery as ServedAt.
- **Evidence or governing references:** BR-AQO-017–024; SOP §§4.3–4.4.

### ADR-AQO-004 — Normalize and snapshot Queue Prefix at Queue Session establishment

- **Decision:** Normalize Queue Prefix with `Trim` + `ToUpperInvariant`; accept exactly one uppercase ASCII letter and enforce uniqueness across active admission Service Points. Preserve the normalized prefix on the admission Queue Session. Derive the five-character Queue Label by concatenating that snapshot and NoUrut formatted as exactly four digits without a separator, for example `A0032`. Allocation is limited to 1–9999; after 9999, reject further allocation. Under ADR-AQO-009, a replacement session is not permitted on the same Business Date.
- **Status:** Accepted Target.
- **Context:** Historical labels must survive later catalog changes.
- **Rationale:** One immutable session snapshot and one canonical formatting rule avoid client-specific labels while retaining deterministic historical labels.
- **Consequences:** Domain validation, active-prefix uniqueness, allocation exhaustion, and format boundary tests are required. Migration adds prefix snapshot; existing sessions still need an approved backfill strategy.
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
- **Status:** Superseded for Pragmatic V1 by ADR-AQO-013.
- **Context:** Queue Display requires near-real-time updates but does not yet exist, and transient delivery cannot be authoritative.
- **Rationale:** Separates queue consistency from client availability and reuses proven durable outbound patterns.
- **Consequences:** Add outbox persistence, worker, hub adapter, idempotent client handling, snapshot recovery, and operational monitoring; transport dispatch success is not client-render acknowledgement.
- **Rejected alternatives:** SignalR send inside the command transaction; database polling as the only permanent mechanism; reuse EMR outbound queue.
- **Evidence or governing references:** SOP §§4.3, 5.8; existing EMR/Lab outbound queues; absence of SignalR.

### ADR-AQO-007 — Authenticate and scope device clients independently

- **Decision:** Kiosk and Queue Display use authenticated service/device identities; human actors use JWT claims and contextual Loket authorization.
- **Status:** Superseded for Kiosk/Queue Display resource resolution by ADR-AQO-013; human authentication remains applicable.
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

### ADR-AQO-009 — Use one lazily established daily admission session per Service Point

- **Decision:** Pragmatic V1 permits one admission Queue Session per `(ServicePointId, BusinessDate)`, with interval `00:00:00` through `23:59:59.9999999`. The server resolves Business Date through the shared capability. The first valid intake lazily establishes the dedicated session row. Intake is rejected when ServicePointId is inactive or the daily session is exhausted after 9999; Kiosk-local offerings are not server authority.
- **Status:** Accepted Target.
- **Context:** The previous architecture did not define session creation authority, daily uniqueness, interval, or date trust boundary.
- **Rationale:** A single server-dated daily session is deterministic, matches the current all-day persistence shape, prevents clients from backdating/future-dating intake, and avoids speculative operating-hours administration in Pragmatic V1.
- **Consequences:** Add natural uniqueness for Service Point and Business Date, handle concurrent lazy creation by one-winner/reload behavior, remove authoritative date from new intake commands, and reject same-date intake after exhaustion rather than creating a second session.
- **Rejected alternatives:** Caller-supplied date; multiple intervals per Service Point/date; pre-creating every daily session; silently creating another same-date session after 9999.
- **Evidence or governing references:** Decision B; BR-AQO-007–007d and BR-AQO-008a; shared Business Date implementation.

### ADR-AQO-010 — Complete queue service only from an Admisi-owned final Registration Outcome

- **Decision:** Admisi Rajal persists a Registration Outcome with stable OutcomeId and QueueEntryId. Result is `Established` with RegId or `NotEstablished` with ReasonCode. Validation errors create no final outcome and leave the Queue Entry InService. Patient Tracker may conditionally complete the matching Queue Entry after the final outcome is valid; `NotEstablished` may complete an anonymous entry without creating a Patient Journey.
- **Status:** Accepted Target.
- **Context:** Queue completion previously had no durable negative Registration outcome and successful Registration was the only implemented completion source.
- **Rationale:** A durable final decision separates correctable failure from accountable completion while preserving Admisi Rajal's Registration authority and Patient Tracker's queue authority.
- **Consequences:** Add the Admisi Rajal Registration Outcome aggregate/repository, a cross-context completion contract, conditional queue persistence, authenticated actor/time capture, and tests for both results and rollback.
- **Rejected alternatives:** Treat validation errors/exceptions as final; use queue Done as proof of Registration outcome; require Tracker creation for NotEstablished; store the outcome as queue-owned truth.
- **Evidence or governing references:** Decision C; BR-ARJ-020a–020h; BR-AQO-024–025; SOP §§4.5–4.6 and 5.11–5.12.

### ADR-AQO-011 — Keep the Patient Tracker worklist projection queue-only

- **Decision:** Patient Tracker owns an `Admission Queue Worklist Projection` containing Queue Label, Service Point, state, call state, Loket, queue timestamps, and optional TrackerId. The Admission Module or an Admisi Rajal application query composes it with Booking, identity, Registration, and administrative context to produce the `Admisi Rajal Work List`.
- **Status:** Accepted by Decision D.
- **Rationale:** Queue truth remains reusable and context-pure while Admisi Rajal can present the enriched operational view required by officers.
- **Consequences:** UC-AQO-010 and its DAL/API must expose only the queue contract. Composition is read-only and must not introduce a second ledger or direct cross-context table writes.
- **Evidence or governing references:** Decision D; `docs/contexts/admisi-rajal/admisi-rajal-domain.md` §5.6 and BR-ARJ-010–012a.

### ADR-AQO-012 — Resolve Loket and Queue Display scope from authenticated server context

- **Decision:** Pragmatic V1 assigns zero or one active Loket to an authenticated Login Session through `AdmissionLoketAssignment`. Queue handlers resolve Current User + Current Login Session → active assignment → Loket → authorized Service Points. Queue Display is an Active/Retired authenticated device resource assigned to one Announcement Scope; V1 uses one scope for all active admission Service Points.
- **Status:** Superseded for Pragmatic V1 by the later developer decisions in ADR-AQO-013.
- **Rationale:** Client payloads cannot confer physical-service or display-subscription authority. Server-owned assignment and device resources provide stable authorization and audit boundaries.
- **Consequences:** Add assignment, display, and scope persistence; supervisor/admin use cases; active-assignment uniqueness; device/session context ports; authorization tests; and a bootstrapped V1 scope. Globally unique active Queue Prefixes remain required.
- **Rejected alternatives:** Caller-authoritative LoketId; browser-local assignment; caller-selected SignalR groups; per-building scopes invented before hospital location policy exists.

### ADR-AQO-013 — Adopt the developer-approved simplified persistence profile for Pragmatic V1

- **Decision:** Retain the existing dedicated daily Queue Session table, canonical SequenceTag, and legacy `ISequencer` with a 1–9999 no-cycle admission limit and unique `(ServicePointId, BusinessDate)`; treat Loket and Kiosk as deployment configuration; permit every configured Loket to serve every active Service Point; use passive displays backed by shared current-per-Loket state and AnnouncementVersion; retain CallCount instead of Call Attempt history; redirect by explicit origin disposition plus a new Priority entry with CreationReason and source composite identity; and publish best-effort SignalR refresh after commit with polling recovery and no outbox. Its optional ClientRequestId direction is superseded for current V1 by ADR-AQO-020.
- **Status:** Accepted from developer discussion; supersedes conflicting V1 portions of ADR-AQO-001, 003, 006, 007, and 012.
- **Rationale:** Reduce V1 persistence and administration scope while keeping queue numbering and current operational display state database-authoritative.
- **Consequences:** Deployment configuration becomes a trusted boundary; kiosk-specific server authorization is absent; current Kiosk intake is non-idempotent under the later R-05B deferral; CallCount cannot reconstruct detailed attempt/no-show evidence; SignalR delivery is not durable; redirection needs explicit origin disposition and preferably one local transaction.
- **Safety constraints:** do not automate history-dependent policies from CallCount, explicitly describe current Kiosk intake as non-idempotent, do not claim durable notifications, validate active ServicePointId server-side, reject missing/invalid workstation Loket configuration, and never leave both origin and Priority replacement active.

### ADR-AQO-014 — Use deployment-owned LoketKey and enforce one current patient per Loket

- **Decision:** A configured Loket is identified by a stable unique LoketKey sourced from controlled workstation configuration, such as PC name or a configuration file. The server does not accept an arbitrary operator-entered LoketKey. One LoketKey may have at most one Outstanding or InService Queue Entry.
- **Status:** Accepted V1 design decision.
- **Consequences:** Missing or duplicate LoketKey blocks Call/Recall; workstation rename requires controlled configuration update; a database-coordinated active-work claim is required across Queue Sessions.
- **Limitation:** Without a Loket master, detecting two separate workstations configured with the same key requires an explicit deployment validation or runtime lease/registration mechanism; the exact mechanism remains GAP-AQO-004.

### ADR-AQO-015 — Keep Call separate from service start and manual queue progression

- **Decision:** Call/Recall updates current display state and CallCount. Queue Entry remains Waiting until the operator explicitly starts service. CallCount is informational only and does not automatically mark No-Show, postpone an entry, calculate subsequent-patient rules, or choose the next entry.
- **Status:** Accepted V1 design decision.
- **Consequences:** No-show and recall policy remain manual. Automated history-dependent behavior requires a later persistence/design decision.

### ADR-AQO-016 — Treat Priority as an indicator with explicit provenance

- **Decision:** Priority is a visual and default-sorting aid, not an automatic calling rule. The operator retains call-selection authority. Queue Entry persists CreationReason as `Normal`, `Redirected`, or `ManualPriority`, with nullable composite `(SourceAntrianId, SourceNoUrut)`; `Redirected` requires both source fields, `Normal` prohibits both, and a partial source key is invalid.
- **Status:** Accepted V1 design decision.
- **Consequences:** Redirection creates a new Priority entry rather than moving the origin. UI/query sorting may surface it, but handlers must not automatically bypass other entries.

### ADR-AQO-017 — Use authoritative current-call projection with recoverable passive displays

- **Decision:** `BILRG_AdmLoketCurrentCall` stores only the latest visible call per LoketKey and is authoritative projection state, not history. SignalR is only a refresh trigger. Displays reload after a trigger, reconnect, and periodic polling. AnnouncementVersion increments only on Call/Recall requiring audio.
- **Status:** Accepted V1 design decision.
- **Consequences:** Missed SignalR does not cause permanent staleness; ordinary screen refresh/service-state changes do not retrigger audio; past activity cannot be reconstructed from this table.

### ADR-AQO-018 — Reuse existing queue tables under the verified minimal persistence profile

- **Decision:** Reuse `BILRG_Antrian` as Queue Session and `BILRG_AntrianEntry` as Queue Entry. Add a Service Point master and latest-state `BILRG_AdmLoketCurrentCall`; do not add Loket, Kiosk, assignment, authorization, call-history, or managed-display masters. Queue lifecycle remains on the existing entry model.
- **Status:** Accepted direction; **partially supported** by the codebase and not migration-ready.
- **Verified support:** The two queue tables, composite entry identity, lifecycle timestamps/status, `ServicePointCode`, and unique `SequenceTag` already exist. Prefix snapshot, Priority, CallCount, Service Point master, current-call projection, Call/Recall behavior, and versioning do not.
- **Resolved constraints:** Queue Prefix is exactly one normalized uppercase ASCII character; Queue Number is exactly four digits; legacy `ISequencer` is the sole allocator and no `LastQueueNumber` is added.
- **Resolved persistence constraints:** Priority controls indication/default sorting only; `(AntrianId, NoUrut)` is retained for current-call, source, and cross-context outcome references; RowVersion is distinct from AnnouncementVersion; composite provenance is retained; ClientRequestId persistence is deferred; and Decision C outcomes are persisted separately in Admisi Rajal.
- **Evidence:** The queue SQL definitions and migration, Domain models, DTO/DAL/repository mappings, anonymous-intake handler, and start-service handler; detailed findings are in GAP-READY-009 of the implementation-gap artifact.

### ADR-AQO-019 — Use the current-call row as the cross-session active Loket claim

- **Decision:** `BILRG_AdmLoketCurrentCall` is the authoritative active ownership claim and latest current-call projection. Its primary key is LoketKey. `Outstanding` and `InService` are active; `Released` confers no ownership and may be replaced by a later Call. A filtered unique index on `(AntrianId, NoUrut)` for active states prevents one Queue Entry being claimed by multiple Loket.
- **Status:** Accepted V1 design decision on 2026-07-23.
- **Transitions:** Call acquires or replaces Released; Recall retains Outstanding; Start Service retains and changes to InService; No-Show, Withdraw, Redirect, Complete, and rollback to Waiting release the matching claim. Claim mutation and Queue Entry mutation share one local database transaction.
- **Concurrency:** Use database uniqueness, expected-state SQL, and RowVersion. Conflicts raise `AdmissionQueueConcurrencyException` and map to HTTP 409. In-memory locks, generic lock tables, and automatic alternate selection are prohibited.
- **Stale policy:** Age alone never releases a claim. Release requires an explicit Queue Entry business transition or a controlled administrative recovery operation with expected identity/state/version and audit evidence.
- **Implementation contract:** [TRACKER-ADMISSION-QUEUE-R04-LOKET-CLAIM-CONTRACT.md](./TRACKER-ADMISSION-QUEUE-R04-LOKET-CLAIM-CONTRACT.md).
- **Boundary:** The claim table enforces database ownership under one LoketKey; detecting two workstations configured with the same key remains GAP-AQO-004.

### ADR-AQO-020 — Split bounded allocation from deferred Kiosk idempotency

- **Decision:** R-05A retains `ISequencer` as the sole Admission Queue number authority and implements canonical `AN{yyMMdd}0000_{ServicePointCode}`, range 1–9999, `NO CYCLE`, explicit exhaustion, acceptable gaps, and concurrency-safe SQL allocation. R-05B defers ClientRequestId persistence, uniqueness, recovery, and retry behavior until the Kiosk can generate, retain, and reuse one identity for a specific uncertain interaction.
- **Status:** Accepted and R-05A implemented on 2026-07-23; R-05B is non-blocking.
- **Consequences:** Current Kiosk V1 intake is explicitly non-idempotent. Repeated requests may create separate Queue Entries. The client prevents repeat presses while pending and requires deliberate retry after an uncertain response. No ClientRequestId database constraint or deduplication claim exists in this phase.
- **Implementation evidence:** [tracker-admission-queue-r05a-sequencer-implementation-report.md](./tracker-admission-queue-r05a-sequencer-implementation-report.md).

## 17. Pragmatic V1 Implementation Notes

- Resolve LoketKey through a configuration adapter using controlled PC name or configuration file. Precedence, normalization, and duplicate-detection mechanism belong to the implementation/configuration contract.
- Treat workstation rename as a deployment change; update configuration in a controlled rollout before queue operation resumes.
- Validate configured ServicePointId references against existing active Service Point master data. Configuration cannot create or modify that data.
- Do not add ClientRequestId persistence/uniqueness for current Kiosk V1. Reserve it for a future end-to-end client request-identity and retry contract.
- Increment AnnouncementVersion on Call or Recall only when audio is required. Reloading a screen does not increment it.
- Queue Display reloads after SignalR and reconnect, and polls every configured N seconds. The exact N is an operational setting, not an architecture invariant.
- The Priority indicator must be visible in the queue-only worklist; sorting may surface Priority entries but never auto-selects them.

## 18. Open Gaps and Deferred Decisions

| ID | Gap or decision | Why it matters | Owner / authority needed | Blocks | Safe interim position |
|---|---|---|---|---|---|
| GAP-AQO-001 | Exact no-show threshold and whether an entry returns to Waiting or becomes Withdrawn | Determines supervisor action, timers, projections, and tests | Hospital Queue Operations policy owner | Final no-show UI/API and automation | Require explicit supervisor disposition; do not automate a threshold |
| GAP-AQO-002 | Passive display API/access policy | No QueueDisplay master/scope exists, but read access still needs an explicit deployment/API security position | Security/platform owner | Production display exposure | Restrict at network/API layer until the contract is approved; display remains read-only |
| GAP-AQO-004 | Trusted LoketKey distribution, integrity, and cross-workstation duplicate detection mechanism | Two machines using the same key are indistinguishable without a registry, deployment validation, or runtime lease; local validation alone cannot prove uniqueness | Deployment/security owner | Production Loket accountability and safe calling | Block missing/known duplicate values, log resolved key, control rename/config rollout, and choose a duplicate-detection mechanism before production |
| GAP-AQO-005 | API instance topology and SignalR scale-out/backplane | Multi-instance refresh delivery can be missed across instances | Deployment/platform owner | Final real-time infrastructure | Poll persisted current display state; do not claim SignalR delivery guarantees |
| GAP-AQO-006 | Kiosk and Queue Display client technology, repository, and hosting | Determines later UI and deployment artifacts, not backend authority | Product/platform owner | Client implementation | Keep backend contracts transport/client agnostic |
| GAP-AQO-007 | Queue Entry/current-display/audit retention periods | Affects storage volume and authorized history | Compliance and hospital operations | Retention jobs and capacity sizing | Retain Queue Entries and audit; current display state is replaceable operational state |
| GAP-AQO-008 | Exact API contract and visible UI labels beyond `Call` | Required for client implementation and contract tests | Product/UI/API owners | UI delivery and API endpoint implementation | Implement Application contracts first; do not invent routes or labels in architecture |
| GAP-AQO-009 | Operational evidence used to offer or transfer BPJS versus General service | Queue does not own eligibility truth | Admisi Rajal/guarantor policy owner | Automated routing suggestions | Let Patient choose an offered Service Point and require accountable transfer when wrong |
| GAP-AQO-010 | Prefix backfill for historical and currently active admission sessions | Historical rows have no authoritative prefix | Data migration and operations owner | Enabling Queue Label projection for legacy sessions | Preserve raw NoUrut and mark prefix migration unresolved; do not infer from arbitrary descriptions |
| GAP-AQO-011 | Deprecation date and consumers of current direct admission `start` | New Call/Start split cannot safely replace unknown clients immediately | API/product owner | Removal or semantic change of existing capability | Keep compatibility endpoint feature-gated; new clients use split commands |
| GAP-AQO-012 | Resolved by ADR-AQO-019: Start Service retains the row as InService; No-Show, Withdraw, Redirect, Complete, and rollback release it; a later Call may replace only Released | Active display queries exclude Released; the table cannot provide call history | Resolved | No longer blocks claim persistence; final UI labels remain GAP-AQO-008 | Preserve AnnouncementVersion except for audio Call/Recall |
| GAP-AQO-013 | Registration Outcome ReasonCode catalog and ownership | Required reasons must be stable and selectable without inventing codes in clients | Admisi Rajal operations/domain owner | Final NotEstablished API/UI and reporting | Require a non-empty code in the model; do not invent or hard-code reason values until approved |
