# Domain Operasional RUANG RANAP

> Pendamping Bahasa Indonesia untuk [`RNA-DOMAIN.md`](RNA-DOMAIN.md). Dokumen berbahasa Inggris tetap menjadi spesifikasi kanonis untuk AI Agent.

## 1. Gambaran Umum Bisnis

Manajemen Operasional RUANG RANAP mengatur akomodasi registrasi pasien rawat inap dan pelaksanaan layanan klinis yang dilakukan di bawah tanggung jawab ruang ranap.

Tujuan bisnisnya adalah memastikan pasien ditempatkan di akomodasi yang sesuai, akomodasi tetap dapat ditelusuri secara operasional selama pemindahan dan pelepasan, serta layanan yang dilakukan ruang ranap dicatat sebagai fakta pelaksanaan yang sebenarnya.

Domain ini mencakup dua tanggung jawab utama:

- Accommodation Management.
- RUANG RANAP Service Execution.

Accommodation Management mengatur penempatan pasien, tujuan hunian, lokasi klinis saat ini, pengaturan rooming-in, akomodasi yang dipertahankan, pemindahan, pelepasan, dan kesiapan tempat tidur.

RUANG RANAP Service Execution mengatur pencatatan yang sesuai dengan kenyataan atas pekerjaan yang dilakukan oleh ruang ranap, baik pekerjaan yang berasal dari Clinical Order maupun yang dicatat sebagai Ad Hoc Tindakan yang berwenang. RNA menerima pekerjaan, dapat menetapkan Performer, serta mencatat deskripsi atau referensi pelaksanaan secara independen dari ketersediaan Tarif. ServiceId yang memenuhi syarat memperkaya publikasi yang dapat ditagihkan setelah ditemukan, tetapi bukan prasyarat untuk mencatat fakta pelaksanaan. RNA tidak memiliki kewenangan atas maksud klinis atau otorisasi Clinical Order dan tidak mendefinisikan layanan. Artefak Domain dan SOP menetapkan semantik kewenangan bisnis ini; implementasi saat ini hanya mengasumsikan autentikasi dasar dan akses aplikasi berbutir kasar, sedangkan penerapan otorisasi kontekstual berbutir halus sengaja ditangguhkan ke Phase-99 berdasarkan ARCH-020.

Manajemen Operasional RUANG RANAP tidak memiliki kewenangan atas:

- Penyusunan, otorisasi, routing, amendemen, pembatalan, penghentian, atau rekonsiliasi Clinical Order, yang menjadi kewenangan CPOE.
- Pengkajian keperawatan, diagnosis keperawatan, perencanaan asuhan keperawatan, dokumentasi intervensi keperawatan, atau evaluasi keperawatan, yang menjadi kewenangan NERS.
- Medication Administration.
- Shift Handover.
- Workflow fulfilment terperinci untuk Laboratorium, Radiologi, Kamar Operasi, Farmasi, Rehabilitasi, atau departemen khusus lainnya.
- Service Definition dan konfigurasi layanan, termasuk jenis performer, aturan jumlah/satuan, persyaratan dokumentasi, kriteria penyelesaian, dan katalog outcome, yang menjadi kewenangan Tarif Context atau context pemilik lain yang disebutkan secara eksplisit.
- Charge Eligibility, pemilihan tarif, paket, cakupan, perhitungan tagihan, penyesuaian, pembayaran, dan setiap konsekuensi finansial lainnya, yang menjadi kewenangan Tata Rekening.
- Otorisasi pemulangan klinis.
- Pembuatan, kepemilikan, penentuan prioritas, routing pembatalan, dan penetapan Admission Waiting List, yang menjadi kewenangan Admisi.
- Konten dan tanggung jawab klinis antar-ruang ranap, yang menjadi kewenangan EMR.
- Perhitungan BOR atau perhitungan indikator utilisasi lainnya.
- Tata kelola master data tempat tidur, kamar, ruang ranap, kelas, atau layanan.

Manajemen Operasional RUANG RANAP dapat menyajikan informasi yang relevan dari domain tetangga, tetapi penyajian tersebut tidak memindahkan kewenangan bisnis.

## 2. Ubiquitous Language

| Inggris | Indonesia | Definisi |
|---|---|---|
| RUANG RANAP | Ruang Rawat Inap | Unit organisasi rawat inap yang bertanggung jawab mengakomodasi pasien dan melaksanakan layanan klinis yang diotorisasi ruang ranap. |
| Accommodation | Akomodasi | Penggunaan ruang ranap, kamar, tempat tidur, atau sumber daya rawat inap terkait untuk tujuan perawatan pasien, retensi, pendamping, atau rooming-in yang ditetapkan. |
| Accommodation Allocation | Alokasi Akomodasi | Penetapan aktif atau historis yang menghubungkan registrasi rawat inap, atau penanda Companion Accommodation-nya, dengan tempat tidur/sumber daya untuk tujuan yang dinyatakan. |
| Clinical Accommodation | Akomodasi Klinis | Akomodasi yang merepresentasikan lokasi perawatan rawat inap pasien saat ini. |
| Retained Accommodation | Akomodasi yang Dipertahankan | Accommodation Allocation yang sudah ada dan sengaja dipertahankan dalam keadaan Active sementara pasien menerima perawatan di akomodasi lain. Alokasi ini terus menggunakan kapasitas tempat tidur dan menghasilkan fakta akomodasi sampai pasien dipulangkan. |
| Companion Accommodation | Akomodasi Pendamping | Penetapan tempat tidur melalui prosedur persetujuan dan validasi Bed Assignment biasa, yang ditandai `IsCompanionBed = true` untuk pendamping yang terkait dengan registrasi rawat inap. Penetapan ini menggunakan tempat tidur yang ditetapkan, tetapi bukan pasien, Clinical Accommodation, atau registrasi pasien. |
| Rooming-In | Rawat Gabung | Pengaturan khusus Ibu dan Bayi ketika Bayi berbagi tempat tidur Ibu, sementara keduanya tetap memiliki registrasi dan riwayat akomodasi yang berbeda. |
| Primary Occupant | Penghuni Utama | Pasien yang penghuniannya menetapkan penggunaan utama akomodasi. |
| Associated Occupant | Penghuni Terkait | Pasien yang diakomodasi bersama Primary Occupant berdasarkan kebijakan yang diizinkan, seperti bayi dalam rooming-in. |
| Clinical Location | Lokasi Klinis | Ruang ranap atau unit klinis tempat pasien saat ini menerima tanggung jawab rawat inap dan koordinasi perawatan. |
| Physical Occupancy | Hunian Fisik | Kehadiran aktual atau penggunaan akomodasi yang diakui oleh pasien atau pendamping. |
| Accommodation Purpose | Tujuan Akomodasi | Alasan yang dinyatakan sehingga suatu alokasi tetap aktif, seperti perawatan klinis, retensi kamar, rooming-in, atau penggunaan oleh pendamping. |
| Occupancy Treatment | Perlakuan Hunian | Kebijakan yang menentukan pengaruh suatu alokasi terhadap kapasitas dan ketersediaan tempat tidur. |
| Reporting Treatment | Perlakuan Pelaporan | Perlakuan deskriptif atas fakta akomodasi yang diberikan kepada konsumen hilir yang berwenang. RNA tidak menghitung BOR atau membuat keputusan finansial darinya. |
| Bed | Tempat Tidur | Sumber daya akomodasi rawat inap terkecil yang diatur dan lazim digunakan untuk penempatan pasien. Master Bed menyimpan status kesiapan operasional terkini sebagai proyeksi pencarian cepat dari transaksi RNA sah terakhir; RNA tidak memiliki kewenangan atas tata kelola master data lainnya. |
| Bed Readiness | Kesiapan Tempat Tidur | Transaksi operasional milik RNA yang mencatat satu transisi tempat tidur ke status kesiapan baru. Transaksi terakhir memproyeksikan status kesiapan terkini yang ditampilkan oleh Bed dan menentukan apakah tempat tidur dapat menerima alokasi baru yang diizinkan. |
| Bed Readiness History | Riwayat Kesiapan Tempat Tidur | Riwayat operasional RNA yang bersifat append-only atas transaksi Bed Readiness. Setiap transisi mengidentifikasi tempat tidur, status kesiapan baru, waktu bisnis, aktor atau pemeriksa yang bertanggung jawab, alasan yang berlaku, serta bukti atau referensi opsional. |
| Bed Assignability | Kelayakan Penetapan Tempat Tidur | Penentuan wajib bahwa tempat tidur dapat menerima alokasi: tempat tidur ada dan aktif, merupakan bagian dari Ward yang dituju, berstatus Ready, tidak memiliki alokasi aktif yang berbenturan, serta memenuhi batasan kapasitas atau hunian yang berlaku. |
| Mandatory Bed Assignability | Kelayakan Wajib Penetapan Tempat Tidur | Penentuan penempatan otomatis yang lengkap dan satu-satunya di RNA: tempat tidur ada dan aktif, merupakan bagian dari Ward yang dituju, berstatus Ready, tidak memiliki alokasi aktif yang berbenturan, dan memiliki kapasitas tersedia berdasarkan kebijakan hunian. |
| Accommodation Correction Fact | Fakta Koreksi Akomodasi | Fakta append-only baru yang mengoreksi Accommodation Fact terdahulu tanpa mengubah atau menghapus fakta asli, mengidentifikasi alasan koreksi, aktor, waktu, dan fakta asli, serta tetap berada dalam Ward dari fakta asli. |
| RegId | Identitas Registrasi | Identitas stabil untuk satu registrasi rawat inap. Pemindahan antar-ruang ranap mempertahankan RegId yang sama, sementara Ward sebelumnya, Ward baru, dan fakta akomodasi tetap dapat ditelusuri secara terpisah. |
| Assign Accommodation | Menetapkan Akomodasi | Membentuk Accommodation Allocation baru untuk sebuah registrasi. |
| Transfer Accommodation | Memindahkan Akomodasi | Memindahkan atau mengklasifikasikan ulang akomodasi pasien sambil mempertahankan riwayat alokasi sebelumnya. |
| Release Accommodation | Melepaskan Akomodasi | Mengakhiri Accommodation Allocation aktif karena tujuan yang dinyatakan tidak lagi berlaku. |
| Internal Transfer | Pemindahan Internal | Pemindahan antar-tempat tidur atau kamar ketika tanggung jawab tetap berada dalam ruang ranap yang sama. |
| Inter-RUANG RANAP Transfer | Pemindahan Antar-RUANG RANAP | Workflow pelepasan kembali ke Admisi yang mempertahankan RegId yang sama: RUANG RANAP asal melepaskan akomodasi dan memberi tahu Admisi, Admisi memiliki Waiting List, dan RUANG RANAP tujuan kemudian menerima pasien melalui alur penempatan Waiting List biasa. |
| Bed Ready | Tempat Tidur Siap | Kondisi tempat tidur yang menunjukkan bahwa tempat tidur dapat menerima alokasi yang diizinkan oleh kebijakan. |
| Cleaning Required | Perlu Dibersihkan | Kondisi tempat tidur yang menunjukkan bahwa hunian telah berakhir, tetapi tempat tidur belum siap untuk ditetapkan kembali. |
| Out of Service | Tidak Dapat Digunakan | Kondisi tempat tidur yang menunjukkan bahwa tempat tidur tidak dapat digunakan karena pemeliharaan, keselamatan, atau pembatasan operasional. |
| RUANG RANAP Service | Layanan RUANG RANAP | Identitas layanan yang ditetapkan oleh Tarif Context dan dirujuk oleh RNA ketika layanan dilaksanakan di bawah tanggung jawab ruang ranap. |
| RUANG RANAP Service Execution | Pelaksanaan Layanan RUANG RANAP | Fakta otoritatif milik RNA bahwa pekerjaan yang dideskripsikan atau dirujuk telah dilakukan oleh Performer pada waktu yang dinyatakan untuk Patient, RegId, dan Destination yang bertanggung jawab, beserta riwayat sumber dan koreksinya. Pengayaan Tarif terpisah dari kebenaran pelaksanaan. |
| Ordered RUANG RANAP Service | Layanan RUANG RANAP Terpesan | RUANG RANAP Service Execution yang berasal dari Clinical Order. |
| Ad Hoc Tindakan | Tindakan Ad Hoc | Tindakan klinis yang tidak direncanakan, timbul dari kebutuhan pasien segera, dan dilakukan berdasarkan kewenangan profesional, keadaan darurat, protokol, atau kewenangan lain yang diizinkan dan dinyatakan. |
| Independent Tindakan | Tindakan Mandiri | RUANG RANAP Service yang dilakukan berdasarkan kewenangan profesional sendiri tanpa Clinical Order prospektif individual. |
| Execution Source | Sumber Pelaksanaan | Asal dan dasar kewenangan yang dinyatakan dari suatu RUANG RANAP Service Execution. |
| Order Occurrence | Kejadian Order | Satu pelaksanaan terbatas yang direncanakan secara eksplisit dari suatu Clinical Order, diidentifikasi oleh `OrderOccurrenceId` dan dilacak secara independen oleh CPOE. |
| Performer | Pelaksana | Profesional yang melakukan atau memimpin langsung suatu RUANG RANAP Service Execution. |
| Responsible RUANG RANAP | RUANG RANAP Penanggung Jawab | Ruang ranap yang bertanggung jawab atas pekerjaan yang diterima dan pencatatan pelaksanaan yang sesuai dengan kenyataan. |
| Entered in Error | Dinyatakan Salah Catat | Pernyataan bahwa catatan pelaksanaan seharusnya tidak pernah ada sebagai catatan yang sah, tanpa menghapus riwayatnya. |
| Performed At | Waktu Pelaksanaan | Waktu bisnis aktual ketika layanan dilakukan; merupakan `OccurredAt` dari fakta Service Execution. |
| Recorded At | Waktu Pencatatan | Waktu persistensi sistem untuk fakta pelaksanaan; merupakan `RecordedAt` dan hanya digunakan untuk audit serta penelusuran teknis. |
| Service Execution Fact | Fakta Pelaksanaan Layanan | Pernyataan bisnis yang tidak dapat diubah bahwa seorang Performer melaksanakan pekerjaan yang dideskripsikan atau dirujuk pada waktu Z untuk Patient, RegId, dan Destination yang bertanggung jawab. Untuk pekerjaan native CPOE, fakta ini juga mengidentifikasi ClinicalOrderId dan OrderOccurrenceId. ServiceId yang telah ditemukan dapat mendukung publikasi billable yang idempoten, tetapi tidak menentukan apakah pelaksanaan terjadi. |
| Not Performed Fulfilment Evidence | Bukti Fulfilment Tidak Dilaksanakan | Pernyataan otoritatif RNA bahwa satu Order Occurrence yang diarahkan tidak dilaksanakan, dengan mengidentifikasi occurrence, waktu efektif, Destination yang bertanggung jawab, referensi bukti, dan alasan. Pernyataan ini bukan Service Execution Fact atau katalog outcome. |

