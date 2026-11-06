---
Title: Test Package Rekonsiliasi Inpatient Order dan Prefill Register Ibu Aktif Admisi Rawat Inap
Code: ISSUE-ADMISI-RANAP-REKONSILIASI-ORDER-001
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-11-06
Status: READY-FOR-EXECUTION
Target Plan: ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-IMPLEMENTATION-PLAN.md
Author: ica-tester
---

# 1. Overview & Objective

Dokumen ini merupakan **TEST-PACKAGE** resmi untuk memverifikasi fungsionalitas menyeluruh dari implementasi **Penyatuan Relasi InpatientOrderId, Pembersihan Kolom Warisan, serta Otomasi Pengisian Register Ibu Aktif pada Registrasi Bayi Baru Lahir (BBL)** pada modul **Admisi Rawat Inap** (`c012_myhospital_web`) dan backend **Admisi Ranap** (`b09-bilreg-api`).

Tujuan pengujian adalah memastikan bahwa:
1. **Eliminasi Anomali Status "Perlu Pemeriksaan Data"**: Pasien hasil pendaftaran admisi dari jalur Reservasi Kamar maupun Opname Request (seperti kasus "SRI AJENG RETNO") secara deterministik memperoleh tahap operasional yang benar, yaitu **"Menunggu Diterima Bangsal"** (`WardAcceptanceRequired`), bukan anomali palsu akibat rekonsiliasi dual-source.
2. **Eliminasi Infinite Loading / Hang**: Mengklik kartu pasien pada daftar kerja (*worklist*) konsol admisi ranap segera merender panel kerja (*Patient Journey Workspace*) secara instan tanpa tertahan pada *skeleton loading*.
3. **Pencarian dan Resolusi Register Ibu Cepat & Akurat**: Pada alur **Registrasi BBL**, pemilihan pasien neonatus yang memiliki data Nomor RM Ibu (`FS_MR_IBU`) langsung memunculkan dan memilih register rawat inap ibu aktif secara otomatis melalui data `BILRG_RegAktif` dan `BILRG_AdmAdmission` tanpa melakukan kueri berat ke tabel riwayat besar `ta_registrasi`.
4. **Otomasi Prefill Data Ibu ke Form Bayi**: Register ibu yang terpilih mem-prefill otomatis field **Kelas**, **Bangsal**, **Dokter DPJP**, **Cara Masuk**, dan **Prosedur Masuk Inap** pada formulir pendaftaran bayi baru lahir.
5. **Kebebasan Modifikasi Petugas**: Petugas admisi tetap dapat mengubah atau menyesuaikan data akomodasi maupun dokter hasil prefill sebelum menyelesaikan proses pendaftaran.
6. **Integritas Pembatalan Terkoordinasi**: Pembatalan pendaftaran admisi mengembalikan status pesanan rawat inap (*Inpatient Order*) ke status semula secara konsisten tanpa kesalahan konkurensi.

Dokumen ini dirancang tester-agnostik menggunakan bahasa operasional yang dapat dieksekusi langsung oleh penguji manusia (*Tester, QA, Petugas Admisi, Trainer, atau Supervisor*) tanpa memerlukan pemahaman mendalam tentang kode sumber.

---

# 2. Scope & Testing Principles

## 2.1 Lingkup Pengujian

- **In-Scope**:
  - Daftar kerja (*worklist*) Admisi Rawat Inap: tampilan kartu pasien, badge tahap operasional, dan pemilihan item.
  - Panel Detail Perjalanan Pasien (*Patient Journey Workspace*): pemuatan data, rendering identitas pasien, alur tahapan, dan daftar tugas operasional.
  - Dialog Pencarian Pasien mode BBL (`RanapPatientSearchDialog.vue`).
  - Formulir Proses Admisi BBL (`ProsesAdmisi.vue`): section relasi ibu, indikator ibu terpilih, kartu jaminan, akomodasi, dan data kunjungan.
  - Pengambilan data pendaftaran aktif ibu melalui endpoint `GET /api/admisi-ranap/admission?pasienId={mrIbu}`.
  - Orkestrasi penyimpanan pendaftaran admisi BBL dan reservasi/opname via backend `b09-bilreg-api`.
  - Alur pembatalan admisi (*Coordinated Cancellation*).

