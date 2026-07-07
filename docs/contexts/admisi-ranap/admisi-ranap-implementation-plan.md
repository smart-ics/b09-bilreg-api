# admisi-ranap-implementation-plan.md — Rawat Inap Admission Backend

> **Status:** Planning only — no implementation in this document.  
> **Canonical location:** `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md`  
> **API project:** `Bilreg.Api`  
> **Scope root:** `{Layer}/AdmisiRanapContext/` in each project (new context; does not replace `AdmisiContext`).

---

## 1. Feature Overview

**Rawat Inap Admission** is the administrative capability that prepares patients for inpatient care after a clinical hospitalization decision. The module hands over accommodation demand to the Ward domain via **Waiting List**; it never allocates Rooms or Beds.

| Owns | Does not own |
|------|----------------|
| Opname Request lifecycle | Room allocation |
| Reservation lifecycle | Bed allocation |
| Admission lifecycle | Ward accommodation operations |
| Waiting List lifecycle | Patient registration authority (REG) |
| Accommodation request hand-over to Ward | Billing / financial responsibility |

**Business boundary ends when** an Admission has either been handed over to the Waiting List for accommodation, or is ready for direct accommodation by the responsible Ward.

**Primary references (read before any slice):**

| Artifact | Use when |
|----------|----------|
| `docs/contexts/admisi-ranap/admisi-ranap-domain.md` | Aggregates, rules, lifecycles, workflows |
| `docs/contexts/admisi-ranap/admisi-ranap-architecture.md` | Use cases, feature boundaries, ADRs, integration |
| `docs/ENGINEERING.md` | Layers, repository, domain event philosophy |
| `docs/DATABASE.md` | Table/column/audit rules |
| `docs/NAMING.md` | Naming conventions |
| `docs/INSTRUCTION.md` | Global engineering stance |
| `docs/concepts/operational-events.md` | Disambiguate operational timeline vs domain events |
| `docs/skills/feature-model-generation.md` | Domain models |
| `docs/skills/feature-persistence-generation.md` | DTO/DAL/Repo |
| `docs/skills/use-case-generation.md` | Cmd/Query/Handler |

**Feature folders (target):**

| Folder | Aggregate Root |
|--------|----------------|
| `OpnameRequestFeature` | Opname Request |
| `ReservationFeature` | Reservation |
| `AdmissionFeature` | Admission |
| `WaitingListFeature` | Waiting List |

---

## 2. Current Backend Analysis

### 2.1 Solution structure (exists)

```text
Bilreg.Api            → HTTP, IMediator, JSendOk, JWT [Authorize]
Bilreg.Application    → Handlers, I*Repo, integration interfaces
Bilreg.Domain         → Models, enums, behaviour
Bilreg.Infrastructure → Dto, Dal (Dapper), Repo, integration implementations
Bilreg.SqlDb          → DDL per Context/Feature
Bilreg.Test           → Domain, handler, repo, optional DAL integration tests
```

**Dependency flow:** `Api → Application → Domain`; `Infrastructure → Application → Domain`.

**DI:** MediatR scans `Bilreg.Application`. DAL/Repo auto-registered via Scrutor (`InfrastructureService.cs`).

### 2.2 Admisi Ranap module status

| Component | Status | Notes |
|-----------|--------|-------|
| `AdmisiRanapContext` (all layers) | **PLANNED** | No feature folders exist |
| Opname Request aggregate | **PLANNED** | — |
| Reservation aggregate | **PLANNED** | — |
| Admission aggregate | **PLANNED** | — |
| Waiting List aggregate | **PLANNED** | — |
| Use cases (10) | **PLANNED** | Per architecture doc |
| REST APIs | **PLANNED** | — |
| Permission-based authorization | **PLANNED** | Architecture requires; codebase has auth only |
| SQL tables | **PLANNED** | — |

### 2.3 Related existing code (not the target module)

| Area | Location | Relationship |
|------|----------|--------------|
| Outpatient registration | `AdmisiContext/RegFeature` (`RegModel`, `RegController`) | Separate concern; `JenisRegEnum.RegInap` exists but is not Admission |
| Inpatient entry procedure lookup | `RegFeature/ProsedurMasukInap*` | Master-data read only (`ta_caramasuk_inap`) |
| Ward / bed master | `BedUsageContext/WardFeature` | Ward owns accommodation; consume Waiting List |
| Patient demographics | `PasienContext/PasienFeature` | Integration source for Patient Administration |
| Doctor / PPA | `AdmisiContext/PpaFeature` | Integration source for Doctor Services |

