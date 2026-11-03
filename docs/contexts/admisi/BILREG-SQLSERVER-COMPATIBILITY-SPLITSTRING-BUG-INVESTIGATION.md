# BUG-INVESTIGATION

## Context

Issue: [BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-BUG-ISSUE.md) (`ISSUE-BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-001`)  
Problem Summary: Terjadi kegagalan eksekusi kueri basis data pada lingkungan klien yang menggunakan Microsoft SQL Server versi sebelum 2016 (seperti SQL Server 2008, 2008 R2, 2012, dan 2014) karena kueri di layer infrastruktur `b09-bilreg-api` memanggil fungsi bawaan `STRING_SPLIT` yang belum tersedia pada versi-versi tersebut.

## Current State

Saat ini terdapat 3 (tiga) komponen pada layer infrastruktur di `b09-bilreg-api` yang menggunakan fungsi bawaan T-SQL `STRING_SPLIT`:

1. **Antrian Loket Admisi Rawat Jalan (`AdmissionQueueOperationalProjection.cs`)**:
   - Berkas: `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AdmissionQueueOperationalProjection.cs`
   - Kueri `ListWorklistPage` memfilter kolom `ServicePointCode` dengan memecah string parameter `@ServicePointIds`:
     ```sql
     AND (@ServicePointIds IS NULL OR q.ServicePointCode IN (
         SELECT value FROM STRING_SPLIT(@ServicePointIds, ',')))
     ```
   - Filter ini dieksekusi dua kali (pada `pageSql` dan `countSql`).

2. **Pencarian Riwayat Registrasi Pasien Rawat Jalan (`RegistrationHistoryReader.cs`)**:
   - Berkas: `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegistrationHistoryReader.cs`
   - Kueri `FindByPatientIds` dan `FindByRegistrationIds` memecah parameter CSV `@patientIdsCsv` dan `@registrationIdsCsv`:
     ```sql
     AND reg.fs_mr IN (
         SELECT LTRIM(RTRIM(value))
         FROM STRING_SPLIT(@patientIdsCsv, ',')
         WHERE LTRIM(RTRIM(value)) <> ''
     )
     ```
     dan
     ```sql
     AND reg.fs_kd_reg IN (
         SELECT LTRIM(RTRIM(value))
         FROM STRING_SPLIT(@registrationIdsCsv, ',')
         WHERE LTRIM(RTRIM(value)) <> ''
     )
     ```

3. **Pemuatan Batch Data Tambahan Perjalanan Pasien Rawat Inap (`JourneyDal.Batch.cs`)**:
   - Berkas: `src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs`
   - Kueri `BuildRegExtrasSelectSql` memecah parameter CSV `@RegIdCsv`:
     ```sql
     FROM (SELECT DISTINCT LTRIM(RTRIM(value)) AS RegId FROM STRING_SPLIT(@RegIdCsv, ',') WHERE LTRIM(RTRIM(value)) <> '') x
     LEFT JOIN ta_registrasi reg ON reg.fs_kd_reg = x.RegId
     ...
     AND h.fs_kd_reg IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@RegIdCsv, ',') WHERE LTRIM(RTRIM(value)) <> '')
     ```

Ketika aplikasi terhubung ke instance basis data dengan tingkat kompatibilitas di bawah SQL Server 2016, eksekusi kueri langsung dibatalkan oleh database engine dengan pesan kesalahan:
`Msg 195, Level 15, State 10: 'STRING_SPLIT' is not a recognized built-in function name.`

## Problem Analysis

### 1. Ketergantungan Versi Microsoft SQL Server
Fungsi `STRING_SPLIT` diperkenalkan pada SQL Server 2016 (13.x) dan membutuhkan database compatibility level minimal 130. Banyak rumah sakit klien yang masih menggunakan SQL Server 2008 R2, 2012, atau 2014 karena batasan lisensi atau kompatibilitas aplikasi SIMRS legacy.

### 2. Batasan Parameter SQL Server (Maksimal 2.100 Parameter)
Pada `RegistrationHistoryReaderTest.cs`, terdapat unit test eksplisit:
- `CreatePatientIdParameters_2600Ids_UsesTwoSqlParameters()`
- `CreateRegistrationIdParameters_2600Ids_UsesOneSqlParameter()`

