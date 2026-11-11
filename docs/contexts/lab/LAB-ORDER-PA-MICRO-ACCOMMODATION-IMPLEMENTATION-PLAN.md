---
Title: Implementation Plan Akomodasi Order Laboratorium Patologi Anatomi dan Mikrobiologi
Code: LAB-ORDER-PA-MICRO-ACCOMMODATION-IMPLEMENTATION-PLAN
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-04
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan transformasi sistem order laboratorium pada **`b09-bilreg-api`** dan **`c012_myhospital_web`** agar mampu mengakomodasi pemeriksaan Patologi Anatomi (PA) dan Mikrobiologi Klinik (Micro) berdampingan dengan Patologi Klinik (PK) eksisting.

Realisasi ini mencakup:
1. Pembentukan master klasifikasi kanonikal: `LaboratoryServiceType` (enum), master referensi `BILRG_LabDiscipline`, dan master referensi `BILRG_LabTestGroup`.
2. Perluasan enum `VacutainerTypeEnum` dengan `None = 0` untuk spesimen non-darah.
3. Mesin resolusi tes yang menyimpan snapshot klasifikasi ke baris order (`BILRG_LabOrderItem`).
4. Mesin *Transparent Auto-Split* pada use-case pembuatan order di backend (`1 Order = 1 ServiceType`).
5. Pembaruan formulir pemesanan terpadu tanpa tab di frontend HIS Web (`LabOrderCreateCanvas.vue`).
6. Perutean polimorfik form entri hasil (*Result Entry*) sesuai tipe layanan (PK tabular, PA naratif, Micro biakan/AST) serta penyajian View Result hierarkis 2-tingkat.
7. Menjaga batas integrasi middleware `a046_oware_ordermiddlewareapi` tetap 100% As-Is tanpa perubahan kode.

Planning Mode: **FEATURE-PLANNING**

Referenced artifacts:
- ARCHITECTURE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md)
- FEASIBILITY-ASSESSMENT: [LAB-ORDER-PA-MICRO-ACCOMMODATION-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-FEASIBILITY-ASSESSMENT.md)
- ISSUE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md)
- DOMAIN Bilreg: [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)

Architecture Applicability: **`ARCHITECTURE-REQUIRED`**

---

# 2. Planning Scope

Rencana implementasi didekomposisi ke dalam 5 phase dan 9 slice terisolasi:
1. **Phase 1: Database Migration & Domain Foundation (`b09-bilreg-api`)**: Pembuatan tabel master referensi baru (`BILRG_LabDiscipline`, `BILRG_LabTestGroup`), skrip alter tabel, perluasan enum `VacutainerTypeEnum`, enum `LaboratoryServiceType`, dan update invariant domain model.
2. **Phase 2: Master Data Management & Definition Resolution (`b09-bilreg-api`)**: DAL, repository, query/command handlers, dan controller endpoints untuk master referensi `LabDiscipline` dan `LabTestGroup`, pembaruan `LabTestDefinitionFeature`, serta pengayaan `LabTestResolutionService` dengan snapshot persistensi.
3. **Phase 3: Order Workflow & Auto-Split Engine (`b09-bilreg-api`)**: Persistensi `LabOrderModel` dan `LabOrderItemModel` di DAL/Repo, implementasi logika *Transparent Auto-Split* pada `LabOrderCreateFromEmrHandler` dan handler pasien luar, serta pengayaan query detail order.
4. **Phase 4: Frontend HIS Web Ordering & Master Lookup (`c012_myhospital_web`)**: Pembaruan Zod schema master tarif/order, composable pencarian tes, penyesuaian `LabOrderCreateCanvas.vue` (kalkulator tabung darah mengabaikan `vacutainerType === 0`).
5. **Phase 5: Frontend Result Management & View Grouping (`c012_myhospital_web`)**: Pembuatan komponen perute `LabResultEntryRouter.vue` (PK tabular, PA naratif, Micro biakan/AST) dan perenderan hierarki visual pada `LabResultView.vue` berbasis snapshot order item.

---

# 3. Dependencies

