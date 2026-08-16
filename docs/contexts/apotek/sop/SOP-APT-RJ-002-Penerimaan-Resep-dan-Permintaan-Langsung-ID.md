# SOP APT-RJ-002 — Menerima Permintaan Obat Pasien Rawat Jalan

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-002`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-002 — Accept Outpatient Medication Demand](./SOP-APT-RJ-002-Penerimaan-Resep-dan-Permintaan-Langsung-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Memberikan langkah yang sama bagi petugas untuk menerima Resep yang telah ditelaah atau Direct Medication Request yang disetujui. Sistem kemudian membentuk Sales Order dan Dispense Order utama untuk pelayanan rawat jalan, dengan keterlacakan yang jelas ke Resep atau permintaan asalnya.

### 1.1 Posisi Patient Medication Demand dalam alur data

`Patient Medication Demand` atau Permintaan Obat Pasien adalah konsep bisnis payung untuk permintaan obat seorang Pasien. Patient Medication Demand bukan transaksi tambahan setelah Resep. Setiap Patient Medication Demand berasal dari tepat satu sumber berikut:

- `Resep`, baik Resep Elektronik maupun Resep Fisik yang telah dicatat; atau
- `Direct Medication Request`, ketika permintaan obat tanpa Resep diperbolehkan.

Hanya Patient Medication Demand yang diterima yang membentuk `Sales Order`. Sales Order kemudian mengoordinasikan dua jalur turunan yang independen: jalur komersial menuju Medication Sale yang direpresentasikan oleh `Sales Invoice`, serta jalur pemenuhan fisik menuju `Dispense Order`. Keterlacakan melalui Sales Order Line: setiap Sales Invoice Item dan setiap Dispense Order Line mereferensikan tepat satu Sales Order Line.

```text
Resep ───────────────────┐
                         ├─ Patient Medication Demand
Direct Medication Request┘        │
                                  ├─ ditolak  → tidak ada Sales Order
                                  └─ diterima → Sales Order
                                                   ├─ Sales Invoice
                                                   │     └── Sales Invoice Item ← Sales Order Line
                                                   └─ Dispense Order
                                                         └── Dispense Order Line ← Sales Order Line
