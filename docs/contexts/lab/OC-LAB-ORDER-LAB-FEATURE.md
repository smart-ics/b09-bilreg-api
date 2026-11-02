---
Title: Order Pemeriksaan Laboratorium
Code: OC-LAB-ORDER-LAB
Artifact: FEATURE
Version: 1.1
LastUpdated: 2026-10-15
---

# 1. Purpose

Rumah sakit membutuhkan kapabilitas terstandar, resmi, dan persisten untuk mencatat, memvalidasi, dan mengelola permintaan pemeriksaan laboratorium (*laboratory examination order*). Kapabilitas ini memungkinkan klinisi perujuk (Dokter Penanggung Jawab Pelayanan / DPJP) dari seluruh instalasi pelayanan klinis (Rawat Jalan, Rawat Inap, Gawat Darurat) maupun loket pendaftaran mandiri pasien luar (`LAB-EXTERNAL`) untuk mengomunikasikan instruksi pemeriksaan penunjang diagnostik secara terstruktur ke instalasi laboratorium. Hal ini memastikan parameter tes, urgensi pemeriksaan (Rutin vs Cito), persyaratan persiapan klinis pasien (seperti puasa atau penundaan obat), catatan klinis (`ClinicalNote`), serta dasar pembebanan biaya terekam dengan akuntabilitas medis yang sah.

# 2. Business Outcome

Permintaan pemeriksaan laboratorium atas nama pasien tercatat secara resmi sebagai fakta bisnis persisten (*Laboratory Examination Order exists*) dengan identitas unik (`OrderLabId` dan `OrderNo`), terikat pada konteks pasien dan registrasi kunjungan aktif, memuat dokter perujuk, layanan perujuk, catatan klinis terpadu (`ClinicalNote`), daftar item tes terstruktur yang dipetakan ke kebutuhan spesimen biologis dan tarif resmi, serta siap digunakan sebagai dasar pembebanan tarif (`TRK-BILLING`) dan persiapan pengambilan spesimen (`LAB-COLLECT`).

# 3. Participating Domains

Berdasarkan arsitektur fungsional MyHosWeb:

- **Laboratory (`LAB`)**: Primary Domain (Pemilik Utama). Bertanggung jawab atas penerimaan permintaan, validasi katalog tes laboratorium terhadap definisi uji dan kebutuhan vacutainer, pemeliharaan status operasional order (`LAB-ORDER`), serta penyediaan acuan kebutuhan spesimen bagi flebotomi (`LAB-COLLECT`).
- **Pasien (`PAS`)**: Contributing Domain. Menyediakan snapshot data sosial dan identitas demografi resmi pasien melalui `PAS-DATSOS` (Nomor RM, nama lengkap, jenis kelamin, tanggal lahir/usia).
- **Admission (`ADM`)**: Contributing Domain. Menyediakan verifikasi status keaktifan episode registrasi kunjungan rumah sakit (`RegId` pada `ADM-REG`) sebagai prasyarat otorisasi order klinis internal.
- **Organisasi (`ORG`)**: Contributing Domain. Menyediakan master kredensial klinis dokter perujuk (`ORG-PPA`) dan master layanan perujuk (`ORG-LAYANAN`).
- **Pelayanan Klinis (`RJL` / `RNA` / `IGD`)**: Contributing Domains. Menyediakan konteks episode pelayanan klinis tempat permintaan laboratorium diinstruksikan (`RJL-KONSUL`, `RNA-TRANSFER`, `IGD-VISIT`).
- **Tata Rekening (`TRK`)**: Contributing Domain. Menyediakan referensi tarif tindakan laboratorium resmi (`TRK-TARIF`) dan menerima pembebanan biaya tindakan laboratorium (`TRK-BILLING`).

# 4. Trigger

