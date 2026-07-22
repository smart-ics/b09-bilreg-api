# Domain Patient Tracker

**Status artefak:** Spesifikasi bisnis kanonis

**Bounded context:** Patient Tracker

**Cakupan versi:** V1 pragmatis

**Sumber kanonis bahasa Inggris:** [TRACKER-DOMAIN.md](./TRACKER-DOMAIN.md)

**Spesifikasi fitur antrean admisi:** [Domain Operasi Antrean Admisi](./TRACKER-ADMISSION-QUEUE-DOMAIN-ID.md)

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan

Patient Tracker menjaga kesinambungan satu Patient Journey yang logis melintasi booking, registrasi, konsultasi, apotek, dan Service Point lain yang memberikan bukti operasional.

Patient Tracker mencatat hal-hal yang dapat dibuktikan organisasi melalui interaksi bisnis. Patient Tracker tidak mengamati Patient secara terus-menerus dan tidak menyatakan posisi fisik Patient di antara interaksi tersebut.

### 1.2 Nilai bisnis

Patient Tracker menyediakan:

- satu `TrackerId` yang stabil untuk satu Patient Journey yang logis;
- satu timeline kumulatif berisi bukti operasional yang diberikan oleh aktivitas bisnis lain;
- kesinambungan antara pengambilan antrean anonim dan Patient Journey yang telah teridentifikasi;
- antrean Service Point dengan milestone menunggu, mulai dilayani, dan selesai yang eksplisit;
- Journey Candidate yang dapat dipilih operator ketika bukti demografis tidak unik; dan
- interpretasi waktu tunggu yang tidak keliru menganggap waktu booking atau pergerakan fisik yang tidak teramati sebagai waktu tunggu pelayanan.

### 1.3 Cakupan

V1 memiliki lima kapabilitas bisnis:

1. Logical Patient Journey Tracking.
2. Operational Evidence Timeline.
3. Service Point Queue Coordination.
4. Journey Candidate Resolution.
5. Operational Time Interpretation.

Fondasi journey V1 memiliki dua aggregate:

1. `Patient Tracker Aggregate`.
2. `Queue Session Aggregate`.

Fitur Admission Queue Operations mengelaborasi perilaku Queue Session dan mendefinisikan identitas bisnis pendukung yang stabil untuk Service Point, Loket, dan Kiosk dalam spesifikasi domain fiturnya.

### 1.4 Batas bisnis

Patient Tracker memiliki kepemilikan atas identitas logical journey, Tracking Period, timeline bukti kumulatif, Queue Session, Queue Entry, dan milestone pelayanan antrean.

Patient Tracker tidak memiliki kepemilikan atas:

