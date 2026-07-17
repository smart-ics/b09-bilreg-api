# CPOE Architecture

This document is the canonical technical specification for realizing the business defined in [`docs/contexts/cpoe/CPOE-DOMAIN.md`](CPOE-DOMAIN.md). It owns CPOE software structure, application boundaries, interfaces, integrations, security, infrastructure, and significant architectural decisions. Business meaning remains owned by the domain document. The target transport-agnostic collaboration contract with RNA is [`docs/contexts/bangsal/CPOE-RNA-INTEGRATION.md`](../bangsal/CPOE-RNA-INTEGRATION.md); unresolved decisions in that contract remain implementation blockers and are not resolved here.

## 1. Architecture Overview

CPOE is a UI-agnostic clinical coordination capability in the Bilreg modular monolith. It owns Clinical Order behavior, application use cases, lifecycle decisions, shared coordination state, operational projections, and integration contracts. It does not own a standalone primary operational user interface.

Actor-facing features compose CPOE capabilities into their own interaction context:

- SOAP composes Clinical Order authoring with clinical documentation.
- Ward Operational System may compose receiver coordination, outstanding-order views, and CPOE Generic Fulfilment.
- Laboratory Workflow and other specialized departmental features compose their own fulfilment workflow with CPOE coordination contracts.
- Diagnostic or development tooling may call the same application interfaces, but it is not the intended operational UX.

The host feature owns interaction composition, presentation state, and cross-capability read composition. CPOE remains the authority for every command that creates or changes a Clinical Order, evaluates a CPOE lifecycle transition, or changes shared CPOE coordination state. A host must not reproduce those decisions in its UI or application layer.

CPOE follows the repository's Clean Architecture, Pragmatic Layer Isolation, Tactical DDD, explicit persistence, and command/query conventions. The dependency direction is:

```text
Bilreg.Api             -> Bilreg.Application -> Bilreg.Domain
Bilreg.Infrastructure  -> Bilreg.Application -> Bilreg.Domain
```

The target organization is:

- `src/bilreg/Bilreg.Domain/CpoeContext` contains Aggregate Roots, child entities, value types, lifecycle behavior, and domain-level policies without transport or persistence dependencies.
- `src/bilreg/Bilreg.Application/CpoeContext` contains MediatR commands and queries, handlers, repository contracts, projection contracts, contextual authorization ports, integration contracts, and explicit transaction orchestration.
- `src/bilreg/Bilreg.Infrastructure/CpoeContext` contains repositories, deterministic DTO mapping, explicit SQL DALs, cross-context and legacy adapters, and durable delivery mechanisms.
- `src/bilreg/Bilreg.Api` contains optional HTTP transport adapters for CPOE application and integration contracts. A controller is not a user-facing feature and does not imply a CPOE screen.
- `src/bilreg/Bilreg.Api` remains the composition root for dependency registration, authentication, and runtime configuration.

The repository does not yet contain a `CpoeContext` implementation. The CPOE types and modules named in this document are target boundaries; MediatR, explicit transactions, aggregate repositories, projection DALs, JWT authentication, audit persistence, SQL Server, and adapter-based integration are established Bilreg mechanisms.

There are two distinct invocation paths:

```text
Authenticated actor
  -> actor-facing host composition
  -> CPOE command or query
  -> CPOE application and domain authority

Authenticated source service
  -> CPOE integration contract
  -> source validation and idempotent fact ingestion
  -> CPOE coordination state and projections
```

Actor-initiated commands carry authenticated actor context and are authorized against the patient, care context, Order Type, Destination, and requested action. Service-to-service contracts carry a service identity, source-system identity, stable source-fact identity, and separately represented accountable clinical actor where applicable. The two paths must not impersonate one another.

Application handlers define transaction boundaries. Domain models protect aggregate consistency. Repositories reconstruct and persist Aggregate Roots. DALs execute explicit SQL. Operational queries use purpose-built projections where aggregate reconstruction is unnecessary.

Direct application orchestration is the default inside the monolith. MediatR notifications may represent local post-decision collaboration where useful, but the domain events named in `docs/contexts/cpoe/CPOE-DOMAIN.md` do not imply event sourcing or a generic event bus. Delivery across a process or system boundary uses explicit contracts and durable delivery state.

## 2. Module Boundaries

| Module | Responsibility | Dependencies |
|---|---|---|
| Order Definition | Maintains versioned Order Definitions and Order Set definitions used to validate and snapshot new orders. | Clinical governance, organization, professional-authority, and Destination reference ports. |
| Clinical Order | Owns authoring, authorization, dispatch, lifecycle, occurrences, change history, exceptional authority, current responsibility, and shared fulfilment associations for one order. | Order Definition, patient and care-context references, professional authority, and Destination references. |
| Receiver Coordination | Applies acceptance, rejection, clarification, and shared receiver-coordination decisions to Clinical Orders and exposes receiver projections. | Clinical Order and contextual Destination authorization. |
| Fulfilment Coordination | Ingests authoritative progress, occurrence, outcome, result, documentation, and Charge Eligibility facts without taking ownership of specialized execution. | Clinical Order and executing-domain integration contracts. |
| Generic Fulfilment | Owns execution evidence only for an ordered activity explicitly configured with no specialized Executing Domain. | Clinical Order reference, Order Definition, performer, place, and Generic Fulfilment authorization. |
| Outstanding-Order Reconciliation | Owns encounter-level reconciliation and coordinates responsibility reassessment during care transitions and discharge. | Clinical Order projections, care-context integration, and Clinical Order commands. |
| Operational Projections | Provides receiver work, outstanding orders, due occurrences, overdue authorization, reconciliation candidates, history, and integration recovery views. | CPOE persistence; it does not mutate Aggregate Roots or decide lifecycle transitions. |
| CPOE Integration Contracts | Defines inbound and outbound contracts for host features, care contexts, executing domains, result/documentation authorities, Tata Rekening, and legacy systems. | Application ports implemented by Infrastructure adapters. External transport and persistence models do not enter the Domain. |