### Standar Waktu Bisnis

RNA menggunakan satu model waktu untuk setiap fakta bisnis:

- **`OccurredAt`** adalah waktu bisnis otoritatif saat fakta terjadi. Fakta penetapan, pelepasan, dan koreksi akomodasi menggunakan waktu tindakan yang dapat dipertanggungjawabkan; Bed Readiness menggunakan waktu transisi kesiapan; Service Execution menggunakan **Performed At**; dan Integration Fact menggunakan waktu bisnis dari fakta sumber.
- **`RecordedAt`** adalah waktu persistensi sistem ketika RNA atau context pengirim menyimpan fakta. Waktu ini hanya digunakan untuk audit dan penelusuran teknis, tidak pernah untuk menetapkan urutan bisnis atau menggantikan waktu bisnis fakta tersebut.
- Fakta bisnis diurutkan berdasarkan `OccurredAt`. Dengan demikian, entri terlambat mempertahankan kronologi bisnis asli sekaligus menyimpan `RecordedAt` yang lebih kemudian.
- Jika waktu bisnis aktual tidak diketahui, produsen menetapkan `OccurredAt = RecordedAt` dan tidak mereka-reka waktu yang lebih awal.
- `OccurredAt` dan `RecordedAt` disimpan sebagai instant UTC. Offset sumber atau tanggal bisnis lokal yang diberikan dapat dipertahankan sebagai metadata tampilan/context, tetapi tidak mengubah pengurutan.

Standar ini berlaku secara konsisten untuk Accommodation Facts, transaksi Bed Readiness, Service Execution Facts, Correction Facts, serta Admisi/RNA Integration Facts.

## 3. Kapabilitas Bisnis

### 3.1 Accommodation Demand Management

**Indonesia:** Pengelolaan Permintaan Akomodasi

Meninjau entri Waiting List milik Admisi untuk Ward yang dituju dan hanya mempertahankan visibilitas yang diperlukan untuk melakukan Bed Assignment. RNA tidak memiliki Waiting List atau mengambil tanggung jawab atas suatu entri sebelum Bed Assignment berhasil.

### 3.2 Accommodation Assignment

**Indonesia:** Penetapan Akomodasi

Menetapkan akomodasi yang sesuai untuk tujuan yang dinyatakan sambil mempertahankan makna pasien, ruang ranap, kamar, tempat tidur, kapasitas, dan pelaporan.

### 3.3 Accommodation Occupancy Management

**Indonesia:** Pengelolaan Hunian Akomodasi

Memelihara Clinical Accommodation saat ini, Retained Accommodation, rooming-in, penggunaan oleh pendamping, dan alokasi serentak lain yang diizinkan. Temporary Absence bukan kapabilitas RNA.

### 3.4 Accommodation Transfer

**Indonesia:** Pemindahan Akomodasi

Mengoordinasikan perubahan akomodasi internal dan melepaskan kasus antar-ruang ranap kembali ke Admisi Waiting List tanpa kehilangan riwayat alokasi. RNA tidak pernah mengoordinasikan antrean pemindahan langsung antar-Ward. Untuk perpindahan antar-Ward, RNA menyediakan fakta akomodasi yang diperlukan CPOE untuk memulai Active Order Reconciliation, tetapi tidak memiliki Transfer Reconciliation Aggregate atau keputusannya.

### 3.5 Accommodation Release and Bed Readiness

**Indonesia:** Pelepasan Akomodasi dan Kesiapan Tempat Tidur

Mengakhiri tujuan akomodasi dan mencatat transaksi Bed Readiness yang memindahkan tempat tidur melalui status cleaning, blocked, out-of-service, atau ready. Status operasional terkini yang ditampilkan Bed merupakan proyeksi transaksi kesiapan terakhir, bukan riwayat utama.

### 3.6 RUANG RANAP Service Work Management

**Indonesia:** Pengelolaan Pekerjaan Layanan RUANG RANAP

Mempertahankan visibilitas atas pekerjaan yang diterima dan penetapan opsional sampai pelaksanaan dicatat atau kewajiban pekerjaan milik sumber ditarik kembali.

### 3.7 Ordered RUANG RANAP Service Execution

**Indonesia:** Pelaksanaan Layanan RUANG RANAP Terpesan

Mencatat pelaksanaan yang berasal dari Clinical Order dan segera melaporkan Service Execution Fact yang otoritatif sebagai bukti fulfilment Completed kepada CPOE, secara independen dari resolusi Tarif. Ketika Destination yang bertanggung jawab menetapkan bahwa suatu Order Occurrence yang diarahkan tidak dilaksanakan, RNA melaporkan Not Performed Fulfilment Evidence minimal beserta alasannya, alih-alih membuat Service Execution Fact.

### 3.8 Ad Hoc Tindakan Execution

**Indonesia:** Pelaksanaan Tindakan Ad Hoc

