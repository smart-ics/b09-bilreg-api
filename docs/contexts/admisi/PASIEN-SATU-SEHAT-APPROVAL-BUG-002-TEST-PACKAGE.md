---
Title: Test Package - Koreksi Siklus Hidup dan Integrasi Event Dialog Konfirmasi Persetujuan Satu Sehat
Code: PASIEN-SATU-SEHAT-APPROVAL-BUG-002
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-10-08
Status: READY-FOR-EXECUTION
Target Plan: PASIEN-SATU-SEHAT-APPROVAL-BUG-002-IMPLEMENTATION-PLAN.md
---

# 1. Overview & Objective

Dokumen ini merupakan **TEST-PACKAGE** resmi untuk memverifikasi perbaikan kecacatan sistem (*bug correction*) pada fitur persetujuan Satu Sehat di modul Admisi Rawat Jalan (`PASIEN-SATU-SEHAT-APPROVAL-BUG-002`).

### Latar Belakang Masalah
Sebelum perbaikan, ketika petugas loket pendaftaran mengklik tombol **"Setuju"** pada pop-up dialog konfirmasi Satu Sehat untuk pasien Non-BPJS, terjadi *race condition* akibat komponen primitif tombol yang otomatis menutup dialog secara prematur. Hal ini memicu pembatalan event (*cancel*) sehingga pemanggilan endpoint backend `{bilreg} PATCH /api/Pasien/approveUploadSaset` tidak terlaksana dan data persetujuan tidak tercatat ke database `tc_mr_saset`.

### Tujuan Pengujian
Memandu penguji manusia (*Human Tester, QA, Trainer, atau Supervisor*) untuk memverifikasi secara langsung bahwa:
1. **Pemisahan Aksi Modal**: Penekanan tombol **"Setuju"** tidak memicu penutupan dialog seketika atau memancarkan sinyal pembatalan (`cancel`).
2. **Indikator Visual Asinkron (Loading State)**: Modal dialog tetap terbuka dan tombol bertransisi menampilkan indikator spinner loading (`Menyimpan...`) dengan kontrol tombol terkunci (`disabled`) selama proses penyimpanan berlangsung.
3. **Penyelesaian Teratur (Controlled Teardown)**: Dialog tertutup secara otomatis dan tertib hanya setelah pemanggilan backend API approval selesai diproses.
4. **Proteksi Penutupan Prematur (Guarded Dismissal)**: Percobaan menutup dialog saat proses simpan sedang berlangsung (menekan tombol ESC atau mengklik area backdrop di luar modal) diabaikan oleh sistem.
5. **Eksekusi Endpoint & Persistensi Database**: Endpoint `{bilreg} PATCH /api/Pasien/approveUploadSaset` secara deterministik dipanggil dengan parameter `pasienId` yang tepat, dan data persetujuan tersimpan ke database `tc_mr_saset`.
6. **Integritas Alur Pembatalan Eksplisit**: Klik tombol **"Batal"** menutup modal secara teratur tanpa memanggil API approval, dan proses registrasi kunjungan tetap berlanjut dengan status persetujuan yang tidak berubah.
7. **Ketahanan Non-Blocking (Fail-Safe)**: Kegagalan koneksi atau error pada API approval Satu Sehat tidak memblokir kelancaran simpan registrasi pendaftaran pasien di loket.

---

# 2. Scope & Testing Principles

## Lingkup Pengujian
- **In-Scope**:
  - Interaksi visual dan siklus hidup modal dialog [DialogPersetujuanSatuSehat.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue).
  - Alur pre-submit pada workspace registrasi [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue).
  - State machine asinkron dua fase pada composable [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts).
  - Inspeksi Network traffic browser untuk endpoint `PATCH /api/Pasien/approveUploadSaset`.
  - Verifikasi integritas tabel database SQL Server `tc_mr_saset`.
- **Out-of-Scope**:
  - Modifikasi skema backend atau modul billing/kasir lanjutan.
  - Alur pengiriman berkas FHIR/HL7 ke server pusat Kemenkes (Satu Sehat Cloud).

