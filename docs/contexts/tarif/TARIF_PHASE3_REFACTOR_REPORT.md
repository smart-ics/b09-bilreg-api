# Tarif Phase 3 — Publish Engine Pragmatic Refactor Report

**Date:** 2026-05-26  
**Scope:** Simplify Phase-3 publish orchestration to match [`ENGINEERING.md`](../../ENGINEERING.md) and [`use-case-generation.md`](../../skills/use-case-generation.md).  
**Behavior:** Unchanged — transactional publish, re-publish, projection upsert, audit log.

---

## What was removed

| Removed | Files | Rationale |
| ------- | ----- | --------- |
| `ITarifPublishValidator` + `TarifPublishValidator` | 2 | Orchestration-specific validation belongs in handler private methods |
| `ITarifVariantProjectionMapper` + `TarifVariantProjectionMapper` | 2 | Single-use-case mapping; not a reusable boundary |
| `TarifPublishException` + `TarifPublishErrorCodes` | 2 | No project-wide error-catalog pattern in ChargeContext handlers |

**Total:** 6 Application files deleted under `Publishing/`.  
**DI:** Removed explicit registrations from `ApplicationService.cs`.

---

## What was preserved

| Boundary | Why kept |
| -------- | -------- |
| `ITarifPolicyRepo` | Aggregate persistence gateway |
| `ITarifPublishLogRepo` | Append-only audit persistence |
| `INilaiTarifProjectionWriter` | Operational projection upsert (separate from policy aggregate) |
| `ITarifRepo`, `ITipeTarifRepo`, `IKelasRepo`, `IKomponenRepo` | Master read repos (existing persistence contracts) |
| Domain methods | `ValidateForPublish`, `ValidateForRepublish`, `MarkPublished`, `ToPublishedSnapshot` |
| `TransHelper.NewScope()` | Single transaction for log + projection + policy save |

Aggregate separation, projection store (`BILRG_NilaiTarif*`), and Billing consumer paths are **unchanged**.

---

## Target structure

All publish orchestration lives in [`TrfPublishTarifPolicyCmd.cs`](../../../Bilreg.Application/ChargeContext/TarifFeature/UseCases/TrfPublishTarifPolicyCmd.cs):

- `Handle` — linear flow
- `EnsurePublishable` / `EnsureMasterReferences` — pre-transaction validation
- `CreateProjection` — variant → `NilaiTarifType`
- `WithVariants` — rebuild policy graph before save

---

## Alignment with ENGINEERING.md

| Principle | How refactor aligns |
| --------- | ------------------- |
| §1 Low cognitive load | One file to read for publish behavior |
| §10 Use-case orchestration | Handler coordinates flow; domain owns invariants |
| §11 Layered validation | Primitives via Guard; business via domain + handler checks |
| §12 Transaction in Application | Explicit `TransHelper.NewScope()` unchanged |

---

## Tests

- Deleted `TarifPublishValidatorTest` (logic covered by handler tests UT5–UT7).
- `TrfPublishTarifPolicyHandlerTest` uses master-repo mocks; 7 cases including validation and re-publish.
- **Gate:** `dotnet test --filter FullyQualifiedName~TarifFeature` — 135 passed.

---

## Remaining acceptable abstractions

- Persistence repos and projection writer (not workflow facades).
- MediatR command/handler (project standard).
- No `ITarifPublishService`, no publish-specific mapper/validator interfaces.

Phase 4 may add HTTP and map standard exceptions to API error shapes.
