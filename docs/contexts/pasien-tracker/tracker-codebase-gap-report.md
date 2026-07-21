# Tracker Domain — Codebase Gap Report

**Date:** 2026-07-21  
**Authoritative business definition:** `docs/contexts/pasien-tracker/TRACKER-DOMAIN.md`  
**Assessment type:** Static codebase investigation plus a bounded, non-database unit-test run  
**Implementation scope:** `src/bilreg`, `src/farinv`, and the relevant Taksaka maintenance worker

## 1. Executive Summary

The current codebase contains recognizable precursors of both Tracker aggregates, but it does not implement the clarified Patient Tracker domain end to end.

The strongest implemented portion is booking-time creation: a booking creates a ULID-based `PasienTrackerId`, a `BOOKING` event, a physician queue header, an identified queue entry, and a queue number that is also written to the booking and legacy queue map. The persisted queue entry also has the three intended milestone columns and the same `Waiting`, `InService`, and `Done` enum names.

The implementation diverges from the canonical domain at the points that determine business meaning:

- Tracking Period persistence exists (`StartPeriod`/`LastPeriod` in model, DAL, and additive DDL), but change/cancel paths and incomplete later evidence still limit BR-TRK-005 through BR-TRK-008 in live workflows (see F-01/F-02/F-08).
- There is no operational Journey Resolution use case. Booking creation performs a duplicate rejection, not candidate presentation and accountable selection. Anonymous queue intake is present only as an unused domain method and a fully commented-out handler.
- `AntrianEntryModel.AssignPasien` copies only the name and does not assign the `Tracker` key. Even if the dormant anonymous flow were enabled, it would not make the entry Identified.
- Registration and physician service semantics are conflated. Both walk-in and booking-based registration call `Serve` on the physician queue entry at registration time. No separate admission queue service is completed, so registration waiting time, registration service duration, and post-registration consultation waiting time cannot be calculated according to BR-TRK-040 through BR-TRK-043.
- Tracker event persistence shape is append-only with deterministic `(PasienTrackerId, NoUrut)` ordering (see F-12/F-03), but only Booking/Registration produce evidence today and cancellation still deletes tracker headers (see F-02/F-08).
- Registration cancellation and booking deletion physically remove queue entries and tracker headers. A change-of-visit creates a new `TrackerId` for the same registration. These paths conflict with stable journey identity and cumulative evidence, although the correct cancellation/reschedule representation requires a domain decision because `TRACKER-DOMAIN.md` does not define those workflows.
- Consultation evidence is not recorded in the tracker. Pharmacy has a separate `FARIN_Antrian` model and lifecycle, but it has no `TrackerId`, no integration with Bilreg Tracker, and no application handlers that invoke its prepare/deliver transitions from drug-sale confirmation or medicine handover.
- No candidate, timeline, anonymous intake, identification, service-start, or Tracker-specific API contract exists. The only completion endpoint returns without awaiting its command.
- None of the ten stable domain facts in section 9 are represented explicitly. Direct orchestration is acceptable under `docs/ENGINEERING.md`, but the required cross-context facts and workflows are still absent.

Overall assessment: the implementation is a **partial, pre-domain queue/booking solution**, not a conforming V1 Tracker bounded context. The most urgent issues are journey identity continuity, evidence durability, Journey Resolution, and separation of admission/registration milestones from physician service milestones.

Severity totals for the confirmed detailed findings in section 5:

| Severity | Count |
|---|---:|
| Critical | 5 |
| High | 6 |
| Medium | 3 |
| Low | 0 |

## 2. Investigation Scope and Method

### 2.1 Sources used

The assessment treated `docs/contexts/pasien-tracker/TRACKER-DOMAIN.md` as business truth. In particular, the investigation enumerated:

- all five capabilities in section 3;
- all actors, entities, value objects, and both aggregates in sections 4–6;
- every rule from BR-TRK-001 through BR-TRK-050;
- all lifecycle transitions in section 8;
- all ten stable domain facts in section 9; and
- all eight workflows in section 10.

Repository guidance was taken from `docs/ARTIFACTS.md`, `docs/INSTRUCTION.md`, `docs/ENGINEERING.md`, `docs/DATABASE.md`, `docs/WORKFLOW.md`, and `docs/concepts/operational-events.md`. The last document was used to distinguish `PasienTrackerEvent` timeline evidence from compliance audit and architectural domain events.

`docs/contexts/pasien-tracker/pasien-tracker-narrative-explanation.md` was not treated as implementation evidence or as business authority.

### 2.2 Code paths inspected

The investigation traced actual calls and persistence through:

- Bilreg domain models in `Bilreg.Domain/AdmisiContext/AntrianFeature`;
- Bilreg use cases in Booking, Registration, Jadwal Praktek, and Antrian features;
- Bilreg repositories, DTOs, DALs, SQL DDL, API controller, and dependency registration;
- legacy `AntrianMapModel`, `ta_no_antrian_map_hdr`, and `ta_no_antrian_map` paths;
- external EMR queue calls from booking and registration;
- the Taksaka `AntrianConsistencyRepairWorker`;
- Farinv pharmacy queue domain/application/infrastructure/API/SQL; and
- Bilreg and Taksaka tests related to these paths.

Name similarity or a dormant method was not counted as implementation unless an active application/API path used it with the required semantics.

### 2.3 Verification performed

Static searches found no implementation under Tracker-related code for `TrackingPeriod` (value object), `JourneyCandidate`, `QueueEvidenceReference`, or the section-9 domain-fact names. `StartPeriod`/`LastPeriod` and `ServicePointCode` persistence are present in source (see F-12).

A bounded unit-test run was executed:

```text
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore \
  --filter "FullyQualifiedName~PasienTrackerModelTest|FullyQualifiedName~AntrianFactoryTests|FullyQualifiedName~AntrianEntryModelTest"
```

Result: **19 total, 14 passed, 5 failed**. One failure proves that the default `Serve()`/`Done()` path cannot complete because both receive `DateTime.MinValue`; four failures show stale `SequenceTag` expectations. This result is evidence about current test reliability, not evidence that all Tracker code is broken.

### 2.4 Limits and evidence confidence

This was a source investigation. No production or staging database was queried, no external EMR/desktop application was available, and no live Taksaka job configuration was inspected. Therefore:

- source-defined schemas and SQL behavior are verified;
- actual deployed schema/version and existing data quality are **Unable to Verify**;
- external desktop consumers and their reliance on individual legacy columns are **Unable to Verify** beyond in-repository callers; and
- runtime delivery/ordering of external calls is inferred from code, not observed in production.

## 3. Current Implementation Overview

