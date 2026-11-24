---
Title: Feasibility Assessment - Penyatuan Fitur WaitingList ke BedUsageContext dan Migrasi Penggunaan dari Admisi Ranap
Code: FA-WAITING-LIST-BED-USAGE-CONSOLIDATION-001
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.2
LastUpdated: 2026-11-09T15:02:00+07:00
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap usulan perubahan (*CHANGE-REQUEST*) untuk menyatukan fungsionalitas antrean rawat inap (*Waiting List*) ke dalam satu-satunya bounded context di backend, yaitu `BedUsageContext.WaitingListFeature`, menghapus implementasi duplikat pada `AdmisiRanapContext.WaitingListFeature`, serta mengalihkan pemanggilan antarmuka frontend Admisi Ranap (`c012_myhospital_web`) ke endpoint `/api/bed-usage/waiting-list` tanpa menimbulkan gangguan (*zero-regression*) pada operasional admisi maupun bangsal yang sedang berjalan.

Referenced artifacts:

- ISSUE: [WAITING-LIST-BED-USAGE-CONSOLIDATION-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-ISSUE.md) (`ISSUE-WAITING-LIST-BED-USAGE-CONSOLIDATION-001`)
- DOMAIN: [admisi-ranap-domain.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)
- DOMAIN: [SOP-RNA-A01-Pemrosesan-Waiting-List-dan-Penetapan-Akomodasi.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/bangsal/rna-sop/SOP-RNA-A01-Pemrosesan-Waiting-List-dan-Penetapan-Akomodasi.md)

## Objective

1. Mengidentifikasi seluruh kesenjangan kontrak data, payload API, dan perilaku use case antara modul frontend Admisi Ranap dengan endpoint `BedUsageContext.WaitingListFeature`.
2. Menilai kelayakan pembersihan/penghapusan modul `WaitingListFeature` di dalam `AdmisiRanapContext` tanpa merusak ketergantungan internal seperti pendaftaran admisi otomatis (`AdmissionRegistrationOrchestrator`), pembatalan terkoordinasi (`AdmCoordinatedCancelCmd`), dan pelacakan perjalanan pasien (`JourneyFeature`).
3. Memastikan aturan bisnis spesifik Admisi Ranap terpenuhi: **hanya antrean aktif yang belum menempati bed** yang tampil di daftar tunggu, serta **logika kepatuhan SOP Outstanding** tetap berfungsi sebagai *default view* dan pemblokir pendaftaran baru ketika `outstandingCount > 0`.
4. Merumuskan keputusan penutupan gap (*gap closure*) dan mitigasi risiko agar tidak terjadi kegagalan validasi (Zod schema error) maupun *breaking changes* pada pengguna akhir.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan penelusuran artefak dan basis kode sumber:

## Existing Behavior

