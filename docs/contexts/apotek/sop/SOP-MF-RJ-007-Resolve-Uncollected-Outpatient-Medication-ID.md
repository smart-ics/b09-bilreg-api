# SOP MF-RJ-007 — Menyelesaikan Obat Rawat Jalan yang Tidak Diambil

**Status artefak:** Spesifikasi operasional target kanonis

**Bounded context:** Medication Fulfillment

**Workflow:** `WF-MF-RJ-007`

**Sumber kanonis bahasa Inggris:** [SOP MF-RJ-007 — Resolve Uncollected Outpatient Medication](./SOP-MF-RJ-007-Resolve-Uncollected-Outpatient-Medication.md)

**Status terminologi aplikasi:** `Apotek` dan `Apotek Rajal` merupakan istilah aplikasi yang telah ditetapkan. Kontrol lain dijelaskan berdasarkan tindakan operasional karena label target-workflow yang disetujui belum tersedia.

## 1. Tujuan

Menyediakan prosedur manual berulang untuk memberikan expiry terotorisasi, disposition Inventory, dan outcome komersial sesuai payer kepada obat Prepared atau In-Transit yang tidak diambil.

## 2. Aktor dan Tanggung Jawab

| Aktor | Tipe | Tanggung jawab operasional |
|---|---|---|
| Pharmacy Supervisor | Manusia | Mengotorisasi dan mencatat akhir manual kesempatan pengambilan. |
| Pharmacy Staff | Manusia | Mengidentifikasi demand dan jumlah terdampak serta memverifikasi operational outcome yang dihasilkan. |
| Medication Fulfillment Application | Aplikasi | Mencatat No-Show, `Expired`, `Collection Window Expired`, Unfulfilled Medication Outcome, dan final Sales Order progress. |
| Inventory | Subsistem | Menentukan eligibility return dan menyediakan authoritative return atau final disposition lain. |
| Tata Rekening | Subsistem | Menyediakan Credit Note, Refund, atau final commercial outcome lain bagi obat yang telah dibayar. |
| Patient Tracker | Subsistem | Mempertahankan queue lifecycle existing dan tidak membalik `DoneAt`. |

## 3. Prasyarat

1. Pharmacy Supervisor dan Pharmacy Staff telah sign in dengan izin exception resolution yang diperlukan.
2. Obat berstatus `Prepared` atau In-Transit, dan Medication Handover belum selesai.
3. Patient tidak mengambil obat.
4. Authorized closing role, affected quantity, reason, dan effective business time diketahui.
5. Keberadaan Sales Invoice dan financial disposition terlihat untuk setiap payer allocation.

## 4. Langkah Operasional

1. **Pharmacy Staff** membuka Queue Entry yang tidak diambil dan memverifikasi Pharmacy Sales Order sumber, Dispense Order, jumlah terdampak, payer allocation, ketiadaan handover, serta current Inventory disposition.
2. **Pharmacy Supervisor** mengonfirmasi bahwa kesempatan pengambilan yang diizinkan telah berakhir; batas waktu numerik otomatis atau rekaan tidak digunakan.
3. **Pharmacy Supervisor** mencatat manual uncollected-medication resolution beserta responsible party, effective business time, affected quantity, dan reason `Collection Window Expired`.
4. **Medication Fulfillment Application** mencatat Patient sebagai No-Show untuk fulfillment terdampak dan mengubah setiap Dispense Order terdampak menjadi `Expired`.
5. **Medication Fulfillment Application** mencatat Unfulfilled Medication Outcome bagi setiap affected quantity dan mempertahankan Source Traceability.
6. **Inventory** menilai obat reserved atau In-Transit dan hanya menyediakan Return to Stock ketika eligible, atau memberikan final Inventory disposition lain.
7. **Medication Fulfillment Application** menampilkan authoritative Inventory disposition tanpa menyimpulkan mutasi stok.
8. Untuk allocation BPJS uninvoiced, **Medication Fulfillment Application** mempertahankan ketiadaan Sales Invoice BPJS dan hanya menyelesaikan konsekuensi fulfillment serta Inventory.
9. Untuk allocation Pasien Umum paid, **Medication Fulfillment Application** mengirim konsekuensi finansial yang diperlukan kepada **Tata Rekening** dan mempertahankan Pharmacy Sales Order sebagai `Active`.
10. **Tata Rekening** menyediakan Credit Note, Refund, atau final commercial outcome accountable lain; **Medication Fulfillment Application** menampilkan hasilnya terhadap originating allocation.
11. Untuk mixed coverage, **Medication Fulfillment Application** mencatat konsekuensi covered uninvoiced dan Patient-payable paid secara terpisah.
12. **Patient Tracker** mempertahankan Queue Entry state dan `DoneAt` existing; No-Show resolution tidak membuka kembali antrean.
13. **Medication Fulfillment Application** mengubah Pharmacy Sales Order menjadi `Resolved` hanya setelah setiap accepted quantity, Inventory disposition, dan konsekuensi komersial wajib final.
14. **Pharmacy Staff** memverifikasi final state atau outstanding financial consequence yang ditampilkan secara eksplisit.

## 5. Pengecualian Operasional

### 5.1 Inventory menolak Return to Stock

- **Inventory** menyediakan rejection dan final disposition accountable lain.
- **Medication Fulfillment Application** mempertahankan Sales Order unresolved sampai disposition tersebut ditampilkan.

### 5.2 Konsekuensi finansial paid tetap outstanding

- **Medication Fulfillment Application** menampilkan Dispense Order sebagai `Expired` dan Pharmacy Sales Order sebagai `Active`.
- **Pharmacy Staff** tidak menghapus atau mereklasifikasikan paid consequence sebagai outcome BPJS uninvoiced.
- **Tata Rekening** menyelesaikan financial resolution yang diperlukan.

### 5.3 Queue sudah `Done`

- **Patient Tracker** mempertahankan `DoneAt` tanpa perubahan.
- **Pharmacy Supervisor** melanjutkan Medication Fulfillment resolution tanpa membuka atau menyelesaikan antrean kembali.

## 6. Kriteria Penyelesaian

1. Setiap Dispense Order terdampak berstatus `Expired` dengan reason `Collection Window Expired`, responsible party, effective business time, dan affected quantity.
2. Setiap affected quantity memiliki Unfulfilled Medication Outcome dan authoritative Inventory disposition.
3. Sales Invoice BPJS tidak tersedia ketika BPJS handover tidak terjadi.
4. Pharmacy Sales Order Pasien Umum paid tetap `Active` sampai final Credit Note, Refund, atau commercial outcome lain terlihat.
5. Pharmacy Sales Order menjadi `Resolved` hanya setelah seluruh konsekuensi fulfillment dan komersial final.
6. Queue `DoneAt` tetap tidak berubah.

## 7. Referensi

- [Domain Medication Fulfillment](../medication-fulfillment-domain-id.md), khususnya `BR-MF-018`–`BR-MF-019`, `BR-MF-027`, `BR-MF-045`–`BR-MF-047`, `BR-MF-052`–`BR-MF-060`, `BR-MF-069`, `BR-MF-078`–`BR-MF-080`, dan `BR-MF-095`.
- [Workflow Outpatient Medication Fulfillment](../outpatient-medication-fulfillment-workflow-id.md), `WF-MF-RJ-007`.
- [Domain Patient Tracker](../../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md).
- [Domain Tata Rekening](../../../contexts/TataRekening/02-domain.md).
