---
Title: IGD Triage EMR Timeline Integration Architecture
Code: IGD-EMR-TIMELINE
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-09-28
---

# 1. Overview

## Realized Bug Correction

Arsitektur ini merealisasikan perbaikan bug **ISSUE-IGD-EMR-TIMELINE-001**: data asesmen IGD Triage yang berhasil disimpan dan dihubungkan ke registrasi (`RegId`) sudah tampil pada menu Catalog EMR, namun belum muncul pada menu Timeline EMR (`GET /api/Label/TimeLine`) karena entitas label (`EMREC_label`) dengan tipe label `12` belum digenerate ke EMR 2.0.

Arsitektur ini mendefinisikan realisasi teknis perbaikan pada sisi **BILREG API** (`b09-bilreg-api`):
1. Perluasan kontrak hasil link gateway SMASS untuk mengekspos daftar `LinkedAssessmentIds` (`ListAssesmentId`) yang dikembalikan oleh SMASS `linkIgdVisit`.
2. Penambahan outbound port gateway `IEmrLabelGateway` dan adapter RestSharp `EmrLabelGateway` untuk memanggil endpoint `{Emr20Api} POST /api/LabelV2/AddSmass`.
3. Penambahan konfigurasi opsi `Emr20Options` (`BaseApiUrl`) dan penambahan opsi `SmassTriagePaperName` pada `IgdVisitOptions`.
4. Orkestrasi pemanggilan pendaftaran label ke EMR 2.0 secara berurutan pasca-sukses link SMASS di dalam hook `IgdVisitSmassLinkHook` dan executor retry `IgdVisitSmassTaskRetryExecutor`.
5. Penerapan prinsip isolasi kegagalan (*fault isolation*) agar kegagalan pembuatan label ke EMR tidak memengaruhi komitmen transaksi registrasi pasien di BILREG.

## Referenced Artifacts

- Originating BUG ISSUE: `b09-bilreg-api/docs/contexts/igd/IGD-EMR-TIMELINE-ISSUE.md`
- Analysis Input: `b09-bilreg-api/docs/contexts/igd/IGD-EMR-TIMELINE-BUG-INVESTIGATION.md`
- Prior Architecture Context: `b09-bilreg-api/docs/contexts/igd/igd-triage-abc-smass-architecture.md` (§9.7 Risk R-03 & AR-14)
- Domain Context: `b09-bilreg-api/docs/contexts/igd/igd-02-domain.md`

# 2. Architectural Basis

## Business Context

- **Issue**: `ISSUE-IGD-EMR-TIMELINE-001`
- **Tujuan Operasional**: Ketika kunjungan IGD (`IgdVisit`) berhasil dihubungkan (*link*) dengan pendaftaran (`RegId`), data asesmen klinis hasil triage IGD harus otomatis muncul dalam kronologi riwayat pasien di Timeline EMR (`GET /api/Label/TimeLine`), melengkapi ketersediaan data yang saat ini sudah ada di Catalog EMR.

## Analysis Input

Mengonsumsi keputusan-keputusan yang telah disetujui pada `IGD-EMR-TIMELINE-BUG-INVESTIGATION.md`:
- **Q-01 (Boundary Ownership)**: BILREG bertindak sebagai orkestrator yang memanggil `{Emr20Api} POST /api/LabelV2/AddSmass` secara berurutan segera setelah pemanggilan `linkIgdVisit` ke SMASS berhasil.
- **Q-02 (Parameter Metadata PaperName)**: Nilai `PaperName` diambil dari konfigurasi opsional baru `IgdVisitOptions:SmassTriagePaperName` pada servis BILREG.
- **Q-03 (Handling Multi-Assessment)**: Seluruh `AssesmentId` yang terdaftar pada `ListAssesmentId` dalam respons sukses SMASS `linkIgdVisit` akan diproses untuk dibuatkan label ke EMR 2.0.
- **Q-04 (Resilience & Task Logging)**: Eksekusi pembuatan label EMR bersifat langsung (*best-effort inline post-link*) tanpa penambahan tabel atau antrean task terpisah pada database BILREG.

# 3. Scope

## Included

