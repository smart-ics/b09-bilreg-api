# Domain Apotek Rawat Jalan

**Status artefak:** Spesifikasi bisnis kanonis

**Bounded context:** Apotek Rawat Jalan (`Apotek Rajal`)

**Cakupan versi:** Alur bisnis target

**Sumber kanonis bahasa Inggris:** [apotek-rajal-domain.md](./apotek-rajal-domain.md)

**Context bisnis terkait:** [Patient Tracker](../../contexts/pasien-tracker/TRACKER-DOMAIN-ID.md), [Tata Rekening](../../contexts/TataRekening/02-domain.md), [CPOE](../../contexts/cpoe/CPOE-DOMAIN-ID.md), [Medication Fulfillment](./medication-fulfillment-domain.md)

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan dan nilai bisnis

Apotek Rawat Jalan mengubah permintaan obat yang eligible menjadi obat siap serah, Penjualan yang dapat dipertanggungjawabkan, dan Serah Obat yang aman. Domain ini mengoordinasikan telaah resep, mapping antrean ke permintaan, alokasi stok, clearance komersial sesuai penjamin, dispensing fisik, telaah obat akhir, edukasi Pasien, dan penyelesaian no-show.

Bisnis harus memastikan bahwa:

- hanya obat yang dapat diterima secara klinis dan tersedia yang dipenuhi;
- setiap Penjualan berasal dari obat yang benar-benar eligible untuk dispensing;
- tidak ada user yang menginput Penjualan atau `DU` legacy secara manual;
- Pasien Umum membayar sebelum dispensing fisik;
- Pasien BPJS tidak diminta melakukan konfirmasi pembelian maupun pembayaran Pasien; dan
- obat tidak dinyatakan telah diserahkan kecuali penerima hadir dan Serah Obat dapat dipertanggungjawabkan.

### 1.2 Cakupan

Spesifikasi ini mencakup pelayanan apotek rawat jalan yang berasal dari:

1. Electronic Prescription yang dibentuk oleh poli atau unit layanan lain;
2. Physical Prescription yang dicatat oleh Pharmacy Staff; atau
3. Direct Sale Request untuk obat yang dijual tanpa Prescription.

Cakupan dimulai dari tersedianya Prescription atau permintaan, kemudian Order Dispensing, pembentukan Sale, dispensing, Medicine Handover, hingga penyelesaian no-show.

### 1.3 Batas bisnis dan context terkait

Apotek Rawat Jalan memiliki Prescription Review Outcome, Order Dispensing, waktu pembentukan Sale sesuai penjamin, outcome dispensing, Medicine Review, Medicine Handover, dan penyelesaian no-show apotek.

Domain ini bergantung pada context bisnis lain tanpa mengambil alih kewenangannya:

- context peresepan atau clinical order memiliki maksud klinis asli pada Electronic Prescription;
- Patient Tracker memiliki identitas antrean apotek, Queue Session, Queue Number, dan lifecycle operasional antrean;
- Apotek Rawat Jalan memiliki keputusan bisnis untuk memetakan Pharmacy Queue Entry ke Prescription atau Direct Sale Request yang berlaku;
- pemilik inventory tetap authoritative atas saldo stok fisik dan mencatat outcome reserve, In-Transit, pengeluaran, serta pengembalian yang diminta alur ini;
- aktivitas kasir/pembayaran memiliki penerimaan pembayaran Pasien Umum;
- Tata Rekening memiliki tanggung jawab finansial Pasien yang lebih luas, alokasi penjamin, finalisasi, dan settlement; dan
- kebijakan eligibility serta klaim BPJS tetap authoritative di luar bounded context ini.

**Kebutuhan alignment yang diketahui:** Aturan Patient Tracker `BR-TRK-045` saat ini menganggap pembentukan Sale yang telah dikonfirmasi sebagai bukti mulai layanan apotek. Interpretasi tersebut tetap berlaku untuk alur Umum, tetapi tidak dapat diterapkan tanpa perubahan pada BPJS karena BPJS Sale baru dibentuk saat Medicine Handover berhasil. Semantik milestone lintas-context harus direkonsiliasi dengan `BR-APR-045` sebelum perubahan implementasi dianggap lengkap.

### 1.4 Penekanan domain yang wajib

Empat fakta menjadi inti domain ini:

1. Prescription Review dan penerimaan antrean apotek merupakan dua alur independen.
2. Order Dispensing adalah satu-satunya dasar bisnis bagi setiap Sale.
3. Sale Umum dan BPJS terbentuk pada momen bisnis yang berbeda.
4. No-show BPJS sebelum Medicine Handover mengembalikan stok tanpa membatalkan Sale karena Sale belum terbentuk.

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| Outpatient Pharmacy Service | Pelayanan Apotek Rawat Jalan | Seluruh tanggung jawab pemenuhan obat rawat jalan sejak adanya permintaan eligible sampai Medicine Handover atau outcome tidak terpenuhi yang dapat dipertanggungjawabkan. |
| Electronic Prescription | Resep Elektronik | Prescription yang dibentuk oleh poli atau unit layanan lain dan tersedia untuk pekerjaan apotek tanpa menunggu kedatangan Pasien. |
| Physical Prescription | Resep Fisik | Prescription kertas yang dibawa Pasien dan dicatat sebagai Prescription oleh Pharmacy Staff. |
| Direct Sale Request | Permintaan Penjualan Bebas | Permintaan obat yang dijual tanpa Prescription; tetap wajib melalui Order Dispensing dan Sale. |
| Prescription Review | Telaah Resep | Penilaian profesional Pharmacist terhadap Prescription sebelum baris obat eligible dapat masuk Order Dispensing. |
| Eligible Medicine Line | Baris Obat Layak Dipenuhi | Obat dan jumlah yang diminta, telah lolos Prescription Review bila diwajibkan, dan dapat dipenuhi dari stok tersedia. |
| Order Dispensing | Perintah Dispensing | Instruksi pemenuhan authoritative yang hanya berisi Eligible Medicine Line. Menjadi satu-satunya sumber setiap Sale. |
| Pharmacy Queue Entry | Entri Antrean Apotek | Keikutsertaan Pasien dalam antrean apotek rawat jalan, dengan identitas dan lifecycle yang dimiliki Patient Tracker. |
| Queue Mapping | Pemetaan Antrean | Pengaitan yang dapat dipertanggungjawabkan antara Pharmacy Queue Entry dan Prescription, Order Dispensing, atau Direct Sale Request yang berlaku. |
| Tracker Mapping | Pemetaan dengan Tracker | Queue Mapping otomatis menggunakan bukti Patient Tracker atau registrasi yang ditunjukkan Pasien. |
| Manual Mapping | Pemetaan Manual | Queue Mapping oleh Pharmacy Staff setelah Queue Number dipanggil dan Pasien atau permintaan diidentifikasi. |
| Stock Reservation | Reservasi Stok | Stok yang dialokasikan untuk satu Order Dispensing agar tidak dijanjikan kembali selama pemenuhan masih aktif. |
| In-Transit Stock | Stok Dalam Penyerahan | Obat yang telah disiapkan dan dikeluarkan dari ketersediaan umum apotek, tetapi belum diserahkan kepada Pasien. |
| Sale | Penjualan | Transaksi komersial yang berasal dari satu Order Dispensing. `DU` dan `Trs.DU (DO-Bill Umum)` adalah nama legacy untuk konsep bisnis yang sama. |
| General Sale | Penjualan Umum | Sale yang harus dikonfirmasi dan dibayar oleh Pasien sebelum dispensing fisik. |
| BPJS Sale | Penjualan BPJS | Sale dalam penjaminan BPJS yang berlaku, tanpa konfirmasi pembelian dan pembayaran oleh Pasien. |
| Purchase Confirmation | Konfirmasi Pembelian | Keputusan Pasien Umum untuk melanjutkan setelah Pharmacy Staff menyampaikan nilai final Sale. |
| Payment Clearance | Izin Pembayaran Terpenuhi | Kondisi bisnis yang mengizinkan dispensing Pasien Umum setelah Sale dibayar. |
| BPJS Clearance | Izin Pemenuhan BPJS | Kondisi bisnis yang mengizinkan dispensing BPJS setelah mapping antrean, kesiapan Order Dispensing, dan validasi penjamin yang berlaku, tanpa Sale terlebih dahulu. |
| Physical Dispensing | Penyiapan Obat Secara Fisik | Penyiapan, pelabelan, peracikan bila diperlukan, dan pengemasan obat oleh TTK berdasarkan Order Dispensing. |
| Medicine Review | Telaah Obat | Pemeriksaan akhir oleh Pharmacist terhadap obat yang sudah disiapkan sebelum Pasien dipanggil untuk mengambil obat. |
| Patient Education | Edukasi Pasien | Penjelasan oleh Pharmacist mengenai penggunaan obat dan perhatian yang relevan saat Medicine Handover. |
| Medicine Handover | Serah Obat | Pemindahan obat yang sudah ditelaah kepada Pasien atau penerima berwenang yang telah diverifikasi. |
| No-Show | Tidak Hadir | Outcome ketika Pasien tidak hadir untuk Medicine Handover sampai batas layanan yang berlaku. |
| Copy Prescription | Salinan Resep | Catatan yang dapat dipertanggungjawabkan mengenai item atau jumlah dalam Prescription yang tidak dapat dipenuhi. |
| Pharmacy Service Start Evidence | Bukti Mulai Layanan Apotek | Bukti bisnis yang dapat dipertanggungjawabkan bahwa layanan aktif apotek telah dimulai; tidak selalu identik dengan pembentukan Sale. |

