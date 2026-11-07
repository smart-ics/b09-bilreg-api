# ISSUE

## Metadata

ID: ISSUE-LAB-HASIL-LAB-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Implementasi Outcome OC-LAB-HASIL-LAB: Hasil Pemeriksaan Laboratorium Terverifikasi (Termasuk Bridging LIS via Oware)

## Source

Reported By: User / Product Owner
Reported Date: 2026-11-05

## Description

Permintaan implementasi dan penyelarasan kapabilitas pencatatan, validasi analitik, verifikasi medis, pemeliharaan versi amandemen (*full snapshot versioning*), serta rilis administratif hasil pemeriksaan laboratorium (*validated laboratory result*) sebagai fakta bisnis persisten (*persisted business fact*) ke dalam sistem MyHospital Web berdasarkan spesifikasi kanonikal [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md).

Selain entri manual (*manual entry*) oleh petugas analis laboratorium, sistem hasil lab ini secara eksplisit disyaratkan harus mendukung mekanisme *bridging* untuk menerima dan merekonsiliasi hasil pemeriksaan dari sistem *Laboratory Information System* (LIS) pihak ketiga melalui middleware Oware ([`a046_oware_ordermiddlewareapi`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi)).

Sebelumnya telah diimplementasikan outcome-outcome prasyarat yang berada pada siklus hidup operasional sebelum hasil laboratorium:
1. Pendaftaran Pasien Eksternal ([OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md))
2. Pemesanan Pemeriksaan Laboratorium ([OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md))
3. Pembebanan Biaya / Tindakan Laboratorium (`OC-LAB-ORDER-CHARGE` / [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md))
4. Pengambilan Spesimen Laboratorium ([OC-LAB-SAMPLE-COLLECTION.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-SAMPLE-COLLECTION.md))
5. Definisi & Katalog Pemeriksaan Laboratorium ([OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md))

Terdapat *existing codebase* awal untuk hasil laboratorium di backend `b09-bilreg-api`, middleware `a046_oware_ordermiddlewareapi`, dan frontend `c012_myhospital_web`. Sesuai arahan pengguna, *codebase* lama dijadikan referensi namun ditargetkan untuk ditingkatkan kualitas dan keselarasan arsitekturnya, sehingga diizinkan untuk direfaktor, disempurnakan, atau ditimpa (*overwrite/replace*) agar sepenuhnya memenuhi standar kualitas, aturan bisnis, dan kondisi penerimaan kanonikal.

## Desired Outcome

Sesuai spesifikasi kanonikal pada [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md):

1. **Eksistensi Dokumen Hasil Laboratorium Unik & Perancah Terstruktur (*Scaffolding*)**:
   - Terbentuk satu dokumen hasil laboratorium unik (`ResultDocumentId`) dengan nomor versi yang sah (`VersionNumber`, diawali versi 1).
   - Dokumen terikat secara tepat pada satu permintaan laboratorium (`OrderLabId`) dan spesimen biologis yang sah (`SampleCollectionId`).
   - Memuat struktur rincian parameter uji terstruktur yang bersumber dari spesifikasi katalog tes pada order (nama komponen, satuan, dan rentang nilai rujukan).

2. **Perekaman Nilai Analitik & Evaluasi Rentang Rujukan Otomatis (*Auto-Flagging*)**:
   - Pencatatan nilai analitik per baris komponen uji sesuai tipe data (Numerik, Teks, Opsi/Kualitatif, Naratif).
   - Penentuan indikator keparahan nilai secara otomatis (*auto-flagging*): **Normal**, **Low (L)**, **High (H)**, atau **Critical (Panic Value)** berdasarkan usia dan jenis kelamin pasien (`PAS-DATSOS`).
   - Identifikasi dan audit khusus untuk kondisi nilai kritis (*Critical / Panic Value Reporting*).

