# BUG-INVESTIGATION

## Context

- Issue: [WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-ISSUE.md) (`ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-002`)
- Problem Summary: Endpoint `GET /api/bed-usage/waiting-list` menghasilkan HTTP 500 Internal Server Error saat dieksekusi terhadap database SQL Server karena kueri SQL pada `WaitingListWorklistDal` langsung menggunakan nama properti DTO alih-alih memetakan kolom fisik tabel `BILRG_BedWaitingList`.

## Current State

Endpoint `GET /api/bed-usage/waiting-list` melempar HTTP 500 Internal Server Error dengan pesan dari SQL Server:
```text
Invalid column name 'ArrivalSourceType'.
Invalid column name 'ReferringUnitId'.
Invalid column name 'ReferringUnitName'.
Invalid column name 'ReferringDoctorId'.
Invalid column name 'ReferringDoctorName'.
Invalid column name 'ClinicalUrgency'.
Invalid column name 'SpecialRequirements'.
Invalid column name 'PlannedBedId'.
Invalid column name 'PlannedBedName'.
Invalid column name 'PreviousWaitListId'.
```
Akibatnya, antarmuka konsol Admisi Rawat Inap pada jalur *Daftar Tunggu* (`DaftarTungguLane.vue`) tidak dapat memuat kartu antrean aktif, serta menyebabkan kasus uji TC-01 berstatus FAIL dan TC-02 berstatus BLOCKED pada pengujian end-to-end.

## Problem Analysis

1. **Observasi Kode & Perbandingan DAL**:
   - Pada `WaitingListDal.cs` (yang terbukti berjalan normal untuk query single record pada TC-03), proyeksi kolom SELECT memetakan nama kolom fisik basis data menggunakan alias yang sesuai ke DTO:
     ```sql
     ISNULL(aa.EntrySource, 0) AS ArrivalSourceType,
     ISNULL(aa.UnitPengirimId, '') AS ReferringUnitId,
     ISNULL(aa.UnitPengirimName, '') AS ReferringUnitName,
     ISNULL(aa.DpjpId, '') AS ReferringDoctorId,
     ISNULL(aa.DpjpName, '') AS ReferringDoctorName,
     ISNULL(aa.UrgencyLevel, 0) AS ClinicalUrgency,
     ISNULL(aa.SpecialNeeds, '') AS SpecialRequirements,
     ISNULL(aa.AdvisoryBedId, '') AS PlannedBedId,
     ISNULL(aa.AdvisoryBedName, '') AS PlannedBedName,
     '' AS PreviousWaitListId,
     ```
   - Sebaliknya, pada `WaitingListWorklistDal.cs` (metode `List` dan `ListActiveWorklist`), klausa SELECT ditulis langsung mereferensikan nama properti DTO sebagai nama kolom fisik tabel:
     ```sql
     aa.ArrivalSourceType,
     aa.ReferringUnitId,
     aa.ReferringUnitName,
     aa.ReferringDoctorId,
     aa.ReferringDoctorName,
     aa.ClinicalUrgency,
     aa.SpecialRequirements,
     aa.PlannedBedId,
     aa.PlannedBedName,
     aa.PreviousWaitListId,
     ```
   - Tabel fisik `BILRG_BedWaitingList` di SQL Server tidak memiliki kolom-kolom bernama `ArrivalSourceType`, `ReferringUnitId`, dll., sehingga kueri langsung digagalkan oleh SQL Server.

2. **Faktor Lolosnya Pengujian Unit**:
   - Unit test pada tahap implementasi (`WaitingListHandlerTest.cs` dan `WaitingListControllerTest.cs`) menggunakan mock interface `IWaitingListWorklistDal`, sehingga pengujian unit lulus 100% tanpa menyentuh database nyata.
   - Defect ini baru terdeteksi saat pengujian otomatis browser dijalankan langsung terhadap instance SQL Server nyata.

## Affected Components

- **Backend Module**: `Bilreg.Infrastructure.BedUsageContext.WaitingListFeature` (`b09-bilreg-api`)
- **Data Access Layer**: [`WaitingListWorklistDal.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/BedUsageContext/WaitingListFeature/WaitingListWorklistDal.cs) (metode `List` dan `ListActiveWorklist`)
- **Frontend UI Area**: Konsol Admisi Ranap (`c012_myhospital_web`), komponen [`DaftarTungguLane.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/lanes/DaftarTungguLane.vue)