**Do not** implement Admission by extending `RegModel` or overloading `RegInap` registration flows. The new module is a **greenfield context** with explicit integration ports.

### 2.4 Approved reference patterns

| Pattern | Reference | Notes |
|---------|-----------|-------|
| Aggregate + behaviour methods | `ChargeContext/TarifFeature/TarifPolicyType` | State transitions, invariant enforcement |
| Repo reconstruct + atomic save | `ChargeContext/TarifFeature/TarifPolicyRepo` | Header + detail delete+insert |
| Use-case handler | `ChargeContext/TarifFeature/UseCases/TrfCreateTarifPolicyCmd` | Guard in handler; behaviour in model |
| API surface | `ChargeContext/TarifPolicyController` | Thin MediatR, use-case-shaped routes |
| Cross-aggregate orchestration | `IgdContext/IgdVisitFeature` handlers | Explicit, small transaction scope |
| Domain tests | `Bilreg.Test/ChargeContext/TarifFeature/TarifPolicyTypeTest` | Invariant coverage |
| Handler tests | `TrfTarifPolicyAdminHandlerTest` | Mock repo |
| DAL integration tests | `TarifPolicyDalIntegrationTest` | Optional; graceful skip if tables missing |

### 2.5 Do NOT use as reference

| File | Reason |
|------|--------|
| `RegJalanWalkInCommand.cs` | Legacy orchestration; not approved pattern |
| Direct bed/room assignment in admission flow | Violates BR-RI-011, BR-RI-012, ADR-002 |

---

## 3. Gap Analysis (target architecture vs codebase)

### 3.1 Domain

| Target | Gap |
|--------|-----|
| Four aggregate roots with explicit consistency boundaries | None implemented |
| State machines per `admisi-ranap-domain.md` §8 | None implemented |
| Business rules BR-RI-001 … BR-RI-012 | None enforced in code |
| Admission does not own Waiting List, Room, Bed | Must be structural (no accommodation APIs on Admission) |

### 3.2 Application

| Target | Gap |
|--------|-----|
| One use case per business capability | 0 of 10 handlers |
| Cross-aggregate coordination in application layer | Not present |
| One repository per aggregate root | Not present |
| Repositories never coordinate workflows | Convention exists; repos not built |

### 3.3 Infrastructure

| Target | Gap |
|--------|-----|
| `OpnameRequestRepository`, `ReservationRepository`, `AdmissionRepository`, `WaitingListRepository` | Not present |
| Waiting List persisted independently from Admission (ADR-003) | Not present |
| Synchronous integrations: Doctor Services, Ward, Patient Administration | Not present |
| No messaging infrastructure | Aligns with architecture — do not introduce |

### 3.4 API

| Target | Gap |
|--------|-----|
| REST, CQS, use-case-shaped endpoints | Not present |
| Waiting List API for Ward consumers | Not present |
| Commands: Create, Update, Cancel, Fulfill, Close | Not present |
| Queries: aggregate retrieval, waiting list retrieval, admission lookup | Not present |

### 3.5 Authorization

| Target | Gap |
|--------|-----|
| Permission-based authorization per business responsibility | Only `[Authorize]` (authentication) today |
| Examples: Process Admission, Maintain Reservation, Manage Waiting List | No permission model or policies |

### 3.6 Tests

| Target | Gap |
|--------|-----|
| Domain invariant tests per aggregate | Not present |
| Use-case orchestration tests | Not present |
| Repo round-trip tests | Not present |
| Optional DAL integration tests | Not present |

---

## 4. Engineering Conventions (this module)

