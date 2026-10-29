---
Title: Pengambilan Spesimen Laboratorium (Cakupan HIS)
Code: OC-LAB-SAMPLE-COLLECTION
Artifact: FEATURE
Version: 1.1
LastUpdated: 2026-10-16
---

# 1. Purpose

Rumah sakit membutuhkan kapabilitas operasional pada HIS (Hospital Information System / SIMRS) untuk mencatat dan memvalidasi fakta bisnis serah-terima fisik materi biologis pasien (*sample collection*) pada order laboratorium yang aktif.

Pada konteks HIS (berbeda dengan LIS murni yang mengelola mikrobiologi/rak tabung/antarmuka alat analitik), fungsionalitas ini memastikan:
1. Rumah sakit mendokumentasikan bahwa spesimen pasien telah resmi diambil dan berada dalam penguasaan instalasi laboratorium.
2. Merekam waktu riil pengambilan (*CollectedDate*), identitas staf pelaksana/pencatat (*CollectedUserId*), dan catatan operasional (*CollectionNote*).
3. Mengunci rincian pemeriksaan pada order laboratorium terkait agar tidak dapat diubah atau dibatalkan sembarangan setelah spesimen diambil.
4. Menandai awal penghitungan waktu tunggu pelayanan laboratorium (*Turn Around Time* / TAT) dari sisi operasional rumah sakit.
5. Menyediakan panduan kebutuhan tabung (*vacutainer grouping*) bagi petugas sebelum penusukan dilakukan.

# 2. Business Outcome

Pencatatan pengambilan spesimen biologis pasien terekam secara persisten pada agregat `LabOrderModel` (`CollectionInfo` terisi dan status order menjadi `Collected`), mengunci item pemeriksaan dari perubahan sepihak, dan siap dilanjutkan ke proses perekaman hasil analisis laboratorium (`LAB-RESULT`).

# 3. Participating Domains

Berdasarkan arsitektur fungsional sistem MyHosWeb:

- **Laboratory (`LAB`)**: Primary Domain. Mengelola penerimaan permintaan sampling, kueri panduan tabung vacutainer, pemutakhiran status order menjadi `Collected`, dan pencatatan informasi sampling (`CollectionInfo`) pada agregat `LabOrder`.
- **Pasien (`PAS`)**: Contributing Domain. Menyediakan data demografi pasien (`PAS-DATSOS`) untuk verifikasi identitas pasien sebelum sampling.
- **Organisasi (`ORG`)**: Contributing Domain. Menyediakan identitas petugas pelaksana sampling dan unit layanan asal/lokasi sampling.
- **Pelayanan Klinis (`RNA` / `RJL` / `IGD`)**: Contributing Domains. Titik pelayanan tempat tindakan sampling dilaksanakan.

# 4. Trigger

Petugas flebotomi atau perawat melakukan tindakan pengambilan spesimen pasien berdasarkan order laboratorium yang telah terbit dan siap disampling.

# 5. Preconditions

1. Order laboratorium berstatus `Charged` (untuk Rawat Jalan dan Pasien Eksternal) ATAU berstatus `Ordered` (untuk Rawat Inap dan IGD).
2. Order tidak sedang dalam masa penundaan puasa/persiapan medis (`Deferred`).
3. Order tidak dalam status dibatalkan (`Cancelled`).
4. Identitas pasien telah diverifikasi secara positif (*positive patient identification*).

# 6. Operational Flow

1. **Verifikasi Identitas & Panduan Tabung**:
   - Petugas memverifikasi identitas pasien dengan mencocokkan data order terhadap gelang/identitas fisik pasien.
   - Petugas memeriksa kebutuhan jenis dan jumlah tabung yang disyaratkan melalui kueri panduan tabung (`LabCollectionPreparationQuery`).
2. **Pelaksanaan Tindakan Pengambilan Spesimen**:
   - Petugas melakukan penusukan atau penampungan sampel sesuai SOP klinis.
