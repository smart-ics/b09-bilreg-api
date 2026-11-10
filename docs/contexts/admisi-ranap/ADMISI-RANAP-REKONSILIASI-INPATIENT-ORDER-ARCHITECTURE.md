---
Title: Architecture - Penyatuan Relasi InpatientOrderId dan Pembersihan Total Kolom Warisan pada Admisi Ranap Journey
Code: ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER
Artifact: ARCHITECTURE
Version: 1.1
LastUpdated: 2026-11-05
---

# 1. Overview

Arsitektur ini mendefinisikan realisasi teknis untuk mengoreksi dua anomali sistem pada alur pemantauan perjalanan pasien rawat inap (*Admisi Ranap Journey*):
1. **Penguncian Status Rekonsiliasi Palsu**: Pasien yang berhasil didaftarkan dari reservasi/pemesanan rawat inap (contoh: "SRI AJENG RETNO", No. RM `00-00-60-63`, order `OPN0A31CDRTQ`) terkunci pada badge merah **"Perlu Pemeriksaan Data"** (`NeedsReconciliation`), padahal seharusnya berada pada tahap operasional **"Menunggu Diterima Bangsal"** (`WardAcceptanceRequired`).
2. **Infinite Skeleton Loading / Hang pada Frontend**: Panel detail perjalanan (*Journey Workspace*) di sisi kanan terus menampilkan *skeleton placeholder* tanpa henti saat kartu pasien diklik dari daftar worklist samping kiri.

Penyelesaian teknis dan arahan arsitektur:
- **Prinsip Skema vs Kode (*Physical DB Preserved, Code Base Fully Cleansed*)**:
  - **Tabel Basis Data**: Kolom fisik `OpnameRequestId` dan `ReservationId` pada tabel `BILRG_AdmAdmission` **tidak di-DROP** demi menjamin backwards compatibility data riwayat lama di level basis data.
  - **Codebase & DAL**: Seluruh lapisan kode C# (DAL, DTO, Repository, SQL queries, Domain Model) **dibersihkan secara total** dari referensi kolom warisan `OpnameRequestId` dan `ReservationId`. Kode tidak lagi membaca, menulis, memetakan, ataupun memvalidasi kedua kolom tersebut, dan beralih 100% ke `InpatientOrderId`.
- **Pembersihan Orkestrasi Pendaftaran Admisi**: Mengarahkan seluruh pembuatan pendaftaran rawat inap (`AdmissionModel.Admit`) agar menggunakan referensi pesanan tunggal `InpatientOrderId`.
- **Eliminasi Proyeksi Ganda Lapisan DAL**: Menghilangkan klausul SQL `COALESCE(InpatientOrderId)` ganda pada `JourneyDal.Batch.cs` dan `JourneyDal.cs`, serta memproyeksikan tipe sumber secara deterministik berdasarkan atribut `OrderSource` dari entitas `BILRG_AdmInpatientOrder`.
- **Penyelarasan Integritas Relasi Perjalanan**: Memastikan `JourneyIntegrityValidator.cs` mengevaluasi keabsahan admisi berdasarkan `InpatientOrderId` dan `OrderSource` tunggal, sehingga menghilangkan deteksi benturan palsu `AdmissionHasBothSources`.
- **Konsistensi Sinkronisasi Identitas Perjalanan (`JourneyId`)**: Menjamin konsistensi pembentukan JourneyId antara daftar worklist dan detail workspace (`rsv:OPN...` untuk reservasi/booking, `opn:OPN...` untuk permintaan opname), sehingga pencocokan ID di antarmuka web (`c012_myhospital_web`) berhasil dan transisi ke status `ready` terjadi seketika.

Originating Bug Issue:
- [ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-ISSUE.md) (`ISSUE-ADMISI-RANAP-REKONSILIASI-ORDER-001`)

---

# 2. Architectural Basis

## Business Context

