---
Title: Test Package - Verifikasi Perbaikan Defek Batal Finalisasi, Siklus Reopen Billing, dan Eliminasi Tombol Settlement Tata Rekening
Code: TEST-TR-BUG-001
Artifact: TEST-PACKAGE
Version: 1.0.0
LastUpdated: 2026-11-06
Status: READY-FOR-EXECUTION
Target Plan: TATA-REKENING-CANCEL-FINALIZATION-REOPEN-SETTLEMENT-IMPLEMENTATION-PLAN.md
---

# 1. Overview & Objective

Dokumen ini merupakan **TEST-PACKAGE** resmi untuk memverifikasi secara langsung dan menyeluruh perbaikan atas tiga isu fungsional dan antarmuka pada siklus hidup penanganan tagihan di modul **Tata Rekening**:

1. **Batal Finalisasi (Cancel Finalization)**: Memastikan pembatalan status final berhasil dieksekusi tanpa terhambat oleh blocker alokasi penjamin internal, membatalkan alokasi terkait, dan mengembalikan status ke `CLOSED` (dengan tombol aktif berikutnya terarah ke *Langkah 2: Finalisasi Billing*).
2. **Siklus Hidup Reopen Billing**: Memverifikasi secara formal transisi status dari `CLOSED` langsung ke `OPEN`, melepaskan kunci operasional (*Billing Lock*), serta membersihkan alokasi dan verifikasi finansial sesuai `SOP-TR-09`.
3. **Pembersihan Antarmuka dari Tombol Settlement**: Memastikan tombol `Inisiasi Settlement (Reg Out)` beserta ajakan aksi settlement telah dihilangkan dari panel aksi (`TataRekeningActionPanel.vue`) dan toolbar aksi (`TataRekeningActionToolbar.vue`), menyelaraskan tampilan antarmuka dengan alur kerja 2-langkah (*Close Bill* $\rightarrow$ *Finalize Billing*).

---

# 2. Scope & Testing Principles

## 2.1 Lingkup Pengujian
- **In-Scope**:
  - Alur pembatalan finalisasi pada tagihan berpenjamin (BPJS, Asuransi, dsb.) melalui UI Web dan API backend.
  - Validasi penolakan pembatalan finalisasi jika terdapat transaksi pembayaran kasir riil (`HasCashierSettlement`).
  - Alur Reopen Billing pada tagihan berstatus `CLOSED` dan pelepasan kunci tagihan.
  - Inspeksi antarmuka pengguna Tata Rekening terhadap ketiadaan elemen tombol dan dialog Inisiasi Settlement.
  - Verifikasi reaktivitas kartu panduan *Next Action* pasca-batal finalisasi dan pasca-reopen.
  - Eksekusi suite pengujian otomatis backend (`Bilreg.Test`) dan frontend (`vitest`).
- **Out-of-Scope**:
  - Modifikasi skema basis data SQL Server.
  - Pengujian penerimaan pembayaran uang fisik / EDC di aplikasi modul Kasir.

## 2.2 Prinsip Pengujian
- **Human Executability**: Langkah-langkah pengujian dirancang konkret dan dapat diikuti dengan mudah oleh penguji manusia (*QA, Verifikator, Tester, atau Developer*) melalui web browser dan terminal.
- **Tester-Agnostic**: Instruksi fokus pada *langkah uji* dan *hasil yang diharapkan* tanpa bergantung pada profil penguji tertentu.
- **Observable Behavior**: Verifikasi didasarkan pada tampilan visual tombol di UI, status tagihan pada badge ringkasan, notifikasi pesan, dan status respons API.

---

# 3. Environment & Prerequisites

## 3.1 Kebutuhan Lingkungan
1. **Frontend Web (`c012_myhospital_web`)**:
   - Berjalan pada `http://localhost:5173` (atau URL staging frontend).
   - Penguji login dengan akun yang memiliki peran **Verifikator Tata Rekening** / **Supervisor Tata Rekening** / **Admin Keuangan**.
