# Domain CPOE

**Status artefak:** Spesifikasi bisnis kanonis  
**Bounded context:** Computerized Physician Order Entry (CPOE)  
**Cakupan versi:** V1 pragmatis  
**Sumber kanonis bahasa Inggris:** [CPOE-DOMAIN.md](./CPOE-DOMAIN.md)

## 1. Gambaran Umum Bisnis

### 1.1 Tujuan

CPOE menangkap, merutekan, dan melacak maksud seorang klinisi sebagai `Clinical Order`. CPOE menjaga maksud tersebut beserta riwayat bisnis lengkapnya sejak dibuat hingga mencapai outcome akhir.

CPOE tidak melaksanakan pekerjaan klinis. `Destination` melaksanakan pekerjaan yang diminta dan menyediakan `Fulfilment Evidence`; CPOE mencatat outcome yang dilaporkan.

### 1.2 Nilai bisnis

CPOE menyediakan satu catatan otoritatif mengenai:

- pekerjaan klinis yang diminta;
- pihak yang meminta dan `Patient` yang menjadi subjeknya;
- tujuan perutean setiap pelaksanaan yang direncanakan;
- apakah setiap pelaksanaan masih `Active`, telah `Completed`, `Not Performed`, atau `Cancelled`;
- kemajuan sebuah scheduled order; dan
- apa yang berubah, alasan perubahan, serta pihak yang melakukan perubahan.

### 1.3 Cakupan

V1 memiliki tepat empat kapabilitas bisnis utama:

1. Clinical Order Management.
2. Order Routing.
3. Order Fulfilment Tracking.
4. Scheduled Order Management.

V1 juga memiliki satu kapabilitas pendukung: Active Order Reconciliation untuk `Inter Ward Transfer`.

### 1.4 Batasan bisnis

CPOE memiliki maksud klinisi, penetapan destination, status order dan occurrence, completion progress, keputusan rekonsiliasi, serta riwayat order lengkap.

CPOE tidak memiliki:

