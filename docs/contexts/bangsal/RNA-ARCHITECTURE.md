# RNA Architecture

This document is the canonical technical specification for realizing the business defined in [`docs/contexts/bangsal/RNA-DOMAIN.md`](RNA-DOMAIN.md) and the approved procedures indexed by [`docs/contexts/bangsal/rna-sop/RNA-SOP-INDEX.md`](rna-sop/RNA-SOP-INDEX.md). Business meaning remains owned by the domain document; operational sequence and evidence remain owned by the SOPs. The authoritative semantic contract for collaboration with Admisi is [`docs/contexts/bangsal/ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md); this architecture does not restate that cross-context contract.

## 1. Architecture Overview

RUANG RANAP Operational Management (RNA) is the target technical capability for two related authorities: inpatient Accommodation Management and truthful RUANG RANAP Service Execution recording. It is actor-facing through a ward operations host, UI-agnostic at the Application boundary, and integration-facing toward Admisi Rawat Inap, CPOE, Tarif, Tata Rekening, Registration/Patient Administration, Bed master data, Housekeeping, and Maintenance.

**Existing.** Bilreg is a .NET 8 modular monolith with separate `Bilreg.Domain`, `Bilreg.Application`, `Bilreg.Infrastructure`, and `Bilreg.Api` projects. MediatR, explicit SQL/Dapper persistence, repositories, ambient transaction scopes, JWT authentication, explicit `AuditLog`, and feature-local durable queues are established mechanisms. The codebase has no RNA aggregate, use case, persistence, projection, API, or integration implementation. Empty `AkomodasiContext` folders are scaffolding, not existing behavior.

**Target.** RNA is one bounded-context module named `RnaContext`, organized by business capability within the existing Clean Architecture projects:

- `Bilreg.Domain/RnaContext` owns RNA aggregates, value objects, lifecycle behavior, and domain policies.
- `Bilreg.Application/RnaContext` owns commands, queries, handlers, repository contracts, future authorization ports, integration ports, projections, transaction boundaries, and durable-delivery orchestration.
- `Bilreg.Infrastructure/RnaContext` owns repositories, deterministic DTO mapping, DALs, SQL-backed projections, legacy anti-corruption adapters, and inbox/outbox delivery.
- `Bilreg.Api` adapts authenticated transports and remains the composition root. It does not own RNA decisions.

The dependency direction is mandatory:

```text
Bilreg.Api            -> Bilreg.Application -> Bilreg.Domain
Bilreg.Infrastructure -> Bilreg.Application -> Bilreg.Domain
```

RNA is not a replacement for `BedUsageContext/WardFeature`, which owns ward, room, class, and bed master data. RNA references those stable identities while owning Bed Readiness History as operational transaction data. Bed master keeps the current operational readiness status as an RNA-maintained latest-transaction projection for fast lookup; that projection is not the authoritative audit trail. RNA also references Tarif-owned Service identity, remains distinct from CPOE intent and NERS documentation, and publishes execution facts to Tata Rekening without deciding Charge Eligibility.

Major target gaps are RNA write/read models, implementation of the approved CPOE, Admisi, and Tindakan/Tata Rekening contracts, the Tarif reference contract, legacy coexistence/persistence extension, and durable integration recovery. Contextual authorization is intentionally deferred to Phase-99 under ARCH-020. The Admisi collaboration semantics are approved in [`docs/contexts/bangsal/ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md). Target CPOE ↔ RNA semantics are defined in [`docs/contexts/bangsal/CPOE-RNA-INTEGRATION.md`](CPOE-RNA-INTEGRATION.md), and target RNA ↔ Tata Rekening semantics are defined in [`docs/contexts/bangsal/RNA-TATA-REKENING-INTEGRATION.md`](RNA-TATA-REKENING-INTEGRATION.md). Mandatory Bed Assignability, Waiting List processing, Accommodation Correction, Retained Accommodation, Mother-and-Baby Rooming-In, Release-to-Waiting-List inter-ward movement, and transactional Bed Readiness are approved business models. Section 17 makes each remaining implementation dependency explicit.

## 2. Codebase Evidence and Constraints

| Evidence | Verified location | Architectural implication |
|---|---|---|
| Layer projects and inward project references | `src/bilreg/Bilreg.Domain/Bilreg.Domain.csproj`, `Bilreg.Application.csproj`, `Bilreg.Infrastructure.csproj`, `Bilreg.Api.csproj` | RNA must use the existing four-layer modular-monolith boundary; Domain cannot depend on Application or Infrastructure. |
| RNA implementation is absent; accommodation placeholders are empty | `src/bilreg/Bilreg.Domain/AkomodasiContext/HousekeepingFeature`, `.../PakaiBedFeature`; no matching Application/Infrastructure modules | Every RNA type in this document is **Target**, not Existing. Empty folders must not be cited as implemented behavior. |
| Existing ward models are reference/master models | `Bilreg.Domain/BedUsageContext/WardFeature/BedModel.cs`, `KamarModel.cs`, `BangsalModel.cs`; `Bilreg.SqlDb/BedUsageContext/WardFeature/ta_bed.sql`, `ta_kamar.sql`, `ta_bangsal.sql` | RNA consumes identities through a port. `BedType.IsAktif` is not Bed Readiness and `ta_bed` is not an occupancy ledger. The designated current-readiness field remains only a projection of RNA history. |
| Established repository mapping uses Application interfaces, Infrastructure repositories/DALs, Dapper, and explicit DTO conversion | `IBedRepo.cs`, `BedRepo.cs`, `BedDal.cs`, `BedDto.cs` | RNA aggregate repositories follow explicit reconstruction and persistence mapping; SQL rows must not enter Domain/Application. |
| Admisi owns Waiting List and exposes a ward gateway | `AdmisiRanapContext/WaitingListFeature`; `IWardAccommodationGateway.cs` | RNA uses an Application-boundary contract and never mutates Admisi persistence. The governing collaboration semantics are in `docs/contexts/bangsal/ADMISI-RNA-INTEGRATION.md`. |
| Ward placement notification is currently a no-op; V1 relies on Waiting List persistence/API | `Bilreg.Infrastructure/AdmisiRanapContext/Integration/WardAccommodationGateway.cs` | The approved contract is not implemented. Adapters, durable delivery, acknowledgement, reconciliation, and acceptance tests remain implementation work; this is not an unresolved architecture contract. |
| `RegInapModel` owns inpatient doctor assignments, not accommodation | `Bilreg.Domain/AdmisiContext/RegFeature/RegInapModel.cs`; `ta_reg_inap.sql` | RNA references `RegId` and validates encounter facts through a port; it must not add accommodation behavior to the registration aggregate or infer location from `ta_reg_inap`. |
| CPOE target architecture exists, but no `CpoeContext` implementation exists | `docs/contexts/cpoe/CPOE-ARCHITECTURE.md`; absent `src/bilreg/*/CpoeContext` | Ordered RNA execution contracts are **Target** and require coordinated implementation or an explicit legacy adapter. RNA must not manufacture Clinical Order state. |
| Legacy Tindakan creates execution-like, billing, and journal records in one command | `TdkCreateTindakanCmd.cs`, `TindakanModel.cs`, `BILRG_Tindakan` DAL | `Tindakan` remains the billable-execution record after cutover, but RNA execution history remains authoritative and append-only. An owning application adapter creates at most one `Tindakan` per billable `ServiceExecutionFactId`; non-billable execution creates none. |
| Legacy ordered-tindakan lifecycle is only `Ordered`, `Executed`, `Cancelled`; ordered execution command is empty | `OrderTdkModel.cs`; `TdkCreateTindakanByOrderCmd.cs` | Legacy status alone cannot prove RNA's Service + Performer + Performed At execution fact or occurrence identity. |
| Application transaction abstraction and ambient implementation exist | `Bilreg.Application/Shared/IUnitOfWork.cs`; `Bilreg.Infrastructure/Shared/TransHelperUnitOfWork.cs` | RNA handlers own explicit local transaction scopes. Repositories must participate in the same ambient scope. |
| JWT and actor ID extraction exist, but contextual RNA authorization does not | `Bilreg.Api/Configurations/PresentationService.cs`; `Bilreg.Api/Authorization/HttpCurrentUserContext.cs` | The current implementation assumes baseline authentication and coarse-grained application access only. Fine-grained Ward, competency, correction, exception, and service-identity enforcement is intentionally deferred to Phase-99 under ARCH-020. |
| Explicit append-oriented compliance audit exists | `Bilreg.Domain/Shared/AuditLogFeature/AuditLog.cs`; `BILRG_AuditLog.sql`; `docs/shared/audit-log.md` | Material RNA commands write searchable audit entries in the same local transaction as state and mandatory outbound obligations. AuditLog does not replace aggregate history. |
| Feature-local durable retry exists | `LabOwareOutboundQueueModel.cs`, repository/DAL, processor, SQL, and tests | RNA may implement its own SQL-backed inbox/outbox and recovery projection; the Lab queue is evidence of a pattern, not a reusable RNA store. |

Explicit constraints:

- **Constraint:** SQL Server, explicit SQL, DTO/DAL/repository mapping, standard transaction audit columns, non-destructive void/history, and no database foreign-key constraints follow `docs/DATABASE.md`.
- **Constraint:** aggregate behavior remains in Domain; Application orchestrates; DALs and projections must not decide lifecycles.
- **Constraint:** `BedUsageContext/WardFeature` remains master-data authority. RNA may refresh only the designated current-readiness projection through an explicit master-owned port when appending readiness history; it must not convert master tables into operational history or occupancy write models.
- **Constraint:** Admisi owns Admission and Waiting List; CPOE owns Clinical Order intent and lifecycle; Tata Rekening owns every financial consequence.
- **Constraint:** domain-event names in `docs/contexts/bangsal/RNA-DOMAIN.md` express business facts. They do not mandate event sourcing or a generic event bus.
- **Constraint:** sensitive patient and clinical data must remain scoped to a legitimate care or operational relationship.

Explicit gaps:

- **Gap:** there is no authoritative RNA persistence, repository, handler, projection, controller, integration ledger, or test suite; the contextual authorization provider is a Phase-99 capability, not a current-phase implementation requirement.
- **Gap:** only items still marked open in `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` remain implementation gates. GAP-RNA-003/004/005/007 are closed.
- **Gap:** current code has no reliable source-of-truth mapping for legacy inpatient occupancy, retained accommodation, rooming-in, Bed Readiness, or RNA execution history.
- **Gap:** remaining cross-context contracts outside the approved Admisi collaboration may not yet provide the stable fact identity, acknowledgement, correction/reversal, or recovery semantics required by RNA. Admisi semantics are governed exclusively by `docs/contexts/bangsal/ADMISI-RNA-INTEGRATION.md`.

## 3. Module and Bounded-Context Boundaries

