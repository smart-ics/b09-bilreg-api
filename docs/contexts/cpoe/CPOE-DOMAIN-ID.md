# Domain CPOE — Bahasa Indonesia

> Versi Bahasa Indonesia dari [`CPOE-DOMAIN.md`](CPOE-DOMAIN.md). Istilah domain, nama state, dan nama event berbahasa Inggris dipertahankan apabila merupakan terminologi standar atau lebih umum digunakan dalam praktik klinis dan pengembangan perangkat lunak.

## 1. Gambaran Umum Bisnis

Computerized Provider Order Entry (CPOE) mengatur penyampaian, authorization, routing, koordinasi, dan closure dari clinical order.

Tujuan bisnisnya adalah memastikan suatu kebutuhan klinis menjadi instruksi yang eksplisit, sampai ke unit fulfilment yang bertanggung jawab, memperoleh outcome yang dapat dipertanggungjawabkan, dan tetap dapat ditelusuri sepanjang lifecycle-nya.

CPOE memperluas praktik legacy yang mencatat `Tindakan` terutama untuk billing. Clinical Order merepresentasikan tujuan klinis yang diharapkan (prospective clinical intent). Clinical Order bukan bukti bahwa suatu pelayanan telah dilakukan dan tidak otomatis menimbulkan biaya (charge).

Domain ini mencakup:

- Structured order entry dan authorization.
- Klasifikasi order dan routing ke unit pelaksana.
- Priority, Requested Timing, Clinical Indication, dan Order Instruction.
- Pengelolaan Outstanding Order dan pekerjaan Receiver.
- Acceptance, Rejection, dan Clarification.
- Koordinasi dan pencatatan Hasil Pelaksanaan (Fulfilment Outcome).
- Amendment, Cancellation, Discontinuation, serta riwayat koreksi.
- Hubungan dengan departmental fulfilment, result, execution documentation, dan Charge Eligibility.
- Emergency Action, Verbal Order, Protocol-Based Action, dan Retrospective Order.
- Auditability dan tanggung jawab atas Outstanding Order.
- Discharge Reconciliation tanpa menghambat discharge.

Domain ini tidak memiliki kewenangan atas:

- Asuhan keperawatan rutin yang dilakukan dalam tanggung jawab normal perawat.
- Workflow fulfilment departemen secara terperinci apabila sudah ada domain fulfilment yang authoritative.
- Clinical result atau execution document authoritative yang dimiliki domain klinis lain.
- Penentuan tarif, coverage finansial, perhitungan bill, atau pembayaran.
- Konsumsi inventory dan stock control.
- Clinical review dan tindak lanjut atas result yang telah dirilis pada Phase 1.

