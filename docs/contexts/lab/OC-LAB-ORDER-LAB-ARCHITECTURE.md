---
Title: Order Pemeriksaan Laboratorium Architecture
Code: OC-LAB-ORDER-LAB
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-10-15
---

# 1. Overview

## Realized Feature

Arsitektur ini merealisasikan spesifikasi fitur **[OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)** yang diturunkan dari outcome bisnis **[OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)** dan menindaklanjuti issue **[OC-LAB-ORDER-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ISSUE.md)** (`ISSUE-LAB-ORDER-LAB-001`).

Arsitektur ini menetapkan realisasi teknis komprehensif pada backend BILREG API (`b09-bilreg-api`):
1. **Penyelarasan Agregat `LabOrderModel`**:
   - Memperkaya agregat inti `LabOrderModel` dengan data pertanggungjawaban klinis: dokter perujuk (`DokterPerujukId`, `DokterPerujukName`), layanan perujuk (`LayananPerujukId`, `LayananPerujukName`), catatan klinis terpadu (`ClinicalNote`), dan tingkat urgensi pemeriksaan (`Urgency`: Rutin vs Cito).
2. **Penyelarasan Item Order `LabOrderItemModel`**:
   - Menambahkan penanda tingkat urgensi pada tingkat baris pemeriksaan (`Urgency`) dan properti turunan `IsCito`.
3. **Penerapan Dual-Path Validation**:
   - Jalur EMR internal: Memvalidasi keaktifan episode registrasi kunjungan ke domain Admission (`ADM-REG` / `_regRepo`) dan memvalidasi keabsahan dokter perujuk ke master tenaga medis (`ORG-PPA` / `_ppaRepo`).
   - Jalur Pasien Eksternal: Memvalidasi keaktifan registrasi transaksi eksternal ke `LAB-EXTERNAL` (`_labExternalRepo`), melakukan *auto-population* data perujuk dan catatan klinis dari `ReferralInfo`, serta melakukan bypass total terhadap master data internal RS guna menjamin isolasi data (*Master Isolation*).
4. **Peningkatan Visibilitas Worklist Operasional Laboratorium**:
   - Memperkaya DTO `LabOrderWorklistView` dan query DAL dengan penanda prioritas darurat `IsCito`, nama dokter perujuk, nama layanan perujuk, dan alasan penundaan (`DeferredReason`) agar order Cito dapat didahulukan dalam antrean tabung dan sampling.
5. **Penegakan Konsistensi Pembatalan & Guard Spesimen**:
   - Memperketat guard pembatalan: menolak pembatalan jika spesimen sudah diambil (`LabOrderStatus >= Collected`).
   - Melakukan koordinasi pelepasan beban biaya (*billing void / reversal*) ke Tata Rekening (`TRK-BILLING`) jika order yang dibatalkan telah berstatus `Charged` dan tagihan belum final/lunas di kasir.
6. **Protokol Peringatan Duplikasi 24 Jam**:
   - Menyediakan deteksi otomatis atas tes laboratorium berulang pada pasien yang sama dalam 24 jam terakhir dengan mekanisme konfirmasi klinis (*soft-warning* / overrideable via parameter `AllowDuplicateConfirmation`).
7. **Migrasi Skema Basis Data Non-Breaking**:
   - Menyediakan skrip DDL SQL `BILRG_LabOrder_M11_Alter.sql` untuk menambahkan kolom-kolom baru dengan default constraint, menjamin kompatibilitas penuh dan *zero-downtime* terhadap data historis.

## Referenced Artifacts

- Originating ISSUE: [OC-LAB-ORDER-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ISSUE.md)
- Domain Context: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md), [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md), [03-ADMISSION-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/03-ADMISSION-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)
- Target Outcome: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- Feature Specification: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- Analysis Input: [OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)

---

# 2. Architectural Basis

## Business Context