1. **Inisiasi Klinis Internal (EMR)**: Dokter penanggung jawab pasien (DPJP / dokter perujuk) atau perawat atas instruksi dokter pada unit Rawat Jalan, Rawat Inap, atau Gawat Darurat memutuskan perlunya pemeriksaan penunjang laboratorium dan menginput daftar tes pada lembar order klinis.
2. **Inisiasi Pasien Eksternal**: Petugas pendaftaran laboratorium membuat order pemeriksaan laboratorium untuk pasien rujukan luar atau walk-in yang telah terdaftar melalui kapabilitas pendaftaran pasien eksternal (`LAB-EXTERNAL`).

# 5. Preconditions

1. Pasien memiliki nomor registrasi kunjungan yang sah dan aktif (`RegId` pada `ADM-REG` untuk pasien rumah sakit atau `RegExternalId` pada `LAB-EXTERNAL` untuk pasien luar).
2. Episode perawatan pasien belum ditutup secara administratif (*not discharged / not cancelled*).
3. Untuk order internal RS: Dokter perujuk terdaftar aktif dan memiliki kewenangan klinis sah pada master tenaga medis (`ORG-PPA`). Untuk order eksternal: Data perujuk diturunkan dari informasi rujukan luar (`ReferralInfo`) atau pendaftaran mandiri (APS) tanpa validasi `ORG-PPA`.
4. Untuk order internal RS: Layanan perujuk terdaftar aktif pada master organisasi (`ORG-LAYANAN`). Untuk order eksternal: Layanan perujuk mengacu pada faskes perujuk luar atau loket mandiri laboratorium.
5. Setiap item pemeriksaan laboratorium yang dipilih berstatus aktif dalam katalog uji laboratorium (`LAB-ORDER`) dan memiliki pemetaan tarif yang sah di `TRK-TARIF`.

# 6. Operational Flow

1. **Pemilihan Pasien & Verifikasi Kunjungan**:
   - Pengorder mengidentifikasi pasien berdasarkan nomor registrasi aktif (`RegId` atau `RegExternalId`).
   - Sistem memvalidasi bahwa episode kunjungan berstatus aktif dan belum ditutup.
2. **Perekaman Tanggung Jawab Medis & Konteks Klinis**:
   - Pengorder menetapkan dokter perujuk (`DokterPerujuk`), layanan perujuk (`LayananPerujuk`), dan catatan klinis terpadu (`ClinicalNote` yang merangkum indikasi klinis dan catatan khusus dokter).
3. **Penyusunan Rincian Pemeriksaan & Pemetaan Spesimen**:
   - Pengorder memilih satu atau beberapa item/paket pemeriksaan laboratorium.
   - Sistem memvalidasi keaktifan definisi uji dan tarif tindakan (`TRK-TARIF`).
   - Sistem secara otomatis memetakan setiap item tes ke kebutuhan spesimen biologis (darah vena, urine, feses, dll.), jenis tabung (*vacutainer*), dan jumlah tabung yang disyaratkan.
4. **Penetapan Urgensi & Kesiapan Klinis**:
   - Pengorder menetapkan tingkat urgensi: **Rutin** atau **Cito (STAT / Emergency)**.
   - Pengorder menetapkan status kesiapan klinis:
     - *Ready to Collect*: Pasien siap langsung diambil sampelnya.
     - *Deferred*: Pasien memerlukan persiapan khusus (misal puasa 10–12 jam, penundaan minum obat). Pengorder mengisi alasan penundaan dan estimasi waktu pengambilan spesimen.
5. **Persistensi Order & Penerbitan Nomor Referensi**:
   - Sistem memvalidasi kelengkapan seluruh parameter wajib.
   - Sistem menerbitkan nomor transaksi unik (`OrderLabId` dan `OrderNo`).
   - Sistem menyimpan order dengan status awal:
     - `Ordered` (jika ready), atau
     - `Deferred` (jika memerlukan penundaan).
   - Jika order berstatus Cito, sistem secara otomatis memberikan penanda darurat pada daftar kerja (*worklist*) operasional laboratorium.
