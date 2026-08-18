# SOP APT-RJ-007 — Menangani Obat Rawat Jalan yang Tidak Diambil

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-007`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-007 — Resolve Uncollected Outpatient Medication](./SOP-APT-RJ-007-Penanganan-Obat-Tidak-Diambil-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah manual bagi petugas untuk menangani obat `Prepared` dalam Dispensing Temporary Custody yang tidak diambil Pasien. Kepala Apotek menetapkan bahwa kesempatan pengambilan telah berakhir, lalu Mutasi pengembalian stok dan urusan keuangan ditangani menurut penanggung biayanya.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Kepala Apotek | Petugas | Menyetujui dan mencatat berakhirnya kesempatan Pasien untuk mengambil obat. Ini adalah Apoteker yang berwenang menurut kebijakan operasional. Tidak ada ambang persetujuan berdasarkan nilai uang. |
| Staf Apotek | Petugas | Menentukan resep atau permintaan obat serta jumlah obat yang terdampak, lalu memeriksa hasil akhir penanganannya. |
| Sistem Apotek | Subsistem | Mencatat Pasien tidak datang mengambil obat, status `Expired`, alasan `Collection Window Expired`, obat yang tidak dapat diserahkan, dan keadaan akhir pesanan apotek. |
| Sistem Persediaan | Subsistem | Menerapkan Stock Mutasi dari Dispensing Temporary Unit kembali ke Pharmacy Unit atas arahan Pharmacy. Tidak menyimpan status No Show. |
| Tata Rekening | Subsistem | Memberikan izin finansial untuk merevisi faktur, atau nota kredit, pengembalian dana, atau penyelesaian keuangan pengecualian ketika revisi tidak lagi diizinkan. |
| Sistem Antrian Pasien | Subsistem | Mencatat `DoneAt` dan `Done` ketika penyelesaian No Show menyelesaikan Queue Entry yang masih `In Service`. Mempertahankan `DoneAt` yang sudah ada ketika Queue Entry sudah `Done`. Tidak pernah membalik `DoneAt`. |

## 3. Prasyarat

1. Kepala Apotek dan Staf Apotek telah masuk ke aplikasi dengan hak akses untuk menangani kondisi khusus.
2. Obat berstatus `Prepared` dalam Dispensing Temporary Custody, dan belum ada catatan penyerahan obat.
3. Pasien tidak mengambil obat.
4. Petugas yang berwenang menutup kesempatan pengambilan, jumlah obat yang terdampak, alasan, dan waktu efektif keputusan telah diketahui.
5. Petugas dapat melihat apakah ada faktur dan bagaimana keadaan keuangan untuk setiap penanggung biaya.

## 4. Langkah Operasional

1. **Staf Apotek** membuka entri antrian untuk obat yang tidak diambil. **Staf Apotek** memeriksa pesanan apotek asal, tugas penyiapan obat, jumlah obat yang terdampak, penanggung biaya, ketiadaan catatan penyerahan, dan keadaan stok saat ini.
2. **Kepala Apotek** memastikan kesempatan pengambilan obat yang diizinkan memang sudah berakhir. Collection Window (default 7 hari) mengklasifikasikan Ready for Pickup menjadi Pickup Expired bila habis; klasifikasi itu tidak meng-expire Dispensing. Tidak ada ambang persetujuan berdasarkan nilai uang.
3. **Kepala Apotek** mencatat keputusan penanganan obat yang tidak diambil, termasuk petugas penanggung jawab, waktu keputusan mulai berlaku, jumlah obat yang terdampak, dan alasan `Collection Window Expired`.
4. **Sistem Apotek** mencatat bahwa Pasien tidak datang mengambil obat dan mengubah setiap tugas penyiapan obat yang terdampak menjadi `Expired`.
5. **Sistem Apotek** mencatat setiap jumlah obat yang tidak dapat diserahkan dan menyimpan hubungannya dengan resep atau permintaan obat asal.
6. **Sistem Apotek** meminta Stock Mutasi dari Dispensing Temporary Unit kembali ke Pharmacy Unit untuk jumlah yang eligible. **Sistem Persediaan** hanya menerapkan pergerakan pengembalian yang diarahkan Pharmacy dan tidak menyimpan status No Show.
7. **Sistem Apotek** menampilkan catatan resmi dari Sistem Persediaan. Aplikasi tidak menyimpulkan sendiri bahwa stok sudah berpindah.
8. Untuk obat BPJS yang belum difakturkan, **Sistem Apotek** tetap tidak membuat faktur BPJS dan hanya menyelesaikan penyiapan obat serta urusan stoknya.
9. Untuk obat Pasien Umum yang sudah dibayar, **Sistem Apotek** mempertahankan pesanan apotek berstatus `Active` dan menerapkan `BR-APT-027`: merevisi faktur ketika Tata Rekening masih mengizinkan perubahan; bila tidak, mengirimkan urusan keuangan yang harus diselesaikan kepada **Tata Rekening**.
10. Ketika revisi faktur tidak lagi diizinkan, **Tata Rekening** memberikan nota kredit, pengembalian dana, atau hasil keuangan pengecualian. **Sistem Apotek** menampilkan hasil tersebut pada bagian obat asalnya.
11. Bila penjaminannya campuran, **Sistem Apotek** mencatat secara terpisah bagian BPJS yang belum difakturkan dan bagian Pasien yang sudah dibayar.
12. **Sistem Antrian Pasien** menyelesaikan atau mempertahankan Queue Entry sebagai berikut:
    - Jika Queue Entry masih `In Service` karena pickup call belum terjadi, **Sistem Antrian Pasien** memindahkannya ke `Done` dan mencatat `DoneAt`. Ini penyelesaian antrean, bukan Pharmacy Queue Close, dan tidak berarti Medication Handover.
    - Jika Queue Entry sudah `Done`, **Sistem Antrian Pasien** mempertahankan `DoneAt`.
    - `DoneAt` tidak pernah dibalik. Status antrean baru tidak diperkenalkan. State alur kerja farmasi tidak ditambahkan ke Queue Entry.
13. **Sistem Apotek** baru mengubah pesanan apotek menjadi `Resolved` setelah seluruh jumlah obat, keputusan stok, dan urusan keuangan yang diperlukan selesai.
14. **Staf Apotek** memeriksa keadaan akhir atau memastikan urusan keuangan yang masih belum selesai ditampilkan dengan jelas.

## 5. Pengecualian Operasional

### 5.1 Sistem Persediaan menolak pengembalian obat ke stok

- **Sistem Persediaan** memberikan catatan penolakan dan keputusan stok akhir lainnya.
- **Sistem Apotek** mempertahankan pesanan apotek sebagai belum selesai sampai keputusan stok tersebut ditampilkan.

### 5.2 Urusan keuangan obat yang telah dibayar belum selesai

- **Sistem Apotek** menampilkan tugas penyiapan obat sebagai `Expired` dan pesanan apotek sebagai `Active`.
- **Staf Apotek** tidak menghapus atau mengubah urusan keuangan tersebut menjadi obat BPJS yang belum difakturkan.
- Penyelesaian komersial mengikuti `BR-APT-027`. **Tata Rekening** menyelesaikan koreksi keuangan yang diperlukan ketika revisi faktur tidak lagi diizinkan.

### 5.3 Antrian sudah berstatus `Done`

- **Sistem Antrian Pasien** mempertahankan `DoneAt` tanpa perubahan.
- **Kepala Apotek** melanjutkan penanganan obat tanpa membuka kembali antrian.

### 5.4 Antrian masih `In Service` karena pickup call belum terjadi

- **Sistem Antrian Pasien** boleh menyelesaikan Queue Entry: `In Service` → `Done`, dan mencatat `DoneAt`.
- Penyelesaian itu bukan Pharmacy Queue Close dan tidak membuktikan Medication Handover.
- **Kepala Apotek** melanjutkan penanganan obat pada lifecycle yang sama `Waiting` → `In Service` → `Done`. Status antrean baru tidak diperkenalkan.

## 6. Kriteria Penyelesaian

1. Setiap tugas penyiapan obat yang terdampak berstatus `Expired` dengan alasan `Collection Window Expired`, petugas penanggung jawab, waktu keputusan mulai berlaku, dan jumlah obat yang terdampak.
2. Setiap jumlah obat yang terdampak memiliki catatan bahwa obat tidak dapat diserahkan dan catatan resmi keputusan stok.
3. Faktur BPJS tidak dibuat bila penyerahan obat BPJS tidak terjadi.
4. Pesanan apotek Pasien Umum yang sudah dibayar tetap `Active` sampai konsekuensi komersial diselesaikan menurut `BR-APT-027`.
5. Pesanan apotek baru menjadi `Resolved` setelah seluruh urusan obat dan keuangan selesai.
6. Queue Entry terkait berstatus `Done`. `DoneAt` dicatat pada pickup call atau pada penyelesaian No Show ini dan tidak dibalik.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-069`, `BR-APT-078`–`BR-APT-080`, `BR-APT-095`, dan `BR-APT-135`–`BR-APT-142`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-007`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