3. **Dukungan Bridging & Integrasi Hasil dari LIS Pihak Ketiga via Oware (Middleware)**:
   - Sistem mampu menerima data hasil analitik yang dikirim oleh LIS pihak ketiga melalui perantara middleware Oware (`a046_oware_ordermiddlewareapi`).
   - Hasil dari LIS pihak ketiga terpetakan secara presisi ke dalam struktur perancah komponen hasil (*scaffold*) pada dokumen hasil yang terikat pada `OrderLabId`.
   - Sumber hasil ditandai secara akurat (`ResultSource = External-LIS` atau `Instrument`).
   - Hasil yang masuk via *bridging* tetap tunduk pada aturan bisnis yang sama: validasi rentang normal (*auto-flagging*), deteksi nilai kritis (*panic value*), serta mewajibkan penelaahan dan otorisasi verifikasi medis resmi oleh Dokter Spesialis Patologi Klinik sebelum dokumen dapat dirilis.

4. **Otorisasi Verifikasi Medis Tunggal oleh Dokter Spesialis Patologi Klinik**:
   - Otorisasi pengesahan verifikasi medis pada status `Verified` dibatasi secara mutlak hanya untuk akun pengguna berkredensial Dokter Spesialis Patologi Klinik (`ORG-PPA`).
   - Memvalidasi kelengkapan seluruh komponen uji mandatori (*mandatory*) sebelum verifikasi dapat dieksekusi.
   - Perekaman permanen identitas dokter patolog verifikator, stempel waktu verifikasi, dan catatan ekspertise klinis.

5. **Ketidakterubahan Dokumen Terverifikasi & Prosedur Amandemen (*Full Snapshot Versioning*)**:
   - Dokumen berstatus `Verified` bersifat *immutable* (tidak dapat diubah di tempat / dilarang *in-place update*).
   - Setiap revisi/koreksi hasil yang telah diverifikasi wajib melalui prosedur amandemen yang mewajibkan input alasan amandemen, menghasilkan nomor versi baru (`VersionNumber + 1`), membentuk salinan penuh seluruh komponen hasil (*full snapshot*), dan menandai versi terdahulu sebagai non-aktif (`IsCurrentVersion = false`).

6. **Pemisahan Tegas Validitas Klinis dan Gerbang Rilis Administratif (*Billing Gate*)**:
   - Dokumen berstatus `Verified` secara otomatis dapat diakses dan dilihat oleh tim medis internal rumah sakit (Rawat Jalan, Rawat Inap, IGD, EMR) demi keselamatan pasien (*clinical visibility preserved*).
   - Penyerahan berkas dokumen hasil resmi ke pasien (`Released`) dikendalikan secara independen melalui verifikasi kelayakan billing (`TRK-BILLING`):
     - Jika status billing `CLEAR`, hasil bertransisi menjadi `Released`.
     - Jika status billing `BLOCKED`, rilis administratif ditahan (*Blocked*), namun status medis internal tetap `Verified`.

7. **Penyajian Dokumen Resmi & Integrasi Antarmuka Web**:
   - Penghasilan dokumen hasil resmi (PDF) secara *on-demand* dengan penandaan versi operasional terkini.
   - Ketersediaan antarmuka pengguna yang mulus pada `c012_myhospital_web` untuk alur input nilai analis, pemantauan bridging Oware, *worklist* verifikasi dokter patolog, riwayat versi amandemen, dan panel verifikasi *billing clearance* saat rilis hasil.

## Current Situation

1. **Spesifikasi Kanonikal**:
   - Dokumen spesifikasi outcome telah tersedia di [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md) (Status: Draft, Versi 1.0).
2. **Kesiapan Outcome Hulu (Prasyarat)**:
   - Outcome-outcome sebelumnya telah selesai diimplementasikan:
     - `OC-LAB-TEST`: Katalog definisi tes, komponen, dan rentang rujukan.
     - `OC-LAB-REG-EXTERNAL`: Pendaftaran pasien eksternal laboratorium.
     - `OC-LAB-ORDER-LAB`: Pemesanan pemeriksaan lab (Rutin/Cito, Deferred/Ready).
     - `OC-LAB-SAMPLE-COLLECTION`: Pengambilan spesimen lab (`CollectionInfo`).
