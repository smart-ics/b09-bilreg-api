---
Title: Order Pemeriksaan Laboratorium Backend Implementation Plan
Code: OC-LAB-ORDER-LAB
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-15
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan realisasi teknis backend untuk kapabilitas **Order Pemeriksaan Laboratorium** pada repositori `b09-bilreg-api` sesuai spesifikasi target arsitektur yang telah disahkan pada [OC-LAB-ORDER-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ARCHITECTURE.md).

Tujuan operasional backend:
1. Memperkaya agregat inti `LabOrderModel` dan item `LabOrderItemModel` dengan konteks dokter perujuk (`DokterPerujukId`, `DokterPerujukName`), layanan perujuk (`LayananPerujukId`, `LayananPerujukName`), catatan klinis terpadu (`ClinicalNote`), dan tingkat urgensi pemeriksaan (`Urgency`: `0 = Rutin`, `1 = Cito`).
2. Menerapkan skrip migrasi database DDL non-breaking `BILRG_LabOrder_M11_Alter.sql` lengkap dengan default constraint `DEFAULT(0)` dan index performa `IX_BILRG_LabOrder_Urgency`.
3. Menerapkan *Dual-Path Intake Validation*:
   - Jalur EMR internal: Memvalidasi keaktifan episode kunjungan ke `ADM-REG` (`_regRepo`) dan keaktifan dokter perujuk ke `ORG-PPA` (`_ppaRepo`).
   - Jalur Pasien Eksternal: Memvalidasi keaktifan registrasi ke `LAB-EXTERNAL` (`_labExternalRepo`), auto-populasi perujuk luar dari `ReferralInfo`, serta bypass total validasi master internal RS guna menjamin prinsip *Master Isolation*.
