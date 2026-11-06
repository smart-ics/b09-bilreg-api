---
Title: Spesifikasi Arsitektur Teknis Akomodasi Order Laboratorium Patologi Anatomi dan Mikrobiologi
Code: LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE
Artifact: ARCHITECTURE
Version: 1.1
LastUpdated: 2026-11-04
---

# 1. Overview

Arsitektur teknis ini mendefinisikan realisasi spesifikasi teknis pada bounded context Laboratorium di **`b09-bilreg-api`** dan antarmuka web HIS di **`c012_myhospital_web`** untuk mengakomodasi pemesanan pemeriksaan laboratorium dari disiplin **Patologi Anatomi (Anatomical Pathology / Lab PA)** dan **Mikrobiologi Klinik (Microbiology / Lab Micro)** berdampingan dengan pemeriksaan **Patologi Klinik (Clinical Pathology / Lab PK)** yang telah berjalan.

Arsitektur ini merealisasikan keputusan-keputusan strategis hasil analisis kelayakan:
1. **Klasifikasi Kanonikal Master Data**: Menetapkan pemisahan antara tipe layanan (`LaboratoryServiceType`), bidang pemeriksaan (`LaboratoryDiscipline`), dan grup panel klinis (`LaboratoryTestGroup`) pada master data pemeriksaan lab (`LabTestDefinitionModel`).
2. **Pemesanan Tunggal Terpadu (Single Cart Intake)**: Mempertahankan kenyamanan dokter/perawat pada frontend form pemesanan (`LabOrderCreateCanvas.vue`) tanpa kerepotan pergantian multi-tab, di mana seleksi tes apapun (PK, PA, maupun Micro) dapat dimasukkan dalam satu keranjang pemesanan yang sama.
3. **Pemisahan Transaksi Berbasis Layanan (Auto-Split Engine)**: Backend secara transparan memecah item pemesanan yang berbeda disiplin layanan menjadi entitas transaksi order terpisah (`1 Order = 1 ServiceType`), menjamin isolasi siklus hidup (TAT), penagihan (billing release), serta perutean LIS.
4. **Pergeseran Diferensiasi ke Result Management**: Titik pembedaan operasional sesungguhnya diletakkan pada antarmuka perekaman hasil (Result Entry Form) dan penyajian hasil (View Result & Printout Hasil Lab).
5. **Akomodasi Sampel Non-Darah Ringkas**: Memperluas `VacutainerTypeEnum` dengan nilai `None = 0` untuk spesimen jaringan PA, swab/kultur mikro, urin, feses, dan cairan tubuh tanpa tabung vacutainer darah.
6. **Zero-Modification pada Middleware OWARE**: Menjaga middleware `a046_oware_ordermiddlewareapi` tetap 100% As-Is tanpa perubahan kode maupun kontrak DTO.

Referenced artifacts:
- ISSUE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE.md)
- FEASIBILITY ASSESSMENT: [LAB-ORDER-PA-MICRO-ACCOMMODATION-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-FEASIBILITY-ASSESSMENT.md)
- IMPLEMENTATION PLAN: [LAB-ORDER-PA-MICRO-ACCOMMODATION-IMPLEMENTATION-PLAN.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-IMPLEMENTATION-PLAN.md)
- DOMAIN Bilreg: [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- DOMAIN PA OWARE: [LAB-PA-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/docs/LAB-PA-DOMAIN.md)
- DOMAIN Mikrobiologi OWARE: [CANONICAL-DOMAIN-SPECIFICATION-MICROBIOLOGY.md](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/docs/CANONICAL-DOMAIN-SPECIFICATION-MICROBIOLOGY.md)
- OUTCOME: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)

---

# 2. Architectural Basis

## Business Context

Sesuai spesifikasi domain laboratorium pada [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md), laboratorium rumah sakit modern terbagi menjadi tiga pilar spesialisasi klinis utama:
- **Patologi Klinik (PK)**: Pemeriksaan darah dan cairan tubuh rutin, otomatisasi alat tinggi, TAT cepat (menit hingga jam), divalidasi oleh dokter Spesialis Patologi Klinik (Sp.PK).
- **Patologi Anatomi (PA)**: Pemeriksaan jaringan biopsi/operasi, sitologi, FNAB, dan potong beku (*frozen section*), proses manual bertahap (fiksasi, pemotongan, pewarnaan), TAT 3–7 hari, dilaporkan naratif oleh dokter Spesialis Patologi Anatomi (Sp.PA).
- **Mikrobiologi Klinik (Micro)**: Pemeriksaan biakan kuman, isolasi patogen, dan uji kepekaan antibiotik (AST/antibiogram), proses inkubasi 2–5 hari, divalidasi oleh dokter Spesialis Mikrobiologi Klinik (Sp.MK).

## Analysis Input

