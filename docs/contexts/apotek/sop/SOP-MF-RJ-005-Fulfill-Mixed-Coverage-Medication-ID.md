# SOP MF-RJ-005 — Memenuhi Obat dengan Coverage Campuran

**Status artefak:** Spesifikasi operasional target kanonis

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-005`

**Sumber kanonis bahasa Inggris:** [SOP MF-RJ-005 — Fulfill Mixed-Coverage Medication](./SOP-MF-RJ-005-Fulfill-Mixed-Coverage-Medication.md)

**Status terminologi aplikasi:** `Apotek` dan `Apotek Rajal` merupakan istilah aplikasi yang telah ditetapkan. Kontrol lain dijelaskan berdasarkan tindakan operasional karena label target-workflow yang disetujui belum tersedia.

## 1. Tujuan

Menyediakan prosedur berulang untuk memisahkan tanggung jawab komersial BPJS-covered dan Patient-payable sambil mengoordinasikan jumlah yang telah memperoleh clearance untuk satu pickup Rawat Jalan.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Patient or Caregiver | Manusia | Mengonfirmasi atau menolak bagian Patient-payable, membayar bila mengonfirmasi, hadir untuk pickup, menerima edukasi, dan menerima obat ketika authorized. |
| Pharmacy Staff | Manusia | Memisahkan Billing Allocation, menyampaikan nilai Patient-payable, mencatat invoice yang dikonfirmasi, mengoordinasikan readiness, dan melakukan pickup call. |
| Pharmacy Supervisor | Manusia | Mengotorisasi manual uncollected-medication resolution ketika Patient tidak mengambil obat yang telah disiapkan. |
| Cashier or Payment Authority | Manusia atau Subsistem | Menerima pembayaran dan memberikan Payment Clearance untuk Sales Invoice Pasien Umum. |
| Pharmacy Technician | Manusia | Menyiapkan atau meracik obat yang telah memperoleh clearance berdasarkan Dispense Order. |
| Pharmacist | Manusia | Memverifikasi penerima, menyelesaikan Final Dispense Review, dan memberikan Patient Education. |
| SEP and Fornas Authorities | Subsistem | Menyediakan validitas SEP dan coverage item-level. |
| Medication Fulfillment Application | Aplikasi | Mempertahankan allocation dan clearance sesuai payer serta mencatat invoice terpisah dengan coordinated handover. |
| Patient Tracker | Subsistem | Mencatat satu `ServedAt` dan satu `DoneAt` bagi Queue Entry yang sama. |
| Inventory | Subsistem | Menyediakan outcome reservation, issue, dan return disposition. |
| Tata Rekening | Subsistem | Menerima dan menyelesaikan konsekuensi finansial sesuai payer. |

## 3. Prasyarat

1. Staff yang berpartisipasi telah sign in dengan izin yang diperlukan.
2. Satu Pharmacy Sales Order aktif memiliki BPJS-covered quantity dan Patient-payable quantity.
3. SEP valid dan mapping Fornas authoritative mengidentifikasi covered dan non-covered quantity.
4. Fulfillment Allocation dan Dispense Order yang berlaku telah ditampilkan.
5. Belum ada Sales Invoice Pasien Umum atau BPJS untuk allocation yang diusulkan, kecuali prosedur dilanjutkan setelah exception accountable.

## 4. Langkah Operasional

1. **Pharmacy Staff** membuka mixed-coverage demand di `Apotek Rajal` dan memverifikasi jumlah Pharmacy Sales Order serta payer classification.
2. **Pharmacy Staff** mencatat Billing Allocation BPJS-covered dan Patient-payable secara terpisah tanpa mengubah identitas atau jumlah obat yang diterima.
3. **SEP and Fornas Authorities** menyediakan SEP valid dan coverage item-level; **Medication Fulfillment Application** menampilkan Coverage Clearance untuk covered quantity.
4. **Medication Fulfillment Application** menghitung dan menampilkan nilai Patient-payable dari non-covered Billing Allocation.
5. **Pharmacy Staff** menyampaikan nilai tersebut secara lisan sebelum Sales Invoice Pasien Umum tersedia.
6. **Patient or Caregiver** memberikan konfirmasi lisan atas bagian Patient-payable.
7. **Pharmacy Staff** mencatat transaksi Patient-payable yang dikonfirmasi; **Medication Fulfillment Application** membentuk Sales Invoice Pasien Umum terpisah dari allocation tersebut.
8. **Cashier or Payment Authority** menerima pembayaran dan memberikan Payment Clearance untuk Sales Invoice Pasien Umum.
9. **Medication Fulfillment Application** membentuk Fulfillment Clearance untuk covered quantity dari Coverage Clearance dan Patient-payable quantity dari Payment Clearance.
10. **Inventory** mengamankan Stock Reservation yang diperlukan dan menyediakan outcome-nya.
11. Setelah setiap jumlah yang hendak diserahkan memiliki clearance yang berlaku, **Pharmacy Technician** memulai dan menyelesaikan Medication Preparation.
12. **Medication Fulfillment Application** mencatat `Medication Preparation Started` pertama; **Patient Tracker** memindahkan Queue Entry yang sama ke In Service dan mencatat satu `ServedAt`.
13. **Medication Fulfillment Application** menampilkan setiap Dispense Order yang hendak diserahkan sebagai `Prepared` atau dengan exception outcome accountable.
14. **Pharmacy Staff** melakukan satu coordinated pickup call; **Patient Tracker** membuat Queue Entry yang sama `Done` dan mencatat satu `DoneAt`.
15. Dengan Patient atau caregiver hadir, **Pharmacist** memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review, dan mencatat Patient Education.
16. **Pharmacy Staff** menyelesaikan physical handover setelah otorisasi Pharmacist.
17. **Medication Fulfillment Application** membentuk Sales Invoice BPJS hanya bersama handover berhasil, mencatat Medication Dispense dan Medication Handover bagi seluruh jumlah yang berlaku, serta mempertahankan kedua allocation sesuai payer.
18. **Inventory** menyediakan outcome Inventory Issue; **Medication Fulfillment Application** menampilkan progress final Dispense Order dan Pharmacy Sales Order.

## 5. Pengecualian Operasional

### 5.1 Patient menolak bagian non-covered sebelum invoice dibentuk

- **Pharmacy Staff** mencatat penolakan dan tidak membentuk Sales Invoice Pasien Umum.
- **Medication Fulfillment Application** mencatat Patient-payable allocation sebagai declined atau commercially unallocated dan mengizinkan bagian covered berjalan secara independen.

### 5.2 Item tidak memiliki coverage Fornas authoritative

- **Medication Fulfillment Application** menampilkan jumlah terdampak sebagai not covered.
- **Pharmacy Staff** mereklasifikasikannya hanya melalui Billing Allocation Patient-payable accountable, menyampaikan nilai revisi, dan memperoleh konfirmasi lisan baru.

### 5.3 Tidak semua intended quantity memiliki clearance

- **Medication Fulfillment Application** memblokir coordinated preparation dan pickup bagi uncleared quantity.
- **Pharmacy Staff** menyelesaikan jalur coverage atau payment yang berlaku sebelum melanjutkan.

### 5.4 Invoice existing, non-fulfillment, atau No-Show memerlukan koreksi

- **Medication Fulfillment Application** mempertahankan konsekuensi covered dan Patient-payable secara terpisah.
- **Tata Rekening** menyediakan koreksi yang diperlukan untuk bagian paid; Sales Invoice BPJS yang belum ada tetap tidak tersedia sampai handover berhasil.
- **Pharmacy Supervisor** menerapkan `SOP-MF-RJ-007` untuk obat yang tidak diambil.

## 6. Kriteria Penyelesaian

1. Billing Allocation covered dan Patient-payable tetap terlihat terpisah.
2. Sales Invoice Pasien Umum yang financially cleared dan Sales Invoice BPJS pada waktu handover terpisah serta traceable ke Pharmacy Sales Order yang sama.
3. Satu coordinated Medication Handover mencatat setiap applicable quantity dan Authorized Recipient.
4. Pharmacy Sales Order berstatus `Resolved`, atau tetap `Active` dengan konsekuensi unresolved sesuai payer yang ditampilkan secara eksplisit.

## 7. Referensi

- [Domain Medication Fulfillment](../medication-fulfillment-domain-id.md), khususnya `BR-MF-015`, `BR-MF-020`–`BR-MF-028`, `BR-MF-040`–`BR-MF-046`, `BR-MF-056`–`BR-MF-060`, `BR-MF-070`–`BR-MF-078`, dan `BR-MF-090`–`BR-MF-095`.
- [Workflow Outpatient Medication Fulfillment](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-005`.
- [Domain Patient Tracker](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
