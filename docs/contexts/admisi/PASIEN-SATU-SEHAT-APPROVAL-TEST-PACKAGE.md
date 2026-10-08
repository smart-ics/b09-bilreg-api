---
Title: Test Package - Integrasi Data Status Satu Sehat Pasien dan Konfirmasi Persetujuan Upload pada Registrasi Admisi
Code: PASIEN-SATU-SEHAT-APPROVAL
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-10-08
Status: READY-FOR-EXECUTION
Target Plan: PASIEN-SATU-SEHAT-APPROVAL-IMPLEMENTATION-PLAN.md
---

# 1. Overview & Objective

Dokumen ini merupakan **TEST-PACKAGE** resmi untuk memvalidasi implementasi fitur **Integrasi Data Status Satu Sehat Pasien dan Konfirmasi Persetujuan Upload pada Registrasi Admisi** (`PASIEN-SATU-SEHAT-APPROVAL`).

Tujuan pengujian adalah memastikan bahwa:
1. Indikator visual status Satu Sehat (`#SatuSehat`) tampil akurat dan reaktif pada antarmuka loket registrasi admisi.
2. Alur persetujuan upload Satu Sehat berjalan tepat sasaran berdasarkan jenis jaminan pasien:
   - **BPJS**: Otomatis disetujui (*auto-approval*) tanpa memunculkan dialog konfirmasi.
   - **Non-BPJS**: Memunculkan dialog konfirmasi persetujuan (*informed consent*) yang dapat disetujui atau dibatalkan oleh petugas.
3. Pasien yang sudah pernah disetujui upload tidak lagi memunculkan dialog konfirmasi maupun auto-approval ulang.
4. Kebijakan toleransi kesalahan (*fail-safe non-blocking / zero-blockage policy*) berjalan sempurna: kegagalan teknis pada lookup jaminan atau API persetujuan Satu Sehat tidak boleh menghalangi proses simpan pendaftaran pasien di loket admisi.

Dokumen ini disusun agar dapat dieksekusi secara langsung oleh penguji manusia (*Tester, QA, Trainer, atau User*) tanpa memerlukan pemahaman mendalam tentang arsitektur kode internal.

---

# 2. Scope & Testing Principles

## Lingkup Pengujian
- **In-Scope**:
  - Visualisasi label status `#SatuSehat` dan tooltip informasi pada kartu data pasien di modul Admisi.
  - Alur pra-simpan registrasi admisi rawat jalan saat tombol "Simpan Registrasi" ditekan.
  - Logika pemisahan jaminan BPJS vs Non-BPJS melalui integrasi mapping JetliApi.
  - Interaksi modal dialog konfirmasi persetujuan Satu Sehat untuk pasien Non-BPJS.
  - Mekanisme pembaruan status dan persistensi *Upsert* data Satu Sehat pasien di database (`tc_mr_saset`).
  - Ketahanan sistem (*resilience & fail-safe*) terhadap gangguan jaringan/timeout layanan eksternal.
- **Out-of-Scope**:
  - Pengiriman fisik berkas FHIR/HL7 ke server Cloud Kemenkes Satu Sehat (dikelola oleh service background terpisah).
  - Persetujuan *View* data Satu Sehat (`IsApprovedView`) dan manajemen berkas scan *General Consent*.
  - Alur kasir, billing, farmasi, atau laboratorium setelah pendaftaran selesai.

## Prinsip Pengujian
- Pengujian difokuskan pada **perilaku yang dapat diamati secara visual dan operasional** (*observable behavior*).
- Setiap langkah pengujian dirancang dengan instruksi konkret tanpa asumsi teknis.
- Validasi hasil menguji skenario positif (*Happy Path*), aturan bisnis (*Business Rules*), penanganan galat (*Error Handling & Resilience*), dan risiko regresi (*Regression Risks*).

---

# 3. Environment & Prerequisites

## 3.1 Kebutuhan Lingkungan & Hak Akses
- **Aplikasi Web**: Frontend `c012_myhospital_web` berjalan di browser (Chrome / Edge / Firefox) dan terhubung ke backend API.
- **Layanan Backend**:
  - `b09-bilreg-api` aktif dan terhubung ke database operasional SQL Server.
  - `jetliApi` aktif untuk endpoint `/JetliAPi/api/GrupJaminan/map`.
