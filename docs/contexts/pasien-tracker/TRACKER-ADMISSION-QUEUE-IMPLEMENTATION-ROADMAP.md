# Patient Tracker — Admission Queue Implementation Roadmap

**Status:** Actionable implementation plan

**Primary source:** [TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-GAP-ANALYSIS.md](./TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-GAP-ANALYSIS.md)

**Business specification:** [TRACKER-ADMISSION-QUEUE-DOMAIN.md](./TRACKER-ADMISSION-QUEUE-DOMAIN.md)

**Target architecture:** [TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md](./TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md)

**Prepared:** 2026-07-22

## 1. Delivery position

End-to-end implementation remains a conditional no-go. Bounded foundations can proceed, but production intake, calling, display, and Booking-assistance delivery depend on contracts that are not yet complete.

The roadmap uses the approved Pragmatic V1 profile: reuse `BILRG_Antrian` and `BILRG_AntrianEntry`; retain `ISequencer`; add only a Service Point master and latest-state `BILRG_AdmLoketCurrentCall`; treat Loket and Kiosk as deployment configuration; keep Call separate from Start Service; use best-effort SignalR plus polling; and avoid call-history, Loket, Kiosk, display, assignment, and transfer masters.

Priority means implementation order:

- **Blocker:** unsafe or impossible to release the dependent capability without closure.
- **High:** required for a coherent V1 workflow or to remove a material integrity/security risk.
- **Medium:** required for production operability or an important secondary workflow.
- **Low:** deferrable hardening or a deliberately limited V1 capability.

Solvability means:

- **Ready to implement:** approved semantics and sufficient local evidence exist.
- **Requires design decision:** an authoritative behavioral or technical choice is still missing.
- **Requires additional artifact:** the choice is mostly known but must be made executable through a persistence, API, migration, or rollout contract.
- **External dependency:** closure requires a policy, platform, product, compliance, or deployment owner.

## 2. Gap roadmap

### R-00 — Preserve the assessed baseline

**Source:** Gap analysis §2.1 and Gate 0  
**Priority:** Blocker  
**Solvability:** Requires additional artifact

**Status:** Completed on 2026-07-22

**Evidence:** [R-00 baseline implementation report](./tracker-admission-queue-r00-baseline-implementation-report.md)

**Why it exists:** The assessed tracker and admission artifacts are modified in the working tree. Implementation cannot reliably distinguish the accepted late-identification baseline from new work.

**Capability affected:** Safe incremental delivery, review, rollback, and reproducible verification.

**Risk if unresolved:** Existing work may be overwritten or attributed to the wrong slice; test results will not identify a reproducible source revision.

**Pragmatic solution:** The repository owner checkpoints the intended baseline and records its commit in the first implementation report. Do not mix semantic artifact edits with the first code slice.

**Tasks and layers:**

- **Delivery:** review the current diff and identify intended late-identification changes.
- **Git/release:** commit or otherwise checkpoint the accepted baseline.
- **Documentation:** record baseline commit, focused test command, and result.

**Resolution:** The accepted baseline is commit `2bac008b1da15946e7aa2fd23429fefdfacc8835`. The documented focused regression selection passed 17/17 tests from a clean working tree.

**Prerequisites:** None. Gate satisfied; R-01 and R-02 may begin from the recorded baseline.

---

### R-01 — Replace unsafe admission lifecycle persistence

**Source:** GAP-READY-014, 015, 021  
**Priority:** Blocker  
**Solvability:** Ready to implement

**Status:** Completed on 2026-07-22

**Evidence:** [R-01 CAS persistence implementation report](./tracker-admission-queue-r01-cas-persistence-implementation-report.md)

**Why it exists:** Admission lifecycle paths still use unrestricted whole-aggregate saves. Those saves can overwrite concurrent changes and physically delete entries omitted from the in-memory collection.

**Capability affected:** Existing anonymous intake, service start, Journey association, Registration completion, and every future Call/Withdraw/Redirect transition.

