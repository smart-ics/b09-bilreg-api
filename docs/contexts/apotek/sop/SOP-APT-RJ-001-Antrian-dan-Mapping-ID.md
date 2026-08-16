# SOP APT-RJ-001 — Menerbitkan Nomor Antrian dan Melakukan Mapping dengan Resep atau Permintaan Obat Langsung

**Status dokumen:** Pendamping operasional Bahasa Indonesia

**Konteks domain:** Pelayanan Obat

**Alur kerja:** `WF-APT-RJ-001`

**Dokumen acuan bahasa Inggris:** [SOP APT-RJ-001 — Acquire and Map Outpatient Pharmacy Queue](./SOP-APT-RJ-001-Antrian-dan-Mapping-EN.md)

**Aturan precedence:** Jika terdapat perbedaan semantik, dokumen acuan bahasa Inggris berlaku setelah diverifikasi terhadap WORKFLOW dan DOMAIN yang lebih tinggi; pasangan dokumen harus segera disinkronkan.

**Istilah pada aplikasi:** `Apotek` dan `Apotek Rajal` adalah nama menu yang telah ditetapkan. Nilai status sistem ditulis dalam tanda backtick.

## 1. Tujuan

Menetapkan tata cara penerbitan nomor antrian apotek rawat jalan dan melakukan mappingnya dengan Resep atau Permintaan Obat Langsung yang sesuai, baik secara otomatis melalui sistem maupun secara manual oleh petugas.

## 2. Pelaksana dan Tanggung Jawab

| Pelaksana | Jenis | Tanggung jawab |
|---|---|---|
| Pasien atau Keluarga Pasien | Pengguna layanan | Mengambil nomor antrian melalui kiosk serta memindai bukti pelacakan pasien atau registrasi yang tersedia. |
| Staf Apotek | Petugas | Menemukan antrian yang belum di-mapping, memeriksa bukti, mencatat resep kertas bila ada, dan melakukan Manual Mapping. |
| Sistem Apotek | Subsistem | Mencoba melakukan Tracker Mapping antrean dengan Resep yang sudah ada, mencatat mapping-nya, dan menampilkan perkembangan setiap sumber pelayanan obat. |
| Sistem Antrian Pasien | Subsistem | Membuat entri antrian apotek, menerbitkan nomor antrian, mencatat `CreatedAt`, dan mengelola siklus antrian. |

## 3. Prasyarat

1. Staf Apotek telah masuk ke ruang kerja `Apotek Rajal` dan memiliki hak untuk melakukan mapping antrean.
2. Sesi antrian apotek rawat jalan tersedia.
3. Pasien mengambil nomor antrian melalui kiosk atau memindai bukti pelacakan pasien maupun registrasi yang sah.
4. Setiap Resep atau Permintaan Obat Langsung yang akan di-mapping mempunyai sumber yang dapat dipertanggungjawabkan atau sumber tersebut dicatat saat Manual Mapping.

## 4. Langkah Operasional

