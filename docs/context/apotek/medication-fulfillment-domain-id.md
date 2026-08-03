# Domain Medication Fulfillment — Pelayanan Obat Pasien

**Status artifact:** Pendamping semantik Bahasa Indonesia

**Bounded context:** Medication Fulfillment (`Pelayanan Obat Pasien`)

**Cakupan versi:** Target model bisnis lintas Rawat Jalan, Rawat Inap, IGD, dan Unit Dose Dispensing

**Sumber canonical English:** [medication-fulfillment-domain.md](./medication-fulfillment-domain.md)

**Context bisnis terkait:** [CPOE](../../contexts/cpoe/CPOE-DOMAIN-ID.md), [Policy Apotek Rawat Jalan](./apotek-rajal-domain-id.md), [Tata Rekening](../../contexts/TataRekening/02-domain.md)

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan dan nilai

Medication Fulfillment mengubah permintaan obat patient-specific yang diterima Farmasi menjadi alokasi komersial dan pemenuhan fisik obat yang dapat dipertanggungjawabkan. Domain ini memisahkan gabungan delivery stok dan billing pada legacy `Trs.DU (DO-Bill)` menjadi lifecycle `Sales Invoice` dan `Dispense Order` yang independen serta dikoordinasikan oleh `Pharmacy Sales Order`.

Bisnis harus memastikan bahwa:

- Prescription asli milik klinisi tetap authoritative dan dapat ditelusuri;
- hanya permintaan obat yang telah diterima secara profesional yang masuk ke Pharmacy Sales Order;
- Sales Invoice dan Dispense Order dialokasikan secara independen dari Sales Order Line;
- billing, payment atau coverage, ketersediaan stok, penyiapan, dan penyerahan tetap menjadi fakta bisnis yang berbeda;
- partial billing dan partial fulfillment tetap dapat dipertanggungjawabkan secara kuantitatif; dan
- setiap Accepted Quantity mencapai outcome fulfilled atau unfulfilled yang accountable.

### 1.2 Cakupan

Context ini mencakup:

1. Prescription Review dan penerimaan Direct Medication Request;
2. pembentukan dan alokasi Pharmacy Sales Order;
3. pembentukan Medication Sale dan Sales Invoice;
4. pembentukan Dispense Order dan physical dispensing;
5. commercial atau coverage clearance untuk fulfillment;
6. Medication Dispense dan Medication Handover; dan
7. shortage, substitution, backorder, cancellation, return, dan outcome non-fulfillment lainnya.

Context ini berlaku lintas Rawat Jalan, Rawat Inap, IGD, dan Unit Dose Dispensing.

### 1.3 Batas bisnis

Medication Fulfillment memiliki Prescription Review Outcome, Pharmacy Sales Order, Billing Allocation, Fulfillment Allocation, Medication Sale yang direpresentasikan oleh Sales Invoice, Dispense Order, Medication Dispense, Medication Handover, dan penyelesaian fulfillment.

Context ini bergantung pada context terkait tanpa mengambil alih authority mereka:

- CPOE atau authority clinical order lain memiliki Prescription asli dan intent klinisi;
- Medication Catalog atau authority formularium memiliki identitas obat dan kebijakan formularium;
- Inventory memiliki saldo stok dan mutasi stok authoritative;
- Payment memiliki receipt dan settlement evidence;
- Tata Rekening memiliki Financial Responsibility tingkat registrasi, alokasi payer, finalization, dan settlement initiation;
- Patient Tracker memiliki identitas dan lifecycle antrean Rawat Jalan; dan
- context pelayanan klinis memiliki Medication Administration.

Purchasing, supplier management, replenishment, transfer gudang, enterprise accounting, dan Medication Administration berada di luar context ini.

**Kebutuhan alignment yang diketahui:** spesifikasi Apotek Rawat Jalan yang ada dibuat sebelum pemisahan lintas setting ini dan menurunkan `Sale` secara langsung dari `Order Dispensing`. Relasi lama tersebut tidak mengatur shared target model dan harus diselaraskan sebelum perubahan implementasi terkait dianggap selesai. Kebijakan khusus Rawat Jalan mengenai antrean, pickup, dan no-show tetap dimiliki spesifikasi Apotek Rawat Jalan.

### 1.4 Pemisahan bisnis utama

Alur target adalah:

```text
Prescription atau Direct Medication Request
  -> Prescription Review atau direct acceptance
  -> Pharmacy Sales Order
       -> nol atau lebih Sales Invoice
       -> nol atau lebih Dispense Order
            -> Medication Dispense
            -> Medication Handover
```

