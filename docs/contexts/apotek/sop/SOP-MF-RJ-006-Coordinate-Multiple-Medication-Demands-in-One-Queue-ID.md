# SOP MF-RJ-006 — Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrean

**Status artefak:** Spesifikasi operasional target kanonis

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-006`

**Sumber kanonis bahasa Inggris:** [SOP MF-RJ-006 — Coordinate Multiple Medication Demands in One Queue](./SOP-MF-RJ-006-Coordinate-Multiple-Medication-Demands-in-One-Queue.md)

**Status terminologi aplikasi:** `Apotek` dan `Apotek Rajal` merupakan istilah aplikasi yang telah ditetapkan. Kontrol lain dijelaskan berdasarkan tindakan operasional karena label target-workflow yang disetujui belum tersedia.

## 1. Tujuan

Menyediakan prosedur berulang untuk mengoordinasikan dua atau lebih medication demand yang accountable secara independen dalam satu antrean dan pickup session Rawat Jalan tanpa menggabungkan record-nya.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Patient or Caregiver | Manusia | Menyelesaikan interaksi sesuai payer, hadir untuk satu coordinated pickup, menerima consolidated education, dan menerima obat yang berlaku. |
| Pharmacy Staff | Manusia | Memverifikasi mapping dan progress terpisah, mengoordinasikan payer readiness, menyampaikan exception, dan melakukan satu pickup call. |
| Pharmacy Technician | Manusia | Menyiapkan setiap Dispense Order yang telah memperoleh clearance secara terpisah. |
| Pharmacist | Manusia | Memverifikasi penerima, menelaah setiap Prepared Medication, dan memberikan consolidated education dengan instruksi khusus obat. |
| Medication Fulfillment Application | Aplikasi | Memproyeksikan progress per demand serta mencatat allocation, invoice, Dispense Order, dispense, dan handover outcome secara terpisah. |
| Patient Tracker | Subsistem | Mempertahankan satu Queue Entry dengan satu `CreatedAt`, maksimal satu `ServedAt`, dan satu `DoneAt`. |
| Cashier or Payment Authority | Manusia atau Subsistem | Menyediakan Payment Clearance bagi Patient-payable demand yang berlaku. |
| SEP and Fornas Authorities | Subsistem | Menyediakan bukti coverage bagi BPJS demand yang berlaku. |
| Inventory | Subsistem | Menyediakan outcome reservation, issue, dan disposition per Dispense Order. |

## 3. Prasyarat

1. Staff yang berpartisipasi telah sign in dengan izin yang diperlukan.
2. Satu Pharmacy Queue Entry memiliki Outpatient Queue Mapping terpisah ke setidaknya dua medication demand.
3. Setiap Prescription memiliki Prescription Review sendiri.
4. Setiap accepted source memiliki Pharmacy Sales Order dan active primary outpatient Dispense Order sendiri.

## 4. Langkah Operasional

1. **Pharmacy Staff** membuka Queue Entry yang sama di `Apotek Rajal`.
2. **Medication Fulfillment Application** menampilkan setiap mapped demand secara terpisah beserta sumber, Pharmacy Sales Order, payer, Billing Allocation, Fulfillment Allocation, Sales Invoice, clearance, dan progress Dispense Order.
3. **Pharmacy Staff** memverifikasi bahwa tidak ada demand, Pharmacy Sales Order, Sales Invoice, atau Dispense Order yang digabungkan dengan demand lain.
4. **Pharmacy Staff** menerapkan SOP Pasien Umum, BPJS, atau mixed coverage kepada setiap demand sesuai payer classification.
5. **Cashier or Payment Authority** menyediakan Payment Clearance yang berlaku; **SEP and Fornas Authorities** menyediakan Coverage Clearance yang berlaku.
6. **Pharmacy Technician** menyiapkan setiap released Dispense Order secara terpisah dan mencatat completion masing-masing.
7. Saat preparation pertama yang berlaku dimulai, **Medication Fulfillment Application** mencatat `Medication Preparation Started`; **Patient Tracker** mencatat satu `ServedAt` dan memindahkan Queue Entry yang sama ke In Service.
8. **Medication Fulfillment Application** menampilkan setiap demand yang hendak diambil sebagai `Prepared` atau dengan exception outcome accountable.
9. **Pharmacy Staff** meninjau seluruh progress per demand dan tidak menyatakan demand unresolved sebagai siap.
10. Ketika seluruh included demand siap atau selesai secara accountable untuk pickup yang dimaksud, **Pharmacy Staff** melakukan satu coordinated pickup call.
11. **Patient Tracker** mencatat satu `DoneAt` dan membuat Queue Entry yang sama `Done`.
12. **Pharmacy Staff** menyampaikan setiap exception outcome accountable bersama informasi ready demand.
13. Dengan Patient atau caregiver hadir, **Pharmacist** memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review untuk setiap Prepared Medication, dan mencatat consolidated Patient Education dengan instruksi khusus obat.
14. **Pharmacy Staff** menyelesaikan physical handover setelah otorisasi Pharmacist.
15. **Medication Fulfillment Application** mencatat Medication Dispense dan Medication Handover terhadap setiap Dispense Order dan Pharmacy Sales Order yang berlaku secara terpisah.
16. **Inventory** menyediakan Inventory Issue atau disposition outcome terpisah bagi setiap Dispense Order sumber.

## 5. Pengecualian Operasional

### 5.1 Satu demand tetap unresolved

- **Medication Fulfillment Application** menampilkan demand tersebut sebagai belum siap.
- **Pharmacy Staff** menunda coordinated call kecuali SOP payer yang berlaku mengizinkan dan mencatat accountable partial path yang diterima Patient.

### 5.2 Satu demand memiliki exception outcome accountable

- **Pharmacy Staff** menyertakan resolved exception dalam coordinated communication dan melanjutkan ready demand hanya ketika prosedur payer mengizinkan.
- **Medication Fulfillment Application** mempertahankan exception di bawah Sales Order dan Dispense Order sumbernya.

### 5.3 Satu mapping atau demand memerlukan koreksi

- **Pharmacy Staff** hanya mengoreksi mapping atau demand terdampak.
- **Medication Fulfillment Application** mempertahankan riwayat setiap demand lain dan tidak menyimpulkan bahwa satu handover memenuhi demand lain.

## 6. Kriteria Penyelesaian

1. Queue Entry yang sama menampilkan satu `CreatedAt`, maksimal satu `ServedAt`, dan satu `DoneAt`.
2. Setiap Prescription, Pharmacy Sales Order, Sales Invoice, dan Dispense Order mempertahankan identitas serta progress independen.
3. Satu pickup call tercatat, sedangkan setiap applicable demand memiliki fakta Medication Handover sendiri atau exception outcome accountable.

## 7. Referensi

- [Domain Medication Fulfillment](../medication-fulfillment-domain-id.md), khususnya `BR-MF-011`, `BR-MF-015`, `BR-MF-022`, `BR-MF-030`, `BR-MF-056`–`BR-MF-060`, `BR-MF-084`–`BR-MF-088`, dan `BR-MF-095`.
- [Workflow Outpatient Medication Fulfillment](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-006`.
- [Domain Patient Tracker](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), khususnya `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, dan `BR-TRK-045a`.