3. **Perekaman Pengambilan Spesimen**:
   - Petugas menginput konfirmasi sampling ke sistem via `LabOrderCollectSpecimenCmd`:
     - `OrderId`
     - `UserId` (identitas staf pengambil/pencatat)
     - `CollectedDate` (waktu riil pelaksanaan)
     - `CollectionNote` (opsional: catatan jika ada kondisi khusus)
4. **Persistensi & Penguncian Status Order**:
   - Sistem memvalidasi kelayakan status order.
   - Sistem memperbarui status order menjadi `Collected` dan menyimpan `CollectionInfo`.
   - Order terkunci dari pembatalan reguler (`Cancel`) dan rincian tes tidak dapat diubah.
5. **Penanganan Kegagalan / Kendala Sampling (Kondisional)**:
   - Jika pengambilan spesimen gagal saat penusukan (vena tidak dapat, pasien menolak), status order tidak diubah menjadi `Collected` (tetap menunggu).
   - Jika spesimen yang sudah diambil terbukti rusak atau pemeriksaan terpaksa dihentikan permanen pasca-sampling, penutupan order dilakukan melalui alur terminasi operasional (`Terminate`).

# 7. Domain Orchestration

1. **Pelayanan Klinis (`RNA` / `RJL` / `IGD`) ↔ Laboratory (`LAB-ORDER`)**:
   - Order yang berstatus `Ordered` / `Charged` menjadi dasar otorisasi tindakan sampling.
2. **Laboratory (`LAB-COLLECT` / `LAB-ORDER`) ↔ Pasien (`PAS-DATSOS`)**:
   - Memverifikasi kesesuaian identitas pasien sebelum tindakan flebotomi.
3. **Laboratory (`LAB-ORDER`) ↔ Laboratory (`LAB-RESULT`)**:
   - Order yang berstatus `Collected` muncul pada daftar kerja meja laboratorium dan siap diinputkan hasilnya oleh analis.

# 8. Constraints

1. **Prinsip Immutability Order Pasca-Sampling**:
   - Order yang telah berstatus `Collected` tidak dapat dibatalkan via alur pembatalan biasa (`Cancel`).
2. **Integritas Waktu Sampling Riil**:
   - Waktu yang dicatat merefleksikan tanggal dan jam penusukan fisik riil sebagai baseline Turn Around Time (TAT).
3. **Pencegahan Sampling pada Order Tidak Siap**:
   - Order yang masih `Deferred` atau `Cancelled` dilarang keras dicatat pengambilan sampelnya.
4. **Batas Kewenangan HIS**:
   - HIS tidak mengelola rack analyzer, barcode per tabung, atau pembacaan instrumen LIS.

# 9. Exceptions

- **EX-01: Order Berstatus Deferred**: Sistem menolak pencatatan sampling dengan pesan bahwa pasien masih dalam masa persiapan puasa/klinis.
- **EX-02: Order Berstatus Cancelled**: Sistem menolak pencatatan sampling karena order sudah dibatalkan sebelumnya.
- **EX-03: Upaya Pembatalan Pasca-Sampling**: Sistem menolak eksekusi `Cancel` pada order yang telah berstatus `Collected`.

# 10. Acceptance Criteria

- **AC-01**: Sistem berhasil mencatat data pengambilan spesimen (`CollectionInfo`: `CollectedDate`, `CollectedUserId`, `CollectionNote`) pada agregat `LabOrderModel`.
- **AC-02**: Status `LabOrderStatus` berhasil diperbarui menjadi `Collected` secara persisten pada basis data.
- **AC-03**: Kueri panduan tabung `LabCollectionPreparationQuery` berhasil menyajikan pengelompokan jenis dan jumlah tabung vacutainer yang harus disiapkan.
- **AC-04**: Pencatatan pengambilan spesimen diizinkan untuk order berstatus `Charged` serta `Ordered` (Ranap/IGD).
- **AC-05**: Upaya pencatatan spesimen pada order yang berstatus `Deferred` atau `Cancelled` berhasil dicegah oleh guard validation.
- **AC-06**: Order yang berstatus `Collected` secara otomatis dilindungi dari eksekusi pembatalan biasa (`Cancel`).
