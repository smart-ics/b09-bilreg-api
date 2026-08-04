# SOP MF-RJ-001 — Menerbitkan Nomor Antrian dan Mengkoneksikannya dengan Resep atau Permintaan Obat Langsung

**Status dokumen:** Spesifikasi operasional acuan

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-MF-RJ-001`

**Dokumen acuan bahasa Inggris:** [SOP MF-RJ-001 — Acquire and Map Outpatient Pharmacy Queue](./SOP-MF-RJ-001-Acquire-and-Map-Outpatient-Pharmacy-Queue.md)

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara penerbitan nomor antrian apotek rawat jalan dan mengkoneksikannya dengan Resep atau Permintaan Obat Langsung yang sesuai, baik secara otomatis melalui sistem maupun secara manual oleh petugas.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Mengambil nomor antrian serta menunjukkan bukti pelacakan pasien, registrasi, resep, atau permintaan obat langsung yang tersedia. |
| Staf Apotek | Petugas | Menemukan antrian yang belum terkoneksi, memeriksa bukti, mencatat resep kertas bila ada, dan melakukan pemetaan manual. |
| Aplikasi Pelayanan Obat | Aplikasi | Mencoba mengkoneksikan antrian secara otomatis dengan Resep yang sudah ada, mencatat koneksinya, dan menampilkan perkembangan setiap sumber pelayanan obat. |
| Sistem Antrian Pasien | Subsistem | Membuat entri antrian apotek, menerbitkan nomor antrian, mencatat `CreatedAt`, dan mengelola siklus antrian. |

## 3. Prasyarat

1. Staf Apotek telah masuk ke ruang kerja `Apotek Rajal` dan memiliki hak untuk memetakan antrian.
2. Sesi antrian apotek rawat jalan tersedia.
3. Pasien meminta nomor antrian secara langsung atau menunjukkan bukti pelacakan pasien maupun registrasi yang sah.
4. Setiap Resep atau Permintaan Obat Langsung yang akan dikoneksikan mempunyai sumber yang dapat dipertanggungjawabkan atau sumber tersebut dicatat saat koneksi manual.

## 4. Langkah Operasional

1. **Pasien atau Keluarga Pasien** meminta nomor antrian apotek rawat jalan atau menunjukkan bukti pelacakan pasien maupun registrasi yang sah.
2. **Sistem Antrian Pasien** membuat entri antrian, menerbitkan nomor antrian, mencatat `CreatedAt`, lalu menampilkan atau mengirimkan nomor tersebut.
3. **Aplikasi Pelayanan Obat** mencoba mengkoneksikan antrian secara otomatis dengan satu atau beberapa Resep yang sudah ada berdasarkan bukti tracker atau registrasi yang diberikan. Proses ini tidak membuat Resep baru dan tidak berlaku untuk Permintaan Obat Langsung.
4. Untuk setiap Resep yang ditemukan, **Aplikasi Pelayanan Obat** membuat catatan koneksi antrian secara terpisah dan menampilkan perkembangan masing-masing Resep.
5. Jika antrian masih berstatus `Unmapped`, **Staf Apotek** dapat memanggil nomor antrian untuk keperluan administrasi. Pemanggilan ini tidak boleh mencatat `ServedAt` ataupun `DoneAt`.
6. **Pasien atau Keluarga Pasien** menunjukkan nomor antrian beserta resep, bukti registrasi, atau permintaan obat langsung yang tersedia.
7. **Staf Apotek** memilih antrian yang belum terkoneksi dan mencari resep atau pesanan penjualan apotek yang sesuai.
8. Jika pasien membawa resep kertas, **Staf Apotek** mencatat resep tersebut sebelum meminta telaah resep. Jika pasien mengajukan permintaan obat langsung, **Staf Apotek** menilainya menurut prosedur penerimaan kebutuhan obat.
9. **Staf Apotek** mengkoneksikan antrian secara manual dengan setiap Resep atau Permintaan Obat Langsung yang berhasil diidentifikasi.
10. **Aplikasi Pelayanan Obat** menampilkan koneksi yang berhasil dengan status `Mapped` dan tetap menyimpan setiap sumber pelayanan obat sebagai catatan tersendiri.
11. **Staf Apotek** memastikan seluruh sumber pelayanan obat tampil di bawah nomor antrian yang sama, kemudian melanjutkan pelayanan sesuai SOP profesi dan jenis penanggung biaya yang berlaku.

## 5. Penanganan Kondisi Khusus

### 5.1 Bukti tidak menemukan Resep

- **Aplikasi Pelayanan Obat** mempertahankan antrian dengan status `Unmapped` dan tidak mengizinkan proses yang mensyaratkan koneksi antrian.
- **Staf Apotek** meminta bukti tambahan dan mengulangi koneksi manual. Staf Apotek tidak boleh membuat resep elektronik sebagai pengganti bukti yang tidak ada.

### 5.2 Permintaan obat langsung ditolak

- **Staf Apotek** tidak mencatat permintaan obat langsung ataupun membuat pesanan penjualan apotek.
- **Sistem Antrian Pasien** mempertahankan antrian dengan status menunggu sampai kebijakan pembatalan antrian diterapkan.

### 5.3 Koneksi antrian yang sudah tercatat ternyata salah

- **Staf Apotek** memilih Resep atau sumber pelayanan obat yang benar.
- **Aplikasi Pelayanan Obat** memperbarui koneksi antrian yang aktif ke sumber yang benar. Sistem tidak perlu menyimpan riwayat perubahan koneksi antrian.
- Perubahan koneksi antrian tidak mengubah isi Resep, hasil telaah resep, atau pesanan penjualan apotek karena data tersebut berdiri sendiri.

## 6. Kriteria Penyelesaian

1. Setiap Resep atau Permintaan Obat Langsung telah mempunyai catatan koneksi tersendiri dengan nomor antrian yang sama, atau antrian tetap terlihat berstatus `Unmapped` sambil menunggu bukti tambahan.
2. Proses mengkoneksikan antrian tidak mencatat `ServedAt` ataupun `DoneAt`.
3. Setiap sumber pelayanan obat tetap memiliki identitas resep bila ada, pesanan penjualan apotek, faktur penjualan, dan perintah penyiapan obat masing-masing.

## 7. Referensi

- [Domain Pelayanan Obat](../medication-fulfillment-domain-id.md), khususnya `BR-MF-061`–`BR-MF-065`, `BR-MF-082`, dan `BR-MF-084`–`BR-MF-087`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-001`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), khususnya `BR-TRK-026`–`BR-TRK-035`.