| Concern | Convention |
|---------|------------|
| Context name | `AdmisiRanapContext` |
| Domain type | `{Name}Model` (e.g. `AdmissionModel`) |
| Value objects | `{Name}Type`, `{Name}Reff` |
| Key | `I{Name}Key` |
| Status enum | `{Name}StatusEnum` |
| Command / Query | `Rir{Name}{Action}Cmd`, `Rir{Name}{Action}Qry` (`Rir` = Rawat Inap Ranap prefix) |
| Handler | `Rir{Name}{Action}Handler` |
| Repo interface | `Application/.../I{Name}Repo` |
| Repo implementation | `Infrastructure/.../{Name}Repo` |
| DAL | Dapper, Nuna `IInsert`/`IUpdate`/`IGetData`/`IListData` |
| Validation | `Guard` in handler; invariants in model |
| API | `JSendOk`; route prefix `api/admisi-ranap/...` |
| Transaction | `TransHelper.NewScope()` in handler when cross-repo |
| **Tables** | `BILRG_Adm*` for admission workflow roots; `BILRG_BedWaitingList` for Waiting List |

Per `docs/ENGINEERING.md` §14: domain events are **allowed but optional**. Prefer **direct orchestration** in use-case handlers. If operational timeline rows are needed, follow `docs/concepts/operational-events.md` — not event sourcing.

---

## 5. Recommended Folder Structure

```text
Bilreg.Domain/AdmisiRanapContext/
  OpnameRequestFeature/
    IOpnameRequestKey.cs
    OpnameRequestModel.cs
    OpnameRequestStatusEnum.cs
  ReservationFeature/
    IReservationKey.cs
    ReservationModel.cs
    ReservationStatusEnum.cs
  AdmissionFeature/
    IAdmissionKey.cs
    AdmissionModel.cs
    AdmissionStatusEnum.cs
  WaitingListFeature/
    IWaitingListKey.cs
    WaitingListModel.cs
    WaitingListStatusEnum.cs

Bilreg.Application/AdmisiRanapContext/
  OpnameRequestFeature/
    IOpnameRequestRepo.cs
    UseCases/...
  ReservationFeature/
    IReservationRepo.cs
    UseCases/...
  AdmissionFeature/
    IAdmissionRepo.cs
    UseCases/...
  WaitingListFeature/
    IWaitingListRepo.cs
    IWaitingListWorklistDal.cs          # operational projection for active queue
    UseCases/...
  Integration/
    IDoctorServiceGateway.cs
    IPatientAdministrationGateway.cs
    IWardAccommodationGateway.cs

Bilreg.Infrastructure/AdmisiRanapContext/
  OpnameRequestFeature/                 # Dto, Dal, Repo
  ReservationFeature/
  AdmissionFeature/
  WaitingListFeature/
  Integration/
    DoctorServiceGateway.cs
    PatientAdministrationGateway.cs
    WardAccommodationGateway.cs

Bilreg.Api/Controllers/AdmisiRanapContext/
  OpnameRequestController.cs
  ReservationController.cs
  AdmissionController.cs
  WaitingListController.cs

Bilreg.SqlDb/AdmisiRanapContext/
  OpnameRequestFeature/BILRG_AdmOpnameRequest.sql
  ReservationFeature/BILRG_AdmReservation.sql
  AdmissionFeature/BILRG_AdmAdmission.sql
  WaitingListFeature/BILRG_BedWaitingList.sql

Bilreg.Test/AdmisiRanapContext/
  OpnameRequestFeature/...
  ReservationFeature/...
  AdmissionFeature/...
  WaitingListFeature/...
```

---

## 6. Aggregate Implementation Summary

Behaviour and lifecycles are defined in `admisi-ranap-domain.md`. This section records implementation obligations only.

### 6.1 Opname Request

| Item | Detail |
|------|--------|
| States | Requested → Fulfilled \| Cancelled |
| Key rules | BR-RI-001 (Doctor only creates), BR-RI-002 (not an Admission), BR-RI-005 (≤1 Admission fulfillment) |
| Cross-aggregate | Fulfillment coordinated when Process Admission references Opname Request |

### 6.2 Reservation

| Item | Detail |
|------|--------|
| States | Reserved → Maintained → Realized \| Cancelled |
| Key rules | BR-RI-003 (optional before Admission), BR-RI-006 (≤1 Admission realization) |
| Cross-aggregate | Realization coordinated when Process Admission references Reservation |

### 6.3 Admission