Mencatat pelaksanaan yang tidak direncanakan atau diotorisasi secara mandiri terhadap identitas layanan Tarif yang ada, berdasarkan klasifikasi kewenangan yang sesuai dengan kenyataan.

### 3.9 Execution Recording and Correction

**Indonesia:** Pencatatan dan Koreksi Pelaksanaan

Mencatat Performer dan Performed At, mempertahankan kronologi entri terlambat, serta menambahkan koreksi atau riwayat Entered in Error tanpa menghapus fakta sebelumnya.

Aktor care-team yang berwenang dapat melaporkan kesalahan. Head Nurse dari Ward pemilik dapat memfinalkan koreksi biasa, tetapi tidak boleh menyetujui laporan atau usulan koreksinya sendiri. Perubahan identitas Patient, Registration, Service, atau source-order, penggantian, Entered in Error, kasus yang disengketakan, dan koreksi material lainnya memerlukan reviewer kedua yang independen dan diberi kewenangan oleh Clinical Governance. Setiap koreksi membawa alasan terstruktur dan identitas audit lengkap; koreksi material juga memerlukan bukti.

### 3.10 Billable Service Execution Fact Publication

**Indonesia:** Publikasi Fakta Pelaksanaan Layanan yang Dapat Ditagihkan

Mempublikasikan Service Execution Facts dan koreksi billable yang telah diperkaya secara finansial ke boundary Tindakan/Tata Rekening setelah ServiceId yang memenuhi syarat ditemukan. Publikasi finansial dapat tetap pending tanpa menunda pencatatan pelaksanaan atau bukti fulfilment CPOE. Satu fakta billable menghasilkan paling banyak satu `Tindakan` tertaut; pelaksanaan non-billable tidak menghasilkan Tindakan. Tata Rekening memiliki setiap konsekuensi tarif, penagihan, penyesuaian, dan penyelesaian selanjutnya.

### 3.11 Accommodation Correction

**Indonesia:** Koreksi Akomodasi

Memungkinkan hanya Head Nurse untuk menambahkan Accommodation Correction Fact sebelum Tata Rekening berstatus `FINALIZED`, selama fakta yang dikoreksi tetap dimiliki Ward yang sama. RNA mempublikasikan koreksi tersebut; setiap bounded context hilir merekonsiliasi datanya sendiri.

### 3.12 RUANG RANAP Operational Audit

**Indonesia:** Audit Operasional RUANG RANAP

Mempertahankan akuntabilitas atas keputusan akomodasi, keputusan pelaksanaan ruang ranap, identitas performer, waktu bisnis, alasan, sumber, dan status yang dihasilkan.

## 4. Aktor & Peran

| Actor or Role | Tanggung Jawab dan Kewenangan Bisnis |
|---|---|
| RUANG RANAP Nurse | Mengoordinasikan akomodasi pasien, memproses pekerjaan layanan ruang ranap, serta melaksanakan atau mencatat layanan dalam lingkup tanggung jawab dan penugasan profesional. |
| RUANG RANAP Midwife | Melaksanakan tanggung jawab operasional ruang ranap yang sama dalam lingkup maternitas, neonatal, dan profesional yang diizinkan. |
| RUANG RANAP Coordinator or Head Nurse | Mengawasi kapasitas ruang ranap, pengecualian alokasi, koordinasi pemindahan, beban kerja, dan eskalasi yang dapat dipertanggungjawabkan. |
| Bed Coordinator | Mengoordinasikan permintaan akomodasi dan penempatan lintas-ruang ranap ketika rumah sakit menetapkan tanggung jawab ini secara terpisah dari ruang ranap. |
| Admisi Actor | Memiliki Waiting List dan mengarahkan pemindahan antar-Ward yang telah dilepaskan melalui proses penempatan Admisi biasa. |
| Performer | Melaksanakan atau memimpin langsung RUANG RANAP Service Execution sesuai kompetensi dan penugasan profesional. |
| Supporting Performer | Berpartisipasi dalam pelaksanaan tanpa menggantikan Performer yang bertanggung jawab. |
| Ordering PPA | Menetapkan maksud klinis prospektif melalui CPOE. Ordering PPA tidak memiliki RUANG RANAP Service Execution. |
| Responsible Clinician | Memegang tanggung jawab klinis atas pasien dan dapat dilibatkan ketika pelaksanaan tidak dapat dilanjutkan, harus diubah, atau memerlukan eskalasi. |
| Patient or Patient Representative | Berpartisipasi dalam akomodasi dan pelaksanaan layanan, dapat memberikan persetujuan yang diperlukan, dan dapat menolak layanan. |
| Housekeeping Actor | Memulihkan kesiapan tempat tidur ketika tanggung jawab pembersihan ditetapkan kepada housekeeping. |
| Maintenance or Facilities Actor | Menyelesaikan kondisi tempat tidur atau kamar yang membuat akomodasi tidak tersedia atau tidak aman. |
| Clinical Governance Authority | Menetapkan rooming-in, tujuan alokasi, serta kewenangan, peninjauan, dan eskalasi Ad Hoc atau Independent Tindakan yang diizinkan tanpa mendefinisikan ulang layanan milik Tarif atau kebijakan finansial milik Tata Rekening. Peran ini tidak menambahkan pembatasan otomatis penempatan RNA. |
| Tarif Context | Memiliki Service Definition, identitas layanan, jenis performer, aturan jumlah/satuan, persyaratan dokumentasi, kriteria penyelesaian, katalog outcome, dan konfigurasi layanan. |
| Tindakan / Tata Rekening | Pemilik Tindakan membuat satu catatan tertaut untuk setiap pelaksanaan billable; Tata Rekening memiliki tarif, paket, cakupan, jumlah, penyesuaian, pembayaran, penyelesaian, dan setiap konsekuensi finansial selanjutnya. |

Satu orang dapat menjalankan beberapa peran jika kebijakan rumah sakit mengizinkannya. Penggabungan peran tidak menghapus akuntabilitas yang berbeda untuk alokasi, pelaksanaan, koreksi, dan penanganan finansial.

## 5. Domain Objects

### 5.1 Accommodation Allocation

Merepresentasikan satu penggunaan akomodasi yang dinyatakan untuk sebuah registrasi rawat inap.

Objek ini mengidentifikasi:

- Registrasi rawat inap.
- Ruang ranap, kamar, tempat tidur, atau sumber daya akomodasi lainnya.
- Accommodation Purpose.
- Apakah alokasi tersebut merupakan Clinical Accommodation saat ini.
- Peran Primary atau Associated Occupant.
- Hubungan dengan registrasi lain ketika rooming-in berlaku.
- Awal dan akhir alokasi.
- Perlakuan okupansi dan pelaporan.
- Alasan penempatan, transfer, retensi, dan pelepasan.

Alokasi aktif tidak selalu berarti pasien secara fisik berada di tempat tidur atau bahwa alokasi tersebut berkontribusi terhadap BOR.

### 5.2 Bed

Merepresentasikan sumber daya akomodasi yang dikelola dengan kebijakan okupansi yang diizinkan. Master Bed menyimpan status kesiapan operasional saat ini yang diproyeksikan dari transaksi Bed Readiness terbaru milik RNA.

Bed tidak memiliki seluruh riwayat akomodasi pasien maupun riwayat utama kesiapan. Penggunaan saat ini ditentukan dari Accommodation Allocation aktif dan kebijakan okupansi yang berlaku.

### 5.3 Bed Readiness

Merepresentasikan satu transaksi operasional milik RNA yang mengubah status kesiapan tempat tidur. Ini merupakan riwayat bisnis append-only, bukan flag master Bed yang dapat diubah.

Objek ini membedakan kondisi operasional seperti:

- Ready.
- Cleaning Required.
- Cleaning in Progress.
- Blocked.
- Out of Service.

Setiap transaksi mempertahankan Bed, status kesiapan baru, waktu bisnis, aktor atau verifikator yang bertanggung jawab, alasan bila berlaku, serta bukti atau referensi opsional. Status saat ini yang diekspos oleh Bed merupakan proyeksi transaksi valid terbaru. Kesiapan terpisah dari okupansi dan perlakuan pelaporan.

### 5.4 Accommodation Occupancy Policy

Menentukan kombinasi alokasi aktif yang diizinkan untuk suatu tempat tidur atau kamar.

Kebijakan ini dapat mengatur:

- Kapasitas okupansi utama.
- Satu Rooming-In Baby terkait sebagai tambahan atas satu Primary Occupant tanpa menambah kapasitas tempat tidur.
- Penggunaan oleh pendamping.
- Akomodasi yang dipertahankan.
- Ko-okupansi sementara.
- Apakah alokasi baru memengaruhi ketersediaan.

### 5.5 Accommodation Reporting Policy

Menentukan perlakuan deskriptif yang melekat pada fakta akomodasi bagi konsumen hilir yang berwenang. RNA tidak menghitung BOR, LOS, hari perawatan, sensus, penagihan, maupun outcome hilir lainnya.

### 5.7 Rooming-In Association

Merepresentasikan hubungan khusus Ibu dan Bayi antara Ibu sebagai Primary Occupant dan bayinya sebagai Associated Occupant. Hubungan tersebut dirujuk dari Patient Social Data: Rekam Medis Bayi merujuk Rekam Medis Ibu.

Setiap pasien mempertahankan registrasi dan riwayat akomodasi yang berbeda. Setiap tempat tidur mendukung paling banyak satu Associated Occupant tersebut sebagai tambahan atas satu Primary Occupant; hal ini tidak menambah kapasitas tempat tidur.

### 5.8 Tarif Service Reference

Untuk publikasi yang dapat ditagihkan, RNA me-resolve identitas Service aktif melalui jalur otoritatif `Bangsal → Layanan → Tarif.AllowedLayanan`. RNA dapat mempertahankan ID Tarif/Service yang stabil dan snapshot tampilan immutable minimum yang diperlukan untuk memahami pekerjaan historis, tetapi RNA tidak menyalin atau mengatur aturan Service Definition. Jika Tarif tidak tersedia, resolusi ServiceId dan publikasi yang dapat ditagihkan tetap pending, sementara Service Execution Fact dan CPOE fulfilment evidence tetap valid. Setiap eksekusi membawa deskripsi atau referensinya; eksekusi non-billable tidak memiliki ServiceId.