| Area | Current implementation | Verified behavior |
|---|---|---|
| Logical tracker | `PasienTrackerModel` | ULID `PasienTrackerId`; name/date-of-birth snapshot; `VisitDate` plus `StartPeriod`/`LastPeriod`; list of `PasienTrackerEventType`. Factories exist only for Booking and Registration. |
| Tracker timeline persistence | `PasienTrackerRepo`, `PasienTrackerEventDal`, `BILRG_PasienTracker*` | Header plus detail rows with `StartPeriod`/`LastPeriod`. Events use PK `(PasienTrackerId, NoUrut)`, `ORDER BY EventDate, NoUrut`, and insert-only repo path; later-stage evidence and cancellation retention remain gaps (F-02/F-08). |
| Queue session | `AntrianModel`, `AntrianFactory`, `BILRG_Antrian` | Date/time-bounded header with `SequenceTag`, description, and explicit `ServicePointCode` column (dual-read fallback from tag when empty). `UX_BILRG_Antrian_SequenceTag` enforces lookup uniqueness. |
| Queue entry | `AntrianEntryModel`, `BILRG_AntrianEntry` | Composite `(AntrianId, NoUrut)` identity, optional sentinel tracker `"-"`, name snapshot, `CreatedAt`, `ServedAt`, `DoneAt`, and status. |
| Booking workflow | `BookingCreateHandler`, `BookingCreateFromHidokHandler` | Creates booking, new tracker with `BOOKING` event, physician queue entry, legacy map slot, and queue number. Local flow performs duplicate rejection; external flow does not. |
| Registration by booking | `RegJalanByBookingHandler` | Reuses the booking queue row and therefore retains its `PasienTrackerId`, changes queue reference from Booking to Registration, and immediately calls `Serve`. It does not append tracker evidence. |
| Walk-in registration | `RegJalanCreateHandler` | Creates a new registration and tracker, creates a physician queue entry with `REG` reference, and immediately calls `Serve`. It does not create an admission queue entry. |
| Consultation completion | `QueSelesaiPeriksaHandler` | Calls `Done` on the selected queue entry and saves the queue. No Medical Chart evidence is appended. Controller does not await the command. |
| Legacy slot map | `AntrianMapModel`, `ta_no_antrian_map*` | Pre-seeded/automatic physician slots keyed by doctor, service, date, time, and number. Stores patient/source references but not Tracker identity or service milestones. |
| Legacy/external synchronization | `AddAntrianEmrByBookingService`, `AddAntrianEmrByRegService`, `AntrianConsistencyRepairWorker` | Sends queue number and booking/registration identity to external EMR; later repair rewrites `BILRG_AntrianEntry.ReffId/ReffDesc` from booking to registration. No `TrackerId` is exchanged. |
| Pharmacy | Farinv `AntrianModel`, `FARIN_Antrian*` | Independent queue with `Taken/Assigned/Prepared/Delivered/Cancelled` lifecycle and `RegId`, but no Tracker link or drug-sale/handover application orchestration. |
| HTTP API | Bilreg `AntrianController` and Farinv `AntrianController` | Quota/number/list/get/complete/repair in Bilreg; add/list in Farinv. No Tracker timeline or Journey Resolution API. |

The current ownership is split: Bilreg `AntrianModel` behaves like the proposed Queue Session, `PasienTrackerModel` behaves like a partial Patient Tracker, legacy `AntrianMapModel` still allocates physician slots, and Farinv owns a separate pharmacy queue. This split is operationally understandable but does not yet have the compatibility boundary required by the canonical domain.

## 4. Domain Coverage Matrix

### 4.1 Capabilities, aggregates, entities, and value objects

| Requirement | Status | Primary evidence and conclusion |
|---|---|---|
| Logical Patient Journey Tracking | Partially Implemented | `PasienTrackerModel.Create` creates a stable-looking ULID, but only one `VisitDate` exists and change/cancel paths replace or delete identity. See F-01/F-02. |
| Operational Evidence Timeline | Partially Implemented | Booking/Registration events can be stored, but only those two event descriptions are produced; ordering, immutability, and later-stage evidence are missing/conflicting. See F-03/F-08. |
| Service Point Queue Coordination | Partially Implemented | Queue header/entries/numbers/statuses exist. Anonymous intake is inactive, identification is defective, lifecycle guards are incomplete, and Service Point identity is implicit. See F-05/F-06. |
| Journey Candidate Resolution | Not Implemented | Repository filtering is used only as duplicate rejection. There is no candidate query with evidence, operator selection, or association command. See F-04. |
| Operational Time Interpretation | Implemented Differently | Milestone columns exist, but registration calls `Serve` on the physician entry and there are no canonical duration projections. See F-07/F-09. |
| Patient Tracker aggregate | Partially Implemented | Root, Tracking Period columns, and append-only event persistence exist; continuity rules and later evidence are still incomplete (F-01/F-02/F-08). |
| Queue Session aggregate | Partially Implemented | Root, entries, and explicit `ServicePointCode` persistence exist; aggregate invariants and natural session uniqueness policy remain incomplete (F-06/F-12 residual). |
| Patient Tracker entity | Partially Implemented | ID/person/event collection exist; period and continuity rules do not. |
| Tracker Event entity | Partially Implemented | Description/reference/time/order fields exist; PK `(PasienTrackerId, NoUrut)` and chronological reads are durable; only Booking/Reg events are produced (F-08). |
| Queue Session entity | Partially Implemented | Stable ID/date/start/end and `ServicePointCode` column exist; `SequenceTag` remains operational lookup key; natural tuple uniqueness deferred (F-12 residual). |
| Queue Entry entity | Partially Implemented | Number/tracker/name/milestones/state exist; identification and state guards conflict with the domain. |
| TrackerId | Partially Implemented | ULID and queue reference exist, but replacement/deletion workflows break logical stability. |
| Person Identity Snapshot | Implemented | Name and date of birth are copied into `PersonType` and tracker persistence. Matching policy remains ambiguous. |
| Tracking Period | Partially Implemented | Model, DTO, SQL columns, and overlap list filter exist; workflow continuity and extension in all paths remain incomplete (F-01). |
| Evidence Reference | Partially Implemented | `ReffId` exists on tracker events; there is no typed provenance or source/queue-reference distinction. |
| Queue Evidence Reference | Not Implemented | No composite value, serialization, or event contributor uses `(Queue Session identity, Queue Number)`. |
| Service Point | Partially Implemented | `ServicePointType` and `BILRG_Antrian.ServicePointCode` exist with dual-read fallback from `SequenceTag`; no catalogue table/`IServicePointDal` implementation. |
| Patient or Visitor role | Partially Implemented | Booking/registration contracts accept identity evidence and queue numbers are returned, but no anonymous admission-number contract is active. |
| Journey Resolution Operator role | Not Implemented | No candidate review, selection/new decision, accountable actor field, or association command exists. |
| Service Point Operator role | Partially Implemented | Completion can be invoked, but service start is automated by registration rather than recognized by the responsible operator. |
| Source Activity Owner role | Partially Implemented | Booking/Registration supply IDs/times; Medical Chart, prescription, drug-sale, and handover contributors are absent. |

### 4.2 Rule-by-rule coverage

