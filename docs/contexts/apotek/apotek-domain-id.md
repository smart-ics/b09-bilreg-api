# Domain Apotek — Pelayanan Obat Pasien

**Status artifact:** Pendamping semantik Bahasa Indonesia

**Bounded context:** Apotek (`Pelayanan Obat Pasien`)

**Cakupan versi:** Target model bisnis lintas Rawat Jalan, Rawat Inap, IGD, dan Unit Dose Dispensing

**Sumber canonical English:** [apotek-domain.md](./apotek-domain.md)

**Context bisnis terkait:** [CPOE](../../contexts/cpoe/CPOE-DOMAIN-ID.md), [Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), [Tata Rekening](../../contexts/TataRekening/02-domain.md)

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan dan nilai

Apotek mengubah permintaan obat patient-specific yang diterima Farmasi menjadi alokasi komersial dan pemenuhan fisik obat yang dapat dipertanggungjawabkan. Domain ini memisahkan gabungan delivery stok dan billing pada legacy `Trs.DU (DO-Bill)` menjadi lifecycle `Sales Invoice` dan `Dispense Order` yang independen serta dikoordinasikan oleh `Sales Order`.

Bisnis harus memastikan bahwa:

- Resep asli milik klinisi tetap authoritative dan dapat ditelusuri;
- hanya permintaan obat yang telah diterima secara profesional yang masuk ke Sales Order;
- Sales Invoice dan Dispense Order dialokasikan secara independen dari Sales Order Line;
- billing, payment atau coverage, ketersediaan stok, penyiapan, dan penyerahan tetap menjadi fakta bisnis yang berbeda;
- partial billing dan partial fulfillment tetap dapat dipertanggungjawabkan secara kuantitatif; dan
- setiap Accepted Quantity mencapai outcome fulfilled atau unfulfilled yang accountable.

### 1.2 Cakupan

Context ini mencakup:

1. Telaah Resep dan penerimaan Direct Medication Request;
2. pembentukan dan alokasi Sales Order;
3. pembentukan Medication Sale dan Sales Invoice;
4. pembentukan Dispense Order dan physical dispensing;
5. commercial atau coverage clearance untuk fulfillment;
6. Medication Dispense dan Medication Handover; dan
7. shortage, substitution, backorder, cancellation, return, dan outcome non-fulfillment lainnya; dan
8. kebijakan workflow Rawat Jalan yang saat ini telah didefinisikan untuk antrean, payer, pickup, dan no-show.

Context ini berlaku lintas Rawat Jalan, Rawat Inap, IGD, dan Unit Dose Dispensing.

### 1.3 Batas bisnis

Apotek memiliki Hasil Telaah Resep, Sales Order, Billing Allocation, Fulfillment Allocation, Medication Sale yang direpresentasikan oleh Sales Invoice, Dispense Order, Medication Dispense, Medication Handover, dan penyelesaian fulfillment.

Context ini bergantung pada context terkait tanpa mengambil alih authority mereka:

- CPOE atau authority clinical order lain memiliki Resep asli dan intent klinisi;
- Medication Catalog atau authority formularium memiliki identitas obat dan kebijakan formularium;
- Inventory memiliki saldo stok dan mutasi stok authoritative;
- Payment memiliki receipt dan settlement evidence;
- Tata Rekening memiliki Financial Responsibility tingkat registrasi, alokasi payer, finalization, dan settlement initiation;
- Patient Tracker memiliki identitas dan lifecycle antrean Rawat Jalan; dan
- context pelayanan klinis memiliki Medication Administration.

Purchasing, supplier management, replenishment, transfer gudang, enterprise accounting, dan Medication Administration berada di luar context ini.

Identitas dan lifecycle antrean Rawat Jalan tetap dimiliki secara eksternal oleh Patient Tracker. Apotek memiliki keputusan bisnis Rawat Jalan yang melakukan mapping antara Pharmacy Queue Entry dengan sumber pelayanan obat yang sesuai serta memiliki kebijakan payer, pickup, handover, dan no-show setelah mapping tersebut.

Untuk antrean apotek Rawat Jalan, Patient Tracker mencatat `CreatedAt` ketika Queue Number diterbitkan, `ServedAt` ketika Dispense Order pertama yang berlaku memasuki `Preparing`, dan `DoneAt` ketika Staf Apotek melakukan pickup call. Milestone antrean tersebut menggambarkan kemajuan operasional antrean dan tidak membuktikan Medication Handover.

### 1.4 Pemisahan bisnis utama

Alur target adalah:

```text
Resep atau Direct Medication Request
  -> Telaah Resep atau direct acceptance
  -> Sales Order
       -> nol atau lebih Sales Invoice
       -> nol atau lebih Dispense Order
            -> Medication Dispense
            -> Medication Handover
```

Resep tidak berubah menjadi Sales Order. Keputusan profesional yang selesai mengotorisasi pembentukan Sales Order baru dengan tetap mempertahankan Resep sebagai sumber klinisnya.

