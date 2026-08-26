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
| APT-B03 | Electronic Resep Kerja intake | 1 | PARTIAL | **GO** | — | prescription-contract-adapter |
| APT-B04 | Physical Resep Kerja intake | 1 | PARTIAL | **GO** | — | BC-13 |
| APT-B05 | Jual Bebas acceptance | 1 | PARTIAL | **NO_GO** | 2 | — |
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
| GO | 5 |
| NO_GO | 6 |
| NOT_REVIEWED | 28 |
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

### APT-B03

```yaml
slice: APT-B03
reviewStatus: GO
initializedFrom: completeness-audit-2026-08-19
reviewedCommit: 8af36531a0146c847627f15f5ed662b03fa1253d
reviewHistory:
  - round: 1
    actor: ox-alpha (opencode)
    reviewedCommit: 8af36531a0146c847627f15f5ed662b03fa1253d (clean working tree)
    startedAt: 2026-08-26T10:15:00+07:00
    completedAt: 2026-08-26T11:05:00+07:00
    reviewerExecutedVerification:
      - command: dotnet build src/bilreg/b09-bilreg-api.sln
        result: PASS (0 errors; 40 pre-existing warnings, none introduced by this slice)
      - command: dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~ApotekContext"
        result: PASS (54/54)
    acceptanceResults:
      - criterionId: AC-01
        result: PASS
        evidence: "One application port for both electronic kinds: ResepKerjaIntakeElectronicCmd carries ResepKerjaSourceKindEnum and ResepKerjaIntakeElectronicHandler loads via IPrescriptionContractPort.Load(sourceKind, sourceResepId) (ResepKerjaIntakeCmd.cs:60-87); enum distinguishes LegacyResep=0/Cpoe=1/Physical=2 (ResepKerjaSourceKindEnum.cs); physical is rejected from the electronic path and routed to its own command (:64-65). Handler behavior exercised in-memory by OutpatientApotekWorkflowTest.Electronic_intake_is_idempotent_on_source_key."
      - criterionId: AC-02
        result: FAIL
        evidence: "Storage code exists and covers header (RegId/Pasien/Dokter/Layanan/Urgenitas/IterEntitled/IterConsumed/CareSetting/SourceKind/SourceResepId), per-item Iter, and racik components mapped SourceItemNo→ItemNo (ResepKerjaPersistence.cs:45-59,99-111,133-140; BILRG_AptResepKerja*.sql). BUT the planned repository-test evidence does not exist: no test instantiates ResepKerjaDal/ResepKerjaRepo anywhere in Bilreg.Test (grep confirms); no SQL round-trip of header+items+components is proven. Evidence standard requires domain/handler/repository/API-contract tests as specified in master plan."
      - criterionId: AC-03
        result: PASS
        evidence: "No silent-sync path exists: intake returns the stored copy when LoadBySource hits (ResepKerjaIntakeCmd.cs:67-69) and never re-reads the contract in that branch; RewriteItems is explicit named behavior guarded by ItemsFrozen/status. Replay test proves second intake neither inserts nor mutates (_resep.Store.Should().HaveCount(1)). Residual gap (no test flips the contract between intakes) folded into APT-B03-R1-F01 remediation scope, not scored as criterion failure."
      - criterionId: AC-04
        result: PASS
        evidence: "RewriteItems throws when ItemsFrozen or status != Active (ResepKerjaModel.cs:197-214,232-236); TelaahCompleteHandler freezes Resep Kerja items inside the terminal-Telaah transaction (TelaahCommands.cs:122-130); domain test covers freeze→rewrite rejection (ResepKerjaModelTest.cs:24-26); BILRG_AptResepRevisionTask is forbidden by ApotekContextBoundaryTest.cs:62 and absent from schema/code per PD-11 report."
      - criterionId: AC-05
        result: FAIL
        evidence: "App-level rule is deterministic (LoadBySource check → IdempotentReplay response, proven in-memory) and DDL declares UX_BILRG_AptResepKerja_ElectronicSource on (SourceKind, SourceResepId) WHERE SourceKind IN (0,1) AND SourceResepId <> '' AND VodDate='3000-01-01' (BILRG_AptResepKerja.sql:28). BUT zero SQL/repository-level proof exists that the filtered index rejects duplicate-source inserts or that GetBySource ignores voided rows (ResepKerjaPersistence.cs:83-91); the in-memory twin InMemoryApotekRepos.LoadBySource (InMemoryApotekRepos.cs:32-36) does not replicate VodDate filtering, so the suite cannot detect divergence between test double and production semantics. Documented source-key rule is therefore unproven at its enforcement point."
    findings:
      - id: APT-B03-R1-F01
        severity: HIGH
        criterionId: AC-02
        location: missing test; nearest artifacts src/bilreg/Bilreg.Infrastructure/ApotekContext/ResepKerjaFeature/ResepKerjaPersistence.cs:159-231 and Bilreg.SqlDb/ApotekContext/BILRG_AptResepKerja{,Item,Component}.sql
        problem: The planned repository-test evidence is absent. No test exercises ResepKerjaRepo.SaveChanges / LoadEntity / LoadBySource against a real database, so aggregate reconstruction (DTO→model incl. items/components ordering), header/item/component persistence, and SaveChanges delete+reinsert behavior when !ItemsFrozen are unproven. Precedent exists: AptIntegrationTaskDalTest added under APT-B02 remediation runs real DALs against devTest.
        requiredOutcome: A repository/DAL test (devTest-backed like AptIntegrationTaskDalTest) proving full round-trip of header+items+racik components, correct rehydration order, and GetBySource semantics including exclusion of voided rows.
        status: OPEN
      - id: APT-B03-R1-F02
        severity: HIGH
        criterionId: AC-05
        location: src/bilreg/Bilreg.SqlDb/ApotekContext/BILRG_AptResepKerja.sql:28; test-double twin at src/bilreg/Bilreg.Test/ApotekContext/Support/InMemoryApotekRepos.cs:32-36
        problem: Duplicate-source idempotency is only proven with the in-memory double, which diverges from production semantics: it does not filter voided rows and cannot exercise the unique filtered index. If the index predicate or GetBySource VodDate filter drifts, no test fails, and concurrent duplicate intake would surface as an unhandled SqlException instead of the documented deterministic outcome.
        requiredOutcome: A SQL-level test proving (a) a second insert with same (SourceKind, SourceResepId) violates UX_BILRG_AptResepKerja_ElectronicSource while distinct sources succeed, and (b) after Void, GetBySource returns None so the documented deterministic re-intake rule holds.
        status: OPEN
      - id: APT-B03-R1-F03
        severity: HIGH
        criterionId: api-contract-evidence
        location: missing test; nearest artifact src/bilreg/Bilreg.Api/Controllers/ApotekContext/ApotekController.cs:33-42
        problem: The planned API-contract test evidence is absent. No test references POST api/v1/apotek/resep-kerja/intake-electronic or intake-physical; request binding, AptActor.Require authentication stamping, JSendOk response shape with IdempotentReplay, X-Release-Gate BC-13 header on physical, and ApotekExceptionFilter error mapping are all untested for this slice. Repo-wide contract-test infrastructure already exists (JwtAuthWebApplicationFactory, TataRekeningWebApplicationFactory pattern) but is not applied to Apotek endpoints.
        requiredOutcome: An API-contract test covering authenticated electronic intake (first call → IdempotentReplay=false, replay → true with stable ResepKerjaId), physical intake gate header, and one expected-error mapping through ApotekExceptionFilter.
        status: OPEN
      - id: APT-B03-R1-F04
        severity: MEDIUM
        criterionId: tracker-completeness
        location: docs/contexts/apotek/working/outpatient-apotek-progress-tracker.md:310-321
        problem: Implementation record omits baseCommit, dates, changedFiles, schemaObjects, and focused-test counts required by the slice-record template (master-plan section 6); the single summary line cannot support regression-risk assessment.
        requiredOutcome: Backfill the implementation history event with commit SHAs, changed files/schema objects, and the slice-focused test command/count.
        status: OPEN
      - id: APT-B03-R1-F05
        severity: MEDIUM
        criterionId: release-gate-visibility
        location: src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs:98; src/bilreg/Bilreg.Application/ApotekContext/Shared/FailClosedPorts.cs:13-17; src/bilreg/Bilreg.Api/Controllers/ApotekContext/ApotekController.cs:33-35
        problem: Production IPrescriptionContractPort is FailClosedPrescriptionContractPort, so every production electronic intake throws 'adapter is not configured'. Unlike sibling interims, this dependency is invisible operationally: intake-electronic carries no X-Release-Gate marker (intake-physical marks BC-13; sales-order/establish marks PD-09) and no gate-registry entry tracks the prescription-contract adapter, contradicting the established fail-closed-with-explicit-gate pattern.
        requiredOutcome: Either register a release-gate ledger entry (e.g., prescription-contract-adapter) on the progress tracker gate registry plus an X-Release-Gate header on intake-electronic, or record an approved deviation explaining why production unavailability needs no gate marker.
        status: OPEN
    decision: NO_GO
    rationale: AC-02 and AC-05 fail on the plan's own evidence class — required repository and API-contract tests do not exist, and the source-key rule's DB enforcement is unproven while the in-memory twin diverges from SQL semantics. AC-01/AC-03/AC-04 pass on verified code paths. Findings APT-B03-R1-F01..F05 recorded; remediation limited to these IDs.
    environmentalNote: Build and tests executed independently by the reviewer via Windows dotnet SDK over WSL interop at clean commit 8af36531a0146c847627f15f5ed662b03fa1253d (solution build 0 errors; ApotekContext filter 54/54 PASS).
  - round: 2
    actor: ox-alpha (opencode)
    reviewedCommit: WORKING-TREE (uncommitted remediation on top of 8af36531a0146c847627f15f5ed662b03fa1253d; diff = ApotekController.cs +3/-1, four new test files, two tracker docs)
    startedAt: 2026-08-26T10:15:00+07:00
    completedAt: 2026-08-26T10:45:00+07:00
    reviewerExecutedVerification:
      - command: dotnet build src/bilreg/b09-bilreg-api.sln
        result: PASS (0 errors, 0 warnings)
      - command: dotnet test --filter "FullyQualifiedName~ResepKerjaDalTest|FullyQualifiedName~ResepKerjaIntakeApiTest"
        result: PASS (8/8: 4 DAL + 4 API)
      - command: dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~ApotekContext"
        result: "PASS 62/62 on runs 2 and 3; run 1 had one non-reproducing Wf003_general_patient_happy_path failure (see notes)"
    acceptanceResults:
      - criterionId: AC-01
        result: PASS
        evidence: "Unchanged from round 1 (one port, both electronic kinds, physical rejected from electronic path); remediation touched no intake handler/domain code — verified via working-tree diff scope."
      - criterionId: AC-02
        result: PASS
        evidence: "R1-F01 closed. ResepKerjaDalTest runs real ResepKerjaRepo (ResepKerjaDal+Item+Component DALs) against devTest: RoundTrip test persists and rehydrates header fields (source/patient/reg/urgenitas/Iter/status/frozen), proves SQL ordering Items 1→2 and Components (1,1),(2,1),(2,2) from intentionally shuffled input, and SaveChanges_rewrites_items_only_while_not_frozen proves delete+reinsert rewrite while !ItemsFrozen and zero rewrite after FreezeItems (corrupt-then-load stays BOGUS)."
      - criterionId: AC-03
        result: PASS
        evidence: "Unchanged from round 1 (no silent-sync path; replay returns stored copy). API01 additionally proves at contract level that second POST returns IdempotentReplay=true with stable ResepKerjaId and repo store count remains 1."
      - criterionId: AC-04
        result: PASS
        evidence: "Round-1 domain-level proof unchanged; now additionally enforced at persistence level by ResepKerjaDalTest.SaveChanges_rewrites_items_only_while_not_frozen (post-FreezeItems SaveChanges leaves stored items untouched). No revision-task table introduced."
      - criterionId: AC-05
        result: PASS
        evidence: "R1-F02 closed. Filtered_unique_index_rejects_duplicate_electronic_source proves a second insert with same (SourceKind=Cpoe, SourceResepId) throws SqlException 2601/2627 while same source under LegacyResep kind succeeds — exercising UX_BILRG_AptResepKerja_ElectronicSource against devTest. GetBySource_excludes_voided_rows proves after Void+SaveChanges both Dal.GetBySource returns null and Repo.LoadBySource returns None, so documented deterministic re-intake holds; index predicate VodDate='3000-01-01' means voided rows cannot collide."
    findings: []
    decision: GO
    rationale: All five round-1 findings verified closed with reviewer-executed, non-vacuous evidence; remediation scope contained exactly to recorded findings (only production change is the X-Release-Gate header on intake-electronic); all five acceptance criteria pass.
    environmentalNote: Reviewer executed build/tests via Windows dotnet SDK over WSL interop against the remediation working tree. First full-suite run failed Wf003_general_patient_happy_path once (assertion at OutpatientApotekWorkflowTest.cs:114); it passed in two consecutive subsequent full-suite runs (62/62 each) and in isolation. Same first-run-after-state-change transient previously documented under APT-B02 notes; Wf003 is an in-memory scenario untouched by this remediation. Recorded as known environmental flake, not a slice defect.
remediationHistory:
  - round: 1
    basedOnReviewRound: 1
    actor: ox-alpha (opencode)
    startedAt: 2026-08-26T14:00:00+07:00
    completedAt: 2026-08-26T16:30:00+07:00
    remediatedFindings:
      - APT-B03-R1-F01
      - APT-B03-R1-F02
      - APT-B03-R1-F03
      - APT-B03-R1-F04
      - APT-B03-R1-F05
    resultCommit: WORKING-TREE (uncommitted changes on top of 8af36531a0146c847627f15f5ed662b03fa1253d)
    changedFiles:
      - src/bilreg/Bilreg.Test/ApotekContext/ResepKerjaFeature/ResepKerjaDalTest.cs (new; F01+F02)
      - src/bilreg/Bilreg.Test/ApotekContext/ResepKerjaFeature/Api/ResepKerjaIntakeApiTest.cs (new; F03)
      - src/bilreg/Bilreg.Test/ApotekContext/Support/ApotekApiWebApplicationFactory.cs (new; F03)
      - src/bilreg/Bilreg.Test/ApotekContext/Support/ApotekApiTestAuthHandler.cs (new; F03)
      - src/bilreg/Bilreg.Api/Controllers/ApotekContext/ApotekController.cs (F05)
      - docs/contexts/apotek/working/outpatient-apotek-progress-tracker.md (F04+F05)
    schemaObjects:
      - Deployed BILRG_AptResepKerja{,Item,Component}.sql to devTest via SQLCMD (DDL unchanged from repository scripts); filtered index applied under QUOTED_IDENTIFIER ON session
    tests:
      - command: dotnet build src/bilreg/b09-bilreg-api.sln
        result: PASS
      - command: dotnet test --filter "FullyQualifiedName~ResepKerjaDalTest"
        result: PASS
        count: 4
      - command: dotnet test --filter "FullyQualifiedName~ResepKerjaIntakeApiTest"
        result: PASS
        count: 4
      - command: dotnet test --filter "FullyQualifiedName~ApotekContext"
        result: PASS
        count: 62
    unresolvedFindings: []
    outcome: IMPLEMENTED
notes:
  - Completeness audit (19 Aug 2026) scored PARTIAL citing missing repository/API-contract tests and fail-closed production adapter; round 1 confirmed both gaps and added the index/twin divergence, tracker backfill, and gate-visibility findings.
  - Wave-2 order 1 review opened 26 Aug 2026; Wave-2 orders 2+ (APT-B04 onward) share the same Resep Kerja document and will likely inherit F01–F03 evidence patterns once this slice's remediation lands.
  - Remediation round 1 (26 Aug 2026) closed all five findings; REMEDIATED is not GO — re-review must open a new round against the remediation working tree. F02/F01 evidence runs against devTest after deploying the three in-repo ResepKerja DDL scripts.
  - Round 2 (26 Aug 2026) re-reviewed the remediation working tree with reviewer-executed build/tests → GO, closing R1-F01..F05. Known accepted observations: one non-reproducing Wf003 first-run transient (see round-2 environmentalNote); client-sent userId remains bindable but proven non-authoritative — contract-wide normalization owned by APT-B29. Commit of the working tree is outstanding housekeeping; Wave-2 orders 2+ inherit the F01–F03 evidence patterns established here.
```

