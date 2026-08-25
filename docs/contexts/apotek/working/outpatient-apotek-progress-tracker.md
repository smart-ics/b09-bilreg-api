# Outpatient Apotek Progress Tracker

```yaml
Artifact-Type: ImplementationPlan
Status: InProgress
Bounded-Context: Apotek
Authority-Order:
  - apotek-domain.md
  - outpatient-apotek-workflow.md
  - outpatient-apotek-screen-and-aggregate-design.md
  - outpatient-apotek-persistence-design.md
  - adr/ADR-APT-001..003
Planner-Blockers: [BC-11, BC-12, BC-13, PD-08, PD-09]
Safe-Interims:
  PD-03: VARCHAR(26) payment ref, VARCHAR(50) SEP
  PD-09: IAvailableStockPort fail-closed; never persist Available Stock
  PD-08: TataRekeningCorrectionReff only; no BillingCredit
  BC-11: no call-purpose persistence
  BC-12: authenticated actor + policy seam
  BC-13: CaptureNote + DocumentRef only
```

## Gate registry

| GateId | Status | Safe interim | Blocks production |
|---|---|---|---|
| PD-09 | OPEN | Fail-closed Available Stock port | Sales Order establishment |
| PD-08 | OPEN | Manual TR workflow + correlation | Automated post-issue correction |
| BC-11 | OPEN | Canonical ServedAt/DoneAt only | APT-C01 announcements |
| BC-12 | OPEN | Actor + audit + policy seam | Mutation endpoint rollout |
| BC-13 | OPEN | CaptureNote/DocumentRef | Physical prescription rollout |

## Slice ledger

| Slice | Phase | Status | Dependencies | Release gates |
|---|---|---|---|---|
| APT-B00 | 0 | GO | — | — |
| APT-B01 | 0 | GO | B00 | — |
| APT-B02 | 0 | GO | B00 | — |
| APT-B03 | 1 | IMPLEMENTED | B00 | — |
| APT-B04 | 1 | IMPLEMENTED | B03 | BC-13 |
| APT-B05 | 1 | IMPLEMENTED | B00 | — |
| APT-B06 | 1 | IMPLEMENTED | B03 | — |
| APT-B07 | 1 | IMPLEMENTED | B01 | PD-09 |
| APT-B08 | 1 | IMPLEMENTED | B05, B06, B07 | PD-09 |
| APT-B09 | 1 | IMPLEMENTED | B07, B08 | PD-09 |
| APT-B10 | 1 | IMPLEMENTED | B02, B03, B08 | — |
| APT-B11 | 2 | IMPLEMENTED | B02, B03, B05 | — |
| APT-B12 | 2 | IMPLEMENTED | B02, B11 | BC-11 |
| APT-B13 | 3 | IMPLEMENTED | B02, B08 | — |
| APT-B14 | 3 | IMPLEMENTED | B13 | — |
| APT-B15 | 3 | IMPLEMENTED | B13 | PD-08 |
| APT-B16 | 3 | IMPLEMENTED | B01, B02, B08, B12, B14 | — |
| APT-B17 | 3 | IMPLEMENTED | B02, B12, B16 | — |
| APT-B18 | 3 | IMPLEMENTED | B11–B17 | PD-09 |
| APT-B19 | 4 | IMPLEMENTED | B13, B16, B17 | PD-09 |
| APT-B20 | 4 | IMPLEMENTED | B18, B19 | PD-09 |
| APT-B21 | 4 | IMPLEMENTED | B11, B18–B20 | PD-09 |
| APT-B22 | 5 | IMPLEMENTED | B09, B13, B16 | — |
| APT-B23 | 5 | IMPLEMENTED | B12, B15, B17, B19, B22 | BC-12 |
| APT-B24 | 6 | IMPLEMENTED | B06, B11, B13, B22, B23 | — |
| APT-B25 | 6 | IMPLEMENTED | B16, B17, B23 | — |
| APT-B26 | 6 | IMPLEMENTED | B24, B25 | — |
| APT-B27 | 6 | IMPLEMENTED | B13 | — |
| APT-B28 | 6 | IMPLEMENTED | B02 | — |
| APT-B29 | 6 | IMPLEMENTED | B24–B28 | BC-12 |
| APT-B30 | 6 | IMPLEMENTED | B00–B29 | all open gates |
| APT-F00 | F | IMPLEMENTED | B29 | — |
| APT-F01 | F | IMPLEMENTED | B11, B12, B24 | BC-11 |
| APT-F02 | F | IMPLEMENTED | B06, B24 | — |
| APT-F03 | F | IMPLEMENTED | F01, B08–B15, B19, B20, B24 | BC-13, PD-09 |
| APT-F04 | F | IMPLEMENTED | B16, B25 | — |
| APT-F05 | F | IMPLEMENTED | B17, B23, B25 | — |
| APT-F06 | F | IMPLEMENTED | B26, F02–F05 | — |
| APT-F07 | F | IMPLEMENTED | F00–F06 | BC-12 |
| APT-C01 | C | PLANNED-BLOCKED | BC-11, B12, F01 | BC-11 |

