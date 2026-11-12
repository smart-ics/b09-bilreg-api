---
Title: Rencana Implementasi Akurasi Stempel Waktu Timeline Perjalanan Pasien Rawat Inap (Admisi Ranap Journey)
Code: ADM-RANAP-JOURNEY-TIMELINE
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-19
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan perbaikan stempel waktu (*timestamp*) pada riwayat kronologis perjalanan pasien rawat inap (*Admisi Ranap Journey*) pada endpoint `GET /api/admisi-ranap/journeys/{journeyId}` di backend `b09-bilreg-api`.

Perbaikan ini mentransformasikan sistem agar setiap peristiwa (*timeline event*) menampilkan stempel waktu keterjadian (*occurredAt*) yang sebenarnya sesuai titik bisnis (waktu pembuatan entitas vs waktu mutasi status), mengeliminasi stempel waktu seragam semu, dan menjaga integritas rekam jejak audit medis rawat inap.

Referenced artifacts:
- Originating BUG ISSUE: [ADM-RANAP-JOURNEY-TIMELINE-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-TIMELINE-BUG-ISSUE.md) (`ISS-ADM-REG-008`)
- BUG-INVESTIGATION: [ADM-RANAP-JOURNEY-TIMELINE-BUG-INVESTIGATION.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-TIMELINE-BUG-INVESTIGATION.md)
- ARCHITECTURE: [ADM-RANAP-JOURNEY-TIMELINE-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-TIMELINE-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-REQUIRED (Dirujuk dan direalisasikan secara presisi dari dokumen ARCHITECTURE yang telah disahkan).

---

# 2. Planning Scope

## Lingkup yang Dikerjakan (Included):
1. **Perluasan Kontrak Fact DTO (`b09-bilreg-api`)**:
   - Menambahkan properti `DateTime? CreatedAt = null` pada `OpnameRequestJourneyFact` dan `ReservationJourneyFact`.
   - Menambahkan properti `DateTime? CreatedAt = null` dan `DateTime? UpdatedAt = null` pada `AdmissionJourneyFact`.
   - Memastikan kompatibilitas mundur (*backward compatibility*) sehingga instansiasi eksisting tidak terganggu.
2. **Ekstraksi Data di Lapisan DAL (`b09-bilreg-api`)**:
   - Memetakan kolom `CrtDate` dan `UpdDate` dari kueri SQL eksisting di `JourneyDal.cs` (`LoadOpnameJourney`, `LoadReservationJourney`, `AssembleAdmissionBag`).
   - Memetakan kolom `CrtDate` pada metode batch `JourneyDal.Batch.cs` (`FoldOpnameJourney`, `FoldReservationJourney`).
   - Mengonversi nilai sentinel void `3000-01-01` (`VoidSentinel`) menjadi `null` untuk `UpdatedAt`.
3. **Resolusi Stempel Waktu Timeline di Resolver (`b09-bilreg-api`)**:
   - Memperbarui `Release1JourneyStageResolver.cs:BuildTimeline` agar memetakan:
     - `OpnameRequested` -> `opn.CreatedAt ?? opn.StatusAt`
     - `ReservationCreated` -> `rsv.CreatedAt ?? rsv.StatusAt`
     - Status mutasi Opname/Reservasi -> `StatusAt`
     - `AdmissionRegistered` -> `admission.AdmissionAt ?? admission.CreatedAt`
     - `AdmissionUpdated` / `AdmissionCancelled` -> `admission.UpdatedAt ?? admission.AdmissionAt`
   - Mempertahankan urutan kelompok kategori (Opname -> Reservasi -> Admisi -> Waiting List).
4. **Penguatan Test Suite (`b09-bilreg-api`)**:
   - Menambahkan pengujian unit pada `Release1JourneyStageResolverTest.cs` untuk memverifikasi pemisahan stempel waktu pembuatan vs mutasi status.
   - Memperbarui data seed pada `JourneyDalIntegrationTest.cs` agar memiliki jeda waktu nyata antara `CrtDate` dan `UpdDate`, serta memverifikasi array timeline end-to-end.
   - Memastikan seluruh tes pada `Bilreg.Test` lulus 100%.

## Lingkup yang Dikecualikan (Excluded):
- Modifikasi skema tabel fisik database (DDL).
- Perubahan format wire response JSON API v1 (properti `timeline` tetap berformat `{ kind, occurredAt, relatedRecordId, summary }`).
- Perubahan kode frontend `c012_myhospital_web` (antarmuka Vue sudah siap menampilkan string waktu terformat).
- Modifikasi logika stage resolver operasional, kueri CTE listing, maupun evaluasi hak aksi admisi.

---

# 3. Dependencies

## External Dependencies:
- Tidak ada dependensi ke sistem eksternal atau service baru.
- Bergantung pada kolom `CrtDate`, `UpdDate`, dan `AdmissionDate` yang sudah tersedia pada tabel `BILRG_AdmReservation`, `BILRG_AdmOpnameRequest`, dan `BILRG_AdmAdmission`.

## Slice Dependencies Graph:
```text
P1-S01 (Contracts Expansion)
  ├──> P1-S02 (DAL Extraction Mapping) ─────────┐
  └──> P2-S03 (Resolver BuildTimeline) ──┬──────┴──> P2-S05 (Integration Seed & Test)
                                         └──> P2-S04 (Resolver Unit Test)
```

- `P1-S01`: None (prasyarat awal untuk seluruh DTO Fact).
- `P1-S02`: Depends On `P1-S01` (DAL membutuhkan field DTO Fact yang sudah diperluas).
- `P2-S03`: Depends On `P1-S01` (Resolver membutuhkan field DTO Fact yang sudah diperluas).
- `P2-S04`: Depends On `P2-S03` (Unit test menguji logika pemetaan BuildTimeline baru).
- `P2-S05`: Depends On `P1-S02, P2-S03` (Integration test menguji integrasi DAL dan Resolver dari database).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
| :--- | :--- | :--- | :---: |
| **P1 - Kontrak Fact & Lapisan Akses Data** | IMPLEMENTED | GO | 2/2 |
| **P2 - Resolver Timeline & Verifikasi Pengujian** | IMPLEMENTED | GO | 3/3 |

---

# 5. Phases

## P1 - Kontrak Fact & Lapisan Akses Data

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01: Perluasan Kontrak Fact Agregasi (`JourneyContracts.cs`)

Title: Perluasan Kontrak Fact Agregasi dengan Field CreatedAt dan UpdatedAt  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan parameter opsional `DateTime? CreatedAt = null` pada record `OpnameRequestJourneyFact` dan `ReservationJourneyFact`, serta parameter `DateTime? CreatedAt = null` dan `DateTime? UpdatedAt = null` pada record `AdmissionJourneyFact` di `JourneyContracts.cs`.

Depends On: None  
Repository: `b09-bilreg-api`  

Completion Criteria:  
1. Berkas [`JourneyContracts.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/JourneyContracts.cs) memuat field:
   - `OpnameRequestJourneyFact`: `DateTime? CreatedAt = null`
   - `ReservationJourneyFact`: `DateTime? CreatedAt = null`
   - `AdmissionJourneyFact`: `DateTime? CreatedAt = null, DateTime? UpdatedAt = null`
2. Nilai default parameter `= null` menjaga kompatibilitas mundur instansiasi eksisting (termasuk di `JourneyIdFactory.cs`).
3. Proyek `Bilreg.Application` berhasil dikompilasi tanpa *build error*.

Implementation Notes:  
- Menambahkan parameter opsional `DateTime? CreatedAt = null` pada record `OpnameRequestJourneyFact` dan `ReservationJourneyFact`.
- Menambahkan parameter opsional `DateTime? CreatedAt = null, DateTime? UpdatedAt = null` pada record `AdmissionJourneyFact`.
- Nilai default `= null` diletakkan setelah parameter wajib/eksisting sehingga mempertahankan kompatibilitas posisional maupun named-argument pada instansiasi eksisting (`JourneyIdFactory.cs`, DAL, dan resolver tests).
- Kompilasi proyek `Bilreg.Application` dan pengujian unit `JourneyFeature` di `Bilreg.Test` lulus 100% (76 passed).

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/JourneyContracts.cs`

---

### P1-S02: Pemetaan Ekstraksi Stempel Waktu di Lapisan DAL (`JourneyDal.cs` & `JourneyDal.Batch.cs`)

Title: Pemetaan CrtDate dan UpdDate ke Kontrak Fact pada Lapisan DAL  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memetakan nilai `CrtDate` dan `UpdDate` dari rekaman yang dibaca oleh kueri SQL DAL ke dalam field `CreatedAt` dan `UpdatedAt` pada Fact DTO.

Depends On: `P1-S01`  
Repository: `b09-bilreg-api`  

Completion Criteria:  
1. Pada [`JourneyDal.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs):
   - `LoadOpnameJourney`: memetakan `CreatedAt = opn.CrtDate`, `StatusAt = opn.UpdDate == VoidSentinel ? opn.CrtDate : opn.UpdDate`.
   - `LoadReservationJourney`: memetakan `CreatedAt = rsv.CrtDate`, `StatusAt = rsv.UpdDate == VoidSentinel ? rsv.CrtDate : rsv.UpdDate`.
   - `AssembleAdmissionBag`: memetakan `CreatedAt = adm.CrtDate`, `AdmissionAt = adm.AdmissionDate`, `UpdatedAt = adm.UpdDate == VoidSentinel ? null : adm.UpdDate`. Fallback rekaman admisi hilang tetap mempertahankan `CreatedAt = AdmissionDate`.
2. Pada [`JourneyDal.Batch.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs):
   - `FoldOpnameJourney`: memetakan `CreatedAt = opn.CrtDate`.
   - `FoldReservationJourney`: memetakan `CreatedAt = rsv.CrtDate`.
3. Proyek `Bilreg.Infrastructure` berhasil dikompilasi tanpa *build error*.

Implementation Notes:  
- Pada `JourneyDal.cs`:
  - `LoadOpnameJourney`: Menambahkan argumen `CreatedAt: opn.CrtDate` pada konstruksi `OpnameRequestJourneyFact`.
  - `LoadReservationJourney`: Menambahkan argumen `CreatedAt: rsv.CrtDate` pada konstruksi `ReservationJourneyFact`.
  - `AssembleAdmissionBag`: Menambahkan pemetaan `CreatedAt: opname.CrtDate` untuk `opnFact`, `CreatedAt: reservation.CrtDate` untuk `rsvFact`, fallback rekaman hilang dengan `CreatedAt: admission.AdmissionDate`, serta memetakan `CreatedAt: admission.CrtDate` dan `UpdatedAt: admission.UpdDate == VoidSentinel ? null : admission.UpdDate` pada `AdmissionJourneyFact`.
- Pada `JourneyDal.Batch.cs`:
  - `FoldOpnameJourney`: Memetakan `CreatedAt: opn.CrtDate` pada `OpnameRequestJourneyFact`.
  - `FoldReservationJourney`: Memetakan `CreatedAt: rsv.CrtDate` pada `ReservationJourneyFact`.
- Kompilasi proyek `Bilreg.Infrastructure` berhasil tanpa error (`0 Error(s)`).
- Seluruh 76 unit test `JourneyFeature` di `Bilreg.Test` lulus 100%.

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs`

---

## P2 - Resolver Timeline & Verifikasi Pengujian

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S03: Rekonstruksi Pemetaan Waktu pada Timeline Resolver (`Release1JourneyStageResolver.cs`)

Title: Pemetaan Stempel Waktu Deterministik pada BuildTimeline  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mengoreksi logika `BuildTimeline` pada `Release1JourneyStageResolver.cs` agar menggunakan `CreatedAt` untuk event pembuatan dan `StatusAt`/`UpdatedAt` untuk event perubahan status.

Depends On: `P1-S01`  
Repository: `b09-bilreg-api`  

Completion Criteria:  
1. Pada [`Release1JourneyStageResolver.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolver.cs#L382-L473):
   - `OpnameRequested` menggunakan `opn.CreatedAt ?? opn.StatusAt`
   - `ReservationCreated` menggunakan `rsv.CreatedAt ?? rsv.StatusAt`
   - Event mutasi status Opname dan Reservasi tetap menggunakan `StatusAt`
   - `AdmissionRegistered` menggunakan `admission.AdmissionAt ?? admission.CreatedAt`
   - `AdmissionUpdated` dan `AdmissionCancelled` menggunakan `admission.UpdatedAt ?? admission.AdmissionAt`
2. Urutan kategori event tetap dipertahankan: Opname -> Reservasi -> Admisi -> Waiting List.
3. Proyek `Bilreg.Application` berhasil dikompilasi tanpa *build error*.

Implementation Notes:  
- Pada `Release1JourneyStageResolver.cs` (`BuildTimeline`):
  - Memperbarui pemetaan `OpnameRequested` menggunakan `opn.CreatedAt ?? opn.StatusAt`.
  - Memperbarui pemetaan `ReservationCreated` menggunakan `rsv.CreatedAt ?? rsv.StatusAt`.
  - Mempertahankan stempel waktu `StatusAt` untuk event mutasi status Opname (`OpnameCancelled`, `OpnameFulfilled`) dan Reservasi (`ReservationMaintained`, `ReservationCancelled`, `ReservationRealized`).
  - Memperbarui pemetaan `AdmissionRegistered` menggunakan `admission.AdmissionAt ?? admission.CreatedAt`.
  - Memperbarui pemetaan `AdmissionUpdated` dan `AdmissionCancelled` menggunakan `admission.UpdatedAt ?? admission.AdmissionAt`.
- Mempertahankan struktur urutan kategori event (Opname -> Reservasi -> Admisi -> Waiting List) tanpa `OrderBy(occurredAt)` global sesuai TD-05.
- Kompilasi proyek `Bilreg.Application` berhasil tanpa error (`0 Error(s)`).
- Seluruh 65 unit test pada `AdmisiRanapContext.JourneyFeature` (termasuk 32 unit test pada `Release1JourneyStageResolverTest`) lulus 100%.

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolver.cs`

---

### P2-S04: Penguatan Unit Test Suite Timeline (`Release1JourneyStageResolverTest.cs`)

Title: Penambahan Kasus Uji Unit Disparitas Waktu Timeline  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan unit test baru di `Release1JourneyStageResolverTest.cs` untuk menguji skenario perbedaan waktu pembuatan vs mutasi status secara presisi.

Depends On: `P2-S03`  
Repository: `b09-bilreg-api`  

Completion Criteria:  
1. Menambahkan kasus uji pada [`Release1JourneyStageResolverTest.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolverTest.cs):
   - Skenario Reservasi: `CreatedAt = T0 (09:16:57)` dan `StatusAt = T1 (09:18:51)`. Verifikasi bahwa `ReservationCreated` bernilai `T0` dan `ReservationRealized` bernilai `T1`.
   - Skenario Opname Request: `CreatedAt = T0` dan `StatusAt = T1`. Verifikasi `OpnameRequested` bernilai `T0` dan `OpnameFulfilled` bernilai `T1`.
   - Skenario Admisi Updated: `AdmissionAt = T1` dan `UpdatedAt = T2`. Verifikasi `AdmissionRegistered` bernilai `T1` dan `AdmissionUpdated` bernilai `T2`.
2. Seluruh unit test pada `Release1JourneyStageResolverTest` lulus 100% saat dieksekusi via `dotnet test`.

Implementation Notes:  
- Pada `Release1JourneyStageResolverTest.cs`:
  - Memperbarui helper instansiasi fact `Opname`, `Reservation`, dan `Admission` untuk mendukung parameter opsional `statusAt`, `createdAt`, `admissionAt`, dan `updatedAt` dengan tetap menjaga kelangsungan backward compatibility.
  - Menambahkan pengujian unit disparitas waktu reservasi (`Timeline_ReservationWithTimeDisparity_ResolvesCreatedAndRealizedTimestampsAccurately`) memvalidasi `ReservationCreated = T0 (09:16:57)` dan `ReservationRealized = T1 (09:18:51)`.
  - Menambahkan pengujian unit disparitas waktu opname request (`Timeline_OpnameRequestWithTimeDisparity_ResolvesRequestedAndFulfilledTimestampsAccurately`) memvalidasi `OpnameRequested = T0` dan `OpnameFulfilled = T1`.
  - Menambahkan pengujian unit disparitas waktu admisi diperbarui (`Timeline_AdmissionUpdatedWithTimeDisparity_ResolvesRegisteredAndUpdatedTimestampsAccurately`) memvalidasi `AdmissionRegistered = T1` dan `AdmissionUpdated = T2`.
  - Menambahkan pengujian unit fallback (`Timeline_WhenCreationOrUpdateTimestampIsNull_FallsBackGracefully`) untuk memverifikasi fallback ke `StatusAt`/`AdmissionAt` ketika `CreatedAt`/`UpdatedAt` bernilai null.
- Seluruh 36 pengujian unit di `Release1JourneyStageResolverTest` lulus 100% via `dotnet test`.

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolverTest.cs`

---

### P2-S05: Pembaruan Data Seed & Verifikasi Integrasi End-to-End (`JourneyDalIntegrationTest.cs`)

Title: Pembaruan Seed Data Uji Integrasi dan Validasi End-to-End  
Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memperbaiki metode seed di `JourneyDalIntegrationTest.cs` agar memberikan perbedaan waktu nyata antara `CrtDate` dan `UpdDate`, serta menambahkan assert verifikasi elemen timeline.

Depends On: `P1-S02, P2-S03`  
Repository: `b09-bilreg-api`  

Completion Criteria:  
1. Pada [`JourneyDalIntegrationTest.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/JourneyDalIntegrationTest.cs#L478-L506):
   - Memperbarui helper seed (`SeedReservation`, `SeedOpnameRequest`, `SeedAdmission`) agar `CrtDate` dan `UpdDate` tidak bernilai identik pada rekaman yang mengalami mutasi status.
   - Menambahkan assertion eksplisit pada pengujian pemuatan journey untuk memastikan `CreatedAt` terisi dan timeline menghasilkan stempel waktu yang berbeda antara pembuatan dan realisasi.
2. Pengujian SQL Stage Parity (`Release1JourneySqlStageParityTest.cs`) tetap lulus tanpa regresi.
3. Seluruh test suite pada `Bilreg.Test` lulus 100% (`dotnet test`).

Implementation Notes:  
- Pada `JourneyDalIntegrationTest.cs`:
  - Memperbarui helper seed `SeedOpname`, `SeedReservation`, dan `SeedAdmission` (serta menambahkan alias `SeedOpnameRequest`) dengan parameter opsional `crtDate` dan `updDate`. Untuk rekaman yang mengalami mutasi status (`status != Requested/Reserved/Admitted`), secara otomatis memberikan jeda waktu tetap deterministik (`CrtDate = now.AddMinutes(-5)` dan `UpdDate = now`).
  - Menambahkan assertion eksplisit pada pengujian `Fulfilled_Opname_Reachable_Through_Admission_JourneyId` untuk memvalidasi `CreatedAt` terisi pada `singleBag.Facts.OpnameRequest`, serta memastikan `OpnameRequested` bernilai sebelum `OpnameFulfilled`.
  - Menambahkan assertion eksplisit pada pengujian `Realized_Reservation_Reachable_Through_Admission` untuk memvalidasi `CreatedAt` terisi pada `singleBag.Facts.Reservation`, serta memastikan `ReservationCreated` bernilai sebelum `ReservationRealized`.
  - Menambahkan pengujian integrasi baru `Admission_Updated_Timeline_Has_Accurate_Distinct_Registered_And_Updated_Timestamps` untuk memvalidasi stempel waktu admisi yang diperbarui menghasilkan `AdmissionRegistered` sebelum `AdmissionUpdated`.
- Seluruh 20 pengujian integrasi pada `JourneyDalIntegrationTest` lulus 100% (`20 passed`).
- Seluruh 15 pengujian parity pada `Release1JourneySqlStageParityTest` tetap lulus tanpa regresi (`15 passed`).
- Seluruh 69 unit test pada `AdmisiRanapContext.JourneyFeature` lulus 100% (`69 passed`).

Changed Files:  
- `b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/JourneyDalIntegrationTest.cs`

---

# 6. Change Log

- **2026-10-19**: Dokumen IMPLEMENTATION-PLAN awal disahkan (v1.0) dengan `Execution Approval: APPROVED` berdasarkan target arsitektur `ADM-RANAP-JOURNEY-TIMELINE-ARCHITECTURE.md`.