- **Akun Penguji**: Akun pengguna aplikasi yang memiliki hak akses (*role*) **Petugas Loket Admisi / Pendaftaran Rawat Jalan**.

## 3.2 Kebutuhan Data Uji (Test Data)
Penguji menyiapkan atau memilih data pasien dan master data berikut di lingkungan pengujian:

| ID Data Uji | Deskripsi Data Pasien / Parameter | Kondisi Awal Status Satu Sehat |
|---|---|---|
| `DATA-PASIEN-01` | Pasien lama / baru yang **belum disetujui** Satu Sehat (`IsApprovedUpload = false` atau belum ada record di `tc_mr_saset`). | `#SatuSehat` Abu-abu |
| `DATA-PASIEN-02` | Pasien yang **sudah disetujui** Satu Sehat sebelumnya (`IsApprovedUpload = true`). | `#SatuSehat` Hijau |
| `DATA-JAMINAN-BPJS` | Tipe Jaminan kategori BPJS Kesehatan (misal: "BPJS PBI", "BPJS Non-PBI", atau kode jaminan BPJS RS). | Terpetakan ke grup BPJS di JetliApi |
| `DATA-JAMINAN-NONBPJS` | Tipe Jaminan kategori Non-BPJS (misal: "Umum / Pribadi", "Asuransi Swasta Prudential", "Perusahaan"). | Tidak terpetakan ke grup BPJS di JetliApi |
| `DATA-DOKTER-POLI` | Jadwal dokter dan poliklinik rawat jalan aktif untuk kebutuhan simulasi pendaftaran. | Siap untuk registrasi |

---

# 4. Test Execution Sequence

Urutan pelaksanaan pengujian yang direkomendasikan:

```
[Tahap 1: Verifikasi Visual Indikator]
    ├── TC-SS-01 (Indikator Belum Disetujui / Abu-abu)
    └── TC-SS-02 (Indikator Sudah Disetujui / Hijau)
            ↓
[Tahap 2: Alur Pasien Sudah Disetujui]
    └── TC-SS-03 (Bypass Konfirmasi untuk Pasien yang Sudah Disetujui)
            ↓
[Tahap 3: Alur Pasien BPJS (Auto-Approval)]
    └── TC-SS-04 (Auto-Approval Otomatis Pasien BPJS)
            ↓
[Tahap 4: Alur Pasien Non-BPJS (Informed Consent)]
    ├── TC-SS-05 (Konfirmasi Non-BPJS: Pilihan "Setuju")
    └── TC-SS-06 (Konfirmasi Non-BPJS: Pilihan "Batal")
            ↓
[Tahap 5: Ketahanan Sistem & Toleransi Kesalahan (Fail-Safe)]
    ├── TC-SS-07 (Fail-Safe: Layanan JetliApi Timeout/Offline)
    └── TC-SS-08 (Fail-Safe: API Approval Backend Error Non-Blocking)
            ↓
[Tahap 6: Performa & Verifikasi Database]
    ├── TC-SS-09 (Efisiensi Caching Client-Side Grup Jaminan)
    └── TC-SS-10 (Integritas Data Database tc_mr_saset & API Detail Pasien)
```

---

# 5. Detailed Test Cases

---

### TC-SS-01: Verifikasi Indikator Visual Pasien Belum Disetujui (#SatuSehat Abu-abu)

- **ID Kasus Uji**: TC-SS-01
- **Kategori**: Visual & Observasi UI
- **Tujuan**: Memastikan kartu ringkasan data pasien menampilkan teks `#SatuSehat` berwarna abu-abu jika pasien belum memiliki persetujuan upload.
- **Prasyarat**: Petugas loket telah login dan membuka halaman Pendaftaran Rawat Jalan.
- **Data Uji**: `DATA-PASIEN-01` (pasien dengan status `IsApprovedUpload = false` atau pasien baru).
- **Langkah Pengujian**:
  1. Pada menu Admisi, buka antarmuka pendaftaran rawat jalan (*Registration Workspace*).
  2. Lakukan pencarian pasien menggunakan No. Rekam Medis (No. RM) atau NIK dari `DATA-PASIEN-01`.
  3. Pilih pasien tersebut sehingga panel kartu ringkasan data pasien terbuka.
  4. Amati label indikator `#SatuSehat` yang berada di samping ikon jenis kelamin pada kartu data pasien.
  5. Arahkan kursor mouse (*hover*) ke atas label `#SatuSehat`.
