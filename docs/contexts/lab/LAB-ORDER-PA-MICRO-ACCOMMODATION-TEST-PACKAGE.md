---
Title: Test Package - Akomodasi Order Laboratorium Patologi Anatomi dan Mikrobiologi
Code: LAB-ORDER-PA-MICRO-ACCOMMODATION
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-11-04
Status: READY-FOR-EXECUTION
Target Plan: LAB-ORDER-PA-MICRO-ACCOMMODATION-IMPLEMENTATION-PLAN.md
---

# 1. Overview & Objective

Dokumen ini merupakan **TEST-PACKAGE** resmi untuk memvalidasi implementasi fitur **Akomodasi Order Laboratorium Patologi Anatomi (PA) dan Mikrobiologi Klinik (Micro)** (`LAB-ORDER-PA-MICRO-ACCOMMODATION`) pada sistem informasi rumah sakit (HIS) **`c012_myhospital_web`** dan backend **`b09-bilreg-api`**.

### Tujuan Pengujian
Memastikan secara menyeluruh bahwa:
1. **Pemesanan Tunggal Terpadu (*Single Cart Ordering*)**: Dokter/perawat dapat memilih dan memesan item pemeriksaan dari disiplin Patologi Klinik (PK), Patologi Anatomi (PA), maupun Mikrobiologi Klinik (Micro) dalam satu keranjang pemesanan terpadu pada antarmuka canvas tanpa hambatan pergantian tab (*frictionless*).
2. **Mesin Pemecahan Otomatis (*Transparent Auto-Split Engine*)**: Backend memecah pesanan campuran lintas disiplin layanan secara otomatis dan transparan menjadi transaksi order terpisah (`1 Order = 1 ServiceType`) dalam satu transaksi database yang atomik (`TransHelper.NewScope()`).
3. **Penyimpanan Snapshot Klasifikasi (*Zero-JOIN Snapshot Persistence*)**: Setiap baris item order (`BILRG_LabOrderItem`) menyimpan snapshot klasifikasi (`ServiceType`, `DisciplineId`, `DisciplineName`, `TestGroupId`, `TestGroupName`) secara lengkap dan akurat.
4. **Perutean Polimorfik Entri Hasil (*Polymorphic Result Entry*)**: Petugas laboratorium disajikan antarmuka entri hasil yang tepat sesuai jenis layanan order:
   - **Patologi Klinik (PK)**: Formulir tabular analit numerik/kualitatif dengan nilai rujukan dan bendera abnormal.
   - **Patologi Anatomi (PA)**: Formulir laporan naratif klinis mencakup seksi makroskopis, mikroskopis, diagnosa PA, dan catatan dokter Sp.PA.
   - **Mikrobiologi (Micro)**: Formulir kultur mikroba mencakup identifikasi koloni/spesies patogen dan matriks uji kepekaan antibiotik (*Antimicrobial Susceptibility Testing / AST* - S/I/R).
5. **Pengelompokan Hierarki 2-Tingkat (*2-Level Grouping*) & Cetak Slip**: Rincian pemeriksaan pada panel kerja dan lembar cetak bukti permintaan laboratorium dikelompokkan secara elegan (Level 1: Bidang Disiplin $\rightarrow$ Level 2: Kelompok/Panel Tes $\rightarrow$ Level 3: Item Pemeriksaan) dengan penanganan data historis (*graceful fallback*).
6. **Penanganan Spesimen Non-Darah**: Kalkulator tabung flebotomi mengabaikan spesimen non-darah (`VacutainerType == None / 0`), sehingga tabung flebotomi hanya dihitung untuk sampel darah.
7. **Perlindungan Batas Integrasi**: Middleware integrasi LIS/PACS `a046_oware_ordermiddlewareapi` beroperasi normal tanpa perubahan kode (*100% As-Is*).

Dokumen ini disusun agar dapat dipahami dan dieksekusi oleh penguji manusia (*Tester, QA Engineer, Dokter Penguji, Analis Laboratorium, atau Product Owner*) tanpa memerlukan keahlian pemrograman internal.

---

# 2. Scope & Testing Principles

## 2.1 Lingkup Pengujian
- **In-Scope**:
  - Formulir pemesanan terpadu pada frontend HIS Web (`LabOrderCreateCanvas.vue`).
  - Kalkulator kebutuhan tabung darah flebotomi (`VacutainerTypeEnum.None = 0`).
  - Pembuatan order internal EMR (`LabOrderCreateFromEmrCmd`) dan order pasien luar (`LabOrderCreateExternalPatientCmd`).
  - Logika pemisahan order otomatis (*Auto-Split*) pada backend `LabOrderCreateHelper`.
  - Persistensi snapshot klasifikasi pada `BILRG_LabOrder` dan `BILRG_LabOrderItem`.
  - Komponen perute form hasil polimorfik `LabResultEntryRouter.vue` beserta sub-form (`LabResultEntryPkTabular.vue`, `LabResultEntryPaForm.vue`, `LabResultEntryMicroForm.vue`).
  - Pengelompokan hierarki 2-tingkat pada `LabOrderDetailPanel.vue` dan slip cetak `LabOrderSlip.vue`.
  - Ketahanan fallback terhadap data historis tanpa snapshot klasifikasi.