- pelaksanaan pekerjaan klinis yang diminta;
- hasil klinis atau result review;
- detail pelaksanaan khusus destination;
- workflow draft, acceptance, atau clarification;
- workflow generic fulfilment atau workflow escalation;
- aturan routing yang kompleks;
- discharge reconciliation;
- recurrence tingkat lanjut atau tak terbatas;
- protocol atau order template;
- pembuatan order berbantuan AI; atau
- model otorisasi.

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| Clinical Order | Instruksi Klinis | Pernyataan otoritatif atas maksud seorang klinisi agar pekerjaan klinis tertentu dilakukan untuk satu Patient. |
| Clinical Order Identifier | Identitas Instruksi Klinis | Identitas bisnis yang stabil untuk sebuah Clinical Order sepanjang lifecycle-nya. |
| Ordering Clinician | Klinisi Pemesan | Klinisi yang bertanggung jawab membuat Clinical Order serta melakukan modification atau cancellation yang diizinkan. |
| Patient | Pasien | Orang yang menjadi subjek pekerjaan klinis yang dipesan. |
| RegId | Identitas Registrasi | Identitas registrasi yang stabil untuk episode perawatan Patient. Inter Ward Transfer mempertahankan RegId yang sama. |
| Order Type | Jenis Order | Klasifikasi bisnis dari pekerjaan klinis yang diminta dan digunakan untuk menentukan Destination yang sesuai. |
| Order Specification | Spesifikasi Order | Pekerjaan klinis yang diminta dan instruksi yang diperlukan agar Destination memahami maksud Ordering Clinician. |
| Destination | Unit Tujuan | Unit operasional yang bertanggung jawab melaksanakan Order Occurrence yang telah dirutekan, misalnya ward, laboratorium, unit radiologi, atau farmasi. |
| Order Occurrence | Kejadian Pelaksanaan Order | Satu pelaksanaan Clinical Order yang direncanakan secara eksplisit, dengan identitas, waktu pelaksanaan terencana, Destination, status, dan Fulfilment Evidence opsionalnya sendiri. |
| Single-Occurrence Order | Order Satu Kejadian | Clinical Order yang berisi tepat satu Order Occurrence. |
| Scheduled Order | Order Terjadwal | Clinical Order yang berisi lebih dari satu Order Occurrence yang terbatas dan direncanakan secara eksplisit. |
| Planned Execution Time | Waktu Pelaksanaan Terencana | Waktu bisnis ketika sebuah Order Occurrence dimaksudkan untuk dilakukan. |
| Active | Aktif | Status belum final yang menandakan Clinical Order atau Order Occurrence masih tertunda. |
| Completed | Selesai Dilaksanakan | Status final yang menandakan occurrence telah dilakukan, atau order ditutup melalui fulfilment dengan setidaknya satu occurrence Completed. |
| Not Performed | Tidak Dilaksanakan | Status final yang menandakan occurrence tidak dilakukan, atau order ditutup melalui fulfilment tanpa occurrence yang Completed. |
| Cancelled | Dibatalkan | Status final yang menandakan Ordering Clinician mengakhiri maksud yang tersisa sebelum semua occurrence tertunda dipenuhi. |
| Fulfilment Evidence | Bukti Pemenuhan | Pengesahan bisnis dari Destination bahwa sebuah Order Occurrence telah Completed atau Not Performed, mencakup outcome, waktu berlaku, Destination yang bertanggung jawab, referensi bukti, serta alasan bila tidak dilakukan. |
| Completion Progress | Kemajuan Penyelesaian | Jumlah occurrence total, Active, Completed, Not Performed, dan Cancelled untuk sebuah Clinical Order. |
| Destination Worklist | Daftar Kerja Tujuan | Kumpulan Order Occurrence Active yang saat ini ditetapkan pada satu Destination, masing-masing dengan konteks order yang cukup untuk memahami pekerjaan yang diminta. |
| Order History | Riwayat Order | Catatan bisnis lengkap, kronologis, dan append-only mengenai pembuatan, modification, keputusan routing, outcome fulfilment, keputusan rekonsiliasi, serta cancellation. |
| Inter Ward Transfer | Transfer Antar-Ward | Perpindahan dari satu ward ke ward lain dalam registrasi Patient yang berlanjut dan diidentifikasi oleh RegId. |
| Active Order Reconciliation | Rekonsiliasi Order Aktif | Peninjauan berbantuan atas Order Occurrence Active setelah Inter Ward Transfer untuk menentukan apakah Destination-nya masih sesuai. |
| Transfer Reconciliation | Rekonsiliasi Transfer | Catatan bisnis yang mengoordinasikan satu Active Order Reconciliation untuk satu transfer Patient. |
| Affected Occurrence Review | Peninjauan Kejadian Terdampak | Assessment dan keputusan manusia untuk satu Order Occurrence Active yang Destination-nya mungkin terdampak Inter Ward Transfer. |
| Reconciliation Reviewer | Peninjau Rekonsiliasi | Klinisi atau wakil operasional yang bertanggung jawab memutuskan apakah Destination terdampak dipertahankan atau diubah. |

## 3. Kapabilitas Bisnis

### 3.1 Primary: Clinical Order Management

**Indonesia:** Manajemen Instruksi Klinis

CPOE dapat:

- membuat Clinical Order yang langsung menjadi Active;
- mengubah maksud klinisi hanya selama pekerjaan klinis yang terdampak belum dieksekusi;
- membatalkan Clinical Order Active beserta occurrence Active yang tersisa; dan
- menjaga Order History yang lengkap tanpa menggantikan fakta bisnis sebelumnya.

Tidak ada status Draft pada V1.

### 3.2 Primary: Order Routing

**Indonesia:** Perutean Order

CPOE dapat:

- menentukan Destination berdasarkan Order Type, Order Specification, RegId, dan ward saat ini bila relevan;
- menetapkan tepat satu Destination untuk setiap Order Occurrence Active;
- memperbarui Destination yang memenuhi syarat melalui keputusan bisnis eksplisit; dan
- menyediakan Destination Worklist yang dikelompokkan berdasarkan Destination.

Routing menetapkan tanggung jawab pelaksanaan. Routing tidak berarti Destination telah menerima order atau melaksanakan pekerjaan.

