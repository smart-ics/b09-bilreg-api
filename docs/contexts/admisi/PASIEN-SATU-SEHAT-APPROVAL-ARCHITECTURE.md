---
Title: Architecture - Integrasi Data Status Satu Sehat Pasien dan Konfirmasi Persetujuan Upload pada Registrasi Admisi
Code: PASIEN-SATU-SEHAT-APPROVAL
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-10-07
---

# 1. Overview

Arsitektur ini mendefinisikan realisasi teknis untuk integrasi data status Satu Sehat pasien (`tc_mr_saset`) ke dalam agregat data sosial pasien di sistem Bilreg (`b09-bilreg-api`), penyediaan endpoint konfirmasi persetujuan (*consent*) upload Satu Sehat, serta visualisasi indikator status Satu Sehat dan otomatisasi alur konfirmasi persetujuan upload berbasis grup jaminan pasien (BPJS vs Non-BPJS) pada antarmuka registrasi Admisi (`c012_myhospital_web`).

Referenced Issue:
- [PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-ISSUE.md) (`ISSUE-ADMISI-PASIEN-SASET-001`)

---

# 2. Architectural Basis

## Business Context

- DOMAIN Rajal: [admisi-rajal-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-rajal/admisi-rajal-domain.md)

Dalam konteks loket admisi rumah sakit, pencatatan persetujuan (*consent*) pengiriman resume medis ke platform nasional Satu Sehat Kemenkes merupakan prasyarat interoperabilitas data kesehatan. Pasien jaminan BPJS Kesehatan memiliki kewajiban regulasi untuk pengiriman data kesehatan sehingga persetujuan upload dilakukan otomatis, sedangkan pasien umum dan asuransi non-BPJS berhak atas persetujuan sadar (*informed consent*) yang dikonfirmasi oleh petugas loket sebelum pendaftaran disimpan. Seluruh alur konfirmasi persetujuan harus bersifat *fail-safe* dan *non-blocking* agar tidak menghambat kecepatan antrean pendaftaran pasien.

## Analysis Input

- FEASIBILITY-ASSESSMENT: [PASIEN-SATU-SEHAT-APPROVAL-FEASIBILITY-ASSESSMENT.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-FEASIBILITY-ASSESSMENT.md)

Keputusan analisis kelayakan (*Gap Closure*) yang direalisasikan dalam arsitektur ini:
- **GAP-001 & OQ-001:** Pembuatan entitas domain `PasienSasetModel`, komponen DAL `PasienSasetDal`, dan DTO `PasienSasetDto` untuk tabel `tc_mr_saset` dengan pola *Upsert* (auto-create record jika belum ada saat approval).
- **GAP-002:** Integrasi pemuatan data Satu Sehat pada `LoadEntity` dan penyimpanan data Satu Sehat pada `SaveChanges` di `PasienRepo`.
- **GAP-003:** Perluasan kontrak DTO respon query pasien `PasienGetResponse` untuk memuat atribut data `PasienSaset`.
- **GAP-004, OQ-002 & OQ-005:** Pembuatan usecase command `PasienApproveUploadSasetCmd(string PasienId)` dengan rute HTTP PATCH `/api/pasien/approveUploadSaset` yang mengembalikan `JSendOk("Done")`.
- **GAP-005:** Perluasan skema validasi Zod `dataSosialPasienSchemaNew` pada frontend `pasien.ts` dan penyediaan hook `useApproveUploadSaset` pada `PasienService.ts`.
- **GAP-006:** Visualisasi label `#SatuSehat` pada kartu ringkasan pasien di `LegacyRegistrationWorkspace.vue` (hijau jika disetujui, abu-abu jika belum disetujui).
- **GAP-007, GAP-008, OQ-003 & OQ-004:** Implementasi interseptor pra-simpan di `useRegistrasiActions.ts` dengan client-side caching hasil mapping JetliApi `/JetliAPi/api/GrupJaminan/map`, auto-approval tanpa dialog untuk pasien BPJS, dan pop-up dialog konfirmasi persetujuan upload untuk pasien Non-BPJS.
- **GAP-009:** Isolasi error dan proteksi timeout (*fail-safe non-blocking*) agar kegagalan jaringan pada JetliApi atau API approval tidak pernah menggagalkan simpan registrasi pasien.

