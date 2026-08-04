# SOP MF-RJ-003 — Memenuhi Obat untuk Pasien Umum

**Status artefak:** Spesifikasi operasional target kanonis

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-003`

**Sumber kanonis bahasa Inggris:** [SOP MF-RJ-003 — Fulfill Medication for a General Patient](./SOP-MF-RJ-003-Fulfill-Medication-for-a-General-Patient.md)

**Status terminologi aplikasi:** `Apotek` dan `Apotek Rajal` merupakan istilah aplikasi yang telah ditetapkan. Kontrol lain dijelaskan berdasarkan tindakan operasional karena label target-workflow yang disetujui belum tersedia.

## 1. Tujuan

Menyediakan prosedur berulang untuk memperoleh Purchase Confirmation lisan, membentuk dan memberikan clearance pada Sales Invoice Pasien Umum, menyiapkan obat, serta menyelesaikan Medication Handover Rawat Jalan yang accountable.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Patient or Caregiver | Manusia | Mengonfirmasi atau menolak pembelian yang dihitung, membayar bila mengonfirmasi, hadir untuk pickup, menerima edukasi, dan menerima obat ketika authorized. |
| Pharmacy Staff | Manusia | Menyampaikan nilai yang dihitung, mencatat Sales Invoice yang dikonfirmasi, mengoordinasikan readiness, dan melakukan pickup call. |
| Pharmacy Supervisor | Manusia | Mengotorisasi manual uncollected-medication resolution ketika Patient tidak mengambil obat yang telah disiapkan. |
| Cashier or Payment Authority | Manusia atau Subsistem | Menerima pembayaran dan memberikan Payment Clearance. |
| Pharmacy Technician | Manusia | Menyiapkan atau meracik obat berdasarkan released Dispense Order. |
| Pharmacist | Manusia | Memverifikasi penerima, menyelesaikan Final Dispense Review, dan memberikan Patient Education. |
| Medication Fulfillment Application | Aplikasi | Menampilkan allocation dan nilai, mencatat invoice serta clearance, melacak preparation, dan mencatat dispense serta handover. |
| Patient Tracker | Subsistem | Mencatat `ServedAt` saat preparation dimulai dan `DoneAt` saat pickup call. |
| Inventory | Subsistem | Menyediakan outcome reservation, issue, dan return disposition. |
| Tata Rekening | Subsistem | Menerima Financial Charge dan menyediakan outcome koreksi yang diperlukan. |

## 3. Prasyarat

1. Staff yang berpartisipasi telah sign in dengan izin yang diperlukan.
2. Outpatient Queue Mapping, Pharmacy Sales Order aktif, Billing Allocation Patient-payable, dan Pricing Snapshot yang dihitung telah ditampilkan.
3. Belum ada Sales Invoice untuk penjualan Patient-payable yang diusulkan.
4. Dispense Order yang berlaku tersedia atau dapat dibentuk dari Fulfillment Allocation.

## 4. Langkah Operasional

1. **Pharmacy Staff** membuka mapped demand di `Apotek Rajal` dan memverifikasi nilai Patient-payable serta Billing Allocation yang dihitung.
2. **Pharmacy Staff** menerima Patient saat Manual Mapping atau melakukan administrative Queue Number call setelah Tracker Mapping; **Patient Tracker** tidak mencatat `ServedAt` atau `DoneAt` untuk interaksi ini.
3. **Pharmacy Staff** menyampaikan nilai yang dihitung secara lisan sebelum Sales Invoice tersedia.
4. **Patient or Caregiver** memberikan konfirmasi pembelian secara lisan.
5. **Pharmacy Staff** mencatat transaksi yang dikonfirmasi; **Medication Fulfillment Application** membentuk Sales Invoice hanya dari Billing Allocation yang dikonfirmasi dan menampilkan identifier serta nilainya.
6. **Cashier or Payment Authority** menerima pembayaran dan memberikan Payment Clearance bagi Sales Invoice tersebut.
7. **Medication Fulfillment Application** menampilkan Payment Clearance dan membentuk Fulfillment Clearance bagi jumlah Dispense Order yang berlaku.
8. **Inventory** mengamankan Stock Reservation yang diperlukan bila belum tersedia; **Medication Fulfillment Application** menampilkan reservation outcome.
9. **Pharmacy Technician** memulai Medication Preparation hanya setelah Dispense Order berstatus released.
10. **Medication Fulfillment Application** mencatat `Medication Preparation Started`; **Patient Tracker** memindahkan Queue Entry ke In Service dan mencatat `ServedAt`.
11. **Pharmacy Technician** menyelesaikan preparation atau compounding dan mencatat completion; **Medication Fulfillment Application** menampilkan Dispense Order sebagai `Prepared`.
12. **Pharmacy Staff** memverifikasi setiap Dispense Order yang hendak diserahkan telah `Prepared` atau memiliki exception outcome accountable.
13. **Pharmacy Staff** melakukan satu coordinated pickup call; **Patient Tracker** membuat Queue Entry `Done` dan mencatat `DoneAt`.
14. Dengan Patient atau caregiver hadir, **Pharmacist** memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review, dan mencatat Patient Education yang berlaku.
15. **Medication Fulfillment Application** memblokir handover sampai persyaratan review, recipient, dan education dicatat.
16. **Pharmacy Staff** menyelesaikan physical handover setelah otorisasi Pharmacist; **Medication Fulfillment Application** mencatat Medication Dispense dan Medication Handover untuk setiap jumlah yang berlaku.
17. **Inventory** menyediakan outcome Inventory Issue authoritative; **Medication Fulfillment Application** menampilkan Dispense Order sebagai `Completed` ketika seluruh outcome wajib tersedia.
18. **Medication Fulfillment Application** menampilkan Pharmacy Sales Order sebagai `Resolved` hanya ketika setiap accepted quantity dan konsekuensi komersial final.

## 5. Pengecualian Operasional

### 5.1 Patient menolak sebelum invoice dibentuk

- **Pharmacy Staff** mencatat penolakan dan tidak membentuk Sales Invoice.
- **Medication Fulfillment Application** mencatat allocation sebagai declined atau commercially unallocated dan meminta **Inventory** melepaskan reservation yang tidak digunakan.

### 5.2 Nilai berubah sebelum invoice dibentuk

- **Medication Fulfillment Application** menampilkan nilai hasil perhitungan yang direvisi.
- **Pharmacy Staff** menyampaikannya kembali dan memperoleh konfirmasi lisan baru sebelum mencatat transaksi.

### 5.3 Pembayaran belum selesai atau invoice existing memerlukan koreksi

- **Medication Fulfillment Application** mempertahankan blokir Medication Preparation selama Payment Clearance belum tersedia.
- **Pharmacy Staff** hanya membatalkan ketika lifecycle Sales Invoice yang ditampilkan mengizinkan; selain itu **Tata Rekening** menyediakan Financial Adjustment, Credit Note, Refund, atau outcome accountable lain.

### 5.4 Shortage atau Final Dispense Review gagal

- **Pharmacy Technician** mencatat Backorder atau sumber lain yang disetujui untuk produk obat yang sama; substitution tidak dilakukan.
- **Pharmacist** mencatat failed review outcome dan tidak mengotorisasi handover.
- **Medication Fulfillment Application** mencatat Unfulfilled Medication Outcome yang berlaku dan mempertahankan konsekuensi finansial wajib tetap terlihat.

### 5.5 Patient tidak mengambil obat

- **Pharmacy Supervisor** menerapkan `SOP-MF-RJ-007`; Queue `DoneAt` tidak dibalik.

## 6. Kriteria Penyelesaian

1. Sales Invoice terlihat financially cleared.
2. Dispense Order berstatus `Completed`, dan Medication Handover mengidentifikasi Authorized Recipient serta effective time.
3. Inventory Issue ditampilkan sebagai outcome Inventory authoritative.
4. Pharmacy Sales Order berstatus `Resolved`, atau tetap `Active` dengan konsekuensi fulfillment atau komersial unresolved yang ditampilkan secara eksplisit.
5. Queue `Done` tidak digunakan sebagai bukti Medication Handover.

## 7. Referensi

- [Domain Medication Fulfillment](../medication-fulfillment-domain-id.md), khususnya `BR-MF-020`–`BR-MF-028`, `BR-MF-033`–`BR-MF-046`, `BR-MF-056`–`BR-MF-060`, `BR-MF-067`–`BR-MF-072`, `BR-MF-076`–`BR-MF-083`, `BR-MF-088`, dan `BR-MF-095`.
- [Workflow Outpatient Medication Fulfillment](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-003`.
- [Domain Patient Tracker](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), `BR-TRK-045`, `BR-TRK-045a`, dan `BR-TRK-046`.
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
