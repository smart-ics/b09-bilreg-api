# ISSUE

## Metadata

ID: ISSUE-IGD-EMR-TIMELINE-001
Type: BUG
Status: OPEN
Title: Data Assesment IGD Triage Tidak Tampil di Timeline EMR Setelah Link Registrasi (RegId)

## Source

Reported By: User
Reported Date: 2026-09-28

## Description

Assesment yang dibuat melalui IgdVisit Triage berhasil disimpan ke `SMASS_Assesment`. Setelah `IgdVisit` dihubungkan (*link*) dengan `RegId` pendaftaran, data Assesment tersebut sudah dapat tampil pada menu Catalog di EMR. Namun, data Assesment tersebut belum muncul pada Timeline EMR.

## Desired Outcome

Setelah `IgdVisit` berhasil di-*link* dengan `RegId`, data Assesment IGD Triage yang bersangkutan dapat tampil secara otomatis pada Timeline EMR (`GET /api/Label/TimeLine`) selain tampil di Catalog EMR.

## Current Situation

- Assesment dari `IgdVisitTriage` berhasil di-generate dan tersimpan di `SMASS_Assesment`.
- Saat awal pembuatan Triage IGD, data `RegId`, `PasienId`, dan `PasienName` belum terisi karena pasien belum melalui proses registrasi.
- Saat proses registrasi/linking melalui `{BilregApi} PATCH api/IgdVisit/{id}/register`, `IgdVisit` berhasil terhubung dengan `RegId`.
- Di EMR, data Assesment hasil generate dari IGD sudah muncul di Catalog, namun data Assesment tersebut tidak ditemukan/belum tampil pada Timeline EMR.

## Evidence

- Response endpoint `{Emr20Api} GET /api/Label/TimeLine` tidak memuat data Assesment IGD terkait setelah proses registrasi.
- Data tercatat ada di tabel `SMASS_Assesment` dan muncul di Catalog EMR.

## Notes

- Catatan & dugaan pelapor:
  - Tampilan Timeline EMR mengandalkan data label (`EMREC_label`). Diduga data Assesment tersebut belum digenerate ke entitas label.
  - Terdapat endpoint yang diketahui untuk generate label data Assesment: `{Emr20Api} POST /api/LabelV2/AddSmass` dengan spesifikasi payload:
    ```json
    {
      "AssesmentId": "string",
      "LayananId": "string",
      "PaperId": "string",
      "PaperName": "string",
      "RegId": "string",
      "UserrId": "string"
    }
    ```
  - Pada konfigurasi `appsettings.json` di Bilreg, section `"Emr"` saat ini baru mendefinisikan `"BaseApiUrl"` untuk `Emr25Api` dan belum terdapat konfigurasi URL untuk `Emr20Api` (keduanya adalah API yang terpisah).
- Informasi teknis di atas dicatat sebagai bahan masukan awal untuk investigasi dan tidak membatasi keputusan investigasi/arsitektur pada tahapan selanjutnya.