| Rule | Requirement summary | Status | Traceable evidence / gap |
|---|---|---|---|
| BR-TRK-001 | One tracker represents one logical journey | Partially Implemented | Structural root exists; `RegJalanUbahKunjunganHandler` creates a new tracker for an existing registration (`RegJalanUbahKunjunganCmd.cs:130-136`). |
| BR-TRK-002 | Stable TrackerId and one identity snapshot | Partially Implemented | `PasienTrackerModel.cs:12-18,49-54`; continuity is broken by replacement/deletion paths. |
| BR-TRK-003 | TrackerId is logical, not physical | Implemented | Code uses it only as a logical/persistence key; no physical tracker/location representation was found. |
| BR-TRK-004 | Established tracker has at least one event | Partially Implemented | Booking/Reg factories append one event (`PasienTrackerModel.cs:21-39`), but the public constructor/default allow an empty list. |
| BR-TRK-005 | StartPeriod equals first event date | Not Implemented | No `StartPeriod` in model, DTO, SQL, repository, or API. |
| BR-TRK-006 | Booking LastPeriod is max(StartPeriod, VisitDate) | Implemented Differently | Only `VisitDate` is stored (`PasienTrackerModel.cs:53`; `BILRG_PasienTracker.sql:5`); no max rule exists. |
| BR-TRK-007 | Non-future LastPeriod starts at StartPeriod | Not Implemented | No Tracking Period behavior. |
| BR-TRK-008 | Later evidence extends but never shrinks LastPeriod | Not Implemented | `AddEvent` only appends to `_listEvent` and never updates a period (`PasienTrackerModel.cs:58-67`). |
| BR-TRK-009 | RegId/source IDs never replace TrackerId | Partially Implemented | Booking registration keeps the queue row's tracker, but visit-change creates a new tracker and cancellation/deletion removes it. Taksaka rewrites only queue `ReffId`, not `TrackerId`. |
| BR-TRK-010 | Event has tracker, description, reference, OccurredAt | Partially Implemented | Aggregate ownership plus `PasienTrackerEventType`; default `DateTime` is accepted and event does not independently carry root identity. |
| BR-TRK-011 | Evidence only from accountable source or queue | Partially Implemented | Active calls originate from Booking/Reg, but public `AddEvent(string,string,DateTime)` accepts arbitrary evidence without source validation. |
| BR-TRK-012 | Use primary source stable identity when it exists | Partially Implemented | Booking/Reg factories use their IDs. No Medical Chart, drug-sale, or other source integration exists. |
| BR-TRK-013 | Queue evidence may be used before source transaction exists | Not Implemented | No Queue Evidence Reference; dormant handler proposed `"-"` instead. |
| BR-TRK-014 | Start and Done may use different reference kinds | Not Implemented | No start/done tracker evidence orchestration exists. |
| BR-TRK-015 | Evidence reference is provenance, not lifecycle correlation | Partially Implemented | Per-event `ReffId` exists, but queue repair mutates a general `ReffId` from Booking to Reg and no provenance type documents the distinction. |
| BR-TRK-016 | Description is free text and non-behavioral | Implemented | `EventName` is a string; no Tracker search or transition branches on it. |
| BR-TRK-017 | Events are cumulative and append-only | Implemented Differently | `PasienTrackerRepo.SaveChanges` computes deletions/changes (`:28-35`); DAL exposes update/delete; source deletion removes tracker roots. See F-03. |
| BR-TRK-018 | Chronological timeline with deterministic tie order | Implemented | `PasienTrackerEventDal.ListData` orders by `EventDate, NoUrut`; PK is `(PasienTrackerId, NoUrut)` (`BILRG_PasienTrackerEvent.sql`, M2 alter). |
| BR-TRK-019 | OccurredAt preserves source business time | Partially Implemented | Main Booking/Reg use cases pass `ITglJamProvider.Now`; domain defaults allow `DateTime.MinValue`, and no late-association workflow is present. |
| BR-TRK-020 | Candidate search uses name, DOB, relevant date | Partially Implemented | `ListData(Periode, tglLahir)` filters VisitDate/DOB; Booking handler applies exact normalized name. It is a duplicate guard, not a candidate use case. |
| BR-TRK-021 | Relevant date is inclusive within Start/Last | Partially Implemented | `PasienTrackerDal.ListData` uses overlap `StartPeriod <= @Tgl2 AND LastPeriod >= @Tgl1`; candidate workflow still incomplete (F-04). |
| BR-TRK-022 | Return all candidates and never silently choose | Implemented Differently | Repository can return multiple rows, but Booking uses `FirstOrDefault` to reject and no evidence-bearing candidate response exists. |
| BR-TRK-023 | Accountable operator selects among candidates | Not Implemented | No command, actor, audit, API, or UI contract. |
| BR-TRK-024 | Equal demographics do not merge/prove identity | Implemented Differently | No automatic merge occurs, but `ThrowExceptionIfTrackerExists` treats equal name/DOB/VisitDate as a duplicate unless `IsForceDuplicatedTracker` is set (`BookingCreateCmd.cs:102-104,152-163`). |
| BR-TRK-025 | Establish new tracker when no candidate applies | Partially Implemented | Booking and walk-in can create new trackers, but not as an outcome of Journey Resolution. |
| BR-TRK-026 | Session has one Service Point/date/start/end | Partially Implemented | Date/start/end exist. Service Point is encoded in `SequenceTag`/description; physician queues use doctor/schedule instead of an explicit Service Point field. |
| BR-TRK-027 | Queue number unique within session, reusable elsewhere | Partially Implemented | SQL composite PK enforces uniqueness per `AntrianId`; aggregate `AddEntry(int,...)` has no duplicate guard and relies on persistence failure. |
| BR-TRK-028 | Entry belongs to exactly one session and number | Implemented | Aggregate ownership and `(AntrianId, NoUrut)` persistence key match this requirement. |
| BR-TRK-029 | Anonymous entry may be created | Partially Implemented | `AntrianModel.AddEntry(DateTime)` creates sentinel identity, but the only application handler is entirely commented out and no API invokes it. |
| BR-TRK-030 | Booking/identified source starts with one tracker | Implemented | Both Booking create handlers pass the newly created tracker into the physician queue entry. Walk-in registration does the same. |
| BR-TRK-031 | Anonymous entry becomes identified only after resolution | Implemented Differently | No resolution path; `AssignPasien` changes only `Visitor`, not `Tracker` (`AntrianEntryModel.cs:51-56`). |
| BR-TRK-032 | Tracker can join many sessions; entry has at most one tracker | Partially Implemented | One scalar `PasienTrackerId` exists per entry, but no active multi-Service-Point journey orchestration exists. |
| BR-TRK-033 | CreatedAt is create/reserve time, not universally arrival | Implemented | Booking uses booking `occurredAt` when reserving a future physician entry. No physical-arrival claim is stored. |
| BR-TRK-034 | Booking entry may predate its session | Implemented | Booking queue session is on `TglBerobat`; entry `CreatedAt` is booking creation time (`BookingCreateCmd.cs:65,116`). |
| BR-TRK-035 | New entry Waiting; Created present; Served/Done absent | Partially Implemented | Status/sentinel milestones match logically, but `createdAt = default` can persist `DateTime.MinValue`. |
| BR-TRK-036 | Only Waiting may enter In Service | Implemented Differently | `Serve` unconditionally changes status and time, with no current-state or chronology check (`AntrianEntryModel.cs:58-62`). |
| BR-TRK-037 | Only In Service may become Done | Implemented Differently | `Done` checks only the ServedAt sentinel and ordering, not `AntrianStatus == InService`. |
| BR-TRK-038 | Served >= Created; Done >= Served | Partially Implemented | Done-before-Served is guarded; Served-before-Created is not. Code also rejects equal Served/Done although the rule only prohibits precedence. |
| BR-TRK-039 | Done is final | Implemented Differently | `Serve` can be called again after Done and return it to In Service; queue rows can also be physically removed by cancellation workflows. |
| BR-TRK-040 | Registration wait = admission Created→Served | Not Implemented | No active admission queue workflow or metric. |
| BR-TRK-041 | Registration duration = admission Served→Done | Not Implemented | Registration does not complete an admission queue entry. |
| BR-TRK-042 | Post-reg consultation wait = registration Done→physician Served | Implemented Differently | Registration calls `Serve` on the physician entry immediately (`RegJalanByBookingCmd.cs:170-175`; `RegJalanWalkInCommand.cs:250-253`). |
| BR-TRK-043 | Consultation duration = physician Served→Done | Implemented Differently | Milestones exist and `QueSelesaiPeriksaHandler` calls Done, but Served has registration-time semantics and no duration projection exists. |
| BR-TRK-044 | Pharmacy queue creation is not physical arrival | Partially Implemented | Farinv has `TakenAt`, not physical location, but it is a separate queue with no Tracker journey integration. |
| BR-TRK-045 | Confirmed drug sale establishes pharmacy ServedAt/evidence | Not Implemented | No Penjualan use case invokes Farinv queue transitions; no Bilreg Tracker evidence integration exists. |
| BR-TRK-046 | Pharmacy duration = Served→Done | Not Implemented | Farinv has Assigned/Prepared/Delivered timestamps, not canonical Served/Done mapping or Tracker metric. |
| BR-TRK-047 | Do not infer physical movement/location | Implemented | No Tracker physical position/travel model or inferred movement logic was found. |
| BR-TRK-048 | Source activity remains authoritative | Implemented | Tracker factories copy source evidence and do not validate or rewrite Booking/Reg content. Source cancellation remains owned by source workflows. |
| BR-TRK-049 | Preserve supplied source time/reference | Partially Implemented | Creation paths supply source IDs/times, but Tracker persistence permits mutation/deletion and later source evidence is not recorded. |
| BR-TRK-050 | Tracker evidence does not replace compliance audit | Implemented | `AuditLog` is separate; for example registration cancellation creates an audit after operational changes (`RegJalanBatalCmd.cs:101-114`). |

### 4.3 State machines and lifecycle coverage