- **Out-of-Scope**:
  - Alur analiser mesin laboratorium otomatis di dalam laboratorium fisik (LIS vendor).
  - Modifikasi kode middleware `a046_oware_ordermiddlewareapi` (ditegaskan 100% As-Is).
  - Pembayaran kasir tunai / EDC (`TRK-KASIR`).

## 2.2 Prinsip Pengujian
- **Berorientasi Perilaku yang Teramati (*Observable Behavior*)**: Pengujian berfokus pada apa yang tampil pada layar antarmuka pengguna, format slip cetak fisik/PDF, dan respon data operasional.
- **Tester-Agnostic**: Instruksi langkah disusun konkret dan lugas, dapat dieksekusi melalui antarmuka web MyHospital HIS maupun via REST API client (Postman/Swagger).
- **Cakupan Pengujian Komprehensif**: Menjamin jalur utama (*Happy Path*), aturan bisnis (*Business Rules*), validasi data (*Validation Rules*), penanganan galat (*Error Handling*), dan proteksi regresi (*Regression Protection*).

---

# 3. Environment & Prerequisites

## 3.1 Kebutuhan Lingkungan
- **Aplikasi Frontend**: `c012_myhospital_web` berjalan aktif pada `http://localhost:5173`.
- **Layanan Backend**: `b09-bilreg-api` berjalan aktif pada port lokal (misal: `http://localhost:5189` atau `https://localhost:7001`).
- **Target Database**: SQL Server aktif (database `hospital_pkl` atau `BilregDb`) yang telah memiliki:
  - Tabel master `BILRG_LabDiscipline` dengan 7 data seed (`DISC-HEM`, `DISC-KIM`, `DISC-IMM`, `DISC-URI`, `DISC-MIK`, `DISC-PAH`, `DISC-PAS`).
  - Tabel master `BILRG_LabTestGroup`.
  - Kolom klasifikasi pada `BILRG_LabTestDefinition`, `BILRG_LabOrder`, dan snapshot pada `BILRG_LabOrderItem`.
- **Aktor Penguji**:
  - Akun Dokter / Perawat Poliklinik / IGD (Hak akses memesan order lab EMR).
  - Akun Analis / Petugas Laboratorium (Hak akses worklist lab dan result entry).
  - Akun Petugas Loket Lab (Hak akses registrasi pasien luar/eksternal).

## 3.2 Kebutuhan Data Uji (Test Data)

| ID Data Uji | Tipe Skenario | Item Pemeriksaan yang Digunakan | Layanan & Disiplin |
|---|---|---|---|
| `DATA-PK-RUTIN` | Order Murni PK | `LAB0103002` (Hemoglobin) & `LAB0101006` (SGOT) | `ServiceType: 1 (PK)`<br>Disiplin: `DISC-HEM` & `DISC-KIM` |
| `DATA-PA-HISTO` | Order Murni PA Jaringan | `LAB0221002` (PA 3 Jaringan) | `ServiceType: 2 (PA)`<br>Disiplin: `DISC-PAH`<br>Sampel: Jaringan Biopsi (Non-Darah) |
| `DATA-PA-SITO` | Order Murni PA Sitologi | `LAB0216001` (Pap Smear) & `LAB0221001` (Sitologi Pleura) | `ServiceType: 2 (PA)`<br>Disiplin: `DISC-PAS`<br>Sampel: Cairan / Usapan (Non-Darah) |
| `DATA-MIC-BTA` | Order Murni Mikrobiologi | `LAB0116004` (BTA 3x) & `LAB0116002` (Sekret 1x) | `ServiceType: 3 (Micro)`<br>Disiplin: `DISC-MIK`<br>Sampel: Sputum / Sekret (Non-Darah) |
| `DATA-CAMPURAN-PK-PA` | Order Campuran 2 Layanan | `LAB0221005` (CBC - PK) + `LAB0221002` (PA 3 Jaringan - PA) | Campuran `ServiceType: 1` dan `2` |
| `DATA-CAMPURAN-3-SVC` | Order Campuran 3 Layanan | `LAB0103002` (Hb - PK) + `LAB0216001` (Pap Smear - PA) + `LAB0116004` (BTA 3x - Micro) | Campuran `ServiceType: 1`, `2`, dan `3` |
| `DATA-HISTORIS-LEGACY` | Order Historis Lama | Order lama sebelum migrasi klasifikasi | `DisciplineId: ""` (Snapshot Kosong) |

---

# 4. Test Execution Sequence