| Item | Detail |
|------|--------|
| States | Admitted → Updated → Waiting → Completed \| Cancelled |
| Key rules | BR-RI-004 (one inpatient episode), BR-RI-007–BR-RI-011 |
| Does not own | Waiting List entity, Room, Bed, accommodation |
| Produces | Waiting List creation **request** (orchestrated in application layer) |

### 6.4 Waiting List

| Item | Detail |
|------|--------|
| States | Waiting → Accepted → Closed |
| Key rules | BR-RI-007–BR-RI-010, BR-RI-008 (≤1 active per Admission) |
| Owns | Waiting status, priority, accommodation requirements, destination Ward info |
| Independent persistence | Separate table(s) and repo per ADR-003 |
| Ward contract | Ward consumes Waiting List, not Admission (ADR-004) |

### 6.5 Domain implementation order

1. Status enums + keys + shared value objects (Care Class, Care Level refs)  
2. `OpnameRequestModel` + transitions + tests  
3. `ReservationModel` + transitions + tests  
4. `AdmissionModel` + transitions + tests  
5. `WaitingListModel` + transitions + tests  

**Skill:** `docs/skills/feature-model-generation.md`

---

## 7. Database Implementation Plan

### 7.1 Tables

**Naming:**

| Table | Purpose |
|-------|---------|
| `BILRG_AdmOpnameRequest` | Opname Request aggregate root |
| `BILRG_AdmReservation` | Reservation aggregate root |
| `BILRG_AdmAdmission` | Admission aggregate root |
| `BILRG_BedWaitingList` | Waiting List aggregate root (independent operational working set) |

Detail tables added only when aggregate owns child collections requiring deterministic ordering. Use composite PK `(ParentId, ItemNo)` per `DATABASE.md`.

**Standards:** Application-generated PK on roots (see §7.3 for per-aggregate length), no FK constraints, audit + void columns, `INT` status enums, `'3000-01-01'` empty dates, PascalCase columns.

Logical references (no DB FK):

| Column (conceptual) | Type | References |
|---------------------|------|------------|
| `OpnameRequestId` on Admission | `VARCHAR(12)` | Opname Request (optional) |
| `ReservationId` on Admission | `VARCHAR(12)` | Reservation (optional) |
| `AdmissionId` on Waiting List | `VARCHAR(10)` | Admission (required) |
| `PasienId`, `DokterId`, `BangsalId` | per master | External master / Ward |

Snapshot columns (Patient name, Doctor name, Ward name) per `DATABASE.md` §9 where operationally queried.

### 7.2 Indexes (initial)

| Table | Index | Purpose |
|-------|-------|---------|
| `BILRG_BedWaitingList` | `(WaitingListStatus, CrtDate)` | Active queue scan |
| `BILRG_BedWaitingList` | `(BangsalId, WaitingListStatus)` | Ward-filtered queue |
| `BILRG_AdmAdmission` | `(AdmissionStatus, CrtDate)` | Admission lookup |
| `BILRG_AdmAdmission` | `(PasienId, AdmissionStatus)` | Patient admission history |
| `BILRG_AdmOpnameRequest` | `(OpnameRequestStatus, CrtDate)` | Open requests |
| `BILRG_AdmReservation` | `(ReservationStatus, PlannedDate)` | Planned admissions |

### 7.3 Identifier strategy

Application-generated opaque IDs. Status columns → `INT` enum storage.

| Aggregate | PK column | SQL type | Generator | Prefix |
|-----------|-----------|----------|-----------|--------|
| Admission | `AdmissionId` | `VARCHAR(10)` | `NunaId.NewLegacyCompact()` | `RG` |
| Opname Request | `OpnameRequestId` | `VARCHAR(12)` | `NunaId.NewLegacy()` | `OPN` |
| Reservation | `ReservationId` | `VARCHAR(12)` | `NunaId.NewLegacy()` | `RES` |
| Waiting List | `WaitingListId` | `VARCHAR(12)` | `NunaId.NewLegacy()` | `WTL` |

**Examples (illustrative):** `RG00001234`, `OPN000000001`, `RES000000001`, `WTL000000001`.

**Rules:**

- Admission uses the compact legacy format aligned with existing registration identifiers (`RG` prefix, 10 chars).
- All other aggregate roots use standard legacy format (`VARCHAR(12)`).
- Foreign-key columns must match the referenced aggregate PK width (`AdmissionId` → `VARCHAR(10)` on `BILRG_BedWaitingList`).

