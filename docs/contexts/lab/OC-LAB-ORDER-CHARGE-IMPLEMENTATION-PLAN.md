---
Title: Pembebanan Biaya Pemeriksaan Laboratorium Implementation Plan
Code: OC-LAB-ORDER-CHARGE
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-17
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan penyelarasan fitur pembebanan biaya (*billing charge*) dan pembatalan transaksi tagihan order laboratorium pada agregat `LabOrderModel`, membersihkan leaky abstraction pada application layer, dan mengonsolidasikan integrasi ke Tata Rekening melalui port `ILabBillingIntegration`.

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-LAB-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-CHARGE-FEATURE.md)
- ARCHITECTURE: [OC-LAB-ORDER-CHARGE-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-CHARGE-ARCHITECTURE.md)
- Originating Outcome: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

1. **Application Layer (`Bilreg.Application.LabContext.LabOrderFeature`)**:
   - Memperluas antarmuka `ILabBillingIntegration` dengan metode `ChargeOrder` dan `CancelCharge` beserta DTO terkait (`LabBillingChargeOrderRequest`, `LabBillingChargeResult`, `LabBillingCancelRequest`).
   - Membersihkan `LabOrderChargeHandler` dari 6 repositori master billing, mendelegasikan pembebanan ke `ILabBillingIntegration`.
   - Mengalihkan validasi pembatalan kasir dan void tagihan pada `LabOrderCancelHandler` ke `ILabBillingIntegration.CancelCharge`.
2. **Infrastructure Layer (`Bilreg.Infrastructure.LabContext.Integration`)**:
   - Mengimplementasikan `ChargeOrder` di `LabBillingIntegration` dengan logika dual-path (pasien internal via `_regRepo` vs pasien eksternal via `_labExternalRepo`), resolusi tarif & penjamin, dan persistensi `TrsBill` via `AddBillAppService`.
   - Mengimplementasikan `CancelCharge` di `LabBillingIntegration` dengan pengecekan status `TataRekening` (`Finalized`/`Lunas` $\rightarrow$ ditolak, `Opened` $\rightarrow$ void/delete tagihan).
3. **Domain Layer (`Bilreg.Domain.LabContext.LabOrderFeature`)**:
   - Menambahkan guard invariant pada `LabOrderModel.CollectSpecimen`: pemeriksaan rutin wajib berstatus `Charged`, sedangkan pemeriksaan Cito (`IsCito == true`) diizinkan langsung dari `Ordered`.
4. **Test Suite (`Bilreg.Test.LabContext.LabOrderFeature`)**:
   - Memperbarui test mock dan skenario pada `LabOrderChargeHandlerTest`, `LabOrderCancelHandlerTest`, `LabOrderCollectSpecimenHandlerTest`, `LabBillingIntegrationTest`, dan `LabOrderModelTest`.

---

# 3. Dependencies

- `b09-bilreg-api` .NET 8 codebase
- Modul Tata Rekening (`Bilreg.Application.PaymentContext.TrsBillingFeature` & `TataRekeningFeature`)
- Existing `LabOrderModel`, `ILabOrderRepo`, `AddBillAppService`

---

# 4. Progress Summary

| Phase | Title | Implementation Status | Review Status | Progress |
|---|---|---|---|---|
| P1 | Integration Contracts | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P2 | Infrastructure Realization | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P3 | Application Handlers Refactoring | NOT-STARTED | NOT-REVIEWED | 0/2 |
| P4 | Domain Invariants | NOT-STARTED | NOT-REVIEWED | 0/1 |
| P5 | Verification & Test Alignment | NOT-STARTED | NOT-REVIEWED | 0/1 |

---

# 5. Phases

## P1 - Integration Contracts

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P1-S01: Contract Expansion for ILabBillingIntegration

Title: Definisi Kontrak Charge dan Cancel pada ILabBillingIntegration  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mendefinisikan DTO request/result dan memperluas port `ILabBillingIntegration` untuk mendukung eksekusi charge dan pembatalan finansial secara terstandar.  
Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Antarmuka `ILabBillingIntegration` memuat method `ChargeOrder(LabOrderModel order, string userId, DateTime occurredAt)` (atau `LabBillingChargeOrderRequest`) yang mengembalikan string billing reference ID.
- Antarmuka `ILabBillingIntegration` memuat method `CancelCharge(string orderId, string regId, string userId, string reason, DateTime cancelledAt)`.
- DTO dan tipe pengecualian `LabBillingChargeException` tertata rapi di namespace `Bilreg.Application.LabContext.LabOrderFeature.Integration`.

