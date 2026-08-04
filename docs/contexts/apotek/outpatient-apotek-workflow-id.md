# Workflow Outpatient Apotek

**Status artifact:** Pendamping semantik workflow bisnis Bahasa Indonesia

**Bounded context:** Apotek (`Pelayanan Obat Pasien`)

**Care setting:** Apotek Rawat Jalan

**Sumber canonical English:** [outpatient-apotek-workflow.md](./outpatient-apotek-workflow.md)

**Domain authoritative:** [Domain Apotek](./apotek-domain-id.md)

## 1. Gambaran Umum Workflow

Workflow ini mengoordinasikan pelayanan obat Rawat Jalan sejak Pasien memperoleh Nomor Antrean Apotek sampai Medication Handover atau outcome non-fulfillment yang accountable.

Cakupannya meliputi Electronic Prescription, Physical Prescription yang dicatat, dan Direct Medication Request; Tracker Mapping dan Manual Mapping; jalur komersial Pasien Umum, BPJS, dan mixed coverage; satu Pharmacy Queue Entry yang dimappingkan ke beberapa medication demand; dispensing; pickup; Final Dispense Review; Patient Education; serta penyelesaian No-Show.

Prescription Review dapat berjalan sebelum Pasien datang dan secara independen dari queue mapping. Progress komersial dan physical fulfillment tetap independen serta dikoordinasikan melalui Pharmacy Sales Order.

```text
Pharmacy Queue Entry dan medication demand
  -> Outpatient Queue Mapping
  -> Prescription Review atau penerimaan direct request
  -> Pharmacy Sales Order
       -> Billing Allocation -> Sales Invoice sesuai timing payer
       -> Fulfillment Allocation -> Dispense Order
  -> clearance yang diwajibkan
  -> Medication Preparation
  -> pickup call
  -> telaah Pharmacist, verifikasi penerima, dan edukasi
  -> Medication Handover atau penyelesaian non-fulfillment yang accountable
```

## 2. Authority Domain dan Referensi

Workflow ini menerapkan spesifikasi domain yang direferensikan. Workflow ini tidak mendefinisikan ulang Ubiquitous Language, Aggregate boundary, Business Rules, state, lifecycle, atau Domain Events.

| Authority | Tanggung jawab yang digunakan workflow ini |
|---|---|
| [Domain Apotek](./apotek-domain.md) | Prescription Review, Pharmacy Sales Order, allocation, timing Sales Invoice, Dispense Order, clearance, dispensing, handover, dan kebijakan non-fulfillment. |
| [Domain Apotek — Bahasa Indonesia](./apotek-domain-id.md) | Pendamping semantik yang mudah dibaca manusia untuk domain Apotek canonical. |
| [Domain Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md) | Identitas Pharmacy Queue Entry, Queue Number, Queue Session, `CreatedAt`, `ServedAt`, `DoneAt`, dan lifecycle antrean. |
| [Domain CPOE](../../contexts/cpoe/CPOE-DOMAIN-ID.md) | Electronic Prescription asli dan intent klinisi; CPOE tetap authoritative atas Clinical Order miliknya. |
| [Domain Tata Rekening](../../contexts/TataRekening/02-domain.md) | Financial Charge, Financial Responsibility, koreksi finansial, dan tanggung jawab settlement. |
| Pembahasan bisnis apotek Rawat Jalan yang disetujui | Pengambilan dan mapping antrean, timing payer, tanggung jawab profesional, coordinated pickup, mixed coverage, dan penyelesaian manual obat tidak diambil. |

Authority Inventory, Payment, SEP, Fornas, dan Medication Catalog tetap eksternal meskipun belum ada artifact domain khusus dalam registry.

## 3. Cakupan dan Batasan

### 3.1 Dimulai

Koordinasi Rawat Jalan end-to-end dimulai ketika Pasien memperoleh Nomor Antrean Apotek dengan cara:

- meminta Queue Number secara langsung; atau
- menunjukkan evidence tracker atau registrasi valid yang menyebabkan Queue Entry dibuat.

Prescription Review untuk Electronic Prescription mungkin sudah dimulai atau selesai sebelum trigger end-to-end ini.

### 3.2 Berakhir

Workflow berakhir ketika setiap medication demand yang dimappingkan ke Pharmacy Queue Entry mencapai salah satu outcome accountable berikut:

- Medication Handover selesai untuk jumlah Dispense Order yang dimaksud;
- Accepted Quantity memperoleh Unfulfilled Medication Outcome yang accountable;
- penyelesaian manual obat tidak diambil yang diotorisasi menetapkan `Collection Window Expired` dan seluruh konsekuensi stok serta komersial telah diselesaikan; atau
- resolusi finansial eksternal yang diidentifikasi secara eksplisit masih outstanding dan Pharmacy Sales Order tetap `Active` dengan benar.

### 3.3 Termasuk

- Electronic Prescription yang tersedia sebelum atau setelah Queue Entry dibuat.
- Pencatatan Physical Prescription oleh Staf Apotek.
- Penerimaan Direct Medication Request, rujukan kepada Pharmacist, atau penolakan.
- Tracker Mapping dan Manual Mapping.
- Satu Queue Entry yang dimappingkan ke satu atau beberapa medication demand.
- Prescription Review dan Medication Substitution sebelum Sales Order.
- Purchase Confirmation lisan Pasien Umum sebelum Sales Invoice dibentuk.
- Coverage BPJS dari SEP valid dan mapping Fornas authoritative.
- Jumlah yang ditanggung BPJS dan dibayar Pasien secara campuran.
- Payment Clearance, Coverage Clearance, dan Fulfillment Clearance.
- Stock Reservation, Medication Preparation, pickup call, Final Dispense Review, verifikasi Authorized Recipient, Patient Education, Medication Dispense, dan Medication Handover.
- Backorder atau sumber stok lain yang disetujui untuk produk obat yang sama.
- No-Show dan penyelesaian manual obat tidak diambil.

### 3.4 Tidak termasuk

- Screen, field, tindakan save, dan navigasi operator; semuanya milik SOP.
- Aturan saldo dan mutasi Inventory, termasuk eligibility return.
- Pelaksanaan receipt dan settlement pembayaran.
- Pembuatan SEP dan administrasi eligibility BPJS.
- Tata kelola master data Fornas.
- Perubahan Clinical Order dan lifecycle CPOE.
- Medication Administration.
- Batas numerik pengambilan; keputusan penutupan dilakukan manual sampai kebijakan terpisah menyediakan nilainya.

## 4. Partisipan dan Perpindahan Tanggung Jawab