```text
DOMAIN + FEATURE (Admisi Rajal & Pasien)
        +
FEASIBILITY-ASSESSMENT (PASIEN-SATU-SEHAT-APPROVAL-FEASIBILITY-ASSESSMENT)
        ↓
    ARCHITECTURE (PASIEN-SATU-SEHAT-APPROVAL-ARCHITECTURE)
```

---

# 3. Scope

## Included

1. **Backend - Domain Layer (`Bilreg.Domain`)**:
   - Pembuatan entitas domain `PasienSasetModel` dengan atribut: `PasienId`, `SasetId`, `IsApprovedUpload`, `TglJamApprovedUpload`, `FileGeneralConcentUpload`, `IsApprovedView`, `TglJamApprovedView`, `FileGeneralConcentView`.
   - Perluasan agregat `PasienModel` untuk memiliki referensi ke `PasienSasetModel` serta method mutasi `ApproveUploadSaset()`.
2. **Backend - Infrastructure Layer (`Bilreg.Infrastructure`)**:
   - Interface `IPasienSasetDal`, class DTO `PasienSasetDto`, dan implementasi `PasienSasetDal` berbasis Dapper untuk mengakses tabel `tc_mr_saset`.
   - Perluasan `PasienRepo` untuk menginjeksi `IPasienSasetDal`, memuat data Satu Sehat pada `LoadEntity`, dan mengeksekusi operasi *Upsert* (Insert jika belum ada, Update jika sudah ada) pada `SaveChanges`.
3. **Backend - Application Layer (`Bilreg.Application`)**:
   - Perluasan record `PasienGetResponse` pada `PasienGetQuery.cs` dengan menyertakan sub-record `PasienSasetResponse`.
   - Pembuatan command `PasienApproveUploadSasetCmd(string PasienId) : IRequest, IPasienKey` dan handler `PasienApproveUploadSasetHandler`.
4. **Backend - Presentation API Layer (`Bilreg.Api`)**:
   - Endpoint `[HttpPatch("approveUploadSaset")]` pada `PasienController` yang meneruskan command ke MediatR dan mengembalikan respons `JSendOk("Done")`.
   - Registrasi dependency injection untuk `IPasienSasetDal` pada `Bilreg.Infrastructure` / `Bilreg.Api`.
5. **Frontend - BillingBase Module (`c012_myhospital_web`)**:
   - Perluasan Zod schema `dataSosialPasienSchemaNew` dan tipe `DataSosialPasienNew` pada `pasien.ts` dengan properti `pasienSaset`.
   - Penambahan skema payload approval dan mutation hook `useApproveUploadSaset` pada `PasienService.ts`.
6. **Frontend - Admisi Module (`c012_myhospital_web`)**:
   - Penambahan visual badge `#SatuSehat` dengan status warna kondisional pada kartu ringkasan pasien di `LegacyRegistrationWorkspace.vue`.
   - Penyediaan komponen/composable dialog konfirmasi persetujuan upload Satu Sehat untuk pasien Non-BPJS.
   - Pengecekan pra-simpan registrasi pada `handleSubmitRegister` di `useRegistrasiActions.ts` (evaluasi flag `isApprovedUpload`, cache lookup JetliApi, auto-approval BPJS, dialog konfirmasi Non-BPJS, serta pembungkusan try-catch non-blocking).
7. **Pengujian Unit & Integrasi**:
   - Unit test domain `PasienModel` dan `PasienSasetModel`.
   - Unit test `PasienRepo` (skenario load & upsert `tc_mr_saset`).
   - Unit test use case `PasienApproveUploadSasetHandler` dan `PasienGetHandler`.
   - Unit test frontend untuk composable / interceptor pra-simpan registrasi.

## Excluded

1. Sinkronisasi transmisi payload FHIR/HL7 ke server Cloud Kemenkes Satu Sehat (dikelola oleh service background/integrator terpisah di luar Bilreg).
2. Modifikasi logika approval *View* (`IsApprovedView`) maupun upload berkas fisik surat consent (`FileGeneralConcentUpload` / `FileGeneralConcentView`) pada fase ini.
3. Modifikasi alur kasir, billing, farmasi, atau laboratorium.

---

# 4. Technical Decisions

