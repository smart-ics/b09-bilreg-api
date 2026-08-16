# Domain Apotek — Pelayanan Obat Pasien

**Status artefak:** Pendamping semantik Bahasa Indonesia

**Bounded context:** Apotek (`Pelayanan Obat Pasien`)

**Cakupan versi:** Model bisnis target untuk Rawat Jalan, Rawat Inap, IGD, dan Unit Dose Dispensing

**Sumber canonical English:** [apotek-domain.md](./apotek-domain.md)

**Konteks bisnis terkait:** [CPOE](../../contexts/cpoe/CPOE-DOMAIN-ID.md), [Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), [Tata Rekening](../../contexts/TataRekening/02-domain.md)

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan dan nilai

Apotek mengubah permintaan obat khusus untuk pasien yang telah diterima Farmasi menjadi penagihan komersial dan pemenuhan fisik yang dapat dipertanggungjawabkan. Domain ini memisahkan pengiriman stok dan penagihan yang dahulu tergabung dalam `Trs.DU (DO-Bill)` menjadi lifecycle `Sales Invoice` dan `Dispense Order` yang terpisah, lalu dikoordinasikan oleh `Sales Order`.

Bisnis harus memastikan bahwa:

- Resep asli dari klinisi tetap menjadi rujukan utama dan dapat ditelusuri;
- hanya permintaan obat yang telah diterima secara profesional yang masuk ke Sales Order;
- Sales Invoice dapat dibentuk dari Sales Order Line secara independen dari Dispense Order yang juga dibentuk dari Sales Order Line tersebut;
- penagihan, pembayaran atau penjaminan, ketersediaan stok, penyiapan, dan penyerahan tetap merupakan fakta bisnis yang berbeda;
- penagihan maupun pemenuhan sebagian tetap dapat dipertanggungjawabkan menurut jumlahnya; dan
- setiap Accepted Quantity memiliki hasil pemenuhan atau ketidakpemenuhan yang dapat dipertanggungjawabkan.

### 1.2 Cakupan

Konteks ini mencakup:

1. Telaah Resep dan penerimaan Direct Medication Request;
2. pembentukan Sales Order, penagihan komersial, dan perencanaan pemenuhan;
3. pembentukan Medication Sale dan Sales Invoice;
4. pembentukan Dispense Order dan dispensing fisik;
5. clearance komersial atau penjaminan untuk pemenuhan;
6. Medication Dispense dan Medication Handover; dan
7. kekurangan stok, substitusi, pembatalan, retur, dan hasil ketidakpemenuhan lainnya; Apotek Rawat Jalan tidak mendukung Backorder; serta
8. kebijakan workflow Rawat Jalan yang saat ini ditetapkan untuk antrean, penjamin, pengambilan, dan no-show.

Konteks ini berlaku untuk Rawat Jalan, Rawat Inap, IGD, dan Unit Dose Dispensing.

### 1.3 Batas bisnis

Apotek memiliki Hasil Telaah Resep, Sales Order, Medication Sale yang direpresentasikan oleh Sales Invoice, Dispense Order, Medication Dispense, Medication Handover, dan penyelesaian pemenuhan.

Konteks ini bergantung pada konteks terkait tanpa mengambil alih kewenangannya:

- CPOE atau otoritas instruksi klinis lain memiliki Resep asli dan maksud klinisi;
- Medication Catalog atau otoritas formularium memiliki identitas obat dan kebijakan formularium;
- Inventory memiliki saldo dan mutasi stok yang menjadi rujukan;
- Payment memiliki bukti penerimaan dan penyelesaian pembayaran;
- Tata Rekening memiliki Financial Responsibility tingkat registrasi, alokasi payer, finalization, dan settlement initiation;
- Patient Tracker memiliki identitas dan lifecycle antrean Rawat Jalan; dan
- konteks pelayanan klinis memiliki Medication Administration.

Pengadaan, pengelolaan pemasok, pengisian ulang stok, transfer gudang, akuntansi perusahaan, dan Medication Administration berada di luar konteks ini.

Identitas dan lifecycle antrean Rawat Jalan tetap dimiliki oleh Patient Tracker. `QueueEntry` Patient Tracker adalah satu-satunya identitas antrean apotek Rawat Jalan yang canonical. Identitas antrean Farinv legacy didepresiasi, tidak boleh membuat record antrean aktif untuk interaksi apotek Rawat Jalan baru, dan data antrean Farinv historis bersifat read-only. Model antrean dual-active tidak diizinkan.

Apotek memiliki keputusan bisnis Rawat Jalan untuk mengaitkan Pharmacy Queue Entry dengan sumber pelayanan obat yang sesuai, serta kebijakan penjamin, pengambilan, penyerahan, dan no-show setelah pengaitan tersebut.

Untuk Apotek Rawat Jalan, batas fulfillment adalah Registration Period yang aktif. Resep dapat ditelaah, ditelaah ulang, dan dipenuhi selama Registration asal tetap aktif. Tidak ada konsep Fulfillment Episode terpisah.

Untuk antrean Apotek Rawat Jalan, Patient Tracker mencatat `CreatedAt` ketika Queue Number diterbitkan, `ServedAt` ketika Dispense Order pertama yang berlaku memasuki `Preparing`, dan `DoneAt` ketika penyelesaian antrean terjadi. Penyelesaian antrean dapat dipicu oleh coordinated pickup call atau oleh penyelesaian No Show (`WF-APT-RJ-007`) ketika Queue Entry masih `In Service` karena pickup call belum terjadi. Lifecycle antrean tetap `Waiting` → `In Service` → `Done`. Status antrean baru tidak diperkenalkan, dan state alur kerja farmasi tidak ditambahkan ke Queue Entry. Penanda tersebut hanya menunjukkan kemajuan antrean dan tidak membuktikan Medication Handover. `Done` pada antrean hanya berarti lifecycle layanan antrean telah selesai. Evidence `Apotek-Start` dan `Apotek-Done` Patient Tracker tetap dapat digunakan kembali tetapi harus mereferensikan `QueueEntryId` canonical, bukan identitas antrean Farinv.

### 1.4 Pemisahan bisnis utama

Alur target adalah:

```text
Resep atau Direct Medication Request
  -> Telaah Resep atau penerimaan langsung
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
| Apotek | Pelayanan Obat Pasien | Bounded context yang mengoordinasikan permintaan obat khusus untuk pasien sejak diterima Farmasi sampai penagihan komersial dan penyelesaian pemenuhan fisik. |
| Patient Medication Demand | Permintaan Obat Pasien | Kebutuhan obat khusus untuk pasien yang berasal dari Resep atau Direct Medication Request. |
| Resep | Resep | Maksud klinisi yang menjadi rujukan agar obat disediakan atau diberikan kepada Pasien. |
| Resep Elektronik | Resep Elektronik | Resep yang dibuat dan dikirim melalui authority clinical order elektronik. |
| Resep Fisik | Resep Fisik | Resep non-elektronik yang harus dicatat sebelum ditelaah Farmasi. |
| Direct Medication Request | Permintaan Obat Langsung | Permintaan obat gaya ritel tanpa Resep yang berasal di luar alur perawatan rumah sakit dan dapat diterima atau ditolak Staf Apotek. |
| Baris Resep | Baris Resep | Satu obat, instruksi dosis, dan jumlah yang diminta dalam Resep. |
| Source Traceability | Keterlacakan Sumber | Hubungan yang dapat dipertanggungjawabkan dari Medication Sale dan hasil dispensing kembali ke Sales Order, permintaan yang diterima, dan sumber aslinya. |
| Telaah Resep | Telaah Resep | Penilaian administratif, farmasetik, dan klinis oleh Pharmacist terhadap Resep. |
| Hasil Telaah Resep | Hasil Telaah Resep | Keputusan profesional atas resep: disetujui, disetujui sebagian, atau ditolak. Obat yang diterima diwujudkan sebagai Sales Order Line. |
| Accepted Medication Line | Baris Obat Diterima | Baris obat yang diterima secara profesional untuk dimasukkan ke Sales Order, terlepas dari ketersediaan stok saat itu. |
| Sales Order | Sales Order | Permintaan obat yang telah diterima dan dimiliki Farmasi sebagai sumber bersama Medication Sale dan Dispense Order. |
| Sales Order Line | Sales Order Line | Satu obat, jumlah, instruksi, dan dasar komersial yang berlaku dalam Sales Order. |
| Accepted Quantity | Jumlah Diterima | Jumlah maksimum Sales Order Line yang tersedia untuk ditagihkan, dipenuhi secara fisik, dan diselesaikan secara accountable. |
| Medication Sale | Penjualan Obat | Transaksi komersial yang direpresentasikan oleh satu Sales Invoice dari satu Sales Order. |
| Sales Invoice | Faktur Jual | Dokumen komersial authoritative dan Aggregate Root yang merepresentasikan satu Medication Sale. |
| Legacy DU | DU Legacy | Transaksi legacy `Trs.DU (DO-Bill)` yang menggabungkan penagihan obat dan pengiriman stok; pada target model, faktanya direpresentasikan melalui Sales Invoice dan satu atau lebih Dispense Order yang dikoordinasikan oleh Sales Order serta dapat ditelusuri pada tingkat baris. |
| Sales Invoice Item | Item Faktur Jual | Satu obat, BHP, atau baris katalog lain beserta jumlah, harga, diskon, line-level charge, dan nilai di dalam Sales Invoice. Setiap Sales Invoice Item obat atau BHP berasal dari tepat satu Sales Order Line dan menunjukkan bagian dari baris tersebut yang ditagihkan dalam faktur. |
| BHP | BHP | Item katalog standar yang dapat tampil sebagai baris penjualan. BHP bukan komponen faktur free-form. |
| Line-level Charge | Charge Tingkat Baris | Charge komersial khusus item yang melekat pada baris penjualan, misalnya biaya kemasan atau racikan. |
| Invoice-level Charge | Charge Tingkat Faktur | Penyesuaian komersial seluruh transaksi yang melekat pada Sales Invoice, misalnya pembulatan. |
| Pricing Snapshot | Rekaman Harga Transaksi | Dasar komersial yang tidak dapat diubah dan digunakan ketika Sales Invoice dibentuk. |
| Payer | Penjamin | Pasien, BPJS, asuransi, perusahaan, atau pihak lain yang diharapkan menanggung charge obat. |
| Financial Charge | Tagihan Finansial | Konsekuensi finansial yang diberikan kepada Tata Rekening dari Medication Sale. |
| Purchase Confirmation | Konfirmasi Pembelian | Keputusan lisan Pasien Umum untuk melanjutkan setelah Staf Apotek menyampaikan nilai yang dihitung sebelum Sales Invoice dibentuk. Aktivitas workflow ini tidak disimpan sebagai business object atau transaksi terpisah. |
| General Patient | Pasien Umum | Pasien yang Medication Sale-nya mewajibkan Purchase Confirmation dan pembayaran Pasien sebelum Medication Preparation. |
| BPJS Patient | Pasien BPJS | Pasien yang Medication Sale-nya ditanggung berdasarkan kebijakan BPJS tanpa Purchase Confirmation atau pembayaran Pasien. |
| Payment Clearance | Izin Pembayaran | Bukti bahwa persyaratan pembayaran yang diperlukan telah terpenuhi. |
| Coverage Clearance | Izin Penjamin | Bukti bahwa penjamin yang berlaku mengizinkan pemenuhan tanpa pembayaran langsung dari Pasien. Untuk pemenuhan BPJS Rawat Jalan, bukti ini menggabungkan SEP yang valid untuk encounter dengan penjaminan per item berdasarkan mapping Fornas yang menjadi rujukan. |
| Dispense Authorized | Dispense Authorized | Hasil evaluasi kebijakan yang menunjukkan Medication Preparation dan dispensing boleh dimulai, diturunkan dari evidence keuangan dan/atau coverage. Bukan aggregate, entity, business object yang dipersist, sumber kebenaran, atau transaction boundary. |
| Financial Adjustment | Penyesuaian Finansial | Koreksi accountable terhadap Medication Sale atau konsekuensi finansialnya. |
| Credit Note | Nota Kredit | Dokumen komersial yang mengurangi atau membalik nilai Sales Invoice yang telah diterbitkan. |
| Refund | Pengembalian Dana | Pengembalian dana yang sebelumnya telah diselesaikan secara accountable. |
| Dispense Order | Perintah Dispensing | Instruksi authoritative untuk memenuhi secara fisik satu atau lebih Sales Order Line dari satu Sales Order. |
| Dispense Order Line | Baris Perintah Dispensing | Satu jumlah obat yang harus dipenuhi secara fisik dalam Dispense Order. Baris ini mereferensikan tepat satu Sales Order Line serta memuat Care Setting dan Dispense Cycle yang berlaku. |
| Dispense Cycle | Siklus Dispensing | Periode atau batch fulfillment yang ditentukan, terutama untuk Rawat Inap dan Unit Dose Dispensing. |
| Unit Dose Dispensing | Dispensing Dosis Unit | Pemenuhan dalam dosis unit patient-specific atau periode pemberian yang ditentukan. |
| Stock Availability | Ketersediaan Stok | Representasi Stock Ledger mengenai jumlah yang tersedia saat ini untuk mendukung fulfillment. |
| Pharmacy Unit | Unit Farmasi | Lokasi stok farmasi biasa dari mana obat Rawat Jalan dipindahkan ke Dispensing Temporary Unit. |
| Dispensing Temporary Unit | Unit Sementara Dispensing | Lokasi stok farmasi yang menahan obat dalam custody dispensing aktif setelah Dispensing Started dan sebelum handover atau pengembalian No Show. |
| Pharmacy Reserve | Cadangan Farmasi | Penempatan stok atas arahan Pharmacy untuk Dispense Order, diimplementasikan hanya sebagai Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. Tidak ada kontrak `ReserveStock` terpisah. |
| Stock Mutasi | Mutasi Stok | Transfer accountable Stock Ledger antar lokasi stok tanpa mengubah Receipt Source. |
| Remove Stock | Pengeluaran Stok | Pengeluaran outbound accountable dari suatu lokasi stok, termasuk dari Dispensing Temporary Unit pada Medication Handover. |
| Dispensing Temporary Custody | Custody Sementara Dispensing | Jumlah obat yang ditahan di Dispensing Temporary Unit setelah Dispensing Started dan sebelum Medication Handover atau pengembalian No Show. |
| Medication Preparation | Penyiapan Obat | Pengambilan, penghitungan, pelabelan, pengemasan, dan penyiapan obat lainnya untuk fulfillment. |
| Compounding | Peracikan Obat | Penyiapan produk obat dari bahan atau komponen untuk kebutuhan fulfillment tertentu. |
| Final Dispense Review | Telaah Obat | Telaah profesional final oleh Pharmacist dengan Pasien atau caregiver hadir setelah pickup call dan sebelum Medication Handover. |
| Final Dispense Review Record | Catatan Telaah Obat | Catatan yang tidak dapat diubah untuk satu percobaan Final Dispense Review pada Dispense Order, mencakup hasil lulus atau gagal, alasan kegagalan, Pharmacist penanggung jawab, waktu bisnis efektif, dan jumlah terdampak. Satu Dispense Order dapat memiliki beberapa catatan telaah. |
| Prepared Medication | Obat Siap Diserahkan | Obat yang penyiapan fisiknya selesai dan menunggu telaah akhir atau handover. `Prepared` adalah state Dispense Order, bukan state Inventory. |
| Medication Dispense | Realisasi Dispensing | Fakta accountable bahwa sejumlah obat benar-benar disediakan untuk Pasien. |
| Medication Handover | Penyerahan Obat | Pemindahan obat yang accountable kepada Authorized Recipient. |
| Authorized Recipient | Penerima Berwenang | Pasien, caregiver, practitioner, bangsal, atau pihak lain yang menerima obat pada Medication Handover. Verifikasi penerima adalah tanggung jawab operasional Pharmacist dan tidak ditegakkan oleh sistem. |
| Patient Education | Edukasi Pasien | Konseling obat yang diberikan kepada Pasien atau caregiver sebelum Medication Handover. |
| Patient Education Acknowledgement | Pengakuan Edukasi Pasien | Catatan ringan bahwa Pharmacist mengonfirmasi konseling telah diberikan. Mencatat waktu edukasi dan Pharmacist penanggung jawab. Catatan konseling rinci bersifat opsional. |
| Fulfilled Quantity | Jumlah Terpenuhi | Jumlah Sales Order Line yang mencapai outcome Medication Dispense berhasil. |
| Partial Prescription Fulfillment | Pemenuhan Resep Sebagian | Membentuk Sales Order dari subset baris resep ketika Patient Request, Stock Shortage, atau Fornas Not Covered berlaku. |
| Partial Fulfillment | Pemenuhan Sebagian | Eksekusi fulfillment ketika satu Sales Order dipenuhi melalui beberapa Dispense Order, atau kurang dari total Accepted Quantity Sales Order Line dipenuhi sementara jumlah lain unresolved atau memperoleh outcome berbeda. Ini bukan kebijakan Partial Prescription Fulfillment. |
| Fulfillment Completion | Penyelesaian Pemenuhan | Kondisi ketika setiap Accepted Quantity telah memiliki outcome final yang accountable. |
| Medication Administration | Pemberian Obat kepada Pasien | Fakta klinis bahwa obat benar-benar diberikan kepada atau dikonsumsi Pasien; dimiliki context eksternal. |
| Medication Shortage | Kekurangan Stok Obat | Stok tidak cukup untuk memenuhi jumlah obat yang dialokasikan. |
| Stock Discrepancy | Selisih Stok | Perbedaan antara stok tercatat dan stok fisik yang memengaruhi fulfillment. |
| Backorder | Pemenuhan Tertunda | Jumlah unresolved yang dipertahankan untuk dipenuhi kemudian saat supply tersedia. Apotek Rawat Jalan tidak mendukung Backorder. |
| Medication Substitution | Substitusi Obat | Penggantian accountable atas produk obat yang diminta berdasarkan authority profesional yang berlaku. |
| Unfulfilled Medication Outcome | Outcome Obat Tidak Terpenuhi | Alasan final dan accountable bahwa jumlah obat yang diterima tidak dipenuhi. |
| Salinan Resep | Salinan Resep | Salinan Resep (Prescription Copy) accountable untuk obat atau jumlah resep yang tidak dimasukkan ke Sales Order atau tidak dipenuhi, bila berlaku. |
| Dispense Cancellation | Pembatalan Dispensing | Pengakhiran accountable suatu Dispense Order sebelum fulfillment berhasil. |
| Fulfillment Expiry | Berakhirnya Pemenuhan | Berakhirnya kesempatan fulfillment karena periode layanan yang diizinkan telah lewat. |
| Medication Return | Retur Obat | Pengembalian accountable atas obat yang sebelumnya disiapkan, dipindahkan, atau diserahkan. |
| Return to Stock | Pengembalian ke Stok | Penerimaan authoritative oleh Inventory atas obat retur yang eligible menjadi stok tersedia. |
| No-Show | Pasien Tidak Hadir | Outcome Rawat Jalan ketika Pasien tidak mengambil obat dan penyelesaian obat tidak diambil yang diotorisasi dicatat. |
| Collection Window | Jendela Pengambilan | Jumlah hari maksimum yang dapat dikonfigurasi bagi obat siap ambil. Default 7 hari. Jendela dimulai ketika Dispense Order pertama kali menjadi Ready for Pickup. |
| Pickup Expired | Pengambilan Kedaluwarsa | Kategori worklist Serah Obat setelah Collection Window habis tanpa Medication Handover. Ini kategori projection, bukan state Dispense Order. |
| Collection Window Override | Override Jendela Pengambilan | Fakta Pharmacist berwenang yang mengizinkan Medication Handover setelah Pickup Expired. Mencatat alasan override, Pharmacist yang mengotorisasi, dan effective business time. |
| Pharmacy Queue Entry | Entri Antrean Apotek | Partisipasi Pasien dalam antrean apotek Rawat Jalan yang identitas dan lifecycle-nya dimiliki Patient Tracker. |
| Pharmacy Queue Close | Penutupan Antrean Apotek | Fakta Staf Apotek yang mengakhiri Pharmacy Queue Entry yang belum masuk alur pelayanan obat. Mencatat alasan penutupan wajib, Staf Apotek penanggung jawab, dan effective business time. Patient Tracker kemudian menetapkan entri `Withdrawn` dari `Waiting`. |
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

Menerima permintaan dari Resep yang telah ditelaah atau Direct Medication Request yang diizinkan tanpa mengubah maksud klinis aslinya.

### 3.2 Telaah Resep

**Indonesia:** Telaah Resep

Menetapkan keputusan profesional atas setiap Resep dan barisnya, termasuk penerimaan sebagian dan substitusi yang telah diterima.

### 3.3 Sales Order Management

**Indonesia:** Sales Order Management

Membentuk dan menjaga permintaan obat yang diterima, jumlahnya, keterlacakan sumbernya, dan penyelesaian akhirnya.

### 3.4 Commercial Invoicing

**Indonesia:** Penagihan Komersial

Membentuk satu atau lebih Medication Sale dan Sales Invoice dari Sales Order Line tanpa bergantung pada jumlah maupun waktu pembentukan Dispense Order. Sales Invoice Item menyatakan jumlah dan nilai yang ditagihkan dari Sales Order Line sumbernya.

### 3.5 Fulfillment Planning

**Indonesia:** Perencanaan Pemenuhan

Membentuk satu atau lebih Dispense Order dari Sales Order Line berdasarkan Care Setting, jumlah, lokasi, siklus, dan kebijakan pemenuhan.

### 3.6 Dispense Authorization

**Indonesia:** Otorisasi Dispense

Mengevaluasi evidence keuangan dan coverage untuk menentukan kapan Medication Preparation dan dispensing boleh dimulai (`Dispense Authorized`).

### 3.7 Physical Dispensing

**Indonesia:** Dispensing Fisik

Mengoordinasikan Pharmacy Reserve melalui Stock Mutasi, Medication Preparation, Compounding, Final Dispense Review, Medication Dispense, Medication Handover, dan pengembalian stok No Show.

### 3.8 Partial and Unit-Dose Fulfillment

**Indonesia:** Pemenuhan Sebagian dan Dosis Unit

Mendukung Partial Prescription Fulfillment pada batas Prescription ke Sales Order, bagian penagihan dan pemenuhan yang dihitung terpisah melalui beberapa Dispense Order per Sales Order, Unit Dose Dispensing, dan Dose Window.

### 3.9 Exception and Return Resolution

**Indonesia:** Penyelesaian Exception dan Retur

Menyelesaikan kekurangan stok, selisih stok, substitusi, pembatalan, kedaluwarsa, retur, no-show, dan konsekuensi finansial yang diperlukan. Apotek Rawat Jalan tidak menahan Backorder atau mengarahkan fulfillment ke sumber stok alternatif.

### 3.10 Cross-Setting Traceability

**Indonesia:** Keterlacakan Lintas Setting

Menjaga keterlacakan jumlah dan sumber pada hasil Rawat Jalan, Rawat Inap, IGD, penagihan, dispensing, dan handover.

### 3.11 Outpatient Queue Coordination

**Indonesia:** Koordinasi Antrean Rawat Jalan

Mengaitkan Pharmacy Queue Entry yang dimiliki konteks eksternal dengan sumber pelayanan obat yang sesuai melalui Tracker Mapping atau Manual Mapping, tanpa menjadikan kedatangan dalam antrean sebagai prasyarat Telaah Resep.

### 3.12 Outpatient Payer, Pickup, and No-Show Resolution

**Indonesia:** Penyelesaian Payer, Pengambilan, dan No-Show Rawat Jalan

Menerapkan kebijakan clearance, timing invoice, pickup, handover, dan no-show untuk Pasien Umum dan BPJS dengan tetap memisahkan outcome komersial dan fulfillment.

## 4. Aktor & Peran

### 4.1 Dokter Penulis Resep

Memiliki Resep asli. Klarifikasi dengan Pharmacist berlangsung di luar sistem dan tidak mengubah Resep asli. Dokter Penulis Resep tidak memiliki penerimaan oleh Farmasi, pembentukan Sales Invoice, atau pelaksanaan dispensing.

### 4.2 Pharmacist

Memiliki Hasil Telaah Resep, Medication Substitution yang diotorisasi selama Telaah Resep, Final Dispense Review, verifikasi operasional Authorized Recipient, dan Patient Education Acknowledgement. Verifikasi Authorized Recipient bukan gate yang ditegakkan sistem. Pharmacist tidak memiliki pemanggilan administratif antrean Rawat Jalan. Authority Medication Substitution berakhir ketika Sales Order dibentuk.

### 4.3 Staf Apotek

Mengoordinasikan permintaan yang diterima, perkembangan Sales Order, interaksi administratif Rawat Jalan, penyiapan atau peracikan obat, dan penyerahan sesuai kewenangan. Untuk pelayanan Rawat Jalan, Staf Apotek memanggil Nomor Antrean, membuat Manual Mapping, menyampaikan nilai Pasien Umum sebelum Sales Invoice dibentuk, menyimpan Sales Invoice yang telah dikonfirmasi, menyiapkan obat sesuai Dispense Order, dan melakukan panggilan pengambilan. Untuk Permintaan Obat Langsung, Staf Apotek menerima atau menolak tanpa membentuk Resep. Konsultasi Pharmacist secara operasional bersifat opsional sebagai panduan SOP saja dan tidak dimodelkan sebagai approval gate. Ketika stok Rawat Jalan tidak mendukung pemenuhan penuh, Staf Apotek hanya memasukkan baris yang dapat dipenuhi ke Sales Order dan menerbitkan Salinan Resep untuk baris yang tidak dipenuhi. Staf Apotek tidak boleh membuat Backorder, memilih sumber stok alternatif, atau mengganti jenis obat.

### 4.4 Patient or Caregiver

Memberikan confirmation dan payment yang berlaku, menerima edukasi, dan menerima obat ketika bertindak sebagai Authorized Recipient.

### 4.5 Cashier

Menerima pembayaran dan memberikan evidence Payment Clearance. Cashier tidak menetapkan eligibility obat atau fulfillment quantity.

### 4.6 Inpatient Authorized Recipient

Menerima Ward Delivery untuk Pasien dan tetap teridentifikasi dalam outcome Medication Handover. Penerimaan tersebut tidak merepresentasikan Medication Administration.

### 4.7 Pharmacy Supervisor

Pharmacist yang berwenang menurut kebijakan operasional. Mengotorisasi retur, koreksi, Collection Window Override, penutupan koleksi kedaluwarsa, dan exception dispensing lain. Penanganan exception berbasis authority; tidak ada model ambang persetujuan moneter. Pharmacist mana yang berwenang ditentukan kebijakan operasional. Pharmacy Supervisor adalah sebutan operasional untuk authority tersebut ketika diperlukan peran pengotorisasi yang bernama.

## 5. Domain Objects

### 5.1 Telaah Resep

Merepresentasikan proses penilaian profesional Farmasi terhadap satu Resep. Object ini mempertahankan keputusan per baris, Pharmacist yang bertanggung jawab, dan Source Traceability tanpa menulis ulang Resep. Hasil obat yang diterima diwujudkan pada Sales Order.

### 5.2 Sales Order

Merepresentasikan satu permintaan obat khusus pasien yang telah diterima. Object ini memiliki Sales Order Line, jumlah yang diterima, dan perkembangan penyelesaian. Sales Order mengoordinasikan penagihan komersial serta pemenuhan fisik melalui Sales Invoice dan Dispense Order, yang item atau barisnya mereferensikan Sales Order Line.

### 5.3 Sales Invoice

Merepresentasikan satu Medication Sale dari satu Sales Order. Object ini memiliki Sales Invoice Item, klasifikasi payer, nilai komersial, financial disposition, adjustment, dan outcome Financial Charge. Setiap Sales Invoice Item obat mengidentifikasi satu Sales Order Line asalnya.

Sales Order Line adalah item Sales Order dalam konteks ini. Hubungannya dengan Sales Invoice Item langsung menunjukkan bagian yang ditagihkan:

```text
Sales Order Line
        │
        └──> Sales Invoice Item