1. **SMASS Gateway Result Extension**:
   - Pembaruan `SmassGatewayResult` di `Bilreg.Application.IgdContext.Integration` agar memuat koleksi `IReadOnlyList<string> LinkedAssessmentIds`.
   - Pembaruan deserialisasi `LinkResponse` pada `SmassAssessmentGateway.LinkIgdVisit` di `Bilreg.Infrastructure.IgdContext.Integration` untuk memetakan `ListAssesmentId` ke `LinkedAssessmentIds`.
2. **EMR 2.0 Outbound Integration Port & Adapter**:
   - Abstraksi `IEmrLabelGateway`, DTO request `EmrAddSmassLabelRequest`, dan DTO response `EmrLabelGatewayResult` di `Bilreg.Application.IgdContext.Integration`.
   - Implementasi adapter `EmrLabelGateway` di `Bilreg.Infrastructure.IgdContext.Integration` menggunakan `IRestClientFactory` menuju endpoint `{Emr20Api} POST /api/LabelV2/AddSmass`.
3. **Configuration & Dependency Injection**:
   - Pembuatan kelas opsi `Emr20Options` di `Bilreg.Infrastructure.Shared.Helpers` untuk section `"Emr20"` (`BaseApiUrl`).
   - Penambahan properti `SmassTriagePaperName` pada `IgdVisitOptions` di `Bilreg.Application.IgdContext`.
   - Pembaruan `appsettings.json` di `Bilreg.Api` dengan konfigurasi default `"Emr20"` dan `"SmassTriagePaperName"`.
   - Pendaftaran `IEmrLabelGateway` dan binding `Emr20Options` pada `InfrastructureService.cs`.
4. **Post-Link Hook & Retry Orchestration**:
   - Pembaruan `IgdVisitSmassLinkHook.RunAsync` untuk menerima dependensi `IEmrLabelGateway` dan nilai `userId`.
   - Eksekusi pembuatan label ke EMR untuk setiap `assessmentId` yang ada pada `LinkedAssessmentIds` saat SMASS link berhasil.
   - Pembaruan pemanggil `IgdVisitSmassLinkHook` pada `IgdVisitAssignRegisterCmd` dan `IgdVisitReplaceRegisterCmd` untuk meneruskan `request.UserId` dan `IEmrLabelGateway`.
   - Pembaruan `IgdVisitSmassTaskRetryExecutor` untuk turut mengeksekusi pembuatan label EMR saat task `Link` berhasil di-retry secara manual.
5. **Fault Isolation**:
   - Penanganan error fail-safe pada pembuatan label EMR agar kegagalan HTTP/koneksi ke EMR 2.0 dicatat sebagai log warning dan tidak pernah melempar exception ke alur utama registrasi.

## Excluded

1. Perubahan internal pada servis SMASS API (`SMASSAPI`): SMASS sudah menyediakan endpoint `PATCH /api/Assesment/linkIgdVisit` beserta respons `ListAssesmentId`.
2. Perubahan internal pada servis EMR 2.0 API (`emr20-api`): EMR 2.0 sudah menyediakan endpoint `POST /api/LabelV2/AddSmass` dan `GET /api/Label/TimeLine`.
3. Perubahan frontend aplikasi web (`c012_myhospital_web`).
4. Perubahan skema database DDL di database BILREG (`HOSPITAL_PKL`): tabel `BILRG_IgdVisitSmassTask` tetap fokus melacak task SMASS, tanpa penambahan tabel label terpisah.
5. Mekanisme rekonsiliasi batch otomatis untuk data historis lama (data lama dapat direkonsiliasi melalui retry manual task operasional jika diperlukan).

# 4. Technical Decisions

## TD-01: Kontrak Outbound Gateway EMR 2.0 (`IEmrLabelGateway`)

Realisasi teknis pemanggilan EMR 2.0 ditempatkan pada lapisan Application sebagai port (`IEmrLabelGateway`) dan Infrastructure sebagai adapter (`EmrLabelGateway`).

