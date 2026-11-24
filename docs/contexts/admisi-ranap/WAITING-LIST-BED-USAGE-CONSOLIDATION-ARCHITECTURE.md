---
Title: Architecture - Penyatuan Fitur WaitingList ke BedUsageContext dan Migrasi Penggunaan dari Admisi Ranap
Code: WAITING-LIST-BED-USAGE-CONSOLIDATION
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-09
---

# 1. Overview

Arsitektur ini mendefinisikan realisasi teknis untuk menyatukan fungsionalitas antrean rawat inap (*Waiting List*) ke dalam satu-satunya bounded context di backend, yaitu **`BedUsageContext.WaitingListFeature`** (`/api/bed-usage/waiting-list`), menghapus duplikasi implementasi pada **`AdmisiRanapContext.WaitingListFeature`**, serta mengalihkan seluruh pemanggilan dari frontend Admisi Ranap (**`c012_myhospital_web`**) ke endpoint BedUsage tersebut.

Realisasi teknis ini memastikan:
1. **Single Source of Truth**: Seluruh siklus hidup antrean tempat tidur dikelola secara eksklusif oleh `BedUsageContext`, menghilangkan redundansi domain model, use case, DTO, dan repository.
2. **Zero Regression pada Frontend Admisi Ranap**: Seluruh kontrak REST API (format response single-object/nullable pada `by-reg`, envelope `{ items: [...] }` pada list antrean, dan validasi schema Zod) dipenuhi secara presisi.
3. **Preservasi Kepatuhan SOP Outstanding**: Menjamin worklist antrean aktif hanya menampilkan pasien yang belum menempati bed (`Waiting (0)`, `Reserved (1)`, `Rerouted (4)`, `Rejected (5)`), mengecualikan pasien yang sudah menempati bed (`Placed (2)`) atau batal (`Cancelled (3)`), serta menjaga fungsi penguncian *default view* dan pemblokiran pendaftaran baru saat `outstandingCount > 0`.
4. **Integrasi Reaktif Real-Time**: Penerbitan notifikasi event real-time ke bangsal (`IWardCockpitEventPublisher`) saat pendaftaran admisi berhasil menerbitkan antrean baru.

Referenced Issue:
- [WAITING-LIST-BED-USAGE-CONSOLIDATION-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-ISSUE.md) (`ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-001`)

---

# 2. Architectural Basis

## Business Context

- **Manajemen Antrean Tempat Tidur (Bed Waiting List)**: Mengelola pasien yang telah diputuskan rawat inap namun menunggu ketersediaan kamar, penyiapan tempat tidur oleh perawat, atau sedang dialihkan karena kapasitas ruangan penuh.
- **Konsol Admisi Rawat Inap**: Petugas admisi memantau daftar antrean pasien secara visual (`DaftarTungguLane.vue`), menginput prioritas dan bangsal tujuan, serta menangani pasien yang ditolak oleh ruangan.
- **SOP Admisi Rawat Inap**: Mengutamakan penyelesaian antrean/reservasi yang terlambat (*Outstanding*). Selama terdapat pasien *outstanding*, konsol otomatis mengunci tampilan kerja dan memblokir pendaftaran baru.

Referenced Knowledge Artifacts:
- DOMAIN: [`admisi-ranap-domain.md`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)
- DOMAIN: [`SOP-RNA-A01-Pemrosesan-Waiting-List-dan-Penetapan-Akomodasi.md`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/bangsal/rna-sop/SOP-RNA-A01-Pemrosesan-Waiting-List-dan-Penetapan-Akomodasi.md)

## Analysis Input

Realisasi arsitektur ini didasarkan pada keputusan penutupan gap (*Gap Closure Decisions*) yang disetujui pada:
- **FEASIBILITY-ASSESSMENT**: [`WAITING-LIST-BED-USAGE-CONSOLIDATION-FEASIBILITY-ASSESSMENT.md`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-FEASIBILITY-ASSESSMENT.md) (`FA-WAITING-LIST-BED-USAGE-CONSOLIDATION-001`, Status: `READY-FOR-PLANNING`).