- identitas master Patient;
- Booking, Registration, Medical Chart, resep, transaksi penjualan obat, atau transaksi sumber lainnya;
- pelayanan klinis atau dispensing apotek;
- operational truth yang terkandung dalam transaksi sumber yang direferensikan;
- lokasi fisik atau waktu perjalanan Patient;
- GPS, beacon, wearable, atau pelacakan fisik kontinu lainnya;
- katalog canonical untuk deskripsi Tracker Event;
- riwayat audit kepatuhan; atau
- event sourcing maupun kerangka event generik lintas context.

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| Patient Journey | Perjalanan Pasien | Satu kesinambungan logis dari interaksi operasional Patient untuk kunjungan yang direncanakan atau benar-benar terjadi. |
| Patient Tracker | Pelacak Perjalanan Pasien | Catatan bisnis authoritative yang mengidentifikasi satu Patient Journey dan memiliki seluruh Tracker Event kumulatifnya. |
| TrackerId | Identitas Pelacak | Identitas logis yang stabil untuk satu Patient Journey. TrackerId bukan token fisik atau perangkat pelacak. |
| Person Identity Snapshot | Rekaman Identitas Orang | Nama dan tanggal lahir Patient yang dipertahankan untuk membantu mengenali journey; bukan catatan master Patient. |
| StartPeriod | Periode Awal | Tanggal bukti pertama yang dicatat untuk suatu Patient Journey. |
| LastPeriod | Periode Akhir | Batas tanggal atas inklusif untuk menemukan Patient Journey. Untuk booking, nilainya dimulai dari VisitDate dan kemudian dapat diperpanjang sampai tanggal bukti yang lebih akhir. |
| Tracking Period | Rentang Pelacakan | Interval tanggal inklusif dari StartPeriod sampai LastPeriod yang digunakan untuk membatasi Journey Candidate. |
| VisitDate | Tanggal Kunjungan | Tanggal yang direncanakan bagi Patient untuk menerima pelayanan. Untuk journey yang dibuat dari booking, tanggal ini menetapkan LastPeriod awal. |
| Tracker Event | Peristiwa Pelacakan | Satu butir bukti operasional append-only yang diberikan kepada Patient Tracker oleh aktivitas bisnis sumber. Bukan pengamatan lokasi fisik. |
| Event Description | Deskripsi Peristiwa | Teks bebas yang membantu manusia memahami Tracker Event. Deskripsi ini hanya untuk menampilkan bukti dan bukan kunci pencarian atau pembeda perilaku pada V1. |
| Evidence Reference | Referensi Bukti | Referensi stabil menuju bukti operasional terkuat yang tersedia ketika Tracker Event dicatat. Direpresentasikan oleh ReffId. |
| Source Transaction Reference | Referensi Transaksi Sumber | Evidence Reference yang dihasilkan aktivitas bisnis utama, seperti transaksi Booking, Registration, Medical Chart, atau penjualan obat. |
| Queue Evidence Reference | Referensi Bukti Antrean | Gabungan identitas Queue Session dan Queue Number yang digunakan sebagai Evidence Reference ketika transaksi sumber utama belum tersedia. |
| OccurredAt | Waktu Kejadian | Waktu bisnis saat interaksi yang dibuktikan terjadi. Waktu ini dapat berbeda dari waktu kemudian ketika bukti dikaitkan dengan Patient Tracker. |
| Journey Candidate | Kandidat Perjalanan | Patient Tracker yang dikembalikan karena Person Identity Snapshot dan Tracking Period-nya sesuai dengan bukti pencarian yang tersedia. |
| Journey Resolution | Resolusi Perjalanan | Keputusan manusia yang bertanggung jawab untuk mengaitkan Queue Entry dengan Patient Tracker existing yang sesuai atau, ketika aktivitas sumber pemilik membentuk journey baru, dengan Patient Tracker yang baru dibentuk tersebut. |
| Queue Session | Sesi Antrean | Satu antrean dengan batas waktu untuk satu Service Point pada satu Session Date. |
| Service Point | Titik Layanan | Tempat atau tanggung jawab operasional tempat Patient menunggu pelayanan, seperti loket admisi, dokter, atau apotek rawat jalan. |
| Queue Entry | Entri Antrean | Keikutsertaan satu Patient atau pengunjung anonim dalam satu Queue Session. |
| Queue Number | Nomor Antrean | Nomor yang unik dalam satu Queue Session. Nomor yang sama dapat digunakan dalam Queue Session lain. |
| Anonymous Queue Entry | Entri Antrean Anonim | Queue Entry yang dibuat sebelum Patient Tracker teridentifikasi. |
| Identified Queue Entry | Entri Antrean Teridentifikasi | Queue Entry yang dikaitkan dengan tepat satu Patient Tracker. |
| CreatedAt | Waktu Dibuat | Waktu ketika Queue Entry masuk atau mendapat reservasi dalam antrean. Tidak selalu berarti waktu kedatangan fisik. |
| ServedAt | Waktu Mulai Dilayani | Waktu ketika Service Point mengakui bahwa pelayanan operasional telah dimulai. |
| DoneAt | Waktu Selesai | Waktu ketika Service Point mengakui bahwa pelayanan operasional telah selesai. |
| Waiting | Menunggu | State Queue Entry setelah dibuat dan sebelum pelayanan dimulai. |
| In Service | Sedang Dilayani | State Queue Entry setelah pelayanan dimulai dan sebelum pelayanan selesai. |
| Done | Selesai | State final Queue Entry setelah pelayanan selesai. |
| Withdrawn | Dihentikan sebelum Dilayani | State final Queue Entry ketika keikutsertaan berakhir sebelum pelayanan dimulai berdasarkan feature policy yang berlaku. |
| Registration Waiting Time | Waktu Tunggu Registrasi | Interval dari CreatedAt antrean loket admisi sampai ServedAt antrean tersebut. |
| Post-Registration Consultation Waiting Time | Waktu Tunggu Konsultasi Pasca-Registrasi | Interval dari DoneAt registrasi sampai ServedAt dokter, tanpa bergantung pada waktu pembuatan Queue Entry dokter. |
| Service Duration | Durasi Pelayanan | Interval dari ServedAt sampai DoneAt untuk satu Queue Entry. |

## 3. Kapabilitas Bisnis

### 3.1 Logical Patient Journey Tracking

**Indonesia:** Pelacakan Perjalanan Pasien Logis

Patient Tracker dapat:

- membentuk satu TrackerId yang stabil sebelum RegId tersedia;
- mempertahankan TrackerId yang sama di seluruh tahap journey yang memiliki bukti;
- memelihara Person Identity Snapshot dan Tracking Period; dan
- membedakan journey yang terpisah tanpa menganggap nama dan tanggal lahir sebagai identitas unik.

### 3.2 Operational Evidence Timeline

**Indonesia:** Timeline Bukti Operasional

Patient Tracker dapat:

- menambahkan bukti yang diterima dari Booking, Registration, aktivitas klinis, apotek, antrean, dan aktivitas sumber lainnya;
- mempertahankan Evidence Reference dan OccurredAt dari sumber;
- menampilkan seluruh Tracker Event secara kumulatif dan kronologis; dan
- mempertahankan berbagai jenis Evidence Reference dalam satu journey.

