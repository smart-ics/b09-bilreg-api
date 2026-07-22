# Patient Tracker — Admission Queue Architecture Implementation Gap Analysis

**Artifact status:** Codebase-grounded implementation readiness assessment

**Bounded context:** Patient Tracker

**Assessment date:** 2026-07-22

**Target architecture:** [TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md](./TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md)

**Primary business specification:** [TRACKER-ADMISSION-QUEUE-DOMAIN.md](./TRACKER-ADMISSION-QUEUE-DOMAIN.md)

## 1. Executive verdict

**Verdict: conditional NO-GO for autonomous end-to-end implementation.**

An agent can safely implement bounded preparatory work and some isolated foundations, but it cannot safely implement the complete target architecture without inventing business, security, persistence, and API decisions that the artifacts explicitly leave unresolved or do not yet identify.

The architecture is directionally sound and aligns with the major ownership rules in Patient Tracker and Admisi Rajal. In particular, it correctly keeps queue truth in Patient Tracker, separates Queue Call from service start, preserves late Patient Journey identification, uses direct local orchestration for Registration, and treats display delivery as non-authoritative.

However, implementation is blocked at several slice boundaries by missing authoritative decisions:

1. conditional persistence and database constraints are described as goals, but the concrete persistence contract needed to protect cross-session LoketKey and idempotency invariants is absent.
2. cross-workstation duplicate LoketKey detection, passive-display access, API contracts, compatibility consumers, migration values, and production topology are not yet resolved.
3. the Booking Self-Registration → Assistance decision and business-level duplicate-assistance guard are not represented in the architecture use cases.

Therefore, the safe instruction is:

```text
Do not ask one agent to implement UC-AQO-001 through UC-AQO-022 as a single feature.

Resolve the blocking contracts below, checkpoint the current working tree,
then implement and verify one gated increment at a time.
```

## 2. Scope and evidence

This assessment compared the target architecture against:

- [TRACKER-ADMISSION-QUEUE-DOMAIN.md](./TRACKER-ADMISSION-QUEUE-DOMAIN.md);
- [TRACKER-ADMISSION-QUEUE-SOP.md](./TRACKER-ADMISSION-QUEUE-SOP.md);
- [TRACKER-DOMAIN.md](./TRACKER-DOMAIN.md);
- [admisi-rajal-domain.md](../admisi-rajal/admisi-rajal-domain.md);
- [tracker-admission-queue-late-identification-gap-analysis.md](./tracker-admission-queue-late-identification-gap-analysis.md);
- [TRACKER-COMPATIBILITY.md](./TRACKER-COMPATIBILITY.md);
- [admisi-rajal-queue-number-feasibility-analysis.md](../admisi-rajal/admisi-rajal-queue-number-feasibility-analysis.md);
- [tracker-codebase-gap-report.md](./tracker-codebase-gap-report.md);
- [operational-events.md](../../concepts/operational-events.md);
- [ENGINEERING.md](../../ENGINEERING.md), [DATABASE.md](../../DATABASE.md), [NAMING.md](../../NAMING.md), and [WORKFLOW.md](../../WORKFLOW.md); and
- current Domain, Application, Infrastructure, API, SQL, and focused test code under `src/bilreg`.

This was a source-level assessment. No staging or production database, external Kiosk, Queue Display, Admission Module, identity provider, or deployment topology was inspected.

### 2.1 Working-tree qualification

The assessed workspace is not a clean committed baseline. The architecture/domain/SOP artifacts are currently untracked, and the late-identification implementation spans modified, deleted, and untracked source files. The assessment therefore describes the **current workspace state**, not necessarily the last committed release.

Before implementation begins, preserve or commit this baseline so an implementation agent can distinguish prior work from its own changes and avoid overwriting concurrent artifact changes.

## 3. Artifact-to-artifact alignment

