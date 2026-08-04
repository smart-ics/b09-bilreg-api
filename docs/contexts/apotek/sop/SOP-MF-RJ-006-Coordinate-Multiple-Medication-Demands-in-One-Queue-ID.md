# SOP MF-RJ-006 — Mengoordinasikan Beberapa Kebutuhan Obat dalam Satu Antrian

**Status dokumen:** Spesifikasi operasional acuan

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-MF-RJ-006`

**Dokumen acuan bahasa Inggris:** [SOP MF-RJ-006 — Coordinate Multiple Medication Demands in One Queue](./SOP-MF-RJ-006-Coordinate-Multiple-Medication-Demands-in-One-Queue.md)

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara mengoordinasikan dua atau lebih kebutuhan obat dalam satu antrian dan satu kali pengambilan, tanpa menggabungkan catatan maupun pertanggungjawaban setiap kebutuhan obat.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyelesaikan pembayaran atau penjaminan yang berlaku, hadir dalam satu kali pengambilan, menerima edukasi terpadu, dan menerima obat. |
| Staf Apotek | Petugas | Memeriksa koneksi antrian dan perkembangan setiap sumber pelayanan obat, menyiapkan setiap perintah penyiapan obat yang telah mendapat izin secara terpisah, mengoordinasikan kesiapan menurut penanggung biaya, menyampaikan kendala, dan melakukan satu kali panggilan. |
| Apoteker | Petugas | Memeriksa penerima dan setiap obat yang telah disiapkan, serta memberikan edukasi terpadu dengan aturan pakai masing-masing obat. |
| Aplikasi Pelayanan Obat | Aplikasi | Menampilkan perkembangan setiap kebutuhan obat serta mencatat alokasi, faktur, perintah penyiapan, pemberian, dan penyerahan secara terpisah. |
| Sistem Antrian Pasien | Subsistem | Mempertahankan satu entri antrian dengan satu `CreatedAt`, paling banyak satu `ServedAt`, dan satu `DoneAt`. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Memberikan status lunas untuk kebutuhan obat yang harus dibayar Pasien. |
| Sistem SEP dan Fornas | Subsistem | Memberikan bukti penjaminan bagi kebutuhan obat BPJS. |
| Sistem Persediaan | Subsistem | Menyediakan hasil pemesanan, pengeluaran, dan penyelesaian stok untuk setiap perintah penyiapan obat. |

## 3. Prasyarat

1. Petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Satu entri antrian telah dikoneksikan secara terpisah dengan sedikitnya dua sumber pelayanan obat.
3. Setiap resep memiliki telaah resepnya sendiri.
4. Setiap sumber yang diterima memiliki pesanan penjualan apotek dan perintah penyiapan obat rawat jalan utama yang aktif secara tersendiri.

## 4. Langkah Operasional

1. **Staf Apotek** membuka entri antrian pada `Apotek Rajal`.
2. **Aplikasi Pelayanan Obat** menampilkan setiap kebutuhan obat secara terpisah beserta sumber, pesanan penjualan apotek, penanggung biaya, alokasi tagihan, alokasi pelayanan, faktur, izin penyiapan, dan perkembangan penyiapan obat.
3. **Staf Apotek** memastikan tidak ada kebutuhan obat, pesanan penjualan, faktur, atau perintah penyiapan yang digabungkan dengan kebutuhan obat lain.
4. **Staf Apotek** menerapkan SOP Pasien Umum, BPJS, atau penjaminan campuran pada masing-masing kebutuhan sesuai penanggung biayanya.
5. **Kasir atau Sistem Pembayaran** memberikan status lunas yang diperlukan. **Sistem SEP dan Fornas** memberikan persetujuan penjaminan yang diperlukan.
6. **Staf Apotek** menyiapkan setiap perintah penyiapan berstatus `Released` secara terpisah dan mencatat penyelesaian masing-masing.
7. Saat penyiapan pertama dimulai, **Aplikasi Pelayanan Obat** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mencatat satu `ServedAt` dan mengubah antrian menjadi `In Service`.
8. **Aplikasi Pelayanan Obat** menampilkan setiap kebutuhan obat yang akan diambil sebagai `Prepared` atau dengan hasil penanganan khusus yang dapat dipertanggungjawabkan.
9. **Staf Apotek** meninjau perkembangan seluruh kebutuhan dan tidak menyatakan kebutuhan yang belum selesai sebagai siap.
10. Setelah seluruh kebutuhan yang akan diserahkan siap atau telah mendapat penyelesaian yang dapat dipertanggungjawabkan, **Staf Apotek** melakukan satu kali panggilan pengambilan obat.
11. **Sistem Antrian Pasien** mencatat satu `DoneAt` dan mengubah antrian menjadi `Done`.
12. **Staf Apotek** menyampaikan setiap kendala atau hasil penanganan khusus bersama informasi obat yang sudah siap.
13. Ketika Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima yang berhak, melakukan pemeriksaan akhir setiap obat yang disiapkan, dan mencatat edukasi terpadu beserta aturan pakai khusus masing-masing obat.
14. Setelah mendapat persetujuan Apoteker, **Staf Apotek** menyerahkan obat.
15. **Aplikasi Pelayanan Obat** mencatat pemberian dan penyerahan terhadap setiap perintah penyiapan dan pesanan penjualan secara terpisah.
16. **Sistem Persediaan** mencatat pengeluaran atau penyelesaian stok secara terpisah untuk setiap perintah penyiapan sumber.

## 5. Penanganan Kondisi Khusus

### 5.1 Satu kebutuhan obat belum selesai

- **Aplikasi Pelayanan Obat** menampilkan kebutuhan tersebut sebagai belum siap.
- **Staf Apotek** menunda panggilan bersama, kecuali SOP penanggung biaya mengizinkan penyerahan sebagian dan persetujuan Pasien atas penyerahan sebagian telah dicatat.

### 5.2 Satu kebutuhan obat mempunyai hasil penanganan khusus

- **Staf Apotek** menyampaikan hasil tersebut saat memberikan informasi pengambilan. Kebutuhan lain yang sudah siap hanya boleh dilanjutkan bila SOP penanggung biaya mengizinkannya.
- **Aplikasi Pelayanan Obat** menyimpan hasil penanganan khusus di bawah pesanan penjualan dan perintah penyiapan sumbernya.

### 5.3 Koneksi antrian atau sumber pelayanan obat perlu dikoreksi

- **Staf Apotek** hanya mengoreksi koneksi atau sumber pelayanan obat yang terdampak.
- **Aplikasi Pelayanan Obat** mempertahankan riwayat kebutuhan lainnya. Satu catatan penyerahan tidak boleh dianggap menyelesaikan kebutuhan obat lain.

## 6. Kriteria Penyelesaian

1. Entri antrian yang sama menampilkan satu `CreatedAt`, paling banyak satu `ServedAt`, dan satu `DoneAt`.
2. Setiap resep, pesanan penjualan apotek, faktur, dan perintah penyiapan obat mempertahankan identitas serta perkembangannya masing-masing.
3. Hanya ada satu panggilan pengambilan, sedangkan setiap kebutuhan obat mempunyai catatan penyerahan sendiri atau hasil penanganan khusus yang dapat dipertanggungjawabkan.

## 7. Referensi

- [Domain Pelayanan Obat](../medication-fulfillment-domain-id.md), khususnya `BR-MF-011`, `BR-MF-015`, `BR-MF-022`, `BR-MF-030`, `BR-MF-056`–`BR-MF-060`, `BR-MF-084`–`BR-MF-088`, dan `BR-MF-095`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-006`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), khususnya `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, dan `BR-TRK-045a`.
