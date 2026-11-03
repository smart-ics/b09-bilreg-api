---
Title: Implementation Plan - Eliminasi STRING_SPLIT untuk Kompatibilitas Microsoft SQL Server Versi di Bawah 2016
Code: PLAN-BILREG-SQLSERVER-SPLITSTRING-001
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-03T15:00:00+07:00
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Memperbaiki ketergantungan pada fungsi bawaan T-SQL `STRING_SPLIT` pada kueri-kueri layer infrastruktur `b09-bilreg-api` agar seluruh modul sistem (antrian loket admisi, pencarian riwayat registrasi pasien, dan pemuatan batch perjalanan rawat inap) dapat berjalan dengan sukses, andal, dan optimal pada Microsoft SQL Server versi di bawah 2016 (seperti SQL Server 2008, 2008 R2, 2012, dan 2014) maupun versi modern tanpa menuntut perubahan skema basis data (*zero DDL*).

Referenced artifacts:

- ISSUE: [BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-BUG-ISSUE.md) (`ISSUE-BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-001`)
- BUG-INVESTIGATION: [BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-BUG-INVESTIGATION.md)
- FEASIBILITY-ASSESSMENT: Terintegrasi dalam BUG-INVESTIGATION (Status: CLOSED / APPROVED)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED

No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved feasibility decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

Perbaikan ini terisolasi sepenuhnya di dalam satu repositori backend: **`b09-bilreg-api`** (tidak menyentuh frontend `c012_myhospital_web` maupun service backend lainnya).

Komponen yang dimodifikasi:
1. **`AdmissionQueueOperationalProjection.cs`**:
   - Menghapus klausa `STRING_SPLIT(@ServicePointIds, ',')` pada `pageSql` dan `countSql`.
   - Menggunakan parameter list Dapper langsung `@ServicePointIds` yang dipetakan dari koleksi `filter.ServicePointIds` tanpa konversi `string.Join`.
2. **`RegistrationHistoryReader.cs`**:
   - Menghapus klausa `STRING_SPLIT(@patientIdsCsv, ',')` dan `STRING_SPLIT(@registrationIdsCsv, ',')`.
   - Menggantinya dengan parsing in-memory XML (`CAST('<x>' + REPLACE(...) + '</x>' AS XML)` dan `.nodes('/x')`).
   - Mempertahankan parameter tunggal CSV `@patientIdsCsv` dan `@registrationIdsCsv` agar aman dari batas 2.100 parameter SQL Server saat menangani ribuan ID.
3. **`JourneyDal.Batch.cs`**:
   - Menghapus klausa `STRING_SPLIT(@RegIdCsv, ',')` pada `BuildRegExtrasSelectSql()`.
   - Mengubah sumber data kueri menjadi `FROM ta_registrasi reg` langsung dengan filter `reg.fs_kd_reg IN @RegIds`, serta filter subquery riwayat dokter `h.fs_kd_reg IN @RegIds`.
   - Membersihkan parameter `@RegIdCsv` dari `BuildIdParams()`.
4. **Verifikasi Otomatis**:
   - Menjalankan unit test `RegistrationHistoryReaderTest.cs` (termasuk skenario 2.600 ID).
   - Menjalankan unit test antrian dan perjalananan pasien ranap di `Bilreg.Test`.
   - Memastikan guardrail `git grep -in "STRING_SPLIT" src/` menghasilkan 0 kemunculan.

---

# 3. Dependencies

- **External Dependencies**:
  - Runtime .NET 8 SDK.
  - Konektivitas basis data pengujian / CI.
- **Slice Dependencies**:
  - `P1-S01`: Tidak memiliki ketergantungan (independen).
  - `P1-S02`: Tidak memiliki ketergantungan (independen).
  - `P1-S03`: Tidak memiliki ketergantungan (independen).
  - `P2-S04`: Bergantung pada penyelesaian `P1-S01`, `P1-S02`, dan `P1-S03`.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---------|---------|---------|---------|