Pemeriksaan laboratorium merupakan prosedur diagnostik penunjang yang memerlukan instruksi resmi persisten dari klinisi berwenang (Dokter Perujuk / DPJP) atau permintaan mandiri pasien luar. Fakta order laboratorium yang diterbitkan menjadi fondasi hukum dan operasional bagi laboratorium untuk:
1. Menyiapkan tabung vacutainer dan melakukan pengambilan spesimen biologis (`LAB-COLLECT`).
2. Memicu pembentukan pembebanan biaya tindakan laboratorium pada akun penagihan pasien (`TRK-BILLING`).
3. Menjadi acuan scaffolding parameter uji analitik bagi verifikasi hasil laboratorium (`LAB-RESULT`).

Order laboratorium harus memuat akuntabilitas medis yang sah (siapa dokter perujuknya, unit layanan asal mana, apa indikasi klinisnya), tingkat urgensi (Rutin vs Cito), serta persyaratan kesiapan pasien (puasa / *Deferred*).

## Analysis Input

Arsitektur ini merealisasikan secara langsung seluruh keputusan penutupan celah (*Gap Closure Decisions*) yang telah disahkan pada [OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT.md):

- **GAP-001 (Konteks Perujuk & Catatan Klinis)**: Menambahkan atribut `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, dan `ClinicalNote` (penampung terpadu indikasi klinis & catatan dokter perujuk) pada agregat, DTO, dan tabel database.
- **GAP-002 & OQ-001 (Tingkat Urgensi Cito)**: Memperkenalkan `LabOrderUrgencyEnum` (`Rutin = 0`, `Cito = 1`). Urgensi dicatat pada header dan item. Jika terdapat $\ge 1$ item Cito, maka header order otomatis menjadi Cito (`IsCito = true`).
- **GAP-003 (Dual-Path Validasi Registrasi Kunjungan)**:
  - Jalur Internal: Validasi `RegId` aktif ke `ADM-REG` (`_regRepo`).
  - Jalur Eksternal: Validasi `RegExternalId` aktif ke `LAB-EXTERNAL` (`_labExternalRepo`), bypass `ADM-REG`.
- **GAP-004 (Dual-Path Validasi Dokter Perujuk)**:
  - Jalur Internal: Validasi `DokterPerujukId` ke `ORG-PPA` (`_ppaRepo`).
  - Jalur Eksternal: Data perujuk diturunkan dari `ReferralInfo` pada `LabExternalModel` (atau default APS), bypass `ORG-PPA`.
- **GAP-005 (Peningkatan Worklist Laboratorium)**: Memperkaya `LabOrderWorklistView` dengan `IsCito`, `DokterPerujukName`, `LayananPerujukName`, dan `DeferredReason`, serta filter prioritas Cito pada query DAL.
- **GAP-006 & OQ-002 (Integritas Pembatalan Order)**: Guard pembatalan menolak jika spesimen sudah diambil (`LabOrderStatus >= Collected`). Jika order berstatus `Charged`, koordinasikan pelepasan beban biaya ke Tata Rekening sebelum status diubah ke `Cancelled`.
- **GAP-007 & OQ-003 (Peringatan Duplikasi 24 Jam)**: Pemeriksaan kueri pendeteksi order berulang dalam 24 jam terakhir; melempar peringatan konfirmasi jika `AllowDuplicateConfirmation == false`.

```text
[07-LABORATORY-DOMAIN] + [OC-LAB-ORDER-LAB-FEATURE]
                         +
    [OC-LAB-ORDER-LAB-FEASIBILITY-ASSESSMENT]
                         ↓
       [OC-LAB-ORDER-LAB-ARCHITECTURE]
```

---

# 3. Scope

## Included

1. **Domain Layer (`Bilreg.Domain.LabContext.LabOrderFeature`)**:
   - Enumerasi: `LabOrderUrgencyEnum` (`Rutin = 0`, `Cito = 1`).
   - Perluasan `LabOrderModel`:
     - Properti: `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, `Urgency`, dan getter `IsCito => Urgency == LabOrderUrgencyEnum.Cito`.
     - Factory methods: `CreateFromEmr` dan `CreateExternal` diperbarui untuk menerima data perujuk, catatan klinis, dan urgensi.
     - Invariant: Guard pembatalan `Cancel(...)` menolak pembatalan jika `LabOrderStatus >= Collected` (`EX-03`).
   - Perluasan `LabOrderItemModel`:
     - Properti: `Urgency` (`LabOrderUrgencyEnum`) dan getter `IsCito`.
     - Factory method `Create` diperbarui untuk menerima `Urgency`.