**Skill:** `docs/skills/feature-persistence-generation.md`

---

## 8. Use Cases & API Mapping

Use cases are authoritative in `admisi-ranap-architecture.md` §3. API exposes business capabilities — not CRUD tables.

### 8.1 Opname Request

| Use Case | HTTP (proposed) | Command / Query |
|----------|-----------------|-----------------|
| Create Opname Request | `POST api/admisi-ranap/opname-request` | `RirCreateOpnameRequestCmd` |
| Cancel Opname Request | `POST api/admisi-ranap/opname-request/{id}/cancel` | `RirCancelOpnameRequestCmd` |
| Get Opname Request | `GET api/admisi-ranap/opname-request/{id}` | `RirGetOpnameRequestQry` |
| List Opname Requests | `GET api/admisi-ranap/opname-request` | `RirListOpnameRequestQry` |

### 8.2 Reservation

| Use Case | HTTP (proposed) | Command / Query |
|----------|-----------------|-----------------|
| Create Reservation | `POST api/admisi-ranap/reservation` | `RirCreateReservationCmd` |
| Maintain Reservation | `PUT api/admisi-ranap/reservation/{id}` | `RirMaintainReservationCmd` |
| Get Reservation | `GET api/admisi-ranap/reservation/{id}` | `RirGetReservationQry` |
| List Reservations | `GET api/admisi-ranap/reservation` | `RirListReservationQry` |

### 8.3 Admission

| Use Case | HTTP (proposed) | Command / Query |
|----------|-----------------|-----------------|
| Process Admission | `POST api/admisi-ranap/admission` | `RirProcessAdmissionCmd` |
| Update Admission | `PUT api/admisi-ranap/admission/{id}` | `RirUpdateAdmissionCmd` |
| Cancel Admission | `POST api/admisi-ranap/admission/{id}/cancel` | `RirCancelAdmissionCmd` |
| Get Admission | `GET api/admisi-ranap/admission/{id}` | `RirGetAdmissionQry` |
| Admission lookup | `GET api/admisi-ranap/admission` | `RirLookupAdmissionQry` |

### 8.4 Waiting List

| Use Case | HTTP (proposed) | Command / Query |
|----------|-----------------|-----------------|
| Create Waiting List | `POST api/admisi-ranap/waiting-list` | `RirCreateWaitingListCmd` |
| Update Waiting List | `PUT api/admisi-ranap/waiting-list/{id}` | `RirUpdateWaitingListCmd` |
| Close Waiting List | `POST api/admisi-ranap/waiting-list/{id}/close` | `RirCloseWaitingListCmd` |
| Get Waiting List | `GET api/admisi-ranap/waiting-list/{id}` | `RirGetWaitingListQry` |
| List active Waiting List | `GET api/admisi-ranap/waiting-list` | `RirListWaitingListQry` |

**Ward consumer:** `GET api/admisi-ranap/waiting-list` is the primary integration surface (ADR-004).

**Skill:** `docs/skills/use-case-generation.md`

---

## 9. Integration Plan

All communication is **synchronous** per architecture doc. No messaging infrastructure.

| Integration | Port (Application) | Purpose | Direction |
|-------------|-------------------|---------|-----------|
| Doctor Services | `IDoctorServiceGateway` | Validate doctor identity; clinical request context | Inbound to Create Opname Request |
| Patient Administration | `IPatientAdministrationGateway` | Patient demographic lookup / snapshot | Inbound to Admission, Reservation |
| Ward | `IWardAccommodationGateway` | Hand-over Waiting List; Ward accepts accommodation responsibility | Outbound from Waiting List lifecycle |

**Ward boundary:** Admission module calls Ward only through Waiting List contract. Ward accommodation permissions belong to Ward module.

**V1 strategy:** Implement gateway interfaces with adapters wrapping existing repos (`IPasienRepo`, `IPpaRepo`, `IWardRepo` or equivalent). Replace with HTTP clients only when external service boundaries require it.

---

## 10. Authorization Plan

Architecture requires **permission-based authorization** aligned to business responsibilities.

### 10.1 Proposed permissions