- **Out-of-Scope**:
  - Alur pendaftaran Rawat Jalan (Poliklinik) dan Gawat Darurat (IGD).
  - Modul Kasir / Billing dan Apotek / Farmasi.
  - Perubahan DDL fisik pada kolom lama database.

## 2.2 Prinsip Pengujian

- **Tester-Agnostic**: Instruksi langkah pengujian mendeskripsikan tindakan nyata (klik, ketik, amati tampilan visual) dan kriteria hasil yang teramati (*observable behavior*).
- **Zero-Guessing**: Semua prasyarat, data uji, dan langkah eksekusi didefinisikan secara eksplisit.
- **Fail-Fast Verification**: Memvalidasi integritas data dan pencegahan kesalahan pada tahap seawal mungkin.

---

# 3. Test Environment & Data Setup

## 3.1 Lingkungan Pengujian

- **Frontend Web**: Antarmuka Modul Admisi Rawat Inap (`c012_myhospital_web`) berjalan di browser (Chrome / Edge).
- **Backend Service**: REST API Admisi Ranap (`b09-bilreg-api`) aktif pada branch `arf_gabungOpnameRequest-Reservasi`.
- **Database**: SQL Server MyHospital dengan skema terbarui.

## 3.2 Data Uji yang Diperlukan

| ID Data | Entitas / Tipe | Karakteristik / Nilai Contoh | Tujuan Pengujian |
|---|---|---|---|
| **DATA-01** | Pasien Reservasi Kamar | Pasien dengan tiket reservasi rawat inap aktif, misal: "SRI AJENG RETNO" (No. RM `00-00-60-63`, Order ID `OPN0A31CDRTQ` atau reservasi aktif lain) | Verifikasi tahap "Menunggu Diterima Bangsal" & eliminasi hang |
| **DATA-02** | Pasien Ibu Rawat Inap Aktif | Pasien wanita dewasa dengan registrasi Rawat Inap aktif di `BILRG_RegAktif` (`JenisReg = '1'`) dan `BILRG_AdmAdmission` (contoh: No. RM `33750220006063`, No. Reg `RGA7BKFDMG`, Bangsal `ST ANNA`, Kelas `KELAS I`) | Verifikasi auto-lookup dan prefill data ibu |
| **DATA-03** | Pasien Neonatus Terhubung Ibu | Pasien bayi baru lahir (usia < 1 bulan) yang memiliki relasi `FS_MR_IBU = '33750220006063'` di data sosial demografi (contoh: "ambar nivianti", No. RM `337502200265906`) | Verifikasi otomatisasi pencarian Register Ibu BBL |
| **DATA-04** | Pasien Neonatus Mandiri | Pasien bayi baru lahir (usia < 1 bulan) tanpa nomor RM ibu di data sosial atau ibu tidak sedang rawat inap di rumah sakit | Verifikasi alur opsional BBL tanpa register ibu |
| **DATA-05** | Pasien Non-BBL | Pasien berusia >= 1 bulan (contoh: anak usia 2 tahun atau dewasa) | Verifikasi proteksi penolakan usia pada dialog BBL |
| **DATA-06** | Pasien Opname Request Aktif | Pasien dengan tiket rujukan rawat inap (Opname Request) aktif dari IGD / Poliklinik | Verifikasi konsistensi pendaftaran via Opname |

---

# 4. Acceptance Criteria Traceability Matrix

