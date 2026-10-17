---
Title: Feasibility Assessment - Pengambilan Spesimen Laboratorium (Cakupan HIS)
Code: OC-LAB-SAMPLE-COLLECTION
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-10-16
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan penyelarasan (*CHANGE-REQUEST*) untuk menyederhanakan kapabilitas pencatatan pengambilan spesimen laboratorium (*Sample Collection*) agar selaras dan proporsional untuk cakupan Hospital Information System (HIS / SIMRS), berbasis pada *codebase* eksisting di [`Bilreg.Domain/LabContext/LabOrderFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature).

Referenced artifacts:

- ISSUE: [OC-LAB-SAMPLE-COLLECTION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-SAMPLE-COLLECTION-ISSUE.md) (`ISSUE-LAB-SAMPLE-COLLECTION-001`)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md) (`LAB-COLLECT`, `LAB-ORDER`)
- OUTCOME: [OC-LAB-SAMPLE-COLLECTION.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-SAMPLE-COLLECTION.md) & [OC-08-04-SAMPLE-COLLECTION-OUTCOME.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/_review/OC-08-04-SAMPLE-COLLECTION-OUTCOME.md)
- FEATURE: [OC-LAB-SAMPLE-COLLECTION-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-SAMPLE-COLLECTION-FEATURE.md)

## Objective

Menilai kelayakan teknis implementasi pengambilan spesimen dalam batasan wewenang HIS; mengonfirmasi bahwa fungsionalitas ini tidak memerlukan sistem manajemen tabung atau instrumen LIS yang rumit; membuktikan bahwa *codebase* eksisting di [`LabOrderFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature) telah mencukupi kebutuhan tersebut; serta merumuskan penyesuaian minimal yang diperlukan pada guard status kelayakan sampling.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan audit kode sumber backend `b09-bilreg-api`:

## Existing Behavior

1. **Struktur Model Agregat Eksisting ([`LabOrderModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs))**:
   - Agregat `LabOrderModel` telah memiliki properti `CollectionInfo` bertipe [`CollectionInfoType`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/CollectionInfoType.cs) yang menyimpan:
     - `CollectedDate` (waktu pelaksanaan sampling).
     - `CollectedUserId` (identitas staf pengambil/pencatat).
     - `CollectionNote` (catatan operasional jika ada hal khusus).
   - Telah memiliki metode `CollectSpecimen(string userId, CollectionInfoType collectionInfo, DateTime collectedAt)`.
   - Telah memiliki status siklus hidup [`LabOrderStatusEnum.Collected`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderStatusEnum.cs).
   - Telah memiliki proteksi pembatalan: metode `Cancel()` pada `LabOrderModel` secara tegas memblokir pembatalan order jika status sudah `Collected`, `Recorded`, `Verified`, atau `Released`.
   - Telah memiliki alur penghentian darurat pasca-sampling: metode `Terminate()` dapat digunakan untuk menutup order yang sudah `Collected` apabila terjadi kondisi medis darurat atau pembatalan tak terhindarkan.

2. **Use Case Intake Sampling Eksisting ([`LabOrderCollectSpecimenCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCollectSpecimenCmd.cs))**:
   - Command MediatR menerima `(OrderId, UserId, CollectionNote, CollectedDate)`.
   - Handler memvalidasi parameter, memanggil `order.CollectSpecimen()`, dan mempersistensikan perubahan via `_labOrderRepo.SaveChanges(order)` dalam *transaction scope*.

3. **Kueri Panduan Tabung Eksisting ([`LabCollectionPreparationQuery.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabCollectionPreparationQuery.cs) & [`LabCollectionPreparationDal.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabCollectionPreparationDal.cs))**:
   - Menyediakan panduan persiapan flebotomis melalui pengelompokan kebutuhan tabung vacutainer (*vacutainer grouping rule*) secara *read-only* dari baris rincian pemeriksaan order (`BILRG_LabOrderItem`).
   - Flebotomis dapat melihat jenis tabung (EDTA, Serum, Citrate, Heparin) dan estimasi jumlah tabung yang harus disiapkan sebelum menemui pasien.