**Risk if unresolved:** Lost updates and deletion of queue history under concurrent operation.

**Pragmatic solution:** Extend the proven compare-and-set pattern used by anonymous InService association. Add explicit expected-state repository operations per transition; prohibit collection removal as an admission lifecycle mechanism. Do not create a generic transition framework.

**Tasks and layers:**

- **Domain:** preserve intention-revealing transition methods and add `Withdrawn` only when its persistence slice begins.
- **Application:** extend `IAntrianRepo` with narrow conditional operations; convert zero affected rows to `AdmissionQueueConcurrencyException`.
- **Infrastructure:** add explicit conditional Dapper updates to `AntrianEntryDal`; avoid `AntrianRepo.SaveChanges` for active admission transitions.
- **Database:** no initial schema change is required for current-state CAS; use status and expected milestone predicates.
- **API:** map concurrency conflict consistently to HTTP 409 when transport work begins.
- **Tests:** handler conflict, affected-row, rollback, and real SQL race tests; regression tests proving no physical delete.

**Prerequisites:** R-00.

---

### R-02 — Use authenticated actor identity for new human commands

**Source:** GAP-READY-013 and GAP-READY-010  
**Priority:** High  
**Solvability:** Ready to implement for Application; API policy remains in R-10

**Why it exists:** Existing queue commands accept caller-supplied `UserId` despite an implemented `ICurrentUserContext`.

**Capability affected:** Accountable Call, Start Service, identification, completion, withdrawal, redirection, and administration.

**Risk if unresolved:** Actor spoofing and unreliable operational audit.

**Pragmatic solution:** New human commands derive the actor from `ICurrentUserContext`. Preserve legacy request fields temporarily, but ignore or validate them during compatibility migration.

**Tasks and layers:**

- **Application:** inject `ICurrentUserContext`; remove request identity as write authority.
- **API:** keep authentication mandatory; defer role/policy names to R-10.
- **Infrastructure/audit:** persist and log the resolved actor on mutations.
- **Tests:** prove request-body identity cannot override claims identity.

**Prerequisites:** R-00. Parallelizable with R-01.

---

### R-03 — Publish and implement the additive persistence contract

**Source:** GAP-READY-007, 008, 009, 020 and Gate 3  
**Priority:** Blocker  
**Solvability:** Requires additional artifact

**Why it exists:** Existing queue tables are reusable, but exact columns, defaults, indexes, conditional operations, audit treatment, backfill, and rollback for V1 additions are not migration-ready.

**Capability affected:** Service Point authority, immutable Queue Labels, idempotent intake, Priority provenance, calling, current display state, and concurrency safety.

**Risk if unresolved:** Incorrect labels, duplicate sessions or requests, incompatible migrations, lost current-call state, and unsafe multi-process behavior.

**Pragmatic solution:** Produce one explicit persistence contract, then implement additive migrations in dependency order. Retain composite Queue Entry identity `(AntrianId, NoUrut)` and `ISequencer`; do not add `LastQueueNumber`, surrogate QueueEntryId, or new resource masters.

**Contract must specify:**

- `BILRG_AdmServicePoint`: key, name, normalized `VARCHAR(1)` prefix, Active/Retired status, audit columns, and filtered/conditional active-prefix uniqueness strategy.
- `BILRG_Antrian`: immutable prefix snapshot, authoritative Business Date interpretation, and unique `(ServicePointCode, AntrianDate)` admission session constraint.
- `BILRG_AntrianEntry`: `Priority`, `CallCount`, call state/Loket as required by the chosen active-claim design, `CreationReason`, composite source fields, optional `ClientRequestId`, audit treatment, and `RowVersion` where required.
- `BILRG_AdmLoketCurrentCall`: one latest row per LoketKey, composite entry reference, display fields, `AnnouncementVersion`, `RowVersion`, audit columns, and replace/clear rules.
- filtered unique `(AntrianId, ClientRequestId)` for non-empty IDs and check constraints for composite provenance.
- expected-state SQL for acquire/release/transition, migration preflight, historical-prefix handling, deployment order, and rollback.

