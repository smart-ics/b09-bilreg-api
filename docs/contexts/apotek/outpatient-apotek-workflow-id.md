# Workflow Outpatient Apotek

**Status artifact:** Pendamping semantik workflow bisnis Bahasa Indonesia

**Bounded context:** Apotek (`Pelayanan Obat Pasien`)

**Care setting:** Apotek Rawat Jalan

**Sumber canonical English:** [outpatient-apotek-workflow.md](./outpatient-apotek-workflow.md)

**Domain authoritative:** [Domain Apotek](./apotek-domain-id.md)

## 1. Gambaran Umum Workflow

Workflow ini mengoordinasikan pelayanan obat Rawat Jalan sejak Pasien memperoleh Nomor Antrean Apotek sampai Medication Handover atau outcome non-fulfillment yang accountable.

Cakupannya meliputi Resep Elektronik, Resep Fisik yang dicatat, dan Jual Bebas; Tracker Mapping dan Manual Mapping; jalur komersial Pasien Umum, BPJS, dan mixed coverage; satu Pharmacy Queue Entry yang dimappingkan ke beberapa Resep Kerja atau Jual Bebas; dispensing; pickup; Final Dispense Review; Patient Education; serta penyelesaian No-Show.

Telaah Resep dapat berjalan sebelum Pasien datang dan secara independen dari queue mapping. Progress komersial dan physical fulfillment tetap independen serta dikoordinasikan melalui Sales Order.

```text
Pharmacy Queue Entry dan Resep Kerja atau Jual Bebas
  -> Outpatient Queue Mapping
  -> Telaah Resep atau penerimaan Jual Bebas
  -> Sales Order
       -> Invoice sesuai waktu penagihan payer
            -> Invoice Item mereferensikan Sales Order Item
       -> Dispensing
            -> Dispensing Item mereferensikan Sales Order Item
  -> clearance yang diwajibkan
  -> Medication Preparation
  -> pickup call
  -> telaah Pharmacist, pemeriksaan penerima operasional, dan edukasi
  -> Medication Handover atau penyelesaian non-fulfillment yang accountable
```

## 2. Authority Domain dan Referensi

Workflow ini menerapkan spesifikasi domain yang direferensikan. Workflow ini tidak mendefinisikan ulang Ubiquitous Language, Aggregate boundary, Business Rules, state, lifecycle, atau Domain Events.

| Authority | Tanggung jawab yang digunakan workflow ini |
|---|---|
| [Domain Apotek](./apotek-domain.md) | Telaah Resep, Sales Order, waktu pembentukan Invoice, Dispensing, clearance, dispensing, handover, dan kebijakan non-fulfillment. |
| [Domain Apotek — Bahasa Indonesia](./apotek-domain-id.md) | Pendamping semantik yang mudah dibaca manusia untuk domain Apotek canonical. |
| [Domain Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md) | Identitas Pharmacy Queue Entry, Queue Number, Queue Session, `CreatedAt`, `ServedAt`, `DoneAt`, dan lifecycle antrean. |
| [Domain CPOE](../../contexts/cpoe/CPOE-DOMAIN-ID.md) | Resep Elektronik asli dan intent klinisi; CPOE tetap authoritative atas Clinical Order miliknya. |
| [Domain Tata Rekening](../../contexts/TataRekening/02-domain.md) | Financial Charge, Financial Responsibility, koreksi finansial, dan tanggung jawab settlement. |
| Pembahasan bisnis apotek Rawat Jalan yang disetujui | Pengambilan dan mapping antrean, timing payer, tanggung jawab profesional, coordinated pickup, mixed coverage, dan penyelesaian manual obat tidak diambil. |

Authority Inventory, Payment, SEP, Fornas, dan Medication Catalog tetap eksternal meskipun belum ada artifact domain khusus dalam registry.

## 3. Cakupan dan Batasan

### 3.1 Dimulai

Koordinasi Rawat Jalan end-to-end dimulai ketika Pasien memperoleh Nomor Antrean Apotek dengan cara:

- meminta Queue Number secara langsung; atau
- menunjukkan evidence tracker atau registrasi valid yang menyebabkan Queue Entry dibuat.

Telaah Resep untuk Resep Elektronik mungkin sudah dimulai atau selesai sebelum trigger end-to-end ini.

### 3.2 Berakhir

Workflow berakhir ketika setiap medication demand yang dimappingkan ke Pharmacy Queue Entry mencapai salah satu outcome accountable berikut:

- Medication Handover selesai untuk jumlah Dispensing yang dimaksud;
- Accepted Quantity memperoleh Unfulfilled Medication Outcome yang accountable;
- penyelesaian manual obat tidak diambil yang diotorisasi menetapkan `Collection Window Expired` dan seluruh konsekuensi stok serta komersial telah diselesaikan; atau
- resolusi finansial eksternal yang diidentifikasi secara eksplisit masih outstanding dan Sales Order tetap `Active` dengan benar.

### 3.3 Termasuk

- Resep Elektronik yang tersedia sebelum atau setelah Queue Entry dibuat.
- Pencatatan Resep Fisik oleh Staf Apotek.
- Penerimaan atau penolakan Jual Bebas oleh Staf Apotek.
- Tracker Mapping dan Manual Mapping.
- Satu Queue Entry yang dimappingkan ke satu atau beberapa Resep Kerja atau Jual Bebas. Mapping tidak menunjuk Sales Order, Invoice, atau Dispensing.
- Telaah Resep dan Medication Substitution sebelum Sales Order.
- Purchase Confirmation lisan Pasien Umum sebelum Invoice dibentuk.
- Coverage BPJS dari SEP valid dan mapping Fornas authoritative.
- Jumlah yang ditanggung BPJS dan dibayar Pasien secara campuran.
- Payment Clearance, Coverage Clearance, dan Dispense Authorized.
- Pharmacy Reserve (Stock Mutasi ke Dispensing Temporary Unit), Medication Preparation, pickup call, Final Dispense Review, verifikasi operasional Authorized Recipient, Patient Education, Medication Dispense, dan Medication Handover.
- Penanganan kekurangan stok melalui Partial Sales Order atas item yang dapat dipenuhi dan Salinan Resep untuk item resep yang tidak dipenuhi.
- No-Show dan penyelesaian manual obat tidak diambil.

### 3.4 Tidak termasuk

- Screen, field, tindakan save, dan navigasi operator; semuanya milik SOP.
- Aturan saldo dan mutasi Inventory, termasuk eligibility return.
- Pelaksanaan receipt dan settlement pembayaran.
- Pembuatan SEP dan administrasi eligibility BPJS.
- Tata kelola master data Fornas.
- Perubahan Clinical Order dan lifecycle CPOE.
- Medication Administration.
- Backorder, pemilihan sumber stok alternatif, fulfillment routing, dan inter-pharmacy sourcing.

## 4. Partisipan dan Perpindahan Tanggung Jawab

| Partisipan | Tanggung jawab dalam workflow | Kondisi handoff |
|---|---|---|
| Patient or Caregiver | Mengambil Queue Number, memberikan mapping evidence atau Resep Fisik, memberi konfirmasi lisan ketika Patient-payable, membayar ketika diwajibkan, hadir untuk pickup, menerima edukasi, dan menerima obat ketika authorized. | Evidence diberikan, konfirmasi diberikan atau ditolak, Payment Clearance diperoleh, atau Medication Handover selesai. |
| Patient Tracker | Memiliki identitas Pharmacy Queue Entry, Queue Number, dan lifecycle antrean. | `Queue Entry Created`, `Queue Service Started`, `Queue Service Completed`, atau `Queue Entry Withdrawn`. |
| Staf Apotek | Melakukan panggilan antrian, Manual Mapping, mencatat Resep Fisik, menerima atau menolak Jual Bebas, mencatat Pharmacy Queue Close bila entri tidak dilanjutkan ke alur pelayanan obat, mengoordinasikan alokasi, menyampaikan nilai Pasien Umum, membentuk faktur terkonfirmasi, menyiapkan atau meracik obat, menangani kekurangan stok melalui Partial Sales Order dan Salinan Resep, dan melakukan panggilan pengambilan. | `Outpatient Queue Mapped`, `Sales Order Established`, `Invoice Established`, `Medication Prepared`, `Patient Called for Pickup`, atau `Pharmacy Queue Close Recorded`. |
| Pharmacist | Melakukan Telaah Resep, mengotorisasi Medication Substitution yang eligible sebelum Sales Order dibentuk, memverifikasi penerima secara operasional, melakukan Final Dispense Review, mencatat Patient Education Acknowledgement, dan mencatat Collection Window Override ketika Pickup Expired. Verifikasi penerima tidak ditegakkan sistem. | `Telaah Resep Completed`, `Final Dispense Review Completed`, `Patient Education Acknowledged`, `Collection Window Override Recorded`, atau Medication Handover diizinkan selesai. |
| Cashier or Payment Authority | Menerima pembayaran Pasien yang diwajibkan dan memberikan Payment Clearance. | `Payment Clearance Established`. |
| SEP and Fornas Authorities | Memberikan validitas SEP tingkat encounter dan coverage BPJS item-level. | `Coverage Clearance Established` untuk jumlah covered. |
| Stock Ledger | Memiliki Stock Availability, Stock Mutasi, Remove Stock, dan riwayat pergerakan. | `Stock Transferred to Dispensing Temporary Unit`, `Stock Removed from Dispensing Temporary Unit`, atau `Stock Returned to Pharmacy Unit`. |
| Tata Rekening | Memiliki Financial Responsibility dan konsekuensi finansial yang diperlukan ketika obat yang telah dibayar tidak dipenuhi atau diambil. | Credit Note, Refund, atau outcome komersial final lain diberikan. |
| CPOE | Memiliki Resep Elektronik asli yang tidak diubah oleh Apotek. | Resep asli tersedia. |
| Pharmacy Supervisor | Pharmacist yang berwenang menurut kebijakan operasional. Mengotorisasi retur, koreksi, override koleksi kedaluwarsa, dan exception dispensing lain. Penanganan exception berbasis authority; tidak ada ambang persetujuan moneter. | Outcome exception accountable dibentuk. |

