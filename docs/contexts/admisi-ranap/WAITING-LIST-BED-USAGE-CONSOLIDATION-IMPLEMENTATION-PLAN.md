---
Title: Implementation Plan - Penyatuan Fitur WaitingList ke BedUsageContext dan Migrasi Penggunaan dari Admisi Ranap
Code: WAITING-LIST-BED-USAGE-CONSOLIDATION
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-09
Status: IN-PROGRESS
Execution Approval: APPROVED
---

# 1. Objective

Implementasi ini merealisasikan penyatuan fitur antrean rawat inap (*Waiting List*) ke dalam satu-satunya bounded context di backend, yaitu **`BedUsageContext.WaitingListFeature`** (`/api/bed-usage/waiting-list`), menghapus seluruh kode duplikat pada **`AdmisiRanapContext.WaitingListFeature`**, serta memigrasikan pemanggilan antarmuka frontend Admisi Ranap (**`c012_myhospital_web`**) ke endpoint BedUsage tersebut.

Rencana implementasi ini dirancang dengan prinsip **zero regression**:
1. Menjamin seluruh kontrak antarmuka API (format single-object/null pada `by-reg`, envelope `{ items: [...] }` pada list antrean, dan kesesuaian schema Zod) terpenuhi.
2. Memastikan worklist antrean aktif murni menampilkan pasien yang belum menempati bed (`Waiting`, `Reserved`, `Rerouted`, `Rejected`) dan mengecualikan pasien yang sudah masuk bed (`Placed`) atau batal (`Cancelled`).
3. Menjamin kepatuhan terhadap SOP *Outstanding* di konsol Admisi Ranap (`outstandingCount > 0` mengunci tampilan ke *Outstanding Only* dan memblokir pendaftaran baru).
4. Mengintegrasikan notifikasi event bus real-time ke bangsal (`IWardCockpitEventPublisher`) saat admisi menerbitkan antrean baru.

