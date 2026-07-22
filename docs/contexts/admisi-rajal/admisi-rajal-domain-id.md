# Domain Admisi Rajal

**Status artefak:** Spesifikasi bisnis kanonis awal  
**Bounded context:** Admisi Rajal  
**Cakupan versi:** Fondasi untuk elaborasi fitur secara bertahap  
**Sumber kanonis Bahasa Inggris:** [admisi-rajal-domain.md](admisi-rajal-domain.md)

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan

Admisi Rajal adalah kapabilitas bisnis yang bertanggung jawab menyiapkan dan menetapkan akses administratif Pasien ke pelayanan rawat jalan.

Admisi Rajal menghubungkan kebutuhan pelayanan yang direncanakan maupun tidak direncanakan dengan Pasien yang teridentifikasi, tujuan rawat jalan yang memenuhi ketentuan, pengaturan pembayaran atau penjamin yang berlaku, serta Outpatient Registration yang otoritatif.

### 1.2 Nilai bisnis

Admisi Rajal menyediakan:

- pintu masuk administratif yang konsisten menuju pelayanan rawat jalan;
- kesinambungan antara Booking, registrasi mandiri atau berbantuan, dan tujuan rawat jalan yang dimaksud;
- visibilitas atas pekerjaan registrasi yang belum terselesaikan bagi Admission Officer;
- outcome Outpatient Registration yang otoritatif;
- fondasi bagi penerimaan identitas Pasien, penentuan coverage, kualifikasi rujukan, penetapan tujuan, dan pembentukan Initial Charge; serta
- bukti bisnis yang dapat dipertanggungjawabkan bagi Patient Journey yang lebih luas.

### 1.3 Cakupan dan elaborasi bertahap

Spesifikasi awal ini menetapkan batas konteks yang stabil dan dua kapabilitas yang dielaborasi: Booking Management serta Registration Intake and Work Coordination.

Kapabilitas berikut diakui sebagai bagian dari bisnis Admisi Rajal, tetapi memerlukan spesifikasi fitur lanjutan sebelum kebijakan detailnya menjadi kanonis:

- New Patient Recording;
- Insurance and Guarantor Determination;
- Referral Qualification;
- Outpatient Destination Assignment;
- pembentukan Initial Charge, termasuk `Karcis`;
- kebijakan lanjutan Practice Schedule Management di luar artefak jadwal yang sudah ada;
- kebijakan lanjutan perubahan Booking dan penanganan pengecualian di luar aturan fondasional dokumen ini; serta
- perubahan registrasi, pembatalan, dan penanganan pengecualian di luar aturan fondasional dokumen ini.

Pencantuman dalam inventaris ini tidak otomatis menetapkan seluruh aturan, batas Aggregate, syarat eligibility, atau lifecycle kapabilitas tersebut. Artefak domain berikutnya harus memperinci dokumen ini tanpa bertentangan dengan batas kepemilikan dan Ubiquitous Language yang telah ditetapkan.

### 1.4 Batas bisnis

Admisi Rajal memiliki:

- kebenaran bisnis Booking rawat jalan;
- kebenaran dan outcome bisnis Outpatient Registration;
- penentuan bisnis bahwa Registration Assistance diperlukan;
- konteks registrasi yang ditampilkan sebagai pekerjaan aktif kepada Admission Officer;
- pemilihan tujuan rawat jalan sesuai kebijakan yang berlaku;
- konteks pembayaran, penjamin, rujukan, dan Initial Charge pada registrasi; serta
- kebenaran bisnis jadwal praktik rawat jalan yang saat ini dipelihara dalam konteks ini.

Admisi Rajal tidak memiliki:

- identitas master Pasien kanonis, yang dimiliki Patient context;
- identitas Patient Journey, `TrackerId`, Queue Session, Queue Entry, Queue Number, atau milestone layanan antrean, yang dimiliki Patient Tracker;
- konsultasi klinis atau Medical Chart;
- kebenaran eligibility eksternal milik organisasi penjamin;
- kebijakan tarif atau definisi harga tagihan yang otoritatif;
- settlement pembayaran atau keseluruhan lifecycle rekening finansial Pasien;
- definisi klinis unit rawat jalan atau profesi dokter; maupun
- lokasi atau pergerakan fisik Pasien.