Receiver Coordination and Fulfilment Coordination are application boundaries over the Clinical Order Aggregate; they do not create additional Aggregate Roots.

The following ownership rules apply across modules and neighboring features:

- An actor-facing host owns UX composition and may coordinate several application capabilities, but it invokes CPOE commands for all Clinical Order mutations. Hosting an action does not transfer the use case or lifecycle authority to the host.
- A specialized Executing Domain owns its detailed fulfilment workflow, execution record, correction policy, and the Charge Eligibility fact arising from that fulfilment. It sends common facts to CPOE through integration contracts.
- CPOE Generic Fulfilment is used only when the Order Definition explicitly identifies that no specialized Executing Domain exists. A host such as Ward Operational System may present the interaction, but the Generic Fulfilment command and Aggregate Root remain CPOE-owned.
- The authority that owns execution establishes Charge Eligibility. Tata Rekening consumes that authoritative fact and independently decides tariff, coverage, bundling, bill creation, adjustment, and payment. CPOE may associate or relay the fact without becoming its authority.
- Modules do not use another bounded context's DAL or tables as their application contract. Local collaboration uses application ports; legacy database access, when unavoidable, is isolated behind an Infrastructure anti-corruption adapter.

## 3. Use Cases

The tables below define CPOE application interactions, not screens or endpoint inventory. An actor-facing host may invoke and compose them, but their authority remains with CPOE.

### Governance and definition

| Use case | Purpose | Primary aggregate or model | Primary outcome | Initiator |
|---|---|---|---|---|
| Maintain Order Definition | Create or revise governed configuration for an Order Type. | Order Definition model | A new immutable definition version is available for governance review. | Authorized governance actor through a governance host. |
| Activate Order Definition | Select the definition version allowed for new orders. | Order Definition model | One governed version becomes active without changing existing orders. | Authorized governance actor. |
| Maintain Order Set Definition | Maintain a governed collection of Order Definition references. | Order Set definition model | A versioned Order Set is available to authoring hosts. | Authorized governance actor. |

### Clinical Order authoring and lifecycle

| Use case | Purpose | Primary aggregate or model | Primary outcome | Initiator |
|---|---|---|---|---|
| Draft Clinical Order | Capture one prospective instruction against a definition version. | Clinical Order | A non-actionable Clinical Order is created. | Authorized actor through SOAP or another clinical host. |
| Draft Orders from Order Set | Create independently governed orders from selected set items. | Clinical Order collection | One Clinical Order is created per item with independent identity and lifecycle. | Authorized actor through a clinical host. |
| Authorize Clinical Order | Apply accountable authorization after contextual authority validation. | Clinical Order | Authorization is recorded and the order becomes eligible for dispatch. | Authorized clinical actor through a host. |
| Dispatch Clinical Order | Hand an authorized instruction to its resolved Destination. | Clinical Order | Dispatch is recorded and one idempotent fulfilment obligation is prepared. | CPOE application orchestration following authorization or an explicit authorized action. |
| Amend Clinical Order | Replace the current authorized instruction while preserving prior versions. | Clinical Order | A new accountable revision becomes current and affected delivery is prepared. | Authorized clinical actor through a host. |
| Cancel Clinical Order | Terminate an eligible order before fulfilment begins. | Clinical Order | Cancellation is recorded and the Destination is notified. | Authorized clinical actor through a host. |
| Discontinue Clinical Order | End future or remaining activity for an active order. | Clinical Order | Remaining activity is terminated while valid completed outcomes remain associated. | Authorized clinical actor through a host. |
| Mark Clinical Order Entered in Error | Correct an invalid instruction without deleting its history. | Clinical Order | The order leaves active work while remaining traceable. | Specifically authorized clinical actor through a host. |
| Record Exceptional Action | Capture verbal, emergency, protocol-based, or retrospective authority and actual chronology. | Clinical Order | The exceptional instruction or action and its accountability requirement are recorded. | Authorized actor through the relevant clinical host. |
| Record Subsequent Authorization | Confirm accountable authorization for an exceptional record. | Clinical Order | The subsequent-authorization obligation is resolved or remains explicitly outstanding. | Authorized clinical actor through a host. |
| Evaluate Clinical Order Completion | Apply the configured Completion Criterion after a relevant fact or command. | Clinical Order | CPOE updates fulfilment and closure state without transferring authority for specialized execution. | CPOE application orchestration. |

### Receiver and fulfilment coordination

