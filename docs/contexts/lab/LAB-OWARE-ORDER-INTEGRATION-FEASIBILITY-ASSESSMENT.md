---
Title: Integrasi Pengiriman Order Pemeriksaan Laboratorium ke OWARE Feasibility Assessment
Code: LAB-OWARE-ORDER-INTEGRATION
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-10-29
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan teknis implementasi integrasi pengiriman data order pemeriksaan laboratorium dari Laboratory Workflow Feature (LWF) pada backend `b09-bilreg-api` ke OWARE order middleware (`a046_oware_ordermiddlewareapi`) untuk diteruskan ke Laboratory Information System (LIS) atau analyzer diagnostik rumah sakit.

Referenced artifacts:
- ISSUE: [LAB-OWARE-ORDER-INTEGRATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-INTEGRATION-ISSUE.md)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- INTEGRATION SPEC: [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md)
- WORKFLOW SPEC: [lab-workflow.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-workflow.md)
- RELATED OUTCOMES: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md), [OC-LAB-SAMPLE-COLLECTION.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-SAMPLE-COLLECTION.md)

## Objective

Menilai kesiapan sistem, mengidentifikasi celah teknis (*gaps*), menganalisis dampak integrasi lintas-service antara `b09-bilreg-api` dan `a046_oware_ordermiddlewareapi`, serta merumuskan keputusan penutupan celah (*gap closure decisions*) yang telah disepakati melalui sesi penajaman (*grill-me*) sebelum diserahkan ke peran **Architect** (`ica-architect`).

---

# 2. Current State

## Existing Behavior

### 1. Sistem Pengirim: `b09-bilreg-api` (LWF - `LabOwareFeature` & `LabOrderFeature`)
- **Domain Layer**:
  - Agregat `LabOrderModel` telah memiliki properti `OwareStatus` berbasis enum `OwareStatusEnum` (`Pending = 0`, `Sent = 1`, `Failed = 2`).
  - `LabOrderModel` memiliki guard method `EnsureCanEnqueueOware()` yang mengunci status order agar tidak berstatus `Ordered`, `Deferred`, `Cancelled`, atau `Terminated`. Enqueue diizinkan pada status `Charged` atau sesudahnya.
  - Terdapat model antrean outbound `LabOwareOutboundQueueModel` yang dikelola secara persisten dengan status `LabOwareQueueStatusEnum` (`Pending = 0`, `Processing = 1`, `Succeeded = 2`, `Failed = 3`), mencakup kolom `RetryCount`, `LastRetryDate`, `ProcessedDate`, `LastError`, dan `PayloadJson`.
- **Application Layer**:
  - Terdapat query detail order [`LabOrderGetQuery.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs) yang mengembalikan `LabOrderGetResponse`, telah lengkap memuat identitas pasien, nomor registrasi, dokter, unit layanan, catatan klinis, serta rincian item pemeriksaan (`Items`) yang menyertakan `TarifId`, `TarifCode`, `TarifName`, `LabTestCode`, `LabTestName`, jenis tabung, dan spesimen.
  - Use case enqueue: `LabOwareQueueEnqueueCmd` menerima `OrderId` dan `UserId`, memvalidasi status via `EnsureCanEnqueueOware()`, menyimpan antrean baru (`CreatePending`), dan memutasi status order ke `Pending`.
  - Use case processing: `LabOwareQueueProcessor` dan `LabOwareQueueProcessCmd` memuat batch antrean berstatus `Pending`/`Failed`, menandai `Processing`, memanggil interface `ILabOwareIntegration.Send(payloadJson)`, dan memutasi status antrean (`Succeeded`/`Failed`) serta status order (`MarkOwareSent`/`MarkOwareFailed`).
  - Use case manual retry: `LabOwareRetryCmd` memungkinkan operator mengulang pengiriman antrean yang berstatus `Failed`.
- **Infrastructure & API Layer**:
  - Tabel SQL `BILRG_LabOwareOutboundQueue` telah didefinisikan lengkap dengan indeks `(QueueStatus, CrtDate)`.
  - Repositori dan DAL (`LabOwareOutboundQueueRepo`, `LabOwareOutboundQueueDal`) telah tersedia.
  - Controller `LabOwareController` mengekspos endpoint HTTP untuk `enqueue`, `retry`, `process`, dan `worklist`.
  - Controller `LabOrderController` mengekspos endpoint detail order: `GET /api/LabContext/LabOrderFeature/{orderId}` (dilindungi anotasi `[Authorize]`).
  - **Kondisi Kritis**: Implementasi adapter [`LabOwareIntegration.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabOwareIntegration.cs) saat ini **masih berupa mock in-memory simulasi** yang hanya mendeteksi kata `"FAIL"` dalam payload dan belum melakukan pemanggilan network/HTTP riil ke OWARE.