```

Satu Sales Order Line dapat direpresentasikan oleh Sales Invoice Item dalam satu atau lebih Sales Invoice untuk penagihan sebagian. Setiap Sales Invoice Item berasal dari tepat satu Sales Order Line.

### 5.4 Dispense Order

Merepresentasikan satu instruksi pemenuhan fisik dari satu Sales Order. Object ini memiliki Dispense Order Line; setiap baris mereferensikan tepat satu Sales Order Line dari Sales Order tersebut, serta memuat perkembangan penyiapan dan telaah, outcome Medication Dispense, dan disposition pemenuhan akhirnya.

### 5.5 Dispense Authorized

Merepresentasikan hasil evaluasi kebijakan Pharmacy atas evidence keuangan dan/atau coverage untuk menentukan apakah Medication Preparation dan dispensing boleh dimulai. Bukan aggregate, entity, business object yang dipersist, sumber kebenaran, atau transaction boundary. Diperlukan sebelum Medication Preparation Started dan Dispensing. Tidak diperlukan untuk Medication Handover.

### 5.6 Medication Dispense

Merepresentasikan obat dan jumlah aktual yang disediakan untuk Pasien, termasuk responsible party, effective time, dan Dispense Order sumber.

### 5.7 Medication Handover

Merepresentasikan pemindahan obat pada waktu handover, termasuk destination bila berlaku dan Patient Education Acknowledgement. Nomor telepon penerima dan hubungan dengan Pasien boleh dicatat secara opsional hanya sebagai referensi dan tidak membuktikan identitas atau otorisasi hukum.

### 5.8 Outpatient Queue Mapping

Merepresentasikan mapping aktif antara Pharmacy Queue Entry yang dimiliki context eksternal dan sumber pelayanan obat yang sesuai. Mapping ini dapat diperbarui langsung bila sumber yang dipilih salah; riwayat perubahannya tidak perlu disimpan. Object ini mencatat metode mapping saat ini tanpa memiliki Queue Number atau lifecycle antrean.

### 5.9 Unfulfilled Medication Outcome

Merepresentasikan alasan final suatu Accepted Quantity tidak dipenuhi dan mengidentifikasi Salinan Resep, return, atau financial correction yang diperlukan. Apotek Rawat Jalan tidak menggunakan penutupan backorder.

### 5.10 Final Dispense Review Record

Merepresentasikan satu percobaan Final Dispense Review yang immutable dan dimiliki sebagai detail dari satu Dispense Order. Catatan review ditambahkan dan tidak diganti agar setiap kegagalan dan kelulusan review tetap accountable sesuai urutannya.

### 5.11 Patient Education Acknowledgement

Merepresentasikan konfirmasi Pharmacist bahwa konseling obat telah diberikan sebelum Medication Handover. Mencatat waktu edukasi dan Pharmacist penanggung jawab. Bukan catatan isi konseling terstruktur. Catatan konseling rinci boleh dilampirkan hanya ketika Pharmacist menilai dokumentasi tambahan diperlukan.

### 5.12 Collection Window Override

Merepresentasikan izin Pharmacist berwenang untuk menyelesaikan Medication Handover setelah Pickup Expired. Mencatat alasan override, Pharmacist yang mengotorisasi, dan effective business time. Tidak meng-expire Dispense Order dan tidak mengembalikan stok.

### 5.13 Pharmacy Queue Close

Merepresentasikan Staf Apotek mengakhiri Pharmacy Queue Entry yang belum masuk alur pelayanan obat. Mencatat alasan penutupan wajib, Staf Apotek penanggung jawab, dan effective business time. Meminta Patient Tracker `Withdrawn` dari `Waiting` dan tidak menambah state antrean.

### 5.14 Batas Pharmacy dan Stock Ledger

Pharmacy memiliki Sales Order, Dispense Order, lifecycle dispensing, `Prepared`, `Handed Over`, dan penyelesaian No Show. Stock Ledger memiliki jumlah stok, Mutasi, Remove Stock, dan riwayat pergerakan saja.

Pharmacy Reserve diimplementasikan hanya sebagai Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. Medication Handover meminta Remove Stock dari Dispensing Temporary Unit. Penyelesaian No Show meminta Stock Mutasi dari Dispensing Temporary Unit kembali ke Pharmacy Unit. `Prepared` adalah state Dispense Order saja dan bukan state Inventory.

```text
Dispensing Started
  -> Stock Mutasi: Pharmacy Unit -> Dispensing Temporary Unit

