# Admisi Rajal High-Density Worklist — Phase 0 Implementation Summary

**Status:** Decision record complete; frontend baseline verification pending trusted package-manager recovery  
**Date:** 2026-07-25  
**Source roadmap:** `c012_myhospital_web/docs/_notes/admisi/admisi-rajal-high-density-worklist-implementation-roadmap.md`  
**Scope:** Baseline, decisions, and rollout safety. No user workflow, API, schema, or database behavior changed.

## 1. Result

Phase 0 establishes the contracts and rollback rules required before the high-density worklist is
implemented. The current Admission Queue remains the operational baseline: Patient Tracker owns
queue and claim state; Admisi Rajal composes read context and completes Registration atomically.

The existing frontend configuration seam is accepted for the unreleased product:

```json
"admissionQueue": {
  "enabled": true,
  "useLegacySidebarFallback": false,
  "worklistPollMs": 15000
}
```

This is an environment-wide switch, not a workstation-targeting mechanism. No dedicated
high-density-workspace flag is added in Phase 0.

## 2. Approved decisions

| Decision | Approved contract |
| --- | --- |
| Rollout and rollback | Reuse `admissionQueue.enabled` and `useLegacySidebarFallback`. The Admission Supervisor owns the kill switch. Set `useLegacySidebarFallback: true`, then refresh affected clients, to restore the legacy sidebar. |
| Initial rollout | The product is unreleased, so enablement is environment-wide rather than workstation-targeted. A later released-product pilot needs a distinct targeting mechanism before workstation-only rollout is claimed. |
| Return to Waiting | A future officer action releases only the caller's own Loket Outstanding claim. It preserves the Waiting Queue Entry, requires the current claim RowVersion, records actor/time/reason audit evidence, and must not reuse final No-Show semantics. |
| No-Show | Officers may issue No-Show. It remains a final, audited withdrawal and is distinct from Return to Waiting and `NotEstablished`. |
| Queue-linked existing Registration | A successful save of a valid queue-linked existing Registration completes the queue atomically. No separate `Complete Assistance` action is introduced for this path. |
| NotEstablished reasons | The backend owns and validates the approved reason-code allowlist. Frontend configuration may display labels but cannot authorize an arbitrary code. |
| Active worklist paging | Phase 1 retains offset/limit and `priority DESC, createdAt ASC, noUrut ASC` ordering. A request that opts into paging metadata receives `items`, `hasMore`, and `nextOffset`; the current list response remains available to legacy callers. |
| Universal search | Exact identifiers search immediately. Broad text requires three characters, defaults to the business date, returns at most ten results per entity type, masks sensitive identifiers, and always requires Preview followed by an explicit entity-specific confirmation. |
| Performance | Retain the 15-second poll interval. The release target is p95 <= 2 seconds for 100 active entries at the representative 400-entry/20-Loket load; validate it before release. |
| Privacy | Show full name and MR only where operationally necessary. Mask NIK, phone, and membership values, and never send raw search terms to telemetry. |

## 3. Baseline matrix and fixtures

| Scenario | Current baseline | Later regression assertion |
| --- | --- | --- |
| Waiting Booking | Officer can Preview/Call; linked Booking is read composition | Selection remains non-mutating; Call behavior is unchanged. |
| Waiting Walk-In / unresolved identity | Queue entry may have no canonical Patient | UI shows unresolved state and never guesses identity. |
| Existing Registration | Registration context can be composed from tracker events | Queue-linked successful save completes the matching In Service entry atomically. |
| Outstanding claim | Call creates an Outstanding current-Loket claim | Recall, Start Service, and future Return to Waiting require matching RowVersion. |
| In Service claim | Start Service transitions the current Loket claim | Current Loket remains visible independently of Waiting-page membership. |
| Final outcomes | No-Show withdraws; Established/NotEstablished are durable outcomes | Final transitions are mutually exclusive, auditable, and concurrency-safe. |
| Conflict | Queue commands detect stale claim versions | Refresh authoritative worklist/current-Loket state and return safely to Preview. |

The Phase 1 fixture set must contain all rows above, including more than 100 active entries,
same-name patients, stale RowVersions, another-Loket claim, and queue entries without a Patient,
Booking, or Registration reference.

## 4. Compatibility and deployment rules

1. Deploy any future database expansion or index first, then additive backend contracts, then a
   disabled or legacy-fallback frontend, then enable the frontend switch.
2. Existing worklist callers continue receiving the current list response unless they opt into the
   Phase 1 paging metadata. New filters and request fields remain optional.
3. New queue-aware Registration request fields are optional and omitted outside an active
   queue-originated session. Server queue state remains authoritative.
4. A future Return to Waiting route is versioned and new; it never changes No-Show semantics.
5. A future universal-search endpoint is read-only and does not replace existing search routes in
   its first release.
6. No migration or data rollback is required for Phase 0. Future migrations must be expand-first,
   separately deployed, and independently reversible.

## 5. Rollback runbook

**Owner:** Admission Supervisor.

1. Confirm the symptom and affected environment; preserve relevant queue/claim identifiers and
   timestamps without copying unmasked personal data into incident notes.
2. Set `admissionQueue.useLegacySidebarFallback` to `true` in the environment runtime
   `global_config.json`.
3. Publish the configuration and have affected clients refresh. Verify `RegistrasiRajal` renders
   `SidebarAntrianPasien`, not the Admission Queue surface.
4. Smoke-test the legacy queue selection and Registration flow. Do not attempt data rollback: the
   queue and Registration backend remain authoritative and unchanged by this Phase 0 work.
5. Record supervisor, environment, time, reason, configuration revision, and smoke-test result
   before re-enabling later functionality.

Setting `enabled` to `false` also selects the legacy path, but `useLegacySidebarFallback: true` is
the primary rollback because it states the intended fallback explicitly.

## 6. Verification evidence

### Backend baseline

The focused baseline completed successfully on 2026-07-25:

```powershell
dotnet test src\bilreg\Bilreg.Test\Bilreg.Test.csproj --filter "FullyQualifiedName~AdmisiRajalOfficerWorklistQueryTest|FullyQualifiedName~AdmissionQueueOperationalCommandsTest|FullyQualifiedName~RegistrationOutcomeTest" --no-restore
```

Result: **15 passed, 0 failed, 0 skipped**. The coverage includes worklist composition, operational
queue transitions, and Registration outcome behavior.

Existing queue-runbook evidence remains the proof of record for representative SQL volume:
400 entries, 20 Loket claims, a worklist page of 100, and current-display query completion within
five seconds. This is characterization evidence, not proof of the new p95 <= 2 second target.

### Frontend baseline

`pnpm tc:app` was intentionally not bypassed. The local package manager rejected the locked
`pnpm@10.11.1` release because its registry signature could not be verified after a fetch failure.
Recover the trusted package-manager artifact or registry connectivity, then run the frontend
type-check, existing unit tests, and the legacy configuration-off smoke test before marking the
Phase 0 frontend exit gate complete.

## 7. Phase 0 exit status

| Exit item | Status |
| --- | --- |
| Product/operations and engineering decisions | Complete |
| Backend focused baseline | Complete |
| Current polling, prior representative-volume evidence, and release target | Recorded |
| Rollback owner and procedure | Complete |
| Frontend type-check and full baseline | Pending trusted pnpm recovery |
| Future-phase deployment paths | Complete |

Phase 1 may use this decision record as its contract baseline. Production release remains gated on
the pending frontend baseline and each later phase's own compatibility, concurrency, performance,
accessibility, and rollout criteria.