## 5. Kondisi Awal dan Trigger

### 5.1 Trigger end-to-end

`Queue Entry Created` untuk Service Point apotek Rawat Jalan memulai koordinasi antrean. Patient Tracker mencatat `CreatedAt` ketika Queue Number diterbitkan.

### 5.2 Trigger medication demand independen

- Resep Elektronik tersedia dari CPOE atau authority clinical order lain.
- Staf Apotek mencatat Resep Fisik yang ditunjukkan.
- Staf Apotek menerima atau menolak Jual Bebas.

Trigger tersebut dapat terjadi sebelum atau setelah Outpatient Queue Mapping berdasarkan `BR-APT-061` dan `BR-APT-062`.

### 5.3 Preconditions

- Setiap Resep mempertahankan sumber authoritative dan association Pasien.
- Resep Fisik dicatat sebelum Telaah Resep.
- Jual Bebas diterima sebelum dapat membentuk Sales Order.
- Tracker Mapping memerlukan bukti sah yang menemukan satu atau beberapa Resep yang sudah ada; proses ini tidak berlaku untuk Jual Bebas.
- Manual Mapping mengharuskan Staf Apotek mengidentifikasi Queue Number dan Resep Kerja atau Jual Bebas yang berlaku.
- Medication Preparation memerlukan Dispensing aktif dan Dispense Authorized sesuai payer.
- Medication Handover memerlukan Prepared Medication, Final Dispense Review berhasil, dan Patient Education Acknowledgement. Jika Pickup Expired, Collection Window Override juga diwajibkan. Verifikasi penerima adalah tanggung jawab operasional Pharmacist dan bukan prasyarat sistem. Nomor telepon penerima dan hubungan dengan Pasien boleh dicatat secara opsional sebagai referensi. Catatan konseling rinci bersifat opsional.

### 5.4 Kondisi penghalang

- Queue Number langsung yang belum teridentifikasi tidak dapat melewati clearance yang bergantung pada mapping.
- Resep dengan Telaah Resep belum selesai tidak dapat membentuk Sales Order.
- Resep rejected atau Jual Bebas declined tidak dapat membentuk Sales Order.
- Jumlah Pasien Umum tidak dapat memulai Medication Preparation tanpa Payment Clearance.
- Jumlah BPJS-covered tidak dapat memulai Medication Preparation tanpa SEP valid, coverage Fornas authoritative, dan Dispense Authorized.
- Identitas obat tidak dapat disubstitusi setelah Sales Order dibentuk.

## 6. Daftar Workflow

| ID | Workflow canonical | Deskripsi outcome Indonesia |
|---|---|---|
| `WF-APT-RJ-001` | Acquire and Map Outpatient Pharmacy Queue | Pharmacy Queue Entry dimappingkan secara accountable ke satu atau beberapa Resep Kerja atau Jual Bebas, atau Staf Apotek mencatat Pharmacy Queue Close dan Patient Tracker menetapkan `Withdrawn`. |
| `WF-APT-RJ-002` | Accept Outpatient Medication Demand | Resep yang diterima membentuk Sales Order dan primary outpatient Dispensing yang traceable, atau memperoleh outcome rejection. |
| `WF-APT-RJ-003` | Fulfill Medication for a General Patient | Obat yang dikonfirmasi lisan dan dibayar disiapkan serta diserahkan, atau memperoleh alternative atau exception outcome yang accountable. |
| `WF-APT-RJ-004` | Fulfill Medication for a BPJS Patient | Obat covered disiapkan tanpa Invoice sebelumnya dan Invoice BPJS hanya dibentuk bersama Medication Handover yang berhasil. |
| `WF-APT-RJ-005` | Fulfill Mixed-Coverage Medication | Item Fornas Not Covered membentuk Patient-Pay Sales Order independen; item Covered tetap pada jalur BPJS; keduanya dapat dikoordinasikan untuk satu pengambilan. |
| `WF-APT-RJ-006` | Coordinate Multiple Medication Demands in One Queue | Beberapa lifecycle demand, Sales Order, invoice, dan Dispensing independen dikoordinasikan menjadi satu pelayanan antrean dan pickup tanpa digabungkan. |
| `WF-APT-RJ-007` | Resolve Uncollected Outpatient Medication | Obat siap yang tidak diambil memperoleh expiry terotorisasi, disposition Inventory, dan resolusi komersial sesuai payer. |

## 7. Spesifikasi Workflow

### WF-APT-RJ-001 — Acquire and Map Outpatient Pharmacy Queue

**Indonesia:** Mengambil dan Memapping Antrean Apotek Rawat Jalan

#### Purpose

melakukan mapping satu Pharmacy Queue Entry dengan seluruh sumber pelayanan obat rawat jalan yang sesuai tanpa menjadikan kedatangan antrean sebagai prasyarat Telaah Resep.

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
- Resep Kerja yang sudah ada, dari Resep Elektronik atau Resep Fisik yang dicatat, atau Jual Bebas bila telah tersedia.
- Resep Fisik atau Jual Bebas bila ditunjukkan di loket.

#### Main Flow

1. Patient Tracker membentuk Pharmacy Queue Entry, menetapkan Queue Number, dan mencatat `CreatedAt`.
2. Ketika bukti tracker atau registrasi yang sah menemukan satu atau beberapa Resep yang sudah ada dan berlaku, Aplikasi Pelayanan Obat membuat Tracker Mapping.
3. Aplikasi Pelayanan Obat membuat catatan mapping tersendiri antara entri antrian dan Resep Kerja yang sesuai untuk setiap Resep yang ditemukan.
4. Untuk setiap Resep Kerja yang telah di-mapping, Aplikasi Pelayanan Obat menampilkan perkembangannya tanpa memindahkan kepemilikan status kepada Sistem Antrian Pasien.
5. Koordinasi antrean menunggu fulfillment sesuai payer sementara Telaah Resep dan pembentukan Sales Order dapat berlanjut secara independen.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Bukti tracker atau registrasi menemukan satu atau beberapa Resep yang sudah ada | Aplikasi Pelayanan Obat | Hubungkan setiap Resep secara otomatis; asosiasi dicatat pada Resep Kerja yang sesuai; panggilan administrasi awal tidak diperlukan. |
| Bukti gagal menemukan Resep | Staf Apotek | Beralih ke Manual Mapping. |
| Queue Number diperoleh langsung | Staf Apotek | Panggil Queue Number untuk identifikasi administratif dan Manual Mapping. |
| Satu Queue Entry mempunyai beberapa applicable demand | Staf Apotek | Mapping setiap demand secara terpisah ke Queue Entry yang sama berdasarkan `BR-APT-084` dan `BR-APT-085`. |
| Queue Entry tidak dilanjutkan ke alur pelayanan obat | Staf Apotek | Catat Pharmacy Queue Close beserta alasan wajib; Patient Tracker menetapkan `Withdrawn` dari `Waiting`. |

Untuk Manual Mapping:

1. Staf Apotek memanggil unresolved Queue Number tanpa memulai pelayanan Patient Tracker.
2. Staf Apotek mengidentifikasi Resep Kerja atau Jual Bebas yang sudah ada, mencatat Resep Fisik yang ditunjukkan (menghasilkan Resep Kerja), atau menilai Jual Bebas.
3. Apotek membentuk Manual Mapping untuk setiap applicable demand yang teridentifikasi.

#### Exception and Compensation Flows

- Jika Jual Bebas ditolak, record Jual Bebas dan Sales Order tidak dibentuk. Staf Apotek boleh mencatat Pharmacy Queue Close beserta alasan wajib; Patient Tracker menetapkan Queue Entry yang masih Waiting menjadi `Withdrawn`. Penutupan tidak boleh menyatakan `In Service` atau `Done`.
- Jika Queue Number tidak dapat dicocokkan dengan Patient Journey atau medication demand yang accountable, Queue Entry tetap unmapped dan tidak dapat memperoleh Dispense Authorized yang bergantung pada mapping. Staf Apotek boleh membiarkannya menunggu evidence atau mencatat Pharmacy Queue Close pada jalur penutupan yang sama.
- Jika mapping antrean salah, Staf Apotek memilih Resep Kerja atau Jual Bebas yang benar dan Aplikasi Pelayanan Obat memperbarui mapping aktif. Riwayat perubahan mapping antrean tidak perlu disimpan. Pembaruan ini tidak mengubah Resep, hasil telaah resep, atau pesanan penjualan apotek.

