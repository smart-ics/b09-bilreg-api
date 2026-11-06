---
Title: Feasibility Assessment Akomodasi Order Laboratorium untuk Pemeriksaan Patologi Anatomi dan Mikrobiologi
Code: LAB-ORDER-PA-MICRO-ACCOMMODATION-FEASIBILITY-ASSESSMENT
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.3
LastUpdated: 2026-11-04
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap usulan pengembangan sistem order laboratorium (`LAB-ORDER`) pada HIS/Bilreg agar mampu mengakomodasi permintaan pemeriksaan laboratorium untuk disiplin **Patologi Anatomi (Anatomical Pathology / Lab PA)** dan **Mikrobiologi Klinik (Microbiology / Lab Micro)**, serta mengintegrasikan transmisi order tersebut ke middleware **OWARE** (`a046_oware_ordermiddlewareapi`) yang telah memiliki kapabilitas bridging hasil pemeriksaan untuk kedua disiplin tersebut.

Referenced artifacts:

- ISSUE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md)
- ARCHITECTURE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md)
- IMPLEMENTATION PLAN: [LAB-ORDER-PA-MICRO-ACCOMMODATION-IMPLEMENTATION-PLAN.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-IMPLEMENTATION-PLAN.md)
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

| ID | Question | Impact | Status |
|------|------|------|------|
| **OQ-001** | **Strategi Pemisahan Order vs Order Campuran**: Apakah order untuk pemeriksaan Patologi Anatomi dan Mikrobiologi wajib dibuat sebagai order transaksi terpisah dari Patologi Klinik (1 Order = 1 Disiplin Lab), ataukah diperbolehkan dalam 1 order campuran (*mixed*)? | Menentukan apakah perutean ke LIS vendor di OWARE dilakukan berbasis level Order (`OrderModel.OrderType`) atau memerlukan logika pemecahan order (*order splitting*) yang rumit jika item dalam 1 order memiliki tujuan vendor berbeda. | **CLOSED** |
| **OQ-002** | **Kardinalitas Sediaan Jaringan PA**: Pada pemeriksaan Patologi Anatomi, apakah satu item tindakan (misal: Histopatologi Jaringan Sedang) hanya mewakili 1 lokasi sediaan (`SpecimenSite`), ataukah klinisi dapat memesan 1 tindakan dengan beberapa botol sediaan terpisah (misal Botol A: Mamma Dextra, Botol B: KGB Aksila)? | Menentukan struktur pemodelan atribut: apakah cukup berupa string field pada item order, atau memerlukan relasi child koleksi sediaan (*SpecimenContainer / SpecimenList*). | **CLOSED** |
| **OQ-003** | **Penentuan Disiplin Lab di Master Data**: Apakah disiplin pemeriksaan (PK, PA, Mikrobiologi) ditentukan secara otomatis dari master data tes / kelompok tarif di Bilreg, atau dipilih manual oleh user di form frontend? | Mempengaruhi kebutuhan penambahan kolom/properti klasifikasi disiplin pada `LabTestDefinitionModel` atau master tarif, serta otomatisasi validasi form di frontend. | **CLOSED** |
| **OQ-004** | **Input Mikrobiologi: Terstruktur vs Naratif**: Apakah data riwayat antibiotik dan kecurigaan mikroorganisme perlu dibuatkan field input form terstruktur tersendiri, atau cukup distandarisasikan format penulisannya di dalam field `ClinicalNote` / catatan klinis? | Menentukan kompleksitas UI form pemesanan dan validasi schema pada frontend `c012_myhospital_web`. | **CLOSED** |

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
| **RISK-003** | **Breaking Change pada Schema Master Tes (`VacutainerTypeEnum`)**: Mengubah atau menghapus nilai pada `VacutainerTypeEnum` yang sudah digunakan oleh tes-tes PK eksisting dapat merusak data tersimpan dan query yang ada. | MEDIUM | Memperluas enum secara backward-compatible dengan menambahkan tipe wadah baru (misal `PotFormalin = 10`, `PotSteril = 11`, `TransportMedium = 12`, `KacaPreparat = 13`, `BotolKulturDarah = 14`, `TanpaWadahKhusus = 99`) tanpa mengubah nilai 1 s/d 4 yang sudah ada. |
| **RISK-004** | **Beban Kerja Penginputan Dokter**: Menambahkan terlalu banyak field wajib pada form order dapat memperlambat alur kerja klinisi di poli atau ruangan ranap. | MEDIUM | Mendesain formulir secara kontekstual: field khusus PA (asal organ, prosedur PA) hanya muncul saat user memilih pemeriksaan PA; untuk pemeriksaan PK rutin, form tetap ringkas seperti semula. |