2. **Backend API (`b09-bilreg-api`)**:
   - Layanan API aktif dan terhubung ke basis data rumah sakit/staging.
3. **Testing Tools (Opsional)**:
   - REST API Client (Swagger UI / Postman / curl) untuk pengujian endpoint langsung.
   - Terminal PowerShell / Bash untuk menjalankan test runner otomatis.

## 3.2 Data Uji (Test Data)

| ID Data | Entitas / Deskripsi | Kondisi Awal |
| :--- | :--- | :--- |
| `DATA-REG-BPJS` | Registrasi Pasien BPJS / Asuransi | Memiliki rincian biaya tindakan/obat, penjamin BPJS aktif, status tagihan `FINALIZED`. |
| `DATA-REG-CLOSED` | Registrasi Pasien Rawat Inap / Jalan | Tagihan berstatus `CLOSED` (Tutup Billing telah dieksekusi). |
| `DATA-REG-CASHIER` | Registrasi dengan Transaksi Kasir | Tagihan berstatus `FINALIZED` dan telah memiliki transaksi penerimaan pembayaran kasir riil. |

---

# 4. Test Execution Sequence

```text
[Tahap 1: Verifikasi Batal Finalisasi & Reaktivitas Aksi]
    ├── TC-TR-01: Batal Finalisasi Tagihan Berpenjamin (Alokasi Internal Dibatalkan -> Status CLOSED)
    └── TC-TR-02: Downstream Barrier Guard (Penolakan Batal Finalisasi Saat Ada Transaksi Kasir Riil)
            ↓
[Tahap 2: Verifikasi Alur Reopen Billing]
    └── TC-TR-03: Reopen Billing Tagihan CLOSED (Status Berubah ke OPEN & Billing Lock Dilepas)
            ↓
[Tahap 3: Verifikasi Antarmuka UI (Eliminasi Tombol Settlement)]
    ├── TC-TR-04: Ketiadaan Tombol Settlement pada Action Panel & Toolbar (Status FINALIZED)
    └── TC-TR-05: Verifikasi Alur Siklus Penuh (Open -> Close -> Finalize -> Batal Finalize -> Reopen)
            ↓
[Tahap 4: Verifikasi Otomatis (Regression Test Suites)]
    └── TC-TR-06: Eksekusi Test Runner Backend (.NET) & Frontend (Vitest)
```

---

# 5. Test Cases

### TC-TR-01: Batal Finalisasi Tagihan Berpenjamin (Alokasi Internal Dibatalkan $\rightarrow$ Status CLOSED)

- **Objective**: Memverifikasi bahwa aksi Batal Finalisasi pada tagihan yang memiliki alokasi penjamin berhasil dieksekusi tanpa terblokir pesan downstream blocker palsu, serta status kembali ke `CLOSED` dengan tombol aktif berikutnya mengarah ke `Langkah 2: Finalisasi Billing`.
- **Preconditions**:
  - Akun login memiliki hak akses Supervisor Tata Rekening atau Admin Keuangan.
  - Tagihan pasien (`DATA-REG-BPJS`) berstatus `FINALIZED` dengan alokasi penjamin (BPJS/Asuransi) dan belum ada transaksi kasir.
- **Steps**:
  1. Buka halaman Tata Rekening dan pilih registrasi pasien `DATA-REG-BPJS`.
  2. Pastikan badge status tagihan menampilkan `FINALIZED`.
  3. Pada panel aksi di sisi kanan, klik tombol **Batalkan Finalisasi**.
  4. Periksa dialog konfirmasi pembatalan finalisasi yang muncul.
  5. Masukkan alasan pembatalan pada kolom textarea (contoh: *"Koreksi alokasi penjamin BPJS pasien"* - minimal 10 karakter).
  6. Klik tombol **Batalkan Finalisasi** pada dialog.
