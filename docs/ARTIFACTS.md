# ARTIFACTS.md — Documentation index

**Read this file first** when using AI prompts or onboarding. All canonical paths are listed here with full paths — do not reference bare `DOMAIN.md`, `AGENT.md`, or `WORKFLOW.md` without a context prefix.

---

## Prompt recipe (ordered)

1. [`docs/INSTRUCTION.md`](INSTRUCTION.md) — global engineering stance
2. **Bounded context** — [`docs/contexts/{context}/`](contexts/) (see table below). IGD UI/integration: also [`docs/contexts/igd/igd-04-api-contract.md`](contexts/igd/igd-04-api-contract.md); ops/DBA: [`docs/contexts/igd/igd-05-runbook.md`](contexts/igd/igd-05-runbook.md). Tarif: [`docs/contexts/tarif/tarif-01-context.md`](contexts/tarif/tarif-01-context.md) through `tarif-07-admin-workflow.md`.
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

### Admisi — Booking / Jadwal Praktek (`docs/contexts/admisi/`)

| Path | Purpose |
|------|---------|
| `docs/contexts/admisi/jadwal-praktek-investigation-report.md` | Codebase investigation — `JadwalPraktekType` usage, consumers, and update paths |
| `docs/contexts/admisi/jadwal-praktek-harian-architecture-analysis.md` | Architecture analysis — daily schedule (`JadwalPraktekHarian`) design, resolver, migration roadmap |
| `docs/contexts/admisi/jadwal-praktek-harian-deployment-manual.md` | Deployment & operations — SQL order, feature toggle rollout, validation, rollback |
| `docs/contexts/admisi/adr/ADR-001-runtime-effective-schedule.md` | ADR — runtime `JadwalPraktekEffective` and resolver as single authority |
| `docs/contexts/admisi/adr/ADR-002-manual-override-independence.md` | ADR — `Source = MANUAL` daily rows independent from template |
| `docs/contexts/admisi/adr/ADR-003-booking-schedule-references.md` | ADR — dual nullable schedule IDs on booking |

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