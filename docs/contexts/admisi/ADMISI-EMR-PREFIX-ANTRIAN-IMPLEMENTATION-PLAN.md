---
Title: Implementation Plan - Pengiriman Prefix Antrian Rawat Jalan Berdasarkan Master Ruang ke EMR25 Dashboard
Code: ADMISI-EMR-PREFIX-ANTRIAN
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-24
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan arsitektur target yang telah disetujui pada [ADMISI-EMR-PREFIX-ANTRIAN-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-EMR-PREFIX-ANTRIAN-ARCHITECTURE.md) (V1.0) untuk merealisasikan CHANGE-REQUEST [ADMISI-EMR-PREFIX-ANTRIAN-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-EMR-PREFIX-ANTRIAN-ISSUE.md) (`ISSUE-BILREG-EMR-PREFIX-ANTRIAN-001`):

Menyertakan atribut kode prefix antrian (`PrefixAntrian`) dari master ruang lokal `HiDok_Ruang` melalui jadwal praktek dokter pada saat pendaftaran Rawat Jalan (Walk-In dan By Booking) dikirimkan ke dasbor EMR25 (`a054_emr25_api`) melalui endpoint `POST api/Dashboard/addReg`, dengan mempertahankan mekanisme penyimpanan lokal *Transactional Outbox* (`BILRG_EmrAntrianOutboundQueue`) tanpa penambahan kolom/tabel fisik database baru, serta memastikan alur Rawat Darurat (IGD) tetap terisolasi dengan nilai default kosong (`""`).

Referenced artifacts:

- ARCHITECTURE: [ADMISI-EMR-PREFIX-ANTRIAN-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-EMR-PREFIX-ANTRIAN-ARCHITECTURE.md) (V1.0)
- FEASIBILITY-ASSESSMENT: [ADMISI-EMR-PREFIX-ANTRIAN-FEASIBILITY-ASSESSMENT.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-EMR-PREFIX-ANTRIAN-FEASIBILITY-ASSESSMENT.md) (Status: READY-FOR-PLANNING)
- ISSUE: [ADMISI-EMR-PREFIX-ANTRIAN-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADMISI-EMR-PREFIX-ANTRIAN-ISSUE.md) (`ISSUE-BILREG-EMR-PREFIX-ANTRIAN-001`)
- DOMAIN: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Target repository:
- `b09-bilreg-api` (Seluruh fase dan slice menargetkan satu repositori ini).

In scope (bagian arsitektur yang direalisasikan):
- Penambahan parameter opsional `string PrefixAntrian = ""` pada positional record `AddAntrianEmrByRegCommand` di `Bilreg.Application`.
- Serialisasi dan deserialisasi outbox JSON (`EmrAntrianOutboundPayloadBuilder`) dengan dukungan `prefixAntrian` serta verifikasi backward-compatibility terhadap data antrian lama.
- Ekstraksi, normalisasi defensif, dan pemetaan `PrefixAntrian` pada use case pendaftaran Rawat Jalan Walk-In (`RegJalanWalkInCommand`).
- Resolusi jadwal booking dokter, normalisasi defensif, dan pemetaan `PrefixAntrian` pada use case pendaftaran Rawat Jalan By Booking (`RegJalanByBookingCmd`).
- Verifikasi isolasi alur Rawat Darurat IGD (`RegDaruratCreateCmd`) yang tetap menggunakan nilai default string kosong `""`.
- Pembuatan suite unit test baru dan pembaruan unit test eksisting pada `Bilreg.Test` untuk menjamin verifikasi 100% lulus tanpa regresi.

Out of scope (arsitektur §3 Excluded):
- Penambahan kolom fisik atau tabel baru pada database SQL Server lokal (`BILRG_EmrAntrianOutboundQueue`, `ta_registrasi`, `ta_no_antrian_map`, dsb.).
- Perubahan pada codebase servis EMR25 (`a054_emr25_api`), karena EMR25 sudah siap menerima `PrefixAntrian`.
- Pengiriman prefix antrian untuk alur IGD (layanan IGD tidak menggunakan prefix antrian).
- Perubahan alur pengiriman worker `EmrAntrianOutboundProcessor`.