| Module | Responsibility | Owns | Depends on | Must not own |
|---|---|---|---|---|
| Accommodation Management | Establish and preserve allocation purpose, clinical-location designation, retention, Mother-and-Baby Rooming-In, internal transfer, inter-ward release, and correction history | `AccommodationAllocationModel`; allocation policies; registration-scoped accommodation history | Registration/care-context facts, Patient Social Data relationship, ward master references, Admisi Waiting List | Admission, Waiting List, transfer queue, EMR clinical content, encounter discharge, bed/room master data, BOR, Charge Eligibility or financial charges |
| Bed Operational Readiness | Record a Bed Readiness transaction that moves a referenced bed to Ready, Cleaning Required/In Progress, Blocked, or Out of Service | RNA-owned readiness history, latest-state projection, restriction/reason, occupancy-policy reference | Bed master reference, active-allocation facts, authenticated readiness evidence | Bed master identity/configuration, all allocation history, housekeeping or maintenance workflow administration |
| RNA Service Work and Execution | Receive ordered or Ad Hoc work, preserve source/occurrence identity, optionally assign a Performer, and record/correct the Service Execution Fact | `RnaServiceExecutionModel`; execution and correction history | CPOE or legacy source contract, Tarif Service reference, actor authorization | Service Definition/configuration, Clinical Order intent/lifecycle, specialized departmental fulfilment, outcome catalogues, NERS document content |
| Execution Fact Publication | Deliver RNA's authoritative billable Service Execution Fact or correction without financial interpretation | Delivery obligation, stable source-fact identity, acknowledgement state | Approved `RNA-TATA-REKENING-INTEGRATION.md` and CPOE association contract | Charge Eligibility, tariff, coverage, package, bill, journal, adjustment, or payment decisions. RNA's user-selected billable/non-billable classification only selects whether this delivery obligation exists. |
| Operational Projections and Recovery | Provide demand, occupancy, bed-availability, service work, history, and failed-delivery views | Read models and query DALs; recovery visibility | RNA-owned persistence plus authorized reference snapshots | Any state transition or second write authority |
| RNA Integration Boundary | Authenticate, deduplicate, translate, deliver, and reconcile cross-context facts | Application ports; Infrastructure inbox/outbox, legacy mappings, delivery attempts | Admisi, CPOE, Tata Rekening, Registration/Patient, NERS, identity/governance, Housekeeping/Maintenance | Neighbor repositories/tables as public contracts; neighbor business rules |

Cross-context authority is explicit:

- Admisi creates, owns, prioritizes, and closes its Waiting List. RNA owns Accommodation Assignment. Their collaboration, including rejection, assignment, correction, release, responsibility effects, acknowledgement, and reconciliation, is defined only by `docs/contexts/bangsal/ADMISI-RNA-INTEGRATION.md`.
- Inter-ward movement is Release-to-Waiting-List. RNA owns the source Accommodation Release and Admisi owns its resulting Waiting List work; the complete collaboration is defined only by `docs/contexts/bangsal/ADMISI-RNA-INTEGRATION.md`. EMR owns inter-ward clinical content.
- `BedUsageContext/WardFeature` supplies ward/room/bed/class references and active master status. RNA decides operational readiness and occupancy meaning.
- Tarif supplies the existing Service identity and owns Service Definition/configuration. RNA stores only the reference and minimum historical display snapshot.
- CPOE supplies prospective Clinical Order intent, occurrence, revision, and cancellation/discontinuation authority. RNA supplies the execution fact for order coordination.
- Tata Rekening consumes RNA's execution fact and solely evaluates Charge Eligibility and all financial consequences. RNA may expose delivery status, never the decision controls.
- NERS or another documentation domain owns clinical content. RNA neither copies it nor makes it an RNA completion requirement.
- Housekeeping and Maintenance supply authenticated recovery facts within their assigned responsibility. RNA owns the final Bed Readiness transition only according to the approved readiness policy.
- Patient Social Data owns the Mother-and-Baby relationship; RNA only validates that the Baby Medical Record references the Mother's Medical Record before activating Rooming-In.

For every accommodation assignment, Application first establishes the Domain-defined **Mandatory Bed Assignability** facts: the bed exists and is active, belongs to the intended Ward, is Ready, has no conflicting active allocation, and has capacity available under the occupancy policy. These are the only automated RNA placement checks. Gender, isolation, equipment, and hospital-specific operational policies remain Ward personnel decisions and must never cause RNA to reject Bed Assignment. Care Class and Care Context are not inputs to the decision; they are derived only from the active patient Clinical Accommodation after assignment, including an intensive-care context derived from an ICU, PICU, NICU, or equivalent accommodation. A Companion Accommodation is not a patient Clinical Accommodation.

## 4. Clean Architecture Layer Responsibilities

| Layer | Feature responsibilities | Permitted dependencies | Prohibited dependencies |
|---|---|---|---|
| Domain — target `Bilreg.Domain/RnaContext` | Aggregate construction/rehydration; invariant-preserving behavior; value objects; state machines; occupancy, accommodation-fact treatment, Mandatory Bed Assignability, Accommodation Correction, and truthful execution/correction behavior | Domain primitives and stable identity/reference types that carry no external behavior | BOR or financial rules, optional operational placement policies, Service Definition rules, Charge Eligibility, Application handlers, MediatR, SQL/DTO/DAL, HTTP/JWT, clocks, external SDKs, neighboring repositories |
| Application — target `Bilreg.Application/RnaContext` | Commands/queries/handlers; orchestration; transaction scopes; repository, projection, authorization, clock/ID, and integration ports; idempotency decisions; audit creation; result categories | Domain and inward contracts | SQL text, Dapper, transport DTOs, direct use of another context's DAL/table, financial or Clinical Order rule duplication |
| Infrastructure — target `Bilreg.Infrastructure/RnaContext` | Repositories; DTO mapping; DALs; SQL locking/version checks; projection queries; inbox/outbox; source adapters; legacy translation; delivery/retry | Application and Domain contracts; SQL/client libraries | Business lifecycle decisions, authorization by UI visibility, direct controller coupling |
| API / Presentation — `Bilreg.Api` | Transport authentication; request-to-command and result-to-response mapping; composition root; transport versioning; correlation propagation | Application commands/queries and Infrastructure registration | Aggregate mutation, repository/DAL access, SOP orchestration, business authorization decisions |

Read-model interfaces belong in Application because they describe consumer needs. Optimized SQL implementations belong in Infrastructure and may join RNA tables with approved reference sources for read-only display, but they must not decide whether an allocation, bed, or execution may transition and must never evaluate Charge Eligibility.

Repository contracts belong in Application in line with current Bilreg convention. Domain policies receive already-resolved domain facts and collections; they do not call repositories or integration gateways.

## 5. Application Use Cases

The following are target application interactions, not screens or endpoint definitions. Use cases marked as policy-gated must not be implemented beyond a safe rejection until the corresponding Section 17 gap is approved.

| ID | Use case | Kind | Purpose | Authority / initiator | Primary model | Dependencies | Transaction outcome |
|---|---|---|---|---|---|---|---|
| UC-RNA-001 | Receive Waiting List Entry | Integration Inbound | Receive authorized Admisi-owned Waiting List demand as Ward review work without taking Waiting List ownership | Authenticated Admisi service | Integration inbox + Waiting List review projection | [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md), registration/ward reference ports | Contract-defined inbox outcome and projection reference commit once; no allocation is created |
| UC-RNA-002 | List Waiting List Review Work | Query | Return caller-scoped Admisi-owned Waiting List entries for the Ward | Authorized RNA actor | Waiting List review projection | Query authorization | Read-only |
| UC-RNA-003 | Reject Waiting List Entry | Command | Record a Ward rejection without taking Waiting List ownership | RNA Coordinator/Head Nurse or approved Ward reviewer | Review record | Contextual authorization; [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) | RNA audit and the contract-defined outbound obligation commit; no RNA allocation is created |
| UC-RNA-004 | Assign Accommodation | Command | Activate one allocation and designate Clinical Accommodation after atomic Mandatory Bed Assignability validation | Authorized RNA placement actor | `AccommodationAllocationModel`; `BedModel` guard | Registration facts, bed master, allocation/bed repos; [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) | Allocation, bed occupancy epoch/version, audit, and the contract-defined outbound obligation commit atomically |
| UC-RNA-005 | Get Accommodation Detail and History | Query | Retrieve current clinical location, simultaneous purposes, transfers, releases, and corrections | Authorized care/operations/audit actor | Accommodation projections | Patient/care-context query scope | Read-only |
| UC-RNA-006 | Establish Retained Accommodation | Command | Keep an existing allocation Active while another accommodation becomes the treatment location | Ward Nurse or Head Nurse | `AccommodationAllocationModel` | Current-location facts, authorization | Allocation remains Active, continues consuming capacity and producing accommodation facts; audit commits |
| UC-RNA-007 | Release Retained Accommodation at Discharge | Command | Release every active retained allocation as part of discharge accommodation closure | Ward Nurse or Head Nurse | `AccommodationAllocationModel` | Authoritative discharge fact, accommodation repo | All retained releases and audit commit; RNA accommodation closure remains incomplete while any retained allocation is Active, without owning clinical discharge authorization |
| UC-RNA-008 | Start Mother-and-Baby Rooming-In | Command | Establish a Baby associated allocation linked to the Mother's distinct primary registration | Ward Nurse or Head Nurse | Associated `AccommodationAllocationModel` | Patient Social Data Mother-Baby reference, two registration facts, bed guard | One Baby associated allocation and association evidence commit without increasing bed capacity |
| UC-RNA-009 | End Mother-and-Baby Rooming-In | Command | End the association while preserving both registrations and histories | Ward Nurse or Head Nurse | Associated `AccommodationAllocationModel` | Accommodation repo | Association end and explicit Baby allocation outcome commit with audit |
| UC-RNA-010 | Transfer Accommodation Internally | Command | Move Clinical Accommodation within one RNA while preserving old allocation disposition | Authorized RNA transfer actor | Source and target `AccommodationAllocationModel`; target `BedModel` | Mandatory Bed Assignability, occupancy; bed/allocation repos | Target activation and clinical designation plus source release/reclassification commit in one local transaction |
| UC-RNA-011 | Release Accommodation for Inter-Ward Movement | Command | Release the source Clinical Accommodation under the approved Admisi collaboration | Authorized source Ward Nurse or Head Nurse | Source `AccommodationAllocationModel` | Care-context facts, authorization, bed repo; [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) | Source release, post-use readiness transaction, audit, and the contract-defined outbound obligation commit atomically |
| UC-RNA-012 | Deliver Inter-Ward Release | Integration Outbound / Background Process | Deliver the contract-defined release fact | RNA delivery worker | Outbox delivery record | [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) | Delivery and reconciliation effects follow the contract; RNA creates no transfer queue |
| UC-RNA-013 | Process Inter-Ward Patient from Waiting List | Reuse UC-RNA-001–004 | Apply the ordinary placement flow to contract-defined Admisi demand | Authenticated Admisi service and authorized destination RNA actors | Waiting List review + destination allocation | [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md), Mandatory Bed Assignability | No special inter-RNA state |
| UC-RNA-014 | Release Accommodation | Command | End one declared purpose at actual business time without implying discharge or Ready | Authorized RNA actor | `AccommodationAllocationModel` | Care-context facts, allocation/bed repos | Allocation release, post-use Bed state, audit, and recovery obligation commit atomically |
| UC-RNA-015 | Correct Accommodation Fact | Command | A Head Nurse appends an Accommodation Correction Fact without erasing history | Head Nurse of the original fact's Ward | Accommodation or Bed aggregate and correction-fact history | Same-Ward ownership, Tata Rekening finalization-status port, affected downstream ports | Original remains unchanged; correction fact, audit, and downstream correction obligations commit |
| UC-RNA-016 | Record Bed Recovery Progress | Command / Integration Inbound | Record an RNA-owned readiness transaction for Cleaning Required/In Progress, Blocked, Out of Service, recovery evidence, or return-to-service | Authorized RNA actor or authenticated Housekeeping/Maintenance source | `BedModel` | Readiness policy, service/actor authority | Readiness transaction/history, latest-state projection, and audit commit once per source fact |
| UC-RNA-017 | Record Verified Bed Ready | Command | Record the `Ready` readiness transaction only after required recovery conditions are verified | Approved final readiness verifier | `BedModel` | Readiness policy, active-allocation check, authorization | Ready transaction, latest-state projection, version increment, and audit commit atomically |
| UC-RNA-018 | List Bed Availability and Recovery Work | Query | Return policy-aware availability and unresolved recovery work by RNA/room | Authorized operations actor | Bed-availability projection | Bed master, RNA state, query scope | Read-only; result is advisory to a command recheck |
| UC-RNA-019 | Receive Ordered Service Work | Integration Inbound | Create one RNA work obligation per stable CPOE or legacy occurrence | Authenticated CPOE/approved legacy source | `RnaServiceExecutionModel` + inbox | Tarif Service reference, source authorization, registration/care context | Inbox, Pending work, audit, and acknowledgement commit once |
| UC-RNA-020 | Apply Ordered Service Change | Integration Inbound | Apply amendment, cancellation, or discontinuation facts without rewriting completed execution | Authenticated CPOE/approved legacy source | `RnaServiceExecutionModel` | Source identity/revision contract | Source fact and permitted pending-work transition commit idempotently |
| UC-RNA-021 | List RNA Service Work | Query | Return pending, assigned, executed, cancelled/withdrawn, or integration-blocked work within caller scope | Authorized RNA actor | Service-work projection | Contextual query authorization | Read-only |
| UC-RNA-022 | Get RNA Service Execution History | Query | Retrieve Service reference, source/occurrence, assignment, Performer, Performed At, corrections, and per-destination delivery state | Authorized care/audit actor | Execution-history projection | Contextual query authorization | Read-only |
| UC-RNA-023 | Assign RNA Service Work | Command | Optionally assign a responsible Performer/team without implying execution | Authorized RNA Coordinator/Head Nurse | `RnaServiceExecutionModel` | Actor/assignment authorization port | Assignment and audit commit atomically |
| UC-RNA-024 | Record Ordered Service Execution | Command | Record actual Performer and Performed At with an eligible Service when billable, or description when non-billable | Authorized Performer | `RnaServiceExecutionModel` | Tarif eligibility query, actor authorization, clock | Execution fact and audit commit atomically; Tata Rekening outbox exists only when billable |
| UC-RNA-025 | Withdraw Unexecuted Service Work | Integration Inbound / Command | End pending work from an authoritative source change without creating an execution fact | Authenticated source or authorized coordinator under source contract | `RnaServiceExecutionModel` | Source identity/revision contract | Work state and audit commit; no execution outbox |
| UC-RNA-026 | Record Ad Hoc or Independent Execution | Command | Record truthful source, authority basis, Performer, and Performed At with eligible Service when billable or description when non-billable, without fabricating a prospective order | Authorized Performer | `RnaServiceExecutionModel` | Tarif eligibility query, authority/actor authorization | Execution fact, authority record, and audit commit atomically; Tata Rekening outbox exists only when billable |
| UC-RNA-027 | Associate Subsequent Authorization Status | Integration Inbound | Associate CPOE-owned authorization/overdue fact with the RNA execution | Authenticated CPOE service | `RnaServiceExecutionModel` authority reference | CPOE source contract and inbox | Reference/status association commits once; actual execution is not erased |
| UC-RNA-028 | Correct RNA Service Execution | Command | Append a corrected Service Execution Fact or mark Entered in Error while preserving the original | Specifically authorized RNA correction actor | `RnaServiceExecutionModel` | Correction authority, affected integration ports | Correction, audit, and CPOE/Tata Rekening correction outboxes commit atomically |
| UC-RNA-030 | Deliver RNA Execution Fact to CPOE | Integration Outbound / Background Process | Deliver execution/correction facts for order coordination at least once | RNA delivery worker | Outbox delivery record | CPOE contract | Acknowledgement or failure state commits; execution is unchanged |
| UC-RNA-031 | Deliver RNA Execution Fact to Tata Rekening | Integration Outbound / Background Process | Deliver the fact “Service X was executed by Performer Y at time Z” without financial interpretation | RNA delivery worker | Outbox delivery record | Tata Rekening inbound execution-fact contract | Delivery outcome commits; retries reuse the same source-fact identity |
| UC-RNA-032 | List Integration Recovery Work | Query | Expose pending, stale, failed, rejected, or conflicting inbound/outbound obligations | Authorized support actor | Integration-recovery projection | Recovery authorization | Read-only |
| UC-RNA-033 | Retry or Reconcile Integration Delivery | Command / Background Process | Safely reclaim stale work, retry a stable obligation, or re-query an authoritative acknowledgement | Authorized support actor or scheduled worker | Inbox/outbox delivery record | Destination/source adapter | Attempt state commits without repeating aggregate behavior |