4. Menerapkan protokol konfirmasi duplikasi 24 jam untuk mendeteksi pesanan tes berulang pada pasien yang sama dalam 24 jam terakhir (Exception `EX-05`).
5. Memperketat alur pembatalan order (`LabOrderCancelCmd`): menolak pembatalan jika spesimen telah diambil (`>= Collected`), dan menyinkronkan pembatalan beban tagihan ke Tata Rekening jika order telah berstatus `Charged` dan tagihan belum final/lunas di kasir.
6. Memperkaya DTO `LabOrderWorklistView` dan query DAL dengan penanda darurat `IsCito`, nama dokter perujuk, nama layanan perujuk, dan alasan penundaan (`DeferredReason`) agar order Cito dapat didahulukan dalam antrean tabung dan sampling.
7. Memperbarui Web API Controller `LabOrderController.cs` dengan kontrak respons terstandar `JSendOk`.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- ARCHITECTURE: [OC-LAB-ORDER-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ARCHITECTURE.md)
- FEASIBILITY-ASSESSMENT: [OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- ISSUE: [OC-LAB-ORDER-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ISSUE.md) (`ISSUE-LAB-ORDER-LAB-001`)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Target repository:
- `b09-bilreg-api` (Seluruh irisan implementasi / slice pada rencana ini menargetkan repositori backend BILREG API).

In Scope:
1. **Database DDL**: Pembuatan skrip `BILRG_LabOrder_M11_Alter.sql` dan pendaftaran pada `Bilreg.SqlDb.sqlproj`.
2. **Domain Layer**:
   - Enumerasi `LabOrderUrgencyEnum` (`Rutin = 0`, `Cito = 1`).
   - Perluasan agregat `LabOrderModel` dan item `LabOrderItemModel` (`DokterPerujuk`, `LayananPerujuk`, `ClinicalNote`, `Urgency`, `IsCito`).
   - Invariant guard pembatalan spesimen (`LabOrderStatus < Collected`).
   - Pembaruan unit test domain di `Bilreg.Test/LabContext/LabOrderFeature`.
3. **Infrastructure & Persistence Layer**:
   - Pembaruan DTO `LabOrderDto` dan `LabOrderItemDto`.
   - Pembaruan query Dapper `LabOrderDal`, `LabOrderItemDal`, dan `LabOrderRepo`.
   - Penambahan kueri deteksi tes duplikat 24 jam pada `LabOrderDal`.
   - Pembaruan `LabOrderWorklistDal` dengan kolom Cito, perujuk, dan filter Cito.
4. **Application Layer (Use Cases & Handlers)**:
   - Command `LabOrderCreateFromEmrCmd` & handler dengan validasi `ADM-REG`, `ORG-PPA`, dan cek duplikasi 24 jam.
   - Command `LabOrderCreateExternalPatientCmd` & handler dengan validasi `LAB-EXTERNAL`, auto-populasi `ReferralInfo`, dan cek duplikasi 24 jam.
   - Command `LabOrderCancelCmd` & handler dengan koordinasi void tagihan ke Tata Rekening dan guard status spesimen.
   - Query `LabOrderGetQuery` & handler dengan response terperinci.
   - Query `LabOrderWorklistQuery` & handler dengan filter dan view terfilter Cito.
5. **Web API Layer**:
   - Pembaruan controller `LabOrderController.cs` dengan endpoint RESTful dan envelope `JSendOk`.
6. **Automated Testing**:
   - Unit & integration tests komprehensif di `Bilreg.Test/LabContext/LabOrderFeature` mencakup seluruh invariant, dual-path validasi, exception EX-01 s/d EX-05, dan kueri worklist.

Out of Scope (Arsitektur §3 Excluded):
1. Prosedur flebotomi fisik, penusukan vena, dan pencetakan barcode tabung (`LAB-COLLECT` / `OC-LAB-SAMPLE-COLLECTION`).
2. Perekaman nilai analitik hasil tes dan verifikasi dokter spesialis patologi klinik (`LAB-RESULT` / `OC-LAB-HASIL-LAB`).
3. Pembuatan master pasien permanen di `PAS-DATSOS` atau kunjungan umum di `ADM-REG` untuk pasien luar.
4. Modifikasi modul pembayaran kasir fisik (`TRK-KASIR`).

---

# 3. Dependencies

External dependencies:
- Framework .NET 8 / C# 12.
- Micro-ORM Dapper untuk eksekusi parameterized SQL query.
- MediatR untuk pipeline CQRS (Command & Query handlers).
- Nuna library: `TransHelper` (manajemen transaction scope), `NunaId` (generator ID), `ISequencer` (sequencer nomor urut).
- Ardalis.GuardClauses untuk validasi input fail-fast.
- xUnit, Moq, dan FluentAssertions pada `Bilreg.Test`.

Slice dependency rules:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID yang valid dalam rencana ini.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED`.
- Kepuasan dependensi tidak mensyaratkan status review `GO`.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Foundation (Database DDL & Domain Model) | NOT-STARTED | GO | 0/2 |
| P2 - Infrastructure & Data Access Layer | NOT-STARTED | GO | 0/1 |
| P3 - Core Application Intake Handlers | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P4 - Cancellation & Query Enhancement | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P5 - Web API Endpoints & Contract Verification | NOT-STARTED | NOT-REVIEWED | 0/1 |

---

# 5. Phases

## P1 - Foundation (Database DDL & Domain Model)

Implementation Status: NOT-STARTED
Review Status: GO

### P1-S01
Title: Database DDL Migration Script for Clinical Context & Urgency
Implementation Status: IMPLEMENTED
Review Status: GO
Objective: Menyiapkan skrip DDL migrasi SQL Server untuk menambahkan kolom `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, dan `Urgency` pada tabel `BILRG_LabOrder`, menambahkan kolom `Urgency` pada `BILRG_LabOrderItem`, menambahkan index `IX_BILRG_LabOrder_Urgency`, serta mendaftarkannya pada proyek `Bilreg.SqlDb.sqlproj`.
Depends On: None
Repository: `b09-bilreg-api`
Completion Criteria:
1. File `BILRG_LabOrder_M11_Alter.sql` dibuat di folder `Bilreg.SqlDb/LabContext/LabOrderFeature/`.
2. Skrip DDL mendefinisikan penambahan kolom dengan `NOT NULL CONSTRAINT DF_... DEFAULT('')` untuk string dan `DEFAULT(0)` untuk `Urgency`.
3. Skrip DDL mendefinisikan index non-clustered `IX_BILRG_LabOrder_Urgency` dan `IX_BILRG_LabOrder_Perujuk`.
4. File skrip terdaftar pada file proyek `Bilreg.SqlDb/Bilreg.SqlDb.sqlproj`.
Notes: Bersifat idempotent (`IF NOT EXISTS`).
Implementation Notes:
- Membuat skrip DDL idempotent `BILRG_LabOrder_M11_Alter.sql` di `src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/`.
- Menambahkan kolom `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, dan `Urgency` dengan default constraint yang sesuai pada `BILRG_LabOrder`.
- Menambahkan kolom `Urgency` dengan `DEFAULT(0)` pada `BILRG_LabOrderItem`.
- Menambahkan non-clustered index `IX_BILRG_LabOrder_Urgency` dan filtered non-clustered index `IX_BILRG_LabOrder_Perujuk`.
- Memperbarui skema dasar `BILRG_LabOrder.sql` dan `BILRG_LabOrderItem.sql` agar konsisten dengan skrip alter.
- Mendaftarkan `BILRG_LabOrder_M11_Alter.sql` pada elemen `<Build Include="...">` di `src/bilreg/Bilreg.SqlDb/Bilreg.SqlDb.sqlproj`.
- Verifikasi build `dotnet build src/bilreg/b09-bilreg-api.sln` berhasil bersih dengan 0 warning dan 0 error.
Changed Files:
- `src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrder_M11_Alter.sql`
- `src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrder.sql`
- `src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrderItem.sql`
- `src/bilreg/Bilreg.SqlDb/Bilreg.SqlDb.sqlproj`

---

### P1-S02
Title: Domain Model Enrichment & Urgency Enum Definition
Implementation Status: IMPLEMENTED
Review Status: GO
Objective: Membuat enumerasi `LabOrderUrgencyEnum` (`Rutin = 0`, `Cito = 1`), memperkaya agregat `LabOrderModel` dan item `LabOrderItemModel` dengan atribut perujuk, catatan klinis, dan urgensi Cito, memperbarui factory method, menambahkan guard pembatalan pasca-spesimen (`>= Collected`), serta memperbarui domain test suite.
Depends On: P1-S01
Repository: `b09-bilreg-api`
Completion Criteria:
1. File `LabOrderUrgencyEnum.cs` dibuat di `Bilreg.Domain/LabContext/LabOrderFeature/` dengan nilai `Rutin = 0` dan `Cito = 1`.
2. `LabOrderItemModel` memuat properti `Urgency` dan getter `IsCito => Urgency == LabOrderUrgencyEnum.Cito`.
3. `LabOrderModel` memuat properti `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, `Urgency`, dan getter `IsCito`.
4. Factory method `CreateFromEmr` dan `CreateExternal` menerima data perujuk, catatan klinis, dan urgensi dengan aturan invariant konsistensi Cito.
5. Metode `Cancel(...)` melempar `InvalidOperationException` jika status order sudah `Collected` ke atas.
6. Seluruh unit test domain di `Bilreg.Test/LabContext/LabOrderFeature/LabOrderModelTest.cs` lulus pengujian.
Notes: Menjaga factory method yang ada tetap kompatibel via default value atau overloading.
Implementation Notes:
- Membuat enum `LabOrderUrgencyEnum` (`Rutin = 0`, `Cito = 1`) di `Bilreg.Domain/LabContext/LabOrderFeature/LabOrderUrgencyEnum.cs`.
- Memperkaya `LabOrderItemModel` dengan properti `Urgency` (default `Rutin`) dan properti turunan `IsCito`.
- Memperkaya agregat root `LabOrderModel` dengan `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, `Urgency`, dan getter `IsCito`.
- Memperbarui factory method `CreateFromEmr` dan `CreateExternal` dengan parameter opsional ber-default value untuk menjaga *backward compatibility*, menerapkan resolusi Cito hierarkis (Architecture AD-03): order otomatis menjadi Cito jika ada item Cito atau header Cito, dan mempropagasi status Cito ke item saat header diorder Cito.
- Menambahkan invariant guard pembatalan spesimen pada `Cancel(...)`: menolak pembatalan dan melempar `InvalidOperationException` bertanda status jika order sudah `Collected` ke atas (`Collected`, `Recorded`, `Verified`, `Released`).
- Memperbaiki penanganan default timestamp pada `Cancel(...)` dan `Terminate(...)`.
- Menambahkan 9 test case baru di `Bilreg.Test/LabContext/LabOrderFeature/LabOrderModelTest.cs` mencakup pengujian konteks perujuk, catatan klinis, hierarki Cito order & item, dan guard pembatalan pasca-spesimen. Seluruh 62 unit test di `LabOrderModelTest` lulus 100%.
- Solusi berhasil dibangun bersih tanpa error (`dotnet build src/bilreg/b09-bilreg-api.sln`).
Changed Files:
- `src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderUrgencyEnum.cs`
- `src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemModel.cs`
- `src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs`
- `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderModelTest.cs`

---

## P2 - Infrastructure & Data Access Layer

Implementation Status: NOT-STARTED
Review Status: GO

### P2-S03
Title: DTO Mapping, DAL Query Updates, and 24-Hour Duplicate Detection
Implementation Status: IMPLEMENTED
Review Status: GO
Objective: Memperbarui DTO `LabOrderDto` dan `LabOrderItemDto`, memperbarui kueri Dapper pada `LabOrderDal`, `LabOrderItemDal`, dan `LabOrderRepo`, menambahkan kueri deteksi order duplikat 24 jam, serta memperbarui `LabOrderWorklistDal` untuk mendukung penyajian dan filter prioritas Cito.
Depends On: P1-S02
Repository: `b09-bilreg-api`
Completion Criteria:
1. `LabOrderDto` dan `LabOrderItemDto` memuat kolom baru perujuk, catatan klinis, dan urgensi.
2. `LabOrderDal.Insert` dan `Update` menyertakan kolom-kolom baru pada parameterized statement Dapper.
3. `LabOrderDal` menyediakan method `ListActiveTestsByPatientWithinHours(string patientId, int hours)` untuk mendeteksi order tes aktif dalam 24 jam terakhir.
4. `LabOrderRepo` memetakan secara presisi antara `LabOrderModel` dan `LabOrderDto`.
5. `ILabOrderWorklistDal` dan `LabOrderWorklistDal` memperbarui `LabOrderWorklistView` dengan `IsCito`, `DokterPerujukName`, `LayananPerujukName`, dan `DeferredReason`, serta mendukung parameter `IsCitoOnly` pada `LabOrderWorklistFilter`.
6. Unit test DAL di `Bilreg.Test/LabContext/LabOrderFeature` lulus pengujian.
Notes: Menggunakan Dapper parameter binding yang aman dari SQL injection.
Implementation Notes:
- Updated `LabOrderDto` and `LabOrderItemDto` to include `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, and `Urgency` (with model-to-dto and dto-to-model bidirectional mappings).
- Updated `LabOrderDal`:
  - Enriched `SelectColumns`, `Insert`, `Update`, and `BuildParams` to persist and retrieve the 6 new clinical context & urgency columns.
  - Added `LabOrderActiveTestDto` record and implemented `ListActiveTestsByPatientWithinHours(string patientId, int hours)` on `ILabOrderDal` and `LabOrderDal` for 24-hour duplicate active test detection across non-cancelled lab orders.
- Updated `LabOrderItemDal`:
  - Added `Urgency` column mapping to SqlBulkCopy (`bcp.AddMap("Urgency", "Urgency")`) and `Urgency` projection in `ListData`.
- Updated `LabOrderRepo`:
  - Preserved full mapping between `LabOrderModel` (with `LabOrderUrgencyEnum`) and `LabOrderDto`/`LabOrderItemDto`.
  - Added unit tests in `LabOrderRepoTest` verifying bidirectional mapping of clinical context and urgency fields for both Rutin and Cito.
- Updated `ILabOrderWorklistDal` & `LabOrderWorklistDal`:
  - Added `IsCitoOnly` optional filter to `LabOrderWorklistFilter`.
  - Added `IsCito`, `DokterPerujukName`, `LayananPerujukName`, and `DeferredReason` to `LabOrderWorklistView` and query mapping with `aa.Urgency = 1` conditional filtering.
- Updated `LabOrderWorklistHandlerTest` for the new `LabOrderWorklistView` signature.
- Build verified with 0 errors (`dotnet build src/bilreg/b09-bilreg-api.sln`), and 105 domain, repo, and handler unit tests passing cleanly.
Changed Files:
- `src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/ILabOrderWorklistDal.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabOrderDto.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabOrderItemDto.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabOrderDal.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabOrderItemDal.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabOrderWorklistDal.cs`
- `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderRepoTest.cs`
- `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderWorklistHandlerTest.cs`

---

## P3 - Core Application Intake Handlers

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P3-S04
Title: EMR Intake Command Handler with ADM-REG & ORG-PPA Validation & 24h Duplicate Check
Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED
Objective: Memperbarui command `LabOrderCreateFromEmrCmd` dan `LabOrderCreateFromEmrHandler` untuk menerima data dokter perujuk, layanan perujuk, `ClinicalNote`, dan `Urgency`, melakukan validasi status kunjungan aktif ke `ADM-REG` (`_regRepo`), validasi dokter perujuk ke `ORG-PPA` (`_ppaRepo`), serta mengeksekusi pemeriksaan duplikasi 24 jam dengan opsi konfirmasi klinis (`AllowDuplicateConfirmation`).
Depends On: P2-S03
Repository: `b09-bilreg-api`
Completion Criteria:
1. `LabOrderCreateFromEmrCmd` memuat parameter: `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, `Urgency`, dan `AllowDuplicateConfirmation`.
2. Handler memvalidasi `RegId` ke `_regRepo`: menolak jika registrasi tidak ditemukan atau sudah ditutup/discharged (Exception `EX-01`).
3. Handler memvalidasi `DokterPerujukId` ke `_ppaRepo`: menolak jika dokter tidak aktif (Condition 5.3 #2).
4. Handler mendeteksi tes duplikat 24 jam: jika ada dan `AllowDuplicateConfirmation == false`, melempar exception peringatan duplikasi (`EX-05`).
5. Transaksi berhasil menyimpan data order lengkap dengan perujuk dan urgensi Cito.
6. Unit test di `Bilreg.Test/LabContext/LabOrderFeature/LabOrderCreateFromEmrHandlerTest.cs` mencakup skenario sukses, validasi EX-01, validasi PPA, dan peringatan EX-05.
Notes: Memanfaatkan `TransHelper.NewScope()`.

---

### P3-S05
Title: External Patient Intake Command Handler with Auto-population & Isolation
Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED
Objective: Memperbarui command `LabOrderCreateExternalPatientCmd` dan `LabOrderCreateExternalPatientHandler` untuk menerima data perujuk, `ClinicalNote`, dan `Urgency`, memvalidasi keaktifan `RegExternalId` ke `LAB-EXTERNAL` (`_labExternalRepo`), melakukan auto-populasi data perujuk dan catatan klinis dari `ReferralInfo`, melakukan bypass validasi master internal RS, dan memeriksa duplikasi 24 jam.
Depends On: P2-S03
Repository: `b09-bilreg-api`
Completion Criteria:
1. `LabOrderCreateExternalPatientCmd` memuat parameter: `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, `Urgency`, dan `AllowDuplicateConfirmation`.
2. Handler memvalidasi `RegExternalId` ke `_labExternalRepo`: memastikan entitas pendaftaran eksternal aktif.
3. Jika parameter perujuk kosong, handler secara otomatis mengisi `DokterPerujukName`, `LayananPerujukName`, dan `ClinicalNote` dari `LabExternalModel.ReferralInfo` (atau default APS).
4. Handler tidak melakukan validasi ke `_regRepo` maupun `_ppaRepo` (menjaga isolasi master data RS).
5. Handler melakukan pemeriksaan duplikasi 24 jam.
6. Unit test di `Bilreg.Test/LabContext/LabOrderFeature/LabOrderCreateExternalPatientHandlerTest.cs` lulus pengujian.
Notes: Menjamin interoperabilitas mulus dengan `OC-LAB-REG-EXTERNAL`.

---

## P4 - Cancellation & Query Enhancement

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P4-S06
Title: Cancellation Handler with Financial Coordination & Specimen Guard
Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED
Objective: Memperbarui command `LabOrderCancelCmd` dan `LabOrderCancelHandler` untuk menegakkan guard pencegahan pembatalan pasca-spesimen (`>= Collected`), serta menyinkronkan pelepasan beban biaya tagihan ke domain Tata Rekening (`TRK-BILLING`) jika order telah berstatus `Charged`.
Depends On: P1-S02, P2-S03
Repository: `b09-bilreg-api`
Completion Criteria:
1. Handler menolak pembatalan dan melempar exception jika status order sudah `Collected` ke atas (Exception `EX-03`).
2. Jika order berstatus `Charged` dan memiliki `BillingTindakanId`:
   - Handler memverifikasi status billing ke Tata Rekening (`ITataRekeningRepo`).
   - Jika billing pasien telah `Final` atau `Lunas`, sistem menolak pembatalan order lab.
   - Jika billing masih terbuka, sistem memicu void/penghapusan charge tindakan di Tata Rekening sebelum status order lab diubah menjadi `Cancelled`.
3. Jejak audit pembatalan (`CancelledReason`, `CancelledDate`, `CancelledUserId`) tersimpan dengan benar.
4. Unit test pembatalan mencakup skenario: pembatalan order reguler, pembatalan order charged dengan billing terbuka, penolakan pembatalan order charged dengan billing final/lunas, dan penolakan pembatalan pasca-spesimen.
Notes: Memenuhi Exception EX-03 dan menjaga konsistensi keuangan antar-domain.

---

### P4-S07
Title: Detail & Worklist Query Handlers Enhancement
Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED
Objective: Memperbarui use case `LabOrderGetQuery` dan `LabOrderWorklistQuery` untuk menyajikan data perujuk, `ClinicalNote`, dan `Urgency`, serta memfasilitasi penyaringan khusus order darurat (Cito) pada worklist operasional laboratorium.
Depends On: P2-S03
Repository: `b09-bilreg-api`
Completion Criteria:
1. `LabOrderGetResponse` mengembalikan `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, `Urgency`, dan `IsCito`.
2. `LabOrderItemResponse` mengembalikan `Urgency` dan `IsCito` per baris item.
3. `LabOrderWorklistQuery` mendukung parameter filter `IsCitoOnly` pada `LabOrderWorklistFilter`.
4. `LabOrderWorklistView` menyajikan `IsCito`, `DokterPerujukName`, `LayananPerujukName`, dan `DeferredReason`.
5. Unit test kueri detail dan worklist di `Bilreg.Test/LabContext/LabOrderFeature` lulus pengujian.
Notes: Memenuhi kriteria AC-04 dan AC-08.

---

## P5 - Web API Endpoints & Contract Verification

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P5-S08
Title: Web API Controller Endpoints & Contract Integration Verification
Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED
Objective: Memperbarui endpoint pada `LabOrderController.cs` untuk menerima request model baru (perujuk, catatan klinis, urgensi, flag konfirmasi duplikasi) dan menyajikan respons terstandar `JSendOk` untuk order intake, detail kueri, dan worklist kueri.
Depends On: P3-S04, P3-S05, P4-S06, P4-S07
Repository: `b09-bilreg-api`
Completion Criteria:
1. Endpoint `POST /api/LabContext/LabOrderFeature/from-emr` menerima dan memproses request DTO yang memuat data perujuk dan urgensi.
2. Endpoint `POST /api/LabContext/LabOrderFeature/external-patient` menerima dan memproses request DTO pasien eksternal.
3. Endpoint `POST /api/LabContext/LabOrderFeature/cancel` memproses pembatalan dengan alasan pembatalan.
4. Endpoint `GET /api/LabContext/LabOrderFeature/{orderId}` menyajikan detail lengkap pesanan laboratorium.
5. Endpoint `GET /api/LabContext/LabOrderFeature/worklist` mendukung parameter `isCitoOnly` dan menyajikan data perujuk serta status darurat Cito.
6. Seluruh controller unit test dan integration test di `Bilreg.Test/LabContext/LabOrderFeature` lulus pengujian dengan nol warning/error.
Notes: Seluruh endpoint membungkus payload menggunakan envelope `JSendOk`.

---

# 6. Change Log

- **2026-10-15**: Rencana implementasi awal dirilis dengan status `Execution Approval: APPROVED` oleh peran Arsitek (`ica-architect`), mencakup 5 fase dan 8 irisan implementasi (*continuous numbering* P1-S01 s/d P5-S08) untuk repositori backend `b09-bilreg-api`.