## 3. Kapabilitas Bisnis

### 3.1 Service Demand Intake

**Indonesia:** Penerimaan Permintaan Layanan

Menerima Electronic Prescription, mencatat Physical Prescription, atau mengenali Direct Sale Request sebagai sumber permintaan pelayanan apotek rawat jalan.

### 3.2 Prescription Review

**Indonesia:** Telaah Resep

Memungkinkan Pharmacist menelaah setiap Prescription yang tersedia tanpa menunggu kedatangan Pasien atau Queue Mapping.

### 3.3 Queue Mapping

**Indonesia:** Pemetaan Antrean

Mengaitkan Pharmacy Queue Entry milik Pasien dengan Prescription atau Direct Sale Request melalui Tracker Mapping atau Manual Mapping.

### 3.4 Dispensing Eligibility Determination

**Indonesia:** Penentuan Kelayakan Dispensing

Menentukan baris obat yang lolos telaah profesional yang diwajibkan dan dapat dipenuhi dari stok tersedia.

### 3.5 Order Dispensing Management

**Indonesia:** Pengelolaan Order Dispensing

Membentuk dan menjaga instruksi pemenuhan yang menjadi dasar bersama untuk alokasi stok, pembentukan Sale, dispensing, dan Serah Obat.

### 3.6 Stock Allocation

**Indonesia:** Alokasi Stok

Mereservasi obat eligible, mengakui obat siap serah sebagai In-Transit, dan mengembalikan obat yang tidak diserahkan ketika pemenuhan berakhir tanpa Serah Obat.

### 3.7 Payer-Specific Sale Formation

**Indonesia:** Pembentukan Penjualan Sesuai Penjamin

Membentuk setiap Sale secara otomatis dari Order Dispensing pada momen bisnis yang sesuai untuk penjamin Umum atau BPJS.

### 3.8 Commercial Clearance

**Indonesia:** Pemenuhan Persyaratan Komersial

Memperoleh Purchase Confirmation dan pembayaran untuk Pasien Umum sekaligus mengizinkan Pasien BPJS berjalan tanpa pembayaran Pasien.

### 3.9 Physical Dispensing and Final Assurance

**Indonesia:** Dispensing Fisik dan Jaminan Akhir

Menyiapkan obat melalui pekerjaan TTK dan menyelesaikan Medicine Review oleh Pharmacist sebelum pengambilan.

### 3.10 Handover and No-Show Resolution

**Indonesia:** Serah Obat dan Penyelesaian Ketidakhadiran

Memanggil Pasien, memverifikasi identitas, memberikan Patient Education, menyelesaikan Medicine Handover, atau menyelesaikan No-Show tanpa menyisakan outcome Sale maupun stok yang tidak didukung.

## 4. Aktor & Peran

### 4.1 Patient or Authorized Recipient

Pasien atau penerima berwenang menunjukkan bukti antrean atau tracker, menyerahkan Physical Prescription bila berlaku, mengonfirmasi dan membayar General Sale, menerima edukasi, dan menerima obat.

### 4.2 Pharmacy Staff

Pharmacy Staff mengoordinasikan loket apotek, memanggil Queue Number, melakukan Manual Mapping, mencatat Physical Prescription, mencatat Direct Sale Request, menyampaikan nilai General Sale, dan memverifikasi identitas saat Serah Obat. Pharmacy Staff tidak melakukan telaah profesional milik Pharmacist.