#### Outcomes and Postconditions

- Berhasil: `Outpatient Queue Mapped` tersedia untuk satu atau beberapa demand.
- Non-completion accountable: Queue Entry tetap unmapped menunggu evidence, atau Pharmacy Queue Close mencatat alasan wajib dan Patient Tracker menetapkan `Withdrawn`.
- Mapping tidak menetapkan `ServedAt`, membentuk Resep Elektronik, atau menyelesaikan Telaah Resep.

#### Domain References

`BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087`, `BR-APT-097`, `BR-APT-143`–`BR-APT-145`; `BR-TRK-026`–`BR-TRK-035`, `BR-TRK-039a`, `BR-TRK-051`, `BR-TRK-052`.

#### Domain Events

- Dikonsumsi: `Queue Entry Created`.
- Dihasilkan atau diamati: `Outpatient Queue Mapped`, `Pharmacy Queue Close Recorded`, `Queue Entry Withdrawn`, `Queue Entry Identified` ketika berlaku secara eksternal.

### WF-APT-RJ-002 — Accept Outpatient Medication Demand

**Indonesia:** Menerima Permintaan Obat Rawat Jalan

#### Purpose

Mengubah Resep yang telah ditelaah atau Jual Bebas yang diterima menjadi Sales Order dan primary outpatient Dispensing yang traceable.

#### Trigger

Resep tersedia atau Jual Bebas ditunjukkan untuk dinilai.

#### Preconditions

- Resep dimiliki CPOE atau authority clinical order accountable lain, atau Staf Apotek berwenang menilai Jual Bebas.
- Resep Fisik telah dicatat sebelum review.
- Untuk Resep, Registration asal tetap aktif.

#### Participants

Pharmacist, Staf Apotek, CPOE or Dokter Penulis Resep.

#### Input Business Facts

- Resep Elektronik atau Resep Fisik yang dicatat.
- Detail Jual Bebas bila berlaku.
- Medication Catalog dan professional acceptance policy.
- Stock Availability sebagai fakta fulfillment eksternal yang tidak menentukan clinical acceptance.

#### Main Flow

1. Untuk Resep, Pharmacist memulai Telaah Resep segera setelah Resep tersedia tanpa menunggu kedatangan Pasien atau Outpatient Queue Mapping.
2. Pharmacist menelaah setiap Baris Resep. Jika diperlukan, Pharmacist melakukan klarifikasi kepada Dokter Penulis Resep di luar sistem; Resep tetap utuh dan review tetap `Under Review`.
3. Pharmacist menetapkan setiap line sebagai diterima sesuai resep, diterima dengan obat pengganti, atau ditolak. Obat yang diterima dicatat pada Sales Order Item; obat pengganti disertai alasan, jumlah terdampak, Pharmacist penanggung jawab, dan referensi ke Baris Resep asli.
4. Pharmacist menyelesaikan Telaah Resep sebagai `Approved`, `Partially Approved`, atau `Rejected`.
5. Untuk Jual Bebas, Staf Apotek menerima atau menolaknya; Resep tidak dibentuk. Konsultasi Pharmacist secara opsional hanya panduan SOP dan tidak dimodelkan sebagai approval.
6. Apotek membentuk Sales Order dari tepat satu completed accepted-demand source dan mempertahankan Source Traceability.
7. Apotek dapat membentuk Invoice beserta Invoice Item-nya dan Dispensing beserta Dispensing Item-nya secara independen dan pada waktu bisnis yang berbeda. Setiap Invoice Item obat dan setiap Dispensing Item mereferensikan tepat satu Sales Order Item yang berlaku.
8. Untuk jalur Rawat Jalan normal dalam Registration aktif, Apotek membentuk satu active primary Dispensing bagi Sales Order aktif.
9. Inventory dapat mencatat Pharmacy Reserve melalui Stock Mutasi sebelum Pasien datang atau queue mapping, sedangkan Medication Preparation menunggu Dispense Authorized yang berlaku.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Semua Baris Resep diterima | Pharmacist | `Approved`; seluruh accepted line dapat membentuk Sales Order. |
| Sebagian line diterima | Pharmacist | `Partially Approved`; hanya Accepted Medication Item masuk Sales Order. |
| Tidak ada line diterima | Pharmacist | `Rejected`; Sales Order tidak dibentuk. |
| Jual Bebas diterima | Staf Apotek | Terima dan bentuk sumber Jual Bebas. |
| Jual Bebas ditolak | Staf Apotek | Jangan membentuk request record atau Sales Order. |
| Iter belum terpakai tetapi Pharmacist menolak honor | Pharmacist | Tolak fulfillment; catat outcome accountable tanpa mengonsumsi Iter. |
| Iter belum terpakai dihormati pada fulfillment | Pharmacist | Lanjutkan fulfillment; sistem mencatat konsumsi Iter. |
| Partial resep atas permintaan Pasien | Staf Apotek | Bentuk Sales Order hanya dengan item terpilih; item dikecualikan tetap pada Resep; terbitkan Salinan Resep bila diperlukan. |
| Partial resep karena Stock Shortage | Staf Apotek | Bentuk Sales Order hanya dengan item yang dapat dipenuhi; item tidak tersedia tetap pada Resep; terbitkan Salinan Resep bila diperlukan. |
| Review profesional diperlukan untuk partial path | Pharmacist | Setujui atau tolak keputusan fulfillment; sistem tidak mensubstitusi atau mengarahkan eksternal secara otomatis. |
| Item Fornas Not Covered | Staf Apotek | Bentuk Patient-Pay Sales Order independen untuk item yang tidak dijamin; item Covered membentuk Sales Order BPJS. |

#### Exception and Compensation Flows

- Stock shortage setelah Sales Order dibentuk tidak mengubah Hasil Telaah Resep. Staf Apotek tidak boleh membuat Backorder atau memilih sumber stok alternatif. Jumlah yang tidak dapat dipenuhi memperoleh Unfulfilled Medication Outcome dan Salinan Resep bila berlaku.
- Partial Prescription Fulfillment sebelum Sales Order dibentuk hanya diizinkan untuk Patient Request, Stock Shortage, atau item Fornas Not Covered. Tidak ada alasan partialitas lain yang diakui.
- Identitas obat pada Sales Order Item yang sudah dibentuk tidak boleh diubah. Jika penggantian diperlukan kemudian, batalkan item atau pesanan yang terdampak, telaah kembali Resep asli, lalu bentuk Sales Order Item baru tanpa mensyaratkan Resep perbaikan atau pengganti.
- Accepted Quantity yang tidak dapat dipenuhi harus mempertahankan `Cancelled`, `Expired`, atau Unfulfilled Medication Outcome lain yang accountable. Apotek Rawat Jalan tidak menahan Backorder.

#### Outcomes and Postconditions

- Berhasil: `Telaah Resep Completed`, `Sales Order Established`, dan `Dispensing Established` diamati bila berlaku.
- Berhasil sebagian: hanya item resep terpilih atau yang dapat dipenuhi masuk Sales Order atas Patient Request atau Stock Shortage; item dikecualikan tetap pada Resep asal dan dapat memperoleh Salinan Resep.
- Rejection: Sales Order tidak tersedia bagi source yang rejected.
- Sales Order bukan Invoice, Dispensing, reservation, atau dispense evidence.

#### Domain References

`BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-054`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, `BR-APT-089`, `BR-APT-105`–`BR-APT-124`; lifecycle Telaah Resep dan Sales Order.

#### Domain Events

- Dikonsumsi: `Clinical Order Created` atau fakta Resep availability authoritative lain.
- Dihasilkan: `Telaah Resep Started`, `Medication Substitution Authorized`, `Telaah Resep Completed`, `Jual Bebas Accepted`, `Sales Order Established`, `Dispensing Established`, `Stock Transferred to Dispensing Temporary Unit` ketika diberikan secara eksternal.

### WF-APT-RJ-003 — Fulfill Medication for a General Patient

**Indonesia:** Memenuhi Obat untuk Pasien Umum

#### Purpose

Memperoleh Purchase Confirmation lisan sebelum Invoice dibentuk, memperoleh pembayaran Pasien, dan menyelesaikan Medication Handover Rawat Jalan yang accountable.

#### Trigger

Outpatient Queue Mapping, Sales Order aktif, Sales Order Item yang berlaku, dan nilai yang harus dibayar Pasien telah tersedia.

#### Preconditions