Prescription tidak berubah menjadi Pharmacy Sales Order. Keputusan profesional yang selesai mengotorisasi pembentukan Pharmacy Sales Order baru dengan tetap mempertahankan Prescription sebagai sumber klinisnya.

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| Medication Fulfillment | Pelayanan Obat Pasien | Bounded context yang mengoordinasikan permintaan obat patient-specific sejak diterima Farmasi sampai alokasi komersial dan penyelesaian pemenuhan fisik. |
| Patient Medication Demand | Kebutuhan Obat Pasien | Kebutuhan obat patient-specific yang berasal dari Prescription atau Direct Medication Request. |
| Prescription | Resep | Intent authoritative klinisi agar obat disediakan atau diberikan kepada Pasien. |
| Electronic Prescription | Resep Elektronik | Prescription yang dibuat dan dikirim melalui authority clinical order elektronik. |
| Physical Prescription | Resep Fisik | Prescription non-elektronik yang harus dicatat sebelum ditelaah Farmasi. |
| Direct Medication Request | Permintaan Obat Langsung | Permintaan obat tanpa Prescription yang diizinkan. |
| Prescription Line | Baris Resep | Satu obat, instruksi dosis, dan jumlah yang diminta dalam Prescription. |
| Source Traceability | Keterlacakan Sumber | Hubungan accountable dari Medication Sale dan outcome dispensing kembali ke Sales Order, permintaan yang diterima, dan sumber aslinya. |
| Prescription Review | Telaah Resep | Penilaian administratif, farmasetik, dan klinis oleh Pharmacist terhadap Prescription. |
| Prescription Review Outcome | Hasil Telaah Resep | Disposition profesional atas Prescription atau Prescription Line berupa approved, partially approved, rejected, atau requiring clarification. |
| Clinical Clarification | Klarifikasi Klinis | Permintaan accountable untuk menyelesaikan ambiguitas atau masalah dengan klinisi yang bertanggung jawab sebelum penerimaan final. |
| Accepted Medication Line | Baris Obat Diterima | Baris obat yang diterima secara profesional untuk dimasukkan ke Pharmacy Sales Order, terlepas dari ketersediaan stok saat itu. |
| Pharmacy Sales Order | Pesanan Obat | Permintaan obat yang telah diterima dan dimiliki Farmasi sebagai sumber bersama alokasi komersial dan fulfillment. |
| Sales Order Line | Baris Pesanan Obat | Satu obat, jumlah, instruksi, dan dasar komersial yang berlaku dalam Pharmacy Sales Order. |
| Accepted Quantity | Jumlah Diterima | Jumlah maksimum Sales Order Line yang tersedia untuk dialokasikan dan diselesaikan secara accountable. |
| Order Allocation | Alokasi Pesanan | Penetapan seluruh atau sebagian Sales Order Line untuk tujuan komersial atau fulfillment. |
| Billing Allocation | Alokasi Penjualan | Penetapan jumlah atau nilai dari Sales Order Line kepada Medication Sale yang direpresentasikan oleh Sales Invoice. |
| Fulfillment Allocation | Alokasi Pemenuhan | Penetapan jumlah dari Sales Order Line kepada Dispense Order. |
| Medication Sale | Penjualan Obat | Transaksi komersial yang dibentuk dari satu atau lebih Billing Allocation milik satu Pharmacy Sales Order. |
| Sales Invoice | Faktur Jual | Dokumen komersial authoritative dan Aggregate Root yang merepresentasikan satu Medication Sale. |
| Billing Line | Baris Tagihan | Satu obat atau layanan yang berlaku beserta jumlah, harga, diskon, dan nilai di dalam Sales Invoice. |
| Pricing Snapshot | Rekaman Harga Transaksi | Dasar komersial immutable yang digunakan ketika Billing Allocation dan Sales Invoice dibentuk. |
| Payer | Penjamin | Pasien, BPJS, asuransi, perusahaan, atau pihak lain yang diharapkan menanggung charge obat. |
| Financial Charge | Tagihan Finansial | Konsekuensi finansial yang diberikan kepada Tata Rekening dari Medication Sale. |
| Purchase Confirmation | Konfirmasi Pembelian | Keputusan Pasien Umum untuk melanjutkan setelah nilai komersial yang berlaku disampaikan. |
| Payment Clearance | Izin Pembayaran | Evidence bahwa persyaratan pembayaran yang diperlukan telah terpenuhi. |
| Coverage Clearance | Izin Penjamin | Evidence bahwa payer yang berlaku mengotorisasi fulfillment tanpa pembayaran langsung Pasien. |
| Fulfillment Clearance | Izin Pemenuhan | Otorisasi bisnis yang mengizinkan Dispense Order berjalan berdasarkan kebijakan payment atau coverage yang berlaku. |
| Financial Adjustment | Penyesuaian Finansial | Koreksi accountable terhadap Medication Sale atau konsekuensi finansialnya. |
| Credit Note | Nota Kredit | Dokumen komersial yang mengurangi atau membalik nilai Sales Invoice yang telah diterbitkan. |
| Refund | Pengembalian Dana | Pengembalian dana yang sebelumnya telah diselesaikan secara accountable. |
| Dispense Order | Perintah Dispensing | Instruksi authoritative untuk memenuhi secara fisik satu atau lebih Fulfillment Allocation dari satu Pharmacy Sales Order. |
| Dispense Order Line | Baris Perintah Dispensing | Satu obat dan jumlah yang dialokasikan untuk dipenuhi secara fisik dalam Dispense Order. |
| Dispense Cycle | Siklus Dispensing | Periode atau batch fulfillment yang ditentukan, terutama untuk Rawat Inap dan Unit Dose Dispensing. |
| Unit Dose Dispensing | Dispensing Dosis Unit | Pemenuhan dalam dosis unit patient-specific atau periode pemberian yang ditentukan. |
| Stock Availability | Ketersediaan Stok | Representasi Inventory mengenai jumlah yang tersedia saat ini untuk mendukung fulfillment. |
| Stock Allocation | Alokasi Stok | Pengaitan stok oleh Inventory kepada kebutuhan fulfillment sebelum issue final. |
| Stock Reservation | Reservasi Stok | Stok yang diamankan untuk Dispense Order agar tidak dijanjikan kepada permintaan lain. |
| Inventory Issue | Pengeluaran Stok | Pengakuan authoritative Inventory bahwa obat telah keluar dari lokasi persediaan. |
| Medication Preparation | Penyiapan Obat | Pengambilan, penghitungan, pelabelan, pengemasan, dan penyiapan obat lainnya untuk fulfillment. |
| Compounding | Peracikan Obat | Penyiapan produk obat dari bahan atau komponen untuk kebutuhan fulfillment tertentu. |
| Final Dispense Review | Telaah Akhir Obat | Pemeriksaan profesional final bahwa obat yang disiapkan sesuai Dispense Order dan dapat dilanjutkan ke handover. |
| Prepared Medication | Obat Siap Diserahkan | Obat yang penyiapan fisiknya selesai dan menunggu telaah akhir atau handover. |
| In-Transit Medication | Obat Dalam Proses Penyerahan | Obat yang telah disiapkan dan dikeluarkan dari ketersediaan umum tetapi belum diserahkan kepada Authorized Recipient. |
| Medication Dispense | Realisasi Dispensing | Fakta accountable bahwa sejumlah obat benar-benar disediakan untuk Pasien. |
| Medication Handover | Penyerahan Obat | Pemindahan obat yang accountable kepada Authorized Recipient. |
| Authorized Recipient | Penerima Berwenang | Pasien, caregiver, practitioner, bangsal, atau pihak lain yang terverifikasi dan diizinkan menerima obat untuk Pasien. |
| Patient Education | Edukasi Pasien | Penjelasan accountable tentang penggunaan, penyimpanan, perhatian khusus, dan informasi obat relevan lainnya kepada Pasien atau caregiver. |
| Fulfilled Quantity | Jumlah Terpenuhi | Jumlah Sales Order Line yang mencapai outcome Medication Dispense berhasil. |
| Partial Fulfillment | Pemenuhan Sebagian | Pemenuhan kurang dari total Accepted Quantity ketika jumlah lain masih unresolved atau memperoleh outcome berbeda. |
| Fulfillment Completion | Penyelesaian Pemenuhan | Kondisi ketika setiap Accepted Quantity telah memiliki outcome final yang accountable. |
| Medication Administration | Pemberian Obat kepada Pasien | Fakta klinis bahwa obat benar-benar diberikan kepada atau dikonsumsi Pasien; dimiliki context eksternal. |
| Medication Shortage | Kekurangan Stok Obat | Stok tidak cukup untuk memenuhi jumlah obat yang dialokasikan. |
| Stock Discrepancy | Selisih Stok | Perbedaan antara stok tercatat dan stok fisik yang memengaruhi fulfillment. |
| Backorder | Pemenuhan Tertunda | Jumlah unresolved yang dipertahankan untuk dipenuhi kemudian saat supply tersedia. |
| Medication Substitution | Substitusi Obat | Penggantian accountable atas produk obat yang diminta berdasarkan authority profesional yang berlaku. |
| Unfulfilled Medication Outcome | Outcome Obat Tidak Terpenuhi | Alasan final dan accountable bahwa jumlah obat yang diterima tidak dipenuhi. |
| Copy Prescription | Salinan Resep | Catatan accountable mengenai obat atau jumlah dalam Prescription yang tidak dipenuhi, bila berlaku. |
| Dispense Cancellation | Pembatalan Dispensing | Pengakhiran accountable suatu Dispense Order sebelum fulfillment berhasil. |
| Fulfillment Expiry | Berakhirnya Pemenuhan | Berakhirnya kesempatan fulfillment karena periode layanan yang diizinkan telah lewat. |
| Medication Return | Retur Obat | Pengembalian accountable atas obat yang sebelumnya disiapkan, dipindahkan, atau diserahkan. |
| Return to Stock | Pengembalian ke Stok | Penerimaan authoritative oleh Inventory atas obat retur yang eligible menjadi stok tersedia. |
| No-Show | Pasien Tidak Hadir | Outcome Rawat Jalan ketika Pasien tidak mengambil obat dalam batas layanan yang berlaku. |
| Care Setting | Setting Pelayanan | Setting operasional yang menentukan kebijakan fulfillment, seperti Rawat Jalan, Rawat Inap, atau IGD. |
| Outpatient Fulfillment | Pelayanan Obat Rawat Jalan | Medication Fulfillment berdasarkan kebijakan kedatangan, antrean, pembayaran, pickup, dan no-show Rawat Jalan. |
| Inpatient Fulfillment | Pelayanan Obat Rawat Inap | Medication Fulfillment berdasarkan kebijakan bangsal, supply terjadwal, dan retur Rawat Inap. |
| Emergency Fulfillment | Pelayanan Obat IGD | Medication Fulfillment berdasarkan kebijakan urgent care yang berlaku. |
| Dose Window | Jendela Dosis | Periode pemberian yang ditentukan untuk merencanakan Dispense Cycle tanpa merepresentasikan Medication Administration. |
| Ward Delivery | Pengiriman ke Bangsal | Medication Handover dari Farmasi kepada Authorized Recipient di bangsal Rawat Inap. |

