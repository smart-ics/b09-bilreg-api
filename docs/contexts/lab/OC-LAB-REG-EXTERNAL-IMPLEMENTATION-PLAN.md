---
Title: Registrasi Pasien Eksternal Laboratorium Backend Implementation Plan
Code: OC-LAB-REG-EXTERNAL
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-14
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan realisasi teknis backend untuk fitur **Registrasi Pasien Eksternal Laboratorium** pada repositori `b09-bilreg-api` sesuai spesifikasi target arsitektur yang telah disahkan pada [OC-LAB-REG-EXTERNAL-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-ARCHITECTURE.md).

Tujuan operasional backend:
1. Membentuk fitur bounded baru `LabExternalFeature` di `Bilreg.Domain`, `Bilreg.Application`, `Bilreg.Infrastructure`, `Bilreg.SqlDb`, dan `Bilreg.Api`.
2. Menyimpan fakta pendaftaran mandiri pasien luar secara persisten pada tabel `BILRG_LabExternal`.
3. Menerbitkan nomor rekam medis operasional sekali pakai (`ExternalMrNo`) dengan format `EXT-{yyyyMM}-{00000}` via `ISequencer`.
4. Menginisialisasi akun penagihan langsung (*cash-first billing account*) pada `TataRekeningModel` secara atomik saat pendaftaran disimpan (`RegId = RegExternalId`).
5. Memperbaiki alur pembebanan biaya pada `LabOrderChargeCmd` (`GAP-002`) agar mengenali pasien luar dan menagih ke `TataRekening` tanpa mencari ke `_regRepo` (mencegah `KeyNotFoundException`).
6. Menegakkan aturan pembatalan non-destructive: menolak pembatalan jika tagihan kasir telah lunas (`EX-01`) atau spesimen telah diambil (`EX-02`).
7. Menyediakan REST API endpoint terstruktur dengan envelope `JSendOk` untuk kebutuhan integrasi frontend.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-LAB-REG-EXTERNAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-FEATURE.md)
- ARCHITECTURE: [OC-LAB-REG-EXTERNAL-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-ARCHITECTURE.md)
- FEASIBILITY-ASSESSMENT: [OC-LAB-REG-EXTERNAL-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-FEASIBILITY-ASSESSMENT.md)
- ISSUE: [OC-LAB-REG-EXTERNAL-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-ISSUE.md) (`ISSUE-LAB-REG-EXTERNAL-001`)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Target repository:
- `b09-bilreg-api` (Seluruh irisan implementasi / slice pada rencana ini menargetkan repositori backend BILREG API).

Sesuai arahan pengguna (**Opsi 1 - Khusus Backend**), implementasi frontend pada `c012_myhospital_web` dijadwalkan pada siklus perencanaan terpisah setelah seluruh irisan backend pada rencana ini berstatus `COMPLETED` dan terverifikasi `GO`.

In Scope:
1. **Database DDL**: Pembuatan skrip `BILRG_LabExternal.sql` dan pendaftaran pada `Bilreg.SqlDb.sqlproj`.
2. **Domain Layer**: Agregat `LabExternalModel`, antarmuka `ILabExternalKey`, value objects (`Demographics`, `ReferralInfo`, `CancelInfo`), enumerasi (`LabReferralTypeEnum`, `LabExternalStatusEnum`), dan domain unit tests.
3. **Infrastructure Layer**: DTO `LabExternalDto`, DAL `LabExternalDal` berbasis Dapper, repositori `LabExternalRepo` (mengimplementasikan `ILabExternalRepo`), dan registrasi dependency injection pada `InfrastructureService.cs`.
4. **Application Layer (Use Cases & Queries)**:
   - Helper penomoran `LabExternalNumberHelper` (`EXT-{yyyyMM}-{00000}` & `LEX{10}`).
   - Command `LabExternalCreateCmd` & handler dengan pembuatan akun `TataRekeningModel` atomik dalam `TransHelper.NewScope()`.
   - Query `LabExternalGetQuery` & `LabExternalListQuery` (omni-search riwayat).
   - Command `LabExternalCancelCmd` & handler dengan verifikasi guard kasir (`EX-01`) dan spesimen (`EX-02`).