Timeline melaporkan bukti. Timeline tidak mengambil alih kepemilikan atas operational truth milik aktivitas sumber.

### 3.3 Service Point Queue Coordination

**Indonesia:** Koordinasi Antrean Titik Layanan

Patient Tracker dapat:

- membentuk satu Queue Session untuk satu Service Point dan interval operasional;
- menetapkan Queue Number yang unik dalam Queue Session tersebut;
- menerima Queue Entry anonim atau teridentifikasi;
- mengaitkan Anonymous Queue Entry dengan Patient Tracker yang telah diresolusi; dan
- melacak setiap Queue Entry dari Waiting melalui In Service sampai Done, atau sampai Withdrawn ketika feature policy yang berlaku mengakhiri keikutsertaan sebelum pelayanan dimulai.

### 3.4 Journey Candidate Resolution

**Indonesia:** Resolusi Kandidat Perjalanan

Patient Tracker dapat:

- menemukan Journey Candidate berdasarkan nama, tanggal lahir, dan tanggal bisnis yang relevan;
- menampilkan bukti operasional setiap kandidat;
- mempertahankan beberapa kandidat yang valid tanpa menebak; dan
- memungkinkan operator yang bertanggung jawab memilih journey existing yang sesuai; ketika tidak ada journey yang sesuai, aktivitas sumber pemilik dapat membentuk Patient Tracker baru dari bukti authoritative miliknya.

### 3.5 Operational Time Interpretation

**Indonesia:** Interpretasi Waktu Operasional

Patient Tracker dapat membedakan:

- waktu reservasi di muka dari waktu tunggu pelayanan aktual;
- waktu pengambilan antrean anonim dari resolusi identitas yang terjadi kemudian;
- selesainya registrasi dari mulainya pelayanan dokter;
- pembuatan antrean apotek dari kedatangan fisik; dan
- interaksi sistem yang teramati dari pergerakan yang tidak teramati.

## 4. Aktor & Peran

### 4.1 Patient or Visitor

Patient atau Visitor:

- berpartisipasi dalam satu atau lebih antrean Service Point;
- memberikan bukti identitas yang tersedia ketika diminta; dan
- dapat menunjukkan Queue Number atau bukti booking.

Patient tidak perlu membawa TrackerId internal sebagai token fisik.

### 4.2 Journey Resolution Operator

Journey Resolution Operator:

- mencari Journey Candidate menggunakan bukti yang tersedia;
- meninjau bukti yang ditampilkan untuk setiap kandidat;
- memilih Patient Journey yang sesuai atau membentuk journey baru; dan
- mengaitkan journey terpilih dengan Anonymous Queue Entry.

### 4.3 Service Point Operator

Service Point Operator:

- mengoordinasikan Queue Entry untuk satu Service Point;
- mengakui waktu dimulai dan selesainya pelayanan; dan
- memberikan milestone antrean yang sesuai kepada Patient Journey.

### 4.4 Source Activity Owner

Source Activity Owner adalah peran bisnis yang bertanggung jawab atas aktivitas utama seperti Booking, Registration, konsultasi, atau penjualan apotek. Peran ini:

- menjalankan atau menyelesaikan aktivitas bisnis utama;
- memberikan waktu bisnis dan referensi sumbernya sebagai bukti operasional; dan
- tetap bertanggung jawab atas kebenaran transaksi sumber.

## 5. Domain Objects

### 5.1 Entities

#### Patient Tracker

Tujuan: merepresentasikan satu Patient Journey yang logis.

Tanggung jawab:

- mempertahankan TrackerId yang stabil;
- mempertahankan Person Identity Snapshot;
- menetapkan dan memelihara StartPeriod serta LastPeriod;
- memiliki seluruh kumpulan Tracker Event; dan
- menampilkan bukti untuk Journey Resolution oleh manusia.

#### Tracker Event

Tujuan: mempertahankan satu butir bukti operasional dalam suatu Patient Journey.

Tanggung jawab:

- mempertahankan Event Description;
- mempertahankan Evidence Reference yang diberikan aktivitas sumber;
- mempertahankan OccurredAt sebagai waktu bisnis milik sumber; dan
- tetap append-only setelah dicatat.

Tracker Event bukan event integrasi generik, entri audit kepatuhan, atau bukti lokasi fisik kontinu.

#### Queue Session

Tujuan: merepresentasikan satu antrean untuk satu Service Point pada satu Session Date dan interval operasional.

Tanggung jawab:

- mempertahankan identitas Queue Session yang stabil;
- mengidentifikasi Service Point, Session Date, Start Time, dan End Time;
- memiliki seluruh Queue Entry; dan
- menjaga keunikan Queue Number di dalam session.

#### Queue Entry

Tujuan: merepresentasikan partisipasi satu pengunjung anonim atau Patient Journey teridentifikasi dalam satu Queue Session.