## 3. Kapabilitas Bisnis

### 3.1 Medication Demand Acceptance

**Indonesia:** Penerimaan Permintaan Obat

Menerima permintaan dari Prescription yang telah ditelaah atau Direct Medication Request yang diizinkan tanpa mengubah intent klinis asli.

### 3.2 Prescription Review

**Indonesia:** Telaah Resep

Menetapkan disposition profesional setiap Prescription dan barisnya, termasuk klarifikasi dan penerimaan sebagian.

### 3.3 Pharmacy Sales Order Management

**Indonesia:** Pengelolaan Pesanan Obat

Membentuk dan mempertahankan permintaan obat yang diterima, jumlah, Source Traceability, dan penyelesaian finalnya.

### 3.4 Commercial Allocation

**Indonesia:** Alokasi Komersial

Mengalokasikan Sales Order Line ke satu atau lebih Medication Sale dan Sales Invoice tanpa bergantung pada jumlah atau timing Dispense Order.

### 3.5 Fulfillment Allocation

**Indonesia:** Alokasi Pemenuhan

Mengalokasikan Sales Order Line ke satu atau lebih Dispense Order berdasarkan Care Setting, jumlah, lokasi, cycle, dan kebijakan fulfillment.

### 3.6 Commercial and Coverage Clearance

