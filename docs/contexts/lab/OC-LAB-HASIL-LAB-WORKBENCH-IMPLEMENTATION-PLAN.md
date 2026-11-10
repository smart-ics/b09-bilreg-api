---
Title: Hasil Pemeriksaan Laboratorium Terverifikasi UI & BFF Implementation Plan
Code: OC-LAB-HASIL-LAB
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-10
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan realisasi teknis antarmuka pengguna konsol meja kerja hasil laboratorium (*Laboratory Results & Expertise Management Workspace*) dan penyelarasan Backend-For-Frontend (BFF) pada repositori backend `b09-bilreg-api` dan frontend `c012_myhospital_web`, sesuai dengan spesifikasi target arsitektur yang telah disahkan pada **[OC-LAB-HASIL-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ARCHITECTURE.md)** (v1.2).

Tujuan operasional implementasi:
1. **Core Backend BILREG API (`b09-bilreg-api`)**:
   - Penegakan invarian alasan amandemen resmi minimal 10 karakter (**EX-03**) pada `LabResultDocumentModel.AmendVerifiedToNewVersion`.
   - Penyediaan generator nomor register fisik Patologi Anatomi adaptif menggunakan `ISequencer` (`H.{YY}.{NoUrut:D4}` untuk `DISC-PAH` vs `S.{YY}.{NoUrut:D4}` untuk `DISC-PAS`).
   - Perluasan kontrak DTO baca `LabResultView` pada `LabResultGetQuery` untuk memetakan `ExpertiseNote`, `CriticalNote`, `AmendmentReason`, `PreviousVersionId`, `AmendedDate`, dan `AmendedUserId`.
   - Perluasan DTO `LabResultVerificationWorklistView` dan kueri DAL Dapper `LabResultVerificationWorklistDal` dengan field `IsCito`, `Gender`, `AgeAtOrder`, `LayananPerujukName`, `DokterPerujukName`, `TestNames`, `HasCriticalValue`, `VersionNo`, `ResultSource`, dengan pengurutan prioritas CITO di baris teratas (`ORDER BY o.Urgency DESC, d.RecordedDate DESC`).
2. **Frontend LWF Web (`c012_myhospital_web`)**:
   - Penyelarasan skema Zod typesafe pada `lab-result.schema.ts` dan penambahan `labOrderReleaseResponseSchema` pada `lab-order.schema.ts`.
   - Pembaruan mutasi `useReleaseOrderMutation` pada `LabOrderService.ts` untuk mengonsumsi respon objek terstruktur `LabOrderReleaseResponse` (mengeliminasi crash validasi runtime).
   - Aktivasi navigasi persona terfokus: re-aktivasi rute tab `hasil_lab` pada `configs/tabs.ts` dan refaktor `views/HasilLab.vue` menjadi *route wrapper* terpadu yang memanggil `LabWorkflowView.vue` dengan initial query `activeQueue = 'verification'`.
   - Transformasi komponen entri PK `LabResultEntryPkTabular.vue` menjadi tabel grid spreadsheet berkecepatan tinggi dengan navigasi keyboard fluid (`Enter`/`Tab`), auto-flagging instan di sisi client, shortcut global `Ctrl+S`, penanda soft-warning nilai kritis, dan penegakan *inline mandatory guard* pada `CriticalNote` sebelum simpan.
   - Penyesuaian formulir Patologi Anatomi `LabResultEntryPaForm.vue`: header nomor register fisik PA, adaptif menyajikan Makroskopis untuk Histologi vs Adekuasi Sediaan + Rekomendasi untuk Sitologi, serta chip template diagnosis PA.
   - Pengayaan kartu antrean verifikasi Sp.PK `LabVerificationWorklistRow.vue` dengan badge CITO mencolok, banner peringatan nilai kritis, dan badge LIS (`[🤖 LIS Bridge • Tervalidasi Otomatis]`).
   - Transformasi stasiun telaah Sp.PK `LabVerificationPanel.vue` dengan ringkasan temuan kritis di baris atas, editor catatan ekspertise dengan chip template cepat, dan tombol verifikasi 1-klik langsung (*Direct 1-Click Verification*).
   - Penyederhanaan alur rilis pada `LabReleaseQueuePanel.vue` dengan tombol single-click *"🖨️ Cetak & Rilis Hasil"*, penghapusan komponen approval kasir usang (`LabFinancialClearancePanel.vue`), dan penanganan status `CLEAR` vs `BLOCKED` yang ramah.
   - Pembangunan dialog modal pratinjau lembar hasil PDF resmi `LabResultPdfPreviewDialog.vue` dan dialog amandemen terproteksi `LabAmendResultDialog.vue` (validasi min 10 karakter alasan dan otorisasi Sp.PK / Supervisor).

Planning Mode: **FEATURE-PLANNING**