## Impact Assessment

- **Business Impact**: Petugas admisi tidak dapat memantau antrean rawat inap aktif pada konsol admisi.
- **Operational Impact**: Alur pemrosesan antrean dan alokasi tempat tidur terhambat karena daftar antrean tidak muncul di layar.
- **Technical Impact**: Endpoint `GET /api/bed-usage/waiting-list` melempar HTTP 500 error secara konsisten saat dipanggil.

## Assumptions

1. Skema fisik tabel `BILRG_BedWaitingList` tidak mengalami perubahan DDL dan tetap memiliki kolom `EntrySource`, `UnitPengirimId`, `UnitPengirimName`, `DpjpId`, `DpjpName`, `UrgencyLevel`, `SpecialNeeds`, `AdvisoryBedId`, `AdvisoryBedName`.
2. DTO `WaitingListWorklistView` tetap dipertahankan propertinya agar kompatibel dengan kontrak envelope `{ items: [...] }` yang dikonsumsi frontend.

## Open Questions

- *Tidak ada open question.* Skema tabel fisik dan pemetaan kolom pada `WaitingListDal` sudah diverifikasi dan terbukti valid di basis data aktif.

## Recommended Decision

Selaraskan proyeksi klausa SQL SELECT pada `WaitingListWorklistDal.cs` (metode `List` dan `ListActiveWorklist`) agar memetakan kolom fisik basis data ke alias properti DTO `WaitingListWorklistView`, identik dengan pola pemetaan yang digunakan pada `WaitingListDal.cs`.

## Decision

Memperbarui kueri SQL SELECT di `WaitingListWorklistDal.cs` pada kedua metode (`List` dan `ListActiveWorklist`):
- Ganti `aa.ArrivalSourceType` menjadi `ISNULL(aa.EntrySource, 0) AS ArrivalSourceType`
- Ganti `aa.ReferringUnitId` menjadi `ISNULL(aa.UnitPengirimId, '') AS ReferringUnitId`
- Ganti `aa.ReferringUnitName` menjadi `ISNULL(aa.UnitPengirimName, '') AS ReferringUnitName`
- Ganti `aa.ReferringDoctorId` menjadi `ISNULL(aa.DpjpId, '') AS ReferringDoctorId`
- Ganti `aa.ReferringDoctorName` menjadi `ISNULL(aa.DpjpName, '') AS ReferringDoctorName`
- Ganti `aa.ClinicalUrgency` menjadi `ISNULL(aa.UrgencyLevel, 0) AS ClinicalUrgency`
- Ganti `aa.SpecialRequirements` menjadi `ISNULL(aa.SpecialNeeds, '') AS SpecialRequirements`
- Ganti `aa.PlannedBedId` menjadi `ISNULL(aa.AdvisoryBedId, '') AS PlannedBedId`
- Ganti `aa.PlannedBedName` menjadi `ISNULL(aa.AdvisoryBedName, '') AS PlannedBedName`
- Ganti `aa.PreviousWaitListId` menjadi `'' AS PreviousWaitListId`
- Pastikan klausa ORDER BY menggunakan alias atau kolom fisik yang valid (`aa.Priority DESC, aa.CrtDate ASC`).

## Decision Rationale

- **Zero Breaking Changes**: Mengoreksi ketidakcocokan nama kolom SQL tanpa mengubah kontrak publik REST API, skema JSON, DTO layer, maupun kode frontend.
- **Terbukti Berjalan**: Pola pemetaan ini identik dengan `WaitingListDal.cs` yang telah terbukti berhasil dieksekusi oleh SQL Server pada pengujian single-record.
- **Konsistensi Arsitektur**: Menjaga integritas `BedUsageContext` sebagai single source of truth tanpa memerlukan perubahan skema fisik DDL basis data.

## Architecture Applicability

### Decision

**ARCHITECTURE-NOT-REQUIRED**

### Rationale

Perbaikan ini merupakan koreksi kesalahan pemetaan kueri lokal (*localized DAL SQL projection defect*) pada layer infrastruktur (`WaitingListWorklistDal.cs`). Tidak ada perubahan pada batas modul, domain agregat, kontrak interface, DTO, use cases, maupun integrasi lintas-konteks. Target arsitektur pada dokumen Permanent Knowledge [WAITING-LIST-BED-USAGE-CONSOLIDATION-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-ARCHITECTURE.md) tetap sepenuhnya valid.