Admisi Rajal dapat memakai fakta rujukan dari konteks-konteks tersebut, tetapi tidak menggantikan otoritasnya.

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| Admisi Rajal | Admisi Rawat Jalan | Tanggung jawab bisnis administratif yang menyiapkan dan menetapkan akses menuju pelayanan rawat jalan. |
| Patient | Pasien | Orang yang mencari atau menerima pelayanan rawat jalan. |
| Patient Representative | Perwakilan Pasien | Orang yang memberikan informasi administratif atau bertindak bagi Pasien ketika diizinkan. |
| Admission Officer | Petugas Admisi | Peran staf yang bertanggung jawab menyelesaikan pekerjaan registrasi rawat jalan berbantuan. |
| Outpatient Registration | Registrasi Rawat Jalan | Catatan administratif otoritatif yang menerima satu Pasien untuk satu kunjungan rawat jalan yang dimaksud. |
| RegId | Identitas Registrasi | Referensi bisnis stabil dari Registration yang telah terbentuk. RegId bukan identitas Patient Journey. |
| Registration Intake Path | Jalur Masuk Registrasi | Asal suatu upaya registrasi: `By Booking` atau `By Walk-In`. |
| By Booking | Melalui Booking | Registration Intake Path yang dimulai dari Booking yang sudah ada. |
| By Walk-In | Datang Langsung | Registration Intake Path yang dimulai tanpa Booking yang berlaku. |
| Booking | Pemesanan Kunjungan | Rencana bagi Pasien atau calon Pasien untuk mengunjungi tujuan rawat jalan tertentu pada Visit Date saat ini atau yang akan datang. |
| Booking Channel | Kanal Booking | Kanal bisnis yang menjadi asal permintaan Booking: `HiDok` atau `Admission-Assisted`. |
| HiDok | HiDok | Booking Channel digital mandiri yang disediakan melalui aplikasi Android milik organisasi. |
| Admission-Assisted | Berbantuan Admisi | Booking Channel ketika Admission Officer mencatat permintaan yang diterima melalui telepon, WhatsApp, atau metode komunikasi lain yang disetujui. |
| Practice Session | Sesi Praktik | Ketersediaan terjadwal seorang dokter pada suatu Outpatient Destination untuk satu Visit Date dan interval operasional. |
| Schedule Capacity | Kapasitas Jadwal | Jumlah maksimum Patient yang direncanakan untuk satu Practice Session. Batas ini bersifat keras bagi HiDok dan bersifat perencanaan bagi Admission-Assisted Booking. |
| Capacity Override | Pengecualian Kapasitas | Keputusan akuntabel Admission Officer untuk menerima Admission-Assisted Booking setelah Schedule Capacity tercapai. |
| Unresolved Patient Identity | Identitas Pasien Belum Terselesaikan | Kondisi ketika Booking mempertahankan snapshot identitas demografis tetapi belum dikaitkan dengan Patient kanonis. |
| Expired Unfulfilled | Kedaluwarsa Belum Terpenuhi | Kondisi Booking ketika Visit Date telah berlalu tanpa Registration, pembatalan, atau penjadwalan ulang. Kondisi ini bukan bukti ketidakhadiran fisik Patient. |
| Self-Registration | Registrasi Mandiri | Registrasi yang dilakukan Pasien melalui saluran mandiri resmi tanpa bantuan Admission Officer. |
| Self-Registration Requires Assistance | Registrasi Mandiri Memerlukan Bantuan | Outcome bisnis bahwa Self-Registration tidak berhasil membentuk Registration dan diperlukan registrasi berbantuan. |
| Registration Assistance | Bantuan Registrasi | Pekerjaan administratif oleh manusia yang diperlukan untuk membentuk atau menyelesaikan Outpatient Registration. |
| Admisi Rajal Work List | Daftar Kerja Admisi Rawat Jalan | Kumpulan Registration Assistance aktif yang direpresentasikan oleh Queue Entry Patient Tracker yang aktif pada Service Point Admisi Rajal dan diperkaya dengan konteks Admisi Rajal. |
| Work Item | Butir Pekerjaan | Representasi operasional dari satu kewajiban Registration Assistance aktif. Work Item bukan antrean atau Aggregate Root tersendiri. |
| Patient Identity Intake | Penerimaan Identitas Pasien | Pengumpulan dan penyelesaian identitas secara akuntabel untuk merujuk Pasien yang sudah ada atau mengajukan New Patient Recording. |
| New Patient Recording | Pencatatan Pasien Baru | Pembentukan identitas Pasien kanonis baru ketika tidak ada catatan Pasien yang berlaku dan kebijakan mengizinkan pembentukan tersebut. |
| Coverage Arrangement | Pengaturan Penanggungan | Konteks tanggung jawab pembayaran yang diusulkan atau diterima untuk Registration, termasuk bayar sendiri, asuransi, atau penjamin lain. |
| Guarantor | Penjamin | Pihak yang diperkirakan menanggung sebagian atau seluruh tanggung jawab finansial berdasarkan pengaturan yang berlaku. |
| Referral | Rujukan | Bukti bahwa Pasien diarahkan ke pelayanan rawat jalan oleh pihak perujuk yang berlaku. |
| Outpatient Destination | Tujuan Rawat Jalan | Unit pelayanan rawat jalan dan pemberi asuhan yang bertanggung jawab sebagai tujuan Registration atau Booking. |
| Poli | Poliklinik | Istilah operasional yang mapan untuk unit pelayanan rawat jalan yang dipakai sebagai Outpatient Destination. |
| Practice Schedule | Jadwal Praktik | Ketersediaan terencana dari pemberi asuhan pada suatu Outpatient Destination. |
| Visit Date | Tanggal Kunjungan | Tanggal pelayanan rawat jalan direncanakan atau diregistrasikan. |
| Initial Charge | Tagihan Awal | Kewajiban tagihan awal yang terbentuk karena Registration memberikan akses ke tujuan rawat jalan. |
| Karcis | Karcis | Istilah bisnis yang mapan untuk pengaturan Initial Charge terkait registrasi dan berlaku bagi suatu tujuan. |
| Patient Journey | Perjalanan Pasien | Satu kesinambungan logis interaksi operasional Pasien yang dimiliki Patient Tracker. |
| TrackerId | Identitas Pelacakan Perjalanan | Identitas logis stabil dari satu Patient Journey yang dimiliki Patient Tracker. |
| Queue Entry | Entri Antrean | Keikutsertaan Pasien atau pengunjung anonim dalam satu Queue Session yang dimiliki Patient Tracker. |
| Registration Outcome | Hasil Registrasi | Hasil otoritatif dari suatu upaya registrasi, termasuk Registration Established atau Registration Not Established. |