| Lifecycle/transition | Status | Evidence |
|---|---|---|
| First evidence establishes Tracker | Partially Implemented | Booking/Reg factories append one event, but period derivation and invariant enforcement are absent. |
| Later evidence appends and may extend period | Not Implemented | No application path appends later journey evidence; no LastPeriod behavior. |
| Tracker has no Open/Closed/Completed state | Implemented | No Tracker status exists. Physical deletion nevertheless terminates visibility. |
| Tracker Event becomes immutable after recording | Implemented Differently | Record type is immutable in memory, but repository/DAL update/delete it. |
| Queue Entry: Waiting → In Service | Implemented Differently | Transition exists without state/chronology guards and is invoked at the wrong business milestone. |
| Queue Entry: In Service → Done | Partially Implemented | Transition/time guard exists, but status is not checked and endpoint dispatch is not awaited. |
| Queue Entry: Done → no next state | Implemented Differently | `Serve` permits Done → In Service. |
| Anonymous → Identified | Implemented Differently | `AssignPasien` does not assign TrackerId; no Journey Resolution orchestration. |
| Identified entries never pass through Anonymous | Implemented | Booking/walk-in entries are constructed with a tracker. |

### 4.4 Stable domain facts

Section 9 does not mandate an event bus, and `docs/ENGINEERING.md` prefers direct orchestration. The status below concerns whether each fact is represented and available to orchestration/integration, not whether a generic event framework exists.

| Stable fact | Status | Evidence / conclusion |
|---|---|---|
| Patient Tracker Established | Partially Implemented | Fact occurs in Booking/Reg factories but is not represented explicitly for consumers. |
| Tracker Evidence Recorded | Partially Implemented | Event rows are written, but no explicit fact/handler and evidence is mutable. |
| Tracking Period Extended | Not Implemented | No Tracking Period. |
| Journey Candidate Selected | Not Implemented | No selection workflow. |
| Queue Session Established | Partially Implemented | Queue header is saved, without explicit fact or robust Service Point identity. |
| Queue Entry Created | Partially Implemented | Rows are created; no explicit fact or anonymous intake endpoint. |
| Queue Entry Identified | Not Implemented | Identification behavior is defective and unused. |
| Queue Service Started | Partially Implemented | `Serve` changes state, but at registration rather than physician service recognition and without an explicit integration fact. |
| Queue Service Completed | Partially Implemented | `Done` changes state; no Tracker evidence/fact is produced. |
| Booking Queue Number Assigned | Partially Implemented | Booking is assigned a number in the same transaction, then separately sent to legacy EMR; no explicit fact or reliable delivery record exists. |

### 4.5 Workflow coverage

| Workflow | Status | Actual behavior |
|---|---|---|
| 10.1 Establish journey from Booking | Partially Implemented | Tracker/event/session/identified entry/number exist. Start/LastPeriod do not; queue-session uniqueness is not protected; external delivery is after commit and unverified. |
| 10.2 Anonymous admission queue | Not Implemented | Only an unused model method and commented-out handler exist. |
| 10.3 Resolve journey and perform Registration | Implemented Differently | Walk-in skips anonymous intake/candidate resolution, creates a new tracker, and starts the physician queue. Booking registration reuses the tracker key but appends no evidence and also starts physician service. |
| 10.4 Physician consultation | Partially Implemented | One physician queue entry can be completed, but service start occurs during registration and start/done Tracker evidence is absent. |
| 10.5 Establish downstream pharmacy work | Not Implemented | Farinv queue is independent and no consultation/prescription integration creates a same-Tracker entry. |
| 10.6 Perform/complete pharmacy service | Not Implemented | Domain methods exist in Farinv, but no application handlers invoke them from drug sale/handover and no Tracker events are written. |
| 10.7 Resolve multiple candidates | Not Implemented | No candidate evidence or operator selection. |
| 10.8 Interpret outpatient timeline | Not Implemented | No projection/calculation API; recorded timestamps have conflicting semantics. |

### 4.6 Integration coverage

| Source/integration described by the domain | Status | Traceable evidence / gap |
|---|---|---|
| Booking → Tracker and physician queue | Partially Implemented | Both Booking handlers create a Tracker, `BOOKING` evidence, and identified physician entry; Tracking Period and reliable `Booking Queue Number Assigned` fact are absent. |
| Admission queue → Tracker evidence | Not Implemented | No active anonymous admission handler, identification, Queue Evidence Reference, check-in evidence, or registration-start evidence. |
| Registration → existing journey | Implemented Differently | Booking registration retains the queue TrackerId but appends no evidence; walk-in creates a new Tracker and starts the physician queue; change-of-visit replaces identity. |
| Medical Chart/consultation → Tracker | Not Implemented | No Medical Chart reference is used by Tracker code; `QueSelesaiPeriksaHandler` only marks the queue Done. |
| Prescription/completed consultation → pharmacy queue | Not Implemented | No Bilreg-to-Farinv queue contract or same-Tracker entry creation exists. |
| Drug sale confirmation → pharmacy service start | Not Implemented | Farinv Penjualan does not call queue transitions and no Tracker service-start evidence is written. |
| Medicine handover → pharmacy completion | Not Implemented | Farinv `DeliverSlot` exists only as an unused domain method; no Tracker completion evidence. |
| Queue milestones → Patient Tracker | Not Implemented | `Serve`/`Done` update only `BILRG_AntrianEntry`; they do not append Tracker Events or extend a period. |
| Compliance audit separation | Implemented | AuditLog remains a separate cross-cutting mechanism; Tracker events are not used as compliance audit rows. |
| Legacy desktop/EMR projection | Partially Implemented | Active dual writes and external calls preserve number/source fields, but no TrackerId is exchanged and delivery success is not persisted. |
| Taksaka consistency repair | Implemented Differently | Repairs `BOK`/`REG` queue correlation only; it does not repair canonical journey evidence. |

## 5. Detailed Gap Findings

This section contains confirmed implementation gaps. Ambiguities, missing runtime evidence, and non-correctness refactoring are separated in section 7.

### F-01 — Tracking Period is absent

- **Domain requirement:** BR-TRK-005 through BR-TRK-008; Patient Tracker aggregate owns `StartPeriod` and `LastPeriod` and extends only the upper boundary.
- **Status:** Not Implemented / Implemented Differently.
- **Evidence:** `PasienTrackerModel` exposes only `VisitDate` (`PasienTrackerModel.cs:49-54`). `PasienTrackerDto`, `PasienTrackerDal`, and `BILRG_PasienTracker.sql` persist/query only `VisitDate`. `AddEvent` does not alter a period.
- **Gap/conflict:** Booking occurrence truth is not retained as `StartPeriod`, non-booking trackers have no derived period, and later evidence cannot extend candidate eligibility. `VisitDate BETWEEN` is not equivalent to inclusive `StartPeriod <= date <= LastPeriod`.
- **Business impact:** Cross-date booking and later-stage continuity cannot be resolved reliably. A valid journey can be omitted or an unrelated visit-date row can be considered.
- **Legacy compatibility impact:** Adding period columns can be additive and need not alter `VisitDate`, which may remain a legacy/read-model compatibility field during transition. Backfill rules must profile historical event data first.
- **Recommended direction:** Add explicit period state to the aggregate/persistence; derive Start from first evidence, initial Last from the domain rules, and extend Last atomically when appending evidence. Preserve `VisitDate` until all legacy consumers are mapped.
- **Severity:** High.

### F-02 — Stable journey identity is broken by change and deletion workflows