---

# 7. Recommendations

## Option A: Pemisahan Order Berdasarkan Disiplin Laboratorium (1 Order = 1 Disiplin Lab) — (APPROVED)

Sistem menetapkan kebijakan bahwa satu transaksi order laboratorium strictly memuat pemeriksaan dari satu `LaboratoryServiceType` yang sama.

1. **Patologi Klinik (PK)**: Order lab standar (darah, urin, cairan tubuh rutin berbasis tabung vacutainer).
2. **Patologi Anatomi (PA)**: Order pemeriksaan jaringan dan sel (histopatologi, sitologi, FNAB, potong beku) yang mewajibkan input lokasi jaringan (`SpecimenSite`), diagnosa pengantar (`ClinicalDiagnosis`), dan jenis prosedur (`ProcedureType`).
3. **Mikrobiologi (Micro)**: Order kultur kuman, resistensi antibiotik, dan mikroskopis khusus yang mewajibkan input jenis bahan biologis, lokasi pengambilan, dan informasi kecurigaan klinis.

Setiap order yang diterbitkan memiliki satu `ServiceType` yang tegas pada entitas `LabOrderModel` di Bilreg, yang secara direct dipetakan 1:1 ke `OrderTypeEnum` di OWARE (`Lab`, `LabPa`, atau `LabMicro`).

### Advantages

- **Pemisahan Bounded Context yang Bersih**: Selaras dengan kenyataan operasional rumah sakit di mana instalasi Patologi Anatomi dan Mikrobiologi memiliki alur penerimaan sediaan dan dokter penanggung jawab spesialis yang berbeda (Sp.PK vs Sp.PA vs Sp.MK).
- **Perutean OWARE Sangat Sederhana & Andal**: OWARE dapat langsung memetakan `OrderTypeEnum.LabPa` ke adapter vendor PA (misal Berliando) dan `OrderTypeEnum.LabMicro` ke adapter Mikrobiologi tanpa perlu mekanisme *order splitting* yang rentan desinkronisasi.
- **Siklus Hidup dan Status Hasil Independen**: Pemeriksaan PK umumnya selesai dalam hitungan jam, sedangkan PA dan kultur Mikrobiologi membutuhkan waktu 3 hingga 7 hari kerja. Pemisahan order mencegah status order PK tertahan oleh proses kultur yang memakan waktu lama.
- **Validasi UI Kontekstual**: Form pemesanan di frontend menyajikan field input yang relevan sesuai disiplin yang dipilih tanpa mengotori alur pemesanan darah rutin PK.

---

# 8. Gap Closure

Semua kesenjangan (GAP) dan pertanyaan terbuka (OQ) telah diputuskan dan ditutup secara resmi bersama Domain Owner:

## GAP-001: Model `VacutainerTypeEnum` dan Validasi `LabTestDefinitionModel`

### Status: CLOSED

### Decision
Menyederhanakan penanganan spesimen non-darah dengan menambahkan nilai `None = 0` pada `VacutainerTypeEnum`:
- Nilai eksisting tabung vacutainer darah tetap utuh: `Edta = 1`, `Serum = 2`, `Citrate = 3`, `Heparin = 4`.
- Nilai baru: `None = 0` (Tanpa Tabung Vacutainer / Non-Blood Sample).
- Aturan Operasional:
  - Untuk sampel non-darah (jaringan sediaan PA, kultur/swab mikrobiologi, urin tampung, feses), `VacutainerType` bernilai `None (0)` dan `RequiredTubeCount = 0`.
  - Spesifikasi jenis bahan biologis tetap dijelaskan secara tekstual melalui properti `SpecimenType` (misal: "Jaringan", "Sputum", "Pus", "Urine", "Swab Tenggorok").
  - Frontend UI ([LabOrderCreateCanvas.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue)) dan [lab-formatters.ts](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/lib/lab-formatters.ts) tidak menampilkan kalkulasi tabung darah jika `vacutainerType === 0`.

