# ARTIFACTS.md — Documentation index

**Read this file first** when using AI prompts or onboarding. All canonical paths are listed here with full paths — do not reference bare `DOMAIN.md`, `AGENT.md`, or `WORKFLOW.md` without a context prefix.

---

## Prompt recipe (ordered)

1. [`docs/INSTRUCTION.md`](INSTRUCTION.md) — global engineering stance
2. **Bounded context** — [`docs/contexts/{context}/`](contexts/) (see table below). IGD UI/integration: also [`docs/contexts/igd/igd-04-api-contract.md`](contexts/igd/igd-04-api-contract.md); ops/DBA: [`docs/contexts/igd/igd-05-runbook.md`](contexts/igd/igd-05-runbook.md). Tarif: [`docs/contexts/tarif/tarif-01-context.md`](contexts/tarif/tarif-01-context.md) through `tarif-07-admin-workflow.md`. Tata Rekening: [`docs/contexts/TataRekening/01-context.md`](contexts/TataRekening/01-context.md) through [`04-sop.md`](contexts/TataRekening/04-sop.md) and `SOP-TR-01` … `SOP-TR-10`.
3. **Global standards** (as needed):
   - [`docs/ENGINEERING.md`](ENGINEERING.md) — layers, repository, domain events philosophy
   - [`docs/DATABASE.md`](DATABASE.md) — SQL, tables, audit columns
   - [`docs/NAMING.md`](NAMING.md) — naming conventions
   - [`docs/WORKFLOW.md`](WORKFLOW.md) — operational UX / queue / workspace (global only)
4. **Skills** (implementation generation):
   - [`docs/skills/feature-model-generation.md`](skills/feature-model-generation.md)
   - [`docs/skills/feature-persistence-generation.md`](skills/feature-persistence-generation.md)
   - [`docs/skills/use-case-generation.md`](skills/use-case-generation.md)
5. **Cross-cutting concepts** — [`docs/concepts/operational-events.md`](concepts/operational-events.md) when "event" behavior is ambiguous

---

## Global standards (`docs/`)

| Path | Purpose |
|------|---------|
| `docs/INSTRUCTION.md` | Global engineering instruction |
| `docs/ENGINEERING.md` | Architecture philosophy |
| `docs/DATABASE.md` | SQL persistence standard |
| `docs/NAMING.md` | Naming standard |
| `docs/WORKFLOW.md` | Operational workflow UX standard (not Lab-specific) |
| `docs/business-date-implementation-review.md` | Business Date implementation, clock audit, migrations, and verification |
| `docs/skills/*.md` | AI generation skills |

---

## Bounded contexts (`docs/contexts/`)

### Laboratory (`docs/contexts/lab/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/lab/lab-domain.md` | Aggregates, states, boundaries |
| `docs/contexts/lab/lab-agent.md` | Invariants, forbidden design |
| `docs/contexts/lab/lab-implementation-plan.md` | Implementation slices and references |
| `docs/contexts/lab/lab-workflow.md` | LWF workflow lifecycle and rules |
| `docs/contexts/lab/lab-integration.md` | EMR, REG, BIL, OWR integration |
| `docs/contexts/lab/lab-test-scenarios.md` | Test scenarios |
| `docs/contexts/lab/LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md` | Master component, test definition, Tarif resolution blueprint |
| `docs/contexts/lab/LAB_MASTER_TEST_ALIGNMENT.md` | Phase-0 alignment report (naming, contracts, readiness) |
| `docs/contexts/lab/LAB_API_CONTRACT.md` | Frontend/API integration contract |

### IGD (`docs/contexts/igd/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/igd/igd-01-context.md` | IGD visit — why (business context, operational flow) |
| `docs/contexts/igd/igd-02-domain.md` | IGD visit — what (aggregates, rules, state) |
| `docs/contexts/igd/igd-03-design.md` | IGD visit — how (architecture, persistence) |
| `docs/contexts/igd/igd-04-api-contract.md` | IGD visit — integration (frontend/API contract) |
| `docs/contexts/igd/igd-05-runbook.md` | IGD visit — operation (runbook, troubleshooting, recovery) |