```mermaid
flowchart TD
    subgraph Tahap 1: Pemesanan Order Tunggal
        T1["TC-LPAM-01 (Order Murni PK: Kalkulasi Tabung Darah Normal)"]
        T2["TC-LPAM-02 (Order Murni PA: Spesimen Non-Darah, Tabung = 0)"]
        T3["TC-LPAM-03 (Order Murni Micro: Spesimen Non-Darah, Tabung = 0)"]
        T4["TC-LPAM-04 (Order Pasien Eksternal Murni PA)"]
    end

    subgraph Tahap 2: Pemesanan Campuran & Transparent Auto-Split
        T5["TC-LPAM-05 (Order Campuran PK + PA: Terpecah Menjadi 2 Sub-Order)"]
        T6["TC-LPAM-06 (Order Campuran PK + Micro: Terpecah Menjadi 2 Sub-Order)"]
        T7["TC-LPAM-07 (Order Campuran 3 Layanan: PK + PA + Micro Menjadi 3 Sub-Order)"]
        T8["TC-LPAM-08 (Atomisitas Transaksi Auto-Split: Rollback Bila Gagal)"]
        T9["TC-LPAM-09 (Replikasi Catatan Klinis ke Seluruh Sub-Order)"]
    end

    subgraph Tahap 3: Perutean Polimorfik Form Entri Hasil
        T10["TC-LPAM-10 (Buka Order PK -> Form Tabular Analit Standar)"]
        T11["TC-LPAM-11 (Buka Order PA -> Form Naratif Makro/Mikro/Diagnosa PA)"]
        T12["TC-LPAM-12 (Buka Order Micro -> Form Biakan Kuman & Antibiogram AST)"]
        T13["TC-LPAM-13 (Pergantian Cepat Worklist Lintas Tipe Layanan)"]
    end

    subgraph Tahap 4: Pengelompokan Hierarki 2-Tingkat & Cetak Slip
        T14["TC-LPAM-14 (Rincian Order: Tampilan Hierarki Disiplin -> Grup -> Item)"]
        T15["TC-LPAM-15 (Cetak Lembar Permintaan Lab dengan Pengelompokan Rapi)"]
        T16["TC-LPAM-16 (Graceful Fallback untuk Order Historis/Snapshot Kosong)"]
    end

    subgraph Tahap 5: Non-Darah & Proteksi Regresi
        T17["TC-LPAM-17 (Kalkulator Tabung Mengabaikan Item Non-Darah Campuran)"]
        T18["TC-LPAM-18 (Verifikasi Integritas As-Is Middleware OWARE)"]
    end

    Tahap 1 --> Tahap 2
    Tahap 2 --> Tahap 3
    Tahap 3 --> Tahap 4
    Tahap 4 --> Tahap 5
```

---

# 5. Detailed Test Cases

## Kategori A: Pemesanan Order Tunggal (Single-Discipline Orders)

---

### TC-LPAM-01: Pemesanan Order Tunggal Murni Patologi Klinik (PK) dari EMR Canvas

- **Objective**: Memverifikasi bahwa pemesanan tes murni Patologi Klinik menghasilkan 1 entitas order tunggal (`ServiceType = 1`), nomor order standar tanpa akhiran suffix auto-split, dan kalkulator tabung flebotomi menghitung kebutuhan tabung darah secara akurat.
- **Preconditions**:
  - Pengguna masuk ke HIS Web dengan peran Dokter/Perawat Rawat Jalan.
  - Membuka rekam medis pasien aktif pada modul EMR.
  - Membuka canvas pemesanan laboratorium (`LabOrderCreateCanvas.vue`).
- **Test Data**:
  - Pasien: Rawat Jalan Poliklinik Penyakit Dalam.
  - Item Pemeriksaan:
    1. `LAB0103002` (Hemoglobin, Darah EDTA, `Vacutainer: Ungu / EDTA`)
    2. `LAB0101006` (SGOT, Darah Serum, `Vacutainer: Merah / Plain`)
  - Catatan Klinis: "Evaluasi anemia dan fungsi hati rutin".
- **Steps**:
  1. Pada kotak pencarian tes laboratorium terpadu, ketik "Hemoglobin" dan pilih `HEMOGLOBIN (Hb.)`.
  2. Ketik "SGOT" dan pilih `SGOT`.
  3. Amati bagian ringkasan kalkulator tabung flebotomi pada panel kanan formulir.
  4. Isi catatan klinis dengan "Evaluasi anemia dan fungsi hati rutin".
  5. Klik tombol **Kirim Permintaan Lab**.
  6. Buka daftar order pada meja kerja laboratorium (*Lab Worklist*).
- **Expected Result**:
  - Pada canvas, kalkulator tabung menampilkan 2 tabung flebotomi yang dibutuhkan (1 Tabung Ungu EDTA dan 1 Tabung Merah Plain).
  - Sistem berhasil menerbitkan **1 nomor order lab tunggal** (contoh: `ORD-20261104-0001`).
  - Nilai `ServiceType` pada order adalah `1` (`ClinicalPathology`).
  - Baris order item menyimpan snapshot `DisciplineId: DISC-HEM` untuk Hemoglobin dan `DisciplineId: DISC-KIM` untuk SGOT.
  - Tidak terjadi pemecahan suffix (`-PK` tidak ditambahkan karena murni 1 layanan).

---

### TC-LPAM-02: Pemesanan Order Tunggal Murni Patologi Anatomi (PA) dari EMR Canvas (Non-Darah)

- **Objective**: Memverifikasi bahwa pemesanan pemeriksaan Patologi Anatomi (sampel jaringan biopsi) menghasilkan order bertipe PA (`ServiceType = 2`), dan kalkulator tabung flebotomi mengabaikan item non-darah (`vacutainerType === 0`).
- **Preconditions**:
  - Pasien aktif berada di Poliklinik Bedah Umum.
  - Formulir order laboratorium dibuka pada antarmuka canvas.
- **Test Data**:
  - Item Pemeriksaan: `LAB0221002` (PA 3 JARINGAN, Spesimen: Jaringan Biopsi, `Vacutainer: None / 0`).
  - Catatan Klinis: "Biopsi eksisi benjolan mammae dextra, fiksasi formalin 10%".
