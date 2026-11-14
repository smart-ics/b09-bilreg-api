---
Title: Feasibility Assessment - Hasil Pemeriksaan Laboratorium Terverifikasi (Termasuk Bridging LIS via Oware)
Code: OC-LAB-HASIL-LAB
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-11-05
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan perubahan (*CHANGE-REQUEST*) untuk mengimplementasikan dan menyelaraskan kapabilitas pencatatan, validasi analitik, verifikasi medis oleh Dokter Spesialis Patologi Klinik, amandemen *full snapshot*, rilis administratif berbasis *billing clearance*, serta *bridging* penerimaan hasil LIS pihak ketiga via middleware Oware berdasarkan spesifikasi kanonikal [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md).

Referenced artifacts:

- ISSUE: [OC-LAB-HASIL-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ISSUE.md) (`ISSUE-LAB-HASIL-LAB-001`)
- OUTCOME: [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md)
- FEATURE: [OC-LAB-HASIL-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEATURE.md)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md), [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)

## Objective

Menilai kelayakan teknis, fungsional, dan arsitektural dari implementasi Outcome `OC-LAB-HASIL-LAB` pada backend `b09-bilreg-api`, middleware `a046_oware_ordermiddlewareapi`, dan frontend `c012_myhospital_web`; mengevaluasi kesenjangan (*gaps*) pada *codebase* eksisting; merumuskan strategi penimpaan/penyempurnaan (*overwrite/refactor*) sesuai arahan pengguna; serta merumuskan keputusan penutupan celah (*Gap Closure Decisions*) yang telah ditinjau dan dikonfirmasi bersama Product Owner sebelum perancangan arsitektur teknis difinalisasi oleh Arsitek (`ica-architect`).

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan penelusuran artefak dokumen dan basis kode:

## Existing Behavior

1. **Backend `b09-bilreg-api` (`LabResultFeature` & `LabOrderFeature`)**:
   - Model agregat [`LabResultDocumentModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultDocumentModel.cs) mengelola dokumen hasil dengan properti: `ResultDocumentId`, `OrderId`, `VersionNo`, `IsCurrentVersion`, `ResultSource`, `ResultStatus`, `RecordedDate`, `RecordedUserId`, `VerifiedDate`, `VerifiedUserId`, `AmendmentReason`, `AmendedDate`, `AmendedUserId`, `PreviousVersionId`, `AuditTrail`, dan koleksi `Items` ([`LabResultItemModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultItemModel.cs)).
   - Evaluasi rentang rujukan telah diterapkan pada [`LabResultFlagging.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultFlagging.cs) yang menghasilkan flag `Normal`, `Low`, `High`, dan `Critical` untuk tipe numerik.
   - Amandemen versi telah memiliki metode `AmendVerifiedToNewVersion` yang menghasilkan salinan versi lama non-aktif (`IsCurrentVersion = false`) dan versi baru (`VersionNo + 1`) berstatus `Recorded`.
   - Perintah verifikasi [`LabResultVerifyCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultVerifyCmd.cs) memperbarui status dokumen dan order menjadi `Verified`.
   - Perintah rilis [`LabOrderReleaseCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderReleaseCmd.cs) melakukan validasi gerbang tagihan ke domain Tata Rekening (`ILabBillingIntegration.ValidateReleaseEligibility`) dengan status `Clear` atau `Blocked`.
   - Renderer PDF telah tersedia pada [`LabResultPdfRenderer.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultPdfRenderer.cs) untuk menghasilkan berkas cetak PDF.

2. **Middleware `a046_oware_ordermiddlewareapi` (LIS Ingestion)**:
   - Controller [`ResultController.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs) menerima data hasil LIS pihak ketiga melalui `POST api/Result/lab` dan `POST api/Result/lab/reconcile`.
   - Parser JSON LIS seperti [`JsonResultLabPkHubParser.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/JsonResultLabPkHubParser.cs) mengekstraksi data hasil LIS ke model Oware.
   - Namun, hasil yang diterima Oware saat ini hanya disimpan ke tabel Oware (`OWARE_ResultLabPemeriksaan`) dan belum terhubung secara otomatis ke agregat `LabResultDocumentModel` di `b09-bilreg-api`.