## 2. Ubiquitous Language
| Inggris | Indonesia | Definisi |
|---|---|---|
| Apotek | Pelayanan Obat Pasien | Bounded context yang mengoordinasikan permintaan obat patient-specific sejak diterima Farmasi sampai alokasi komersial dan penyelesaian pemenuhan fisik. |
| Patient Medication Demand | Permintaan Obat Pasien | Permintaan obat patient-specific yang berasal dari Resep atau Direct Medication Request. |
| Resep | Resep | Intent authoritative klinisi agar obat disediakan atau diberikan kepada Pasien. |
| Resep Elektronik | Resep Elektronik | Resep yang dibuat dan dikirim melalui authority clinical order elektronik. |
| Resep Fisik | Resep Fisik | Resep non-elektronik yang harus dicatat sebelum ditelaah Farmasi. |
| Direct Medication Request | Permintaan Obat Langsung | Permintaan obat tanpa Resep yang diizinkan. |
| Baris Resep | Baris Resep | Satu obat, instruksi dosis, dan jumlah yang diminta dalam Resep. |
| Source Traceability | Keterlacakan Sumber | Hubungan accountable dari Medication Sale dan outcome dispensing kembali ke Sales Order, permintaan yang diterima, dan sumber aslinya. |
| Telaah Resep | Telaah Resep | Penilaian administratif, farmasetik, dan klinis oleh Pharmacist terhadap Resep. |
| Hasil Telaah Resep | Hasil Telaah Resep | Keputusan profesional atas resep: disetujui, disetujui sebagian, atau ditolak. Obat yang diterima diwujudkan sebagai Sales Order Line. |
| Accepted Medication Line | Baris Obat Diterima | Baris obat yang diterima secara profesional untuk dimasukkan ke Sales Order, terlepas dari ketersediaan stok saat itu. |
| Sales Order | Sales Order | Permintaan obat yang telah diterima dan dimiliki Farmasi sebagai sumber bersama alokasi komersial dan fulfillment. |
| Sales Order Line | Sales Order Line | Satu obat, jumlah, instruksi, dan dasar komersial yang berlaku dalam Sales Order. |
| Accepted Quantity | Jumlah Diterima | Jumlah maksimum Sales Order Line yang tersedia untuk dialokasikan dan diselesaikan secara accountable. |
| Order Allocation | Alokasi Pesanan | Penetapan seluruh atau sebagian Sales Order Line untuk tujuan komersial atau fulfillment. |
| Billing Allocation | Alokasi Penjualan | Penetapan jumlah atau nilai dari Sales Order Line kepada Medication Sale yang direpresentasikan oleh Sales Invoice. |
| Fulfillment Allocation | Alokasi Pemenuhan | Penetapan jumlah dari Sales Order Line kepada Dispense Order. |
| Medication Sale | Penjualan Obat | Transaksi komersial yang dibentuk dari satu atau lebih Billing Allocation milik satu Sales Order. |
| Sales Invoice | Faktur Jual | Dokumen komersial authoritative dan Aggregate Root yang merepresentasikan satu Medication Sale. |
| Legacy DU | DU Legacy | Transaksi legacy `Trs.DU (DO-Bill)` yang menggabungkan concern billing obat dan delivery stok; pada target model, faktanya direpresentasikan melalui satu Sales Invoice dan satu atau lebih Dispense Order yang dikorelasikan oleh alokasi Sales Order. |
| Billing Line | Baris Tagihan | Satu obat atau layanan yang berlaku beserta jumlah, harga, diskon, dan nilai di dalam Sales Invoice. |
| Pricing Snapshot | Rekaman Harga Transaksi | Dasar komersial immutable yang digunakan ketika Billing Allocation dan Sales Invoice dibentuk. |
| Payer | Penjamin | Pasien, BPJS, asuransi, perusahaan, atau pihak lain yang diharapkan menanggung charge obat. |
| Financial Charge | Tagihan Finansial | Konsekuensi finansial yang diberikan kepada Tata Rekening dari Medication Sale. |
| Purchase Confirmation | Konfirmasi Pembelian | Keputusan lisan Pasien Umum untuk melanjutkan setelah Staf Apotek menyampaikan nilai yang dihitung sebelum Sales Invoice dibentuk. Aktivitas workflow ini tidak disimpan sebagai business object atau transaksi terpisah. |
| General Patient | Pasien Umum | Pasien yang Medication Sale-nya mewajibkan Purchase Confirmation dan pembayaran Pasien sebelum Medication Preparation. |
| BPJS Patient | Pasien BPJS | Pasien yang Medication Sale-nya ditanggung berdasarkan kebijakan BPJS tanpa Purchase Confirmation atau pembayaran Pasien. |
| Payment Clearance | Izin Pembayaran | Evidence bahwa persyaratan pembayaran yang diperlukan telah terpenuhi. |
| Coverage Clearance | Izin Penjamin | Evidence bahwa payer yang berlaku mengotorisasi fulfillment tanpa pembayaran langsung Pasien. Untuk fulfillment BPJS Rawat Jalan, evidence ini menggabungkan SEP yang valid bagi encounter dengan coverage item-level berdasarkan mapping Fornas authoritative. |
| Fulfillment Clearance | Izin Pemenuhan | Otorisasi bisnis yang mengizinkan Dispense Order berjalan berdasarkan kebijakan payment atau coverage yang berlaku. |
| Financial Adjustment | Penyesuaian Finansial | Koreksi accountable terhadap Medication Sale atau konsekuensi finansialnya. |
| Credit Note | Nota Kredit | Dokumen komersial yang mengurangi atau membalik nilai Sales Invoice yang telah diterbitkan. |
| Refund | Pengembalian Dana | Pengembalian dana yang sebelumnya telah diselesaikan secara accountable. |
| Dispense Order | Perintah Dispensing | Instruksi authoritative untuk memenuhi secara fisik satu atau lebih Fulfillment Allocation dari satu Sales Order. |
| Dispense Order Line | Baris Perintah Dispensing | Satu obat dan jumlah yang dialokasikan untuk dipenuhi secara fisik dalam Dispense Order. |
| Dispense Cycle | Siklus Dispensing | Periode atau batch fulfillment yang ditentukan, terutama untuk Rawat Inap dan Unit Dose Dispensing. |
| Unit Dose Dispensing | Dispensing Dosis Unit | Pemenuhan dalam dosis unit patient-specific atau periode pemberian yang ditentukan. |
| Stock Availability | Ketersediaan Stok | Representasi Inventory mengenai jumlah yang tersedia saat ini untuk mendukung fulfillment. |
| Stock Allocation | Alokasi Stok | Pengaitan stok oleh Inventory kepada kebutuhan fulfillment sebelum issue final. |
| Stock Reservation | Reservasi Stok | Stok yang diamankan untuk Dispense Order agar tidak dijanjikan kepada permintaan lain. |
| Inventory Issue | Pengeluaran Stok | Pengakuan authoritative Inventory bahwa obat telah keluar dari lokasi persediaan. |
| Medication Preparation | Penyiapan Obat | Pengambilan, penghitungan, pelabelan, pengemasan, dan penyiapan obat lainnya untuk fulfillment. |
| Compounding | Peracikan Obat | Penyiapan produk obat dari bahan atau komponen untuk kebutuhan fulfillment tertentu. |
| Final Dispense Review | Telaah Obat | Telaah profesional final oleh Pharmacist dengan Pasien atau caregiver hadir setelah pickup call dan sebelum Medication Handover. |
| Final Dispense Review Record | Catatan Telaah Obat | Catatan immutable untuk satu percobaan Final Dispense Review pada Dispense Order, termasuk hasil lulus atau gagal, alasan kegagalan, Pharmacist penanggung jawab, waktu bisnis efektif, dan jumlah terdampak. Satu Dispense Order dapat memiliki beberapa catatan review. |
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
| Salinan Resep | Salinan Resep | Catatan accountable mengenai obat atau jumlah dalam Resep yang tidak dipenuhi, bila berlaku. |
| Dispense Cancellation | Pembatalan Dispensing | Pengakhiran accountable suatu Dispense Order sebelum fulfillment berhasil. |
| Fulfillment Expiry | Berakhirnya Pemenuhan | Berakhirnya kesempatan fulfillment karena periode layanan yang diizinkan telah lewat. |
| Medication Return | Retur Obat | Pengembalian accountable atas obat yang sebelumnya disiapkan, dipindahkan, atau diserahkan. |
| Return to Stock | Pengembalian ke Stok | Penerimaan authoritative oleh Inventory atas obat retur yang eligible menjadi stok tersedia. |
| No-Show | Pasien Tidak Hadir | Outcome Rawat Jalan ketika Pasien tidak mengambil obat dalam batas layanan yang berlaku. |
| Pharmacy Queue Entry | Entri Antrean Apotek | Partisipasi Pasien dalam antrean apotek Rawat Jalan yang identitas dan lifecycle-nya dimiliki Patient Tracker. |
| Outpatient Queue Mapping | Outpatient Queue Mapping | Mapping antara Entri Antrian Apotek dengan Resep, Permintaan Obat Langsung, Sales Order, atau sumber pelayanan obat lain yang sesuai. |
| Tracker Mapping | Tracker Mapping | Outpatient Queue Mapping yang dibuat otomatis ketika bukti dari Sistem Antrian Pasien atau registrasi menemukan satu atau beberapa Resep yang sudah ada. Proses ini tidak membuat Resep dan tidak berlaku untuk Permintaan Obat Langsung. |
| Manual Mapping | Manual Mapping | Outpatient Queue Mapping yang dibuat oleh Staf Apotek setelah Nomor Antrian dan sumber pelayanan obat yang sesuai diidentifikasi. |
| Pharmacy Service Start Evidence | Bukti Mulai Layanan Apotek | Fakta `Medication Preparation Started` yang menyebabkan Pharmacy Queue Entry Rawat Jalan mencatat `ServedAt`. |
| Care Setting | Setting Pelayanan | Setting operasional yang menentukan kebijakan fulfillment, seperti Rawat Jalan, Rawat Inap, atau IGD. |
| Outpatient Fulfillment | Pelayanan Obat Rawat Jalan | Apotek berdasarkan kebijakan kedatangan, antrean, pembayaran, pickup, dan no-show Rawat Jalan. |
| Inpatient Fulfillment | Pelayanan Obat Rawat Inap | Apotek berdasarkan kebijakan bangsal, supply terjadwal, dan retur Rawat Inap. |
| Emergency Fulfillment | Pelayanan Obat IGD | Apotek berdasarkan kebijakan urgent care yang berlaku. |
| Dose Window | Jendela Dosis | Periode pemberian yang ditentukan untuk merencanakan Dispense Cycle tanpa merepresentasikan Medication Administration. |
| Ward Delivery | Pengiriman ke Bangsal | Medication Handover dari Farmasi kepada Authorized Recipient di bangsal Rawat Inap. |