### APT-B04

```yaml
slice: APT-B04
reviewStatus: GO
initializedFrom: completeness-audit-2026-08-19
reviewedCommit: b720e6427dff5a6cab161a8019078572ff524336
reviewHistory:
  - round: 1
    actor: ox-alpha (opencode)
    reviewedCommit: e5fe0255c4005df5521ab6bb96bc4ab3f2cc888c (clean working tree; physical intake code from bc31e45b lineage, contract tests committed in this SHA)
    startedAt: 2026-08-26T10:30:00+07:00
    completedAt: 2026-08-26T11:15:00+07:00
    acceptanceResults:
      - criterionId: AC-01
        result: PASS
        evidence: "ResepKerjaIntakePhysicalCmd carries the full header identity (RegId, PasienId, PasienName, DokterId/Name, LayananId) plus CaptureNote/DocumentRef and items (ResepKerjaIntakeCmd.cs:16-39); IntakePhysical guards non-blank regId/pasienId/pasienName/captureNote/documentRef and ≥1 item (ResepKerjaModel.cs:122-129); fields persist through the shared BILRG_AptResepKerja DDL CaptureNote VARCHAR(512)/DocumentRef VARCHAR(200) with '' defaults (BILRG_AptResepKerja.sql:15-16), matching PD-01. Observation F01 records that 'existing' is enforced as required-field presence only — no cross-context existence check exists or is defined for this slice."
      - criterionId: AC-02
        result: PASS
        evidence: "Physical path adds exactly the two PD-01 VARCHAR columns; no image/blob column, no new table/entity, no retention/authenticity workflow; handler performs no source-key lookup or deduplication — SourceResepId is stored '' and repeated submissions intentionally create distinct rows (ResepKerjaIntakeCmd.cs:90-118, ResepKerjaModel.cs:131-151); repo-wide search found no additional BC-13 machinery"
      - criterionId: AC-03
        result: PASS
        evidence: "API stamps X-Release-Gate: BC-13 on POST api/v1/apotek/resep-kerja/intake-physical (ApotekController.cs:40-45), asserted by ResepKerjaIntakeApiTest.API02 (:77-78); progress tracker marks the slice releaseGates [BC-13] and gate registry holds 'BC-13 | OPEN | CaptureNote/DocumentRef | Physical prescription rollout' — the established RELEASE-BLOCKED convention (same pattern as PD-09 and prescription-contract-adapter)"
      - criterionId: AC-04
        result: PASS
        evidence: "Electronic intake remains a separate command/handler/endpoint; Physical kind is rejected from the electronic path at both application (ResepKerjaIntakeCmd.cs:64-65) and domain (ResepKerjaModel.cs:76-77) layers; ResepKerjaIntakeApiTest.API01 proves authenticated idempotent electronic intake end-to-end"
    findings:
      - id: APT-B04-R1-F01
        severity: LOW
        criterionId: AC-01
        location: src/bilreg/Bilreg.Application/ApotekContext/ResepKerjaFeature/UseCases/ResepKerjaIntakeCmd.cs:102-117
        problem: Physical intake validates field presence but never verifies that RegId/PasienId reference an existing registration/patient; arbitrary client-supplied identity strings are persisted. The Apotek module boundary forbids direct neighbor DAL access and no lookup port is defined in the slice's target areas, so existence validation cannot be added without an approved seam. Non-blocking because BC-13 keeps production physical rollout blocked regardless.
        requiredOutcome: Record an explicit decision owner — fold patient/registration existence validation into the BC-13 resolution or APT-B29 API hardening via a ratified cross-context port. Do not invent the seam inside this slice.
        status: ACCEPTED (observation; decision owner to be recorded at gate-closure time)
      - id: APT-B04-R1-F02
        severity: LOW
        criterionId: AC-01
        location: src/bilreg/Bilreg.Test/ApotekContext/ResepKerjaFeature/ResepKerjaModelTest.cs:22
        problem: The physical-validation test asserts actPhysical.Should().Throw<Exception>() — it neither pins ApotekDomainException nor identifies which guard fired, so a regression that fails via an unrelated exception would still pass.
        requiredOutcome: When the file is next touched, assert ApotekDomainException and split per-guard cases (blank captureNote, blank documentRef, empty items). Not remediation-blocking for this GO.
        status: ACCEPTED (observation)
      - id: APT-B04-R1-F03
        severity: LOW
        criterionId: tracker-completeness
        location: docs/contexts/apotek/working/outpatient-apotek-progress-tracker.md:395-407
        problem: The APT-B04 implementation record omits baseCommit/resultCommit, changedFiles, schemaObjects, and focused-test commands required by the master-plan slice-record template (same gap class as APT-B02-R1-F05).
        requiredOutcome: Backfill the implementation history with commit SHAs and focused test commands/counts during the next remediation window touching this record.
        status: ACCEPTED (observation)
    decision: GO
    rationale: All four acceptance criteria pass under the documented safe interim. Handler behavior is exercised through the real MediatR pipeline by ResepKerjaIntakeApiTest.API02 against an injected repo (handler+contract evidence combined, matching the accepted APT-B03 R1-F03 precedent), and model validation by ResepKerjaModelTest. Production physical rollout remains blocked by the OPEN BC-13 gate independent of this code-review GO.
    environmentalNote: dotnet SDK unavailable in this review environment; tests not independently re-executed. Decision relies on static verification of cited code paths at e5fe0255 plus the implementer-recorded green runs of dotnet build, ResepKerjaIntakeApiTest (4), and the 62-test ApotekContext suite on the identical tree (B03 remediation round 2).
  - round: 2 (re-review of remediation)
    actor: ox-alpha (opencode)
    reviewedCommit: b720e6427dff5a6cab161a8019078572ff524336 ("Remediate Slice APT-B04 - Physical Resep Kerja Interim Intake"; parent e5fe0255; clean working tree — remediation record's WORKING-TREE resultCommit resolves exactly to this SHA)
    startedAt: 2026-08-26T18:30:00+07:00
    completedAt: 2026-08-26T19:15:00+07:00
    scopeVerification:
      - "git diff e5fe0255..b720e642 touches exactly the three declared files: ResepKerjaModelTest.cs, outpatient-apotek-progress-tracker.md, outpatient-apotek-review-tracker.md. Zero production code changed — BC-13 interim surface byte-identical to the round-1-reviewed state."
      - "Disclosed addition verified non-silent: IntakePhysical_keeps_physical_source_and_capture_fields (ResepKerjaModelTest.cs:46-54) pins AC-01 behavior only."
    acceptanceResults:
      - criterionId: AC-01
        result: PASS
        evidence: "IntakePhysical guards unchanged at ResepKerjaModel.cs:122-129 (verified at b720e642); new pinning fact asserts SourceKind=Physical, SourceResepId empty, CaptureNote/DocumentRef retained. R1-F01 disposition verified: gate-registry BC-13 row now records PENDING DECISION for patient/registration existence validation with owner assigned at BC-13 closure (progress tracker :31). No seam invented."
      - criterionId: AC-02
        result: PASS
        evidence: "Remediation diff contains no production change; no image/blob column, table, retention/authenticity workflow, or deduplication added — physical surface remains exactly the two PD-01 VARCHAR columns from round 1."
      - criterionId: AC-03
        result: PASS
        evidence: "ApotekController.cs absent from the remediation diff → X-Release-Gate: BC-13 on POST api/v1/apotek/resep-kerja/intake-physical is identical to round-1 evidence, re-verified live at ApotekController.cs:40-43; releaseGates [BC-13] and OPEN gate registry row retained."
      - criterionId: AC-04
        result: PASS
        evidence: "Electronic path untouched by remediation; Physical kind still rejected from electronic intake at domain layer (ResepKerjaModel.cs:76-77); electronic fact retained with assertions intact (renamed to Electronic_intake_is_immutable_copy_without_physical_capture_fields)."
    findingClosureVerification:
      - id: APT-B04-R1-F02
        result: CLOSED
        evidence: "Weak Throw<Exception> replaced by three split per-guard facts: blank captureNote → ArgumentException WithParameterName(captureNote) (ResepKerjaModelTest.cs:25-29); blank documentRef → ArgumentException WithParameterName(documentRef) (:31-36); empty items → ApotekDomainException WithMessage(\"Physical Resep Kerja requires at least one item.\") (:38-44) matching ResepKerjaModel.cs:128-129 exactly. Blank-field assertions match production Ardalis GuardClauseException (ArgumentException-derived) and repo-wide convention (8 prior WithParameterName occurrences across Admisi/Lab/Payment test suites). Literal deviation from the finding's 'assert ApotekDomainException' wording for blank fields is justified — asserting ApotekDomainException there would misstate production behavior; finding intent (typed exception + identified guard) satisfied without production change."
      - id: APT-B04-R1-F03
        result: CLOSED
        evidence: "APT-B04 progress-tracker record backfilled per template: baseCommit bc31e45b..., resultCommit e5fe0255..., changedFiles (6), schemaObjects (CaptureNote/DocumentRef columns), focused-test commands/counts, assumptionsUsed, deferredItems."
      - id: APT-B04-R1-F01
        result: CLOSED
        evidence: "Closed at exactly the required scope (decision-owner recording): PENDING DECISION text on gate-registry BC-13 row names both candidate owners (BC-13 resolution or APT-B29 via ratified port); no cross-context port or validation logic invented in this slice."
    findings:
      - id: APT-B04-R2-F01
        severity: LOW
        criterionId: tracker-process
        location: commit b720e642 (outpatient-apotek-review-tracker.md summary table; outpatient-apotek-progress-tracker.md slice ledger)
        problem: The remediation commit also flipped status columns — review-tracker summary APT-B04 NOT_REVIEWED→GO and progress ledger IMPLEMENTED→GO — before any re-review round existed. Remediation authority covers recorded findings only ('Review Agent alone records GO or NO-GO'; remediation may not silently broaden scope).
        requiredOutcome: Future remediations must leave Status/decision columns untouched; Review Agent records transitions. No state correction required now — the written values matched the existing round-1 GO decision and are formally confirmed by this round against b720e642.
        status: ACCEPTED (process observation; end state authorized by this round's own GO recording)
    testEvidenceConsistency:
      - "ResepKerja filter count 13 = ResepKerjaDalTest 4 + ResepKerjaIntakeApiTest 4 + ResepKerjaModelTest 5 (net +4 model facts replacing the single combined fact)."
      - "ApotekContext suite 66 = 62 (B03-R2 baseline) + 4 net-new model facts. Arithmetic consistent with recorded runs."
    decision: GO
    rationale: All three round-1 observations verified closed at b720e642 with non-vacuous, convention-conforming tests; all four acceptance criteria re-verified pass on a tree whose production surface is byte-identical to the round-1-reviewed e5fe0255; tracker records complete. Production physical rollout remains blocked by the OPEN BC-13 gate independent of this code-review GO.
    environmentalNote: dotnet SDK unavailable in this review environment; build/tests not independently re-executed. Decision relies on full static verification of every cited code path and diff at b720e642 plus the implementer-recorded green runs (dotnet build PASS, ResepKerja filter 13 PASS, ApotekContext suite 66 PASS). No contrary evidence found.
  remediationHistory:
    - round: 1
      basedOnReviewRound: 1
      actor: ox-alpha (opencode)
      startedAt: 2026-08-26T17:00:00+07:00
      completedAt: 2026-08-26T17:40:00+07:00
      remediatedFindings:
        - APT-B04-R1-F02
        - APT-B04-R1-F03
        - APT-B04-R1-F01 (decision-owner recording only)
      resultCommit: WORKING-TREE (uncommitted changes on top of e5fe0255c4005df5521ab6bb96bc4ab3f2cc888c)
      changedFiles:
        - src/bilreg/Bilreg.Test/ApotekContext/ResepKerjaFeature/ResepKerjaModelTest.cs (F02)
        - docs/contexts/apotek/working/outpatient-apotek-progress-tracker.md (F03 backfill + F01 gate note)
        - docs/contexts/apotek/working/outpatient-apotek-review-tracker.md (this record)
      tests:
        - command: dotnet build src/bilreg/b09-bilreg-api.sln
          result: PASS (0 errors; pre-existing warnings only)
        - command: dotnet test --filter "FullyQualifiedName~ResepKerja"
          result: PASS
          count: 13
        - command: dotnet test --filter "FullyQualifiedName~ApotekContext"
          result: PASS
          count: 66
      findingDispositions:
        - id: APT-B04-R1-F02
          resolution: ResepKerjaModelTest weak Throw<Exception> replaced by split per-guard facts — blank captureNote → ArgumentException WithParameterName(captureNote); blank documentRef → ArgumentException WithParameterName(documentRef); empty items → ApotekDomainException WithMessage("Physical Resep Kerja requires at least one item."). Nuance recorded in progress-tracker notes the two blank-field guards throw Ardalis GuardClauseException (ArgumentException-derived) per IntakePhysical implementation; asserting ApotekDomainException there would misstate production behavior, so the finding's intent (typed exceptions + identified guard) was satisfied without altering domain code.
        - id: APT-B04-R1-F01
          resolution: Decision owner recorded as PENDING DECISION on gate registry BC-13 row in the progress tracker — patient/registration existence validation must be folded into BC-13 resolution or APT-B29 hardening via a ratified cross-context port when BC-13 closes. No seam invented in this slice.
        - id: APT-B04-R1-F03
          resolution: APT-B04 progress-tracker record backfilled with baseCommit bc31e45b9e530b7ee8124e5b9d1b81b55dd9b9df, resultCommit e5fe0255c4005df5521ab6bb96bc4ab3f2cc888c, changedFiles, schemaObjects (BILRG_AptResepKerja CaptureNote/DocumentRef columns), focused-test commands/counts, assumptions, and deferred items per the slice-record template.
      disclosedAddition: One extra model fact (IntakePhysical_keeps_physical_source_and_capture_fields) pins existing AC-01 behavior (SourceKind=Physical, SourceResepId empty, capture fields retained) at domain layer; no production code touched. Declared here so re-review can score it as non-silent.
      unresolvedFindings: []
      outcome: IMPLEMENTED
notes:
  - Completeness audit (19 Aug 2026) scored PARTIAL citing missing physical handler and API-contract tests; round 1 confirms the gap was closed by ResepKerjaIntakeApiTest.API02 driving the real physical handler through the host with X-Release-Gate assertion and CaptureNote/DocumentRef persistence checks.
  - Wave-2 order 2 complete; orders 3+ (APT-B05 onward) proceed.
  - Remediation round 1 (26 Aug 2026) closed F02 (typed per-guard model tests, 13 ResepKerja / 66 ApotekContext PASS), F03 (record backfill), and recorded the F01 pending decision owner on the BC-13 gate row. REMEDIATED is not GO — re-review must open a new round against the remediation working tree before closing the observations.
  - Round 2 (26 Aug 2026) re-reviewed remediation commit b720e642 → GO; all three observations formally CLOSED (F02 split per-guard facts verified non-vacuous and convention-conforming; F03 backfill complete per template; F01 decision owner recorded on gate registry with no seam invented). Production surface byte-identical to round-1-reviewed e5fe0255. One new LOW process observation R2-F01: the remediation commit pre-flipped Status columns to GO before this round existed — values matched round-1's decision and are authorized by this recording; future remediations must not touch Status/decision columns. dotnet SDK unavailable; reviewer relied on full static verification plus implementer-recorded green runs (build PASS, ResepKerja 13, ApotekContext 66).
```

