# ISSUE

## Metadata

ID: ISSUE-LAB-ORDER-LAB-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Implementasi Outcome OC-LAB-ORDER-LAB: Order Pemeriksaan Laboratorium

## Source

Reported By: User
Reported Date: 2026-10-15

## Description

Permintaan implementasi dan penyelarasan kapabilitas pencatatan, validasi, dan pemeliharaan permintaan pemeriksaan laboratorium (*laboratory examination order*) sebagai fakta bisnis persisten (*persisted business fact*) berdasarkan spesifikasi kanonikal [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md).

Pencatatan Order Pemeriksaan Laboratorium memungkinkan klinisi pengorder dari seluruh unit pelayanan rumah sakit (Rawat Jalan, Rawat Inap, Gawat Darurat) maupun pendaftaran langsung eksternal (`LAB-EXTERNAL`) untuk mengomunikasikan kebutuhan tes diagnostik pasien secara terstruktur kepada instalasi laboratorium. Permintaan ini merinci daftar parameter atau paket tes yang diminta beserta tingkat urgensinya (Rutin atau Cito/Emergency), dan menetapkan syarat kesiapan khusus pasien (seperti puasa atau penundaan persiapan obat).

Fakta Order Laboratorium ini berfungsi sebagai:
1. Dasar otorisasi resmi bagi instalasi laboratorium untuk mempersiapkan tabung/wadah dan melakukan pengambilan spesimen (`LAB-COLLECT` / Outcome *SampleCollection*).
2. Pemicu pembentukan pembebanan biaya tindakan laboratorium pada akun tagihan pasien (`TRK-BILLING`).
3. Acuan pemetaan (*scaffolding*) parameter uji bagi perekaman dan validasi hasil laboratorium (`LAB-RESULT` / Outcome *HasilLab*).

Telah terdapat codebase awal terkait `LabOrderFeature` pada backend `b09-bilreg-api` yang perlu ditinjau, diselaraskan, dipertahankan, atau disesuaikan (refactored) agar sepenuhnya memenuhi spesifikasi bisnis dan kondisi penerimaan pada [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md).

## Desired Outcome

Sesuai spesifikasi bisnis pada [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md):

1. **Eksistensi Catatan Permintaan Laboratorium yang Unik**:
   - Terbentuk satu catatan permintaan laboratorium unik dengan nomor referensi order (`OrderLabId` dan `OrderNo`) yang sah dan terdaftar dalam sistem.

2. **Keterikatan Konteks Pasien & Pelayanan**:
   - Order terikat secara valid pada identitas pasien (`PAS-DATSOS`) dan nomor registrasi kunjungan aktif (`RegId` pada `ADM-REG`), atau terikat pada registrasi pasien eksternal (`RegExternalId` pada `LAB-EXTERNAL`).

3. **Akuntabilitas Medis Dokter Pengorder & Unit Layanan**:
   - Teridentifikasi dokter penanggung jawab pengorder yang memiliki kredensial medis sah (`ORG-PPA`) serta unit layanan asal pengorder (`ORG-LAYANAN`).
   - Merekam diagnosis kerja / indikasi klinis serta catatan khusus pengorder.

4. **Rincian Rencana Pemeriksaan Laboratorium yang Terstruktur**:
   - Memuat minimal satu item/paket tes laboratorium yang valid dan aktif.
   - Setiap item pemeriksaan terpetakan secara lengkap ke jenis spesimen biologis, tipe tabung/wadah (*vacutainer*), prioritas item (Rutin/Cito), dan referensi tarif tindakan resmi (`TRK-TARIF`).

5. **Penetapan Tingkat Urgensi Definitif**:
   - Menetapkan tingkat urgensi pemeriksaan secara tegas: **Rutin** atau **Cito (STAT / Emergency)**.
   - Order berprioritas Cito wajib secara otomatis ditandai dengan label prioritas darurat pada daftar kerja (*worklist*) laboratorium.

6. **Penetapan Kesiapan Klinis (Deferred / Ready)**:
   - Menetapkan apakah order siap langsung diambil sampelnya (*Ready to Collect*) atau memerlukan penundaan persiapan medis pasien (*Deferred*, seperti puasa 10–12 jam, penundaan konsumsi obat, atau pemeriksaan serial terjadwal).
   - Selama berstatus Deferred, sistem mengunci alur pengambilan spesimen hingga status diaktifkan kembali.

7. **Status Siklus Hidup Awal & Kebijakan Pembatalan Non-Destructive**:
   - Order memiliki status siklus hidup awal definitif: `Requested / Ordered`, `Deferred`, atau `Cancelled`.
   - Pembatalan order hanya diizinkan sebelum pengambilan spesimen dilaksanakan (`SampleCollection` belum berstatus selesai).
   - Pembatalan bersifat *non-destructive* dan wajib merekam jejak audit (waktu pembatalan, petugas pembatal, dan alasan resmi pembatalan).

8. **Kesiapan Konsumsi Downstream**:
   - Data order laboratorium yang berstatus `Requested / Ordered` siap dikonsumsi oleh `TRK-BILLING` untuk pembebanan biaya tindakan dan oleh `LAB-COLLECT` untuk persiapan sampling.

## Current Situation

1. **Spesifikasi Kanonikal**:
   - Spesifikasi outcome telah didefinisikan pada [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md) (Versi 1.0, Draft, LastUpdated: 2026-10-10).