Tarif Context, atau pemilik lain yang disebutkan secara eksplisit dan dikoordinasikan oleh Tarif, tetap menjadi otoritas atas tipe pelaksana, aturan kuantitas/unit, persyaratan dokumentasi, kriteria penyelesaian, katalog outcome, dan konfigurasi layanan.

### 5.9 RUANG RANAP Service Execution

Merepresentasikan satu instance eksekusi ruang ranap yang otoritatif.

Objek ini mengidentifikasi:

- Pasien dan konteks perawatan.
- Deskripsi atau referensi eksekusi, beserta identitas Tarif Service ketika sudah di-resolve untuk publikasi yang dapat ditagihkan.
- Execution Source.
- Clinical Order dan Order Occurrence asal untuk pekerjaan native CPOE.
- Patient, RegId, dan Responsible RUANG RANAP Destination.
- Penugasan opsional dan Performer aktual.
- Performed At dan Recorded At.
- Identitas Service Execution Fact yang stabil dan status publikasi.
- Riwayat Correction atau Entered in Error.

Untuk pekerjaan native CPOE, hubungan eksekusinya adalah:

```text
Clinical Order
  → one or more finite Order Occurrences
  → zero or one final RNA fulfilment evidence chain per Order Occurrence
```

Correction merevisi evidence chain yang ada untuk Order Occurrence yang dirujuk. Correction tidak merepresentasikan atau menciptakan pelaksanaan tambahan. Ketika evidence yang dikoreksi merupakan pekerjaan native CPOE, RNA melaporkan correction evidence kepada CPOE; CPOE mencatat konflik atau kebutuhan rekonsiliasi yang timbul tanpa secara otomatis menulis ulang status terminal yang ada.

Ketika Destination yang bertanggung jawab menetapkan bahwa Order Occurrence tidak dilaksanakan, RNA hanya mencatat dan melaporkan evidence minimum berikut:

```text
OrderOccurrenceId
Outcome = Not Performed
EffectiveTime
ResponsibleDestination
EvidenceReference
Reason
```

Not Performed Fulfilment Evidence tidak menyatakan adanya eksekusi, tidak membuat Service Execution Fact, dan tidak memperkenalkan katalog outcome yang kompleks.

### 5.10 Execution Authority Record

Merepresentasikan dasar kewenangan yang sebenarnya untuk Ad Hoc Tindakan atau Independent Tindakan.

Objek ini dapat mengidentifikasi:

- Kewenangan profesional independen.
- Protokol dan versi yang disetujui.
- Dasar kegawatdaruratan.
- Instruksi verbal.
- Dokumentasi retrospektif.

RNA memiliki pencatatan yang benar atas dasar kewenangan dan aktor yang akuntabel. Clinical Governance memiliki setiap peninjauan, keputusan pengecualian, atau eskalasi yang diperlukan. Akuntabilitas ini tidak menciptakan Clinical Order secara retrospektif, dan CPOE hanya dilibatkan ketika eksekusi merujuk `ClinicalOrderId` dan `OrderOccurrenceId` yang sudah ada.

## 6. Aggregates

### 6.1 Accommodation Allocation Aggregate

**Aggregate Root:** Accommodation Allocation

**Tanggung jawab bisnis:** Mempertahankan makna, tujuan, riwayat, dan validitas saat ini dari satu penggunaan akomodasi oleh satu registrasi.

**Consistency boundary mencakup:**

- Accommodation Purpose.
- Peran occupant.
- Penetapan Clinical Accommodation saat ini.
- Awal dan akhir.
- Rooming-In Association bila berlaku.
- Perlakuan okupansi dan pelaporan.
- Keputusan penempatan, transfer, retensi, dan pelepasan.
- Accommodation Correction Facts append-only yang merujuk fakta asli.

Aggregate memastikan lifecycle-nya sendiri tetap koheren dan dapat diaudit. Kebijakan lintas alokasi, termasuk kapasitas tempat tidur dan lokasi klinis utama, dievaluasi melalui domain policies terhadap alokasi aktif yang relevan.

### 6.2 Bed Operational Aggregate

**Aggregate Root:** Bed

**Tanggung jawab bisnis:** Mempertahankan riwayat kesiapan transaksional dan kegunaan yang dikelola untuk satu tempat tidur.

**Consistency boundary mencakup:**

- Transaksi Bed Readiness append-only, masing-masing dengan status baru, waktu bisnis, aktor atau verifikator yang bertanggung jawab, alasan bila berlaku, serta bukti/referensi opsional.
- Transaksi kesiapan terbaru yang digunakan untuk memproyeksikan status saat ini yang ditampilkan oleh Bed.
- Pembatasan dan alasan kesiapan.
- Referensi kebijakan okupansi khusus Bed.

Bed Aggregate tidak memuat seluruh Accommodation Allocation dan tidak memperlakukan satu field pasien saat ini sebagai otoritas okupansi.

### 6.3 RUANG RANAP Service Execution Aggregate

**Aggregate Root:** RUANG RANAP Service Execution

**Tanggung jawab bisnis:** Mempertahankan pekerjaan yang diterima, penugasan opsional, eksekusi yang benar terlepas dari ketersediaan Tarif, pengayaan ServiceId opsional untuk publikasi yang dapat ditagihkan, serta riwayat koreksi non-destruktif.

**Consistency boundary mencakup:**

- Execution Source.
- Referensi `ClinicalOrderId` dan `OrderOccurrenceId` untuk pekerjaan native CPOE.
- Execution Authority Record untuk Ad Hoc atau Independent Tindakan.
- RUANG RANAP yang bertanggung jawab.
- Penugasan opsional dan akuntabilitas Performer aktual.
- Patient, RegId, Destination yang bertanggung jawab, deskripsi atau referensi eksekusi, Performer aktual, Performed At, Recorded At, dan identitas execution fact.
- ServiceId yang telah di-resolve secara opsional dan status publikasi finansial pending atau selesai, tanpa menjadikan keduanya bagian dari kebenaran eksekusi.
- Minimal Not Performed Fulfilment Evidence ketika Destination yang bertanggung jawab menetapkan bahwa Order Occurrence tidak dilaksanakan.
- Riwayat Correction dan Entered in Error dalam occurrence evidence chain yang sama.
- Status publikasi dan acknowledgement per destination, di luar keputusan bisnis itu sendiri.

Satu Clinical Order dapat dikaitkan dengan beberapa RUANG RANAP Service Execution Aggregate hanya ketika setiap aggregate merujuk Order Occurrence terbatas yang berbeda. Setiap Order Occurrence memiliki paling banyak satu RNA fulfilment evidence chain valid saat ini.

## 7. Aturan Bisnis

### Makna dan alokasi akomodasi

**BR-RNA-001** — Accommodation Allocation wajib mengidentifikasi satu registrasi rawat inap, satu sumber daya akomodasi, satu Accommodation Purpose, dan waktu mulainya. Companion Accommodation merujuk registrasi rawat inap terkait dan juga ditandai `IsCompanionBed = true`; hal tersebut tidak membuat registrasi pasien pendamping.

**BR-RNA-002** — Satu registrasi rawat inap dapat memiliki beberapa Accommodation Allocation aktif secara bersamaan apabila tujuannya eksplisit dan diizinkan.

**BR-RNA-003** — Retained Accommodation atau Rooming-In yang aktif tidak boleh secara otomatis diartikan sebagai Clinical Accommodation pasien saat ini. Companion Accommodation tidak pernah menjadi Clinical Accommodation.

**BR-RNA-004** — Current Clinical Location wajib diturunkan dari alokasi aktif yang ditetapkan untuk tanggung jawab perawatan klinis saat ini, bukan dari setiap alokasi aktif.

**BR-RNA-005** — Transfer ke unit klinis lain dapat mengakhiri, mempertahankan, atau mereklasifikasi akomodasi sebelumnya berdasarkan keputusan eksplisit; transfer tidak boleh secara otomatis mengakhiri setiap alokasi sebelumnya.

**BR-RNA-006** — Accommodation Allocation historis tidak boleh ditimpa ketika pasien berpindah tempat tidur, kamar, ruang ranap, tujuan, atau perlakuan pelaporan.

**BR-RNA-007** — Pelepasan Accommodation Allocation mengakhiri tujuan akomodasi yang dinyatakan tersebut dan wajib mengidentifikasi alasan pelepasan yang dapat dipertanggungjawabkan.

**BR-RNA-007a** — Retained Accommodation wajib tetap menjadi Accommodation Allocation berstatus Active, terus menggunakan kapasitas tempat tidur, dan terus menghasilkan fakta akomodasi hingga pasien dipulangkan.

**BR-RNA-007b** — Hanya Ward Nurse atau Head Nurse yang boleh menetapkan Retained Accommodation. Pemulangan mengharuskan setiap Retained Accommodation aktif untuk registrasi tersebut dilepaskan.

### Okupansi tempat tidur, rooming-in, dan ketersediaan

**BR-RNA-008** — Sebuah tempat tidur dapat memiliki satu Primary Occupant dan, hanya untuk Mother-and-Baby Rooming-In, satu Associated Occupant (Baby). Occupant terkait tersebut tidak menambah kapasitas tempat tidur.

**BR-RNA-009** — Rooming-In wajib mempertahankan identitas registrasi yang terpisah bagi Primary Occupant dan Associated Occupant.

**BR-RNA-010** — Rooming-In Association wajib mengidentifikasi Ibu sebagai Primary Occupant dan Bayi sebagai Associated Occupant dengan merujuk hubungan Patient Social Data yang tersimpan, yaitu Rekam Medis Bayi merujuk Rekam Medis Ibu.

**BR-RNA-011** — Rooming-In tidak boleh direpresentasikan dengan menggabungkan registrasi ibu dan bayi atau menetapkan satu identitas registrasi kepada kedua pasien.