### 4.3 Pharmacist

Pharmacist melakukan Prescription Review, Medicine Review, dan Patient Education. Pharmacist tidak melakukan pemanggilan antrean administratif.

### 4.4 Pharmacy Technician (`TTK`)

TTK melakukan Physical Dispensing berdasarkan Order Dispensing dan memperbaiki obat yang disiapkan apabila Medicine Review menemukan ketidaksesuaian.

### 4.5 Cashier

Cashier menerima pembayaran Pasien Umum dan membentuk Payment Clearance. Cashier tidak terlibat dalam pembayaran Pasien BPJS.

### 4.6 Pharmacy Supervisor

Pharmacy Supervisor memiliki keputusan pengecualian yang dapat dipertanggungjawabkan, seperti expiry pemenuhan, pengembalian obat siap serah, dan koreksi yang melampaui kewenangan staff biasa.

## 5. Domain Objects

### 5.1 Prescription

Merepresentasikan maksud obat yang diresepkan dan tersedia untuk apotek. Prescription dapat berupa elektronik atau dicatat dari Physical Prescription. Sumbernya tidak mengubah kewajiban Prescription Review.

### 5.2 Prescription Review Outcome

Mencatat baris Prescription yang disetujui, disetujui sebagian, ditolak, atau memerlukan klarifikasi. Keberadaannya independen dari Pharmacy Queue Entry.

### 5.3 Direct Sale Request

Merepresentasikan permintaan tanpa Prescription yang dicatat oleh Pharmacy Staff. Direct Sale Request melewati pembentukan Prescription, tetapi tidak pernah melewati Order Dispensing maupun Sale.

### 5.4 Queue Mapping

Mengaitkan Pharmacy Queue Entry yang dimiliki context lain dengan permintaan apotek yang berlaku. Pengaitan dapat terbentuk otomatis atau dilakukan Pharmacy Staff.

### 5.5 Order Dispensing

Hanya berisi Eligible Medicine Line dan jumlah pemenuhannya. Order Dispensing mempertahankan hubungan sumber, dasar nilai yang berlaku, dan alokasi stok yang diperlukan untuk menyelesaikan pemenuhan.

### 5.6 Stock Allocation

Merepresentasikan obat yang direservasi atau ditempatkan sebagai In-Transit untuk satu Order Dispensing beserta outcome akhirnya: dikeluarkan atau dikembalikan.

### 5.7 Sale

Merepresentasikan nilai komersial satu Order Dispensing. Baris dan jumlahnya wajib berasal dari Order Dispensing dan tidak pernah diinput secara independen.

### 5.8 Medicine Review Outcome

Mencatat apakah obat yang disiapkan sesuai dengan Order Dispensing dan aman dilanjutkan ke pengambilan, atau memerlukan koreksi.

### 5.9 Medicine Handover

Mencatat outcome penyerahan yang dapat dipertanggungjawabkan, penerima yang diverifikasi, tanggung jawab edukasi, serta Sale dan Order Dispensing yang diselesaikan.

## 6. Aggregates

### 6.1 Pharmacy Prescription Aggregate

**Aggregate Root:** `Pharmacy Prescription`

Aggregate menjaga Prescription yang diterima, sumbernya, Prescription Review Outcome, dan eligibility pemenuhan per baris tetap konsisten satu sama lain. Aggregate tidak menulis ulang maksud peresepan asli dari klinisi.

### 6.2 Order Dispensing Aggregate

**Aggregate Root:** `Order Dispensing`

Aggregate memiliki Eligible Medicine Line, jumlah pemenuhan, keterlacakan sumber, snapshot nilai, Stock Reservation, progres dispensing, outcome obat siap serah, serta outcome expiry atau delivery.

### 6.3 Sale Aggregate

**Aggregate Root:** `Sale`

Aggregate menjaga klasifikasi penjamin, baris Sale, nilai, Purchase Confirmation bila berlaku, disposition pembayaran, pembatalan, dan penyelesaian tetap konsisten. Sale tidak dapat memuat baris yang tidak ada dalam Order Dispensing sumbernya.

### 6.4 Medicine Handover Aggregate

**Aggregate Root:** `Medicine Handover`

Aggregate menjaga verifikasi penerima, penyelesaian Medicine Review, Patient Education, referensi Sale, referensi Order Dispensing, dan outcome delivery tetap konsisten.