Dispensing Completed / Prepared
  -> tidak ada aksi Stock Ledger

Medication Handed Over
  -> Remove Stock from Dispensing Temporary Unit

No Show resolution
  -> Stock Mutasi: Dispensing Temporary Unit -> Pharmacy Unit
```

## 6. Aggregates

### 6.1 Telaah Resep Aggregate

**Aggregate Root:** `Telaah Resep`
Aggregate menjaga referensi sumber Resep, professional disposition per baris, Pharmacist yang bertanggung jawab, dan completion outcome tetap konsisten. Aggregate tidak menyimpan komunikasi klarifikasi dan tidak dapat mengubah Resep asli milik klinisi.

### 6.2 Sales Order Aggregate
**Aggregate Root:** `Sales Order`

Aggregate memiliki Sales Order Line, Accepted Quantity, Fulfilled Quantity, unfulfilled outcome, dan overall resolution. Aggregate mengoordinasikan lifecycle komersial dan pemenuhan serta merekonsiliasi jumlah dalam Sales Invoice Item dan Dispense Order Line yang mereferensikan setiap Sales Order Line.

Aggregate memastikan jumlah yang ditagihkan dan pemenuhan fisik tetap dapat ditelusuri serta tidak melebihi kewenangan Sales Order Line yang berlaku. Aggregate tidak memiliki payment settlement Sales Invoice, saldo Inventory, atau pelaksanaan physical dispensing.

### 6.3 Medication Sale Aggregate

**Aggregate Root:** `Sales Invoice`

Aggregate merepresentasikan satu Medication Sale. Aggregate menjaga Sales Invoice Item, Pricing Snapshot, Payer, line-level charge, invoice-level charge, financial disposition, Credit Note, refund, dan outcome Financial Charge tetap konsisten. Purchase Confirmation lisan Pasien Umum dibuktikan oleh pembentukan Sales Invoice yang accountable dan tidak disimpan sebagai object terpisah. Fakta komersial non-obat menggunakan model penjualan legacy: BHP sebagai baris katalog, charge khusus item pada baris, dan penyesuaian seluruh transaksi pada faktur. Tidak ada model komponen faktur tambahan.

Sales Invoice mereferensikan tepat satu Sales Order tetapi dapat mencakup satu atau lebih Sales Order Line miliknya.

### 6.4 Dispense Order Aggregate

**Aggregate Root:** `Dispense Order`

Aggregate memiliki Dispense Order Line dan menjaga penyiapan fisik, kumpulan Final Dispense Review Record yang immutable, Patient Education Acknowledgement, Collection Window Override bila berlaku, Medication Dispense, Medication Handover, cancellation, expiry, return, dan non-fulfillment outcome tetap konsisten. Dispense Order mereferensikan tepat satu Sales Order dan dapat memenuhi satu atau lebih Sales Order Line. Setiap Dispense Order Line mereferensikan tepat satu Sales Order Line dari Sales Order tersebut; satu Sales Order Line dapat dipenuhi melalui beberapa Dispense Order Line pada beberapa Dispense Order.

### 6.5 Relasi lintas aggregate

Satu Sales Order dapat memiliki nol atau lebih Sales Invoice dan nol atau lebih Dispense Order. Sales Invoice dan Dispense Order tidak diwajibkan memiliki jumlah atau waktu pembentukan yang sama.

Korelasi bisnisnya dinyatakan melalui referensi Sales Invoice Item dan Dispense Order Line kepada Sales Order Line, serta evaluasi kebijakan Dispense Authorized atas evidence keuangan dan coverage. Penggunaan Sales Order yang sama tidak dengan sendirinya menetapkan bahwa setiap Sales Invoice mengotorisasi setiap Dispense Order.

Outpatient Queue Mapping merupakan mapping aktif kepada Pharmacy Queue Entry yang dimiliki context eksternal, bukan Aggregate Root Apotek dan bukan catatan transaksi Perubahan mapping. Patient Tracker tetap authoritative atas Queue Session, Queue Number, dan lifecycle antrean. Pharmacy Queue Close adalah fakta operasional Apotek yang meminta Patient Tracker `Withdrawn`; bukan Aggregate Root dan bukan state antrean.

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
- **BR-APT-009** — Direct Medication Request adalah permintaan obat gaya ritel yang berasal di luar alur perawatan rumah sakit. Staf Apotek harus menerima atau menolaknya tanpa membentuk Resep. Konsultasi Pharmacist dapat terjadi secara operasional tetapi hanya panduan SOP opsional dan tidak boleh dimodelkan sebagai approval workflow, authority threshold, escalation, risk classification, domain state, atau business-rule gate.

### 7.2 Sales Order

- **BR-APT-010** — Sales Order harus berasal dari tepat satu sumber accepted demand yang telah selesai.
- **BR-APT-011** — Satu Resep harus membentuk maksimal satu Sales Order aktif per Registration selama Registration tersebut tetap aktif, kecuali baris Fornas Not Covered boleh membentuk Patient-Pay Sales Order terpisah yang independen dari Sales Order BPJS berdasarkan `BR-APT-119`–`BR-APT-124`.
- **BR-APT-012** — Sales Order harus memiliki minimal satu Sales Order Line dengan Accepted Quantity positif.
- **BR-APT-013** — Setiap Sales Order Line harus mempertahankan Source Traceability ke Baris Resep atau baris Direct Medication Request sumbernya; untuk obat pengganti, Sales Order Line memuat obat pengganti sementara referensi sumber tetap menunjuk Baris Resep asli.
- **BR-APT-014** — Sales Order bukan Sales Invoice, catatan pembayaran, pergerakan Pharmacy Reserve, Dispense Order, atau evidence Medication Dispense.
- **BR-APT-015** — Pembentukan Sales Invoice dan Dispense Order dapat terjadi secara independen pada waktu bisnis yang berbeda.
- **BR-APT-016** — Jumlah aktif Dispense Order Line yang mereferensikan suatu Sales Order Line tidak boleh melebihi Accepted Quantity yang belum terselesaikan.
- **BR-APT-017** — Fulfilled Quantity tidak boleh melebihi jumlah Dispense Order Line-nya.
- **BR-APT-018** — Setiap Accepted Quantity pada akhirnya harus fulfilled, cancelled, expired, atau memperoleh Unfulfilled Medication Outcome lain yang accountable. Apotek Rawat Jalan tidak boleh menetapkan Backorder.
- **BR-APT-019** — Sales Order hanya boleh mencapai Fulfillment Completion ketika setiap Accepted Quantity memiliki outcome final yang accountable.
- **BR-APT-105** — Batas fulfillment Apotek Rawat Jalan harus berupa Registration Period yang aktif. Resep dapat ditelaah, ditelaah ulang, dan dipenuhi selama Registration asal tetap aktif. Tidak boleh ada konsep Fulfillment Episode terpisah.
- **BR-APT-106** — Hak ulang resep harus dimiliki Resep melalui mekanisme `Iter` Legacy Resep. Sistem harus mengalokasikan Iter, melacak konsumsi Iter, dan menghitung sisa Iter.
- **BR-APT-107** — Sistem tidak menentukan apakah Iter yang belum terpakai tetap valid untuk fulfillment. Pharmacist harus menentukan apakah Iter yang belum terpakai masih dapat dihormati pada waktu fulfillment dan dapat menolak fulfillment meskipun sisa Iter masih ada.

### 7.3 Medication Sale dan Sales Invoice

- **BR-APT-020** — Setiap Medication Sale harus direpresentasikan oleh tepat satu Sales Invoice.
- **BR-APT-021** — Setiap Sales Invoice harus berasal dari tepat satu Sales Order. Setiap Sales Invoice Item obat harus berasal dari tepat satu Sales Order Line dari Sales Order tersebut.
- **BR-APT-022** — Satu Sales Order dapat menghasilkan nol, satu, atau beberapa Sales Invoice.
- **BR-APT-023** — Sales Invoice dapat mencakup satu atau lebih Sales Order Line melalui Sales Invoice Item-nya dan harus mempertahankan baris sumber, jumlah yang ditagihkan, serta nilai setiap item.
- **BR-APT-024** — Sales Invoice Item tidak boleh memperkenalkan komponen faktur non-obat free-form. BHP hanya boleh tampil sebagai baris katalog. Charge khusus item harus berupa line-level charge. Penyesuaian seluruh transaksi harus berupa invoice-level charge.
- **BR-APT-025** — Sales Invoice harus mempertahankan Pricing Snapshot dan Payer yang berlaku saat dibentuk.
- **BR-APT-026** — Pembentukan Sales Invoice tidak membuktikan bahwa stok tersedia, direservasi, disiapkan, didispensing, atau diserahkan.
- **BR-APT-027** — Sales Invoice yang issued atau financially settled harus dikoreksi melalui Financial Adjustment, Credit Note, atau outcome Refund yang accountable, bukan penggantian diam-diam.
- **BR-APT-028** — Setiap Financial Charge yang dikirim ke Tata Rekening harus mempertahankan Source Traceability ke Sales Invoice dan Sales Order sumbernya.

### 7.4 Dispense Order dan dispensing

- **BR-APT-029** — Setiap Dispense Order harus berasal dari Sales Order Line milik tepat satu Sales Order, dan setiap Dispense Order Line harus mereferensikan tepat satu Sales Order Line dari Sales Order tersebut.
- **BR-APT-030** — Satu Sales Order dapat menghasilkan nol, satu, atau beberapa Dispense Order.
- **BR-APT-031** — Dispense Order dapat mencakup satu atau lebih Sales Order Line dan harus mempertahankan setiap referensi baris langsung serta jumlahnya. Satu Sales Order Line dapat dibagi ke beberapa Dispense Order Line.
- **BR-APT-032** — Jumlah, pembagian quantity, dan timing Dispense Order dapat berbeda dari jumlah, pembagian nilai, dan timing Sales Invoice.
- **BR-APT-033** — Stock Mutasi dan Remove Stock harus tetap menjadi outcome authoritative milik Stock Ledger yang diminta Pharmacy untuk Dispense Order Line.
- **BR-APT-034** — Medication Preparation dan Compounding harus menggunakan Dispense Order aktif sebagai authority.
- **BR-APT-035** — Prepared Medication harus menyelesaikan Final Dispense Review sebelum Medication Handover.
- **BR-APT-036** — Medication Dispense tidak boleh melebihi unresolved quantity milik Dispense Order Line.
- **BR-APT-037** — Medication Handover harus mencatat effective business time. Verifikasi Authorized Recipient adalah tanggung jawab operasional Pharmacist yang menyerahkan obat dan tidak boleh ditegakkan oleh sistem. Sistem boleh secara opsional mencatat nomor telepon penerima dan hubungan dengan Pasien hanya sebagai referensi.
- **BR-APT-038** — Ward Delivery tidak boleh diperlakukan sebagai Medication Administration.
- **BR-APT-039** — Medication Administration tidak boleh disimpulkan dari Sales Invoice, Remove Stock, Medication Dispense, atau Ward Delivery.
- **BR-APT-096** — Setiap percobaan Final Dispense Review harus menambahkan Final Dispense Review Record yang immutable pada Dispense Order. Review yang gagal harus mencatat alasan, Pharmacist penanggung jawab, waktu bisnis efektif, dan jumlah terdampak, mengembalikan Dispense Order dari `Prepared` ke `Preparing`, serta melarang Medication Handover. Setelah koreksi selesai, Dispense Order harus kembali ke `Prepared` dan menjalani Final Dispense Review baru; hanya catatan review terbaru dengan hasil lulus yang dapat mengubahnya menjadi `Reviewed` dan mengizinkan Medication Handover.

### 7.5 Otorisasi Dispense dan koordinasi lintas aggregate

- **BR-APT-040** — Medication Preparation dan Dispensing hanya boleh berjalan ketika kebijakan Pharmacy mengevaluasi evidence keuangan dan coverage yang berlaku sebagai Dispense Authorized.
- **BR-APT-041** — Payment Clearance harus berasal dari payment authority yang bertanggung jawab dan tidak boleh disimpulkan hanya dari keberadaan Sales Invoice.
- **BR-APT-042** — Coverage Clearance harus mengidentifikasi Payer dan authority covered fulfillment yang berlaku.
- **BR-APT-043** — Dispense Authorized harus berupa hasil evaluasi kebijakan saja. Tidak boleh dipersist sebagai aggregate, entity, sumber kebenaran, atau transaction boundary.
- **BR-APT-044** — Satu Sales Invoice dapat mendukung evaluasi Dispense Authorized untuk beberapa Dispense Order, dan satu Dispense Order dapat bergantung pada beberapa Sales Invoice Item atau Sales Invoice ketika diwajibkan kebijakan.
- **BR-APT-045** — Sales Invoice yang paid atau financially cleared tidak menjamin fulfillment berhasil ketika terjadi shortage, discrepancy, expiry, atau exception sah lainnya.
- **BR-APT-046** — Financial clearance yang diikuti non-fulfillment harus menghasilkan Unfulfilled Medication Outcome yang accountable serta Credit Note, Refund, atau resolution komersial lain yang disetujui. Kondisi tersebut tidak boleh mensubstitusi Sales Order Line setelah Sales Order dibentuk. Apotek Rawat Jalan tidak menyelesaikan kondisi itu melalui Backorder atau sumber stok alternatif.

### 7.6 Partial fulfillment, UDD, dan exception

- **BR-APT-047** — Partial Fulfillment pada tingkat eksekusi Sales Order diwakili oleh satu Sales Order yang dipenuhi melalui beberapa Dispense Order, atau oleh jumlah fulfilled dan unresolved pada Sales Order Line. Ini adalah eksekusi fulfillment dan bukan kebijakan Partial Prescription Fulfillment. Dispense Order tidak memiliki semantics kebijakan Partial Prescription Fulfillment.
- **BR-APT-048** — Unit Dose Dispensing dapat membagi satu Sales Order Line menjadi beberapa Dispense Cycle dan Dispense Order.
- **BR-APT-049** — Dose Window harus memandu perencanaan fulfillment dan tidak boleh menyatakan Medication Administration.
- **BR-APT-050** — Untuk obat pengganti yang diterima, Sales Order Line harus mencatat obat pengganti, Pharmacist yang bertanggung jawab, alasan, dan jumlah yang terpengaruh serta tetap mereferensikan Baris Resep asli. Identitas obat pada Sales Order Line yang sudah dibentuk tidak boleh diubah; kebutuhan penggantian berikutnya ditangani dengan membatalkan item atau pesanan yang terdampak, menelaah kembali Resep asli, dan membentuk Sales Order Line baru tanpa mensyaratkan Resep perbaikan atau pengganti.
- **BR-APT-051** — Medication Shortage atau Stock Discrepancy tidak boleh mengubah Resep asli atau menghapus Sales Invoice yang sudah ada.
- **BR-APT-052** — Medication Return harus mengidentifikasi Dispense Order sumber, jumlah, alasan, dan disposition Inventory finalnya.
- **BR-APT-053** — Return to Stock hanya boleh terjadi ketika Inventory menerima obat retur berdasarkan kebijakannya sendiri.
- **BR-APT-054** — Salinan Resep harus mengidentifikasi obat atau jumlah resep yang tidak dipenuhi atau dikecualikan dari Sales Order, termasuk baris yang eligible untuk fulfillment eksternal.
- **BR-APT-055** — No-Show harus menjadi outcome kebijakan Rawat Jalan dan tidak boleh diterapkan pada Ward Delivery Rawat Inap.
- **BR-APT-108** — Partial Prescription Fulfillment hanya diizinkan untuk Patient Request, Stock Shortage, dan baris Fornas Not Covered. Tidak ada alasan lain yang diakui sistem.
- **BR-APT-109** — Untuk Patient Request, Staf Apotek dapat membentuk Sales Order yang hanya berisi baris resep yang dipilih. Baris resep yang dikecualikan tetap unfulfilled pada Resep asal. Sistem harus mendukung Salinan Resep untuk baris yang tidak dipenuhi.
- **BR-APT-110** — Untuk Stock Shortage sebelum Sales Order dibentuk, Staf Apotek dapat membentuk Sales Order yang hanya berisi baris resep yang dapat dipenuhi. Baris yang tidak tersedia tetap unfulfilled pada Resep asal. Sistem harus mendukung Salinan Resep untuk baris yang tidak dipenuhi. Tidak boleh dibentuk outstanding fulfillment obligation, waiting demand, atau backorder record.
- **BR-APT-111** — Pharmacist tetap bertanggung jawab menyetujui keputusan fulfillment yang dihasilkan ketika review profesional diperlukan. Sistem tidak menentukan substitusi alternatif atau tindakan fulfillment eksternal secara otomatis.
- **BR-APT-112** — Partialitas Partial Prescription Fulfillment hanya ada antara Prescription dan Sales Order. Partialitas tidak ada antara Sales Order dan Dispense Order.
- **BR-APT-113** — Sales Order yang dipenuhi melalui satu atau lebih Dispense Order adalah eksekusi fulfillment dan bukan kebijakan Partial Prescription Fulfillment.
- **BR-APT-114** — Apotek Rawat Jalan tidak mendukung Backorder. Kekurangan stok tidak boleh membentuk outstanding fulfillment obligation, waiting demand, atau backorder record.
- **BR-APT-115** — Kekurangan stok Rawat Jalan harus diselesaikan segera melalui Partial Sales Order atas baris yang dapat dipenuhi dan Salinan Resep untuk baris resep yang tidak dipenuhi. Salinan Resep dapat digunakan Pasien untuk memperoleh obat dari apotek lain.
- **BR-APT-116** — Ketika persediaan Rawat Jalan tidak cukup, hanya baris resep yang dapat dipenuhi yang boleh masuk Sales Order. Baris yang tidak dapat dipenuhi tetap di luar Sales Order pada Resep asal.
- **BR-APT-117** — Apotek Rawat Jalan tidak mengimplementasikan pemilihan sumber stok alternatif, fulfillment routing, inter-pharmacy sourcing, atau backorder management. Ketersediaan stok dievaluasi terhadap otoritas stok yang sedang tersedia.
- **BR-APT-118** — Ketika kekurangan stok Rawat Jalan teridentifikasi setelah Sales Order dibentuk atau financial clearance, jumlah yang tidak dapat dipenuhi harus memperoleh Unfulfilled Medication Outcome yang accountable dan Salinan Resep bila berlaku, plus Credit Note atau Refund ketika ada konsekuensi komersial. Jumlah tersebut tidak boleh di-backorder atau diarahkan ke sumber stok alternatif.
- **BR-APT-119** — Validasi Fornas harus mengklasifikasikan baris resep sebagai Covered atau Not Covered.
- **BR-APT-120** — Baris Covered harus mengikuti workflow fulfillment BPJS normal. Evidence coverage cukup untuk Dispense Authorized pada baris tersebut.
- **BR-APT-121** — Baris Not Covered tidak boleh dibatalkan secara otomatis. Farmasi boleh membentuk Patient-Pay Sales Order terpisah untuk baris yang tidak dijamin. Patient-Pay Sales Order itu independen dari Sales Order yang ditanggung BPJS. Baris tidak dijamin tidak boleh tetap pada jalur fulfillment BPJS.
- **BR-APT-122** — Patient-Pay Sales Order harus memerlukan Payment Clearance menurut workflow self-pay normal sebelum Dispense Authorized diberikan. Ini adalah evaluasi evidence keuangan, bukan aggregate Financial Clearance.
- **BR-APT-123** — Setiap baris resep mengikuti jalur otorisasi sendiri: baris BPJS Covered menggunakan Coverage Evidence untuk menjadi Dispense Authorized; baris Patient-Pay menggunakan Payment Clearance untuk menjadi Dispense Authorized.
- **BR-APT-124** — Membentuk Patient-Pay Sales Order terpisah untuk baris Fornas Not Covered adalah Partial Prescription Fulfillment. Satu Resep asal dapat menghasilkan Sales Order yang ditanggung BPJS dan Patient-Pay Sales Order untuk baris resep yang berbeda.
- **BR-APT-125** — Apotek Rawat Jalan harus mengadopsi model penjualan legacy yang ada untuk fakta komersial non-obat. Tidak boleh diperkenalkan model komponen faktur tambahan.
- **BR-APT-126** — BHP harus diperlakukan sebagai item katalog standar dan boleh tampil sebagai baris penjualan. BHP tidak boleh direpresentasikan sebagai item faktur free-form.
- **BR-APT-127** — Charge khusus item, termasuk biaya kemasan dan racikan, harus dicatat sebagai line-level charge pada baris penjualan yang berlaku.
- **BR-APT-128** — Penyesuaian seluruh transaksi, termasuk pembulatan, harus dicatat sebagai invoice-level charge pada Sales Invoice.
- **BR-APT-129** — Verifikasi Authorized Recipient tetap menjadi tanggung jawab operasional Pharmacist yang menyerahkan obat dan tidak boleh ditegakkan oleh sistem.
- **BR-APT-130** — Selama Medication Handover, sistem boleh secara opsional mencatat nomor telepon penerima dan hubungan dengan Pasien hanya sebagai referensi. Informasi penerima yang dicatat tidak merupakan bukti identitas, otorisasi hukum, atau workflow gate.
- **BR-APT-131** — Sistem tidak boleh mensyaratkan validasi identitas, verifikasi hubungan hukum, penangkapan dokumen, atau authorization workflow sebagai evidence penerima Medication Handover.
- **BR-APT-132** — Patient Education harus dicatat sebagai Patient Education Acknowledgement yang ringan sebelum Medication Handover. Pharmacist harus mengonfirmasi bahwa konseling obat telah diberikan. Pengakuan tersebut adalah handover gate.
- **BR-APT-133** — Patient Education Acknowledgement harus mencatat waktu edukasi dan Pharmacist penanggung jawab. Tidak mensyaratkan isi konseling terstruktur, template khusus obat, tanda tangan Pasien, atau identitas orang yang diedukasi.
- **BR-APT-134** — Catatan konseling rinci bersifat opsional dan hanya dicatat ketika Pharmacist menilai dokumentasi tambahan diperlukan. Tidak adanya catatan rinci tidak boleh menghalangi Medication Handover setelah pengakuan dicatat.
- **BR-APT-135** — Penanganan exception harus berbasis authority. Retur, koreksi, override koleksi kedaluwarsa, dan exception dispensing lain harus memerlukan otorisasi Pharmacist yang berwenang menurut kebijakan operasional.
- **BR-APT-136** — Sistem tidak boleh memperkenalkan model ambang persetujuan moneter. Otorisasi exception tidak boleh ditentukan oleh amount, nilai kuantitas, atau pita persetujuan numerik serupa.
- **BR-APT-137** — Otorisasi exception harus mencatat Pharmacist yang mengotorisasi, effective business time, dan alasan. Kebijakan operasional menentukan Pharmacist mana yang berwenang. Pharmacy Supervisor adalah sebutan operasional untuk authority tersebut ketika diperlukan peran pengotorisasi yang bernama.
- **BR-APT-138** — Obat Rawat Jalan yang menunggu pengambilan boleh tetap Ready for Pickup selama Collection Window yang dapat dikonfigurasi. Default Collection Window adalah 7 hari. Jendela dimulai ketika Dispense Order pertama kali menjadi Ready for Pickup: `Prepared`, dimaksudkan untuk pickup Rawat Jalan, dan Medication Handover belum selesai.
- **BR-APT-139** — Ketika Collection Window habis tanpa Medication Handover, kategori worklist Serah Obat menjadi Pickup Expired. Pickup Expired adalah kategori projection dan bukan state Dispense Order.
- **BR-APT-140** — Medication Handover biasa tidak boleh dilanjutkan selama Pickup Expired. Hanya Pharmacist berwenang yang boleh mencatat Collection Window Override lalu melanjutkan handover.
- **BR-APT-141** — Collection Window Override harus mencatat Pharmacist yang mengotorisasi, effective business time, dan alasan override. Medication Handover setelah Pickup Expired dilarang tanpa override tersebut.
- **BR-APT-142** — Pickup Expired tidak dengan sendirinya meng-expire Dispense Order, membalik `DoneAt` antrean, menetapkan No-Show, atau mengembalikan stok. Penyelesaian terminal obat tidak diambil tetap kegiatan manual yang diotorisasi berdasarkan `BR-APT-079`, `BR-APT-080`, dan `WF-APT-RJ-007`.
- **BR-APT-143** — Pharmacy Queue Entry yang belum masuk alur pelayanan obat boleh ditutup dari status antrean pra-layanan. Status itu adalah Patient Tracker `Waiting`. TAKEN dalam keputusan ini menamai partisipasi pra-layanan yang sama dan tidak boleh ditambahkan sebagai state Patient Tracker.
- **BR-APT-144** — Pharmacy Queue Close harus mencatat alasan penutupan wajib, Staf Apotek penanggung jawab, dan effective business time. Patient Tracker harus menetapkan Queue Entry `Withdrawn`. State antrean tambahan tidak boleh diperkenalkan.
- **BR-APT-145** — Pharmacy Queue Close tidak boleh mencatat `ServedAt` atau `DoneAt`, tidak boleh menyatakan `In Service` atau `Done`, dan tidak boleh membentuk Sales Order, Dispense Order, atau Medication Handover. Setelah `Medication Preparation Started`, jalur penutupan ini tidak berlaku.

### 7.7 Completion dan history

- **BR-APT-056** — Commercial progress dan fulfillment progress Sales Order harus dilacak secara independen.
- **BR-APT-057** — Commercial resolution tidak dengan sendirinya menyelesaikan physical fulfillment, dan physical fulfillment tidak dengan sendirinya membuktikan financial resolution.
- **BR-APT-058** — Keputusan material mengenai review, pembentukan Sales Invoice, pembentukan Dispense Order, clearance, dispensing, handover, exception, dan correction harus mempertahankan responsible party dan effective business time.
- **BR-APT-059** — Source Traceability harus dipertahankan dari Resep atau Direct Medication Request melalui Sales Order, Sales Invoice, Dispense Order, dan final outcome.
- **BR-APT-060** — Hasil bisnis yang sudah selesai atau dibatalkan tidak boleh dihapus; koreksi setelahnya harus menambahkan fakta koreksi yang dapat dipertanggungjawabkan. Aturan ini tidak berlaku untuk koreksi Outpatient Queue Mapping yang masih aktif; mapping tersebut diperbarui langsung sesuai `BR-APT-062`.

### 7.8 Kebijakan workflow Rawat Jalan

- **BR-APT-061** — Pharmacist dapat melakukan Telaah Resep segera setelah Resep tersedia; kedatangan Pasien dan Outpatient Queue Mapping tidak boleh menjadi prasyarat.
- **BR-APT-062** — Outpatient Queue Mapping harus merepresentasikan mapping catatan bisnis yang sudah ada dan tidak boleh membentuk atau mengubah Resep, Hasil Telaah Resep, atau Sales Order. Jika mapping salah, sistem harus memperbarui mapping aktif ke sumber yang benar tanpa mewajibkan riwayat perubahan mapping.
- **BR-APT-063** — Tracker Mapping harus digunakan ketika bukti tracker atau registrasi yang sah menemukan satu atau beberapa Resep yang sudah ada dan berlaku. Proses ini tidak boleh membuat Resep atau menemukan Permintaan Obat Langsung; jika gagal, proses harus beralih ke Manual Mapping.
- **BR-APT-064** — Pharmacy Queue Entry yang diterbitkan langsung atau belum teridentifikasi harus tetap unmapped sampai Staf Apotek mengidentifikasi dan melakukan mapping sumber pelayanan obat yang sesuai, atau sampai Staf Apotek mencatat Pharmacy Queue Close berdasarkan `BR-APT-143`–`BR-APT-145`.
- **BR-APT-065** — Staf Apotek harus memiliki pemanggilan administratif antrean dan pickup; tanggung jawab tersebut tidak boleh dipindahkan kepada Pharmacist.
- **BR-APT-066** — Dalam alur normal Resep Elektronik BPJS dengan Tracker Mapping berhasil, Pasien hanya memerlukan satu pemanggilan apotek Rawat Jalan, yaitu pickup call setelah setiap Dispense Order yang berlaku mencapai `Prepared`.
- **BR-APT-067** — Outpatient Queue Mapping dan Purchase Confirmation Pasien Umum dapat diselesaikan dalam satu interaksi loket ketika Sales Order Line yang berlaku dan nilai yang dihitung telah tersedia. Ketika Tracker Mapping selesai tanpa Pasien hadir di loket, Staf Apotek harus memanggil Queue Number untuk interaksi Purchase Confirmation sebelum Sales Invoice dibentuk; panggilan administratif ini tidak menetapkan `ServedAt` atau `DoneAt`.
- **BR-APT-068** — Dispense Order Rawat Jalan dan Pharmacy Reserve melalui Stock Mutasi dapat dibentuk sebelum kedatangan Pasien atau Outpatient Queue Mapping, tetapi Medication Preparation tetap harus memerlukan Dispense Authorized.
- **BR-APT-069** — Obat yang disiapkan untuk pickup Rawat Jalan harus tetap dalam Dispensing Temporary Custody sampai Medication Handover yang accountable atau Mutasi pengembalian No Show.
- **BR-APT-070** — Sebelum Sales Invoice Pasien Umum tersedia, Staf Apotek harus menyampaikan nilai yang dihitung dari Sales Order Line dan Pricing Snapshot yang berlaku serta memperoleh Purchase Confirmation lisan. Penyimpanan transaksi yang telah dikonfirmasi membentuk Sales Invoice beserta Sales Invoice Item-nya dari baris tersebut; object atau transaksi Purchase Confirmation terpisah tidak disimpan.
- **BR-APT-071** — Ketika Pasien Umum menolak Purchase Confirmation sebelum transaksi disimpan, Sales Invoice tidak boleh dibentuk dan jumlah Pharmacy Reserve yang tidak digunakan harus dikembalikan ke Pharmacy Unit melalui Stock Mutasi. Sales Invoice yang dibentuk setelah konfirmasi hanya dapat dibatalkan selama lifecycle-nya mengizinkan; konsekuensi issued atau financially cleared harus mengikuti `BR-APT-027`.
- **BR-APT-072** — Medication Preparation Pasien Umum tidak boleh dimulai sebelum Dispense Authorized terpenuhi dari Payment Clearance dan evidence Sales Invoice yang berlaku.
- **BR-APT-073** — Pasien BPJS tidak boleh diminta melakukan Purchase Confirmation atau pembayaran Pasien; jumlah Patient-payable harus nol dan payment disposition harus `Not Required`, sedangkan nilai gross atau covered dapat tetap bukan nol.
- **BR-APT-074** — Medication Preparation BPJS dapat dimulai ketika Outpatient Queue Mapping, Dispense Order yang berlaku, Coverage Clearance, dan Dispense Authorized terpenuhi; keberadaan Sales Invoice tidak boleh menjadi prasyarat.
- **BR-APT-075** — Untuk kebijakan BPJS Rawat Jalan saat ini, Sales Invoice hanya boleh dibentuk sebagai bagian dari Medication Handover yang berhasil dikonfirmasi; pembentukan Sales Invoice dan penyelesaian handover harus menjadi satu outcome bisnis accountable.
- **BR-APT-076** — Staf Apotek harus memanggil Pasien untuk pickup Rawat Jalan setelah setiap Dispense Order dalam coordinated pickup mencapai `Prepared`. Pharmacist kemudian melakukan Final Dispense Review dengan Pasien atau caregiver hadir sebelum Medication Handover.
- **BR-APT-077** — Dalam interaksi loket yang sama setelah pickup call, Pharmacist harus memverifikasi penerima secara operasional, menyelesaikan Final Dispense Review, mencatat Patient Education Acknowledgement, dan baru kemudian menyelesaikan Medication Handover Rawat Jalan. Verifikasi penerima tidak boleh menjadi gate yang ditegakkan sistem.
- **BR-APT-078** — Medication Handover Rawat Jalan yang berhasil harus menyelesaikan jumlah Dispense Order yang berlaku dan meminta Remove Stock dari Dispensing Temporary Unit melalui Stock Ledger.
- **BR-APT-079** — No-Show BPJS sebelum Medication Handover tidak boleh membentuk atau membatalkan Sales Invoice. Penyelesaian manual obat tidak diambil yang diotorisasi harus membuat Dispense Order terkait `Expired`, meminta Stock Mutasi dari Dispensing Temporary Unit kembali ke Pharmacy Unit bila berlaku, dan hanya mengizinkan Sales Order menjadi `Resolved` dengan alasan `Collection Window Expired` setelah setiap Accepted Quantity dan konsekuensi komersial memiliki outcome final.
- **BR-APT-080** — No-Show Pasien Umum setelah pembayaran harus menggunakan penyelesaian manual obat tidak diambil yang sama untuk konsekuensi fulfillment, tetapi Sales Order harus tetap `Active` sampai Tata Rekening atau financial authority yang bertanggung jawab memberikan Credit Note, Refund, atau outcome komersial final lain yang accountable.
- **BR-APT-081** — `Medication Preparation Started` harus menjadi Pharmacy Service Start Evidence bagi setiap jalur payer Rawat Jalan dan menyebabkan Patient Tracker mencatat `ServedAt`. Pembentukan Sales Invoice dan Purchase Confirmation tidak boleh menetapkan `ServedAt` apotek Rawat Jalan.
- **BR-APT-082** — Patient Tracker harus tetap authoritative atas identitas Pharmacy Queue Entry, Queue Number, dan lifecycle antrean meskipun Apotek memiliki Outpatient Queue Mapping dan tujuan pemanggilan.
- **BR-APT-083** — User tidak boleh menginput Legacy DU atau Sales Invoice Item obat yang berdiri sendiri secara manual; tindakan user hanya dapat memicu pembentukan Sales Invoice dari Sales Order Line yang accountable. User tidak boleh menginput item faktur non-obat free-form.
- **BR-APT-084** — Satu Pharmacy Queue Entry dapat dimappingkan ke satu atau beberapa Resep atau Direct Medication Request. Setiap demand yang dimappingkan harus mempertahankan Telaah Resep bila berlaku, Sales Order, Sales Invoice, Dispense Order, dan lifecycle accountable masing-masing.
- **BR-APT-085** — Mapping beberapa medication demand ke satu Pharmacy Queue Entry harus mengoordinasikan satu pelayanan Rawat Jalan dan tidak boleh menggabungkan Sales Order, Sales Invoice, atau Dispense Order masing-masing.
- **BR-APT-086** — Dalam satu Registration aktif, satu Sales Order aktif harus dikoordinasikan melalui satu Dispense Order aktif untuk jalur Rawat Jalan normal. Pharmacy Queue Entry yang sama dapat mengoordinasikan beberapa pasangan Sales Order dan Dispense Order tersebut.
- **BR-APT-087** — Tampilan progress per demand pada antrean harus berupa projection fakta Apotek untuk setiap demand yang dimappingkan; Patient Tracker tidak boleh menjadi authoritative atas state Telaah Resep, Sales Invoice, atau Dispense Order.
- **BR-APT-088** — Satu coordinated pickup call hanya boleh dilakukan setelah setiap Dispense Order yang hendak diserahkan mencapai `Prepared` atau memperoleh outcome exception yang accountable.
- **BR-APT-089** — Staf Apotek harus menerima atau menolak Direct Medication Request. Penerimaan membentuk record Direct Medication Request; penolakan tidak boleh membentuk record Direct Medication Request maupun Sales Order. Tidak ada persetujuan Pharmacist, rujukan, escalation, atau approval threshold.
- **BR-APT-090** — Coverage Clearance BPJS Rawat Jalan harus memerlukan SEP yang valid untuk encounter terkait dan coverage Fornas item-level authoritative untuk jumlah yang diberi clearance.
- **BR-APT-091** — Baris resep Fornas Not Covered tidak boleh tetap pada Sales Order yang ditanggung BPJS. Baris Covered membentuk Sales Order yang ditanggung BPJS. Baris Not Covered boleh membentuk Patient-Pay Sales Order terpisah. Setiap Sales Order hanya menghasilkan Sales Invoice dari jalur payer-nya sendiri.
- **BR-APT-092** — Untuk Patient-Pay Sales Order, Sales Invoice Pasien Umum hanya boleh dibentuk setelah Purchase Confirmation lisan dan mengikuti workflow self-pay. Sales Invoice BPJS dari Sales Order BPJS yang independen hanya boleh dibentuk bersama Medication Handover yang berhasil berdasarkan `BR-APT-075`.
- **BR-APT-093** — Coordinated pickup call mixed coverage harus menunggu sampai setiap Dispense Order yang hendak diserahkan memperoleh Dispense Authorized dari jalurnya sendiri dan mencapai `Prepared`.
- **BR-APT-094** — Jika Pasien menolak Patient-Pay Sales Order sebelum Sales Invoice-nya dibentuk, Patient-Pay Sales Order tersebut harus memperoleh outcome declined yang accountable, sedangkan Sales Order BPJS yang independen dapat dilanjutkan.
- **BR-APT-095** — Patient Tracker harus mencatat `DoneAt` apotek Rawat Jalan ketika penyelesaian antrean terjadi. Penyelesaian antrean dapat dipicu oleh (1) Staf Apotek melakukan coordinated pickup call, atau (2) penyelesaian No Show terotorisasi berdasarkan `WF-APT-RJ-007` ketika Queue Entry masih `In Service` karena pickup call belum terjadi. Jika Queue Entry sudah `Done` ketika penyelesaian No Show dijalankan, `DoneAt` tidak dicatat ulang. `DoneAt` tidak pernah dibalik. `Done` pada antrean tidak membuktikan Final Dispense Review, Patient Education Acknowledgement, Medication Dispense, atau Medication Handover; itu hanya berarti lifecycle layanan antrean telah selesai. Status antrean baru tidak diperkenalkan, dan state alur kerja farmasi tidak ditambahkan ke Queue Entry. Jalur penyelesaian ini bukan Pharmacy Queue Close (`BR-APT-143`–`BR-APT-145`).
- **BR-APT-097** — `QueueEntry` Patient Tracker harus menjadi satu-satunya identitas antrean apotek Rawat Jalan yang canonical. Identitas antrean Farinv legacy didepresiasi dan tidak boleh membuat record antrean aktif. Data antrean Farinv historis bersifat read-only. Model antrean dual-active tidak diizinkan. Evidence `Apotek-Start` dan `Apotek-Done` Patient Tracker harus mereferensikan `QueueEntryId` canonical.
- **BR-APT-098** — Pharmacy Reserve harus diimplementasikan hanya sebagai Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. Tidak boleh ada kontrak `ReserveStock` terpisah.
- **BR-APT-099** — `Prepared` harus menjadi state Dispense Order saja. Dispense Order mencapai `Prepared` ketika semua pergerakan dispensing yang diperlukan untuk penyiapan itu selesai. `Prepared` bukan state Inventory.
- **BR-APT-100** — Dispensing Started harus menyebabkan Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. Dispensing Completed atau `Prepared` tidak menyebabkan aksi Inventory.
- **BR-APT-101** — Medication Handed Over harus menyebabkan Remove Stock dari Dispensing Temporary Unit melalui Stock Ledger.
- **BR-APT-102** — Penyelesaian No Show dimiliki Pharmacy. Inventory hanya menerapkan Stock Mutasi dari Dispensing Temporary Unit kembali ke Pharmacy Unit yang diarahkan Pharmacy dan tidak menyimpan status No Show.
- **BR-APT-103** — Stock Ledger tidak memiliki state lifecycle dispensing, `Prepared`, `Handed Over`, No Show, atau fulfillment.
- **BR-APT-104** — Semantics pemenuhan sebagian dimiliki Sales Order. Satu Sales Order dapat dipenuhi melalui beberapa Dispense Order.

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
| Established | Accepted demand telah tersedia dan penagihan komersial serta perencanaan pemenuhan dapat dimulai. |
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
  -> Invoiced through Sales Invoice Item or Commercially Unallocated
  -> Referenced by Dispense Order Line or Not Yet Planned for Fulfillment
  -> Fulfilled | Cancelled | Expired | Other Unfulfilled Outcome
```

