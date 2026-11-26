---
Title: Penjadwalan Pemeriksaan Radiologi (JadwalRadiologi) Backend Review
Code: OC-RAD-JADWAL-BE
Artifact: REVIEW
Version: 1.0
LastUpdated: 2026-11-26
ReviewIteration: 1
---

# 1. Review Summary

- **Plan**: [OC-RAD-JADWAL-BE-IMPLEMENTATION-PLAN.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-BE-IMPLEMENTATION-PLAN.md)
- **Architecture**: [OC-RAD-JADWAL-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-ARCHITECTURE.md)
- **Slice Reviewed**: `P2-S05` — Application Commands (Create, Reschedule, Cancel, Clearance) & Handlers
- **Decision**: `GO`
- **Verification Build (Iteration 1)**: `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Application/Bilreg.Application.csproj --no-restore -p:UseSharedCompilation=false` — Build succeeded (0 Warning(s), 0 Error(s)).
- **Verification Test Run (Iteration 0)**: `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RadOrderCancel"` — 16/16 passed, 0 failed, 0 skipped.

---

# 2. Findings

## REV-P2S05-001
- **Severity**: `MAJOR`
- **Resolution Status**: `RESOLVED`
- **Description**: `RadJadwalCreateHandler` mengeksekusi pengecekan benturan waktu (`EnsureSlotAvailable` -> `IRadJadwalDal.IsSlotOverlapping` dengan `WITH (UPDLOCK, HOLDLOCK)`) di luar cakupan `TransHelper.NewScope()`, sehingga kuncian transaksi database langsung dilepas sebelum penulisan jadwal dan pembaruan order dimulai. Hal ini melanggar arsitektur **TD-02** dan **Section 9.1** (*Concurrency & Race Condition Control*).
- **Evidence**:
  - [RadJadwalCreateCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCreateCmd.cs#L109-L149): `RadJadwalCommandSupport.EnsureSlotAvailable(...)` dipanggil pada baris 109–115, sedangkan `using (var trans = TransHelper.NewScope())` baru dibuka pada baris 144 untuk `_radJadwalRepo.SaveChanges(jadwal)` dan `_radOrderRepo.SaveChanges(order)`.
  - [RadJadwalDal.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadJadwalDal.cs#L245-L278): `IsSlotOverlapping` membuka `SqlConnection` sendiri dan menjalankan `SELECT TOP 1 1 FROM BILRG_RadJadwal WITH (UPDLOCK, HOLDLOCK) ...` yang akan langsung *autocommit* saat dipanggil di luar `TransactionScope`.
- **Required Correction**:
  - Bungkus pemanggilan `RadJadwalCommandSupport.EnsureSlotAvailable(...)` bersama `_radJadwalRepo.SaveChanges(jadwal)` dan `_radOrderRepo.SaveChanges(order)` di dalam blok `using (var trans = TransHelper.NewScope())` yang sama (panggil `EnsureSlotAvailable(...)` setelah `TransHelper.NewScope()` dibuka dan sebelum `SaveChanges`), sehingga kuncian `UPDLOCK, HOLDLOCK` bertahan hingga transaksi komit.
- **Remediation Evidence (Iteration 1)**:
  - [RadJadwalCreateCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCreateCmd.cs#L136-L149): `RadJadwalCommandSupport.EnsureSlotAvailable(...)` kini dieksekusi di dalam `using (var trans = TransHelper.NewScope())` sebelum `_radJadwalRepo.SaveChanges(jadwal)`, `_radOrderRepo.SaveChanges(order)`, dan `trans.Complete()`.

## REV-P2S05-002
- **Severity**: `MAJOR`
- **Resolution Status**: `RESOLVED`
- **Description**: `RadJadwalRescheduleHandler` mengeksekusi pengecekan benturan waktu (`EnsureSlotAvailable` -> `IRadJadwalDal.IsSlotOverlapping` dengan `WITH (UPDLOCK, HOLDLOCK)`) tanpa transaksi pembungkus `TransHelper.NewScope()` bersama `_radJadwalRepo.SaveReschedule(...)`. `RadJadwalRepo.SaveReschedule` membuka `TransactionScope` internal sendiri setelah kueri overlap sudah selesai dan kunciannya telah dilepas. Hal ini melanggar **TD-02** dan **Section 9.1**, meninggalkan celah *race condition* (*double-booking*) pada penjadwalan ulang konkuren.
- **Evidence**:
  - [RadJadwalRescheduleCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalRescheduleCmd.cs#L73-L105): `RadJadwalCommandSupport.EnsureSlotAvailable(...)` dipanggil pada baris 73–80 di luar transaksi, dan `_radJadwalRepo.SaveReschedule(jadwal, snapshot)` dipanggil pada baris 105 tanpa `TransHelper.NewScope()` yang melingkupi keduanya.
  - [RadJadwalRepo.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/RadContext/RadJadwalFeature/RadJadwalRepo.cs#L30-L39): `SaveReschedule` hanya membungkus `Update` dan `InsertReschedule`, bukan pengecekan overlap sebelumnya.
- **Required Correction**:
  - Bungkus `RadJadwalCommandSupport.EnsureSlotAvailable(...)` hingga `_radJadwalRepo.SaveReschedule(jadwal, snapshot)` dalam satu `using (var trans = TransHelper.NewScope())` dan panggil `trans.Complete()` setelah penyimpanan selesai agar kuncian `UPDLOCK, HOLDLOCK` bertahan hingga pembaruan jadwal dan penulisan histori reschedule komit secara atomik.
- **Remediation Evidence (Iteration 1)**:
  - [RadJadwalRescheduleCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalRescheduleCmd.cs#L74-L110): `RadJadwalCommandSupport.EnsureSlotAvailable(...)`, mutasi domain `jadwal.Reschedule(...)`, `_radJadwalRepo.SaveReschedule(jadwal, snapshot)`, dan `trans.Complete()` kini dibungkus di dalam satu blok `using (var trans = TransHelper.NewScope())`.

---

# 3. Verified Passing Items (P2-S05)

- `RadJadwalCancelCmd` & `RadJadwalCancelHandler` ([RadJadwalCancelCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalCancelCmd.cs)): membatalkan jadwal, memanggil `order.Unschedule(...)` bila order berstatus `Scheduled`, menyimpan keduanya dalam `TransHelper.NewScope()`, dan menerbitkan milestone `"Cancelled"` secara resilient.
- `RadJadwalClearanceCmd` & `RadJadwalClearanceHandler` ([RadJadwalClearanceCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadJadwalFeature/RadJadwalClearanceCmd.cs)): mencatat kedatangan (`RecordArrival`), memverifikasi keselamatan (`VerifyClearance`), hanya mentransisikan ke `Completed` saat `IsArrived && IsCleared` (TD-04 / EX-04), dan menerbitkan milestone `"Completed"` secara resilient saat selesai.
- `RadOrderCancelHandler` ([RadOrderCancelCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/UseCases/RadOrderCancelCmd.cs)): memuat jadwal aktif via `IRadJadwalRepo.LoadActiveByOrderId`, membatalkan jadwal dengan `RadCancellationCategoryEnum.OrderDibatalkan` secara atomik bersama pembatalan order dalam `TransHelper.NewScope()`, dan menjaga kompatibilitas konstruktor opsional (16/16 test `RadOrderCancel` lulus).

---

# 4. Remediation History & Review Iterations

| Iteration | Date | Reviewer | Decision | Notes |
|---|---|---|---|---|
| 0 | 2026-11-26 | `ica-reviewer` | `NO-GO` | Initial review of `P2-S05`. Found 2 MAJOR concurrency/transaction scope defects (`REV-P2S05-001`, `REV-P2S05-002`) violating TD-02 and Section 9.1. |
| 1 | 2026-11-26 | `ica-reviewer` | `GO` | Re-review of `P2-S05`. Both `REV-P2S05-001` and `REV-P2S05-002` verified resolved (`EnsureSlotAvailable` wrapped in `TransHelper.NewScope()` with `trans.Complete()`). `Bilreg.Application` build passed (0 warnings, 0 errors). |