**BR-RNA-011a** — Hanya Ward Nurse atau Head Nurse yang boleh mengaktifkan Rooming-In. Setiap tempat tidur mendukung Rooming-In berdasarkan aturan satu Primary ditambah satu Baby.

**BR-RNA-011b** — RNA tidak boleh menghitung BOR untuk Rooming-In. Sampai perilaku legacy dikonfirmasi, asumsi penagihan hilir yang terdokumentasi adalah hanya satu biaya kamar; RNA mencatat dan memublikasikan fakta akomodasi tanpa menerapkan keputusan penagihan tersebut.

**BR-RNA-012** — Ketersediaan tempat tidur wajib ditentukan dari Bed Readiness, alokasi aktif, tujuan okupansi, dan Accommodation Occupancy Policy, bukan hanya dari ada atau tidaknya alokasi aktif.

**BR-RNA-013** — RNA wajib mempertahankan fakta akomodasi secara eksplisit dan tidak boleh menghitung BOR atau menyimpulkan hasil finansial dari jumlah registrasi yang dikaitkan dengan suatu tempat tidur.

**BR-RNA-014** — RNA wajib memublikasikan fakta akomodasi yang benar ketika konsumen hilir yang berwenang memerlukannya; RNA tidak boleh menafsirkan fakta tersebut sebagai Charge Eligibility atau keputusan finansial lainnya.

**BR-RNA-015** — Sebuah tempat tidur hanya boleh menerima alokasi baru ketika Mandatory Bed Assignability telah ditetapkan: tempat tidur ada dan aktif, merupakan milik Ward yang dituju, berstatus Ready, tidak memiliki alokasi aktif yang berkonflik, serta memiliki kapasitas yang tersedia berdasarkan kebijakan okupansi yang berlaku. RNA tidak boleh menolak Bed Assignment atas dasar gender, isolasi, peralatan, atau kebijakan operasional khusus rumah sakit lainnya.

Care Class dan Care Context merupakan fakta turunan dari Clinical Accommodation aktif: Care Class diturunkan dari kamar/tempat tidur yang ditetapkan, sedangkan konteks intensive-care diturunkan dari akomodasi ICU, PICU, NICU, atau yang setara yang ditetapkan. Keduanya merupakan outcome dari Accommodation Assignment, bukan atribut pasien atau prasyarat yang digunakan untuk memilih tempat tidur.

### Penempatan dan transfer

**BR-RNA-016** — Accommodation assignment wajib mengidentifikasi aktor yang bertanggung jawab, waktu bisnis, tujuan, dan evidence Mandatory Bed Assignability.

**BR-RNA-016b** — Companion Accommodation wajib menggunakan prosedur Bed Assignment resmi, pemeriksaan Mandatory Bed Assignability, fakta penempatan, proses pelepasan, dan audit yang sama seperti bed assignment biasa, dengan `IsCompanionBed = true`. Akomodasi tersebut menggunakan tempat tidur yang ditetapkan, dikaitkan dengan registrasi rawat inap, dan dipublikasikan kepada konsumen hilir dengan flag tersebut agar laporan penghitungan pasien, termasuk RL, dapat mengecualikannya. Akomodasi ini tidak menciptakan outcome Admisi Waiting List atau mengubah lokasi klinis pasien.

**BR-RNA-016a** — Admission memiliki setiap entri Waiting List selama Ward meninjaunya. Penolakan Ward membuat tanggung jawab tetap berada pada Admission. Bed Assignment yang berhasil membuat Accommodation Fact, menutup Waiting List sebagai konsekuensi, dan secara otomatis memindahkan tanggung jawab kepada RNA. Tidak ada status Accepted atau pemindahan tanggung jawab perantara.

**BR-RNA-017** — Transfer internal wajib mempertahankan kontinuitas dan riwayat akomodasi yang dilepaskan maupun yang baru ditetapkan.

**BR-RNA-018** — Inter-RUANG RANAP Transfer tidak boleh berupa transfer langsung antarward. RNA sumber melepaskan Clinical Accommodation aktif dan memberi tahu Admisi agar pasien kembali ke Admission Waiting List.

**BR-RNA-019** — Setelah Release, RNA tidak memiliki antrean transfer, keputusan destination, maupun tanggung jawab operasional atas transfer. Admisi memiliki routing Waiting List dan destination assignment; Ward tujuan meninjau entri melalui alur Waiting List dan Bed Assignment yang sama seperti pasien lainnya.

**BR-RNA-019a** — Kepemilikan oleh destination sebelum Bed Assignment berhasil dan kontrak pemindahan tanggung jawab eksplisit tidak boleh ada. Konten klinis antarward merupakan milik EMR.

**BR-RNA-019b** — Transfer antarward yang dibatalkan kembali melalui proses Admission Waiting List yang sama; RNA tidak membuat workflow pengembalian akibat pembatalan secara terpisah.

**BR-RNA-019c** — Transfer antarward wajib mempertahankan `RegId` yang sama; Ward sebelumnya, Ward baru, dan riwayat akomodasi wajib tetap dapat diidentifikasi secara terpisah.

**BR-RNA-019d** — Release dari Ward sebelumnya atau assignment ke Ward baru tidak boleh secara otomatis mengubah Destination dari Clinical Order atau Order Occurrence mana pun.

**BR-RNA-019e** — Untuk transfer antarward, RNA wajib mengekspos `RegId`, Ward sebelumnya, Ward baru, waktu efektif transfer, dan referensi ke pekerjaan Active CPOE relevan yang diketahui RNA agar CPOE dapat memulai assisted reconciliation.

**BR-RNA-019f** — RNA hanya boleh menerapkan perubahan Destination terhadap tanggung jawab pending-work yang cocok setelah menerima keputusan rekonsiliasi CPOE yang telah dikonfirmasi. RNA tidak boleh mengubah Clinical Order Aggregate secara langsung.

**BR-RNA-019g** — Transfer akomodasi, release, atau assignment Ward baru tidak boleh diartikan sebagai pembatalan Clinical Order atau Order Occurrence. Pekerjaan aktif tetap diatur oleh CPOE sampai CPOE mencatat fulfilment outcome yang memenuhi syarat, cancellation, atau reconciliation decision.

**BR-RNA-020** — Temporary Absence tidak dimodelkan, disimpan, ditampilkan, ataupun ditindaklanjuti oleh RNA. Temporary Absence tidak memiliki lifecycle RNA dan tidak boleh memengaruhi akomodasi, okupansi, bed readiness, pelaporan pasien, atau fakta penagihan.

**BR-RNA-021** — Otorisasi pemulangan tidak dengan sendirinya membuktikan bahwa akomodasi telah dilepaskan secara fisik.

**BR-RNA-022** — Pelepasan akomodasi tidak dengan sendirinya menetapkan bahwa encounter rawat inap telah dipulangkan.

### Kesiapan tempat tidur

**BR-RNA-023** — Pelepasan akomodasi tidak boleh secara otomatis menjadikan tempat tidur Ready.

**BR-RNA-024** — Tempat tidur yang memerlukan pembersihan, inspeksi, atau pemeliharaan wajib tetap tidak tersedia sampai kondisi kesiapan yang menjadi tanggung jawab tersebut diselesaikan.

**BR-RNA-025** — Setiap transisi Bed Readiness wajib dicatat sebagai transaksi operasional milik RNA; transaksi tersebut wajib mengidentifikasi Bed, status kesiapan baru, waktu bisnis, aktor atau verifikator yang bertanggung jawab, alasan bila berlaku, serta bukti atau referensi opsional.

**BR-RNA-025a** — Status kesiapan saat ini yang diekspos oleh Bed wajib merupakan proyeksi transaksi Bed Readiness valid terbaru dan tidak boleh diperlakukan sebagai riwayat bisnis utama atau diubah tanpa mencatat transaksi tersebut.

**BR-RNA-025b** — Ready wajib diverifikasi secara eksplisit oleh aktor yang berwenang. Flag status saat ini Ready tanpa transaksi Bed Readiness terverifikasi yang sesuai bukan merupakan riwayat otoritatif.

### Kewenangan dan sumber eksekusi RUANG RANAP

**BR-RNA-026** — RNA wajib mencatat eksekusi otoritatif untuk pekerjaan yang dilaksanakan di bawah tanggung jawab ruang ranap terlepas dari ketersediaan Tarif. Eksekusi wajib mengidentifikasi Patient, RegId, Destination yang bertanggung jawab, Performer aktual, Performed At, dan deskripsi atau referensi eksekusi; pekerjaan native CPOE juga wajib mengidentifikasi `ClinicalOrderId` dan `OrderOccurrenceId`. RNA tidak boleh menentukan layanan apa yang tersedia atau mengonfigurasi aturan eksekusinya.

**BR-RNA-027** — Laboratorium, Radiologi, Kamar Operasi, Farmasi, Rehabilitasi, dan layanan khusus lainnya wajib tetap menjadi otoritas atas workflow eksekusi masing-masing.

**BR-RNA-028** — Ruang ranap dapat mengoordinasikan persiapan, transportasi, atau pekerjaan pendukung untuk Executing Domain lain, tetapi tidak boleh mencatat layanan domain tersebut sebagai selesai kecuali kewenangan telah didelegasikan secara eksplisit.

**BR-RNA-029** — Setiap RUANG RANAP Service Execution wajib menyatakan Execution Source-nya.

**BR-RNA-030** — Native Ordered RUANG RANAP Service Execution wajib merujuk `ClinicalOrderId` asal dan `OrderOccurrenceId`-nya.

**BR-RNA-031** — Ad Hoc Tindakan wajib mengidentifikasi dasar kewenangan yang melandasi pelaksanaannya.

**BR-RNA-032** — Independent Tindakan wajib dibedakan dari tindakan yang memerlukan Clinical Order sebelumnya tetapi tidak memilikinya.

