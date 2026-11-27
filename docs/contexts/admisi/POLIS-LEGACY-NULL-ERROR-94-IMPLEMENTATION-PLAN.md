---
Title: Implementation Plan - Kompatibilitas Persistensi Polis Asuransi (ta_polis & ta_polis_cover) dengan fo-legacy
Code: PLAN-POLIS-LEGACY-NULL-ERROR-94-001
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-27
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Menyelaraskan persistensi data polis asuransi (`ta_polis` dan `ta_polis_cover`) pada `b09-bilreg-api` agar kompatibel dengan skema fisik, ekspektasi pembacaan non-null, dan perilaku penyimpanan `fo-legacy` (`[MSB-35] Polis Asuransi`), sehingga menghilangkan galat `Run-time error '94': Invalid use of Null`.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:

- ISSUE: [POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/admisi/POLIS-LEGACY-NULL-ERROR-94-BUG-ISSUE.md) (`ISSUE-POLIS-LEGACY-NULL-ERROR-94-001`)
- BUG-INVESTIGATION: [POLIS-LEGACY-NULL-ERROR-94-BUG-INVESTIGATION.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/admisi/POLIS-LEGACY-NULL-ERROR-94-BUG-INVESTIGATION.md)
- ARCHITECTURE: [POLIS-LEGACY-NULL-ERROR-94-ARCHITECTURE.md](file:///E:/PROJECT/ICS/PROJECT-ACTIVE/MHW/b09-bilreg-api/docs/contexts/admisi/POLIS-LEGACY-NULL-ERROR-94-ARCHITECTURE.md) (`ARCH-POLIS-LEGACY-NULL-ERROR-94-001`)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Ruang lingkup implementasi berada sepenuhnya di repositori **`b09-bilreg-api`**:

1. **Penyelarasan Skema Referensi `Bilreg.SqlDb` (`P1-S01`)**:
   - Memperbarui `ta_polis.sql` dan `ta_polis_cover.sql` pada `Bilreg.SqlDb/AdmisiContext/JaminanFeature/` agar mencerminkan ke-20 kolom `ta_polis` dan ke-14 kolom `ta_polis_cover` beserta constraint `NOT NULL DEFAULT`.
2. **Penyelarasan Persistensi Header & Detail Polis serta Unit Test (`P1-S02`)**:
   - Memperbarui `PolisDto` dan `PolisDal` untuk menyertakan kolom legacy (`fs_kd_bu`, `fs_departemen`, `fs_no_payroll`) dan kolom audit (`crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr`) pada `Insert`, serta memperbarui `upd*` dan menjaga `fs_kd_bu`, `fs_departemen`, `fs_no_payroll` tetap *read-only* (dengan `ISNULL(..., '')`) pada `Update`.
   - Memperbarui `PolisCoverDto`, `PolisCoverDal`, dan `PolisRepo` untuk menyertakan `fd_tgl_expired` (mengikuti `PolisModel.ExpiredDate` / `ta_polis.fd_expired`) dan kolom audit `crt*`/`upd*` pada `ta_polis_cover`.
   - Memperbarui `PolisDalTest` dan `PolisCoverDalTest` untuk memverifikasi penyimpanan dan pembacaan kolom-kolom tersebut.

---

# 3. Dependencies

- **External Dependencies**:
  - .NET 8 SDK.
  - SQL Server test environment (`ConnStringHelper.GetTestEnv()`).
- **Slice Dependencies**:
  - `P1-S01`: `None`
  - `P1-S02`: `Depends On: P1-S01`

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---------|---------|---------|---------|
| P1 | IMPLEMENTED | GO | 2/2 |

---

# 5. Phases

## P1 - Legacy Persistence Compatibility (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED
Review Status: GO

### P1-S01

Title: Align ta_polis and ta_polis_cover SQL Schema Definitions in Bilreg.SqlDb

Implementation Status: IMPLEMENTED
Review Status: GO

Objective:
Menyelaraskan file definisi tabel `ta_polis.sql` dan `ta_polis_cover.sql` di `Bilreg.SqlDb` dengan seluruh kolom legacy dan audit yang ada pada basis data operasional `fo-legacy`.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:
1. `src/bilreg/Bilreg.SqlDb/AdmisiContext/JaminanFeature/ta_polis.sql` mendefinisikan 20 kolom (`fs_kd_polis`, `fs_kd_tipe_jaminan`, `fs_no_polis`, `fs_atas_nama`, `fd_expired`, `fs_kd_kelas_ri`, `fb_cover_rj`, `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, `crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr`) dengan constraint `NOT NULL DEFAULT` yang sesuai (`DEFAULT('3000-01-01')` untuk kolom tanggal, `DEFAULT('00:00:00')` untuk kolom jam, `DEFAULT('')` untuk kolom teks termasuk `fs_no_payroll`, dan `DEFAULT(0)` untuk `fb_cover_rj`).
2. `src/bilreg/Bilreg.SqlDb/AdmisiContext/JaminanFeature/ta_polis_cover.sql` mendefinisikan 14 kolom (`fs_kd_polis`, `fs_mr` `VARCHAR(15)`, `fs_kd_status`, `fd_tgl_expired` `DEFAULT('3000-01-01')`, `crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr`) dengan constraint `NOT NULL DEFAULT` yang sesuai.

Notes:
- Tidak ada perubahan logika C# pada slice ini; fokus murni pada skema SQL referensi di `Bilreg.SqlDb`.
- Implementation Notes (2026-11-27):
  - Updated `src/bilreg/Bilreg.SqlDb/AdmisiContext/JaminanFeature/ta_polis.sql` to define all 20 columns (`fs_kd_polis`, `fs_kd_tipe_jaminan`, `fs_no_polis`, `fs_atas_nama`, `fd_expired`, `fs_kd_kelas_ri`, `fb_cover_rj`, `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, `crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr`) with exact data types and `NOT NULL DEFAULT` constraints matching `fo-legacy` operational schema (`DEFAULT('3000-01-01')` for date fields, `DEFAULT('00:00:00')` for time fields, `DEFAULT('')` for string fields, and `DEFAULT(0)` for `fb_cover_rj`).
  - Updated `src/bilreg/Bilreg.SqlDb/AdmisiContext/JaminanFeature/ta_polis_cover.sql` to define all 14 columns (`fs_kd_polis`, `fs_mr VARCHAR(15)`, `fs_kd_status`, `fd_tgl_expired DEFAULT('3000-01-01')`, `crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr`) with `NOT NULL DEFAULT` constraints.
  - Changed Files:
    - `src/bilreg/Bilreg.SqlDb/AdmisiContext/JaminanFeature/ta_polis.sql`
    - `src/bilreg/Bilreg.SqlDb/AdmisiContext/JaminanFeature/ta_polis_cover.sql`

---

### P1-S02

Title: Implement Legacy-Compatible Persistence in PolisDal, PolisCoverDal, PolisRepo, and Tests

Implementation Status: IMPLEMENTED
Review Status: GO

Objective:
Memperbarui `PolisDto`, `PolisDal`, `PolisCoverDto`, `PolisCoverDal`, dan `PolisRepo` agar selalu menulis nilai non-null yang kompatibel dengan `fo-legacy` pada `ta_polis` dan `ta_polis_cover`, mempropagasi `ta_polis.fd_expired` ke `ta_polis_cover.fd_tgl_expired`, mempertahankan nilai `fs_kd_bu`, `fs_departemen`, `fs_no_payroll` saat update, serta memverifikasinya melalui `PolisDalTest` dan `PolisCoverDalTest`.

Depends On: P1-S01

Repository: `b09-bilreg-api`

Completion Criteria:
1. `PolisDto` dan `PolisDal.Insert` menyertakan `fs_kd_bu` (`""`), `fs_departemen` (`""`), `fs_no_payroll` (`""`), serta kolom audit `crttgl` (`yyyy-MM-dd`), `crtjam` (`HH:mm:ss`), `crtipa` (`""`), `crtver` (`""`), `crtusr` (`""`), `updtgl` (`"3000-01-01"`), `updjam` (`"00:00:00"`), `updipa` (`""`), `updver` (`""`), `updusr` (`""`).
2. `PolisDal.Update` memperbarui kolom bisnis polis, memperbarui `updtgl` (`yyyy-MM-dd`), `updjam` (`HH:mm:ss`), `updipa`, `updver`, `updusr`, tidak mengubah `crt*`, serta menjaga kolom legacy *read-only* dengan `fs_kd_bu = ISNULL(fs_kd_bu, '')`, `fs_departemen = ISNULL(fs_departemen, '')`, dan `fs_no_payroll = ISNULL(fs_no_payroll, '')`.
3. `PolisCoverDto`, `PolisCoverDal.Insert`, dan `PolisRepo.SaveChanges` mengisi `fd_tgl_expired` mengikuti `PolisModel.ExpiredDate` (`yyyy-MM-dd`) serta memetakan 10 kolom audit `crt*`/`upd*` ke `ta_polis_cover`.
4. `PolisCoverDal.ListData` membaca `ISNULL(aa.fd_tgl_expired, '3000-01-01') AS fd_tgl_expired`.
5. Seluruh test di `Bilreg.Test` untuk `JaminanFeature` (`PolisDalTest`, `PolisCoverDalTest`, `PolisModelTest`, `PolisUpdateHandlerTest`, `PolisControllerTest`) lulus (`dotnet test --filter "FullyQualifiedName~JaminanFeature"`).

Notes:
- Pastikan `PolisDto` dan `PolisCoverDto` menggunakan nilai default pada parameter opsional/konstruktor agar tidak merusak pemanggilan yang sudah ada.
- Implementation Notes (2026-11-27):
  - Updated `PolisDto` with default parameters for legacy corporate columns (`fs_kd_bu`, `fs_departemen`, `fs_no_payroll`) and audit columns (`crttgl`, `crtjam`, `crtipa`, `crtver`, `crtusr`, `updtgl`, `updjam`, `updipa`, `updver`, `updusr`), and populated creation timestamps (`yyyy-MM-dd` / `HH:mm:ss`) plus sentinel update timestamps (`3000-01-01` / `00:00:00`) in `PolisDto.FromModel`.
  - Updated `PolisDal.Insert` to write all 20 columns of `ta_polis`, `PolisDal.Update` to update business and `upd*` audit columns while preserving `crt*` and preserving/self-healing read-only legacy columns via `ISNULL(fs_kd_bu, '')`, `ISNULL(fs_departemen, '')`, and `ISNULL(fs_no_payroll, '')`, and `PolisDal` read queries (`GetData`, `ListData`, `GetDataByNoPeserta`) to apply defensive `ISNULL(...)` wrappers.
  - Updated `PolisCoverDto`, `PolisCoverDal.Insert`, `PolisCoverDal.ListData`, and `PolisRepo.SaveChanges` to propagate `model.ExpiredDate` (`yyyy-MM-dd`) into `ta_polis_cover.fd_tgl_expired`, map all 14 columns in `SqlBulkCopy`, and read `ISNULL(aa.fd_tgl_expired, '3000-01-01') AS fd_tgl_expired` plus audit columns.
  - Updated `PolisDalTest` and `PolisCoverDalTest` to verify 20-column and 14-column persistence, read-only legacy column preservation on update, header expiration propagation, and DTO mapping defaults.
  - Verification Command & Result:
    - `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --filter "FullyQualifiedName~JaminanFeature" -p:UseSharedCompilation=false /nodeReuse:false`: 80 passed, 6 failed (all 6 failures are pre-existing in `GetGrupJaminanJetliServiceTest`, outside slice scope).
    - `dotnet test src/bilreg/Bilreg.Test/Bilreg.Test.csproj --no-build --filter "FullyQualifiedName~Polis"`: 81 passed, 0 failed (`PolisDalTest`, `PolisCoverDalTest`, `PolisModelTest`, `PolisUpdateHandlerTest`, `PolisControllerTest`).
  - Changed Files:
    - `src/bilreg/Bilreg.Infrastructure/AdmisiContext/JaminanFeature/PolisDto.cs`
    - `src/bilreg/Bilreg.Infrastructure/AdmisiContext/JaminanFeature/PolisDal.cs`
    - `src/bilreg/Bilreg.Infrastructure/AdmisiContext/JaminanFeature/PolisCoverDto.cs`
    - `src/bilreg/Bilreg.Infrastructure/AdmisiContext/JaminanFeature/PolisCoverDal.cs`
    - `src/bilreg/Bilreg.Infrastructure/AdmisiContext/JaminanFeature/PolisRepo.cs`
    - `src/bilreg/Bilreg.Test/AdmisiContext/JaminanFeature/PolisDalTest.cs`
    - `src/bilreg/Bilreg.Test/AdmisiContext/JaminanFeature/PolisCoverDalTest.cs`

---

# 6. Change Log

- **v1.0 (2026-11-27)**: Initial implementation plan created from `ARCH-POLIS-LEGACY-NULL-ERROR-94-001` and released with `Execution Approval: APPROVED`.
