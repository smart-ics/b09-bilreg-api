# Release 1 Phase B3.1 — Journey API staging verification

**Date:** 2026-07-13  
**Decision:** **PROVISIONAL PASS**  
**Scope:** Read-only verification of the Phase B3 Rawat Inap Patient Journey API against staging. No frontend work, data repair, seeding, configuration change, or business-rule change was performed.

Warm server-side list duration for page size 50 sits in the documented 1–2 second band with a fixed four database round trips. Correctness and parity gates passed. Recommend an initial client page size of **20–25** and continued monitoring before advertising a tighter list SLO.

## 1. Environment and topology

| Item | Result |
|---|---|
| Verification client | Developer workstation `JUDE7` |
| Staging API base URL | `http://dev.smart-ics.com:8089/BilregApi` (resolved `202.152.141.36`) |
| Authenticated Bearer token | Supplied for this verification session (value not recorded here) |
| `BILREG_JOURNEY_IT_SERVER` | `dev.smart-ics.com` |
| `BILREG_JOURNEY_IT_DATABASE` | `HOSPITAL_HPL` |
| Staging database | Same host/database as `Bilreg.Api` appsettings (`dev.smart-ics.com` / `HOSPITAL_HPL`) |
| API-host co-located client | Not available; all HTTP timings are workstation → staging API |
| Seq (`dev.smart-ics:5341` / `dev.smart-ics.com:5341`) | Unreachable from the verification host (DNS miss / connection refused) |

Interpretation of topology:

- `Server-Timing` is measured on the staging API host and is the authoritative server-side duration.
- Client totals from the workstation are higher by roughly 50–150 ms on warm list calls (network/client path), with occasional client outliers above 2.5 s while `Server-Timing` stayed stable.
- Direct DAL volume tests from the workstation under ambient `TransactionScope` were much slower (page50 ≈ 11 s) than API `Server-Timing` (page50 warm median ≈ 1.5 s). That gap supports “API nearer to SQL than the developer workstation,” not an API regression versus B2.1 ambient-TX profiling.

## 2. Feature-flag configuration

| Check | Result |
|---|---|
| Checked-in `AdmisiRanap:JourneyEndpointsEnabled` | `true` in `src/bilreg/Bilreg.Api/appsettings.json` |
| Staging behavior | Authenticated journey routes return `200` (not feature-disabled `503`) |
| Feature disabled → `503` | Verified by local `JourneyApiIntegrationTest.FeatureFlagDisabled_ReturnsEstablished503_AndOperationalWorklistRouteStillExists` (staging flag left enabled; not flipped in production/staging config) |
| `/operational-worklist` | Still present; authenticated `200` with existing worklist payload shape; unauthenticated `401` |

Do not enable this feature in production as part of B3.1.

## 3. Integration-test results

Commands (transaction-scoped; no operational seed/void/repair outside rollback):

```powershell
$env:BILREG_JOURNEY_IT_SERVER='dev.smart-ics.com'
$env:BILREG_JOURNEY_IT_DATABASE='HOSPITAL_HPL'
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "Category=JourneyDalIntegration"
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-build --filter "Category=JourneyDalVolume"
```

| Suite | Result | Evidence |
|---|---:|---|
| `JourneyDalIntegration` | **19 passed** | Schema gate, rollback, B1/B2/B2.1 parity, consolidation, fixed round trips, cancel blockers |
| `JourneyDalVolume` | **3 passed** | 500-journey pagination stability; timings printed; `rt=4` |
| Soft skip | None | Missing env/schema fails loud via `JourneyDalTestEnv` |
| Transaction rollback | Pass | `TransactionRollback_Leaves_No_Seeded_Records` |
| Supporting API contract suite | **12 passed** | `JourneyApiIntegrationTest` (503/401/400/404/409, Server-Timing, workspace without placement claims) |

Volume suite timings from the developer workstation (ambient `TransHelper` scope — **not** the API path):

| Metric | page10 | page50 | notes |
|---|---:|---:|---|
| Total list ms | 2729 | 11304 | Ambient TX + remote SQL from workstation |
| Facet+page / batch / resolve (page50 line) | facetPage50≈458; batch50≈10845; resolve50≈0 | | Same console line |
| Round trips | 4 | 4 | Fixed |