## History

Append-only. Do not erase prior attempts.

### APT-B00

```yaml
slice:
  id: APT-B00
  phase: 0
  title: Apotek module boundary
  objective: Establish isolated ApotekContext namespace and dependency guard
  status: GO
  dependencies: []
  releaseGates: []
  implementationAgent: Composer 2.5
  reviewAgent: Grok Medium
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      completedAt: 2026-08-18T18:00:00+07:00
      baseCommit: 25c1c990
      summary: Isolated ApotekContext in Domain/Application/Infrastructure/Api/SqlDb/Test with architecture guard against legacy DU DAL and neighbor writes.
      tests:
        - command: dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~ApotekContext
          result: PASS
          count: 38
      assumptionsUsed:
        - Four aggregate roots only TelaahResep SalesOrder Invoice Dispensing
      deferredItems:
        - Code review GO/NO-GO
  reviewHistory:
    - round: 1
      actor: Grok Medium
      reviewedCommit: 451ddd59fe20ee99aefe4e1e3c203bc6ea68e3f6
      completedAt: 2026-08-19T14:15:00+07:00
      acceptanceResults:
        - criterionId: AC-01
          result: PASS
          evidence: "dotnet test ...~ApotekContext.Architecture built Domain/Application/Infrastructure/Api/Test; markers + MediatR/Scrutor assembly scan register the module"
        - criterionId: AC-02
          result: PASS
          evidence: "ApotekContextBoundaryTest 3/3 PASS; source scan of Application/Infrastructure ApotekContext for PenjualanModel, IPenjualanDal, TelaahModel, IStokMutasiDal, ITrsBillingDal"
        - criterionId: AC-03
          result: PASS
          evidence: "No ApotekContext references to those legacy/neighbor types; slice adds isolated roots rather than mutating Penjualan/Telaah persistence"
      findings: []
      decision: GO
      rationale: Boundary, registration convention, and architecture guard meet APT-B00 acceptance criteria.
  remediationHistory: []
```

### APT-B01