### Admisi — Rawat Inap (`docs/contexts/admisi-ranap/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/admisi-ranap/admisi-ranap-domain.md` | Admisi Ranap — what (aggregates, rules, lifecycles) |
| `docs/contexts/admisi-ranap/admisi-ranap-architecture.md` | Admisi Ranap — how (use cases, repos, API, integration, ADRs) |
| `docs/contexts/admisi-ranap/admisi-ranap-registration-orchestration.md` | Admission + legacy Registration orchestration, shared `RegId`, mapping rules, and AI implementation guidance |
| `docs/contexts/admisi-ranap/admisi-ranap-coordinated-cancellation-design.md` | Coordinated Admission/Registration cancellation — eligibility, state transitions, source restoration, transaction, concurrency, and tests |
| `docs/contexts/admisi-ranap/ta-reg-inap-persistence-contract.md` | `ta_reg_inap` / `RegInapModel` persistence contract for inpatient registration — ownership, fields, DAL defects, readiness |
| `docs/contexts/admisi-ranap/admisi-ranap-step-4c-verification-report.md` | Step 4C — end-to-end + DB verification report (Opname/Reservation, RegInap, rollback, follow-ups) |
| `docs/contexts/admisi-ranap/admisi-ranap-persistent-workspace-capability-matrix.md` | Persistent workspace — backend capability matrix (Ready / Propose / Defer); Phase 2 WL-by-regId Ready |
| `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md` | Admisi Ranap — phased backend implementation plan |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-0-implementation-report.md` | Phase 0 — folder scaffolding, conventions, build verification |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-1-implementation-report.md` | Phase 1 — domain aggregates, state machines, invariant tests |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-2-implementation-report.md` | Phase 2 — SQL, DTO/DAL, repositories, worklist projection |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-3-implementation-report.md` | Phase 3 — MediatR use cases, cross-aggregate orchestration, handler tests |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-4-implementation-report.md` | Phase 4 — REST API controllers, 19 endpoints, JSendOk, baseline auth |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-5-implementation-report.md` | Phase 5 — integration gateways (Doctor, Patient, Ward), handler refactor, adapter tests |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-7-implementation-report.md` | Phase 7 — hardening, rollout, audit logging, E2E workflow validation |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b1-journey-resolver-implementation-report.md` | Release 1 Phase B1 — journey contracts, JourneyId, integrity, pure stage resolver |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b2-journey-projection-implementation-report.md` | Release 1 Phase B2 — journey read DAL/projection, SQL stage parity, list/detail, integration tests |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b2.1-journey-projection-hardening-report.md` | Release 1 Phase B2.1 — batch list hydration, IT isolation, allowed-action billing gate, volume evidence |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b3.1-staging-verification-report.md` | Release 1 Phase B3.1 — staging verification result, API/DB prerequisites, contract evidence, and performance gate |
| `docs/contexts/admisi-ranap/admisi-ranap-runbook.md` | Admisi Ranap — operation (deployment, validation, rollback) |
| `docs/contexts/admisi-ranap/admisi-ranap-rollout-checklist.md` | Admisi Ranap — production rollout gates |

Persistent-workspace Phase 2 close-out summary (frontend docs tree): `c012_myhospital_web/docs/modules/admisi-ranap/persistent-workspace-phase-2-implementation-summary.md`

### Admisi — Rawat Jalan (`docs/contexts/admisi-rajal/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/admisi-rajal/admisi-rajal-domain.md` | Admisi Rajal — canonical business truth, boundaries, foundational capabilities, and Work List ownership |
| `docs/contexts/admisi-rajal/admisi-rajal-domain-id.md` | Admisi Rajal — Bahasa Indonesia semantic companion |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-0-implementation-summary.md` | High-density worklist Phase 0 — approved contracts, baseline evidence, and rollout safety |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-1-implementation-summary.md` | High-density worklist Phase 1 — active filtering, paging metadata, compatibility, diagnostics, and verification |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-2-implementation-summary.md` | High-density worklist Phase 2 — dense read-only workspace, virtual rows, compatibility, and verification |
| `docs/contexts/admisi-rajal/jadwal-praktek-investigation-report.md` | Codebase investigation — `JadwalPraktekType` usage, consumers, and update paths |
| `docs/contexts/admisi-rajal/jadwal-praktek-harian-architecture-analysis.md` | Architecture analysis — daily schedule (`JadwalPraktekHarian`) design, resolver, migration roadmap |
| `docs/contexts/admisi-rajal/jadwal-praktek-harian-deployment-manual.md` | Deployment & operations — SQL order, feature toggle rollout, validation, rollback |
| `docs/contexts/admisi-rajal/adr/ADR-001-runtime-effective-schedule.md` | ADR — runtime `JadwalPraktekEffective` and resolver as single authority |
| `docs/contexts/admisi-rajal/adr/ADR-002-manual-override-independence.md` | ADR — `Source = MANUAL` daily rows independent from template |
| `docs/contexts/admisi-rajal/adr/ADR-003-booking-schedule-references.md` | ADR — dual nullable schedule IDs on booking |

