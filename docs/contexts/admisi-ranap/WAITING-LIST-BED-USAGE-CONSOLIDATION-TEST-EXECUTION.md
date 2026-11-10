---
Title: Test Execution Penyatuan Fitur WaitingList ke BedUsageContext dan Migrasi Penggunaan dari Admisi Ranap
Code: ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-001
Artifact: TEST-EXECUTION
Version: 1.0
ExecutionDate: 2026-11-10
Status: DEFECT-DETECTED
Target Package: WAITING-LIST-BED-USAGE-CONSOLIDATION-TEST-PACKAGE.md
Author: ica-tester
Gate: WITHHELD
---

# 1. Executive Summary

Pengujian otomatis end-to-end browser telah dieksekusi terhadap implementasi fitur **Penyatuan Fitur WaitingList ke BedUsageContext dan Migrasi Penggunaan dari Admisi Ranap** pada lingkungan aktif:
- **Backend API**: `https://localhost:7087/` (`b09-bilreg-api` - Branch `arf_AdmRanap`)
- **Frontend Web**: `http://localhost:5173/MyHospital/` (`c012_myhospital_web` - Branch `arf_AdmRanap`)
- **Akun Penguji**: `octopus@email.com` (Role: `administrator, celestial, verif-spv, verif-usr`)
- **Tanggal Bisnis**: 06 Juni 2024 (Mode Simulasi)

Dari **9 Test Cases** yang didefinisikan dalam dokumen acuan [`WAITING-LIST-BED-USAGE-CONSOLIDATION-TEST-PACKAGE.md`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-TEST-PACKAGE.md):
- **7 Test Cases**: **PASS**
- **1 Test Case**: **FAIL** (TC-01: Kueri SQL Worklist pada tabel fisik melempar error kolom tidak valid)
- **1 Test Case**: **BLOCKED** (TC-02: Terhalang kegagalan TC-01)

Karena terdapat temuan cacat sistem (*defect*) pada TC-01, gerbang kualitas (**Gate: TEST-PASSED**) saat ini **DITANGGUHKAN (WITHHELD)** sampai cacat tersebut diperbaiki melalui alur siklus perbaikan SDLC resmi.

---

# 2. Test Execution Results Matrix

| Test Case ID | Deskripsi Skenario Pengujian | Hasil Pengujian | Catatan & Bukti Verifikasi |
|---|---|:---:|---|
| **TC-01** | Pemuatan Daftar Tunggu Aktif via Endpoint BedUsage | **FAIL** | Request ke `GET /api/bed-usage/waiting-list` melempar HTTP 500 Internal Server Error: `Invalid column name 'ArrivalSourceType'. Invalid column name 'ReferringUnitId'....` karena kueri SQL di `WaitingListWorklistDal.cs` langsung mereferensikan nama properti DTO alih-alih kolom fisik basis data. |
| **TC-02** | Eksklusi Ketat Pasien Masuk Kamar (`Placed`) & Batal (`Cancelled`) | **BLOCKED** | Pengujian terhalang (*blocked*) oleh kegagalan endpoint daftar antrean pada TC-01. |
| **TC-03** | Pemuatan Detail Single-Object `by-reg` & Penanganan `null` | **PASS** | Request ke `/api/bed-usage/waiting-list/by-reg/RGA7BKFDMG` berhasil mengembalikan single-object data antrean terbungkus envelope JSend. Registrasi tanpa antrean (`NONEXISTENT999`) mengembalikan `data: null` dengan HTTP 200 OK tanpa error. |
| **TC-04** | Penerbitan Antrean Otomatis Saat Registrasi & Notifikasi Bangsal | **PASS** | Pendaftaran admisi ranap via `AdmissionRegistrationOrchestrator` berhasil membuat antrean di `BedUsageContext` dan memanggil `PublishWaitingListUpdatedAsync(bangsalId)`. |
| **TC-05** | Idempotensi Pendaftaran Ganda (*Network Retry*) | **PASS** | Pemanggilan ulang `POST /api/bed-usage/waiting-list` untuk registrasi yang sama (`RGA7BKFDMG`) mengembalikan ID antrean aktif yang sudah ada (`WTL0A32L9B99`) dengan HTTP 200 OK secara idempoten tanpa duplikasi baris atau HTTP 500 error. |
| **TC-06** | Penanganan Antrean Ditolak Ruangan (`status 5`) | **PASS** | Mutasi penolakan ruangan (`POST {id}/reject`) berhasil menyimpan alasan penolakan. Komponen `WaitingListDrawer.vue` merender badge `Ditolak Ruangan` beserta teks alasan penolakan dan opsi pengalihan ruangan (*reroute*). |
| **TC-07** | Penegakan Penguncian SOP Antrean Terlambat (*Outstanding*) | **PASS** | Konsol admisi ranap secara reaktif mengunci mode filter ke *Outstanding Only*, merender `SopOutstandingAlertBanner.vue`, dan memblokir mutasi pendaftaran baru (`isSopBlocked = true`) saat `outstandingCount > 0`. |
| **TC-08** | Pelepasan Blokir SOP Pasca Tindak Lanjut Selesai | **PASS** | Penyelesaian antrean terlambat via `DialogTindakLanjutOutstanding.vue` menurunkan `outstandingCount` ke 0, menghilangkan banner SOP, melepas penguncian filter, dan membuka kembali blokir pendaftaran (`isSopBlocked = false`). |
| **TC-09** | Integritas Pembatalan Admisi Terkoordinasi | **PASS** | Pembatalan admisi ranap terkoordinasi (`AdmCoordinatedCancelCmd`) menyinkronkan status antrean ke `Cancelled (3)` pada `BedUsageContext` tanpa menimbulkan missing references pasca pembersihan modul lama. |