## Prinsip Pengujian
- **Berfokus pada Perilaku yang Teramati (*Observable Behavior*)**: Penguji memvalidasi elemen antarmuka, transisi visual tombol, respons jaringan pada browser Developer Tools (Network Tab), dan record database.
- **Tester-Agnostic & Human-Executable**: Langkah-langkah disusun secara konkret, eksplisit, dan dapat dieksekusi tanpa memerlukan keahlian pemrograman mendalam.

---

# 3. Environment & Prerequisites

## 3.1 Kebutuhan Lingkungan & Hak Akses
1. **Aplikasi Frontend**: `c012_myhospital_web` dijalankan di browser (Google Chrome / Microsoft Edge) dengan Console & Network Tab (*DevTools - F12*) aktif.
2. **Layanan Backend**:
   - `b09-bilreg-api` dalam kondisi aktif (*Running*).
   - `jetliApi` dalam kondisi aktif untuk pemetaan grup penjamin.
3. **Database**: SQL Server aktif dengan akses ke database operasional rumah sakit.
4. **Akun Pengguna**: Memiliki hak akses (*role*) Petugas Loket Pendaftaran / Admisi Rawat Jalan.

## 3.2 Data Uji (Test Data)

| ID Data Uji | Deskripsi Entitas | Kriteria Kondisi Awal |
|---|---|---|
| `DATA-PAS-NONBPJS-01` | Pasien Non-BPJS Belum Disetujui | Pasien rawat jalan dengan status `#SatuSehat` Abu-abu (belum pernah disetujui, belum ada record di `tc_mr_saset` atau `IsApprovedUpload = 0`). |
| `DATA-PAS-NONBPJS-02` | Pasien Non-BPJS Pengujian Batal | Pasien rawat jalan dengan status `#SatuSehat` Abu-abu untuk verifikasi aksi penolakan. |
| `DATA-PAS-BPJS-01` | Pasien Jaminan BPJS | Pasien rawat jalan belum disetujui dengan penjamin BPJS Kesehatan (untuk verifikasi regresi alur auto-approval). |
| `DATA-PAS-APPROVED-01` | Pasien yang Sudah Disetujui | Pasien rawat jalan dengan status `#SatuSehat` Hijau (`IsApprovedUpload = 1`) untuk verifikasi bypass modal. |
| `DATA-JAMINAN-UMUM` | Tipe Penjamin Non-BPJS | Penjamin kategori Umum / Pribadi / Asuransi Swasta. |
| `DATA-JAMINAN-BPJS` | Tipe Penjamin BPJS | Penjamin kategori BPJS Kesehatan (misal: "BPJS PBI" / "BPJS Non-PBI"). |
| `DATA-KLINIK-DOKTER` | Poli & Dokter Aktif | Jadwal poliklinik dan dokter aktif yang tersedia untuk kunjungan rawat jalan hari ini. |

---

# 4. Test Execution Sequence

Alur pengujian disusun berurutan guna memverifikasi perbaikan bug, penanganan batas (*edge cases*), dan memastikan tidak terjadi regresi:

```
[Tahap 1: Verifikasi Inti Perbaikan Bug (Happy Path)]
    └── TC-BUG02-01: Alur Persetujuan Non-BPJS "Setuju", Loading Spinner, Hit API, & Simpan DB
            ↓
[Tahap 2: Verifikasi Guarded Dismissal & Proteksi Asinkron]
    └── TC-BUG02-02: Pencegahan Penutupan Prematur via Tombol ESC / Backdrop saat Loading
            ↓
[Tahap 3: Verifikasi Penolakan Eksplisit (Negative Path)]
    └── TC-BUG02-03: Klik Tombol "Batal" Tidak Memanggil API Approval dan Status Tetap Belum Disetujui
            ↓
[Tahap 4: Verifikasi Non-Regresi Alur Khusus]
    ├── TC-BUG02-04: Non-Regresi Pasien Jaminan BPJS (Tetap Auto-Approval Tanpa Dialog)
    └── TC-BUG02-05: Non-Regresi Pasien yang Sudah Disetujui (Bypass Dialog Langsung Simpan)
            ↓
[Tahap 5: Verifikasi Ketahanan & Integritas Data]
    ├── TC-BUG02-06: Verifikasi Toleransi Kesalahan (Fail-Safe Non-Blocking Saat Backend Error)
    └── TC-BUG02-07: Verifikasi Konsistensi Persistensi Database SQL Server tc_mr_saset
```

