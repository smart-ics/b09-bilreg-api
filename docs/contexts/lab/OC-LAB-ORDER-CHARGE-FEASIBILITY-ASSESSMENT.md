---
Title: Pembebanan Biaya Pemeriksaan Laboratorium Feasibility Assessment
Code: OC-LAB-ORDER-CHARGE
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-10-17
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan teknis implementasi fitur pembebanan biaya (*order billing charge*) dan kompensasi pembatalan tagihan laboratorium terhadap arsitektur dan codebase yang ada di `b09-bilreg-api`.

Referenced artifacts:
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md), [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- FEATURE: [OC-LAB-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-CHARGE-FEATURE.md)
- Originating Outcome: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)

## Objective

Menilai keselarasan (*alignment*) antara spesifikasi fitur `OC-LAB-ORDER-CHARGE-FEATURE.md` dengan codebase implementasi saat ini (`b09-bilreg-api`), mengidentifikasi celah arsitektural (*gaps*), merumuskan keputusan penutupan celah (*gap closure decisions*), dan menetapkan kesiapan untuk fase arsitektur dan perencanaan eksekusi.

---

# 2. Current State

## Existing Behavior

1. **Domain Layer (`Bilreg.Domain.LabContext.LabOrderFeature`)**:
   - `LabOrderStatusEnum` telah memiliki status `Charged = 3`.
   - `LabOrderModel` telah memiliki method `Charge(userId)` yang memvalidasi bahwa order tidak sedang `Deferred`, berstatus tepat `Ordered`, dan belum memiliki `BillingTindakanId`.
   - `LabOrderModel.MarkCharged(tindakanId, userId, chargedAt)` memutasi status menjadi `Charged` dan mengosongkan `BillingLastError`.
   - `LabOrderModel.RecordBillingError(error, userId, recordedAt)` mencatat pesan kesalahan pada `BillingLastError` tanpa memajukan status order.
   - `LabOrderModel.CollectSpecimen(...)` saat ini mengizinkan pengambilan sampel jika status adalah `Charged` **atau** `Ordered` tanpa memeriksa apakah order berkategori Cito atau Rutin.
2. **Application Layer (`Bilreg.Application.LabContext.LabOrderFeature`)**:
   - `LabOrderChargeCmd` dan `LabOrderChargeHandler` telah diimplementasikan:
     - Handler menginjeksi 6 repository master eksternal: `IRegRepo`, `ITarifRepo`, `INilaiTarifRepo`, `IKomponenRepo`, `IJaminanRepo`, `ILabExternalRepo`, serta `IAddBillAppService` dan `ILabBillingIntegration`.
     - Handler mengeksekusi logika dual-path (pasien internal vs eksternal) dan memanggil `_addBillAppService.FromLabOrderItem(...)` per baris item untuk membuat record `TrsBill` di Tata Rekening.
     - Handler kemudian memanggil `_labBillingIntegration.CreateTindakan(...)` yang mengembalikan identifier mock stub `TDK-FAKE-0001` dan menyimpannya ke `BillingTindakanId`.
   - `LabOrderCancelCmd` dan `LabOrderCancelHandler`:
     - Handler menginjeksi `ITataRekeningRepo` secara langsung.
     - Melakukan pengecekan status kasir pada `TataRekeningModel`: menolak jika `Finalized`/`Lunas` atau terdapat pembayaran.
     - Memeriksa apakah `tataRekening.ListTrsBill` memuat `b.TrsBillingId == order.BillingTindakanId`. Karena `BillingTindakanId` berisi `TDK-FAKE-xxxx` sedangkan ID bill adalah `${order.OrderId}-${itemNo:D2}`, pencocokan ini selalu gagal (*mismatch*).
3. **Infrastructure Layer (`Bilreg.Infrastructure.LabContext.Integration`)**:
   - `LabBillingIntegration` hanya berupa implementasi stub sederhana (`CreateTindakan` menghasilkan string acak `TDK-FAKE-xxxx`).
