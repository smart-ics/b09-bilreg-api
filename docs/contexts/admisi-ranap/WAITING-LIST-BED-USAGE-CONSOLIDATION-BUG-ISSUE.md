# ISSUE

## Metadata

ID: ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-002
Type: BUG
Status: OPEN
Title: Kegagalan Kueri Worklist Antrean Rawat Inap pada Endpoint /api/bed-usage/waiting-list Akibat Ketidakcocokan Kolom SQL (HTTP 500)

## Source

Reported By: ica-tester
Reported Date: 2026-11-10

## Description

Saat menjalankan pengujian eksekusi pada endpoint terkonsolidasi antrean rawat inap (`GET /api/bed-usage/waiting-list`), sistem mengembalikan respons HTTP 500 Internal Server Error dengan pesan galat dari basis data SQL Server:
`Invalid column name 'ArrivalSourceType'. Invalid column name 'ReferringUnitId'. Invalid column name 'ReferringUnitName'. Invalid column name 'ReferringDoctorId'. Invalid column name 'ReferringDoctorName'. Invalid column name 'ClinicalUrgency'. Invalid column name 'SpecialRequirements'. Invalid column name 'PlannedBedId'. Invalid column name 'PlannedBedName'. Invalid column name 'PreviousWaitListId'.`

Akibat kegagalan kueri ini, antarmuka konsol Admisi Rawat Inap tidak dapat memuat daftar antrean rawat inap aktif pada jalur kerja (*Daftar Tunggu Lane*), serta memblokir verifikasi kasus uji TC-01 dan TC-02 pada paket pengujian `WAITING-LIST-BED-USAGE-CONSOLIDATION-TEST-PACKAGE.md`.

## Desired Outcome

1. Pemanggilan endpoint `GET /api/bed-usage/waiting-list` (baik tanpa parameter maupun dengan filter `bangsalId` / `waitingListStatus`) merespons dengan status HTTP 200 OK dan mengembalikan daftar antrean aktif yang valid dalam format envelope JSON `{ status: "success", data: { items: [...] } }`.
2. Antarmuka konsol Admisi Rawat Inap (`DaftarTungguLane.vue`) dapat memuat dan menampilkan kartu-kartu antrean aktif secara lancar tanpa mengalami kesalahan internal server.
3. Seluruh skenario pengujian worklist antrean (TC-01 dan TC-02) dapat dieksekusi dengan hasil PASS.

## Current Situation

1. Pengguna membuka Konsol Admisi Ranap pada modul Admisi Rawat Inap (`c012_myhospital_web`).
2. Aplikasi mengirim permintaan `GET` ke `/api/bed-usage/waiting-list` untuk mengambil daftar antrean pasien.
3. Backend `b09-bilreg-api` melalui `WaitingListWorklistDal` mengeksekusi kueri SELECT pada tabel fisik `BILRG_BedWaitingList`.
4. SQL Server menolak kueri dengan pesan kesalahan bahwa kolom-kolom `ArrivalSourceType`, `ReferringUnitId`, `ReferringUnitName`, `ReferringDoctorId`, `ReferringDoctorName`, `ClinicalUrgency`, `SpecialRequirements`, `PlannedBedId`, `PlannedBedName`, dan `PreviousWaitListId` tidak ditemukan pada tabel `BILRG_BedWaitingList`.
5. API mengembalikan HTTP 500 Internal Server Error, menyebabkan pemuatan antrean di frontend gagal.

## Evidence

1. **Laporan Eksekusi Pengujian**:
   - Berkas: [WAITING-LIST-BED-USAGE-CONSOLIDATION-TEST-EXECUTION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-TEST-EXECUTION.md)
   - Bagian: `TC-01` (FAIL) dan `DEFECT-01`.
2. **Respons HTTP 500 dari Backend**:
   - URL: `GET https://localhost:7087/api/bed-usage/waiting-list`
   - Payload:
     ```json
     {
       "status": "Internal Server Error",
       "code": "500",
       "data": "Invalid column name 'ArrivalSourceType'.\r\nInvalid column name 'ReferringUnitId'.\r\nInvalid column name 'ReferringUnitName'.\r\nInvalid column name 'ReferringDoctorId'.\r\nInvalid column name 'ReferringDoctorName'.\r\nInvalid column name 'ClinicalUrgency'.\r\nInvalid column name 'SpecialRequirements'.\r\nInvalid column name 'PlannedBedId'.\r\nInvalid column name 'PlannedBedName'.\r\nInvalid column name 'PreviousWaitListId'."
     }
     ```

## Notes

- Masalah diklasifikasikan sebagai **BUG** dengan tingkat keparahan **BLOCKER** pada repositori backend `b09-bilreg-api` (`BedUsageContext.WaitingListFeature`).
- Dokumen ini disusun secara *solution-neutral* sesuai tata kelola Knowledge-Centric SDLC (`ica-issuer`).
- Rute alur kerja selanjutnya: Penugasan ke **`ica-architect`** / **`ica-analyst`** untuk **BUG-INVESTIGATION**, diikuti pembaruan rencana implementasi dan perbaikan kode.
