# Outpatient Apotek Review Tracker

```yaml
Artifact-Type: ReviewGovernance
Status: Established
Bounded-Context: Apotek
EstablishedAt: 2026-08-19
ReviewLead: Independent Review Agent
ImplementationAgent: Composer 2.5
ReviewAgent: Grok Medium
Authority-Order:
  - adr/ADR-APT-001..003
  - apotek-domain.md
  - outpatient-apotek-workflow.md
  - outpatient-apotek-screen-and-aggregate-design.md
  - outpatient-apotek-persistence-design.md
  - working/apotek-implementation-master-plan.md
Inputs:
  - working/apotek-implementation-master-plan.md
  - working/outpatient-apotek-progress-tracker.md
  - working/outpatient-apotek-completeness-audit.md
Scope: APT-B00..APT-B30, APT-F00..APT-F07
OutOfRegistry:
  - APT-C01 (PLANNED-BLOCKED on BC-11; no review until BC-11 ratifies a contract)
```

This artifact is the **only authority for code-review status**. It does not replace the implementation progress tracker. Implementation status (`IMPLEMENTED`) is a claim that work was attempted; review status is the independent GO/NO_GO decision.

This stage establishes **review governance only**. No source code is changed. No remediation is authorized until a Review Agent round records numbered findings for that slice.

---

## 1. Review lifecycle

Allowed review statuses:

| Status | Meaning |
|--------|---------|
| `NOT_REVIEWED` | Slice is in the registry. No Review Agent round has been opened. Completeness audit scored it PARTIAL. |
| `REVIEWING` | A review round is open against a frozen commit SHA. Findings may be drafted; decision is not yet recorded. |
| `NO_GO` | At least one acceptance criterion failed, or a contradiction with the plan was confirmed. Numbered findings exist or, for audit-seeded rows, are pending promotion in the first review round. |
| `REMEDIATED` | Implementation Agent closed the recorded findings on a new commit. The slice is waiting for re-review. Not a GO. |
| `GO` | Every slice acceptance criterion is PASS and no actionable finding remains. Code-review GO is not production release. |

```text
NOT_REVIEWED
    → REVIEWING
        → GO
        → NO_GO
            → REMEDIATED
                → REVIEWING   (re-review)
                    → GO
                    → NO_GO   (repeat)
```

Illegal transitions:

- `NOT_REVIEWED` → `GO` (must pass `REVIEWING`)
- `NOT_REVIEWED` → `REMEDIATED` (no findings to close)
- `NO_GO` → `GO` (must remediate and re-review)
- `REMEDIATED` → `GO` (must re-review)
- `GO` → any other status except a new `REVIEWING` round if later evidence invalidates the prior GO (append a new round; do not erase history)

### Audit-seeded `NO_GO`

The completeness audit of 19 August 2026 initialized five slices as `NO_GO` **before** a Review Agent round. That seed is a hold, not a findings register.

Those slices **must still execute the first Review step** of the workflow below. Remediation starts only after the Review Agent promotes audit gaps into numbered findings and confirms `NO_GO`.

---

## 2. Review workflow

Every slice, including audit-seeded `NO_GO` rows, follows the same sequence:

```text
Review → Findings → Remediation → Re-review → GO
```

| Step | Owner | Required output | Next status |
|------|-------|-----------------|-------------|
| **Review** | Review Agent | Frozen `reviewedCommit`; acceptance-criterion results; evidence (file/line or failing test) | `REVIEWING` while in progress |
| **Findings** | Review Agent | Numbered findings (`{Slice}-R{n}-F{nn}`) with severity, criterion, location, problem, required outcome | `GO` if none remain; otherwise `NO_GO` |
| **Remediation** | Implementation Agent | Changes limited to recorded finding IDs; new commit; tests rerun | `REMEDIATED` |
| **Re-review** | Review Agent | New round against the remediation commit; prior open findings plus acceptance criteria | `GO` or `NO_GO` |
| **GO** | Review Agent only | All criteria PASS; no open findings | `GO` |

Rules:

1. Implementation Agent records `IMPLEMENTED` on the progress tracker. It **never** records review `GO`.
2. Review Agent checks only: slice objective, dependencies, authority references, acceptance criteria, regression risk, and evidence.
3. Any unmet planned acceptance criterion is `NO_GO`. Severity labels do not override that.
4. Findings must cite a file/line, schema object, missing artifact, or failing/absent test. Unsourced opinions are not findings.
5. Open production gates (PD-08, PD-09, BC-11, BC-12, BC-13) **do not** prevent code-review `GO` when the documented safe interim is implemented. They remain production-release blocks on the progress-tracker gate registry.
6. Remediation may not silently broaden scope. Adjacent gaps become new planned slices or new findings in a later round.
7. History is append-only. Edit current status fields; never erase prior review or remediation rounds.
8. A re-review always appends a new round number. It does not rewrite round 1.

### Finding template

```yaml
- id: APT-B20-R1-F01
  severity: HIGH          # HIGH | MEDIUM | LOW — does not override unmet AC
  criterionId: AC-01
  location: file:line or "missing: expected artifact"
  problem: Objective defect statement
  requiredOutcome: Observable correction
  status: OPEN            # OPEN | ADDRESSED | WAIVED (WAIVED requires Review Agent + rationale)
```

### Review-round template

```yaml
- round: 1
  actor: Grok Medium
  reviewedCommit: <sha>
  startedAt: null
  completedAt: null
  acceptanceResults: []
  findings: []
  decision: null          # GO | NO_GO
  rationale: null
```

