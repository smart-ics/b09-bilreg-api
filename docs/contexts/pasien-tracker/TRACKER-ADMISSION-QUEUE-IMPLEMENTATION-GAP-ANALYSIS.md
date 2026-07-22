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

1. Queue Label formatting is business-significant but undefined.
2. Admission Queue Session establishment and operating-interval selection are undefined.
3. trusted workstation-to-Loket assignment is required for authorization but is not modeled as an owned capability.
4. Queue Display identity and announcement-scope assignment are required but are not modeled.
5. completion after `Registration Not Established` has no explicit application contract or durable outcome reference.
6. the relationship between the Patient Tracker queue projection and the Admisi Rajal enriched Work List is ambiguous.
7. conditional persistence and database constraints are described as goals, but the concrete persistence contract needed to protect cross-session Loket and idempotency invariants is absent.
8. device/API contracts, compatibility consumers, migration values, and production topology are not yet resolved.
9. the Booking Self-Registration → Assistance decision and business-level duplicate-assistance guard are not represented in the architecture use cases.

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
| Anonymous intake | Aligned | Queue intake does not create or select a Patient Tracker. |
| Late identification | Aligned | Existing journey selection and Registration-owned new Walk-In Tracker creation follow the parent Tracker and Admisi Rajal rules. |
| Call versus service start | Aligned target; not current runtime | Domain, SOP, and architecture distinguish them. Current `AdmissionQueueStartCmd` directly moves Waiting to InService and has no Queue Call or Loket. |
| Registration versus queue completion | Partly aligned | Ownership is explicit, but the application contract for completing after `Registration Not Established` is missing. |
| Work List | Ambiguous | Admisi Rajal defines its Work List as Patient Tracker queue truth enriched with Admisi context. Architecture defines a Patient Tracker `Admission Worklist View` without saying where enrichment/composition occurs. |
| Operational events | Aligned | Events are business facts; direct orchestration and a dedicated notification outbox avoid a speculative generic event bus. |
| Display authority | Aligned in principle | Queue Display is read-only and snapshot recovery is authoritative, but display identity/scope administration is not modeled. |
| Audit versus operational history | Aligned | Queue Call history remains operational truth; exceptional/admin decisions use compliance audit. |
| Historical retention | Aligned | Withdraw/transfer preserve historical entries and calls rather than deleting them. |
| Physician queue compatibility | Aligned | Admission queue numbering remains separate from the legacy physician `AntrianMap` compatibility adapter. |
| Global queue flexibility | Mostly aligned | Manual selection and supervised exceptions fit `docs/WORKFLOW.md`; the architecture correctly avoids automatic re-selection after a conflict. |

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
- no SignalR registration, hub, worker, or queue notification outbox exists;
- controllers use generic `[Authorize]` only;
- current admission commands still accept caller-supplied `UserId`;
- anonymous intake accepts caller-supplied Service Point code/name and optional business date;
- current start has no call, Loket, assignment, or contextual authorization precondition;
- whole-aggregate `AntrianRepo.SaveChanges` still performs unrestricted updates and can physically delete missing entries;
- identified InService → Done still uses an unrestricted whole-aggregate save;
- current queue tables do not contain the global transaction audit columns;
- current Service Point session factory always uses `00:00` through `23:59:59.9999999`, not an approved operational interval; and
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

## 5. Blocking gaps to resolve before the affected implementation slice

### GAP-READY-001 — Queue Label grammar is undefined

**Severity:** Blocker for labelled intake, display, reprint, prefix validation, and migration.

The domain requires a stable Queue Label formed from Queue Prefix Snapshot plus a **formatted** Queue Number, but no artifact defines padding width, separator, allowed prefix characters, case normalization, maximum length, overflow behavior, or examples that are declared normative.

The architecture assigns Queue Label formation to Domain, so an agent cannot defer this as a UI detail without violating the business specification.

**Required resolution:** add a canonical Queue Label value-object contract, for example its accepted prefix grammar, normalization, numeric formatting, maximum number, output examples, and validation errors. Do not allow each client to format the label independently.

### GAP-READY-002 — Queue Session operating policy is undefined

**Severity:** Blocker for resource-backed intake and deterministic session lookup.

The parent domain defines a Queue Session by Service Point, Session Date, Start Time, and End Time. The SOP requires an applicable session to be available. The architecture says intake will “find/create” a session, but neither the Service Point nor Kiosk model owns operating hours, and no application policy explains:

- who establishes a session;
- whether one Service Point has one or several intervals per business date;
- whether intake outside the interval is rejected;
- how overnight intervals behave;
- how business date is selected; or
- whether a Kiosk may supply a date.

Current code silently creates an all-day session and accepts optional caller-supplied `TglYmd`. That is transitional behavior, not enough to implement the target safely.

**Required resolution:** define session establishment authority and lookup key. Prefer server-derived business date and an explicit configured/maintained interval; document any all-day V1 policy as an approved rule rather than an implementation accident.

### GAP-READY-003 — Workstation/session-to-Loket assignment has no owner or persistence model

**Severity:** Blocker for call, recall, start, worklist scoping, and the “one active entry per Loket” invariant.

The SOP requires one active Loket assignment for the officer's workstation or session. The architecture correctly rejects a caller-supplied arbitrary `LoketId`, but only lists Service Point, Loket authorization, and Kiosk offering persistence. It does not define the aggregate, record, use cases, lifecycle, or repository that make the trusted assignment authoritative.

**Required resolution:** identify the owner and model for assignment, including assignment subject (workstation, login session, user, or device), uniqueness, activation/expiry, supervisor actions, audit, and how handlers resolve it. Add it to module boundaries, use cases, persistence additions, and tests.

### GAP-READY-004 — Queue Display resource and announcement scope are not modeled

**Severity:** Blocker for production display authorization and subscription scoping.

The architecture requires authenticated display identity and an approved announcement scope, but there is no Display aggregate/catalog, display-to-scope assignment, administration use case, or persistence entry. `GAP-AQO-002` and `GAP-AQO-004` acknowledge policy and credential gaps but do not close the ownership gap.

**Required resolution:** define the display resource identity and scope-assignment authority, or explicitly delegate them to an existing identity/device platform with a concrete application port and claims contract.

### GAP-READY-005 — `Registration Not Established` cannot yet complete queue service through an explicit contract

**Severity:** Blocker for the SOP's negative registration outcome and BR-AQO-024.

The SOP permits Queue Entry completion after either Registration Established or an accountable `Registration Not Established` outcome. Current code completes admission queue service inside successful Walk-In/Booking Registration creation and records `REGISTER` by `RegId`. The architecture's UC-AQO-019 and transaction table are also phrased around Registration completion/source identities.

No use case defines how a failed-but-accountably-resolved registration attempt is identified, who records the outcome, what stable reference/reason is retained, or how an anonymous InService entry can validly become Done without a new Tracker when no source activity established one.

**Required resolution:** define an explicit accountable Registration Outcome contract. It must distinguish correctable validation failure (remain InService) from final `Registration Not Established` (eligible for Done), identify its authority and evidence reference, and state whether the Queue Entry may remain anonymous when completed.

### GAP-READY-006 — Patient Tracker projection versus Admisi Rajal Work List composition is ambiguous

**Severity:** Blocker for UC-AQO-010 API/read-model ownership.

Admisi Rajal defines its Work List as active Patient Tracker Queue Entries enriched with Admisi Rajal context. The architecture defines `Admission Worklist View` under Patient Tracker and says it is sourced from Queue Session/Entry/Call only.

Both can be valid if the Patient Tracker projection is explicitly a queue worklist slice and the Admission Module or an Admisi Rajal query composes the enrichment. Without that statement, an agent may either duplicate Admisi-owned fields in Patient Tracker or deliver a projection that does not satisfy the canonical Admisi Rajal Work List.

**Required resolution:** name the queue-only projection distinctly and define the composition boundary, query owner, and allowed cross-context contract.

### GAP-READY-007 — Active-Loket concurrency spans Queue Sessions but the consistency mechanism is incomplete

**Severity:** Blocker for safe multi-Service-Point calling.

One Loket may serve multiple Service Points, while BR-AQO-020 permits only one outstanding or InService entry at a Loket by default. Those entries can belong to different Queue Session aggregates. A single Queue Session aggregate cannot enforce this cross-session invariant in memory.

The architecture says database uniqueness must support the invariant, but it does not specify:

- where the active Loket claim is stored after a Queue Call is acknowledged;
- how Outstanding and InService share one exclusivity key;
- how the claim is released on no-show, withdrawal, transfer, completion, and rollback; or
- the exact conditional SQL/index strategy.