Tanggung jawab:

- mempertahankan Queue Number;
- mempertahankan asosiasi opsional dengan Patient Tracker serta snapshot nama yang tersedia;
- mempertahankan milestone CreatedAt, ServedAt, dan DoneAt; dan
- bertransisi dari Waiting ke In Service lalu Done, atau dari Waiting ke Withdrawn berdasarkan feature policy yang berlaku.

### 5.2 Value Objects

#### TrackerId

Identitas logis immutable untuk satu Patient Journey. TrackerId dapat dibentuk sebelum Registration menghasilkan RegId dan tetap tidak berubah ketika transaksi sumber berikutnya dibuat.

#### Person Identity Snapshot

Nama dan tanggal lahir yang digunakan untuk mengenali journey. Snapshot yang sama dapat menghasilkan beberapa Journey Candidate dan tidak boleh diperlakukan sebagai bukti bahwa dua journey merupakan journey yang sama.

#### Tracking Period

Batas inklusif StartPeriod dan LastPeriod yang digunakan untuk membatasi Journey Candidate. Periode ini mendukung booking lintas tanggal tanpa mengubah kebenaran event pertama.

#### Evidence Reference

Referensi menuju bukti operasional terkuat yang tersedia. Referensi ini dapat mengidentifikasi transaksi sumber atau Queue Evidence Reference. Tujuannya adalah provenance, bukan korelasi Start-to-Done.

#### Queue Evidence Reference

Gabungan identitas Queue Session dan Queue Number. Referensi ini mengidentifikasi satu Queue Entry meskipun transaksi utama belum tersedia.

#### Service Point

Tanggung jawab operasional bernama yang memiliki Queue Session dan mengakui milestone pelayanan.

## 6. Aggregates

### 6.1 Patient Tracker Aggregate

**Aggregate Root:** `Patient Tracker`

**Owned entities:**

- satu atau lebih entity `Tracker Event`.

**Consistency boundary:**

Aggregate menjaga TrackerId, Person Identity Snapshot, Tracking Period, dan timeline bukti kumulatif agar saling konsisten.

Hanya Patient Tracker yang dapat:

- menambahkan Tracker Event pada journey-nya;
- menetapkan StartPeriod dari bukti pertamanya;
- menginisialisasi LastPeriod dari VisitDate yang berlaku; atau
- memperpanjang LastPeriod ketika bukti berikutnya jatuh setelah batas saat ini.

Aggregate tidak memvalidasi atau menggantikan kebenaran bisnis Booking, Registration, Medical Chart, penjualan apotek, atau Queue Entry yang direferensikan.

### 6.2 Queue Session Aggregate

**Aggregate Root:** `Queue Session`

**Owned entities:**

- nol atau lebih entity `Queue Entry`.

**Consistency boundary:**

Aggregate menjaga identitas Service Point, waktu session, keunikan Queue Number, asosiasi identitas Queue Entry, dan milestone pelayanan agar saling konsisten.

Hanya Queue Session yang dapat:

- menetapkan Queue Number;
- menambahkan Anonymous atau Identified Queue Entry;
- mengaitkan Anonymous Queue Entry dengan satu Patient Tracker yang telah diresolusi;
- memulai pelayanan bagi Waiting Queue Entry;
- menyelesaikan In Service Queue Entry; atau
- menghentikan Waiting Queue Entry berdasarkan feature policy yang berlaku.

Queue Entry mereferensikan Patient Tracker melalui TrackerId, tetapi tidak memiliki atau mengubah Patient Tracker Aggregate.

## 7. Aturan Bisnis

### 7.1 Patient Journey identity and period

- **BR-TRK-001** — Patient Tracker harus merepresentasikan tepat satu Patient Journey yang logis.
- **BR-TRK-002** — Setiap Patient Tracker harus memiliki satu TrackerId yang stabil dan satu Person Identity Snapshot.
- **BR-TRK-003** — TrackerId harus mengidentifikasi logical journey dan tidak boleh direpresentasikan sebagai pelacak fisik, lokasi Patient, atau token fisik.
- **BR-TRK-004** — Patient Tracker harus memiliki setidaknya satu Tracker Event ketika dibentuk.
- **BR-TRK-005** — StartPeriod harus sama dengan tanggal kalender Tracker Event pertama dan harus mempertahankan kebenaran bukti pertama tersebut.
- **BR-TRK-006** — Untuk Patient Tracker yang dibentuk dari Booking, LastPeriod pada awalnya harus sama dengan nilai yang lebih akhir antara StartPeriod dan VisitDate.
- **BR-TRK-007** — Untuk Patient Tracker yang dibentuk tanpa VisitDate di masa depan, LastPeriod pada awalnya harus sama dengan StartPeriod.
- **BR-TRK-008** — Pencatatan bukti berikutnya harus menetapkan LastPeriod sebagai nilai yang lebih akhir antara nilai saat ini dan tanggal OccurredAt Tracker Event; LastPeriod tidak boleh bergerak mundur.
- **BR-TRK-009** — Pembentukan RegId atau transaksi sumber lainnya tidak boleh menggantikan TrackerId.