| Use case | Purpose | Primary aggregate or model | Primary outcome | Initiator |
|---|---|---|---|---|
| Accept Clinical Order | Record Destination commitment through an authorized Receiver. | Clinical Order | Acceptance and responsible Receiver are recorded. | Receiver through a Destination-facing host, or an authorized executing-domain contract. |
| Reject Clinical Order | Record an accountable Destination refusal. | Clinical Order | Rejection, reason, and responsible Receiver are recorded. | Receiver through a Destination-facing host, or an authorized executing-domain contract. |
| Request Clarification | Open a structured question affecting fulfilment. | Clinical Order | A clarification and its responsibility are recorded. | Authorized Receiver or responsible actor through a host. |
| Respond to Clarification | Record the responsible party's answer. | Clinical Order | A response is appended to the open clarification. | Authorized responder through the host appropriate to their care context. |
| Resolve Clarification | Record the resulting Destination disposition. | Clinical Order | The clarification closes with an explicit coordination outcome. | Authorized Receiver through a Destination-facing host. |
| Record Fulfilment Coordination Fact | Apply common scheduling, verification, assignment, preparation, or start information. | Clinical Order | Shared coordination state is updated without treating the fact as proof of completion. | Authorized Executing Domain service. |
| Record Occurrence Outcome | Apply the authoritative outcome of one scheduled or recurring occurrence. | Clinical Order | The occurrence association and CPOE completion assessment are updated. | Authoritative Executing Domain or CPOE Generic Fulfilment. |
| Record Fulfilment Summary | Apply common outcome facts and authoritative fulfilment references. | Clinical Order | CPOE stores the summary and reevaluates outstanding coordination responsibility. | Authoritative Executing Domain or CPOE Generic Fulfilment. |
| Associate Result | Link an authoritative result without copying its clinical content. | Clinical Order | A Result Reference is attached idempotently. | Authoritative result domain. |
| Associate Execution Documentation | Link authoritative execution documentation. | Clinical Order | An Execution Documentation Reference is attached idempotently. | Authoritative documentation or Executing Domain. |
| Record Charge Eligibility Association | Associate an authoritative eligibility fact with its order or occurrence. | Clinical Order | CPOE records source authority and source-fact identity without making a financial decision. | Authoritative Executing Domain or CPOE Generic Fulfilment. |
| Record Generic Fulfilment | Record authoritative execution for an Order Definition routed to Generic Fulfilment. | Generic Fulfilment | CPOE-owned execution evidence is persisted and summarized to the Clinical Order. | Authorized performer through an actor-facing host. |
| Establish Generic Fulfilment Charge Eligibility | Establish eligibility from CPOE-owned Generic Fulfilment when its fulfilment policy permits it. | Generic Fulfilment | An authoritative, uniquely identified eligibility fact is available to CPOE and Tata Rekening. | CPOE application orchestration from Generic Fulfilment outcome. |

### Responsibility and reconciliation

| Use case | Purpose | Primary aggregate or model | Primary outcome | Initiator |
|---|---|---|---|---|
| Transfer Order Responsibility | Reassign current responsibility after a care-context or responsible-clinician change. | Clinical Order | Current responsibility changes without changing original authorship. | CPOE in response to an authenticated care-transition fact or authorized actor action. |
| Reassess Outstanding Orders | Evaluate orders affected by a transfer or responsibility change. | Clinical Order collection | Each affected order receives an explicit CPOE disposition. | CPOE application orchestration. |
| Start Discharge Reconciliation | Capture the encounter's outstanding-order candidates. | Discharge Reconciliation | A reconciliation is created with independently assessable items. | Discharge host through a CPOE command, or an authenticated encounter-completion contract. |
| Record Reconciliation Disposition | Record one item's disposition, acknowledgement, responsibility, or escalation. | Discharge Reconciliation | The item receives an accountable disposition and linked order outcome. | Authorized discharge actor through its host. |
| Complete Discharge Reconciliation | Validate the reconciliation's accountable item outcomes. | Discharge Reconciliation | Reconciliation closes and its summary becomes available to the care-context owner. | Authorized discharge actor through its host. |

### Queries

| Use case | Purpose | Primary aggregate or model | Primary outcome | Initiator |
|---|---|---|---|---|
| Get Clinical Order | Retrieve current order detail permitted for the caller's context. | Clinical Order read model | A read-only order representation is returned. | Any authorized host or source service with scoped need. |
| Get Receiver Work | Retrieve orders requiring Destination action. | Receiver work projection | A filtered Destination-scoped work view is returned. | Ward or departmental host. |
| Get Outstanding Orders | Retrieve unresolved coordination responsibility by patient, encounter, care context, or responsible role. | Outstanding-order projection | A current outstanding-order view is returned. | SOAP, Ward, discharge, or another authorized host. |
| Get Due Occurrences | Retrieve scheduled or recurring work requiring attention. | Due-occurrence projection | A filtered occurrence view is returned. | Authorized host or executing-domain integration. |
| Get Overdue Authorization Work | Retrieve unresolved exceptional-accountability obligations. | Overdue-authorization projection | Actionable overdue items are returned for authorized follow-up. | Governance or clinical-responsibility host. |
| Get Reconciliation Candidates | Retrieve outstanding orders relevant to a care transition or discharge. | Reconciliation-candidate projection | An encounter-scoped candidate view is returned. | Authorized care-transition or discharge host. |
| Get Clinical Order History | Retrieve lifecycle decisions, revisions, responsibility, fulfilment associations, and audit references. | Clinical Order history projection | A chronologically ordered, read-only history is returned. | Authorized clinical or audit host. |
| Get Integration Recovery Work | Retrieve pending, failed, or conflicting integration obligations. | Integration-recovery projection | An operational recovery view is returned. | Authorized support tooling or worker. |