- **Outcome Registrasi**: Memastikan pasien yang telah didaftarkan rawat inap dapat segera diproses penempatan tempat tidurnya oleh petugas bangsal tanpa hambatan peringatan integritas palsu.
- **Konsol Admisi Rawat Inap**: Petugas admisi mengandalkan panel daftar pasien aktif untuk memantau status antrean bangsal (`WaitingList`) dan mengeklik kartu untuk melihat detail akomodasi, penjamin, serta rujukan pasien.
- **Domain Context**:
  - `Bilreg.Domain.AdmisiRanapContext.AdmissionFeature`: Mengatur siklus hidup pendaftaran admisi rawat inap (`AdmissionModel`).
  - `Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature`: Mengatur konsolidasi seluruh pesanan rawat inap (baik dari rawat jalan, IGD, maupun booking admisi) melalui `InpatientOrderModel` dan `OrderSourceEnum`.
  - `Bilreg.Application.AdmisiRanapContext.JourneyFeature`: Mengatur proyeksi status operasional Release 1, validasi integritas relasi, dan derivasi `JourneyId`.

## Analysis Input

- **BUG-INVESTIGATION**: [ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-INVESTIGATION.md)  
  Temuan analisis yang menjadi dasar realisasi teknis:
  1. *Dual-Source Mapping Collision*: Proyeksi `COALESCE(NULLIF(a.InpatientOrderId,'-'), a.OpnameRequestId)` dan `COALESCE(NULLIF(a.InpatientOrderId,'-'), a.ReservationId)` memetakan satu order ke kedua atribut sumber, memicu pelanggaran `AdmissionHasBothSources` pada `JourneyIntegrityValidator.cs`.
  2. *State Lockdown*: Adanya *blocking integrity issue* memaksa `Release1JourneyStageResolver` memilih `NeedsReconciliation` ("Perlu Pemeriksaan Data"), memotong evaluasi status antrean bangsal `Waiting`.
  3. *JourneyId Mismatch*: Karena anomali integritas, backend mengaktifkan fallback ID menjadi `reg:RG...`, sedangkan kartu di worklist menggunakan `rsv:OPN...`. Pengecekan `data.journeyId === selectedJourneyId` pada `useJourneyQueryState.ts` menghasilkan `false`, menjebak UI dalam status `{ kind: 'loading' }` (skeleton hang).
  4. *Schema Preparedness*: Tabel `BILRG_AdmAdmission` telah memiliki kolom `InpatientOrderId` dan indeks non-terklaster `IX_bilrg_admadmission_inpatientorderid`.

```text
ISSUE (ISSUE-ADMISI-RANAP-REKONSILIASI-ORDER-001)
        +
BUG-INVESTIGATION (ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-BUG-INVESTIGATION)
        ↓
    ARCHITECTURE (ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-ARCHITECTURE)
```

---

# 3. Scope

## Included

1. **Backend - Pembersihan Penuh Lapisan DAL Admisi (`AdmissionDal.cs`, `AdmissionDto.cs`)**:
   - Menghapus kolom `OpnameRequestId` dan `ReservationId` dari `SELECT_COLUMNS`, `INSERT INTO BILRG_AdmAdmission`, `UPDATE BILRG_AdmAdmission`, dan `MapWriteParams`.
   - Mengalihkan penulisan dan pembacaan murni ke kolom `InpatientOrderId`.
   - Menghapus atribut `OpnameRequestId` dan `ReservationId` dari DTO `AdmissionDto`.
2. **Backend - Pembersihan Repositori Pembatalan Terkoordinasi (`CoordinatedCancellationRepo.cs`)**:
   - Menghapus kolom `OpnameRequestId` dan `ReservationId` dari kueri `BILRG_AdmAdmission WITH (UPDLOCK,HOLDLOCK)` dan record internal `AdmissionRow`, serta menggunakan murni `InpatientOrderId`.
3. **Backend - Orkestrasi Pendaftaran Admisi (`AdmissionRegistrationOrchestrator.cs`)**:
   - Pemutakhiran alur pendaftaran admisi (`ProcessReservation`, `ProcessOpnameRequest`, `ProcessInpatientOrder`) agar memanggil overload `AdmissionModel.Admit` murni dengan `inpatientOrderId`.
