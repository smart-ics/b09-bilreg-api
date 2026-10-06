# ISSUE

## Metadata

ID: ISSUE-BILREG-ADMISI-EVENT-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Publikasi Event Registrasi Pasien (Rawat Jalan, Rawat Inap, Rawat Darurat) ke RabbitMQ

## Source

Reported By: User
Reported Date: 2026-10-03

## Description

Pada proses penyimpanan registrasi pasien di Bilreg untuk layanan Rawat Jalan, Rawat Inap, dan Rawat Darurat, sistem belum melakukan raise/publish event notifikasi ke message broker (RabbitMQ). Sehubungan dengan rencana penghentian penggunaan komponen `BipubApi` pada `project_MyHospitalWeb`, fungsi publikasi event registrasi diminta untuk dijalankan langsung oleh sistem Bilreg.

## Desired Outcome

1. Setiap kali proses registrasi berhasil disimpan pada:
   - Rawat Jalan (Walk-In)
   - Rawat Inap (pemrosesan permintaan opname / reservation)
   - Rawat Darurat (IGD)
   sistem secara otomatis mempublikasikan event notifikasi registrasi ke RabbitMQ.
2. Layanan atau sistem konsumen downstream dapat menerima event notifikasi registrasi dari message broker tanpa memerlukan perantara `BipubApi`.

## Current Situation

1. Handler registrasi rawat jalan (`RegJalanCreateHandler` via [RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs)) telah menangani penyimpanan data registrasi, antrian, billing, jurnal, dan outbox antrian EMR, namun belum melakukan raise event ke RabbitMQ.
2. Handler registrasi rawat inap ([AdmProcessOpnameRequestCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmProcessOpnameRequestCmd.cs) dan [AdmProcessReservationCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmProcessReservationCmd.cs) melalui `AdmissionRegistrationOrchestrator`) telah menyimpan admission, registrasi ranap, dan audit log, namun belum melakukan raise event ke RabbitMQ.
3. Handler registrasi rawat darurat (`RegDaruratCreateHandler` via [RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs)) telah menangani registrasi IGD, tindakan awal, billing, jurnal, dan outbox antrian EMR, namun belum melakukan raise event ke RabbitMQ.
4. Fungsi publish event registrasi sebelumnya terdapat pada `BipubApi` (contoh: `RegRajalCreatedNotifEvent` dan `RegRanapCreatedNotifEvent`), namun `BipubApi` tidak akan digunakan lagi untuk `project_MyHospitalWeb`.

## Evidence

- Use cases registrasi target di Bilreg:
  - [RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs)
  - [AdmProcessOpnameRequestCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmProcessOpnameRequestCmd.cs)
  - [AdmProcessReservationCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmProcessReservationCmd.cs)
  - [RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs)
- Implementasi referensi sebelumnya di Bipub:
  - [RegRajalCreatedNotifEventCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/a045_bipub_billingpublicapi/Bipub.Application/AdmisiContext/RegAgg/RegJalan/RegRajalCreatedNotifEventCommand.cs)
  - [RegInapCreatedNotifEventCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/a045_bipub_billingpublicapi/Bipub.Application/AdmisiContext/RegAgg/RegInap/RegInapCreatedNotifEventCommand.cs)
  - `Bipub.Infrastructure.Helpers.EventPublisher` (menggunakan MassTransit `IBus.Publish`)

## Notes

- `BipubApi` dinyatakan deprecated / tidak akan digunakan lagi dalam ekosistem `project_MyHospitalWeb`.
- Pola raise event sebelumnya pada Bipub memanfaatkan pesan kontrak dari package `MyHospital.MsgContract.Billing.AdmisiEvents` (seperti `RegRajalCreatedNotifEvent`, `RegRanapCreatedNotifEvent`).
- Dokumen ini murni intake kebutuhan (solution-neutral). Detail teknis arsitektur (definisi kontrak pesan event untuk IGD, mekanisme publishing direct atau transactional outbox, registrasi RabbitMQ bus/MassTransit di Bilreg, serta penanganan error publishing) diserahkan ke tahapan analisis dan arsitektur lebih lanjut.