```csharp
namespace Bilreg.Application.IgdContext.Integration;

public record EmrAddSmassLabelRequest
{
    public string AssesmentId { get; init; } = string.Empty;
    public string LayananId { get; init; } = string.Empty;
    public string PaperId { get; init; } = string.Empty;
    public string PaperName { get; init; } = string.Empty;
    public string RegId { get; init; } = string.Empty;
    public string UserrId { get; init; } = string.Empty;
}

public record EmrLabelGatewayResult(bool Success, string? ErrorMessage);

public interface IEmrLabelGateway
{
    Task<EmrLabelGatewayResult> AddSmassLabel(
        EmrAddSmassLabelRequest request,
        CancellationToken cancellationToken = default);
}
```

*Catatan*: Nama properti `UserrId` dengan dua huruf 'r' dipertahankan secara eksplisit agar sesuai dengan kontrak JSON serializer endpoint `{Emr20Api} POST /api/LabelV2/AddSmass`.

## TD-02: Perluasan `SmassGatewayResult` untuk Menampung `LinkedAssessmentIds`

Tipe record `SmassGatewayResult` diperluas tanpa merusak kompatibilitas penggunaan sebelumnya:

```csharp
namespace Bilreg.Application.IgdContext.Integration;

public record SmassGatewayResult(
    bool Success,
    string? AssessmentId,
    string? ErrorMessage,
    IReadOnlyList<string>? LinkedAssessmentIds = null);
```

Pada `SmassAssessmentGateway.LinkIgdVisit`:
- Respons JSON dari SMASS (`LinkResponse`) sudah memiliki properti `public string[] ListAssesmentId { get; set; }`.
- Hasil sukses dikembalikan sebagai:
  ```csharp
  return new SmassGatewayResult(
      true,
      null,
      null,
      envelope.Data.ListAssesmentId ?? Array.Empty<string>());
  ```

## TD-03: Strategi Konfigurasi Lingkungan (`Emr20Options` & `IgdVisitOptions`)

1. **Konfigurasi URL EMR 2.0**:
   Dibuat kelas opsi terpisah `Emr20Options` untuk menghindari konflik dengan `EmrOptions` (`Emr25Api`):
   ```csharp
   namespace Bilreg.Infrastructure.Shared.Helpers;

   public class Emr20Options
   {
       public const string SECTION_NAME = "Emr20";
       public string BaseApiUrl { get; set; } = string.Empty;
   }
   ```
   Pada `appsettings.json`:
   ```json
   "Emr20": {
     "BaseApiUrl": "http://dev.smart-ics.com:8083/emr20-api"
   }
   ```

2. **Konfigurasi PaperName Triage IGD**:
   Ditambahkan pada `IgdVisitOptions`:
   ```csharp
   public string SmassTriagePaperName { get; set; } = string.Empty;
   ```
   Pada `appsettings.json`:
   ```json
   "IgdVisit": {
     "EnableSmassIntegration": true,
     "SmassTriagePaperId": "PP-ICS-TRGE",
     "SmassTriagePaperName": "FORMULIR TRIASE IGD",
     "SmassLayananId": "1GD01"
   }
   ```

## TD-04: Alur Orkestrasi Pasca-Link pada `IgdVisitSmassLinkHook`

Orkestrasi dijalankan secara berurutan (*sequential*) dan terlindung dari exception:

```text
[Transaksi Registrasi Committed]
            ↓
[IgdVisitSmassLinkHook.RunAsync]
            ↓
1. Panggil SMASS Link (ISmassAssessmentGateway.LinkIgdVisit)
            ↓
   Apakah Link Sukses & LinkedAssessmentIds memiliki data?
      ├─ TIDAK  ──> Selesai (catat status task SMASS seperti eksisting)
      └─ YA     ──> Lanjutkan ke Pembuatan Label EMR
                     ↓
2. Loop setiap AssessmentId di LinkedAssessmentIds:
      ├─ Bentuk EmrAddSmassLabelRequest:
      │    AssesmentId = item
      │    LayananId   = reg.Layanan.LayananId (fallback ke options.SmassLayananId)
      │    PaperId     = options.SmassTriagePaperId
      │    PaperName   = options.SmassTriagePaperName
      │    RegId       = reg.RegId
      │    UserrId     = userId
      │
      └─ Eksekusi IEmrLabelGateway.AddSmassLabel
         (Try-Catch / Log Warning jika gagal; jangan throw)
```

