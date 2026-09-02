# ARTIFACTS.md — Documentation index

**Read this file first** when using AI prompts or onboarding. All canonical paths are listed here with full paths — do not reference bare `DOMAIN.md`, `AGENT.md`, or `WORKFLOW.md` without a context prefix.

---

## Prompt recipe (ordered)

1. [`docs/INSTRUCTION.md`](INSTRUCTION.md) — global engineering stance
2. **Bounded context** — [`docs/contexts/{context}/`](contexts/) (see table below). Use full paths; never bare `DOMAIN.md` / `WORKFLOW.md`.
3. **Global standards** (as needed):
   - [`docs/ENGINEERING.md`](ENGINEERING.md) — layers, repository, domain events philosophy
   - [`docs/DATABASE.md`](DATABASE.md) — SQL, tables, audit columns
   - [`docs/NAMING.md`](NAMING.md) — naming conventions
   - [`docs/WORKFLOW.md`](WORKFLOW.md) — operational UX / queue / workspace (global only)
4. **Skills** — [`docs/skills/`](skills/) (artifact and implementation generation)
5. **Cross-cutting concepts** — [`docs/concepts/operational-events.md`](concepts/operational-events.md) when "event" behavior is ambiguous
6. **Artifact stewardship** — [`docs/agents/feature-knowledge-steward.md`](agents/feature-knowledge-steward.md)

---

## Global standards (`docs/`)

| Path | Purpose |
|------|---------|
| `docs/DATABASE.md` | DATABASE.md — SQL Persistence Engineering Standard |
| `docs/ENGINEERING.md` | ENGINEERING.md — Engineering Architecture Philosophy |
| `docs/INSTRUCTION.md` | GLOBAL ENGINEERING INSTRUCTION |
| `docs/NAMING.md` | NAMING.md — Naming Engineering Standard |
| `docs/WORKFLOW.md` | WORKFLOW.md — Operational Workflow Engineering Standard |
| `docs/business-date-implementation-review.md` | Business Date implementation review |

---

## Bounded contexts (`docs/contexts/`)

### Laboratory (`docs/contexts/lab/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/lab/LAB_API_CONTRACT.md` | LAB_API_CONTRACT.md — Laboratory Workflow API (pre-release) |
| `docs/contexts/lab/LAB_COMPONENT_OPERATIONAL_CATALOG.md` | LAB_COMPONENT_OPERATIONAL_CATALOG.md |
| `docs/contexts/lab/LAB_FINANCIAL-CLEARENCE-ANALYSIS-REPORT.md` | LAB Financial Clearance — Architecture / Domain Analysis Report |
| `docs/contexts/lab/LAB_MASTER_TEST_ALIGNMENT.md` | LAB_MASTER_TEST_ALIGNMENT.md — Phase-0 Alignment Report |
| `docs/contexts/lab/LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md` | LAB_MASTER_TEST_IMPLEMENTATION_PLAN.md — Laboratory Master & LabTest Definition |
| `docs/contexts/lab/lab-agent.md` | lab-agent.md — Laboratory Workflow Feature (Backend) |
| `docs/contexts/lab/lab-domain.md` | lab-domain.md — Laboratory Workflow Feature |
| `docs/contexts/lab/lab-implementation-plan.md` | lab-implementation-plan.md — Laboratory Workflow Feature (LWF) |
| `docs/contexts/lab/lab-integration.md` | lab-integration.md — Laboratory Workflow Feature |
| `docs/contexts/lab/lab-test-scenarios.md` | lab-test-scenarios.md — Laboratory Workflow Feature |
| `docs/contexts/lab/lab-workflow.md` | lab-workflow.md — Laboratory Workflow Feature |

### IGD (`docs/contexts/igd/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/igd/igd-01-context.md` | 01-context.md — IGD Visit |
| `docs/contexts/igd/igd-02-domain.md` | 02-domain.md — IGD Visit |
| `docs/contexts/igd/igd-03-design.md` | 03-design.md — IGD Visit |
| `docs/contexts/igd/igd-04-api-contract.md` | igd-04-api-contract.md — IGD Visit API |
| `docs/contexts/igd/igd-05-runbook.md` | igd-05-runbook.md — IGD Visit Operations |
| `docs/contexts/igd/igd-artifacts-gap-analysis-report.md` | IGD Visit Artifact-to-Code Gap Analysis Report |

