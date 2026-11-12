---
Title: Pembebanan Biaya Pemeriksaan Laboratorium Architecture
Code: OC-LAB-ORDER-CHARGE
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-10-17
---

# 1. Overview

Arsitektur ini merealisasikan spesifikasi fitur **[OC-LAB-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-CHARGE-FEATURE.md)** yang diturunkan dari outcome bisnis **[OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)**.

Arsitektur ini menetapkan penyelarasan arsitektur teknis pembebanan biaya (*billing charge*) dan kompensasi pembatalan pada backend BILREG API (`b09-bilreg-api`):
1. **Penerapan Clean Architecture & Decoupling pada `LabOrderChargeHandler`**:
   - Menghilangkan *architectural leak* di mana handler sebelumnya menginjeksi langsung 6 repository master billing (`IRegRepo`, `ITarifRepo`, `INilaiTarifRepo`, `IKomponenRepo`, `IJaminanRepo`, `ILabExternalRepo`).
   - Memindahkan seluruh logika resolusi tarif, penjamin, dan pemanggilan `AddBillAppService` ke dalam implementasi adapter infrastruktur `LabBillingIntegration`.
2. **Standardisasi Kontrak Port `ILabBillingIntegration`**:
   - Memperluas antarmuka `ILabBillingIntegration` untuk mencakup metode `ChargeOrder` dan `CancelCharge` yang bersih dan independen.
3. **Penegakan Invariant Siklus Hidup & Urgensi Cito pada Pengambilan Sampel**:
   - Memperbarui invariant `LabOrderModel.CollectSpecimen`: pemeriksaan Rutin wajib berstatus `Charged`, sedangkan pemeriksaan Cito (`IsCito == true`) diizinkan langsung diambil spesimennya dari status `Ordered`.
4. **Delegasi Kompensasi Finansial Pembatalan Order**:
   - Memindahkan koordinasi pembatalan tagihan kasir dari `LabOrderCancelHandler` ke `ILabBillingIntegration.CancelCharge`, memastikan rekening yang telah `Final`/`Lunas` tidak dapat dibatalkan dan seluruh baris tagihan terkait order dibersihkan.
5. **Kompatibilitas Skema Database**:
   - Mempertahankan kolom `BillingTindakanId` dan `BillingLastError` pada tabel `BILRG_LabOrder` tanpa memerlukan migrasi skema database yang memecah kompatibilitas historis (*zero-breaking schema*).

# 2. Architectural Basis

## Business Context

Pemeriksaan laboratorium yang telah tercatat secara resmi (`Ordered`) harus dibukukan beban biayanya ke dalam sistem penagihan rumah sakit (`TRK-BILLING`) agar biaya tindakan terikat pada rekening kunjungan pasien. Hasil pembebanan ini memberikan otorisasi finansial bagi petugas laboratorium untuk melaksanakan prosedur flebotomi/sampling rutin, sekaligus menjamin akurasi billing rumah sakit.

## Analysis Input

Berdasarkan kesepakatan desain alignment `/grill-me` dan dokumen pendukung:
- **Prinsip Modular Monolith**: LWF (`LabContext`) dan Billing (`PaymentContext` / `TRK`) berada dalam solusi yang sama. Komunikasi antar-konteks dilakukan melalui in-process application integration service, bukan HTTP atau cross-context direct database queries.
- **Pembersihan Dualisme Codebase**: Menghilangkan stub palsu (`TDK-FAKE-0001`) dan menyatukan penagihan resmi ke `AddBillAppService` di balik port `ILabBillingIntegration`.
- **Hierarchical Urgency Rule**: Memanfaatkan bendera `IsCito` yang sudah ada pada `LabOrderModel` untuk mendukung bypass billing darurat pada pengambilan sampel.

```text
[OC-LAB-ORDER-LAB.md] + [OC-LAB-ORDER-CHARGE-FEATURE.md]
                          ↓
          [OC-LAB-ORDER-CHARGE-ARCHITECTURE]
```

# 3. Scope

## Included

1. **Domain Layer (`Bilreg.Domain.LabContext.LabOrderFeature`)**:
   - Penegakan invariant pada `LabOrderModel.CollectSpecimen(...)` berbasis status `Charged` dan status urgensi `IsCito`.
2. **Application Layer (`Bilreg.Application.LabContext.LabOrderFeature`)**:
   - Pembaruan kontrak `ILabBillingIntegration` dengan method `ChargeOrder(...)` dan `CancelCharge(...)`.
   - Pembersihan `LabOrderChargeHandler` dari dependensi repository eksternal.
   - Refactoring `LabOrderCancelHandler` untuk mendelegasikan kompensasi pembatalan finansial ke `ILabBillingIntegration`.
