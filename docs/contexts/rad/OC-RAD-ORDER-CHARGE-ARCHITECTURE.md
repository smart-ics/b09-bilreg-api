---
Title: Pembebanan Biaya Pemeriksaan Radiologi Architecture
Code: OC-RAD-ORDER-CHARGE
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-24
---

# 1. Overview

Arsitektur ini merealisasikan spesifikasi fitur **[OC-RAD-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-ORDER-CHARGE-FEATURE.md)** yang diturunkan dari outcome bisnis **[OC-RAD-ORDER-RAD.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-ORDER-RAD.md)**.

Arsitektur ini menetapkan penyelarasan teknis pembebanan biaya (*billing charge*) dan kompensasi pembatalan pada backend BILREG API (`b09-bilreg-api`):
1. **Penerapan Clean Architecture & Decoupling pada `RadOrderChargeHandler`**:
   - Handler aplikasi tidak mengakses langsung repository master billing maupun database Tata Rekening.
   - Seluruh logika resolusi registrasi, penjamin, pemetaan tarif komponen tindakan radiologi, dan pemanggilan `IAddBillAppService` diisolasi di balik adapter infrastruktur `RadBillingIntegration`.
2. **Standardisasi Kontrak Port `IRadBillingIntegration`**:
   - Mendefinisikan antarmuka `IRadBillingIntegration` pada Application layer untuk mencakup operasi `ChargeOrder` dan `CancelCharge` yang bersih dan independen.
3. **Penyempurnaan State Machine `RadOrderStatusEnum`**:
   - Menambahkan status `Charged = 4` ke dalam `RadOrderStatusEnum`.
   - Mengakomodasi fleksibilitas alur operasional: order dapat di-charge langsung dari status `Requested`, maupun setelah penjadwalan (`Scheduled`).
4. **Penegakan Gerbang Finansial & Pengecualian Cito pada Pemindaian Fisik**:
   - Menegakkan aturan invariant pada tahap pemindaian fisik (`RAD-EXAM`): order Rutin wajib berstatus `Charged`, sedangkan pemeriksaan Cito (`IsCito == true`) diizinkan bypass langsung untuk menyelamatkan nyawa pasien.
5. **Delegasi Kompensasi Finansial Pembatalan Order (Guarded Void)**:
   - Memperbarui `RadOrderCancelHandler` untuk mendelegasikan kompensasi pembatalan finansial ke `IRadBillingIntegration.CancelCharge`.
   - Menolak pembatalan order klinis jika transaksi kasir pasien telah `Finalized` / `Lunas`.
6. **Evolusi Skema Basis Data Non-Destruktif**:
   - Menambahkan kolom `BillingTindakanId VARCHAR(12)` dan `BillingLastError VARCHAR(200)` pada tabel `BILRG_RadOrder`.

# 2. Architectural Basis

## Business Context

Pemeriksaan radiologi yang telah tercatat (`Requested` atau `Scheduled`) harus dibukukan beban biayanya ke dalam sistem penagihan rumah sakit (`TRK-BILLING`) agar biaya tindakan terikat pada rekening kunjungan pasien. Hasil pembebanan ini memberikan otorisasi finansial bagi instalasi radiologi untuk melaksanakan prosedur pemindaian fisik rutin di mesin, sekaligus menjamin akurasi billing rumah sakit.

## Analysis Input

Berdasarkan kesepakatan desain alignment `/grill-me` dan dokumen pendukung:
- **Prinsip Modular Monolith**: `RadContext` dan Billing (`PaymentContext` / `TRK`) berada dalam solusi yang sama. Komunikasi antar-konteks dilakukan melalui in-process application integration service, bukan HTTP atau cross-context direct database queries.
- **Konsistensi dengan Standar Laboratorium**: Mengadopsi pola proven dari `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md` yang telah teruji di produksi.
- **Hierarchical Urgency Rule**: Menggunakan bendera `IsCito` yang sudah ada pada `RadOrderModel` untuk mendukung bypass billing darurat pada pemindaian fisik.

```text
[OC-RAD-ORDER-RAD.md] + [OC-RAD-ORDER-CHARGE-FEATURE.md]
                          ↓
          [OC-RAD-ORDER-CHARGE-ARCHITECTURE]
```

# 3. Scope

## Included

1. **Domain Layer (`Bilreg.Domain.RadContext.RadOrderFeature`)**:
   - Penambahan nilai `Charged = 4` pada `RadOrderStatusEnum`.
   - Penambahan properti `BillingTindakanId` dan `BillingLastError` pada `RadOrderModel`.
   - Penambahan metode domain `Charge(userId)`, `MarkCharged(tindakanId, userId, chargedAt)`, dan `RecordBillingError(error, userId, recordedAt)`.
   - Update invariant `Cancel(...)` untuk mendukung pembatalan dari status `Charged`.
2. **Application Layer (`Bilreg.Application.RadContext.RadOrderFeature`)**:
   - Kontrak port `IRadBillingIntegration` dengan method `ChargeOrder(...)` dan `CancelCharge(...)`.
   - Command `RadOrderChargeCmd` dan handler `RadOrderChargeHandler`.
   - Pembaruan `RadOrderCancelCmd` & `RadOrderCancelHandler` untuk memanggil `IRadBillingIntegration.CancelCharge(...)`.