| Concern | Alignment result | Assessment |
|---|---|---|
| Queue ownership | Aligned | Patient Tracker owns Queue Session, Queue Entry, Queue Number, calls, and milestones; Admisi Rajal owns Registration truth. |
| Queue Label grammar | Resolved | Decision A defines prefix normalization/validation, global active uniqueness, 1–9999 numbers, minimum-three-digit formatting, no separator, and rejection after 9999. |
| Queue Session policy | Resolved/simplified | Decision B defines one session per Service Point per server-resolved Business Date and lazy all-day establishment. The later developer decision retains a dedicated table with LastQueueNumber and replaces Kiosk offering checks with active ServicePointId validation. |
| Anonymous intake | Aligned | Queue intake does not create or select a Patient Tracker. |
| Late identification | Aligned | Existing journey selection and Registration-owned new Walk-In Tracker creation follow the parent Tracker and Admisi Rajal rules. |
| Call versus service start | Aligned target; not current runtime | Domain, SOP, and architecture distinguish them. Current `AdmissionQueueStartCmd` directly moves Waiting to InService and has no Queue Call or Loket. |
| Registration versus queue completion | Resolved at artifact level | Decision C gives Admisi Rajal a durable final Registration Outcome and allows Patient Tracker to complete the matching entry, including anonymous NotEstablished completion. |
| Work List | Resolved at artifact level | Decision D defines the Patient Tracker `Admission Queue Worklist Projection` as queue-only and the `Admisi Rajal Work List` as a composed, enriched operational view rather than a second ledger. |
| Operational events | Aligned with accepted V1 tradeoff | Events are business facts; direct orchestration preserves local consistency. Display refresh is intentionally best-effort SignalR plus polling, without a notification outbox. |
| Loket authority | Superseded/simplified | The later developer decision supersedes Decision E: Loket comes from trusted workstation configuration, any authenticated officer may use it, and no master/assignment/authorization tables exist. |
| Display authority | Superseded/simplified | The later developer decision supersedes Decision E: passive displays reload shared current-per-Loket state and AnnouncementVersion; no QueueDisplay or AnnouncementScope masters exist. |
| Audit versus operational history | Aligned | Queue Call history remains operational truth; exceptional/admin decisions use compliance audit. |
| Historical retention | Aligned | Withdraw/transfer preserve historical entries and calls rather than deleting them. |
| Physician queue compatibility | Aligned | Admission queue numbering remains separate from the legacy physician `AntrianMap` compatibility adapter. |
| Global queue flexibility | Mostly aligned | Manual selection and supervised exceptions fit `docs/WORKFLOW.md`; the architecture correctly avoids automatic re-selection after a conflict. |

### 3.1 Developer-decision label reconciliation

The developer discussion reuses `GAP-READY-002` through `GAP-READY-010` for topics that do not match the stable identifiers already used by this artifact (for example, this artifact's GAP-READY-005 is Final Registration Outcome, while the discussion's label 005 means Call History). To preserve traceability, the decisions are recorded here by topic and ADR-AQO-013 rather than renumbering existing findings:

| Developer label | Topic adopted for V1 | Existing finding affected |
|---|---|---|
| 002 | Dedicated daily Queue Session with LastQueueNumber | GAP-READY-002, 008, 009 |
| 003 | Deployment-configured Loket; no master/assignment | GAP-READY-003, 007, 010 |
| 004 | Passive displays; shared current-per-Loket state/version | GAP-READY-004, 009, 010, 018 |
| 005 | CallCount instead of Call Attempt history | GAP-READY-017 and operational-history design |
| 006 | Priority replacement instead of QueueTransfer aggregate | GAP-READY-007, 009 and exception design |
| 007 | Locally configured Kiosk offerings; no Kiosk master | GAP-READY-010, 011 |
| 008 | Optional ClientRequestId on Queue Entry | GAP-READY-008, 010 |
| 009 | Post-commit SignalR plus polling; no outbox | GAP-READY-018 and delivery design |
| 010 | Every configured Loket may serve every Service Point | GAP-READY-003, 007 |

