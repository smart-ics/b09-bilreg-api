---
Title: Implementation Plan - Penyatuan Relasi InpatientOrderId dan Pembersihan Total Kolom Warisan pada Admisi Ranap Journey
Code: ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-05
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Implementasi ini merealisasikan arsitektur penyatuan relasi `InpatientOrderId` dan pembersihan total kolom warisan `OpnameRequestId` serta `ReservationId` dari seluruh lapisan kode program (*DAL, DTO, Repository, Orchestrator, Domain Model*) pada konteks Admisi Ranap dan pemantauan perjalanan (*Journey Feature*).

Perbaikan ini menyelesaikan dua masalah operasional utama:
1. **Eliminasi Penguncian Status "Perlu Pemeriksaan Data"**: Mengembalikan penentuan tahap operasional Release 1 yang benar yaitu **"Menunggu Diterima Bangsal"** (`WardAcceptanceRequired`) untuk pasien hasil registrasi reservasi/opname (seperti pasien "SRI AJENG RETNO", No. RM `00-00-60-63`, order `OPN0A31CDRTQ`).
2. **Eliminasi Infinite Loading / Hang**: Mengembalikan konsistensi JourneyId antara worklist (`rsv:OPN...`) dan respons detail, sehingga panel kanan (*Journey Workspace*) langsung memuat dan merender data pasien tanpa tertahan pada *skeleton loading*.

