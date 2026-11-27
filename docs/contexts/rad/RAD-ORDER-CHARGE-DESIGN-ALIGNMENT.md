# Hasil Wawancara /grill-me: Fitur CHARGE Order Radiologi

Berdasarkan kesepakatan sesi wawancara mendalam (**/grill-me**) mengacu pada outcome dasar [OC-RAD-ORDER-RAD.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-ORDER-RAD.md) dan referensi fitur [OC-LAB-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-CHARGE-FEATURE.md), seluruh percabangan desain untuk fitur **Pembebanan Biaya Pemeriksaan Radiologi (`OC-RAD-ORDER-CHARGE`)** telah disepakati dan diformalkan ke dalam repositori.

---

## 1. Rangkuman Keputusan Desain (Design Decisions)

```mermaid
flowchart TD
    Req["Order Dibuat (Requested)"] -->|Penjadwalan Opsional| Sched["Order Terjadwal (Scheduled)"]
    Req -->|Manual Loket Radiologi: Charge| Chg["Order Dibebankan (Charged)"]
    Sched -->|Konfirmasi Loket: Charge| Chg
    
    subgraph Pelaksanaan Fisik (RAD-EXAM)
        Chg -->|Pemeriksaan Rutin: Gate Lolos| Exam["Scan Fisik Dimulai (RAD-EXAM)"]
        Req -.->|Bypass Darurat: IsCito == true| Exam
        Sched -.->|Bypass Darurat: IsCito == true| Exam
    end

    subgraph Kompensasi Finansial (Cancel)
        Chg -->|Batal saat TataRekening 'Opened'| Void["Hapus TrsBill & Cancel Order"]
        Chg -->|Batal saat Kasir 'Final/Lunas'| Block["Ditolak Sistem (Retur via Kasir)"]
    end
```

| Dimensi Keputusan | Keputusan yang Disepakati | Rujukan Arsitektur |
|---|---|---|
| **Titik Pemicu Charge** | Manual oleh petugas loket Radiologi saat order diterima/dikonfirmasi (sebelum penjadwalan atau sebelum pemanggilan mesin). | Konsisten dengan alur Lab Order Charge |
| **Representasi Status** | Menambahkan `Charged = 4` ke dalam `RadOrderStatusEnum`. Transisi fleksibel: `Requested` $\rightarrow$ `Charged` $\rightarrow$ `Scheduled`, atau `Scheduled` $\rightarrow$ `Charged` saat pasien tiba. | AD-03 |
| **Gerbang Finansial (Gate Rule)** | **Hierarchical Gate (Pola Lab)**: Pemeriksaan **Rutin** wajib berstatus `Charged` sebelum radiografer dapat memulai pemindaian di mesin (`RAD-EXAM`). Pemeriksaan **Cito** (`IsCito == true`) diizinkan bypass pemindaian fisik langsung demi menyelamatkan nyawa pasien, dengan pembebanan menyusul. | AD-03 |
| **Kompensasi Pembatalan** | **Guarded Void (Pola Lab)**: Jika rekening `TataRekening` masih `Opened`, `TrsBill` otomatis dihapus/void dan order dibatalkan. Jika rekening pasien sudah `Finalized`, `Lunas`, atau terbayar di kasir, pembatalan order klinis ditolak dan diarahkan ke kasir/keuangan. | AD-04 |
| **Cakupan Segmen Pasien** | Prioritas Pasien Internal RS (Rawat Jalan, Rawat Inap, IGD) yang memiliki `RegId` aktif, dengan arsitektur adapter yang extensible untuk walk-in eksternal. | AD-02 |
| **Jalur Deliverable** | Pendekatan bertahap (ICA Standard): Menyusun dokumen resmi FEATURE & ARCHITECTURE terlebih dahulu di `docs/contexts/rad/`, diikuti Implementation Plan dan eksekusi kode backend. | ICA Metamodel |

---

## 2. Dokumen Resmi yang Dihasilkan

Seluruh artefak spesifikasi telah berhasil disusun dan diterbitkan ke dalam direktori bounded context `rad`:

1. **Spesifikasi Fitur Bisnis**:  
   [OC-RAD-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-ORDER-CHARGE-FEATURE.md)
2. **Spesifikasi Arsitektur Teknis**:  
   [OC-RAD-ORDER-CHARGE-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-ORDER-CHARGE-ARCHITECTURE.md)
3. **Rencana Implementasi Eksekusi**:  
   [OC-RAD-ORDER-CHARGE-IMPLEMENTATION-PLAN.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-ORDER-CHARGE-IMPLEMENTATION-PLAN.md)

---

## 3. Rencana Eksekusi Kode (Implementation Plan Breakdown)

- **Phase 1: Database Schema & Domain Updates**
  - `P1-S01`: Skrip migrasi SQL penambahan kolom `BillingTindakanId VARCHAR(12)` dan `BillingLastError VARCHAR(200)` pada tabel `BILRG_RadOrder`.
  - `P1-S02`: Penambahan enum `RadOrderStatusEnum.Charged`, properti billing, serta metode domain `Charge`, `MarkCharged`, dan `RecordBillingError` pada `RadOrderModel`.
- **Phase 2: Payment Context Extension**
  - `P2-S01`: Implementasi `TrsBillFactory.CreateFromRadOrderItem` dan `IAddBillAppService.FromRadOrderItem`.
- **Phase 3: Application Integration & Use Cases**
  - `P3-S01`: Kontrak port `IRadBillingIntegration` & DTO di Application layer.
  - `P3-S02`: Command `RadOrderChargeCmd` dan MediatR handler `RadOrderChargeHandler`.
  - `P3-S03`: Integrasi void kompensasi finansial pada `RadOrderCancelHandler`.
- **Phase 4: Infrastructure Realization**
  - `P4-S01`: Implementasi adapter `RadBillingIntegration` (resolusi tarif, penjamin, persistensi `TrsBill`, dan pengecekan status kasir).
  - `P4-S02`: Pembaruan DTO dan query mapping pada `RadOrderDal`.
- **Phase 5: API Controller & Endpoint**
  - `P5-S01`: Endpoint `POST /api/rad/order/{id}/charge` dan `PATCH /api/RadContext/RadOrderFeature/charge` pada `RadOrderController`.
- **Phase 6: Verification & Test Suite**
  - `P6-S01`: Unit test komprehensif (domain, use case, adapter billing, DAL) dengan target lulus 100%.