3. **Infrastructure Layer (`Bilreg.Infrastructure.LabContext.Integration`)**:
   - Implementasi penuh `LabBillingIntegration` mencakup dual-path resolution (pasien admisi vs pasien eksternal), lookup tarif dan penjamin, serta pemanggilan `AddBillAppService`.
   - Implementasi logika verifikasi status kasir dan void bill pada `CancelCharge`.
4. **Unit Test Suite (`Bilreg.Test.LabContext.LabOrderFeature`)**:
   - Penyesuaian unit test handler dan integration test untuk memverifikasi alur charge dan cancel yang bersih.

## Excluded

1. Alur pembayaran tunai/non-tunai di loket kasir (`TRK-KASIR`).
2. Finalisasi dan penutupan rekening pasien (`TRK-FINALIZE`).
3. Algoritma penghitungan diskon kompleks atau penjaminan ganda (*co-insurance*).
4. Tindakan medis di luar instalasi laboratorium.

# 4. Technical Decisions

## AD-01: Decoupled Billing Integration Port

**Keputusan**: `LabOrderChargeHandler` tidak boleh mengetahui detail internal domain Billing/Tata Rekening (struktur registrasi, kelas, jaminan, maupun komponen tarif).
- Didefinisikan kontrak port `ILabBillingIntegration` pada `Bilreg.Application`:
  ```csharp
  public interface ILabBillingIntegration
  {
      Task<LabBillingChargeResult> ChargeOrder(LabBillingChargeRequest request, CancellationToken cancellationToken = default);
      Task CancelCharge(LabBillingCancelRequest request, CancellationToken cancellationToken = default);
      BillingReleaseValidationResult ValidateReleaseEligibility(LabBillingReleaseValidationRequest request);
  }
  ```
- `LabOrderChargeHandler` hanya berinteraksi dengan `ILabOrderRepo` dan `ILabBillingIntegration`.

## AD-02: Infrastructure-Level Dual-Path Charging Resolution

**Keputusan**: Seluruh perbedaan penanganan antara pasien internal RS (`LabOrderSourceEnum.Emr`) dan pasien eksternal (`LabOrderSourceEnum.ExternalPatient`) diisolasi di layer Infrastructure (`LabBillingIntegration`):
- **Pasien Internal**: Mengambil `RegModel` via `_regRepo`, mengekstrak penjamin, menyelesaikan tipe tarif berdasarkan jenis registrasi (Rajal vs Ranap), memuat nilai tarif, dan memanggil `AddBillAppService.FromLabOrderItem`.
- **Pasien Eksternal**: Mengambil `LabExternalModel` via `_labExternalRepo`, membentuk registrasi sintetis bertipe Umum/Kelas III, dan memanggil `AddBillAppService.FromLabOrderItem`.
- ID referensi transaksi penagihan yang dihasilkan dikembalikan ke application layer untuk disimpan pada `BillingTindakanId`.

## AD-03: Operational Trigger & Urgency Invariant Enforcement

**Keputusan**: Transisi `Ordered` $\rightarrow$ `Charged` dipicu secara eksplisit oleh aktor melalui endpoint `PATCH /charge`.
- Pada tahap pengambilan spesimen (`CollectSpecimen`), sistem menegakkan aturan invariant:
  ```csharp
  if (LabOrderStatus == LabOrderStatusEnum.Ordered && !IsCito)
  {
      throw new InvalidOperationException(
          $"LabOrder {OrderId} berstatus Ordered; pemeriksaan rutin wajib berstatus Charged sebelum pengambilan spesimen.");
  }
  ```
- Hal ini menjamin tertib administrasi untuk pemeriksaan rutin tanpa menghambat tindakan penyelamatan nyawa pada kasus Cito.

## AD-04: Non-Destructive Cancellation & Delegated Financial Compensation

**Keputusan**: Pembatalan order yang sudah `Charged` memicu kompensasi pembatalan di Tata Rekening melalui `ILabBillingIntegration.CancelCharge`:
1. `LabBillingIntegration` memeriksa status rekening pasien (`TataRekeningModel`):
   - Jika status `Finalized`, `Lunas`, atau telah terdapat rekaman pembayaran kasir: sistem melempar exception untuk menolak pembatalan order laboratorium klinis.
   - Jika status masih `Opened`: sistem menghapus/void seluruh `TrsBill` yang berasosiasi dengan order tersebut (`OrderId-ItemNo`).
2. Setelah kompensasi finansial berhasil, `LabOrderCancelHandler` memutasi status order klinis menjadi `Cancelled`.

## AD-05: Non-Breaking Schema Compatibility