Apabila belum tersedia domain fulfilment khusus, CPOE dapat sementara mengatur Generic Fulfilment. Tanggung jawab transisional ini tidak mengubah batas antara Clinical Order dan bukti pelaksanaannya.

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| Clinical Order | Instruksi Klinis | Instruksi klinis prospective yang telah di-authorize untuk meminta pelayanan, intervensi, pemeriksaan, terapi, konsultasi, atau aktivitas klinis lain bagi seorang pasien. |
| Order Author | Penyusun Instruksi | Tenaga profesional yang diizinkan untuk menyiapkan Clinical Order. Author tidak selalu menjadi Authorizer. |
| Order Authorizer | Pemberi Otorisasi Instruksi | Tenaga profesional yang mengambil accountability atas Clinical Order sesuai professional authority dan clinical privilege-nya. |
| Ordering PPA | PPA Pemberi Instruksi | Profesional Pemberi Asuhan yang membuat atau meng-authorize Clinical Order dalam scope kewenangannya. |
| Order Type | Jenis Instruksi | Klasifikasi yang bermakna secara klinis dan menentukan aktivitas yang diminta, informasi wajib, kewenangan yang diizinkan, Destination, serta Completion Criterion. |
| Order Set | Paket Instruksi | Kumpulan Clinical Order terkait yang dikelola untuk situasi klinis tertentu. Setiap order di dalamnya tetap memiliki lifecycle sendiri. |
| Clinical Indication | Indikasi Klinis | Alasan atau pertanyaan klinis yang mendasari Clinical Order. |
| Priority | Prioritas | Tingkat urgensi klinis suatu order, misalnya routine, urgent, atau emergency. |
| Requested Timing | Waktu Pelaksanaan yang Diminta | Waktu, jadwal, frequency, duration, atau kondisi fulfilment yang diinginkan. |
| Order Instruction | Petunjuk Pelaksanaan Instruksi | Informasi di luar identitas aktivitas yang diminta dan diperlukan agar fulfilment aman serta tepat. |
| Destination | Unit Tujuan | Layanan organisasi yang bertanggung jawab menerima dan mengoordinasikan fulfilment suatu order. |
| Receiver | Penerima Instruksi | Tenaga profesional berwenang di Destination yang mengambil penanganan operasional atas order yang telah di-dispatch. |
| Outstanding Order | Instruksi yang Belum Terselesaikan | Order yang masih memiliki tanggung jawab koordinasi CPOE yang belum terselesaikan. |
| Acceptance | Penerimaan | Komitmen Destination untuk mengoordinasikan fulfilment suatu order. |
| Rejection | Penolakan | Penolakan Destination untuk melakukan fulfilment karena order tidak dapat atau tidak boleh dilaksanakan sebagaimana diminta. |
| Clarification Request | Permintaan Klarifikasi | Permintaan formal untuk menyelesaikan ambiguitas, inkonsistensi, kekurangan informasi, atau masalah keselamatan sebelum fulfilment dilanjutkan. |
| Fulfilment | Pelaksanaan | Pelaksanaan aktivitas yang diminta oleh Clinical Order. |
| Executing Domain | Domain Pelaksana | Layanan klinis yang memiliki workflow fulfilment authoritative dan execution record untuk suatu Order Type. |
| Generic Fulfilment | Pelaksanaan Generik | Kapabilitas fulfilment sementara yang diatur CPOE bagi aktivitas yang belum mempunyai Executing Domain khusus. |
| Fulfilment Outcome | Hasil Pelaksanaan | Kesimpulan terstruktur dari fulfilment, termasuk completion atau alasan aktivitas tidak dilakukan. |
| Fulfilment Summary | Ringkasan Pelaksanaan | Fakta operasional umum mengenai fulfilment yang disimpan CPOE, sementara detail execution tetap dimiliki Executing Domain. |
| Fulfilment Reference | Referensi Pelaksanaan | Identitas aktivitas departmental fulfilment authoritative yang terkait dengan Clinical Order. |
| Result Reference | Referensi Hasil | Hubungan antara Clinical Order dan clinical result authoritative-nya. |
| Execution Documentation Reference | Referensi Dokumentasi Pelaksanaan | Hubungan antara Clinical Order dan execution/procedure documentation authoritative-nya. |
| Completion Criterion | Kriteria Penyelesaian | Kondisi bisnis untuk suatu Order Type yang menentukan kapan fulfilment dinyatakan selesai. |
| Occurrence | Kejadian Pelaksanaan | Satu pelaksanaan yang diwajibkan dalam Clinical Order recurring atau terjadwal. |
| Amendment | Amandemen | Perubahan yang dapat dipertanggungjawabkan terhadap authorized order, dengan mempertahankan instruksi sebelumnya dan mengomunikasikan perubahan kepada pihak terdampak. |
| Cancellation | Pembatalan | Penghentian order sebelum clinical fulfilment dimulai. |
| Discontinuation | Penghentian | Penghentian fulfilment yang akan datang atau tersisa setelah order menjadi active, started, recurring, atau partially fulfilled. |
| Entered in Error | Dimasukkan secara Keliru | Pernyataan bahwa order keliru dicatat sebagai instruksi yang valid, tanpa menghapus riwayatnya. |
| Not Fulfilled | Tidak Dilaksanakan | Outcome akhir bahwa aktivitas yang diminta tidak dilakukan, disertai alasan yang dapat dipertanggungjawabkan. |
| Independent Tindakan | Tindakan Mandiri | Tindakan klinis yang dilakukan berdasarkan kewenangan profesional sendiri tanpa individual prospective Clinical Order. |
| Ad Hoc Tindakan | Tindakan Tidak Terencana | Tindakan klinis tidak terencana karena kebutuhan pasien yang segera dan diklasifikasikan berdasarkan kewenangan yang melandasi pelaksanaannya. |
| Verbal Order | Instruksi Lisan | Clinical Order yang disampaikan secara lisan atau melalui telepon ketika prospective electronic authorization tidak praktis; membutuhkan pencatatan read-back dan Subsequent Authorization sesuai kebijakan rumah sakit. |
| Emergency Action | Tindakan Darurat | Tindakan mendesak sebelum authorization order biasa karena penundaan akan membahayakan pasien. |
| Protocol-Based Action | Tindakan Berbasis Protokol | Tindakan yang di-authorize oleh protokol klinis yang disetujui ketika triggering criteria terpenuhi. |
| Retrospective Order | Instruksi Retrospektif | Order yang dicatat setelah execution dan secara jujur menunjukkan actual instruction time, execution time, waktu entry kemudian, serta alasan keterlambatan. |
| Subsequent Authorization | Otorisasi Susulan | Konfirmasi accountability setelah Verbal Order, Emergency Action, atau Retrospective Order. Hal ini tidak menyiratkan bahwa prospective authorization pernah terjadi. |
| Countersignature | Pengesahan Susulan | Konfirmasi berikutnya oleh tenaga profesional yang bertanggung jawab atas exceptional order atau action. |
| Charge Eligibility | Kelayakan Pembebanan Biaya | Fakta fulfilment yang menyatakan bahwa pelayanan aktual atau bagian yang dapat dibenarkan boleh dipertimbangkan untuk billing. Ini bukan tarif ataupun bill. |
| Discharge Reconciliation | Rekonsiliasi Pemulangan | Penilaian dan disposition atas Outstanding Order ketika inpatient encounter berakhir. |
| Reconciliation Warning | Peringatan Rekonsiliasi | Pemberitahuan wajib yang tidak memblokir bahwa masih terdapat unresolved order saat discharge dan membutuhkan acknowledgement, disposition, atau escalation. |
| Carry Forward | Pelanjutan Instruksi | Kelanjutan atau penggantian order secara eksplisit dalam care context yang berbeda setelah discharge atau transfer. |
| Clinical Result Review | Tinjauan Hasil Klinis | Acknowledgement, interpretasi, dan tindak lanjut result oleh Responsible Clinician. Hal ini berada di luar Phase 1. |

## 3. Kapabilitas Bisnis

