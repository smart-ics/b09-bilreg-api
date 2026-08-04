# SOP APT-RJ-005 — Melayani Obat dengan Penjaminan Campuran

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-005`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-005 — Fulfill Mixed-Coverage Medication](./SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas ketika satu pesanan obat berisi obat yang ditanggung BPJS dan obat yang harus dibayar Pasien. Kedua bagian diproses menurut penanggung biayanya masing-masing, tetapi obat yang sudah siap dapat diserahkan bersama dalam satu kali pengambilan.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyetujui atau menolak bagian yang harus dibayar sendiri, membayar setelah menyetujui, datang untuk mengambil obat, menerima edukasi, dan menerima obat bila berhak. |
| Staf Apotek | Petugas | Memisahkan bagian tagihan BPJS dan Pasien, menyampaikan jumlah yang harus dibayar Pasien, mencatat transaksi yang disetujui, menyiapkan atau meracik obat yang sudah boleh diproses, mengoordinasikan kesiapan obat, dan memanggil Pasien. |
| Kepala Apotek | Petugas | Menyetujui penanganan manual untuk obat yang sudah disiapkan tetapi tidak diambil Pasien. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Menerima pembayaran dan mengirimkan informasi bahwa faktur Pasien Umum telah lunas. |
| Apoteker | Petugas | Memeriksa penerima yang berhak, melakukan pemeriksaan akhir obat, dan memberikan edukasi kepada Pasien. |
| Sistem SEP dan Fornas | Subsistem | Menyediakan hasil pemeriksaan keabsahan SEP dan jaminan untuk setiap item obat. |
| Sistem Apotek | Subsistem | Menyimpan catatan tagihan dan persetujuan menurut penanggung biaya, membuat faktur secara terpisah, dan mengoordinasikan penyerahan obat. |
| Sistem Antrian Pasien | Subsistem | Mencatat satu `ServedAt` dan satu `DoneAt` untuk antrian yang sama. |
| Sistem Persediaan | Subsistem | Menyediakan hasil pemesanan, pengeluaran, dan keputusan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima dan menyelesaikan urusan keuangan sesuai penanggung biaya. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Satu pesanan apotek yang aktif memuat obat yang ditanggung BPJS dan obat yang harus dibayar Pasien.
3. SEP masih sah dan pemetaan Fornas menunjukkan jumlah obat yang dijamin serta yang tidak dijamin.
4. Catatan obat yang boleh diproses dan tugas untuk menyiapkan obat telah ditampilkan.
5. Belum ada faktur Pasien Umum maupun faktur BPJS untuk bagian obat tersebut, kecuali proses dilanjutkan setelah kondisi khusus yang sudah tercatat.

## 4. Langkah Operasional

1. **Staf Apotek** membuka permintaan obat dengan penjaminan campuran pada `Apotek Rajal`. **Staf Apotek** memeriksa jumlah obat dalam pesanan apotek dan penanggung biaya setiap jumlah obat.
2. **Staf Apotek** mencatat secara terpisah bagian obat yang ditanggung BPJS dan bagian yang harus dibayar Pasien. Identitas dan jumlah obat yang sudah diterima tidak diubah.
3. **Sistem SEP dan Fornas** mengirimkan hasil pemeriksaan SEP dan jaminan untuk setiap item obat. **Sistem Apotek** menampilkan persetujuan jaminan untuk jumlah obat yang ditanggung BPJS.
4. **Sistem Apotek** menghitung dan menampilkan jumlah yang harus dibayar Pasien untuk obat yang tidak dijamin.
5. **Staf Apotek** menyampaikan jumlah tersebut secara lisan sebelum faktur Pasien Umum dibuat.
6. **Pasien atau Keluarga Pasien** menyatakan persetujuan lisan atas bagian obat yang harus dibayar sendiri.
7. **Staf Apotek** mencatat transaksi yang disetujui. **Sistem Apotek** membuat faktur Pasien Umum secara terpisah untuk bagian obat yang dibayar Pasien.
8. **Kasir atau Sistem Pembayaran** menerima pembayaran dan mengirimkan informasi pelunasan untuk faktur Pasien Umum.
9. **Sistem Apotek** menetapkan obat sudah boleh diproses: berdasarkan persetujuan jaminan untuk bagian BPJS dan berdasarkan informasi pelunasan untuk bagian yang dibayar Pasien.
10. **Sistem Persediaan** mengamankan stok yang diperlukan dan mengirimkan hasilnya.
11. Setelah setiap jumlah obat yang akan diserahkan sudah boleh diproses, **Staf Apotek** memulai dan menyelesaikan penyiapan obat.
12. Saat penyiapan obat pertama dimulai, **Sistem Apotek** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat satu `ServedAt`.
13. **Sistem Apotek** menampilkan setiap tugas penyiapan obat yang akan diserahkan sebagai `Prepared` atau dengan catatan alasan yang jelas bila obat tidak dapat diserahkan.
14. **Staf Apotek** melakukan satu kali panggilan agar Pasien mengambil obat. **Sistem Antrian Pasien** mengubah antrian yang sama menjadi `Done` dan mencatat satu `DoneAt`.
15. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima yang berhak, melakukan pemeriksaan akhir obat, dan mencatat edukasi yang perlu diberikan. Bila pemeriksaan lulus, **Sistem Apotek** menambahkan catatan pemeriksaan dan menampilkan tugas penyiapan obat berstatus `Reviewed`.
16. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik.
17. Ketika obat berhasil diserahkan, **Sistem Apotek** membuat faktur BPJS untuk bagian obat yang dijamin. Aplikasi juga mencatat obat yang diberikan dan diserahkan untuk seluruh jumlah obat, serta tetap menyimpan pemisahan menurut penanggung biayanya.
18. **Sistem Persediaan** mengirimkan catatan pengeluaran stok. **Sistem Apotek** menampilkan perkembangan akhir tugas penyiapan obat dan pesanan apotek.

## 5. Pengecualian Operasional

### 5.1 Pasien menolak bagian obat yang tidak dijamin sebelum faktur dibuat

- **Staf Apotek** mencatat penolakan dan tidak membuat faktur Pasien Umum.
- **Sistem Apotek** menandai bagian yang harus dibayar Pasien sebagai ditolak atau belum dialokasikan untuk penjualan. Bagian yang dijamin BPJS tetap dapat diproses secara terpisah.

### 5.2 Item obat tidak memiliki jaminan Fornas yang sah

- **Sistem Apotek** menampilkan jumlah obat yang terdampak sebagai tidak dijamin.
- **Staf Apotek** hanya dapat memindahkan jumlah tersebut menjadi bagian yang dibayar Pasien melalui pencatatan yang dapat dipertanggungjawabkan. **Staf Apotek** kemudian menyampaikan jumlah terbaru dan meminta persetujuan lisan ulang.

### 5.3 Belum semua jumlah obat boleh diproses

- **Sistem Apotek** mencegah penyiapan dan pengambilan bersama untuk jumlah obat yang belum mendapat persetujuan jaminan atau belum lunas.
- **Staf Apotek** menyelesaikan proses penjaminan atau pembayaran yang berlaku sebelum melanjutkan.

### 5.4 Pemeriksaan akhir obat tidak lulus

- **Apoteker** mencatat alasan kegagalan dan jumlah obat yang terdampak serta tidak menyetujui penyerahan.
- **Sistem Apotek** menambahkan catatan pemeriksaan yang tidak dapat diubah, lengkap dengan Apoteker dan waktu keputusan, mengembalikan tugas penyiapan obat terdampak dari `Prepared` ke `Preparing`, mencegah penyerahan bersama, dan tetap tidak membuat faktur BPJS.
- **Staf Apotek** memperbaiki dan menyiapkan kembali obat yang terdampak. **Sistem Apotek** mengembalikan tugas tersebut ke `Prepared`, lalu **Apoteker** melakukan pemeriksaan akhir obat yang baru. Catatan pemeriksaan sebelumnya tetap terlihat dan tidak berubah.

### 5.5 Faktur yang sudah ada, obat yang tidak dapat dilayani, atau Pasien yang tidak datang memerlukan koreksi

- **Sistem Apotek** tetap memisahkan urusan BPJS dan urusan bagian yang dibayar Pasien.
- **Tata Rekening** memberikan koreksi yang diperlukan untuk bagian yang telah dibayar. Faktur BPJS tetap belum dibuat sampai obat berhasil diserahkan.
- **Kepala Apotek** menerapkan `SOP-APT-RJ-007` untuk obat yang tidak diambil.

## 6. Kriteria Penyelesaian

1. Bagian obat yang ditanggung BPJS dan bagian yang dibayar Pasien tetap terlihat terpisah.
2. Faktur Pasien Umum yang sudah lunas dan faktur BPJS yang dibuat saat obat diserahkan tampil terpisah, tetapi keduanya tetap terhubung ke pesanan apotek yang sama.
3. Satu kali penyerahan obat mencatat seluruh jumlah obat yang berlaku dan penerima yang berhak.
4. Pesanan apotek berstatus `Resolved`, atau tetap `Active` dengan masalah yang belum selesai ditampilkan secara jelas menurut penanggung biayanya.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, dan `BR-APT-090`–`BR-APT-096`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-005`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