### Rationale
Menghilangkan kompleksitas penambahan daftar enum wadah non-darah yang berlebihan (pot formalin, pot steril, kaca preparat, dsb.). Cukup membedakan secara biner apakah tes membutuhkan tabung vacutainer darah (nilai 1–4) atau tidak membutuhkan tabung vacutainer (`None = 0`).

### Impact
Perubahan minimal: hanya menambahkan `None = 0` pada `VacutainerTypeEnum` di `Bilreg.Domain.LabContext.LabOrderFeature`. Database dan logika eksisting kompatibel 100%.

### Architecture Impact
`VacutainerTypeEnum` mempertahankan namanya dan tetap berupa enum integer ringkas (0 s/d 4).

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-04

---

## GAP-002: Atribut Spesifik Patologi Anatomi (PA) pada Model Order Item Dihilangkan (Handled at Result Entry & LIS)

### Status: CLOSED - DROPPED / HANDLED AT RESULT ENTRY & LIS

### Decision
Atribut spesifik PA (`SpecimenSite`, `ProcedureType`, `ClinicalDiagnosis`) **TIDAK ditambahkan pada `LabOrderItemModel`**:
- Informasi mengenai lokasi jaringan sediaan (`SpecimenSite`), jenis prosedur PA (`ProcedureType`), nomor sediaan laboratorium PA (`CaseNumber`), dan diagnosis klinis PA sepenuhnya ditangani di **sistem LIS eksternal** atau pada saat **perekaman hasil pemeriksaan (Result Entry)**, di mana model [`LabPaResultModel.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/Entities/Lab/LabPaResultModel.cs) sudah memiliki atribut-atribut tersebut secara lengkap.
- Pada tahap pembuatan order di HIS, klinisi cukup memesan item tindakan tarif PA yang dimaksud. Jika ada catatan atau indikasi medis awal dari dokter pengirim, dapat dicatat pada field `ClinicalNote` umum yang sudah tersedia di level order header.

### Rationale
Menyederhanakan alur kerja penginputan order bagi klinisi di form order, menghindari redundansi data antara order dan laporan hasil, serta memanfaatkan fakta bahwa sistem LIS PA dan model hasil `LabPaResultModel` memang sudah menjadi tempat yang tepat secara klinis untuk mendokumentasikan rincian sediaan makroskopis, mikroskopis, dan lokasi jaringan.

### Impact
**Zero schema changes** pada tabel `BILRG_LabOrderItem` untuk atribut PA. Form pemesanan lab di frontend tidak perlu menambahkan kolom-kolom input kompleks per item.

### Architecture Impact
`LabOrderItemModel` tetap ramping dan tidak dibebani field-field naratif sediaan.

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-04

---

## GAP-003: Atribut Spesifik Mikrobiologi (Micro) pada Model Order

### Status: CLOSED - DISIMPLIFIKASI (HANDLED VIA CLINICALNOTE & SPECIMENTYPE)

### Decision
Menyederhanakan penanganan atribut mikrobiologi:
1. **Tidak menambahkan kolom baru pada `LabOrderModel` (`PriorAntibiotics`, `ClinicalIndication`)**: Informasi mengenai riwayat terapi antimikroba/antibiotik terakhir, kecurigaan patogen/infeksi, atau catatan klinis lainnya ditangani langsung melalui properti `ClinicalNote` yang sudah tersedia pada header `LabOrderModel`.
2. **Tidak menambahkan `AnatomicSource` pada `LabOrderItemModel`**: Deskripsi bahan biologis / spesimen mikrobiologi (Sputum, Urin porsi tengah, Darah, Pus, Swab luka, LCS) tetap diakomodasi melalui properti `SpecimenType` yang sudah ada, atau didokumentasikan di LIS dan pada saat perekaman hasil (`LabMicroResultModel`).
3. **Form Pemesanan Frontend**: Tidak memunculkan field input kondisional tambahan untuk mikrobiologi. Klinisi cukup menuliskan catatan klinis yang relevan pada kolom `ClinicalNote` umum yang sudah ada.

### Rationale
Menghilangkan kompleksitas form pemesanan dan skema database, menghindari kolom-kolom parsial yang jarang diisi lengkap oleh klinisi, serta memaksimalkan penggunaan kolom catatan klinis `ClinicalNote` yang sudah ada.

### Impact
Zero schema changes untuk atribut mikro pada `BILRG_LabOrder` dan `BILRG_LabOrderItem`. Form order frontend tetap bersih tanpa komponen input kondisional.

### Architecture Impact
`LabOrderModel` dan `LabOrderItemModel` tidak dibebani field spesifik mikrobiologi.

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-04

---

## GAP-004: Seleksi Disiplin Lab dan Form Input Terstruktur di Frontend

### Status: CLOSED

### Decision
Menerapkan **Form Pemesanan Tunggal Terpadu (Single Cart) Tanpa Multi-Tab**:
1. Form pemesanan di frontend ([LabOrderCreateCanvas.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue)) **tidak menggunakan multi-tab** yang merepotkan klinisi.
2. Dokter/perawat menggunakan satu kotak pencarian tes umum untuk memilih pemeriksaan apapun (baik PK, PA, maupun Micro) dalam satu keranjang belanja yang sama.
3. Seluruh klasifikasi layanan (`ServiceType`) ditangani otomatis oleh sistem berdasarkan master data [`LabTestDefinitionModel`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabTestDefinitionFeature/LabTestDefinitionModel.cs).
4. **Fokus Pembedaan Utama Dialihkan ke Pengelolaan Hasil (Result Management)**: Pembedaan antarmuka form input secara hakiki terjadi pada saat entry hasil laboratorium (Form tabular analit PK, Form laporan naratif PA, dan Form biakan/AST Mikrobiologi).

### Rationale
Memberikan pengalaman pemesanan yang paling ringkas, cepat, dan nyaman bagi dokter/perawat di rawat jalan maupun rawat inap, sementara pembedaan teknis spesialisasi laboratorium ditangani pada meja kerja penginputan hasil.

### Impact
Komponen form order tetap sederhana dan tidak memerlukan tab switching. Form perekaman hasil di frontend mengadaptasi layout form berdasarkan `ServiceType` dari order yang dibuka.

### Architecture Impact
Frontend menyajikan form order terpadu; backend use-case menangani penyimpanan order secara transparan.

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-04

---

## GAP-005 & GAP-006: Pemetaan OrderType dan DTO di OWARE Diterima As-Is (Zero Changes to OWARE)

### Status: CLOSED - ACCEPTED AS-IS

### Decision
Menerima implementasi middleware OWARE (`a046_oware_ordermiddlewareapi`) **As-Is sepenuhnya**:
1. OWARE tetap menggunakan `OrderType = OrderTypeEnum.Lab` pada [OrderLabBilregBuilder.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Workers/Creators/OrderLabBilregBuilder.cs).
2. Kontrak DTO [BilregLabOrderResponseDto.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/DTOs/BilregLabOrderResponseDto.cs) tidak perlu diubah.
3. Mekanisme perutean order ke vendor LIS (baik vendor PK Sysmex/HCLAB, vendor PA Berliando, maupun vendor Micro Prolims) sudah berjalan dengan andal melalui [OrderLabHubService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/OrderLabHubService.cs) berbasis konfigurasi `MapTarifLab` (`ConsId`) dan `ConsumerModel.OrderType` (misal: vendor `"6"` untuk Berliando PA, `"4"` atau `"11"` untuk Prolims Micro).

### Rationale
OWARE tidak membutuhkan modifikasi enum atau diskriminator tipe order baru karena keputusan perutean vendor LIS di OWARE didasarkan pada mapping tarif ke vendor eksternal (`ConsId`), bukan pada properti `OrderModel.OrderType`. Menerima OWARE as-is meminimalkan risiko regresi pada middleware integrasi yang sudah stabil.

### Impact
**Zero code changes** pada repositori `a046_oware_ordermiddlewareapi`. Seluruh penyesuaian fungsional dan klasifikasi terisolasi secara bersih pada backend `b09-bilreg-api` dan frontend `c012_myhospital_web`.

### Architecture Impact
Middleware OWARE tidak memerlukan perubahan arsitektural.

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-03

---

## OQ-001: Strategi Pemisahan Order vs Order Campuran

### Status: CLOSED

### Decision
**Satu Keranjang Terpadu di Form Order dengan Auto-Split Transparan Berbasis Master Data**:
- Pengguna di frontend memesan melalui satu form order terpadu tanpa batasan multi-tab.
- Saat disimpan, sistem backend secara otomatis memecah (*auto-split*) item berdasarkan `ServiceType` dari master data menjadi nomor order resmi per layanan (`LBO-PK-...`, `LBO-PA-...`, `LBO-MIC-...`).
- **Fokus Diferensiasi Utama Terletak pada Result Management**: Pengelolaan hasil pemeriksaan adalah titik pemisahan operasional sejati di mana form input, validasi dokter spesialis (Sp.PK, Sp.PA, Sp.MK), dan penyajian hasil dibedakan secara tegas sesuai `ServiceType`.

### Rationale
Menggabungkan keunggulan fleksibilitas UX pemesanan (1 klik simpan bagi dokter) dengan ketertiban arsitektur data backend dan LIS (transaksi per layanan yang terisolasi dengan siklus hidup TAT masing-masing).

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-04

---

## OQ-002: Kardinalitas Sediaan Jaringan PA

### Status: CLOSED

### Decision
**Dihilangkan dari Level Order Item**: Atribut sediaan fisik (`SpecimenSite`, `ProcedureType`, `ClinicalDiagnosis`) tidak dicatat pada level order item. Seluruh detail sediaan jaringan, makroskopis, dan mikroskopis didokumentasikan di sistem LIS atau pada saat perekaman hasil ([`LabPaResultModel.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/Entities/Lab/LabPaResultModel.cs)).

