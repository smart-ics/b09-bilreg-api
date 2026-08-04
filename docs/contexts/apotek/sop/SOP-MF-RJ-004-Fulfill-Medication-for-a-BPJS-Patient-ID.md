# SOP MF-RJ-004 — Memenuhi Obat untuk Pasien BPJS

**Status artefak:** Spesifikasi operasional target kanonis

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-004`

**Sumber kanonis bahasa Inggris:** [SOP MF-RJ-004 — Fulfill Medication for a BPJS Patient](./SOP-MF-RJ-004-Fulfill-Medication-for-a-BPJS-Patient.md)

**Status terminologi aplikasi:** `Apotek` dan `Apotek Rajal` merupakan istilah aplikasi yang telah ditetapkan. Kontrol lain dijelaskan berdasarkan tindakan operasional karena label target-workflow yang disetujui belum tersedia.

## 1. Tujuan

Menyediakan prosedur berulang untuk memberikan clearance dan menyiapkan obat Rawat Jalan yang ditanggung BPJS tanpa pembayaran Patient atau Sales Invoice sebelumnya, kemudian membentuk Sales Invoice BPJS hanya bersama Medication Handover yang berhasil.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Patient or Caregiver | Manusia | Hadir untuk pickup, menerima edukasi, dan menerima obat ketika authorized. |
| Pharmacy Staff | Manusia | Memverifikasi covered work projection, mengoordinasikan readiness, dan melakukan pickup call. |
| Pharmacy Supervisor | Manusia | Mengotorisasi manual uncollected-medication resolution ketika Patient tidak mengambil obat yang telah disiapkan. |
| Pharmacy Technician | Manusia | Menyiapkan atau meracik obat berdasarkan released Dispense Order. |
| Pharmacist | Manusia | Memverifikasi penerima, menyelesaikan Final Dispense Review, dan memberikan Patient Education. |
| SEP and Fornas Authorities | Subsistem | Menyediakan validitas SEP tingkat encounter dan coverage Fornas item-level. |
| Medication Fulfillment Application | Aplikasi | Mencatat Coverage dan Fulfillment Clearance, melacak preparation, serta secara atomic mencatat Sales Invoice BPJS dan successful handover outcome. |
| Patient Tracker | Subsistem | Mencatat `ServedAt` saat preparation dimulai dan `DoneAt` saat pickup call. |
| Inventory | Subsistem | Menyediakan outcome reservation, issue, dan return disposition. |
| Tata Rekening | Subsistem | Menerima outcome Financial Charge BPJS. |

## 3. Prasyarat

1. Staff yang berpartisipasi telah sign in dengan izin yang diperlukan.
2. Outpatient Queue Mapping, Pharmacy Sales Order aktif, covered Billing Allocation, dan Dispense Order yang berlaku telah ditampilkan.
3. SEP valid tersedia untuk encounter, dan mapping Fornas authoritative mendukung setiap covered quantity.
4. Nilai Patient-payable nol, payment disposition `Not Required`, dan Sales Invoice BPJS belum tersedia.

## 4. Langkah Operasional

1. **Pharmacy Staff** membuka mapped BPJS demand di `Apotek Rajal` dan memverifikasi Patient, referensi SEP, covered Billing Allocation, serta jumlah Dispense Order.
2. **SEP and Fornas Authorities** menyediakan outcome SEP valid dan coverage item-level.
3. **Medication Fulfillment Application** menampilkan Coverage Clearance untuk setiap covered quantity dan membentuk Fulfillment Clearance terkait tanpa mewajibkan Sales Invoice.
4. **Inventory** mengamankan Stock Reservation bila belum tersedia; **Medication Fulfillment Application** menampilkan reservation outcome.
5. **Pharmacy Technician** memulai Medication Preparation hanya setelah Dispense Order berstatus released.
6. **Medication Fulfillment Application** mencatat `Medication Preparation Started`; **Patient Tracker** memindahkan Queue Entry ke In Service dan mencatat `ServedAt`.
7. **Pharmacy Technician** menyelesaikan preparation atau compounding dan mencatat completion; **Medication Fulfillment Application** menampilkan Dispense Order sebagai `Prepared`.
8. **Pharmacy Staff** memverifikasi setiap Dispense Order yang hendak diserahkan telah `Prepared` atau memiliki exception outcome accountable.
9. **Pharmacy Staff** melakukan satu coordinated pickup call; **Patient Tracker** membuat Queue Entry `Done` dan mencatat `DoneAt`.
10. Dengan Patient atau caregiver hadir, **Pharmacist** memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review, dan mencatat Patient Education yang berlaku.
11. **Medication Fulfillment Application** memblokir completion ketika recipient verification atau final review belum lengkap.
12. **Pharmacy Staff** menyelesaikan physical handover setelah otorisasi Pharmacist.
13. Sebagai satu outcome accountable, **Medication Fulfillment Application** membentuk Sales Invoice BPJS dari covered Billing Allocation, mencatat Medication Dispense, dan mencatat Medication Handover.
14. **Inventory** menyediakan outcome Inventory Issue authoritative; **Medication Fulfillment Application** menampilkan Dispense Order sebagai `Completed`.
15. **Medication Fulfillment Application** menampilkan Pharmacy Sales Order sebagai `Resolved` hanya ketika setiap accepted quantity dan konsekuensi komersial final.

## 5. Pengecualian Operasional

### 5.1 SEP tidak valid atau coverage tidak tersedia

- **SEP and Fornas Authorities** tidak menyediakan Coverage Clearance untuk jumlah terdampak.
- **Medication Fulfillment Application** mempertahankan blokir preparation.
- **Pharmacy Staff** mengarahkan non-covered quantity melalui `SOP-MF-RJ-005` ketika berlaku.

### 5.2 Shortage terjadi setelah Sales Order dibentuk

- **Pharmacy Technician** mencatat Backorder atau sumber stok lain yang disetujui untuk produk obat yang sama.
- **Medication Fulfillment Application** mempertahankan identitas obat yang diterima dan menampilkan unresolved outcome.

### 5.3 Final Dispense Review gagal

- **Pharmacist** mencatat failed review dan tidak mengotorisasi handover.
- **Medication Fulfillment Application** tidak membentuk Sales Invoice BPJS maupun Medication Handover.

### 5.4 Patient tidak mengambil obat

- **Pharmacy Supervisor** menerapkan `SOP-MF-RJ-007`.
- **Medication Fulfillment Application** tidak membentuk atau membatalkan Sales Invoice BPJS untuk No-Show.

## 6. Kriteria Penyelesaian

1. Nilai Patient-payable nol dan payment disposition `Not Required`.
2. Sales Invoice BPJS dan Medication Handover ditampilkan sebagai satu successful accountable outcome.
3. Dispense Order berstatus `Completed`, Authorized Recipient teridentifikasi, dan Inventory Issue ditampilkan.
4. Pharmacy Sales Order berstatus `Resolved`, atau tetap `Active` dengan unresolved outcome yang ditampilkan secara eksplisit.
5. Queue `Done` tidak digunakan sebagai bukti Medication Handover.

## 7. Referensi

- [Domain Medication Fulfillment](../medication-fulfillment-domain-id.md), khususnya `BR-MF-020`–`BR-MF-026`, `BR-MF-029`–`BR-MF-045`, `BR-MF-066`, `BR-MF-068`–`BR-MF-069`, `BR-MF-073`–`BR-MF-079`, `BR-MF-081`–`BR-MF-083`, `BR-MF-088`, `BR-MF-090`, dan `BR-MF-095`.
- [Workflow Outpatient Medication Fulfillment](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-004`.
- [Domain Patient Tracker](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), `BR-TRK-045`, `BR-TRK-045a`, dan `BR-TRK-046`.
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