3. **Infrastruktur Bridging Eksisting di Middleware Oware (`a046_oware_ordermiddlewareapi`)**:
   - Layanan middleware Oware telah memiliki endpoint ingesti hasil lab dari LIS eksternal:
     - Controller: [`ResultController.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs) (`SaveResultLab`, `ReconcileResultLab`).
     - Parser LIS JSON: [`JsonResultLabPkHubParser.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/JsonResultLabPkHubParser.cs), [`JsonResultLabPaHubParser.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/JsonResultLabPaHubParser.cs), [`JsonResultLabMicroHubParser.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/JsonResultLabMicroHubParser.cs).
     - Tabel Basis Data Hasil LIS: `OWARE_Result.sql`, `OWARE_ResultLabPemeriksaan.sql`.
4. **Codebase Eksisting di Backend (`b09-bilreg-api`)**:
   - Modul `LabResultFeature`:
     - Domain: [`Bilreg.Domain/LabContext/LabResultFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature) (`LabResultDocumentModel`, `LabResultItemModel`, `LabResultFlagging`, `LabResultSourceEnum.ExternalLis`).
     - Application: [`Bilreg.Application/LabContext/LabResultFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature) (`LabResultRecordCmd`, `LabResultVerifyCmd`, `LabResultAmendCmd`, `LabResultGetQuery`, `LabResultVerificationWorklistQuery`, `LabResultPdfQuery`).
     - Infrastructure: [`Bilreg.Infrastructure/LabContext/LabResultFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabResultFeature) (`LabResultDocumentRepo`, `LabResultScaffoldService`, `LabResultPdfRenderer`).
     - Controller: [`Bilreg.Api/Controllers/LabContext/LabResultFeature/LabResultController.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabResultFeature/LabResultController.cs).
     - Basis Data SQL: `BILRG_LabResultDocument.sql`, `BILRG_LabResultItem.sql`.
   - Modul `LabOwareFeature`:
     - [`Bilreg.Application/LabContext/LabOwareFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOwareFeature) (`ILabOwareIntegration.cs`, `LabOwareOutboundWorker.cs`).
5. **Codebase Eksisting di Frontend (`c012_myhospital_web`)**:
   - Modul laboratorium di `src/modules/Laboratory/`:
     - Tampilan utama workflow [`LabWorkflowView.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/views/LabWorkflowView.vue) dan tab placeholder [`HasilLab.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/views/HasilLab.vue).
     - Komponen hasil: [`components/result/`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/result/) (`LabResultEntryForm.vue`, `LabResultEntryPkTabular.vue`, `LabResultComponentRow.vue`, `LabResultFlagBadge.vue`, dsb.).
     - Komponen integrasi Oware: [`LabOwareWorklistPanel.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/integration/LabOwareWorklistPanel.vue), [`LabOwareRetryDialog.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/integration/LabOwareRetryDialog.vue).
     - Composable & Service: [`composables/useLabResultEntry.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/composables/useLabResultEntry.ts), [`queries/LabResultService.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/queries/LabResultService.ts), [`queries/LabOwareService.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/queries/LabOwareService.ts).
6. **Kebutuhan Evaluasi & Peningkatan Mutu**:
   - Kode yang ada perlu dievaluasi secara menyeluruh terhadap kriteria penerimaan `OC-LAB-HASIL-LAB`:
     - Keandalan *bridging* penerimaan hasil LIS pihak ketiga melalui Oware ke dokumen hasil SIMRS.
     - Ketepatan alur scaffolding dari order & spesimen valid.
     - Penegakan integritas auto-flagging dan penanganan nilai kritis (*critical panic value*).
     - Integritas immutability dan pemeliharaan versi amandemen utuh (*full snapshot*).
     - Sinkronisasi gerbang kelayakan billing (`TRK-BILLING`) pada saat rilis hasil.
     - Sesuai instruksi pengguna, *existing codebase* boleh ditimpa / direfaktor untuk menghasilkan kualitas arsitektur dan pengalaman pengguna yang lebih baik.

## Evidence

- **Authoritative Outcome Specification**:
  - [OC-LAB-HASIL-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-HASIL-LAB.md) (Outcome definitif Result Management / SC-08 Laboratorium)
