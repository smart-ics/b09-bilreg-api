---
Title: Implementation Plan - Optimalisasi Kueri dan Akses Data Perjalanan Pasien Rawat Inap (Admisi Ranap Journey)
Code: ADM-RANAP-JOURNEY-LOAD-PERFORMANCE
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-26
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Implementasi ini merealisasikan perbaikan teknis untuk mengatasi masalah degradasi performa dan *timeout* pada endpoint perjalanan pasien rawat inap:
- `GET /api/admisi-ranap/journeys?scope=active&pageSize=20` (pemuatan daftar pasien aktif)
- `GET /api/admisi-ranap/journeys/{journeyId}` (pemuatan detail perjalanan pasien, misal `rsv:RSV097XD67QY`)

Referenced artifacts:
- Originating BUG ISSUE: [ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-ISSUE.md) (`ISS-ADM-REG-009`)
- Analysis Artifact: [ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-INVESTIGATION.md)
- Target Architecture: [ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

## Included Scope
1. **Penyelarasan Skrip DDL Basis Data (`Bilreg.SqlDb`)**:
   - Menambahkan skrip migrasi idempotent untuk indeks non-terklaster `IX_bilrg_admadmission_reservationid` dan `IX_bilrg_admadmission_opnamerequestid` pada tabel `BILRG_AdmAdmission`.
2. **Modernisasi Kontrak Asinkron DAL (`IJourneyDal`)**:
   - Memutakhirkan kontrak antarmuka `IJourneyDal.cs` dengan metode asinkron `ListAsync`, `GetByJourneyIdAsync`, dan `ResolveLegacyRecordAsync` yang mendukung `CancellationToken`.
3. **Restrukturisasi Kueri Proyeksi Daftar (`BuildJourneyCte`)**:
   - Mengganti subkueri global `GROUP BY ta_reg_history_dokter` dengan correlated `OUTER APPLY` per registrasi kandidat (`a.RegId`).
   - Menerapkan *early filter* status admisi aktif (`@ScopeActive = 0 OR a.AdmissionStatus <> 4`) pada CTE `AdmissionRoots`.
4. **Penyatuan Eksekusi Batch Facet & Paging**:
   - Menyatukan kueri `facetSql` dan `pageSql` menjadi satu teks kueri batch yang dieksekusi dalam satu *round-trip* basis data via `conn.QueryMultipleAsync`.
5. **Transisi Non-Blocking Async I/O**:
   - Mengubah implementasi akses data pada `JourneyDal.cs` dan `JourneyDal.Batch.cs` menjadi asinkron penuh menggunakan metode Dapper async.
   - Mengubah handler MediatR di `AdmListJourneyQry.cs` agar meng-`await` metode async tanpa pemanggilan blocking `Task.FromResult`.
6. **Verifikasi Test Suite**:
   - Menyesuaikan pengujian integrasi dan unit di `Bilreg.Test` yang mengonsumsi `IJourneyDal` dan memvalidasi seluruh test suite lulus 100%.

## Excluded Scope
1. Tidak ada perubahan pada kontrak respons JSON wire format eksternal.
2. Tidak ada perubahan pada logika tahapan bisnis (`Release1JourneyStageResolver` dan `Release1JourneySqlStageExpression`).
3. Tidak ada modifikasi kode pada frontend `c012_myhospital_web`.
4. Tidak menyentuh DAL atau handler di luar bounded context `AdmisiRanapContext/JourneyFeature`.

---

# 3. Dependencies

- **Database Dependencies**: Basis data `HOSPITAL_PKL` telah memiliki indeks `IX_ta_trs_billing_fs_kd_reg` dan `IX_bilrg_admadmission_reservationid`.
- **Inter-Slice Dependencies**: Seluruh dependensi antarslice dideklarasikan secara eksplisit berdasarkan urutan prasyarat teknis:
  - `P1-S01` (DDL Script) dan `P1-S02` (Async Interface) dapat dimulai secara independen.
  - `P2-S03` bergantung pada `P1-S02`.
  - `P2-S04` bergantung pada `P2-S03`.
  - `P3-S05` bergantung pada `P2-S04`.
  - `P3-S06` bergantung pada `P3-S05`.

---

# 4. Progress Summary

Status: COMPLETED  
Execution Approval: APPROVED

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Foundation & Contract Modernization | IMPLEMENTED | GO | 2/2 |
| P2 - CTE Query Optimization & DAL Async Transition | IMPLEMENTED | GO | 2/2 |
| P3 - Application Handlers & Test Verification | IMPLEMENTED | GO | 2/2 |

---

# 5. Phases

## P1 - Foundation & Contract Modernization

Implementation Status: IMPLEMENTED  
Review Status: GO

### P1-S01

Title: Penyelarasan Skrip DDL Indeks Relasi Admisi di Bilreg.SqlDb

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Menambahkan berkas skrip migrasi SQL di repositori `Bilreg.SqlDb` untuk mendokumentasikan indeks pendukung `IX_bilrg_admadmission_reservationid` dan `IX_bilrg_admadmission_opnamerequestid` secara idempotent.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:
1. Berkas skrip SQL baru `src/bilreg/Bilreg.SqlDb/AdmisiRanapContext/AdmissionFeature/BILRG_AdmAdmission_Reservation_Opname_Index.sql` tersedia.
2. Skrip memuat pembuatan indeks non-terklaster dengan klausa `IF NOT EXISTS` untuk:
   - `IX_bilrg_admadmission_reservationid` pada `BILRG_AdmAdmission (ReservationId, VodDate, RegId)`
   - `IX_bilrg_admadmission_opnamerequestid` pada `BILRG_AdmAdmission (OpnameRequestId, VodDate, RegId)`

Notes:
- Skrip ini menyelaraskan repositori dengan skema riil database yang sudah berjalan.
- [IMPLEMENTATION] Berkas skrip SQL `src/bilreg/Bilreg.SqlDb/AdmisiRanapContext/AdmissionFeature/BILRG_AdmAdmission_Reservation_Opname_Index.sql` telah dibuat dengan klausa `IF NOT EXISTS` dan `WITH (FILLFACTOR = 90)` untuk `IX_bilrg_admadmission_reservationid` dan `IX_bilrg_admadmission_opnamerequestid`. Proyek `src/bilreg/Bilreg.SqlDb/Bilreg.SqlDb.sqlproj` juga telah dimutakhirkan untuk menyertakan skrip tersebut.

---

### P1-S02

Title: Modernisasi Kontrak Asinkron pada IJourneyDal

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memutakhirkan antarmuka `IJourneyDal.cs` dengan menambahkan metode asinkron yang menerima `CancellationToken`.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:
1. Berkas `src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/IJourneyDal.cs` mendefinisikan metode:
   - `Task<JourneyListResult> ListAsync(JourneyListFilter filter, DateTime businessNow = default, CancellationToken cancellationToken = default)`
   - `Task<JourneyDetailWorkspace?> GetByJourneyIdAsync(string journeyId, DateTime businessNow = default, CancellationToken cancellationToken = default)`
   - `Task<JourneyLegacyResolution?> ResolveLegacyRecordAsync(string recordType, string recordId, DateTime businessNow = default, CancellationToken cancellationToken = default)`
2. Metode sinkron lama dapat dipertahankan sebagai deprecation wrapper atau didelegasikan jika masih dibutuhkan oleh mock pengujian lama.

Notes:
- Mengizinkan implementasi non-blocking di lapisan infrastruktur.
- [IMPLEMENTATION] Antarmuka `IJourneyDal` di `src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/IJourneyDal.cs` telah dimutakhirkan dengan mendefinisikan metode asinkron `ListAsync`, `GetByJourneyIdAsync`, dan `ResolveLegacyRecordAsync` yang mendukung `CancellationToken`. Default interface implementation (DIM) mendelegasikan ke metode sinkron yang ada untuk menjaga stabilitas kompilasi dan backward compatibility sebelum transisi penuh di P2-S04. Seluruh solusi terkompilasi bersih dan pengujian unit lolos 100%.

---

## P2 - CTE Query Optimization & DAL Async Transition

Implementation Status: IMPLEMENTED  
Review Status: GO

### P2-S03

Title: Restrukturisasi Kueri CTE Proyeksi Daftar (BuildJourneyCte)

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Mengoptimalkan kueri CTE `BuildJourneyCte` pada `JourneyDal.cs` dengan mengganti subkueri global riwayat dokter menjadi `OUTER APPLY` terarah dan menambahkan filter dini status admisi aktif.

Depends On: P1-S02

Repository: `b09-bilreg-api`

Completion Criteria:
1. Pada `BuildJourneyCte` di `src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs`:
   - Join subkueri `hd` pada `AdmissionRoots` diganti dengan:
     ```sql
     OUTER APPLY (
         SELECT TOP 1 h.fs_kd_dokter AS DokterId, ISNULL(p.fs_nm_peg, '') AS DokterName
         FROM ta_reg_history_dokter h
         LEFT JOIN td_peg p ON h.fs_kd_dokter = p.fs_kd_peg
         WHERE h.fs_kd_reg = a.RegId
           AND ISNULL(LTRIM(RTRIM(h.fd_tgl_selesai)),'') = ''
         ORDER BY h.fd_tgl_mulai DESC
     ) hd
     ```
   - Klausa `WHERE` pada `AdmissionRoots` memuat filter dini:
     ```sql
     WHERE a.VodDate = @VodDate
       AND (@ScopeActive = 0 OR a.AdmissionStatus <> 4)
     ```
2. Logika ekspresi tahapan (`SqlStage`) dan integritas kolom keluaran tetap identik dengan kontrak sebelumnya.

Notes:
- Perubahan ini mengeliminasi pemindaian penuh pada seluruh riwayat dokter rumah sakit.
- [IMPLEMENTATION] Subkueri global riwayat dokter `ta_reg_history_dokter` pada `AdmissionRoots` di `BuildJourneyCte` (`JourneyDal.cs`) telah digantikan dengan `OUTER APPLY` per registrasi kandidat (`a.RegId`) yang memanfaatkan indeks terklaster dengan pengurutan `h.fd_tgl_mulai DESC`. Filter dini status admisi aktif `AND (@ScopeActive = 0 OR a.AdmissionStatus <> 4)` telah ditambahkan ke klausa `WHERE` pada `AdmissionRoots`. Seluruh proyek terkompilasi bersih tanpa eror dan pengujian unit `AdmisiRanapContext` lulus 100% (279 passed).

---

### P2-S04

Title: Penyatuan Eksekusi Batch Facet & Paging dan Transisi Asinkron DAL

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Menggabungkan eksekusi kueri hitung facet dan kueri paging ke dalam satu teks batch SQL via `conn.QueryMultipleAsync`, serta mengubah seluruh pemanggilan database pada `JourneyDal.cs` dan `JourneyDal.Batch.cs` menjadi asinkron penuh.

Depends On: P2-S03

Repository: `b09-bilreg-api`

Completion Criteria:
1. Metode `ListAsync` pada `JourneyDal.cs` mengeksekusi `BuildFacetSql` dan `BuildPageSql` dalam satu perintah `QueryMultipleAsync` (atau batch SQL tunggal), memangkas *round-trip* dan menghindari evaluasi CTE ganda.
2. Seluruh pemanggilan Dapper di `JourneyDal.cs` dan `JourneyDal.Batch.cs` menggunakan metode asinkron:
   - `conn.OpenAsync(cancellationToken)`
   - `conn.QueryAsync<...>` / `conn.QueryFirstOrDefaultAsync<...>`
   - `conn.QueryMultipleAsync`
   - `conn.ExecuteScalarAsync<...>`
3. Pemuatan batch fakta pada `BatchLoadFactBags` di `JourneyDal.Batch.cs` diubah menjadi `async Task<Dictionary<string, JourneyFactBag>> BatchLoadFactBagsAsync(...)`.

Notes:
- Menghilangkan beban blocking worker thread di backend dan memangkas waktu pemrosesan kueri daftar.
- [IMPLEMENTATION] Eksekusi facet dan paging pada `JourneyDal.ListAsync` telah disatukan ke dalam satu batch SQL via `BuildFacetAndPageSql` dan dieksekusi dalam satu *round-trip* basis data menggunakan `conn.QueryMultipleAsync`. Seluruh metode akses data dan koneksi Dapper pada `JourneyDal.cs` dan `JourneyDal.Batch.cs` telah ditransisikan menjadi asinkron penuh (`OpenAsync`, `QueryMultipleAsync`, `QueryFirstOrDefaultAsync`, `QueryAsync`, `ExecuteScalarAsync`, `BatchLoadFactBagsAsync`) dengan dukungan `CancellationToken`. Metode sinkron dipertahankan mendelegasikan ke metode asinkron untuk backwards compatibility. Solusi terkompilasi bersih tanpa eror dan test suite `AdmisiRanapContext` lulus 100% (279 passed).

---

## P3 - Application Handlers & Test Verification

Implementation Status: IMPLEMENTED  
Review Status: GO

### P3-S05

Title: Pemutakhiran Handler MediatR dan Endpoint Controller Menjadi Asinkron

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memutakhirkan implementasi handler query MediatR dan controller API agar meng-`await` operasi asinkron DAL serta meneruskan `CancellationToken`.

Depends On: P2-S04

Repository: `b09-bilreg-api`

Completion Criteria:
1. Pada `src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/UseCases/AdmListJourneyQry.cs`:
   - `AdmListJourneyHandler.Handle` memanggil `await _dal.ListAsync(filter, _tglJamProvider.Now, cancellationToken)`.
   - `AdmGetJourneyHandler.Handle` memanggil `await _dal.GetByJourneyIdAsync(request.JourneyId.Trim(), _tglJamProvider.Now, cancellationToken)`.
   - `AdmResolveJourneyLegacyRecordHandler.Handle` memanggil `await _dal.ResolveLegacyRecordAsync(request.RecordType, request.RecordId, _tglJamProvider.Now, cancellationToken)`.
2. Pada `src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/JourneyController.cs`:
   - Metode `List`, `Get`, dan `Resolve` meneruskan `HttpContext.RequestAborted` ke `_mediator.Send`.

Notes:
- Menjamin pipeline API sepenuhnya non-blocking dari level controller hingga database connection.
- [IMPLEMENTATION] Handler MediatR `AdmListJourneyHandler`, `AdmGetJourneyHandler`, dan `AdmResolveJourneyLegacyRecordHandler` pada `AdmListJourneyQry.cs` telah dimutakhirkan menjadi `async Task` yang meng-`await` operasi asinkron DAL (`ListAsync`, `GetByJourneyIdAsync`, `ResolveLegacyRecordAsync`) dan meneruskan `cancellationToken`. Endpoint controller `JourneyController` (`List`, `Resolve`, `Get`) telah diperbarui untuk meneruskan `HttpContext.RequestAborted` ke `_mediator.Send`. Solusi berhasil dikompilasi bersih (`dotnet build b09-bilreg-api.sln`) dengan 0 eror.

---

### P3-S06

Title: Pemutakhiran dan Verifikasi Test Suite Bilreg.Test

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Menyesuaikan test suite integrasi dan unit pada `Bilreg.Test` yang mengonsumsi `IJourneyDal` agar kompatibel dengan metode asinkron baru, serta memverifikasi seluruh pengujian lulus dengan status hijau (100% PASS).

Depends On: P3-S05

Repository: `b09-bilreg-api`

Completion Criteria:
1. `src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/JourneyApiIntegrationTest.cs` disesuaikan untuk mem-mock metode async `IJourneyDal` (`ListAsync`, `GetByJourneyIdAsync`, `ResolveLegacyRecordAsync`).
2. Pengujian integrasi `JourneyDalIntegrationTest.cs` (jika ada) memverifikasi eksekusi `ListAsync` dan `GetByJourneyIdAsync`.
3. Seluruh unit test pada `Release1JourneyStageResolverTest.cs` tetap berjalan dan lulus 100%.
4. Eksekusi `dotnet test` pada proyek `Bilreg.Test` berhasil tanpa ada kegagalan uji (*0 failed*).

Notes:
- Menjamin paritas fungsional dan integritas regresi sistem.
- [IMPLEMENTATION] Pengujian integrasi API pada JourneyApiIntegrationTest.cs telah dimutakhirkan untuk mem-mock dan memverifikasi metode asinkron IJourneyDal (ListAsync, GetByJourneyIdAsync, ResolveLegacyRecordAsync). JourneyDalIntegrationTest.cs telah dilengkapi dengan pengujian ListAsync dan GetByJourneyIdAsync. Seluruh unit test pada Release1JourneyStageResolverTest.cs dan suite AdmisiRanapContext di Bilreg.Test berjalan dan lulus 100% (279 passed, 0 failed).

---

# 6. Change Log

- 2026-10-26: Initial implementation plan release for `ADM-RANAP-JOURNEY-LOAD-PERFORMANCE`. Execution Approval set to APPROVED.
- 2026-10-26: Eksekusi implementasi selesai (Phase 1, 2, dan 3). Seluruh slice P1-S01 hingga P3-S06 telah berstatus IMPLEMENTED dan meraih keputusan reviu GO. Status rencana kerja diperbarui menjadi COMPLETED.