- **Hasil yang Diharapkan**:
  1. Label `#SatuSehat` tampil dengan warna **abu-abu** (kelas CSS slate-400 / teks tidak tebal).
  2. Saat kursor diarahkan ke label, muncul tooltip dengan teks:  
     `"Persetujuan upload Satu Sehat: Belum Disetujui"`.
  3. Tampilan kartu pasien tetap rapi, simetris, dan tidak ada elemen tata letak yang rusak.

---

### TC-SS-02: Verifikasi Indikator Visual Pasien Sudah Disetujui (#SatuSehat Hijau)

- **ID Kasus Uji**: TC-SS-02
- **Kategori**: Visual & Observasi UI
- **Tujuan**: Memastikan kartu ringkasan data pasien menampilkan teks `#SatuSehat` berwarna hijau jika pasien sudah memiliki persetujuan upload.
- **Prasyarat**: Petugas loket telah login dan membuka halaman Pendaftaran Rawat Jalan.
- **Data Uji**: `DATA-PASIEN-02` (pasien dengan status `IsApprovedUpload = true`).
- **Langkah Pengujian**:
  1. Buka antarmuka pendaftaran rawat jalan (*Registration Workspace*).
  2. Lakukan pencarian pasien menggunakan No. Rekam Medis (No. RM) dari `DATA-PASIEN-02`.
  3. Pilih pasien tersebut hingga panel kartu ringkasan data pasien terbuka.
  4. Amati label indikator `#SatuSehat` pada kartu data pasien.
  5. Arahkan kursor mouse (*hover*) ke atas label `#SatuSehat`.
- **Hasil yang Diharapkan**:
  1. Label `#SatuSehat` tampil dengan warna **hijau** (kelas CSS emerald-600 / teks semi-bold).
  2. Saat kursor diarahkan ke label, muncul tooltip dengan teks:  
     `"Persetujuan upload Satu Sehat: Disetujui"`.

---

### TC-SS-03: Pendaftaran Pasien yang Sudah Disetujui (Bypass Konfirmasi)

- **ID Kasus Uji**: TC-SS-03
- **Kategori**: Aturan Bisnis & Happy Path
- **Tujuan**: Memastikan bahwa proses registrasi untuk pasien yang sudah pernah disetujui langsung disimpan tanpa memunculkan dialog konfirmasi dan tanpa approval ulang.
- **Prasyarat**: Petugas loket membuka pendaftaran pasien dengan data `DATA-PASIEN-02` (sudah disetujui).
- **Data Uji**: `DATA-PASIEN-02`, pilihan jaminan apa saja (BPJS atau Non-BPJS), poliklinik tujuan aktif.
- **Langkah Pengujian**:
  1. Pilih pasien `DATA-PASIEN-02` di layar pendaftaran.
  2. Lengkapi data registrasi: pilih Poliklinik, Dokter, dan Jenis Penjamin.
  3. Klik tombol **"Simpan Registrasi"**.
- **Hasil yang Diharapkan**:
  1. **Tidak muncul** pop-up dialog konfirmasi Satu Sehat.
  2. Pendaftaran pasien langsung diproses dan berhasil disimpan.
  3. Muncul notifikasi sukses pendaftaran (*toast success* / bukti registrasi tercetak sesuai alur normal).

---

### TC-SS-04: Pendaftaran Pasien Jaminan BPJS (Auto-Approval Otomatis Tanpa Dialog)

