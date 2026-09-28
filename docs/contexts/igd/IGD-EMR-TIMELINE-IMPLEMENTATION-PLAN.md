---
Title: IGD Triage EMR Timeline Integration Implementation Plan
Code: IGD-EMR-TIMELINE
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-09-28
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan arsitektur target yang telah disetujui pada [IGD-EMR-TIMELINE-ARCHITECTURE.md](file:///D:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/igd/IGD-EMR-TIMELINE-ARCHITECTURE.md) (V1.0) untuk menyelesaikan bug [ISSUE-IGD-EMR-TIMELINE-001](file:///D:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/igd/IGD-EMR-TIMELINE-ISSUE.md):
Setiap kali asesmen IGD Triage berhasil dihubungkan ke pendaftaran administrasi (`RegId`) via pemanggilan SMASS `linkIgdVisit`, BILREG secara otomatis mendaftarkan entitas label klinis ke EMR 2.0 API (`POST /api/LabelV2/AddSmass`) untuk seluruh asesmen yang sukses di-link, sehingga kronologi riwayat asesmen triage muncul secara otomatis pada Timeline EMR pasien (`GET /api/Label/TimeLine`).

Referenced artifacts:
- ARCHITECTURE: [IGD-EMR-TIMELINE-ARCHITECTURE.md](file:///D:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/igd/IGD-EMR-TIMELINE-ARCHITECTURE.md) (V1.0)
- BUG-INVESTIGATION: [IGD-EMR-TIMELINE-BUG-INVESTIGATION.md](file:///D:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/igd/IGD-EMR-TIMELINE-BUG-INVESTIGATION.md)
- BUG ISSUE: [IGD-EMR-TIMELINE-ISSUE.md](file:///D:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/igd/IGD-EMR-TIMELINE-ISSUE.md)
- Prior Architecture Context: [igd-triage-abc-smass-architecture.md](file:///D:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-architecture.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Target repository:
- `b09-bilreg-api` (Seluruh slice menargetkan satu repositori ini).

In scope (bagian arsitektur yang direalisasikan):
- Perluasan tipe data `SmassGatewayResult` dan ekstraksi `ListAssesmentId` pada `SmassAssessmentGateway.LinkIgdVisit`.
- Penambahan port outbound `IEmrLabelGateway` beserta DTO request/response di `Bilreg.Application.IgdContext.Integration`.
- Penambahan adapter outbound `EmrLabelGateway` RestSharp di `Bilreg.Infrastructure.IgdContext.Integration`.
- Penambahan konfigurasi opsi `Emr20Options` dan pembaruan `IgdVisitOptions` (`SmassTriagePaperName`).
- Pendaftaran service dan binding options pada `InfrastructureService.cs` dan `appsettings.json`.
- Integrasi orkestrasi pendaftaran label pada hook pasca-commit `IgdVisitSmassLinkHook` dan pemanggilnya (`IgdVisitAssignRegisterCmd`, `IgdVisitReplaceRegisterCmd`).
- Dukungan pembuatan label EMR pada retry manual task link di `IgdVisitSmassTaskRetryExecutor`.
- Unit test komprehensif pada setiap slice menggunakan xUnit, Moq, dan FluentAssertions.

Out of scope (arsitektur §3 Excluded):
- Perubahan kode backend SMASS API (`SMASSAPI`) dan EMR 2.0 API (`emr20-api`).
- Perubahan frontend web (`c012_myhospital_web`).
- Perubahan skema database DDL di BILREG (`HOSPITAL_PKL`).
- Migrasi atau backfilling data riwayat lama secara massal.

---

# 3. Dependencies

External dependencies:
- Endpoint `{SmassApi} PATCH /api/Assesment/linkIgdVisit` telah beroperasi dan mengembalikan properti `ListAssesmentId`.
- Endpoint `{Emr20Api} POST /api/LabelV2/AddSmass` telah tersedia di lingkungan EMR 2.0.
- Penggunaan utilitas eksisting: `IRestClientFactory`, `IConfiguration`, MediatR, dan Nuna library.

Slice dependencies:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED`.
- Kepuasan dependensi tidak mensyaratkan status review `GO`.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Contracts & Configuration Foundation | IMPLEMENTED | GO | 2/2 |
| P2 - Outbound Adapter & Infrastructure Wiring | IMPLEMENTED | GO | 1/1 |
| P3 - Post-Commit Hook & Registration Handlers | IMPLEMENTED | GO | 2/2 |
| P4 - Manual Retry & Worklist Integration | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Contracts & Configuration Foundation

Implementation Status: IMPLEMENTED  
Review Status: GO

### P1-S01

Title: SMASS Gateway Result Extension & Link Response Mapping

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperluas kontrak `SmassGatewayResult` di Application layer agar mengekspos properti `LinkedAssessmentIds` (`IReadOnlyList<string>`), dan memperbarui `SmassAssessmentGateway.LinkIgdVisit` di Infrastructure layer untuk mengekstrak `ListAssesmentId` dari respons `LinkResponse` SMASS.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:
1. `SmassGatewayResult` memiliki parameter `IReadOnlyList<string>? LinkedAssessmentIds = null` dengan nilai default null agar kompatibel ke belakang.
2. `SmassAssessmentGateway.LinkIgdVisit` berhasil memetakan `envelope.Data.ListAssesmentId` ke `LinkedAssessmentIds` pada objek `SmassGatewayResult` yang dikembalikan saat status sukses.
3. Unit test di `Bilreg.Test` memverifikasi bahwa respons sukses SMASS `linkIgdVisit` yang berisi `ListAssesmentId` menghasilkan `SmassGatewayResult` dengan `LinkedAssessmentIds` yang sesuai.

Notes:
- Lokasi file: `src/bilreg/Bilreg.Application/IgdContext/Integration/SmassGatewayResult.cs`
- Lokasi file: `src/bilreg/Bilreg.Infrastructure/IgdContext/Integration/SmassAssessmentGateway.cs`
- Implementation Notes:
  - Added `IReadOnlyList<string>? LinkedAssessmentIds = null` optional parameter to `SmassGatewayResult` record.
  - Updated `SmassAssessmentGateway.LinkIgdVisit` to map `envelope.Data.ListAssesmentId ?? Array.Empty<string>()` to `LinkedAssessmentIds`.
  - Added unit test suite `SmassAssessmentGatewayTest` in `Bilreg.Test` covering success with assessment IDs, empty array, null array, fail status, HTTP error, and backwards compatibility with `GenerateIgdTriage`.
- Changed Files:
  - `src/bilreg/Bilreg.Application/IgdContext/Integration/SmassGatewayResult.cs`
  - `src/bilreg/Bilreg.Infrastructure/IgdContext/Integration/SmassAssessmentGateway.cs`
  - `src/bilreg/Bilreg.Test/IgdContext/Integration/SmassAssessmentGatewayTest.cs`

---

### P1-S02

Title: EMR 2.0 Integration Port, DTOs, and Configuration Options

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Mendefinisikan kontrak interface outbound port `IEmrLabelGateway` beserta request DTO `EmrAddSmassLabelRequest` dan result DTO `EmrLabelGatewayResult` di Application layer, menambahkan kelas opsi `Emr20Options`, memperluas `IgdVisitOptions` dengan properti `SmassTriagePaperName`, serta menambahkan konfigurasi default di `appsettings.json`.

Depends On: None

Repository: `b09-bilreg-api`

Completion Criteria:
1. File `EmrAddSmassLabelRequest.cs` (atau di dalam kontrak gateway) mendefinisikan field: `AssesmentId`, `LayananId`, `PaperId`, `PaperName`, `RegId`, dan `UserrId` (dua huruf 'r' sesuai kontrak API EMR 2.0).
2. Interface `IEmrLabelGateway` terdefinisi dengan method `Task<EmrLabelGatewayResult> AddSmassLabel(EmrAddSmassLabelRequest request, CancellationToken cancellationToken = default);`.
3. Kelas `Emr20Options` dibuat di `Bilreg.Infrastructure.Shared.Helpers` dengan `SECTION_NAME = "Emr20"` dan properti `BaseApiUrl`.
4. Properti `public string SmassTriagePaperName { get; set; } = string.Empty;` ditambahkan ke `IgdVisitOptions`.
5. File `appsettings.json` memuat section `"Emr20": { "BaseApiUrl": "http://dev.smart-ics.com:8083/emr20-api" }` dan nilai `"SmassTriagePaperName": "FORMULIR TRIASE IGD"` di bawah section `"IgdVisit"`.

Notes:
- Lokasi file baru: `src/bilreg/Bilreg.Application/IgdContext/Integration/IEmrLabelGateway.cs`
- Lokasi file baru: `src/bilreg/Bilreg.Infrastructure/Shared/Helpers/Emr20Options.cs`
- Lokasi file modifikasi: `src/bilreg/Bilreg.Application/IgdContext/IgdVisitOptions.cs`
- Lokasi file modifikasi: `src/bilreg/Bilreg.Api/appsettings.json`
- Implementation Notes:
  - Created `IEmrLabelGateway.cs` containing `EmrAddSmassLabelRequest` (with `AssesmentId`, `LayananId`, `PaperId`, `PaperName`, `RegId`, `UserrId`), `EmrLabelGatewayResult` record, and `IEmrLabelGateway` interface with `AddSmassLabel`.
  - Created `Emr20Options.cs` in `Bilreg.Infrastructure.Shared.Helpers` with `SECTION_NAME = "Emr20"` and `BaseApiUrl`.
  - Added `SmassTriagePaperName` to `IgdVisitOptions`.
  - Added `"Emr20"` section and `"SmassTriagePaperName"` to `appsettings.json` (and `appsettings.Development.json`).
  - Added unit test suite `EmrLabelGatewayContractTest.cs` in `Bilreg.Test` verifying DTO initialization, default values, and options properties.
- Changed Files:
  - `src/bilreg/Bilreg.Application/IgdContext/Integration/IEmrLabelGateway.cs`
  - `src/bilreg/Bilreg.Infrastructure/Shared/Helpers/Emr20Options.cs`
  - `src/bilreg/Bilreg.Application/IgdContext/IgdVisitOptions.cs`
  - `src/bilreg/Bilreg.Api/appsettings.json`
  - `src/bilreg/Bilreg.Api/appsettings.Development.json`
  - `src/bilreg/Bilreg.Test/IgdContext/Integration/EmrLabelGatewayContractTest.cs`

---

## P2 - Outbound Adapter & Infrastructure Wiring

Implementation Status: IMPLEMENTED  
Review Status: GO

### P2-S03

Title: EMR 2.0 Label Gateway RestSharp Implementation & DI Registration

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Mengimplementasikan adapter `EmrLabelGateway` berbasis RestSharp di Infrastructure layer yang memanggil endpoint `{Emr20Api} POST /api/LabelV2/AddSmass`, menangani validasi konfigurasi (fail-closed jika BaseApiUrl kosong), menyerap exception agar tidak pernah melempar error tak tertangani, serta mendaftarkan dependensi di `InfrastructureService.cs`.

Depends On: P1-S02

Repository: `b09-bilreg-api`

Completion Criteria:
1. Kelas `EmrLabelGateway` mengimplementasikan `IEmrLabelGateway` menggunakan `IRestClientFactory` dan `IOptions<Emr20Options>`.
2. Jika `Emr20Options.BaseApiUrl` kosong atau whitespace, method mengembalikan failure result tanpa melakukan panggilan HTTP.
3. Melakukan HTTP POST ke endpoint `/api/LabelV2/AddSmass` dengan body JSON serialisasi `EmrAddSmassLabelRequest`.
4. Setiap kegagalan HTTP atau exception koneksi ditangkap dan dikembalikan sebagai `EmrLabelGatewayResult(false, ...)`.
5. `InfrastructureService.cs` mendaftarkan `services.AddScoped<IEmrLabelGateway, EmrLabelGateway>()` dan `services.Configure<Emr20Options>(configuration.GetSection(Emr20Options.SECTION_NAME))`.
6. Unit test memverifikasi skenario sukses, skenario kegagalan HTTP/exception, dan validasi konfigurasi kosong.

Notes:
- Lokasi file baru: `src/bilreg/Bilreg.Infrastructure/IgdContext/Integration/EmrLabelGateway.cs`
- Lokasi file modifikasi: `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs`
- Implementation Notes:
  - Created `EmrLabelGateway` in `Bilreg.Infrastructure.IgdContext.Integration` implementing `IEmrLabelGateway` using `IRestClientFactory` and `IOptions<Emr20Options>`.
  - Added fail-closed configuration validation returning failure if `BaseApiUrl` is empty or null without calling HTTP.
  - Implemented HTTP POST to `/api/LabelV2/AddSmass` serializing `EmrAddSmassLabelRequest` with timeout protection (10 seconds).
  - Absorbed HTTP failures and network exceptions into `EmrLabelGatewayResult(false, ...)`.
  - Registered `services.AddScoped<IEmrLabelGateway, EmrLabelGateway>()` and `services.Configure<Emr20Options>(configuration.GetSection(Emr20Options.SECTION_NAME))` in `InfrastructureService.cs`.
  - Added comprehensive unit tests in `EmrLabelGatewayTest.cs` covering success, payload serialization, fail-closed empty config, null request, HTTP 500 error, network exceptions, and DI registration/options binding.
- Changed Files:
  - `src/bilreg/Bilreg.Infrastructure/IgdContext/Integration/EmrLabelGateway.cs`
  - `src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs`
  - `src/bilreg/Bilreg.Test/IgdContext/Integration/EmrLabelGatewayTest.cs`

---

## P3 - Post-Commit Hook & Registration Handlers

Implementation Status: IMPLEMENTED  
Review Status: GO

### P3-S04

Title: Post-Link EMR Label Generation in IgdVisitSmassLinkHook

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui method `IgdVisitSmassLinkHook.RunAsync` untuk menerima dependensi `IEmrLabelGateway` dan parameter `userId`. Ketika hasil link SMASS sukses dan memuat `LinkedAssessmentIds`, hook secara berurutan memanggil `AddSmassLabel` untuk setiap ID asesmen, dengan penanganan try-catch fail-safe sehingga kesalahan EMR tidak mengubah status task SMASS dan tidak mengganggu alur registrasi.

Depends On: P1-S01, P2-S03

Repository: `b09-bilreg-api`

Completion Criteria:
1. Signature `IgdVisitSmassLinkHook.RunAsync` menerima `IEmrLabelGateway emrLabelGateway` dan `string userId`.
2. Setelah pemanggilan `gateway.LinkIgdVisit` berhasil (`result.Success == true`), jika `result.LinkedAssessmentIds` memiliki elemen:
   - Dilakukan iterasi untuk setiap `assessmentId`.
   - Dibangun payload `EmrAddSmassLabelRequest` dengan:
     - `AssesmentId = assessmentId`
     - `LayananId = !string.IsNullOrWhiteSpace(reg.Layanan.LayananId) ? reg.Layanan.LayananId : options.SmassLayananId`
     - `PaperId = options.SmassTriagePaperId`
     - `PaperName = options.SmassTriagePaperName`
     - `RegId = reg.RegId`
     - `UserrId = userId`
   - Mengeksekusi `emrLabelGateway.AddSmassLabel(...)`.
3. Panggilan pembuatan label dibungkus dengan proteksi error (swallowed / warning log), sehingga kegagalan EMR label tidak menggagalkan status sukses task SMASS `BILRG_IgdVisitSmassTask`.
4. Unit test memverifikasi alur pemanggilan label saat link sukses (single & multiple assessment), serta memastikan isolasi kegagalan saat panggilan EMR gagal.

Notes:
- Lokasi file modifikasi: `src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitSmassLinkHook.cs`
- Implementation Notes:
  - Updated `IgdVisitSmassLinkHook.RunAsync` signature to accept `IEmrLabelGateway emrLabelGateway` and `string userId`.
  - Added post-link sequential iteration over `result.LinkedAssessmentIds` invoking `emrLabelGateway.AddSmassLabel` with `EmrAddSmassLabelRequest` using fallback to `options.SmassLayananId` when `reg.Layanan.LayananId` is empty.
  - Wrapped each EMR label invocation in a try-catch block to guarantee that EMR label failures are swallowed and isolated, preserving the SMASS task's Succeeded status and keeping the registration flow unharmed.
  - Added a backward-compatible overload for callers awaiting wiring in slice P3-S05.
  - Added unit test suite `IgdVisitSmassLinkHookTest` in `Bilreg.Test` covering single/multiple assessment labeling, fallback LayananId, error swallow/isolation, link failure guard, null/empty assessment IDs guard, disabled integration toggle, and backward-compatible overload.
- Changed Files:
  - `src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitSmassLinkHook.cs`
  - `src/bilreg/Bilreg.Test/IgdContext/IgdVisitFeature/UseCases/IgdVisitSmassLinkHookTest.cs`

---

### P3-S05

Title: Wire Gateway & User Context into Assign & Replace Register Handlers

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Menyuntikkan dependensi `IEmrLabelGateway` ke dalam `IgdVisitAssignRegisterHandler` dan `IgdVisitReplaceRegisterHandler`, serta meneruskan instance gateway dan `request.UserId` ke pemanggilan `IgdVisitSmassLinkHook.RunAsync`.

Depends On: P3-S04

Repository: `b09-bilreg-api`

Completion Criteria:
1. `IgdVisitAssignRegisterHandler` menerima `IEmrLabelGateway` melalui constructor injection dan meneruskannya beserta `request.UserId` ke `IgdVisitSmassLinkHook.RunAsync`.
2. `IgdVisitReplaceRegisterHandler` menerima `IEmrLabelGateway` melalui constructor injection dan meneruskannya beserta `request.UserId` ke `IgdVisitSmassLinkHook.RunAsync`.
3. Unit test yang sudah ada untuk kedua handler tersebut diperbarui agar lulus dengan dependensi baru mock `IEmrLabelGateway`.

Notes:
- Lokasi file modifikasi: `src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitAssignRegisterCmd.cs`
- Lokasi file modifikasi: `src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitReplaceRegisterCmd.cs`
- Implementation Notes:
  - Injected `IEmrLabelGateway` into `IgdVisitAssignRegisterHandler` constructor and forwarded `_emrLabelGateway` and `request.UserId` to `IgdVisitSmassLinkHook.RunAsync`.
  - Injected `IEmrLabelGateway` into `IgdVisitReplaceRegisterHandler` constructor and forwarded `_emrLabelGateway` and `request.UserId` to `IgdVisitSmassLinkHook.RunAsync`.
  - Created comprehensive unit test suites `IgdVisitAssignRegisterHandlerTest` and `IgdVisitReplaceRegisterHandlerTest` in `Bilreg.Test` covering valid registration assignment/replacement with EMR label gateway invocation, user context forwarding, failure isolation (non-blocking exception handling), integration toggle bypass, and domain/repository validation rules.
- Changed Files:
  - `src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitAssignRegisterCmd.cs`
  - `src/bilreg/Bilreg.Application/IgdContext/IgdVisitFeature/UseCases/IgdVisitReplaceRegisterCmd.cs`
  - `src/bilreg/Bilreg.Test/IgdContext/IgdVisitFeature/UseCases/IgdVisitAssignRegisterHandlerTest.cs`
  - `src/bilreg/Bilreg.Test/IgdContext/IgdVisitFeature/UseCases/IgdVisitReplaceRegisterHandlerTest.cs`

---

## P4 - Manual Retry & Worklist Integration

Implementation Status: IMPLEMENTED  
Review Status: GO

### P4-S06

Title: Manual Retry Orchestration in IgdVisitSmassTaskRetryExecutor

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui `IgdVisitSmassTaskRetryExecutor.ExecuteAsync` agar ketika operator melakukan retry manual pada task berjenis `Link` dan pemanggilan `LinkIgdVisit` menghasilkan status sukses, executor secara otomatis memicu pendaftaran label EMR 2.0 untuk seluruh asesmen pada `LinkedAssessmentIds`, serta memperbarui pemanggilnya pada `IgdVisitSmassTaskRetryCmd` dan `IgdVisitSmassTaskProcessCmd`.

Depends On: P2-S03, P3-S04

Repository: `b09-bilreg-api`

Completion Criteria:
1. `IgdVisitSmassTaskRetryExecutor.ExecuteAsync` menerima parameter `IEmrLabelGateway emrLabelGateway` dan `string userId` (dengan fallback ke `reg.Audit.UserId` jika userId kosong).
2. Jika `task.TaskType == SmassTaskTypeEnum.Link` dan hasil link berhasil dengan `LinkedAssessmentIds` yang tidak kosong, executor memanggil `emrLabelGateway.AddSmassLabel` untuk setiap asesmen.
3. `IgdVisitSmassTaskRetryHandler` dan `IgdVisitSmassTaskProcessHandler` menyuntikkan `IEmrLabelGateway` dan meneruskannya ke `IgdVisitSmassTaskRetryExecutor.ExecuteAsync`.
4. Unit test memverifikasi bahwa retry manual task link yang sukses memicu pembuatan label EMR untuk setiap asesmen yang ter-link.

Notes:
- Lokasi file modifikasi: `src/bilreg/Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskRetryExecutor.cs`
- Lokasi file modifikasi: `src/bilreg/Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskRetryCmd.cs`
- Lokasi file modifikasi: `src/bilreg/Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskProcessCmd.cs`
- Implementation Notes:
  - Updated `IgdVisitSmassTaskRetryExecutor.ExecuteAsync` to accept `IEmrLabelGateway emrLabelGateway` and `string userId` (falling back to `reg.RegMasukAudit.UserId` if userId is empty, and falling back to `options.SmassLayananId` if `reg.Layanan.LayananId` is empty).
  - Implemented post-retry sequential iteration over `result.LinkedAssessmentIds` invoking `emrLabelGateway.AddSmassLabel` with `EmrAddSmassLabelRequest` when `task.TaskType == SmassTaskTypeEnum.Link` succeeds.
  - Wrapped each EMR label invocation in a try-catch block to guarantee that EMR label failures are swallowed and isolated (TD-05), keeping the SMASS retry outcome and task status intact.
  - Retained backward-compatible overload for `ExecuteAsync`.
  - Injected `IEmrLabelGateway` into `IgdVisitSmassTaskRetryHandler` and forwarded `request.UserId` to `ExecuteAsync`.
  - Injected `IEmrLabelGateway` into `IgdVisitSmassTaskProcessHandler` and forwarded `request.UserId` to `ExecuteAsync`.
  - Created unit tests in `IgdVisitSmassTaskRetryExecutorTest.cs`, `IgdVisitSmassTaskRetryCmdTest.cs`, and `IgdVisitSmassTaskProcessCmdTest.cs` covering EMR label generation, fallback user ID, failure isolation, empty assessment IDs guard, task failure guard, batch processing, and backward compatibility.
- Changed Files:
  - `src/bilreg/Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskRetryExecutor.cs`
  - `src/bilreg/Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskRetryCmd.cs`
  - `src/bilreg/Bilreg.Application/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskProcessCmd.cs`
  - `src/bilreg/Bilreg.Test/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskRetryExecutorTest.cs`
  - `src/bilreg/Bilreg.Test/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskRetryCmdTest.cs`
  - `src/bilreg/Bilreg.Test/IgdContext/IgdVisitSmassTaskFeature/UseCases/IgdVisitSmassTaskProcessCmdTest.cs`

---

# 6. Change Log

- 2026-09-28: Initial release (V1.0) of Implementation Plan based on approved architecture `IGD-EMR-TIMELINE-ARCHITECTURE.md` (V1.0). Status initialized to `NOT-STARTED`, `Execution Approval: APPROVED`.
