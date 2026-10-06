---
Title: Architecture - Publikasi Event Registrasi Pasien ke RabbitMQ
Code: ADMISI-REG-EVENT-RABBITMQ
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-10-05
---

# 1. Overview

Arsitektur ini mendefinisikan realisasi teknis untuk memindahkan tanggung jawab publikasi event notifikasi registrasi pasien (Rawat Jalan, Rawat Inap, Rawat Darurat) dari komponen deprecated `BipubApi` langsung ke dalam sistem Bilreg (`b09-bilreg-api`) dengan perantara message broker RabbitMQ.

Referenced Issue:
- [ADMISI-REG-EVENT-RABBITMQ-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-ISSUE.md) (`ISSUE-BILREG-ADMISI-EVENT-001`)

---

# 2. Architectural Basis

## Business Context

- DOMAIN Rajal: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)
- DOMAIN Ranap: [admisi-ranap-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)

Dalam konteks bisnis admisi rumah sakit, keberhasilan registrasi pasien memicu pemberitahuan asinkronus ke subsistem downstream (misal: antrian poliklinik/farmasi, kasir, dan penunjang medis). Decommissioning `BipubApi` mengharuskan `b09-bilreg-api` sebagai bounded context pemilik otoritatif registrasi bertindak langsung sebagai penerbit (*event publisher*) ke message broker.

## Analysis Input

- FEASIBILITY-ASSESSMENT: [ADMISI-REG-EVENT-RABBITMQ-FEASIBILITY-ASSESSMENT.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-REG-EVENT-RABBITMQ-FEASIBILITY-ASSESSMENT.md)

Keputusan analisis kelayakan yang direalisasikan dalam arsitektur ini:
- **GAP-001 & OQ-004:** Adopsi `MassTransit` dengan transport RabbitMQ pada `b09-bilreg-api` dan dependensi `MyHospital.MsgContract`.
- **GAP-002 & OQ-001:** Registrasi Rawat Darurat (IGD) memetakan notifikasi ke kontrak `RegRajalCreatedNotifEvent`.
- **GAP-003:** Integrasi pemanggilan publish pada 4 titik flow registrasi: Rajal Walk-In, Rajal Booking, Rawat Darurat, dan Rawat Inap (Opname & Reservasi).
- **GAP-004 & OQ-002:** Mekanisme Direct Publish post-commit (`trans.Complete()`) tanpa tabel outbox untuk fase saat ini.
- **GAP-005:** Isolasi kegagalan koneksi RabbitMQ (*resilience*) dengan `try-catch` dan error logging agar registrasi pasien di database tidak pernah dibatalkan karena kegagalan broker.
- **OQ-003:** Ruang lingkup terbatas pada registrasi baru (create), pembatalan registrasi dikecualikan.

```text
DOMAIN + FEATURE (Admisi Rajal & Ranap)
        +
FEASIBILITY-ASSESSMENT (ADMISI-REG-EVENT-RABBITMQ-FEASIBILITY-ASSESSMENT)
        ↓
    ARCHITECTURE (ADMISI-REG-EVENT-RABBITMQ-ARCHITECTURE)
```

---

# 3. Scope

## Included

1. Penambahan dependensi paket `MassTransit` (RabbitMQ transport) dan package contract `MyHospital.MsgContract` ke solusi Bilreg.
2. Konfigurasi `RabbitMqOption` di `appsettings.json` dan registrasi bus MassTransit pada DI container `Bilreg.Api`.
3. Penyediaan interface abstraksi `IAdmisiEventPublisher` di layer `Bilreg.Application` untuk menjaga prinsip Clean Architecture dan kemudahan pengujian unit.
4. Implementasi `AdmisiEventPublisher` di layer `Bilreg.Infrastructure` yang membungkus MassTransit `IBus.Publish` dengan proteksi error handling dan logging.
5. Pemicuan event notifikasi setelah commit transaksi (`trans.Complete()`) pada:
   - Rawat Jalan Walk-In (`RegJalanCreateHandler`) -> `RegRajalCreatedNotifEvent`
   - Rawat Jalan Booking (`RegJalanByBookingHandler`) -> `RegRajalCreatedNotifEvent`
   - Rawat Darurat / IGD (`RegDaruratCreateHandler`) -> `RegRajalCreatedNotifEvent`
   - Rawat Inap Opname & Reservasi (`AdmissionRegistrationOrchestrator`) -> `RegRanapCreatedNotifEvent`
6. Pengujian unit (unit tests) untuk use case handlers dan event publisher.

## Excluded

1. Publikasi event pembatalan registrasi / pembatalan admisi (dikecualikan sesuai keputusan OQ-003).
2. Pembuatan tabel database outbox baru dan background worker outbox dispatcher (dikecualikan sesuai keputusan OQ-002).
3. Modifikasi atau rilis NuGet package baru untuk event khusus IGD (menggunakan kontrak yang ada sesuai keputusan OQ-001).
4. Pembuatan consumer baru di downstream.

---

# 4. Technical Decisions

