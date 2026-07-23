# Patient Tracker — Admission Queue Architecture Reconciliation

**Verdict:** `ARCHITECTURE UPDATED — READY FOR IMPLEMENTATION PLANNING`

**Evidence date:** 2026-07-23

**Scope:** architecture reconciliation, implementation-state classification, remaining delivery
plan, and first-slice prompt. This artifact does not authorize or claim feature implementation.

## 1. Overall verdict

The approved Pragmatic V1 target is internally consistent after the R-00 through R-14 decisions.
The architecture required correction because its pre-roadmap evidence and gap language still
described capabilities that now exist in source and migration scripts, and ADR-AQO-008 still implied
idempotent Kiosk recovery that R-05B explicitly deferred.

The backend is substantially implemented, but the Admission Queue feature is not delivered end to
end. Source presence is not applied-schema evidence; focused mocked tests are not the mandatory
real-SQL concurrency/rollback gate; API presence is not a deployed officer/Kiosk/display workflow;
and the no-op refresh publisher is not SignalR delivery.

No owner decision blocks backend planning. The external ReasonCode catalog, client repositories,
deployment topology, production authorization policy, and HiDok call-site require their respective
owners before those delivery slices can exit.

## 2. Architecture statement classification

| Architecture section | Classification | Reconciliation |
|---|---|---|
| §1 overview | Valid but clarified by later decisions | Removed persisted Loket/Kiosk implication; separated backend source from end-to-end delivery. |
| §2 evidence and constraints | Superseded by later implementation | Replaced pre-R-06–R-13 absence claims with current source/schema evidence. |
| §2.1 constraints | Still valid | Existing physician compatibility, sentinels, SQL/Dapper, client-independent truth, and server allocation remain. |
| §2.2 gaps | Superseded in part | Service Point, labels, claim, transitions, projection, outcomes, API, workstation checks, and audit schema exist. Real-SQL, clients, delivery, identity, and rollout remain. |
| §3 boundaries | Still valid | Patient Tracker owns queue truth; Admisi Rajal owns Registration truth and enrichment. |
| §4 layer responsibilities | Still valid | Current code follows the intended Domain/Application/Infrastructure/API split. |
| §5 use cases | Valid but status labels were historical | Most backend use cases now have source and transport; usability depends on applied schema, integration verification, and consumers. |
| §6 traceability | Still valid | Domain/SOP trace remains authoritative; implementation status belongs in this matrix. |
| §7 aggregate realization | Valid but clarified | No Loket/Kiosk aggregate. Queue Call is realized as entry `CallCount` plus current claim/display state, not call history. |
| §8 persistence | Already implemented in source/schema, deployment unverified | Additive scripts and CAS repos exist; no applied-database manifest or real-SQL gate proves readiness. |
| §9 read models | Already implemented backend; client composition absent | Queue-only worklist/current display queries exist. Admisi enrichment is not present here. |
| §10 API | Already implemented backend | Versioned controller exists; actor/role hardening is intentionally deferred. |
| §11 integration | Partially implemented | Registration completion and Booking assistance backend exist; external HiDok branch and composed worklist do not. |
| §12 security/audit | Foundation only / deferred | `[Authorize]`, workstation mapping, and audit columns exist. Claims-derived actor and contextual roles do not. |
| §13 transactions/concurrency/idempotency | Partially implemented | CAS/claim/outcome transactions exist; Kiosk transport idempotency is deferred; mandatory real-SQL proofs remain. |
| §14 infrastructure | Partially implemented | SQL scripts and persisted recovery exist. SignalR, clients, scale-out, monitoring, and rollout evidence do not. |
| §15 agent guidance | Still valid | Expected-state SQL and no speculative resources remain governing constraints. |
| §16 ADRs | Valid after corrections | ADR-AQO-008 clarified by 020; ADR-AQO-012 superseded; ADR-AQO-018 status updated. |
| §17 V1 notes | Still valid | Static workstation identity, null historical labels, explicit transitions, and Priority-only sorting remain. |
| §18 gaps | Valid after corrections | Backend API is resolved; historical prefix non-backfill is an intentional decision, not a blocker. |

## 3. Reconciled decisions

### 3.1 Queue Session and allocation

- One session per `(ServicePointId, authoritative BusinessDate)` is represented by the canonical
  `SequenceTag` and unique index.