**Indonesia:** Izin Komersial dan Penjamin

Menentukan kapan Dispense Order dapat berjalan berdasarkan Payment Clearance, Coverage Clearance, atau kebijakan payer lain yang disetujui.

### 3.7 Physical Dispensing

**Indonesia:** Dispensing Fisik

Mengoordinasikan Stock Reservation, Medication Preparation, Compounding, Final Dispense Review, Medication Dispense, dan Medication Handover.

### 3.8 Partial and Unit-Dose Fulfillment

**Indonesia:** Pemenuhan Sebagian dan Dosis Unit

Mendukung tranche billing dan fulfillment dengan jumlah independen, termasuk Unit Dose Dispensing dan Dose Window.

### 3.9 Exception and Return Resolution

**Indonesia:** Penyelesaian Exception dan Retur

Menyelesaikan shortage, discrepancy, substitution, backorder, cancellation, expiry, return, no-show, dan konsekuensi finansial yang diperlukan.

### 3.10 Cross-Setting Traceability

**Indonesia:** Keterlacakan Lintas Setting

Mempertahankan keterlacakan kuantitatif dan sumber lintas outcome Rawat Jalan, Rawat Inap, IGD, billing, dispensing, dan handover.

## 4. Aktor & Peran

### 4.1 Prescribing Clinician

Memiliki Prescription asli dan merespons Clinical Clarification. Prescribing Clinician tidak memiliki penerimaan oleh Farmasi, pembentukan Sales Invoice, atau pelaksanaan dispensing.

### 4.2 Pharmacist

Memiliki Prescription Review Outcome, Medication Substitution yang diotorisasi secara profesional, Final Dispense Review, dan Patient Education yang berlaku.

### 4.3 Pharmacy Staff

Mengoordinasikan permintaan yang diterima, alokasi Sales Order, interaksi administratif Rawat Jalan, workflow penyiapan, dan handover accountable sesuai authority yang diberikan.

### 4.4 Pharmacy Technician

Melakukan Medication Preparation dan Compounding sesuai Dispense Order serta menyelesaikan discrepancy penyiapan di bawah supervisi profesional.

### 4.5 Patient or Caregiver

Memberikan confirmation dan payment yang berlaku, menerima edukasi, dan menerima obat ketika bertindak sebagai Authorized Recipient.

### 4.6 Cashier

Menerima pembayaran dan memberikan evidence Payment Clearance. Cashier tidak menetapkan eligibility obat atau fulfillment quantity.

### 4.7 Inpatient Authorized Recipient

Menerima Ward Delivery untuk Pasien dan tetap teridentifikasi dalam outcome Medication Handover. Penerimaan tersebut tidak merepresentasikan Medication Administration.

### 4.8 Pharmacy Supervisor

Memiliki keputusan exceptional di luar authority biasa, termasuk expiry, return, shortage resolution, dan koreksi accountable yang disetujui.

## 5. Domain Objects

### 5.1 Prescription Review

Merepresentasikan penilaian profesional Farmasi terhadap satu Prescription. Object ini mempertahankan keputusan per baris, clarification, Pharmacist yang bertanggung jawab, dan Source Traceability tanpa menulis ulang Prescription.

### 5.2 Pharmacy Sales Order

Merepresentasikan satu permintaan obat patient-specific yang diterima. Object ini memiliki Sales Order Line, Accepted Quantity, Billing Allocation, Fulfillment Allocation, dan resolution progress.

### 5.3 Billing Allocation

Merepresentasikan bagian Sales Order Line yang dialokasikan kepada satu Medication Sale dan Sales Invoice. Object ini mempertahankan dasar jumlah atau nilai yang berlaku serta referensi Pricing Snapshot.

### 5.4 Fulfillment Allocation

Merepresentasikan bagian Sales Order Line yang dialokasikan kepada satu Dispense Order. Object ini mempertahankan jumlah, Care Setting, dan Dispense Cycle yang berlaku.

### 5.5 Sales Invoice

Merepresentasikan satu Medication Sale dari satu Pharmacy Sales Order. Object ini memiliki Billing Line, klasifikasi payer, nilai komersial, financial disposition, adjustment, dan outcome Financial Charge.

### 5.6 Dispense Order

Merepresentasikan satu instruksi physical fulfillment dari satu Pharmacy Sales Order. Object ini memiliki Dispense Order Line, progress penyiapan dan review, outcome Medication Dispense, dan final fulfillment disposition.

### 5.7 Fulfillment Clearance

Menghubungkan jumlah yang diizinkan dari satu Dispense Order dengan Payment Clearance, Coverage Clearance, atau commercial evidence lain yang disetujui. Object ini dapat mereferensikan beberapa Sales Invoice bila diwajibkan allocation policy.

### 5.8 Medication Dispense

Merepresentasikan obat dan jumlah aktual yang disediakan untuk Pasien, termasuk responsible party, effective time, dan Dispense Order sumber.

### 5.9 Medication Handover

Merepresentasikan pemindahan kepada Authorized Recipient, termasuk verifikasi penerima, handover time, destination bila berlaku, dan tanggung jawab Patient Education.

### 5.10 Unfulfilled Medication Outcome

Merepresentasikan alasan final suatu Accepted Quantity tidak dipenuhi dan mengidentifikasi Copy Prescription, penutupan backorder, return, atau financial correction yang diperlukan.

## 6. Aggregates

### 6.1 Prescription Review Aggregate

**Aggregate Root:** `Prescription Review`

Aggregate menjaga referensi sumber Prescription, professional disposition per baris, Clinical Clarification, Pharmacist yang bertanggung jawab, dan completion outcome tetap konsisten. Aggregate tidak dapat mengubah Prescription asli milik klinisi.