### 2. Sistem Penerima: `a046_oware_ordermiddlewareapi` (OWARE Middleware)
- **Routing & Dispatching**:
  - OWARE memiliki komponen hub [`OrderLabHubService`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/OrderLabHubService.cs) yang mengorkestrasi pengiriman order ke berbagai vendor LIS/analyzer (Sysmex, Prolims, Premier, Softmedix, Berliando, dsb.) berdasarkan konfigurasi `ConsId` / `OrderType`.
  - Pemetaan tarif RS ke LIS dikelola melalui tabel master `OWARE_MapTarifLab` (`IMapTarifLabDal`) berbasis `TarifId` → `ConsId` dan kode tes LIS.
- **Existing Endpoints & Builders**:
  - Controller [`OrderController.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/OrderController.cs) memiliki endpoint `POST /api/order/lab` yang menangani `CreateOrderLabCommand(string TindakanId)` berbasis tabel legacy `BIL_Tindakan`.
  - Terdapat endpoint `POST /api/order/cancel` (`CancelOrderCommand`) untuk meneruskan pembatalan ke vendor LIS.
  - **Kondisi Kritis**: OWARE belum memiliki endpoint maupun command handler untuk menerima notifikasi order dari LWF (`b09-bilreg-api`) serta belum memiliki klien untuk melakukan penarikan data detail order dari API Bilreg.

## Existing Constraints

1. **Prinsip Arsitektur Asynchronous**: Sesuai dokumen [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md) (§6 & §11), integrasi LWF ke OWARE bersifat asynchronous via queue. Gangguan pada OWARE atau server LIS tidak boleh memblokir transaksi klinis pelayanan laboratorium di LWF.
2. **Kedaulatan Otoritas Operasional LWF**: LWF adalah sumber kebenaran operasional (*operational source of truth*). OWARE murni bertindak sebagai integration bridge / middleware dan tidak boleh mengubah alur bisnis atau status klinis pasien di luar pelaporan status pengiriman (`OwareStatus`).
3. **Pemisahan Boundary Antar-Service**: `b09-bilreg-api` dan `a046_oware_ordermiddlewareapi` merupakan bounded context dan repository terpisah. Komunikasi antar-keduanya dilakukan murni via REST/HTTP API dan DTO eksplisit, tanpa percampuran database schema atau direct query.
4. **Pola Notifikasi Ringan & Penarikan Data (Notification & Pull Pattern)**: Pengiriman notifikasi ke OWARE cukup berupa identifier (`OrderId`), dan OWARE bertugas menarik detail data yang dibutuhkan ke LWF via API resmi.

---

# 3. Gap Analysis

| ID | Severity | Status | Gap Description |
|---|---|---|---|
| **GAP-001** | CRITICAL | CLOSED | **Implementasi Adapter Riil pada `ILabOwareIntegration` Belum Ada**: [`LabOwareIntegration.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabOwareIntegration.cs) di `b09-bilreg-api` masih berupa kelas mock in-memory. Belum tersedia klien HTTP (`HttpClient`) untuk mengeksekusi request jaringan riil ke endpoint OWARE. |
| **GAP-002** | CRITICAL | CLOSED | **Ketidaksesuaian Kontrak Endpoint Penerimaan pada OWARE**: `POST /api/order/lab` pada OWARE hanya menerima `TindakanId` dan menarik data dari tabel warisan `BIL_Tindakan`. OWARE belum memiliki endpoint khusus untuk menerima order LWF kanonikal. |
| **GAP-003** | MAJOR | CLOSED | **Ketiadaan Konfigurasi Integrasi OWARE di `b09-bilreg-api`**: Belum terdapat entri konfigurasi pada `appsettings.json` (seperti `BaseUrl`, `OrderLabUrl`, `TimeoutSeconds`, dan batas percobaan retry) serta belum ada registrasi `HttpClient` terisolasi pada DI container. |
| **GAP-004** | MAJOR | CLOSED | **Enqueue Masih Bergantung pada Triger Eksternal/Manual**: `LabOwareQueueEnqueueCmd` saat ini hanya terpapar via HTTP endpoint dan belum terintegrasi secara otomatis saat order berhasil di-charge (`Charged`). |
| **GAP-005** | MAJOR | CLOSED | **Ketiadaan Background Worker Otomatis untuk Antrean**: Proses pemrosesan antrean (`LabOwareQueueProcessor.ProcessBatch`) hanya dapat dijalankan jika endpoint `POST /api/LabContext/LabOwareFeature/process` dipanggil dari luar. |
| **GAP-006** | MINOR | CLOSED | **Pemetaan Vendor LIS Menggunakan TarifId**: OWARE membutuhkan `TarifId` untuk mencari `ConsId` (Sysmex, Prolims, dsb.) pada tabel `OWARE_MapTarifLab`. Penarikan data detail order dari Bilreg harus memastikan `TarifId` disertakan. |
| **GAP-007** | MAJOR | CLOSED | **Otomatisasi Pembatalan Order ke OWARE**: Belum ada mekanisme otomatis di LWF yang memicu antrean pembatalan (`MessageType = "LabOrderCancel"`) ke OWARE saat order yang sudah dikirim (`Sent`) dibatalkan. |