Cabang billing dan fulfillment berjalan independen. Resolusi Sales Order final mensyaratkan rekonsiliasi kedua cabang, bukan jumlah dokumen yang sama. Apotek Rawat Jalan tidak menggunakan `Backordered`.

### 8.6 Relasi Outpatient Queue Mapping

```text
Unmapped
  -> Mapped
       method: Tracker Mapping | Manual Mapping
  -> Pharmacy Queue Close
       -> Patient Tracker Withdrawn
```

Relasi ini melakukan mapping sumber pelayanan obat dengan Pharmacy Queue Entry yang dimiliki context eksternal. Relasi ini tidak menggantikan lifecycle antrean Patient Tracker dan tidak mengubah state Telaah Resep. Pharmacy Queue Close mengakhiri partisipasi unmapped atau declined dari `Waiting` tanpa menambah state antrean.

### 8.7 Lifecycle pickup dan handover Rawat Jalan

```text
Prepared
  -> Ready for Pickup
       -> Patient Called
            -> Final Review Completed
                 -> Education Provided
                      -> Handed Over

Ready for Pickup or Patient Called
  -> Pickup Expired
       -> Collection Window Override
            -> Patient Called / Final Review / Education / Handed Over
       -> No-Show
            -> penyelesaian manual obat tidak diambil
                 -> Dispense Order Expired
                 -> alasan resolution Collection Window Expired

Prepared, Ready for Pickup, or Patient Called
  -> No-Show
       -> penyelesaian manual obat tidak diambil
            -> Dispense Order Expired
            -> alasan resolution Collection Window Expired
```

