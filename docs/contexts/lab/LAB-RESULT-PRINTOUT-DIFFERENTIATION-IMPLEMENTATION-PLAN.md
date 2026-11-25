---
Title: Implementation Plan Diferensiasi Cetakan Hasil Laboratorium PK, PA, dan Mikro
Code: LAB-RESULT-PRINTOUT-DIFFERENTIATION-IMPLEMENTATION-PLAN
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-25
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan diferensiasi cetakan resmi (*print-out* dokumen PDF) hasil pemeriksaan laboratorium pada **`b09-bilreg-api`** dan penyelarasan preview pada **`c012_myhospital_web`** agar mencerminkan karakteristik klinis masing-masing disiplin laboratorium (*Clinical Pathology*, *Anatomical Pathology*, dan *Microbiology*).

Tujuan spesifik realisasi ini:
1. **Lab Patologi Klinik (PK)**:
   - Menyajikan hierarki visual berjenjang: **Disiplin** (Level 1, shaded/bold header) $\rightarrow$ **Group Pemeriksaan** (Level 2, sub-header bila ada) $\rightarrow$ Daftar parameter.
   - Kolom pemeriksaan hanya menampilkan nama **Parameter / Komponen** (misal: *Hemoglobin*, *Leukosit*), tanpa mengulang nama tes/grup di kolom test.
   - Mengadopsi struktur tabel 5 kolom terpisah: `[Pemeriksaan / Parameter]` | `[Hasil]` | `[Satuan]` | `[Nilai Rujukan]` | `[Flag / Status]`.
   - Mempertahankan kotak peringatan nilai kritis (*Critical Value Alert*) dan catatan ekspertise Sp.PK.
2. **Lab Patologi Anatomi (PA)**:
   - Menyajikan format laporan naratif klinis diagnostik resmi beridentitas nomor registrasi PA (`H.{yy}.{NoUrut}` / `S.{yy}.{NoUrut}`), nomor sediaan/slide, dan topografi asal spesimen.
   - Seksi naratif terstruktur: Pemeriksaan Makroskopis, Pemeriksaan Mikroskopis, kotak sorotan khusus Kesimpulan / Diagnosis Patologi, Rekomendasi Klinis, serta Kode Morfologi (ICD-O / SNOMED CT).
   - Blok tanda tangan verifikasi Dokter Spesialis Patologi Anatomi (Sp.PA).
3. **Lab Mikrobiologi (Mikro)**:
   - Menyajikan laporan mikrobiologi komprehensif: Sediaan Langsung (Gram/BTA/KOH), Biakan & Identifikasi Mikroorganisme (Status Pertumbuhan, Hitung Koloni, Organisme yang Diisolasi).
   - Tabel Uji Kepekaan Antibiotik (*Antibiogram / AST*): `[Nama Antibiotik]` | `[Nilai MIC (µg/mL)]` | `[Sensitivitas (S / I / R)]` dengan legenda standar CLSI/EUCAST.
   - Penanganan dinamis untuk biakan steril: Menyembunyikan tabel AST bila tidak ada pertumbuhan kuman dan menampilkan keterangan informatif.
   - Blok tanda tangan verifikasi Dokter Spesialis Mikrobiologi Klinik (Sp.MK).
4. **Frontend HIS Web**:
   - Menyelaraskan judul modal dialog preview PDF pada `LabResultPdfPreviewDialog.vue` agar dinamis dan informatif sesuai dengan `ServiceType` order.

Planning Mode: **FEATURE-PLANNING**

Referenced artifacts:
- ARCHITECTURE: [LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-ORDER-PA-MICRO-ACCOMMODATION-ARCHITECTURE.md)
- ARCHITECTURE: [OC-LAB-HASIL-LAB-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-HASIL-LAB-ARCHITECTURE.md)
- DOMAIN Bilreg: [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)

Architecture Applicability: **`ARCHITECTURE-REQUIRED`**

---

# 2. Planning Scope

Rencana kerja ini mencakup perubahan pada backend Core Bilreg (`b09-bilreg-api`) dan frontend HIS Web (`c012_myhospital_web`), didekomposisi ke dalam 3 phase dan 6 slice terisolasi:

1. **Phase 1: Application Layer DTO & View Model Enrichment (`b09-bilreg-api`)**:
   - Pengayaan `LabResultPdfView` dan `LabResultPdfItemView` pada `Bilreg.Application` untuk menampung `ServiceType`, `DokterPerujukName`, `LayananPerujukName`, per-item `DisciplineName`, `TestGroupName`, `ComponentCode`, `ResultType`, serta metadata PA dan Mikro.
   - Pemetaan korelasi item hasil dengan snapshot baris order (`order.Items`) pada `LabResultPdfHandler`.