- Business Date is server-resolved from `ITglJamProvider`; new intake accepts no date.
- The first intake lazily creates the session. Unique-index one-winner behavior exists, but ordinary
  intake does not yet transparently reload the winner after a concurrent first-create conflict.
- `QueuePrefixSnapshot` is immutable for the session.
- `ISequencer` is the only allocator. Admission allocation is 1–9999, `NO CYCLE`; 9999 succeeds and
  the next request fails explicitly. Transaction rollback may leave acceptable gaps.
- The sequence creation path uses SQL application locking. Queue Session concurrent-first-create
  still belongs in the real-SQL gate.

### 3.2 Service Point

- `BILRG_AdmServicePoint` is the V1 authoritative master.
- Active/Retired is sufficient.
- A filtered unique index enforces active prefix uniqueness.
- Client/workstation configuration cannot create Service Point authority.
- Historical sessions without a prefix snapshot return no fabricated Queue Label.

### 3.3 Queue Entry identity and provenance

- `(AntrianId, NoUrut)` remains identity; no surrogate QueueEntryId is approved or present.
- Entry schema/model contains `CreationReason`, nullable composite source identity, Priority, and
  CallCount.
- Priority is an indicator/default sort only.
- CallCount is informational and is not call history.
- Operational transitions retain historical rows. Admission v1 transition handlers do not invoke
  whole-aggregate removal.

### 3.4 Loket and workstation

- No Loket master, assignment, or Loket-ServicePoint authorization table belongs to V1.
- Every configured Loket may serve every active Service Point.
- Controlled static workstation configuration maps workstation key to LoketKey. Missing, duplicate,
  unmapped, or mismatched configuration blocks Loket mutation.
- `BILRG_AdmLoketCurrentCall` is both latest visible state and the active claim.
- One Loket has at most one active Outstanding/InService entry; one entry has at most one active
  Loket claim.
- `RowVersion` is optimistic concurrency state. `AnnouncementVersion` is audio-replay intent.
- Cross-installation or spoofed-header integrity remains deployment/security work.

### 3.5 Call and service lifecycle

- Call and Recall retain Queue Entry `Waiting`, increment CallCount, and hold Outstanding claim.
- Start Service explicitly changes the entry to `InService` and the claim to InService.
- No-Show is an explicit command and is stored as Withdrawn/`NoShow`.
- CallCount and Priority never automate disposition or selection.
- Redirect withdraws the origin and creates a Priority Waiting replacement with composite
  provenance.
- Start, release, Redirect, and final outcome use local transactional coordination with the claim.

### 3.6 Display

- There is no managed Queue Display master.
- Persisted current-per-Loket state is authoritative.
- SignalR is specified as a best-effort reload hint only; the current implementation is a no-op.
- Reconnect and polling must reload the persisted snapshot.
- AnnouncementVersion changes only for Call/Recall audio intent.
- Display state never becomes queue write authority.

### 3.7 Registration Outcome

- Admisi Rajal owns immutable `BILRG_RegOutcome`; Patient Tracker owns Queue Entry lifecycle.
- `Established` requires RegId. `NotEstablished` requires ReasonCode.
- Validation failure creates no final outcome.
- NotEstablished can complete an anonymous entry without creating a Tracker.
- Outcome remains outside queue tables.
- Explicit finalization inserts the outcome, completes the entry, and releases the matching claim
  in one local transaction.
- The ReasonCode catalog remains an external operational dependency.

### 3.8 Worklists and Booking assistance

- The queue-only worklist contains no Booking, patient identity, Registration, eligibility, or
  physician enrichment. Admisi may compose it read-only.
- Successful Self-Registration must create no assistance entry.
- `BookingAssistanceIntakeCmd` ensures one active assistance entry only after it is invoked for an
  accountable assistance-required result.
- Business duplicate protection by BookingId is distinct from deferred Kiosk request idempotency.
- The HiDok Self-Registration decision branch/caller is external to this repository.

### 3.9 Explicitly deferred

- Claims-derived platform actor migration and contextual role redesign.
- Kiosk retry/transport idempotency.
- Automatic No-Show, queue aging, next-entry selection, and automatic Service Point routing.
- Display timing/clear policy, UI wording, backend print telemetry.
- Production SignalR scale-out/backplane.
- Retention, archive, purge, capacity, dashboards, and monitoring infrastructure.