2. **Application Layer (`Bilreg.Application.LabContext.LabOrderFeature`)**:
   - Use Case `LabOrderCreateFromEmrCmd`:
     - Parameter baru: `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, `Urgency`, `AllowDuplicateConfirmation`.
     - Handler menyuntikkan port `IRegRepo` (validasi aktif `RegId`), `IPpaRepo` (validasi aktif `DokterPerujukId`), dan `ILabOrderDal` (deteksi duplikasi 24 jam).
   - Use Case `LabOrderCreateExternalPatientCmd`:
     - Parameter baru: `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, `Urgency`, `AllowDuplicateConfirmation`.
     - Handler menyuntikkan `ILabExternalRepo` (validasi aktif `RegExternalId`), melakukan auto-populasi perujuk jika string kosong, dan bypass validasi `IRegRepo` / `IPpaRepo`.
   - Use Case `LabOrderCancelCmd`:
     - Handler memvalidasi status penagihan pada Tata Rekening sebelum pembatalan jika order berstatus `Charged`.
   - Use Case `LabOrderGetQuery`:
     - `LabOrderGetResponse` dan `LabOrderItemResponse` menyajikan field perujuk, `ClinicalNote`, `Urgency`, dan `IsCito`.
   - Use Case `LabOrderWorklistQuery`:
     - `LabOrderWorklistFilter` mendukung filter `IsCitoOnly`.
     - `LabOrderWorklistView` mengembalikan `IsCito`, `DokterPerujukName`, `LayananPerujukName`, dan `DeferredReason`.
3. **Infrastructure & Persistence Layer (`Bilreg.Infrastructure.LabContext.LabOrderFeature`)**:
   - DTO Mapping: `LabOrderDto` dan `LabOrderItemDto` diperbarui dengan kolom-kolom perujuk, catatan klinis, dan urgensi.
   - DAL: `LabOrderDal` dan `LabOrderItemDal` memperbarui query Dapper `Insert`, `Update`, `SelectByKey`, dan menambahkan query `HasRecentDuplicateOrder`.
   - Worklist DAL: `LabOrderWorklistDal` memperbarui query SQL untuk join/select data perujuk dan urgensi Cito.
4. **Database DDL (`Bilreg.SqlDb/LabContext/LabOrderFeature`)**:
   - Skrip migrasi DDL `BILRG_LabOrder_M11_Alter.sql` untuk tabel `BILRG_LabOrder` dan `BILRG_LabOrderItem`.
   - Non-clustered index untuk optimasi kueri order Cito pada tabel `BILRG_LabOrder`.
5. **Web API Controller (`Bilreg.Api/Controllers/LabContext/LabOrderFeature`)**:
   - Pembaruan controller `LabOrderController.cs` untuk mengekspos endpoint pembuatan order, kueri detail, dan worklist dengan model DTO yang diperkaya.

## Excluded

1. Pelaksanaan fisik flebotomi, penusukan vena, pencetakan barcode tabung spesimen (`LAB-COLLECT` / Outcome `OC-LAB-SAMPLE-COLLECTION`).
2. Entri hasil numerik analitik, penyesuaian rentang referensi, dan validasi medis dokter patologi klinik (`LAB-RESULT` / Outcome `OC-LAB-HASIL-LAB`).
3. Pendaftaran awal pasien eksternal dan penerbitan nomor RM sementara (`LAB-EXTERNAL` / Outcome `OC-LAB-REG-EXTERNAL` — kapabilitas ini sudah ada sebagai penyedia input bagi `LabOrder`).
4. Penghitungan rupiah tarif, skema diskon, dan transaksi kasir penerimaan uang (`TRK-BILLING`, `TRK-TARIF`, `TRK-KASIR`).

