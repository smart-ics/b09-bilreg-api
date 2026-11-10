---
Title: Hasil Pemeriksaan Laboratorium Terverifikasi Architecture (Termasuk Konsol Web Workbench & Bridging LIS Oware)
Code: OC-LAB-HASIL-LAB
Artifact: ARCHITECTURE
Version: 1.2
LastUpdated: 2026-11-10
---

# 1. Overview

## Realized Feature

Arsitektur ini merealisasikan spesifikasi fitur **[OC-LAB-HASIL-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEATURE.md)** yang diturunkan dari outcome kanonikal **[OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md)**, menindaklanjuti intake **[OC-LAB-HASIL-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ISSUE.md)** (`ISSUE-LAB-HASIL-LAB-001`), serta merealisasikan cetak biru UI pada **[OC-LAB-HASIL-LAB-SCREEN-INVENTORY.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB-SCREEN-INVENTORY.md)** dan prototipe operasional interaktif **[mockup-lab-hasil.html](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/docs/modules/laboratory/mockup-lab-hasil.html)**.

Versi 1.2 ini merupakan **arsitektur teknis definitif menyeluruh (end-to-end)** yang memperluas cakupan backend v1.1 dengan mengintegrasikan realisasi penuh lapisan konsol web frontend client (*Laboratory Results & Expertise Management Workspace*) pada `c012_myhospital_web`, menghubungkan middleware bridging LIS pada `a046_oware_ordermiddlewareapi`, dan menyelaraskan seluruh kontrak data Backend-For-Frontend (BFF) pada `b09-bilreg-api`.

Arsitektur ini mengorkestrasi 3 repositori utama:
1. **Core Billing/Registration Backend (`b09-bilreg-api`)**:
   - Pemodelan domain `LabResultDocumentModel` dengan proteksi immutability, amandemen snapshot versi baru N+1 dengan validasi alasan minimal 10 karakter (**EX-03**), guard komponen mandatori (**EX-02**), catatan ekspertise patolog (`ExpertiseNote VARCHAR(2000)`), dan catatan nilai kritis (`CriticalNote VARCHAR(1000)`).
   - Penyediaan endpoint REST API realtime ingesti hasil LIS pihak ketiga (`POST /api/LabContext/LabResultFeature/ingest-lis`) dengan pencocokan deterministik 1:1 ke perancah order (*order scaffold*), penandaan `ResultSource = ExternalLis`, dan adopsi otomatis status `Verified` beserta identitas validator asal LIS.
   - Penegakan otorisasi verifikasi medis manual berbasis klaim JWT/Role (`doktersppk` / `lab.result.verify`) (**EX-01**).
   - Perluasan kontrak DTO baca `LabResultView` pada `LabResultGetQuery` untuk menyajikan seluruh metadata audit dan klinis (`ExpertiseNote`, `CriticalNote`, `AmendmentReason`, `PreviousVersionId`, `AmendedDate`, `AmendedUserId`).
   - Perluasan DTO dan kueri DAL `LabResultVerificationWorklistDal` untuk menyajikan metadata lengkap antrean kerja Sp.PK (prioritas CITO di baris teratas, indikator nilai kritis, ringkasan nama tes, identitas pasien, dan badge sumber LIS).
   - Penyediaan generator nomor register fisik Patologi Anatomi adaptif menggunakan `ISequencer` (`H.{YY}.{NoUrut:D4}` untuk Histopatologi vs `S.{YY}.{NoUrut:D4}` untuk Sitopatologi).
   - Sinkronisasi status rilis dengan Tata Rekening (`TRK-BILLING` gate) yang mengembalikan kontrak objek terstruktur `LabOrderReleaseResponse(bool Released, string BillingStatus, string Message)`.
   - Layanan penyusunan dan pengunduhan lembar hasil resmi berformat PDF (`LabResultPdfRenderer`).
2. **Frontend Web Client (`c012_myhospital_web`)**:
   - Navigasi persona terfokus: re-aktivasi rute tab `hasil_lab` pada `configs/tabs.ts` dan refaktor `views/HasilLab.vue` menjadi *route wrapper* terpadu yang memanggil `LabWorkflowView.vue` dengan initial query `activeQueue = 'verification'`.
   - Grid entri hasil Patologi Klinik berkecepatan tinggi (*Spreadsheet-like Tabular Grid*) pada `LabResultEntryPkTabular.vue` dengan navigasi tombol keyboard fluid (`Enter`/`Tab`), kalkulasi instan status flag di sisi client (*Normal/Low/High/Critical*), dan shortcut global `Ctrl+S`.
   - Penegakan *Inline Mandatory Panic Value Guard* (SCR-LAB-RES-08): soft-warning visual merah berkedip pada analit kritis yang mewajibkan pengisian catatan readback telepon (`CriticalNote`) secara inline sebelum simpan diizinkan, tanpa modal popup pemblokir layar.
   - Stasiun Telaah & Verifikasi Medis Sp.PK (`LabVerificationPanel.vue`): ringkasan temuan kritis di baris atas, chip template ekspertise klinis cepat, dan tombol verifikasi 1-klik langsung (*Direct 1-Click Verification*) tanpa modal konfirmasi berulang.
   - Formulir Patologi Anatomi adaptif multidisiplin (`LabResultEntryPaForm.vue`): adaptif membedakan Histopatologi (`DISC-PAH` dengan Makroskopis/Gross) vs Sitopatologi (`DISC-PAS` dengan Adekuasi Sediaan + Rekomendasi Klinis), menampilkan nomor register fisik PA, dan chip template diagnosis.
   - Formulir Mikrobiologi (`LabResultEntryMicroForm.vue`): pewarnaan gram, isolat kultur, dan tabel dinamis antibiogram (AST).
   - Alur rilis instan (*Single-Click Release*): penghapusan alur approval finansial kasir usang (`LabFinancialClearancePanel.vue`), evaluasi kontrak terstruktur `LabOrderReleaseResponse`; jika `CLEAR` langsung buka dialog pratinjau cetak PDF resmi (`LabResultPdfPreviewDialog.vue`), jika `BLOCKED` sajikan notifikasi informatif ramah penundaan fisik tanpa melempar runtime exception crash (akses klinis EMR internal tetap aktif bagi dokter DPJP).
   - Dialog amandemen terproteksi (`LabAmendResultDialog.vue`): penegakan batas minimal 10 karakter alasan dan pembatasan peran eksklusif untuk Dokter Sp.PK dan Supervisor Lab.
   - Layanan TanStack Query & skema validasi Zod typesafe (`LabResultService.ts`, `LabOrderService.ts`, `lab-result.schema.ts`, `lab-order.schema.ts`).
3. **Integration Middleware (`a046_oware_ordermiddlewareapi`)**:
   - Resolusi pemetaan tes di sisi Oware menggunakan tabel `OWARE_MapPemeriksaan` (+ `ConsId` untuk multi-vendor LIS) sebelum data di-dispatch ke Bilreg.
   - HTTP Client outbound `BilregLabResultClient` untuk meneruskan data hasil analitik yang telah dinormalisasi dari LIS pihak ketiga langsung ke Bilreg API secara realtime dengan mekanisme *fallback retry*.

## Referenced Artifacts

- Originating ISSUE: [OC-LAB-HASIL-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ISSUE.md)
- Domain Contexts: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md), [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)
- Workflow Architecture: [lab-workflow.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-workflow.md)
- Canonical Business Outcome: [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md)
- Feature Specification: [OC-LAB-HASIL-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEATURE.md)
- UI Screen Inventory: [OC-LAB-HASIL-LAB-SCREEN-INVENTORY.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB-SCREEN-INVENTORY.md)
- Interactive UI Mockup: [mockup-lab-hasil.html](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/docs/modules/laboratory/mockup-lab-hasil.html)
- Analysis Input: [OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md) (Backend Core) & [OC-LAB-HASIL-LAB-BFF-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB-BFF-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)

---

# 2. Architectural Basis

## Business Context

Pencatatan, evaluasi klinis, verifikasi medis, dan rilis hasil pemeriksaan laboratorium adalah fakta bisnis persisten penunjang medik yang paling kritis dalam menentukan tindakan diagnostik dan terapeutik pasien. Dokumen hasil pengujian analitis harus mencerminkan data objektif yang sahih dari spesimen pasien yang telah diambil (`SampleCollection`), dievaluasi terhadap nilai rujukan berbasis demografi usia dan gender, diidentifikasi nilai kritisnya demi keselamatan jiwa pasien, serta disahkan oleh Dokter Spesialis Patologi Klinik (`ORG-PPA`).

Keabsahan klinis (akses dokter internal melalui EMR) dipisahkan secara tegas dari otorisasi penyerahan dokumen fisik ke pasien/keluarga yang tunduk pada aturan penyelesaian administrasi tagihan di Tata Rekening (`TRK-BILLING`).

## Analysis Input

Arsitektur ini merealisasikan secara langsung seluruh keputusan penutupan celah (*Gap Closure Decisions*) yang telah disetujui bersama pengguna melalui sesi *Grill-Me Alignment Session* (2026-11-10) pada [OC-LAB-HASIL-LAB-BFF-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB-BFF-FEASIBILITY-ASSESSMENT.md) dan [OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT.md):

1. **GAP-BFF-01 (Penyelarasan Read DTO `LabResultView`)**: Memperluas DTO baca pada `LabResultGetQuery.cs` dengan menambahkan properti `ExpertiseNote`, `CriticalNote`, `AmendmentReason`, `PreviousVersionId`, `AmendedDate`, `AmendedUserId`, memetakan nilai dari model domain di `LabResultGetHandler.cs`, serta menyinkronkan skema Zod `labResultDetailSchema` di frontend.
2. **GAP-BFF-02 & OQ-BFF-02 (Pengayaan Antrean Verifikasi Sp.PK & Triase CITO)**: Memperluas DTO `LabResultVerificationWorklistView` dan kueri SQL DAL `LabResultVerificationWorklistDal.cs` dengan `IsCito`, `Gender`, `AgeAtOrder`, `LayananPerujukName`, `DokterPerujukName`, `TestNames`, `HasCriticalValue`, `VersionNo`, `ResultSource`, dengan pengurutan prioritas `ORDER BY o.Urgency DESC, d.RecordedDate DESC`. Hasil LIS diadopsi otomatis sebagai `Verified` dan dialirkan langsung ke antrean Siap Rilis (*Zero-Friction Ingestion*).
3. **GAP-BFF-03 & OQ-BFF-03 (Standardisasi Respon Rilis & Gerbang Billing Ramah)**: Menyelaraskan mutasi `useReleaseOrderMutation` menggunakan skema Zod terstruktur `labOrderReleaseResponseSchema = z.object({ released: z.boolean(), billingStatus: z.string(), message: z.string() })`. Jika `CLEAR`, buka pratinjau cetak PDF otomatis (`LabResultPdfPreviewDialog.vue`); jika `BLOCKED`, tampilkan alert ramah penundaan fisik tanpa crash (akses internal EMR tetap aktif).
4. **GAP-BFF-04 (Inline Panic Value Guard)**: Menyertakan `criticalNote` pada `labResultRecordCmdSchema` dan mapper frontend. Jika ada analit `Critical`, input catatan readback telepon `CriticalNote` wajib diisi sebelum form dapat disimpan, tanpa popup dialog yang memblokir layar.
5. **GAP-BFF-05 & GAP-BFF-08 (1-Click Inline Medical Verification)**: Menyertakan `expertiseNote` pada `labResultVerifyCmdSchema`. Mengubah `LabVerificationPanel.vue` menjadi kanvas review terpadu dengan ringkasan temuan kritis di baris atas, chip template ekspertise instan, dan tombol verifikasi 1-klik langsung tanpa popup modal berulang.
6. **GAP-BFF-06 (Persona Routing `views/HasilLab.vue`)**: Mengaktifkan rute tab `hasil_lab` di `configs/tabs.ts` dan merefaktor `views/HasilLab.vue` menjadi *route wrapper* yang memanggil `LabWorkflowView.vue` dengan initial query `activeQueue = 'verification'`.
7. **GAP-BFF-07 (Spreadsheet-like Tabular Grid)**: Merefaktor `LabResultEntryPkTabular.vue` menjadi tabel HTML terpadu dengan navigasi keyboard fluid (`Enter`/`Tab`), auto-flagging instan di sisi client (*Normal/Low/High/Critical*), dan shortcut global `Ctrl+S`.
8. **GAP-BFF-09 (Pembersihan Alur Approval Finansial Usang)**: Menghapus alur kasir manual pada `LabFinancialClearancePanel.vue` dan `LabReleaseDialog.vue`, menggantikannya dengan tombol tunggal *"🖨️ Cetak & Rilis Hasil"* yang mengevaluasi gerbang `TRK-BILLING` secara otomatis.
9. **GAP-BFF-10 & OQ-BFF-01 (PA Auto-Sequencer & Form Adaptif Multidisiplin)**: Backend menyediakan sequencer otomatis nomor register fisik PA (`H.{YY}.{NoUrut:D4}` untuk `DISC-PAH` vs `S.{YY}.{NoUrut:D4}` untuk `DISC-PAS`). Form `LabResultEntryPaForm.vue` beradaptasi menyajikan Makroskopis/Gross untuk Histologi vs Adekuasi Sediaan + Rekomendasi untuk Sitologi.
10. **GAP-BFF-11 (Dialog Modal Pratinjau PDF Resmi)**: Membangun komponen `LabResultPdfPreviewDialog.vue` yang mengonsumsi stream PDF dari endpoint backend `fetchResultPdfBlob` dengan fitur cetak instan ke printer fisik.
11. **GAP-BFF-12 (Penegakan Invariant Alasan Amandemen & Otorisasi Ketat)**: Memperbarui guard pada `LabResultDocumentModel.AmendVerifiedToNewVersion` untuk menolak alasan < 10 karakter (**EX-03**) dan membatasi otorisasi amandemen pada client hanya untuk peran Dokter Sp.PK dan Supervisor Lab.

```text
[07-LABORATORY-DOMAIN] + [OC-LAB-HASIL-LAB-FEATURE] + [OC-LAB-HASIL-LAB-SCREEN-INVENTORY]
                         +
[OC-LAB-HASIL-LAB-FEASIBILITY-ASSESSMENT] + [OC-LAB-HASIL-LAB-BFF-FEASIBILITY-ASSESSMENT]
                         ↓
       [OC-LAB-HASIL-LAB-ARCHITECTURE (v1.2)]
```