Target arsitektur ini secara langsung mengonsumsi keputusan-keputusan resmi dari [LAB-ORDER-PA-MICRO-ACCOMMODATION-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-FEASIBILITY-ASSESSMENT.md):
- **GAP-001**: Penambahan `None = 0` pada `VacutainerTypeEnum` untuk sampel non-darah, dengan `RequiredTubeCount = 0`.
- **GAP-002 & OQ-002**: Menghilangkan atribut spesifik sediaan PA (`SpecimenSite`, `ProcedureType`, `ClinicalDiagnosis`) dari `LabOrderItemModel`. Atribut-atribut ini sepenuhnya ditangani di sistem LIS atau pada saat perekaman hasil (`LabPaResultModel`).
- **GAP-003**: Parameter spesifik mikrobiologi disederhanakan: riwayat antibiotik dan indikasi klinis ditangani melalui properti `ClinicalNote` eksisting pada header order tanpa menambah kolom baru pada `LabOrderModel`, `AnatomicSource` dihilangkan dari item (cukup diwakili oleh `SpecimenType` dan catatan klinis), serta form pemesanan frontend tidak memunculkan input metadata mikro kondisional.
- **GAP-004**: Menolak antarmuka multi-tab pemesanan; menyajikan keranjang pemesanan terpadu (*single cart*) di frontend dan memindahkan diferensiasi ke meja kerja hasil (Result Management).
- **GAP-005 & GAP-006**: Menerima OWARE (`a046_oware_ordermiddlewareapi`) **100% As-Is**; perutean vendor LIS ditangani melalui `MapTarifLab` (`ConsId`) dan `ConsumerModel.OrderType`.
- **OQ-001**: Kebijakan `1 Order = 1 ServiceType` direalisasikan via *Transparent Auto-Split Engine* di application layer backend.
- **OQ-003**: Penetapan klasifikasi kanonikal (`LaboratoryServiceType`, `Discipline`, `TestGroup`) pada master data serta penyimpanan snapshot pada baris order transaksi (`LabOrderItemModel`).
- **OQ-004**: Pemeriksaan mikroskopis infeksi tanpa kultur (Gram, BTA, KOH) tetap berada di bawah PK (`Discipline: ClinicalMicroscopy`); Microbiology dikhususkan untuk biakan dan uji kepekaan antimikroba (AST).

---

# 3. Scope

## Included

1. **`b09-bilreg-api` (Domain Layer)**:
   - Enum baru `LaboratoryServiceType` (`ClinicalPathology = 1`, `AnatomicalPathology = 2`, `Microbiology = 3`).
   - Perluasan enum `VacutainerTypeEnum` dengan menambahkan `None = 0`.
   - Pembaruan `LabTestDefinitionModel`: menambahkan properti `ServiceType`, `DisciplineId`, dan `TestGroupId`.
   - Entitas master referensi baru `LabDisciplineModel` (`DisciplineId`, `DisciplineName`, `AuditTrail`) dan `LabTestGroupModel` (`TestGroupId`, `TestGroupName`, `AuditTrail`).
   - Pembaruan `LabOrderModel`: menambahkan properti `ServiceType` (enum `LaboratoryServiceType`). Informasi riwayat antibiotik dan indikasi patogen ditampung dalam properti `ClinicalNote` eksisting.
   - Pembaruan `LabOrderItemModel`: menambahkan properti snapshot `ServiceType`, `DisciplineId`, `DisciplineName`, `TestGroupId`, dan `TestGroupName` (`AnatomicSource` dihilangkan, cukup diwakili oleh `SpecimenType` dan catatan klinis).

2. **`b09-bilreg-api` (Application & Infrastructure Layer)**:
   - Modifikasi `LabTestResolutionService`: memetakan snapshot disiplin/grup ke `ResolvedLabOrderLine` dan menetapkan `RequiredTubeCount = 0` bila `VacutainerType == VacutainerTypeEnum.None`.
   - Implementasi logika *Transparent Auto-Split* pada `LabOrderCreateFromEmrHandler` dan `LabOrderCreateExternalPatientHandler`:
     - Memecah daftar item hasil resolusi berdasarkan `ServiceType`.
     - Menerbitkan entitas `LabOrderModel` mandiri untuk setiap grup `ServiceType` dalam satu transaksi atomik.
     - Mengembangkan skema nomor unik `EmrOrderId` bila terjadi pemecahan (`{EmrOrderId}-PK`, `{EmrOrderId}-PA`, `{EmrOrderId}-MIC`).
   - Query & Command handlers untuk master referensi `LabDiscipline` dan `LabTestGroup` (List, Get, Create, Update, Delete).
   - Pembaruan endpoint `LabTestDefinitionFeature` untuk menerima dan mengembalikan `ServiceType`, `DisciplineId`, dan `TestGroupId`.

3. **`b09-bilreg-api` (SqlDb Layer)**:
   - Tabel baru `BILRG_LabDiscipline` dan `BILRG_LabTestGroup`.
   - Skrip alter tabel `BILRG_LabTestDefinition` (menambahkan `ServiceType`, `DisciplineId`, `TestGroupId`).
   - Skrip alter tabel `BILRG_LabOrder` (menambahkan `ServiceType`).
   - Skrip alter tabel `BILRG_LabOrderItem` (menambahkan `ServiceType`, `DisciplineId`, `DisciplineName`, `TestGroupId`, `TestGroupName`).