3. **Frontend `c012_myhospital_web` (`src/modules/Laboratory/`)**:
   - Tampilan alur kerja utama [`LabWorkflowView.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/views/LabWorkflowView.vue) menggabungkan seluruh antrean dalam satu tampilan (`ordered`, `collected`, `verification`, `release`, `oware`).
   - Layar [`HasilLab.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/views/HasilLab.vue) saat ini berupa *placeholder* `<ModuleUnderDev>` dan dinonaktifkan di `tabs.ts`.
   - Komponen entri hasil ([`LabResultEntryForm.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/result/LabResultEntryForm.vue), `LabResultEntryPkTabular.vue`) dan dialog verifikasi ([`LabVerifyResultDialog.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/verification/LabVerifyResultDialog.vue)) telah tersedia, namun belum mendukung catatan ekspertise patolog, audit pelaporan nilai kritis, maupun penandaan LIS bridging yang komprehensif.

## Existing Constraints

1. **Domain Invariants**:
   - Dokumen hasil yang telah berstatus `Verified` bersifat *immutable* (tidak dapat diubah langsung).
   - Verifikasi medis dibatasi eksklusif hanya untuk Dokter Spesialis Patologi Klinik (`ORG-PPA`).
   - Setiap revisi hasil wajib membentuk nomor versi baru melalui mekanisme amandemen *full snapshot*.
   - Rilis administratif dokumen resmi ke pasien dikendalikan oleh gerbang kelayakan tagihan di `TRK-BILLING`.
2. **Keterikatan Prasyarat Siklus Hidup**:
   - Dokumen hasil hanya dapat dicatat terhadap order yang telah berstatus `Collected` (spesimen biologis telah berhasil diserahkan/diambil).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| **GAP-001** | CRITICAL | **Ketiadaan Validasi Kredensial Dokter Spesialis Patologi Klinik (`ORG-PPA`) pada Eksekusi Verifikasi Medis.** Handler `LabResultVerifyHandler` menerima sembarang `VerifiedUserId` tanpa memvalidasi klaim role pengguna. Hal ini melanggar AC-04, Business Condition 5.3 #3, dan Exception EX-01 pada `OC-LAB-HASIL-LAB`. |