- Nilai Pasien Umum dihitung dari Sales Order Item dan Pricing Snapshot yang berlaku.
- Invoice belum dibentuk untuk penjualan Patient-payable yang diusulkan.
- Dispensing yang berlaku tersedia atau dapat dibentuk dari Sales Order Item melalui Dispensing Item.

#### Participants

Patient or Caregiver, Staf Apotek, Cashier or Payment Authority, Staf Apotek, Pharmacist, Patient Tracker, Inventory, Tata Rekening.

#### Input Business Facts

- Outpatient Queue Mapping.
- Sales Order dan jumlah Sales Order Item yang harus dibayar Pasien.
- Pricing Snapshot dan nilai yang dihitung.
- Dispensing dan Pharmacy Reserve (Stock Mutasi ke Dispensing Temporary Unit) bila telah tersedia.

#### Main Flow

1. Staf Apotek menerima Pasien dalam interaksi Purchase Confirmation—dalam interaksi loket Manual Mapping bila memungkinkan, atau melalui administrative Queue Number call terpisah setelah Tracker Mapping—dan menyampaikan total yang dihitung secara lisan sebelum Invoice tersedia. Interaksi ini tidak menetapkan `ServedAt` atau `DoneAt`.
2. Pasien memberikan Purchase Confirmation lisan.
3. Staf Apotek membentuk Invoice beserta Invoice Item-nya dari jumlah Sales Order Item yang dikonfirmasi; pembentukan Invoice menjadi bukti yang dapat dipertanggungjawabkan bahwa konfirmasi telah diperoleh dan tidak ada object atau transaksi konfirmasi terpisah.
4. Cashier menerima pembayaran dan memberikan Payment Clearance bagi Invoice.
5. Apotek mengevaluasi evidence keuangan dan coverage sebagai Dispense Authorized untuk jumlah Dispensing yang berlaku.
6. Stock Ledger mencatat Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit ketika Pharmacy Reserve diperlukan dan belum berada di Dispensing Temporary Unit.
7. Staf Apotek memulai Medication Preparation berdasarkan released Dispensing.
8. Apotek mengamati `Medication Preparation Started`; Patient Tracker memasukkan Pharmacy Queue Entry ke In Service dan mencatat `ServedAt`.
9. Staf Apotek menyelesaikan Medication Preparation; Dispensing mencapai `Prepared` ketika pergerakan dispensing yang diperlukan selesai. Stock Ledger tidak beraksi pada `Prepared`; obat tetap dalam Dispensing Temporary Custody.
10. Ketika setiap Dispensing yang dimaksud dalam coordinated handover berstatus `Prepared` atau memiliki exception outcome accountable, Staf Apotek melakukan pickup call.
11. Patient Tracker membuat Pharmacy Queue Entry `Done` dan mencatat `DoneAt` pada waktu pickup call.
12. Dengan Pasien atau caregiver hadir, Pharmacist memverifikasi penerima secara operasional, menyelesaikan Final Dispense Review, dan mencatat Patient Education Acknowledgement dalam interaksi loket yang sama. Review yang lulus menambahkan catatan review immutable dan mengubah Dispensing menjadi `Reviewed`. Sistem mencatat waktu edukasi dan Pharmacist penanggung jawab. Catatan konseling rinci bersifat opsional. Sistem boleh secara opsional mencatat nomor telepon penerima dan hubungan sebagai referensi dan tidak menegakkan validasi identitas. Jika Pickup Expired, Pharmacist berwenang mencatat Collection Window Override beserta alasan sebelum handover.
13. Apotek mencatat Medication Dispense dan menyelesaikan Medication Handover untuk setiap jumlah Dispensing yang berlaku.
14. Medication Handover menyelesaikan jumlah Dispensing dan meminta Remove Stock dari Dispensing Temporary Unit melalui Stock Ledger.
15. Sales Order hanya menjadi `Resolved` ketika seluruh Accepted Quantity dan konsekuensi komersial memiliki outcome final accountable.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Pasien menolak sebelum Invoice dibentuk | Patient | Invoice tidak dibentuk; jumlah yang tidak digunakan di Dispensing Temporary Unit dikembalikan ke Pharmacy Unit melalui Stock Mutasi; jumlah Sales Order Item yang harus dibayar Pasien memperoleh outcome declined atau commercially unallocated yang dapat dipertanggungjawabkan. |
| Nilai yang dihitung berubah sebelum pembentukan | Staf Apotek | Sampaikan nilai revisi dan dapatkan kembali konfirmasi lisan sebelum membentuk Invoice. |
| Beberapa demand memakai satu Queue Entry | Staf Apotek | Terapkan `WF-APT-RJ-006`; pertahankan Sales Order, invoice, dan Dispensing terpisah. |
| Pasien tidak mengambil setelah pickup call | Pharmacy Supervisor | Terapkan `WF-APT-RJ-007`. Queue `DoneAt` tidak dibalik. |
| Pasien tidak mengambil sebelum pickup call | Pharmacy Supervisor | Terapkan `WF-APT-RJ-007`. Queue Entry mungkin masih `In Service`; resolusi itu boleh menyelesaikannya menjadi `Done` dan mencatat `DoneAt`. |
| Pickup Expired | Pharmacist berwenang | Catat Collection Window Override beserta alasan lalu lanjutkan handover, atau terapkan `WF-APT-RJ-007`. |

#### Exception and Compensation Flows

- Jika pembayaran tidak selesai setelah Invoice dibentuk, Medication Preparation tetap terblokir. Invoice hanya dapat `Cancelled` selama lifecycle mengizinkan.
- Jika Invoice issued atau financially cleared memerlukan koreksi, gunakan Financial Adjustment, Credit Note, atau Refund berdasarkan authority Tata Rekening; jangan menggantinya diam-diam.
- Jika shortage terjadi setelah pembayaran, Staf Apotek tidak boleh membuat Backorder atau memilih sumber stok alternatif. Jumlah yang tidak dapat dipenuhi memperoleh Unfulfilled Medication Outcome dan Salinan Resep bila berlaku, plus Credit Note atau Refund berdasarkan authority Tata Rekening. Substitution dilarang karena Sales Order telah tersedia.
- Jika fulfillment tidak dapat selesai, jumlah terdampak memperoleh Unfulfilled Medication Outcome yang accountable dan Tata Rekening menerima konsekuensi finansial yang diperlukan.
- Final Dispense Review yang gagal menambahkan catatan review immutable berisi alasan, Pharmacist penanggung jawab, waktu bisnis efektif, dan jumlah terdampak; mengembalikan Dispensing dari `Prepared` ke `Preparing`; serta mencegah Medication Handover. Setelah koreksi selesai, Dispensing kembali ke `Prepared` dan harus menjalani Final Dispense Review baru.

#### Outcomes and Postconditions

- Selesai berhasil: Invoice financially cleared, Dispensing `Completed`, Medication Handover mencatat waktu efektif dengan referensi penerima opsional, dan seluruh jumlah tetap traceable.
- Pembelian ditolak: Invoice tidak tersedia bagi usulan yang ditolak.
- Paid non-fulfillment atau No-Show: konsekuensi fulfillment dan komersial tetap accountable secara terpisah; Sales Order tetap `Active` sampai keduanya final.
- Queue `Done` tidak membuktikan Medication Handover.

#### Domain References

`BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, `BR-APT-125`–`BR-APT-134`, `BR-APT-138`–`BR-APT-142`; lifecycle Invoice dan Dispensing; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046`.

#### Domain Events