Implementation Notes:
- Didefinisikan DTO `LabBillingCancelRequest` bertipe record `(OrderId, RegId, UserId, Reason, CancelledAt)` di namespace `Bilreg.Application.LabContext.LabOrderFeature.Integration`.
- Didefinisikan DTO pendukung `LabBillingChargeOrderRequest` dan `LabBillingChargeResult` di namespace `Bilreg.Application.LabContext.LabOrderFeature.Integration`.
- Dilengkapi tipe pengecualian `LabBillingChargeException` dengan konstruktor standar `.NET` (`()`, `(message)`, `(message, innerException)`).
- Diperluas antarmuka `ILabBillingIntegration` dengan method `ChargeOrder(LabOrderModel order, string userId, DateTime occurredAt)` dan `CancelCharge(string orderId, string regId, string userId, string reason, DateTime cancelledAt)` serta overload `CancelCharge(LabBillingCancelRequest request)`. Kontrak lama `CreateTindakan` dan `ValidateReleaseEligibility` tetap dipertahankan.
- Diimplementasikan signature metode `ChargeOrder` dan `CancelCharge` pada adapter konkret `LabBillingIntegration` di `Bilreg.Infrastructure.LabContext.Integration` sehingga seluruh solusi terkompilasi bersih tanpa error.
- Ditambahkan pengujian unit pada `LabBillingIntegrationTest` untuk memverifikasi eksekusi `ChargeOrder` dan `CancelCharge`. Seluruh pengujian lulus 100%.

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/Integration/LabBillingCancelRequest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/Integration/LabBillingChargeOrderRequest.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/Integration/LabBillingChargeException.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/Integration/ILabBillingIntegration.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabBillingIntegration.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabBillingIntegrationTest.cs`

---

## P2 - Infrastructure Realization

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P2-S02: Implementation of LabBillingIntegration Adapter

Title: Realisasi Pembebanan dan Void Tagihan di LabBillingIntegration  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Mengimplementasikan logika charge terpadu (pasien rawat jalan/inap/darurat dan pasien eksternal lab) serta kompensasi pembatalan pada `LabBillingIntegration`.  
Depends On: P1-S01  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabBillingIntegration.ChargeOrder` mengorkestrasi lookup `IRegRepo`, `IJaminanRepo`, `ITarifRepo`, `INilaiTarifRepo`, `IKomponenRepo`, atau `ILabExternalRepo`.
- Memanggil `IAddBillAppService.FromLabOrderItem` untuk setiap item order.
- Menghasilkan billing reference ID yang valid (misal ID bill gabungan atau bill pertama).
- `LabBillingIntegration.CancelCharge` memeriksa `ITataRekeningRepo`: melempar exception jika status `Finalized`/`Lunas`/ada pembayaran; menghapus seluruh `TrsBill` terkait jika status `Opened`.