- **ID Kasus Uji**: TC-SS-04
- **Kategori**: Aturan Bisnis & Regulasi Wajib BPJS
- **Tujuan**: Memastikan pasien jaminan BPJS yang belum disetujui Satu Sehat secara otomatis disetujui sistem (*auto-approve*) tanpa intervensi dialog pop-up saat simpan registrasi.
- **Prasyarat**: Pasien `DATA-PASIEN-01` (status `#SatuSehat` abu-abu) dimuat pada form pendaftaran.
- **Data Uji**: `DATA-PASIEN-01`, Jenis Penjamin: `DATA-JAMINAN-BPJS`, Poliklinik tujuan aktif.
- **Langkah Pengujian**:
  1. Muat pasien `DATA-PASIEN-01` (pastikan indikator `#SatuSehat` awalnya abu-abu).
  2. Pilih Penjamin: jaminan kategori **BPJS Kesehatan**.
  3. Pilih Poliklinik dan Dokter tujuan.
  4. Klik tombol **"Simpan Registrasi"**.
  5. Amati perilaku antarmuka saat tombol diklik hingga proses pendaftaran selesai.
  6. Periksa kembali kartu data pasien setelah pendaftaran sukses.
- **Hasil yang Diharapkan**:
  1. **Tidak ada** pop-up dialog konfirmasi Satu Sehat yang muncul ke layar petugas.
  2. Sistem secara otomatis mengirimkan approval upload Satu Sehat di latar belakang.
  3. Indikator visual `#SatuSehat` pada kartu ringkasan pasien langsung beralih warna menjadi **hijau**.
  4. Pendaftaran rawat jalan berhasil tersimpan dengan notifikasi sukses.
  5. Pada basis data, kolom `IsApprovedUpload` untuk pasien tersebut berubah menjadi `1` (`true`) dengan `TglJamApprovedUpload` terisi tanggal dan waktu saat ini.

---

### TC-SS-05: Pendaftaran Pasien Jaminan Non-BPJS - Petugas Menyetujui Konfirmasi ("Setuju")

- **ID Kasus Uji**: TC-SS-05
- **Kategori**: Alur Pengguna & Informed Consent
- **Tujuan**: Memastikan sistem memunculkan dialog konfirmasi persetujuan Satu Sehat untuk pasien Non-BPJS dan memproses persetujuan saat petugas menekan tombol "Setuju".
- **Prasyarat**: Pasien `DATA-PASIEN-01` (status `#SatuSehat` abu-abu) dimuat pada form pendaftaran.
- **Data Uji**: `DATA-PASIEN-01`, Jenis Penjamin: `DATA-JAMINAN-NONBPJS` (Umum / Asuransi Swasta), Poliklinik tujuan aktif.
- **Langkah Pengujian**:
  1. Muat pasien `DATA-PASIEN-01` pada layar pendaftaran.
  2. Pilih Penjamin: **Umum / Tunai** atau **Asuransi Swasta Non-BPJS**.
  3. Lengkapi Poli dan Dokter tujuan.
  4. Klik tombol **"Simpan Registrasi"**.
  5. Amati dialog pop-up yang muncul: periksa judul dialog, teks penjelasan, No. RM, dan Nama Pasien.
  6. Klik tombol **"Setuju"** pada dialog konfirmasi tersebut.
  7. Amati proses penyimpanan pendaftaran.
- **Hasil yang Diharapkan**:
  1. Muncul dialog modal dengan judul **"Konfirmasi Persetujuan Satu Sehat"**.
  2. Dialog memuat informasi identitas pasien (Nama dan No. RM) serta klausul persetujuan integrasi data ke platform Satu Sehat Kemenkes.
  3. Tersedia tombol **"Setuju"** (warna hijau/emerald) dan tombol **"Batal"** (outline).
  4. Saat tombol **"Setuju"** diklik, dialog menutup dan tombol menampilkan status loading singkat jika proses sedang berjalan.
  5. Sistem memproses persetujuan ke backend dan indikator `#SatuSehat` pada kartu pasien berubah menjadi **hijau**.
  6. Registrasi pasien berhasil disimpan dengan sukses.
  7. Pada basis data, kolom `IsApprovedUpload` bernilai `1` (`true`) dengan catatan waktu approval terbaru.

---

### TC-SS-06: Pendaftaran Pasien Jaminan Non-BPJS - Petugas Menolak/Membatalkan Konfirmasi ("Batal")

