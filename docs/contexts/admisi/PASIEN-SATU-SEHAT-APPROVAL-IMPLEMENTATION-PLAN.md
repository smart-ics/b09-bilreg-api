---
Title: Implementation Plan - Integrasi Data Status Satu Sehat Pasien dan Konfirmasi Persetujuan Upload pada Registrasi Admisi
Code: PASIEN-SATU-SEHAT-APPROVAL
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-07
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan arsitektur target yang telah disetujui pada [PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md) (V1.0) untuk merealisasikan CHANGE-REQUEST [PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md) (`ISSUE-ADMISI-PASIEN-SASET-001`):
1. Mengintegrasikan entitas dan tabel data status Satu Sehat pasien (`tc_mr_saset`) ke dalam agregat data sosial pasien di sistem Bilreg (`b09-bilreg-api`) dengan strategi *Upsert*.
2. Memperluas kontrak query data pasien (`PasienGetQuery`) untuk menyertakan data status Satu Sehat.
3. Menyediakan use case command `PasienApproveUploadSasetCmd` dan endpoint HTTP PATCH `/api/pasien/approveUploadSaset`.
4. Menampilkan indikator visual status `#SatuSehat` dengan pewarnaan kondisional pada kartu data pasien di layar registrasi admisi (`c012_myhospital_web`).
5. Mengimplementasikan interseptor pra-simpan registrasi pada antarmuka loket admisi yang melakukan lookup grup jaminan ke JetliApi (dengan client-side caching), otomatisasi persetujuan upload untuk pasien BPJS, pemunculan pop-up dialog konfirmasi untuk pasien Non-BPJS, serta proteksi toleransi kesalahan (*fail-safe non-blocking*).

Referenced artifacts:

- ARCHITECTURE: [PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md) (V1.0)
- FEASIBILITY-ASSESSMENT: [PASIEN-SATU-SEHAT-APPROVAL-FEASIBILITY-ASSESSMENT.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-FEASIBILITY-ASSESSMENT.md) (Status: READY-FOR-PLANNING)
- ISSUE: [PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md) (`ISSUE-ADMISI-PASIEN-SASET-001`)
- DOMAIN Rajal: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Target repositories:
- `b09-bilreg-api` (Backend .NET 8.0: Domain, Infrastructure, Application, API, dan Unit Tests)
- `c012_myhospital_web` (Frontend Vue 3 + TypeScript: BillingBase Types/Services, Admisi Components/Composables, dan Unit Tests)

In scope:
- Domain model `PasienSasetModel` dan penambahan properti/behavior di `PasienModel`.
- Dapper DAL `IPasienSasetDal`, DTO `PasienSasetDto`, dan implementasi `PasienSasetDal` untuk tabel `tc_mr_saset`.
- Orkestrasi persistensi *Upsert* dan pemuatan data Satu Sehat pada `PasienRepo`.
- Perluasan response record `PasienGetResponse` pada `PasienGetQuery.cs`.
- Command MediatR `PasienApproveUploadSasetCmd` dan rute controller HTTP PATCH `/api/pasien/approveUploadSaset`.
- Skema Zod `dataSosialPasienSchemaNew` dan tipe `DataSosialPasienNew` pada frontend `pasien.ts`.
- Mutation hook TanStack Query `useApproveUploadSaset` pada `PasienService.ts`.
- Visual badge `#SatuSehat` dengan warna kondisional pada `LegacyRegistrationWorkspace.vue`.
- Komponen dialog konfirmasi persetujuan Satu Sehat `DialogPersetujuanSatuSehat.vue` untuk pasien Non-BPJS.
- Orkestrasi pra-simpan registrasi di `useRegistrasiActions.ts` (evaluasi flag `isApprovedUpload`, cache lookup JetliApi, auto-approval BPJS, dialog konfirmasi Non-BPJS, serta isolasi exception non-blocking).
- Pengujian unit komprehensif pada backend dan frontend.

Out of scope:
- Pengiriman/transmisi payload FHIR/HL7 ke server Cloud Kemenkes Satu Sehat (dikelola oleh service background/integrator terpisah di luar Bilreg).
- Penanganan persetujuan *View* (`IsApprovedView`) maupun berkas fisik general consent (`FileGeneralConcentUpload` / `FileGeneralConcentView`).
- Perubahan alur kasir, billing, farmasi, atau laboratorium.

---

# 3. Dependencies

External dependencies:
- Tabel operasional `tc_mr_saset` telah ada di database operasional SQL Server dengan kolom: `KodeMr`, `KodeSaset`, `IsApprovedUpload`, `TglJamApprovedUpload`, `FileGeneralConcentUpload`, `IsApprovedView`, `TglJamApprovedView`, `FileGeneralConcentView`.
- Service `jetliApi` aktif dan dapat diakses melalui base URL yang terkonfigurasi di `global_config.json` (`/JetliAPi/api/GrupJaminan/map`).