- **Expected Result**:
  - Tidak muncul pesan error penolakan *"Pembatalan status Final ditolak: Alokasi Pembayaran atau Transaksi Kasir telah diproses..."*.
  - Muncul toast notifikasi sukses pembatalan finalisasi.
  - Dialog tertutup otomatis dan data workspace me-refresh.
  - Status tagihan pada kartu ringkasan berubah dari `FINALIZED` menjadi **`CLOSED`**.
  - Alokasi penjamin sebelumnya telah dibersihkan.
  - Kartu panduan langkah berikutnya (*Next Action Card*) otomatis menampilkan: **"Langkah 2: Finalisasi Billing"**.
  - Tombol **2. Finalisasi Billing** otomatis aktif dan disorot warna hijau emerald (`bg-emerald-600 text-white`).

---

### TC-TR-02: Downstream Barrier Guard (Penolakan Batal Finalisasi Saat Ada Transaksi Kasir Riil)

- **Objective**: Memverifikasi bahwa proteksi downstream barrier tetap aktif dan menolak pembatalan status final apabila kasir telah memproses pembayaran riil.
- **Preconditions**:
  - Tagihan pasien (`DATA-REG-CASHIER`) berstatus `FINALIZED` dan telah memiliki transaksi kasir riil (`HasCashierSettlement = true`).
- **Steps**:
  1. Buka halaman Tata Rekening dan pilih registrasi pasien `DATA-REG-CASHIER`.
  2. Klik tombol **Batalkan Finalisasi**.
  3. Masukkan alasan pembatalan valid (minimal 10 karakter).
  4. Klik tombol **Batalkan Finalisasi** pada dialog konfirmasi.
- **Expected Result**:
  - Sistem menolak pembatalan finalisasi.
  - Muncul pesan peringatan: *"Pembatalan status Final ditolak: Alokasi Pembayaran atau Transaksi Kasir telah diproses. Batalkan transaksi kasir/alokasi pembayaran terlebih dahulu sebelum membuka kunci tagihan."*.
  - Status tagihan tetap bertahan pada **`FINALIZED`**.

---

### TC-TR-03: Reopen Billing Tagihan CLOSED (Status Berubah ke OPEN & Billing Lock Dilepas)

- **Objective**: Memverifikasi bahwa aksi Buka Kembali (Reopen) pada tagihan berstatus `CLOSED` langsung mengubah status menjadi `OPEN` dan melepaskan penguncian biaya operasional sesuai `SOP-TR-09`.
- **Preconditions**:
  - Tagihan pasien (`DATA-REG-CLOSED`) berada dalam status **`CLOSED`**.
- **Steps**:
  1. Buka halaman Tata Rekening dan pilih registrasi `DATA-REG-CLOSED`.
  2. Pada panel aksi (atau toolbar aksi), klik tombol **Buka Kembali (Reopen)**.
  3. Pada dialog konfirmasi Reopen, masukkan alasan pembukaan kembali billing (contoh: *"Penambahan resep obat dan tindakan bangsal"*).
  4. Klik tombol konfirmasi Reopen.
- **Expected Result**:
  - Muncul toast notifikasi sukses pembukaan kembali tagihan.
  - Status tagihan pada kartu ringkasan langsung berubah dari `CLOSED` menjadi **`OPEN`** (atau `Opened`).
  - Billing Lock dilepaskan (unit pelayanan dapat menambahkan tindakan/obat baru).
  - Kartu panduan langkah berikutnya (*Next Action Card*) otomatis mengarahkan ke: **"Langkah 1: Tutup Billing (Close)"**.
  - Tombol **1. Tutup Billing** aktif dan disorot warna biru.

---

### TC-TR-04: Ketiadaan Tombol Settlement pada Action Panel & Toolbar (Status FINALIZED)

- **Objective**: Memverifikasi bahwa tombol Inisiasi Settlement (Reg Out) telah dihilangkan dari antarmuka pengguna pada tagihan berstatus `FINALIZED`.
- **Preconditions**:
  - Tagihan pasien berada dalam status **`FINALIZED`**.