## 3. Kapabilitas Bisnis

### 3.1 Medication Demand Acceptance

**Indonesia:** Penerimaan Permintaan Obat

Menerima permintaan dari Resep yang telah ditelaah atau Direct Medication Request yang diizinkan tanpa mengubah intent klinis asli.

### 3.2 Telaah Resep

**Indonesia:** Telaah Resep

Menetapkan disposition profesional setiap Resep dan barisnya, termasuk klarifikasi dan penerimaan sebagian.

### 3.3 Sales Order Management

**Indonesia:** Sales Order Management

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

### 3.11 Outpatient Queue Coordination

**Indonesia:** Koordinasi Antrean Rawat Jalan

melakukan mapping antara Pharmacy Queue Entry yang dimiliki context eksternal dengan sumber pelayanan obat yang sesuai melalui Tracker Mapping atau Manual Mapping tanpa menjadikan kedatangan dalam antrean sebagai prasyarat Telaah Resep.

### 3.12 Outpatient Payer, Pickup, and No-Show Resolution

**Indonesia:** Penyelesaian Payer, Pengambilan, dan No-Show Rawat Jalan

Menerapkan kebijakan clearance, timing invoice, pickup, handover, dan no-show untuk Pasien Umum dan BPJS dengan tetap memisahkan outcome komersial dan fulfillment.

## 4. Aktor & Peran

### 4.1 Dokter Penulis Resep

Memiliki Resep asli. Klarifikasi dengan Pharmacist berlangsung di luar sistem dan tidak mengubah Resep asli. Dokter Penulis Resep tidak memiliki penerimaan oleh Farmasi, pembentukan Sales Invoice, atau pelaksanaan dispensing.

### 4.2 Pharmacist

Memiliki Hasil Telaah Resep, Medication Substitution yang diotorisasi selama Telaah Resep, Final Dispense Review, verifikasi Authorized Recipient, dan Patient Education yang berlaku. Pharmacist tidak memiliki pemanggilan administratif antrean Rawat Jalan. Authority Medication Substitution berakhir ketika Sales Order dibentuk.

### 4.3 Staf Apotek

Mengoordinasikan permintaan yang diterima, alokasi Sales Order, interaksi administratif Rawat Jalan, penyiapan atau peracikan obat, dan penyerahan sesuai kewenangan. Untuk pelayanan Rawat Jalan, Staf Apotek memanggil Nomor Antrian, membuat Manual Mapping, menyampaikan nilai Pasien Umum sebelum faktur dibentuk, menyimpan faktur yang telah dikonfirmasi, menyiapkan obat sesuai Perintah Penyiapan Obat, dan melakukan panggilan pengambilan. Untuk Permintaan Obat Langsung, Staf Apotek menerima sesuai kewenangan, meminta persetujuan Apoteker ketika diperlukan, atau menolak tanpa membentuk permintaan maupun pesanan penjualan. Jika stok tidak mendukung pelayanan setelah pesanan penjualan dibentuk, Staf Apotek memilih pesanan tertunda atau sumber stok lain yang disetujui untuk obat yang sama sesuai kewenangan. Staf Apotek tidak boleh mengganti jenis obat.

### 4.4 Patient or Caregiver

Memberikan confirmation dan payment yang berlaku, menerima edukasi, dan menerima obat ketika bertindak sebagai Authorized Recipient.

### 4.5 Cashier

Menerima pembayaran dan memberikan evidence Payment Clearance. Cashier tidak menetapkan eligibility obat atau fulfillment quantity.

### 4.6 Inpatient Authorized Recipient

Menerima Ward Delivery untuk Pasien dan tetap teridentifikasi dalam outcome Medication Handover. Penerimaan tersebut tidak merepresentasikan Medication Administration.

### 4.7 Pharmacy Supervisor

Memiliki keputusan exceptional di luar authority biasa, termasuk expiry, return, shortage resolution, dan koreksi accountable yang disetujui.

## 5. Domain Objects

### 5.1 Telaah Resep

Merepresentasikan proses penilaian profesional Farmasi terhadap satu Resep. Object ini mempertahankan keputusan per baris, Pharmacist yang bertanggung jawab, dan Source Traceability tanpa menulis ulang Resep. Hasil obat yang diterima diwujudkan pada Sales Order.

### 5.2 Sales Order

Merepresentasikan satu permintaan obat patient-specific yang diterima. Object ini memiliki Sales Order Line, Accepted Quantity, Billing Allocation, Fulfillment Allocation, dan resolution progress.

### 5.3 Billing Allocation