Slice dependencies:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID yang valid dalam rencana ini.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED` dan artefak keluaran terverifikasi di repositori terkait.
- Kepuasan dependensi tidak mensyaratkan status review `GO`.
- Slices yang tidak memiliki dependensi satu sama lain dapat dikerjakan secara paralel (Dependency-Driven Parallelism).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Backend Domain & Persistence Foundation | IMPLEMENTED | GO | 3/3 |
| P2 - Backend Application & API Endpoints | IMPLEMENTED | GO | 2/2 |
| P3 - Frontend Data Contract & Service Layer | IMPLEMENTED | GO | 2/2 |
| P4 - Frontend UI & Admisi Workflow Orchestration | IMPLEMENTED | GO | 3/3 |

---

# 5. Phases

## P1 - Backend Domain & Persistence Foundation

Implementation Status: IMPLEMENTED  
Review Status: GO  

Fase ini meletakkan fondasi entitas domain, data access layer (DAL), dan orkestrasi repositori persistensi untuk tabel `tc_mr_saset` di repositori backend `b09-bilreg-api`.

### P1-S01

Title: Domain Model PasienSasetModel & Integrasi Agregat PasienModel

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Membuat kelas model domain `PasienSasetModel` dan mengintegrasikannya ke dalam agregat root `PasienModel` beserta perilaku mutasi persetujuan upload (`ApproveUploadSaset`).

Depends On: None  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- File `src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienSasetModel.cs` dibuat dengan properti: `PasienId`, `SasetId`, `IsApprovedUpload`, `TglJamApprovedUpload`, `FileGeneralConcentUpload`, `IsApprovedView`, `TglJamApprovedView`, `FileGeneralConcentView`.
- Factory method `PasienSasetModel.Default(string pasienId = "-")` mengembalikan entitas dengan nilai default yang aman (`"-"`, `false`, `new DateTime(3000, 1, 1)`).
- Method `ApproveUpload(DateTime approvedAt)` mengubah `IsApprovedUpload = true` dan mengeset `TglJamApprovedUpload`.
- File `src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienModel.cs` diperluas dengan properti `PasienSasetModel PasienSaset { get; private set; }`, method `SetSaset(PasienSasetModel saset)`, dan method `ApproveUploadSaset(DateTime approvedAt)`.
- Unit test `PasienSasetModelTest` dan pembaruan `PasienModelTest` di `src/test/Bilreg.Domain.Test` terverifikasi lulus (PASS).

Implementation Notes:  
- Membuat kelas `PasienSasetModel` di `src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienSasetModel.cs` dengan seluruh properti, factory method `Default(string pasienId = "-")`, dan method mutasi `ApproveUpload(DateTime approvedAt)`.
- Memperluas agregat `PasienModel` di `src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienModel.cs` dengan properti `PasienSaset`, inisialisasi default pada konstruktor untuk menjamin non-null safe default, serta method `SetSaset(PasienSasetModel saset)` dan `ApproveUploadSaset(DateTime approvedAt)`.
- Membuat unit test suite `PasienSasetModelTest` (4 test cases) dan `PasienModelTest` (7 test cases) di `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/`. Seluruh 11 test terverifikasi lulus (PASS).

Changed Files:
- `src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienSasetModel.cs` (created)
- `src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienModel.cs` (modified)
- `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienSasetModelTest.cs` (created)
- `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienModelTest.cs` (created)

---

### P1-S02

Title: Infrastructure DAL & DTO tc_mr_saset

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Membuat interface `IPasienSasetDal`, DTO `PasienSasetDto`, dan implementasi `PasienSasetDal` berbasis Dapper untuk mengeksekusi operasi CRUD pada tabel `tc_mr_saset`.

Depends On: P1-S01  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- File `src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienSasetDto.cs` dibuat memetakan kolom `tc_mr_saset`: `KodeMr`, `KodeSaset`, `IsApprovedUpload`, `TglJamApprovedUpload`, `FileGeneralConcentUpload`, `IsApprovedView`, `TglJamApprovedView`, `FileGeneralConcentView` dengan method `FromModel` dan `ToModel`.
- Interface `IPasienSasetDal` dibuat mengimplementasikan `IInsert<PasienSasetDto>`, `IUpdate<PasienSasetDto>`, `IDelete<IPasienKey>`, dan `IGetData<PasienSasetDto, IPasienKey>`.
- File `src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienSasetDal.cs` dibuat mengimplementasikan `IPasienSasetDal` menggunakan Dapper SQL queries sesuai spesifikasi TD-03.
- Unit test `PasienSasetDalTest` di `src/test/Bilreg.Infrastructure.Test` terverifikasi lulus (PASS).

Implementation Notes:  
- Membuat DTO `PasienSasetDto` di `src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienSasetDto.cs` yang memetakan seluruh kolom `tc_mr_saset`, dilengkapi factory method `FromModel(PasienSasetModel model)`, konversi `ToModel()`, serta safe fallback `Default`.
- Mendefinisikan interface `IPasienSasetDal` turunan `IInsert<PasienSasetDto>`, `IUpdate<PasienSasetDto>`, `IDelete<IPasienKey>`, dan `IGetData<PasienSasetDto, IPasienKey>`.
- Mengimplementasikan `PasienSasetDal` di `src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienSasetDal.cs` berbasis Dapper parameterized SQL queries sesuai spesifikasi TD-03 (`Insert`, `Update`, `Delete`, `GetData`).
- Membuat unit test suite `PasienSasetDalTest` (4 test cases: Insert, Update, Delete, GetData) dan `PasienSasetDtoTest` (2 test cases: FromModel, ToModel) di `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/`. Seluruh pengujian terverifikasi lulus (PASS).

Changed Files:
- `src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienSasetDto.cs` (created)
- `src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienSasetDal.cs` (created)
- `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienSasetDalTest.cs` (created)
- `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienSasetDtoTest.cs` (created)

---

### P1-S03

Title: Integrasi PasienRepo & Upsert Persistensi tc_mr_saset

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memperbarui `PasienRepo` untuk menginjeksi `IPasienSasetDal`, memuat data Satu Sehat pada `LoadEntity` secara aman, dan mengeksekusi persistensi *Upsert* pada `SaveChanges` di dalam skop transaksi.

Depends On: P1-S01, P1-S02  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- `PasienRepo.cs` menginjeksi `IPasienSasetDal` melalui constructor.
- Method `LoadEntity` memanggil `_pasienSasetDal.GetData(key)`. Jika null, model agregat menginisialisasi `PasienSasetModel.Default(key.PasienId)` sehingga agregat tidak melempar null reference exception.
- Method `SaveChanges` memeriksa keberadaan baris `tc_mr_saset` via DAL; jika ada memanggil `Update`, jika belum ada memanggil `Insert` (*Upsert*) di dalam `using var trans = TransHelper.NewScope()`.
- Method `DeleteEntity` menghapus baris terkait di `tc_mr_saset` via `_pasienSasetDal.Delete(key)`.
- Unit test `PasienRepoTest` di `src/test/Bilreg.Infrastructure.Test` memverifikasi skenario load pasien tanpa data saset, insert saset baru, dan update saset eksisting (PASS).

Implementation Notes:  
- Memperbarui constructor `PasienRepo.cs` untuk menginjeksi dependensi `IPasienSasetDal`.
- Memperbarui method `LoadEntity` untuk memanggil `_pasienSasetDal.GetData(key)` dan menginisialisasi aman via `PasienSasetModel.Default(key.PasienId)` saat data DAL bernilai null, mencegah null reference exception.
- Mengimplementasikan logika persistensi *Upsert* pada `SaveChanges`: mengecek keberadaan baris `tc_mr_saset` via DAL; memanggil `Update` jika sudah ada, atau `Insert` jika belum ada di dalam transaksi `using var trans = TransHelper.NewScope()`.
- Memperbarui method `DeleteEntity` untuk menghapus record terkait di `tc_mr_saset` via `_pasienSasetDal.Delete(key)` di dalam skop transaksi.
- Membuat unit test suite `PasienRepoTest` (6 test cases) di `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienRepoTest.cs` yang memverifikasi skenario load pasien tanpa saset (safe default fallback), load pasien dengan saset, save changes insert saset baru, save changes update saset eksisting, load pasien not found, dan delete entity. Seluruh 6 unit test terverifikasi lulus (PASS).

Changed Files:
- `src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienRepo.cs` (modified)
- `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienRepoTest.cs` (created)

Notes:  
Pola *Upsert* menjamin interoperabilitas aman bagi pasien lama yang belum pernah memiliki record di `tc_mr_saset`.

---

## P2 - Backend Application & API Endpoints

Implementation Status: IMPLEMENTED  
Review Status: GO  

Fase ini mengimplementasikan perluasan kontrak query detail data pasien serta use case command dan rute controller untuk persetujuan upload Satu Sehat.

### P2-S04

Title: Kontrak Query Pasien (PasienGetQuery & PasienGetResponse)

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Memperluas kontrak respons query detail data pasien (`PasienGetResponse`) untuk memuat atribut data status Satu Sehat.

Depends On: P1-S03  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- Record `PasienSasetResponse(string SasetId, bool IsApprovedUpload, string TglJamApprovedUpload, string FileGeneralConcentUpload, bool IsApprovedView, string TglJamApprovedView, string FileGeneralConcentView)` ditambahkan pada `src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienGetQuery.cs`.
- Record `PasienGetResponse` diperluas dengan field `PasienSasetResponse PasienSaset`.
- Method `BuildPasienResponse` di `PasienGetHandler` memetakan data dari `pasien.PasienSaset` ke record `PasienSasetResponse`.
- Unit test `PasienGetHandlerTest` di `src/test/Bilreg.Application.Test` terverifikasi lulus dan memvalidasi mapping data Satu Sehat (PASS).

Implementation Notes:  
- Menambahkan record `PasienSasetResponse(string SasetId, bool IsApprovedUpload, string TglJamApprovedUpload, string FileGeneralConcentUpload, bool IsApprovedView, string TglJamApprovedView, string FileGeneralConcentView)` pada `src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienGetQuery.cs`.
- Memperluas record `PasienGetResponse` dengan menambahkan field `PasienSasetResponse PasienSaset`.
- Memperbarui method `BuildPasienResponse` di `PasienGetHandler` untuk memetakan agregat model `pasien.PasienSaset` ke record `PasienSasetResponse`, termasuk memformat tanggal ke format ISO `"yyyy-MM-dd HH:mm:ss"` atau string kosong `""` jika default/kosong.
- Membuat unit test suite `PasienGetHandlerTest` (6 test cases) di `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienGetHandlerTest.cs` yang memvalidasi mapping data status Satu Sehat default, mapping data status Satu Sehat disetujui, mutasi approve upload, resolusi kode RS, dan skenario pasien tidak ditemukan. Seluruh 6 unit test terverifikasi lulus (PASS).

Changed Files:
- `src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienGetQuery.cs` (modified)
- `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienGetHandlerTest.cs` (created)

Notes:  
Penyertaan data Satu Sehat dalam query detail pasien memungkinkan aplikasi klien (frontend) mendapatkan status Satu Sehat dalam satu roundtrip jaringan.

---

### P2-S05

Title: Command PasienApproveUploadSasetCmd & Endpoint Controller

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Membuat use case command MediatR `PasienApproveUploadSasetCmd`, handler eksekusi, rute controller HTTP PATCH `/api/pasien/approveUploadSaset`, serta registrasi dependency injection `IPasienSasetDal`.

Depends On: P1-S03  

Repository: `b09-bilreg-api`  

Completion Criteria:  
- File `src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienApproveUploadSasetCmd.cs` dibuat dengan record command `PasienApproveUploadSasetCmd(string PasienId) : IRequest, IPasienKey` dan handler `PasienApproveUploadSasetHandler`.
- Handler memuat model via `_pasienRepo.LoadEntity`, memanggil `pasien.ApproveUploadSaset(DateTime.Now)`, dan menyimpan perubahan via `_pasienRepo.SaveChanges(pasien)`.
- Rute `[HttpPatch("approveUploadSaset")]` ditambahkan ke `src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs` dan mengembalikan `Ok(new JSendOk("Done"))`.
- `IPasienSasetDal` didaftarkan ke DI container di `Bilreg.Api` / `Bilreg.Infrastructure`.
- Unit test `PasienApproveUploadSasetHandlerTest` dan `PasienControllerTest` terverifikasi lulus (PASS).

Implementation Notes:  
- Membuat command MediatR `PasienApproveUploadSasetCmd(string PasienId) : IRequest, IPasienKey` dan handler `PasienApproveUploadSasetHandler` di `src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienApproveUploadSasetCmd.cs`. Handler memvalidasi parameter, memuat agregat pasien via `_pasienRepo.LoadEntity`, menjalankan mutasi domain `pasien.ApproveUploadSaset(DateTime.Now)`, dan menyimpannya melalui `_pasienRepo.SaveChanges(pasien)`.
- Menambahkan endpoint HTTP PATCH `approveUploadSaset` pada `src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs` yang mengeksekusi MediatR command dan mengembalikan response `Ok(new JSendOk("Done"))`.
- Mendaftarkan interface `IPasienSasetDal` dan implementasi `PasienSasetDal` ke DI container pada `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs`.
- Membuat unit test suite `PasienApproveUploadSasetHandlerTest` (4 test cases: valid request with default saset, valid request with existing saset, empty PasienId guard validation, and patient not found exception) dan `PasienControllerTest` (2 test cases: successful mediator execution returning JSendOk "Done", and mediator exception propagation) di `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/`. Seluruh pengujian terverifikasi lulus (PASS).

Changed Files:
- `src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienApproveUploadSasetCmd.cs` (created)
- `src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs` (modified)
- `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs` (modified)
- `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienApproveUploadSasetHandlerTest.cs` (created)
- `src/bilreg/Bilreg.Test/PasienContext/PasienFeature/PasienControllerTest.cs` (created)

Notes:  
Format return `JSendOk("Done")` konsisten dengan endpoint mutasi sederhana pasien lainnya (`addContact`, `nonActive`, dll.).

---

## P3 - Frontend Data Contract & Service Layer

Implementation Status: IMPLEMENTED  
Review Status: GO  

Fase ini memperbarui skema tipe Zod dan service API klien pada modul `BillingBase` di repositori frontend `c012_myhospital_web`.

### P3-S06

Title: Pembaruan Tipe & Skema Zod Pasien (pasien.ts)

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan skema validasi Zod dan antarmuka TypeScript untuk objek `pasienSaset` pada skema agregat data pasien serta payload command persetujuan upload Satu Sehat.

Depends On: P2-S04, P2-S05  

Repository: `c012_myhospital_web`  

Completion Criteria:  
- File `src/modules/BillingBase/types/pasien.ts` diperbarui: properti `pasienSaset` ditambahkan ke `dataSosialPasienSchemaNew` dengan sub-field `sasetId`, `isApprovedUpload`, `tglJamApprovedUpload`, `fileGeneralConcentUpload`, `isApprovedView`, `tglJamApprovedView`, `fileGeneralConcentView` (nullable & optional).
- Tipe `DataSosialPasienNew` terefleksi secara otomatis memuat properti `pasienSaset`.
- Skema payload `payloadApproveUploadSasetSchema = z.object({ pasienId: z.string() })` dan tipe `PayloadApproveUploadSaset` diekspor.
- Unit test validasi skema Zod (jika ada di modul BillingBase) terverifikasi lulus (PASS).

Implementation Notes:  
- Menambahkan Zod schema `pasienSasetSchema` dengan properti `sasetId`, `isApprovedUpload`, `tglJamApprovedUpload`, `fileGeneralConcentUpload`, `isApprovedView`, `tglJamApprovedView`, dan `fileGeneralConcentView` (nullable & optional) serta tipe `PasienSaset`.
- Memperbarui `dataSosialPasienSchemaNew` di `src/modules/BillingBase/types/pasien.ts` dengan properti `pasienSaset: pasienSasetSchema.nullable().optional()`, yang secara otomatis merefleksikan perubahan ke tipe TypeScript `DataSosialPasienNew`.
- Menambahkan Zod schema `payloadApproveUploadSasetSchema = z.object({ pasienId: z.string() })` dan mengekspor tipe `PayloadApproveUploadSaset`.
- Membuat unit test suite `pasien.spec.ts` (9 test cases) di `src/modules/BillingBase/types/__tests__/pasien.spec.ts` untuk memvalidasi skema Zod `pasienSasetSchema`, kompatibilitas backward dan format data pada `dataSosialPasienSchemaNew`, serta skema payload `payloadApproveUploadSasetSchema`.
- Menjalankan pengujian vitest (`npx vitest run src/modules/BillingBase`) dan `npm run type-check`; seluruh pengujian (103/103 tests) dan pengecekan tipe TypeScript terverifikasi lulus (PASS).

Changed Files:
- `src/modules/BillingBase/types/pasien.ts` (modified)
- `src/modules/BillingBase/types/__tests__/pasien.spec.ts` (created)

Notes:  
Menggunakan validasi Zod menjamin keamanan runtime data yang diterima dari backend.

---

### P3-S07

Title: Mutation Hook & Service Pasien (PasienService.ts)

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menambahkan konstanta service key, mutation hook TanStack Query `useApproveUploadSaset`, dan direct fetcher pada `PasienService.ts`.

Depends On: P3-S06  

Repository: `c012_myhospital_web`  

Completion Criteria:  
- Konstanta `APPROVE_UPLOAD_SASET: 'pasien.approve-upload-saset'` ditambahkan ke `PASIEN_SERVICE_KEYS` di `src/modules/BillingBase/services/PasienService.ts`.
- Hook `useApproveUploadSaset()` dibuat menggunakan `createPatchMutationFn` mengarah ke endpoint `pasien/approveUploadSaset` dengan apiName `bilregApi`.
- Pada `onSuccess`, mutasi melakukan invalidasi cache `queryKeys.pasien.all`.
- Fungsi pembantu / direct fetcher diekspor bersama return service composable.
- Unit test untuk `PasienService` terverifikasi lulus (PASS).

Implementation Notes:  
- Menambahkan konstanta `APPROVE_UPLOAD_SASET: 'pasien.approve-upload-saset'` pada `PASIEN_SERVICE_KEYS` di `src/modules/BillingBase/services/PasienService.ts`.
- Mengimpor `payloadApproveUploadSasetSchema` dan `type PayloadApproveUploadSaset` dari `@/modules/BillingBase/types/pasien`.
- Mengimplementasikan mutation hook `useApproveUploadSaset()` yang mengarahkan mutasi PATCH ke `${API_ENDPOINT.PASIEN}/approveUploadSaset` dengan `apiName: 'bilregApi'`, serta melakukan cache invalidation `queryKeys.pasien.all` pada `onSuccess`.
- Mengimplementasikan fungsi pembantu / direct fetcher `approveUploadSaset(payload)` yang mengeksekusi direct PATCH request via `createPatchMutationFn`.
- Mengekspor `useApproveUploadSaset` dan `approveUploadSaset` pada return object dari composable `usePasienService()`.
- Membuat unit test suite `PasienService.spec.ts` (6 test cases) di `src/modules/BillingBase/services/__tests__/PasienService.spec.ts` untuk memverifikasi definisi service keys, konfigurasi mutation hook, invokasi endpoint PATCH via `bilregApi`, cache invalidation, direct fetcher execution, serta ekspos antarmuka composable.
- Menjalankan pengujian vitest (`npm run test:unit -- run src/modules/BillingBase`) dengan hasil 109/109 tests lulus (PASS) dan `npm run tc` dengan 0 error.

Changed Files:
- `src/modules/BillingBase/services/PasienService.ts` (modified)
- `src/modules/BillingBase/services/__tests__/PasienService.spec.ts` (created)

Notes:  
Konsisten dengan arsitektur TanStack Vue Query dan factory mutation frontend MyHospital.

---

## P4 - Frontend UI & Admisi Workflow Orchestration

Implementation Status: IMPLEMENTED  
Review Status: GO  

Fase ini mengimplementasikan visualisasi status indikator, komponen pop-up dialog konfirmasi, dan interseptor pra-simpan registrasi cerdas yang tahan terhadap kegagalan jaringan di modul Admisi.

### P4-S08

Title: Visualisasi Indikator #SatuSehat pada LegacyRegistrationWorkspace.vue

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Menampilkan visual badge `#SatuSehat` dengan status warna kondisional pada kartu data pasien di antarmuka registrasi loket admisi.