- **Domain requirement:** BR-TRK-001, BR-TRK-002, BR-TRK-009; evidence grows under one stable TrackerId.
- **Status:** Implemented Differently.
- **Evidence:** `RegJalanUbahKunjunganHandler` creates `PasienTrackerModel.Create(reg, occurredAt)` for an existing registration (`RegJalanUbahKunjunganCmd.cs:130-136`), deletes/removes the old queue context, and saves the new tracker (`:303-318,365-372`). `DeleteBookingWorkflow` removes the queue entry and deletes the tracker (`DeleteBookingWorkflow.cs:72-87`). `RegJalanBatalHandler.VoidAntrian` does the same (`RegJalanBatalCmd.cs:207-215`).
- **Gap/conflict:** A visit change can split one logical journey across TrackerIds. Cancellation/deletion makes the root unavailable instead of retaining cumulative operational evidence.
- **Business impact:** Journey history, provenance, downstream links, and longitudinal timing can become orphaned or misleading. A stored queue entry or external reference may point to a deleted/non-current tracker.
- **Legacy compatibility impact:** Legacy map workflows require releasing/voiding slots. That requirement does not require deleting Tracker evidence. A compatibility operation can clear/void the legacy slot while retaining Tracker history.
- **Recommended direction:** Keep TrackerId stable across registration creation, reschedule, and source-ID assignment. Define cancellation/reschedule evidence before changing these paths; separate legacy slot release from Tracker retention.
- **Severity:** Critical.

### F-03 — Tracker Event persistence contradicts append-only history and contains destructive defects

- **Domain requirement:** BR-TRK-017 through BR-TRK-019; immutable cumulative evidence with chronological and deterministic ordering.
- **Status:** Implemented Differently.
- **Evidence:** `PasienTrackerRepo.SaveChanges` computes `deletedItems` and `changedItems` and executes delete/update (`PasienTrackerRepo.cs:28-35`). `PasienTrackerEventDal.Update` has `WHERE PasienTrackerId = @PasienTrackerId` only, so one changed item targets all rows (`PasienTrackerEventDal.cs:51-63`). Aggregate delete executes a `SELECT`, not `DELETE` (`:131-146`), leaving orphan events after header deletion. Event reads have no `ORDER BY`. `BILRG_PasienTrackerEvent.sql` uses PK `(PasienTrackerId, EventDate)` although `NoUrut` is the deterministic order.
- **Gap/conflict:** Existing evidence can be changed/deleted, an update can overwrite every event, aggregate delete can orphan events, equal timestamps cannot be recorded, and query order is nondeterministic.
- **Business impact:** This can corrupt or make inaccessible the very evidence Tracker is intended to preserve. It also makes reconciliation unreliable.
- **Legacy compatibility impact:** Existing rows and references must be preserved. Any key/index migration needs a data-profile step for duplicates/orphans and a non-destructive migration; legacy `ReffId` values should not be rewritten.
- **Recommended direction:** Make event persistence insert-only for normal operations; introduce a stable event/order identity that permits equal `OccurredAt`; order by `OccurredAt` plus recorded order; remove mutation paths from the aggregate repository. Handle existing orphans through an explicit migration/reconciliation decision, not automatic deletion.
- **Severity:** Critical.

### F-04 — Journey Candidate Resolution is replaced by duplicate rejection

- **Domain requirement:** BR-TRK-020 through BR-TRK-025 and workflow 10.7.
- **Status:** Implemented Differently / Not Implemented.
- **Evidence:** `IPasienTrackerRepo.ListData(Periode, DateOnly)` returns only header views. `PasienTrackerDal.ListData` filters a single `VisitDate` range; `PasienTrackerRepo` then filters date of birth. `BookingCreateHandler.ThrowExceptionIfTrackerExists` applies normalized exact name and takes the first match, throwing unless `IsForceDuplicatedTracker` is true (`BookingCreateCmd.cs:102-104,152-163`). No candidate query/selection command/API was found.
- **Gap/conflict:** Candidates are neither computed with Tracking Period nor presented with evidence. An operator cannot select an existing journey; the system only blocks or force-creates.
- **Business impact:** The same journey can be duplicated, while different people with equal demographics can be blocked. Anonymous entries cannot be responsibly resolved.
- **Legacy compatibility impact:** Legacy desktop may still rely on duplicate warnings. Preserve that warning as a compatibility/UI signal, but it must not become identity proof or automatic merge.
- **Recommended direction:** Implement a read projection returning every eligible candidate and its evidence; add explicit select/new commands with accountable actor metadata; retain duplicate warning only as advisory behavior.
- **Severity:** Critical.

### F-05 — Anonymous admission intake and identification are not operational

- **Domain requirement:** BR-TRK-029, BR-TRK-031, lifecycle 8.4, workflows 10.2–10.3.
- **Status:** Partially Implemented / Implemented Differently.
- **Evidence:** `AntrianModel.AddEntry(DateTime)` can create a sentinel anonymous row, but `QueGetNoAntrianByServicePointCmd.cs` is entirely commented out. `AntrianEntryModel.AssignPasien` sets only `Visitor`; it never sets `Tracker` (`AntrianEntryModel.cs:51-56`). `AntrianRepo.AreEqual` also omits `PasienTrackerId` from change detection (`AntrianRepo.cs:110-119`).
- **Gap/conflict:** No active use case creates the admission queue entry, and the apparent identification behavior cannot associate a TrackerId.
- **Business impact:** Continuity from anonymous number to identified journey—the central bridge in the domain—cannot occur.
- **Legacy compatibility impact:** Legacy admission-number allocation and display rules need discovery. A new admission Queue Session can coexist with physician `AntrianMap` slots if identifiers and counters are kept distinct.
- **Recommended direction:** Add active anonymous intake and resolution commands. Identification must atomically set TrackerId and snapshot under the Queue Session aggregate, then append the required queue evidence to the selected tracker in one application transaction/orchestration boundary.
- **Severity:** Critical.

### F-06 — Queue aggregate does not protect identity and service lifecycle invariants

- **Domain requirement:** BR-TRK-026 through BR-TRK-039.
- **Status:** Partially Implemented / Implemented Differently.
- **Evidence:** `AntrianModel.AddEntry(int,...)` does not check duplicate numbers; SQL PK is the final guard. Queue header has no `ServicePointCode` column. `Serve` has no state or time checks; `Done` does not check `InService`; `Serve` can reopen Done (`AntrianEntryModel.cs:58-73`). Public constructors can reconstruct invalid combinations. The focused test run confirms default `Serve()` then `Done()` fails.
- **Gap/conflict:** Consistency is enforced late or not at all. The aggregate permits illegal transitions and timestamp sequences.
- **Business impact:** Queue state and durations can be invalid even when rows persist successfully; concurrent number allocation can fail unpredictably.
- **Legacy compatibility impact:** Legacy number maps may remain the transitional allocator. Their assigned number must be reserved atomically in the canonical Queue Session instead of bypassing its uniqueness guard.
- **Recommended direction:** Make Service Point explicit; enforce duplicate prevention and allowed transitions in the aggregate; require valid business times; add optimistic/concurrency protection for queue creation and number assignment.
- **Severity:** High.

### F-07 — Registration and consultation milestones are semantically conflated

- **Domain requirement:** BR-TRK-040 through BR-TRK-043; workflows 10.3, 10.4, and 10.8.
- **Status:** Implemented Differently.
- **Evidence:** Booking registration changes the physician queue reference to `REG` and calls `Serve` immediately (`RegJalanByBookingCmd.cs:170-175`). Walk-in creates a physician queue entry and calls `Serve` in the registration transaction (`RegJalanWalkInCommand.cs:250-257`). No admission Queue Session/entry is created or completed. `QueSelesaiPeriksaHandler` later completes the same physician row.
- **Gap/conflict:** Physician `ServedAt` represents registration time, not physician-recognized service start. Registration Created/Served/Done milestones do not exist separately.
- **Business impact:** Registration wait, registration duration, post-registration consultation wait, and consultation duration are all absent or materially wrong. Operational performance reporting would misstate patient experience.
- **Legacy compatibility impact:** Existing desktop dashboards may expect a booking/registration queue row to become active at registration. Preserve that projection if needed, but do not reuse its timestamp as canonical Tracker consultation evidence.
- **Recommended direction:** Introduce a distinct admission Queue Session/entry; complete it when Registration completes; keep the booking-created physician entry Waiting until physician service actually begins. Project legacy states separately from canonical milestones during transition.
- **Severity:** Critical.