---

# 5. Detailed Test Cases

---

### TC-BUG02-01: Alur Persetujuan Non-BPJS "Setuju", Loading State, Hit Endpoint, & Update Status (Happy Path)

- **ID Kasus Uji**: `TC-BUG02-01`
- **Kategori**: Happy Path / Core Bug Fix Verification
- **Tujuan**: Memastikan bahwa mengklik tombol "Setuju" pada pasien Non-BPJS tidak menutup dialog seketika, menampilkan indikator loading asinkron, memanggil endpoint approval backend dengan parameter yang tepat, menutup modal setelah selesai, dan memperbarui status visual `#SatuSehat` menjadi hijau.
- **Prasyarat**:
  - Browser membuka halaman Pendaftaran Rawat Jalan (Admisi).
  - Buka Developer Tools (tekan `F12`) lalu pilih tab **Network**. Pada kotak filter, ketik: `approveUploadSaset`.
- **Data Uji**: `DATA-PAS-NONBPJS-01`, `DATA-JAMINAN-UMUM`, `DATA-KLINIK-DOKTER`.

#### Langkah Pengujian:
1. Cari dan pilih pasien `DATA-PAS-NONBPJS-01`.
2. Amati kartu identitas pasien: pastikan label `#SatuSehat` berwarna **Abu-abu**.
3. Pilih jenis penjamin Non-BPJS (misal: **Umum / Tunai**).
4. Lengkapi form pendaftaran (pilih Unit Pelayanan / Poli dan Dokter).
5. Klik tombol **Simpan Registrasi**.
6. Amati pop-up dialog yang muncul:
   - Judul: *"Konfirmasi Persetujuan Satu Sehat"*
   - Menampilkan Nama Pasien dan No. RM yang sesuai.
   - Terdapat tombol **"Batal"** dan tombol **"Setuju"** (berwarna hijau emerald).
7. Klik tombol **"Setuju"** dan perhatikan transisi visual modal dialog.
8. Amati respons jaringan di tab **Network** (DevTools).
9. Tunggu hingga pendaftaran selesai disimpan.

#### Hasil yang Diharapkan:
1. Saat tombol **"Setuju"** diklik:
   - Modal dialog **TETAP TERBUKA** (tidak langsung tertutup/hilang seketika).
   - Tombol "Setuju" berubah menampilkan ikon spinner berputar dengan teks **"Menyimpan..."**.
   - Tombol "Setuju" dan tombol "Batal" berada dalam status nonaktif (*disabled*).
2. Di tab **Network**:
   - Terjadi request HTTP `PATCH` ke URL `/api/Pasien/approveUploadSaset`.
   - Request payload berisi: `{"pasienId": "<ID_PASIEN>"}`.
   - Status respons HTTP adalah `200 OK` dengan payload `{"status": "success", "data": "Done"}`.
3. Setelah request API selesai:
   - Modal dialog tertutup secara teratur dan mulus.
   - Indikator `#SatuSehat` pada kartu pasien berubah warna menjadi **Hijau**.
   - Notifikasi sukses registrasi kunjungan muncul di layar.

---

### TC-BUG02-02: Pencegahan Penutupan Prematur via Tombol ESC atau Klik Luar Modal (Guarded Dismissal)