### 3.3 Primary: Order Fulfilment Tracking

**Indonesia:** Pelacakan Pemenuhan Order

CPOE dapat:

- mencatat Fulfilment Evidence yang disediakan oleh Destination;
- memfinalkan Order Occurrence sebagai Completed atau Not Performed;
- menurunkan status Clinical Order dari outcome occurrence dan cancellation eksplisit; dan
- melaporkan Completion Progress tanpa memiliki detail pelaksanaan atau hasil klinis.

### 3.4 Primary: Scheduled Order Management

**Indonesia:** Manajemen Order Terjadwal

CPOE dapat merepresentasikan satu Clinical Order sebagai kumpulan terbatas dari Order Occurrence yang dilacak secara independen.

Penjadwalan V1 sengaja dibuat eksplisit:

- setiap occurrence direncanakan sebagai bagian dari daftar terbatas;
- setiap occurrence memiliki Planned Execution Time, Destination, dan statusnya sendiri;
- occurrence tidak memiliki dependensi satu sama lain; dan
- order final tidak dapat memperbarui dirinya atau menghasilkan occurrence tambahan.

V1 tidak menggunakan recurrence rule, cron expression, recurrence tak terbatas, atau scheduling engine tingkat lanjut.

### 3.5 Supporting: Active Order Reconciliation

**Indonesia:** Rekonsiliasi Order Aktif

Setelah Inter Ward Transfer, CPOE dapat:

- mengevaluasi Order Occurrence Active milik Patient;
- mengidentifikasi occurrence yang Destination-nya mungkin bergantung pada ward sebelumnya;
- menyajikan setiap occurrence terdampak untuk ditinjau;
- mencatat keputusan untuk mempertahankan atau mengubah Destination; dan
- menjaga keputusan tersebut dalam Order History terkait.

Rekonsiliasi bersifat berbantuan. Transfer ward tidak pernah mengubah Destination secara otomatis.

## 4. Aktor & Peran

### 4.1 Ordering Clinician

Ordering Clinician:

- menyatakan maksud klinis dengan membuat Clinical Order;
- menyediakan Patient, RegId, Order Type, Order Specification, dan rencana occurrence terbatas;
- mengubah maksud yang memenuhi syarat sebelum eksekusi;
- membatalkan Clinical Order yang memenuhi syarat dengan alasan bisnis; dan
- tetap dapat diidentifikasi dalam Order History.

### 4.2 Destination Fulfilment Representative

Destination Fulfilment Representative adalah klinisi atau wakil operasional yang bertindak untuk Destination. Peran ini:

- menggunakan Destination Worklist untuk memahami occurrence yang masih tertunda;
- menyediakan Fulfilment Evidence untuk occurrence Completed atau Not Performed; dan
- tidak mendefinisikan ulang maksud klinis awal.

### 4.3 Reconciliation Reviewer

Reconciliation Reviewer:

- menilai Affected Occurrence Review setelah Inter Ward Transfer;
- memutuskan apakah setiap Destination terdampak dipertahankan atau diubah;
- menyediakan alasan keputusan tersebut; dan
- tidak mengubah Destination hanya karena transfer terjadi.

## 5. Domain Objects

### 5.1 Entities

#### Clinical Order

Tujuan: merepresentasikan satu maksud klinisi untuk satu Patient.

Tanggung jawab:

- mempertahankan Clinical Order Identifier yang stabil;
- mengidentifikasi Patient, RegId, Ordering Clinician, Order Type, dan Order Specification;
- memiliki satu atau lebih Order Occurrence;
- mengatur modification, cancellation, status, dan Completion Progress; dan
- memiliki Order History lengkap.

#### Order Occurrence

Tujuan: merepresentasikan satu pelaksanaan Clinical Order yang direncanakan.

Tanggung jawab:

- mempertahankan identitas yang unik di dalam Clinical Order;
- mempertahankan Planned Execution Time dan Destination;
- bertransisi secara independen dari Active ke satu status final; dan
- mempertahankan paling banyak satu catatan Fulfilment Evidence.

Order Occurrence tidak memiliki makna bisnis di luar Clinical Order induknya.