Depends On: P3-S06  

Repository: `c012_myhospital_web`  

Completion Criteria:  
- File `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue` diperbarui pada bagian template kartu data pasien.
- Menampilkan teks/badge `#SatuSehat`:
  - Jika `pasienDetail?.pasienSaset?.isApprovedUpload === true`, kelas CSS menggunakan `text-emerald-600 font-semibold`.
  - Jika `pasienDetail?.pasienSaset?.isApprovedUpload` bernilai false, null, atau undefined, kelas CSS menggunakan `text-slate-400 font-normal`.
- Ditambahkan tooltip penjelasan status persetujuan Satu Sehat pada elemen indikator.
- Tampilan kartu pasien tetap rapi, responsif, dan konsisten dengan antarmuka yang ada.

Implementation Notes:  
- Menambahkan teks/badge `#SatuSehat` pada template kartu data pasien di samping `GenderIcon` pada `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue`.
- Menerapkan styling kondisional menggunakan utility helper `cn`: `text-emerald-600 font-semibold` jika `pasienDetail?.pasienSaset?.isApprovedUpload === true`, dan `text-slate-400 font-normal` jika bernilai false, null, atau undefined.
- Membungkus indikator dengan komponen `TooltipProvider`, `Tooltip`, `TooltipTrigger`, dan `TooltipContent` yang menampilkan pesan status: `"Persetujuan upload Satu Sehat: Disetujui"` ketika aktif atau `"Persetujuan upload Satu Sehat: Belum Disetujui"` ketika belum aktif.
- Menjaga estetika, keterbacaan, dan responsivitas kartu pasien dengan penambahan kelas `shrink-0` dan `cursor-help`.
- Membuat unit test suite `LegacyRegistrationWorkspace.satuSehat.spec.ts` (6 test cases) di `src/modules/Admisi/components/registrationAssistance/__tests__/` yang memvalidasi perenderan badge, conditional class switching, fallback null/undefined, pesan tooltip, serta reaktivitas saat status upload disetujui.
- Menjalankan pengujian vitest (`LegacyRegistrationWorkspace.satuSehat.spec.ts` 6/6 tests PASS, `RegistrasiRajal.spec.ts` 15/15 tests PASS) dan `npm run type-check` dengan 0 error.

