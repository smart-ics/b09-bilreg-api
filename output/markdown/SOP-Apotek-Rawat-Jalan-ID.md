# SOP Apotek Rawat Jalan

Kompilasi tujuh Standard Operating Procedure versi Bahasa Indonesia, dihasilkan ulang dari sumber `docs/contexts/apotek/sop/*-ID.md`.

## Daftar Isi

- [Menerbitkan Nomor Antrian dan Melakukan Mapping dengan Resep Kerja atau Jual Bebas](#menerbitkan-nomor-antrian-dan-melakukan-mapping-dengan-resep-kerja-atau-jual-bebas)
- [Menerima Permintaan Obat Pasien Rawat Jalan](#menerima-permintaan-obat-pasien-rawat-jalan)
- [Melayani Obat untuk Pasien Umum](#melayani-obat-untuk-pasien-umum)
- [Melayani Obat untuk Pasien BPJS](#melayani-obat-untuk-pasien-bpjs)
- [Melayani Obat dengan Penjaminan Campuran](#melayani-obat-dengan-penjaminan-campuran)
- [Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrian](#mengoordinasikan-beberapa-permintaan-obat-dalam-satu-antrian)
- [Menangani Obat Rawat Jalan yang Tidak Diambil](#menangani-obat-rawat-jalan-yang-tidak-diambil)

---

# SOP APT-RJ-001 — Menerbitkan Nomor Antrian dan Melakukan Mapping dengan Resep Kerja atau Jual Bebas

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-001`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-001 — Acquire and Map Outpatient Pharmacy Queue](./SOP-APT-RJ-001-Antrian-dan-Mapping-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara penerbitan nomor antrian apotek rawat jalan dan melakukan mappingnya dengan Resep Kerja atau Jual Bebas yang sesuai, baik secara otomatis melalui sistem maupun secara manual oleh petugas. Mapping tidak menunjuk Sales Order, Invoice, atau Dispensing.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Mengambil nomor antrian melalui kiosk serta memindai bukti pelacakan pasien atau registrasi yang tersedia. |
| Staf Apotek | Petugas | Menemukan antrian yang belum di-mapping, memeriksa bukti, mencatat resep kertas bila ada, dan melakukan Manual Mapping. |
| Sistem Apotek | Subsistem | Mencoba melakukan Tracker Mapping antrean dengan Resep yang sudah ada, mencatat mapping pada Resep Kerja yang sesuai, dan menampilkan perkembangan setiap sumber permintaan. |
| Sistem Antrian Pasien | Subsistem | Membuat entri antrian apotek, menerbitkan nomor antrian, mencatat `CreatedAt`, dan mengelola siklus antrian. |

## 3. Prasyarat

1. Staf Apotek telah masuk ke ruang kerja `Apotek Rajal` dan memiliki hak untuk melakukan mapping antrean.
2. Sesi antrian apotek rawat jalan tersedia.
3. Pasien mengambil nomor antrian melalui kiosk atau memindai bukti pelacakan pasien maupun registrasi yang sah.
4. Setiap Resep Kerja atau Jual Bebas yang akan di-mapping sudah ada atau dicatat saat Manual Mapping.

## 4. Langkah Operasional

1. **Pasien atau Keluarga Pasien** mengambil nomor antrian apotek rawat jalan melalui kiosk atau memindai bukti pelacakan pasien maupun registrasi yang sah.
2. **Sistem Antrian Pasien** membuat entri antrian, menerbitkan nomor antrian, mencatat `CreatedAt`, lalu menampilkan atau mengirimkan nomor tersebut.
3. **Sistem Apotek** mencoba melakukan mapping antrean secara otomatis dengan satu atau beberapa Resep yang sudah ada berdasarkan bukti tracker atau registrasi yang diberikan. Proses ini tidak membuat Resep baru dan tidak berlaku untuk Jual Bebas.
4. Untuk setiap Resep yang ditemukan, **Sistem Apotek** membuat catatan mapping antrean secara terpisah pada Resep Kerja yang sesuai dan menampilkan perkembangan masing-masing Resep Kerja.
5. Jika antrian masih berstatus `Unmapped`, **Staf Apotek** dapat memanggil nomor antrian untuk keperluan administrasi. Pemanggilan ini tidak boleh mencatat `ServedAt` ataupun `DoneAt`.
6. **Pasien atau Keluarga Pasien** menunjukkan nomor antrian beserta resep, bukti registrasi, atau Jual Bebas yang tersedia.
7. **Staf Apotek** memilih antrian yang belum termapping dan mencari Resep Kerja atau Jual Bebas yang sesuai.
8. Jika pasien membawa resep kertas, **Staf Apotek** mencatat resep tersebut sebagai Resep Kerja sebelum meminta telaah resep. Jika pasien mengajukan Jual Bebas, **Staf Apotek** menilainya menurut prosedur penerimaan permintaan obat.
9. **Staf Apotek** melakukan mapping antrean secara manual dengan setiap Resep Kerja atau Jual Bebas yang berhasil diidentifikasi.
10. **Sistem Apotek** menampilkan mapping yang berhasil dengan status `Mapped` dan tetap menyimpan setiap sumber pelayanan obat sebagai catatan tersendiri.
11. **Staf Apotek** memastikan seluruh sumber pelayanan obat tampil di bawah nomor antrian yang sama, kemudian melanjutkan pelayanan sesuai SOP profesi dan jenis penanggung biaya yang berlaku.

## 5. Penanganan Kondisi Khusus

### 5.1 Bukti tidak menemukan Resep

- **Sistem Apotek** mempertahankan antrian dengan status `Unmapped` dan tidak mengizinkan proses yang mensyaratkan mapping antrean.
- **Staf Apotek** meminta bukti tambahan dan mengulangi Manual Mapping. Staf Apotek tidak boleh membuat resep elektronik sebagai pengganti bukti yang tidak ada.
- **Staf Apotek** boleh sebaliknya mencatat Pharmacy Queue Close beserta alasan wajib. **Sistem Antrian Pasien** menetapkan Queue Entry yang masih Waiting menjadi `Withdrawn`. Penutupan tidak mencatat `ServedAt` atau `DoneAt`.

### 5.2 Jual Bebas ditolak

- **Staf Apotek** tidak mencatat Jual Bebas ataupun membuat pesanan penjualan apotek.
- **Staf Apotek** boleh mencatat Pharmacy Queue Close beserta alasan wajib. **Sistem Antrian Pasien** menetapkan Queue Entry yang masih Waiting menjadi `Withdrawn`. State antrean tambahan tidak digunakan.

### 5.3 mapping antrean yang sudah tercatat ternyata salah

- **Staf Apotek** memilih Resep Kerja atau Jual Bebas yang benar.
- **Sistem Apotek** memperbarui mapping antrean yang aktif ke sumber yang benar. Sistem tidak perlu menyimpan riwayat perubahan mapping antrean.
- Perubahan mapping antrean tidak mengubah isi Resep, hasil telaah resep, atau pesanan penjualan apotek karena data tersebut berdiri sendiri.

## 6. Kriteria Penyelesaian

1. Setiap Resep Kerja atau Jual Bebas telah mempunyai catatan mapping tersendiri dengan nomor antrian yang sama, atau antrian tetap terlihat berstatus `Unmapped` sambil menunggu bukti tambahan, atau Pharmacy Queue Close dicatat dan Queue Entry berstatus `Withdrawn`.
2. Proses melakukan mapping antrean tidak mencatat `ServedAt` ataupun `DoneAt`.
3. Setiap permintaan yang di-mapping tetap memiliki identitas Resep Kerja atau Jual Bebas. Sales Order, Invoice, dan Dispensing di hilir tetap berdiri sendiri dan bukan target mapping.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087`, dan `BR-APT-143`–`BR-APT-145`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-001`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), khususnya `BR-TRK-026`–`BR-TRK-035`, `BR-TRK-039a`, dan `BR-TRK-052`.

---

# SOP APT-RJ-002 — Menerima Permintaan Obat Pasien Rawat Jalan

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-002`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-002 — Accept Outpatient Medication Demand](./SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas untuk menerima Resep yang telah ditelaah atau Jual Bebas yang disetujui. Sistem kemudian membentuk Sales Order dan Dispensing utama untuk pelayanan rawat jalan, dengan keterlacakan yang jelas ke Resep atau permintaan asalnya.

### 1.1 Posisi Patient Medication Demand dalam alur data

`Patient Medication Demand` atau Permintaan Obat Pasien adalah konsep bisnis payung untuk permintaan obat seorang Pasien. Patient Medication Demand bukan transaksi tambahan setelah Resep. Setiap Patient Medication Demand berasal dari tepat satu sumber berikut:

- `Resep`, baik Resep Elektronik maupun Resep Fisik yang telah dicatat; atau
- `Jual Bebas`, ketika permintaan obat tanpa Resep diperbolehkan.

Hanya Patient Medication Demand yang diterima yang membentuk `Sales Order`. Sales Order kemudian mengoordinasikan dua jalur turunan yang independen: jalur komersial menuju Medication Sale yang direpresentasikan oleh `Invoice`, serta jalur pemenuhan fisik menuju `Dispensing`. Keterlacakan melalui Sales Order Item: setiap Invoice Item dan setiap Dispensing Item mereferensikan tepat satu Sales Order Item.

```text
Resep ───────────────────┐
                         ├─ Patient Medication Demand
Jual Bebas ──────────────┘
                                  ├─ ditolak  → tidak ada Sales Order
                                  └─ diterima → Sales Order
                                                   ├─ Invoice
                                                   │     └── Invoice Item ← Sales Order Item
                                                   └─ Dispensing
                                                         └── Dispensing Item ← Sales Order Item
```

Resep atau Jual Bebas sumber tetap dipertahankan dan tidak berubah menjadi Sales Order. Invoice dan Dispensing dapat dibentuk serta berjalan secara independen sesuai kebijakan payer dan fulfillment yang berlaku.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Apoteker | Petugas | Menelaah setiap item obat, menghubungi Dokter Penulis Resep di luar sistem bila perlu, menetapkan obat yang dapat dilayani, lalu menyelesaikan telaah resep. |
| Staf Apotek | Petugas | Mencatat resep kertas; menerima atau menolak Jual Bebas; serta, sebelum Sales Order dibentuk, menangani kekurangan stok melalui Partial Sales Order berisi item yang dapat dipenuhi dan Copy Resep. Kekurangan stok setelah Sales Order dibentuk mengikuti `SOP-APT-RJ-003` pengecualian 5.4 (atau SOP payer yang setara). |
| CPOE | Subsistem | Menyediakan resep asli yang sah. Sistem Apotek tidak mengubah resep tersebut. |
| Katalog Obat | Subsistem | Menyediakan identitas obat dan informasi formularium yang diperlukan saat telaah resep. |
| Sistem Apotek | Subsistem | Mencatat hasil telaah, membuat pesanan apotek, serta mencatat tagihan dan kesiapan pelayanan obat secara terpisah. Aplikasi juga membuat tugas utama untuk menyiapkan obat. |
| Sistem Persediaan | Subsistem | Menyediakan ketersediaan stok dan Pharmacy Reserve melalui Stock Mutasi, tanpa menentukan apakah obat dapat diterima secara profesional. |

## 3. Prasyarat

1. Staf Apotek atau Apoteker yang bertanggung jawab telah masuk ke `Apotek Rajal` dengan hak akses yang diperlukan.
2. Tersedia resep elektronik dari sumber yang sah, resep kertas yang telah dicatat, atau Jual Bebas yang diajukan kepada Staf Apotek yang berwenang.
3. Resep mencantumkan Pasien dan perintah klinis yang menjadi sumbernya.
4. Informasi dari Katalog Obat dan ketentuan profesional untuk menerima obat tersedia.

## 4. Langkah Operasional

1. **Sistem Apotek** menampilkan resep elektronik, resep kertas yang sudah dicatat, atau Jual Bebas. Pasien tidak harus sudah datang dan nomor antrian tidak harus sudah terkonek agar data tersebut dapat ditampilkan.
2. Untuk resep, **Apoteker** memulai telaah resep. **Apoteker** memeriksa Pasien, sumber resep, obat, aturan pakai, jumlah obat, dan informasi klinis yang tersedia.
3. **Apoteker** mencatat keputusan untuk setiap item obat pada resep.
4. Bila memerlukan klarifikasi, **Apoteker** menghubungi Dokter Penulis Resep di luar sistem. Aplikasi tidak mencatat permintaan atau jawaban klarifikasi, dan tidak membuat status khusus untuk klarifikasi. Telaah tetap berstatus `Under Review` sampai **Apoteker** mengambil keputusan.
5. **Apoteker** menetapkan keputusan akhir setiap item obat: diterima sesuai resep, diterima dengan obat pengganti, atau ditolak. Resep asli dan item obat pada resep tidak diubah.
6. Bila memilih obat pengganti, **Apoteker** mencatat obat pengganti, alasan, jumlah yang terdampak, dan Apoteker yang bertanggung jawab pada item pesanan apotek. Item pesanan itu tetap terhubung ke item obat pada resep asli.
7. **Apoteker** menyelesaikan telaah resep dengan status `Approved`, `Partially Approved`, atau `Rejected`.
8. Untuk Jual Bebas, **Staf Apotek** mencatat rincian permintaan. **Staf Apotek** kemudian menerima atau menolak permintaan tersebut.
9. Untuk resep yang disetujui seluruhnya atau sebagian, atau untuk Jual Bebas yang diterima, **Sistem Apotek** membuat satu pesanan apotek dari sumber tersebut. Sistem menyimpan hubungan pesanan itu dengan resep atau permintaan asalnya.
10. **Sistem Apotek** dapat membentuk Invoice beserta Invoice Item-nya dan Dispensing beserta Dispensing Item-nya secara independen pada waktu bisnis yang berbeda. Setiap Invoice Item obat dan setiap Dispensing Item mereferensikan tepat satu Sales Order Item yang berlaku.
11. Untuk pelayanan rawat jalan biasa, **Sistem Apotek** membuat satu tugas utama untuk menyiapkan obat dan menampilkan status awal tugas tersebut.
12. **Stock Ledger** dapat mencatat Pharmacy Reserve melalui Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. **Sistem Apotek** menampilkan Mutasi itu, tetapi Mutasi tersebut belum berarti petugas boleh mulai menyiapkan obat (Dispense Authorized tetap evaluasi kebijakan terpisah).
13. Sesuai asal permintaannya, **Apoteker** atau **Staf Apotek** memeriksa hasil akhir telaah, nomor pesanan apotek, item obat yang diterima, dan nomor tugas penyiapan obat.

## 5. Pengecualian Operasional

### 5.1 Tidak ada item obat yang diterima atau Jual Bebas ditolak

- **Sistem Apotek** mencatat status `Rejected` untuk resep yang telah ditelaah. Jual Bebas yang ditolak tidak dicatat sebagai permintaan yang diterima.
- **Sistem Apotek** tidak membuat pesanan apotek.

### 5.2 Stok tidak cukup sebelum Sales Order dibentuk

Pengecualian ini berlaku hanya ketika kekurangan stok diketahui **sebelum** Sales Order dibentuk (`BR-APT-110`, alur alternatif `WF-APT-RJ-002`). Pengecualian ini tidak berlaku setelah Sales Order tersedia.

- **Sistem Persediaan** menampilkan informasi kekurangan atau selisih stok. Informasi ini tidak mengubah hasil telaah resep.
- **Staf Apotek** membentuk Sales Order hanya dari item resep yang dapat dipenuhi. Item yang tidak dapat dipenuhi tetap pada resep asal. Item tersebut tidak dimasukkan ke Sales Order dan tidak dihapus dari Sales Order, karena belum ada Sales Order yang memuatnya.
- **Sistem Apotek** mendukung Copy Resep untuk item yang tidak dipenuhi. Pasien dapat menggunakan Copy Resep tersebut untuk memperoleh obat dari apotek lain.
- **Staf Apotek** tidak membuat pesanan tertunda, tidak memilih sumber stok alternatif, dan tidak mengganti obat.

Jika Sales Order sudah dibentuk, jangan gunakan pengecualian ini. Jangan menghapus item dari Sales Order yang sudah ada. Terapkan `SOP-APT-RJ-003` pengecualian 5.4 (Pasien Umum) atau `SOP-APT-RJ-004` pengecualian 5.2 (BPJS): pertahankan Sales Order, catat Unfulfilled Medication Outcome, dan terapkan koreksi keuangan bila diperlukan (`BR-APT-118`).

### 5.3 Obat perlu diganti setelah pesanan apotek dibuat

- **Sistem Apotek** tidak mengizinkan perubahan jenis obat pada item pesanan yang sudah ada.
- **Apoteker** membatalkan item atau pesanan yang terdampak sesuai ketentuan, menelaah kembali resep asli, lalu membuat item pesanan baru. Resep asli tidak diubah dan tidak diperlukan resep perbaikan atau resep pengganti.

## 6. Kriteria Penyelesaian

1. Setiap item obat yang ditelaah sudah memiliki keputusan akhir. Telaah yang belum selesai tetap berstatus `Under Review`.
2. Untuk resep atau permintaan yang diterima, petugas dapat melihat Sales Order, Sales Order Item, dan Dispensing utama beserta Dispensing Item yang mereferensikan item Sales Order tersebut. Semuanya tetap terhubung ke sumbernya.
3. Resep berstatus `Rejected` atau Jual Bebas yang ditolak tidak memiliki pesanan apotek.
4. Informasi Mutasi stok tidak mengubah keputusan profesional untuk menerima obat.
5. Kekurangan stok yang diketahui sebelum Sales Order dibentuk menghasilkan Sales Order berisi hanya item yang dapat dipenuhi, dengan item yang tidak dapat dipenuhi tetap pada resep dan Copy Resep didukung. Kekurangan stok setelah Sales Order dibentuk tidak ditangani dengan menghapus item dari Sales Order itu; penanganannya mengikuti `SOP-APT-RJ-003` pengecualian 5.4 atau SOP payer yang setara.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-054`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, `BR-APT-089`, dan `BR-APT-105`–`BR-APT-118`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-002`.
- [Domain CPOE](../../../contexts/cpoe/CPOE-DOMAIN-ID.md).

---

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
| Kepala Apotek | Petugas | Menyetujui penanganan manual untuk obat yang sudah disiapkan tetapi tidak diambil Pasien. Otorisasi oleh Apoteker yang berwenang menurut kebijakan operasional; tidak ada ambang persetujuan berdasarkan nilai uang. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Menerima pembayaran dan mengirimkan informasi bahwa tagihan telah lunas. |
| Apoteker | Petugas | Memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. Pemeriksaan penerima tidak ditegakkan sistem. Catatan konseling rinci bersifat opsional. |
| Sistem Apotek | Subsistem | Menampilkan tagihan dan nilai yang harus dibayar, mencatat faktur serta status pembayaran, memantau penyiapan obat, dan mencatat obat yang diberikan serta diserahkan. |
| Sistem Antrian Pasien | Subsistem | Mencatat `ServedAt` saat penyiapan obat dimulai dan `DoneAt` ketika penyelesaian antrean terjadi (panggilan pengambilan, atau penyelesaian No Show jika Queue Entry masih `In Service`). `DoneAt` tidak pernah dibalik. |
| Sistem Persediaan | Subsistem | Menyediakan hasil Mutasi, Remove Stock, dan keputusan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima beban keuangan dan memberikan hasil koreksi yang diperlukan. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Outpatient Queue Mapping, pesanan apotek yang aktif, Sales Order Item yang dibayar Pasien, dan rincian harga telah ditampilkan.
3. Belum ada faktur penjualan untuk obat Pasien Umum yang akan ditawarkan.
4. Tugas untuk menyiapkan obat sudah tersedia atau dapat dibuat dari Sales Order Item melalui Dispensing Item.

## 4. Langkah Operasional

1. **Staf Apotek** membuka permintaan obat yang sudah terkonek dengan antrian pada `Apotek Rajal`. **Staf Apotek** memeriksa jumlah yang harus dibayar Pasien dari Sales Order Item yang berlaku.
2. Saat Manual Mapping dilakukan, **Staf Apotek** menerima Pasien. Setelah Tracker Mapping, **Staf Apotek** dapat memanggil nomor antrian untuk keperluan administrasi. **Sistem Antrian Pasien** tidak mencatat `ServedAt` atau `DoneAt` pada tahap ini.
3. **Staf Apotek** menyampaikan secara lisan jumlah yang harus dibayar sebelum faktur penjualan dibuat.
4. **Pasien atau Keluarga Pasien** menyatakan persetujuan pembelian secara lisan.
5. **Staf Apotek** mencatat transaksi yang disetujui. **Sistem Apotek** membuat faktur penjualan beserta Invoice Item-nya hanya dari jumlah Sales Order Item yang telah disetujui, lalu menampilkan nomor faktur dan jumlahnya.
6. **Kasir atau Sistem Pembayaran** menerima pembayaran dan mengirimkan informasi pelunasan untuk faktur tersebut.
7. **Sistem Apotek** menampilkan informasi bahwa faktur telah lunas. Untuk jumlah obat yang terkait, aplikasi menetapkan bahwa obat sudah boleh masuk ke proses penyiapan.
8. Bila Pharmacy Reserve belum ada di Dispensing Temporary Unit, **Stock Ledger** mencatat Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. **Sistem Apotek** menampilkan hasil Mutasi tersebut.
9. **Staf Apotek** mulai menyiapkan obat hanya setelah tugas penyiapan obat berstatus `Released`.
10. **Sistem Apotek** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat `ServedAt`.
11. **Staf Apotek** menyelesaikan penyiapan atau peracikan obat dan mencatatnya. **Sistem Apotek** menampilkan tugas penyiapan obat dengan status `Prepared`.
12. **Staf Apotek** memastikan setiap tugas penyiapan obat yang akan diserahkan sudah berstatus `Prepared` atau sudah memiliki catatan alasan yang jelas bila obat tidak dapat diserahkan.
13. **Staf Apotek** melakukan satu kali panggilan agar Pasien mengambil obat. **Sistem Antrian Pasien** mengubah antrian menjadi `Done` dan mencatat `DoneAt`.
14. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. Bila pemeriksaan lulus, **Sistem Apotek** menambahkan catatan pemeriksaan dan menampilkan tugas penyiapan obat berstatus `Reviewed`. **Sistem Apotek** mencatat waktu edukasi dan Apoteker penanggung jawab. Catatan konseling rinci bersifat opsional. Apoteker boleh secara opsional mencatat nomor telepon penerima dan hubungan dengan Pasien sebagai referensi.
15. **Sistem Apotek** tidak mengizinkan obat diserahkan sampai pemeriksaan akhir lulus dan Patient Education Acknowledgement dicatat. Identitas penerima bukan gate sistem. Catatan konseling rinci tidak diwajibkan. Jika Pickup Expired, **Sistem Apotek** juga tidak mengizinkan penyerahan sampai Apoteker berwenang mencatat Collection Window Override beserta alasannya.
16. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik. **Sistem Apotek** mencatat obat yang diberikan dan diserahkan untuk setiap jumlah obat yang berlaku.
17. **Sistem Persediaan** mencatat Remove Stock dari Dispensing Temporary Unit. Bila seluruh catatan yang diperlukan sudah tersedia, **Sistem Apotek** menampilkan tugas penyiapan obat berstatus `Completed`.
18. **Sistem Apotek** menampilkan pesanan apotek berstatus `Resolved` hanya setelah seluruh obat yang diterima dan seluruh urusan keuangannya selesai.

## 5. Pengecualian Operasional

### 5.1 Pasien menolak sebelum faktur dibuat

- **Staf Apotek** mencatat penolakan dan tidak membuat faktur penjualan.
- **Sistem Apotek** menandai jumlah Sales Order Item Pasien yang terdampak sebagai ditolak atau commercially unallocated, lalu meminta Stock Mutasi jumlah yang tidak digunakan dari Dispensing Temporary Unit kembali ke Pharmacy Unit.

### 5.2 Jumlah tagihan berubah sebelum faktur dibuat

- **Sistem Apotek** menampilkan jumlah terbaru yang harus dibayar.
- **Staf Apotek** menyampaikan kembali jumlah tersebut dan meminta persetujuan lisan baru sebelum mencatat transaksi.

### 5.3 Pembayaran belum lunas atau faktur yang sudah dibuat perlu dikoreksi

- **Sistem Apotek** tetap mencegah penyiapan obat dimulai selama informasi pelunasan belum tersedia.
- **Staf Apotek** hanya dapat membatalkan faktur bila status faktur yang ditampilkan mengizinkan. Bila tidak, **Tata Rekening** memberikan penyesuaian keuangan, nota kredit, pengembalian dana, atau hasil koreksi lain yang dapat dipertanggungjawabkan.

### 5.4 Stok kurang setelah Sales Order dibentuk

Pengecualian ini berlaku ketika kekurangan stok diketahui **setelah** Sales Order dibentuk, termasuk setelah pembayaran atau financial clearance (`BR-APT-118`). Kekurangan stok **sebelum** Sales Order dibentuk adalah `SOP-APT-RJ-002` pengecualian 5.2.

- **Staf Apotek** tidak membuat pesanan tertunda, tidak memilih sumber stok alternatif, dan tidak mengganti obat.
- **Staf Apotek** tidak menghapus item dari Sales Order yang sudah dibentuk dan tidak menyusun ulang Sales Order menjadi pesanan parsial.
- **Sistem Apotek** mencatat Unfulfilled Medication Outcome yang berlaku, mendukung Copy Resep untuk item yang tidak dipenuhi, dan tetap menampilkan urusan keuangan yang harus diselesaikan.
- **Tata Rekening** memberikan nota kredit, pengembalian dana, atau koreksi komersial lain yang dapat dipertanggungjawabkan bila ada konsekuensi komersial.

### 5.5 Pemeriksaan akhir obat tidak lulus

- Bila pemeriksaan akhir obat tidak lulus, **Apoteker** mencatat alasan dan jumlah obat yang terdampak serta tidak menyetujui penyerahan.
- **Sistem Apotek** menambahkan catatan pemeriksaan yang tidak dapat diubah, lengkap dengan Apoteker dan waktu keputusan, mengembalikan tugas penyiapan obat dari `Prepared` ke `Preparing`, dan tetap mencegah penyerahan.
- **Staf Apotek** memperbaiki dan menyiapkan kembali obat yang terdampak. **Sistem Apotek** mengembalikan tugas tersebut ke `Prepared`, lalu **Apoteker** melakukan pemeriksaan akhir obat yang baru. Catatan pemeriksaan sebelumnya tetap terlihat dan tidak berubah.

### 5.6 Pasien tidak mengambil obat

- **Kepala Apotek** menerapkan `SOP-APT-RJ-007`. Jika antrian sudah `Done`, catatan `DoneAt` tidak dihapus atau diubah kembali. Jika antrian masih `In Service` karena pickup call belum terjadi, resolusi itu boleh menyelesaikannya menjadi `Done` dan mencatat `DoneAt`.

## 6. Kriteria Penyelesaian

1. Faktur penjualan terlihat sudah lunas.
2. Tugas penyiapan obat berstatus `Completed`, dan catatan penyerahan obat mencantumkan waktu penyerahan. Nomor telepon penerima dan hubungan dengan Pasien boleh dicatat secara opsional sebagai referensi.
3. Remove Stock dari Dispensing Temporary Unit terlihat sebagai catatan Stock Ledger.
4. Pesanan apotek berstatus `Resolved`, atau tetap `Active` dengan masalah pelayanan obat atau urusan keuangan yang belum selesai ditampilkan dengan jelas.
5. Status antrian `Done` bukan bukti bahwa obat sudah diserahkan.
6. Kekurangan stok setelah Sales Order dibentuk tidak mengubah Sales Order itu; jumlah yang tidak dapat dipenuhi memiliki Unfulfilled Medication Outcome dan koreksi keuangan bila diperlukan, bukan item yang dihapus dari Sales Order.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, `BR-APT-125`–`BR-APT-134`, dan `BR-APT-138`–`BR-APT-142`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-003`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), `BR-TRK-045`, `BR-TRK-045a`, dan `BR-TRK-046`.
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).

---

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
- **Tata Rekening** memberikan nota kredit, pengembalian dana, atau koreksi komersial lain yang dapat dipertanggungjawabkan hanya bila ada konsekuensi komersial.

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

---

# SOP APT-RJ-005 — Melayani Obat dengan Penjaminan Campuran

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-005`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-005 — Fulfill Mixed-Coverage Medication](./SOP-APT-RJ-005-Pelayanan-Obat-Penjaminan-Campuran-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas ketika Fornas mengklasifikasikan sebagian item resep sebagai Covered dan sebagian sebagai Not Covered. Item Covered membentuk Sales Order BPJS. Item Not Covered boleh membentuk Patient-Pay Sales Order independen. Kedua pesanan dapat diserahkan bersama dalam satu kali pengambilan.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyetujui atau menolak Patient-Pay Sales Order, membayar setelah menyetujui, datang untuk mengambil obat, menerima edukasi, dan menerima obat bila berhak. |
| Staf Apotek | Petugas | Membentuk Sales Order BPJS untuk item Covered dan Patient-Pay Sales Order independen untuk item Not Covered, menyampaikan jumlah yang harus dibayar Pasien, mencatat transaksi yang disetujui, menyiapkan obat yang sudah boleh diproses, dan memanggil Pasien. |
| Kepala Apotek | Petugas | Menyetujui penanganan manual untuk obat yang sudah disiapkan tetapi tidak diambil Pasien. Otorisasi oleh Apoteker yang berwenang menurut kebijakan operasional; tidak ada ambang persetujuan berdasarkan nilai uang. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Menerima pembayaran dan mengirimkan informasi bahwa faktur Patient-Pay telah lunas. |
| Apoteker | Petugas | Memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. Pemeriksaan penerima tidak ditegakkan sistem. Catatan konseling rinci bersifat opsional. |
| Sistem SEP dan Fornas | Subsistem | Mengklasifikasikan setiap item resep sebagai Covered atau Not Covered dan menyediakan keabsahan SEP. |
| Sistem Apotek | Subsistem | Menyimpan Sales Order independen, faktur sesuai jalur payer, dan evaluasi Dispense Authorized per item. |
| Sistem Antrian Pasien | Subsistem | Mencatat satu `ServedAt` dan satu `DoneAt` untuk antrian yang sama. |
| Sistem Persediaan | Subsistem | Menyediakan hasil Mutasi, Remove Stock, dan keputusan pengembalian stok. |
| Tata Rekening | Subsistem | Menerima dan menyelesaikan urusan keuangan sesuai jalur payer. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Fornas telah mengklasifikasikan item resep sebagai Covered atau Not Covered.
3. SEP masih sah untuk item Covered.
4. Item Not Covered tidak dibatalkan secara otomatis.
5. Belum ada faktur Patient-Pay maupun faktur BPJS untuk item tersebut, kecuali proses dilanjutkan setelah kondisi khusus yang sudah tercatat.

## 4. Langkah Operasional

1. **Sistem SEP dan Fornas** mengklasifikasikan setiap item resep sebagai Covered atau Not Covered. **Sistem Apotek** menampilkan klasifikasi tersebut.
2. **Staf Apotek** membentuk Sales Order BPJS hanya dari item Covered. Item yang tidak dijamin tidak tetap pada jalur BPJS.
3. **Staf Apotek** boleh membentuk Patient-Pay Sales Order terpisah untuk item Not Covered.
4. **Sistem Apotek** menghitung dan menampilkan jumlah yang harus dibayar Pasien dari Patient-Pay Sales Order.
5. **Staf Apotek** menyampaikan jumlah tersebut secara lisan sebelum faktur Pasien Umum dibuat.
6. **Pasien atau Keluarga Pasien** menyatakan persetujuan lisan atas Patient-Pay Sales Order.
7. **Staf Apotek** mencatat transaksi yang disetujui. **Sistem Apotek** membuat faktur Pasien Umum dari Patient-Pay Sales Order tersebut.
8. **Kasir atau Sistem Pembayaran** menerima pembayaran dan mengirimkan informasi pelunasan untuk faktur Patient-Pay.
9. **Sistem Apotek** mengevaluasi Dispense Authorized secara independen: item Covered dari evidence coverage; item Patient-Pay dari Payment Clearance.
10. Setelah setiap jumlah obat yang akan diserahkan memperoleh Dispense Authorized, **Staf Apotek** memulai dan menyelesaikan penyiapan obat pada setiap Dispensing yang berlaku.
11. Saat penyiapan obat pertama dimulai, **Sistem Apotek** mencatat `Medication Preparation Started`. **Sistem Antrian Pasien** mengubah antrian menjadi `In Service` dan mencatat satu `ServedAt`.
12. **Sistem Apotek** menampilkan setiap tugas penyiapan obat yang akan diserahkan sebagai `Prepared` atau dengan catatan alasan yang jelas bila obat tidak dapat diserahkan.
13. **Staf Apotek** melakukan satu kali panggilan agar Pasien mengambil obat. **Sistem Antrian Pasien** mengubah antrian yang sama menjadi `Done` dan mencatat satu `DoneAt`.
14. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima secara operasional, melakukan pemeriksaan akhir obat, dan mencatat Patient Education Acknowledgement. **Sistem Apotek** mencatat waktu edukasi dan Apoteker penanggung jawab. Catatan konseling rinci bersifat opsional. Apoteker boleh secara opsional mencatat nomor telepon penerima dan hubungan dengan Pasien sebagai referensi.
15. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik. Jika Pickup Expired, Apoteker berwenang harus terlebih dahulu mencatat Collection Window Override beserta alasannya.
16. Ketika obat Sales Order BPJS berhasil diserahkan, **Sistem Apotek** membuat faktur BPJS. Aplikasi juga mencatat obat yang diberikan dan diserahkan untuk seluruh jumlah obat, serta tetap memisahkan kedua Sales Order.
17. **Sistem Persediaan** mengirimkan catatan Remove Stock. **Sistem Apotek** menampilkan perkembangan akhir tugas penyiapan obat dan masing-masing Sales Order.

## 5. Pengecualian Operasional

### 5.1 Pasien menolak Patient-Pay Sales Order sebelum faktur dibuat

- **Staf Apotek** mencatat penolakan dan tidak membuat faktur Pasien Umum.
- **Sistem Apotek** mencatat outcome declined pada Patient-Pay Sales Order. Sales Order BPJS tetap dapat diproses secara independen.

### 5.2 Item diklasifikasikan Not Covered

- **Sistem Apotek** menampilkan item terdampak sebagai Not Covered dan tidak memasukkannya ke Sales Order BPJS.
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
- **Kepala Apotek** menerapkan `SOP-APT-RJ-007` untuk obat yang tidak diambil. Jika Queue Entry bersama sudah `Done`, `DoneAt` tidak dibalik. Jika masih `In Service` karena pickup call belum terjadi, resolusi itu boleh menyelesaikannya menjadi `Done` dan mencatat `DoneAt`.

## 6. Kriteria Penyelesaian

1. Item Covered dan Not Covered tetap pada Sales Order yang independen.
2. Faktur Pasien Umum yang sudah lunas dan faktur BPJS yang dibuat saat obat diserahkan tampil terpisah dan terhubung ke Sales Order masing-masing.
3. Satu kali penyerahan obat dapat mencatat seluruh jumlah obat yang berlaku. Nomor telepon penerima dan hubungan dengan Pasien boleh dicatat secara opsional sebagai referensi.
4. Setiap Sales Order berstatus `Resolved`, atau tetap `Active` dengan masalah yang belum selesai ditampilkan secara jelas.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-011`, `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, `BR-APT-090`–`BR-APT-096`, `BR-APT-108`, `BR-APT-119`–`BR-APT-124`, dan `BR-APT-129`–`BR-APT-134`, dan `BR-APT-138`–`BR-APT-142`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-005`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).

---

# SOP APT-RJ-006 — Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrian

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-006`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-006 — Coordinate Multiple Medication Demands in One Queue](./SOP-APT-RJ-006-Koordinasi-Beberapa-Kebutuhan-Obat-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas untuk melayani dua atau lebih resep atau permintaan obat dengan satu nomor antrian dan satu kali pengambilan. Catatan setiap resep atau permintaan tetap terpisah; catatan tersebut tidak digabungkan hanya karena obat diambil pada waktu yang sama.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Menyelesaikan urusan pembayaran atau penjaminan yang berlaku, datang untuk satu kali pengambilan, menerima edukasi gabungan, dan menerima obat yang dapat diserahkan. |
| Staf Apotek | Petugas | Memeriksa mapping dan keadaan setiap resep atau permintaan obat, menyiapkan setiap tugas penyiapan obat secara terpisah, mengoordinasikan kesiapan menurut penanggung biaya, menjelaskan masalah yang terjadi, dan melakukan satu kali panggilan pengambilan obat. |
| Apoteker | Petugas | Memeriksa penerima secara operasional, memeriksa setiap obat yang sudah disiapkan, dan mencatat Patient Education Acknowledgement untuk sesi terkoordinasi. Pemeriksaan penerima tidak ditegakkan sistem. Catatan konseling rinci bersifat opsional. |
| Sistem Apotek | Subsistem | Menampilkan keadaan setiap resep atau permintaan obat serta mencatat Invoice, Dispensing, obat yang diberikan, dan penyerahan obat secara terpisah. |
| Sistem Antrian Pasien | Subsistem | Menyimpan satu entri antrian dengan satu `CreatedAt`, paling banyak satu `ServedAt`, dan satu `DoneAt`. |
| Kasir atau Sistem Pembayaran | Petugas atau subsistem | Mengirimkan informasi pelunasan untuk obat yang harus dibayar Pasien. |
| Sistem SEP dan Fornas | Subsistem | Mengirimkan bukti jaminan untuk obat BPJS yang berlaku. |
| Sistem Persediaan | Subsistem | Menyediakan hasil Mutasi, Remove Stock, dan keputusan stok untuk setiap Dispensing. |

## 3. Prasyarat

1. Semua petugas yang terlibat telah masuk ke aplikasi dan memiliki hak akses yang diperlukan.
2. Satu entri antrian sudah terkonek secara terpisah ke sedikitnya dua Resep Kerja dan/atau Jual Bebas.
3. Setiap resep sudah memiliki telaah resep masing-masing.
4. Setiap sumber yang diterima sudah memiliki pesanan apotek dan tugas utama untuk menyiapkan obat rawat jalan yang aktif masing-masing.

## 4. Langkah Operasional

1. **Staf Apotek** membuka entri antrian bersama pada `Apotek Rajal`.
2. **Sistem Apotek** menampilkan setiap resep atau permintaan obat secara terpisah, termasuk sumbernya, Sales Order, penanggung biaya, Sales Order Item, Invoice, evaluasi Dispense Authorized, dan keadaan Dispensing.
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
13. Saat Pasien atau Keluarga Pasien hadir, **Apoteker** memeriksa penerima secara operasional, melakukan pemeriksaan akhir pada setiap obat yang sudah disiapkan, dan mencatat Patient Education Acknowledgement. Untuk setiap pemeriksaan yang lulus, **Sistem Apotek** menambahkan catatan pemeriksaan dan menampilkan tugas penyiapan obat terkait berstatus `Reviewed`. **Sistem Apotek** mencatat waktu edukasi dan Apoteker penanggung jawab. Catatan konseling rinci bersifat opsional. Apoteker boleh secara opsional mencatat nomor telepon penerima dan hubungan dengan Pasien sebagai referensi.
14. Setelah mendapat persetujuan **Apoteker**, **Staf Apotek** menyerahkan obat secara fisik. Jika Pickup Expired, Apoteker berwenang harus terlebih dahulu mencatat Collection Window Override beserta alasannya.
15. **Sistem Apotek** mencatat obat yang diberikan dan diserahkan pada setiap tugas penyiapan obat dan pesanan apotek secara terpisah.
16. **Sistem Persediaan** mencatat Remove Stock atau Mutasi pengembalian secara terpisah untuk setiap Dispensing asal.

## 5. Pengecualian Operasional

### 5.1 Salah satu resep atau permintaan obat belum selesai

- **Sistem Apotek** menampilkan resep atau permintaan tersebut sebagai belum siap.
- **Staf Apotek** menunda panggilan pengambilan bersama, kecuali SOP menurut penanggung biaya mengizinkan penyerahan sebagian dan Pasien sudah menyetujuinya.

### 5.2 Salah satu resep atau permintaan obat memiliki masalah yang sudah ditangani

- **Staf Apotek** menjelaskan masalah dan hasil penanganannya saat memberi informasi pengambilan. Obat lain yang sudah siap hanya dapat dilanjutkan bila SOP menurut penanggung biaya mengizinkannya.
- **Sistem Apotek** menyimpan catatan masalah tersebut pada pesanan apotek dan tugas penyiapan obat asalnya.

### 5.3 Salah satu pemeriksaan akhir obat tidak lulus

- **Apoteker** mencatat alasan kegagalan dan jumlah obat yang terdampak pada tugas penyiapan asal serta tidak menyetujui penyerahannya.
- **Sistem Apotek** menambahkan catatan pemeriksaan yang tidak dapat diubah, lengkap dengan Apoteker dan waktu keputusan, lalu hanya mengembalikan tugas penyiapan obat tersebut dari `Prepared` ke `Preparing`. Catatan permintaan lain tidak berubah.
- **Staf Apotek** memperbaiki dan menyiapkan kembali obat tersebut. **Sistem Apotek** mengembalikannya ke `Prepared`, lalu **Apoteker** melakukan pemeriksaan baru. Obat itu tidak boleh diserahkan sebelum pemeriksaan baru lulus.

### 5.4 mapping antrean atau sumber pelayanan obat perlu diperbaiki

- **Staf Apotek** memperbaiki hanya mapping atau sumber pelayanan obat yang terdampak.
- **Sistem Apotek** tidak mengubah catatan resep atau permintaan obat lain. Satu catatan penyerahan obat tidak boleh dianggap menyelesaikan resep atau permintaan obat lain.

## 6. Kriteria Penyelesaian

1. Entri antrian bersama menampilkan satu `CreatedAt`, paling banyak satu `ServedAt`, dan satu `DoneAt`.
2. Setiap resep, pesanan apotek, faktur, dan tugas penyiapan obat tetap memiliki identitas serta keadaan masing-masing.
3. Hanya ada satu panggilan pengambilan obat, tetapi setiap resep atau permintaan obat memiliki catatan penyerahan obat sendiri atau alasan yang jelas bila belum dapat diserahkan.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-129`–`BR-APT-134`, dan `BR-APT-138`–`BR-APT-142`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-006`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), khususnya `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, dan `BR-TRK-045a`.

---

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
| Tata Rekening | Subsistem | Memberikan nota kredit, pengembalian dana, atau penyelesaian keuangan lain untuk obat yang telah dibayar. |
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
9. Untuk obat Pasien Umum yang sudah dibayar, **Sistem Apotek** mengirimkan urusan keuangan yang harus diselesaikan kepada **Tata Rekening** dan mempertahankan pesanan apotek berstatus `Active`.
10. **Tata Rekening** memberikan nota kredit, pengembalian dana, atau hasil akhir keuangan lain yang dapat dipertanggungjawabkan. **Sistem Apotek** menampilkan hasil tersebut pada bagian obat asalnya.
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
- **Tata Rekening** menyelesaikan koreksi keuangan yang diperlukan.

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
4. Pesanan apotek Pasien Umum yang sudah dibayar tetap `Active` sampai nota kredit, pengembalian dana, atau hasil akhir keuangan lain terlihat.
5. Pesanan apotek baru menjadi `Resolved` setelah seluruh urusan obat dan keuangan selesai.
6. Queue Entry terkait berstatus `Done`. `DoneAt` dicatat pada pickup call atau pada penyelesaian No Show ini dan tidak dibalik.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-069`, `BR-APT-078`–`BR-APT-080`, `BR-APT-095`, dan `BR-APT-135`–`BR-APT-142`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-007`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