6. **Aktivasi dari Status Deferred (Kondisional)**:
   - Ketika masa persiapan pasien terpenuhi, petugas laboratorium mengaktifkan order dari status `Deferred` menjadi `Ordered`, merekam waktu aktivasi dan nomor registrasi pelaksanaan yang aktif.
7. **Alur Pembatalan Order (Kondisional)**:
   - Dokter perujuk atau petugas berwenang dapat mengajukan pembatalan order sebelum spesimen diambil (`SampleCollection` belum selesai).
   - Pengguna wajib memasukkan alasan resmi pembatalan.
   - Sistem memverifikasi ketiadaan sampel yang telah diambil, memperbarui status menjadi `Cancelled`, dan merekam jejak audit pembatalan (waktu, pelaku, dan alasan).

# 7. Domain Orchestration

1. **Pelayanan Klinis / EMR (`RJL` / `RNA` / `IGD`) → Laboratory (`LAB`)**:
   - Modul klinis mengirimkan data instruksi tes laboratorium beserta identitas dokter perujuk, layanan perujuk, dan catatan klinis (`ClinicalNote`) ke `LAB-ORDER`.
2. **Laboratory (`LAB`) → Admission (`ADM`) & Pasien (`PAS`)**:
   - `LAB-ORDER` memverifikasi status keaktifan registrasi ke `ADM-REG` dan menyematkan snapshot data demografi pasien dari `PAS-DATSOS`.
3. **Laboratory (`LAB`) Internal Orchestration (`LAB-ORDER` ↔ `LAB-EXTERNAL`)**:
   - Untuk pasien luar, `LAB-ORDER` mengikat `RegExternalId` resmi yang diterbitkan oleh agregat `RegExternal` sebagai konteks kunjungan yang sah.
4. **Laboratory (`LAB`) → Tata Rekening (`TRK`)**:
   - `LAB-ORDER` memvalidasi relasi item tes terhadap `TRK-TARIF`.
   - Data rincian pemeriksaan yang dipersistensi siap dikonsumsi oleh `TRK-BILLING` untuk pembebanan biaya tindakan laboratorium.
5. **Laboratory (`LAB`) Internal Orchestration (`LAB-ORDER` → `LAB-COLLECT`)**:
   - Order yang berstatus `Ordered` tampil pada daftar kerja laboratorium dan menjadi dasar otorisasi bagi `LAB-COLLECT` untuk mempersiapkan tabung/wadah dan melaksanakan pengambilan spesimen biologis pasien.

# 8. Constraints

1. **Keterikatan Tunggal pada Episode Kunjungan**:
   - Setiap entitas `OrderLab` harus terikat secara spesifik pada tepat satu nomor registrasi kunjungan aktif (`RegId` atau `RegExternalId`).
2. **Kemandirian Operasional dari Catatan EMR**:
   - `OrderLab` mengelola orkestrasi operasional pemeriksaan laboratorium. Rekam medis lengkap, asesmen SOAP, dan resep obat tetap dikelola secara independen di domain EMR / Rawat Jalan / Rawat Inap.
3. **Immutability Rincian Pemeriksaan Pasca-Sampling**:
   - Apabila spesimen telah diambil (`SampleCollection` selesai), rincian item tes dalam order laboratorium tersebut terkunci dan tidak boleh diubah atau dihapus. Permintaan tes tambahan harus dibuat melalui order baru.
4. **Non-Destructive Cancellation**:
   - Pembatalan order tidak menghapus data secara fisik dari basis data, melainkan memperbarui status menjadi `Cancelled` dan mencatat alasan pembatalan dalam jejak audit permanen.
5. **Integritas Penundaan Persiapan (Deferred Lock)**:
   - Selara berstatus `Deferred`, sistem mengunci pengambilan spesimen hingga status diaktifkan kembali menjadi aktif/ready.

# 9. Exceptions