**Tasks and layers:**

- **Documentation/database design:** publish exact SQL types, lengths, defaults, indexes, constraints, CAS statements, backfill, and rollback.
- **Database:** additive scripts only; no fabricated historical prefix.
- **Infrastructure:** DTO, DAL, and repository mapping with explicit SQL.
- **Application:** repository ports only; no DAL contracts in Application.
- **Tests:** schema, mapping, uniqueness, rollback, concurrent lazy creation, and transition race integration tests.

**Prerequisites:** R-00; R-04 must decide the active Loket claim before the current-call portion is final. R-05 and R-06 supply sequencing/idempotency and prefix-backfill details.

---

### R-04 — Define the cross-session active Loket claim

**Source:** GAP-READY-007 and GAP-AQO-004  
**Priority:** Blocker  
**Solvability:** Requires design decision

**Why it exists:** A Loket may serve all Service Points, so its Outstanding or InService entry can be in any Queue Session. No persistence location currently enforces one active patient per Loket across sessions.

**Capability affected:** Call, Recall, Start Service, No-Show, Withdraw, Redirect, Complete, and rollback.

**Risk if unresolved:** One physical Loket can concurrently claim multiple patients, or a stale claim can block operation indefinitely.

**Pragmatic solution:** Use `BILRG_AdmLoketCurrentCall` as the single database-coordinated claim row per LoketKey, provided it can represent both Outstanding and InService. Acquire and release it in the same local transaction as the Queue Entry transition. Avoid in-memory locks and a separate generic lock table.

**Decision/artifact required:** State matrix for acquire, retain, replace, and release on Call, Recall, Start, No-Show, Withdraw, Redirect, Complete, and rollback; conditional SQL and 409 behavior.

**Tasks and layers:**

- **Domain/Application:** define allowed transition outcomes; operator still selects the entry.
- **Infrastructure/Database:** one-winner conditional insert/update, optimistic row version, and atomic Queue Entry/current-call mutation.
- **API:** stable conflict response.
- **Tests:** same-entry/two-Loket, same-Loket/two-session, rollback, stale version, and release races.

**Prerequisites:** R-01. Blocks final R-03 schema and all write work in R-08.

---

### R-05 — Verify bounded `ISequencer` allocation and conditional intake idempotency

**Source:** GAP-READY-008  
**Priority:** Blocker  
**Solvability:** Requires additional artifact

**Why it exists:** The retained sequencer is not proven to be bounded 1–9999/no-cycle for canonical admission tags. Optional `ClientRequestId` lacks length, comparison, lookup, and conflicting-reuse behavior.

**Capability affected:** Production Kiosk intake and safe retry after uncertain responses.

**Risk if unresolved:** Out-of-format labels, same-day wraparound, or duplicate Queue Entries.

**Pragmatic solution:** Keep `ISequencer` as sole allocator. Define a canonical daily admission SequenceTag and verify sequence creation/configuration. Use session-scoped optional `ClientRequestId`; return the existing compatible result on retry and reject conflicting reuse. Explicitly document that requests without it are not idempotent.

**Tasks and layers:**

- **Application:** retry lookup before allocation and conflict comparison.
- **Infrastructure/Database:** bounded non-cycling sequence verification/configuration; filtered uniqueness; concurrency-safe lookup.
- **API contract:** maximum length, case/collation semantics, optionality, conflict error, and retry response.
- **Tests:** 1, 9999, 10000 rejection, gaps, concurrent allocation, same retry, conflicting retry, and absent-ID behavior.

**Prerequisites:** R-03 session/entry contract. The API details feed R-10.

---

### R-06 — Establish Service Point authority and immutable Queue Labels

**Source:** GAP-READY-001, 002, 009, 012, 016 and GAP-AQO-010  
**Priority:** Blocker for labelled intake  
**Solvability:** Ready to implement for new data; historical backfill is an external dependency