Business-capability coverage is explicit:

| Domain capability | Realizing use cases |
|---|---|
| Clinical Order Definition | Maintain Order Definition; Activate Order Definition; Maintain Order Set Definition. |
| Order Authoring and Authorization | Draft Clinical Order; Draft Orders from Order Set; Authorize Clinical Order. |
| Order Routing | Dispatch Clinical Order. |
| Receiver Work Management | Get Receiver Work; Get Outstanding Orders; Get Due Occurrences. |
| Acceptance, Rejection, and Clarification | Accept Clinical Order; Reject Clinical Order; Request, Respond to, and Resolve Clarification. |
| Fulfilment Coordination | Record Fulfilment Coordination Fact; Record Occurrence Outcome; Record Fulfilment Summary; Evaluate Clinical Order Completion. |
| Generic Fulfilment | Record Generic Fulfilment; Establish Generic Fulfilment Charge Eligibility. |
| Order Change Control | Amend, Cancel, Discontinue, and Mark Clinical Order Entered in Error. |
| Exceptional Order Governance | Record Exceptional Action; Record Subsequent Authorization; Get Overdue Authorization Work. |
| Result and Documentation Association | Associate Result; Associate Execution Documentation. |
| Billing Eligibility Handover | Record Charge Eligibility Association; Establish Generic Fulfilment Charge Eligibility; authoritative fulfilment integration with Tata Rekening. |
| Outstanding-Order Reconciliation | Transfer Order Responsibility; Reassess Outstanding Orders; Start, Record, and Complete Discharge Reconciliation. |
| Clinical Order Audit | Get Clinical Order History plus explicit audit persistence on every material command. |

Commands operate on the smallest valid consistency boundary. Batch and host-composed interactions coordinate independent commands and report per-order outcomes; they do not merge multiple Clinical Orders into one lifecycle or transfer decision authority to the host.

## 4. Aggregate Realization

### Clinical Order

`ClinicalOrderModel` is the Aggregate Root for one clinical instruction.

It owns software representations of authorization, current responsibility, Destination and receiver decisions, clarifications, occurrences, amendments, exceptional authority, Fulfilment Summary, authoritative references, and Charge Eligibility associations. Child records change only through intention-revealing root behavior.

The root stores the Order Definition identity and version used at authoring, plus the instruction facts required for deterministic validation and completion assessment. Later definition changes cannot reinterpret an existing order. Current state supports operational work while material decisions and instruction revisions remain append-oriented history.

A Charge Eligibility association records the authoritative source, stable source-fact identity, applicable order or occurrence, and current correction/supersession relationship. The Clinical Order does not calculate eligibility for a specialized Executing Domain and does not calculate any financial consequence.

One root represents one order. An Order Set use case creates multiple `ClinicalOrderModel` instances and retains batch correlation, but each order is loaded, authorized, dispatched, changed, fulfilled, and closed independently.

### Generic Fulfilment

`GenericFulfilmentModel` is the Aggregate Root for authoritative CPOE-owned execution evidence when the selected Order Definition has no specialized Executing Domain.

It owns assigned and actual performer, execution timing and place, structured outcome, deviations, not-performed reason, supporting-document references, and any Generic Fulfilment-owned Charge Eligibility decision. It identifies the associated Clinical Order and occurrence but is not a child of `ClinicalOrderModel`.

The application coordinates Generic Fulfilment persistence with the corresponding Clinical Order summary. Where both records share the Bilreg database and one decision changes both, they commit in one explicit application transaction. Introducing a specialized Executing Domain changes routing for new orders; it does not rewrite historical Generic Fulfilment records.

### Discharge Reconciliation

`DischargeReconciliationModel` is the Aggregate Root for one encounter-level reconciliation.

It owns references to the outstanding Clinical Orders captured at start, assessed dispositions, acknowledgements, transferred responsibility, unresolved exceptions, and escalation references. It does not own or reconstruct the referenced Clinical Order Aggregates.

Application orchestration applies required Clinical Order commands separately and records their resulting identities and versions on reconciliation items. Reconciliation completion validates the reconciliation's own consistency without creating one large aggregate from all encounter orders.

### Supporting configuration, references, and concurrency

Order Definitions and Order Set definitions are versioned governed configuration models separate from transactional Aggregate Roots. A Clinical Order references an immutable definition version and snapshots only the facts needed for stable validation, routing, and completion assessment.

Patient, care context, practitioner, organization, Destination, result, documentation, fulfilment, and financial identities are lightweight references. CPOE stores only identity and the operational snapshot required for traceability; it does not reproduce the authoritative external aggregate.

All transactional Aggregate Roots use optimistic concurrency with an expected aggregate version. A stale actor command or integration fact fails with an explicit conflict and must be reevaluated against current state. Idempotent replay of an already-applied source fact returns its recorded outcome rather than creating a new transition.

## 5. Repository Strategy

One repository is defined for each transactional Aggregate Root:

- `IClinicalOrderRepo`
- `IGenericFulfilmentRepo`
- `IDischargeReconciliationRepo`

Order Definition and Order Set configuration use explicit versioned catalog repositories. Repositories are application persistence gateways: they reconstruct complete aggregates, persist root and child state atomically, and expose no SQL or persistence DTOs to the Application or Domain layers.