Untuk BPJS, pembentukan Sale dan penyelesaian Medicine Handover merupakan satu outcome bisnis yang tidak dapat dipisahkan: bisnis tidak boleh mengakui salah satunya tanpa yang lain.

### 6.5 Kepemilikan eksternal yang eksplisit

`Pharmacy Queue Entry`, `Queue Session`, identitas Patient, bukti pembayaran kasir, klaim BPJS, dan rekening finansial organisasi bukan Aggregate Root Apotek Rawat Jalan.

## 7. Aturan Bisnis

### 7.1 Penerimaan dan tanggung jawab profesional

- **BR-APR-001** — Permintaan Apotek Rawat Jalan harus berasal tepat dari salah satu: Electronic Prescription, Physical Prescription, atau Direct Sale Request.
- **BR-APR-002** — Electronic Prescription dan Physical Prescription yang telah dicatat harus diperlakukan sebagai sumber Prescription dengan kewajiban telaah profesional yang sama.
- **BR-APR-003** — Pharmacist dapat dan seharusnya melakukan Prescription Review segera setelah Prescription tersedia; Queue Mapping dan kedatangan Pasien tidak boleh menjadi prasyarat.
- **BR-APR-004** — Physical Prescription baru tersedia untuk Prescription Review setelah Pharmacy Staff mencatatnya sebagai Prescription.
- **BR-APR-005** — Direct Sale Request tidak boleh membentuk Prescription, tetapi harus membentuk Order Dispensing sebelum Sale.
- **BR-APR-006** — Hanya Pharmacist yang memiliki outcome Prescription Review, Medicine Review, dan Patient Education.
- **BR-APR-007** — Pharmacy Staff memiliki pemanggilan antrean administratif dan tidak boleh memindahkan tanggung jawab tersebut kepada Pharmacist.
- **BR-APR-008** — TTK hanya boleh menyiapkan obat berdasarkan Order Dispensing.

### 7.2 Mapping antrean dan pemanggilan

- **BR-APR-009** — Queue Mapping harus berlangsung otomatis ketika bukti tracker atau registrasi yang valid berhasil menemukan permintaan apotek yang berlaku.
- **BR-APR-010** — Queue Number yang diterbitkan langsung harus tetap unmapped sampai Pharmacy Staff mengidentifikasi dan mengaitkan permintaan apoteknya.
- **BR-APR-011** — Tracker Mapping yang gagal harus beralih ke Manual Mapping.
- **BR-APR-012** — Pharmacy Staff harus memanggil Queue Number yang unmapped sebelum melakukan Manual Mapping.
- **BR-APR-013** — Queue Mapping mengaitkan catatan bisnis yang sudah ada dan tidak boleh dianggap sebagai penyebab terbentuknya Electronic Prescription atau selesainya Prescription Review.
- **BR-APR-014** — Pasien BPJS dengan Electronic Prescription dan Tracker Mapping yang berhasil hanya memerlukan tepat satu pemanggilan apotek pada alur normal, yaitu pemanggilan pengambilan setelah Medicine Review.
- **BR-APR-015** — Mapping dan Purchase Confirmation Pasien Umum dapat diselesaikan dalam satu interaksi loket ketika Order Dispensing dan nilai final Sale sudah tersedia.
- **BR-APR-016** — Patient Tracker tetap authoritative atas Queue Number dan lifecycle antrean meskipun Apotek Rawat Jalan menentukan mapping dan tujuan pemanggilan.

### 7.3 Order Dispensing dan stok

- **BR-APR-017** — Order Dispensing hanya boleh berisi Eligible Medicine Line.
- **BR-APR-018** — Baris Prescription hanya eligible ketika lolos Prescription Review dan jumlah pemenuhannya dapat dipenuhi dari stok tersedia.
- **BR-APR-019** — Pemenuhan sebagian harus mempertahankan baris atau jumlah Prescription yang tidak terpenuhi sebagai outcome yang dapat dipertanggungjawabkan, termasuk Copy Prescription bila berlaku.
- **BR-APR-020** — Direct Sale Request harus menghasilkan Order Dispensing dari baris obat yang diterima dan tersedia.
- **BR-APR-021** — Order Dispensing dapat menjadi Ready sebelum kedatangan Pasien atau Queue Mapping.
- **BR-APR-022** — Order Dispensing yang Ready harus mengamankan jumlah pemenuhannya melalui Stock Reservation.
- **BR-APR-023** — Obat yang sudah disiapkan harus diakui sebagai In-Transit Stock sampai diserahkan atau dikembalikan.
- **BR-APR-024** — Sale tidak boleh digunakan sebagai sumber eligibility obat; eligibility dimiliki Prescription Review dan Order Dispensing.