**Why it exists:** `ServicePointType` is currently a caller-supplied snapshot; there is no master, active-prefix uniqueness, prefix snapshot, or trustworthy historical prefix mapping.

**Capability affected:** Server-authorized intake, stable labels, retirement, and Service Point selection.

**Risk if unresolved:** Clients can invent service categories, prefix changes rewrite perceived history, or fabricated legacy labels become operational truth.

**Pragmatic solution:** Add a simple Active/Retired Service Point aggregate and repository. Snapshot its prefix into the lazily created daily session. Format Queue Label in Domain as prefix plus four digits. Do not introduce effective dating. For legacy sessions, expose raw `NoUrut` until operations approves an authoritative backfill.

**Tasks and layers:**

- **Domain:** Service Point aggregate, prefix value behavior, retirement, Queue Label formatting, 1–9999 invariant.
- **Application:** establish/retire/list use cases; intake accepts stable ServicePointId and resolves Business Date through the shared capability.
- **Infrastructure/Database:** master persistence, prefix snapshot mapping, active validation, daily session load/create.
- **API:** administration and intake transport deferred to R-10.
- **Tests:** normalization, invalid prefixes, active uniqueness, retirement, immutable snapshot, inactive intake, date trust, concurrent lazy creation, and exhaustion.
- **External/data:** approve historical prefix mapping or an explicit “legacy label unavailable” behavior.

**Prerequisites:** R-03 contract; R-05 for production allocation. New-data implementation can proceed while historical mapping is unresolved.

---

### R-07 — Implement the queue-only operational projections

**Source:** GAP-READY-004, 006, 018 and GAP-AQO-002, 005, 012  
**Priority:** High  
**Solvability:** Ready to implement for internal projections; production display access is external

**Why it exists:** Existing queries are physician/Registration oriented and there is no admission worklist or current-per-Loket display snapshot.

**Capability affected:** Officer worklist, Priority visibility, passive Queue Display recovery, and Admisi Rajal enriched worklist.

**Risk if unresolved:** Clients stretch legacy queries, duplicate queue truth, or rely on transient SignalR messages.

**Pragmatic solution:** Add a purpose-built queue-only DAL/view contract. Compose Admisi enrichment through owning application contracts. Make persisted current state/version authoritative and SignalR only a refresh hint; polling and reconnect reload the snapshot.

**Tasks and layers:**

- **Application:** queue-only worklist and display snapshot query contracts.
- **Infrastructure:** direct projection SQL; Priority may affect default sorting but never automatic selection.
- **API:** read endpoints after R-10; best-effort refresh publisher after commit.
- **Frontend/client:** reload on notification/reconnect and poll at configured interval; audio only when AnnouncementVersion advances.
- **Tests:** field-boundary tests, sorting, no enrichment leakage, version/audio behavior, missed-notification recovery.

**Prerequisites:** R-03 and R-06. Can proceed in parallel with R-08 after the schema is fixed. Production exposure also needs R-10 and R-14.

---

### R-08 — Implement Call, Recall, Start, Withdraw, and Redirect transitions

**Source:** GAP-READY-004, 007, 009, 014, 015, 017  
**Priority:** High  
**Solvability:** Ready after R-04 and R-03

**Why it exists:** Current `AdmissionQueueStartCmd` moves Waiting directly to InService and carries no Loket. CallCount, current call, Withdrawn, No-Show disposition, and Priority replacement do not exist.

**Capability affected:** Core officer queue operation and accountable exception handling.

**Risk if unresolved:** Calling is conflated with service, current display truth is absent, and redirection can corrupt history.

**Pragmatic solution:** Add explicit handlers and narrow conditional persistence. Call/Recall retains Waiting and updates current-call plus CallCount. Start acknowledges the call and enters InService. Redirect atomically withdraws the origin and creates a Priority replacement with provenance. Keep No-Show manual; do not automate from CallCount.

**Tasks and layers:**