| **GAP-002** | CRITICAL | **Ketiadaan Validasi Kelengkapan Komponen Pemeriksaan Mandatori (`IsMandatory`) saat Verifikasi.** Handler `LabResultVerifyHandler` dan model `LabResultDocumentModel` tidak memeriksa apakah seluruh parameter uji yang ditandai mandatori telah terisi sebelum mengubah status menjadi `Verified`. Hal ini melanggar AC-05, Business Condition 5.3 #2, dan Exception EX-02. |
| **GAP-003** | CRITICAL | **Ketiadaan Kontrak Ingesti & Integrasi Hasil Bridging dari LIS Pihak Ketiga via Oware.** Belum ada endpoint API resmi di `b09-bilreg-api` yang dipanggil oleh middleware Oware untuk mengalirkan hasil analitik LIS ke dalam dokumen hasil agregat `LabResultDocumentModel` dengan penandaan `ResultSource = External-LIS`. Hal ini mengakibatkan data hasil LIS terisolasi di database Oware dan tidak dapat diverifikasi oleh Dokter Patolog di HIS. |
| **GAP-004** | MAJOR | **Ketiadaan Kolom Catatan / Ekspertise Patolog Klinik pada Agregat, Skema Basis Data, Command, dan UI.** Dokumen hasil belum mencatat kesimpulan diagnostik atau saran klinis dari Dokter Spesialis Patologi Klinik (`ExpertiseNote` / `CatatanPatolog`). Hal ini melanggar Required Recorded Information 5.2 D. |
| **GAP-005** | MAJOR | **Ketiadaan Alur Konfirmasi & Audit Pelaporan Nilai Kritis (*Critical / Panic Value Reporting*).** Sistem berhasil menetapkan flag `Critical`, namun belum menyediakan struktur pencatatan bukti pelaporan segera ke perawat/DPJP (waktu telepon, nama pelapor, nama penerima laporan, catatan) sesuai Business Condition 5.3 #4 dan AC-08 / EX-05. |
| **GAP-006** | MAJOR | **Inkonsistensi Status Siklus Hidup Dokumen Hasil (`LabResultStatusEnum`) Terhadap Domain Transaksi.** `LabResultStatusEnum` hanya memiliki nilai `Draft`, `Recorded`, `Verified`. Status operasional `Released` dan `Amended` belum tercermin secara utuh pada dokumen hasil, melainkan hanya tercatat di level `LabOrderModel`. |
| **GAP-007** | MAJOR | **Fragmentasi Antarmuka Pengguna & Penonaktifan Layar Hasil Lab (`HasilLab.vue`).** Pada `c012_myhospital_web`, layar `HasilLab.vue` (`SC-08-02 Result Management`) dinonaktifkan sebagai placeholder, sementara `LabWorkflowView.vue` memadatkan antrean order, flebotomi, dan verifikasi tanpa visualisasi yang memadai untuk nilai kritis, ekspertise patolog, dan sumber data bridging LIS. Sesuai instruksi pengguna, *existing codebase* perlu ditimpa/ditingkatkan mutunya. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| **OQ-001** | Bagaimana mekanisme komunikasi ingesti hasil dari middleware Oware ke Bilreg API? | Menentukan kontrak API controller di Bilreg dan arsitektur dispatch di Oware. |
| **OQ-002** | Bagaimana aturan bisnis pelaporan nilai kritis (*Critical Panic Value*) saat terdeteksi hasil kritis? | Menentukan apakah pelaporan telepon bersifat memblokir (hard-block) atau peringatan informatif (soft-warning). |
| **OQ-003** | Bagaimana pengalaman pengguna terbaik di frontend terkait pemisahan layar antarmuka hasil laboratorium? | Menentukan struktur tab `tabs.ts` dan pemisahan workspace di `c012_myhospital_web`. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| **ASM-001** | Token JWT otentikasi pengguna di HIS menyediakan klaim Role (misal `doktersppk` atau permission `lab.result.verify`) untuk memvalidasi otorisasi verifikasi hasil yang dientri langsung secara manual. |
| **ASM-002** | Hasil analitik yang dikirim oleh LIS pihak ketiga melalui Oware telah divalidasi di sistem LIS asal, sehingga memuat identitas dokter/analis validator yang dapat langsung dipetakan ke metadata verifikasi dokumen hasil di Bilreg. |
| **ASM-003** | Order laboratorium yang berstatus `Collected` telah membentuk struktur perancah komponen hasil (*scaffold*) yang lengkap dan konsisten berdasarkan definisi tes pada order (`OC-LAB-TEST`). |
| **ASM-004** | Domain Tata Rekening (`TRK-BILLING`) menyediakan layanan integrasi `ILabBillingIntegration.ValidateReleaseEligibility` yang deterministik dalam mengembalikan status `CLEAR` atau `BLOCKED`. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| **RISK-001** | Ketidakcocokan kode komponen (*mapping mismatch*) antara hasil LIS pihak ketiga dan perancah internal HIS saat ingesti bridging. | Hasil dari mesin LIS gagal terpetakan ke dokumen hasil pasien. | Terapkan resolusi pencocokan berbasis kode komponen dan nama komponen dengan pencatatan audit log jika ada parameter LIS yang tidak dikenali, serta beri opsi koreksi manual oleh analis di UI. |
| **RISK-002** | Nilai kritis (*Panic Value*) tidak tersampaikan secara tepat waktu kepada dokter penanggung jawab pelayanan (DPJP). | Membahayakan keselamatan jiwa pasien. | Tampilkan indikator visual merah berkedip (*prominent critical banner*) pada daftar kerja dan form hasil, serta sediakan pencatatan konfirmasi pelaporan via telepon. |
| **RISK-003** | Regresi atau duplikasi fungsionalitas antara alur order flebotomi dengan meja kerja hasil laboratorium. | Kebingungan operasional petugas lab. | Lakukan pemisahan tab yang tegas: Tab `Tindakan Lab` fokus pada Order & Sampling, Tab `Hasil Lab` fokus pada Entri Hasil, Verifikasi Sp.PK, Bridging LIS, dan Rilis Cetak. |

---

# 7. Recommendations

## Option A: Penambalan Parsial In-Place (*Patch Existing Codebase*)

Mempertahankan seluruh struktur basis kode eksisting dan hanya menambal validasi Sp.PK serta menambahkan properti tambahan secara minimalis.

### Advantages
- Perubahan kode lebih sedikit pada fase awal.

### Disadvantages
- Struktur agregat dan skema data tetap terpecah dan membingungkan (lifecycle status tidak konsisten).
- Masalah bridging LIS via Oware tidak terselesaikan secara tuntas.
- Kualitas UI tetap di bawah standar pengalaman pengguna modern rumah sakit.

## Option B: Rekonstruksi & Penimpaan Berkualitas Tinggi (*Clean Overwrite & Alignment - Recommended & Approved*)

Sesuai arahan eksplisit pengguna ("jadikan referensi tapi saya ingin hasilnya lebih baik lagi. jadi boleh ditimpa"):
Melakukan refaktor komprehensif pada backend `LabResultFeature`, membangun kontrak bridging ingesti resmi realtime dari Oware ke Bilreg, melengkapi domain agregat dengan validasi klaim Sp.PK, guard mandatori, catatan ekspertise, dan audit nilai kritis informatif, serta memisahkan antarmuka frontend menjadi tab khusus `Hasil Lab` (`SC-08-02 Result Management`) yang modern, responsif, dan terintegrasi penuh.

### Advantages
- Selaras 100% dengan spesifikasi bisnis kanonikal [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md).
- Menuntaskan integrasi LIS pihak ketiga melalui middleware Oware secara elegan dan realtime.
- Memberikan kepastian legal medis (otoritas Sp.PK dan audit nilai kritis).
- Memberikan pengalaman pengguna yang jauh lebih baik dengan pemisahan meja kerja Order/Sampling vs Hasil/Verifikasi.

### Disadvantages
- Membutuhkan pemutakhiran pada skema tabel database dan beberapa file komponen frontend terkait.

---

# 8. Gap Closure (Confirmed Decisions)

Seluruh resolusi celah dan pertanyaan terbuka telah ditinjau dan dikonfirmasi bersama Product Owner:

## GAP-001 (Validasi Kredensial Dokter Spesialis Patologi Klinik)

### Decision
1. **Hasil Entri Manual (Direct Entry)**: Otorisasi verifikasi medis ditegakkan berbasis klaim JWT / Role pengguna (role `doktersppk` atau permission `lab.result.verify`). Pengguna non-Sp.PK yang mencoba memverifikasi ditolak dengan respon `403 Forbidden` / `InvalidOperationException`.
2. **Hasil Bridging LIS / Oware**: Status hasil dari LIS dianggap **telah tervalidasi (*already validated*)** di sistem LIS asal. Data validator (nama dokter/petugas verifikator) diambil langsung dari kiriman data LIS, sehingga saat di-ingest ke Bilreg, status dokumen langsung menjadi `Verified` (atau siap rilis) dengan identitas verifikator merujuk ke data validator LIS.

### Rationale
Menghormati pembagian tanggung jawab klinis: LIS pihak ketiga telah melewati alur otorisasi internal laboratorium analitik, sementara hasil entri lokal di HIS wajib diverifikasi oleh akun berkredensial Dokter Sp.PK.

### Impact
Pemeriksaan role pada `LabResultVerifyHandler` dan mapping validator LIS pada handler ingesti Oware.

### Architecture Impact
Penegakan authorization policy berbasis claim dan mapping validator LIS.

### Resolved By
User / Product Owner & Analyst (ica-analyst)

### Resolved Date
2026-11-05

---

## GAP-002 (Pengecekan Komponen Pemeriksaan Mandatori)

### Decision
Pada model domain `LabResultDocumentModel.Verify()`, tambahkan guard invariant:
```csharp
var missingMandatory = _items.Where(i => i.IsMandatory && string.IsNullOrWhiteSpace(i.ResultValue)).ToList();
if (missingMandatory.Any())
    throw new InvalidOperationException($"Komponen mandatori belum terisi: {string.Join(", ", missingMandatory.Select(m => m.ComponentName))}.");
```

### Rationale
Mencegah pengesahan hasil pemeriksaan laboratorium yang belum tuntas diuji sesuai AC-05 dan EX-02.

### Impact
Handler verifikasi secara otomatis menolak verifikasi bila ada komponen wajib yang kosong dan mengembalikan daftar nama komponen yang belum terisi.

### Architecture Impact
Penyempurnaan aturan invarian murni pada agregat domain `LabResultDocumentModel`.