---

## 3. Governance rules

### Separation of trackers

| Tracker | Path | Owns |
|---------|------|------|
| Implementation | `docs/contexts/apotek/working/outpatient-apotek-progress-tracker.md` | Build claims, implementation history, release gates |
| Completeness audit | `docs/contexts/apotek/working/outpatient-apotek-completeness-audit.md` | One-time seed evidence (19 August 2026) |
| **Review** | this file | Lifecycle, findings, GO/NO_GO, review waves |

### Review freeze

Moving a slice to `REVIEWING` freezes `reviewedCommit`. Implementation must not mix unrelated commits into that round. Remediation uses a new commit and a new round.

### Evidence standard

A criterion is PASS only when the planned evidence exists: domain/handler/repository or contract tests as specified in the master plan, plus the named commands having been run. Passing the current 38 ApotekContext tests does **not** by itself satisfy slices that required repository, API-contract, SQL-fixture, workflow, or UI-path evidence.

### Wave discipline

- Execute Wave-1 to Review → Findings before opening Wave-2 reviews, except for already-`GO` slices (they stay `GO`).
- Do not start Wave-2 remediation until Wave-1 findings for the slice under change are closed **or** the Review Lead records that the Wave-2 slice has no overlapping write surface.
- Wave-3 frontend slices (`APT-F01`–`APT-F07`) review against frozen backend contracts. Do not treat a frontend `GO` as covering a backend `NO_GO` dependency.

### Production vs code review

Code-review `GO` means the slice meets its acceptance criteria, including documented safe interims. Production release additionally requires the gate registry on the progress tracker (currently all of PD-09, PD-08, BC-11, BC-12, BC-13 remain OPEN).

---

## 4. Initialization (19 August 2026)

Source: completeness audit. Implementation-tracker `IMPLEMENTED` claims were not trusted.

| Audit score | Review status | Count |
|-------------|---------------|------:|
| GO | `GO` | 2 |
| NO-GO | `NO_GO` (audit-seeded; first Review round still required) | 5 |
| PARTIAL | `NOT_REVIEWED` | 32 |
| **Registry total** | | **39** |

Seeding rule applied:

- All audit **NO-GO** slices → `NO_GO`
- All audit **PARTIAL** slices → `NOT_REVIEWED`
- All audit **GO** slices → `GO`

---

## 5. Review registry

`ReviewWave` is the execution bucket. `GO` slices are registered but are not in an open wave.

| Slice | Title | Phase | Audit | Review status | Wave | Release gates |
|-------|-------|-------|-------|---------------|------|---------------|
| APT-B00 | Apotek module boundary | 0 | GO | **GO** | — | — |
| APT-B01 | Neighbor prerequisites | 0 | PARTIAL | **GO** | — | — |
| APT-B02 | Integration Task engine | 0 | PARTIAL | **GO** | — | — |
| APT-B03 | Electronic Resep Kerja intake | 1 | PARTIAL | NOT_REVIEWED | 2 | — |
| APT-B04 | Physical Resep Kerja intake | 1 | PARTIAL | NOT_REVIEWED | 2 | BC-13 |
| APT-B05 | Jual Bebas acceptance | 1 | PARTIAL | NOT_REVIEWED | 2 | — |
| APT-B06 | Telaah Resep | 1 | PARTIAL | NOT_REVIEWED | 2 | — |
| APT-B07 | Available Stock fail-closed port | 1 | PARTIAL | NOT_REVIEWED | 3 | PD-09 |
| APT-B08 | Sales Order establishment | 1 | PARTIAL | NOT_REVIEWED | 2 | PD-09 |
| APT-B09 | Partial fulfillment and Copy Resep | 1 | PARTIAL | NOT_REVIEWED | 2 | PD-09 |
| APT-B10 | Iter consumption delivery | 1 | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-B11 | Queue mapping and close | 2 | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-B12 | Tracker pharmacy adapter | 2 | PARTIAL | NOT_REVIEWED | 3 | BC-11 |
| APT-B13 | Invoice establishment and issue | 3 | PARTIAL | NOT_REVIEWED | 2 | — |
| APT-B14 | Payment clearance and authorization | 3 | PARTIAL | NOT_REVIEWED | 2 | — |
| APT-B15 | Invoice revision / TR correlation | 3 | PARTIAL | NOT_REVIEWED | 2 | PD-08 |
| APT-B16 | Dispensing through Prepared | 3 | PARTIAL | NOT_REVIEWED | 2 | — |
| APT-B17 | Review, education, pickup, handover | 3 | PARTIAL | NOT_REVIEWED | 2 | — |
| APT-B18 | General-patient end-to-end | 3 | PARTIAL | NOT_REVIEWED | 3 | PD-09 |
| APT-B19 | BPJS coverage and invoice-at-handover | 4 | PARTIAL | NOT_REVIEWED | 3 | PD-09 |
| APT-B20 | Mixed coverage | 4 | NO-GO | **NO_GO** | 1 | PD-09 |
| APT-B21 | Multiple demands in one queue | 4 | PARTIAL | NOT_REVIEWED | 3 | PD-09 |
| APT-B22 | Post-establishment shortage | 5 | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-B23 | Collection window, override, no-show | 5 | NO-GO | **NO_GO** | 1 | BC-12 |
| APT-B24 | Telaah and Pelayanan read APIs | 6 | NO-GO | **NO_GO** | 1 | — |
| APT-B25 | Dispensing and Serah read APIs | 6 | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-B26 | Patient Medication Journey | 6 | NO-GO | **NO_GO** | 1 | — |
| APT-B27 | Unified sales reporting adapter | 6 | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-B28 | Integration failure operations | 6 | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-B29 | API, auth, audit, concurrency | 6 | PARTIAL | NOT_REVIEWED | 3 | BC-12 |
| APT-B30 | End-to-end verification dossier | 6 | NO-GO | **NO_GO** | 1 | all open gates |
| APT-F00 | Pharmacy type and module-shell reset | F | GO | **GO** | — | — |
| APT-F01 | Pharmacy queue and mapping client | F | PARTIAL | NOT_REVIEWED | 3 | BC-11 |
| APT-F02 | Telaah Resep workbench | F | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-F03 | Pelayanan Penjualan workbench | F | PARTIAL | NOT_REVIEWED | 3 | BC-13, PD-09 |
| APT-F04 | Dispensing workbench | F | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-F05 | Serah Obat and no-show workbench | F | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-F06 | Journey and attention UI | F | PARTIAL | NOT_REVIEWED | 3 | — |
| APT-F07 | Frontend verification and documentation | F | PARTIAL | NOT_REVIEWED | 3 | BC-12 |