- **Steps**:
  1. Pada kotak pencarian tes terpadu (tanpa berpindah tab), cari dan pilih "PA 3 JARINGAN".
  2. Periksa panel kalkulator tabung flebotomi di sisi kanan.
  3. Masukkan catatan klinis mengenai asal sediaan dan fiksatif.
  4. Klik tombol **Kirim Permintaan Lab**.
  5. Periksa rincian order pada database atau API `GET /api/LabContext/LabOrderFeature/{orderId}`.
- **Expected Result**:
  - Kalkulator tabung flebotomi menampilkan informasi **"0 Tabung Vacutainer (Spesimen Non-Darah / Wadah Khusus Jaringan)"** atau daftar tabung darah kosong.
  - Pesanan berhasil disimpan sebagai 1 nomor order lab tunggal.
  - Atribut order: `ServiceType = 2` (`AnatomicalPathology`).
  - Item order memiliki snapshot `DisciplineId = 'DISC-PAH'` dan `DisciplineName = 'Patologi Anatomi - Histopatologi'`.
  - Catatan klinis spesimen tersimpan utuh pada kolom `ClinicalNote`.

---

### TC-LPAM-03: Pemesanan Order Tunggal Murni Mikrobiologi (Micro) dari EMR Canvas

- **Objective**: Memverifikasi bahwa pemesanan tes mikrobiologi klinik menghasilkan order bertipe Micro (`ServiceType = 3`) dan kalkulator tabung flebotomi tetap bersih (0 tabung darah).
- **Preconditions**:
  - Pasien aktif berada di Poliklinik Paru.
  - Formulir order laboratorium dibuka pada antarmuka canvas.
- **Test Data**:
  - Item Pemeriksaan: `LAB0116004` (BTA 3X, Spesimen: Sputum Dahak, `Vacutainer: None / 0`).
  - Catatan Klinis: "Batuk berdahak > 2 minggu, suspek TB paru aktif".
- **Steps**:
  1. Cari dan pilih pemeriksaan "BTA 3X" ke dalam keranjang pemesanan.
  2. Verifikasi ringkasan kebutuhan tabung flebotomi.
  3. Isi catatan klinis dan kirim order.
  4. Amati entitas order yang terbentuk di meja kerja laboratorium.
- **Expected Result**:
  - Kebutuhan tabung flebotomi darah tetap 0 (menggunakan pot sputum non-vacutainer).
  - Order tersimpan dengan atribut `ServiceType = 3` (`Microbiology`).
  - Item order memiliki snapshot `DisciplineId = 'DISC-MIK'` dan `DisciplineName = 'Mikrobiologi Klinik'`.
  - Sistem tidak menampilkan field input tambahan mikrobiologi yang membingungkan; informasi klinis tercatat rapi pada `ClinicalNote`.

---

### TC-LPAM-04: Pemesanan Order Laboratorium Pasien Luar (Eksternal) Murni PA

- **Objective**: Memverifikasi pembuatan order laboratorium untuk pasien eksternal/rujukan luar (`LabOrderCreateExternalPatientCmd`) yang memesan pemeriksaan Patologi Anatomi (misal: Pap Smear rujukan bidan).
- **Preconditions**:
  - Pengguna masuk dengan akun Petugas Loket Laboratorium.
  - Membuka menu Registrasi Pasien Luar Laboratorium (`LabExternalRegistrationCanvas.vue`).
- **Test Data**:
  - Pasien: Ny. S (Pasien Rujukan Luar).
  - Dokter Pengirim: Dr. Sp.OG Luar.
  - Item Pemeriksaan: `LAB0216001` (PAP SMEAR, Spesimen: Usapan Serviks, `Vacutainer: None / 0`).
  - Catatan Pengantar: "Skrining rutin papsmear, fiksasi alkohol 96%".
- **Steps**:
  1. Lengkapi formulir identitas pasien eksternal (Nama, NIK, Tgl Lahir, Dokter Perujuk).
  2. Pilih item tes "PAP SMEAR".
  3. Simpan dan terbitkan order laboratorium luar.
- **Expected Result**:
  - Order laboratorium luar berhasil diterbitkan dengan `ServiceType = 2` (`AnatomicalPathology`).
  - Snapshot item mencatat `DisciplineId = 'DISC-PAS'` (`Patologi Anatomi - Sitologi`).
  - Order muncul pada daftar kerja laboratorium dengan label kategori **Patologi Anatomi**.

---

## Kategori B: Pemesanan Campuran & Transparent Auto-Split Engine

---

### TC-LPAM-05: Pemesanan Campuran PK + PA dalam Satu Keranjang Terpadu (Auto-Split Menjadi 2 Sub-Order)

- **Objective**: Memverifikasi bahwa ketika dokter memesan item PK dan item PA sekaligus dalam satu keranjang pemesanan, backend secara otomatis dan transparan memecah pesanan menjadi 2 sub-order terpisah (`-PK` dan `-PA`) dalam satu transaksi atomik.
- **Preconditions**:
  - Pasien aktif berada di Poliklinik Onkologi / Bedah.
  - Membuka `LabOrderCreateCanvas.vue`.
- **Test Data**:
  - Item PK: `LAB0221005` (CBC / Darah Lengkap, `ServiceType = 1`, Darah EDTA).
  - Item PA: `LAB0221002` (PA 3 JARINGAN, `ServiceType = 2`, Jaringan Biopsi).
  - Catatan Klinis: "Persiapan operasi mastektomi dan biopsi histopatologi".