### Rationale
Menghilangkan beban penginputan berlebih pada form pemesanan dan menghindari duplikasi data karena detail sediaan secara alamiah diisi oleh dokter spesialis patologi anatomi saat memeriksa spesimen di laboratorium.

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-04

---

## OQ-003: Penetapan Canonical Classification pada Master Data (ServiceType, Discipline, TestGroup)

### Status: CLOSED

### Decision
Menerapkan model klasifikasi terfokus dan disederhanakan pada [`LabTestDefinitionModel`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabTestDefinitionFeature/LabTestDefinitionModel.cs):

1. **`LaboratoryServiceType` (Enum, Wajib, Default: PK = 1)**:
   - Nilai: `ClinicalPathology = 1`, `AnatomicalPathology = 2`, `Microbiology = 3`.
   - **Tujuan Tunggal**: Menentukan tipe form input result lab (Form tabular analit PK, Form naratif PA, Form biakan/AST Micro), serta menentukan `OrderTypeEnum` saat order dikirim ke OWARE.

2. **`Discipline` (Tabel Master Referensi, Opsional)**:
   - Disimpan dalam tabel master referensi mandiri `BILRG_LabDiscipline` (`DisciplineId VARCHAR(10)`, `DisciplineName VARCHAR(100)`).
   - Di `LabTestDefinitionModel`: properti `DisciplineId` (opsional / nullable). Jika kosong, tes masuk ke kelompok "Uncategorized" / "Lain-lain".
   - **Tujuan**: Mengelompokkan list pemeriksaan saat pemesanan di frontend dan pengelompokan seksi utama (Level 1) pada halaman/printout View Result (misal: "HEMATOLOGI", "KIMIA KLINIK", "URINALISIS", "HISTOPATOLOGI", dll.).

