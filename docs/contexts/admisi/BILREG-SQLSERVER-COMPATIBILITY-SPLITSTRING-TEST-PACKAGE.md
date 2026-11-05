---
Title: Test Package - Eliminasi STRING_SPLIT untuk Kompatibilitas Microsoft SQL Server Versi di Bawah 2016
Code: TEST-BILREG-SQLSERVER-SPLITSTRING-001
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-11-03
Status: READY-FOR-EXECUTION
Target Plan: BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-IMPLEMENTATION-PLAN.md
---

# 1. Overview & Objective

Dokumen ini merupakan **TEST-PACKAGE** resmi untuk memverifikasi penghapusan ketergantungan pada fungsi bawaan T-SQL `STRING_SPLIT` pada repositori backend **`b09-bilreg-api`**.

### Latar Belakang Masalah
Fungsi `STRING_SPLIT` baru diperkenalkan pada Microsoft SQL Server 2016 (Database Compatibility Level 130). Ketika modul admisi dan billing registrasi dijalankan pada lingkungan rumah sakit yang masih menggunakan Microsoft SQL Server versi terdahulu (seperti SQL Server 2008, 2008 R2, 2012, dan 2014) atau basis data dengan level kompatibilitas di bawah 130, kueri-kueri kritis mengalami kegagalan fatal dengan pesan kesalahan:
```
'STRING_SPLIT' is not a recognized built-in function name.
```

### Tujuan Pengujian
Memandu penguji manusia (*Human Tester, QA Engineer, Application Support, atau DBA*) untuk memverifikasi secara langsung dan deterministik bahwa:
1. **Antrian Loket Admisi (Single & Multi-Service Point)**: Daftar antrian loket dapat dimuat, difilter per loket/service point, dan dihitung jumlahnya tanpa memicu error `STRING_SPLIT`, memanfaatkan kemampuan parameter list native Dapper.
2. **Pencarian Riwayat Registrasi Pasien (Normal & Batch)**: Riwayat pendaftaran kunjungan pasien dapat dicari dan dimuat secara akurat menggunakan strategi *in-memory XML shredding* tanpa memanggil `STRING_SPLIT`.
3. **Ketahanan Volume Masal (> 2.100 ID)**: Pencarian riwayat registrasi dalam jumlah masal (2.600+ ID) tetap aman dan tidak melebihi batas 2.100 parameter SQL Server (*parameter limit compliance*).
4. **Pemuatan Detail Ekstra Perjalanan Rawat Inap (Ranap Journey Batch)**: Timeline dan detail ekstra perjalanan rawat inap (riwayat dokter, kamar, DPJP) termuat lengkap secara batch dengan filter registrasi native.
5. **Kompatibilitas Versi Legacy**: Kueri T-SQL yang dimigrasikan dapat dieksekusi dengan sukses pada instance Microsoft SQL Server dengan level kompatibilitas < 130 (SQL Server 2008–2014) maupun versi modern tanpa membutuhkan perubahan skema basis data (*Zero DDL*).
6. **Guardrail Statis Kode**: Memastikan tidak ada lagi sisa kemunculan kata kunci `STRING_SPLIT` di seluruh basis kode `src/`.

---

# 2. Scope & Testing Principles

## 2.1 Lingkup Pengujian
- **In-Scope**:
  - Endpoint dan kueri antrian loket admisi rawat jalan pada `AdmissionQueueOperationalProjection.cs`.
  - Endpoint dan kueri pencarian riwayat registrasi pasien pada `RegistrationHistoryReader.cs`.
  - Logika pemuatan rincian ekstra perjalanan pasien ranap batch pada `JourneyDal.Batch.cs`.
  - Pengujian kompatibilitas sintaks SQL Server (evaluasi XML parsing dan list parameter).
  - Verifikasi otomatis (unit & integration tests di `Bilreg.Test`).
  - Verifikasi kode statis (`git grep` guardrail).
- **Out-of-Scope**:
  - Modifikasi skema basis data DDL di database klien.
  - Perubahan logika bisnis tarif kasir / billing kasir di luar registrasi.
  - Perubahan tampilan UI frontend web (`c012_myhospital_web`).