---

# 3. Dependencies

External dependencies:
- API EMR25 (`a054_emr25_api`) mendukung parameter opsional `PrefixAntrian` pada `POST api/Dashboard/addReg`.
- Tabel `HiDok_Ruang` telah memuat data master ruangan dan prefix antrian.

Slice dependencies:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID yang valid dalam rencana ini.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED` dan artefak keluaran terverifikasi di repositori.
- Kepuasan dependensi tidak mensyaratkan status review `GO`.
- Slices yang tidak memiliki dependensi satu sama lain dapat dikerjakan secara paralel (Dependency-Driven Parallelism).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Foundation & Contract | IMPLEMENTED | GO | 1/1 |
| P2 - Application Layer Integration | IMPLEMENTED | GO | 3/3 |

---

# 5. Phases

## P1 - Foundation & Contract

Implementation Status: IMPLEMENTED  
Review Status: GO  

Fase ini meletakkan fondasi kontrak DTO antrian EMR pada layer Application serta memastikan serializer JSON outbox queue mendukung properti `prefixAntrian` secara backward-compatible.

### P1-S01

Title: Penambahan Properti PrefixAntrian pada AddAntrianEmrByRegCommand dan Serialisasi Outbox JSON

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan parameter opsional ke-10 `string PrefixAntrian = ""` pada positional record `AddAntrianEmrByRegCommand` di [IDashboardEmrAddRegService.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IDashboardEmrAddRegService.cs). Memverifikasi bahwa serialisasi dan deserialisasi JSON outbox pada [EmrAntrianOutboundPayloadBuilder.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundPayloadBuilder.cs) memproses properti `prefixAntrian` dengan benar, serta membuat unit test untuk memvalidasi round-trip serialisasi data baru dan backward-compatibility data lama yang belum memiliki field `prefixAntrian`.

Depends On: None  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- `AddAntrianEmrByRegCommand` pada `IDashboardEmrAddRegService.cs` memiliki 10 parameter dengan parameter ke-10: `string PrefixAntrian = ""`.
- Serialisasi `AddAntrianEmrByRegCommand` via `EmrAntrianOutboundPayloadBuilder.SerializeReg` menghasilkan JSON dengan property camelCase `"prefixAntrian"`.
- Deserialisasi string JSON lama yang tidak memuat `"prefixAntrian"` via `EmrAntrianOutboundPayloadBuilder.DeserializeReg` berhasil mengekstrak objek dengan `PrefixAntrian == ""`.
- Unit test baru di `Bilreg.Test/AdmisiContext/EmrAntrianOutboundFeature/` memverifikasi round-trip serialisasi baru dan deserialisasi payload lama dengan hasil lulus (*passed*).
- Seluruh proyek dapat di-build tanpa error (`dotnet build`).

Notes:  
Mengacu pada keputusan teknis TD-01 dan TD-05. Default parameter value `""` wajib ada di akhir konstruktor untuk memastikan *zero breaking change*.

---

## P2 - Application Layer Integration

Implementation Status: IMPLEMENTED  
Review Status: GO  

Fase ini mengintegrasikan pengambilan data prefix master ruang dari jadwal dokter pada use case Rawat Jalan Walk-In dan By Booking, memverifikasi isolasi alur IGD, serta memvalidasi pengujian regresi seluruh solusi.

### P2-S02

Title: Integrasi Prefix Ruang pada Registrasi Rawat Jalan Walk-In (RegJalanWalkInCommand)

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Pada [RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs), ekstraksi nilai `PrefixAntrian` dari objek `schedule.LegacyJadwal.Ruang` / `schedule.Effective.Ruang`, terapkan normalisasi defensif (trim dan batasan maksimal 2 karakter sesuai TD-02), dan sertakan nilai tersebut saat menginstansiasi `AddAntrianEmrByRegCommand` sebelum di-enqueue ke outbox.

Depends On: P1-S01  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- `RegJalanWalkInCommand.cs` mengambil nilai prefix dari jadwal walk-in, menormalisasinya (trim, fallback ke string kosong bila null, substring max 2 karakter), dan mengisi parameter `PrefixAntrian` pada `emrPayload`.
- Unit test pada `RegJalanCreateHandlerTest.cs` diperbarui/ditambahkan untuk memverifikasi bahwa outbox enqueue menerima payload dengan nilai `PrefixAntrian` yang sesuai dengan master ruang.
- Eksekusi pengujian `dotnet test --filter FullyQualifiedName~RegJalanCreateHandlerTest` menghasilkan 100% passed.

Notes:  
Mengacu pada TD-02 dan TD-03. Tidak ada query database tambahan karena objek `schedule` sudah membawa data `RuangType`.

---

### P2-S03

Title: Integrasi Resolusi Jadwal dan Prefix Ruang pada Registrasi Rawat Jalan By Booking (RegJalanByBookingCmd)

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Pada [RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs), resolusikan jadwal dokter booking memanfaatkan `BookingScheduleResolver.Resolve` dengan tanggal berobat, jam praktek, dan dokter dari `BookingModel`. Ekstrak nilai `PrefixAntrian` dari ruang jadwal tersebut, terapkan normalisasi defensif, dan sertakan nilai tersebut saat menginstansiasi `AddAntrianEmrByRegCommand` sebelum di-enqueue ke outbox.

Depends On: P1-S01  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- `RegJalanByBookingCmd.cs` me-resolve jadwal dokter booking, mengekstrak prefix ruang, menormalisasinya secara defensif, dan mengisi parameter `PrefixAntrian` pada `emrPayload`.
- Unit test pada `RegJalanByBookingHandlerTest.cs` diperbarui/ditambahkan untuk memverifikasi bahwa outbox enqueue menerima payload dengan nilai `PrefixAntrian` yang sesuai dengan master ruang dokter booking.
- Eksekusi pengujian `dotnet test --filter FullyQualifiedName~RegJalanByBookingHandlerTest` menghasilkan 100% passed.

Notes:  
Mengacu pada TD-02 dan TD-04. Dependensi yang diperlukan untuk resolusi jadwal (`_featureResolver` / `_jadwalRepo`) disesuaikan pada handler jika belum terinjeksi.

---

### P2-S04

Title: Verifikasi Isolasi Registrasi IGD dan Pengujian Regresi Solusi

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memverifikasi bahwa pendaftaran Rawat Darurat IGD pada [RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs) tetap memanggil `AddAntrianEmrByRegCommand` dengan nilai default `PrefixAntrian = ""` tanpa lookup ke ruang, serta menjalankan pengujian regresi penuh terhadap seluruh test suite `Bilreg.Test` guna membuktikan tidak ada efek samping atau kegagalan kompilasi.

Depends On: P2-S02, P2-S03  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- `RegDaruratCreateCmd.cs` terkonfirmasi memanggil `AddAntrianEmrByRegCommand` dengan `PrefixAntrian` string kosong `""`.
- Seluruh suite pengujian `Bilreg.Test` dijalankan (`dotnet test`) dan menghasilkan 100% passed (0 failed).
- Tidak ada breaking change terhadap use cases pendaftaran lainnya.

Notes:  
Sesuai arahan bisnis dan arsitektur §3 (Excluded), alur IGD tidak diubah perilakunya.

---

# 6. Change Log

- 2026-10-24: Initial creation of implementation plan for `ADMISI-EMR-PREFIX-ANTRIAN` (V1.0) with Execution Approval APPROVED.