- Dikonsumsi: `Outpatient Queue Mapped`, `Payment Clearance Established`, `Stock Transferred to Dispensing Temporary Unit`.
- Dihasilkan atau diamati: `Invoice Established`, `Invoice Issued`, `Dispense Authorized Evaluated`, `Medication Preparation Started`, `Pharmacy Service Started`, `Medication Prepared`, `Patient Called for Pickup`, `Queue Service Started`, `Queue Service Completed`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Medication Dispensed`, `Medication Handed Over`, `Sales Order Resolved`.

### WF-APT-RJ-004 — Fulfill Medication for a BPJS Patient

**Indonesia:** Memenuhi Obat untuk Pasien BPJS

#### Purpose

Menyiapkan obat covered Rawat Jalan tanpa Invoice sebelumnya atau pembayaran Pasien dan membentuk Invoice BPJS hanya bersama Medication Handover yang berhasil.

#### Trigger

Outpatient Queue Mapping, Sales Order aktif, Dispensing, SEP valid, dan coverage Fornas item-level authoritative telah tersedia.

#### Preconditions

- Pasien memiliki SEP valid untuk encounter terkait.
- Setiap covered quantity didukung mapping Fornas authoritative.
- Purchase Confirmation atau pembayaran Pasien tidak diperlukan untuk covered quantity.
- Invoice BPJS belum dibentuk.

#### Participants

Patient or Caregiver, Staf Apotek, Pharmacist, Patient Tracker, SEP and Fornas Authorities, Inventory, Tata Rekening.

#### Input Business Facts

- Outpatient Queue Mapping.
- Sales Order, jumlah Sales Order Item yang ditanggung, dan Dispensing.
- SEP valid dan coverage Fornas item-level.
- Outcome Stock Availability dan Pharmacy Reserve (Stock Mutasi ke Dispensing Temporary Unit).

#### Main Flow

1. Authority SEP dan Fornas membentuk Coverage Clearance bagi setiap covered quantity.
2. Apotek mengevaluasi evidence keuangan dan coverage sebagai Dispense Authorized bagi jumlah Dispensing terkait tanpa mewajibkan Invoice existing.
3. Stock Ledger mencatat Stock Mutasi dari Pharmacy Unit ke Dispensing Temporary Unit ketika Pharmacy Reserve diperlukan dan belum berada di Dispensing Temporary Unit.
4. Staf Apotek memulai Medication Preparation.
5. `Medication Preparation Started` menyebabkan Patient Tracker mencatat `ServedAt` dan memindahkan Pharmacy Queue Entry ke In Service.
6. Staf Apotek menyelesaikan Medication Preparation; Dispensing mencapai `Prepared` ketika pergerakan dispensing yang diperlukan selesai. Stock Ledger tidak beraksi pada `Prepared`; obat tetap dalam Dispensing Temporary Custody.
7. Ketika setiap Dispensing yang dimaksud dalam coordinated handover berstatus `Prepared` atau memiliki exception outcome accountable, Staf Apotek melakukan pickup call.
8. Patient Tracker mencatat `DoneAt` dan membuat Queue Entry `Done` pada waktu pickup call.
9. Dengan Pasien atau caregiver hadir, Pharmacist memverifikasi penerima secara operasional, menyelesaikan Final Dispense Review, dan mencatat Patient Education Acknowledgement dalam interaksi loket yang sama. Review yang lulus menambahkan catatan review immutable dan mengubah Dispensing menjadi `Reviewed`. Sistem mencatat waktu edukasi dan Pharmacist penanggung jawab. Catatan konseling rinci bersifat opsional. Sistem boleh secara opsional mencatat nomor telepon penerima dan hubungan sebagai referensi dan tidak menegakkan validasi identitas. Jika Pickup Expired, Pharmacist berwenang mencatat Collection Window Override beserta alasan sebelum handover.
10. Sebagai satu hasil bisnis yang dapat dipertanggungjawabkan, Apotek membentuk Invoice BPJS beserta Invoice Item-nya dari jumlah Sales Order Item yang ditanggung, mencatat Medication Dispense, dan menyelesaikan Medication Handover.
11. Medication Handover menyelesaikan setiap jumlah Dispensing yang berlaku dan meminta Remove Stock dari Dispensing Temporary Unit melalui Stock Ledger.
12. Sales Order hanya menjadi `Resolved` ketika setiap Accepted Quantity dan konsekuensi komersial yang diperlukan memiliki outcome final.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Resep Elektronik dan Tracker Mapping berhasil pada jalur normal | Apotek | Satu pemanggilan Rawat Jalan terjadi: pickup call setelah `Prepared`. |
| SEP tidak valid | SEP authority | Coverage Clearance tidak tersedia; Medication Preparation tetap terblokir. |
| Item tidak covered Fornas | Staf Apotek | Arahkan non-covered quantity melalui `WF-APT-RJ-005`. |
| Beberapa demand dimappingkan | Staf Apotek | Terapkan `WF-APT-RJ-006`; pertahankan record terpisah dan satu coordinated pickup. |
| Pasien tidak mengambil setelah pickup call | Pharmacy Supervisor | Terapkan `WF-APT-RJ-007`; Invoice BPJS tidak dibentuk. Queue `DoneAt` tidak dibalik. |
| Pasien tidak mengambil sebelum pickup call | Pharmacy Supervisor | Terapkan `WF-APT-RJ-007`; Invoice BPJS tidak dibentuk. Queue Entry mungkin masih `In Service`; resolusi itu boleh menyelesaikannya menjadi `Done` dan mencatat `DoneAt`. |
| Pickup Expired | Pharmacist berwenang | Catat Collection Window Override beserta alasan lalu lanjutkan handover, atau terapkan `WF-APT-RJ-007`. |

#### Exception and Compensation Flows

- No-Show BPJS sebelum Medication Handover tidak membentuk Invoice dan tidak memerlukan pembatalan Invoice.
- Shortage setelah Sales Order dibentuk tidak mengizinkan Backorder, sumber stok alternatif, atau substitution. Jumlah yang tidak dapat dipenuhi memperoleh Unfulfilled Medication Outcome dan Salinan Resep bila berlaku.
- Final Dispense Review yang gagal menambahkan catatan review immutable, mengembalikan Dispensing dari `Prepared` ke `Preparing`, serta mencegah pembentukan Invoice BPJS dan Medication Handover. Koreksi mengembalikan order ke `Prepared` dan mewajibkan review baru.
- Pharmacy meminta Stock Mutasi dari Dispensing Temporary Unit kembali ke Pharmacy Unit untuk jumlah yang eligible.

#### Outcomes and Postconditions

- Selesai berhasil: pembentukan Invoice BPJS dan Medication Handover menjadi satu outcome accountable; Patient-payable amount nol dan payment disposition `Not Required`.
- Coverage blocked: preparation tidak terjadi bagi uncleared quantity.
- No-Show: Invoice BPJS tidak tersedia; Dispensing dan stok mengikuti `WF-APT-RJ-007`.
- Queue `Done` tetap independen dari selesainya Medication Handover.

#### Domain References

`BR-APT-020`–`BR-APT-026`, `BR-APT-029`–`BR-APT-045`, `BR-APT-066`, `BR-APT-068`–`BR-APT-069`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, `BR-APT-129`–`BR-APT-134`, `BR-APT-138`–`BR-APT-142`; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046`.

#### Domain Events

- Dikonsumsi: `Outpatient Queue Mapped`, `Coverage Clearance Established`, `Stock Transferred to Dispensing Temporary Unit`.
- Dihasilkan atau diamati: `Dispense Authorized Evaluated`, `Medication Preparation Started`, `Pharmacy Service Started`, `Medication Prepared`, `Patient Called for Pickup`, `Queue Service Started`, `Queue Service Completed`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Invoice Established`, `Invoice Issued`, `Medication Dispensed`, `Medication Handed Over`, `Sales Order Resolved`.

### WF-APT-RJ-005 — Fulfill Mixed-Coverage Medication

**Indonesia:** Memenuhi Obat dengan Coverage Campuran

#### Purpose

Memisahkan tanggung jawab komersial BPJS dan Patient-Pay menjadi Sales Order independen sambil mengoordinasikan seluruh jumlah yang dimaksud untuk satu pickup Rawat Jalan.

#### Trigger

Validasi Fornas mengklasifikasikan sebagian item resep sebagai Covered dan sebagian sebagai Not Covered.

#### Preconditions

- SEP valid tersedia untuk encounter.
- Mapping Fornas authoritative mengklasifikasikan setiap item resep sebagai Covered atau Not Covered.
- Item Not Covered tidak dibatalkan secara otomatis.

#### Participants

Patient or Caregiver, Staf Apotek, Cashier or Payment Authority, Pharmacist, Patient Tracker, SEP and Fornas Authorities, Inventory, Tata Rekening.

#### Input Business Facts

- Resep asal dan klasifikasi Fornas per item.
- Sales Order BPJS untuk item Covered, bila dibentuk.
- Patient-Pay Sales Order independen untuk item Not Covered, bila dibentuk.
- SEP valid dan coverage Fornas item-level authoritative.
- Dispensing yang berlaku untuk setiap Sales Order.

#### Main Flow

1. Validasi Fornas mengklasifikasikan setiap item resep sebagai Covered atau Not Covered.
2. Item Covered masuk Sales Order BPJS dan mengikuti `WF-APT-RJ-004`. Evidence coverage cukup untuk Dispense Authorized pada item tersebut.
3. Item Not Covered tidak tetap pada jalur BPJS. Staf Apotek boleh membentuk Patient-Pay Sales Order terpisah untuk item tersebut sebagai Partial Prescription Fulfillment.
4. Patient-Pay Sales Order mengikuti `WF-APT-RJ-003`: Purchase Confirmation lisan, Invoice Pasien Umum, dan Payment Clearance. Payment Clearance wajib sebelum Dispense Authorized pada item Patient-Pay.
5. Setiap item diotorisasi secara independen: item Covered → Coverage Evidence → Dispense Authorized; item Patient-Pay → Payment Clearance → Dispense Authorized.
6. Setelah setiap jumlah yang hendak diserahkan memperoleh Dispense Authorized dari jalurnya sendiri, Staf Apotek memulai dan menyelesaikan Medication Preparation pada setiap Dispensing yang berlaku.
7. `Medication Preparation Started` pertama mencatat `ServedAt` Patient Tracker; setiap intended Dispensing mencapai `Prepared` sebelum pickup.
8. Staf Apotek melakukan satu coordinated pickup call; Patient Tracker mencatat `DoneAt`.
9. Dengan Pasien atau caregiver hadir, Pharmacist memverifikasi penerima secara operasional, menyelesaikan Final Dispense Review untuk setiap Dispensing yang `Prepared`, dan mencatat Patient Education Acknowledgement. Sistem mencatat waktu edukasi dan Pharmacist penanggung jawab. Catatan konseling rinci bersifat opsional. Sistem boleh secara opsional mencatat nomor telepon penerima dan hubungan sebagai referensi dan tidak menegakkan validasi identitas.
10. Apotek membentuk Invoice BPJS hanya ketika Medication Handover Sales Order BPJS berhasil. Invoice Patient-Pay sudah tersedia dan financially cleared.
11. Apotek mencatat Medication Dispense dan Medication Handover bagi seluruh jumlah yang berlaku serta meminta Remove Stock dari Dispensing Temporary Unit.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Pasien mengonfirmasi Patient-Pay Sales Order | Patient | Bentuk dan terima pembayaran Invoice Pasien Umum; koordinasikan kedua Sales Order untuk pickup. |
| Pasien menolak Patient-Pay Sales Order sebelum invoice dibentuk | Patient | Jangan membentuk Invoice Pasien Umum; berikan outcome declined yang accountable kepada Patient-Pay Sales Order; lanjutkan Sales Order BPJS secara independen. |
| Semua item Covered | Apotek | Tetap pada `WF-APT-RJ-004`; jangan membentuk Patient-Pay Sales Order. |
| Tidak semua intended quantity memperoleh Dispense Authorized | Apotek | Jangan memulai coordinated preparation bagi quantity yang belum authorized dan jangan melakukan pickup call. |

#### Exception and Compensation Flows

- Invoice Pasien Umum yang telah dibentuk mengikuti aturan cancellation dan correction Pasien Umum; Invoice BPJS tetap belum ada sampai handover Sales Order BPJS.
- No-Show setelah pembayaran mengikuti jalur komersial paid General Patient sedangkan Invoice BPJS yang belum ada mengikuti jalur BPJS uninvoiced.
- Setiap Sales Order mempertahankan konsekuensi komersial dan fulfillment independen.
- Substitution dilarang setelah Sales Order dibentuk.
- Final Dispense Review yang gagal menambahkan catatan review immutable, mengembalikan hanya Dispensing terdampak dari `Prepared` ke `Preparing`, dan tidak menulis ulang Sales Order lain.

#### Outcomes and Postconditions

- Berhasil: Sales Order BPJS dan Patient-Pay Sales Order tersedia untuk item berbeda dari Resep yang sama; Invoice terpisah merepresentasikan setiap jalur payer; satu Medication Handover terkoordinasi dapat menyelesaikan keduanya.
- Pasien menolak jumlah Patient-Pay: Sales Order BPJS dapat selesai independen.
- Konsekuensi komersial unresolved membuat Sales Order miliknya tetap `Active`.

#### Domain References

`BR-APT-011`, `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, `BR-APT-090`–`BR-APT-096`, `BR-APT-108`, `BR-APT-119`–`BR-APT-124`, `BR-APT-129`–`BR-APT-134`.