Infrastructure uses explicit SQL, dedicated DALs, and deterministic DTO-to-model mapping. Aggregate-owned histories are append-oriented; current operational state may be updated as a projection of that history. Transactional CPOE records are voided or superseded according to their lifecycle and are not physically deleted.

Application transaction boundaries include the changed local aggregate, its explicit compliance audit row, and any durable outbound obligation required by the committed decision. A transaction may include more than one local CPOE Aggregate Root only when immediate consistency is required. Remote systems never participate in the local SQL transaction.

Operational reads use dedicated projection contracts implemented by Infrastructure query DALs rather than aggregate repositories. Projection families are:

- Receiver work by Destination, status, priority, and requested timing.
- Outstanding orders by patient, encounter, care context, and responsible role.
- Occurrences due or in fulfilment.
- Exceptional authorizations approaching or beyond their governed deadline.
- Discharge-reconciliation candidates and unresolved exceptions.
- Clinical Order chronological history.
- Pending, failed, or conflicting integration deliveries.

Projection SQL uses stable explicit query shapes and indexes aligned with operational filtering. A host may combine these read models with its own feature data, but it must not persist a competing CPOE lifecycle projection as an authority.

CPOE owns explicit inbox, outbox, and idempotency persistence for contracts that require reliable delivery. The repository currently provides feature-specific precedents rather than a shared generic framework, so CPOE must not assume that a generic event store or delivery service exists. Required guarantees are:

- A unique source-system and source-fact identity for inbound facts.
- A unique obligation identity for each order, occurrence, instruction revision, transition, result/document association, and eligibility handover that can be retried.
- Atomic persistence of an aggregate decision and its outbound obligation.
- Recorded processing outcome for replay and support investigation.
- At-least-once delivery with idempotent consumer behavior, not a claim of exactly-once transport.

CPOE tables follow the established `{MODULE}_{ENTITY}` convention using `BILRG_Cpoe...` names, application-generated identifiers, standard audit columns, void lifecycle, explicit indexes, and logical relationships without database foreign-key constraints.

## 6. API Philosophy

CPOE's primary interface is its application command/query surface. HTTP is a transport adapter, not the definition of the capability and not evidence of a standalone CPOE product UI.

The interface is divided into three contract families:

1. **Actor application contracts** — commands and queries invoked by SOAP, Ward, discharge, governance, or another actor-facing host. When the host is in the same Bilreg process, it invokes MediatR/application contracts directly. A separate host may use authenticated HTTP without changing CPOE ownership.
2. **Service integration contracts** — authenticated, idempotent contracts for dispatch obligations, executing-domain facts, care transitions, result/document associations, and Charge Eligibility. These contracts identify the source service and source fact separately from any accountable clinical actor.
3. **Support contracts** — restricted queries and retry actions for audit, diagnostics, and integration recovery. They are not an operational CPOE UX.

Commands express business actions rather than table CRUD. Queries return aggregate detail or purpose-built projections. Child entities are addressed only through their Aggregate Root's command surface. CPOE does not prescribe a generic order-management screen, generic form, or generic CRUD controller.

An actor-facing host may expose transport routes under its own feature namespace when it owns the interaction composition. Its adapter maps the interaction to a CPOE command and does not bypass the CPOE application layer. CPOE may also expose reusable transport endpoints where several hosts or separate applications need the same use case. Route placement does not change use-case ownership.

Bilreg controllers conventionally use MediatR, JSON, action-oriented routes, and the `JSendOk` response envelope. CPOE follows those conventions while avoiding an invented `/api/v1/cpoe` namespace until repository-wide API versioning is established. Breaking integration-contract changes require an explicit contract version; compatible additions retain the current version.

Actor commands derive actor identity from authenticated request context. A caller-supplied user identifier is not authorization evidence. Retriable actor commands accept an idempotency/request identity where duplicate business action is unsafe. Integration contracts always carry stable source-system, source-fact, occurrence, and correlation identities as applicable.

Bulk order-set and reconciliation interactions return per-item outcomes when independent Aggregate Roots are involved. Partial success is explicit and is not presented as an all-or-nothing aggregate result.

## 7. Integration

All integrations are defined as Application ports and implemented by Infrastructure adapters. Adapters translate external terminology into CPOE contracts and prevent foreign transport or persistence models from entering the Domain.

