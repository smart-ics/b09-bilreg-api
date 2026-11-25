---
Title: Pembebanan Biaya Pemeriksaan Radiologi (Order Radiologi Charge)
Code: OC-RAD-ORDER-CHARGE
Artifact: FEATURE
Version: 1.0
LastUpdated: 2026-11-24
---

# 1. Purpose

Rumah sakit membutuhkan kapabilitas resmi untuk membebankan tarif dan biaya pemeriksaan radiologi (*radiology order billing charge*) ke dalam rekening transaksi pasien pada domain Tata Rekening (`TRK-BILLING`). Fitur ini menjamin bahwa setiap instruksi pemeriksaan radiologi yang berstatus aktif (`Requested` atau `Scheduled`) ditransformasikan menjadi beban tagihan keuangan yang valid sebelum pemindaian fisik di mesin dilaksanakan (atau secara simultan untuk kasus darurat Cito), mencatat identifier referensi pembebanan, serta mentransisikan siklus hidup order menjadi `Charged`.

# 2. Business Outcome

Permintaan pemeriksaan radiologi yang telah divalidasi berhasil dibebankan ke akun tagihan pasien (*billing charge established*), menghasilkan bukti pembebanan resmi (`BillingTindakanId` / nomor transaksi tagihan), memperbarui status siklus hidup order menjadi `Charged`, dan membuka hak otorisasi pelaksanaan pemindaian fisik rutin bagi instalasi radiologi (`RAD-EXAM`).

# 3. Participating Domains

Berdasarkan arsitektur fungsional MyHosWeb:

- **Radiology (`RAD`)**: Primary Domain. Bertanggung jawab atas inisiasi pembebanan biaya, pemeliharaan status siklus hidup order (`Requested`/`Scheduled` $\rightarrow$ `Charged`), pencatatan nomor referensi billing, penanganan status kegagalan tagihan (`BillingLastError`), serta penguncian aturan pemindaian fisik berbasis status tagihan.
- **Tata Rekening (`TRK`)**: Contributing Domain. Bertanggung jawab atas penerimaan rincian item tagihan pemeriksaan radiologi (`TRK-BILLING`), validasi ketersediaan dan nilai tarif (`TRK-TARIF`), pembuatan transaksi tagihan pasien (`TrsBill`), serta pengelolaan status penagihan akun pasien (`TataRekening`).
- **Admission (`ADM`)**: Contributing Domain. Menyediakan konteks status aktif registrasi kunjungan (`RegId`) dan penjamin pasien (Umum / Asuransi / BPJS) untuk penentuan kelas dan skema tarif penagihan.

# 4. Trigger

Petugas administrasi / loket radiologi mengeksekusi aksi penagihan (*Actor-driven charge action*) pada daftar kerja order radiologi yang berstatus `Requested` atau `Scheduled`.

# 5. Preconditions

1. Order radiologi terdaftar aktif dengan status `Requested` atau `Scheduled`.
2. Order belum pernah dibebankan sebelumnya (belum memiliki referensi penagihan aktif / `BillingTindakanId` masih kosong).
3. Episode registrasi kunjungan pasien masih berstatus aktif dan belum ditutup secara administratif (*not discharged*).
4. Setiap item pemeriksaan radiologi dalam order memiliki pemetaan tarif yang valid dan aktif di katalog tarif (`TRK-TARIF`).
5. Order tidak dalam status dibatalkan (`Cancelled`) atau void.

# 6. Operational Flow

1. **Pemilihan Order untuk Penagihan**:
   - Petugas loket radiologi mengidentifikasi order radiologi pada daftar kerja (*worklist*) berdasarkan `OrderId` atau `OrderNo`.
   - Petugas memeriksa kesiapan administrasi dan memicu perintah penagihan (*Charge*).
2. **Validasi Invariant Siklus Hidup**:
   - Sistem memvalidasi bahwa order berstatus `Requested` atau `Scheduled`. Jika order sudah berstatus `Charged` atau `Cancelled`, aksi ditolak.
3. **Penyusunan dan Pembebanan Tagihan**:
   - Sistem mengambil konteks registrasi kunjungan pasien internal (`RegId`).
   - Sistem memetakan seluruh item pemeriksaan modalitas radiologi ke referensi tarif tindakan resmi, tipe penjamin, dan kelas pelayanan yang berlaku.
   - Sistem membukukan transaksi pembebanan tagihan ke dalam akun rekening pasien pada domain Tata Rekening (`TrsBill`).
4. **Pembaruan Status Siklus Hidup Order**:
   - Jika pembebanan tagihan berhasil dibukukan:
     - Sistem merekam identitas transaksi penagihan (`BillingTindakanId`).
     - Sistem membersihkan riwayat kesalahan penagihan (`BillingLastError = ''`).
     - Sistem memperbarui status siklus hidup order menjadi `Charged`.
     - Sistem merekam jejak audit (waktu dan petugas pelaksana charge).
   - Jika terjadi kendala komunikasi atau validasi tarif:
     - Order tetap pada status asalnya (`Requested` atau `Scheduled`).
     - Sistem merekam penyebab kegagalan pada `BillingLastError` untuk dievaluasi oleh petugas.
5. **Otorisasi Tahap Pelaksanaan Pemindaian Fisik**:
   - Order yang telah berstatus `Charged` mendapatkan otorisasi penuh untuk masuk ke ruang pemeriksaan dan dilakukan pemindaian fisik di mesin (`RAD-EXAM`).