Merepresentasikan bagian Sales Order Line yang dialokasikan kepada satu Medication Sale dan Sales Invoice. Object ini mempertahankan dasar jumlah atau nilai yang berlaku serta referensi Pricing Snapshot.

### 5.4 Fulfillment Allocation

Merepresentasikan bagian Sales Order Line yang dialokasikan kepada satu Dispense Order. Object ini mempertahankan jumlah, Care Setting, dan Dispense Cycle yang berlaku.

### 5.5 Sales Invoice

Merepresentasikan satu Medication Sale dari satu Sales Order. Object ini memiliki Billing Line, klasifikasi payer, nilai komersial, financial disposition, adjustment, dan outcome Financial Charge.

### 5.6 Dispense Order

Merepresentasikan satu instruksi physical fulfillment dari satu Sales Order. Object ini memiliki Dispense Order Line, progress penyiapan dan review, outcome Medication Dispense, dan final fulfillment disposition.

### 5.7 Fulfillment Clearance

Menghubungkan jumlah yang diizinkan dari satu Dispense Order dengan Payment Clearance, Coverage Clearance, atau commercial evidence lain yang disetujui. Object ini dapat mereferensikan beberapa Sales Invoice bila diwajibkan allocation policy.

### 5.8 Medication Dispense

Merepresentasikan obat dan jumlah aktual yang disediakan untuk Pasien, termasuk responsible party, effective time, dan Dispense Order sumber.

### 5.9 Medication Handover

Merepresentasikan pemindahan kepada Authorized Recipient, termasuk verifikasi penerima, handover time, destination bila berlaku, dan tanggung jawab Patient Education.

### 5.10 Outpatient Queue Mapping

Merepresentasikan mapping aktif antara Pharmacy Queue Entry yang dimiliki context eksternal dan sumber pelayanan obat yang sesuai. Mapping ini dapat diperbarui langsung bila sumber yang dipilih salah; riwayat perubahannya tidak perlu disimpan. Object ini mencatat metode mapping saat ini tanpa memiliki Queue Number atau lifecycle antrean.

### 5.11 Unfulfilled Medication Outcome

Merepresentasikan alasan final suatu Accepted Quantity tidak dipenuhi dan mengidentifikasi Salinan Resep, penutupan backorder, return, atau financial correction yang diperlukan.

### 5.12 Final Dispense Review Record

Merepresentasikan satu percobaan Final Dispense Review yang immutable dan dimiliki sebagai detail dari satu Dispense Order. Catatan review ditambahkan dan tidak diganti agar setiap kegagalan dan kelulusan review tetap accountable sesuai urutannya.

## 6. Aggregates

### 6.1 Telaah Resep Aggregate

**Aggregate Root:** `Telaah Resep`
Aggregate menjaga referensi sumber Resep, professional disposition per baris, Pharmacist yang bertanggung jawab, dan completion outcome tetap konsisten. Aggregate tidak menyimpan komunikasi klarifikasi dan tidak dapat mengubah Resep asli milik klinisi.

### 6.2 Sales Order Aggregate
**Aggregate Root:** `Sales Order`

Aggregate memiliki Sales Order Line, Billing Allocation, Fulfillment Allocation, Accepted Quantity, Fulfilled Quantity, unfulfilled outcome, dan overall resolution.

Aggregate memastikan alokasi komersial dan fulfillment tetap dapat ditelusuri serta tidak melebihi authority Sales Order Line yang berlaku. Aggregate tidak memiliki payment settlement Sales Invoice, saldo Inventory, atau pelaksanaan physical dispensing.

### 6.3 Medication Sale Aggregate

**Aggregate Root:** `Sales Invoice`

Aggregate merepresentasikan satu Medication Sale. Aggregate menjaga Billing Line, Pricing Snapshot, Payer, financial disposition, Credit Note, refund, dan outcome Financial Charge tetap konsisten. Purchase Confirmation lisan Pasien Umum dibuktikan oleh pembentukan Sales Invoice yang accountable dan tidak disimpan sebagai object terpisah.

Sales Invoice mereferensikan tepat satu Sales Order tetapi dapat mencakup satu atau lebih Sales Order Line miliknya.

### 6.4 Dispense Order Aggregate

**Aggregate Root:** `Dispense Order`

Aggregate menjaga Fulfillment Allocation, physical preparation, kumpulan one-to-many Final Dispense Review Record yang immutable, Medication Dispense, Medication Handover, cancellation, expiry, return, dan non-fulfillment outcome tetap konsisten.

Dispense Order mereferensikan tepat satu Sales Order tetapi dapat memenuhi satu atau lebih Sales Order Line miliknya.

### 6.5 Relasi lintas aggregate

Satu Sales Order dapat memiliki nol atau lebih Sales Invoice dan nol atau lebih Dispense Order. Sales Invoice dan Dispense Order tidak diwajibkan memiliki jumlah atau waktu pembentukan yang sama.

Korelasi bisnisnya dinyatakan melalui Billing Allocation, Fulfillment Allocation, dan Fulfillment Clearance pada tingkat Sales Order Line dan jumlah. Penggunaan Sales Order yang sama tidak dengan sendirinya menetapkan bahwa setiap Sales Invoice memberikan clearance kepada setiap Dispense Order.

Outpatient Queue Mapping merupakan mapping aktif kepada Pharmacy Queue Entry yang dimiliki context eksternal, bukan Aggregate Root Apotek dan bukan catatan transaksi Perubahan mapping. Patient Tracker tetap authoritative atas Queue Session, Queue Number, dan lifecycle antrean.

## 7. Aturan Bisnis

### 7.1 Sumber dan penerimaan profesional

- **BR-APT-001** — Patient Medication Demand harus berasal dari tepat satu Resep atau Direct Medication Request.
- **BR-APT-002** — Resep asli dan intent klinisi harus tetap dimiliki oleh authority clinical order terkait.
- **BR-APT-003** — Setiap Resep harus menyelesaikan Telaah Resep sebelum barisnya masuk ke Sales Order.
- **BR-APT-004** — Hanya Pharmacist yang boleh menetapkan keputusan akhir setiap Baris Resep sebagai diterima sesuai resep, diterima dengan obat pengganti, atau ditolak. Resep asli tidak boleh diubah; obat yang diterima dicatat pada Sales Order Line.
- **BR-APT-005** — Hasil Telaah Resep harus mencatat keputusan akhir untuk setiap Baris Resep yang ditelaah. Klarifikasi kepada Dokter Penulis Resep berlangsung di luar sistem, tidak dicatat sebagai status atau transaksi, dan telaah tetap `Under Review` sampai keputusan dibuat.
- **BR-APT-006** — Resep yang rejected tidak boleh membentuk Sales Order.
- **BR-APT-007** — Resep yang partially approved dapat membentuk Sales Order yang hanya berisi Accepted Medication Line.
- **BR-APT-008** — Penerimaan klinis harus independen dari Stock Availability saat itu; fakta stok tidak boleh menulis ulang professional eligibility.
- **BR-APT-009** — Direct Medication Request harus mengikuti kebijakan penerimaan profesional dan organisasi yang berlaku tanpa membentuk Resep.