### 7.2 Tracker evidence

- **BR-TRK-010** — Setiap Tracker Event harus mengidentifikasi satu Patient Tracker, satu Event Description, satu Evidence Reference, dan satu OccurredAt.
- **BR-TRK-011** — Tracker Event hanya boleh diberikan dari bukti yang dihasilkan aktivitas bisnis lain yang bertanggung jawab atau dari Queue Entry yang dimiliki domain ini.
- **BR-TRK-012** — Ketika transaksi sumber utama telah tersedia, identitas bisnisnya yang stabil harus digunakan sebagai Evidence Reference.
- **BR-TRK-013** — Ketika transaksi sumber utama belum tersedia, Queue Evidence Reference yang berlaku dapat digunakan sebagai Evidence Reference.
- **BR-TRK-014** — Event untuk milestone Start dan Done dalam satu pelayanan dapat menggunakan jenis Evidence Reference yang berbeda karena setiap referensi mengidentifikasi bukti terkuat yang tersedia pada milestone tersebut.
- **BR-TRK-015** — Evidence Reference harus menyatakan provenance dan tidak wajib mengorelasikan seluruh event dalam satu service lifecycle.
- **BR-TRK-016** — Event Description harus tetap berupa teks bebas untuk ditampilkan kepada manusia dan tidak boleh menentukan pencarian V1, resolusi identitas, transisi state, atau perilaku bisnis.
- **BR-TRK-017** — Tracker Event harus bersifat kumulatif dan append-only; penambahan event tidak boleh menghapus atau menggantikan bukti sebelumnya.
- **BR-TRK-018** — Timeline journey harus ditampilkan secara kronologis berdasarkan OccurredAt dengan tetap mempertahankan urutan pencatatan yang deterministik untuk timestamp yang sama.
- **BR-TRK-019** — OccurredAt harus mempertahankan waktu bisnis aktivitas sumber meskipun Tracker Event baru dikaitkan dengan journey pada waktu yang lebih kemudian.

### 7.3 Journey Candidate Resolution

- **BR-TRK-020** — Pencarian Journey Candidate harus menggunakan nama, tanggal lahir, dan tanggal bisnis relevan yang tersedia.
- **BR-TRK-021** — Patient Tracker memenuhi syarat temporal ketika tanggal bisnis relevan berada secara inklusif di antara StartPeriod dan LastPeriod.
- **BR-TRK-022** — Pencarian harus mengembalikan seluruh Journey Candidate yang cocok dan tidak boleh diam-diam memilih di antara Person Identity Snapshot yang sama.
- **BR-TRK-023** — Ketika masih terdapat beberapa kandidat, Journey Resolution Operator yang bertanggung jawab harus memilih journey yang sesuai berdasarkan bukti yang tersedia.
- **BR-TRK-024** — Kesamaan nama dan tanggal lahir tidak boleh menggabungkan Patient Tracker atau membuktikan bahwa dua journey merupakan journey yang sama.
- **BR-TRK-025** — Ketika tidak ada kandidat yang sesuai, aktivitas sumber pemilik yang bertanggung jawab dapat membentuk Patient Tracker baru dari identitas dan bukti operasional authoritative miliknya; memperoleh Queue Number saja tidak boleh membentuk Patient Tracker.

### 7.4 Queue Session and Queue Entry identity

- **BR-TRK-026** — Setiap Queue Session harus mengidentifikasi tepat satu Service Point, satu Session Date, satu Start Time, dan satu End Time.
- **BR-TRK-027** — Queue Number harus unik dalam satu Queue Session, tetapi dapat berulang pada Queue Session yang berbeda.
- **BR-TRK-028** — Setiap Queue Entry harus termasuk dalam tepat satu Queue Session dan mempertahankan tepat satu Queue Number di dalam session tersebut.
- **BR-TRK-029** — Memperoleh Queue Number dapat membuat Anonymous Queue Entry ketika Patient Journey yang sesuai belum diresolusi secara accountable; alokasi Queue Number tidak boleh dengan sendirinya membentuk Patient Tracker.
- **BR-TRK-030** — Queue Entry yang secara langsung dibuat atau direservasi oleh Booking atau aktivitas sumber upstream teridentifikasi lainnya harus mereferensikan tepat satu Patient Tracker sejak pembentukannya. Admission Queue Entry yang diterbitkan setelah Self-Registration Booking memerlukan bantuan bukan merupakan Queue Entry yang dibuat Booking untuk rule ini dan dapat tetap Anonymous sampai Admission Officer meresolusi journey dari bukti yang diberikan Patient.
- **BR-TRK-031** — Anonymous Queue Entry hanya dapat menjadi Identified setelah resolusi accountable mengaitkannya dengan satu Patient Tracker existing atau aktivitas sumber pemilik membentuk Patient Tracker baru dari bukti authoritative.
- **BR-TRK-031a** — Walk-In admission Queue Entry harus tetap Anonymous sampai Registration membentuk Patient Tracker baru atau Journey Resolution yang accountable memilih Patient Tracker existing yang sesuai.
- **BR-TRK-031b** — Admission Queue Entry milik Booking Patient tidak boleh dikaitkan secara otomatis hanya dari Booking QR yang ditunjukkan; Admission Officer harus menggunakan bukti yang diberikan Patient untuk meresolusi dan mengaitkan existing Booking Patient Tracker yang sesuai.
- **BR-TRK-032** — Satu Patient Tracker dapat berpartisipasi dalam beberapa Queue Session, tetapi satu Queue Entry hanya boleh mereferensikan paling banyak satu Patient Tracker.
- **BR-TRK-033** — CreatedAt harus mencatat waktu ketika Queue Entry dibuat atau mendapat reservasi dan tidak boleh selalu ditafsirkan sebagai kedatangan fisik.
- **BR-TRK-034** — Queue Entry yang dibuat dari booking dapat memiliki CreatedAt sebelum Session Date atau Start Time Queue Session tanpa mengubah waktu kejadian Booking.

