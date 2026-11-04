---
Title: Feasibility Assessment - Order Pemeriksaan Laboratorium
Code: OC-LAB-ORDER-LAB
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-10-15
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan perubahan (*CHANGE-REQUEST*) untuk mengimplementasikan dan menyelaraskan kapabilitas pencatatan, validasi, dan pemeliharaan permintaan pemeriksaan laboratorium (*laboratory examination order*) sebagai fakta bisnis persisten (*persisted business fact*) sesuai spesifikasi kanonikal [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md).

Referenced artifacts:

- ISSUE: [OC-LAB-ORDER-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ISSUE.md) (`ISSUE-LAB-ORDER-LAB-001`)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md), [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md), [03-ADMISSION-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/03-ADMISSION-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)
- OUTCOME: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- FEATURE: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)

## Objective

Menilai kelayakan teknis, fungsional, dan arsitektural dari implementasi Outcome `OC-LAB-ORDER-LAB` pada backend Bilreg (`b09-bilreg-api`); mengevaluasi sejauh mana codebase awal pada [Bilreg.Domain/LabContext/LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature) dapat dipertahankan atau perlu disesuaikan (refactored); mengidentifikasi kesenjangan (*gaps*), pertanyaan terbuka (*open questions*), asumsi, dan risiko; serta merumuskan keputusan penutupan celah (*Gap Closure Decisions*) dengan menerapkan kesepakatan ubiquitous language (`Perujuk`, `LayananPerujuk`, `ClinicalNote`) sebelum perancangan arsitektur teknis difinalisasi oleh Arsitek.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan audit artefak dokumen dan penelusuran kode sumber backend `b09-bilreg-api`:

## Existing Behavior

