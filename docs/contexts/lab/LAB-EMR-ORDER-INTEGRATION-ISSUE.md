# ISSUE

## Metadata

ID: ISSUE-LAB-EMR-ORDER-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Integrasi dan Penyelarasan Order Laboratorium dari EMR ke Order Lab Bilreg

## Source

Reported By: User
Reported Date: 2026-10-31

## Description

Permintaan implementasi dan penyelarasan integrasi pengiriman data order pemeriksaan laboratorium dari Electronic Medical Record (EMR) ke sistem Order Lab Bilreg (`b09-bilreg-api` - Laboratory Workflow Feature / LWF).

Klinisi (dokter/PPA) yang bertugas di unit pelayanan (Rawat Jalan, Rawat Inap, Maupun Gawat Darurat) menginput permintaan pemeriksaan laboratorium melalui modul Medical Chart di EMR (`orderLab.js`). Data order pemeriksaan laboratorium yang dibuat pada modul EMR tersebut perlu terintegrasi dan selaras secara resmi dengan sistem Order Lab Bilreg agar menjadi entitas order yang diakui dan dapat diproses pada alur kerja instalasi laboratorium rumah sakit (pencatatan order, persiapan dan pengambilan spesimen, penagihan biaya tindakan/billing, serta penerusan ke analyzer/LIS via OWARE) tanpa memerlukan entri ulang manual oleh petugas laboratorium.

## Desired Outcome

1. **Penerimaan & Integrasi Order Lab dari EMR ke Bilreg**:
   - Permintaan pemeriksaan laboratorium yang dibuat dan disimpan pada modul Medical Chart EMR dapat diterima dan dicatat secara otomatis menjadi entitas Order Lab yang sah di Bilreg (`b09-bilreg-api`).
   - Order yang terintegrasi memiliki identitas pengenal yang unik dan valid (`OrderNo`, `OrderId`, dan referensi `EmrOrderId`).

2. **Penyelarasan Data Klinis & Parameter Pemeriksaan**:
   - Seluruh atribut penting dari order EMR (identitas pasien, nomor registrasi kunjungan aktif, dokter perujuk/pengorder, unit layanan asal, catatan klinis, tanggal rencana periksa, tingkat urgensi Rutin/Cito, serta daftar item tes/tarif) terpetakan dengan tepat sesuai kontrak dan aturan validasi Order Lab Bilreg.

3. **Korelasi & Penelusuran Dua Arah (Correlation & Traceability)**:
   - Terjalin korelasi yang jelas antara order di EMR (`EmrOrderId` / `ChartID`) dengan entitas order di Bilreg (`OrderNo` / `OrderId`), memungkinkan pelacakan status pemeriksaan oleh klinisi di EMR.

4. **Kesiapan Konsumsi Alur Downstream**:
   - Order EMR yang berhasil masuk ke Bilreg siap dikonsumsi langsung oleh sub-alur laboratorium lanjutan (persiapan sampling/`SampleCollection`, pembebanan biaya/`Billing`, dan transmisi ke middleware LIS/`OWARE`).

5. **Penanganan Perubahan & Pembatalan Order (Cancellation & Update)**:
   - Mekanisme pembatalan atau perubahan order laboratorium dari EMR sebelum spesimen diambil dapat tersinkronisasi ke Bilreg secara aman (*non-destructive*) dengan jejak audit yang valid.

## Current Situation

1. **Frontend EMR (`c001_emr2_frontend`)**:
   - Modul antarmuka pemesanan laboratorium dokter diimplementasikan pada [orderLab.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/orderLab.js).
   - Pengumpulan dan penyimpanan order ke dalam rekam medis dilakukan pada [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js) di mana order lab dikemas ke dalam properti `ListOrder` dengan `OrderTypeID = "LB"`.
   - Terdapat pemanggilan langsung ke OWARE middleware (`sendOrderLIS` menuju `POST Order/lab/chart`), namun belum terhubung ke endpoint Order Lab Bilreg secara terstandar.

2. **Backend Bilreg (`b09-bilreg-api`)**:
   - Telah tersedia spesifikasi dan kontrak integrasi EMR pada [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md) (§3 EMR Integration: `POST /api/LabContext/LabOrderFeature/fromEmr` dan `GET /api/LabContext/LabOrderFeature/byEmrOrderId/{emrOrderId}`).
   - Implementasi endpoint controller telah tersedia pada [LabOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabOrderFeature/LabOrderController.cs) dan use case handler [LabOrderCreateFromEmrCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs).
   - Namun, integrasi menyeluruh (*end-to-end*) antara pemesanan di EMR (`orderLab.js` / backend EMR) dengan API Order Lab Bilreg belum tersambung, dan keselarasan skema payload data (seperti format item tarif, penanganan duplikasi tes 24 jam, dan mapping dokter/layanan) belum divalidasi secara operasional.

## Evidence

- **Frontend EMR**:
  - [orderLab.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/orderLab.js) — Logika UI dan data model order laboratorium di Medical Chart (`saveObjectLab`, `setOrderLabFromChart`, struktur `orderLaborat.ListItem`).
  - [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js) — Pengumpulan order lab (`listOrderLaborat`) ke dalam payload chart note dan fungsi `sendOrderLIS`.
- **Backend Bilreg**:
  - [LabOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabOrderFeature/LabOrderController.cs) — Endpoint `fromEmr`, `from-emr`, dan `byEmrOrderId`.
  - [LabOrderCreateFromEmrCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs) — Command handler pembuatan order lab dari input EMR (`LabOrderTarifItemInput`).
  - [LabOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs) — Domain aggregate method `CreateFromEmr`.
- **Spesifikasi & Dokumen Terkait**:
  - [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md) (§3 EMR Integration)
  - [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md) (Spesifikasi Outcome Order Pemeriksaan Laboratorium)
  - [OC-LAB-ORDER-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ISSUE.md)

## Notes

- Intake ini berfokus pada kebutuhan fungsional dan operasional untuk mengintegrasikan dan menyelaraskan pembuatan order lab dari modul EMR ke backend Bilreg.
- ISSUE ini bersifat *solution-neutral*: tidak mendahului analisis apakah transmisi data akan dilakukan secara langsung dari frontend/backend EMR via synchronous REST API, melalui middleware messaging/RabbitMQ, atau melalui `b22-emid-emr-middleware`.
- Keputusan desain teknis, analisis dampak, pemetaan detail payload, dan strategi mitigasi akan dianalisis lebih lanjut pada tahap `FEASIBILITY-ASSESSMENT` oleh peran Analyst (`ica-analyst`).