### 7.5 Queue Entry service lifecycle

- **BR-TRK-035** — Queue Entry yang baru dibuat harus berada dalam state Waiting, dengan CreatedAt tercatat serta ServedAt dan DoneAt belum tersedia.
- **BR-TRK-036** — Hanya Waiting Queue Entry yang dapat memasuki In Service, dan masuknya ke In Service harus mencatat ServedAt.
- **BR-TRK-037** — Hanya In Service Queue Entry yang dapat menjadi Done, dan perubahan menjadi Done harus mencatat DoneAt.
- **BR-TRK-038** — ServedAt tidak boleh mendahului CreatedAt, dan DoneAt tidak boleh mendahului ServedAt.
- **BR-TRK-039** — Done Queue Entry bersifat final pada V1 dan tidak boleh kembali menjadi Waiting atau In Service.
- **BR-TRK-039a** — Feature policy yang berlaku dapat membuat Waiting Queue Entry menjadi Withdrawn ketika keikutsertaan berakhir sebelum pelayanan dimulai; Withdrawn Queue Entry bersifat final dan tidak boleh direpresentasikan sebagai pelayanan yang selesai.

### 7.6 Operational time interpretation

- **BR-TRK-040** — Registration Waiting Time harus diukur dari CreatedAt Queue Entry admisi sampai ServedAt-nya.
- **BR-TRK-041** — Registration Service Duration harus diukur dari ServedAt Queue Entry admisi sampai DoneAt-nya.
- **BR-TRK-042** — Post-Registration Consultation Waiting Time harus diukur dari DoneAt registrasi sampai ServedAt dokter dan tidak boleh menggunakan CreatedAt Queue Entry dokter yang dibuat dari booking.
- **BR-TRK-043** — Consultation Service Duration harus diukur dari ServedAt Queue Entry dokter sampai DoneAt-nya.
- **BR-TRK-044** — Pembuatan Pharmacy Queue Entry dari resep atau konsultasi yang selesai tidak boleh diperlakukan sebagai bukti bahwa Patient telah tiba secara fisik di apotek.
- **BR-TRK-045** — Untuk alur apotek rawat jalan V1, penyimpanan transaksi penjualan obat yang telah dikonfirmasi harus menetapkan ServedAt dan bukti dimulainya pelayanan apotek.
- **BR-TRK-046** — Pharmacy Service Duration harus diukur dari ServedAt apotek sampai DoneAt apotek.
- **BR-TRK-047** — Patient Tracker tidak boleh menyimpulkan posisi fisik, dimulainya perjalanan, selesainya perjalanan, atau kedatangan di ruang tunggu ketika tidak ada interaksi bisnis yang bertanggung jawab.

### 7.7 Ownership and historical truth

- **BR-TRK-048** — Aktivitas sumber yang direferensikan tetap authoritative atas isi transaksi dan outcome bisnisnya sendiri.
- **BR-TRK-049** — Patient Tracker harus mempertahankan waktu sumber dan Evidence Reference yang diberikan tanpa menulis ulang riwayat transaksi sumber.
- **BR-TRK-050** — Milestone antrean dan Tracker Event harus merepresentasikan bukti operasional dan tidak boleh menggantikan kewajiban audit kepatuhan.

## 8. State Machines & Lifecycles

### 8.1 Patient Tracker lifecycle

Patient Tracker tidak memiliki status Open, Closed, atau Completed pada V1. Lifecycle-nya berupa pertumbuhan bukti dalam Tracking Period yang berbatas.