#### Domain Events

- Dikonsumsi: `Coverage Clearance Established`, `Payment Clearance Established`, `Outpatient Queue Mapped`.
- Dihasilkan atau diamati: `Sales Order Established`, `Invoice Established`, `Invoice Issued`, `Dispense Authorized Evaluated`, `Medication Preparation Started`, `Medication Prepared`, `Patient Called for Pickup`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Medication Dispensed`, `Medication Handed Over`, `Sales Order Resolved` ketika seluruhnya direkonsiliasi.

### WF-APT-RJ-006 — Coordinate Multiple Medication Demands in One Queue

**Indonesia:** Mengoordinasikan Beberapa Permintaan Obat dalam Satu Antrean

#### Purpose

Mengoordinasikan dua atau lebih medication demand yang accountable secara independen dalam satu antrean dan pickup session Rawat Jalan tanpa menggabungkan business record masing-masing.

#### Trigger

Satu Pharmacy Queue Entry dimappingkan ke dua atau lebih Resep Kerja atau Jual Bebas.

#### Preconditions

- Setiap demand memiliki Outpatient Queue Mapping terpisah menuju Queue Entry yang sama. Target mapping hanya Resep Kerja atau Jual Bebas.
- Setiap Resep mengikuti Telaah Resep sendiri.
- Setiap accepted source membentuk Sales Order dan active primary outpatient Dispensing sendiri.

#### Participants

Staf Apotek, Pharmacist, Patient or Caregiver, Patient Tracker, Cashier or Payment Authority, SEP and Fornas Authorities, Inventory.

#### Input Business Facts

- Satu Pharmacy Queue Entry.
- Dua atau lebih medication demand yang dimappingkan.
- Perkembangan setiap demand untuk Sales Order, Invoice, clearance, dan Dispensing, termasuk hubungan pada tingkat itemnya.

#### Main Flow

1. Apotek mempertahankan Outpatient Queue Mapping terpisah bagi setiap sumber pelayanan obat yang di-mapping dengan Pharmacy Queue Entry yang sama.
2. Setiap demand berjalan secara independen melalui Telaah Resep atau penerimaan langsung, pembentukan Sales Order, penagihan komersial, perencanaan pemenuhan, dan clearance payer.
3. Queue-facing view memproyeksikan progress authoritative setiap mapped demand tanpa memiliki state tersebut.
4. `Medication Preparation Started` pertama yang berlaku menyebabkan Patient Tracker mencatat satu `ServedAt` bagi Queue Entry yang sama.
5. Staf Apotek menunggu sampai setiap Dispensing yang dimaksud untuk pickup berstatus `Prepared` atau memiliki exception outcome accountable.
6. Staf Apotek melakukan satu coordinated pickup call; Patient Tracker mencatat satu `DoneAt` bagi Queue Entry yang sama.
7. Dengan Pasien atau caregiver hadir, Pharmacist memverifikasi penerima secara operasional, menyelesaikan Final Dispense Review bagi setiap Prepared Medication, dan mencatat Patient Education Acknowledgement untuk sesi terkoordinasi. Setiap review yang lulus menambahkan catatan review immutable dan mengubah Dispensing terkait menjadi `Reviewed`. Sistem mencatat waktu edukasi dan Pharmacist penanggung jawab. Catatan konseling rinci bersifat opsional. Sistem boleh secara opsional mencatat nomor telepon penerima dan hubungan sebagai referensi dan tidak menegakkan validasi identitas.
8. Apotek mencatat Medication Dispense dan Medication Handover terhadap setiap Dispensing dan Sales Order yang berlaku secara terpisah.

#### Decision and Alternative Flows

| Kondisi | Pemilik keputusan | Cabang |
|---|---|---|
| Setiap included demand siap | Staf Apotek | Lakukan satu coordinated pickup call dan handover session. |
| Satu demand memiliki exception outcome accountable | Staf Apotek | Sertakan resolved exception dalam komunikasi gabungan dan lanjutkan ready demand ketika payer rule mengizinkan. |
| Satu demand masih unresolved | Staf Apotek | Jangan nyatakan demand tersebut ready; tunda coordinated call kecuali Pasien menerima partial path accountable yang diizinkan payer workflow. |
| Demand memiliki klasifikasi payer berbeda | Staf Apotek | Terapkan workflow General, BPJS, atau mixed coverage yang berlaku kepada setiap demand sebelum koordinasi. |

#### Exception and Compensation Flows

- Koreksi satu mapping tidak boleh menulis ulang riwayat demand lain.
- Cancellation, expiry, unfulfilled shortage, financial correction, dan return tetap melekat pada Sales Order dan Dispensing sumbernya.
- Medication Handover yang berhasil bagi satu demand tidak boleh disimpulkan memenuhi mapped demand lain tanpa handover fact masing-masing.
- Final Dispense Review yang gagal menambahkan catatan review immutable dan hanya mengembalikan Dispensing sumbernya dari `Prepared` ke `Preparing`. Demand tersebut tidak boleh diserahkan sampai koreksi mengembalikannya ke `Prepared` dan review baru lulus; demand lain tetap accountable secara independen menurut workflow payer-nya.

#### Outcomes and Postconditions

- Satu Queue Entry memiliki satu `CreatedAt`, maksimal satu `ServedAt`, dan satu `DoneAt`.
- Setiap Resep, Sales Order, Invoice, dan Dispensing mempertahankan identitas dan lifecycle independen.
- Satu pickup call dan interaksi loket dapat mengoordinasikan beberapa fakta Medication Handover accountable.

#### Domain References

`BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-129`–`BR-APT-134`; `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, `BR-TRK-045a`.

#### Domain Events