3. **`TestGroup` (Tabel Master Referensi, Opsional)**:
   - Disimpan dalam tabel master referensi mandiri `BILRG_LabTestGroup` (`TestGroupId VARCHAR(10)`, `TestGroupName VARCHAR(100)`), tanpa relasi foreign key kaku ke `Discipline`.
   - Di `LabTestDefinitionModel`: properti `TestGroupId` (opsional / nullable).
   - **Tujuan**: Mengakomodir sub-pengelompokan/panel klinis (Level 2) di bawah discipline (contoh: di bawah Hematology $\rightarrow$ "CBC / Darah Lengkap", "Profil Anemia"; di bawah Chemistry $\rightarrow$ "Profil Lipid", "Fungsi Hati").

4. **Snapshot Persistence**:
   - Saat order dibuat (`LabOrderItemModel`), dilakukan snapshot nilai: `ServiceType` (int), `DisciplineId`, `DisciplineName`, `TestGroupId`, dan `TestGroupName`.
   - Hal ini memungkinkan View Result dan Printout Hasil Lab melakukan rendering hierarki grouping (`DisciplineName` $\rightarrow$ `TestGroupName` $\rightarrow$ Item/Komponen Hasil) secara instan dan performan tanpa perlu query runtime `JOIN` ke tabel master referensi.