- **ID Kasus Uji**: TC-SS-06
- **Kategori**: Alur Pengguna & Otonomi Pasien (Consent Rejection)
- **Tujuan**: Memastikan bahwa jika petugas/pasien memilih "Batal" pada dialog konfirmasi, sistem TIDAK mencatat approval Satu Sehat tetapi proses pendaftaran registrasi pasien TETAP BERJALAN normal.
- **Prasyarat**: Pasien `DATA-PASIEN-01` (status `#SatuSehat` abu-abu) dimuat pada form pendaftaran.
- **Data Uji**: `DATA-PASIEN-01`, Jenis Penjamin: `DATA-JAMINAN-NONBPJS`, Poliklinik tujuan aktif.
- **Langkah Pengujian**:
  1. Muat pasien `DATA-PASIEN-01` pada layar pendaftaran.
  2. Pilih Penjamin: **Umum / Tunai**.
  3. Lengkapi Poli dan Dokter tujuan.
  4. Klik tombol **"Simpan Registrasi"**.
  5. Saat dialog modal **"Konfirmasi Persetujuan Satu Sehat"** muncul, klik tombol **"Batal"**.
  6. Amati perilaku sistem dan penyelesaian transaksi registrasi.
  7. Periksa kembali status visual `#SatuSehat` pada kartu pasien.
- **Hasil yang Diharapkan**:
  1. Dialog konfirmasi menutup.
  2. Sistem **tidak** melakukan pemanggilan API approval Satu Sehat.
  3. Indikator visual `#SatuSehat` pada kartu pasien **tetap berwarna abu-abu** (belum disetujui).
  4. Proses pendaftaran rawat jalan **tetap berhasil disimpan** tanpa kendala (pendaftaran tidak dibatalkan).
  5. Pada basis data, record pasien tetap memiliki `IsApprovedUpload = 0` (`false`).

---

### TC-SS-07: Toleransi Kesalahan (Fail-Safe) Saat Layanan JetliApi Mengalami Gangguan

- **ID Kasus Uji**: TC-SS-07
- **Kategori**: Ketahanan Sistem (Resilience & Non-Blocking)
- **Tujuan**: Memastikan bahwa jika layanan `jetliApi` (pemeriksaan mapping grup jaminan) mati atau mengalami timeout, sistem secara aman melanjutkan penyimpanan registrasi tanpa menghentikan alur kerja loket.
- **Prasyarat**: Simulasi kondisi layanan JetliApi tidak dapat dijangkau (misal: matikan mock service JetliApi atau gunakan URL endpoint tidak valid sementara pada environment testing).
- **Data Uji**: `DATA-PASIEN-01`, Jenis Penjamin: jaminan apa saja, Poliklinik tujuan aktif.
- **Langkah Pengujian**:
  1. Muat pasien `DATA-PASIEN-01` di form registrasi.
  2. Pilih Penjamin dan Poliklinik.
  3. Pastikan JetliApi dalam kondisi tidak merespons (atau memicu timeout 3 detik).
  4. Klik tombol **"Simpan Registrasi"**.
  5. Amati jalannya antarmuka loket.
- **Hasil yang Diharapkan**:
  1. Antarmuka tidak *freeze* atau *stuck* lebih dari ambang batas toleransi timeout (maksimal ~3 detik).
  2. Sistem secara otomatis menerapkan *fail-safe*: dialog dilewati atau pemanggilan dibatalkan secara aman.
  3. **Simpan registrasi pasien tetap berhasil tuntas**.
  4. Tidak ada modal error teknis yang memblokir petugas loket pendaftaran.

---

### TC-SS-08: Toleransi Kesalahan (Fail-Safe) Saat API Approval Backend Gagal

- **ID Kasus Uji**: TC-SS-08
- **Kategori**: Ketahanan Sistem & Kebijakan Nol Pemblokiran (*Zero-Blockage*)
- **Tujuan**: Memastikan bahwa jika pemanggilan endpoint PATCH `/api/pasien/approveUploadSaset` gagal (misal: HTTP 500 atau koneksi terputus), pendaftaran pasien tetap berhasil disimpan disertai pesan peringatan ramah pengguna (*toast warning*).
- **Prasyarat**: Simulasi endpoint PATCH approval backend mengembalikan respons error HTTP 500 (atau matikan rute approval pada mock).
- **Data Uji**: `DATA-PASIEN-01`, Jenis Penjamin: `DATA-JAMINAN-BPJS` atau `DATA-JAMINAN-NONBPJS` (klik Setuju).
- **Langkah Pengujian**:
  1. Muat pasien `DATA-PASIEN-01` di form registrasi.
  2. Pilih Penjamin dan Poliklinik.
  3. Klik tombol **"Simpan Registrasi"** (jika Non-BPJS, klik **"Setuju"** pada dialog konfirmasi).
  4. Saat sistem memanggil endpoint approval dan mendapatkan respon galat dari backend, amati respon antarmuka web.