- **Steps**:
  1. Pada canvas pencarian terpadu, masukkan "CBC" ke keranjang.
  2. Tanpa berpindah halaman atau tab, cari dan masukkan "PA 3 JARINGAN" ke keranjang yang sama.
  3. Amati daftar item dalam keranjang: keduanya berada dalam 1 tabel keranjang belanja dokter.
  4. Amati kalkulator tabung flebotomi: hanya menghitung tabung EDTA untuk CBC, mengabaikan spesimen PA.
  5. Masukkan catatan klinis lalu klik **Kirim Permintaan Lab**.
  6. Periksa respon API dan daftar order pada meja kerja laboratorium.
- **Expected Result**:
  - Respon API mengindikasikan keberhasilan transaksi dengan daftar order yang dipecah.
  - Terbentuk **2 entitas order baru** di database `BILRG_LabOrder`:
    1. Order 1: Nomor berakhiran `-PK` (contoh: `ORD-20261104-0010-PK`), memiliki `ServiceType = 1`, berisi 1 item (CBC).
    2. Order 2: Nomor berakhiran `-PA` (contoh: `ORD-20261104-0010-PA`), memiliki `ServiceType = 2`, berisi 1 item (PA 3 Jaringan).
  - Kedua order memiliki nomor urut registrasi yang terdaftar rapi pada kunjungan pasien yang sama.
  - Masing-masing order memiliki snapshot klasifikasi yang sesuai.

---

### TC-LPAM-06: Pemesanan Campuran PK + Mikrobiologi dalam Satu Keranjang

- **Objective**: Memverifikasi pemecahan transparan pada kombinasi pemeriksaan darah rutin (PK) dan kultur mikrobiologi (Micro).
- **Preconditions**:
  - Pasien Rawat Inap Ruang Bangsal Paru.
  - Membuka canvas pemesanan lab EMR.
- **Test Data**:
  - Item PK: `LAB0103002` (Hemoglobin) & `LAB0103003` (Leukosit) (`ServiceType = 1`).
  - Item Micro: `LAB0116004` (BTA 3X, `ServiceType = 3`).
- **Steps**:
  1. Masukkan Hemoglobin, Leukosit, dan BTA 3X ke dalam satu keranjang pemesanan.
  2. Klik tombol **Kirim Permintaan Lab**.
  3. Buka modul Worklist Laboratorium.
- **Expected Result**:
  - Terbit 2 sub-order independen:
    1. Sub-order `-PK`: `ServiceType = 1`, berisi Hemoglobin dan Leukosit.
    2. Sub-order `-MIC`: `ServiceType = 3`, berisi BTA 3X.
  - Petugas flebotomi di bangsal hanya melihat instruksi penarikan tabung darah pada sub-order PK.
  - Petugas mikrobiologi menerima sub-order terpisah untuk penyerahan spesimen dahak.

---

### TC-LPAM-07: Pemesanan Campuran 3 Layanan Sekaligus (PK + PA + Micro) Menjadi 3 Sub-Order Atomik

- **Objective**: Memverifikasi skenario ekstrem di mana dokter memesan item dari ketiga pilar layanan laboratorium sekaligus dalam satu tindakan pemesanan.
- **Preconditions**:
  - Pasien Rawat Inap ICU / Bedah.
  - Membuka canvas pemesanan lab EMR.
- **Test Data**:
  - Item PK: `LAB0221005` (CBC, `ServiceType = 1`).
  - Item PA: `LAB0216001` (Pap Smear, `ServiceType = 2`).
  - Item Micro: `LAB0116002` (Sekret 1X, `ServiceType = 3`).
  - Catatan Klinis: "Pasien pasca-histerektomi, evaluasi komprehensif sepsis dan histologi".
- **Steps**:
  1. Masukkan ketiga item berbeda layanan tersebut ke dalam satu keranjang pemesanan.
  2. Pastikan pesan validasi tidak memblokir penambahan item multi-layanan.
  3. Kirim pesanan.
  4. Periksa tabel `BILRG_LabOrder` dan `BILRG_LabOrderItem`.
- **Expected Result**:
  - Sistem secara transparan menghasilkan **3 nomor order terpisah**:
    - `{NoOrder}-PK` (`ServiceType = 1`)
    - `{NoOrder}-PA` (`ServiceType = 2`)
    - `{NoOrder}-MIC` (`ServiceType = 3`)
  - Seluruh sub-order tersimpan sukses dalam satu waktu tanpa jeda atau kegagalan inkonsisten.
  - Setiap sub-order memiliki item pemeriksaan yang terdistribusi secara akurat sesuai jenis layanannya.

---

### TC-LPAM-08: Verifikasi Atomisitas Auto-Split (Rollback Menyeluruh jika Salah Satu Gagal)

- **Objective**: Memverifikasi bahwa seluruh proses pemecahan order berada di dalam satu transaksi database (`TransHelper.NewScope()`). Jika salah satu sub-order gagal disimpan (misal: simulasi gangguan validasi tarif), tidak ada satu pun sub-order yang tersimpan parsial (*Zero Orphan Data*).
- **Preconditions**:
  - Lingkungan pengujian dengan skenario injeksi validasi (atau payload simulasi di mana salah satu tarif tidak aktif).
- **Steps**:
  1. Siapkan keranjang dengan 2 item: 1 item PK valid dan 1 item PA yang tarifnya dinonaktifkan/korup.
  2. Eksekusi pengiriman order.
  3. Periksa isi database `BILRG_LabOrder` untuk nomor EMR terkait.