---

# 4. Open Questions

| ID | Status | Question | Impact |
|---|---|---|---|
| **OQ-001** | CLOSED | Kapan titik pemicu (*trigger point*) otomatis pembentukan antrean OWARE (`Enqueue`) dieksekusi dalam alur kerja LWF? | Menentukan apakah order dikirim saat `Charged` atau `Collected`, serta penanganan order Cito. |
| **OQ-002** | CLOSED | Apa rute endpoint baru di OWARE untuk menerima order dari Bilreg? | Menghindari konflik dengan endpoint lama berbasis `TindakanId`. |
| **OQ-003** | CLOSED | Bagaimana arsitektur dan format payload dari Bilreg ke OWARE, serta bagaimana OWARE melengkapi detail order? | Menentukan apakah data dikirim sekaligus (*push full payload*) atau notifikasi ringan (*notification + pull*). |
| **OQ-004** | CLOSED | Bagaimana mekanisme background worker di `b09-bilreg-api` untuk memproses antrean? | Menentukan eksekusi proses background dan ketersediaan manual retry. |
| **OQ-005** | CLOSED | Bagaimana metode autentikasi komunikasi dua arah antar-service (Bilreg → OWARE dan OWARE → Bilreg)? | Menentukan header auth, token login JWT, dan konfigurasi kredensial service account. |
| **OQ-006** | CLOSED | Bagaimana penanganan pembatalan order di LWF terhadap OWARE? | Menentukan apakah notifikasi pembatalan otomatis dikirim ke OWARE/LIS. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| **ASM-001** | Service `b09-bilreg-api` dan `a046_oware_ordermiddlewareapi` terhubung dalam jaringan internal privat rumah sakit (LAN/intranet) sehingga OWARE dapat diakses secara lokal tanpa autentikasi ketat. |
| **ASM-002** | Endpoint `GET /api/LabContext/LabOrderFeature/{orderId}` di `b09-bilreg-api` sudah menyediakan seluruh data yang dibutuhkan OWARE, termasuk `TarifId` pada setiap baris item pemeriksaan. |
| **ASM-003** | Service account untuk OWARE tersedia atau dapat didaftarkan pada modul User Bilreg untuk memperoleh Token Bearer JWT melalui `/api/User/login`. |
| **ASM-004** | Skema tabel database `BILRG_LabOwareOutboundQueue` yang ada saat ini sudah memadai untuk menampung seluruh status, retry count, dan pesan kesalahan tanpa perubahan skema tabel besar. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| **RISK-001** | Server OWARE atau mesin LIS mengalami downtime atau kegagalan jaringan berkepanjangan. | Antrean `BILRG_LabOwareOutboundQueue` berstatus `Failed` dan pengiriman tertunda. | LWF mengisolasi kegagalan secara asinkron; alur klinis tidak terhenti. Tersedia mekanisme auto-retry oleh background worker dan tombol manual "Kirim ke LIS/OWARE" di antarmuka kerja. |
| **RISK-002** | Token Bearer JWT dari OWARE ke Bilreg kedaluwarsa (*expired*) saat OWARE menarik detail order. | Pemanggilan GET detail order gagal dengan HTTP 401 Unauthorized. | OWARE mengimplementasikan token cache dengan mekanisme re-login otomatis saat menerima HTTP 401. |
| **RISK-003** | Tarif tindakan pada order LWF belum dipetakan ke vendor LIS pada tabel `OWARE_MapTarifLab`. | OWARE tidak dapat menentukan `ConsId` (unmapped vendor). | OWARE mencatat log error pada `OWARE_OrderLog` dan mengembalikan respons kegagalan yang deskriptif ke Bilreg sehingga tercatat di `LastError` antrean. |