| Partisipan | Tanggung jawab dalam workflow | Kondisi handoff |
|---|---|---|
| Patient or Caregiver | Mengambil Queue Number, memberikan mapping evidence atau Physical Prescription, memberi konfirmasi lisan ketika Patient-payable, membayar ketika diwajibkan, hadir untuk pickup, menerima edukasi, dan menerima obat ketika authorized. | Evidence diberikan, konfirmasi diberikan atau ditolak, Payment Clearance diperoleh, atau Medication Handover selesai. |
| Patient Tracker | Memiliki identitas Pharmacy Queue Entry, Queue Number, dan lifecycle antrean. | `Queue Entry Created`, `Queue Service Started`, atau `Queue Service Completed`. |
| Staf Apotek | Melakukan panggilan antrian, Manual Mapping, mencatat Resep Kertas, menilai Permintaan Obat Langsung sesuai kewenangan, mengoordinasikan alokasi, menyampaikan nilai Pasien Umum, membentuk faktur terkonfirmasi, menyiapkan atau meracik obat, menangani pesanan tertunda atau sumber stok lain yang disetujui, dan melakukan panggilan pengambilan. | `Outpatient Queue Mapped`, `Pharmacy Sales Order Established`, `Sales Invoice Established`, `Medication Prepared`, `Dispense Order Backordered`, atau `Patient Called for Pickup`. |
| Pharmacist | Melakukan Prescription Review, mengotorisasi Medication Substitution yang eligible sebelum Pharmacy Sales Order dibentuk, menyetujui Direct Medication Request yang dirujuk, memverifikasi Authorized Recipient, melakukan Final Dispense Review, dan memberikan Patient Education. | `Prescription Review Completed`, `Final Dispense Review Completed`, atau Medication Handover diizinkan selesai. |
| Cashier or Payment Authority | Menerima pembayaran Pasien yang diwajibkan dan memberikan Payment Clearance. | `Payment Clearance Established`. |
| SEP and Fornas Authorities | Memberikan validitas SEP tingkat encounter dan coverage BPJS item-level. | `Coverage Clearance Established` untuk jumlah covered. |
| Inventory | Memiliki Stock Availability, Stock Reservation, Inventory Issue, eligibility return, dan Return to Stock. | `Stock Reserved`, Inventory Issue authoritative, atau disposition return yang diterima. |
| Tata Rekening | Memiliki Financial Responsibility dan konsekuensi finansial yang diperlukan ketika obat yang telah dibayar tidak dipenuhi atau diambil. | Credit Note, Refund, atau outcome komersial final lain diberikan. |
| CPOE | Memiliki Electronic Prescription asli yang tidak diubah oleh Apotek. | Prescription asli tersedia. |
| Pharmacy Supervisor | Mengotorisasi expiry exceptional, penyelesaian manual obat tidak diambil, dan keputusan di luar authority biasa. | Outcome exception accountable dibentuk. |

## 5. Kondisi Awal dan Trigger

### 5.1 Trigger end-to-end

`Queue Entry Created` untuk Service Point apotek Rawat Jalan memulai koordinasi antrean. Patient Tracker mencatat `CreatedAt` ketika Queue Number diterbitkan.

### 5.2 Trigger medication demand independen

- Electronic Prescription tersedia dari CPOE atau authority clinical order lain.
- Staf Apotek mencatat Physical Prescription yang ditunjukkan.
- Staf Apotek menerima Direct Medication Request sesuai authority atau setelah persetujuan Pharmacist yang diwajibkan.

Trigger tersebut dapat terjadi sebelum atau setelah Outpatient Queue Mapping berdasarkan `BR-APT-061` dan `BR-APT-062`.

### 5.3 Preconditions

- Setiap Prescription mempertahankan sumber authoritative dan association Pasien.
- Physical Prescription dicatat sebelum Prescription Review.
- Direct Medication Request diterima sebelum dapat membentuk Pharmacy Sales Order.
- Tracker Mapping memerlukan bukti sah yang menemukan satu atau beberapa Resep yang sudah ada; proses ini tidak berlaku untuk Permintaan Obat Langsung.
- Manual Mapping mengharuskan Staf Apotek mengidentifikasi Queue Number dan demand yang berlaku.
- Medication Preparation memerlukan Dispense Order aktif dan Fulfillment Clearance sesuai payer.
- Medication Handover memerlukan Prepared Medication, Authorized Recipient, Final Dispense Review berhasil, dan Patient Education yang berlaku.

### 5.4 Kondisi penghalang

- Queue Number langsung yang belum teridentifikasi tidak dapat melewati clearance yang bergantung pada mapping.
- Prescription dengan Prescription Review belum selesai tidak dapat membentuk Pharmacy Sales Order.
- Prescription rejected atau Direct Medication Request declined tidak dapat membentuk Pharmacy Sales Order.
- Jumlah Pasien Umum tidak dapat memulai Medication Preparation tanpa Payment Clearance.
- Jumlah BPJS-covered tidak dapat memulai Medication Preparation tanpa SEP valid, coverage Fornas authoritative, dan Fulfillment Clearance.
- Identitas obat tidak dapat disubstitusi setelah Pharmacy Sales Order dibentuk.

## 6. Daftar Workflow

| ID | Workflow canonical | Deskripsi outcome Indonesia |
|---|---|---|
| `WF-APT-RJ-001` | Acquire and Map Outpatient Pharmacy Queue | Pharmacy Queue Entry dimappingkan secara accountable ke satu atau beberapa medication demand, atau outcome unresolved/declined dikembalikan kepada queue policy yang berlaku. |
| `WF-APT-RJ-002` | Accept Outpatient Medication Demand | Resep yang diterima membentuk Pharmacy Sales Order dan primary outpatient Dispense Order yang traceable, atau memperoleh outcome rejection. |
| `WF-APT-RJ-003` | Fulfill Medication for a General Patient | Obat yang dikonfirmasi lisan dan dibayar disiapkan serta diserahkan, atau memperoleh alternative atau exception outcome yang accountable. |
| `WF-APT-RJ-004` | Fulfill Medication for a BPJS Patient | Obat covered disiapkan tanpa Sales Invoice sebelumnya dan Sales Invoice BPJS hanya dibentuk bersama Medication Handover yang berhasil. |
| `WF-APT-RJ-005` | Fulfill Mixed-Coverage Medication | Jumlah covered dan Patient-payable memperoleh commercial allocation dan clearance terpisah dengan tetap dikoordinasikan untuk satu pickup. |
| `WF-APT-RJ-006` | Coordinate Multiple Medication Demands in One Queue | Beberapa lifecycle demand, Sales Order, invoice, dan Dispense Order independen dikoordinasikan menjadi satu pelayanan antrean dan pickup tanpa digabungkan. |
| `WF-APT-RJ-007` | Resolve Uncollected Outpatient Medication | Obat siap yang tidak diambil memperoleh expiry terotorisasi, disposition Inventory, dan resolusi komersial sesuai payer. |

## 7. Spesifikasi Workflow

### WF-APT-RJ-001 — Acquire and Map Outpatient Pharmacy Queue

**Indonesia:** Mengambil dan Memapping Antrean Apotek Rawat Jalan

#### Purpose

melakukan mapping satu Pharmacy Queue Entry dengan seluruh sumber pelayanan obat rawat jalan yang sesuai tanpa menjadikan kedatangan antrean sebagai prasyarat Prescription Review.

#### Trigger

`Queue Entry Created` untuk Service Point apotek Rawat Jalan.

#### Preconditions

- Queue Session tersedia bagi Service Point apotek Rawat Jalan.
- Pasien meminta Queue Number langsung atau menunjukkan evidence tracker atau registrasi.

#### Participants

Patient or Caregiver, Patient Tracker, Staf Apotek.

#### Input Business Facts

- Pharmacy Queue Entry, Queue Number, dan `CreatedAt` dari Patient Tracker.
- Evidence tracker atau registrasi bila ditunjukkan.
- Electronic Prescription atau Pharmacy Sales Order existing bila telah tersedia.
- Physical Prescription atau Direct Medication Request bila ditunjukkan di loket.