2. **Phase 2: Specialized PDF Renderers & Strategy Orchestrator (`b09-bilreg-api`)**:
   - Pembuatan `LabResultPkPdfRenderer` untuk tabel 5 kolom berjenjang (Disiplin $\rightarrow$ Group $\rightarrow$ Parameter).
   - Pembuatan `LabResultPaPdfRenderer` untuk laporan naratif terstruktur diagnosis PA.
   - Pembuatan `LabResultMicroPdfRenderer` untuk laporan kultur dan tabel kepekaan antibiotik AST.
   - Pembaruan orchestrator `LabResultPdfRenderer` yang merutekan rendering berdasarkan `view.ServiceType` beserta pengujian unit komprehensif pada `Bilreg.Test`.
3. **Phase 3: Frontend HIS Web Dialog Title & Preview Alignment (`c012_myhospital_web`)**:
   - Penyesuaian `LabResultPdfPreviewDialog.vue` agar menampilkan judul preview kontekstual sesuai tipe layanan order.
   - Verifikasi pengujian unit pada `LabResultPdfPreviewDialog.spec.ts`.

---

# 3. Dependencies

- **Prasyarat Sistem**:
  - `LaboratoryServiceType` (`ClinicalPathology = 1`, `AnatomicalPathology = 2`, `Microbiology = 3`) sudah terdefinisi di `Bilreg.Domain.LabContext.LabOrderFeature`.
  - Snapshot order item `BILRG_LabOrderItem` (`DisciplineName`, `TestGroupName`) sudah tersimpan pada saat order dibuat.
  - Komponen hasil PA (`PA_REG_NO`, `PA_SLIDE_NO`, `PA_SPECIMEN_SITE`, `PA_MACROSCOPIC`, `PA_MICROSCOPIC`, `PA_CONCLUSION`, dll.) dan Mikro (`MIC_DIRECT_SMEAR`, `MIC_GROWTH_STATUS`, `MIC_COLONY_COUNT`, `MIC_ISOLATED_ORGANISM`, `AST_*`) sudah tersimpan di `BILRG_LabResultItem`.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Application View Model Enrichment | IMPLEMENTED | GO | 1/1 |
| P2 - Specialized PDF Renderers & Strategy Orchestration | IMPLEMENTED | GO | 4/4 |
| P3 - Frontend Dialog Preview Alignment | IMPLEMENTED | GO | 1/1 |

---

# 5. Phases