## 4. HTTP smoke results

Base: `GET http://dev.smart-ics.com:8089/BilregApi/api/admisi-ranap/...`  
Real JourneyId selected from active list (not constructed): `opn:OPN068P8YJPC` (Atiqa-like registered episode).

| Requirement | Staging result |
|---|---|
| `GET .../journeys?scope=active&pageSize=10` | **200** — 10 items, `totalMatches=17`, facets 10+5+2=17 |
| `GET .../journeys?scope=history&pageSize=10` | **200** — same list-item schema; history is full-corpus (active overlap expected) |
| `GET .../journeys/{journeyId}` | **200** — JourneyId from list |
| `GET .../journeys/resolve?recordType=OpnameRequest&recordId=...` | **200** — same JourneyId; no reconciliation |
| `GET .../journeys/resolve?recordType=Registration&recordId=...` | **200** — same JourneyId |
| Missing token | **401** on journeys and operational-worklist |
| Invalid scope / `InWard` / bad cursor / bad page size / bad `dateFrom` | **400** |
| Unknown JourneyId / unknown legacy record | **404** |
| Ambiguous legacy → **409** | No contradictory specimen in the live snapshot; covered by `JourneyApiIntegrationTest.Resolver_MapsLegacyAndMakesAmbiguityExplicit` |
| Feature disabled → **503** | Local API integration only (staging flag true) |
| `/operational-worklist` unchanged | Authenticated success; route still distinct from journeys |

## 5. Response-contract findings

Representative journey: Atiqa search (`search=ATIQA`) returns exactly one journey (`opn:OPN068P8YJPC`).

| Check | Result |
|---|---|
| Admission + Waiting List as one journey | **Pass** — one row; `systemAudit` has `regId` + one `waitingListIds` entry; timeline shows Opname → Admission → Waiting List |
| Registration / Waiting List IDs vs audit | API carries `regId` on list items and `identity.regId`, `handover.summary.activeWaitingListId`, and timeline `relatedRecordId`, in addition to `systemAudit`. This matches the Release 1 projection groups in the workspace plan (Identity / handover / timeline / audit). **UI must still confine raw ID display to Detail Sistem & Audit** (F3–F4). Not treated as an operational correctness defect. |
| Waiting List `Accepted` ≠ bed assignment | **Pass** — Atiqa active WL status is `Waiting` (0) → stage `WardAcceptanceRequired` (“Menunggu penerimaan bangsal”). Placement is explicitly unavailable (Ward Management not implemented). No room/bed/occupancy/transfer/`IN_WARD` fields. |
| Ward-owned task `canExecute=false` | **Pass** — next task / candidate action “Terima handover akomodasi”, owner domain Ward |
| Cancellation candidate vs allowed | **Pass** — distinct `candidateAdmisiActions` and `allowedAdmisiActions` arrays present |
| Tata Rekening bill items block cancellation | No registered active specimen with billing items in this snapshot (all sampled Cancel actions `canExecute=true`). Covered by DAL IT `CancelAdmission_NotExecutable_When_BillingItems_Exist` and API unit coverage. |
| No nonexistent cancellation blocker | **Pass** — no invented bed/medication/documentation/transfer blockers observed |
| Active/history list schema parity | **Pass** — same list-item keys |
| Stage facet exclusivity | **Pass** — facet sum 17 = `totalMatches` 17 |

## 6. Performance measurements

Method: one cold + ten warm requests per scenario from the developer workstation via `curl`.  
`Server-Timing` header: `journey;dur={ms}` (API host).  
Resolve endpoint does not emit `Server-Timing` (controller only stamps list/detail).

Nearest-rank approximate p95 over n=10 warm samples.

### 6.1 Cold and warm summary