## 3. Kapabilitas Bisnis

### 3.1 Patient Identity Intake

**Indonesia:** Penerimaan Identitas Pasien

Memperoleh informasi identitas yang memadai untuk merujuk Pasien yang sudah ada atau memulai New Patient Recording. Kebijakan detail mengenai pencocokan, koreksi, duplikasi, dan pembentukan akan dielaborasi kemudian.

### 3.2 Practice Schedule Management

**Indonesia:** Pengelolaan Jadwal Praktik

Memelihara ketersediaan rawat jalan berulang dan spesifik tanggal yang digunakan oleh Booking dan Registration. Artefak jadwal yang sudah ada memperinci kapabilitas ini.

### 3.3 Booking Management

**Indonesia:** Pengelolaan Booking

Merencanakan kunjungan rawat jalan melalui HiDok atau Admission-Assisted Booking terhadap Practice Session yang berlaku.

Booking Management dapat:

- menerima referensi Patient kanonis yang valid atau mempertahankan snapshot identitas yang belum terselesaikan;
- menerapkan Schedule Capacity sebagai batas keras bagi HiDok sekaligus mengizinkan Capacity Override yang akuntabel bagi Admission-Assisted;
- memperoleh Queue Number dokter dari kapabilitas alokasi nomor antrean otoritatif milik Rumah Sakit;
- mencegah Booking aktif ganda bagi Patient yang telah diidentifikasi secara kanonis pada Practice Session yang sama;
- menjadwalkan ulang Booking tanpa mengganti identitas bisnisnya;
- mengaitkan Booking dengan Outpatient Registration di kemudian hari; serta
- mempertahankan Booking yang tidak terpenuhi sebagai bukti historis setelah Visit Date.

Alokasi Queue Number dan identitas Patient Journey tetap diatur Patient Tracker.

### 3.4 Registration Intake and Work Coordination

**Indonesia:** Penerimaan Registrasi dan Koordinasi Pekerjaan

Mengenali kebutuhan registrasi dari jalur By Booking dan By Walk-In, menentukan kapan Registration Assistance diperlukan, dan menampilkan pekerjaan aktif tanpa mengambil alih kepemilikan antrean Patient Tracker.

### 3.5 Outpatient Registration

**Indonesia:** Registrasi Rawat Jalan

Membentuk catatan administratif otoritatif yang menghubungkan Pasien, Visit Date, Outpatient Destination, dan konteks administratif yang berlaku.

### 3.6 Insurance and Guarantor Determination

**Indonesia:** Penentuan Asuransi dan Penjamin

Menentukan dan mempertahankan Coverage Arrangement yang berlaku bagi Registration. Kebijakan detail mengenai eligibility, otorisasi, koordinasi manfaat, dan fallback akan dielaborasi kemudian.

### 3.7 Referral Qualification

**Indonesia:** Kualifikasi Rujukan

Menentukan apakah bukti Referral diperlukan dan berlaku bagi Registration. Aturan detail rujukan akan dielaborasi kemudian.