### 7.4 Kebijakan Sale dan penjamin

- **BR-APR-025** — Setiap Sale harus dibentuk otomatis dari tepat satu Order Dispensing.
- **BR-APR-026** — Tidak ada user yang boleh menginput Sale, `DU`, atau `Trs.DU` secara manual; tindakan user dapat memicu pembentukan Sale otomatis, tetapi tidak boleh memasok baris Sale secara independen.
- **BR-APR-027** — Baris dan jumlah Sale tidak boleh melebihi atau berbeda dari baris dan jumlah Order Dispensing sumbernya.
- **BR-APR-028** — Satu Order Dispensing tidak boleh memiliki lebih dari satu Sale aktif.
- **BR-APR-029** — General Sale harus dibentuk setelah Queue Mapping dan Order Dispensing Ready, sebelum Purchase Confirmation dan pembayaran.
- **BR-APR-030** — Nilai final yang disampaikan kepada Pasien Umum harus berasal dari General Sale yang diturunkan dari Order Dispensing.
- **BR-APR-031** — Penolakan Purchase Confirmation oleh Pasien Umum harus menyebabkan General Sale dibatalkan dan Stock Reservation yang tidak digunakan dilepas.
- **BR-APR-032** — General Sale harus dibayar sebelum Physical Dispensing dimulai.
- **BR-APR-033** — Pasien BPJS tidak boleh diminta melakukan Purchase Confirmation atau pembayaran Pasien.
- **BR-APR-034** — Physical Dispensing BPJS dapat dimulai ketika Queue Mapping, Order Dispensing Ready, dan BPJS Clearance tersedia; BPJS Sale belum diwajibkan pada tahap tersebut.
- **BR-APR-035** — BPJS Sale hanya boleh dibentuk ketika Medicine Handover berhasil dikonfirmasi, bukan hanya ketika Pasien datang atau dipanggil.
- **BR-APR-036** — BPJS Sale tidak memiliki jumlah yang harus dibayar Pasien dan memiliki disposition pembayaran `Not Required`; nilai gross atau nilai yang ditanggung tidak harus nol.
- **BR-APR-037** — Pembentukan BPJS Sale dan Medicine Handover harus menjadi satu penyelesaian bisnis yang tidak dapat dipisahkan; pengakuan parsial dilarang.
- **BR-APR-038** — Nama legacy `DU` dan `Trs.DU (DO-Bill Umum)` tidak boleh mengubah makna bisnis maupun aturan pembentukan Sale.

### 7.5 Dispensing, Serah Obat, no-show, dan bukti lintas-context

- **BR-APR-039** — Medicine Review harus berhasil diselesaikan sebelum Pharmacy Staff memanggil Pasien untuk mengambil obat.
- **BR-APR-040** — Pharmacy Staff, bukan Pharmacist, harus melakukan pemanggilan pengambilan.
- **BR-APR-041** — Identitas penerima harus diverifikasi dan Pharmacist harus memberikan Patient Education sebelum Medicine Handover selesai.
- **BR-APR-042** — Medicine Handover yang berhasil harus menyelesaikan Order Dispensing dan mengonsumsi In-Transit Stock terkait.
- **BR-APR-043** — No-Show BPJS sebelum Medicine Handover tidak boleh membentuk atau membatalkan Sale; obat yang direservasi atau In-Transit harus dikembalikan dan Order Dispensing berakhir expired atau tanpa delivery.
- **BR-APR-044** — No-Show Pasien Umum setelah pembayaran harus mengikuti kebijakan yang dapat dipertanggungjawabkan untuk Sale yang telah dibayar tetapi tidak diambil; kondisi ini tidak boleh diperlakukan sebagai jalur BPJS tanpa Sale.
- **BR-APR-045** — Pharmacy Service Start Evidence harus berasal dari aktivitas layanan yang dapat dipertanggungjawabkan dan tidak boleh selalu bergantung pada pembentukan Sale. Konfirmasi General Sale dapat menjadi bukti mulai layanan, sedangkan mulai layanan BPJS harus dibuktikan sebelum Sale dibentuk saat Serah Obat.
- **BR-APR-046** — Keterlacakan sumber dari Prescription atau Direct Sale Request melalui Order Dispensing, Sale, sampai Medicine Handover harus dipertahankan.