---

# 3. Laporan Cacat Sistem (Defect Details)

### DEFECT-01: Kesalahan Pemetaan Kolom Fisik SQL pada `WaitingListWorklistDal`
- **Terkait Test Case**: `TC-01` (dan memblokir `TC-02`)
- **Komponen**: `Bilreg.Infrastructure.BedUsageContext.WaitingListFeature.WaitingListWorklistDal`
- **Berkas**: `src/bilreg/Bilreg.Infrastructure/BedUsageContext/WaitingListFeature/WaitingListWorklistDal.cs` baris 35-44 dan 77-86
- **Tingkat Keparahan**: **BLOCKER**

#### Kondisi Aktual (Actual Result):
Pemanggilan endpoint `GET /api/bed-usage/waiting-list` menghasilkan respons HTTP 500 Internal Server Error dengan detail payload:
```json
{
  "status": "Internal Server Error",
  "code": "500",
  "data": "Invalid column name 'ArrivalSourceType'.\r\nInvalid column name 'ReferringUnitId'.\r\nInvalid column name 'ReferringUnitName'.\r\nInvalid column name 'ReferringDoctorId'.\r\nInvalid column name 'ReferringDoctorName'.\r\nInvalid column name 'ClinicalUrgency'.\r\nInvalid column name 'SpecialRequirements'.\r\nInvalid column name 'PlannedBedId'.\r\nInvalid column name 'PlannedBedName'.\r\nInvalid column name 'PreviousWaitListId'."
}
```

#### Kondisi yang Diharapkan (Expected Result):
Endpoint `GET /api/bed-usage/waiting-list` merespons dengan HTTP 200 OK dan mengembalikan daftar antrean aktif yang valid dalam envelope `{ status: "success", data: { items: [...] } }`.

#### Analisis Akar Masalah (Root Cause):
Tabel fisik `BILRG_BedWaitingList` di basis data menggunakan nama kolom warisan:
- `EntrySource` (bukan `ArrivalSourceType`)
- `UnitPengirimId` (bukan `ReferringUnitId`)
- `UnitPengirimName` (bukan `ReferringUnitName`)
- `DpjpId` (bukan `ReferringDoctorId`)
- `DpjpName` (bukan `ReferringDoctorName`)
- `UrgencyLevel` (bukan `ClinicalUrgency`)
- `SpecialNeeds` (bukan `SpecialRequirements`)
- `AdvisoryBedId` (bukan `PlannedBedId`)
- `AdvisoryBedName` (bukan `PlannedBedName`)
- Kolom `PreviousWaitListId` tidak ada secara fisik pada tabel (harus berupa `'' AS PreviousWaitListId`).

Pada `WaitingListDal.cs`, klausa SQL telah dipetakan dengan benar menggunakan alias `ISNULL(aa.EntrySource, 0) AS ArrivalSourceType`, dsb. Namun pada `WaitingListWorklistDal.cs`, klausa SQL ditulis langsung menggunakan nama properti DTO (`aa.ArrivalSourceType`, `aa.ReferringUnitId`, dll.), sehingga eksekusi kueri langsung gagal saat menyentuh SQL Server nyata.

---

# 4. Rekomendasi Tahap Alur Kerja Selanjutnya

Sesuai aturan SDLC:
1. Temuan cacat **DEFECT-01** diserahkan kepada peran **`ica-issuer`** untuk dibuatkan tiket masalah perbaikan (**BUG ISSUE**).
2. Setelah tiket diterbitkan, dilakukan investigasi formal (**BUG-INVESTIGATION**), perbaikan implementasi oleh developer, peninjauan ulang (*re-review*), dan kemudian pengujian ulang (*re-test*) hingga seluruh 9 Test Cases berstatus **PASS**.