- **Expected Result**:
  - Sistem mengembalikan respon error validasi yang jelas kepada pengguna.
  - **Tidak ada order parsial yang tersimpan**: sub-order PK tidak boleh tersimpan sendiri jika sub-order PA gagal.
  - Keranjang pemesanan dokter tetap utuh di layar untuk diperbaiki.

---

### TC-LPAM-09: Verifikasi Replikasi Catatan Klinis (`ClinicalNote`) ke Seluruh Sub-Order

- **Objective**: Memverifikasi bahwa catatan indikasi medis umum, riwayat obat/antibiotik, dan informasi klinis yang diketik dokter disalin secara utuh ke setiap sub-order hasil pemecahan (*auto-split*).
- **Preconditions**:
  - Order campuran (PK + PA + Micro) dibuat dari canvas EMR.
- **Test Data**:
  - Teks Catatan: "Pasien kemoterapi siklus 3, riwayat meropenem 5 hari, suspek infeksi jamur paru dan metastasis serviks".
- **Steps**:
  1. Masukkan catatan klinis di atas pada form canvas pemesanan campuran.
  2. Kirim order hingga terbit 3 sub-order (`-PK`, `-PA`, `-MIC`).
  3. Buka detail masing-masing sub-order via API `GET /api/LabContext/LabOrderFeature/{orderId}` atau panel rincian.
- **Expected Result**:
  - Sub-order `-PK` memiliki `ClinicalNote` yang persis sama.
  - Sub-order `-PA` memiliki `ClinicalNote` yang persis sama (dokter Sp.PA dapat membaca riwayat klinis pengantar).
  - Sub-order `-MIC` memiliki `ClinicalNote` yang persis sama (analis mikrobiologi dapat membaca riwayat antibiotik pasien).

---

## Kategori C: Perutean Polimorfik Entri Hasil (Polymorphic Result Entry)

---

### TC-LPAM-10: Entri Hasil Order Patologi Klinik (PK) Menampilkan Form Tabular Analit Standar

- **Objective**: Memverifikasi bahwa membuka order bertipe PK mengaktifkan komponen perute `LabResultEntryRouter.vue` yang secara tepat merender form tabel analit (`LabResultEntryPkTabular.vue`).
- **Preconditions**:
  - Petugas laboratorium membuka Meja Kerja Hasil (*Lab Result Worklist*).
  - Terdapat order berstatus `Collected` dengan `ServiceType = 1` (PK).
- **Steps**:
  1. Klik baris order PK pada worklist, lalu klik tombol **Entri Hasil** (*Input Result*).
  2. Amati tampilan komponen formulir yang muncul.
- **Expected Result**:
  - Sistem menampilkan form tabular analit standar:
    - Kolom Nama Parameter Tes.
    - Kolom Nilai Hasil (Input Numerik / Kualitatif).
    - Kolom Satuan (misal: `g/dL`, `/uL`, `mg/dL`).
    - Kolom Nilai Rujukan / Batas Normal.
    - Bendera Abnormal (*Flag Low / High / Critical*).
  - Tidak ada seksi laporan naratif PA maupun matriks kultur kuman mikrobiologi pada tampilan ini.

---

### TC-LPAM-11: Entri Hasil Order Patologi Anatomi (PA) Menampilkan Editor Naratif

- **Objective**: Memverifikasi bahwa membuka order bertipe PA mengaktifkan sub-form naratif Patologi Anatomi (`LabResultEntryPaForm.vue`) dengan seksi klinis khusus.
- **Preconditions**:
  - Terdapat order berstatus `Collected` dengan `ServiceType = 2` (PA).
- **Steps**:
  1. Pada worklist hasil lab, pilih order dengan label tipe layanan **PA**.
  2. Klik tombol **Entri Hasil**.
  3. Amati formulir yang ditampilkan.
  4. Lakukan pengisian hasil pada seksi yang tersedia:
     - Makroskopis: "Diterima 1 pot berisi 3 potong jaringan ukuran 2x1x0.5 cm, warna putih kecoklatan".
     - Mikroskopis: "Sediaan menunjukkan proliferasi sel epitel kelenjar atipik...".
     - Kesimpulan / Diagnosa PA: "Invasive Ductal Carcinoma Mammae Grade II".
     - Catatan Khusus: "Disarankan pemeriksaan lanjutan imunohistokimia ER, PR, HER2".
  5. Klik tombol **Simpan Hasil**.
- **Expected Result**:
  - Formulir yang muncul bukan tabel analit angka, melainkan editor laporan naratif Patologi Anatomi.
  - Tersedia textarea terstruktur untuk Makroskopis, Mikroskopis, Diagnosa PA, dan Catatan Dokter.
  - Data naratif berhasil disimpan dan terasosiasi pada dokumen hasil order PA.

---

### TC-LPAM-12: Entri Hasil Order Mikrobiologi (Micro) Menampilkan Form Biakan Kuman & Antibiogram AST

- **Objective**: Memverifikasi bahwa membuka order bertipe Micro mengaktifkan sub-form mikrobiologi (`LabResultEntryMicroForm.vue`) yang memfasilitasi pencatatan isolat kuman dan matriks kepekaan antibiotik.
- **Preconditions**:
  - Terdapat order berstatus `Collected` dengan `ServiceType = 3` (Micro).