**BR-RNA-033** — RUANG RANAP Operational Management tidak boleh secara retrospektif merepresentasikan tindakan Ad Hoc, Emergency, Verbal, Protocol-Based, Independent, atau tindakan yang terlambat dicatat sebagai Clinical Order yang diotorisasi secara prospektif.

**BR-RNA-034** — CPOE tetap menjadi otoritas atas intent, authorization, dan lifecycle Clinical Order yang ada. RNA dan Clinical Governance memiliki dasar kewenangan, peninjauan, dan eskalasi untuk Ad Hoc atau Independent Tindakan. RNA hanya boleh melibatkan CPOE ketika eksekusi merujuk `ClinicalOrderId` dan `OrderOccurrenceId` yang nyata.

**BR-RNA-035** — RUANG RANAP Service yang diarahkan ke RNA tidak boleh menghasilkan execution fact duplikat melalui CPOE Generic Fulfilment.

### Pencatatan eksekusi RUANG RANAP

**BR-RNA-036** — Pending work merepresentasikan permintaan atau kewajiban operasional yang diterima RNA dan bukan evidence bahwa layanan telah terjadi.

**BR-RNA-037** — Performer assignment merupakan koordinasi kerja RNA yang opsional dan tidak membuktikan eksekusi.

**BR-RNA-038** — Pencatatan eksekusi wajib mengidentifikasi Patient, RegId, Destination yang bertanggung jawab, Performer aktual, Performed At, dan deskripsi atau referensi eksekusi, ditambah `ClinicalOrderId` dan `OrderOccurrenceId` untuk pekerjaan native CPOE. Secara bersama-sama, hal tersebut membentuk Service Execution Fact otoritatif milik RNA dan wajib segera dilaporkan sebagai Completed fulfilment evidence untuk Order Occurrence yang dirujuk tanpa menunggu resolusi ServiceId atau publikasi finansial.

**BR-RNA-039** — Penerimaan, peninjauan, penugasan, atau persiapan tidak boleh direpresentasikan sebagai Service Execution Fact.

**BR-RNA-040** — Ketika CPOE membatalkan Clinical Order sumber, RNA wajib menandai pending work yang cocok sebagai source-cancelled. Source cancellation tidak boleh membuat RNA Fulfilment Evidence atau Service Execution Fact karena CPOE sudah memiliki cancellation outcome tersebut.

**BR-RNA-041** — Ketika RNA Destination yang bertanggung jawab menetapkan bahwa Order Occurrence yang diarahkan tidak dilaksanakan, RNA wajib melaporkan Not Performed Fulfilment Evidence yang memuat `OrderOccurrenceId`, `Outcome = Not Performed`, `EffectiveTime`, `ResponsibleDestination`, `EvidenceReference`, dan `Reason`. RNA tidak boleh membuat Service Execution Fact untuk occurrence tersebut atau memperkenalkan katalog partial, aborted, completion, atau outcome yang kompleks.

**BR-RNA-042** — Satu `OrderOccurrenceId` wajib menghasilkan paling banyak satu RNA fulfilment evidence chain yang valid saat ini. Pengiriman atau pencatatan berulang tidak boleh menciptakan eksekusi tambahan.

**BR-RNA-043** — Satu Clinical Order dapat menghasilkan beberapa RUANG RANAP Service Execution hanya ketika setiap eksekusi merujuk Order Occurrence yang berbeda.

**BR-RNA-044** — Status order legacy seperti `Implemented`, `Done`, atau `Executed` tidak boleh diperlakukan sebagai Service Execution Fact milik RNA tanpa Patient, RegId, Destination yang bertanggung jawab, Performer aktual, Performed At, serta deskripsi atau referensi eksekusi.

### Waktu, pencatatan terlambat, koreksi, dan audit

**BR-RNA-045** — Performed At dan Recorded At wajib tetap dapat dibedakan.

**BR-RNA-046** — Entri retrospektif atau terlambat wajib mempertahankan waktu eksekusi aktual, waktu pencatatan, pencatat, dan alasan keterlambatan pencatatan.

**BR-RNA-047** — Correction wajib mempertahankan riwayat eksekusi asli, mengidentifikasi aktor yang melakukan koreksi, waktu koreksi, dan alasan, serta merevisi fulfilment evidence chain yang ada untuk `OrderOccurrenceId` yang sama. Correction tidak boleh membuat atau menyiratkan pelaksanaan tambahan. Untuk pekerjaan native CPOE, RNA wajib melaporkan correction evidence kepada CPOE, yang mencatat konflik atau kebutuhan rekonsiliasi tanpa secara otomatis menulis ulang status occurrence terminal.

**BR-RNA-048** — Eksekusi yang dicatat terhadap pasien yang salah, layanan yang salah, occurrence duplikat, atau tanpa dasar yang sah wajib ditandai Entered in Error, bukan dihapus secara fisik.

**BR-RNA-049** — Tidak ada keputusan akomodasi, Service Execution Fact, correction, atau authority record yang boleh dihapus dari riwayat bisnis.

**BR-RNA-049a** — Hanya Head Nurse yang boleh membuat Accommodation Correction Fact. Fakta tersebut hanya boleh mengoreksi Assignment, Internal Transfer, Release, Retained Accommodation, Rooming-In, atau Bed Readiness Fact yang berlaku selama fakta asli merupakan milik Ward yang sama.

**BR-RNA-049b** — Accommodation Correction hanya diizinkan sebelum Tata Rekening mencapai `FINALIZED`. Setelah `FINALIZED`, RNA wajib menolak koreksi biasa; perubahan lebih lanjut mengikuti proses administratif di luar RNA.

**BR-RNA-049c** — Accommodation Correction Fact wajib merujuk fakta asli dan mempertahankannya tanpa perubahan. RNA memublikasikan correction fact; setiap bounded context hilir merekonsiliasi datanya sendiri.

### Referensi layanan dan execution fact

**BR-RNA-050** — Setiap Bangsal wajib memiliki satu `LayananId` otoritatif. Untuk publikasi intended billable, RNA wajib meminta kepada Tarif daftar Service aktif yang `AllowedLayanan`-nya mencakup Layanan tersebut dan hanya mempertahankan satu `ServiceId` stabil terpilih yang memenuhi syarat. Jika Tarif tidak tersedia atau ServiceId belum di-resolve, pencatatan eksekusi tetap wajib committed, sedangkan pengayaan finansial dan publikasi yang dapat ditagihkan tetap pending. Eksekusi non-billable tidak boleh mempertahankan ServiceId. RNA tidak boleh membuat Service Definition lokal.

**BR-RNA-051** — Tipe Performer, aturan kuantitas/unit, persyaratan dokumentasi, kriteria penyelesaian, katalog outcome, dan konfigurasi layanan tetap dimiliki Tarif Context atau owning context lain yang disebutkan secara eksplisit dan tidak boleh menjadi invariant RNA aggregate.

**BR-RNA-052** — Service Execution Fact yang dilaporkan sebagai clinical execution evidence wajib memuat identitas fakta yang stabil, Patient, RegId, Destination yang bertanggung jawab, Performer, Performed At, deskripsi atau referensi eksekusi, korelasi source/order/occurrence bila berlaku, serta metadata correction. ServiceId disertakan ketika sudah di-resolve, tetapi tidak diwajibkan untuk CPOE fulfilment evidence.

**BR-RNA-053** — Dokumentasi klinis otoritatif tetap berada di NERS atau domain dokumentasi pemiliknya. RNA tidak mewajibkan atau menyalinnya hanya untuk menetapkan execution fact.

### Batas finansial dan publikasi

**BR-RNA-054** — Pengguna eksekusi RNA wajib secara eksplisit mengklasifikasikan eksekusi sebagai intended billable atau non-billable tanpa menunda execution fact. Eksekusi intended billable dengan ServiceId yang belum di-resolve wajib tetap pending untuk pengayaan dan publikasi finansial; eksekusi non-billable tidak membuat `Tindakan`. RNA tidak boleh menghitung tarif, coverage, amount, journal, payment, atau settlement.

**BR-RNA-055** — RNA hanya boleh memublikasikan Service Execution Fact billable yang telah diperkaya secara finansial ke boundary Tindakan/Tata Rekening, dengan membawa identitas eksekusi yang stabil dan Service terpilih, tetapi tanpa field tarif, package, coverage, amount, journal, payment, atau settlement. ServiceId yang belum di-resolve hanya membuat publikasi finansial tetap pending dan tidak boleh menunda CPOE fulfilment evidence.

**BR-RNA-056** — Tata Rekening merupakan satu-satunya otoritas yang mengevaluasi Service Execution Fact dan menentukan apakah terdapat konsekuensi penagihan.

**BR-RNA-057** — Tata Rekening dapat menggabungkan fakta tersebut dengan Tarif dan kebijakan finansial lainnya; RNA tidak boleh menduplikasi aturan tersebut.

**BR-RNA-058** — RNA tidak boleh menentukan kelayakan finansial, tarif, cakupan paket, coverage, jumlah tagihan, adjustment, journal, atau payment. Klasifikasi intended-billable/non-billable yang dipilih pengguna mengendalikan pengayaan finansial dan pengiriman ke Tindakan, bukan apakah kebenaran eksekusi dapat dicatat atau dilaporkan ke CPOE; klasifikasi tersebut bukan perhitungan atau persetujuan finansial.

**BR-RNA-059** — Correction atau Entered in Error setelah publikasi wajib menghasilkan correction fact baru berversi yang merujuk execution fact asli; koreksi finansial hilir tetap menjadi tanggung jawab Tata Rekening. RNA tidak boleh mengasumsikan bahwa CPOE membuka kembali, menggantikan, mengaktifkan kembali, atau menulis ulang dengan cara lain suatu Order Occurrence final karena correction evidence telah dilaporkan.

### Koeksistensi legacy

**BR-RNA-060** — Sumber order legacy yang ada dapat terus membuat pekerjaan ruang ranap sampai CPOE menggantikan otoritas intent prospektifnya.

