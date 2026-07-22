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
- Queue Label yang mudah dikenali seperti `A001` dan `B001`;
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
4. Loket Service Authorization.
5. Queue Call Coordination.
6. Admission Queue Exception Resolution.

Fitur ini mengelaborasi `Queue Session Aggregate` milik Patient Tracker dan memperkenalkan identitas bisnis yang stabil untuk `Service Point`, `Loket`, dan `Kiosk`.

### 1.4 Batas bisnis

Patient Tracker memiliki Service Point identity, Queue Prefix, Queue Session, Queue Entry, Queue Number, Queue Label, Queue Call, Loket Service Authorization, Kiosk Service Offering, dan milestone pelayanan antrean.

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
| Queue Prefix | Awalan Antrean | Urutan karakter yang ditetapkan kepada Service Point dan digunakan saat membentuk Queue Label. |
| Loket | Meja Layanan | Meja layanan fisik yang diakui tempat Service Point Operator memberikan Registration Assistance. |
| LoketId | Identitas Loket | Identitas bisnis yang stabil untuk satu Loket fisik. |
| Loket Service Authorization | Otorisasi Layanan Loket | Izin aktif bagi satu Loket untuk melayani satu Service Point. |
| Kiosk | Kios Mandiri | Fasilitas pengambilan antrean mandiri yang diakui tempat Patient atau Visitor dapat meminta Admission Queue Entry. |
| Kiosk Service Offering | Penawaran Layanan Kios | Ketersediaan aktif satu Service Point melalui satu Kiosk. |
| Queue Display | Tampilan Antrean | Kanal komunikasi operasional yang menampilkan Queue Call saat ini dan Loket tujuannya. |
| Queue Session | Sesi Antrean | Satu antrean dengan batas waktu untuk satu Service Point pada satu Session Date. |
| Queue Entry | Entri Antrean | Keikutsertaan satu Patient atau Visitor anonim dalam satu Queue Session. |
| Queue Number | Nomor Antrean | Nomor urut numerik yang ditetapkan kepada satu Queue Entry dan unik dalam Queue Session-nya. |
| Queue Label | Label Antrean | Identitas yang terlihat publik dan dibentuk dari Queue Prefix yang berlaku saat penerbitan serta Queue Number terformat, seperti `A001`. |
| Queue Prefix Snapshot | Rekaman Awalan Antrean | Queue Prefix yang dipertahankan oleh Queue Session atau Queue Entry agar Queue Label yang telah diterbitkan mempertahankan makna aslinya setelah perubahan Service Point. |
| Queue Call | Panggilan Antrean | Permintaan accountable agar pemegang satu Waiting Queue Entry menuju satu Loket. Queue Call bukan bukti bahwa pelayanan telah dimulai. |
| Call Attempt | Upaya Pemanggilan | Satu kejadian pemanggilan atau pemanggilan ulang Queue Entry menuju Loket. |
| Recall | Pemanggilan Ulang | Call Attempt berikutnya untuk Queue Entry yang sama tanpa mengalokasikan Queue Number lain. |
| Registration Assistance | Bantuan Registrasi | Pekerjaan administratif manusia yang diperlukan untuk membentuk atau menyelesaikan Outpatient Registration. |
| No-Show | Tidak Hadir saat Dipanggil | Kesimpulan operasional bahwa pemegang Queue Entry yang dipanggil tidak hadir untuk pelayanan berdasarkan policy yang berlaku. |
| Service Point Transfer | Pengalihan Titik Layanan | Pengalihan accountable atas kebutuhan antrean yang belum terselesaikan dari satu Service Point ke Service Point lain. |
| Withdrawn | Dihentikan sebelum Dilayani | State terminal Queue Entry yang digunakan ketika pelayanan tidak akan dimulai, termasuk pengalihan yang disetujui menuju Service Point lain. |

## 3. Kapabilitas Bisnis

### 3.1 Admission Service Point Management

**Indonesia:** Pengelolaan Titik Layanan Admisi

