---
Title: Pengambilan Spesimen Laboratorium Implementation Plan (Cakupan HIS)
Code: OC-LAB-SAMPLE-COLLECTION
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-16
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan penyesuaian teknis backend untuk kapabilitas **Pengambilan Spesimen Laboratorium (Sample Collection)** pada sistem HIS (Hospital Information System / SIMRS) pada repositori `b09-bilreg-api` berbasis agregat `LabOrderFeature` yang telah ada.

Tujuan teknis:
1. Menyelaraskan logika kelayakan status order (*order eligibility*) pada metode domain [`LabOrderModel.CollectSpecimen()`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs) agar mengizinkan pencatatan pengambilan spesimen untuk order yang berstatus `Charged` (standar Rawat Jalan & Pasien Eksternal) maupun `Ordered` (untuk Rawat Inap & IGD di mana tindakan penagihan kasir berjalan paralel).
2. Mempertegas penolakan informatif dengan `InvalidOperationException` apabila pengambilan spesimen dicoba pada order yang masih berstatus penundaan (`Deferred`) atau telah dibatalkan (`Cancelled`).
3. Memperbarui dan memperluas test suite unit testing pada [`LabOrderCollectSpecimenHandlerTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCollectSpecimenHandlerTest.cs) dan [`LabOrderModelTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderModelTest.cs) untuk memverifikasi seluruh skenario kelayakan status order secara menyeluruh (*100% test pass*).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- FEATURE: [OC-LAB-SAMPLE-COLLECTION-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-SAMPLE-COLLECTION-FEATURE.md)
- FEASIBILITY-ASSESSMENT: [OC-LAB-SAMPLE-COLLECTION-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-SAMPLE-COLLECTION-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- ISSUE: [OC-LAB-SAMPLE-COLLECTION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-SAMPLE-COLLECTION-ISSUE.md) (`ISSUE-LAB-SAMPLE-COLLECTION-001`)

Architecture Applicability: ARCHITECTURE-NOT-REQUIRED
No architectural target-state artifact was required. Implementation relies on existing technical structure. Approved feasibility decisions are authoritative for the change. The current codebase is the source of current technical truth.

---

# 2. Planning Scope

Target repository:
- `b09-bilreg-api`

In Scope:
1. **Domain Layer**:
   - Penyesuaian guard status kelayakan pada [`LabOrderModel.CollectSpecimen()`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs#L472-L498) agar mengizinkan `LabOrderStatus is Charged or Ordered`.
   - Penegakan guard penolakan eksplisit jika status order `Deferred` atau `Cancelled`.
2. **Automated Unit Testing**:
   - Pembaruan unit test [`LabOrderCollectSpecimenHandlerTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCollectSpecimenHandlerTest.cs) untuk memastikan order `Ordered` berhasil disampling.
   - Penambahan unit test untuk memastikan order `Deferred` dan `Cancelled` gagal disampling.
   - Verifikasi regresi menyeluruh pada test suite `LabContext.LabOrderFeature`.

Out of Scope:
1. Modifikasi skema basis data SQL DDL (tabel `BILRG_LabOrder` telah memiliki kolom `CollectedDate`, `CollectedUserId`, dan `CollectionNote`).
2. Pembuatan agregat baru atau tabel fisik tabung terpisah (merupakan wewenang LIS).
3. Modifikasi kontrak endpoint API `LabOrderController` (endpoint `[HttpPatch("collect")]` dan `[HttpGet("collectionPreparation")]` telah tersedia dan stabil).

---

# 3. Dependencies

External dependencies:
- .NET 8 / C# 12
- MediatR
- FluentAssertions, Moq, xUnit pada `Bilreg.Test`

Slice dependency rules:
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Dependensi mengacu hanya pada Slice ID yang valid dalam rencana ini.
- Pemenuhan dependensi mensyaratkan status implementasi slice rujukan bernilai `IMPLEMENTED`.
- Kepuasan dependensi tidak mensyaratkan status review `GO`.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Domain Logic Enhancement | IMPLEMENTED | GO | 1/1 |
| P2 - Test Suite Verification | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Domain Logic Enhancement

Implementation Status: IMPLEMENTED
Review Status: GO

### P1-S01
Title: Relax Eligibility Guard on LabOrderModel.CollectSpecimen
Implementation Status: IMPLEMENTED
Review Status: GO
Objective: Memperbarui metode `CollectSpecimen` pada `LabOrderModel` agar mengizinkan pengambilan spesimen untuk order berstatus `Charged` dan `Ordered`, serta menolak secara eksplisit dengan exception informatif jika order berstatus `Deferred` atau `Cancelled`.
Depends On: None
Repository: `b09-bilreg-api`
Completion Criteria:
1. `LabOrderModel.CollectSpecimen` mengizinkan eksekusi apabila `LabOrderStatus == LabOrderStatusEnum.Charged` atau `LabOrderStatus == LabOrderStatusEnum.Ordered`.
2. Melemparkan `InvalidOperationException` jika `LabOrderStatus == LabOrderStatusEnum.Deferred` dengan pesan: $"LabOrder {OrderId} berstatus Deferred; pengambilan spesimen tidak diperbolehkan saat order ditunda."
3. Melemparkan `InvalidOperationException` jika `LabOrderStatus == LabOrderStatusEnum.Cancelled` dengan pesan: $"LabOrder {OrderId} sudah dibatalkan; pengambilan spesimen tidak diperbolehkan."
4. Melemparkan `InvalidOperationException` jika order berstatus selain `Charged` atau `Ordered`.
5. Kompilasi `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Domain/Bilreg.Domain.csproj` berhasil tanpa error.
Notes:
- Mengubah file `src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs`.
Implementation Notes:
- Memperbarui metode `CollectSpecimen` pada `LabOrderModel` agar mengizinkan pengambilan spesimen untuk order berstatus `Charged` maupun `Ordered`.
- Menambahkan penolakan eksplisit dengan `InvalidOperationException` untuk status `Deferred` dengan pesan: $"LabOrder {OrderId} berstatus Deferred; pengambilan spesimen tidak diperbolehkan saat order ditunda."
- Menambahkan penolakan eksplisit dengan `InvalidOperationException` untuk status `Cancelled` dengan pesan: $"LabOrder {OrderId} sudah dibatalkan; pengambilan spesimen tidak diperbolehkan."
- Memperbarui fallback guard agar melempar `InvalidOperationException` dengan pesan: $"LabOrder {OrderId} berstatus {LabOrderStatus}; pengambilan spesimen hanya diperbolehkan dari Charged atau Ordered." jika order berstatus selain `Charged` atau `Ordered`.
- Mempertahankan pemeriksaan `AuditTrail.IsVoided`.
- Memverifikasi kompilasi `dotnet build b09-bilreg-api/src/bilreg/Bilreg.Domain/Bilreg.Domain.csproj` berhasil tanpa error.
Changed Files:
- `src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs`

---

## P2 - Test Suite Verification

Implementation Status: IMPLEMENTED
Review Status: GO

### P2-S02
Title: Unit Test Suite Alignment & Invariant Verification
Implementation Status: IMPLEMENTED
Review Status: GO
Objective: Memperbarui dan melengkapi test case pada `LabOrderCollectSpecimenHandlerTest.cs` dan `LabOrderModelTest.cs` untuk memvalidasi seluruh kondisi status kelayakan order saat sampling (`Ordered -> Success`, `Charged -> Success`, `Deferred -> Throws`, `Cancelled -> Throws`, `Voided -> Throws`).
Depends On: P1-S01
Repository: `b09-bilreg-api`
Completion Criteria:
1. `LabOrderCollectSpecimenHandlerTest.cs` memiliki test case `Handle_OrderedOrder_PersistsCollected` yang memverifikasi pengambilan spesimen pada order berstatus `Ordered` berhasil dan tersimpan sebagai `Collected`.
2. `LabOrderCollectSpecimenHandlerTest.cs` memiliki test case `Handle_ChargedOrder_PersistsCollected` yang memverifikasi pengambilan spesimen pada order berstatus `Charged` berhasil dan tersimpan sebagai `Collected`.
3. `LabOrderCollectSpecimenHandlerTest.cs` memiliki test case `Handle_DeferredOrder_ThrowsInvalidOperationException` yang memverifikasi penolakan sampling saat order `Deferred`.
4. `LabOrderCollectSpecimenHandlerTest.cs` memiliki test case `Handle_CancelledOrder_ThrowsInvalidOperationException` yang memverifikasi penolakan sampling saat order `Cancelled`.
5. Seluruh pengujian unit di `Bilreg.Test` yang relevan dengan `LabOrderFeature` lulus 100% (`dotnet test`).
Notes:
- Mengubah file `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCollectSpecimenHandlerTest.cs` dan/atau `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderModelTest.cs`.
Implementation Notes:
- Memperbarui `LabOrderCollectSpecimenHandlerTest.cs` dengan menambahkan `Handle_OrderedOrder_PersistsCollected` untuk memverifikasi transisi order `Ordered` ke `Collected` dan pemanggilan `SaveChanges`.
- Mempertahankan `Handle_ChargedOrder_PersistsCollected` untuk memverifikasi order `Charged`.
- Menggantikan `Handle_NotCharged_ThrowsBeforeSave` dengan `Handle_DeferredOrder_ThrowsInvalidOperationException` dan `Handle_CancelledOrder_ThrowsInvalidOperationException` untuk memvalidasi penolakan sampling dan pencegahan persistensi saat order `Deferred` atau `Cancelled`.
- Memperbarui `LabOrderModelTest.cs` dengan mengganti `CollectSpecimen_WhenOrdered_Throws` menjadi `CollectSpecimen_FromOrdered_SetsCollectedAndCollectionInfo`.
- Menyelaraskan assertion pesan exception pada `CollectSpecimen_WhenDeferred_Throws` dan menambahkan `CollectSpecimen_WhenCancelled_Throws` serta `CollectSpecimen_WhenAlreadyCollected_Throws`.
- Memverifikasi eksekusi pengujian unit `Bilreg.Test` untuk `LabOrderFeature` lulus 100% (160 passed).
Changed Files:
- `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCollectSpecimenHandlerTest.cs`
- `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderModelTest.cs`

---

# 6. Change Log

- 2026-10-16: Rencana implementasi awal dibuat dengan mode FEATURE-PLANNING dan ARCHITECTURE-NOT-REQUIRED berdasarkan hasil evaluasi Feasibility Assessment v1.1 dan konsensus cakupan HIS.