3. **Domain & Application Payment Context (`Bilreg.Domain.PaymentContext.TrsBillFeature` & `Bilreg.Application.PaymentContext.TrsBillingFeature`)**:
   - Penambahan metode `CreateFromRadOrderItem(...)` pada `TrsBillFactory`.
   - Penambahan metode `FromRadOrderItem(...)` pada antarmuka dan implementasi `IAddBillAppService`.
4. **Infrastructure Layer (`Bilreg.Infrastructure.RadContext.Integration`)**:
   - Implementasi penuh `RadBillingIntegration` yang mengorkestrasi lookup registrasi (`IRegRepo`), jaminan (`IJaminanRepo`), tarif (`ITarifRepo`), nilai tarif (`INilaiTarifRepo`), komponen (`IKomponenRepo`), dan pemanggilan `IAddBillAppService.FromRadOrderItem`.
   - Implementasi logika verifikasi status kasir dan void bill pada `CancelCharge`.
   - Penyesuaian `RadOrderDal` untuk pemetaan kolom `BillingTindakanId` dan `BillingLastError`.
5. **API Layer (`Bilreg.Api.Controllers.RadContext`)**:
   - Penambahan endpoint charge pada `RadOrderController`: `POST api/rad/order/{id}/charge` (dan `PATCH api/RadContext/RadOrderFeature/charge`).
6. **Unit Test Suite (`Bilreg.Test.RadContext.RadOrderFeature`)**:
   - Unit test domain lifecycle `RadOrderModel` terkait charge dan cancel.
   - Unit test handler `RadOrderChargeHandler` dan `RadOrderCancelHandler`.
   - Unit test `RadBillingIntegration`.

## Excluded

1. Alur pembayaran tunai/non-tunai di loket kasir (`TRK-KASIR`).
2. Finalisasi dan penutupan rekening pasien (`TRK-FINALIZE`).
3. Algoritma penghitungan diskon kompleks atau penjaminan ganda (*co-insurance*).
4. Tindakan medis di luar instalasi radiologi.
5. Pendaftaran pasien rujukan luar (*walk-in*) tanpa admisi sentral (`RAD-EXTERNAL`), disiapkan fondasi adapter-nya namun implementasi pendaftaran mandiri berada di luar scope fitur ini.

# 4. Technical Decisions

## AD-01: Decoupled Billing Integration Port

**Keputusan**: `RadOrderChargeHandler` tidak boleh mengetahui detail internal domain Billing/Tata Rekening (struktur registrasi, kelas, jaminan, maupun komponen tarif).
- Didefinisikan kontrak port `IRadBillingIntegration` pada `Bilreg.Application`:
  ```csharp
  namespace Bilreg.Application.RadContext.RadOrderFeature.Integration;

  public record RadBillingChargeResult(bool Success, string? BillingTindakanId, string? ErrorMessage);

  public interface IRadBillingIntegration
  {
      string ChargeOrder(RadOrderModel order, string userId, DateTime occurredAt);
      void CancelCharge(string orderId, string regId, string userId, string reason, DateTime cancelledAt);
  }
  ```
- `RadOrderChargeHandler` hanya berinteraksi dengan `IRadOrderRepo` dan `IRadBillingIntegration`.

## AD-02: Infrastructure-Level Charging Resolution

**Keputusan**: Seluruh logika resolusi tarif, penjamin, kelas perawatan, dan penentuan layanan radiologi diisolasi di layer Infrastructure (`RadBillingIntegration`):
- **Pasien Internal**: Mengambil `RegModel` via `_regRepo`, mengekstrak penjamin, menyelesaikan tipe tarif berdasarkan jenis registrasi (Rajal vs Ranap), memuat nilai tarif, dan memanggil `AddBillAppService.FromRadOrderItem`.
- ID referensi transaksi penagihan yang dihasilkan dikembalikan ke application layer untuk disimpan pada `BillingTindakanId`.

## AD-03: Operational Trigger & Urgency Invariant Enforcement

**Keputusan**: Transisi menuju `Charged` dipicu secara eksplisit oleh aktor melalui endpoint `POST api/rad/order/{id}/charge`.
- Pada tahap pemindaian fisik di mesin (`RAD-EXAM`), sistem menegakkan aturan invariant:
  ```csharp
  if ((RadOrderStatus == RadOrderStatusEnum.Requested || RadOrderStatus == RadOrderStatusEnum.Scheduled) && !IsCito)
  {
      throw new InvalidOperationException(
          $"Order {OrderId} berstatus {RadOrderStatus}; pemeriksaan rutin wajib berstatus Charged sebelum pemindaian fisik.");
  }
  ```
- Hal ini menjamin tertib administrasi untuk pemeriksaan rutin tanpa menghambat tindakan penyelamatan nyawa pada kasus Cito.

## AD-04: Non-Destructive Cancellation & Delegated Financial Compensation