## 4. Current implementation-state matrix

The classifications below describe the checked workspace. “Schema” means a script exists, not that
any environment has applied it.

| Capability | State | Executable evidence | Qualification |
|---|---|---|---|
| Queue aggregate and composite entry identity | Implemented and usable | `AntrianModel.cs`, `AntrianEntryModel.cs`, `BILRG_Antrian.sql`, `BILRG_AntrianEntry.sql` | Legacy shared aggregate remains; admission transitions use narrow operations. |
| Authoritative Business Date intake | Implemented and usable | `QueAnonymousIntakeCmd.cs` | Uses server clock; no client date. |
| Lazy daily session establishment | Partially implemented | `QueAnonymousIntakeCmd.cs`, `AntrianFactory.cs`, `UX_BILRG_Antrian_SequenceTag` migration/index | One-winner constraint exists; transparent loser reload and real-SQL race proof absent. |
| Bounded sole allocation | Implemented and usable | `ISequencer.cs`, `Sequencer.cs`, `AntrianModel.AddAdmissionEntry`, `SequencerAdmissionAllocationTest.cs` | SQL allocation tests were reported; applied target-database state is separate. |
| Service Point master/lifecycle | Implemented as foundation only | `AdmissionServicePointModel.cs`, commands/repo, `BILRG_AdmServicePoint.sql`, v1 GET/PUT routes | Requires applied migration, seed, policy/operational ownership before use. |
| Immutable prefix and Queue Label | Implemented and usable for new sessions | `BILRG_Antrian_M2_QueuePrefixSnapshot_Alter.sql`, model/DTO/DAL/projection mappings | Historical label intentionally null; migration application unproven. |
| Queue Entry operational fields/provenance | Present as schema and executable mappings | `BILRG_AntrianEntry_M2_QueueOperations_Alter.sql`, `AntrianEntryModel.cs`, DTO/DAL | Real constraint/migration verification pending. |
| Active Loket claim/current display | Partially implemented | `BILRG_AdmLoketCurrentCall.sql`, `AdmissionQueueOperationRepo.cs`, projection reader | Source/schema complete; real concurrent race/release proof pending. |
| Call/Recall/Start | Partially implemented | `AdmissionQueueOperationalCommands.cs`, operation repo, v1 controller, focused tests | No officer client; refresh publisher is no-op; real-SQL rollback/CAS gate absent. |
| Withdraw/No-Show | Partially implemented | same transition files plus operational tests | Explicit only as approved; client and real-SQL evidence absent. |
| Redirect | Partially implemented | redirect handler/repo, provenance fields, tests | Multi-write rollback and concurrent target/session creation require real-SQL proof. |
| Queue-only officer worklist | Implemented as foundation only | `AdmissionQueueOperationalQueries.cs`, `AdmissionQueueOperationalProjection.cs`, v1 route | Backend query exists; representative-volume query plan and officer UI absent. |
| Current display snapshot/replay policy | Implemented as foundation only | projection reader, `QueueAnnouncementPolicy`, v1 route | Persisted reload exists; display client/polling/audio implementation absent. |
| Best-effort refresh hint | Present only as contract/scaffolding | `IAdmissionQueueRefreshPublisher`, `NullAdmissionQueueRefreshPublisher` | No SignalR hub or publisher. |
| Registration Outcome model/schema | Partially implemented | `RegistrationOutcomeModel.cs`, `BILRG_RegOutcome.sql`, commands/repo/routes/tests | Reason catalog external; real-SQL unique/rollback proof absent. |
| Established Registration compatibility | Partially implemented | `RegJalanWalkInCommand.cs`, `RegJalanByBookingCmd.cs`, outcome repo | Existing flows are wired, but integrated migrated-database journey is not proven here. |
| Anonymous NotEstablished completion | Partially implemented | `FinalizeRegistrationNotEstablishedCmd`, outcome repo, route/tests | No consuming officer workflow/reason catalog. |
| Journey association | Implemented and usable backend | `TrkJourneyResolveSelectCmd.cs`, `AdmissionQueueIdentify.cs`, CAS repository/tests | Existing Patient Tracker capability; client composition still absent. |
| Booking assistance business deduplication | Partially implemented | `BookingAssistanceIntake.cs`, `BookingAssistanceRepo.cs`, `BILRG_AdmBookingAssistance.sql`, route/tests | External AssistanceRequired branch/caller absent; real unique race proof pending. |
| Kiosk request idempotency | Deferred by accepted decision | ADR-AQO-020, R-05A report | Current intake is explicitly non-idempotent. |
| v1 backend API | Partially implemented | `AdmissionQueueV1Controller.cs`, error middleware, API contract tests | Routes exist; no confirmed client and no deployed contract verification. |
| Workstation-to-Loket validation | Implemented as foundation only | API options/validator, controller checks, tests | Static configuration and trusted-edge requirement; cross-installation integrity deferred. |
| Authentication | Implemented as foundation only | controller `[Authorize]` | Authenticated access only. |
| Actor identity and contextual authorization | Deferred by accepted decision | request `UserId` fields, API contract | Payload UserId and generic access remain; do not claim accountable production identity. |
| Audit schema/mutation timestamps | Present as schema or migration | `BILRG_AdmissionQueue_M3_Audit_Alter.sql`, transition SQL | Shared audit-event policy is not a substitute for applied-schema verification. |
| No-deletion operational preservation | Implemented for v1 operations | transition repos use status updates/inserts | Legacy whole-aggregate delete path still exists outside v1 handlers. |
| Admisi enriched worklist | Not implemented | no composing query/client found | Must compose without creating another ledger. |
| Kiosk client | Not implemented in available source | repository inventory | External/client task. |
| Officer queue client | Not implemented in available source | repository inventory | External/client task. |
| Queue Display client | Not implemented in available source | repository inventory | External/client task. |
| Production migrations/seeds/rollout | Not implemented/evidenced | scripts and reports only | No production changes were inspected or authorized. |
| Real-SQL operational gate | Present only as documented tests/scaffolding expectation | R-13 report; existing focused tests | Required race, rollback, and representative-volume suite is not executable/configured. |
| Retention/monitoring/scale-out/print telemetry | Deferred by accepted decision | R-12B/R-13/R-14 artifacts | Explicitly outside V1 feature implementation. |