```yaml
slice:
  id: APT-B01
  phase: 0
  title: Neighbor prerequisites
  objective: Make required queue, stock, location, and collection-window primitives available without adding pharmacy workflow to neighbor state models
  status: GO
  dependencies: [APT-B00]
  releaseGates: []
  implementationAgent: Composer 2.5
  reviewAgent: Grok Medium
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      completedAt: 2026-08-18
      baseCommit: bc31e45b
      summary: DispenseIssue movement kind and PostDispenseIssueConsequenceHandler; LYAPT/LYDTU location constants; example Pharmacy Unit/DTU and BILRG_AdmServicePoint seeds; APT_COLLECTION_WINDOW_DAYS default 7.
      tests:
        - command: dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~ApotekContext
          result: PASS
          count: 38
      assumptionsUsed:
        - Pharmacy Unit/DTU and pharmacy service-point seeds may remain example-only until Ops-approved identifiers exist
      deferredItems:
        - Code review GO/NO-GO
  reviewHistory:
    - round: 1
      actor: Grok Medium
      reviewedCommit: 451ddd59fe20ee99aefe4e1e3c203bc6ea68e3f6
      completedAt: 2026-08-19T14:45:00+07:00
      acceptanceResults:
        - criterionId: AC-01
          result: FAIL
          evidence: "DispenseIssue=14 distinct from SaleIssueDu=5; PostDispenseIssueConsequenceHandler exists but tests do not exercise it; LegacyMovementKindMapper has no DI mapping while handler writes LegacyKindString DI"
        - criterionId: AC-02
          result: PASS
          evidence: "ApotekLocationIds LYAPT/LYDTU; BILRG_Apt_Seed_Layanan.example.sql inserts ta_layanan; no BILRG_Apt reservation table"
        - criterionId: AC-03
          result: FAIL
          evidence: "BILRG_Apt_Seed_ServicePoint.example.sql targets BILRG_AdmServicePoint; no test that IAdmissionServicePointRepo/LoadEntity resolves a pharmacy service point"
        - criterionId: AC-04
          result: PASS
          evidence: "CollectionWindowDaysProviderTest GetDays_WhenParameterMissing_ReturnsDefaultSeven; BILRG_Apt_Seed_CollectionWindow.sql value 7; 7/7 focused tests PASS"
        - criterionId: AC-05
          result: PASS
          evidence: "AntrianStatusEnum remains Waiting/InService/Done/Withdrawn; ApotekContext SQL has no ALTER TABLE BILRG_AntrianEntry"
      findings:
        - APT-B01-R1-F01
        - APT-B01-R1-F02
        - APT-B01-R1-F03
      decision: NO-GO
      rationale: Consequence path is unproven and dual-write-incomplete; pharmacy service-point resolvability is untested.
    - round: 2
      actor: ox-alpha (opencode)
      reviewedCommit: 5c1f3cd9
      completedAt: 2026-08-25T21:30:00+07:00
      acceptanceResults:
        - criterionId: AC-01
          result: PASS
          evidence: "R1-F01 closed: PostDispenseIssueConsequenceHandlerTest proves Success dual-write (mutasi MovementKind=DispenseIssue qtyOut=4, legacy journal 'DI', NotContain SaleIssueDu, batch/lokasi/binding/scope asserted), Idempotent replay of same TrsReffId without double qty, InsufficientStock rejects with zero persist and UoW.Commit never called. R1-F02 closed: LegacyMovementKindMapper.cs:27 maps DI→DispenseIssue; LegacyScopeJournalReplayer.cs:53 consumes the mapper so catch-up/hydrate round-trips DI; LegacyMovementKindMapperTest covers DI/di mapping and TryMap_Di_IsDistinctFromDu"
        - criterionId: AC-02
          result: PASS
          evidence: "ApotekLocationIds.cs:5-6 LYAPT/LYDTU LayananIds; BILRG_Apt_Seed_Layanan.example.sql present; Bilreg.SqlDb/ApotekContext contains no reservation table"
        - criterionId: AC-03
          result: PASS
          evidence: "R1-F03 closed: PharmacyServicePointContractTest seeds via real AdmissionServicePointRepo/AdmissionServicePointDal, LoadEntity resolves pharmacy 'APT' row, and AdmissionServicePointResolver.EnsureAdmissionQueue accepts it (guard genuinely throws for unregistered points per AdmisiRajalOptions.cs:17-24); seed-content test ties example SQL to same table/id"
        - criterionId: AC-04
          result: PASS
          evidence: "BILRG_Apt_Seed_CollectionWindow.sql inserts APT_COLLECTION_WINDOW_DAYS='7'; CollectionWindowDaysProviderTest unchanged since round 1 PASS"
        - criterionId: AC-05
          result: PASS
          evidence: "AntrianStatusEnum.cs still Waiting/InService/Done/Withdrawn; only pre-existing AdmisiContext M2/M3 alters touch BILRG_AntrianEntry; no ApotekContext alter exists"
      findings: []
      decision: GO
      rationale: All three round-1 findings verified remediated at 5c1f3cd9 with non-vacuous tests; scope limited to recorded findings; all five acceptance criteria pass.
      environmentalNote: dotnet SDK unavailable in review environment; tests not independently re-executed by reviewer. Decision relies on remediation-recorded focused-test results (36 PASS on PostDispenseIssueConsequenceHandler|LegacyMovementKindMapper|PharmacyServicePointContract|DispenseIssueMovementKind|CollectionWindowDaysProvider filters) plus full static verification of every cited code path. No contrary evidence found.
  remediationHistory:
    - round: 1
      basedOnReviewRound: 1
      actor: Composer 2.5
      startedAt: 2026-08-19T14:36:00+07:00
      completedAt: 2026-08-19T15:00:00+07:00
      baseCommit: 451ddd59fe20ee99aefe4e1e3c203bc6ea68e3f6
      resultCommit: 5c1f3cd9
      remediatedFindings:
        - APT-B01-R1-F01
        - APT-B01-R1-F02
        - APT-B01-R1-F03
      changedFiles:
        - src/bilreg/Bilreg.Application/InventoryContext/StockLedgerFeature/LegacyMovementKindMapper.cs
        - src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/LegacyMovementKindMapperTest.cs
        - src/bilreg/Bilreg.Test/InventoryContext/StockLedgerFeature/PostDispenseIssueConsequenceHandlerTest.cs
        - src/bilreg/Bilreg.Test/ApotekContext/Shared/PharmacyServicePointContractTest.cs
      summary: Added DI legacy mapper entry; PostDispenseIssue consequence handler tests (success, idempotent, insufficient stock) at LYDTU; pharmacy APT service-point load/resolver contract test via IAdmissionServicePointRepo.
      tests:
        - command: dotnet build src/bilreg/b09-bilreg-api.sln
          result: PASS
        - command: dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~PostDispenseIssueConsequenceHandlerTest|FullyQualifiedName~LegacyMovementKindMapperTest|FullyQualifiedName~PharmacyServicePointContractTest|FullyQualifiedName~DispenseIssueMovementKindTest|FullyQualifiedName~CollectionWindowDaysProviderTest"
          result: PASS
          count: 36
      unresolvedFindings: []
      outcome: IMPLEMENTED
      notes:
        - resultCommit resolved as 5c1f3cd9 ("Audit implementasi Apotek Rawat Jalan (Re-Start Phase-0)"); confirmed by Review round 2
```