#### Order History Entry

Tujuan: menjaga satu fakta bisnis material dalam lifecycle sebuah Clinical Order.

Setiap entri mengidentifikasi apa yang terjadi, kapan terjadi, pihak yang bertanggung jawab, alasan bila diperlukan, serta nilai bisnis sebelum dan sesudah yang relevan. Sebuah entri tidak pernah direvisi atau dihapus.

#### Transfer Reconciliation

Tujuan: mengoordinasikan peninjauan berbantuan setelah satu Inter Ward Transfer untuk satu Patient.

Tanggung jawab:

- mengidentifikasi ward sebelumnya, ward baru, dan waktu efektif transfer;
- mempertahankan kumpulan Affected Occurrence Review; dan
- selesai hanya ketika setiap occurrence terdampak memiliki keputusan yang tercatat.

#### Affected Occurrence Review

Tujuan: merepresentasikan assessment rekonsiliasi atas satu Order Occurrence Active.

Tanggung jawab:

- mengidentifikasi Clinical Order dan Order Occurrence yang ditinjau;
- menjelaskan alasan occurrence mungkin terdampak;
- mencatat keputusan untuk mempertahankan atau mengubah Destination; dan
- mengidentifikasi Reconciliation Reviewer, waktu keputusan, dan alasan.

### 5.2 Value Objects

#### RegId

Identitas registrasi yang stabil untuk episode perawatan Patient. RegId tidak berubah selama Inter Ward Transfer; ward saat ini dan Destination merupakan fakta routing yang terpisah.

#### Order Specification

Pekerjaan klinis yang diminta dan instruksi klinis yang diperlukan untuk menyatakan maksud Ordering Clinician. Order Specification tidak mencakup catatan eksekusi atau hasil klinis.

#### Destination

Unit operasional teridentifikasi yang bertanggung jawab atas pelaksanaan sebuah Order Occurrence.

#### Fulfilment Evidence

Pengesahan yang tidak dapat diubah dari Destination yang bertanggung jawab. Fulfilment Evidence hanya memuat informasi yang diperlukan untuk mendukung outcome Completed atau Not Performed. Fulfilment Evidence bukan hasil klinis.

#### Completion Progress

Ringkasan turunan atas jumlah occurrence. Totalnya tetap setelah occurrence mana pun meninggalkan status Active.

## 6. Aggregates

### 6.1 Clinical Order Aggregate

**Aggregate Root:** `Clinical Order`

**Owned entities:**

- satu atau lebih entity `Order Occurrence`;
- nol atau lebih entity `Order History Entry`.

**Consistency boundary:**

Aggregate ini menjaga maksud klinisi, outcome occurrence, status keseluruhan, Completion Progress, penetapan routing, dan Order History tetap konsisten satu sama lain.

Hanya Clinical Order yang dapat:

- mengubah Order Specification atau rencana occurrence;
- mengubah Destination atau Planned Execution Time occurrence Active;
- memfinalkan occurrence berdasarkan Fulfilment Evidence;
- membatalkan occurrence Active yang tersisa; atau
- menurunkan status keseluruhan dan Completion Progress-nya.

### 6.2 Transfer Reconciliation Aggregate

**Aggregate Root:** `Transfer Reconciliation`

**Owned entities:**

- nol atau lebih entity `Affected Occurrence Review`.

**Consistency boundary:**

Aggregate ini memastikan satu transfer Patient dinilai satu kali sebagai rekonsiliasi yang koheren dan setiap occurrence terdampak memperoleh tepat satu keputusan eksplisit.

Transfer Reconciliation dapat merekomendasikan dan mencatat keputusan Destination. Transfer Reconciliation tidak dapat menetapkan ulang Order Occurrence secara langsung. Perubahan yang dikonfirmasi harus diterima oleh Clinical Order terkait berdasarkan aturannya sendiri dan harus menjadi bagian dari Order History-nya.

## 7. Aturan Bisnis

### 7.1 Identitas dan pembuatan Clinical Order