### TD-01: Domain Aggregate Embedding & Behavior
Data Satu Sehat pasien dihubungkan langsung ke agregat root `PasienModel` sebagai entitas pendukung `PasienSasetModel`:
```csharp
namespace Bilreg.Domain.PasienContext.PasienFeature;

public class PasienSasetModel
{
    public string PasienId { get; private set; }
    public string SasetId { get; private set; }
    public bool IsApprovedUpload { get; private set; }
    public DateTime TglJamApprovedUpload { get; private set; }
    public string FileGeneralConcentUpload { get; private set; }
    public bool IsApprovedView { get; private set; }
    public DateTime TglJamApprovedView { get; private set; }
    public string FileGeneralConcentView { get; private set; }

    public PasienSasetModel(string pasienId, string sasetId, 
        bool isApprovedUpload, DateTime tglJamApprovedUpload, string fileGeneralConcentUpload, 
        bool isApprovedView, DateTime tglJamApprovedView, string fileGeneralConcentView)
    {
        PasienId = pasienId;
        SasetId = sasetId;
        IsApprovedUpload = isApprovedUpload;
        TglJamApprovedUpload = tglJamApprovedUpload;
        FileGeneralConcentUpload = fileGeneralConcentUpload;
        IsApprovedView = isApprovedView;
        TglJamApprovedView = tglJamApprovedView;
        FileGeneralConcentView = fileGeneralConcentView;
    }

    public static PasienSasetModel Default(string pasienId = "-") =>
        new PasienSasetModel(pasienId, "-", false, new DateTime(3000, 1, 1), "-", false, new DateTime(3000, 1, 1), "-");

    public void ApproveUpload(DateTime approvedAt)
    {
        IsApprovedUpload = true;
        TglJamApprovedUpload = approvedAt;
    }
}
```
Pada `PasienModel`, ditambahkan method:
```csharp
public PasienSasetModel PasienSaset { get; private set; }

public void SetSaset(PasienSasetModel saset)
{
    PasienSaset = saset ?? PasienSasetModel.Default(PasienId);
}

public void ApproveUploadSaset(DateTime approvedAt)
{
    PasienSaset ??= PasienSasetModel.Default(PasienId);
    PasienSaset.ApproveUpload(approvedAt);
}
```

### TD-02: Persistence Strategy via DAL & Upsert Pattern
Tabel operasional `tc_mr_saset` memiliki relasi 1-to-1 dengan `tc_mr` melalui `KodeMr`. Karena data pasien terdahulu mungkin belum memiliki record di `tc_mr_saset`, `PasienRepo` menerapkan mekanisme *Upsert*:
1. Di `LoadEntity`:
   - `_pasienSasetDal.GetData(key)` dipanggil. Jika mengembalikan null, repo menginisialisasi `PasienSasetModel.Default(key.PasienId)` sehingga objek agregat tidak bernilai null.
2. Di `SaveChanges`:
   - Repo mengecek `_pasienSasetDal.GetData(PasienModel.Key(model.PasienId))`.
   - Jika record sudah ada di `tc_mr_saset`, panggil `_pasienSasetDal.Update(PasienSasetDto.FromModel(model.PasienSaset))`.
   - Jika belum ada, panggil `_pasienSasetDal.Insert(PasienSasetDto.FromModel(model.PasienSaset))`.
   - Seluruh operasi dieksekusi di dalam lingkup transaksi `using var trans = TransHelper.NewScope()`.

### TD-03: Dapper SQL Mapping untuk `tc_mr_saset`
Komponen `PasienSasetDal` menggunakan Dapper dan mengeksekusi query SQL terstandardisasi:
```csharp
public interface IPasienSasetDal :
    IInsert<PasienSasetDto>,
    IUpdate<PasienSasetDto>,
    IDelete<IPasienKey>,
    IGetData<PasienSasetDto, IPasienKey>
{
}
```
Query SQL:
- **GetData**:
  ```sql
  SELECT 
      KodeMr, KodeSaset, 
      IsApprovedUpload, TglJamApprovedUpload, FileGeneralConcentUpload, 
      IsApprovedView, TglJamApprovedView, FileGeneralConcentView 
  FROM tc_mr_saset 
  WHERE KodeMr = @PasienId
  ```
- **Insert**:
  ```sql
  INSERT INTO tc_mr_saset(
      KodeMr, KodeSaset, 
      IsApprovedUpload, TglJamApprovedUpload, FileGeneralConcentUpload, 
      IsApprovedView, TglJamApprovedView, FileGeneralConcentView
  ) VALUES (
      @KodeMr, @KodeSaset, 
      @IsApprovedUpload, @TglJamApprovedUpload, @FileGeneralConcentUpload, 
      @IsApprovedView, @TglJamApprovedView, @FileGeneralConcentView
  )
  ```
