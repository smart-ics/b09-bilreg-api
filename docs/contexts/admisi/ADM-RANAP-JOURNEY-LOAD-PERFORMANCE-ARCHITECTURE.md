---
Title: Architecture - Optimalisasi Kueri dan Akses Data Perjalanan Pasien Rawat Inap (Admisi Ranap Journey)
Code: ADM-RANAP-JOURNEY-LOAD-PERFORMANCE
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-10-26
---

# 1. Overview

Arsitektur ini mendefinisikan realisasi teknis untuk mengoreksi masalah degradasi performa dan *timeout* pada endpoint pemantauan perjalanan pasien rawat inap (*Admisi Ranap Journey*):
- `GET /api/admisi-ranap/journeys?scope=active&pageSize=20` (pemuatan daftar pasien aktif)
- `GET /api/admisi-ranap/journeys/{journeyId}` (pemuatan detail perjalanan pasien, misal `rsv:RSV097XD67QY`)

Koreksi ini mengatasi masalah latensi tinggi dan kegagalan *timeout* 30 detik pada percobaan pertama (*cold hit*) saat pengguna membuka layar admisi rawat inap atau memilih pasien dari daftar.

Penyelesaian teknis mencakup:
1. **Restrukturisasi Kueri CTE Proyeksi Daftar (`BuildJourneyCte`)**: Mengganti pengelompokan global (*unbounded GROUP BY*) pada tabel riwayat dokter `ta_reg_history_dokter` dengan korelasi terarah (*correlated OUTER APPLY* per registrasi kandidat) dan menerapkan *predicate pushdown* status aktif pada entitas admisi.
2. **Eliminasi Eksekusi Ganda CTE Proyeksi**: Menggabungkan eksekusi facet agregasi (`BuildFacetSql`) dan paging kunci perjalanan (`BuildPageSql`) ke dalam satu *round-trip* basis data menggunakan `QueryMultiple` / *single execution batch*, sehingga grafik CTE tidak lagi dieksekusi 2x berurutan.
3. **Modernisasi Asinkron (*Async I/O Pipeline*)**: Mengubah implementasi sinkron/blocking pada `JourneyDal` dan handler MediatR menjadi *fully non-blocking async* (`OpenAsync`, `QueryAsync`, `QueryMultipleAsync`) guna mencegah kehabisan thread pool (*thread pool starvation*) di backend ASP.NET Core saat kueri konkuren berlangsung.
4. **Kodifikasi dan Pemeliharaan Skema Indeks Basis Data**: Memformalkan skema indeks relasi non-terklaster pada `BILRG_AdmAdmission` (`IX_bilrg_admadmission_reservationid` dan `IX_bilrg_admadmission_opnamerequestid`) serta memastikan indeks verifikasi tagihan pada `ta_trs_billing` tercatat di dalam skrip migrasi `Bilreg.SqlDb`.

Originating Bug Issue:
- [ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-ISSUE.md) (`ISS-ADM-REG-009`)

---

# 2. Architectural Basis

## Business Context