- **Steps**:
  1. Pilih order Mikrobiologi pada worklist dan klik **Entri Hasil**.
  2. Amati komponen formulir yang dirender.
  3. Lakukan pengisian data kultur:
     - Hasil Biakan: Positif Tumbuh Kuman.
     - Nama Mikroorganisme: "Pseudomonas aeruginosa".
     - Hitung Koloni: "> 100.000 CFU/mL".
     - Uji Kepekaan Antibiotik (AST):
       - Amikacin: *Susceptible (S)*
       - Ciprofloxacin: *Resistant (R)*
       - Meropenem: *Intermediate (I)*
  4. Klik tombol **Simpan Hasil**.
- **Expected Result**:
  - Formulir yang dirender adalah formulir khusus biakan & AST mikrobiologi.
  - Antarmuka menyediakan input isolasi bakteri dan matriks tabel kepekaan antibiotik dengan dropdown/toggle opsi `S`, `I`, `R`.
  - Data tersimpan sempurna tanpa error skema.

---

### TC-LPAM-13: Validasi Guard Perutean saat Pergantian Cepat Antar-Order di Meja Kerja

- **Objective**: Memverifikasi bahwa ketika analis berpindah dari memilih order PK ke order PA lalu ke order Micro secara berurutan, form entri hasil berganti secara reaktif dan bersih tanpa ada kebocoran state (*state leakage*) dari form sebelumnya.
- **Preconditions**:
  - Tersedia 3 order di worklist: 1 PK, 1 PA, 1 Micro.
- **Steps**:
  1. Buka entri hasil untuk order PK, ketik nilai numerik pada salah satu analit (jangan disimpan).
  2. Kembali dan langsung klik entri hasil untuk order PA.
  3. Amati apakah form berganti menjadi naratif PA dan input numerik PK sebelumnya bersih.
  4. Kembali dan langsung klik entri hasil untuk order Micro.
  5. Amati apakah form berganti menjadi form kultur kuman.
- **Expected Result**:
  - `LabResultEntryRouter.vue` merender komponen anak yang tepat secara instan berdasarkan `currentOrder.serviceType`.
  - State formulir terisolasi per order dan tidak terjadi error pada console browser (*no unhandled exceptions*).

---

## Kategori D: Pengelompokan Hierarki 2-Tingkat & Cetak Lembar Permintaan

---

### TC-LPAM-14: Verifikasi Tampilan Hierarki 2-Tingkat pada Panel Rincian Order (`LabOrderDetailPanel.vue`)

- **Objective**: Memverifikasi bahwa panel rincian order menyajikan pemeriksaan terkelompok rapi dalam 2 tingkat: Level 1 (Disiplin) $\rightarrow$ Level 2 (Grup Tes) $\rightarrow$ Level 3 (Item Pemeriksaan) berbasis snapshot tanpa memicu kueri runtime join lambat ke tabel master.
- **Preconditions**:
  - Pasien memiliki order lab yang berisi beberapa tes dari disiplin berbeda (misal: Hematologi dan Kimia Darah).
- **Test Data**:
  - Item 1: `HEMOGLOBIN` (Disiplin: `Hematologi`, Grup: `Darah Lengkap`)
  - Item 2: `LEUKOSIT` (Disiplin: `Hematologi`, Grup: `Darah Lengkap`)
  - Item 3: `SGOT` (Disiplin: `Kimia Klinik`, Grup: `Fungsi Hati`)
  - Item 4: `SGPT` (Disiplin: `Kimia Klinik`, Grup: `Fungsi Hati`)
- **Steps**:
  1. Buka panel rincian order pada `LabOrderDetailPanel.vue`.
  2. Amati struktur tabel item pemeriksaan yang ditampilkan.
- **Expected Result**:
  - Tampilan tabel tidak berupa daftar datar (*flat list*), melainkan terstruktur hierarkis:
    - **Header Level 1**: `HEMATOLOGI`
      - **Sub-Header Level 2**: `Darah Lengkap`
        - Baris Tes: `Hemoglobin`
        - Baris Tes: `Leukosit`
    - **Header Level 1**: `KIMIA KLINIK`
      - **Sub-Header Level 2**: `Fungsi Hati`
        - Baris Tes: `SGOT`
        - Baris Tes: `SGPT`
  - Waktu muat panel instan karena seluruh data kelompok dibaca langsung dari snapshot kolom order item.

---

### TC-LPAM-15: Verifikasi Cetak Lembar Permintaan Lab (`LabOrderSlip.vue`)

- **Objective**: Memverifikasi bahwa dokumen cetak bukti permintaan laboratorium (format Thermal 80mm dan Kertas Standar A5) mencetak hierarki pengelompokan disiplin yang rapi beserta informasi tabung dan barcode.
- **Preconditions**:
  - Order laboratorium telah terbit.
  - Membuka dialog pratinjau cetak slip (`LabOrderSlip.vue`).
- **Steps**:
  1. Pada panel rincian order, klik tombol **Cetak Slip** (*Print Slip*).
  2. Periksa tampilan pratinjau cetak (*Print Preview*).
- **Expected Result**:
  - Dokumen cetak menyajikan barcode nomor order yang dapat dipindai (*Code39 SVG*).
  - Daftar pemeriksaan tersusun rapi dengan pemisah header Disiplin dan Grup Tes.
  - Untuk order darah, tertera ringkasan kebutuhan tabung vacutainer.
  - Untuk order non-darah (PA/Micro), tidak muncul teks permintaan tabung darah yang keliru.
  - Tata letak rapi, font terbaca jelas, dan tidak terpotong pada batas halaman.