### 3.8 Outpatient Destination Assignment

**Indonesia:** Penetapan Tujuan Rawat Jalan

Menetapkan Poli dan pemberi asuhan yang dimaksud secara konsisten dengan kebijakan pelayanan dan Practice Schedule yang berlaku.

### 3.9 Initial Charge Establishment

**Indonesia:** Pembentukan Tagihan Awal

Menentukan `Karcis` yang berlaku dan membentuk Initial Charge terkait registrasi. Otoritas Tarif, billing lanjutan, dan settlement tetap berada di luar kapabilitas ini.

## 4. Aktor & Peran

### 4.1 Patient

Patient memberikan bukti identitas dan administratif yang tersedia, memilih atau menerima tujuan rawat jalan, serta berpartisipasi dalam Self-Registration atau Registration berbantuan.

### 4.2 Patient Representative

Patient Representative memberikan informasi atau mengambil keputusan administratif yang diizinkan atas nama Patient.

### 4.3 Admission Officer

Admission Officer:

- mencatat permintaan Admission-Assisted Booking tanpa membedakan apakah permintaan diterima melalui telepon atau WhatsApp;
- menerima atau menolak permintaan yang melampaui Schedule Capacity;
- menyelesaikan Registration Assistance;
- memverifikasi bukti identitas dan administratif yang tersedia;
- membentuk atau mengubah Outpatient Registration sesuai kebijakan; serta
- tetap bertanggung jawab atas keputusan registrasi berbantuan.

### 4.4 Schedule Administrator

Schedule Administrator memelihara kebenaran bisnis Practice Schedule dan perubahan spesifik tanggal yang disetujui.

### 4.5 Care Provider

Care Provider menyediakan konteks ketersediaan dan tujuan bagi Booking dan Registration, tetapi tidak mengambil keputusan administratif Admisi Rajal.

### 4.6 Guarantor Representative

Guarantor Representative memberikan atau mengonfirmasi bukti coverage atas nama penjamin. Admisi Rajal tetap bertanggung jawab atas Coverage Arrangement yang dicatat pada Registration.

## 5. Domain Objects

### 5.1 Outpatient Registration

Merepresentasikan penerimaan administratif satu Patient untuk satu kunjungan rawat jalan yang dimaksud.

Objek ini mempertahankan referensi Patient, Visit Date, Outpatient Destination, Coverage Arrangement, konteks rujukan, dan konteks Initial Charge yang diwajibkan oleh kebijakan yang berlaku.

### 5.2 Booking

Merepresentasikan rencana kunjungan rawat jalan sebelum Registration terbentuk.

Booking mempertahankan Booking Channel, Visit Date, Practice Session, Outpatient Destination, Queue Number dokter, serta referensi Patient kanonis atau snapshot identitas yang belum terselesaikan. Booking kemudian dapat dikaitkan dengan satu Outpatient Registration, tetapi tetap berbeda dari Registration tersebut.

Penjadwalan ulang mengubah Practice Session yang berlaku tanpa mengganti identitas bisnis Booking. Booking yang tidak terpenuhi tetap menjadi bukti historis setelah Visit Date.

### 5.3 Practice Schedule Template

Merepresentasikan ketersediaan pemberi asuhan yang berulang untuk merencanakan tanggal pelayanan rawat jalan.

### 5.4 Daily Practice Schedule

Merepresentasikan ketersediaan efektif atau pengecualian yang disetujui untuk satu tanggal tertentu.

### 5.5 Registration Assistance

Merepresentasikan kebutuhan bisnis agar Admission Officer menyelesaikan suatu upaya registrasi. Keikutsertaan antrean dan milestone layanannya dimiliki Patient Tracker.

### 5.6 Admisi Rajal Work List

Merepresentasikan kumpulan Registration Assistance aktif saat ini. Keanggotaannya berasal dari kebenaran antrean Patient Tracker dan digabungkan dengan konteks registrasi Admisi Rajal. Objek ini bukan buku besar bisnis yang independen.

### 5.7 Patient Identity Reference

Mengidentifikasi Patient kanonis yang digunakan oleh Booking atau Outpatient Registration. Snapshot identitas dapat membantu proses resolusi, tetapi tidak menggantikan Patient master.

### 5.8 Coverage Arrangement

Merepresentasikan dasar tanggung jawab pembayaran yang diterima untuk satu Registration. Kebijakan detailnya akan dielaborasi kemudian.

### 5.9 Outpatient Destination

Merepresentasikan Poli dan pemberi asuhan yang bertanggung jawab sebagai tujuan kunjungan rawat jalan.

### 5.10 Initial Charge

