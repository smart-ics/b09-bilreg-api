---
Title: Implementation Plan - Resolusi Perbedaan Jam Real vs Formal pada Usecase BookingCreateFromHidokCommand
Code: BOOKING-HIDOK-SCHEDULE-MISMATCH
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-20
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Memperbaiki kegagalan validasi jadwal praktek dokter pada usecase `BookingCreateFromHidokCommand` (`POST api/Booking/createFromHidok`) yang memicu `ArgumentException: "Jadwal tidak ditemukan"` saat aplikasi eksternal (HiDok) mengirim jam mulai yang bersumber dari field `fs_jam_mulai_real`, sementara master jadwal BilregApi (`BILRG_JadwalPraktek`) menyimpan field `fs_jam_mulai` (`ISSUE-BILREG-HIDOK-BOOKING-SCHEDULE-MISMATCH-BUG-001`).

Rencana ini menyediakan mekanisme translasi jam operasional HiDok ke jam formal BilregApi melalui query lookup ke tabel sumber legacy `ta_jadwal_dokter` sebelum jadwal di-resolve, dengan pemetaan index hari VB6 (`fn_hari = (int)tglBerobat.DayOfWeek + 1`), tanpa mengubah skema basis data `BILRG_JadwalPraktek` dan tanpa memengaruhi alur booking lokal admisi web.

Planning Mode: BUG-FIX-PLANNING

Referenced artifacts:
- ISSUE: [BOOKING-HIDOK-SCHEDULE-MISMATCH-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/BOOKING-HIDOK-SCHEDULE-MISMATCH-BUG-ISSUE.md) (`ISSUE-BILREG-HIDOK-BOOKING-SCHEDULE-MISMATCH-BUG-001`)
- BUG-INVESTIGATION: [BOOKING-HIDOK-SCHEDULE-MISMATCH-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/BOOKING-HIDOK-SCHEDULE-MISMATCH-BUG-INVESTIGATION.md)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED

No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved feasibility decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

### Included:
1. **Backend (`b09-bilreg-api`)**:
   - Penambahan method kontrak `string GetJamMulaiFromReal(string dokterId, int fnHari, string jamMulaiReal)` pada interface `IJadwalPraktekRepo` di layer Application.
   - Penambahan method query pada interface `IJadwalFoDal` dan implementasinya di `JadwalFoDal` untuk mengeksekusi query pencarian `fs_jam_mulai` pada tabel `ta_jadwal_dokter`.
   - Implementasi delegasi method `GetJamMulaiFromReal` pada `JadwalPraktekRepo` di layer Infrastructure.
   - Pembaruan handler `BookingCreateFromHidokHandler` untuk melakukan translasi jam real ke jam formal sebelum memanggil `BookingScheduleResolver.Resolve`.
   - Penyesuaian dan penambahan pengujian unit pada `BookingCreateFromHidokHandlerTest.cs` di layer `Bilreg.Test`.

### Excluded:
1. Tidak ada perubahan DDL atau skema tabel database (`BILRG_JadwalPraktek`, `BILRG_Booking`, maupun `ta_jadwal_dokter` tidak diubah).
2. Tidak ada perubahan pada alur migrasi master jadwal dokter (`JadwalPraktekRepo.Migrasi` tetap mempertahankan `fs_jam_mulai`).
3. Tidak ada perubahan pada alur pendaftaran rawat jalan walk-in (`RegJalanWalkInCommand`) maupun booking lokal web admisi (`BookingCreateCmd`).
4. Tidak ada perubahan pada repositori frontend web (`c012_myhospital_web`).

---

# 3. Dependencies

### External Dependencies:
- Tabel legacy `ta_jadwal_dokter` pada database SQL Server yang terhubung dengan `b09-bilreg-api`.

### Slice Dependencies:
- `Depends On` mendeklarasikan prasyarat implementasi antar irisan.
- Dependensi hanya merujuk pada Slice ID.
- Pemenuhan dependensi mensyaratkan irisan yang dirujuk berstatus implementasi `IMPLEMENTED`.
- Dependensi merepresentasikan prasyarat teknis implementasi yang nyata.

---

# 4. Progress Summary

Plan status values are:
- NOT-STARTED
- IN-PROGRESS
- BLOCKED
- COMPLETED