Pengujian ini sengaja menguji pengiriman hingga **2.600 ID sekaligus** dalam bentuk 1 parameter CSV. SQL Server membatasi jumlah parameter per query maksimal **2.100 parameter**. Jika string CSV diubah menjadi `IN @List` biasa pada Dapper, Dapper akan membuat 2.600 parameter individual (`@p0, @p1, ... @p2599`), yang akan memicu eksepsi SQL Server:
`The incoming request has too many parameters. The server supports a maximum of 2100 parameters.`
Oleh karena itu, untuk `RegistrationHistoryReader`, pengiriman data harus tetap menggunakan mekanisme 1 parameter CSV / teks tunggal.

### 3. Kebutuhan Performa & Karakteristik Data per Komponen
- **`AdmissionQueueOperationalProjection`**: Jumlah loket/service point yang difilter oleh seorang petugas admisi berkisar antara 1 s.d. 10 ID. Volume sangat kecil.
- **`JourneyDal.Batch`**: Jumlah registrasi ranap dalam satu halaman pemuatan data (worklist/journey page) berkisar antara 10 s.d. 50 ID. Volume relatif kecil.
- **`RegistrationHistoryReader`**: Pencarian riwayat registrasi dapat menerima ratusan hingga ribuan nomor rekam medis saat pencarian masal dijalankan.

## Affected Components

- **Modul/Konteks Backend (`b09-bilreg-api`)**:
  - `AdmisiContext/AntrianFeature` (`AdmissionQueueOperationalProjection.cs`)
  - `AdmisiContext/RegFeature` (`RegistrationHistoryReader.cs`)
  - `AdmisiRanapContext/JourneyFeature` (`JourneyDal.Batch.cs`)
- **Unit Test**:
  - `Bilreg.Test/AdmisiContext/RegFeature/RegistrationHistoryReaderTest.cs`
- **Frontend & Service Lain**:
  - Tidak ada komponen frontend (`c012_myhospital_web`) atau backend lain (`a005`, `a043`, `a045`, `a054`, `b12`, `b21`) yang terpengaruh, karena kontrak API dan model transfer data tidak berubah.

## Impact Assessment

- **Business Impact**: Klien rumah sakit yang menggunakan SQL Server 2008 s.d. 2014 mengalami kegagalan operasional pada modul pendaftaran pasien rawat jalan, monitoring antrian loket, dan admisi rawat inap.
- **Operational Impact**: Tanpa perbaikan ini, rumah sakit terpaksa dituntut melakukan upgrade lisensi dan server basis data yang berisiko tinggi dan memakan biaya besar.
- **Technical Impact**: Terjadi runtime crash pada Dapper/ADO.NET saat mengeksekusi kueri yang mengandung keyword `STRING_SPLIT`.

## Assumptions

1. Klien rumah sakit tidak diwajibkan melakukan upgrade versi SQL Server ke 2016+.
2. Skema basis data klien tidak boleh diwajibkan menjalankan DDL eksternal (seperti pembuatan *User-Defined Function* manual) bila dapat diselesaikan murni di level aplikasi kueri.
3. Batasan 2.100 parameter SQL Server harus tetap dihormati pada pencarian pasien massal.

## Open Questions

- *Tidak ada open question yang memblokir.* Karakteristik data dan limitasi arsitektur basis data sudah terpetakan secara tuntas.

## Alternative Evaluation

### Alternatif 1: Pembuatan User-Defined Function (`dbo.fn_SplitString`) di Database Klien
- **Kelebihan**: Sintaks kueri di C# tetap bergaya pemanggilan fungsi tabel.
- **Kekurangan**: Membutuhkan deployment script DDL ke basis data setiap rumah sakit klien. Rentan terjadi kesalahan operasional jika fungsi belum dibuat pada salah satu database klien.