1. **Pasien atau Keluarga Pasien** mengambil nomor antrian apotek rawat jalan melalui kiosk atau memindai bukti pelacakan pasien maupun registrasi yang sah.
2. **Sistem Antrian Pasien** membuat entri antrian, menerbitkan nomor antrian, mencatat `CreatedAt`, lalu menampilkan atau mengirimkan nomor tersebut.
3. **Sistem Apotek** mencoba melakukan mapping antrean secara otomatis dengan satu atau beberapa Resep yang sudah ada berdasarkan bukti tracker atau registrasi yang diberikan. Proses ini tidak membuat Resep baru dan tidak berlaku untuk Permintaan Obat Langsung.
4. Untuk setiap Resep yang ditemukan, **Sistem Apotek** membuat catatan mapping antrean secara terpisah dan menampilkan perkembangan masing-masing Resep.
5. Jika antrian masih berstatus `Unmapped`, **Staf Apotek** dapat memanggil nomor antrian untuk keperluan administrasi. Pemanggilan ini tidak boleh mencatat `ServedAt` ataupun `DoneAt`.
6. **Pasien atau Keluarga Pasien** menunjukkan nomor antrian beserta resep, bukti registrasi, atau permintaan obat langsung yang tersedia.
7. **Staf Apotek** memilih antrian yang belum termapping dan mencari resep atau pesanan penjualan apotek yang sesuai.
8. Jika pasien membawa resep kertas, **Staf Apotek** mencatat resep tersebut sebelum meminta telaah resep. Jika pasien mengajukan permintaan obat langsung, **Staf Apotek** menilainya menurut prosedur penerimaan permintaan obat.
9. **Staf Apotek** melakukan mapping antrean secara manual dengan setiap Resep atau Permintaan Obat Langsung yang berhasil diidentifikasi.
10. **Sistem Apotek** menampilkan mapping yang berhasil dengan status `Mapped` dan tetap menyimpan setiap sumber pelayanan obat sebagai catatan tersendiri.
11. **Staf Apotek** memastikan seluruh sumber pelayanan obat tampil di bawah nomor antrian yang sama, kemudian melanjutkan pelayanan sesuai SOP profesi dan jenis penanggung biaya yang berlaku.

## 5. Penanganan Kondisi Khusus

### 5.1 Bukti tidak menemukan Resep

- **Sistem Apotek** mempertahankan antrian dengan status `Unmapped` dan tidak mengizinkan proses yang mensyaratkan mapping antrean.
- **Staf Apotek** meminta bukti tambahan dan mengulangi Manual Mapping. Staf Apotek tidak boleh membuat resep elektronik sebagai pengganti bukti yang tidak ada.
- **Staf Apotek** boleh sebaliknya mencatat Pharmacy Queue Close beserta alasan wajib. **Sistem Antrian Pasien** menetapkan Queue Entry yang masih Waiting menjadi `Withdrawn`. Penutupan tidak mencatat `ServedAt` atau `DoneAt`.

### 5.2 Permintaan obat langsung ditolak

- **Staf Apotek** tidak mencatat permintaan obat langsung ataupun membuat pesanan penjualan apotek.
- **Staf Apotek** boleh mencatat Pharmacy Queue Close beserta alasan wajib. **Sistem Antrian Pasien** menetapkan Queue Entry yang masih Waiting menjadi `Withdrawn`. State antrean tambahan tidak digunakan.

### 5.3 mapping antrean yang sudah tercatat ternyata salah

- **Staf Apotek** memilih Resep atau sumber pelayanan obat yang benar.
- **Sistem Apotek** memperbarui mapping antrean yang aktif ke sumber yang benar. Sistem tidak perlu menyimpan riwayat perubahan mapping antrean.
- Perubahan mapping antrean tidak mengubah isi Resep, hasil telaah resep, atau pesanan penjualan apotek karena data tersebut berdiri sendiri.

## 6. Kriteria Penyelesaian

1. Setiap Resep atau Permintaan Obat Langsung telah mempunyai catatan mapping tersendiri dengan nomor antrian yang sama, atau antrian tetap terlihat berstatus `Unmapped` sambil menunggu bukti tambahan, atau Pharmacy Queue Close dicatat dan Queue Entry berstatus `Withdrawn`.
2. Proses melakukan mapping antrean tidak mencatat `ServedAt` ataupun `DoneAt`.
3. Setiap sumber pelayanan obat tetap memiliki identitas resep bila ada, pesanan penjualan apotek, faktur penjualan, dan perintah penyiapan obat masing-masing.

## 7. Referensi

- [Domain Pelayanan Obat](../apotek-domain-id.md), khususnya `BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087`, dan `BR-APT-143`–`BR-APT-145`.
- [Alur Kerja Pelayanan Obat Rawat Jalan](../outpatient-apotek-workflow-id.md), `WF-APT-RJ-001`.
- [Domain Sistem Antrian Pasien](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), khususnya `BR-TRK-026`–`BR-TRK-035`, `BR-TRK-039a`, dan `BR-TRK-052`.