Merepresentasikan kewajiban finansial awal yang timbul dari akses administratif ke Outpatient Destination terpilih. Initial Charge merujuk kebenaran tarif yang berlaku tanpa memiliki kebijakan tarif atau settlement.

## 6. Aggregates

### 6.1 Outpatient Registration Aggregate

**Aggregate Root:** `Outpatient Registration`

Aggregate menjaga referensi Patient, Visit Date, tujuan, Coverage Arrangement, konteks rujukan, konteks Initial Charge, dan status Registration tetap konsisten satu sama lain.

Batas internal detail untuk komponen coverage, rujukan, dan charge akan dielaborasi pada fitur berikutnya.

### 6.2 Booking Aggregate

**Aggregate Root:** `Booking`

Aggregate menjaga Booking Channel, informasi identitas Patient, Visit Date, Practice Session, Outpatient Destination, keputusan Schedule Capacity, referensi reservasi antrean dokter, dan hubungan dengan Registration yang telah terbentuk tetap konsisten satu sama lain.

Aggregate mempertahankan identitas Booking sepanjang penjadwalan ulang. Aggregate tidak memiliki alokasi Queue Number atau identitas Patient Journey.

### 6.3 Practice Schedule Template Aggregate

**Aggregate Root:** `Practice Schedule Template`

Aggregate memiliki satu definisi berulang mengenai ketersediaan pemberi asuhan, tujuan, interval operasional, kapasitas, dan pola antrean.

### 6.4 Daily Practice Schedule Aggregate

**Aggregate Root:** `Daily Practice Schedule`

Aggregate memiliki satu kejadian jadwal spesifik tanggal atau pengecualian yang disetujui. Ketika ditetapkan sebagai pengecualian manual, Aggregate ini tetap independen dari perubahan berikutnya pada template berulang.

### 6.5 Explicit non-aggregates

`Admisi Rajal Work List`, `Work Item`, `Queue Entry`, `TrackerId`, dan `Patient Identity Reference` bukan Aggregate Root Admisi Rajal.

Konsistensi antrean dimiliki Patient Tracker. Konsistensi identitas Patient kanonis dimiliki Patient context.

## 7. Aturan Bisnis

### 7.1 Batas konteks dan identitas

- **BR-ARJ-001** — Setiap Outpatient Registration yang terbentuk harus merujuk tepat satu Patient kanonis.
- **BR-ARJ-002** — `RegId` harus mengidentifikasi Outpatient Registration dan tidak boleh menggantikan `TrackerId` sebagai identitas Patient Journey.
- **BR-ARJ-003** — Admisi Rajal tidak boleh menganggap snapshot identitas, nama yang sama, atau tanggal lahir yang sama sebagai bukti konklusif identitas Patient kanonis.
- **BR-ARJ-004** — Booking dan Outpatient Registration harus tetap menjadi catatan bisnis yang berbeda meskipun Registration berasal dari Booking tersebut.

### 7.2 Jalur masuk dan Self-Registration

- **BR-ARJ-005** — Setiap upaya registrasi harus memiliki satu Registration Intake Path: `By Booking` atau `By Walk-In`.
- **BR-ARJ-006** — Self-Registration yang berhasil harus membentuk Outpatient Registration yang otoritatif tanpa menciptakan Registration Assistance.
- **BR-ARJ-007** — `Self-Registration Requires Assistance` tidak boleh direpresentasikan sebagai Registration yang berhasil.
- **BR-ARJ-008** — Permintaan bantuan berbasis Booking harus mempertahankan Patient Journey yang telah terkait dengan Booking ketika journey tersebut berlaku.
- **BR-ARJ-009** — Walk-In dapat memasuki Registration Assistance sebelum identitas Patient kanonis atau identitas Patient Journey diketahui.

### 7.3 Batas Work List dan Patient Tracker

- **BR-ARJ-010** — Admisi Rajal Work List hanya boleh memuat Registration Assistance aktif untuk Service Point Admisi Rajal.
- **BR-ARJ-011** — Setiap Work Item harus berkorespondensi dengan satu Queue Entry aktif milik Patient Tracker dan tidak boleh membentuk identitas antrean kedua.
- **BR-ARJ-012** — Admisi Rajal tidak boleh menetapkan Queue Number secara independen atau mendefinisikan ulang status Queue Entry.
- **BR-ARJ-013** — Penyelesaian Registration harus ditentukan oleh outcome Outpatient Registration, bukan hanya disimpulkan dari selesainya antrean.
- **BR-ARJ-014** — Penyelesaian antrean harus dicatat melalui Patient Tracker dan tidak boleh hanya disimpulkan dari terbentuknya Registration.
- **BR-ARJ-015** — Permintaan bantuan berulang untuk upaya Booking yang sama dan belum terselesaikan tidak boleh membentuk lebih dari satu kewajiban Registration Assistance aktif.