---

### TC-LPAM-16: Verifikasi Graceful Fallback untuk Order Historis (Snapshot Kosong)

- **Objective**: Memverifikasi ketahanan sistem ketika menampilkan order historis lama (sebelum migrasi) yang memiliki nilai `DisciplineName` atau `TestGroupName` kosong (`""` atau `null`).
- **Preconditions**:
  - Database memiliki data order historis lama (dapat disimulasikan melalui `DATA-HISTORIS-LEGACY`).
- **Steps**:
  1. Cari dan buka order historis lama pada panel rincian order `LabOrderDetailPanel.vue`.
  2. Buka lembar cetak slip untuk order tersebut.
- **Expected Result**:
  - Halaman rincian order dan slip cetak tidak mengalami crash / blank page (*no null reference errors*).
  - Item dengan nama disiplin/grup kosong secara elegan dikelompokkan di bawah header default: **"Pemeriksaan Lain-Lain"**.

---

## Kategori E: Proteksi Non-Darah & Regresi Sistem

---

### TC-LPAM-17: Verifikasi Kalkulator Flebotomi pada Keranjang Campuran Darah dan Non-Darah

- **Objective**: Memverifikasi bahwa dalam keranjang campuran (misal: Darah Lengkap + Feses Rutin + Swab Sekret), kalkulator flebotomi secara akurat HANYA menghitung tabung vacutainer darah dan tidak menambahkan tabung apa pun untuk feses atau swab.
- **Preconditions**:
  - Membuka `LabOrderCreateCanvas.vue`.
- **Test Data**:
  - `LAB0103002` (Hemoglobin, Darah EDTA, `Vacutainer: Ungu / EDTA = 3`)
  - `LAB0109001` (Faeces Rutin, Feses, `Vacutainer: None = 0`)
  - `LAB0116002` (Sekret 1X, Swab Cairan, `Vacutainer: None = 0`)
- **Steps**:
  1. Masukkan ketiga item tersebut ke dalam keranjang pemesanan.
  2. Amati panel kalkulator tabung di kanan bawah.
- **Expected Result**:
  - Total tabung yang terhitung hanya **1 Tabung Ungu (EDTA)** untuk Hemoglobin.
  - Faeces Rutin dan Sekret 1X tidak memicu penambahan tabung darah apa pun.
  - Terdapat indikator visual bahwa sampel non-darah menggunakan wadah khusus / pot spesimen.

---

### TC-LPAM-18: Verifikasi Batas Integritas Middleware OWARE (Zero Regressions)

- **Objective**: Memverifikasi bahwa seluruh penambahan kapabilitas ini tidak mengganggu pemrosesan antrean bridging LIS pada `BILRG_LabOwareOutboundQueue` dan endpoint bridging di `a046_oware_ordermiddlewareapi`.
- **Preconditions**:
  - Layanan OWARE aktif (atau inspeksi antrean outbound di database).
- **Steps**:
  1. Terbitkan order lab baru untuk item yang memiliki pemetaan `OWARE_MapTarifLab`.
  2. Periksa record yang terbentuk pada tabel `BILRG_LabOwareOutboundQueue`.
- **Expected Result**:
  - Payload antrean outbound OWARE terbentuk dengan struktur data yang valid dan kompatibel secara As-Is.
  - Tidak ada kolom atau kontrak yang melanggar spesifikasi penerimaan hasil LIS eksisting (`/api/result/lab/pa`, `/api/result/lab/micro`, `/api/result/lab/bulk`).

---

# 6. Test Result Recording & Defect Reporting

## 6.1 Format Pencatatan Hasil Uji
Hasil eksekusi setiap test case wajib dicatat oleh penguji manusia ke dalam artefak **`TEST-EXECUTION`** dengan pilihan status:
- **`PASS`**: Perilaku sistem yang teramati sesuai 100% dengan *Expected Result*.
- **`FAIL`**: Terjadi perbedaan perilaku, error sistem, kegagalan validasi, atau data tidak konsisten.
- **`BLOCKED`**: Pengujian tidak dapat dilanjutkan karena prasyarat lingkungan atau data uji belum terpenuhi.

## 6.2 Protokol Pelaporan Defek (Bila Terjadi FAIL)
Bila suatu skenario menghasilkan status **`FAIL`**, penguji wajib menyertakan bukti konkret:
1. **Identitas Test Case**: Misal `TC-LPAM-05`.
2. **Langkah yang Dijalankan**: Tindakan spesifik yang memicu kendala.
3. **Hasil yang Teramati (*Actual Result*)**: Screenshot layar, pesan galat, respon JSON API, atau rekaman log.
4. **Hasil yang Diharapkan (*Expected Result*)**: Sesuai klausul pada test case terkait.
5. **Tingkat Keparahan (*Severity*)**:
   - `BLOCKER`: Sistem crash, transaksi database korup, atau order hilang tanpa jejak.
   - `MAJOR`: Alur auto-split gagal memecah order, atau form polimorfik salah render.
   - `MINOR`: Kesalahan ketik visual pada label grup atau margin slip cetak.

Catatan defek tersebut selanjutnya diserahkan kepada peran **`ica-issuer`** untuk diterbitkan secara formal sebagai dokumen `ISSUE` bertipe `BUG`.