1. **Struktur Model Agregat Eksisting ([`LabOrderModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs))**:
   - Telah terbentuk agregat `LabOrderModel` yang mengelola entitas order dengan properti: `OrderId`, `EmrOrderId`, `OrderNo`, `OrderSource`, `LabOrderStatus`, `LastBillingReleaseStatus`, `OwareStatus`, `Patient` (`PatientSnapshotType`), `ExecutionRegId`, `DeferredInfo`, `BillingTindakanId`, `BillingLastError`, `CollectionInfo`, metadata rilis/verifikasi, audit trail pembatalan (`CancelledReason`, `CancelledDate`, `CancelledUserId`), dan koleksi rincian `Items` ([`LabOrderItemModel`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemModel.cs)) serta `ItemComponents` ([`LabOrderItemComponentModel`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemComponentModel.cs)).
   - Telah tersedia layanan resolusi katalog otomatis ([`LabTestResolutionService.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs)) yang memetakan item masukan tarif (`TarifId`) ke definisi uji laboratorium ([`LabTestDefinitionFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabTestDefinitionFeature)), menentukan tipe tabung vacutainer (`VacutainerTypeEnum`), jenis spesimen biologis (`SpecimenType`), dan rincian parameter komponen uji.
   - Status siklus hidup order didefinisikan pada [`LabOrderStatusEnum.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderStatusEnum.cs): `Ordered (1)`, `Deferred (2)`, `Charged (3)`, `Collected (4)`, `Recorded (5)`, `Verified (6)`, `Released (7)`, `Cancelled (8)`, `Terminated (9)`.
   - Logika penundaan kesiapan pasien (`Defer`) dan aktivasi kembali (`ActivateFromDeferred`) telah diimplementasikan dengan penguncian guard status yang rapi.
   - Perintah pembatalan (`Cancel`) telah merekam alasan pembatalan dan identitas petugas pembatal, serta membatasi pembatalan hanya dari status `Ordered`, `Deferred`, atau `Charged`.

2. **Alur Perintah Intake ([`LabOrderCreateFromEmrCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs) & [`LabOrderCreateExternalPatientCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateExternalPatientCmd.cs))**:
   - `LabOrderCreateFromEmrCmd` menerima `EmrOrderId`, `RegId`, `PatientId`, `PatientName`, `BirthDateYmd`, `Gender`, dan daftar `Items`. Handler ini langsung membentuk snapshot pasien dan menyimpan entitas order ke `_labOrderRepo`. Tidak terdapat validasi keaktifan registrasi ke `ADM-REG` atau pencatatan identitas dokter perujuk maupun layanan perujuk.
   - `LabOrderCreateExternalPatientCmd` telah direfaktor sebelumnya untuk mendukung parameter `RegExternalId` dan `ExternalMrNo` dari kapabilitas registrasi eksternal (`LAB-EXTERNAL`).

3. **Kueri Daftar Kerja ([`LabOrderWorklistQuery.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderWorklistQuery.cs) & [`ILabOrderWorklistDal.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/ILabOrderWorklistDal.cs))**:
   - Menyediakan pencarian daftar order berdasarkan status, teks pencarian (nama/no order/RM), dan rentang tanggal.
   - View model `LabOrderWorklistView` mengembalikan data pasien, status, jumlah item, dan waktu pembuatan order.

4. **Struktur Basis Data Relasional ([`BILRG_LabOrder.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrder.sql) & [`BILRG_LabOrderItem.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrderItem.sql))**:
   - Tabel `BILRG_LabOrder` menyimpan header transaksi order laboratorium.
   - Tabel `BILRG_LabOrderItem` menyimpan baris rincian item tes.
   - Tabel `BILRG_LabOrderItemComponent` menyimpan parameter uji per item.

## Existing Constraints

1. **Domain Boundary Alignment**: Outcome `OC-LAB-ORDER-LAB` berfokus pada eksistensi fakta pesanan laboratorium (`LAB-ORDER`). Pengambilan fisik spesimen merupakan wewenang `LAB-COLLECT` (`OC-LAB-SAMPLE-COLLECTION`), verifikasi hasil analitik merupakan wewenang `LAB-RESULT` (`OC-LAB-HASIL-LAB`), dan perhitungan biaya merupakan wewenang `TRK-BILLING`.
2. **Keterikatan Kunjungan Sah**: Order wajib terikat pada tepat satu nomor registrasi kunjungan aktif (`RegId` pada `ADM-REG` atau `RegExternalId` pada `LAB-EXTERNAL`).
3. **Akuntabilitas Medis PPA**: Setiap instruksi pemeriksaan harus merekam identitas dokter perujuk berwenang (`ORG-PPA`) dan unit layanan perujuk (`ORG-LAYANAN`).
4. **Proteksi Immutability Pasca-Sampling**: Rincian item pemeriksaan terkunci permanen begitu spesimen telah diambil (`SampleCollection` selesai).
5. **Non-Destructive Cancellation**: Pembatalan dilakukan via *soft-cancel* berstatus `Cancelled` disertai jejak audit lengkap (waktu, user, alasan).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| **GAP-001** | CRITICAL | **Ketiadaan Konteks Dokter Perujuk, Layanan Perujuk, dan Catatan Klinis (`ClinicalNote`) pada Agregat & Skema Basis Data.** Model `LabOrderModel`, tabel `BILRG_LabOrder`, command intake, dan query get/worklist belum memiliki field: `DokterPerujukId`, `DokterPerujukName` (`ORG-PPA`), `LayananPerujukId`, `LayananPerujukName` (`ORG-LAYANAN`), dan `ClinicalNote` (menggabungkan indikasi klinis dan catatan dokter perujuk). Hal ini melanggar AC-02 dan Business Fact 5.1 #3 pada `OC-LAB-ORDER-LAB`. |
| **GAP-002** | CRITICAL | **Ketiadaan Penetapan Tingkat Urgensi / Prioritas (Rutin vs Cito) pada Header Order, Item Order, Skema Basis Data, dan Worklist.** Tidak ada enum atau field `Urgency` / `IsCito` di `LabOrderModel`, `LabOrderItemModel`, `BILRG_LabOrder`, `BILRG_LabOrderItem`, maupun `LabOrderWorklistView`. Hal ini melanggar AC-04, Business Fact 5.1 #5, dan Business Condition 5.3 #4. |
| **GAP-003** | MAJOR | **Ketiadaan Validasi Keaktifan Registrasi Kunjungan Pasien (`RegId` / `RegExternalId`) saat Pembuatan Order.** Command `LabOrderCreateFromEmrCmd` menerima `RegId` tanpa memeriksa keaktifan episode kunjungan pada `ADM-REG` (`_regRepo`). Pembuatan order pada episode kunjungan yang sudah tutup (*discharged*) atau dibatalkan seharusnya ditolak sesuai Exception EX-01. |
| **GAP-004** | MAJOR | **Ketiadaan Validasi Kredensial dan Keaktifan Dokter Perujuk terhadap Master PPA (`ORG-PPA`).** Tidak ada verifikasi apakah dokter perujuk yang meminta pemeriksaan terdaftar aktif dan memiliki kewenangan klinis sesuai Business Condition 5.3 #2. |
| **GAP-005** | MAJOR | **Ketiadaan Informasi Klinis & Urgensi Cito pada Tampilan Worklist Operasional Laboratorium.** View model `LabOrderWorklistView` dan `ILabOrderWorklistDal` tidak menyajikan kolom `IsCito`, `DokterPerujukName`, `LayananPerujukName`, dan informasi penundaan `DeferredReason`. Akibatnya petugas lab tidak dapat mengenali order darurat secara visual untuk didahulukan dalam antrean tabung dan sampling. |
| **GAP-006** | MAJOR | **Inkonsistensi Alur Pembatalan Order (`LabOrderCancelCmd`) Terhadap Status Penagihan (`TRK-BILLING`) dan Status Pengambilan Spesimen.** Handler pembatalan belum memeriksa apakah spesimen telah diambil (`SampleCollection` selesai, melanggar EX-03) dan belum menyinkronkan status pembatalan beban biaya pada `TRK-BILLING` jika order telah berstatus `Charged`. |
| **GAP-007** | MINOR | **Ketiadaan Peringatan Konfirmasi Potensi Tes Duplikat dalam Kurun 24 Jam (EX-05).** Belum ada query atau validasi pendeteksi order aktif berulang untuk tes yang sama pada pasien yang sama dalam 24 jam terakhir guna mencegah pengambilan spesimen berulang yang tidak diperlukan. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| **OQ-001** | Bagaimana representasi granularitas prioritas (Rutin vs Cito)—apakah diatur pada tingkat Header Order, tingkat per Item Pemeriksaan, atau keduanya? | Menentukan apakah penandaan Cito disimpan pada `BILRG_LabOrder`, `BILRG_LabOrderItem`, atau keduanya, serta bagaimana alokasi worklist laboratorium memprioritaskannya. |
| **OQ-002** | Bagaimana sinkronisasi pembatalan order (`LabOrderCancelCmd`) yang sudah terlanjur berstatus `Charged` terhadap akun penagihan di Tata Rekening (`TRK-BILLING`)? | Menentukan apakah `LabOrderCancelHandler` wajib memanggil service pembebasan/void tagihan di Tata Rekening, atau membatasi pembatalan jika episode penagihan di Tata Rekening sudah `Final` / `Lunas`. |
| **OQ-003** | Apakah validasi duplikasi tes 24 jam (EX-05) bersifat pemblokiran mutlak (*hard-block*) atau peringatan konfirmasi klinis (*soft-warning / overrideable*)? | Mempengaruhi perancangan kontrak API pada command intake (apakah memerlukan flag konfirmasi `AllowDuplicateConfirmation`). |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| **ASM-001** | Bounded context `LabExternalFeature` (`LAB-EXTERNAL`) yang telah dibangun sebelumnya menyediakan identifier resmi `RegExternalId` dan nomor identitas operasional sekali pakai `ExternalMrNo` yang sah untuk diikat oleh `LabOrderFeature`. |
| **ASM-002** | Master tenaga medis (`ORG-PPA`) dan master layanan perujuk (`ORG-LAYANAN`) di `Bilreg.Domain` / `b09-bilreg-api` menyediakan referensi lookup yang dapat diakses oleh `LabContext` untuk validasi kredensial dokter perujuk dan layanan perujuk. |
| **ASM-003** | Domain Tata Rekening (`TRK-BILLING` dan `TRK-TARIF`) menyediakan port integrasi untuk pembebanan biaya tindakan laboratorium berdasarkan baris rincian tarif yang divalidasi pada order lab. |
| **ASM-004** | Status lanjutan operasional pasca-order (`Collected`, `Recorded`, `Verified`, `Released`) yang sudah ada pada `LabOrderModel` tetap dipertahankan sebagai status operasional LWF untuk menjaga keselarasan dengan subsistem LIS/LWF yang sudah berjalan, tanpa mengurangi fokus outcome `OC-LAB-ORDER-LAB` pada pembentukan fakta order awal (`Ordered`, `Deferred`, `Cancelled`). |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| **RISK-001** | Penambahan kolom baru pada tabel `BILRG_LabOrder` dan `BILRG_LabOrderItem` berpotensi menimbulkan *downtime* atau kegagalan migrasi jika terdapat data historis tanpa nilai default. | CRITICAL | Menggunakan skrip SQL alter DDL dengan deklarasi eksplisit `NOT NULL CONSTRAINT DF_... DEFAULT('')` atau `DEFAULT(0)`, menjamin integritas penuh dan *zero-breakage* pada data eksisting. |
| **RISK-002** | Perubahan signature konstruktor dan factory method pada `LabOrderModel` berpotensi mematahkan puluhan unit test eksisting pada `Bilreg.Test/LabContext/LabOrderFeature`. | MAJOR | Menggunakan teknik constructor overloading atau memperbarui `LabOrderTestSupport.cs` secara terpusat untuk menyediakan nilai default yang valid bagi seluruh test fixture. |
| **RISK-003** | Keterlambatan penanganan pasien darurat jika label prioritas 'Cito' tidak mencolok pada antarmuka daftar kerja laboratorium. | MAJOR | Menambahkan properti `IsCito` secara eksplisit pada `LabOrderWorklistView` dan menyertakan kueri filter khusus order Cito pada query dal. |

---

# 7. Recommendations

## Option A: Mempertahankan dan Memperkaya Fitur `LabOrderFeature` Eksisting (Direkomendasikan)

Mempertahankan fondasi kode yang telah ada pada `Bilreg.Domain.LabContext.LabOrderFeature`, dan melakukan pembaruan terukur:
1. Menambahkan kolom dan properti klinis: `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote` (sebagai penampung tunggal indikasi klinis dan catatan dokter perujuk), serta `Urgency` (Rutin/Cito) pada `LabOrderModel`, `LabOrderItemModel`, dan tabel SQL.
2. Memperbarui `LabOrderCreateFromEmrCmd` dan `LabOrderCreateExternalPatientCmd` agar menerima data dokter perujuk, layanan perujuk, catatan klinis (`ClinicalNote`), dan tingkat urgensi.
3. Memperkenalkan guard validasi keaktifan registrasi (`ADM-REG` via `_regRepo`) dan keabsahan dokter perujuk (`ORG-PPA`).
4. Memperkaya kueri `LabOrderWorklistDal` dan `LabOrderWorklistView` dengan penanda prioritas Cito, nama dokter perujuk, dan nama layanan perujuk.
5. Memperketat aturan pembatalan order (`Cancel`) dengan memvalidasi guard status spesimen (`SampleCollection`) dan sinkronisasi pelepasan beban biaya billing.

### Advantages

- Mempertahankan komponen yang sudah matang dan teruji: `LabTestResolutionService` (pemetaan otomatis vacutainer dan komponen tes), `LabOrderRepo`, alur `Deferred`, dan arsitektur MediatR.
- Menjaga kompatibilitas mundur (*backwards compatibility*) dengan endpoint API dan subsistem LWF yang sudah memanfaatkan `LabOrderModel`.
- Efisiensi waktu dan minim risiko regresi jika dibandingkan dengan menulis ulang dari nol.
- Struktur data lebih ringkas dan intuitif dengan menggabungkan catatan pengorder ke dalam `ClinicalNote`.

### Disadvantages

- Memerlukan pembaruan skrip migrasi database SQL DDL (`M11_Alter`) dan pembaruan beberapa file test fixture.

## Option B: Menulis Ulang Fitur `LabOrder` secara Terpisah Tanpa Siklus Hidup Lanjutan

Membangun agregat baru yang hanya mencatat status `Ordered`, `Deferred`, dan `Cancelled`, membuang status `Charged`, `Collected`, `Recorded`, `Verified`, `Released`.

### Advantages

- Pemisahan tanggung jawab yang sepenuhnya murni sesuai dokumen spesifikasi outcome awal.

### Disadvantages

- Menghancurkan alur operasional LWF eksisting yang saat ini menggunakan `LabOrderModel` sebagai jangkar status pemeriksaan laboratorium.
- Memerlukan perombakan besar-besaran pada modul `LabResultFeature`, `LabOwareFeature`, dan antarmuka web LWF eksisting.
- Biaya refaktorisasi sangat tinggi tanpa memberikan nilai tambah fungsional yang signifikan bagi pengguna.

---

# 8. Gap Closure

## GAP-001: Ketiadaan Konteks Dokter Perujuk, Layanan Perujuk, dan Catatan Klinis (`ClinicalNote`)
- **Status:** CLOSED
- **Decision:** Menambahkan field `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, dan `ClinicalNote` (menggabungkan indikasi klinis dan instruksi/catatan dokter perujuk ke dalam satu field tunggal) pada `LabOrderModel`, tabel `BILRG_LabOrder`, DTO `LabOrderDto`, `LabOrderCreateFromEmrCmd`, `LabOrderCreateExternalPatientCmd`, dan `LabOrderGetResponse`. Kolom terpisah `CatatanPengorder` ditiadakan.
- **Rationale:** Mengadopsi kesepakatan ubiquitous language yang konsisten (`Perujuk` untuk klinisi pengirim, `LayananPerujuk` untuk unit pengirim) dan menyederhanakan dokumentasi klinis ke dalam satu atribut `ClinicalNote` sesuai hasil Gap Discussion.
- **Impact:** Perubahan pada model domain, skrip DDL SQL alter, DTO input/output, dan mapper DAL.
- **Architecture Impact:** Penambahan value/properti klinis terstandardisasi pada agregat `LabOrderModel`.
- **Resolved By:** Analyst (Konsisten dengan Gap Discussion & Alignment)
- **Resolved Date:** 2026-10-15

---

## GAP-002: Ketiadaan Penetapan Tingkat Urgensi / Prioritas (Rutin vs Cito)
- **Status:** CLOSED
- **Decision:** Membuat enum `LabOrderUrgencyEnum` (`Rutin = 0`, `Cito = 1`). Menambahkan properti `Urgency` pada `LabOrderModel` (header) dan `LabOrderItemModel` (item). Menambahkan kolom `Urgency` pada `BILRG_LabOrder` dan `BILRG_LabOrderItem` dengan default constraint `DEFAULT(0)`. Jika salah satu item berstatus Cito, maka header order otomatis menjadi Cito. Menambahkan flag boolean `IsCito => Urgency == LabOrderUrgencyEnum.Cito` pada response query dan worklist view.
- **Rationale:** Memenuhi kriteria AC-04 dan Business Fact 5.1 #5; memastikan laboratorium dapat mendeteksi dan memprioritaskan order darurat/cito secara otomatis.
- **Impact:** Penambahan enum baru, perubahan skema basis data, dan penyesuaian kueri worklist.
- **Architecture Impact:** Modifikasi struktur domain `LabOrderFeature` dengan enum urgensi baru.
- **Resolved By:** Analyst (Konsisten dengan OC-LAB-ORDER-LAB Section 5.1 #5 & AC-04)
- **Resolved Date:** 2026-10-15

---

## GAP-003: Ketiadaan Validasi Keaktifan Registrasi Kunjungan Pasien
- **Status:** CLOSED
- **Decision:** Menerapkan validasi keaktifan kunjungan berbasis *dual-path* sesuai asal order (`OrderSource`):
  1. **Order Internal RS (`OrderSource.Emr`)**: Menyuntikkan port `IRegRepo` (`ADM-REG`) untuk memvalidasi bahwa `RegId` terdaftar aktif dan status kunjungannya belum ditutup (*not discharged/closed*). Jika tidak aktif atau tidak ditemukan, sistem melempar validasi domain Exception EX-01.
  2. **Order Eksternal Laboratorium (`OrderSource.ExternalPatient`)**: Menyuntikkan port `ILabExternalRepo` (`LAB-EXTERNAL`) untuk memvalidasi bahwa `RegExternalId` terdaftar dan berstatus `LabExternalStatusEnum.Active`. Validasi terhadap `ADM-REG` di-bypass total guna menjamin isolasi master data kunjungan RS.
- **Rationale:** Memenuhi aturan Exception EX-01 dengan menghormati batas kedaulatan domain: pasien internal memvalidasi kunjungan admisi reguler, sedangkan pasien luar memvalidasi registrasi transaksi eksternal mandiri.
- **Impact:** Penambahan dependency check pada application handler masing-masing alur pembuatan order.
- **Architecture Impact:** Penegasan boundary validasi konsumsi antar-domain antara `LAB` dan `ADM` / `LAB-EXTERNAL`.
- **Resolved By:** Analyst (Konsisten dengan Gap Discussion & OC-LAB-ORDER-LAB EX-01)
- **Resolved Date:** 2026-10-15

---

## GAP-004: Ketiadaan Validasi Kredensial dan Keaktifan Dokter Perujuk
- **Status:** CLOSED
- **Decision:** Menerapkan aturan validasi dokter perujuk berbasis konteks kepemilikan klinis (*clinical ownership context*):
  1. **Order Internal RS (`OrderSource.Emr`)**: `DokterPerujukId` wajib diisi dan divalidasi status aktifnya terhadap repository master PPA rumah sakit (`ORG-PPA`). `LayananPerujukId` wajib divalidasi ke master layanan (`ORG-LAYANAN`).
  2. **Order Eksternal Laboratorium (`OrderSource.ExternalPatient`)**: Validasi terhadap master PPA (`ORG-PPA`) dan master layanan (`ORG-LAYANAN`) internal RS **di-bypass** dengan ketentuan:
     - **Kasus APS (Atas Permintaan Sendiri)**: Pasien datang mandiri tanpa rujukan dokter. Atribut diisi nilai default: `DokterPerujukId = ""` (atau `"APS"`), `DokterPerujukName = "Atas Permintaan Sendiri (APS)"`, dan `LayananPerujukName = "Laboratorium (Pendaftaran Mandiri)"`.
     - **Kasus Rujukan Fasilitas Luar (Faskes Luar)**: Pasien membawa surat rujukan dokter/klinik luar. Nilai `DokterPerujukName`, `LayananPerujukName` (instansi perujuk), dan `ClinicalNote` secara otomatis diturunkan (*auto-populated*) dari `ReferralInfo` pada agregat `LabExternalModel` (`ReferralDoctor`, `ReferralOrigin`, `ClinicalNotes`). `DokterPerujukId` dan `LayananPerujukId` bernilai string kosong karena berada di luar master organisasi internal RS.
- **Rationale:** Memenuhi Business Condition 5.3 #2 tanpa menghambat operasional pasien luar yang sah (APS maupun rujukan dokter luar) yang dokternya memang tidak terdaftar dalam struktur kepegawaian internal rumah sakit.
- **Impact:** Application handler `LabOrderCreateExternalPatientHandler` secara otomatis menyerap data rujukan dari entitas `LabExternalModel` tanpa validasi foreign-key ke master internal `ORG-PPA`.
- **Architecture Impact:** Pemisahan kontrak validasi dokter internal (relasional ID `ORG-PPA`) versus dokter eksternal (*free-text snapshot* dari `LAB-EXTERNAL`).
- **Resolved By:** Analyst (Konsisten dengan Gap Discussion & Alignment)
- **Resolved Date:** 2026-10-15

---

## GAP-005: Ketiadaan Informasi Klinis & Urgensi Cito pada Tampilan Worklist
- **Status:** CLOSED
- **Decision:** Memperbarui record `LabOrderWorklistView` dengan menambahkan field: `IsCito`, `DokterPerujukName`, `LayananPerujukName`, dan `DeferredReason`. Menyesuaikan query SQL pada `LabOrderWorklistDal` serta menambahkan opsi filter prioritas Cito pada `LabOrderWorklistFilter`.
- **Rationale:** Memenuhi kriteria AC-04 dan AC-08 agar petugas laboratorium di worklist operasional langsung dapat mengidentifikasi sampel cito serta asal dokter dan layanan perujuk.
- **Impact:** Perubahan DTO worklist dan query SQL DAL.
- **Architecture Impact:** Perluasan kontrak antarmuka kueri operasional laboratorium.
- **Resolved By:** Analyst (Konsisten dengan Gap Discussion & Alignment)
- **Resolved Date:** 2026-10-15

---

## GAP-006: Inkonsistensi Alur Pembatalan Order Terhadap Tagihan Billing dan Status Spesimen
- **Status:** CLOSED
- **Decision:** 
  1. Pada `LabOrderModel.Cancel(...)`, perketat guard: pembatalan ditolak jika `LabOrderStatus >= LabOrderStatusEnum.Collected` (spesimen sudah diambil).
  2. Pada `LabOrderCancelHandler`, jika order memiliki `BillingTindakanId` (status `Charged`), handler memverifikasi integritas penagihan dengan Tata Rekening: jika billing belum `Final/Lunas`, eksekusi pembatalan charge billing; jika billing telah `Final/Lunas`, tolak pembatalan order laboratorium hingga tagihan diselesaikan di kasir/tata rekening.
- **Rationale:** Memenuhi Exception EX-03 dan menjaga konsistensi finansial antara domain laboratorium dan domain Tata Rekening.
- **Impact:** Penambahan guard logic pada domain model dan koordinasi pelepasan billing pada application handler.
- **Architecture Impact:** Penegasan alur kompensasi / pembatalan lintas domain `LAB` -> `TRK`.
- **Resolved By:** Analyst (Konsisten dengan OC-LAB-ORDER-LAB EX-03 & Constraint 7 #5)
- **Resolved Date:** 2026-10-15

---

## GAP-007: Ketiadaan Peringatan Konfirmasi Potensi Tes Duplikat dalam Kurun 24 Jam
- **Status:** CLOSED
- **Decision:** Menyediakan metode pemeriksaan pada query/DAL laboratorium untuk mendeteksi order tes laboratorium dengan kode tes identik pada pasien yang sama dalam kurun waktu 24 jam terakhir yang berstatus aktif. Handler mendukung flag `AllowDuplicateConfirmation = false` secara default; jika terdeteksi duplikasi dan flag false, kembalikan respon konfirmasi peringatan sesuai EX-05.
- **Rationale:** Mencegah terjadinya kesalahan pemesanan ganda yang merugikan pasien dan membebani kapasitas analitik laboratorium.
- **Impact:** Penambahan kueri deteksi duplikasi dan parameter konfirmasi opsional pada command intake.
- **Architecture Impact:** Mekanisme soft-warning / confirmation flow pada boundary API order.
- **Resolved By:** Analyst (Konsisten dengan OC-LAB-ORDER-LAB EX-05)
- **Resolved Date:** 2026-10-15

---

## OQ-001: Representasi Granularitas Prioritas (Rutin vs Cito)
- **Status:** CLOSED
- **Decision:** Urgensi ditetapkan pada tingkat Header Order dan diwariskan ke setiap baris Item Pemeriksaan, dengan aturan bahwa setiap baris juga dapat memiliki prioritas item individual. Jika minimal 1 baris item bertanda Cito, maka status Order Header secara otomatis bernilai Cito (`IsCito = true`).
- **Rationale:** Memberikan fleksibilitas klinis tingkat item sekaligus menjamin visibilitas level order pada antarmuka daftar kerja laboratorium.
- **Impact:** Sinkronisasi konsistensi antara header dan baris item pada agregat `LabOrderModel`.
- **Architecture Impact:** Hierarki status prioritas pada domain model.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-15

---

## OQ-002: Sinkronisasi Pembatalan Order yang Sudah Terlanjur Charged
- **Status:** CLOSED
- **Decision:** Pembatalan order yang berstatus `Charged` diizinkan hanya jika episode billing di Tata Rekening belum `Final` atau `Lunas`. Handler pembatalan akan memicu pelepasan beban biaya (`VoidTindakan`) pada Tata Rekening sebelum mengubah status order menjadi `Cancelled`.
- **Rationale:** Menjamin bahwa pembatalan di penunjang medis tidak meninggalkan piutang atau tagihan menggantung pada rekening kasir pasien.
- **Impact:** Integrasi kompensasi pembatalan dengan Tata Rekening.
- **Architecture Impact:** Panggilan integrasi balik dari LabContext ke ChargeContext/PaymentContext.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-15

---

## OQ-003: Karakter Peringatan Duplikasi 24 Jam
- **Status:** CLOSED
- **Decision:** Peringatan bersifat *soft-warning / overrideable*. Pada permintaan pembuatan order pertama, sistem mendeteksi duplikasi dan memberikan konfirmasi peringatan. Klinisi dapat menyetujui pemesanan ulang (misal untuk pemantauan gula darah serial atau enzim jantung serial) dengan mengirimkan parameter `AllowDuplicateConfirmation: true`.
- **Rationale:** Membedakan antara kesalahan tidak sengaja (*unintended duplicate*) dengan kebutuhan medis riil pemeriksaan serial.
- **Impact:** Mendukung kebutuhan klinis pemantauan intensif tanpa memblokir dokter.
- **Architecture Impact:** Desain protokol request-confirmation pada application layer.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-15

---

# 9. Architecture Applicability

Record the Architecture Applicability decision:

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Implementasi ini memerlukan perancangan arsitektur teknis formal karena:
1. Melakukan modifikasi skema data dan persistensi relasional pada tabel `BILRG_LabOrder` dan `BILRG_LabOrderItem` (penambahan field `DokterPerujukId`, `DokterPerujukName`, `LayananPerujukId`, `LayananPerujukName`, `ClinicalNote`, dan urgensi Cito).
2. Memperluas agregat domain `LabOrderModel` dan kontrak DTO input/output.
3. Melibatkan integrasi lintas domain (*cross-domain orchestration*) dengan `ADM-REG` (validasi keaktifan kunjungan), `ORG-PPA` (validasi dokter perujuk), `LAB-EXTERNAL` (konsumsi `RegExternalId`), dan `TRK-BILLING` (sinkronisasi pembatalan charge billing).
4. Menyesuaikan kueri performa daftar kerja (*worklist*) operasional laboratorium.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status

**READY-FOR-PLANNING**

*(Gerbang READY-FOR-PLANNING telah dievaluasi dan diberikan secara resmi oleh peran ica-architect pada 2026-10-15).*

## Notes

Seluruh kesenjangan (*gaps*), risiko teknis, dan pertanyaan terbuka telah berhasil diselesaikan dengan menyerap masukan Gap Discussion: penggunaan terminologi `Perujuk`, `LayananPerujuk`, dan `ClinicalNote` (penggabungan indikasi klinis dan catatan dokter). Artefak ini siap diserahkan kepada peran **ica-architect** untuk penyusunan dan pembaruan arsitektur teknis target ([`OC-LAB-ORDER-LAB-ARCHITECTURE.md`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ARCHITECTURE.md)) serta pemberian otorisasi gerbang `READY-FOR-PLANNING`.

---

# 11. References

Referenced artifacts:

- ISSUE: [OC-LAB-ORDER-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-ISSUE.md)
- FEATURE: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- OUTCOME: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- DOMAIN:
  - [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md)
  - [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md)
  - [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md)
  - [03-ADMISSION-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/03-ADMISSION-DOMAIN.md)
  - [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)
- RELATED FEASIBILITY:
  - [OC-LAB-REG-EXTERNAL-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-FEASIBILITY-ASSESSMENT.md)

Referenced codebase locations:

- Domain Model: [Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs)
- Item Model: [Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemModel.cs)
- Resolution Service: [Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs)
- Commands: [Bilreg.Application/LabContext/LabOrderFeature/UseCases](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases)
- SQL Tables: [Bilreg.SqlDb/LabContext/LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature)
- Tests: [Bilreg.Test/LabContext/LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature)