### F-08 — Evidence timeline stops after Booking or initial Registration

- **Domain requirement:** BR-TRK-010 through BR-TRK-019; workflows 10.3–10.6.
- **Status:** Partially Implemented.
- **Evidence:** Global call search found active `AddEvent` only inside `PasienTrackerModel.Create(Booking)` and `.Create(Reg)`. Booking-based registration never loads/saves the tracker; consultation completion does not append Medical Chart evidence; no pharmacy code references Bilreg Tracker.
- **Gap/conflict:** Check-in, registration-start, registration-done, consultation-start, consultation-done, pharmacy-start, and pharmacy-done evidence are absent. Queue Evidence Reference is absent.
- **Business impact:** The timeline cannot explain a journey or expose evidence for human resolution. LastPeriod could not be extended even if added.
- **Legacy compatibility impact:** Existing `EventName`/`ReffId` columns can carry transitional events but length/key/order limits must be fixed first. Legacy event wording must remain display-only.
- **Recommended direction:** After fixing append-only persistence and reference representation, append each workflow milestone from its accountable source time/reference. Do not infer unobserved movement.
- **Severity:** High.

### F-09 — Pharmacy queue is a separate, unintegrated lifecycle

- **Domain requirement:** BR-TRK-044 through BR-TRK-046; workflows 10.5–10.6.
- **Status:** Not Implemented in Tracker; partially implemented differently in Farinv.
- **Evidence:** Farinv `AntrianModel`/`AntrianEntryModel` use `Taken`, `Assigned`, `Prepared`, `Delivered`, and `Cancelled` with `RegId`, not TrackerId. `QueAddAntrian*` only adds entries. No application caller invokes `AssignSlot`, `PrepareSlot`, `DeliverSlot`, or `CancelSlot`; `PenjualanModel` queue imports/code are commented. `FARIN_AntrianEntry` has no TrackerId or canonical Served/Done fields.
- **Gap/conflict:** Consultation/prescription does not create a same-Tracker pharmacy entry. Confirmed drug sale does not establish canonical ServedAt/service-start evidence, and handover does not complete Tracker service evidence.
- **Business impact:** V1 outpatient journey continuity ends before pharmacy, and pharmacy service duration cannot be interpreted canonically.
- **Legacy compatibility impact:** Farinv's richer preparation/delivery lifecycle may be operationally necessary and should not be collapsed. A compatibility mapping should identify which Farinv fact corresponds to Tracker Served/Done while retaining Farinv states.
- **Recommended direction:** Add an explicit cross-context contract carrying TrackerId, Queue Session/number, business time, and source reference. Map confirmed sale to Tracker service start and medicine handover to completion without treating queue creation as arrival.
- **Severity:** High.

### F-10 — HTTP/application contracts do not expose core Tracker operations

- **Domain requirement:** Journey Resolution, timeline evidence, anonymous intake/identification, queue service lifecycle, and time interpretation must be operable.
- **Status:** Closed for core Tracker/Queue intent contracts (2026-07-21). Operational time interpretation projection API (workflow 10.8 / BR-TRK-040..046) remains out of scope for this finding.
- **Evidence (after fix):** `PasienTrackerController` exposes `GET {pasienTrackerId}` (`TrkGetQuery` with chronological `ListEvent`), `GET candidates`, `POST resolve/select`, `POST resolve/new`, and `POST pharmacy/evidence`. `AntrianController` exposes `POST anonymous-intake`, `PATCH mulaiPeriksa`, and `PATCH selesaiPeriksa`; all MediatR dispatches are awaited. `QueMulaiPeriksaCmd` / `QueSelesaiPeriksaCmd` return `QueAntrianEntryActionResponse` with `AntrianId`, `NoUrut`, `PasienTrackerId`, and `Status`. `QueGetAntrianResponse` includes `PasienTrackerId`, `ServedAt`, and `DoneAt`.
- **Gap/conflict (resolved):** Core domain actions are now invocable and observable through additive HTTP contracts. Service start/complete responses carry stable queue-entry identity and TrackerId instead of opaque status strings.
- **Remaining note:** Dedicated duration/timeline interpretation endpoints (BR-TRK-040..046) are tracked under workflow 10.8 / implementation sequence step 9, not this HTTP-contract gap.
- **Recommended direction:** Define additive Tracker/Queue contracts around domain intents and evidence. Await all commands and return TrackerId plus stable queue-entry identity where required. **Applied.**
- **Severity:** High (closed).

### F-11 — Domain facts and cross-context reliability are absent

- **Domain requirement:** Section 9 stable facts and workflows involving Booking, Registration, Medical Chart, pharmacy sale, and Queue Entry evidence.
- **Status:** Partially Implemented (Slice 1 applied 2026-07-21).
- **Evidence:** EMR antrian publication for BookingCreate and RegJalan (WalkIn/ByBooking) now enqueues `BILRG_EmrAntrianOutboundQueue` in the same transaction as local writes; `EmrAntrianOutboundProcessor` delivers via `IEmrAntrianOutboundIntegration` with HTTP success/failure inspection, retry, and worklist API (`EmrAntrianOutboundController`). Direct post-commit fire-and-forget removed from those handlers. Pharmacy Farinv→Bilreg evidence, remaining section-9 fact contracts, and full reconciliation reporting remain open.
- **Gap/conflict (remaining):** Pharmacy and consultation facts still lack durable cross-context delivery; Taksaka repair remains legacy projection-only (see F-14).
- **Business impact (mitigated for Slice 1):** Booking/Reg queue-number publication to EMR is no longer silent; failed/empty-config deliveries are visible and retryable.
- **Legacy compatibility impact:** Taksaka `AntrianConsistencyRepairWorker` retained until outbox parity is proven.
- **Recommended direction:** Slice 1 applied for EMR Booking/Reg. Next: pharmacy outbox (Slice 2) and explicit fact contracts for remaining section-9 items.
- **Severity:** High (Slice 1 closed for EMR Booking/Reg; pharmacy/full facts remain).

### F-12 — Persistence shape cannot faithfully represent Queue Session and deterministic evidence

- **Domain requirement:** Explicit Service Point/session identity, queue number uniqueness, optional Tracker association, deterministic event order, and durable milestones.
- **Status:** Mostly Implemented / **Closed in source** (2026-07-21 verification).
- **Evidence (after fix):**
  - `BILRG_Antrian.ServicePointCode` in green DDL plus `BILRG_Antrian_M1_ServicePointCode_Alter.sql` (backfill from `SequenceTag`, `UX_BILRG_Antrian_SequenceTag` with duplicate guard).
  - `BILRG_PasienTracker.StartPeriod`/`LastPeriod` in green DDL plus `BILRG_PasienTracker_M1_TrackingPeriod_Alter.sql` (backfill from events/`VisitDate`, period index).
  - Event PK `(PasienTrackerId, NoUrut)` in green DDL plus `BILRG_PasienTrackerEvent_M2_AppendOnlyKey_Alter.sql` (profile orphans/duplicates before PK change); `ReffId` widened to `VARCHAR(40)` via M3.
  - Domain/DAL mapping: `AntrianDto`/`AntrianDal` read-write `ServicePointCode`; `PasienTrackerDto`/`PasienTrackerDal` read-write periods with overlap filter; `PasienTrackerEventDal` insert-only with `ORDER BY EventDate, NoUrut`; `PasienTrackerRepo` append-only events and `DeleteEntity` throws.
  - Alter scripts M1–M3 included in `Bilreg.SqlDb.sqlproj` as `<None>` (project-wide pattern).
  - Bounded test run (29 tests, filter Antrian/PasienTracker persistence shape): **29 passed** after `PasienTrackerDalTest.EnsureTrackingPeriodColumns` helper aligned test DB with additive columns.