### 7.2 Sales Order

- **BR-APT-010** — Sales Order harus berasal dari tepat satu sumber accepted demand yang telah selesai.
- **BR-APT-011** — Satu Resep harus membentuk maksimal satu Sales Order aktif dalam satu fulfillment episode.
- **BR-APT-012** — Sales Order harus memiliki minimal satu Sales Order Line dengan Accepted Quantity positif.
- **BR-APT-013** — Setiap Sales Order Line harus mempertahankan Source Traceability ke Baris Resep atau baris Direct Medication Request sumbernya; untuk obat pengganti, Sales Order Line memuat obat pengganti sementara referensi sumber tetap menunjuk Baris Resep asli.
- **BR-APT-014** — Sales Order bukan Sales Invoice, catatan pembayaran, Stock Reservation, Dispense Order, atau evidence Medication Dispense.
- **BR-APT-015** — Billing Allocation dan Fulfillment Allocation dapat terjadi secara independen pada waktu bisnis yang berbeda.
- **BR-APT-016** — Fulfillment Allocation aktif suatu Sales Order Line tidak boleh melebihi Accepted Quantity yang belum terselesaikan.
- **BR-APT-017** — Fulfilled Quantity tidak boleh melebihi jumlah yang dialokasikan untuk fulfillment.
- **BR-APT-018** — Setiap Accepted Quantity pada akhirnya harus fulfilled, cancelled, expired, backordered, atau memperoleh Unfulfilled Medication Outcome lain yang accountable.
- **BR-APT-019** — Sales Order hanya boleh mencapai Fulfillment Completion ketika setiap Accepted Quantity memiliki outcome final yang accountable.

### 7.3 Medication Sale dan Sales Invoice

- **BR-APT-020** — Setiap Medication Sale harus direpresentasikan oleh tepat satu Sales Invoice.
- **BR-APT-021** — Setiap Sales Invoice harus berasal dari Billing Allocation milik tepat satu Sales Order.
- **BR-APT-022** — Satu Sales Order dapat menghasilkan nol, satu, atau beberapa Sales Invoice.
- **BR-APT-023** — Sales Invoice dapat mencakup satu atau lebih Sales Order Line dan harus mempertahankan setiap source allocation.
- **BR-APT-024** — Billing Line tidak boleh memperkenalkan baris obat yang tidak ada pada Sales Order sumbernya, kecuali komponen komersial non-obat yang diotorisasi secara eksplisit.
- **BR-APT-025** — Sales Invoice harus mempertahankan Pricing Snapshot dan Payer yang berlaku saat dibentuk.
- **BR-APT-026** — Pembentukan Sales Invoice tidak membuktikan bahwa stok tersedia, direservasi, disiapkan, didispensing, atau diserahkan.
- **BR-APT-027** — Sales Invoice yang issued atau financially settled harus dikoreksi melalui Financial Adjustment, Credit Note, atau outcome Refund yang accountable, bukan penggantian diam-diam.
- **BR-APT-028** — Setiap Financial Charge yang dikirim ke Tata Rekening harus mempertahankan Source Traceability ke Sales Invoice dan Sales Order sumbernya.

### 7.4 Dispense Order dan dispensing

- **BR-APT-029** — Setiap Dispense Order harus berasal dari Fulfillment Allocation milik tepat satu Sales Order.
- **BR-APT-030** — Satu Sales Order dapat menghasilkan nol, satu, atau beberapa Dispense Order.
- **BR-APT-031** — Dispense Order dapat mencakup satu atau lebih Sales Order Line dan harus mempertahankan setiap source allocation.
- **BR-APT-032** — Jumlah, pembagian quantity, dan timing Dispense Order dapat berbeda dari jumlah, pembagian nilai, dan timing Sales Invoice.
- **BR-APT-033** — Stock Reservation dan Inventory Issue harus tetap menjadi outcome authoritative milik Inventory yang diminta untuk Dispense Order.
- **BR-APT-034** — Medication Preparation dan Compounding harus menggunakan Dispense Order aktif sebagai authority.
- **BR-APT-035** — Prepared Medication harus menyelesaikan Final Dispense Review sebelum Medication Handover.
- **BR-APT-036** — Medication Dispense tidak boleh melebihi unresolved quantity milik Dispense Order Line.
- **BR-APT-037** — Medication Handover harus mengidentifikasi Authorized Recipient dan effective business time.
- **BR-APT-038** — Ward Delivery tidak boleh diperlakukan sebagai Medication Administration.
- **BR-APT-039** — Medication Administration tidak boleh disimpulkan dari Sales Invoice, Inventory Issue, Medication Dispense, atau Ward Delivery.
- **BR-APT-096** — Setiap percobaan Final Dispense Review harus menambahkan Final Dispense Review Record yang immutable pada Dispense Order. Review yang gagal harus mencatat alasan, Pharmacist penanggung jawab, waktu bisnis efektif, dan jumlah terdampak, mengembalikan Dispense Order dari `Prepared` ke `Preparing`, serta melarang Medication Handover. Setelah koreksi selesai, Dispense Order harus kembali ke `Prepared` dan menjalani Final Dispense Review baru; hanya catatan review terbaru dengan hasil lulus yang dapat mengubahnya menjadi `Reviewed` dan mengizinkan Medication Handover.

### 7.5 Clearance dan koordinasi lintas aggregate

- **BR-APT-040** — Dispense Order hanya boleh berjalan ketika Fulfillment Clearance yang diwajibkan oleh kebijakan Care Setting dan Payer tersedia.
- **BR-APT-041** — Payment Clearance harus berasal dari payment authority yang bertanggung jawab dan tidak boleh disimpulkan hanya dari keberadaan Sales Invoice.
- **BR-APT-042** — Coverage Clearance harus mengidentifikasi Payer dan authority covered fulfillment yang berlaku.
- **BR-APT-043** — Fulfillment Clearance harus mengidentifikasi jumlah Dispense Order yang diotorisasi dan supporting commercial evidence.
- **BR-APT-044** — Satu Sales Invoice dapat mendukung clearance beberapa Dispense Order, dan satu Dispense Order dapat bergantung pada beberapa commercial allocation ketika diwajibkan kebijakan.
- **BR-APT-045** — Sales Invoice yang paid atau financially cleared tidak menjamin fulfillment berhasil ketika terjadi shortage, discrepancy, expiry, atau exception sah lainnya.
- **BR-APT-046** — Financial clearance yang diikuti non-fulfillment harus menghasilkan Backorder, fulfillment dari sumber stok lain yang disetujui untuk produk obat yang sama, Credit Note, Refund, atau resolution lain yang disetujui dan accountable. Kondisi tersebut tidak boleh mensubstitusi Sales Order Line setelah Sales Order dibentuk.