- **BR-CPOE-001** — Sebuah Clinical Order wajib merepresentasikan tepat satu maksud Ordering Clinician untuk tepat satu Patient.
- **BR-CPOE-002** — Sebuah Clinical Order wajib memiliki Clinical Order Identifier yang stabil, RegId, Order Type, Order Specification, dan setidaknya satu Order Occurrence.
- **BR-CPOE-003** — Clinical Order yang valid wajib langsung menjadi Active saat dibuat; V1 tidak boleh menyimpan Clinical Order Draft.
- **BR-CPOE-004** — Setiap Order Occurrence wajib memiliki identitas yang unik dalam Clinical Order-nya dan satu Planned Execution Time yang eksplisit.
- **BR-CPOE-005** — Clinical Order dengan satu occurrence adalah Single-Occurrence Order; Clinical Order dengan lebih dari satu occurrence adalah Scheduled Order.

### 7.2 Routing dan worklist

- **BR-CPOE-006** — Setiap Order Occurrence Active wajib memiliki tepat satu Destination.
- **BR-CPOE-007** — Sebuah Destination wajib sesuai dengan Order Type, Order Specification, RegId, dan ward saat ini yang diketahui ketika keputusan routing dibuat.
- **BR-CPOE-008** — Sebuah Destination Worklist hanya boleh memuat Order Occurrence Active yang ditetapkan ke Destination tersebut.
- **BR-CPOE-009** — Routing tidak boleh menyiratkan bahwa Destination telah menerima, memulai, atau melakukan pekerjaan yang diminta.
- **BR-CPOE-010** — Perubahan Destination wajib mengidentifikasi pihak yang bertanggung jawab, waktu keputusan, alasan, Destination sebelumnya, dan Destination baru dalam Order History.

### 7.3 Modification dan cancellation

- **BR-CPOE-011** — Patient dan Clinical Order Identifier tidak boleh berubah setelah pembuatan.
- **BR-CPOE-012** — Order Type atau Order Specification hanya boleh diubah selama setiap Order Occurrence masih Active.
- **BR-CPOE-013** — Rencana occurrence terbatas hanya boleh diubah selama setiap Order Occurrence masih Active.
- **BR-CPOE-014** — Planned Execution Time atau Destination pada occurrence tertentu hanya boleh diubah selama occurrence tersebut masih Active.
- **BR-CPOE-015** — Setiap modification wajib mencatat pihak yang bertanggung jawab, waktu modification, alasan, dan nilai sebelum-serta-sesudah yang relevan dalam Order History.
- **BR-CPOE-016** — Hanya Clinical Order Active yang boleh dibatalkan.
- **BR-CPOE-017** — Membatalkan Clinical Order wajib mengubah setiap occurrence Active yang tersisa menjadi Cancelled dan tidak boleh mengubah occurrence yang sudah Completed atau Not Performed.
- **BR-CPOE-018** — Cancellation wajib mencatat pihak yang bertanggung jawab, waktu cancellation, dan alasan dalam Order History.
- **BR-CPOE-019** — Clinical Order atau Order Occurrence final tidak boleh kembali menjadi Active.

### 7.4 Fulfilment dan kemajuan

- **BR-CPOE-020** — Order Occurrence Active hanya boleh menjadi Completed berdasarkan Fulfilment Evidence yang disediakan oleh Destination yang bertanggung jawab.
- **BR-CPOE-021** — Order Occurrence Active hanya boleh menjadi Not Performed berdasarkan Fulfilment Evidence yang disediakan oleh Destination yang bertanggung jawab, dan bukti tersebut wajib memuat alasan.
- **BR-CPOE-022** — Fulfilment Evidence wajib berlaku untuk tepat satu Order Occurrence Active dan tidak dapat diubah setelah dicatat.
- **BR-CPOE-023** — CPOE hanya boleh mencatat outcome fulfilment yang dilaporkan beserta buktinya; CPOE tidak boleh menyimpulkan detail eksekusi atau hasil klinis.
- **BR-CPOE-024** — Completion Progress wajib sama dengan jumlah occurrence dalam setiap status, dan jumlah tersebut harus selalu sama dengan jumlah occurrence total.
- **BR-CPOE-025** — Clinical Order wajib tetap Active selama setidaknya satu occurrence masih Active, kecuali Clinical Order dibatalkan secara eksplisit.
- **BR-CPOE-026** — Ketika semua occurrence menjadi final melalui fulfilment dan setidaknya satu occurrence Completed, Clinical Order wajib menjadi Completed.
- **BR-CPOE-027** — Ketika semua occurrence menjadi final melalui fulfilment dan tidak ada occurrence yang Completed, Clinical Order wajib menjadi Not Performed.
- **BR-CPOE-028** — Cancellation eksplisit wajib menjadikan Clinical Order Cancelled bahkan ketika occurrence sebelumnya sudah Completed atau Not Performed; outcome sebelumnya tersebut wajib tetap terlihat pada Completion Progress dan Order History.