- **Eksternal**:
  - Repositori `a046_oware_ordermiddlewareapi` berada dalam status As-Is (Zero Changes).
  - Database SQL Server eksisting pada database `BilregDb`.
- **Antar-Slice (Dependency Graph)**:
  - `P1-S01`: None (Skrip DDL SQL)
  - `P1-S02`: `P1-S01` (Domain entities, enums, dan invariants)
  - `P2-S03`: `P1-S02` (Master DAL, Repo, UseCases LabDiscipline & LabTestGroup)
  - `P2-S04`: `P1-S02`, `P2-S03` (LabTestDefinition updates & LabTestResolutionService snapshot logic)
  - `P3-S05`: `P1-S02` (LabOrder & LabOrderItem DAL persistence & snapshot mappings)
  - `P3-S06`: `P2-S04`, `P3-S05` (Auto-Split Engine on LabOrderCreate handlers)
  - `P4-S07`: `P2-S04`, `P3-S06` (Frontend Master Lookup & Order Create Canvas)
  - `P5-S08`: `P3-S06` (Frontend Result Entry Form Polymorphic Routing)
  - `P5-S09`: `P3-S06`, `P5-S08` (Frontend View Result & Printout Hierarchical Grouping)

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| **P1 - Database Migration & Domain Foundation** | IMPLEMENTED | GO | 2/2 |
| **P2 - Master Data Management & Definition Resolution** | IMPLEMENTED | GO | 2/2 |
| **P3 - Order Workflow & Auto-Split Engine** | IMPLEMENTED | GO | 2/2 |
| **P4 - Frontend HIS Web Ordering & Master Lookup** | IMPLEMENTED | GO | 1/1 |
| **P5 - Frontend Result Management & View Grouping** | IMPLEMENTED | GO | 2/2 |

- **Plan Status**: `COMPLETED`
- **Execution Approval**: **`APPROVED`**

---

# 5. Phases

## P1 - Database Migration & Domain Foundation

Implementation Status: IMPLEMENTED  
Review Status: GO

---

### P1-S01

**Title**: DDL Skrip SQL & Database Migration untuk Klasifikasi Lab dan Snapshot Order

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Membuat berkas DDL SQL tabel baru pada `src/bilreg/Bilreg.SqlDb/LabContext/LabMasterFeature/`:
   - `BILRG_LabDiscipline.sql`: Kolom `DisciplineId VARCHAR(10) NOT NULL`, `DisciplineName VARCHAR(100) NOT NULL`, audit trail (`CrtUser`, `CrtDate`, `UpdUser`, `UpdDate`, `VodUser`, `VodDate`), Primary Key clustered `(DisciplineId)`, index `IX_BILRG_LabDiscipline_Name`.
   - `BILRG_LabTestGroup.sql`: Kolom `TestGroupId VARCHAR(10) NOT NULL`, `TestGroupName VARCHAR(100) NOT NULL`, audit trail, Primary Key clustered `(TestGroupId)`, index `IX_BILRG_LabTestGroup_Name`.
2. Membuat berkas DDL alter migration pada `src/bilreg/Bilreg.SqlDb/LabContext/LabTestDefinitionFeature/`:
   - `BILRG_LabTestDefinition_Classification_Alter.sql`: Menambahkan kolom `ServiceType INT NOT NULL DEFAULT(1)`, `DisciplineId VARCHAR(10) NOT NULL DEFAULT('')`, `TestGroupId VARCHAR(10) NOT NULL DEFAULT('')`, dan index `IX_BILRG_LabTestDefinition_ServiceDiscipline`.
3. Membuat berkas DDL alter migration pada `src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/`:
   - `BILRG_LabOrder_ServiceType_Alter.sql`: Menambahkan kolom `ServiceType INT NOT NULL DEFAULT(1)` dan index `IX_BILRG_LabOrder_ServiceType`.
   - `BILRG_LabOrderItem_ClassificationSnapshot_Alter.sql`: Menambahkan kolom `ServiceType INT NOT NULL DEFAULT(1)`, `DisciplineId VARCHAR(10) NOT NULL DEFAULT('')`, `DisciplineName VARCHAR(100) NOT NULL DEFAULT('')`, `TestGroupId VARCHAR(10) NOT NULL DEFAULT('')`, `TestGroupName VARCHAR(100) NOT NULL DEFAULT('')`, dan index `IX_BILRG_LabOrderItem_Grouping`.