| Permission | Use cases |
|------------|-----------|
| `admisi-ranap.opname-request.create` | Create Opname Request |
| `admisi-ranap.opname-request.cancel` | Cancel Opname Request |
| `admisi-ranap.reservation.create` | Create Reservation |
| `admisi-ranap.reservation.maintain` | Maintain Reservation |
| `admisi-ranap.admission.process` | Process Admission |
| `admisi-ranap.admission.update` | Update Admission |
| `admisi-ranap.admission.cancel` | Cancel Admission |
| `admisi-ranap.waiting-list.manage` | Create, Update, Close Waiting List |
| `admisi-ranap.waiting-list.read` | Query Waiting List (Ward consumer) |

### 10.2 Enforcement layers

1. **API:** `[Authorize(Policy = "...")]` on controller actions.  
2. **Application:** Handler checks permission via `ICurrentUserContext` extension or dedicated `IAdmisiRanapAuthorizationService` (defense in depth).

Ward accommodation permissions (`bed assign`, `room assign`) remain in **BedUsageContext** — not in this module.

### 10.3 Current gap

Today `Bilreg.Api` registers JWT authentication and `ICurrentUserContext` (user id only). Permission claims and authorization policies must be introduced before production rollout of write endpoints.

---

## 11. Phased Implementation Roadmap

Recommended order minimizes risk: **new context, new tables, new routes** — no modification to legacy `AdmisiContext.RegFeature` flows.

### Phase 0 — Scaffolding & conventions

| | |
|--|--|
| **Objective** | Establish module boundary and naming so parallel work is predictable. |
| **Scope** | Create empty `AdmisiRanapContext` folders in Domain, Application, Infrastructure, Api, SqlDb, Test. Document table prefix, command prefix (`Rir`), route prefix. |
| **Deliverables** | Folder skeleton; solution builds with no behaviour. |
| **Dependencies** | None. |
| **Acceptance criteria** | Build succeeds; no existing endpoint changes; conventions documented in this plan §4. |

---

### Phase 1 — Domain layer

| | |
|--|--|
| **Objective** | Implement four aggregate roots with state machines and BR-RI-001…BR-RI-012 invariants. |
| **Scope** | Domain models, enums, keys, value objects; domain unit tests per aggregate. |
| **Deliverables** | `OpnameRequestModel`, `ReservationModel`, `AdmissionModel`, `WaitingListModel` + `*TypeTest.cs` files. |
| **Dependencies** | Phase 0. |
| **Acceptance criteria** | All domain tests pass; Domain has zero Infrastructure references; Waiting List is structurally independent from Admission. |

**Tests:**

| ID | Scenario |
|----|----------|
| DT-OR-01 | Only valid transitions on Opname Request |
| DT-OR-02 | Cancelled request cannot be fulfilled |
| DT-RS-01 | Reservation optional path (no Opname Request required for elective) |
| DT-AD-01 | Admission represents one episode |
| DT-AD-02 | Admission has no room/bed allocation methods |
| DT-WL-01 | Only admitted patient may enter Waiting List |
| DT-WL-02 | At most one active Waiting List per Admission |
| DT-WL-03 | Waiting List status change does not mutate Admission status |

---

### Phase 2 — Persistence & repositories

| | |
|--|--|
| **Objective** | Durable storage with one repository per aggregate root; independent Waiting List persistence. |
| **Scope** | SQL scripts, DTOs, DALs, repos; repo unit tests; optional DAL integration tests. |
| **Deliverables** | `BILRG_AdmOpnameRequest`, `BILRG_AdmReservation`, `BILRG_AdmAdmission`, `BILRG_BedWaitingList`; `OpnameRequestRepo`, `ReservationRepo`, `AdmissionRepo`, `WaitingListRepo`; `IWaitingListWorklistDal` for queue projection. |
| **Dependencies** | Phase 1. |
| **Acceptance criteria** | Aggregate round-trip save/load; active Waiting List query does not scan Admission history; repos contain no workflow orchestration. |

**Tests:**

| ID | Scenario |
|----|----------|
| UT-RP-01 | New aggregate insert |
| UT-RP-02 | Existing aggregate update |
| UT-RP-03 | Load reconstructs full aggregate |
| IT-DL-01 | SQL round-trip (skip gracefully if tables not deployed) |