---

# 3. Scope

## Included

### 1. Core Backend (`b09-bilreg-api`)
- **Domain Layer (`Bilreg.Domain.LabContext.LabResultFeature`)**:
  - Agregat `LabResultDocumentModel`:
    - Kolom `ExpertiseNote VARCHAR(2000)` dan `CriticalNote VARCHAR(1000)`.
    - Guard komponen mandatori (`EX-02`) pada metode `Verify`.
    - Invariant alasan amandemen resmi minimal 10 karakter (`EX-03`) pada metode `AmendVerifiedToNewVersion`.
    - Lifecycle status: `Draft (1)`, `Recorded (2)`, `Verified (3)`, `Released (4)`, `Amended (5)`.
    - Factory method `CreateFromLisIngestion(...)` yang langsung menetapkan status `Verified` dengan validator LIS asal.
  - Domain Service `LabResultFlagging`: evaluasi numerik dan teks terhadap rentang rujukan (Normal, Low, High, Critical).
- **Application Layer (`Bilreg.Application.LabContext.LabResultFeature`)**:
  - `LabResultGetQuery` & `LabResultGetHandler`: membaca dokumen aktif, memetakan DTO `LabResultView` lengkap dengan `ExpertiseNote`, `CriticalNote`, `AmendmentReason`, `PreviousVersionId`, `AmendedDate`, `AmendedUserId`.
  - `LabResultVerificationWorklistDal` & `ILabResultVerificationWorklistDal`: kueri daftar kerja verifikasi Sp.PK dengan field `IsCito`, `Gender`, `AgeAtOrder`, `LayananPerujukName`, `DokterPerujukName`, `TestNames`, `HasCriticalValue`, `VersionNo`, `ResultSource`, terurut `ORDER BY o.Urgency DESC, d.RecordedDate DESC`.
  - `LabResultRecordCmd`: menerima `CriticalNote`, mengevaluasi auto-flagging, dan memajukan status dokumen ke `Recorded`.
  - `LabResultVerifyCmd`: memvalidasi otorisasi role `doktersppk` / izin `lab.result.verify`, memeriksa kelengkapan komponen mandatori (`EX-02`), menyimpan `ExpertiseNote`, dan memajukan status ke `Verified`.
  - `LabResultIngestFromLisCmd`: menerima payload dari Oware, mencocokkan kode tes ke perancah order 1:1, menyimpan hasil bersumber `ExternalLis`, dan mengadopsi status `Verified` dengan nama validator LIS.
  - `LabResultAmendCmd`: memvalidasi otorisasi Sp.PK / Supervisor, menegakkan alasan >= 10 karakter, memensiunkan versi lama, dan membentuk versi N+1 berstatus `Recorded`.
  - `LabPaRegisterSequencer`: helper generator nomor urut register fisik PA menggunakan `ISequencer` (`H.{YY}.{NoUrut:D4}` untuk `DISC-PAH` vs `S.{YY}.{NoUrut:D4}` untuk `DISC-PAS`).
- **Application Layer (`Bilreg.Application.LabContext.LabOrderFeature`)**:
  - `LabOrderReleaseCmd`: mengevaluasi kelayakan tagihan kasir `TRK-BILLING` secara in-process synchronous, memutakhirkan order dan dokumen hasil ke `Released` jika `CLEAR`, dan mengembalikan DTO respon terstruktur `LabOrderReleaseResponse(bool Released, string BillingStatus, string Message)`.
- **Infrastructure & API Layer (`Bilreg.Infrastructure`, `Bilreg.Api`)**:
  - `LabResultController`: endpoint `record`, `verify`, `ingest-lis`, `worklist/verification`, `{orderId}`, `amend`, dan `{orderId}/pdf`.
  - `LabResultPdfRenderer`: penyusunan lembar cetak PDF resmi (PDFSharp) mencantumkan catatan ekspertise patolog, tanda tangan digital/identitas Sp.PK atau validator LIS, catatan nilai kritis, dan nomor register fisik PA.

### 2. Frontend Web Client (`c012_myhospital_web`)
- **Navigasi & Route Wrapper (`src/modules/Laboratory/views`, `configs`)**:
  - `configs/tabs.ts`: re-aktivasi konfigurasi menu tab `hasil_lab`.
  - `views/HasilLab.vue`: *Route Wrapper* terpadu yang memanggil `LabWorkflowView.vue` dengan preset filter antrean `activeQueue = 'verification'`.
  - `views/LabWorkflowView.vue`: meja kerja konsol terpadu yang menyajikan sub-antrean dinamis (Order, Sampling, Input Hasil, Verifikasi Medis, Siap Rilis, dan Integrasi LIS).
- **Formulir & Grid Entri Hasil (`components/result/`)**:
  - `LabResultEntryRouter.vue`: router penentu formulir analitis berdasarkan `serviceType` / disiplin order.
  - `LabResultEntryPkTabular.vue`: tabel grid spreadsheet keyboard-friendly dengan auto-advance baris (`Enter`/`Tab`), kalkulasi lokal auto-flagging instan, shortcut global `Ctrl+S`, dan soft-warning nilai kritis.
  - Penegakan *Inline Mandatory Panic Value Guard* (SCR-LAB-RES-08): field `CriticalNote` wajib diisi sebelum tombol simpan aktif jika terdapat analit berstatus `Critical`.
  - `LabResultEntryPaForm.vue`: formulir adaptif Patologi Anatomi:
    - Menampilkan nomor register fisik PA (`H.{YY}.{NoUrut}` vs `S.{YY}.{NoUrut}`).
    - Disiplin `DISC-PAH` (Histopatologi): menyajikan input *Pemeriksaan Makroskopis (Gross Description)*.
    - Disiplin `DISC-PAS` (Sitopatologi): menyajikan pemilih cepat *Kecukupan Sediaan (Specimen Adequacy)* dan *Rekomendasi Klinis*.
    - Komponen bersama: Asal Bahan Spesimen, Mikroskopis, Kesimpulan PA, dan Kode Morfologi ICD-O (SNOMED).
    - Tombol chip template diagnosis PA cepat.
  - `LabResultEntryMicroForm.vue`: formulir mikrobiologi terstruktur (pewarnaan gram, isolat kultur, tabel dinamis antibiogram / AST).
- **Stasiun Verifikasi Medis Sp.PK (`components/verification/`)**:
  - `LabVerificationPanel.vue`: kanvas review Sp.PK dengan banner ringkasan temuan abnormal/kritis di baris paling atas, editor `ExpertiseNote`, chip template ekspertise klinis cepat, dan tombol eksekusi verifikasi 1-klik langsung (*Direct 1-Click Verification*).
  - `LabVerificationWorklistRow.vue`: kartu daftar kerja verifikasi dengan badge prioritas CITO merah menyala di baris teratas, banner peringatan nilai kritis, ringkasan nama tes, dan penanda hasil bridging LIS (`[🤖 LIS Bridge • Tervalidasi Otomatis]`).
- **Alur Rilis & Billing Gate (`components/release/`, `components/dialogs/`)**:
  - `LabReleaseQueuePanel.vue`: antrean order terverifikasi yang siap diserahkan ke pasien dengan tombol tunggal *"🖨️ Cetak & Rilis Hasil"*.
  - Pembersihan (*retire*) komponen approval kasir usang: penghapusan `LabFinancialClearancePanel.vue` dan penyederhanaan `LabReleaseDialog.vue`.
  - Penanganan respon `LabOrderReleaseResponse`: jika `CLEAR` langsung membuka dialog PDF, jika `BLOCKED` menampilkan banner alert ramah penundaan fisik tanpa exception crash.
  - `LabResultPdfPreviewDialog.vue`: dialog modal pratinjau lembar hasil PDF resmi dengan tombol cetak ke printer fisik.
  - `LabAmendResultDialog.vue`: modal amandemen hasil dengan validasi panjang alasan minimal 10 karakter dan pembatasan peran pengguna (Sp.PK / Supervisor).
- **Service & Schemas TanStack Query (`queries/`, `types/`)**:
  - `types/lab-result.schema.ts`: pemutakhiran `labResultDetailSchema` (+ field catatan dan riwayat amandemen), `labResultRecordCmdSchema` (+ `criticalNote`), `labResultVerifyCmdSchema` (+ `expertiseNote`), dan `labResultVerificationWorklistItemSchema` (+ metadata antrean lengkap).
  - `types/lab-order.schema.ts`: penambahan `labOrderReleaseResponseSchema`.
  - `queries/LabResultService.ts`: integrasi query detail, worklist verifikasi Sp.PK, mutasi record, mutasi verify, dan mutasi amend.
  - `queries/LabOrderService.ts`: pembaruan mutasi `useReleaseOrderMutation` menggunakan `labOrderReleaseResponseSchema`.

### 3. Integration Middleware (`a046_oware_ordermiddlewareapi`)
- DDL Migrasi skema `OWARE_MapPemeriksaan` dengan kolom `ConsId VARCHAR(20)` dan indeks pencarian `(ConsId, LabTestCode)`.
- Reverse-lookup kode tes LIS ke `PemeriksaanId` HIS pada `MapPemeriksaanModel` dan `IMapPemeriksaanDal`.
- HTTP Client outbound `BilregLabResultClient` yang memanggil `POST /api/LabContext/LabResultFeature/ingest-lis` secara realtime dengan pencatatan log dan *fallback retry*.

## Excluded

1. Komunikasi tingkat rendah instrumen analitik laboratorium (driver serial RS-232, protokol ASTM, format pesan HL7 mentah) yang merupakan tanggung jawab sistem LIS eksternal / analyzer driver.
2. Penegakan diagnosis kerja klinis DPJP pada rekam medis (ranah `EMRContext`).
3. Transaksi pembayaran loket kasir dan penerbitan kuitansi keuangan (ranah `TRK-KASIR`).

---

# 4. Technical Decisions

## TD-01: Dual-Path Medical Verification Strategy
- **Manual Entry Path**: Ketika hasil diinput langsung oleh analis di HIS, verifikasi medis wajib melewati pengecekan otorisasi pada Controller/Handler: pengguna wajib memiliki Role `doktersppk` atau permission `lab.result.verify` (**EX-01**). Jika pengguna bukan Sp.PK, sistem menolak dengan HTTP 403 / Domain Exception. Dokumen juga memvalidasi seluruh komponen mandatori (`IsMandatory == true`) sebelum verifikasi disahkan (**EX-02**).
- **Bridging LIS Path (*Zero-Friction Ingestion*)**: Hasil analitik dari LIS pihak ketiga yang dialirkan via Oware dianggap telah melewati kendali mutu dan verifikasi di sistem LIS asal. Endpoint ingesti mencatat status dokumen langsung sebagai `Verified` dengan menyimpan nama validator yang dikirim LIS pada properti `VerifiedUserId` / `ValidatorName`, diberi badge `[🤖 LIS Bridge • Tervalidasi Otomatis]`, dan langsung dialirkan ke antrean `Siap Rilis` tanpa membebani Sp.PK untuk verifikasi ulang hasil instrumen rutin.

## TD-02: Realtime Ingestion REST API Contract & Oware Mapping Strategy
- Pemetaan kode tes dilakukan terpusat di sisi Oware menggunakan tabel `OWARE_MapPemeriksaan` yang diperkaya kolom `ConsId` (Vendor LIS):
  - Oware mengeksekusi reverse-lookup `(ConsId, LabTestCode) -> PemeriksaanId`.
  - Item hasil LIS yang kodenya belum terdaftar dilewati (*skipped*) dan dicatat pada `OWARE_ResultLog` untuk audit administrator lab.
  - Oware memanggil REST API internal Bilreg: `POST /api/LabContext/LabResultFeature/ingest-lis`.
- Request Payload memuat: `OrderId`, `LisRefNo`, `ValidatorName`, `ValidatedAt`, `CriticalNote`, dan koleksi `Components` `(ComponentCode, Value, Unit, ReferenceRangeText, Flag)`.
- Jika Bilreg API tidak dapat dihubungi (timeout/restart), Oware mencatat status antrean `Failed` dan mengeksekusi *retry* otomatis pada siklus berikutnya.

## TD-03: Deterministic Scaffold Direct Matching & Flag Adoption
- Bilreg menerima `ComponentCode` dari Oware yang telah dipetakan ke `PemeriksaanId` HIS.
- Bilreg langsung mencocokkan `ComponentCode` secara deterministik 1:1 terhadap `LabOrderItemComponent.ComponentCode` pada perancah order (*order scaffold*).
- Penentuan Flag Analitik: Bilreg mengadopsi flag analitik dari LIS asal (`Normal`, `Low`, `High`, `Critical`). Jika LIS tidak menyertakan flag, Bilreg mengevaluasi otomatis menggunakan domain service `LabResultFlagging`.
- Sistem mendukung hasil parsial: komponen yang berhasil dicocokkan langsung terisi nilainya, komponen yang belum dikirim tetap bernilai kosong/pending.

## TD-04: Non-Blocking Panic Value Policy (Soft-Warning & Inline Mandatory Note Guard)
- Auto-flagging menghitung status `Critical` menggunakan aturan rentang nilai kritis pada master test.
- Penanda Visual Menyerang (*Soft-Warning*): Baris analit kritis dan kartu antrean diberi penanda visual merah berkedip (*pulsing badge*) yang sangat kontras, serta ringkasan nilai kritis ditampilkan di baris paling atas (SCR-LAB-RES-08).
- *Inline Mandatory Note Guard*: Sesuai keputusan Grill-Me (GAP-BFF-04 / RISK-BFF-01), sistem **TIDAK** menggunakan dialog modal popup yang membekukan layar (agar analis dapat mengetik cepat tanpa hambatan). Namun, jika terdeteksi minimal satu analit `Critical`, field teks catatan pelaporan telepon `CriticalNote VARCHAR(1000)` di bawah tabel menjadi **wajib diisi (mandatory)** sebelum form dapat disimpan (mencatat siapa penerima laporan dan jam readback).

## TD-05: Unified Single-Console Laboratory Workspace Architecture (`LabWorkflowView.vue` with Persona Wrapper)
- Seluruh siklus hidup pengujian laboratorium dikelola dalam satu meja kerja konsol terpadu (`LabWorkflowView.vue`) pada tab `Tindakan Lab` guna memberikan efisiensi alur kerja maksimal tanpa friksi (*zero context-switching*) dan visibilitas 360 derajat status pasien.
- Menggunakan sub-antrean dinamis pada panel daftar kerja:
  1. `Antrean Order` (Order Masuk & Triage Cito)
  2. `Pengambilan Sampel` (Flebotomi & Panduan Tabung Vacutainer)
  3. `Input Hasil` (Order status `Collected` yang menunggu pencatatan nilai analitik)
  4. `Verifikasi Medis` (Khusus Dokter Sp.PK: telaah analitik, auto-flagging, indikator nilai kritis, dan catatan ekspertise)
  5. `Rilis & Billing` (Pemeriksaan kelayakan billing clearance `TRK-BILLING` dan penyerahan/cetak hasil resmi)
  6. `Integrasi LIS (Oware)` (Monitoring status bridging realtime hasil dari LIS pihak ketiga)
- *Hybrid Persona Routing*: Mengaktifkan rute tab `hasil_lab` pada `configs/tabs.ts` dan merefaktor `views/HasilLab.vue` menjadi *route wrapper* terpadu yang me-render `LabWorkflowView.vue` dengan initial preset query `activeQueue = 'verification'`. Dokter Spesialis Patologi Klinik yang membuka menu *"Hasil Lab"* langsung disajikan antrean verifikasi medis Sp.PK tanpa distraksi antrean order atau sampling.

## TD-06: Tabular Spreadsheet Data Entry Grid with Keyboard Navigation Protocol
- Menata ulang komponen `LabResultEntryPkTabular.vue` menjadi format tabel grid terpadu (*Spreadsheet-like Tabular Grid*) (SCR-LAB-RES-03).
- *Fluid Keyboard Navigation*: Menekan tombol `Enter` atau `Tab` pada kolom input nilai langsung memvalidasi input, menghitung status flag secara instan di sisi client (*Normal/Low/High/Critical*), dan memindahkan fokus kursor ke input baris berikutnya secara otomatis.
- *Quick Save Shortcut*: Menambahkan shortcut keyboard global `Ctrl+S` untuk mengeksekusi simpan hasil instan tanpa harus menggulirkan layar ke tombol bawah.

## TD-07: Single-Click Release & Non-Crashing Billing Gate Interaction
- SIMRS tidak memiliki siklus hidup approval kasir manual di instalasi laboratorium. Komponen approval kasir usang (`LabFinancialClearancePanel.vue`) dihapus (*retired*).
- Tombol tunggal *"🖨️ Cetak & Rilis Hasil"* langsung mengeksekusi mutasi `useReleaseOrderMutation` yang mengevaluasi gerbang `TRK-BILLING` secara in-process synchronous di backend `LabOrderReleaseCmd`.
- Kontrak Respon Terstruktur: Backend mengembalikan `LabOrderReleaseResponse(bool Released, string BillingStatus, string Message)`.
  - Jika `released === true` (`BillingStatus === 'CLEAR'`): Status order lokal berubah menjadi `Released`, dan dialog pratinjau cetak PDF resmi (`LabResultPdfPreviewDialog.vue`) langsung terbuka otomatis.
  - Jika `released === false` (`BillingStatus === 'BLOCKED'`): Menampilkan notifikasi informatif yang ramah bahwa dokumen fisik ditahan karena tagihan kasir belum lunas (tanpa error crash), tombol cetak fisik ke pasien ditahan, namun **akses pembacaan data medis oleh DPJP di EMR internal rumah sakit tetap terbuka**.

## TD-08: Unified Multi-Disciplinary Pathology Report with Dynamic Auto-Sequencer
- *Backend Auto-Sequencer*: Nomor register fisik PA dibangkitkan secara otomatis oleh sequencer backend (`b09-bilreg-api`) saat draft hasil PA terbentuk, berbasis klasifikasi disiplin master tes: prefix `H.{YY}.{NoUrut:D4}` untuk Histopatologi (`DISC-PAH`) dan prefix `S.{YY}.{NoUrut:D4}` untuk Sitopatologi (`DISC-PAS`). Nomor ini disimpan permanen pada komponen `PA_REG_NO` dan ditampilkan pada header formulir serta cetakan PDF.
- *Form PA Adaptif (`LabResultEntryPaForm.vue`)*:
  - Disiplin `DISC-PAH` (Histologi): Menampilkan kolom input *Pemeriksaan Sediaan Makroskopis (Gross Description)* (`PA_MACROSCOPIC`).
  - Disiplin `DISC-PAS` (Sitologi): Menyembunyikan makroskopis, dan menampilkan kolom *Kecukupan Sediaan / Adekuasi Sampel (Specimen Adequacy)* (`PA_SPECIMEN_ADEQUACY`) dengan tombol pilihan cepat (*Adekuat, Hiposeluler, Tercampur Darah*), serta kolom *Rekomendasi Klinis* (`PA_RECOMMENDATION`).
  - Komponen bersama: Asal Bahan Spesimen (`PA_SPECIMEN_SITE`), Mikroskopis (`PA_MICROSCOPIC`), Kesimpulan / Diagnosis PA (`PA_CONCLUSION`), dan Kode Morfologi ICD-O (`PA_SNOMED`).
  - Tombol chip template diagnosis PA cepat (*Fibroadenoma Mammae, Adenokarsinoma Kolon, Papillary Thyroid Carcinoma, dll.*).

## TD-09: Immutability Audit Trail & Role-Restricted Version N+1 Amendment
- Dokumen berstatus `Verified` terkunci permanen dari segala bentuk modifikasi langsung.
- Perubahan nilai pasca-verifikasi hanya dapat dilakukan melalui pembuatan versi amandemen baru N+1 via `LabResultAmendCmd`.
- *Strict Authorization*: Aksi amandemen di frontend dialog `LabAmendResultDialog.vue` dan backend handler dibatasi secara eksklusif untuk peran Dokter Spesialis Patologi Klinik (`doktersppk`) atau Supervisor Laboratorium (`supervisor`).
- *Strict Reason Invariant*: Metode `LabResultDocumentModel.AmendVerifiedToNewVersion` menegakkan validasi panjang alasan amandemen resmi minimal 10 karakter (**EX-03**): `if (r.Length < 10) throw new ArgumentException("Alasan amandemen resmi wajib diisi minimal 10 karakter.", nameof(reason));`.
- Versi lama dipensiunkan (`IsCurrentVersion = false`, `ResultStatus = Amended`), dan versi baru dibentuk dengan status `Recorded` serta relasi `PreviousVersionId` yang merujuk ke versi sebelumnya.

## TD-10: Backend-for-Frontend Data Aggregation & Rich Worklist Optimization
- Menghilangkan latensi jaringan dan masalah *N+1 HTTP calls* pada client dengan memusatkan agregasi data pada Read DTO dan Worklist DAL di backend:
  - `LabResultGetQuery` / `LabResultView` mengembalikan dokumen hasil lengkap dengan catatan klinis (`ExpertiseNote`, `CriticalNote`) dan jejak amandemen (`AmendmentReason`, `PreviousVersionId`, `AmendedDate`, `AmendedUserId`).
  - `LabResultVerificationWorklistDal` melakukan penggabungan data order, pasien, kamar/klinik, dokter perujuk, dan indikator nilai kritis dalam satu kali kueri SQL terindeks, dengan pengurutan prioritas gawat darurat (CITO) teratas.