| P1 | IMPLEMENTED | GO | 3/3 |
| P2 | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Backward-Compatible Query Migrations (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01

Title: Migrasi Kueri Antrian Loket ke Dapper Parameter List Native

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menghapus penggunaan `STRING_SPLIT` pada kueri antrian loket di `AdmissionQueueOperationalProjection.cs` dan memanfaatkan kemampuan parameter list native Dapper (`IN @ServicePointIds`).

Depends On: None  

Repository: b09-bilreg-api  

Completion Criteria:  
1. Berkas [AdmissionQueueOperationalProjection.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AdmissionQueueOperationalProjection.cs) dimodifikasi:
   - Baris 41-42 (`pageSql`) dan baris 57-58 (`countSql`) diubah dari `IN (SELECT value FROM STRING_SPLIT(@ServicePointIds, ','))` menjadi `IN @ServicePointIds`.
   - Penyiapan parameter `args` memetakan `ServicePointIds = filter.ServicePointIds?.ToArray() ?? ["\0"]` (atau list kosong ditangani secara aman tanpa konversi CSV).
2. Tidak ada lagi kata kunci `STRING_SPLIT` di dalam berkas ini.
3. Seluruh unit test terkait antrian di `Bilreg.Test` lulus via perintah:
   `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~AdmissionQueue"`

Implementation Notes:
- Mengganti klausa filter `STRING_SPLIT` pada `pageSql` dan `countSql` menjadi `AND (@HasServicePointIds = 0 OR q.ServicePointCode IN @ServicePointIds)`.
- Menggunakan parameter evaluasi eksplisit `@HasServicePointIds` untuk menghindari kegagalan sintaks T-SQL pada row-value constructor Dapper list parameter saat diperluas.
- Memetakan `args.ServicePointIds` ke array native dari `filter.ServicePointIds` dengan fallback `["\0"]` ketika null/kosong untuk keamanan ekspansi parameter Dapper.
- Verifikasi berhasil: kata kunci `STRING_SPLIT` nihil pada berkas target, build `Bilreg.Infrastructure` sukses (0 error), dan seluruh unit test non-integration `Bilreg.Test` lulus.

Changed Files:
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AdmissionQueueOperationalProjection.cs`

---

### P1-S02

Title: Migrasi Kueri Pencarian Riwayat Registrasi ke Inline XML Shredding

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menghapus penggunaan `STRING_SPLIT` pada `RegistrationHistoryReader.cs` dan menggantinya dengan in-memory XML shredding (`.nodes('/x')`), dengan mempertahankan signature parameter tunggal CSV guna menjamin kepatuhan batas 2.100 parameter SQL Server saat menangani pencarian masal (2.600+ ID).

Depends On: None  

Repository: b09-bilreg-api  

Completion Criteria:  
1. Berkas [RegistrationHistoryReader.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegistrationHistoryReader.cs) dimodifikasi:
   - Kueri `FindByPatientIds` (baris 49-53) dan `FindByRegistrationIds` (baris 77-81) mengganti `STRING_SPLIT` dengan blok subquery inline XML:
     ```sql
     SELECT LTRIM(RTRIM(n.value('.', 'VARCHAR(50)')))
     FROM (SELECT CAST('<x>' + REPLACE(@patientIdsCsv, ',', '</x><x>') + '</x>' AS XML) AS t) x
     CROSS APPLY x.t.nodes('/x') AS r(n)
     WHERE n.value('.', 'VARCHAR(50)') <> ''
     ```
   - Metode `CreatePatientIdParameters` dan `CreateRegistrationIdParameters` tetap menghasilkan 1 parameter string CSV `@patientIdsCsv` / `@registrationIdsCsv`.
2. Unit test di [RegistrationHistoryReaderTest.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg\Bilreg.Test\AdmisiContext\RegFeature\RegistrationHistoryReaderTest.cs) lulus 100%:
   `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RegistrationHistoryReaderTest"`
3. Unit test alur pencarian `AdmisiRajalPatientContextQueryTest.cs` lulus tanpa regresi.

Implementation Notes:
- Mengganti penggunaan `STRING_SPLIT` pada kueri `FindByPatientIds` dan `FindByRegistrationIds` di `RegistrationHistoryReader.cs` dengan inline XML shredding (`.nodes('/x')`).
- Mempertahankan signature parameter string tunggal `@patientIdsCsv` dan `@registrationIdsCsv` di `CreatePatientIdParameters` dan `CreateRegistrationIdParameters` guna menjamin kepatuhan batas 2.100 parameter SQL Server saat pencarian masal (2.600+ ID).
- Verifikasi berhasil: kata kunci `STRING_SPLIT` sepenuhnya tereliminasi dari `RegistrationHistoryReader.cs`, unit test `RegistrationHistoryReaderTest` lulus 100% (2/2 lulus), dan pengujian alur pencarian `AdmisiRajalPatientContextQueryTest` lulus tanpa regresi (29/29 lulus).

Changed Files:
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegistrationHistoryReader.cs`

---

### P1-S03

Title: Migrasi Kueri Batch Perjalanan Pasien Ranap ke Dapper Parameter List Native

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menghapus penggunaan `STRING_SPLIT` pada `BuildRegExtrasSelectSql()` di `JourneyDal.Batch.cs` dan menggunakan filter langsung `reg.fs_kd_reg IN @RegIds` serta `h.fs_kd_reg IN @RegIds`, serta membersihkan parameter `@RegIdCsv` yang usang.

Depends On: None  

Repository: b09-bilreg-api  

Completion Criteria:  
1. Berkas [JourneyDal.Batch.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs) dimodifikasi:
   - `BuildRegExtrasSelectSql()` (baris 274-298) diubah sehingga langsung melakukan `FROM ta_registrasi reg` difilter dengan `reg.fs_kd_reg IN @RegIds` dan subquery riwayat dokter difilter dengan `h.fs_kd_reg IN @RegIds`.
   - `BuildIdParams()` (baris 208-216) menghapus properti `RegIdCsv`.
2. Tidak ada lagi kata kunci `STRING_SPLIT` di dalam berkas ini.
3. Seluruh unit test ranap journey di `Bilreg.Test` lulus via perintah:
   `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~Journey"`

Implementation Notes:
- Menghapus pembentukan parameter `@RegIdCsv` pada `BuildIdParams()` di `JourneyDal.Batch.cs`.
- Memperbarui `BuildRegExtrasSelectSql()` untuk mengambil data registrasi langsung via `FROM ta_registrasi reg` dengan klausa filter `reg.fs_kd_reg IN @RegIds` dan menyelaraskan filter subquery riwayat dokter menjadi `h.fs_kd_reg IN @RegIds`.
- Mempertahankan proteksi penjaga `@HasRegIds = 1` agar konsisten dengan `BuildWaitingListSelectSql()`.
- Verifikasi berhasil: kata kunci `STRING_SPLIT` dan `RegIdCsv` sepenuhnya tereliminasi dari `JourneyDal.Batch.cs` dan repositori (0 temuan), kompilasi `b09-bilreg-api.sln` sukses 0 error, dan seluruh unit test Ranap Journey lulus (80/80 lulus).

Changed Files:
- `src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs`

---

## P2 - Verification & Regression Guardrails (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S04

Title: Verifikasi Kompilasi Menyeluruh, Unit Test Suite, dan Git Grep Guardrail

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memvalidasi secara menyeluruh bahwa tidak ada lagi sintaks `STRING_SPLIT` yang tertinggal di seluruh direktori `src/` pada repositori `b09-bilreg-api`, dan memastikan seluruh suite pengujian otomatis berjalan tanpa kegagalan.

Depends On: P1-S01, P1-S02, P1-S03  

Repository: b09-bilreg-api  

Completion Criteria:  
1. Eksekusi `git grep -in "STRING_SPLIT" src/` pada `b09-bilreg-api` menghasilkan **0 baris** (bersih total).
2. Eksekusi kompilasi solusi:
   `dotnet build src/bilreg/Bilreg.sln` sukses dengan 0 error.
3. Seluruh unit test di `Bilreg.Test` lulus 100%:
   `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj`
4. Tidak ada perubahan yang keluar dari cakupan (hanya menyentuh 3 file infrastruktur yang ditargetkan).

Implementation Notes:
- Verifikasi guardrail `git grep -in "STRING_SPLIT" src/` berhasil dengan 0 temuan (bersih total di seluruh direktori `src/`).
- Kompilasi solusi `dotnet build src/bilreg/b09-bilreg-api.sln` berhasil dengan 0 error (95 warning bawaan non-blocking).
- Eksekusi unit test suite terkait migrasi dan alur admisi:
  - `RegistrationHistoryReaderTest`: 2/2 lulus 100% (termasuk validasi inline XML shredding dengan 2.600+ ID masal).
  - `AdmisiRajalPatientContextQueryTest`: 29/29 lulus 100%.
  - Ranap `Journey` tests: 80/80 lulus 100%.
  - Total unit test yang lulus di `Bilreg.Test`: 3.798 test.
- Verifikasi cakupan perubahan (`git status`): hanya memodifikasi tepat 3 berkas layer infrastruktur yang ditargetkan (`AdmissionQueueOperationalProjection.cs`, `RegistrationHistoryReader.cs`, dan `JourneyDal.Batch.cs`).

Changed Files:
None (slice verifikasi; tidak ada berkas kode sumber baru yang dimodifikasi).

---

# 6. Change Log

- 2026-11-03: Rencana implementasi versi 1.0 difinalisasi oleh Architect dengan status `Execution Approval: APPROVED`. Pendekatan hibrida (Dapper parameter list untuk volume kecil dan inline XML untuk volume masal) diadopsi tanpa memerlukan modifikasi skema basis data di sisi klien.
