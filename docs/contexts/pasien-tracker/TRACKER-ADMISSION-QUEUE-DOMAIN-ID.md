# Domain Patient Tracker — Operasi Antrean Admisi

**Status artefak:** Spesifikasi bisnis fitur kanonis

**Bounded context:** Patient Tracker

**Domain induk:** [Domain Patient Tracker](./TRACKER-DOMAIN-ID.md)

**Sumber kanonis bahasa Inggris:** [Patient Tracker — Admission Queue Operations Domain](./TRACKER-ADMISSION-QUEUE-DOMAIN.md)

**Spesifikasi operasional:** [SOP Operasi Antrean Admisi](./TRACKER-ADMISSION-QUEUE-SOP-ID.md)

**Spesifikasi teknis:** [Arsitektur Admission Queue Operations](./TRACKER-ADMISSION-QUEUE-ARCHITECTURE.md)

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan

Admission Queue Operations mengoordinasikan bagaimana Patient atau Visitor memperoleh Queue Number, menunggu, dipanggil, dan menerima Registration Assistance melalui antrean admisi rawat jalan.

Domain ini membedakan kategori antrean yang direpresentasikan oleh `Service Point` dari meja layanan fisik yang direpresentasikan oleh `Loket`. Domain ini mempertahankan identitas antrean dan milestone operasional tanpa membuat Patient Journey hanya karena Queue Number diterbitkan.

### 1.2 Nilai bisnis

Admission Queue Operations menyediakan:

- antrean yang dikelola secara mandiri untuk BPJS, umum, gedung tertentu, atau layanan admisi lainnya;
- Queue Label yang mudah dikenali seperti `A0001` dan `B0001`;
- hubungan terkendali antara Kiosk, Service Point, dan Loket;
- pemanggilan Queue Entry secara accountable menuju Loket fisik;
- pemisahan antara pemanggilan antrean dan pelayanan yang benar-benar dimulai;
- kesinambungan dari pengambilan antrean anonim sampai identifikasi Patient Journey kemudian; dan
- interpretasi waktu tunggu dan durasi pelayanan yang andal.

### 1.3 Cakupan

Fitur ini memiliki spesialisasi admisi dari kapabilitas bisnis berikut:

1. Admission Service Point Management.
2. Admission Queue Intake.
3. Queue Number Allocation and Labelling.
4. Operasi Loket terkonfigurasi.
5. Queue Call Coordination.
6. Admission Queue Exception Resolution.

Fitur ini mengelaborasi `Queue Session Aggregate` milik Patient Tracker dan memperkenalkan identitas bisnis stabil untuk `Service Point`. Dalam Pragmatic V1, Loket dan Kiosk merupakan konfigurasi deployment, bukan business resource yang dikelola.

### 1.4 Batas bisnis

Patient Tracker memiliki Service Point identity, Queue Prefix, Queue Session, Queue Entry, Queue Number, Queue Label, current Queue Call state, CallCount, current-per-Loket display state, dan milestone pelayanan antrean.

Admisi Rajal memiliki keputusan Registration Assistance dan outcome Outpatient Registration. Context Patient memiliki canonical Patient identity. Guarantor atau integrasi accountable miliknya memiliki external coverage eligibility truth.

Admission Queue Operations tidak mendefinisikan:

- cara Patient membuktikan eligibility BPJS, umum, atau layanan lainnya;
- isi atau outcome Outpatient Registration;
- implementasi fisik Kiosk, Queue Display, atau Admission Module;
- tata letak layar, perilaku tombol, teknologi pencetakan, teknologi audio, atau prosedur operator;
- application interface, pengiriman pesan, skema persistence, atau mekanisme concurrency; atau
- posisi fisik Patient sebelum interaksi pelayanan yang accountable terjadi.

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| Admission Queue Operations | Operasi Antrean Admisi | Koordinasi pengambilan antrean, pemanggilan, mulai pelayanan, dan penyelesaian untuk Registration Assistance rawat jalan. |
| Service Point | Titik Layanan | Kategori antrean yang diakui dan merepresentasikan satu tanggung jawab admisi, seperti Admisi BPJS atau Admisi Umum. |
| ServicePointId | Identitas Titik Layanan | Identitas bisnis yang stabil untuk satu Service Point. |
| Service Point Name | Nama Titik Layanan | Nama Service Point yang dapat dikenali manusia. |
| Queue Prefix | Awalan Antrean | Tepat satu huruf ASCII kapital yang ditetapkan kepada Service Point dan digunakan saat membentuk Queue Label. Input dinormalisasi dengan `Trim` dan `ToUpperInvariant`; spasi dan pemisah dilarang. |
| Loket | Meja Layanan | Meja layanan fisik yang diakui tempat Service Point Operator memberikan Registration Assistance. |
| LoketId | Identitas Loket | Identitas bisnis yang stabil untuk satu Loket fisik. |
| Configured Loket | Loket Terkonfigurasi | Identitas meja dari konfigurasi deployment workstation tepercaya. Dalam Pragmatic V1, objek ini bukan master resource Patient Tracker. |
| LoketKey | Kunci Loket | Identitas deployment stabil dan unik untuk satu Loket terkonfigurasi, berasal dari PC name atau nilai konfigurasi yang dikendalikan. |
| Kiosk | Kios Mandiri | Client mandiri dari konfigurasi deployment tempat Patient atau Visitor dapat meminta Admission Queue Entry. Dalam Pragmatic V1, objek ini bukan master resource Patient Tracker. |
| Queue Display | Tampilan Antrean | Kanal komunikasi operasional yang menampilkan Queue Call saat ini dan Loket tujuannya. |
| Current Queue Display State | Status Tampilan Antrean Saat Ini | Panggilan terlihat terbaru untuk satu LoketKey yang dipersistenkan dalam `BILRG_AdmLoketCurrentCall`. Ini adalah authoritative projection state, bukan riwayat panggilan. |
| AnnouncementVersion | Versi Pengumuman | Nilai yang selalu bertambah milik Current Queue Display State. Nilai bertambah hanya untuk Call atau Recall yang membutuhkan audio, bukan refresh biasa atau perubahan service state. |
| RowVersion | Versi Baris | Token concurrency database untuk Current Queue Display State. Nilainya berubah secara independen dari AnnouncementVersion setiap kali row diperbarui. |
| Admission Queue Worklist Projection | Proyeksi Daftar Kerja Antrean Admisi | Read model khusus antrean milik Patient Tracker yang memuat Queue Label, Service Point, state Queue Entry, call state, LoketKey, timestamp antrean, indikator Priority, dan TrackerId opsional. |
| Business Date | Tanggal Operasional | Tanggal operasional otoritatif yang diselesaikan di sisi server melalui kapabilitas Business Date bersama. Tanggal ini tidak diberikan secara otoritatif oleh Kiosk atau client lain. |
| Queue Session | Sesi Antrean | Satu-satunya antrean admisi untuk satu Service Point pada satu Business Date dalam Pragmatic V1, yang beroperasi dari `00:00:00` sampai `23:59:59.9999999`. |
| Queue Entry | Entri Antrean | Keikutsertaan satu Patient atau Visitor anonim dalam satu Queue Session. |
| Queue Number | Nomor Antrean | Nomor urut numerik yang ditetapkan kepada satu Queue Entry dan unik dalam Queue Session-nya. |
| Queue Label | Label Antrean | Identitas publik lima karakter yang dibentuk dengan menggabungkan Queue Prefix Snapshot satu huruf dan Queue Number empat digit tanpa pemisah, seperti `A0001`, `A0032`, atau `B1000`. |
| Queue Prefix Snapshot | Rekaman Awalan Antrean | Queue Prefix yang dipertahankan oleh Queue Session atau Queue Entry agar Queue Label yang telah diterbitkan mempertahankan makna aslinya setelah perubahan Service Point. |
| Queue Call | Panggilan Antrean | Permintaan accountable agar pemegang satu Waiting Queue Entry menuju satu Loket. Queue Call bukan bukti bahwa pelayanan telah dimulai. |
| CallCount | Jumlah Panggilan | Jumlah Queue Entry dipanggil, bertambah pada panggilan pertama dan setiap Recall. Nilai ini bukan riwayat detail Call Attempt. |
| Recall | Pemanggilan Ulang | Panggilan berikutnya untuk Queue Entry yang sama yang menambah CallCount tanpa mengalokasikan Queue Number lain. |
| Registration Assistance | Bantuan Registrasi | Pekerjaan administratif manusia yang diperlukan untuk membentuk atau menyelesaikan Outpatient Registration. |
| Registration Outcome Reference | Referensi Hasil Registrasi | OutcomeId stabil yang diberikan Admisi Rajal sebagai bukti bahwa Registration Assistance mencapai keputusan final `Established` atau `NotEstablished`. |
| No-Show | Tidak Hadir saat Dipanggil | Kesimpulan operasional bahwa pemegang Queue Entry yang dipanggil tidak hadir untuk pelayanan berdasarkan policy yang berlaku. |
| Priority Replacement Entry | Entri Pengganti Prioritas | Queue Entry Priority baru pada Service Point tujuan ketika kebutuhan dialihkan, dengan identitas sumber komposit `(SourceAntrianId, SourceNoUrut)` yang mereferensikan asal. Objek ini bukan Queue Transfer Aggregate tersendiri. |
| Priority | Prioritas | Indikator visual dan sorting milik antrean. Indikator tidak memaksa urutan pemanggilan otomatis; operator tetap memiliki otoritas pemilihan. |
| CreationReason | Alasan Pembentukan | Klasifikasi provenance Queue Entry: `Normal`, `Redirected`, atau `ManualPriority`. |
| Source Queue Entry Identity | Identitas Entri Asal | Referensi komposit opsional `(SourceAntrianId, SourceNoUrut)` ke Queue Entry asal; keduanya wajib untuk `Redirected` dan keduanya tidak ada untuk `Normal`. |
| Withdrawn | Dihentikan sebelum Dilayani | State terminal Queue Entry yang digunakan ketika pelayanan tidak akan dimulai, termasuk pengalihan yang disetujui menuju Service Point lain. |