### 7.4 Fondasi Registration

- **BR-ARJ-016** — Setiap Outpatient Registration yang terbentuk harus mengidentifikasi satu Visit Date dan satu Outpatient Destination.
- **BR-ARJ-017** — Outpatient Destination harus memenuhi ketentuan pelayanan rawat jalan berdasarkan kebijakan tujuan dan jadwal yang berlaku.
- **BR-ARJ-018** — Registration harus mempertahankan Coverage Arrangement, konteks rujukan, dan konteks Initial Charge yang diwajibkan oleh kebijakan saat pembentukan.
- **BR-ARJ-019** — Admisi Rajal tidak boleh menyatakan eligibility penjamin eksternal ketika bukti yang diwajibkan belum terbentuk.
- **BR-ARJ-020** — Pembentukan Initial Charge tidak boleh memindahkan kepemilikan kebijakan tarif atau settlement ke Admisi Rajal.

### 7.5 Booking Management

- **BR-ARJ-023** — Setiap Booking harus berasal dari tepat satu Booking Channel: `HiDok` atau `Admission-Assisted`.
- **BR-ARJ-024** — Telepon dan WhatsApp harus diperlakukan sebagai metode komunikasi dari Booking Channel `Admission-Assisted` dan tidak boleh membentuk Booking Channel tersendiri.
- **BR-ARJ-025** — Setiap Booking harus mengidentifikasi satu Practice Session, Visit Date, Outpatient Destination, dan dokter yang berlaku serta tidak dibatalkan.
- **BR-ARJ-026** — `PasienId` yang diberikan harus merujuk Patient kanonis yang sudah ada; `PasienId` tidak valid harus menyebabkan permintaan Booking ditolak.
- **BR-ARJ-027** — Booking dapat dibentuk tanpa `PasienId` dan dalam kondisi tersebut harus mempertahankan snapshot Unresolved Patient Identity.
- **BR-ARJ-028** — Kemiripan atau ambiguitas demografis tidak boleh dengan sendirinya menghalangi pembentukan Booking, menetapkan identitas Patient kanonis, atau mengizinkan penggabungan Patient.
- **BR-ARJ-029** — HiDok tidak boleh membentuk Booking setelah Schedule Capacity untuk Practice Session tercapai.
- **BR-ARJ-030** — Admission Officer dapat menolak Admission-Assisted Booking setelah Schedule Capacity tercapai atau menerimanya melalui Capacity Override yang akuntabel.
- **BR-ARJ-031** — HiDok tidak boleh melakukan Capacity Override.
- **BR-ARJ-032** — Setiap Booking yang terbentuk harus memperoleh Queue Number dokter dari kapabilitas alokasi nomor antrean otoritatif milik Rumah Sakit; Admisi Rajal tidak boleh mendefinisikan mekanisme alokasi lama maupun saat ini.
- **BR-ARJ-033** — Satu Patient yang telah diidentifikasi secara kanonis tidak boleh memiliki lebih dari satu Booking aktif untuk dokter, Visit Date, dan Practice Session yang sama.
- **BR-ARJ-034** — Booking dapat dijadwalkan ulang dan harus mempertahankan identitas Booking serta data bisnis yang sudah ada, kecuali fakta yang berubah memerlukan penggantian.
- **BR-ARJ-035** — Penjadwalan ulang harus memerlukan Practice Session tujuan yang berlaku dan harus mengganti reservasi antrean dokter lama dengan reservasi untuk Practice Session baru melalui kapabilitas pemilik antrean.
- **BR-ARJ-036** — Booking hanya berlaku untuk Visit Date-nya dan tidak boleh digunakan untuk memperoleh Registration pada tanggal berikutnya.
- **BR-ARJ-037** — Booking yang Visit Date-nya berlalu tanpa Registration, pembatalan, atau penjadwalan ulang harus tetap menjadi bukti historis Expired Unfulfilled.
- **BR-ARJ-038** — Expired Unfulfilled tidak boleh ditafsirkan sebagai bukti bahwa Patient tidak hadir secara fisik.
- **BR-ARJ-039** — HiDok tidak boleh menyediakan Booking Cancellation; pembatalan melalui kanal Rumah Sakit resmi lainnya tetap tunduk pada kebijakan akuntabilitasnya.
- **BR-ARJ-040** — Perubahan atau pembatalan Practice Schedule tidak boleh secara diam-diam memindahkan atau membatalkan Booking yang sudah ada.
- **BR-ARJ-041** — Booking dengan Unresolved Patient Identity harus menjalani validasi identitas melalui Admisi Rajal sebelum Outpatient Registration dapat dibentuk.
- **BR-ARJ-042** — Validasi identitas harus mengaitkan Booking dengan satu Patient kanonis yang sudah ada atau menyelesaikan New Patient Recording berdasarkan kebijakan Patient pemiliknya.
- **BR-ARJ-043** — Patient dengan Booking hanya boleh menuju ruang tunggu dokter setelah Outpatient Registration dibentuk melalui Self-Registration atau Registration berbantuan.
- **BR-ARJ-044** — Booking Admisi Rajal harus menyumbangkan bukti Booking kepada Patient Tracker, tetapi tidak boleh mendefinisikan aturan pembentukan, pemilihan, penggunaan ulang, atau lifecycle `TrackerId`.