## 5. Contradictions and qualifications found

1. The old §2 absence claims contradicted current source and migrations.
2. ADR-AQO-008's “retrieve the same result” consequence contradicted the accepted R-05B deferral.
3. R-07's “no production API” statement is historically accurate for that increment but no longer
   describes current source because R-10 added the v1 controller.
4. R-08 calls post-commit refresh, but production DI binds the port to
   `NullAdmissionQueueRefreshPublisher`; refresh delivery is scaffolding only.
5. Implementation reports repeatedly require migrated real-SQL verification. No configured,
   executable suite or result closes that gate.
6. The API derives Loket from controlled headers/configuration but still accepts `UserId` in
   payloads. This is an accepted security deferral, not claims-derived accountability.
7. Current worktree changes touch anonymous intake, factory, DI/config, tests, and an R-01 report.
   They were treated as uncommitted workspace evidence and were not modified by this review.

## 6. Remaining implementation plan

### Slice 1 — Migrated-database operational proof and daily-session race hardening

**Business outcome:** prove that the backend already present can safely allocate, claim, transition,
redirect, finalize, and recover on a disposable SQL Server database with all Admission Queue
migrations applied.

**Baseline:** source/schema and focused tests exist. The R-13 real-SQL gate is documented but not
executable. First-session unique-index conflict is not transparently recovered by ordinary intake.

**Remaining work**

- Create an isolated integration-test migration fixture that applies the exact Admission Queue
  scripts in dependency order and never targets production.
- Add real-SQL tests for same-Loket/two-entry, two-Loket/one-entry, stale RowVersion,
  release-versus-Recall, concurrent daily-session creation, Redirect rollback, outcome rollback,
  Booking-assistance unique race, and 9999 exhaustion.
- Add representative-volume worklist/current-display tests and capture query-plan/index evidence.
- If the concurrent daily-session test exposes the known loser failure, add the narrow
  one-winner/reload behavior without introducing another allocator or lock table.
- Publish a verification report with database version, script order, commands, and results.

**Affected contexts:** Patient Tracker; Admisi Rajal only for final outcome integration.

**Domain:** no new business policy. Only preserve existing invariants.

**Application:** at most narrow concurrent-first-session recovery.

