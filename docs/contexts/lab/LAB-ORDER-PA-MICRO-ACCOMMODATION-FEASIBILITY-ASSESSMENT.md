---
Title: Feasibility Assessment Akomodasi Order Laboratorium untuk Pemeriksaan Patologi Anatomi dan Mikrobiologi
Code: LAB-ORDER-PA-MICRO-ACCOMMODATION-FEASIBILITY-ASSESSMENT
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-10-30
Status: NOT-READY
---

# 1. Request Summary

Penilaian kelayakan terhadap usulan pengembangan sistem order laboratorium (`LAB-ORDER`) pada HIS/Bilreg agar mampu mengakomodasi permintaan pemeriksaan laboratorium untuk disiplin **Patologi Anatomi (Anatomical Pathology / Lab PA)** dan **Mikrobiologi Klinik (Microbiology / Lab Micro)**, serta mengintegrasikan transmisi order tersebut ke middleware **OWARE** (`a046_oware_ordermiddlewareapi`) yang telah memiliki kapabilitas bridging hasil pemeriksaan untuk kedua disiplin tersebut.

Referenced artifacts:

- ISSUE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md)
- DOMAIN Bilreg: [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- DOMAIN PA OWARE: [LAB-PA-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/docs/LAB-PA-DOMAIN.md)
- DOMAIN Mikrobiologi OWARE: [CANONICAL-DOMAIN-SPECIFICATION-MICROBIOLOGY.md](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/docs/CANONICAL-DOMAIN-SPECIFICATION-MICROBIOLOGY.md)
- OUTCOME MyHosWeb: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- OUTCOME ARCHITECTURE: [OC-LAB-ORDER-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ARCHITECTURE.md)

## Objective

1. Mengidentifikasi kebutuhan atribut klinis dan operasional spesifik untuk pemesanan pemeriksaan Patologi Anatomi (lokasi spesimen jaringan, diagnosa klinis pengantar, tipe prosedur PA) dan Mikrobiologi (bahan biologis/spesimen, lokasi pengambilan, indikasi klinis patogen, riwayat terapi antibiotik).
2. Mengevaluasi batasan domain eksisting pada `b09-bilreg-api` (khususnya asumsi `VacutainerTypeEnum` tabung darah, validasi `LabTestDefinitionModel`, dan model `LabOrderModel` / `LabOrderItemModel`).
3. Menganalisis keselarasan kontrak dan pemetaan data pada middleware `a046_oware_ordermiddlewareapi` (`OrderTypeEnum.LabPa`, `OrderTypeEnum.LabMicro`, `OrderLabBilregBuilder`, `OrderLabHubService`, dan adapter vendor LIS terkait).
4. Mengevaluasi kesiapan antarmuka pemesanan pada Frontend `c012_myhospital_web` (`LabOrderCreateCanvas.vue`) untuk memfasilitasi seleksi disiplin pemeriksaan dan input data klinis pendukung.
5. Merumuskan opsi arsitektural pengelolaan order (apakah dipisahkan per disiplin laboratorium atau disatukan dalam satu order kolektif) beserta analisis risiko dan keuntungannya.

---

# 2. Current State

## Existing Behavior

1. **Frontend Pemesanan Lab (`c012_myhospital_web`)**:
   - Komponen pembuat order [LabOrderCreateCanvas.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue) menyediakan formulir pencarian tes laboratorium berbasis master tarif (`useTarifLabSearch`).
   - Seluruh pemeriksaan diasumsikan sebagai pemeriksaan Patologi Klinik (PK). Indikator spesimen (`tubeSummary`) menghitung dan menampilkan kebutuhan tabung vacutainer darah (misal: "1x Ungu (EDTA)", "1x Merah (Serum)").
   - Tidak tersedia input untuk:
     - Asal organ / lokasi sediaan anatomis (`SpecimenSite`) untuk jaringan biopsi/operasi.
     - Diagnosa klinis pengantar dari dokter bedah/pengirim (`ClinicalDiagnosis`).
     - Jenis prosedur PA (`ProcedureType`: histopatologi, biopsi eksisi/insisi, FNAB, sitologi cairan, potong beku / *frozen section*).
     - Detail spesimen mikrobiologi (sumber bahan biologis, indikasi kultur, riwayat penggunaan antibiotik terakhir).

2. **Backend Lab Workflow di Bilreg (`b09-bilreg-api`)**:
   - Model `LabOrderModel` dan `LabOrderItemModel` dirancang murni untuk alur kerja Patologi Klinik.
   - Entitas `LabOrderItemModel` mewajibkan `VacutainerTypeEnum TubeType` yang hanya memiliki nilai tabung darah: `Edta = 1, Serum = 2, Citrate = 3, Heparin = 4` ([VacutainerTypeEnum.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/VacutainerTypeEnum.cs#L3-L9)).
   - Komponen resolusi tarif ke tes [LabTestResolutionService.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs#L25-L80) memvalidasi bahwa setiap tes aktif harus memiliki `VacutainerType` yang terdefinisi pada enum tersebut.
   - Dokumen domain awal [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md#L629-L645) pada Bagian 39 menyatakan bahwa "*pathology workflow*" dan "*microbiology workflow*" berada di luar ruang lingkup internal LWF (LWF tidak mengelola siklus bangku kerja / *bench workflow* laboratorium PA dan biakan mikrobiologi).
   - Namun, proses pencatatan permintaan (*order intake*), pembebanan billing tindakan (`TRK-BILLING`), dan pengiriman order ke middleware OWARE tetap menjadi tanggung jawab alur order laboratorium.

3. **Middleware OWARE (`a046_oware_ordermiddlewareapi`)**:
   - **Bridging Hasil Sudah Siap**:
     - OWARE telah memiliki model kanonikal dan endpoint untuk menerima hasil PA: `LabPaResultModel` ([LabPaResultModel.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/Entities/Lab/LabPaResultModel.cs)) dan endpoint `POST /api/result/lab/pa` pada [ResultController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs#L214-L229).
     - OWARE telah memiliki model kanonikal dan endpoint untuk menerima hasil Mikrobiologi: `LabMicroResultModel` ([LabMicroResultModel.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/Entities/Lab/LabMicroResultModel.cs)) dan endpoint `POST /api/result/lab/micro` pada [ResultController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs#L231-L245).
     - OWARE telah memiliki enum tipe order yang mencakup: `OrderTypeEnum.LabPa` dan `OrderTypeEnum.LabMicro` ([OrderTypeEnum.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/ValueObjects/OrderTypeEnum.cs#L3-L6)).
     - OWARE telah memiliki adapter pengiriman order PA: [OrderLabPaBerliandoService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesLabPaBerliando/OrderLabPaBerliandoService.cs) (tipe vendor `"6"` pada `OrderLabHubService`).
   - **Keterbatasan Order Ingestion dari Bilreg**:
     - Handler `CreateOrderLabFromBilregHandler.cs` menerima order dari Bilreg dan memanggil builder `OrderLabBilregBuilder.cs`.
     - Pada `OrderLabBilregBuilder.cs` (baris 54), builder secara kaku menetapkan:
       ```csharp
       OrderType = OrderTypeEnum.Lab,
       ```
     - DTO penghubung [BilregLabOrderResponseDto.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/DTOs/BilregLabOrderResponseDto.cs) belum memuat field penanda disiplin lab (`OrderType` / `DisciplineType`), lokasi jaringan (`SpecimenSite`), diagnosa PA (`ClinicalDiagnosis`), maupun informasi spesifik mikrobiologi.
     - Akibatnya, order dari Bilreg tidak pernah diklasifikasikan sebagai `OrderTypeEnum.LabPa` maupun `OrderTypeEnum.LabMicro`.

## Existing Constraints

1. **Integritas Penagihan Biaya (Billing Integrity)**: Setiap order pemeriksaan (baik PK, PA, maupun Mikrobiologi) harus memiliki tarif yang valid (`TarifId`) dan menghasilkan pencatatan tindakan billing yang sah (`BillingTindakanId`) melalui bounded context `TRK-BILLING`.
2. **Karakteristik Spesimen yang Berbeda Signifikan**:
   - Patologi Klinik: Darah, serum, urin rutin (satuan: tabung vacutainer).
   - Patologi Anatomi: Potongan jaringan biopsi, organ eksisi, cairan serosa (satuan: pot sediaan berformalin buffer 10%, kaca preparat sitologi).
   - Mikrobiologi: Spesimen biologis steril/infeksius, botol kultur darah Bactec/BacT-ALERT, media transpor swab (satuan: wadah steril / media transpor).
   - Skema tabung vacutainer tidak dapat dipaksakan untuk merepresentasikan wadah spesimen PA dan Mikrobiologi.
3. **Pemisahan Vendor LIS Eksternal**:
   - Di banyak rumah sakit, sistem LIS untuk Patologi Klinik (misal Sysmex, HCLAB, Softmedix), Patologi Anatomi (misal Berliando, Prolims PA), dan Mikrobiologi (misal Prolims Mikro, VITEK) adalah modul terpisah atau bahkan vendor perangkat lunak yang berbeda.
   - Pengiriman order dari OWARE ke sistem LIS harus dapat diarahkan ke vendor yang tepat sesuai disiplin pemeriksaannya.
4. **Backward Compatibility**: Penyesuaian skema DTO dan model order lab tidak boleh merusak alur order Patologi Klinik yang sudah stabil dan berjalan di unit Rawat Jalan, Rawat Inap, dan IGD.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|------|------|------|
| **GAP-001** | CRITICAL | **`VacutainerTypeEnum` dan Validasi `LabTestDefinitionModel` di Bilreg Terikat Eksklusif pada Tabung Darah**: Enum `VacutainerTypeEnum` hanya mendukung `Edta`, `Serum`, `Citrate`, dan `Heparin`. Wadah spesimen untuk PA (pot formalin, kaca sediaan) dan Mikrobiologi (pot steril, botol biakan, media transpor) tidak dapat direpresentasikan secara valid di master definisi tes. |
| **GAP-002** | CRITICAL | **Model Order Bilreg (`LabOrderModel` / `LabOrderItemModel`) Belum Memiliki Atribut Klinis Patologi Anatomi**: Tidak ada field untuk menyimpan informasi krusial PA seperti asal jaringan / lokasi organ (`SpecimenSite`), diagnosa klinis pengantar (`ClinicalDiagnosis`), jenis prosedur PA (`ProcedureType`), dan penandaan onkologi/kanker pada level order maupun item. |
| **GAP-003** | MAJOR | **Model Order Bilreg Belum Mengakomodasi Parameter Spesifik Mikrobiologi**: Informasi mengenai jenis bahan/spesimen mikrobiologi, lokasi/sumber pengambilan sampel, kecurigaan patogen/indikasi biakan, serta riwayat terapi antibiotik belum dapat dicatat secara terstruktur. |
| **GAP-004** | CRITICAL | **Frontend Pemesanan Lab (`LabOrderCreateCanvas.vue`) Tidak Memiliki Seleksi Disiplin Lab dan Form Input Terstruktur**: Form order lab di frontend tidak menyediakan filter/tab pemilihan disiplin pemeriksaan (PK, PA, Mikrobiologi) dan tidak memiliki form input dinamis untuk field-field klinis wajib PA dan Mikrobiologi. |
| **GAP-005** | CRITICAL | **Builder OWARE (`OrderLabBilregBuilder.cs`) Selalu Mematok `OrderType = OrderTypeEnum.Lab`**: Middleware OWARE tidak mendeteksi apakah order dari Bilreg ditujukan untuk Patologi Anatomi (`OrderTypeEnum.LabPa`) atau Mikrobiologi (`OrderTypeEnum.LabMicro`), sehingga order tidak dapat diteruskan ke adapter LIS PA (misal `OrderLabPaBerliandoService`) atau LIS Mikrobiologi. |
| **GAP-006** | MAJOR | **Kontrak DTO Antar-Service (`BilregLabOrderResponseDto` dan Payload Outbound) Belum Sinkron**: DTO yang dikembalikan oleh endpoint Bilreg `GET api/LabContext/LabOrderFeature/{orderId}` dan dibaca oleh OWARE belum memuat properti klasifikasi disiplin lab dan atribut klinis PA/Mikro. |

---

# 4. Open Questions

| ID | Question | Impact |
|------|------|------|
| **OQ-001** | **Strategi Pemisahan Order vs Order Campuran**: Apakah order untuk pemeriksaan Patologi Anatomi dan Mikrobiologi wajib dibuat sebagai order transaksi terpisah dari Patologi Klinik (1 Order = 1 Disiplin Lab), ataukah diperbolehkan dalam 1 order campuran (*mixed*)? | Menentukan apakah perutean ke LIS vendor di OWARE dilakukan berbasis level Order (`OrderModel.OrderType`) atau memerlukan logika pemecahan order (*order splitting*) yang rumit jika item dalam 1 order memiliki tujuan vendor berbeda. |
| **OQ-002** | **Kardinalitas Sediaan Jaringan PA**: Pada pemeriksaan Patologi Anatomi, apakah satu item tindakan (misal: Histopatologi Jaringan Sedang) hanya mewakili 1 lokasi sediaan (`SpecimenSite`), ataukah klinisi dapat memesan 1 tindakan dengan beberapa botol sediaan terpisah (misal Botol A: Mamma Dextra, Botol B: KGB Aksila)? | Menentukan struktur pemodelan atribut: apakah cukup berupa string field pada item order, atau memerlukan relasi child koleksi sediaan (*SpecimenContainer / SpecimenList*). |
| **OQ-003** | **Penentuan Disiplin Lab di Master Data**: Apakah disiplin pemeriksaan (PK, PA, Mikrobiologi) ditentukan secara otomatis dari master data tes / kelompok tarif di Bilreg, atau dipilih manual oleh user di form frontend? | Mempengaruhi kebutuhan penambahan kolom/properti klasifikasi disiplin pada `LabTestDefinitionModel` atau master tarif, serta otomatisasi validasi form di frontend. |
| **OQ-004** | **Input Mikrobiologi: Terstruktur vs Naratif**: Apakah data riwayat antibiotik dan kecurigaan mikroorganisme perlu dibuatkan field input form terstruktur tersendiri, atau cukup distandarisasikan format penulisannya di dalam field `ClinicalNote` / catatan klinis? | Menentukan kompleksitas UI form pemesanan dan validasi schema pada frontend `c012_myhospital_web`. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| **ASM-001** | Setiap tindakan pemeriksaan Patologi Anatomi dan Mikrobiologi telah memiliki kode tarif yang sah di master tarif rumah sakit (`ta_tarif` / `TRK-TARIF`). |
| **ASM-002** | Alur finansial dan pembebanan billing tindakan (`TRK-BILLING`) untuk pemeriksaan PA dan Mikrobiologi menggunakan mekanisme yang sama dengan Patologi Klinik (melalui `LabOrderChargeCmd` yang menghasilkan `BillingTindakanId`). |
| **ASM-003** | Kapabilitas bridging penerimaan hasil (*result ingestion*) untuk Patologi Anatomi (`LabPaResultModel`) dan Mikrobiologi (`LabMicroResultModel`) di OWARE sudah berfungsi dengan benar dan tidak memerlukan perombakan arsitektur hasil. |
| **ASM-004** | Laboratorium eksternal / vendor LIS untuk PA (misal Berliando) dan Mikrobiologi (misal Prolims) mengharapkan data pemesanan dikirim dengan tipe order yang sesuai (`LabPa` atau `LabMicro`). |
| **ASM-005** | Rumah sakit memproses operasional teknis PA dan Mikrobiologi pada meja kerja / instalasi laboratorium yang terpisah dari laboratorium Patologi Klinik rutin. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| **RISK-001** | **Kebingungan Routing Vendor LIS pada Order Campuran**: Jika klinisi memesan darah rutin (PK) dan biopsi jaringan (PA) dalam satu nomor order yang sama, OWARE akan kesulitan menentukan vendor LIS penerima karena `OrderLabModel` hanya memiliki satu `OrderType`. | HIGH | Menerapkan kebijakan arsitektur **Pemisahan Order per Disiplin Laboratorium**: sistem mewajibkan satu nomor order hanya berisi pemeriksaan dalam disiplin yang sama (PK, PA, atau Mikrobiologi). |
| **RISK-002** | **Penolakan Sediaan oleh Laboratorium PA Akibat Data Jaringan Tidak Lengkap**: Jika klinisi tidak mengisi asal jaringan (`SpecimenSite`) atau diagnosa klinis pengantar saat memesan pemeriksaan histopatologi, dokter Sp.PA dan teknisi lab tidak dapat memproses sediaan. | HIGH | Menerapkan validasi wajib (*mandatory*) di frontend dan backend: field `SpecimenSite` dan `ClinicalDiagnosis` wajib diisi jika order bertipe Patologi Anatomi. |
| **RISK-003** | **Breaking Change pada Schema Master Tes (`VacutainerTypeEnum`)**: Mengubah atau menghapus nilai pada `VacutainerTypeEnum` yang sudah digunakan oleh tes-tes PK eksisting dapat merusak data tersimpan dan query yang ada. | MEDIUM | Memperluas enum secara backward-compatible dengan menambahkan tipe wadah baru (misal `FormalinPot = 10`, `SterileContainer = 11`, `TransportMedium = 12`, `NonContainer = 99`) tanpa mengubah nilai 1 s/d 4 yang sudah ada. |
| **RISK-004** | **Beban Kerja Penginputan Dokter**: Menambahkan terlalu banyak field wajib pada form order dapat memperlambat alur kerja klinisi di poli atau ruangan ranap. | MEDIUM | Mendesain formulir secara kontekstual: field khusus PA (asal organ, prosedur PA) hanya muncul saat user memilih pemeriksaan PA; untuk pemeriksaan PK rutin, form tetap ringkas seperti semula. |

---

# 7. Recommendations

## Option A: Pemisahan Order Berdasarkan Disiplin Laboratorium (1 Order = 1 Disiplin Lab) — (RECOMMENDED)

Pada opsi ini, sistem membagi alur pemesanan ke dalam 3 kategori disiplin laboratorium:
1. **Patologi Klinik (PK)**: Order lab standar (darah, urin, cairan tubuh rutin berbasis tabung vacutainer).
2. **Patologi Anatomi (PA)**: Order pemeriksaan jaringan dan sel (histopatologi, sitologi, FNAB, potong beku) yang mewajibkan input lokasi jaringan (`SpecimenSite`), diagnosa pengantar (`ClinicalDiagnosis`), dan jenis prosedur (`ProcedureType`).
3. **Mikrobiologi (Micro)**: Order kultur kuman, resistensi antibiotik, dan mikroskopis khusus yang mewajibkan input jenis bahan biologis, lokasi pengambilan, dan informasi kecurigaan klinis.

Setiap order yang diterbitkan memiliki satu `DisciplineType` yang tegas pada entitas `LabOrderModel` di Bilreg, yang secara direct dipetakan ke `OrderTypeEnum` di OWARE (`Lab`, `LabPa`, atau `LabMicro`).

### Advantages

- **Pemisahan Bounded Context yang Bersih**: Selaras dengan kenyataan operasional rumah sakit di mana instalasi Patologi Anatomi dan Mikrobiologi memiliki alur penerimaan sediaan dan dokter penanggung jawab spesialis yang berbeda (Sp.PK vs Sp.PA vs Sp.MK).
- **Perutean OWARE Sangat Sederhana & Andal**: OWARE dapat langsung memetakan `OrderTypeEnum.LabPa` ke adapter vendor PA (misal Berliando) dan `OrderTypeEnum.LabMicro` ke adapter Mikrobiologi tanpa perlu mekanisme *order splitting* yang rentan desinkronisasi.
- **Siklus Hidup dan Status Hasil Independen**: Pemeriksaan PK umumnya selesai dalam hitungan jam, sedangkan PA dan kultur Mikrobiologi membutuhkan waktu 3 hingga 7 hari kerja. Pemisahan order mencegah status order PK tertahan oleh proses kultur yang memakan waktu lama.
- **Validasi UI Kontekstual**: Form pemesanan di frontend menyajikan field input yang relevan sesuai disiplin yang dipilih tanpa mengotori alur pemesanan darah rutin PK.

### Disadvantages

- Klinisi yang ingin meminta pemeriksaan darah rutin (PK) dan biopsi jaringan (PA) pada satu pasien dalam satu waktu harus membuat dua transaksi order terpisah. (Namun hal ini merupakan praktik standar di hampir seluruh rumah sakit besar).

---

## Option B: Unified Order (Satu Order Kolektif Campuran dengan Item-Level Routing)

Pada opsi ini, klinisi dapat menggabungkan pemeriksaan PK, PA, dan Mikrobiologi dalam satu nomor order yang sama. Atribut spesifik seperti lokasi jaringan dan jenis bahan disimpan sebagai metadata per baris item pemeriksaan (`LabOrderItemModel`). Middleware OWARE atau Bilreg bertanggung jawab untuk memecah (*splitting*) item order dan mengirimkannya ke masing-masing vendor LIS.

### Advantages

- Klinisi hanya perlu melakukan proses check-out/submit pemesanan satu kali untuk seluruh kebutuhan laboratorium pasien.

### Disadvantages

- **Kompleksitas Perutean dan Pemecahan Order di OWARE Sangat Tinggi**: OWARE harus membuat sub-order virtual atau membagi payload pengiriman ke beberapa vendor LIS, yang dapat menimbulkan inkonsistensi saat satu vendor berhasil menerima order sedangkan vendor lain gagal (*partial failure*).
- **Pengelolaan Status Order Menjadi Rumpun Masalah**: Bagaimana menentukan status `LabOrderStatus` pada Bilreg jika hasil PK sudah rilis (`Released`) sementara hasil kultur Mikrobiologi masih dalam tahap inkubasi hari ke-2 (`InProgress`)?
- **Desinkronisasi Tindakan Billing**: Pembebanan biaya tindakan (`BillingTindakanId`) yang disatukan akan menyulitkan pembatalan parsial jika salah satu pemeriksaan (misal PA) dibatalkan oleh DPJP sementara pemeriksaan PK sudah selesai dikerjakan.

---

# 8. Gap Closure

Resolusi terhadap kesenjangan (GAP) dan pertanyaan terbuka (OQ) diuraikan di bawah ini:

## GAP-001: Model `VacutainerTypeEnum` dan Validasi `LabTestDefinitionModel`

### Decision
Memperluas enum representasi wadah spesimen di Bilreg dari `VacutainerTypeEnum` menjadi wadah spesimen komprehensif, atau menambahkan nilai enum baru secara backward-compatible:
- Nilai eksisting dipertahankan: `Edta = 1`, `Serum = 2`, `Citrate = 3`, `Heparin = 4`.
- Penambahan nilai baru: `PotFormalin = 10` (untuk sediaan PA), `PotSteril = 11` (untuk sputum/urin kultur/cairan), `TransportMedium = 12` (untuk swab kuman/Amies/Stuart), `KacaPreparat = 13` (untuk sitologi/FNAB), `TanpaWadahKhusus = 99`.

### Rationale
Menghindari kegagalan validasi pada `LabTestResolutionService` saat mendaftarkan master definisi tes untuk tindakan PA dan Mikrobiologi, tanpa merusak data dan kode eksisting yang menggunakan tabung vacutainer darah.

### Impact
Perubahan minor pada enum `VacutainerTypeEnum` di `Bilreg.Domain.LabContext.LabOrderFeature` dan tabel database terkait.

### Architecture Impact
Architect perlu memformalkan apakah enum tetap bernama `VacutainerTypeEnum` dengan penambahan item, atau dialiaskan menjadi `SpecimenContainerTypeEnum`.

---

## GAP-002 & GAP-003: Atribut Spesifik PA dan Mikrobiologi pada Model Order

### Decision
Menambahkan struktur data metadata klinis pada `LabOrderModel` (jika mengadopsi Option A) atau pada `LabOrderItemModel`:
- Untuk Patologi Anatomi:
  - `SpecimenSite` (string, wajib untuk order PA): Lokasi anatomis / asal organ jaringan.
  - `ClinicalDiagnosis` (string, wajib untuk order PA): Diagnosa klinis pengantar dokter.
  - `ProcedureType` (string / enum opsional): Jenis tindakan PA (Histopatologi, Sitologi, FNAB, Potong Beku).
- Untuk Mikrobiologi:
  - `Specimen` / `MaterialType` (string, wajib untuk order Micro): Jenis bahan biologis.
  - `AnatomicSource` (string, opsional): Lokasi pengambilan sampel mikrobiologi.
  - `ClinicalIndication` (string, opsional): Kecurigaan infeksi/patogen.
  - `PriorAntibiotics` (string, opsional): Riwayat penggunaan antibiotik terakhir.

### Rationale
Parameter-parameter ini merupakan data klinis primer yang diwajibkan oleh dokter Sp.PA dan Sp.MK serta sistem LIS eksternal (seperti Berliando dan Prolims) untuk memproses spesimen secara akurat dan bermakna.

### Impact
Penambahan kolom/tipe data pada tabel order lab di Bilreg, DTO query respons, dan DTO integrasi OWARE.

### Architecture Impact
Mendefinisikan apakah atribut ini disimpan sebagai value object terpisah (misal `PaOrderMetadataType`, `MicroOrderMetadataType`) atau field langsung pada entity.

---

## GAP-004: Seleksi Disiplin Lab dan Form Input Terstruktur di Frontend

### Decision
Memperbarui [LabOrderCreateCanvas.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue) dengan menyediakan:
1. Pilihan Tab / Selector Disiplin Lab di bagian atas pemilihan tes: `Patologi Klinik (PK)` | `Patologi Anatomi (PA)` | `Mikrobiologi (Micro)`.
2. Menampilkan field input kontekstual saat tab `Patologi Anatomi` aktif (`SpecimenSite`, `ProcedureType`, `ClinicalDiagnosis`).
3. Menampilkan field input kontekstual saat tab `Mikrobiologi` aktif (`SpecimenType`, `AnatomicSource`, `PriorAntibiotics`).
4. Menyesuaikan indikator `tubeSummary` agar tidak menampilkan kalkulasi tabung darah jika order yang dibuat adalah PA (menampilkan jumlah pot/wadah jaringan) atau Mikrobiologi (menampilkan wadah spesimen).

### Rationale
Memberikan pengalaman pengguna yang intuitif, mencegah kesalahan input klinis, dan memastikan data wajib terisi sebelum order disubmit ke backend.

### Impact
Perubahan pada komponen Vue 3 di `c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue` dan schema terkait di `lab-order.schema.ts`.

### Architecture Impact
Architect perlu memvalidasi keselarasan desain interaksi antarmuka dengan pedoman desain HIS Web.

---

## GAP-005 & GAP-006: Pemetaan OrderType dan Sinkronisasi DTO di OWARE

### Decision
1. Memperbarui `BilregLabOrderResponseDto` pada OWARE agar memuat properti `OrderType` / `DisciplineType`, `SpecimenSite`, `ClinicalDiagnosis`, `ProcedureType`, dan informasi spesimen mikrobiologi.
2. Memperbarui [OrderLabBilregBuilder.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Workers/Creators/OrderLabBilregBuilder.cs) agar memetakan `OrderType` secara dinamis:
   ```csharp
   OrderType = dto.DisciplineType switch
   {
       1 => OrderTypeEnum.Lab,      // Clinical Pathology (PK)
       2 => OrderTypeEnum.LabPa,    // Anatomical Pathology (PA)
       3 => OrderTypeEnum.LabMicro, // Microbiology
       _ => OrderTypeEnum.Lab
   };
   ```
3. Memetakan properti PA ke dalam `OrderLabModel` dan `LisOrderDto` pada [OrderLabPaBerliandoService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesLabPaBerliando/OrderLabPaBerliandoService.cs).

### Rationale
Menghilangkan hardcoding `OrderTypeEnum.Lab` sehingga OWARE dapat meneruskan order PA ke adapter LIS PA dan order Micro ke adapter LIS Mikrobiologi.

### Impact
Perubahan pada proyek `Oware.Application` dan `Oware.Infrastructure`.

### Architecture Impact
Sinkronisasi kontrak DTO antar-service antara repo `b09-bilreg-api` dan `a046_oware_ordermiddlewareapi`.

---

# 9. Architecture Applicability

Record the Architecture Applicability decision:

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Perubahan ini melibatkan:
1. Modifikasi kontrak DTO lintas-service antara Core Backend (`b09-bilreg-api`) dan Middleware Bridge (`a046_oware_ordermiddlewareapi`).
2. Perubahan model domain dan skema database pada bounded context Lab (`LabOrderModel`, `LabOrderItemModel`, enum tipe wadah spesimen).
3. Penyesuaian arsitektural antarmuka pemesanan pada frontend (`c012_myhospital_web`).
4. Penetapan keputusan arsitektural terkait kebijakan pemisahan order per disiplin laboratorium vs order campuran (*unified vs segregated order*).

Oleh karena itu, diperlukan penyusunan dokumen **ARCHITECTURE** formal sebelum implementasi dapat direncanakan dan dieksekusi.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps identified and assessed
- [x] Clear recommendations formulated (Option A vs Option B)
- [x] Preliminary gap closure decisions recorded from analytical perspective
- [ ] Architecture Applicability confirmed by Architect
- [ ] Target Architecture finalized and approved by Architect
- [ ] Open questions resolved by domain owner & Architect

## Status

**NOT-READY**

## Notes

Status ditetapkan sebagai **NOT-READY** karena dokumen ini berada pada tahap analisis (*Analyst analysis activity*). Berdasarkan tata kelola SDLC, hak untuk memberikan status **READY-FOR-PLANNING** berada secara eksklusif pada peran **Architect** (`ica-architect`) setelah meninjau penilaian kelayakan ini dan memformalkan dokumen Target Architecture.

---

# 11. References

Referenced artifacts:

- ISSUE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md)
- DOMAIN Bilreg: [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- DOMAIN PA OWARE: [LAB-PA-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/docs/LAB-PA-DOMAIN.md)
- DOMAIN Mikrobiologi OWARE: [CANONICAL-DOMAIN-SPECIFICATION-MICROBIOLOGY.md](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/docs/CANONICAL-DOMAIN-SPECIFICATION-MICROBIOLOGY.md)
- OUTCOME: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)

Referenced codebase locations:

- Frontend Canvas Pemesanan Lab: [LabOrderCreateCanvas.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue)
- Frontend Lab Order Schema: [lab-order.schema.ts](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/types/lab-order.schema.ts)
- Bilreg Lab Order Aggregate: [LabOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs)
- Bilreg Lab Order Item: [LabOrderItemModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemModel.cs)
- Bilreg Vacutainer Enum: [VacutainerTypeEnum.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/VacutainerTypeEnum.cs)
- Bilreg Lab Test Resolution: [LabTestResolutionService.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs)
- OWARE Order Type Enum: [OrderTypeEnum.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/ValueObjects/OrderTypeEnum.cs)
- OWARE Order Model: [OrderModel.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/Entities/OrderModel.cs) & [OrderLabModel.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/Entities/Lab/OrderLabModel.cs)
- OWARE Bilreg Order Builder: [OrderLabBilregBuilder.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Workers/Creators/OrderLabBilregBuilder.cs)
- OWARE LIS Order Hub: [OrderLabHubService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/OrderLabHubService.cs)
- OWARE LIS PA Berliando Adapter: [OrderLabPaBerliandoService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesLabPaBerliando/OrderLabPaBerliandoService.cs)
- OWARE Bilreg Response DTO: [BilregLabOrderResponseDto.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/DTOs/BilregLabOrderResponseDto.cs)
- OWARE Result Controller: [ResultController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs)