---

# 7. Recommendations

Rekomendasi pendekatan teknis untuk mengatasi celah integrasi pengiriman order lab ke OWARE.

## Option A: Pola Notification & Pull via Dedicated Endpoint (Recommended)

1. Pada `b09-bilreg-api`: Saat order berhasil di-charge (`Charged`), LWF memasukkan antrean outbound dengan payload notifikasi minimalis: `{ "orderId": "..." }`.
2. Background worker di `b09-bilreg-api` memproses antrean dan memanggil endpoint baru OWARE: `POST /api/order/lab/bilreg` tanpa autentikasi khusus.
3. OWARE menerima `orderId`, lalu mengeksekusi HTTP GET ke API resmi Bilreg: `GET /api/LabContext/LabOrderFeature/{orderId}` menggunakan Token Bearer JWT untuk menarik data order lengkap.
4. OWARE memetakan item pemeriksaan berdasarkan `TarifId` ke vendor LIS (`ConsId`) via tabel `OWARE_MapTarifLab` yang sudah ada, menyimpan order ke `OWARE_Order`, dan mendispatch ke `OrderLabHubService`.
5. Pembatalan order di LWF otomatis mengirimkan notifikasi pembatalan ke endpoint cancel OWARE jika order sebelumnya berstatus `Sent`.

### Advantages

- Payload notifikasi dari LWF sangat ringan, menghemat bandwidth dan mengurangi kompleksitas serialisasi di LWF.
- Menjamin *single source of truth*: OWARE selalu menarik data terbaru langsung dari endpoint resmi detail order di Bilreg.
- Endpoint baru di OWARE (`/api/order/lab/bilreg`) menjamin zero regression terhadap RS client lama yang masih memakai format `TindakanId`.
- Memanfaatkan tabel pemetaan `OWARE_MapTarifLab` yang sudah ada tanpa perlu membuat tabel pemetaan baru.

### Disadvantages

- Memerlukan mekanisme login dan pengelolaan token JWT di OWARE untuk mengakses API Bilreg yang dilindungi `[Authorize]`.
- Terjadi round-trip HTTP tambahan (Bilreg POST notifikasi ke OWARE, lalu OWARE GET detail ke Bilreg).

## Option B: Pola Direct Full Payload Push

1. LWF membentuk model data komprehensif (`LabOwareOutboundPayload`) berisi data pasien, tabung, spesimen, dan seluruh rincian tes lengkap dengan `TarifId`.
2. LWF mengirimkan seluruh payload lengkap dalam sekali request HTTP POST ke OWARE.
3. OWARE langsung mendispatch ke vendor LIS tanpa perlu melakukan pemanggilan balik (*pulling*) ke Bilreg.

### Advantages

- Tidak ada round-trip balik dari OWARE ke Bilreg.
- OWARE tidak memerlukan service account login JWT ke Bilreg.

### Disadvantages