### Admisi — Rawat Inap (`docs/contexts/admisi-ranap/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/admisi-ranap/admisi-ranap-architecture.md` | Rawat Inap Admission Architecture |
| `docs/contexts/admisi-ranap/admisi-ranap-coordinated-cancellation-design.md` | Coordinated Cancellation Design — Rawat Inap Admission and Registration |
| `docs/contexts/admisi-ranap/admisi-ranap-domain.md` | Rawat Inap Admission Domain |
| `docs/contexts/admisi-ranap/admisi-ranap-implementation-plan.md` | admisi-ranap-implementation-plan.md — Rawat Inap Admission Backend |
| `docs/contexts/admisi-ranap/admisi-ranap-persistent-workspace-capability-matrix.md` | Admisi Ranap — Persistent Workspace Capability Matrix |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-0-implementation-report.md` | Admisi Ranap Phase 0 — Implementation Report |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-1-implementation-report.md` | Admisi Ranap Phase 1 — Implementation Report |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-2-implementation-report.md` | Admisi Ranap Phase 2 — Implementation Report |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-3-implementation-report.md` | Admisi Ranap Phase 3 — Implementation Report |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-4-implementation-report.md` | Admisi Ranap Phase 4 — Implementation Report |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-5-implementation-report.md` | Admisi Ranap Phase 5 — Implementation Report |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-7-implementation-report.md` | Admisi Ranap Phase 7 — Implementation Report |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b1-journey-resolver-implementation-report.md` | Admisi Ranap Release 1 Phase B1 — Journey Contracts & Pure Resolver |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b2-journey-projection-implementation-report.md` | Admisi Ranap Release 1 Phase B2 — Journey Read DAL / Projection |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b2.1-journey-projection-hardening-report.md` | Admisi Ranap Release 1 Phase B2.1 — Journey Projection Hardening |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b3-journey-api-implementation-report.md` | Release 1 Phase B3 — Journey API implementation report |
| `docs/contexts/admisi-ranap/admisi-ranap-phase-b3.1-staging-verification-report.md` | Release 1 Phase B3.1 — Journey API staging verification |
| `docs/contexts/admisi-ranap/admisi-ranap-registration-orchestration.md` | Admisi Ranap Registration Orchestration |
| `docs/contexts/admisi-ranap/admisi-ranap-rollout-checklist.md` | Admisi Ranap — Rollout Checklist |
| `docs/contexts/admisi-ranap/admisi-ranap-runbook.md` | admisi-ranap-runbook.md — Rawat Inap Admission Operations |
| `docs/contexts/admisi-ranap/admisi-ranap-scenario-10-waiting-list-reverification.md` | Scenario 10 — Waiting List continuation (re-verification) |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-A1 Create Opname Request.md` | SOP-RI-A1 — Create Opname Request |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-A2 Cancel Opname Request.md` | SOP-RI-A2 — Cancel Opname Request |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-B1 Create Reservation.md` | SOP-RI-B1 — Create Reservation |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-B2 Maintain Reservation.md` | SOP-RI-B2 — Maintain Reservation |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-C1 Proses Admision.md` | SOP-RI-C1 — Process Admission |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-C2 Cancel Admision.md` | SOP-RI-C2 — Cancel Admission |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-C3 Update Admision.md` | SOP-RI-C3 — Update Admission |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-D1 Waiting List Management.md` | SOP-RI-D1 — Waiting List Management |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-D2 Assign Room Bed.md` | SOP-RI-D2 — Assign Room & Bed |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/SOP-RI-D3 Release Bed Assignment.md` | SOP-RI-D3 — Release Bed Assignment |
| `docs/contexts/admisi-ranap/admisi-ranap-sop/list-sop-admisi-ranap.md` | SOP Registrasi Rawat Inap |
| `docs/contexts/admisi-ranap/admisi-ranap-step-4c-verification-report.md` | Step 4C — End-to-End and Database Verification Report |
| `docs/contexts/admisi-ranap/admisi-ranap-step-6-browser-e2e-verification-report.md` | Step 6 — Browser E2E Verification Report |
| `docs/contexts/admisi-ranap/admisi-ranap-waiting-list-create-422-investigation.md` | Waiting List Create 422 — Investigation Report |
| `docs/contexts/admisi-ranap/admission-cancellation-implementation-contract.md` | Admission Cancellation — Implementation Contract |
| `docs/contexts/admisi-ranap/admission-cancellation-task-10f-verification-report.md` | Task 10F — Coordinated Admission Cancellation Verification Report |
| `docs/contexts/admisi-ranap/rawat-inap-patient-journey-workspace-implementation-plan.md` | Rawat Inap Patient Journey Workspace Implementation Plan |
| `docs/contexts/admisi-ranap/rawat-inap-persistent-workspace-backend-gap-analysis.md` | Rawat Inap Persistent Workspace — Backend Gap Analysis |
| `docs/contexts/admisi-ranap/ta-reg-inap-persistence-contract.md` | `ta_reg_inap` Persistence Contract — Inpatient Registration |

### Admisi — Rawat Jalan (`docs/contexts/admisi-rajal/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/admisi-rajal/admisi-rajal-domain-id.md` | Domain Admisi Rajal |
| `docs/contexts/admisi-rajal/admisi-rajal-domain.md` | Admisi Rajal Domain |
| `docs/contexts/admisi-rajal/admisi-rajal-officer-worklist-card-projection-feature-id.md` | Proyeksi Data Kartu Worklist Petugas Admisi Rajal |
| `docs/contexts/admisi-rajal/admisi-rajal-officer-worklist-card-projection-feature.md` | Admisi Rajal Officer Worklist Card Projection |
| `docs/contexts/admisi-rajal/admisi-rajal-queue-number-feasibility-analysis.md` | Admisi Rajal Queue Number — Codebase Feasibility Analysis |
| `docs/contexts/admisi-rajal/adr/ADR-001-runtime-effective-schedule.md` | ADR-001 — Runtime Effective Schedule |
| `docs/contexts/admisi-rajal/adr/ADR-002-manual-override-independence.md` | ADR-002 — Manual Override Independence |
| `docs/contexts/admisi-rajal/adr/ADR-003-booking-schedule-references.md` | ADR-003 — Booking Schedule References |
| `docs/contexts/admisi-rajal/jadwal-praktek-harian-architecture-analysis.md` | JadwalPraktekHarian — Architecture Analysis |
| `docs/contexts/admisi-rajal/jadwal-praktek-harian-deployment-manual.md` | jadwal-praktek-harian-deployment-manual.md — Deployment & Operations |
| `docs/contexts/admisi-rajal/jadwal-praktek-investigation-report.md` | JadwalPraktek Investigation Report |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-mode-feasibility-analysis.md` | Feasibility Analysis — Admisi Rajal Admission Officer Workspace Modes |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-0-implementation-summary.md` | Admission Officer Workspace — Phase 0 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-1-implementation-summary.md` | Admission Officer Workspace — Phase 1 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-2-implementation-summary.md` | Admission Officer Workspace — Phase 2 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-3-implementation-summary.md` | Admission Officer Workspace — Phase 3 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-4-implementation-summary.md` | Admission Officer Workspace — Phase 4 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-5-implementation-summary.md` | Admission Officer Workspace — Phase 5 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-6-implementation-summary.md` | Admission Officer Workspace — Phase 6 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-7-implementation-summary.md` | Admission Officer Workspace — Phase 7 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-8-implementation-summary.md` | Admission Officer Workspace — Phase 8 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-phase-9-implementation-summary.md` | Admission Officer Workspace — Phase 9 Implementation Summary |
| `docs/contexts/admisi-rajal/queue-workspace/admisi-rajal-officer-workspace-refactoring-implementation-master-plan.md` | Implementation Master Plan — Admisi Rajal Admission Officer Workspace Refactoring |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-0-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 0 Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-1-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 1 Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-2-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 2 Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-3-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 3 Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-4-implementation-summary.md` | Phase 4 — Return to Waiting Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-5-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 5 Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-6-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 6 Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-7-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 7 Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-8-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 8 Implementation Summary |
| `docs/contexts/admisi-rajal/redesign-ui/admisi-rajal-high-density-worklist-phase-9-implementation-summary.md` | Admisi Rajal High-Density Worklist — Phase 9 Implementation Summary |
| `docs/contexts/admisi-rajal/reg-deep-search-feature-id.md` | Fitur Reg Deep Search |
| `docs/contexts/admisi-rajal/reg-deep-search-feature.md` | Reg Deep Search Feature |

### Stock Ledger (`docs/contexts/stok-ledger/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/stok-ledger/stok-ledger-S1-A0-implementation-summary.md` | Stock Ledger S1-A0 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-A1-implementation-summary.md` | Stock Ledger S1-A1 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-A2-implementation-summary.md` | Stock Ledger S1-A2 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-B-implementation-summary.md` | Stock Ledger S1-B — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-C1-implementation-summary.md` | Stock Ledger S1-C1 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-C2-implementation-summary.md` | Stock Ledger S1-C2 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-D1-implementation-summary.md` | Stock Ledger S1-D1 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-D2-implementation-summary.md` | Stock Ledger S1-D2 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-E-implementation-summary.md` | Stock Ledger S1-E — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-F1-implementation-summary.md` | Stock Ledger S1-F1 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-F2-implementation-summary.md` | Stock Ledger S1-F2 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-G1-implementation-summary.md` | Stock Ledger S1-G1 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-G2-implementation-summary.md` | Stock Ledger S1-G2 — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-S1-H-implementation-summary.md` | Stock Ledger S1-H — Implementation Summary |
| `docs/contexts/stok-ledger/stok-ledger-architecture.md` | Stock Ledger Architecture |
| `docs/contexts/stok-ledger/stok-ledger-domain-id.md` | Domain Stock Ledger |
| `docs/contexts/stok-ledger/stok-ledger-domain.md` | Stock Ledger Domain |
| `docs/contexts/stok-ledger/stok-ledger-implementation-plan.md` | Stock Ledger v2 — Slice 1 Implementation Plan |
| `docs/contexts/stok-ledger/stok-ledger-maintainer-brief.md` | Stock Ledger — Brief untuk Programmer Maintenance |