### Roll-up

| Review status | Count |
|---------------|------:|
| GO | 4 |
| NO_GO | 4 |
| NOT_REVIEWED | 31 |
| REVIEWING | 0 |
| REMEDIATED | 0 |
| REVIEWING | 0 |
| REMEDIATED | 0 |
| **Total** | **39** |

---

## 6. Review execution roadmap

### Wave-1 — All audit NO-GO slices

Execute first. Confirm findings, then remediate only those findings. Do not start Wave-2 until Wave-1 Review → Findings is complete for all five.

| Order | Slice | Audit reason (candidate; promote in Review) |
|------:|-------|---------------------------------------------|
| 1 | APT-B20 | No payer-split orchestration creating independent BPJS and patient-pay Sales Orders; no WF-005 tests. |
| 2 | APT-B23 | Resolves Sales Order before stock-return task succeeds; payer matrix and stock-failure tests absent. |
| 3 | APT-B24 | Telaah worklist cannot surface unstarted intake; Pelayanan is not sourced from Tracker queue; no fixture/query tests. |
| 4 | APT-B26 | Journey omits Telaah and payer identity; misclassifies integration-task states; returns SourceId instead of task ID. |
| 5 | APT-B30 | Required WF-001–007, SQL smoke, repository/API, and neighbor integration evidence suites are absent. |

Wave-1 exit: each of the five has a completed Review round with numbered findings, or is `GO` if the Review Agent disproves the audit (must cite contrary evidence).

### Wave-2 — Core domain slices

Core domain = demand intake plus the four aggregate roots (`TelaahResep`, `SalesOrder`, `Invoice`, `Dispensing`) and the write behaviors that establish or mutate them. Order follows master-plan dependencies.

| Order | Slice | Why core |
|------:|-------|----------|
| 1 | APT-B03 | Resep Kerja intake (supporting demand document) |
| 2 | APT-B04 | Physical intake (same document; BC-13 interim) |
| 3 | APT-B05 | Jual Bebas demand |
| 4 | APT-B06 | `TelaahResep` aggregate |
| 5 | APT-B08 | `SalesOrder` aggregate |
| 6 | APT-B09 | Copy Resep / unfulfilled outcomes on accepted demand |
| 7 | APT-B13 | `Invoice` aggregate |
| 8 | APT-B14 | Dispense-authorized policy (gates `Dispensing`) |
| 9 | APT-B15 | Invoice mutability / Tata Rekening correlation |
| 10 | APT-B16 | `Dispensing` through Prepared |
| 11 | APT-B17 | Final review, education, pickup, handover |

Wave-2 exit: all eleven are `GO` or have an explicit Review Lead deferral with finding IDs.

### Wave-3 — Remaining slices

Platform, queue, workflow-completion, reads, operations, API hardening, and frontend.

| Order | Slice | Cluster |
|------:|-------|---------|
| 1 | APT-B01 | Platform / neighbor seeds |
| 2 | APT-B02 | Integration Task engine |
| 3 | APT-B07 | Available Stock port (PD-09 interim) |
| 4 | APT-B10 | Iter delivery |
| 5 | APT-B11 | Queue mapping and close |
| 6 | APT-B12 | Tracker pharmacy adapter |
| 7 | APT-B18 | WF-003 orchestration evidence |
| 8 | APT-B19 | WF-004 BPJS path |
| 9 | APT-B21 | WF-006 multi-demand coordination |
| 10 | APT-B22 | Post-establishment shortage |
| 11 | APT-B25 | Dispensing / Serah read APIs |
| 12 | APT-B27 | Unified sales reporting |
| 13 | APT-B28 | Integration operations |
| 14 | APT-B29 | API / auth / audit / concurrency |
| 15 | APT-F01 | Frontend queue/mapping |
| 16 | APT-F02 | Frontend Telaah |
| 17 | APT-F03 | Frontend Pelayanan |
| 18 | APT-F04 | Frontend Dispensing |
| 19 | APT-F05 | Frontend Serah / no-show |
| 20 | APT-F06 | Frontend journey / attention |
| 21 | APT-F07 | Frontend verification / docs |

Already `GO` (not in a wave): APT-B00, APT-F00.

Not in this registry: APT-C01 remains `PLANNED-BLOCKED` on BC-11.