4. **Skema Basis Data SQL Eksisting ([`BILRG_LabOrder.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrder.sql))**:
   - Tabel `BILRG_LabOrder` telah memiliki kolom-kolom persisten yang lengkap:
     - `CollectedDate DATETIME NOT NULL DEFAULT('3000-01-01')`
     - `CollectedUserId VARCHAR(50) NOT NULL DEFAULT('')`
     - `CollectionNote VARCHAR(200) NOT NULL DEFAULT('')`
   - Tidak memerlukan penambahan kolom baru atau tabel baru.

## Existing Constraints

1. **Batasan Validasi Kelayakan Status Saat Ini**:
   - Pada baris 481-484 `LabOrderModel.cs`, terdapat pengecekan guard:
     ```csharp
     if (LabOrderStatus != LabOrderStatusEnum.Charged)
         throw new InvalidOperationException(
             $"LabOrder {OrderId} berstatus {LabOrderStatus}; pengambilan spesimen hanya diperbolehkan dari Charged.");
     ```
   - Aturan ini mengunci bahwa sampling hanya dapat dilakukan dari status `Charged`. Untuk Rawat Inap dan IGD di mana tindakan penagihan kasir berjalan paralel, kondisi ini perlu dilonggarkan agar juga mengizinkan pengambilan sampel dari status `Ordered`.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| **GAP-001** | MAJOR | **Kekakuan Guard Status Kelayakan Sampling pada `LabOrderModel.CollectSpecimen()`.** Metode domain saat ini hanya mengizinkan sampling dari status `Charged`. Pada alur operasional Rawat Inap dan IGD, sampling darah sering kali harus dilakukan segera saat order masih berstatus `Ordered` tanpa menunggu penagihan billing kasir selesai. Di samping itu, perlu penegakan guard eksplisit bahwa order berstatus `Deferred` (masih puasa/persiapan) atau `Cancelled` dilarang disampling. |

*(Catatan: Kebutuhan agregat baru, tabel barcode fisik per tabung, dan scoring lisis/ikterik/lipemik telah dipastikan Out of Scope untuk sistem HIS).*

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| **OQ-001** | Apakah order berstatus `Ordered` dari semua unit atau khusus unit ranap/IGD yang diizinkan untuk disampling langsung? | Menentukan apakah guard pengecekan status cukup `LabOrderStatus is Charged or Ordered` atau perlu memeriksa tipe layanan perujuk. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| **ASM-001** | Seluruh detail teknis LIS (manajemen barcode analyzer per tabung, kalibrasi instrumen, rack slot, dan pengukuran indeks serum) dikelola oleh sistem LIS eksternal atau instrumen analitik, bukan oleh SIMRS / HIS. |
| **ASM-002** | Skema basis data tabel `BILRG_LabOrder` dan seluruh DTO DAL terkait `CollectionInfo` yang ada saat ini sudah 100% memadai dan tidak memerlukan migrasi DDL SQL baru. |
| **ASM-003** | Seluruh 156 unit test fungsional pada `Bilreg.Test/LabContext/LabOrderFeature` tetap valid dan menjadi jaring pengaman regresi. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| **RISK-001** | Pasien rawat jalan mengambil sampel darah sebelum melunasi tagihan kasir jika status `Ordered` diizinkan tanpa batasan. | MINOR | Pada antarmuka UI loket sampling rawat jalan, filter kerja sampling tetap mengutamakan status `Charged`, sementara sistem backend mengizinkan fleksibilitas `Ordered` untuk mendukung pelayanan ranap/IGD. |

---

# 7. Recommendations

## Opsi Tunggal: Penyesuaian Guard Kelayakan pada `LabOrderModel.CollectSpecimen()` (Direkomendasikan)

Mempertahankan 100% arsitektur dan *codebase* yang telah ada pada `Bilreg.Domain.LabContext.LabOrderFeature`, dengan hanya melakukan penyesuaian guard status:
```csharp
if (LabOrderStatus != LabOrderStatusEnum.Charged && LabOrderStatus != LabOrderStatusEnum.Ordered)
    throw new InvalidOperationException(
        $"LabOrder {OrderId} berstatus {LabOrderStatus}; pengambilan spesimen hanya diperbolehkan dari Charged atau Ordered.");
```
Serta memastikan bahwa jika order berstatus `Deferred` atau `Cancelled`, sistem melemparkan pesan kesalahan yang informatif.

### Advantages

- **Zero Database Migration**: Tidak ada tabel baru, tidak ada kolom baru, dan tidak ada risiko *downtime*.
- **Zero Architecture Overhead**: Tidak memerlukan pembuatan agregat baru atau orkestrasi antar-agregat yang kompleks.
- **Maksimal Efisiensi**: Memanfaatkan kode yang sudah teruji 100% (`CollectionInfoType`, `LabOrderCollectSpecimenCmd`, `LabCollectionPreparationDal`).
- **Tepat Sasaran untuk HIS**: Memenuhi kebutuhan esensial rumah sakit (fakta sampling, penguncian order, penghitungan TAT, panduan vacutainer).

---

# 8. Gap Closure

## GAP-001: Kekakuan Guard Status Kelayakan Sampling pada `LabOrderModel.CollectSpecimen()`
- **Status:** CLOSED
- **Decision:** Memperbarui guard validasi pada `LabOrderModel.CollectSpecimen` agar mengizinkan eksekusi pengambilan spesimen apabila order berstatus `Charged` ATAU `Ordered`. Menolak secara tegas jika order berstatus `Deferred` (masih dalam masa penundaan klinis) atau `Cancelled`.
- **Rationale:** Memberikan fleksibilitas bagi pelayanan gawat darurat (IGD) dan rawat inap tanpa mengorbankan integritas data order.
- **Impact:** Perubahan minimal 1-3 baris kode logika pada `LabOrderModel.cs` dan pembaruan unit test terkait.
- **Architecture Impact:** Zero architectural impact (arsitektur teknis tetap).
- **Resolved By:** Analyst & User (via Grill-Me Alignment)
- **Resolved Date:** 2026-10-16

---

## OQ-001: Validasi Status Kelayakan Berdasarkan Unit
- **Status:** CLOSED
- **Decision:** Validasi status di tingkat domain model cukup mengecek `LabOrderStatus is Charged or Ordered`. Pemilahan alur kasir rawat jalan dikendalikan secara visual pada kueri filter antrean sampling (*worklist UI*).
- **Rationale:** Menjaga kesederhanaan model domain dan menghindari dependensi rumit terhadap jenis layanan.
- **Impact:** Logika domain tetap bersih dan fleksibel.
- **Architecture Impact:** Tidak ada.
- **Resolved By:** Analyst & User
- **Resolved Date:** 2026-10-16

---

# 9. Architecture Applicability

Record the Architecture Applicability decision.

## Decision

**ARCHITECTURE-NOT-REQUIRED**

## Rationale

Struktur teknis eksisting pada [`LabOrderFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature) telah menyediakan seluruh komponen yang disyaratkan oleh cakupan HIS (model domain, value object, command intake, query panduan tabung, dan kolom basis data). Perubahan yang disepakati hanya berupa penyesuaian guard status order pada metode `CollectSpecimen`, tanpa memerlukan komponen baru, tabel baru, maupun perubahan batas arsitektural.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated (Architecture-Not-Required)

## Status

**READY-FOR-PLANNING**

## Notes

Seluruh kesenjangan telah tertutup dengan solusi penyederhanaan yang elegan dan minim dampak regresi. Berdasarkan evaluasi Arsitek, keputusan ARCHITECTURE-NOT-REQUIRED telah divalidasi dan disahkan. Dengan ini, gerbang **READY-FOR-PLANNING** resmi diberikan untuk melanjutkan ke tahapan pembuatan IMPLEMENTATION-PLAN.

---

# 11. References

Referenced artifacts:

- ISSUE: [OC-LAB-SAMPLE-COLLECTION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-SAMPLE-COLLECTION-ISSUE.md)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md)
- OUTCOME: [OC-LAB-SAMPLE-COLLECTION.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-SAMPLE-COLLECTION.md)
- FEATURE: [OC-LAB-SAMPLE-COLLECTION-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-SAMPLE-COLLECTION-FEATURE.md)

Referenced codebase locations:

- [`Bilreg.Domain/LabContext/LabOrderFeature/CollectionInfoType.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/CollectionInfoType.cs)
- [`Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs)
- [`Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCollectSpecimenCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCollectSpecimenCmd.cs)
- [`Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabCollectionPreparationQuery.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabCollectionPreparationQuery.cs)
- [`Bilreg.Infrastructure/LabContext/LabOrderFeature/LabCollectionPreparationDal.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabCollectionPreparationDal.cs)
- [`Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrder.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrder.sql)