```

Resep atau Direct Medication Request sumber tetap dipertahankan dan tidak berubah menjadi Sales Order. Sales Invoice dan Dispense Order dapat dibentuk serta berjalan secara independen sesuai kebijakan payer dan fulfillment yang berlaku.

## 2. Aktor dan Tanggung Jawab

| Aktor | Jenis | Tanggung jawab |
|---|---|---|
| Apoteker | Petugas | Menelaah setiap item obat, menghubungi Dokter Penulis Resep di luar sistem bila perlu, menetapkan obat yang dapat dilayani, lalu menyelesaikan telaah resep. |
| Staf Apotek | Petugas | Mencatat resep kertas; menerima atau menolak permintaan obat langsung; serta, sebelum Sales Order dibentuk, menangani kekurangan stok melalui Partial Sales Order berisi baris yang dapat dipenuhi dan Salinan Resep. Kekurangan stok setelah Sales Order dibentuk mengikuti `SOP-APT-RJ-003` pengecualian 5.4 (atau SOP payer yang setara). |
| CPOE | Subsistem | Menyediakan resep asli yang sah. Sistem Apotek tidak mengubah resep tersebut. |
| Katalog Obat | Subsistem | Menyediakan identitas obat dan informasi formularium yang diperlukan saat telaah resep. |
| Sistem Apotek | Subsistem | Mencatat hasil telaah, membuat pesanan apotek, serta mencatat tagihan dan kesiapan pelayanan obat secara terpisah. Aplikasi juga membuat tugas utama untuk menyiapkan obat. |
| Sistem Persediaan | Subsistem | Menyediakan ketersediaan stok dan Pharmacy Reserve melalui Stock Mutasi, tanpa menentukan apakah obat dapat diterima secara profesional. |

## 3. Prasyarat

1. Staf Apotek atau Apoteker yang bertanggung jawab telah masuk ke `Apotek Rajal` dengan hak akses yang diperlukan.
2. Tersedia resep elektronik dari sumber yang sah, resep kertas yang telah dicatat, atau permintaan obat langsung yang diajukan kepada Staf Apotek yang berwenang.
3. Resep mencantumkan Pasien dan perintah klinis yang menjadi sumbernya.
4. Informasi dari Katalog Obat dan ketentuan profesional untuk menerima obat tersedia.

## 4. Langkah Operasional

1. **Sistem Apotek** menampilkan resep elektronik, resep kertas yang sudah dicatat, atau permintaan obat langsung. Pasien tidak harus sudah datang dan nomor antrian tidak harus sudah terkonek agar data tersebut dapat ditampilkan.
2. Untuk resep, **Apoteker** memulai telaah resep. **Apoteker** memeriksa Pasien, sumber resep, obat, aturan pakai, jumlah obat, dan informasi klinis yang tersedia.
3. **Apoteker** mencatat keputusan untuk setiap item obat pada resep.
4. Bila memerlukan klarifikasi, **Apoteker** menghubungi Dokter Penulis Resep di luar sistem. Aplikasi tidak mencatat permintaan atau jawaban klarifikasi, dan tidak membuat status khusus untuk klarifikasi. Telaah tetap berstatus `Under Review` sampai **Apoteker** mengambil keputusan.
5. **Apoteker** menetapkan keputusan akhir setiap item obat: diterima sesuai resep, diterima dengan obat pengganti, atau ditolak. Resep asli dan item obat pada resep tidak diubah.
6. Bila memilih obat pengganti, **Apoteker** mencatat obat pengganti, alasan, jumlah yang terdampak, dan Apoteker yang bertanggung jawab pada item pesanan apotek. Item pesanan itu tetap terhubung ke item obat pada resep asli.
7. **Apoteker** menyelesaikan telaah resep dengan status `Approved`, `Partially Approved`, atau `Rejected`.
8. Untuk permintaan obat langsung, **Staf Apotek** mencatat rincian permintaan. **Staf Apotek** kemudian menerima atau menolak permintaan tersebut.
9. Untuk resep yang disetujui seluruhnya atau sebagian, atau untuk permintaan obat langsung yang diterima, **Sistem Apotek** membuat satu pesanan apotek dari sumber tersebut. Sistem menyimpan hubungan pesanan itu dengan resep atau permintaan asalnya.
10. **Sistem Apotek** dapat membentuk Sales Invoice beserta Sales Invoice Item-nya dan Dispense Order beserta Dispense Order Line-nya secara independen pada waktu bisnis yang berbeda. Setiap Sales Invoice Item obat dan setiap Dispense Order Line mereferensikan tepat satu Sales Order Line yang berlaku.
11. Untuk pelayanan rawat jalan biasa, **Sistem Apotek** membuat satu tugas utama untuk menyiapkan obat dan menampilkan status awal tugas tersebut.
12. **Stock Ledger** dapat mencatat Pharmacy Reserve melalui Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. **Sistem Apotek** menampilkan Mutasi itu, tetapi Mutasi tersebut belum berarti petugas boleh mulai menyiapkan obat (Dispense Authorized tetap evaluasi kebijakan terpisah).
13. Sesuai asal permintaannya, **Apoteker** atau **Staf Apotek** memeriksa hasil akhir telaah, nomor pesanan apotek, item obat yang diterima, dan nomor tugas penyiapan obat.

## 5. Pengecualian Operasional

### 5.1 Tidak ada item obat yang diterima atau permintaan langsung ditolak

- **Sistem Apotek** mencatat status `Rejected` untuk resep yang telah ditelaah. Permintaan obat langsung yang ditolak tidak dicatat sebagai permintaan yang diterima.
- **Sistem Apotek** tidak membuat pesanan apotek.

### 5.2 Stok tidak cukup sebelum Sales Order dibentuk

Pengecualian ini berlaku hanya ketika kekurangan stok diketahui **sebelum** Sales Order dibentuk (`BR-APT-110`, alur alternatif `WF-APT-RJ-002`). Pengecualian ini tidak berlaku setelah Sales Order tersedia.

- **Sistem Persediaan** menampilkan informasi kekurangan atau selisih stok. Informasi ini tidak mengubah hasil telaah resep.
- **Staf Apotek** membentuk Sales Order hanya dari item resep yang dapat dipenuhi. Item yang tidak dapat dipenuhi tetap pada resep asal. Item tersebut tidak dimasukkan ke Sales Order dan tidak dihapus dari Sales Order, karena belum ada Sales Order yang memuatnya.
- **Sistem Apotek** mendukung Salinan Resep untuk item yang tidak dipenuhi. Pasien dapat menggunakan salinan tersebut untuk memperoleh obat dari apotek lain.
- **Staf Apotek** tidak membuat pesanan tertunda, tidak memilih sumber stok alternatif, dan tidak mengganti obat.

Jika Sales Order sudah dibentuk, jangan gunakan pengecualian ini. Jangan menghapus item dari Sales Order yang sudah ada. Terapkan `SOP-APT-RJ-003` pengecualian 5.4 (Pasien Umum) atau `SOP-APT-RJ-004` pengecualian 5.2 (BPJS): pertahankan Sales Order, catat Unfulfilled Medication Outcome, dan terapkan koreksi keuangan bila diperlukan (`BR-APT-118`).

### 5.3 Obat perlu diganti setelah pesanan apotek dibuat

- **Sistem Apotek** tidak mengizinkan perubahan jenis obat pada item pesanan yang sudah ada.
- **Apoteker** membatalkan item atau pesanan yang terdampak sesuai ketentuan, menelaah kembali resep asli, lalu membuat item pesanan baru. Resep asli tidak diubah dan tidak diperlukan resep perbaikan atau resep pengganti.

## 6. Kriteria Penyelesaian

1. Setiap item obat yang ditelaah sudah memiliki keputusan akhir. Telaah yang belum selesai tetap berstatus `Under Review`.
2. Untuk resep atau permintaan yang diterima, petugas dapat melihat Sales Order, Sales Order Line, dan Dispense Order utama beserta Dispense Order Line yang mereferensikan baris Sales Order tersebut. Semuanya tetap terhubung ke sumbernya.
3. Resep berstatus `Rejected` atau permintaan obat langsung yang ditolak tidak memiliki pesanan apotek.
4. Informasi Mutasi stok tidak mengubah keputusan profesional untuk menerima obat.
5. Kekurangan stok yang diketahui sebelum Sales Order dibentuk menghasilkan Sales Order berisi hanya baris yang dapat dipenuhi, dengan baris yang tidak dapat dipenuhi tetap pada resep dan Salinan Resep didukung. Kekurangan stok setelah Sales Order dibentuk tidak ditangani dengan menghapus baris dari Sales Order itu; penanganannya mengikuti `SOP-APT-RJ-003` pengecualian 5.4 atau SOP payer yang setara.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-054`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, `BR-APT-089`, dan `BR-APT-105`–`BR-APT-118`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-002`.
- [Domain CPOE](../../../contexts/cpoe/CPOE-DOMAIN-ID.md).