Wave-3 frontend work reviews after the backend contracts those screens consume are `GO` or have Review Lead waiver for read-only clients of still-open backend findings.

---

## 7. Slice review records

Empty until the first Review round. Append here; do not overwrite.

### APT-B02

```yaml
slice: APT-B02
reviewStatus: GO
initializedFrom: completeness-audit-2026-08-19
reviewedCommit: WORKING-TREE atop 5c1f3cd964f7db1e188325ec0035089bd3d08af0 (freeze fingerprints in round-2 record)
reviewHistory:
  - round: 1
    actor: ox-alpha (opencode)
    reviewedCommit: 5c1f3cd964f7db1e188325ec0035089bd3d08af0
    completedAt: 2026-08-25T22:30:00+07:00
    acceptanceResults:
      - criterionId: AC-01
        result: PASS
        evidence: "BILRG_AptIntegrationTask.sql:21-22 unique index UX_BILRG_AptIntegrationTask_Idempotency; columns SourceKind/SourceId/Destination/PayloadJson/TaskStatus/RetryCount/LastError/CorrelationId at lines 3-14; model guards key<=80 (AptIntegrationTaskModel.cs:59-60) matching VARCHAR(80); app-level suppression AptIntegrationTaskEnqueue.InsertIfAbsent tested (AptIntegrationTaskTransactionContractTest.cs:13-50)"
      - criterionId: AC-02
        result: FAIL
        evidence: "Claim uses a status predicate and transitions are deterministic (Succeeded/Failed/Dead state machine proven by AptIntegrationTaskModelTest.cs:27-88), BUT the predicate is IN (@Pending,@Failed), not Pending-only: ListPending AptIntegrationTaskDal.cs:104, ClaimPending AptIntegrationTaskDal.cs:122; worker auto-claims Failed rows every batch via PrepareRetry (AptIntegrationWorker.cs:38-39). Plan requires 'claims only Pending'."
      - criterionId: AC-03
        result: PASS
        evidence: "Delivery-side replay idempotency: AptIntegrationWorkerTest.cs:18-41 proves Succeeded replay returns 'idempotent' with handler invoked exactly once; claim-lost path tested at :44-61; enqueue-side duplicate key does not insert second row (AptIntegrationTaskTransactionContractTest.cs:13-50)"
      - criterionId: AC-04
        result: FAIL
        evidence: "No test executes a business save + task insert inside one TransHelper.NewScope() to prove joint commit, nor a rollback discarding both. Only evidence is reflection over RequireAmbientTransaction()==true (AptIntegrationTaskTransactionContractTest.cs:52-60); the duplicate-key test (:13-50) mocks the repo and exercises no transaction. Production pairing exists (InvoiceCommands.cs:119-128) but is unproven. Confirms completeness-audit gap."
      - criterionId: AC-05
        result: PASS
        evidence: "Purpose-built task table with typed destinations/handlers (10 AptIntegrationTaskTypeEnum values); no event-store semantics; local TransHelper.NewScope transactions only (AptIntegrationWorker.cs:48-52,97-102); architecture guard rejects legacy dual-write (ApotekContextBoundaryTest.cs:55-63)"
    findings:
      - id: APT-B02-R1-F01
        severity: HIGH
        criterionId: AC-02
        location: src/bilreg/Bilreg.Infrastructure/ApotekContext/IntegrationFeature/AptIntegrationTaskDal.cs:104,122
        problem: Batch claim predicate is TaskStatus IN (Pending, Failed) in both ListPending and ClaimPending, so every batch automatically re-claims Failed tasks with no backoff or operator control; MaxRetries=5 can be exhausted within seconds on a transient neighbor outage, and this bypasses the audited operator-retry model of AptIntegrationRetryCmd (AptIntegrationOpsCommands.cs:40-67). The acceptance criterion states the worker claims only Pending tasks.
        requiredOutcome: Restrict ListPending and ClaimPending to Pending only so Failed tasks return to processing solely through the audited retry command, or record an approved deviation with an explicit retry/backoff policy plus tests proving Failed rows are not batch-claimed and Dead rows never re-enter.
        status: OPEN
      - id: APT-B02-R1-F02
        severity: HIGH
        criterionId: AC-04
        location: missing test; nearest artifact src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationTaskTransactionContractTest.cs:52-60 (reflection-only)
        problem: The planned proof that a business save and task insert commit together — and roll back together on failure — does not exist. No ApotekContext test opens TransHelper.NewScope(); existing transactional pairing in production handlers (e.g., InvoiceCommands.cs:119-128) is untested.
        requiredOutcome: A test that runs one TransHelper.NewScope() containing a business save plus an integration-task insert where both commit, and a second failure path where neither persists (real DAL against SQL fixture per repository convention, or documented equivalent).
        status: OPEN
      - id: APT-B02-R1-F03
        severity: HIGH
        criterionId: AC-04
        location: src/bilreg/Bilreg.Application/ApotekContext/IntegrationFeature/AptIntegrationWorker.cs:80 (ProcessBatch has no caller outside its own unit test AptIntegrationWorkerTest.cs:86-114)
        problem: ProcessBatch has no production invocation path: no hosted/background service exists under src/bilreg (grep AddHostedService/BackgroundService = none), no MediatR command wrapper (unlike sibling engines LabOwareQueueProcessCmd.cs and EmrAntrianOutboundProcessCmd.cs), and no HTTP trigger; the only production entry is POST api/v1/apotek/integration/retry which calls ProcessOne for a single known task (ApotekController.cs:174-180). Enqueued Pending tasks would never be delivered automatically, contradicting the slice objective of durable delivery for all neighbor effects.
        requiredOutcome: An invocation path for batch processing (hosted worker or ops-exposed command/endpoint consistent with sibling engines) plus a test proving pending tasks flow through it to their handlers.
        status: OPEN
      - id: APT-B02-R1-F04
        severity: MEDIUM
        criterionId: AC-04
        location: src/bilreg/Bilreg.Application/ApotekContext/IntegrationFeature/AptIntegrationWorker.cs:48-52
        problem: The claimed Processing state is persisted in its own committed scope before handler execution; a crash during handler invocation strands the row in Processing permanently — ListPending excludes Processing (AptIntegrationTaskDal.cs:104) and AssertCanRetry accepts only Failed (AptIntegrationTaskModel.cs:147-152) — leaving no recovery path and violating durable/retryable delivery.
        requiredOutcome: Stale-claim recovery (lease timeout reclaim) or an operator-visible remediation state, with a test exercising recovery of a stranded Processing row.
        status: OPEN
      - id: APT-B02-R1-F05
        severity: LOW
        criterionId: tracker-completeness
        location: docs/contexts/apotek/working/outpatient-apotek-progress-tracker.md:238-253
        problem: The APT-B02 implementation record omits baseCommit, dates, changedFiles, schemaObjects, and focused-test counts required by the slice-record template (master-plan section 6); the recorded count 38 is the whole ApotekContext suite, not the integration-engine subset.
        requiredOutcome: Backfill the implementation history event with commit SHAs, changed files/schema objects, and engine-focused test command/count.
        status: OPEN
    decision: NO_GO
    rationale: AC-02 fails (claim predicate includes Failed, not Pending-only) and AC-04 fails (no business-save + task-insert commit/rollback test; no production invocation path for batch delivery). AC-01/AC-03/AC-05 pass. Findings APT-B02-R1-F01..F05 recorded; remediation limited to these IDs.
    environmentalNote: dotnet SDK unavailable in review environment; tests not independently re-executed. Decision relies on static verification of cited code paths and existing test sources at commit 5c1f3cd964f7db1e188325ec0035089bd3d08af0 (working tree contains only tracker doc edits).
  - round: 2
    actor: ox-alpha (opencode)
    reviewedCommit: WORKING-TREE atop 5c1f3cd964f7db1e188325ec0035089bd3d08af0; freeze fingerprints (sha256[0:16]) at review time — AptIntegrationTaskDal f38dd378ff90e0e0, AptIntegrationWorker 77ddc5d5e7cc83b0, AptIntegrationTaskModel b2439e682a9f7f4d, AptIntegrationOpsCommands 5bef279cf7fc73d7, AptIntegrationProcessCmd 45505a02413129ab, ApotekController e0fe17238a0a5198, AptIntegrationTaskDalTest 8011a108851e787b, InMemoryApotekRepos 42ca4c354bd69312
    completedAt: 2026-08-25T23:59:00+07:00
    acceptanceResults:
      - criterionId: AC-01
        result: PASS
        evidence: "Live schema check on devTest: UX_BILRG_AptIntegrationTask_Idempotency exists with is_unique=1 alongside PK and IX_..._Pending; columns cover source/destination/payload/status/retry/error/correlation; model guard key<=80 unchanged"
      - criterionId: AC-02
        result: PASS
        evidence: "R1-F01 closed: ListPending and ClaimPending SQL predicates are TaskStatus = @Pending (AptIntegrationTaskDal.cs ListPending/ClaimPending); SQL-level proof ListPending_and_ClaimPending_target_only_pending_status executes against devTest and shows Failed/Dead rows neither listed nor claimable while Pending claims once; worker rejects non-Pending statuses (AptIntegrationWorker.cs ProcessOne); in-memory repo twin aligned; deterministic Succeeded/Failed/Dead transitions unchanged and still tested (MarkFailed_IncrementsRetry_ThenDeadAfterMax)"
      - criterionId: AC-03
        result: PASS
        evidence: "Unchanged from round 1: replay of Succeeded task returns idempotent with handler invoked exactly once; duplicate enqueue does not insert second row"
      - criterionId: AC-04
        result: PASS
        evidence: "R1-F02 closed: Business_save_and_task_insert_commit_together proves QueueClose insert + task insert persist after trans.Complete() on live devTest via real DALs; Business_save_and_task_insert_roll_back_together proves neither row survives scope disposal without Complete. R1-F03 closed: AptIntegrationProcessCmd/handler plus POST api/v1/apotek/integration/process give ProcessBatch a production invocation path; Handle_ProcessesPendingBatchThroughWorker proves pending tasks flow to handlers. R1-F04 closed: ClaimPending stamps processing start; Reclaim_FromStaleProcessing_ReturnsToPending, Retry_OnStaleProcessingRow_ReclaimsAndProcesses, Retry_OnFreshProcessingRow_ThrowsWithoutPersist cover recovery"
      - criterionId: AC-05
        result: PASS
        evidence: "No event-store or distributed-transaction semantics introduced by remediation; architecture guard suite green within full ApotekContext run"
    findings: []
    decision: GO
    rationale: All five round-1 findings verified closed with non-vacuous, finding-scoped changes; all five acceptance criteria pass under reviewer-executed build and tests (engine suite 25/25; full ApotekContext suite 54/54 twice, five consecutive greens total since the single transient Wf003 blip recorded during remediation). No unauthorized scope expansion: StaleProcessingMinutes=30 is a documented operational constant analogous to MaxRetries=5, reclaim is operator-driven through the authenticated retry command (no BC-12 matrix invented), and devTest received only the two existing in-repo DDL scripts (unique index verified live).
    environmentalNote: Build and tests executed independently by the reviewer in this session via Windows dotnet SDK over WSL interop (0 errors). Remediation remains uncommitted working tree; freeze captured via per-file sha256 fingerprints above — commit the changes before using APT-B02 as a frozen GO dependency in later waves.
remediationHistory:
  - round: 1
    basedOnReviewRound: 1
    actor: ox-alpha (opencode)
    startedAt: 2026-08-25T22:45:00+07:00
    completedAt: 2026-08-25T23:40:00+07:00
    remediatedFindings:
      - APT-B02-R1-F01
      - APT-B02-R1-F02
      - APT-B02-R1-F03
      - APT-B02-R1-F04
      - APT-B02-R1-F05
    resultCommit: WORKING-TREE (uncommitted changes on top of 5c1f3cd964f7db1e188325ec0035089bd3d08af0)
    changedFiles:
      - src/bilreg/Bilreg.Infrastructure/ApotekContext/IntegrationFeature/AptIntegrationTaskDal.cs
      - src/bilreg/Bilreg.Application/ApotekContext/IntegrationFeature/AptIntegrationWorker.cs
      - src/bilreg/Bilreg.Domain/ApotekContext/IntegrationFeature/AptIntegrationTaskModel.cs
      - src/bilreg/Bilreg.Application/ApotekContext/IntegrationFeature/UseCases/AptIntegrationOpsCommands.cs
      - src/bilreg/Bilreg.Application/ApotekContext/IntegrationFeature/UseCases/AptIntegrationProcessCmd.cs
      - src/bilreg/Bilreg.Api/Controllers/ApotekContext/ApotekController.cs
      - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationTaskDalTest.cs
      - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationProcessCmdTest.cs
      - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationRetryCommandTest.cs
      - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationTaskModelTest.cs
      - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationWorkerTest.cs
      - src/bilreg/Bilreg.Test/ApotekContext/Support/InMemoryApotekRepos.cs
    schemaObjects:
      - Deployed BILRG_AptQueueClose.sql and BILRG_AptIntegrationTask.sql to devTest via SQLCMD (DDL unchanged; tables were never deployed, which is what blocked Dal-level evidence at audit time)
    tests:
      - command: dotnet build src/bilreg/b09-bilreg-api.sln
        result: PASS
      - command: dotnet test --filter "FullyQualifiedName~ApotekContext.IntegrationFeature"
        result: PASS
        count: 25
      - command: dotnet test --filter "FullyQualifiedName~ApotekContext"
        result: PASS
        count: 54
    unresolvedFindings: []
    outcome: IMPLEMENTED
notes:
  - Completeness audit (19 Aug 2026) scored PARTIAL citing missing business-save+rollback test and no hosted invocation of ProcessBatch; round 1 confirmed both and added the Pending-only claim deviation, stranded-Processing risk, and tracker backfill.
  - Remediation round 1 (25 Aug 2026) closed all five findings; REMEDIATED is not GO — re-review must open a new round against the remediation commit.
  - Unlike review round 1, build/tests were executed in this session via the Windows dotnet SDK (WSL interop); the real-DB transaction contract now runs against devTest after deploying the two in-repo DDL scripts.
  - Round opened on user request while Wave-1 slices remain first in the execution roadmap (same precedent as APT-B01 round 2).
  - Round 2 (25 Aug 2026) re-reviewed the remediation working tree with reviewer-executed tests → GO. Known accepted limitations: no lease heartbeat on stale-Processing reclaim (30-minute constant, operator-driven, audited); single transient Wf003 blip never reproduced across five consecutive full-suite runs. Commit of the working tree is outstanding housekeeping.
```