Ready for Pickup dan Pickup Expired adalah kategori projection. Collection Window (default 7 hari) dimulai ketika Dispense Order pertama kali menjadi Ready for Pickup. Pickup Expired tidak mengubah state Dispense Order. Handover biasa diblokir sampai Collection Window Override dicatat. Penutupan terminal obat tidak diambil tetap tindakan terpisah yang diotorisasi.

Pickup call adalah salah satu pemicu yang menyelesaikan antrean Patient Tracker (`In Service` → `Done`) dan mencatat `DoneAt`. Ketika penyelesaian No Show dijalankan sebelum pickup call, Apotek boleh menyelesaikan Queue Entry terkait pada lifecycle yang sama; Patient Tracker mencatat `DoneAt` pada saat penyelesaian itu. Ketika penyelesaian No Show dijalankan setelah pickup call, Queue Entry mungkin sudah `Done`; `DoneAt` dipertahankan dan tidak pernah dibalik. `Done` pada antrean tidak menyelesaikan Medication Handover dan tidak menambahkan state alur kerja farmasi ke Queue Entry. Final Dispense Review dan Patient Education Acknowledgement dilakukan dengan Pasien atau caregiver hadir setelah pickup call. Pharmacist memverifikasi penerima secara operasional selama interaksi loket itu; verifikasi bukan langkah lifecycle yang ditegakkan sistem. Sistem boleh secara opsional mencatat nomor telepon penerima dan hubungan sebagai referensi. Patient Education Acknowledgement mencatat waktu edukasi dan Pharmacist penanggung jawab; catatan konseling rinci bersifat opsional. Konsekuensi komersial yang berlaku bergantung pada payer. Pasien Umum dapat telah memiliki Sales Invoice yang financially cleared, sedangkan kebijakan BPJS saat ini baru membentuk Sales Invoice bersama Medication Handover yang berhasil.