## 3. Kapabilitas Bisnis

### 3.1 Admission Service Point Management

**Indonesia:** Pengelolaan Titik Layanan Admisi

Patient Tracker dapat membentuk dan mempertahankan Service Point admisi yang stabil beserta nama yang mudah dikenali dan Queue Prefix, serta dapat menghentikan Service Point dari penggunaan berikutnya tanpa menulis ulang Queue Label historis.

### 3.2 Admission Queue Intake

**Indonesia:** Pengambilan Antrean Admisi

Patient Tracker dapat membuat Anonymous Admission Queue Entry untuk Service Point aktif yang dipilih melalui Kiosk dengan konfigurasi lokal. Konfigurasi Kiosk lokal mengendalikan presentasi, bukan eligibility atau authorization truth di server.

Pengambilan antrean tidak membentuk atau memilih Patient Tracker.

### 3.3 Queue Number Allocation and Labelling

**Indonesia:** Alokasi dan Pelabelan Nomor Antrean

Patient Tracker dapat mengalokasikan Queue Number numerik dalam satu Queue Session dan menggabungkannya dengan Queue Prefix yang berlaku untuk menghasilkan Queue Label publik yang stabil.

### 3.4 Configured Loket Operation

**Indonesia:** Operasi Loket Terkonfigurasi

Patient Tracker mencatat identitas Loket dari konfigurasi deployment workstation. Setiap Admission Officer terautentikasi dapat mengoperasikan setiap Loket terkonfigurasi, dan setiap Loket terkonfigurasi dapat melayani seluruh Service Point aktif dalam Pragmatic V1.

### 3.5 Queue Call Coordination

**Indonesia:** Koordinasi Pemanggilan Antrean

Patient Tracker dapat mencatat bahwa Waiting Queue Entry dipanggil atau dipanggil ulang menuju satu Loket dari konfigurasi workstation, sambil mempertahankan perbedaan antara pemanggilan, kehadiran Patient, dan pelayanan yang benar-benar dimulai.

### 3.6 Admission Queue Exception Resolution

**Indonesia:** Penyelesaian Pengecualian Antrean Admisi

Patient Tracker dapat mempertahankan outcome accountable ketika Patient tidak hadir, memilih Service Point yang tidak berlaku, atau harus dialihkan, tanpa mencatat Registration Assistance secara keliru sebagai selesai.

### 3.7 Admission Queue Worklist Projection

**Indonesia:** Proyeksi Daftar Kerja Antrean Admisi

Patient Tracker dapat menyediakan proyeksi operasional khusus antrean untuk pemindaian antrean yang terotorisasi. Konteks Booking, identitas, Registration, dan administratif bukan bagian proyeksi ini; Admission Module atau application query Admisi Rajal menyusun enrichment tersebut tanpa membentuk ledger antrean lain.

## 4. Aktor & Peran

### 4.1 Patient or Visitor

Patient atau Visitor:

- memilih Service Point yang berlaku dan ditawarkan;
- menerima dan menyimpan Queue Label;
- menunggu Queue Call; dan
- menunjukkan bukti identitas, booking, atau coverage yang tersedia ketika diminta.

### 4.2 Admission Officer

Admission Officer bertindak sebagai Service Point Operator dan Journey Resolution Operator untuk Registration Assistance. Admission Officer:

- bekerja dari Loket yang dikonfigurasi pada workstation;
- dapat melayani setiap Service Point aktif;
- memanggil atau memanggil ulang Queue Entry;
- mengakui saat Registration Assistance benar-benar dimulai dan selesai; dan
- secara accountable menyelesaikan Patient Journey yang berlaku dari bukti yang tersedia.

### 4.3 Queue Operations Administrator

Queue Operations Administrator:

- membentuk dan menghentikan Service Point;
- menetapkan Queue Prefix;
- mengoordinasikan konfigurasi deployment Kiosk dan Loket di luar Patient Tracker; dan
- mencegah ambiguitas operasional di antara Queue Label yang digunakan secara bersamaan.

