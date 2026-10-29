# ISSUE

## Metadata

ID: ISSUE-LAB-REG-EXTERNAL-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Implementasi Outcome OC-LAB-REG-EXTERNAL: Registrasi Pasien Eksternal Laboratorium

## Source

Reported By: User
Reported Date: 2026-10-13

## Description

Permintaan implementasi kapabilitas dan alur kerja pencatatan resmi pendaftaran langsung pasien luar laboratorium (*direct external lab registration*) sebagai fakta bisnis persisten (*persisted business fact*) berdasarkan spesifikasi [OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md).

Pencatatan Registrasi Pasien Eksternal Laboratorium memungkinkan pasien yang datang langsung (*walk-in* atas inisiatif sendiri / Atas Permintaan Sendiri - APS) maupun pasien yang membawa surat rujukan dari fasilitas kesehatan luar (dokter praktik mandiri, puskesmas, klinik pratama, atau rumah sakit lain) untuk segera mendapatkan pelayanan diagnostik laboratorium tanpa harus mengantre dan melalui prosedur registrasi kunjungan rumah sakit reguler di loket admisi umum (*Admission / ADM*).

Fungsionalitas yang diminta mencakup pencatatan registrasi eksternal dengan nomor identitas operasional transaksi laboratorium yang bersifat sekali pakai (*one-time use only*), tidak membentuk atau memelihara master rekam medis permanen di Domain Pasien (`PAS-DATSOS`), membentuk akun penagihan langsung ke kasir laboratorium/Tata Rekening (`TRK-BILLING` dan `TRK-KASIR`), serta menjadi dasar otorisasi bagi pembuatan permintaan tes laboratorium khusus pasien luar (`LAB-ORDER`).

## Desired Outcome

Sesuai spesifikasi bisnis pada [OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md):

1. **Pencatatan Registrasi Eksternal Unik**:
   - Pendaftaran langsung pasien eksternal laboratorium tercatat secara resmi sebagai fakta bisnis persisten (`Direct External Lab Registration exists`) dengan identifier unik `RegExternalId`.

2. **Penerbitan Nomor Rekam Medis Operasional Sekali Pakai (*One-Time Identity*)**:
   - Diterbitkan nomor identitas/rekam medis sementara dengan format khusus (misal: penomoran khusus eksternal) yang ditandai secara tegas sebagai nomor sekali pakai (*one-time use only*).
   - Nomor identitas eksternal dilarang digunakan kembali untuk episode kunjungan rawat inap, rawat jalan, atau gawat darurat rumah sakit reguler.

3. **Perekaman Data Identitas Pasien Luar (Demografi Terbatas)**:
   - Data sosial dan demografi pasien luar terekam lengkap: Nama Lengkap, NIK/KTP (opsional jika tersedia), Tanggal Lahir atau Estimasi Usia, Jenis Kelamin (wajib untuk penetapan rentang nilai normal analitik), Nomor Telepon/WhatsApp aktif, dan Alamat Tinggal domisili singkat.

4. **Kejelasan Kategori Asal Rujukan Luar**:
   - Teridentifikasi asal kedatangan: **Atas Permintaan Sendiri (APS)** atau **Rujukan Fasilitas Luar** (dilengkapi nama dokter pengirim, instansi/klinik perujuk luar, dan diagnosis/indikasi klinis rujukan).

5. **Keterikatan Akun Penagihan Kasir**:
   - Terbentuk akun penagihan langsung pada sistem penagihan Tata Rekening (`TRK-BILLING`) untuk memproses pembayaran kasir (`TRK-KASIR`) dengan skema penjaminan/pembayaran yang valid sebelum pengambilan spesimen atau penyerahan hasil (*Cash-First Principle*).

6. **Kemandirian dari Master Pasien RS (*Master Isolation*)**:
   - Terkonfirmasi bahwa data registrasi beroperasi secara terisolasi dalam lingkup transaksional laboratorium dan tidak melakukan pembaruan atau pembuatan entitas pada master data pasien permanen rumah sakit (`PAS-DATSOS`).