5. **LabOrder Integration Refactoring**:
   - Refaktorisasi `LabOrderCreateExternalPatientCmd` agar mengonsumsi `RegExternalId` dan `ExternalMrNo`.
   - Refaktorisasi `LabOrderChargeCmd` (`GAP-002`) untuk mendukung resolusi penagihan mandiri tanpa ketergantungan `_regRepo`.
6. **Web API Layer**: Controller `LabExternalController.cs` dengan endpoint RESTful dan envelope `JSendOk`.
7. **Automated Testing**: Unit test komprehensif di `Bilreg.Test` mencakup domain invariant, guard clauses, DAL, use cases, dan controller.

Out of Scope (Arsitektur §3 Excluded):
1. Perubahan atau pembuatan kode frontend web (`c012_myhospital_web`).
2. Penulisan atau manipulasi tabel master data pasien rumah sakit (`PAS_Pasien` / `PAS-DATSOS`).
3. Pembuatan episode pendaftaran pada tabel admisi reguler rumah sakit (`BILRG_Reg` / `ADM-REG`).
4. Integrasi bridging klaim BPJS Kesehatan (VClaim).
5. Modifikasi modul kasir rumah sakit secara menyeluruh.

---

# 3. Dependencies

External dependencies:
- Framework .NET 8 / C# 12.
- Pustaka Dapper untuk eksekusi parameterized SQL query.
- MediatR untuk pipeline CQRS (Command & Query handlers).
- Nuna library: `TransHelper` (manajemen transaction scope), `NunaId` (generator ID), `ISequencer` (sequencer nomor urut).
- Ardalis.GuardClauses untuk validasi input fail-fast.
- xUnit, Moq, dan FluentAssertions pada `Bilreg.Test`.