### Apotek (`docs/contexts/apotek/`)

Index lists **Active** and **Working** only. Historical reports live under [`docs/contexts/apotek/archive/`](contexts/apotek/archive/) and must not be used as current design (see [`docs/skills/artifact-management-skill.md`](skills/artifact-management-skill.md)). Cleanup record: [`docs/contexts/apotek/working/APOTEK-ARTIFACT-CLEANUP-REPORT.md`](contexts/apotek/working/APOTEK-ARTIFACT-CLEANUP-REPORT.md).

| Path | Purpose |
|------|---------|
| `docs/contexts/apotek/adr/ADR-APT-001-queu-boundary-and-pharmacy-workflow-state-ownership.md` | ADR-APT-001 Queue Boundary and Pharmacy Workflow State Ownership |
| `docs/contexts/apotek/adr/ADR-APT-002-pharmacy-stock-ledger-boundary.md` | ADR-APT-002 Pharmacy and Stock Ledger Boundary |
| `docs/contexts/apotek/adr/ADR-APT-003-invoice-mutability-owned-by-tata-rekening.md` | ADR-APT-003 Invoice Mutability Owned by Tata Rekening Permission |
| `docs/contexts/apotek/apotek-domain-id.md` | Domain Apotek — Pelayanan Obat Pasien |
| `docs/contexts/apotek/apotek-domain.md` | Apotek Domain |
| `docs/contexts/apotek/outpatient-apotek-persistence-design.md` | Outpatient Apotek Persistence Design |
| `docs/contexts/apotek/outpatient-apotek-screen-and-aggregate-design.md` | Outpatient Apotek Screen and Aggregate Design |
| `docs/contexts/apotek/outpatient-apotek-workflow-id.md` | Workflow Outpatient Apotek |
| `docs/contexts/apotek/outpatient-apotek-workflow.md` | Outpatient Apotek Workflow |
| `docs/contexts/apotek/outpatient-apotek-progress-tracker.md` | Outpatient Apotek Implementation Progress Tracker |
| `docs/contexts/apotek/working/APOTEK-AVAILABLE-STOCK-CONCEPT-INTRODUCTION.md` | Available Stock ≠ Current Stock (formula still open: PD-09) |
| `docs/contexts/apotek/working/outpatient-apotek-repository-gap-analysis-report.md` | Repository Gap Analysis — remaining BC-11–BC-13 |
| `docs/contexts/apotek/working/APOTEK-RESEP-REVISION-TASK-CONSISTENCY-REPORT.md` | PD-11 — Remove `BILRG_AptResepRevisionTask` |
| `docs/contexts/apotek/sop/DAFTAR-SOP-APT-RJ.md` | Daftar SOP Pelayanan Obat Pasien Rawat Jalan |
| `docs/contexts/apotek/sop/SOP-APT-RJ-001-Antrian-dan-Mapping-EN.md` | SOP APT-RJ-001 — Acquire and Map Outpatient Pharmacy Queue |
| `docs/contexts/apotek/sop/SOP-APT-RJ-001-Antrian-dan-Mapping-ID.md` | SOP APT-RJ-001 — Menerbitkan Nomor Antrian dan Melakukan Mapping dengan Resep Kerja atau Jual Bebas |
| `docs/contexts/apotek/sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md` | SOP APT-RJ-002 — Accept Outpatient Medication Demand |
| `docs/contexts/apotek/sop/SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-ID.md` | SOP APT-RJ-002 — Menerima Permintaan Obat Pasien Rawat Jalan |
| `docs/contexts/apotek/sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md` | SOP APT-RJ-003 — Fulfill Medication for a General Patient |
| `docs/contexts/apotek/sop/SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-ID.md` | SOP APT-RJ-003 — Melayani Obat untuk Pasien Umum |
| `docs/contexts/apotek/sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md` | SOP APT-RJ-004 — Fulfill Medication for a BPJS Patient |
| `docs/contexts/apotek/sop/SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-ID.md` | SOP APT-RJ-004 — Melayani Obat untuk Pasien BPJS |
| `docs/contexts/apotek/sop/SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-EN.md` | SOP APT-RJ-005 — Fulfill Mixed-Coverage Medication |
| `docs/contexts/apotek/sop/SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-ID.md` | SOP APT-RJ-005 — Melayani Obat dengan Penjaminan Campuran |
| `docs/contexts/apotek/sop/SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-EN.md` | SOP APT-RJ-006 — Coordinate Multiple Medication Demands in One Queue |
| `docs/contexts/apotek/sop/SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-ID.md` | SOP APT-RJ-006 — Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrian |
| `docs/contexts/apotek/sop/SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-EN.md` | SOP APT-RJ-007 — Resolve Uncollected Outpatient Medication |
| `docs/contexts/apotek/sop/SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-ID.md` | SOP APT-RJ-007 — Menangani Obat Rawat Jalan yang Tidak Diambil |