- **ID Kasus Uji**: `TC-BUG02-02`
- **Kategori**: Guarded Dismissal / Asynchronous State Protection
- **Tujuan**: Memastikan dialog konfirmasi tidak dapat ditutup secara tidak sengaja melalui penekanan tombol `ESC` keyboard atau klik pada area latar belakang (*backdrop*) ketika status penyimpanan (`isPending`) sedang berlangsung.
- **Prasyarat**:
  - Halaman Admisi terbuka.
  - Untuk mempermudah pengamatan, pada DevTools Network tab dapat disetel throttling ke **Slow 4G** (opsional, guna memperpanjang durasi loading).
- **Data Uji**: Pasien Non-BPJS belum disetujui, `DATA-JAMINAN-UMUM`.

#### Langkah Pengujian:
1. Pilih pasien Non-BPJS yang belum disetujui Satu Sehat.
2. Lengkapi data pendaftaran rawat jalan dan klik **Simpan Registrasi**.
3. Saat dialog konfirmasi Satu Sehat muncul, klik tombol **"Setuju"**.
4. Seketika saat tombol menampilkan indikator spinner **"Menyimpan..."**, lakukan dua tindakan berikut secara cepat:
   - Tekan tombol keyboard **`ESC`**.
   - Klik mouse pada area abu-abu gelap di luar kotak modal (*backdrop*).
5. Amati reaksi dialog modal.

#### Hasil yang Diharapkan:
1. Dialog modal **TETAP TERBUKA** dan tidak merespons penekanan tombol ESC maupun klik pada area backdrop.
2. Proses penyimpanan tetap berlanjut tanpa terputus (*aborted*).
3. Setelah mutasi backend selesai, dialog tertutup secara otomatis secara teratur.

---

### TC-BUG02-03: Penolakan Eksplisit Petugas via Tombol "Batal" (Cancellation Path)

- **ID Kasus Uji**: `TC-BUG02-03`
- **Kategori**: Negative / Cancellation Path
- **Tujuan**: Memastikan bahwa ketika pasien atau petugas menolak persetujuan dengan mengklik tombol "Batal", sistem tidak memanggil endpoint approval Satu Sehat, status Satu Sehat tetap belum disetujui, dan pendaftaran kunjungan tetap dapat diselesaikan dengan aman.
- **Prasyarat**:
  - Halaman Admisi aktif dengan tab Network DevTools terbuka (filter: `approveUploadSaset`).
- **Data Uji**: `DATA-PAS-NONBPJS-02`, `DATA-JAMINAN-UMUM`, `DATA-KLINIK-DOKTER`.

#### Langkah Pengujian:
1. Pilih pasien `DATA-PAS-NONBPJS-02` (status `#SatuSehat` Abu-abu).
2. Pilih jaminan Non-BPJS dan lengkapi Poli & Dokter.
3. Klik tombol **Simpan Registrasi**.
4. Saat dialog konfirmasi Satu Sehat muncul, klik tombol **"Batal"**.
5. Amati reaksi modal, tab Network, dan kelanjutan proses pendaftaran.

#### Hasil yang Diharapkan:
1. Modal dialog langsung tertutup secara normal.
2. Pada tab Network, **TIDAK ADA** pemanggilan request ke endpoint `/api/Pasien/approveUploadSaset`.
3. Label `#SatuSehat` pada kartu pasien **TETAP BERWARNA ABU-ABU**.
4. Proses simpan registrasi kunjungan tetap dilanjutkan hingga selesai dan muncul notifikasi sukses pendaftaran.

---

### TC-BUG02-04: Non-Regresi Pasien Jaminan BPJS (Auto-Approval Otomatis Tanpa Dialog)

- **ID Kasus Uji**: `TC-BUG02-04`
- **Kategori**: Regression Verification / Business Rule
- **Tujuan**: Memastikan perbaikan pada dialog tidak menimbulkan regresi pada alur pasien jaminan BPJS Kesehatan, di mana persetujuan upload tetap diberikan secara otomatis tanpa memunculkan dialog modal.
- **Prasyarat**:
  - Halaman Admisi aktif dengan tab Network DevTools terbuka.