### Resolved By
User / Product Owner & Analyst (ica-analyst)

### Resolved Date
2026-11-05

---

## GAP-003 & OQ-001 (Kontrak Bridging Ingesti LIS via Oware)

### Decision
1. **Protokol Komunikasi Realtime**: Oware langsung memanggil REST API endpoint internal di Bilreg: `POST /api/LabContext/LabResultFeature/ingest-lis` secara *realtime* begitu data hasil dari LIS pihak ketiga berhasil diparsing, dengan mekanisme *fallback retry* jika Bilreg sedang offline/sibuk.
2. **Pemetaan Komponen Deterministik**: Pencocokan otomatis berbasis `ComponentCode` / `ComponentId` terhadap perancah order (*order scaffold*). Item yang cocok langsung diisi nilainya beserta nilai rujukan dan auto-flagging.
3. **Dukungan Hasil Parsial (Bertahap)**: Sistem mendukung penerimaan hasil parsial bertahap; komponen yang sudah dikirim oleh LIS terisi nilainya, sedangkan komponen lain tetap menunggu (*pending*). Analis dan dokter patolog dapat melihat progres kelengkapan pada meja kerja hasil lab.
4. **Penandaan Sumber**: Dokumen hasil menandai `ResultSource = LabResultSourceEnum.ExternalLis`.
5. **Penyederhanaan Kontrak Klinis (Konteks HIS)**: Kontrak ingesti LIS hanya mentransfer fakta klinis inti (`OrderNo`, `ValidatorName`, `ValidatedDate`, `CriticalNote`, dan koleksi item: `ComponentCode`, `ResultValue`, `Flag`) tanpa membawa metadata teknis instrumen/analyzer LIS (nomor rak tabung, lot reagen, kurva kalibrasi).

### Rationale
Menghubungkan LIS pihak ketiga dan Bilreg secara langsung, cepat, dan transparan tanpa ketergantungan antrean message broker eksternal yang rumit.

### Impact
Penambahan endpoint ingesti di `Bilreg.Api` dan pembuatan outbound HTTP client di `a046_oware_ordermiddlewareapi`.

### Architecture Impact
Definisi API Contract baru antara Oware dan Bilreg.

### Resolved By
User / Product Owner & Analyst (ica-analyst)

### Resolved Date
2026-11-05

---

## GAP-004 (Catatan / Ekspertise Patolog Klinik)

### Decision
Tambahkan field `ExpertiseNote` (string, max 2000 karakter, opsional) pada:
1. `LabResultDocumentModel` (properti dan parameter `Verify()`).
2. Skema basis data `BILRG_LabResultDocument.sql` (kolom `ExpertiseNote VARCHAR(2000) NULL`).
3. DTO command `LabResultVerifyCmd` dan view query `LabResultDetailDto`.
4. Template PDF `LabResultPdfRenderer.cs` (ditampilkan di atas tanda tangan dokter).
5. Modal verifikasi di frontend `c012_myhospital_web`.

### Rationale
Memenuhi kebutuhan pencatatan interpretasi klinis atau saran diagnosis dari dokter spesialis patologi klinik sesuai dokumen kanonikal 5.2 D.

### Impact
Perubahan skema tabel `BILRG_LabResultDocument` dan kontrak API verifikasi.

### Architecture Impact
Penyelarasan persistensi dan presentasi data klinis hasil.

### Resolved By
User / Product Owner & Analyst (ica-analyst)

### Resolved Date
2026-11-05

---

## GAP-005 & OQ-002 (Penyederhanaan Penanganan Nilai Kritis - Properti CriticalNote Konteks HIS)

### Decision
1. **Peringatan Informatif (*Soft-Warning*)**: Sistem menampilkan indikator nilai kritis secara mencolok (badge merah menyala dan banner peringatan) pada daftar kerja, form hasil, serta lembar cetak hasil PDF saat terdeteksi flag `Critical`.
2. **Penyederhanaan Menjadi Properti `CriticalNote`**: Alih-alih membuat Value Object kompleks atau use case terpisah, informasi pelaporan nilai kritis disederhanakan menjadi satu properti teks: `CriticalNote VARCHAR(1000)` pada `LabResultDocumentModel`.
3. **Pengisian Fleksibel & Non-Blocking**: `CriticalNote` (misal: *"Sudah lapor ke Ns. Ani R. Melati via telp pk 14:15"*) dapat diisi langsung saat pencatatan hasil (`LabResultRecordCmd`), saat verifikasi Sp.PK (`LabResultVerifyCmd`), maupun saat ingesti LIS (`LabResultIngestFromLisCmd`), tanpa memblokir alur kerja verifikasi maupun rilis hasil.

