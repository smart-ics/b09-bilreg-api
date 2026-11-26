---
Title: Penjadwalan Pemeriksaan Radiologi (JadwalRadiologi) Backend Implementation Plan
Code: OC-RAD-JADWAL-BE
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-26
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan fitur Penjadwalan Pemeriksaan Radiologi (*Radiology Examination Scheduling*) pada sisi **Backend Core (`b09-bilreg-api`)** sebagai fakta bisnis persisten mandiri (`RadJadwalModel`) pada bounded context `RadContext`, merealisasikan target arsitektur teknis [OC-RAD-JADWAL-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-ARCHITECTURE.md) yang diturunkan dari outcome kanonikal [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md).

**Replacement Plan Notice**:
Dokumen ini merupakan *replacement implementation plan* yang menggantikan [OC-RAD-JADWAL-IMPLEMENTATION-PLAN.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-IMPLEMENTATION-PLAN.md) (yang disimpan sebagai catatan historis) untuk memfokuskan eksekusi saat ini secara eksklusif pada **Backend (`b09-bilreg-api`)**. Irisan Frontend (`c012_myhospital_web`) ditangguhkan ke siklus perencanaan terpisah setelah perancangan UI/UX dan analisis BFF selesai dilakukan.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-RAD-JADWAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEATURE.md)
- ARCHITECTURE: [OC-RAD-JADWAL-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-ARCHITECTURE.md)
- FEASIBILITY-ASSESSMENT: [OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- CANONICAL OUTCOME: [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

## Included (Backend `b09-bilreg-api`)
1. **Database Schema (`Bilreg.SqlDb.RadContext`)**:
   - DDL skrip migrasi idempoten untuk `BILRG_RadJadwal`, `BILRG_RadEquipment`, dan `BILRG_RadJadwalReschedule`.
   - Composite index untuk optimasi kueri deteksi tumpang-tindih waktu (*collision detection*) dan tampilan kalender.
   - Seed data awal master mesin fisik radiologi (`BILRG_RadEquipment`).
2. **Domain Layer (`Bilreg.Domain.RadContext`)**:
   - Aggregate Root `RadJadwalModel` (`RadModalityEnum`, enum status `Scheduled`/`Completed`/`Cancelled`, kategori pembatalan).
   - Value Objects untuk panduan persiapan, checklist skrining keselamatan, dan jejak audit.
   - Penambahan metode domain `Unschedule()` pada `RadOrderModel` di `RadOrderFeature`.
3. **Application Layer (`Bilreg.Application.RadContext.RadJadwalFeature`)**:
   - Kontrak port repository & data access (`IRadJadwalRepo`, `IRadJadwalDal`, `IRadEquipmentDal`).
   - Kontrak port integrasi tracker (`IRadTrackerIntegration`).
   - Command use cases: pembuatan jadwal baru, reschedule *in-place*, pembatalan decoupled (`RadJadwalCancelHandler`), cascade cancel pada `RadOrderCancelHandler`, dan verifikasi kesiapan/clearance.
   - Query use cases: kueri timeline kalender harian, worklist penjadwalan, detail jadwal, dan lookup master alat.
4. **Infrastructure Layer (`Bilreg.Infrastructure.RadContext.RadJadwalFeature`)**:
   - Implementasi DAL dan Repository menggunakan `System.Data.SqlClient` + Dapper murni dengan kuncian transaksi atomik (`TransactionScope` + `HOLDLOCK, UPDLOCK`) serta pemetaan `DateTime`/`TimeSpan` untuk `DateOnly`/`TimeOnly`.
   - Implementasi adapter integrasi tracker yang tangguh (*resilient/non-blocking try-catch*).
   - Pendaftaran dependensi DI pada `Bilreg.Api/Configurations/InfrastructureService.cs`.
5. **API Layer (`Bilreg.Api.Controllers.RadContext`)**:
   - Controller REST API `RadJadwalController` dan `RadEquipmentController` menggunakan `[Authorize]` dan `JSendOk`/`JSendFail`.
6. **Backend Verification (`Bilreg.Test.RadContext.RadJadwalFeature`)**:
   - Unit test domain model lifecycle, validation invariants, command/query handlers, dan DAL.

## Deferred (Out of Current Plan Scope)
- Implementasi Frontend (`c012_myhospital_web`: Types, TanStack Query composables, UI Dialogs, Print Confirmation Slip, dan Timeline Refactoring) ditangguhkan hingga perancangan UI/UX dan analisis BFF selesai.

---

# 3. Dependencies

- Solution Backend: `b09-bilreg-api` (C# .NET 8, `System.Data.SqlClient`, Dapper, MediatR, `Nuna.Lib`).
- Context Dependencies: `RadOrderFeature` (existing), `PatientSnapshotType`, `AuditTrailType`, `NunaId`, `ITglJamProvider`.
- External Integration: `IPasienTrackerRepo` / tracker service di `b09-bilreg-api`.

---

# 4. Progress Summary

| Phase | Title | Implementation Status | Review Status | Progress |
|---|---|---|---|---|
| P1 | Database Schema & Domain Modeling (BE) | IMPLEMENTED | GO | 3/3 |
| P2 | Application Use Cases & Contracts (BE) | IMPLEMENTED | GO | 3/3 |
| P3 | Infrastructure Persistence & Adapters (BE) | IMPLEMENTED | GO | 2/2 |
| P4 | API Controllers & Backend Tests (BE) | IMPLEMENTED | GO | 2/2 |

---

# 5. Phases

## P1 - Database Schema & Domain Modeling (BE)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P1-S01: Database Migrations for RadJadwal, RadEquipment & Reschedule

Title: Migrasi Tabel SQL Basis Data untuk RadJadwal, RadEquipment, dan Reschedule  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menyediakan skrip DDL SQL Server idempoten untuk tabel `BILRG_RadJadwal`, `BILRG_RadEquipment`, dan `BILRG_RadJadwalReschedule` beserta composite index untuk performa kueri deteksi tumpang-tindih waktu dan seed data awal master alat.  
Depends On: None  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.SqlDb/RadContext/RadJadwalFeature/BILRG_RadJadwal.sql`
- Created: `src/bilreg/Bilreg.SqlDb/RadContext/RadJadwalFeature/BILRG_RadEquipment.sql`
- Created: `src/bilreg/Bilreg.SqlDb/RadContext/RadJadwalFeature/BILRG_RadJadwalReschedule.sql`
- Created: `src/bilreg/Bilreg.SqlDb/RadContext/RadJadwalFeature/Seed_BILRG_RadEquipment.sql`  
Implementation Notes:
- Seluruh skrip DDL dibuat idempoten menggunakan `IF NOT EXISTS (SELECT * FROM sys.tables ...)` dan pengecekan `sys.indexes` untuk setiap indeks, mengikuti konvensi penamaan constraint `DF_{Table}_{Column}` dan `PK_{Table}` pada `RadOrderFeature` serta standar `DATABASE.md` (tanpa physical FK constraint, sentinel `'3000-01-01'` / `DEFAULT('')` untuk kolom non-null; `ArrivedAt` dan `ClearedAt` didefinisikan `DATETIME NULL` sesuai Section 8 arsitektur).
- `BILRG_RadJadwal.sql` mendefinisikan PK `JadwalRadId`, unique index `UX_BILRG_RadJadwal_ScheduleNo`, index `IX_BILRG_RadJadwal_OrderId` pada `(OrderId, Status)`, filtered index `IX_BILRG_RadJadwal_RegId`, dan composite index `IX_BILRG_RadJadwal_EquipmentSlot` pada `(EquipmentId, ScheduleDate, Status, StartTime, EndTime)`.
- `BILRG_RadEquipment.sql` mendefinisikan PK `EquipmentId` dan index `IX_BILRG_RadEquipment_Modality` pada `(Modality, IsActive)`.
- `BILRG_RadJadwalReschedule.sql` mendefinisikan PK `RescheduleId`, foreign key logis `JadwalRadId`, dan index `IX_BILRG_RadJadwalReschedule_JadwalRadId`.
- `Seed_BILRG_RadEquipment.sql` menyediakan seed data idempoten (`IF NOT EXISTS`) untuk modalitas `CT` (`EQ-CT01`, `EQ-CT02`), `MRI` (`EQ-MRI01`), `USG` (`EQ-USG01`, `EQ-USG02`), dan `Fluoroscopy` (`EQ-FLR01`).  
Completion Criteria:
- File DDL SQL dibuat dengan pengecekan idempoten `IF NOT EXISTS (SELECT * FROM sys.tables ...)`.
- Tabel `BILRG_RadJadwal` memiliki primary key `JadwalRadId`, unique index pada `ScheduleNo`, index pada `OrderId`, serta composite index pada `(EquipmentId, ScheduleDate, Status, StartTime, EndTime)`.
- Tabel `BILRG_RadEquipment` memiliki primary key `EquipmentId` dan data seed awal untuk modalitas CT, MRI, USG, dan Fluoroskopi.
- Tabel `BILRG_RadJadwalReschedule` memiliki foreign key logis ke `JadwalRadId`.

### P1-S02: Domain Models, Enums & Value Objects for RadJadwal

Title: Pembentukan Aggregate Root RadJadwalModel dan Value Objects Pendukung  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengimplementasikan aggregate root `RadJadwalModel` (menggunakan `RadModalityEnum`), enum status dan kategori pembatalan, value object checklist keselamatan klinis, serta aturan invarian domain (validasi rentang waktu dan aturan clearance gate).  
Depends On: None  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/IRadJadwalKey.cs`
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/RadJadwalStatusEnum.cs`
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/RadCancellationCategoryEnum.cs`
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/RadSafetyChecklistType.cs`
- Created: `src/bilreg/Bilreg.Domain/RadContext/RadJadwalFeature/RadJadwalModel.cs`  
Implementation Notes:
- Implemented `IRadJadwalKey`, `RadJadwalStatusEnum` (`Scheduled = 1`, `Completed = 2`, `Cancelled = 3`), `RadCancellationCategoryEnum` (`PasienBatal = 1`, `KondisiPasienTidakStabil = 2`, `MesinRusak = 3`, `NoShow = 4`, `OrderDibatalkan = 5`), and `RadSafetyChecklistType` record (`HasMetalImplant`, `HasContrastAllergy`, `IsPregnant`, `Notes`).
- Implemented `RadJadwalModel` aggregate root using `RadModalityEnum`, `PatientSnapshotType`, `AuditTrailType`, and `NunaId.New("JAD")` with `Create(...)`, `Reschedule(...)`, `Cancel(...)`, `RecordArrival(...)`, `VerifyClearance(...)`, and `Complete(...)` (enforcing `IsArrived` and `IsCleared` invariants and turnaround buffer defaults: 15 min for `MR`, 10 min otherwise).
- Verified compilation via `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Domain/Bilreg.Domain.csproj --no-restore` (0 errors, 0 warnings in `RadJadwalFeature`).  
Completion Criteria:
- `RadJadwalModel` memiliki factory method `Create(...)`, metode `Reschedule(...)`, `Cancel(...)`, `RecordArrival(...)`, dan `Complete(...)`.
- Metode `Complete(...)` menegakkan invariant: melempar exception jika `IsArrived == false` atau `IsCleared == false`.
- Status enum mendukung `Scheduled = 1`, `Completed = 2`, `Cancelled = 3`.

### P1-S03: Domain Lifecycle Extension on RadOrderModel (Unschedule)

Title: Penambahan Metode Unschedule pada RadOrderModel untuk Decoupled Cancellation  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menambahkan metode `Unschedule(string userId, DateTime unscheduledAt)` pada `RadOrderModel` agar order dapat dikembalikan dari status `Scheduled` ke `Requested` saat jadwal dibatalkan, serta menulis unit test siklus hidupnya.  
Depends On: None  
Repository: `b09-bilreg-api`  
Changed Files:
- Modified: `src/bilreg/Bilreg.Domain/RadContext/RadOrderFeature/RadOrderModel.cs`
- Modified: `src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderModelTest.cs`  
Implementation Notes:
- Menambahkan metode `public void Unschedule(string userId, DateTime unscheduledAt = default)` pada `RadOrderModel` dengan guard `Guard.Against.NullOrWhiteSpace(userId, nameof(userId))`, pengecekan `AuditTrail.IsVoided` dan `RadOrderStatus != RadOrderStatusEnum.Scheduled` (melempar `InvalidOperationException`), serta transisi `RadOrderStatus = RadOrderStatusEnum.Requested` dan pencatatan `AuditTrail.Modif(userId, at)`.
- Menambahkan unit test pada `RadOrderModelTest` untuk skenario transisi `Schedule()` -> `Unschedule()` (dengan timestamp eksplisit maupun default) serta guard invalid state (`Requested`, `Charged`, `Cancelled`, voided, dan `userId` kosong/whitespace).
- Verifikasi test: `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RadOrderModelTest"` lulus seluruhnya (40/40 passed, 0 failed, 0 skipped).  
Completion Criteria:
- `RadOrderModel` memiliki metode `public void Unschedule(string userId, DateTime unscheduledAt = default)`.
- Validasi guard: melempar exception jika order void atau jika status bukan `Scheduled`.
- Memperbarui `RadOrderStatus` menjadi `RadOrderStatusEnum.Requested` dan mencatat audit modifikasi.
- Unit test `RadOrderModelTest` mencakup skenario transisi `Schedule()` lalu `Unschedule()`.

---

## P2 - Application Use Cases & Contracts (BE)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P2-S04: Port Interfaces & DTO Contracts for RadJadwal

Title: Definisi Kontrak Port Repository, DAL, dan DTO Jadwal Radiologi  
Implementation Status: IMPLEMENTED  
Review Status: GO  
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
Implementation Notes:
- Implemented `IRadJadwalDal` inheriting `IInsert<RadJadwalDto>` and `IUpdate<RadJadwalDto>` (`Nuna.Lib.DataAccessHelper`) with `GetData(IRadJadwalKey)`, `GetActiveByOrderId(string)`, `ListByOrderId(string)`, `ListByDateRange(...)`, `ListCalendarSlots(...)`, `IsSlotOverlapping(string equipmentId, DateTime scheduleDate, TimeSpan startTime, TimeSpan endTime, string? excludeJadwalId = null)`, `InsertReschedule(...)`, and `ListRescheduleHistory(...)`.
- Implemented `IRadJadwalRepo` inheriting `ISaveChange<RadJadwalModel>` and `ILoadEntity<RadJadwalModel, IRadJadwalKey>` with `MayBe<RadJadwalModel> LoadActiveByOrderId(string orderId)`, `IEnumerable<RadJadwalModel> ListByOrderId(string orderId)`, and `SaveReschedule(RadJadwalModel model, RadJadwalRescheduleDto rescheduleHistory)`.
- Implemented `IRadEquipmentDal` (`GetData`, `ListByModality`, `ListActive`, `ListAll`) and `IRadTrackerIntegration` (`PublishMilestoneAsync(regId, jadwalRadId, milestone, note, ct)`).
- Implemented `RadJadwalDto` (aligned with `BILRG_RadJadwal` columns, `SafetyChecklistJson` serialization, `ScheduleDate` as `DateTime` and `StartTime`/`EndTime` as `TimeSpan` converted to `DateOnly`/`TimeOnly` in `ToModel()`/`FromModel()`), `RadJadwalRescheduleDto`, `RadEquipmentDto`, and `RadJadwalCalendarDto` (`RadJadwalCalendarSlotDto`, `RadJadwalCalendarEquipmentDto`, `RadJadwalCalendarDto`).
- Verified compilation via `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Application/Bilreg.Application.csproj --no-restore` (0 errors, 0 warnings in `RadJadwalFeature`).  
Completion Criteria:
- `IRadJadwalDal` memiliki method `IsSlotOverlapping(equipmentId, scheduleDate, startTime, endTime, excludeJadwalId)`.
- `IRadJadwalRepo` memiliki method `LoadActiveByOrderId(string orderId)` untuk mendukung pengecekan jadwal aktif dan cascade cancel dari order.
- `IRadTrackerIntegration` memiliki method `PublishMilestoneAsync(regId, jadwalRadId, milestone, note, ct)`.
- DTO mencakup data jadwal lengkap, snapshot pasien, rincian waktu, checklist keselamatan, dan jejak reschedule.

### P2-S05: Application Commands (Create, Reschedule, Cancel, Clearance) & Handlers

Title: Implementasi Use Case Application Commands dan Orkestrasi Transaksi  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengimplementasikan handler use cases: pembuatan jadwal baru dengan pengecekan collision, reschedule in-place, pembatalan decoupled dengan pemanggilan `RadOrder.Unschedule()`, pembaruan `RadOrderCancelHandler` untuk cascade cancel jadwal aktif, verifikasi kesiapan/clearance, serta publikasi milestone ke tracker.  
Depends On: P1-S03, P2-S04  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCreateCmd.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalRescheduleCmd.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCancelCmd.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalClearanceCmd.cs`
- Modified: `src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/UseCases/RadOrderCancelCmd.cs`  
Implementation Notes:
- Implemented `RadJadwalCreateCmd` & `RadJadwalCreateHandler`: validates `OrderId` (`Requested` and non-void/non-cancelled), validates active matching equipment from `IRadEquipmentDal` (normalizing `MRI`->`MR`, `USG`->`US`), computes modality-based default duration (or uses custom duration) plus turnaround buffer, checks `IRadJadwalDal.IsSlotOverlapping` (throwing `RadJadwalSlotConflictException` on conflict), generates `ScheduleNo` via `ISequencer.GetNextNo(RadJadwalModel.ScheduleNoPrefix, now)`, creates `RadJadwalModel`, calls `order.Schedule(...)`, persists both atomically in `TransHelper.NewScope()`, and publishes `"Scheduled"` milestone resiliently via `IRadTrackerIntegration`.
- Implemented `RadJadwalRescheduleCmd` & `RadJadwalRescheduleHandler`: validates `Scheduled` non-void schedule and active matching equipment, checks `IRadJadwalDal.IsSlotOverlapping(..., excludeJadwalId: request.JadwalRadId)`, snapshots previous slot into `RadJadwalRescheduleDto`, calls `jadwal.Reschedule(...)`, persists via `_radJadwalRepo.SaveReschedule(jadwal, snapshot)`, and publishes `"Rescheduled"` milestone resiliently.
- Implemented `RadJadwalCancelCmd` & `RadJadwalCancelHandler`: calls `jadwal.Cancel(...)`, loads linked `RadOrderModel` and calls `order.Unschedule(...)` if status is `Scheduled` and non-void, persists atomically in `TransHelper.NewScope()`, and publishes `"Cancelled"` milestone resiliently.
- Implemented `RadJadwalClearanceCmd` & `RadJadwalClearanceHandler`: calls `RecordArrival` when `IsArrived` is true and not yet arrived, calls `VerifyClearance` with `SafetyChecklist` and `ClearanceNotes`, calls `Complete(...)` when both `IsArrived` and `IsCleared` are true (otherwise keeps status `Scheduled`), persists via `_radJadwalRepo.SaveChanges`, and publishes `"Completed"` milestone resiliently upon completion.
- Updated `RadOrderCancelHandler` in `RadOrderCancelCmd.cs` with optional constructor-injected `IRadJadwalRepo?` and `IRadTrackerIntegration?` (preserving 3-arg constructor compatibility for existing callers/tests) to load active schedule via `LoadActiveByOrderId`, cancel it with `RadCancellationCategoryEnum.OrderDibatalkan`, persist atomically with order cancellation, and publish `"Cancelled"` milestone resiliently.
- Verified compilation via `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Application/Bilreg.Application.csproj --no-restore -p:UseSharedCompilation=false` (0 errors, 0 warnings in `RadContext`) and verified existing tests via `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RadOrderCancel"` (16/16 passed) and `FullyQualifiedName~RadOrderLifecycleScenario` (12/12 passed).
- **Remediation (2026-11-26 — `REV-P2S05-001`, `REV-P2S05-002`)**:
  - `RadJadwalCreateCmd.cs`: Moved `RadJadwalCommandSupport.EnsureSlotAvailable(...)` inside `using (var trans = TransHelper.NewScope())` immediately before `_radJadwalRepo.SaveChanges(jadwal)` and `_radOrderRepo.SaveChanges(order)` so `UPDLOCK, HOLDLOCK` locks are held until transaction commit.
  - `RadJadwalRescheduleCmd.cs`: Wrapped `RadJadwalCommandSupport.EnsureSlotAvailable(...)` through `_radJadwalRepo.SaveReschedule(jadwal, snapshot)` inside `using (var trans = TransHelper.NewScope())` with `trans.Complete()` so `UPDLOCK, HOLDLOCK` locks are held across overlap check, schedule update, and reschedule history insert.
  - Re-verified compilation via `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Application/Bilreg.Application.csproj --no-restore -p:UseSharedCompilation=false` (0 errors, 0 warnings in `RadContext`/`RadJadwalFeature`).  
Completion Criteria:
- `RadJadwalCreateHandler` memvalidasi kelayakan order, kueri collision detection, menyimpan jadwal, memanggil `order.Schedule()`, dan mempublikasikan milestone `Scheduled`.
- `RadJadwalRescheduleHandler` memvalidasi slot baru bebas benturan, memperbarui jadwal, mencatat riwayat reschedule, dan mempublikasikan milestone `Rescheduled`.
- `RadJadwalCancelHandler` menandai jadwal `Cancelled`, memanggil `order.Unschedule()`, dan mempublikasikan milestone `Cancelled`.
- `RadOrderCancelHandler` memeriksa jadwal aktif via `IRadJadwalRepo.LoadActiveByOrderId` dan otomatis membatalkan slot jadwal terkait saat order dibatalkan.
- `RadJadwalClearanceHandler` memperbarui `IsArrived` dan `IsCleared`, mentransisikan status ke `Completed`, dan mempublikasikan milestone `Completed`.

### P2-S06: Application Queries (Get, Worklist, CalendarTimeline, EquipmentLookup) & Handlers

Title: Implementasi Use Case Application Queries untuk Kalender dan Worklist  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengimplementasikan handler query untuk pengambilan data detail jadwal, daftar kerja penjadwalan (*worklist*), data lini masa kalender harian per mesin, dan lookup master mesin fisik aktif.  
Depends On: P2-S04  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalGetQuery.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalWorklistQuery.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCalendarTimelineQuery.cs`
- Created: `src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadEquipmentLookupQuery.cs`  
Implementation Notes:
- Implemented `RadJadwalGetQuery` / `RadJadwalGetHandler` (`IRadJadwalKey`) returning `RadJadwalDetailResponse(Header, RescheduleHistory)` ordered by `RescheduleIndex` (or `null` when schedule is not found).
- Implemented `RadJadwalWorklistQuery` / `RadJadwalWorklistHandler` returning `RadJadwalWorklistResponse(Schedules, PendingOrders)`: queries schedules via `IRadJadwalDal.ListByDateRange(startDate, endDate, modality, status, equipmentId)` and pending unscheduled orders (`RadOrderStatusEnum.Requested`) across the date range via `IRadOrderDal.GetWorklist` when `Status` is `null` or `Scheduled`.
- Implemented `RadJadwalCalendarTimelineQuery` / `RadJadwalCalendarTimelineHandler` returning `RadJadwalCalendarDto` by combining active equipment (`IRadEquipmentDal`) and scheduled slots (`IRadJadwalDal.ListCalendarSlots`) grouped by `EquipmentId` and sorted by `StartTime`, preserving slots whose equipment is inactive.
- Implemented `RadEquipmentLookupQuery` / `RadEquipmentLookupHandler` returning active equipment filtered by optional `Modality` (with `ActiveOnly = true` default) via `IRadEquipmentDal`.
- Verified compilation via `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Application/Bilreg.Application.csproj --no-restore` (0 errors, 0 warnings in `RadJadwalFeature`).  
Completion Criteria:
- `RadJadwalCalendarTimelineHandler` mengembalikan daftar slot terjadwal dikelompokkan per mesin/modalitas untuk tanggal tertentu.
- `RadJadwalWorklistHandler` mengembalikan order yang memerlukan jadwal dan jadwal aktif dengan filter status dan rentang tanggal.
- `RadEquipmentLookupHandler` mengembalikan daftar mesin aktif berdasarkan filter modalitas.

---

## P3 - Infrastructure Persistence & Adapters (BE)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P3-S07: DAL & Repository Implementation for RadJadwal and RadEquipment

Title: Implementasi Akses Data SQL Server Menggunakan Dapper dan Kuncian Konkurensi  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengimplementasikan `RadJadwalDal`, `RadJadwalRepo`, dan `RadEquipmentDal` menggunakan `System.Data.SqlClient` + Dapper murni dengan transaksi berpelindung kuncian konkurensi (`TransactionScope` + `HOLDLOCK, UPDLOCK`) saat pengecekan benturan waktu.  
Depends On: P1-S01, P2-S04  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadJadwalDal.cs`
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadJadwalRepo.cs`
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadEquipmentDal.cs`
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadJadwalRescheduleDal.cs`  
Implementation Notes:
- Implemented `RadJadwalDal` (`IRadJadwalDal`) using `System.Data.SqlClient` + Dapper, `IOptions<DatabaseOptions>`, and `ConnStringHelper.Get(_opt)` covering `Insert`, `Update`, `GetData`, `GetActiveByOrderId`, `ListByOrderId`, `ListByDateRange`, `ListCalendarSlots`, `IsSlotOverlapping`, `InsertReschedule`, and `ListRescheduleHistory`. `ScheduleDate` is bound as `DbType.Date` (`DateTime`) and `StartTime`/`EndTime` as `DbType.Time` (`TimeSpan`), with nullable `ArrivedAt`/`ClearedAt` bound as `DbType.DateTime`.
- Implemented `IsSlotOverlapping(...)` using `SELECT TOP 1 1 FROM BILRG_RadJadwal WITH (UPDLOCK, HOLDLOCK)` checking `EquipmentId = @EquipmentId AND ScheduleDate = @ScheduleDate AND Status = @ScheduledStatus AND StartTime < @EndTime AND EndTime > @StartTime` (with optional `JadwalRadId <> @ExcludeJadwalId`).
- Implemented `RadJadwalRescheduleDal` (`IRadJadwalRescheduleDal`) for `BILRG_RadJadwalReschedule` insert and history query (`ListByJadwalRadId`), integrated cleanly with `RadJadwalDal` so DI only requires `IRadJadwalDal` + `IOptions<DatabaseOptions>`.
- Implemented `RadJadwalRepo` (`IRadJadwalRepo`) using `TransactionScope(TransactionScopeAsyncFlowOption.Enabled)` for atomic `SaveChanges` (upsert via `LoadEntity` + `MayBe<RadJadwalModel>.Match`) and `SaveReschedule` (updating `BILRG_RadJadwal` and inserting `BILRG_RadJadwalReschedule`), plus `LoadEntity`, `LoadActiveByOrderId`, and `ListByOrderId`.
- Implemented `RadEquipmentDal` (`IRadEquipmentDal`) querying `BILRG_RadEquipment` (`GetData`, `ListByModality` with `MR`/`MRI` and `US`/`USG` alias normalization, `ListActive`, `ListAll`).
- Verified compilation via `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/Bilreg.Infrastructure.csproj --no-restore` (0 errors, 0 warnings in `RadJadwalFeature`).  
Completion Criteria:
- `RadJadwalDal.IsSlotOverlapping` mengeksekusi SQL kueri overlap dengan parameter `EquipmentId`, `ScheduleDate` (`DateTime`), `StartTime` (`TimeSpan`), `EndTime` (`TimeSpan`), dan status `Scheduled`.
- Penyimpanan jadwal dan riwayat reschedule dieksekusi secara atomik dalam `TransactionScope` dengan `HOLDLOCK, UPDLOCK`.
- Pemetaan data dari tabel `BILRG_RadJadwal` ke `RadJadwalModel` akurat dan konsisten (termasuk konversi `DateTime`/`TimeSpan` ke `DateOnly`/`TimeOnly`).

### P3-S08: Tracker Integration Adapter & Dependency Injection Registration

Title: Adapter Integrasi Tracker Resilient dan Pendaftaran Dependensi Service  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengimplementasikan adapter `RadTrackerIntegration` yang memanggil `IPasienTrackerRepo` / service tracker secara resilient (non-blocking) dan mendaftarkan seluruh service, repository, dan DAL pada container DI di `Bilreg.Api/Configurations/InfrastructureService.cs`.  
Depends On: P2-S04  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadTrackerIntegration.cs`
- Modified: `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs`  
Implementation Notes:
- Implemented `RadTrackerIntegration` (`IRadTrackerIntegration`) resolving the patient's `PasienTrackerModel` (direct `IPasienTrackerRepo.LoadEntity` fallback, then `IRegRepo` + `IPasienTrackerRepo.ListData(periode, TglLahir)` matching `ReffId == regId`), formatting milestone event names (`RAD_{MILESTONE}` capped to 50 chars, `ReffId` capped to 40 chars), appending via `tracker.AddEvent(...)`, and persisting via `IPasienTrackerRepo.SaveChanges(tracker)`.
- Wrapped `PublishMilestoneAsync` in a resilient `try { ... } catch (Exception ex) { _logger.LogWarning(ex, ...); }` block so tracker lookup/persistence errors are logged and never fail the primary scheduling transaction.
- Registered `IRadJadwalRepo` -> `RadJadwalRepo`, `IRadJadwalDal` -> `RadJadwalDal`, `IRadJadwalRescheduleDal` -> `RadJadwalRescheduleDal`, `IRadEquipmentDal` -> `RadEquipmentDal`, and `IRadTrackerIntegration` -> `RadTrackerIntegration` as scoped services in `Bilreg.Api/Configurations/InfrastructureService.cs` following the `RadOrderFeature` pattern.
- Verified compilation via `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/Bilreg.Infrastructure.csproj --no-restore` and `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Api/Bilreg.Api.csproj --no-restore` (0 errors, 0 warnings in `RadJadwalFeature`).  
Completion Criteria:
- `RadTrackerIntegration` menangkap exception tracker dan mencatatnya ke logger tanpa melempar kegagalan ke alur utama transaksi.
- Seluruh interface `IRadJadwalRepo`, `IRadJadwalDal`, `IRadEquipmentDal`, `IRadTrackerIntegration` terdaftar pada `InfrastructureService.cs`.

---

## P4 - API Controllers & Backend Tests (BE)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P4-S09: REST API Controllers (RadJadwalController & RadEquipmentController)

Title: Endpoint Controller REST API untuk Penjadwalan dan Master Mesin Radiologi  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menyediakan endpoint REST API untuk CRUD jadwal, reschedule, pembatalan, clearance check, kueri timeline kalender, dan lookup mesin alat.  
Depends On: P2-S05, P2-S06, P3-S07, P3-S08  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Api/Controllers/RadContext/RadJadwalController.cs`
- Created: `src/bilreg/Bilreg.Api/Controllers/RadContext/RadEquipmentController.cs`  
Implementation Notes:
- Implemented `RadJadwalController` (`[Route("api/rad/jadwal")]`, `[ApiController]`, `[Authorize]`) exposing `POST` (`RadJadwalCreateCmd`), `POST {id}/reschedule` (`RadJadwalRescheduleCmd`), `POST {id}/cancel` (`RadJadwalCancelCmd`), `POST {id}/clearance` (`RadJadwalClearanceCmd`), `GET {id}` (`RadJadwalGetQuery`, returning `NotFound(new JSendFail(...))` when null), `GET calendar` (`RadJadwalCalendarTimelineQuery`), and `GET worklist` (`RadJadwalWorklistQuery`).
- Handled `RadJadwalSlotConflictException` in `Create` and `Reschedule` by returning `Conflict(new JSendFail(new { code = RadJadwalSlotConflictException.Code, message = ex.Message }))`, and wrapped all success responses in `Ok(new JSendOk(response))` matching `RadOrderController` (reusing `JSendFail` in `Bilreg.Api.Controllers.RadContext`).
- Implemented `RadEquipmentController` (`[Route("api/rad/equipment")]`, `[ApiController]`, `[Authorize]`) exposing `GET` (`RadEquipmentLookupQuery` with optional `modality` and `activeOnly = true`) wrapped in `Ok(new JSendOk(response))`.
- Verified compilation via `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Api/Bilreg.Api.csproj --no-restore` (0 errors, 0 warnings in `RadContext`).  
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
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menulis dan mengeksekusi test suite komprehensif di `Bilreg.Test` untuk memverifikasi model domain `RadJadwalModel`, logika deteksi tabrakan waktu, use case command handlers (termasuk cascade cancel di `RadOrderCancelHandler`), dan pemetaan data.  
Depends On: P4-S09  
Repository: `b09-bilreg-api`  
Changed Files:
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalModelTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalCreateHandlerTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalRescheduleHandlerTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalCancelHandlerTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalClearanceHandlerTest.cs`
- Created: `src/bilreg/Bilreg.Test/RadContext/RadJadwalFeature/RadJadwalQueriesTest.cs`
- Modified: `src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderCancelCmdTest.cs`  
Implementation Notes:
- Created `RadJadwalModelTest.cs` covering `RadJadwalModel` creation, modality-based turnaround buffer defaults (15m for `MR`, 10m for `CT`/`US`/`CR`/`DX`), in-place `Reschedule(...)`, `Cancel(...)`, `RecordArrival(...)`, `VerifyClearance(...)`, clearance gate invariants (`Complete()` throwing `InvalidOperationException` when `!IsArrived` or `!IsCleared`), and `RadJadwalDto.FromModel` / `ToModel()` round-trip mapping.
- Created `RadJadwalCreateHandlerTest.cs` covering valid `Requested` order scheduling (with `Order.Schedule(...)`, `TransHelper`, and `"Scheduled"` tracker milestone), `MRI`/`USG` modality alias normalization, `RadJadwalSlotConflictException` collision check without persisting, inactive/missing/mismatched equipment, invalid/voided order statuses, and resilient tracker error handling.
- Created `RadJadwalRescheduleHandlerTest.cs` covering in-place reschedule preserving `JadwalRadId`/`ScheduleNo`, collision check excluding self (`excludeJadwalId`), `RadJadwalRescheduleDto` history snapshot of prior state, and tracker milestone `"Rescheduled"`.
- Created `RadJadwalCancelHandlerTest.cs` covering decoupled schedule cancellation reverting linked `RadOrderModel` via `Unschedule()` back to `Requested`, skipping unschedule for non-`Scheduled` orders, and `"Cancelled"` milestone publishing.
- Created `RadJadwalClearanceHandlerTest.cs` covering arrival + clearance transitioning to `Completed` + `"Completed"` milestone, staying `Scheduled` without milestone when `!IsCleared` or `!IsArrived`, and preserving prior `ArrivedAt`.
- Created `RadJadwalQueriesTest.cs` covering `RadJadwalGetHandler`, `RadJadwalWorklistHandler`, `RadJadwalCalendarTimelineHandler`, and `RadEquipmentLookupHandler`.
- Updated `RadOrderCancelCmdTest.cs` with cascade cancellation test cases verifying `RadOrderCancelHandler` cancels active `RadJadwalModel` (`RadCancellationCategoryEnum.OrderDibatalkan`), skips when none active, and remains resilient if tracker fails.
- Verified via `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RadJadwal|FullyQualifiedName~RadOrderCancel"`: 91/91 passed, 0 failed, 0 skipped.  
Completion Criteria:
- Seluruh unit test baru dan terkait lulus:
  `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RadJadwal|FullyQualifiedName~RadOrderCancel"`
- Verifikasi invariant clearance gate, collision detection, cascade cancel, dan transisi `Unschedule()`.

---

# 6. Change Log

- 2026-11-26: Dibuat sebagai *replacement implementation plan* (`OC-RAD-JADWAL-BE-IMPLEMENTATION-PLAN.md`) menggantikan `OC-RAD-JADWAL-IMPLEMENTATION-PLAN.md` untuk memfokuskan eksekusi pada Backend Core (`b09-bilreg-api`, P1–P4 / S01–S10). Ruang lingkup Frontend (`c012_myhospital_web`) ditangguhkan hingga perancangan UI/UX dan analisis BFF selesai. Status `Execution Approval: APPROVED`.
