# SOP MF-RJ-002 — Menerima Permintaan Obat Rawat Jalan

**Status artefak:** Spesifikasi operasional target kanonis

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-002`

**Sumber kanonis bahasa Inggris:** [SOP MF-RJ-002 — Accept Outpatient Medication Demand](./SOP-MF-RJ-002-Accept-Outpatient-Medication-Demand.md)

**Status terminologi aplikasi:** `Apotek` dan `Apotek Rajal` merupakan istilah aplikasi yang telah ditetapkan. Kontrol lain dijelaskan berdasarkan tindakan operasional karena label target-workflow yang disetujui belum tersedia.

## 1. Tujuan

Menyediakan prosedur berulang untuk menerima Prescription yang telah ditelaah atau Direct Medication Request yang diotorisasi serta membentuk Pharmacy Sales Order dan primary outpatient Dispense Order yang traceable.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Pharmacist | Manusia | Menelaah setiap Prescription Line, meminta clarification, mengotorisasi substitution yang eligible sebelum Sales Order dibentuk, dan menyelesaikan review. |
| Pharmacy Staff | Manusia | Mencatat Physical Prescription serta menerima, merujuk, atau menolak Direct Medication Request sesuai authority. |
| Pharmacy Technician | Manusia | Meninjau outcome stok eksternal dan memilih Backorder atau sumber lain yang disetujui untuk produk yang sama setelah penerimaan ketika diotorisasi. |
| CPOE or Prescribing Clinician | Subsistem atau Manusia | Menyediakan Prescription authoritative dan merespons Clinical Clarification atau memberikan intent terkoreksi. |
| Medication Catalog | Subsistem | Menyediakan identitas obat dan informasi formularium yang digunakan saat review. |
| Medication Fulfillment Application | Aplikasi | Mencatat review outcome serta membentuk allocation, Pharmacy Sales Order, dan primary Dispense Order yang traceable. |
| Inventory | Subsistem | Menyediakan outcome Stock Availability dan Stock Reservation tanpa menentukan penerimaan profesional. |

## 3. Prasyarat

1. Pharmacy Staff atau Pharmacist yang bertanggung jawab telah sign in ke `Apotek Rajal` dengan izin yang diperlukan.
2. Electronic Prescription tersedia dari sumber authoritative, Physical Prescription telah dicatat, atau Direct Medication Request ditunjukkan kepada Pharmacy Staff yang berwenang.
3. Prescription mengidentifikasi Patient dan Clinical Order sumber.
4. Informasi Medication Catalog dan professional acceptance policy yang berlaku tersedia.

## 4. Langkah Operasional

1. **Medication Fulfillment Application** menampilkan Electronic Prescription, Physical Prescription tercatat, atau Direct Medication Request yang tersedia tanpa mewajibkan kedatangan Patient atau queue mapping.
2. Untuk Prescription, **Pharmacist** memulai Prescription Review dan memverifikasi Patient, sumber, obat, instruksi dosis, jumlah, dan informasi klinis yang tersedia.
3. **Pharmacist** mencatat satu disposition untuk setiap Prescription Line.
4. Ketika clarification diperlukan, **Pharmacist** mencatat `Clarification Required` dan mengirim permintaan clarification accountable kepada **CPOE or Prescribing Clinician**.
5. **CPOE or Prescribing Clinician** memberikan clarification atau Prescription yang dikoreksi atau diganti; **Pharmacist** melanjutkan review berdasarkan informasi authoritative tersebut.
6. Ketika substitution diotorisasi secara profesional, **Pharmacist** mencatat obat asli, substitusi yang diterima, alasan, jumlah terdampak, dan Pharmacist yang bertanggung jawab sebelum Pharmacy Sales Order dibentuk.
7. **Pharmacist** menyelesaikan Prescription Review sebagai `Approved`, `Partially Approved`, atau `Rejected`.
8. Untuk Direct Medication Request, **Pharmacy Staff** mencatat detail request dan menerima sesuai authority, merujuk kepada **Pharmacist**, atau menolaknya.
9. Ketika dirujuk, **Pharmacist** mencatat approval atau decline; **Medication Fulfillment Application** hanya mengizinkan penerimaan setelah approval.
10. Untuk Prescription approved atau partially approved, atau Direct Medication Request yang diterima, **Medication Fulfillment Application** membentuk satu Pharmacy Sales Order dari sumber tersebut dan mempertahankan Source Traceability.
11. **Medication Fulfillment Application** membentuk Billing Allocation dan Fulfillment Allocation yang berlaku secara independen serta menampilkan jumlahnya.
12. **Medication Fulfillment Application** membentuk satu active primary outpatient Dispense Order untuk episode normal dan menampilkan state awalnya.
13. **Inventory** dapat mengembalikan bukti Stock Reservation; **Medication Fulfillment Application** menampilkannya tanpa memperlakukannya sebagai Fulfillment Clearance.
14. **Pharmacist** atau **Pharmacy Staff**, sesuai source path, memverifikasi final review outcome, Pharmacy Sales Order identifier, accepted line, dan Dispense Order identifier.

## 5. Pengecualian Operasional

### 5.1 Tidak ada line yang diterima atau direct request ditolak

- **Medication Fulfillment Application** mencatat `Rejected` untuk Prescription yang ditelaah atau tidak mencatat Direct Medication Request yang ditolak.
- **Medication Fulfillment Application** tidak membentuk Pharmacy Sales Order.

### 5.2 Stok tidak cukup setelah penerimaan

- **Inventory** menampilkan outcome shortage atau discrepancy tanpa mengubah Prescription Review Outcome.
- **Pharmacy Technician** mencatat Backorder atau memilih sumber stok lain yang disetujui untuk produk obat yang sama sesuai authority.
- **Pharmacy Technician** tidak melakukan substitution obat.

### 5.3 Penggantian obat diperlukan setelah Sales Order dibentuk

- **Medication Fulfillment Application** memblokir substitution pada Sales Order Line existing.
- **Pharmacist** meminta Prescription yang dikoreksi atau diganti dan melakukan Prescription Review baru.

## 6. Kriteria Penyelesaian

1. Setiap Prescription Line yang ditelaah memiliki final disposition, atau review tetap terlihat `Clarification Required`.
2. Sumber yang diterima menampilkan Pharmacy Sales Order, Billing Allocation, Fulfillment Allocation, dan primary Dispense Order yang traceable.
3. Prescription rejected atau Direct Medication Request declined tidak memiliki Pharmacy Sales Order.
4. Bukti stok tidak mengubah professional acceptance outcome.

## 7. Referensi

- [Domain Medication Fulfillment](../medication-fulfillment-domain-id.md), khususnya `BR-MF-001`–`BR-MF-019`, `BR-MF-029`–`BR-MF-034`, `BR-MF-050`, `BR-MF-061`, `BR-MF-068`, `BR-MF-083`, `BR-MF-086`, dan `BR-MF-089`.
- [Workflow Outpatient Medication Fulfillment](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-002`.
- [Domain CPOE](../../../contexts/cpoe/CPOE-DOMAIN-ID.md).