**Infrastructure/SQL:** test fixture and evidence; correct only demonstrated migration/CAS defects.

**API/client:** no new route or UI.

**Tests:** all R-13 mandatory cases plus regression suite.

**Dependencies:** disposable SQL Server connection supplied through test configuration; R-06
through R-13 scripts.

**Out of scope:** production DB, SignalR, UI, roles, ClientRequestId, monitoring, retention.

**Exit criteria:** clean database can migrate; every mandatory race/rollback test passes repeatedly;
no orphan active claim or double-active entry; query evidence is acceptable; focused regression
passes; results are reproducible.

**Parallelism:** blocks backend rollout and should run first. Client contract reading can proceed,
but mutation clients must not be operationally approved before this gate.

### Slice 2 — Officer operational workflow

**Business outcome:** an Admission Officer can view queue-only work, select Call, Recall, Start,
Redirect, No-Show/Withdraw, resolve journey, and finalize an outcome with conflict recovery.

**Baseline:** v1 API and backend handlers exist; no client is confirmed.

**Remaining work:** implement the client against the published v1 contract; source Loket only from
controlled workstation setup; reload on 409; preserve manual selection; compose Admisi-owned
Booking/identity/Registration enrichment read-only; supply approved ReasonCodes; instrument legacy
endpoint usage and migrate confirmed consumers.

**Affected contexts:** Patient Tracker, Admisi Rajal, existing Admission frontend.

**Domain/Application/SQL:** no new queue policy or ledger. Backend corrections only if contract
tests expose defects.

**API:** consume v1 routes; do not extend payload identity authority.

**Tests:** contract, component, accessibility, conflict/reload, end-to-end Established and
NotEstablished, Redirect, and legacy compatibility tests.

**Dependencies:** Slice 1; approved client repository, ReasonCode catalog, deployment workstation
mapping.

**Out of scope:** automatic selection, role redesign, display client, Kiosk idempotency.

**Exit criteria:** complete officer workflow works on migrated integration environment; no direct
DB writes; composed worklist is not a ledger; conflicts recover by reload; audit fields are visible
in verification evidence.

**Parallelism:** client UI and Admisi composition may run in parallel after contract fixtures are
stable.

### Slice 3 — Kiosk intake and Booking Self-Registration assistance

**Business outcome:** a Kiosk lists active Service Points, issues/prints a committed Queue Label,
and requests Booking assistance only from Admisi's accountable AssistanceRequired result.

**Baseline:** service-point/intake/booking-assistance APIs and business deduplication exist; no Kiosk
or definitive HiDok Self-Registration branch is available here.

**Remaining work:** implement client pending-button/deliberate-retry behavior; print only after
success; connect the narrow assistance endpoint to the approved decision branch; treat
`Existing=true` as success; ensure successful self-registration and transient errors create no
entry.

**Affected contexts:** Patient Tracker, Admisi Rajal, Booking/HiDok, Kiosk client.

**Domain/Application/SQL:** no ClientRequestId and no duplicate allocator; correct only defects
proved by integration tests.

**API:** existing v1 routes.

**Tests:** success/no-entry, AssistanceRequired/new, AssistanceRequired/existing, uncertain response,
print failure, exhausted sequence, inactive Service Point, and concurrent duplicate assistance.

**Dependencies:** Slice 1; external client/caller ownership and approved assistance result.

**Out of scope:** generic Kiosk retry idempotency, backend print telemetry, automatic routing.

**Exit criteria:** successful self-registration never queues; assistance does; duplicate business
requests converge; operators understand uncertain retry behavior.

**Parallelism:** may run in parallel with Slice 2 after Slice 1 contract stability.

### Slice 4 — Recoverable passive Queue Display

**Business outcome:** passive displays reload persisted current-Loket truth, receive best-effort
refresh hints, poll/reconnect safely, and replay audio only for a higher AnnouncementVersion.

**Baseline:** current snapshot route, version policy, and post-commit refresh port exist; publisher,
hub, and client do not.

**Remaining work:** implement one minimal SignalR refresh-hint adapter/hub; keep messages
non-authoritative; implement client initial load, reconnect reload, configured polling, per-Loket
last-version tracking, and audio replay rule.

**Affected contexts:** Patient Tracker API/infrastructure and Queue Display client.

**Domain/Application/SQL:** no managed display master, call history, or timing policy.