### 7.6 Partial fulfillment, UDD, dan exception

- **BR-APT-047** — Partial Fulfillment harus mempertahankan jumlah fulfilled, unresolved, dan unfulfilled secara terpisah.
- **BR-APT-048** — Unit Dose Dispensing dapat membagi satu Sales Order Line menjadi beberapa Dispense Cycle dan Dispense Order.
- **BR-APT-049** — Dose Window harus memandu perencanaan fulfillment dan tidak boleh menyatakan Medication Administration.
- **BR-APT-050** — Untuk obat pengganti yang diterima, Sales Order Line harus mencatat obat pengganti, Pharmacist yang bertanggung jawab, alasan, dan jumlah yang terpengaruh serta tetap mereferensikan Baris Resep asli. Identitas obat pada Sales Order Line yang sudah dibentuk tidak boleh diubah; kebutuhan penggantian berikutnya ditangani dengan membatalkan item atau pesanan yang terdampak, menelaah kembali Resep asli, dan membentuk Sales Order Line baru tanpa mensyaratkan Resep perbaikan atau pengganti.
- **BR-APT-051** — Medication Shortage atau Stock Discrepancy tidak boleh mengubah Resep asli atau menghapus Sales Invoice yang sudah ada.
- **BR-APT-052** — Medication Return harus mengidentifikasi Dispense Order sumber, jumlah, alasan, dan disposition Inventory finalnya.
- **BR-APT-053** — Return to Stock hanya boleh terjadi ketika Inventory menerima obat retur berdasarkan kebijakannya sendiri.
- **BR-APT-054** — Salinan Resep harus mengidentifikasi obat atau jumlah dalam Resep yang tetap tidak terpenuhi.
- **BR-APT-055** — No-Show harus menjadi outcome kebijakan Rawat Jalan dan tidak boleh diterapkan pada Ward Delivery Rawat Inap.

### 7.7 Completion dan history

- **BR-APT-056** — Commercial progress dan fulfillment progress Sales Order harus dilacak secara independen.
- **BR-APT-057** — Commercial resolution tidak dengan sendirinya menyelesaikan physical fulfillment, dan physical fulfillment tidak dengan sendirinya membuktikan financial resolution.
- **BR-APT-058** — Keputusan material mengenai review, allocation, invoice, clearance, dispensing, handover, exception, dan correction harus mempertahankan responsible party dan effective business time.
- **BR-APT-059** — Source Traceability harus dipertahankan dari Resep atau Direct Medication Request melalui Sales Order, Sales Invoice, Dispense Order, dan final outcome.
- **BR-APT-060** — Hasil bisnis yang sudah selesai atau dibatalkan tidak boleh dihapus; koreksi setelahnya harus menambahkan fakta koreksi yang dapat dipertanggungjawabkan. Aturan ini tidak berlaku untuk koreksi Outpatient Queue Mapping yang masih aktif; mapping tersebut diperbarui langsung sesuai `BR-APT-062`.

### 7.8 Kebijakan workflow Rawat Jalan