### Tarif (`docs/contexts/tarif/`)

Charge-context tariff master, operational projection (`NilaiTarif`), and komponen breakdown. **Phases 1–5 LIVE:** policy domain, persistence, publish engine, admin HTTP, migration controls (`TarifMigrationController`). Live ops use staged import/publish authority per `Tarif:Mode` → `BILRG_*`.

| Path | Purpose |
|------|---------|
| `docs/contexts/tarif/tarif-01-context.md` | Tarif — why (business context, scope, operational intent) |
| `docs/contexts/tarif/tarif-02-domain.md` | Tarif — what (aggregates, invariants, projection vs history) |
| `docs/contexts/tarif/tarif-03-design.md` | Tarif — how (ChargeContext layering, import, migration) |
| `docs/contexts/tarif/tarif-04-api-contract.md` | Tarif — integration (live vs proposed HTTP) |
| `docs/contexts/tarif/tarif-05-runbook.md` | Tarif — operation (import, validation, recovery) |
| `docs/contexts/tarif/tarif-06-publish-engine.md` | Tarif — publish orchestration (Phase 3 LIVE) |
| `docs/contexts/tarif/tarif-07-admin-workflow.md` | Tarif — policy admin HTTP workflow (Phase 4 LIVE) |
| `docs/contexts/tarif/tarif-09-rollout-checklist.md` | Tarif — M0–M3 rollout gates (Phase 5 LIVE) |
| `docs/contexts/tarif/tarif-10-migration-strategy.md` | Tarif — import → publish migration strategy (Phase 5 LIVE) |
| `docs/contexts/tarif/TARIF_IMPLEMENTATION_PLAN.md` | Tarif — phased implementation plan (policy, publish, migration) |
| `docs/contexts/tarif/TARIF_PHASE0_REPORT.md` | Phase 0 hardening — what shipped, migration order, rollback |
| `docs/contexts/tarif/TARIF_PHASE1_ALIGNMENT_REPORT.md` | Phase 1 retroactive alignment — domain vs persistence audit |
| `docs/contexts/tarif/TARIF_PHASE2_REPORT.md` | Phase 2 persistence — tables, repos, projection writer, migration |
| `docs/contexts/tarif/TARIF_PHASE3_REFACTOR_REPORT.md` | Phase 3 publish simplification — removed validator/mapper abstractions |
| `docs/tarif/tarif-codebase-retrieval-report.md` | Codebase evidence / gap analysis (retrieval, not stewardship artifact) |

### Tata Rekening (`docs/contexts/TataRekening/`)