- **Gap/conflict (remaining, deferred):**
  - No unique constraint on natural session tuple `(ServicePointCode, AntrianDate, StartTime, EndTime)` — only `UX_BILRG_Antrian_SequenceTag`; policy still open in domain §7.1.5.
  - `BILRG_AntrianEntry.PasienTrackerId` remains NOT NULL with sentinel `"-"`/`""` rather than NULL.
  - `AntrianDal.ListData(DateTime)` view list omits `ServicePointCode` (still derivable from `SequenceTag`).
  - Unused `RegId` on `BILRG_PasienTracker` header retained; not mapped in DTO.
  - **Deployed schema:** Unable to Verify — M1–M3 must be applied and profiled per environment before production constraints take effect.
- **Business impact (mitigated for source):** Queue Session identity, Tracking Period, and deterministic event ordering are now durable persistence facts in code and green DDL; residual items affect policy clarity and list projections, not the core additive shape.
- **Legacy compatibility impact:** Additive columns/indexes preserve `VisitDate`, `SequenceTag`, and sentinel `PasienTrackerId` conventions; dual-read fallback from `SequenceTag` when `ServicePointCode` is empty.
- **Recommended direction:** **Applied** — additive migration with `ServicePointCode`, `StartPeriod`/`LastPeriod`, event identity/order, and `SequenceTag` uniqueness with profile guards. **Deferred:** natural session tuple UX, nullable `PasienTrackerId`, concurrency token, production deploy verification.
- **Severity:** High (closed in source; deploy/residual policy tracked separately).

### F-13 — Canonical Tracker ownership conflicts with legacy slot-map ownership

- **Domain requirement:** Queue Session aggregate alone assigns numbers and owns queue identity/milestones; source activities remain authoritative for their transactions.
- **Status:** **Closed in source** (2026-07-21) for transitional compatibility layer; production cutover to `QueueSession` authority remains deferred.
- **Evidence (after fix):**
  - [`TRACKER-COMPATIBILITY.md`](TRACKER-COMPATIBILITY.md) documents authority map, identity crosswalk, and non-equivalence of `IsTerpakai` vs Waiting/In Service/Done.
  - `IQueueNumberCompatibilityAdapter` / `QueueNumberCompatibilityAdapter` centralize reserve → project → release; handlers no longer dual-write inline.
  - `QueueNumber:Authority` config (default `LegacyMap`); `QueueSession` branch throws until cutover is approved.
  - `AntrianMapDetilModel.Void` + `IsFreeSlot()` align legacy slot release with allocator predicate.
  - Reg-by-booking projects map `ReffId` Booking→Reg via adapter; Hidok uses `AcceptExternalNumber` (queue-only exception).
- **Gap/conflict (resolved):** Queue-number authority is now explicit and transactional; legacy map is a documented projection during transition.
- **Remaining note:** Enable `QueueSession` allocator after parity metrics; optional TrackerId on map/EMR payload is follow-up. See [`tracker-f13-implementation-report.md`](tracker-f13-implementation-report.md).
- **Recommended direction:** **Applied** — compatibility adapter/crosswalk, atomic reserve+project, no `IsTerpakai`↔status mapping, deliberate future cutover flag.
- **Severity:** Medium (closed in source; cutover monitoring tracked separately).

### F-14 — Consistency repair rewrites correlation state but does not repair Tracker truth

- **Domain requirement:** Evidence Reference preserves provenance; source times/references are not rewritten; consistency repair must not manufacture evidence.
- **Status:** Implemented Differently.
- **Evidence:** `AntrianConsistencyRepairQueries.Repair` updates `BILRG_AntrianEntry.ReffId` from booking to registration and `ReffDesc` to `REG`. It does not load TrackerId, append Registration evidence, set milestones, or extend a period. The Bilreg `FixOutstandingReference` path performs the same mutation.
- **Gap/conflict:** The worker repairs a legacy/read correlation but can be mistaken for journey/evidence repair. It loses the booking reference from that queue row and leaves the Tracker timeline unchanged.
- **Business impact:** Queue queries may look consistent while the authoritative journey evidence remains incomplete.
- **Legacy compatibility impact:** The worker is likely operationally necessary for current EMR bridges and should not be removed without replacement.
- **Recommended direction:** Classify it explicitly as a legacy projection repair. Add separate idempotent Tracker reconciliation that preserves both Booking and Registration evidence and never infers missing business time.
- **Severity:** Medium.

### F-15 — Tests neither cover the canonical rules nor currently form a green safety net

- **Domain requirement:** Every aggregate invariant, transition, workflow, persistence rule, and compatibility path needs executable evidence before rollout.
- **Status:** Partially Implemented.
- **Evidence:** `PasienTrackerModelTest` has two creation/default tests; `AntrianEntryModelTest` has two Done tests; no tests cover Start/LastPeriod, candidate resolution, anonymous identification, illegal transitions, equal timestamps, stable TrackerId, consultation timing, pharmacy, or end-to-end workflows. The focused run produced 5 failures out of 19.
- **Gap/conflict:** Existing tests are sparse and stale, and DAL tests do not assert affected-row counts or the erroneous aggregate delete/update predicates.
- **Business impact:** High-risk identity/time/evidence changes cannot be safely verified, and current regressions can be hidden among unrelated tests.
- **Legacy compatibility impact:** There are model/DAL tests for `AntrianMap`, but no acceptance tests proving dual-write parity across booking, registration, cancellation, and repair.
- **Recommended direction:** Build a domain-rule test suite first, then persistence and orchestration tests, followed by compatibility acceptance tests against both new and legacy projections.
- **Severity:** Medium.

## 6. Legacy Compatibility Assessment

### 6.1 What must be preserved unless operational evidence says otherwise

The following are confirmed active compatibility surfaces:

- physician queue number and booking/registration source reference in `ta_no_antrian_map`;
- doctor, service, schedule date/time, flag, and `IsTerpakai` slot semantics in `AntrianMapModel`;
- booking/registration `NoAntrian` values returned by Bilreg APIs;
- external EMR payloads from `AddAntrianEmrByBookingService` and `AddAntrianEmrByRegService`;
- the `BOK` → `REG` repair behavior used by Bilreg and Taksaka; and
- current web queries that join queue `ReffId == RegId` and filter `ReffDesc == "REG"`.

The desktop application's direct SQL dependencies are not present in this repository and are Unable to Verify.

### 6.2 Semantic differences that are not automatically defects

| Legacy concern | Tracker domain concern | Assessment |
|---|---|---|
| `AntrianMap` pre-allocates/flags physician slots | Queue Session assigns numbers and tracks service | Different responsibilities. Keep as compatibility projection/allocator until authority cutover. |
| Legacy row has patient/source ID but no TrackerId | Queue Entry associates one logical TrackerId | Requires a crosswalk/additive field or adapter; do not replace RegId/BookingId with TrackerId. |
| Legacy `IsTerpakai`/flags | Waiting/In Service/Done | Not equivalent state machines. Do not map by numeric/status coincidence. |
| Booking reference later rewritten to Registration | Tracker Events preserve each source evidence item | Retain legacy repair if needed, but append separate immutable Booking and Registration evidence. |
| Cancellation clears/releases a slot | Tracker evidence is cumulative | Slot release can continue; deletion of Tracker history should not. Target cancellation evidence needs a domain decision. |
| Farinv has Taken/Assigned/Prepared/Delivered/Cancelled | Tracker pharmacy needs Created/Served/Done | Preserve Farinv workflow and map only accountable milestones to Tracker. |

### 6.3 Required compatibility strategy

An implementation should use a transitional compatibility layer rather than a schema/name replacement:

1. Keep `TrackerId` as the new logical journey identity; never encode it into legacy RegId/BookingId fields.
2. Maintain explicit mappings among TrackerId, BookingId, RegId, Queue Session/number, legacy map ID/number, and pharmacy queue identity.
3. Choose one transactional queue-number reservation authority per flow. Until cutover, the adapter may reserve through the legacy map, but it must atomically reject duplicates in the canonical Queue Session.
4. Project canonical state to legacy fields without treating legacy fields as evidence truth.
5. Preserve legacy repair/reconciliation until dual-write parity metrics and rollback procedures are proven.
6. Use additive database/API changes and data backfill; do not repurpose existing columns with different semantics.

