---
Title: Pembebanan Biaya Pemeriksaan Radiologi Implementation Plan
Code: OC-RAD-ORDER-CHARGE
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-24
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan fitur pembebanan biaya (*billing charge*) dan kompensasi pembatalan transaksi tagihan order pemeriksaan radiologi pada agregat `RadOrderModel`, menghubungkannya ke modul Tata Rekening (`PaymentContext` / `TRK-BILLING`) melalui port `IRadBillingIntegration`, dan menegakkan gerbang finansial pada pemindaian fisik (`RAD-EXAM`).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-RAD-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-ORDER-CHARGE-FEATURE.md)
- ARCHITECTURE: [OC-RAD-ORDER-CHARGE-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-ORDER-CHARGE-ARCHITECTURE.md)
- Originating Outcome: [OC-RAD-ORDER-RAD.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-ORDER-RAD.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

1. **Database Schema (`Bilreg.SqlDb.RadContext.RadOrderFeature`)**:
   - Skrip migrasi penambahan kolom `BillingTindakanId VARCHAR(12)` dan `BillingLastError VARCHAR(200)` pada tabel `BILRG_RadOrder`.
2. **Domain Layer (`Bilreg.Domain.RadContext.RadOrderFeature`)**:
   - Penambahan status `Charged = 4` pada `RadOrderStatusEnum`.
   - Penambahan properti dan metode domain `Charge(userId)`, `MarkCharged(tindakanId, userId, chargedAt)`, dan `RecordBillingError(error, userId, recordedAt)`.
   - Pembaruan guard `Cancel(...)` untuk mendukung pembatalan dari status `Charged`.
3. **Payment Context Integration (`Bilreg.Domain.PaymentContext` & `Bilreg.Application.PaymentContext`)**:
   - Penambahan metode `CreateFromRadOrderItem(...)` pada `TrsBillFactory`.
   - Penambahan metode `FromRadOrderItem(...)` pada `IAddBillAppService` dan `AddBillAppService`.
4. **Application Layer (`Bilreg.Application.RadContext.RadOrderFeature`)**:
   - Definisi port `IRadBillingIntegration` beserta DTO terkait (`RadBillingChargeResult`, `RadBillingCancelRequest`).
   - Implementasi command `RadOrderChargeCmd` dan handler `RadOrderChargeHandler`.
   - Pembaruan `RadOrderCancelHandler` untuk mendelegasikan kompensasi pembatalan finansial ke `IRadBillingIntegration.CancelCharge`.
5. **Infrastructure Layer (`Bilreg.Infrastructure.RadContext`)**:
   - Implementasi adapter konkret `RadBillingIntegration`.
   - Pembaruan `RadOrderDal` untuk membaca dan menyimpan kolom billing.
6. **API Layer (`Bilreg.Api.Controllers.RadContext`)**:
   - Penambahan endpoint `POST api/rad/order/{id}/charge` pada `RadOrderController`.
7. **Test Suite (`Bilreg.Test.RadContext.RadOrderFeature`)**:
   - Unit test domain, command handler, dan adapter integrasi.

---

# 3. Dependencies

- `b09-bilreg-api` .NET 8 codebase
- Modul Tata Rekening (`Bilreg.Application.PaymentContext.TrsBillingFeature` & `TataRekeningFeature`)
- Existing `RadOrderModel`, `IRadOrderRepo`, `AddBillAppService`

---

# 4. Progress Summary

| Phase | Title | Implementation Status | Review Status | Progress |
|---|---|---|---|---|
| P1 | Database Schema & Domain Updates | IMPLEMENTED | GO | 2/2 |
| P2 | Payment Context Extension | IMPLEMENTED | GO | 1/1 |
| P3 | Application Integration & Use Cases | IMPLEMENTED | GO | 3/3 |
| P4 | Infrastructure Realization | IMPLEMENTED | GO | 2/2 |
| P5 | API Controller & Endpoint | IMPLEMENTED | GO | 1/1 |
| P6 | Verification & Unit Tests | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Database Schema & Domain Updates

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P1-S01: Database Migration for BILRG_RadOrder

Title: Penambahan Kolom Billing pada BILRG_RadOrder  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menyediakan skrip SQL migrasi untuk menambahkan `BillingTindakanId` dan `BillingLastError` pada tabel `BILRG_RadOrder`.  
Depends On: None  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Membuat skrip migrasi idempoten `BILRG_RadOrder_Billing_Alter.sql` yang menambahkan kolom `BillingTindakanId VARCHAR(12) NOT NULL DEFAULT('')` dan `BillingLastError VARCHAR(200) NOT NULL DEFAULT('')` menggunakan pengecekan `COL_LENGTH`.
- Menambahkan filtered index `IX_BILRG_RadOrder_Billing` pada kolom `BillingTindakanId` (dengan klausul `WHERE BillingTindakanId <> ''`) beserta pengecekan idempoten `sys.indexes`.
- Memperbarui skrip DDL `BILRG_RadOrder.sql` dengan menyertakan kedua kolom billing baru beserta filtered index `IX_BILRG_RadOrder_Billing`.  
Changed Files:
- Created: `b09-bilreg-api/src/bilreg/Bilreg.SqlDb/RadContext/RadOrderFeature/BILRG_RadOrder_Billing_Alter.sql`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.SqlDb/RadContext/RadOrderFeature/BILRG_RadOrder.sql`  
Completion Criteria:
- File `BILRG_RadOrder_Billing_Alter.sql` dibuat di `Bilreg.SqlDb/RadContext/RadOrderFeature/`.
- Tabel `BILRG_RadOrder.sql` terbarui dengan kolom baru.
- Query alter bersifat idempoten (menggunakan `COL_LENGTH`).

### P1-S02: Domain State & Lifecycle on RadOrderModel

Title: Pembaruan Status Charged dan Metode Domain pada RadOrderModel  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menambahkan `RadOrderStatusEnum.Charged`, properti billing, serta metode `Charge`, `MarkCharged`, dan `RecordBillingError`.  
Depends On: None  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Menambahkan enum value `Charged = 4` pada `RadOrderStatusEnum`.
- Menambahkan properti domain `BillingTindakanId` dan `BillingLastError` pada `RadOrderModel` dengan nilai awal string kosong, serta helper properti `IsCharged`.
- Memperbarui konstruktor `RadOrderModel` serta factory methods (`Default`, `CreateFromEmr`, `CreateExternal`) untuk menginisialisasi parameter billing dengan nilai default string kosong.
- Mengimplementasikan metode domain `Charge(userId)` dengan guard terhadap order void, validasi status transisi hanya dari `Requested` atau `Scheduled`, dan guard terhadap keberadaan `BillingTindakanId` yang sudah ada.
- Mengimplementasikan metode domain `MarkCharged(tindakanId, userId, chargedAt)` yang memvalidasi parameter, mengubah status ke `Charged`, menyimpan `BillingTindakanId`, mereset `BillingLastError`, serta memperbarui audit trail modifikasi.
- Mengimplementasikan metode domain `RecordBillingError(error, userId, recordedAt)` yang memvalidasi parameter, membatasi pesan maksimal 200 karakter, merekam ke `BillingLastError`, serta mencatat modifikasi audit trail tanpa mengubah status order.
- Memperbarui metode domain `Cancel(reason, userId, cancelDate)` agar mengizinkan pembatalan dari status `Requested`, `Scheduled`, maupun `Charged`.
- Menambahkan unit test suite pada `RadOrderModelTest.cs` (32 tests passing) dan memperbarui assertion message pada `RadOrderCancelCmdTest.cs` (14 tests passing).
Changed Files:
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Domain/RadContext/RadOrderFeature/RadOrderStatusEnum.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Domain/RadContext/RadOrderFeature/RadOrderModel.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderModelTest.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderCancelCmdTest.cs`
Completion Criteria:
- `RadOrderStatusEnum` memuat `Charged = 4`.
- `RadOrderModel` memiliki properti `BillingTindakanId` dan `BillingLastError`.
- `RadOrderModel.Charge(userId)` memvalidasi bahwa order berstatus `Requested` atau `Scheduled` dan belum di-charge.
- `RadOrderModel.MarkCharged(tindakanId, userId, chargedAt)` memutasi status ke `Charged`.
- `RadOrderModel.RecordBillingError(error, userId, recordedAt)` merekam pesan error.
- `RadOrderModel.Cancel(...)` mengizinkan pembatalan dari status `Requested`, `Scheduled`, atau `Charged`.

---

## P2 - Payment Context Extension

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P2-S01: TrsBillFactory and AddBillAppService for Radiology

Title: Dukungan Pembuatan TrsBill dari RadOrderItem  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperluas `TrsBillFactory` dan `IAddBillAppService` agar dapat membukukan item pemeriksaan radiologi ke rekening kunjungan pasien.  
Depends On: P1-S02  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Menambahkan metode `CreateFromRadOrderItem(...)` pada `TrsBillFactory` untuk mengonstruksi entitas `TrsBillType` bertipe `BillModulGroup.Jasa` dengan BillId formatted sebagai `{radOrder.OrderId}-{radOrderItem.ItemNo}`, memetakan komponen tarif ke `TrsBill2TransEventType` dengan PPA dari `radOrder.DokterPengorderId`/`DokterPengorderName`, COA PPDP/PDPT yang sesuai registrasi, serta penentuan `effectiveLayanan`.
- Menambahkan metode `FromRadOrderItem(...)` pada antarmuka `ICreateBillDomService` dan kelas implementasi `CreateBillDomService` dengan validasi registrasi terhadap `TataRekeningModel`.
- Menambahkan metode `FromRadOrderItem(...)` pada antarmuka `IAddBillAppService` dan kelas implementasi `AddBillAppService` untuk me-resolve rekening pasien (`ResolveTataRekening`), mendelegasikan pembuatan bill ke domain service, dan mempersistensi perubahannya ke `_tataRekeningRepo` serta `_trsBillingRepo`.
- Menambahkan test suite komprehensif pada `TrsBillFactoryRadOrderTest.cs` (4 test cases passing) yang memverifikasi konstruksi model tagihan radiologi, resolusi layanan default/override, validasi penjamin, dan integrasi persistensi `AddBillAppService`.
- Memperbaiki penegasan assertions pada `RadOrderChargeCmdTest.cs`.
Changed Files:
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillFactory.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Domain/PaymentContext/TrsBillFeature/CreateBillDomService.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Application/PaymentContext/TrsBillingFeature/AddBillAppService.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderChargeCmdTest.cs`
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Test/PaymentContext/TrsBillingFeature/TrsBillFactoryRadOrderTest.cs`
Completion Criteria:
- `TrsBillFactory.CreateFromRadOrderItem(...)` mengonstruksi `TrsBillType` bertipe `BillModulGroup.Jasa` dengan komponen dan tarif radiologi yang tepat.
- `IAddBillAppService` dan `AddBillAppService` memiliki metode `FromRadOrderItem(...)` yang memanggil `_tataRekeningRepo` dan `_trsBillingRepo`.

---

## P3 - Application Integration & Use Cases

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P3-S01: Contract Port IRadBillingIntegration

Title: Definisi Kontrak Port IRadBillingIntegration dan DTO  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mendefinisikan port interface `IRadBillingIntegration` pada namespace `Bilreg.Application.RadContext.RadOrderFeature.Integration`.  
Depends On: P1-S02  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Mendefinisikan record DTO `RadBillingCancelRequest` dengan properti `OrderId`, `RegId`, `UserId`, `Reason`, dan `CancelledAt`.
- Mendefinisikan record DTO `RadBillingChargeResult` dengan properti `Success`, `BillingTindakanId`, dan `ErrorMessage`.
- Mendefinisikan exception `RadBillingChargeException` turunan `Exception` untuk penanganan kegagalan penagihan finansial.
- Mendefinisikan port interface `IRadBillingIntegration` dengan method `ChargeOrder(RadOrderModel, string, DateTime)`, `CancelCharge(string, string, string, string, DateTime)`, dan overload `CancelCharge(RadBillingCancelRequest)`.
- Verifikasi kompilasi berhasil tanpa error (`dotnet build Bilreg.Application.csproj`).  
Changed Files:
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/Integration/RadBillingCancelRequest.cs`
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/Integration/RadBillingChargeResult.cs`
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/Integration/RadBillingChargeException.cs`
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/Integration/IRadBillingIntegration.cs`  
Completion Criteria:
- `IRadBillingIntegration` mendefinisikan method `ChargeOrder(RadOrderModel order, string userId, DateTime occurredAt)` returning string.
- `IRadBillingIntegration` mendefinisikan method `CancelCharge(string orderId, string regId, string userId, string reason, DateTime cancelledAt)`.
- DTO dan exception `RadBillingChargeException` didefinisikan secara modular.

### P3-S02: RadOrderChargeCmd & Handler

Title: Pembuatan RadOrderChargeCmd dan Handler  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengimplementasikan handler MediatR untuk mengeksekusi proses charge order radiologi.  
Depends On: P3-S01  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Mendefinisikan record command `RadOrderChargeCmd(string OrderId, string UserId) : IRequest<RadOrderChargeResponse>, IRadOrderKey`.
- Mendefinisikan record response `RadOrderChargeResponse(bool Success, string? BillingTindakanId, string? BillingLastError)`.
- Mengimplementasikan handler `RadOrderChargeHandler` dengan injeksi `IRadOrderRepo`, `IRadBillingIntegration`, dan `ITglJamProvider`.
- Menambahkan validasi guard pada request, OrderId, dan UserId.
- Memanggil `order.Charge(request.UserId)` untuk memastikan validitas status agregat.
- Mengintegrasikan pemanggilan `_radBillingIntegration.ChargeOrder(order, request.UserId, occurredAt)`: memanggil `order.MarkCharged` jika sukses, atau menangkap `RadBillingChargeException` dan mencatat `order.RecordBillingError`.
- Membungkus penyimpanan `_radOrderRepo.SaveChanges(order)` dalam transaction scope via `TransHelper.NewScope()`.
- Menambahkan unit test suite `RadOrderChargeCmdTest` yang mencakup validasi guard, penanganan not found, sukses charge, penanganan exception penagihan, serta validasi status awal tidak valid.
- Verifikasi build `Bilreg.Application.csproj` berhasil tanpa error (`0 Error(s)`).
Changed Files:
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/UseCases/RadOrderChargeCmd.cs`
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderChargeCmdTest.cs`
Completion Criteria:
- `RadOrderChargeCmd(string OrderId, string UserId)` dan `RadOrderChargeResponse(bool Success, string? BillingTindakanId, string? BillingLastError)`.
- Handler memanggil `order.Charge(userId)`, mendelegasikan ke `_radBillingIntegration.ChargeOrder`, lalu memanggil `order.MarkCharged` atau `order.RecordBillingError`.
- Perubahan tersimpan via `_radOrderRepo.SaveChanges(order)` dalam transaction scope.

### P3-S03: RadOrderCancelHandler Financial Compensation

Title: Pengikatan Kompensasi Finansial pada RadOrderCancelHandler  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengintegrasikan pemanggilan `_radBillingIntegration.CancelCharge` saat order yang dibatalkan sudah berstatus `Charged`.  
Depends On: P3-S01  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Menginjeksi port `IRadBillingIntegration` ke dalam constructor `RadOrderCancelHandler`.
- Pada `Handle`, menambahkan delegasi kompensasi finansial: jika `order.RadOrderStatus == RadOrderStatusEnum.Charged`, memanggil `_radBillingIntegration.CancelCharge(order.OrderId, order.Patient.RegId, request.UserId, request.Reason, now)` sebelum memanggil `order.Cancel(...)` dan `_radOrderRepo.SaveChanges(order)`.
- Menjamin atomisitas operasional di mana jika kompensasi tagihan gagal (melempar exception, misalnya rekening pasien telah lunas/final di kasir), exception akan menghentikan alur sehingga status klinis tidak termutasi ke `Cancelled` dan perubahan tidak disimpan ke repository.
- Memperbarui unit test suite `RadOrderCancelCmdTest` dengan mock `IRadBillingIntegration`, memvalidasi pembatalan order `Requested` dan `Scheduled` tidak memicu `CancelCharge`, sedangkan order `Charged` memanggil `CancelCharge` dengan parameter yang tepat serta kegagalan finansial mencegah status dibatalkan (16 tests passing).
- Memperbarui test harness pada `RadOrderLifecycleScenarioTest` untuk menyertakan mock `IRadBillingIntegration` dan menyesuaikan assertion pesan invariant pembatalan `RadOrderModel`.
Changed Files:
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/UseCases/RadOrderCancelCmd.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderCancelCmdTest.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderLifecycleScenarioTest.cs`  
Completion Criteria:
- Jika order berstatus `Charged`, `RadOrderCancelHandler` memanggil `_radBillingIntegration.CancelCharge(...)` sebelum memanggil `order.Cancel(...)`.
- Jika rekening sudah final/lunas di kasir, exception ditangkap dan proses pembatalan dicegah.

---

## P4 - Infrastructure Realization

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P4-S01: RadBillingIntegration Implementation

Title: Implementasi Adapter Konkret RadBillingIntegration  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengimplementasikan resolusi registrasi (`IRegRepo`), jaminan (`IJaminanRepo`), tarif (`ITarifRepo`), nilai tarif (`INilaiTarifRepo`), komponen (`IKomponenRepo`), pemanggilan `IAddBillAppService`, serta pembersihan bill pada pembatalan.  
Depends On: P2-S01, P3-S01  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Membuat class `RadOptions` pada namespace `Bilreg.Infrastructure.RadContext` dengan konfigurasi default `LayananRadId = "4RD01"`.
- Mengimplementasikan adapter konkret `RadBillingIntegration` pada namespace `Bilreg.Infrastructure.RadContext.Integration` yang merealisasikan port `IRadBillingIntegration`.
- Menginjeksi dependency: `IRegRepo`, `IJaminanRepo`, `ITarifRepo`, `INilaiTarifRepo`, `IKomponenRepo`, `IAddBillAppService`, `ITataRekeningRepo`, `ILayananRepo?`, dan `IOptions<RadOptions>?`.
- Pada `ChargeOrder`: mengorkestrasi lookup registrasi pasien internal (mendukung `ExecutionRegId` fallback ke `Patient.RegId`), verifikasi `IsAktif`, lookup penjamin `JaminanType`, lookup layanan radiologi default (`4RD01`), resolusi tarif, tipe tarif (Rajal vs Ranap), nilai tarif bertingkat (composite key dengan fallback), komponen tarif, dan memanggil `IAddBillAppService.FromRadOrderItem` untuk tiap order item; mengembalikan ID referensi tagihan (maksimal 12 karakter); membungkus error dalam `RadBillingChargeException`.
- Pada `CancelCharge`: memeriksa status `TataRekeningModel`; menolak pembatalan dengan `InvalidOperationException` jika status rekening sudah `Finalized`, `Lunas`, atau telah memiliki pembayaran kasir (`ListPayment.Any()`); jika berstatus `Opened`, mencari seluruh tagihan terkait (`TrsBillingId` berawalan `${orderId}-` atau sama dengan `orderId`), memanggil `tataRekening.DeleteBill`, dan menyimpan perubahan via `_tataRekeningRepo.SaveChanges`.
- Mendaftarkan port dan adapter `IRadBillingIntegration` -> `RadBillingIntegration` ke service collection di `Bilreg.Api/Configurations/InfrastructureService.cs`.
- Membuat unit test suite komprehensif `RadBillingIntegrationTest` (13 tests passing) mencakup skenario sukses admission patient, execution reg id override, pemotongan ref id > 12 karakter, exception wrapping ketika repo/tarif/dependency gagal, graceful handling saat rekening tidak ditemukan, penolakan saat rekening finalized/lunas/memiliki payment, dan penghapusan tagihan order saat opened.
Changed Files:
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/RadContext/RadOptions.cs`
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/RadContext/Integration/RadBillingIntegration.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Api/Configurations/InfrastructureService.cs`
- Created: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadBillingIntegrationTest.cs`  
Completion Criteria:
- `RadBillingIntegration` terdaftar di DI container.
- `ChargeOrder` menyelesaikan tarif dan penjamin pasien internal RS, membukukan `TrsBill`, dan mengembalikan ID referensi transaksi tagihan.
- `CancelCharge` memeriksa status `TataRekening`: menolak jika `Finalized`/`Lunas`, menghapus `TrsBill` terkait order jika masih `Opened`.

### P4-S02: RadOrderDal Mapping Updates

Title: Pembaruan DAL RadOrder untuk Kolom Billing  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui query SELECT, INSERT, dan UPDATE pada `RadOrderDal` untuk memetakan `BillingTindakanId` dan `BillingLastError`.  
Depends On: P1-S01  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Menambahkan properti `BillingTindakanId = ""` dan `BillingLastError = ""` pada record `RadOrderDto`, memperbarui default constructor, serta memetakan kedua field pada `FromModel` dan `ToModel`.
- Memperbarui konstanta `SelectColumns` pada `RadOrderDal` untuk menyertakan `aa.BillingTindakanId, aa.BillingLastError`.
- Memperbarui query `Insert` dan `Update` beserta helper `BuildParams` pada `RadOrderDal` untuk menyertakan parameter `@BillingTindakanId` dan `@BillingLastError`.
- Memverifikasi `RadOrderRepo` memetakan domain model dan DTO tanpa kehilangan informasi kolom billing.
- Menambahkan pengujian unit test pemetaan field billing pada `RadOrderRepoTest` (`FromModel_MapsBillingFieldsCorrectly`, `FromModel_WhenBillingError_MapsLastErrorCorrectly`, dan perluasan `ToModel_MapsAllFieldsAndAuditTrailCorrectly`).
- Verifikasi build `Bilreg.Infrastructure.csproj` (0 errors) dan seluruh test suite `RadOrderRepoTest` (10 passing), `RadOrderModelTest` (32 passing), `RadOrderCancelCmdTest` (16 passing), `RadOrderQueriesTest` (13 passing), `RadOrderCreateFromEmrCmdTest` (18 passing), `RadOrderLifecycleScenarioTest` (10 passing), dan `RadOrderChildModelsTest` (9 passing).
Changed Files:
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature/RadOrderDto.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/RadContext/RadOrderFeature/RadOrderDal.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderRepoTest.cs`  
Completion Criteria:
- `RadOrderDto` memuat field `BillingTindakanId` dan `BillingLastError`.
- `RadOrderDal.Insert`, `Update`, dan `Select` memetakan kolom-kolom baru.

---

## P5 - API Controller & Endpoint

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P5-S01: RadOrderController Charge Endpoint

Title: Ekspos Endpoint Charge pada RadOrderController  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengekspos endpoint HTTP untuk memicu proses charge order radiologi.  
Depends On: P3-S02  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Menambahkan endpoint `Charge` pada `RadOrderController` dengan routing `[HttpPost("{id}/charge")]` dan `[HttpPatch("charge")]`.
- Mendefinisikan model request `ChargeRadOrderRequest(string UserId, string OrderId = "")`.
- Mengimplementasikan resolusi parameter `effectiveId = string.IsNullOrWhiteSpace(id) ? req.OrderId : id` dan mendelegasikan ke `_mediator.Send(new RadOrderChargeCmd(effectiveId, req.UserId))`.
- Mengembalikan response `OkObjectResult` dengan envelope `JSendOk` membungkus payload `RadOrderChargeResponse`.
- Menambahkan unit test pada `RadOrderControllerTest`: pengujian route id, request body fallback id, dan verifikasi atribut routing (`HttpPost("{id}/charge")`, `HttpPatch("charge")`).
- Seluruh 13 unit test pada `RadOrderControllerTest` lulus 100%.
Changed Files:
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/RadContext/RadOrderController.cs`
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderControllerTest.cs`
Completion Criteria:
- Endpoint `POST /api/rad/order/{id}/charge` dan `PATCH /api/RadContext/RadOrderFeature/charge` terpasang.
- Mengembalikan response JSend standar dengan payload `RadOrderChargeResponse`.

---

## P6 - Verification & Unit Tests

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P6-S01: Comprehensive Unit & Integration Tests

Title: Pengujian Menyeluruh Fitur Charge Radiologi  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menulis unit test untuk memverifikasi lifecycle domain, handler charge, handler cancel, adapter integrasi billing, dan query DAL.  
Depends On: P1-S02, P3-S02, P3-S03, P4-S01  
Repository: `b09-bilreg-api`  
Implementation Notes:
- Menambahkan integrasi `RadOrderChargeHandler` dan wiring `BillingMock` ke dalam `RadOrderLifecycleHarness` pada `RadOrderLifecycleScenarioTest.cs`.
- Menambahkan skenario uji siklus hidup penuh (Requirement 11) pada `RadOrderLifecycleScenarioTest`:
  1. `Req11_OrderChargeAndCancellationLifecycle_TransitionsToChargedWithBillingTindakanId_AndTriggersCancelCharge`: mencakup alur end-to-end pembuatan order (`Requested`), pembebanan biaya via `RadOrderChargeCmd`, transisi ke status `Charged` dengan pengisian `BillingTindakanId`, dan pembatalan order saat `Charged` yang memicu pemanggilan kompensasi `IRadBillingIntegration.CancelCharge` serta memutasi status ke `Cancelled`.
  2. `Req11_ChargedOrder_WhenBillingRejectsCancellationDueToFinalizedAccount_OrderRemainsCharged`: memverifikasi proteksi guard saat pembatalan ditolak oleh adapter billing akibat transaksi kasir pasien telah `Finalized`/`Lunas`, memastikan status agregat tetap aman pada `Charged`.
- Melakukan verifikasi eksekusi test suite komprehensif pada seluruh area terdampak fitur `OC-RAD-ORDER-CHARGE`:
  - `RadOrderLifecycleScenarioTest` (12 tests passing)
  - `RadOrderModelTest` (32 tests passing - mencakup charge sukses, status invalid, double charge guard, cancel saat charged)
  - `RadOrderChargeCmdTest` (8 tests passing - mencakup sukses charge, billing error recording, validasi ID, dan order not found)
  - `RadOrderCancelCmdTest` (16 tests passing - mencakup void bill saat opened, penolakan saat kasir lunas, audit cancel)
  - `RadBillingIntegrationTest` (13 tests passing - mencakup resolusi tarif, penjamin, penolakan rekening finalized/lunas, pembersihan tagihan)
  - `RadOrderControllerTest` (13 tests passing)
  - `RadOrderRepoTest` (10 tests passing)
  - `RadOrderQueriesTest` (13 tests passing)
  - `RadOrderCreateFromEmrCmdTest` (18 tests passing)
  - `RadItemResolutionServiceTest` (13 tests passing)
  - `RadOrderChildModelsTest` (9 tests passing)
  - `TrsBillFactoryRadOrderTest` (4 tests passing)
- Seluruh 157 unit & scenario test yang mencakup domain lifecycle, application handler, infrastructure adapter, DAL, dan controller radiologi lulus 100% tanpa error via `dotnet test`.
Changed Files:
- Modified: `b09-bilreg-api/src/bilreg/Bilreg.Test/RadContext/RadOrderFeature/RadOrderLifecycleScenarioTest.cs`  
Completion Criteria:
- Unit test `RadOrderModelTest` mencakup scenario charge sukses, charge saat status tidak valid, double charge prevention, dan cancel saat charged.
- Unit test `RadOrderChargeHandlerTest` mencakup scenario sukses dan kegagalan billing.
- Unit test `RadOrderCancelHandlerTest` mencakup penolakan saat kasir lunas dan sukses void saat opened.
- Seluruh test lulus 100% via `dotnet test`.