Kapabilitas Bisnis adalah kemampuan yang harus dimiliki organisasi atau domain untuk menjalankan tanggung jawab bisnis dan menghasilkan outcome tertentu. Kapabilitas menjelaskan **apa yang harus mampu dilakukan**, bukan urutan aktivitasnya, fitur aplikasi, atau cara teknis kemampuan tersebut diimplementasikan. Kapabilitas dapat didukung oleh tenaga profesional, kebijakan, proses, dan sistem informasi.

### 3.1 Clinical Order Definition

**Indonesia:** Definisi Instruksi Klinis

Mendefinisikan Order Type terstruktur, makna klinis yang wajib, Authorizer yang diizinkan, Destination, dan Completion Criterion.

### 3.2 Order Authoring and Authorization

**Indonesia:** Penyusunan dan Otorisasi Instruksi

Menangkap clinical intent dan menetapkan professional accountability sebelum order menjadi actionable.

### 3.3 Order Routing

**Indonesia:** Pengarahan Instruksi

Mengarahkan authorized order ke Destination yang bertanggung jawab atas fulfilment-nya.

### 3.4 Receiver Work Management

**Indonesia:** Pengelolaan Pekerjaan Penerima Instruksi

Menjaga visibility dan responsibility untuk order yang menunggu acceptance, clarification, scheduling, execution, atau resolution.

### 3.5 Acceptance, Rejection, and Clarification

**Indonesia:** Penerimaan, Penolakan, dan Klarifikasi

Memungkinkan Destination berkomitmen terhadap fulfilment, menolak dengan alasan, atau menahan pekerjaan terdampak hingga clarification selesai.

### 3.6 Fulfilment Coordination

**Indonesia:** Koordinasi Pelaksanaan

Melacak outcome operasional umum dari departmental fulfilment tanpa mengambil kepemilikan atas detail execution khusus.

### 3.7 Generic Fulfilment

**Indonesia:** Pelaksanaan Generik

Mencatat execution bagi ordered activity yang belum mempunyai Executing Domain khusus.

### 3.8 Order Change Control

**Indonesia:** Pengendalian Perubahan Instruksi

Mengatur Amendment, Cancellation, Discontinuation, koreksi, serta pemeliharaan riwayat order.

### 3.9 Exceptional Order Governance

**Indonesia:** Tata Kelola Instruksi Khusus

Mengatur Verbal Order, Emergency Action, Protocol-Based Action, dan Retrospective Order, termasuk Subsequent Authorization yang diwajibkan.

### 3.10 Result and Documentation Association

**Indonesia:** Pengaitan Hasil dan Dokumentasi

Menghubungkan order dengan result dan execution documentation authoritative tanpa menduplikasi konten klinisnya.

### 3.11 Billing Eligibility Handover

**Indonesia:** Serah Terima Kelayakan Pembebanan Biaya

Mengomunikasikan bahwa fulfilment aktual dapat membenarkan charge, sementara penentuan finansial tetap menjadi kewenangan Tata Rekening.

### 3.12 Outstanding-Order Reconciliation

**Indonesia:** Rekonsiliasi Instruksi yang Belum Terselesaikan

Memastikan responsibility dinilai kembali saat ward transfer, pergantian DPJP, dan discharge.

### 3.13 Clinical Order Audit

**Indonesia:** Audit Instruksi Klinis

Mempertahankan authorship, authorization, responsibility, decision, perubahan, exceptional authority, Fulfilment Outcome, dan discharge acknowledgement.

## 4. Aktor & Peran

| Aktor atau Peran | Tanggung Jawab dan Kewenangan Bisnis |
|---|---|
| Patient atau Patient Representative | Memberikan consent bila diperlukan, mengikuti preparation instruction, dan dapat menerima atau menolak aktivitas yang diminta. |
| Order Author | Menyiapkan Clinical Order dalam professional scope yang diizinkan. Draft yang disiapkan Author tanpa authorization tidak actionable. |
| Order Authorizer | Mengambil clinical accountability atas order. Dapat berupa dokter atau PPA lain sesuai professional authority dan clinical privilege. |
| Responsible Clinician | Memiliki tanggung jawab clinical follow-up dalam care context saat ini, termasuk unresolved order ketika tanggung jawab pelayanan berubah. |
| DPJP | Memegang tanggung jawab klinis utama dalam inpatient care context dan menerima pengalihan responsibility ketika DPJP berganti. |
| Receiver | Meninjau dispatched order dan dapat accept, reject, atau meminta clarification dalam kewenangan Destination. |
| Fulfilment Coordinator | Mengoordinasikan scheduling, resource, preparation, location, dan assignment di dalam Destination. |
| Clinical Verifier | Menentukan apakah order memenuhi persyaratan klinis atau keselamatan khusus layanan sebelum execution. |
| Performer | Melaksanakan ordered activity sesuai professional competency dan mencatat outcome-nya. |
| Result Author atau Validator | Menghasilkan atau memvalidasi result authoritative apabila diwajibkan oleh Order Type. |
| Discharge Actor | Melakukan reconciliation atau acknowledgement atas Outstanding Order ketika menyelesaikan discharge. |
| Clinical Governance Authority | Mendefinisikan Order Type, professional authority, clinical privilege, protocol, Completion Criterion, kebijakan exceptional order, dan periode Countersignature. |
| Billing Officer | Menangani konsekuensi finansial dari eligible fulfilled service sesuai kebijakan Tata Rekening. |