### 3.2 Classification of the additional V1 concepts

| Concept | Classification | Artifact treatment | Remaining gap? |
|---|---|---|---|
| Workstation-owned stable unique LoketKey; arbitrary operator input prohibited | Design decision | ADR-AQO-014; BR-AQO-006b/e/f | Exact cross-workstation duplicate-detection mechanism remains open |
| One Outstanding or InService entry per LoketKey | Design decision | ADR-AQO-014; BR-AQO-020 | Database active-work claim remains GAP-READY-007 |
| Call separate from explicit Start Service | Design decision | ADR-AQO-015; BR-AQO-021–023 | Compatibility/API migration remains open |
| Priority is indicator/sorting aid, never automatic call order | Design decision | ADR-AQO-016; BR-AQO-027a | UI/API field contract remains open |
| SourceAntrianEntryId and CreationReason provenance | Design decision/persistence contract | ADR-AQO-016; BR-AQO-027/027b | Exact SQL types/constraints remain GAP-READY-009 |
| `BILRG_AdmLoketCurrentCall` is latest-state projection, not history | Design decision | ADR-AQO-017; BR-AQO-006d | Clear/replace timing remains GAP-AQO-012 |
| AnnouncementVersion increments only for audio Call/Recall | Design decision | ADR-AQO-017; BR-AQO-021a | None semantically; implementation tests required |
| SignalR trigger plus reload/reconnect/poll recovery | Design decision | ADR-AQO-017 | Poll interval and access/topology are implementation/deployment settings |
| CallCount informational; no automatic no-show/progression | Design decision | ADR-AQO-015; BR-AQO-021b/026 | Hospital manual procedure remains operational policy |
| PC name/config-file source and controlled rename procedure | Implementation note | Architecture §17 | Configuration adapter/rollout contract required |
| Poll every N seconds | Implementation note | Architecture §17 | N is an operational setting, not an architecture gap |
| Optional ClientRequestId for kiosk retries | Implementation note constrained by ADR-AQO-013 | Architecture §17; BR-AQO-012 | Uniqueness scope/API behavior remains GAP-READY-008/010 |
| Configuration references existing active ServicePointId only | Design constraint plus implementation note | BR-AQO-006g; Architecture §17 | Validation/diagnostic behavior belongs implementation contract |

## 4. Codebase baseline

### 4.1 Capabilities present in the assessed workspace

- `AntrianModel` and `AntrianEntryModel` implement Queue Session and Queue Entry identity, allocation, Waiting → InService → Done, and anonymous/identified association.
- `QueAnonymousIntakeCmd` creates an anonymous entry without creating a Tracker.
- the new `AdmissionQueueStartCmd` permits an anonymous Waiting entry to enter InService.
- `TrkJourneyResolveSelectCmd` now associates an anonymous InService entry with an existing Tracker using a conditional compare-and-set update.
- `RegJalanWalkInCommand` now supports Registration-owned creation of a Tracker and attachment/completion of the existing anonymous InService admission entry.
- Registration, Tracker, admission queue, physician queue, billing, journal, remote-print, and existing outbound writes are coordinated inside the existing ambient transaction scope.
- `HttpCurrentUserContext` can derive a human actor from authenticated claims, although admission queue commands do not use it yet.
- shared compliance audit and durable outbound-processing patterns exist elsewhere in the solution.

### 4.2 Capabilities absent or unsafe for the target