```text
Bukti operasional pertama
          |
          v
Patient Tracker dibentuk
          |
          +----> Tracker Event ditambahkan ---+
          |                                    |
          +----> LastPeriod diperpanjang ------+
          |                                    |
          +------------------------------------+
```

Untuk journey yang dibuat dari booking:

```text
StartPeriod = tanggal Booking OccurredAt
LastPeriod  = max(StartPeriod, VisitDate)

Tracker Event berikutnya
  → LastPeriod = max(LastPeriod saat ini, tanggal OccurredAt event)
```

### 8.2 Tracker Event lifecycle

```text
Aktivitas sumber menghasilkan bukti operasional
  → Bukti dikaitkan dengan satu Patient Tracker
  → Tracker Event dicatat
  → Tracker Event tetap immutable
```

Event Description dapat bervariasi karena berupa teks bebas. Evidence Reference dan OccurredAt mempertahankan bukti sumber yang digunakan ketika event dicatat.

### 8.3 Queue Entry service lifecycle

```text
Queue Entry dibuat
        |
        v
     Waiting
        |
        | Pelayanan dimulai / ServedAt dicatat
        v
    In Service
        |
        | Pelayanan selesai / DoneAt dicatat
        v
       Done

     Waiting
        |
        | Keikutsertaan berakhir sebelum pelayanan dimulai
        v
    Withdrawn
```

| State | Makna bisnis | State berikutnya yang diperbolehkan |
|---|---|---|
| Waiting | Queue Entry telah tersedia dan pelayanan belum dimulai. | In Service atau Withdrawn berdasarkan feature policy yang berlaku |
| In Service | Service Point telah mengakui bahwa pelayanan dimulai. | Done |
| Done | Service Point telah mengakui bahwa pelayanan selesai. | Tidak ada |
| Withdrawn | Keikutsertaan antrean berakhir sebelum pelayanan dimulai. | Tidak ada |

### 8.4 Queue Entry identification lifecycle

```text
Anonymous Queue Entry
          |
          | Pengaitan accountable dengan Tracker existing,
          | atau Tracker baru yang dibentuk aktivitas sumber pemilik
          v
Identified Queue Entry
```

Physician Queue Entry yang secara langsung dibuat Booking, atau entry yang secara langsung dibuat aktivitas sumber teridentifikasi lain, dimulai sebagai Identified dan tidak melalui kondisi Anonymous. Admission Queue Entry yang diterbitkan karena Self-Registration Booking memerlukan bantuan merupakan Queue Entry terpisah dan dapat dimulai sebagai Anonymous.

## 9. Domain Events

Event pada bagian ini merupakan business fact stabil milik bounded context ini. Event tersebut berbeda dari Event Description free-text yang disimpan di dalam Tracker Event.

| Domain Event | Makna bisnis |
|---|---|
| Patient Tracker Established | Satu Patient Journey yang logis dibentuk dari bukti pertamanya. |
| Tracker Evidence Recorded | Satu butir bukti operasional ditambahkan ke Patient Tracker. |
| Tracking Period Extended | Bukti berikutnya memindahkan LastPeriod melewati batas sebelumnya. |
| Journey Candidate Selected | Operator yang bertanggung jawab memilih satu Patient Tracker berdasarkan bukti yang tersedia. |
| Queue Session Established | Satu antrean dibentuk untuk satu Service Point dan interval operasional. |
| Queue Entry Created | Anonymous atau Identified Queue Entry menerima Queue Number. |
| Queue Entry Identified | Anonymous Queue Entry dikaitkan dengan satu Patient Tracker yang telah diresolusi. |
| Queue Service Started | Waiting Queue Entry memasuki In Service dan memperoleh ServedAt. |
| Queue Service Completed | In Service Queue Entry menjadi Done dan memperoleh DoneAt. |
| Queue Entry Withdrawn | Waiting Queue Entry berakhir sebelum pelayanan dimulai berdasarkan feature policy yang berlaku. |
| Booking Queue Number Assigned | Queue Number yang direservasi dalam Queue Session dokter dikaitkan dengan Booking. |

## 10. Workflow Bisnis

### 10.1 Establish a journey from Booking

```text
Booking dibentuk dengan VisitDate
  → Patient Tracker dibentuk dengan bukti Booking
  → StartPeriod ditetapkan dari tanggal Booking OccurredAt
  → LastPeriod ditetapkan menjadi max(StartPeriod, VisitDate)
  → Queue Session dokter ditemukan atau dibentuk untuk VisitDate
  → Identified Queue Entry dibuat dengan Booking OccurredAt sebagai CreatedAt
  → Queue Number dikaitkan dengan Booking
```

CreatedAt Queue Entry dokter mempertahankan kebenaran booking. CreatedAt tersebut bukan awal Post-Registration Consultation Waiting Time.

### 10.2 Take an anonymous admission queue number

```text
Patient meminta nomor untuk Service Point admisi
  → Queue Session admisi ditemukan atau dibentuk
  → Anonymous Queue Entry dibuat
  → Queue Number dan CreatedAt dicatat
```