Patient Tracker dapat membentuk dan mempertahankan Service Point admisi yang stabil beserta nama yang mudah dikenali dan Queue Prefix, serta dapat menghentikan Service Point dari penggunaan berikutnya tanpa menulis ulang Queue Label historis.

### 3.2 Admission Queue Intake

**Indonesia:** Pengambilan Antrean Admisi

Patient Tracker dapat menawarkan satu atau lebih Service Point yang berlaku melalui Kiosk dan membuat Anonymous Admission Queue Entry untuk Service Point yang dipilih Patient atau Visitor.

Pengambilan antrean tidak membentuk atau memilih Patient Tracker.

### 3.3 Queue Number Allocation and Labelling

**Indonesia:** Alokasi dan Pelabelan Nomor Antrean

Patient Tracker dapat mengalokasikan Queue Number numerik dalam satu Queue Session dan menggabungkannya dengan Queue Prefix yang berlaku untuk menghasilkan Queue Label publik yang stabil.

### 3.4 Loket Service Authorization

**Indonesia:** Otorisasi Layanan Loket

Patient Tracker dapat mengidentifikasi Loket fisik dan menentukan Service Point yang boleh dilayani setiap Loket. Satu Loket dapat melayani satu atau lebih Service Point, dan satu Service Point dapat dilayani satu atau lebih Loket.

### 3.5 Queue Call Coordination

**Indonesia:** Koordinasi Pemanggilan Antrean

Patient Tracker dapat mencatat bahwa Waiting Queue Entry dipanggil atau dipanggil ulang menuju satu Loket yang berwenang, sambil mempertahankan perbedaan antara pemanggilan, kehadiran Patient, dan pelayanan yang benar-benar dimulai.

### 3.6 Admission Queue Exception Resolution

**Indonesia:** Penyelesaian Pengecualian Antrean Admisi

Patient Tracker dapat mempertahankan outcome accountable ketika Patient tidak hadir, memilih Service Point yang tidak berlaku, atau harus dialihkan, tanpa mencatat Registration Assistance secara keliru sebagai selesai.

## 4. Aktor & Peran

### 4.1 Patient or Visitor

Patient atau Visitor:

- memilih Service Point yang berlaku dan ditawarkan;
- menerima dan menyimpan Queue Label;
- menunggu Queue Call; dan
- menunjukkan bukti identitas, booking, atau coverage yang tersedia ketika diminta.

### 4.2 Admission Officer

Admission Officer bertindak sebagai Service Point Operator dan Journey Resolution Operator untuk Registration Assistance. Admission Officer:

- bekerja dari Loket yang ditetapkan;
- hanya melayani Service Point yang diotorisasi;
- memanggil atau memanggil ulang Queue Entry;
- mengakui saat Registration Assistance benar-benar dimulai dan selesai; dan
- secara accountable menyelesaikan Patient Journey yang berlaku dari bukti yang tersedia.

### 4.3 Queue Operations Administrator

Queue Operations Administrator:

- membentuk dan menghentikan Service Point, Kiosk, dan Loket;
- menetapkan Queue Prefix;
- memelihara Kiosk Service Offering dan Loket Service Authorization; dan
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

### 5.2 Loket

Tujuan: merepresentasikan satu meja fisik yang diakui tempat Registration Assistance dapat diberikan.

Tanggung jawab:

- mempertahankan LoketId yang stabil;
- tetap dapat dibedakan oleh Patient dan Admission Officer; dan
- mempertahankan Loket Service Authorization aktifnya.

### 5.3 Kiosk

Tujuan: merepresentasikan satu fasilitas pengambilan antrean mandiri yang diakui.

Tanggung jawab:

- mempertahankan identitas Kiosk yang stabil;
- mempertahankan Kiosk Service Offering aktifnya; dan
- hanya menawarkan Service Point yang tersedia melalui Kiosk tersebut.

### 5.4 Queue Session

Tujuan: merepresentasikan satu antrean admisi untuk satu Service Point dan interval operasional.

Tanggung jawab:

- mempertahankan Service Point yang berlaku dan Queue Prefix Snapshot;
- mengalokasikan Queue Number yang unik dalam session;
- memiliki Queue Entry dan Call Attempt-nya; dan
- mempertahankan milestone pelayanan antrean.

### 5.5 Queue Entry

Tujuan: merepresentasikan keikutsertaan satu Visitor anonim atau Patient Journey teridentifikasi dalam admission Queue Session.

Tanggung jawab:

- mempertahankan Queue Number numerik dan Queue Label yang stabil;
- mempertahankan kondisi anonim atau teridentifikasi;
- mempertahankan CreatedAt, ServedAt, dan DoneAt; dan
- mempertahankan state pelayanan Waiting, In Service, Done, atau Withdrawn.

### 5.6 Queue Call

Tujuan: merepresentasikan kewajiban pemanggilan saat ini untuk satu Waiting Queue Entry dan mempertahankan Call Attempt-nya.

Tanggung jawab:

- mengidentifikasi Queue Entry dan Loket tujuan;
- mempertahankan setiap kejadian pemanggilan atau Recall;
- membedakan panggilan yang masih Outstanding dari panggilan yang Acknowledged atau telah berakhir; dan
- tidak merepresentasikan panggilan sebagai mulai pelayanan.

### 5.7 Queue Label

Tujuan: menyediakan identitas publik yang digunakan Patient, Admission Officer, dan Queue Display.

Queue Label menggabungkan Queue Prefix Snapshot dan Queue Number terformat. Queue Label tetap tidak berubah setelah diterbitkan.

### 5.8 Loket Service Authorization

Tujuan: merepresentasikan izin bisnis bagi satu Loket untuk melayani satu Service Point selama periode aktifnya.

### 5.9 Kiosk Service Offering

Tujuan: merepresentasikan ketersediaan bisnis satu Service Point melalui satu Kiosk selama periode aktifnya.

## 6. Aggregates

### 6.1 Service Point Aggregate

**Aggregate Root:** `Service Point`

**Consistency boundary:**

Aggregate menjaga ServicePointId, Service Point Name, Queue Prefix, dan ketersediaan untuk pengambilan antrean berikutnya tetap konsisten satu sama lain. Queue Prefix Snapshot historis berada di luar aggregate ini dan tidak ditulis ulang ketika Service Point berubah.

### 6.2 Loket Aggregate

**Aggregate Root:** `Loket`

**Owned entities:**

- nol atau lebih entity `Loket Service Authorization`.

**Consistency boundary:**

Aggregate menjaga Loket identity, operational availability, dan Service Point yang diotorisasi tetap konsisten satu sama lain.

### 6.3 Kiosk Aggregate

**Aggregate Root:** `Kiosk`

**Owned entities:**

- nol atau lebih entity `Kiosk Service Offering`.

**Consistency boundary:**

Aggregate menjaga Kiosk identity, operational availability, dan Service Point yang ditawarkan tetap konsisten satu sama lain.

### 6.4 Queue Session Aggregate

**Aggregate Root:** `Queue Session`

**Owned entities:**

- nol atau lebih entity `Queue Entry`;
- nol atau lebih entity `Queue Call`; dan
- nol atau lebih catatan `Call Attempt` dalam setiap Queue Call.

**Consistency boundary:**

Aggregate menjaga Service Point identity, Queue Prefix Snapshot, keunikan Queue Number, asosiasi identitas Queue Entry, disposition Queue Call, Loket tujuan, dan milestone pelayanan tetap konsisten satu sama lain.

Queue Entry mereferensikan Patient Tracker melalui TrackerId setelah identifikasi tetapi tidak memiliki atau memodifikasi Patient Tracker Aggregate.

## 7. Aturan Bisnis

### 7.1 Identitas Service Point, Loket, dan Kiosk