| Integration | Purpose | Communication style | Ownership | Synchronization strategy |
|---|---|---|---|---|
| Actor-Facing Host Features | Compose CPOE capabilities into SOAP, Ward, discharge, governance, or other actor workflows. | In-process MediatR/application call in Bilreg; authenticated HTTP when deployed separately. | Host owns interaction composition and host data; CPOE owns Clinical Order commands, lifecycle decisions, Generic Fulfilment commands, and CPOE projections. | Synchronous for actor feedback. Host retries use a stable request identity and re-query the authoritative outcome. |
| Patient and Care Context | Validate patient, encounter, location, admission status, and current care context. | Synchronous query behind an application port; authenticated transition fact for later changes. | Patient, Registration, Admisi, or Ward context owns its record and transition. | Validate current facts when required, store identity plus necessary snapshot, and process transitions idempotently. |
| Practitioner, Organization, and Clinical Privilege | Resolve authors, authorizers, Receivers, performers, roles, organization membership, and applicable authority. | Synchronous contextual policy query behind an application port. | Identity/workforce and clinical-governance authorities own identity, privilege, and membership. | Reevaluate current authority for accountable actor commands and persist the decision context required for audit. |
| Specialized Executing Domains | Receive one fulfilment obligation and provide authoritative acceptance, progress, occurrence, outcome, correction, and eligibility facts. | Direct in-process application contract for local domains; authenticated durable integration contract across a process or system boundary. | Each Executing Domain owns its specialized workflow, execution record, corrections, and Charge Eligibility arising from that execution. CPOE owns shared order coordination. | Dispatch once per stable obligation identity; ingest facts idempotently by source, order, occurrence, instruction revision, and source-fact identity. Preserve ordering where facts affect the same obligation. |
| CPOE Generic Fulfilment | Execute ordered activity when no specialized authority exists. | Internal CPOE application orchestration invoked from an authorized host. | CPOE owns the Generic Fulfilment Aggregate, its execution evidence, and any resulting eligibility fact. | Commit fulfilment, order summary, audit, and outbound eligibility obligation in an explicit local transaction where required. |
| Result and Execution Documentation Domains | Associate released authoritative artifacts with the originating order. | Authenticated notification/callback or reconciliation query. | The producing clinical domain owns content, validation, correction, and release state. | Store stable references and limited availability metadata; apply repeated and corrective notifications idempotently. |
| Tata Rekening | Consume Charge Eligibility and determine its financial consequence. | Durable integration contract from the authoritative fulfilment owner; direct local orchestration is allowed only when authority and transaction boundaries remain explicit. | Specialized Executing Domain is authority for its eligibility facts; CPOE is authority only for Generic Fulfilment eligibility; Tata Rekening owns every financial decision. | Publish at least once using a unique eligibility identity. Tata Rekening deduplicates by authoritative source and fact identity. CPOE may relay an unchanged source fact but must not re-author it. |
| Care Transition and Discharge | Trigger responsibility reassessment and outstanding-order reconciliation. | In-process command for local contexts or authenticated durable notification across systems. | Care-context owner owns the transition; CPOE owns order reassessment and reconciliation. | Correlate by encounter and transition identity, process idempotently, and return/query CPOE reconciliation status without sharing repositories. |
| Legacy Departmental Systems | Associate CPOE intent with an existing departmental order and fulfilment workflow. | Anti-corruption adapter over available REST, service call, or controlled database-bound legacy gateway. | CPOE owns native prospective intent; the legacy department remains authoritative for its departmental fulfilment record. | Persist native-to-legacy mapping and stable request identity before retry. Reconcile outcomes without representing legacy-originated work as prospectively authorized CPOE intent. |

For specialized fulfilment, the preferred eligibility path is:

```text
Specialized Executing Domain
  -> Charge Eligibility fact -> Tata Rekening
  -> same authoritative fact -> CPOE association
```

For Generic Fulfilment, the path is:

```text
CPOE Generic Fulfilment
  -> Charge Eligibility fact -> Tata Rekening
  -> CPOE Clinical Order association
```

An adapter may physically relay a specialized eligibility message when topology requires it, but the envelope retains the original authority and source-fact identity. CPOE does not infer Charge Eligibility from order creation, dispatch, acceptance, preparation, or an incomplete fulfilment summary.

Inside the monolith, direct orchestration is preferred where one user interaction needs an immediate response. Cross-system obligations are delivered after commit through durable CPOE outbox state. Inbound service facts use an inbox/idempotency ledger. Failed delivery remains visible through an integration-recovery projection and is retried without repeating aggregate behavior.

The Domain Events catalog in `docs/contexts/cpoe/CPOE-DOMAIN.md` names business facts. An application handler may use a fact for direct local orchestration or translate a committed fact into an integration message, but domain-event naming does not mandate asynchronous publication, a broker, or event sourcing.

Legacy database access is permitted only inside an Infrastructure anti-corruption adapter when the legacy system exposes no safer contract. The adapter owns schema translation, locking/transaction behavior, identity mapping, and idempotency. CPOE application and domain code never issue legacy SQL or treat a legacy table as a CPOE repository.

## 8. Authentication & Authorization

Bilreg authenticates HTTP callers with the configured JWT Bearer mechanism and creates an application `ICurrentUserContext`. The Domain remains independent of JWT, claims, and ASP.NET types.

Interactive calls use the authenticated human subject. A host may hide or disable unavailable actions for usability, but CPOE performs authoritative authorization again when executing its command. CPOE commands do not trust a body-supplied `UserId`, role, Destination, practitioner identity, or privilege assertion.

Service-to-service contracts use a dedicated authenticated service identity and source-system permission. The service principal identifies the integration caller; it is not automatically the authorizer, Receiver, performer, or other accountable clinical actor. Where a source contract carries a clinical actor, CPOE validates that actor and the source's right to attest the fact.

Authorization combines stable capability permissions with contextual policy checks. The target capability families are:

- `CPOE.Order.Author`
- `CPOE.Order.Authorize`
- `CPOE.Order.Receive`
- `CPOE.GenericFulfilment.Perform`
- `CPOE.Order.Reconcile`
- `CPOE.Order.Govern`
- `CPOE.Order.Read`
- `CPOE.Audit.Read`
- `CPOE.Integration.Fulfilment.Write`
- `CPOE.Integration.Recovery`