4. Membuat berkas seed data `BILRG_LabDiscipline_Seed.sql` yang menyertakan data awal:
   - `DISC-HEM` (Hematologi), `DISC-KIM` (Kimia Klinik), `DISC-IMM` (Imunologi & Serologi), `DISC-URI` (Urinalisis & Feses), `DISC-MIK` (Mikrobiologi Klinik), `DISC-PAH` (Patologi Anatomi - Histopatologi), `DISC-PAS` (Patologi Anatomi - Sitologi).

**Depends On**: None

**Repository**: `b09-bilreg-api`

**Completion Criteria**:
- Seluruh berkas SQL DDL dan migration berada pada struktur folder yang tepat di `Bilreg.SqlDb`.
- Skrip sintaks SQL Server valid dan dapat dieksekusi secara idempotent (`IF NOT EXISTS`).

**Notes**:
- Pastikan constraint default diberikan pada setiap kolom baru untuk menjamin backward-compatibility terhadap data historis.

---

### P1-S02

**Title**: Definisi Enum, Entitas Domain, dan Invariants Klasifikasi Lab

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Menambahkan enum [`LaboratoryServiceType.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LaboratoryServiceType.cs) pada namespace `Bilreg.Domain.LabContext.LabOrderFeature`:
   - `ClinicalPathology = 1`, `AnatomicalPathology = 2`, `Microbiology = 3`.
2. Memperbarui [`VacutainerTypeEnum.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/VacutainerTypeEnum.cs):
   - Menambahkan nilai `None = 0` (Tanpa Tabung Vacutainer / Non-Blood Sample).
3. Membuat entitas master referensi baru pada `Bilreg.Domain.LabContext.LabMasterFeature`:
   - `LabDisciplineModel.cs`: Key interface `ILabDisciplineKey`, properti `DisciplineId`, `DisciplineName`, `AuditTrail`, factory method `Create`, `Default`, `Key`.
   - `LabTestGroupModel.cs`: Key interface `ILabTestGroupKey`, properti `TestGroupId`, `TestGroupName`, `AuditTrail`, factory method `Create`, `Default`, `Key`.
4. Memperbarui entitas [`LabTestDefinitionModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabTestDefinitionFeature/LabTestDefinitionModel.cs):
   - Menambahkan properti `ServiceType` (default `ClinicalPathology`), `DisciplineId` (string, default `""`), `TestGroupId` (string, default `""`).
   - Memperbarui constructor, factory methods (`CreateNew`, `ApplyUpdate`, `Load`, `Default`, `Key`), serta validasi invariant (memastikan `ServiceType` adalah enum yang valid).
5. Memperbarui entitas [`LabOrderModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs):
   - Menambahkan properti `ServiceType` (default `ClinicalPathology`).
   - Memperbarui constructor dan factory method `CreateInternal`, `CreateFromEmr`, `CreateExternal`, `Load`, `Default`, `Key`.