### Tarif (`docs/contexts/tarif/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/tarif/TARIF_IMPLEMENTATION_PLAN.md` | TARIF_IMPLEMENTATION_PLAN.md — Tarif Policy & Publish Rollout |
| `docs/contexts/tarif/TARIF_PHASE0_REPORT.md` | Tarif Phase 0 — Implementation Report |
| `docs/contexts/tarif/TARIF_PHASE1_ALIGNMENT_REPORT.md` | TARIF_PHASE1_ALIGNMENT_REPORT.md — Retroactive domain alignment |
| `docs/contexts/tarif/TARIF_PHASE2_REPORT.md` | TARIF_PHASE2_REPORT.md — Policy persistence & projection writer |
| `docs/contexts/tarif/TARIF_PHASE3_REFACTOR_REPORT.md` | Tarif Phase 3 — Publish Engine Pragmatic Refactor Report |
| `docs/contexts/tarif/tarif-01-context.md` | Tarif Subsystem — Context |
| `docs/contexts/tarif/tarif-02-domain.md` | Tarif Subsystem — Domain |
| `docs/contexts/tarif/tarif-03-design.md` | Tarif Subsystem — Design |
| `docs/contexts/tarif/tarif-04-api-contract.md` | Tarif Subsystem — API Contract |
| `docs/contexts/tarif/tarif-05-runbook.md` | Tarif Subsystem — Runbook |
| `docs/contexts/tarif/tarif-06-publish-engine.md` | Tarif Subsystem — Publish Engine (Phase 3) |
| `docs/contexts/tarif/tarif-07-admin-workflow.md` | Tarif Subsystem — Admin Workflow (Phase 4) |
| `docs/contexts/tarif/tarif-09-rollout-checklist.md` | Tarif — Rollout Checklist (M0–M3) |
| `docs/contexts/tarif/tarif-10-migration-strategy.md` | Tarif — Migration Strategy (Import → Publish) |
| `docs/contexts/tarif/tarif-phase2-audit-report.md` | Tarif Subsystem — Phase-2 Architecture Audit Report |