## 6. Use-Case Traceability

| Use case ID | Domain capabilities / rules | SOP references | Owning module | Authorization concern | Required tests |
|---|---|---|---|---|---|
| UC-RNA-001–004 | 3.1–3.2; BR-RNA-001–004, 008, 012–016a | SOP-RNA-A01; GAP-RNA-001/002 CLOSED; [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) | Accommodation Management | Source service, Ward review scope, placement scope | Contract acceptance tests plus bed race and current-location uniqueness |
| UC-RNA-005 | 3.3, 3.11; BR-RNA-002–014, 049 | A01–A06 | Operational Projections | Patient/care-context and RNA scope; audit access separated | Current vs retained/associated display; history order; unauthorized filtering |
| UC-RNA-006–007 | 3.3; BR-RNA-002–007b, 012–014 | SOP-RNA-A02; GAP-RNA-003 CLOSED | Accommodation Management | Ward Nurse or Head Nurse only | Remains Active; consumes capacity; publishes facts; discharge releases all retained allocations |
| UC-RNA-008–009 | 3.3; BR-RNA-008–014 | SOP-RNA-A03; GAP-RNA-004 CLOSED | Accommodation Management | Ward Nurse or Head Nurse; verified Mother-Baby relationship | Separate registrations/histories; every bed; one Primary + one Baby; no capacity increase; no BOR logic |
| UC-RNA-010 | 3.4; BR-RNA-004–007, 012–017, 020, 023–025 | SOP-RNA-A04; GAP-RNA-002 CLOSED | Accommodation Management | Transfer actor | Target unavailable under Mandatory Bed Assignability; atomic source/target disposition; retained branch; lock ordering |
| UC-RNA-011–013 | 3.4; BR-RNA-005–007, 016–019b, 023–025b | SOP-RNA-A05; GAP-RNA-005 CLOSED; [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) | Accommodation Management / Admisi Integration | Source release authority; ordinary destination placement authority | Contract acceptance tests and atomic source release/outbound obligation |
| UC-RNA-014–015 | 3.5, 3.11–3.12; BR-RNA-005–007, 020–025, 049–049c | SOP-RNA-A06; GAP-RNA-006 CLOSED | Accommodation Management | Release authority; Head Nurse only, same-Ward correction, pre-`FINALIZED` gate | Temporary Absence is explicitly not modeled; discharge-not-release; append-only correction of every eligible Accommodation Fact; downstream reconciliation |
| UC-RNA-016–018 | 3.5; BR-RNA-012, 015, 023–025b | SOP-RNA-A07; GAP-RNA-007 CLOSED | Bed Operational Readiness | Authenticated evidence source vs explicit final verifier | Required transaction fields; unresolved blocker; active allocation conflict; duplicate source fact; Ready race |
| UC-RNA-019–020 | 3.6–3.7; BR-RNA-026–036, 042–044, 060–063 | SOP-RNA-S01; GAP-RNA-008 CLOSED (DESIGN) / implementation dependency | Service Work and Execution | Source system/route/occurrence authority | Source dedupe; Bangsal-Layanan Tarif eligibility query; order revision ordering; legacy identity preservation |
| UC-RNA-021–025 | 3.6–3.7; BR-RNA-036–044, 050–052 | SOP-RNA-S01 | Service Work and Execution / Projections | RNA assignment and execution-recording authority | Query scoping; assignment not execution; eligible Service + Performer + Performed At for billable, description + Performer + Performed At for non-billable; occurrence uniqueness; withdrawal creates no fact |
| UC-RNA-026–027 | 3.8; BR-RNA-029, 031–035, 045–046, 050–052 | SOP-RNA-S02; GAP-RNA-008/010 CLOSED (DESIGN) / implementation dependency | Service Work and Execution | Professional/emergency/protocol authority; CPOE service authority | Truthful source; Bangsal-Layanan Tarif eligibility query for billable work; 24-hour-or-discharge deadline; acknowledgement/escalation; late review; overdue authorization does not erase execution |
| UC-RNA-028 | 3.9, 3.11; BR-RNA-045–049, 052–053, 059 | SOP-RNA-S03; GAP-RNA-011 CLOSED | Service Work and Execution | Correction privilege, second review, wrong-patient protection | Preserve original; Entered in Error; replacement linkage; downstream correction obligations |
| UC-RNA-030–031 | 3.10; BR-RNA-052, 054–059 | SOP-RNA-S04; GAP-RNA-012 CLOSED (DESIGN) | Billable Execution Publication | Delivery/recovery authority only; no tariff/settlement authority | Billable-only payload; stable fact identity; one linked Tindakan; idempotent acknowledgement; correction revision; per-destination retry |
| UC-RNA-032–033 | 3.11; BR-RNA-049, 059, 062 | A01, A05, S01–S04 exception/recovery paths | Operational Projections and Recovery | Support-only access; retry cannot change operational truth | Stale claim recovery; replay; permanent rejection; audit/correlation visibility |

Coverage not represented as an active use case:

- Companion Accommodation is covered by the ordinary Bed Assignment procedure with `IsCompanionBed = true`. It occupies a bed, remains associated with the inpatient registration, and is not a patient or Clinical Accommodation; downstream consumers receive the flag for reporting treatment.
- Temporary Absence is **not needed** and is not modeled, displayed, or actioned by RNA.
- Specialized non-RNA fulfilment is **out of scope** by BR-RNA-026–028. RNA may coordinate supporting tasks but cannot record another Executing Domain's completion.

## 7. Aggregate and Domain Model Realization

### `AccommodationAllocationModel` — Target

- **Owns:** one registration's declared use of one accommodation resource; purpose; occupant role; clinical designation; Mother-and-Baby rooming-in reference when associated; start/end; occupancy/reporting fact treatment; internal-transfer or inter-ward-release continuity; transition/correction history; Waiting List source references.
- **Child/value types:** `AccommodationPurposeEnum`, `OccupantRoleEnum`, `OccupancyTreatmentType`, `ReportingTreatmentType`, `RoomingInAssociationType`, `AccommodationTransitionModel`, and business-time/audit value types. No direct inter-RNA transfer aggregate or destination-decision model exists.
- **Protects:** BR-RNA-001–007, 009–011, 016–022, and 049 for its own lifecycle. Cross-allocation BR-RNA-002, 004, 008, 012–015, 017–019 are evaluated by domain policies from a transactionally consistent set loaded by Application.
- **Consistency boundary:** one allocation and its append-oriented history. It does not contain every allocation for a registration or bed.
- **Entry points:** create proposed/active allocation, designate/withdraw clinical status, establish retained status by Ward Nurse/Head Nurse, link/end verified Mother-and-Baby Rooming-In, release, cancel proposal, correct, or mark Entered in Error. Public property setters must not bypass these behaviors.
- **Facts:** allocation proposed/assigned/designated/retained/transferred/released, rooming-in started/ended, and correction facts may be returned to Application for direct orchestration; they do not require a generic event bus.
- **References:** `RegId`, `PasienId`, `BangsalId`, `KamarId`, `BedId`, related registration/allocation, Admisi Waiting List, and transfer IDs are identities/snapshots only.
- **Must not:** decide Admission/Waiting List status directly, own a transfer queue or destination ownership before Bed Assignment, own EMR clinical content, calculate BOR, decide discharge, master-data validity, tariff, bill, or another allocation's transition.