4. **`c012_myhospital_web` (Frontend HIS Web)**:
   - Pembaruan Zod schema master dan order (`labMasterTarifItemSchema`, `labOrderCreateFromEmrCmdSchema`, `labOrderDetailSchema`).
   - Integrasi `LabOrderCreateCanvas.vue`:
     - Tetap satu formulir pencarian tes terpadu tanpa tab.
     - Penyesuaian kalkulator tabung darah: mengabaikan tes dengan `vacutainerType === 0` (non-darah).
     - Tidak ada input kondisional tambahan untuk mikrobiologi; riwayat terapi antibiotik atau indikasi kecurigaan patogen dicatat langsung pada kolom `ClinicalNote` umum yang sudah ada.
   - Perutean formulir entri hasil (`LabResultEntryRouter.vue`):
     - `ServiceType = 1` (PK): Menggunakan form tabular analit eksisting (`LabResultEntryForm.vue`).
     - `ServiceType = 2` (PA): Menggunakan form naratif Patologi Anatomi (`LabResultEntryPaForm.vue`: makroskopis, mikroskopis, diagnosa PA, lokasi sediaan).
     - `ServiceType = 3` (Micro): Menggunakan form biakan & AST (`LabResultEntryMicroForm.vue`: koloni, organisme, antibiogram sensitivitas S/I/R).
   - Halaman View Result & Cetak Hasil: Pengelompokan visual 2-tingkat berbasis snapshot (`DisciplineName` $\rightarrow$ `TestGroupName` $\rightarrow$ Item analit).

## Excluded

- **`a046_oware_ordermiddlewareapi`**: Tidak ada perubahan kode sama sekali (**Zero Code Changes**). Perutean vendor LIS tetap menggunakan `MapTarifLab` dan `ConsumerModel.OrderType` eksisting.
- Modifikasi skema penagihan tindakan pada `TarifContext` / `ta_tindakan` (order lab tetap ditagihkan ke billing akun rawat melalui mekanisme `Charge` eksisting).
- Modifikasi modul antarmuka bangku kerja fisik (*bench analyzer worklist*) untuk proses potong mikrotom jaringan PA atau inkubator mikro (dikelola oleh sistem LIS vendor).

---

# 4. Technical Decisions

### TD-01: Canonical Classification Hierarchy on Master Data

Master data pemeriksaan laboratorium distrukturkan secara tegas dalam tiga konsep ortogonal:

```text
LaboratoryServiceType (Enum: 1=PK, 2=PA, 3=Micro)
  │  [Wajib, Single Select, Default: 1 (PK)]
  │  • Menentukan tipe formulir Result Entry di frontend HIS
  │  • Menentukan isolasi transaksi order (1 Order = 1 ServiceType)
  │  • Menentukan pemetaan OrderType ke OWARE LIS
  │
  ├── LaboratoryDiscipline (Master Reference Table: BILRG_LabDiscipline)
  │     [Opsional, Reference lookup via DisciplineId]
  │     • Bidang keilmuan laboratorium (Hematology, Clinical Chemistry, Histopathology, dll.)
  │     • Berfungsi sebagai Section Header (Level 1) pada View Result & Printout
  │
  └── LaboratoryTestGroup (Master Reference Table: BILRG_LabTestGroup)
        [Opsional, Reference lookup via TestGroupId]
        • Panel klinis fungsional (CBC / Darah Lengkap, Lipid Profile, Liver Function, dll.)
        • Berfungsi sebagai Sub-Header (Level 2) di bawah Discipline
        • Desain Loose Coupling: Tidak memiliki foreign key kaku ke Discipline
```

**Rasional**: Memisahkan kebutuhan administratif/operasional (tipe form dan perutean) dari kebutuhan presentasi klinis (seksi pengelompokan laporan), mencegah over-engineering hierarki pohon yang rumit, dan memberikan kebebasan fleksibel bagi rumah sakit untuk menyusun panel tes.

---

### TD-02: Non-Blood Specimen Accomodation via `VacutainerTypeEnum.None = 0`

Enum `VacutainerTypeEnum` diperluas dengan nilai integer `0`:

```csharp
namespace Bilreg.Domain.LabContext.LabOrderFeature;

public enum VacutainerTypeEnum
{
    None = 0,     // Sampel non-darah (jaringan PA, swab kultur mikro, urin, feses, cairan)
    Edta = 1,     // Tabung Ungu (K2EDTA / K3EDTA)
    Serum = 2,    // Tabung Merah / Kuning (Serum Clot Activator / SST Gel)
    Citrate = 3,  // Tabung Biru (Sodium Citrate 3.2%)
    Heparin = 4   // Tabung Hijau (Lithium Heparin)
}
```