### Alternatif 2: Dapper Native Parameter (`IN @List`) untuk Seluruh Kueri
- **Kelebihan**: Bersih di kode C#, memanfaatkan fitur ekspansi Dapper, dan performa seek indeks optimal.
- **Kekurangan**: Akan gagal total (*crash*) pada `RegistrationHistoryReader` saat jumlah ID melebihi 2.100 entri, serta merusak unit test `RegistrationHistoryReaderTest.cs` yang memvalidasi efisiensi parameter CSV.

### Alternatif 3: Pendekatan Terarah (Hybrid / Fit-for-Purpose) — *Direkomendasikan*
- Pada **`AdmissionQueueOperationalProjection`** dan **`JourneyDal.Batch`**: Menggunakan parameter koleksi native Dapper (`IN @ServicePointIds` dan `IN @RegIds`). Karena jumlah entitas pada kedua fitur ini dijamin kecil (< 100 entitas), pemanfaatan Dapper parameterized menghasilkan eksekusi index seek paling cepat (< 1 ms) tanpa overhead string parsing.
- Pada **`RegistrationHistoryReader`**: Menggunakan teknik **Inline XML Parsing** (`CAST('<x>' + REPLACE(...) + '</x>' AS XML)` dengan `.nodes('/x')`). Mekanisme ini:
  - Berjalan lancar di SQL Server 2008, 2012, 2014, dan 2016+.
  - Tetap menggunakan **1 parameter SQL tunggal** (`@patientIdsCsv` / `@registrationIdsCsv`), sehingga aman dari batas 2.100 parameter.
  - Tidak memerlukan pembuatan fungsi atau objek baru di basis data klien.
  - Mempertahankan kelulusan unit test eksisting 100%.

## Recommended Decision

Mengadopsi **Alternatif 3 (Pendekatan Terarah / Hybrid)**:
1. Memodifikasi kueri antrian loket dan batch ranap untuk memanfaatkan parameter list Dapper secara langsung.
2. Memodifikasi kueri pencarian riwayat registrasi pasien menggunakan in-memory XML shredding guna menjaga batas parameter dan kompatibilitas versi SQL Server.

## Decision

Menyetujui penerapan perbaikan kueri backward-compatible tanpa ketergantungan versi basis data dan tanpa DDL basis data:
1. Menghilangkan `STRING_SPLIT` pada `AdmissionQueueOperationalProjection.cs` dan beralih ke parameter list Dapper `IN @ServicePointIds`.
2. Menghilangkan `STRING_SPLIT` pada `JourneyDal.Batch.cs` dan beralih ke parameter list Dapper `IN @RegIds`.
3. Menghilangkan `STRING_SPLIT` pada `RegistrationHistoryReader.cs` dan beralih ke inline XML nodes extraction dengan tetap mempertahankan parameter tunggal CSV.

## Decision Rationale

- **Kompatibilitas Penuh**: Dapat berjalan di seluruh instalasi SQL Server klien (SQL Server 2008 s.d. 2022) tanpa prasyarat versi tertentu.
- **Tanpa Intervensi DDL**: Tidak memerlukan eksekusi skrip `CREATE FUNCTION` pada basis data klien saat rilis/deployment.
- **Keamanan Skalabilitas Parameter**: Menghindarkan pencarian massal dari kegagalan limit 2.100 parameter SQL Server.
- **Performa Terjaga**: Mempertahankan indeks seek pada filter loket dan batch ranap, serta memastikan latensi pencarian pasien tetap responsif (< 50 ms untuk volume masal).

## Architecture Applicability

### Decision

**ARCHITECTURE-NOT-REQUIRED**

### Rationale

Perubahan ini tidak memperkenalkan aggregate baru, domain event baru, antarmuka service baru, perubahan kontrak API, maupun perubahan kepemilikan data. Perbaikan terisolasi sepenuhnya di dalam implementasi kueri internal pada layer infrastruktur Dapper (`Bilreg.Infrastructure`) untuk mengoreksi sintaks SQL agar kompatibel dengan lingkungan SQL Server target. Struktur teknis dan spesifikasi pada berkas investigasi ini sudah memadai untuk langsung ditindaklanjuti ke perencanaan implementasi (`IMPLEMENTATION-PLAN`).