### `BedModel` — Target RNA operational aggregate

- **Owns:** one referenced bed's append-only RNA Bed Readiness History, restriction/reason, policy reference, version/occupancy epoch, and the latest-state projection derived from that history.
- **Child/value types:** `BedReadinessEnum`, `BedReadinessTransaction`, `BedRestrictionType`, `BedRecoveryEvidenceType`, `OccupancyPolicyReff`, and readiness transition history.
- **Protects:** BR-RNA-012, 015, and 023–025. The Application supplies current active allocations and verified recovery facts to the relevant domain policy.
- **Consistency boundary:** one bed's readiness transactions and latest-state projection, not bed master-data governance and not the complete accommodation history.
- **Entry points:** record authenticated readiness transitions for cleaning, block, out of service, resolved conditions, and verified Ready; each appends a transaction and refreshes the latest-state projection. Advance the occupancy epoch for an allocation decision.
- **Facts:** cleaning required/started, bed blocked/out of service/returned/ready.
- **References:** `BedId` and limited ward/room snapshot from `BedUsageContext`; Housekeeping/Maintenance source-fact identities.
- **Must not:** use `BedType.IsAktif` as readiness, store a single current-patient field as occupancy authority, or make capacity decisions without active allocations and occupancy policy.

### `RnaServiceExecutionModel` — Target

- **Owns:** one RNA work obligation/execution instance; patient/care context; billable Tarif Service reference or non-billable description; source and source occurrence; responsible RNA; optional assignment; actual Performer; Performed At; Recorded At; stable execution-fact identity; authority record; corrections; and per-destination delivery references.
- **Child/value types:** `TarifServiceReff`, `ExecutionSourceEnum`, `ExecutionAuthorityType`, `ServiceExecutionFactType`, and `ExecutionCorrectionModel`.
- **Protects:** BR-RNA-026–063, including truthful source, occurrence uniqueness, billable eligible-Service or non-billable description plus Performer + Performed At completeness, actual/recorded time separation, non-destructive correction, and publication identity.
- **Consistency boundary:** one service execution and its history. One Clinical Order or recurring schedule may reference multiple independent execution aggregates.
- **Entry points:** create ordered obligation, create ad hoc/independent execution, optionally assign, record execution, apply permitted source cancellation/change before execution, associate later authorization, correct, and mark Entered in Error.
- **Facts:** work received/created/assigned/cancelled; service executed; ad hoc execution recorded; execution corrected/entered in error; fact ready for CPOE/Tata Rekening publication.
- **References:** Clinical Order, Planned Occurrence, instruction revision, legacy source, registration, Tarif Service, Performer, RNA, and outbound fact identities.
- **Must not:** author/amend a Clinical Order; own Service Definition/configuration; execute specialized departmental work; copy NERS content; classify outcomes; decide Charge Eligibility, tariff, coverage, bill, journal, adjustment, or payment.

Cross-aggregate coordination belongs in Application. A local use case may atomically persist more than one RNA aggregate only when a domain invariant requires it and all rows share the Bilreg SQL transaction. Cross-context calls are never assumed atomic.

## 8. Persistence and Repository Strategy

**Target repositories:** `IAccommodationAllocationRepo`, `IBedOperationalRepo`, and `IRnaServiceExecutionRepo`, one per Aggregate Root. Contracts live in Application; implementations, DTOs, DALs, and SQL live in Infrastructure/SqlDb. Integration inbox/outbox and projections use dedicated Application persistence ports rather than masquerading as aggregate repositories.

Target transaction tables are feature-owned and use repository standards. The expected logical stores are:

- `BILRG_RnaAccommodationAllocation` plus append-oriented transition/correction details;
- `BILRG_RnaBedOperational` plus readiness/recovery history;
- `BILRG_RnaServiceExecution` plus authority and correction details;
- feature-local RNA inbox, outbox/delivery, source mapping, and request-idempotency stores.

These names are accepted target anchors, not evidence of current tables. Exact schemas and indexes belong to persistence design. Tables must use application-generated opaque IDs, PascalCase columns, integer workflow enums, standard `Crt/Upd/Vod` columns, no physical delete for business history, and no database FK constraints per `docs/DATABASE.md`.

Repository behavior must:

- reconstruct a complete aggregate deterministically and reject internally contradictory persisted state;
- insert new roots, compare aggregate version on update, and append immutable history/correction details;
- participate in the Application-owned ambient `IUnitOfWork` scope;
- keep source business time distinct from recording/audit time;
- apply the RNA Business Time Standard: `OccurredAt` is authoritative business time, `RecordedAt` is persistence time for audit/technical tracing only, and unknown business time uses `OccurredAt = RecordedAt`;
- for billable work, persist a stable Tarif Service reference and minimum display snapshot without copying Service Definition rules; for non-billable work, persist the required description without a Service reference;
- return explicit not-found and concurrency-conflict outcomes rather than silently insert/update the wrong lifecycle;
- never call an external service while a database transaction is open.

Concurrency support must include:

- optimistic `Version` comparison for every aggregate mutation;
- a per-bed operational row/epoch locked during capacity-affecting allocation decisions;
- a registration-scoped guard or equivalent filtered uniqueness protection so one registration cannot have two active Clinical Accommodation designations;
- stable lock ordering by bed identity when a local transaction touches source and target beds;
- unique source scope over ordered work, at minimum source system + source order + occurrence + instruction revision semantics defined by the contract;
- unique inbox source-fact identity and outbound fact/destination identity.

Legacy compatibility:

- `ta_bed`, `ta_kamar`, `ta_bangsal`, and related WardFeature data are accessed through explicit ports. RNA writes no allocation/history into master tables; only the designated current-readiness projection may be refreshed from the latest committed readiness transaction.
- `ta_reg_inap` and Registration repositories are accessed only through a care-context Application port; RNA does not own registration lifecycle. Mother-and-Baby validation uses a read port to Patient Social Data and does not copy or mutate that relationship.
- `BILRG_OrderTdk` remains behind a legacy anti-corruption adapter and becomes read-only for work before the approved facility/unit cutover. It cannot be an RNA repository or native CPOE authority.
- `BILRG_Tindakan` remains active as the billable-execution record. Its owning adapter links one row to `ClinicalOrderId`, `OrderOccurrenceId`, `ServiceExecutionFactId`, and `ServiceId`, enforces uniqueness by `ServiceExecutionFactId`, and creates no row for non-billable execution. It is not the RNA Service Execution Fact authority.
- Accommodation Stay uses a legacy-core plus RNA-extension model. The legacy Stay identity and existing fields remain authoritative; RNA adds only new structure/history in extension tables keyed by that stable identity. No bulk migration, backfill, or authority cutover is required.
- RNA repositories compose the legacy core and RNA extensions. RNA-originated changes to legacy-owned fields call the existing legacy application boundary and commit extensions, audit, and outbox in the same local transaction where possible.
- Legacy-originated writes are supported in parallel. Row-version/update-token checks and reconciliation detect changes that bypass RNA; missing legacy history is never fabricated. Historical views preserve source labels and IDs.

## 9. Read Models and Query Strategy

| Projection | Consumer purpose | Source authority | Freshness | Filters / scope | Must not decide |
|---|---|---|---|---|---|
| Waiting List Review View | Scan Admisi-owned Waiting List entries awaiting Ward review/placement | Admisi Waiting List identity + RNA inbox/review state | Immediate after committed inbound fact; reconciled on demand | Destination RNA, status, priority, age; patient scope | Waiting List lifecycle, responsibility transfer before assignment, or placement |
| Current Accommodation View | Show current Clinical Accommodation plus simultaneous retained/associated purposes | RNA allocations; master display references | Transactionally current after RNA commit | RegId/patient, RNA, active purpose | Which allocation may be released/transferred |
| Accommodation History View | Investigate assignment, transfer, release, correction, and source continuity | RNA allocation/history | Immediate | RegId/allocation/time; audit privilege | Correctness of a disputed record |
| Bed Availability View | Support bed search using Mandatory Bed Assignability facts | RNA Bed/allocations + Bed master | Immediate for RNA state; reference freshness identified | RNA/room/readiness/occupancy characteristics | Final assignability/capacity; commands must recheck under lock |
| Bed Recovery Work View | Track cleaning, block, maintenance, verification, and stale recovery work | RNA Bed/recovery history | Immediate | RNA/room/condition/assignee/age | Mark Ready or close recovery |
| RNA Service Work View | Scan pending/assigned/executed/cancelled or integration-blocked work | RNA executions + source/Tarif reference | Immediate after committed inbound/command | RNA, performer/team, status, priority, due time, patient | Assignment, execution, source cancellation, or Service Definition |
| RNA Service Execution Detail/History | Review Service reference, source/occurrence, Performer, Performed At, Recorded At, corrections, and delivery state | RNA execution/history | Immediate | Execution/order/occurrence/RegId; care/audit scope | Clinical Order, Service Definition, Charge Eligibility, or financial state |
| Execution Fact Delivery View | Reconcile execution/correction delivery and destination acknowledgements | RNA execution + outbox | Immediate | status, destination, age, source fact | Tata Rekening eligibility or financial consequence |
| Integration Recovery View | Recover duplicate, stale, failed, rejected, or conflicting facts/deliveries | RNA inbox/outbox/attempt ledger | Near-real-time | direction, collaborator, status, correlation, age | Repeat aggregate behavior or overwrite source truth |

Projection DALs may use optimized SQL without reconstructing aggregates. Query authorization and row scoping remain in Application. A projection may cache reference names for operational display, but stable IDs and the owning authority must remain visible; stale names cannot alter a command decision.

## 10. Application Interfaces and API Philosophy

RNA Application interfaces are transport-independent commands, queries, and authenticated integration contracts. MediatR is the existing in-process dispatch mechanism; it does not define business ownership.

- Commands express one accountable intent and return deterministic identity/outcome plus conflict information. They must distinguish validation failure, not found, forbidden, policy blocked, lifecycle conflict, concurrency conflict, duplicate replay, dependency unavailable, and unexpected failure.
- Queries return purpose-built, caller-scoped read models. List queries require stable ordering, bounded pagination, explicit filters, and a continuation/total strategy selected by the later API contract.
- Actor requests and service facts are separate contracts. Current actor commands use authenticated identity and coarse-grained application access; they must not trust a body-supplied `UserId` or role. Contextual authorization remains a Phase-99 enforcement concern, while the Domain and SOP artifacts continue to define the business rules.
- Retriable commands require a stable request identity scoped to caller and operation. A replay with the same semantic request returns the prior result; reuse with different semantics fails explicitly.
- Integration facts require source system, source-fact identity, authoritative entity/occurrence identity, source revision when relevant, occurred/business time, recorded time, accountable actor when distinct, and correlation identity.
- API versioning must preserve stable business identities and additive compatibility. Breaking semantic changes require a new contract version and an adapter/reconciliation strategy.
- HTTP controllers may expose RNA capabilities using Bilreg's authenticated REST and `JSend` conventions, but exact routes, request/response schemas, field validation messages, and host-specific view models belong to the later API contract.
- An actor-facing ward host may compose RNA queries with CPOE, NERS, and financial status. It must invoke each owning context's Application command for mutation and must not coordinate a distributed transaction from presentation code.