- **Domain Canonical References**:
  - [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md) (`LAB-RESULT`, `LAB-ORDER`, `LAB-COLLECT`)
  - [outcome-capability-domain-v2.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/outcome-capability-domain-v2.md)
  - [01-PASIEN-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/01-PASIEN-DOMAIN.md) (`PAS-DATSOS`)
  - [02-ORGANISASI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/02-ORGANISASI-DOMAIN.md) (`ORG-PPA`)
  - [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md) (`TRK-BILLING`)
- **Upstream Implemented Lab Outcomes**:
  - [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
  - [OC-LAB-SAMPLE-COLLECTION.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-SAMPLE-COLLECTION.md)
  - [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md)
  - [OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md)
- **Middleware Oware Integration References (`a046_oware_ordermiddlewareapi`)**:
  - API Controller: [`ResultController.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs)
  - Hub Result Parsers: [`JsonResultLabPkHubParser.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/Shared/JsonResultLabPkHubParser.cs)
  - SQL Scripts: [`OWARE_ResultLabPemeriksaan.sql`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.SqlDb/TablesScripts/OWARE_ResultLabPemeriksaan.sql)
- **Existing Backend References (`b09-bilreg-api`)**:
  - Domain Model: [`LabResultDocumentModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultDocumentModel.cs)
  - Domain Items & Flagging: [`LabResultItemModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultItemModel.cs), [`LabResultFlagging.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabResultFeature/LabResultFlagging.cs)
  - Application Layer: [`Bilreg.Application/LabContext/LabResultFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabResultFeature)
  - Oware Integration Layer: [`Bilreg.Application/LabContext/LabOwareFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOwareFeature)
  - API Controller: [`LabResultController.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabResultFeature/LabResultController.cs)
  - Database SQL Scripts: [`BILRG_LabResultDocument.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabResultFeature/BILRG_LabResultDocument.sql)
- **Existing Frontend References (`c012_myhospital_web`)**:
  - Views: [`HasilLab.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/views/HasilLab.vue), [`LabWorkflowView.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/views/LabWorkflowView.vue)
  - Navigation / Tabs: [`tabs.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/configs/tabs.ts)
  - Result Components: [`c012_myhospital_web/src/modules/Laboratory/components/result`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/result)
  - Oware Panel: [`LabOwareWorklistPanel.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/integration/LabOwareWorklistPanel.vue)
  - Services & Schemas: [`LabResultService.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/queries/LabResultService.ts), [`LabOwareService.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/queries/LabOwareService.ts), [`lab-result.schema.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/types/lab-result.schema.ts)

## Notes

- Dokumen ini merupakan artefak intake murni yang bersifat solusi-netral (*solution-neutral*), berfokus mendokumentasikan APA yang diminta dan kondisi eksisting tanpa mendikte solusi teknis atau rincian desain arsitektur.
- Batasan di luar lingkup (*Out of Scope*):
  - **Protokol Mesin Fisik Analyzer**: Komunikasi serial tingkat rendah (RS-232, ASTM, HL7 frame mentah) dikelola oleh vendor LIS / driver instrumen laboratorium; Oware dan HIS beroperasi pada tingkat rekonsiliasi data hasil.
  - **Keputusan Klinis Diagnosa Medis Pasien**: Analisis dokter DPJP dalam menetapkan diagnosis penyakit akhir berdasarkan hasil laboratorium (domain EMR / Pelayanan Klinis).
  - **Prosedur Pengambilan Sampel & Flebotomi**: Pengambilan darah dan penanganan spesimen di ruang sampling (`LAB-COLLECT` / Outcome *SampleCollection*).
  - **Pembayaran Kasir & Penagihan Piutang**: Transaksi kasir, pelunasan kwitansi, atau klaim asuransi (`TRK-KASIR` dan `TRK-BILLING`).
- Alur kerja berikutnya untuk CHANGE-REQUEST ini diteruskan ke peran **ica-analyst** untuk tahapan **Discovery**, **FEASIBILITY-ASSESSMENT**, dan penyelarasan spesifikasi **FEATURE**.