Changed Files:
- `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue` (modified)
- `src/modules/Admisi/components/registrationAssistance/__tests__/LegacyRegistrationWorkspace.satuSehat.spec.ts` (created)

Notes:  
Memberikan visibilitas instan kepada petugas loket sebelum memulai proses pengisian registrasi.

---

### P4-S09

Title: Komponen Dialog Konfirmasi Persetujuan Upload Satu Sehat Non-BPJS

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Membuat komponen pop-up dialog konfirmasi persetujuan upload data Satu Sehat untuk pasien non-BPJS di modul Admisi.

Depends On: None  

Repository: `c012_myhospital_web`  

Completion Criteria:  
- Komponen `src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue` dibuat menggunakan komponen dialog modal (Shadcn-vue / Radix / Tailwind).
- Menampilkan judul modal `"Konfirmasi Persetujuan Satu Sehat"`, pesan informed consent (penjelasan pengiriman rekam medis ke Satu Sehat Kemenkes), tombol persetujuan `"Setuju"` / `"Ya"`, dan tombol penolakan `"Batal"` / `"Tidak"`.
- Memancarkan event `confirm` dan `cancel` / `update:open`.
- Komponen dapat diuji secara independen dengan Storybook / Unit Test spec (PASS).

Implementation Notes:  
- Membuat komponen modal dialog `DialogPersetujuanSatuSehat.vue` di `src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue` menggunakan arsitektur `AlertDialog` (Shadcn-vue / Reka-ui / Tailwind CSS).
- Menampilkan judul `"Konfirmasi Persetujuan Satu Sehat"` dengan icon informed consent `ShieldAlert`, deskripsi penjelasan regulasi pengiriman rekam medis ke platform Satu Sehat Kemenkes untuk pasien Non-BPJS, serta panel konteks pasien (`patientName` dan `noRm`).
- Menyediakan tombol aksi persetujuan dengan label default `"Setuju"` (warna `bg-emerald-600`) dan penolakan `"Batal"` (varian `outline`), dengan dukungan custom label (`confirmLabel`, `cancelLabel`) dan state loading (`isPending` dengan spinner `Loader2`).
- Memancarkan event `confirm`, `cancel`, dan `update:open`.
- Membuat unit test suite `DialogPersetujuanSatuSehat.spec.ts` di `src/modules/Admisi/components/registrationAssistance/__tests__/` yang mencakup 9 test cases (perenderan modal informed consent, tombol default Setuju & Batal, konteks identitas pasien, pemancaran event confirm/cancel/update:open, handling pending state, dan custom labels). Seluruh test lulus (PASS: 9/9).
- Menjalankan `npm run type-check` (vue-tsc) tanpa ada type error.

