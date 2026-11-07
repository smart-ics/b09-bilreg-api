---
Title: Core Feature Hasil Pemeriksaan Laboratorium Terverifikasi Implementation Plan
Code: OC-LAB-HASIL-LAB
Artifact: IMPLEMENTATION-PLAN
Version: 2.0
LastUpdated: 2026-11-06
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Implementasi terfokus pada fondasi dan logika inti (*Core Feature & Integration Engine*) siklus hidup Hasil Pemeriksaan Laboratorium Terverifikasi (*Result Management*). Lingkup difokuskan secara mendalam pada Core Backend (`b09-bilreg-api`) dan Integration Middleware (`a046_oware_ordermiddlewareapi`), meliputi pencatatan nilai analitik, evaluasi auto-flagging rentang rujukan, pencatatan peringatan nilai kritis sederhana (*soft-warning* `CriticalNote`), verifikasi medis Sp.PK dengan catatan ekspertise patolog (`ExpertiseNote`), bridging ingesti hasil analitik otomatis dari LIS pihak ketiga melalui pemetaan `OWARE_MapPemeriksaan` di Oware, sinkronisasi status rilis tagihan (*billing clearance*), dan cetakan PDF resmi.

*(Catatan Lingkup: Implementasi antarmuka frontend web `c012_myhospital_web` dan layer BFF ditangguhkan ke fase berikutnya menunggu penyelesaian proses perancangan UI/UX & BFF).*

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-LAB-HASIL-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEATURE.md)
- ARCHITECTURE: [OC-LAB-HASIL-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ARCHITECTURE.md)
- FEASIBILITY-ASSESSMENT: [OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Lingkup difokuskan pada Core Backend dan Integration Middleware:
1. **Core Billing/Registration Backend (`b09-bilreg-api`)**:
   - Skrip migrasi DDL untuk kolom `ExpertiseNote VARCHAR(2000)` dan `CriticalNote VARCHAR(1000)` pada tabel `BILRG_LabResultDocument`.
   - Pembaruan enumerasi `LabResultStatusEnum` (`Released = 4`, `Amended = 5`) dan agregat domain `LabResultDocumentModel` (guard komponen mandatori, method `Verify`, `MarkReleased`, `SetCriticalNote`, dan factory method `CreateFromLisIngestion`).
   - Pembaruan lapisan persistensi eksplisit Dapper: `LabResultDocumentDto`, `LabResultDocumentDal`, dan `LabResultDocumentRepo`.
   - Use Case Ingesti LIS `LabResultIngestFromLisCmd` (penerima data LIS dari Oware, pencocokan 1:1 perancah order via `ComponentCode`, adopsi flag LIS + fallback flagging, status `Verified`, sumber `ExternalLis`).
   - Peningkatan Use Case Verifikasi `LabResultVerifyCmd` (dukungan `ExpertiseNote`, validasi kredensial Sp.PK) dan Sinkronisasi Rilis `LabOrderReleaseCmd` (sinkronisasi status dokumen ke `Released`).
   - Penyelarasan Use Case Entri Manual `LabResultRecordCmd` dan Amandemen `LabResultAmendCmd` untuk mendukung `CriticalNote`.
   - Pembaruan REST API Controller `LabResultController` (`POST ingest-lis`, update `verify`, queries) dan pembaruan `LabResultPdfRenderer` (menampilkan catatan ekspertise patolog dan catatan nilai kritis).
2. **Integration Middleware (`a046_oware_ordermiddlewareapi`)**:
   - Skrip DDL migrasi `OWARE_MapPemeriksaan_Alter_ConsId.sql` untuk menambahkan kolom `ConsId VARCHAR(20)` dan indeks pencarian.
   - Pembaruan DTO & DAL `MapPemeriksaanModel` dan `IMapPemeriksaanDal` untuk *reverse-lookup* `(ConsId, LabTestCode) -> PemeriksaanId`.
   - Pembuatan HTTP client outbound `IBilregLabResultClient` / `BilregLabResultClient`.
   - Pengintegrasian pemetaan dan dispatch Bilreg pada command handler `SaveResultLabCommandHandler` dengan mekanisme skip & log untuk unmapped item.

---

# 3. Dependencies

- **Prerequisite Outcomes**:
  - `OC-LAB-REG-EXTERNAL`: Registrasi pasien lab luar / rujukan.
  - `OC-LAB-ORDER-LAB`: Order pemeriksaan laboratorium dan perancah tes (`LabOrderModel`).
  - `OC-LAB-ORDER-CHARGE`: Pengenaan tarif pemeriksaan lab.
  - `OC-LAB-SAMPLE-COLLECTION`: Pengambilan spesimen & tabung vacutainer.
  - `OC-LAB-TEST`: Master tes dan komponen analitik lab.
- **External Dependencies**:
  - Basis data SQL Server `b09-bilreg-api` dan `a046_oware_ordermiddlewareapi`.
  - Simulator / payload data LIS pihak ketiga.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Database Schema & Core Domain Model (`b09-bilreg-api`) | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P2 - Persistence & Data Access Layer (`b09-bilreg-api`) | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P3 - Core Application Use Cases (`b09-bilreg-api`) | NOT-STARTED | NOT-REVIEWED | 0/3 |
| P4 - API Presentation & Official PDF Document (`b09-bilreg-api`) | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P5 - Integration Middleware Bridge (`a046_oware_ordermiddlewareapi`) | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P6 - Multi-Repo Targeted Verification & Acceptance Gate | NOT-STARTED | NOT-REVIEWED | 0/1 |

---

# 5. Phases

## P1 - Database Schema & Core Domain Model (`b09-bilreg-api`)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P1-S01

Title: Database Schema Migration for Expertise Note & Critical Note  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menyediakan skrip DDL migrasi SQL untuk menambahkan kolom `ExpertiseNote` dan `CriticalNote` pada tabel `BILRG_LabResultDocument` dengan default constraint yang aman dan non-breaking.  
Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Terciptanya file skrip `BILRG_LabResultDocument_M12_Alter.sql` pada direktori `src/bilreg/Bilreg.SqlDb/LabContext/LabResultFeature/`.
- DDL menambahkan kolom `ExpertiseNote VARCHAR(2000)` dan `CriticalNote VARCHAR(1000)` dengan default constraint `''`.  
Notes: Bersifat non-breaking terhadap data historis.  
Implementation Notes:
- Membuat skrip DDL migrasi `BILRG_LabResultDocument_M12_Alter.sql` dengan pengecekan `COL_LENGTH` yang idempoten untuk menambahkan kolom `ExpertiseNote VARCHAR(2000)` (default `''`) dan `CriticalNote VARCHAR(1000)` (default `''`).
- Memperbarui skrip tabel dasar `BILRG_LabResultDocument.sql` agar mencakup kedua kolom untuk fresh deployment.
- Mendaftarkan skrip migrasi ke `Bilreg.SqlDb.sqlproj`.
- Verifikasi build dan test via `dotnet test` dengan filter `LabResult` lulus (42 passed).
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabResultFeature/BILRG_LabResultDocument_M12_Alter.sql`
- `b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabResultFeature/BILRG_LabResultDocument.sql`
- `b09-bilreg-api/src/bilreg/Bilreg.SqlDb/Bilreg.SqlDb.sqlproj`

---

### P1-S02

Title: Domain Model Evolution for Status Lifecycle, Mandatory Guard, and LIS Ingestion  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memutakhirkan enumerasi status, agregat `LabResultDocumentModel`, dan domain logic untuk mendukung ekspertise patolog, catatan nilai kritis, guard komponen mandatori, sinkronisasi rilis, dan pembentukan dokumen dari hasil LIS.  
Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabResultStatusEnum` dimutakhirkan dengan menambahkan nilai `Released = 4` dan `Amended = 5`.
- `LabResultDocumentModel` memuat properti `ExpertiseNote` dan `CriticalNote`.
- Penambahan metode `SetCriticalNote(string note)` pada `LabResultDocumentModel`.
- Pembaruan metode `Verify(...)` untuk menerima parameter `expertiseNote` dan menegakkan invariant kegagalan jika ada komponen bertanda `IsMandatory == true` yang belum terisi nilainya.
- Penambahan metode `MarkReleased(string userId, DateTime releasedAt)` yang memvalidasi bahwa dokumen berstatus `Verified` sebelum beralih ke `Released`.
- Penambahan factory method `CreateFromLisIngestion(...)` yang membentuk dokumen hasil berstatus `Verified` dengan `ResultSource = LabResultSourceEnum.ExternalLis` dan mencatat validator LIS.
- Pengujian unit pada `Bilreg.Test/LabContext/LabResultFeature/LabResultDocumentModelTest.cs` memverifikasi seluruh transisi status baru, guard komponen mandatori, dan factory method LIS.  
Notes:
- `LabResultStatusEnum` dimutakhirkan dengan menambahkan `Released = 4` dan `Amended = 5`.
- `LabResultDocumentModel` dilengkapi properti `ExpertiseNote` dan `CriticalNote`, metode `SetCriticalNote`, `SetExpertiseNote`, `MarkReleased`, pembaruan `Verify` dengan parameter `expertiseNote` dan penegakan invariant guard komponen mandatori, serta factory method `CreateFromLisIngestion`.
- Penyelarasan siklus amandemen `AmendVerifiedToNewVersion` untuk menandai versi terdahulu sebagai `LabResultStatusEnum.Amended` dan mempertahankan snapshot catatan ekspertise dan kritis ke versi baru.
- Seluruh 22 unit test pada `LabResultDocumentModelTest.cs` dan 52 unit test pada `LabResultFeature` lulus verifikasi (100%).
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultStatusEnum.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultDocumentModel.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultDocumentModelTest.cs`

---

## P2 - Persistence & Data Access Layer (`b09-bilreg-api`)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P2-S03

Title: Infrastructure DTO & DAL Updates for Document Persistence  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui pemetaan DTO, query SQL eksplisit (Dapper), dan repository `ILabResultDocumentRepo` untuk menyimpan dan membaca kolom `ExpertiseNote` dan `CriticalNote`.  
Depends On: P1-S01, P1-S02  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabResultDocumentDto` diperbarui untuk memuat properti `ExpertiseNote` dan `CriticalNote`, serta metode `FromModel` dan `ToModel` memetakan kolom baru secara presisi.
- `LabResultDocumentDal` diperbarui pada klausa `SELECT`, `INSERT`, dan `UPDATE` eksplisit untuk menyertakan `ExpertiseNote` dan `CriticalNote`.
- Kueri `LoadByOrderId` dan `LoadEntity` mengembalikan agregat domain lengkap dengan nilai ekspertise dan catatan kritis.
- Pengujian repositori pada `Bilreg.Test/LabContext/LabResultFeature/LabResultDocumentRepoTest.cs` lulus verifikasi simpan dan muat.  
Notes: Menggunakan Dapper SQL eksplisit tanpa ORM berat.  
Implementation Notes:
- Menambahkan properti `ExpertiseNote` dan `CriticalNote` ke dalam record `LabResultDocumentDto` serta memetakan keduanya pada metode `FromModel` dan `ToModel` (meneruskan ke `LabResultDocumentModel.Load`).
- Memperbarui SQL eksplisit pada `LabResultDocumentDal`: menyertakan kolom `ExpertiseNote` dan `CriticalNote` pada pernyataan `INSERT`, `UPDATE`, `GetData` (`SELECT`), dan `GetByOrderId` (`SELECT`), serta menambahkan parameter Dapper bertipe `SqlDbType.VarChar` pada `BuildParams`.
- Memperbarui dan memperluas unit test pada `LabResultDocumentRepoTest.cs` untuk memverifikasi pemetaan catatan ekspertise dan kritis saat insert, update, pembacaan agregat (`LoadEntity` dan `LoadByOrderId`), serta pemetaan bolak-balik DTO (`FromModel` / `ToModel`).
- Menjalankan seluruh pengujian repositori dan fitur hasil lab: 8 unit test pada `LabResultDocumentRepoTest` dan 55 unit test pada `LabResult` lulus 100%.
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultDocumentDto.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultDocumentDal.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultDocumentRepoTest.cs`

---

## P3 - Core Application Use Cases (`b09-bilreg-api`)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P3-S04

Title: Ingestion Use Case for External LIS Results (Oware to Bilreg)  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengembangkan use case `LabResultIngestFromLisCmd` untuk menerima data analitik yang sudah terpetakan dari Oware, melakukan pencocokan 1:1 ke perancah order berdasarkan ComponentCode (PemeriksaanId), mengadopsi flag LIS dengan fallback domain service, dan menyimpan dokumen berstatus `Verified`.  
Depends On: P1-S02, P2-S03  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Pembuatan record command `LabResultIngestFromLisCmd` memuat nomor order, identitas validator LIS, stempel waktu, catatan kritis opsional, dan daftar komponen hasil `(ComponentCode, Value, Unit, ReferenceRangeText, Flag)` di mana `ComponentCode` sudah merupakan `PemeriksaanId` HIS yang valid dari Oware.
- Handler `LabResultIngestFromLisHandler`:
  - Mengambil data order dan perancah komponen (`ILabResultScaffoldService`).
  - Mencocokkan setiap item hasil secara langsung 1:1 berdasarkan `ComponentCode` terhadap `LabOrderItemComponent.ComponentCode`.
  - Mengadopsi flag LIS (Normal, Low, High, Critical); jika flag LIS kosong/tidak dikenal, mengevaluasi otomatis via domain service `LabResultFlagging`.
  - Menginstansiasi agregat `LabResultDocumentModel` melalui `CreateFromLisIngestion` dengan status `Verified`.
  - Memperbarui status `LabOrderModel` menjadi `Verified`.
  - Menyimpan transaksi order dan dokumen hasil secara atomik (`TransHelper.NewScope()`).
- Pengujian unit `LabResultIngestFromLisHandlerTest.cs` pada `Bilreg.Test` menguji skenario direct matching 1:1, adopsi flag LIS + fallback flagging, dan verifikasi status.  
Notes: Mendukung pembaruan hasil bertahap (*partial result ingestion*).  
Implementation Notes:
- Menambahkan nilai `Critical = 4` ke enumerasi `LabResultFlagEnum` dan menyelaraskan mapping pada `LabResultPdfQuery`.
- Membuat DTO `LabResultIngestItemDto` dan record command `LabResultIngestFromLisCmd` memuat `OrderId`, `OrderNo`, `LisRefNo`, `ValidatorName`, `ValidatedDate`, `Items`, dan `CriticalNote`.
- Mengembangkan `LabResultIngestFromLisHandler` yang mengorkestrasikan:
  - Validasi status order (`Charged`, `Collected`, `Recorded`, `Verified`).
  - Resolusi scaffold komponen order via `ILabResultScaffoldService.BuildFromOrder`.
  - Pencocokan deterministik 1:1 antara `ComponentCode` LIS dan perancah order.
  - Adopsi flag klinis LIS (`Low`, `High`, `Critical`, `Normal`) dengan fallback evaluasi via `LabResultFlagging.Compute`.
  - Dukungan partial result ingestion: komponen yang tidak dikirim dipertahankan sebagai item perancah bernilai default/pending atau mempertahankan nilai ingest sebelumnya.
  - Pembentukan agregat hasil berstatus `Verified` via `LabResultDocumentModel.CreateFromLisIngestion`.
  - Pembaruan status agregat order menjadi `Verified` via `order.MarkRecorded` -> `order.MarkVerified`.
  - Penggantian dokumen lama (jika ada) dan penyimpanan atomik order serta dokumen hasil dalam satu transaksi `TransHelper.NewScope()`.
- Menulis pengujian unit menyeluruh di `LabResultIngestFromLisHandlerTest.cs` mencakup 9 skenario: direct matching 1:1, adopsi flag LIS, fallback auto-flagging rentang rujukan, partial ingestion, transisi status order ke `Verified`, preservasi nilai saat subsequent partial ingest, validasi order not found, dan penolakan status order tidak valid.
- Seluruh 9 test `LabResultIngestFromLis` dan 67 test pada `LabResultFeature` lulus verifikasi 100%.
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultFlagEnum.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultPdfQuery.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultIngestFromLisCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultIngestFromLisHandlerTest.cs`
- `b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-IMPLEMENTATION-PLAN.md`

---

### P3-S05

Title: Verification & Release Use Case Enhancements  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui `LabResultVerifyCmd` untuk mendukung catatan ekspertise patolog dan penegakan otorisasi Sp.PK, serta menyelaraskan `LabOrderReleaseCmd` untuk memperbarui status dokumen hasil menjadi `Released`.  
Depends On: P1-S02, P2-S03  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabResultVerifyCmd` diperbarui menerima `ExpertiseNote`.
- `LabResultVerifyHandler` memvalidasi otorisasi verifikator (memeriksa peran dokter spesialis patologi klinik `doktersppk` atau permission `lab.result.verify`) dan meneruskan catatan ekspertise ke domain model `result.Verify(userId, date, expertiseNote)`.
- Use case `LabOrderReleaseCmd` memuat dokumen hasil yang berelasi dan memanggil `result.MarkReleased(userId, releaseDate)` saat verifikasi clearance billing `TRK-BILLING` berstatus `CLEAR`.
- Pengujian unit `LabResultVerifyHandlerTest.cs` memverifikasi lolosnya verifikasi Sp.PK, penolakan non-Sp.PK, dan penyimpanan catatan ekspertise.  
Notes: Menjaga pemisahan tegas antara otorisasi medis internal dan rilis administratif ke pasien.  
Implementation Notes:
- Memperbarui `LabResultVerifyCmd` dengan penambahan properti opsional `string ExpertiseNote = ""`.
- Memperbarui `LabResultVerifyHandler`:
  - Menginjeksi `ICurrentUserContext` (dengan constructor chaining untuk backwards-compatibility) dan memvalidasi otorisasi verifikator dengan memeriksa peran Sp.PK (`doktersppk`, `dokter_sppk`, `sp.pk`, `sppk`) atau permission `lab.result.verify`. Menolak verifikator tanpa otorisasi dengan melempar `UnauthorizedAccessException`.
  - Meneruskan `request.ExpertiseNote` ke metode agregat domain `result.Verify(request.VerifiedUserId, request.VerifiedDate, request.ExpertiseNote)`.
  - Memastikan pembaruan status `LabOrderModel` dan `LabResultDocumentModel` tersimpan secara atomik dalam satu transaksi database (`TransHelper.NewScope()`).
- Memperbarui `LabOrderReleaseCmd` dan `LabOrderReleaseHandler`:
  - Menambahkan dependensi `ILabResultDocumentRepo? _labResultDocumentRepo` pada constructor.
  - Saat proses pelepasan order (`LabOrderReleaseCmd`), memuat dokumen hasil laboratorium terkait via `_labResultDocumentRepo.LoadByOrderId(request.LabOrderId)`. Jika dokumen hasil ditemukan dan berstatus `Verified`, menandai dokumen menjadi `Released` via `result.MarkReleased(request.UserId, releaseDate)` dan menyimpannya secara atomik dalam transaksi `releaseTrans`.
- Memperbaiki pemanggilan spesimen pada `LabResultIngestFromLisHandlerTest.cs` agar selaras dengan signature domain `CollectSpecimen`.
- Memperbarui dan memperluas unit test pada `LabResultVerifyHandlerTest.cs`:
  - Mocking `ICurrentUserContext` dengan otorisasi `doktersppk` dan `lab.result.verify`.
  - Menguji skenario verifikasi berhasil dan tersimpannya `ExpertiseNote`.
  - Menguji skenario penolakan akses verifikator tanpa peran/permission yang sesuai (`UnauthorizedAccessException`).
  - Menguji skenario invariant guard mandatori komponen hasil lab yang belum terisi (`InvalidOperationException`).
- Menambahkan pengujian pada `LabOrderReleaseHandlerTest.cs` untuk memverifikasi sinkronisasi status dokumen hasil lab ke `Released` ketika order dirilis, serta mengabaikan dokumen yang belum berstatus `Verified`.
- Seluruh pengujian lulus 100%: 8 unit test pada `LabResultVerifyHandlerTest`, 6 unit test pada `LabOrderReleaseHandlerTest`, dan 71 unit test pada `LabResultFeature`.
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultVerifyCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderReleaseCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultVerifyHandlerTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderReleaseHandlerTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultIngestFromLisHandlerTest.cs`

---

### P3-S06

Title: Manual Recording & Amendment Use Case Enhancements  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memutakhirkan use case `LabResultRecordCmd` dan `LabResultAmendCmd` agar mendukung input `CriticalNote` dan pemeliharaan catatan kritis pada siklus amandemen dokumen versi baru.  
Depends On: P1-S02, P2-S03  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabResultRecordCmd` dan `LabResultRecordHandler` diperbarui menerima parameter `CriticalNote` opsional dan menerapkannya pada dokumen hasil yang dibentuk/dimutakhirkan.
- `LabResultAmendCmd` dan `LabResultAmendHandler` mempertahankan salinan snapshot nilai ekspertise dan catatan kritis ke dokumen versi baru berstatus `Recorded`.
- Pengujian unit `LabResultRecordHandlerTest.cs` dan `LabResultAmendHandlerTest.cs` pada `Bilreg.Test` lulus pengujian.  
Notes: Menjamin kelengkapan siklus amandemen dokumen hasil analitik.  
Implementation Notes:
- Menambahkan parameter opsional `string CriticalNote = ""` pada record command `LabResultRecordCmd` dan memanggil `result.SetCriticalNote(request.CriticalNote)` pada `LabResultRecordHandler` sebelum dokumen ditandai `MarkRecorded`.
- Memperbarui `LabResultAmendHandler` agar validasi status dokumen mengizinkan amandemen dari status `Verified` maupun `Released`, serta memastikan amandemen dokumen mempensiunkan versi lama dengan status `LabResultStatusEnum.Amended` dan meneruskan snapshot catatan ekspertise (`ExpertiseNote`) dan catatan nilai kritis (`CriticalNote`) ke dokumen versi baru berstatus `Recorded`.
- Memperluas unit test pada `LabResultRecordHandlerTest.cs` untuk memverifikasi penyimpanan catatan nilai kritis saat entri hasil awal dan pembaruan hasil, serta pada `LabResultAmendHandlerTest.cs` untuk memverifikasi penerusan snapshot `CriticalNote` dan `ExpertiseNote` ke versi baru dan status `Amended` pada versi lama baik untuk dokumen berstatus `Verified` maupun `Released`.
- Menjalankan pengujian `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test --filter "FullyQualifiedName~LabResultRecord|FullyQualifiedName~LabResultAmend"`: 8 passed (100% lolos).
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultRecordCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultAmendCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultRecordHandlerTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultAmendHandlerTest.cs`

---

## P4 - API Presentation & Official PDF Document (`b09-bilreg-api`)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P4-S07

Title: REST API Endpoints & Official PDF Renderer Enhancements  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengekspos endpoint API ingesti LIS, memperbarui endpoint entri & verifikasi, dan memutakhirkan renderer PDF hasil laboratorium agar menampilkan ekspertise patolog dan catatan nilai kritis.  
Depends On: P3-S04, P3-S05, P3-S06  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabResultController` mengekspos endpoint `POST api/LabContext/LabResultFeature/ingest-lis` yang menerima payload DTO ingesti LIS dan memanggil `LabResultIngestFromLisCmd`.
- Endpoint `PATCH api/LabContext/LabResultFeature/verify` menerima field `ExpertiseNote` dan memetakan ke `LabResultVerifyCmd`.
- Endpoint `POST api/LabContext/LabResultFeature/record` menerima field `CriticalNote`.
- `LabResultPdfRenderer` diperbarui untuk merender bagian khusus "Catatan Ekspertise Patologi Klinik", identitas/tanda tangan verifikator Sp.PK (atau validator LIS), serta banner peringatan jika terdapat nilai kritis atau `CriticalNote`.
- Pengujian unit `LabResultPdfRendererTest.cs` memverifikasi kelengkapan layout PDF tanpa error.  
Notes: Format PDF mematuhi standar rekam medis penunjang diagnostic hospital.  
Implementation Notes:
- Memperbarui `LabResultController`:
  - Menambahkan endpoint `POST api/LabContext/LabResultFeature/ingest-lis` (dengan alias `ingestLis`) yang menerima `LabResultIngestLisRequest` dan memetakan ke command `LabResultIngestFromLisCmd`.
  - Memperbarui endpoint `PATCH api/LabContext/LabResultFeature/verify` yang menerima `LabResultVerifyRequest` memuat `ExpertiseNote` dan memetakan ke command `LabResultVerifyCmd`.
  - Memperbarui endpoint `POST api/LabContext/LabResultFeature/record` yang menerima `LabResultRecordRequest` memuat `CriticalNote` serta fallback mapping payload `Items` dan memetakan ke command `LabResultRecordCmd`.
  - Menyediakan implicit conversion operator dan backward-compatible constructors pada seluruh request DTO controller.
- Memperbarui `LabResultPdfView` dan `LabResultPdfQuery`:
  - Menambahkan field `ExpertiseNote` dan `CriticalNote` dengan nilai default kosong pada record `LabResultPdfView`.
  - Memutakhirkan `MapToPdfView` pada `LabResultPdfHandler` untuk memetakan `result.ExpertiseNote` dan `result.CriticalNote`, serta menyelaraskan label status `Released` dan `Amended`.
- Memutakhirkan `LabResultPdfRenderer`:
  - Merender banner peringatan nilai kritis (`[PERINGATAN NILAI KRITIS]`) berlatar belakang merah muda lembut dan bergaris tepi merah jika terdeteksi flag kritis (`C` / `Critical`) atau jika `CriticalNote` terisi.
  - Menyorot komponen tes berstatus kritis dengan warna merah tebal (`fontBodyBold` & dark red brush) pada tabel rincian hasil.
  - Merender bagian khusus "Catatan Ekspertise Patologi Klinik:" secara rapi dengan word-wrapping multiline otomatis jika `ExpertiseNote` terisi.
  - Merender blok penanggung jawab / verifikator digital di bagian bawah dokumen yang membedakan dokter Sp.PK dan validator LIS (`lis:...`) beserta stempel waktu verifikasi.
- Menulis dan memperluas unit test:
  - `LabResultPdfRendererTest.cs`: Menguji rendering PDF standar, dokumen dengan catatan ekspertise patolog, dokumen dengan penanda dan banner nilai kritis, dokumen dengan identitas validator LIS, serta dokumen multi-halaman dengan 60 item komponen.
  - `LabResultControllerTest.cs`: Menguji seluruh anotasi routing, atribut `[ApiController]` dan `[Authorize]`, serta pemanggilan use case `Record`, `Verify`, `IngestLis`, `VerificationWorklist`, `Get`, `Amend`, dan pengunduhan file `Pdf`.
  - `LabResultPdfHandlerTest.cs`: Menguji pemetaan data agregat hasil lab (termasuk `ExpertiseNote` dan `CriticalNote`) ke `LabResultPdfView` pada query handler.
- Menjalankan pengujian: Seluruh 18 unit test pada `LabResultPdf` & `LabResultController`, serta seluruh 88 unit test pada `LabResultFeature` lulus verifikasi 100%.
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabResultFeature/LabResultController.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultPdfQuery.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultPdfRenderer.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultPdfRendererTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultControllerTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultPdfHandlerTest.cs`
- `b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-IMPLEMENTATION-PLAN.md`

---

## P5 - Integration Middleware Bridge (`a046_oware_ordermiddlewareapi`)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P5-S08

Title: OWARE_MapPemeriksaan Schema Evolution & Reverse-Lookup Service  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperkaya skema tabel OWARE_MapPemeriksaan dengan kolom ConsId untuk mendukung multi-vendor LIS, serta menyediakan repository/helper reverse-lookup (ConsId, LabTestCode) -> PemeriksaanId.  
Depends On: None  
Repository: `a046_oware_ordermiddlewareapi`  
Completion Criteria:
- Pembuatan skrip DDL migrasi `OWARE_MapPemeriksaan_Alter_ConsId.sql` di `Oware.SqlDb/TablesScripts/` yang menambahkan kolom `ConsId VARCHAR(20)` dan indeks `IX_OWARE_MapPemeriksaan_ConsId_LabTestCode`.
- Pembaruan `MapPemeriksaanModel` dengan properti `ConsId`.
- Pembaruan `IMapPemeriksaanDal` dan `MapPemeriksaanDal` untuk mendukung penyimpanan, pembacaan, dan kueri reverse-lookup berdasarkan `(ConsId, LabTestCode)`.
- Pengujian unit di `Oware.Test` memverifikasi reverse-lookup pemetaan berhasil menemukan `PemeriksaanId` yang sesuai per `ConsId`.  
Notes: Menjamin isolasi pemetaan antar vendor LIS tanpa bentrok kode tes.  
Implementation Notes:
- Menambahkan skrip DDL `OWARE_MapPemeriksaan_Alter_ConsId.sql` yang menambahkan kolom `ConsId VARCHAR(20)` dengan default constraint `''` dan indeks `IX_OWARE_MapPemeriksaan_ConsId_LabTestCode` covering `PemeriksaanId`.
- Memperbarui `MapPemeriksaanModel` dengan properti `ConsId` dan implementasi interface `IConsumerKey`.
- Memperbarui `IMapPemeriksaanDal` dengan signature reverse-lookup `GetByLabTestCode(string consId, string labTestCode)`.
- Memperbarui `MapPemeriksaanDal` pada query `INSERT`, `UPDATE`, `GetData`, `ListData`, dan menambahkan implementasi `GetByLabTestCode(consId, labTestCode)` serta perbaikan query `Delete`.
- Menambahkan test suite `MapPemeriksaanDalTest.cs` di `Oware.Test` (5 unit test) yang memvalidasi reverse lookup, resolusi per ConsId multi-vendor, unmapped code handling, dan implementasi interface. Seluruh test lulus 100%.  
Changed Files:
- `a046_oware_ordermiddlewareapi/Oware.SqlDb/TablesScripts/OWARE_MapPemeriksaan_Alter_ConsId.sql`
- `a046_oware_ordermiddlewareapi/Oware.Domain/Entities/Lab/MapPemeriksaanModel.cs`
- `a046_oware_ordermiddlewareapi/Oware.Application/Contracts/IRepos/IMapPemeriksaanDal.cs`
- `a046_oware_ordermiddlewareapi/Oware.Infrastructure/Repos/MapPemeriksaanDal.cs`
- `a046_oware_ordermiddlewareapi/Oware.Test/MapPemeriksaanDalTest.cs`

---

### P5-S09

Title: Realtime Result Dispatching via Bilreg Lab Result Client in SaveResultLabCommandHandler  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Mengintegrasikan reverse-lookup pemetaan OWARE_MapPemeriksaan, penanganan unmapped items (logging peringatan), dan pengiriman HTTP client ke Bilreg API secara realtime dalam SaveResultLabCommandHandler.  
Depends On: P4-S07, P5-S08  
Repository: `a046_oware_ordermiddlewareapi`  
Completion Criteria:
- Terciptanya interface `IBilregLabResultClient` dan implementasi `BilregLabResultClient` di `Oware.Infrastructure/Shared/` yang memanggil `POST /api/LabContext/LabResultFeature/ingest-lis`.
- `SaveResultLabCommandHandler` menginjeksi `IMapPemeriksaanDal` dan `IBilregLabResultClient`:
  - Mengambil daftar hasil LIS dari parser.
  - Memetakan setiap item tes LIS menggunakan `(request.ClientId, item.TestId) -> PemeriksaanId`.
  - Item yang tidak terdaftar di `OWARE_MapPemeriksaan` dilewati (*skipped*) dan dicatat warning log ke `OWARE_ResultLog`.
  - Item yang terpetakan disusun menjadi payload ingesti dengan `ComponentCode = PemeriksaanId` dan menyertakan flag analitik asal LIS.
  - Memanggil `_bilregClient.IngestLabResultAsync(...)` secara realtime.
- Jika panggilan Bilreg gagal sementara karena koneksi, log ditandai `Failed` untuk retry tanpa membatalkan transaksi lokal Oware.
- Pengujian unit `SaveResultLabCommandHandlerTest` memverifikasi pemetaan berhasil, unmapped item dicatat log, dan client Bilreg dipanggil dengan payload yang valid.  
Notes: Bilreg menerima data bersih yang sudah terpetakan ke ComponentCode HIS resmi.

---

## P6 - Multi-Repo Targeted Verification & Acceptance Gate

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

### P6-S10

Title: Multi-Repo Backend & Middleware Targeted Regression & Acceptance Verification  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Menjalankan rangkaian verifikasi terarah pada core backend Bilreg dan middleware Oware untuk membuktikan terpenuhinya seluruh kondisi penerimaan arsitektur core (AC-COND-01 s/d AC-COND-05, dan AC-COND-07).  
Depends On: P4-S07, P5-S09  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Pengujian unit backend `b09-bilreg-api`: `dotnet test --filter FullyQualifiedName~LabResult` lulus 100%.
- Pengujian unit middleware `a046_oware_ordermiddlewareapi`: `dotnet test --filter FullyQualifiedName~SaveResultLab` dan `MapPemeriksaan` lulus 100%.
- Verifikasi skenario integrasi core:
  - Reverse-lookup Oware memetakan `LabTestCode` LIS ke `PemeriksaanId` HIS.
  - Ingesti hasil dari Oware masuk ke Bilreg API sebagai `Verified` dengan validator LIS dan `ResultSource = ExternalLis`.
  - Direct matching 1:1 ke perancah order berjalan sukses.
  - Verifikasi manual memvalidasi kelengkapan komponen mandatori dan otorisasi dokter Sp.PK.
  - Catatan ekspertise patolog dan catatan nilai kritis tersimpan persisten dan tampil pada cetakan PDF resmi.
  - Status rilis tersinkronisasi akurat saat clearance tagihan `TRK-BILLING`.  
Notes: Pintu gerbang kelulusan tahap Core Backend sebelum dilanjutkan ke tahap perancangan UI/UX dan BFF frontend.

---

# 6. Change Log

- **2026-11-06 (v1.0)**: Inisiasi rencana implementasi awal untuk Outcome `OC-LAB-HASIL-LAB`.
- **2026-11-06 (v1.1)**: Penyesuaian arsitektur bridging: pemetaan item tes LIS dipindahkan ke sisi Oware memanfaatkan tabel `OWARE_MapPemeriksaan` (+ kolom `ConsId`), Bilreg API melakukan direct matching 1:1 terhadap `ComponentCode` perancah order, dan adopsi flag LIS dengan fallback domain service `LabResultFlagging`.
- **2026-11-06 (v2.0)**: Rekonstruksi fokus rencana implementasi secara eksklusif pada **Core Feature Hasil Lab (Backend `b09-bilreg-api` & Middleware `a046_oware_ordermiddlewareapi`)**. Seluruh pekerjaan frontend web (`c012_myhospital_web`) dan layer BFF ditangguhkan (*deferred*) ke fase terpisah sejalan dengan rencana perancangan UI/UX dan BFF mendatang.