**Required resolution:** provide a persistence contract for the Loket active-work claim and every acquire/release transition. Domain/Application decide the transition; SQL conditional writes protect it across processes.

### GAP-READY-008 — Intake sequencing and idempotency need a concrete concurrency contract

**Severity:** Blocker for production Kiosk intake.

The architecture requires one result per `(KioskId, RequestId)` and preserves `ISequencer` initially. Current `Sequencer`:

- opens its own connection;
- consumes SQL Server sequence values outside rollback semantics;
- creates a sequence dynamically on first use; and
- interpolates a sequence identifier ultimately derived from caller-supplied Service Point code.

Gaps in queue numbers may be acceptable, but first-use creation races, identifier validation, retry ordering, and the relationship between idempotency insertion and number allocation are not specified. The current caller-derived identifier must not be exposed to a device client.

**Required resolution:** define the exact intake transaction algorithm, safe server-owned sequence key, first-use provisioning/race handling, duplicate-request lookup, unique indexes, and recovery behavior. State explicitly that numbers are unique but not gapless if SQL sequences remain in use.

### GAP-READY-009 — Persistence additions are an inventory, not an implementable contract

**Severity:** Blocker before migration generation or DAL/repository implementation.

The architecture intentionally defers columns and indexes. That is appropriate for an architecture artifact, but an implementation agent still needs an approved persistence design covering:

- table names and aggregate ownership;
- key sizes and immutable identities;
- status numeric values, especially additive `Withdrawn`;
- active/retired and effective-period representation;
- Queue Prefix Snapshot and transfer linkage;
- Queue Call/Attempt ordering and dispositions;
- intake idempotency result shape;
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

- attempts or evaluates Self-Registration;
- issues no admission Queue Entry when Registration succeeds;
- requests anonymous admission intake only for an accountable assistance outcome; and
- prevents more than one active Registration Assistance obligation for the same unresolved Booking attempt.

Request idempotency per Kiosk prevents retry duplication only when the same request identity is reused. It does not enforce the business rule when a second request identity is submitted for the same Booking assistance obligation.

**Required resolution:** define an Admisi Rajal-owned Kiosk decision use case and a narrow Patient Tracker intake contract. Define the stable assistance correlation/deduplication key without creating a second Admisi-owned queue ledger. Keep the physician Queue Number and admission Queue Label independent.

## 6. Important non-blocking gaps and hardening work

| ID | Gap | Why it matters | Safe treatment |
|---|---|---|---|
| GAP-READY-012 | Resource authorization/offering “active period” is not defined as dates versus active/retired state | Affects future scheduling and history | Use simple active/retired only if explicitly approved for V1; otherwise define effective dates. |
| GAP-READY-013 | Current human commands trust `UserId` in request bodies | Weak accountability and authorization | New commands use `ICurrentUserContext`; compatibility fields are ignored or validated during migration. |
| GAP-READY-014 | Existing identified InService → Done is not conditional | Concurrent completion can overwrite a competing transition | Add expected-state repository operation before relying on it in target flows. |
| GAP-READY-015 | Whole-aggregate save can physically delete absent entries | Violates target historical truth | Prohibit removal for admission operations; replace active transitions with purpose-built repository operations. |
| GAP-READY-016 | Historical prefix backfill is unresolved | Prevents trustworthy legacy labels | Do not fabricate labels; expose legacy number separately until an approved mapping exists. |
| GAP-READY-017 | No-show threshold and display retention are unresolved | Blocks automation/final UI, not manual facts | Keep explicit supervisor disposition and persist full call history. |
| GAP-READY-018 | Notification retention and multi-instance SignalR topology are unresolved | Affects production operations and capacity | Outbox/snapshot abstractions can be built; production rollout waits for topology and retention decisions. |
| GAP-READY-019 | Kiosk print status/reprint authorization has no backend contract | Affects support and auditability | Printing stays outside queue transaction; define later whether print attempts are client-only telemetry or durable operational records. |
| GAP-READY-020 | Existing queue SQL lacks standard audit columns | Current schema does not meet `docs/DATABASE.md` | Approve additive audit migration/equivalent; all new transaction tables follow the standard. |
| GAP-READY-021 | Real database volume/concurrency evidence is absent | Unit tests cannot prove multi-operator safety | Add SQL integration, race, volume, rollback, and worker recovery tests per increment. |

