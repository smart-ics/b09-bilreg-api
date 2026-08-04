# SOP MF-RJ-002 — Menerima Kebutuhan Obat Pasien Rawat Jalan

**Status dokumen:** Spesifikasi operasional acuan

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-MF-RJ-002`

**Dokumen acuan bahasa Inggris:** [SOP MF-RJ-002 — Accept Outpatient Medication Demand](./SOP-MF-RJ-002-Accept-Outpatient-Medication-Demand.md)

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara menerima resep yang telah ditelaah atau permintaan obat langsung yang telah disetujui, kemudian membuat pesanan penjualan apotek dan perintah penyiapan obat rawat jalan yang dapat ditelusuri kembali ke sumbernya.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Apoteker | Petugas | Menelaah setiap item obat, melakukan klarifikasi kepada Tenaga Medis Penulis Resep di luar sistem bila diperlukan, menetapkan obat yang diterima, dan menyelesaikan telaah resep. |
| Staf Apotek | Petugas | Mencatat resep kertas; menerima, meneruskan, atau menolak permintaan obat langsung sesuai kewenangan; serta menindaklanjuti ketersediaan stok dengan mencatat pesanan tertunda atau memilih sumber stok lain yang disetujui untuk obat yang sama. |
| CPOE | Subsistem | Menyediakan resep asli yang sah dan tidak diubah oleh Aplikasi Pelayanan Obat. |
| Katalog Obat | Subsistem | Menyediakan identitas obat dan informasi formularium yang diperlukan dalam telaah resep. |
| Aplikasi Pelayanan Obat | Aplikasi | Mencatat hasil telaah serta membuat alokasi tagihan, pesanan penjualan apotek, dan perintah penyiapan obat yang dapat ditelusuri ke sumbernya. |
| Sistem Persediaan | Subsistem | Menyediakan informasi ketersediaan dan pemesanan stok tanpa mengambil alih keputusan profesional untuk menerima kebutuhan obat. |

## 3. Prasyarat

1. Staf Apotek atau Apoteker yang bertanggung jawab telah masuk ke `Apotek Rajal` dengan hak akses yang diperlukan.
2. Tersedia resep elektronik dari sumber yang sah, resep kertas yang telah dicatat, atau permintaan obat langsung yang diajukan kepada petugas berwenang.
3. Resep mencantumkan Pasien dan perintah klinis yang menjadi sumbernya.
4. Informasi Katalog Obat dan kebijakan penerimaan profesional yang berlaku tersedia.

## 4. Langkah Operasional

1. **Aplikasi Pelayanan Obat** menampilkan resep elektronik, resep kertas yang telah dicatat, atau permintaan obat langsung tanpa mensyaratkan kedatangan Pasien maupun koneksi dengan antrian.
2. Untuk resep, **Apoteker** memulai telaah resep dan memeriksa Pasien, sumber resep, obat, aturan pakai, jumlah, serta informasi klinis yang tersedia.
3. **Apoteker** mencatat satu keputusan untuk setiap item obat.
4. Jika diperlukan klarifikasi, **Apoteker** menghubungi Tenaga Medis Penulis Resep di luar sistem. Aplikasi tidak mencatat permintaan, jawaban, maupun status khusus untuk klarifikasi; telaah tetap berstatus `Under Review` sampai Apoteker mengambil keputusan.
5. **Apoteker** menetapkan keputusan akhir setiap item obat sebagai diterima sesuai resep, diterima dengan obat pengganti, atau ditolak. Resep dan item obat aslinya tidak diubah.
6. Jika obat pengganti dipilih, **Apoteker** mencatat obat pengganti, alasan, jumlah yang terdampak, dan Apoteker penanggung jawab pada item pesanan penjualan apotek. Item pesanan tersebut tetap mereferensikan item obat pada resep asli.
7. **Apoteker** menyelesaikan telaah resep dengan status `Approved`, `Partially Approved`, atau `Rejected`.
8. Untuk permintaan obat langsung, **Staf Apotek** mencatat rincian permintaan lalu menerima sesuai kewenangan, meneruskannya kepada **Apoteker**, atau menolaknya.
9. Jika permintaan diteruskan, **Apoteker** mencatat persetujuan atau penolakan. **Aplikasi Pelayanan Obat** hanya mengizinkan permintaan diterima setelah mendapat persetujuan.
10. Untuk resep yang disetujui seluruhnya/sebagian, atau permintaan obat langsung yang diterima, **Aplikasi Pelayanan Obat** membuat satu pesanan penjualan apotek dan mempertahankan ketertelusuran sumbernya.
11. **Aplikasi Pelayanan Obat** membuat serta menampilkan alokasi tagihan dan alokasi pelayanan obat secara terpisah.
12. Untuk pelayanan normal, **Aplikasi Pelayanan Obat** membuat satu perintah penyiapan obat rawat jalan utama yang aktif dan menampilkan status awalnya.
13. **Sistem Persediaan** dapat memberikan bukti pemesanan stok. **Aplikasi Pelayanan Obat** menampilkannya, tetapi bukti tersebut bukan izin untuk mulai menyiapkan obat.
14. Sesuai jalur sumbernya, **Apoteker** atau **Staf Apotek** memeriksa hasil akhir telaah, nomor pesanan penjualan apotek, item obat yang diterima, dan nomor perintah penyiapan obat.

## 5. Penanganan Kondisi Khusus

### 5.1 Tidak ada item obat yang diterima atau permintaan langsung ditolak

- **Aplikasi Pelayanan Obat** mencatat status `Rejected` untuk resep yang telah ditelaah. Permintaan obat langsung yang ditolak tidak dicatat sebagai kebutuhan obat yang diterima.
- **Aplikasi Pelayanan Obat** tidak membuat pesanan penjualan apotek.

### 5.2 Stok tidak cukup setelah kebutuhan obat diterima

- **Sistem Persediaan** menampilkan kekurangan atau selisih stok tanpa mengubah hasil telaah resep.
- **Staf Apotek** mencatat pesanan tertunda atau memilih sumber stok lain yang disetujui untuk obat yang sama sesuai kewenangan.
- **Staf Apotek** tidak berwenang mengganti obat.

### 5.3 Obat perlu diganti setelah pesanan penjualan dibuat

- **Aplikasi Pelayanan Obat** tidak mengizinkan perubahan identitas obat pada item pesanan yang sudah ada.
- **Apoteker** membatalkan item atau pesanan yang terdampak sesuai ketentuan, menelaah kembali resep asli, lalu membuat item pesanan baru. Resep asli tidak diubah dan resep perbaikan/pengganti tidak disyaratkan.

## 6. Kriteria Penyelesaian

1. Setiap item obat yang ditelaah mempunyai keputusan akhir. Telaah yang belum selesai tetap berstatus `Under Review`.
2. Sumber yang diterima mempunyai pesanan penjualan apotek, alokasi tagihan, alokasi pelayanan obat, dan perintah penyiapan obat utama yang dapat ditelusuri.
3. Resep berstatus `Rejected` atau permintaan obat langsung yang ditolak tidak mempunyai pesanan penjualan apotek.
4. Informasi stok tidak mengubah keputusan profesional atas penerimaan kebutuhan obat.

## 7. Referensi

- [Domain Pelayanan Obat](../medication-fulfillment-domain-id.md), khususnya `BR-MF-001`–`BR-MF-019`, `BR-MF-029`–`BR-MF-034`, `BR-MF-050`, `BR-MF-061`, `BR-MF-068`, `BR-MF-083`, `BR-MF-086`, dan `BR-MF-089`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-002`.
- [Domain CPOE](../../../contexts/cpoe/CPOE-DOMAIN-ID.md).