Satu orang dapat menjalankan beberapa peran bila diizinkan kebijakan rumah sakit. Penggabungan peran tidak menghilangkan accountability yang berbeda untuk authoring, authorization, reception, verification, execution, atau reconciliation.

## 5. Domain Objects

### 5.1 Clinical Order

Merepresentasikan instruksi klinis untuk satu pasien dan satu care context. Objek ini membawa Order Type, Clinical Indication, Priority, Requested Timing, Order Instruction, Author, Authorizer, Destination, responsibility saat ini, dan lifecycle.

### 5.2 Order Definition

Mendefinisikan makna bisnis stabil dari Order Type, termasuk:

- Informasi klinis yang wajib.
- Peran Author dan Authorizer yang diizinkan.
- Destination yang diizinkan.
- Apakah acceptance, verification, scheduling, result, atau execution documentation diwajibkan.
- Apakah fulfilment bersifat single, recurring, scheduled, atau conditional.
- Completion Criterion.
- Apakah ordered activity dapat menghasilkan Charge Eligibility.

### 5.3 Order Authorization

Merepresentasikan keputusan yang dapat dipertanggungjawabkan bahwa Clinical Order yang telah disiapkan boleh dilanjutkan. Objek ini mengidentifikasi Authorizer, dasar kewenangan, dan authorization time.

### 5.4 Order Responsibility

Mengidentifikasi clinical role dan care context yang accountable atas Outstanding Order. Responsibility dapat dialihkan tanpa mengubah authorship atau authorization awal.

### 5.5 Order Destination

Mengidentifikasi layanan organisasi yang bertanggung jawab menerima dan mengoordinasikan fulfilment.

### 5.6 Receiver Decision

Merepresentasikan Acceptance, Rejection, atau Clarification Request oleh Destination, termasuk actor, time, dan reason yang bertanggung jawab.

### 5.7 Clarification

Merepresentasikan pertanyaan yang harus dijawab untuk menyelesaikan ambiguitas atau risiko. Memuat question, requester, responsible responder, urgency, response, dan resolution.

### 5.8 Order Occurrence

Merepresentasikan satu pelaksanaan yang diwajibkan dalam recurring atau scheduled order. Occurrence dapat fulfilled, omitted dengan reason, atau dihentikan melalui Discontinuation atas sisa order.

### 5.9 Fulfilment Summary

Merepresentasikan outcome operasional bersama yang diperlukan CPOE:

- Fulfilment status.
- Start dan completion time bila berlaku.
- Performer dan place of execution bila berlaku.
- Not-performed reason bila berlaku.
- Explanatory note opsional.
- Fulfilment, result, dan execution-documentation reference.

Fulfilment Summary bukan specialized clinical execution record yang authoritative.

### 5.10 Generic Fulfilment Record

Merepresentasikan execution evidence authoritative bagi ordered activity hanya ketika belum tersedia Executing Domain khusus. Objek ini mengidentifikasi apa yang dilakukan, oleh siapa, kapan, di mana, outcome, deviation, serta not-performed reason bila ada.

### 5.11 Order Amendment

Merepresentasikan perubahan yang dapat dipertanggungjawabkan atas authorized Clinical Order. Objek ini mempertahankan previous instruction, reason, amending authority, time, serta dampaknya terhadap pending fulfilment.

### 5.12 Exceptional Authority Record

Mengidentifikasi dasar Verbal Order, Emergency Action, Protocol-Based Action, atau Retrospective Order, termasuk Subsequent Authorization yang diwajibkan.

### 5.13 Charge Eligibility

Merepresentasikan pernyataan dari fulfilment bahwa actual service, Occurrence, atau bagian yang dapat dibenarkan boleh dipertimbangkan untuk billing. Tata Rekening menentukan konsekuensi finansial secara independen.

### 5.14 Discharge Reconciliation

Merepresentasikan penilaian Outstanding Order saat discharge, disposition-nya, acknowledged exception, responsibility yang dialihkan, dan escalation bila diperlukan.

## 6. Aggregates

### 6.1 Clinical Order Aggregate

**Aggregate Root:** Clinical Order

**Tanggung jawab bisnis:** Mempertahankan makna, accountability, lifecycle, dan current operational responsibility dari satu instruksi klinis.

**Consistency boundary mencakup:**

- Order Authorization.
- Current Order Responsibility.
- Destination dan Receiver Decision.
- Clarification.
- Occurrence.
- Amendment.
- Exceptional Authority Record.
- Fulfilment Summary.
- Result dan documentation association.
- Charge Eligibility association.

Clinical Order Aggregate memastikan tidak ada order yang menjadi actionable tanpa authority yang valid, tidak ada material change yang kehilangan riwayatnya, dan tidak ada lifecycle outcome yang bertentangan dengan recorded fulfilment.

Order Set tidak membentuk satu lifecycle yang tidak dapat dipisahkan. Setiap Clinical Order di dalamnya tetap dapat secara independen di-accept, reject, amend, cancel, fulfil, dan dinyatakan charge-eligible.

### 6.2 Generic Fulfilment Aggregate

**Aggregate Root:** Generic Fulfilment Record

**Tanggung jawab bisnis:** Mengatur authoritative execution evidence ketika belum tersedia Executing Domain khusus.

**Consistency boundary mencakup:**