### 6.2 Pharmacy Sales Order Aggregate

**Aggregate Root:** `Pharmacy Sales Order`

Aggregate memiliki Sales Order Line, Billing Allocation, Fulfillment Allocation, Accepted Quantity, Fulfilled Quantity, unfulfilled outcome, dan overall resolution.

Aggregate memastikan alokasi komersial dan fulfillment tetap dapat ditelusuri serta tidak melebihi authority Sales Order Line yang berlaku. Aggregate tidak memiliki payment settlement Sales Invoice, saldo Inventory, atau pelaksanaan physical dispensing.

### 6.3 Medication Sale Aggregate

**Aggregate Root:** `Sales Invoice`

Aggregate merepresentasikan satu Medication Sale. Aggregate menjaga Billing Line, Pricing Snapshot, Payer, Purchase Confirmation bila berlaku, financial disposition, Credit Note, refund, dan outcome Financial Charge tetap konsisten.

Sales Invoice mereferensikan tepat satu Pharmacy Sales Order tetapi dapat mencakup satu atau lebih Sales Order Line miliknya.

### 6.4 Dispense Order Aggregate

**Aggregate Root:** `Dispense Order`

Aggregate menjaga Fulfillment Allocation, physical preparation, Final Dispense Review, Medication Dispense, Medication Handover, cancellation, expiry, return, dan non-fulfillment outcome tetap konsisten.

Dispense Order mereferensikan tepat satu Pharmacy Sales Order tetapi dapat memenuhi satu atau lebih Sales Order Line miliknya.

### 6.5 Relasi lintas aggregate

Satu Pharmacy Sales Order dapat memiliki nol atau lebih Sales Invoice dan nol atau lebih Dispense Order. Sales Invoice dan Dispense Order tidak diwajibkan memiliki jumlah atau waktu pembentukan yang sama.

Korelasi bisnisnya dinyatakan melalui Billing Allocation, Fulfillment Allocation, dan Fulfillment Clearance pada tingkat Sales Order Line dan jumlah. Penggunaan Pharmacy Sales Order yang sama tidak dengan sendirinya menetapkan bahwa setiap Sales Invoice memberikan clearance kepada setiap Dispense Order.

## 7. Aturan Bisnis

### 7.1 Sumber dan penerimaan profesional

- **BR-MF-001** — Patient Medication Demand harus berasal dari tepat satu Prescription atau Direct Medication Request.
- **BR-MF-002** — Prescription asli dan intent klinisi harus tetap dimiliki oleh authority clinical order terkait.
- **BR-MF-003** — Setiap Prescription harus menyelesaikan Prescription Review sebelum barisnya masuk ke Pharmacy Sales Order.
- **BR-MF-004** — Hanya Pharmacist yang boleh menetapkan Prescription Review Outcome dan Medication Substitution yang diotorisasi secara profesional.
- **BR-MF-005** — Prescription Review Outcome harus mencatat disposition untuk setiap Prescription Line yang ditelaah.
- **BR-MF-006** — Prescription yang rejected tidak boleh membentuk Pharmacy Sales Order.
- **BR-MF-007** — Prescription yang partially approved dapat membentuk Pharmacy Sales Order yang hanya berisi Accepted Medication Line.
- **BR-MF-008** — Penerimaan klinis harus independen dari Stock Availability saat itu; fakta stok tidak boleh menulis ulang professional eligibility.
- **BR-MF-009** — Direct Medication Request harus mengikuti kebijakan penerimaan profesional dan organisasi yang berlaku tanpa membentuk Prescription.

### 7.2 Pharmacy Sales Order

- **BR-MF-010** — Pharmacy Sales Order harus berasal dari tepat satu sumber accepted demand yang telah selesai.
- **BR-MF-011** — Satu Prescription harus membentuk maksimal satu Pharmacy Sales Order aktif dalam satu fulfillment episode.
- **BR-MF-012** — Pharmacy Sales Order harus memiliki minimal satu Sales Order Line dengan Accepted Quantity positif.
- **BR-MF-013** — Setiap Sales Order Line harus mempertahankan Source Traceability ke Prescription Line atau baris Direct Medication Request sumbernya.
- **BR-MF-014** — Pharmacy Sales Order bukan Sales Invoice, catatan pembayaran, Stock Reservation, Dispense Order, atau evidence Medication Dispense.
- **BR-MF-015** — Billing Allocation dan Fulfillment Allocation dapat terjadi secara independen pada waktu bisnis yang berbeda.
- **BR-MF-016** — Fulfillment Allocation aktif suatu Sales Order Line tidak boleh melebihi Accepted Quantity yang belum terselesaikan.
- **BR-MF-017** — Fulfilled Quantity tidak boleh melebihi jumlah yang dialokasikan untuk fulfillment.
- **BR-MF-018** — Setiap Accepted Quantity pada akhirnya harus fulfilled, cancelled, expired, backordered, atau memperoleh Unfulfilled Medication Outcome lain yang accountable.
- **BR-MF-019** — Pharmacy Sales Order hanya boleh mencapai Fulfillment Completion ketika setiap Accepted Quantity memiliki outcome final yang accountable.

### 7.3 Medication Sale dan Sales Invoice