- no Service Point, Loket, Kiosk, Queue Display, Queue Call, Call Attempt, intake-attempt, transfer-link, or queue-notification persistence exists;
- no `Withdrawn` state exists in `AntrianStatusEnum`;
- no admission-specific worklist/display/history projection exists;
- no Admisi Rajal Registration Outcome aggregate, repository, table, or final NotEstablished command exists;
- no SignalR registration, hub, worker, or queue notification outbox exists;
- controllers use generic `[Authorize]` only;
- current admission commands still accept caller-supplied `UserId`;
- anonymous intake accepts caller-supplied Service Point code/name and optional business date;
- current start has no call, Loket, assignment, or contextual authorization precondition;
- whole-aggregate `AntrianRepo.SaveChanges` still performs unrestricted updates and can physically delete missing entries;
- identified InService → Done still uses an unrestricted whole-aggregate save;
- current queue tables do not contain the global transaction audit columns;
- current Service Point session factory already uses the approved all-day interval, but intake still permits a caller-supplied date and does not validate a stable submitted ServicePointId against active Service Point persistence; and
- the current sequencer dynamically interpolates a sequence identifier derived from `SequenceTag` on a separate SQL connection.

### 4.3 Baseline verification

The following focused test selection was executed against the current workspace:

```text
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore \
  --filter "FullyQualifiedName~AdmissionQueueStartHandlerTest|FullyQualifiedName~AdmissionQueueRegistrationResolverTest|FullyQualifiedName~TrkJourneyResolveHandlerTest|FullyQualifiedName~QueAnonymousIntakeHandlerTest|FullyQualifiedName~AdmissionQueueCompleteAndMulaiPeriksaTest|FullyQualifiedName~AntrianEntryDalTest|FullyQualifiedName~AntrianRepoAreEqualTest"
```

Result:

```text
Passed: 17
Failed: 0
Skipped: 0
```

This proves that the current focused mechanics compile and their existing tests pass. It does not prove target architecture conformance, real database concurrency, contextual authorization, or end-to-end Kiosk/display behavior.

## 5. Readiness findings

### GAP-READY-001 — Queue Label grammar resolved by Decision A

**Status:** Resolved at artifact level; implementation remains pending.

The canonical decision is:

- Queue Prefix accepts one through four uppercase ASCII letters after `Trim` + `ToUpperInvariant`, with no spaces or separators;
- Queue Prefix is unique across active admission Service Points;
- Queue Number is an integer from 1 through 9999;
- Queue Number uses a minimum of three digits (`1` → `001`, `27` → `027`, `1000` → `1000`);
- Queue Label is Prefix + formatted Queue Number with no separator; and
- after allocating 9999, the session rejects further allocation; Decision B prohibits a second same-date session, so the next session is available only on the next server-resolved Business Date.

**Implementation requirement:** realize this as Domain value-object/allocation behavior with persistence uniqueness and boundary tests. Do not let clients format labels independently or create a second same-date session after exhaustion.

### GAP-READY-002 — Queue Session policy resolved by Decision B

**Status:** Resolved at artifact level; implementation remains pending.

The canonical Pragmatic V1 decision is:

- one admission Queue Session per Service Point per Business Date;
- fixed interval `00:00:00` through `23:59:59.9999999`;
- Business Date resolved server-side through the shared Business Date capability;
- no authoritative date in Kiosk/client requests;
- lazy establishment by the first valid intake; and
- rejection when the submitted ServicePointId is not active.

Combined with Decision A, an exhausted session cannot be replaced on the same Business Date. Further intake for that Service Point is rejected until the next authoritative Business Date.

The developer decision additionally retains a dedicated Queue Session table with LastQueueNumber and a unique `(ServicePointId, BusinessDate)` constraint. Kiosk offering configuration is local and is not server authority.

**Implementation requirement:** remove authoritative date from the intake contract, resolve Business Date server-side, atomically create/load the dedicated row and advance LastQueueNumber, enforce its natural uniqueness, and validate the Service Point is active.

### GAP-READY-003 — Superseded: Loket is deployment configuration in developer-approved V1

**Status:** Decision E's assignment model is superseded. No Loket master, assignment table, or service-authorization table will be implemented in V1.