### 7.6 Elaborasi bertahap

- **BR-ARJ-021** — Artefak fitur berikutnya dapat memperinci kapabilitas yang diakui dokumen ini, tetapi tidak boleh mengubah secara diam-diam batas kepemilikan atau istilah kanonis yang ditetapkan di sini.
- **BR-ARJ-022** — Ketika kebijakan baru yang disetujui bertentangan dengan spesifikasi awal ini, kedua dokumen bahasa dan setiap artefak fitur yang terdampak harus diperbarui bersama-sama.

## 8. State Machines & Lifecycles

### 8.1 Lifecycle Outpatient Registration

```text
Registration Not Established
  → Registration Established
      → Registration Cancelled
```

`Registration Established` berarti akses administratif rawat jalan yang otoritatif telah ada. `Registration Cancelled` berarti akses tersebut kemudian dibatalkan berdasarkan kebijakan pembatalan yang akuntabel. State penutupan kunjungan dan koreksi yang detail memerlukan elaborasi berikutnya.

### 8.2 Lifecycle Booking

```text
Booking Planned
  → Booking Rescheduled
      → Booking Planned

Booking Planned
  → Registration Established

Booking Planned
  → Booking Cancelled

Booking Planned
  → Expired Unfulfilled
```

Booking tidak berubah menjadi Outpatient Registration. `Registration Established` berarti Booking telah dikaitkan dengan Registration terpisah yang telah terbentuk. Penjadwalan ulang mempertahankan identitas Booking. Expired Unfulfilled tetap menjadi bukti historis dan bukan bukti ketidakhadiran fisik.

### 8.3 Lifecycle identifikasi Booking Patient

```text
Unresolved Patient Identity
  → Canonical Patient Identified
```

Booking dapat dimulai dalam salah satu kondisi tersebut. Identifikasi kanonis diwajibkan sebelum Outpatient Registration dibentuk.

### 8.4 Lifecycle Daily Practice Schedule

```text
Active
  → Cancelled

Active
  → Manually Overridden (Active)
```

Pengecualian manual spesifik tanggal tetap independen dari perubahan template berulang berikutnya, sesuai kebijakan jadwal yang telah disetujui.

### 8.5 Interpretasi Registration Assistance

```text
Waiting
  → In Service
      → Done
```

Ini adalah state Queue Entry Patient Tracker yang diinterpretasikan Admisi Rajal untuk keanggotaan Work List. State tersebut bukan state machine kedua yang dimiliki Admisi Rajal.

## 9. Domain Events

| Domain Event | Makna bisnis |
|---|---|
| Booking Planned | Kunjungan rawat jalan direncanakan terhadap tujuan dan Visit Date yang berlaku. |
| Booking Rescheduled | Booking yang sudah ada dipindahkan ke Practice Session lain yang berlaku tanpa mengganti identitasnya. |
| Booking Cancelled | Kunjungan rawat jalan yang direncanakan dibatalkan sebelum rencananya tetap berlaku. |
| Booking Expired Unfulfilled | Visit Date Booking telah berlalu tanpa Registration, pembatalan, atau penjadwalan ulang. |
| Booking Patient Identified | Booking dengan Unresolved Patient Identity telah dikaitkan dengan satu Patient kanonis. |
| Capacity Override Granted | Admission Officer menerima Admission-Assisted Booking yang melampaui Schedule Capacity. |
| Self-Registration Completed | Self-Registration berhasil membentuk Outpatient Registration yang otoritatif. |
| Self-Registration Requires Assistance | Self-Registration tidak membentuk Registration dan bantuan manusia menjadi diperlukan. |
| Registration Assistance Requested | Kebutuhan aktif akan registrasi rawat jalan berbantuan telah dikenali. |
| Registration Assistance Started | Admission Officer mulai menyelesaikan kewajiban bantuan; kebenaran layanan antrean tetap dimiliki Patient Tracker. |
| Outpatient Registration Established | Outpatient Registration yang otoritatif telah terbentuk. |
| Outpatient Registration Cancelled | Outpatient Registration yang sebelumnya terbentuk dibatalkan berdasarkan kebijakan yang akuntabel. |
| Coverage Arrangement Determined | Dasar pembayaran atau penjamin yang berlaku telah ditentukan untuk Registration. |
| Outpatient Destination Assigned | Poli dan pemberi asuhan yang dimaksud telah ditetapkan pada Registration. |
| Initial Charge Established | Kewajiban Initial Charge terkait Registration telah terbentuk. |

