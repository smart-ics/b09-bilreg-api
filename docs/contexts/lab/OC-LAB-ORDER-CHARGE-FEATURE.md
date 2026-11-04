---
Title: Pembebanan Biaya Pemeriksaan Laboratorium (Order Lab Charge)
Code: OC-LAB-ORDER-CHARGE
Artifact: FEATURE
Version: 1.0
LastUpdated: 2026-10-17
---

# 1. Purpose

Rumah sakit membutuhkan kapabilitas resmi untuk membebankan tarif dan biaya pemeriksaan laboratorium (*laboratory order billing charge*) ke dalam rekening transaksi pasien pada domain Tata Rekening (`TRK-BILLING`). Fitur ini menjamin bahwa setiap instruksi pemeriksaan laboratorium yang berstatus aktif (`Ordered`) ditransformasikan menjadi beban tagihan keuangan yang valid sebelum spesimen biologis diambil (atau secara simultan untuk kasus darurat), mencatat identifier referensi pembebanan, serta mentransisikan siklus hidup order menjadi `Charged`.

# 2. Business Outcome

Permintaan pemeriksaan laboratorium yang telah divalidasi berhasil dibebankan ke akun tagihan pasien (*billing charge established*), menghasilkan bukti pembebanan resmi (`BillingTindakanId` / nomor transaksi tagihan), memperbarui status siklus hidup order menjadi `Charged`, dan membuka hak otorisasi pelaksanaan flebotomi/sampling rutin bagi instalasi laboratorium.

# 3. Participating Domains

Berdasarkan arsitektur fungsional MyHosWeb:

- **Laboratory (`LAB`)**: Primary Domain. Bertanggung jawab atas inisiasi pembebanan biaya, pemeliharaan status siklus hidup order (`Ordered` $\rightarrow$ `Charged`), pencatatan nomor referensi billing, penanganan status kegagalan tagihan (`BillingLastError`), serta penguncian aturan sampling berbasis status tagihan.
- **Tata Rekening (`TRK`)**: Contributing Domain. Bertanggung jawab atas penerimaan rincian item tagihan pemeriksaan laboratorium (`TRK-BILLING`), validasi ketersediaan dan nilai tarif (`TRK-TARIF`), pembuatan transaksi tagihan pasien (`TrsBill`), serta pengelolaan status penagihan akun pasien (`TataRekening`).
- **Admission (`ADM`)**: Contributing Domain. Menyediakan konteks status aktif registrasi kunjungan (`RegId`) dan penjamin pasien (Umum / Asuransi / BPJS) untuk penentuan kelas dan skema tarif penagihan.

# 4. Trigger

Petugas administrasi laboratorium, kasir, atau analis laboratorium mengeksekusi aksi penagihan (*Actor-driven charge action*) pada daftar kerja order laboratorium yang berstatus `Ordered`.

# 5. Preconditions

1. Order laboratorium terdaftar aktif dengan status tepat `Ordered`.
2. Order tidak sedang dalam status penundaan persiapan klinis (`Deferred`).
3. Order belum pernah dibebankan sebelumnya (belum memiliki referensi penagihan aktif).
4. Episode registrasi kunjungan pasien masih berstatus aktif dan belum ditutup secara administratif.
5. Setiap item pemeriksaan memiliki pemetaan tarif yang valid dan aktif di katalog tarif.

# 6. Operational Flow

1. **Pemilihan Order untuk Penagihan**:
   - Petugas mengidentifikasi order laboratorium pada daftar kerja berdasarkan `OrderId` atau `OrderNo`.
   - Petugas memeriksa kesiapan administrasi dan memicu perintah penagihan (*Charge*).
2. **Validasi Invariant Siklus Hidup**:
   - Sistem memvalidasi bahwa order berstatus `Ordered` dan bukan `Deferred`. Jika berstatus `Deferred`, sistem menolak penagihan hingga persiapan klinis terpenuhi dan order diaktifkan.
3. **Penyusunan dan Pembebanan Tagihan**:
   - Sistem mengidentifikasi tipe kunjungan pasien (pasien internal rumah sakit atau pasien eksternal pendaftaran laboratorium).
   - Sistem memetakan seluruh item tes ke referensi tarif tindakan resmi, tipe penjamin, dan kelas pelayanan yang berlaku.
   - Sistem membukukan transaksi pembebanan tagihan ke dalam akun rekening pasien pada domain Tata Rekening.
4. **Pembaruan Status Siklus Hidup Order**:
   - Jika pembebanan tagihan berhasil dibukukan:
     - Sistem merekam identitas transaksi penagihan (`BillingTindakanId`).
     - Sistem membersihkan riwayat kesalahan penagihan (`BillingLastError = ''`).
     - Sistem memperbarui status siklus hidup order menjadi `Charged`.
     - Sistem merekam jejak audit (waktu dan petugas pelaksana charge).
   - Jika terjadi kendala komunikasi atau validasi tarif:
     - Order tetap berstatus `Ordered`.
     - Sistem merekam penyebab kegagalan pada `BillingLastError` untuk dievaluasi oleh petugas.
5. **Otorisasi Tahap Pengambilan Spesimen**:
   - Order yang telah berstatus `Charged` mendapatkan otorisasi penuh untuk dilakukan pengambilan spesimen biologis (`CollectSpecimen` / `LAB-COLLECT`).