LoketKey is supplied by trusted workstation configuration such as controlled PC name or local configuration file. It must be stable and unique. Missing or duplicate configuration blocks calling, PC rename requires controlled update, any authenticated Admission Officer may operate any configured Loket, and each configured Loket may serve every active Service Point.

**Implementation requirement:** define the configuration adapter, precedence, normalization, and controlled rename procedure; validate missing/malformed values; log LoketKey with the actor; and choose how duplicate keys across separate machines are detected. A unique current-call row alone cannot distinguish two machines sharing the same key. This is an accepted deployment trust boundary, not application authorization.

### GAP-READY-004 — Superseded: passive displays and shared current state adopted for V1

**Status:** Decision E's QueueDisplay/AnnouncementScope masters are superseded.

V1 stores one `BILRG_AdmLoketCurrentCall` row per LoketKey containing only the latest visible call. Call/Recall updates the row and increments AnnouncementVersion only when audio is required. Passive displays reload it after SignalR, reconnect, and periodic polling. It is authoritative projection state but cannot reconstruct history.

**Implementation requirement:** define columns/key, atomic update with queue mutation, clear/replace semantics, configurable polling interval, and read-access policy. Ordinary refresh and service-state change must not increment AnnouncementVersion unless audio is explicitly required. No managed display identity or scope is implied.

### GAP-READY-005 — Final Registration Outcome semantics resolved by Decision C

**Status:** Resolved at domain/architecture level; persistence, identity mapping, API, and ReasonCode catalog remain pending.

Decision C establishes a durable Admisi Rajal Registration Outcome with OutcomeId, QueueEntryId, Result, conditional RegId/ReasonCode, Explanation, DecidedAt, and DecidedBy. Validation errors are non-final; correctable validation leaves the Queue Entry InService. Explicit final `NotEstablished` may complete the Queue Entry as Done while it remains anonymous.

Current code still implements only successful Registration completion and has no Registration Outcome aggregate/persistence or negative completion command.

**Implementation requirement:** add the Admisi Rajal outcome aggregate/repository and atomic Patient Tracker completion contract. Resolve the ReasonCode catalog/owner and how Decision C's QueueEntryId maps to existing `(AntrianId, NoUrut)` before final persistence/API generation. Do not infer an outcome or create a Tracker for NotEstablished.

### GAP-READY-006 — Work List ownership and composition resolved by Decision D

**Status:** Resolved at artifact level; implementation pending.

Patient Tracker owns the queue-only `Admission Queue Worklist Projection`: Queue Label, Service Point, state, call state, LoketKey, queue timestamps, Priority indicator, and optional TrackerId. Priority may affect sorting but not auto-selection. The projection must not contain Booking, identity, Registration, or administrative enrichment.

The Admission Module or an Admisi Rajal application query composes that projection with Admisi-owned context to produce the enriched `Admisi Rajal Work List`. This is a read composition, not a second ledger. Choosing whether the composition is delivered directly in the Admission Module or behind an Admisi Rajal application query is an API/deployment choice, not an ownership ambiguity.

**Implementation requirement:** give UC-AQO-010 a purpose-built queue-only contract and DAL; verify its field boundary; compose enrichment through owning application contracts without direct cross-context table access or duplicated queue state.

### GAP-READY-007 — Active configured-Loket concurrency still needs a database claim

**Severity:** Blocker for safe multi-Service-Point calling.

Every configured Loket may serve every Service Point, while BR-AQO-020 permits exactly one current Outstanding or InService entry per LoketKey. Those entries can belong to different Queue Sessions. Removing Loket master/authorization tables does not remove this concurrency invariant.

The architecture says database uniqueness must support the invariant, but it does not specify:

- where the active Loket claim is stored after a Queue Call is acknowledged;
- how Outstanding and InService share one exclusivity key;
- how the claim is released on no-show, withdrawal, Priority redirection, completion, and rollback; or
- the exact conditional SQL/index strategy.