- **Update**:
  ```sql
  UPDATE tc_mr_saset SET 
      KodeSaset = @KodeSaset, 
      IsApprovedUpload = @IsApprovedUpload, 
      TglJamApprovedUpload = @TglJamApprovedUpload, 
      FileGeneralConcentUpload = @FileGeneralConcentUpload, 
      IsApprovedView = @IsApprovedView, 
      TglJamApprovedView = @TglJamApprovedView, 
      FileGeneralConcentView = @FileGeneralConcentView 
  WHERE KodeMr = @KodeMr
  ```
- **Delete**:
  ```sql
  DELETE FROM tc_mr_saset WHERE KodeMr = @PasienId
  ```

### TD-04: API Contract Definition
1. **Query Pasien (`PasienGetQuery`)**:
   Kontrak respons diperluas dengan menambahkan atribut `PasienSasetResponse`:
   ```csharp
   public record PasienSasetResponse(
       string SasetId,
       bool IsApprovedUpload,
       string TglJamApprovedUpload,
       string FileGeneralConcentUpload,
       bool IsApprovedView,
       string TglJamApprovedView,
       string FileGeneralConcentView
   );
   ```
   Atribut ditambahkan ke akhir record `PasienGetResponse`. Nilai tanggal diformat ke string ISO `"yyyy-MM-dd HH:mm:ss"` atau `""` jika default.
2. **Command Persetujuan (`PasienApproveUploadSasetCmd`)**:
   ```csharp
   public record PasienApproveUploadSasetCmd(string PasienId) : IRequest, IPasienKey;
   ```
   Rute di `PasienController`:
   ```csharp
   [HttpPatch]
   [Route("approveUploadSaset")]
   public async Task<IActionResult> ApproveUploadSaset(PasienApproveUploadSasetCmd cmd)
   {
       await _mediator.Send(cmd);
       return Ok(new JSendOk("Done"));
   }
   ```
   Respons berupa `{ status: "success", data: "Done" }` konsisten dengan konvensi API Bilreg.

### TD-05: Frontend Client-Side Pre-Submit Interceptor Flow
Alur penanganan pada tombol "Simpan Registrasi" di `useRegistrasiActions.ts`:
1. Validasi awal form registrasi (eligibilitas, kelengkapan NIK).
2. Pengecekan status Satu Sehat pasien:
   ```typescript
   const isAlreadyApproved = Boolean(pasienDetail.value?.pasienSaset?.isApprovedUpload)
   ```
3. Jika `isAlreadyApproved === true`: langsung lanjutkan eksekusi mutasi registrasi.
4. Jika `isAlreadyApproved === false`:
   a. Lakukan lookup mapping grup jaminan ke JetliApi:
      - Cek in-memory cache map `grupJaminanCache.get(values.tipeJaminanId)`.
      - Jika belum ter-cache, lakukan HTTP GET via ApiService (`apiName: 'jetliApi'`) ke `GrupJaminan/map?tipeJaminanId=${values.tipeJaminanId}` dengan timeout pendek (maksimal 3 detik).
      - Jika JetliApi error/timeout: catat peringatan (fail-safe), lewati konfirmasi, dan langsung lanjutkan simpan registrasi.
   b. Evaluasi jenis grup jaminan:
      - **Jika terdeteksi BPJS** (respons JetliApi 200 dan data mapping ada):
        - Otomatis panggil `approveUploadSaset({ pasienId })` tanpa konfirmasi dialog.
        - Bungkus pemanggilan dalam blok `try-catch` non-blocking.
        - Perbarui local state `pasienDetail.value.pasienSaset.isApprovedUpload = true`.
        - Lanjutkan simpan registrasi.
      - **Jika Non-BPJS** (respons tidak ada data / status non-BPJS):
        - Tampilkan modal/dialog konfirmasi persetujuan Satu Sehat kepada petugas.
        - Jika petugas memilih **"Setuju / Ya"**:
          - Panggil `approveUploadSaset({ pasienId })` dalam blok `try-catch` non-blocking.
          - Perbarui local state `pasienDetail.value.pasienSaset.isApprovedUpload = true`.
          - Lanjutkan simpan registrasi.
        - Jika petugas memilih **"Batal / Tidak"**:
          - Lewati pemanggilan approval.
          - Lanjutkan simpan registrasi seperti biasa.