| ID Kriteria (AC) | Deskripsi Kriteria | Test Case Terkait |
|---|---|---|
| **AC-01** | Pasien hasil pendaftaran Reservasi menampilkan status tahap operasional "Menunggu Diterima Bangsal", bukan "Perlu Pemeriksaan Data". | `TC-REC-01` |
| **AC-02** | Memilih kartu pasien reservasi pada worklist langsung membuka detail Journey Workspace tanpa tertahan pada *infinite skeleton loading*. | `TC-REC-02` |
| **AC-03** | Pendaftaran admisi baru dari Reservasi / Opname menyimpan `InpatientOrderId` dengan kolom lama bernilai default (`-`). | `TC-REC-03` |
| **AC-04** | Pada alur Registrasi BBL, memilih pasien neonatus yang memiliki `FS_MR_IBU` langsung menampilkan Register Ibu aktif secara otomatis tanpa harus klik pencarian manual. | `TC-BBL-01` |
| **AC-05** | Kueri pencarian register ibu aktif bersumber dari `BILRG_RegAktif` dan `BILRG_AdmAdmission`, tidak menyentuh tabel besar `ta_registrasi` sehingga respon cepat (< 1 detik). | `TC-BBL-02` |
| **AC-06** | Pemilihan register ibu mem-prefill otomatis Kelas, Bangsal, Dokter DPJP, Cara Masuk, dan Prosedur Masuk Inap pada form admisi bayi. | `TC-BBL-03` |
| **AC-07** | Petugas admisi dapat mengubah/mengedit data akomodasi dan DPJP hasil prefill jika ada kebutuhan penyesuaian ruangan. | `TC-BBL-04` |
| **AC-08** | Pasien berusia >= 1 bulan ditolak saat dicari melalui dialog "Registrasi BBL". | `TC-BBL-05` |
| **AC-09** | Pembatalan admisi terkoordinasi (*Coordinated Cancellation*) berhasil membatalkan admisi dan mengembalikan status order tanpa deadlock database. | `TC-REC-04` |

---

# 5. Test Execution Scenarios & Test Cases

## Skenario 1: Rekonsiliasi Inpatient Order, Eliminasi Status Anomali, dan Journey Workspace

### TC-REC-01: Verifikasi Tahap Operasional Pasien Registrasi Reservasi ("Menunggu Diterima Bangsal")
- **Objective**: Memastikan pasien hasil registrasi reservasi memperoleh tahap operasional yang benar yaitu "Menunggu Diterima Bangsal" dan tidak lagi terkunci pada "Perlu Pemeriksaan Data".
- **Preconditions**:
  1. Login ke aplikasi sebagai petugas Admisi Rawat Inap.
  2. Terdapat pasien hasil pendaftaran reservasi kamar (misal: "SRI AJENG RETNO" atau pasien sejenis sesuai **DATA-01**).
- **Test Steps**:
  1. Buka halaman utama Admisi Rawat Inap (`/admisi/ranap`).
  2. Pada tab/filter daftar kerja, cari pasien reservasi tersebut melalui kotak pencarian nama atau No. RM.
  3. Amati label badge status tahapan pada kartu pasien di daftar kerja (*worklist*).
- **Expected Results**:
  - Badge status tahapan pasien menampilkan **"Menunggu Diterima Bangsal"** (berwarna biru/kuning operasional standar).
  - Tidak ada badge merah atau peringatan **"Perlu Pemeriksaan Data"** pada kartu pasien tersebut.

---

### TC-REC-02: Verifikasi Eliminasi Infinite Loading / Hang pada Patient Journey Workspace
- **Objective**: Memastikan saat kartu pasien diklik, panel Patient Journey Workspace di sebelah kanan memuat data seketika tanpa tertahan pada *skeleton loading*.
- **Preconditions**:
  1. Berada pada daftar kerja Admisi Rawat Inap.
  2. Kartu pasien pendaftaran reservasi / opname tampil pada daftar kerja.
- **Test Steps**:
  1. Klik kartu pasien hasil reservasi / opname pada daftar kerja sebelah kiri.
  2. Amati panel detail (*Patient Journey Workspace*) di sebelah kanan.
  3. Buka Console DevTools (F12) browser untuk memantau aktivitas jaringan (*Network tab*).
- **Expected Results**:
  - Panel kanan menampilkan informasi lengkap pasien (Identitas, No. RM, Penjamin, Bangsal Tujuan, Dokter DPJP) dalam waktu singkat (< 1.5 detik).
  - Tampilan *skeleton placeholder* / animasi loading segera hilang dan berganti data lengkap.
  - Tidak terjadi hang / *infinite loading*.
  - Pada Network tab, pemanggilan `GET /api/admisi-ranap/journey/{journeyId}` mengembalikan status HTTP 200 OK dengan format ID konsisten (misal: `rsv:OPN...` atau `opn:OPN...`).

---

### TC-REC-03: Verifikasi Pendaftaran Baru dengan InpatientOrderId Murni
- **Objective**: Memastikan pendaftaran rawat inap baru dari alur Reservasi Kamar maupun Opname Request tersimpan dengan referensi `InpatientOrderId` yang valid.
- **Preconditions**:
  1. Terdapat tiket Reservasi Kamar atau Opname Request aktif yang belum diproses pendaftarannya.