Changed Files:
- `src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue` (created)
- `src/modules/Admisi/components/registrationAssistance/__tests__/DialogPersetujuanSatuSehat.spec.ts` (created)

Notes:  
Dirancang independen sehingga dapat dikontrol secara reaktif oleh composable registrasi.

---

### P4-S10

Title: Interseptor Pra-Simpan Registrasi & Fail-Safe Flow di useRegistrasiActions.ts

Implementation Status: IMPLEMENTED  
Review Status: GO  

Objective:  
Mengintegrasikan alur evaluasi status Satu Sehat, lookup cache grup jaminan JetliApi, auto-approval BPJS, dialog konfirmasi Non-BPJS, serta proteksi non-blocking ke dalam tombol Simpan Registrasi di `useRegistrasiActions.ts`.

Depends On: P3-S07, P4-S08, P4-S09  

Repository: `c012_myhospital_web`  

Completion Criteria:  
- Handler `handleSubmitRegister` di `src/modules/Admisi/composables/useRegistrasiActions.ts` menambahkan alur pra-simpan:
  1. Mengevaluasi `pasienDetail.value?.pasienSaset?.isApprovedUpload`.
  2. Jika bernilai `true`: langsung memproses pendaftaran pasien.
  3. Jika bernilai `false`:
     - Memeriksa mapping grup jaminan via `ApiService` (`apiName: 'jetliApi'`) ke `/GrupJaminan/map?tipeJaminanId=${tipeJaminanId}` dengan client-side cache per `tipeJaminanId` dan timeout singkat.
     - Jika pemanggilan JetliApi gagal atau timeout: skip konfirmasi, catat warning log, dan langsung lanjutkan simpan registrasi (fail-safe).
     - Jika terdeteksi BPJS: otomatis panggil mutasi `useApproveUploadSaset` tanpa memunculkan dialog, perbarui local state pasien, lalu lanjutkan simpan registrasi.
     - Jika terdeteksi Non-BPJS: buka `DialogPersetujuanSatuSehat`. Jika disetujui, panggil `useApproveUploadSaset` lalu simpan registrasi; jika dibatalkan/ditolak, langsung simpan registrasi tanpa memanggil approval.