Events tersebut menyatakan fakta bisnis. Mekanisme publikasi dan pengirimannya merupakan tanggung jawab artefak arsitektur.

## 10. Workflow Bisnis

### 10.1 Create a HiDok Booking

```text
HiDok Booking requested
  → PasienId yang diberikan divalidasi bila ada
  → Practice Session yang berlaku diselesaikan
  → Schedule Capacity tersedia
  → Queue Number dokter diperoleh dari otoritas antrean Rumah Sakit
  → Booking Planned
  → Bukti Booking disumbangkan kepada Patient Tracker
```

HiDok tidak menyediakan Capacity Override atau Booking Cancellation.

### 10.2 Create an Admission-Assisted Booking

```text
Permintaan Booking diterima melalui telepon atau WhatsApp
  → Admission Officer mencatat permintaan sebagai Admission-Assisted
  → PasienId yang diberikan divalidasi bila ada
  → Practice Session yang berlaku diselesaikan
  → Schedule Capacity dievaluasi
      → Dalam kapasitas: permintaan dapat diterima
      → Melampaui kapasitas: Admission Officer menerima atau menolak
  → Queue Number dokter diperoleh dari otoritas antrean Rumah Sakit ketika diterima
  → Booking Planned
  → Bukti Booking disumbangkan kepada Patient Tracker
```

### 10.3 Reschedule a Booking

```text
Booking yang sudah ada dipilih untuk penjadwalan ulang
  → Practice Session baru yang berlaku diselesaikan
  → Kebijakan kapasitas sesuai channel diterapkan
  → Queue Number dokter baru diperoleh melalui otoritas antrean Rumah Sakit
  → Reservasi antrean dokter lama dilepas melalui kapabilitas pemilik antrean
  → Booking Rescheduled
  → Identitas Booking yang sama dipertahankan
```

### 10.4 By Booking with successful Self-Registration

```text
Booking Planned
  → Self-Registration attempted
  → Identitas Patient kanonis dan konteks registrasi yang berlaku divalidasi
  → Outpatient Registration Established
  → Self-Registration Completed
  → Registration Assistance tidak diperlukan
  → Patient melanjutkan menuju Outpatient Destination pada Booking
```

### 10.5 By Booking requiring identity validation or other assistance

```text
Booking Planned
  → Self-Registration attempted
  → Self-Registration Requires Assistance
  → Queue Entry Admisi Rajal teridentifikasi disediakan Patient Tracker
  → Registration Assistance muncul pada Admisi Rajal Work List
  → Admission Officer menyelesaikan identitas dan konteks registrasi lain yang diwajibkan
  → Outpatient Registration Established atau Registration Not Established
  → Layanan antrean diselesaikan melalui Patient Tracker
```

Unresolved Patient Identity merupakan alasan eksplisit bagi Registration Assistance, bukan bukti kegagalan teknis Self-Registration.

### 10.6 By Walk-In

```text
Patient meminta registrasi rawat jalan berbantuan
  → Queue Entry Admisi Rajal anonim dapat disediakan Patient Tracker
  → Registration Assistance muncul pada Admisi Rajal Work List
  → Admission Officer memulai bantuan
  → Patient Journey dan identitas Patient kanonis diselesaikan atau dibentuk melalui konteks pemiliknya
  → Visit Date, Outpatient Destination, dan konteks registrasi yang berlaku ditentukan
  → Outpatient Registration Established atau Registration Not Established
  → Layanan antrean diselesaikan melalui Patient Tracker
```

### 10.7 Progressive registration-context determination

```text
Kebutuhan Registration dikenali
  → Konteks identitas Patient ditentukan
  → Konteks coverage dan penjamin ditentukan bila berlaku
  → Konteks rujukan ditentukan bila berlaku
  → Outpatient Destination ditentukan
  → Konteks Initial Charge ditentukan
  → Outpatient Registration Established
```

Urutan detail, opsionalitas, kebutuhan bukti, pengecualian, dan kebijakan koreksi dalam workflow ini tetap menjadi bahan spesifikasi fitur berikutnya.