**Required resolution:** provide a persistence contract for the Loket active-work claim and every acquire/release transition. Domain/Application decide the transition; SQL conditional writes protect it across processes.

### GAP-READY-008 — LastQueueNumber is decided; optional idempotency remains conditional

**Severity:** Blocker for production Kiosk intake.

The developer decision replaces per-Service Point SQL sequence design with LastQueueNumber on the dedicated daily Queue Session row. That resolves sequence ownership but requires an atomic database increment under concurrent intake.

ClientRequestId is stored optionally on Queue Entry. Therefore:

- a supplied identifier can prevent duplicate issuance only if a database uniqueness scope and same-request lookup are defined;
- a request without the identifier is not idempotent and may create duplicates after an uncertain response; and
- the identifier's maximum length, case/collation, global versus scoped uniqueness, and conflicting-payload behavior remain unspecified.

**Required resolution:** define the atomic LastQueueNumber algorithm and ClientRequestId schema/unique index/scope. API documentation must call idempotency conditional, not guaranteed, while ClientRequestId remains optional.

### GAP-READY-009 — Persistence additions are an inventory, not an implementable contract

**Severity:** Blocker before migration generation or DAL/repository implementation.

The architecture intentionally defers columns and indexes. That is appropriate for an architecture artifact, but an implementation agent still needs an approved persistence design covering:

- table names and aggregate ownership;
- key sizes and immutable identities;
- status numeric values, especially additive `Withdrawn`;
- active/retired and effective-period representation;
- Queue Prefix Snapshot, Priority, CreationReason values, and nullable SourceAntrianEntryId constraints;
- current call fields, CallCount, and current-display AnnouncementVersion;
- optional ClientRequestId shape and uniqueness scope;
- expected-state update predicates and affected-row contracts;
- audit columns, void/retention treatment, and operational indexes; and
- additive deployment/backfill/rollback order.

**Required resolution:** create a persistence contract artifact before generating SQL. Do not extend whole-aggregate delete/update behavior to new historical records.

### GAP-READY-010 — API, identity, and compatibility contracts are deferred beyond safe transport implementation

**Severity:** Blocker for API/client delivery and replacement of existing endpoints.

The architecture intentionally leaves exact routes and payloads to a later API artifact. In addition, current clients of `anonymous-intake`, `start`, and Registration admission fields are not inventoried. New device credentials, claims, error payloads, request-id rules, and version/deprecation behavior are also unspecified.

**Required resolution:** produce an API/security contract and consumer migration inventory before changing endpoint semantics or allowing Kiosk/display clients into production. Preserve the compatibility endpoint behind an explicit gate until consumers are verified.

### GAP-READY-011 — Booking Self-Registration assistance orchestration is missing

**Severity:** Blocker for the Booking Kiosk path and BR-ARJ-015.

The SOP applies after Booking Self-Registration requires assistance. Admisi Rajal owns the decision that Registration was established or assistance is required, while Patient Tracker owns queue issuance. The architecture exposes a generic Kiosk issue-entry command but does not define the Admisi Rajal orchestration that:

The later developer simplifications for Loket, Kiosk, and Queue Display still do not define this Booking Self-Registration assistance orchestration or its business deduplication key, so this gap remains open.

- attempts or evaluates Self-Registration;
- issues no admission Queue Entry when Registration succeeds;
- requests anonymous admission intake only for an accountable assistance outcome; and
- prevents more than one active Registration Assistance obligation for the same unresolved Booking attempt.

Request idempotency per Kiosk prevents retry duplication only when the same request identity is reused. It does not enforce the business rule when a second request identity is submitted for the same Booking assistance obligation.

**Required resolution:** define an Admisi Rajal-owned Kiosk decision use case and a narrow Patient Tracker intake contract. Define the stable assistance correlation/deduplication key without creating a second Admisi-owned queue ledger. Keep the physician Queue Number and admission Queue Label independent.

## 6. Important non-blocking gaps and hardening work