### Tata Rekening (`docs/contexts/TataRekening/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/TataRekening/01-context.md` | 01-context.md — Tata Rekening |
| `docs/contexts/TataRekening/02-domain.md` | 02-domain.md — Tata Rekening |
| `docs/contexts/TataRekening/03-design.md` | 03-design.md — Tata Rekening |
| `docs/contexts/TataRekening/04-sop.md` | SOP TATA REKENING |
| `docs/contexts/TataRekening/06-frontend-implementation-plan.md` | 06 — Tata Rekening Frontend Implementation Plan |
| `docs/contexts/TataRekening/SOP Tata Rekening All.md` | SOP TATA REKENING |
| `docs/contexts/TataRekening/SOP-TR-01 — Open Tata Rekening.md` | SOP-TR-01 — Open Tata Rekening |
| `docs/contexts/TataRekening/SOP-TR-02 — Close Bill.md` | SOP-TR-02 — Close Bill |
| `docs/contexts/TataRekening/SOP-TR-03 — Merge Billing.md` | SOP-TR-03 — Merge Billing |
| `docs/contexts/TataRekening/SOP-TR-04 — Financial Verification.md` | SOP-TR-04 — Financial Verification |
| `docs/contexts/TataRekening/SOP-TR-05 — Financial Adjustment.md` | SOP-TR-05 — Financial Adjustment |
| `docs/contexts/TataRekening/SOP-TR-06 — Financial Responsibility Allocation.md` | SOP-TR-06 — Financial Responsibility Allocation |
| `docs/contexts/TataRekening/SOP-TR-07 — Finalize Financial Responsibility.md` | SOP-TR-07 — Finalize Financial Responsibility |
| `docs/contexts/TataRekening/SOP-TR-08 — Cancel Finalization.md` | SOP-TR-08 — Cancel Finalization |
| `docs/contexts/TataRekening/SOP-TR-09 — Reopen Billing.md` | SOP-TR-09 — Reopen Billing |
| `docs/contexts/TataRekening/SOP-TR-10 — Settlement Initiation.md` | SOP-TR-10 — Settlement Initiation |
| `docs/contexts/TataRekening/frontend-integration-guide.md` | Tata Rekening — Frontend Integration Guide |
| `docs/contexts/TataRekening/frontend-readiness-checklist.md` | Tata Rekening — Frontend Readiness Checklist |
| `docs/contexts/TataRekening/tata-rekening-api-contract.md` | Tata Rekening — API Contract |
| `docs/contexts/TataRekening/tata-rekening-domain-gap-analysis-report.md` | Tata Rekening Gap Analysis Report |
| `docs/contexts/TataRekening/tata-rekening-implementation-plan.md` | Tata Rekening Backend Implementation Plan |
| `docs/contexts/TataRekening/tata-rekening-phase-1-implementation-report.md` | Tata Rekening Phase 1 — Implementation Report |
| `docs/contexts/TataRekening/tata-rekening-phase-2-implementation-report.md` | Tata Rekening Phase 2 — Implementation Report |
| `docs/contexts/TataRekening/tata-rekening-phase-3-implementation-report.md` | Tata Rekening Phase 3 — Implementation Report |
| `docs/contexts/TataRekening/tata-rekening-phase-4-implementation-report.md` | Tata Rekening Phase 4 — Implementation Report |
| `docs/contexts/TataRekening/tata-rekening-phase-5-implementation-report.md` | Tata Rekening Phase 5 — Implementation Report |

### Taksaka domain (`docs/contexts/taksaka/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/taksaka/taksaka-01-domain.md` | DOMAIN.md |
| `docs/contexts/taksaka/taksaka-02-architecture.md` | Taksaka V2 — Architecture |