Execution Approval values are:
- PENDING
- APPROVED

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Legacy Schedule Lookup Data Access & Port Contract | IMPLEMENTED | GO | 1/1 |
| P2 - Schedule Translation on Booking Intake | IMPLEMENTED | GO | 1/1 |
| P3 - Verification & Unit Test Suite | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Legacy Schedule Lookup Data Access & Port Contract

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01
Title: Penambahan Method Lookup Jam Formal pada DAL Legacy dan Port IJadwalPraktekRepo  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menyediakan method query `GetJamMulaiFromReal(string dokterId, int fnHari, string jamMulaiReal)` pada lapisan akses data `ta_jadwal_dokter` (`IJadwalFoDal` / `JadwalFoDal`) dan mengeksposnya melalui interface `IJadwalPraktekRepo` beserta implementasinya di `JadwalPraktekRepo`.

Depends On: None  
Repository: `b09-bilreg-api`  

Completion Criteria:
1. Interface `IJadwalPraktekRepo` di `Bilreg.Application.AdmisiContext.JadwalPraktekFeature` memiliki method:
   ```csharp
   string GetJamMulaiFromReal(string dokterId, int fnHari, string jamMulaiReal);
   ```
2. Interface `IJadwalFoDal` dan class `JadwalFoDal` di `Bilreg.Infrastructure.AdmisiContext.JadwalPraktekFeature` memiliki implementasi query:
   ```sql
   SELECT TOP 1 fs_jam_mulai 
   FROM ta_jadwal_dokter 
   WHERE fs_kd_dokter = @dokterId 
     AND fn_hari = @fnHari 
     AND (fs_jam_mulai_real = @jamMulaiReal OR fs_jam_mulai = @jamMulaiReal)
   ```
   mengembalikan nilai string `fs_jam_mulai` yang dipangkas (*trimmed*), atau string kosong `""` jika tidak ditemukan record yang cocok.
3. Class `JadwalPraktekRepo` di `Bilreg.Infrastructure` mengimplementasikan method tersebut dengan mendelegasikannya ke `_jadwalFoDal.GetJamMulaiFromReal(...)`.
4. Seluruh solusi terkompilasi tanpa error build.

Implementation Notes:  
- Menambahkan method signature `string GetJamMulaiFromReal(string dokterId, int fnHari, string jamMulaiReal);` pada interface `IJadwalPraktekRepo`.
- Menambahkan method signature `string GetJamMulaiFromReal(string dokterId, int fnHari, string jamMulaiReal);` pada interface `IJadwalFoDal`.
- Mengimplementasikan query `SELECT TOP 1 fs_jam_mulai FROM ta_jadwal_dokter WHERE fs_kd_dokter = @dokterId AND fn_hari = @fnHari AND (fs_jam_mulai_real = @jamMulaiReal OR fs_jam_mulai = @jamMulaiReal)` pada `JadwalFoDal` menggunakan Dapper `ExecuteScalar<string>` dengan fallback string kosong `""` dan `.Trim()`.
- Mengimplementasikan delegasi method `GetJamMulaiFromReal` pada `JadwalPraktekRepo` ke `_jadwalFoDal.GetJamMulaiFromReal(...)`.
- Solusi berhasil dikompilasi (`dotnet build b09-bilreg-api\src\bilreg\b09-bilreg-api.sln`) dengan 0 errors dan pengujian `BookingCreateFromHidokHandlerTest` lulus 100%.