Patient financial responsibility domain (registration-scoped lifecycle, TrsBill charges, Financial Projection). Legacy persistence: `ta_trs_billing`, `ta_trs_billing2`, `ta_registrasi3`.

| Path | Purpose |
|------|---------|
| `docs/contexts/TataRekening/01-context.md` | Tata Rekening — why (business context, bounded contexts, lifecycle) |
| `docs/contexts/TataRekening/02-domain.md` | Tata Rekening — what (aggregates, invariants, domain events) |
| `docs/contexts/TataRekening/03-design.md` | Tata Rekening — how (architecture, persistence, integration) |
| `docs/contexts/TataRekening/04-sop.md` | Tata Rekening — workflow overview and SOP index |
| `docs/contexts/TataRekening/SOP-TR-01 — Open Tata Rekening.md` | SOP — Open Tata Rekening |
| `docs/contexts/TataRekening/SOP-TR-02 — Close Bill.md` | SOP — Close Bill |
| `docs/contexts/TataRekening/SOP-TR-03 — Merge Billing.md` | SOP — Merge Billing |
| `docs/contexts/TataRekening/SOP-TR-04 — Financial Verification.md` | SOP — Financial Verification |
| `docs/contexts/TataRekening/SOP-TR-05 — Financial Adjustment.md` | SOP — Financial Adjustment |
| `docs/contexts/TataRekening/SOP-TR-06 — Financial Responsibility Allocation.md` | SOP — Financial Responsibility Allocation |
| `docs/contexts/TataRekening/SOP-TR-07 — Finalize Financial Responsibility.md` | SOP — Finalize Financial Responsibility |
| `docs/contexts/TataRekening/SOP-TR-08 — Cancel Finalization.md` | SOP — Cancel Finalization |
| `docs/contexts/TataRekening/SOP-TR-09 — Reopen Billing.md` | SOP — Reopen Billing |
| `docs/contexts/TataRekening/SOP-TR-10 — Settlement Initiation.md` | SOP — Settlement Initiation |
| `docs/contexts/TataRekening/tata-rekening-domain-gap-analysis-report.md` | Domain gap analysis (implementation vs artifact) |

### Taksaka (`docs/contexts/taksaka/` + `docs/taksaka/`)

Background processing platform — job orchestration, worker plugins, operator console.

| Path | Purpose |
|------|---------|
| `docs/contexts/taksaka/taksaka-01-domain.md` | Taksaka — what (domain vision, ubiquitous language, health model) |
| `docs/contexts/taksaka/taksaka-02-architecture.md` | Taksaka — how (engine, plugins, SignalR, operator console) |
| `docs/taksaka/README.md` | **Operations docs index** — deployment, config, runbook, administrator guide |
| `docs/taksaka/01-deployment-guide.md` | Deploy, build, publish, first startup |
| `docs/taksaka/02-configuration-reference.md` | All configuration keys from code |
| `docs/taksaka/03-operator-manual.md` | Operator procedures (start/stop, logs, monitoring) |
| `docs/taksaka/04-troubleshooting.md` | Troubleshooting guide |
| `docs/taksaka/05-architecture-runtime.md` | Runtime architecture (implemented vs stub) |
| `docs/taksaka/06-plugin-development-guide.md` | Worker plugin development |
| `docs/taksaka/07-production-checklist.md` | Production deployment checklist |
| `docs/taksaka/08-operations-runbook.md` | Daily/weekly/monthly runbook, DR, upgrade |
| `docs/taksaka/09-administrator-guide.md` | Practical guide for hospital EDP (Bahasa Indonesia) |

### CPOE (`docs/contexts/cpoe/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/cpoe/CPOE-DOMAIN.md` | CPOE — what (clinical-order business truth, aggregates, rules, lifecycles) |
| `docs/contexts/cpoe/CPOE-DOMAIN-ID.md` | CPOE — versi Bahasa Indonesia (istilah domain standar tetap dipertahankan) |
| `docs/contexts/cpoe/CPOE-ARCHITECTURE.md` | CPOE — how (module boundaries, use cases, persistence, API, integrations, security, infrastructure, ADRs) |