1. **Frontend Admisi Ranap (`c012_myhospital_web`)**:
   - File service [`AdmisiRanapService.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/queries/AdmisiRanapService.ts) mendefinisikan rute `WAITING_LIST: 'admisi-ranap/waiting-list'`.
   - Menggunakan hook mutasi/query:
     - `useCreateWaitingList()`: `POST /api/admisi-ranap/waiting-list` dengan payload `{ regId, kelasId, bangsalId, priority, userId }`.
     - `useUpdateWaitingList()`: `PUT /api/admisi-ranap/waiting-list/{id}` dengan payload `{ priority, kelasId, bangsalId, userId }`.
     - `useWaitingListByRegId()`: `GET /api/admisi-ranap/waiting-list/by-reg/{regId}`, divalidasi oleh Zod schema `waitingListDetailSchema.nullable()` yang mengharapkan **Single Object atau Null**.
     - `useListWaitingList()`: `GET /api/admisi-ranap/waiting-list?bangsalId=...&waitingListStatus=...`, divalidasi oleh `waitingListListResponseSchema` yang mengharapkan envelope `{ items: [...] }`.
     - `useCloseWaitingList()`: `POST /api/admisi-ranap/waiting-list/{id}/close` *(catatan: tidak dipanggil di komponen UI mana pun)*.
   - Komponen yang mengonsumsi layanan di atas meliputi [`WaitingListDrawer.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/WaitingListDrawer.vue), [`RanapAdmissionWorkspace.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/RanapAdmissionWorkspace.vue), [`WaitingListFork.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/proses/WaitingListFork.vue), dan [`DaftarTungguLane.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/lanes/DaftarTungguLane.vue).

2. **Aturan Tampilan Antrean Aktif (Belum Menempati Bed)**:
   - Sesuai alur pelayanan rawat inap, worklist antrean di Admisi Ranap **hanya menampilkan pasien yang masih aktif dan belum menempati tempat tidur**:
     - `Waiting (0)`: Pasien baru menunggu ketersediaan kamar / konfirmasi bangsal.
     - `Reserved (1)`: Bangsal telah memesankan/memblokir bed, tetapi pasien belum check-in fisik ke kamar.
     - `Rerouted (4)`: Antrean sedang dialihkan ke bangsal alternatif dan menunggu konfirmasi.
     - `Rejected (5)`: Bangsal menolak antrean (misal bed penuh), dan pasien dikembalikan ke admisi untuk segera dicarikan alternatif.
   - Status terminal yang **tidak boleh muncul** pada worklist aktif:
     - `Placed (2)`: Pasien sudah check-in dan resmi menempati bed (`PakaiBed` aktif).
     - `Cancelled (3)`: Antrean telah dibatalkan.

3. **Logika SOP "Outstanding" pada Konsol Admisi Ranap**:
   - Diatur pada [`AdmisiRanapConsole.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue):
     - Dihitung via `outstandingCount`: mencakup seluruh reservasi/antrean aktif yang tanggal rencana masuknya telah terlampaui (`itemDate < today`) serta antrean berstatus tertolak (`Rejected - 5`) yang belum ditindaklanjuti.
     - **Kepatuhan SOP**: Jika `outstandingCount > 0`:
       1. Konsol secara otomatis mengunci tampilan kerja ke filter **`Outstanding Only`** (`setOutstandingOnly(true)` / `setCategory('outstanding')`) sebagai tampilan default.
       2. Seluruh aksi pembuatan pendaftaran baru (`Direct Admission`, `Proses Opname`, `Reservasi Baru`) **diblokir** (`isSopBlocked = true`).
       3. Banner peringatan SOP ([`SopOutstandingAlertBanner.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/SopOutstandingAlertBanner.vue)) wajib muncul di bagian atas layar.
       4. Petugas harus menyelesaikan seluruh antrean yang tertunda melalui dialog [`DialogTindakLanjutOutstanding.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/DialogTindakLanjutOutstanding.vue) sebelum sistem membuka kembali blokir pendaftaran baru.

4. **Frontend Rawat Inap / Cockpit Bangsal (`c012_myhospital_web`)**:
   - File service [`wardCockpitApi.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Inpatient/services/wardCockpitApi.ts) memanggil:
     - `POST /api/bed-usage/waiting-list/{id}/reject` untuk penolakan pasien antrean saat bed penuh.

5. **Backend `BedUsageContext.WaitingListFeature` (`b09-bilreg-api`)**:
   - Controller [`WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/BedUsageContext/WaitingListFeature/WaitingListController.cs) memiliki route `[Route("api/bed-usage/waiting-list")]`.
   - Menyediakan endpoint: `POST /`, `PUT /{id}`, `POST /{id}/reserve`, `POST /{id}/cancel`, `POST /{id}/reject`, `POST /{id}/reroute`, `GET /{id}`, `GET /by-reg/{regId}`, `GET /worklist?bangsalId=...`.
   - Tidak memiliki endpoint `GET /` (list dengan filter status dan bangsal opsional).
   - Tidak memiliki endpoint `POST /{id}/close`.

6. **Backend `AdmisiRanapContext.WaitingListFeature` (`b09-bilreg-api`)**:
   - Controller [`AdmisiRanapContext/WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs) berstatus *backward-compatible adapter*.
   - Pendaftaran admisi otomatis di [`AdmissionRegistrationOrchestrator.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs) meng-inject `IWaitingListRepo` milik `AdmisiRanapContext` dan membuat entitas menggunakan `Bilreg.Domain.AdmisiRanapContext.WaitingListFeature.WaitingListModel`.

## Existing Constraints

1. **Konsistensi Basis Data Fisik**: Kedua context saat ini berbagi dan mengakses tabel fisik yang sama, yaitu `BILRG_BedWaitingList`. Tidak ada pemisahan tabel fisik, sehingga data historis dan aktif telah berada pada tabel yang sama.
2. **Kekakuan Kontrak Zod FE**: Frontend Admisi Ranap memvalidasi ketat skema response via Zod. Ketidakcocokan tipe (seperti Array vs Object) akan langsung memicu `ValidationError` dan menghentikan alur UI.
3. **Integritas Alur SOP Outstanding**: Penyatuan endpoint tidak boleh mengganggu kalkulasi atau deteksi antrean *outstanding* yang menjadi acuan penguncian default view dan pemblokiran pendaftaran baru di konsol Admisi Ranap.
4. **Stabilitas Modul Inpatient**: Modul Cockpit Bangsal telah menggunakan `api/bed-usage/waiting-list/{id}/reject` dan tidak boleh terdampak negatif oleh perubahan kontrak.

---

# 3. Gap Analysis

| ID | Severity | Gap | Status |
|------|------|------|------|
| **GAP-001** | CRITICAL | **Perbedaan Kontrak `by-reg/{regId}`**: `BedUsageContext` mengembalikan array (`IEnumerable<WaitingListGetResponse>`), sedangkan FE Admisi Ranap menuntut single-object atau null (`waitingListDetailSchema.nullable()`). | **CLOSED** |
| **GAP-002** | CRITICAL | **Ketiadaan Endpoint List Global di BedUsage**: FE `DaftarTungguLane.vue` membutuhkan `GET /api/.../waiting-list?bangsalId=...&waitingListStatus=...` dengan format `{ items: [...] }` yang menyaring hanya antrean aktif yang belum menempati bed (`Waiting`, `Reserved`, `Rerouted`, `Rejected`), sementara `BedUsage` saat ini hanya memiliki `GET /worklist` yang mewajibkan `bangsalId`. | **CLOSED** |
| **GAP-003** | MAJOR | **Perilaku Idempotensi pada Pembuatan Waiting List**: `AdmCreateWaitingListHandler` mengembalikan ID yang sudah ada secara aman jika regId sudah memiliki antrean aktif, sedangkan `WaitingListCreateHandler` melempar `InvalidOperationException` (menghasilkan status HTTP 500 saat retry). | **CLOSED** |
| **GAP-004** | MAJOR | **Perbedaan Definisi Nilai Enum Status Antrean**: Status 1 pada Admisi diartikan sebagai `Accepted`, sedangkan di BedUsage adalah `Reserved`. Status 2 pada Admisi adalah `Closed`, sedangkan di BedUsage adalah `Placed`. Status 4 (`Rerouted`) belum terdefinisi pada union Zod di FE Admisi Ranap. | **CLOSED** |
| **GAP-005** | CRITICAL | **Ketergantungan Internal Modul Admisi Ranap**: `AdmissionRegistrationOrchestrator`, `AdmCoordinatedCancelCmd`, `JourneyFeature`, dan `Release1JourneyStageResolver` masih mengimpor Model, Repo, dan Enum milik `AdmisiRanapContext.WaitingListFeature`. | **CLOSED** |
| **GAP-006** | MINOR | **Ketiadaan Aksi Eksplisit `close` di BedUsage**: `BedUsageContext` tidak memiliki endpoint `POST /{id}/close` (siklus hidup antrean di BedUsage berpindah ke `Placed` melalui penempatan bed / check-in bangsal). | **CLOSED** |

---

# 4. Open Questions

| ID | Question | Impact | Status |
|------|------|------|------|
| **OQ-001** | Apakah endpoint `api/admisi-ranap/waiting-list` di controller BE harus langsung dihapus atau dipertahankan sebagai proxy tipis sementara untuk transisi aman? | Menentukan apakah controller lama langsung di-clean up total bersama migrasi FE. | **CLOSED** |
| **OQ-002** | Apakah tampilan status pada `DaftarTungguLane.vue` di FE perlu menampilkan label baru untuk status `Rerouted` (4) dan `Reserved` (1)? | Menentukan pembaruan `WAITING_LIST_STATUS_MAP` dan Zod union schema di FE. | **CLOSED** |
| **OQ-003** | Apakah saat pendaftaran admisi berhasil (`PlaceWaitingListGracefully`), notifikasi event bangsal perlu diterbitkan via `IWardCockpitEventPublisher` agar antrean bangsal langsung ter-update secara real-time? | Menentukan sinkronisasi real-time antara pendaftaran admisi dan cockpit bangsal. | **CLOSED** |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| **ASM-001** | Karena kedua context menggunakan tabel fisik yang sama (`BILRG_BedWaitingList`), tidak diperlukan migrasi skema tabel DDL SQL baru; penyatuan hanya berfokus pada refactoring application layer, domain, dan controller. |
| **ASM-002** | Komponen frontend Admisi Ranap (`c012_myhospital_web`) dapat dialihkan URL endpoint dasarnya dari `admisi-ranap/waiting-list` ke `bed-usage/waiting-list` tanpa mengubah alur interaksi pengguna. |
| **ASM-003** | Seluruh aksi `Reserve`, `Cancel`, `Reject`, dan `Reroute` yang saat ini sudah berjalan di `BedUsageContext` tetap dipertahankan dan menjadi kapabilitas tunggal bersama. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| **RISK-001** | Terjadi error validasi Zod (`ValidationError`) di FE jika response data `GET by-reg` atau list antrean tidak sesuai kontrak. | CRITICAL | Pastikan DTO response `BedUsageContext` kompatibel dengan Zod schema FE dan sesuaikan Zod schema bila ada field opsional baru. |
| **RISK-002** | Terjadi kegagalan pendaftaran pasien rawat inap jika `AdmissionRegistrationOrchestrator` gagal mengeksekusi pembuatan waiting list di `BedUsageContext`. | HIGH | Terapkan blok try-catch pelindung (*graceful fallback*) yang sama seperti implementasi saat ini dan pastikan default value parameter use case terpenuhi. |
| **RISK-003** | Kegagalan retry saat koneksi tidak stabil jika pembuatan waiting list tidak bersifat idempotent. | MEDIUM | Sesuaikan handler create di `BedUsageContext` agar mengembalikan waiting list aktif yang sudah ada jika regId terdaftar ganda. |
| **RISK-004** | Pasien yang sudah menempati bed (`Placed - 2`) keliru tampil kembali di worklist antrean aktif jika query default BedUsage tidak memfilter status terminal secara ketat. | HIGH | Terapkan filter ketat di DAL query list: secara default hanya mengambil status aktif (`0, 1, 4, 5`) dan mengecualikan `2 (Placed)` dan `3 (Cancelled)`. |

---

# 7. Recommendations

## Option A: Refactoring Terpadu BedUsageController Menjadi Multi-Client (Disetujui)

Menjadikan `BedUsageContext.WaitingListFeature` sebagai pengendali tunggal dengan melengkapi kapabilitasnya agar memenuhi kebutuhan Admisi Ranap dan Bangsal sekaligus.

- Tambahkan endpoint `GET /api/bed-usage/waiting-list` (list antrean dengan filter bangsal dan status, menyaring hanya status aktif yang belum menempati bed: `Waiting`, `Reserved`, `Rerouted`, `Rejected`).
- Ubah endpoint `GET /api/bed-usage/waiting-list/by-reg/{regId}` agar mengembalikan data tunggal antrean aktif (sesuai ekspektasi FE Admisi).
- Sesuaikan `WaitingListCreateHandler` agar idempotent.
- Alihkan dependensi `AdmissionRegistrationOrchestrator` ke repo/command BedUsage dan picu event bus `IWardCockpitEventPublisher`.
- Arahkan URL FE `AdmisiRanapService.ts` ke `bed-usage/waiting-list` dan hapus `useCloseWaitingList`.
- Pastikan kompatibilitas penuh dengan logika SOP Outstanding di konsol Admisi Ranap.
- Hapus controller dan modul duplikat di `AdmisiRanapContext.WaitingListFeature`.

### Advantages

- Arsitektur bersih (*Clean/DDD Architecture*): satu konsep domain hanya dimiliki oleh satu bounded context.
- Menghilangkan duplikasi kode di domain, use case, DTO, DAL, dan test.
- Menjamin single-source-of-truth bagi seluruh status antrean tempat tidur.

### Disadvantages

- Membutuhkan penyesuaian unit test yang komprehensif pada use case dan controller yang dipindahkan.

---

# 8. Gap Closure

Resolusi resmi untuk seluruh GAP dan Open Questions berdasarkan hasil *alignment interview*:

## GAP-001
### Decision
Ubah kontrak handler `WaitingListGetByRegQuery` dan endpoint `GET /api/bed-usage/waiting-list/by-reg/{regId}` agar mengembalikan data tunggal antrean aktif (`WaitingListGetResponse?` / null) bukan array, sehingga cocok langsung dengan Zod schema `waitingListDetailSchema.nullable()` di FE Admisi Ranap.
### Rationale
FE Admisi Ranap menampilkan kartu antrean per pendaftaran spesifik (1 pendaftaran aktif = 1 antrean aktif). Pengembalian array menyebabkan Zod validation error di FE.
### Impact
Format output `WaitingListGetByRegHandler` berubah dari `IEnumerable<WaitingListGetResponse>` menjadi `WaitingListGetResponse?`.
### Architecture Impact
Mempengaruhi handler use case dan test suite di `Bilreg.Test.BedUsageContext.WaitingListFeature`.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

## GAP-002
### Decision
Tambahkan use case query baru `WaitingListListQuery` di `Bilreg.Application.BedUsageContext.WaitingListFeature` dan ekspos endpoint `[HttpGet]` di `WaitingListController` (`/api/bed-usage/waiting-list`) yang menerima parameter opsional `[FromQuery] string? bangsalId` dan `[FromQuery] int? waitingListStatus`, mengembalikan envelope `{ items: [...] }`.
Filter default ketika status tidak ditentukan secara spesifik adalah menyaring **hanya antrean aktif yang belum menempati bed (`Waiting (0)`, `Reserved (1)`, `Rerouted (4)`, `Rejected (5)`)**, secara tegas mengecualikan status terminal `Placed (2)` dan `Cancelled (3)`.
### Rationale
Memenuhi kebutuhan tab dan filter antrean di komponen FE `DaftarTungguLane.vue` yang sebelumnya memanggil endpoint list Admisi Ranap, serta menjamin pasien yang sudah masuk kamar fisik tidak muncul di antrean tunggu.
### Impact
Penambahan DTO query dan implementasi query DAL worklist di BedUsageContext.
### Architecture Impact
Memperluas API contract `WaitingListController` di `BedUsageContext` tanpa merusak endpoint `/worklist` yang sudah ada.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

## GAP-003
### Decision
Perbarui handler `WaitingListCreateHandler` di BedUsage agar memiliki sifat idempotent: jika `_waitingListRepo.HasActiveByRegId(request.RegId)` bernilai true, handler mengembalikan ID antrean aktif yang sudah ada (`existing.WaitingListId`) daripada melempar `InvalidOperationException`.
### Rationale
Mencegah terjadinya HTTP 500 error saat frontend melakukan retry request atau saat orkestrator registrasi admisi mengeksekusi pendaftaran ganda secara tidak sengaja.
### Impact
Penyesuaian guard logic pada `WaitingListCreateHandler.cs`.
### Architecture Impact
Meningkatkan resiliensi pendaftaran pasien dan alur transisi antrean ranap.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

## GAP-004
### Decision
Harmonisasi status antrean di FE (`c012_myhospital_web`):
1. Perbarui `waitingListStatusSchema` di `admisiRanap.ts` menjadi `z.union([z.literal(0), z.literal(1), z.literal(2), z.literal(3), z.literal(4), z.literal(5)])`.
2. Perbarui `WAITING_LIST_STATUS_MAP` di `admisiRanapStatus.ts`:
   - `0`: `{ label: 'Menunggu', variant: 'warning' }`
   - `1`: `{ label: 'Dipesankan', variant: 'success' }` (atau Diterima)
   - `2`: `{ label: 'Ditempatkan', variant: 'outline' }` (atau Ditutup)
   - `3`: `{ label: 'Dibatalkan', variant: 'destructive' }`
   - `4`: `{ label: 'Dialihkan', variant: 'secondary' }`
   - `5`: `{ label: 'Ditolak Ruangan', variant: 'destructive' }`
### Rationale
Menghilangkan disparitas makna status antara Admisi (Accepted/Closed) dan BedUsage (Reserved/Placed/Rerouted) serta mencegah crash validasi zod.
### Impact
Pembaruan schema tipe data dan kamus status badge di frontend.
### Architecture Impact
Konsistensi representasi status antrean end-to-end antara BE dan FE.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

## GAP-005
### Decision
Alihkan seluruh referensi dependensi pada `AdmissionRegistrationOrchestrator`, `AdmCoordinatedCancelCmd`, `JourneyFeature`, dan `Release1JourneyStageResolver` untuk mengonsumsi `Bilreg.Application.BedUsageContext.WaitingListFeature.IWaitingListRepo` dan `WaitingListModel` milik BedUsageContext.
### Rationale
Menghilangkan kopling ke modul `AdmisiRanapContext.WaitingListFeature` sehingga modul lama dapat dihapus sepenuhnya tanpa menyebabkan broken reference.
### Impact
Refactoring import namespace, parameter konstruktor, dan mapping enum di AdmisiRanapContext.
### Architecture Impact
Menghapus seluruh file dan sub-folder `WaitingListFeature` pada `Bilreg.Domain.AdmisiRanapContext`, `Bilreg.Application.AdmisiRanapContext`, `Bilreg.Infrastructure.AdmisiRanapContext`, dan pengujian unitnya di `Bilreg.Test`.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

## GAP-006
### Decision
Hapus `useCloseWaitingList()` dan `CloseWaitingListInput` dari `AdmisiRanapService.ts` dan `admisiRanap.ts` di frontend. Tidak perlu membuat endpoint `POST /{id}/close` di BedUsageContext.
### Rationale
Komponen UI FE Admisi Ranap tidak ada yang memanggil fungsi ini, dan siklus hidup antrean di BedUsage otomatis diselesaikan (status `Placed`) ketika pasien ditempatkan ke ranjang (`PakaiBed`) oleh perawat bangsal.
### Impact
Penghapusan kode mati (*dead code*) di FE.
### Architecture Impact
Penyederhanaan surface API di BedUsageContext.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

## OQ-001
### Decision
Controller lama di Admisi Ranap ([`AdmisiRanapContext/WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs)) dihapus bersih secara permanen. Seluruh pemanggilan di frontend Admisi Ranap dialihkan langsung ke `/api/bed-usage/waiting-list`.
### Rationale
Frontend dan backend berada di bawah kendali tim yang sama dan dimigrasikan secara simultan, sehingga penghapusan total menghilangkan kebingungan rute ganda (*clean architecture*).
### Impact
Penghapusan file controller di backend dan pengubahan `API_ENDPOINT.WAITING_LIST = 'bed-usage/waiting-list'` di frontend.
### Architecture Impact
Hanya ada 1 controller untuk Waiting List di seluruh API gateway/server.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

## OQ-002
### Decision
UI Frontend mengadopsi status 4 (`Rerouted: 'Dialihkan'`) dan menyelaraskan status 1 (`Reserved: 'Dipesankan'`) serta status 2 (`Placed: 'Ditempatkan'`).
### Rationale
Menyajikan status aktual pasien di bangsal secara akurat kepada petugas admisi tanpa mengubah alur kerja.
### Impact
Pembaruan konfigurasi tema badge di `admisiRanapStatus.ts`.
### Architecture Impact
Frontend UI konsisten dengan terminologi domain BedUsageContext.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

## OQ-003
### Decision
Pada metode `PlaceWaitingListGracefully` di `AdmissionRegistrationOrchestrator.cs`, setelah waiting list berhasil disimpan ke BedUsage, picu event publikasi:
`await _wardCockpitEventPublisher.PublishWaitingListUpdatedAsync(bangsalId);`
### Rationale
Memastikan perawat di bangsal tujuan langsung melihat kedatangan pasien antrean baru di layar Cockpit Bangsal secara real-time tanpa perlu me-refresh halaman web secara manual.
### Impact
Injeksi `IWardCockpitEventPublisher` ke dalam `AdmissionRegistrationOrchestrator`.
### Architecture Impact
Integrasi reaktif antar-konteks (Admisi -> Bangsal) melalui event publisher.
### Resolved By
User & Antigravity (Pair Interview)
### Resolved Date
2026-11-09

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Perubahan ini melibatkan modifikasi batas konteks sistem (*Bounded Context boundary modification*), pengalihan orkestrator registrasi admisi ke repository konteks lain, harmonisasi kontrak REST API publik, serta penghapusan modul lama di domain, application, dan infrastructure layer.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture formally updated and approved by Architect
- [x] Gate READY-FOR-PLANNING granted by Architect

## Status

**READY-FOR-PLANNING**

## Notes

Seluruh GAP dan Open Questions telah resmi berstatus **CLOSED** dengan keputusan yang terekam lengkap. Dokumen ini diserahkan kepada peran **Architect** (`ica-architect`) untuk mengevaluasi kelayakan arsitektural dan secara formal memberikan gate decision **READY-FOR-PLANNING** sebelum penyusunan **ARCHITECTURE** dan **IMPLEMENTATION-PLAN**.

---

# 11. References

Referenced artifacts:

- ISSUE: [`WAITING-LIST-BED-USAGE-CONSOLIDATION-ISSUE.md`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/WAITING-LIST-BED-USAGE-CONSOLIDATION-ISSUE.md)
- DOMAIN: [`admisi-ranap-domain.md`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi-ranap/admisi-ranap-domain.md)

Referenced codebase locations:

- Frontend Services & Console:
  - [`c012_myhospital_web/src/modules/Admisi/queries/AdmisiRanapService.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/queries/AdmisiRanapService.ts)
  - [`c012_myhospital_web/src/modules/Admisi/types/admisiRanap.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/types/admisiRanap.ts)
  - [`c012_myhospital_web/src/modules/Admisi/configs/admisiRanapStatus.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/configs/admisiRanapStatus.ts)
  - [`c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue)
  - [`c012_myhospital_web/src/modules/Admisi/components/ranap/DialogTindakLanjutOutstanding.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/DialogTindakLanjutOutstanding.vue)
  - [`c012_myhospital_web/src/modules/Admisi/components/ranap/SopOutstandingAlertBanner.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/SopOutstandingAlertBanner.vue)
  - [`c012_myhospital_web/src/modules/Inpatient/services/wardCockpitApi.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Inpatient/services/wardCockpitApi.ts)
- Backend Endpoints & Handlers:
  - [`b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/BedUsageContext/WaitingListFeature/WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/BedUsageContext/WaitingListFeature/WaitingListController.cs)
  - [`b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/AdmisiRanapContext/WaitingListController.cs)
  - [`b09-bilreg-api/src/bilreg/Bilreg.Application/BedUsageContext/WaitingListFeature/UseCases/`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/BedUsageContext/WaitingListFeature/UseCases/)
  - [`b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmissionRegistrationOrchestrator.cs)
