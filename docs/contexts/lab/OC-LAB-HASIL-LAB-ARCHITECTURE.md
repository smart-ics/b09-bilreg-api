---
Title: Hasil Pemeriksaan Laboratorium Terverifikasi Architecture (Termasuk Bridging LIS via Oware)
Code: OC-LAB-HASIL-LAB
Artifact: ARCHITECTURE
Version: 1.1
LastUpdated: 2026-11-05
---

# 1. Overview

## Realized Feature

Arsitektur ini merealisasikan spesifikasi fitur **[OC-LAB-HASIL-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEATURE.md)** yang diturunkan dari outcome kanonikal **[OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md)** dan menindaklanjuti intake **[OC-LAB-HASIL-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ISSUE.md)** (`ISSUE-LAB-HASIL-LAB-001`).

Arsitektur ini menetapkan realisasi teknis inti (*core backend & integration bridge*) yang terkoordinasi antara:
1. **Core Billing/Registration Backend (`b09-bilreg-api`)**:
   - Peningkatan agregat `LabResultDocumentModel` dengan dukungan catatan ekspertise patolog (`ExpertiseNote`), catatan peringatan nilai kritis sederhana (`CriticalNote`), penguncian komponen mandatori (*mandatory guard*), dan penyesuaian siklus hidup dokumen (`Draft`, `Recorded`, `Verified`, `Released`, `Amended`).
   - Penyediaan endpoint REST API realtime untuk ingesti hasil analitik dari LIS pihak ketiga (`POST /api/LabContext/LabResultFeature/ingest-lis`) dengan pencocokan deterministik 1:1 ke perancah order (*order scaffold*), penandaan `ResultSource = ExternalLis`, dan adopsi otomatis status validasi beserta nama validator asal LIS.
   - Penegakan otorisasi verifikasi medis hasil entri lokal berbasis klaim JWT/Role (`doktersppk` / `lab.result.verify`).
   - Sinkronisasi status rilis dengan Tata Rekening (`TRK-BILLING` gate) dan pemutakhiran render PDF hasil resmi.
2. **Integration Middleware (`a046_oware_ordermiddlewareapi`)**:
   - Resolusi pemetaan tes di sisi Oware menggunakan tabel `OWARE_MapPemeriksaan` (+ `ConsId` untuk multi-vendor LIS) sebelum data di-dispatch ke Bilreg.
   - Penyediaan service client HTTP outbound `BilregLabResultClient` untuk meneruskan data hasil analitik yang telah dinormalisasi dari LIS pihak ketiga langsung ke Bilreg API secara realtime dengan mekanisme *fallback retry*.

*(Catatan: Implementasi Frontend Web `c012_myhospital_web` dan layer BFF ditangguhkan ke fase berikutnya sejalan dengan proses perancangan desain UI/UX dan BFF).*

## Referenced Artifacts

- Originating ISSUE: [OC-LAB-HASIL-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ISSUE.md)
- Domain Context: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md), [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)
- Canonical Outcome: [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md)
- Feature Specification: [OC-LAB-HASIL-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEATURE.md)
- Analysis Input: [OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)

---

# 2. Architectural Basis

## Business Context

Pencatatan dan verifikasi hasil laboratorium adalah fakta bisnis persisten penunjang medik yang esensial. Hasil analitis harus mencerminkan data objektif yang sahih dari spesimen pasien yang telah diambil (`SampleCollection`), dievaluasi terhadap nilai rujukan berbasis usia/gender, diidentifikasi nilai kritisnya demi keselamatan pasien, serta disahkan oleh Dokter Spesialis Patologi Klinik. Keabsahan klinis (akses dokter internal) dipisahkan dari otorisasi penyerahan dokumen ke pasien yang tunduk pada aturan penyelesaian tagihan di Tata Rekening (`TRK-BILLING`).

## Analysis Input

Arsitektur ini merealisasikan secara langsung seluruh keputusan penutupan celah (*Gap Closure Decisions*) yang telah disetujui bersama Product Owner pada [OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md):

- **GAP-001 (Otoritas Verifikasi Sp.PK)**:
  - Entri Manual: Verifikasi medis dibatasi berbasis klaim JWT/Role (`doktersppk` atau permission `lab.result.verify`).
  - Bridging LIS/Oware: Status hasil dari LIS diakui telah tervalidasi di LIS asal; nama dokter/petugas validator LIS diadopsi langsung sebagai verifikator dokumen di Bilreg.
- **GAP-002 (Guard Komponen Mandatori)**: Model domain memvalidasi kelengkapan seluruh komponen `IsMandatory == true` sebelum verifikasi manual diizinkan (`EX-02`).
- **GAP-003 & OQ-001 (Bridging LIS via Oware)**: Oware memanggil REST API endpoint internal Bilreg `POST /api/LabContext/LabResultFeature/ingest-lis` secara realtime, memetakan komponen secara otomatis ke perancah order, serta mendukung hasil bertahap (*partial results*).
- **GAP-004 (Catatan / Ekspertise Patolog)**: Penambahan atribut `ExpertiseNote` pada agregat, skema SQL `BILRG_LabResultDocument`, DTO, cetakan PDF, dan UI.
- **GAP-005 & OQ-002 (Penyederhanaan Penanganan Nilai Kritis Informatif)**: Peringatan visual mencolok (*soft-warning*) untuk flag `Critical`, dengan penyimpanan catatan nilai kritis sederhana (`CriticalNote VARCHAR(1000)`) langsung pada dokumen hasil tanpa formulir telepon terpisah dan tanpa memblokir alur penyerahan hasil.
- **GAP-006 (Harmonisasi Status Siklus Hidup)**: Sinkronisasi status `LabResultStatusEnum` (`Draft`, `Recorded`, `Verified`, `Released`, `Amended`) dengan mutasi status pada `LabOrderModel`.
- **GAP-007 & OQ-003 (Meja Kerja Terpadu Dimodernisasi)**: Menjaga meja kerja dalam tab terpadu `Tindakan Lab` (`LabWorkflowView.vue`) dengan sub-antrean dinamis dan panel adaptif, menimpa (*overwrite*) komponen lama agar lebih modern dan responsif.

```text
[07-LABORATORY-DOMAIN] + [OC-LAB-HASIL-LAB-FEATURE]
                         +
    [OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT]
                         ↓
        [OC-LAB-HASIL-LAB-ARCHITECTURE]
```

---

# 3. Scope

## Included

1. **Domain Layer (`b09-bilreg-api`)**:
   - Pembaruan enumerasi `LabResultStatusEnum`: `Draft = 1`, `Recorded = 2`, `Verified = 3`, `Released = 4`, `Amended = 5`.
   - Peningkatan `LabResultDocumentModel`:
     - Kolom `ExpertiseNote` (string max 2000).
     - Kolom `CriticalNote` (string max 1000).
     - Pembaruan metode `Verify(string verifiedUserId, DateTime verifiedDate, string expertiseNote)` dengan guard komponen mandatori.
     - Penambahan metode `MarkReleased(string userId, DateTime releasedAt)` dan pembaruan `SetCriticalNote(string note)`.
     - Penambahan factory method `CreateFromLisIngestion(...)` yang langsung menetapkan status `Verified` dengan validator LIS.
2. **Application Layer (`b09-bilreg-api`)**:
   - Use Case baru `LabResultIngestFromLisCmd`:
     - Handler melakukan resolusi order, pencocokan komponen uji ke perancah, kalkulasi auto-flagging, penyimpanan hasil dengan sumber `ExternalLis`, dan penetapan validator LIS.
   - Use Case `LabResultVerifyCmd`:
     - Memvalidasi klaim role `doktersppk` atau permission `lab.result.verify` dari konteks user yang login.
     - Menerima parameter `ExpertiseNote` dan menyimpannya ke dokumen.
   - Penyelarasan `LabOrderReleaseCmd`:
     - Memperbarui status dokumen hasil menjadi `Released` saat validasi billing clearance `TRK-BILLING` sukses (*CLEAR*).
3. **API & Infrastructure Layer (`b09-bilreg-api`)**:
   - Controller `LabResultController`:
     - Endpoint `POST api/LabContext/LabResultFeature/ingest-lis` (dengan otentikasi internal/service key).
     - Pembaruan endpoint `POST api/LabContext/LabResultFeature/verify` (menerima `expertiseNote`).
   - Renderer PDF `LabResultPdfRenderer`:
     - Menampilkan catatan ekspertise patolog, tanda tangan digital/identitas Sp.PK atau validator LIS, dan catatan nilai kritis (`CriticalNote`).
   - Migrasi SQL `BILRG_LabResultDocument_M12_Alter.sql`:
     - Menambahkan kolom `ExpertiseNote` dan `CriticalNote`.
4. **Middleware Oware (`a046_oware_ordermiddlewareapi`)**:
   - DDL Migrasi skema `OWARE_MapPemeriksaan` dengan penambahan kolom `ConsId VARCHAR(20)` dan indeks pencarian.
   - Pembaruan DTO & DAL `MapPemeriksaanModel` dan `IMapPemeriksaanDal` untuk *reverse-lookup* `(ConsId, LabTestCode) -> PemeriksaanId`.
   - Service client HTTP `IBilregLabResultClient` & implementasi `BilregLabResultClient`.
   - Modifikasi `SaveResultLabCommandHandler`: pemetaan hasil LIS ke `PemeriksaanId` HIS, pencatatan unmapped item ke `OWARE_ResultLog`, dan pengiriman payload realtime ke Bilreg API `POST /api/LabContext/LabResultFeature/ingest-lis`.

## Excluded & Deferred Scope

1. **Frontend Web & BFF Layer (`c012_myhospital_web`)**:
   - Tampilan visual meja kerja `LabWorkflowView.vue`, komponen form input hasil tabular, modal verifikasi Sp.PK, dan adaptasi BFF ditangguhkan (*deferred*) ke fase perancangan UI/UX & BFF khusus berikutnya.
2. **Hardware Driver Instrumen Analitik**:
   - Komunikasi tingkat rendah (RS-232, ASTM, format HL7 mentah) merupakan ranah analyzer / driver LIS pihak ketiga.
3. **Rekam Medis Klinis Akhir & Billing Kasir**:
   - Penegakan diagnosis DPJP di rekam medis (ranah EMR) dan transaksi pembayaran loket kasir (ranah `TRK-KASIR`).

---

# 4. Technical Decisions

## TD-01: Dual-Path Medical Verification Strategy

- **Manual Entry Path**: Ketika hasil diinput langsung oleh analis di HIS, aksi verifikasi medis wajib melewati pengecekan otorisasi pada Controller/Handler: pengguna harus memiliki Role `doktersppk` atau permission `lab.result.verify`. Jika verifikator bukan Sp.PK, sistem menolak dengan HTTP 403 / Domain Exception (`EX-01`).
- **Bridging LIS Path**: Ketika hasil dikirim oleh LIS pihak ketiga via Oware, hasil tersebut dianggap telah melewati kendali mutu dan verifikasi di sistem LIS asal. Endpoint ingesti mencatat status dokumen langsung sebagai `Verified` (atau siap rilis) dengan menyimpan nama validator yang dikirim oleh LIS pada field `VerifiedUserId` / `ValidatorName` tanpa mewajibkan verifikasi ulang oleh dokter lokal, kecuali jika dilakukan amandemen di kemudian hari.

## TD-02: Realtime Ingestion REST API Contract & Oware Mapping Strategy

- Pemetaan kode tes dilakukan secara terpusat di sisi Oware menggunakan tabel `OWARE_MapPemeriksaan` yang diperkaya kolom `ConsId` (Vendor LIS):
  - Oware mengeksekusi *reverse-lookup* berdasarkan `(ConsId, LabTestCode)` untuk mendapatkan `PemeriksaanId` (kode komponen resmi HIS).
  - Item hasil LIS yang kodenya belum terdaftar di `OWARE_MapPemeriksaan` dilewati (*skipped*) dan dicatat pada `OWARE_ResultLog` untuk ditinjau oleh administrator laboratorium.
  - Oware memanggil REST API internal Bilreg: `POST /api/LabContext/LabResultFeature/ingest-lis`.
- Request Payload memuat:
  - `OrderId` / `OrderNo` (kunci pencocokan order laboratorium).
  - `LisRefNo` / `BarcodeNo` (nomor transaksi LIS pihak ketiga).
  - `ValidatorName` (identitas dokter/petugas validator LIS).
  - `ValidatedAt` (stempel waktu verifikasi di LIS).
  - `CriticalNote` (catatan nilai kritis jika dilaporkan).
  - `Components`: koleksi baris hasil `(ComponentCode, Value, Unit, ReferenceRangeText, Flag)` di mana `ComponentCode` sudah merupakan `PemeriksaanId` HIS yang valid.
- Jika Bilreg API sementara tidak dapat dihubungi (network timeout / service restart), Oware mencatat status antrean `Failed` dan mengeksekusi mekanisme *retry* otomatis pada siklus berikutnya.

## TD-03: Deterministic Scaffold Direct Matching & Flag Adoption

- Bilreg menerima `ComponentCode` dari Oware yang sudah terpetakan ke `PemeriksaanId` HIS.
- Bilreg langsung mencocokkan `ComponentCode` secara deterministik 1:1 terhadap `LabOrderItemComponent.ComponentCode` pada perancah order (*order scaffold*), tanpa perlu pemetaan alias vendor atau fuzzy-matching nama.
- Penentuan Flag Analitik: Bilreg mengadopsi flag analitik dari LIS asal (`Normal`, `Low`, `High`, `Critical`). Jika LIS tidak menyertakan flag atau bernilai kosong, Bilreg mengevaluasi otomatis menggunakan domain service `LabResultFlagging`.
- Sistem mendukung hasil parsial: komponen yang berhasil dicocokkan langsung terisi nilainya, komponen yang belum dikirim tetap bernilai kosong/pending.

## TD-04: Non-Blocking Critical Panic Value Policy (Soft-Warning) & Simplified HIS Context

- Auto-flagging menghitung status `Critical` menggunakan aturan rentang nilai kritis pada master test atau nilai ambang batas analitis.
- Flag `Critical` memicu penandaan visual merah mencolok pada UI dan lembar cetak hasil PDF (*soft-warning*).
- Sesuai konteks HIS (bukan instrumen LIS), informasi nilai kritis disederhanakan menjadi properti teks `CriticalNote VARCHAR(1000)` pada agregat `LabResultDocumentModel`. Petugas laboratorium atau dokter Sp.PK dapat mencatat catatan penanganan nilai kritis langsung saat penginputan hasil atau saat verifikasi tanpa use case terpisah dan tanpa memblokir alur pelayanan pasien.

## TD-05: Unified Modernized Laboratory Workspace (`LabWorkflowView.vue`)

- Seluruh siklus hidup pengujian laboratorium dikelola dalam satu meja kerja terpadu (`LabWorkflowView.vue`) pada tab `Tindakan Lab` guna memberikan efisiensi alur kerja maksimal tanpa friksi (*zero context-switching*) dan visibilitas 360 derajat status pasien.
- Menggunakan sub-antrean dinamis pada panel daftar kerja:
  1. `Antrean Order` (Order Masuk & Triage Cito)
  2. `Pengambilan Sampel` (Flebotomi & Panduan Tabung Vacutainer)
  3. `Input Hasil` (Order status `Collected` yang menunggu pencatatan nilai analitik)
  4. `Verifikasi Medis` (Khusus Dokter Sp.PK: telaah analitik, auto-flagging, indikator nilai kritis, dan catatan ekspertise)
  5. `Rilis & Billing` (Pemeriksaan kelayakan billing clearance `TRK-BILLING` dan penyerahan/cetak hasil resmi)
  6. `Integrasi LIS (Oware)` (Monitoring status bridging realtime hasil dari LIS pihak ketiga)
- Panel detail beradaptasi otomatis sesuai konteks antrean aktif tanpa reload halaman.
- Komponen lama yang kaku ditimpa (*clean overwrite*) dengan tabel input hasil responsif, real-time auto-flagging, modal ekspertise patolog, dan pratinjau dokumen PDF resmi.

---

# 5. Component Responsibilities

| Component | Repository / Layer | Responsibility |
|---|---|---|
| `LabResultDocumentModel` | `b09-bilreg-api` (Domain) | Entitas agregat inti dokumen hasil laboratorium: mengelola lifecycle status, mutasi nilai analitik, aturan immutability, amandemen *full snapshot*, guard mandatori, catatan ekspertise (`ExpertiseNote`), dan catatan nilai kritis (`CriticalNote`). |
| `LabResultFlagging` | `b09-bilreg-api` (Domain) | Domain service murni penghitung evaluasi rentang rujukan (Normal, Low, High, Critical) berbasis numerik dan teks rujukan. |
| `LabResultIngestFromLisCmd` | `b09-bilreg-api` (Application) | Use case penerima dan pemeta hasil analitik LIS pihak ketiga dari Oware ke perancah order HIS. |
| `LabResultVerifyCmd` | `b09-bilreg-api` (Application) | Use case verifikasi medis hasil entri lokal dengan validasi kredensial Sp.PK dan kelengkapan mandatori. |
| `LabResultAmendCmd` | `b09-bilreg-api` (Application) | Use case amandemen hasil terverifikasi: memensiunkan versi lama dan membentuk versi baru berstatus `Recorded`. |
| `LabOrderReleaseCmd` | `b09-bilreg-api` (Application) | Use case rilis hasil penyerahan ke pasien dengan evaluasi gerbang billing `TRK-BILLING` dan sinkronisasi status dokumen. |
| `LabResultPdfRenderer` | `b09-bilreg-api` (Infrastructure) | Penghasil dokumen PDF resmi hasil laboratorium yang mencantumkan ekspertise, tanda tangan verifikasi, catatan nilai kritis, dan nomor versi aktif. |
| `LabResultController` | `b09-bilreg-api` (API) | Endpoint REST API untuk entri, ingesti LIS, verifikasi, amandemen, dan pengunduhan PDF hasil lab. |
| `BilregLabResultClient` | `a046_oware_ordermiddlewareapi` (Infrastructure) | HTTP Client outbound yang memanggil endpoint ingesti Bilreg API secara realtime setelah data LIS diterima. |
| `SaveResultLabCommandHandler` | `a046_oware_ordermiddlewareapi` (Application) | Handler penerima hasil LIS: memproses payload JSON vendor, menyimpan ke tabel Oware, dan memicu dispatch ke Bilreg. |
| `LabWorkflowView.vue` | `c012_myhospital_web` (Views) | Tampilan utama meja kerja terpadu laboratorium yang mencakup sub-antrean order, sampling, entri hasil analitik, verifikasi Sp.PK, rilis billing, dan LIS bridging. |
| `LabResultService.ts` | `c012_myhospital_web` (Queries) | TanStack Query & Mutation wrapper untuk seluruh operasi backend Hasil Lab di Bilreg API. |

---

# 6. Integration Design

| Source | Target | Protocol / Contract | Purpose |
|---|---|---|---|
| Mesin Analyzer / LIS Vendor | `a046_oware_ordermiddlewareapi` | HTTP REST / JSON Payload | Pengiriman hasil pengujian analitik spesimen dari laboratorium pihak ketiga. |
| `a046_oware_ordermiddlewareapi` | `b09-bilreg-api` | HTTP POST `/api/LabContext/LabResultFeature/ingest-lis` | Bridging realtime hasil analitik LIS ke dokumen perancah order HIS. |
| `c012_myhospital_web` (Hasil Lab) | `b09-bilreg-api` | HTTP REST API (JWT Bearer Auth) | Entri nilai analitik, approval verifikasi Sp.PK, input ekspertise dan catatan nilai kritis, serta unduh PDF. |
| `b09-bilreg-api` (`LabResultFeature`) | `b09-bilreg-api` (`LabOrderFeature`) | Internal Aggregate Coordination | Penyelarasan status siklus hidup antara `LabOrderModel` dan `LabResultDocumentModel`. |
| `b09-bilreg-api` (`LabResultFeature`) | `b09-bilreg-api` (`TRK-BILLING`) | `ILabBillingIntegration.ValidateReleaseEligibility` | Pengecekan kelayakan penyelesaian kewajiban finansial sebelum penyerahan hasil resmi ke pasien. |
| `b09-bilreg-api` (`LabResultFeature`) | `EMR` / Rawat Inap / IGD | Read Query via EMR Service | Akses langsung dokumen hasil terverifikasi untuk pertimbangan medis DPJP tanpa memandang status billing. |

---

# 7. Data Ownership

| Data Aggregate / Entity | Owner (Bounded Context) | Storage Table |
|---|---|---|
| Order Pemeriksaan Lab (`LabOrderModel`) | `LabContext` (`b09-bilreg-api`) | `BILRG_LabOrder`, `BILRG_LabOrderItem`, `BILRG_LabOrderItemComponent` |
| Dokumen Hasil Lab (`LabResultDocumentModel`) | `LabContext` (`b09-bilreg-api`) | `BILRG_LabResultDocument` |
| Rincian Nilai Komponen Hasil (`LabResultItemModel`) | `LabContext` (`b09-bilreg-api`) | `BILRG_LabResultItem` |
| Log & Antrean Masuk LIS Vendor | `Oware` (`a046_oware_ordermiddlewareapi`) | `OWARE_Result`, `OWARE_ResultLabPemeriksaan`, `OWARE_ResultLog` |
| Tagihan & Status Kelayakan Kasir | `TataRekeningContext` (`b09-bilreg-api`) | `BILRG_TrsBilling`, `ta_tata_rekening` |

---

# 8. Database Design

## Modified Tables

### Table: `BILRG_LabResultDocument` (`b09-bilreg-api`)

Penambahan kolom untuk mendukung catatan ekspertise patolog dan catatan peringatan nilai kritis sederhana:

```sql
-- DDL Script: BILRG_LabResultDocument_M12_Alter.sql
ALTER TABLE BILRG_LabResultDocument
ADD 
    ExpertiseNote VARCHAR(2000) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_ExpertiseNote DEFAULT '',
    CriticalNote VARCHAR(1000) NOT NULL CONSTRAINT DF_BILRG_LabResultDocument_CriticalNote DEFAULT '';
GO
```

### Table: `OWARE_MapPemeriksaan` (`a046_oware_ordermiddlewareapi`)

Penambahan kolom `ConsId` untuk mendukung resolusi mapping multi-vendor LIS:

```sql
-- DDL Script: OWARE_MapPemeriksaan_Alter_ConsId.sql
ALTER TABLE OWARE_MapPemeriksaan
ADD 
    ConsId VARCHAR(20) NOT NULL CONSTRAINT DF_OWARE_MapPemeriksaan_ConsId DEFAULT('');
GO
CREATE NONCLUSTERED INDEX IX_OWARE_MapPemeriksaan_ConsId_LabTestCode
ON OWARE_MapPemeriksaan (ConsId, LabTestCode)
INCLUDE (PemeriksaanId);
GO
```

## Relationships

- Relasi `BILRG_LabResultDocument` 1-ke-N terhadap `BILRG_LabResultItem` berdasarkan `ResultDocumentId` (tetap dipertahankan, bersifat *immutable per version*).
- Relasi `BILRG_LabResultDocument` ke `BILRG_LabOrder` berdasarkan `OrderId`.
- Relasi amandemen: `BILRG_LabResultDocument.PreviousVersionId` menunjuk ke `ResultDocumentId` dari versi sebelumnya (`Self-referencing audit trace`).

## Migration Considerations

- Seluruh penambahan kolom pada tabel `BILRG_LabResultDocument` menggunakan default constraint (`DEFAULT ''`, `DEFAULT 0`, `DEFAULT '3000-01-01'`), sehingga bersifat *non-breaking* dan kompatibel 100% terhadap data historis tanpa memerlukan downtime.

---

# 9. Cross-Cutting Concerns