# 7. Domain Orchestration

1. **Radiology (`RAD`) $\rightarrow$ Tata Rekening (`TRK`)**:
   - `RAD` mengirimkan rincian item pemeriksaan, modalitas, konteks pasien/registrasi, dan identitas penagih ke `TRK-BILLING`.
   - `TRK-BILLING` membentuk transaksi tagihan (`TrsBill`) pada rekening kunjungan pasien dan mengembalikan nomor referensi transaksi ke `RAD`.
2. **Radiology (`RAD`) $\rightarrow$ Admission (`ADM`)**:
   - `RAD` memverifikasi status keaktifan registrasi dan penjamin pasien yang berlaku sebagai acuan perhitungan tarif di `TRK`.
3. **Radiology (`RAD`) Internal Orchestration (`RAD-ORDER` $\rightarrow$ `RAD-EXAM`)**:
   - Transisi status menjadi `Charged` membuka gerbang bagi radiografer untuk memulai pemindaian fisik pada modalitas yang ditentukan.

# 8. Constraints

1. **Prasyarat Mutlak Pemindaian Rutin (Hierarchical Financial Gate)**:
   - Pelaksanaan pemindaian fisik di mesin (`RAD-EXAM`) untuk order berkategori **Rutin** hanya boleh dilakukan setelah order berstatus `Charged`.
   - Pengecualian darurat: Order berkategori **Cito (`IsCito == true`)** diizinkan langsung dilakukan pemindaian fisik dari status `Requested`/`Scheduled` guna menyelamatkan nyawa pasien, dengan kewajiban penyelesaian penagihan menyusul.
2. **Idempotensi Penagihan**:
   - Suatu order radiologi yang telah berstatus `Charged` atau telah memiliki nomor referensi penagihan tidak dapat ditagihkan ulang (*double charging prevention*).
3. **Kompensasi Pembatalan Finansial (Guarded Void)**:
   - Pembatalan order yang sudah berstatus `Charged` wajib membatalkan/void beban tagihan di Tata Rekening, selama transaksi penagihan kasir pasien masih berstatus `Opened`.
   - Jika rekening pasien telah berstatus `Finalized`, `Lunas`, atau telah terdapat rekaman pembayaran kasir, pembatalan order klinis secara sepihak ditolak oleh sistem dan diarahkan ke prosedur retur kasir/keuangan.

# 9. Exceptions

- **EX-01: Order Sudah Pernah Di-charge**:
  - *Pemicu*: Pengguna mencoba melakukan charge pada order yang sudah berstatus `Charged` atau memiliki `BillingTindakanId`.
  - *Perilaku*: Sistem menolak aksi dan memberi tahu bahwa order sudah dibebankan tagihan sebelumnya.
- **EX-02: Kegagalan Transaksi Penagihan (Billing Failure)**:
  - *Pemicu*: Terjadi kegagalan validasi tarif, registrasi pasien terkunci, atau masalah internal Tata Rekening saat pembebanan.
  - *Perilaku*: Sistem tidak memajukan status order (tetap `Requested`/`Scheduled`), mencatat pesan error pada `BillingLastError`, dan menampilkan notifikasi kesalahan agar pengguna dapat memperbaiki data lalu mencoba lagi (*retry*).
- **EX-03: Upaya Pemindaian Fisik Rutin Tanpa Status Charged**:
  - *Pemicu*: Radiografer mencoba mencatat permulaan tindakan pemindaian fisik (`RAD-EXAM`) pada order rutin yang belum berstatus `Charged`.
  - *Perilaku*: Sistem memblokir eksekusi pemindaian dan meminta penyelesaian proses billing terlebih dahulu.
- **EX-04: Pembatalan Order yang Tagihannya Sudah Final / Lunas di Kasir**:
  - *Pemicu*: Pengguna mengajukan pembatalan order radiologi yang sudah `Charged`, namun rekening pasien telah ditutup/difinalisasi oleh kasir.
  - *Perilaku*: Sistem menolak pembatalan order radiologi klinis secara langsung dan menginstruksikan koordinasi dengan bagian keuangan/kasir.

# 10. Acceptance Criteria

- **AC-01**: Pengguna dapat memicu perintah penagihan order melalui endpoint `POST api/rad/order/{id}/charge` atau `PATCH api/RadContext/RadOrderFeature/charge` dengan menyertakan `OrderId` dan `UserId`.
- **AC-02**: Penagihan berhasil mengubah status order dari `Requested` atau `Scheduled` menjadi `Charged`, merekam `BillingTindakanId`, dan mengosongkan `BillingLastError`.
- **AC-03**: Sistem menolak eksekusi penagihan jika status order adalah `Charged` atau `Cancelled`.
- **AC-04**: Jika terjadi kegagalan pembebanan tarif, status order tidak berubah dan pesan kesalahan tersimpan pada `BillingLastError`.
- **AC-05**: Pelaksanaan pemindaian fisik (`RAD-EXAM`) untuk order berkategori Rutin ditolak jika order belum berstatus `Charged`.
- **AC-06**: Pelaksanaan pemindaian fisik (`RAD-EXAM`) untuk order berkategori Cito diizinkan langsung dari status `Requested`/`Scheduled`.
- **AC-07**: Pembatalan order berstatus `Charged` berhasil menghapus tagihan di Tata Rekening jika rekening masih berstatus `Opened`, dan ditolak jika rekening sudah `Finalized` atau `Lunas`.