- Assigned atau actual Performer.
- Execution timing dan place.
- Execution outcome.
- Not-performed reason.
- Execution deviation dan supporting documentation association.

Generic Fulfilment hanya tersedia untuk ordered activity. Aggregate ini tidak mengatur routine nursing care atau independent professional documentation.

### 6.3 Discharge Reconciliation Aggregate

**Aggregate Root:** Discharge Reconciliation

**Tanggung jawab bisnis:** Mempertahankan disposition Outstanding Order pada tingkat encounter yang dapat dipertanggungjawabkan, tanpa menjadikan unresolved order sebagai larangan discharge secara universal.

**Consistency boundary mencakup:**

- Order yang teridentifikasi outstanding saat discharge.
- Disposition setiap order yang dinilai.
- Unresolved exception.
- Acknowledgement dan reason untuk melanjutkan.
- Responsibility yang dialihkan setelah discharge.
- Escalation yang diwajibkan.

## 7. Aturan Bisnis

### Clinical intent dan authorization

**BR-CPOE-001** — Clinical Order harus merepresentasikan clinical intent dan tidak boleh diperlakukan sebagai bukti bahwa aktivitas yang diminta telah terjadi.

**BR-CPOE-002** — Clinical Order harus mengidentifikasi satu pasien, satu care context, satu Order Type, Clinical Indication, Priority, Requested Timing, Order Instruction, Author, dan intended Destination sebagaimana diwajibkan Order Definition-nya.

**BR-CPOE-003** — Draft order tidak boleh di-dispatch atau di-fulfil sebelum memiliki authorization yang valid.

**BR-CPOE-004** — Order Authorizer dapat berupa dokter atau PPA lain yang bertindak sesuai professional authority, clinical privilege, dan kebijakan Order Type.

**BR-CPOE-005** — Authorship dan authorization harus tetap dapat dibedakan meskipun dilakukan oleh orang yang sama.

**BR-CPOE-006** — Order Set tidak boleh menghilangkan authorization, lifecycle, atau outcome independen dari setiap Clinical Order di dalamnya.

### Routing dan tanggung jawab Receiver

**BR-CPOE-007** — Setiap authorized order harus memiliki Destination yang bertanggung jawab menerimanya.

**BR-CPOE-008** — Acceptance berarti Destination berkomitmen mengoordinasikan fulfilment; tidak berarti execution telah dimulai atau selesai.

**BR-CPOE-009** — Rejection harus mengidentifikasi Receiver yang accountable dan business reason.

**BR-CPOE-010** — Clarification Request harus mengidentifikasi question, requester, responsible responder, urgency, dan resolution.

**BR-CPOE-011** — Unresolved Clarification harus menempatkan order terdampak dalam status hold untuk clinical fulfilment.

**BR-CPOE-012** — Persiapan nonklinis yang aman boleh berlanjut selama Clarification apabila tidak mengubah kondisi pasien dan tidak menimbulkan risiko klinis.

**BR-CPOE-013** — Emergency fulfilment boleh berlanjut selama unresolved Clarification hanya jika emergency authority dan alasan melanjutkan dicatat.

**BR-CPOE-014** — Clarification yang memengaruhi satu order tidak boleh secara otomatis menahan order lain yang tidak terkait.

### Fulfilment dan completion

**BR-CPOE-015** — Executing Domain memiliki execution record authoritative apabila tersedia specialized fulfilment authority.

**BR-CPOE-016** — CPOE harus menyimpan Fulfilment Summary terstruktur yang cukup untuk menentukan apakah operational responsibility-nya masih outstanding.

**BR-CPOE-017** — Free text boleh menjelaskan Fulfilment Outcome, tetapi tidak boleh menggantikan fulfilment status atau not-performed reason terstruktur.

**BR-CPOE-018** — Koreksi atas execution fact authoritative harus dilakukan di bawah kewenangan Executing Domain.

**BR-CPOE-019** — Generic Fulfilment hanya boleh memiliki execution ketika ordered activity belum mempunyai Executing Domain khusus.

**BR-CPOE-020** — Setiap Order Type harus mendefinisikan Completion Criterion.

**BR-CPOE-021** — Clinical Order menjadi Fulfilled hanya ketika Completion Criterion dari Order Type-nya terpenuhi.

**BR-CPOE-022** — Acceptance, scheduling, preparation, atau execution start tidak dengan sendirinya berarti order telah Fulfilled.

**BR-CPOE-023** — Clinical Order menjadi Closed hanya ketika tidak ada lagi CPOE coordination responsibility, unresolved Clarification, required Occurrence, atau required fulfilment association.

**BR-CPOE-024** — Clinical Result Review tidak diwajibkan untuk closure pada Phase 1.

**BR-CPOE-025** — Expected result atau execution document diwajibkan untuk fulfilment hanya jika Order Definition menjadikannya bagian dari Completion Criterion.

**BR-CPOE-026** — Outcome Not Fulfilled harus menyatakan alasan aktivitas yang diminta tidak dilakukan.

### Perubahan dan penghentian

**BR-CPOE-027** — Perubahan sebelum authorization boleh memperbarui draft tanpa membuat Amendment.

**BR-CPOE-028** — Material change setelah authorization harus dicatat sebagai Amendment yang mempertahankan previous instruction serta mengidentifikasi reason dan authority.

