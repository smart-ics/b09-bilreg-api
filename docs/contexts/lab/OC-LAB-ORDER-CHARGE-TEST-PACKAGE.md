---
Title: Test Package - Pembebanan Biaya Pemeriksaan Laboratorium (Order Lab Charge)
Code: OC-LAB-ORDER-CHARGE
Artifact: TEST-PACKAGE
Version: 1.0
LastUpdated: 2026-10-19
Status: READY-FOR-EXECUTION
Target Plan: OC-LAB-ORDER-CHARGE-IMPLEMENTATION-PLAN.md
---

# 1. Overview & Objective

Dokumen ini merupakan **TEST-PACKAGE** resmi untuk memvalidasi implementasi fitur **Pembebanan Biaya Pemeriksaan Laboratorium (*Order Lab Charge & Billing*)** (`OC-LAB-ORDER-CHARGE`).

Tujuan pengujian adalah memastikan bahwa:
1. Perintah penagihan (*Actor-driven charge*) berhasil mentransisikan status siklus hidup order laboratorium dari `Ordered` menjadi `Charged`.
2. Pembebanan tarif tindakan laboratorium terbentuk secara akurat pada domain Tata Rekening (`TRK-BILLING` / `TrsBill`), baik untuk pasien internal rumah sakit (Rawat Jalan/Rawat Inap/Darurat) maupun pasien eksternal laboratorium.
3. Aturan disiplin pengambilan spesimen biologis (*Sampling Invariant Guard*) ditegakkan dengan tepat:
   - Pemeriksaan berkategori **Rutin** wajib berstatus `Charged` sebelum spesimen diambil.
   - Pemeriksaan berkategori darurat (**Cito**) diizinkan untuk diambil spesimennya langsung dari status `Ordered` guna penyelamatan nyawa pasien.
4. Kompensasi finansial pembatalan order berjalan aman dan konsisten:
   - Pembatalan order yang telah berstatus `Charged` menghapus seluruh tagihan terkait di Tata Rekening jika rekening masih berstatus `Opened`.
   - Pembatalan order diblokir jika tagihan kasir pasien telah berstatus `Finalized` atau `Lunas`.
5. Ketahanan sistem (*resilience & error handling*) berjalan baik: jika terjadi kendala penagihan, status order tetap `Ordered` dan pesan kesalahan terekam pada `BillingLastError` untuk dievaluasi pengguna.

Dokumen ini disusun agar dapat dieksekusi secara langsung oleh penguji manusia (*Tester, QA, Trainer, Petugas Lab, atau Kasir*) tanpa memerlukan pemahaman mendalam mengenai arsitektur kode internal.

---

# 2. Scope & Testing Principles

## 2.1 Lingkup Pengujian
- **In-Scope**:
  - Eksekusi endpoint penagihan `PATCH /api/LabContext/LabOrderFeature/charge`.
  - Mutasi status order laboratorium (`Ordered` $\rightarrow$ `Charged`).
  - Rekaman referensi tagihan (`BillingTindakanId`) dan pengosongan `BillingLastError`.
  - Pembentukan transaksi tagihan `TrsBill` pada rekening kunjungan pasien (`TataRekening`).
  - Penegakan guard pada pengambilan spesimen `PATCH /api/LabContext/LabOrderFeature/collect` untuk Rutin vs Cito.
  - Kompensasi finansial pada pembatalan order `POST /api/LabContext/LabOrderFeature/cancel` untuk rekening `Opened` vs `Finalized`/`Lunas`.
  - Proteksi pembatalan pasca-sampling (status `Collected`, `Recorded`, `Verified`, `Released`).
- **Out-of-Scope**:
  - Alur pembayaran tunai/EDC di loket kasir rumah sakit (`TRK-KASIR`).
  - Penyesuaian medis hasil pemeriksaan laboratorium dan analizer LIS (`LAB-RESULT`).
  - Pendaftaran awal identitas pasien eksternal (`LAB-EXTERNAL`).

## 2.2 Prinsip Pengujian
- **Berorientasi Perilaku Operasional (*Observable Behavior*)**: Fokus pada respon API, perubahan status order, keberadaan tagihan di rekening pasien, dan pesan validasi visual.
- **Tester-Agnostic**: Instruksi langkah dirancang konkret dan dapat dijalankan menggunakan API client (Postman/cURL/Swagger) maupun aplikasi web jika UI telah terintegrasi.
- **Cakupan Pengujian Komprehensif**: Meliputi alur sukses (*Happy Path*), aturan bisnis (*Business Rules*), penanganan galat (*Error Handling*), dan perlindungan regresi (*Regression Protection*).