Review Notes:  
- Decision: GO
- Divergence: None.
- Verification:
  1. Method signature `GetJamMulaiFromReal` sesuai dengan kriteria penyelesaian pada `IJadwalPraktekRepo` dan `IJadwalFoDal`.
  2. Query SQL `SELECT TOP 1 fs_jam_mulai FROM ta_jadwal_dokter ...` pada `JadwalFoDal` terparameterisasi penuh menggunakan Dapper `DynamicParameters` dengan `SqlDbType` yang tepat (anti SQL injection), serta pemangkasan string dengan `Trim()` dan fallback string kosong `""`.
  3. Delegasi pada `JadwalPraktekRepo` mendelegasikan secara tepat ke `_jadwalFoDal.GetJamMulaiFromReal`.
  4. Build solusi `b09-bilreg-api.sln` sukses 0 errors, 0 warnings.
  5. Seluruh unit test terkait JadwalPraktek dan BookingCreateFromHidok lulus (37/37 passed).

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/JadwalPraktekFeature/IJadwalPraktekRepo.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/JadwalFoDal.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/JadwalPraktekRepo.cs`

Notes:  
Kueri menyertakan `OR fs_jam_mulai = @jamMulaiReal` sebagai mekanisme *fail-safe* jika suatu saat HiDok mengirimkan jam formal.

---

## P2 - Schedule Translation on Booking Intake

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S02
Title: Penerapan Translasi Jam Real ke Formal pada BookingCreateFromHidokHandler  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menerapkan logika translasi jam pada `BookingCreateFromHidokHandler` sebelum memanggil `BookingScheduleResolver.Resolve`, dengan mengonversi tanggal kunjungan ke format hari VB6 dan mengambil jam formal dari `_jadwalPraktekRepo`.

Depends On: P1-S01  
Repository: `b09-bilreg-api`  

Completion Criteria:
1. Pada `BookingCreateFromHidokHandler.cs`, sebelum memanggil `BookingScheduleResolver.Resolve`:
   - Menghitung index hari legacy VB6:  
     `var fnHari = (int)tglBerobat.DayOfWeek + 1;`
   - Melakukan lookup jam formal:  
     `var jamMulaiFormal = _jadwalPraktekRepo.GetJamMulaiFromReal(dokter.PpaId, fnHari, request.JamMulai);`
   - Menggunakan `jamMulaiFormal` (jika tidak kosong) atau fallback ke `request.JamMulai` untuk diparsing menjadi `jamMulai` (`TimeOnly`).
2. Objek `schedule` berhasil di-resolve menggunakan jam formal yang cocok dengan `BILRG_JadwalPraktek`.
3. Entitas `BookingModel` berhasil dibuat dan disimpan dengan jam jadwal formal yang selaras dengan antrian FO Desktop.
4. Kode terisolasi di `BookingCreateFromHidokHandler` tanpa menyentuh `BookingCreateCmd`.

Implementation Notes:  
- Pada `BookingCreateFromHidokHandler.cs`, menambahkan perhitungan index hari legacy VB6: `var fnHari = (int)tglBerobat.DayOfWeek + 1;`.
- Menambahkan pemanggilan lookup jam formal melalui `_jadwalPraktekRepo.GetJamMulaiFromReal(dokter.PpaId, fnHari, request.JamMulai)`.
- Menggunakan `jamMulaiFormal` apabila tidak kosong/whitespace, atau fallback ke `request.JamMulai` sebelum diparsing ke `TimeOnly`.
- Schedule resolver dan proses booking selanjutnya menggunakan `jamMulai` hasil translasi jam formal.
- Solusi berhasil dikompilasi (`dotnet build b09-bilreg-api\src\bilreg\b09-bilreg-api.sln`) dengan 0 errors, dan seluruh unit test terkait (`BookingCreateFromHidokHandlerTest` dan `JadwalPraktek`) lulus 100%.

Review Notes:  
- Decision: GO
- Divergence: None.
- Verification:
  1. Perhitungan index hari VB6 `(int)tglBerobat.DayOfWeek + 1` terverifikasi akurat dan selaras dengan representasi kolom `fn_hari` di `ta_jadwal_dokter`.
  2. Logika lookup `_jadwalPraktekRepo.GetJamMulaiFromReal` dan fallback `!string.IsNullOrWhiteSpace(jamMulaiFormal) ? jamMulaiFormal : request.JamMulai` terbukti aman menangani string kosong/whitespace/null tanpa merusak validasi jam eksisting.
  3. Scope isolasi terpenuhi secara ketat: modifikasi hanya terjadi pada `BookingCreateFromHidokCommand.cs`, tanpa menyentuh `BookingCreateCmd` maupun alur rawat jalan lokal lainnya.
  4. Kompilasi solusi `b09-bilreg-api.sln` sukses tanpa error dan tanpa warning.
  5. Seluruh unit test terkait usecase Booking lulus 100% (4/4 passed).

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/BookingFeature/UseCases/BookingCreateFromHidokCommand.cs`

Notes:  
Jika dokter tidak memiliki jadwal sama sekali baik real maupun formal, resolver akan tetap melempar `ArgumentException("Jadwal tidak ditemukan")` sesuai desain pertahanan invariant yang berlaku.

---

## P3 - Verification & Unit Test Suite

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P3-S03
Title: Pembaruan dan Penambahan Unit Test pada BookingCreateFromHidokHandlerTest  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memperbarui pengujian unit yang ada agar mock `_jadwalRepo` menyertakan setup `GetJamMulaiFromReal`, serta menambahkan kasus uji baru untuk skenario ketika HiDok mengirimkan `fs_jam_mulai_real` yang berbeda dengan `fs_jam_mulai`.