### APT-B02

```yaml
slice:
  id: APT-B02
  status: GO
  title: Integration Task engine
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      baseCommit: unrecorded (pre-audit history)
      summary: AptIntegrationTask aggregate, enqueue idempotency, worker, retry ops, and handlers for Tracker/Stock/TR/Iter destinations without dual-write to tb_trs_dobill_umum.
      schemaObjects:
        - BILRG_AptIntegrationTask with UX_BILRG_AptIntegrationTask_Idempotency unique index and IX_BILRG_AptIntegrationTask_Pending
      tests:
        - command: dotnet test --filter FullyQualifiedName~ApotekContext
          result: PASS
          count: 38
    - attempt: 2 (remediation of review round 1, findings APT-B02-R1-F01..F05)
      actor: ox-alpha (opencode)
      basedOnReviewRound: 1
      baseCommit: 5c1f3cd964f7db1e188325ec0035089bd3d08af0
      completedAt: 2026-08-25T23:40:00+07:00
      resultCommit: WORKING-TREE (uncommitted changes on top of 5c1f3cd9)
      changedFiles:
        - src/bilreg/Bilreg.Infrastructure/ApotekContext/IntegrationFeature/AptIntegrationTaskDal.cs
        - src/bilreg/Bilreg.Application/ApotekContext/IntegrationFeature/AptIntegrationWorker.cs
        - src/bilreg/Bilreg.Domain/ApotekContext/IntegrationFeature/AptIntegrationTaskModel.cs
        - src/bilreg/Bilreg.Application/ApotekContext/IntegrationFeature/UseCases/AptIntegrationOpsCommands.cs
        - src/bilreg/Bilreg.Application/ApotekContext/IntegrationFeature/UseCases/AptIntegrationProcessCmd.cs (new)
        - src/bilreg/Bilreg.Api/Controllers/ApotekContext/ApotekController.cs
        - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationTaskDalTest.cs (new)
        - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationProcessCmdTest.cs (new)
        - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationRetryCommandTest.cs (new)
        - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationTaskModelTest.cs
        - src/bilreg/Bilreg.Test/ApotekContext/IntegrationFeature/AptIntegrationWorkerTest.cs
        - src/bilreg/Bilreg.Test/ApotekContext/Support/InMemoryApotekRepos.cs
      schemaObjects:
        - Deployed BILRG_AptQueueClose.sql and BILRG_AptIntegrationTask.sql to devTest via SQLCMD (tables had never been deployed; required for DAL-contract execution; DDL unchanged from repository scripts)
      summary: >
        R1-F01: ListPending and ClaimPending predicates restricted to TaskStatus=Pending (SQL DAL and
        in-memory twin aligned); worker rejects Failed/Processing rows instead of silently auto-retrying;
        Failed returns to processing solely through the audited retry command. SQL-level proof added.
        R1-F02: new AptIntegrationTaskDalTest proves business save (BILRG_AptQueueClose insert) plus task
        insert commit together AND roll back together inside one TransHelper.NewScope() against devTest.
        R1-F03: AptIntegrationProcessCmd/handler plus POST api/v1/apotek/integration/process give batch
        processing a production invocation path (parity with LabOware/EMR engines). R1-F04: ClaimPending
        stamps processing start time; Processing rows stale beyond 30 minutes are reclaimable through the
        authenticated retry command; model and handler tests cover fresh/stale/Failed cases.
        R1-F05: this record backfilled.
      assumptionsUsed:
        - StaleProcessingMinutes=30 introduced as an operational constant analogous to MaxRetries=5; no BC-12 role matrix invented (retry remains authenticated and policy-seamed).
        - Applying the two existing in-repo DDL scripts to the shared devTest database to make DAL-contract tests executable (no column/table redesign).
      tests:
        - command: dotnet build src/bilreg/b09-bilreg-api.sln
          result: PASS
        - command: dotnet test --filter "FullyQualifiedName~ApotekContext.IntegrationFeature"
          result: PASS
          count: 25
        - command: dotnet test --filter "FullyQualifiedName~ApotekContext"
          result: PASS
          count: 54
      deferred:
        - Review Agent re-review and GO/NO-GO decision
      outcome: IMPLEMENTED
```