- Seluruh pemanggilan approval dibungkus blok `try-catch` non-blocking: jika gagal, sistem memunculkan toast warning dan pendaftaran pasien tetap berhasil tersimpan.
- Unit test composable `useRegistrasiActions` terverifikasi lulus (PASS).

Implementation Notes:  
- Menambahkan cache in-memory `grupJaminanCache` (`Map<string, GroupJaminanMap | null>`), helper `clearGrupJaminanCache()`, `isBpjsGroup()`, dan fungsi asinkron `checkIsBpjsJaminan(tipeJaminanId, timeoutMs)` pada `src/modules/Admisi/composables/useRegistrasiActions.ts`.
- Fungsi `checkIsBpjsJaminan` mengimplementasikan timeout 3 detik via `Promise.race`, switching API URL via `ApiService.useUrlApi('jetliApi')` dengan pengembalian ke `bilregApi` di blok `finally`, serta perlindungan *fail-safe* (mengembalikan `null` dan mencatat `console.warn` jika terjadi kegagalan jaringan/timeout).
- Memperluas `RegistrasiActionsContext` dengan field `pasienDetail?: Ref<DataSosialPasienNew | undefined | null>`.
- Mengintegrasikan dialog reactive state `satuSehatDialog` dan flag `openDialogStates.satuSehatConfirmation`, serta handler `requestSatuSehatApproval`, `handleConfirmSatuSehat`, dan `handleCancelSatuSehat`.
- Menerapkan fungsi helper `performApprovalNonBlocking(targetPasienId)` yang memanggil `approveUploadSasetMutation.mutateAsync` (atau direct fetcher `approveUploadSaset`), memperbarui reaktivitas local state `pasienDetail.value.pasienSaset.isApprovedUpload = true`, serta mengisolasi error dengan toast warning non-blocking (`toast.warning('Persetujuan Satu Sehat gagal dicatat, pendaftaran tetap diproses')`).
- Menambahkan alur evaluasi pra-simpan pada `handleSubmitRegister`:
  1. Jika `isApprovedUpload === true`: langsung memproses pendaftaran pasien.
  2. Jika `isApprovedUpload` belum disetujui dan `targetPasienId` ada:
     - Melakukan pengecekan grup jaminan via `checkIsBpjsJaminan`.
     - Jika terdeteksi BPJS: auto-approval via `performApprovalNonBlocking` tanpa membuka dialog konfirmasi.
     - Jika terdeteksi Non-BPJS: menampilkan modal `DialogPersetujuanSatuSehat`; jika disetujui ("Setuju"), memanggil `performApprovalNonBlocking` lalu melanjutkan simpan registrasi; jika ditolak/dibatalkan ("Batal"), langsung melanjutkan simpan registrasi tanpa memanggil approval.
     - Jika fail-safe JetliApi terpicu (`isBpjs === null`): melewati dialog dan approval, langsung melanjutkan simpan registrasi.