### CPOE (`docs/contexts/cpoe/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/cpoe/CPOE-DOMAIN-ID.md` | Domain CPOE |
| `docs/contexts/cpoe/CPOE-DOMAIN.md` | CPOE Domain |

### RUANG RANAP Operational Management (`docs/contexts/bangsal/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/bangsal/ADMISI-RNA-INTEGRATION.md` | ADMISI ↔ RNA Integration |
| `docs/contexts/bangsal/CPOE-RNA-INTEGRATION.md` | CPOE ↔ RNA Integration |
| `docs/contexts/bangsal/RNA-ARCHITECTURE.md` | RNA Architecture |
| `docs/contexts/bangsal/RNA-DOMAIN-ID.md` | Domain Operasional RUANG RANAP |
| `docs/contexts/bangsal/RNA-DOMAIN.md` | RUANG RANAP Operational Domain |
| `docs/contexts/bangsal/RNA-IMPLEMENTATION-PLAN.md` | RNA Implementation Plan |
| `docs/contexts/bangsal/RNA-SERVICE-EXECUTION-MODEL.md` | RNA Service Execution Model |
| `docs/contexts/bangsal/RNA-TATA-REKENING-INTEGRATION.md` | RNA ↔ Tata Rekening Integration |
| `docs/contexts/bangsal/RNA-UIUX-DESIGN.md` | RUANG RANAP Operational Management UI/UX Design |
| `docs/contexts/bangsal/cpoe-rna-gap-report.md` | CPOE–RNA Alignment Gap Report |
| `docs/contexts/bangsal/rna-sop/RNA-SOP-INDEX.md` | Indeks SOP RUANG RANAP Operational System |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-A01-Pemrosesan-Waiting-List-dan-Penetapan-Akomodasi.md` | SOP-RNA-A01 — Pemrosesan Waiting List Admisi dan Penetapan Akomodasi |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-A02-Pengelolaan-Retained-Accommodation.md` | SOP-RNA-A02 — Pengelolaan Retained Accommodation |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-A03-Pengelolaan-Rooming-In.md` | SOP-RNA-A03 — Pengelolaan Rooming-In |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-A04-Transfer-Akomodasi-Internal-RNA.md` | SOP-RNA-A04 — Transfer Akomodasi Internal RUANG RANAP |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-A05-Transfer-Antar-RNA.md` | SOP-RNA-A05 — Release ke Waiting List untuk Perpindahan Antar-RUANG RANAP |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-A06-Pelepasan-Akomodasi.md` | SOP-RNA-A06 — Pelepasan dan Koreksi Akomodasi |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-A07-Pemulihan-Bed-Readiness.md` | SOP-RNA-A07 — Pemulihan Kesiapan Bed |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-S01-Pelaksanaan-RNA-Service-Berdasarkan-Clinical-Order.md` | SOP-RNA-S01 — Penyelesaian Order Occurrence oleh RUANG RANAP |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-S02-Pelaksanaan-Tindakan-Ad-Hoc-atau-Independen.md` | SOP-RNA-S02 — Pencatatan Tindakan Ad Hoc atau Independen |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-S03-Koreksi-RNA-Service-Execution.md` | SOP-RNA-S03 — Koreksi Fakta Pelaksanaan Layanan RUANG RANAP |
| `docs/contexts/bangsal/rna-sop/SOP-RNA-S04-Penetapan-dan-Penyerahan-Charge-Eligibility.md` | SOP-RNA-S04 — Publikasi Service Execution Fact kepada Tata Rekening |

