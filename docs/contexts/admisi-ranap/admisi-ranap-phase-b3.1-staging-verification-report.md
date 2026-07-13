# Release 1 Phase B3.1 — Journey API staging verification

**Date:** 2026-07-13  
**Decision:** **BLOCKED** — staging prerequisites were unavailable; this is not a demonstrated API, database, or business-rule defect.  
**Scope:** Read-only verification of the Phase B3 Rawat Inap Patient Journey API. No frontend work, data repair, data seeding, configuration change, or business-rule change was performed.

## 1. Environment and topology

| Item | Result |
|---|---|
| Verification host | Local development workspace (`D:\\Project.Aktif\\MyHospitalWeb\\b09-bilreg-api`) |
| Staging API base URL | `http://dev.smart-ics.com:8089/BilregApi` |
| Authenticated Bearer token | Not supplied / unavailable |
| `BILREG_JOURNEY_IT_SERVER` | Not set |
| `BILREG_JOURNEY_IT_DATABASE` | Not set |
| Staging database connection | Not attempted; unavailable |
| API-to-SQL network position | Not measurable |
| Developer-to-API network position | Not measurable |

`AdmisiRanap:JourneyEndpointsEnabled` defaults to `false` in the checked-in API configuration. It was not changed. The staging route is reachable, but the required staging-true setting could not be inspected or exercised without an authenticated client.

## 2. Integration-test results

Commands executed sequentially (to avoid a concurrent MSBuild test-manifest file lock):

```powershell
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore --filter "Category=JourneyDalIntegration"
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore --filter "Category=JourneyDalVolume"
```

| Suite | Result | Evidence |
|---|---:|---|
| `JourneyDalIntegration` | Blocked: 19 failed at setup | `JourneyDalTestEnv.RequireConfiguredConnection()` rejected the missing `BILREG_JOURNEY_IT_SERVER` and `BILREG_JOURNEY_IT_DATABASE` values. |
| `JourneyDalVolume` | Blocked: 3 failed at setup | Same explicit configuration error before a database connection/test record could be created. |
| Soft skip | No | Both suites fail loudly with `InvalidOperationException`; they do not skip. |
| Schema detection | Not run | Requires the configured staging connection. |
| Transaction rollback proof | Not run | `TransactionRollback_Leaves_No_Seeded_Records` could not begin without the target connection. |
| B1/B2/B2.1 DB parity/consolidation | Not run | Requires the configured staging connection. |

No operational records were seeded, voided, repaired, or cleaned. The test constructors failed before opening the required staging connection.

## 3. Local B3 contract regression evidence

The database-independent JourneyFeature suite was run:

```powershell
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore --filter "FullyQualifiedName~JourneyFeature&Category!=JourneyDalIntegration&Category!=JourneyDalVolume"
```

**Result: 65 passed, 0 failed.** This is supporting regression evidence only; it is not a staging substitute.

Covered behavior includes:

- disabled journey routes return the established `503`, while `/operational-worklist` remains available;
- no authentication returns `401`;
- invalid scope, terminal active stage, `IN_WARD`, bad cursor, date range, and out-of-range page size return `400`;
- unknown JourneyId/legacy record returns `404`, and explicit legacy ambiguity returns `409`;
- successful list responses expose `Server-Timing`, and detail uses the complete workspace contract;
- Waiting List `Accepted` resolves to handover accepted rather than bed assignment; no placement claims are present;
- Ward-owned work remains non-executable; bill items block cancellation; no unimplemented blocker is invented; and SQL-stage/B1 resolver parity unit cases pass.

The required real-data HTTP smoke cases could not be executed: no staging URL/token was available, so no real JourneyId could be selected from a list response and no legacy `{type}/{id}` could be responsibly chosen.

## 4. HTTP smoke and response-contract findings

| Requirement | Staging result |
|---|---|
| Active/history list, real detail, and legacy resolution | Not executed — no authenticated token |
| Feature flag `true` | Not verified in staging |
| Missing token `401` | **Verified on staging.** `GET /journeys?scope=active&pageSize=10`, `GET /journeys?scope=invalid`, and `GET /operational-worklist` each returned `401`. |
| Feature disabled `503`, invalid request `400`, unknown `404`, ambiguity `409`, `InWard` `400` | Verified by local B3 integration tests only; staging requires an authenticated token before validation. |
| Atiqa-like Admission + Waiting List consolidated as one journey | Not verified against staging data; covered by local/integration test design |
| Registration/Waiting List IDs confined to system/audit detail | Not verified against a real staging response |
| Facet exclusivity and active/history schema parity | Not verified against a real staging response |

## 5. Performance and DAL diagnostics

No API request was made. Therefore all cold and ten-warm measurements, medians, maxima, approximate p95 values, `Server-Timing` values, and B2.1 DAL diagnostics are **not available**.

| Scenario | Cold | Warm n=10 | Median / max / p95 | Server timing | DAL diagnostics |
|---|---:|---:|---|---|---|
| Active, page 10 / 50 | — | — | — | — | — |
| History, page 10 / 50 | — | — | — | — | — |
| Stage, patient-name, doctor/Bangsal filters | — | — | — | — | — |
| Detail and legacy resolution | — | — | — | — | — |

The intended diagnostic contract remains four database round trips for non-empty list pages, with facet, page, batch-hydration, resolver, and total DAL measurements. That invariant was not re-proven against staging.

Server-versus-client comparison and slow-query investigation are likewise not applicable: there is no request data indicating a server duration above two seconds. Do not begin B2.2/index work without that evidence.

## 6. Decision and exact blocker

**BLOCKED.** B3.1 cannot be classified PASS or PROVISIONAL PASS because the mandatory staging integration suites, authenticated HTTP smoke, response inspection, warm-request percentiles, DAL logs, and topology comparison were not executable.

Exact remaining blockers:

1. Confirmation that the supplied staging API is deployed with Phase B3 and `AdmisiRanap:JourneyEndpointsEnabled=true`.
2. A valid authenticated Bearer token.
3. `BILREG_JOURNEY_IT_SERVER` and `BILREG_JOURNEY_IT_DATABASE` pointing to a schema-complete staging database reachable from this verification environment (or execution from the staging API host).
4. Access to the B2.1 DAL diagnostic logs and, only if warm server timing exceeds about two seconds, DBA-supported actual plans plus `STATISTICS IO`/`STATISTICS TIME`.

Once available, rerun the two DAL categories, execute the specified HTTP matrix using a JourneyId selected from the list response, collect one cold plus ten warm samples per scenario from both network positions, and apply the B3.1 PASS/PROVISIONAL PASS/BLOCKED thresholds. F1/F2 frontend work must remain deferred until that verification passes.