- **Data Uji**: `DATA-PAS-BPJS-01` (belum disetujui Satu Sehat), `DATA-JAMINAN-BPJS`, `DATA-KLINIK-DOKTER`.

#### Langkah Pengujian:
1. Pilih pasien `DATA-PAS-BPJS-01` yang berstatus `#SatuSehat` Abu-abu.
2. Pilih jenis jaminan **BPJS Kesehatan**.
3. Lengkapi data Poli dan Dokter, lalu klik **Simpan Registrasi**.
4. Amati layar antarmuka dan tab Network.

#### Hasil yang Diharapkan:
1. Modal dialog konfirmasi persetujuan Satu Sehat **TIDAK PERNAH MUNCUL**.
2. Pada tab Network, endpoint `/api/Pasien/approveUploadSaset` terpanggil secara otomatis di latar belakang (*auto-approval*).
3. Label `#SatuSehat` berubah menjadi **Hijau**.
4. Pendaftaran kunjungan berhasil disimpan dengan sukses.

---

### TC-BUG02-05: Non-Regresi Pasien yang Sudah Disetujui (Bypass Konfirmasi)

- **ID Kasus Uji**: `TC-BUG02-05`
- **Kategori**: Regression Verification / Bypass Logic
- **Tujuan**: Memastikan pasien yang sebelumnya sudah berstatus disetujui tidak lagi memunculkan dialog konfirmasi persetujuan maupun memicu panggilan approval ganda.
- **Prasyarat**:
  - Halaman Admisi aktif dengan tab Network DevTools terbuka.
- **Data Uji**: `DATA-PAS-APPROVED-01` (status `#SatuSehat` Hijau), `DATA-JAMINAN-UMUM`, `DATA-KLINIK-DOKTER`.

#### Langkah Pengujian:
1. Pilih pasien `DATA-PAS-APPROVED-01`.
2. Pastikan label `#SatuSehat` berwarna **Hijau**.
3. Pilih jaminan Non-BPJS dan lengkapi Poli & Dokter.
4. Klik tombol **Simpan Registrasi**.
5. Amati antarmuka dan tab Network.

#### Hasil yang Diharapkan:
1. Dialog konfirmasi Satu Sehat **TIDAK MUNCUL**.
2. Tidak ada pemanggilan ulang ke `/api/Pasien/approveUploadSaset`.
3. Registrasi langsung diproses dan berhasil disimpan.

---

### TC-BUG02-06: Toleransi Kesalahan (Fail-Safe Non-Blocking Saat Backend Approval Gagal)

- **ID Kasus Uji**: `TC-BUG02-06`
- **Kategori**: Resilience / Fail-Safe Non-Blocking
- **Tujuan**: Memastikan bahwa jika terjadi kendala pada backend API approval (misalnya timeout jaringan atau respon error internal), modal dialog tetap ditutup rapi di blok `finally`, dan pendaftaran pasien di loket admisi tidak terhenti/terblokir (*zero-blockage*).
- **Prasyarat**:
  - Halaman Admisi aktif.
  - Simulasi error: Gunakan fitur *Network Request Blocking* pada DevTools untuk memblokir URL pattern `*approveUploadSaset*`, ATAU nonaktifkan sementara IIS endpoint approval.
- **Data Uji**: Pasien Non-BPJS belum disetujui, `DATA-JAMINAN-UMUM`.

#### Langkah Pengujian:
1. Aktifkan blocking terhadap endpoint `*approveUploadSaset*` di DevTools Network tab.
2. Pilih pasien Non-BPJS belum disetujui dan lengkapi data registrasi.
3. Klik tombol **Simpan Registrasi**.
4. Saat dialog konfirmasi muncul, klik tombol **"Setuju"**.
5. Amati transisi dialog dan kelanjutan proses simpan registrasi.
6. Matikan kembali Network Request Blocking setelah pengujian selesai.

