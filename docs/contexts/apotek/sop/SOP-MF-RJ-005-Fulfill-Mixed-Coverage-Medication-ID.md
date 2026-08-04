# SOP MF-RJ-005 — Melayani Obat dengan Penjaminan Campuran

**Status dokumen:** Spesifikasi operasional acuan

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-MF-RJ-005`

**Dokumen acuan bahasa Inggris:** [SOP MF-RJ-005 — Fulfill Mixed-Coverage Medication](./SOP-MF-RJ-005-Fulfill-Mixed-Coverage-Medication.md)

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara memisahkan obat yang ditanggung BPJS dari obat yang harus dibayar Pasien, sekaligus mengoordinasikan penyiapan dan penyerahannya dalam satu kali pengambilan.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyetujui atau menolak bagian yang harus dibayar sendiri, membayar bila setuju, hadir saat dipanggil, menerima edukasi, dan menerima obat setelah disetujui Apoteker. |
| Staf Apotek | Petugas | Memisahkan alokasi tagihan, menyampaikan nilai yang harus dibayar Pasien, mencatat transaksi yang disetujui, menyiapkan atau meracik obat yang telah mendapat izin, mengoordinasikan kesiapan, dan memanggil Pasien. |
| Penanggung Jawab Apotek | Petugas | Menyetujui penanganan obat yang tidak diambil. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Menerima pembayaran dan memberikan status lunas untuk faktur pasien umum. |
| Apoteker | Petugas | Memeriksa penerima, melakukan pemeriksaan akhir obat, dan memberikan edukasi kepada Pasien. |
| Sistem SEP dan Fornas | Subsistem | Memberikan hasil pemeriksaan keabsahan SEP dan penjaminan setiap item obat. |
| Aplikasi Pelayanan Obat | Aplikasi | Memisahkan alokasi dan persetujuan menurut penanggung biaya, mencatat faktur secara terpisah, dan mengoordinasikan penyerahan obat. |
| Sistem Antrian Pasien | Subsistem | Mencatat satu `ServedAt` dan satu `DoneAt` untuk antrian yang sama. |
| Sistem Persediaan | Subsistem | Menyediakan hasil pemesanan, pengeluaran, dan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima dan menyelesaikan akibat keuangan sesuai penanggung biaya. |

## 3. Prasyarat

1. Petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Satu pesanan penjualan apotek yang aktif memuat jumlah yang ditanggung BPJS dan jumlah yang harus dibayar Pasien.
3. SEP masih sah dan pemetaan Fornas menunjukkan jumlah yang dijamin serta tidak dijamin.
4. Alokasi pelayanan dan perintah penyiapan obat telah ditampilkan.
5. Belum ada faktur pasien umum maupun faktur BPJS untuk alokasi tersebut, kecuali proses dilanjutkan setelah penanganan kondisi khusus yang tercatat.

## 4. Langkah Operasional

1. **Staf Apotek** membuka kebutuhan obat dengan penjaminan campuran pada `Apotek Rajal`, lalu memeriksa jumlah obat dan jenis penanggung biayanya.
2. **Staf Apotek** mencatat alokasi tagihan BPJS dan alokasi tagihan Pasien secara terpisah tanpa mengubah identitas atau jumlah obat yang telah diterima.
3. **Sistem SEP dan Fornas** memberikan hasil pemeriksaan SEP dan penjaminan setiap item. **Aplikasi Pelayanan Obat** menampilkan persetujuan penjaminan untuk jumlah yang ditanggung BPJS.
4. **Aplikasi Pelayanan Obat** menghitung dan menampilkan nilai obat yang tidak dijamin dan harus dibayar Pasien.
5. **Staf Apotek** menyampaikan nilai tersebut secara lisan sebelum faktur pasien umum dibuat.
6. **Pasien atau Keluarga Pasien** menyampaikan persetujuan lisan atas bagian yang harus dibayar sendiri.
7. **Staf Apotek** mencatat transaksi yang disetujui. **Aplikasi Pelayanan Obat** membuat faktur pasien umum secara terpisah untuk alokasi tersebut.
8. **Kasir atau Sistem Pembayaran** menerima pembayaran dan memberikan status lunas untuk faktur pasien umum.
9. **Aplikasi Pelayanan Obat** memberikan izin penyiapan: berdasarkan persetujuan penjaminan untuk bagian BPJS dan berdasarkan status lunas untuk bagian yang dibayar Pasien.
10. **Sistem Persediaan** memesan stok yang diperlukan dan memberikan hasilnya.
11. Setelah setiap jumlah yang akan diserahkan mendapat izin, **Staf Apotek** memulai dan menyelesaikan penyiapan obat.
12. Saat penyiapan pertama dimulai, **Aplikasi Pelayanan Obat** mencatat `Medication Preparation Started`; **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat satu `ServedAt`.
13. **Aplikasi Pelayanan Obat** menampilkan setiap perintah penyiapan yang akan diserahkan sebagai `Prepared` atau dengan hasil penanganan khusus yang dapat dipertanggungjawabkan.
14. **Staf Apotek** melakukan satu kali panggilan pengambilan obat. **Sistem Antrian Pasien** mengubah antrian yang sama menjadi `Done` dan mencatat satu `DoneAt`.
15. Ketika Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima yang berhak, melakukan pemeriksaan akhir obat, dan mencatat edukasi yang diberikan.
16. Setelah mendapat persetujuan Apoteker, **Staf Apotek** menyerahkan obat.
17. **Aplikasi Pelayanan Obat** membuat faktur BPJS hanya bersamaan dengan penyerahan yang berhasil, mencatat pemberian dan penyerahan seluruh jumlah obat, serta mempertahankan kedua alokasi menurut penanggung biayanya.
18. **Sistem Persediaan** mencatat pengeluaran stok. **Aplikasi Pelayanan Obat** menampilkan perkembangan akhir perintah penyiapan dan pesanan penjualan apotek.

## 5. Penanganan Kondisi Khusus

### 5.1 Pasien menolak bagian yang tidak dijamin sebelum faktur dibuat

- **Staf Apotek** mencatat penolakan dan tidak membuat faktur pasien umum.
- **Aplikasi Pelayanan Obat** menandai alokasi yang harus dibayar Pasien sebagai ditolak atau tidak dialokasikan untuk penjualan. Bagian yang dijamin tetap dapat diproses secara terpisah.

### 5.2 Item obat tidak mempunyai penjaminan Fornas yang sah

- **Aplikasi Pelayanan Obat** menampilkan jumlah yang terdampak sebagai tidak dijamin.
- **Staf Apotek** hanya boleh mengubahnya menjadi alokasi tagihan Pasien melalui pencatatan yang dapat dipertanggungjawabkan, kemudian menyampaikan nilai baru dan meminta persetujuan lisan ulang.

### 5.3 Belum semua jumlah mendapat izin penyiapan

- **Aplikasi Pelayanan Obat** memblokir penyiapan dan pengambilan untuk jumlah yang belum mendapat izin.
- **Staf Apotek** menyelesaikan proses penjaminan atau pembayaran yang berlaku sebelum melanjutkan.

### 5.4 Faktur yang sudah ada, obat yang tidak dapat dilayani, atau Pasien yang tidak datang memerlukan koreksi

- **Aplikasi Pelayanan Obat** tetap memisahkan akibat pada bagian BPJS dan bagian yang dibayar Pasien.
- **Tata Rekening** memberikan koreksi untuk bagian yang telah dibayar. Faktur BPJS tetap belum dibuat sampai penyerahan berhasil.
- **Penanggung Jawab Apotek** menerapkan `SOP-MF-RJ-007` untuk obat yang tidak diambil.

## 6. Kriteria Penyelesaian

1. Alokasi tagihan BPJS dan alokasi tagihan Pasien tetap terlihat terpisah.
2. Faktur pasien umum yang sudah lunas dan faktur BPJS yang dibuat saat penyerahan terpisah, tetapi keduanya dapat ditelusuri ke pesanan penjualan apotek yang sama.
3. Satu penyerahan terkoordinasi mencatat seluruh jumlah obat yang berlaku dan penerima yang berhak.
4. Pesanan penjualan apotek berstatus `Resolved`, atau tetap `Active` dengan urusan yang belum selesai ditampilkan secara jelas menurut penanggung biayanya.

## 7. Referensi

- [Domain Pelayanan Obat](../medication-fulfillment-domain-id.md), khususnya `BR-MF-015`, `BR-MF-020`–`BR-MF-028`, `BR-MF-040`–`BR-MF-046`, `BR-MF-056`–`BR-MF-060`, `BR-MF-070`–`BR-MF-078`, dan `BR-MF-090`–`BR-MF-095`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-005`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
