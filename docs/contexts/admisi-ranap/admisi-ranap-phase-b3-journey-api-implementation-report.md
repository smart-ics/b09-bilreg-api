# Release 1 Phase B3 — Journey API implementation report

## Delivered

Phase B3 adds the authenticated, feature-gated journey read contract without changing the existing
record-oriented worklist routes:

- `GET /api/admisi-ranap/journeys`
- `GET /api/admisi-ranap/journeys/{journeyId}`
- `GET /api/admisi-ranap/journeys/resolve?recordType={type}&recordId={id}`

The controller delegates list/detail resolution to the existing B2/B2.1 MediatR queries and
`IJourneyDal`. It neither reconstructs aggregate rows nor resolves operational stages. Detail
continues to preserve B1 resolver `CandidateAdmisiActions` separately from the billing-aware
`AllowedAdmisiActions` produced by `JourneyAllowedActionEvaluator`.

Legacy resolution supports Opname Request, Reservation, Admission/Registration, and Waiting List.
The Waiting List path resolves only through its recorded `RegId`; it never guesses from patient or
date attributes. Contradictory relationships return a `409` response containing structured
reconciliation issues and no selected `journeyId`.

## Contract and validation

List accepts `scope`, `stage`, `search`, `dokterId`, `bangsalId`, `kelasId`,
`tipeJaminanId`, `priority`, `dateFrom`, `dateTo`, `cursor`, and `pageSize`.

- `scope` is `active` or `history`; active cannot request terminal `Completed`/`Cancelled` stages.
- `stage` accepts only implemented Release 1 stages; deferred `InWard` is rejected.
- Dates must be ISO-8601 and `dateFrom <= dateTo`.
- Page size is `1..200`; cursors must use the existing opaque cursor format.
- Search and identifiers are length-bounded before reaching the DAL.
- Invalid input returns `400`; unknown journey/legacy records return `404`; ambiguous legacy
  relationships return `409`; unauthenticated requests retain the existing `401` behavior.

Successful list responses are the B2.1 `JourneyListResult` contract: one journey per item,
server-derived facets, `totalMatches`, `nextCursor`, `projectionVersion`, and `asOf`.
Successful detail responses are the complete B2.1 `JourneyDetailWorkspace` contract. No placement,
bed, occupancy, medication, documentation, transfer, or other invented blocker was added.

## Rollout and diagnostics

`AdmisiRanap:JourneyEndpointsEnabled` gates only the three new routes and defaults to `false`.
When disabled, they return the established `503` JSend service-disabled response; existing
`/operational-worklist` behavior is unchanged.

The new endpoints add a `Server-Timing: journey;dur=...` response metric for end-to-end API-host
duration. B2.1 list diagnostics remain available from `IJourneyDal.LastListDiagnostics` and are
logged by the DAL: fixed round trips, facet/page SQL time, batch hydration time, resolver time,
and total list time.

## Verification

Executed:

```powershell
dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-restore --filter "FullyQualifiedName~JourneyFeature&Category!=JourneyDalIntegration&Category!=JourneyDalVolume"
```

Result: **64 passed, 0 failed**. Coverage includes journey resolver/DAL unit behavior plus API
authentication, feature flag, validation, list/detail contract shape, unknown records, explicit
legacy ambiguity, billing-blocked cancellation, Ward non-executability, accepted handover, and
placement-unavailable representation.

The `JourneyDalIntegration` and `JourneyDalVolume` suites require
`BILREG_JOURNEY_IT_SERVER` and `BILREG_JOURNEY_IT_DATABASE`; they correctly fail fast when these
are absent and were not run against an unspecified database.

## API-path performance status

No cold/warm HTTP figures are recorded in this workspace. The required read-only staging API host,
authenticated client route, and `BILREG_JOURNEY_IT_*` integration environment are not configured.
Therefore Phase F1 frontend rollout remains **performance-blocked** pending these measurements:

1. cold request and ten warm requests for active/history lists at page sizes 10 and 50;
2. stage-filtered and search list requests; detail request;
3. client-to-API duration from API-host-near and developer-workstation clients;
4. `Server-Timing` and B2.1 facet/page/batch/resolver diagnostics for each run; and
5. execution plan plus `STATISTICS IO/TIME` if warm server execution exceeds about two seconds.

The existing transaction-scoped 500-record test remains a correctness/query-count aid, not the
HTTP latency gate.