- **Outcome Registrasi**: [OC-ADM-REGISTRASI.md](file:///d:/project_aktif/project_MyHospitalWeb/b21-myhosweb-system/outcomes/OC-ADM-REGISTRASI.md)  
  Prinsip kecepatan respons loket dan ketersediaan data seketika: Layar pemantauan admisi rawat inap merupakan antarmuka operasional primer bagi petugas loket admisi dalam mengidentifikasi pasien aktif yang membutuhkan penempatan kamar, persetujuan bangsal, maupun registrasi masuk. Keterlambatan pemuatan data (> 5 detik) atau kegagalan *timeout* langsung berdampak pada waktu tunggu antrean pasien di loket.
- **Domain Context**:
  - `03-ADMISSION-DOMAIN.md`: Mengatur siklus hidup permintaan opname (*Opname Request*), pemesanan tempat tidur (*Reservation*), pendaftaran rawat inap (*Admission*), dan antrean tempat tidur (*Bed Waiting List*).

## Analysis Input

- **BUG-INVESTIGATION**: [ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-INVESTIGATION.md)  
  Temuan analisis yang menjadi dasar realisasi teknis:
  1. **Indeks Verifikasi Tagihan Sudah Optimal**: Pemeriksaan `sp_helpindex ta_trs_billing` membuktikan bahwa indeks `IX_ta_trs_billing_fs_kd_reg` dan `IX_ta_trs_billing_fs_kd_reg2` telah aktif dan kueri `QueryHasBillingItems` dieksekusi via *Index Seek* (< 1 ms).
  2. **Indeks Relasi Admisi Sudah Ada**: Tabel `BILRG_AdmAdmission` pada basis data aktif telah memiliki `IX_bilrg_admadmission_reservationid` (`ReservationId, VodDate, RegId`).
  3. **Akar Masalah Utama Teridentifikasi**: Bottleneck utama berasal dari eksekusi ganda CTE `BuildJourneyCte` pada kueri daftar aktif (`journeys?scope=active&pageSize=20`) yang memuat subkueri `GROUP BY` terhadap seluruh isi tabel `ta_reg_history_dokter`, memindai data historis penuh tanpa pembatasan status dini, serta mengeksekusi operasi basis data secara sinkron/blocking. Beban ini memicu antrean sumber daya (*resource contention*) yang menyebabkan pemanggilan detail pasien `journeys/rsv%3ARSV097XD67QY` mengalami *timeout* 30 detik pada kondisi *cold cache*.

```text
ISSUE (ISS-ADM-REG-009)
        +
BUG-INVESTIGATION (ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-INVESTIGATION)
        ↓
    ARCHITECTURE (ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-ARCHITECTURE)
```

---

# 3. Scope

## Included

1. **Backend - Optimalisasi Kueri CTE Proyeksi Daftar (`b09-bilreg-api`)**:
   - Refaktor `BuildJourneyCte` pada `JourneyDal.cs`:
     - Mengubah join `hd` (subkueri riwayat dokter) dari global `GROUP BY ta_reg_history_dokter` menjadi correlated `OUTER APPLY` per baris `a.RegId`.
     - Menyaring data admisi historis pada CTE `AdmissionRoots` saat filter `@ScopeActive = 1` dengan membatasi status admisi non-batal (`a.AdmissionStatus <> 4`).
   - Penyatuan eksekusi kueri facet dan kueri paging ke dalam satu *round-trip* basis data via `conn.QueryMultipleAsync`.
2. **Backend - Lapisan Asinkron DAL & Pipeline MediatR (`b09-bilreg-api`)**:
   - Pemutakhiran antarmuka `IJourneyDal`:
     - `Task<JourneyListResult> ListAsync(...)`
     - `Task<JourneyDetailWorkspace?> GetByJourneyIdAsync(...)`
     - `Task<JourneyLegacyResolution?> ResolveLegacyRecordAsync(...)`
   - Migrasi pemanggilan Dapper di `JourneyDal.cs` dan `JourneyDal.Batch.cs` menggunakan metode non-blocking: `OpenAsync()`, `QueryAsync()`, `QueryFirstOrDefaultAsync()`, `ExecuteScalarAsync()`, dan `QueryMultipleAsync()`.
   - Pemutakhiran handler MediatR (`AdmListJourneyHandler`, `AdmGetJourneyHandler`, `AdmResolveJourneyLegacyRecordHandler`) menjadi *fully asynchronous*.
3. **Backend - Skrip Migrasi Skema Basis Data (`b09-bilreg-api`)**:
   - Menambahkan berkas skrip migrasi resmi di folder `src/bilreg/Bilreg.SqlDb/AdmisiRanapContext/AdmissionFeature/` untuk mendokumentasikan dan memastikan keberadaan indeks `IX_bilrg_admadmission_reservationid` dan `IX_bilrg_admadmission_opnamerequestid`.
4. **Backend - Test Suite (`b09-bilreg-api`)**:
   - Pemutakhiran pengujian integrasi dan unit di `Bilreg.Test` yang mengonsumsi `IJourneyDal` agar kompatibel dengan kontrak asinkron baru.
   - Verifikasi paritas fungsional hasil daftar dan facet antara kueri lama dan kueri teroptimasi.

## Excluded

1. **Perubahan Kontrak Eksternal API (Wire Format)**: Skema JSON respons `JourneyListResult` dan `JourneyDetailWorkspace` tidak mengalami perubahan properti apa pun (*100% backward-compatible*).
2. **Perubahan Logika Resolusi Tahapan (*Stage Resolver*)**: Aturan bisnis pada `Release1JourneyStageResolver` dan ekspresi CASE pada `Release1JourneySqlStageExpression` dipertahankan sepenuhnya.
3. **Perubahan Frontend (`c012_myhospital_web`)**: Tidak ada perubahan kode pada UI Vue 3 (`AdmisiRanapConsole.vue`, `JourneyWorkspace.vue`). Klien frontend otomatis menikmati pemuatan data yang jauh lebih cepat dan terhindar dari *timeout*.

---

# 4. Technical Decisions

### TD-01: Correlated `OUTER APPLY` untuk Riwayat Dokter Menggantikan Global `GROUP BY`
- **Keputusan**: Mengubah join riwayat dokter pada `AdmissionRoots` menjadi:
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
- **Alasan**: Tabel `ta_reg_history_dokter` memiliki clustered index PK pada `(fs_kd_reg, fs_kd_dokter, fd_tgl_mulai)`. Dengan `OUTER APPLY`, SQL Server melakukan *Index Seek* langsung per `a.RegId` kandidat. Kueri tidak lagi melakukan *Table Scan* dan agregasi `GROUP BY` pada jutaan data riwayat dokter se-rumah sakit.

### TD-02: Penyatuan Eksekusi Kueri Facet dan Paging via Single Batch Query
- **Keputusan**: Menggabungkan `facetSql` dan `pageSql` menjadi satu teks kueri batch yang dieksekusi melalui `conn.QueryMultipleAsync`.
- **Alasan**: Sebelumnya, CTE `BuildJourneyCte()` dieksekusi 2 kali berurutan. Dengan menyatukannya dalam satu batch SQL:
  ```sql
  -- Single execution batch
  {BuildJourneyCte()}
  SELECT j.SqlStage, COUNT(*) AS Cnt
  FROM JourneyProjected j
  WHERE {BuildNonStageWhere(filter)}
  GROUP BY j.SqlStage;

  SELECT TOP (@PageSize) j.JourneyId, j.SortAt, j.SqlStage, j.OriginKind, j.RegId
  FROM JourneyProjected j
  WHERE {BuildNonStageWhere(filter)}
    AND (@HasStageFilter = 0 OR j.SqlStage = @StageFilter)
    AND (
          @HasCursor = 0
          OR j.SortAt < @CursorSortAt
          OR (j.SortAt = @CursorSortAt AND j.JourneyId < @CursorJourneyId)
        )
  ORDER BY j.SortAt DESC, j.JourneyId DESC;
  ```
  Biaya komputasi dan *round-trip* jaringan terpangkas separuh, serta menghindari eksekusi ganda pada grafik relasi yang sama.

### TD-03: Early Filtering Status Admisi pada Scope Active
- **Keputusan**: Menambahkan klausa penyaringan awal pada CTE `AdmissionRoots`:
  ```sql
  WHERE a.VodDate = @VodDate
    AND (@ScopeActive = 0 OR a.AdmissionStatus <> 4)
  ```
- **Alasan**: Kolom `AdmissionStatus` pada `BILRG_AdmAdmission` telah memiliki indeks `IX_BILRG_AdmAdmission_Status_CrtDate`. Menyingkirkan rekaman berstatus batal (`Cancelled = 4`) secara dini mengurangi volume baris yang diproses ke dalam `JourneyUnion`.

### TD-04: Transisi Menyeluruh ke Non-Blocking Asynchronous I/O pada DAL
- **Keputusan**: Mengubah seluruh metode akses data `JourneyDal` dan MediatR handler menjadi `async Task`.
- **Alasan**: Eksekusi sinkron (`conn.Query` + `Task.FromResult`) memblokir thread pool ASP.NET Core selama kueri basis data berjalan. Penggunaan `async/await` membebaskan worker thread untuk melayani permintaan lain (seperti permintaan detail pasien), menghilangkan bottleneck antrean thread server.

---

# 5. Component Responsibilities

| Komponen | Tanggung Jawab |
|---|---|
| `IJourneyDal` | Mendefinisikan kontrak asinkron untuk operasi pembacaan daftar perjalanan (`ListAsync`), detail perjalanan (`GetByJourneyIdAsync`), dan resolusi rekaman legacy (`ResolveLegacyRecordAsync`). |
| `JourneyDal.cs` | Mengimplementasikan pembacaan data asinkron berbasis Dapper, pembangunan SQL CTE teroptimasi (`BuildJourneyCte`), eksekusi batch `QueryMultipleAsync`, dan pemetaan hasil. |
| `JourneyDal.Batch.cs` | Mengimplementasikan pemuatan fakta jamak (*batch hydration*) secara asinkron tanpa *N+1 round-trip*. |
| `AdmListJourneyHandler` | Handler MediatR untuk query daftar perjalanan pasien rawat inap yang mendelegasikan ke `_dal.ListAsync` tanpa blocking. |
| `AdmGetJourneyHandler` | Handler MediatR untuk query detail perjalanan pasien yang mendelegasikan ke `_dal.GetByJourneyIdAsync` tanpa blocking. |
| `AdmResolveJourneyLegacyRecordHandler` | Handler MediatR untuk resolusi ID legacy yang mendelegasikan ke `_dal.ResolveLegacyRecordAsync` secara asinkron. |
| `JourneyController.cs` | Controller API ASP.NET Core yang mengekspos endpoint REST dan meneruskan permintaan ke MediatR secara asinkron. |
| `Bilreg.SqlDb` | Menyimpan dan memelihara skrip migrasi DDL indeks pendukung pada tabel `BILRG_AdmAdmission`. |

---

# 6. Integration Design

| Komponen Sumber | Komponen Target | Tujuan / Protokol |
|---|---|---|
| `JourneyController` | `IMediator` | Mengirimkan `AdmListJourneyQry` / `AdmGetJourneyQry` melalui perantara MediatR (`Send(query, cancellationToken)`). |
| `AdmListJourneyHandler` | `IJourneyDal` | Memanggil `_dal.ListAsync(filter, businessNow, cancellationToken)` untuk mengambil data daftar pasien terproyeksi. |
| `AdmGetJourneyHandler` | `IJourneyDal` | Memanggil `_dal.GetByJourneyIdAsync(journeyId, businessNow, cancellationToken)` untuk memuat detail perjalanan pasien. |
| `JourneyDal` | `IDbConnection` (SQL Server) | Mengeksekusi kueri teroptimasi secara non-blocking via Dapper async extension methods (`QueryMultipleAsync`, `QueryFirstOrDefaultAsync`). |

---

# 7. Data Ownership

| Entitas Data | Pemilik Otoritatif (*Authority*) | Model Akses pada Arsitektur Ini |
|---|---|---|
| Permintaan Opname (`BILRG_AdmOpnameRequest`) | Opname Request Context | Proyeksi Baca (*Read Projection*) |
| Reservasi Admisi (`BILRG_AdmReservation`) | Reservation Context | Proyeksi Baca (*Read Projection*) |
| Pendaftaran Admisi (`BILRG_AdmAdmission`) | Admission Context | Proyeksi Baca (*Read Projection*) |
| Antrean Bangsal (`BILRG_BedWaitingList`) | Waiting List Context | Proyeksi Baca (*Read Projection*) |
| Riwayat Penugasan Dokter (`ta_reg_history_dokter`) | Sistem Informasi Registrasi (Legacy SIMRS) | Akses Baca Terindeks via Correlated Outer Apply |
| Transaksi Tagihan (`ta_trs_billing`) | Billing / Tata Rekening Context | Pengecekan Kelaikan Aksi Pembatalan via Index Seek |

---

# 8. Database Design

## New Tables
Tidak ada tabel baru yang dibuat.

## Modified Tables
Tidak ada perubahan skema kolom pada tabel yang ada.

## New / Maintained Indexes

| Tabel | Nama Indeks | Jenis | Kolom Kunci | Keterangan |
|---|---|---|---|---|
| `BILRG_AdmAdmission` | `IX_bilrg_admadmission_reservationid` | Non-Clustered | `ReservationId, VodDate, RegId` | Menangani *Index Seek* kueri `FindAdmissionByReservation` dan relasi reservasi. |
| `BILRG_AdmAdmission` | `IX_bilrg_admadmission_opnamerequestid` | Non-Clustered | `OpnameRequestId, VodDate, RegId` | Menangani *Index Seek* kueri `FindAdmissionByOpname` dan relasi opname. |

## Migration Considerations
- Indeks pada tabel `BILRG_AdmAdmission` diverifikasi keberadaannya melalui skrip migrasi idempotent (`IF NOT EXISTS (SELECT 1 FROM sys.indexes ...)`).
- Penambahan indeks tidak memerlukan *downtime* atau modifikasi data (*zero-impact migration*).

---

# 9. Cross-Cutting Concerns

### Performance & Latency
- Pemuatan awal daftar pasien aktif (`journeys?scope=active&pageSize=20`) ditargetkan selesai dalam **< 2.000 ms** pada kondisi *cold cache* (dibandingkan sebelumnya yang mencapai 30–45 detik).
- Pemuatan detail pasien (`/journeys/{journeyId}`) ditargetkan selesai dalam **< 500 ms** tanpa mengalami kegagalan *timeout*.

### Concurrency & Thread Pool Health
- Penggunaan asynchronous I/O (`await conn.QueryAsync(...)`) pada seluruh siklus hidup request di backend menjamin worker thread ASP.NET Core tidak terblokir selama I/O basis data berlangsung, mencegah kegagalan *timeout* berantai saat pengguna membuka layar dan memilih pasien secara bersamaan.

### Resource Attribution & Logging
- Metrik durasi eksekusi tetap dipantau melalui header `Server-Timing: journey;dur=...` serta *structured log* pada `JourneyDal.ListAsync` (`facetPageMs`, `batchMs`, `resolveMs`, `totalMs`).

---

# 10. Implementation Constraints

1. **Kompatibilitas Dapper & C#**:
   - Seluruh kueri basis data harus menggunakan metode asinkron Dapper (`QueryAsync`, `QueryMultipleAsync`, `ExecuteScalarAsync`).
   - Tidak diperkenankan menggunakan `Task.FromResult` untuk membungkus operasi database sinkron di layer handler.
2. **Kepatuhan Terhadap Wire Contract**:
   - Struktur DTO kontrak respons `JourneyListResult` dan `JourneyDetailWorkspace` tidak boleh dimodifikasi demi menjaga paritas penuh dengan frontend `c012_myhospital_web`.
3. **Paritas Logika Tahapan (*Stage Logic Parity*)**:
   - Optimasi pada SQL CTE tidak boleh mengubah klasifikasi tahap perjalanan yang dihasilkan oleh `Release1JourneySqlStageExpression` dan `Release1JourneyStageResolver`.

---

# 11. Acceptance Conditions

1. **Pemuatan Daftar Pasien Responsif**:
   Panggilan `GET /api/admisi-ranap/journeys?scope=active&pageSize=20` pada basis data aktif berhasil mengembalikan status 200 OK beserta data 20 pasien dan facet tahap dalam waktu < 2 detik pada kondisi *cold cache*.
2. **Pemuatan Detail Bebas Timeout**:
   Panggilan `GET /api/admisi-ranap/journeys/rsv%3ARSV097XD67QY` berhasil mengembalikan status 200 OK beserta data lengkap perjalanan pasien pada percobaan pertama tanpa mengalami kegagalan *timeout* 30 detik.
3. **Eliminasi Full Table Scan**:
   Eksekusi kueri `BuildJourneyCte` tidak lagi melakukan agregasi global `GROUP BY` pada `ta_reg_history_dokter`.
4. **Eksekusi Asinkron Menyeluruh**:
   Tidak ada lagi metode blocking `conn.Query` atau `Task.FromResult(_dal.List(...))` pada pipeline pemrosesan perjalanan pasien rawat inap di `b09-bilreg-api`.
5. **Integritas Pengujian Otomatis**:
   Seluruh test suite pada `Bilreg.Test` yang menguji `JourneyDal`, `JourneyApiIntegrationTest`, dan `Release1JourneyStageResolverTest` lulus dengan status hijau (100% PASS).
