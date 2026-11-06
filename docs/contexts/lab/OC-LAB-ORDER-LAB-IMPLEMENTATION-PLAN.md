---
Title: Order Pemeriksaan Laboratorium Execution Registration & Billing Realization Implementation Plan
Code: OC-LAB-ORDER-LAB
Artifact: IMPLEMENTATION-PLAN
Version: 2.0
LastUpdated: 2026-11-06
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan realisasi teknis dan operasional lapangan untuk kapabilitas **Order Pemeriksaan Laboratorium** khususnya penyelesaian alur eksekusi order tertunda (*Deferred Order Execution*) dan resolusi penagihan billing (*Billing Resolution*) sesuai spesifikasi target arsitektur [OC-LAB-ORDER-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ARCHITECTURE.md) Version 1.1.

Tujuan operasional rencana pengganti (*replacement plan*) ini:
1. **Mengganti Placeholder `REG-FAKE-0001`**: Mengimplementasikan `LabRegIntegration` riil dengan alur hibrida (*Hybrid Model*):
   - *Pasien Luar (`OrderSource.ExternalPatient`)*: Menggunakan kembali `RegExternalId` sebagai `ExecutionRegId`.
   - *Pasien Internal RS (`OrderSource.Emr`)*:
     - Mengikat `ExecutionRegId` eksplisit jika dikirimkan oleh pemanggil.
     - Auto-binding jika ditemukan tepat 1 registrasi aktif hari ini untuk pasien di `_regAktifRepo`.
     - Validasi pemilihan jika ditemukan >1 registrasi aktif hari ini.
     - Proteksi jaminan BPJS/Asuransi: wajib mendaftar di Loket Admisi untuk penerbitan SEP legal.
     - Auto-generate registrasi penunjang laboratorium di `ta_registrasi` dan `BILRG_RegAktif` untuk pasien Umum (Layanan = Laboratorium dari konfigurasi `appsettings.json`, Dokter = DPJP Perujuk asal, Karcis = `KarcisType.Default` Rp 0).
2. **Menyelaraskan Resolusi Penagihan Kasir (`TRK-BILLING`)**:
   - Memperbarui `LabBillingIntegration.ChargeAdmisiPatient` dan `CancelCharge` agar memprioritaskan `ExecutionRegId` di atas `Patient.RegId` (`effectiveRegId = !string.IsNullOrWhiteSpace(order.ExecutionRegId) ? order.ExecutionRegId : order.Patient.RegId`).
3. **Memperluas Antarmuka Dialog Aktivasi Frontend (`c012_myhospital_web`)**:
   - Mengintegrasikan dialog `LabActivateDeferredDialog.vue` dengan endpoint registrasi aktif pasien hari ini (`GET /api/AdmisiContext/RegFeature/aktif/{pasienId}`), menyajikan pilihan interaktif multiregistrasi, dan memproteksi pasien BPJS agar tertib ke loket Admisi.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- ARCHITECTURE: [OC-LAB-ORDER-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ARCHITECTURE.md) (Version 1.1)