### TD-01: Framework & Messaging Library
Menggunakan **`MassTransit`** (versi 8.x yang kompatibel dengan .NET 8.0) dengan transport RabbitMQ, konsisten dengan stack pesan yang sebelumnya digunakan di `BipubApi`.

### TD-02: Message Contract Mapping
Menggunakan tipe kontrak yang sudah terdefinisi di package **`MyHospital.MsgContract.Billing.AdmisiEvents`**:
- Registrasi Rawat Jalan -> `RegRajalCreatedNotifEvent(string RegId)`
- Registrasi Rawat Darurat -> `RegRajalCreatedNotifEvent(string RegId)` (selaras dengan keputusan OQ-001)
- Registrasi Rawat Inap -> `RegRanapCreatedNotifEvent(string RegId)`

### TD-03: Publisher Abstraction (Clean Architecture & Testability)
Layer `Bilreg.Application` tidak boleh bergantung langsung pada pustaka `MassTransit`. Sebagai gantinya, didefinisikan antarmuka abstraksi:
```csharp
namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IAdmisiEventPublisher
{
    Task PublishRajalCreatedAsync(string regId, CancellationToken cancellationToken = default);
    Task PublishRanapCreatedAsync(string regId, CancellationToken cancellationToken = default);
}
```
Use case handler hanya bergantung pada `IAdmisiEventPublisher`, sehingga handler dapat diuji secara independen tanpa memerlukan mock `IBus` MassTransit yang kompleks.

### TD-04: Execution Timing (Post-Commit Direct Publish)
Penerbitan event ke broker **wajib dilakukan setelah** skop transaksi database SQL Server berhasil di-commit (`trans.Complete()`), di luar blok `using (var trans = TransHelper.NewScope())`.
Hal ini mencegah pengiriman event palsu (ghost event) apabila terjadi kegagalan komit data registrasi di SQL Server.

### TD-05: Resilience & Error Isolation
Implementasi `AdmisiEventPublisher` di `Bilreg.Infrastructure` wajib membungkus pemanggilan `_bus.Publish` dalam blok `try-catch`:
- Jika terjadi exception (misal: RabbitMQ broker offline, koneksi terputus, atau network timeout), publisher mencatat error log terstruktur melalui `ILogger<AdmisiEventPublisher>`.
- Publisher **tidak melempar exception (swallow & log)** ke pemanggil, sehingga response registrasi pasien tetap berhasil dikembalikan ke klien loket/kiosk rumah sakit.

---

# 5. Component Responsibilities

| Component | Responsibility |
|---|---|
| `Bilreg.Application.AdmisiContext.RegFeature.IAdmisiEventPublisher` | Kontrak abstraksi publishing event registrasi (Rajal & Ranap) di layer Application. |
| `Bilreg.Infrastructure.AdmisiContext.RegFeature.AdmisiEventPublisher` | Implementasi publisher yang memetakan model ke `RegRajalCreatedNotifEvent` / `RegRanapCreatedNotifEvent`, mengeksekusi `IBus.Publish`, dan mengisolasi kegagalan broker dengan logging. |
| `Bilreg.Api.Configurations.MassTransitConfiguration` (atau ekstensi DI terkait) | Konfigurasi pembacaan section `RabbitMqOption` dari `appsettings.json`, inisialisasi MassTransit RabbitMQ host, dan registrasi DI service. |
| `RegJalanCreateHandler` | Menjalankan persistensi registrasi rajal walk-in dan memanggil `_publisher.PublishRajalCreatedAsync` setelah `trans.Complete()`. |
| `RegJalanByBookingHandler` | Menjalankan persistensi registrasi rajal dari booking dan memanggil `_publisher.PublishRajalCreatedAsync` setelah `trans.Complete()`. |
| `RegDaruratCreateHandler` | Menjalankan persistensi registrasi IGD dan memanggil `_publisher.PublishRajalCreatedAsync` setelah `trans.Complete()`. |
| `AdmissionRegistrationOrchestrator` | Menjalankan persistensi registrasi rawat inap (opname/reservasi) dan memanggil `_publisher.PublishRanapCreatedAsync` setelah `trans.Complete()`. |

---

# 6. Integration Design

| Source | Target | Purpose |
|---|---|---|
| `RegJalanCreateHandler` | `IAdmisiEventPublisher` | Memicu publikasi notifikasi registrasi rajal walk-in setelah commit DB. |
| `RegJalanByBookingHandler` | `IAdmisiEventPublisher` | Memicu publikasi notifikasi registrasi rajal booking setelah commit DB. |
| `RegDaruratCreateHandler` | `IAdmisiEventPublisher` | Memicu publikasi notifikasi registrasi rawat darurat (IGD) setelah commit DB. |
| `AdmissionRegistrationOrchestrator` | `IAdmisiEventPublisher` | Memicu publikasi notifikasi registrasi rawat inap setelah commit DB. |
| `AdmisiEventPublisher` | MassTransit `IBus` | Mengirimkan pesan `RegRajalCreatedNotifEvent` / `RegRanapCreatedNotifEvent` ke RabbitMQ exchange. |
| MassTransit `IBus` | RabbitMQ Server | Transport AMQP pengiriman pesan event ke exchange downstream. |