4. **API Layer (`Bilreg.Api.Controllers.LabContext.LabOrderFeature`)**:
   - `LabOrderController` telah mengekspos endpoint `PATCH api/LabContext/LabOrderFeature/charge`.
5. **Database Persistence (`BILRG_LabOrder`)**:
   - Kolom `BillingTindakanId VARCHAR(12)` dan `BillingLastError VARCHAR(200)` telah tersedia pada tabel `BILRG_LabOrder`.

## Existing Constraints

1. **In-Process Modular Monolith**: Komunikasi antara konteks laboratorium (`LabContext`) dan konteks keuangan/billing (`PaymentContext`/`TRK`) wajib melalui in-process integration service di backend yang sama, tanpa HTTP atau remote RPC.
2. **Database Boundary**: Dilarang melakukan SQL join atau direct query lintas tabel antara domain Lab (`BILRG_LabOrder`) dan domain Keuangan (`BILRG_TrsBill`).
3. **Immutability of Finalized Bills**: Transaksi penagihan yang telah difinalisasi atau dilunasi di loket kasir tidak boleh dimutasi atau dihapus secara sepihak oleh subsistem operasional laboratorium.

---

# 3. Gap Analysis

| ID | Severity | Gap Description |
|---|---|---|
| **GAP-001** | CRITICAL | **Leaky Abstraction pada `LabOrderChargeHandler`**: Application Handler menginjeksi langsung 6 repository master billing (`IRegRepo`, `ITarifRepo`, `INilaiTarifRepo`, `IKomponenRepo`, `IJaminanRepo`, `ILabExternalRepo`), melanggar batas kontekstual modul (*bounded context decoupling*). |
| **GAP-002** | CRITICAL | **Dualisme Implementasi Penagihan & Mock Stub**: Handler memanggil `AddBillAppService` (persistensi riil `TrsBill`), namun secara bersamaan memanggil `_labBillingIntegration.CreateTindakan` yang masih berupa stub palsu (`TDK-FAKE-xxxx`), menyebabkan `BillingTindakanId` tidak mencerminkan transaksi penagihan sebenarnya. |
| **GAP-003** | MAJOR | **Ketidaksesuaian Invariant Pengambilan Sampel (`CollectSpecimen`)**: `LabOrderModel.CollectSpecimen` mengizinkan pengambilan spesimen langsung dari status `Ordered` untuk semua jenis pemeriksaan, padahal pemeriksaan Rutin diwajibkan telah berstatus `Charged` terlebih dahulu. |
| **GAP-004** | CRITICAL | **Cross-Context Leak & Mismatch Void pada `LabOrderCancelHandler`**: `LabOrderCancelHandler` menginjeksi `ITataRekeningRepo` secara langsung dan menggunakan pencocokan string tunggal `b.TrsBillingId == order.BillingTindakanId` yang tidak cocok dengan format ID `TrsBill` multi-item (`${OrderId}-${ItemNo}`). |
| **GAP-005** | MINOR | **Ketiadaan DTO Penagihan Terpadu**: Belum tersedianya kontrak DTO yang secara eksplisit memisahkan parameter request charge, hasil tagihan, dan kompensasi pembatalan pada port `ILabBillingIntegration`. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| **OQ-001** | Identifier apa yang harus disimpan pada kolom `BillingTindakanId` ketika order memiliki beberapa baris item pemeriksaan dengan ID `TrsBill` terpisah (`${OrderId}-01`, `${OrderId}-02`)? | Menentukan format nilai string snapshot penagihan pada agregat `LabOrderModel` dan tabel `BILRG_LabOrder`. |
| **OQ-002** | Apakah pembatalan order berstatus `Charged` harus menghapus tagihan secara fisik (`DeleteBill`) atau menandai pembatalan (`VoidBill`) di Tata Rekening? | Mempengaruhi integritas jejak audit finansial pada entitas `TataRekeningModel`. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| **ASM-001** | Pasien luar (`LabOrderSourceEnum.ExternalPatient`) selalu dibebankan dengan tarif Umum dan Kelas III tanpa penjamin pihak ketiga. |
| **ASM-002** | Layanan `AddBillAppService` pada domain Tata Rekening adalah komponen otoritatif tunggal untuk penerbitan tagihan penunjang medis. |
| **ASM-003** | Database column `BillingTindakanId VARCHAR(12)` dapat digunakan untuk menyimpan identifier transaksi penagihan tanpa mengubah tipe data kolom. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| **RISK-001** | Kegagalan penagihan akibat data penjamin atau tarif tidak aktif dapat menghambat proses pelayanan pasien. | Status order tetap `Ordered` dan sampling tertunda. | Sistem merekam alasan kegagalan pada `BillingLastError` untuk dievaluasi petugas dan di-retry via `PATCH /charge`. |
| **RISK-002** | Order Cito yang diambil spesimennya langsung dari `Ordered` berpotensi terlewat dari proses penagihan jika petugas lupa melakukan charge susulan. | Potensi kerugian pendapatan rumah sakit (*revenue leakage*). | Worklist operasional laboratorium wajib menampilkan indikator visual bagi order Cito yang belum berstatus `Charged`. |
| **RISK-003** | Pembatalan sepihak atas order yang tagihannya telah dilunasi di kasir dapat menyebabkan ketidaksinkronan data medis dan keuangan. | Inkonsistensi data akuntansi dan penagihan kasir. | `LabBillingIntegration.CancelCharge` memvalidasi status kasir dan menolak keras pembatalan jika status rekening sudah `Finalized` atau `Lunas`. |