---

# 3. Environment & Prerequisites

## 3.1 Kebutuhan Lingkungan
- **Layanan Backend**: `b09-bilreg-api` aktif pada port lokal (misal: `https://localhost:7001` atau `http://localhost:5000`).
- **Database**: SQL Server aktif dengan skema `BILRG_LabOrder`, `BILRG_LabOrderItem`, `BILRG_TrsBill`, dan `BILRG_TataRekening`.
- **Alat Uji**: Postman, cURL, Swagger UI (`/swagger`), atau Test Console.

## 3.2 Kebutuhan Data Uji (Test Data)

| ID Data Uji | Tipe Skenario | Karakteristik Data | Status Awal |
|---|---|---|---|
| `DATA-ORD-RAJAL-RUTIN` | Pasien Rawat Jalan Internal | Order Lab dari EMR Poli Umum/Penyakit Dalam, Urgensi Rutin, 2 item tes (Darah Lengkap & Gula Darah). Rekening Tata Rekening `Opened`. | `Ordered` |
| `DATA-ORD-EKSTERNAL-RUTIN` | Pasien Eksternal Lab | Order Lab dari pendaftaran luar (`ExternalPatient`), Kategori Umum Kelas III, Urgensi Rutin, 1 item tes (Urine Lengkap). Rekening `Opened`. | `Ordered` |
| `DATA-ORD-CITO` | Pasien Darurat (IGD) | Order Lab dari IGD, Urgensi **Cito** (`IsCito = true`), 1 item tes Cito (Troponin / Elektrolit Cito). | `Ordered` |
| `DATA-ORD-DEFERRED` | Pasien Puasa | Order Lab terjadwal dengan penundaan puasa 10 jam. | `Deferred` |
| `DATA-ORD-FINAL-BILL` | Pasien Selesai Kasir | Order Lab yang telah `Charged`, dan rekening pasien di Tata Rekening telah `Finalized` atau `Lunas` di kasir. | `Charged` |
| `DATA-ORD-COLLECTED` | Pasien Sudah Sampling | Order Lab yang spesimennya telah diambil oleh analis flebotomi. | `Collected` |

---

# 4. Test Execution Sequence

```
[Tahap 1: Validasi Pra-Syarat & Guard Status]
    ├── TC-LOC-06 (Blokir Penagihan pada Order Berstatus Deferred)
    └── TC-LOC-07 (Idempotensi: Tolak Penagihan Ulang Order yang Sudah Charged)
            ↓
[Tahap 2: Alur Utama Penagihan (Happy Path)]
    ├── TC-LOC-01 (Penagihan Pasien Internal EMR: Rajal / Ranap)
    └── TC-LOC-02 (Penagihan Pasien Eksternal Laboratorium)
            ↓
[Tahap 3: Disiplin Pengambilan Spesimen (Sampling Invariants)]
    ├── TC-LOC-03 (Sampling Rutin Berhasil Setelah Status Charged)
    ├── TC-LOC-04 (Blokir Sampling Rutin Jika Masih Status Ordered)
    └── TC-LOC-05 (Bypass Sampling Darurat: Order Cito Langsung dari Ordered)
            ↓
[Tahap 4: Toleransi Kesalahan & Resiliensi]
    └── TC-LOC-08 (Ketahanan Saat Gangguan Penagihan: Rekam BillingLastError)
            ↓
[Tahap 5: Kompensasi Pembatalan Finansial]
    ├── TC-LOC-09 (Pembatalan Order Charged: Void Tagihan pada Rekening Opened)
    ├── TC-LOC-10 (Proteksi Finansial: Tolak Pembatalan Jika Kasir Sudah Final/Lunas)
    └── TC-LOC-11 (Proteksi Medis: Tolak Pembatalan Jika Spesimen Sudah Diambil)
```

---

# 5. Test Cases

## TC-LOC-01: Penagihan Pasien Internal EMR (Happy Path)