**API:** additive hub transport only; existing snapshot remains recovery path.

**Tests:** publisher-after-commit, no publish on rollback, missed hint recovery, reconnect, polling,
ordinary state change no audio, Recall higher-version audio, stale/out-of-order hint.

**Dependencies:** Slice 1; display hosting/access decision. Single-instance delivery can precede
deferred scale-out.

**Out of scope:** display timing/clear rules, durable outbox, managed devices/scopes, Redis/backplane.

**Exit criteria:** disabling SignalR does not lose queue truth; polling recovers; audio is version
driven; no display operation mutates queue state.

**Parallelism:** can run in parallel with Slices 2–3 after Slice 1.

### Slice 5 — Integration rollout and compatibility closure

**Business outcome:** the complete V1 workflow is reproducibly deployable to an integration
environment and confirmed consumers use the new contract.

**Baseline:** scripts, API toggle, and partial reports exist; no consolidated deployment evidence.

**Remaining work:** codify migration/seed order, preflight schema checks, rollback boundary,
workstation provisioning, protected headers, health checks, client configuration, smoke tests,
legacy endpoint usage observation, and explicit go/no-go checklist.

**Affected contexts:** Bilreg API, Patient Tracker, Admisi Rajal, clients, deployment operations.

**Domain/Application/API:** no new policy.

**Infrastructure/SQL:** migration manifest and safe integration deployment only.

**Tests:** full operational journey, restart/reconnect, rollback rehearsal, compatibility, load
smoke, and security-edge checks.

**Dependencies:** Slices 1–4 and owner decisions for deployment access.

**Out of scope:** production execution in this task, platform security redesign, scale-out,
retention/monitoring platform.

**Exit criteria:** signed integration evidence, reproducible rollback, seeded Service Points,
unique workstation mappings, clients configured, no unresolved severity-one data/concurrency issue.

**Parallelism:** preparation can run earlier; final gate is sequential after delivered clients.

## 7. Recommended first implementation task

Use this prompt unchanged unless the integration database mechanism has changed:

```text
Implement only “Admission Queue Slice 1 — Migrated-database operational proof and daily-session
race hardening” in D:\Project.Aktif\MyHospitalWeb\b09-bilreg-api.

Read docs/ARTIFACTS.md, docs/INSTRUCTION.md, docs/ENGINEERING.md,
docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md,
docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ARCHITECTURE-RECONCILIATION.md,
docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-R04-LOKET-CLAIM-CONTRACT.md, and the R-05A
through R-14 reports before editing.

Goal:
Create a reproducible, disposable real-SQL integration gate for the already-present Admission Queue
backend. Apply the exact R-06 through R-13 Admission Queue scripts in dependency order, then prove:
same Loket/two entries, two Loket/one entry, stale RowVersion, release-versus-Recall, concurrent
first daily-session creation, Redirect rollback, final-outcome rollback, Booking-assistance unique
race, allocation 9999/exhaustion, and representative-volume worklist/current-display queries.

Rules:
- Never connect to or modify production. Require an explicit integration-test connection and fail
  closed when it is absent.
- Do not implement Kiosk, officer UI, Queue Display, SignalR, roles, claims-derived actor migration,
  ClientRequestId, monitoring, retention, or any automatic queue policy.
- Do not introduce a QueueEntryId, LastQueueNumber, lock table, Loket master, assignment table,
  authorization table, managed display, or second allocator.
- Preserve `(AntrianId, NoUrut)`, `ISequencer`, accepted sequence gaps, explicit officer actions,
  current-call claim semantics, and local transaction boundaries.
- Treat current uncommitted workspace changes as user work; do not overwrite or reformat them.
- Fix production code only when a real-SQL test demonstrates a defect. The known permitted fix is a
  narrow one-winner/reload path for concurrent first daily-session creation. Do not broaden scope.

Deliver:
1. the isolated migration/test fixture;
2. deterministic real-SQL race and rollback tests;
3. representative-volume projection evidence;
4. any narrowly demonstrated fix with unit/regression coverage;
5. a Slice 1 verification report recording database version, script order, commands, results, and
   remaining deployment qualifications.

Exit only when the clean-database migration is reproducible, all mandatory tests pass repeatedly,
no double-active claim/orphan state remains, and the existing focused Admission Queue regression
suite passes.
```