---

# 7. Recommendations

## Option A: Refactoring Bersih Melalui Adapter Infrastruktur (Rekomendasi Utama)

Memindahkan seluruh logika pembebanan tarif, penjamin, dan pemanggilan `AddBillAppService` ke dalam implementasi `LabBillingIntegration` di layer Infrastructure. `LabOrderChargeHandler` dan `LabOrderCancelHandler` hanya berinteraksi dengan port `ILabBillingIntegration`.

### Advantages
- Menegakkan prinsip Clean Architecture dan modul modular monolith yang bersih.
- Mengeliminasi duplikasi logika dan stub palsu `TDK-FAKE-xxxx`.
- Memudahkan pengujian unit testing (mocking interface port yang bersih).

### Disadvantages
- Memerlukan refactoring pada `LabBillingIntegration` dan pembaruan mock test suite.

## Option B: Konsolidasi Langsung di Application Handler

Mempertahankan injeksi repositori master di `LabOrderChargeHandler`, namun menghapus pemanggilan `CreateTindakan` dan menggunakan ID `TrsBill` secara langsung.

### Advantages
- Perubahan kode lebih sedikit pada layer Infrastructure.

### Disadvantages
- Melanggar arsitektur modular monolith: `LabOrderFeature` tetap terikat erat dengan detail internal domain Tata Rekening dan Administrasi.

---

# 8. Gap Closure

## GAP-001: Leaky Abstraction pada `LabOrderChargeHandler`

### Decision
Pindahkan seluruh logika penagihan (lookup registrasi, penjamin, tarif, nilai tarif, komponen) dan pemanggilan `AddBillAppService` ke implementasi adapter `LabBillingIntegration` di layer Infrastructure. `LabOrderChargeHandler` hanya menyuntikkan `ILabOrderRepo`, `ILabBillingIntegration`, dan `ITglJamProvider`.

### Rationale
Menjaga isolasi domain kontekstual, mengurangi dependensi eksternal pada handler, dan mematuhi panduan arsitektur pada `lab-implementation-plan.md` Section 9.

