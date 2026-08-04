# SOP MF-RJ-007 — Menangani Obat Rawat Jalan yang Tidak Diambil

**Status dokumen:** Spesifikasi operasional acuan

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-MF-RJ-007`

**Dokumen acuan bahasa Inggris:** [SOP MF-RJ-007 — Resolve Uncollected Outpatient Medication](./SOP-MF-RJ-007-Resolve-Uncollected-Outpatient-Medication.md)

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara menutup kesempatan pengambilan obat secara manual dan berwenang, menentukan penyelesaian stok, serta menyelesaikan akibat keuangan menurut penanggung biaya untuk obat yang telah disiapkan atau sedang dikirim tetapi tidak diambil Pasien.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Penanggung Jawab Apotek | Petugas | Menyetujui dan mencatat berakhirnya kesempatan pengambilan obat. |
| Staf Apotek | Petugas | Mengidentifikasi kebutuhan dan jumlah obat yang terdampak serta memeriksa hasil akhir penanganannya. |
| Aplikasi Pelayanan Obat | Aplikasi | Mencatat Pasien tidak datang, status `Expired`, alasan `Collection Window Expired`, obat yang tidak dapat dilayani, dan perkembangan akhir pesanan penjualan. |
| Sistem Persediaan | Subsistem | Menentukan apakah obat dapat dikembalikan ke stok dan mencatat pengembalian atau penyelesaian stok lainnya. |
| Tata Rekening | Subsistem | Menerbitkan nota kredit, pengembalian dana, atau penyelesaian komersial lain untuk obat yang telah dibayar. |
| Sistem Antrian Pasien | Subsistem | Mempertahankan riwayat antrian yang sudah ada dan tidak menghapus atau membalik `DoneAt`. |

## 3. Prasyarat

1. Penanggung Jawab Apotek dan Staf Apotek telah masuk ke aplikasi dengan hak untuk menangani kondisi khusus.
2. Obat berstatus `Prepared` atau `In-Transit`, dan belum ada catatan penyerahan obat.
3. Pasien tidak mengambil obat.
4. Petugas yang berwenang menutup proses, jumlah yang terdampak, alasan, dan waktu efektif kegiatan diketahui.
5. Keberadaan faktur serta penyelesaian keuangan setiap alokasi penanggung biaya dapat dilihat.

## 4. Langkah Operasional

1. **Staf Apotek** membuka antrian yang obatnya tidak diambil, lalu memeriksa pesanan penjualan sumber, perintah penyiapan, jumlah terdampak, alokasi penanggung biaya, ketiadaan penyerahan, dan kondisi stok saat ini.
2. **Penanggung Jawab Apotek** memastikan kesempatan pengambilan yang diizinkan telah berakhir. Sistem tidak boleh memakai batas waktu otomatis atau angka rekaan.
3. **Penanggung Jawab Apotek** mencatat penutupan kesempatan pengambilan beserta petugas penanggung jawab, waktu efektif kegiatan, jumlah terdampak, dan alasan `Collection Window Expired`.
4. **Aplikasi Pelayanan Obat** mencatat bahwa Pasien tidak datang untuk mengambil obat dan mengubah setiap perintah penyiapan yang terdampak menjadi `Expired`.
5. **Aplikasi Pelayanan Obat** mencatat setiap jumlah obat yang tidak dapat dilayani dan mempertahankan ketertelusuran sumbernya.
6. **Sistem Persediaan** menilai obat yang telah dipesan atau sedang dikirim. Obat hanya dikembalikan ke stok bila memenuhi ketentuan; bila tidak, Sistem Persediaan mencatat penyelesaian stok lainnya.
7. **Aplikasi Pelayanan Obat** menampilkan hasil resmi dari Sistem Persediaan dan tidak menyimpulkan sendiri bahwa mutasi stok telah terjadi.
8. Untuk alokasi BPJS yang belum difakturkan, **Aplikasi Pelayanan Obat** tidak membuat faktur BPJS dan hanya menyelesaikan urusan pelayanan serta persediaan.
9. Untuk alokasi pasien umum yang telah dibayar, **Aplikasi Pelayanan Obat** mengirimkan akibat keuangan kepada **Tata Rekening** dan mempertahankan pesanan penjualan sebagai `Active`.
10. **Tata Rekening** menerbitkan nota kredit, pengembalian dana, atau penyelesaian komersial lain yang dapat dipertanggungjawabkan. **Aplikasi Pelayanan Obat** menampilkan hasilnya pada alokasi asal.
11. Untuk penjaminan campuran, **Aplikasi Pelayanan Obat** mencatat akibat pada bagian BPJS yang belum difakturkan dan bagian yang telah dibayar Pasien secara terpisah.
12. **Sistem Antrian Pasien** mempertahankan status antrian dan `DoneAt` yang sudah ada. Penanganan Pasien yang tidak datang tidak membuka kembali antrian.
13. **Aplikasi Pelayanan Obat** baru mengubah pesanan penjualan menjadi `Resolved` setelah seluruh jumlah obat, penyelesaian stok, dan akibat komersial telah selesai.
14. **Staf Apotek** memeriksa keadaan akhir atau memastikan sisa urusan keuangan yang belum selesai ditampilkan secara jelas.

## 5. Penanganan Kondisi Khusus

### 5.1 Obat tidak dapat dikembalikan ke stok

- **Sistem Persediaan** mencatat alasan penolakan dan penyelesaian stok lainnya yang dapat dipertanggungjawabkan.
- **Aplikasi Pelayanan Obat** mempertahankan pesanan penjualan sebagai belum selesai sampai penyelesaian stok tersebut ditampilkan.

### 5.2 Akibat keuangan untuk obat yang telah dibayar belum selesai

- **Aplikasi Pelayanan Obat** menampilkan perintah penyiapan sebagai `Expired` dan pesanan penjualan sebagai `Active`.
- **Staf Apotek** tidak boleh menghapus kewajiban tersebut atau menggolongkannya sebagai transaksi BPJS yang belum difakturkan.
- **Tata Rekening** menyelesaikan koreksi keuangan yang diperlukan.

### 5.3 Antrian sudah berstatus `Done`

- **Sistem Antrian Pasien** mempertahankan `DoneAt` tanpa perubahan.
- **Penanggung Jawab Apotek** melanjutkan penanganan obat tanpa membuka atau menyelesaikan ulang antrian.

## 6. Kriteria Penyelesaian

1. Setiap perintah penyiapan yang terdampak berstatus `Expired` dengan alasan `Collection Window Expired`, petugas penanggung jawab, waktu efektif kegiatan, dan jumlah terdampak.
2. Setiap jumlah yang terdampak mempunyai catatan obat yang tidak dapat dilayani dan penyelesaian stok resmi.
3. Faktur BPJS tidak dibuat bila penyerahan obat BPJS tidak terjadi.
4. Pesanan penjualan pasien umum yang telah dibayar tetap `Active` sampai nota kredit, pengembalian dana, atau penyelesaian komersial lainnya terlihat.
5. Pesanan penjualan baru menjadi `Resolved` setelah seluruh urusan pelayanan dan keuangan selesai.
6. Catatan `DoneAt` pada antrian tidak berubah.

## 7. Referensi

- [Domain Pelayanan Obat](../medication-fulfillment-domain-id.md), khususnya `BR-MF-018`–`BR-MF-019`, `BR-MF-027`, `BR-MF-045`–`BR-MF-047`, `BR-MF-052`–`BR-MF-060`, `BR-MF-069`, `BR-MF-078`–`BR-MF-080`, dan `BR-MF-095`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-007`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