**BR-CPOE-029** — Amendment yang memengaruhi pending fulfilment harus dikomunikasikan kepada Destination yang bertanggung jawab.

**BR-CPOE-030** — Cancellation hanya berlaku sebelum clinical fulfilment dimulai.

**BR-CPOE-031** — Discontinuation menghentikan fulfilment yang akan datang atau tersisa setelah order active, started, recurring, atau partially fulfilled.

**BR-CPOE-032** — Discontinuation tidak boleh membatalkan completed Occurrence atau execution history yang valid.

**BR-CPOE-033** — Order yang dicatat untuk pasien yang salah atau dibuat tanpa clinical intent yang sah harus ditandai Entered in Error, bukan Cancelled.

**BR-CPOE-034** — Riwayat order, authorization, Amendment, termination, fulfilment, atau exceptional authority tidak boleh dihapus dari business history.

### Ad hoc dan exceptional action

**BR-CPOE-035** — Routine nursing care dalam tanggung jawab normal keperawatan berada di luar CPOE.

**BR-CPOE-036** — Tindakan keperawatan yang secara spesifik di-order dapat diatur oleh CPOE.

**BR-CPOE-037** — Independent Tindakan harus dibedakan dari tindakan yang semestinya memerlukan prior authorization tetapi tidak memilikinya.

**BR-CPOE-038** — Verbal Order harus mengidentifikasi issuer, Receiver, instruction, read-back confirmation, dan actual instruction time.

**BR-CPOE-039** — Emergency Action boleh mendahului authorization biasa ketika penundaan membahayakan pasien, tetapi emergency basis, Performer, action, dan execution time harus dicatat.

**BR-CPOE-040** — Protocol-Based Action harus mengidentifikasi approved protocol, applicable version, triggering criteria, dan Performer.

**BR-CPOE-041** — Retrospective Order harus membedakan instruction time, execution time, recording time, dan alasan keterlambatan pencatatan.

**BR-CPOE-042** — Subsequent Authorization yang diwajibkan harus mengonfirmasi accountability tanpa memberi kesan palsu bahwa prospective authorization pernah terjadi.

**BR-CPOE-043** — Kegagalan memperoleh Subsequent Authorization dalam governed period tetap menjadi outstanding exception dan tidak menghapus execution aktual.

### Responsibility dan transisi pelayanan

**BR-CPOE-044** — Responsibility atas Outstanding Order melekat pada clinical role dan care context yang ditentukan, bukan secara permanen pada Author awal.

**BR-CPOE-045** — Ward transfer tidak boleh diam-diam membatalkan Outstanding Order; Destination, validity, dan responsibility-nya harus dinilai kembali.

**BR-CPOE-046** — Pergantian DPJP harus mengalihkan unresolved clinical responsibility kepada DPJP penerus atau responsible care team tanpa mengubah authorship awal.

**BR-CPOE-047** — Discharge harus memulai reconciliation atas seluruh Outstanding Order.

**BR-CPOE-048** — Outstanding Order tidak boleh secara universal memblokir discharge.

**BR-CPOE-049** — Discharge dengan unresolved order harus mewajibkan acknowledgement eksplisit atas Reconciliation Warning.

**BR-CPOE-050** — Reconciliation harus mempertahankan status, disposition, acknowledgement, reason untuk melanjutkan, serta post-discharge responsibility atau escalation bagi setiap unresolved order.

**BR-CPOE-051** — Discharge tidak boleh diam-diam membatalkan seluruh Outstanding Order.

**BR-CPOE-052** — Inpatient recurring order yang tidak sengaja dilanjutkan harus memiliki remaining Occurrence yang di-discontinue saat discharge.

**BR-CPOE-053** — Order yang masih dibutuhkan secara klinis setelah discharge harus secara eksplisit di-carry forward, convert, replace, atau diberi continuing responsibility.

### Result, dokumentasi, dan billing

**BR-CPOE-054** — Result atau execution document authoritative harus tetap dimiliki domain klinis yang bertanggung jawab.

**BR-CPOE-055** — CPOE harus menghubungkan Clinical Order dengan fulfilment, result, dan execution-documentation reference authoritative yang tersedia.

**BR-CPOE-056** — Pembuatan, authorization, dispatch, acceptance, scheduling, atau preparation order tidak boleh menghasilkan Charge Eligibility.

**BR-CPOE-057** — Charge Eligibility hanya timbul dari fulfilment event aktual atau fulfilled portion yang dapat dibenarkan sesuai kebijakan fulfilment terkait.

**BR-CPOE-058** — Cancellation pada umumnya tidak menghasilkan Charge Eligibility.

**BR-CPOE-059** — Discontinuation harus mempertahankan Charge Eligibility yang telah dihasilkan oleh completed Occurrence yang valid atau justified partial fulfilment.

**BR-CPOE-060** — Tata Rekening secara independen menentukan tarif, coverage, bundling, pembuatan bill, adjustment, dan payment berdasarkan eligible fulfilment fact.

### Koeksistensi legacy dan audit

**BR-CPOE-061** — CPOE adalah canonical authority untuk prospective clinical intent yang dimasukkan melalui CPOE.

**BR-CPOE-062** — Legacy departmental order boleh tetap authoritative atas departmental fulfilment-nya sambil mempertahankan hubungan dengan originating Clinical Order.

**BR-CPOE-063** — Legacy transaction yang dibuat langsung harus secara jujur diidentifikasi sebagai legacy-originated atau retrospective; transaction tersebut tidak boleh direpresentasikan sebagai prospectively authorized native CPOE order.