- **Objective**: Memverifikasi bahwa order lab pasien internal berstatus `Ordered` dapat ditagihkan, status berubah menjadi `Charged`, referensi penagihan tersimpan, dan tagihan tercatat di Tata Rekening.
- **Kategori**: Happy Path
- **Pemetaan Kriteria Penerimaan**: `AC-01`, `AC-02`
- **Preconditions**:
  - Order `DATA-ORD-RAJAL-RUTIN` terdaftar dengan status `Ordered`.
  - Akun rekening pasien di Tata Rekening berstatus `Opened`.
- **Payload Request**:
  - Endpoint: `PATCH /api/LabContext/LabOrderFeature/charge`
  - Body (JSON):
    ```json
    {
      "orderId": "{DATA-ORD-RAJAL-RUTIN-ID}",
      "userId": "petugas-kasir-01"
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request `PATCH` ke endpoint `/charge` dengan payload di atas.
  2. Amati respon HTTP dan data respon JSON.
  3. Lakukan query detail order via `GET /api/LabContext/LabOrderFeature/{orderId}`.
- **Expected Result**:
  - Respon API: HTTP 200 OK dengan format JSend: `status: "success"`, `data: { success: true, billingTindakanId: "{NON-EMPTY-STRING}", billingLastError: null }`.
  - Pada detail order:
    - `labOrderStatus` bernilai `3` (`Charged`).
    - `billingTindakanId` terisi dengan nomor referensi penagihan ($\le 12$ karakter).
    - `billingLastError` bernilai string kosong `""`.
  - Pada modul Tata Rekening: terbentuk catatan transaksi tagihan `TrsBill` untuk setiap item pemeriksaan pasien.

---

## TC-LOC-02: Penagihan Pasien Eksternal Laboratorium

- **Objective**: Memverifikasi bahwa order lab pasien eksternal (`ExternalPatient`) berhasil dibebankan dengan tarif kategori Umum Kelas III secara otomatis.
- **Kategori**: Happy Path & Dual-Path
- **Pemetaan Kriteria Penerimaan**: `AC-01`, `AC-02`
- **Preconditions**:
  - Order `DATA-ORD-EKSTERNAL-RUTIN` berstatus `Ordered` dengan `orderSource = 2` (`ExternalPatient`).
- **Payload Request**:
  - Endpoint: `PATCH /api/LabContext/LabOrderFeature/charge`
  - Body:
    ```json
    {
      "orderId": "{DATA-ORD-EKSTERNAL-RUTIN-ID}",
      "userId": "petugas-lab-01"
    }
    ```
- **Langkah Pengujian**:
  1. Eksekusi request `PATCH /charge`.
  2. Periksa respon dan status order melalui `GET /{orderId}`.
- **Expected Result**:
  - Respon HTTP 200 OK dengan `data.success: true`.
  - Status order berubah menjadi `Charged`.
  - Transaksi tagihan di Tata Rekening terbit menggunakan penjamin Umum dan Kelas III tanpa kendala.

---

## TC-LOC-03: Pengambilan Spesimen Rutin Berhasil Pasca-Charged

- **Objective**: Memverifikasi bahwa order berkategori Rutin yang telah berstatus `Charged` dapat dilakukan tindakan pengambilan spesimen biologis (*sampling*).
- **Kategori**: Business Rules & Lifecycle
- **Pemetaan Kriteria Penerimaan**: `AC-05`
- **Preconditions**:
  - Order telah sukses berstatus `Charged` (hasil dari `TC-LOC-01`).
- **Payload Request**:
  - Endpoint: `PATCH /api/LabContext/LabOrderFeature/collect`
  - Body:
    ```json
    {
      "orderId": "{DATA-ORD-RAJAL-RUTIN-ID}",
      "userId": "analis-flebotomi-01",
      "collectedDate": "2026-10-19T08:30:00",
      "collectionNote": "Spesimen darah vena berhasil diambil tanpa hemolisis."
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request `PATCH /collect`.
  2. Lakukan kueri detail order `GET /{orderId}`.
- **Expected Result**:
  - Respon HTTP 200 OK (`Done`).
  - Status order berubah menjadi `4` (`Collected`).
  - Data `collectionInfo` terisi lengkap (tanggal sampling, petugas, dan catatan sampling).

---

## TC-LOC-04: Blokir Pengambilan Spesimen Rutin Jika Masih Status Ordered

- **Objective**: Memverifikasi penegakan aturan invariant di mana order Rutin yang masih berstatus `Ordered` (belum dibayar/dicharge) ditolak saat hendak diambil spesimennya.
- **Kategori**: Validation Rules & Invariant Guard
- **Pemetaan Kriteria Penerimaan**: `AC-05`
- **Preconditions**:
  - Order baru dibuat dengan urgensi Rutin dan status awal `Ordered` (belum dilakukan `PATCH /charge`).
- **Payload Request**:
  - Endpoint: `PATCH /api/LabContext/LabOrderFeature/collect`
  - Body:
    ```json
    {
      "orderId": "{ORDER-RUTIN-MASIH-ORDERED-ID}",
      "userId": "analis-flebotomi-01",
      "collectedDate": "2026-10-19T08:45:00",
      "collectionNote": "Mencoba sampling tanpa penagihan"
    }
    ```
- **Langkah Pengujian**:
  1. Jalankan request `PATCH /collect` pada order rutin tersebut.
- **Expected Result**:
  - Sistem menolak aksi pengambilan spesimen (HTTP 400 Bad Request / 500 Unhandled Exception).
  - Pesan kesalahan eksplisit: `"LabOrder {OrderId} berstatus Ordered; pemeriksaan rutin wajib berstatus Charged sebelum pengambilan spesimen."`
  - Status order tetap `Ordered` (tidak berubah menjadi `Collected`).

---

## TC-LOC-05: Pengecualian Kasus Darurat (Bypass Sampling pada Order Cito)

- **Objective**: Memverifikasi bahwa order berkategori darurat (**Cito**) diizinkan untuk langsung diambil spesimennya dari status `Ordered` demi keselamatan pasien.
- **Kategori**: Emergency Exemption (Business Rules)
- **Pemetaan Kriteria Penerimaan**: `AC-06`
- **Preconditions**:
  - Order `DATA-ORD-CITO` terdaftar dengan urgensi Cito (`urgency = 1` / `IsCito = true`) dan status `Ordered`.
- **Payload Request**:
  - Endpoint: `PATCH /api/LabContext/LabOrderFeature/collect`
  - Body:
    ```json
    {
      "orderId": "{DATA-ORD-CITO-ID}",
      "userId": "analis-igd-01",
      "collectedDate": "2026-10-19T09:00:00",
      "collectionNote": "Sampling cito darurat troponin di ruang resusitasi IGD"
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request `PATCH /collect` langsung tanpa menjalankan `PATCH /charge` terlebih dahulu.
  2. Periksa status order via `GET /{orderId}`.
- **Expected Result**:
  - Respon HTTP 200 OK (`Done`).
  - Order berhasil beralih langsung dari status `Ordered` menjadi `4` (`Collected`).
  - Tindakan sampling darurat tidak terhambat oleh proses penagihan kasir.

---

## TC-LOC-06: Blokir Penagihan pada Order Berstatus Deferred

- **Objective**: Memverifikasi bahwa order yang sedang ditunda persiapannya (misal puasa) tidak dapat dibebankan tagihan sebelum diaktifkan.
- **Kategori**: Preconditions & Exception
- **Pemetaan Kriteria Penerimaan**: `AC-03`, `EX-01`
- **Preconditions**:
  - Order `DATA-ORD-DEFERRED` berstatus `2` (`Deferred`).
- **Payload Request**:
  - Endpoint: `PATCH /api/LabContext/LabOrderFeature/charge`
  - Body:
    ```json
    {
      "orderId": "{DATA-ORD-DEFERRED-ID}",
      "userId": "petugas-kasir-01"
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request `PATCH /charge`.
- **Expected Result**:
  - Sistem menolak transaksi penagihan.
  - Pesan kesalahan: `"LabOrder {OrderId} berstatus Deferred; charge tidak diperbolehkan saat order ditunda."`
  - Status order tetap `Deferred`.

---

## TC-LOC-07: Idempotensi Penagihan (Tolak Penagihan Ulang pada Order Charged)

- **Objective**: Memverifikasi pencegahan penagihan ganda (*double charge*) pada order yang sudah berstatus `Charged`.
- **Kategori**: Business Constraint & Idempotency
- **Pemetaan Kriteria Penerimaan**: `AC-03`
- **Preconditions**:
  - Order telah sukses berstatus `Charged` pada pengujian sebelumnya.
- **Payload Request**:
  - Endpoint: `PATCH /api/LabContext/LabOrderFeature/charge`
  - Body:
    ```json
    {
      "orderId": "{ORDER-SUDAH-CHARGED-ID}",
      "userId": "petugas-kasir-02"
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request `PATCH /charge` untuk kedua kalinya pada order yang sama.
- **Expected Result**:
  - Sistem menolak transaksi penagihan ulang.
  - Pesan validasi: `"LabOrder {OrderId} berstatus Charged; charge hanya diperbolehkan dari Ordered."`
  - Tagihan tidak bertambah ganda di Tata Rekening.

---

## TC-LOC-08: Penanganan Kegagalan Tagihan (Resilience & BillingLastError)

- **Objective**: Memverifikasi bahwa ketika terjadi kesalahan penagihan (misal parameter registrasi tidak valid atau layanan billing bermasalah), order tetap `Ordered` dan pesan kesalahan dicatat pada `BillingLastError`.
- **Kategori**: Error Handling & Resilience
- **Pemetaan Kriteria Penerimaan**: `AC-04`, `EX-02`
- **Preconditions**:
  - Order berstatus `Ordered`, namun disimulasikan memiliki data tarif yang sengaja di-nonaktifkan atau penjamin tidak valid di lingkungan uji.
- **Payload Request**:
  - Endpoint: `PATCH /api/LabContext/LabOrderFeature/charge`
  - Body:
    ```json
    {
      "orderId": "{ORDER-SIMULASI-GAGAL-ID}",
      "userId": "petugas-kasir-01"
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request `PATCH /charge`.
  2. Amati respon JSON dan lakukan query `GET /{orderId}`.
- **Expected Result**:
  - Respon API: HTTP 200 OK dengan format: `data: { success: false, billingTindakanId: null, billingLastError: "{DESKRIPSI_KESALAHAN}" }`.
  - Pada agregat order:
    - Status order tetap `1` (`Ordered`) (tidak maju ke `Charged`).
    - Properti `billingLastError` menyimpan deskripsi kesalahan (maksimal 200 karakter).
  - Pengguna dapat memperbaiki data lalu melakukan retry `PATCH /charge` kembali.

---

## TC-LOC-09: Kompensasi Pembatalan Order Charged (Rekening Masih Opened)

- **Objective**: Memverifikasi bahwa pembatalan order yang sudah `Charged` membatalkan seluruh transaksi tagihan di Tata Rekening selama rekening pasien masih dibuka (`Opened`).
- **Kategori**: Financial Compensation & Cancellation
- **Pemetaan Kriteria Penerimaan**: `AC-07`
- **Preconditions**:
  - Order berstatus `Charged` dan memiliki tagihan `TrsBill` di Tata Rekening.
  - Akun rekening pasien berstatus `Opened` dan belum memiliki pembayaran kasir.
- **Payload Request**:
  - Endpoint: `POST /api/LabContext/LabOrderFeature/cancel` atau `PATCH /cancel`
  - Body:
    ```json
    {
      "orderId": "{ORDER-CHARGED-ID}",
      "userId": "dokter-pengorder-01",
      "reason": "Pasien menolak tes dan meminta pulang atas permintaan sendiri"
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request pembatalan order.
  2. Periksa detail order `GET /{orderId}`.
  3. Periksa daftar tagihan di rekening pasien Tata Rekening.
- **Expected Result**:
  - Respon HTTP 200 OK (`Done`).
  - Status order laboratorium berubah menjadi `8` (`Cancelled`) dengan `cancelledReason` terekam.
  - Seluruh tagihan `TrsBill` terkait order tersebut (`${orderId}-*`) berhasil dihapus (*voided*) dari akun rekening pasien.

---

## TC-LOC-10: Proteksi Pembatalan Jika Kasir Pasien Telah Final / Lunas

- **Objective**: Memverifikasi bahwa order lab yang tagihannya telah dilunasi di loket kasir tidak dapat dibatalkan secara sepihak untuk mencegah ketidaksinkronan akuntansi.
- **Kategori**: Financial Protection & Exception
- **Pemetaan Kriteria Penerimaan**: `AC-07`, `EX-04`
- **Preconditions**:
  - Order `DATA-ORD-FINAL-BILL` berstatus `Charged`.
  - Akun rekening pasien di Tata Rekening telah berstatus `Finalized` atau `Lunas` (atau memiliki rekaman `ListPayment`).
- **Payload Request**:
  - Endpoint: `POST /api/LabContext/LabOrderFeature/cancel`
  - Body:
    ```json
    {
      "orderId": "{DATA-ORD-FINAL-BILL-ID}",
      "userId": "petugas-01",
      "reason": "Mencoba membatalkan order yang sudah lunas"
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request pembatalan order.
- **Expected Result**:
  - Sistem menolak keras pembatalan.
  - Pesan kesalahan: `"Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas."`
  - Status order tetap `Charged` dan tagihan di kasir tetap utuh.

---

## TC-LOC-11: Proteksi Medis: Tolak Pembatalan Jika Spesimen Telah Diambil

- **Objective**: Memverifikasi perlindungan integritas klinis bahwa order yang telah diambil spesimennya (`Collected`) dilarang dibatalkan.
- **Kategori**: Clinical Invariant & Guard
- **Pemetaan Kriteria Penerimaan**: `EX-03`
- **Preconditions**:
  - Order `DATA-ORD-COLLECTED` berstatus `4` (`Collected`).
- **Payload Request**:
  - Endpoint: `POST /api/LabContext/LabOrderFeature/cancel`
  - Body:
    ```json
    {
      "orderId": "{DATA-ORD-COLLECTED-ID}",
      "userId": "petugas-01",
      "reason": "Membatalkan padahal sampel sudah diambil"
    }
    ```
- **Langkah Pengujian**:
  1. Kirim request pembatalan order.
- **Expected Result**:
  - Sistem menolak aksi pembatalan.
  - Pesan kesalahan: `"LabOrder {OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status Collected)."`
  - Status order tetap `Collected`.

---

# 6. Traceability Matrix

Matriks ketertelusuran antara Kriteria Penerimaan Fitur ([`OC-LAB-ORDER-CHARGE-FEATURE.md`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-CHARGE-FEATURE.md)) dan Kasus Uji:

| Kode Kriteria Penerimaan | Deskripsi Kriteria | Kasus Uji Pemverifikasi |
|---|---|---|
| **AC-01** | Pengguna dapat memicu perintah penagihan order melalui endpoint `PATCH /charge`. | `TC-LOC-01`, `TC-LOC-02` |
| **AC-02** | Penagihan berhasil mengubah status menjadi `Charged`, merekam `BillingTindakanId`, dan mengosongkan `BillingLastError`. | `TC-LOC-01`, `TC-LOC-02` |
| **AC-03** | Penagihan ditolak jika order berstatus `Deferred`, `Charged`, `Collected`, `Cancelled`, atau `Terminated`. | `TC-LOC-06`, `TC-LOC-07` |
| **AC-04** | Jika terjadi kegagalan pembebanan tarif, status order tetap `Ordered` dan pesan kesalahan tersimpan di `BillingLastError`. | `TC-LOC-08` |
| **AC-05** | Pengambilan spesimen (`CollectSpecimen`) untuk order Rutin ditolak jika order belum berstatus `Charged`. | `TC-LOC-03`, `TC-LOC-04` |
| **AC-06** | Pengambilan spesimen (`CollectSpecimen`) untuk order Cito diizinkan langsung dari status `Ordered`. | `TC-LOC-05` |
| **AC-07** | Pembatalan order `Charged` menghapus tagihan jika rekening `Opened`, dan ditolak jika rekening sudah `Finalized`/`Lunas`. | `TC-LOC-09`, `TC-LOC-10` |
| **EX-03** | Pembatalan ditolak jika spesimen telah diambil (`Collected`, `Recorded`, `Verified`, `Released`). | `TC-LOC-11` |

---

# 7. Petunjuk Pelaporan Hasil Uji (Bagi Penguji Manusia)

Setiap penguji yang mengeksekusi paket ini mencatat hasil pengujian ke dalam lembar catatan eksekusi pengujian (`TEST-EXECUTION`):
1. Jika hasil uji sesuai ekspektasi $\rightarrow$ Tandai **`PASS`** dan lampirkan snapshot respon API atau layar aplikasi.
2. Jika hasil uji tidak sesuai ekspektasi $\rightarrow$ Tandai **`FAIL`**, dokumentasikan bukti galat (*evidence / error log*), dan serahkan kepada peran **`ica-issuer`** untuk penerbitan tiket **`ISSUE (BUG)`**.