Referenced artifacts:
- FEATURE: [OC-LAB-HASIL-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEATURE.md)
- ARCHITECTURE: [OC-LAB-HASIL-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ARCHITECTURE.md) (v1.2)
- FEASIBILITY-ASSESSMENT: [OC-LAB-HASIL-LAB-BFF-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB-BFF-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- UI SCREEN INVENTORY: [OC-LAB-HASIL-LAB-SCREEN-INVENTORY.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB-SCREEN-INVENTORY.md)
- INTERACTIVE MOCKUP: [mockup-lab-hasil.html](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/docs/modules/laboratory/mockup-lab-hasil.html)

Architecture Applicability: **`ARCHITECTURE-REQUIRED`**

---

# 2. Planning Scope

Target repositories:
1. `b09-bilreg-api`: Repositori Core Backend API BILREG (C# / .NET 8 / MediatR / Dapper / SQL Server).
2. `c012_myhospital_web`: Repositori Frontend SIMRS MyHospital Web (Vue 3 / TypeScript / Tailwind CSS v4 / TanStack Query / Zod).

### In Scope:
- **Backend `b09-bilreg-api`**:
  1. Validasi guard `reason.Trim().Length >= 10` pada domain aggregate `LabResultDocumentModel.AmendVerifiedToNewVersion`.
  2. Implementasi helper sequencer nomor register PA `LabPaRegisterSequencer` berbasis `ISequencer`.
  3. Pemetaan field klinis dan riwayat amandemen pada Read DTO `LabResultView` & handler `LabResultGetHandler`.
  4. Pengayaan metadata dan pengurutan prioritas CITO pada DAL `LabResultVerificationWorklistDal` & DTO `LabResultVerificationWorklistView`.
  5. Pengujian unit terarah pada `Bilreg.Test`.
- **Frontend `c012_myhospital_web`**:
  1. Sinkronisasi skema Zod `labResultDetailSchema`, `labResultRecordCmdSchema`, `labResultVerifyCmdSchema`, `labResultVerificationWorklistItemSchema`, dan `labOrderReleaseResponseSchema`.
  2. Pemutakhiran service query & mutation pada `LabOrderService.ts` dan `LabResultService.ts`.
  3. Re-aktivasi konfigurasi tab `hasil_lab` dan implementasi route wrapper `views/HasilLab.vue`.
  4. Grid spreadsheet entri PK `LabResultEntryPkTabular.vue` dengan keyboard fluid navigation, auto-flagging instan, shortcut `Ctrl+S`, dan soft-warning inline critical note guard.
  5. Formulir adaptif PA `LabResultEntryPaForm.vue` (Histopatologi Makroskopis vs Sitopatologi Adekuasi + Rekomendasi).
  6. Kartu antrean verifikasi Sp.PK `LabVerificationWorklistRow.vue`.
  7. Stasiun telaah & verifikasi 1-klik `LabVerificationPanel.vue`.
  8. Panel rilis single-click `LabReleaseQueuePanel.vue` (pembersihan UI approval kasir usang).
  9. Dialog modal pratinjau PDF `LabResultPdfPreviewDialog.vue`.
  10. Dialog amandemen terproteksi `LabAmendResultDialog.vue`.
  11. Verifikasi komprehensif frontend: `pnpm tc:app`, targeted specs, dan `pnpm lint`.

### Out of Scope:
- Komunikasi serial instrumen analyzer tingkat rendah (driver ASTM/HL7 ditangani LIS pihak ketiga).
- Transaksi pembayaran loket kasir finansial (ranah modul Kasir / `TRK-KASIR`).
- Lembar pengisian diagnosis DPJP pada rekam medis (ranah `EMRContext`).

---

# 3. Dependencies

- **Prerequisite Outcomes**:
  - `OC-LAB-ORDER-LAB`: Order pemeriksaan laboratorium dan perancah komponen uji (`LabOrderModel`).
  - `OC-LAB-SAMPLE-COLLECTION`: Pengambilan spesimen pasien (`SampleCollection`).
- **Target Repository Prerequisites**:
  - `b09-bilreg-api`: Kolom `ExpertiseNote` dan `CriticalNote` pada tabel `BILRG_LabResultDocument` telah tersedia dari implementasi backend inti.
- **Slice Dependencies**:
  - `Depends On` mendeklarasikan prasyarat implementasi antar-irisan (*prerequisites*).
  - Ketergantungan dinyatakan terpenuhi saat irisan rujukan berstatus `IMPLEMENTED`.
  - Irisan independen dirancang untuk dapat dieksekusi secara paralel.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Backend DTO, Worklist & Invariant Alignment (`b09-bilreg-api`) | IMPLEMENTED | GO | 4/4 |
| P2 - Frontend Types, Schemas & Query Services (`c012_myhospital_web`) | IMPLEMENTED | GO | 2/2 |
| P3 - Frontend Workstation Routing & Data Entry Forms (`c012_myhospital_web`) | IMPLEMENTED | GO | 3/3 |
| P4 - Sp.PK Verification Station & Worklist UI (`c012_myhospital_web`) | IMPLEMENTED | GO | 2/2 |
| P5 - Release Gating, PDF Preview & Strict Amendment Dialogs (`c012_myhospital_web`) | IMPLEMENTED | GO | 3/3 |
| P6 - Multi-Repo End-to-End Verification & Acceptance Gate (`c012_myhospital_web`) | IMPLEMENTED | GO | 1/1 |

**Plan-Level Status**: `COMPLETED`  
**Execution Approval**: `APPROVED` (Released for execution)

---

# 5. Phases

## P1 - Backend DTO, Worklist & Invariant Alignment (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01

Title: Domain Model Amendment Invariant & PA Register Sequencer  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menegakkan invarian domain bahwa alasan amandemen resmi wajib minimal 10 karakter (**EX-03**) pada `LabResultDocumentModel.AmendVerifiedToNewVersion`, serta menyediakan helper generator nomor register fisik PA `LabPaRegisterSequencer` menggunakan `ISequencer` (`H.{YY}.{NoUrut:D4}` untuk `DISC-PAH` vs `S.{YY}.{NoUrut:D4}` untuk `DISC-PAS`).  
Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabResultDocumentModel.AmendVerifiedToNewVersion` melempar `ArgumentException` jika panjang `reason.Trim()` kurang dari 10 karakter.
- Helper `LabPaRegisterSequencer` membangkitkan nomor urut berformat standar sesuai disiplin (`H.YY.0001` atau `S.YY.0001`).  
Notes: Realisasi GAP-BFF-10, GAP-BFF-12, dan TD-08, TD-09.  
Target Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultDocumentModel.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/LabPaRegisterSequencer.cs`
Implementation Notes:
- Menegakkan invarian domain EX-03 pada `LabResultDocumentModel.AmendVerifiedToNewVersion` dengan memvalidasi `string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10` dan melempar `ArgumentException("Alasan amandemen wajib diisi minimal 10 karakter.", nameof(reason))`.
- Mengimplementasikan `ILabPaRegisterSequencer` dan helper class `LabPaRegisterSequencer` di `Bilreg.Application.LabContext.LabResultFeature` yang memanfaatkan `ISequencer` untuk membangkitkan nomor urut register PA dengan format `H.{YY}.{NoUrut:D4}` untuk disiplin Histopatologi (`PAH`/`HISTO`/`H`) dan `S.{YY}.{NoUrut:D4}` untuk Sitopatologi (`PAS`/`SITO`/`CYTO`/`S`).
- Menambahkan unit test komprehensif pada `LabResultDocumentModelTest.cs` (25 tests passed) dan `LabPaRegisterSequencerTest.cs` (18 tests passed).
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultDocumentModel.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/LabPaRegisterSequencer.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultDocumentModelTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabPaRegisterSequencerTest.cs`

---

### P1-S02

Title: Read DTO & Query Handler Alignment for Full Clinical Notes & Audit Trace  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperluas record DTO `LabResultView` pada `LabResultGetQuery.cs` dengan field `ExpertiseNote`, `CriticalNote`, `AmendmentReason`, `PreviousVersionId`, `AmendedDate`, dan `AmendedUserId`, serta memperbarui `LabResultGetHandler.cs` untuk memetakan field-field tersebut dari model domain dokumen hasil aktif.  
Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Record `LabResultView` memiliki ke-6 field baru dengan nilai default yang aman.
- `LabResultGetHandler` memetakan properti dari `LabResultDocumentModel` secara lengkap tanpa data yang hilang.  
Notes: Realisasi GAP-BFF-01 dan TD-10. Menghilangkan keterbatasan ketiadaan field klinis pada respons baca.  
Target Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultGetQuery.cs`
Implementation Notes:
- Record DTO `LabResultView` diperluas dengan 6 field baru: `ExpertiseNote` (string = ""), `CriticalNote` (string = ""), `AmendmentReason` (string = ""), `PreviousVersionId` (string = ""), `AmendedDate` (DateTime? = null), dan `AmendedUserId` (string = "").
- `LabResultGetHandler` memetakan field-field tersebut secara komprehensif dari `LabResultDocumentModel` aktif (`ExpertiseNote`, `CriticalNote`, `AmendmentReason`, `PreviousVersionId`, `AmendedDate` dengan konversi sentinel/default ke null, dan `AmendedUserId`), serta pada fallback DTO dokumen kosong (`empty`).
- Verifikasi build `Bilreg.Application` dan eksekusi tes `dotnet test --filter "FullyQualifiedName~LabResultGet"` serta `FullyQualifiedName~LabResultControllerTest` lulus 100% tanpa regresi.
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultGetQuery.cs`

---

### P1-S03

Title: Sp.PK Verification Worklist DTO & DAL Query Enrichment with CITO Sorting  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperluas record DTO `LabResultVerificationWorklistView` dan kueri SQL DAL Dapper pada `LabResultVerificationWorklistDal.cs` dengan menambahkan field `IsCito`, `Gender`, `AgeAtOrder`, `LayananPerujukName`, `DokterPerujukName`, `TestNames`, `HasCriticalValue`, `VersionNo`, `ResultSource`, serta menerapkan pengurutan prioritas CITO di baris teratas (`ORDER BY o.Urgency DESC, d.RecordedDate DESC`).  
Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `ILabResultVerificationWorklistDal.cs` dan `LabResultVerificationWorklistDal.cs` mengembalikan seluruh metadata antrean Sp.PK yang dibutuhkan UI tanpa N+1 query.
- Kueri SQL mengevaluasi penanda analit kritis (`HasCriticalValue`) dan mengurutkan kasus CITO prioritas teratas.  
Notes: Realisasi GAP-BFF-02, OQ-BFF-02, dan TD-10.  
Target Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/ILabResultVerificationWorklistDal.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultVerificationWorklistDal.cs`
Implementation Notes:
- Record DTO `LabResultVerificationWorklistView` diperluas dengan 9 field baru: `IsCito` (bool), `Gender` (string), `AgeAtOrder` (int), `LayananPerujukName` (string), `DokterPerujukName` (string), `TestNames` (string), `HasCriticalValue` (bool), `VersionNo` (int), dan `ResultSource` (int) dengan nilai default yang aman.
- Query SQL Dapper pada `LabResultVerificationWorklistDal` diperkaya untuk mengevaluasi `IsCito` (`o.Urgency = 1`), nama-nama tes teragregasi via `FOR XML PATH`, nilai kritis `HasCriticalValue` via subquery `EXISTS` terhadap `BILRG_LabResultItem` dengan `FlagStatus = 4` (`LabResultFlagEnum.Critical`), serta pengurutan prioritas CITO teratas (`ORDER BY o.Urgency DESC, d.RecordedDate DESC`).
- Verifikasi build `Bilreg.Application`, `Bilreg.Infrastructure`, dan eksekusi tes `dotnet test --filter "FullyQualifiedName~LabResult"` berhasil 100% (88 passed, 0 failed).
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/ILabResultVerificationWorklistDal.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultVerificationWorklistDal.cs`

---

### P1-S04

Title: Backend Unit Testing Verification for LabResult Invariants & Worklist DAL  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menyediakan pengujian unit terarah pada `Bilreg.Test` untuk memverifikasi penolakan amandemen jika alasan < 10 karakter, pemetaan DTO `LabResultView`, format nomor register PA, dan proyeksi worklist verifikasi Sp.PK.  
Depends On: P1-S01, P1-S02, P1-S03  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Eksekusi `dotnet test --filter "LabResult"` lulus 100% tanpa regresi.  
Notes: Memenuhi protokol targeted verification pada backend.  
Target Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultDocumentModelTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultGetHandlerTest.cs`
Implementation Notes:
- Memperbarui `LabResultAmendHandlerTest.cs` agar payload `LabResultAmendCmd` mengirim alasan amandemen valid (>= 10 karakter) serta menambahkan pengetesan validasi alasan pendek (< 10 karakter) dan guard null/empty.
- Menambahkan boundary test pada `LabResultDocumentModelTest.cs` untuk memastikan amandemen dengan alasan tepat 10 karakter atau lebih (`Length >= 10`) diterima dengan benar.
- Membuat suite pengujian unit baru `LabResultGetHandlerTest.cs` untuk memverifikasi pemetaan penuh DTO `LabResultView` termasuk `ExpertiseNote`, `CriticalNote`, `AmendmentReason`, `PreviousVersionId`, `AmendedDate`, `AmendedUserId`, penanganan tanggal sentinel, fallback dokumen kosong, serta pengecekan guard.
- Membuat suite pengujian unit baru `LabResultVerificationWorklistHandlerTest.cs` untuk memverifikasi pemetaan proyeksi antrean verifikasi Sp.PK (`IsCito`, `Gender`, `AgeAtOrder`, `LayananPerujukName`, `DokterPerujukName`, `TestNames`, `HasCriticalValue`, `VersionNo`, `ResultSource`) dan nilai default DTO.
- Menjalankan pengetesan komprehensif `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test --filter "FullyQualifiedName~LabResult|FullyQualifiedName~LabPaRegister"` dan verifikasi post-edit `dotnet test b09-bilreg-api/src/bilreg/Bilreg.Test --filter "FullyQualifiedName~LabResult"` dengan hasil 100% lulus (125 passed, 0 failed).
Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultAmendHandlerTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultDocumentModelTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultGetHandlerTest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultVerificationWorklistHandlerTest.cs`

---

## P2 - Frontend Types, Schemas & Query Services (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S05

Title: Zod Schemas & TypeScript Types Synchronization  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menyinkronkan seluruh skema validasi Zod pada `lab-result.schema.ts` dan `lab-order.schema.ts` untuk mencocokkan kontrak backend yang telah diperluas: `labResultDetailSchema` (+ field catatan dan jejak amandemen), `labResultRecordCmdSchema` (+ `criticalNote`), `labResultVerifyCmdSchema` (+ `expertiseNote`), `labResultVerificationWorklistItemSchema` (+ metadata antrean Sp.PK lengkap), serta penambahan `labOrderReleaseResponseSchema`.  
Depends On: P1-S02, P1-S03  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Skema Zod `labResultDetailSchema` memvalidasi seluruh properti pembacaan detail hasil.
- Skema `labOrderReleaseResponseSchema` terdefinisi: `z.object({ released: z.boolean(), billingStatus: z.string(), message: z.string() })`.
- Eksekusi `pnpm tc:app` mengenali tipe baru tanpa error.  
Notes: Realisasi GAP-BFF-01, GAP-BFF-02, GAP-BFF-03, GAP-BFF-04, GAP-BFF-05.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/types/lab-result.schema.ts`
- `c012_myhospital_web/src/modules/Laboratory/types/lab-order.schema.ts`
Implementation Notes:
- Di `src/modules/Laboratory/types/lab-result.schema.ts`:
  - Memperluas `labResultDetailSchema` dengan field `expertiseNote: z.string().optional().default('')`, `criticalNote: z.string().optional().default('')`, `amendmentReason: z.string().optional().default('')`, `previousVersionId: z.string().optional().default('')`, `amendedDate: z.string().nullable().optional()`, dan `amendedUserId: z.string().optional().default('')`.
  - Memperbarui `labResultRecordCmdSchema` untuk menyertakan `criticalNote: z.string().optional().default('')`.
  - Memperbarui `labResultVerifyCmdSchema` untuk menyertakan `expertiseNote: z.string().optional().default('')` dan mengekspor `LabResultVerifyCmd` sebagai `z.input<typeof labResultVerifyCmdSchema>` agar mendukung payload verifikasi dengan input opsional catatan ekspertise secara typesafe.
  - Memperluas `labResultVerificationWorklistItemSchema` dengan 9 field metadata antrean Sp.PK: `isCito: z.boolean().optional().default(false)`, `gender: z.string().optional().default('')`, `ageAtOrder: z.number().optional().default(0)`, `layananPerujukName: z.string().optional().default('')`, `dokterPerujukName: z.string().optional().default('')`, `testNames: z.string().optional().default('')`, `hasCriticalValue: z.boolean().optional().default(false)`, `versionNo: z.number().optional().default(1)`, dan `resultSource: z.number().optional().default(0)`.
  - Memperbarui mapper form entri (`mapLabResultEntryFormToRecordCmd`, `mapLabResultPaFormToRecordCmd`, `mapLabResultMicroFormToRecordCmd`) untuk menyertakan default `criticalNote: ''`.
- Di `src/modules/Laboratory/types/lab-order.schema.ts`:
  - Menambahkan skema Zod `labOrderReleaseResponseSchema = z.object({ released: z.boolean(), billingStatus: z.string(), message: z.string() })` dan ekspor tipe TypeScript `LabOrderReleaseResponse`.
- Menambahkan suite pengujian unit terarah baru pada `src/modules/Laboratory/__tests__/lab-result.schema.spec.ts` (10 tes lulus 100%).
- Verifikasi post-edit: Prettier terformat rapi, `pnpm tc:app` lulus dengan 0 type errors, targeted unit test `lab-result.schema.spec.ts` (10/10) dan `lab-order.schema.spec.ts` (36/36) lulus, serta oxlint dan eslint bersih (0 error, 0 warning) pada seluruh berkas yang dimodifikasi.
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/types/lab-result.schema.ts`
- `c012_myhospital_web/src/modules/Laboratory/types/lab-order.schema.ts`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/lab-result.schema.spec.ts`

---

### P2-S06

Title: Query & Mutation Services Alignment for Release Response & Clinical Commands  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui hook mutasi `useReleaseOrderMutation` pada `LabOrderService.ts` untuk memvalidasi respons menggunakan `labOrderReleaseResponseSchema` (menggantikan `labOrderDoneSchema`), serta memastikan `LabResultService.ts` meneruskan payload `criticalNote` dan `expertiseNote` secara typesafe.  
Depends On: P2-S05  
Repository: `c012_myhospital_web`  
Completion Criteria:
- `useReleaseOrderMutation` mengembalikan objek terstruktur berstatus `released`, `billingStatus`, dan `message`.
- Crash validasi Zod runtime pada saat rilis order berhasil dieliminasi 100%.  
Notes: Realisasi GAP-BFF-03, OQ-BFF-03, dan TD-07.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/queries/LabOrderService.ts`
- `c012_myhospital_web/src/modules/Laboratory/queries/LabResultService.ts`
Implementation Notes:
- Di `LabOrderService.ts`:
  - Memperbarui mutasi `useReleaseOrderMutation` untuk memvalidasi respons payload dengan `labOrderReleaseResponseSchema` (menggantikan `labOrderDoneSchema`), mengeliminasi runtime validation crash saat rilis order.
  - Menetapkan return type eksplisit `Promise<LabOrderReleaseResponse>` (`{ released: boolean, billingStatus: string, message: string }`) dan mengekspor tipe `LabOrderReleaseResponse`.
  - Membersihkan casting `as any` pada `useCollectionPreparation` menjadi `as Record<string, unknown>`.
- Di `LabResultService.ts`:
  - Memverifikasi `useRecordResultMutation` meneruskan payload `criticalNote` melalui `labResultRecordCmdSchema` ke endpoint API `POST LabContext/LabResultFeature/record`.
  - Memverifikasi `useVerifyResultMutation` meneruskan payload `expertiseNote` melalui `labResultVerifyCmdSchema` ke endpoint API `PATCH LabContext/LabResultFeature/verify`.
  - Menyelaraskan ekspor tipe perintah klinis (`LabResultRecordCmd`, `LabResultVerifyCmd`, `LabResultAmendCmd`, `LabResultDetail`, `LabResultVerificationWorklistItem`) langsung dari service layer.
- Di `lab-result.schema.ts`:
  - Mengekspor `LabResultRecordCmd` sebagai `z.input<typeof labResultRecordCmdSchema>` agar `criticalNote?: string` bersifat opsional pada input dan otomatis ter-default ke string kosong typesafe.
- Di `LabResultEntryPaForm.vue`:
  - Memperbaiki impor icon package `@lucide/vue` (menggantikan `lucide-vue-next`).
- Menambahkan unit test terarah komprehensif pada `src/modules/Laboratory/__tests__/LabServices.spec.ts` (9 tests passed).
- Verifikasi post-edit: `npx prettier --write` terformat rapi, `pnpm tc:app` sukses (0 errors), targeted unit tests `LabServices.spec.ts` (9/9) dan `lab-result.schema.spec.ts` (10/10) lulus, oxlint dan eslint bersih (0 error, 0 warning) pada berkas yang dimodifikasi.
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/queries/LabOrderService.ts`
- `c012_myhospital_web/src/modules/Laboratory/queries/LabResultService.ts`
- `c012_myhospital_web/src/modules/Laboratory/types/lab-result.schema.ts`
- `c012_myhospital_web/src/modules/Laboratory/components/result/LabResultEntryPaForm.vue`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/LabServices.spec.ts`
- `b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-WORKBENCH-IMPLEMENTATION-PLAN.md`

---

## P3 - Frontend Workstation Routing & Data Entry Forms (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P3-S07

Title: Persona Routing Wrapper & Menu Tab Re-activation  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengaktifkan kembali konfigurasi rute menu tab `hasil_lab` pada `configs/tabs.ts` dan merefaktor `views/HasilLab.vue` dari placeholder `ModuleUnderDev.vue` menjadi *route wrapper* terpadu yang memanggil `LabWorkflowView.vue` dengan initial query parameter `activeQueue = 'verification'`.  
Depends On: None  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Menu tab Hasil Lab aktif dan dapat diklik pada navigasi modul laboratorium.
- Membuka halaman Hasil Lab langsung menyajikan tampilan meja kerja terpadu dengan antrean verifikasi medis Sp.PK aktif secara otomatis.  
Notes: Realisasi GAP-BFF-06 dan TD-05.  
Implementation Notes:
- Re-activated menu tab `hasil_lab` with `CheckCircle` icon and `views/HasilLab.vue` component in `src/modules/Laboratory/configs/tabs.ts`.
- Refactored `views/HasilLab.vue` from placeholder `ModuleUnderDev.vue` into a route wrapper rendering `<LabWorkflowView initial-queue="verification" />` and synchronizing the router query `activeQueue = 'verification'`.
- Extended `views/LabWorkflowView.vue` and `useLabWorkflow.ts` to support optional `initialQueue?: LabQueueKey` prop and query fallback, preserving `'ordered'` default when not specified.
- Verified with Prettier formatting, `pnpm tc:app` (0 errors), oxlint (0 errors on affected files), and eslint (0 errors on affected files).  
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/configs/tabs.ts`
- `c012_myhospital_web/src/modules/Laboratory/views/HasilLab.vue`
- `c012_myhospital_web/src/modules/Laboratory/views/LabWorkflowView.vue`
- `c012_myhospital_web/src/modules/Laboratory/composables/useLabWorkflow.ts`  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/configs/tabs.ts`
- `c012_myhospital_web/src/modules/Laboratory/views/HasilLab.vue`

---

### P3-S08

Title: Spreadsheet-like Tabular Grid Data Entry & Inline Panic Values Guard  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mentransformasikan komponen entri PK `LabResultEntryPkTabular.vue` menjadi format tabel HTML grid terpadu yang mendukung navigasi keyboard fluid (`Enter`/`Tab` memindahkan kursor ke baris berikutnya), auto-flagging instan di sisi client (*Normal/Low/High/Critical*), shortcut global `Ctrl+S`, penanda soft-warning merah berkedip pada analit kritis, dan penegakan *inline mandatory guard* pada field `CriticalNote` sebelum tombol simpan aktif.  
Depends On: P2-S05, P2-S06  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Analis dapat mengisi puluhan hasil uji secara berurutan menggunakan tombol `Enter` atau `Tab`.
- Nilai di luar rentang rujukan langsung diberi label flag visual secara instan.
- Jika terdapat analit berstatus `Critical`, tombol simpan dinonaktifkan sampai field `CriticalNote` diisi secara inline.
- Shortcut `Ctrl+S` memicu penyimpanan hasil.  
Notes: Realisasi GAP-BFF-04, GAP-BFF-07, TD-04, TD-06, dan SCR-LAB-RES-03, SCR-LAB-RES-08.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/components/result/LabResultEntryPkTabular.vue`
Implementation Notes:
- Mentransformasikan `LabResultEntryPkTabular.vue` menjadi spreadsheet grid berkecepatan tinggi dengan HTML table terpadu (#, Parameter Uji, Nilai Hasil, Satuan, Nilai Rujukan, Penanda Flag, dan Catatan Analit).
- Mengimplementasikan navigasi keyboard fluid: menekan `Enter` atau `Tab` pada input nilai analit otomatis memindahkan fokus kursor ke input baris berikutnya (dan `Shift+Tab` / `ArrowUp` ke baris sebelumnya, serta `ArrowDown` ke baris berikutnya). Jika berada di baris terakhir dan ada nilai kritis, fokus otomatis diarahkan ke input `CriticalNote`.
- Mengembangkan domain helper `lab-flagging.ts` (`evaluateAnalyteFlag` & `parseReferenceRangeBounds`) yang mengevaluasi nilai numerik secara instan di sisi client terhadap rentang rujukan dan batas kritis (`critLow`/`critHigh` atau lookup klinis standar: Hb, WBC, PLT, Glu, Kalium, Natrium, dll.) untuk menyajikan badge status `Normal` (hijau), `Low` (biru), `High` (kuning/oranye), atau `Critical` (merah menyala).
- Menambahkan soft-warning banner nilai kritis mencolok berkedip (`🚨 Peringatan Nilai Kritis (Panic Value) Terdeteksi!`) di baris paling atas lengkap dengan ringkasan temuan kritis terperinci.
- Menegakkan *inline mandatory guard* pada `CriticalNote`: tombol simpan otomatis dinonaktifkan (`disabled`) saat terdapat analit kritis sampai field catatan pelaporan telepon (`criticalNote`) diisi secara inline (tanpa popup dialog modal yang membekukan layar), lengkap dengan tombol template cepat `+ Template Telp`.
- Menyediakan shortcut keyboard global `Ctrl+S` untuk memicu penyimpanan hasil instan.
- Mengintegrasikan fungsi simpan dengan mutasi backend `useRecordResultMutation` dan emit form `'save'`, serta mengekspos `buildRecordCommand(userId)`, `resetToDefaults()`, dan `hasCriticalValues`.
- Menambahkan suite pengujian unit terarah komprehensif pada `src/modules/Laboratory/__tests__/LabResultEntryPkTabular.spec.ts` (7 tests lulus 100%).
- Verifikasi post-edit: Prettier terformat rapi, `pnpm tc:app` lulus (0 type errors), targeted vitest specs lulus 100%, dan oxlint/eslint bersih (0 errors, 0 warnings).
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/components/result/LabResultEntryPkTabular.vue`
- `c012_myhospital_web/src/modules/Laboratory/lib/lab-flagging.ts`
- `c012_myhospital_web/src/modules/Laboratory/lib/lab-formatters.ts`
- `c012_myhospital_web/src/modules/Laboratory/types/lab-enums.ts`
- `c012_myhospital_web/src/modules/Laboratory/types/lab-result.schema.ts`
- `c012_myhospital_web/src/modules/Laboratory/composables/useLabResultEntry.ts`
- `c012_myhospital_web/src/modules/Laboratory/components/result/LabResultFlagBadge.vue`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/LabResultEntryPkTabular.spec.ts`

---

### P3-S09

Title: Adaptive Pathology Anatomy Multi-Disciplinary Form & PA Register Numbering  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menyesuaikan komponen formulir Patologi Anatomi `LabResultEntryPaForm.vue` agar adaptif menyajikan input Makroskopis (Gross Description) untuk Histopatologi (`DISC-PAH`) vs Kecukupan Sediaan (Specimen Adequacy) dan Rekomendasi Klinis untuk Sitopatologi (`DISC-PAS`), menampilkan nomor register fisik PA (`H.{YY}.{NoUrut}` vs `S.{YY}.{NoUrut}`), dan menyediakan chip pilihan cepat diagnosis PA.  
Depends On: P2-S05  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Tampilan form PA beradaptasi otomatis sesuai jenis disiplin pemeriksaan order PA.
- Nomor register PA tampil jelas pada header form.
- Tombol chip diagnosis cepat mengisikan teks kesimpulan diagnosis secara otomatis.  
Notes: Realisasi GAP-BFF-10, OQ-BFF-01, TD-08, dan SCR-LAB-RES-03.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/components/result/LabResultEntryPaForm.vue`
Implementation Notes:
- Merefaktor komponen `LabResultEntryPaForm.vue` menjadi formulir adaptif multidisiplin Patologi Anatomi:
  - Header menampilkan nomor register fisik PA secara otomatis dengan format `H.{YY}.{NoUrut}` untuk Histopatologi vs `S.{YY}.{NoUrut}` untuk Sitopatologi (menggunakan prop `paRegNo`, komponen tersimpan `PA_REG_NO`, atau derivasi sequence order).
  - Adaptif menyajikan field: untuk Histopatologi (`DISC-PAH`/`PAH`) menyajikan input Makroskopis (Gross Description), Mikroskopis, dan Kesimpulan Diagnosis; untuk Sitopatologi (`DISC-PAS`/`PAS`) menyajikan Adekuasi Sediaan (Specimen Adequacy) beserta pilihan cepat seluler, Mikroskopis, Kesimpulan Diagnosis, dan Rekomendasi Klinis.
  - Menyediakan tombol chip template diagnosis cepat ("Radang Kronis Non-Spesifik", "Fibroadenoma Mammae", "Negatif Keganasan", "Infiltrating Ductal Carcinoma", dsb.) yang ketika diklik langsung mengisi teks kesimpulan diagnosis, kode SNOMED/ICD-O, dan rekomendasi klinis.
  - Memperluas skema validasi `labResultPaFormSchema`, `buildInitialLabResultPaFormValues`, dan mapper `mapLabResultPaFormToRecordCmd` di `lab-result.schema.ts` untuk menangani `paRegNo`, `specimenAdequacy`, dan `recommendation` secara typesafe.
- Menambahkan unit test terarah pada `src/modules/Laboratory/__tests__/LabResultEntryPaForm.spec.ts` (7 tests lulus 100%).
- Verifikasi post-edit: Prettier terformat rapi, `pnpm tc:app` lulus (0 type error), targeted vitest specs (`LabResultEntryPaForm.spec.ts` & `lab-result.schema.spec.ts`) lulus 100%, serta oxlint dan eslint bersih (0 error, 0 warning) pada berkas yang dimodifikasi.
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/components/result/LabResultEntryPaForm.vue`
- `c012_myhospital_web/src/modules/Laboratory/types/lab-result.schema.ts`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/LabResultEntryPaForm.spec.ts`

---

## P4 - Sp.PK Verification Station & Worklist UI (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P4-S10

Title: Rich Sp.PK Verification Worklist Card with CITO Priority Badge  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui tampilan kartu antrean verifikasi `LabVerificationWorklistRow.vue` untuk menyajikan badge prioritas CITO merah menyala di baris paling atas, penanda temuan nilai kritis, ringkasan nama-nama uji yang diperiksa, demografi pasien (Gender, Usia), dokter & unit perujuk, serta badge sumber LIS (`[🤖 LIS Bridge • Tervalidasi Otomatis]`).  
Depends On: P2-S05  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Kartu verifikasi menampilkan informasi lengkap pasien dan pemeriksaan tanpa mengharuskan dokter membuka kanvas detail terlebih dahulu.
- Order CITO dan temuan nilai kritis tampak sangat menonjol secara visual.  
Notes: Realisasi GAP-BFF-02 dan SCR-LAB-RES-01, SCR-LAB-RES-01B.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/components/verification/LabVerificationWorklistRow.vue`
Implementation Notes:
- Mengimplementasikan `c012_myhospital_web/src/modules/Laboratory/components/verification/LabVerificationWorklistRow.vue` sebagai kartu kerja kaya informasi untuk workstation Sp.PK:
  1. Menampilkan badge prioritas CITO merah menyala berkedip (`animate-pulse-slow`) dan border kiri merah (`border-l-4 border-l-rose-600`) jika `item.isCito === true`, serta badge Rutin jika bukan CITO.
  2. Menampilkan banner nilai kritis merah menyala (`⚠️ NILAI KRITIS TERDETEKSI`) jika `item.hasCriticalValue === true`.
  3. Menyajikan ringkasan nama-nama uji analitis yang diperiksa (`item.testNames`).
  4. Menyajikan demografi pasien lengkap: Gender (`item.gender`) dan Usia (`item.ageAtOrder` tahun).
  5. Menyajikan konteks perujuk lengkap: Layanan/Unit Perujuk (`item.layananPerujukName`) dan Dokter Perujuk (`item.dokterPerujukName`), dengan fallback 'Laboratorium Sentral'.
  6. Menampilkan badge sumber LIS (`[🤖 LIS Bridge • Tervalidasi Otomatis]`) jika `item.resultSource === 1` atau `ResultSource.ExternalLis (3)`.
  7. Menampilkan nomor versi amandemen (`v{versionNo}`) jika `versionNo > 1`, tanggal/waktu rekam terformat rapi, nomor rekam medis (`patientId`), serta badge status verifikasi medis.
- Memperbarui `c012_myhospital_web/src/modules/Laboratory/components/workspace/LabVerificationWorklistRow.vue` untuk mendelegasikan secara transparan ke komponen di `verification/` agar kompatibel penuh dengan pemanggilan eksisting.
- Menambahkan suite pengujian unit terarah `src/modules/Laboratory/__tests__/LabVerificationWorklistRow.spec.ts` (13/13 tes passed).
- Verifikasi post-edit: Prettier terformat rapi, `pnpm tc:app` lulus (0 errors), targeted unit tests `LabVerificationWorklistRow.spec.ts` (13/13) dan `LabOrderWorklistPanel.spec.ts` (10/10) lulus 100%, serta oxlint dan eslint bersih (0 error, 0 warning) pada seluruh berkas yang dimodifikasi.
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/components/verification/LabVerificationWorklistRow.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/workspace/LabVerificationWorklistRow.vue`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/LabVerificationWorklistRow.spec.ts`

---

### P4-S11

Title: Unified Sp.PK Verification Canvas & Direct 1-Click Action  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mentransformasikan kanvas `LabVerificationPanel.vue` menjadi stasiun review medis Sp.PK terpadu yang menampilkan banner ringkasan temuan abnormal/kritis di baris paling atas, editor catatan ekspertise patolog dengan tombol chip template cepat (*Dalam Batas Normal*, *Anemia Mikrositik*, *Hiperglikemia*, *Reset*), dan tombol verifikasi 1-klik langsung (*Direct 1-Click Verification*) tanpa memicu dialog popup modal berulang.  
Depends On: P2-S05, P2-S06  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Banner ringkasan di baris atas merangkum analit abnormal/kritis secara instan.
- Chip template klinis langsung mengisi textarea `ExpertiseNote`.
- Mengklik tombol *"🩺 Sahkan & Verifikasi Medis (1 Klik)"* langsung mengeksekusi mutasi verifikasi ke backend dan memutakhirkan antrean kerja.  
Notes: Realisasi GAP-BFF-05, GAP-BFF-08, TD-01, dan SCR-LAB-RES-04, SCR-LAB-RES-05.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/components/verification/LabVerificationPanel.vue`
Implementation Notes:
- Mentransformasikan `LabVerificationPanel.vue` menjadi stasiun telaah medis & ekspertise Sp.PK terpadu:
  1. Menambahkan banner ringkasan temuan kritis (merah berkedip dengan catatan pelaporan readback kritis dan kartu analit) saat terdeteksi parameter `Critical` (`flagStatus === 4`).
  2. Menambahkan banner ringkasan temuan abnormal (amber/sky untuk `High` / `Low`) saat parameter uji berada di luar rentang rujukan.
  3. Menambahkan banner hijau saat seluruh parameter uji analitis berada dalam rentang rujukan normal.
  4. Menambahkan banner informasi LIS Bridge saat order bersumber dari LIS eksternal (`ResultSource.ExternalLis`), mencantumkan identitas validator LIS dan stempel waktu validasi otomatis.
  5. Menambahkan banner dokumen terverifikasi medis ("Medical Validity Clear") jika order atau dokumen hasil sudah berstatus `Verified`.
  6. Menyediakan editor catatan ekspertise patolog (`expertiseNote` textarea) dengan sinkronisasi reaktif, counter karakter (maks. 2000 karakter), dan tombol chip template klinis cepat: `+ Dalam Batas Normal`, `+ Anemia Mikrositik Hipokrom`, `+ Hiperglikemia / Terduga Diabetes`, `+ Trombositopenia CITO`, serta tombol `Reset` untuk mengosongkan input.
  7. Menambahkan tombol aksi langsung 1-klik *"🩺 Sahkan & Verifikasi Medis (1 Klik)"* yang langsung memanggil `useVerifyResultMutation` dengan payload `orderId`, `verifiedUserId`, `verifiedDate`, dan `expertiseNote`, tanpa memicu dialog popup konfirmasi modal berulang. Dilengkapi indikator loading spinner (`Loader2`) saat mutasi berjalan, feedback toast sukses/error, dan emisi event `verified`.
- Menambahkan suite pengujian unit terarah komprehensif pada `src/modules/Laboratory/__tests__/LabVerificationPanel.spec.ts` (10 tests lulus 100%).
- Verifikasi post-edit: Prettier terformat rapi, `pnpm tc:app` lulus 100% (0 errors), targeted unit tests `LabVerificationPanel.spec.ts` (10/10) dan `LabVerificationWorklistRow.spec.ts` (13/13) lulus, serta oxlint dan eslint bersih (0 error, 0 warning) pada berkas yang dimodifikasi.
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/components/verification/LabVerificationPanel.vue`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/LabVerificationPanel.spec.ts`

---

## P5 - Release Gating, PDF Preview & Strict Amendment Dialogs (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P5-S12

Title: Single-Click Release & Retirement of Fake Cashier Clearance UI  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui panel `LabReleaseQueuePanel.vue` dengan menghapus alur persetujuan kasir manual (`LabFinancialClearancePanel.vue`), menggantikannya dengan tombol tunggal *"🖨️ Cetak & Rilis Hasil"* yang memanggil mutasi rilis, mengevaluasi respon `LabOrderReleaseResponse`: jika `CLEAR` langsung membuka dialog PDF hasil resmi, jika `BLOCKED` menyajikan banner alert informatif ramah bahwa dokumen fisik ditahan tanpa error crash.  
Depends On: P2-S06  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Komponen dan tombol persetujuan kasir usang berhasil dibersihkan dari antrean rilis.
- Tombol rilis menangani respons `CLEAR` dan `BLOCKED` secara mulus tanpa unhandled runtime exception.  
Notes: Realisasi GAP-BFF-03, GAP-BFF-09, TD-07, dan SCR-LAB-RES-07.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/components/release/LabReleaseQueuePanel.vue`
Implementation Notes:
- Mengimplementasikan `LabReleaseQueuePanel.vue` di `c012_myhospital_web/src/modules/Laboratory/components/release/`:
  1. Membersihkan tombol persetujuan kasir usang/manual yang tidak realistis di modul laboratorium (`LabFinancialClearancePanel.vue`), mendeprekasikannya dan mendelegasikannya secara langsung ke `LabReleaseQueuePanel.vue`.
  2. Menyediakan tombol tunggal terpadu *"🖨️ Cetak & Rilis Hasil"* (`data-testid="btn-print-and-release"`) yang memanggil mutasi rilis `useReleaseOrderMutation` secara typesafe.
  3. Mengevaluasi respons objek `LabOrderReleaseResponse`:
     - Jika `CLEAR` (atau `res.released === true`): status rilis sukses, memunculkan notifikasi toast sukses, membuka otomatis dialog pratinjau lembar hasil PDF resmi (`LabResultPdfPreviewDialog.vue`) untuk pencetakan fisik langsung, serta meng-emit event `released` dan `refresh`.
     - Jika `BLOCKED`: menampilkan banner alert peringatan informatif yang ramah (*"Pemberian hasil fisik ditunda: Tagihan belum selesai di loket kasir / Tata Rekening (TRK-BILLING). Pasien dapat diarahkan menyelesaikan administrasi terlebih dahulu."*) lengkap dengan catatan penegasan bahwa akses lembar hasil medis digital internal tetap aktif dan dapat diakses oleh dokter DPJP di Rekam Medis (EMR), tanpa unhandled runtime crash/exception, serta meng-emit event `blocked`.
  4. Menyajikan kartu riwayat rilis saat order berstatus `Released`, lengkap dengan waktu rilis, identitas petugas, catatan rilis, dan tombol cetak ulang PDF (`"🖨️ Cetak & Pratinjau PDF Hasil"`).
  5. Memperbarui `LabOrderDetailPanel.vue` untuk mengonsumsi `LabReleaseQueuePanel.vue` menggantikan komponen usang.
  6. Memperbarui method `executeRelease` pada composable `useLabWorkflow.ts` untuk memeriksa secara typesafe status `CLEAR` vs `BLOCKED`.
- Menambahkan suite pengujian unit terarah komprehensif pada `c012_myhospital_web/src/modules/Laboratory/__tests__/LabReleaseQueuePanel.spec.ts` (5/5 tes lulus 100%).
- Verifikasi post-edit: Prettier terformat rapi, `pnpm tc:app` lulus (0 type errors), targeted vitest specs lulus 100%, serta oxlint dan eslint bersih (0 warning, 0 error).
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/components/release/LabReleaseQueuePanel.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/release/LabFinancialClearancePanel.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/workspace/LabOrderDetailPanel.vue`
- `c012_myhospital_web/src/modules/Laboratory/composables/useLabWorkflow.ts`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/LabReleaseQueuePanel.spec.ts`
- `b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-WORKBENCH-IMPLEMENTATION-PLAN.md`

---

### P5-S13

Title: Official Result PDF Preview Modal Dialog  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Membangun komponen dialog modal `LabResultPdfPreviewDialog.vue` di bawah direktori `components/dialogs/` yang menerima blob stream dari `fetchResultPdfBlob(orderId)`, menyajikannya dalam viewer PDF yang responsif, serta menyediakan tombol cetak langsung ke printer lokal dan tombol tutup.  
Depends On: None  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Dialog modal menampilkan berkas PDF resmi dari backend dengan layout responsif.
- Tombol cetak memicu dialog print peramban secara langsung.  
Notes: Realisasi GAP-BFF-11 dan SCR-LAB-RES-10.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/components/dialogs/LabResultPdfPreviewDialog.vue`
Implementation Notes:
- Membangun komponen `LabResultPdfPreviewDialog.vue` di `c012_myhospital_web/src/modules/Laboratory/components/dialogs/` dengan dukungan props `open`, `modelValue`, `orderId`, `pdfBlob`, `pdfUrl`, `orderNo`, `patientName`, dan `title`.
- Mengintegrasikan penarikan berkas blob PDF via `fetchResultPdfBlob(orderId)` dari `useLabResultService()` lengkap dengan status loading spinner dan penanganan error ramah pengguna yang dilengkapi tombol coba lagi.
- Menyajikan penampil responsif terintegrasi menggunakan `iframe` dan pengelolaan daur hidup object URL (`URL.createObjectURL` / `URL.revokeObjectURL`) saat dialog dibuka, ditutup, atau unmounted.
- Menyediakan tombol aksi "🖨️ Cetak" (memicu iframe contentWindow print atau fallback window print), "Unduh Berkas", "Buka Tab Baru", dan "Tutup" (meng-emit `update:open`, `update:modelValue`, dan `close`).
- Membuat unit test terarah komprehensif pada `c012_myhospital_web/src/modules/Laboratory/__tests__/LabResultPdfPreviewDialog.spec.ts` (8 tes lulus 100%).
- Memverifikasi format kode dengan Prettier, linting oxlint/eslint (0 error, 0 warning), dan type-checking `pnpm tc:app` (0 error).
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/components/dialogs/LabResultPdfPreviewDialog.vue`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/LabResultPdfPreviewDialog.spec.ts`

---

### P5-S14

Title: Strict Amendment Modal Dialog & Authorization Restriction  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui dialog amandemen hasil `LabAmendResultDialog.vue` untuk membatasi akses amandemen hanya bagi pengguna dengan peran Dokter Sp.PK (`doktersppk`) atau Supervisor Lab (`supervisor`), serta menegakkan validasi input alasan amandemen minimal 10 karakter (**EX-03**) dengan indikator counter karakter visual.  
Depends On: P2-S05, P2-S06  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Tombol eksekusi amandemen dinonaktifkan jika teks alasan kurang dari 10 karakter.
- Pengguna tanpa peran Sp.PK atau Supervisor tidak dapat mengakses aksi amandemen.  
Notes: Realisasi GAP-BFF-12, TD-09, dan SCR-LAB-RES-06.  
Target Files:
- `c012_myhospital_web/src/modules/Laboratory/components/dialogs/LabAmendResultDialog.vue`
Implementation Notes:
- Memperbarui `LabAmendResultDialog.vue` di `c012_myhospital_web/src/modules/Laboratory/components/dialogs/` dengan:
  - Pembatasan otorisasi ketat berbasis helper `checkAmendmentAuthorization`: hanya mengizinkan peran Dokter Sp.PK (`doktersppk`, `dokter_sppk`, `sp.pk`) atau Supervisor Lab (`supervisor`, `admin`).
  - Banner peringatan akses ditolak (`data-testid="unauthorized-warning"`) dan tombol "Tutup" khusus jika pengguna tidak memiliki hak akses amandemen.
  - Penegakan invarian alasan amandemen resmi minimal 10 karakter (**EX-03**) dengan visual character counter (`data-testid="char-counter"`, `${trimmedLength} / 10 karakter`), warning teks saat < 10 karakter, serta penonaktifan tombol "Ajukan Amandemen Hasil" (`:disabled="!isAuthorized || !isReasonValid || isPending"`).
  - Integrasi mutasi amandemen `useAmendResultMutation` dari `LabResultService.ts`: ketika disubmit dengan `orderId`, memanggil mutasi backend `PATCH LabContext/LabResultFeature/amend`, menampilkan notifikasi sukses `vue-sonner`, meng-emit event `success` dan `amended` untuk pengalihan ke versi baru berstatus `Recorded`, serta mendukung fallback event `submit` untuk parent orchestrator.
- Menyelesaikan sinkronisasi tipe `lab-enums.ts` (`ResultStatus.Released = 4`, `ResultStatus.Amended = 5`), penyesuaian properti DTO pembacaan user di `LabVerificationPanel.vue` dan `LabResultEntryPkTabular.vue`, serta normalisasi inisialisasi mutasi pada `LabReleaseQueuePanel.vue`.
- Menambahkan suite pengujian unit terarah komprehensif pada `c012_myhospital_web/src/modules/Laboratory/__tests__/LabAmendResultDialog.spec.ts` (8 tes lulus 100%).
- Memverifikasi kode dengan Prettier, type-check `pnpm tc:app` (0 error), oxlint dan eslint (0 error, 0 warning pada seluruh berkas yang disentuh).
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/components/dialogs/LabAmendResultDialog.vue`
- `c012_myhospital_web/src/modules/Laboratory/__tests__/LabAmendResultDialog.spec.ts`
- `c012_myhospital_web/src/modules/Laboratory/types/lab-enums.ts`
- `c012_myhospital_web/src/modules/Laboratory/components/verification/LabVerificationPanel.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/result/LabResultEntryPkTabular.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/release/LabReleaseQueuePanel.vue`
- `b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-WORKBENCH-IMPLEMENTATION-PLAN.md`

---

## P6 - Multi-Repo End-to-End Verification & Acceptance Gate (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P6-S15

Title: Comprehensive Frontend Type-Checking, Targeted Specs & Lint Verification  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memverifikasi integritas tipe, pemformatan kode, dan linting secara menyeluruh pada repositori `c012_myhospital_web` pasca implementasi seluruh irisan kerja frontend sesuai protokol penjaminan kualitas.  
Depends On: P3-S07, P3-S08, P3-S09, P4-S10, P4-S11, P5-S12, P5-S13, P5-S14  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Pemformatan Prettier berjalan sukses pada seluruh berkas yang dimodifikasi.
- `cd c012_myhospital_web && pnpm tc:app` berhasil dengan 0 type errors.
- `cd c012_myhospital_web && pnpm lint` berhasil dengan 0 lint errors/warnings.
- Seluruh targeted unit test terkait Hasil Lab lulus 100%.  
Notes: Gerbang verifikasi akhir frontend sebelum diserahkan ke peninjauan Reviewer (`ica-reviewer`).  
Target Files:
- Seluruh berkas frontend pada `c012_myhospital_web` yang disentuh pada P2, P3, P4, dan P5.
Implementation Notes:
- Melaksanakan penjaminan kualitas menyeluruh pada repositori `c012_myhospital_web`:
  1. Menjalankan pemformatan Prettier pada seluruh berkas modul Laboratory (`npx prettier --write "src/modules/Laboratory/**/*.{ts,vue}"`), terformat rapi 100%.
  2. Melakukan pemeriksaan tipe menyeluruh via `pnpm tc:app` (`vue-tsc --noEmit --project tsconfig.app.json`), lulus dengan 0 type errors.
  3. Menjalankan 9 berkas targeted unit test modul Laboratory yang mencakup seluruh spesifikasi meja kerja hasil lab (`LabResultPdfPreviewDialog.spec.ts`, `lab-result.schema.spec.ts`, `LabServices.spec.ts`, `LabResultEntryPaForm.spec.ts`, `LabVerificationWorklistRow.spec.ts`, `LabReleaseQueuePanel.spec.ts`, `LabAmendResultDialog.spec.ts`, `LabVerificationPanel.spec.ts`, `LabResultEntryPkTabular.spec.ts`), seluruh 77 unit test lulus 100% (77/77 passed).
  4. Menjalankan linter oxlint dan eslint terarah pada seluruh berkas yang disentuh dan dimodifikasi pada P2, P3, P4, dan P5, lulus bersih tanpa peringatan dan tanpa galat (0 errors, 0 warnings).
Changed Files:
- `c012_myhospital_web/src/modules/Laboratory/__tests__/lab-order-mutations.spec.ts`
- `c012_myhospital_web/src/modules/Laboratory/archive/TindakanLab.vue`
- `c012_myhospital_web/src/modules/Laboratory/archive/constants.ts`
- `c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/dialogs/LabCollectSpecimenDialog.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/dialogs/LabDeferOrderDialog.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/dialogs/LabTerminateOrderDialog.vue`
- `c012_myhospital_web/src/modules/Laboratory/components/release/LabReleaseDialog.vue`
- `c012_myhospital_web/src/modules/Laboratory/composables/useLabWorkflowActions.ts`
- `c012_myhospital_web/src/modules/Laboratory/lib/lab-barcode.ts`
- `b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-WORKBENCH-IMPLEMENTATION-PLAN.md`

---

# 6. Change Log

- **2026-11-10 (v1.0)**: Penyusunan rencana implementasi komprehensif UI & BFF konsol meja kerja hasil laboratorium (`OC-LAB-HASIL-LAB`), mencakup 6 fase dan 15 irisan kerja yang merealisasikan 12 keputusan celah BFF (GAP-BFF-01 s/d GAP-BFF-12), 7 kesepakatan sesi *Grill-Me Alignment*, serta target arsitektur teknis v1.2. Status persetujuan eksekusi: `Execution Approval: APPROVED`.