4. **Backend - Pembersihan dan Penyelarasan Kueri Perjalanan (`JourneyDal.Batch.cs`, `JourneyDal.cs`)**:
   - Menghapus pembacaan `a.OpnameRequestId` dan `a.ReservationId` pada `BuildAdmissionSelectSql`.
   - Menggabungkan data admisi dengan `BILRG_AdmInpatientOrder` via `a.InpatientOrderId = io.InpatientOrderId` untuk memproyeksikan tipe sumber secara deterministik berdasarkan `io.OrderSource`.
   - Menghapus proyeksi ganda pada `FindAdmissionByOpnameAsync` dan `FindAdmissionByReservationAsync`.
   - Membersihkan CTE `AdmissionRoots` dari evaluasi `OpnameRequestId` dan `ReservationId`.
5. **Backend - Validasi Integritas Perjalanan (`JourneyIntegrityValidator.cs`, `JourneyIdFactory.cs`)**:
   - Menghapus deteksi benturan `AdmissionHasBothSources` yang dipicu oleh pemetaan ganda ID pesanan.
   - Menjamin derivasi `JourneyId` deterministik (`rsv:OPN...` untuk reservasi/booking, `opn:OPN...` untuk permintaan opname).
6. **Verifikasi Pengujian Regresi (`b09-bilreg-api`)**:
   - Pemutakhiran unit test pada `AdmissionRegistrationOrchestratorTest`, `AdmissionDalTest`, `JourneyIntegrityValidatorTest`, dan `Release1JourneyStageResolverTest`.

## Excluded

- **DDL DROP COLUMN pada Basis Data**: Kolom fisik `OpnameRequestId` dan `ReservationId` pada tabel `BILRG_AdmAdmission` di basis data SQL Server **tetap dibiarkan ada** (tidak di-drop DDL) demi keselamatan skema dan integritas data riwayat.
- **Modifikasi Frontend (`c012_myhospital_web`)**: Tidak ada modifikasi kode pada antarmuka web karena koreksi ID yang konsisten dari backend secara otomatis mengembalikan fungsionalitas UI.

---

# 4. Technical Decisions

### TD-01: Pembersihan Total `OpnameRequestId` dan `ReservationId` pada `AdmissionDal.cs` & `AdmissionDto.cs`
- **Keputusan**: Seluruh operasi CRUD pada `AdmissionDal.cs` dibersihkan dari kolom `OpnameRequestId` dan `ReservationId`.
- **Implementasi**:
  1. `SELECT_COLUMNS`:
     ```sql
     aa.RegId, aa.AdmissionStatus, aa.AdmissionSource, aa.PasienId,
     ISNULL(bb.fs_nm_pasien, '') AS PasienName,
     ISNULL(bb.fs_jns_kelamin, '') AS Gender,
     aa.KelasDkId, aa.KelasDkName, aa.BangsalId, aa.BangsalName, aa.AdmissionDate,
     aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
     ISNULL(aa.InpatientOrderId, '-') AS InpatientOrderId
     ```
  2. `INSERT`:
     ```sql
     INSERT INTO BILRG_AdmAdmission (
         RegId, AdmissionStatus, AdmissionSource,
         PasienId, InpatientOrderId,
         KelasDkId, KelasDkName, BangsalId, BangsalName, AdmissionDate,
         CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
     VALUES (
         @RegId, @AdmissionStatus, @AdmissionSource,
         @PasienId, @InpatientOrderId,
         @KelasDkId, @KelasDkName, @BangsalId, @BangsalName, @AdmissionDate,
         @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
     ```
     *(Catatan: Kolom fisik `OpnameRequestId` dan `ReservationId` di DB secara otomatis terisi nilai DEFAULT `'-'` atau NULL).*
  3. `UPDATE`: Menghapus klausa `OpnameRequestId = @OpnameRequestId` dan `ReservationId = @ReservationId`.
  4. `AdmissionDto.cs`: Menghapus properti `OpnameRequestId` dan `ReservationId`.

