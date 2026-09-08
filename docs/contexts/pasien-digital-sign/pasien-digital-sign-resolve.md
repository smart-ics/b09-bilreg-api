# Pasien Digital Sign — Signer Resolve (MyHospital/Bilreg BFF)

**Status:** GO (implemented) — 7 September 2026
**Context:** MyHospital backend obtains a provisioned PenaEl Patient Signer for a Hospital (RSID) + NoMR before a Hospital publishes a PenaEl Signing Request.

This document covers only the **identity sync** capability (BFF). PenaEl create/allocate/upload, PIN, approve, reject, and webhooks are **out of scope** and owned by HiDok (`HiDokBackEnd/docs/contexts/pasien-digital-sign/digital-sign-contract-hospital-*.md`).

---

## 1. Position in the flow

```text
Klinik/HIS frontend                       MyHospital (Bilreg)                   HiDok
        │  resolve(mr)                        │                                 │
        │ ───────────────────────────────────▶│  GET /api/digital-sign/          │
        │                                     │      patient-signers/resolve     │
        │                                     │      ?hospitalId=<RSID>&mr=       │
        │                                     │      header X-Api-Key            │
        │                                     │ ────────────────────────────────▶│
        │                                     │  200 { UserrId, SignerId }        │ ◀── provision on-demand
        │  200 { UserrId, SignerId }          │                                 │
        │ ◀───────────────────────────────────│                                 │
```

The BFF **never** forwards a Patient credential; the HiDok call is server-to-server with a static API key (`ADR-PDS-016`).

## 2. Contract summary

| Item | Value |
|---|---|
| HiDok endpoint | `GET /api/digital-sign/patient-signers/resolve?hospitalId={RSID}&mr={NoMR}` |
| HiDok auth | header `X-Api-Key` (shared secret, `HiDokOptions.ApiKey`) |
| BFF endpoint | `GET /api/digital-sign/patient-signers/resolve?mr={NoMR}` (JWT-authorized) |
| Success | `200` `JSendOk { UserrId, SignerId }` |
| Failures | `404` not registered (`JSendFailed`), `502` provision failed, `400` validation |
| Out-of-scope here | PenaEl allocate/upload/create, PIN, approve/reject, webhook sync |

## 3. Local RSID resolution

The BFF resolves the caller's own hospital RSID via `IGetKodeRsService.Execute()` and passes it as `hospitalId` — the caller does not assert an arbitrary hospital identity.

## 4. Files

| Concern | Path |
|---|---|
| Port (Application) | `Bilreg.Application/PasienContext/DigitalSignFeature/IHiDokPatientSignerResolveClient.cs` |
| MediatR query | `Bilreg.Application/PasienContext/DigitalSignFeature/ResolvePatientSignerQuery.cs` |
| Client (Infrastructure) | `Bilreg.Infrastructure/PasienContext/DigitalSignFeature/HiDokPatientSignerResolveClient.cs` (RestSharp + cache) |
| BFF controller (Api) | `Bilreg.Api/Controllers/PasienContext/DigitalSignFeature/DigitalSignController.cs` |
| Config | `HiDokOptions.BaseApiUrl`, `HiDokOptions.ApiKey` (`appsettings*.json`) |
| Tests | `Bilreg.Test/PasienContext/DigitalSignFeature/HiDokPatientSignerResolveClientTest.cs` (6 cases, all pass) |

## 5. Caching

Successful resolves are cached in-memory (`IMemoryCache`, key `hidok:patient-signer:{hospitalId}:{NoMR}`, TTL 30 min). Failures are not cached.

## 6. Config/ops notes

- `HiDokOptions.ApiKey` must match HiDok `Web.config` → `AppSettings["BilregApiKey"]` (never commit real key).
- HiDok gates PenaEl provisioning behind `PenaElProvisionEnabled` (currently `0`) — while disabled, resolve returns `502` for patients without an existing `SignerId`.
- Reference: `HiDokBackEnd/docs/contexts/pasien-digital-sign/digital-sign-contract-hospital-resolve.md`, ADR-PDS-016.