Implementation Notes:
- Diimplementasikan `ChargeOrder` pada `LabBillingIntegration` dengan dual-path resolution (pasien admisi internal via `_regRepo` vs pasien eksternal via `_labExternalRepo`), resolusi tarif & jaminan, dan persistensi tagihan via `_addBillAppService.FromLabOrderItem`.
- Seluruh pengecualian transaksi finansial dibungkus menjadi `LabBillingChargeException(ex.Message, ex)`.
- Mengembalikan reference ID penagihan yang dipotong maksimal 12 karakter untuk kompatibilitas kolom `BillingTindakanId VARCHAR(12)`.
- Diimplementasikan `CancelCharge` pada `LabBillingIntegration` dengan validasi status `TataRekening`: menolak pembatalan jika status `Finalized`, `Lunas`, atau memiliki pembayaran; menghapus tagihan matching (`orderId-` atau `orderId`) jika status `Opened` dan memanggil `_tataRekeningRepo.SaveChanges`.
- Ditambahkan konstruktor injeksi DI dengan penanda `[ActivatorUtilitiesConstructor]` dan mempertahankan konstruktor parameterless untuk fleksibilitas testing.
- Ditambahkan 15 unit test komprehensif pada `LabBillingIntegrationTest` yang menguji skenario sukses charge, dual-path external vs admisi, penanganan error/wrapping exception, batas 12 karakter ID, serta skenario cancel charge pada berbagai status Tata Rekening. Seluruh pengujian lulus 100%.

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabBillingIntegration.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabBillingIntegrationTest.cs`

---

## P3 - Application Handlers Refactoring

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P3-S03: Refactor LabOrderChargeHandler

Title: Pembersihan Dependensi dan Perampingan LabOrderChargeHandler  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Menghapus 6 dependensi repositori billing dari `LabOrderChargeHandler` dan hanya menyisakan `ILabOrderRepo`, `ILabBillingIntegration`, dan `ITglJamProvider`.  
Depends On: P2-S02  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Constructor `LabOrderChargeHandler` hanya menyuntikkan 3 dependensi inti.
- Alur eksekusi memvalidasi `order.Charge(userId)`, memanggil `_labBillingIntegration.ChargeOrder(...)`, memanggil `order.MarkCharged(...)` jika sukses, atau `order.RecordBillingError(...)` jika gagal.
- Transaksi perubahan disimpan via `_labOrderRepo.SaveChanges(order)` dalam `TransHelper.NewScope()`.

Implementation Notes:
- Menghapus 7 dependensi master billing dan app service (`IRegRepo`, `ITarifRepo`, `INilaiTarifRepo`, `IKomponenRepo`, `IJaminanRepo`, `IAddBillAppService`, `ILabExternalRepo`) dari `LabOrderChargeHandler`.
- Konstruktor `LabOrderChargeHandler` kini ramping dengan hanya 3 dependensi: `ILabOrderRepo`, `ILabBillingIntegration`, dan `ITglJamProvider`.
- Seluruh logika resolusi tarif, penjamin, dan penagihan pasien admisi maupun eksternal didelegasikan ke `_labBillingIntegration.ChargeOrder(...)`.
- Handler mengeksekusi `order.Charge(userId)`, membungkus pemanggilan `ChargeOrder` dalam try-catch untuk memanggil `order.MarkCharged(billingRefId, ...)` jika sukses atau `order.RecordBillingError(ex.Message, ...)` jika terjadi `LabBillingChargeException`, serta menyimpan agregat order dalam `TransHelper.NewScope()`.
- Menghapus method helper yang telah dipindahkan ke Infrastructure pada P2-S02 (`ChargeExternalPatient`, `ChargeAdmisiPatient`, `LoadReg`, dll.).
- Memperbarui unit test pada `LabOrderChargeHandlerTest` agar menggunakan konstruktor 3 dependensi dan mock `ILabBillingIntegration.ChargeOrder` untuk skenario sukses, kegagalan billing, validasi input, order deferred, dan order not found. Seluruh pengujian (7 test) lulus 100%.

Changed Files:
- `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderChargeCmd.cs`
- `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderChargeHandlerTest.cs`

### P3-S04: Refactor LabOrderCancelHandler

Title: Delegasi Pembatalan Tagihan Kasir ke ILabBillingIntegration  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Menghilangkan dependensi langsung `ITataRekeningRepo` dari `LabOrderCancelHandler` dan mendelegasikannya ke `_labBillingIntegration.CancelCharge`.  
Depends On: P2-S02  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabOrderCancelHandler` tidak lagi menginjeksi `ITataRekeningRepo`.
- Jika order berstatus `Charged`, handler memanggil `_labBillingIntegration.CancelCharge(...)` sebelum memanggil `order.Cancel(...)`.
- Guard spesimen tetap terjaga (`EX-03`).

---

## P4 - Domain Invariants

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P4-S05: Enforce Sampling Invariant in LabOrderModel

Title: Penegakan Aturan Pengambilan Sampel Berbasis Status Charged dan Cito  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Memperketat metode `LabOrderModel.CollectSpecimen` agar mewajibkan status `Charged` untuk pemeriksaan rutin, namun tetap mengizinkan pengambilan spesimen langsung dari `Ordered` untuk pemeriksaan Cito.  
Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Jika `LabOrderStatus == Ordered` dan `!IsCito`, `CollectSpecimen` melempar `InvalidOperationException` dengan pesan bahwa order rutin wajib berstatus Charged.
- Jika `LabOrderStatus == Ordered` dan `IsCito == true`, `CollectSpecimen` berhasil memajukan status menjadi `Collected`.
- Jika `LabOrderStatus == Charged`, `CollectSpecimen` berhasil memajukan status menjadi `Collected`.

---

## P5 - Verification & Test Alignment

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

### P5-S06: Unit Test Suite Synchronization & Verification

Title: Sinkronisasi Seluruh Unit Test Suite Terkait Charge, Cancel, dan Sampling  
Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED  
Objective: Memperbarui mock dan assertion pada test suite unit testing untuk memastikan seluruh test lulus (*100% Green Build*).  
Depends On: P3-S03, P3-S04, P4-S05  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabOrderChargeHandlerTest` berjalan bersih dengan mock `ILabBillingIntegration`.
- `LabOrderCancelHandlerTest` memverifikasi skenario delegasi pembatalan tagihan.
- `LabOrderCollectSpecimenHandlerTest` dan `LabOrderModelTest` memverifikasi rule invariant rutin vs Cito.
- `dotnet test` untuk seluruh suite LabContext berjalan lulus tanpa kegagalan.

---

# 6. Change Log

- 2026-10-17: Rencana implementasi awal dibentuk berdasarkan hasil sesi alignment `/grill-me` dan dokumen arsitektur `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`.