## 8. State Machines & Lifecycles

### 8.1 Lifecycle Prescription Review

```text
Prescription Recorded
  → Under Review
      → Approved
      → Partially Approved
      → Rejected
      → Clarification Required
```

Electronic Prescription dan Physical Prescription menggunakan lifecycle yang sama. Queue Mapping bukan transition dalam lifecycle ini.

### 8.2 Relasi Queue Mapping

```text
Unmapped
  → Mapped
```

Ini adalah relasi pengaitan permintaan apotek, bukan pengganti lifecycle antrean milik Patient Tracker.

### 8.3 Lifecycle Order Dispensing

```text
Established
  → Ready
      → Dispensing
          → Prepared
              → Delivered

Ready or Prepared
  → Cancelled or Expired
```

`Delivered` memerlukan Medicine Handover yang berhasil. No-Show BPJS berakhir melalui `Expired` atau outcome tanpa delivery lain yang disetujui, bukan melalui pembatalan Sale.

### 8.4 Lifecycle Stock Allocation

```text
Available
  → Reserved
      → In-Transit
          → Issued to Patient
          → Returned to Pharmacy

Reserved
  → Released to Available
```

### 8.5 Lifecycle General Sale

```text
Established
  → Awaiting Confirmation
      → Awaiting Payment
          → Paid
              → Completed

Awaiting Confirmation
  → Cancelled
```

### 8.6 Lifecycle dan pembentukan BPJS Sale

BPJS Sale belum ada selama Order Dispensing sedang ditelaah, direservasi, didispensing, disiapkan, atau menunggu pengambilan.

```text
Successful BPJS Medicine Handover
  → BPJS Sale Established with Payment Not Required
  → BPJS Sale Completed
```

Pembentukan dan penyelesaian Sale terjadi dalam outcome Serah Obat yang sama dan dapat dipertanggungjawabkan. “Not yet established” adalah ketiadaan Sale, bukan state Sale.

### 8.7 Lifecycle Medicine Handover

```text
Pending Final Review
  → Ready for Pickup
      → Recipient Verified
          → Education Provided
              → Delivered

Ready for Pickup
  → No-Show
```

## 9. Domain Events

| Domain Event | Makna bisnis |
|---|---|
| Prescription Recorded | Prescription telah tersedia untuk ditelaah oleh apotek. |
| Prescription Review Completed | Pharmacist telah menetapkan disposition profesional setiap baris Prescription. |
| Pharmacy Queue Mapped | Pharmacy Queue Entry telah dikaitkan dengan permintaan apotek yang berlaku. |
| Order Dispensing Established | Eligible Medicine Line telah membentuk instruksi pemenuhan yang authoritative. |
| Stock Reserved | Stok telah dialokasikan kepada Order Dispensing. |
| General Sale Established | General Sale telah diturunkan dari Order Dispensing yang Ready. |
| Purchase Confirmed | Pasien Umum telah menyetujui untuk melanjutkan pada nilai Sale yang disampaikan. |
| General Sale Paid | Payment Clearance telah terbentuk untuk General Sale. |
| BPJS Clearance Established | Pemenuhan BPJS dapat dilanjutkan tanpa pembayaran Pasien dan tanpa Sale yang sudah ada. |
| Dispensing Started | TTK telah memulai penyiapan fisik berdasarkan Order Dispensing. |
| Medicine Prepared | Obat telah memasuki kondisi siap serah dan In-Transit. |
| Medicine Review Completed | Pharmacist telah memastikan obat siap dilanjutkan ke pengambilan. |
| Patient Called for Pickup | Pharmacy Staff telah memanggil Pasien untuk Medicine Handover. |
| BPJS Sale Established | BPJS Sale telah diturunkan dari Order Dispensing sebagai bagian dari Medicine Handover yang berhasil. |
| Medicine Handed Over | Obat yang sudah ditelaah telah dipindahkan kepada penerima terverifikasi dengan Patient Education. |
| Order Dispensing Expired | Pemenuhan berakhir tanpa delivery dalam batas layanan yang berlaku. |
| Pharmacy Stock Returned | Obat Reserved atau In-Transit telah kembali ke ketersediaan apotek. |