Referenced artifacts:
- Originating BUG ISSUE: [ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-ISSUE.md) (`ISSUE-ADMISI-RANAP-REKONSILIASI-ORDER-001`)
- Analysis Artifact: [ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-INVESTIGATION.md)
- Target Architecture: [ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

## Included Scope
1. **Orkestrasi Pendaftaran Admisi (`AdmissionRegistrationOrchestrator.cs`)**:
   - Pemutakhiran alur pendaftaran (`ProcessReservation`, `ProcessOpnameRequest`, `ProcessInpatientOrder`) agar memanggil `AdmissionModel.Admit` murni dengan `inpatientOrderId`, mengosongkan parameter `opnameRequestId` dan `reservationId` (`null` / `"-"`).
2. **Pembersihan Total Lapisan DAL Admisi (`AdmissionDal.cs`, `AdmissionDto.cs`)**:
   - Menghapus kolom `OpnameRequestId` dan `ReservationId` dari `SELECT_COLUMNS`, `INSERT INTO BILRG_AdmAdmission`, `UPDATE BILRG_AdmAdmission`, dan `MapWriteParams`.
   - Menghapus atribut `OpnameRequestId` dan `ReservationId` dari DTO `AdmissionDto`.
3. **Pembersihan Repositori Pembatalan Terkoordinasi (`CoordinatedCancellationRepo.cs`)**:
   - Menghapus kolom warisan dari kueri `BILRG_AdmAdmission WITH (UPDLOCK,HOLDLOCK)` dan record `AdmissionRow`, serta menggunakan murni `InpatientOrderId`.
4. **Pembersihan dan Penyelarasan Kueri Proyeksi Worklist (`JourneyDal.Batch.cs`)**:
   - Menghapus pembacaan kolom warisan `a.OpnameRequestId` dan `a.ReservationId` pada kueri `BuildAdmissionSelectSql`.
   - Melakukan JOIN ke `BILRG_AdmInpatientOrder` berbasis `InpatientOrderId` untuk memproyeksikan asal pesanan secara deterministik berdasarkan `OrderSource`.
5. **Penyelarasan Kueri Detail dan Derivasi Status (`JourneyDal.cs`, `JourneyIntegrityValidator.cs`)**:
   - Menyelaraskan kueri `FindAdmissionByReservationAsync`, `FindAdmissionByOpnameAsync`, dan CTE `AdmissionRoots` agar murni menggunakan `InpatientOrderId`.
   - Memperbarui aturan `ValidateAdmissionSources` pada `JourneyIntegrityValidator.cs` agar tidak lagi menandai benturan dual-source pada data yang bersumber dari `InpatientOrderId`.
6. **Verifikasi Test Suite Otomatis**:
   - Menjalankan test suite unit dan integrasi untuk memvalidasi zero regressions.

## Excluded Scope
1. **Tidak Melakukan DDL Destruktif**: Kolom fisik `OpnameRequestId` dan `ReservationId` di basis data SQL Server dibiarkan ada (tidak di-drop DDL) demi keselamatan skema dan integritas data riwayat.
2. **Tidak Ada Perubahan Kode Frontend (`c012_myhospital_web`)**: Konsistensi ID dan tahap operasional yang dikembalikan oleh backend akan otomatis menyelesaikan masalah infinite loading pada frontend tanpa perubahan kode presentasi.
3. **Kompatibilitas Mutlak SQL Server 2012**: Kueri T-SQL dilarang menggunakan fitur/fungsi di atas SQL Server 2012 (dilarang menggunakan `STRING_SPLIT`, `STRING_AGG`, `TRIM()`, `JSON_VALUE`, dll; wajib menggunakan sintaks kompatibel SQL Server 2012 seperti `LTRIM(RTRIM())`, `CASE WHEN`, `ISNULL`, `COALESCE`).

---

# 3. Dependencies

- **Database Prerequisites**: Tabel `BILRG_AdmAdmission` telah memiliki kolom `InpatientOrderId` dan indeks non-terklaster `IX_bilrg_admadmission_inpatientorderid`.
- **Inter-Slice Dependencies**:
  - `P1-S01` (Orkestrasi Pendaftaran) menjadi fondasi penulisan admisi baru.
  - `P2-S02` (Pembersihan DAL & DTO Admisi) bergantung pada `P1-S01`.
  - `P2-S03` (Pembersihan Kueri Worklist Batch) bergantung pada `P2-S02`.
  - `P3-S04` (Penyelarasan Kueri Detail & CTE) bergantung pada `P2-S03`.
  - `P3-S05` (Penyelarasan Validasi Integritas) bergantung pada `P3-S04`.
  - `P3-S06` (Verifikasi Menyeluruh & Testing) bergantung pada `P3-S05`.

---

# 4. Progress Summary

Status: NOT-STARTED  
Execution Approval: APPROVED

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Domain & Application Orchestration Cleanup | NOT-STARTED | GO | 0/1 |
| P2 - DAL & Repository Total Cleansing | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P3 - Journey Queries, Integrity Validation & Verification | NOT-STARTED | NOT-REVIEWED | 0/3 |

---

# 5. Phases

## P1 - Domain & Application Orchestration Cleanup

Implementation Status: NOT-STARTED  
Review Status: GO

### P1-S01

Title: Pembersihan Pembuatan Admisi di `AdmissionRegistrationOrchestrator.cs`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Mengarahkan seluruh pembuatan pendaftaran rawat inap (`AdmissionModel.Admit`) pada alur `ProcessReservation`, `ProcessOpnameRequest`, dan `ProcessInpatientOrder` agar menyuplai murni `inpatientOrderId: order.InpatientOrderId` dan mengosongkan parameter warisan `opnameRequestId` dan `reservationId` (`null` / `"-"`).

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:
1. `AdmissionRegistrationOrchestrator.cs` pada `ProcessReservation` (baris 255) memanggil `CreateAdmission` dengan `opnameRequestId: null`, `reservationId: null`, dan `inpatientOrderId: order.InpatientOrderId`.
2. `AdmissionRegistrationOrchestrator.cs` pada `ProcessOpnameRequest` (baris 167) memanggil `CreateAdmission` dengan `opnameRequestId: null`, `reservationId: null`, dan `inpatientOrderId: order.InpatientOrderId`.
3. Unit test terkait orkestrasi pendaftaran di `Bilreg.Test` dikompilasi dan lulus.

Implementation Notes:
- Pemanggilan `CreateAdmission` pada `ProcessOpnameRequest`, `ProcessReservation`, dan `ProcessInpatientOrder` di `AdmissionRegistrationOrchestrator.cs` telah diperbarui dengan `opnameRequestId: null`, `reservationId: null`, dan `inpatientOrderId: order.InpatientOrderId`.
- Assertions ditambahkan pada `AdmissionRegistrationOrchestratorTest.cs` untuk memastikan `OpnameRequestId` dan `ReservationId` bernilai `"-"` sementara `InpatientOrderId` terisi nilai order ID.
- Seluruh 50 unit test di `AdmissionRegistrationOrchestratorTest` lulus verifikasi.

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmissionRegistrationOrchestratorTest.cs`

Notes:
- Mengacu pada keputusan arsitektur TD-03.

---

## P2 - DAL & Repository Total Cleansing

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P2-S02

Title: Pembersihan Total Kolom Warisan pada Lapisan DAL Admisi & Pembatalan Terkoordinasi

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Menghilangkan seluruh referensi kolom `OpnameRequestId` dan `ReservationId` dari `AdmissionDal.cs`, `AdmissionDto.cs`, dan `CoordinatedCancellationRepo.cs`, sehingga lapisan persistensi admisi murni mengelola `InpatientOrderId`.

Depends On: P1-S01

Repository: `b09-bilreg-api`

Completion Criteria:
1. `SELECT_COLUMNS` pada `AdmissionDal.cs` tidak lagi memuat `aa.OpnameRequestId` dan `aa.ReservationId`.
2. Kueri `INSERT INTO BILRG_AdmAdmission` dan `UPDATE BILRG_AdmAdmission` pada `AdmissionDal.cs` tidak lagi memuat kolom dan parameter `@OpnameRequestId` serta `@ReservationId`.
3. `MapWriteParams` pada `AdmissionDal.cs` tidak lagi menyuplai `@OpnameRequestId` dan `@ReservationId`.
4. `AdmissionDto.cs` tidak lagi memiliki properti `OpnameRequestId` dan `ReservationId`.
5. Kueri `BILRG_AdmAdmission` dan record `AdmissionRow` pada `CoordinatedCancellationRepo.cs` bersih dari `OpnameRequestId` dan `ReservationId`, menggunakan `InpatientOrderId` sebagai referensi tunggal.
6. Proyek `Bilreg.Infrastructure` berhasil dikompilasi tanpa galat.

Implementation Notes:
- `AdmissionDal.cs`: Menghapus kolom warisan `aa.OpnameRequestId` dan `aa.ReservationId` dari `SELECT_COLUMNS`, kueri `INSERT`, kueri `UPDATE`, dan `MapWriteParams`.
- `AdmissionDto.cs`: Menghapus properti `OpnameRequestId` dan `ReservationId` dari record definition, `FromModel`, serta memetakan `"-"` pada `ToModel` untuk parameter konstruktor model warisan.
- `CoordinatedCancellationRepo.cs`: Membersihkan kueri `BILRG_AdmAdmission` dan record internal `AdmissionRow` dari `OpnameRequestId` dan `ReservationId`, menghapus `OpnameRow` dan `ReservationRow`, serta menyatukan pemulihan dan pembatalan sumber murni ke `InpatientOrderId`.
- Menyelaraskan konstruksi DTO dan tes terkait pada `AdmissionDalTest.cs`, `JourneyDalIntegrationTest.cs`, dan `JourneyDalVolumeTest.cs`.
- Seluruh proyek berhasil dikompilasi (`Bilreg.Infrastructure` dan `Bilreg.Test`) dengan 0 galat, serta 65 pengujian unit `AdmissionFeature` dan 36 pengujian unit `JourneyFeature` berhasil lulus 100%.

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/AdmissionDal.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/AdmissionDto.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/AdmissionFeature/CoordinatedCancellationRepo.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/AdmissionFeature/AdmissionDalTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/JourneyDalIntegrationTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/JourneyDalVolumeTest.cs`

Notes:
- Mengacu pada keputusan arsitektur TD-01 dan TD-02.
- Kolom fisik di basis data SQL Server tetap ada tanpa DDL DROP.

---

### P2-S03

Title: Pembersihan dan Penyelarasan Kueri Proyeksi Worklist di `JourneyDal.Batch.cs`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Membersihkan kueri `BuildAdmissionSelectSql` pada `JourneyDal.Batch.cs` dari pembacaan kolom warisan `a.OpnameRequestId` dan `a.ReservationId`, serta menggantikannya dengan proyeksi deterministik berbasis `BILRG_AdmInpatientOrder.OrderSource`.

Depends On: P2-S02

Repository: `b09-bilreg-api`

Completion Criteria:
1. `BuildAdmissionSelectSql` pada `JourneyDal.Batch.cs` melakukan LEFT JOIN ke `BILRG_AdmInpatientOrder io ON a.InpatientOrderId = io.InpatientOrderId AND io.VodDate = @VodDate`.
2. Proyeksi `OpnameRequestId` dan `ReservationId` pada kueri tersebut bersifat mutually exclusive berdasarkan `io.OrderSource` (1,2 untuk Opname; 3,4 untuk Reservasi), dan tidak lagi menggunakan klausa `COALESCE` ganda yang memasukkan nilai sama ke kedua field.
3. Klausul `WHERE` pada `BuildAdmissionSelectSql` menyaring `a.InpatientOrderId IN @OpnIds OR a.InpatientOrderId IN @RsvIds`, bukan kolom lama.

Implementation Notes:
- Menyelaraskan `BuildAdmissionSelectSql` di `JourneyDal.Batch.cs` dengan LEFT JOIN ke `BILRG_AdmInpatientOrder io ON a.InpatientOrderId = io.InpatientOrderId AND io.VodDate = @VodDate`.
- Mengganti proyeksi ganda `COALESCE` dengan ekspresi deterministik mutually exclusive `CASE WHEN ... io.OrderSource IN (1, 2)` untuk `OpnameRequestId` dan `CASE WHEN ... io.OrderSource IN (3, 4)` untuk `ReservationId`.
- Memperbarui filter `WHERE` agar menyaring murni `a.InpatientOrderId IN @OpnIds OR a.InpatientOrderId IN @RsvIds`, mengeliminasi pengecekan pada kolom warisan `a.OpnameRequestId` dan `a.ReservationId`.
- Menjadikan `BuildAdmissionSelectSql` bersifat `internal static` dan menambahkan unit test baru `JourneyDalBatchSelectSqlTest` yang memverifikasi ketiga completion criteria secara otomatis.

Changed Files:
- `src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs`
- `src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/JourneyDalBatchSelectSqlTest.cs`

Notes:
- Mengacu pada keputusan arsitektur TD-04.

---

## P3 - Journey Queries, Integrity Validation & Verification

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P3-S04

Title: Penyelarasan Kueri Detail Tunggal dan CTE `AdmissionRoots` pada `JourneyDal.cs`

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Menyelaraskan kueri pencarian admisi tunggal (`FindAdmissionByReservationAsync`, `FindAdmissionByOpnameAsync`, `LoadRegJourneyAsync`) dan CTE `AdmissionRoots` pada `JourneyDal.cs` agar murni mencari dan mengevaluasi `InpatientOrderId`.

Depends On: P2-S03

Repository: `b09-bilreg-api`

Completion Criteria:
1. Kueri `FindAdmissionByReservationAsync` mencari admisi dengan kriteria `WHERE a.VodDate = @VodDate AND a.InpatientOrderId = @ReservationId`, serta memproyeksikan `ReservationId = a.InpatientOrderId` dan `OpnameRequestId = '-'`.
2. Kueri `FindAdmissionByOpnameAsync` mencari admisi dengan kriteria `WHERE a.VodDate = @VodDate AND a.InpatientOrderId = @OpnameRequestId`, serta memproyeksikan `OpnameRequestId = a.InpatientOrderId` dan `ReservationId = '-'`.
3. Proyeksi JourneyId dan evaluasi `HasBlockingIntegrity` pada CTE `AdmissionRoots` dibersihkan dari pengecekan kolom lama `a.OpnameRequestId` dan `a.ReservationId`.

Implementation Notes:
- Menyelaraskan kueri utama dan fallback pada `FindAdmissionByReservationAsync` agar memfilter `a.InpatientOrderId = @ReservationId` dan memproyeksikan `ReservationId = a.InpatientOrderId`, `OpnameRequestId = '-'`.
- Menyelaraskan kueri utama dan fallback pada `FindAdmissionByOpnameAsync` agar memfilter `a.InpatientOrderId = @OpnameRequestId` dan memproyeksikan `OpnameRequestId = a.InpatientOrderId`, `ReservationId = '-'`.
- Menyelaraskan `LoadRegJourneyAsync` agar memproyeksikan sumber pesanan secara mutually exclusive menggunakan `io.OrderSource` dan `a.InpatientOrderId` tanpa memeriksa kolom warisan `a.OpnameRequestId` maupun `a.ReservationId`.
- Membersihkan CTE `AdmissionRoots`, `ProspectiveOpname`, dan `ProspectiveReservation` dari pembacaan kolom warisan `a.OpnameRequestId` dan `a.ReservationId`:
  - Mengganti `OpnameMulti` dan `ReservationMulti` dengan CTE `OrderMulti` tunggal berbasis `InpatientOrderId`.
  - Proyeksi `JourneyId` dan evaluasi `HasBlockingIntegrity` pada `AdmissionRoots` murni didasarkan pada `a.InpatientOrderId` dan `om.InpatientOrderId`.
- Menambahkan static builder method untuk kueri SQL terkait (`BuildFindAdmissionByOpnameSql`, `BuildFindAdmissionByOpnameFallbackSql`, `BuildFindAdmissionByReservationSql`, `BuildFindAdmissionByReservationFallbackSql`, `BuildLoadRegJourneySql`) serta menambahkan unit test komprehensif pada `JourneyDalSingleQueryAndCteTest.cs`.
- Seluruh 98 unit test `JourneyFeature` berhasil lulus 100%.

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/JourneyDalSingleQueryAndCteTest.cs`

Notes:
- Mengacu pada keputusan arsitektur TD-05.
- Kueri mematuhi standar kompatibilitas SQL Server 2012.

---

### P3-S05

Title: Penyelarasan Aturan Integritas pada `JourneyIntegrityValidator.cs`

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Memperbarui metode `ValidateAdmissionSources` pada `JourneyIntegrityValidator.cs` agar tidak menandai anomali `AdmissionHasBothSources` jika admisi berasal dari satu pesanan rawat inap terpadu, serta memvalidasi konsistensi derivasi `JourneyIdFactory.cs`.

Depends On: P3-S04

Repository: `b09-bilreg-api`

Completion Criteria:
1. `ValidateAdmissionSources` hanya memicu `AdmissionHasBothSources` jika `hasOpn && hasRsv && admission.OpnameRequestId != admission.ReservationId`.
2. Derivasi `JourneyIdFactory.Derive` mengembalikan ID yang konsisten dengan prefiks asalnya (`rsv:OPN...` / `opn:OPN...`) saat data admisi valid.
3. Unit test `JourneyIntegrityValidatorTest` dan `Release1JourneyStageResolverTest` lulus.

Notes:
- Mengacu pada keputusan arsitektur TD-06 dan TD-07.

---

### P3-S06

Title: Verifikasi End-to-End Test Suite dan Validasi Perilaku Operasional

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Mengeksekusi seluruh test suite otomatis di `Bilreg.Test` untuk memastikan seluruh komponen lulus tanpa regresi, serta memverifikasi bahwa skenario pendaftaran reservasi (seperti pasien SRI AJENG RETNO) menghasilkan tahap operasional `WardAcceptanceRequired` tanpa hang.

Depends On: P3-S05

Repository: `b09-bilreg-api`

Completion Criteria:
1. Solusi `b09-bilreg-api.sln` berhasil dikompilasi dengan 0 error dan 0 warning baru.
2. Seluruh unit test di `Bilreg.Test` (termasuk `InpatientOrderFeature`, `AdmissionFeature`, dan `JourneyFeature`) lulus 100%.
3. Terverifikasi bahwa pasien registrasi reservasi berstatus "Menunggu Diterima Bangsal", ID perjalanan bernilai `rsv:OPN...`, dan respons workspace siap dirender seketika.

Notes:
- Gerbang penyelesaian untuk menetapkan status rencana ke `COMPLETED`.

---

# 6. Change Log

- 2026-11-05: Initial creation of the implementation plan based on Architecture Version 1.1 (`ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-ARCHITECTURE.md`). Plan structure released with `Execution Approval: APPROVED`.