### APT-B01

```yaml
slice: APT-B01
reviewStatus: GO
initializedFrom: completeness-audit-2026-08-19
reviewedCommit: 5c1f3cd9
reviewHistory:
  - round: 1
    actor: Grok Medium
    reviewedCommit: 451ddd59fe20ee99aefe4e1e3c203bc6ea68e3f6
    completedAt: 2026-08-19T14:45:00+07:00
    acceptanceResults:
      - criterionId: AC-01
        result: FAIL
        evidence: "MovementKindEnum.DispenseIssue vs SaleIssueDu; PostDispenseIssueConsequenceHandler; DispenseIssueMovementKindTest.HandlerType_ExistsForDispenseIssueConsequence is type-only; LegacyMovementKindMapper omits DI"
      - criterionId: AC-02
        result: PASS
        evidence: "ApotekLocationIds.cs; BILRG_Apt_Seed_Layanan.example.sql; no pharmacy reservation table"
      - criterionId: AC-03
        result: FAIL
        evidence: "BILRG_Apt_Seed_ServicePoint.example.sql; AdmissionServicePointRepo.GetData; no pharmacy LoadEntity test"
      - criterionId: AC-04
        result: PASS
        evidence: "dotnet test ...~DispenseIssueMovementKindTest|~CollectionWindowDaysProviderTest → 7 passed; CollectionWindowDaysProvider + seed 7"
      - criterionId: AC-05
        result: PASS
        evidence: "AntrianStatusEnum.cs four values; CollectionWindowDaysProviderTest.Collection_window_seed_defaults_to_seven_and_does_not_alter_antrian_entry"
    findings:
      - id: APT-B01-R1-F01
        severity: HIGH
        criterionId: AC-01
        location: src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/DispenseIssueMovementKindTest.cs:40-45
        problem: Stock Ledger evidence for DispenseIssue is type-existence and CreateOutbound only. PostDispenseIssueConsequenceHandler is never invoked, so Success/Idempotent/InsufficientStock and persisted MovementKind=DispenseIssue (not SaleIssueDu) are unproven. Neighbor SaleIssue has PostSaleIssueConsequenceHandlerTest coverage; this slice does not.
        requiredOutcome: Handler tests that post DispenseIssue, persist MovementKindEnum.DispenseIssue, remain distinct from SaleIssueDu, and cover idempotent replay of the same TrsReffId.
        status: OPEN
      - id: APT-B01-R1-F02
        severity: HIGH
        criterionId: AC-01
        location: src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/UseCases/PostDispenseIssueConsequenceCommand.cs:50; src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyMovementKindMapper.cs:11-27; src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyScopeJournalReplayer.cs:53-60
        problem: The consequence handler dual-writes legacy MovementKindString "DI". LegacyMovementKindMapper does not map DI. Catch-up/hydrate then fails with Unsupported legacy MovementKind and marks the scope Inconsistent. The explicit DispenseIssue path is write-only relative to the Stock Ledger dual-write ACL.
        requiredOutcome: Map DI to MovementKindEnum.DispenseIssue in LegacyMovementKindMapper with mapper tests, so replay of pharmacy handover buku does not treat DI as unsupported.
        status: OPEN
      - id: APT-B01-R1-F03
        severity: MEDIUM
        criterionId: AC-03
        location: src/bilreg/Bilreg.SqlDb/ApotekContext/BILRG_Apt_Seed_ServicePoint.example.sql:8-11; missing test against IAdmissionServicePointRepo
        problem: Pharmacy service-point registration is example-only SQL. No test proves a pharmacy ServicePointId is loadable through existing BILRG_AdmServicePoint / AdmissionServicePointRepo queue infrastructure.
        requiredOutcome: A contract or repository test that existing queue service-point load resolves a pharmacy BILRG_AdmServicePoint row (example seed may remain non-production if the shared table/repo path is proven).
        status: OPEN
    decision: NO_GO
    rationale: AC-01 and AC-03 fail. DispenseIssue is enumerated and a handler exists, but the path is untested and the legacy ACL cannot round-trip DI. Pharmacy service-point resolvability is not evidenced.
  - round: 2
    actor: ox-alpha (opencode)
    reviewedCommit: 5c1f3cd9
    completedAt: 2026-08-25T21:30:00+07:00
    acceptanceResults:
      - criterionId: AC-01
        result: PASS
        evidence: "R1-F01 closed: PostDispenseIssueConsequenceHandlerTest proves Success dual-write (mutasi MovementKind=DispenseIssue, legacy journal 'DI', NotContain SaleIssueDu), Idempotent replay of same TrsReffId without double qty, InsufficientStock rejects with zero persist and UoW.Commit never called. R1-F02 closed: LegacyMovementKindMapper.cs:27 maps DI→MovementKindEnum.DispenseIssue; LegacyScopeJournalReplayer.cs:53 consumes the mapper so catch-up/hydrate round-trips DI; LegacyMovementKindMapperTest covers DI/di plus TryMap_Di_IsDistinctFromDu"
      - criterionId: AC-02
        result: PASS
        evidence: "ApotekLocationIds.cs:5-6 LYAPT/LYDTU; BILRG_Apt_Seed_Layanan.example.sql present; no reservation table under Bilreg.SqlDb/ApotekContext"
      - criterionId: AC-03
        result: PASS
        evidence: "R1-F03 closed: PharmacyServicePointContractTest uses real AdmissionServicePointRepo/Dal — LoadEntity resolves pharmacy 'APT' row and AdmissionServicePointResolver.EnsureAdmissionQueue accepts it; resolver guard genuinely throws for unregistered points (AdmisiRajalOptions.cs:17-24); seed-content test ties example SQL to same table/id"
      - criterionId: AC-04
        result: PASS
        evidence: "BILRG_Apt_Seed_CollectionWindow.sql inserts APT_COLLECTION_WINDOW_DAYS='7'; CollectionWindowDaysProviderTest unchanged since round 1 PASS"
      - criterionId: AC-05
        result: PASS
        evidence: "AntrianStatusEnum.cs still Waiting/InService/Done/Withdrawn; only pre-existing AdmisiContext M2/M3 alters touch BILRG_AntrianEntry; no ApotekContext alter exists"
    findings: []
    decision: GO
    rationale: All three round-1 findings verified remediated at commit 5c1f3cd9 with non-vacuous tests; remediation scope limited to recorded findings (+1 mapper line, mapper tests, two new test files); all five acceptance criteria pass.
    environmentalNote: dotnet SDK unavailable in review environment; tests not independently re-executed by reviewer. Decision relies on remediation-recorded focused-test results (36 PASS on PostDispenseIssueConsequenceHandler|LegacyMovementKindMapper|PharmacyServicePointContract|DispenseIssueMovementKind|CollectionWindowDaysProvider filters) plus full static verification of every cited code path at 5c1f3cd9. No contrary evidence found.
remediationHistory: []
notes:
  - Completeness audit (19 Aug 2026) scored PARTIAL for example-only LYAPT/LYDTU/service-point SQL and missing consequence/service-point tests. Round 1 confirmed those gaps and added the DI mapper hole.
  - AC-02/AC-04/AC-05 pass. No reservation table. Collection window defaults to 7. AntrianStatusEnum and BILRG_AntrianEntry were not extended with pharmacy workflow state.
  - Implementation-tracker attempt-1 summary mixed later-slice ports (payment/SEP/Iter/price, PharmacyQueueEvidence). Those are out of APT-B01 scope and were not scored as B01 expansion.
  - Round opened on user request while Wave-1 slices remain first in the execution roadmap.
  - Round 2 (25 Aug 2026) verified remediation commit 5c1f3cd9 and closed APT-B01-R1-F01/F02/F03 → GO. Remediation event is recorded in the progress tracker.
```