## TD-05: Isolasi Kegagalan & Ketahanan (*Fault-Isolation & Resilience*)

Sesuai resolusi Q-04:
1. Kegagalan pembuatan label ke EMR (misal timeout, endpoint down, atau response error) **tidak boleh** membatalkan atau melempar exception ke alur transaksi registrasi (`IgdVisitAssignRegisterCmd` / `IgdVisitReplaceRegisterCmd`).
2. Kegagalan label EMR **tidak mengubah** status task SMASS (`BILRG_IgdVisitSmassTask`). Status `Succeeded` pada task SMASS merefleksikan keberhasilan linking di SMASS.
3. Seluruh kesalahan integrasi EMR dicatat via logger sebagai *Warning* berstruktur agar dapat dipantau oleh tim operasional.

## TD-06: Manual Retry Support pada `IgdVisitSmassTaskRetryExecutor`

Ketika operator menjalankan retry manual pada task `Link` yang berstatus `Failed` via `IgdVisitSmassTaskRetryCmd` atau batch `IgdVisitSmassTaskProcessCmd`:
- Jika pemanggilan `LinkIgdVisit` pada retry tersebut menghasilkan `Success == true`, maka executor juga memicu loop pembuatan label EMR dengan pola yang sama seperti di `IgdVisitSmassLinkHook`.

# 5. Component Responsibilities

| Komponen | Namespace / File | Tanggung Jawab |
|---|---|---|
| `ISmassAssessmentGateway` | `Bilreg.Application.IgdContext.Integration` | Interface port outbound menuju SMASS; mengembalikan `SmassGatewayResult` termasuk `LinkedAssessmentIds`. |
| `SmassAssessmentGateway` | `Bilreg.Infrastructure.IgdContext.Integration` | Adapter implementasi RestSharp menuju SMASS API; memetakan respons JSON `LinkResponse.ListAssesmentId` ke `SmassGatewayResult.LinkedAssessmentIds`. |
| `IEmrLabelGateway` | `Bilreg.Application.IgdContext.Integration` | Interface port outbound untuk pendaftaran entitas label ke EMR 2.0 API. |
| `EmrLabelGateway` | `Bilreg.Infrastructure.IgdContext.Integration` | Adapter implementasi RestSharp menuju `{Emr20Api} POST /api/LabelV2/AddSmass`. Memvalidasi konfigurasi fail-closed dan menangani kegagalan HTTP. |
| `Emr20Options` | `Bilreg.Infrastructure.Shared.Helpers` | POCO konfigurasi untuk membaca section `"Emr20"` (`BaseApiUrl`). |
| `IgdVisitOptions` | `Bilreg.Application.IgdContext` | POCO konfigurasi IGD, diperluas dengan properti `SmassTriagePaperName`. |
| `IgdVisitSmassLinkHook` | `Bilreg.Application.IgdContext.IgdVisitFeature.UseCases` | Logika orkestrasi pasca-commit registrasi: memanggil SMASS link, lalu memanggil `IEmrLabelGateway.AddSmassLabel` untuk setiap asesmen yang berhasil di-link. |
| `IgdVisitAssignRegisterHandler` | `Bilreg.Application.IgdContext.IgdVisitFeature.UseCases` | Handler MediatR penugasan registrasi awal; menyuntikkan dependensi gateway EMR dan meneruskan `UserId` ke hook. |
| `IgdVisitReplaceRegisterHandler` | `Bilreg.Application.IgdContext.IgdVisitFeature.UseCases` | Handler MediatR penggantian registrasi; menyuntikkan dependensi gateway EMR dan meneruskan `UserId` ke hook. |
| `IgdVisitSmassTaskRetryExecutor` | `Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases` | Executor retry manual; mengeksekusi ulang link SMASS dan memicu pendaftaran label EMR jika link berhasil. |
| `InfrastructureService` | `Bilreg.Api.Configurations` | Pendaftaran IoC service (`AddScoped<IEmrLabelGateway, EmrLabelGateway>`) dan binding konfigurasi (`Configure<Emr20Options>`). |

# 6. Integration Design