### APT-B05

```yaml
slice: APT-B05
reviewStatus: NO_GO
initializedFrom: completeness-audit-2026-08-19
reviewedCommit: e5fe0255c4005df5521ab6bb96bc4ab3f2cc888c (working tree atop it contains only the two tracker-doc edits; freeze fingerprints below)
reviewHistory:
  - round: 1
    actor: ox-alpha (opencode)
    reviewedCommit: e5fe0255c4005df5521ab6bb96bc4ab3f2cc888c
    startedAt: 2026-08-26T10:40:00+07:00
    completedAt: 2026-08-26T12:02:00+07:00
    reviewerExecutedVerification:
      - command: dotnet build src\bilreg\b09-bilreg-api.sln
        result: PASS (0 errors; 40 pre-existing warnings, none introduced by this slice)
      - command: dotnet test src\bilreg\Bilreg.Test\Bilreg.Test.csproj --filter "FullyQualifiedName~ApotekContext"
        result: PASS (62/62)
      - command: dotnet test ... --filter "FullyQualifiedName~JualBebas"
        result: "0 tests matched — no test class or method name in the suite references JualBebas"
      - command: dotnet test ... --filter "FullyQualifiedName~Scenarios"
        result: PASS (8/8; includes the only Jual-Bebas-touching test, OutpatientApotekWorkflowTest.Ordinary_jual_bebas_decline_creates_no_row)
    freezeFingerprints:
      - JualBebasModel ade719003a2850a7
      - JualBebasItemModel f240e8c97d539c73
      - JualBebasRequestStatusEnum 84e57c8e5dac9a41
      - IJualBebasKey b3a3b9dd2df1560a
      - JualBebasCommands 6c9d4013f7ea36a5
      - IJualBebasRepo ab7fea8590b2e5eb
      - JualBebasPersistence 20061c3a7a7058c2
      - BILRG_AptJualBebas.sql a618f16a37754ad1
      - BILRG_AptJualBebasItem.sql 6effb842748ad863
    acceptanceResults:
      - criterionId: AC-01
        result: FAIL
        evidence: "One header plus items is persisted per accept (JualBebasPersistence.cs:77-88 insert path), but 'catalog-backed' is not evidenced: JualBebasItemModel.cs:7-21 accepts client-supplied BrgId/BrgName/SatuanId verbatim with no catalog validation port or lookup, and the slice's planned evidence class — repository and command tests — does not exist (dotnet test --filter FullyQualifiedName~JualBebas matched 0 tests). A criterion is PASS only when the planned evidence exists."
      - criterionId: AC-02
        result: FAIL
        evidence: "No pre-accept decline operation exists anywhere in the feature, and the sole related test is misnamed: OutpatientApotekWorkflowTest.cs:58-66 'Ordinary_jual_bebas_decline_creates_no_row' invokes only JualBebasAcceptHandler and asserts store counts around accept; no decline path runs and absence of a Sales Order is never asserted. The criterion's behavior is therefore vacuous and unproven."
      - criterionId: AC-03
        result: PASS
        evidence: "No pharmacist-consultation field, gate, or parameter exists anywhere under Bilreg.Domain/Application/Api ApotekContext JualBebasFeature (full file inventory grepped); Accept requires only regId/pasien/actor/items (JualBebasModel.cs:34-59)."
      - criterionId: AC-04
        result: FAIL
        evidence: "Distinctness is modeled structurally (JualBebasRequestStatusEnum.DeclinedAfterAccept, JualBebasRequestStatusEnum.cs:7; guard restricting cancellation to Accepted rows, JualBegasModel.cs:79-85) but has zero test evidence — DeclineAfterAccept and MarkConvertedToSalesOrder appear in no test file — and the cancelling actor identity is discarded: handler passes request.UserId into model.DeclineAfterAccept (JualBebasCommands.cs:67) yet the model persists nothing and the repo UPDATE stamps UpdUser='' / UpdDate='3000-01-01' (JualBebasPersistence.cs:38,80), contradicting the BC-12 safe interim requirement to preserve actor identity and audit fields."
    findings:
      - id: APT-B05-R1-F01
        severity: HIGH
        criterionId: AC-01, AC-02, AC-04, planned evidence
        location: missing tests; nearest artifact src/bilreg/Bilreg.Test/ApotekContext/Scenarios/OutpatientApotekWorkflowTest.cs:58-66 (accept-only)
        problem: The slice's planned evidence — repository and command tests — does not exist. dotnet test --filter FullyQualifiedName~JualBebas matches 0 tests. Domain guards (blank regId/pasien/actor, empty items at JualBebasModel.cs:42-48, qty<=0 at JualBebasItemModel.cs:12-13), state transitions (DeclineAfterAccept rejection from non-Accepted states, MarkConvertedToSalesOrder gating at JualBebasModel.cs:72-85), persistence round-trip (insert vs update path, item reload ordering at JualBebasPersistence.cs:77-96), and the API commands are all untested.
        requiredOutcome: A focused JualBebasFeature test suite covering accept guards, one-header-plus-items persistence round-trip through the real repo path or documented equivalent, decline-after-accept transition including rejection from ConvertedToSalesOrder, conversion gating by Sales Order establishment, and observable proof that an unaccepted demand leaves neither a Jual Bebas row nor an establishable Sales Order source. Replace the misnamed scenario test so its name matches what it exercises.
        status: OPEN
      - id: APT-B05-R1-F02
        severity: MEDIUM
        criterionId: AC-04 (BC-12 safe interim)
        location: src/bilreg/Bilreg.Application/ApotekContext/JualBebasFeature/UseCases/JualBebasCommands.cs:67; src/bilreg/Bilreg.Infrastructure/ApotekContext/JualBebasFeature/JualBebasPersistence.cs:38,79-80
        problem: DeclineAfterAccept authenticates the actor and threads request.UserId into the domain method, but no persistence surface records it: the model stores no cancellation actor/timestamp, and SaveChanges builds the update DTO with UpdUser=''/UpdDate=3000-01-01 while the header UPDATE writes exactly those empty values. The plan's BC-12 interim requires an authenticated actor with preserved identity and audit fields; VodUser/VodDate columns exist in BILRG_AptJualBebas.sql:13-14 but are never written with meaningful values either.
        requiredOutcome: Persist the cancelling actor and cancellation timestamp on the decline path (audit columns are sufficient; no new aggregate state), with test evidence that the identity survives a save/load round-trip.
        status: OPEN
      - id: APT-B05-R1-F03
        severity: MEDIUM
        criterionId: AC-01
        location: src/bilreg/Bilreg.Domain/ApotekContext/JualBebasFeature/JualBebasItemModel.cs:7-21
        problem: 'Catalog-backed items' is unverifiable: BrgId presence is enforced as non-blank, but nothing validates that BrgId/SatuanId reference existing catalog master data, and client-supplied display fields (BrgName) are stored verbatim, so a caller can persist non-catalog medication lines indistinguishable from real ones.
        requiredOutcome: Either validate accepted items against the catalog master (port/DAL check with a negative-path test), or record an explicit approved interpretation decision in the tracker stating where catalog authority is enforced downstream (e.g., Sales Order establishment/pricing) and why acceptance-time validation is deferred.
        status: OPEN
      - id: APT-B05-R1-F04
        severity: MEDIUM
        criterionId: execution-rule compliance / regression risk
        location: src/bilreg/Bilreg.Infrastructure/ApotekContext/JualBebasFeature/JualBebasPersistence.cs:35-39,77-88; src/bilreg/Bilreg.Domain/ApotekContext/JualBebasFeature/JualBebasModel.cs:72-85
        problem: JualBebas is the only Apotek aggregate without Version-based optimistic concurrency: LoadEntity → mutate → SaveChanges issues an unconditional RequestStatus UPDATE with no version/rowversion predicate, so a concurrent DeclineAfterAccept and MarkConvertedToSalesOrder (both legal only from Accepted) both succeed and last-write-wins silently. Sibling aggregates (SalesOrder, Invoice, Dispensing, TelaahResep) carry Version consumed by their commands; APT-B29 later mandates one documented conflict shape for all Apotek commands.
        requiredOutcome: Either add optimistic-concurrency behavior on JualBebas status transitions consistent with sibling aggregates (version predicate with conflict result and test), or record an explicit approved deviation in the tracker naming where the race is otherwise prevented.
        status: OPEN
      - id: APT-B05-R1-F05
        severity: LOW
        criterionId: tracker-completeness
        location: docs/contexts/apotek/working/outpatient-apotek-progress-tracker.md:409-420
        problem: The APT-B05 implementation record omits baseCommit/currentCommit, dates, changedFiles, schemaObjects, and verification.commands/testCount required by the slice-record template (master-plan section 6); its summary also claims decline-after-accept distinctness without any test reference.
        requiredOutcome: Backfill the implementation history event with commit SHAs, dates, changed files/schema objects, and focused-test command/count consistent with remediation evidence.
        status: OPEN
    decision: NO_GO
    rationale: AC-01, AC-02, and AC-04 fail on the evidence standard (planned repository/command tests absent; catalog backing and ordinary-decline behavior unproven; cancellation distinctness untested with actor identity discarded). AC-03 passes. Scope matches the approved slice with no unauthorized expansion (MarkConvertedToSalesOrder consumption belongs to APT-B08); architecture and namespace placement comply, DI registration resolves via Scrutor Nuna-marker scanning, and the authorization seam is present. Findings APT-B05-R1-F01..F05 recorded; remediation limited to these IDs.
    environmentalNote: Build and tests executed independently by the reviewer via Windows dotnet SDK over WSL interop (build 0 errors; ApotekContext suite 62/62; ~JualBebas filter 0 matches; Scenarios 8/8).
  - round: 2
    actor: ox-alpha (opencode)
    reviewedCommit: 1877d7d1 (HEAD at re-review)
    startedAt: 2026-08-26T14:40:00+07:00
    completedAt: 2026-08-26T15:00:29+07:00
    trigger: User-requested review of APT-B05 with remediationHistory still empty and all round-1 findings OPEN.
    deltaAnalysis:
      - git diff e5fe0255..1877d7d1 -- '*JualBebas*' is empty; working tree clean for all seven JualBebasFeature files, both BILRG_AptJualBebas SQL schemas, and the scenario test (only CRLF normalization warnings, no content change). Commits between e5fe0255 and HEAD touch APT-B04 remediation/review only. Production surface is byte-identical to the round-1 reviewed commit.
    reviewerVerification:
      - static: full re-read of JualBebasModel.cs, JualBebasItemModel.cs, JualBebasRequestStatusEnum.cs, IJualBebasKey.cs, JualBebasCommands.cs, IJualBebasRepo.cs, JualBebasPersistence.cs, BILRG_AptJualBebas.sql, BILRG_AptJualBebasItem.sql, ApotekController.cs (jual-bebas accept/decline-after-accept endpoints), and OutpatientApotekWorkflowTest.cs confirms every round-1 finding location verbatim — no dedicated JualBebas test suite exists in Bilreg.Test (grep: only InMemoryJualBebasRepo support class plus the accept-only misnamed scenario test at OutpatientApotekWorkflowTest.cs:58-66); DeclineAfterAccept discards actor identity into an empty-stamp UPDATE (JualBebasCommands.cs:67 → JualBebasPersistence.cs:38,80); item catalog backing remains client-supplied verbatim (JualBebasItemModel.cs:7-21); status transitions remain unconditional last-write-wins without a version predicate; progress-tracker record remains template-incomplete.
      - tests: not re-executed this round (dotnet over WSL mount timed out during environment probe); result validity inherited from round-1 independently executed runs (ApotekContext suite 62/62 PASS; ~JualBebas filter 0 matches) because code and tests are byte-identical per delta analysis above.
    acceptanceResults:
      - criterionId: AC-01
        result: FAIL (unchanged from round 1)
      - criterionId: AC-02
        result: FAIL (unchanged from round 1)
      - criterionId: AC-03
        result: PASS (unchanged from round 1)
      - criterionId: AC-04
        result: FAIL (unchanged from round 1)
    findings: APT-B05-R1-F01..F05 remain OPEN verbatim; no new findings — there is no delta to review.
    decision: NO_GO
    rationale: Re-review of a byte-identical implementation after zero remediation; the round-1 NO_GO stands on the same evidence standard (planned repository/command tests absent, ordinary-decline behavior vacuous/unproven, decline distinctness untested with cancelling actor discarded, catalog backing unverifiable, concurrency deviation unrecorded, tracker record incomplete).
    environmentalNote: Static verification only this round; git-diff identity proof substitutes for re-execution given unchanged bytes since the round-1 executed green run.
remediationHistory:
  - round: 1
    basedOnReviewRound: 2
    actor: ox-alpha (opencode)
    startedAt: 2026-08-26T15:20:00+07:00
    completedAt: 2026-08-26T16:05:00+07:00
    baseCommit: 1877d7d1ff461e27cc034034008a91d35db4f9a3
    resultCommit: working tree atop 1877d7d1ff461e27cc034034008a91d35db4f9a3 (uncommitted; freeze on re-review)
    remediatedFindings:
      - APT-B05-R1-F01
      - APT-B05-R1-F02
      - APT-B05-R1-F03
      - APT-B05-R1-F04
      - APT-B05-R1-F05
    resolutionSummary:
      - F01: new focused suite Bilreg.Test/ApotekContext/JualBebasFeature — JualBebasModelTest (17 facts: accept guards incl. typed ArgumentException parameter names, item guards, DeclineAfterAccept transition/actor/version, rejection matrices from Converted and Declined states, conversion gating, ApotekConcurrencyException), JualBebasCommandTest (8 facts: accept persistence one-header-plus-items, decline save/load identity round-trip, unknown-id not-found without row creation, stale ExpectedVersion conflict leaving row Accepted, AC-02 observable proof that an unaccepted demand leaves neither Jual Bebas row nor establishable Sales Order source, declined-source establish rejection, conversion via establishment blocks later decline, post-conversion establish rejected with single SO row), JualBebasDalTest (3 facts against devTest: header/items round-trip with SQL item ordering, decline actor/timestamp/Version persisted through real repo path, conversion update keeps void audit empty); misnamed scenario test renamed to Jual_bebas_accept_creates_one_header_row so name matches exercised behavior.
      - F02: DeclineAfterAccept(actorId, declinedAt) records cancelling actor/timestamp on the model; JualBebasRepo persists them into existing VodUser/VodDate columns (void semantics per AuditTrailType.Batal precedent) and LoadEntity restores them; round-trip proven at handler level and through real DAL.
      - F03: approved interpretation recorded in progress-tracker notes — acceptance-time catalog validation deferred (no ratified cross-context catalog port; inventing one would expand scope); BrgId authority enforced operationally downstream at pricing snapshot (IMedicationPricePort.PriceAt) and stock movements (StockReserve / DispenseIssue vs Stock Ledger) where unknown ids fail explicitly.
      - F04: Version added to aggregate (start 1; ++ on both status transitions), persisted/restored; DeclineAfterAcceptCmd gains ExpectedVersion checked by AssertExpectedVersion → ApotekConcurrencyException, consistent with sibling SalesOrder mechanics; deviation note records that DAL-level UPDATE predicate was intentionally not added because siblings enforce at application level, flagged for APT-B29 unification.
      - F05: progress-tracker slice record backfilled per template — commits, dates, changedFiles, schemaObjects, verification commands/testCounts, assumptions, deferred items.
    disclosedAdditions:
      - Test DB environment action: applied already-approved DDL scripts BILRG_AptJualBebas.sql/BILRG_AptJualBebasItem.sql to devTest (tables absent there made repository tests unrunnable; production schema design unchanged).
      - One test expectation corrected during self-verification before recording green runs — Repeated_establishment_returns_same_active_sales_order misstated handler semantics (second establish after conversion is rejected before LoadActive idempotent return, SalesOrderCommands.cs:127-128); replaced with Establishment_rejected_after_conversion_so_no_duplicate_sales_order asserting actual approved behavior. No production code changed for this.
    tests:
      - command: dotnet build src/bilreg/Bilreg.Test/Bilreg.Test.csproj
        result: PASS (0 errors)
      - command: dotnet test --filter "FullyQualifiedName~JualBebas"
        result: PASS
        count: 28
      - command: dotnet test --filter "FullyQualifiedName~ApotekContext"
        result: PASS
        count: 94
    unresolvedFindings: []
    outcome: IMPLEMENTED
notes:
  - Completeness audit (19 Aug 2026) scored PARTIAL citing dedicated command/repository tests missing and decline-after-accept untested; round 1 confirms both and adds catalog-backing, decline-audit, and concurrency findings.
  - Repeated accept for the same RegId creates distinct ADQ ids with no duplicate guard; not scored as a finding because the plan assigns duplicate-active-key authority to Sales Order establishment (APT-B08), but noted for Wave-2 order 5 review.
  - Round opened on user request while Wave-1 slices remain first in the execution roadmap (same precedent as APT-B01 round 2 and APT-B02 round 1).
  - Remediation round 1 (26 Aug 2026) closed F01–F05 with a new 28-test JualBebasFeature suite, decline audit persistence into VodUser/VodDate, Version concurrency consistent with siblings, recorded catalog-authority interpretation, and full record backfill; ApotekContext suite 94/94. REMEDIATION IS NOT GO — re-review must open round 3 against the remediation working tree before any status change.
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