- **BR-AQO-001** — Setiap Service Point harus memiliki satu ServicePointId yang stabil, satu Service Point Name, dan satu Queue Prefix.
- **BR-AQO-002** — ServicePointId harus mengidentifikasi kategori antrean dan tidak boleh mengidentifikasi Loket fisik.
- **BR-AQO-003** — Setiap Loket harus memiliki satu LoketId yang stabil dan harus merepresentasikan tepat satu meja layanan fisik.
- **BR-AQO-004** — Satu Loket dapat melayani beberapa Service Point, dan satu Service Point dapat dilayani beberapa Loket, hanya melalui Loket Service Authorization yang aktif.
- **BR-AQO-005** — Satu Kiosk dapat menawarkan satu atau lebih Service Point hanya melalui Kiosk Service Offering yang aktif.
- **BR-AQO-006** — Service Point, Loket, atau Kiosk yang memiliki aktivitas antrean historis harus dihentikan dari penggunaan berikutnya dan tidak boleh dihapus dari riwayat bisnis.

### 7.2 Queue Number dan Queue Label

- **BR-AQO-007** — Setiap admission Queue Session harus mengidentifikasi tepat satu Service Point dan mempertahankan Queue Prefix yang berlaku ketika session tersebut dibentuk.
- **BR-AQO-008** — Setiap Queue Entry harus menerima tepat satu Queue Number numerik yang unik dalam Queue Session-nya.
- **BR-AQO-009** — Setiap Queue Entry harus memiliki satu Queue Label yang dibentuk dari Queue Prefix Snapshot dan Queue Number terformat.
- **BR-AQO-010** — Queue Label harus tetap tidak berubah setelah diterbitkan walaupun Service Point Name atau Queue Prefix berubah kemudian.
- **BR-AQO-011** — Queue Prefix tidak boleh menghasilkan Queue Label aktif yang ambigu di antara Service Point yang diumumkan kepada kelompok Patient yang sama.
- **BR-AQO-012** — Penerbitan ulang atau penyajian ulang bukti Queue Entry yang sama tidak boleh mengalokasikan Queue Number lain.

### 7.3 Pengambilan anonim dan asosiasi Patient Journey

- **BR-AQO-013** — Memperoleh admission Queue Number harus membuat Anonymous Queue Entry ketika Patient Journey belum diselesaikan secara accountable dan tidak boleh membentuk atau memilih Patient Tracker.
- **BR-AQO-014** — Walk-In Queue Entry harus tetap Anonymous sampai Registration membentuk Patient Tracker baru atau Journey Resolution yang accountable memilih Patient Tracker existing yang berlaku.
- **BR-AQO-015** — Admission Queue Entry milik Booking Patient tidak boleh diasosiasikan dengan Patient Tracker hanya dari Booking evidence; Admission Officer harus menyelesaikan existing Booking Patient Tracker yang berlaku dari bukti yang diberikan Patient.
- **BR-AQO-016** — Asosiasi Anonymous Queue Entry dengan Patient Tracker harus tunggal, accountable, dan tidak dapat dibalik dalam Queue Entry tersebut.

### 7.4 Pemanggilan dan pelayanan antrean

- **BR-AQO-017** — Hanya Waiting Queue Entry yang boleh memiliki Queue Call yang Outstanding.
- **BR-AQO-018** — Setiap Queue Call harus mengidentifikasi tepat satu Loket tujuan yang memiliki otorisasi aktif untuk Service Point milik Queue Entry.
- **BR-AQO-019** — Satu Queue Entry tidak boleh memiliki lebih dari satu Queue Call yang Outstanding pada waktu yang sama.
- **BR-AQO-020** — Satu Loket tidak boleh memiliki lebih dari satu Queue Entry yang Outstanding atau In Service kecuali policy operasional yang disetujui secara eksplisit mengizinkan pelayanan paralel.
- **BR-AQO-021** — Recall harus membuat Call Attempt lain untuk Queue Entry yang sama dan tidak boleh mengalokasikan Queue Number lain.
- **BR-AQO-022** — Queue Call atau Call Attempt tidak boleh dengan sendirinya membentuk ServedAt atau membuktikan bahwa Registration Assistance telah dimulai.
- **BR-AQO-023** — Waiting Queue Entry hanya dapat masuk ke In Service ketika Admission Officer mengakui bahwa Registration Assistance telah dimulai di Loket tujuan.
- **BR-AQO-024** — In Service Queue Entry hanya dapat menjadi Done ketika Registration Assistance mencapai outcome yang accountable.
- **BR-AQO-025** — Registration outcome tetap authoritative di Admisi Rajal; penyelesaian Queue Entry tidak boleh secara mandiri membentuk Outpatient Registration.