### 7.5 Scheduled order

- **BR-CPOE-029** — Scheduled Order wajib berisi daftar Order Occurrence yang terbatas dan eksplisit.
- **BR-CPOE-030** — Setiap occurrence wajib dilacak secara independen; outcome satu occurrence tidak boleh secara langsung mengubah status occurrence lain.
- **BR-CPOE-031** — Order Occurrence tidak boleh bergantung pada completion, kegagalan, atau waktu occurrence lain.
- **BR-CPOE-032** — Clinical Order tidak boleh menghasilkan occurrence dari recurrence tak terbatas, recurrence rule, cron expression, atau kebijakan auto-renewal.
- **BR-CPOE-033** — Setelah occurrence mana pun menjadi final, jumlah occurrence total Clinical Order tidak boleh bertambah atau berkurang.

### 7.6 Active Order Reconciliation

- **BR-CPOE-034** — Active Order Reconciliation hanya boleh dimulai untuk Inter Ward Transfer.
- **BR-CPOE-035** — Rekonsiliasi hanya boleh mengevaluasi Order Occurrence Active untuk Patient yang ditransfer.
- **BR-CPOE-036** — Sebuah occurrence hanya boleh diidentifikasi terdampak apabila Destination-nya mungkin bergantung pada ward sebelumnya atau ward saat ini, sedangkan RegId tetap tidak berubah.
- **BR-CPOE-037** — Inter Ward Transfer tidak boleh menetapkan ulang Destination secara otomatis.
- **BR-CPOE-038** — Setiap Affected Occurrence Review wajib berakhir dengan tepat satu keputusan: mempertahankan Destination saat ini atau mengubah ke Destination tertentu.
- **BR-CPOE-039** — Keputusan rekonsiliasi wajib mengidentifikasi Reconciliation Reviewer, waktu keputusan, dan alasan.
- **BR-CPOE-040** — Keputusan untuk mengubah Destination hanya boleh diterapkan jika occurrence masih Active dan Destination yang diusulkan sesuai pada saat perubahan.
- **BR-CPOE-041** — Apabila occurrence menjadi final sebelum keputusan rekonsiliasinya diterapkan, Destination occurrence tersebut wajib tetap tidak berubah dan keputusan yang tidak diterapkan wajib dicatat demikian.
- **BR-CPOE-042** — Transfer Reconciliation hanya boleh menjadi Completed setelah setiap Affected Occurrence Review memiliki keputusan yang tercatat; Transfer Reconciliation dapat selesai segera apabila tidak ditemukan occurrence terdampak.

### 7.7 Riwayat audit

- **BR-CPOE-043** — Order History wajib kronologis, append-only, dan lengkap untuk seluruh fakta lifecycle order yang material.
- **BR-CPOE-044** — Koreksi atau keputusan yang lebih baru wajib menambahkan Order History Entry baru dan tidak boleh menghapus atau menggantikan entri sebelumnya.

## 8. State Machines & Lifecycles

### 8.1 Lifecycle Clinical Order

```text
Create valid Clinical Order
          |
          v
        Active
       /   |   \
      /    |    \
     v     v     v
Completed  Not Performed  Cancelled
```