### TD-02: Pembersihan `CoordinatedCancellationRepo.cs`
- **Keputusan**: Pada alur pembatalan terkoordinasi admisi:
  - Kueri `SELECT`: Mengambil `RegId, AdmissionStatus, AdmissionSource, ISNULL(InpatientOrderId,'-') AS InpatientOrderId, UpdDate`.
  - Record internal `AdmissionRow`: Mengeliminasi field `OpnameRequestId` dan `ReservationId`.
  - Penentuan ID sumber: Menggunakan murni `InpatientOrderId` jika tidak bernilai `'-'`.

### TD-03: Migrasi Penuh Pembuatan Admisi di `AdmissionRegistrationOrchestrator.cs`
- **Keputusan**: Menghapus pengisian parameter `opnameRequestId` dan `reservationId` pada `AdmissionRegistrationOrchestrator.cs`.
- **Implementasi**:
  ```csharp
  // Menggunakan overload bersih:
  var admission = CreateAdmission(
      order.Pasien,
      request.KelasDkId,
      request.BangsalId,
      opnameRequestId: null,
      reservationId: null,
      request.UserId,
      occurredAt,
      bangsal,
      inpatientOrderId: order.InpatientOrderId);
  ```
  Di domain model `AdmissionModel`, entitas baru mencatat referensi pesanan murni melalui `InpatientOrderId`.

### TD-04: Eliminasi Proyeksi Ganda dan Pembersihan Kueri di `JourneyDal.Batch.cs`
- **Keputusan**: Kueri `BuildAdmissionSelectSql` dibersihkan dari pembacaan `a.OpnameRequestId` dan `a.ReservationId`. Penentuan sumber dilakukan murni via JOIN ke `BILRG_AdmInpatientOrder`:
- **Implementasi**:
  ```sql
  SELECT a.RegId, a.AdmissionStatus, a.PasienId,
         CASE 
             WHEN a.InpatientOrderId NOT IN ('','-') AND io.OrderSource IN (1, 2) THEN a.InpatientOrderId
             ELSE '-'
         END AS OpnameRequestId,
         CASE 
             WHEN a.InpatientOrderId NOT IN ('','-') AND io.OrderSource IN (3, 4) THEN a.InpatientOrderId
             ELSE '-'
         END AS ReservationId,
         a.KelasDkId, a.KelasDkName, a.BangsalId, a.BangsalName, a.AdmissionDate, a.CrtDate, a.UpdDate,
         ISNULL(mr.fs_nm_pasien,'') AS PasienName,
         ISNULL(mr.fs_jns_kelamin,'') AS Gender,
         CASE WHEN ISDATE(mr.fd_tgl_lahir)=1 THEN CAST(mr.fd_tgl_lahir AS DATETIME) ELSE NULL END AS TglLahir
  FROM BILRG_AdmAdmission a
  LEFT JOIN tc_mr mr ON a.PasienId = mr.fs_mr
  LEFT JOIN BILRG_AdmInpatientOrder io ON a.InpatientOrderId = io.InpatientOrderId AND io.VodDate = @VodDate
  WHERE a.VodDate = @VodDate
    AND (
          a.RegId IN @RegIds
          OR a.InpatientOrderId IN @OpnIds
          OR a.InpatientOrderId IN @RsvIds
        )
  ```
  Kueri ini memastikan `OpnameRequestId` dan `ReservationId` pada DTO hasil kueri terisi secara mutually exclusive sesuai `OrderSource`.

### TD-05: Penyelarasan Kueri Detail Tunggal pada `JourneyDal.cs`
- **Keputusan**: Pada `FindAdmissionByOpnameAsync` dan `FindAdmissionByReservationAsync`, kueri hanya mencari berdasarkan `a.InpatientOrderId`:
  - `FindAdmissionByReservationAsync`:
    `WHERE a.VodDate = @VodDate AND a.InpatientOrderId = @ReservationId`
    `ReservationId` diproyeksikan dari `a.InpatientOrderId`, dan `OpnameRequestId` disetel `'-'`.
  - `FindAdmissionByOpnameAsync`:
    `WHERE a.VodDate = @VodDate AND a.InpatientOrderId = @OpnameRequestId`
    `OpnameRequestId` diproyeksikan dari `a.InpatientOrderId`, dan `ReservationId` disetel `'-'`.
  - Pada CTE `AdmissionRoots`: Membersihkan klausa pengecekan kolom lama `a.OpnameRequestId` dan `a.ReservationId`, serta mendasarkan identitas perjalanan dan integritas pada `a.InpatientOrderId`.

