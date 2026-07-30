# Admisi Rajal High-Density Worklist — Phase 1 Implementation Summary

**Status:** Implementation complete; representative disposable-SQL performance evidence pending  
**Date:** 2026-07-25  
**Source roadmap:** `c012_myhospital_web/docs/_notes/admisi/admisi-rajal-high-density-worklist-implementation-roadmap.md`  
**Scope:** Additive active filtering, offset paging metadata, deterministic ordering, diagnostics,
and frontend client contracts. No visible layout, polling, schema, or database change.

## 1. Result

The composed Admisi Rajal officer worklist can now return only operationally active Queue Entries
and disclose whether another page exists. Existing consumers remain compatible: requests that do
not opt into paging metadata still receive the original array in the JSend `data` property.

The current Registrasi Rajal UI continues to use that legacy array path. A separate frontend query
hook and cache key prepare the high-density grid without enabling it in this phase.

## 2. API contract

Route:

```text
GET /api/v1/admisi-rajal/officer-worklist
```

Additive query parameters:

| Parameter | Default | Behavior |
| --- | ---: | --- |
| `activeOnly` | `false` | When true, returns only Waiting (`0`) and In Service (`1`). |
| `includePagingMetadata` | `false` | When true, returns the page object instead of the legacy array. |
| `offset` | `0` | Zero-based offset. |
| `limit` | `100` | Page size, constrained to `1..500`. |

`queueStatus` remains available for one explicit status. It cannot be combined with
`activeOnly=true`. The authenticated access boundary is unchanged.

Default compatibility response:

```json
{
  "status": "success",
  "data": []
}
```

Metadata response:

```json
{
  "status": "success",
  "data": {
    "items": [],
    "hasMore": true,
    "nextOffset": 100
  }
}
```

`nextOffset` is `null` when `hasMore` is false. `totalActive` is intentionally absent: completeness
is determined by fetching `limit + 1`, not by issuing a count query.

## 3. Ordering, validation, and diagnostics

- Active filtering is performed in SQL before offset paging, so Done and Withdrawn rows cannot
  consume an active page.
- Ordering remains Priority descending, CreatedAt ascending, and NoUrut ascending. AntrianId
  ascending is the final deterministic tie-breaker.
- Invalid dates, status values, offsets, limits, and conflicting active/status filters return the
  existing `AQ_INVALID_REQUEST` boundary.
- Structured logs record active mode, offset, limit, returned count, `hasMore`, queue-query time,
  enrichment time, and total time. They contain no patient, queue, Booking, Registration, Service
  Point, or Loket identifiers.

## 4. Frontend contract

The frontend adds `officerWorklistPageSchema`, `OfficerWorklistPage`, and a dedicated
`useOfficerWorklistPage()` query. That hook always sends `includePagingMetadata=true` and stores
paged data under a separate query key.

The existing `useOfficerWorklist()` and `useOfficerAdmissionQueue()` paths are unchanged. Phase 2
may display loaded counts using:

- complete page: `"{loaded} antrean dimuat"`;
- incomplete page: `"{loaded} antrean dimuat; masih ada antrean lain"`.

The loaded value must never be labelled as a server total.

## 5. Verification

Completed locally:

```powershell
dotnet test src\bilreg\Bilreg.Test\Bilreg.Test.csproj --filter "FullyQualifiedName~AdmisiRajalOfficerWorklistQueryTest|FullyQualifiedName~AdmissionQueueOperationalQueriesTest|FullyQualifiedName~AdmissionQueueApiContractTest" --no-restore
```

Result: **40 passed, 0 failed, 0 skipped**.

The broader focused Admission Queue and Registration-outcome regression selection also passed:
**51 passed, 0 failed, 0 skipped**.

```powershell
npx --no-install vitest run src/modules/Admisi/types/__tests__/admissionQueue.spec.ts src/core/api/__tests__/admissionQueueQueryKeys.spec.ts --maxWorkers=1 --no-file-parallelism
npx --no-install vue-tsc --noEmit --project tsconfig.app.json
```

Result: **12 tests passed** and the frontend application type-check completed successfully.

The fail-closed real-SQL gate now covers mixed active/final states, repeated offset paging without
duplicates or gaps, Statistics IO capture, and 20-run composed-query p95 validation against the
approved 2-second target. It was not executed because no `BILREG_AQ_IT_*` disposable SQL
configuration is available in this workspace. This evidence remains an explicit release gate. No
index was added speculatively; a measured failure creates the Phase 8 batching/index task.

## 6. Deployment and rollback

1. Deploy the backend first. No database migration is required.
2. Deploy the frontend client contract; it does not switch the current UI to the paged hook.
3. Keep the existing 15-second polling interval and 100-row default page.
4. Rollback requires no data action. The frontend can remain on the legacy array path, and the
   additive backend parameters can remain deployed safely.
5. If the API build must be reverted, older clients remain compatible because the default response
   and existing parameters were not changed.