---

### Phase 3 — Application use cases

| | |
|--|--|
| **Objective** | Implement all 10 architecture use cases with cross-aggregate orchestration. |
| **Scope** | MediatR commands/queries + handlers; mock-repo handler tests. |
| **Deliverables** | Handlers for Create/Cancel Opname Request; Create/Maintain Reservation; Process/Update/Cancel Admission; Create/Update/Close Waiting List; query handlers. |
| **Dependencies** | Phase 2. |
| **Acceptance criteria** | Handler tests pass; BR-RI-005/006/008 enforced in orchestration; Process Admission can optionally fulfill Opname Request and realize Reservation in one transaction scope. |

**Workflow coverage:**

| Workflow | Use-case chain |
|----------|----------------|
| Direct Admission | Create Opname Request → Process Admission → (optional) Create Waiting List |
| Planned Admission | Create Opname Request → Create Reservation → Process Admission → (optional) Create Waiting List |
| Elective Admission | Create Reservation → Process Admission → (optional) Create Waiting List |
| Patient Transfer | (Ward release — external) → Create Waiting List → (Ward accommodation — external) |

**Tests:**

| ID | Scenario |
|----|----------|
| UT-UC-01 | Process Admission fulfills Opname Request |
| UT-UC-02 | Second Admission for same Opname Request rejected |
| UT-UC-03 | Create Waiting List requires admitted patient |
| UT-UC-04 | Close Waiting List does not cancel Admission |

---

### Phase 4 — REST API

| | |
|--|--|
| **Objective** | Expose use-case-shaped REST endpoints with CQS separation. |
| **Scope** | Four controllers per §8; Swagger documentation; `[Authorize]` baseline. |
| **Deliverables** | `OpnameRequestController`, `ReservationController`, `AdmissionController`, `WaitingListController`. |
| **Dependencies** | Phase 3. |
| **Acceptance criteria** | All routes map 1:1 to use cases; `JSendOk` responses; existing APIs unchanged; Swagger lists new routes. |

---

### Phase 5 — Integration gateways

| | |
|--|--|
| **Objective** | Wire synchronous integrations for Doctor, Patient Administration, and Ward. |
| **Scope** | Application ports + Infrastructure adapters; contract tests or mocked adapter tests. |
| **Deliverables** | `DoctorServiceGateway`, `PatientAdministrationGateway`, `WardAccommodationGateway`. |
| **Dependencies** | Phase 3 (minimum); Phase 4 for external Ward HTTP if applicable. |
| **Acceptance criteria** | Ward consumes Waiting List API/contract only; no bed allocation in Admisi Ranap; patient snapshot populated on Admission. |

---

### Phase 6 — Permission-based authorization

| | |
|--|--|
| **Objective** | Enforce architecture §8 authorization at API and handler level. |
| **Scope** | Permission definitions; authorization policies; handler defense-in-depth. |
| **Deliverables** | Policy registration; `[Authorize(Policy)]` on endpoints; `IAdmisiRanapAuthorizationService` or equivalent. |
| **Dependencies** | Phase 4; identity provider must emit permission claims (coordination with auth team). |
| **Acceptance criteria** | Unauthorized requests return 403; each write use case has explicit permission; Ward read permission separable from Admisi write permissions. |

---

### Phase 7 — Hardening, migration & rollout

| | |
|--|--|
| **Objective** | Production-ready rollout without breaking existing functionality. |
| **Scope** | Deployment scripts order; rollback scripts; audit logging; end-to-end scenario validation; legacy coexistence. |
| **Deliverables** | SQL deployment checklist; rollback scripts; operational runbook section (future `admisi-ranap-runbook.md` if needed); E2E test checklist. |
| **Dependencies** | Phases 1–6. |
| **Acceptance criteria** | All four business workflows validated; Waiting List queue performance acceptable; existing `RegFeature` tests still pass; additive schema only. |

---