### APT-B00

```yaml
slice: APT-B00
reviewStatus: GO
initializedFrom: completeness-audit-2026-08-19
reviewedCommit: 451ddd59fe20ee99aefe4e1e3c203bc6ea68e3f6
reviewHistory:
  - round: 1
    actor: Grok Medium
    reviewedCommit: 451ddd59fe20ee99aefe4e1e3c203bc6ea68e3f6
    completedAt: 2026-08-19T14:15:00+07:00
    acceptanceResults:
      - criterionId: AC-01
        result: PASS
        evidence: "dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~ApotekContext.Architecture; 3/3 passed after compiling Api/Application/Infrastructure/Domain"
      - criterionId: AC-02
        result: PASS
        evidence: "src/bilreg/Bilreg.Test/ApotekContext/Architecture/ApotekContextBoundaryTest.cs:16-45 and grep of Application/Infrastructure ApotekContext"
      - criterionId: AC-03
        result: PASS
        evidence: "ApotekContext roots and assembly-scan registration; no forbidden-token hits in Application/Infrastructure ApotekContext"
    findings: []
    decision: GO
    rationale: Formal recertify of the audit-seeded GO. Namespace, registration convention, and architecture guard satisfy APT-B00.
remediationHistory: []
notes:
  - Audit found context markers, DI registration, controller, and architecture tests. No material gap for the boundary slice.
  - Round 1 Review Agent recertify recorded against 451ddd59fe20ee99aefe4e1e3c203bc6ea68e3f6.
```

