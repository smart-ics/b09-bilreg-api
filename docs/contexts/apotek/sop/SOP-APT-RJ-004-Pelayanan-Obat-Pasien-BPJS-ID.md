# SOP APT-RJ-004 — Melayani Obat untuk Pasien BPJS

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-004`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-004 — Fulfill Medication for a BPJS Patient](./SOP-APT-RJ-004-Pelayanan-Obat-Pasien-BPJS-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas untuk memastikan obat rawat jalan ditanggung BPJS, menyiapkan obat, dan menyerahkannya kepada penerima yang berhak. Pasien tidak membayar di muka dan faktur BPJS baru dibuat ketika obat berhasil diserahkan.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Datang untuk mengambil obat, menerima edukasi, dan menerima obat bila berhak. |
| Staf Apotek | Petugas | Memeriksa obat yang dijamin, menyiapkan atau meracik obat setelah tugas penyiapan obat diizinkan, mengoordinasikan kesiapan obat, dan memanggil Pasien untuk mengambil obat. |
| Kepala Apotek | Petugas | Menyetujui penanganan manual untuk obat yang sudah disiapkan tetapi tidak diambil Pasien. |
| Apoteker | Petugas | Memeriksa penerima yang berhak, melakukan pemeriksaan akhir obat, dan memberikan edukasi kepada Pasien. |
| Sistem SEP dan Fornas | Subsistem | Menyediakan hasil pemeriksaan keabsahan SEP pada kunjungan Pasien dan jaminan Fornas untuk setiap item obat. |
| Sistem Apotek | Subsistem | Mencatat persetujuan jaminan BPJS dan izin penyiapan obat, memantau penyiapan, serta mencatat faktur BPJS dan penyerahan obat dalam satu hasil transaksi. |
| Sistem Antrian Pasien | Subsistem | Mencatat `ServedAt` saat penyiapan obat dimulai dan `DoneAt` saat Pasien dipanggil untuk mengambil obat. |
| Sistem Persediaan | Subsistem | Menyediakan hasil pemesanan, pengeluaran, dan keputusan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima hasil pembebanan biaya kepada BPJS. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Outpatient Queue Mapping, pesanan apotek yang aktif, bagian obat yang ditanggung BPJS, dan tugas untuk menyiapkan obat telah ditampilkan.
3. SEP untuk kunjungan Pasien masih sah, dan pemetaan Fornas menyatakan setiap jumlah obat yang ditanggung dapat dilayani.
4. Jumlah yang harus dibayar Pasien adalah nol, status pembayaran `Not Required`, dan faktur BPJS belum dibuat.

## 4. Langkah Operasional

1. **Staf Apotek** membuka permintaan obat BPJS yang sudah terkonek dengan antrian pada `Apotek Rajal`. **Staf Apotek** memeriksa Pasien, nomor rujukan SEP, bagian obat yang ditanggung BPJS, dan jumlah obat pada tugas penyiapan.
2. **Sistem SEP dan Fornas** mengirimkan hasil pemeriksaan keabsahan SEP dan jaminan untuk setiap item obat.
3. **Sistem Apotek** menampilkan bahwa setiap jumlah obat yang dijamin sudah disetujui BPJS. Untuk jumlah tersebut, aplikasi menetapkan bahwa obat boleh masuk ke proses penyiapan tanpa menunggu faktur penjualan dibuat.
4. Bila belum ada pemesanan stok, **Sistem Persediaan** mengamankan stok yang diperlukan. **Sistem Apotek** menampilkan hasil pemesanan stok tersebut.
5. **Staf Apotek** mulai menyiapkan obat hanya setelah tugas penyiapan obat berstatus `Released`.
6. **Sistem Apotek** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat `ServedAt`.
7. **Staf Apotek** menyelesaikan penyiapan atau peracikan obat dan mencatatnya. **Sistem Apotek** menampilkan tugas penyiapan obat dengan status `Prepared`.
8. **Staf Apotek** memastikan setiap tugas penyiapan obat yang akan diserahkan sudah berstatus `Prepared` atau sudah memiliki catatan alasan yang jelas bila obat tidak dapat diserahkan.
9. **Staf Apotek** melakukan satu kali panggilan agar Pasien mengambil obat. **Sistem Antrian Pasien** mengubah antrian menjadi `Done` dan mencatat `DoneAt`.
10. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima yang berhak, melakukan pemeriksaan akhir obat, dan mencatat edukasi yang perlu diberikan. Bila pemeriksaan lulus, **Sistem Apotek** menambahkan catatan pemeriksaan dan menampilkan tugas penyiapan obat berstatus `Reviewed`.
11. **Sistem Apotek** tidak mengizinkan proses diselesaikan bila pemeriksaan penerima atau pemeriksaan akhir obat belum lengkap.
12. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik.
13. Dalam satu hasil transaksi, **Sistem Apotek** membuat faktur BPJS dari bagian obat yang dijamin, mencatat obat yang diberikan, dan mencatat penyerahan obat.
14. **Sistem Persediaan** memberikan catatan resmi bahwa stok telah dikeluarkan. **Sistem Apotek** menampilkan tugas penyiapan obat berstatus `Completed` setelah catatan tersebut tersedia.
15. **Sistem Apotek** menampilkan pesanan apotek berstatus `Resolved` hanya setelah seluruh obat yang diterima dan seluruh urusan keuangannya selesai.

## 5. Pengecualian Operasional

### 5.1 SEP tidak sah atau obat tidak dijamin

- **Sistem SEP dan Fornas** tidak memberikan persetujuan jaminan untuk jumlah obat yang terdampak.
- **Sistem Apotek** tetap mencegah obat tersebut masuk ke proses penyiapan.
- Bila ada jumlah obat yang tidak dijamin, **Staf Apotek** melanjutkannya melalui `SOP-APT-RJ-005` bila sesuai.

### 5.2 Stok kurang setelah pesanan apotek dibuat

- **Staf Apotek** tidak membuat pesanan tertunda dan tidak memilih sumber stok alternatif.
- **Sistem Apotek** mencatat obat yang tidak dapat dilayani, mendukung Salinan Resep untuk item yang tidak dipenuhi, dan tetap menggunakan identitas obat yang sudah diterima.

### 5.3 Pemeriksaan akhir obat tidak lulus

- **Apoteker** mencatat alasan kegagalan dan jumlah obat yang terdampak serta tidak menyetujui penyerahan.
- **Sistem Apotek** menambahkan catatan pemeriksaan yang tidak dapat diubah, lengkap dengan Apoteker dan waktu keputusan, mengembalikan tugas penyiapan obat dari `Prepared` ke `Preparing`, serta tidak membuat faktur BPJS maupun catatan penyerahan obat.
- **Staf Apotek** memperbaiki dan menyiapkan kembali obat yang terdampak. **Sistem Apotek** mengembalikan tugas tersebut ke `Prepared`, lalu **Apoteker** melakukan pemeriksaan akhir obat yang baru. Catatan pemeriksaan sebelumnya tetap terlihat dan tidak berubah.

### 5.4 Pasien tidak mengambil obat

- **Kepala Apotek** menerapkan `SOP-APT-RJ-007`.
- **Sistem Apotek** tidak membuat atau membatalkan faktur BPJS untuk Pasien yang tidak datang mengambil obat.

## 6. Kriteria Penyelesaian

1. Jumlah yang harus dibayar Pasien adalah nol dan status pembayaran `Not Required`.
2. Faktur BPJS dan catatan penyerahan obat tampil sebagai satu hasil transaksi yang berhasil.
3. Tugas penyiapan obat berstatus `Completed`, penerima yang berhak tercatat, dan catatan resmi pengeluaran stok ditampilkan.
4. Pesanan apotek berstatus `Resolved`, atau tetap `Active` dengan masalah yang belum selesai ditampilkan dengan jelas.
5. Status antrian `Done` bukan bukti bahwa obat sudah diserahkan.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-020`–`BR-APT-026`, `BR-APT-029`–`BR-APT-045`, `BR-APT-066`, `BR-APT-068`–`BR-APT-069`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095`–`BR-APT-096`, dan `BR-APT-114`–`BR-APT-118`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-004`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), `BR-TRK-045`, `BR-TRK-045a`, dan `BR-TRK-046`.
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