- **BR-APT-061** — Pharmacist dapat melakukan Telaah Resep segera setelah Resep tersedia; kedatangan Pasien dan Outpatient Queue Mapping tidak boleh menjadi prasyarat.
- **BR-APT-062** — Outpatient Queue Mapping harus merepresentasikan mapping catatan bisnis yang sudah ada dan tidak boleh membentuk atau mengubah Resep, Hasil Telaah Resep, atau Sales Order. Jika mapping salah, sistem harus memperbarui mapping aktif ke sumber yang benar tanpa mewajibkan riwayat perubahan mapping.
- **BR-APT-063** — Tracker Mapping harus digunakan ketika bukti tracker atau registrasi yang sah menemukan satu atau beberapa Resep yang sudah ada dan berlaku. Proses ini tidak boleh membuat Resep atau menemukan Permintaan Obat Langsung; jika gagal, proses harus beralih ke Manual Mapping.
- **BR-APT-064** — Pharmacy Queue Entry yang diterbitkan langsung atau belum teridentifikasi harus tetap unmapped sampai Staf Apotek mengidentifikasi dan melakukan mapping sumber pelayanan obat yang sesuai.
- **BR-APT-065** — Staf Apotek harus memiliki pemanggilan administratif antrean dan pickup; tanggung jawab tersebut tidak boleh dipindahkan kepada Pharmacist.
- **BR-APT-066** — Dalam alur normal Resep Elektronik BPJS dengan Tracker Mapping berhasil, Pasien hanya memerlukan satu pemanggilan apotek Rawat Jalan, yaitu pickup call setelah setiap Dispense Order yang berlaku mencapai `Prepared`.
- **BR-APT-067** — Outpatient Queue Mapping dan Purchase Confirmation Pasien Umum dapat diselesaikan dalam satu interaksi loket ketika Billing Allocation dan nilai yang dihitung telah tersedia. Ketika Tracker Mapping selesai tanpa Pasien hadir di loket, Staf Apotek harus memanggil Queue Number untuk interaksi Purchase Confirmation sebelum Sales Invoice dibentuk; administrative call ini tidak menetapkan `ServedAt` atau `DoneAt`.
- **BR-APT-068** — Dispense Order Rawat Jalan dan Stock Reservation-nya dapat dibentuk sebelum kedatangan Pasien atau Outpatient Queue Mapping, tetapi Medication Preparation tetap harus mengikuti kebijakan Fulfillment Clearance yang berlaku.
- **BR-APT-069** — Obat yang disiapkan untuk pickup Rawat Jalan harus tetap menjadi In-Transit Medication sampai Medication Handover yang accountable atau disposition return.
- **BR-APT-070** — Sebelum Sales Invoice Pasien Umum tersedia, Staf Apotek harus menyampaikan nilai yang dihitung dari Billing Allocation dan Pricing Snapshot yang berlaku serta memperoleh Purchase Confirmation lisan. Penyimpanan transaksi yang telah dikonfirmasi membentuk Sales Invoice dari Billing Allocation tersebut; object atau transaksi Purchase Confirmation terpisah tidak disimpan.
- **BR-APT-071** — Ketika Pasien Umum menolak Purchase Confirmation sebelum transaksi disimpan, Sales Invoice tidak boleh dibentuk dan Stock Reservation yang tidak digunakan harus dilepas melalui Inventory. Sales Invoice yang dibentuk setelah konfirmasi hanya dapat dibatalkan selama lifecycle-nya mengizinkan; konsekuensi issued atau financially cleared harus mengikuti `BR-APT-027`.
- **BR-APT-072** — Medication Preparation Pasien Umum tidak boleh dimulai sebelum Payment Clearance membentuk Fulfillment Clearance yang diwajibkan.
- **BR-APT-073** — Pasien BPJS tidak boleh diminta melakukan Purchase Confirmation atau pembayaran Pasien; jumlah Patient-payable harus nol dan payment disposition harus `Not Required`, sedangkan nilai gross atau covered dapat tetap bukan nol.
- **BR-APT-074** — Medication Preparation BPJS dapat dimulai ketika Outpatient Queue Mapping, Dispense Order yang berlaku, Coverage Clearance, dan Fulfillment Clearance tersedia; keberadaan Sales Invoice tidak boleh menjadi prasyarat.
- **BR-APT-075** — Untuk kebijakan BPJS Rawat Jalan saat ini, Sales Invoice hanya boleh dibentuk sebagai bagian dari Medication Handover yang berhasil dikonfirmasi; pembentukan Sales Invoice dan penyelesaian handover harus menjadi satu outcome bisnis accountable.
- **BR-APT-076** — Staf Apotek harus memanggil Pasien untuk pickup Rawat Jalan setelah setiap Dispense Order dalam coordinated pickup mencapai `Prepared`. Pharmacist kemudian melakukan Final Dispense Review dengan Pasien atau caregiver hadir sebelum Medication Handover.
- **BR-APT-077** — Dalam interaksi loket yang sama setelah pickup call, Pharmacist harus memverifikasi Authorized Recipient, menyelesaikan Final Dispense Review, memberikan Patient Education yang berlaku, dan baru kemudian menyelesaikan Medication Handover Rawat Jalan.
- **BR-APT-078** — Medication Handover Rawat Jalan yang berhasil harus menyelesaikan jumlah Dispense Order yang berlaku dan meminta outcome Inventory Issue authoritative terkait.
- **BR-APT-079** — No-Show BPJS sebelum Medication Handover tidak boleh membentuk atau membatalkan Sales Invoice. Penyelesaian manual obat tidak diambil yang diotorisasi harus membuat Dispense Order terkait `Expired`, meminta disposition return Inventory yang accountable, dan hanya mengizinkan Sales Order menjadi `Resolved` dengan alasan `Collection Window Expired` setelah setiap Accepted Quantity dan konsekuensi komersial memiliki outcome final.
- **BR-APT-080** — No-Show Pasien Umum setelah pembayaran harus menggunakan penyelesaian manual obat tidak diambil yang sama untuk konsekuensi fulfillment, tetapi Sales Order harus tetap `Active` sampai Tata Rekening atau financial authority yang bertanggung jawab memberikan Credit Note, Refund, atau outcome komersial final lain yang accountable.
- **BR-APT-081** — `Medication Preparation Started` harus menjadi Pharmacy Service Start Evidence bagi setiap jalur payer Rawat Jalan dan menyebabkan Patient Tracker mencatat `ServedAt`. Pembentukan Sales Invoice dan Purchase Confirmation tidak boleh menetapkan `ServedAt` apotek Rawat Jalan.
- **BR-APT-082** — Patient Tracker harus tetap authoritative atas identitas Pharmacy Queue Entry, Queue Number, dan lifecycle antrean meskipun Apotek memiliki Outpatient Queue Mapping dan tujuan pemanggilan.
- **BR-APT-083** — User tidak boleh menginput Legacy DU atau baris Sales Invoice obat independen secara manual; tindakan user hanya dapat memicu pembentukan Sales Invoice dari Billing Allocation yang accountable.
- **BR-APT-084** — Satu Pharmacy Queue Entry dapat dimappingkan ke satu atau beberapa Resep atau Direct Medication Request. Setiap demand yang dimappingkan harus mempertahankan Telaah Resep bila berlaku, Sales Order, alokasi Sales Invoice, Dispense Order, dan lifecycle accountable masing-masing.
- **BR-APT-085** — Mapping beberapa medication demand ke satu Pharmacy Queue Entry harus mengoordinasikan satu pelayanan Rawat Jalan dan tidak boleh menggabungkan Sales Order, Sales Invoice, atau Dispense Order masing-masing.
- **BR-APT-086** — Dalam satu normal outpatient fulfillment episode, satu Sales Order aktif harus dikoordinasikan melalui satu Dispense Order aktif. Pharmacy Queue Entry yang sama dapat mengoordinasikan beberapa pasangan Sales Order dan Dispense Order tersebut.
- **BR-APT-087** — Tampilan progress per demand pada antrean harus berupa projection fakta Apotek untuk setiap demand yang dimappingkan; Patient Tracker tidak boleh menjadi authoritative atas state Telaah Resep, Sales Invoice, atau Dispense Order.
- **BR-APT-088** — Satu coordinated pickup call hanya boleh dilakukan setelah setiap Dispense Order yang hendak diserahkan mencapai `Prepared` atau memperoleh outcome exception yang accountable.
- **BR-APT-089** — Direct Medication Request harus diterima Staf Apotek sesuai authority, dirujuk untuk persetujuan Pharmacist ketika diwajibkan, atau ditolak. Request yang ditolak tidak boleh membentuk record Direct Medication Request maupun Sales Order.
- **BR-APT-090** — Coverage Clearance BPJS Rawat Jalan harus memerlukan SEP yang valid untuk encounter terkait dan coverage Fornas item-level authoritative untuk jumlah yang diberi clearance.
- **BR-APT-091** — Ketika satu Sales Order memiliki jumlah yang ditanggung BPJS dan dibayar Pasien, Staf Apotek harus membentuk Billing Allocation terpisah untuk tanggung jawab payer tersebut. Alokasi covered membentuk Sales Invoice BPJS dan alokasi Patient-payable membentuk Sales Invoice Pasien Umum terpisah.
- **BR-APT-092** — Dalam mixed-coverage fulfillment, Sales Invoice Pasien Umum hanya boleh dibentuk setelah Purchase Confirmation lisan, sedangkan Sales Invoice BPJS hanya boleh dibentuk bersama Medication Handover yang berhasil berdasarkan `BR-APT-075`.
- **BR-APT-093** — Coordinated pickup call mixed coverage harus menunggu sampai setiap jumlah yang hendak diserahkan memperoleh Coverage Clearance atau Payment Clearance yang berlaku dan Dispense Order-nya mencapai `Prepared`.
- **BR-APT-094** — Jika Pasien menolak bagian non-covered sebelum Sales Invoice-nya dibentuk, alokasi Pasien Umum harus memperoleh outcome declined atau commercially unallocated yang accountable, sedangkan bagian BPJS dapat dilanjutkan secara independen.
- **BR-APT-095** — Patient Tracker harus mencatat `DoneAt` apotek Rawat Jalan ketika Staf Apotek melakukan coordinated pickup call. Selesainya antrean tidak membuktikan Final Dispense Review, Patient Education, Medication Dispense, atau Medication Handover.

## 8. State Machines & Lifecycles