### 4.4 Queue Operations Supervisor

Queue Operations Supervisor menyelesaikan pengecualian yang diotorisasi seperti disposition No-Show, pengalihan layanan, atau perubahan sementara mengenai Loket yang melayani suatu Service Point.

## 5. Domain Objects

### 5.1 Service Point

Tujuan: merepresentasikan satu kategori kebutuhan layanan admisi yang diakui.

Tanggung jawab:

- mempertahankan ServicePointId yang stabil;
- mempertahankan Service Point Name;
- mempertahankan Queue Prefix yang berlaku untuk Queue Session baru; dan
- menentukan apakah Service Point tetap tersedia untuk pengambilan antrean berikutnya.

Service Point bukan Loket fisik. Contohnya adalah `ADM-BPJS` dan `ADM-UMUM`.

### 5.2 Configured Loket Reference

Tujuan: mencatat identitas dari konfigurasi workstation untuk meja fisik tempat Registration Assistance diberikan.

Tanggung jawab:

- mengidentifikasi tujuan yang ditampilkan kepada Patient dan Admission Officer; dan
- tetap menjadi referensi pada current queue/display state tanpa menjadi managed aggregate.

### 5.3 Kiosk Client

Tujuan: merepresentasikan client mandiri dari konfigurasi deployment.

Tanggung jawab:

- menampilkan Service Point dari konfigurasi workstation lokal; dan
- mengirim ServicePointId stabil yang dipilih sementara server memvalidasi bahwa Service Point aktif.

### 5.4 Queue Session

Tujuan: merepresentasikan satu-satunya antrean admisi untuk satu Service Point dan Business Date yang diselesaikan server, menggunakan interval Pragmatic V1 `00:00:00` sampai `23:59:59.9999999`.

Tanggung jawab:

- mempertahankan Service Point yang berlaku dan Queue Prefix Snapshot;
- mempertahankan Business Date otoritatif dan interval tetap Pragmatic V1;
- mengalokasikan Queue Number unik dalam session melalui legacy `ISequencer` yang menggunakan SequenceTag kanonis milik session;
- memiliki Queue Entry dan CallCount-nya; dan
- mempertahankan milestone pelayanan antrean.

### 5.5 Queue Entry

Tujuan: merepresentasikan keikutsertaan satu Visitor anonim atau Patient Journey teridentifikasi dalam admission Queue Session.

Tanggung jawab:

- mempertahankan Queue Number numerik dan Queue Label yang stabil;
- mempertahankan kondisi anonim atau teridentifikasi;
- mempertahankan CreatedAt, ServedAt, dan DoneAt; dan
- mempertahankan state pelayanan Waiting, In Service, Done, atau Withdrawn.

### 5.6 Queue Call

Tujuan: merepresentasikan kewajiban pemanggilan saat ini untuk satu Waiting Queue Entry dan CallCount-nya tanpa riwayat detail attempt.

Tanggung jawab:

- mengidentifikasi Queue Entry dan Loket tujuan;
- mempertahankan setiap kejadian pemanggilan atau Recall;
- membedakan panggilan yang masih Outstanding dari panggilan yang Acknowledged atau telah berakhir; dan
- tidak merepresentasikan panggilan sebagai mulai pelayanan.

### 5.7 Queue Label

Tujuan: menyediakan identitas publik yang digunakan Patient, Admission Officer, dan Queue Display.

Queue Label menggabungkan Queue Prefix Snapshot satu huruf dan Queue Number yang diformat tepat empat digit tanpa pemisah. Contoh: `1` → `A0001`, `32` → `A0032`, dan `1000` → `A1000`. Queue Label tetap tidak berubah setelah diterbitkan.

### 5.8 Current Queue Display State

Tujuan: menyimpan antrean saat ini yang dilayani setiap Loket terkonfigurasi dan AnnouncementVersion yang bertambah. Display pasif memuat ulang state ini setelah refresh SignalR atau polling periodik.

### 5.9 Client Request Identity

Tujuan: secara opsional mengorelasikan retry client dengan Queue Entry yang telah diterbitkan melalui ClientRequestId. Tanpa ClientRequestId, tidak ada jaminan deduplikasi retry.

## 6. Aggregates

### 6.1 Service Point Aggregate

**Aggregate Root:** `Service Point`

**Consistency boundary:**

Aggregate menjaga ServicePointId, Service Point Name, Queue Prefix, dan ketersediaan untuk pengambilan antrean berikutnya tetap konsisten satu sama lain. Queue Prefix Snapshot historis berada di luar aggregate ini dan tidak ditulis ulang ketika Service Point berubah.