A capability grants access to a use-case family, not unconditional authority over every order. Application handlers additionally evaluate, as applicable:

- Professional authority and clinical privilege for the Order Type.
- Patient and care-context scope.
- Current responsible clinician or care-team relationship.
- Destination membership and Receiver responsibility.
- Performer competency and assignment for Generic Fulfilment.
- Source-system authority for the order, Destination, executing route, and fact type.
- Encounter and Destination scope for operational projections and history.

Specialized performers are authorized by their Executing Domain for specialized execution. CPOE authorizes only ingestion of that domain's signed or authenticated facts. The `CPOE.GenericFulfilment.Perform` capability never grants permission to bypass an available specialized Executing Domain.

The existing Bilreg authorization baseline validates JWTs and commonly applies `[Authorize]`, while `ICurrentUserContext` currently exposes only an actor identifier and no CPOE contextual privilege provider exists. CPOE therefore requires application ports for capability, professional-authority, care-context, and Destination checks; the mapping to identity-provider claims and hospital privilege sources must be configured outside the Domain.

Every material command writes explicit audit information with authenticated actor or service identity, accountable clinical actor where distinct, business time, correlation identity, reason when required, and a pre-change snapshot where applicable. Aggregate change, audit row, and mandatory outbound obligation share one local transaction. Authorization failures are logged without exposing clinical content.

## 9. Infrastructure

CPOE depends on established Bilreg infrastructure:

- .NET 8 projects with MediatR command/query dispatch and dependency registration from the Bilreg API composition root.
- SQL Server as durable operational storage for Aggregate Roots, immutable decision history, configuration versions, projections, inbox/outbox records, and idempotency ledgers.
- Explicit SQL access, DALs, and deterministic DTO mapping using the repository's existing data-access conventions; no ORM change tracking or database-resident business workflow.
- Explicit application transaction scopes through the configured transaction/unit-of-work mechanism.
- ASP.NET Core JSON HTTP hosting and the Bilreg response-envelope/error conventions for transport adapters.
- JWT Bearer authentication plus contextual identity, privilege, practitioner, organization, and care-context providers.
- Append-only `BILRG_AuditLog` persistence created explicitly by CPOE use cases.
- Serilog-based structured application and integration logging with correlation across HTTP calls, audit entries, source facts, and delivery attempts.
- Scheduled/background execution for durable delivery retries, overdue-authorization detection, and reconciliation repair. Workers call CPOE application ports and contain no clinical rules.
- Optional read-through caching for active Order Definition versions. SQL Server remains authoritative and immutable version identity prevents stale configuration from reinterpreting existing orders.

CPOE requires no standalone frontend project. Its operational projections and commands are consumed by actor-facing hosts.

CPOE transaction tables use `BILRG_Cpoe...` names and follow the SQL standards for application-generated identity, PascalCase columns, standard `Crt`/`Upd`/`Vod` audit columns, void lifecycle, explicit indexing, and logical rather than database-enforced foreign relationships.

No message broker, distributed cache, object storage, search engine, or event store is required for the initial architecture. A SQL-backed CPOE inbox/outbox is sufficient. A broker or the Taksaka worker platform may later host transport and scheduling behind existing application ports without changing Aggregate Root behavior or integration ownership.

The current Lab Oware queue and Admisi Ranap request ledger demonstrate feature-local durable retry and idempotent replay patterns, but they are not a reusable CPOE delivery platform. CPOE delivery tables and workers must make claim/lock behavior, retry policy, stale-processing recovery, deduplication, and support visibility explicit.

## 10. Architectural Decisions (ADRs)

### ADR-001 — CPOE Is UI-Agnostic

**Decision:** Implement CPOE as a reusable clinical coordination capability with no standalone primary operational UI.

**Rationale:** Order authoring, receiver work, fulfilment, and reconciliation occur in actor-specific clinical contexts whose interaction composition belongs to SOAP, Ward, departmental, discharge, or governance features.

**Consequence:** CPOE exposes application commands, queries, projections, and integration contracts. Those interfaces must not be converted into a generic CPOE screen or CRUD product merely because they are transport-accessible.

### ADR-002 — Host Features Own Composition; CPOE Owns Order Use Cases

**Decision:** Let actor-facing features compose CPOE interactions while requiring all Clinical Order mutations and CPOE lifecycle decisions to execute through CPOE application use cases.

**Rationale:** UI context and domain authority are different responsibilities. Keeping them separate allows the same order capability to be safely composed into several clinical features.

**Consequence:** Hosts may aggregate reads and coordinate user interactions, but they cannot duplicate order state machines, write CPOE persistence, or treat presentation authorization as authoritative.

### ADR-003 — CPOE Remains a Module in the Bilreg Modular Monolith

**Decision:** Place CPOE in the existing Domain, Application, Infrastructure, and API projects under `CpoeContext`.

**Rationale:** The repository already provides the required layer boundaries, MediatR application model, SQL persistence, authentication, audit, and local integration mechanisms.

**Consequence:** Local contexts can collaborate through in-process application ports. Module boundaries remain explicit, and CPOE Domain/Application code cannot access another context's DAL.

### ADR-004 — One Clinical Order Is One Aggregate

**Decision:** Realize each Clinical Order as an independent `ClinicalOrderModel`; an Order Set is an application-level batch source, not a shared lifecycle aggregate.