---

# 4. Technical Decisions

## AD-01: Aggregate Enrichment & Backward Compatibility

**Keputusan**: Mempertahankan agregat `LabOrderModel` yang telah ada pada namespace `Bilreg.Domain.LabContext.LabOrderFeature`, dan menambahkan atribut klinis baru (`DokterPerujuk`, `LayananPerujuk`, `ClinicalNote`, `Urgency`) dengan menyediakan konstruktor/factory method yang *backward-compatible*.

**Realisasi**:
- Menambahkan parameter baru dengan nilai default pada factory method `CreateFromEmr` dan `CreateExternal`.
- Properti baru dideklarasikan dengan tipe data primitif/string yang aman (`string.Empty` dan `LabOrderUrgencyEnum.Rutin` sebagai default).

## AD-02: Dual-Path Intake & Validation Strategy

**Keputusan**: Memisahkan alur validasi intake order ke dalam dua jalur tegas berdasarkan `OrderSource`:
1. **Jalur Internal RS (`LabOrderCreateFromEmrCmd`)**:
   - Mengambil data kunjungan dari `_regRepo.LoadEntity(RegModel.Key(cmd.RegId))`. Jika tidak ditemukan atau kunjungan sudah berstatus tutup (`Discharged` / `Cancelled`), sistem melempar `ValidationException` (Exception `EX-01`).
   - Memvalidasi bahwa `DokterPerujukId` terdaftar aktif pada master tenaga medis (`_ppaRepo.LoadEntity(...)`).
2. **Jalur Pasien Eksternal (`LabOrderCreateExternalPatientCmd`)**:
   - Mengambil data registrasi eksternal dari `_labExternalRepo.LoadEntity(LabExternalModel.Key(cmd.RegExternalId))`. Jika tidak ditemukan atau status bukan `Active`, sistem melempar `ValidationException`.
   - Mengisi `DokterPerujukName`, `LayananPerujukName`, dan `ClinicalNote` dari `LabExternalModel.ReferralInfo` apabila tidak diisi secara eksplisit pada payload command.
   - Melakukan bypass total terhadap pemeriksaan `_regRepo` dan `_ppaRepo`.

## AD-03: Hierarchical Cito Urgency Resolution

**Keputusan**: Urgensi order didefinisikan sebagai nilai hierarkis antara Header Order dan Item Baris Pemeriksaan:
- Header memiliki nilai `Urgency` (`LabOrderUrgencyEnum`).
- Setiap baris item memiliki nilai `Urgency` (`LabOrderUrgencyEnum`).
- **Aturan Konsistensi Invariant**:
  $$\text{Header.Urgency} = \begin{cases} \text{Cito}, & \text{jika } \exists \text{ item dengan } \text{item.Urgency} = \text{Cito} \lor \text{HeaderInput} = \text{Cito} \\ \text{Rutin}, & \text{lainnya} \end{cases}$$
- Di tingkat basis data, kolom `Urgency INT` disimpan di `BILRG_LabOrder` dan `BILRG_LabOrderItem`.
- Di tingkat worklist operasional, order Cito mendapatkan bendera boolean `IsCito = (Urgency == 1)` dan dapat difilter secara instan.

## AD-04: Non-Breaking Database Migration Strategy

**Keputusan**: Perubahan skema relasional diimplementasikan melalui skrip DDL `BILRG_LabOrder_M11_Alter.sql` dengan pendekatan non-breaking:
- Kolom baru dideklarasikan dengan klausul `NOT NULL CONSTRAINT DF_... DEFAULT(...)`.
- Nilai default untuk kolom string adalah `''` dan untuk enum `Urgency` adalah `0` (Rutin).
- Data historis yang sudah ada di basis data tetap valid tanpa perlu skrip backfill yang rumit atau downtime tabel.

## AD-05: Non-Destructive Cancellation & Cross-Domain Financial Compensation