- **BR-MF-020** — Setiap Medication Sale harus direpresentasikan oleh tepat satu Sales Invoice.
- **BR-MF-021** — Setiap Sales Invoice harus berasal dari Billing Allocation milik tepat satu Pharmacy Sales Order.
- **BR-MF-022** — Satu Pharmacy Sales Order dapat menghasilkan nol, satu, atau beberapa Sales Invoice.
- **BR-MF-023** — Sales Invoice dapat mencakup satu atau lebih Sales Order Line dan harus mempertahankan setiap source allocation.
- **BR-MF-024** — Billing Line tidak boleh memperkenalkan baris obat yang tidak ada pada Pharmacy Sales Order sumbernya, kecuali komponen komersial non-obat yang diotorisasi secara eksplisit.
- **BR-MF-025** — Sales Invoice harus mempertahankan Pricing Snapshot dan Payer yang berlaku saat dibentuk.
- **BR-MF-026** — Pembentukan Sales Invoice tidak membuktikan bahwa stok tersedia, direservasi, disiapkan, didispensing, atau diserahkan.
- **BR-MF-027** — Sales Invoice yang issued atau financially settled harus dikoreksi melalui Financial Adjustment, Credit Note, atau outcome Refund yang accountable, bukan penggantian diam-diam.
- **BR-MF-028** — Setiap Financial Charge yang dikirim ke Tata Rekening harus mempertahankan Source Traceability ke Sales Invoice dan Pharmacy Sales Order sumbernya.

### 7.4 Dispense Order dan dispensing

- **BR-MF-029** — Setiap Dispense Order harus berasal dari Fulfillment Allocation milik tepat satu Pharmacy Sales Order.
- **BR-MF-030** — Satu Pharmacy Sales Order dapat menghasilkan nol, satu, atau beberapa Dispense Order.
- **BR-MF-031** — Dispense Order dapat mencakup satu atau lebih Sales Order Line dan harus mempertahankan setiap source allocation.
- **BR-MF-032** — Jumlah, pembagian quantity, dan timing Dispense Order dapat berbeda dari jumlah, pembagian nilai, dan timing Sales Invoice.
- **BR-MF-033** — Stock Reservation dan Inventory Issue harus tetap menjadi outcome authoritative milik Inventory yang diminta untuk Dispense Order.
- **BR-MF-034** — Medication Preparation dan Compounding harus menggunakan Dispense Order aktif sebagai authority.
- **BR-MF-035** — Prepared Medication harus menyelesaikan Final Dispense Review sebelum Medication Handover.
- **BR-MF-036** — Medication Dispense tidak boleh melebihi unresolved quantity milik Dispense Order Line.
- **BR-MF-037** — Medication Handover harus mengidentifikasi Authorized Recipient dan effective business time.
- **BR-MF-038** — Ward Delivery tidak boleh diperlakukan sebagai Medication Administration.
- **BR-MF-039** — Medication Administration tidak boleh disimpulkan dari Sales Invoice, Inventory Issue, Medication Dispense, atau Ward Delivery.

### 7.5 Clearance dan koordinasi lintas aggregate

- **BR-MF-040** — Dispense Order hanya boleh berjalan ketika Fulfillment Clearance yang diwajibkan oleh kebijakan Care Setting dan Payer tersedia.
- **BR-MF-041** — Payment Clearance harus berasal dari payment authority yang bertanggung jawab dan tidak boleh disimpulkan hanya dari keberadaan Sales Invoice.
- **BR-MF-042** — Coverage Clearance harus mengidentifikasi Payer dan authority covered fulfillment yang berlaku.
- **BR-MF-043** — Fulfillment Clearance harus mengidentifikasi jumlah Dispense Order yang diotorisasi dan supporting commercial evidence.
- **BR-MF-044** — Satu Sales Invoice dapat mendukung clearance beberapa Dispense Order, dan satu Dispense Order dapat bergantung pada beberapa commercial allocation ketika diwajibkan kebijakan.
- **BR-MF-045** — Sales Invoice yang paid atau financially cleared tidak menjamin fulfillment berhasil ketika terjadi shortage, discrepancy, expiry, atau exception sah lainnya.
- **BR-MF-046** — Financial clearance yang diikuti non-fulfillment harus menghasilkan backorder, substitution, Credit Note, Refund, atau resolution lain yang disetujui dan accountable.

### 7.6 Partial fulfillment, UDD, dan exception

- **BR-MF-047** — Partial Fulfillment harus mempertahankan jumlah fulfilled, unresolved, dan unfulfilled secara terpisah.
- **BR-MF-048** — Unit Dose Dispensing dapat membagi satu Sales Order Line menjadi beberapa Dispense Cycle dan Dispense Order.
- **BR-MF-049** — Dose Window harus memandu perencanaan fulfillment dan tidak boleh menyatakan Medication Administration.
- **BR-MF-050** — Medication Substitution harus mempertahankan obat yang diminta, obat yang disediakan, responsible authority, alasan, dan jumlah yang terpengaruh.
- **BR-MF-051** — Medication Shortage atau Stock Discrepancy tidak boleh mengubah Prescription asli atau menghapus Sales Invoice yang sudah ada.
- **BR-MF-052** — Medication Return harus mengidentifikasi Dispense Order sumber, jumlah, alasan, dan disposition Inventory finalnya.
- **BR-MF-053** — Return to Stock hanya boleh terjadi ketika Inventory menerima obat retur berdasarkan kebijakannya sendiri.
- **BR-MF-054** — Copy Prescription harus mengidentifikasi obat atau jumlah dalam Prescription yang tetap tidak terpenuhi.
- **BR-MF-055** — No-Show harus menjadi outcome kebijakan Rawat Jalan dan tidak boleh diterapkan pada Ward Delivery Rawat Inap.

### 7.7 Completion dan history