- **Domain:** call-state behavior, `Withdrawn`, CreationReason/provenance invariants, Priority indicator.
- **Application:** Call, Recall, Start, Withdraw, explicit No-Show disposition, and Redirect commands using workstation context and current user.
- **Infrastructure/Database:** CAS operations and active-claim acquire/release in one transaction.
- **API:** new versioned commands; preserve legacy `start` behind a compatibility gate.
- **Frontend:** explicit buttons and conflict refresh; no automatic next-entry choice.
- **Tests:** state matrix, CallCount/version, atomic redirect, conflicts, rollback, and history preservation.

**Prerequisites:** R-01, R-02, R-03, R-04, R-06. Exact routes require R-10.

---

### R-09 — Persist final Registration Outcomes and conditionally complete queue service

**Source:** GAP-READY-005, 014 and GAP-AQO-013  
**Priority:** High  
**Solvability:** Requires additional artifact; ReasonCode catalog is external

**Why it exists:** Current code completes only successful Registration and cannot represent an accountable final `NotEstablished` outcome.

**Capability affected:** Correct queue completion for both established and explicitly unsuccessful Registration assistance.

**Risk if unresolved:** Validation failures may be misreported as completion, negative outcomes are unauditable, or queue Done is mistaken for Registration truth.

**Pragmatic solution:** Add a small Admisi Rajal Registration Outcome aggregate/table keyed by stable OutcomeId and referencing `(AntrianId, NoUrut)`. In the same local orchestration, persist the final outcome and conditionally complete the InService entry. Do not create a Tracker for anonymous `NotEstablished`.

**Tasks and layers:**

- **Domain (Admisi Rajal):** Established/NotEstablished invariants and conditional RegId/ReasonCode.
- **Application:** final-decision commands with claim-derived actor/time; queue completion port.
- **Infrastructure/Database:** outcome DTO/DAL/repository and unique outcome/entry protection; atomic transaction.
- **API/Frontend:** final decision contract and selectable approved reasons.
- **Tests:** both outcomes, validation non-finality, duplicate outcome, concurrent completion, anonymous negative result, and rollback.

**Prerequisites:** R-01, R-02, R-03. External owner must publish the ReasonCode catalog before final API/UI; persistence may initially enforce only non-empty codes behind an internal contract.

---

### R-10 — Publish API, security, and compatibility contracts

**Source:** GAP-READY-010 and GAP-AQO-002, 008, 011  
**Priority:** Blocker for client delivery  
**Solvability:** Requires additional artifact plus external product/security input

**Why it exists:** Exact routes, payloads, errors, policies, display access, client identity position, and consumers of current endpoints are not inventoried.

**Capability affected:** Safe client rollout and removal or replacement of `anonymous-intake` and direct `start` semantics.

**Risk if unresolved:** Breaking unknown consumers, insecure display/Kiosk access, and incompatible retry/error behavior.

**Pragmatic solution:** Publish a versioned API/security artifact after Application contracts stabilize. Inventory existing consumers. Keep current endpoints feature-gated until migration is verified; do not silently change their semantics.

**Tasks and layers:**

- **Product/API:** consumer inventory, routes, payloads, visible labels, response/error taxonomy, idempotency rules, and deprecation dates.
- **Security/platform:** officer/admin/supervisor policies and Kiosk/display network or API access policy.
- **API:** claim-derived actors, workstation context adapter, validation, authorization, 409 mapping, and compatibility gates.
- **Frontend/clients:** migrate to split Call/Start and snapshot reload behavior.
- **Tests:** contract, authorization, spoofing, compatibility, and deprecation tests.

**Prerequisites:** Application contracts from R-05 through R-09. Contract drafting can run in parallel once those shapes are stable.

---

### R-11 — Define Booking Self-Registration assistance orchestration

**Source:** GAP-READY-011  
**Priority:** Blocker for Booking Kiosk path  
**Solvability:** Requires design decision

**Why it exists:** Retry idempotency does not prevent a second request ID from creating another assistance obligation for the same unresolved Booking attempt.

**Capability affected:** Booking Self-Registration that falls back to human assistance.