## 7. Safe implementation envelope

An agent may safely perform the following before every blocker is resolved, provided changes are isolated and do not claim production readiness:

1. preserve and expand tests for anonymous intake, anonymous InService, existing-Tracker selection, Registration-owned Tracker creation, and transaction rollback;
2. replace caller `UserId` use in new human commands with `ICurrentUserContext` without changing business policy;
3. add explicit expected-state repository operations for existing lifecycle transitions and remove admission paths from unrestricted whole-aggregate updates;
4. introduce transport-independent ports for actor/device identity, notification delivery, and projections without inventing claims or routes;
5. prototype resource aggregates behind tests after identity and lifecycle assumptions are stated, but do not deploy catalog authority until prefix/session/backfill decisions are approved;
6. prepare read-only investigation queries and migration preflight checks that do not mutate operational data; and
7. create the missing persistence, API/security, and rollout artifacts.

An agent must not yet:

- finalize Queue Label formatting;
- invent admission opening hours or session rollover;
- trust request-body Kiosk, Display, Loket, Service Point, date, or User identity as authority;
- invent workstation assignment or display announcement scopes;
- mark a failed registration as a completed queue service without an accountable outcome contract;
- implement a second Admisi Work List ledger;
- use in-memory locks to protect multi-instance queue invariants;
- expose SignalR groups chosen by callers;
- infer historical prefixes from descriptions; or
- replace or remove current endpoints without a consumer inventory.

## 8. Readiness by architecture increment

| Architecture increment | Readiness | Blocking items | Safe next action |
|---|---|---|---|
| 1. Resource authority | Conditional | GAP-READY-001, 002, 003; prefix scope/backfill | Resolve resource/session/assignment contracts; then build aggregates and persistence. |
| 2. Idempotent labelled intake | No-Go | GAP-READY-001, 002, 008, 009, 010, 011; device identity | Approve Queue Label, session, intake transaction, Booking-assistance orchestration, and Kiosk auth/API contracts. |
| 3. Operational projections | Partial | GAP-READY-004, 006, 009, 010 | Define queue-only versus enriched worklist and display scope; then implement read models. |
| 4. Call and service transitions | No-Go | GAP-READY-003, 007, 009, 010 | Define trusted Loket resolution and database active-work claim first. |
| 5. Durable display delivery | Partial foundation only | GAP-READY-004, 009, 010, 018 | Outbox/transport ports and snapshot design are safe; production hub/scoping is not. |
| 6. Journey and Registration alignment | Mostly ready for bounded hardening | GAP-READY-005, 010, 013, 014 | Preserve the passing late-identification baseline; add claim-derived actor identity, negative-outcome contract, and conditional completion. |
| 7. Exceptions | Partial | GAP-READY-001, 007, 009; no-show policy | Explicit supervised no-show/transfer can follow after persistence and label rules; no automation yet. |
| 8. Hardening and rollout | No-Go | All deployment, client, retention, migration, and performance gates | Execute only after prior increments and external decisions are verified. |

## 9. Required resolution order

### Gate 0 — Stabilize the baseline

- checkpoint the current dirty working tree;
- identify which late-identification changes are intended baseline;
- record the commit used by implementation and verification.

### Gate 1 — Close business semantics

- approve Queue Label grammar;
- approve Queue Session/open-hours/business-date policy;
- define final `Registration Not Established` evidence and queue completion behavior;
- clarify queue-only projection versus enriched Admisi Rajal Work List;
- define Booking Self-Registration-to-assistance orchestration and business deduplication;
- approve default no-parallel-service behavior and manual no-show position.

### Gate 2 — Close authority and security

- model trusted workstation/session-to-Loket assignment;
- model or delegate Queue Display identity and announcement scope;
- define Kiosk/Display credential claims and rotation;
- define administrator/supervisor/officer policies.

### Gate 3 — Approve persistence and concurrency design

- publish tables, columns, enum values, indexes, CAS operations, idempotency algorithm, audit fields, and migration order;
- explicitly protect cross-session active Loket work;
- validate sequencer provisioning and concurrency behavior;
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

The implementation can proceed safely only as gated slices. The most immediately viable slice is hardening Journey/Registration alignment already present in the workspace. Resource-backed intake, Call/Loket operation, Queue Display, and production rollout must wait for the corresponding business, security, persistence, and API decisions identified above.