Slice dependency rules:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID yang valid dalam rencana ini.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED`.
- Kepuasan dependensi tidak mensyaratkan status review `GO`.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Foundation (Database DDL & Domain Model) | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P2 - Data Access & Infrastructure Layer | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P3 - Core Application Commands & Queries | NOT-STARTED | NOT-REVIEWED | 0/3 |
| P4 - LabOrder Integration & Dual-Path Charging Refactor | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P5 - Web API Endpoints & Contract Verification | NOT-STARTED | NOT-REVIEWED | 0/1 |

---

# 5. Phases

## P1 - Foundation (Database DDL & Domain Model)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P1-S01

Title: Database DDL BILRG_LabExternal & SqlProj Registration

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Membuat skrip DDL SQL Server untuk tabel fisik `BILRG_LabExternal` lengkap dengan default constraints, clustered primary key, dan non-clustered indexes untuk keunikan `ExternalMrNo` serta pencarian omni-search, dan mendaftarkannya ke dalam `Bilreg.SqlDb.sqlproj`.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:
1. File skrip DDL `b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabExternalFeature/BILRG_LabExternal.sql` dibuat dengan struktur kolom: `RegExternalId VARCHAR(15)`, `ExternalMrNo VARCHAR(20)`, `PatientName VARCHAR(100)`, `Nik VARCHAR(20)`, `BirthDate DATETIME`, `Gender VARCHAR(1)`, `Phone VARCHAR(30)`, `Address VARCHAR(250)`, `ReferralType INT`, `ReferralDoctor VARCHAR(100)`, `ReferralOrigin VARCHAR(100)`, `ClinicalNotes VARCHAR(250)`, `BillingAccountId VARCHAR(15)`, `Status INT`, `CancelReason VARCHAR(250)`, `CancelledUserId VARCHAR(50)`, `CancelledDate DATETIME`, dan kolom audit standard Bilreg (`CrtUser`, `CrtDate`, `UpdUser`, `UpdDate`, `VodUser`, `VodDate`).
2. Terdapat constraint `PK_BILRG_LabExternal` clustered pada `RegExternalId`.
3. Terdapat unique non-clustered index `IX_BILRG_LabExternal_ExternalMrNo` pada `ExternalMrNo`.
4. Terdapat index performa `IX_BILRG_LabExternal_Search` pada `(PatientName, Phone, Status)` dan `IX_BILRG_LabExternal_StatusDate` pada `(Status, CrtDate)`.
5. File `BILRG_LabExternal.sql` didaftarkan pada elemen `<Build Include="...">` di `b09-bilreg-api/src/bilreg/Bilreg.SqlDb/Bilreg.SqlDb.sqlproj`.

Notes:
- Mengikuti pola default constraints yang konsisten di `Bilreg.SqlDb` (`DEFAULT('')`, `DEFAULT('3000-01-01')`, `DEFAULT(0)` / `DEFAULT(1)`).

---

### P1-S02

Title: Domain Aggregate LabExternalModel, Value Objects, Enums & Invariant Tests

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Membangun agregat root domain `LabExternalModel`, antarmuka identitas `ILabExternalKey`, value objects pendukung, enumerasi domain, serta unit test lengkap yang memvalidasi domain invariants (validasi demografi minimal, state transitions `Active` $\rightarrow$ `Completed` / `Cancelled`, penegakan guard `EX-01` pembayaran kasir dan `EX-02` pengambilan spesimen).

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:
1. File antarmuka `ILabExternalKey.cs` dengan properti `string RegExternalId { get; }` dibuat di namespace `Bilreg.Domain.LabContext.LabExternalFeature`.
2. File enumerasi `LabReferralTypeEnum.cs` (`Aps = 1`, `FaskesLuar = 2`) dan `LabExternalStatusEnum.cs` (`Active = 1`, `Completed = 2`, `Cancelled = 3`) dibuat.
3. File value objects dibuat: `LabExternalDemographicsType.cs`, `LabExternalReferralInfoType.cs`, `LabExternalCancelInfoType.cs`.
4. Agregat `LabExternalModel.cs` dibuat dengan factory methods `Create(...)` dan `Load(...)`, metode mutasi berproteksi invariant `Cancel(...)` dan `MarkCompleted(...)`.
5. Invariant `Cancel(...)` melempar `InvalidOperationException` bertanda `EX-01` jika `hasPaidCashier == true`.
6. Invariant `Cancel(...)` melempar `InvalidOperationException` bertanda `EX-02` jika `hasCollectedSpecimen == true`.
7. Unit test `LabExternalModelTest.cs` dibuat di `Bilreg.Test/LabContext/LabExternalFeature/` dan berhasil lulus 100% memverifikasi seluruh skenario validasi, state mutation, dan exception guard.

Notes:
- Murni domain logic, tanpa dependensi infrastruktur atau framework luar.

---

## P2 - Data Access & Infrastructure Layer

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P2-S03

Title: Infrastructure DAL, DTO, Repository Implementation & DI Wiring

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Mengimplementasikan lapisan Data Access Layer (DAL) berbasis Dapper, DTO mapping, dan repositori `LabExternalRepo` yang mengimplementasikan antarmuka `ILabExternalRepo` di Application layer, serta mendaftarkannya pada kontainer dependensi di `InfrastructureService.cs`.

Depends On: P1-S01, P1-S02

Repository: `b09-bilreg-api`

Completion Criteria:
1. Antarmuka `ILabExternalRepo` dibuat di `Bilreg.Application/LabContext/LabExternalFeature/` mewarisi `ISaveChange<LabExternalModel>`, `ILoadEntity<LabExternalModel, ILabExternalKey>`, dan metode query pendukung (`LoadByExternalMrNo`, `ListData`).
2. DTO `LabExternalDto.cs` dibuat di `Bilreg.Infrastructure/LabContext/LabExternalFeature/` dengan mapper `FromModel` dan `ToModel`.
3. Interface DAL `ILabExternalDal.cs` dan implementasi `LabExternalDal.cs` dibuat menggunakan Dapper dengan parameterized SQL (`Insert`, `Update`, `GetData`, `GetByExternalMrNo`, `ListData`).
4. Repositori `LabExternalRepo.cs` dibuat mengorkestrasikan `ILabExternalDal` dan pemetaan DTO.
5. Pendaftaran `ILabExternalDal`, `LabExternalDal`, `ILabExternalRepo`, dan `LabExternalRepo` ditambahkan ke `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/InfrastructureService.cs`.
6. Unit/integration test `LabExternalDalTest.cs` dan `LabExternalRepoTest.cs` di `Bilreg.Test/LabContext/LabExternalFeature/` berhasil lulus memverifikasi CRUD dan pemetaan field database secara presisi.

Notes:
- Menggunakan konvensi Dapper yang sudah ada di `Bilreg.Infrastructure` (`ISqlDatabase`, parameterized queries).

---

## P3 - Core Application Commands & Queries

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P3-S04

Title: Number Sequencer Helper & LabExternalCreateCmd with Atomic TataRekening Integration

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Mengimplementasikan helper generator nomor operasional (`ExternalMrNo` dan `RegExternalId`), command `LabExternalCreateCmd`, dan handler `LabExternalCreateHandler` yang secara atomik membuat entitas pendaftaran eksternal dan akun penagihan `TataRekeningModel` dalam satu transaksi `TransHelper.NewScope()`.

Depends On: P2-S03

Repository: `b09-bilreg-api`

Completion Criteria:
1. Helper `LabExternalNumberHelper.cs` dibuat di Application layer untuk menerbitkan `ExternalMrNo` dengan format `EXT-{yyyyMM}-{00000}` via `ISequencer.GetNextNoUrut($"EXT-{timestamp:yyyyMM}")` dan `RegExternalId` via `NunaId.New("LEX")`.
2. Command `LabExternalCreateCmd` dan DTO response `LabExternalCreateResponse(string RegExternalId, string ExternalMrNo, string BillingAccountId)` dibuat.
3. Handler `LabExternalCreateHandler` mengorkestrasikan:
   - Validasi data masukan via GuardClauses.
   - Penerbitan `RegExternalId` dan `ExternalMrNo`.
   - Pembuatan `TataRekeningModel.Create(regExternalId)` dan persistensi via `ITataRekeningRepo.SaveChanges(...)`.
   - Pembentukan dan persistensi `LabExternalModel` via `ILabExternalRepo.SaveChanges(...)`.
   - Pembungkusan seluruh operasi dalam `TransHelper.NewScope()`.
4. Unit test `LabExternalCreateHandlerTest.cs` di `Bilreg.Test/LabContext/LabExternalFeature/` berhasil memverifikasi pembuatan pendaftaran, pemanggilan sequencer, dan pembentukan akun penagihan kasir.

Notes:
- Memenuhi prinsip *Cash-First* dan menutup celah `OQ-002`.

---

### P3-S05

Title: Query Handlers LabExternalGetQuery & LabExternalListQuery (Omni-Search)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Mengimplementasikan use case pembacaan data pendaftaran eksternal: `LabExternalGetQuery` untuk mendapatkan detail pendaftaran berdasarkan ID, dan `LabExternalListQuery` untuk mendukung fitur omni-search riwayat pasien luar berdasarkan nama, nomor telepon, NIK, atau nomor eksternal.

Depends On: P2-S03

Repository: `b09-bilreg-api`

Completion Criteria:
1. Query `LabExternalGetQuery(string RegExternalId)` dan response DTO `LabExternalDetailResponse` dibuat lengkap dengan informasi demografi, rujukan, billing account, dan status.
2. Query `LabExternalListQuery(string? SearchTerm, DateTime? Date1, DateTime? Date2, int? Status)` dan response DTO list item dibuat.
3. Handler `LabExternalGetHandler` membaca data via `_labExternalRepo` dan mengembalikan detail jika ditemukan atau throw error jika tidak ditemukan.
4. Handler `LabExternalListHandler` mengeksekusi pencarian fleksibel berbasis term (mencocokkan nama pasien, nomor WhatsApp/telepon, NIK, atau `ExternalMrNo`).
5. Unit test `LabExternalQueryHandlersTest.cs` di `Bilreg.Test/LabContext/LabExternalFeature/` memverifikasi pembacaan detail dan pemfilteran hasil pencarian.

Notes:
- Search query mengoptimalkan indeks `IX_BILRG_LabExternal_Search` dan `IX_BILRG_LabExternal_StatusDate`.

---

### P3-S06

Title: LabExternalCancelCmd Handler with Financial & Specimen Guard Checks (EX-01 & EX-02)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Mengimplementasikan use case pembatalan pendaftaran eksternal `LabExternalCancelCmd` yang memverifikasi pra-kondisi ketiadaan transaksi pembayaran kasir di `TataRekening` (`EX-01`) dan ketiadaan spesimen yang telah diambil pada order lab (`EX-02`) sebelum memicu domain method `Cancel(...)`.

Depends On: P3-S04

Repository: `b09-bilreg-api`

Completion Criteria:
1. Command `LabExternalCancelCmd(string RegExternalId, string Reason, string UserId)` dan response DTO `LabExternalCancelResponse(bool Success, string Message)` dibuat.
2. Handler `LabExternalCancelHandler`:
   - Membaca agregat `LabExternalModel` dari `_labExternalRepo`.
   - Membaca `TataRekeningModel` dari `_tataRekeningRepo` untuk mendeteksi apakah `ListPayment.Any()` atau status billing tertutup/lunas $\rightarrow$ set `hasPaidCashier = true`.
   - Membaca order laboratorium dari `_labOrderRepo` untuk mendeteksi apakah terdapat order dengan `CollectedDate != EmptyDate` atau status $\ge$ `SpecimenCollected` $\rightarrow$ set `hasCollectedSpecimen = true`.
   - Memanggil `labExternal.Cancel(...)` dengan melewatkan flag tersebut.
   - Menyimpan pembaruan status `LabExternalModel` ke database dalam `TransHelper.NewScope()`.
3. Unit test `LabExternalCancelHandlerTest.cs` di `Bilreg.Test/LabContext/LabExternalFeature/` memverifikasi:
   - Exception `EX-01` terjadi jika tagihan kasir telah lunas.
   - Exception `EX-02` terjadi jika spesimen order lab telah diambil.
   - Pembatalan berhasil mengubah status menjadi `Cancelled` dan mencatat alasan audit jika kedua kondisi bebas.

Notes:
- Menutup celah `GAP-005`, `EX-01`, dan `EX-02`.

---

## P4 - LabOrder Integration & Dual-Path Charging Refactor

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P4-S07

Title: LabOrder Integration Refactoring & Dual-Path Charging (Fix GAP-002)

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Memperbarui use case pembuatan order eksternal `LabOrderCreateExternalPatientCmd` agar mengonsumsi `RegExternalId` dan `ExternalMrNo` yang valid, serta merefaktorisasi `LabOrderChargeHandler` (`GAP-002`) agar memproses charging pasien eksternal langsung ke akun `TataRekening` tanpa mencari ke `_regRepo` (mencegah `KeyNotFoundException`).

Depends On: P3-S04

Repository: `b09-bilreg-api`

Completion Criteria:
1. Handler `LabOrderCreateExternalPatientHandler` disesuaikan untuk memastikan `snapshot.RegId` berisi `RegExternalId` yang sah dan `snapshot.PatientId` berisi `ExternalMrNo`.
2. Handler `LabOrderChargeHandler` dimodifikasi:
   - Jika `order.OrderSource == LabOrderSourceEnum.ExternalPatient`: tidak memanggil `LoadReg(order.Patient.RegId)` ke `_regRepo`. Sebaliknya, memverifikasi keberadaan registrasi eksternal via `_labExternalRepo` (atau billing account di `_tataRekeningRepo`), menetapkan tipe tarif default umum (`UMUM` / kelas rawat jalan), dan membebankan tindakan langsung ke `TataRekeningModel` pasien eksternal.
   - Jika `order.OrderSource != LabOrderSourceEnum.ExternalPatient`: alur eksisting via `_regRepo` tetap berjalan tanpa regresi.
3. Unit test `LabOrderChargeHandlerTest.cs` diperbarui/ditambahkan untuk memverifikasi eksekusi charge order eksternal berhasil sukses tanpa melempar `KeyNotFoundException`.
4. Unit test `LabOrderCreateExternalPatientHandlerTest.cs` memverifikasi snapshot pasien eksternal tersimpan dengan `RegExternalId` dan `ExternalMrNo`.

Notes:
- Menyelesaikan celah kritis `GAP-002` yang sebelumnya menyebabkan kegagalan sistem saat charging order pasien luar.

---

## P5 - Web API Endpoints & Contract Verification

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P5-S08

Title: Web API Controller LabExternalController & Endpoint Integration Tests

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

Objective:
Mengekspos seluruh fungsionalitas pendaftaran eksternal melalui controller REST HTTP ASP.NET Core `LabExternalController` dengan routing `api/LabContext/LabExternalFeature`, wrapper respon standar `JSendOk`, dan pengujian integrasi endpoint.

Depends On: P3-S04, P3-S05, P3-S06, P4-S07

Repository: `b09-bilreg-api`

Completion Criteria:
1. Controller `LabExternalController.cs` dibuat di `b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabExternalFeature/` dengan atribut `[Route("api/LabContext/LabExternalFeature")]`, `[ApiController]`, dan `[Authorize]`.
2. Endpoint diimplementasikan:
   - `POST /api/LabContext/LabExternalFeature` memicu `LabExternalCreateCmd` $\rightarrow$ mengembalikan `JSendOk(LabExternalCreateResponse)`.
   - `POST /api/LabContext/LabExternalFeature/cancel` memicu `LabExternalCancelCmd` $\rightarrow$ mengembalikan `JSendOk(LabExternalCancelResponse)`.
   - `GET /api/LabContext/LabExternalFeature/{regExternalId}` memicu `LabExternalGetQuery` $\rightarrow$ mengembalikan `JSendOk(LabExternalDetailResponse)`.
   - `GET /api/LabContext/LabExternalFeature/search` memicu `LabExternalListQuery` $\rightarrow$ mengembalikan `JSendOk(List<LabExternalListItemResponse>)`.
3. Seluruh controller unit/integration test di `Bilreg.Test/LabContext/LabExternalFeature/LabExternalControllerTest.cs` berhasil lulus memverifikasi response status code HTTP 200, binding request, dan penanganan HTTP 400 Bad Request saat terjadi exception bisnis.
4. Seluruh test suite `dotnet test` pada repositori `b09-bilreg-api` berhasil lulus tanpa error (zero regressions).

Notes:
- Mengikuti pola standar controller Bilreg yang konsisten (`MediatR` dispatcher, `JSendOk`).

---

# 6. Change Log

- 2026-10-14: Dokumen perencanaan awal (V1.0) dibuat oleh peran `ica-architect` khusus untuk repositori backend `b09-bilreg-api` (Opsi 1) dengan 5 fase dan 8 irisan implementasi (P1-S01 s/d P5-S08). Status persetujuan eksekusi: `APPROVED`.