### RUANG RANAP Operational Management (`docs/contexts/bangsal/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/bangsal/RNA-DOMAIN.md` | RNA — what (accommodation and RUANG RANAP service-execution business truth) |
| `docs/contexts/bangsal/RNA-DOMAIN-ID.md` | RNA — versi Bahasa Indonesia (semantic companion; canonical domain identities retained) |
| `docs/contexts/bangsal/RNA-ARCHITECTURE.md` | RNA — how (codebase-grounded modules, use cases, persistence, integrations, consistency, security, ADRs, and gaps) |
| `docs/contexts/bangsal/CPOE-RNA-INTEGRATION.md` | CPOE ↔ RNA — order dispatch/change, Ward coordination requests, execution facts/corrections, exceptional accountability, reliability, and reconciliation semantic contract |
| `docs/contexts/bangsal/RNA-TATA-REKENING-INTEGRATION.md` | RNA ↔ Tata Rekening — execution facts, corrections, lifecycle query, acknowledgement, reliability, and reconciliation semantic contract |
| `docs/contexts/bangsal/rna-sop/RNA-SOP-INDEX.md` | RNA — approved accommodation and service-execution SOP index |
| `docs/contexts/bangsal/rna-sop/RNA-SOP-GAPS.md` | RNA — unresolved policy decisions that gate architecture and implementation |
| `docs/contexts/bangsal/rna-sop/RNA-CPOE-DOMAIN-GAP-ANALYSIS.md` | RNA SOP versus simplified CPOE domain — alignment, cross-context gaps, and recommended change order |