### 6.2 Queue Session Aggregate

**Aggregate Root:** `Queue Session`

**Owned entities:**

- nol atau lebih entity `Queue Entry` dengan current-call fields, termasuk CallCount.

**Consistency boundary:**

Persistence root Queue Session khusus menjaga Service Point identity, Business Date, Queue Prefix Snapshot, SequenceTag kanonis, keunikan Queue Number, asosiasi identitas Queue Entry, current call state, Loket terkonfigurasi tujuan, dan milestone pelayanan tetap konsisten satu sama lain. Legacy `ISequencer` menjadi satu-satunya otoritas alokasi.

Queue Entry mereferensikan Patient Tracker melalui TrackerId setelah identifikasi tetapi tidak memiliki atau memodifikasi Patient Tracker Aggregate.

## 7. Aturan Bisnis

### 7.1 Service Point dan konfigurasi deployment

- **BR-AQO-001** — Setiap Service Point harus memiliki satu ServicePointId yang stabil, satu Service Point Name, dan satu Queue Prefix.
- **BR-AQO-001a** — Input Queue Prefix harus dinormalisasi menggunakan `Trim` kemudian `ToUpperInvariant` dan harus berisi tepat satu huruf ASCII kapital (`A`–`Z`) tanpa spasi atau pemisah.
- **BR-AQO-002** — ServicePointId harus mengidentifikasi kategori antrean dan tidak boleh mengidentifikasi Loket fisik.
- **BR-AQO-003** — Identitas Loket terkonfigurasi harus merepresentasikan satu meja layanan fisik, tetapi bukan managed master resource Patient Tracker dalam Pragmatic V1.
- **BR-AQO-004** — Setiap Loket terkonfigurasi boleh melayani seluruh Service Point admisi aktif; aplikasi tidak menegakkan matriks otorisasi Loket-ke-Service Point dalam Pragmatic V1.
- **BR-AQO-005** — Service Point yang terlihat pada Kiosk berasal dari konfigurasi deployment lokal; server memvalidasi ServicePointId yang dikirim ada dan aktif, tetapi tidak memelihara Kiosk offering records.
- **BR-AQO-006** — Service Point yang memiliki aktivitas antrean historis harus dihentikan dari penggunaan berikutnya dan tidak boleh dihapus dari riwayat bisnis.
- **BR-AQO-006a** — Setiap Admission Officer terautentikasi boleh mengoperasikan setiap Loket terkonfigurasi.
- **BR-AQO-006b** — LoketKey berasal dari konfigurasi deployment workstation tepercaya; server tidak boleh menerima nilai sembarang yang dimasukkan operator. Ini adalah deployment trust boundary V1 yang diterima, bukan server-managed assignment authority.
- **BR-AQO-006c** — Client Queue Display bersifat pasif dan harus memuat ulang shared current-per-Loket display state setelah refresh notification atau polling periodik.
- **BR-AQO-006d** — `BILRG_AdmLoketCurrentCall` hanya boleh memuat panggilan terlihat terbaru untuk setiap LoketKey dan tidak boleh digunakan untuk merekonstruksi aktivitas antrean lampau.
- **BR-AQO-006e** — LoketKey harus unik di seluruh workstation admisi yang dideploy. Konfigurasi yang hilang atau duplikat harus memblokir Call dan Recall.
- **BR-AQO-006f** — Penggantian nama workstation harus melalui pembaruan konfigurasi terkendali yang mempertahankan LoketKey yang dimaksud atau mengubahnya secara accountable.
- **BR-AQO-006g** — Konfigurasi deployment hanya boleh mereferensikan ServicePointId existing yang aktif dan tidak boleh membentuk atau mengubah Service Point master data.

### 7.2 Queue Number dan Queue Label

