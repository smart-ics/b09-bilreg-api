# ISSUE

## Metadata

ID: ISSUE-ADMISI-PASIEN-SASET-BUG-001
Type: BUG
Status: OPEN
Title: Dialog Konfirmasi Persetujuan Satu Sehat Tidak Muncul untuk Jaminan Non-BPJS Saat Registrasi Admisi

## Source

Reported By: Tester Admisi
Reported Date: 2026-10-08
Originating Test Execution: [PASIEN-SATU-SEHAT-APPROVAL-TEST-EXECUTION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-TEST-EXECUTION.md#L85-L113) (TC-SS-05 / DEF-001)
Related Feature: PASIEN-SATU-SEHAT-APPROVAL
Severity: MAJOR

## Description

Pada saat melakukan pendaftaran rawat jalan (Admisi) untuk pasien dengan jaminan Non-BPJS (Umum / Tunai dengan tipeJaminanId: "00000") yang belum disetujui Satu Sehat (indikator visual `#SatuSehat` abu-abu), pop-up modal "Konfirmasi Persetujuan Satu Sehat" tidak muncul ke layar petugas saat tombol "Simpan Registrasi" ditekan. Sistem langsung memproses dan menyimpan data pendaftaran tanpa meminta konfirmasi persetujuan (*informed consent*) dari petugas/pasien.

## Desired Outcome

Sistem mendeteksi bahwa penjamin yang dipilih adalah Non-BPJS dan menampilkan pop-up dialog/modal "Konfirmasi Persetujuan Satu Sehat" (dengan opsi persetujuan "Setuju" dan "Batal") sebelum proses simpan registrasi dijalankan, sehingga alur persetujuan upload Satu Sehat untuk pasien Non-BPJS dapat terpenuhi sesuai spesifikasi.

## Current Situation

Sistem melewati (*bypasses*) dialog konfirmasi persetujuan Satu Sehat ketika tombol "Simpan Registrasi" ditekan untuk pasien jaminan Non-BPJS yang belum disetujui, dan langsung mengeksekusi penyimpanan registrasi.

## Evidence

- **Observasi Pengujian**: 
  - Menu: Pendaftaran Rawat Jalan (Admisi).
  - Status Pasien: Belum disetujui Satu Sehat (`#SatuSehat` berwarna abu-abu).
  - Penjamin: Umum / Tunai (`tipeJaminanId` = `"00000"`).
  - Aksi: Klik tombol "Simpan Registrasi".
  - Hasil Pengujian: [PASIEN-SATU-SEHAT-APPROVAL-TEST-EXECUTION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-TEST-EXECUTION.md#L85-L113) mencatat hasil `FAIL` pada TC-SS-05 dan defect record `DEF-001`.
- **Inspeksi Tab Network Browser**:
  - Request URL: `http://dev.smart-ics.com:8888/JknTrustedLink/api/GrupJaminan/map?tipeJaminanId=00000`
  - HTTP Status: `400 Bad Request`
  - Response Body:
    ```json
    {
      "status": "Data Not Found",
      "code": "400",
      "data": "Data Mapping Group Jaminan tidak ditemukan"
    }
    ```

## Notes

- Masalah teridentifikasi pada penanganan respons pemetaan grup jaminan eksternal (`GrupJaminan/map`), di mana status HTTP 400 dengan pesan "Data Not Found" (indikasi jaminan tidak dipetakan ke BPJS) menyebabkan evaluasi jaminan Non-BPJS tidak terpenuhi dan mekanisme dialog terlewati.
- Analisis mendalam dan perbaikan teknis akan dilakukan pada tahap Bug Investigation dan Architecture Update.