## 10. Workflow Bisnis

### 10.1 Review an Electronic Prescription before Patient arrival

```text
Electronic Prescription tersedia
  → Pharmacist melakukan Prescription Review
  → Eligible Medicine Line ditentukan
  → Order Dispensing dibentuk
  → stok direservasi
  → Order Dispensing menunggu Queue Mapping secara independen
```

### 10.2 BPJS with Electronic Prescription and successful Tracker Mapping

```text
Electronic Prescription telah memiliki Order Dispensing Ready
  → Pasien memindai bukti tracker
  → Queue Mapping berhasil otomatis
  → BPJS Clearance terbentuk
  → TTK melakukan Physical Dispensing
  → Pharmacist menyelesaikan Medicine Review
  → Pharmacy Staff memanggil Pasien satu kali untuk pengambilan
  → Pasien diverifikasi dan diberi edukasi
  → BPJS Sale dan Medicine Handover selesai bersama-sama
```

### 10.3 Manual Queue Mapping

```text
Pasien mengambil Queue Number langsung atau Tracker Mapping gagal
  → Pharmacy Staff memanggil Queue Number
  → Pharmacy Staff mengidentifikasi sumber layanan
      → Electronic Prescription yang sudah ada dikaitkan
      → Physical Prescription dicatat
      → Direct Sale Request dicatat
  → Pharmacy Queue Entry menjadi Mapped
```

### 10.4 Physical Prescription fulfilment

```text
Pharmacy Staff mencatat Physical Prescription
  → Pharmacist melakukan Prescription Review
  → Eligible Medicine Line ditentukan
  → Order Dispensing dibentuk
  → clearance sesuai penjamin dilanjutkan
```

### 10.5 Direct Sale fulfilment

```text
Pharmacy Staff mencatat Direct Sale Request
  → baris obat yang diterima dan tersedia ditentukan
  → Order Dispensing dibentuk
  → General Sale diturunkan
  → Purchase Confirmation dan pembayaran dilanjutkan
```

### 10.6 General Patient fulfilment

```text
Queue Mapping dan Order Dispensing Ready tersedia
  → General Sale diturunkan dari Order Dispensing
  → Pharmacy Staff menyampaikan nilai final Sale
  → Pasien mengonfirmasi pembelian
  → Cashier menerima pembayaran
  → TTK melakukan Physical Dispensing
  → Pharmacist menyelesaikan Medicine Review
  → Pharmacy Staff memanggil Pasien untuk pengambilan
  → Pasien diverifikasi dan diberi edukasi
  → Medicine Handover menyelesaikan Sale dan Order Dispensing
```

Jika Pasien menolak sebelum pembayaran, General Sale dibatalkan dan stok yang tidak digunakan dilepas.

### 10.7 Successful BPJS Medicine Handover

```text
Obat BPJS Prepared lolos Medicine Review
  → Pharmacy Staff memanggil Pasien
  → Pasien atau penerima berwenang hadir dan diverifikasi
  → Pharmacist memberikan Patient Education
  → BPJS Sale diturunkan dari Order Dispensing
  → Medicine Handover dicatat
  → In-Transit Stock dikeluarkan
  → Order Dispensing dan layanan antrean apotek selesai
```

BPJS Sale dan Medicine Handover merupakan satu outcome yang dapat dipertanggungjawabkan meskipun Sale ditulis lebih dahulu untuk keterlacakan sumber.

### 10.8 BPJS No-Show

```text
Obat BPJS Prepared lolos Medicine Review
  → Pharmacy Staff memanggil Pasien
  → Pasien tidak hadir sampai batas layanan yang berlaku
  → BPJS Sale tidak dibentuk
  → Reserved atau In-Transit Stock kembali ke ketersediaan apotek
  → Order Dispensing berakhir expired tanpa delivery
  → keikutsertaan antrean apotek ditutup menurut kebijakan Patient Tracker yang berlaku
```

### 10.9 No eligible medicine

```text
Prescription Review dan penilaian stok selesai
  → tidak ada Eligible Medicine Line tersisa
  → Order Dispensing tidak dibentuk
  → Sale tidak dibentuk
  → outcome tidak terpenuhi dan Copy Prescription dicatat bila berlaku
```
