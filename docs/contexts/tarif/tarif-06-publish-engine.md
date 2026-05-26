# Tarif Subsystem — Publish Engine (Phase 3)

**Bounded context:** `ChargeContext` / Tarif  
**Artifact role:** HOW — manual publish orchestration (application layer)  
**Status:** **LIVE** (explicit handler; HTTP in Phase 4)

**Code:**

| Piece | Path |
| ----- | ---- |
| Command / handler | `Bilreg.Application/ChargeContext/TarifFeature/UseCases/TrfPublishTarifPolicyCmd.cs` |
| Projection upsert | `INilaiTarifProjectionWriter` (Infrastructure — no orchestration) |

Validation and variant→projection mapping live as **private methods** on the handler (`EnsurePublishable`, `CreateProjection`) — not separate Application services.

---

## Purpose

Enable **manual** activation of a `TarifPolicy` draft (or deterministic **re-publish** of an already `Published` policy) by:

1. Writing an append-only **publish log** (+ per-variant detail).
2. Upserting operational **`BILRG_NilaiTarif*`** projection rows (variant mode A — upsert only).
3. Persisting **`PublishedNilaiTarifId`** on policy variant lines.
4. Transitioning policy status to **`Published`** on first publish.

**Out of scope:** scheduler, effective-date auto activation, billing recalculation, HTTP API (Phase 4).

---

## Orchestration flow

```text
TrfPublishTarifPolicyHandler
  1. Load policy (ITarifPolicyRepo)
  2. EnsurePublishable(policy) — private; before transaction
  3. TransHelper.NewScope
  4. FOR EACH variant (ordered by ItemNo):
       CreateProjection(variant)
       INilaiTarifProjectionWriter.Upsert(projection, sourcePolicyId)
       variant.ToPublishedSnapshot(nilaiTarifId)
  5. IF Draft/Reviewed → MarkPublished(user)
     IF Published → status unchanged (re-publish)
  6. ITarifPublishLogRepo.Insert(log + details)
  7. ITarifPolicyRepo.SaveChanges(policy with snapshots)
  8. trans.Complete
```

Repositories remain **persistence-only** — no cross-repo orchestration inside Infrastructure.

---

## Transaction boundary

Single `TransHelper.NewScope()` wraps:

- All projection upserts
- Publish log insert
- Policy save (header + variant/komponen child replace)

Failure before `Complete()` → full rollback; projection and policy unchanged.

---

## First publish vs re-publish

| Case | Policy status | Domain | Projection | Log |
| ---- | ------------- | ------ | ---------- | --- |
| First publish | `Draft` or `Reviewed` | `MarkPublished` | Upsert per variant | New row |
| Re-publish | `Published` | `ValidateForRepublish` (no status change) | Deterministic upsert; **same** `NilaiTarifId` when composite exists | **New** row; prior logs preserved |
| Archived / invalid | — | Rejected | — | — |

Re-publish does **not** mutate posted `TrsBilling` or tindakan snapshots.

---

## Idempotency

- **Not** distributed/idempotency-key based.
- Re-publish same policy: projection replaced deterministically by composite `(TarifId, TipeTarifId, KelasId)`; `NilaiTarifId` preserved when row exists.
- Each successful run appends a new `BILRG_TarifPublishLog` row.

---

## Errors (pragmatic)

Handler uses standard exceptions (aligned with other ChargeContext use-cases):

| Case | Exception |
| ---- | --------- |
| Policy not found | `KeyNotFoundException` |
| Business / status / duplicate variant | `InvalidOperationException` |
| Komponen invariant | `ArgumentException` (domain) |
| Transaction failure | `InvalidOperationException` wrapper with rollback message |

Structured HTTP error codes are **Phase 4** (API layer).

---

## MediatR usage (no HTTP yet)

```http
POST /api/tarif-policy/{policyId}/publish   # Phase 4 — not implemented
```

Today invoke via MediatR:

```csharp
await mediator.Send(new TrfPublishTarifPolicyCmd(policyId, publishedBy, note));
```

---

## Draft-save guardrail (future handlers)

`TarifPolicyRepo.SaveChanges` does **not** enforce `EnsureEditable()`. Any mutating policy handler **must** call `EnsureEditable()` before save. Publish handler does not use `EnsureEditable` for re-publish (intentional).

---

## Tests

`dotnet test --filter FullyQualifiedName~TarifFeature`

Key suites: `TrfPublishTarifPolicyHandlerTest`, `NilaiTarifProjectionWriterTest`, `TarifPolicyTypeTest` (DT15–DT16 republish).

---

## Related artifacts

- [`tarif-02-domain.md`](tarif-02-domain.md) — aggregates, publish semantics
- [`tarif-03-design.md`](tarif-03-design.md) — persistence layout
- [`tarif-04-api-contract.md`](tarif-04-api-contract.md) — proposed HTTP + error shapes
- [`tarif-05-runbook.md`](tarif-05-runbook.md) — ops: import vs publish
- [`TARIF_IMPLEMENTATION_PLAN.md`](TARIF_IMPLEMENTATION_PLAN.md) — phase ledger
- [`TARIF_PHASE3_REFACTOR_REPORT.md`](TARIF_PHASE3_REFACTOR_REPORT.md) — pragmatic simplification notes