**BR-CPOE-064** — Satu Clinical Order tidak boleh membuat kewajiban departmental fulfilment ganda akibat handover berulang.

**BR-CPOE-065** — Setiap material order decision harus mengidentifikasi responsible actor, business time, reason apabila diwajibkan, dan resulting state.

## 8. State Machine & Lifecycle

### 8.1 Clinical Order Lifecycle

```text
Draft
  → Authorized
  → Dispatched
  → Accepted
  → In Fulfilment
  → Fulfilled
  → Closed
```

Makna bisnis:

| State | Makna |
|---|---|
| Draft | Clinical intent sedang disiapkan dan belum actionable. |
| Authorized | Tenaga profesional yang diizinkan telah mengambil accountability atas order. |
| Dispatched | Order telah diserahkan kepada Destination. |
| Accepted | Destination telah berkomitmen mengoordinasikan fulfilment. |
| In Fulfilment | Preparation atau clinical execution telah dimulai. |
| Fulfilled | Completion Criterion dari Order Type telah terpenuhi. |
| Closed | CPOE tidak lagi memiliki coordination responsibility atas order pada Phase 1. |

Alternatif terminal yang diizinkan:

| State | Makna |
|---|---|
| Rejected | Destination menolak order dengan alasan yang dapat dipertanggungjawabkan. |
| Cancelled | Order dihentikan sebelum clinical fulfilment dimulai. |
| Discontinued | Fulfilment yang akan datang atau tersisa dihentikan setelah order active, started, recurring, atau partially fulfilled. |
| Not Fulfilled | Aktivitas yang diminta mencapai outcome akhir tidak dilaksanakan, disertai alasan. |
| Entered in Error | Order seharusnya tidak pernah ada sebagai instruksi klinis yang valid. |

Unresolved Clarification Request menambahkan kondisi `On Hold for Clarification` pada order yang semula active. Resolution mengembalikan order ke lifecycle state yang tepat, menghasilkan Amendment dan penanganan ulang, atau menghentikan order melalui Rejection, Cancellation, Discontinuation, atau Entered in Error.

### 8.2 Occurrence Lifecycle

```text
Planned
  → Due
  → In Fulfilment
  → Fulfilled
```

Outcome alternatif:

- Omitted dengan reason.
- Cancelled sebelum execution.
- Discontinued sebagai bagian dari remaining order.
- Not Fulfilled dengan reason.

Parent Clinical Order menjadi Fulfilled hanya ketika seluruh required Occurrence memenuhi Completion Criterion, atau remaining Occurrence memiliki terminal disposition yang valid.

### 8.3 Clarification Lifecycle

```text
Requested
  → Responded
  → Resolved
```

Clarification dapat di-withdraw ketika requester menentukan bahwa response tidak lagi diperlukan. Responded tidak berarti Resolved; Receiver menentukan apakah response memungkinkan safe fulfilment.

### 8.4 Exceptional Authorization Lifecycle

```text
Exceptional Action Recorded
  → Subsequent Authorization Required
  → Authorized
```

Jika authorization tidak selesai dalam governed period, record menjadi `Authorization Overdue`. Execution aktual tetap menjadi bagian dari clinical history.

Protocol-Based Action dapat dinyatakan complete berdasarkan standing authority dari protocol ketika individual Countersignature tidak diwajibkan.

### 8.5 Discharge Reconciliation Lifecycle

```text
Outstanding Orders Identified
  → Orders Assessed
  → Dispositions Recorded
  → Reconciliation Completed
```

Ketika order tetap unresolved:

```text
Unresolved Order Identified
  → Warning Acknowledged
  → Responsibility Assigned or Exception Escalated
  → Discharge May Proceed
```

Reconciliation completion berarti kondisi unresolved terlihat dan dapat dipertanggungjawabkan. Hal ini tidak berarti setiap order telah selesai secara klinis.

## 9. Domain Events