### Rationale
Menyederhanakan arsitektur klasifikasi agar fokus tepat pada 3 kebutuhan praktis operasional (tipe form input hasil, pengelompokan list/view result, dan sub-panel grup klinis) dengan overhead minimal dan fleksibilitas konfigurasi rumah sakit yang maksimal.

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-03

---

## OQ-004: Pembagian Ruang Lingkup Mikroskopis Infeksi (PK vs Micro)

### Status: CLOSED

### Decision
**Berbasis Alur Proses (Kultur vs Non-Kultur)**:
- Pemeriksaan mikroskopis cepat tanpa kultur (Pewarnaan Gram langsung, Pewarnaan BTA langsung, Preparat KOH jamur langsung) tetap berada di bawah `LaboratoryServiceType.ClinicalPathology` dengan `LaboratoryDiscipline.ClinicalMicroscopy`.
- `LaboratoryServiceType.Microbiology` dikhususkan untuk pemeriksaan biakan/kultur kuman, isolasi patogen, identifikasi spesies, dan antibiogram / uji kepekaan antibiotik (AST).

### Rationale
Selaras dengan alur kerja teknis di laboratorium rumah sakit, waktu tunggu hasil (TAT), dan alokasi meja kerja analis/dokter penanggung jawab.

### Resolved By: Domain Owner & Analyst (/grill-me session)
### Resolved Date: 2026-11-03

---

# 9. Architecture Applicability

Record the Architecture Applicability decision:

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Perubahan ini melibatkan:
1. Perubahan model domain dan skema database pada bounded context Lab di `b09-bilreg-api` (`LabTestDefinitionModel`, master referensi `BILRG_LabDiscipline` dan `BILRG_LabTestGroup`, `LabOrderModel`, `LabOrderItemModel`, serta perluasan `VacutainerTypeEnum`).
2. Penyesuaian arsitektural antarmuka pemesanan pada frontend `c012_myhospital_web` (`LabOrderCreateCanvas.vue`, schema Zod, form input kontekstual PA & Micro, serta View Result grouping).
3. Penetapan keputusan arsitektural terkait kebijakan pemisahan order per disiplin laboratorium (`1 Order = 1 ServiceType`).
4. Middleware OWARE (`a046_oware_ordermiddlewareapi`) diterima **As-Is sepenuhnya** tanpa modifikasi kode.

Oleh karena itu, diperlukan penyusunan dokumen **ARCHITECTURE** formal sebelum implementasi dapat direncanakan dan dieksekusi.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture Applicability confirmed (ARCHITECTURE-REQUIRED)
- [x] Target Architecture finalized and approved by Architect
- [x] Ready for Planning gate granted by Architect

## Status

**READY-FOR-PLANNING**

## Notes

Seluruh kesenjangan (GAP) dan pertanyaan terbuka (OQ) telah berhasil diputuskan dan ditutup (**CLOSED**) melalui sesi alignment bersama Domain Owner. Target Architecture formal disusun pada [LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md). Gate **READY-FOR-PLANNING** resmi diberikan oleh **Architect** (`ica-architect`). Usulan perubahan siap dilanjutkan ke tahap penyusunan Rencana Implementasi (Implementation Plan).

---

# 11. References

Referenced artifacts:

- ISSUE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md)
- ARCHITECTURE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md)
- IMPLEMENTATION PLAN: [LAB-ORDER-PA-MICRO-ACCOMMODATION-IMPLEMENTATION-PLAN.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-IMPLEMENTATION-PLAN.md)
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