6. Memperbarui record [`LabOrderItemModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemModel.cs):
   - Menambahkan properti snapshot: `LaboratoryServiceType ServiceType`, `string DisciplineId = ""`, `string DisciplineName = ""`, `string TestGroupId = ""`, `string TestGroupName = ""`.
   - Memperbarui factory method `Create`.
7. Menambahkan unit tests pada `Bilreg.Test` untuk memverifikasi validitas invariant entitas yang diperbarui.

**Depends On**: `P1-S01`

**Repository**: `b09-bilreg-api`

**Completion Criteria**:
- Seluruh tipe enum dan model domain terdefinisi bersih di `Bilreg.Domain`.
- `dotnet build src/bilreg/Bilreg.Domain/Bilreg.Domain.csproj` berhasil tanpa error.
- Unit test domain di `Bilreg.Test` lulus 100%.

**Notes**:
- Jangan mengubah invariant komponen tes (`ValidateActiveInvariants`) kecuali untuk mengakomodasi enum baru.

---

## P2 - Master Data Management & Definition Resolution

Implementation Status: IMPLEMENTED  
Review Status: GO

---

### P2-S03

**Title**: Master Referensi `LabDiscipline` dan `LabTestGroup` (DAL, Repository, UseCases, API)

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Membuat interface & implementasi DAL / Repository di `Bilreg.Application` dan `Bilreg.Infrastructure`:
   - `ILabDisciplineDal.cs` & `ILabDisciplineRepo.cs`, implementasi Dapper `LabDisciplineDal.cs` & `LabDisciplineRepo.cs`.
   - `ILabTestGroupDal.cs` & `ILabTestGroupRepo.cs`, implementasi Dapper `LabTestGroupDal.cs` & `LabTestGroupRepo.cs`.
2. Membuat use case commands dan queries MediatR di `Bilreg.Application.LabContext.LabMasterFeature`:
   - `LabDisciplineListQuery` & Handler (mengembalikan daftar disiplin aktif).
   - `LabDisciplineSaveCmd` & Handler (insert/update disiplin).
   - `LabTestGroupListQuery` & Handler (mengembalikan daftar test group).
   - `LabTestGroupSaveCmd` & Handler (insert/update test group).
3. Mendaftarkan endpoint controller pada `Bilreg.Api`:
   - `GET api/LabContext/LabMasterFeature/disciplines`
   - `POST api/LabContext/LabMasterFeature/disciplines`
   - `GET api/LabContext/LabMasterFeature/test-groups`
   - `POST api/LabContext/LabMasterFeature/test-groups`
4. Menuliskan unit test handler pada `Bilreg.Test` untuk operasi query dan command master data.

**Depends On**: `P1-S02`

**Repository**: `b09-bilreg-api`

**Completion Criteria**:
- CRUD master referensi disiplin dan test group berfungsi penuh via Dapper SQL.
- Endpoint API mengembalikan respons JSON sesuai format standar `Nuna.Lib`.
- Unit test master use-case lulus 100%.

**Notes**:
- Master data ini bersifat referensial independen; query list harus ringan dan terindeks.

---

### P2-S04

**Title**: Pembaruan `LabTestDefinitionFeature` & Pengayaan Snapshot pada `LabTestResolutionService`

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Memperbarui DAL dan Repository `ILabTestDefinitionDal` / `LabTestDefinitionDal` serta `ILabTestDefinitionRepo` / `LabTestDefinitionRepo`:
   - Memetakan kolom baru `ServiceType`, `DisciplineId`, dan `TestGroupId` pada query `INSERT`, `UPDATE`, `SELECT`.
2. Memperbarui query/command DTOs dan handlers pada `Bilreg.Application.LabContext.LabTestDefinitionFeature`:
   - Menambahkan `ServiceType`, `DisciplineId`, `TestGroupId` pada `LabTestDefinitionCreateCmd`, `LabTestDefinitionUpdateCmd`, `LabTestDefinitionGetResponse`, dan `LabTestDefinitionListResponse`.
3. Memperbarui [`LabTestResolutionService.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs):
   - Menginjeksi `ILabDisciplineRepo` dan `ILabTestGroupRepo` (atau lookup batch) untuk mendapatkan nama deskriptif saat resolusi.
   - Mengatur `RequiredTubeCount = 0` apabila `definition.VacutainerType == VacutainerTypeEnum.None`.
   - Mengisi snapshot properties pada `LabOrderItemModel`: `ServiceType = definition.ServiceType`, `DisciplineId = definition.DisciplineId`, `DisciplineName = disciplineName`, `TestGroupId = definition.TestGroupId`, `TestGroupName = testGroupName`.
