---
Title: Penjadwalan Pemeriksaan Radiologi (JadwalRadiologi) Implementation Plan
Code: OC-RAD-JADWAL
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-25
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan fitur Penjadwalan Pemeriksaan Radiologi (*Radiology Examination Scheduling*) sebagai fakta bisnis persisten mandiri (`RadJadwalModel`) pada bounded context `RadContext` di backend `b09-bilreg-api` dan modul `Radiology` di frontend `c012_myhospital_web`, merealisasikan target arsitektur teknis [OC-RAD-JADWAL-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-ARCHITECTURE.md) yang diturunkan dari outcome kanonikal [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-RAD-JADWAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEATURE.md)
- ARCHITECTURE: [OC-RAD-JADWAL-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-ARCHITECTURE.md)
- FEASIBILITY-ASSESSMENT: [OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- CANONICAL OUTCOME: [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

1. **Database Schema (`Bilreg.SqlDb.RadContext`)**:
   - DDL skrip migrasi idempoten untuk `BILRG_RadJadwal`, `BILRG_RadEquipment`, dan `BILRG_RadJadwalReschedule`.
   - Composite index untuk optimasi kueri deteksi tumpang-tindih waktu (*collision detection*) dan tampilan kalender.
   - Seed data awal master mesin fisik radiologi (`BILRG_RadEquipment`).
2. **Domain Layer (`Bilreg.Domain.RadContext`)**:
   - Aggregate Root `RadJadwalModel` beserta enum status (`Scheduled`, `Completed`, `Cancelled`) dan kategori pembatalan.
   - Value Objects untuk rentang waktu, panduan persiapan, checklist skrining keselamatan, dan jejak audit.
   - Penambahan metode domain `Unschedule()` pada `RadOrderModel` di `RadOrderFeature`.
3. **Application Layer (`Bilreg.Application.RadContext.RadJadwalFeature`)**:
   - Kontrak port repository & data access (`IRadJadwalRepo`, `IRadJadwalDal`, `IRadEquipmentDal`).
   - Kontrak port integrasi tracker (`IRadTrackerIntegration`).
   - Command use cases: pembuatan jadwal baru, reschedule *in-place*, pembatalan decoupled, dan verifikasi kesiapan/clearance.
   - Query use cases: kueri timeline kalender harian, worklist penjadwalan, detail jadwal, dan lookup master alat.
4. **Infrastructure Layer (`Bilreg.Infrastructure.RadContext.RadJadwalFeature`)**:
   - Implementasi DAL dan Repository menggunakan Dapper / ADO.NET SQL Server dengan kuncian transaksi atomik (`HOLDLOCK, UPDLOCK`).
   - Implementasi adapter integrasi tracker yang tangguh (*resilient/non-blocking try-catch*).
   - Pendaftaran dependensi DI pada service collection.
5. **API Layer (`Bilreg.Api.Controllers.RadContext`)**:
   - Controller REST API `RadJadwalController` dan `RadEquipmentController`.
6. **Backend Verification (`Bilreg.Test.RadContext.RadJadwalFeature`)**:
   - Unit test domain model lifecycle, validation invariants, command/query handlers, dan DAL.
7. **Frontend Core (`c012_myhospital_web/src/modules/Radiology`)**:
   - Definisi tipe data TypeScript dan skema Zod kontrak API.
   - Service API klien dan TanStack Query composables (`useRadJadwal`, `useRadEquipment`).
8. **Frontend UI & Views (`c012_myhospital_web/src/modules/Radiology`)**:
   - Komponen dialog form reservasi jadwal baru, pemindahan jadwal (reschedule), pembatalan, dan verifikasi clearance.
   - Komponen cetak Lembar Konfirmasi Jadwal dan Panduan Persiapan Pasien via browser print layout.
   - Refaktorisasi visual timeline kalender scheduler pada [RadSchedulingPanel.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/components/RadSchedulingPanel.vue) dan integrasi antarmuka utama pada [TindakanRadiologi.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/views/TindakanRadiologi.vue).
   - Verifikasi type-check (`pnpm tc:app`) dan linter.

---

# 3. Dependencies

- Solution Backend: `b09-bilreg-api` (C# .NET 8 / 9, Dapper, SQL Server).
- Solution Frontend: `c012_myhospital_web` (Vue 3, TypeScript, Vite, Tailwind CSS v4, TanStack Query v5, Zod, Luxon).
- Context Dependencies: `RadOrderFeature` (existing), `PatientSnapshotType`, `AuditTrailType`, `NunaId`.
- External Integration: `IPasienTrackerService` di backend.

---

# 4. Progress Summary

| Phase | Title | Implementation Status | Review Status | Progress |
|---|---|---|---|---|
| P1 | Database Schema & Domain Modeling (BE) | NOT-STARTED | NOT-REVIEWED | 0/3 |
| P2 | Application Use Cases & Contracts (BE) | NOT-STARTED | NOT-REVIEWED | 0/3 |
| P3 | Infrastructure Persistence & Adapters (BE) | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P4 | API Controllers & Backend Tests (BE) | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P5 | Frontend Contracts & Composables (FE) | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P6 | Frontend UI, Dialogs, Print & Integration (FE) | NOT-STARTED | NOT-REVIEWED | 0/3 |

---

# 5. Phases

## P1 - Database Schema & Domain Modeling (BE)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P1-S01: Database Migrations for RadJadwal, RadEquipment & Reschedule

Title: Migrasi Tabel SQL Basis Data untuk RadJadwal, RadEquipment, dan Reschedule  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Menyediakan skrip DDL SQL Server idempoten untuk tabel `BILRG_RadJadwal`, `BILRG_RadEquipment`, dan `BILRG_RadJadwalReschedule` beserta composite index untuk performa kueri deteksi tumpang-tindih waktu dan seed data awal master alat.  
Depends On: None  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.SqlDb/RadContext/RadJadwalFeature/BILRG_RadJadwal.sql`
- Created: `src/bilreg/Bilreg.SqlDb/RadContext/RadJadwalFeature/BILRG_RadEquipment.sql`
- Created: `src/bilreg/Bilreg.SqlDb/RadContext/RadJadwalFeature/BILRG_RadJadwalReschedule.sql`
- Created: `src/bilreg/Bilreg.SqlDb/RadContext/RadJadwalFeature/Seed_BILRG_RadEquipment.sql`  
Completion Criteria:
- File DDL SQL dibuat dengan pengecekan idempoten `IF NOT EXISTS (SELECT * FROM sys.tables ...)`.
- Tabel `BILRG_RadJadwal` memiliki primary key `JadwalRadId`, unique index pada `ScheduleNo`, index pada `OrderId`, serta composite index pada `(EquipmentId, ScheduleDate, Status, StartTime, EndTime)`.
- Tabel `BILRG_RadEquipment` memiliki primary key `EquipmentId` dan data seed awal untuk modalitas CT, MRI, USG, dan Fluoroskopi.
- Tabel `BILRG_RadJadwalReschedule` memiliki foreign key logis ke `JadwalRadId`.

### P1-S02: Domain Models, Enums & Value Objects for RadJadwal

Title: Pembentukan Aggregate Root RadJadwalModel dan Value Objects Pendukung  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mengimplementasikan aggregate root `RadJadwalModel`, enum status dan kategori pembatalan, value object checklist keselamatan klinis, serta aturan invarian domain (validasi rentang waktu dan aturan clearance gate).  
Depends On: None  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/IRadJadwalKey.cs`
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/RadJadwalStatusEnum.cs`
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/RadCancellationCategoryEnum.cs`
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/RadSafetyChecklistType.cs`
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/RadJadwalModel.cs`  
Completion Criteria:
- `RadJadwalModel` memiliki factory method `Create(...)`, metode `Reschedule(...)`, `Cancel(...)`, `RecordArrival(...)`, dan `Complete(...)`.
- Metode `Complete(...)` menegakkan invariant: melempar exception jika `IsArrived == false` atau `IsCleared == false`.
- Status enum mendukung `Scheduled = 1`, `Completed = 2`, `Cancelled = 3`.

### P1-S03: Domain Lifecycle Extension on RadOrderModel (Unschedule)

Title: Penambahan Metode Unschedule pada RadOrderModel untuk Decoupled Cancellation  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Menambahkan metode `Unschedule(string userId, DateTime unscheduledAt)` pada `RadOrderModel` agar order dapat dikembalikan dari status `Scheduled` ke `Requested` saat jadwal dibatalkan, serta menulis unit test siklus hidupnya.  
Depends On: None  
Repository: `b09-bilreg-api`  
Changed Files:
- Modified: `src/bilreg/Bilreg.Domain/RadContext/RadOrderFeature/RadOrderModel.cs`
- Modified: `src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderModelTest.cs`  
Completion Criteria:
- `RadOrderModel` memiliki metode `public void Unschedule(string userId, DateTime unscheduledAt = default)`.
- Validasi guard: melempar exception jika order void atau jika status bukan `Scheduled`.
- Memperbarui `RadOrderStatus` menjadi `RadOrderStatusEnum.Requested` dan mencatat audit modifikasi.
- Unit test `RadOrderModelTest` mencakup skenario transisi `Schedule()` lalu `Unschedule()`.

---

## P2 - Application Use Cases & Contracts (BE)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P2-S04: Port Interfaces & DTO Contracts for RadJadwal

Title: Definisi Kontrak Port Repository, DAL, dan DTO Jadwal Radiologi  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mendefinisikan interface repository `IRadJadwalRepo`, data access `IRadJadwalDal`, master alat `IRadEquipmentDal`, integrasi tracker `IRadTrackerIntegration`, serta struktur DTO input/output.  
Depends On: P1-S02  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/IRadJadwalDal.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/IRadJadwalRepo.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/IRadEquipmentDal.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/IRadTrackerIntegration.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalDto.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadEquipmentDto.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCalendarDto.cs`  
Completion Criteria:
- `IRadJadwalDal` memiliki method `IsSlotOverlapping(equipmentId, scheduleDate, startTime, endTime, excludeJadwalId)`.
- `IRadJadwalRepo` memiliki method `LoadActiveByOrderId(string orderId)` untuk mendukung pengecekan jadwal aktif dan cascade cancel dari order.
- `IRadTrackerIntegration` memiliki method `PublishMilestoneAsync(regId, jadwalRadId, milestone, note, ct)`.
- DTO mencakup data jadwal lengkap, snapshot pasien, rincian waktu, checklist keselamatan, dan jejak reschedule.

### P2-S05: Application Commands (Create, Reschedule, Cancel, Clearance) & Handlers

Title: Implementasi Use Case Application Commands dan Orkestrasi Transaksi  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mengimplementasikan handler use cases: pembuatan jadwal baru dengan pengecekan collision, reschedule in-place, pembatalan decoupled dengan pemanggilan `RadOrder.Unschedule()`, pembaruan `RadOrderCancelHandler` untuk cascade cancel jadwal aktif, verifikasi kesiapan/clearance, serta publikasi milestone ke tracker.  
Depends On: P1-S03, P2-S04  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCreateCmd.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalRescheduleCmd.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCancelCmd.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalClearanceCmd.cs`
- Modified: `src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/UseCases/RadOrderCancelCmd.cs`  
Completion Criteria:
- `RadJadwalCreateHandler` memvalidasi kelayakan order, kueri collision detection, menyimpan jadwal, memanggil `order.Schedule()`, dan mempublikasikan milestone `Scheduled`.
- `RadJadwalRescheduleHandler` memvalidasi slot baru bebas benturan, memperbarui jadwal, mencatat riwayat reschedule, dan mempublikasikan milestone `Rescheduled`.
- `RadJadwalCancelHandler` menandai jadwal `Cancelled`, memanggil `order.Unschedule()`, dan mempublikasikan milestone `Cancelled`.
- `RadOrderCancelHandler` memeriksa jadwal aktif via `IRadJadwalRepo.LoadActiveByOrderId` dan otomatis membatalkan slot jadwal terkait saat order dibatalkan.
- `RadJadwalClearanceHandler` memperbarui `IsArrived` dan `IsCleared`, mentransisikan status ke `Completed`, dan mempublikasikan milestone `Completed`.

### P2-S06: Application Queries (Get, Worklist, CalendarTimeline, EquipmentLookup) & Handlers

Title: Implementasi Use Case Application Queries untuk Kalender dan Worklist  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mengimplementasikan handler query untuk pengambilan data detail jadwal, daftar kerja penjadwalan (*worklist*), data lini masa kalender harian per mesin, dan lookup master mesin fisik aktif.  
Depends On: P2-S04  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalGetQuery.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalWorklistQuery.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCalendarTimelineQuery.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadEquipmentLookupQuery.cs`  
Completion Criteria:
- `RadJadwalCalendarTimelineHandler` mengembalikan daftar slot terjadwal dikelompokkan per mesin/modalitas untuk tanggal tertentu.
- `RadJadwalWorklistHandler` mengembalikan order yang memerlukan jadwal dan jadwal aktif dengan filter status dan rentang tanggal.
- `RadEquipmentLookupHandler` mengembalikan daftar mesin aktif berdasarkan filter modalitas.

---

## P3 - Infrastructure Persistence & Adapters (BE)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P3-S07: DAL & Repository Implementation for RadJadwal and RadEquipment

Title: Implementasi Akses Data SQL Server Menggunakan Dapper dan Kuncian Konkurensi  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mengimplementasikan `RadJadwalDal`, `RadJadwalRepo`, dan `RadEquipmentDal` menggunakan pure Dapper / ADO.NET SQL Server dengan transaksi berpelindung kuncian konkurensi (`HOLDLOCK, UPDLOCK`) saat pengecekan benturan waktu.  
Depends On: P1-S01, P2-S04  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadJadwalDal.cs`
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadJadwalRepo.cs`
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadEquipmentDal.cs`
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadJadwalRescheduleDal.cs`  
Completion Criteria:
- `RadJadwalDal.IsSlotOverlapping` mengeksekusi SQL kueri overlap dengan parameter `EquipmentId`, `ScheduleDate` (`DateTime`), `StartTime` (`TimeSpan`), `EndTime` (`TimeSpan`), dan status `Scheduled`.
- Penyimpanan jadwal dan riwayat reschedule dieksekusi secara atomik dalam `TransactionScope` dengan `HOLDLOCK, UPDLOCK`.
- Pemetaan data dari tabel `BILRG_RadJadwal` ke `RadJadwalModel` akurat dan konsisten (termasuk konversi `DateTime`/`TimeSpan` ke `DateOnly`/`TimeOnly`).

### P3-S08: Tracker Integration Adapter & Dependency Injection Registration

Title: Adapter Integrasi Tracker Resilient dan Pendaftaran Dependensi Service  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mengimplementasikan adapter `RadTrackerIntegration` yang memanggil `IPasienTrackerRepo` / service tracker secara resilient (non-blocking) dan mendaftarkan seluruh service, repository, dan DAL pada container DI di `Bilreg.Api/Configurations/InfrastructureService.cs`.  
Depends On: P2-S04  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadTrackerIntegration.cs`
- Modified: `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs`  
Completion Criteria:
- `RadTrackerIntegration` menangkap exception tracker dan mencatatnya ke logger tanpa melempar kegagalan ke alur utama transaksi.
- Seluruh interface `IRadJadwalRepo`, `IRadJadwalDal`, `IRadEquipmentDal`, `IRadTrackerIntegration` terdaftar pada `InfrastructureService.cs`.

---

## P4 - API Controllers & Backend Tests (BE)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P4-S09: REST API Controllers (RadJadwalController & RadEquipmentController)

Title: Endpoint Controller REST API untuk Penjadwalan dan Master Mesin Radiologi  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Menyediakan endpoint REST API untuk CRUD jadwal, reschedule, pembatalan, clearance check, kueri timeline kalender, dan lookup mesin alat.  
Depends On: P2-S05, P2-S06, P3-S07, P3-S08  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Api/Controllers/RadContext/RadJadwalController.cs`
- Created: `src/bilreg/Bilreg.Api/Controllers/RadContext/RadEquipmentController.cs`  
Completion Criteria:
- Endpoint terbit:
  - `POST api/rad/jadwal` (Create)
  - `POST api/rad/jadwal/{id}/reschedule` (Reschedule)
  - `POST api/rad/jadwal/{id}/cancel` (Cancel)
  - `POST api/rad/jadwal/{id}/clearance` (Clearance / Complete)
  - `GET api/rad/jadwal/{id}` (Get Detail)
  - `GET api/rad/jadwal/calendar` (Calendar Timeline)
  - `GET api/rad/jadwal/worklist` (Worklist)
  - `GET api/rad/equipment` (Equipment Lookup)
- Controller menggunakan `[Authorize]` dan membungkus respon dengan `JSendOk` / `JSendFail` (`Nuna.Lib.ActionResultHelper`) selaras dengan `RadOrderController`.

### P4-S10: Backend Unit & Integration Tests

Title: Pengujian Unit Domain, Command/Query Handlers, dan DAL di Bilreg.Test  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Menulis dan mengeksekusi test suite komprehensif di `Bilreg.Test` untuk memverifikasi model domain `RadJadwalModel`, logika deteksi tabrakan waktu, use case command handlers, dan pemetaan data.  
Depends On: P4-S09  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalModelTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalCreateHandlerTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalRescheduleHandlerTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalCancelHandlerTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalClearanceHandlerTest.cs`  
Completion Criteria:
- Seluruh unit test baru lulus:
  `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RadJadwal"`
- Verifikasi invariant clearance gate, collision detection, dan transisi `Unschedule()`.

---

## P5 - Frontend Contracts & Composables (FE)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P5-S11: TypeScript Types, Zod Schemas & API Client Service

Title: Pembuatan Kontrak Tipe TypeScript, Skema Zod, dan Service Klien API  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mendefinisikan tipe data TypeScript dan skema Zod yang selaras dengan DTO backend untuk jadwal radiologi, master alat, checklist keselamatan, dan fungsi pemanggil API via `ApiService`.  
Depends On: P4-S09  
Repository: `c012_myhospital_web`  
Changed Files:
- Created: `src/modules/Radiology/types/jadwal.ts`
- Created: `src/modules/Radiology/services/radJadwalService.ts`
- Modified: `src/modules/Radiology/types/index.ts`  
Completion Criteria:
- Definisi tipe `RadJadwal`, `RadEquipment`, `SafetyChecklist`, `CreateJadwalPayload`, `ReschedulePayload`, `CancelPayload`, `ClearancePayload`.
- Service `radJadwalService` mengimplementasikan fungsi HTTP request menggunakan `ApiService`.

### P5-S12: TanStack Query Composables for RadJadwal

Title: Implementasi Composable Reaktif TanStack Query (useRadJadwal & useRadEquipment)  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mengimplementasikan composable hooks reaktif untuk kueri timeline kalender harian, lookup mesin, dan mutasi aksi (Create, Reschedule, Cancel, Clearance) dengan auto cache invalidation.  
Depends On: P5-S11  
Repository: `c012_myhospital_web`  
Changed Files:
- Created: `src/modules/Radiology/composables/useRadJadwal.ts`
- Created: `src/modules/Radiology/composables/useRadEquipment.ts`  
Completion Criteria:
- Composable mengekspos: `useRadJadwalCalendarQuery`, `useRadJadwalDetailQuery`, `useCreateRadJadwalMutation`, `useRescheduleRadJadwalMutation`, `useCancelRadJadwalMutation`, `useClearanceRadJadwalMutation`.
- Setiap mutasi memicu invalidasi query key `['rad-jadwal']` dan toast notifikasi sukses/gagal.

---

## P6 - Frontend UI, Dialogs, Print & Integration (FE)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P6-S13: Scheduling, Reschedule & Cancellation Dialog Components

Title: Pembuatan Komponen Modal Dialog Reservasi, Reschedule, dan Pembatalan Jadwal  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Membuat komponen antarmuka dialog form pemesanan jadwal baru (pemilihan mesin, kalkulasi durasi otomatis + buffer, catatan persiapan), dialog pemindahan jadwal (reschedule), dan dialog pembatalan bersebab.  
Depends On: P5-S12  
Repository: `c012_myhospital_web`  
Changed Files:
- Created: `src/modules/Radiology/components/RadScheduleDialog.vue`
- Created: `src/modules/Radiology/components/RadRescheduleDialog.vue`
- Created: `src/modules/Radiology/components/RadCancelDialog.vue`  
Completion Criteria:
- `RadScheduleDialog.vue` menampilkan dropdown mesin berbasis modalitas, opsi tanggal dan waktu, estimasi total durasi dengan buffer (10/15 menit), serta form catatan persiapan pasien.
- `RadRescheduleDialog.vue` memfasilitasi pemilihan slot/mesin baru dengan input wajib alasan pemindahan.
- `RadCancelDialog.vue` menyediakan pilihan kategori alasan pembatalan resmi.

### P6-S14: Clearance Verification Dialog & Print Confirmation Slip

Title: Komponen Dialog Verifikasi Kesiapan Klinis dan Cetak Lembar Konfirmasi Jadwal  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Membuat komponen dialog verifikasi kedatangan pasien dan checklist skrining keselamatan pra-tindakan (`IsArrived`, `IsCleared`), serta komponen cetak Lembar Konfirmasi Jadwal dan Panduan Persiapan Pasien.  
Depends On: P5-S12  
Repository: `c012_myhospital_web`  
Changed Files:
- Created: `src/modules/Radiology/components/RadClearanceDialog.vue`
- Created: `src/modules/Radiology/components/RadScheduleConfirmationPrint.vue`  
Completion Criteria:
- `RadClearanceDialog.vue` memuat checklist keselamatan (implan feromagnetik, alergi kontras, kehamilan, eGFR) dan tombol konfirmasi kesiapan klinis sebelum dialihkan ke antrean pemindaian fisik.
- `RadScheduleConfirmationPrint.vue` menyediakan layout cetak dokumen standar rumah sakit (identitas, nomor transaksi, jam tiba, instruksi puasa/minum).

### P6-S15: Timeline Scheduler Refactoring & End-to-End View Integration

Title: Refaktorisasi RadSchedulingPanel dan Integrasi Penuh TindakanRadiologi  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Merefaktor `RadSchedulingPanel.vue` dan `TindakanRadiologi.vue` agar menggantikan mock data dengan live TanStack Query hooks, menghubungkan seluruh modal dialog aksi, serta memvalidasi type check (`pnpm tc:app`) dan linter.  
Depends On: P6-S13, P6-S14  
Repository: `c012_myhospital_web`  
Changed Files:
- Modified: `src/modules/Radiology/components/RadSchedulingPanel.vue`
- Modified: `src/modules/Radiology/views/TindakanRadiologi.vue`  
Completion Criteria:
- `RadSchedulingPanel.vue` menampilkan slot jadwal nyata dari server dengan indikator visual status (`Scheduled`, `Completed`, `Cancelled`).
- Tombol aksi di `TindakanRadiologi.vue` membuka modal yang sesuai dan memperbarui status tabel secara reaktif.
- Verifikasi berhasil: `cd c012_myhospital_web && pnpm tc:app && pnpm lint`.

---

# 6. Change Log

- 2026-11-25: Inisiasi awal dokumen IMPLEMENTATION PLAN untuk implementasi Outcome OC-RAD-JADWAL (Penjadwalan Pemeriksaan Radiologi) mencakup 6 fase dan 15 irisan (*slices*) lintas repositori backend (`b09-bilreg-api`) dan frontend (`c012_myhospital_web`). Status rencana: `NOT-STARTED`, persetujuan eksekusi: `PENDING`.
- 2026-11-26: Reviu arsitektur (`ica-architect`) terhadap codebase eksisting: menyelaraskan lokasi DI (`InfrastructureService.cs`), menambahkan update `RadOrderCancelCmd.cs` pada `P2-S05` untuk cascade cancel, menyelaraskan konvensi `System.Data.SqlClient` + Dapper (`DateTime`/`TimeSpan`) dan `JSendOk`/`[Authorize]`, serta mengesahkan `Execution Approval: APPROVED`.