```text
ISSUE (ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-001)
        +
FEASIBILITY-ASSESSMENT (FA-WAITING-LIST-BED-USAGE-CONSOLIDATION-001)
        ↓
    ARCHITECTURE (WAITING-LIST-BED-USAGE-CONSOLIDATION-ARCHITECTURE)
```

---

# 3. Scope

## Included

1. **Pengembangan & Standardisasi Kontrak di `BedUsageContext.WaitingListFeature` (`b09-bilreg-api`)**:
   - Menambahkan query dan endpoint `GET /api/bed-usage/waiting-list` (list antrean dengan filter `bangsalId` dan `waitingListStatus`).
   - Mengubah use case dan endpoint `GET /api/bed-usage/waiting-list/by-reg/{regId}` agar mengembalikan *single object / null*, bukan array.
   - Memperbarui `WaitingListCreateHandler` agar idempotent (mengembalikan antrean aktif yang sudah ada jika regId terdaftar ganda).
   - Menjamin filter default hanya menyaring antrean aktif yang belum menempati bed (`Waiting`, `Reserved`, `Rerouted`, `Rejected`).
2. **Pembersihan Total Modul di `AdmisiRanapContext`**:
   - Mengalihkan dependensi `AdmissionRegistrationOrchestrator`, `AdmCoordinatedCancelCmd`, dan `JourneyFeature` langsung ke `BedUsageContext.WaitingListFeature`.
   - Mengintegrasikan penerbitan event `IWardCockpitEventPublisher.PublishWaitingListUpdatedAsync(bangsalId)` di `AdmissionRegistrationOrchestrator`.
   - Menghapus controller [`AdmisiRanapContext/WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs).
   - Menghapus seluruh file Domain, Application, Infrastructure, dan Test `WaitingListFeature` pada `AdmisiRanapContext`.
3. **Pembaruan Frontend (`c012_myhospital_web`)**:
   - Mengubah rute endpoint `WAITING_LIST` di `AdmisiRanapService.ts` ke `bed-usage/waiting-list`.
   - Menghapus fungsi *dead-code* `useCloseWaitingList()` dan schema terkait.
   - Memperbarui Zod schema status `waitingListStatusSchema` agar memuat status `4` (`Rerouted: 'Dialihkan'`).
   - Menyelaraskan kamus status `WAITING_LIST_STATUS_MAP` di `admisiRanapStatus.ts`.

## Excluded

1. Perubahan skema DDL fisik pada basis data (tabel fisik `BILRG_BedWaitingList` dipertahankan apa adanya).
2. Perubahan pada alur operasional Cockpit Bangsal (`wardCockpitApi.ts`, `WardCheckInCommand`, `WardRoomingInCommand`).

---

# 4. Technical Decisions

### TD-001: Pilihan Pengendali Tunggal (Single Controller Architecture)
* **Realisasi**: Seluruh antrean rawat inap diakses melalui satu controller publik: [`BedUsageContext.WaitingListFeature.WaitingListController`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/BedUsageContext/WaitingListFeature/WaitingListController.cs) dengan rute `api/bed-usage/waiting-list`. Controller lama di `AdmisiRanapContext` dihapus permanen.
* **Tanggung Jawab**: Melayani kebutuhan pendaftaran antrean admisi, monitoring worklist bangsal, alokasi bed, penolakan bangsal, pengalihan ruangan, dan pembatalan antrean.

### TD-002: Desain Kontrak `GET by-reg/{regId}` (Single-Record Active Query)
* **Realisasi**: Mengubah signature request MediatR `WaitingListGetByRegQuery(string RegId)` agar mengembalikan `IRequest<WaitingListGetResponse?>`.
* **Logika Query**: Mengambil antrean aktif (`WaitingListStatus IN (0, 1, 4, 5)`) untuk `RegId` tersebut. Jika tidak ditemukan antrean aktif, mengembalikan antrean dengan `CrtDate` terbaru atau `null`.
* **Integrasi Frontend**: Memenuhi kontrak Zod `waitingListDetailSchema.nullable()` tanpa error parsing tipe array.

### TD-003: Desain Kontrak `GET /api/bed-usage/waiting-list` (Filtered List Query)
* **Realisasi**: Membuat query MediatR baru:
  ```csharp
  public record WaitingListListQuery(
      string? BangsalId = null,
      int? WaitingListStatus = null) : IRequest<WaitingListListResponse>;

  public record WaitingListListResponse(IReadOnlyList<WaitingListWorklistView> Items);
  ```
* **Filter Default**:
  - Jika `WaitingListStatus` tidak disuplai (null), klausa SQL secara default memfilter:
    `aa.WaitingListStatus IN (0, 1, 4, 5)` *(Waiting, Reserved, Rerouted, Rejected)*.
  - Status `Placed (2)` dan `Cancelled (3)` dieksklusikan secara ketat agar pasien yang sudah menempati bed atau batal tidak muncul di worklist antrean aktif.
  - Jika `WaitingListStatus` disuplai, memfilter sesuai status spesifik tersebut.

### TD-004: Idempotensi Pendaftaran Antrean (`WaitingListCreateCmd`)
* **Realisasi**: Pada `WaitingListCreateHandler.cs`, mengganti pelemparan exception benturan dengan pengecekan idempotensi:
  ```csharp
  if (_waitingListRepo.HasActiveByRegId(request.RegId))
  {
      var existing = _waitingListRepo.LoadActiveByRegId(request.RegId);
      if (existing.HasValue)
          return Task.FromResult(new WaitingListCreateResponse(existing.Value.WaitingListId));
  }
  ```
* **Manfaat**: Menjamin pendaftaran otomatis dari reservasi atau retry antarmuka web tidak menghasilkan HTTP 500 error.

### TD-005: Penyelarasan Status Enum dan Pemetaan UI
* **Realisasi**:
  - **Backend**: Menggunakan nilai enum `BedUsageContext.WaitingListStatusEnum`:
    - `0 = Waiting`
    - `1 = Reserved`
    - `2 = Placed`
    - `3 = Cancelled`
    - `4 = Rerouted`
    - `5 = Rejected`
  - **Frontend**: Memperbarui `WAITING_LIST_STATUS_MAP` di `admisiRanapStatus.ts`:
    - `0`: `{ label: 'Menunggu', variant: 'warning' }`
    - `1`: `{ label: 'Dipesankan', variant: 'success' }`
    - `2`: `{ label: 'Ditempatkan', variant: 'outline' }`
    - `3`: `{ label: 'Dibatalkan', variant: 'destructive' }`
    - `4`: `{ label: 'Dialihkan', variant: 'secondary' }`
    - `5`: `{ label: 'Ditolak Ruangan', variant: 'destructive' }`

### TD-006: Event Notification Reaktif Lintas-Konteks
* **Realisasi**: Di `AdmissionRegistrationOrchestrator.cs` (Admisi Ranap), saat metode `PlaceWaitingListGracefully` selesai menyimpan antrean baru ke `BedUsageContext`, sistem menginjeksi dan memanggil:
  `await _wardCockpitEventPublisher.PublishWaitingListUpdatedAsync(bangsalId);`
* **Manfaat**: Layar Cockpit Bangsal tujuan langsung menerima sinyal SignalR / event refresh tanpa mengharuskan perawat melakukan *hard reload*.

---

# 5. Component Responsibilities

| Komponen | Layer | Tanggung Jawab Utama |
| :--- | :--- | :--- |
| **`WaitingListController`** (`BedUsageContext`) | Api | Mengekspos rute REST `/api/bed-usage/waiting-list` untuk seluruh mutasi dan query antrean. |
| **`WaitingListModel`** (`BedUsageContext`) | Domain | Entitas agregat tunggal yang mengontrol aturan invarian status, alokasi bed, pembatalan, penolakan, dan pengalihan ruangan. |
| **`WaitingListCreateHandler`** (`BedUsageContext`) | Application | Menangani pembuatan antrean secara idempotent dengan default parameter lengkap. |
| **`WaitingListGetByRegHandler`** (`BedUsageContext`) | Application | Mengambil antrean aktif tunggal (atau rekaman terbaru) untuk suatu `RegId`. |
| **`WaitingListListHandler`** (`BedUsageContext`) | Application | Menyediakan data antrean aktif yang belum menempati bed untuk worklist konsol. |
| **`WaitingListDal` & `WaitingListWorklistDal`** (`BedUsageContext`) | Infrastructure | Eksekusi query Dapper SQL pada tabel `BILRG_BedWaitingList`. |
| **`AdmissionRegistrationOrchestrator`** (`AdmisiRanapContext`) | Application | Mengorkestrasi pendaftaran admisi dan memanggil `BedUsageContext.WaitingListFeature.IWaitingListRepo` serta menerbitkan notifikasi event bangsal. |
| **`AdmisiRanapService.ts`** (`c012_myhospital_web`) | Frontend Service | Klien HTTP terpadu yang memanggil rute `bed-usage/waiting-list` untuk komponen UI Admisi. |
| **`AdmisiRanapConsole.vue`** (`c012_myhospital_web`) | Frontend View | Mengorkestrasi tampilan konsol admisi, filter worklist antrean, dan penguncian SOP *Outstanding*. |

---

# 6. Integration Design

| Pengirim (*Source*) | Penerima (*Target*) | Protokol / Saluran | Tujuan Integrasi |
| :--- | :--- | :--- | :--- |
| **`AdmisiRanapService.ts`** (FE) | **`WaitingListController`** (BE) | HTTP REST (`/api/bed-usage/waiting-list`) | Operasi CRUD, pembatalan, perbaruan, dan query antrean dari UI Admisi. |
| **`wardCockpitApi.ts`** (FE) | **`WaitingListController`** (BE) | HTTP REST (`/api/bed-usage/waiting-list/{id}/reject`) | Penolakan antrean oleh perawat bangsal saat bed penuh. |
| **`WaitingListController`** | **MediatR Pipeline** | In-Process CQRS | Meneruskan command dan query ke use case handler `BedUsageContext`. |
| **`AdmissionRegistrationOrchestrator`** | **`IWaitingListRepo`** (`BedUsageContext`) | Direct Interface Injection | Menyimpan antrean otomatis saat registrasi admisi berhasil. |
| **`AdmissionRegistrationOrchestrator`** | **`IWardCockpitEventPublisher`** | Event Bus In-Process | Menerbitkan event notifikasi ke bangsal tujuan agar antrean diperbarui secara real-time. |
| **`WaitingListDal` / `Repo`** | **Database `BILRG_BedWaitingList`** | Dapper (T-SQL) | Persistensi data siklus hidup antrean. |

---

# 7. Data Ownership

| Data Agregat / Entitas | Pemilik Tunggal (*Owner*) | Keterangan Kebijakan Kepemilikan |
| :--- | :--- | :--- |
| **Waiting List Pasien Ranap** (`BILRG_BedWaitingList`) | **`BedUsageContext.WaitingListFeature`** | Pemilik mutlak seluruh status antrean, alokasi bed sementara, histori penolakan, dan pengalihan ruangan. Modul lain tidak boleh menulis langsung ke tabel ini. |
| **Pendaftaran Rawat Inap** (`BILRG_AdmAdmission`) | **`AdmisiRanapContext.AdmissionFeature`** | Mengelola dokumen legal pendaftaran ranap dan riwayat status admisi pasien. |
| **Alokasi Tempat Tidur Fisik** (`BILRG_RnaPakaiBed`) | **`BedUsageContext.PakaiBedFeature`** | Mengelola penggunaan tempat tidur nyata saat pasien check-in (mengubah status waiting list menjadi `Placed`). |

---

# 8. Database Design

## Modified Tables / Preserved Physical Storage

Tidak ada tabel baru yang dibuat. Kedua modul menggunakan tabel yang sudah ada:

| Tabel Basis Data | Status Perubahan | Keterangan Desain |
| :--- | :--- | :--- |
| **`BILRG_BedWaitingList`** | **Preserved (No DDL Change)** | Seluruh kolom eksisting (`WaitingListId`, `RegId`, `WaitingListStatus`, `Priority`, `KelasId`, `BangsalId`, `PlannedBedId`, `RejectReason`, `CancellationReason`, `RerouteReason`, dll) dipertahankan. Lapisan DAL BedUsage telah mencakup seluruh kolom tersebut. |

## Migration & Data Compatibility Considerations

1. **Backwards Data Compatibility**:
   Karena data `WaitingList` selama ini telah ditulis ke tabel fisik yang sama (`BILRG_BedWaitingList`), penyatuan ke `BedUsageContext` tidak memerlukan script migrasi data SQL (*no data migration needed*). Data historis tetap utuh dan terbaca langsung oleh DAL BedUsage.
2. **Kesesuaian Kolom Audit**:
   Kolom audit trail (`CrtUser`, `CrtDate`, `UpdUser`, `UpdDate`, `VodUser`, `VodDate`) telah dipetakan secara konsisten oleh `WaitingListDal` di BedUsageContext.

---

# 9. Cross-Cutting Concerns

1. **Konkurensi & Idempotensi**:
   Pendaftaran rawat inap yang memicu `WaitingListCreateCmd` dilindungi oleh pemeriksaan `HasActiveByRegId`. Jika koneksi terputus dan antarmuka mengulang *request*, sistem tidak menerbitkan ID antrean baru melainkan mengembalikan antrean aktif yang sudah tercipta.
2. **Real-time Observability**:
   Aksi mutasi antrean (termasuk *create* otomatis dari admisi dan *reject* dari bangsal) memanggil `IWardCockpitEventPublisher` agar layar perawat bangsal dan petugas admisi tersinkronisasi.
3. **Penyaringan Worklist Konsol & SOP Outstanding**:
   Query worklist antrean wajib mengecualikan status `Placed (2)` dan `Cancelled (3)`, sehingga kartu antrean yang tampil di konsol admisi murni pasien yang belum menempati kamar.
   Kepatuhan terhadap SOP *Outstanding* tetap dijamin melalui fungsi evaluasi `outstandingCount` pada konsol Admisi Ranap.

---

# 10. Implementation Constraints

1. **Struktur Bersih (*Clean Architecture*)**:
   Tidak boleh ada sisa file atau referensi ke `AdmisiRanapContext.WaitingListFeature`. Seluruh namespace yang berkaitan dengan `Bilreg.Application.AdmisiRanapContext.WaitingListFeature`, `Bilreg.Domain.AdmisiRanapContext.WaitingListFeature`, dan `Bilreg.Infrastructure.AdmisiRanapContext.WaitingListFeature` harus dihapus bersih dari solusi.
2. **Framework & Library**:
   - Backend menggunakan C# .NET 8, Dapper, MediatR, dan Nuna library.
   - Frontend menggunakan Vue 3, Pinia / TanStack Query, dan Zod validasi.
3. **Zero Regression pada Inpatient**:
   Endpoint `POST /api/bed-usage/waiting-list/{id}/reject` yang telah digunakan oleh `wardCockpitApi.ts` tidak boleh mengalami perubahan signature payload atau respons.

---

# 11. Acceptance Conditions

Implementasi arsitektur ini dinyatakan lengkap dan valid apabila:

1. **Backend Tests Green**:
   Seluruh test suite di `b09-bilreg-api` (khususnya `Bilreg.Test.BedUsageContext.WaitingListFeature` dan `Bilreg.Test.AdmisiRanapContext`) lulus 100% tanpa kesalahan kompilasi atau kegagalan assertion.
2. **Single Context Enforcement**:
   Folder `WaitingListFeature` di `AdmisiRanapContext` (Domain, Application, Infrastructure, Controller, Test) telah terhapus seutuhnya, dan backend hanya memiliki `api/bed-usage/waiting-list`.
3. **Endpoint Parity & Contract Matching**:
   - `GET /api/bed-usage/waiting-list/by-reg/{regId}` mengembalikan single-object atau null yang lolos validasi Zod `waitingListDetailSchema.nullable()`.
   - `GET /api/bed-usage/waiting-list` mengembalikan `{ items: [...] }` yang hanya berisi pasien berstatus aktif (`Waiting`, `Reserved`, `Rerouted`, `Rejected`).
4. **Frontend Integration Verified**:
   - Pembuatan pendaftaran admisi baru, perbaruan prioritas antrean, dan tampilan tab Daftar Tunggu pada `c012_myhospital_web` berfungsi normal.
   - Logika SOP *Outstanding* tetap aktif dan memblokir pendaftaran baru jika `outstandingCount > 0`.