**Keputusan**: Pembatalan order yang sudah `Charged` memicu kompensasi pembatalan di Tata Rekening melalui `IRadBillingIntegration.CancelCharge`:
1. `RadBillingIntegration` memeriksa status rekening pasien (`TataRekeningModel`):
   - Jika status `Finalized`, `Lunas`, atau telah terdapat rekaman pembayaran kasir: sistem melempar exception untuk menolak pembatalan order radiologi klinis.
   - Jika status masih `Opened`: sistem menghapus/void seluruh `TrsBill` yang berasosiasi dengan order tersebut (`OrderId-ItemNo`).
2. Setelah kompensasi finansial berhasil, `RadOrderCancelHandler` memutasi status order klinis menjadi `Cancelled`.

## AD-05: Schema Enhancement for BILRG_RadOrder

**Keputusan**: Menambahkan kolom `BillingTindakanId` dan `BillingLastError` pada tabel `BILRG_RadOrder` melalui skrip migrasi SQL:
- `BillingTindakanId VARCHAR(12) NOT NULL DEFAULT('')`
- `BillingLastError VARCHAR(200) NOT NULL DEFAULT('')`

# 5. Component Responsibilities

| Komponen | Layer | Tanggung Jawab Utama |
|---|---|---|
| `RadOrderModel` | Domain | Menjaga invariant siklus hidup, status `Charged`, validasi `MarkCharged`, dan status `BillingLastError`. |
| `IRadBillingIntegration` | Application | Port kontrak pembebanan biaya (`ChargeOrder`) dan kompensasi pembatalan (`CancelCharge`). |
| `RadOrderChargeCmd` & `Handler` | Application | Menerima instruksi charge, memvalidasi prasyarat order, mendelegasikan ke `IRadBillingIntegration`, memperbarui status agregat, dan mempersistensi order. |
| `RadOrderCancelCmd` & `Handler` | Application | Mengoordinasikan pembatalan order klinis dengan memanggil `IRadBillingIntegration.CancelCharge` sebelum memutasi status ke `Cancelled`. |
| `RadBillingIntegration` | Infrastructure | Adapter konkret yang mengorkestrasi repository Billing/Tata Rekening (`AddBillAppService`, `IRegRepo`, `ITarifRepo`, dll.) dan mengelola void tagihan. |
| `RadOrderController` | API | Mengekspos endpoint `POST api/rad/order/{id}/charge` dan `POST api/rad/order/{id}/cancel`. |
| `TrsBillFactory` | Domain (Payment) | Membuat entitas tagihan `TrsBillType` khusus untuk item pemeriksaan radiologi. |
| `AddBillAppService` | Application (Payment) | Memvalidasi dan menyimpan transaksi tagihan radiologi ke dalam `TataRekening`. |

# 6. Integration Design

| Source | Target | Interface / Method | Purpose |
|---|---|---|---|
| `RadOrderChargeHandler` | `IRadBillingIntegration` | `ChargeOrder(...)` | Memicu pembebanan tarif item radiologi |
| `RadOrderCancelHandler` | `IRadBillingIntegration` | `CancelCharge(...)` | Membatalkan beban tagihan di Tata Rekening |
| `RadBillingIntegration` | `IAddBillAppService` | `FromRadOrderItem(...)` | Mempersistensi catatan tagihan `TrsBill` |
| `RadBillingIntegration` | `ITataRekeningRepo` | `LoadEntity` / `SaveChanges` | Memeriksa status kasir dan menghapus bill |
| `RadOrderController` | `IMediator` | `Send(RadOrderChargeCmd)` | Menerima request HTTP charge |

# 7. Data Ownership

| Data / Entitas | Pemilik (Owner) | Keterangan |
|---|---|---|
| `RadOrderModel.RadOrderStatus` | Radiology (`RAD`) | Status operasional order (termasuk `Charged`) dikuasai oleh RadContext. |
| `RadOrderModel.BillingTindakanId` | Radiology (`RAD`) | Snapshot referensi penagihan tersimpan pada order radiologi. |
| `TrsBillType` | Tata Rekening (`TRK`) | Catatan resmi transaksi penagihan keuangan pasien. |
| `TataRekeningModel` | Tata Rekening (`TRK`) | Akun induk rekening episode kunjungan pasien. |

# 8. Database Design

Skrip migrasi penambahan kolom penagihan pada basis data SQL Server:

```sql
-- Migration: Add Billing Columns to BILRG_RadOrder
IF COL_LENGTH('BILRG_RadOrder', 'BillingTindakanId') IS NULL
BEGIN
    ALTER TABLE BILRG_RadOrder ADD
        BillingTindakanId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_RadOrder_BillingTindakanId DEFAULT(''),
        BillingLastError VARCHAR(200) NOT NULL CONSTRAINT DF_BILRG_RadOrder_BillingLastError DEFAULT('');
END
GO

CREATE INDEX IX_BILRG_RadOrder_Billing ON BILRG_RadOrder(BillingTindakanId) WHERE BillingTindakanId <> '';
GO
```