**Aturan Resolusi & UI**:
- Pada `LabTestResolutionService`, jika `definition.VacutainerType == VacutainerTypeEnum.None`, maka `RequiredTubeCount` diset ke `0`.
- Pada frontend ([`LabOrderCreateCanvas.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue)), kalkulasi `tubeSummary` dan fungsi pemformat badge ([`lab-formatters.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/lib/lab-formatters.ts)) mengabaikan item pemeriksaan yang memiliki `vacutainerType === 0`.
- Jenis fisik bahan biologis dicatat secara deskriptif pada string `SpecimenType` (misal: `"Jaringan Biopsi"`, `"Sputum"`, `"Urin Tampung"`, `"Swab Luka"`).

---

### TD-03: Order Intake Transparent Auto-Split Engine

Untuk mempertahankan UX formulir pemesanan dokter yang cepat dan terpadu tanpa tab, use-case `LabOrderCreateFromEmrHandler` (dan padanannya untuk pasien luar) mengimplementasikan mesin *Auto-Split*:

```text
Doctor/Nurse in HIS Web (LabOrderCreateCanvas)
  │
  │ [Submits Single Cart: 2x PK Items, 1x PA Item, 1x Micro Item]
  ▼
LabOrderCreateFromEmrHandler
  │
  ├─► 1. Resolve all items via LabTestResolutionService
  ├─► 2. Group resolved lines by line.Item.ServiceType:
  │      • Group PK   (ServiceType = 1) -> 2 items
  │      • Group PA   (ServiceType = 2) -> 1 item
  │      • Group Micro(ServiceType = 3) -> 1 item
  │
  ├─► 3. For each group within an atomic TransHelper scope:
  │      • Determine EmrOrderId:
  │          - If single group: preserve exact request.EmrOrderId
  │          - If multi group : append suffix (e.g. $"{request.EmrOrderId}-PK", $"-PA", $"-MIC")
  │      • Allocate OrderNo via ISequencer
  │      • Instantiate LabOrderModel with specific ServiceType
  │      • Preserve shared ClinicalNote on all split sub-orders
  │      • Persist via _labOrderRepo.SaveChanges(order)
  │
  └─► 4. Return LabOrderCreateFromEmrResponse containing primary & split order references
```

**Idempotensi & Transaksional**:
- Seluruh pemecahan order dibungkus dalam `using (var trans = TransHelper.NewScope())`. Jika satu sub-order gagal (misal validasi duplikasi atau error database), seluruh order dibatalkan (*rollback*) secara utuh.
- Pengecekan idempotensi `LoadByEmrOrderId` mendeteksi baik `request.EmrOrderId` maupun versi ber-sufiks untuk mencegah pembuatan order ganda saat jaringan *retry*.

---

### TD-04: Order Item Snapshot Persistence for Zero-JOIN View Rendering

Saat entitas baris order (`LabOrderItemModel`) dibentuk oleh `LabTestResolutionService`, data klasifikasi dari master disalin (*snapshot*) ke dalam baris order:
- `ServiceType` (integer)
- `DisciplineId` (string)
- `DisciplineName` (string)
- `TestGroupId` (string)
- `TestGroupName` (string)

**Keuntungan**:
- Komponen frontend View Result dan template cetak dokumen hasil laboratorium dapat melakukan pengelompokan hierarkis (`DisciplineName` $\rightarrow$ `TestGroupName` $\rightarrow$ Analit) secara langsung dari payload order tanpa memicu query `JOIN` yang berat ke tabel master.
- Menjamin integritas historis rekam medis: Perubahan nama disiplin atau pemindahan grup tes di master data pada masa mendatang tidak akan merusak layout dokumen hasil lab pasien yang sudah selesai diperiksa di masa lampau.

---

### TD-05: Result Management Form Polymorphism

Frontend HIS Web mendesentralisasi penanganan hasil pemeriksaan berbasis nilai `order.serviceType`. Dibuat komponen perute (*router component*) `LabResultEntryRouter.vue` yang menginspeksi tipe layanan order:

1. **`ServiceType = 1` (ClinicalPathology)**:
   - Komponen: `LabResultEntryPkTabular.vue` (refactor dari `LabResultEntryForm.vue`).
   - Tampilan: Grid baris analit komponen tes dengan input nilai numerik/teks/opsi, unit satuan, nilai rujukan, dan indikator flag (Normal, High, Low, Critical).
2. **`ServiceType = 2` (AnatomicalPathology)**:
   - Komponen: `LabResultEntryPaNarrative.vue`.
   - Tampilan: Editor teks naratif terstruktur mencakup:
     - Asal Jaringan / Topografi Spesimen (`SpecimenSite`).
     - Pemeriksaan Makroskopis (`MacroscopicText`).
     - Pemeriksaan Mikroskopis (`MicroscopicText`).
     - Kesimpulan / Diagnosis Patologi (`ConclusionText`).
     - Kode Morfologi / ICD-O (`SnomedCode`).