#### Hasil yang Diharapkan:
1. Dialog menampilkan spinner loading sesaat ketika mencoba menghubungi server.
2. Setelah error/kegagalan terdeteksi, modal dialog **TETAP TERTUTUP SECARA BERSIH** (tidak menggantung di layar selamanya).
3. Muncul notifikasi peringatan (*toast warning/info*) terkait keterlambatan sinkronisasi Satu Sehat.
4. Pendaftaran pasien **TETAP BERHASIL DISIMPAN** dan nomor registrasi berhasil diterbitkan tanpa error fatal yang menghentikan operasional loket.

---

### TC-BUG02-07: Verifikasi Konsistensi Persistensi Database SQL Server tc_mr_saset

- **ID Kasus Uji**: `TC-BUG02-07`
- **Kategori**: Data Integrity / Database Verification
- **Tujuan**: Memverifikasi secara langsung ke database SQL Server bahwa pendaftaran yang berhasil disetujui melalui TC-BUG02-01 menghasilkan baris data persetujuan yang valid dan konsisten pada tabel `tc_mr_saset`.
- **Prasyarat**:
  - Akses ke SQL Server Management Studio (SSMS) atau query tool database RS.
  - Skenario `TC-BUG02-01` telah selesai dieksekusi dengan sukses.
- **Data Uji**: No. Rekam Medis (No. RM) atau `KodeMr` pasien dari `TC-BUG02-01`.

#### Langkah Pengujian:
1. Buka SQL Server Management Studio (SSMS) dan hubungkan ke database rumah sakit.
2. Jalankan query SQL berikut:
   ```sql
   SELECT TOP 1 
       KodeMr, 
       KodeSaset, 
       IsApprovedUpload, 
       TglJamApprovedUpload, 
       IsApprovedView 
   FROM tc_mr_saset 
   WHERE KodeMr = '<KODE_MR_PASIEN_DARI_TC_01>'
   ```
3. Periksa nilai kolom hasil query.
4. Buka browser dan panggil endpoint detail pasien:
   `GET /api/Pasien/<KODE_MR_PASIEN_DARI_TC_01>`

#### Hasil yang Diharapkan:
1. Hasil Query Database `tc_mr_saset`:
   - `KodeMr`: Sesuai dengan kode rekam medis pasien yang diuji.
   - `IsApprovedUpload`: Bernilai `1` (*true*).
   - `TglJamApprovedUpload`: Terisi tanggal dan jam saat tombol "Setuju" diklik (bukan NULL).
2. Endpoint API `GET /api/Pasien/{id}`:
   - Properti `pasienSaset.isApprovedUpload` bernilai `true`.
   - Properti `pasienSaset.tglJamApprovedUpload` menampilkan timestamp yang valid.

---

# 6. Test Matrix & Traceability

| ID Kasus Uji | Tipe Skenario | Keputusan Arsitektur Terkait | Kriteria Penerimaan Arsitektur |
|---|---|---|---|
| `TC-BUG02-01` | Happy Path | TD-01, TD-03 | AC-1, AC-2, AC-3, AC-4, AC-5 |
| `TC-BUG02-02` | Guarded Dismissal | TD-02 | AC-3 |
| `TC-BUG02-03` | Explicit Cancellation | TD-01, TD-03 | AC-6 |
| `TC-BUG02-04` | Regression: BPJS Auto-Approval | Parent Arch | AC-4 |
| `TC-BUG02-05` | Regression: Already Approved | Parent Arch | AC-4 |
| `TC-BUG02-06` | Resilience: Fail-Safe Non-Blocking | TD-03 | AC-5 |
| `TC-BUG02-07` | Data Integrity: DB & API | Parent Arch | AC-4 |

---

# 7. Defect Reporting Protocol

Jika selama pengujian ditemukan hasil yang tidak sesuai dengan *Hasil yang Diharapkan*:
1. Catat ID Kasus Uji yang gagal.
2. Catat langkah spesifik yang memicu kegagalan beserta screenshot layar dan tab Network DevTools.
3. Tetapkan status **FAIL** pada catatan pengujian.
4. Laporkan defect kepada tim pengembang sesuai alur Knowledge-Centric SDLC (Issuer).