- **Steps**:
  1. Buka halaman Tata Rekening dan pilih registrasi yang berstatus `FINALIZED`.
  2. Periksa panel aksi di sisi kanan (*Action Panel*).
  3. Periksa bilah alat aksi di bagian atas (*Action Toolbar*).
  4. Periksa kartu panduan langkah berikutnya (*Next Action Card*).
- **Expected Result**:
  - Tombol bertuliskan **"Inisiasi Settlement (Reg Out)"** atau **"Inisiasi Settlement"** **TIDAK ADA** pada Action Panel maupun Action Toolbar.
  - Kartu panduan langkah berikutnya menampilkan kartu status informatif bertuliskan **"Siap Pembayaran Kasir"** (warna ungu/lembut) tanpa tombol aksi klik settlement.
  - Tombol sekunder yang tersedia pada panel aksi adalah tombol koreksi **Batalkan Finalisasi**.

---

### TC-TR-05: Verifikasi Alur Siklus Penuh (Open $\rightarrow$ Close $\rightarrow$ Finalize $\rightarrow$ Batal Finalize $\rightarrow$ Reopen)

- **Objective**: Memverifikasi ketahanan siklus hidup lengkap Tata Rekening tanpa anomali state atau kegagalan mutasi data.
- **Preconditions**:
  - Tagihan pasien awal berada dalam status **`OPEN`**.
- **Steps**:
  1. Klik tombol **1. Tutup Billing** $\rightarrow$ Status berubah menjadi **`CLOSED`**.
  2. Klik tombol **2. Finalisasi Billing**, alokasikan penjamin BPJS, lalu simpan $\rightarrow$ Status berubah menjadi **`FINALIZED`**.
  3. Verifikasi tombol settlement tidak muncul dan kartu status menampilkan "Siap Pembayaran Kasir".
  4. Klik tombol **Batalkan Finalisasi**, isi alasan, lalu konfirmasi $\rightarrow$ Status kembali ke **`CLOSED`**, tombol **2. Finalisasi Billing** otomatis disorot.
  5. Klik tombol **Buka Kembali (Reopen)**, isi alasan, lalu konfirmasi $\rightarrow$ Status kembali ke **`OPEN`**, tombol **1. Tutup Billing** otomatis disorot.
- **Expected Result**:
  - Seluruh transisi status berjalan mulus, deterministik, dan tanpa pesan error penolakan alokasi.
  - Log pembatalan dan audit trail tercatat dengan benar pada backend.

---

### TC-TR-06: Eksekusi Test Runner Backend (.NET) & Frontend (Vitest)

- **Objective**: Memverifikasi integritas seluruh unit test otomatis yang mencakup aturan bisnis backend dan komponen antarmuka frontend.
- **Preconditions**:
  - Terminal PowerShell / Bash tersedia di root direktori proyek `WebBilling`.
- **Steps**:
  1. Jalankan pengujian backend:
     ```powershell
     dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~TataRekening|FullyQualifiedName~AlokasiPembayaran"
     ```
  2. Jalankan pengujian frontend:
     ```powershell
     cd c012_myhospital_web
     npx vitest run src/modules/TataRekening/
     ```
- **Expected Result**:
  - Backend test runner: **100% Passed (330/330 tests pass, 0 failed)**.
  - Frontend vitest runner: **100% Passed (25 test files, 350/350 tests pass, 0 failed)**.

---

# 6. Test Result Recording Guide

Ketika penguji manusia mengeksekusi paket pengujian ini:
- Hasil pencatatan dicatat dalam artefak **`TEST-EXECUTION.md`**.
- Setiap kasus uji diberi status **PASS** atau **FAIL**.
- Apabila ditemukan ketidaksesuaian (**FAIL**), lampirkan:
  - ID Kasus Uji (mis. `TC-TR-01`).
  - Langkah aktual yang dijalankan.
  - Hasil yang diamati beserta tangkapan layar / pesan error.
  - Catatan tersebut diteruskan ke peran **`ica-issuer`** untuk dicatat sebagai formal ISSUE defek.