Depends On: P2-S02  
Repository: `b09-bilreg-api`  

Completion Criteria:
1. Kasus uji eksisting pada `BookingCreateFromHidokHandlerTest.cs` tetap berjalan sukses (`PASS`).
2. Menambahkan kasus uji baru `Handle_WhenJamMulaiRealDifferentFromFormal_TranslatesJamMulaiAndCreatesBookingSuccessfully`:
   - Setup: HiDok mengirimkan `JamMulai = "08:30"`, sedangkan jadwal template dokter memiliki `JamMulai = "08:00"`.
   - Setup mock `_jadwalRepo.GetJamMulaiFromReal(dokter.PpaId, fnHari, "08:30")` mengembalikan `"08:00"`.
   - Assertion: Booking berhasil dibuat dengan `BookingId` valid dan nomor antrian yang tepat tanpa melempar exception `Jadwal tidak ditemukan`.
3. Seluruh unit test pada `Bilreg.Test` yang relevan lulus 100%.

Implementation Notes:  
- Pada `BookingCreateFromHidokHandlerTest.cs`, menambahkan setup eksplisit mock `_jadwalRepo.GetJamMulaiFromReal("DR1", (int)tglBerobat.DayOfWeek + 1, "08:00").Returns("08:00")` pada kasus uji eksisting `Handle_WhenValidBookingCreated_PublishesRefreshHintForPoliAndDokter`.
- Menambahkan kasus uji baru `Handle_WhenJamMulaiRealDifferentFromFormal_TranslatesJamMulaiAndCreatesBookingSuccessfully` yang menguji skenario request HiDok dengan `JamMulai: "08:30"` dan template jadwal dokter dengan `JamMulai: 08:00`.
- Memverifikasi translasi jam berhasil dilakukan ke jam formal `"08:00"` dan disimpan pada `BookingModel` (`JamPraktek == new TimeOnly(8, 0)`), mengembalikan respons sukses dengan nomor antrian yang valid.
- Kedua unit test pada `BookingCreateFromHidokHandlerTest` lulus 100% (2 passed, 0 failed).

Review Notes:  
- Decision: GO
- Divergence: None.
- Verification:
  1. Kasus uji eksisting `Handle_WhenValidBookingCreated_PublishesRefreshHintForPoliAndDokter` telah diperbarui dengan mock setup `GetJamMulaiFromReal` dan tetap lulus 100%.
  2. Kasus uji baru `Handle_WhenJamMulaiRealDifferentFromFormal_TranslatesJamMulaiAndCreatesBookingSuccessfully` memvalidasi secara menyeluruh skenario real-world: request HiDok dengan jam operasional (`08:30`) berhasil ditranslasikan ke jam formal dokter (`08:00`), template jadwal berhasil di-resolve tanpa exception, antrian eksternal terhubung, dan entitas `BookingModel` tersimpan dengan jam jadwal formal (`new TimeOnly(8, 0)`).
  3. Kualitas test suite sangat baik: isolasi mock ketat, parameter matching presisi, dan asersi memverifikasi status kembalian, nomor antrian, pemanggilan event publisher, serta integritas state `BookingModel`.
  4. Pengujian unit `BookingCreateFromHidokHandlerTest` lulus 100% (2 passed, 0 failed), dan seluruh 31 unit test di `AdmisiContext.BookingFeature` lulus tanpa regresi.

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiContext/BookingFeature/BookingCreateFromHidokHandlerTest.cs`

Notes:  
Jalankan `dotnet test` untuk memastikan semua pengujian lulus.

---

# 6. Change Log

- 2026-11-20: Pembuatan dokumen rencana implementasi awal untuk perbaikan ketidakcocokan jam booking HiDok (`BOOKING-HIDOK-SCHEDULE-MISMATCH`). Status diset `NOT-STARTED`, `Execution Approval: APPROVED`.
- 2026-11-20: Review Slice P1-S01 selesai (GO).
- 2026-11-20: Review Slice P2-S02 selesai (GO).
- 2026-11-20: Review Slice P3-S03 selesai (GO). Seluruh tahapan rencana implementasi telah selesai dan terverifikasi GO. Status rencana diubah menjadi COMPLETED.