- Dikonsumsi: `Outpatient Queue Mapped`, `Medication Preparation Started`, `Medication Prepared`.
- Dihasilkan atau diamati: `Queue Service Started`, `Patient Called for Pickup`, `Queue Service Completed`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Medication Dispensed`, `Medication Handed Over` bagi setiap demand yang berlaku.

### WF-APT-RJ-007 — Resolve Uncollected Outpatient Medication

**Indonesia:** Menyelesaikan Obat Rawat Jalan yang Tidak Diambil

#### Purpose

Memberikan expiry terotorisasi, disposition stok, dan outcome komersial sesuai payer kepada Prepared Medication yang tidak diambil. Pickup Expired tidak dengan sendirinya meng-expire Dispensing.

#### Trigger

Pharmacy Supervisor atau Pharmacist berwenang lain menurut kebijakan operasional secara manual menetapkan bahwa kesempatan pengambilan telah berakhir bagi Prepared Medication yang belum diserahkan. Pickup Expired (Collection Window habis; default 7 hari) tidak dengan sendirinya meng-expire Dispensing. Otorisasi berbasis authority; tidak ada ambang persetujuan moneter.

#### Preconditions

- Obat tetap `Prepared` dalam Dispensing Temporary Custody dan Medication Handover belum selesai.
- Pasien tidak mengambil obat.
- Authority penutupan manual dan effective business time diketahui.

#### Participants

Pharmacy Supervisor, Staf Apotek, Inventory, Tata Rekening, Patient Tracker.

#### Input Business Facts

- Pharmacy Queue Entry yang mungkin sudah `Done` setelah pickup call, atau masih `In Service` jika pickup call belum terjadi.
- Sales Order, Dispensing, dan unresolved quantity.
- Keberadaan Invoice dan financial disposition sesuai payer.
- State Dispensing `Prepared` dan jumlah yang ditahan di Dispensing Temporary Unit.

#### Main Flow

1. Peran authorized melakukan penyelesaian manual obat tidak diambil dan mencatat Pasien sebagai No-Show untuk fulfillment terkait.
2. Jika Queue Entry terkait masih `In Service` karena pickup call belum terjadi, Apotek boleh menyelesaikan Queue Entry itu. Patient Tracker memindahkannya ke `Done` dan mencatat `DoneAt`. Jika Queue Entry sudah `Done`, Patient Tracker mempertahankan `DoneAt`. `DoneAt` tidak pernah dibalik. `Done` pada antrean tidak berarti Medication Handover dan tidak menambahkan state alur kerja farmasi ke Queue Entry. Ini bukan Pharmacy Queue Close.
3. Apotek memberikan terminal state `Expired` kepada setiap Dispensing terdampak.
4. Resolution mempertahankan reason `Collection Window Expired`, responsible party, effective business time, affected quantity, dan Source Traceability.
5. Apotek mencatat penyelesaian No Show dan meminta Stock Mutasi dari Dispensing Temporary Unit kembali ke Pharmacy Unit untuk jumlah yang eligible.
6. Apotek mencatat Unfulfilled Medication Outcome yang dihasilkan bagi setiap affected quantity.
7. Apotek menyelesaikan konsekuensi komersial sesuai payer.
8. Sales Order hanya menjadi `Resolved` setelah setiap Accepted Quantity dan konsekuensi komersial yang diperlukan memiliki outcome final accountable.

#### Decision and Alternative Flows

| Kondisi payer | Outcome komersial |
|---|---|
| Invoice BPJS belum dibentuk karena handover gagal | Jangan membentuk atau membatalkan invoice; selesaikan fulfillment dan Inventory saja, lalu resolve Sales Order ketika seluruh outcome final. |
| Invoice Pasien Umum telah dibayar | Tata Rekening atau financial authority yang bertanggung jawab memberikan Credit Note, Refund, atau outcome final lain; Sales Order tetap `Active` sampai saat itu. |
| Usulan Pasien Umum ditolak sebelum invoice dibentuk | Invoice tidak tersedia; selesaikan jumlah Pharmacy Reserve yang tidak digunakan melalui Stock Mutasi dan commercially unallocated quantity. |
| Mixed coverage | Selesaikan konsekuensi jumlah yang ditanggung tetapi belum ditagihkan dan jumlah yang dibayar Pasien secara terpisah melalui hubungan Sales Order Item dan Invoice Item masing-masing. |

#### Exception and Compensation Flows

- Collection Window (default 7 hari) mengklasifikasikan Ready for Pickup menjadi Pickup Expired bila habis. Klasifikasi itu tidak meng-expire Dispensing. Penutupan terminal obat tidak diambil tetap aktivitas manual yang diotorisasi. Tidak ada ambang persetujuan moneter.
- Queue `DoneAt` tidak pernah dibalik. Queue Entry mungkin sudah `Done` sebelum workflow ini, atau workflow ini boleh menyelesaikan Queue Entry `In Service` menjadi `Done` dan mencatat `DoneAt`. Status antrean baru tidak diperkenalkan. Pharmacy Queue Close (`Withdrawn` dari `Waiting`) tidak berlaku setelah `Medication Preparation Started`.
- Inventory dapat menolak Return to Stock berdasarkan kebijakannya; return yang ditolak tetap memerlukan disposition Inventory final accountable.
- Konsekuensi paid tidak boleh dihapus diam-diam atau diperlakukan sebagai jalur BPJS uninvoiced.

#### Outcomes and Postconditions

- No-Show BPJS: Dispensing `Expired`, Invoice BPJS tidak tersedia, dan stok mempunyai disposition accountable.
- No-Show Pasien Umum paid: Dispensing `Expired`; Sales Order tetap `Active` sampai outcome finansial final.
- Antrean: Queue Entry terkait berstatus `Done`. `DoneAt` dicatat pada pickup call atau pada resolusi ini dan tidak dibalik. `Done` pada antrean tidak membuktikan Medication Handover.
- Final resolution: Sales Order `Resolved` dengan reason `Collection Window Expired` setelah seluruh konsekuensi fulfillment dan komersial final.

#### Domain References

`BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-069`, `BR-APT-078`–`BR-APT-080`, `BR-APT-095`, `BR-APT-135`–`BR-APT-142`; lifecycle Dispensing, quantity, pickup, dan Sales Order.

#### Domain Events

- Dikonsumsi: `Patient Called for Pickup` jika pickup call sudah terjadi, `Medication Prepared`.
- Dihasilkan atau diamati: `Outpatient No-Show Recorded`, `Queue Service Completed` ketika workflow ini menyelesaikan Queue Entry yang masih `In Service`, `Dispensing Expired`, `Unfulfilled Medication Recorded`, `Medication Returned`, `Invoice Credited`, `Refund Required`, `Sales Order Resolved` ketika seluruhnya direkonsiliasi.

## 8. Handoff Lintas Context

| Dari | Fakta bisnis authoritative | Kepada | Tanggung jawab hasil handoff |
|---|---|---|---|
| Patient Tracker | `Queue Entry Created`, Queue Number, `CreatedAt` | Apotek | Membentuk Tracker Mapping atau Manual Mapping tanpa mengambil ownership identitas antrean. |
| Apotek | `Medication Preparation Started` | Patient Tracker | Memindahkan Pharmacy Queue Entry ke In Service dan mencatat `ServedAt`; Patient Tracker tidak boleh menyimpulkan Medication Handover. |
| Apotek | `Patient Called for Pickup` | Patient Tracker | Membuat Pharmacy Queue Entry `Done` dan mencatat `DoneAt` jika belum dicatat; telaah profesional dan handover berikutnya tetap menjadi fakta Apotek. `Done` pada antrean tidak berarti Medication Handover. |
| Apotek | `Outpatient No-Show Recorded` ketika Queue Entry masih `In Service` | Patient Tracker | Membuat Pharmacy Queue Entry `Done` dan mencatat `DoneAt` jika belum dicatat; jangan membalik `DoneAt`; jangan menyimpulkan Medication Handover. Ini penyelesaian antrean, bukan Pharmacy Queue Close, dan tidak menambahkan state farmasi ke antrean. |
| Apotek | `Pharmacy Queue Close Recorded` | Patient Tracker | Membuat Waiting Pharmacy Queue Entry menjadi `Withdrawn`; jangan mencatat `ServedAt` atau `DoneAt`. |
| CPOE atau clinical-order authority | Resep availability dan clinician intent | Apotek | Melakukan Telaah Resep tanpa mengubah Resep asli. |
| Apotek | Fulfillment projection per Resep dan realisasi Medication Handover | Pelaporan EMR | Menampilkan informasi Resep-to-realization tanpa mengubah Clinical Order CPOE pada scope awal. |
| SEP authority | SEP valid | Apotek | Menilai coverage BPJS tingkat encounter; SEP saja tidak mengidentifikasi covered medication quantity. |
| Fornas authority | Mapping coverage item-level | Apotek | Membentuk Coverage Clearance hanya bagi covered quantity yang berlaku bersama SEP valid. |
| Cashier atau Payment authority | `Payment Clearance Established` | Apotek | Mengevaluasi Dispense Authorized dari evidence pembayaran; payment tidak membuktikan stok atau handover. |
| Stock Ledger | Stock Availability dan `Stock Transferred to Dispensing Temporary Unit` | Apotek | Mencatat Mutasi dan Remove Stock hanya dari permintaan yang diotorisasi Pharmacy; fakta stok tidak menulis ulang Telaah Resep. |
| Apotek | Permintaan handover, expiry, shortage, atau pengembalian No Show | Stock Ledger | Mencatat Remove Stock atau Mutasi pengembalian; Apotek tidak boleh menyimpulkan pergerakan stok tanpa outcome Stock Ledger. |
| Apotek | Kebutuhan Financial Charge, Credit Note, atau Refund | Tata Rekening | Menyelesaikan Financial Responsibility dan konsekuensi settlement tanpa mengubah riwayat fulfillment. |

## 9. Waktu Bisnis dan Batas Layanan

| Fakta timing | Aturan authoritative |
|---|---|
| Pharmacy `CreatedAt` | Dicatat ketika Patient Tracker menerbitkan Queue Number. |
| Pharmacy `ServedAt` | Dicatat ketika Dispensing pertama yang berlaku menghasilkan `Medication Preparation Started`. |
| Pharmacy `DoneAt` | Dicatat ketika penyelesaian antrean terjadi: Staf Apotek melakukan coordinated pickup call, atau penyelesaian No Show menyelesaikan Queue Entry yang masih `In Service`. `DoneAt` tidak pernah dibalik. `Done` pada antrean tidak membuktikan Medication Handover. |
| Telaah Resep | Dapat dimulai segera setelah Resep tersedia; tidak menunggu kedatangan atau mapping Pasien. |
| Preparation Pasien Umum | Tidak dapat dimulai sebelum Payment Clearance dan evidence Invoice yang berlaku memenuhi Dispense Authorized. |
| Preparation BPJS | Tidak dapat dimulai sebelum SEP valid, mapping Fornas covered, dan Dispense Authorized. Invoice tidak diwajibkan. |
| Pickup call | Hanya terjadi setelah setiap Dispensing yang dimaksud untuk handover berstatus `Prepared` atau mempunyai exception outcome accountable. |
| Final Dispense Review dan edukasi | Terjadi dengan Pasien atau caregiver hadir setelah pickup call dan sebelum Medication Handover. Patient Education Acknowledgement mencatat waktu edukasi dan Pharmacist penanggung jawab; catatan rinci bersifat opsional. Verifikasi penerima bersifat operasional dan bukan timing gate sistem. |
| Invoice BPJS | Hanya dibentuk bersama Medication Handover yang berhasil. |
| Batas pengambilan | Collection Window yang dapat dikonfigurasi, default 7 hari, dimulai ketika Dispensing pertama kali menjadi Ready for Pickup. Setelah itu kategori worklist adalah Pickup Expired. Handover biasa memerlukan Collection Window Override. Penyelesaian terminal obat tidak diambil adalah kegiatan manual yang diotorisasi dengan alasan `Collection Window Expired`. |

Technical timeout, polling, retry, dan performa aplikasi berada di luar workflow ini.

## 10. Keterlacakan

`Domain References` pada spesifikasi masing-masing workflow adalah sumber acuan. Kolom `Domain rules` di bawah merupakan proyeksi yang harus sama persis dengan referensi tersebut, termasuk aturan Patient Tracker bila dirujuk.

| Workflow ID | Domain rules | States | Domain Events | External authority |
|---|---|---|---|---|
| `WF-APT-RJ-001` | `BR-APT-061`–`BR-APT-065`, `BR-APT-082`, `BR-APT-084`–`BR-APT-087`, `BR-APT-097`, `BR-APT-143`–`BR-APT-145`; `BR-TRK-026`–`BR-TRK-035`, `BR-TRK-039a`, `BR-TRK-051`, `BR-TRK-052` | `Unmapped`, `Mapped`, `Waiting`, `Withdrawn` | `Queue Entry Created`, `Outpatient Queue Mapped`, `Pharmacy Queue Close Recorded`, `Queue Entry Withdrawn`, `Queue Entry Identified` | Patient Tracker |
| `WF-APT-RJ-002` | `BR-APT-001`–`BR-APT-019`, `BR-APT-029`–`BR-APT-034`, `BR-APT-050`, `BR-APT-054`, `BR-APT-061`, `BR-APT-068`, `BR-APT-083`, `BR-APT-086`, `BR-APT-089`, `BR-APT-105`–`BR-APT-124` | `Available`, `Under Review`, `Approved`, `Partially Approved`, `Rejected`, `Established`, `Active` | `Telaah Resep Started`, `Medication Substitution Authorized`, `Telaah Resep Completed`, `Jual Bebas Accepted`, `Sales Order Established`, `Dispensing Established` | CPOE, Medication Catalog, Inventory |
| `WF-APT-RJ-003` | `BR-APT-020`–`BR-APT-028`, `BR-APT-033`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-067`–`BR-APT-072`, `BR-APT-076`–`BR-APT-083`, `BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, `BR-APT-125`–`BR-APT-134`, `BR-APT-138`–`BR-APT-142`; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046` | `Established`, `Issued`, `Financially Cleared`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `In Service`, `Done` | `Invoice Established`, `Payment Clearance Established`, `Medication Preparation Started`, `Medication Prepared`, `Patient Called for Pickup`, `Final Dispense Review Completed`, `Final Dispense Review Failed`, `Medication Handed Over` | Patient Tracker, Payment, Inventory, Tata Rekening |
| `WF-APT-RJ-004` | `BR-APT-020`–`BR-APT-026`, `BR-APT-029`–`BR-APT-045`, `BR-APT-066`, `BR-APT-068`–`BR-APT-069`, `BR-APT-073`–`BR-APT-079`, `BR-APT-081`–`BR-APT-083`, `BR-APT-088`, `BR-APT-090`, `BR-APT-095`–`BR-APT-096`, `BR-APT-114`–`BR-APT-118`, `BR-APT-129`–`BR-APT-134`, `BR-APT-138`–`BR-APT-142`; `BR-TRK-045`, `BR-TRK-045a`, `BR-TRK-046` | `Awaiting Clearance`, `Released`, `Preparing`, `Prepared`, `Reviewed`, `Completed`, `In Service`, `Done` | `Coverage Clearance Established`, `Medication Preparation Started`, `Patient Called for Pickup`, `Final Dispense Review Failed`, `Invoice Established`, `Medication Handed Over` | Patient Tracker, SEP, Fornas, Inventory, Tata Rekening |
| `WF-APT-RJ-005` | `BR-APT-011`, `BR-APT-015`, `BR-APT-020`–`BR-APT-028`, `BR-APT-040`–`BR-APT-046`, `BR-APT-056`–`BR-APT-060`, `BR-APT-070`–`BR-APT-078`, `BR-APT-090`–`BR-APT-096`, `BR-APT-108`, `BR-APT-119`–`BR-APT-124`, `BR-APT-129`–`BR-APT-134` | State Sales Order BPJS dan Patient-Pay independen | `Coverage Clearance Established`, `Payment Clearance Established`, `Sales Order Established`, `Final Dispense Review Failed`, `Invoice Established`, `Medication Handed Over` | SEP, Fornas, Payment, Tata Rekening |
| `WF-APT-RJ-006` | `BR-APT-011`, `BR-APT-015`, `BR-APT-022`, `BR-APT-030`, `BR-APT-056`–`BR-APT-060`, `BR-APT-084`–`BR-APT-088`, `BR-APT-095`–`BR-APT-096`, `BR-APT-129`–`BR-APT-134`; `BR-TRK-032`, `BR-TRK-035`–`BR-TRK-039`, `BR-TRK-045`, `BR-TRK-045a` | State authoritative per demand; satu antrean `Waiting` → `In Service` → `Done` | `Outpatient Queue Mapped`, `Medication Preparation Started`, `Patient Called for Pickup`, `Final Dispense Review Failed`, `Medication Handed Over` | Patient Tracker |
| `WF-APT-RJ-007` | `BR-APT-018`–`BR-APT-019`, `BR-APT-027`, `BR-APT-045`–`BR-APT-047`, `BR-APT-052`–`BR-APT-060`, `BR-APT-069`, `BR-APT-078`–`BR-APT-080`, `BR-APT-095`, `BR-APT-135`–`BR-APT-142` | `Expired`, `Active`, `Resolved`; antrean `In Service` atau `Done` | `Outpatient No-Show Recorded`, `Queue Service Completed`, `Dispensing Expired`, `Unfulfilled Medication Recorded`, `Medication Returned`, `Invoice Credited`, `Refund Required`, `Sales Order Resolved` | Inventory, Tata Rekening, Patient Tracker |

Artifact canonical terkait:

- [Domain Apotek](./apotek-domain.md)
- [Domain Apotek — Bahasa Indonesia](./apotek-domain-id.md)
- [Outpatient Apotek Workflow — English](./outpatient-apotek-workflow.md)
- [Domain Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md)
- [Domain CPOE](../../contexts/cpoe/CPOE-DOMAIN-ID.md)
- [Domain Tata Rekening](../../contexts/TataRekening/02-domain.md)

Tujuh pasang spesifikasi operasional Rawat Jalan tercantum dalam [Indeks SOP Outpatient Apotek](./sop/DAFTAR-SOP-APT-RJ.md). Belum ada artifact integration/architecture khusus yang direferensikan oleh workflow pada revisi ini.