### Patient Tracker (`docs/contexts/pasien-tracker/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/pasien-tracker/TRACKER-DOMAIN.md` | Patient Tracker — canonical business truth for Patient Journey continuity and Service Point queues |
| `docs/contexts/pasien-tracker/TRACKER-DOMAIN-ID.md` | Patient Tracker — Bahasa Indonesia semantic companion |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-DOMAIN.md` | Patient Tracker — canonical Admission Queue Operations business specification for Service Points, Loket, Kiosks, Queue Labels, and Queue Calls |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md` | Patient Tracker Admission Queue Operations — Bahasa Indonesia semantic companion |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-SOP.md` | Patient Tracker — canonical target operational procedure for admission queue intake, calling, service, Journey Resolution, and completion |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-SOP-ID.md` | Patient Tracker Admission Queue Operations SOP — Bahasa Indonesia operational companion |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md` | Patient Tracker — codebase-grounded current-to-target architecture for admission queue resources, calls, projections, Kiosk, Queue Display, security, and delivery |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ARCHITECTURE-RECONCILIATION.md` | Patient Tracker — post-R-14 architecture verdict, implementation-state matrix, remaining delivery plan, and first-slice prompt |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md` | Patient Tracker — backend-only phased implementation plan for remaining Admission Queue work (external clients summarized out of scope) |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-EXTERNAL-CLIENTS-IMPLEMENTATION-PLAN.md` | Patient Tracker — Part 2 external clients plan (C0–C4 closed) |
| `docs/contexts/pasien-tracker/kiosk-queue-display-web.md` | Patient Tracker — kiosk/display path-based IIS deploy + monorepo architecture decision (feeds C2/C3) |
| `docs/contexts/pasien-tracker/tracker-c2-kiosk-implementation-report.md` | Patient Tracker — C2 kiosk monorepo path boot, intake, and local print/reprint closure |
| `docs/contexts/pasien-tracker/tracker-c3-queue-display-implementation-report.md` | Patient Tracker — C3 queue display snapshot-first, SignalR RefreshHint, TTS, and version.json idle reload |
| `docs/contexts/pasien-tracker/tracker-c4-integration-deployment-implementation-report.md` | Patient Tracker — C4 cross-client E2E, IIS packaging, and runbook client-section closure |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-IIS-CLIENT-CUTOVER-CHECKLIST.md` | Patient Tracker — Kiosk/Display IIS packaging and cutover Go/No-Go gates (C4) |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-KIOSK-DISPLAY-DEPLOY-ID.md` | Patient Tracker — Buku panduan step-by-step deploy & konfigurasi Kiosk + Queue Display (Bahasa Indonesia; implementor / IT RS) |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase1-slice1-verification-report.md` | Patient Tracker — Phase 1 / Slice 1 disposable migrated-SQL operational gate, race matrix results, and session one-winner reload evidence |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase2-officer-contracts-implementation-report.md` | Patient Tracker — Phase 2 officer contracts, ReasonCode catalog boundary, Admisi enriched worklist, legacy inventory |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase3-booking-assistance-implementation-report.md` | Patient Tracker — Phase 3 booking-assistance receive-side closure and external HiDok consumer contract |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase4-signalr-refresh-implementation-report.md` | Patient Tracker — Phase 4 SignalR refresh-hint hub/adapter behind IAdmissionQueueRefreshPublisher |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase5-rollout-implementation-report.md` | Patient Tracker — Phase 5 backend integration rollout package, preflight status, go/no-go closure |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-RUNBOOK.md` | Patient Tracker — Admission Queue integration migrate/seed/config/smoke/rollback runbook + Officer/Kiosk/Display client sections (C4) |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ROLLOUT-CHECKLIST.md` | Patient Tracker — Admission Queue integration go/no-go and legacy disposition checklist |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-GAP-ANALYSIS.md` | Patient Tracker — artifact/codebase alignment and implementation-readiness verdict for Admission Queue Operations |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-ROADMAP.md` | Patient Tracker — prioritized, dependency-ordered roadmap for closing Admission Queue implementation gaps |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-R04-LOKET-CLAIM-CONTRACT.md` | Patient Tracker — accepted cross-session active Loket claim state, persistence, concurrency, and test contract |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r00-baseline-implementation-report.md` | Patient Tracker — R-00 accepted baseline commit and focused verification evidence |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r01-cas-persistence-implementation-report.md` | Patient Tracker — R-01 compare-and-set admission lifecycle persistence |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r05a-sequencer-implementation-report.md` | Patient Tracker — R-05A bounded, non-cycling, concurrency-safe Admission Queue sequencer implementation |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r06-service-point-implementation-report.md` | Patient Tracker — R-06 authoritative Admission Service Points and immutable Queue Label snapshots |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r07-projections-implementation-report.md` | Patient Tracker — R-07 queue-only officer worklist and current Loket recovery projections |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r08-transitions-implementation-report.md` | Patient Tracker — R-08 explicit Queue operational transition matrix, CAS coordination, and Redirect |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r09-registration-outcome-implementation-report.md` | Patient Tracker — R-09 immutable final Registration outcomes and atomic Queue completion |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-API-V1.md` | Patient Tracker — R-10 versioned Admission Queue API, errors, security boundary, consumer inventory, and compatibility |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r11-hidok-assistance-implementation-report.md` | Patient Tracker — R-11 HiDok Booking assistance ensure API, business deduplication, and external integration boundary |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r12a-workstation-identity-implementation-report.md` | Patient Tracker — R-12A configured workstation-to-Loket identity validation and R-12B operations boundary |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r13-audit-operational-validation-report.md` | Patient Tracker — R-13 audit-schema alignment, persistence classification, and continuous operational-validation gate |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r14-deferred-operational-policies.md` | Patient Tracker — R-14 explicit-action guardrail and intentionally deferred operational policies |
| `docs/contexts/pasien-tracker/TRACKER-COMPATIBILITY.md` | Patient Tracker — legacy AntrianMap ↔ Queue Session authority map, crosswalk, and adapter contract (F-13) |
| `docs/contexts/pasien-tracker/tracker-codebase-gap-report.md` | Patient Tracker — implementation gap report vs canonical domain |
| `docs/contexts/pasien-tracker/tracker-f01-implementation-report.md` | Patient Tracker — F-01 closed: explicit Tracking Period |
| `docs/contexts/pasien-tracker/tracker-f02-implementation-report.md` | Patient Tracker — F-02 closed: stable TrackerId across visit change / cancel |
| `docs/contexts/pasien-tracker/tracker-f03-implementation-report.md` | Patient Tracker — F-03 closed: append-only Tracker Event persistence (PK NoUrut, insert-only DAL/repo) |
| `docs/contexts/pasien-tracker/tracker-f04-implementation-report.md` | Patient Tracker — F-04 closed: Journey Candidate Resolution (soft-duplicate Booking, candidate query, select/new) |
| `docs/contexts/pasien-tracker/tracker-f05-implementation-report.md` | Patient Tracker — F-05 closed: anonymous admission intake & atomic identify + Queue Evidence Reference |
| `docs/contexts/pasien-tracker/tracker-f06-implementation-report.md` | Patient Tracker — F-06 closed: Queue aggregate Service Point, number uniqueness, Serve/Done lifecycle invariants |
| `docs/contexts/pasien-tracker/tracker-f07-implementation-report.md` | Patient Tracker — F-07 closed: registration vs consultation milestone separation (admission Done; physician Serve via MulaiPeriksa) |
| `docs/contexts/pasien-tracker/tracker-f08-implementation-report.md` | Patient Tracker — F-08 closed: Consult-Start / Consult-Done evidence on MulaiPeriksa / SelesaiPeriksa |
| `docs/contexts/pasien-tracker/tracker-f09-implementation-report.md` | Patient Tracker — F-09 closed: pharmacy queue same TrackerId; sale→Apotek-Start; handover→Apotek-Done (Farinv cmds completed in F-10 companion) |
| `docs/contexts/pasien-tracker/tracker-f10-implementation-report.md` | Patient Tracker — F-10 closed: GET tracker/timeline + Mulai/Selesai/QueGet response contracts (Farinv Que* in same commit attributed to F-09) |
| `docs/contexts/pasien-tracker/tracker-f11-implementation-report.md` | Patient Tracker — F-11 Slice 1 closed: EMR antrian outbox for BookingCreate + RegJalan (WalkIn/ByBooking); pharmacy/full §9 facts remain open |
| `docs/contexts/pasien-tracker/tracker-f12-implementation-report.md` | Patient Tracker — F-12 closed in source: Queue Session + deterministic evidence persistence shape (cross-commit F-01/F-03/F-05/F-06; close `0256688d`) |
| `docs/contexts/pasien-tracker/tracker-f13-implementation-report.md` | Patient Tracker — F-13 closed in source: queue-number compatibility adapter & authority map |
| `docs/contexts/pasien-tracker/tracker-admission-queue-late-identification-gap-analysis.md` | Patient Tracker — implementation gap analysis for anonymous admission service, late Tracker association, and Registration-owned Walk-In Tracker creation |