| ID | Gap | Why it matters | Safe treatment |
|---|---|---|---|
| GAP-READY-012 | Service Point active period is not defined as dates versus active/retired state | Affects future scheduling and history | Use simple active/retired for V1 unless effective dating is separately approved. |
| GAP-READY-013 | Current human commands trust `UserId` in request bodies | Weak accountability and authorization | New commands use `ICurrentUserContext`; compatibility fields are ignored or validated during migration. |
| GAP-READY-014 | Existing identified InService → Done is not conditional | Concurrent completion can overwrite a competing transition | Add expected-state repository operation before relying on it in target flows. |
| GAP-READY-015 | Whole-aggregate save can physically delete absent entries | Violates target historical truth | Prohibit removal for admission operations; replace active transitions with purpose-built repository operations. |
| GAP-READY-016 | Historical prefix backfill is unresolved | Prevents trustworthy legacy labels | Do not fabricate labels; expose legacy number separately until an approved mapping exists. |
| GAP-READY-017 | Manual no-show/progression semantics are resolved; detailed operational policy remains external | CallCount cannot prove attempt times, no-show outcomes, or “five subsequent patients” | CallCount is informational only; keep decisions manual and never automate from it. |
| GAP-READY-018 | Multi-instance SignalR topology, display access, and polling configuration remain implementation/deployment work | Refresh hints may be missed or reach only some instances | Persist current state/version, reload after reconnect, poll at configured N, and do not claim durable delivery. |
| GAP-READY-019 | Kiosk print status/reprint authorization has no backend contract | Affects support and auditability | Printing stays outside queue transaction; define later whether print attempts are client-only telemetry or durable operational records. |
| GAP-READY-020 | Existing queue SQL lacks standard audit columns | Current schema does not meet `docs/DATABASE.md` | Approve additive audit migration/equivalent; all new transaction tables follow the standard. |
| GAP-READY-021 | Real database volume/concurrency evidence is absent | Unit tests cannot prove multi-operator safety | Add SQL integration, race, volume, rollback, and worker recovery tests per increment. |

## 7. Safe implementation envelope

An agent may safely perform the following before every blocker is resolved, provided changes are isolated and do not claim production readiness:

1. preserve and expand tests for anonymous intake, anonymous InService, existing-Tracker selection, Registration-owned Tracker creation, and transaction rollback;
2. replace caller `UserId` use in new human commands with `ICurrentUserContext` without changing business policy;
3. add explicit expected-state repository operations for existing lifecycle transitions and remove admission paths from unrestricted whole-aggregate updates;
4. introduce transport-independent ports for workstation configuration, best-effort refresh, and projections without inventing claims or routes;
5. implement the dedicated Queue Session and current-display persistence behind integration tests after the remaining column/index contract is approved;
6. prepare read-only investigation queries and migration preflight checks that do not mutate operational data; and
7. create the missing persistence, API/security, and rollout artifacts.

An agent must not yet:

- accept a Kiosk/client-supplied authoritative Business Date or create a second same-date session after exhaustion;
- treat Kiosk-local offerings as server authorization or accept an inactive/unknown ServicePointId;
- accept arbitrary request-body LoketId outside the controlled workstation-configuration adapter;
- mark a failed registration as a completed queue service without an accountable outcome contract;
- implement a second Admisi Work List ledger;
- use in-memory locks to protect multi-instance queue invariants;
- claim SignalR delivery is durable or use AnnouncementVersion without atomic current-state update;
- infer detailed attempt/no-show history from CallCount;
- issue a Priority replacement while leaving the origin active;
- infer historical prefixes from descriptions; or
- replace or remove current endpoints without a consumer inventory.

## 8. Readiness by architecture increment