No controller, transport status, UI button, or projection row is business authority. Commands reload authoritative state and reevaluate authorization and invariants.

## 11. Integration and Cross-Context Collaboration

| Collaborator | Direction | Purpose | Owning authority | Contract style | Consistency / delivery | Failure handling |
|---|---|---|---|---|---|---|
| Admisi Rawat Inap | Contract-defined demand, decision, assignment, correction, release, acknowledgement, and reconciliation collaboration | Collaborate without sharing Waiting List or Accommodation ownership | Admisi owns Admission and Waiting List; RNA owns Accommodation Assignment and Release | [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) is the sole semantic contract; adapters remain transport-agnostic | Identities, revisions, local-commit boundaries, delivery, acknowledgement, and reconciliation follow the integration contract | Implement the contract's recovery behavior without direct persistence access or a distributed transaction |
| Registration / Patient Administration | Query inbound; transition fact inbound | Validate RegId, patient, encounter activity, care context, and discharge/transition facts | Registration/Patient owner | Synchronous read port; durable transition facts where required | Store identity and minimum decision snapshot; revalidate on material commands | Dependency outage blocks unsafe mutation; never infer discharge/release from stale projection |
| Bed/Room/Ward Master (`BedUsageContext`) | Query inbound | Resolve stable bed, room, ward, active-master, and governed reference facts | BedUsageContext/master-data owner | Direct in-process Application/reference port | Synchronous validation with bounded cache allowed for immutable/reference data | Cache cannot override inactive/missing authority; command fails safely when mandatory assignability facts are unavailable; an optional constraint is evaluated only when its policy and source facts are available |
| CPOE | Ordered work/change inbound; execution/correction association outbound | Coordinate Clinical Order occurrences and RNA execution recording | CPOE owns intent/lifecycle; RNA owns the Service Execution Fact | Approved hybrid topology: SQL-backed outbox/inbox durable push plus authoritative reconciliation queries; direct in-process adapter is transport optimization only | At-least-once; stable order, occurrence, revision, obligation, and source-fact identities; durable acknowledgement; per-order ordering where required | Duplicate ingestion returns prior result; stale revision conflicts visibly; failed/stale/conflicting delivery remains recoverable; in-process calls preserve identical durability semantics |
| Tarif Context | Active-Service eligibility query/inbound display snapshot | Resolve active Services allowed for the authoritative `Bangsal → Layanan` mapping when recording billable work | Tarif owns Service Definition, `AllowedLayanan`, configuration, and catalogue identity; RNA owns the Bangsal mapping and only the selected execution reference/fact | Synchronous reference port; versioned display projection/cache is optional | Stable `ServiceId`; minimum display snapshot and source version may be retained with the fact; save revalidates against Tarif | Tarif unavailable produces `ServiceDependencyUnavailable`; an empty eligible result produces `NoEligibleService`; billable recording is blocked in either case. RNA never creates a permissive local definition. |
| Legacy Accommodation Stay | Legacy Stay core read/write plus RNA extension/history | Run legacy and RNA in parallel without migration; compose one Accommodation model | Legacy owner owns existing Stay fields; RNA owns new extension/history fields | Compatibility repository/facade; legacy application boundary for legacy-owned writes; extension tables for RNA structure | Stable `LegacyStayId`; legacy row version/update token; RNA extension version; source and reconciliation IDs | Field ownership prevents dual writes; bypass writes are detected/reconciled; no fabricated historical facts; no bulk backfill |
| Legacy `OrderTdk` / retained `Tindakan` | Pre-cutover order history inbound/read-only; billable execution association outbound | Replace new `OrderTdk` with CPOE while retaining `Tindakan` as the billable-execution record | CPOE owns native order; RNA owns execution fact; Tindakan owner owns its record | Cutover-aware legacy adapter plus idempotent Tindakan application adapter | Explicit `CutoverAt`; stable legacy IDs; unique `ServiceExecutionFactId` linkage; historical source label | Quarantine ambiguous legacy identity; one billable fact creates at most one Tindakan; non-billable creates none; rollback never deletes committed facts |
| EMR / clinical documentation domains | Inter-ward clinical content and optional display/reference query | Own clinical content between wards and show separately owned documentation when useful | EMR/producing documentation domain | Authenticated lookup or owning EMR workflow | RNA does not create a cross-RNA responsibility contract or copy documentation | EMR content availability does not move Waiting List or accommodation ownership into RNA |
| Patient Social Data | Mother-Baby relationship query | Verify the Baby Medical Record references the Mother's Medical Record before Rooming-In | Patient Social Data owner | Synchronous read port | Store stable relationship evidence/reference with RNA association; keep registrations separate | Missing or mismatched relationship blocks Rooming-In activation; RNA does not create or repair social data |
| Practitioner / Identity / Clinical Governance | Query inbound | Resolve actor identity, RNA membership, privilege, and Ad Hoc authority | Identity/workforce/governance owners | Future contextual authorization ports (Phase-99) | Decision snapshot/version recorded with material action when enforcement is implemented | Current phase assumes baseline authentication/coarse application access; Domain/SOP policy remains authoritative |
| Housekeeping / Maintenance | Recovery evidence/reference inbound; optional work delivery outbound | Supply authenticated evidence that cleaning, inspection, maintenance, or restriction resolution was performed | Operational actor owns its workflow and performed-work evidence; RNA owns the recorded Bed Readiness transaction and latest-state projection | Direct local port or authenticated durable fact | Stable recovery work/fact IDs; duplicates ignored | Bed remains unavailable until approved evidence and verifier authority exist |
| Tindakan / Tata Rekening | Billable Service Execution Fact/correction outbound; acknowledgement inbound | Create/resolve one Tindakan per billable execution and preserve lifecycle/correction handling | RNA user supplies billable classification; RNA owns execution fact; Tindakan owner owns linked record; Tata Rekening owns all subsequent financial lifecycle | Approved hybrid durable outbox/inbox plus outcome query; direct local call only behind an outbox-aware port | At-least-once; unique `ServiceExecutionFactId`; acknowledgement may return `TindakanId` | Non-billable creates none; finalized/paid correction becomes `ReconciliationRequired`; RNA repositories never write Tindakan/billing/journal tables |

Inside the monolith, direct Application orchestration is preferred for immediate validation and response. Mandatory cross-context effects that can fail after RNA commits use an outbox and acknowledgement; no distributed atomicity is claimed. A generic event bus is not required. Domain facts may be translated into explicit integration messages only after the authoritative local transaction commits.

## 12. Authentication, Authorization, and Audit

**Phase boundary (ARCH-020).** The current implementation assumes only baseline authentication and coarse-grained application access. Fine-grained contextual authorization—including Ward scope, professional competency, assignment, correction, exception, and service-principal policy—is intentionally out of scope for the current phase and deferred to Phase-99. The Domain and SOP artifacts remain authoritative for these business rules; enforcement through contextual authorization is deferred and must not alter those semantics.

**Authentication.** Existing HTTP callers use JWT Bearer and `ICurrentUserContext`. Service integrations require a dedicated authenticated service principal and allowed source-system identity. A service principal is not automatically the accountable nurse, midwife, performer, receiver, verifier, or corrector carried in a fact.

**Target capability families:**

- `RNA.Accommodation.Read`
- `RNA.Accommodation.Receive`
- `RNA.Accommodation.Assign`
- `RNA.Accommodation.Transfer`
- `RNA.Accommodation.Release`
- `RNA.Accommodation.Correct`
- `RNA.BedReadiness.Update`
- `RNA.BedReadiness.Verify`
- `RNA.ServiceWork.Read`
- `RNA.ServiceExecution.Assign`
- `RNA.ServiceExecution.Perform`
- `RNA.ServiceExecution.Correct`
- `RNA.ExecutionFact.Deliver`
- `RNA.Audit.Read`
- `RNA.Integration.Write`
- `RNA.Integration.Recovery`

A capability grants access to a use-case family, not unconditional authority. Application authorization must additionally evaluate, as applicable: patient/encounter relationship; RNA membership; current responsibility; assignment; actor privilege; Tarif Service identity availability; exception/approval policy; Ward Nurse/Head Nurse authority for retained accommodation and Rooming-In; verified Mother-Baby relationship; correction threshold; and Housekeeping/Maintenance source scope. No RNA capability grants BOR or Charge Eligibility authority.

Command authorization and query scoping are separate. Worklists must filter to authorized RNAs, assignments, care contexts, and minimum necessary patient data. Audit and integration-recovery access must not grant broad clinical-content access.

**Audit.** Every material command records authenticated actor/service identity, accountable clinical actor when distinct, action, aggregate/fact identity, business time, recording time, reason, policy/definition version, request/correlation identity, and pre-change snapshot or original-fact reference where applicable. Aggregate-owned transition/correction history is business truth; shared `AuditLog` is cross-cutting investigation evidence. Both commit with state and mandatory outbox work.

**Business Time Standard.** `OccurredAt` is the authoritative UTC instant at which a business fact happened; `RecordedAt` is the UTC persistence instant used only for audit and technical tracing. All Accommodation, Bed Readiness, Service Execution, Correction, and Integration Facts are ordered by `OccurredAt`. When the actual business time is unknown, the producer sets `OccurredAt = RecordedAt`. A source offset or local business date may be retained as context metadata but never changes ordering.

Wrong-patient correction, Entered in Error, emergency/ad hoc authority, late entry, inter-ward release completion, and readiness verification require especially explicit reason and actor evidence. Logs and errors must avoid leaking sensitive clinical content or credentials.

## 13. Transactions, Consistency, Concurrency, and Idempotency