Referenced artifacts:
- Target Architecture: [WAITING-LIST-BED-USAGE-CONSOLIDATION-ARCHITECTURE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-ARCHITECTURE.md)
- Feasibility Assessment: [WAITING-LIST-BED-USAGE-CONSOLIDATION-FEASIBILITY-ASSESSMENT.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-FEASIBILITY-ASSESSMENT.md) (`READY-FOR-PLANNING`)
- Originating Issue: [WAITING-LIST-BED-USAGE-CONSOLIDATION-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-ISSUE.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

## Included Scope
1. **Peningkatan Kapabilitas Use Case & DAL BedUsage (`b09-bilreg-api`)**:
   - Pembuatan use case `WaitingListListQuery` dan DTO respons `{ items: [...] }` di `BedUsageContext`.
   - Implementasi query penyaringan worklist antrean aktif (`0, 1, 4, 5`) dan pengecualian status terminal (`2, 3`) pada `WaitingListWorklistDal`.
   - Penyesuaian `WaitingListGetByRegQuery` & handler agar mengembalikan single-object/null (`WaitingListGetResponse?`).
   - Penyesuaian `WaitingListCreateHandler` agar bersifat idempotent saat menerima registrasi ganda / network retry.
2. **Peningkatan Controller BedUsage (`b09-bilreg-api`)**:
   - Penambahan endpoint `GET /api/bed-usage/waiting-list` (list filter status & bangsal).
   - Penyesuaian respons endpoint `GET /api/bed-usage/waiting-list/by-reg/{regId}`.
3. **Pengalihan Orkestrator Admisi Ranap ke BedUsage (`b09-bilreg-api`)**:
   - Pengalihan injeksi `IWaitingListRepo` dan pembuatan model di `AdmissionRegistrationOrchestrator.cs` ke BedUsage.
   - Penambahan pemanggilan `IWardCockpitEventPublisher.PublishWaitingListUpdatedAsync(bangsalId)`.
   - Penyesuaian pembatalan terkoordinasi (`AdmCoordinatedCancelCmd.cs`) dan timeline journey (`JourneyFeature`).
4. **Pembersihan Total Modul Lama di Admisi Ranap (`b09-bilreg-api`)**:
   - Penghapusan controller `Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs`.
   - Penghapusan seluruh file Domain, Application, Infrastructure, dan Test `WaitingListFeature` pada `AdmisiRanapContext`.
5. **Migrasi Frontend Admisi Ranap (`c012_myhospital_web`)**:
   - Pengubahan konstanta `API_ENDPOINT.WAITING_LIST` ke `'bed-usage/waiting-list'`.
   - Penghapusan fungsi dead-code `useCloseWaitingList`.
   - Pembaruan skema Zod `waitingListStatusSchema` (memuat status 4) dan kamus label `WAITING_LIST_STATUS_MAP`.
   - Verifikasi pengujian unit konsol Admisi Ranap dan validasi alur SOP *Outstanding*.

## Excluded Scope
1. DDL modifikasi skema tabel basis data fisik (tabel `BILRG_BedWaitingList` dipertahankan apa adanya).
2. Perubahan pada alur operasional internal Cockpit Bangsal (`wardCockpitApi.ts`, `WardCheckInCommand`, `WardRoomingInCommand`).

---

# 3. Dependencies

- **Pre-requisite Status**: Seluruh GAP dan OQ pada `WAITING-LIST-BED-USAGE-CONSOLIDATION-FEASIBILITY-ASSESSMENT.md` berstatus `CLOSED`, gerbang `READY-FOR-PLANNING` telah disetujui, dan `WAITING-LIST-BED-USAGE-CONSOLIDATION-ARCHITECTURE.md` telah dirilis.
- **Inter-Slice Dependencies**:
  - `P1-S01` (BedUsage Use Case & Query Parity) adalah fondasi bagi endpoint controller.
  - `P1-S02` (BedUsage Controller & Tests) bergantung pada `P1-S01`.
  - `P2-S03` (Migrasi Orkestrator Admisi ke BedUsage) bergantung pada `P1-S02`.
  - `P2-S04` (Pembersihan Modul Lama Admisi) bergantung pada `P2-S03`.
  - `P3-S05` (Migrasi Service & Schema FE) bergantung pada `P1-S02`.
  - `P3-S06` (Verifikasi Komponen FE & Kepatuhan SOP) bergantung pada `P2-S04` dan `P3-S05`.

---

# 4. Progress Summary

Status: IN-PROGRESS  
Execution Approval: APPROVED  

| Phase | Description | Repository | Implementation Status | Review Status | Progress |
| :--- | :--- | :--- | :--- | :--- | :---: |
| **P1** | BedUsage API Enhancement & Parity | `b09-bilreg-api` | IMPLEMENTED | GO | 2/2 |
| **P2** | Admisi Ranap Backend Migration & Cleanup | `b09-bilreg-api` | NOT-STARTED | NOT-REVIEWED | 0/2 |
| **P3** | Frontend Migration & UI Verification | `c012_myhospital_web` | NOT-STARTED | NOT-REVIEWED | 0/2 |

---

# 5. Phases

## P1 - BedUsage API Enhancement & Parity

Implementation Status: IMPLEMENTED  
Review Status: GO  

---

### P1-S01
Title: Implementasi Query Parity (Single by-reg & Filtered List) dan Idempotensi Create di BedUsage  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective:
Menyediakan use case dan persistensi di `BedUsageContext.WaitingListFeature` yang mencakup:
1. Mengubah `WaitingListGetByRegQuery` & handler agar mengembalikan single-object/null (`WaitingListGetResponse?`) untuk registrasi tertentu.
2. Membuat `WaitingListListQuery` dan DTO response envelope `{ items: [...] }` di application layer.
3. Mengimplementasikan kueri worklist pada `WaitingListWorklistDal` yang secara default memfilter hanya status aktif (`0=Waiting`, `1=Reserved`, `4=Rerouted`, `5=Rejected`) dan mengecualikan status terminal `2=Placed` serta `3=Cancelled`.
4. Mengubah `WaitingListCreateHandler` agar idempotent (mengembalikan antrean aktif yang ada jika `HasActiveByRegId` bernilai true).

Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `WaitingListGetByRegQuery` mengembalikan `WaitingListGetResponse?` (bukan list).
- `WaitingListListQuery` tersedia dan dapat dieksekusi via MediatR.
- `WaitingListWorklistDal` memfilter status aktif secara ketat dan mengecualikan status `Placed (2)` dan `Cancelled (3)`.
- `WaitingListCreateHandler` mengembalikan ID yang sudah ada secara aman jika regId terdaftar ganda.

Implementation Notes:
- Mengubah signature `WaitingListGetByRegQuery` menjadi `IRequest<WaitingListGetResponse?>` dan mengimplementasikan pengambilan antrean aktif via `LoadActiveByRegId`, fallback ke antrean historis terbaru via `ListByRegId().FirstOrDefault()`, atau `null`.
- Membuat `WaitingListListQuery`, `WaitingListListResponse`, dan `WaitingListListHandler` dengan envelope `{ items: [...] }` menggunakan DTO `WaitingListWorklistView`.
- Menambahkan metode `List(WaitingListWorklistFilter filter)` pada `IWaitingListWorklistDal` dan `WaitingListWorklistDal` dengan filter default status aktif `(0, 1, 4, 5)` dan mengecualikan status terminal `Placed (2)` dan `Cancelled (3)`, serta menyaring `VodDate = 3000-01-01` dan pengurutan `Priority DESC, CrtDate ASC`.
- Memperbarui `WaitingListDal.GetActiveByRegId` agar mencakup status aktif `(Waiting, Reserved, Rerouted, Rejected)`.
- Memperbarui `WaitingListCreateHandler` agar memeriksa `HasActiveByRegId` di awal dan mengembalikan antrean aktif yang sudah ada secara idempotent tanpa memicu error atau exception pendaftaran ganda.
- Memperbarui dan menambahkan unit test pada `WaitingListHandlerTest.cs` (UT03 idempotency, UT17 active queue, UT18 latest history, UT18B null, UT21 list query envelope) dan `WaitingListControllerTest.cs`. Seluruh 95 pengujian unit pada `BedUsageContext.WaitingListFeature` lulus 100%.

Changed Files:
- `src/bilreg/Bilreg.Application/BedUsageContext/WaitingListFeature/IWaitingListWorklistDal.cs`
- `src/bilreg/Bilreg.Application/BedUsageContext/WaitingListFeature/UseCases/WaitingListListQuery.cs`
- `src/bilreg/Bilreg.Application/BedUsageContext/WaitingListFeature/UseCases/WaitingListGetByRegQuery.cs`
- `src/bilreg/Bilreg.Application/BedUsageContext/WaitingListFeature/UseCases/WaitingListCreateCmd.cs`
- `src/bilreg/Bilreg.Infrastructure/BedUsageContext/WaitingListFeature/WaitingListDal.cs`
- `src/bilreg/Bilreg.Infrastructure/BedUsageContext/WaitingListFeature/WaitingListWorklistDal.cs`
- `src/bilreg/Bilreg.Test/BedUsageContext/WaitingListFeature/WaitingListHandlerTest.cs`
- `src/bilreg/Bilreg.Test/BedUsageContext/WaitingListFeature/WaitingListControllerTest.cs`

---

### P1-S02
Title: Ekspos Endpoint List & By-Reg Parity di WaitingListController BedUsage dan Pengujian Unit  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective:
Menghubungkan use case baru ke controller REST publik dan memperbarui unit test:
1. Pada [`WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/BedUsageContext/WaitingListFeature/WaitingListController.cs):
   - Tambahkan endpoint `[HttpGet]` yang menerima `[FromQuery] string? bangsalId` dan `[FromQuery] int? waitingListStatus`, mengirim `WaitingListListQuery`, dan mengembalikan `JSendOk(result)`.
   - Pastikan endpoint `[HttpGet("by-reg/{regId}")]` mengembalikan single object / null.
2. Tambahkan/perbarui unit test di `Bilreg.Test/BedUsageContext/WaitingListFeature/` (`WaitingListHandlerTest.cs` dan `WaitingListControllerTest.cs`) untuk memvalidasi list query, single by-reg, dan idempotensi create.

Depends On: P1-S01  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Endpoint `GET /api/bed-usage/waiting-list` dan `GET /api/bed-usage/waiting-list/by-reg/{regId}` siap menerima request.
- Seluruh pengujian unit di `Bilreg.Test/BedUsageContext/WaitingListFeature/` lulus 100%.

Implementation Notes:
- Menambahkan endpoint `[HttpGet]` pada `WaitingListController` yang menerima query string `bangsalId` dan `waitingListStatus`, mengeksekusi `WaitingListListQuery`, dan mengembalikan hasil terbungkus `JSendOk(result)`.
- Memverifikasi endpoint `[HttpGet("by-reg/{regId}")]` mengembalikan single object `WaitingListGetResponse` atau `null` terbungkus `JSendOk(result)`.
- Mempertahankan integritas seluruh rute mutasi dan worklist, termasuk `POST {id}/reject` untuk konsumsi Cockpit Bangsal (`wardCockpitApi.ts`).
- Menambahkan pengujian unit pada `WaitingListControllerTest.cs` untuk validasi `List` dengan parameter terisi maupun null, validasi `GetByReg` untuk kondisi ditemukan (single object) dan tidak ditemukan (null), serta validasi respons idempotent pendaftaran ganda pada `Create`.
- Menambahkan unit test UT22 pada `WaitingListHandlerTest.cs` untuk memvalidasi `WaitingListListQuery` dengan filter null.
- Seluruh 100 pengujian unit pada `Bilreg.Test/BedUsageContext/WaitingListFeature/` lulus 100%.

Changed Files:
- `src/bilreg/Bilreg.Api/Controllers/BedUsageContext/WaitingListFeature/WaitingListController.cs`
- `src/bilreg/Bilreg.Test/BedUsageContext/WaitingListFeature/WaitingListControllerTest.cs`
- `src/bilreg/Bilreg.Test/BedUsageContext/WaitingListFeature/WaitingListHandlerTest.cs`

Notes:
- Jangan mengubah route atau parameter yang sudah digunakan oleh `wardCockpitApi.ts` (`POST {id}/reject`).

---

## P2 - Admisi Ranap Backend Migration & Cleanup

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

---

### P2-S03
Title: Pengalihan Dependensi Orkestrator Admisi ke BedUsage dan Notifikasi Event Real-Time  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective:
Mengalihkan seluruh ketergantungan internal pada konteks Admisi Ranap ke `BedUsageContext`:
1. Pada [`AdmissionRegistrationOrchestrator.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs):
   - Alihkan injeksi `IWaitingListRepo` ke namespace `Bilreg.Application.BedUsageContext.WaitingListFeature.IWaitingListRepo`.
   - Pada metode `PlaceWaitingListGracefully`, buat entitas menggunakan model `Bilreg.Domain.BedUsageContext.WaitingListFeature.WaitingListModel`.
   - Injeksi `IWardCockpitEventPublisher` dan panggil `await _wardCockpitEventPublisher.PublishWaitingListUpdatedAsync(bangsalId);` setelah antrean tersimpan.
2. Pada `AdmCoordinatedCancelCmd.cs` dan `JourneyFeature` (`Release1JourneyStageResolver.cs`, `JourneyIntegrityValidator.cs`):
   - Ganti referensi `WaitingListStatusEnum` ke `Bilreg.Domain.BedUsageContext.WaitingListFeature.WaitingListStatusEnum`.
3. Perbarui konfigurasi DI di `Bilreg.Api/Configurations/InfrastructureService.cs`.
4. Perbarui unit test orkestrator di `Bilreg.Test/AdmisiRanapContext/` agar menggunakan mock BedUsage repo.

Depends On: P1-S02  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Pendaftaran admisi otomatis berhasil memanggil repository BedUsage.
- Event `PublishWaitingListUpdatedAsync` dipicu saat registrasi berhasil.
- Seluruh test di `Bilreg.Test/AdmisiRanapContext/` lulus tanpa referensi ke repo lama.

Notes:
- Pastikan blok *graceful fallback* (try-catch) pada `PlaceWaitingListGracefully` tetap membungkus proses agar kegagalan antrean tidak membatalkan registrasi pasien.

---

### P2-S04
Title: Penghapusan Total Modul dan Controller Lama di AdmisiRanapContext  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective:
Membersihkan seluruh sisa kode duplikat WaitingList di `AdmisiRanapContext`:
1. Hapus berkas controller [`Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs).
2. Hapus direktori `Bilreg.Domain/AdmisiRanapContext/WaitingListFeature/`.
3. Hapus direktori `Bilreg.Application/AdmisiRanapContext/WaitingListFeature/`.
4. Hapus direktori `Bilreg.Infrastructure/AdmisiRanapContext/WaitingListFeature/`.
5. Hapus direktori `Bilreg.Test/AdmisiRanapContext/WaitingListFeature/`.
6. Hapus pendaftaran DI lama di `InfrastructureService.cs`.

Depends On: P2-S03  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Tidak ada lagi direktori `WaitingListFeature` di dalam `AdmisiRanapContext`.
- Proyek `b09-bilreg-api` berhasil di-build (`dotnet build`) dengan 0 error dan seluruh test (`dotnet test`) lulus 100%.

Notes:
- Pastikan tidak ada namespace using yang tertinggal di berkas-berkas lain.

---

## P3 - Frontend Migration & UI Verification

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  

---

### P3-S05
Title: Pembaruan Endpoint Service, Pembersihan Dead-Code, dan Penyelarasan Schema di Frontend  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective:
Mengalihkan pemanggilan API dan menyelaraskan skema kontrak pada modul Admisi Ranap di `c012_myhospital_web`:
1. Di [`AdmisiRanapService.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/queries/AdmisiRanapService.ts):
   - Ubah `API_ENDPOINT.WAITING_LIST` dari `'admisi-ranap/waiting-list'` menjadi `'bed-usage/waiting-list'`.
   - Hapus fungsi `useCloseWaitingList()`.
2. Di [`admisiRanap.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/types/admisiRanap.ts):
   - Perbarui `waitingListStatusSchema` menjadi:
     `z.union([z.literal(0), z.literal(1), z.literal(2), z.literal(3), z.literal(4), z.literal(5)])`.
   - Hapus skema `closeWaitingListInputSchema` dan tipe `CloseWaitingListInput`.
3. Di [`admisiRanapStatus.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/configs/admisiRanapStatus.ts):
   - Perbarui `WAITING_LIST_STATUS_MAP` untuk menyertakan:
     - `1: { label: 'Dipesankan', variant: 'success' }`
     - `2: { label: 'Ditempatkan', variant: 'outline' }`
     - `4: { label: 'Dialihkan', variant: 'secondary' }`
4. Di `admisiRanapConsole.ts`:
   - Tambahkan opsi status `4` pada `WAITING_LIST_STATUS_FILTER_OPTIONS`.

Depends On: P1-S02  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Pemanggilan service antrean mengarah ke `api/bed-usage/waiting-list`.
- Zod schema validasi berhasil mem-parse status `0, 1, 2, 3, 4, 5`.
- Fungsi `useCloseWaitingList` bersih dari codebase frontend.

Notes:
- Pastikan `waitingListDetailSchema` dan `waitingListListResponseSchema` tetap cocok dengan JSON yang dikembalikan backend.

---

### P3-S06
Title: Verifikasi Komponen UI Admisi Ranap dan Validasi Kepatuhan SOP Outstanding  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective:
Memvalidasi integrasi antarmuka pengguna pada konsol Admisi Ranap:
1. Verifikasi [`DaftarTungguLane.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/lanes/DaftarTungguLane.vue):
   - Data antrean aktif termuat dengan benar melalui endpoint BedUsage.
   - Pasien berstatus `Placed (2)` dan `Cancelled (3)` tidak tampil di antrean aktif.
2. Verifikasi [`WaitingListDrawer.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/WaitingListDrawer.vue) & [`WaitingListFork.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/proses/WaitingListFork.vue):
   - Pembuatan antrean baru saat admisi dan perbaruan data antrean berjalan tanpa error.
   - Banner penolakan ruangan (`status 5`) tampil dengan alasan penolakan.
3. Validasi SOP Outstanding pada [`AdmisiRanapConsole.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue):
   - Saat `outstandingCount > 0`, konsol mengunci filter ke `Outstanding Only`.
   - Aksi registrasi baru diblokir (`isSopBlocked = true`) dan [`SopOutstandingAlertBanner.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/SopOutstandingAlertBanner.vue) tampil.
   - Tindak lanjut via [`DialogTindakLanjutOutstanding.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/DialogTindakLanjutOutstanding.vue) berhasil membuka blokir saat antrean beres.
4. Jalankan unit test frontend modul Admisi (`npm run test:unit`).

Depends On: P2-S04, P3-S05  
Repository: `c012_myhospital_web`  
Completion Criteria:
- Seluruh pengujian unit frontend pada modul Admisi Ranap lulus 100%.
- Alur pendaftaran admisi, mutasi antrean, dan alur SOP Outstanding berjalan mulus tanpa regresi (*zero-regression*).

Notes:
- Lakukan pengujian regresi menyeluruh pada komponen dialog dan workspace admisi ranap.

---

# 6. Change Log

- 2026-11-09: Dokumen perencanaan awal disusun dan disetujui (`Execution Approval: APPROVED`) oleh Architect (`ica-architect`).
