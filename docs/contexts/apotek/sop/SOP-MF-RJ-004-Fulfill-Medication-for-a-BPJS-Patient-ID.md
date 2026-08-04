# SOP MF-RJ-004 — Melayani Obat Pasien BPJS

**Status dokumen:** Spesifikasi operasional acuan

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-MF-RJ-004`

**Dokumen acuan bahasa Inggris:** [SOP MF-RJ-004 — Fulfill Medication for a BPJS Patient](./SOP-MF-RJ-004-Fulfill-Medication-for-a-BPJS-Patient.md)

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara menyiapkan dan menyerahkan obat rawat jalan yang ditanggung BPJS tanpa pembayaran Pasien maupun faktur di muka. Faktur BPJS baru dibuat bersamaan dengan penyerahan obat yang berhasil.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Hadir saat dipanggil, menerima edukasi, dan menerima obat setelah disetujui Apoteker. |
| Staf Apotek | Petugas | Memeriksa obat yang dijamin, menyiapkan atau meracik obat berdasarkan perintah yang telah diizinkan, mengoordinasikan kesiapan, dan memanggil Pasien untuk mengambil obat. |
| Penanggung Jawab Apotek | Petugas | Menyetujui penanganan obat yang tidak diambil. |
| Apoteker | Petugas | Memeriksa penerima, melakukan pemeriksaan akhir obat, dan memberikan edukasi kepada Pasien. |
| Sistem SEP dan Fornas | Subsistem | Memberikan hasil pemeriksaan keabsahan SEP dan penjaminan Fornas untuk setiap item obat. |
| Aplikasi Pelayanan Obat | Aplikasi | Mencatat persetujuan penjaminan dan izin penyiapan, memantau penyiapan, serta mencatat faktur BPJS dan penyerahan obat sebagai satu kesatuan transaksi. |
| Sistem Antrian Pasien | Subsistem | Mencatat `ServedAt` saat penyiapan dimulai dan `DoneAt` saat Pasien dipanggil untuk mengambil obat. |
| Sistem Persediaan | Subsistem | Menyediakan hasil pemesanan, pengeluaran, dan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima beban keuangan BPJS. |

## 3. Prasyarat

1. Petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Koneksi antrian rawat jalan, pesanan penjualan apotek yang aktif, alokasi tagihan BPJS, dan perintah penyiapan obat telah ditampilkan.
3. SEP masih sah dan pemetaan Fornas mendukung setiap jumlah obat yang dijamin.
4. Nilai yang harus dibayar Pasien adalah nol, status pembayaran `Not Required`, dan faktur BPJS belum dibuat.

## 4. Langkah Operasional

1. **Staf Apotek** membuka kebutuhan obat BPJS yang telah terhubung dengan antrian pada `Apotek Rajal`, lalu memeriksa Pasien, referensi SEP, alokasi tagihan BPJS, dan jumlah obat.
2. **Sistem SEP dan Fornas** memberikan hasil pemeriksaan keabsahan SEP dan penjaminan setiap item obat.
3. **Aplikasi Pelayanan Obat** menampilkan persetujuan penjaminan bagi setiap jumlah yang dijamin dan memberikan izin penyiapan tanpa mensyaratkan faktur penjualan.
4. Bila diperlukan, **Sistem Persediaan** memesan stok dan **Aplikasi Pelayanan Obat** menampilkan hasilnya.
5. **Staf Apotek** mulai menyiapkan obat hanya setelah perintah penyiapan berstatus `Released`.
6. **Aplikasi Pelayanan Obat** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat `ServedAt`.
7. **Staf Apotek** menyelesaikan penyiapan atau peracikan dan mencatat hasilnya. **Aplikasi Pelayanan Obat** menampilkan perintah penyiapan sebagai `Prepared`.
8. **Staf Apotek** memastikan setiap obat yang akan diserahkan sudah berstatus `Prepared` atau mempunyai hasil penanganan khusus yang dapat dipertanggungjawabkan.
9. **Staf Apotek** melakukan satu kali panggilan pengambilan obat. **Sistem Antrian Pasien** mengubah antrian menjadi `Done` dan mencatat `DoneAt`.
10. Ketika Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima yang berhak, melakukan pemeriksaan akhir obat, dan mencatat edukasi yang diberikan.
11. **Aplikasi Pelayanan Obat** tidak mengizinkan penyelesaian transaksi sebelum identitas penerima dan pemeriksaan akhir lengkap.
12. Setelah mendapat persetujuan Apoteker, **Staf Apotek** menyerahkan obat.
13. Dalam satu transaksi yang dapat dipertanggungjawabkan, **Aplikasi Pelayanan Obat** membuat faktur BPJS dari alokasi yang dijamin serta mencatat pemberian dan penyerahan obat.
14. **Sistem Persediaan** mencatat pengeluaran stok. **Aplikasi Pelayanan Obat** kemudian menampilkan perintah penyiapan sebagai `Completed`.
15. Pesanan penjualan apotek hanya berubah menjadi `Resolved` setelah seluruh jumlah yang diterima dan akibat komersialnya selesai.

## 5. Penanganan Kondisi Khusus

### 5.1 SEP tidak sah atau obat tidak dijamin

- **Sistem SEP dan Fornas** tidak memberikan persetujuan penjaminan untuk jumlah yang terdampak.
- **Aplikasi Pelayanan Obat** tetap memblokir penyiapan obat tersebut.
- Bila jumlah tersebut harus dibayar Pasien, **Staf Apotek** melanjutkan melalui `SOP-MF-RJ-005`.

### 5.2 Stok kurang setelah pesanan dibuat

- **Staf Apotek** mencatat pesanan tertunda atau memilih sumber stok lain yang disetujui untuk obat yang sama.
- **Aplikasi Pelayanan Obat** mempertahankan identitas obat yang telah diterima dan menampilkan masalah yang belum selesai.

### 5.3 Pemeriksaan akhir obat gagal

- **Apoteker** mencatat hasil pemeriksaan yang gagal dan tidak menyetujui penyerahan.
- **Aplikasi Pelayanan Obat** tidak membuat faktur BPJS maupun catatan penyerahan obat.

### 5.4 Pasien tidak mengambil obat

- **Penanggung Jawab Apotek** menerapkan `SOP-MF-RJ-007`.
- Untuk Pasien yang tidak datang, **Aplikasi Pelayanan Obat** tidak membuat faktur BPJS dan tidak perlu membatalkan faktur BPJS karena faktur tersebut memang belum ada.

## 6. Kriteria Penyelesaian

1. Nilai yang harus dibayar Pasien adalah nol dan status pembayaran `Not Required`.
2. Faktur BPJS dan catatan penyerahan obat tampil sebagai satu transaksi yang berhasil dan dapat dipertanggungjawabkan.
3. Perintah penyiapan berstatus `Completed`, penerima yang berhak tercatat, dan pengeluaran stok telah ditampilkan.
4. Pesanan penjualan apotek berstatus `Resolved`, atau tetap `Active` dengan urusan yang belum selesai ditampilkan secara jelas.
5. Status antrian `Done` tidak boleh dianggap sebagai bukti bahwa obat telah diserahkan.

## 7. Referensi

- [Domain Pelayanan Obat](../medication-fulfillment-domain-id.md), khususnya `BR-MF-020`–`BR-MF-026`, `BR-MF-029`–`BR-MF-045`, `BR-MF-066`, `BR-MF-068`–`BR-MF-069`, `BR-MF-073`–`BR-MF-079`, `BR-MF-081`–`BR-MF-083`, `BR-MF-088`, `BR-MF-090`, dan `BR-MF-095`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-004`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), `BR-TRK-045`, `BR-TRK-045a`, dan `BR-TRK-046`.
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