#### Main Flow

1. Patient Tracker membentuk Pharmacy Queue Entry, menetapkan Queue Number, dan mencatat `CreatedAt`.
2. Ketika bukti tracker atau registrasi yang sah menemukan satu atau beberapa Resep yang sudah ada dan berlaku, Aplikasi Pelayanan Obat membuat Tracker Mapping.
3. Aplikasi Pelayanan Obat membuat catatan mapping tersendiri antara entri antrian dan setiap Resep yang ditemukan.
4. Untuk setiap Resep yang telah di-mapping, Aplikasi Pelayanan Obat menampilkan perkembangannya tanpa memindahkan kepemilikan status kepada Sistem Antrian Pasien.
5. Koordinasi antrean menunggu fulfillment sesuai payer sementara Prescription Review dan pembentukan Pharmacy Sales Order dapat berlanjut secara independen.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Bukti tracker atau registrasi menemukan satu atau beberapa Resep yang sudah ada | Aplikasi Pelayanan Obat | Hubungkan setiap Resep secara otomatis; panggilan administrasi awal tidak diperlukan. |
| Bukti gagal menemukan Resep | Staf Apotek | Beralih ke Manual Mapping. |
| Queue Number diperoleh langsung | Staf Apotek | Panggil Queue Number untuk identifikasi administratif dan Manual Mapping. |
| Satu Queue Entry mempunyai beberapa applicable demand | Staf Apotek | Mapping setiap demand secara terpisah ke Queue Entry yang sama berdasarkan `BR-APT-084` dan `BR-APT-085`. |

Untuk Manual Mapping:

1. Staf Apotek memanggil unresolved Queue Number tanpa memulai pelayanan Patient Tracker.
2. Staf Apotek mengidentifikasi Prescription atau Pharmacy Sales Order existing, mencatat Physical Prescription yang ditunjukkan, atau menilai Direct Medication Request.
3. Apotek membentuk Manual Mapping untuk setiap applicable demand yang teridentifikasi.

#### Exception and Compensation Flows

- Jika Direct Medication Request ditolak, record Direct Medication Request dan Pharmacy Sales Order tidak dibentuk. Disposition final Queue Entry yang masih `Waiting` mengikuti withdrawal policy Patient Tracker yang berlaku dan tetap eksternal terhadap Apotek.
- Jika Queue Number tidak dapat dicocokkan dengan Patient Journey atau medication demand yang accountable, Queue Entry tetap unmapped dan tidak dapat memperoleh Fulfillment Clearance yang bergantung pada mapping.
- Jika mapping antrean salah, Staf Apotek memilih Resep atau sumber pelayanan obat yang benar dan Aplikasi Pelayanan Obat memperbarui mapping aktif. Riwayat perubahan mapping antrean tidak perlu disimpan. Pembaruan ini tidak mengubah Resep, hasil telaah resep, atau pesanan penjualan apotek.

#### Outcomes and Postconditions

- Berhasil: `Outpatient Queue Mapped` tersedia untuk satu atau beberapa demand.
- Non-completion accountable: Queue Entry tetap unmapped menunggu evidence, atau Direct Medication Request yang ditolak tidak menghasilkan medication demand.
- Mapping tidak menetapkan `ServedAt`, membentuk Electronic Prescription, atau menyelesaikan Prescription Review.

#### Domain References

`BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087`; `BR-TRK-026`–`BR-TRK-035`.

#### Domain Events

- Dikonsumsi: `Queue Entry Created`.
- Dihasilkan atau diamati: `Outpatient Queue Mapped`, `Queue Entry Identified` ketika berlaku secara eksternal.

### WF-APT-RJ-002 — Accept Outpatient Medication Demand

**Indonesia:** Menerima Permintaan Obat Rawat Jalan

#### Purpose

Mengubah Prescription yang telah ditelaah atau Direct Medication Request yang diterima menjadi Pharmacy Sales Order dan primary outpatient Dispense Order yang traceable.

#### Trigger

Prescription tersedia atau Direct Medication Request ditunjukkan untuk dinilai.

#### Preconditions

- Prescription dimiliki CPOE atau authority clinical order accountable lain, atau Staf Apotek berwenang menilai Direct Medication Request.
- Physical Prescription telah dicatat sebelum review.

#### Participants

Pharmacist, Staf Apotek, CPOE or Prescribing Clinician.

#### Input Business Facts

- Electronic Prescription atau Physical Prescription yang dicatat.
- Detail Direct Medication Request bila berlaku.
- Medication Catalog dan professional acceptance policy.
- Stock Availability sebagai fakta fulfillment eksternal yang tidak menentukan clinical acceptance.

#### Main Flow

1. Untuk Prescription, Pharmacist memulai Prescription Review segera setelah Prescription tersedia tanpa menunggu kedatangan Pasien atau Outpatient Queue Mapping.
2. Pharmacist menelaah setiap Prescription Line. Jika diperlukan, Pharmacist melakukan klarifikasi kepada Prescribing Clinician di luar sistem; Prescription tetap utuh dan review tetap `Under Review`.
3. Pharmacist menetapkan setiap line sebagai diterima sesuai resep, diterima dengan obat pengganti, atau ditolak. Obat yang diterima dicatat pada Sales Order Line; obat pengganti disertai alasan, jumlah terdampak, Pharmacist penanggung jawab, dan referensi ke Prescription Line asli.
4. Pharmacist menyelesaikan Prescription Review sebagai `Approved`, `Partially Approved`, atau `Rejected`.
5. Untuk Direct Medication Request yang diterima, Staf Apotek menerima sesuai authority atau memperoleh persetujuan Pharmacist yang diwajibkan; Prescription tidak dibentuk.
6. Apotek membentuk Pharmacy Sales Order dari tepat satu completed accepted-demand source dan mempertahankan Source Traceability.
7. Apotek membentuk Billing Allocation dan Fulfillment Allocation yang berlaku secara independen.
8. Untuk episode Rawat Jalan normal, Apotek membentuk satu active primary Dispense Order bagi Pharmacy Sales Order aktif.
9. Inventory dapat membentuk Stock Reservation sebelum Pasien datang atau queue mapping, sedangkan Medication Preparation menunggu Fulfillment Clearance yang berlaku.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Semua Prescription Line diterima | Pharmacist | `Approved`; seluruh accepted line dapat membentuk Pharmacy Sales Order. |
| Sebagian line diterima | Pharmacist | `Partially Approved`; hanya Accepted Medication Line masuk Pharmacy Sales Order. |
| Tidak ada line diterima | Pharmacist | `Rejected`; Pharmacy Sales Order tidak dibentuk. |
| Direct request dalam authority staf | Staf Apotek | Terima dan bentuk sumber Direct Medication Request. |
| Direct request memerlukan persetujuan profesional | Staf Apotek dan Pharmacist | Rujuk, lalu terima hanya setelah persetujuan. |
| Direct request ditolak | Staf Apotek atau Pharmacist | Jangan membentuk request record atau Pharmacy Sales Order. |

#### Exception and Compensation Flows