3. **`ServiceType = 3` (Microbiology)**:
   - Komponen: `LabResultEntryMicroCulture.vue`.
   - Tampilan: Formulir biakan dan sensitivitas mikro mencakup:
     - Evaluasi sediaan langsung / pewarnaan Gram (`DirectSmearFindings`).
     - Hitung jumlah koloni (`ColonyCount`).
     - Status pertumbuhan kuman (`IsGrowth`: Ya / Tidak ada pertumbuhan).
     - Daftar organisme yang terisolasi (`Organisms`).
     - Matriks tabel uji kepekaan antibiotik (Antibiogram Grid: Nama Antibiotika, Nilai MIC, Kategori S / I / R).

---

### TD-06: Zero-Modification Interface Boundary for Middleware OWARE

Middleware OWARE (`a046_oware_ordermiddlewareapi`) dipertahankan 100% As-Is tanpa ada perubahan kode sumber:
- Worker OWARE memanggil `GET api/LabContext/LabOrderFeature/{orderId}` pada Bilreg. Kontrak DTO `BilregLabOrderResponseDto` tetap menerima field eksisting.
- Builder `OrderLabBilregBuilder.cs` tetap mengisi `OrderType = OrderTypeEnum.Lab`.
- Router [OrderLabHubService.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/OrderLabHubService.cs) di OWARE secara otomatis merutekan order ke vendor LIS eksternal yang tepat berdasarkan konfigurasi pemetaan tarif `MapTarifLab` (`ConsId`) dan `ConsumerModel.OrderType`:
  - Item dengan mapping LIS PA diarahkan ke adapter vendor PA (misal `OrderLabPaBerliandoService`).
  - Item dengan mapping LIS Micro diarahkan ke adapter vendor Micro (misal Prolims).
  - Item dengan mapping LIS PK standar diarahkan ke adapter Sysmex / HCLAB.

---

# 5. Component Responsibilities

| Repositori / Layer | Komponen / Modul | Tanggung Jawab Utama |
|---|---|---|
| **`b09-bilreg-api` (Domain)** | `LaboratoryServiceType.cs` | Mendefinisikan enum klasifikasi tipe layanan laboratorium (PK=1, PA=2, Micro=3). |
| **`b09-bilreg-api` (Domain)** | `VacutainerTypeEnum.cs` | Mendefinisikan enum wadah spesimen, diperkaya nilai `None = 0`. |
| **`b09-bilreg-api` (Domain)** | `LabDisciplineModel.cs` | Entitas domain master referensi disiplin/bidang laboratorium. |
| **`b09-bilreg-api` (Domain)** | `LabTestGroupModel.cs` | Entitas domain master referensi grup panel klinis pemeriksaan. |
| **`b09-bilreg-api` (Domain)** | `LabTestDefinitionModel.cs` | Memegang konfigurasi tes laboratorium beserta atribut `ServiceType`, `DisciplineId`, dan `TestGroupId`. |
| **`b09-bilreg-api` (Domain)** | `LabOrderModel.cs` | Agregat transaksi order laboratorium, memegang `ServiceType`. Catatan riwayat terapi atau indikasi patogen ditampung pada `ClinicalNote`. |
| **`b09-bilreg-api` (Domain)** | `LabOrderItemModel.cs` | Snapshot baris tes order yang menyimpan `ServiceType`, `DisciplineId`, `DisciplineName`, `TestGroupId`, `TestGroupName`. |
| **`b09-bilreg-api` (App/Infra)** | `LabTestResolutionService.cs` | Resolusi tarif ke tes, mengisi snapshot klasifikasi, dan mengatur `RequiredTubeCount = 0` bila `VacutainerType == None`. |
| **`b09-bilreg-api` (Application)** | `LabOrderCreateFromEmrHandler.cs` | Orkestrasi pembuatan order dari EMR, mengeksekusi mesin *Auto-Split* order per `ServiceType`. |
| **`b09-bilreg-api` (Application)** | `LabDisciplineUseCases` | Query & Command handlers untuk master referensi `BILRG_LabDiscipline`. |
| **`b09-bilreg-api` (Application)** | `LabTestGroupUseCases` | Query & Command handlers untuk master referensi `BILRG_LabTestGroup`. |
| **`b09-bilreg-api` (SqlDb)** | Script DDL SQL | Pembuatan tabel `BILRG_LabDiscipline`, `BILRG_LabTestGroup`, dan alter skema tabel terkait. |
| **`c012_myhospital_web`** | `useLabMasterLookup.ts` | Menyediakan composable pencarian master tarif lab terpadu beserta filter `discipline` dan `serviceType`. |
| **`c012_myhospital_web`** | `LabOrderCreateCanvas.vue` | Antarmuka keranjang pemesanan terpadu (*single cart*) tanpa input kondisional tambahan; penyesuaian kalkulator tabung darah mengabaikan tes non-darah. |
| **`c012_myhospital_web`** | `LabResultEntryRouter.vue` | Komponen perute formulir penginputan hasil sesuai `ServiceType` order (PK tabular, PA naratif, Micro biakan/AST). |
| **`c012_myhospital_web`** | `LabResultView.vue` | Penyajian hasil pemeriksaan dengan hierarki pengelompokan 2-tingkat (`Discipline` $\rightarrow$ `TestGroup` $\rightarrow$ Item analit). |
| **`a046_oware`** | *(As-Is)* | Menjaga bridging pertukaran data ke vendor LIS tanpa modifikasi kode sumber. |