### Rationale
Sesuai arahan Product Owner, konteks sistem informasi rumah sakit (HIS) berfokus pada catatan klinis esensial yang ringkas dan fleksibel, bukan struktur kompleks ala LIS mesin. Hal ini mempermudah tenaga medis mencatat komunikasi nilai kritis tanpa terhambat birokrasi formulir terpisah.

### Impact
Skema tabel `BILRG_LabResultDocument` hanya membutuhkan satu kolom tambahan `CriticalNote VARCHAR(1000) NOT NULL DEFAULT ''`, dan antarmuka UI menampilkan field input catatan kritis saat flag Critical terdeteksi.

### Architecture Impact
Penyederhanaan model domain dan eliminasi use case / Value Object terpisah.

### Resolved By
User / Product Owner & Analyst (ica-analyst)

### Resolved Date
2026-11-06

---

## GAP-006 (Harmonisasi Status Siklus Hidup Dokumen dan Order)

### Decision
Sinkronkan status siklus hidup dokumen hasil dan order:
- `LabResultStatusEnum` diselaraskan untuk merefleksikan tahapan: `Draft (1)`, `Recorded (2)`, `Verified (3)`, `Released (4)`, `Amended (5)`.
- Ketika `LabOrderReleaseCmd` sukses lolos validasi `TRK-BILLING`, selain mengupdate `LabOrderModel` menjadi `Released`, sistem juga memperbarui status `LabResultDocumentModel` menjadi `Released`.
- Ketika `LabResultAmendCmd` dijalankan, dokumen versi lama ditandai `ResultStatus = Amended` dan `IsCurrentVersion = false`.

### Rationale
Menghilangkan ambiguitas status antara agregat Order dan agregat Dokumen Hasil sehingga kedua entitas mencerminkan fakta bisnis yang selaras.

### Impact
Konsistensi status pelaporan pada antarmuka, PDF, dan kueri rekam medis.

### Architecture Impact
Penyelarasan mutasi status pada use case Release dan Amend.

### Resolved By
User / Product Owner & Analyst (ica-analyst)

### Resolved Date
2026-11-05

---

## GAP-007 & OQ-003 (Penguatan Meja Kerja Terpadu yang Dimodernisasi pada LabWorkflowView)

### Decision
1. **Pilihan Meja Kerja Terpadu Dimodernisasi (*Option A - Single Unified Workspace*)**:
   - Mempertahankan tab tunggal **`Tindakan Lab`** (`LabWorkflowView.vue`) sebagai meja kerja laboratorium terpadu (*Unified Laboratory Operational Desk*) guna menjamin efisiensi alur kerja tanpa friksi (*zero context-switching*), pencarian pasien 360 derajat di satu tempat, dan fleksibilitas bagi staf laboratorium yang bertugas.
   - Mengorganisasikan alur secara dinamis melalui sub-antrean kerja yang jelas dan adaptif pada panel daftar kerja:
     1. `Antrean Order` (Order Masuk & Triage Cito)
     2. `Pengambilan Sampel` (Flebotomi & Panduan Tabung Vacutainer)
     3. `Input Hasil` (Order status `Collected` yang menunggu pencatatan nilai analitik)
     4. `Verifikasi Medis` (Khusus Dokter Sp.PK: telaah analitik, auto-flagging, indikator nilai kritis, dan catatan ekspertise)
     5. `Rilis & Billing` (Pemeriksaan kelayakan billing clearance `TRK-BILLING` dan penyerahan/cetak hasil resmi)
     6. `Integrasi LIS (Oware)` (Monitoring status bridging realtime hasil dari LIS pihak ketiga)
