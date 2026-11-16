---
Title: Test Package - Integrasi Penagihan Tindakan Medis dan BHP Kunjungan IGD ke Tata Rekening
Code: IGD-VISIT-BILLING-DISCONNECT
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-11-10
Status: Ready-For-Execution
Stage: Stage-8 (Testing)
Prerequisites: IMPLEMENTATION-PLAN (COMPLETED), ARCHITECTURE (Approved)
---

# TEST PACKAGE: Integrasi Penagihan Tindakan Medis dan BHP Kunjungan IGD ke Tata Rekening

| Parameter | Nilai / Deskripsi Acuan |
|---|---|
| **Kode Dokumen** | `IGD-VISIT-BILLING-DISCONNECT` |
| **Originating Bug Issue** | [`IGD-VISIT-BILLING-DISCONNECT-BUG-ISSUE.md`](file:///d:/project_aktif/project_MyHospitalWeb/b21-myhosweb-system/Issues/IGD-VISIT-BILLING-DISCONNECT-BUG-ISSUE.md) (`ISS-IGD-VISIT-001`) |
| **Status Rencana Kerja** | [`IGD-VISIT-BILLING-DISCONNECT-BUG-IMPLEMENTATION-PLAN.md`](file:///d:/project_aktif/project_MyHospitalWeb/b21-myhosweb-system/Issues/IGD-VISIT-BILLING-DISCONNECT-BUG-IMPLEMENTATION-PLAN.md) berstatus **`COMPLETED`** |
| **Arsitektur Acuan** | [`IGD-VISIT-BILLING-DISCONNECT-BUG-ARCHITECTURE.md`](file:///d:/project_aktif/project_MyHospitalWeb/b21-myhosweb-system/Issues/IGD-VISIT-BILLING-DISCONNECT-BUG-ARCHITECTURE.md) |
| **Target Antarmuka (UI)** | Web Front-End (`c012_myhospital_web`): Modul Konsol IGD / Triase IGD (`TriaseIGD.vue`), Panel Billing IGD (`BillingPanel.vue`), Form Input Tindakan/BHP (`EmergencyBillingForm.vue`), Kasir Rawat Jalan/IGD (`TataRekeningWorkspace.vue`) |
| **Target Layanan Backend** | API Backend (`b09-bilreg-api`): `IgdVisitAssignRegisterCmd`, `IgdTindakanAddCmd`, `IgdBhpAddCmd`, `IgdVisitRetryBillingPostingCmd`, `IgdVisitDischargeCmd`, `IgdTindakanVoidCmd`, `IgdBillingPostingOrchestrator` |
| **Target Basis Data** | SQL Server `HOSPITAL_PKL`: `BILRG_TindakanIgd`, `BILRG_BhpIgd`, `BILRG_IgdVisit`, `ta_trs_billing`, `ta_trs_billing2`, `ta_trs_tindakan` |
| **Pelaksana Uji** | Penguji Manusia (*Human Tester*: Perawat/Dokter IGD, Staf Kasir/Tata Rekening, Staf Admisi, Tim QA/EDP) didampingi agen `ica-tester` |

---

# 1. Panduan Penguji (Tester Guidance)

Paket pengujian ini disusun untuk memandu penguji manusia (*human tester*) dalam memverifikasi secara langsung dan menyeluruh bahwa seluruh tindakan medis darurat dan pemakaian bahan medis habis pakai (BHP) pada pasien Instalasi Gawat Darurat (IGD) berhasil dibukukan secara akurat ke dalam rekening kasir pasien di Tata Rekening:

1. **Prinsip Keterbacaan & Bahasa Operasional**:
   - Seluruh langkah pengujian dirancang dengan instruksi operasional pelayanan rumah sakit tanpa mengharuskan pemahaman teknis pemrograman.
   - Pengujian berfokus pada **perilaku yang teramati (*observable behavior*)** di layar antarmuka pengguna (UI) IGD dan Kasir, serta verifikasi fisik data pada basis data.

2. **Dua Mode Penagihan yang Diuji**:
   - **Mode Buffer Pra-Registrasi (*Clinical Flow First*)**: Pasien darurat ditangani segera saat belum memiliki nomor registrasi administratif (`RegId = "-"`). Tindakan dan BHP dapat disimpan di IGD dengan badge "Buffer Pra-Registrasi". Tagihan baru diposting ke kasir secara otomatis saat registrasi resmi ditautkan via menu *Tautkan Registrasi*.
   - **Mode Direct Charge Capture Pasca-Registrasi**: Pasien yang sudah memiliki nomor registrasi resmi langsung meneruskan setiap tindakan dan BHP yang diinput ke kasir seketika (*instant posting*) dengan badge indikator "Terposting Langsung ke Kasir".

3. **Mekanisme Observabilitas & Ketahanan (Resilience)**:
   - Jika terdapat kegagalan penentuan tarif (misal: tarif belum dikonfigurasi untuk jaminan tertentu), sistem **tidak mengalami crash**, melainkan memunculkan badge peringatan mencolok **`Tagihan Gantung`** dan menyediakan tombol **`Retry Posting Tagihan`**.
   - Pemulangan klinis (*Discharge*) di IGD bersifat **non-blocking**: perawat tetap dapat memulangkan pasien secara medis dan melepaskan tempat tidur observasi meskipun terdapat tagihan gantung, dengan flag peringatan yang jelas agar kasir dapat melakukan rekonsiliasi.

4. **Kriteria Evaluasi Hasil Uji**:
   - **PASS**: Seluruh perilaku pada butir *"Hasil yang Diharapkan"* teramati dengan tepat, responsif, badge status tampil sesuai keadaan, rincian biaya muncul di kasir, dan tidak terjadi galat tak terduga.
   - **FAIL**: Terjadi anomali (misal: tindakan/BHP tidak muncul di rincian kasir setelah registrasi ditautkan, tombol retry gagal memproses, status tagihan tidak terupdate, atau terjadi galat sistem yang memblokir alur pelayanan). Catat bukti tangkapan layar, nomor kunjungan/registrasi, dan pesan galat pada lembar uji `TEST-EXECUTION`.

---

# 2. Persiapan Data & Lingkungan Uji (Prerequisites & Test Data)

Sebelum memulai pengujian, pastikan lingkungan uji telah memenuhi kriteria berikut:

1. **Aplikasi Backend & Frontend Aktif**:
   - Backend API (`b09-bilreg-api`) aktif pada branch `arf_igdTriage` dan terhubung ke database `HOSPITAL_PKL`.
   - Frontend Web (`c012_myhospital_web`) aktif pada branch `arf_igdTriage` dan dapat diakses melalui peramban (Chrome / Edge).
2. **Pengguna Uji & Hak Akses**:
   - Akun Perawat/Dokter IGD: Memiliki hak akses membuka konsol Triase IGD (`/emergency`).
   - Akun Petugas Kasir: Memiliki hak akses modul Tata Rekening (`/tata-rekening`).
   - Akun Petugas Admisi: Memiliki hak akses modul Pendaftaran Gawat Darurat (`/admisi`).
3. **Master Data Layanan & Tarif**:
   - Master Tindakan IGD dengan tarif aktif:
     - Tindakan A: **Pemasangan Infus Dewasa** (memiliki tarif Umum dan BPJS).
     - Tindakan B: **Jahit Luka / Hecting 1-5 Jahitan** (memiliki komponen Jasa Medis & Sarana).
     - Tindakan C: **EKG 12 Lead** (tindakan diagnostik darurat).
   - Master BHP / Barang Medis IGD:
     - BHP 1: **Infus Ringer Lactate (RL) 500ml** (Satuan: Botol/Kolf).
     - BHP 2: **IV Catheter / Abocath No. 20** (Satuan: Pcs).
     - BHP 3: **Kassa Steril 16x16** (Satuan: Lembar/Pcs).
     - BHP 4: **Spuit 5cc Terumo** (Satuan: Pcs).
4. **Alat Bantu Verifikasi Basis Data (SQL Query)**:
   - Tersedia akses SQL Management Studio / DBeaver / terminal query untuk memverifikasi fisik data penagihan:
     ```sql
     -- 1. Verifikasi Status Kunjungan IGD
     SELECT IgdVisitId, RegId, PasienId, PasienNm, TriaseKategori, HasPendingBilling, Status
     FROM dbo.BILRG_IgdVisit
     WHERE IgdVisitId = 'ID_KUNJUNGAN_UJI';

     -- 2. Verifikasi Tracking Tindakan IGD
     SELECT TindakanIgdId, IgdVisitId, RegId, TindakanId, TindakanNm, TrsTdkId, TrsBillId, IsPosted, PostedDate, IsVoid
     FROM dbo.BILRG_TindakanIgd
     WHERE IgdVisitId = 'ID_KUNJUNGAN_UJI';

     -- 3. Verifikasi Tracking BHP IGD
     SELECT BhpIgdId, IgdVisitId, RegId, BarangId, BarangNm, Qty, TrsBillId, IsPosted, PostedDate
     FROM dbo.BILRG_BhpIgd
     WHERE IgdVisitId = 'ID_KUNJUNGAN_UJI';

     -- 4. Verifikasi Buku Rekening Kasir di Tata Rekening
     SELECT fs_kd_trs, fs_kd_reg, fs_kd_tarif, fn_nilai, fn_qty, fn_diskon, fn_sub_total, fs_kd_petugas
     FROM dbo.ta_trs_billing
     WHERE fs_kd_reg = 'NOMOR_REGISTRASI_UJI';

     -- 5. Verifikasi Dekomposisi Komponen Biaya (ta_trs_billing2)
     SELECT fs_kd_trs, fs_kd_komponen, fn_nilai_komp
     FROM dbo.ta_trs_billing2
     WHERE fs_kd_trs IN (
         SELECT fs_kd_trs FROM dbo.ta_trs_billing WHERE fs_kd_reg = 'NOMOR_REGISTRASI_UJI'
     );
     ```

---

# 3. Matriks Skenario Uji (Test Suites Overview)

| Suite | Fokus Skenario Pengujian | Jumlah Kasus Uji |
|:---:|---|:---:|
| **Suite 1** | Buffer Klinis Pra-Registrasi & Otomasi Posting saat Penautan Registrasi | 2 Kasus Uji |
| **Suite 2** | Direct Charge Capture Pasca-Registrasi (Direct Bill Item ke Kasir) | 2 Kasus Uji |
| **Suite 3** | Observabilitas Tagihan Gantung & Mekanisme Retry Posting | 2 Kasus Uji |
| **Suite 4** | Pemulangan Klinis Non-Blocking (Discharge Resilience) | 2 Kasus Uji |
| **Suite 5** | Pembatalan Tindakan Berizin & Cascade Void ke Kasir | 2 Kasus Uji |
| **Suite 6** | Verifikasi Multi-Penjamin & Integritas Varian Tarif 3-Dimensi | 1 Kasus Uji |

**Total Skenario Uji**: **11 Kasus Uji Terstruktur**.

---

# 4. Rincian Kasus Uji (Test Cases Breakdown)

---

## SUITE 1: Buffer Klinis Pra-Registrasi & Otomasi Posting saat Penautan Registrasi

### TC-IGD-BILL-01: Pencatatan Tindakan Medis & Pemakaian BHP pada Pasien Pra-Registrasi (Buffer Klinis)
- **Tujuan**: Memverifikasi bahwa perawat/dokter dapat mencatat tindakan darurat dan konsumsi BHP pada pasien gawat darurat yang baru masuk IGD dan belum memiliki nomor registrasi resmi tanpa diblokir oleh sistem (*Clinical Flow First*).
- **Prasyarat**: Pasien darurat baru (misal: "Tn. Darurat Tanpa Reg") telah didaftarkan triasenya di Konsol IGD, berstatus aktif (`Triase Selesai`), dan belum memiliki nomor registrasi (`RegId = "-"`).
- **Langkah Uji**:
  1. Buka modul **Konsol IGD / Triase IGD** (`/emergency`).
  2. Klik kartu kunjungan pasien "Tn. Darurat Tanpa Reg".
  3. Buka tab / panel **Billing / Tindakan & BHP**.
  4. Amati tampilan formulir input:
     - Pastikan terdapat badge/indikator **"Buffer Pra-Registrasi"**.
  5. Pada form tindakan, pilih tindakan medis: **"Pemasangan Infus Dewasa"**, pilih dokter/pelaksana, isi catatan, lalu klik **Simpan Tindakan**.
  6. Pada form BHP, pilih bahan medis: **"Infus Ringer Lactate 500ml"** (Qty: 2) dan **"IV Catheter No. 20"** (Qty: 1), lalu klik **Simpan BHP**.
  7. Amati daftar riwayat tindakan & BHP pada timeline event.
  8. Jalankan query database nomor 2 dan 3 untuk kunjungan tersebut.
- **Hasil yang Diharapkan**:
  - Tindakan dan BHP berhasil disimpan tanpa pesan galat.
  - Pada timeline riwayat muncul badge **"Pending Kasir"** (warna amber) di samping item tindakan dan BHP.
  - Pada basis data (`BILRG_TindakanIgd` & `BILRG_BhpIgd`):
    - Nilai `RegId` tersimpan sebagai `"-"`.
    - Kolom `IsPosted` bernilai `0` (false).
    - Kolom `TrsBillId` dan `TrsTdkId` bernilai `NULL`.
  - Belum ada baris tagihan yang masuk ke tabel `ta_trs_billing` di Tata Rekening (kasir belum terbebani sebelum registrasi sah).

---

### TC-IGD-BILL-02: Penautan Nomor Registrasi Resmi Memproses Posting Otomatis ke Kasir Tata Rekening
- **Tujuan**: Memastikan bahwa ketika nomor registrasi administratif diterbitkan dan ditautkan ke kunjungan IGD, sistem secara otomatis memperbarui `RegId` pada seluruh record tindakan dan BHP aktif, me-resolve varian tarif, serta membukukannya ke rekening kasir pasien.
- **Prasyarat**: Pasien dari TC-IGD-BILL-01 telah selesai didaftarkan di Admisi IGD dan memperoleh nomor registrasi resmi (misal: `RG261110-001`, Penjamin: Umum / Mandiri).
- **Langkah Uji**:
  1. Pada Konsol IGD, pilih pasien "Tn. Darurat Tanpa Reg".
  2. Klik tombol aksi **Tautkan Registrasi** (*Assign Register*).
  3. Masukkan nomor registrasi resmi `RG261110-001` (atau pilih dari daftar registrasi aktif), lalu klik **Konfirmasi Tautkan**.
  4. Perhatikan notifikasi dan pembaruan antarmuka:
     - Amati nomor registrasi pada header kunjungan.
     - Amati status item tindakan dan BHP pada panel billing dan timeline event.
  5. Buka modul **Kasir / Tata Rekening** (`/tata-rekening`) pada tab Rawat Jalan / IGD.
  6. Cari nomor registrasi `RG261110-001`.
  7. Buka rincian rekening tagihan pasien tersebut.
  8. Jalankan query database nomor 2, 3, 4, dan 5.
- **Hasil yang Diharapkan**:
  - Penautan registrasi berhasil dan nomor registrasi `RG261110-001` terpampang pada kunjungan IGD.
  - Badge pada rincian tindakan dan BHP di Konsol IGD berubah dari "Pending Kasir" menjadi **"Terposting ke Kasir"** (warna hijau emerald).
  - Badge peringatan "Tagihan Gantung" **tidak muncul** (karena semua item berhasil terposting).
  - Pada basis data (`BILRG_TindakanIgd` & `BILRG_BhpIgd`):
    - Nilai `RegId` terupdate menjadi `RG261110-001`.
    - Nilai `IsPosted` menjadi `1` (true), dan `PostedDate` terisi waktu saat penautan.
    - Nilai `TrsBillId` dan `TrsTdkId` terisi ID transaksi penagihan yang valid.
  - Pada modul Kasir / Tata Rekening:
    - Muncul baris tagihan tindakan "Pemasangan Infus Dewasa" dengan nilai tarif yang sesuai.
    - Muncul baris tagihan BHP "Infus Ringer Lactate 500ml" (Qty: 2) dan "IV Catheter No. 20" (Qty: 1) dengan subtotal yang akurat.
    - Dekomposisi komponen biaya di `ta_trs_billing2` terbentuk secara lengkap.

---

## SUITE 2: Direct Charge Capture Pasca-Registrasi (Direct Bill Item ke Kasir)

### TC-IGD-BILL-03: Pencatatan Tindakan Medis Pasca-Registrasi Terposting Seketika ke Kasir
- **Tujuan**: Memverifikasi bahwa penambahan tindakan medis pada pasien IGD yang sudah memiliki nomor registrasi sah langsung diposting seketika (*direct charge capture*) ke rekening kasir tanpa perlu penautan ulang.
- **Prasyarat**: Pasien IGD telah memiliki nomor registrasi aktif (misal `RG261110-001`).
- **Langkah Uji**:
  1. Buka Konsol IGD dan pilih pasien `RG261110-001`.
  2. Buka tab **Billing / Tindakan & BHP**.
  3. Amati formulir input:
     - Terdapat badge/banner **"Terposting Langsung ke Kasir"** dengan keterangan: *"Tindakan dan BHP yang disimpan langsung diteruskan ke billing kasir."*
  4. Input tindakan medis baru: **"Jahit Luka / Hecting 1-5 Jahitan"**, pilih dokter pelaksana, lalu klik **Simpan Tindakan**.
  5. Perhatikan notifikasi sukses dan item pada daftar riwayat tindakan.
  6. Buka rincian rekening tagihan di modul Kasir / Tata Rekening untuk registrasi `RG261110-001`.
- **Hasil yang Diharapkan**:
  - Tindakan medis berhasil disimpan dan langsung diberi badge status **"Terposting ke Kasir"** (hijau).
  - Pada tabel `BILRG_TindakanIgd`, kolom `IsPosted` langsung bernilai `1` dan `TrsBillId` terisi.
  - Pada rincian kasir di Tata Rekening, baris tagihan "Jahit Luka / Hecting 1-5 Jahitan" langsung muncul seketika beserta komponen jasa medis dan sarana terkait.

---

### TC-IGD-BILL-04: Pencatatan BHP Medis Pasca-Registrasi Terposting Seketika ke Kasir
- **Tujuan**: Memverifikasi bahwa penambahan bahan medis habis pakai (BHP) pada pasien IGD pasca-registrasi langsung dibukukan seketika ke kasir sebagai item tagihan obat/alkes.
- **Prasyarat**: Pasien IGD telah memiliki nomor registrasi aktif `RG261110-001`.
- **Langkah Uji**:
  1. Pada panel billing kunjungan pasien `RG261110-001`, buka form pemakaian BHP.
  2. Pilih barang: **"Kassa Steril 16x16"** (Qty: 5 lembar) dan **"Spuit 5cc Terumo"** (Qty: 2 pcs).
  3. Klik tombol **Simpan BHP**.
  4. Periksa timeline riwayat BHP pada Konsol IGD.
  5. Periksa layar rincian tagihan kasir di Tata Rekening.
- **Hasil yang Diharapkan**:
  - Item pemakaian BHP berhasil disimpan dan langsung berstatus **"Terposting ke Kasir"**.
  - Pada basis data `BILRG_BhpIgd`, kolom `IsPosted = 1` dan `TrsBillId` terisi.
  - Pada modul Kasir, muncul baris tagihan kelompok BHP/Obat untuk "Kassa Steril 16x16" dan "Spuit 5cc Terumo" dengan nilai perkalian harga satuan dan kuantitas yang benar.

---

## SUITE 3: Observabilitas Tagihan Gantung & Mekanisme Retry Posting

### TC-IGD-BILL-05: Penanganan Transaksi Gagal Tarif Menampilkan Badge "Tagihan Gantung"
- **Tujuan**: Memverifikasi bahwa ketika terjadi anomali tarif (misal master tarif belum memiliki nilai untuk kelas/penjamin pasien), sistem tidak mengalami kegagalan fatal/crash, melainkan menandai kunjungan dengan flag observabilitas "Tagihan Gantung" dan mengizinkan alur klinis terus berjalan.
- **Prasyarat**: Pasien darurat baru di IGD didaftarkan dengan tindakan yang tarifnya sengaja belum didefinisikan untuk jaminannya (atau simulasi nilai tarif kosong). Pasien kemudian ditautkan ke nomor registrasi.
- **Langkah Uji**:
  1. Buat kunjungan darurat baru tanpa registrasi.
  2. Input tindakan yang belum memiliki varian tarif aktif untuk jaminan tertentu.
  3. Lakukan penautan nomor registrasi administratif ke kunjungan tersebut.
  4. Amati respons sistem pada Konsol IGD:
     - Periksa notifikasi toast.
     - Periksa tampilan header kartu kunjungan dan panel billing.
  5. Buka daftar antrean kunjungan IGD (`IgdVisitList`).
  6. Jalankan query database nomor 1 pada tabel `BILRG_IgdVisit`.
- **Hasil yang Diharapkan**:
  - Proses penautan registrasi **tetap sukses** (penautan registrasi tidak di-rollback demi keselamatan alur administratif).
  - Pada header kartu kunjungan dan rincian billing muncul badge peringatan mencolok berwarna amber/kuning: **`Tagihan Gantung`** dengan ikon peringatan.
  - Pada panel billing tampil seksi peringatan khusus: *"Terdapat tagihan yang belum berhasil terposting ke kasir."* disertai tombol aktif **`Retry Posting Tagihan`**.
  - Pada basis data `BILRG_IgdVisit`, kolom `HasPendingBilling` bernilai `1` (true).
  - Item tindakan yang gagal tetap berstatus `IsPosted = 0` ("Pending Kasir").

---

### TC-IGD-BILL-06: Eksekusi Tombol "Retry Posting Tagihan" Berhasil Menyelesaikan Tagihan Gantung
- **Tujuan**: Memastikan tombol "Retry Posting Tagihan" dapat diklik oleh petugas setelah masalah tarif/koneksi terselesaikan, memicu orkestrator posting ulang secara tuntas, dan membersihkan badge tagihan gantung.
- **Prasyarat**: Kunjungan dari TC-IGD-BILL-05 berada pada status `HasPendingBilling = true`. Master tarif yang bermasalah telah diperbaiki/dikonfigurasi dengan benar oleh tim tarif/admin.
- **Langkah Uji**:
  1. Pada Konsol IGD, buka kunjungan pasien yang memiliki badge "Tagihan Gantung".
  2. Buka panel billing dan temukan tombol **Retry Posting Tagihan**.
  3. Klik tombol **Retry Posting Tagihan**.
  4. Amati perilaku tombol:
     - Tombol berubah ke keadaan loading dengan spinner dan teks *"Memproses Posting..."*.
     - Tombol berada dalam kondisi dinonaktifkan (*disabled*) selama proses berjalan untuk mencegah klik ganda (*double-submit*).
  5. Amati hasil akhir setelah mutasi selesai:
     - Perhatikan notifikasi toast.
     - Perhatikan badge "Tagihan Gantung".
     - Periksa status item pada riwayat tindakan.
  6. Jalankan query database nomor 1 dan 2.
- **Hasil yang Diharapkan**:
  - Muncul notifikasi toast sukses: *"Retry posting tagihan berhasil diproses"*.
  - Badge peringatan **"Tagihan Gantung" hilang** dari header kartu kunjungan dan daftar kunjungan IGD.
  - Status item tindakan pada timeline event berubah menjadi **"Terposting ke Kasir"** (hijau).
  - Pada tabel `BILRG_IgdVisit`, kolom `HasPendingBilling` berubah kembali menjadi `0` (false).
  - Pada modul Kasir / Tata Rekening, tagihan tindakan tersebut kini telah resmi muncul di rekening pasien.

---

## SUITE 4: Pemulangan Klinis Non-Blocking (Discharge Resilience)

### TC-IGD-BILL-07: Pemulangan Pasien IGD (*Discharge*) dengan Kondisi Tagihan Gantung Bersifat Non-Blocking
- **Tujuan**: Memverifikasi bahwa kepulangan klinis pasien IGD (pelepasan tempat tidur observasi & selesai perawatan darurat) tidak di-hard block oleh keberadaan transaksi tagihan gantung, dengan flag observabilitas tetap terjaga untuk rekonsiliasi kasir.
- **Prasyarat**: Pasien IGD telah selesai menjalani observasi klinis dan diputuskan pulang/rawat jalan oleh dokter, namun masih memiliki flag `HasPendingBilling = true` (misal dari kasus TC-IGD-BILL-05). Pasien saat ini menempati Bed Observasi IGD (misal Bed 02).
- **Langkah Uji**:
  1. Buka Konsol IGD dan pilih pasien tersebut.
  2. Klik tombol aksi **Pulangkan Pasien / Discharge**.
  3. Pada dialog konfirmasi pemulangan:
     - Pilih cara keluar: **Atas Persetujuan Dokter (Sembuh/Membaik)**.
     - Isi kondisi keluar dan edukasi kepulangan.
     - Klik **Konfirmasi Pemulangan**.
  4. Amati respons sistem:
     - Apakah proses pemulangan berhasil atau terjadi lemparan error penolakan kasir?
     - Periksa status ketersediaan Bed Observasi 02 pada denah bed IGD.
     - Periksa kartu kunjungan pada tab riwayat kepulangan / pasien keluar.
  5. Jalankan query database nomor 1.
- **Hasil yang Diharapkan**:
  - Proses pemulangan klinis **berhasil diproses** tanpa terblokir (*non-blocking discharge*).
  - Status kunjungan pasien berubah menjadi **`Discharged`**.
  - Bed Observasi 02 berhasil dilepaskan (*vacated/ready*) dan siap digunakan oleh pasien darurat berikutnya.
  - Flag `HasPendingBilling` pada entitas kunjungan **tetap bernilai `1` (true)** sehingga petugas kasir/keuangan tetap dapat melacak dan melakukan retry rekonsiliasi.

---

### TC-IGD-BILL-08: Pemulangan Pasien IGD dengan Seluruh Tagihan Terposting Selesai Bersih (*Zero Pending*)
- **Tujuan**: Memverifikasi bahwa pasien IGD yang seluruh tindakan dan BHP-nya telah berhasil terposting dapat dipulangkan dengan status bersih (*Zero Pending*) tanpa memunculkan bendera tagihan gantung.
- **Prasyarat**: Pasien IGD yang seluruh tindakan dan BHP-nya berstatus `IsPosted = true` (dari TC-IGD-BILL-02 atau TC-IGD-BILL-03) menempati Bed Observasi 01.
- **Langkah Uji**:
  1. Buka Konsol IGD dan pilih pasien tersebut.
  2. Klik tombol aksi **Pulangkan Pasien / Discharge**.
  3. Isi form pemulangan dan konfirmasi kepulangan.
  4. Periksa status kunjungan dan status bed pada sistem.
  5. Buka modul Kasir untuk registrasi pasien tersebut.
- **Hasil yang Diharapkan**:
  - Kunjungan berhasil diselesaikan dengan status `Discharged`.
  - Bed 01 berstatus kosong/siap pakai.
  - Flag `HasPendingBilling = 0` (tidak ada tagihan gantung).
  - Seluruh transaksi di kasir siap diproses untuk penerbitan kuitansi pelunasan kasir atau pengajuan klaim penjamin.

---

## SUITE 5: Pembatalan Tindakan Berizin & Cascade Void ke Kasir

### TC-IGD-BILL-09: Pembatalan Tindakan Terposting Mengeksekusi Cascade Void ke Rekening Kasir
- **Tujuan**: Memverifikasi bahwa pembatalan tindakan medis IGD yang telah terposting ke kasir secara otomatis membatalkan (*cascade void*) baris transaksi tagihan di `ta_trs_billing` & `ta_trs_billing2` selama status rekening kasir belum final/lunas.
- **Prasyarat**: Pasien IGD memiliki tindakan "EKG 12 Lead" yang sudah berstatus `IsPosted = 1` pada registrasi `RG261110-001`. Status billing di kasir masih berstatus aktif/draft (*Open*).
- **Langkah Uji**:
  1. Periksa rincian tagihan di modul Kasir: pastikan item "EKG 12 Lead" tercantum dengan nilai nominalnya.
  2. Kembali ke modul Konsol IGD, buka kunjungan pasien tersebut.
  3. Pada panel rincian tindakan, cari item "EKG 12 Lead".
  4. Klik tombol **Batal Tindakan / Void**.
  5. Masukkan alasan pembatalan: *"Salah input tindakan medis oleh petugas"*, lalu konfirmasi.
  6. Perhatikan respons sistem di Konsol IGD.
  7. Buka kembali modul Kasir / Tata Rekening dan refresh rincian tagihan registrasi tersebut.
  8. Jalankan query database nomor 2 dan 4.
- **Hasil yang Diharapkan**:
  - Tindakan "EKG 12 Lead" di Konsol IGD berhasil dibatalkan dan ditandai sebagai *Void*.
  - Pada basis data `BILRG_TindakanIgd`, kolom `IsVoid = 1` dengan alasan pembatalan tersimpan.
  - Di modul Kasir / Tata Rekening, baris tagihan "EKG 12 Lead" pada `ta_trs_billing` dan rincian komponen di `ta_trs_billing2` **telah terhapus / dibatalkan secara kaskade** sehingga total saldo tagihan kasir berkurang secara otomatis.

---

### TC-IGD-BILL-10: Penolakan Pembatalan Tindakan IGD saat Rekening Kasir Sudah Berstatus Final atau Lunas
- **Tujuan**: Memastikan guard validasi penagihan (`BillingLockValidationService`) bekerja dengan benar untuk menolak pembatalan tindakan medis di IGD jika transaksi kasir untuk kunjungan tersebut telah berstatus `Final` atau `Lunas` (*Financial Lock Protection*).
- **Prasyarat**: Pasien IGD telah menyelesaikan administrasi pembayaran di kasir, dan kasir telah mengunci tagihan menjadi status **`Final`** atau **`Lunas`** (kuitansi pelunasan telah dicetak).
- **Langkah Uji**:
  1. Pada modul Kasir, pastikan rekening registrasi pasien sudah berstatus `Final` / `Lunas`.
  2. Buka modul Konsol IGD dan pilih pasien tersebut.
  3. Buka panel riwayat tindakan.
  4. Coba lakukan klik tombol **Batal Tindakan / Void** pada salah satu tindakan yang sudah terposting.
  5. Isi alasan pembatalan dan klik **Konfirmasi Batal**.
  6. Amati pesan peringatan/galat yang dimunculkan sistem.
- **Hasil yang Diharapkan**:
  - Sistem **menolak pembatalan tindakan** dengan memunculkan pesan peringatan jelas (misal: *"Tindakan tidak dapat dibatalkan karena rekening tagihan kasir telah Final atau Lunas"*).
  - Status tindakan di IGD tetap aktif (tidak menjadi void).
  - Integritas data keuangan di kasir tetap terlindungi tanpa risiko pembatalan sepihak setelah pembayaran diterima.

---

## SUITE 6: Verifikasi Multi-Penjamin & Integritas Varian Tarif 3-Dimensi

### TC-IGD-BILL-11: Verifikasi Varian Tarif 3D untuk Pasien Umum vs Pasien BPJS/Asuransi pada Snapshot Billing
- **Tujuan**: Memverifikasi bahwa formula varian tarif 3-dimensi ($\mathbf{TarifId} \times \mathbf{Kelas} \times \mathbf{TipePenjamin}$) diterapkan dengan benar saat posting penagihan tindakan IGD, sehingga tarif yang dibebankan kepada pasien Umum berbeda secara sah dengan tarif pasien BPJS/Asuransi sesuai master tarif rumah sakit.
- **Prasyarat**:
  - Kunjungan Uji 1: Pasien Umum (Penjamin: Umum/Mandiri).
  - Kunjungan Uji 2: Pasien BPJS (Penjamin: BPJS Kesehatan).
  - Kedua pasien diberikan tindakan medis yang identik di IGD: **"Jahit Luka / Hecting 1-5 Jahitan"**.
- **Langkah Uji**:
  1. Input tindakan "Jahit Luka / Hecting 1-5 Jahitan" pada pasien Kunjungan Uji 1 (Umum) pasca-registrasi.
  2. Input tindakan "Jahit Luka / Hecting 1-5 Jahitan" pada pasien Kunjungan Uji 2 (BPJS) pasca-registrasi.
  3. Buka rincian rekening kasir di Tata Rekening untuk masing-masing pasien.
  4. Bandingkan nilai nominal total tarif dan komponen biaya pada tabel `ta_trs_billing` & `ta_trs_billing2`.
- **Hasil yang Diharapkan**:
  - Pasien Kunjungan Uji 1 menerima beban tagihan sesuai master tarif IGD kelas rawat untuk Penjamin Umum.
  - Pasien Kunjungan Uji 2 menerima beban tagihan sesuai master tarif IGD kelas rawat untuk Penjamin BPJS.
  - Nilai snapshot tarif terkunci secara permanen dan tidak berubah bila di kemudian hari master tarif disesuaikan.

---

# 5. Lembar Rekapitulasi Pelaksanaan Uji (Execution Checklist)

Penguji dapat menggunakan lembar rekapitulasi berikut saat mendokumentasikan hasil pengujian bersama agen `ica-tester`:

| ID Kasus Uji | Skenario Pengujian | Hasil (PASS/FAIL) | Catatan / Bukti Temuan | Paraf Penguji |
|---|---|:---:|---|:---:|
| **TC-IGD-BILL-01** | Buffer Klinis Pra-Registrasi (Tindakan & BHP tanpa RegId) | [ ] | | |
| **TC-IGD-BILL-02** | Otomasi Posting ke Kasir saat Registrasi Ditautkan | [ ] | | |
| **TC-IGD-BILL-03** | Direct Charge Capture Tindakan Medis Pasca-Registrasi | [ ] | | |
| **TC-IGD-BILL-04** | Direct Charge Capture BHP Pasca-Registrasi | [ ] | | |
| **TC-IGD-BILL-05** | Observabilitas Flag & Badge "Tagihan Gantung" saat Gagal Tarif | [ ] | | |
| **TC-IGD-BILL-06** | Eksekusi Tombol Retry Posting Menyelesaikan Tagihan Gantung | [ ] | | |
| **TC-IGD-BILL-07** | Pemulangan Klinis Non-Blocking saat Ada Tagihan Gantung | [ ] | | |
| **TC-IGD-BILL-08** | Pemulangan Klinis Bersih (*Zero Pending*) | [ ] | | |
| **TC-IGD-BILL-09** | Pembatalan Tindakan Mengeksekusi Cascade Void ke Kasir | [ ] | | |
| **TC-IGD-BILL-10** | Penolakan Pembatalan Tindakan saat Rekening Kasir Final/Lunas | [ ] | | |
| **TC-IGD-BILL-11** | Verifikasi Varian Tarif 3D (Umum vs BPJS) | [ ] | | |

---

# 6. Tahapan Selanjutnya (Next Workflow Stage)

Setelah paket pengujian ini disahkan:
1. **Human Test Execution**: Penguji manusia melakukan pengujian pada lingkungan operasional pengujian (*staging/development*) mengikuti panduan langkah-langkah di atas.
2. **Testing Assistance**: Agen `ica-tester` (dengan skill `ics-testing-assistance`) mencatat setiap hasil per kasus uji, melampirkan bukti pengujian, serta mendokumentasikan temuan cacat bila ada ke dalam dokumen artefak **`TEST-EXECUTION.md`**.
3. **Gate Approval**: Gerbang kelulusan `TEST PASSED` hanya dapat disahkan apabila seluruh kasus uji (11 kasus uji) memperoleh hasil **PASS**.