### TD-06: Resilience & Zero-Blockage Policy
1. Kegagalan atau timeout saat memanggil JetliApi `/JetliAPi/api/GrupJaminan/map` **tidak boleh menghentikan pendaftaran pasien**.
2. Kegagalan HTTP dari mutasi `approveUploadSaset` **tidak boleh membatalkan simpan registrasi**.
3. Jika approval gagal, tampilkan toast peringatan (`toast.warning('Persetujuan Satu Sehat gagal dicatat, pendaftaran tetap diproses')`) dan lanjutkan proses registrasi hingga selesai.

---

# 5. Component Responsibilities

| Component | Responsibility |
|---|---|
| `Bilreg.Domain.PasienContext.PasienFeature.PasienSasetModel` | Merepresentasikan model domain entitas data Satu Sehat pasien beserta perilaku bisnis persetujuan upload (`ApproveUpload`). |
| `Bilreg.Domain.PasienContext.PasienFeature.PasienModel` | Agregat root pasien yang mengelola siklus hidup data pasien secara utuh termasuk delegasi ke `PasienSasetModel`. |
| `Bilreg.Infrastructure.PasienContext.PasienFeature.IPasienSasetDal` & `PasienSasetDal` | Akses data CRUD tingkat rendah menggunakan Dapper untuk tabel `tc_mr_saset`. |
| `Bilreg.Infrastructure.PasienContext.PasienFeature.PasienSasetDto` | Data Transfer Object untuk pertukaran data baris tabel `tc_mr_saset` dengan `PasienSasetModel`. |
| `Bilreg.Infrastructure.PasienContext.PasienFeature.PasienRepo` | Mengorkestrasikan pemuatan agregat (`LoadEntity`) dan persistensi transaksi *Upsert* (`SaveChanges`) untuk seluruh tabel data pasien termasuk `tc_mr_saset`. |
| `Bilreg.Application.PasienContext.PasienFeature.PasienGetQuery` & `PasienGetHandler` | Mengambil data pasien lengkap dan menyertakan data status Satu Sehat dalam `PasienGetResponse`. |
| `Bilreg.Application.PasienContext.PasienFeature.PasienApproveUploadSasetCmd` & Handler | Use case command untuk menyetujui upload Satu Sehat pasien dan menyimpannya melalui repositori. |
| `Bilreg.Api.Controllers.PasienContext.PasienController` | Menyediakan endpoint HTTP GET query pasien dan HTTP PATCH `/api/pasien/approveUploadSaset`. |
| `c012_myhospital_web: pasien.ts` | Mendefinisikan Zod schema dan TypeScript interface untuk `pasienSaset` serta payload request approval. |
| `c012_myhospital_web: PasienService.ts` | Menyediakan mutation hook TanStack Query `useApproveUploadSaset` dan direct fetcher untuk pemanggilan API approval ke backend. |
| `c012_myhospital_web: LegacyRegistrationWorkspace.vue` | Menampilkan indikator visual status `#SatuSehat` (hijau jika disetujui, abu-abu jika belum) pada kartu ringkasan pasien di panel pendaftaran. |
| `c012_myhospital_web: useRegistrasiActions.ts` | Mengorkestrasikan interseptor pra-simpan registrasi: evaluasi status persetujuan, lookup cache JetliApi, auto-approval BPJS, pemunculan dialog konfirmasi non-BPJS, serta isolasi kegagalan non-blocking. |
| `c012_myhospital_web: DialogPersetujuanSatuSehat` | Komponen pop-up konfirmasi yang menanyakan persetujuan upload Satu Sehat untuk pasien Non-BPJS saat proses registrasi. |

---

# 6. Integration Design