### Patient Tracker (`docs/contexts/pasien-tracker/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-API-V1.md` | Tracker–Admission Queue API v1 and Compatibility Contract |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ARCHITECTURE-RECONCILIATION.md` | Patient Tracker — Admission Queue Architecture Reconciliation |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md` | Patient Tracker — Admission Queue Operations Architecture |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md` | Domain Patient Tracker — Operasi Antrean Admisi |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-DOMAIN.md` | Patient Tracker — Admission Queue Operations Domain |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-EXTERNAL-CLIENTS-IMPLEMENTATION-PLAN.md` | Patient Tracker — Admission Queue External Clients Implementation Plan |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-IIS-CLIENT-CUTOVER-CHECKLIST.md` | Tracker–Admission Queue — IIS Client Cutover Checklist |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-GAP-ANALYSIS.md` | Patient Tracker — Admission Queue Architecture Implementation Gap Analysis |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-PLAN.md` | Patient Tracker — Admission Queue Implementation Plan |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-IMPLEMENTATION-ROADMAP.md` | Patient Tracker — Admission Queue Implementation Roadmap |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-KIOSK-DISPLAY-DEPLOY-ID.md` | Buku Panduan: Deploy dan Konfigurasi Kiosk Antrian Admisi & Queue Display |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-R04-LOKET-CLAIM-CONTRACT.md` | Patient Tracker — R-04 Cross-Session Active Loket Claim Contract |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-ROLLOUT-CHECKLIST.md` | Tracker–Admission Queue — Rollout Go / No-Go Checklist |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-RUNBOOK.md` | Tracker–Admission Queue Runbook |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-SOP-ID.md` | SOP Patient Tracker — Operasi Antrean Admisi |
| `docs/contexts/pasien-tracker/TRACKER-ADMISSION-QUEUE-SOP.md` | Patient Tracker — Admission Queue Operations SOP |
| `docs/contexts/pasien-tracker/TRACKER-COMPATIBILITY.md` | Patient Tracker — Legacy Compatibility Contract |
| `docs/contexts/pasien-tracker/TRACKER-DOMAIN-ID.md` | Domain Patient Tracker |
| `docs/contexts/pasien-tracker/TRACKER-DOMAIN.md` | Patient Tracker Domain |
| `docs/contexts/pasien-tracker/kiosk-queue-display-web.md` | Kiosk Web & Queue Display Web — Deployment & Repository Strategy |
| `docs/contexts/pasien-tracker/pasien-tracker-narrative-explanation.md` | PASIEN TRACKER |
| `docs/contexts/pasien-tracker/tracker-admission-queue-late-identification-gap-analysis.md` | Patient Tracker — Admission Queue Late-Identification Gap Analysis |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase1-slice1-verification-report.md` | Admission Queue Phase 1 / Slice 1 — Verification Report |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase2-officer-contracts-implementation-report.md` | Phase 2 — Officer-supporting backend contracts and Admisi enrichment |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase3-booking-assistance-implementation-report.md` | Phase 3 — Booking Self-Registration assistance receive-side closure |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase4-signalr-refresh-implementation-report.md` | Phase 4 — SignalR refresh-hint backend adapter |
| `docs/contexts/pasien-tracker/tracker-admission-queue-phase5-rollout-implementation-report.md` | Phase 5 — Backend integration rollout and compatibility closure |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r00-baseline-implementation-report.md` | Patient Tracker — Admission Queue R-00 Baseline Implementation Report |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r01-cas-persistence-implementation-report.md` | Patient Tracker — Admission Queue R-01 Implementation Report |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r05a-sequencer-implementation-report.md` | Patient Tracker — R-05A Sequencer Implementation Report |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r06-service-point-implementation-report.md` | R-06 — Service Point Authority and Immutable Queue Labels |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r07-projections-implementation-report.md` | R-07 — Queue-Only Operational Projections |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r08-transitions-implementation-report.md` | R-08 — Queue Operational Transitions |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r09-registration-outcome-implementation-report.md` | R-09 — Final Registration Outcomes and Queue Completion |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r11-hidok-assistance-implementation-report.md` | R-11 — HiDok Self-Registration Assistance Fallback |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r12a-workstation-identity-implementation-report.md` | R-12A — Workstation Identity Validation |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r13-audit-operational-validation-report.md` | R-13 — Audit and Operational Validation |
| `docs/contexts/pasien-tracker/tracker-admission-queue-r14-deferred-operational-policies.md` | R-14 — Deferred Operational Policies |
| `docs/contexts/pasien-tracker/tracker-c2-kiosk-implementation-report.md` | C2 Implementation Report — Kiosk Client (monorepo path boot, intake, print) |
| `docs/contexts/pasien-tracker/tracker-c3-queue-display-implementation-report.md` | C3 Implementation Report — Queue Display Client (snapshot-first + SignalR) |
| `docs/contexts/pasien-tracker/tracker-c4-integration-deployment-implementation-report.md` | C4 Implementation Report — Integration & Deployment |
| `docs/contexts/pasien-tracker/tracker-codebase-gap-report.md` | Tracker Domain — Codebase Gap Report |
| `docs/contexts/pasien-tracker/tracker-f01-implementation-report.md` | F-01 Implementation Report — Explicit Tracking Period |
| `docs/contexts/pasien-tracker/tracker-f02-implementation-report.md` | F-02 Implementation Report — Stable Journey Identity |
| `docs/contexts/pasien-tracker/tracker-f03-implementation-report.md` | F-03 Implementation Report — Append-Only Tracker Event Persistence |
| `docs/contexts/pasien-tracker/tracker-f04-implementation-report.md` | F-04 Implementation Report — Journey Candidate Resolution |
| `docs/contexts/pasien-tracker/tracker-f05-implementation-report.md` | F-05 Implementation Report — Anonymous Admission Intake & Identification |
| `docs/contexts/pasien-tracker/tracker-f06-implementation-report.md` | F-06 Implementation Report — Queue Aggregate Identity & Lifecycle Invariants |
| `docs/contexts/pasien-tracker/tracker-f07-implementation-report.md` | F-07 Implementation Report — Registration vs Consultation Milestone Separation |
| `docs/contexts/pasien-tracker/tracker-f08-implementation-report.md` | F-08 Implementation Report — Consultation Evidence on Patient Tracker Timeline |
| `docs/contexts/pasien-tracker/tracker-f09-implementation-report.md` | F-09 Implementation Report — Pharmacy Queue Integrated into Patient Journey |
| `docs/contexts/pasien-tracker/tracker-f10-implementation-report.md` | F-10 Implementation Report — Core Tracker HTTP / Application Contracts |
| `docs/contexts/pasien-tracker/tracker-f11-implementation-report.md` | F-11 Implementation Report — EMR Antrian Outbox (Slice 1) |
| `docs/contexts/pasien-tracker/tracker-f12-implementation-report.md` | F-12 Implementation Report — Persistence Shape (Queue Session + Deterministic Evidence) |
| `docs/contexts/pasien-tracker/tracker-f13-implementation-report.md` | F-13 Implementation Report — Compatibility Adapter & Authority Map |