## P1 - Application View Model Enrichment

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P1-S01
Title: Pengayaan LabResultPdfView & Korelasi Snapshot Order pada LabResultPdfHandler  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperluas DTO view `LabResultPdfView` dan `LabResultPdfItemView` pada `Bilreg.Application` agar memuat `ServiceType`, metadata perujuk (`DokterPerujukName`, `LayananPerujukName`), serta atribut snapshot klasifikasi (`DisciplineName`, `TestGroupName`, `ComponentCode`, `ResultType`, nilai naratif/opsi), dan mengimplementasikan korelasi snapshot order item pada `LabResultPdfHandler.MapToPdfView`.  
Depends On: None  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabResultPdfView` memiliki properti `ServiceType` (`LaboratoryServiceType`), `DokterPerujukName`, dan `LayananPerujukName`.
- `LabResultPdfItemView` memiliki properti `DisciplineName`, `TestGroupName`, `ComponentCode`, `ResultType` (`LabResultTypeEnum`), `NarrativeValue`, dan `OptionValue`.
- `LabResultPdfHandler.MapToPdfView` mengorelasikan `result.Items` dengan `order.Items` berdasarkan `TestId` atau `TestName` untuk mengisi `DisciplineName` dan `TestGroupName`.
- Unit test `LabResultPdfHandlerTest` diperbarui dan lulus verifikasi `dotnet test`.  
Notes: Diimplementasikan pengayaan LabResultPdfView (ServiceType, DokterPerujukName, LayananPerujukName) dan LabResultPdfItemView (DisciplineName, TestGroupName, ComponentCode, ResultType, NarrativeValue, OptionValue). Ditambahkan korelasi snapshot order item pada LabResultPdfHandler.MapToPdfView. File diubah: src/bilreg/Bilreg.Application/LabContext/LabResultFeature/UseCases/LabResultPdfQuery.cs dan src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultPdfHandlerTest.cs.

---

## P2 - Specialized PDF Renderers & Strategy Orchestration

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P2-S02
Title: Implementasi LabResultPkPdfRenderer (Tabel 5 Kolom Berjenjang Disiplin & Group)  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Membangun renderer PDF khusus untuk Patologi Klinik (`LabResultPkPdfRenderer`) yang mengelompokkan analit berdasarkan Disiplin (Level 1) dan Group Pemeriksaan (Level 2), menampilkan hanya nama parameter di kolom pemeriksaan, menyajikan tabel 5 kolom terpisah (`Parameter` | `Hasil` | `Satuan` | `Nilai Rujukan` | `Flag`), serta mempertahankan banner nilai kritis dan tanda tangan Sp.PK.  
Depends On: P1-S01  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Komponen `LabResultPkPdfRenderer` terimplementasi di `Bilreg.Infrastructure/LabContext/LabResultFeature/`.
- Judul laporan: "HASIL PEMERIKSAAN PATOLOGI KLINIK".
- Menghasilkan header seksi berlatar/shaded untuk tiap `DisciplineName` unik.
- Menghasilkan sub-header dengan indentasi untuk tiap `TestGroupName` unik (jika ada).
- Kolom tabel: `[Pemeriksaan / Parameter]`, `[Hasil]`, `[Satuan]`, `[Nilai Rujukan]`, `[Flag / Status]`.
- Kolom `Pemeriksaan / Parameter` hanya menampilkan nama parameter (`ComponentName` atau fallback `TestName`), tanpa format ganda `TestName / ComponentName`.
- Menampilkan kotak peringatan merah jika terdapat flag kritis (`FlagLabel == "C"`).
- Menampilkan catatan ekspertise Patologi Klinik dan tanda tangan validator/Sp.PK.  
Notes: Mengimplementasikan LabResultPkPdfRenderer di Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultPkPdfRenderer.cs dengan tabel 5 kolom berjenjang (Disiplin shaded Level 1, Group bullet indent Level 2, Parameter indent Level 3), pemformatan nama parameter tunggal (ComponentName / fallback TestName), nilai tanpa satuan di kolom hasil, alert banner nilai kritis merah, catatan ekspertise, tanda tangan validator Sp.PK digital, dan pagination handling aman. Pengujian unit komprehensif ditambahkan di Bilreg.Test/LabContext/LabResultFeature/LabResultPkPdfRendererTest.cs dan seluruh pengujian lolos (3/3 passed).

---

### P2-S03
Title: Implementasi LabResultPaPdfRenderer (Laporan Naratif Diagnosis Patologi Anatomi)  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Membangun renderer PDF khusus untuk Patologi Anatomi (`LabResultPaPdfRenderer`) yang menyajikan laporan diagnostik naratif formal dengan identitas nomor registrasi PA, sediaan/slide, topografi spesimen, uraian makroskopis & mikroskopis, blok sorotan diagnosis patologi, rekomendasi klinis, dan tanda tangan Sp.PA.  
Depends On: P1-S01  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Komponen `LabResultPaPdfRenderer` terimplementasi di `Bilreg.Infrastructure/LabContext/LabResultFeature/`.
- Judul laporan: "HASIL PEMERIKSAAN PATOLOGI ANATOMI".
- Header spesimen menyajikan No. Registrasi PA (`PA_REG_NO`), No. Sediaan / Slide (`PA_SLIDE_NO`), Asal Jaringan (`PA_SPECIMEN_SITE`), dan Adekuasi Sediaan (`PA_SPECIMEN_ADEQUACY`).
- Seksi naratif tersusun rapi: "Pemeriksaan Makroskopis" dan "Pemeriksaan Mikroskopis" dengan auto-wrapping teks.
- Seksi "Kesimpulan / Diagnosis Patologi" ditampilkan dalam kotak sorotan khusus (*highlight box*) yang mencolok dan jelas.
- Seksi rekomendasi klinis dan kode morfologi (ICD-O / SNOMED CT) ditampilkan bila ada.
- Area verifikasi menampilkan label penanggung jawab "Dokter Spesialis Patologi Anatomi (Sp.PA)".  
Notes: Mengimplementasikan LabResultPaPdfRenderer di Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultPaPdfRenderer.cs yang menyajikan laporan diagnostik naratif formal Patologi Anatomi. Mencakup: Header RS & judul resmi ('HASIL PEMERIKSAAN PATOLOGI ANATOMI'), header identitas pasien 2-kolom (No. Order, No. RM, Pasien, Gender/Umur, Tgl Lahir, RegId, Dokter Perujuk, Layanan Perujuk, Status), kotak identitas spesimen bergaris batas (No. Reg PA, No. Sediaan/Slide, Asal Jaringan, Adekuasi Sediaan), seksi naratif Makroskopis & Mikroskopis dengan text wrapping otomatis, kotak sorotan khusus (highlight box border 1.5 shaded background) Kesimpulan / Diagnosis Patologi, seksi kode morfologi (ICD-O / SNOMED CT), seksi rekomendasi klinis / saran, penanganan aman multi-halaman (continuation running header), dan blok tanda tangan verifikator Dokter Spesialis Patologi Anatomi (Sp.PA). Ditambahkan suite unit test di Bilreg.Test/LabContext/LabResultFeature/LabResultPaPdfRendererTest.cs (4 test cases, seluruhnya lulus verifikasi).

---

### P2-S04
Title: Implementasi LabResultMicroPdfRenderer (Laporan Kultur & Matriks Antibiogram AST)  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Membangun renderer PDF khusus untuk Mikrobiologi (`LabResultMicroPdfRenderer`) yang menyajikan sediaan langsung mikroskopis, hasil biakan & koloni kuman, serta tabel kepekaan antibiotik (AST) dengan deteksi otomatis kondisi steril (tanpa pertumbuhan kuman).  
Depends On: P1-S01  
Repository: `b09-bilreg-api`  
Completion Criteria:
- Komponen `LabResultMicroPdfRenderer` terimplementasi di `Bilreg.Infrastructure/LabContext/LabResultFeature/`.
- Judul laporan: "HASIL PEMERIKSAAN MIKROBIOLOGI".
- Seksi sediaan langsung menyajikan hasil mikroskopis langsung / pewarnaan Gram (`MIC_DIRECT_SMEAR`).
- Seksi biakan menyajikan status pertumbuhan (`MIC_GROWTH_STATUS`), hitung koloni (`MIC_COLONY_COUNT`), dan mikroorganisme terisolasi (`MIC_ISOLATED_ORGANISM`).
- Jika terdapat pertumbuhan kuman: Menyajikan tabel antibiogram 3 kolom: `[Nama Antibiotik]` | `[Nilai MIC]` | `[Interpretasi (S / I / R)]` beserta keterangan legenda (S=Sensitif, I=Intermediet, R=Resisten).
- Jika biakan steril (tidak ada pertumbuhan kuman): Menyembunyikan tabel antibiogram dan menampilkan kalimat informatif penjelas.
- Area verifikasi menampilkan label penanggung jawab "Dokter Spesialis Mikrobiologi Klinik (Sp.MK)".  
Notes: Mengimplementasikan LabResultMicroPdfRenderer di Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultMicroPdfRenderer.cs dengan struktur laporan mikrobiologi formal: judul 'HASIL PEMERIKSAAN MIKROBIOLOGI', patient info header 2-kolom, Seksi 1 Sediaan Mikroskopis Langsung (MIC_DIRECT_SMEAR), Seksi 2 Hasil Biakan & Identifikasi Mikroorganisme (Status Pertumbuhan, Hitung Koloni, Organisme Diisolasi), Seksi 3 Uji Kepekaan Antibiotik (AST) dengan deteksi otomatis kondisi steril (banner informatif tanpa tabel AST jika tidak ada pertumbuhan) vs tabel antibiogram 3 kolom (Nama Antibiotik, Nilai MIC, Sensitivitas/Interpretasi) dengan highlighting resisten dan legenda standar CLSI/EUCAST, catatan ekspertise Mikrobiologi Klinik, blok tanda tangan digital penanggung jawab Sp.MK, serta safe multi-page pagination with continuation headers. Pengujian unit komprehensif ditambahkan di Bilreg.Test/LabContext/LabResultFeature/LabResultMicroPdfRendererTest.cs (5/5 passed).

---

### P2-S05
Title: Integrasi Orchestrator LabResultPdfRenderer & Pengujian Unit Komprehensif  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui kelas orkestrator utama `LabResultPdfRenderer` agar mendelegasikan proses rendering ke `LabResultPkPdfRenderer`, `LabResultPaPdfRenderer`, atau `LabResultMicroPdfRenderer` berdasarkan nilai `view.ServiceType`, serta menulis pengujian unit lengkap di `Bilreg.Test`.  
Depends On: P2-S02, P2-S03, P2-S04  
Repository: `b09-bilreg-api`  
Completion Criteria:
- `LabResultPdfRenderer.Render` mengevaluasi `view.ServiceType`:
  - `LaboratoryServiceType.ClinicalPathology` $\rightarrow$ mendelegasikan ke `LabResultPkPdfRenderer`.
  - `LaboratoryServiceType.AnatomicalPathology` $\rightarrow$ mendelegasikan ke `LabResultPaPdfRenderer`.
  - `LaboratoryServiceType.Microbiology` $\rightarrow$ mendelegasikan ke `LabResultMicroPdfRenderer`.
  - Fallback ke `LabResultPkPdfRenderer` jika tipe layanan tidak ditentukan.
- Suite pengujian unit `LabResultPdfRendererTest.cs` diperkaya dengan test cases:
  - Render hasil PK dengan grouping disiplin dan sub-group.
  - Render hasil PK kolom parameter tanpa nama tes.
  - Render hasil PA naratif lengkap dengan diagnosis patologi.
  - Render hasil Mikro dengan pertumbuhan dan tabel antibiogram.
  - Render hasil Mikro steril tanpa tabel antibiogram.
- Seluruh pengujian pada `b09-bilreg-api` lulus dengan `dotnet test`.  
Notes: Memperbarui LabResultPdfRenderer sebagai orkestrator strategi yang mengevaluasi view.ServiceType dan mendelegasikan ke LabResultPkPdfRenderer (Patologi Klinik / fallback), LabResultPaPdfRenderer (Patologi Anatomi), atau LabResultMicroPdfRenderer (Mikrobiologi). Mempertahankan backward compatibility penuh untuk ILabResultPdfRenderer. Menambahkan pengujian unit komprehensif di Bilreg.Test/LabContext/LabResultFeature/LabResultPdfRendererTest.cs (16 unit tests, seluruhnya lulus verifikasi). File diubah: src/bilreg/Bilreg.Infrastructure/LabContext/LabResultFeature/LabResultPdfRenderer.cs dan src/bilreg/Bilreg.Test/LabContext/LabResultFeature/LabResultPdfRendererTest.cs.

---

## P3 - Frontend Dialog Preview Alignment

Implementation Status: IMPLEMENTED  
Review Status: GO  

### P3-S06
Title: Penyesuaian Judul Dialog Kontekstual pada LabResultPdfPreviewDialog.vue  
Implementation Status: IMPLEMENTED  
Review Status: GO  
Objective: Memperbarui komponen `LabResultPdfPreviewDialog.vue` di `c012_myhospital_web` agar dapat menerima atau mendeteksi tipe layanan order (`serviceType`) dan menyajikan judul dialog yang kontekstual (*Preview Lembar Hasil Patologi Klinik / Patologi Anatomi / Mikrobiologi*).  
Depends On: P2-S05  
Repository: `c012_myhospital_web`  
Completion Criteria:
- `LabResultPdfPreviewDialog.vue` menerima props opsional `serviceType` atau menghitung judul dinamis berbasis konteks order.
- Judul default menjadi kontekstual: "Preview Lembar Hasil Patologi Klinik", "Preview Lembar Hasil Patologi Anatomi", atau "Preview Lembar Hasil Mikrobiologi".
- Unit test `LabResultPdfPreviewDialog.spec.ts` diperbarui untuk memverifikasi variasi judul kontekstual.
- Verifikasi frontend lulus: `pnpm tc:app`, `pnpm test src/modules/Laboratory/__tests__/LabResultPdfPreviewDialog.spec.ts`, dan `pnpm lint`.  
Notes: Menambahkan prop opsional serviceType pada LabResultPdfPreviewDialog.vue dan computed effectiveTitle untuk diferensiasi kontekstual: Patologi Anatomi ('Preview Lembar Hasil Patologi Anatomi'), Mikrobiologi ('Preview Lembar Hasil Mikrobiologi'), dan default/PK ('Preview Lembar Hasil Patologi Klinik'), dengan penanganan override judul kustom. Menyelaraskan LabReleaseQueuePanel.vue untuk meneruskan order.serviceType. Unit test di LabResultPdfPreviewDialog.spec.ts diperkaya mencakup seluruh varian serviceType dan custom title (13/13 tests passed). Seluruh verifikasi c012_myhospital_web lulus: prettier, tc:app, targeted test, dan lint.

---

# 6. Change Log

- 2026-11-25: Inisiasi plan v1.0 diferensiasi cetakan PDF laboratorium untuk PK, PA, dan Mikro berdasarkan kesepakatan desain /grill-me.