## 2.2 Prinsip Pengujian
- **Human Executability**: Langkah-langkah pengujian dijelaskan secara konkret, dapat dijalankan melalui antarmuka web, pemanggilan API (Swagger / Postman), maupun CLI test runner.
- **Tester-Agnostic**: Instruksi berfokus pada *apa* yang diuji dan *hasil yang diharapkan*, bukan siapa personil yang menjalankannya.
- **Observable Behavior**: Verifikasi didasarkan pada respons API (status code HTTP, payload data), keterlihatan data di UI, dan tidak adanya exception SQL Server pada log backend.

---

# 3. Environment & Prerequisites

## 3.1 Kebutuhan Lingkungan & Konfigurasi
1. **Layanan Backend (`b09-bilreg-api`)**:
   - Berjalan pada runtime .NET 8 SDK.
   - Terhubung ke instance Microsoft SQL Server (database rumah sakit atau staging).
2. **Database SQL Server**:
   - Mendukung salah satu dari versi berikut: SQL Server 2008, 2008 R2, 2012, 2014, 2016, 2017, 2019, atau 2022.
   - Untuk uji kompatibilitas backward: disarankan menguji pada database dengan `COMPATIBILITY_LEVEL <= 120`.
3. **Aplikasi Klien / Testing Tools**:
   - Web browser (Google Chrome / Microsoft Edge) untuk verifikasi UI `c012_myhospital_web`.
   - Tool REST API (Swagger UI pada `http://localhost:5000/swagger`, Postman, atau curl).
   - Terminal PowerShell / Bash dengan .NET 8 CLI untuk eksekusi test runner.

## 3.2 Data Uji (Test Data)

| ID Data Uji | Entitas / Deskripsi | Kriteria Awal |
| :--- | :--- | :--- |
| `DATA-SP-01` | Service Point Antrian Tunggal | Kode loket pendaftaran yang aktif (contoh: `"LOKET-01"` atau `"A"`). |
| `DATA-SP-MULTI` | Service Point Antrian Jamak | Daftar beberapa kode loket aktif (contoh: `["LOKET-01", "LOKET-02", "LOKET-03"]`). |
| `DATA-PAS-01` | Pasien Riwayat Normal | Pasien dengan 1 hingga 5 riwayat registrasi kunjungan terdahulu. |
| `DATA-PAS-BULK` | Kumpulan Pasien Skala Besar | Daftar ID pasien berjumlah > 2.100 ID (dihasilkan via generator test runner 2.600 ID). |
| `DATA-RANAP-BATCH` | Daftar Registrasi Rawat Inap | Sekumpulan nomor registrasi pasien rawat inap yang memiliki riwayat dokter / konsul. |

---

# 4. Test Execution Sequence

Alur eksekusi pengujian diatur secara berurutan mulai dari fungsionalitas fitur hingga pengujian volume dan guardrail statis:

```
[Tahap 1: Verifikasi Fungsional Antrian Loket Admisi]
    └── TC-SPLIT-01: Pemuatan & Pemfilteran Antrian Loket (Single, Multi, & Tanpa Filter)
            ↓
[Tahap 2: Verifikasi Pencarian Riwayat Registrasi Pasien]
    ├── TC-SPLIT-02: Pencarian Riwayat Pasien Normal (Single & Multi-Pasien via XML Shredding)
    └── TC-SPLIT-03: Stress / Volume Test Riwayat Pasien (> 2.100 ID - Batas Parameter SQL Server)
            ↓
[Tahap 3: Verifikasi Batch Perjalanan Rawat Inap (Ranap Journey)]
    └── TC-SPLIT-04: Pemuatan Batch Rincian Ekstra Perjalanan Pasien Ranap (Doctor & Room History)
            ↓
[Tahap 4: Verifikasi Kompatibilitas Database & Guardrail Kode]
    ├── TC-SPLIT-05: Verifikasi Eksekusi pada SQL Server Compatibility Level <= 120 (Legacy Engine)
    └── TC-SPLIT-06: Verifikasi Statis Guardrail (Zero Occurrence of STRING_SPLIT)
```