| Source | Target | Endpoint / Kontrak | Tujuan |
|---|---|---|---|
| BILREG (`SmassAssessmentGateway`) | SMASS API (`SmassOptions.BaseApiUrl`) | `PATCH /api/Assesment/linkIgdVisit` | Menghubungkan metadata registrasi pasien ke asesmen triage di SMASS dan memperoleh `ListAssesmentId`. |
| BILREG (`EmrLabelGateway`) | EMR 2.0 API (`Emr20Options.BaseApiUrl`) | `POST /api/LabelV2/AddSmass` | Mendaftarkan record label asesmen (`LabelTypeID = "12"`) ke tabel `EMREC_label` EMR 2.0. |
| Web EMR Client / Timeline Query | EMR 2.0 API | `GET /api/Label/TimeLine` | Mengambil kronologi riwayat medis pasien berbasis `EMREC_label` (otomatis memuat asesmen triage setelah label terbentuk). |

## Detail Kontrak Request Pembuatan Label EMR

```text
Endpoint : {Emr20Options.BaseApiUrl}/api/LabelV2/AddSmass
Method   : POST
Header   : Content-Type: application/json
Payload  :
{
  "AssesmentId": "<AssesmentId dari SMASS LinkResponse>",
  "LayananId": "<LayananId registrasi, misal 1GD01>",
  "PaperId": "<IgdVisitOptions.SmassTriagePaperId, misal PP-ICS-TRGE>",
  "PaperName": "<IgdVisitOptions.SmassTriagePaperName, misal FORMULIR TRIASE IGD>",
  "RegId": "<Nomor Registrasi Pasien>",
  "UserrId": "<UserId petugas yang melakukan registrasi>"
}
```

# 7. Data Ownership

| Entitas Data | Pemilik Otoritatif (Owner) | Kebijakan Akses / Perubahan |
|---|---|---|
| `EMREC_label` | EMR 2.0 API (`LabelContext`) | Dimiliki dan dikelola secara eksklusif oleh EMR 2.0. BILREG hanya meminta penambahan melalui `POST /api/LabelV2/AddSmass`. |
| `SMASS_Assesment` | SMASS API (`AssesmentContext`) | Dimiliki secara eksklusif oleh SMASS. BILREG hanya meminta pembaruan linking via `PATCH /api/Assesment/linkIgdVisit`. |
| `BILRG_IgdVisit` & Triage | BILREG API (`IgdContext`) | Dimiliki secara eksklusif oleh BILREG sebagai rekam transaksi primer kunjungan gawat darurat. |
| `BILRG_IgdVisitSmassTask` | BILREG API (`IgdContext`) | Dimiliki secara eksklusif oleh BILREG untuk melacak status operasional integrasi SMASS. |

# 8. Database Design

## New Tables

Tidak ada (*None*).

## Modified Tables

Tidak ada (*None*).

## Relationships

Tidak ada relasi tingkat database baru yang diperkenalkan di database BILREG. Integrasi antar-sistem dilakukan sepenuhnya via REST API.

## Migration Considerations

1. Tidak ada migrasi skema DDL yang perlu dijalankan pada database BILREG.
2. Tidak ada breaking change terhadap data yang tersimpan di `BILRG_IgdVisitSmassTask`.
3. Pasien baru yang diregistrasi pasca deployment otomatis akan memiliki record di `EMREC_label` dan langsung tampil di Timeline EMR.
4. Untuk data kunjungan IGD masa lampau yang asesmennya belum masuk Timeline, tim operasional dapat memicu perbaikan melalui mekanisme retry task link pada worklist integrasi IGD.

# 9. Cross-Cutting Concerns

## 1. Observability & Logging
- Setiap kegagalan pemanggilan ke endpoint `{Emr20Api} POST /api/LabelV2/AddSmass` dicatat menggunakan logging terstruktur (`Serilog`):
  `Log.Warning("Gagal membuat label EMR untuk AssesmentId {AssesmentId}, RegId {RegId}: {Error}", assessmentId, regId, errorMessage)`.
- Tidak mencatat payload yang memuat data kredensial rahasia.

## 2. Timeout & Connection Management
- `EmrLabelGateway` menggunakan instance `RestClient` yang dikelola melalui `IRestClientFactory`.
- Timeout HTTP request default ditetapkan 10 detik (dapat dibatasi maksimum 30 detik) agar tidak menahan thread eksekusi.

