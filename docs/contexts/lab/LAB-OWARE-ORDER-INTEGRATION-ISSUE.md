# ISSUE

## Metadata

ID: ISSUE-LAB-OWARE-ORDER-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Integrasi Pengiriman Order Pemeriksaan Laboratorium ke OWARE (Middleware LIS/PACS)

## Source

Reported By: User
Reported Date: 2026-10-29

## Description

Permintaan implementasi integrasi pengiriman data order pemeriksaan laboratorium dari sistem Laboratory Workflow Feature (LWF) pada backend `b09-bilreg-api` ke middleware OWARE (`a046_oware_ordermiddlewareapi`) untuk diteruskan ke Laboratory Information System (LIS) dan instrumen/analyzer eksternal laboratorium rumah sakit.

Pengiriman order laboratorium ke OWARE ditujukan untuk mengotomatisasi transmisi permintaan pemeriksaan diagnostik tanpa input manual ulang pada analyzer atau sistem LIS eksternal, dengan tetap mempertahankan LWF sebagai sumber kebenaran operasional (*operational source of truth*).

## Desired Outcome

1. **Pengiriman Order Laboratorium ke OWARE Terhubung Nyata**:
   - Order laboratorium yang telah diverifikasi dan siap dikerjakan pada LWF dapat ditransmisikan secara andal ke service OWARE (`a046_oware_ordermiddlewareapi`).
2. **Karakter Asynchronous & Isolasi Kegagalan**:
   - Proses pengiriman ke OWARE berlangsung secara asynchronous (melalui antrean/queue atau background worker) sehingga kelambatan atau kegagalan koneksi ke OWARE/LIS tidak memblokir alur pelayanan klinis utama di LWF.
3. **Visibilitas Status Pengiriman Terpantau**:
   - Status pengiriman ke OWARE (`OwareStatus`: `Pending`, `Sent`, `Failed`) terperbarui dan tercatat pada data order laboratorium serta antrean integrasi, lengkap dengan log pesan kesalahan apabila terjadi kendala pengiriman.
4. **Mekanisme Retry**:
   - Tersedia mekanisme pengulangan pengiriman (*retry*) baik secara otomatis maupun manual untuk menangani order yang berstatus gagal (*Failed*) akibat kendala jaringan atau ketidaksiapan service penerima.

## Current Situation

1. Konsep integrasi outbound LWF ke OWARE telah didokumentasikan pada [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md) dan [lab-workflow.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-workflow.md), menetapkan OWARE sebagai middleware bridge dan komunikasi bersifat asynchronous.
2. Pada `b09-bilreg-api` (fitur `LabOwareFeature`), telah tersedia rancangan antrean outbound (`LabOwareOutboundQueueModel`), controller antrean, dan processor antrean, namun implementasi adapter pengiriman riil [LabOwareIntegration.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabOwareIntegration.cs) saat ini masih berstatus simulasi/mock in-memory dan belum melakukan pemanggilan HTTP/network ke OWARE API.
3. Pada `a046_oware_ordermiddlewareapi`, endpoint order lab yang ada saat ini (`POST /api/order/lab`) masih terikat pada model lama berbasis `TindakanId` dan belum disesuaikan untuk menerima payload order kanonikal dari `b09-bilreg-api`.

## Evidence

- **Spesifikasi & Panduan Integrasi**:
  - [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md) (§6 OWR Integration, §7 OWR Outbound Flow, §11 Integration Retry Strategy)
  - [lab-workflow.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-workflow.md) (§14 OWARE Status, §15 Integration Architecture)
  - [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md) (§21 OWARE Integration, §22 OWARE Communication, §23 OWARE Status)
- **Implementasi Komponen Outbound Eksisting (`b09-bilreg-api`)**:
  - [LabOwareIntegration.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabOwareIntegration.cs)
  - [LabOwareQueueProcessor.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOwareFeature/LabOwareQueueProcessor.cs)
  - [LabOwarePayloadBuilder.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOwareFeature/LabOwarePayloadBuilder.cs)
  - [LabOwareController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabOwareFeature/LabOwareController.cs)
- **Endpoint Order Eksisting (`a046_oware_ordermiddlewareapi`)**:
  - [OrderController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/OrderController.cs)

## Notes

- Intake ini berfokus pada pengiriman outbound order laboratorium dari LWF ke OWARE. Alur inbound hasil pemeriksaan (*result retrieval / ingestion*) dari OWARE ke LWF merupakan subjek terpisah yang dapat ditangani dalam siklus tersendiri.
- Detail spesifikasi kontrak payload, konfigurasi koneksi antar-service, mekanisme pemicu antrean (event/trigger point), dan penanganan kegagalan akan ditelaah pada tahap analisis berikutnya.