---

# 5. Detailed Test Cases

---

### TC-SPLIT-01: Pemuatan & Pemfilteran Antrian Loket Admisi (Dapper Parameter List)

- **ID Kasus Uji**: `TC-SPLIT-01`
- **Kategori**: Fungsionalitas / Happy Path
- **Tujuan**: Memastikan kueri proyeksi operasional antrian loket dapat memuat antrian dengan filter 1 loket, beberapa loket, maupun tanpa filter loket tanpa memicu kesalahan sintaks `STRING_SPLIT`.
- **Prasyarat**:
  - Backend `b09-bilreg-api` dalam keadaan berjalan.
  - Terdapat data antrian pada hari pengujian.

#### Langkah Pengujian:
1. Akses menu **Antrian Loket Admisi** pada aplikasi frontend atau panggil endpoint proyeksi antrian:
   `GET /api/admisi/antrian/operational-worklist` (atau panggil query handler antrian loket).
2. Skenario A (Filter Loket Tunggal):
   - Masukkan filter Service Point ID: `["LOKET-01"]`.
   - Amati respons.
3. Skenario B (Filter Loket Jamak):
   - Masukkan filter Service Point ID: `["LOKET-01", "LOKET-02"]`.
   - Amati respons.
4. Skenario C (Tanpa Filter Loket / Tampilkan Semua):
   - Kosongkan filter Service Point ID (kirim koleksi kosong atau null).
   - Amati respons.

#### Hasil yang Diharapkan:
- Respons menghasilkan status **HTTP 200 OK**.
- Data daftar antrian dan total counter (*countSql*) terisi sesuai antrian yang ada.
- Pada log backend, kueri SQL Server menggunakan sintaks `IN @ServicePointIds` dengan penjaga parameter `@HasServicePointIds`.
- Tidak ada exception: `'STRING_SPLIT' is not a recognized built-in function name`.

---

### TC-SPLIT-02: Pencarian Riwayat Registrasi Pasien (Inline XML Shredding)

- **ID Kasus Uji**: `TC-SPLIT-02`
- **Kategori**: Fungsionalitas / Happy Path
- **Tujuan**: Memverifikasi bahwa pencarian riwayat pendaftaran kunjungan pasien (baik pencarian tunggal maupun jamak) berhasil diproses melalui parsing in-memory XML shredding dan mengembalikan daftar riwayat lengkap.
- **Prasyarat**:
  - Pasien uji `DATA-PAS-01` memiliki riwayat registrasi poli rawat jalan atau bangsal rawat inap terdahulu.

#### Langkah Pengujian:
1. Akses menu **Pendaftaran Rawat Jalan (Admisi)**.
2. Cari pasien `DATA-PAS-01` menggunakan No. Rekam Medis atau No. KTP.
3. Buka tab atau panel **Riwayat Kunjungan / Registrasi Pasien**.
4. Periksa data kunjungan yang ditampilkan (nomor registrasi, tanggal registrasi, poliklinik tujuan, dokter pemeriksa).
5. Lakukan uji pemanggilan batch riwayat pendaftaran dengan memilih beberapa ID pasien.

#### Hasil yang Diharapkan:
- Panel menampilkan daftar riwayat kunjungan pasien secara kronologis dan valid.
- Backend memproses kueri melalui sintaks XML nodes:
  `CAST('<x>' + REPLACE(@patientIdsCsv, ',', '</x><x>') + '</x>' AS XML)`
- Seluruh riwayat registrasi terkait pasien yang diminta tampil tanpa ada data yang terpotong.
- Tidak ada pesan kesalahan database terkait `STRING_SPLIT`.

---

### TC-SPLIT-03: Stress / Volume Test Riwayat Pasien (> 2.100 ID - Batas Parameter SQL Server)