- **BR-MF-056** — Commercial progress dan fulfillment progress Pharmacy Sales Order harus dilacak secara independen.
- **BR-MF-057** — Commercial resolution tidak dengan sendirinya menyelesaikan physical fulfillment, dan physical fulfillment tidak dengan sendirinya membuktikan financial resolution.
- **BR-MF-058** — Keputusan material mengenai review, allocation, invoice, clearance, dispensing, handover, exception, dan correction harus mempertahankan responsible party dan effective business time.
- **BR-MF-059** — Source Traceability harus dipertahankan dari Prescription atau Direct Medication Request melalui Pharmacy Sales Order, Sales Invoice, Dispense Order, dan final outcome.
- **BR-MF-060** — Outcome bisnis yang completed atau cancelled tidak boleh dihapus; koreksi kemudian harus menambahkan correcting fact yang accountable.

## 8. State Machines & Lifecycles

### 8.1 Lifecycle Prescription Review

```text
Available
  -> Under Review
       -> Clarification Required
            -> Under Review
       -> Approved
       -> Partially Approved
       -> Rejected
```

`Approved`, `Partially Approved`, dan `Rejected` adalah outcome review final. Prescription yang dikoreksi atau diganti memerlukan keputusan review yang dapat ditelusuri secara terpisah.

### 8.2 Lifecycle Pharmacy Sales Order

```text
Established
  -> Active
       -> Resolved
       -> Cancelled
```

| State | Makna bisnis |
|---|---|
| Established | Accepted demand telah tersedia dan allocation dapat dimulai. |
| Active | Minimal satu accepted quantity masih belum terselesaikan secara komersial atau fisik. |
| Resolved | Setiap accepted quantity dan konsekuensi komersial yang diperlukan telah memiliki outcome final yang accountable. |
| Cancelled | Sisa accepted demand diakhiri berdasarkan keputusan berwenang; outcome sebelumnya tetap dipertahankan. |

Commercial progress dan fulfillment progress menjadi dimensi terpisah di dalam `Active` dan tidak digabungkan menjadi banyak nama state kombinasi.

### 8.3 Lifecycle Sales Invoice

```text
Established
  -> Issued
       -> Financially Cleared
            -> Resolved

Established or Issued
  -> Cancelled

Issued or Financially Cleared
  -> Adjusted or Credited
       -> Resolved
```

Payment dan settlement evidence tetap dimiliki context eksternal. `Financially Cleared` dapat didukung oleh Payment Clearance atau Coverage Clearance sesuai kebijakan payer.

### 8.4 Lifecycle Dispense Order

```text
Established
  -> Awaiting Clearance
  -> Released
  -> Preparing
  -> Prepared
  -> Reviewed
  -> Completed

Established, Awaiting Clearance, Released, Preparing, Prepared, or Reviewed
  -> Cancelled | Expired | Unfulfilled
```

`Completed` membutuhkan Medication Dispense yang accountable dan Medication Handover yang berlaku. `Unfulfilled` membutuhkan alasan serta resolution atas stok yang dialokasikan dan konsekuensi finansialnya.

### 8.5 Lifecycle jumlah Medication Fulfillment

```text
Accepted Quantity
  -> Billing Allocated or Commercially Unallocated
  -> Fulfillment Allocated or Fulfillment Unallocated
  -> Fulfilled | Backordered | Cancelled | Expired | Other Unfulfilled Outcome
```

Cabang billing dan fulfillment berkembang secara independen. Resolution Sales Order final memerlukan rekonsiliasi kedua cabang, bukan jumlah dokumen yang identik.

## 9. Domain Events

| Domain Event | Makna bisnis |
|---|---|
| Prescription Review Started | Pharmacist memulai penilaian profesional atas Prescription. |
| Clinical Clarification Requested | Suatu masalah memerlukan clarification sebelum review dapat diselesaikan. |
| Prescription Review Completed | Setiap baris yang ditelaah memperoleh professional disposition final. |
| Direct Medication Request Accepted | Permintaan non-resep yang diizinkan telah diterima Farmasi. |
| Pharmacy Sales Order Established | Permintaan obat yang diterima tersedia untuk alokasi komersial dan fulfillment. |
| Billing Allocation Established | Jumlah atau nilai Sales Order dialokasikan kepada Medication Sale. |
| Fulfillment Allocation Established | Jumlah Sales Order dialokasikan kepada Dispense Order. |
| Sales Invoice Established | Medication Sale dibentuk dari Billing Allocation. |
| Sales Invoice Issued | Sales Invoice menjadi dokumen komersial authoritative. |
| Payment Clearance Established | Payment authority yang bertanggung jawab mengonfirmasi kondisi pembayaran yang berlaku. |
| Coverage Clearance Established | Payer yang berlaku mengotorisasi covered fulfillment. |
| Fulfillment Clearance Established | Suatu jumlah Dispense Order diotorisasi untuk berjalan. |
| Dispense Order Established | Instruksi physical fulfillment dibentuk dari Fulfillment Allocation. |
| Stock Reserved | Inventory mengamankan stok untuk Dispense Order. |
| Medication Preparation Started | Penyiapan fisik dimulai berdasarkan Dispense Order yang telah released. |
| Medication Prepared | Obat yang dialokasikan menyelesaikan penyiapan fisik. |
| Final Dispense Review Completed | Prepared Medication lulus pemeriksaan profesional akhir yang diperlukan. |
| Medication Dispensed | Sejumlah obat disediakan secara accountable untuk Pasien. |
| Medication Handed Over | Obat dipindahkan kepada Authorized Recipient. |
| Medication Shortage Identified | Stok tersedia tidak dapat mendukung jumlah fulfillment yang dimaksud. |
| Medication Substitution Authorized | Authority yang accountable menyetujui penggantian obat yang diminta. |
| Dispense Order Backordered | Jumlah unresolved dipertahankan untuk fulfillment kemudian. |
| Dispense Order Cancelled | Keputusan berwenang mengakhiri Dispense Order sebelum completion. |
| Dispense Order Expired | Periode fulfillment yang diizinkan berakhir tanpa completion. |
| Unfulfilled Medication Recorded | Accepted Quantity memperoleh outcome non-fulfillment final. |
| Medication Returned | Obat yang sebelumnya disiapkan atau disediakan telah dikembalikan. |
| Sales Invoice Credited | Credit Note mengurangi atau membalik konsekuensi Sales Invoice. |
| Refund Required | Financial resolution memerlukan pengembalian dana yang telah diselesaikan. |
| Pharmacy Sales Order Resolved | Setiap Accepted Quantity dan konsekuensi komersial yang diperlukan memperoleh outcome final yang accountable. |