- Payload JSON di antrean LWF berukuran besar.
- Jika ada perubahan struktur data pasien atau master di masa depan, kontrak payload push harus terus disinkronkan secara ketat di kedua belah pihak.

## Option C: Distributed Message Broker (RabbitMQ)

1. LWF mempublikasikan event `LabOrderChargedEvent` ke message broker RabbitMQ.
2. OWARE bertindak sebagai consumer yang mendengarkan event dan meneruskannya ke vendor LIS.

### Advantages

- Decoupling sangat tinggi antar-service.

### Disadvantages

- Memerlukan instalasi dan pemeliharaan infrastruktur message broker baru khusus untuk jalur LWF-OWARE.
- Memerlukan perombakan besar pada struktur tabel antrean `BILRG_LabOwareOutboundQueue` dan prosesor yang sudah terlanjur dibangun berbasis persistent SQL.

---

# 8. Gap Closure

## GAP-001 (Implementasi Adapter Riil pada `ILabOwareIntegration`)
### Decision
Mengganti mock `LabOwareIntegration` di `Bilreg.Infrastructure` dengan implementasi `HttpClient` riil yang memanggil endpoint OWARE `POST /api/order/lab/bilreg` dengan payload `{ "orderId": "..." }`, tanpa header autentikasi khusus (anotasi di OWARE mengizinkan akses internal).
### Rationale
Mock in-memory tidak menghubungkan transaksi ke service OWARE. Payload minimalis mengurangi beban serialisasi di LWF.
### Impact
Penambahan `LabOwareHttpService` di `Bilreg.Infrastructure` dan registrasi via `IHttpClientFactory` di `Bilreg.Api`.
### Architecture Impact
Mendefinisikan komponen outbound HTTP Client pada arsitektur integrasi infrastruktur LWF.
### Resolved By
User & ica-analyst (sesi grill-me)
### Resolved Date
2026-10-29

---

## GAP-002 & OQ-002 (Endpoint Penerimaan di OWARE)
### Decision
Menambahkan endpoint baru di OWARE: `POST /api/order/lab/bilreg` pada `OrderController.cs` yang menerima model request `{ "orderId": "string" }` tanpa memerlukan autentikasi.
### Rationale
Menjamin zero regression pada RS client lama yang masih menggunakan `POST /api/order/lab` berbasis `TindakanId`.
### Impact
Penambahan endpoint, Command, dan Handler baru di `a046_oware_ordermiddlewareapi`.
### Architecture Impact
Menetapkan kontrak endpoint inter-service OWARE khusus penerimaan order Bilreg.
### Resolved By
User & ica-analyst (sesi grill-me)
### Resolved Date
2026-10-29

---

## GAP-003 (Konfigurasi Integrasi di `b09-bilreg-api`)
### Decision
Menambahkan section konfigurasi `OwareOptions` pada `appsettings.json` di `b09-bilreg-api` memuat:
- `BaseUrl`: URL host OWARE (misal `http://localhost:5255` atau IP server OWARE).
- `OrderLabEndpoint`: rute endpoint (`/api/order/lab/bilreg`).
- `TimeoutSeconds`: batas waktu HTTP timeout (default 10 detik).
### Rationale
Memisahkan konfigurasi URL antar lingkungan (development, staging, production) secara bersih.
### Impact
Penambahan options class di `Bilreg.Infrastructure` dan konfigurasi di `appsettings.json`.
### Architecture Impact
Standardisasi konfigurasi koneksi eksternal di lapisan infrastruktur.
### Resolved By
User & ica-analyst (sesi grill-me)
### Resolved Date
2026-10-29

---

