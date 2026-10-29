---
Title: Rencana Implementasi Integrasi Pengiriman Order Laboratorium ke OWARE
Code: LAB-OWARE-ORDER-INTEGRATION
Artifact: IMPLEMENTATION-PLAN
Version: 1.1
LastUpdated: 2026-10-29
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Implementasi integrasi pengiriman data order pemeriksaan laboratorium dari Laboratory Workflow Feature (LWF) pada backend `b09-bilreg-api` ke middleware OWARE (`a046_oware_ordermiddlewareapi`) untuk diteruskan ke Laboratory Information System (LIS) atau analyzer laboratorium rumah sakit menggunakan pola **Notification & Pull**, serta penyelarasan antarmuka kerja laboratorium pada frontend web (`c012_myhospital_web`).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- ISSUE: [LAB-OWARE-ORDER-INTEGRATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-INTEGRATION-ISSUE.md)
- FEASIBILITY-ASSESSMENT: [LAB-OWARE-ORDER-INTEGRATION-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-INTEGRATION-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- ARCHITECTURE: [LAB-OWARE-ORDER-INTEGRATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-INTEGRATION-ARCHITECTURE.md)
- DOMAIN & WORKFLOW SPECS: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md), [lab-workflow.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-workflow.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Rencana implementasi mencakup modifikasi teknis terisolasi pada tiga repositori:
1. **`a046_oware_ordermiddlewareapi`**:
   - Penyediaan endpoint penerima baru: `POST /api/order/lab/bilreg`.
   - Implementasi service autentikasi Token Bearer JWT dan klien HTTP penarikan detail order dari Bilreg.
   - Implementasi builder untuk transformasi `LabOrderGetResponse` ke `OrderLabModel` dan pemetaan `TarifId` ke `ConsId` melalui tabel `OWARE_MapTarifLab`.
   - Dispatch order ke vendor LIS via `OrderLabHubService` dan penanganan idempotency.
2. **`b09-bilreg-api`**:
   - Konfigurasi `OwareOptions` pada `appsettings.json` dan registrasi `IHttpClientFactory`.
   - Penggantian mock `LabOwareIntegration` dengan adapter HTTP riil `LabOwareHttpService`.
   - Otomatisasi pemicu pembentukan antrean (`Enqueue`) saat order berhasil di-charge (`Charged`) di `LabOrderChargeHandler`.
   - Otomatisasi pemicu pembatalan (`MessageType = "LabOrderCancel"`) di `LabOrderCancelHandler`.
   - Penambahan hosted background service `LabOwareOutboundWorker` di `Bilreg.Api`.
3. **`c012_myhospital_web`**:
   - Penyelarasan aturan visibilitas tombol aksi pada workbench laboratorium (`lab-workflow-actions.config.ts`): tombol manual enqueue hanya tampil jika belum terantre, dan tombol retry aktif saat status gagal.
   - Penambahan indikator/tooltip pesan kesalahan (`lastError`) pada badge dan strip status OWARE di panel detail order.

---

# 3. Dependencies

## External Dependencies
- Service `b09-bilreg-api` dan `a046_oware_ordermiddlewareapi` dapat saling berkomunikasi melalui protokol HTTP pada port lokal/intranet masing-masing (default: Bilreg port 5189, OWARE port 5255).
- Kredensial akun service login tersedia untuk autentikasi OWARE ke `/api/User/login` di Bilreg.

## Slice Dependencies Rule
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Ketergantungan mengacu secara eksplisit pada Slice ID.
- Pemenuhan ketergantungan tercapai saat slice prasyarat berstatus `IMPLEMENTED` dan output kode yang disyaratkan telah tersedia di repositori target.

---

# 4. Progress Summary

| Phase | Description | Repository Scope | Slices | Implementation Status | Review Status | Progress |
|---|---|---|---|---|---|---|
| **P1** | OWARE Ingestion & Pulling Pipeline | `a046_oware_ordermiddlewareapi` | P1-S01 s/d P1-S03 | IMPLEMENTED | GO | 3/3 |
| **P2** | LWF Outbound Adapter & Worker Pipeline | `b09-bilreg-api` | P2-S04 s/d P2-S06 | IMPLEMENTED | GO | 3/3 |
| **P3** | Frontend Action Visibility & Status UX | `c012_myhospital_web` | P3-S07 | NOT-STARTED | NOT-REVIEWED | 0/1 |
| **P4** | Unit Testing & End-to-End Verification | Cross-Repo (`b09`, `oware`, `fe`) | P4-S08 s/d P4-S10 | NOT-STARTED | NOT-REVIEWED | 0/3 |

---

# 5. Phases

## P1 - OWARE Ingestion & Pulling Pipeline (`a046_oware_ordermiddlewareapi`)

Implementation Status: IMPLEMENTED
Review Status: GO

### P1-S01
- **Title**: Client Penarikan Data Bilreg & Manajemen Token JWT
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Mengimplementasikan service autentikasi Token Bearer JWT (`IBilregAuthTokenService`) dan klien HTTP (`IBilregLabOrderClient`) di OWARE untuk menarik data detail order dari API `b09-bilreg-api`.
- **Depends On**: None
- **Repository**: `a046_oware_ordermiddlewareapi`
- **Completion Criteria**:
  - Konfigurasi `BilregClient` (`BaseUrl`, `Username`, `Password`, `TimeoutSeconds`) terdefinisi di `appsettings.json` dan di-bind via `IOptions<BilregClientOptions>`.
  - `IBilregAuthTokenService` berhasil melakukan POST ke `/api/User/login`, menyimpan token JWT di `IMemoryCache` dengan penanganan expiry, serta auto-refresh saat token kedaluwarsa.
  - `IBilregLabOrderClient` berhasil mengeksekusi HTTP GET ke `/api/LabContext/LabOrderFeature/{orderId}` dengan header `Authorization: Bearer <token>` dan me-deserialize respon ke DTO detail order `BilregLabOrderResponseDto`.
  - DI container me-resolve kedua service dengan benar saat startup.
- **Notes**: 
  - Ditempatkan pada layer `Oware.Application` (kontrak & DTO) dan `Oware.Infrastructure` (implementasi HTTP & cache).
  - Changed / created files:
    - [`BilregClientOptions.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Helpers/BilregClientOptions.cs)
    - [`BilregLabOrderResponseDto.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/DTOs/BilregLabOrderResponseDto.cs)
    - [`IBilregAuthTokenService.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/IServices/IBilregAuthTokenService.cs)
    - [`IBilregLabOrderClient.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/IServices/IBilregLabOrderClient.cs)
    - [`BilregAuthTokenService.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesBilreg/BilregAuthTokenService.cs)
    - [`BilregLabOrderClient.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesBilreg/BilregLabOrderClient.cs)
    - [`appsettings.json`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/appsettings.json)
    - [`Program.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Program.cs)
    - [`Startup.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Startup.cs)
    - [`BilregAuthTokenServiceTest.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Test/BilregAuthTokenServiceTest.cs)
    - [`BilregLabOrderClientTest.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Test/BilregLabOrderClientTest.cs)
  - Verification: Solusi `A046_Oware_OrderMiddlewareApi.sln` sukses build dengan 0 Error; 9 targeted unit test di `Oware.Test` lulus 100%.

---

### P1-S02
- **Title**: Order Builder & Pemetaan Vendor LIS Berbasis TarifId
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Mengimplementasikan `IOrderLabBilregBuilder` untuk mengonversi data detail order dari Bilreg menjadi `OrderLabModel`, memetakan item pemeriksaan ke vendor LIS (`ConsId`) via `OWARE_MapTarifLab`, dan memvalidasi kelengkapan data.
- **Depends On**: P1-S01
- **Repository**: `a046_oware_ordermiddlewareapi`
- **Completion Criteria**:
  - Interface `IOrderLabBilregBuilder` dan kelas implementasi `OrderLabBilregBuilder` terdefinisi di `Oware.Application/Workers/Creators`.
  - Builder memetakan seluruh properti pasien, registrasi, nomor order (`OrderNo`), dokter pengorder, catatan klinis, dan urgensi (`IsCito`).
  - Untuk setiap item tes pada order, builder mencocokkan `TarifId` ke data cache `OWARE_MapTarifLab` (`IMapTarifLabDal`) untuk mengisi `ConsId` (Sysmex, Prolims, dsb.) dan kode tes LIS pada `OrderLabTarifModel` dan `OrderLabPemeriksaanModel`.
  - Jika `TarifId` tidak terpetakan pada suatu vendor, builder mencatat peringatan dan menandai status validasi secara tepat.
- **Notes**: 
  - Re-use struktur `OrderLabModel` eksisting agar kompatibel 100% dengan dispatcher `OrderLabHubService`.
  - Changed / created files:
    - [`IOrderLabBilregBuilder.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Workers/Creators/IOrderLabBilregBuilder.cs)
    - [`OrderLabBilregBuilder.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Workers/Creators/OrderLabBilregBuilder.cs)
    - [`Startup.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Startup.cs) (DI registration: `services.AddScoped<IOrderLabBilregBuilder, OrderLabBilregBuilder>()`)
    - [`OrderLabBilregBuilderTest.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Test/OrderLabBilregBuilderTest.cs)
  - Verification: Solusi `A046_Oware_OrderMiddlewareApi.sln` berhasil build dengan 0 Error; 8 unit test di `OrderLabBilregBuilderTest` lulus 100% (total 17 test lulus pada suite Bilreg).

---

### P1-S03
- **Title**: Endpoint Penerimaan Order Bilreg & Command Handler (`POST /api/order/lab/bilreg`)
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Menyediakan endpoint API baru `POST /api/order/lab/bilreg` pada `OrderController` yang menerima notifikasi `{ "orderId": "..." }` tanpa autentikasi, memicu penarikan data, persistensi order, dan dispatching ke vendor LIS.
- **Depends On**: P1-S02
- **Repository**: `a046_oware_ordermiddlewareapi`
- **Completion Criteria**:
  - DTO request `CreateOrderLabFromBilregCommand(string OrderId)` terdefinisi di `Oware.Application/UseCases/Commands`.
  - Handler `CreateOrderLabFromBilregHandler`:
    1. Memanggil `IBilregLabOrderClient.GetOrderDetail(request.OrderId)`.
    2. Menghasilkan `OrderLabModel` melalui `IOrderLabBilregBuilder`.
    3. Menyimpan order ke tabel `OWARE_Order` via `IOrderWriter`.
    4. Meneruskan ke `IOrderLabHubService.Execute(orderLab)` untuk transmisi ke mesin LIS/analyzer.
    5. Menyimpan audit log ke `OWARE_OrderLog`.
    6. Mengembalikan respons `CreateOrderLabFromBilregResponse` (`Success`, `Message`, `OrderId`, `VendorReffId`).
  - Action method `CreateFromBilreg` terdaftar di `Oware.Api/Controllers/OrderController.cs` dengan rute `[HttpPost("lab/bilreg")]` (tanpa proteksi authorize).
  - Pengecekan idempotensi: jika `OrderId` sudah pernah diproses dan sukses terkirim, handler mengembalikan acknowledgement sukses tanpa mengirim ganda ke LIS.
- **Notes**: 
  - Endpoint ini menjamin zero regression terhadap endpoint legacy `[HttpPost("lab")]`.
  - Changed / created files:
    - [`CreateOrderLabFromBilregCommand.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/UseCases/Commands/CreateOrderLabFromBilregCommand.cs) (Command, Response DTO, dan Handler `CreateOrderLabFromBilregHandler`)
    - [`OrderController.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/OrderController.cs) (Menambahkan action `CreateFromBilreg` dengan `[HttpPost]` `Route("lab/bilreg")` dan `[AllowAnonymous]`)
    - [`CreateOrderLabFromBilregHandlerTest.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Test/CreateOrderLabFromBilregHandlerTest.cs) (Unit test komprehensif menguji skenario sukses, idempotensi, order not found, dispatch LIS failure, konsolidasi LIS tidak terpetakan, dan penanganan open retry)
  - Verification: Solusi `A046_Oware_OrderMiddlewareApi.sln` build 100% sukses (0 Error); seluruh 29 unit test pada suite Bilreg lulus (12 test baru di `CreateOrderLabFromBilregHandlerTest`, 8 di `OrderLabBilregBuilderTest`, 7 di `BilregLabOrderClientTest`, 2 di `BilregAuthTokenServiceTest`).

---

## P2 - LWF Outbound Adapter & Worker Pipeline (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED
Review Status: GO

### P2-S04
- **Title**: Konfigurasi OwareOptions & Adapter HTTP Outbound Riil
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Mengganti implementasi mock `LabOwareIntegration` dengan adapter HTTP riil `LabOwareHttpService` yang terdaftar via `IHttpClientFactory` dan membaca konfigurasi `OwareOptions` dari `appsettings.json`.
- **Depends On**: None
- **Repository**: `b09-bilreg-api`
- **Completion Criteria**:
  - Class `OwareOptions` terdefinisi di `Bilreg.Infrastructure/LabContext/LabOwareFeature` memuat `BaseUrl`, `OrderLabEndpoint`, `TimeoutSeconds`, dan `MaxRetryCount`.
  - Section `"Oware"` ditambahkan ke `appsettings.json` di `Bilreg.Api`.
  - Registrasi named `HttpClient` (`"OwareClient"`) di DI container (`InfrastructureService.cs`) dengan timeout 10 detik.
  - Implementasi riil `LabOwareIntegration : ILabOwareIntegration` di `Bilreg.Infrastructure/LabContext/Integration`:
    1. Menerima payload JSON minimalis `{ "orderId": "..." }`.
    2. Mengirimkan HTTP POST ke endpoint OWARE (`{BaseUrl}/api/order/lab/bilreg`).
    3. Menangani respons HTTP 200/400/500/timeout dan mengembalikan `LabOwareSendResult(success, errorMessage)`.
  - Unit test / mock test memverifikasi serialization dan pemetaan status respons.
- **Notes**: 
  - Menghapus simulasi string `"FAIL"` in-memory lama dan menggantinya dengan adapter HTTP riil terkonfigurasi.
  - Changed / created files:
    - [`OwareOptions.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOwareFeature/OwareOptions.cs)
    - [`appsettings.json`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/appsettings.json)
    - [`InfrastructureService.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs)
    - [`LabOwareIntegration.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabOwareIntegration.cs)
    - [`Bilreg.Infrastructure.csproj`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/Bilreg.Infrastructure.csproj)
    - [`LabOwareIntegrationTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOwareFeature/LabOwareIntegrationTest.cs)
  - Verification: Solusi `b09-bilreg-api.sln` build berhasil (0 Errors); seluruh 27 unit test pada test suite `LabOware` lulus 100% (termasuk 11 test baru di `LabOwareIntegrationTest`).

---

### P2-S05
- **Title**: Otomatisasi Pemicu Enqueue saat Charged & Enqueue Pembatalan
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Mengintegrasikan pembentukan antrean `LabOwareOutboundQueueModel` secara otomatis di dalam `LabOrderChargeHandler` saat order berstatus `Charged`, serta otomatisasi enqueue pembatalan di `LabOrderCancelHandler`.
- **Depends On**: P2-S04
- **Repository**: `b09-bilreg-api`
- **Completion Criteria**:
  - Di `LabOrderChargeHandler.cs`: setelah pembebanan billing berhasil dan order berstatus `Charged`, handler membentuk record antrean `LabOwareOutboundQueueModel.CreatePending(order.OrderId, payloadJson, technicalNow)` dan memutasi `order.MarkOwarePending(userId, technicalNow)` dalam satu scope transaksi (`TransHelper.NewScope()`).
  - Payload antrean yang disimpan berisi JSON ringkas: `{"orderId":"<OrderId>"}`.
  - Aturan invariant: order Cito dan Rutin sama-sama dipicu saat status `Charged`.
  - Di `LabOrderCancelHandler.cs`: jika order yang dibatalkan memiliki status `order.OwareStatus == OwareStatusEnum.Sent`, handler menyimpan antrean baru dengan `MessageType = "LabOrderCancel"` dan payload `{"orderId":"<OrderId>"}`.
  - Dukungan tombol manual di UI: endpoint `LabOwareRetryCmd` tetap berfungsi untuk retry antrean yang berstatus `Failed`.
- **Notes**: 
  - Memastikan tidak ada ketergantungan pemanggilan controller eksternal untuk pembentukan antrean selama siklus hidup order normal.
  - Changed / created files:
    - [`LabOrderChargeCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderChargeCmd.cs) (Injeksi `ILabOwareOutboundQueueRepo`, enqueue payload `{"orderId":"..."}`, dan mutasi `order.MarkOwarePending`)
    - [`LabOrderCancelCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCancelCmd.cs) (Injeksi `ILabOwareOutboundQueueRepo`, enqueue `LabOrderCancel` saat order berstatus `OwareStatus == Sent`)
    - [`LabOrderChargeHandlerTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderChargeHandlerTest.cs) (Menambahkan verifikasi pembentukan antrean OWARE saat charge sukses dan non-enqueue saat gagal)
    - [`LabOrderCancelHandlerTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCancelHandlerTest.cs) (Menambahkan verifikasi pembentukan antrean pembatalan OWARE saat order berstatus `Sent`)
    - [`LabOrderM9HandlersTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderM9HandlersTest.cs) (Memperbarui injeksi mock queue repo pada handler cancel)
  - Verification: Solusi `b09-bilreg-api.sln` build berhasil (0 Errors); seluruh 32 unit test pada `LabOrderChargeHandlerTest`, `LabOrderCancelHandlerTest`, dan `LabOrderM9HandlersTest` lulus 100%, serta 27 unit test pada test suite `LabOware` lulus 100%.

---

### P2-S06
- **Title**: Background Worker Antrean Latar Belakang (LabOwareOutboundWorker)
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Mengimplementasikan .NET `BackgroundService` (`LabOwareOutboundWorker`) di host `Bilreg.Api` untuk secara periodik memproses batch antrean pending/failed melalui `LabOwareQueueProcessor`.
- **Depends On**: P2-S05
- **Repository**: `b09-bilreg-api`
- **Completion Criteria**:
  - Class `LabOwareOutboundWorker` mengimplementasikan `BackgroundService` di `Bilreg.Api/HostedServices`.
  - Worker membaca parameter `WorkerEnabled` (boolean) dan `IntervalSeconds` (default 5-10 detik) dari konfigurasi `OwareOptions`.
  - Worker menggunakan `IServiceScopeFactory` untuk me-resolve `LabOwareQueueProcessor` di setiap iterasi batch secara aman.
  - Memanggil `processor.ProcessBatch(batchSize: 20, actor: LabOwareQueueProcessor.WorkerUserId)`.
  - Dilengkapi mekanisme try-catch global agar exception jaringan/transmisi tidak mematikan background service.
  - Terdaftar via `services.AddHostedService<LabOwareOutboundWorker>()` di `Bilreg.Api/Configurations`.
  - Endpoint controller `LabOwareController` (`process` dan `retry`) tetap aktif dan dapat dipanggil secara manual.
- **Notes**: 
  - Worker dirancang ringan dan non-blocking terhadap thread pool utama.
  - Changed / created files:
    - [`LabOwareOutboundWorker.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/HostedServices/LabOwareOutboundWorker.cs) (Implementasi background worker ter-scoped dengan fallback interval dan exception handling)
    - [`InfrastructureService.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs) (Registrasi `AddHostedService<LabOwareOutboundWorker>()`)
    - [`LabOwareOutboundWorkerTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOwareFeature/LabOwareOutboundWorkerTest.cs) (Unit test worker ketika enabled, disabled, exception handling, dan start/stop lifecycle)
  - Verification: Solusi `b09-bilreg-api.sln` build berhasil (0 Errors); 31 unit test suite `LabOware` (termasuk 4 test `LabOwareOutboundWorkerTest`) dan 58 unit test gabungan `LabOrder` & `LabOware` lulus 100%.

---

## P3 - Frontend Action Visibility & Status UX (`c012_myhospital_web`)

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P3-S07
- **Title**: Penyelarasan Visibilitas Aksi & Indikator Status OWARE di Frontend
- **Implementation Status**: NOT-STARTED
- **Review Status**: NOT-REVIEWED
- **Objective**: Menyelaraskan aturan visibilitas tombol aksi pada workbench laboratorium (`lab-workflow-actions.config.ts`), merapikan guard `canEnqueueOware` agar tombol manual hanya tampil jika belum terantre, memastikan `canRetryOware` aktif saat status `Failed`, serta menambahkan indikator/tooltip `lastError` pada `LabOwareStatusBadge.vue` dan `LabOrderDetailPanel.vue`.
- **Depends On**: P2-S06
- **Repository**: `c012_myhospital_web`
- **Completion Criteria**:
  - `canEnqueueOware(order)` di `src/modules/Laboratory/configs/lab-workflow-actions.config.ts` diselaraskan sehingga mengembalikan `true` hanya jika order minimal berstatus `Charged` dan status OWARE belum pernah terantre (`order.owareStatus === 0`).
  - `canRetryOware(order)` memastikan tombol retry aktif hanya jika `order.owareStatus === OwareStatus.Failed`.
  - `LabOwareStatusBadge.vue` dan strip integrasi di `LabOrderDetailPanel.vue` menampilkan tooltip atau deskripsi pesan kesalahan teknis (`lastError`) saat status order adalah `Failed`.
  - Mematuhi working directory `c012_myhospital_web`, diformat dengan Prettier, lulus type-check `pnpm tc:app`, lulus targeted test `pnpm test src/modules/Laboratory/__tests__/lab-workflow-actions.config.spec.ts`, dan lulus `pnpm lint`.
- **Notes**: Operasi frontend murni di repositori `c012_myhospital_web`.

---

## P4 - Unit Testing & End-to-End Verification

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P4-S08
- **Title**: Unit & Integration Testing LWF Outbound di Bilreg
- **Implementation Status**: NOT-STARTED
- **Review Status**: NOT-REVIEWED
- **Objective**: Menyusun dan memverifikasi unit test suite untuk seluruh komponen baru/termodifikasi di `b09-bilreg-api`.
- **Depends On**: P2-S06
- **Repository**: `b09-bilreg-api`
- **Completion Criteria**:
  - Test suite `LabOwareIntegrationTest` menguji skenario sukses HTTP 200, kegagalan HTTP 500, dan timeout.
  - Test suite `LabOrderChargeHandlerTest` memverifikasi pembentukan antrean `BILRG_LabOwareOutboundQueue` dan pembaruan `OwareStatus = Pending` saat order di-charge.
  - Test suite `LabOrderCancelHandlerTest` memverifikasi pembentukan antrean pembatalan jika `OwareStatus == Sent`.
  - Test suite `LabOwareQueueProcessorTest` memverifikasi transisi status antrean (`Succeeded`/`Failed`) dan status order (`Sent`/`Failed`).
  - Seluruh targeted test lulus tanpa regresi pada test suite lab lainnya.
- **Notes**: Menggunakan xUnit dan Moq di proyek `Bilreg.Test`.

---

### P4-S09
- **Title**: Unit & Integration Testing Inbound Order Processing di OWARE
- **Implementation Status**: NOT-STARTED
- **Review Status**: NOT-REVIEWED
- **Objective**: Menyusun dan memverifikasi unit test suite untuk seluruh komponen baru di `a046_oware_ordermiddlewareapi`.
- **Depends On**: P1-S03
- **Repository**: `a046_oware_ordermiddlewareapi`
- **Completion Criteria**:
  - Test suite `CreateOrderLabFromBilregHandlerTest` memverifikasi alur: penarikan detail order dari mock client, mapping via builder, penyimpanan order, dan penerusan ke `OrderLabHubService`.
  - Test suite `BilregAuthTokenServiceTest` memverifikasi login service account, penyimpanan token di memory cache, dan re-login saat token kedaluwarsa.
  - Test suite `OrderLabBilregBuilderTest` memverifikasi kecocokan pemetaan `TarifId` ke `ConsId` vendor LIS.
  - Seluruh test di `Oware.Test` berjalan sukses dan lulus 100%.
- **Notes**: Ditempatkan di proyek `Oware.Test`.

---

### P4-S10
- **Title**: Frontend Targeted Spec Testing & Regression Check
- **Implementation Status**: NOT-STARTED
- **Review Status**: NOT-REVIEWED
- **Objective**: Menjalankan targeted test suite pada frontend untuk memverifikasi fungsionalitas badge status OWARE, aksi workbench, dan dialog retry tanpa regresi.
- **Depends On**: P3-S07
- **Repository**: `c012_myhospital_web`
- **Completion Criteria**:
  - Menjalankan targeted test: `pnpm test src/modules/Laboratory/__tests__/lab-workflow-actions.config.spec.ts` dan `src/modules/Laboratory/__tests__/LabOrderDetailPanel.spec.ts`.
  - Menjalankan type-check cepat: `pnpm tc:app` (0 errors).
  - Menjalankan linter: `pnpm lint` (0 errors).
  - Tidak ada regresi pada pengujian komponen lab workflow lainnya.
- **Notes**: Dilakukan langsung di working directory `c012_myhospital_web`.

---

# 6. Change Log

- **2026-10-29**: Initial plan release (Version 1.0) disahkan langsung dengan status `Execution Approval: APPROVED` berdasarkan persetujuan arsitektur teknis lintas-service LWF (`b09-bilreg-api`) dan OWARE (`a046_oware_ordermiddlewareapi`).
- **2026-10-29**: Plan update (Version 1.1) menambahkan Phase 3 (`P3-S07`) khusus penyelarasan frontend (`c012_myhospital_web`) untuk merapikan visibilitas tombol aksi OWARE dan tooltip error indikator, serta memperluas Phase 4 testing hingga `P4-S10`. Structure re-approved as `Execution Approval: APPROVED`.