**Keputusan**: Alur pembatalan order (`LabOrderCancelCmd`) mengintegrasikan validasi klinis dan kompensasi finansial:
1. **Validasi Status Spesimen**: Pada `LabOrderModel.Cancel(...)`, ditambahkan guard:
   ```csharp
   if (LabOrderStatus >= LabOrderStatusEnum.Collected)
       throw new InvalidOperationException($"LabOrder {OrderId} tidak dapat dibatalkan karena spesimen telah diambil (status {LabOrderStatus}).");
   ```
2. **Kompensasi Finansial Tata Rekening**: Pada `LabOrderCancelHandler`:
   - Jika order berstatus `Charged` dan memiliki referensi `BillingTindakanId`:
     - Handler memanggil repositori/service Tata Rekening (`ITataRekeningRepo`) untuk memeriksa status billing pasien.
     - Jika status billing pasien telah `Final` atau `Lunas`, pembatalan order laboratorium **ditolak** dengan melempar exception: *"Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas."*
     - Jika status billing masih terbuka (*open*), handler memicu pembatalan/void charge pada Tata Rekening sebelum memperbarui status order menjadi `Cancelled`.

## AD-06: Soft-Warning Protocol for 24-Hour Duplicate Test Orders

**Keputusan**: Untuk mencegah pemborosan biaya dan flebotomi ganda (Exception `EX-05`):
- Handler mengeksekusi kueri cepat pada `_labOrderDal.ListActiveTestsByPatientWithinHours(patientId, 24)`.
- Jika ditemukan tes laboratorium identik yang masih berstatus aktif (`Ordered`, `Deferred`, `Charged`, `Collected`) dan command membawa flag `AllowDuplicateConfirmation == false`:
  - Handler melempar `DuplicateLabOrderWarningException` yang berisi daftar nama tes yang duplikat beserta nomor order sebelumnya.
- Jika pengguna mengirimkan command dengan `AllowDuplicateConfirmation == true`, sistem melanjutkan proses persistensi order.

---

# 5. Component Responsibilities

| Komponen | Layer | Tanggung Jawab Utama |
|---|---|---|
| `LabOrderModel` | Domain | Menjaga invarian integritas order laboratorium, status siklus hidup, metadata perujuk, catatan klinis, urgensi Cito, aturan pembatalan non-destructive, dan koleksi baris item. |
| `LabOrderItemModel` | Domain | Menyimpan fakta rincian satu baris tes laboratorium, pemetaan ke definisi uji & tarif tindakan, tabung vacutainer, spesimen biologis, dan urgensi item. |
| `LabOrderUrgencyEnum` | Domain | Mendefinisikan nilai diskret tingkat urgensi: `Rutin = 1`, `Cito = 2`. |
| `LabOrderCreateFromEmrCmd` & `Handler` | Application | Menerima instruksi order klinis dari EMR RS, memvalidasi keaktifan kunjungan ke `ADM-REG` dan dokter perujuk ke `ORG-PPA`, memeriksa duplikasi 24 jam, dan mempersistensi order. |
| `LabOrderCreateExternalPatientCmd` & `Handler` | Application | Menerima instruksi order pasien luar, memvalidasi keaktifan registrasi ke `LAB-EXTERNAL`, melakukan auto-populasi perujuk/catatan klinis, memeriksa duplikasi, dan mempersistensi order. |
| `LabOrderCancelCmd` & `Handler` | Application | Memverifikasi ketiadaan spesimen yang telah diambil, memverifikasi status kasir di Tata Rekening, mengeksekusi kompensasi void tagihan jika perlu, dan mengubah status menjadi `Cancelled`. |
| `LabOrderGetQuery` & `Handler` | Application | Mengambil data detail lengkap suatu order laboratorium termasuk rincian perujuk, catatan klinis, dan urgensi. |
| `LabOrderWorklistQuery` & `Handler` | Application | Mengambil daftar kerja operasional laboratorium terfilter dengan visibilitas status darurat Cito dan identitas perujuk. |
| `ILabOrderRepo` & `LabOrderRepo` | Domain / Infra | Menyediakan abstraksi dan implementasi persistensi agregat `LabOrderModel`. |
| `ILabOrderDal` & `LabOrderDal` | Application / Infra | Menyediakan eksekusi kueri Dapper tingkat baris untuk persistensi tabel `BILRG_LabOrder` dan deteksi order duplikat 24 jam. |
| `ILabOrderItemDal` & `LabOrderItemDal` | Application / Infra | Menyediakan eksekusi kueri Dapper untuk baris rincian pada tabel `BILRG_LabOrderItem`. |
| `ILabOrderWorklistDal` & `LabOrderWorklistDal` | Application / Infra | Menyediakan eksekusi kueri SQL teroptimasi untuk worklist operasional laboratorium dengan filter Cito. |
| `LabOrderController` | API | Menyediakan endpoint RESTful Web API untuk pembuatan order, pembatalan, pembacaan detail, dan worklist operasional. |

