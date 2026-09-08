# Pasien Digital Sign — Implementasi Signer Resolve (Implementation Report)

**Date:** 7 September 2026
**Status:** GO — terimplementasi di MyHospital (Bilreg) & HiDok; menunggu konfigurasi key produksi dan PenaEl provisioning

---

## 1. Ringkasan

Sinkronisasi identitas pasien untuk Digital Sign diimplementasikan sebagai **identity sync saja** (BFF): MyHospital menyediakan endpoint untuk klinik, lalu memanggil endpoint HiDok secara server-to-server untuk mendapatkan `SignerId` PenaEl. Create/publish Signing Request ke PenaEl, PIN, approve/reject, dan webhook **tidak termasuk** cakupan ini (dimiliki HiDok / PenaEl, direferensikan di dokumen HiDok).

## 2. Apa yang dibuat

### HiDok (server penyedia)
| Asset | Detail |
|---|---|
| `ResolvePatientSignerHandler` | `Resolve(hospitalId, mr)`; 15-char NoMR dinormalisasi `Substring(7)` dengan fallback; validasi; provision on-demand via `IPatientSigningParticipationService` |
| `HospitalApiKeyVerifier` | verifikasi `X-Api-Key` dengan perbandingan constant-time (`FixedTimeEquals`) |
| `DigitalSignHospitalController` | `GET api/digital-sign/patient-signers/resolve` `[AllowAnonymous]` + `X-Api-Key`; mapping 200/400/401/404/502/500 (JSend) |
| Konfigurasi | `Web.config` → `AppSettings["BilregApiKey"]`; registrasi DI di `WebApiConfig.cs` |
| Test | `ResolvePatientSignerHandlerTest`, `HospitalApiKeyVerifierTest`, `DigitalSignHospitalControllerTest` — 20 kasus, lulus semua |

### MyHospital/Bilreg (BFF)
| Asset | Detail |
|---|---|
| Port | `IHiDokPatientSignerResolveClient` (`INunaService<Response, Request>`) + enum status + records |
| Query | `ResolvePatientSignerQuery` (MediatR), `hospitalId` dari `IGetKodeRsService.Execute()` |
| Client | `HiDokPatientSignerResolveClient` (RestSharp, header `X-Api-Key`, parse JSend, cache sukses 30 menit) |
| Controller | `DigitalSignController` → `GET /api/digital-sign/patient-signers/resolve?mr={NoMR}`; Ok/404/502 JSend |
| Config | `HiDok:BaseApiUrl`, `HiDok:ApiKey` di `appsettings*.json` |
| Test | `HiDokPatientSignerResolveClientTest` — 6 kasus (WireMock): sukses+header, 404, 502, 401, 500, cache |

## 3. Verifikasi

| Check | Hasil |
|---|---|
| `dotnet build` Bilreg (Bilreg.Api) | Sukses (hanya warning pre-existing) |
| `dotnet test` → `HiDokPatientSignerResolveClientTest` | 6/6 lulus |
| HiDok `BackEnd` build | Sukses |
| HiDok `BackEnd.Test` + `WebApi` (VS MSBuild 18) | Sukses; 20/20 test resolve lulus |

## 4. Konfigurasi produksi (wajib)

1. Isi nilai **nyata** `HiDokOptions.ApiKey` (Bilreg) dan `AppSettings["BilregApiKey"]` (HiDok) — **jangan pernah commit**.
2. Nyalakan PenaEl provisioning: set `PenaElProvisionEnabled` = `1` di HiDok (saat ini `0` → pasien tanpa `SignerId` menerima `502`).

## 5. Keputusan & catatan (ADR-PDS-016)

- Opsi A (BFF) disetujui: identity sync, auth static `X-Api-Key`, provision on-demand, tanpa backfill.
- Rantai identitas: `Userr.UserrID` → `Userr.SignerId` ↔ PenaEl `users.hidok_user_id`.
- Akses BFF dilindungi JWT (authN klinik); panggilan ke HiDok server-to-server (tanpa kredensial pasien).
- PenaEl create (`allocate → upload → create` dan `patientSignerId`) tetap **di luar cakupan** repo ini — lihat `HiDokBackEnd/docs/contexts/pasien-digital-sign/digital-sign-contract-hospital-publish.md`.

## 6. Referensi

- `docs/contexts/pasien-digital-sign/pasien-digital-sign-resolve.md`
- `HiDokBackEnd/docs/contexts/pasien-digital-sign/digital-sign-contract-hospital-resolve.md`
- `HiDokBackEnd/docs/contexts/pasien-digital-sign/digital-sign-architecture.md` — ADR-PDS-016