---

# 6. Integration Design

Diagram berikut mengilustrasikan kolaborasi antar-komponen saat pemesanan tes campuran dan alur perekaman hasil:

```mermaid
sequenceDiagram
    autonumber
    actor Clinician as Dokter / Perawat (HIS Web)
    participant UI as LabOrderCreateCanvas.vue
    participant BilregApi as Bilreg LabOrderCreateFromEmrHandler
    participant Resolver as LabTestResolutionService
    participant Repo as LabOrderRepo & DB
    actor LabStaff as Analis / Sp.PK / Sp.PA / Sp.MK
    participant ResultUI as LabResultEntryRouter.vue
    participant Oware as OWARE Middleware (As-Is)

    Note over Clinician,UI: 1. Pemesanan Terpadu (Single Cart)
    Clinician->>UI: Cari tes (Hematologi Rutin, Biopsi Jaringan, Kultur Urin)
    UI->>Clinician: Tampilkan keranjang belanja tanpa tab; tabung darah dihitung hanya untuk tes darah
    Clinician->>UI: Klik Simpan Order Lab

    Note over UI,BilregApi: 2. Transmisi Payload & Auto-Split
    UI->>BilregApi: POST api/LabContext/LabOrderFeature/from-emr
    BilregApi->>Resolver: ResolveByTarifItems(items)
    Resolver-->>BilregApi: List of ResolvedOrderLines (dengan ServiceType & Disiplin)
    BilregApi->>BilregApi: Auto-Split: Kelompokkan item berdasarkan ServiceType (PK, PA, Micro)
    BilregApi->>Repo: Simpan Order PK (LBO-..., ServiceType=1)
    BilregApi->>Repo: Simpan Order PA (LBO-..., ServiceType=2)
    BilregApi->>Repo: Simpan Order Micro (LBO-..., ServiceType=3)
    BilregApi-->>UI: Response Success (Created Orders: PK, PA, Micro)

    Note over Repo,Oware: 3. Pengambilan Order oleh OWARE (As-Is)
    Oware->>Repo: GET api/LabContext/LabOrderFeature/{orderId}
    Oware->>Oware: MapTarifLab & OrderLabHubService rute ke Sysmex (PK), Berliando (PA), Prolims (Micro)

    Note over LabStaff,ResultUI: 4. Pengelolaan Hasil (Result Management)
    LabStaff->>ResultUI: Buka Order Hasil Lab
    alt Order ServiceType == 1 (PK)
        ResultUI->>LabStaff: Buka LabResultEntryPkTabular (Grid analit normal/tinggi/rendah)
    else Order ServiceType == 2 (PA)
        ResultUI->>LabStaff: Buka LabResultEntryPaNarrative (Makroskopis, Mikroskopis, Diagnosa PA)
    else Order ServiceType == 3 (Micro)
        ResultUI->>LabStaff: Buka LabResultEntryMicroCulture (Koloni, Organisme, Tabel AST)
    end
```

---

# 7. Data Ownership

| Kategori Data | Pemilik Tunggal (*Owner*) | Sistem Konsumen (*Consumers*) |
|---|---|---|
| **Master Klasifikasi Lab** (`ServiceType`, `Discipline`, `TestGroup`) | `b09-bilreg-api` (`LabContext.LabTestDefinitionFeature`) | `c012_myhospital_web`, Form Order Canvas, Setup Tarif Lab |
| **Agregat Order Lab** (`BILRG_LabOrder`) | `b09-bilreg-api` (`LabContext.LabOrderFeature`) | `c012_myhospital_web`, `a046_oware_ordermiddlewareapi`, Kasir/Billing |
| **Snapshot Baris Order** (`BILRG_LabOrderItem`) | `b09-bilreg-api` (`LabContext.LabOrderFeature`) | `c012_myhospital_web` (View Result & Cetak Hasil Lab) |
| **Dokumen Hasil Lab** (`BILRG_LabResultDocument`) | `b09-bilreg-api` (`LabContext.LabResultFeature`) | `c012_myhospital_web`, Resume Medis EMR, Cetak Hasil Lab |
| **Konfigurasi Routing Vendor LIS** (`MapTarifLab`) | `a046_oware_ordermiddlewareapi` | Vendor LIS Sysmex, Berliando, Prolims, HCLAB |

---

# 8. Database Design

## New Tables

### 1. `BILRG_LabDiscipline`
Tabel master referensi bidang keilmuan laboratorium (Level 1 visual grouping).