**BR-RNA-061** — Pekerjaan yang berasal dari legacy wajib mempertahankan identitas sumbernya dan tidak boleh direpresentasikan sebagai intent native CPOE kecuali benar-benar dibuat dan diotorisasi melalui CPOE.

**BR-RNA-062** — Pengiriman ulang atau retry sumber legacy tidak boleh membuat kewajiban RUANG RANAP Service Execution duplikat untuk source occurrence yang sama.

**BR-RNA-063** — Transisi dari ordering legacy ke CPOE tidak boleh mengubah makna bisnis atau riwayat RUANG RANAP Service Execution yang telah selesai.

**BR-RNA-063a** — Setelah cutover fasilitas/unit yang disetujui, native CPOE menggantikan `OrderTdk` untuk order baru; `OrderTdk` historis tetap read-only dan diberi label sumber secara eksplisit.

**BR-RNA-063b** — `Tindakan` tetap menjadi catatan billable-execution. Satu `ServiceExecutionFactId` billable dapat membuat paling banyak satu `Tindakan` tertaut; eksekusi non-billable tidak membuat apa pun. `Tindakan` tidak menggantikan riwayat eksekusi otoritatif milik RNA.

**BR-RNA-063c** — Rollback cutover wajib menghentikan intake native baru tanpa menghapus atau membuat ulang native order yang telah committed, execution fact RNA, atau record `Tindakan` tertaut.

**BR-RNA-063d** — Legacy Accommodation Stay dan RNA wajib beroperasi secara paralel. Identitas Stay legacy dan field milik legacy tetap otoritatif; extension dan riwayat milik RNA menggunakan identitas legacy yang stabil sebagai key.

**BR-RNA-063e** — RNA tidak boleh mewajibkan migrasi massal atau backfill untuk membaca Stay yang ada. Detail historis yang tidak tersedia tetap tidak diketahui; RNA tidak boleh menciptakan waktu bisnis, tujuan, atau fakta transisi yang tidak pernah ada.

**BR-RNA-063f** — Satu field memiliki satu write authority. Perubahan yang berasal dari RNA terhadap field milik legacy menggunakan boundary aplikasi legacy; perubahan bypass yang berasal dari legacy dideteksi melalui pemeriksaan versi/rekonsiliasi dan tidak secara diam-diam menimpa riwayat extension RNA.

## 8. State Machines & Lifecycles

### 8.1 Accommodation Allocation Lifecycle

```text
Proposed
  → Active
  → Released
```

Outcome alternatifnya adalah:

- Cancelled sebelum aktivasi.
- Entered in Error.

Makna bisnis:

| State | Makna |
|---|---|
| Proposed | Akomodasi telah diidentifikasi atau dipersiapkan, tetapi belum menjadi alokasi aktif. |
| Active | Tujuan akomodasi sedang berlaku. |
| Released | Tujuan akomodasi yang dinyatakan telah berakhir. |
| Cancelled | Alokasi yang diusulkan tidak akan menjadi aktif. |
| Entered in Error | Alokasi tersebut seharusnya tidak pernah ada sebagai catatan bisnis yang valid. |

Transfer tidak menimpa alokasi Active. Transfer melepaskan atau mereklasifikasi alokasi sebelumnya dan menetapkan alokasi baru yang diperlukan dengan tetap menjaga kesinambungan.

Retained Accommodation tetap `Active` hingga pasien dipulangkan dan dilepaskan sebagai bagian dari penutupan akomodasi saat pemulangan. Transfer antar-RUANG RANAP tidak menciptakan transisi RNA-ke-RNA: Clinical Accommodation sumber dilepaskan, kemudian setiap alokasi tujuan berikutnya ditetapkan secara mandiri melalui alur Admisi Waiting List dan Bed Assignment.

### 8.2 Bed Readiness Lifecycle

```text
Ready
  → Occupancy or Retention Use
  → Cleaning Required
  → Cleaning in Progress
  → Ready
```

Kondisi tambahannya adalah:

- Blocked.
- Out of Service.

Bed hanya dapat kembali ke Ready ketika kondisi pemblokiran, pembersihan, inspeksi, atau pemeliharaannya telah diselesaikan dan verifikator berwenang secara eksplisit mencatat transaksi Ready beserta waktu bisnis, aktor, alasan opsional, dan bukti opsional. Current state yang berorientasi pada Bed kemudian diproyeksikan dari transaksi terbaru tersebut.

### 8.3 RUANG RANAP Service Execution Lifecycle

```text
Pending
  → Assigned (optional)
  → Executed
```

Untuk pekerjaan CPOE native, pembatalan oleh sumber dapat mengakhiri pekerjaan pending sebagai source-cancelled sebelum eksekusi. Koreksi kemudian dapat menandai suatu eksekusi sebagai Entered in Error. Hal-hal tersebut bukan katalog clinical outcome yang dimiliki RNA.

Untuk pekerjaan CPOE native, makna koordinasi terminal berikut berbeda satu sama lain:

- pembatalan oleh sumber mengubah pekerjaan RNA yang pending menjadi source-cancelled dan tidak membuat RNA Fulfilment Evidence;
- penetapan oleh Destination bahwa pekerjaan tidak dilakukan mencatat Not Performed Fulfilment Evidence beserta alasannya; dan
- pekerjaan yang dilakukan mencatat Service Execution Fact dan melaporkan Completed fulfilment evidence.

Makna bisnis:

| State | Makna |
|---|---|
| Pending | Ruang ranap memiliki tanggung jawab layanan yang belum terselesaikan dan belum dimulai. |
| Assigned | Performer secara opsional telah ditetapkan; hal ini tidak menyiratkan bahwa eksekusi telah dilakukan. |
| Executed | RNA mencatat Patient, RegId, Destination yang bertanggung jawab, Performer aktual, Performed At, serta deskripsi atau referensi eksekusi sebagai Service Execution Fact yang otoritatif. ServiceId mungkin masih belum terselesaikan. |
| Cancelled or Withdrawn | Kewajiban pekerjaan yang dimiliki sumber berakhir tanpa adanya RNA execution fact. |
| Entered in Error | Catatan eksekusi tersebut seharusnya tidak pernah ada sebagai catatan yang valid. |

RNA tidak menggunakan lifecycle ini untuk menetapkan kriteria penyelesaian layanan atau clinical outcome. Definisi tersebut tetap dimiliki oleh Tarif Context atau konteks pemilik lain yang disebutkan secara eksplisit.

### 8.4 Service Execution Fact Delivery Lifecycle

```text
Execution Fact Recorded
  → Completed Evidence Reported to CPOE Immediately When Ordered
  → Financial Classification Recorded
      → Non-Billable: No Financial Publication
      → Intended Billable: Tarif Resolution Pending When ServiceId Is Unavailable
          → ServiceId Resolved
          → Financial Publication Pending
          → Acknowledged by Tata Rekening or Delivery Failed
```

Ketika eksekusi dikoreksi:

```text
Execution Fact Previously Published
  → Correction Fact Recorded
  → Correction Evidence Reported to CPOE When ClinicalOrderId and OrderOccurrenceId Exist
  → CPOE Records Conflict or Reconciliation Need
  → Existing CPOE Terminal Status Is Not Automatically Rewritten
  → Financial Correction Delivery Pending/Acknowledged When Applicable
```

Delivery state merupakan integration metadata. Kegagalan resolusi Tarif atau pengiriman finansial tidak pernah membatalkan kebenaran eksekusi maupun menunda Completed evidence ke CPOE. Acknowledgement dapat menampilkan `TindakanId` yang terkait, tetapi tidak menampilkan outcome tarif atau settlement; Tata Rekening memiliki seluruh financial lifecycle berikutnya.

## 9. Domain Events