---

# 7. Data Ownership

| Data | Owner |
|---|---|
| Registrasi Rawat Jalan & Darurat (`RegModel`, `RegAktifModel`) | `Bilreg.Domain.AdmisiContext.RegFeature` |
| Registrasi Rawat Inap (`AdmissionModel`, `RegInapModel`) | `Bilreg.Domain.AdmisiRanapContext.AdmissionFeature` |
| Event Payload Data (`string RegId`) | Berasal dari `RegModel.RegId` atau `AdmissionModel.RegId`, diterbitkan sebagai event notifikasi eksternal. |
| Konfigurasi Broker (`RabbitMqOption`) | `Bilreg.Api` configuration (`appsettings.json`) |

---

# 8. Database Design

## New Tables
Tidak ada. (Sesuai keputusan Direct Publish post-commit tanpa tabel outbox).

## Modified Tables
Tidak ada.

## Relationships
Tidak ada perubahan relasi skema database.

## Migration Considerations
Tidak ada migrasi database atau schema modification yang diperlukan.

---

# 9. Cross-Cutting Concerns

### 1. Resilience & Failure Isolation
Jika koneksi RabbitMQ terputus, pesan gagal dipublikasikan namun proses penyimpanan registrasi pasien di SQL Server tetap utuh. Exception ditangkap di layer infrastructure, dan peringatan dicatat pada log Seq/Serilog dengan detail `RegId` dan jenis event.

### 2. Asynchronous Execution
Semua operasi publishing bersifat non-blocking (`async/await`) dan meneruskan `CancellationToken` dari MediatR request untuk efisiensi thread pool ASP.NET Core.

### 3. Observability & Logging
Setiap publikasi yang berhasil dapat dicatat pada level Debug/Information, sedangkan kegagalan publikasi dicatat pada level Warning/Error dengan payload kontekstual:
`"Gagal mempublikasikan event registrasi {EventType} untuk RegId: {RegId}. Error: {ErrorMessage}"`.

### 4. Configuration Security
Kredensial koneksi RabbitMQ (`Server`, `UserName`, `Password`) dibaca dari konfigurasi standar `appsettings.json` dengan opsi override melalui environment variable atau machine-specific settings.

---

# 10. Implementation Constraints

1. **Target Framework**: Seluruh pustaka baru dan paket NuGet yang ditambahkan ke `b09-bilreg-api` harus kompatibel dengan .NET 8.0 (`net8.0`).
2. **Clean Architecture Boundary**: Layer `Bilreg.Application` tidak boleh mereferensikan namespace `MassTransit` secara langsung; hanya menggunakan `IAdmisiEventPublisher`.
3. **Strict Post-Commit Trigger**: Pemanggilan `_publisher.Publish*` dilarang ditempatkan di dalam blok transaksi `using (var trans = TransHelper.NewScope())` sebelum `trans.Complete()`.
4. **Resilience Non-Breaking**: Kegagalan `IAdmisiEventPublisher` tidak boleh melempar unhandled exception yang membatalkan response sukses dari MediatR request handler.
5. **Existing Tests**: Test suite eksisting pada use cases registrasi harus tetap lulus dengan menyediakan mock `IAdmisiEventPublisher` pada setup pengujian.

---

# 11. Acceptance Conditions

1. Paket `MassTransit` (RabbitMQ transport) dan `MyHospital.MsgContract` terpasang dan terkonfigurasi dengan benar di `b09-bilreg-api`.
2. Konfigurasi `RabbitMqOption` tersedia di `appsettings.json` dan terdaftar pada Dependency Injection container saat aplikasi startup.
3. Use case `RegJalanWalkInCommand` mempublikasikan `RegRajalCreatedNotifEvent` berisi `RegId` yang valid tepat setelah commit database.
4. Use case `RegJalanByBookingCmd` mempublikasikan `RegRajalCreatedNotifEvent` berisi `RegId` yang valid tepat setelah commit database.
5. Use case `RegDaruratCreateCmd` mempublikasikan `RegRajalCreatedNotifEvent` berisi `RegId` yang valid tepat setelah commit database.
6. Use case `AdmProcessOpnameRequestCmd` dan `AdmProcessReservationCmd` mempublikasikan `RegRanapCreatedNotifEvent` berisi `RegId` yang valid tepat setelah commit database.
7. Simulasi kegagalan broker RabbitMQ (misal broker offline atau exception) tidak menyebabkan request registrasi mengembalikan HTTP 500 atau membatalkan data di SQL Server.
8. Seluruh unit test untuk `RegJalanCreateHandler`, `RegJalanByBookingHandler`, `RegDaruratCreateHandler`, `AdmissionRegistrationOrchestrator`, dan `AdmisiEventPublisher` berstatus hijau (PASS).