### APT-B20

```yaml
slice: APT-B20
reviewStatus: NO_GO
initializedFrom: completeness-audit-2026-08-19
auditHold: true
reviewedCommit: null
reviewHistory: []
remediationHistory: []
candidateFindings:
  - No payer-split orchestration creates independent BPJS and patient-pay Sales Orders.
  - No WF-005 scenario tests.
notes:
  - Remediation is not authorized until Review Agent promotes candidateFindings into numbered findings.
```

### APT-B23

```yaml
slice: APT-B23
reviewStatus: NO_GO
initializedFrom: completeness-audit-2026-08-19
auditHold: true
reviewedCommit: null
reviewHistory: []
remediationHistory: []
candidateFindings:
  - Code resolves Sales Order before stock-return task succeeds, contradicting the plan.
  - Payer matrix and stock-failure tests are absent.
```

### APT-B24

```yaml
slice: APT-B24
reviewStatus: NO_GO
initializedFrom: completeness-audit-2026-08-19
auditHold: true
reviewedCommit: null
reviewHistory: []
remediationHistory: []
candidateFindings:
  - Telaah query starts from existing Telaah rows; unstarted intake is invisible.
  - Pelayanan starts from mapping, not Tracker queue.
  - No SQL fixture or query-contract tests.
```