- **Test Steps**:
  1. Klik tombol **"Proses Admisi"** pada tiket reservasi / opname yang dipilih.
  2. Lengkapi formulir pendaftaran (Akomodasi Kelas, Bangsal, Dokter, Cara Masuk, Prosedur Masuk).
  3. Klik tombol **"Proses Registrasi"**.
  4. Konfirmasi dialog persetujuan pendaftaran.
- **Expected Results**:
  - Pendaftaran berhasil diproses dengan notifikasi sukses *"Registrasi rawat inap berhasil disimpan"*.
  - Pasien berpindah ke daftar aktif dengan status tahap "Menunggu Diterima Bangsal".
  - Pada basis data `BILRG_AdmAdmission`, kolom `InpatientOrderId` terisi ID pesanan, sementara kolom warisan `OpnameRequestId` dan `ReservationId` terisi default `'-'`.

---

### TC-REC-04: Verifikasi Pembatalan Pendaftaran Admisi Terkoordinasi
- **Objective**: Memastikan pembatalan admisi mengembalikan tiket pesanan ke status semula secara terkoordinasi tanpa menimbulkan kesalahan inkonsistensi data.
- **Preconditions**:
  1. Pasien pendaftaran reservasi / opname yang baru didaftarkan berada pada tahap "Menunggu Diterima Bangsal".
- **Test Steps**:
  1. Pilih pasien pada konsol admisi ranap.
  2. Klik tombol aksi **"Batalkan Admisi"**.
  3. Masukkan alasan pembatalan (misal: *"Pembatalan oleh pasien/keluarga"*) dan konfirmasi.
- **Expected Results**:
  - Notifikasi sukses pembatalan tampil.
  - Registrasi admisi dinyatakan void.
  - Tiket pesanan rawat inap (*Inpatient Order*) kembali aktif berstatus *Requested* / siap diproses ulang jika diperlukan.

---

## Skenario 2: Alur Registrasi Bayi Baru Lahir (BBL) & Auto-Lookup Register Ibu

### TC-BBL-01: Verifikasi Auto-Lookup Register Ibu Aktif pada Registrasi BBL
- **Objective**: Memastikan saat pasien bayi baru lahir dipilih pada pendaftaran BBL, Register Ibu otomatis terdeteksi dan terisi tanpa perlu mencari secara manual.
- **Preconditions**:
  1. Pasien Ibu memiliki registrasi rawat inap aktif di RS (misal: No. RM `33750220006063`, No. Reg `RGA7BKFDMG`, Bangsal `ST ANNA`, Kelas `KELAS I`).
  2. Pasien Bayi (misal: "ambar nivianti", No. RM `337502200265906`) memiliki data sosial yang terhubung dengan `FS_MR_IBU = '33750220006063'`.
- **Test Steps**:
  1. Buka halaman utama Admisi Rawat Inap (`/admisi/ranap`).
  2. Klik tombol **"Registrasi BBL"** pada bar filter atas.
  3. Pada dialog pencarian pasien neonatus, cari pasien bayi "ambar nivianti" atau No. RM `337502200265906`.
  4. Klik tombol **"Pilih"** pada baris pasien bayi tersebut.
  5. Amati halaman formulir Proses Admisi yang terbuka, khususnya section **Relasi Ibu (Kasus Perinatal)** di bawah Card Jaminan Pasien.
- **Expected Results**:
  - Section **Relasi Ibu (Kasus Perinatal)** secara otomatis menampilkan Card Register Ibu terpilih.
  - Informasi pada card ibu memuat No. Registrasi Ibu (`RGA7BKFDMG`), Nama Ibu, Bangsal (`ST ANNA`), dan Kelas (`KELAS I`).
  - Alert kuning *"Registrasi ibu belum dipilih (Opsional)"* tidak lagi tampil, melainkan berganti menjadi ringkasan data ibu aktif.
  - Petugas **tidak perlu** mengklik tombol *"Cari Register Ibu"* secara manual.

---

### TC-BBL-02: Verifikasi Kecepatan & Sumber Kueri Register Ibu (BILRG_RegAktif)
- **Objective**: Memastikan pencarian register ibu berlangsung cepat (< 1 detik) karena membaca tabel `BILRG_RegAktif` dan `BILRG_AdmAdmission`, bukan tabel lambat `ta_registrasi`.
- **Preconditions**:
  1. Developer Tools (F12) browser dibuka pada tab *Network*.