---

# 6. Integration Design

| Komponen Sumber (Source) | Komponen Tujuan (Target) | Protokol / Port | Tujuan Integrasi |
|---|---|---|---|
| `LabOrderCreateFromEmrHandler` | `IRegRepo` (`ADM-REG`) | In-Memory C# Interface | Validasi bahwa `RegId` kunjungan berstatus aktif dan belum ditutup/discharged (Exception `EX-01`). |
| `LabOrderCreateFromEmrHandler` | `IPpaRepo` (`ORG-PPA`) | In-Memory C# Interface | Validasi bahwa `DokterPerujukId` terdaftar aktif sebagai tenaga medis berwenang (Condition 5.3 #2). |
| `LabOrderCreateExternalPatientHandler` | `ILabExternalRepo` (`LAB-EXTERNAL`) | In-Memory C# Interface | Validasi bahwa `RegExternalId` terdaftar aktif dan auto-populasi data perujuk luar dari `ReferralInfo`. |
| `LabOrderCreateFromEmrHandler` / `External` | `ILabTestResolutionService` | In-Memory C# Service | Resolusi pemetaan `TarifId` $\rightarrow$ `TestDefinitionId`, tipe tabung vacutainer, jenis spesimen, dan komponen uji. |
| `LabOrderCancelHandler` | `ITataRekeningRepo` / `ChargeContext` | In-Memory C# Interface | Verifikasi status tagihan kasir dan eksekusi kompensasi pelepasan charge (`VoidTindakan`) jika order dibatalkan pasca-charge. |
| `LabOrderController` | Frontend Web / EMR Modul | HTTP REST (JSON) | Pertukaran data transaksi pemesanan tes lab, pembatalan, pemantauan worklist, dan pengambilan detail order. |

---

# 7. Data Ownership

| Entitas Data | Pemilik Otoritatif (Authoritative Owner) | Keterangan Batasan Kepemilikan |
|---|---|---|
| Catatan Permintaan Laboratorium (`BILRG_LabOrder`, `BILRG_LabOrderItem`) | `LAB-ORDER` (`LabOrderFeature`) | Agregat `LabOrderModel` adalah pemilik tunggal dari fakta pemesanan tes laboratorium, status kesiapan (*Ready/Deferred*), urgensi Cito, dan jejak pembatalan. |
| Data Sosial & Demografi Pasien (`PAS-DATSOS`) | Pasien Domain (`PAS`) | `LabOrder` hanya menyimpan snapshot demografi baca-saja (*read-only snapshot*) pada saat order dibuat. |
| Episode Kunjungan Rumah Sakit (`ADM-REG`) | Admission Domain (`ADM`) | `LabOrder` hanya membaca status kunjungan untuk otorisasi order klinis internal RS. |
| Pendaftaran Eksternal Laboratorium (`BILRG_LabExternal`) | `LAB-EXTERNAL` (`LabExternalFeature`) | `LabOrder` hanya membaca dan mengaitkan `RegExternalId` sebagai konteks kunjungan pasien luar. |
| Definisi Uji & Spesimen Lab (`BILRG_LabTestDefinition`) | `LabTestDefinitionFeature` | `LabOrder` mengonsumsi definisi katalog tes untuk memetakan vacutainer dan komponen analitik. |
| Akun Tagihan & Beban Biaya (`BILRG_Tindakan`, `TataRekening`) | Tata Rekening (`TRK`) | `LabOrder` hanya menyimpan referensi `BillingTindakanId` hasil pembebanan biaya tindakan laboratorium. |