```sql
CREATE TABLE BILRG_LabDiscipline (
    DisciplineId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_DisciplineId DEFAULT(''),
    DisciplineName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_DisciplineName DEFAULT(''),
    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_CrtUser DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_CrtDate DEFAULT('3000-01-01'),
    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_UpdUser DEFAULT(''),
    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_UpdDate DEFAULT('3000-01-01'),
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabDiscipline_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_LabDiscipline PRIMARY KEY CLUSTERED (DisciplineId)
);
GO

CREATE INDEX IX_BILRG_LabDiscipline_Name ON BILRG_LabDiscipline(DisciplineName);
GO
```

### 2. `BILRG_LabTestGroup`
Tabel master referensi sub-pengelompokan panel klinis (Level 2 sub-panel).

```sql
CREATE TABLE BILRG_LabTestGroup (
    TestGroupId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabTestGroup_TestGroupId DEFAULT(''),
    TestGroupName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabTestGroup_TestGroupName DEFAULT(''),
    CrtUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabTestGroup_CrtUser DEFAULT(''),
    CrtDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabTestGroup_CrtDate DEFAULT('3000-01-01'),
    UpdUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabTestGroup_UpdUser DEFAULT(''),
    UpdDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabTestGroup_UpdDate DEFAULT('3000-01-01'),
    VodUser VARCHAR(50) NOT NULL CONSTRAINT DF_BILRG_LabTestGroup_VodUser DEFAULT(''),
    VodDate DATETIME NOT NULL CONSTRAINT DF_BILRG_LabTestGroup_VodDate DEFAULT('3000-01-01'),

    CONSTRAINT PK_BILRG_LabTestGroup PRIMARY KEY CLUSTERED (TestGroupId)
);
GO

CREATE INDEX IX_BILRG_LabTestGroup_Name ON BILRG_LabTestGroup(TestGroupName);
GO
```

---

## Modified Tables

### 1. `BILRG_LabTestDefinition`
Penambahan kolom klasifikasi pada definisi tes:

```sql
ALTER TABLE BILRG_LabTestDefinition
ADD ServiceType INT NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_ServiceType DEFAULT(1),
    DisciplineId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_DisciplineId DEFAULT(''),
    TestGroupId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabTestDefinition_TestGroupId DEFAULT('');
GO

CREATE INDEX IX_BILRG_LabTestDefinition_ServiceDiscipline 
ON BILRG_LabTestDefinition(ServiceType, DisciplineId, TestGroupId);
GO
```

### 2. `BILRG_LabOrder`
Penambahan kolom tipe layanan pada header transaksi order:

```sql
ALTER TABLE BILRG_LabOrder
ADD ServiceType INT NOT NULL CONSTRAINT DF_BILRG_LabOrder_ServiceType DEFAULT(1);
GO

CREATE INDEX IX_BILRG_LabOrder_ServiceType ON BILRG_LabOrder(ServiceType, CrtDate);
GO
```

### 3. `BILRG_LabOrderItem`
Penambahan kolom snapshot klasifikasi master pada baris item order:

```sql
ALTER TABLE BILRG_LabOrderItem
ADD ServiceType INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_ServiceType DEFAULT(1),
    DisciplineId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_DisciplineId DEFAULT(''),
    DisciplineName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_DisciplineName DEFAULT(''),
    TestGroupId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TestGroupId DEFAULT(''),
    TestGroupName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_TestGroupName DEFAULT('');
GO

CREATE INDEX IX_BILRG_LabOrderItem_Grouping 
ON BILRG_LabOrderItem(OrderId, DisciplineName, TestGroupName);
GO
```

---

## Migration Considerations

1. **Backfill Nilai Eksisting (Backward-Compatibility)**:
   - Kolom `ServiceType` pada `BILRG_LabTestDefinition`, `BILRG_LabOrder`, dan `BILRG_LabOrderItem` diberi default `1` (`ClinicalPathology`). Seluruh data historis otomatis berkategori PK tanpa perlu manual update.
   - Kolom `DisciplineId` dan `TestGroupId` memiliki default `''` (string kosong), yang secara aman diperlakukan sebagai *"Uncategorized / Umum"* pada rendering antarmuka.
2. **Seed Data Awal Master Referensi Disiplin**:
   - Skrip migrasi menyertakan seed data awal untuk disiplin laboratorium umum:
     - `DISC-HEM`: Hematologi
     - `DISC-KIM`: Kimia Klinik
     - `DISC-IMM`: Imunologi & Serologi
     - `DISC-URI`: Urinalisis & Feses
     - `DISC-MIK`: Mikrobiologi Klinik
     - `DISC-PAH`: Patologi Anatomi - Histopatologi
     - `DISC-PAS`: Patologi Anatomi - Sitologi

---

# 9. Cross-Cutting Concerns

### 1. Transaksional & Atomisitas Pemecahan Order
Mesin Auto-Split pada `LabOrderCreateFromEmrHandler` harus membungkus seluruh pembuatan entitas sub-order dalam satu `TransactionScope` (`TransHelper.NewScope()`). Apabila terdapat order item yang valid sementara item lain memicu kegagalan (misalnya duplikasi tes pada salah satu disiplin atau kegagalan alokasi nomor urut sequencer), maka seluruh rangkaian sub-order dibatalkan secara bersamaan (*atomic all-or-nothing*).