| Domain Event | Makna Bisnis |
|---|---|
| Accommodation Proposed | Akomodasi telah diidentifikasi untuk kemungkinan penetapan. |
| Accommodation Assigned | Sebuah Accommodation Allocation telah menjadi aktif. |
| Clinical Accommodation Designated | Sebuah alokasi aktif telah ditetapkan sebagai clinical accommodation pasien saat ini. |
| Accommodation Retained | Sebuah alokasi tetap aktif untuk tujuan retention atau tujuan non-primary clinical lainnya setelah lokasi klinis berubah. |
| Rooming-In Started | Primary Occupant dan Associated Occupant telah memulai pengaturan rooming-in yang sah. |
| Rooming-In Ended | Pengaturan rooming-in yang sah telah berakhir. |
| Inter-Ward Accommodation Released to Admission | RNA sumber melepaskan Clinical Accommodation untuk perpindahan antar-ward dan wajib memberi tahu Admisi. |
| Inter-Ward Reconciliation Facts Exposed | RNA mengekspos RegId yang tidak berubah, Ward sebelumnya, Ward baru, waktu efektif transfer, dan referensi pekerjaan CPOE Active yang relevan agar CPOE dapat memulai assisted reconciliation. |
| Confirmed CPOE Destination Decision Applied | RNA menerapkan keputusan rekonsiliasi CPOE yang telah dikonfirmasi pada tanggung jawab pending-work yang sesuai tanpa mengubah Clinical Order Aggregate. |
| Admission Waiting List Notification Prepared | RNA menyiapkan release fact untuk Admisi; Admisi tetap menjadi pemilik Waiting List. |
| Accommodation Released | Tujuan akomodasi aktif telah berakhir. |
| Accommodation Correction Fact Recorded | Head Nurse menambahkan koreksi yang merujuk pada Accommodation Fact yang dimiliki Ward yang sama. |
| Accommodation Correction Fact Published | RNA mengirimkan correction fact kepada downstream bounded context yang berwenang untuk rekonsiliasinya sendiri. |
| Bed Cleaning Required | Sebuah bed belum dapat ditetapkan kembali karena memerlukan pembersihan. |
| Bed Cleaning Started | Tanggung jawab pembersihan telah dimulai. |
| Bed Marked Ready | Bed telah tersedia untuk penetapan yang diizinkan. |
| Bed Blocked | Bed tidak lagi tersedia karena alasan operasional. |
| Bed Taken Out of Service | Bed tidak dapat digunakan karena alasan keselamatan, pemeliharaan, atau operasional. |
| Bed Returned to Service | Kondisi out-of-service telah diselesaikan. |
| RUANG RANAP Service Work Received | Ruang ranap telah menerima tanggung jawab layanan baru yang belum terselesaikan. |
| RUANG RANAP Service Execution Created | Sebuah RUANG RANAP Service Execution telah ditetapkan dari order, tindakan ad hoc, atau sumber lain yang diizinkan. |
| RUANG RANAP Service Assigned | Tanggung jawab eksekusi telah ditetapkan kepada aktor atau tim ruang ranap. |
| RUANG RANAP Service Executed | RNA mencatat bahwa Service yang dirujuk telah dieksekusi oleh Performer yang disebutkan pada Performed At. |
| RUANG RANAP Service Work Source-Cancelled | CPOE membatalkan Clinical Order sumber sehingga pekerjaan RNA pending yang sesuai berakhir tanpa RNA Fulfilment Evidence maupun Service Execution Fact. |
| RUANG RANAP Service Not Performed Evidence Recorded | Destination yang bertanggung jawab menetapkan bahwa satu Order Occurrence yang dirutekan tidak dilakukan serta mencatat alasan dan evidence reference yang diwajibkan. |
| RUANG RANAP Service Execution Entered in Error | Catatan eksekusi telah dinyatakan tidak valid dengan tetap mempertahankan riwayat. |
| Ad Hoc Tindakan Recorded | Tindakan ruang ranap yang tidak direncanakan atau disahkan secara mandiri telah didokumentasikan beserta dasar kewenangannya. |
| RUANG RANAP Service Execution Fact Published | Execution fact yang otoritatif telah diserahkan kepada consumer yang berwenang tanpa interpretasi billing. |
| RUANG RANAP Service Execution Fact Corrected | Koreksi berversi yang merujuk pada execution fact yang sebelumnya dipublikasikan telah dicatat. Untuk pekerjaan CPOE native, koreksi dilaporkan sebagai bukti conflict atau kebutuhan rekonsiliasi dan tidak menjanjikan penulisan ulang terminal status. |
| RUANG RANAP Execution Fact Reported to CPOE | Execution fact yang otoritatif, yang membawa ClinicalOrderId dan OrderOccurrenceId nyata, telah diserahkan kepada CPOE untuk koordinasi order. |

## 10. Workflow Bisnis

### 10.1 Standard Accommodation Assignment

```text
Admisi Waiting List Entry Visible to Ward
  → Ward Reviews Entry
  → Reject: Waiting List Remains under Admission
  → or Mandatory Bed Assignability Established
  → Accommodation Assigned
  → Waiting List Closed by Admisi as Consequence
  → Responsibility Moves to RNA
  → Clinical Accommodation Designated
  → Occupancy and Reporting Treatment Applied
```

### 10.2 Internal Accommodation Transfer

```text
Need for Bed or Room Change Identified
  → Destination Accommodation Assessed
  → New Allocation Established
  → Patient Moved
  → Prior Allocation Released or Reclassified
  → Bed Readiness Reassessed
```

### 10.3 Inter-RUANG RANAP Release to Waiting List

```text
Transfer Need Identified
  → Source Clinical Accommodation Released
  → RNA Notifies Admisi of the Release
  → Admisi Returns Patient to Its Waiting List
  → Destination Ward Reviews Ordinary Waiting List Entry
  → Destination Accommodation Assigned Through Standard Placement
  → RegId, Prior Ward, New Ward, Transfer Effective Time, and Relevant Active Work References Exposed to CPOE
  → CPOE Performs Assisted Active Order Reconciliation
  → RNA Applies a Pending-Work Destination Change Only After a Confirmed CPOE Decision
```

RNA tidak memiliki Waiting List, transfer queue, Transfer Reconciliation Aggregate, maupun keputusan rekonsiliasi. Penolakan oleh Destination Ward membuat tanggung jawab tetap berada pada Admission hingga Bed Assignment berhasil. Konten klinis antar-ward dimiliki oleh EMR. Pembatalan kembali melalui proses Admisi Waiting List yang sama. Release fact, assignment fact, dan transfer fact tidak pernah secara otomatis mengubah Destination suatu order atau membatalkan order.

### 10.4 ICU Transfer with Retained VIP Accommodation

```text
Patient Requires ICU Transfer
  → Ward Nurse or Head Nurse Establishes VIP Allocation as Retained
  → VIP Allocation Remains Active and Occupies Bed Capacity
  → ICU Accommodation Becomes Current Clinical Accommodation Through Its Owning Placement Flow
  → VIP Allocation Continues Producing Accommodation Facts
  → VIP Allocation Released at Discharge
```

### 10.5 Mother and Baby Rooming-In

```text
Mother Has Active Primary Accommodation
  → Patient Social Data Confirms Baby Medical Record References Mother Medical Record
  → Ward Nurse or Head Nurse Activates Rooming-In
  → Rooming-In Association Established
  → Baby Receives Associated Accommodation Allocation
  → Bed Holds One Primary Occupant and One Associated Occupant Without Capacity Increase
  → Mother and Baby Retain Separate Registrations and Histories
  → Rooming-In Ends When Shared Accommodation Ends
```

RNA tidak melakukan perhitungan BOR. Asumsi billing legacy saat ini adalah hanya satu room charge dan tetap didokumentasikan untuk konfirmasi downstream; asumsi tersebut bukan aturan billing RNA.

### 10.6 Accommodation Release and Bed Readiness

```text
Accommodation Purpose Ends
  → Accommodation Released
  → Bed Condition Assessed
  → Cleaning, Inspection, or Maintenance Completed When Required
  → Bed Marked Ready
```

### 10.6a Accommodation Correction

```text
Accommodation Fact Error Identified
  → Head Nurse Verifies Original Fact Belongs to the Same Ward
  → Tata Rekening Status Checked
  → Not FINALIZED
  → Original Fact Preserved Unchanged
  → Accommodation Correction Fact Appended
  → RNA Publishes Correction Fact
  → Each Downstream Bounded Context Reconciles Its Own Data
```

Jika Tata Rekening berstatus `FINALIZED`, RNA menolak koreksi biasa dan perubahan yang diminta mengikuti proses administratif di luar RNA.

### 10.7 Ordered RUANG RANAP Service Execution

```text
Clinical Order Routed to RUANG RANAP
  → RUANG RANAP Service Work Received
  → Order Occurrence Identified by OrderOccurrenceId
  → Execution Assigned or Taken
  → Service Execution Fact Recorded: Patient, RegId, ClinicalOrderId, OrderOccurrenceId, Responsible Destination, Performer, Performed At, Description or Reference
  → Completed Fulfilment Evidence Reported to CPOE Immediately
  → If Intended Billable, ServiceId Resolution Attempted
      → If Unavailable, Financial Enrichment and Publication Remain Pending
      → When Resolved, Financially Enriched Fact Published to Tata Rekening
```

### 10.8 Ad Hoc Tindakan

```text
Immediate Patient Need Identified
  → Permitted Authority Basis Determined
  → RUANG RANAP Service Performed
  → Service Execution Fact Recorded with Patient, RegId, Destination, Performer, Performed At, and Description or Reference
  → Authority Accountability Retained by RNA
  → Clinical Governance Review or Escalation When Required
  → If Intended Billable, ServiceId Resolution and Financial Publication Proceed Independently
```

Akuntabilitas Ad Hoc atau Independent Tindakan tetap sepenuhnya berada pada RNA dan Clinical Governance serta tidak dilaporkan kepada CPOE. Koordinasi CPOE hanya berlaku pada eksekusi yang telah merujuk pada `ClinicalOrderId` dan `OrderOccurrenceId` nyata.

### 10.9 RUANG RANAP Work Not Executed

```text
RUANG RANAP Service Work Is Pending
  → Source Cancellation Received from CPOE
      → Matching Pending Work Marked Source-Cancelled
      → No RNA Fulfilment Evidence Created
      → No Service Execution Fact Created
  → or Destination Determines Work Was Not Performed
      → Reason and Effective Time Recorded
      → Not Performed Fulfilment Evidence Reported to CPOE
      → No Service Execution Fact Created
```

### 10.10 Execution Correction

```text
Execution Error Identified
  → Original Execution Preserved
  → Correction or Entered in Error Recorded
  → If ClinicalOrderId and OrderOccurrenceId Both Exist, Correction Evidence Reported to CPOE
  → CPOE Records Conflict or Reconciliation Need
  → Existing CPOE Terminal Status Is Not Automatically Rewritten
  → Versioned Execution Correction Fact Published to Tata Rekening
```

RNA tidak mengasumsikan bahwa CPOE mendukung pembukaan kembali, superseding, atau pengaktifan kembali Order Occurrence yang final. RNA mempertahankan dan melaporkan koreksi otoritatifnya, sedangkan CPOE mempertahankan terminal lifecycle dan catatan rekonsiliasi yang dapat dipertanggungjawkan miliknya sendiri.

### 10.11 Legacy Order Coexistence

```text
Pre-Cutover OrderTdk Remains Read-Only History
  → Native CPOE Creates New Orders After CutoverAt
  → RNA Records Authoritative Execution Fact
  → Billable Execution Creates/Links One Tindakan by ServiceExecutionFactId
  → Non-Billable Execution Creates No Tindakan
  → Source Labels and History Preserved; Duplicate Tindakan Prevented
```

### 10.12 RUANG RANAP Execution Fact to Tata Rekening

```text
RUANG RANAP Records “Service X Was Executed by Performer Y at Time Z”
  → If Billable, RNA Publishes the Authoritative Execution Fact
  → Tindakan Owner Creates or Resolves One Tindakan by ServiceExecutionFactId
  → Tata Rekening Acknowledges with TindakanId or Reconciliation Outcome
  → If Non-Billable, No Tindakan Is Created
```