4. Memperbarui unit test [`LabTestResolutionServiceTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabTestResolutionServiceTest.cs) dan handler test terkait.

**Depends On**: `P1-S02`, `P2-S03`

**Repository**: `b09-bilreg-api`

**Completion Criteria**:
- `LabTestResolutionService` menghasilkan `ResolvedLabOrderLine` dengan atribut snapshot klasifikasi lengkap.
- Jika tes memiliki `VacutainerType == None`, `RequiredTubeCount` bernilai 0.
- Unit test resolusi tes lulus 100%.

**Notes**:
- Lakukan caching atau in-memory map sederhana pada resolution service bila data master referensi disiplin sedikit untuk menjaga performa resolusi order item.

---

## P3 - Order Workflow & Auto-Split Engine

Implementation Status: IMPLEMENTED  
Review Status: GO

---

### P3-S05

**Title**: Persistensi `LabOrderModel` dan `LabOrderItemModel` di DAL / Repo

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Memperbarui DAL dan DTO pemetaan pada `Bilreg.Infrastructure.LabContext.LabOrderFeature`:
   - `LabOrderDal.cs`: Menyimpan dan membaca `ServiceType` pada tabel `BILRG_LabOrder`.
   - `LabOrderItemDal.cs`: Menyimpan dan membaca `ServiceType`, `DisciplineId`, `DisciplineName`, `TestGroupId`, `TestGroupName` pada tabel `BILRG_LabOrderItem`.
2. Memperbarui mapping repositori [`LabOrderRepo.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabOrderRepo.cs):
   - Memastikan saat memuat `LabOrderModel` dari database, seluruh nilai enum `ServiceType` dan snapshot item dipetakan secara akurat ke domain aggregate.
3. Memperbarui `LabOrderGetQuery` dan record `LabOrderGetResponse`:
   - Menambahkan field `ServiceType` pada level order dan properti snapshot pada list `Items`.
4. Menambahkan atau memperbarui unit test DAL / Repo pada `Bilreg.Test`.

**Depends On**: `P1-S02`

**Repository**: `b09-bilreg-api`

**Completion Criteria**:
- Operasi `_labOrderRepo.SaveChanges` dan `_labOrderRepo.LoadEntity` berhasil menyimpan dan memuat order beserta snapshot item tanpa data loss.
- Kontrak `LabOrderGetResponse` menyertakan `ServiceType` dan snapshot klasifikasi item.
- Unit test persistence lulus 100%.

**Notes**:
- Query Dapper harus memetakan kolom baru secara eksplisit tanpa asumsi urutan kolom (*positional column*).

---

### P3-S06