## 3. Idempotency & Fault Isolation
- Endpoint `{Emr20Api} POST /api/LabelV2/AddSmass` di EMR 2.0 menangani pembuatan label. Jika endpoint mengembalikan status non-sukses atau terjadi transient network error, error tersebut diserap (*swallowed*) setelah dicatat di log.
- Transaksi `IgdVisit` dan `Reg` yang telah di-commit ke database BILREG tidak akan di-rollback.

# 10. Implementation Constraints

1. **Clean Architecture Boundaries**:
   - Lapisan Domain dan Application tidak boleh mereferensikan RestSharp, System.Net.Http, atau detail konfigurasi infrastruktur.
   - Kontrak gateway didefinisikan di `Bilreg.Application.IgdContext.Integration`.
   - Implementasi adapter RestSharp ditempatkan di `Bilreg.Infrastructure.IgdContext.Integration`.
2. **REST Client Pattern**:
   - Wajib menggunakan `IRestClientFactory` yang sudah terdaftar di kontainer IoC BILREG.
3. **No Direct DB Query Across Boundaries**:
   - Dilarang keras melakukan query SQL langsung dari BILREG ke tabel `EMREC_label` atau database EMR 2.0. Seluruh interaksi wajib melalui HTTP endpoint resmi EMR 2.0.
4. **Preserve Existing SMASS Integration Semantics**:
   - Pemanggilan ke SMASS `linkIgdVisit` tetap mempertahankan semantik eksisting, termasuk penanganan task status `BILRG_IgdVisitSmassTask`.

# 11. Acceptance Conditions

Implementation dianggap memenuhi standar arsitektur jika seluruh kondisi berikut terpenuhi:

1. **AC-01 (Contract Integrity)**:
   - `SmassGatewayResult` mengembalikan properti `LinkedAssessmentIds`.
   - `SmassAssessmentGateway.LinkIgdVisit` berhasil memetakan `ListAssesmentId` dari respons SMASS ke dalam `LinkedAssessmentIds`.
2. **AC-02 (EMR Gateway Implementation)**:
   - `IEmrLabelGateway` terdefinisi dan terimplementasi via `EmrLabelGateway`.
   - Adapter memvalidasi ketersediaan `Emr20Options.BaseApiUrl` (fail-closed jika kosong).
   - Adapter mengirim payload JSON dengan format yang tepat termasuk field `UserrId`.
3. **AC-03 (Configuration Wiring)**:
   - `Emr20Options` terdaftar di DI dan membaca section `"Emr20"` dari `appsettings.json`.
   - `IgdVisitOptions` memiliki properti `SmassTriagePaperName` yang terisi dari section `"IgdVisit"`.
   - `IEmrLabelGateway` terdaftar sebagai scoped service di `InfrastructureService.cs`.
4. **AC-04 (Hook & Retry Orchestration)**:
   - Saat registrasi IGD (`IgdVisitAssignRegisterCmd` / `IgdVisitReplaceRegisterCmd`) berhasil di-commit dan SMASS link mengembalikan daftar asesmen sukses, `AddSmassLabel` terpanggil untuk setiap asesmen tersebut.
   - Saat manual retry task link dieksekusi melalui `IgdVisitSmassTaskRetryExecutor` dan menghasilkan sukses, `AddSmassLabel` juga terpanggil untuk setiap asesmen yang berhasil di-link.
5. **AC-05 (Resilience Verification)**:
   - Jika endpoint `{Emr20Api} POST /api/LabelV2/AddSmass` mengembalikan HTTP 500 atau timeout, registrasi pasien di BILREG tetap berhasil tanpa error yang bocor ke response API BILREG.
   - Status task SMASS pada `BILRG_IgdVisitSmassTask` tetap tercatat `Succeeded` jika SMASS link sukses, terlepas dari hasil panggilan ke EMR.
6. **AC-06 (End-to-End Verification)**:
   - Data asesmen IGD Triage pasien yang telah di-link registrasi dapat ditemukan pada response `{Emr20Api} GET /api/Label/TimeLine` dengan `ReffID = AssesmentId` dan `LabelTypeID = "12"`.
