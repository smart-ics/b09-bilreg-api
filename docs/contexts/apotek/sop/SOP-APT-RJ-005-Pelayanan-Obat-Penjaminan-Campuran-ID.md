# SOP APT-RJ-005 — Melayani Obat dengan Penjaminan Campuran

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-005`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-005 — Fulfill Mixed-Coverage Medication](./SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas ketika Fornas mengklasifikasikan sebagian baris resep sebagai Covered dan sebagian sebagai Not Covered. Baris Covered membentuk Sales Order BPJS. Baris Not Covered boleh membentuk Patient-Pay Sales Order independen. Kedua pesanan dapat diserahkan bersama dalam satu kali pengambilan.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyetujui atau menolak Patient-Pay Sales Order, membayar setelah menyetujui, datang untuk mengambil obat, menerima edukasi, dan menerima obat bila berhak. |
| Staf Apotek | Petugas | Membentuk Sales Order BPJS untuk baris Covered dan Patient-Pay Sales Order independen untuk baris Not Covered, menyampaikan jumlah yang harus dibayar Pasien, mencatat transaksi yang disetujui, menyiapkan obat yang sudah boleh diproses, dan memanggil Pasien. |
| Kepala Apotek | Petugas | Menyetujui penanganan manual untuk obat yang sudah disiapkan tetapi tidak diambil Pasien. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Menerima pembayaran dan mengirimkan informasi bahwa faktur Patient-Pay telah lunas. |
| Apoteker | Petugas | Memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. Pemeriksaan penerima tidak ditegakkan sistem. Catatan konseling rinci bersifat opsional. |
| Sistem SEP dan Fornas | Subsistem | Mengklasifikasikan setiap baris resep sebagai Covered atau Not Covered dan menyediakan keabsahan SEP. |
| Sistem Apotek | Subsistem | Menyimpan Sales Order independen, faktur sesuai jalur payer, dan evaluasi Dispense Authorized per baris. |
| Sistem Antrian Pasien | Subsistem | Mencatat satu `ServedAt` dan satu `DoneAt` untuk antrian yang sama. |
| Sistem Persediaan | Subsistem | Menyediakan hasil Mutasi, Remove Stock, dan keputusan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima dan menyelesaikan urusan keuangan sesuai jalur payer. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Fornas telah mengklasifikasikan baris resep sebagai Covered atau Not Covered.
3. SEP masih sah untuk baris Covered.
4. Baris Not Covered tidak dibatalkan secara otomatis.
5. Belum ada faktur Patient-Pay maupun faktur BPJS untuk baris tersebut, kecuali proses dilanjutkan setelah kondisi khusus yang sudah tercatat.

## 4. Langkah Operasional

1. **Sistem SEP dan Fornas** mengklasifikasikan setiap baris resep sebagai Covered atau Not Covered. **Sistem Apotek** menampilkan klasifikasi tersebut.
2. **Staf Apotek** membentuk Sales Order BPJS hanya dari baris Covered. Baris yang tidak dijamin tidak tetap pada jalur BPJS.
3. **Staf Apotek** boleh membentuk Patient-Pay Sales Order terpisah untuk baris Not Covered.
4. **Sistem Apotek** menghitung dan menampilkan jumlah yang harus dibayar Pasien dari Patient-Pay Sales Order.
5. **Staf Apotek** menyampaikan jumlah tersebut secara lisan sebelum faktur Pasien Umum dibuat.
6. **Pasien atau Keluarga Pasien** menyatakan persetujuan lisan atas Patient-Pay Sales Order.
7. **Staf Apotek** mencatat transaksi yang disetujui. **Sistem Apotek** membuat faktur Pasien Umum dari Patient-Pay Sales Order tersebut.
8. **Kasir atau Sistem Pembayaran** menerima pembayaran dan mengirimkan informasi pelunasan untuk faktur Patient-Pay.
9. **Sistem Apotek** mengevaluasi Dispense Authorized secara independen: baris Covered dari evidence coverage; baris Patient-Pay dari Payment Clearance.
10. Setelah setiap jumlah obat yang akan diserahkan memperoleh Dispense Authorized, **Staf Apotek** memulai dan menyelesaikan penyiapan obat pada setiap Dispense Order yang berlaku.
11. Saat penyiapan obat pertama dimulai, **Sistem Apotek** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat satu `ServedAt`.
12. **Sistem Apotek** menampilkan setiap tugas penyiapan obat yang akan diserahkan sebagai `Prepared` atau dengan catatan alasan yang jelas bila obat tidak dapat diserahkan.
13. **Staf Apotek** melakukan satu kali panggilan agar Pasien mengambil obat. **Sistem Antrian Pasien** mengubah antrian yang sama menjadi `Done` dan mencatat satu `DoneAt`.
14. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. **Sistem Apotek** mencatat waktu edukasi dan Apoteker penanggung jawab. Catatan konseling rinci bersifat opsional. Apoteker boleh secara opsional mencatat nomor telepon penerima dan hubungan dengan Pasien sebagai referensi.
15. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik.
16. Ketika obat Sales Order BPJS berhasil diserahkan, **Sistem Apotek** membuat faktur BPJS. Aplikasi juga mencatat obat yang diberikan dan diserahkan untuk seluruh jumlah obat, serta tetap memisahkan kedua Sales Order.
17. **Sistem Persediaan** mengirimkan catatan Remove Stock. **Sistem Apotek** menampilkan perkembangan akhir tugas penyiapan obat dan masing-masing Sales Order.

## 5. Pengecualian Operasional

### 5.1 Pasien menolak Patient-Pay Sales Order sebelum faktur dibuat

- **Staf Apotek** mencatat penolakan dan tidak membuat faktur Pasien Umum.
- **Sistem Apotek** mencatat outcome declined pada Patient-Pay Sales Order. Sales Order BPJS tetap dapat diproses secara independen.

### 5.2 Baris diklasifikasikan Not Covered

- **Sistem Apotek** menampilkan baris terdampak sebagai Not Covered dan tidak memasukkannya ke Sales Order BPJS.
- **Staf Apotek** boleh membentuk Patient-Pay Sales Order independen, menyampaikan jumlah, dan meminta persetujuan lisan.

### 5.3 Belum semua jumlah obat memperoleh Dispense Authorized

- **Sistem Apotek** mencegah penyiapan dan pengambilan bersama untuk jumlah obat yang belum authorized.
- **Staf Apotek** menyelesaikan proses penjaminan atau pembayaran yang berlaku sebelum melanjutkan.

### 5.4 Pemeriksaan akhir obat tidak lulus

- **Apoteker** mencatat alasan kegagalan dan jumlah obat yang terdampak serta tidak menyetujui penyerahan.
- **Sistem Apotek** menambahkan catatan pemeriksaan yang tidak dapat diubah, mengembalikan hanya tugas penyiapan obat terdampak dari `Prepared` ke `Preparing`, dan tidak menulis ulang Sales Order lain.
- **Staf Apotek** memperbaiki dan menyiapkan kembali obat yang terdampak, lalu **Apoteker** melakukan pemeriksaan akhir obat yang baru.

### 5.5 Faktur yang sudah ada, obat yang tidak dapat dilayani, atau Pasien yang tidak datang memerlukan koreksi

- **Sistem Apotek** tetap memisahkan konsekuensi Sales Order BPJS dan Patient-Pay.
- **Tata Rekening** memberikan koreksi yang diperlukan untuk bagian Patient-Pay yang telah dibayar. Faktur BPJS tetap belum dibuat sampai obat Sales Order BPJS berhasil diserahkan.
- **Kepala Apotek** menerapkan `SOP-APT-RJ-007` untuk obat yang tidak diambil.

## 6. Kriteria Penyelesaian

1. Baris Covered dan Not Covered tetap pada Sales Order yang independen.
2. Faktur Pasien Umum yang sudah lunas dan faktur BPJS yang dibuat saat obat diserahkan tampil terpisah dan terhubung ke Sales Order masing-masing.
3. Satu kali penyerahan obat dapat mencatat seluruh jumlah obat yang berlaku. Nomor telepon penerima dan hubungan dengan Pasien boleh dicatat secara opsional sebagai referensi.
4. Setiap Sales Order berstatus `Resolved`, atau tetap `Active` dengan masalah yang belum selesai ditampilkan secara jelas.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-011`, `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, `BR-APT-090`–`BR-APT-096`, `BR-APT-108`, `BR-APT-119`–`BR-APT-124`, dan `BR-APT-129`–`BR-APT-134`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-005`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