**Title**: Mesin *Transparent Auto-Split* pada Handler Pembuatan Order Lab

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Memperbarui [`LabOrderCreateFromEmrHandler.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs):
   - Menerima kumpulan item campuran (PK, PA, Micro) dari `request.Items`.
   - Mengelompokkan `resolvedLines` berdasarkan `x.Item.ServiceType`.
   - Dalam scope atomik `using (var trans = TransHelper.NewScope())`:
     - Jika hanya 1 kelompok layanan: buat `LabOrderModel` tunggal dengan `EmrOrderId = request.EmrOrderId`, generate 1 `OrderNo`.
     - Jika terdapat >1 kelompok layanan (*mixed cart*): iterasi setiap kelompok layanan, generate `EmrOrderId` spesifik (`$"{request.EmrOrderId}-PK"`, `$"{request.EmrOrderId}-PA"`, `$"{request.EmrOrderId}-MIC"`), generate `OrderNo` per order via `_sequencer`, buat `LabOrderModel` dengan `ServiceType` yang sesuai, dan simpan via `_labOrderRepo.SaveChanges(order)`.
     - `ClinicalNote` dari request disalin utuh ke seluruh sub-order yang dihasilkan.
     - Selesaikan transaksi atomik (`trans.Complete()`).
   - Memperbarui respons `LabOrderCreateFromEmrResponse` untuk mengembalikan ID/No order primer dan daftar sub-order yang terbentuk (`CreatedOrders`).
2. Menerapkan logika pemecahan yang selaras pada `LabOrderCreateExternalPatientCmd` & Handler.
3. Memperbarui pengecekan idempotensi untuk menangani order dengan sufiks.
4. Menuliskan unit test komprehensif pada `Bilreg.Test` yang menguji:
   - Pembuatan order tunggal PK standar (backward-compatible).
   - Pembuatan order campuran (PK + PA + Micro) yang menghasilkan 3 sub-order terpisah secara transparan dalam 1 transaksi.
   - Kegagalan rollback transaksi atomik jika salah satu sub-order memicu exception.

**Depends On**: `P2-S04`, `P3-S05`

**Repository**: `b09-bilreg-api`

**Completion Criteria**:
- Keranjang pemesanan dengan item campuran otomatis terpecah menjadi sub-order mandiri per `ServiceType`.
- Masing-masing sub-order memiliki nomor transaksi `OrderNo` dan `ServiceType` yang valid.
- Transaksi atomik terverifikasi via unit test.

**Notes**:
- Pastikan deteksi duplikasi 24 jam (`EX-05`) berjalan akurat terhadap masing-masing item sebelum pemecahan dilakukan.

---

## P4 - Frontend HIS Web Ordering & Master Lookup

Implementation Status: IMPLEMENTED  
Review Status: GO

---

### P4-S07

**Title**: Sinkronisasi Schema Master Tarif & Adaptasi Form Order Canvas

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Memperbarui Zod schema di `c012_myhospital_web`:
   - [`useLabMasterLookup.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/composables/useLabMasterLookup.ts): memperluas `labMasterTarifItemSchema` dengan `serviceType: z.number().optional().default(1)`, `disciplineId: z.string().optional().default('')`, `disciplineName: z.string().optional().default('')`, `testGroupId: z.string().optional().default('')`, `testGroupName: z.string().optional().default('')`.
   - [`lab-order.schema.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/types/lab-order.schema.ts): memperbarui `labOrderDetailSchema`, `labOrderItemDetailSchema`, dan `labOrderCreateFromEmrResponseSchema` untuk menerima `serviceType` dan daftar created orders.
2. Memperbarui helper pemformat tabung [`lab-formatters.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/lib/lab-formatters.ts):
   - Memastikan `vacutainerType === 0` (atau `None`) mengembalikan `null` atau representasi non-tabung darah sehingga diabaikan oleh kalkulator total tabung.
3. Memperbarui komponen [`LabOrderCreateCanvas.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue):
   - Mempertahankan kotak pencarian tes tunggal terpadu tanpa tab.
   - Item tes non-darah (PA/Micro/Urin/Feses) menampilkan indikator `SpecimenType` tanpa memicu penambahan badge tabung vacutainer darah.
   - Pemanfaatan textarea `clinicalNote` eksisting untuk instruksi medis umum atau riwayat klinis.
   - Mengonsumsi respons order creation dan menampilkan notifikasi sukses yang informatif bila order dipecah menjadi beberapa nomor order.
4. Menjalankan verifikasi frontend:
   - `npx prettier --write <modified-files>`
   - `cd c012_myhospital_web && pnpm tc:app`
   - `cd c012_myhospital_web && pnpm lint`

**Depends On**: `P2-S04`, `P3-S06`

**Repository**: `c012_myhospital_web`

**Completion Criteria**:
- Pengguna dapat mencari dan memilih tes PK, PA, dan Micro secara simultan dalam 1 canvas pemesanan.
- Indikator kebutuhan tabung darah pada footer canvas hanya menghitung tes bertabung vacutainer (`vacutainerType > 0`).
- Type check `pnpm tc:app` dan linter `pnpm lint` lulus tanpa error.

**Notes**:
- Patuhi protokol Frontend: kerjakan строго di direktori `c012_myhospital_web` dan jangan jalankan full regression test.

---

## P5 - Frontend Result Management & View Grouping

Implementation Status: IMPLEMENTED  
Review Status: GO

---

### P5-S08

**Title**: Perutean Polimorfik Form Entri Hasil Laboratorium (`LabResultEntryRouter`)

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Menambahkan enum `LaboratoryServiceType` pada `c012_myhospital_web/src/modules/Laboratory/types/lab-enums.ts`.
2. Merefaktor [`LabResultEntryForm.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/result/LabResultEntryForm.vue) menjadi komponen perute dinamis atau membuat `LabResultEntryRouter.vue`:
   - Memeriksa nilai `order.serviceType`.
   - `ServiceType = 1` (PK): Me-render formulir tabular analit eksisting (`LabResultEntryPkTabular.vue` / form row grid).
   - `ServiceType = 2` (PA): Me-render formulir naratif PA (`LabResultEntryPaForm.vue`: input narasi makroskopis, mikroskopis, kesimpulan/diagnosa patologi, dan lokasi jaringan sediaan).
   - `ServiceType = 3` (Micro): Me-render formulir biakan/AST (`LabResultEntryMicroForm.vue`: evaluasi sediaan langsung, jumlah koloni, status pertumbuhan, organisme terisolasi, dan matriks kepekaan antibiotik S/I/R).