7. **Status Siklus Hidup dan Audit Pembatalan**:
   - Registrasi memiliki status siklus hidup yang jelas: `Active`, `Completed`, atau `Cancelled`.
   - Pembatalan registrasi eksternal hanya dapat dieksekusi jika belum ada pembayaran yang diselesaikan di kasir dan belum ada spesimen yang diambil pada order laboratorium terkait.
   - Pembatalan bersifat *non-destructive* dengan merekam jejak audit (waktu, staf pembatal, dan alasan resmi pembatalan).

8. **Kesiapan Sebagai Konteks Downstream Order Lab**:
   - Entitas registrasi eksternal aktif dapat dipilih dan digunakan sebagai konteks pasien yang sah pada pembuatan order pemeriksaan laboratorium (`LAB-ORDER`).

## Current Situation

1. Pada codebase [b09-bilreg-api](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases), telah terdapat use case [LabOrderCreateExternalPatientCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateExternalPatientCmd.cs) yang mengizinkan pembuatan order lab langsung dengan snapshot pasien eksternal ([PatientSnapshotType.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/PatientSnapshotType.cs)).
2. Namun, implementasi yang ada saat ini:
   - Belum memiliki kapabilitas atau agregat terpisah `LAB-EXTERNAL` (`RegExternal`) yang memformalkan pencatatan entitas registrasi eksternal persisten.
   - Penerimaan parameter `RegId` dan `PatientId` pada `LabOrderCreateExternalPatientCmd` bersifat opsional/bebas tanpa penerbitan nomor identitas operasional sekali pakai (`ExternalMrNo`).
   - Belum memiliki pencatatan data identitas sosial lengkap pasien luar (NIK, nomor kontak aktif telepon/WA, alamat tinggal domisili singkat).
   - Belum mendukung perekaman asal kedatangan dan rujukan luar (kategori APS vs Rujukan Luar dengan dokter perujuk dan instansi pengirim).
   - Belum mengintegrasikan pembentukan akun penagihan kasir langsung ke domain Tata Rekening (`TRK-BILLING` dan `TRK-KASIR`).
   - Belum memiliki pengelolaan siklus hidup registrasi (`Active`, `Completed`, `Cancelled`) beserta validasi pembatalan terkait status pembayaran kasir dan penanganan spesimen.

## Evidence

- **Authoritative Outcome Specification**:
  - [OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md) (Versi 1.0, Draft, LastUpdated: 2026-10-10)
- **Domain Canonical Reference**:
  - [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md) (Definisi domain Laboratory dan kapabilitas `LAB-EXTERNAL`)
- **Existing Codebase References**:
  - [LabOrderCreateExternalPatientCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateExternalPatientCmd.cs)
  - [LabOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs) (Metode `CreateExternal`)
  - [PatientSnapshotType.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/PatientSnapshotType.cs)
  - [LabOrderSourceEnum.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderSourceEnum.cs)

## Notes

- Dokumen ini adalah artefak intake yang bersifat solusi-netral (*solution-neutral*), mendeskripsikan APA yang diminta dan kondisi saat ini tanpa mendikte rincian desain arsitektur teknis atau implementasi kode.
- Ruang lingkup di luar batas (*Out of Scope*):
  - **Pendaftaran Kunjungan Rumah Sakit Reguler**: Registrasi rawat jalan, rawat inap, atau IGD reguler (wewenang `ADM-REG` / Outcome Registrasi).
  - **Pengelolaan Master Identitas Pasien Permanen**: Pemeliharaan data sosial pasien seumur hidup (wewenang `PAS-DATSOS`).
  - **Penerbitan Order Tes Laboratorium**: Pemilihan daftar item dan penanganan analitik tes lab (wewenang `LAB-ORDER` / Outcome OrderLab).
  - **Penerimaan Fisik Uang Kasir & Struk**: Transaksi kasir penerimaan pembayaran tunai/debit/qris (wewenang `TRK-KASIR`).
- Alur kerja SDLC berikutnya untuk CHANGE-REQUEST ini diteruskan ke peran **ica-analyst** untuk tahapan **Discovery** dan **FEASIBILITY-ASSESSMENT**.