### Impact
Pembersihan konstruktor `LabOrderChargeHandler`, perampingan parameter uji `LabOrderChargeHandlerTest`.

### Architecture Impact
Realisasi keputusan AD-01 dan AD-02 pada `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`.

### Resolved By
Pair alignment via `/grill-me`.

### Resolved Date
2026-10-17

---

## GAP-002: Dualisme Implementasi Penagihan & Mock Stub

### Decision
Hapus stub palsu `CreateTindakan`. Jadikan metode `ChargeOrder` pada `ILabBillingIntegration` sebagai satu-satunya jalur penagihan resmi yang mengembalikan ID referensi penagihan riil (misal `${order.OrderId}`).

### Rationale
Menghilangkan kebingungan antara data mock dan transaksi nyata Tata Rekening, serta menjamin keterlacakan audit (*traceability*).

### Impact
`BillingTindakanId` pada `LabOrderModel` mencatat ID transaksi penagihan yang sesungguhnya.

### Architecture Impact
Realisasi keputusan AD-01 dan AD-05 pada `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`.

### Resolved By
Pair alignment via `/grill-me`.

### Resolved Date
2026-10-17

---

## GAP-003: Ketidaksesuaian Invariant Pengambilan Sampel (`CollectSpecimen`)

### Decision
Perbarui metode `LabOrderModel.CollectSpecimen`:
- Jika status order `Ordered` dan bukan Cito (`!IsCito`), lemparkan `InvalidOperationException`: *"Pemeriksaan rutin wajib berstatus Charged sebelum pengambilan spesimen."*
- Jika status order `Ordered` dan berkategori Cito (`IsCito == true`), izinkan pengambilan spesimen langsung memajukan status ke `Collected`.
- Jika status order `Charged`, izinkan pengambilan spesimen baik untuk Rutin maupun Cito.

### Rationale
Menegakkan disiplin finansial untuk order rutin tanpa menghambat prosedur penyelamatan nyawa pasien darurat/Cito.

### Impact
Pembaruan invariant domain `LabOrderModel.cs` dan test case `LabOrderCollectSpecimenHandlerTest.cs` serta `LabOrderModelTest.cs`.

### Architecture Impact
Realisasi keputusan AD-03 pada `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`.

### Resolved By
Pair alignment via `/grill-me`.

### Resolved Date
2026-10-17

---

## GAP-004: Cross-Context Leak & Mismatch Void pada `LabOrderCancelHandler`

### Decision
Tambahkan metode `CancelCharge` pada antarmuka `ILabBillingIntegration`. `LabOrderCancelHandler` mendelegasikan pengecekan status kasir dan pembatalan bill ke metode ini. Di layer Infrastructure, `LabBillingIntegration` akan menghapus seluruh `TrsBill` yang berawalan `${orderId}-` jika status rekening masih `Opened`.

### Rationale
Menghilangkan ketergantungan langsung `LabOrderCancelHandler` pada `ITataRekeningRepo` dan memperbaiki bug di mana bill multi-item tidak terhapus saat pembatalan order.

### Impact
Refactoring `LabOrderCancelHandler`, perbaikan unit test pembatalan order.

### Architecture Impact
Realisasi keputusan AD-04 pada `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`.

### Resolved By
Pair alignment via `/grill-me`.

### Resolved Date
2026-10-17

---

## GAP-005: Ketiadaan DTO Penagihan Terpadu

### Decision
Definisikan DTO `LabBillingChargeOrderRequest` dan DTO hasil pembebanan pada namespace `Bilreg.Application.LabContext.LabOrderFeature.Integration`.

### Rationale
Menstandarkan kontrak komunikasi data in-process antara Lab dan Billing.

### Impact
Pembaruan `ILabBillingIntegration.cs`.

### Architecture Impact
Realisasi keputusan AD-01 pada `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`.

### Resolved By
Pair alignment via `/grill-me`.

