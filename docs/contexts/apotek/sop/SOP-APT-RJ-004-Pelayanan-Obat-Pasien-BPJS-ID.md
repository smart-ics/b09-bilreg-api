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
| Kepala Apotek | Petugas | Menyetujui penanganan manual untuk obat yang sudah disiapkan tetapi tidak diambil Pasien. Otorisasi oleh Apoteker yang berwenang menurut kebijakan operasional; tidak ada ambang persetujuan berdasarkan nilai uang. |
| Apoteker | Petugas | Memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. Pemeriksaan penerima tidak ditegakkan sistem. Catatan konseling rinci bersifat opsional. |
| Sistem SEP dan Fornas | Subsistem | Menyediakan hasil pemeriksaan keabsahan SEP pada kunjungan Pasien dan jaminan Fornas untuk setiap item obat. |
| Sistem Apotek | Subsistem | Mencatat persetujuan jaminan BPJS dan izin penyiapan obat, memantau penyiapan, serta mencatat faktur BPJS dan penyerahan obat dalam satu hasil transaksi. |
| Sistem Antrian Pasien | Subsistem | Mencatat `ServedAt` saat penyiapan obat dimulai dan `DoneAt` ketika penyelesaian antrean terjadi (panggilan pengambilan, atau penyelesaian No Show jika Queue Entry masih `In Service`). `DoneAt` tidak pernah dibalik. |
| Sistem Persediaan | Subsistem | Menyediakan hasil Mutasi, Remove Stock, dan keputusan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima hasil pembebanan biaya kepada BPJS. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Outpatient Queue Mapping, pesanan apotek yang aktif, Sales Order Item yang ditanggung BPJS, dan tugas untuk menyiapkan obat telah ditampilkan.
3. SEP untuk kunjungan Pasien masih sah, dan pemetaan Fornas menyatakan setiap jumlah obat yang ditanggung dapat dilayani.
4. Jumlah yang harus dibayar Pasien adalah nol, status pembayaran `Not Required`, dan faktur BPJS belum dibuat.

## 4. Langkah Operasional

1. **Staf Apotek** membuka permintaan obat BPJS yang sudah terkonek dengan antrian pada `Apotek Rajal`. **Staf Apotek** memeriksa Pasien, nomor rujukan SEP, jumlah Sales Order Item yang ditanggung BPJS, dan jumlah obat pada tugas penyiapan.
2. **Sistem SEP dan Fornas** mengirimkan hasil pemeriksaan keabsahan SEP dan jaminan untuk setiap item obat.
3. **Sistem Apotek** menampilkan bahwa setiap jumlah obat yang dijamin sudah disetujui BPJS. Untuk jumlah tersebut, aplikasi menetapkan bahwa obat boleh masuk ke proses penyiapan tanpa menunggu faktur penjualan dibuat.
4. Bila Pharmacy Reserve belum ada di Dispensing Temporary Unit, **Stock Ledger** mencatat Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. **Sistem Apotek** menampilkan hasil Mutasi tersebut.
5. **Staf Apotek** mulai menyiapkan obat hanya setelah tugas penyiapan obat berstatus `Released`.
6. **Sistem Apotek** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat `ServedAt`.
7. **Staf Apotek** menyelesaikan penyiapan atau peracikan obat dan mencatatnya. **Sistem Apotek** menampilkan tugas penyiapan obat dengan status `Prepared`.
8. **Staf Apotek** memastikan setiap tugas penyiapan obat yang akan diserahkan sudah berstatus `Prepared` atau sudah memiliki catatan alasan yang jelas bila obat tidak dapat diserahkan.
9. **Staf Apotek** melakukan satu kali panggilan agar Pasien mengambil obat. **Sistem Antrian Pasien** mengubah antrian menjadi `Done` dan mencatat `DoneAt`.
10. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. Bila pemeriksaan lulus, **Sistem Apotek** menambahkan catatan pemeriksaan dan menampilkan tugas penyiapan obat berstatus `Reviewed`. **Sistem Apotek** mencatat waktu edukasi dan Apoteker penanggung jawab. Catatan konseling rinci bersifat opsional. Apoteker boleh secara opsional mencatat nomor telepon penerima dan hubungan dengan Pasien sebagai referensi.
11. **Sistem Apotek** tidak mengizinkan proses diselesaikan bila pemeriksaan akhir obat belum lengkap atau Patient Education Acknowledgement belum dicatat. Pemeriksaan penerima bukan gate sistem. Catatan konseling rinci tidak diwajibkan. Jika Pickup Expired, **Sistem Apotek** juga tidak mengizinkan penyelesaian sampai Collection Window Override beserta alasan dicatat.
12. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik.
13. Dalam satu hasil transaksi, **Sistem Apotek** membuat faktur BPJS beserta Invoice Item-nya dari jumlah Sales Order Item yang dijamin, mencatat obat yang diberikan, dan mencatat penyerahan obat.
14. **Sistem Persediaan** mencatat Remove Stock dari Dispensing Temporary Unit. **Sistem Apotek** menampilkan tugas penyiapan obat berstatus `Completed` setelah catatan tersebut tersedia.
15. **Sistem Apotek** menampilkan pesanan apotek berstatus `Resolved` hanya setelah seluruh obat yang diterima dan seluruh urusan keuangannya selesai.