- **BR-AQO-007** — Setiap admission Queue Session harus mengidentifikasi tepat satu Service Point, satu Business Date otoritatif, interval `00:00:00` sampai `23:59:59.9999999`, dan Queue Prefix yang berlaku ketika session tersebut dibentuk.
- **BR-AQO-007a** — Pragmatic V1 mengizinkan paling banyak satu admission Queue Session untuk satu Service Point pada satu Business Date.
- **BR-AQO-007b** — Server harus menyelesaikan Business Date otoritatif melalui kapabilitas Business Date bersama. Kiosk atau client lain tidak boleh memberikan atau mengganti tanggal otoritatif tersebut.
- **BR-AQO-007c** — Queue Session yang berlaku harus dibentuk secara lazy oleh valid intake request pertama ketika belum ada session untuk Service Point dan Business Date tersebut.
- **BR-AQO-007d** — Intake harus ditolak ketika ServicePointId yang dikirim tidak mengidentifikasi Service Point aktif. Konfigurasi offering lokal Kiosk bukan otoritas server.
- **BR-AQO-008** — Setiap Queue Entry harus menerima tepat satu Queue Number numerik dari 1 sampai 9999 yang unik dalam Queue Session-nya.
- **BR-AQO-008a** — Setelah Queue Number 9999 dialokasikan, Queue Session harus menolak alokasi berikutnya. Berdasarkan kebijakan V1 satu session per Service Point per Business Date, session berikutnya hanya tersedia pada Business Date otoritatif berikutnya; sistem tidak boleh membuat session kedua pada tanggal yang sama, mengulang counter, atau diam-diam mengubah format Queue Label.
- **BR-AQO-009** — Setiap Queue Entry harus memiliki Queue Label lima karakter yang dibentuk dengan menggabungkan Queue Prefix Snapshot satu huruf dan Queue Number empat digit tanpa pemisah: `1` → `A0001`, `32` → `A0032`, dan `1000` → `A1000` untuk prefix `A`.
- **BR-AQO-010** — Queue Label harus tetap tidak berubah setelah diterbitkan walaupun Service Point Name atau Queue Prefix berubah kemudian.
- **BR-AQO-011** — Queue Prefix harus unik di seluruh admission Service Point yang aktif.
- **BR-AQO-012** — Ketika ClientRequestId diberikan, retry dengan identifier yang sama harus mengembalikan Queue Entry existing dan tidak mengalokasikan Queue Number lain. Tanpa ClientRequestId, pencegahan duplikasi tidak dijamin.

### 7.3 Pengambilan anonim dan asosiasi Patient Journey

- **BR-AQO-013** — Memperoleh admission Queue Number harus membuat Anonymous Queue Entry ketika Patient Journey belum diselesaikan secara accountable dan tidak boleh membentuk atau memilih Patient Tracker.
- **BR-AQO-014** — Walk-In Queue Entry harus tetap Anonymous sampai Registration membentuk Patient Tracker baru atau Journey Resolution yang accountable memilih Patient Tracker existing yang berlaku.
- **BR-AQO-015** — Admission Queue Entry milik Booking Patient tidak boleh diasosiasikan dengan Patient Tracker hanya dari Booking evidence; Admission Officer harus menyelesaikan existing Booking Patient Tracker yang berlaku dari bukti yang diberikan Patient.
- **BR-AQO-016** — Asosiasi Anonymous Queue Entry dengan Patient Tracker harus tunggal, accountable, dan tidak dapat dibalik dalam Queue Entry tersebut.

### 7.4 Pemanggilan dan pelayanan antrean

- **BR-AQO-017** — Hanya Waiting Queue Entry yang boleh memiliki Queue Call yang Outstanding.
- **BR-AQO-018** — Setiap Queue Call harus mengidentifikasi tepat satu Loket tujuan dari konfigurasi deployment.
- **BR-AQO-019** — Satu Queue Entry tidak boleh memiliki lebih dari satu Queue Call yang Outstanding pada waktu yang sama.
- **BR-AQO-020** — Satu LoketKey hanya boleh memiliki paling banyak satu Queue Entry current yang Outstanding atau In Service pada satu waktu dalam Pragmatic V1.
- **BR-AQO-021** — Panggilan pertama atau Recall harus menaikkan CallCount pada Queue Entry dan tidak boleh mengalokasikan Queue Number lain. Riwayat detail Call Attempt berada di luar Pragmatic V1.
- **BR-AQO-021a** — Call atau Recall yang membutuhkan audio harus menaikkan AnnouncementVersion. Refresh layar biasa dan perubahan service state tidak menaikkannya kecuali audio announcement secara eksplisit diperlukan.
- **BR-AQO-021b** — CallCount hanya bersifat informasional. Nilai ini tidak boleh otomatis menetapkan No-Show, menunda entry, menghitung aturan “lima pasien berikutnya”, atau memilih entry berikutnya.
- **BR-AQO-022** — Current Queue Call state atau CallCount tidak boleh dengan sendirinya membentuk ServedAt atau membuktikan bahwa Registration Assistance telah dimulai.
- **BR-AQO-023** — Waiting Queue Entry hanya dapat masuk ke In Service ketika Admission Officer mengakui bahwa Registration Assistance telah dimulai di Loket tujuan.
- **BR-AQO-024** — In Service Queue Entry hanya dapat menjadi Done setelah Admisi Rajal mempersistenkan Registration Outcome final yang eksplisit dengan OutcomeId stabil, QueueEntryId yang sesuai, Result, DecidedAt, dan DecidedBy.
- **BR-AQO-024a** — Validation error yang dapat dikoreksi bukan Registration Outcome final dan harus mempertahankan Queue Entry sebagai In Service.
- **BR-AQO-024b** — Outcome `Established` harus memiliki RegId. Outcome `NotEstablished` harus memiliki ReasonCode dan dapat memiliki Explanation; outcome tersebut harus merupakan keputusan final operator yang eksplisit.
- **BR-AQO-024c** — Queue Entry yang diselesaikan dari outcome final `NotEstablished` dapat tetap Anonymous ketika tidak ada Patient Journey yang dibentuk.
- **BR-AQO-025** — Registration outcome tetap authoritative di Admisi Rajal; penyelesaian Queue Entry tidak boleh secara mandiri membentuk Outpatient Registration.