3. Memperbarui schema submit hasil pada [`lab-result.schema.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/types/lab-result.schema.ts) agar mampu memetakan payload naratif PA dan matriks mikro ke entitas hasil backend.
4. Menjalankan verifikasi frontend:
   - `npx prettier --write <modified-files>`
   - `cd c012_myhospital_web && pnpm tc:app`
   - `cd c012_myhospital_web && pnpm lint`

**Depends On**: `P3-S06`

**Repository**: `c012_myhospital_web`

**Completion Criteria**:
- Saat membuka order berkategori PA, form menyajikan editor naratif makro/mikro/diagnosa PA.
- Saat membuka order berkategori Micro, form menyajikan editor biakan kuman dan tabel antibiogram.
- Saat membuka order berkategori PK, form tabular analit eksisting berfungsi tanpa regresi.
- `pnpm tc:app` dan `pnpm lint` lulus tanpa error.

**Notes**:
- Komponen PA naratif dapat memanfaatkan properti `NarrativeValue` pada model hasil eksisting.

---

### P5-S09

**Title**: Pengelompokan Hierarki 2-Tingkat pada View Result & Printout Hasil Lab

**Implementation Status**: IMPLEMENTED  
**Review Status**: GO

**Objective**:
1. Memperbarui komponen tampilan hasil pemeriksaan [`LabResultView.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/result/LabResultComponentRow.vue) (atau komponen presenter hasil terkait):
   - Membaca snapshot `disciplineName` dan `testGroupName` dari baris item order.
   - Mengelompokkan item pemeriksaan secara hierarkis:
     - **Level 1**: Header Disiplin (misal: "HEMATOLOGI", "KIMIA KLINIK", "PATOLOGI ANATOMI").
     - **Level 2**: Sub-Header Test Group (misal: "CBC / Darah Lengkap", "Profil Lipid", "Histopatologi Jaringan").
     - **Level 3**: Komponen analit / baris hasil.
   - Jika `disciplineName` atau `testGroupName` kosong, masukkan ke grup *"Pemeriksaan Lain-Lain"*.
2. Memperbarui template cetak dokumen hasil laboratorium (*Printout Layout*) agar menyajikan hierarki pengelompokan yang identik.
3. Menjalankan verifikasi frontend:
   - `npx prettier --write <modified-files>`
   - `cd c012_myhospital_web && pnpm tc:app`
   - `cd c012_myhospital_web && pnpm lint`

**Depends On**: `P3-S06`, `P5-S08`

**Repository**: `c012_myhospital_web`

**Completion Criteria**:
- Halaman View Result menampilkan hasil tes terkelompok rapi berdasarkan Disiplin dan Grup Tes tanpa memicu query runtime join ke master.
- Template cetak dokumen hasil lab memisahkan seksi pemeriksaan secara profesional dan rapi.
- `pnpm tc:app` dan `pnpm lint` lulus tanpa error.

**Notes**:
- Pastikan perenderan tetap stabil meskipun order historis lama memiliki `disciplineName` kosong (graceful fallback).

---

# 6. Change Log

- **2026-11-04**: Versi 1.0 diterbitkan dengan persetujuan `Execution Approval: APPROVED`. Mengakomodasi klasifikasi kanonikal (`LaboratoryServiceType`, `Discipline`, `TestGroup`), penanganan sampel non-darah (`VacutainerTypeEnum.None`), *Transparent Auto-Split* order campuran, perutean form entri hasil polimorfik, serta hierarki visual view result 2-tingkat.