1. **Security & Authorization**:
   - Endpoint publik internal `ingest-lis` dilindungi oleh API Key / Service Token internal antara Oware dan Bilreg.
   - Endpoint verifikasi lokal `verify` memeriksa klaim role pengguna (`User.IsInRole("doktersppk")` atau policy `RequireSpPkRole`).
2. **Audit Trail & Immutability**:
   - Seluruh mutasi rekaman hasil dicatat melalui `AuditTrailType` (`Created`, `Modified`, `Voided`).
   - Dokumen berstatus `Verified` terkunci dari segala bentuk modifikasi langsung; perubahan hanya diizinkan via `LabResultAmendCmd` yang membentuk ID dokumen baru.
3. **Observability & Error Handling**:
   - Panggilan bridging dari Oware ke Bilreg dilengkapi pencatatan structured logging (`ResultId`, `OrderId`, `Duration`, `StatusCode`). Jika terjadi kegagalan HTTP, status transaksi di Oware ditandai `Failed` untuk memungkinkan aksi *retry* manual atau otomatis.
4. **Performance & Optimistic Rendering**:
   - Kueri daftar kerja meja hasil membatasi rentang tanggal default (hari ini) dan dioptimalkan melalui indeks `OrderId` dan `ResultStatus`.

---

# 10. Implementation Constraints

1. **Explicit SQL Persistence (Dapper)**: Seluruh interaksi basis data pada `b09-bilreg-api` wajib menggunakan SQL eksplisit via Dapper / ADO.NET (dilarang menggunakan Entity Framework / ORM berat).
2. **Tactical DDD & Solution Neutrality**: Logika bisnis invarian (guard mandatori, validasi status, amandemen snapshot) wajib berada di dalam domain aggregate `LabResultDocumentModel`, bukan tersebar di controller atau frontend.
3. **Frontend Reactivity & Auto-Imports**: Komponen frontend di `c012_myhospital_web` wajib mematuhi panduan Vue 3 Composition API (`<script setup lang="ts">`), TanStack Query untuk state server, dan memanfaatkan sistem auto-import komponen UI.
4. **Targeted Verification Protocol**: Setiap perubahan pada modul backend dan frontend wajib diverifikasi secara bertahap melalui pengujian unit terarah (`dotnet test --filter ...` dan `pnpm test <spec-path>`), tanpa menjalankan monster test suite penuh selama proses implementasi.

---

# 11. Acceptance Conditions

Implementasi dinyatakan lengkap secara arsitektural apabila:

1. **AC-COND-01**: Endpoint `POST /api/LabContext/LabResultFeature/ingest-lis` berhasil menerima payload hasil dari Oware, memetakan ke perancah order, menandai `ResultSource = ExternalLis`, dan menetapkan validator LIS secara otomatis.
2. **AC-COND-02**: Endpoint `POST /api/LabContext/LabResultFeature/verify` menolak eksekusi verifikasi manual jika akun pengguna bukan Dokter Sp.PK, atau jika terdapat komponen mandatori yang masih kosong (`EX-01`, `EX-02`).
3. **AC-COND-03**: Kolom `ExpertiseNote` berhasil disimpan persisten, dapat dimutakhirkan saat verifikasi, dan tampil pada pratinjau dokumen PDF resmi.
4. **AC-COND-04**: Parameter uji dengan flag `Critical` memicu penanda visual merah pada UI dan lembar cetak PDF (*soft-warning*), serta memungkinkan pencatatan `CriticalNote` tanpa memblokir alur kerja pelayanan pasien.
5. **AC-COND-05**: Status rilis dokumen hasil dan order tersinkronisasi secara akurat dengan evaluasi kelayakan tagihan `TRK-BILLING`.
6. **AC-COND-06**: Reverse-lookup di Oware via `OWARE_MapPemeriksaan` (+ `ConsId`) berhasil memetakan `LabTestCode` LIS ke `PemeriksaanId` HIS, mencatat log unmapped items, dan men-dispatch payload analitik ke Bilreg API secara realtime.
7. **AC-COND-07**: Seluruh pengujian unit terarah pada `Bilreg.Test` dan `Oware.Test` lulus 100%.
