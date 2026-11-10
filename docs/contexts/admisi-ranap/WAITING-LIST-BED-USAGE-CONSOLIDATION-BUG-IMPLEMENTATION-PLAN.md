---
Title: Implementation Plan - Perbaikan Proyeksi Kueri SQL Worklist Antrean Rawat Inap pada BedUsageContext
Code: ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-002
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-10
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Rencana implementasi ini merealisasikan perbaikan cacat sistem (*defect fix*) yang dilaporkan pada dokumen investigasi [WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-INVESTIGATION.md) dan isu [WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-ISSUE.md).

Tujuan utama perbaikan ini adalah mengeliminasi galat HTTP 500 Internal Server Error pada pemanggilan endpoint worklist antrean rawat inap (`GET /api/bed-usage/waiting-list`) dengan menyelaraskan proyeksi klausa SQL SELECT pada `WaitingListWorklistDal.cs` agar memetakan nama kolom fisik basis data SQL Server tabel `BILRG_BedWaitingList` ke alias properti DTO `WaitingListWorklistView`.

Referenced artifacts:
- Originating BUG ISSUE: [WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-ISSUE.md) (`ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-002`)
- Analysis Artifact: [WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-INVESTIGATION.md)
- Target Architecture (Permanent Knowledge): [WAITING-LIST-BED-USAGE-CONSOLIDATION-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED

No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved feasibility decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

## Included Scope
1. **Penyelarasan Proyeksi SQL DAL (`WaitingListWorklistDal.cs`)**:
   - Memperbaiki klausa SELECT pada metode `ListActiveWorklist` dan `List` agar memetakan kolom fisik:
     - `ISNULL(aa.EntrySource, 0) AS ArrivalSourceType`
     - `ISNULL(aa.UnitPengirimId, '') AS ReferringUnitId`
     - `ISNULL(aa.UnitPengirimName, '') AS ReferringUnitName`
     - `ISNULL(aa.DpjpId, '') AS ReferringDoctorId`
     - `ISNULL(aa.DpjpName, '') AS ReferringDoctorName`
     - `ISNULL(aa.UrgencyLevel, 0) AS ClinicalUrgency`
     - `ISNULL(aa.SpecialNeeds, '') AS SpecialRequirements`
     - `ISNULL(aa.AdvisoryBedId, '') AS PlannedBedId`
     - `ISNULL(aa.AdvisoryBedName, '') AS PlannedBedName`
     - `'' AS PreviousWaitListId`
   - Memastikan klausa ORDER BY menggunakan kolom fisik atau alias yang valid (`aa.Priority DESC, aa.CrtDate ASC` dan `aa.UrgencyLevel DESC, aa.CrtDate ASC`).
   - Memastikan konsistensi pengecualian rekaman yang divoid (`aa.VodDate = @VodDate` dengan `@VodDate = VoidSentinel`).
2. **Penyusunan Pengujian Otomatis DAL (`WaitingListWorklistDalTest.cs`)**:
   - Menambahkan pengujian integrasi/unit DAL di `Bilreg.Test\BedUsageContext\WaitingListFeature` untuk memvalidasi query `List` dan `ListActiveWorklist` terhadap database atau memastikan tidak ada galat pemetaan kolom SQL.
   - Menjalankan seluruh test suite unit `BedUsageContext.WaitingListFeature` guna menjamin zero regressions.

## Excluded Scope
1. **Tidak Ada Perubahan Skema Database (No DDL Changes)**:
   - Tidak melakukan modifikasi tabel fisik `BILRG_BedWaitingList` di SQL Server.
2. **Tidak Ada Perubahan Antarmuka Frontend (`c012_myhospital_web`)**:
   - Kontrak DTO `WaitingListWorklistView` dan schema envelope JSON `{ status: "success", data: { items: [...] } }` tetap tidak berubah, sehingga komponen frontend `DaftarTungguLane.vue` dapat langsung mengonsumsi data yang dikembalikan.
3. **Kompatibilitas Mutlak SQL Server 2012**:
   - Seluruh fungsi SQL yang digunakan tetap mematuhi standar SQL Server 2012 (`ISNULL`, `CONVERT`, klausa JOIN standar).

---

# 3. Dependencies

- **Prasyarat Basis Data**: Database SQL Server dengan tabel `BILRG_BedWaitingList` dan `tc_mr`.
- **Dependensi Antar-Slice**:
  - `P1-S01` (Koreksi Proyeksi SQL DAL) tidak memiliki dependensi dan menjadi prasyarat untuk pengujian.
  - `P2-S02` (Pengujian Otomatis & Regresi DAL) bergantung langsung pada penyelesaian `P1-S01`.

---

# 4. Progress Summary

Status: COMPLETED  
Execution Approval: APPROVED  

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - DAL SQL Query Projection Fix | IMPLEMENTED | GO | 1/1 |
| P2 - DAL Automated Verification & Test Hardening | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - DAL SQL Query Projection Fix

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01

Title: Penyelarasan Proyeksi Kolom Fisik SQL pada `WaitingListWorklistDal.cs`

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mengoreksi klausa SQL SELECT pada metode `ListActiveWorklist(string bangsalId)` dan `List(WaitingListWorklistFilter filter)` di dalam `WaitingListWorklistDal.cs` agar memetakan kolom fisik tabel `BILRG_BedWaitingList` (`EntrySource`, `UnitPengirimId`, `UnitPengirimName`, `DpjpId`, `DpjpName`, `UrgencyLevel`, `SpecialNeeds`, `AdvisoryBedId`, `AdvisoryBedName`) dengan alias DTO yang sesuai (`ArrivalSourceType`, `ReferringUnitId`, `ReferringUnitName`, `ReferringDoctorId`, `ReferringDoctorName`, `ClinicalUrgency`, `SpecialRequirements`, `PlannedBedId`, `PlannedBedName`), menyediakan literal `'' AS PreviousWaitListId`, serta menyelaraskan klausa ORDER BY dan penanganan filter void sentinel.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:  
1. Berkas [`WaitingListWorklistDal.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/BedUsageContext/WaitingListFeature/WaitingListWorklistDal.cs) diperbarui pada kedua metode (`ListActiveWorklist` dan `List`) dengan pemetaan `ISNULL(...) AS ...` yang identik dengan pola `WaitingListDal.cs`.
2. Klausa ORDER BY pada `ListActiveWorklist` menggunakan `aa.UrgencyLevel DESC, aa.CrtDate ASC`.
3. Parameter `@VodDate` disertakan secara konsisten untuk memastikan data ter-void tidak terambil.
4. Solusi dikompilasi sukses tanpa warning/error (`dotnet build` sukses).

Notes:  
- Klausa SELECT pada `ListActiveWorklist` dan `List` di `WaitingListWorklistDal.cs` telah diperbarui dengan pemetaan kolom fisik SQL (`EntrySource`, `UnitPengirimId`, `UnitPengirimName`, `DpjpId`, `DpjpName`, `UrgencyLevel`, `SpecialNeeds`, `AdvisoryBedId`, `AdvisoryBedName`) ke alias DTO `WaitingListWorklistView` menggunakan `ISNULL(...) AS ...`, serta literal `'' AS PreviousWaitListId`.
- Klausa WHERE pada `ListActiveWorklist` diperbarui menyertakan `AND aa.VodDate = @VodDate` dengan penambahan parameter `@VodDate` bernilai `VoidSentinel`.
- Klausa ORDER BY pada `ListActiveWorklist` diperbarui menggunakan kolom fisik `aa.UrgencyLevel DESC, aa.CrtDate ASC`.
- Klausa ORDER BY pada `List` dipertahankan menggunakan `aa.Priority DESC, aa.CrtDate ASC`.
- Kompilasi `Bilreg.Infrastructure.csproj` sukses tanpa error (0 error). Seluruh unit test eksisting pada WaitingListFeature (100 test) lulus 100%.

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/BedUsageContext/WaitingListFeature/WaitingListWorklistDal.cs`

---

## P2 - DAL Automated Verification & Test Hardening

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S02

Title: Pengujian Otomatis DAL dan Verifikasi Regresi `WaitingListWorklistDal`

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menyusun test class otomatis [`WaitingListWorklistDalTest.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/BedUsageContext/WaitingListFeature/WaitingListWorklistDalTest.cs) untuk memverifikasi eksekusi metode `List` dan `ListActiveWorklist` terhadap database pengujian atau memvalidasi integritas query SQL, serta menjalankan seluruh rangkaian pengujian unit pada fitur WaitingList untuk menjamin zero regressions.

Depends On: P1-S01

Repository: `b09-bilreg-api`

Completion Criteria:  
1. Berkas pengujian `WaitingListWorklistDalTest.cs` terdaftar dan berjalan pada proyek `Bilreg.Test`.
2. Eksekusi `dotnet test --filter "FullyQualifiedName~BedUsageContext.WaitingListFeature"` menghasilkan 100% PASS tanpa kegagalan.
3. Kueri SQL terbukti valid dan tidak lagi melempar pengecualian `Invalid column name` saat diuji dengan runner test database.

Notes:  
- Berkas pengujian `WaitingListWorklistDalTest.cs` ditambahkan pada `Bilreg.Test\BedUsageContext\WaitingListFeature` menguji `ListActiveWorklist` dan `List` menggunakan `ConnStringHelper.GetTestEnv()` dan `TransHelper.NewScope()`.
- Metode `List` diuji baik dengan filter spesifik (`WaitingListWorklistFilter("B01", 0)`), filter default (`new WaitingListWorklistFilter()`), maupun filter null (`null!`).
- Pertahanan defensif `filter ??= new WaitingListWorklistFilter();` ditambahkan pada `WaitingListWorklistDal.List` untuk mencegah NRE pada filter null.
- Eksekusi `dotnet test --filter "FullyQualifiedName~BedUsageContext.WaitingListFeature"` menghasilkan 102/102 test PASS (100% lulus, 0 gagal, 0 skipped).

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/BedUsageContext/WaitingListFeature/WaitingListWorklistDal.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/BedUsageContext/WaitingListFeature/WaitingListWorklistDalTest.cs`

---

# 6. Change Log

- **2026-11-10**: Dokumen dibuat oleh peran `ica-architect` berdasarkan analisis cacat sistem pada [WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-BUG-INVESTIGATION.md). Status Execution Approval: APPROVED.
