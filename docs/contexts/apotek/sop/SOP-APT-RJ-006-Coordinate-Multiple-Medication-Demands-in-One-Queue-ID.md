# SOP APT-RJ-006 — Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrian

**Status dokumen:** Spesifikasi operasional acuan

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-006`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-006 — Coordinate Multiple Medication Demands in One Queue](./SOP-APT-RJ-006-Coordinate-Multiple-Medication-Demands-in-One-Queue.md)

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas untuk melayani dua atau lebih resep atau permintaan obat dengan satu nomor antrian dan satu kali pengambilan. Catatan setiap resep atau permintaan tetap terpisah; catatan tersebut tidak digabungkan hanya karena obat diambil pada waktu yang sama.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyelesaikan urusan pembayaran atau penjaminan yang berlaku, datang untuk satu kali pengambilan, menerima edukasi gabungan, dan menerima obat yang dapat diserahkan. |
| Staf Apotek | Petugas | Memeriksa mapping dan keadaan setiap resep atau permintaan obat, menyiapkan setiap tugas penyiapan obat secara terpisah, mengoordinasikan kesiapan menurut penanggung biaya, menjelaskan masalah yang terjadi, dan melakukan satu kali panggilan pengambilan obat. |
| Apoteker | Petugas | Memeriksa penerima yang berhak, memeriksa setiap obat yang sudah disiapkan, dan memberikan edukasi gabungan dengan aturan pakai masing-masing obat. |
| Sistem Apotek | Subsistem | Menampilkan keadaan setiap resep atau permintaan obat serta mencatat tagihan, faktur, tugas penyiapan, obat yang diberikan, dan penyerahan obat secara terpisah. |
| Sistem Antrian Pasien | Subsistem | Menyimpan satu entri antrian dengan satu `CreatedAt`, paling banyak satu `ServedAt`, dan satu `DoneAt`. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Mengirimkan informasi pelunasan untuk obat yang harus dibayar Pasien. |
| Sistem SEP dan Fornas | Subsistem | Mengirimkan bukti jaminan untuk obat BPJS yang berlaku. |
| Sistem Persediaan | Subsistem | Menyediakan hasil pemesanan, pengeluaran, dan keputusan stok untuk setiap tugas penyiapan obat. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Satu entri antrian sudah terkonek secara terpisah ke sedikitnya dua resep atau permintaan obat.
3. Setiap resep sudah memiliki telaah resep masing-masing.
4. Setiap sumber yang diterima sudah memiliki pesanan apotek dan tugas utama untuk menyiapkan obat rawat jalan yang aktif masing-masing.

## 4. Langkah Operasional

1. **Staf Apotek** membuka entri antrian bersama pada `Apotek Rajal`.
2. **Sistem Apotek** menampilkan setiap resep atau permintaan obat secara terpisah, termasuk sumbernya, pesanan apotek, penanggung biaya, bagian obat yang menjadi tagihan, bagian obat yang sudah boleh diproses, faktur, status persetujuan atau pelunasan, dan keadaan tugas penyiapan obat.
3. **Staf Apotek** memastikan setiap resep atau permintaan obat tetap memiliki pesanan apotek, faktur, dan tugas penyiapan obatnya sendiri. Tidak ada catatan yang digabungkan dengan catatan sumber lain.
4. **Staf Apotek** menerapkan SOP Pasien Umum, SOP BPJS, atau SOP penjaminan campuran untuk setiap resep atau permintaan obat sesuai penanggung biayanya.
5. **Kasir atau Sistem Pembayaran** mengirimkan informasi pelunasan yang diperlukan. **Sistem SEP dan Fornas** mengirimkan persetujuan jaminan yang diperlukan.
6. **Staf Apotek** menyiapkan setiap tugas penyiapan obat yang sudah berstatus `Released` secara terpisah dan mencatat penyelesaian masing-masing.
7. Saat penyiapan obat pertama dimulai, **Sistem Apotek** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mencatat satu `ServedAt` dan mengubah antrian bersama menjadi `In Service`.
8. **Sistem Apotek** menampilkan setiap resep atau permintaan obat yang akan diambil sebagai `Prepared`, atau menampilkan alasan yang jelas bila obat belum dapat diserahkan.
9. **Staf Apotek** memeriksa keadaan semua resep atau permintaan obat dan tidak menyatakan obat yang masih bermasalah sebagai siap diambil.
10. Setelah seluruh obat yang akan diambil sudah siap atau masalahnya sudah ditangani dengan jelas, **Staf Apotek** melakukan satu kali panggilan agar Pasien mengambil obat.
11. **Sistem Antrian Pasien** mencatat satu `DoneAt` dan mengubah antrian bersama menjadi `Done`.
12. **Staf Apotek** menjelaskan kepada Pasien obat yang sudah siap sekaligus masalah atau tindak lanjut yang berlaku untuk obat yang belum dapat diserahkan.
13. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima yang berhak, melakukan pemeriksaan akhir pada setiap obat yang sudah disiapkan, dan mencatat edukasi gabungan beserta aturan pakai untuk masing-masing obat.
14. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik.
15. **Sistem Apotek** mencatat obat yang diberikan dan diserahkan pada setiap tugas penyiapan obat dan pesanan apotek secara terpisah.
16. **Sistem Persediaan** mengirimkan catatan pengeluaran stok atau keputusan stok lain secara terpisah untuk setiap tugas penyiapan obat asalnya.

## 5. Pengecualian Operasional

### 5.1 Salah satu resep atau permintaan obat belum selesai

- **Sistem Apotek** menampilkan resep atau permintaan tersebut sebagai belum siap.
- **Staf Apotek** menunda panggilan pengambilan bersama, kecuali SOP menurut penanggung biaya mengizinkan penyerahan sebagian dan Pasien sudah menyetujuinya.

### 5.2 Salah satu resep atau permintaan obat memiliki masalah yang sudah ditangani

- **Staf Apotek** menjelaskan masalah dan hasil penanganannya saat memberi informasi pengambilan. Obat lain yang sudah siap hanya dapat dilanjutkan bila SOP menurut penanggung biaya mengizinkannya.
- **Sistem Apotek** menyimpan catatan masalah tersebut pada pesanan apotek dan tugas penyiapan obat asalnya.

### 5.3 mapping antrean atau sumber pelayanan obat perlu diperbaiki

- **Staf Apotek** memperbaiki hanya mapping atau sumber pelayanan obat yang terdampak.
- **Sistem Apotek** tidak mengubah catatan resep atau permintaan obat lain. Satu catatan penyerahan obat tidak boleh dianggap menyelesaikan resep atau permintaan obat lain.

## 6. Kriteria Penyelesaian

1. Entri antrian bersama menampilkan satu `CreatedAt`, paling banyak satu `ServedAt`, dan satu `DoneAt`.
2. Setiap resep, pesanan apotek, faktur, dan tugas penyiapan obat tetap memiliki identitas serta keadaan masing-masing.
3. Hanya ada satu panggilan pengambilan obat, tetapi setiap resep atau permintaan obat memiliki catatan penyerahan obat sendiri atau alasan yang jelas bila belum dapat diserahkan.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, dan `BR-APT-095`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-006`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), khususnya `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, dan `BR-TRK-045a`.