### 8.1 Lifecycle Telaah Resep

```text
Available
  -> Under Review
       -> Approved
       -> Partially Approved
       -> Rejected
```

`Approved`, `Partially Approved`, dan `Rejected` adalah outcome review final. Klarifikasi di luar sistem membiarkan telaah berstatus `Under Review`; klarifikasi tersebut tidak membentuk status atau Domain Event tersendiri.

### 8.2 Lifecycle Sales Order

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

Prepared
  -> Preparing (Final Dispense Review gagal; koreksi diperlukan)
       -> Prepared (koreksi selesai; review ulang diwajibkan)

Established, Awaiting Clearance, Released, Preparing, Prepared, or Reviewed
  -> Cancelled | Expired | Unfulfilled
```

`Completed` membutuhkan Medication Dispense yang accountable dan Medication Handover yang berlaku. `Unfulfilled` membutuhkan alasan serta resolution atas stok yang dialokasikan dan konsekuensi finansialnya. Setiap percobaan Final Dispense Review ditambahkan sebagai detail immutable Dispense Order. Percobaan yang gagal mengembalikan `Prepared` ke `Preparing`; kegagalan tersebut tidak menghapus catatan sebelumnya dan tidak mengizinkan handover. Percobaan berikutnya harus lulus sebelum status menjadi `Reviewed`.

### 8.5 Lifecycle jumlah Apotek

```text
Accepted Quantity
  -> Billing Allocated or Commercially Unallocated
  -> Fulfillment Allocated or Fulfillment Unallocated
  -> Fulfilled | Backordered | Cancelled | Expired | Other Unfulfilled Outcome
```

Cabang billing dan fulfillment berkembang secara independen. Resolution Sales Order final memerlukan rekonsiliasi kedua cabang, bukan jumlah dokumen yang identik.

### 8.6 Relasi Outpatient Queue Mapping

```text
Unmapped
  -> Mapped
       method: Tracker Mapping | Manual Mapping
```

Relasi ini melakukan mapping sumber pelayanan obat dengan Pharmacy Queue Entry yang dimiliki context eksternal. Relasi ini tidak menggantikan lifecycle antrean Patient Tracker dan tidak mengubah state Telaah Resep.

### 8.7 Lifecycle pickup dan handover Rawat Jalan

```text
Prepared
  -> Ready for Pickup
       -> Patient Called
            -> Recipient Verified
                 -> Final Review Completed
                      -> Education Provided
                           -> Handed Over

Prepared, Ready for Pickup, or Patient Called
  -> No-Show
       -> penyelesaian manual obat tidak diambil
            -> Dispense Order Expired
            -> alasan resolution Collection Window Expired
```

Pickup call menyelesaikan antrean Patient Tracker, tetapi tidak menyelesaikan Medication Handover. Final Dispense Review, verifikasi Authorized Recipient, dan Patient Education dilakukan dengan Pasien atau caregiver hadir setelah panggilan tersebut. Konsekuensi komersial yang berlaku bergantung pada payer. Pasien Umum dapat telah memiliki Sales Invoice yang financially cleared, sedangkan kebijakan BPJS saat ini baru membentuk Sales Invoice bersama Medication Handover yang berhasil.

## 9. Domain Events

| Domain Event | Makna bisnis |
|---|---|
| Telaah Resep Started | Pharmacist memulai penilaian profesional atas Resep. |
| Telaah Resep Completed | Setiap baris yang ditelaah memperoleh professional disposition final. |
| Outpatient Queue Mapped | Pharmacy Queue Entry telah di-mapping dengan sumber pelayanan obat yang sesuai. |
| Direct Medication Request Accepted | Permintaan non-resep yang diizinkan telah diterima Farmasi. |
| Sales Order Established | Permintaan obat yang diterima tersedia untuk alokasi komersial dan fulfillment. |
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
| Final Dispense Review Completed | Dengan Pasien atau caregiver hadir setelah pickup call, Prepared Medication lulus pemeriksaan profesional akhir yang diperlukan. |
| Final Dispense Review Failed | Prepared Medication gagal dalam pemeriksaan profesional akhir; catatan review immutable ditambahkan dan Dispense Order kembali dari `Prepared` ke `Preparing` untuk dikoreksi. |
| Patient Called for Pickup | Staf Apotek memanggil Pasien untuk Medication Handover Rawat Jalan. |
| Medication Dispensed | Sejumlah obat disediakan secara accountable untuk Pasien. |
| Medication Handed Over | Obat dipindahkan kepada Authorized Recipient. |
| Outpatient No-Show Recorded | Pasien tidak mengambil obat dalam batas layanan Rawat Jalan yang berlaku. |
| Medication Shortage Identified | Stok tersedia tidak dapat mendukung jumlah fulfillment yang dimaksud. |
| Medication Substitution Authorized | Authority yang accountable menyetujui penggantian obat yang diminta. |
| Dispense Order Backordered | Jumlah unresolved dipertahankan untuk fulfillment kemudian. |
| Dispense Order Cancelled | Keputusan berwenang mengakhiri Dispense Order sebelum completion. |
| Dispense Order Expired | Periode fulfillment yang diizinkan berakhir tanpa completion. |
| Unfulfilled Medication Recorded | Accepted Quantity memperoleh outcome non-fulfillment final. |
| Medication Returned | Obat yang sebelumnya disiapkan atau disediakan telah dikembalikan. |
| Sales Invoice Credited | Credit Note mengurangi atau membalik konsekuensi Sales Invoice. |
| Refund Required | Financial resolution memerlukan pengembalian dana yang telah diselesaikan. |
| Pharmacy Service Started | `Medication Preparation Started` menetapkan evidence `ServedAt` apotek Rawat Jalan. |
| Sales Order Resolved | Setiap Accepted Quantity dan konsekuensi komersial yang diperlukan memperoleh outcome final yang accountable. |

## 10. Workflow Bisnis

Domain ini diterapkan melalui spesifikasi workflow sesuai Care Setting. Aturan domain, Aggregate boundary, state, lifecycle, dan Domain Events dalam dokumen ini tetap authoritative.

| Workflow | Outcome bisnis umum | Artifact workflow canonical |
|---|---|---|
| Outpatient Apotek | Mengoordinasikan pengambilan antrean, penerimaan permintaan obat, clearance payer, dispensing, pickup, handover, dan penyelesaian non-fulfillment Rawat Jalan yang accountable. | [Bahasa Indonesia](./outpatient-apotek-workflow-id.md) · [English](./outpatient-apotek-workflow.md) |

Trigger, urutan, keputusan, alternative, exception, compensation, handoff, dan postcondition terperinci untuk fulfillment Rawat Jalan dimiliki oleh spesifikasi workflow yang direferensikan. Workflow terperinci untuk Care Setting lain memerlukan artifact workflow tersendiri di masa depan.