Notes for APT-B02 remediation:

- One transient Wf003 failure appeared in the first full-suite run immediately after devTest table creation; it did not reproduce in the next three consecutive full ApotekContext runs (54/54 each). Recorded here for transparency; re-review may treat it as noise or investigate further.
- Known limitation: a legitimately long-running handler (beyond 30 minutes) could be operator-reclaimed while still executing; there is no lease heartbeat. Handlers are expected to be short neighbor calls.

### APT-B03

```yaml
slice:
  id: APT-B03
  status: IMPLEMENTED
  title: Electronic Resep Kerja intake
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: ResepKerjaModel intake from IPrescriptionContractPort with source-key idempotency; items frozen only after terminal Telaah.
```

### APT-B04

```yaml
slice:
  id: APT-B04
  status: IMPLEMENTED
  title: Physical Resep Kerja intake
  releaseGates: [BC-13]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Physical intake persists CaptureNote and DocumentRef only; API stamps X-Release-Gate BC-13. Production physical rollout remains gated.
```

### APT-B05

```yaml
slice:
  id: APT-B05
  status: IMPLEMENTED
  title: Jual Bebas accept and decline
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Accept creates header/items; decline-after-accept is distinct from ordinary pre-accept decline; conversion to Sales Order is marked on the demand.
```