**Risk if unresolved:** Duplicate active assistance Queue Entries or issuance even when Registration succeeded.

**Pragmatic solution:** Admisi Rajal owns one narrow self-registration decision use case. Only an accountable `AssistanceRequired` result invokes Patient Tracker intake. Define one stable assistance correlation key and enforce one active obligation without creating an Admisi queue ledger.

**Tasks and layers:**

- **Domain/Application (Admisi Rajal):** outcome and correlation semantics.
- **Application (Patient Tracker):** narrow intake port accepting the assistance correlation.
- **Database/Infrastructure:** uniqueness/query needed for the business deduplication guard.
- **API/Kiosk:** orchestration contract and recovery behavior.
- **Tests:** success creates no queue entry, assistance creates one, new request ID does not duplicate, retry, and rollback.

**Prerequisites:** Decision by Admisi Rajal/product owner; R-05 and R-06. It does not block Walk-In/manual admission slices.

---

### R-12 — Production topology, Loket integrity, and display access

**Source:** GAP-AQO-002, 004, 005, 006 and GAP-READY-018  
**Priority:** Blocker for production Call/display; Medium for backend development  
**Solvability:** External dependency

**Why it exists:** A local configured LoketKey cannot prove uniqueness across machines; passive-display access and multi-instance SignalR behavior are deployment concerns; client repositories/hosting are unknown.

**Capability affected:** Accountable calling, real-time refresh expectations, and production client deployment.

**Risk if unresolved:** Two workstations are indistinguishable, refreshes are missed across instances, or display data is exposed improperly.

**Pragmatic solution:** Deployment/security owners choose controlled LoketKey distribution and duplicate detection (deployment validation or a narrow runtime registration/lease), define rename procedures, publish display access controls, and document API instance topology. Polling remains mandatory; never claim SignalR durability.

**Tasks and layers:** deployment runbook, configuration validation/diagnostics, startup health checks, topology/backplane decision, network/API policy, observability, and client hosting selection.

**Prerequisites:** R-04 claim semantics and R-10 security contract. Backend work can use a fixed configuration adapter meanwhile.

---

### R-13 — Retention, audit migration, and operational evidence

**Source:** GAP-AQO-007 and GAP-READY-020, 021  
**Priority:** Medium  
**Solvability:** External dependency for retention; Ready to implement for test coverage

**Why it exists:** Retention periods are unapproved, legacy queue tables lack standard audit columns, and unit tests do not prove operational concurrency or volume.

**Capability affected:** Compliance, capacity, incident investigation, and rollout confidence.

**Risk if unresolved:** Unbounded or premature deletion, incomplete accountability, and production-only race/performance failures.

**Pragmatic solution:** Retain Queue Entries and audit records until policy is approved; treat current display state as replaceable latest state. Add audit columns or an explicitly approved equivalent through R-03. Build SQL race/volume/rollback tests per slice rather than postponing them to the end.

**Tasks and layers:** compliance decision, capacity estimate, migration, audit mapping, retention job only after approval, integration test fixtures, representative-volume query plans, and operational dashboards/logs.

**Prerequisites:** R-03. Test work is continuous and parallelizable.

---

### R-14 — Deferred V1 policy and client hardening

**Source:** GAP-AQO-001, 006, 008, 009, 012 and GAP-READY-017, 019  
**Priority:** Low to Medium by capability  
**Solvability:** External dependency

**Why it exists:** No-show thresholds, display clear timing, BPJS/General routing evidence, print telemetry, client technology, and exact UI labels remain product/operations choices.

**Capability affected:** Automated no-show, polished displays, routing assistance, reprint support, and final UI delivery.

**Risk if unresolved:** Mainly constrained functionality; risk becomes high only if code invents policy from CallCount or queue-owned eligibility data.

**Pragmatic solution:** Keep No-Show and progression explicit/manual, let patients choose offered Service Points, redirect accountably, keep printing outside the backend transaction, and keep backend contracts client-agnostic. Implement policy only after an owner approves it.