- **ID Kasus Uji**: `TC-SPLIT-03`
- **Kategori**: Skalabilitas & Edge Case (Volume Testing)
- **Tujuan**: Memastikan arsitektur parsing XML dengan parameter tunggal CSV `@patientIdsCsv` mampu menangani pencarian masal ribuan entri (misal 2.600 pasien) tanpa melebihi batas 2.100 parameter SQL Server.
- **Prasyarat**:
  - Akses CLI terminal ke repositori `b09-bilreg-api`.

#### Langkah Pengujian:
1. Buka terminal di direktori proyek `d:\project_aktif\project_MyHospitalWeb\b09-bilreg-api`.
2. Jalankan unit test spesifik volume riwayat pasien:
   ```powershell
   dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~RegistrationHistoryReaderTest"
   ```
3. Periksa rincian asersi pada test `FindByPatientIds_WhenManyPatientIds_CreatesSingleCsvParameterUnder2100Limit`:
   - Menghasilkan daftar 2.600 ID acak.
   - Memvalidasi pembentukan DynamicParameters.

#### Hasil yang Diharapkan:
- Pengujian unit test lulus 100% (**Passed: 2, Failed: 0**).
- DynamicParameters hanya berisi tepat 1 parameter string berformat CSV (`@patientIdsCsv`), bukan 2.600 parameter individual.
- Tidak terjadi exception:
  `The incoming request has too many parameters. The server supports a maximum of 2100 parameters.`

---

### TC-SPLIT-04: Pemuatan Batch Rincian Ekstra Perjalanan Rawat Inap (Ranap Journey)

- **ID Kasus Uji**: `TC-SPLIT-04`
- **Kategori**: Fungsionalitas / Happy Path
- **Tujuan**: Memastikan kueri pemuatan data rincian ekstra perjalanan pasien rawat inap (`BuildRegExtrasSelectSql()`) yang membaca riwayat dokter konsul dan penempatan kamar dieksekusi secara mulus menggunakan parameter list Dapper `@RegIds`.
- **Prasyarat**:
  - Terdapat beberapa pasien rawat inap yang sedang aktif dirawat di bangsal/kamar.

#### Langkah Pengujian:
1. Masuk ke modul **Admisi Rawat Inap** -> submenu **Perjalanan Pasien Rawat Inap (Ranap Journey)**.
2. Muat daftar pasien bangsal aktif (misal: Ruang Teratai / Melati).
3. Buka salah satu kartu perjalanan pasien dan lihat rincian:
   - Riwayat dokter DPJP / dokter konsul.
   - Riwayat perpindahan ranjang/kamar.
4. Jalankan rangkaian pengujian otomatis Ranap Journey via CLI:
   ```powershell
   dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~Journey&Category!=JourneyDalIntegration&Category!=JourneyDalVolume"
   ```

#### Hasil yang Diharapkan:
- Seluruh kartu perjalanan pasien di bangsal menampilkan informasi dokter konsul dan detail kamar dengan lengkap.
- Pengujian otomatis Ranap Journey menghasilkan **80 Passed, 0 Failed (100% Lulus)**.
- Kueri `BuildRegExtrasSelectSql()` tidak lagi mengandung referensi `@RegIdCsv` maupun `STRING_SPLIT`, melainkan langsung menggunakan `FROM ta_registrasi reg WHERE reg.fs_kd_reg IN @RegIds`.

---

### TC-SPLIT-05: Verifikasi Eksekusi pada SQL Server Compatibility Level <= 120 (Legacy Engine)

- **ID Kasus Uji**: `TC-SPLIT-05`
- **Kategori**: Kompatibilitas Lingkungan / Database Engine
- **Tujuan**: Membuktikan bahwa kueri-kueri baru dapat dieksekusi secara native pada database dengan compatibility level 110 (SQL Server 2012) atau level 120 (SQL Server 2014) tanpa memerlukan upgrade versi database rumah sakit.
- **Prasyarat**:
  - Akses SQL Server Management Studio (SSMS) atau sqlcmd ke database pengujian.

#### Langkah Pengujian:
1. Buka query editor SSMS pada database pengujian.
2. Periksa level kompatibilitas database saat ini:
   ```sql
   SELECT name, compatibility_level FROM sys.databases WHERE name = DB_NAME();
   ```