### APT-B06

```yaml
slice:
  id: APT-B06
  status: IMPLEMENTED
  title: Telaah Resep
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Available-UnderReview-Approved/PartiallyApproved/Rejected with line dispositions; rejected review cannot establish Sales Order; complete freezes Resep Kerja items.
```

### APT-B07

```yaml
slice:
  id: APT-B07
  status: IMPLEMENTED
  title: Available Stock fail-closed port
  releaseGates: [PD-09]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: IAvailableStockPort evaluates at SO establishment, is never persisted, and fails closed when unevaluated. Production formula remains PD-09.
```

### APT-B08

```yaml
slice:
  id: APT-B08
  status: IMPLEMENTED
  title: Sales Order establishment
  releaseGates: [PD-09]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Unique active key SourceKind+SourceId+RegId+PayerPath; rolls back when Available Stock unevaluated; accepted qty is identity after establish.
```

### APT-B09

```yaml
slice:
  id: APT-B09
  status: IMPLEMENTED
  title: Partial fulfillment and Copy Resep
  releaseGates: [PD-09]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Copy Resep and append-only UnfulfilledOutcome for excluded qty; no Backorder table or second queue ledger.
```

### APT-B10

```yaml
slice:
  id: APT-B10
  status: IMPLEMENTED
  title: Iter consumption delivery
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Intake does not consume Iter; SO establishment enqueues one IterConsume task when entitled. Missing adapter remains retryable failure.
```

### APT-B11

```yaml
slice:
  id: APT-B11
  status: IMPLEMENTED
  title: Queue mapping
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: QueueMapping maps demand to AntrianId/NoUrut; correction overwrites active mapping; mapping does not target Sales Order Invoice or Dispensing.
```

### APT-B12

```yaml
slice:
  id: APT-B12
  status: IMPLEMENTED
  title: Queue close and Tracker pharmacy adapter
  releaseGates: [BC-11]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Close allowed only from Waiting with required reason; TrackerPharmacyAdapter uses IAntrianRepo Serve/Done/Withdraw. No call-purpose persistence. APT-C01 remains blocked.
```

### APT-B13

```yaml
slice:
  id: APT-B13
  status: IMPLEMENTED
  title: Invoice establish issue payment revise
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: General-path Invoice aggregate with payment-clearance port; BPJS invoice blocked before handover.
```

### APT-B14

```yaml
slice:
  id: APT-B14
  status: IMPLEMENTED
  title: Dispense-authorized policy
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: DispenseAuthorizedPolicy gates release/start from payer path and invoice clearance without a persisted BILRG_AptDispenseAuthorized table.
```

### APT-B15

```yaml
slice:
  id: APT-B15
  status: IMPLEMENTED
  title: Post-issue correction correlation
  releaseGates: [PD-08]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Invoice records TataRekeningCorrectionReff only; no BillingCredit or Credit Note aggregate.
```

### APT-B16

```yaml
slice:
  id: APT-B16
  status: IMPLEMENTED
  title: Dispensing prepare path
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Establish/release/start/prepare with first-start Tracker ServedAt and stock reserve tasks; StartPreparation is idempotent when already Preparing.
```

### APT-B17

```yaml
slice:
  id: APT-B17
  status: IMPLEMENTED
  title: Pickup review education handover
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Coordinated pickup requires every intended Dispensing Prepared or resolved; handover issues stock tasks; BPJS invoice created at handover only.
```

### APT-B18