- **Hasil yang Diharapkan**:
  1. Muncul notifikasi peringatan (*toast warning*):  
     `"Persetujuan Satu Sehat gagal dicatat, pendaftaran tetap diproses"`.
  2. Proses simpan registrasi pasien **tidak dibatalkan**; pendaftaran tetap diproses hingga selesai dan berhasil dibuatkan No. Registrasi.
  3. Petugas loket dapat melanjutkan antrean pasien berikutnya tanpa hambatan operasional.

---

### TC-SS-09: Efisiensi Caching Client-Side Pemetaan Grup Jaminan

- **ID Kasus Uji**: TC-SS-09
- **Kategori**: Performa & Optimasi Jaringan
- **Tujuan**: Memastikan hasil pengecekan grup jaminan ke JetliApi disimpan dalam memori browser (*cache*), sehingga registrasi berulang dengan penjamin yang sama tidak mengirimkan request HTTP berulang.
- **Prasyarat**: Browser Developer Tools (Network Tab) dibuka pada browser penguji.
- **Data Uji**: Dua pasien berbeda yang keduanya belum disetujui Satu Sehat, menggunakan jenis jaminan yang sama (misal: `DATA-JAMINAN-BPJS`).
- **Langkah Pengujian**:
  1. Buka Tab *Network* pada Developer Tools browser dan filter URL dengan kata kunci `GrupJaminan`.
  2. Daftarkan Pasien Pertama dengan `DATA-JAMINAN-BPJS`. Klik **"Simpan Registrasi"**.
  3. Amati permintaan jaringan ke `/JetliAPi/api/GrupJaminan/map`.
  4. Tanpa memuat ulang halaman browser (*reload/refresh*), muat Pasien Kedua dengan jenis jaminan yang sama (`DATA-JAMINAN-BPJS`).
  5. Klik **"Simpan Registrasi"** untuk Pasien Kedua.
  6. Amati kembali Tab *Network*.
- **Hasil yang Diharapkan**:
  1. Pada pendaftaran Pasien Pertama, tercatat 1 request HTTP ke `/JetliAPi/api/GrupJaminan/map`.
  2. Pada pendaftaran Pasien Kedua, **tidak ada** request HTTP tambahan ke endpoint tersebut (data diambil dari cache in-memory client).
  3. Proses validasi pra-simpan Pasien Kedua berjalan instan tanpa jeda jaringan.

---

### TC-SS-10: Integritas Data Database tc_mr_saset & Query Detail Pasien

- **ID Kasus Uji**: TC-SS-10
- **Kategori**: Validasi Persistensi & Konsistensi Data (Regresi)
- **Tujuan**: Memverifikasi bahwa data persetujuan Satu Sehat tersimpan secara benar pada tabel `tc_mr_saset` di database SQL Server dan terbaca utuh saat query detail pasien (`PasienGetQuery`).
- **Prasyarat**: Database SQL Server operasional dapat diakses (via SQL Server Management Studio / tool database) dan Swagger / API client aktif.
- **Data Uji**: Pasien yang baru saja disetujui pada TC-SS-04 atau TC-SS-05.
- **Langkah Pengujian**:
  1. Jalankan query SQL verifikasi pada database:
     ```sql
     SELECT KodeMr, KodeSaset, IsApprovedUpload, TglJamApprovedUpload 
     FROM tc_mr_saset 
     WHERE KodeMr = '<PasienId>';
     ```
  2. Periksa nilai kolom hasil query.
  3. Lakukan pemanggilan API detail pasien melalui browser/Swagger/Postman:
     `GET /api/pasien/<PasienId>`
  4. Periksa struktur JSON respon bagian `pasienSaset`.