## GAP-004 & OQ-001 (Titik Pemicu Otomatis Enqueue Antrean)
### Decision
Pemicu otomatis pembentukan antrean OWARE (`Enqueue`) dieksekusi **hanya saat status order berhasil menjadi `Charged` (penagihan sukses)** di dalam `LabOrderChargeHandler`. Tidak ada pengiriman update susulan saat sampling (`Collected`). Untuk semua jenis pemeriksaan, termasuk Cito, tetap wajib berstatus `Charged` sebelum dikirim ke OWARE. Jika pengiriman otomatis sebelumnya gagal (`Failed`), sistem menyediakan tombol manual "Kirim ke LIS/OWARE" di antarmuka kerja laboratorium.
### Rationale
Penagihan di LWF terjadi langsung saat order diproses laboratorium. Memastikan keteraturan workflow keuangan sebelum order masuk analitik, sekaligus menyediakan mekanisme recovery manual yang handal bagi petugas lab.
### Impact
Modifikasi `LabOrderChargeHandler` untuk memicu enqueue secara in-process, dan penyesuaian endpoint retry/enqueue manual di UI.
### Architecture Impact
Orkestrasi use case `Charge` ke antrean `LabOwareOutboundQueue`.
### Resolved By
User & ica-analyst (sesi grill-me)
### Resolved Date
2026-10-29

---

## GAP-005 & OQ-004 (Mekanisme Background Worker Otomatis)
### Decision
Menerapkan pendekatan **Hybrid**:
1. Menambahkan .NET `BackgroundService` (`LabOwareOutboundWorker`) di host `Bilreg.Api` yang berjalan otomatis di latar belakang secara periodik (interval configurable via `appsettings.json`, misal tiap 5-10 detik) untuk memanggil `LabOwareQueueProcessor.ProcessBatch`.
2. Tetap mempertahankan endpoint HTTP `POST /process` dan `PATCH /retry` di `LabOwareController` untuk kebutuhan trigger manual dari antarmuka web.
### Rationale
Otomatisasi pengiriman berjalan mulus tanpa ketergantungan scheduler eksternal, namun operator tetap memiliki kontrol interaktif langsung saat melakukan retry manual.
### Impact
Penambahan class `BackgroundService` di `Bilreg.Api` dan registrasi di `Program.cs`.
### Architecture Impact
Penambahan komponen hosted service background worker pada diagram arsitektur runtime.
### Resolved By
User & ica-analyst (sesi grill-me)
### Resolved Date
2026-10-29

---

## GAP-006 & OQ-003 (Penarikan Detail Order & Pemetaan Vendor di OWARE)
### Decision
OWARE menerima notifikasi `{ "orderId": "..." }`, lalu mengeksekusi HTTP GET ke `b09-bilreg-api`: `GET /api/LabContext/LabOrderFeature/{orderId}`. Dari payload detail order `LabOrderGetResponse`, OWARE mengambil `TarifId` dari masing-masing baris item pemeriksaan dan memetakan ke vendor LIS (`ConsId`) menggunakan tabel `OWARE_MapTarifLab` yang sudah ada, lalu mendispatch ke `OrderLabHubService`.
### Rationale
Sesuai arahan spesifik user: LWF tidak perlu mengirim full payload yang rumit; OWARE mengambil detail dari API resmi Bilreg yang sudah memuat `TarifId` lengkap.
### Impact
Penambahan HTTP client service di OWARE untuk mengambil data dari Bilreg dan memetakan ke `OrderLabModel`.
### Architecture Impact
Menjadikan API GET LabOrder sebagai sumber resmi pembentukan model order di OWARE.
### Resolved By
User & ica-analyst (sesi grill-me)
### Resolved Date
2026-10-29

---

## OQ-005 (Autentikasi Komunikasi Antar-Service)
### Decision
1. **Bilreg → OWARE (`POST /api/order/lab/bilreg`)**: Diakses tanpa autentikasi (anotasi controller tidak memerlukan `[Authorize]`).
2. **OWARE → Bilreg (`GET /api/LabContext/LabOrderFeature/{orderId}`)**: Menggunakan Token Bearer JWT. OWARE melakukan login ke `/api/User/login` di Bilreg menggunakan kredensial service account yang dikonfigurasi di `appsettings.json` OWARE, menyimpan token di memory cache, dan menyertakannya pada header `Authorization: Bearer <token>`.
### Rationale
Memenuhi aturan keamanan Bilreg di mana seluruh controller dilindungi `[Authorize]`, sambil menjaga kesederhanaan akses ke OWARE dalam jaringan privat internal.
### Impact
Penambahan token management service di OWARE (login & token caching).
### Architecture Impact
Mendefinisikan mekanisme autentikasi dan otorisasi komunikasi antar-service.
### Resolved By
User & ica-analyst (sesi grill-me)
### Resolved Date
2026-10-29