| Domain Event | Makna Bisnis |
|---|---|
| Clinical Order Drafted | Prospective clinical instruction telah disiapkan tetapi belum actionable. |
| Clinical Order Authorized | Tenaga profesional yang diizinkan telah mengambil accountability atas order. |
| Clinical Order Dispatched | Order telah diserahkan kepada Destination. |
| Clinical Order Accepted | Destination telah berkomitmen terhadap fulfilment coordination. |
| Clinical Order Rejected | Destination menolak fulfilment dengan alasan. |
| Order Clarification Requested | Destination atau pihak yang bertanggung jawab mengajukan pertanyaan yang memengaruhi safe fulfilment. |
| Order Clarification Responded | Pihak yang bertanggung jawab telah memberikan jawaban. |
| Order Clarification Resolved | Order boleh dilanjutkan atau telah memperoleh disposition eksplisit lainnya. |
| Order Fulfilment Started | Clinical preparation atau execution telah dimulai. |
| Order Occurrence Fulfilled | Satu required Occurrence telah memenuhi Completion Criterion. |
| Clinical Order Fulfilled | Order telah memenuhi Completion Criterion. |
| Clinical Order Closed | CPOE tidak lagi memiliki Phase 1 coordination responsibility. |
| Clinical Order Amended | Authorized instruction telah direvisi secara accountable. |
| Clinical Order Cancelled | Order dihentikan sebelum fulfilment dimulai. |
| Clinical Order Discontinued | Fulfilment yang akan datang atau tersisa telah dihentikan. |
| Clinical Order Not Fulfilled | Aktivitas yang diminta berakhir tanpa pelaksanaan dan disertai alasan. |
| Clinical Order Entered in Error | Order dinyatakan tidak valid sebagai instruksi klinis dengan tetap mempertahankan riwayatnya. |
| Fulfilment Outcome Recorded | Executing Domain atau Generic Fulfilment telah menyatakan structured operational outcome. |
| Clinical Result Made Available | Result authoritative telah dihubungkan dengan order. |
| Execution Documentation Made Available | Execution documentation authoritative telah dihubungkan dengan order. |
| Charge Eligibility Established | Fulfilment aktual menghasilkan fakta yang boleh dipertimbangkan untuk billing. |
| Verbal Order Recorded | Instruksi yang disampaikan secara verbal dan read-back telah didokumentasikan. |
| Emergency Action Recorded | Tindakan di bawah emergency authority telah didokumentasikan. |
| Protocol-Based Action Recorded | Tindakan telah dilakukan berdasarkan approved protocol. |
| Retrospective Order Recorded | Order telah didokumentasikan setelah execution dengan chronology aktual. |
| Exceptional Order Subsequently Authorized | Accountability yang diwajibkan telah dikonfirmasi setelah exceptional action. |
| Exceptional Authorization Became Overdue | Subsequent accountability yang diwajibkan tidak selesai dalam policy period. |
| Order Responsibility Transferred | Accountability atas Outstanding Order telah berpindah ke role atau care context penerus. |
| Discharge Reconciliation Started | Outstanding Order sedang dinilai pada akhir encounter. |
| Reconciliation Warning Acknowledged | Discharge Actor secara eksplisit mengakui unresolved order. |
| Outstanding Order Escalated | Unresolved order telah ditugaskan untuk exceptional follow-up. |
| Discharge Reconciliation Completed | Outstanding Order memiliki disposition yang accountable atau acknowledged exception. |

## 10. Workflow Bisnis

### 10.1 Standard Clinical Order

```text
Clinical Need Identified
  → Clinical Order Authored
  → Clinical Order Authorized
  → Order Routed to Destination
  → Destination Accepts Order
  → Order Fulfilled
  → Required Result or Documentation Associated
  → Clinical Order Closed
```

### 10.2 Receiver Clarification

```text
Order Received
  → Ambiguity or Risk Identified
  → Clarification Requested
  → Affected Fulfilment Held
  → Clarification Responded
  → Order Resumed, Amended, or Terminated
```

### 10.3 Specialized Departmental Fulfilment

```text
Clinical Order Accepted
  → Departmental Fulfilment Begins
  → Authoritative Execution Recorded by Executing Domain
  → Fulfilment Summary Associated with Clinical Order
  → Required Result or Documentation Associated
  → Order Fulfilled and Closed
```

### 10.4 Generic Fulfilment

```text
Clinical Order Accepted
  → No Specialized Executing Domain Exists
  → Generic Fulfilment Performed
  → Execution Outcome Recorded
  → Order Fulfilled and Closed
```

### 10.5 Order Amendment

```text
Material Change Required
  → Amendment Authorized
  → Previous Instruction Preserved
  → Destination Informed
  → Pending Fulfilment Continues Under Amended Instruction or Is Reassessed
```

### 10.6 Cancellation and Discontinuation

```text
Order No Longer Required
  → Fulfilment History Assessed
  → Not Started: Cancelled
  → Active, Recurring, or Partially Fulfilled: Discontinued
  → Completed History and Eligible Fulfilment Preserved
```

### 10.7 Exceptional Action

```text
Immediate or Exceptional Need Identified
  → Verbal, Emergency, Protocol, or Retrospective Authority Identified
  → Action Performed and Truthful Chronology Recorded
  → Subsequent Authorization Completed When Required
  → Overdue Accountability Escalated When Not Completed
```

### 10.8 Ward Transfer atau Pergantian DPJP

```text
Care Context or Responsible Clinician Changes
  → Outstanding Orders Identified
  → Clinical Validity, Destination, and Responsibility Reassessed
  → Responsibility Transferred, Order Amended, or Order Terminated
```

### 10.9 Discharge Reconciliation

```text
Discharge Initiated
  → Outstanding Orders Identified
  → Each Order Assessed
  → Complete, Cancel, Discontinue, Carry Forward, or Escalate
  → Unresolved Exceptions Explicitly Acknowledged
  → Discharge Proceeds
```

### 10.10 Fulfilment ke Billing

```text
Clinical Order Fulfilment Occurs
  → Executing Authority Determines Charge Eligibility
  → Eligible Fulfilment Handed to Tata Rekening
  → Tata Rekening Determines Financial Consequence
```

### 10.11 Koeksistensi Legacy Departmental

```text
CPOE Clinical Intent Authorized
  → Order Associated with Legacy Departmental Fulfilment
  → Legacy Department Performs Existing Fulfilment Responsibility
  → Fulfilment Outcome Returned to CPOE
  → Result, Documentation, and Billing Eligibility Associated
```

Aktivitas yang berasal langsung dari sistem legacy mengikuti klasifikasi legacy atau retrospective secara jujur dan tidak diklasifikasikan ulang sebagai prospectively authorized CPOE intent.