- **Hasil yang Diharapkan**:
  1. Hasil query SQL mengembalikan 1 baris data:
     - `KodeMr` sesuai ID pasien.
     - `IsApprovedUpload` bernilai `1` (`true`).
     - `TglJamApprovedUpload` berisi tanggal dan jam terkini (bukan tanggal default `3000-01-01`).
  2. Respon API detail pasien memuat objek:
     ```json
     "pasienSaset": {
       "sasetId": "...",
       "isApprovedUpload": true,
       "tglJamApprovedUpload": "YYYY-MM-DD HH:mm:ss",
       ...
     }
     ```
  3. Seluruh atribut data sosial pasien lainnya (Nama, Alamat, KTP, Telepon) tetap utuh dan tidak mengalami degradasi data.

---

# 6. Traceability Matrix

| Kebutuhan Bisnis (ISSUE-001) | Keputusan Arsitektur | Slice Plan | Kasus Uji (TEST-PACKAGE) |
|---|---|---|---|
| Indikator visual `#SatuSehat` abu-abu vs hijau pada kartu pasien | TD-01, TD-04, GAP-006 | P4-S08, P3-S06 | **TC-SS-01, TC-SS-02** |
| Pasien yang sudah disetujui tidak perlu konfirmasi ulang | TD-05 poin 3 | P4-S10 | **TC-SS-03** |
| Otomatisasi persetujuan (*auto-approval*) untuk jaminan BPJS tanpa dialog | TD-05 poin 4.b, OQ-003 | P4-S10, P3-S07, P2-S05 | **TC-SS-04** |
| Dialog konfirmasi informed consent untuk jaminan Non-BPJS (Setuju) | TD-05 poin 4.b, OQ-004 | P4-S09, P4-S10, P2-S05 | **TC-SS-05** |
| Penolakan dialog konfirmasi Non-BPJS (Batal) tanpa blokir registrasi | TD-05 poin 4.b, OQ-004 | P4-S09, P4-S10 | **TC-SS-06** |
| Ketahanan terhadap kegagalan / timeout JetliApi (*Fail-Safe*) | TD-05 poin 4.a, TD-06, GAP-009 | P4-S10 | **TC-SS-07** |
| Toleransi kesalahan non-blocking jika API approval gagal | TD-06, GAP-009 | P4-S10 | **TC-SS-08** |
| Client-side caching lookup grup jaminan JetliApi | TD-05 poin 4.a, GAP-007 | P4-S10 | **TC-SS-09** |
| Persistensi Upsert tabel `tc_mr_saset` & perluasan query detail pasien | TD-01, TD-02, TD-03, TD-04 | P1-S01..S03, P2-S04 | **TC-SS-10** |

---

# 7. Reporting & Defect Logging Protocol

Petugas penguji (*Tester*) mencatat seluruh hasil eksekusi uji ke dalam lembar **TEST-EXECUTION**.

Pedoman pencatatan dan pelaporan:
1. **Status Uji**:
   - **PASS**: Seluruh hasil yang diamati sesuai persis dengan *Expected Result*.
   - **FAIL**: Terdapat deviasi, kegagalan fungsi, pesan error tak terduga, atau penghentian alur pendaftaran.
2. **Prosedur Penanganan Temuan Cacat (*Defect Handling*)**:
   - Jika ditemukan hasil **FAIL**, tester mencatat:
     - ID Kasus Uji yang gagal.
     - Langkah aktual yang dilakukan saat terjadi kegagalan.
     - Hasil aktual (*Actual Result*) dan bukti pendukung (*evidence screenshot*, rekaman network log, atau response backend).
   - Penguji menyerahkan catatan FAIL ke peran **Issuer** untuk dibuatkan artefak formal `ISSUE (BUG)` dan dianalisis melalui tahapan `BUG-INVESTIGATION`.
   - **PERINGATAN**: Tester dilarang langsung memodifikasi kode sumber atau meminta perbaikan langsung ke pengembang (*Implementer*) tanpa melalui alur resmi Issue Intake.
3. **Kriteria Kelulusan Gerbang (*Gate: TEST PASSED*)**:
   - Seluruh kasus uji (TC-SS-01 sampai TC-SS-10) berstatus **PASS**, ATAU seluruh temuan FAIL telah diselesaikan melalui siklus perbaikan resmi dan diuji ulang (*re-tested*) dengan status **PASS**.

---
*Dokumen ini dibuat secara resmi oleh peran SDLC Tester (`ica-tester`) mengacu pada IMPLEMENTATION-PLAN (COMPLETED).*