### 6.4 Data migration implications

Before backfill, profile:

- tracker headers without events and event rows without headers;
- multiple events at identical timestamps that could not currently coexist;
- queue entries with `PasienTrackerId = '-'`, missing tracker headers, or inconsistent `ReffDesc`;
- multiple queue headers for the same derived Service Point/date/interval;
- visit-change flows that created multiple tracker IDs for one RegId;
- booking entries rewritten to Reg without corresponding tracker evidence; and
- legacy/Farinv queue rows that can be safely cross-walked to a logical journey.

Where source business time is unavailable, migration must mark the conclusion as unavailable/reconciled rather than substitute current time. This follows BR-TRK-019 and BR-TRK-049.

## 7. Domain Ambiguities or Decisions Required

These items are not classified as confirmed code defects because the canonical document does not specify enough target behavior.

### 7.1 Business decisions required

1. **Cancellation, no-show, and booking deletion.** The document defines Done as final but does not define cancellation/no-show states or whether a never-served queue entry remains Waiting, becomes void, or is retained with separate evidence.
2. **Reschedule/change-of-visit.** LastPeriod never moves backward, but the expected evidence and Queue Entry behavior when doctor/service/VisitDate changes is not stated.
3. **Person snapshot correction.** It is unclear whether name/date-of-birth snapshots are immutable historical evidence or correctable, and how corrections affect candidate search.
4. **Candidate name matching policy.** “Available name” does not specify exact, normalized, phonetic, or fuzzy matching, ranking, or minimum evidence display.
5. **Queue Session natural uniqueness.** The document requires Service Point/date/start/end but does not state whether overlapping or duplicate sessions for the same tuple are valid.
6. **Queue Evidence Reference representation.** The composite meaning is clear; its serialization, maximum length, versioning, and collision rules are not.
7. **Recorded-order identity and concurrency.** Deterministic order for equal timestamps is required, but no event ID/sequence allocation/idempotency rule is defined.
8. **Time standard.** Time zone, offset storage, precision, and conversion at cross-system boundaries are not defined.
9. **Domain-fact delivery contract.** Section 9 defines stable facts but not which are internal only, which cross bounded contexts, or their idempotency/delivery guarantees.
10. **Pharmacy milestone mapping.** Farinv has preparation/delivery states. The accountable action for Tracker `DoneAt` should be confirmed as medicine handover, including partial delivery/cancellation behavior.

### 7.2 Missing evidence

- Actual production/staging DDL and migration history.
- Row counts and data-quality distribution for all Tracker/new queue/legacy map tables.
- Desktop application's direct table/API dependencies.
- Runtime enablement/schedule and operational success rate of `AntrianConsistencyRepairWorker`.
- External EMR contract behavior on failed/duplicate booking/registration calls.
- Frontend callers of current Bilreg/Farinv queue endpoints.
- Medical Chart and prescription/drug-sale transaction contracts suitable for evidence references.

Conclusions depending on these facts should remain **Unable to Verify** until runtime or consumer evidence is collected.

### 7.3 Optional improvements, not business-correctness gaps

- Renaming Indonesian implementation types to English domain terms.
- Moving files into a new physical bounded-context folder before behavior is corrected.
- Introducing a generic event bus instead of explicit orchestration.
- Replacing explicit DAL/repository code with an ORM.
- Removing legacy tables solely for architectural cleanliness.

These may be considered later but are not required to close the canonical business gaps.

### 7.4 Refactoring opportunities separated from correctness

- Replace repeated queue-header lookup code with a shared resolver only after the Service Point/session identity policy is decided.
- Replace sentinel dates/IDs with clearer internal option types while maintaining compatible persistence mapping.
- Correct typos such as `SquenceTag` and `AntaianEntryOutStandingDto` when contracts are already being versioned.
- Consolidate duplicated Bilreg/Taksaka outstanding-reference SQL after its legacy role is explicitly documented.

## 8. Recommended Implementation Sequence

This sequence prioritizes business correctness and dependency order. It is an analysis recommendation, **not authorization to implement**.

1. **Resolve policy decisions and inventory live compatibility.** Decide cancellation/reschedule, matching, session uniqueness, evidence-reference format, event ordering/idempotency, time standard, and pharmacy milestones. Verify desktop/API consumers and profile live data.
2. **Define the compatibility contract and authority map.** Document canonical versus legacy ownership for TrackerId, queue number, queue state, Booking/Reg references, `AntrianMap`, external EMR, Taksaka repair, and Farinv pharmacy.
3. **Harden evidence persistence additively.** Add Start/LastPeriod, stable event identity/recorded order, explicit Service Point identity, and safe indexes/constraints. Backfill only from verified source evidence. Make normal Tracker event persistence insert-only and add reconciliation reporting for existing orphans/corruption.
4. **Correct aggregate invariants and tests.** Enforce first evidence, stable TrackerId, LastPeriod extension, queue-number uniqueness, optional identity, allowed transitions, chronology, and Done finality. Establish green rule-level tests before orchestration changes.
5. **Implement Journey Candidate Resolution and anonymous admission.** Add evidence-bearing candidate queries, accountable select/new commands, anonymous admission Queue Sessions, and atomic identification plus queue evidence.
6. **Correct Booking and Registration orchestration.** Reuse the same TrackerId; preserve booking occurrence and future queue reservation; use a separate admission entry for registration milestones; leave physician entry Waiting until actual consultation start. Keep legacy map/EMR projections through the compatibility adapter.
7. **Implement consultation evidence.** Start physician service only on accountable physician interaction; complete it on consultation completion; append Queue and Medical Chart evidence with source business time.
8. **Integrate pharmacy across contexts.** Carry TrackerId into a pharmacy queue crosswalk/contract; create the entry from prescription/completed consultation; map confirmed drug sale to ServedAt and medicine handover to DoneAt while preserving Farinv's internal states.
9. **Build canonical timeline and time projections/APIs.** Return chronologically ordered evidence and calculate only the intervals defined in BR-TRK-040 through BR-TRK-046. Do not expose inferred movement.
10. **Add reliable facts, reconciliation, acceptance tests, and phased rollout.** Publish or directly orchestrate the section-9 facts with idempotency; test dual-write parity, failures, concurrency, cancellations, and repairs; monitor before retiring any legacy path.

## 9. Final Assessment

The codebase has useful structural foundations:

- a logical ULID TrackerId rather than a physical token;
- person identity snapshots;
- Booking and Registration source references;
- queue session/entry persistence with number and milestones;
- a Booking-created future physician queue entry with booking-time `CreatedAt`;
- a three-state queue enum; and
- deliberate legacy dual-write/repair mechanisms.

Those foundations do not yet provide the business behavior defined by `TRACKER-DOMAIN.md`. The current system is best classified as:

| Dimension | Final status |
|---|---|
| Logical journey continuity | Partially Implemented, with critical identity conflicts |
| Evidence timeline | Partially Implemented, with critical append-only/persistence conflicts |
| Queue coordination | Partially Implemented, with lifecycle and ownership gaps |
| Journey Resolution | Not Implemented |
| Operational time interpretation | Implemented Differently / Not Implemented |
| Booking integration | Partially Implemented |
| Registration integration | Implemented Differently |
| Consultation integration | Partially Implemented |
| Pharmacy integration | Not Implemented in Tracker |
| Legacy compatibility | Active and operationally significant; requires a compatibility layer and staged migration |
| Verification confidence | High for source behavior; Unable to Verify for deployed data, external consumers, and runtime operations |

The business-correctness dependency is clear: **first preserve stable identity and immutable evidence, then implement accountable resolution and correct queue milestones, then connect consultation/pharmacy and derive operational time.** Building metrics or new UI on the current timestamps would institutionalize incorrect semantics.