Flow ini berlaku untuk Walk-In Patient maupun Booking Patient yang Self-Registration-nya memerlukan bantuan. Menunjukkan atau memindai bukti Booking tidak otomatis mengidentifikasi admission Queue Entry dan tidak membentuk Patient Tracker lain. Tidak ada bukti Patient Tracker yang ditambahkan sampai Queue Entry secara accountable dikaitkan dengan journey yang telah diresolusi.

### 10.3 Resolve the journey and perform Registration

```text
Queue Number admisi dipanggil
  → Admission Officer meminta identitas dan bukti kunjungan yang tersedia kepada Patient
  → Journey Candidate ditemukan dari bukti yang tersedia dan tanggal relevan
  → Jalur Booking: Operator memilih existing Booking Tracker yang sesuai
  → Jalur Walk-In dengan journey existing yang sesuai: Operator memilih Tracker tersebut
  → Jalur Walk-In tanpa journey yang sesuai: Registration membentuk Tracker baru dari bukti Registration
  → Anonymous Queue Entry menjadi Identified dengan Tracker yang dipilih atau baru dibentuk
  → Pelayanan antrean dimulai dan ServedAt dicatat
  → Bukti check-in menggunakan Queue Evidence Reference dan CreatedAt antrean
  → Bukti mulai registrasi menggunakan Queue Evidence Reference dan ServedAt antrean
  → Registration selesai dan DoneAt dicatat
  → Bukti selesai registrasi menggunakan referensi transaksi Registration
```

Transaksi Registration tetap authoritative atas outcome registrasi. Untuk Walk-In tanpa journey existing yang sesuai, memperoleh atau memanggil Queue Number tidak membentuk Tracker; Tracker dibentuk ketika Registration menyediakan bukti sumber authoritative, lalu Queue Entry existing dikaitkan dengannya. OccurredAt pada bukti antrean yang ditambahkan kemudian tetap mempertahankan waktu milestone antrean yang lebih awal.

### 10.4 Perform physician consultation

```text
Registration selesai
  → Patient tetap direpresentasikan oleh Queue Entry dokter yang dibuat dari booking
  → Pelayanan dokter dimulai dan ServedAt dicatat
  → Bukti mulai konsultasi menggunakan Queue Evidence Reference
  → Konsultasi selesai dan DoneAt dicatat
  → Bukti selesai konsultasi menggunakan referensi transaksi Medical Chart
```

Post-Registration Consultation Waiting Time dihitung dari DoneAt registrasi sampai ServedAt dokter.

### 10.5 Establish downstream pharmacy work

```text
Konsultasi selesai dan pekerjaan resep dihasilkan
  → Queue Session apotek ditemukan atau dibentuk
  → Identified Pharmacy Queue Entry dibuat untuk TrackerId yang sama
  → CreatedAt apotek mencatat pembuatan antrean
  → Tidak ada event kedatangan fisik di apotek yang disimpulkan
```

### 10.6 Perform and complete pharmacy service

```text
Apotek mengonfirmasi pembelian obat Patient
  → Transaksi penjualan obat dibentuk
  → Pharmacy Queue Entry memasuki In Service
  → ServedAt apotek dan bukti mulai pelayanan menggunakan waktu konfirmasi
  → Dispensing dan pembayaran Patient dapat berjalan secara independen
  → Obat diserahkan kepada Patient
  → Pharmacy Queue Entry menjadi Done
  → Bukti selesai pelayanan apotek dicatat
```

Evidence Reference untuk mulai pelayanan dapat mengidentifikasi transaksi penjualan obat, sedangkan penyelesaian dapat menggunakan Queue Evidence Reference ketika tidak ada transaksi penyelesaian yang lebih kuat.

### 10.7 Resolve multiple Journey Candidates

```text
Nama, tanggal lahir, dan tanggal relevan diberikan
  → Seluruh Journey Candidate yang memenuhi syarat temporal ditampilkan bersama buktinya
  → Tidak ada penggabungan atau pemilihan demografis otomatis
  → Operator yang bertanggung jawab memilih satu journey
     atau membentuk Patient Tracker baru
  → TrackerId terpilih dikaitkan dengan Queue Entry saat ini
```

### 10.8 Interpret an outpatient journey timeline

```text
Waktu Booking
  → reservasi antrean dokter di muka

CreatedAt admisi → ServedAt admisi
  = Registration Waiting Time

ServedAt admisi → DoneAt admisi
  = Registration Service Duration

DoneAt admisi → ServedAt dokter
  = Post-Registration Consultation Waiting Time

ServedAt dokter → DoneAt dokter
  = Consultation Service Duration

ServedAt apotek → DoneAt apotek
  = Pharmacy Service Duration
```

Tidak ada interval dalam workflow ini yang menjadi bukti pergerakan fisik kecuali interaksi bisnis lain yang bertanggung jawab secara eksplisit mencatatnya.