| Material flow | Local transaction and synchronous invariants | Eventual facts | Duplicate key / retry | Concurrency and recovery |
|---|---|---|---|---|
| Admisi Waiting List receipt/rejection | As defined by `ADMISI-RNA-INTEGRATION.md` | As defined by `ADMISI-RNA-INTEGRATION.md` | As defined by `ADMISI-RNA-INTEGRATION.md` | RNA applies the contract without taking Waiting List ownership |
| Accommodation assignment | Lock registration clinical-location guard and target Bed row; validate active master, intended Ward, Ready state, conflicting active allocations, and capacity/occupancy; insert allocation; bump bed epoch; audit/outbox | Contract-defined RNA-to-Admisi assignment fact | Actor request ID; cross-context identity/revision semantics follow `ADMISI-RNA-INTEGRATION.md` | Version/lock conflict returns retryable conflict; command reloads and reevaluates, never blindly retries a stale decision |
| Internal transfer | Lock registration, source, and target beds in stable ID order; activate target and release/reclassify source atomically | Bed-recovery work for released source if separately consumed | Request ID; source allocation + transfer intent identity | Failure rolls back both allocations; target unavailability leaves source clinical allocation unchanged |
| Inter-ward Release to Waiting List | Release source allocation, append post-use readiness state, audit, and the contract-defined outbound obligation atomically | Effects and reconciliation follow `ADMISI-RNA-INTEGRATION.md`; EMR owns clinical content | Contract-defined release identity and revision semantics | RNA applies the contract without a transfer queue, destination ownership, or cross-context atomicity |
| Release/readiness | Release and immediate post-use non-Ready readiness transaction commit together; later readiness transactions are separate and each refresh the latest-state projection | Recovery evidence/references | Allocation/request ID; recovery source-fact ID | Ready requires current version, no incompatible active allocation, and all blockers resolved |
| Ordered work receipt/change | Inbox + work create/change + audit/ack commit | Acceptance and later execution fact back to CPOE | Source + order + occurrence + instruction revision + fact ID | Stale/out-of-order changes conflict visibly; an existing execution fact is not overwritten by later cancellation |
| Ordered execution recording | One aggregate version protects the work/fact; billable eligible-Service or non-billable description + Performer + Performed At, audit, CPOE outbox, and billable-only Tata Rekening outbox commit together | CPOE and Tata Rekening delivery when applicable | Request ID; one active execution fact per source occurrence/actual execution | Stale performer action fails; handler reloads and requires human reassessment |
| Ad hoc/independent execution | Billable eligible Tarif Service or non-billable description, Performer, Performed At, authority evidence, audit, optional CPOE obligation, and billable-only Tata Rekening obligation commit | Subsequent-authorization coordination and fact delivery | Actor request ID and encounter/service-or-description/performed-time duplicate screen; confirmed duplicate links to original | Possible duplicate is not silently merged; disputed records remain visible for correction review |
| Correction / Entered in Error | Original history plus correction/replacement link, audit, and all required correction outboxes commit | CPOE and Tata Rekening correction fact | Correction request ID and monotonically versioned execution-fact revision | Partial external delivery is repaired per destination; original record is never deleted |
| Execution fact delivery | No RNA business transition occurs in worker; committed fact is delivered unchanged | Tata Rekening independently evaluates eligibility/financial consequences | Execution fact ID + revision + destination ledger | Destination rejection does not roll back execution; RNA corrects only its operational fact or retries the same delivery |
| Delivery worker | Claim one delivery atomically; external call occurs outside aggregate transaction; persist acknowledgement/attempt separately | Destination state | Outbox ID + destination; stable payload fact identity reused | Lease/claim timeout enables stale recovery; exponential backoff/dead-letter policy is configuration, not Domain behavior |

No flow claims distributed atomicity. Compensation means an accountable corrective fact, explicit source disposition, or retry/reconciliation—not deleting committed clinical or accommodation history.

## 14. Infrastructure and Operational Concerns

- RNA SQL and migrations belong under `Bilreg.SqlDb/RnaContext` and must be independently deployable in dependency order. Migration must not repurpose legacy tables without an approved reconciliation and rollback decision.
- SQL-backed inbox/outbox/delivery stores are sufficient initially. No broker, distributed cache, event store, or new platform is required.
- A scheduled worker processes outbound delivery, stale claims, policy-approved retained-accommodation review reminders, and integration reconciliation. Workers call Application use cases and contain no accommodation, clinical, Service Definition, or eligibility rules.
- Taksaka may later host scheduling behind the same Application ports; that hosting choice must not change RNA ownership or aggregate behavior.
- Cache is permitted only for governed, versioned reference data such as Tarif Service identity/display data. Owning reference authority remains canonical. Occupancy, readiness, work, execution facts, and delivery state must not rely on a non-authoritative cache.
- Structured logs include correlation/request ID, aggregate/source fact ID, RNA/destination, transition, attempt number, outcome category, duration, and concurrency result. Patient names, free text, and clinical document content must not be placed in routine logs.
- Metrics should cover Waiting List review age, placement conflicts, occupancy/readiness contradictions, inter-ward release-notification age, bed recovery age, RNA service work age, execution-recording latency, late entry, Accommodation Correction rate/rejection by finalization, authorization overdue, execution-fact delivery failure/retry age, and stale claims.
- Health signals distinguish database connectivity, worker liveness, backlog age, and each external collaborator. A collaborator outage may degrade integration while local reads remain healthy.
- Feature flags may gate inbound Waiting List visibility, accommodation write authority, ordered execution, legacy adapter coexistence, and financial delivery. Flags cannot bypass Domain invariants or reinterpret existing records.
- Operational recovery requires authorized projections and commands; direct database repair is not an ordinary workflow. Any exceptional repair must be reconciled into aggregate/audit history.

## 15. Implementation Guidance for AI Agents

An increment is an end-to-end capability through Clean Architecture layers, not a declaration of Vertical Slice Architecture. A policy-gated increment must stop until its listed gap is approved.

| Increment | Included use cases | Required layers | External dependencies | Verification gate |
|---|---|---|---|---|
| I-RNA-01 — Foundations and read authority | UC-RNA-005, 018, shared identity/time/auth ports, aggregate skeletons, versioning, audit conventions | Domain, Application, Infrastructure; API only after contract | Bed master, Registration read ports | Layer dependency test; deterministic reconstruction; scoped query tests; no legacy write |
| I-RNA-02 — Admisi Waiting List processing and initial placement | UC-RNA-001–004 | All layers + inbox/outbox | [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) | Contract acceptance tests, inbound replay, parallel bed assignment, atomic placement/audit/outbound obligation, acknowledgement, and reconciliation |
| I-RNA-03 — Release and readiness | UC-RNA-014, 016–018 | Domain through API; worker/adapter as needed | Bed Readiness transaction contract and authenticated evidence/reference integration as applicable | Discharge-not-release, release-not-Ready, blocker resolution, verifier authorization, stale fact recovery |
| I-RNA-04 — Internal occupancy variants | UC-RNA-006–010 | Domain through API | Approved retention/rooming-in policies | Multi-allocation policy, two-registration rooming-in, transfer atomicity/deadlock, historical continuity; no optional-policy rejection |
| I-RNA-05 — Inter-Ward Release-to-Waiting-List | UC-RNA-011–013 | Domain, Application, Infrastructure, Admisi integration adapter | GAP-RNA-005 CLOSED; [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) | Contract acceptance tests, atomic release/outbound obligation, duplicate/failed delivery recovery, and reconciliation |
| I-RNA-06 — Accommodation correction | UC-RNA-015 | All layers | Tata Rekening finalization-status and downstream correction contracts | Original preserved; Head Nurse/same-Ward/pre-`FINALIZED` enforcement; correction fact publication; downstream reconciliation |
| I-RNA-07 — Ordered RNA execution | UC-RNA-019–025, 030–031 | All layers + delivery worker | Tarif active-Service eligibility dependency; CPOE or approved legacy contract; Tata Rekening inbound fact contract | Source/occurrence dedupe; billable eligible-Service or non-billable description + Performer + Performed At; save-time Tarif revalidation; actual/recorded time; CPOE/Tata delivery and retry; no Service Definition or financial-eligibility logic |
| I-RNA-08 — Ad hoc authority | UC-RNA-026–027 | All layers | Tarif Service-reference dependency; GAP-RNA-010 CLOSED; CPOE exceptional-action contract implementation | Truthful source, emergency/late chronology, prohibited missing authority, 24-hour-or-discharge deadline, acknowledgement/escalation, overdue and late-review fact association, Tata delivery |
| I-RNA-09 — Execution correction | UC-RNA-028 | All layers | GAP-RNA-011 CLOSED; CPOE and Tata Rekening correction contract implementation | Non-destructive correction, wrong patient, replacement, independent material review, ordered delivery of corrected facts |
| I-RNA-10 — Billable execution delivery | UC-RNA-031 | All layers + outbox/worker | GAP-RNA-012 and ARCH-017 CLOSED (DESIGN) | Billable-only payload, unique Tindakan by fact ID, acknowledgement/TindakanId, correction revision, finalized/LUNAS reconciliation, no direct table writes |
| I-RNA-11 — Recovery and hardening | UC-RNA-032–033 across all integrations | Application, Infrastructure, support transport | All active collaborators | Claim/lease tests, replay/conflict, backlog/health/metrics, security scoping, end-to-end partial-failure recovery |

For every increment, implement and run:

- Domain tests for each referenced BR-RNA invariant and lifecycle transition;
- Application tests for orchestration, authorization, transaction completion/rollback, idempotent replay, concurrency conflict, and audit/outbox creation;
- Infrastructure tests for DTO round-trip, aggregate reconstruction, SQL version checks, indexes/uniqueness, transaction participation, and adapter translation;
- contract/integration tests at every neighboring context boundary, including duplicate, out-of-order, unavailable, and corrected facts;
- API tests only after a versioned transport contract exists; and
- regression tests proving current Admisi, Ward master, Registration, CPOE/Lab, Tindakan, and Tata Rekening behavior is unchanged until an approved migration deliberately changes it.

Implementation prohibitions:

- Do not mutate Admisi, CPOE, NERS, Tindakan, Tata Rekening, or journal tables from RNA repositories. Do not mutate Ward master except the explicitly designated current-readiness projection through its owning port as part of the approved readiness transaction flow.
- Do not place state transitions, capacity rules, or exceptional authority in controllers, handlers, DALs, workers, or projections; do not place eligibility rules anywhere in RNA.
- Do not treat `BedType.IsAktif`, an empty bed field, Waiting List closure, discharge authorization, legacy `Implemented/Done/Executed`, bill existence, or UI state as RNA completion/readiness truth.
- Do not collapse simultaneous allocations into one current-bed field or overwrite transfer/correction history.
- Do not merge mother/baby registrations for Rooming-In.
- Do not create direct ward-to-ward transfer queues, destination ownership before successful Bed Assignment, or RNA-to-RNA responsibility contracts. Inter-ward movement is Release-to-Waiting-List; EMR owns clinical content.
- Do not calculate BOR. Do not implement the documented legacy one-room-charge Rooming-In assumption as RNA billing behavior.
- Do not physically delete allocations, execution facts, authority evidence, or corrections.
- Do not create a generic event bus, shared mega-repository, or distributed transaction to hide explicit boundaries.
- Do not fabricate unresolved policy, Service Definitions, performer/quantity/documentation/completion/outcome rules, roles, SLA, or external contracts.

## 16. Architectural Decisions

### ADR-RNA-001 — One `RnaContext` Owns the Target Capability

- **Decision:** Implement both Accommodation Management and RNA Service Execution under `RnaContext` across existing Clean Architecture projects.
- **Status:** Accepted Target.
- **Context:** The business defines one RUANG RANAP Operational Management boundary, while existing `AkomodasiContext` folders are empty and too narrow for service execution.
- **Rationale:** One explicit boundary preserves the shared RNA responsibility while capability folders keep aggregates independent.
- **Consequences:** Empty accommodation scaffolding must be removed or left unused during implementation; no split ownership may arise from folder history.
- **Rejected alternatives:** Put all behavior in `BedUsageContext`; split execution into `ChargeContext`; treat empty folders as implemented architecture.
- **Evidence or governing references:** `docs/contexts/bangsal/RNA-DOMAIN.md` §§1, 3, 6; empty `AkomodasiContext`; `docs/ENGINEERING.md`.

### ADR-RNA-002 — Bed Readiness History Is RNA-Owned; Bed State Is Its Projection