---

# 5. Component Responsibilities

| Component | Repository / Layer | Responsibility |
|---|---|---|
| `LabResultDocumentModel` | `b09-bilreg-api` (Domain) | Entitas agregat inti dokumen hasil laboratorium: mengelola siklus hidup status (`Draft`, `Recorded`, `Verified`, `Released`, `Amended`), mutasi baris analit, aturan immutability, amandemen *full snapshot*, guard mandatori (`EX-02`), guard alasan amandemen minimal 10 karakter (`EX-03`), catatan ekspertise (`ExpertiseNote`), dan catatan nilai kritis (`CriticalNote`). |
| `LabResultFlagging` | `b09-bilreg-api` (Domain) | Domain service murni pengevaluasi rentang rujukan (Normal, Low, High, Critical) berbasis numerik dan teks. |
| `LabResultGetQuery` / `LabResultGetHandler` | `b09-bilreg-api` (Application) | Query handler pembaca data hasil lengkap: membaca dokumen aktif dan memetakan DTO `LabResultView` beserta catatan ekspertise, nilai kritis, dan riwayat amandemen. |
| `LabResultVerificationWorklistDal` | `b09-bilreg-api` (Infrastructure) | DAL Dapper pengambil antrean verifikasi Sp.PK dengan metadata lengkap (`IsCito`, `Gender`, `AgeAtOrder`, `LayananPerujukName`, `DokterPerujukName`, `TestNames`, `HasCriticalValue`, `VersionNo`, `ResultSource`) terurut prioritas CITO teratas. |
| `LabResultRecordCmd` | `b09-bilreg-api` (Application) | Use case perekaman hasil analitis analis: menerima nilai analit dan `CriticalNote`, mengevaluasi auto-flagging, dan memajukan status ke `Recorded`. |
| `LabResultVerifyCmd` | `b09-bilreg-api` (Application) | Use case pengesahan medis Sp.PK: memvalidasi peran `doktersppk` / `lab.result.verify`, memverifikasi kelengkapan mandatori, menyimpan `ExpertiseNote`, dan memajukan status ke `Verified`. |
| `LabResultIngestFromLisCmd` | `b09-bilreg-api` (Application) | Use case penerima hasil LIS dari Oware: memetakan tes 1:1 ke perancah order, menandai `ExternalLis`, dan menetapkan validator LIS otomatis. |
| `LabResultAmendCmd` | `b09-bilreg-api` (Application) | Use case amandemen hasil: memvalidasi otorisasi Sp.PK/Supervisor, memeriksa panjang alasan >= 10 karakter, memensiunkan versi lama, dan membentuk versi N+1. |
| `LabOrderReleaseCmd` | `b09-bilreg-api` (Application) | Use case rilis penyerahan hasil ke pasien: mengevaluasi kelayakan tagihan kasir `TRK-BILLING`, menandai status `Released`, dan mengembalikan DTO respon terstruktur. |
| `LabPaRegisterSequencer` | `b09-bilreg-api` (Application) | Helper generator nomor urut register fisik PA menggunakan `ISequencer` (`H.{YY}.{NoUrut:D4}` vs `S.{YY}.{NoUrut:D4}`). |
| `LabResultPdfRenderer` | `b09-bilreg-api` (Infrastructure) | Penghasil dokumen PDF resmi hasil laboratorium yang mencantumkan ekspertise patolog, tanda tangan Sp.PK / validator LIS, catatan nilai kritis, dan nomor versi aktif. |
| `LabResultController` | `b09-bilreg-api` (API) | REST API Controller untuk seluruh operasi hasil lab: entri, verifikasi, ingesti LIS, daftar kerja, amandemen, dan pengunduhan PDF. |
| `BilregLabResultClient` | `a046_oware_ordermiddlewareapi` (Infrastructure) | HTTP Client outbound yang memanggil endpoint ingesti Bilreg API secara realtime dengan mekanisme *fallback retry*. |
| `SaveResultLabCommandHandler` | `a046_oware_ordermiddlewareapi` (Application) | Handler penerima hasil LIS: memproses payload vendor LIS, melakukan reverse-lookup di `OWARE_MapPemeriksaan`, dan men-dispatch payload ke Bilreg. |
| `views/HasilLab.vue` | `c012_myhospital_web` (Views) | *Route Wrapper* terpadu persona Sp.PK: merender `LabWorkflowView.vue` dengan initial query `activeQueue = 'verification'`. |
| `configs/tabs.ts` | `c012_myhospital_web` (Configs) | Konfigurasi menu tab navigasi modul laboratorium, mengaktifkan kembali tab `hasil_lab`. |
| `views/LabWorkflowView.vue` | `c012_myhospital_web` (Views) | Tampilan utama meja kerja konsol terpadu laboratorium (antrean order, sampling, entri hasil analitik, verifikasi Sp.PK, rilis billing, dan LIS bridging). |
| `LabResultEntryRouter.vue` | `c012_myhospital_web` (Components) | Router penentu formulir analitis berdasarkan disiplin order (PK, PA, atau Mikrobiologi). |
| `LabResultEntryPkTabular.vue` | `c012_myhospital_web` (Components) | Grid spreadsheet input hasil PK dengan navigasi keyboard fluid (`Enter`/`Tab`), auto-flagging instan, shortcut `Ctrl+S`, dan soft-warning nilai kritis. |
| `LabResultEntryPaForm.vue` | `c012_myhospital_web` (Components) | Formulir adaptif PA: Histopatologi (`H.{YY}.{NoUrut}`, Makroskopis) vs Sitopatologi (`S.{YY}.{NoUrut}`, Adekuasi Sediaan, Rekomendasi), nomor register PA, dan chip diagnosis. |
| `LabResultEntryMicroForm.vue` | `c012_myhospital_web` (Components) | Formulir terstruktur mikrobiologi (pewarnaan gram, isolat kultur, tabel antibiogram dinamis AST). |
| `LabVerificationPanel.vue` | `c012_myhospital_web` (Components) | Stasiun review & verifikasi Sp.PK: ringkasan temuan kritis di baris atas, editor `ExpertiseNote`, chip template ekspertise klinis cepat, dan tombol verifikasi 1-klik langsung. |
| `LabVerificationWorklistRow.vue` | `c012_myhospital_web` (Components) | Kartu antrean verifikasi Sp.PK dengan badge CITO mencolok di baris atas, indikator nilai kritis, ringkasan nama tes, dan badge sumber LIS. |
| `LabReleaseQueuePanel.vue` | `c012_myhospital_web` (Components) | Panel antrean siap rilis dengan tombol tunggal *"🖨️ Cetak & Rilis Hasil"* yang mengevaluasi respon `LabOrderReleaseResponse`. |
| `LabResultPdfPreviewDialog.vue` | `c012_myhospital_web` (Components) | Dialog modal pratinjau lembar hasil PDF resmi dengan fitur streaming PDF viewer dan cetak ke printer fisik. |
| `LabAmendResultDialog.vue` | `c012_myhospital_web` (Components) | Dialog modal amandemen hasil dengan penegakan validasi panjang alasan minimal 10 karakter dan otorisasi Sp.PK / Supervisor. |
| `LabResultService.ts` / `LabOrderService.ts` | `c012_myhospital_web` (Queries) | TanStack Query hook, cache invalidation, dan service mutasi untuk modul laboratorium. |
| `lab-result.schema.ts` / `lab-order.schema.ts` | `c012_myhospital_web` (Types) | Definisi skema Zod typesafe untuk payload request, response, dan model data domain laboratorium. |