- FEASIBILITY-ASSESSMENT: [OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- ISSUE: [OC-LAB-ORDER-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ISSUE.md) (`ISSUE-LAB-ORDER-LAB-001`)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Target repositories:
- `b09-bilreg-api` (Backend Core API)
- `c012_myhospital_web` (Frontend HIS Web Application)

In Scope:
1. **Konfigurasi Backend**: Penambahan `LabOptions:LayananLabId` pada `appsettings.json` dan dependency injection.
2. **Kontrak Perintah & Handler**: Penambahan parameter opsional `ExecutionRegId` pada `LabOrderActivateDeferredCmd` dan `ILabRegIntegration`.
3. **Integrasi Registrasi Riil (`LabRegIntegration`)**: Deteksi registrasi aktif hari ini (`RegAktifRepo`), penolakan pasien BPJS tanpa SEP aktif, dan pembuatan registrasi penunjang mandiri untuk pasien Umum.
4. **Integrasi Billing (`LabBillingIntegration`)**: Resolusi `effectiveRegId` pada `Charge` dan `CancelCharge`.
5. **Web API Endpoint**: Verifikasi kontrak payload dan envelope `JSendOk` pada `LabOrderController.cs`.
6. **Frontend Dialog**: Peningkatan `LabActivateDeferredDialog.vue` untuk pemeriksaan registrasi aktif hari ini, seleksi multikunjungan, dan proteksi edukasi BPJS.
7. **Pengujian Otomatis**: Unit tests backend komprehensif di `Bilreg.Test` dan komponen tests di Vitest frontend.

Out of Scope:
1. Pembuatan fisik bridging SEP real-time ke web service BPJS VClaim (ditangani oleh Admisi / Jetli).
2. Perubahan master katalog tarif atau skema pembagian jasa kasir.

---

# 3. Dependencies

External dependencies:
- Framework .NET 8 / C# 12 (`b09-bilreg-api`).
- Dapper Micro-ORM & MediatR pipeline.
- Nuna Library: `ISequencerManual`, `TransHelper`.
- Vue 3 / Vite / TypeScript / Tailwind CSS (`c012_myhospital_web`).
- TanStack Query & Axios ApiService.

Slice dependency rules:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID yang valid dalam rencana ini.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED`.
- Dependensi merepresentasikan prasyarat implementasi, bukan relasi konseptual.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Backend Configuration & Options | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P2 - Backend Command & Contract Extension | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P3 - Backend Real Infrastructure Integration | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P4 - Backend API Verification | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P5 - Frontend Interactive Activation Dialog | NOT-STARTED | NOT-REVIEWED | 0/1 |

---

# 5. Phases

## P1 - Backend Configuration & Options

### P1-S01
Title: Add LabOptions Configuration for Default Laboratory Service ID
Implementation Status: IMPLEMENTED
Review Status: GO

Objective:
Menambahkan model opsi konfigurasi `LabOptions` dan binding konfigurasi di `appsettings.json` serta `Bilreg.Api/Configurations` dengan parameter `LayananLabId = "LAB"`, sehingga kode unit laboratorium dapat disesuaikan per instalasi rumah sakit tanpa hardcode.

Depends On: None
Repository: b09-bilreg-api

Completion Criteria:
1. Kelas `LabOptions` terbentuk di namespace `Bilreg.Application.LabContext.LabOrderFeature` atau `Bilreg.Infrastructure.LabContext`.
2. Opsi `LabOptions` didaftarkan pada service collection di `Bilreg.Api/Configurations/InfrastructureService.cs`.
3. File `appsettings.json` memuat seksi `"LabOptions": { "LayananLabId": "LAB" }`.
4. Build proyek `b09-bilreg-api` sukses tanpa peringatan.

Implementation Notes:
- Membuat model konfigurasi `LabOptions` di `Bilreg.Infrastructure.LabContext` dengan properti `LayananLabId = "LAB"`.
- Mendaftarkan opsi pada service container di `InfrastructureService.cs` melalui `services.Configure<LabOptions>(configuration.GetSection(LabOptions.SECTION_NAME))`.
- Menambahkan seksi `LabOptions` pada `appsettings.json` dan `appsettings.development.json`.
- Build proyek `Bilreg.Api.csproj` diverifikasi sukses tanpa error (0 errors, 0 warnings).

Changed Files:
- b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOptions.cs
- b09-bilreg-api/src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs
- b09-bilreg-api/src/bilreg/Bilreg.Api/appsettings.json
- b09-bilreg-api/src/bilreg/Bilreg.Api/appsettings.development.json

---

## P2 - Backend Command & Contract Extension

### P2-S02
Title: Extend Deferred Order Activation Command and Integration Contract
Implementation Status: IMPLEMENTED
Review Status: GO

Objective:
Memperluas kontrak `LabOrderActivateDeferredCmd` dan record `LabRegExecutionRequest` agar mendukung parameter opsional `string? ExecutionRegId = null`. Memperbarui `LabOrderActivateDeferredHandler` agar meneruskan nilai `ExecutionRegId` tersebut ke `ILabRegIntegration.CreateExecutionRegistration(...)`.

Depends On: None
Repository: b09-bilreg-api

Completion Criteria:
1. `LabOrderActivateDeferredCmd` memiliki parameter opsional `ExecutionRegId = null`.
2. `LabRegExecutionRequest` memiliki parameter opsional `ExecutionRegId = null`.
3. `LabOrderActivateDeferredHandler` mengirimkan `request.ExecutionRegId` ke `_labRegIntegration.CreateExecutionRegistration(...)`.
4. Unit test `LabOrderActivateDeferredHandlerTest.cs` memverifikasi penerusan `ExecutionRegId` ke `ILabRegIntegration`.

Notes:
- Parameter opsional mempertahankan kompatibilitas mundur (*backward compatibility*) dengan klien API eksisting.
- `LabRegExecutionRequest` diperluas dengan `string? ExecutionRegId = null`.
- `LabOrderActivateDeferredCmd` diperluas dengan `string? ExecutionRegId = null`.
- `LabOrderActivateDeferredHandler` meneruskan `request.ExecutionRegId` ke `_labRegIntegration.CreateExecutionRegistration(...)`.
- Unit test `LabOrderActivateDeferredHandlerTest` memverifikasi skenario default (`ExecutionRegId = null`) dan skenario dengan `ExecutionRegId` (semua 2 test passed).

Changed Files:
- b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/Integration/ILabRegIntegration.cs
- b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderActivateDeferredCmd.cs
- b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderActivateDeferredHandlerTest.cs

---

## P3 - Backend Real Infrastructure Integration

### P3-S03
Title: Implement Real Operational Hybrid Flow in LabRegIntegration
Implementation Status: IMPLEMENTED
Review Status: GO

Objective:
Menggantikan dummy sequencer `REG-FAKE-0001` pada `LabRegIntegration.cs` dengan alur operasional riil Hybrid Model:
1. Jika `order.OrderSource == LabOrderSourceEnum.ExternalPatient`: kembalikan `order.Patient.RegId` (`RegExternalId`).
2. Jika `order.OrderSource == LabOrderSourceEnum.Emr`:
   - Jika `request.ExecutionRegId` terisi: validasi bahwa registrasi tersebut aktif hari ini untuk pasien (`_regAktifRepo.LoadEntity`), kembalikan `ExecutionRegId`.
   - Jika `request.ExecutionRegId` kosong:
     - Kueri `_regAktifRepo.ListData(pasien)` yang bertanggal hari ini.
     - Jika tepat 1 registrasi: kembalikan `RegId` tersebut.
     - Jika > 1 registrasi: lempar `ValidationException` meminta pemilihan registrasi.
     - Jika 0 registrasi:
       - Muat registrasi asal (`_regRepo.LoadEntity(order.Patient.RegId)`).
       - Jika pasien BPJS/Asuransi (`TipeJaminanId != "00000"` / `JaminanId != "000"`): lempar `ValidationException` (*"Pasien BPJS/Asuransi wajib melakukan pendaftaran di Loket Admisi terlebih dahulu untuk penerbitan eligibilitas (SEP)."*).
       - Jika pasien Umum: buat `RegModel` baru dengan sequence `NOREG`, `Layanan` dari `LabOptions.LayananLabId`, `Dokter` dari DPJP perujuk asal, `KarcisType.Default` (Rp 0), simpan ke `_regRepo` dan `_regAktifRepo`, lalu kembalikan `newReg.RegId`.

Depends On: P1-S01, P2-S02
Repository: b09-bilreg-api

Completion Criteria:
1. `LabRegIntegration.cs` menyuntikkan `ILabOrderRepo`, `IRegAktifRepo`, `IRegRepo`, `ILayananRepo`, `ISequencerManual`, `IOptions<LabOptions>`, dan `ITglJamProvider`.
2. Seluruh alur Hybrid (External, 1 aktif, >1 aktif, 0 aktif BPJS, 0 aktif Umum) terimplementasi dengan guard yang ketat.
3. Unit test `LabRegIntegrationTest.cs` mencakup 5 skenario operasional tersebut dengan assert validasi registrasi di `_regRepo` dan `_regAktifRepo`.

Notes:
- Menggunakan `TransHelper.NewScope()` saat pembuatan registrasi penunjang baru agar penyimpanan `_regRepo` dan `_regAktifRepo` atomik.

Implementation Notes:
- Mengimplementasikan alur operasional Hybrid pada `LabRegIntegration.cs` dengan dependency injection lengkap (`ILabOrderRepo`, `IRegAktifRepo`, `IRegRepo`, `ILayananRepo`, `ISequencerManual`, `IOptions<LabOptions>`, `ITglJamProvider`).
- Menambahkan validasi `request.ExecutionRegId` eksplisit terhadap status aktif dan tanggal hari ini.
- Menangani auto-resolution untuk pasien EMR: auto-bind jika tepat 1 aktif, throw `ValidationException` jika > 1 aktif, dan deteksi tipe jaminan saat 0 aktif (edukasi pendaftaran Admisi untuk BPJS/asuransi vs auto-generate penunjang Umum secara atomik dengan `TransHelper`).
- Mendaftarkan dependensi `ISequencerManual`, `ILabOrderRepo`, `IRegAktifRepo`, dan `ILayananRepo` di `InfrastructureService.cs` serta penanganan `ValidationException` di `ErrorHandlerMiddleware.cs`.
- Menambahkan 7 unit tests di `LabRegIntegrationTest.cs` mencakup semua skenario operasional hibrida; seluruh pengujian lolos 100%.

Changed Files:
- b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabRegIntegration.cs
- b09-bilreg-api/src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs
- b09-bilreg-api/src/bilreg/Bilreg.Api/Configurations/ErrorHandlerMiddleware.cs
- b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabRegIntegrationTest.cs

---

### P3-S04
Title: Align Billing Resolution to Effective Registration in LabBillingIntegration and LabOrderCancelCmd
Implementation Status: IMPLEMENTED
Review Status: GO

Objective:
Menyelaraskan alur penagihan dan pembatalan penagihan pada `LabBillingIntegration.cs` dan `LabOrderCancelCmd.cs` agar merujuk ke nomor registrasi efektif (`effectiveRegId = !string.IsNullOrWhiteSpace(order.ExecutionRegId) ? order.ExecutionRegId : order.Patient.RegId`):
1. `ChargeAdmisiPatient`: memuat `LoadReg(RegModel.Key(effectiveRegId))` dan membuat transaksi billing di bawah `effectiveRegId`.
2. `CancelCharge`: membatalkan pembebanan tindakan pada Tata Rekening di bawah `effectiveRegId`.
3. `LabOrderCancelCmd`: meneruskan `effectiveRegId` ke `_labBillingIntegration.CancelCharge`.

Depends On: P2-S02
Repository: b09-bilreg-api

Completion Criteria:
1. `LabBillingIntegration.ChargeAdmisiPatient` menggunakan `effectiveRegId` saat order memiliki `ExecutionRegId`.
2. `LabBillingIntegration.CancelCharge` dan `LabOrderCancelCmd` menggunakan `effectiveRegId` saat membatalkan tagihan.
3. Unit tests di `LabBillingIntegrationTest.cs` dan `LabOrderCancelHandlerTest.cs` memverifikasi bahwa order dengan `ExecutionRegId` berhasil di-charge dan di-cancel pada akun registrasi eksekusi tersebut.

Notes:
- Menjaga garansi konsistensi finansial pada kasir aktif hari ini.

Implementation Notes:
- Di `LabBillingIntegration.cs`: Memperbarui `ChargeAdmisiPatient` untuk mengevaluasi `effectiveRegId` (`!string.IsNullOrWhiteSpace(order.ExecutionRegId) ? order.ExecutionRegId : order.Patient.RegId`), memuat `RegModel` dengan `effectiveRegId`, dan membebankan tagihan laboratorium ke registrasi efektif tersebut.
- Di `LabOrderCancelCmd.cs`: Memperbarui `LabOrderCancelHandler` agar menyelesaikan `effectiveRegId` dari order, memvalidasi billing lock dengan `EnsureCanModifyBillingAsync(effectiveRegId)`, dan membatalkan tagihan kasir via `_labBillingIntegration.CancelCharge(order.OrderId, effectiveRegId, request.UserId, request.Reason, occurredAt)`.
- Di `LabBillingIntegrationTest.cs`: Menambahkan test `ChargeOrder_AdmissionPatient_WithExecutionRegId_ChargesAgainstExecutionRegistration` untuk memverifikasi pembebanan item billing ke registrasi eksekusi saat order memiliki `ExecutionRegId`.
- Di `LabOrderCancelHandlerTest.cs`: Menginjeksikan mock `IBillingLockValidationService` dan menambahkan test `Handle_GivenChargedOrderWithExecutionRegId_CallsBillingIntegrationCancelChargeWithExecutionRegId` yang memverifikasi pembatalan tagihan kasir dan pemeriksaan billing lock ditujukan ke `executionRegId`.

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabBillingIntegration.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCancelCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabBillingIntegrationTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCancelHandlerTest.cs`
- `b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-IMPLEMENTATION-PLAN.md`

---

## P4 - Backend API Verification

### P4-S05
Title: Web API Endpoint Contract & End-to-End Test Verification
Implementation Status: IMPLEMENTED
Review Status: GO

Objective:
Memverifikasi endpoint REST `PATCH /api/LabContext/LabOrderFeature/activateDeferred` pada `LabOrderController.cs` agar menerima DTO `LabOrderActivateDeferredCmd` (dengan properti `ExecutionRegId` opsional) dan mengembalikan respons `JSendOk(LabOrderActivateDeferredResponse)`. Menjalankan regression check dan unit/integration tests pada `b09-bilreg-api`.

Depends On: P3-S03, P3-S04
Repository: b09-bilreg-api

Completion Criteria:
1. Endpoint `activateDeferred` di `LabOrderController.cs` menerima payload JSON `{ "orderId": "...", "userId": "...", "executionRegId": "..." }`.
2. Test fixture di `LabOrderControllerTest.cs` memverifikasi pemanggilan command dengan `ExecutionRegId`.
3. Seluruh unit test pada `Bilreg.Test/LabContext/LabOrderFeature` lulus 100%.

Notes:
- Jalankan `dotnet test` secara terarah pada proyek pengujian lab context.

Implementation Notes:
- Memverifikasi endpoint REST `PATCH /api/LabContext/LabOrderFeature/activateDeferred` pada `LabOrderController.cs` yang memetakan body `LabOrderActivateDeferredCmd` (dengan opsional `ExecutionRegId`) dan mengembalikan `Ok(new JSendOk(response))`.
- Menambahkan unit test di `LabOrderControllerTest.cs`:
  - `ActivateDeferredAction_HasExpectedHttpAttributes`: memvalidasi anotasi atribut `[HttpPatch("activateDeferred")]`.
  - `ActivateDeferred_GivenValidCommandWithExecutionRegId_DispatchesAndReturnsOkWithJSendOk`: memverifikasi pengiriman command dengan `ExecutionRegId` eksplisit ke mediator dan pengembalian respons JSendOk sukses.
  - `ActivateDeferred_GivenCommandWithoutExecutionRegId_DispatchesAndReturnsOkWithJSendOk`: memverifikasi pengiriman command tanpa `ExecutionRegId` (null) ke mediator dan pengembalian respons JSendOk sukses.
- Menjalankan regression tests:
  - `LabOrderControllerTest`: 18 tests passed (100%).
  - `LabContext.LabOrderFeature` (excluding Dal): 249 tests passed (100%).

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderControllerTest.cs`
- `b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-IMPLEMENTATION-PLAN.md`

---

## P5 - Frontend Interactive Activation Dialog

### P5-S06
Title: Enhance LabActivateDeferredDialog with Active Visit Detection and Selection
Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

Objective:
Memperbarui antarmuka dialog aktivasi deferred order `LabActivateDeferredDialog.vue` di `c012_myhospital_web`:
1. Saat dialog terbuka untuk order internal EMR:
   - Panggil endpoint `GET /api/AdmisiContext/RegFeature/aktif/{pasienId}` via TanStack Query.
2. Logika tampilan:
   - Jika ditemukan **> 1 registrasi aktif hari ini**: tampilkan radio button / select picker berisi daftar kunjungan (No Reg, Layanan/Poli, Dokter, Jam Masuk) dan wajibkan petugas memilih salah satu sebelum tombol Aktifkan ditekan.
   - Jika ditemukan **tepat 1 registrasi aktif hari ini**: tampilkan kotak informasi bahwa order akan ditautkan ke registrasi aktif tersebut (`{RegId} - {LayananName}`).
   - Jika **0 registrasi aktif hari ini**:
     - Jika pasien BPJS/Asuransi: tampilkan kotak peringatan kuning (*alert warning*) bahwa pasien wajib mendaftar ke Loket Admisi terlebih dahulu untuk penerbitan SEP, dan nonaktifkan tombol submit.
     - Jika pasien Umum: tampilkan info bahwa sistem akan otomatis menerbitkan registrasi kunjungan laboratorium baru hari ini.
3. Pada saat tombol konfirmasi ditekan, mutasi `useActivateDeferredMutation` mengirimkan payload `{ orderId, userId, executionRegId }`.

Depends On: P4-S05
Repository: c012_myhospital_web

Completion Criteria:
1. `LabOrderService.ts` memperbarui fungsi `activateDeferred` dengan parameter opsional `executionRegId`.
2. `LabActivateDeferredDialog.vue` mengimplementasikan UI state untuk 1 registrasi, >1 registrasi, 0 registrasi BPJS (blocked with message), dan 0 registrasi Umum.
3. Spec test `LabActivateDeferredDialog.spec.ts` memverifikasi rendering selector multiregistrasi dan penolakan submit saat BPJS tanpa registrasi aktif.
4. Verifikasi standar frontend: `npx prettier --write`, `pnpm tc:app`, dan `pnpm lint` lulus tanpa error.

Notes:
- Menggunakan komponen UI standar yang telah ada di `src/shared/components/ui/`.

---

# 6. Change Log

- 2026-11-06: Version 2.0 dirilis menggantikan Version 1.0 (Replacement Plan). Menambahkan realisasi operasional lapangan untuk Deferred Order Execution Registration (Hybrid Model), sinkronisasi penagihan kasir pada `effectiveRegId`, dan dialog aktivasi interaktif frontend.