### Resolved Date
2026-10-17

---

## OQ-001: Snapshot Identifier pada `BillingTindakanId`

### Decision
Gunakan format `${order.OrderId}` (atau ID transaksi bill pertama) yang memiliki panjang $\le 12$ karakter untuk disimpan pada properti `BillingTindakanId`.

### Rationale
Memenuhi batas panjang kolom `VARCHAR(12)` pada tabel `BILRG_LabOrder` tanpa memerlukan skrip DDL alter baru.

### Impact
Kompatibilitas penuh dengan skema database yang sudah ada.

### Architecture Impact
Realisasi keputusan AD-05 pada `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`.

### Resolved By
Pair alignment via `/grill-me`.

### Resolved Date
2026-10-17

---

## OQ-002: Penghapusan Fisik vs Void pada Pembatalan Tagihan Kasir

### Decision
Gunakan metode `tataRekening.DeleteBill(billId)` selama status rekening masih `Opened`. Jika rekening sudah `Finalized` atau `Lunas`, tolak pembatalan order dengan melempar exception.

### Rationale
Konsisten dengan aturan bisnis domain Tata Rekening di mana tagihan pada rekening terbuka dapat dihapus sebelum finalisasi kasir.

### Impact
Penghapusan record bill pada rekening terbuka; perlindungan atas rekening yang sudah final.

### Architecture Impact
Realisasi keputusan AD-04 pada `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`.

### Resolved By
Pair alignment via `/grill-me`.

### Resolved Date
2026-10-17

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Implementasi fitur ini melibatkan restrukturisasi boundary integrasi antar-konteks (`ILabBillingIntegration`), pemindahan logika resolusi penjamin dan tarif dari layer Application ke layer Infrastructure, penegakan invariant domain baru terkait urgensi Cito pada agregat inti `LabOrderModel`, serta perbaikan protokol pembatalan kompensasi finansial.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved (GAP-001, GAP-002, GAP-003, GAP-004, GAP-005 telah memiliki keputusan definitif)
- [x] All required decisions recorded (Keputusan, rasional, dampak, dan stempel waktu resolusi tercatat lengkap)
- [x] All blocking open questions resolved (OQ-001 dan OQ-002 telah terjawab)
- [x] Architecture can be finalized or updated (Telah diselaraskan pada `OC-LAB-ORDER-CHARGE-ARCHITECTURE.md`)

## Status

**READY-FOR-PLANNING**

## Notes

Seluruh celah teknis dan pertanyaan terbuka telah berhasil dianalisis dan diputuskan secara tuntas. Otorisasi gerbang (gate) `READY-FOR-PLANNING` secara resmi disetujui dan diberikan oleh peran `ica-architect` pada 2026-10-17.

---

# 11. References

Referenced artifacts:
- [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md)
- [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)
- [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- [OC-LAB-ORDER-CHARGE-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-CHARGE-FEATURE.md)
- [OC-LAB-ORDER-CHARGE-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-CHARGE-ARCHITECTURE.md)

Referenced codebase locations:
- `Bilreg.Domain.LabContext.LabOrderFeature.LabOrderModel`
- `Bilreg.Application.LabContext.LabOrderFeature.UseCases.LabOrderChargeCmd`
- `Bilreg.Application.LabContext.LabOrderFeature.UseCases.LabOrderCancelCmd`
- `Bilreg.Application.LabContext.LabOrderFeature.Integration.ILabBillingIntegration`
- `Bilreg.Infrastructure.LabContext.Integration.LabBillingIntegration`
- `Bilreg.Application.PaymentContext.TrsBillingFeature.AddBillAppService`

Referenced documents:
- `b09-bilreg-api/docs/contexts/lab/lab-workflow.md`
- `b09-bilreg-api/docs/contexts/lab/lab-integration.md`
- `b09-bilreg-api/docs/contexts/lab/lab-implementation-plan.md`