2. **Codebase Eksisting di `b09-bilreg-api`**:
   - Telah terdapat codebase awal pada `b09-bilreg-api` di `LabContext/LabOrderFeature`, meliputi:
     - Domain Model: [LabOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs), [LabOrderItemModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemModel.cs), [LabOrderItemComponentModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemComponentModel.cs).
     - Application Commands & Queries: `LabOrderCreateFromEmrCmd`, `LabOrderCreateExternalPatientCmd`, `LabOrderDeferCmd`, `LabOrderActivateDeferredCmd`, `LabOrderCancelCmd`, `LabOrderChargeCmd`, `LabOrderCollectSpecimenCmd`, `LabOrderReleaseCmd`, `LabOrderGetQuery`, `LabOrderWorklistQuery`.
     - Infrastructure & Data Access: `LabOrderRepo`, `LabOrderDal`, `LabOrderItemDal`, `LabOrderWorklistDal`.
     - API Controller: `LabOrderController`.
     - Basis Data SQL: tabel `BILRG_LabOrder`, `BILRG_LabOrderItem`, `BILRG_LabOrderItemComponent`.
3. **Kebutuhan Penyelarasan (Alignment Gap & Scope)**:
   - Codebase eksisting dibuat sebelum spesifikasi outcome [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md) diformalkan dan sebelum kapabilitas pendaftaran pasien eksternal (`LAB-EXTERNAL` / [OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md)) direalisasikan.
   - Perlu peninjauan menyeluruh terhadap keselarasan struktur data, validasi, dan alur kerja eksisting:
     - Penyesuaian relasi pasien eksternal: memastikan `LabOrder` mengikat `RegExternalId` resmi dari agregat `RegExternal`, bukan lagi parameter snapshot bebas.
     - Penyelarasan batasan (*boundary*) operasional: memisahkan tanggung jawab order dari pembebanan billing (`TRK-BILLING`) dan pengambilan spesimen (`LAB-COLLECT`), menjaga kedaulatan domain sesuai arsitektur terbaru.
     - Penegakan business rule dan exception handling sesuai spesifikasi kanonikal: keabsahan episode registrasi aktif, validasi item tes & tarif aktif, penandaan prioritas Cito otomatis pada worklist, penguncian sampling saat status Deferred, dan penegakan audit trail pembatalan.

## Evidence

- **Authoritative Outcome Specification**:
  - [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md) (Versi 1.0, Draft, LastUpdated: 2026-10-10)
- **Domain Canonical References**:
  - [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md) (Definisi domain Laboratory, kapabilitas `LAB-ORDER`, `LAB-COLLECT`, `LAB-RESULT`, `LAB-EXTERNAL`)
  - [outcome-capability-domain-v2.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/outcome-capability-domain-v2.md)
  - [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md) (`PAS-DATSOS`)
  - [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md) (`ORG-PPA`, `ORG-LAYANAN`)
  - [03-ADMISSION-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/03-ADMISSION-DOMAIN.md) (`ADM-REG`)
  - [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md) (`TRK-TARIF`, `TRK-BILLING`)
- **Existing Backend Codebase References (`b09-bilreg-api`)**:
  - Domain Model: [Bilreg.Domain/LabContext/LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature)
  - Application Layer: [Bilreg.Application/LabContext/LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature)
  - Infrastructure Layer: [Bilreg.Infrastructure/LabContext/LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature)
  - API Controller: [Bilreg.Api/Controllers/LabContext/LabOrderFeature/LabOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabOrderFeature/LabOrderController.cs)
  - Database Scripts: [Bilreg.SqlDb/LabContext/LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature)
  - Unit & Integration Tests: [Bilreg.Test/LabContext/LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature)
- **Related Context Documentation**:
  - [OC-LAB-REG-EXTERNAL-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-ISSUE.md)
  - [OC-LAB-REG-EXTERNAL-IMPLEMENTATION-PLAN.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-IMPLEMENTATION-PLAN.md)
  - [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
  - [lab-workflow.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-workflow.md)

## Notes

- Dokumen ini adalah artefak intake yang bersifat solusi-netral (*solution-neutral*), berfokus mendokumentasikan APA yang diminta dan kondisi saat ini tanpa mendikte solusi teknis atau rincian desain arsitektur.
- Codebase eksisting di `LabOrderFeature` dapat dipertahankan, dimodifikasi, atau direfaktor sesuai hasil analisis keselarasan (*feasibility assessment*) dan target arsitektur teknis.
- Ruang lingkup di luar batas (*Out of Scope*):
  - **Pengambilan Fisik Spesimen & Flebotomi**: Prosedur penusukan vena, penempelan barcode tabung, dan verifikasi volume spesimen (`LAB-COLLECT` / Outcome *SampleCollection*).
  - **Perekaman Nilai Parameter & Validasi Medis Hasil**: Pengisian nilai hasil tes, validasi teknis, dan verifikasi dokter spesialis patologi klinik (`LAB-RESULT` / Outcome *HasilLab*).
  - **Pendaftaran Mandiri Pasien Eksternal**: Penerbitan registrasi eksternal dan identitas sementara (`LAB-EXTERNAL` / Outcome *RegExternal*).
  - **Penghitungan Nilai Rupiah Tarif & Transaksi Kasir**: Penghitungan tagihan, diskon, dan pencatatan struk pembayaran kasir (`TRK-BILLING`, `TRK-TARIF`, `TRK-KASIR`).
  - **Keputusan Klinis Pemilihan Indikasi Pemeriksaan**: Keputusan dokter mengenai tes yang dibutuhkan pasien (merupakan domain klinis dan EMR).
- Alur kerja berikutnya untuk CHANGE-REQUEST ini diteruskan ke peran **ica-analyst** untuk tahapan **Discovery** dan **FEASIBILITY-ASSESSMENT** (Analisis Kelayakan).