- Stock shortage tidak mengubah Prescription Review Outcome. Staf Apotek dapat memilih Backorder atau sumber stok lain yang disetujui untuk produk obat yang sama setelah Pharmacy Sales Order dibentuk.
- Identitas obat pada Sales Order Line yang sudah dibentuk tidak boleh diubah. Jika penggantian diperlukan kemudian, batalkan item atau pesanan yang terdampak, telaah kembali Prescription asli, lalu bentuk Sales Order Line baru tanpa mensyaratkan Prescription perbaikan atau pengganti.
- Accepted Quantity yang tidak dapat dipenuhi harus mempertahankan Backorder, `Cancelled`, `Expired`, atau Unfulfilled Medication Outcome lain yang accountable.

#### Outcomes and Postconditions

- Berhasil: `Prescription Review Completed`, `Pharmacy Sales Order Established`, `Fulfillment Allocation Established`, dan `Dispense Order Established` diamati bila berlaku.
- Berhasil sebagian: hanya Accepted Medication Line masuk Pharmacy Sales Order.
- Rejection: Pharmacy Sales Order tidak tersedia bagi source yang rejected.
- Pharmacy Sales Order bukan Sales Invoice, Dispense Order, reservation, atau dispense evidence.

#### Domain References

`BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, `BR-APT-089`; lifecycle Prescription Review dan Pharmacy Sales Order.

#### Domain Events

- Dikonsumsi: `Clinical Order Created` atau fakta Prescription availability authoritative lain.
- Dihasilkan: `Prescription Review Started`, `Medication Substitution Authorized`, `Prescription Review Completed`, `Direct Medication Request Accepted`, `Pharmacy Sales Order Established`, `Billing Allocation Established`, `Fulfillment Allocation Established`, `Dispense Order Established`, `Stock Reserved` ketika diberikan secara eksternal.

### WF-APT-RJ-003 — Fulfill Medication for a General Patient

**Indonesia:** Memenuhi Obat untuk Pasien Umum

#### Purpose

Memperoleh Purchase Confirmation lisan sebelum Sales Invoice dibentuk, memperoleh pembayaran Pasien, dan menyelesaikan Medication Handover Rawat Jalan yang accountable.

#### Trigger

Outpatient Queue Mapping, Pharmacy Sales Order aktif, Billing Allocation yang berlaku, dan nilai Patient-payable yang dihitung telah tersedia.

#### Preconditions

- Nilai Pasien Umum dihitung dari Billing Allocation accountable dan Pricing Snapshot yang berlaku.
- Sales Invoice belum dibentuk untuk penjualan Patient-payable yang diusulkan.
- Dispense Order yang berlaku tersedia atau dapat dibentuk dari Fulfillment Allocation.

#### Participants

Patient or Caregiver, Staf Apotek, Cashier or Payment Authority, Staf Apotek, Pharmacist, Patient Tracker, Inventory, Tata Rekening.

#### Input Business Facts

- Outpatient Queue Mapping.
- Pharmacy Sales Order dan Billing Allocation Patient-payable.
- Pricing Snapshot dan nilai yang dihitung.
- Dispense Order dan Stock Reservation bila telah tersedia.

#### Main Flow

1. Staf Apotek menerima Pasien dalam interaksi Purchase Confirmation—dalam interaksi loket Manual Mapping bila memungkinkan, atau melalui administrative Queue Number call terpisah setelah Tracker Mapping—dan menyampaikan total yang dihitung secara lisan sebelum Sales Invoice tersedia. Interaksi ini tidak menetapkan `ServedAt` atau `DoneAt`.
2. Pasien memberikan Purchase Confirmation lisan.
3. Staf Apotek membentuk Sales Invoice dari Billing Allocation yang dikonfirmasi; pembentukan Sales Invoice menjadi evidence accountable bahwa konfirmasi diperoleh dan tidak ada object atau transaksi konfirmasi terpisah.
4. Cashier menerima pembayaran dan memberikan Payment Clearance bagi Sales Invoice.
5. Apotek membentuk Fulfillment Clearance untuk jumlah Dispense Order yang berlaku.
6. Inventory mengamankan Stock Reservation yang diperlukan bila belum direservasi.
7. Staf Apotek memulai Medication Preparation berdasarkan released Dispense Order.
8. Apotek mengamati `Medication Preparation Started`; Patient Tracker memasukkan Pharmacy Queue Entry ke In Service dan mencatat `ServedAt`.
9. Staf Apotek menyelesaikan Medication Preparation; Dispense Order mencapai `Prepared` dan obat tetap In-Transit Medication.
10. Ketika setiap Dispense Order yang dimaksud dalam coordinated handover berstatus `Prepared` atau memiliki exception outcome accountable, Staf Apotek melakukan pickup call.
11. Patient Tracker membuat Pharmacy Queue Entry `Done` dan mencatat `DoneAt` pada waktu pickup call.
12. Dengan Pasien atau caregiver hadir, Pharmacist memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review, dan memberikan Patient Education yang berlaku dalam interaksi loket yang sama.
13. Apotek mencatat Medication Dispense dan menyelesaikan Medication Handover untuk setiap jumlah Dispense Order yang berlaku.
14. Medication Handover menyelesaikan jumlah Dispense Order dan meminta outcome Inventory Issue authoritative dari Inventory.
15. Pharmacy Sales Order hanya menjadi `Resolved` ketika seluruh Accepted Quantity dan konsekuensi komersial memiliki outcome final accountable.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Pasien menolak sebelum Sales Invoice dibentuk | Patient | Sales Invoice tidak dibentuk; Stock Reservation yang tidak digunakan dilepas; alokasi Patient-payable memperoleh outcome declined atau commercially unallocated yang accountable. |
| Nilai yang dihitung berubah sebelum pembentukan | Staf Apotek | Sampaikan nilai revisi dan dapatkan kembali konfirmasi lisan sebelum membentuk Sales Invoice. |
| Beberapa demand memakai satu Queue Entry | Staf Apotek | Terapkan `WF-APT-RJ-006`; pertahankan Sales Order, invoice, dan Dispense Order terpisah. |
| Pasien tidak mengambil setelah pickup call | Pharmacy Supervisor | Terapkan `WF-APT-RJ-007`. |

#### Exception and Compensation Flows

- Jika pembayaran tidak selesai setelah Sales Invoice dibentuk, Medication Preparation tetap terblokir. Sales Invoice hanya dapat `Cancelled` selama lifecycle mengizinkan.
- Jika Sales Invoice issued atau financially cleared memerlukan koreksi, gunakan Financial Adjustment, Credit Note, atau Refund berdasarkan authority Tata Rekening; jangan menggantinya diam-diam.
- Jika shortage terjadi setelah pembayaran, Staf Apotek dapat memilih Backorder atau sumber stok lain yang disetujui untuk produk obat yang sama. Substitution dilarang karena Pharmacy Sales Order telah tersedia.
- Jika fulfillment tidak dapat selesai, jumlah terdampak memperoleh Unfulfilled Medication Outcome yang accountable dan Tata Rekening menerima konsekuensi finansial yang diperlukan.
- Final Dispense Review yang gagal mencegah Medication Handover dan mengembalikan Dispense Order terdampak ke penyelesaian exception accountable tanpa mengubah Prescription asli.

#### Outcomes and Postconditions

- Selesai berhasil: Sales Invoice financially cleared, Dispense Order `Completed`, Medication Handover mengidentifikasi Authorized Recipient, dan seluruh jumlah tetap traceable.
- Pembelian ditolak: Sales Invoice tidak tersedia bagi usulan yang ditolak.
- Paid non-fulfillment atau No-Show: konsekuensi fulfillment dan komersial tetap accountable secara terpisah; Pharmacy Sales Order tetap `Active` sampai keduanya final.
- Queue `Done` tidak membuktikan Medication Handover.

#### Domain References

`BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`; lifecycle Sales Invoice dan Dispense Order; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046`.