## 10. Workflow Bisnis

### 10.1 Accept a Prescription and establish a Pharmacy Sales Order

**Indonesia:** Menerima Prescription dan membentuk Pharmacy Sales Order

```text
Prescription tersedia
  -> Pharmacist melakukan Prescription Review
  -> setiap Prescription Line memperoleh disposition
  -> Accepted Medication Line diidentifikasi
  -> Pharmacy Sales Order dibentuk
  -> Source Traceability ke Prescription asli dipertahankan
```

Jika tidak ada baris yang diterima, Pharmacy Sales Order tidak dibentuk.

### 10.2 Accept a Direct Medication Request

**Indonesia:** Menerima Direct Medication Request

```text
Direct Medication Request diterima
  -> kebijakan penerimaan yang berlaku diselesaikan
  -> baris obat yang diterima diidentifikasi
  -> Pharmacy Sales Order dibentuk
```

Prescription tidak dibentuk hanya untuk mendukung direct request.

### 10.3 Allocate one Sales Order into multiple Sales Invoices

**Indonesia:** Mengalokasikan satu Sales Order ke beberapa Sales Invoice

```text
Pharmacy Sales Order aktif
  -> Billing Allocation membagi Sales Order Line yang berlaku
  -> satu atau lebih Medication Sale dibentuk
  -> setiap Medication Sale direpresentasikan oleh satu Sales Invoice
  -> setiap Billing Line mempertahankan sumber Sales Order
```

Pembagian dapat mengikuti payer, periode komersial, covered quantity, atau dasar bisnis lain yang disetujui.

### 10.4 Allocate one Sales Order into multiple Dispense Orders

**Indonesia:** Mengalokasikan satu Sales Order ke beberapa Dispense Order

```text
Pharmacy Sales Order aktif
  -> Fulfillment Allocation membagi Sales Order Line yang berlaku
  -> satu atau lebih Dispense Order dibentuk
  -> setiap Dispense Order mengikuti kebijakan Care Setting dan Dispense Cycle
```

Pembagian Dispense Order independen dari pembagian Sales Invoice.

### 10.5 Clear and fulfill medication

**Indonesia:** Memberikan clearance dan memenuhi obat

```text
Sales Invoice atau dasar coverage yang disetujui tersedia
  -> Payment Clearance atau Coverage Clearance dibentuk
  -> Fulfillment Clearance mengotorisasi jumlah Dispense Order yang berlaku
  -> stok direservasi
  -> obat disiapkan
  -> Final Dispense Review selesai
  -> Medication Dispense dicatat
  -> Medication Handover selesai
```

Urutan persis pembentukan invoice, reservasi stok, dan penyiapan mengikuti kebijakan payer dan Care Setting, tetapi clearance yang diwajibkan harus tersedia sebelum controlled fulfillment point menurut kebijakan tersebut.

### 10.6 Unit Dose Dispensing

**Indonesia:** Unit Dose Dispensing

```text
Pharmacy Sales Order Rawat Inap aktif
  -> Accepted Quantity direncanakan lintas Dose Window
  -> setiap Dispense Cycle memperoleh Fulfillment Allocation
  -> satu atau lebih Dispense Order dibentuk
  -> obat disiapkan dan ditelaah per cycle
  -> Ward Delivery mencatat Medication Handover
  -> cycle masa depan tetap dapat diubah secara independen selama belum fulfilled
```

Ward Delivery tidak mencatat Medication Administration.

### 10.7 Resolve paid medication that cannot be fulfilled

**Indonesia:** Menyelesaikan obat yang telah dibayar tetapi tidak dapat dipenuhi

```text
Sales Invoice financially cleared
  -> Dispense Order mengalami shortage atau discrepancy
  -> pihak bertanggung jawab menilai resolution yang tersedia
       -> fulfillment kemudian melalui Backorder
       -> Medication Substitution yang diotorisasi
       -> fulfillment dari sumber stok lain yang disetujui
       -> cancellation dengan Credit Note atau Refund
       -> Unfulfilled Medication Outcome lain yang accountable
  -> jumlah Sales Order dan konsekuensi komersial direkonsiliasi
```

Prescription dan fakta finansial sebelumnya tetap dapat ditelusuri.

### 10.8 Resolve outpatient No-Show

**Indonesia:** Menyelesaikan No-Show Rawat Jalan

```text
Prepared Medication siap untuk handover Rawat Jalan
  -> Pasien tidak mengambil dalam batas layanan yang berlaku
  -> No-Show ditetapkan
  -> Dispense Order mengikuti kebijakan expiry atau cancellation
  -> obat dikembalikan ketika eligible
  -> konsekuensi komersial paid, covered, atau uninvoiced diselesaikan
```

### 10.9 Complete a Pharmacy Sales Order

**Indonesia:** Menyelesaikan Pharmacy Sales Order

```text
Seluruh Billing Allocation mencapai final commercial disposition
  + seluruh Accepted Quantity mencapai outcome fulfilled atau accountable unfulfilled
  -> Pharmacy Sales Order menjadi Resolved
```

Resolution tidak mengharuskan jumlah Sales Invoice dan Dispense Order sama.