- **Test Steps**:
  1. Lakukan pemilihan pasien bayi pada dialog Registrasi BBL (seperti langkah TC-BBL-01).
  2. Amati request jaringan `GET /api/admisi-ranap/admission?pasienId=33750220006063` yang dipicu oleh aplikasi.
- **Expected Results**:
  - Request API menghasilkan status HTTP 200 OK.
  - Waktu respon request sangat cepat (< 500 ms).
  - Payload JSON menyajikan data registrasi ibu aktif beserta detail bangsal dan kelas perawatan.

---

### TC-BBL-03: Verifikasi Prefill Otomatis Data Akomodasi dan Registrasi dari Ibu
- **Objective**: Memastikan terpilihnya register ibu secara otomatis mem-prefill field akomodasi dan data registrasi bayi.
- **Preconditions**:
  1. Pasien bayi dengan register ibu aktif telah termuat di halaman proses admisi (TC-BBL-01).
- **Test Steps**:
  1. Periksa bagian formulir **Akomodasi Tujuan**:
     - Amati nilai field **CARE CLASS**.
     - Amati nilai field **BANGSAL TUJUAN**.
     - Amati nilai field **DOKTER**.
  2. Periksa bagian formulir **Data Registrasi**:
     - Amati nilai field **CARA MASUK**.
     - Amati nilai field **PROSEDUR MASUK INAP**.
     - Amati nilai field **JENIS INAP**.
- **Expected Results**:
  - **CARE CLASS**: Otomatis terpilih sesuai kelas ibu (misal: "KELAS I").
  - **BANGSAL TUJUAN**: Otomatis terpilih sesuai bangsal ibu (misal: "ST ANNA").
  - **DOKTER**: Otomatis terpilih sesuai dokter penanggung jawab ibu jika tersedia.
  - **CARA MASUK**: Otomatis terpilih sesuai cara masuk ibu (atau default "DATANG SENDIRI").
  - **PROSEDUR MASUK INAP**: Otomatis terpilih sesuai prosedur masuk ibu (atau default klinis terkait).
  - **JENIS INAP**: Terkunci pada opsi **"Perinatal (Bayi < 1 Bulan)"** dengan status non-aktif (*disabled*).

---

### TC-BBL-04: Verifikasi Fleksibilitas Modifikasi Manual Field Prefill
- **Objective**: Memastikan petugas admisi tetap memiliki kebebasan untuk mengubah data hasil prefill jika bayi memerlukan penanganan di ruangan/kelas berbeda (misal: ruang Perinatologi/NICU).
- **Preconditions**:
  1. Data prefill ibu telah terisi pada formulir admisi bayi.
- **Test Steps**:
  1. Ubah pilihan **BANGSAL TUJUAN** menjadi bangsal perawatan bayi (misal: ruang Perinatologi atau NICU).
  2. Ubah pilihan **CARE CLASS** bila diperlukan.
  3. Pilih atau ganti **DOKTER DPJP** khusus dokter spesialis anak.
  4. Klik tombol **"Proses Registrasi"**.
  5. Konfirmasi dialog persetujuan.
- **Expected Results**:
  - Formulir mengizinkan pengubahan tanpa ada hambatan (*read-only lock*) pada field bangsal/kelas/dokter.
  - Pendaftaran berhasil disimpan dengan data akomodasi dan dokter yang baru dipilih oleh petugas.
  - Hubungan perinatal dengan register ibu tetap tercatat secara akurat di backend.

---

### TC-BBL-05: Verifikasi Proteksi Batasan Usia Pasien pada Registrasi BBL
- **Objective**: Memastikan dialog pendaftaran BBL menolak pemilihan pasien yang berusia >= 1 bulan (anak/dewasa).
- **Preconditions**:
  1. Terdapat data pasien anak atau dewasa berusia di atas 1 bulan (sesuai **DATA-05**).
- **Test Steps**:
  1. Klik tombol **"Registrasi BBL"** pada konsol admisi ranap.
  2. Cari pasien anak atau dewasa tersebut pada kolom pencarian.
  3. Coba pilih pasien tersebut.
