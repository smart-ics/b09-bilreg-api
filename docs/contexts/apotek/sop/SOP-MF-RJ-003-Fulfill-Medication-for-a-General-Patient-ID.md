# SOP MF-RJ-003 — Melayani Obat Pasien Umum

**Status dokumen:** Spesifikasi operasional acuan

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-MF-RJ-003`

**Dokumen acuan bahasa Inggris:** [SOP MF-RJ-003 — Fulfill Medication for a General Patient](./SOP-MF-RJ-003-Fulfill-Medication-for-a-General-Patient.md)

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara memperoleh persetujuan pembelian secara lisan, membuat dan melunasi faktur penjualan pasien umum, menyiapkan obat, serta menyerahkan obat rawat jalan secara aman dan dapat dipertanggungjawabkan.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyetujui atau menolak nilai pembelian, membayar bila setuju, hadir saat dipanggil, menerima edukasi, dan menerima obat setelah disetujui Apoteker. |
| Staf Apotek | Petugas | Menyampaikan nilai pembelian, mencatat transaksi yang disetujui, menyiapkan atau meracik obat berdasarkan perintah yang telah diizinkan, mengoordinasikan kesiapan obat, dan memanggil Pasien untuk mengambil obat. |
| Penanggung Jawab Apotek | Petugas | Menyetujui penanganan obat yang tidak diambil. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Menerima pembayaran dan memberikan status lunas. |
| Apoteker | Petugas | Memeriksa penerima, melakukan pemeriksaan akhir obat, dan memberikan edukasi kepada Pasien. |
| Aplikasi Pelayanan Obat | Aplikasi | Menampilkan alokasi dan nilai tagihan, mencatat faktur serta status pembayaran, memantau penyiapan, dan mencatat pemberian serta penyerahan obat. |
| Sistem Antrian Pasien | Subsistem | Mencatat `ServedAt` saat penyiapan dimulai dan `DoneAt` saat Pasien dipanggil untuk mengambil obat. |
| Sistem Persediaan | Subsistem | Menyediakan hasil pemesanan, pengeluaran, dan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima beban keuangan dan memberikan hasil koreksi bila diperlukan. |

## 3. Prasyarat

1. Petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Koneksi antrian rawat jalan, pesanan penjualan apotek yang aktif, alokasi tagihan pasien, dan rincian harga telah ditampilkan.
3. Belum ada faktur penjualan untuk transaksi pasien umum yang akan ditawarkan.
4. Perintah penyiapan obat tersedia atau dapat dibuat dari alokasi pelayanan obat.

## 4. Langkah Operasional

1. **Staf Apotek** membuka kebutuhan obat yang telah terhubung dengan antrian pada `Apotek Rajal`, kemudian memeriksa alokasi tagihan dan nilai yang harus dibayar Pasien.
2. **Staf Apotek** melayani Pasien saat pemetaan manual atau memanggil nomor antrian untuk keperluan administrasi setelah pemetaan otomatis. Interaksi ini tidak mencatat `ServedAt` ataupun `DoneAt`.
3. **Staf Apotek** menyampaikan nilai pembelian secara lisan sebelum faktur penjualan dibuat.
4. **Pasien atau Keluarga Pasien** menyampaikan persetujuan pembelian secara lisan.
5. **Staf Apotek** mencatat transaksi yang disetujui. **Aplikasi Pelayanan Obat** membuat faktur hanya untuk alokasi tagihan yang disetujui, lalu menampilkan nomor dan nilainya.
6. **Kasir atau Sistem Pembayaran** menerima pembayaran dan memberikan status lunas untuk faktur tersebut.
7. Setelah status lunas tersedia, **Aplikasi Pelayanan Obat** memberikan izin penyiapan untuk jumlah obat yang bersangkutan.
8. Bila diperlukan, **Sistem Persediaan** memesan stok dan **Aplikasi Pelayanan Obat** menampilkan hasilnya.
9. **Staf Apotek** mulai menyiapkan obat hanya setelah perintah penyiapan berstatus `Released`.
10. **Aplikasi Pelayanan Obat** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat `ServedAt`.
11. **Staf Apotek** menyelesaikan penyiapan atau peracikan dan mencatat hasilnya. **Aplikasi Pelayanan Obat** menampilkan perintah penyiapan dengan status `Prepared`.
12. **Staf Apotek** memastikan setiap obat yang akan diserahkan sudah berstatus `Prepared` atau mempunyai hasil penanganan khusus yang dapat dipertanggungjawabkan.
13. **Staf Apotek** melakukan satu kali panggilan pengambilan obat. **Sistem Antrian Pasien** mengubah antrian menjadi `Done` dan mencatat `DoneAt`.
14. Ketika Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima yang berhak, melakukan pemeriksaan akhir obat, dan mencatat edukasi yang diberikan.
15. **Aplikasi Pelayanan Obat** tidak mengizinkan penyerahan sampai pemeriksaan akhir, identitas penerima, dan edukasi telah dicatat.
16. Setelah mendapat persetujuan Apoteker, **Staf Apotek** menyerahkan obat. **Aplikasi Pelayanan Obat** mencatat pemberian dan penyerahan setiap jumlah obat.
17. **Sistem Persediaan** mencatat pengeluaran stok. Setelah seluruh hasil wajib tersedia, **Aplikasi Pelayanan Obat** menampilkan perintah penyiapan sebagai `Completed`.
18. Pesanan penjualan apotek hanya berubah menjadi `Resolved` setelah seluruh jumlah yang diterima dan seluruh akibat komersialnya selesai.

## 5. Penanganan Kondisi Khusus

### 5.1 Pasien menolak sebelum faktur dibuat

- **Staf Apotek** mencatat penolakan dan tidak membuat faktur penjualan.
- **Aplikasi Pelayanan Obat** menandai alokasi sebagai ditolak atau tidak dialokasikan untuk penjualan, lalu meminta **Sistem Persediaan** melepaskan pemesanan stok yang tidak digunakan.

### 5.2 Nilai berubah sebelum faktur dibuat

- **Aplikasi Pelayanan Obat** menampilkan nilai perhitungan terbaru.
- **Staf Apotek** menyampaikan kembali nilai tersebut dan meminta persetujuan lisan baru sebelum mencatat transaksi.

### 5.3 Pembayaran belum selesai atau faktur perlu dikoreksi

- **Aplikasi Pelayanan Obat** tetap memblokir penyiapan obat selama status lunas belum tersedia.
- **Staf Apotek** hanya dapat membatalkan bila status faktur mengizinkan. Selain itu, **Tata Rekening** menerbitkan penyesuaian keuangan, nota kredit, pengembalian dana, atau hasil lain yang dapat dipertanggungjawabkan.

### 5.4 Stok kurang atau pemeriksaan akhir obat gagal

- **Staf Apotek** mencatat pesanan tertunda atau memilih sumber stok lain yang disetujui untuk obat yang sama. Staf Apotek tidak boleh mengganti jenis obat.
- **Apoteker** mencatat hasil pemeriksaan yang gagal dan tidak menyetujui penyerahan.
- **Aplikasi Pelayanan Obat** mencatat obat yang tidak dapat dilayani dan tetap menampilkan akibat keuangannya.

### 5.5 Pasien tidak mengambil obat

- **Penanggung Jawab Apotek** menerapkan `SOP-MF-RJ-007`. Catatan `DoneAt` pada antrian tidak dihapus atau dibalik.

## 6. Kriteria Penyelesaian

1. Faktur penjualan terlihat berstatus lunas.
2. Perintah penyiapan obat berstatus `Completed`, sedangkan catatan penyerahan mencantumkan penerima yang berhak dan waktu penyerahan.
3. Pengeluaran stok tercatat sebagai hasil resmi dari Sistem Persediaan.
4. Pesanan penjualan apotek berstatus `Resolved`, atau tetap `Active` dengan urusan pelayanan maupun keuangan yang belum selesai ditampilkan secara jelas.
5. Status antrian `Done` tidak boleh dianggap sebagai bukti bahwa obat telah diserahkan.

## 7. Referensi

- [Domain Pelayanan Obat](../medication-fulfillment-domain-id.md), khususnya `BR-MF-020`–`BR-MF-028`, `BR-MF-033`–`BR-MF-046`, `BR-MF-056`–`BR-MF-060`, `BR-MF-067`–`BR-MF-072`, `BR-MF-076`–`BR-MF-083`, `BR-MF-088`, dan `BR-MF-095`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-003`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), `BR-TRK-045`, `BR-TRK-045a`, dan `BR-TRK-046`.
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
