---
Title: Implementation Plan - Publikasi Event Registrasi Pasien ke RabbitMQ
Code: ADMISI-REG-EVENT-RABBITMQ
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-05
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan arsitektur target yang telah disetujui pada [ADMISI-REG-EVENT-RABBITMQ-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ARCHITECTURE.md) (V1.0) untuk merealisasikan CHANGE-REQUEST [ADMISI-REG-EVENT-RABBITMQ-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ISSUE.md) (`ISSUE-BILREG-ADMISI-EVENT-001`):
Memindahkan tanggung jawab penerbitan event notifikasi registrasi pasien (Rawat Jalan, Rawat Inap, dan Rawat Darurat/IGD) dari komponen deprecated `BipubApi` langsung ke dalam solusi `b09-bilreg-api` melalui message broker RabbitMQ dengan menggunakan pustaka `MassTransit` dan kontrak pesan `MyHospital.MsgContract`.

Referenced artifacts:

- ARCHITECTURE: [ADMISI-REG-EVENT-RABBITMQ-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ARCHITECTURE.md) (V1.0)
- FEASIBILITY-ASSESSMENT: [ADMISI-REG-EVENT-RABBITMQ-FEASIBILITY-ASSESSMENT.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-FEASIBILITY-ASSESSMENT.md) (Status: READY-FOR-PLANNING)
- ISSUE: [ADMISI-REG-EVENT-RABBITMQ-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ISSUE.md) (`ISSUE-BILREG-ADMISI-EVENT-001`)
- DOMAIN Rajal: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)
- DOMAIN Ranap: [admisi-ranap-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Target repository:
- `b09-bilreg-api` (Seluruh fase dan slice menargetkan satu repositori ini).

In scope (bagian arsitektur yang direalisasikan):
- Penambahan referensi paket NuGet `MassTransit`, `MassTransit.RabbitMQ`, dan `MyHospital.MsgContract` pada solusi Bilreg.
- Pembuatan interface abstraksi `IAdmisiEventPublisher` pada layer `Bilreg.Application` (`AdmisiContext.RegFeature`).
- Implementasi adapter publisher `AdmisiEventPublisher` pada layer `Bilreg.Infrastructure` (`AdmisiContext.RegFeature`) dengan pembungkus resilience (`try-catch` dan error logging) serta mapping event `RegRajalCreatedNotifEvent` dan `RegRanapCreatedNotifEvent`.
- Konfigurasi opsi RabbitMQ (`RabbitMqOption`) pada `appsettings.json` serta registrasi MassTransit dan dependency injection service pada startup `Bilreg.Api`.
- Integrasi pemicuan event pasca-commit transaksi (`trans.Complete()`) pada use case handlers:
  - Rawat Jalan Walk-In (`RegJalanCreateHandler` via `RegJalanWalkInCommand`)
  - Rawat Jalan Booking (`RegJalanByBookingHandler` via `RegJalanByBookingCmd`)
  - Rawat Darurat / IGD (`RegDaruratCreateHandler` via `RegDaruratCreateCmd`)
  - Rawat Inap Opname & Reservasi (`AdmissionRegistrationOrchestrator` via `AdmProcessOpnameRequestCmd` dan `AdmProcessReservationCmd`)
- Pembuatan dan pembaruan unit test komprehensif pada use cases dan publisher adapter dengan verifikasi skenario sukses serta ketahanan terhadap kegagalan broker (resilience isolation).

Out of scope (arsitektur §3 Excluded):
- Publikasi event pembatalan registrasi / pembatalan admisi (dikecualikan sesuai keputusan OQ-003).
- Pembuatan tabel transactional outbox baru dan background worker outbox dispatcher di Bilreg (dikecualikan sesuai keputusan GAP-004 & OQ-002).
- Pembuatan tipe event contract khusus IGD (IGD menggunakan kontrak `RegRajalCreatedNotifEvent` sesuai GAP-002 & OQ-001).
- Pembuatan atau modifikasi event consumer pada sistem downstream.

---

# 3. Dependencies

External dependencies:
- RabbitMQ broker host telah aktif dan dapat diakses (misal host pengujian/staging `dev.smart-ics.com`).
- NuGet package `MyHospital.MsgContract` (versi 2.1.24) tersedia di package source / cache lokal.
- Paket `MassTransit` dan `MassTransit.RabbitMQ` versi 8.x kompatibel dengan runtime .NET 8.0.

Slice dependencies:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID yang valid dalam rencana ini.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED` dan artefak keluaran terverifikasi di repositori.
- Kepuasan dependensi tidak mensyaratkan status review `GO`.
- Slices yang tidak memiliki dependensi satu sama lain dapat dikerjakan secara paralel (Dependency-Driven Parallelism).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Foundation & Contracts | IMPLEMENTED | GO | 2/2 |
| P2 - Infrastructure & Configuration | IMPLEMENTED | GO | 2/2 |
| P3 - Application Integration (Rajal & IGD) | IMPLEMENTED | GO | 2/2 |
| P4 - Ranap Integration & Solution Verification | IMPLEMENTED | GO | 2/2 |

---

# 5. Phases

## P1 - Foundation & Contracts

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

Fase ini meletakkan fondasi pustaka perpesanan dan kontrak antarmuka Clean Architecture yang dibutuhkan oleh layer aplikasi dan infrastruktur.

### P1-S01

Title: Penambahan Dependensi MassTransit dan MsgContract

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan referensi NuGet package `MassTransit` dan `MassTransit.RabbitMQ` (versi 8.x yang kompatibel dengan .NET 8.0) ke `Bilreg.Infrastructure.csproj` dan `Bilreg.Api.csproj`, serta package `MyHospital.MsgContract` (versi 2.1.24) ke `Bilreg.Infrastructure.csproj`.

Depends On: None  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- `src/bilreg/Bilreg.Infrastructure/Bilreg.Infrastructure.csproj` memuat package reference `MassTransit`, `MassTransit.RabbitMQ`, dan `MyHospital.MsgContract`.
- `src/bilreg/Bilreg.Api/Bilreg.Api.csproj` memuat package reference `MassTransit` dan `MassTransit.RabbitMQ`.
- Eksekusi `dotnet restore` berhasil tanpa konflik dependensi atau peringatan downgrade yang memblokir build.
- Namespace `MyHospital.MsgContract.Billing.AdmisiEvents` dapat diakses dari `Bilreg.Infrastructure`.

Notes:  
Mengacu pada keputusan TD-01 dan TD-02. Jangan menambahkan `MyHospital.MsgContract` ke `Bilreg.Application` untuk mematuhi prinsip Clean Architecture.  
- Implementation Notes:
  - Added package references `MassTransit` (v8.2.5), `MassTransit.RabbitMQ` (v8.2.5), and `MyHospital.MsgContract` (v2.1.24) to `Bilreg.Infrastructure.csproj`.
  - Added package references `MassTransit` (v8.2.5) and `MassTransit.RabbitMQ` (v8.2.5) to `Bilreg.Api.csproj`.
  - Executed `dotnet restore` and confirmed clean restore without dependency conflicts or downgrade warnings.
  - Verified accessibility of `MyHospital.MsgContract.Billing.AdmisiEvents` namespace types (`RegRajalCreatedNotifEvent`, `RegRanapCreatedNotifEvent`) from `Bilreg.Infrastructure`.
  - Built `Bilreg.Infrastructure` and `Bilreg.Api` successfully with 0 errors.
- Changed Files:
  - `src/bilreg/Bilreg.Infrastructure/Bilreg.Infrastructure.csproj`
  - `src/bilreg/Bilreg.Api/Bilreg.Api.csproj`

---

### P1-S02

Title: Pembuatan Interface Abstraksi IAdmisiEventPublisher

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mendefinisikan antarmuka abstraksi `IAdmisiEventPublisher` di namespace `Bilreg.Application.AdmisiContext.RegFeature` agar use case handler dapat mempublikasikan event tanpa mengetahui detail teknis RabbitMQ atau MassTransit.

Depends On: None  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- Berkas `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IAdmisiEventPublisher.cs` dibuat dengan isi antarmuka:
  ```csharp
  namespace Bilreg.Application.AdmisiContext.RegFeature;

  public interface IAdmisiEventPublisher
  {
      Task PublishRajalCreatedAsync(string regId, CancellationToken cancellationToken = default);
      Task PublishRanapCreatedAsync(string regId, CancellationToken cancellationToken = default);
  }
  ```
- Proyek `Bilreg.Application` berhasil dikompilasi tanpa adanya dependensi ke paket `MassTransit`.

Notes:  
Mengacu pada keputusan TD-03. S02 independen dari S01 dan dapat dikerjakan secara paralel.  
- Implementation Notes:
  - Created `IAdmisiEventPublisher` interface in `Bilreg.Application.AdmisiContext.RegFeature` namespace declaring `PublishRajalCreatedAsync(string regId, CancellationToken cancellationToken = default)` and `PublishRanapCreatedAsync(string regId, CancellationToken cancellationToken = default)`.
  - Verified `Bilreg.Application` has no dependency on `MassTransit` or `MyHospital.MsgContract`, keeping Clean Architecture boundaries intact.
  - Compiled `Bilreg.Application` and the full solution `b09-bilreg-api.sln` successfully with 0 errors.
- Changed Files:
  - `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IAdmisiEventPublisher.cs`

---

## P2 - Infrastructure & Configuration

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

Fase ini mengimplementasikan adapter publisher RabbitMQ menggunakan MassTransit, mekanisme isolasi kesalahan (resilience), konfigurasi runtime, dan pendaftaran Dependency Injection.

### P2-S03

Title: Implementasi AdmisiEventPublisher & Error Isolation di Infrastructure

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mengimplementasikan kelas `AdmisiEventPublisher` pada `Bilreg.Infrastructure.AdmisiContext.RegFeature` yang mengimplementasikan `IAdmisiEventPublisher`, memetakan event ke `RegRajalCreatedNotifEvent` dan `RegRanapCreatedNotifEvent`, serta mengisolasi kegagalan koneksi RabbitMQ menggunakan `try-catch` dan pencatatan error log terstruktur melalui `ILogger<AdmisiEventPublisher>`, disertai pembuatan unit test `AdmisiEventPublisherTest`.

Depends On: P1-S01, P1-S02  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- Berkas `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/AdmisiEventPublisher.cs` dibuat dan menginjeksi `MassTransit.IBus` serta `ILogger<AdmisiEventPublisher>`.
- Metode `PublishRajalCreatedAsync` mengeksekusi `_bus.Publish(new RegRajalCreatedNotifEvent(regId), cancellationToken)`.
- Metode `PublishRanapCreatedAsync` mengeksekusi `_bus.Publish(new RegRanapCreatedNotifEvent(regId), cancellationToken)`.
- Blok `try-catch` menangani `Exception`: mencatat log error kontekstual dan tidak melempar kembali (*swallow exception*) agar tidak mengganggu flow bisnis.
- Berkas pengujian unit dibuat di `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/AdmisiEventPublisherTest.cs` yang memverifikasi:
  1. Sukses publish memanggil `IBus.Publish` dengan tipe event dan `RegId` yang sesuai.
  2. Kegagalan `IBus.Publish` (melempar exception) ditangkap dengan aman, mencatat log error, dan tidak melempar unhandled exception.
- Seluruh pengujian di `AdmisiEventPublisherTest` berstatus PASS.

Notes:  
Mengacu pada keputusan TD-05 dan arsitektur §9 (Resilience & Logging).  
- Implementation Notes:
  - Created `AdmisiEventPublisher` in `Bilreg.Infrastructure.AdmisiContext.RegFeature` implementing `IAdmisiEventPublisher`.
  - Injected `MassTransit.IBus` and `Microsoft.Extensions.Logging.ILogger<AdmisiEventPublisher>`.
  - Implemented `PublishRajalCreatedAsync` and `PublishRanapCreatedAsync` with structured try-catch exception handling and error logging to ensure failure isolation (swallow exception so business flow is not interrupted).
  - Created unit tests in `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/AdmisiEventPublisherTest.cs` covering success publishing (verifying event type and `RegId`) and broker failure isolation (verifying exception swallowing and error logging).
  - All 4 unit tests in `AdmisiEventPublisherTest` executed and PASSED.
- Changed Files:
  - `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/AdmisiEventPublisher.cs`
  - `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/AdmisiEventPublisherTest.cs`

---

### P2-S04

Title: Konfigurasi RabbitMQ dan Registrasi MassTransit pada DI Container

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan section konfigurasi `RabbitMqOption` ke dalam `Bilreg.Api/appsettings.json`, membuat konfigurasi MassTransit RabbitMQ di `Bilreg.Api.Configurations`, serta mendaftarkan `IAdmisiEventPublisher` ke `AdmisiEventPublisher` pada DI container ASP.NET Core.

Depends On: P2-S03  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- Section `RabbitMqOption` ditambahkan ke `src/bilreg/Bilreg.Api/appsettings.json`:
  ```json
  "RabbitMqOption": {
    "Server": "dev.smart-ics.com",
    "UserName": "hospitalx",
    "Password": "intersoftindo"
  }
  ```
- Ekstensi registrasi konfigurasi MassTransit dibuat di `src/bilreg/Bilreg.Api/Configurations/MassTransitConfiguration.cs` (atau terintegrasi via DI helper di API layer) yang membaca konfigurasi host, username, dan password serta mengonfigurasi `UsingRabbitMq`.
- Registrasi DI untuk `IAdmisiEventPublisher` diarahkan ke `AdmisiEventPublisher` (Scoped / Transient).
- Pemanggilan konfigurasi diaktifkan pada `src/bilreg/Bilreg.Api/Program.cs` atau `InfrastructureService.cs`.
- Proyek `Bilreg.Api` berhasil dikompilasi dan DI container dapat resolve `IAdmisiEventPublisher`.

Notes:  
Mengacu pada arsitektur §5 (`MassTransitConfiguration`) dan preseden konfigurasi dari `BipubApi`.  
- Implementation Notes:
  - Added `RabbitMqOption` section to `src/bilreg/Bilreg.Api/appsettings.json` and `src/bilreg/Bilreg.Api/appsettings.development.json` with host `dev.smart-ics.com`, user `hospitalx`, and password `intersoftindo`.
  - Created configuration POCO `RabbitMqOption` class in `src/bilreg/Bilreg.Api/Configurations/RabbitMqOption.cs`.
  - Created extension method `AddMassTransitConfiguration` in `src/bilreg/Bilreg.Api/Configurations/MassTransitConfiguration.cs` configuring MassTransit RabbitMQ (`UsingRabbitMq`) with host and credentials from `RabbitMqOption`, kebab-case endpoint naming, and registering `IAdmisiEventPublisher` to `AdmisiEventPublisher` as Scoped.
  - Activated `.AddMassTransitConfiguration(builder.Configuration)` in `src/bilreg/Bilreg.Api/Program.cs`.
  - Created unit test `MassTransitConfigurationTest` in `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/MassTransitConfigurationTest.cs` verifying DI registration descriptor, configuration options binding, and container resolution of `IAdmisiEventPublisher` to `AdmisiEventPublisher` and `IBus`.
  - Compiled `Bilreg.Api` and verified tests pass (100% green on publisher and DI configuration tests).
- Changed Files:
  - `src/bilreg/Bilreg.Api/appsettings.json`
  - `src/bilreg/Bilreg.Api/appsettings.development.json`
  - `src/bilreg/Bilreg.Api/Configurations/RabbitMqOption.cs`
  - `src/bilreg/Bilreg.Api/Configurations/MassTransitConfiguration.cs`
  - `src/bilreg/Bilreg.Api/Program.cs`
  - `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/MassTransitConfigurationTest.cs`

---

## P3 - Application Integration (Rajal & IGD)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

Fase ini mengintegrasikan pemanggilan penerbitan event notifikasi pada alur pendaftaran Rawat Jalan (Walk-In dan Booking) serta Rawat Darurat (IGD).

### P3-S05

Title: Integrasi Publikasi Event pada Alur Rawat Jalan (Walk-In & Booking)

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mengintegrasikan injeksi `IAdmisiEventPublisher` ke dalam `RegJalanCreateHandler` ([RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs)) dan `RegJalanByBookingHandler` ([RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs)), serta memanggil `await _publisher.PublishRajalCreatedAsync(reg.RegId, cancellationToken)` di luar blok transaksi `TransHelper.NewScope()` setelah `trans.Complete()`.

Depends On: P1-S02  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- Konstruktor `RegJalanCreateHandler` dan `RegJalanByBookingHandler` menerima `IAdmisiEventPublisher`.
- Pemanggilan `PublishRajalCreatedAsync` dieksekusi secara asinkronus tepat setelah `trans.Complete()` dan di luar blok `using (var trans = TransHelper.NewScope())`.
- Dibuat unit test untuk memverifikasi pemanggilan `PublishRajalCreatedAsync` saat pendaftaran walk-in dan booking berhasil.
- Unit test memastikan jika terjadi exception sebelum `trans.Complete()`, `PublishRajalCreatedAsync` tidak pernah dipanggil.
- Pengujian unit use case berjalan hijau (PASS).

Notes:  
Mengacu pada keputusan TD-04 (Strict Post-Commit Trigger). Perhatikan bahwa slice ini hanya memerlukan `IAdmisiEventPublisher` (P1-S02) dan dapat dikerjakan secara paralel dengan fase infrastruktur berkat decoupling Clean Architecture.  
- Implementation Notes:
  - Injected `IAdmisiEventPublisher` into `RegJalanCreateHandler` (`RegJalanWalkInCommand.cs`) and `RegJalanByBookingHandler` (`RegJalanByBookingCmd.cs`).
  - Added strict post-commit publishing calls `await _publisher.PublishRajalCreatedAsync(reg.RegId, cancellationToken);` immediately after `trans.Complete()` and outside the `using (var trans = TransHelper.NewScope())` block for both walk-in and booking handlers (complying with TD-04).
  - Created unit tests `RegJalanCreateHandlerTest` and `RegJalanByBookingHandlerTest` in `Bilreg.Test/AdmisiContext/RegFeature/` verifying:
    1. Successful registration publishes `RegRajalCreatedNotifEvent` via `PublishRajalCreatedAsync` with valid `RegId` and cancellation token.
    2. Exceptions before transaction commit prevent `PublishRajalCreatedAsync` from being invoked.
    3. Errors during persistence/commit prevent `PublishRajalCreatedAsync` from being invoked.
  - All use case unit tests execute and pass (100% GREEN).
- Changed Files:
  - `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs`
  - `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs`
  - `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegJalanCreateHandlerTest.cs`
  - `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegJalanByBookingHandlerTest.cs`

---

### P3-S06

Title: Integrasi Publikasi Event pada Alur Rawat Darurat (IGD)

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mengintegrasikan injeksi `IAdmisiEventPublisher` ke dalam `RegDaruratCreateHandler` ([RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb\b09-bilreg-api\src\bilreg\Bilreg.Application\AdmisiContext\RegFeature\UseCases\RegDaruratCreateCmd.cs)), mengubah implementasi method handler menjadi asynchronous (`async Task<RegDaruratCreateResponse>`), dan memanggil `await _publisher.PublishRajalCreatedAsync(reg.RegId, cancellationToken)` setelah commit transaksi.

Depends On: P1-S02  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- Konstruktor `RegDaruratCreateHandler` menerima `IAdmisiEventPublisher`.
- Method `Handle` diubah menjadi method `async Task<RegDaruratCreateResponse> Handle(...)`.
- `PublishRajalCreatedAsync(reg.RegId, cancellationToken)` dipanggil tepat setelah `trans.Complete()` di luar transaksi database.
- Unit test dibuat untuk memverifikasi `RegDaruratCreateHandler` memicu `PublishRajalCreatedAsync` dengan `RegId` yang valid saat transaksi sukses.
- Pengujian unit use case IGD berstatus PASS.

Notes:  
Mengacu pada keputusan GAP-002 & OQ-001 bahwa IGD memetakan notifikasi ke kontrak `RegRajalCreatedNotifEvent`.  
- Implementation Notes:
  - Injected `IAdmisiEventPublisher` into `RegDaruratCreateHandler` constructor (`RegDaruratCreateCmd.cs`).
  - Converted `Handle` method to asynchronous signature `async Task<RegDaruratCreateResponse> Handle(RegDaruratCreateCmd request, CancellationToken cancellationToken)`.
  - Added strict post-commit trigger `await _publisher.PublishRajalCreatedAsync(reg.RegId, cancellationToken);` immediately after `trans.Complete()` outside the `using (var trans = TransHelper.NewScope())` block, mapping IGD registration to `RegRajalCreatedNotifEvent` per GAP-002 and OQ-001.
  - Created comprehensive unit test suite in `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegDaruratCreateHandlerTest.cs` covering:
    1. Successful IGD registration triggers `PublishRajalCreatedAsync` with valid `RegId` and cancellation token.
    2. Exceptions prior to commit prevent event publication.
    3. Errors during database commit prevent event publication.
  - All unit tests in `RegDaruratCreateHandlerTest` executed and PASSED.
- Changed Files:
  - `src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs`
  - `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegDaruratCreateHandlerTest.cs`

---

## P4 - Ranap Integration & Solution Verification

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

Fase ini mengintegrasikan penerbitan notifikasi registrasi Rawat Inap (Opname dan Reservasi), memperbarui test suite eksisting, dan menjalankan verifikasi solusi menyeluruh termasuk ketahanan terhadap kegagalan broker.

### P4-S07

Title: Integrasi Publikasi Event pada Alur Rawat Inap (Opname & Reservasi)

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mengintegrasikan injeksi `IAdmisiEventPublisher` ke dalam `AdmissionRegistrationOrchestrator` ([AdmissionRegistrationOrchestrator.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs)), memicu `await _publisher.PublishRanapCreatedAsync(admission.RegId, cancellationToken)` setelah `trans.Complete()` pada alur `ProcessOpnameRequest` dan `ProcessReservation`, serta memperbarui test harness pada `AdmissionRegistrationOrchestratorTest.cs`.

Depends On: P1-S02  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- Konstruktor `AdmissionRegistrationOrchestrator` menerima `IAdmisiEventPublisher`.
- `ProcessOpnameRequest` memanggil `await _publisher.PublishRanapCreatedAsync(admission.RegId, cancellationToken)` setelah `trans.Complete()`.
- `ProcessReservation` memanggil `await _publisher.PublishRanapCreatedAsync(admission.RegId, cancellationToken)` setelah `trans.Complete()`.
- Test harness pada `src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmissionRegistrationOrchestratorTest.cs` diperbarui untuk menyertakan mock `IAdmisiEventPublisher`.
- Ditambahkan assertion pengujian bahwa `PublishRanapCreatedAsync` dipanggil dengan `RegId` yang diharapkan pada kedua alur (opname dan reservasi).
- Seluruh test di `AdmissionRegistrationOrchestratorTest` berstatus PASS.

Notes:  
Pastikan skop transaksi database telah selesai/dispose sebelum pemanggilan publish agar koneksi/transaksi SQL Server tidak tertahan oleh panggilan jaringan broker.  
- Implementation Notes:
  - Injected `IAdmisiEventPublisher` into `AdmissionRegistrationOrchestrator` constructor (`AdmissionRegistrationOrchestrator.cs`).
  - Updated both `ProcessOpnameRequest` and `ProcessReservation` methods to asynchronous signatures returning `Task<AdmProcessAdmissionResponse>`.
  - Moved `using (var trans = TransHelper.NewScope()) { ... trans.Complete(); }` to a discrete block disposing the transaction scope prior to invoking `await _publisher.PublishRanapCreatedAsync(admission.RegId, cancellationToken);`, guaranteeing that database connections/transactions are not held open during broker network calls.
  - Updated `OpnameHarness` and `ReservationHarness` in `AdmissionRegistrationOrchestratorTest.cs` to supply a mock `IAdmisiEventPublisher`.
  - Added assertions to existing tests and added dedicated unit tests verifying:
    1. `ProcessOpnameRequest` triggers `PublishRanapCreatedAsync` with expected `RegId`.
    2. `ProcessReservation` triggers `PublishRanapCreatedAsync` with expected `RegId`.
    3. Errors during persistence/rollback prevent event publishing.
  - Executed `dotnet test` on `AdmissionRegistrationOrchestratorTest` with all 13 tests passing (100% PASS).
- Changed Files:
  - `src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs`
  - `src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmissionRegistrationOrchestratorTest.cs`

---

### P4-S08

Title: Verifikasi Solusi End-to-End, Resilience, dan Regresi

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Melakukan verifikasi komprehensif terhadap seluruh solusi `b09-bilreg-api`, memastikan seluruh unit test lulus (PASS), memverifikasi bahwa skenario kegagalan koneksi RabbitMQ tidak menggagalkan pendaftaran pasien di database (resilience check), dan memastikan seluruh kriteria penerimaan (Acceptance Conditions) terpenuhi.

Depends On: P2-S04, P3-S05, P3-S06, P4-S07  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- Eksekusi `dotnet build` pada solusi berhasil dengan 0 error.
- Seluruh unit test di `Bilreg.Test` dieksekusi dengan status hijau (PASS).
- Skenario pengujian resilience memverifikasi bahwa ketika `IBus.Publish` gagal/melempar exception, use case handler tetap mengembalikan response sukses pendaftaran dan transaksi SQL Server tetap ter-commit tanpa HTTP 500 error.
- Seluruh 8 butir kondisi penerimaan pada arsitektur §11 terverifikasi terpenuhi.

Notes:  
Slice ini menandai penyelesaian implementasi seluruh rencana sebelum handoff ke tahap Review dan Testing.  
- Implementation Notes:
  - Executed `dotnet build src/bilreg/b09-bilreg-api.sln` confirming clean build with 0 errors and 0 warnings.
  - Implemented broker failure resilience integration tests across all registration use case handlers (`RegJalanCreateHandlerTest`, `RegJalanByBookingHandlerTest`, `RegDaruratCreateHandlerTest`, `AdmissionRegistrationOrchestratorTest`) verifying that when `IBus.Publish` throws an exception, all registration flows succeed, commit database state, log structured errors via `ILogger<AdmisiEventPublisher>`, and return valid responses with `RegId` without throwing unhandled exceptions or causing HTTP 500 errors.
  - Executed full target test suite covering publisher adapter, DI configuration, registration use cases, and resilience scenarios (`AdmisiEventPublisherTest`, `MassTransitConfigurationTest`, `RegJalanCreateHandlerTest`, `RegJalanByBookingHandlerTest`, `RegDaruratCreateHandlerTest`, `AdmissionRegistrationOrchestratorTest`) with all 32 tests passing (100% PASS, 0 failures).
  - Verified satisfaction of all 8 acceptance conditions in ARCHITECTURE §11:
    1. NuGet references `MassTransit`, `MassTransit.RabbitMQ`, `MyHospital.MsgContract` installed.
    2. `RabbitMqOption` configured in `appsettings.json` and registered on DI container.
    3. `RegJalanWalkInCommand` triggers `RegRajalCreatedNotifEvent` post-commit.
    4. `RegJalanByBookingCmd` triggers `RegRajalCreatedNotifEvent` post-commit.
    5. `RegDaruratCreateCmd` triggers `RegRajalCreatedNotifEvent` post-commit.
    6. `AdmProcessOpnameRequestCmd` and `AdmProcessReservationCmd` trigger `RegRanapCreatedNotifEvent` post-commit.
    7. Failure isolation and resilience verified: broker failure does not fail registration or throw unhandled exceptions.
    8. All unit and resilience test suites passing green.
- Changed Files:
  - `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegJalanCreateHandlerTest.cs`
  - `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegJalanByBookingHandlerTest.cs`
  - `src/bilreg/Bilreg.Test/AdmisiContext/RegFeature/RegDaruratCreateHandlerTest.cs`
  - `src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmissionRegistrationOrchestratorTest.cs`

---

# 6. Change Log

- **2026-10-05 (V1.0)**: Inisialisasi dokumen rencana implementasi (IMPLEMENTATION-PLAN) berdasarkan arsitektur yang disetujui [ADMISI-REG-EVENT-RABBITMQ-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ARCHITECTURE.md). Pembagian menjadi 4 fase dan 8 slice independen dengan penerapan prinsip Dependency-Driven Parallelism dan isolasi Clean Architecture. Status gerbang `Execution Approval: APPROVED` diberikan oleh Architect.