| Source | Target | Purpose |
|---|---|---|
| `PasienRepo` | `IPasienSasetDal` | Membaca data `tc_mr_saset` saat `LoadEntity` dan mengeksekusi Insert/Update saat `SaveChanges`. |
| `PasienApproveUploadSasetHandler` | `IPasienRepo` | Memuat model agregat pasien, memanggil `ApproveUploadSaset()`, dan menyimpan perubahan via `SaveChanges()`. |
| `PasienController` | `IMediator` | Meneruskan command `PasienApproveUploadSasetCmd` dari HTTP PATCH ke Application Handler. |
| Frontend `useRegistrasiActions.ts` | `ApiService` (`apiName: 'jetliApi'`) | Memanggil `{GET} /GrupJaminan/map?tipeJaminanId={id}` untuk mendeteksi apakah jaminan termasuk kategori BPJS. |
| Frontend `useRegistrasiActions.ts` | `PasienService: useApproveUploadSaset` | Mengirimkan mutasi HTTP PATCH ke `/api/pasien/approveUploadSaset` dengan payload `{ pasienId }`. |
| Frontend `LegacyRegistrationWorkspace.vue` | State `pasienDetail.pasienSaset` | Me-render warna dan status label visual `#SatuSehat` secara reaktif. |
| Frontend `useRegistrasiActions.ts` | Registrasi Mutator (`createWalkIn`, `createFromBooking`, dll.) | Menjalankan penyimpanan registrasi pasien setelah tahap evaluasi dan persetujuan Satu Sehat selesai atau di-bypass. |

---

# 7. Data Ownership

| Data | Owner |
|---|---|
| Status & Detail Satu Sehat Pasien (`PasienSasetModel`, `tc_mr_saset`) | `Bilreg.Domain.PasienContext.PasienFeature` |
| Agregat Pasien Utama (`PasienModel`, `tc_mr`, `tc_mr_ktp`, `tc_mr_telp`, `tc_mr_id`) | `Bilreg.Domain.PasienContext.PasienFeature` |
| Data Transaksi Registrasi Pasien (`RegModel`, `RegAktifModel`, dll.) | `Bilreg.Domain.AdmisiContext.RegFeature` |
| Pemetaan Grup Jaminan (`GrupJaminan`) | Sistem Eksternal Jetli / Middleware JKN-RS (`jetliApi`) |
| Cache Client Grup Jaminan | Frontend Runtime State (`useRegistrasiActions` / composable cache) |

---

# 8. Database Design

## New Tables
Tidak ada. Tabel `tc_mr_saset` telah ada di skema database fisik.

## Modified Tables
Tabel `tc_mr_saset` yang sebelumnya belum diakses oleh `b09-bilreg-api` kini dikelola secara formal:

| Table | Change |
|---|---|
| `tc_mr_saset` | Dikelola oleh `PasienSasetDal` & `PasienRepo` dengan operasi CRUD dan pola *Upsert* berbasis `KodeMr`. Kolom: `KodeMr` (PK), `KodeSaset`, `IsApprovedUpload`, `TglJamApprovedUpload`, `FileGeneralConcentUpload`, `IsApprovedView`, `TglJamApprovedView`, `FileGeneralConcentView`. |

## Relationships
- Relasi 1-to-1 antara `tc_mr` (`fs_mr`) dan `tc_mr_saset` (`KodeMr`).
- `KodeMr` pada `tc_mr_saset` mereferensikan `PasienId` (`fs_mr`). Record dibuat jika belum ada (*lazy on-demand creation*) saat pertama kali persetujuan dicatat.

## Migration Considerations
- Tidak diperlukan DDL migrasi skema karena tabel `tc_mr_saset` sudah ada di database operasional.
- Tidak diperlukan batch data backfill massal; pasien lama yang belum memiliki baris data di `tc_mr_saset` akan diisi secara otomatis (*lazy upsert*) saat pasien melakukan kunjungan dan disetujui untuk upload.

---

# 9. Cross-Cutting Concerns

### 1. Resilience & Fault Tolerance (Non-Blocking Guarantee)
Proses simpan registrasi admisi adalah alur operasional kritis. Oleh karena itu:
- Seluruh pemanggilan ke JetliApi dan endpoint approval Satu Sehat dibungkus dalam blok `try-catch` dengan timeout terbatas (maksimal 3-5 detik).
- Jika terjadi exception (network timeout, service down, HTTP 500), alur simpan registrasi **tetap diproses hingga tuntas**. Petugas loket mendapatkan notifikasi peringatan (toast warning), bukan pesan error yang menghentikan sistem.

### 2. Client-Side Performance & Caching
Endpoint `/JetliAPi/api/GrupJaminan/map` diakses menggunakan *in-memory client caching* berdasarkan `tipeJaminanId`. Permintaan HTTP hanya dikirimkan satu kali per jenis jaminan selama sesi browser aktif, meniadakan penundaan jaringan yang berulang saat klik simpan registrasi.

