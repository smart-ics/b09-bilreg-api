# ISSUE

## Metadata

ID: ISSUE-LAB-SAMPLE-COLLECTION-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Implementasi Outcome OC-LAB-SAMPLE-COLLECTION: Pengambilan Spesimen Laboratorium (Cakupan HIS)

## Source

Reported By: User
Reported Date: 2026-10-16

## Description

Permintaan penyelarasan dan penyederhanaan kapabilitas pencatatan bukti serah-terima pengambilan spesimen laboratorium (*Sample Collection*) yang sesuai untuk cakupan sistem informasi rumah sakit (Hospital Information System / HIS / SIMRS), berbasis pada *codebase* eksisting di [`Bilreg.Domain/LabContext/LabOrderFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature).

Pada konteks HIS (berbeda dengan *Laboratory Information System* / LIS yang mengelola detail teknis tabung analyzer, kalibrasi instrumen, dan indeks hemolisis/ikterik/lipemik), fokus fungsionalitas pencatatan spesimen adalah:
1. Memvalidasi dan mencatat fakta bisnis bahwa materi biologis pasien untuk order laboratorium telah berhasil diambil.
2. Merekam waktu riil sampling (*CollectedDate*), identitas petugas pelaksana/pencatat (*CollectedUserId*), dan catatan bebas jika terdapat kondisi khusus (*CollectionNote*).
3. Memperbarui status siklus hidup order laboratorium menjadi `Collected`, yang mengunci rincian pemeriksaan dan menandai dimulainya penghitungan waktu tunggu pelayanan (*Turn Around Time* / TAT).
4. Menyediakan panduan persiapan tabung (*vacutainer grouping*) bagi petugas flebotomis melalui antarmuka layar sebelum penusukan dilakukan.
5. Mendukung fleksibilitas pelayanan rumah sakit di mana pengambilan spesimen dapat dilakukan dari status `Charged` (standar Rawat Jalan & Pasien Eksternal) maupun langsung dari status `Ordered` (untuk Rawat Inap & IGD yang tindakan billingnya diproses paralel).

## Desired Outcome

1. **Pencatatan Fakta Pengambilan Spesimen pada LabOrder**:
   - Bukti pengambilan spesimen terekam langsung pada agregat `LabOrderModel` melalui properti `CollectionInfo` (`CollectedDate`, `CollectedUserId`, `CollectionNote`).
   - Status order diperbarui secara definitif menjadi `LabOrderStatusEnum.Collected`.
   - Tidak memerlukan pembentukan agregat baru, tabel baru, atau penomoran barcode fisik tersendiri di tingkat HIS.

2. **Fleksibilitas Status Kelayakan Order (Eligibility)**:
   - Pengambilan spesimen diizinkan apabila order berstatus `Charged` (Rawat Jalan / Pasien Eksternal) ATAU berstatus `Ordered` (Rawat Inap / IGD).
   - Upaya pencatatan spesimen pada order yang masih berstatus penundaan (`Deferred`) atau telah dibatalkan (`Cancelled`) ditolak dengan pesan validasi yang jelas.

3. **Penguncian Immutability Order Pasca-Sampling**:
   - Begitu order berstatus `Collected`, rincian pemeriksaan terkunci dan order tidak dapat dibatalkan melalui prosedur `Cancel` reguler.
   - Apabila pemeriksaan terpaksa dihentikan pasca-sampling (misal sampel tumpah/pasien pulang paksa), penutupan dilakukan melalui alur terminasi operasional (`Terminate`).

4. **Panduan Persiapan Tabung Flebotomis (Vacutainer Grouping)**:
   - Sistem menyediakan kueri persiapan pengambilan sampel (`LabCollectionPreparationQuery`) yang mengelompokkan jenis tes berdasarkan jenis tabung (*tube type*) dan spesimen, sehingga petugas mengetahui jumlah tabung yang harus disiapkan.

5. **Penanganan Masalah Spesimen Minimalis**:
   - Jika terjadi kegagalan penusukan saat tindakan, status order tidak diubah (tetap `Ordered`/`Charged`).
   - Catatan kondisi spesimen atau kegagalan sampling cukup dituangkan dalam field catatan bebas `CollectionNote`.

## Current Situation

1. **Codebase Eksisting di `b09-bilreg-api`**:
   - Pada [`Bilreg.Domain/LabContext/LabOrderFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature), sistem telah memiliki seluruh fondasi yang dibutuhkan untuk model HIS ini:
     - Value Object [`CollectionInfoType.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/CollectionInfoType.cs): `(DateTime CollectedDate, string CollectedUserId, string CollectionNote)`.
     - Enum [`VacutainerTypeEnum.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/VacutainerTypeEnum.cs): `Edta`, `Serum`, `Citrate`, `Heparin`.
     - Logika Domain pada [`LabOrderModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs): Metode `CollectSpecimen`, properti `CollectionInfo`, status `LabOrderStatusEnum.Collected`, proteksi `Cancel` pasca-sampling, dan metode `Terminate`.
     - Use Case [`LabOrderCollectSpecimenCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCollectSpecimenCmd.cs) & Handler.
     - Kueri Panduan Tabung [`LabCollectionPreparationQuery.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabCollectionPreparationQuery.cs) & [`LabCollectionPreparationDal.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabCollectionPreparationDal.cs).
     - Skema Basis Data [`BILRG_LabOrder.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrder.sql) yang telah memiliki kolom `CollectedDate`, `CollectedUserId`, dan `CollectionNote`.
2. **Kesenjangan (Gap) yang Perlu Disesuaikan**:
   - Metode `LabOrderModel.CollectSpecimen` saat ini memiliki validasi ketat: `if (LabOrderStatus != LabOrderStatusEnum.Charged) throw ...`. Validasi ini perlu dilonggarkan agar mengizinkan order berstatus `Ordered` (khusus Rawat Inap dan IGD).

## Evidence

- **Authoritative Outcome Specification**:
  - [OC-LAB-SAMPLE-COLLECTION.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-SAMPLE-COLLECTION.md)
  - [OC-08-04-SAMPLE-COLLECTION-OUTCOME.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/_review/OC-08-04-SAMPLE-COLLECTION-OUTCOME.md) (Outcome review sebelumnya yang selaras dengan cakupan HIS)
- **Domain Canonical References**:
  - [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md) (`LAB-COLLECT`, `LAB-ORDER`)
- **Existing Backend Codebase References (`b09-bilreg-api`)**:
  - Domain Model: [`LabOrderModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs) (baris 472-498 untuk `CollectSpecimen`, baris 625-630 untuk proteksi cancel)
  - Value Object: [`CollectionInfoType.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/CollectionInfoType.cs)
  - Application Layer: [`LabOrderCollectSpecimenCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCollectSpecimenCmd.cs), [`LabCollectionPreparationQuery.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabCollectionPreparationQuery.cs)
  - Infrastructure DAL: [`LabCollectionPreparationDal.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabCollectionPreparationDal.cs)
  - Database Script: [`BILRG_LabOrder.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrder.sql)
  - Unit Tests: [`LabOrderCollectSpecimenHandlerTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCollectSpecimenHandlerTest.cs), [`LabCollectionPreparationDalTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabCollectionPreparationDalTest.cs)

## Notes

- Dokumen ini adalah intake yang diselaraskan khusus untuk batasan fungsionalitas HIS.
- Ruang lingkup di luar batas (*Out of Scope*):
  - **Manajemen Tabung & Barcode Fisik Level LIS**: Pembuatan barcode fisik per tabung, tracking rak tabung (*tube rack*), dan interfacing analyzer laboratorium (merupakan wewenang sistem LIS eksternal).
  - **Penilaian Kualitas Analitik Spesimen**: Pengukuran indeks hemolisis, ikterik, dan lipemik pada mesin analyzer (merupakan wewenang instrumen LIS).
  - **Penerbitan Order & Pembebanan Biaya**: Wewenang `LAB-ORDER` dan `TRK-BILLING`.
- Alur kerja berikutnya diteruskan ke peran **ica-analyst** untuk memperbarui **FEATURE** dan **FEASIBILITY-ASSESSMENT**.