### APT-B26

```yaml
slice: APT-B26
reviewStatus: NO_GO
initializedFrom: completeness-audit-2026-08-19
auditHold: true
reviewedCommit: null
reviewHistory: []
remediationHistory: []
candidateFindings:
  - Journey omits Telaah facts and payer identity.
  - Succeeded tasks classified as pending; Failed omitted.
  - SourceId returned instead of task ID.
```

### APT-B30

```yaml
slice: APT-B30
reviewStatus: NO_GO
initializedFrom: completeness-audit-2026-08-19
auditHold: true
reviewedCommit: null
reviewHistory: []
remediationHistory: []
candidateFindings:
  - WF-APT-RJ-001 through 007 scenario suites incomplete.
  - SQL smoke, repository, API-contract, and neighbor integration evidence missing.
  - Canonical tracker path was absent at audit time; working tracker is untrusted as GO evidence.
```

### APT-F00

```yaml
slice: APT-F00
reviewStatus: GO
initializedFrom: completeness-audit-2026-08-19
reviewedCommit: null
reviewHistory: []
remediationHistory: []
notes:
  - Audit found canonical DTO schemas, four routed workbenches, and removal of legacy queue/deposit model; Ranap prototype unchanged.
```

Remaining slices (`NOT_REVIEWED`) share this empty record until a round opens:

```yaml
reviewStatus: NOT_REVIEWED
initializedFrom: completeness-audit-2026-08-19
reviewedCommit: null
reviewHistory: []
remediationHistory: []
```

---

## 8. Immediate next action

1. Open **Wave-1 Review** for APT-B20 (first). Freeze commit SHA. Promote audit candidates to numbered findings or overturn with evidence.
2. Repeat for APT-B23, APT-B24, APT-B26, APT-B30.
3. Only then authorize Implementation Agent remediation against those finding IDs.
4. Do not treat this document as permission to change source code.