---

# 8. Database Design

## Modified Tables

### 1. `BILRG_LabOrder`
Tabel penyimpan data header transaksi pesanan laboratorium:

| Kolom Baru | Tipe Data | Nullable | Nilai Default | Keterangan |
|---|---|---|---|---|
| `DokterPerujukId` | `VARCHAR(12)` | NO | `''` | Identifier dokter perujuk (`ORG-PPA` untuk internal RS, atau kosong untuk luar/APS). |
| `DokterPerujukName` | `VARCHAR(100)` | NO | `''` | Nama lengkap dokter perujuk (internal RS atau dokter luar). |
| `LayananPerujukId` | `VARCHAR(10)` | NO | `''` | Identifier layanan perujuk (`ORG-LAYANAN` untuk internal RS, atau kosong untuk luar). |
| `LayananPerujukName` | `VARCHAR(100)` | NO | `''` | Nama klinik/ruangan pengirim atau instansi faskes luar. |
| `ClinicalNote` | `VARCHAR(500)` | NO | `''` | Catatan klinis terpadu: memuat indikasi medis dan instruksi khusus dokter perujuk. |
| `Urgency` | `INT` | NO | `0` | Tingkat urgensi order: `0 = Rutin`, `1 = Cito (STAT / Emergency)`. |

### 2. `BILRG_LabOrderItem`
Tabel penyimpan baris rincian pemeriksaan laboratorium:

| Kolom Baru | Tipe Data | Nullable | Nilai Default | Keterangan |
|---|---|---|---|---|
| `Urgency` | `INT` | NO | `0` | Tingkat urgensi per item pemeriksaan: `0 = Rutin`, `1 = Cito`. |

## Database Indexing

Untuk mengoptimalkan kueri worklist laboratorium dan penyaringan order darurat (*Cito*):
```sql
CREATE INDEX IX_BILRG_LabOrder_Urgency 
ON BILRG_LabOrder(Urgency, LabOrderStatus, CrtDate);
GO

CREATE INDEX IX_BILRG_LabOrder_Perujuk 
ON BILRG_LabOrder(DokterPerujukId, LayananPerujukId) 
WHERE DokterPerujukId <> '';
GO
```

## Migration Considerations (DDL Script)

Skrip migrasi database disiapkan dalam file `BILRG_LabOrder_M11_Alter.sql`:
```sql
-- BILRG_LabOrder_M11_Alter.sql
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrder') AND name = 'DokterPerujukId')
BEGIN
    ALTER TABLE BILRG_LabOrder ADD DokterPerujukId VARCHAR(12) NOT NULL CONSTRAINT DF_BILRG_LabOrder_DokterPerujukId DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD DokterPerujukName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrder_DokterPerujukName DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD LayananPerujukId VARCHAR(10) NOT NULL CONSTRAINT DF_BILRG_LabOrder_LayananPerujukId DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD LayananPerujukName VARCHAR(100) NOT NULL CONSTRAINT DF_BILRG_LabOrder_LayananPerujukName DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD ClinicalNote VARCHAR(500) NOT NULL CONSTRAINT DF_BILRG_LabOrder_ClinicalNote DEFAULT('');
    ALTER TABLE BILRG_LabOrder ADD Urgency INT NOT NULL CONSTRAINT DF_BILRG_LabOrder_Urgency DEFAULT(0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('BILRG_LabOrderItem') AND name = 'Urgency')
BEGIN
    ALTER TABLE BILRG_LabOrderItem ADD Urgency INT NOT NULL CONSTRAINT DF_BILRG_LabOrderItem_Urgency DEFAULT(0);
END;
GO
```
Skrip ini bersifat idempotent dan dapat dijalankan berulang kali tanpa risiko kesalahan duplikasi kolom.