---

## Shared (`docs/shared/`)

| Path | Purpose |
|------|---------|
| `docs/shared/audit-log.md` | Compliance `AuditLog` developer guide |
| `docs/concepts/operational-events.md` | Disambiguates operational vs audit vs DDD events |

---

## Path migration (old → new)

| Old path | New path |
|----------|----------|
| `Bilreg.Domain/LabContext/docs/DOMAIN.md` | `docs/contexts/lab/lab-domain.md` |
| `Bilreg.Domain/LabContext/docs/AGENT.md` | `docs/contexts/lab/lab-agent.md` |
| `Bilreg.Domain/LabContext/docs/IMPLEMENTATION_PLAN.md` | `docs/contexts/lab/lab-implementation-plan.md` |
| `Bilreg.Domain/LabContext/docs/PRG-1-WORKFOW.md` | `docs/contexts/lab/lab-workflow.md` |
| `Bilreg.Domain/LabContext/docs/PRG-2-INTEGRATION.md` | `docs/contexts/lab/lab-integration.md` |
| `Bilreg.Domain/LabContext/docs/PRG-3-TEST-SCENARIO.md` | `docs/contexts/lab/lab-test-scenarios.md` |
| `Bilreg.Domain/IgdContext/docs/DOMAIN.md` | `docs/contexts/igd/igd-02-domain.md` |
| `Bilreg.Domain/IgdContext/docs/OPERATIONAL_RECOVERY.md` | `docs/contexts/igd/igd-05-runbook.md` |
| `docs/contexts/igd/igd-domain.md` | `docs/contexts/igd/igd-01-context.md`, `igd-02-domain.md`, `igd-03-design.md`, `igd-04-api-contract.md`, `igd-05-runbook.md` |
| `docs/contexts/igd/igd-operational-recovery.md` (content) | `docs/contexts/igd/igd-05-runbook.md` |
| `docs/contexts/trsbilling/trsbilling-01-context.md` | `docs/contexts/TataRekening/01-context.md` |
| `docs/contexts/trsbilling/trsbilling-02-domain.md` | `docs/contexts/TataRekening/02-domain.md` |
| `docs/contexts/trsbilling/trsbilling-03-design.md` | `docs/contexts/TataRekening/03-design.md` |
| `docs/contexts/trsbilling/trsbilling-04-pasien-balance.md` | merged into `docs/contexts/TataRekening/01-context.md`, `02-domain.md`, `03-design.md` |
| `docs/contexts/trsbilling/trsbilling-domain-gap-analysis-report.md` | `docs/contexts/TataRekening/tata-rekening-domain-gap-analysis-report.md` |
| `Bilreg.Domain/Shared/AuditLogFeature/AUDIT_LOG_README.md` | `docs/shared/audit-log.md` |