---

# 6. Integration Design

| Source | Target | Protocol / Contract | Purpose |
|---|---|---|---|
| Mesin Analyzer / LIS Vendor | `a046_oware_ordermiddlewareapi` | HTTP REST / JSON Payload | Pengiriman hasil pengujian analitik spesimen dari laboratorium pihak ketiga ke middleware Oware. |
| `a046_oware_ordermiddlewareapi` | `b09-bilreg-api` | HTTP POST `/api/LabContext/LabResultFeature/ingest-lis` (Internal Auth Token) | Bridging realtime hasil analitik LIS ke dokumen perancah order HIS. |
| `c012_myhospital_web` (`LabResultEntryPkTabular.vue`) | `b09-bilreg-api` | HTTP POST `/api/LabContext/LabResultFeature/record` (JWT Bearer Auth) | Pengiriman data rekaman nilai analitis komponen uji dan catatan nilai kritis (`CriticalNote`). |
| `c012_myhospital_web` (`LabVerificationPanel.vue`) | `b09-bilreg-api` | HTTP PATCH `/api/LabContext/LabResultFeature/verify` (JWT Bearer Auth) | Pengesahan verifikasi medis 1-klik oleh Dokter Sp.PK beserta catatan ekspertise (`ExpertiseNote`). |
| `c012_myhospital_web` (`LabAmendResultDialog.vue`) | `b09-bilreg-api` | HTTP PATCH `/api/LabContext/LabResultFeature/amend` (JWT Bearer Auth) | Pembentukan versi amandemen baru N+1 dengan alasan resmi minimal 10 karakter oleh Sp.PK / Supervisor. |
| `c012_myhospital_web` (`LabReleaseQueuePanel.vue`) | `b09-bilreg-api` | HTTP PATCH `/api/LabContext/LabOrderFeature/release` (JWT Bearer Auth) | Eksekusi rilis penyerahan hasil ke pasien dengan evaluasi gerbang billing `TRK-BILLING`. |
| `c012_myhospital_web` (`LabResultPdfPreviewDialog.vue`) | `b09-bilreg-api` | HTTP GET `/api/LabContext/LabResultFeature/{orderId}/pdf` (JWT Bearer Auth) | Pengunduhan stream berkas PDF resmi untuk pratinjau dan pencetakan fisik ke printer lokal. |
| `b09-bilreg-api` (`LabResultFeature`) | `b09-bilreg-api` (`LabOrderFeature`) | In-Process Aggregate Coordination | Penyelarasan status siklus hidup antara `LabOrderModel` dan `LabResultDocumentModel`. |
| `b09-bilreg-api` (`LabOrderFeature`) | `b09-bilreg-api` (`TataRekeningContext`) | `ILabBillingIntegration.ValidateReleaseEligibility` | Pengecekan kelayakan penyelesaian kewajiban finansial kasir sebelum hasil resmi dapat dirilis fisik ke pasien. |
| `b09-bilreg-api` (`LabResultFeature`) | `EMR` / Rawat Inap / IGD | Read Query via EMR Service | Akses langsung dokumen hasil terverifikasi untuk pertimbangan medis DPJP tanpa memandang status billing. |

---

# 7. Data Ownership

| Data Aggregate / Entity | Owner (Bounded Context) | Storage Table |
|---|---|---|
| Order Pemeriksaan Lab (`LabOrderModel`) | `LabContext` (`b09-bilreg-api`) | `BILRG_LabOrder`, `BILRG_LabOrderItem`, `BILRG_LabOrderItemComponent` |
| Dokumen Hasil Lab (`LabResultDocumentModel`) | `LabContext` (`b09-bilreg-api`) | `BILRG_LabResultDocument` |
| Rincian Nilai Komponen Hasil (`LabResultItemModel`) | `LabContext` (`b09-bilreg-api`) | `BILRG_LabResultItem` |
| Log & Antrean Masuk LIS Vendor | `Oware` (`a046_oware_ordermiddlewareapi`) | `OWARE_Result`, `OWARE_ResultLabPemeriksaan`, `OWARE_ResultLog` |
| Pemetaan Kode Tes Multi-Vendor | `Oware` (`a046_oware_ordermiddlewareapi`) | `OWARE_MapPemeriksaan` |
| Tagihan & Status Kelayakan Kasir | `TataRekeningContext` (`b09-bilreg-api`) | `BILRG_TrsBilling`, `ta_tata_rekening` |

---

# 8. Database Design

## Modified Tables

### Table: `BILRG_LabResultDocument` (`b09-bilreg-api`)
Penambahan kolom catatan ekspertise patolog dan catatan peringatan nilai kritis sederhana:

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

- Relasi `BILRG_LabResultDocument` 1-ke-N terhadap `BILRG_LabResultItem` berdasarkan `ResultDocumentId` (bersifat *immutable per version*).
- Relasi `BILRG_LabResultDocument` ke `BILRG_LabOrder` berdasarkan `OrderId`.
- Relasi jejak amandemen: `BILRG_LabResultDocument.PreviousVersionId` menunjuk ke `ResultDocumentId` dari versi sebelumnya (*Self-referencing audit trace*).

## Migration Considerations

- Seluruh penambahan kolom pada tabel `BILRG_LabResultDocument` menggunakan default constraint (`DEFAULT ''`, `DEFAULT 0`, `DEFAULT '3000-01-01'`), sehingga bersifat *non-breaking* dan kompatibel 100% terhadap data historis tanpa memerlukan downtime sistem.
- Penomoran register fisik PA menggunakan mekanisme auto-numbering `ISequencer` (`H.{YY}.{NoUrut:D4}` vs `S.{YY}.{NoUrut:D4}`) yang mengandalkan tabel penomoran standar `sys_penomoran` / sequencer helper yang sudah ada di `b09-bilreg-api`.

---

# 9. Cross-Cutting Concerns

1. **Security & Authorization**:
   - Endpoint internal `ingest-lis` dilindungi oleh API Key / Service Token internal antara Oware dan Bilreg.
   - Endpoint verifikasi lokal `verify` memeriksa klaim role pengguna (`User.IsInRole("doktersppk")` atau policy `lab.result.verify`).
   - Dialog dan use case amandemen `amend` membatasi eksekusi hanya untuk peran Dokter Spesialis Patologi Klinik (`doktersppk`) atau Supervisor Laboratorium (`supervisor`).