- **Decision:** Reference `BedUsageContext/WardFeature` master identities while RNA owns append-only Bed Readiness transactions. Bed exposes the current operational state as the latest-transaction projection for fast operational queries.
- **Status:** Accepted Target.
- **Context:** Existing `BedType` contains identity, room/ward reference, and `IsAktif`; the domain requires operational readiness history and policy-aware availability while operations need a fast current-state lookup.
- **Rationale:** Active master status, readiness history, occupancy, and reporting are distinct facts and authorities. A mutable current state alone cannot provide accountable readiness history.
- **Consequences:** Commands resolve master facts through a port, append an RNA readiness transaction with business time, responsible actor/verifier, status, optional reason, and optional evidence, and atomically refresh the designated Bed-master current-state projection. Transaction history remains the authoritative audit trail. Availability projections compose master activity, projected readiness, and occupancy authorities.
- **Rejected alternatives:** Change a Bed-master readiness flag without a transaction; treat `IsAktif` as Ready; make RNA own Housekeeping or Maintenance workflows.
- **Evidence or governing references:** BR-RNA-012, 015, 023–025; `BedModel.cs`; `ta_bed.sql`.

### ADR-RNA-003 — Allocation History Is Append-Oriented and Purpose-Explicit

- **Decision:** Use one aggregate per Accommodation Allocation; transfer creates/activates the destination and releases or reclassifies the source without overwriting history.
- **Status:** Accepted Target.
- **Context:** Multiple simultaneous purposes, active retained accommodation, and Mother-and-Baby Rooming-In invalidate a single current-bed record.
- **Rationale:** Independent allocation identity preserves occupancy, capacity, and correction meaning while exposing truthful accommodation facts to downstream owners.
- **Consequences:** Current Clinical Accommodation is a constrained designation across active allocations; Retained Accommodation stays Active until release and is released at discharge; Mother and Baby retain separate registrations and histories with one Primary plus one Associated Occupant and no capacity increase.
- **Rejected alternatives:** One mutable bed field per registration; merge Rooming-In registrations; auto-release all prior allocations.
- **Evidence or governing references:** BR-RNA-001–014, 017; SOP-RNA-A02–A05.

### ADR-RNA-004 — Cross-Allocation Invariants Serialize Per Bed and Registration

- **Decision:** Combine aggregate versioning with a per-bed operational lock/epoch and registration clinical-location guard inside explicit local transactions.
- **Status:** Accepted Target.
- **Context:** Capacity and unique clinical location span independent allocations and can race.
- **Rationale:** Aggregate independence must not permit double assignment or two clinical locations.
- **Consequences:** Infrastructure implements deterministic lock order and compare-and-swap; commands recheck policy under lock. Projections remain advisory.
- **Rejected alternatives:** UI-only reservation; eventually repair double occupancy; make one Bed aggregate contain all allocation history.
- **Evidence or governing references:** BR-RNA-002, 004, 008, 012, 015; existing `IUnitOfWork`/`TransHelperUnitOfWork`.

### ADR-RNA-005 — Direct Local Collaboration, Durable Boundary Delivery

- **Decision:** Use direct Application ports for local synchronous checks and SQL-backed inbox/outbox delivery for retriable cross-context obligations.
- **Status:** Accepted Target.
- **Context:** Bilreg prefers direct orchestration, while acknowledgement, correction, and external failures require durable recovery.
- **Rationale:** This preserves explicit ownership without a generic event platform or distributed transaction.
- **Consequences:** Delivery is at least once; consumers are idempotent; recovery is visible; aggregate transactions never include network calls.
- **Rejected alternatives:** Fire-and-forget notifications; database sharing as integration; mandatory broker/event sourcing.
- **Evidence or governing references:** `docs/ENGINEERING.md` §§12–14; `docs/concepts/operational-events.md`; [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) for Admisi semantics; Lab Oware queue evidence.

### ADR-RNA-006 — RNA Records Execution Against a Tarif Service Reference

- **Decision:** RNA records actual billable execution against an eligible existing Tarif Service identity resolved through `Bangsal → Layanan → Tarif.AllowedLayanan`; non-billable execution carries description instead. Tarif owns Service Definition and CPOE owns intent, occurrence coordination, and order lifecycle.
- **Status:** Accepted Target.
- **Context:** Ordered service work crosses intent and fulfilment authorities.
- **Rationale:** A truthful execution fact must arise from Performer + Performed At and either an eligible billable Service or a non-billable description, not order status or receipt, while Service rules stay with their owner.
- **Consequences:** Stable Tarif Service-reference and CPOE order/occurrence/revision contracts are required; Generic Fulfilment and RNA cannot publish duplicate execution facts.
- **Rejected alternatives:** Define an RNA service catalogue; copy Clinical Order state into RNA; infer execution from legacy status.
- **Evidence or governing references:** BR-RNA-026–044; SOP-RNA-S01; CPOE ADR-006/007.

### ADR-RNA-007 — Tata Rekening Owns Charge Eligibility

- **Decision:** Keep Charge Eligibility completely outside RNA. RNA delivers only the unchanged Service Execution Fact and versioned corrections to Tata Rekening.
- **Status:** Accepted Target — closes the design dependency for `GAP-RNA-012` and `GAP-RNA-ARCH-017`; implementation remains pending.
- **Context:** Existing Tindakan creation couples a recorded action directly to billing and accounting.
- **Rationale:** RNA owns operational truth; Tata Rekening is the sole authority that evaluates eligibility and every billing consequence using Tarif and other financial policies.
- **Consequences:** RNA has no eligibility state, policy, role, use case, or UI. It never loads tariff/coverage or writes bills/journals. For an execution explicitly recorded as billable under the approved dispatch contract, an owning application adapter idempotently creates the retained `Tindakan` record; this does not make RNA the billing authority. Corrections publish a new execution-fact revision.
- **Rejected alternatives:** Evaluate eligibility in RNA; send eligible/not-eligible flags; write `Tindakan`, bills, or journals directly from an RNA repository; treat `Tindakan` as RNA execution-history authority.
- **Evidence or governing references:** BR-RNA-054–059; SOP-RNA-S04; Tarif context; Tata Rekening context.

### ADR-RNA-008 — Asymmetric OrderTdk/Tindakan Cutover

- **Decision:** At a facility/unit `CutoverAt`, native CPOE replaces new `OrderTdk`, while historical `OrderTdk` remains read-only. `Tindakan` remains active as the idempotent billable-execution record linked one-to-one from a billable `ServiceExecutionFactId`; non-billable execution creates no `Tindakan`.
- **Status:** Accepted Target.
- **Context:** `OrderTdk` can be retired for new order intent, but existing financial operations continue to require `Tindakan` as the billable execution record.
- **Rationale:** The asymmetric boundary introduces native order/execution truth without breaking retained `Tindakan` processing or duplicating historical and financial records.
- **Consequences:** Source labels and identities are preserved; one billable fact creates at most one `Tindakan`; ambiguity is quarantined; rollback stops native intake without deleting committed facts; historical views span both sources.
- **Rejected alternatives:** Replace `Tindakan`; dual-create native and legacy orders after cutover; recreate committed native facts during rollback; use `Tindakan` as RNA's authoritative execution history.
- **Evidence or governing references:** BR-RNA-060–063c; CPOE-RNA-OD-008; `OrderTdkModel.cs`; `TindakanModel.cs`.

### ADR-RNA-013 — Legacy Accommodation Stay Coexists with RNA Extensions

- **Decision:** Run legacy Stay persistence and RNA in parallel. Legacy Stay remains the core authority for existing fields; RNA extension tables hold only new structure and append-only RNA history, keyed by `LegacyStayId`.
- **Status:** Accepted Target.
- **Context:** A migration or cutover would require inventing or transforming legacy history and is not required for the RNA capability.
- **Rationale:** A compatibility repository can compose the legacy core and RNA extensions while preserving existing legacy workflows and enabling new RNA facts.
- **Consequences:** Field ownership is explicit; RNA uses the legacy application boundary for legacy-owned writes; row-version/update-token checks and reconciliation handle bypass writes; no bulk backfill or fabricated history is allowed.
- **Rejected alternatives:** Copy all legacy stays into new RNA tables; dual-write the same field from both contexts; infer missing historical business times or transitions.
- **Evidence or governing references:** BR-RNA-063d–063f; GAP-RNA-ARCH-018; legacy Stay persistence contract.

### ADR-RNA-009 — Operational Work Uses Projections; Writes Use Aggregates

- **Decision:** Provide dedicated demand, accommodation, bed, service execution, delivery, and recovery projections, while every transition reloads authoritative aggregates.
- **Status:** Accepted Target.
- **Context:** Ward operations require high-volume scanning across many independent aggregates.
- **Rationale:** Projection queries provide operational performance without becoming a second authority.
- **Consequences:** Read DALs may be optimized independently; stale projection results never authorize a mutation.
- **Rejected alternatives:** Reconstruct every aggregate for lists; update lifecycle through projection tables.
- **Evidence or governing references:** `docs/ENGINEERING.md` §8; RNA capabilities 3.1, 3.6, 3.11.

### ADR-RNA-010 — Optimistic Aggregate Concurrency and Explicit Audit Are Mandatory

- **Decision:** Version every RNA aggregate and commit aggregate history, shared audit, request/idempotency outcome, and mandatory outbox records in the same local transaction.
- **Status:** Accepted Target.
- **Context:** placement, transfers, performer actions, corrections, and integration facts can race and are clinically/accountably material.
- **Rationale:** Stale writes must not silently erase newer facts, and audit must correspond to committed state.
- **Consequences:** Concurrency failures are explicit; correction history is append-oriented; background retry never repeats aggregate behavior.
- **Rejected alternatives:** last-write-wins; audit middleware only; physical delete on correction.
- **Evidence or governing references:** BR-RNA-045–049; `AuditLog.cs`; `IUnitOfWork.cs`; `docs/DATABASE.md` §§12–16.

### ADR-RNA-011 — Inter-Ward Movement Uses Release-to-Waiting-List

- **Decision:** RNA never performs a direct ward-to-ward transfer. RNA owns the source Accommodation Release and Admisi owns the resulting Waiting List work, as defined only by [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md).
- **Status:** Accepted Target.
- **Context:** The approved operating model returns responsibility to Admission after Release and assigns inter-ward clinical content to EMR.
- **Rationale:** Reusing the Admission Waiting List preserves bounded-context ownership and avoids a second transfer queue or RNA-to-RNA responsibility protocol.
- **Consequences:** RNA has no transfer queue, destination ownership, or post-Release transfer responsibility. Delivery failure and reconciliation follow the integration contract and never recreate source accommodation responsibility.
- **Rejected alternatives:** Direct destination ownership before Bed Assignment; shared transfer aggregate; destination bed reservation before Admisi assignment; RNA-owned clinical-content workflow.
- **Evidence or governing references:** BR-RNA-016a, 018–019b; SOP-RNA-A05; [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md); EMR boundary.

### ADR-RNA-012 — Contextual Authorization Is Deferred to Phase-99

- **Decision:** Assume only baseline authentication and coarse-grained application access in the current implementation. Defer fine-grained contextual authorization, authorization providers, permission/policy models, Ward scope, professional competency, correction, exception, and service-principal enforcement to Phase-99.
- **Status:** CLOSED (Deferred to Phase-99).
- **Context:** The Domain and SOP artifacts define the required business responsibilities and rules, but the current implementation is early-stage and does not provide the contextual authorization infrastructure.
- **Rationale:** Reducing implementation complexity allows the team to validate business workflows and integration boundaries before adding a broad policy-enforcement dependency.
- **Consequences:** Current authentication does not prove contextual authority. Architecture and UI descriptions may retain the business/contract rules, but must not claim that fine-grained enforcement exists in this phase. Phase-99 must add enforcement without changing Domain/SOP semantics.
- **Evidence or governing references:** ARCH-020; `docs/contexts/bangsal/RNA-DOMAIN.md`; `docs/contexts/bangsal/rna-sop/RNA-SOP-INDEX.md`.