### TD-06: Penyelarasan Aturan Integritas pada `JourneyIntegrityValidator.cs`
- **Keputusan**: Memperbarui `ValidateAdmissionSources` agar hanya menandai `AdmissionHasBothSources` jika terdapat dua referensi sumber yang benar-benar berbeda:
  ```csharp
  var hasOpn = JourneyIdFactory.HasValue(admission.OpnameRequestId);
  var hasRsv = JourneyIdFactory.HasValue(admission.ReservationId);
  if (hasOpn && hasRsv && admission.OpnameRequestId != admission.ReservationId)
  {
      issues.Add(Issue(
          JourneyReconciliationCodes.AdmissionHasBothSources,
          $"Admission {admission.RegId} references both an Opname Request and a Reservation.",
          [admission.RegId, admission.OpnameRequestId, admission.ReservationId]));
  }
  ```
  Karena DTO hasil kueri kini eksklusif berdasarkan `OrderSource`, kondisi ini tidak akan pernah terpenuhi secara palsu.

### TD-07: Konsistensi Sinkronisasi `JourneyId` dan Solusi Hang di Frontend
- **Keputusan**: Mengeliminasi anomali fallback `reg:RG...` pada data yang sah.
- **Hasil**: Respons detail `GET /api/admisi-ranap/journeys/rsv:{InpatientOrderId}` tetap membawa `journeyId: "rsv:{InpatientOrderId}"`. Pada frontend (`useJourneyQueryState.ts`), kondisi `data.journeyId === selectedJourneyId.value` bernilai `true`, sehingga status UI beralih seketika ke `ready` dan merender panel workspace tanpa *hang*.

---

# 5. Component Responsibilities

| Komponen | Tanggung Jawab Teknis |
|---|---|
| `AdmissionModel` (`Bilreg.Domain`) | Menampung entitas pendaftaran rawat inap dengan referensi tunggal `InpatientOrderId` dan mengeliminasi ketergantungan pada kolom warisan. |
| `AdmissionRegistrationOrchestrator` (`Bilreg.Application`) | Mengorkestrasi pendaftaran admisi dengan menyuplai `InpatientOrderId` murni. |
| `AdmissionDal` (`Bilreg.Infrastructure`) | Menyimpan dan membaca `InpatientOrderId` pada tabel `BILRG_AdmAdmission` tanpa lagi menyentuh kolom `OpnameRequestId` dan `ReservationId`. |
| `CoordinatedCancellationRepo` (`Bilreg.Infrastructure`) | Menangani pembatalan terkoordinasi admisi berbasis `InpatientOrderId` murni. |
| `JourneyDal.Batch` (`Bilreg.Infrastructure`) | Menjalankan proyeksi batch worklist dengan JOIN ke `BILRG_AdmInpatientOrder` untuk menentukan asal pesanan secara deterministik. |
| `JourneyDal` (`Bilreg.Infrastructure`) | Menjalankan kueri fakta detail dan derivasi workspace secara asinkron tanpa proyeksi benturan ganda. |
| `JourneyIntegrityValidator` (`Bilreg.Application`) | Memvalidasi integritas relasi Release 1 tanpa memicu *false-positive* pada entitas berbasis `InpatientOrderId`. |
| `JourneyIdFactory` (`Bilreg.Application`) | Memastikan konsistensi pembentukan JourneyId antara worklist dan detail workspace. |

---

# 6. Integration Design