### 7.5 Pengecualian dan kebenaran historis

- **BR-AQO-026** — Kesimpulan No-Show tidak boleh direpresentasikan sebagai Registration Assistance yang selesai. Pragmatic V1 hanya mempertahankan CallCount, sehingga aturan yang membutuhkan timing atau bukti detail panggilan sebelumnya tetap menjadi prosedur operasional manual.
- **BR-AQO-027** — Pengalihan kebutuhan membentuk Queue Entry Priority baru pada Service Point tujuan dengan CreationReason `Redirected` dan `(SourceAntrianId, SourceNoUrut)` komposit yang mereferensikan asal. Queue Entry asal harus menerima disposition nonaktif yang eksplisit dan tidak boleh diganti label secara diam-diam. Tidak ada QueueTransfer aggregate atau transfer-history table dalam Pragmatic V1.
- **BR-AQO-027a** — Priority entry harus menampilkan indikator dan boleh berpartisipasi dalam sorting, tetapi tidak boleh otomatis melewati entry lain atau memaksa urutan pemanggilan. Operator memilih entry tersedia yang akan dipanggil.
- **BR-AQO-027b** — CreationReason harus tepat `Normal`, `Redirected`, atau `ManualPriority`. `Normal` tidak memiliki kedua field source key; `Redirected` mewajibkan keduanya; `ManualPriority` boleh mempertahankan keduanya hanya ketika entry asal benar-benar ada. Source identity komposit parsial dilarang.
- **BR-AQO-028** — Queue Display harus menampilkan Queue Call truth yang tercatat dan tidak boleh memiliki keputusan pemilihan atau lifecycle Queue Entry.
- **BR-AQO-029** — Waktu tunggu dan pelayanan antrean harus diturunkan dari milestone Queue Entry dan tidak boleh disimpulkan hanya dari Queue Call.
- **BR-AQO-030** — Admission Queue Operations tidak boleh menyatakan kehadiran fisik Patient sebelum interaksi pelayanan yang accountable mengakuinya.

## 8. State Machines & Lifecycles

### 8.1 Lifecycle ketersediaan Service Point

```text
Active
  → Retired
```

Retired berarti tidak tersedia untuk intake antrean baru. Queue Label historis tetap valid. Loket dan Kiosk tidak memiliki managed lifecycle dalam Pragmatic V1.

### 8.2 Lifecycle pelayanan Queue Entry

```text
Waiting
  → In Service
      → Done

Waiting
  → Withdrawn
```

Lifecycle Queue Entry memperluas lifecycle domain induk Patient Tracker dengan `Withdrawn` untuk keikutsertaan antrean yang berakhir sebelum pelayanan dimulai. Queue Call terjadi saat Queue Entry masih Waiting dan tidak menambahkan state pelayanan kedua.

### 8.3 Lifecycle Queue Call

```text
Outstanding
  → Acknowledged

Outstanding
  → No-Show

Outstanding
  → Withdrawn
```

`Acknowledged` berarti kewajiban pemanggilan berakhir karena pelayanan diakui telah dimulai. `No-Show` berarti policy no-show yang berlaku selesai tanpa mulai pelayanan. `Withdrawn` berarti panggilan berakhir tanpa menyatakan pelayanan atau no-show. Recall menaikkan CallCount selama Queue Call tetap Outstanding.

### 8.4 Lifecycle identifikasi Queue Entry

```text
Anonymous Queue Entry
  → Identified Queue Entry
```

Penerbitan antrean dan Queue Call tidak menyebabkan transisi ini. Transisi hanya terjadi melalui asosiasi accountable dengan Patient Tracker existing atau Patient Tracker baru yang dibentuk oleh aktivitas sumber pemilik.

Registration Outcome final `NotEstablished` dapat menyelesaikan service lifecycle sementara identification lifecycle ini tetap Anonymous.

## 9. Domain Events