| State | Makna bisnis | Allowed next states |
|---|---|---|
| Active | Setidaknya satu occurrence masih tertunda dan order belum dibatalkan secara eksplisit. | Completed, Not Performed, Cancelled |
| Completed | Semua occurrence final melalui fulfilment dan setidaknya satu occurrence Completed. | None |
| Not Performed | Semua occurrence final melalui fulfilment dan tidak ada occurrence yang Completed. | None |
| Cancelled | Maksud klinisi yang tersisa diakhiri secara eksplisit; outcome occurrence sebelumnya, bila ada, tetap dipertahankan. | None |

Pembuatan tidak memiliki tahap Draft. Completed, Not Performed, dan Cancelled adalah status final.

### 8.2 Lifecycle Order Occurrence

```text
          Active
         /   |   \
        v    v    v
Completed  Not Performed  Cancelled
```

| Transition | Penyebab |
|---|---|
| Active -> Completed | Destination yang bertanggung jawab menyediakan Fulfilment Evidence Completed. |
| Active -> Not Performed | Destination yang bertanggung jawab menyediakan Fulfilment Evidence Not Performed dengan alasan. |
| Active -> Cancelled | Clinical Order induk dibatalkan secara eksplisit. |

Setiap occurrence final mempertahankan outcomenya secara permanen.

### 8.3 Lifecycle Completion Progress

Completion Progress dimulai ketika semua occurrence Active. Setiap transition occurrence memindahkan tepat satu occurrence dari Active ke satu jumlah final. Kemajuan menjadi lengkap ketika jumlah Active mencapai nol.

Untuk Clinical Order Cancelled, kemajuan tetap membedakan occurrence Completed dan Not Performed sebelumnya dari occurrence yang dibatalkan sebelum eksekusi.

### 8.4 Lifecycle Transfer Reconciliation

```text
Inter Ward Transfer reported
            |
            v
          Active
            |
            v
        Completed
```

| State | Makna bisnis | Allowed next states |
|---|---|---|
| Active | Occurrence Active sedang dievaluasi atau occurrence terdampak menunggu keputusan. | Completed |
| Completed | Setiap occurrence terdampak memiliki keputusan tercatat, atau tidak ditemukan occurrence terdampak. | None |

Selesainya rekonsiliasi tidak berarti setiap perubahan Destination yang diusulkan telah diterapkan; keputusan yang tidak diterapkan tetap dicatat.

## 9. Domain Events

| Domain Event | Makna bisnis |
|---|---|
| Clinical Order Created | Clinical Order valid menjadi Active dengan rencana occurrence terbatas dan routing awalnya. |
| Clinical Order Modified | Maksud klinisi atau perencanaan occurrence yang memenuhi syarat berubah sebelum pekerjaan terdampak dieksekusi. |
| Order Occurrence Routed | Order Occurrence Active ditetapkan ke sebuah Destination. |
| Order Occurrence Destination Changed | Keputusan eksplisit mengubah Destination sebuah Order Occurrence Active. |
| Order Occurrence Completed | Fulfilment Evidence menetapkan bahwa satu Order Occurrence telah dilakukan. |
| Order Occurrence Not Performed | Fulfilment Evidence menetapkan bahwa satu Order Occurrence tidak dilakukan. |
| Clinical Order Completed | Fulfilment menutup seluruh occurrence dan setidaknya satu occurrence Completed. |
| Clinical Order Not Performed | Fulfilment menutup seluruh occurrence dan tidak ada yang Completed. |
| Clinical Order Cancelled | Ordering Clinician mengakhiri Clinical Order Active beserta occurrence Active yang tersisa. |
| Transfer Reconciliation Started | Inter Ward Transfer memulai evaluasi atas Order Occurrence Active milik Patient. |
| Affected Order Occurrence Identified | Order Occurrence Active ditemukan memiliki Destination yang berpotensi bergantung pada ward. |
| Reconciliation Decision Recorded | Reconciliation Reviewer memutuskan untuk mempertahankan atau mengubah Destination occurrence terdampak. |
| Transfer Reconciliation Completed | Setiap occurrence terdampak telah memperoleh keputusan, atau tidak ditemukan occurrence terdampak. |

Setiap event mencatat fakta bisnis setelah aturan yang mengaturnya dipenuhi. Event tidak menyatakan pekerjaan klinis telah terjadi kecuali didukung oleh Fulfilment Evidence.