| Architecture increment | Readiness | Blocking items | Safe next action |
|---|---|---|---|
| 1. Service Point/configuration authority | Conditional | GAP-READY-009, 010; prefix backfill; workstation integrity | Implement Service Point persistence and explicit configuration adapters; no Loket/Kiosk/Display masters. |
| 2. Retry-aware labelled intake | No-Go | GAP-READY-008, 009, 010, 011 | Implement atomic LastQueueNumber; define optional ClientRequestId contract and Booking-assistance orchestration before production. |
| 3. Operational projections | Partial | GAP-READY-009, 010 | Implement Decision D worklist plus shared current-per-Loket state/version and define display read policy. |
| 4. Call and service transitions | No-Go | GAP-READY-007, 009, 010 | Define database active-work claim, controlled workstation Loket source, CallCount updates, and transport contract. |
| 5. Best-effort display refresh | Partial foundation only | GAP-READY-009, 010, 018 | Current-state/version plus polling is implementable; topology, polling interval, access policy, and observability still gate production. |
| 6. Journey and Registration alignment | Conditional | GAP-READY-009, 010, 013, 014; ReasonCode catalog and QueueEntryId mapping | Preserve the passing late-identification baseline; implement Decision C persistence, claim-derived decision identity, both final outcomes, and conditional completion. |
| 7. Exceptions | Partial | GAP-READY-007, 009; no-show/redirection safety | Keep no-show procedural; require explicit origin disposition and preferably atomic Priority replacement. |
| 8. Hardening and rollout | No-Go | All deployment, client, retention, migration, and performance gates | Execute only after prior increments and external decisions are verified. |

## 9. Required resolution order

### Gate 0 — Stabilize the baseline

- checkpoint the current dirty working tree;
- identify which late-identification changes are intended baseline;
- record the commit used by implementation and verification.

### Gate 1 — Close business semantics

- implement and verify the approved Queue Label grammar from Decision A;
- implement and verify the approved Queue Session and Business Date policy from Decision B;
- implement and verify the approved final Registration Outcome semantics from Decision C;
- implement and verify the Decision D queue-only projection and read-only Admisi Rajal Work List composition;
- define Booking Self-Registration-to-assistance orchestration and business deduplication;
- approve default no-parallel-service behavior and manual no-show position.

### Gate 2 — Close authority and security

- specify and verify trusted workstation Loket configuration, validation, and audit logging;
- specify passive display read access, shared current-state/version semantics, and polling recovery;
- define Kiosk/passive-display API or network access policy despite the absence of device masters;
- define administrator/supervisor/officer policies.

### Gate 3 — Approve persistence and concurrency design

- publish tables, columns, enum values, indexes, CAS operations, idempotency algorithm, audit fields, and migration order;
- explicitly protect cross-session active Loket work;
- validate atomic LastQueueNumber allocation and concurrent lazy session creation;
- define prefix backfill and rollback.

### Gate 4 — Approve API and rollout contracts

- inventory current consumers;
- publish versioned commands, queries, responses, errors, and idempotency semantics;
- select deployment topology and SignalR scale-out approach;
- approve retention and operational monitoring.

### Gate 5 — Implement incrementally

For each increment: Domain tests → Application tests → SQL/DAL integration tests → API/security tests → concurrency/rollback tests → operational verification. Do not advance merely because unit tests are green.

## 10. Final assessment

The target architecture is a strong architecture **analysis**, but it is not yet a complete implementation contract. Its major boundaries are safe, and the current late-identification work provides a viable base. The remaining risk is not primarily coding difficulty; it is the possibility that an agent will fill policy and authority gaps with plausible but unauthorized behavior.

The implementation can proceed safely only as gated slices. Decisions A–D remain intact; conflicting parts of Decision E are superseded by the developer-approved simplified V1 profile. Queue Session, configured Loket/Kiosk, passive display, CallCount, Priority redirection, optional ClientRequestId, post-commit SignalR, and universal Loket service access now have direction, but production implementation still requires the persistence, concurrency, API/security, migration, polling, and deployment contracts identified above.