**Keputusan**: Kolom database yang telah terpasang pada tabel `BILRG_LabOrder` tetap digunakan:
- `BillingTindakanId VARCHAR(12)`: Menyimpan ID referensi penagihan/billing yang diterbitkan.
- `BillingLastError VARCHAR(200)`: Menyimpan pesan kegagalan jika transaksi pembebanan tarif gagal.
- Tidak diperlukan skrip migrasi SQL baru karena kolom telah tersedia sejak M8/M9.

# 5. Component Responsibilities

| Komponen | Layer | Tanggung Jawab Utama |
|---|---|---|
| `LabOrderModel` | Domain | Menjaga invariant siklus hidup, status `Charged`, guard `CollectSpecimen` berbasis urgensi Cito, dan status `BillingLastError`. |
| `ILabBillingIntegration` | Application | Port kontrak pembebanan biaya (`ChargeOrder`), kompensasi pembatalan (`CancelCharge`), dan validasi rilis hasil. |
| `LabOrderChargeCmd` & `Handler` | Application | Menerima instruksi charge, memvalidasi prasyarat order, mendelegasikan ke `ILabBillingIntegration`, memperbarui status agregat, dan mempersistensi order. |
| `LabOrderCancelCmd` & `Handler` | Application | Mengoordinasikan pembatalan order klinis dengan memanggil `ILabBillingIntegration.CancelCharge` sebelum memutasi status ke `Cancelled`. |
| `LabBillingIntegration` | Infrastructure | Adapter konkret yang mengorkestrasi repository Billing/Tata Rekening (`AddBillAppService`, `IRegRepo`, `ITarifRepo`, dll.), menangani dual-path pasien, dan mengelola void tagihan. |
| `LabOrderController` | API | Mengekspos endpoint `PATCH api/LabContext/LabOrderFeature/charge` dan `PATCH api/LabContext/LabOrderFeature/cancel`. |

# 6. Integration Design

| Source | Target | Interface / Method | Purpose |
|---|---|---|---|
| `LabOrderChargeHandler` | `ILabBillingIntegration` | `ChargeOrder(...)` | Memicu pembebanan tarif item lab |
| `LabOrderCancelHandler` | `ILabBillingIntegration` | `CancelCharge(...)` | Membatalkan beban tagihan di Tata Rekening |
| `LabBillingIntegration` | `IAddBillAppService` | `FromLabOrderItem(...)` | Mempersistensi catatan tagihan `TrsBill` |
| `LabBillingIntegration` | `ITataRekeningRepo` | `LoadEntity` / `SaveChanges` | Memeriksa status kasir dan menghapus bill |
| `LabOrderController` | `IMediator` | `Send(LabOrderChargeCmd)` | Menerima request HTTP PATCH charge |

# 7. Data Ownership

| Data / Entitas | Pemilik (Owner) | Keterangan |
|---|---|---|
| `LabOrderModel.LabOrderStatus` | Laboratory (`LAB`) | Status operasional order (termasuk `Charged`) dikuasai oleh LabContext. |
| `LabOrderModel.BillingTindakanId` | Laboratory (`LAB`) | Snapshot referensi penagihan tersimpan pada order lab. |
| `TrsBillType` | Tata Rekening (`TRK`) | Catatan resmi transaksi penagihan keuangan pasien. |
| `TataRekeningModel` | Tata Rekening (`TRK`) | Akun induk rekening episode kunjungan pasien. |

# 8. Database Design

## Modified Tables

Tidak ada modifikasi skema tabel baru. Skema tabel `BILRG_LabOrder` yang ada saat ini sudah mendukung atribut:
- `BillingTindakanId VARCHAR(12) NOT NULL DEFAULT('')`
- `BillingLastError VARCHAR(200) NOT NULL DEFAULT('')`

# 9. Cross-Cutting Concerns

- **Audit Trail**: Setiap pembaruan status menjadi `Charged` mencatat identitas pelaku dan stempel waktu resmi melalui `AuditTrail.Modif(userId, chargedAt)`.
- **Transaksionalitas**: Mutasi status order pada `ILabOrderRepo` dieksekusi dalam lingkup transaksi `TransHelper.NewScope()`.
- **Resiliensi & Error Reporting**: Kegagalan pada subsystem billing ditangkap sebagai `LabBillingChargeException`, tidak menyebabkan crash sistem, melainkan terekam pada `BillingLastError` untuk dapat di-retry oleh pengguna.

# 10. Implementation Constraints

1. Seluruh kode backend mengikuti pola arsitektur C# .NET 8 di namespace `Bilreg.*`.
2. Larangan keras melakukan kueri langsung antar-tabel database lintas konteks (`BILRG_LabOrder` vs `BILRG_TrsBill`).
3. Seluruh unit test yang ada pada `Bilreg.Test` wajib tetap berstatus lulus (*Green Build*).