- **EX-01: Registrasi Kunjungan Tidak Aktif / Ditutup**:
  - *Pemicu*: Pengorder membuat order laboratorium menggunakan nomor registrasi yang tidak ditemukan, dibatalkan, atau status episode perawatannya sudah ditutup (*discharged*).
  - *Perilaku*: Sistem menolak pembuatan order dan menginformasikan bahwa pasien harus memiliki status kunjungan aktif.
- **EX-02: Item Pemeriksaan Tidak Memiliki Definisi / Tarif Aktif**:
  - *Pemicu*: Item tes laboratorium yang dipilih tidak aktif di katalog atau pemetaan tarifnya di `TRK-TARIF` tidak ditemukan.
  - *Perilaku*: Sistem menolak penyimpanan baris item tersebut dan meminta administrator memperbarui katalog tes.
- **EX-03: Pembatalan pada Order yang Sudah Diambil Sampelnya**:
  - *Pemicu*: Pengguna berupaya membatalkan order laboratorium padahal spesimen telah diambil (`SampleCollection` sudah terbentuk/selesai).
  - *Perilaku*: Sistem menolak pembatalan order secara langsung. Pembatalan hanya dapat dilakukan melalui prosedur penolakan spesimen atau terminasi resmi dengan otorisasi khusus.
- **EX-04: Pembatalan Tanpa Alasan Resmi**:
  - *Pemicu*: Pengguna mengubah status order menjadi `Cancelled` tanpa mengisi keterangan alasan pembatalan.
  - *Perilaku*: Sistem memblokir aksi pembatalan dan mewajibkan pengisian alasan pembatalan minimal.
- **EX-05: Duplikasi Pemeriksaan Identik dalam Waktu Singkat**:
  - *Pemicu*: Sistem mendeteksi adanya order aktif untuk jenis tes laboratorium yang sama pada pasien yang sama dalam kurun waktu 24 jam terakhir.
  - *Perilaku*: Sistem memberikan peringatan konfirmasi (*warning dialog*) mengenai potensi tes ganda guna mencegah pemborosan biaya atau flebotomi berulang yang tidak perlu.

# 10. Acceptance Criteria

- **AC-01**: Sistem berhasil mencatat permintaan laboratorium baru dengan identifier unik `OrderLabId`, nomor order `OrderNo`, dan menghubungkannya dengan nomor registrasi kunjungan aktif yang sah (`RegId` atau `RegExternalId`).
- **AC-02**: Catatan order memuat secara lengkap dokter perujuk (`ORG-PPA`), layanan perujuk (`ORG-LAYANAN`), catatan klinis terpadu (`ClinicalNote`), snapshot identitas pasien (`PAS-DATSOS`), dan minimal satu item pemeriksaan laboratorium terstruktur.
- **AC-03**: Setiap item pemeriksaan terpetakan dengan benar ke jenis spesimen biologis yang disyaratkan serta tipe tabung vacutainer yang sesuai berdasarkan definisi katalog tes.
- **AC-04**: Penetapan prioritas 'Cito' berhasil direkam pada header dan item order, serta secara otomatis menandai order dengan label prioritas darurat pada daftar kerja (*worklist*) instalasi laboratorium.
- **AC-05**: Order yang memerlukan puasa/persiapan medis berhasil dicatat dengan status 'Deferred' beserta rincian instruksi persiapan dan target waktu sampling.
- **AC-06**: Upaya pembuatan order pada kunjungan yang sudah ditutup atau dengan item tes non-aktif berhasil dicegah dengan pesan validasi yang jelas.
- **AC-07**: Pembatalan order sebelum pengambilan sampel berhasil mengubah status menjadi 'Cancelled' dan merekam waktu, identitas petugas, serta alasan pembatalan dalam jejak audit.
- **AC-08**: Order laboratorium yang tersimpan berstatus 'Ordered' dapat diakses pada worklist operasional laboratorium dan siap digunakan oleh kapabilitas `LAB-COLLECT` untuk tahap pengambilan spesimen.