### Purchasing (`docs/contexts/purchasing/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/purchasing/purchasing-delivery-order-domain.md` | Delivery Order (DO) Domain — Penerimaan Barang dari Supplier |

### TrsBilling redirect (`docs/contexts/trsbilling/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/trsbilling/README.md` | Moved — Tata Rekening |

---

## Skills (`docs/skills/`)

| Path | Purpose |
|------|---------|
| `docs/skills/architecture-creation-skill.md` | Architecture Creation Skill |
| `docs/skills/artifact-management-skill.md` | Artifact Management Skill |
| `docs/skills/domain-creation-skill.md` | DOMAIN CREATION SKILL |
| `docs/skills/feature-model-generation.md` | FEATURE MODEL GENERATION SKILL |
| `docs/skills/feature-persistence-generation.md` | FEATURE PERSISTENCE GENERATION SKILL |
| `docs/skills/integration-document-creation-skill.md` | integration-document-creation-skill.md |
| `docs/skills/sop-creation-skill.md` | SOP CREATION SKILL |
| `docs/skills/use-case-generation.md` | USECASE GENERATION SKILL |
| `docs/skills/workflow-creation-skill.md` | WORKFLOW CREATION SKILL |
## Concepts (`docs/concepts/`)

| Path | Purpose |
|------|---------|
| `docs/concepts/operational-events.md` | operational-events.md — Event terminology |
## Shared (`docs/shared/`)

| Path | Purpose |
|------|---------|
| `docs/shared/audit-log.md` | audit-log.md — Audit Log Developer Guide |
## Agents (`docs/agents/`)

| Path | Purpose |
|------|---------|
| `docs/agents/feature-knowledge-steward.md` | FEATURE KNOWLEDGE STEWARD AGENT |
## Taksaka operations (`docs/taksaka/`)

| Path | Purpose |
|------|---------|
| `docs/taksaka/00-getting-started.md` | Taksaka — Getting Started |
| `docs/taksaka/01-deployment-guide.md` | Taksaka — Deployment Guide |
| `docs/taksaka/02-configuration-reference.md` | Taksaka — Configuration Reference |
| `docs/taksaka/03-operator-manual.md` | Taksaka — Operator Manual |
| `docs/taksaka/04-troubleshooting.md` | Taksaka — Troubleshooting Guide |
| `docs/taksaka/05-architecture-runtime.md` | Taksaka — Architecture at Runtime |
| `docs/taksaka/06-plugin-development-guide.md` | Taksaka — Plugin Development Guide |
| `docs/taksaka/07-production-checklist.md` | Taksaka — Production Deployment Checklist |
| `docs/taksaka/08-operations-runbook.md` | Taksaka — Operations Runbook |
| `docs/taksaka/09-administrator-guide.md` | Taksaka — Administrator Guide (Panduan Administrator) |
| `docs/taksaka/README.md` | Taksaka — Operations Documentation |
| `docs/taksaka/reports/abstractions-migration-report.md` | Abstractions Migration Report |
| `docs/taksaka/reports/antrian-worker-refactor-diagram.md` | Antrian Worker Refactor — Dependency Diagram |
| `docs/taksaka/reports/architecture-violations-report.md` | Architecture Violations Report |
| `docs/taksaka/reports/plugin-manifest-example.md` | Plugin Manifest Example — AntrianConsistencyRepairWorker |
| `docs/taksaka/reports/production-readiness-report.md` | Taksaka V2 — Production Readiness Report |
| `docs/taksaka/reports/removed-manual-registrations.md` | Removed Manual Worker Registrations |
| `docs/taksaka/reports/worker-discovery-flow.md` | Worker Discovery Flow |
## Tarif retrieval (`docs/tarif/`)

| Path | Purpose |
|------|---------|
| `docs/tarif/tarif-codebase-retrieval-report.md` | Tarif Subsystem — Codebase Retrieval Report |
## External references (`docs/external/`)

| Path | Purpose |
|------|---------|
| `docs/external/nuna-lib.md` | nuna-lib.md — External Nuna.Lib.NetStandard documentation |
## Works / investigations (`docs/works/`)

| Path | Purpose |
|------|---------|
| `docs/works/authorization-investigation-report.md` | Authorization Investigation Report — Bilreg.Api |
| `docs/works/authorization-simplification-report.md` | Authorization Simplification Report — Bilreg.Api |

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
| `docs/contexts/igd/igd-domain.md` | `docs/contexts/igd/igd-01-context.md` … `igd-05-runbook.md` |
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

- see [`docs/agents/feature-knowledge-steward.md`](agents/feature-knowledge-steward.md)