## 17. Open Gaps and Deferred Decisions

| ID | Gap or decision | Why it matters | Owner / authority needed | Blocks | Safe interim position |
|---|---|---|---|---|---|
| GAP-RNA-001 — CLOSED | Admission → RNA Waiting List Processing | Ownership and state effects are defined by `ADMISI-RNA-INTEGRATION.md`. | Approved business decision | None in RNA business model; approved contract implementation remains separate work | Implement UC-RNA-001–004 according to the integration contract. |
| GAP-RNA-002 — CLOSED | Optional Placement Constraints | RNA enforces only Mandatory Bed Assignability: active existing bed, Ready, no conflicting active allocation, capacity available, and correct destination Ward. | Approved business decision | None | Never reject Bed Assignment for gender, isolation, equipment, or hospital-specific operational policy; Ward personnel decide those operational considerations manually. |
| GAP-RNA-003 — CLOSED | Retained Accommodation | Existing allocation remains Active while treatment occurs elsewhere; continues occupying capacity and producing accommodation facts; Ward Nurse/Head Nurse only; all retained allocations release at discharge | Approved business decision | None | Implement UC-RNA-006/007 exactly; RNA publishes facts and makes no billing decision |
| GAP-RNA-004 — CLOSED | Mother-and-Baby Rooming-In | Only Mother-Baby; Patient Social Data relation; every bed; one Primary + one Baby; no capacity increase; separate registrations/histories; Ward Nurse/Head Nurse only | Approved business decision + Patient Social Data owner | None | Implement UC-RNA-008/009; no BOR logic; retain one-room-charge legacy assumption as documentation only pending confirmation |
| GAP-RNA-005 — CLOSED | Inter-Ward Release-to-Waiting-List | Ownership and cross-context effects are defined by `ADMISI-RNA-INTEGRATION.md`; EMR owns clinical content | Approved business decision + Admisi/EMR owners | None in RNA business model; approved contract implementation remains separate work | Implement UC-RNA-011–013 according to the integration contract, without a direct destination workflow or RNA transfer queue |
| GAP-RNA-006 — CLOSED | Accommodation Correction | Only Head Nurse may correct any Accommodation Fact owned by the same Ward. Correction is append-only and allowed only before Tata Rekening is `FINALIZED`; RNA publishes the correction and each downstream context reconciles its own data. | Approved business decision | None; finalization-status and downstream publication are integration contracts | Implement UC-RNA-015 with original fact immutable, no delete, same-Ward check, `FINALIZED` rejection, correction fact audit, and publication. |
| GAP-RNA-007 — CLOSED | Transactional Bed Readiness model | Bed readiness requires accountable, non-destructive operational history while Bed must retain a fast current-state lookup | RNA owns readiness transaction history; Bed master exposes the current-state projection; Housekeeping/Maintenance own their own workflows | None | Append one authenticated readiness transaction per transition with business time, responsible actor, status, optional reason/evidence; Ready requires explicit authorized verification; project the latest valid transaction to Bed |
| GAP-RNA-008 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY | Tarif Service eligibility contract | Each Bangsal has one authoritative `LayananId`; Tarif owns active Service and `AllowedLayanan`. RNA queries eligible Services for that Layanan and never owns Service Definition. | Tarif architecture owner + RNA architecture owner | UC-RNA-019, 024, 026 | Require an eligible `ServiceId` only for billable execution; allow non-billable execution with description only; never create a local permissive Service Definition. |
| GAP-RNA-009 — CLOSED FOR RNA | Outcome catalogue and completion rules | RNA no longer models partial/aborted/not-performed outcomes or completion criteria | Tarif or explicitly named owning context | None in RNA | Keep such classifications outside RNA; unexecuted work creates no Service Execution Fact |
| GAP-RNA-010 — CLOSED | Subsequent authorization timeframe, authorizer, escalation, acknowledgement, and overdue handling | Authorization is due within 24 hours of `OccurredAt` or before discharge, whichever is earlier. The accountable authorizer is the attending/clinically responsible physician, falling back to the designated on-call physician. Escalation proceeds to on-call/service lead and then Clinical Governance; task acknowledgement is distinct from authorization. CPOE sets terminal `Authorization Overdue` after the deadline and records later review as late review. | CPOE + Clinical Governance | No policy block; UC-RNA-027 and CPOE follow-up implementation remain integration work | RNA publishes truthful `ExceptionalExecutionRecorded` chronology and authority basis; it never self-authorizes, erases execution, or treats overdue/late review as timely prospective authorization. |
| GAP-RNA-011 — CLOSED | Execution correction authority, second review, dispute handling, evidence | RNA owns append-only correction of Service Execution Facts. Authorized care-team actors may report errors; the owning Ward's Head Nurse may finalize ordinary corrections except self-approval. Patient/Registration/Service/source-order identity changes, replacement, Entered in Error, disputed, and other material corrections require an independent Clinical Governance-authorized second reviewer. | Clinical Governance + RNA Operations | UC-RNA-028 | Require structured reason and complete audit identity for every correction; require evidence for material corrections; preserve the immutable original; publish correction facts to CPOE and Tata Rekening for independent consequence handling |
| GAP-RNA-012 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY | Billable Service Execution Fact → Tindakan contract | Approved `RNA-TATA-REKENING-INTEGRATION.md` closes the ARCH-017 design dependency: RNA user classifies billable/non-billable; billable facts idempotently create one linked Tindakan, non-billable creates none; correction/lifecycle and acknowledgement semantics are approved | Tindakan/Tata Rekening + RNA architecture owners | UC-RNA-031 implementation only | Implement the approved durable/idempotent contract: persist and retry billable fact only; expose Tindakan linkage; never direct-write Tindakan/billing/journal tables or reopen/reverse settled history |
| GAP-RNA-013 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY | Companion Accommodation as flagged Bed Assignment | Companion uses the ordinary Bed Assignment approval, Mandatory Bed Assignability, assignment/release lifecycle, and audit, with `IsCompanionBed = true`. It occupies a bed and is associated with the inpatient registration, but is not a patient or Clinical Accommodation. | Approved business decision | Bed Assignment extension, persistence flag, outbound reporting flag, UI, and tests | Implement as an extension of ordinary Bed Assignment; publish the flag for downstream reporting such as RL to exclude companion beds from patient counts; do not create a separate approval workflow or companion patient registration. |
| GAP-RNA-014 — CLOSED (NOT NEEDED) | Temporary Absence | RNA has no need to distinguish a temporary physical absence from an active accommodation. | Approved scope decision | None | Do not model, store, display, or action Temporary Absence; it cannot affect accommodation, occupancy, bed readiness, reporting, or billing facts. |
| GAP-RNA-ARCH-015 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY | Admisi ↔ RNA integration contract | Approved `ADMISI-RNA-INTEGRATION.md` defines hybrid delivery, reviewable statuses/legacy migration, free-text rejection, release-to-Waiting-List behavior, correction reconciliation, optional destination Ward, and v1 rollout | Admisi and RNA architecture owners + Operations | No design block; adapters, persistence, workers, recovery views, migration, and tests remain | Implement only the approved contract; never direct-write the other context's persistence or infer placement from legacy `Accepted` |
| GAP-RNA-ARCH-016 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY | CPOE ↔ RNA v1 semantic contract and cutover | Approved `CPOE-RNA-INTEGRATION.md` settles dispatch, coordination, conflict handling, exceptional accountability, correction, hybrid delivery, asymmetric `OrderTdk`/`Tindakan` cutover, and v1 rollout. OD-009 fine-grained authorization is explicitly closed as deferred to Phase-99 under ARCH-020. | CPOE architecture owner + RNA service owner + Operations | No design block; native models, persistence, inbox/outbox, workers, adapters, acknowledgements, queries, recovery tooling, migration, and acceptance tests remain | Implement approved v1 semantics without claiming contextual authorization or implementation completion; define compatibility policy only before a future breaking version |
| GAP-RNA-ARCH-017 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY | RNA billable execution ↔ Tindakan/Tata Rekening contract | Approved `RNA-TATA-REKENING-INTEGRATION.md` defines billable classification, one-Tindakan-per-fact idempotency, non-billable exclusion, correction/finalization handling, hybrid delivery, acknowledgements, reconciliation, and v1 rollout | Tindakan/Tata Rekening + RNA architecture owners | No design block; consumer, persistence linkage/index, workers, queries, recovery UI, and tests remain | Implement only the approved contract; never direct-write foreign persistence or auto-reopen/reverse settled financial history |
| GAP-RNA-ARCH-018 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY | Legacy Accommodation Stay coexistence and RNA persistence extension | Legacy and RNA run in parallel. Legacy Stay remains the authoritative core; RNA adds extension/history tables keyed by `LegacyStayId`. No migration, backfill, or cutover is required. | Data owner + RNA architecture owner + legacy Stay owner | Compatibility repository, field-ownership matrix, concurrency token, extension schema, reconciliation, and tests | Implement lazy composition and same-transaction legacy-boundary writes; detect bypass changes; never fabricate history or dual-write the same field |
| GAP-RNA-ARCH-019 — CLOSED (DESIGN) / IMPLEMENTATION DEPENDENCY | Tarif active-Service availability and display snapshot contract | Tarif is the sole Service authority. RNA resolves active Services through `Bangsal → Layanan → Tarif.AllowedLayanan`, retains only a stable selected reference and minimum display snapshot, and revalidates at save. | Tarif architecture owner | I-RNA-07–10 | Expose `ServiceDependencyUnavailable` when Tarif cannot answer and `NoEligibleService` when it returns no active Service; block only billable save, allow non-billable description-only recording, and do not persist copied Service rules. |
| GAP-RNA-ARCH-020 | Contextual authorization provider and service-principal mapping | Current `ICurrentUserContext` exposes only actor ID; fine-grained contextual enforcement is intentionally deferred during early workflow and boundary validation | Identity/Authorization owner + clinical governance | Phase-99 authorization implementation | **CLOSED (Deferred to Phase-99).** Current implementation assumes baseline authentication and coarse-grained application access only; Domain/SOP rules remain authoritative and enforcement is deferred |

Implementation agents must not resolve these gaps by convention. The named owner must approve the business or integration decision, after which this architecture, the affected SOP, and the later API/persistence contracts must be updated together.

### Admisi Integration Implementation Follow-up (Not Design Gaps)

The following work remains before the approved contract is implemented. It is implementation-only and must preserve [`ADMISI-RNA-INTEGRATION.md`](ADMISI-RNA-INTEGRATION.md) without adding business rules:

- create Application-boundary ports and transport adapters for the approved contracts;
- implement RNA accommodation persistence, inbox/outbox or equivalent durable delivery, acknowledgement handling, and reconciliation views;
- replace the current no-op `WardAccommodationGateway` behavior with an Admisi-side contract consumer and its local Waiting List effects;
- provide authenticated service-principal and Ward-scope enforcement in Phase-99 without changing the Domain/SOP business semantics; and
- implement the contract's acceptance tests for replay, stale/conflicting delivery, receiver outage, correction ordering, and reconciliation.