Old locations may contain short redirect stubs during transition.

---

## Do not use in prompts

| Unsafe | Use instead |
|--------|-------------|
| `DOMAIN.md` (no path) | `docs/contexts/lab/lab-domain.md` or `docs/contexts/igd/igd-02-domain.md` |
| `igd-domain.md` (no path) | `docs/contexts/igd/igd-02-domain.md` (domain); use full trio for feature work |
| `01-context.md` / `02-domain.md` / `03-design.md` (IGD, no `igd-` prefix) | `docs/contexts/igd/igd-01-context.md`, `igd-02-domain.md`, `igd-03-design.md` |
| `tarif-01-context.md` … `tarif-05-runbook.md` (no path) | `docs/contexts/tarif/tarif-01-context.md` … `tarif-05-runbook.md` |
| `01-context.md` / `02-domain.md` / `03-design.md` / `04-sop.md` (Tata Rekening, no path) | `docs/contexts/TataRekening/01-context.md`, `02-domain.md`, `03-design.md`, `04-sop.md` |
| `SOP-TR-01` … `SOP-TR-10` (no path) | `docs/contexts/TataRekening/SOP-TR-01 — Open Tata Rekening.md` … `SOP-TR-10 — Settlement Initiation.md` |
| `igd-operational-recovery.md` (no path) | `docs/contexts/igd/igd-05-runbook.md` |
| `AGENT.md` (no path) | `docs/contexts/lab/lab-agent.md` |
| `WORKFLOW.md` without path | `docs/WORKFLOW.md` (global) **or** `docs/contexts/lab/lab-workflow.md` (Lab) |
| "the event doc" | `docs/concepts/operational-events.md` + specific feature path |

---

## Code layout (not markdown)

Implementation lives under `{Layer}/{Context}/{Feature}/` (e.g. `Bilreg.Domain/LabContext/LabOrderFeature/`). SQL scripts under `Bilreg.SqlDb/`. Documentation does not live next to feature code except redirect stubs.

## Artifact Stewardship

Feature artifacts are maintained by:

- Feature Knowledge Steward Agent

Feature artifact lifecycle and maintenance policy:

- see `docs/agents/feature-knowledge-steward.md`
-