# 7. Domain Orchestration

1. **Laboratory (`LAB`) $\rightarrow$ Tata Rekening (`TRK`)**:
   - `LAB` mengirimkan rincian item pemeriksaan, konteks pasien/registrasi, dan identitas penagih ke `TRK-BILLING`.
   - `TRK-BILLING` membentuk transaksi tagihan (`TrsBill`) pada rekening kunjungan pasien dan mengembalikan nomor referensi transaksi ke `LAB`.
2. **Laboratory (`LAB`) $\rightarrow$ Admission (`ADM`)**:
   - `LAB` memverifikasi status keaktifan registrasi dan penjamin pasien yang berlaku sebagai acuan perhitungan tarif di `TRK`.
3. **Laboratory (`LAB`) Internal Orchestration (`LAB-ORDER` $\rightarrow$ `LAB-COLLECT`)**:
   - Transisi status menjadi `Charged` membuka hak akses bagi petugas flebotomi untuk melakukan tindakan pengambilan darah/spesimen rutin.

# 8. Constraints

1. **Prasyarat Mutlak Pengambilan Sampel Rutin**:
   - Pengambilan spesimen untuk order berkategori **Rutin** hanya boleh dilakukan setelah order berstatus `Charged`.
   - Pengecualian darurat: Order berkategori **Cito (`IsCito == true`)** diizinkan untuk diambil spesimennya langsung dari status `Ordered` guna menyelamatkan nyawa pasien, dengan kewajiban penyelesaian penagihan menyusul.
2. **Idempotensi Penagihan**:
   - Suatu order laboratorium yang telah berstatus `Charged` atau telah memiliki nomor referensi penagihan tidak dapat ditagihkan ulang (*double charging prevention*).
3. **Larangan Penagihan saat Deferred**:
   - Order yang masih berstatus `Deferred` (menunggu puasa atau persiapan obat) dilarang keras untuk dibebankan ke billing hingga order resmi diaktifkan kembali.
4. **Kompensasi Pembatalan Finansial**:
   - Pembatalan order yang sudah berstatus `Charged` wajib membatalkan beban tagihan di Tata Rekening, selama transaksi penagihan kasir pasien belum berstatus `Final` atau `Lunas`.

# 9. Exceptions

- **EX-01: Order Sedang Ditunda (Deferred)**:
  - *Pemicu*: Pengguna mencoba melakukan charge pada order yang masih berstatus `Deferred`.
  - *Perilaku*: Sistem menolak aksi dan memberi tahu bahwa order harus diaktifkan terlebih dahulu setelah masa persiapan klinis selesai.
- **EX-02: Kegagalan Transaksi Penagihan (Billing Failure)**:
  - *Pemicu*: Terjadi kegagalan validasi tarif, registrasi pasien terkunci, atau masalah internal Tata Rekening saat pembebanan.
  - *Perilaku*: Sistem tidak memajukan status order (tetap `Ordered`), mencatat pesan error pada `BillingLastError`, dan menampilkan notifikasi kesalahan agar pengguna dapat memperbaiki data lalu melakukan retry.
- **EX-03: Upaya Pengambilan Sampel Rutin Tanpa Status Charged**:
  - *Pemicu*: Petugas flebotomi mencoba mencatat `CollectSpecimen` pada order rutin yang belum berstatus `Charged`.
  - *Perilaku*: Sistem memblokir pengambilan spesimen dan meminta penyelesaian proses billing terlebih dahulu.
- **EX-04: Pembatalan Order yang Tagihannya Sudah Final / Lunas di Kasir**:
  - *Pemicu*: Pengguna mengajukan pembatalan order lab yang sudah `Charged`, namun rekening pasien telah ditutup/difinalisasi oleh kasir.
  - *Perilaku*: Sistem menolak pembatalan order laboratorium klinis secara langsung dan menginstruksikan koordinasi dengan bagian keuangan/kasir.

# 10. Acceptance Criteria

- **AC-01**: Pengguna dapat memicu perintah penagihan order melalui endpoint `PATCH /charge` dengan menyertakan `OrderId` dan `UserId`.
- **AC-02**: Penagihan berhasil mengubah status order dari `Ordered` menjadi `Charged`, merekam `BillingTindakanId`, dan mengosongkan `BillingLastError`.
- **AC-03**: Sistem menolak eksekusi penagihan jika status order adalah `Deferred`, `Charged`, `Collected`, `Cancelled`, atau `Terminated`.
- **AC-04**: Jika terjadi kegagalan pembebanan tarif, status order tetap `Ordered` dan pesan kesalahan tersimpan pada `BillingLastError`.
- **AC-05**: Pengambilan spesimen (`CollectSpecimen`) untuk order berkategori Rutin ditolak jika order belum berstatus `Charged`.
- **AC-06**: Pengambilan spesimen (`CollectSpecimen`) untuk order berkategori Cito diizinkan langsung dari status `Ordered`.
- **AC-07**: Pembatalan order berstatus `Charged` berhasil menghapus tagihan di Tata Rekening jika rekening masih berstatus `Opened`, dan ditolak jika rekening sudah `Finalized` atau `Lunas`.
