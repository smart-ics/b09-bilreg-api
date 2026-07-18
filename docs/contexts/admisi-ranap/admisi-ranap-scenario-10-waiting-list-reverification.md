# Scenario 10 — Waiting List continuation (re-verification)

**Date:** 2026-07-12  
**Verdict:** PASS  
**Scope:** Focused Scenario 10 only (not full Admission E2E)

## Environment

| Layer | Target |
| --- | --- |
| Frontend under test | Workspace Vite `http://localhost:5173/MyHospital/` (app `1.6.27`) — current Waiting List success-fork contract |
| Bilreg API | Deployed `http://dev.smart-ics.com:8089/BilregApi/api` (patched `GetActiveByRegId`) |
| Database | `HOSPITAL_HPL` on `dev.smart-ics.com` |
| IIS-deployed FE | `http://dev.smart-ics.com/MyHospital/` reports client version `1.4.4` — not used for this scenario (missing current Proses Registrasi Waiting List fork) |

## Identities

| Field | Value |
| --- | --- |
| Source (Opname Request) | `OPN0689IGD4A` |
| Patient | `347137300000057` |
| Registration `regId` | `RGA4LLI4LK` |
| Waiting List ID | `WTL0689IMW9Y` |

## Waiting List request

- **Endpoint:** `POST http://dev.smart-ics.com:8089/BilregApi/api/admisi-ranap/waiting-list`
- **Payload:**

```json
{
  "regId": "RGA4LLI4LK",
  "kelasId": "K04",
  "bangsalId": "R1",
  "priority": 3,
  "userId": "SPR001"
}
```

## HTTP response (create)

```json
{
  "status": "success",
  "code": "200",
  "data": {
    "waitingListId": "WTL0689IMW9Y"
  }
}
```

## Frontend checks

| Check | Result |
| --- | --- |
| Exactly one Waiting List POST on first create | Pass (`routePostHitsAfterFirstSuccess: 1`) |
| Request uses returned `regId` | Pass (`RGA4LLI4LK`) |
| Hospital `kelasId`, `bangsalId`, priority, `userId` present | Pass (`K04`, `R1`, `3`, `SPR001`) |
| Button pending/disabled while creating | Pass |
| Repeat click while pending does not send another request | Pass (Playwright click timed out on disabled button; hit count unchanged) |
| Success toast visible | Pass (`Pasien ditambahkan ke Waiting List`) |
| Repeat create after success | FE button re-enables; backend rejects (see below) |
| Browser console | No unexpected errors (only expected `400` network log on duplicate create) |

## Backend / database

Exactly one `BILRG_BedWaitingList` row:

| Column | Value |
| --- | --- |
| WaitingListId | `WTL0689IMW9Y` |
| RegId | `RGA4LLI4LK` |
| WaitingListStatus | `0` (Waiting) |
| KelasId | `K04` |
| BangsalId | `R1` |
| Priority | `3` |
| CrtUser | `SPR001` |

Admission after Waiting List create:

| Column | Value |
| --- | --- |
| RegId | `RGA4LLI4LK` |
| AdmissionStatus | `0` (Diadmit — unchanged; expected) |
| AdmissionSource | `0` |
| PasienId | `347137300000057` |

Audit:

- `BILRG_AuditLog`: `WaitingListModel` / `CREATE` / `WTL0689IMW9Y`
- `BILRG_AuditLog`: `AdmissionModel` / `CREATE` / `RGA4LLI4LK`
- Ward handover notification: V1 `NotifyHandOver` is a no-op (ADR-004 persistence/GET only); no separate notification row expected

## Duplicate create

Second browser POST after success:

```json
{
  "status": "Bad Request",
  "code": "400",
  "data": "Admission 'RGA4LLI4LK' sudah memiliki Waiting List aktif."
}
```

- Not the old null-source `422` (`Value cannot be null. (Parameter 'source')`)
- Still exactly one Waiting List row
- Concurrent API race probe also yielded one `200` + one `400` and a single DB row

## Cleanup

- Test records for `RGA4LLI4LK` / `OPN0689IGD4A` / `WTL0689IMW9Y` removed
- Retained registration `RGA4LISM7N` not modified

## Artifacts

- `c012_myhospital_web/e2e/artifacts/step6/scenario10/report.json`
- `c012_myhospital_web/e2e/artifacts/step6/scenario10/http_captures.json`
- `c012_myhospital_web/e2e/artifacts/step6/scenario10/db_evidence.json`
- `c012_myhospital_web/e2e/artifacts/step6/scenario10/scenario10-success.png`

## Final verdict

**PASS** — Waiting List continuation after Proses Registrasi works against the patched Bilreg API. The previous `GetActiveByRegId` null-source `422` is resolved.
