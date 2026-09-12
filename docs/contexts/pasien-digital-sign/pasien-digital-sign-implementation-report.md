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
| Test | `ResolvePatientSignerHandlerTest`, `HospitalApiKeyVerifierTest`, `DigitalSignHospitalControllerTest` — 23 kasus, lulus semua |

### MyHospital/Bilreg (BFF)
| Asset | Detail |
|---|---|
| Port | `IHiDokPatientSignerResolveClient` (`INunaService<Response, Request>`) + enum status + records |
| Query | `ResolvePatientSignerQuery` (MediatR), `hospitalId` dari `IGetProjectIdService.Execute()` (param `RS__XXXXXX_PROJECT_ID_`); untuk status gagal (404/428/502) melampirkan data sosial pasien via `IPasienRepo.LoadEntity` (`fs_mr = mr`) |
| Client | `HiDokPatientSignerResolveClient` (RestSharp, header `X-Api-Key`, parse JSend, cache sukses 30 menit) |
| Controller | `DigitalSignController` → `GET /api/digital-sign/patient-signers/resolve?mr={NoMR}`; success `JSendOk`; 404/428/502 `JSendModel` `data = { message, patient? }` (patient nested, key PascalCase) |
| Config | `HiDok:BaseApiUrl`, `HiDok:ApiKey` di `appsettings*.json` |
| Test | `HiDokPatientSignerResolveClientTest` — 8 kasus (WireMock): sukses+header, 404, 428 NotVerified, 502, 401, 500, cache, no-cache-for-NotVerified; `ResolvePatientSignerQueryHandlerTest` — 6 kasus; `DigitalSignControllerTest` — 5 kasus |

## 3. Verifikasi

| Check | Hasil |
|---|---|
| `dotnet build` Bilreg (Bilreg.Api) | Sukses (hanya warning pre-existing) |
| `dotnet test` → DigitalSign (Bilreg.Test) | 19/19 lulus (8 client + 6 handler + 5 controller) |
| HiDok `BackEnd` build | Sukses |
| HiDok `BackEnd.Test` + `WebApi` (VS MSBuild 18) | Sukses; 23/23 test DigitalSign (resolve/controller/key verifier) lulus |

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

## 7. Verified-gate implementation (7 September 2026)

| Item | Detail |
|---|---|
| Enum | `HiDokPatientSignerResolveStatus.NotVerified` added to `IHiDokPatientSignerResolveClient` |
| HiDok controller mapping | `DigitalSignHospitalController.cs` maps `NotVerified` → HTTP 428 JSend failed |
| b09 client mapping | `HiDokPatientSignerResolveClient` maps HTTP 428 → `NotVerified` with Indonesian message: "Pasien belum terverifikasi di RS ini. Silakan verifikasi kartu pasien (scan QR MR) di depan petugas admisi via aplikasi HiDok." |
| b09 controller mapping | `DigitalSignController` maps `NotVerified` → HTTP 428 JSend failed |
| Caching | `NotVerified` is **not** cached; only `Success` is cached (TTL 30 min) |
| Tests | `HiDokPatientSignerResolveClientTest` passes 8/8 (adds 428 NotVerified case and a no-cache-for-NotVerified case) |
| HiDokBackEnd tests | DigitalSign tests (DigitalSignHospitalControllerTest + ResolvePatientSignerHandlerTest) pass 18/18 |

## 8. Data sosial pasien pada respons error (11 September 2026)

Untuk kesalahan `NotFound`/`NotVerified`/`ProvisionFailed`, BFF menyertakan **data sosial pasien** agar frontend bisa menampilkan konteks pasien:

- Envelope: `data = { message, patient: { UserrID, NoMR, PasienName, Alamat, TglLahir, NoTelp, NoKTP, RSID } }`.
- `patient` hanya diisi bila pasien lokal (`tc_mr` by `fs_mr = mr`) ditemukan via `IPasienRepo.LoadEntity(PasienModel.Key(mr))`; bila tidak ditemukan, `data = { message }` saja.
- `NoMR` = `mr` apa adanya (tanpa normalisasi). `RSID` dari `IGetProjectIdService.Execute()` (param `RS__XXXXXX_PROJECT_ID_`). `TglLahir` format `dd-MM-yyyy`. `NoTelp` = kontak **Mobile** dulu, fallback **Phone**; nilai `-`/kosong → `""`. `Alamat` = gabungan baris alamat non-kosong.
- `UserrID` mengikuti `UserrId` yang dikembalikan HiDok (kosong pada status gagal).
- Controller: `DigitalSignController.cs` — `404`/`428`/`502` memakai `JSendModel` flat dengan `status=failed`, `code` = HTTP status, `data` nested di atas; key `patient` memakai PascalCase via `ResolvePatientSignerPatientDto`.
- Tests: `ResolvePatientSignerQueryHandlerTest` (pemetaan untuk 3 status, pasien lokal tidak ditemukan, success tanpa lookup, fallback no-telp) & `DigitalSignControllerTest` (nested envelope, message-only, 428, 502, success) — semua lulus.