3. (Opsional pada database staging) Ubah sementara level kompatibilitas ke level 110 atau 120:
   ```sql
   ALTER DATABASE CURRENT SET COMPATIBILITY_LEVEL = 110;
   ```
4. Jalankan kueri uji eliminasi `STRING_SPLIT` untuk parsing XML:
   ```sql
   DECLARE @patientIdsCsv VARCHAR(MAX) = 'P001,P002,P003';
   SELECT LTRIM(RTRIM(n.value('.', 'VARCHAR(50)'))) AS PatientId
   FROM (SELECT CAST('<x>' + REPLACE(@patientIdsCsv, ',', '</x><x>') + '</x>' AS XML) AS t) x
   CROSS APPLY x.t.nodes('/x') AS r(n)
   WHERE n.value('.', 'VARCHAR(50)') <> '';
   ```
5. Jalankan kueri uji eliminasi `STRING_SPLIT` untuk antrian loket:
   ```sql
   DECLARE @HasServicePointIds INT = 1;
   SELECT TOP 10 fs_kd_layanan, fs_no_antrian
   FROM ta_antrian_loket q
   WHERE (@HasServicePointIds = 0 OR q.fs_kd_layanan IN ('LOKET-01', 'LOKET-02'));
   ```

#### Hasil yang Diharapkan:
- Kedua kueri di atas tereksekusi dengan sukses dan mengembalikan baris data tanpa sintaks error.
- Terbukti bahwa seluruh fitur admisi dan billing registrasi kompatibel penuh dengan SQL Server 2008, 2012, dan 2014.

---

### TC-SPLIT-06: Verifikasi Statis Guardrail (Zero Occurrence of STRING_SPLIT)

- **ID Kasus Uji**: `TC-SPLIT-06`
- **Kategori**: Regression Guardrail / Static Code Analysis
- **Tujuan**: Memastikan tidak ada satupun kata kunci `STRING_SPLIT` yang tersisa atau terlewat di seluruh berkas kode sumber `b09-bilreg-api/src/`.
- **Prasyarat**:
  - Terminal Git / PowerShell pada root repositori `b09-bilreg-api`.

#### Langkah Pengujian:
1. Buka terminal di direktori `d:\project_aktif\project_MyHospitalWeb\b09-bilreg-api`.
2. Jalankan perintah pencarian statis:
   ```powershell
   git grep -in "STRING_SPLIT" src/
   ```
3. Periksa juga apakah ada sisa parameter usang `RegIdCsv`:
   ```powershell
   git grep -in "RegIdCsv" src/
   ```

#### Hasil yang Diharapkan:
- Kedua perintah di atas menghasilkan **0 baris** (keluaran kosong / exit code 1).
- Menjamin 100% kepatuhan kode terhadap kompatibilitas versi database.

---

# 6. Defect Reporting Protocol & Next Stages

### Protokol Pencatatan Cacat (*Defect Protocol*)
Jika salah satu kasus uji di atas menghasilkan status **FAIL**:
1. Catat rincian kegagalan pada dokumen pelacakan eksekusi `TEST-EXECUTION.md`:
   - ID Kasus Uji.
   - Pesan kesalahan (stack trace / error database).
   - Payload input dan langkah reproduksi.
2. Serahkan catatan temuan (*Defect Record*) kepada pemegang peran **`ica-issuer`** untuk dibuatkan tiket `ISSUE (BUG)` resmi.
3. Penguji tidak diperkenankan mengubah kode sumber atau mengubah status rencana secara langsung.

### Kriteria Kelulusan Pengujian (*Test Gate: TEST PASSED*)
Gerbang **TEST PASSED** hanya dapat diberikan ketika seluruh kasus uji (`TC-SPLIT-01` s/d `TC-SPLIT-06`) berstatus **PASS**, atau seluruh temuan **FAIL** telah diselesaikan melalui siklus perbaikan resmi dan diuji ulang dengan hasil **PASS**.