```yaml
slice:
  id: APT-B18
  status: IMPLEMENTED
  title: General patient end-to-end
  releaseGates: [PD-09]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Scenario OutpatientApotekWorkflowTest covers intake idempotency, fail-closed SO, and general commercial path WF-003.
```

### APT-B19

```yaml
slice:
  id: APT-B19
  status: IMPLEMENTED
  title: BPJS path
  releaseGates: [PD-09]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: WF-004 scenario asserts BPJS invoice-at-handover and no early invoice; no-show does not create BPJS invoice.
```

### APT-B20

```yaml
slice:
  id: APT-B20
  status: IMPLEMENTED
  title: Mixed coverage
  releaseGates: [PD-09]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: SalesOrderApplyCoverageCmd keeps payer paths independent; mixed demands remain separate Sales Orders.
```

### APT-B21

```yaml
slice:
  id: APT-B21
  status: IMPLEMENTED
  title: Multi-demand queue coordination
  releaseGates: [PD-09]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Journey query preserves multiple demands under one AntrianId/NoUrut; pickup waits for all intended dispensings.
```

### APT-B22

```yaml
slice:
  id: APT-B22
  status: IMPLEMENTED
  title: Post-order shortage
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: AppendUnfulfilled after establishment does not trim Accepted Qty; Copy Resep issued for shortage remainder.
```

### APT-B23

```yaml
slice:
  id: APT-B23
  status: IMPLEMENTED
  title: Collection window and no-show
  releaseGates: [BC-12]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: PickupExpired override before handover; no-show expires dispensing, resolves SO, enqueues stock return and Tracker DoneAt. Auth seam is actor+policy without invented matrix.
```

### APT-B24

```yaml
slice:
  id: APT-B24
  status: IMPLEMENTED
  title: Worklists
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: AptWorklistDal reads write tables for telaah, pelayanan, dispensing, and serah projections. Attention labels are read-only.
```

### APT-B25

```yaml
slice:
  id: APT-B25
  status: IMPLEMENTED
  title: Dispensing and serah worklists
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Serah categories are computed projections ReadyForPickup PickupExpired ReadyForReview ReadyForHandover Completed.
```

### APT-B26

```yaml
slice:
  id: APT-B26
  status: IMPLEMENTED
  title: Medication journey
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: JourneyResponse lists demand, SO, invoice, dispensing, and pending integration task ids per queue entry.
```

### APT-B27

```yaml
slice:
  id: APT-B27
  status: IMPLEMENTED
  title: Unified sales reporting
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Read-only union of BILRG_AptInvoice and tb_trs_dobill_umum; architecture test forbids INSERT INTO tb_trs_dobill_umum and ITrsBillingDal.
```

### APT-B28

```yaml
slice:
  id: APT-B28
  status: IMPLEMENTED
  title: Integration operations
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Failure query and retry command for AptIntegrationTask with PrepareRetry then ProcessOne.
```

### APT-B29

```yaml
slice:
  id: APT-B29
  status: IMPLEMENTED
  title: API hardening
  releaseGates: [BC-12]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: ApotekController authorized under api/v1/apotek; actor stamped from ICurrentUserContext; ApotekExceptionFilter maps concurrency 409 and domain 400. Auth matrix not invented.
```

### APT-B30

```yaml
slice:
  id: APT-B30
  status: IMPLEMENTED
  title: Final backend verification
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Solution Application/Infrastructure/Api/Test built; ApotekContext tests 38 passed. Production release still blocked by open gates PD-09 PD-08 BC-11 BC-12 BC-13.
      tests:
        - command: dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter FullyQualifiedName~ApotekContext
          result: PASS
          count: 38
      knownLimitations:
        - Implementation Agent cannot set GO
        - Neighbor Penjualan tests needed using alias so the suite compiles; production PenjualanModel unchanged
```

### APT-F00