2. **Audit Trail & Immutability**:
   - Seluruh mutasi rekaman hasil dicatat melalui `AuditTrailType` (`Created`, `Modified`, `Voided`).
   - Dokumen berstatus `Verified` terkunci permanen dari segala bentuk modifikasi langsung; perubahan hanya diizinkan via `LabResultAmendCmd` yang membentuk ID dokumen baru N+1 dengan alasan minimal 10 karakter.
3. **Observability & Error Handling**:
   - Panggilan bridging dari Oware ke Bilreg dilengkapi pencatatan structured logging (`ResultId`, `OrderId`, `Duration`, `StatusCode`). Jika terjadi kegagalan HTTP, status transaksi di Oware ditandai `Failed` untuk memungkinkan aksi *retry* otomatis.
   - Kontrak mutasi rilis pada frontend client menangani status `BLOCKED` secara eksplisit dan ramah tanpa memicu unhandled runtime error crash.
4. **Performance & UX Ergonomics**:
   - Grid entri hasil PK mengadopsi navigasi keyboard fluid (`Enter`/`Tab`) dan auto-flagging instan di sisi client untuk memaksimalkan throughput analis pada puluhan sampel per jam.
   - Stasiun verifikasi Sp.PK menyediakan ringkasan temuan kritis di baris atas dan tombol verifikasi 1-klik langsung untuk memangkas kelelahan klik berulang pada verifikasi hasil normal volume tinggi.
   - Kueri daftar kerja meja hasil membatasi rentang tanggal default (hari ini) dan dioptimalkan melalui indeks `OrderId` dan `ResultStatus`.

---

# 10. Implementation Constraints

1. **Explicit SQL Persistence (Dapper)**: Seluruh interaksi basis data pada `b09-bilreg-api` wajib menggunakan SQL eksplisit via Dapper / ADO.NET (dilarang keras menggunakan Entity Framework / ORM berat).
2. **Tactical DDD & Encapsulated Invariants**: Logika bisnis invarian (guard mandatori, validasi status, amandemen snapshot, validasi panjang alasan amandemen minimal 10 karakter) wajib berada di dalam domain aggregate `LabResultDocumentModel`, bukan tersebar di controller atau frontend.
3. **Frontend Reactivity & Auto-Imports**: Komponen frontend di `c012_myhospital_web` wajib mematuhi panduan Vue 3 Composition API (`<script setup lang="ts">`), TanStack Query untuk state server, pemanfaatan sistem auto-import komponen UI, dan Tailwind CSS v4 untuk styling.
4. **No Fake Cashier Clearance**: Dilarang menghidupkan kembali tombol atau form persetujuan kasir manual di instalasi laboratorium; gerbang finansial dikendalikan 100% secara sinkron oleh domain `TRK-BILLING`.
5. **Targeted Verification Protocol**: Setiap perubahan pada modul backend dan frontend wajib diverifikasi secara bertahap melalui pengujian unit terarah (`dotnet test --filter ...` dan `pnpm test <spec-path>`), tanpa menjalankan monster test suite penuh selama proses implementasi per-slice.

---

# 11. Acceptance Conditions

Implementasi dinyatakan lengkap secara arsitektural apabila:

1. **AC-ARCH-01**: Endpoint `POST /api/LabContext/LabResultFeature/ingest-lis` berhasil menerima payload hasil dari Oware, memetakan ke perancah order secara deterministik 1:1, menandai `ResultSource = ExternalLis`, menetapkan validator LIS secara otomatis, serta mengalirkan hasil langsung ke antrean Siap Rilis.
2. **AC-ARCH-02**: Endpoint `PATCH /api/LabContext/LabResultFeature/verify` menolak eksekusi verifikasi manual jika akun pengguna bukan Dokter Sp.PK (`EX-01`), atau jika terdapat komponen mandatori yang masih kosong (`EX-02`).
3. **AC-ARCH-03**: Kolom `ExpertiseNote VARCHAR(2000)` dan `CriticalNote VARCHAR(1000)` tersimpan persisten di basis data, terbaca lengkap pada Read DTO `LabResultView`, dan tampil secara akurat pada lembar cetak PDF resmi.
4. **AC-ARCH-04**: Parameter uji dengan flag `Critical` memicu penanda visual merah berkedip (*soft-warning*), ringkasan temuan kritis di baris teratas, dan penegakan *inline mandatory guard* pada `CriticalNote` sebelum form dapat disimpan, tanpa popup dialog yang membekukan layar.
5. **AC-ARCH-05**: Status rilis dokumen hasil dan order tersinkronisasi secara akurat dengan evaluasi kelayakan tagihan `TRK-BILLING` melalui kontrak respon `LabOrderReleaseResponse(bool Released, string BillingStatus, string Message)`. Jika `CLEAR`, membuka otomatis dialog PDF hasil; jika `BLOCKED`, menampilkan alert ramah penundaan fisik tanpa exception crash.
6. **AC-ARCH-06**: Reverse-lookup di Oware via `OWARE_MapPemeriksaan` (+ `ConsId`) berhasil memetakan `LabTestCode` LIS ke `PemeriksaanId` HIS, mencatat log unmapped items, dan men-dispatch payload analitik ke Bilreg API secara realtime.
7. **AC-ARCH-07**: DTO `LabResultVerificationWorklistView` dan query DAL menyajikan metadata lengkap antrean verifikasi Sp.PK terurut prioritas CITO teratas (`ORDER BY o.Urgency DESC, d.RecordedDate DESC`).
8. **AC-ARCH-08**: Rute tab `hasil_lab` pada `configs/tabs.ts` aktif, dan `views/HasilLab.vue` berfungsi sebagai route wrapper yang me-render `LabWorkflowView.vue` dengan preset filter antrean `activeQueue = 'verification'`.
9. **AC-ARCH-09**: Komponen `LabResultEntryPkTabular.vue` beroperasi sebagai spreadsheet grid dengan navigasi keyboard fluid (`Enter`/`Tab`), auto-flagging instan di sisi client, dan shortcut simpan `Ctrl+S`.
10. **AC-ARCH-10**: Komponen `LabVerificationPanel.vue` menyediakan telaah Sp.PK terintegrasi dengan ringkasan temuan kritis, chip template ekspertise klinis cepat, dan tombol verifikasi medis 1-klik langsung.
11. **AC-ARCH-11**: Formulir `LabResultEntryPaForm.vue` beradaptasi menyajikan input Makroskopis untuk Histopatologi vs Adekuasi Sediaan dan Rekomendasi untuk Sitologi, serta menampilkan nomor register fisik PA yang dibangkitkan otomatis (`H.{YY}.{NoUrut}` vs `S.{YY}.{NoUrut}`).
12. **AC-ARCH-12**: Komponen modal `LabResultPdfPreviewDialog.vue` berhasil menampilkan pratinjau lembar hasil PDF resmi dari backend blob stream dan mendukung cetak langsung.
13. **AC-ARCH-13**: Metode `LabResultDocumentModel.AmendVerifiedToNewVersion` menolak alasan amandemen < 10 karakter (**EX-03**), dan aksi amandemen hanya dapat diakses oleh peran Dokter Sp.PK dan Supervisor Lab.
14. **AC-ARCH-14**: Seluruh pengujian unit terarah pada `b09-bilreg-api` (`Bilreg.Test`) dan `c012_myhospital_web` (`pnpm tc:app`, targeted vitest specs) lulus 100%.