```text
[Frontend: AdmisiRanapConsole.vue]
        │
        ├─ (1) GET /api/admisi-ranap/journeys?scope=active
        │      │
        │      ▼
        │   [JourneyDal.Batch.cs: BuildAdmissionSelectSql]
        │      │ (JOIN BILRG_AdmAdmission + BILRG_AdmInpatientOrder via InpatientOrderId)
        │      │ (Proyeksi bersih: OrderSource 3/4 -> ReservationId='OPN...', OpnameRequestId='-')
        │      ▼
        │   [Release1JourneyStageResolver: DecideStage]
        │      │ (HasBlockingIntegrity = false, ActiveWaitingListStatus = Waiting)
        │      ▼
        │   Mengembalikan Card Worklist: JourneyId = "rsv:OPN0A31CDRTQ", Stage = WardAcceptanceRequired ("Menunggu Diterima Bangsal")
        │
        ├─ (2) User mengklik card (selectedJourneyId = "rsv:OPN0A31CDRTQ")
        │      │
        │      ▼
        │   [GET /api/admisi-ranap/journeys/rsv%3AOPN0A31CDRTQ]
        │      │
        │      ▼
        │   [JourneyDal.cs: LoadReservationJourneyAsync]
        │      │ (FindAdmissionByReservationAsync menemukan RegId via InpatientOrderId)
        │      │ (AssembleAdmissionBag memvalidasi integritas: VALID / No Issues)
        │      ▼
        │   Mengembalikan Detail: journeyId = "rsv:OPN0A31CDRTQ", stage = WardAcceptanceRequired
        │
        ▼
[Frontend: useJourneyQueryState.ts]
        │ (data.journeyId === selectedJourneyId -> TRUE)
        ▼
[Frontend: JourneyWorkspace.vue]
        Render langsung detail pasien, tombol serah terima bangsal, dan form akomodasi (Zero Hang!)
```

---

# 7. Data Ownership

| Entitas Data | Pemilik (*Owner*) | Keterangan Relasi |
|---|---|---|
| `BILRG_AdmInpatientOrder` | `InpatientOrderFeature` | Sumber kebenaran tunggal (*Single Source of Truth*) pesanan rawat inap dengan atribut `OrderSource` (1,2: Opname; 3,4: Reservasi). |
| `BILRG_AdmAdmission` | `AdmissionFeature` | Menyimpan transaksi pendaftaran admisi dengan foreign reference `InpatientOrderId` menuju `BILRG_AdmInpatientOrder`. |
| `BILRG_BedWaitingList` | `WaitingListFeature` | Menyimpan antrean tempat tidur bangsal yang terhubung via `RegId`. |
| `JourneyFactBag` & `JourneyDetailWorkspace` | `JourneyFeature` | Model proyeksi agregat yang merangkum fakta admisi, pesanan, dan antrean untuk presentasi UI. |

---

# 8. Database Design

## New Tables
*(Tidak ada tabel baru)*

## Modified Tables

| Tabel | Modifikasi Arsitektural |
|---|---|
| `BILRG_AdmAdmission` | - **Kolom Aktif Utama**: `InpatientOrderId VARCHAR(12) NOT NULL` (didukung indeks non-terklaster `IX_bilrg_admadmission_inpatientorderid`).<br>- **Kebijakan Kolom Fisik**: Kolom fisik `OpnameRequestId` dan `ReservationId` **tetap dipertahankan di basis data** (tidak di-drop DDL) demi keamanan riwayat data.<br>- **Pembersihan Lapisan Kode**: Seluruh kueri DAL (Insert, Update, Select) pada aplikasi dibersihkan 100% dari kolom `OpnameRequestId` dan `ReservationId`. |

## Relationships
- `BILRG_AdmAdmission.InpatientOrderId` $\rightarrow$ `BILRG_AdmInpatientOrder.InpatientOrderId` (Relasi 1-ke-1 saat pesanan terealisasi / `Fulfilled`).

## Migration Considerations
- Skema kolom dan indeks fisik telah tersedia melalui migrasi [BILRG_AdmAdmission_M3_InpatientOrderId_Alter.sql](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/AdmisiRanapContext/AdmissionFeature/BILRG_AdmAdmission_M3_InpatientOrderId_Alter.sql).
- Tidak diperlukan migrasi skema DDL baru.

---

# 9. Cross-Cutting Concerns