#### Domain Events

- Dikonsumsi: `Outpatient Queue Mapped`, `Billing Allocation Established`, `Payment Clearance Established`, `Stock Reserved`.
- Dihasilkan atau diamati: `Sales Invoice Established`, `Sales Invoice Issued`, `Fulfillment Clearance Established`, `Medication Preparation Started`, `Pharmacy Service Started`, `Medication Prepared`, `Patient Called for Pickup`, `Queue Service Started`, `Queue Service Completed`, `Final Dispense Review Completed`, `Medication Dispensed`, `Medication Handed Over`, `Pharmacy Sales Order Resolved`.

### WF-APT-RJ-004 — Fulfill Medication for a BPJS Patient

**Indonesia:** Memenuhi Obat untuk Pasien BPJS

#### Purpose

Menyiapkan obat covered Rawat Jalan tanpa Sales Invoice sebelumnya atau pembayaran Pasien dan membentuk Sales Invoice BPJS hanya bersama Medication Handover yang berhasil.

#### Trigger

Outpatient Queue Mapping, Pharmacy Sales Order aktif, Dispense Order, SEP valid, dan coverage Fornas item-level authoritative telah tersedia.

#### Preconditions

- Pasien memiliki SEP valid untuk encounter terkait.
- Setiap covered quantity didukung mapping Fornas authoritative.
- Purchase Confirmation atau pembayaran Pasien tidak diperlukan untuk covered quantity.
- Sales Invoice BPJS belum dibentuk.

#### Participants

Patient or Caregiver, Staf Apotek, Pharmacist, Patient Tracker, SEP and Fornas Authorities, Inventory, Tata Rekening.

#### Input Business Facts

- Outpatient Queue Mapping.
- Pharmacy Sales Order, covered Billing Allocation, dan Dispense Order.
- SEP valid dan coverage Fornas item-level.
- Outcome Stock Availability dan Stock Reservation.

#### Main Flow

1. Authority SEP dan Fornas membentuk Coverage Clearance bagi setiap covered quantity.
2. Apotek membentuk Fulfillment Clearance bagi jumlah Dispense Order terkait tanpa mewajibkan Sales Invoice existing.
3. Inventory mengamankan Stock Reservation bila belum direservasi.
4. Staf Apotek memulai Medication Preparation.
5. `Medication Preparation Started` menyebabkan Patient Tracker mencatat `ServedAt` dan memindahkan Pharmacy Queue Entry ke In Service.
6. Staf Apotek menyelesaikan Medication Preparation; Dispense Order mencapai `Prepared` dan obat tetap In-Transit Medication.
7. Ketika setiap Dispense Order yang dimaksud dalam coordinated handover berstatus `Prepared` atau memiliki exception outcome accountable, Staf Apotek melakukan pickup call.
8. Patient Tracker mencatat `DoneAt` dan membuat Queue Entry `Done` pada waktu pickup call.
9. Dengan Pasien atau caregiver hadir, Pharmacist memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review, dan memberikan Patient Education yang berlaku dalam interaksi loket yang sama.
10. Sebagai satu outcome bisnis accountable, Apotek membentuk Sales Invoice BPJS dari covered Billing Allocation, mencatat Medication Dispense, dan menyelesaikan Medication Handover.
11. Medication Handover menyelesaikan setiap jumlah Dispense Order yang berlaku dan meminta outcome Inventory Issue authoritative.
12. Pharmacy Sales Order hanya menjadi `Resolved` ketika setiap Accepted Quantity dan konsekuensi komersial yang diperlukan memiliki outcome final.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Electronic Prescription dan Tracker Mapping berhasil pada jalur normal | Apotek | Satu pemanggilan Rawat Jalan terjadi: pickup call setelah `Prepared`. |
| SEP tidak valid | SEP authority | Coverage Clearance tidak tersedia; Medication Preparation tetap terblokir. |
| Item tidak covered Fornas | Staf Apotek | Arahkan non-covered quantity melalui `WF-APT-RJ-005`. |
| Beberapa demand dimappingkan | Staf Apotek | Terapkan `WF-APT-RJ-006`; pertahankan record terpisah dan satu coordinated pickup. |
| Pasien tidak mengambil | Pharmacy Supervisor | Terapkan `WF-APT-RJ-007`; Sales Invoice BPJS tidak dibentuk. |

#### Exception and Compensation Flows

- No-Show BPJS sebelum Medication Handover tidak membentuk Sales Invoice dan tidak memerlukan pembatalan Sales Invoice.
- Shortage setelah Pharmacy Sales Order dibentuk mengizinkan Backorder atau sumber stok lain yang disetujui untuk produk obat yang sama; substitution tidak diizinkan.
- Final Dispense Review yang gagal mencegah pembentukan Sales Invoice BPJS dan Medication Handover.
- Inventory menentukan apakah reserved atau In-Transit Medication eligible untuk return.

#### Outcomes and Postconditions

- Selesai berhasil: pembentukan Sales Invoice BPJS dan Medication Handover menjadi satu outcome accountable; Patient-payable amount nol dan payment disposition `Not Required`.
- Coverage blocked: preparation tidak terjadi bagi uncleared quantity.
- No-Show: Sales Invoice BPJS tidak tersedia; Dispense Order dan stok mengikuti `WF-APT-RJ-007`.
- Queue `Done` tetap independen dari selesainya Medication Handover.

#### Domain References

`BR-APT-020`–`BR-APT-026`, `BR-APT-029`–`BR-APT-045`, `BR-APT-066`, `BR-APT-068`–`BR-APT-069`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095`; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046`.

#### Domain Events

- Dikonsumsi: `Outpatient Queue Mapped`, `Coverage Clearance Established`, `Fulfillment Allocation Established`, `Stock Reserved`.
- Dihasilkan atau diamati: `Fulfillment Clearance Established`, `Medication Preparation Started`, `Pharmacy Service Started`, `Medication Prepared`, `Patient Called for Pickup`, `Queue Service Started`, `Queue Service Completed`, `Final Dispense Review Completed`, `Sales Invoice Established`, `Sales Invoice Issued`, `Medication Dispensed`, `Medication Handed Over`, `Pharmacy Sales Order Resolved`.

### WF-APT-RJ-005 — Fulfill Mixed-Coverage Medication

**Indonesia:** Memenuhi Obat dengan Coverage Campuran

#### Purpose

Memisahkan tanggung jawab komersial BPJS-covered dan Patient-payable sambil mengoordinasikan seluruh jumlah yang dimaksud untuk satu pickup Rawat Jalan.

#### Trigger

Satu Pharmacy Sales Order memiliki jumlah yang diklasifikasikan sebagian BPJS-covered dan sebagian Patient-payable.

#### Preconditions

