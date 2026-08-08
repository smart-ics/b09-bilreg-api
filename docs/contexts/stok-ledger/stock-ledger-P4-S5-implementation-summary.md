# Stock Ledger Phase 4 / P4-S5 — Implementation Summary

**Status:** COMPLETE  
**Date:** 2026-08-08  
**Slice:** P4-S5 — Coexistence proof + Phase 4 exit  
**Plan:** [`stock-ledger-phase4-implementation-plan.md`](./stock-ledger-phase4-implementation-plan.md)  
**Dependencies:** P4-S1–S4 COMPLETE; Phase 3 Freshness Gate + sync stack  
**Phase exit report:** [`stock-ledger-phase4-implementation-report.md`](./stock-ledger-phase4-implementation-report.md)

---

## Objective

Answer: after a Native DO Receipt, can later VB6-shaped consumption/transfer be detected and synchronized by Phase 3 mechanisms before the next Ledger-dependent operation — and can disabling the capability leave legacy operation unaffected?

---

## Verdict

**PASS.** On disposable `devTest`, Native DO Receipt → fixture-simulated VB6 outbound consume → Freshness Gate → Phase 3 sync catch-up advances `fingerprint-v1`, reduces Native-origin layer remaining to match legacy authority, and leaves Scope `Reconstructed` + `Current` (not an authority violation). AlternatingWriters activated to fixture-simulated extent. Capability-disabled continuity proven at harness level. No production Application/Infrastructure changes were required.

---

## What was implemented

| Area | Deliverable |
|---|---|
| Live coexistence harness | `CreateLiveCoexistenceHarness` — live read/discovery/post writer + sync stack with fake sync writer spy |
| VB6 fixture helper | `SimulateVb6PartialConsume` — direct SQL `tb_stok` reduce + `tb_buku` DU outbound insert (avoids `GetData`/`tb_barang` join on disposable DB) |
| Normative coexistence test | `NativeReceipt_Vb6ShapedChange_SyncCatchUp_ContinuesProcessing` |
| AlternatingWriters | Un-skipped / implemented — Native once + two VB6 consumes + gate/sync each |
| Capability-off harness | `CapabilityDisabled_NoNativeWrites_LegacySimulationUnaffected` |
| Docs | This summary + Phase 4 exit report + plan progress + ARTIFACTS + roadmap |

**Explicitly not implemented:** live VB6 binary / concurrent sessions; production capability enablement; production DI/HTTP write endpoints; Phase 5 ConcurrentOutbound; Stage C / `IsAuthoritative`.

---

## Normative coexistence sequence proven

```text
Native DO Receipt (capability enabled)
  → tb_stok + tb_buku + Native movement/layer + Scope baseline + SYNC bootstrap
  → Simulate VB6 partial consume (DU journal out + balance qty reduce)
  → LegacyStockFreshnessGate.EnsureFreshAsync
  → SynchronizeStockLedgerScopeHandler catch-up (≤1×)
  → LegacySynchronized outbound on Ledger; fingerprint-v1 advances
  → Scope remains Reconstructed + Current; IsSafeToTrustLedgerLayers = true
```

Native-origin layers are **not** treated as a VB6 prohibition. Sync path does **not** rewrite legacy authority (`SyncLegacyWriter.Applied` empty).

---

## Repository decisions

| Decision | Rationale |
|---|---|
| Test-primary slice; no Domain/Application/Infrastructure production change | P4-S2 already bootstraps SYNC identities; Phase 3 sync already handles outbound JournalInsert |
| Separate post UoW (live writer) vs sync UoW (fake writer) | Proves sync never mutates legacy; post still writes authoritative DM rows |
| AlternatingWriters = Native once + multiple VB6-shaped changes | P4-S2 greenfield-only blocks second Native post on same Item+DO (`ScopeNotEligible`) |
| Direct SQL for balance reduce | Disposable `devTest` lacks `tb_barang` / `ta_layanan`; `tb_stok_dal.GetData` joins those tables |
| Keep `ConcurrentOutbound` skipped | Phase 5 ownership (unchanged) |

---

## Deviations from the Phase 4 plan

| Plan expectation | Actual | Impact |
|---|---|---|
| Optional “tiny Application glue” if bootstrap incomplete | Not required — P4-S2 post-commit `BootstrapDiscoveryIdentities` suffices | Cleaner exit |
| AlternatingWriters may mean bidirectional native/legacy FO writers | Interpreted as Native establish + alternating VB6-shaped legacy mutations | Documented; matches greenfield constraint |

---

## Validation evidence

Focused P4-S5 filter (disposable `devTest` only):

```text
dotnet test --filter FullyQualifiedName~NativeReceipt_Vb6ShapedChange|FullyQualifiedName~AlternatingWriters|FullyQualifiedName~CapabilityDisabled_NoNativeWrites
Passed!  - Failed: 0, Passed: 3, Skipped: 0
```

Full coexistence harness:

```text
dotnet test --filter FullyQualifiedName~StockLedgerCoexistenceHarnessPlaceholderTest
Passed!  - Failed: 0, Passed: 10, Skipped: 1
```

Skipped: `ConcurrentOutbound` (Phase 5).

Full StockLedgerFeature (single-threaded; avoids known shared-`devTest` deadlock noise):

```text
dotnet test --filter FullyQualifiedName~StockLedgerFeature -- xUnit.MaxParallelThreads=1
Passed!  - Failed: 0, Passed: 284, Skipped: 1
```

| Coverage | Result |
|---|---|
| Native → VB6 consume → Gate `SynchronizedNow` → qty/position coherent | Pass |
| Native origin retained; LegacySynchronized catch-up present | Pass |
| AlternatingWriters (two VB6 waves) remain reconcileable | Pass |
| Capability disabled → zero Native/Ledger writes; legacy-only readable | Pass |
| Sync does not Apply legacy writer | Pass |
| Prior Phase 4 / Phase 1–3 suite regression (single-threaded) | Pass |

---

## Residual risks / handoff

| Item | Note for Phase 5 / Phase 9 |
|---|---|
| **FQ-06 / production G-17** | Explicitly **not** claimed — fixture-simulated VB6 only |
| **Capability** | Keep `StockLedgerDoReceipt:Enabled` default **false**; no production DI/HTTP |
| **ConcurrentOutbound** | Still skipped — Phase 5 |
| **Shared `devTest` deadlocks** | Known ambient risk under parallel xUnit; gate evidence is focused / single-threaded filters |
| **Second Native post on same DO** | Still `ScopeNotEligible` by design; later phases own multi-touch Native posts if needed |
| **Outbound / FIFO allocation** | Phase 5 — not claimed by Phase 4 technical capability |

**Ready for Phase 4 exit:** yes. Phase 5 may begin Availability / FIFO / transfer work without reopening Stage B authority.

---

## Rollback / containment

Keep capability false. Coexistence tests only mutate disposable `devTest`. No production registration was added. Re-skip AlternatingWriters only if instability returns (not observed on focused filter).