## 12. Technical Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Concept overlap with `RegInap` / `ProsedurMasukInap` | Wrong aggregate boundaries | Dedicated `AdmisiRanapContext`; integrate via ports, not extend `RegModel` |
| Waiting List coupled to Admission table | Violates ADR-001, ADR-003 | Separate repo + table; explicit tests |
| Room/bed allocation leaks into Admission | Violates BR-RI-011, ADR-002 | Code review gate; no `Bed`/`Kamar` refs in Admission feature |
| Permission model not in identity token | Authorization blocked | Phase 6 coordinates with auth; stub policies for dev/test only |
| Cross-aggregate race (duplicate fulfillment) | Data inconsistency | Application transaction scope; unique logical constraint checks in handler |
| Legacy consumers expect old registration API | Integration breakage | New routes only; bridge adapter later if required |
| Migration from historical data | Incomplete records | Phase 7 optional backfill script; do not block greenfield go-live |

---

## 13. Backward Compatibility & Migration

### 13.1 Principles

- **Additive only:** new tables, new context, new API routes.
- **No changes** to `AdmisiContext.RegFeature` endpoints or `RegModel` behaviour in initial rollout.
- **No database FK constraints** per `DATABASE.md` — logical references enforced in application layer.
- **Void lifecycle** for cancellations — no hard delete on transaction tables.

### 13.2 Deployment order

1. Deploy SQL create scripts (`BILRG_AdmOpnameRequest`, `BILRG_AdmReservation`, `BILRG_AdmAdmission`, `BILRG_BedWaitingList`).  
2. Deploy application binaries (new handlers inactive until API routed).  
3. Enable API routes.  
4. Configure permissions in identity provider.  
5. Enable Ward consumer on Waiting List query endpoints.

### 13.3 Rollback

- API: disable routes or feature flag.  
- Application: previous binary.  
- Database: tables remain (no destructive rollback required); optional rollback scripts drop new tables only if no production data.

### 13.4 Legacy bridge (optional, post-MVP)

If consumers still use registration-centric inpatient flows, introduce a **read-only bridge** or **compat command** that translates legacy `RegInap` identifiers to `AdmissionId` — without merging aggregates.

---

## 14. Test Strategy

Priority per `ENGINEERING.md` §22:

| Layer | Focus |
|-------|-------|
| Domain | State transitions, BR-RI invariants, forbidden operations |
| Application | Orchestration, cross-aggregate rules, permission rejection |
| Infrastructure | Repo reconstruction, DAL round-trip (optional integration) |
| API | Policy enforcement smoke tests (after Phase 6) |

**Test location:** `Bilreg.Test/AdmisiRanapContext/{Feature}/`

**Naming:** `DT*` domain tests, `UT*` unit/handler tests, `IT*` integration tests — consistent with Tarif module.

---

## 15. Implementation Order Summary

```text
Phase 0  Scaffolding
   ↓
Phase 1  Domain (4 aggregates + tests)          ← lowest risk, no DB
   ↓
Phase 2  SQL + DAL + Repos                     ← isolated new tables
   ↓
Phase 3  Use cases (10 handlers + tests)        ← business value
   ↓
Phase 4  REST API                               ← consumable
   ↓
Phase 5  Integrations (Doctor, Pasien, Ward)
   ↓
Phase 6  Permission-based authorization
   ↓
Phase 7  Hardening + migration + E2E validation
```

Phases 1–4 deliver a vertically testable module. Phases 5–7 make it production-safe.

---

## 16. Phase Ledger

| Phase | Status | Notes |
|-------|--------|-------|
| 0 — Scaffolding | **LIVE** | See `admisi-ranap-phase-0-implementation-report.md` |
| 1 — Domain | **PLANNED** | — |
| 2 — Persistence | **PLANNED** | — |
| 3 — Use cases | **PLANNED** | — |
| 4 — API | **PLANNED** | — |
| 5 — Integration | **PLANNED** | — |
| 6 — Authorization | **PLANNED** | — |
| 7 — Hardening / rollout | **PLANNED** | — |

Update this ledger as phases ship.

---

## 17. Related Artifacts (future)

| Path | Purpose |
|------|---------|
| `docs/contexts/admisi-ranap/admisi-ranap-api-contract.md` | Frontend / Ward integration contract (create when API stabilizes) |
| `docs/contexts/admisi-ranap/admisi-ranap-runbook.md` | Deployment, troubleshooting, recovery |

Do not create these until Phase 4+ unless explicitly requested.