### 2. Idempotensi Sufiks `EmrOrderId`
Untuk memastikan integrasi EMR tidak menghasilkan duplikasi saat terjadi jaringan terputus (*network timeout*):
- Jika order asli di-split menjadi misalnya `EMR-9988-PK` dan `EMR-9988-PA`, maka pengecekan idempotensi mengecek keberadaan `EMR-9988` atau awalan `EMR-9988-%`. Jika salah satu telah tercatat, handler mengembalikan ID order yang telah ada.

### 3. Integritas Audit Trail & Keamanan
Setiap operasi penambahan dan modifikasi master disiplin dan definisi tes wajib mencatat metadata audit lengkap (`CrtUser`, `CrtDate`, `UpdUser`, `UpdDate`, `VodUser`, `VodDate`). Perekaman hasil dan verifikasi klinis tetap mematuhi aturan otorisasi PPA dokter spesialis yang bersangkutan (Sp.PK untuk PK, Sp.PA untuk PA, dan Sp.MK untuk Micro).

---

# 10. Implementation Constraints

1. **Backend Technology Stack (`b09-bilreg-api`)**:
   - Wajib menggunakan pola Tactical DDD yang konsisten dengan modul `LabContext` eksisting.
   - Akses data menggunakan SQL eksplisit / Dapper (tanpa Entity Framework Core).
   - Penggunaan helper framework internal `Nuna.Lib.AutoNumberHelper`, `Nuna.Lib.TransactionHelper.TransHelper`, dan `Ardalis.GuardClauses`.
   - Mengikuti aturan arsitektur: Domain Entity merepresentasikan invariant bisnis murni; Repository / DAL bertugas menangani pemetaan SQL.

2. **Frontend Technology Stack (`c012_myhospital_web`)**:
   - Seluruh kode beroperasi di dalam direktori `c012_myhospital_web/`.
   - Menggunakan Vue 3 Composition API `<script setup lang="ts">`, TanStack Query (`useQuery`, `useMutation`), dan Zod schema validation.
   - Menggunakan styling Tailwind CSS v4 dan shadcn-vue base components.
   - Wajib memformat kode dengan `npx prettier --write` dan memverifikasi type-check dengan `pnpm tc:app`.

3. **Integrasi Middleware (`a046_oware_ordermiddlewareapi`)**:
   - Dilarang keras mengubah kode, DTO, atau enum pada repositori OWARE. OWARE diperlakukan sebagai sistem eksternal yang sudah stabil.

---

# 11. Acceptance Conditions

Implementasi arsitektur ini dinyatakan lengkap apabila memenuhi kriteria berikut:

1. **Master Data Klasifikasi Lab**:
   - [ ] Enum `LaboratoryServiceType` dan nilai `VacutainerTypeEnum.None = 0` terdefinisi dan tervalidasi pada `Bilreg.Domain`.
   - [ ] Tabel `BILRG_LabDiscipline` dan `BILRG_LabTestGroup` terbentuk di database dengan endpoint CRUD yang berfungsi.
   - [ ] Entitas `LabTestDefinitionModel` mampu menyimpan dan memuat `ServiceType`, `DisciplineId`, dan `TestGroupId`.
2. **Order Intake & Auto-Split Engine**:
   - [ ] Dokter dapat memilih kombinasi tes PK, PA, dan Micro secara bersamaan pada `LabOrderCreateCanvas.vue` dalam satu keranjang tanpa tab.
   - [ ] Kalkulator kebutuhan tabung vacutainer pada canvas tidak menghitung tes dengan `vacutainerType = None (0)`.
   - [ ] Backend `LabOrderCreateFromEmrHandler` berhasil memecah keranjang campuran menjadi sub-order mandiri sesuai `ServiceType` secara atomik.
   - [ ] Masing-masing sub-order yang dihasilkan memiliki nomor order transaksi (`OrderNo`) tersendiri dengan `ServiceType` yang tepat.
   - [ ] Tabel `BILRG_LabOrderItem` menyimpan snapshot lengkap (`ServiceType`, `DisciplineName`, `TestGroupName`).
3. **OWARE Compatibility**:
   - [ ] Worker OWARE mampu membaca setiap sub-order dari `b09-bilreg-api` dan merutekannya ke vendor LIS masing-masing (PK ke Sysmex, PA ke Berliando, Micro ke Prolims) tanpa error.
4. **Result Management Polymorphism**:
   - [ ] Antarmuka perekaman hasil (`LabResultEntryRouter.vue`) menampilkan form tabular untuk order PK, form naratif terstruktur untuk order PA, dan form biakan/AST untuk order Micro.
   - [ ] Halaman View Result dan template cetak dokumen lab berhasil mengelompokkan hasil pemeriksaan dalam hierarki visual `DisciplineName` $\rightarrow$ `TestGroupName` $\rightarrow$ Item analit.