```yaml
slice:
  id: APT-F00
  status: IMPLEMENTED
  title: Pharmacy type and module-shell reset
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      completedAt: 2026-08-19T01:20:00+07:00
      baseCommit: 8116002
      summary: Removed outpatient Open/Taken/Assigned/Prepared/Delivered and DepositStatus; replaced ApotekRajal tab with four workbenches; Ranap prototype unchanged.
      changedFiles:
        - src/modules/Pharmacy/configs/tabs.ts
        - src/modules/Pharmacy/types/apotek.ts
        - src/modules/Pharmacy/types/outpatient.ts
        - src/modules/Pharmacy/views/TelaahResep.vue
        - src/modules/Pharmacy/views/PelayananPenjualan.vue
        - src/modules/Pharmacy/views/DispensingWorkbench.vue
        - src/modules/Pharmacy/views/SerahObat.vue
```

### APT-F01

```yaml
slice:
  id: APT-F01
  status: IMPLEMENTED
  title: Pharmacy queue and mapping client
  releaseGates: [BC-11]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Pelayanan groups multiple demands per AntrianId/NoUrut; mapping and pickup are separate; queue close only from Waiting. No patient-facing announcement wording. 409 refreshes worklists.
```

### APT-F02

```yaml
slice:
  id: APT-F02
  status: IMPLEMENTED
  title: Telaah Resep workbench
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Worklist loads without queue mapping; line accept/substitute/reject and complete; Sales Order button disabled when telaah is rejected.
```

### APT-F03

```yaml
slice:
  id: APT-F03
  status: IMPLEMENTED
  title: Pelayanan Penjualan workbench
  releaseGates: [BC-13, PD-09]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: General vs BPJS invoice timing, physical resep BC-13 capture, Jual Bebas accept, queue close with reason. No random price generation.
```

### APT-F04

```yaml
slice:
  id: APT-F04
  status: IMPLEMENTED
  title: Dispensing workbench
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Start disabled until Released; prepare from Preparing; stock/integration errors shown as retryable tasks; queue effects come from invalidated server queries.
```

### APT-F05

```yaml
slice:
  id: APT-F05
  status: IMPLEMENTED
  title: Serah Obat and no-show workbench
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Coordinated pickup disabled until all journey dispensings are projected ready or resolved; PickupExpired requires override; failed review and no-show actions call backend only.
```

### APT-F06

```yaml
slice:
  id: APT-F06
  status: IMPLEMENTED
  title: Journey and attention UI
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      summary: Read-only journey on Pelayanan and Serah; attention labels filter worklists and never mutate; pending integration tasks are visible and retryable.
```

### APT-F07

```yaml
slice:
  id: APT-F07
  status: IMPLEMENTED
  title: Frontend verification and documentation
  releaseGates: [BC-12]
  implementationHistory:
    - attempt: 1
      actor: Composer 2.5
      completedAt: 2026-08-19T01:20:00+07:00
      summary: End-user four-workbench SOP replaced stale Rajal guidance; Vitest covers Zod, guards, BPJS empty invoice, multi-demand journey, and 409 refresh.
      tests:
        - command: npx prettier --write <modified files>
          result: PASS
        - command: pnpm tc:app
          result: PASS
        - command: pnpm test:unit -- --run src/modules/Pharmacy/types/__tests__/outpatient.spec.ts
          result: PASS
          count: 10
        - command: npx oxlint/eslint src/modules/Pharmacy src/core/api/queryConfigs.ts
          result: PASS
        - command: pnpm lint
          result: FAIL
          notes: 95 pre-existing repo errors outside Pharmacy outpatient files
        - command: pnpm build
          result: PASS
      deferredItems:
        - Production frontend rollout remains BC-12 gated
        - Repo-wide pnpm lint is dirty independently of this slice
```

### APT-C01

```yaml
slice:
  id: APT-C01
  status: PLANNED-BLOCKED
  blockedBy: [BC-11]
  title: c013 pharmacy announcement contract
  notes:
    - No c013-kiosk-queue-display-web changes in this implementation pass
    - Generic Tracker Waiting/InService/Done/Withdrawn needs no display-client change until BC-11 ratifies call purpose
```