---

## GAP-007 & OQ-006 (Otomatisasi Pembatalan Order ke OWARE)
### Decision
Ketika order laboratorium dibatalkan di LWF (`LabOrderCancelCmd`), jika order tersebut sebelumnya sudah pernah berhasil terkirim ke OWARE (`OwareStatus == Sent`), handler secara otomatis memasukkan antrean pembatalan baru ke `BILRG_LabOwareOutboundQueue` dengan `MessageType = "LabOrderCancel"`. Worker akan meneruskan pembatalan ini ke endpoint cancel OWARE (`POST /api/order/cancel`).
### Rationale
Mencegah analyzer atau staf laboratorium mengerjakan sampel yang telah dibatalkan secara medis/operasional di SIMRS.
### Impact
Penambahan logika enqueue pembatalan di `LabOrderCancelHandler` dan handling `MessageType` pada `LabOwareQueueProcessor`.
### Architecture Impact
Menyinkronkan siklus hidup pembatalan order antar-domain.
### Resolved By
User & ica-analyst (sesi grill-me)
### Resolved Date
2026-10-29

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
Keputusan penutupan celah telah menetapkan pola integrasi **Notification & Pull**:
- Endpoint baru di OWARE: `POST /api/order/lab/bilreg`
- Penarikan data detail order via `GET /api/LabContext/LabOrderFeature/{orderId}` dengan autentikasi Token Bearer JWT.
- Klien HTTP riil di `Bilreg.Infrastructure` untuk pengiriman notifikasi antrean.
- Background worker hosted service di `Bilreg.Api` untuk pemrosesan antrean otomatis.
- Pemicu otomatis antrean pada `LabOrderChargeHandler` dan penanganan pembatalan pada `LabOrderCancelHandler`.

Seluruh keputusan ini memerlukan dokumen perancangan arsitektur teknis formal (**ARCHITECTURE**) lintas-service sebelum rencana implementasi (**IMPLEMENTATION-PLAN**) dapat disusun.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] Semua celah kritis (*critical gaps*) GAP-001 s/d GAP-007 telah ditutup dengan keputusan final
- [x] Seluruh pertanyaan terbuka OQ-001 s/d OQ-006 telah disepakati bersama user
- [x] Pola integrasi *Notification & Pull* telah disepakati secara mendalam
- [x] Kontrak komunikasi dua arah (Bilreg ↔ OWARE) dan metode autentikasi telah definitif
- [x] Dokumen spesifikasi teknis arsitektur (**ARCHITECTURE**) disusun dan diverifikasi oleh Architect

## Status

**READY-FOR-PLANNING**

## Notes

Seluruh kesenjangan dan pertanyaan terbuka telah berhasil diselesaikan (**RESOLVED**). Dokumen spesifikasi arsitektur teknis (**ARCHITECTURE**) telah disusun. Status dokumen secara resmi disahkan menjadi **READY-FOR-PLANNING** oleh peran **Architect** (`ica-architect`).

---

# 11. References

Referenced artifacts:
- [LAB-OWARE-ORDER-INTEGRATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-INTEGRATION-ISSUE.md)
- [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md)
- [lab-workflow.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-workflow.md)
- [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)

Referenced codebase locations (`b09-bilreg-api`):
- Model antrean: [LabOwareOutboundQueueModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOwareFeature/LabOwareOutboundQueueModel.cs)
- Agregat Order: [LabOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs)
- Query Detail Order: [LabOrderGetQuery.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs)
- Queue Processor: [LabOwareQueueProcessor.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOwareFeature/LabOwareQueueProcessor.cs)
- Adapter Mock Eksisting: [LabOwareIntegration.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabOwareIntegration.cs)
- Controller Antrean: [LabOwareController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabOwareFeature/LabOwareController.cs)

Referenced codebase locations (`a046_oware_ordermiddlewareapi`):
- Controller Order: [OrderController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/OrderController.cs)
- Hub Dispatcher LIS: [OrderLabHubService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/OrderLabHubService.cs)
