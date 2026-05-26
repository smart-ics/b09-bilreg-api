# Tarif — Rollout Checklist (M0–M3)

**Artifact role:** OPERATION — staged rollout gates  
**Audience:** Engineering, Keuangan, DBA

---

## Pre-requisites (all stages)

- [ ] Phases 0–4 deployed (`BILRG_Tarif*`, publish engine, policy API)
- [ ] Phase 5 SQL: `Bilreg.SqlDb/ChargeContext/TarifFeature/BILRG_TarifOperationalState.sql`
- [ ] `appsettings` section `Tarif` present
- [ ] `dotnet test --filter FullyQualifiedName~TarifFeature` green
- [ ] Import backup procedure verified ([`tarif-05-runbook.md`](tarif-05-runbook.md))

---

## M0 — Import only

**Config:** `"Mode": "ImportOnly"`

| Engineering | Operations |
| ----------- | ---------- |
| [ ] Deploy API with Phase 5 guards | [ ] No policy publish in prod |
| [ ] `GET /api/tarif-migration/status` returns `ImportOnly` | [ ] Legacy → import remains sole path |
| [ ] Publish returns 400 with clear message | [ ] Train: publish not available yet |

---

## M1 — Hybrid (pilot)

**Config:** `"Mode": "Hybrid"`

| Engineering | Operations |
| ----------- | ---------- |
| [ ] Publish + import both succeed in UAT | [ ] Parallel spot-check: import vs publish sample variants |
| [ ] Optional: `POST /api/tarif-migration/baseline` in UAT | [ ] Keuangan trained on policy draft → publish |
| [ ] `GET /api/tarif-migration/consistency` healthy | [ ] Document first production SK as policy |

---

## M2 — Publish primary

**Config:** `"Mode": "PublishPrimary"`, `"AllowEmergencyImport": false` (enable only for drills)

| Engineering | Operations |
| ----------- | ---------- |
| [ ] Routine import blocked without emergency | [ ] New tariff changes via policy publish |
| [ ] Emergency import tested with `isEmergency: true` | [ ] Import only with written approval + backup |
| [ ] Publish log audited weekly | [ ] Reduce scheduled full imports |

---

## M3 — Import deprecated (operational)

**Config:** `"Mode": "ImportDeprecated"`

| Engineering | Operations |
| ----------- | ---------- |
| [ ] Import endpoint still present (fallback) | [ ] Routine legacy import retired |
| [ ] `AllowEmergencyImport` documented for DR | [ ] DR playbook: emergency import + restore |
| [ ] Full `TarifVariant` archive for audit questions | [ ] Billing discrepancies use transaction snapshot |

---

## Rollback

| Situation | Action |
| --------- | ------ |
| Bad publish committed | New corrective policy + publish |
| Need legacy projection fast | `AllowEmergencyImport: true` + emergency import (backup first) |
| Wrong migration mode | `DELETE /api/tarif-migration/mode` or `PUT` previous mode; or redeploy appsettings |
| Software rollback | DB forward-only; set `ImportOnly` to disable publish API |

---

## Related

| Path | Role |
| ---- | ---- |
| [`tarif-10-migration-strategy.md`](tarif-10-migration-strategy.md) | Strategy narrative |
| [`tarif-05-runbook.md`](tarif-05-runbook.md) | Procedures |
| [`tarif-04-api-contract.md`](tarif-04-api-contract.md) | HTTP reference |