- Menghubungkan komponen `DialogPersetujuanSatuSehat` ke dalam template `LegacyRegistrationWorkspace.vue` di dalam blok `RegistrationMutationDialogs`, serta meneruskan `pasienDetail: pasien.pasienDetail` ke composable `useRegistrasiActions`.
- Membuat unit test suite `useRegistrasiActions.satuSehat.spec.ts` (10 test cases) di `src/modules/Admisi/composables/__tests__/` yang mencakup: lookup cache JetliApi, HTTP query & caching, deteksi status Non-BPJS, fail-safe JetliApi timeout/error, auto-approval BPJS, dialog konfirmasi Non-BPJS (Setuju vs Batal), dan isolasi error non-blocking. Seluruh 10 test lulus (PASS: 10/10).
- Memverifikasi pengujian integrasi terkait (`LegacyRegistrationWorkspace.satuSehat.spec.ts`, `DialogPersetujuanSatuSehat.spec.ts`, `RegistrasiRajal.spec.ts`) seluruhnya lulus (PASS), dan `npm run type-check` (vue-tsc) dengan 0 type error.

Changed Files:
- `src/modules/Admisi/composables/useRegistrasiActions.ts` (modified)
- `src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue` (modified)
- `src/modules/Admisi/composables/__tests__/useRegistrasiActions.satuSehat.spec.ts` (created)

Notes:  
Menjamin zero-blockage policy pada operasional loket pendaftaran rumah sakit.

---

# 6. Change Log

- 2026-10-07: V1.0 - Inisialisasi struktur Implementation Plan berdasarkan persetujuan arsitektur [PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE.md). Rencana terdiri dari 4 Fase dan 10 Slices dengan alokasi repositori terpisah (`b09-bilreg-api` dan `c012_myhospital_web`). Execution Approval disetujui (`APPROVED`).