- SEP valid tersedia untuk encounter.
- Mapping Fornas authoritative mengidentifikasi covered dan non-covered quantity.
- Staf Apotek dapat membentuk Billing Allocation terpisah tanpa mengubah identitas atau jumlah obat yang diterima.

#### Participants

Patient or Caregiver, Staf Apotek, Cashier or Payment Authority, Staf Apotek, Pharmacist, Patient Tracker, SEP and Fornas Authorities, Inventory, Tata Rekening.

#### Input Business Facts

- Satu Pharmacy Sales Order dan Sales Order Line miliknya.
- SEP valid dan coverage Fornas item-level authoritative.
- Billing Allocation covered dan Patient-payable.
- Dispense Order dan Fulfillment Allocation yang berlaku.

#### Main Flow

1. Staf Apotek memisahkan Billing Allocation menjadi jumlah BPJS-covered dan Patient-payable.
2. Validitas SEP dan mapping Fornas membentuk Coverage Clearance bagi covered quantity.
3. Sebelum Sales Invoice Pasien Umum tersedia, Staf Apotek melakukan interaksi Purchase Confirmation yang didefinisikan `WF-APT-RJ-003` dan menyampaikan nilai Patient-payable yang dihitung secara lisan.
4. Pasien memberikan Purchase Confirmation lisan bagi non-covered quantity.
5. Staf Apotek membentuk Sales Invoice Pasien Umum terpisah dari Billing Allocation Patient-payable yang dikonfirmasi.
6. Cashier memberikan Payment Clearance bagi Sales Invoice Pasien Umum.
7. Apotek membentuk Fulfillment Clearance bagi covered quantity dari Coverage Clearance dan bagi Patient-payable quantity dari Payment Clearance.
8. Setelah setiap jumlah yang dimaksud untuk handover memperoleh clearance yang berlaku, Staf Apotek memulai dan menyelesaikan Medication Preparation.
9. `Medication Preparation Started` pertama mencatat `ServedAt` Patient Tracker; setiap intended Dispense Order mencapai `Prepared` sebelum pickup.
10. Staf Apotek melakukan satu coordinated pickup call; Patient Tracker mencatat `DoneAt`.
11. Dengan Pasien atau caregiver hadir, Pharmacist memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review, dan memberikan Patient Education.
12. Apotek membentuk Sales Invoice BPJS dari covered Billing Allocation hanya ketika Medication Handover berhasil; Sales Invoice Pasien Umum sudah tersedia dan financially cleared.
13. Apotek mencatat Medication Dispense dan Medication Handover bagi seluruh jumlah yang berlaku serta meminta outcome Inventory Issue.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Pasien mengonfirmasi bagian non-covered | Patient | Bentuk dan terima pembayaran Sales Invoice Pasien Umum; koordinasikan kedua bagian payer. |
| Pasien menolak bagian non-covered sebelum invoice dibentuk | Patient | Jangan membentuk Sales Invoice Pasien Umum; berikan outcome declined atau commercially unallocated yang accountable kepada allocation; lanjutkan bagian BPJS secara independen. |
| Covered item tidak mempunyai coverage Fornas | Staf Apotek | Reklasifikasi menjadi Patient-payable hanya melalui Billing Allocation accountable; sampaikan nilai revisi dan minta konfirmasi lisan. |
| Tidak semua intended quantity memperoleh clearance | Apotek | Jangan memulai coordinated preparation bagi quantity tersebut dan jangan melakukan pickup call. |

#### Exception and Compensation Flows

- Sales Invoice Pasien Umum yang telah dibentuk mengikuti aturan cancellation dan correction Pasien Umum; Sales Invoice BPJS tetap belum ada sampai handover.
- No-Show setelah pembayaran mengikuti jalur komersial paid General Patient sedangkan Sales Invoice BPJS yang belum ada mengikuti jalur BPJS uninvoiced.
- Partial non-fulfillment mempertahankan Billing Allocation sesuai payer dan memerlukan konsekuensi komersial terpisah.
- Substitution dilarang setelah Pharmacy Sales Order dibentuk.

#### Outcomes and Postconditions

- Berhasil: Sales Invoice terpisah merepresentasikan Medication Sale covered dan Patient-payable, serta satu coordinated Medication Handover mempertahankan traceability tingkat allocation.
- Pasien menolak non-covered quantity: covered quantity dapat selesai independen tanpa Sales Invoice Pasien Umum bagi bagian yang ditolak.
- Konsekuensi komersial unresolved membuat Pharmacy Sales Order tetap `Active`.

#### Domain References

`BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, `BR-APT-090`–`BR-APT-095`.

#### Domain Events

- Dikonsumsi: `Billing Allocation Established`, `Coverage Clearance Established`, `Payment Clearance Established`, `Outpatient Queue Mapped`.
- Dihasilkan atau diamati: `Sales Invoice Established`, `Sales Invoice Issued`, `Fulfillment Clearance Established`, `Medication Preparation Started`, `Medication Prepared`, `Patient Called for Pickup`, `Final Dispense Review Completed`, `Medication Dispensed`, `Medication Handed Over`, `Pharmacy Sales Order Resolved` ketika seluruhnya direkonsiliasi.

### WF-APT-RJ-006 — Coordinate Multiple Medication Demands in One Queue

**Indonesia:** Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrean

#### Purpose

Mengoordinasikan dua atau lebih medication demand yang accountable secara independen dalam satu antrean dan pickup session Rawat Jalan tanpa menggabungkan business record masing-masing.

#### Trigger

Satu Pharmacy Queue Entry dimappingkan ke dua atau lebih Prescription, Direct Medication Request, atau Pharmacy Sales Order yang dihasilkannya.

#### Preconditions

- Setiap demand memiliki Outpatient Queue Mapping terpisah menuju Queue Entry yang sama.
- Setiap Prescription mengikuti Prescription Review sendiri.
- Setiap accepted source membentuk Pharmacy Sales Order dan active primary outpatient Dispense Order sendiri.

#### Participants

Staf Apotek, Pharmacist, Patient or Caregiver, Patient Tracker, Cashier or Payment Authority, SEP and Fornas Authorities, Inventory.

#### Input Business Facts

- Satu Pharmacy Queue Entry.
- Dua atau lebih medication demand yang dimappingkan.
- Progress per-demand untuk Pharmacy Sales Order, Billing Allocation, Fulfillment Allocation, Sales Invoice, clearance, dan Dispense Order.

#### Main Flow

1. Apotek mempertahankan Outpatient Queue Mapping terpisah bagi setiap sumber pelayanan obat yang di-mapping dengan Pharmacy Queue Entry yang sama.
2. Setiap demand bergerak independen melalui Prescription Review atau direct acceptance, pembentukan Pharmacy Sales Order, commercial allocation, fulfillment allocation, dan payer clearance.
3. Queue-facing view memproyeksikan progress authoritative setiap mapped demand tanpa memiliki state tersebut.
4. `Medication Preparation Started` pertama yang berlaku menyebabkan Patient Tracker mencatat satu `ServedAt` bagi Queue Entry yang sama.
5. Staf Apotek menunggu sampai setiap Dispense Order yang dimaksud untuk pickup berstatus `Prepared` atau memiliki exception outcome accountable.
6. Staf Apotek melakukan satu coordinated pickup call; Patient Tracker mencatat satu `DoneAt` bagi Queue Entry yang sama.
7. Dengan Pasien atau caregiver hadir, Pharmacist memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review bagi setiap Prepared Medication, dan memberikan Patient Education gabungan dengan tetap mempertahankan instruksi khusus obat.
8. Apotek mencatat Medication Dispense dan Medication Handover terhadap setiap Dispense Order dan Pharmacy Sales Order yang berlaku secara terpisah.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Setiap included demand siap | Staf Apotek | Lakukan satu coordinated pickup call dan handover session. |
| Satu demand memiliki exception outcome accountable | Staf Apotek | Sertakan resolved exception dalam komunikasi gabungan dan lanjutkan ready demand ketika payer rule mengizinkan. |
| Satu demand masih unresolved | Staf Apotek | Jangan nyatakan demand tersebut ready; tunda coordinated call kecuali Pasien menerima partial path accountable yang diizinkan payer workflow. |
| Demand memiliki klasifikasi payer berbeda | Staf Apotek | Terapkan workflow General, BPJS, atau mixed coverage yang berlaku kepada setiap demand sebelum koordinasi. |

#### Exception and Compensation Flows

- Koreksi satu mapping tidak boleh menulis ulang riwayat demand lain.
- Cancellation, expiry, Backorder, financial correction, dan return tetap melekat pada Pharmacy Sales Order dan Dispense Order sumbernya.
- Medication Handover yang berhasil bagi satu demand tidak boleh disimpulkan memenuhi mapped demand lain tanpa handover fact masing-masing.

#### Outcomes and Postconditions

- Satu Queue Entry memiliki satu `CreatedAt`, maksimal satu `ServedAt`, dan satu `DoneAt`.
- Setiap Prescription, Pharmacy Sales Order, Sales Invoice, dan Dispense Order mempertahankan identitas dan lifecycle independen.
- Satu pickup call dan interaksi loket dapat mengoordinasikan beberapa fakta Medication Handover accountable.

#### Domain References

`BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, `BR-APT-095`; `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, `BR-TRK-045a`.

#### Domain Events

- Dikonsumsi: `Outpatient Queue Mapped`, `Medication Preparation Started`, `Medication Prepared`.
- Dihasilkan atau diamati: `Queue Service Started`, `Patient Called for Pickup`, `Queue Service Completed`, `Final Dispense Review Completed`, `Medication Dispensed`, `Medication Handed Over` bagi setiap demand yang berlaku.

### WF-APT-RJ-007 — Resolve Uncollected Outpatient Medication

**Indonesia:** Menyelesaikan Obat Rawat Jalan yang Tidak Diambil

#### Purpose

Memberikan expiry terotorisasi, disposition stok, dan outcome komersial sesuai payer kepada Prepared Medication yang tidak diambil tanpa membuat batas waktu otomatis.

#### Trigger

Pharmacy Supervisor atau peran authorized lain secara manual menetapkan bahwa kesempatan pengambilan telah berakhir bagi Prepared Medication yang belum diserahkan.

#### Preconditions

- Obat tetap Prepared atau In-Transit dan Medication Handover belum selesai.
- Pasien tidak mengambil obat.
- Authority penutupan manual dan effective business time diketahui.

#### Participants

Pharmacy Supervisor, Staf Apotek, Inventory, Tata Rekening, Patient Tracker.

#### Input Business Facts

- Pharmacy Queue Entry yang mungkin sudah `Done` setelah pickup call.
- Pharmacy Sales Order, Dispense Order, dan unresolved quantity.
- Keberadaan Sales Invoice dan financial disposition sesuai payer.
- Prepared atau In-Transit Medication dan eligibility disposition Inventory.

#### Main Flow

1. Peran authorized melakukan penyelesaian manual obat tidak diambil dan mencatat Pasien sebagai No-Show untuk fulfillment terkait.
2. Apotek memberikan terminal state `Expired` kepada setiap Dispense Order terdampak.
3. Resolution mempertahankan reason `Collection Window Expired`, responsible party, effective business time, affected quantity, dan Source Traceability.
4. Inventory menentukan disposition authoritative bagi reserved atau In-Transit Medication dan hanya menerima Return to Stock ketika eligible.
5. Apotek mencatat Unfulfilled Medication Outcome yang dihasilkan bagi setiap affected quantity.
6. Apotek menyelesaikan konsekuensi komersial sesuai payer.
7. Pharmacy Sales Order hanya menjadi `Resolved` setelah setiap Accepted Quantity dan konsekuensi komersial yang diperlukan memiliki outcome final accountable.

#### Decision and Alternative Flows

| Kondisi payer | Outcome komersial |
|---|---|
| Sales Invoice BPJS belum dibentuk karena handover gagal | Jangan membentuk atau membatalkan invoice; selesaikan fulfillment dan Inventory saja, lalu resolve Pharmacy Sales Order ketika seluruh outcome final. |
| Sales Invoice Pasien Umum telah dibayar | Tata Rekening atau financial authority yang bertanggung jawab memberikan Credit Note, Refund, atau outcome final lain; Pharmacy Sales Order tetap `Active` sampai saat itu. |
| Usulan Pasien Umum ditolak sebelum invoice dibentuk | Sales Invoice tidak tersedia; selesaikan unused reservation dan commercially unallocated quantity. |
| Mixed coverage | Selesaikan konsekuensi covered uninvoiced dan Patient-payable paid secara terpisah menggunakan Billing Allocation masing-masing. |

#### Exception and Compensation Flows

- Batas pengambilan numerik tidak dibuat. Sampai kebijakan authoritative menyediakannya, hanya aktivitas manual authorized yang menetapkan akhir kesempatan pengambilan.
- Queue `DoneAt` tidak dibalik; penyelesaian No-Show dimiliki Apotek setelah antrean selesai.
- Inventory dapat menolak Return to Stock berdasarkan kebijakannya; return yang ditolak tetap memerlukan disposition Inventory final accountable.
- Konsekuensi paid tidak boleh dihapus diam-diam atau diperlakukan sebagai jalur BPJS uninvoiced.

#### Outcomes and Postconditions

- No-Show BPJS: Dispense Order `Expired`, Sales Invoice BPJS tidak tersedia, dan stok mempunyai disposition accountable.
- No-Show Pasien Umum paid: Dispense Order `Expired`; Pharmacy Sales Order tetap `Active` sampai outcome finansial final.
- Final resolution: Pharmacy Sales Order `Resolved` dengan reason `Collection Window Expired` setelah seluruh konsekuensi fulfillment dan komersial final.

#### Domain References

`BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-069`, `BR-APT-078`–`BR-APT-080`, `BR-APT-095`; lifecycle Dispense Order, quantity, pickup, dan Pharmacy Sales Order.

#### Domain Events

- Dikonsumsi: `Patient Called for Pickup`, `Medication Prepared`.
- Dihasilkan atau diamati: `Outpatient No-Show Recorded`, `Dispense Order Expired`, `Unfulfilled Medication Recorded`, `Medication Returned`, `Sales Invoice Credited`, `Refund Required`, `Pharmacy Sales Order Resolved` ketika seluruhnya direkonsiliasi.

## 8. Handoff Lintas Context

| Dari | Fakta bisnis authoritative | Kepada | Tanggung jawab hasil handoff |
|---|---|---|---|
| Patient Tracker | `Queue Entry Created`, Queue Number, `CreatedAt` | Apotek | Membentuk Tracker Mapping atau Manual Mapping tanpa mengambil ownership identitas antrean. |
| Apotek | `Medication Preparation Started` | Patient Tracker | Memindahkan Pharmacy Queue Entry ke In Service dan mencatat `ServedAt`; Patient Tracker tidak boleh menyimpulkan Medication Handover. |
| Apotek | `Patient Called for Pickup` | Patient Tracker | Membuat Pharmacy Queue Entry `Done` dan mencatat `DoneAt`; telaah profesional dan handover berikutnya tetap menjadi fakta Apotek. |
| CPOE atau clinical-order authority | Prescription availability dan clinician intent | Apotek | Melakukan Prescription Review tanpa mengubah Prescription asli. |
| Apotek | Fulfillment projection per Prescription dan realisasi Medication Handover | Pelaporan EMR | Menampilkan informasi Prescription-to-realization tanpa mengubah Clinical Order CPOE pada scope awal. |
| SEP authority | SEP valid | Apotek | Menilai coverage BPJS tingkat encounter; SEP saja tidak mengidentifikasi covered medication quantity. |
| Fornas authority | Mapping coverage item-level | Apotek | Membentuk Coverage Clearance hanya bagi covered quantity yang berlaku bersama SEP valid. |
| Cashier atau Payment authority | `Payment Clearance Established` | Apotek | Membentuk Fulfillment Clearance yang berlaku; payment tidak membuktikan stok atau handover. |
| Inventory | Stock Availability dan `Stock Reserved` | Apotek | Menyiapkan hanya jumlah Dispense Order yang diotorisasi; fakta stok tidak menulis ulang Prescription Review. |
| Apotek | Permintaan handover, expiry, shortage, atau return | Inventory | Memberikan Inventory Issue atau disposition return authoritative; Apotek tidak boleh menyimpulkan mutasi inventory. |
| Apotek | Kebutuhan Financial Charge, Credit Note, atau Refund | Tata Rekening | Menyelesaikan Financial Responsibility dan konsekuensi settlement tanpa mengubah riwayat fulfillment. |

## 9. Waktu Bisnis dan Batas Layanan

| Fakta timing | Aturan authoritative |
|---|---|
| Pharmacy `CreatedAt` | Dicatat ketika Patient Tracker menerbitkan Queue Number. |
| Pharmacy `ServedAt` | Dicatat ketika Dispense Order pertama yang berlaku menghasilkan `Medication Preparation Started`. |
| Pharmacy `DoneAt` | Dicatat ketika Staf Apotek melakukan coordinated pickup call. |
| Prescription Review | Dapat dimulai segera setelah Prescription tersedia; tidak menunggu kedatangan atau mapping Pasien. |
| Preparation Pasien Umum | Tidak dapat dimulai sebelum Payment Clearance membentuk Fulfillment Clearance. |
| Preparation BPJS | Tidak dapat dimulai sebelum SEP valid, mapping Fornas covered, dan Fulfillment Clearance. Sales Invoice tidak diwajibkan. |
| Pickup call | Hanya terjadi setelah setiap Dispense Order yang dimaksud untuk handover berstatus `Prepared` atau mempunyai exception outcome accountable. |
| Final Dispense Review dan edukasi | Terjadi dengan Pasien atau caregiver hadir setelah pickup call dan sebelum Medication Handover. |
| Sales Invoice BPJS | Hanya dibentuk bersama Medication Handover yang berhasil. |
| Batas pengambilan | Belum ada nilai numerik authoritative. Penyelesaian manual obat tidak diambil yang diotorisasi menetapkan `Collection Window Expired`. |

Technical timeout, polling, retry, dan performa aplikasi berada di luar workflow ini.

## 10. Keterlacakan

| Workflow ID | Domain rules | States | Domain Events | External authority |
|---|---|---|---|---|
| `WF-APT-RJ-001` | `BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087` | `Unmapped`, `Mapped`, `Waiting` | `Queue Entry Created`, `Outpatient Queue Mapped`, `Queue Entry Identified` | Patient Tracker |
| `WF-APT-RJ-002` | `BR-APT-001`–`BR-APT-019`, `BR-APT-050`, `BR-APT-061`, `BR-APT-086`, `BR-APT-089` | `Available`, `Under Review`, `Approved`, `Partially Approved`, `Rejected`, `Established`, `Active` | `Prescription Review Started`, `Medication Substitution Authorized`, `Prescription Review Completed`, `Direct Medication Request Accepted`, `Pharmacy Sales Order Established`, `Dispense Order Established` | CPOE, Medication Catalog, Inventory |
| `WF-APT-RJ-003` | `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095` | `Established`, `Issued`, `Financially Cleared`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `In Service`, `Done` | `Sales Invoice Established`, `Payment Clearance Established`, `Medication Preparation Started`, `Medication Prepared`, `Patient Called for Pickup`, `Final Dispense Review Completed`, `Medication Handed Over` | Patient Tracker, Payment, Inventory, Tata Rekening |
| `WF-APT-RJ-004` | `BR-APT-066`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095` | `Awaiting Clearance`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `In Service`, `Done` | `Coverage Clearance Established`, `Medication Preparation Started`, `Patient Called for Pickup`, `Sales Invoice Established`, `Medication Handed Over` | Patient Tracker, SEP, Fornas, Inventory, Tata Rekening |
| `WF-APT-RJ-005` | `BR-APT-015`, `BR-APT-022`, `BR-APT-044`, `BR-APT-090`–`BR-APT-095` | State Sales Invoice sesuai payer dan state Dispense Order bersama | `Billing Allocation Established`, `Coverage Clearance Established`, `Payment Clearance Established`, `Sales Invoice Established`, `Medication Handed Over` | SEP, Fornas, Payment, Tata Rekening |
| `WF-APT-RJ-006` | `BR-APT-084`–`BR-APT-088`, `BR-APT-095` | State authoritative per demand; satu antrean `Waiting` → `In Service` → `Done` | `Outpatient Queue Mapped`, `Medication Preparation Started`, `Patient Called for Pickup`, `Medication Handed Over` | Patient Tracker |
| `WF-APT-RJ-007` | `BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-079`–`BR-APT-080` | `Expired`, `Active`, `Resolved` | `Outpatient No-Show Recorded`, `Dispense Order Expired`, `Unfulfilled Medication Recorded`, `Medication Returned`, `Sales Invoice Credited`, `Refund Required`, `Pharmacy Sales Order Resolved` | Inventory, Tata Rekening |

Artifact canonical terkait:

- [Domain Apotek](./apotek-domain.md)
- [Domain Apotek — Bahasa Indonesia](./apotek-domain-id.md)
- [Outpatient Apotek Workflow — English](./outpatient-apotek-workflow.md)
- [Domain Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md)
- [Domain CPOE](../../contexts/cpoe/CPOE-DOMAIN-ID.md)
- [Domain Tata Rekening](../../contexts/TataRekening/02-domain.md)

Tujuh pasang spesifikasi operasional Rawat Jalan tercantum dalam [Indeks SOP Outpatient Apotek](./sop/SOP-APT-RJ-INDEX.md). Belum ada artifact integration/architecture khusus yang direferensikan oleh workflow pada revisi ini.