- **Expected Results**:
  - Sistem menampilkan pesan validasi: *"Registrasi BBL hanya diperuntukkan bagi pasien neonatus berusia kurang dari 1 bulan"*.
  - Tombol pilih dinonaktifkan atau pemilihan diblokir.
  - Pengguna tidak dialihkan ke halaman formulir admisi BBL.

---

### TC-BBL-06: Verifikasi Alur BBL Tanpa Register Ibu (Kasus Ibu Tidak Rawat Inap di RS)
- **Objective**: Memastikan pendaftaran BBL tetap dapat diselesaikan dengan sukses apabila bayi dirawat tanpa rawat inap ibu (misal: rujukan dari luar atau ibu sudah pulang).
- **Preconditions**:
  1. Terdapat pasien bayi neonatus tanpa relasi ibu aktif di RS (sesuai **DATA-04**).
- **Test Steps**:
  1. Buka dialog **"Registrasi BBL"** dan pilih pasien bayi tersebut.
  2. Amati bahwa section **Relasi Ibu** menampilkan pesan peringatan opsional *"Registrasi ibu belum dipilih (Opsional)"*.
  3. Isi secara manual Bangsal Tujuan, Care Class, Dokter DPJP, dan Cara Masuk.
  4. Klik tombol **"Proses Registrasi"** dan konfirmasi.
- **Expected Results**:
  - Pendaftaran berhasil disimpan tanpa error validasi register ibu wajib.
  - Pasien bayi tercatat aktif di rawat inap dengan jenis inap Perinatal.

---

# 6. Test Execution Reporting & Verification Checklist

Petugas penguji dapat menggunakan lembar checklist di bawah ini untuk mencatat hasil pengujian lapangan:

| No. Test Case | Skenario Pengujian | Hasil Uji (PASS / FAIL) | Catatan / Bukti Pengujian |
|---|---|---|---|
| **TC-REC-01** | Status tahap reservasi: "Menunggu Diterima Bangsal" | `[ ] PASS  [ ] FAIL` | |
| **TC-REC-02** | Eliminasi infinite loading pada Journey Workspace | `[ ] PASS  [ ] FAIL` | |
| **TC-REC-03** | Pendaftaran admisi baru menyimpan `InpatientOrderId` | `[ ] PASS  [ ] FAIL` | |
| **TC-REC-04** | Pembatalan admisi terkoordinasi (*Coordinated Cancellation*) | `[ ] PASS  [ ] FAIL` | |
| **TC-BBL-01** | Auto-lookup Register Ibu aktif saat memilih bayi BBL | `[ ] PASS  [ ] FAIL` | |
| **TC-BBL-02** | Kueri cepat Register Ibu (< 500 ms) via `BILRG_RegAktif` | `[ ] PASS  [ ] FAIL` | |
| **TC-BBL-03** | Prefill otomatis Kelas, Bangsal, DPJP, Cara & Prosedur Masuk | `[ ] PASS  [ ] FAIL` | |
| **TC-BBL-04** | Kebebasan edit manual data akomodasi hasil prefill | `[ ] PASS  [ ] FAIL` | |
| **TC-BBL-05** | Penolakan pasien usia >= 1 bulan pada dialog BBL | `[ ] PASS  [ ] FAIL` | |
| **TC-BBL-06** | Pendaftaran BBL berhasil meski tanpa register ibu (opsional) | `[ ] PASS  [ ] FAIL` | |

---

# 7. Next Workflow Stage & Handoff

Sesuai tata kelola **Knowledge-Centric SDLC**:
1. Dokumen **TEST-PACKAGE** ini telah berstatus `READY-FOR-EXECUTION`.
2. Tahapan berikutnya adalah **Testing Assistance & Execution** yang dipandu oleh peran **Tester (`ica-tester`)** bersama penguji manusia (*Human Tester / QA / Admisi*).
3. Hasil eksekusi pengujian akan dicatat ke dalam dokumen pendamping `ADMISI-RANAP-REKONSILIASI-INPATIENT-ORDER-TEST-EXECUTION.md`.
4. Jika seluruh skenario menghasilkan **PASS**, gerbang persetujuan **TEST PASSED** akan diterbitkan. Apabila ditemukan ketidaksesuaian (**FAIL**), temuan akan diserahkan ke peran **Issuer (`ica-issuer`)** untuk penerbitan laporan BUG resmi.