## 9. Domain Events

| Domain Event | Makna bisnis |
|---|---|
| Telaah Resep Started | Pharmacist memulai penilaian profesional atas Resep. |
| Telaah Resep Completed | Setiap baris yang ditelaah memperoleh professional disposition final. |
| Outpatient Queue Mapped | Pharmacy Queue Entry telah di-mapping dengan sumber pelayanan obat yang sesuai. |
| Pharmacy Queue Close Recorded | Staf Apotek menutup Queue Entry yang belum masuk alur pelayanan obat, dengan alasan wajib. |
| Direct Medication Request Accepted | Permintaan non-resep yang diizinkan telah diterima Farmasi. |
| Sales Order Established | Permintaan obat yang diterima tersedia untuk penagihan komersial dan perencanaan fulfillment. |
| Sales Invoice Established | Medication Sale beserta Sales Invoice Item-nya dibentuk dari Sales Order Line. |
| Sales Invoice Issued | Sales Invoice menjadi dokumen komersial authoritative. |
| Payment Clearance Established | Payment authority yang bertanggung jawab mengonfirmasi kondisi pembayaran yang berlaku. |
| Coverage Clearance Established | Payer yang berlaku mengotorisasi covered fulfillment. |
| Dispense Authorized Evaluated | Kebijakan Pharmacy menentukan bahwa preparation dan dispensing boleh dilanjutkan untuk jumlah yang berlaku. |
| Dispense Order Established | Instruksi physical fulfillment beserta Dispense Order Line-nya dibentuk langsung dari Sales Order Line. |
| Stock Transferred to Dispensing Temporary Unit | Stock Ledger mencatat Pharmacy Reserve sebagai Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit. |
| Stock Removed from Dispensing Temporary Unit | Stock Ledger mencatat Remove Stock dari Dispensing Temporary Unit setelah Medication Handover. |
| Stock Returned to Pharmacy Unit | Stock Ledger mencatat Stock Mutasi dari Dispensing Temporary Unit kembali ke Pharmacy Unit setelah penyelesaian No Show atau pengembalian Pharmacy Reserve yang tidak digunakan. |
| Medication Preparation Started | Penyiapan fisik dimulai berdasarkan Dispense Order yang telah released. |
| Medication Prepared | Jumlah obat pada Dispense Order Line menyelesaikan penyiapan fisik. |
| Final Dispense Review Completed | Dengan Pasien atau caregiver hadir setelah pickup call, Prepared Medication lulus pemeriksaan profesional akhir yang diperlukan. |
| Final Dispense Review Failed | Prepared Medication gagal dalam pemeriksaan profesional akhir; catatan review immutable ditambahkan dan Dispense Order kembali dari `Prepared` ke `Preparing` untuk dikoreksi. |
| Patient Education Acknowledged | Pharmacist mengonfirmasi bahwa konseling obat telah diberikan; waktu edukasi dan Pharmacist penanggung jawab dicatat. |
| Pickup Expired Classified | Collection Window habis tanpa Medication Handover; kategori worklist menjadi Pickup Expired. |
| Collection Window Override Recorded | Pharmacist berwenang mencatat alasan yang mengizinkan handover setelah Pickup Expired. |
| Patient Called for Pickup | Staf Apotek memanggil Pasien untuk Medication Handover Rawat Jalan. |
| Medication Dispensed | Sejumlah obat disediakan secara accountable untuk Pasien. |
| Medication Handed Over | Obat dipindahkan kepada Pasien atau penerima lain sebagaimana ditentukan secara operasional oleh Pharmacist. |
| Outpatient No-Show Recorded | Pasien tidak mengambil obat dalam batas layanan Rawat Jalan yang berlaku. Ketika Queue Entry terkait masih `In Service`, resolusi ini boleh menyelesaikan antrean menjadi `Done` dan mencatat `DoneAt`; ketika Queue Entry sudah `Done`, `DoneAt` tidak dibalik. |
| Medication Shortage Identified | Stok tersedia tidak dapat mendukung jumlah fulfillment yang dimaksud. |
| Medication Substitution Authorized | Authority yang accountable menyetujui penggantian obat yang diminta. |
| Dispense Order Backordered | Jumlah unresolved dipertahankan untuk fulfillment kemudian. Tidak digunakan di Apotek Rawat Jalan. |
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