## 10. Workflow Bisnis

### 10.1 Create and route a Clinical Order

1. Ordering Clinician menyatakan Order Specification untuk satu Patient dan RegId.
2. Ordering Clinician mendefinisikan satu occurrence atau satu kumpulan occurrence terjadwal yang terbatas.
3. Destination yang sesuai ditentukan untuk setiap occurrence.
4. Clinical Order menjadi Active.
5. Setiap occurrence Active muncul dalam Destination Worklist-nya.

**Outcome:** satu Clinical Order Active tersedia dengan routing dan riwayat awal yang lengkap.

### 10.2 Modify unexecuted clinical intent

1. Ordering Clinician mengidentifikasi Clinical Order Active atau Order Occurrence Active yang memenuhi syarat.
2. Perubahan yang diusulkan dinilai terhadap aturan modification.
3. Nilai bisnis yang memenuhi syarat diubah.
4. Routing dinilai kembali apabila perubahan dapat memengaruhi Destination.
5. Perubahan dan alasannya ditambahkan ke Order History.

**Outcome:** maksud yang belum dieksekusi diperbarui tanpa mengubah fakta bisnis sebelumnya.

### 10.3 Record occurrence fulfilment

1. Destination yang bertanggung jawab menyediakan Fulfilment Evidence untuk satu occurrence Active.
2. Occurrence menjadi Completed atau Not Performed.
3. Completion Progress dihitung ulang.
4. Clinical Order tetap Active bila occurrence lain masih Active.
5. Ketika tidak ada occurrence yang Active melalui fulfilment, Clinical Order menjadi Completed bila setidaknya satu occurrence Completed; jika tidak, menjadi Not Performed.

**Outcome:** outcome eksekusi yang dilaporkan dicatat tanpa CPOE melaksanakan atau menafsirkan pekerjaan klinis.

### 10.4 Cancel a Clinical Order

1. Ordering Clinician mengidentifikasi Clinical Order Active dan menyediakan alasan cancellation.
2. Setiap occurrence Active yang tersisa menjadi Cancelled.
3. Outcome occurrence Completed dan Not Performed sebelumnya tetap tidak berubah.
4. Clinical Order menjadi Cancelled.
5. Completion Progress dan Order History mempertahankan outcome lengkap.

**Outcome:** maksud klinisi yang masih tertunda berakhir tanpa menyamarkan pekerjaan yang sudah dilakukan atau tidak dilakukan.

### 10.5 Track a Scheduled Order

1. Rencana occurrence terbatas ditetapkan ketika Clinical Order dibuat.
2. Setiap occurrence dirutekan dan dilacak secara independen.
3. Setiap Fulfilment Evidence hanya memfinalkan occurrence yang dirujuknya.
4. Completion Progress melaporkan seluruh jumlah occurrence.
5. Clinical Order mencapai status final ketika tidak ada occurrence Active atau ketika order dibatalkan secara eksplisit.

**Outcome:** satu Clinical Order menyediakan pelaksanaan terbatas yang dapat ditelusuri secara independen serta kemajuan keseluruhan yang deterministik.

### 10.6 Reconcile Active Orders after an Inter Ward Transfer

1. Inter Ward Transfer memulai satu Transfer Reconciliation untuk Patient.
2. Order Occurrence Active milik Patient dievaluasi terhadap konteks ward sebelumnya dan ward baru.
3. Occurrence yang berpotensi bergantung pada ward menjadi Affected Occurrence Review.
4. Reconciliation Reviewer memutuskan untuk mempertahankan atau mengubah setiap Destination terdampak serta mencatat alasannya.
5. Perubahan yang dikonfirmasi hanya diterapkan bila occurrence masih Active dan Destination yang diusulkan tetap sesuai.
6. Setiap keputusan dan perubahan yang diterapkan ditambahkan ke Order History terkait.
7. Transfer Reconciliation menjadi Completed setelah setiap occurrence terdampak memiliki keputusan, atau segera ketika tidak ada occurrence terdampak.

**Outcome:** routing yang bergantung pada ward ditinjau secara eksplisit tanpa penetapan ulang otomatis.