---

# 9. Cross-Cutting Concerns

1. **Audit Trail**:
   - Setiap mutasi order (`Create`, `Defer`, `Activate`, `Cancel`) mencatat `UserId` dan timestamp secara persisten pada `AuditTrailType` (`CrtUser`, `CrtDate`, `UpdUser`, `UpdDate`, `VodUser`, `VodDate`).
   - Pembatalan order mencatat `CancelledUserId`, `CancelledDate`, dan `CancelledReason`.
2. **Concurrency & Data Consistency**:
   - Menggunakan transaksi atomik (`TransHelper.NewScope()`) pada seluruh alur pembuatan dan pembatalan order.
   - Pengecekan status guard pada domain model menjamin operasi mutasi terhindar dari kondisi balapan (*race conditions*).
3. **Query Performance (Worklist Scalability)**:
   - Kueri daftar kerja operasional laboratorium (`ILabOrderWorklistDal`) menggunakan indeks komposit pada `LabOrderStatus`, `Urgency`, dan `CrtDate` sehingga pencarian order aktif dan Cito berlangsung instan dalam skala ribuan transaksi harian.

---

# 10. Implementation Constraints

1. **Target Framework & Language**: .NET 8 dengan C# 12.
2. **Pattern & Architecture**: Domain-Driven Design (DDD) & Clean Architecture dengan pemisahan tegas antara Domain, Application, Infrastructure, dan Web API layer.
3. **Command & Query Handling**: Memanfaatkan `MediatR` untuk pemisahan Command (mutasi) dan Query (pembacaan).
4. **Data Access Technology**:
   - Dapper untuk Micro-ORM berkecepatan tinggi pada layer infrastruktur.
   - Tanpa Entity Framework.
5. **Backwards Compatibility**:
   - Seluruh endpoint API eksisting harus tetap berjalan tanpa memutus integrasi klien web atau EMR yang telah berjalan.
6. **Code Quality**:
   - Menjaga nol (*zero*) compiler warning.
   - Unit test dan handler test wajib mencakup skenario sukses, validasi batas, penolakan Cito, dan exception EX-01 s/d EX-05.

---

# 11. Acceptance Conditions

Implementasi dianggap lengkap secara arsitektural apabila:
1. Skrip migrasi DDL `BILRG_LabOrder_M11_Alter.sql` berhasil dieksekusi pada database target dan terdaftar pada proyek `Bilreg.SqlDb.sqlproj`.
2. Model domain `LabOrderModel` dan `LabOrderItemModel` memuat dan memvalidasi `DokterPerujuk`, `LayananPerujuk`, `ClinicalNote`, serta `Urgency` (Rutin/Cito).
3. Use case `LabOrderCreateFromEmrCmd` berhasil memvalidasi keaktifan kunjungan ke `ADM-REG`, memvalidasi dokter perujuk ke `ORG-PPA`, mendeteksi duplikasi tes 24 jam, dan menyimpan transaksi order.
4. Use case `LabOrderCreateExternalPatientCmd` berhasil memvalidasi keaktifan `RegExternalId` ke `LAB-EXTERNAL`, melakukan auto-populasi perujuk luar, dan melakukan bypass validasi master data internal RS.
5. Use case `LabOrderCancelCmd` menolak pembatalan jika spesimen telah diambil (`>= Collected`), serta menyinkronkan pembatalan beban billing ke Tata Rekening jika order telah berstatus `Charged`.
6. Antarmuka kueri `LabOrderWorklistDal` menyajikan kolom `IsCito`, nama perujuk, layanan perujuk, dan mendukung filter prioritas order Cito.
7. Seluruh pengujian otomatis (*unit & integration tests*) pada `Bilreg.Test/LabContext/LabOrderFeature` lulus 100%.