## 5. Pengecualian Operasional

### 5.1 SEP tidak sah atau obat tidak dijamin

- **Sistem SEP dan Fornas** tidak memberikan persetujuan jaminan untuk jumlah obat yang terdampak.
- **Sistem Apotek** tetap mencegah obat tersebut masuk ke proses penyiapan.
- Bila ada jumlah obat yang tidak dijamin, **Staf Apotek** melanjutkannya melalui `SOP-APT-RJ-005` bila sesuai.

### 5.2 Stok kurang setelah pesanan apotek dibuat

Pengecualian ini berlaku ketika kekurangan stok diketahui **setelah** Sales Order dibentuk (`BR-APT-118`). Kekurangan stok **sebelum** Sales Order dibentuk adalah `SOP-APT-RJ-002` pengecualian 5.2.

- **Staf Apotek** tidak membuat pesanan tertunda, tidak memilih sumber stok alternatif, dan tidak mengganti obat.
- **Staf Apotek** tidak menghapus item dari Sales Order yang sudah dibentuk dan tidak menyusun ulang Sales Order menjadi pesanan parsial.
- **Sistem Apotek** mencatat Unfulfilled Medication Outcome, mendukung Copy Resep untuk item yang tidak dipenuhi, dan tetap menggunakan identitas obat yang sudah diterima.
- Jika faktur BPJS sudah ada, konsekuensi komersial mengikuti `BR-APT-027`: **Sistem Apotek** merevisi faktur ketika Tata Rekening masih mengizinkan perubahan; bila tidak, **Tata Rekening** memberikan nota kredit, pengembalian dana, atau koreksi komersial pengecualian.

### 5.3 Pemeriksaan akhir obat tidak lulus

- **Apoteker** mencatat alasan kegagalan dan jumlah obat yang terdampak serta tidak menyetujui penyerahan.
- **Sistem Apotek** menambahkan catatan pemeriksaan yang tidak dapat diubah, lengkap dengan Apoteker dan waktu keputusan, mengembalikan tugas penyiapan obat dari `Prepared` ke `Preparing`, serta tidak membuat faktur BPJS maupun catatan penyerahan obat.
- **Staf Apotek** memperbaiki dan menyiapkan kembali obat yang terdampak. **Sistem Apotek** mengembalikan tugas tersebut ke `Prepared`, lalu **Apoteker** melakukan pemeriksaan akhir obat yang baru. Catatan pemeriksaan sebelumnya tetap terlihat dan tidak berubah.

### 5.4 Pasien tidak mengambil obat

- **Kepala Apotek** menerapkan `SOP-APT-RJ-007`. Jika antrian sudah `Done`, catatan `DoneAt` tidak dihapus atau diubah kembali. Jika antrian masih `In Service` karena pickup call belum terjadi, resolusi itu boleh menyelesaikannya menjadi `Done` dan mencatat `DoneAt`.
- **Sistem Apotek** tidak membuat atau membatalkan faktur BPJS untuk Pasien yang tidak datang mengambil obat.

## 6. Kriteria Penyelesaian

1. Jumlah yang harus dibayar Pasien adalah nol dan status pembayaran `Not Required`.
2. Faktur BPJS dan catatan penyerahan obat tampil sebagai satu hasil transaksi yang berhasil.
3. Tugas penyiapan obat berstatus `Completed`, catatan penyerahan mencantumkan waktu penyerahan, dan Remove Stock dari Dispensing Temporary Unit ditampilkan. Nomor telepon penerima dan hubungan dengan Pasien boleh dicatat secara opsional sebagai referensi.
4. Pesanan apotek berstatus `Resolved`, atau tetap `Active` dengan masalah yang belum selesai ditampilkan dengan jelas.
5. Status antrian `Done` bukan bukti bahwa obat sudah diserahkan.
6. Kekurangan stok setelah Sales Order dibentuk tidak mengubah Sales Order itu; jumlah yang tidak dapat dipenuhi memiliki Unfulfilled Medication Outcome dan koreksi keuangan bila diperlukan, bukan item yang dihapus dari Sales Order.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-020`–`BR-APT-026`, `BR-APT-029`–`BR-APT-045`, `BR-APT-066`, `BR-APT-068`–`BR-APT-069`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, dan `BR-APT-129`–`BR-APT-134`, dan `BR-APT-138`–`BR-APT-142`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-004`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), `BR-TRK-045`, `BR-TRK-045a`, dan `BR-TRK-046`.
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