### 7.5 Pengecualian dan kebenaran historis

- **BR-AQO-026** — Kesimpulan No-Show harus mempertahankan seluruh Call Attempt sebelumnya dan tidak boleh direpresentasikan sebagai Registration Assistance yang selesai; policy yang berlaku harus menentukan apakah Queue Entry tetap Waiting atau menjadi Withdrawn.
- **BR-AQO-027** — Pengalihan kebutuhan yang belum terselesaikan ke Service Point lain harus membuat Waiting Queue Entry asli menjadi Withdrawn, mempertahankan hubungan antara keikutsertaan antrean asli dan pengganti, serta tidak boleh mengganti label Queue Entry asli secara diam-diam.
- **BR-AQO-028** — Queue Display harus menampilkan Queue Call truth yang tercatat dan tidak boleh memiliki keputusan pemilihan atau lifecycle Queue Entry.
- **BR-AQO-029** — Waktu tunggu dan pelayanan antrean harus diturunkan dari milestone Queue Entry dan tidak boleh disimpulkan hanya dari Queue Call.
- **BR-AQO-030** — Admission Queue Operations tidak boleh menyatakan kehadiran fisik Patient sebelum interaksi pelayanan yang accountable mengakuinya.

## 8. State Machines & Lifecycles

### 8.1 Lifecycle ketersediaan Service Point, Loket, dan Kiosk

```text
Active
  → Retired
```

Retired berarti tidak tersedia untuk penggunaan operasional baru. Hubungan historis dan Queue Label tetap valid.

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

`Acknowledged` berarti kewajiban pemanggilan berakhir karena pelayanan diakui telah dimulai. `No-Show` berarti policy no-show yang berlaku selesai tanpa mulai pelayanan. `Withdrawn` berarti panggilan berakhir tanpa menyatakan pelayanan atau no-show. Recall menambahkan Call Attempt lain selama Queue Call tetap Outstanding.

### 8.4 Lifecycle identifikasi Queue Entry

```text
Anonymous Queue Entry
  → Identified Queue Entry
```

Penerbitan antrean dan Queue Call tidak menyebabkan transisi ini. Transisi hanya terjadi melalui asosiasi accountable dengan Patient Tracker existing atau Patient Tracker baru yang dibentuk oleh aktivitas sumber pemilik.

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
| Admission Queue Entry Recalled | Call Attempt lain dilakukan untuk Queue Entry dan Loket yang sama. |
| Admission Queue Call Acknowledged | Panggilan Outstanding berakhir karena Registration Assistance diakui telah dimulai. |
| Admission Queue Entry Marked No-Show | Panggilan Outstanding berakhir tanpa Patient atau Visitor hadir berdasarkan policy yang berlaku. |
| Admission Queue Entry Identified | Anonymous Admission Queue Entry diasosiasikan dengan satu Patient Tracker. |
| Admission Queue Service Started | Registration Assistance untuk Waiting Queue Entry dimulai di satu Loket. |
| Admission Queue Service Completed | Registration Assistance untuk In Service Queue Entry mencapai outcome yang accountable. |
| Admission Queue Entry Withdrawn | Waiting Queue Entry berakhir tanpa mulai pelayanan. |
| Admission Queue Entry Transferred | Kebutuhan antrean yang belum terselesaikan dialihkan ke Service Point lain dengan riwayat yang dipertahankan. |

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
  → Admission Officer memilih Service Point yang diotorisasi
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
  → Call Attempt lain dipertahankan ketika Recall
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