| Scenario | Cold client / server (ms) | Warm client med / max / p95 | Warm server med / max / p95 |
|---|---:|---:|---:|
| Active page 10 | 765 / 685 | 743.5 / 807 / 807 | **674 / 682 / 682** |
| Active page 50 | 1659 / 1532 | 1627.5 / 2697 / 2697 | **1519 / 1551 / 1551** |
| History page 10 | 862 / 675 | 753.5 / 952 / 952 | **667.5 / 687 / 687** |
| History page 50 | 1560 / 1493 | 1608 / 2712 / 2712 | **1516.5 / 1563 / 1563** |
| Stage `WardAcceptanceRequired` | 583 / 468 | 521.5 / 557 / 557 | **459 / 501 / 501** |
| Search `ATIQA` | 661 / 602 | 298.5 / 317 / 317 | **241.5 / 245 / 245** |
| Filter `dokterId` | 1187 / 1113 | 301 / 1354 / 1354 | **240 / 256 / 256** |
| Filter `bangsalId` | 1578 / 1515 | 515.5 / 561 / 561 | **453.5 / 468 / 468** |
| Detail (selected JourneyId) | 2791 / 1679 | 1793.5 / 2834 / 2834 | **1722.5 / 1751 / 1751** |
| Resolve OpnameRequest | 1760 / — | 1800.5 / 2842 / 2842 | — (no Server-Timing) |

Page size 50 is reported explicitly; it is not hidden behind page size 10.

### 6.2 Server versus client

| Observation | Implication |
|---|---|
| Warm page50 server median ≈ 1.52 s; client median ≈ 1.63 s | Small network/client overhead on the workstation path |
| Occasional client max ≈ 2.7 s with server still ≈ 1.55 s | Client/network jitter, not server regression |
| Workstation ambient-TX DAL page50 ≈ 11 s vs API Server-Timing ≈ 1.5 s | Do not use ambient-TX remote IT timings as the API SLO |

API-host-local HTTP replay was not possible in this session.

### 6.3 DAL diagnostic breakdown

Live Seq ingestion of `JourneyDal.List` logs was unreachable from the verification host.

Authoritative substitutes:

| Source | Facet/page | Batch hydration | Resolver | Total | Round trips |
|---|---:|---:|---:|---:|---:|
| Volume IT console (ambient TX, workstation→SQL, page50) | ≈458 ms (facet+page) | ≈10845 ms | ≈0 ms | ≈11304 ms | **4** |
| Integration `QueryCount_Is_Constant_Across_PageSizes_1_10_50` | — | — | — | — | **4** for sizes 1/10/50 |
| Staging API `Server-Timing` (no ambient TX) | not split in header | not split | not split | list page50 warm med **1519 ms** | assumed 4 (unchanged DAL) |

Staging warm list `Server-Timing` never exceeded ~2 s (page50 warm max **1551 ms**). Per B3.1 rules, actual execution plans / `STATISTICS IO` / `STATISTICS TIME` were **not** captured and no index was added.

## 7. Slow-query investigation

**Not required.** Warm server duration for list page sizes 10 and 50 stayed under the ~2 s investigation threshold. Detail warm server median was ~1.72 s (also under threshold). Round-trip count remains fixed at four in IT.

## 8. Decision and remaining limitations

**PROVISIONAL PASS.**

Reasons:

1. Warm server-side list duration is operationally usable; page10 ≈ 0.67 s, page50 ≈ 1.52 s (1–2 s band).
2. Database round trips remain fixed at four; B1/B2/B2.1 parity and consolidation IT passed without soft-skip.
3. HTTP smoke, Atiqa consolidation, placement-unavailable semantics, Ward non-executability, facet exclusivity, and validation status codes behave as specified.
4. No warm server list duration above ~2 s, so B2.2 / index work is **not** started.

Documented limitations (not blockers for F1/F2 foundation work):

1. Recommend initial FE page size **20–25**; keep page50 as an explicit monitored path.
2. Seq DAL facet/batch/resolver split was not harvested from the live API host; re-check after Seq access is restored.
3. API-host co-located client timings were not collected.
4. Live **409** ambiguity and billing-blocked cancel specimens were absent from the snapshot; covered by automated tests.
5. Frontend must not surface raw Registration/Waiting List IDs in the normal list/workspace chrome; confine display to Detail Sistem & Audit even though the projection payload includes designed identity/handover/timeline references.

### Exact remaining blocker

None for starting **F1 + F2** (schemas/services/query state, JourneyId selection, legacy URL migration).

Do **not** begin the worklist redesign / unified right panel (**F3–F4**) until F1/F2 are verified on this provisional performance envelope. Do **not** begin a focused B2.2 optimization unless monitoring shows warm server list duration climbing back above ~2 s.