**Prerequisites:** None for policy work; implementation depends on the relevant preceding slice.

## 3. Dependency and delivery sequence

### Critical path

1. **R-00 — Baseline checkpoint.** No mutating slice starts before this.
2. **R-01 + R-02 — Working-state hardening.** Conditional persistence and authenticated actors.
3. **R-04 + R-05 design; R-03 contract.** Resolve active Loket claim and sequencing/idempotency, then freeze the additive persistence contract.
4. **R-06 — Service Point, Business Date session, prefix snapshot, and labelled intake foundation.**
5. **R-07 and R-08 — Queue reads and operational transitions.** Run in parallel after schema and claim semantics stabilize.
6. **R-09 — Final Registration Outcome and conditional completion.** Can overlap R-07/R-08 after its schema contract is approved.
7. **R-10 — API/security/compatibility implementation.** Draft earlier; finalize against stable Application contracts.
8. **R-11 — Booking assistance.** Separate gated path; does not delay Walk-In/manual workflow.
9. **R-12 + R-13 — Production readiness.** Deployment integrity, topology, retention, volume, and operational verification.
10. **R-14 — Policy/client hardening.** Deliver only approved needs.

### Blocking gaps

- R-00 blocks all source mutation.
- R-03, R-04, and R-05 block safe production persistence, Call, and Kiosk intake.
- R-10 blocks client rollout and endpoint replacement.
- R-11 blocks only the Booking Self-Registration assistance path.
- R-12 blocks production Call/display accountability, not Domain/Application development.
- Historical prefix approval in R-06 blocks trustworthy labels for legacy sessions, not new sessions.

### Parallelizable work

- R-01 and R-02 can run together after the baseline checkpoint.
- R-04 decision work, R-05 sequencer investigation, R-10 consumer inventory, R-11 business design, and R-12 deployment design can proceed in parallel.
- After R-03/R-06, R-07 projection work, R-08 transition work, and R-09 outcome work can proceed independently behind separate contracts.
- R-13 integration/race/volume tests should accompany every slice rather than form one late phase.
- Frontend Kiosk/display development waits for R-10 but can use frozen response fixtures once the API artifact is approved.

## 4. Recommended first implementation slice

### Slice 1 — Admission queue transition safety

Complete **R-01 and the Application portion of R-02** first.

**Why this slice first:** It removes an existing lost-update/history-deletion risk, protects both current late-identification behavior and all later V1 work, requires no unresolved Kiosk/display/business policy, and keeps external behavior stable.

**Scope:**

1. checkpoint the baseline and rerun the existing 17 focused tests;
2. add explicit expected-state repository operations for the existing Waiting → InService and InService → Done paths;
3. route existing admission handlers away from unrestricted whole-aggregate entry mutation;
4. derive the actor from `ICurrentUserContext` in the touched human commands without changing routes;
5. map a failed compare-and-set to the existing concurrency exception;
6. add unit and SQL integration tests for success, conflict, rollback, and history preservation; and
7. publish a short implementation report with the baseline commit and verification evidence.

**Out of scope:** Service Point schema, Queue Labels, Call/Recall, Loket claims, SignalR, endpoint redesign, Registration Outcomes, Booking assistance, and frontend work.

**Working-state exit criteria:**

- all existing focused tests remain green;
- no touched admission lifecycle path physically deletes a Queue Entry;
- competing expected-state transitions produce one winner and one explicit conflict;
- actor identity comes from authenticated context in touched new commands;
- no route or payload contract changes; and
- database integration evidence exists for the conditional update, not only mocked unit tests.

## 5. Increment verification rule

Every later slice follows this order:

1. Domain invariant tests;
2. Application orchestration and conflict tests;
3. SQL/DAL/repository integration and migration rollback tests;
4. API/security/compatibility tests when transport is included;
5. multi-actor race and transaction rollback tests;
6. representative-volume projection evidence; and
7. implementation report and rollout/rollback update.

A green unit-test suite alone does not advance a slice through its release gate.