2. **Panel Detail Adaptif Cerdas (*Context-Aware Panel*)**:
   - Panel detail beradaptasi secara otomatis sesuai status dan antrean aktif:
     - Jika antrean `Pengambilan Sampel`: menampilkan ringkasan tabung dan form konfirmasi flebotomi.
     - Jika antrean `Input Hasil`: menampilkan lembar entri hasil tabular yang leluasa dengan *real-time auto-flagging*.
     - Jika antrean `Verifikasi Medis`: menampilkan panel telaah klinis Sp.PK, badge status nilai kritis, dan input ekspertise.
     - Jika antrean `Rilis & Billing`: menampilkan visualisasi status clearance kasir dan aksi cetak PDF.
3. **Penimpaan (*Overwrite*) Codebase Lama**:
   - Seluruh komponen hasil lama yang kaku ditimpa dengan arsitektur komponen modern yang modular, responsif, dan reaktif sesuai standar Vue 3 Composition API.

### Rationale
Hasil laboratorium di rumah sakit merupakan alur berkesinambungan dari order hingga penyerahan hasil. Meja kerja terpadu adaptif menghilangkan hambatan bolak-balik tab, memberikan visibilitas komprehensif bagi petugas dan dokter, serta sangat fleksibel untuk berbagai kondisi beban kerja dan shift laboratorium.

### Impact
Penyederhanaan navigasi antarmuka dan modernisasi komponen di dalam `c012_myhospital_web/src/modules/Laboratory/views/LabWorkflowView.vue` dan folder `components/`.

### Architecture Impact
Penyatuan meja kerja presentasi frontend pada `LabWorkflowView.vue` dengan sub-antrean dan panel detail adaptif.

### Resolved By
User / Product Owner & Analyst (ica-analyst)

### Resolved Date
2026-11-05

---

# 9. Architecture Applicability

Record the Architecture Applicability decision.

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
Perubahan ini melibatkan:
1. Penambahan kontrak bridging baru realtime lintas repositori antara middleware Oware (`a046_oware_ordermiddlewareapi`) dan backend Bilreg (`b09-bilreg-api`).
2. Perubahan skema basis data dan model agregat domain (`LabResultDocumentModel`): penambahan catatan ekspertise patolog, audit nilai kritis, dan status siklus hidup baru.
3. Penegakan aturan otorisasi klaim role `doktersppk` pada hasil entri langsung, dan otomatisasi status tervalidasi pada hasil kiriman LIS.
4. Rekonstruksi dan modernisasi antarmuka pengguna meja kerja terpadu (`LabWorkflowView.vue`) di `c012_myhospital_web`.

Oleh karena itu, diperlukan perancangan arsitektur teknis formal (`OC-LAB-HASIL-LAB-ARCHITECTURE.md`) oleh Arsitek sebelum rencana implementasi dibuat.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved (GAP-001 s/d GAP-007 telah memiliki keputusan penutupan resmi dan disepakati Product Owner)
- [x] All required decisions recorded (Seluruh keputusan penutupan celah terdokumentasi lengkap dengan Decision, Rationale, Impact, Architecture Impact, Resolved By, dan Resolved Date)
- [x] All blocking open questions resolved (OQ-001 s/d OQ-003 telah dijawab dan disepakati solusinya)
- [x] Architecture can be finalized or updated (Persetujuan gerbang READY-FOR-PLANNING resmi diberikan oleh Arsitek)

## Status

**READY-FOR-PLANNING**

## Notes

Sesuai tata kelola Knowledge-Centric SDLC, peran Analis (*ica-analyst*) menyusun seluruh analisis kelayakan dan keputusan penutupan celah dengan status awal **NOT-READY**. Seluruh gap telah disepakati dan dikonfirmasi tuntas bersama Product Owner, sehingga dokumen ini siap ditinjau oleh peran Arsitek (*ica-architect*) untuk memberikan persetujuan gerbang (*grant* **READY-FOR-PLANNING**).

---

# 11. References

Referenced artifacts:

- ISSUE: [OC-LAB-HASIL-LAB-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ISSUE.md)
- OUTCOME: [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md)
- FEATURE: [OC-LAB-HASIL-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-FEATURE.md)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md), [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)

Referenced codebase locations:

- Backend Domain: [`Bilreg.Domain/LabContext/LabResultFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature)
- Backend Application: [`Bilreg.Application/LabContext/LabResultFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature)
- Backend Database: [`Bilreg.SqlDb/LabContext/LabResultFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabResultFeature)
- Middleware Oware: [`a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs)
- Frontend Module: [`c012_myhospital_web/src/modules/Laboratory`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory)
