# SOP MF-RJ-001 — Mengambil dan Memetakan Antrean Apotek Rawat Jalan

**Status artefak:** Spesifikasi operasional target kanonis

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-001`

**Sumber kanonis bahasa Inggris:** [SOP MF-RJ-001 — Acquire and Map Outpatient Pharmacy Queue](./SOP-MF-RJ-001-Acquire-and-Map-Outpatient-Pharmacy-Queue.md)

**Status terminologi aplikasi:** `Apotek` dan `Apotek Rajal` merupakan istilah aplikasi yang telah ditetapkan. Kontrol lain dijelaskan berdasarkan tindakan operasional karena label target-workflow yang disetujui belum tersedia.

## 1. Tujuan

Menyediakan prosedur berulang untuk mengaitkan satu Pharmacy Queue Entry Rawat Jalan dengan setiap medication demand yang berlaku melalui Tracker Mapping atau Manual Mapping.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Patient or Caregiver | Manusia | Mengambil Queue Number dan menunjukkan bukti tracker, registrasi, Prescription, atau direct request yang tersedia. |
| Pharmacy Staff | Manusia | Mengidentifikasi Queue Entry yang belum terselesaikan, memverifikasi bukti, mencatat Physical Prescription yang ditunjukkan bila berlaku, dan membentuk Manual Mapping. |
| Medication Fulfillment Application | Aplikasi | Mencoba Tracker Mapping, mencatat Outpatient Queue Mapping, dan menampilkan progress per demand. |
| Patient Tracker | Subsistem | Membentuk Pharmacy Queue Entry, menetapkan Queue Number, mencatat `CreatedAt`, dan mempertahankan authority lifecycle antrean. |

## 3. Prasyarat

1. Pharmacy Staff telah sign in ke workspace `Apotek Rajal` dengan izin queue mapping.
2. Queue Session apotek Rawat Jalan tersedia.
3. Patient meminta Queue Number secara langsung atau menunjukkan bukti tracker atau registrasi yang valid.
4. Setiap medication demand yang dipilih untuk mapping telah memiliki sumber accountable atau dicatat selama Manual Mapping.

## 4. Langkah Operasional

1. **Patient or Caregiver** meminta Queue Number apotek Rawat Jalan atau menunjukkan bukti tracker atau registrasi yang valid.
2. **Patient Tracker** membentuk Pharmacy Queue Entry, menetapkan Queue Number, mencatat `CreatedAt`, serta menampilkan atau menyediakan Queue Number.
3. **Medication Fulfillment Application** mencoba Tracker Mapping ketika bukti yang diberikan menemukan satu atau beberapa medication demand yang berlaku.
4. **Medication Fulfillment Application** mencatat Outpatient Queue Mapping terpisah untuk setiap demand yang ditemukan dan menampilkan masing-masing demand beserta progress authoritative-nya.
5. Jika Queue Entry tetap unresolved, **Pharmacy Staff** melakukan administrative Queue Number call tanpa mencatat `ServedAt` atau `DoneAt`.
6. **Patient or Caregiver** menunjukkan Queue Number dan bukti Prescription, registrasi, atau direct request yang tersedia.
7. **Pharmacy Staff** memilih unresolved Queue Entry dan mengidentifikasi setiap Prescription atau Pharmacy Sales Order existing yang berlaku.
8. Ketika Physical Prescription ditunjukkan, **Pharmacy Staff** mencatatnya sebelum meminta Prescription Review; ketika Direct Medication Request ditunjukkan, **Pharmacy Staff** menilainya berdasarkan prosedur penerimaan.
9. **Pharmacy Staff** mencatat Manual Mapping antara Queue Entry dan setiap applicable demand yang teridentifikasi.
10. **Medication Fulfillment Application** menampilkan setiap mapping yang berhasil sebagai `Mapped` dan mempertahankan setiap demand sebagai record terpisah.
11. **Pharmacy Staff** memverifikasi bahwa seluruh applicable demand terlihat di bawah Queue Entry yang sama dan menyerahkan proses profesional serta payer kepada SOP yang berlaku.

## 5. Pengecualian Operasional

### 5.1 Bukti tidak menemukan medication demand

- **Medication Fulfillment Application** mempertahankan Queue Entry sebagai `Unmapped` dan memblokir clearance yang bergantung pada mapping.
- **Pharmacy Staff** memperoleh bukti tambahan dan mengulangi Manual Mapping; staff tidak membuat Electronic Prescription.

### 5.2 Direct Medication Request ditolak

- **Pharmacy Staff** tidak mencatat Direct Medication Request maupun Pharmacy Sales Order.
- **Patient Tracker** mempertahankan Queue Entry yang masih Waiting sampai withdrawal policy eksternal diterapkan.

### 5.3 Mapping existing tidak benar

- **Pharmacy Staff** mencatat koreksi mapping yang accountable.
- **Medication Fulfillment Application** mempertahankan riwayat mapping, Prescription Review, dan Pharmacy Sales Order sebelumnya.

## 6. Kriteria Penyelesaian

1. Setiap applicable demand memiliki outcome `Outpatient Queue Mapped` terpisah yang terhubung ke Queue Number yang sama; atau Queue Entry tetap terlihat `Unmapped` menunggu bukti.
2. Mapping tidak mencatat `ServedAt` atau `DoneAt`.
3. Setiap mapped demand mempertahankan identitas Prescription, Pharmacy Sales Order, Sales Invoice, dan Dispense Order sendiri bila berlaku.

## 7. Referensi

- [Domain Medication Fulfillment](../medication-fulfillment-domain-id.md), khususnya `BR-MF-061`–`BR-MF-065`, `BR-MF-082`, dan `BR-MF-084`–`BR-MF-087`.
- [Workflow Outpatient Medication Fulfillment](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-001`.
- [Domain Patient Tracker](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), khususnya `BR-TRK-026`–`BR-TRK-035`.
