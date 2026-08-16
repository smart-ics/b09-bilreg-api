# SOP APT-RJ-003 — Melayani Obat untuk Pasien Umum

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-003`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-003 — Fulfill Medication for a General Patient](./SOP-APT-RJ-003-Pelayanan-Obat-Pasien-Umum-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas untuk memperoleh persetujuan pembelian secara lisan, membuat tagihan Pasien Umum, menerima pembayaran, menyiapkan obat, dan menyerahkan obat rawat jalan kepada penerima yang berhak.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyetujui atau menolak pembelian, membayar setelah menyetujui, datang untuk mengambil obat, menerima edukasi, dan menerima obat bila berhak. |
| Staf Apotek | Petugas | Menyampaikan nilai yang harus dibayar, mencatat transaksi yang disetujui, menyiapkan atau meracik obat setelah tugas penyiapan obat diizinkan, mengoordinasikan kesiapan obat, dan memanggil Pasien untuk mengambil obat. |
| Kepala Apotek | Petugas | Menyetujui penanganan manual untuk obat yang sudah disiapkan tetapi tidak diambil Pasien. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Menerima pembayaran dan mengirimkan informasi bahwa tagihan telah lunas. |
| Apoteker | Petugas | Memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. Pemeriksaan penerima tidak ditegakkan sistem. Catatan konseling rinci bersifat opsional. |
| Sistem Apotek | Subsistem | Menampilkan tagihan dan nilai yang harus dibayar, mencatat faktur serta status pembayaran, memantau penyiapan obat, dan mencatat obat yang diberikan serta diserahkan. |
| Sistem Antrian Pasien | Subsistem | Mencatat `ServedAt` saat penyiapan obat dimulai dan `DoneAt` saat Pasien dipanggil untuk mengambil obat. |
| Sistem Persediaan | Subsistem | Menyediakan hasil pemesanan, pengeluaran, dan keputusan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima beban keuangan dan memberikan hasil koreksi yang diperlukan. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Outpatient Queue Mapping, pesanan apotek yang aktif, bagian obat yang menjadi tagihan Pasien, dan rincian harga telah ditampilkan.
3. Belum ada faktur penjualan untuk obat Pasien Umum yang akan ditawarkan.
4. Tugas untuk menyiapkan obat sudah tersedia atau dapat dibuat dari obat yang telah memenuhi syarat untuk dilayani.

## 4. Langkah Operasional

1. **Staf Apotek** membuka permintaan obat yang sudah terkonek dengan antrian pada `Apotek Rajal`. **Staf Apotek** memeriksa jumlah yang harus dibayar Pasien dan bagian obat yang menjadi tagihan.
2. Saat Manual Mapping dilakukan, **Staf Apotek** menerima Pasien. Setelah Tracker Mapping, **Staf Apotek** dapat memanggil nomor antrian untuk keperluan administrasi. **Sistem Antrian Pasien** tidak mencatat `ServedAt` atau `DoneAt` pada tahap ini.
3. **Staf Apotek** menyampaikan secara lisan jumlah yang harus dibayar sebelum faktur penjualan dibuat.
4. **Pasien atau Keluarga Pasien** menyatakan persetujuan pembelian secara lisan.
5. **Staf Apotek** mencatat transaksi yang disetujui. **Sistem Apotek** membuat faktur penjualan hanya dari bagian tagihan yang telah disetujui, lalu menampilkan nomor faktur dan jumlahnya.
6. **Kasir atau Sistem Pembayaran** menerima pembayaran dan mengirimkan informasi pelunasan untuk faktur tersebut.
7. **Sistem Apotek** menampilkan informasi bahwa faktur telah lunas. Untuk jumlah obat yang terkait, aplikasi menetapkan bahwa obat sudah boleh masuk ke proses penyiapan.
8. Bila belum ada pemesanan stok, **Sistem Persediaan** mengamankan stok yang diperlukan. **Sistem Apotek** menampilkan hasil pemesanan stok tersebut.
9. **Staf Apotek** mulai menyiapkan obat hanya setelah tugas penyiapan obat berstatus `Released`.
10. **Sistem Apotek** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat `ServedAt`.
11. **Staf Apotek** menyelesaikan penyiapan atau peracikan obat dan mencatatnya. **Sistem Apotek** menampilkan tugas penyiapan obat dengan status `Prepared`.
12. **Staf Apotek** memastikan setiap tugas penyiapan obat yang akan diserahkan sudah berstatus `Prepared` atau sudah memiliki catatan alasan yang jelas bila obat tidak dapat diserahkan.
13. **Staf Apotek** melakukan satu kali panggilan agar Pasien mengambil obat. **Sistem Antrian Pasien** mengubah antrian menjadi `Done` dan mencatat `DoneAt`.
14. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. Bila pemeriksaan lulus, **Sistem Apotek** menambahkan catatan pemeriksaan dan menampilkan tugas penyiapan obat berstatus `Reviewed`. **Sistem Apotek** mencatat waktu edukasi dan Apoteker penanggung jawab. Catatan konseling rinci bersifat opsional. Apoteker boleh secara opsional mencatat nomor telepon penerima dan hubungan dengan Pasien sebagai referensi.
15. **Sistem Apotek** tidak mengizinkan obat diserahkan sampai pemeriksaan akhir lulus dan Patient Education Acknowledgement dicatat. Identitas penerima bukan gate sistem. Catatan konseling rinci tidak diwajibkan.
16. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik. **Sistem Apotek** mencatat obat yang diberikan dan diserahkan untuk setiap jumlah obat yang berlaku.
17. **Sistem Persediaan** memberikan catatan resmi bahwa stok telah dikeluarkan. Bila seluruh catatan yang diperlukan sudah tersedia, **Sistem Apotek** menampilkan tugas penyiapan obat berstatus `Completed`.
18. **Sistem Apotek** menampilkan pesanan apotek berstatus `Resolved` hanya setelah seluruh obat yang diterima dan seluruh urusan keuangannya selesai.

## 5. Pengecualian Operasional

### 5.1 Pasien menolak sebelum faktur dibuat

- **Staf Apotek** mencatat penolakan dan tidak membuat faktur penjualan.
- **Sistem Apotek** menandai bagian tagihan sebagai ditolak atau belum dialokasikan untuk penjualan, lalu meminta **Sistem Persediaan** melepaskan pemesanan stok yang tidak digunakan.

### 5.2 Jumlah tagihan berubah sebelum faktur dibuat

- **Sistem Apotek** menampilkan jumlah terbaru yang harus dibayar.
- **Staf Apotek** menyampaikan kembali jumlah tersebut dan meminta persetujuan lisan baru sebelum mencatat transaksi.

### 5.3 Pembayaran belum lunas atau faktur yang sudah dibuat perlu dikoreksi

- **Sistem Apotek** tetap mencegah penyiapan obat dimulai selama informasi pelunasan belum tersedia.
- **Staf Apotek** hanya dapat membatalkan faktur bila status faktur yang ditampilkan mengizinkan. Bila tidak, **Tata Rekening** memberikan penyesuaian keuangan, nota kredit, pengembalian dana, atau hasil koreksi lain yang dapat dipertanggungjawabkan.

### 5.4 Stok kurang

- **Staf Apotek** tidak membuat pesanan tertunda dan tidak memilih sumber stok alternatif. **Staf Apotek** tidak mengganti obat.
- **Sistem Apotek** mencatat obat yang tidak dapat dilayani, mendukung Salinan Resep untuk item yang tidak dipenuhi, dan tetap menampilkan urusan keuangan yang harus diselesaikan.

### 5.5 Pemeriksaan akhir obat tidak lulus

- Bila pemeriksaan akhir obat tidak lulus, **Apoteker** mencatat alasan dan jumlah obat yang terdampak serta tidak menyetujui penyerahan.
- **Sistem Apotek** menambahkan catatan pemeriksaan yang tidak dapat diubah, lengkap dengan Apoteker dan waktu keputusan, mengembalikan tugas penyiapan obat dari `Prepared` ke `Preparing`, dan tetap mencegah penyerahan.
- **Staf Apotek** memperbaiki dan menyiapkan kembali obat yang terdampak. **Sistem Apotek** mengembalikan tugas tersebut ke `Prepared`, lalu **Apoteker** melakukan pemeriksaan akhir obat yang baru. Catatan pemeriksaan sebelumnya tetap terlihat dan tidak berubah.

### 5.6 Pasien tidak mengambil obat

- **Kepala Apotek** menerapkan `SOP-APT-RJ-007`. Catatan `DoneAt` pada antrian tidak dihapus atau diubah kembali.

## 6. Kriteria Penyelesaian

1. Faktur penjualan terlihat sudah lunas.
2. Tugas penyiapan obat berstatus `Completed`, dan catatan penyerahan obat mencantumkan waktu penyerahan. Nomor telepon penerima dan hubungan dengan Pasien boleh dicatat secara opsional sebagai referensi.
3. Pengeluaran stok terlihat sebagai catatan resmi dari Sistem Persediaan.
4. Pesanan apotek berstatus `Resolved`, atau tetap `Active` dengan masalah pelayanan obat atau urusan keuangan yang belum selesai ditampilkan dengan jelas.
5. Status antrian `Done` bukan bukti bahwa obat sudah diserahkan.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, dan `BR-APT-125`–`BR-APT-134`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-003`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), `BR-TRK-045`, `BR-TRK-045a`, dan `BR-TRK-046`.
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
