# ARTIFACTS.md — Documentation index

**Read this file first** when using AI prompts or onboarding. All canonical paths are listed here with full paths — do not reference bare `DOMAIN.md`, `AGENT.md`, or `WORKFLOW.md` without a context prefix.

---

## Prompt recipe (ordered)

1. [`docs/INSTRUCTION.md`](INSTRUCTION.md) — global engineering stance
2. **Bounded context** — [`docs/contexts/{context}/`](contexts/) (see table below). IGD UI/integration: also [`docs/contexts/igd/igd-04-api-contract.md`](contexts/igd/igd-04-api-contract.md); ops/DBA: [`docs/contexts/igd/igd-05-runbook.md`](contexts/igd/igd-05-runbook.md).
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