### 3. Idempotency & Concurrency
Endpoint `PasienApproveUploadSasetCmd` bersifat idempoten: jika pasien sudah pernah disetujui, pemanggilan ulang hanya memperbarui waktu approval tanpa memicu duplikasi data atau error *constraint violation* di database.

### 4. Audit & Traceability
Waktu persetujuan disimpan dalam kolom `TglJamApprovedUpload`. Log peringatan dicatat pada console dan monitoring bila terjadi kegagalan sinkronisasi dengan format terstruktur.

---

# 10. Implementation Constraints

1. **Target Framework & Compatibility**:
   - Backend `b09-bilreg-api`: .NET 8.0 (`net8.0`).
   - Frontend `c012_myhospital_web`: Vue 3 + TypeScript + TanStack Vue Query + Zod + Tailwind CSS.
2. **Architecture Compliance**:
   - Layer `Bilreg.Domain` tidak boleh bergantung pada layer Infrastructure atau Application.
   - Operasi database harus menggunakan Dapper murni melalui `IPasienSasetDal` dengan query SQL eksplisit (tanpa EF Core).
   - Pengelolaan transaksi database di `PasienRepo` wajib menggunakan `using var trans = TransHelper.NewScope()`.
3. **Response Envelope Consistency**:
   - Endpoint PATCH `approveUploadSaset` harus mengembalikan format standar `JSendOk("Done")`.
4. **Resilient Frontend Interception**:
   - Handler `handleSubmitRegister` dilarang memunculkan dialog persetujuan jika pasien terbukti merupakan jaminan BPJS (wajib auto-approve).
   - Handler dilarang memblokir pendaftaran jika persetujuan gagal dieksekusi.
5. **Preserve Existing Tests**:
   - Seluruh test suite eksisting untuk `PasienRepo`, `PasienGetQuery`, dan usecase registrasi admisi harus tetap lulus (*PASS*).

---

# 11. Acceptance Conditions

1. Kelas entitas `PasienSasetModel`, interface `IPasienSasetDal`, DTO `PasienSasetDto`, dan implementasi `PasienSasetDal` terpasang di `b09-bilreg-api` dan terdaftar pada DI container.
2. `PasienRepo.LoadEntity` memuat data `PasienSasetModel` dengan aman (mengembalikan nilai default yang valid jika baris di `tc_mr_saset` belum ada).
3. `PasienRepo.SaveChanges` melakukan operasi *Upsert* (Insert jika belum ada, Update jika sudah ada) ke tabel `tc_mr_saset`.
4. Endpoint `{GET} /api/pasien/{id}` mengembalikan data atribut Satu Sehat pada properti `pasienSaset` di dalam respons JSON.
5. Endpoint `{PATCH} /api/pasien/approveUploadSaset` dapat dipanggil dengan body `{ "pasienId": "..." }` dan mengembalikan respons `{ "status": "success", "data": "Done" }` serta memperbarui `IsApprovedUpload = 1` pada database.
6. Frontend `dataSosialPasienSchemaNew` berhasil memvalidasi payload respon data pasien yang memuat objek `pasienSaset`.
7. Kartu data pasien di `LegacyRegistrationWorkspace.vue` menampilkan indikator `#SatuSehat` dengan warna hijau jika `isApprovedUpload === true`, dan warna abu-abu jika belum disetujui / false.
8. Pada saat tombol **Simpan Registrasi** ditekan:
   - Jika pasien belum disetujui (`isApprovedUpload === false`) dan jaminan terdeteksi sebagai **BPJS**: sistem otomatis memanggil endpoint approval Satu Sehat tanpa memunculkan pop-up dialog, kemudian melanjutkan simpan registrasi.
   - Jika pasien belum disetujui (`isApprovedUpload === false`) dan jaminan adalah **Non-BPJS**: sistem memunculkan pop-up dialog konfirmasi persetujuan upload Satu Sehat. Jika disetujui (OK), endpoint approval dipanggil lalu registrasi disimpan; jika ditolak/dibatalkan, registrasi langsung disimpan tanpa approval.
   - Jika endpoint approval atau JetliApi mengalami error/timeout: sistem menampilkan warning toast dan proses simpan registrasi tetap berhasil tersimpan.
9. Seluruh pengujian unit (*unit tests*) untuk use case baru dan modifikasi repositori berstatus hijau (*PASS*).