| Domain Event | Makna bisnis |
|---|---|
| Admission Service Point Established | Kategori baru untuk layanan antrean admisi mulai diakui. |
| Admission Service Point Retired | Service Point berhenti menerima pengambilan antrean baru sambil mempertahankan riwayatnya. |
| Loket Authorized for Service Point | Loket fisik mulai diizinkan melayani satu Service Point. |
| Kiosk Service Point Offered | Service Point mulai tersedia untuk dipilih Patient melalui satu Kiosk. |
| Admission Queue Session Established | Antrean admisi dibentuk untuk satu Service Point dan interval operasional. |
| Admission Queue Entry Created | Anonymous atau Identified Queue Entry menerima Queue Number dan Queue Label. |
| Admission Queue Entry Called | Waiting Queue Entry dipanggil menuju satu Loket yang diotorisasi. |
| Admission Queue Entry Recalled | CallCount dinaikkan untuk Queue Entry dan Loket yang sama. |
| Admission Queue Call Acknowledged | Panggilan Outstanding berakhir karena Registration Assistance diakui telah dimulai. |
| Admission Queue Entry Marked No-Show | Panggilan Outstanding berakhir tanpa Patient atau Visitor hadir berdasarkan policy yang berlaku. |
| Admission Queue Entry Identified | Anonymous Admission Queue Entry diasosiasikan dengan satu Patient Tracker. |
| Admission Queue Service Started | Registration Assistance untuk Waiting Queue Entry dimulai di satu Loket. |
| Admission Queue Service Completed | Registration Assistance untuk In Service Queue Entry mencapai Registration Outcome final yang dipersistenkan dan diidentifikasi oleh OutcomeId. |
| Admission Queue Entry Withdrawn | Waiting Queue Entry berakhir tanpa mulai pelayanan. |
| Priority Replacement Queue Entry Issued | Queue Entry Priority baru diterbitkan pada Service Point lain dengan referensi opsional ke asal. |

Event ini merupakan fakta bisnis yang stabil. Perilaku display, audio, integrasi, dan delivery berada dalam artifact architecture.

## 10. Workflow Bisnis

### 10.1 Memperoleh admission Queue Number

```text
Patient atau Visitor memilih Service Point yang ditawarkan
  → Queue Session yang berlaku diakui
  → Anonymous Queue Entry dibuat
  → Queue Number dialokasikan
  → Queue Label diterbitkan
  → Patient atau Visitor menunggu
```

Workflow ini tidak membuat atau memilih Patient Tracker.

### 10.2 Memanggil dan memulai Registration Assistance

```text
Admission Officer bekerja dari Loket yang diotorisasi
  → Admission Officer memilih Service Point aktif
  → Waiting Queue Entry dipilih
  → Queue Entry dipanggil menuju Loket
  → Patient atau Visitor hadir di Loket
  → Queue Call diakui
  → Registration Assistance dimulai
  → Queue Entry masuk ke In Service
```

Pemanggilan dan mulai pelayanan merupakan fakta bisnis yang berbeda.

### 10.3 Menyelesaikan Booking Patient Journey

```text
Anonymous admission Queue Entry berada dalam In Service
  → Admission Officer meminta bukti yang diberikan Patient
  → Existing Booking Patient Tracker yang berlaku diselesaikan
  → Queue Entry diasosiasikan dengan Tracker tersebut
  → Registration Assistance berlanjut
  → Registration outcome ditentukan oleh Admisi Rajal
  → Pelayanan antrean diselesaikan
```

### 10.4 Menyelesaikan Walk-In Patient Journey

```text
Anonymous admission Queue Entry berada dalam In Service
  → Admission Officer meminta bukti yang diberikan Patient
  → Patient Tracker existing yang berlaku dipilih ketika tersedia
     atau Registration membentuk Patient Tracker baru
  → Queue Entry existing diasosiasikan dengan Tracker tersebut
  → Registration outcome ditentukan oleh Admisi Rajal
  → Pelayanan antrean diselesaikan
```

### 10.5 Recall atau menyimpulkan No-Show

```text
Queue Call tetap Outstanding
  → Admission Officer melakukan Recall terhadap Queue Entry yang sama
     atau policy No-Show yang berlaku terpenuhi
  → CallCount dinaikkan ketika Recall
     atau Queue Call berakhir sebagai No-Show
```

Recall tidak mengalokasikan Queue Number lain. No-Show tidak berarti Registration Assistance selesai.

### 10.6 Mengalihkan ke Service Point lain

```text
Service Point yang dipilih ditemukan tidak berlaku
  → Pengalihan accountable disetujui
  → Queue Entry asli menjadi Withdrawn dan dipertahankan sebagai riwayat
  → Queue Entry pengganti dibentuk untuk Service Point yang berlaku
  → Hubungan antara kedua keikutsertaan antrean dipertahankan
```

Queue Label asli tidak diubah secara diam-diam menjadi label milik Service Point lain.