- **Performa Kueri**: Menggunakan indeks terarah `IX_bilrg_admadmission_inpatientorderid` (`InpatientOrderId, VodDate, RegId`) dan `IX_bilrg_adminpatientorder_pk` memastikan kueri `JOIN` dan pencarian detail admisi dieksekusi via *Index Seek* (< 2 ms).
- **Audit Trail**: Pembuatan dan perubahan status admisi tetap mencatat jejak audit `AuditTrailType` pengguna dan stempel waktu UTC/lokal secara konsisten.
- **Integritas Release 1**: Aturan integritas tetap aktif dan tegas mencegah inkonsistensi data riil, namun tidak lagi memblokir aliran data yang sah akibat kesalahan proyeksi DTO.

---

# 10. Implementation Constraints

1. **Physical DB Preserved, Code Base Fully Cleansed**: Kolom fisik di tabel database tidak di-DROP, tetapi DAL dan seluruh lapisan kode C# wajib bersih total dari referensi `ReservationId` dan `OpnameRequestId`.
2. **Kompatibilitas Mutlak SQL Server 2012 (Compatibility Level 110)**:
   Karena terdapat basis data klien yang masih beroperasi pada **SQL Server 2012**, seluruh kueri T-SQL pada lapisan DAL dilarang keras menggunakan fitur, sintaks, maupun fungsi yang baru diperkenalkan pada SQL Server versi di atasnya (2016, 2017, 2019, 2022).
   - **Dilarang**: `STRING_SPLIT`, `STRING_AGG`, `JSON_VALUE`, `OPENJSON`, fungsi `TRIM()` bawaan SQL 2017, `CONCAT_WS`, `DROP TABLE IF EXISTS`, `CREATE OR ALTER`.
   - **Wajib Digunakan**: Fungsi T-SQL standar SQL Server 2012 seperti `LTRIM(RTRIM(...))`, `CASE WHEN ... THEN ... ELSE ... END`, `ISNULL(...)`, `COALESCE(...)`, dan `LEFT JOIN` standar.
3. **Dapper & Parameterized SQL Only**: Seluruh kueri DAL menggunakan Dapper dengan parameterized SQL (`DynamicParameters` / anonymous object).
4. **Preservasi Logika Asinkron**: Seluruh metode di `JourneyDal` dan MediatR handler wajib mempertahankan pemanggilan non-blocking (`async/await`, `OpenAsync`, `QueryAsync`, `QueryMultipleAsync`).
5. **Zero Regressions pada Unit Test**: Seluruh pengujian pada test suite `Bilreg.Test` yang relevan dengan pendaftaran admisi dan tahap perjalanan harus lulus 100%.

---

# 11. Acceptance Conditions

1. **Status Worklist Benar**: Pasien yang didaftarkan melalui alur reservasi (seperti "SRI AJENG RETNO") tampil di panel kiri worklist dengan badge status **"Menunggu Diterima Bangsal"** (`WardAcceptanceRequired`), bukan badge merah "Perlu Pemeriksaan Data".
2. **Eliminasi Infinite Loading / Hang**: Saat kartu pasien hasil reservasi diklik di daftar worklist, panel kanan (*Journey Workspace*) menampilkan data detail perjalanan pasien secara responsif (< 1 detik) tanpa tertahan pada *skeleton loading*.
3. **Konsistensi Derivasi ID**: Nilai `detail.JourneyId` yang dikembalikan dari endpoint `GET /api/admisi-ranap/journeys/rsv:{InpatientOrderId}` bernilai persis `rsv:{InpatientOrderId}`.
4. **Pembersihan Kode Sumber**: Seluruh berkas DAL dan DTO (`AdmissionDal.cs`, `AdmissionDto.cs`, `CoordinatedCancellationRepo.cs`, `JourneyDal.cs`, `JourneyDal.Batch.cs`) tidak lagi memuat referensi atau parameter ke kolom `OpnameRequestId` dan `ReservationId`.
5. **Verifikasi Pengujian Otomatis**: Unit test pada `AdmissionRegistrationOrchestratorTest`, `AdmissionDalTest`, dan `JourneyIntegrityValidatorTest` berjalan sukses tanpa galat kompilasi atau regresi fungsional.