**Rationale:** Receiver decisions, revisions, occurrences, outcomes, and integrations are independently accountable for each order.

**Consequence:** Order-set interactions create and report multiple aggregate outcomes, and no order is locked or terminated solely because another set item changes.

### ADR-005 — Order Definition Versions Are Immutable References

**Decision:** Persist versioned Order Definitions and bind each Clinical Order to the version used at authoring.

**Rationale:** Active orders require deterministic validation, routing, and completion assessment after governance changes configuration for future orders.

**Consequence:** Definition changes create a new version; existing orders retain their original definition identity and required snapshot.

### ADR-006 — Specialized Execution Remains Outside CPOE

**Decision:** Integrate specialized Executing Domains through common fulfilment contracts and store only CPOE's coordination summary and authoritative references.

**Rationale:** A prospective instruction and a specialized departmental execution record have different ownership and consistency boundaries.

**Consequence:** Laboratory, radiology, pharmacy, procedure, and other departments keep their specialized workflow and correction authority. CPOE lifecycle evaluation consumes their authoritative facts without duplicating them.

### ADR-007 — Generic Fulfilment Is CPOE-Owned but Separately Aggregated

**Decision:** Use `GenericFulfilmentModel` only for Order Definitions explicitly routed to CPOE Generic Fulfilment.

**Rationale:** Authoritative execution evidence has a different consistency boundary from prospective instruction and is needed only where no specialized authority exists.

**Consequence:** A host may present Generic Fulfilment actions, but CPOE owns the use cases and record. Introducing a specialized domain retires the generic route for new orders without rewriting history.

### ADR-008 — Charge Eligibility Authority Follows Fulfilment Authority

**Decision:** Let the authoritative Executing Domain establish Charge Eligibility for specialized fulfilment and let CPOE establish it only for CPOE Generic Fulfilment. Tata Rekening consumes the authoritative fact directly or through a relay that preserves its source.

**Rationale:** Eligibility is evidence arising from actual fulfilment, not from order coordination, and financial consequence belongs independently to Tata Rekening.

**Consequence:** CPOE can associate or transport an eligibility fact but cannot re-author a specialized fact or determine tariff, coverage, bundling, bill, adjustment, or payment.

### ADR-009 — Actor and Service Contracts Are Separate

**Decision:** Separate actor-initiated application contracts from service-to-service integration contracts.

**Rationale:** A human action requires contextual clinical authorization, while an integration fact requires source-system authentication, source authority, stable fact identity, and idempotent ingestion.

**Consequence:** A service principal is not treated as a clinical actor, and an actor command cannot masquerade as an authoritative executing-domain callback.

### ADR-010 — Operational Work Uses Projections

**Decision:** Provide dedicated CPOE projections for receiver work, outstanding orders, due occurrences, overdue authorization, reconciliation, history, and integration recovery.

**Rationale:** Host composition and operational scanning need efficient filters across many aggregates without reconstructing every Aggregate Root.

**Consequence:** Command repositories remain consistency-focused, projection DALs evolve with read demand, and hosts consume CPOE-owned state rather than recreate it.

### ADR-011 — Direct Orchestration Internally; Durable Delivery at System Boundaries

**Decision:** Use direct application orchestration inside the monolith and SQL-backed inbox/outbox delivery across process or system boundaries; do not require a generic event bus.

**Rationale:** This matches Bilreg's explicit transaction style while providing reliable external delivery without speculative infrastructure.

**Consequence:** Local actor interactions can be synchronous. External delivery is at least once, consumers must be idempotent, and pending or failed obligations are operationally visible.

### ADR-012 — Integration Identity Is Stable and Idempotent

**Decision:** Give every retriable actor request, dispatch obligation, instruction revision, fulfilment fact, result/document association, care transition, and Charge Eligibility fact a stable identity in its authority scope.

**Rationale:** Retries, callbacks, and legacy reconciliation must not repeat domain behavior or create duplicate departmental work or financial handovers.

**Consequence:** Inbox, outbox, and request-ledger persistence enforce uniqueness and retain the prior processing outcome for safe replay.

### ADR-013 — Concurrency Is Optimistic per Aggregate

**Decision:** Persist and compare an aggregate version for every CPOE command mutation.

**Rationale:** Authorizer, Receiver, performer, integration, and reconciliation actions can race with amendments or terminal decisions.

**Consequence:** Stale writes fail explicitly. Handlers reload and reevaluate current state instead of silently overwriting newer clinical decisions.

### ADR-014 — Reconciliation Is Encounter-Level but Incremental

**Decision:** Keep Discharge Reconciliation as an encounter-level Aggregate Root containing references and dispositions while applying Clinical Order changes through explicit application orchestration.

**Rationale:** Reconciliation needs one accountable encounter summary without turning all outstanding orders into one large transactional aggregate.

**Consequence:** Item processing can progress and retry independently; reconciliation completes only from recorded item outcomes and preserves explicit unresolved exceptions.

### ADR-015 — Audit History Is Explicit and Append-Oriented

**Decision:** Preserve material domain history within aggregate-owned records and write searchable compliance entries through the shared explicit audit facility.

**Rationale:** Clinical accountability requires state-consistent history and cross-cutting investigation capability without hidden middleware behavior.

**Consequence:** Material handlers visibly persist aggregate changes, audit entries, and mandatory outbound obligations in an explicit transaction; transactional history is not physically deleted.
