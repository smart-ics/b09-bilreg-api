---
Title: Rencana Implementasi Integrasi Order Laboratorium EMR ke Order Lab Bilreg
Code: LAB-EMR-ORDER-INTEGRATION
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-02
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Mewujudkan integrasi dan penyelarasan alur pembuatan permintaan pemeriksaan laboratorium (*laboratory examination order*) dari antarmuka Electronic Medical Record (EMR) Medical Chart ([orderLab.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/orderLab.js) dan [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js) pada `c001_emr2_frontend`) ke Laboratory Workflow Feature (LWF) pada backend Bilreg ([LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs) pada `b09-bilreg-api`), mengeliminasi jalur bypass lama langsung ke OWARE (`sendOrderLIS`), serta menegakkan Bilreg LWF sebagai sumber kebenaran operasional (*operational source of truth*) yang mengendalikan alur sampling (`LAB-COLLECT`), penagihan tindakan (`TRK-BILLING`), dan penerusan order ke analyzer/LIS via OWARE (`LabOwareFeature`).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- ISSUE: [LAB-EMR-ORDER-INTEGRATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-EMR-ORDER-INTEGRATION-ISSUE.md)
- FEASIBILITY-ASSESSMENT: [LAB-EMR-ORDER-INTEGRATION-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-EMR-ORDER-INTEGRATION-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- ARCHITECTURE: [LAB-EMR-ORDER-INTEGRATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-EMR-ORDER-INTEGRATION-ARCHITECTURE.md)
- FEATURE SPEC: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- CANONICAL OUTCOME: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- INTEGRATION SPEC: [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md) (§3 EMR Integration)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Rencana implementasi ini mencakup penyelesaian pekerjaan teknis terstruktur pada dua repositori utama:
1. **`c001_emr2_frontend`**:
   - Pemutakhiran [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js) untuk mengirimkan order lab ke endpoint Bilreg `POST /api/LabContext/LabOrderFeature/fromEmr` segera setelah rekam medis berhasil disimpan (`Chart/Add`).
   - Pemetaan payload `LabOrderCreateFromEmrCmd` menggunakan `ChartOrderId` (kunci primer `dbo.EMREC_ChartOrder`) sebagai `EmrOrderId` kanonikal.
   - Penanganan respons error HTTP 409 (`DUPLICATE_LAB_ORDER_WARNING`): menampilkan dialog konfirmasi SweetAlert interaktif ke DPJP dan retry otomatis dengan flag `AllowDuplicateConfirmation: true`.
   - Penanganan error respons `LAB_TEST_DEFINITION_NOT_FOUND` secara informatif di UI.
   - Penonaktifan pemanggilan legacy `sendOrderLIS` ketika order berhasil dikirim ke Bilreg.
   - Pemutakhiran [orderLab.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/orderLab.js) untuk query status real-time (`GET byEmrOrderId/{emrOrderId}`), men-disable tombol hapus jika spesimen telah diambil (`SampleCollected` ke atas), dan memanggil endpoint pembatalan Bilreg `POST /cancel` jika order dibatalkan sebelum pengambilan sampel.
2. **`b09-bilreg-api`**:
   - Verifikasi dan validasi kesiapan kontrak endpoint `POST /fromEmr`, `GET /byEmrOrderId/{emrOrderId}`, dan `POST /cancel`.
   - Skrip audit dan verifikasi kelengkapan master data `BILRG_LabTestDefinition` terhadap seluruh item tarif lab aktif di `TRK-TARIF`.
   - Pengujian integrasi unit dan end-to-end menyeluruh untuk skenario pemesanan, konfirmasi duplikasi 24 jam, pembatalan medis, dan transmisi otomatis ke OWARE.

---

# 3. Dependencies

## External Dependencies
- Service `b09-bilreg-api` dapat diakses dari browser frontend EMR melalui URL konfigurasi `config.base_api_bilreg` (port default development: `5189`).
- Middleware `a046_oware_ordermiddlewareapi` aktif untuk menerima push order dari Bilreg `LabOwareFeature`.

## Slice Dependencies Rule
- `Depends On` menyatakan prasyarat implementasi antar-slice.
- Ketergantungan mengacu secara eksplisit pada Slice ID.
- Pemenuhan ketergantungan tercapai saat slice prasyarat berstatus `IMPLEMENTED` dan output kode yang disyaratkan telah tersedia di repositori target.

---

# 4. Progress Summary

| Phase | Description | Repository Scope | Slices | Implementation Status | Review Status | Progress |
|---|---|---|---|---|---|---|
| **P1** | Bilreg Contract Readiness & Master Test Audit | `b09-bilreg-api` | P1-S01 s/d P1-S02 | NOT-STARTED | NOT-REVIEWED | 0/2 |
| **P2** | EMR Frontend Order Transmission & Duplicate UX | `c001_emr2_frontend` | P2-S03 s/d P2-S04 | NOT-STARTED | NOT-REVIEWED | 0/2 |
| **P3** | EMR Frontend Lifecycle & Cancellation Protection | `c001_emr2_frontend` | P3-S05 | NOT-STARTED | NOT-REVIEWED | 0/1 |
| **P4** | End-to-End Integration & LIS Alignment Verification | `b09-bilreg-api` | P4-S06 | NOT-STARTED | NOT-REVIEWED | 0/1 |

---

# 5. Phases

## P1 - Bilreg Contract Readiness & Master Test Audit (`b09-bilreg-api`)

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P1-S01
- **Title**: Verifikasi Kontrak Handler `fromEmr`, Idempotensi, dan Suite Pengujian Bilreg
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Memastikan kesiapan dan validitas fungsional use case `LabOrderCreateFromEmrCmd`, controller `LabOrderController`, respons JSend HTTP 409 pada exception `DuplicateLabOrderWarningException`, serta penanganan `AllowDuplicateConfirmation`.
- **Depends On**: None
- **Repository**: `b09-bilreg-api`
- **Completion Criteria**:
  - `LabOrderCreateFromEmrHandler` terbukti mengeksekusi validasi `ADM-REG` (aktif), `ORG-PPA` (dokter perujuk), deteksi duplikasi 24 jam via `_labOrderDal`, resolusi tarif ke definisi uji via `_resolutionService`, dan penerbitan `OrderNo`.
  - Terverifikasi bahwa ketika terjadi duplikasi dan `AllowDuplicateConfirmation: false`, API mengembalikan HTTP 409 Conflict dengan payload JSend terstandar yang memuat `data.duplicateTestNames` dan `data.previousOrderIds`.
  - Terverifikasi bahwa ketika `AllowDuplicateConfirmation: true`, order berhasil tersimpan dan mengembalikan respons `LabOrderCreateFromEmrResponse`.
  - Seluruh unit test pada `Bilreg.Test/LabContext/LabOrderFeature/LabOrderCreateFromEmrHandlerTest.cs` berstatus passing.
- **Notes**:
  - Target files:
    - `src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs`
    - `src/bilreg/Bilreg.Api/Controllers/LabContext/LabOrderFeature/LabOrderController.cs`
    - `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCreateFromEmrHandlerTest.cs`
  - Implementation Notes:
    - Menambahkan mekanisme idempotensi pada `LabOrderCreateFromEmrHandler`: memeriksa order eksisting via `_labOrderRepo.LoadByEmrOrderId(request.EmrOrderId)` sebelum memproses pemesanan baru sehingga tidak terbentuk order ganda saat retry/timeout.
    - Menambahkan unit test `Handle_EmrOrderIdAlreadyExists_ReturnsExistingOrderWithoutCreatingNew` untuk memverifikasi idempotensi.
    - Menambahkan unit test `Handle_TarifNotMapped_ThrowsLabTestDefinitionNotFound` untuk memverifikasi propagasi exception saat tarif belum terpetakan ke `LabTestDefinition`.
    - Memverifikasi endpoint `LabOrderController` (`POST /fromEmr`, `POST /from-emr`, `GET /byEmrOrderId/{emrOrderId}`, `POST /cancel`).
    - Memverifikasi penanganan `DuplicateLabOrderWarningException` menghasilkan HTTP 409 Conflict dengan JSend payload (`duplicateTestNames`, `previousOrderIds`) via `ErrorHandlerMiddleware` dan `DuplicateLabOrderErrorHandlerTest`.
    - Menjalankan seluruh test pada `Bilreg.Test` untuk `LabOrderFeature` dan `LabContext` (447 tests passing, 0 failed).
  - Changed Files:
    - `b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs`
    - `b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderCreateFromEmrHandlerTest.cs`

---

### P1-S02
- **Title**: Audit & Sinkronisasi Master `LabTestDefinition` Terhadap Master Tarif Lab Aktif
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Menyusun query dan skrip verifikasi untuk mengaudit kelengkapan data master `BILRG_LabTestDefinition` di database Bilreg terhadap seluruh tarif aktif yang digunakan pada formulir pencarian EMR (`TRK-TARIF`), mencegah timbulnya exception `LAB_TEST_DEFINITION_NOT_FOUND` saat klinisi memesan tes di EMR.
- **Depends On**: P1-S01
- **Repository**: `b09-bilreg-api`
- **Completion Criteria**:
  - Tersedia skrip SQL / audit tool di `Bilreg.SqlDb` atau dokumen verifikasi yang membandingkan tarif laboratorium aktif dengan tabel `BILRG_LabTestDefinition`.
  - Seluruh tarif tes laboratorium rutin yang ada pada katalog EMR terpetakan ke entitas `LabTestDefinitionModel` yang valid (lengkap dengan `VacutainerType`, `SpecimenType`, dan relasi komponen uji aktif).
- **Notes**:
  - Target files:
    - `src/bilreg/Bilreg.SqlDb/LabContext/LabTestDefinitionFeature/BILRG_LabTestDefinition_AuditMasterTarif.sql`
    - `docs/contexts/lab/LAB_MASTER_TEST_ALIGNMENT.md`
  - Implementation Notes:
    - Menyusun skrip SQL audit komprehensif `src/bilreg/Bilreg.SqlDb/LabContext/LabTestDefinitionFeature/BILRG_LabTestDefinition_AuditMasterTarif.sql` untuk mengaudit dan memverifikasi keselarasan katalog tarif laboratorium aktif (`ta_tarif` / `TRK-TARIF`) terhadap master data `BILRG_LabTestDefinition`, komponen uji `BILRG_LabTestComponent`, dan master analit vendor `BILRG_LabComponentMaster`.
    - Mengintegrasikan deteksi potensi exception runtime domain Bilreg (`LAB_TEST_DEFINITION_NOT_FOUND`, `LAB_TEST_DEFINITION_INACTIVE`, `LAB_TEST_DEFINITION_EMPTY_COMPONENTS`, `LAB_COMPONENT_NOT_FOUND`, `LAB_COMPONENT_INACTIVE`, `LAB_DUPLICATE_SEQUENCE`, `LAB_DUPLICATE_COMPONENT`).
    - Menyediakan scorecard kesehatan pemetaan, deteksi anomali, serta checklist kelengkapan 9 panel klinis rutin rumah sakit (Darah Rutin/CBC, Profil Lipid, Fungsi Hati, Fungsi Ginjal, Glukosa, Urinalisis, Elektrolit, Imunoserologi, Hemostasis).
    - Memperbarui dokumen `docs/contexts/lab/LAB_MASTER_TEST_ALIGNMENT.md` (§7) dengan spesifikasi teknis skrip audit, ringkasan pemetaan panel klinis rutin standar, diagram alur penanganan tarif unmapped, SOP tata kelola registrasi tarif lab baru, dan template SQL remediasi.
  - Changed Files:
    - `b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabTestDefinitionFeature/BILRG_LabTestDefinition_AuditMasterTarif.sql`
    - `b09-bilreg-api/docs/contexts/lab/LAB_MASTER_TEST_ALIGNMENT.md`

---

## P2 - EMR Frontend Order Transmission & Duplicate UX (`c001_emr2_frontend`)

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P2-S03
- **Title**: Integrasi Pengiriman Order Lab EMR ke Bilreg API (`chartNote.js`)
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Mengimplementasikan fungsi transmisi `sendOrderLabToBilreg(result)` pada `chartNote.js` untuk mengirimkan data order laboratorium dari rekam medis EMR ke endpoint Bilreg `POST /api/LabContext/LabOrderFeature/fromEmr` segera setelah penyimpanan chart note sukses (`POST Chart/Add`), serta menonaktifkan pemanggilan bypass legacy `sendOrderLIS`.
- **Depends On**: P1-S01
- **Repository**: `c001_emr2_frontend`
- **Completion Criteria**:
  - Fungsi `sendOrderLabToBilreg(result)` membaca daftar order lab (`OrderTypeID == "LB"`) dari rekam medis yang baru disimpan.
  - Mengambil nilai `ChartOrderId` dari respons `result.ListOrder` sebagai `emrOrderId`.
  - Memetakan seluruh atribut payload `LabOrderCreateFromEmrCmd`:
    - `userId`: ID dokter/pengguna login (`user.UserrID`)
    - `emrOrderId`: `ChartOrderId`
    - `regId`: `activeRegID`
    - `patientId`: `pasienIDCurrent`
    - `patientName`: nama pasien aktif
    - `birthDateYmd`: format `YYYY-MM-DD`
    - `gender`: `L` atau `P`
    - `items`: array `[{ tarifId: item.TariffID, tarifName: item.TariffName }]`
    - `dokterPerujukId`: `chart.DokterID`
    - `dokterPerujukName`: `chart.DokterName`
    - `layananPerujukId`: `activeLynID`
    - `layananPerujukName`: nama layanan aktif
    - `clinicalNote`: deskripsi order lab dokter
    - `urgency`: 0 (Rutin) atau 1 (Cito)
    - `allowDuplicateConfirmation`: false
  - Mengeksekusi AJAX POST ke `config.base_api_bilreg + 'LabContext/LabOrderFeature/fromEmr'`.
  - Jika pengiriman sukses: menampilkan toastr sukses ("Order Lab terkirim ke Laboratorium - No Order: {orderNo}") dan **tidak menjalankan** `sendOrderLIS(result.ChartID)`.
- **Notes**:
  - Target files:
    - `assets/js/MedicalChart/chartNote.js`
    - `config/config.js`
  - Implementation Notes:
    - Menambahkan toggle `order_lab_to_bilreg: false` pada section `medicalChart` di `config/config.js` sebagai switch dual-mode bridging, serta memverifikasi ketersediaan konfigurasi `base_api_bilreg`.
    - Mengimplementasikan fungsi async `sendOrderLabToBilreg(result)` pada `assets/js/MedicalChart/chartNote.js` yang mengekstrak seluruh order lab (`OrderTypeID == "LB"`), mengambil `ChartOrderId` sebagai `emrOrderId`, dan menyusun DTO `LabOrderCreateFromEmrCmd`.
    - Melakukan mapping lengkap atribut demografi pasien (`patientName`, `birthDateYmd` berformat `YYYY-MM-DD`, `gender` ternormalisasi `L`/`P`), informasi dokter perujuk (`dokterPerujukId`, `dokterPerujukName`), informasi layanan perujuk (`layananPerujukId`, `layananPerujukName`), item pemeriksaan (`tarifId`, `tarifName`), catatan klinis, dan tingkat urgensi (0 Rutin / 1 Cito).
    - Menangani pengiriman AJAX POST ke `${config.base_api_bilreg}LabContext/LabOrderFeature/fromEmr` dengan Authorization header (bila token cookie tersedia) dan notifikasi toastr sukses memuat `orderNo`.
    - Memperbarui blok pasca-simpan chart pada `saveMedicalChart` agar saat `config.medicalChart.order_lab_to_bilreg` aktif, alur memanggil `sendOrderLabToBilreg(result)` dan menonaktifkan pemanggilan legacy bypass `sendOrderLIS`.
    - Memverifikasi sintaks Javascript pada kedua file target (`node -c`) berhasil tanpa error syntax.
  - Changed Files:
    - `c001_emr2_frontend/assets/js/MedicalChart/chartNote.js`
    - `c001_emr2_frontend/config/config.js`

---

### P2-S04
- **Title**: Penanganan Interaktif Peringatan Duplikasi 24 Jam & Error Validasi UI EMR (`chartNote.js`)
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Mengimplementasikan penanganan error HTTP 409 (`DUPLICATE_LAB_ORDER_WARNING`) dan error definisi tes pada `chartNote.js` dengan modal konfirmasi SweetAlert interaktif untuk dokter pengorder.
- **Depends On**: P2-S03
- **Repository**: `c001_emr2_frontend`
- **Completion Criteria**:
  - Pada blok `error` pemanggilan AJAX ke Bilreg:
    - Jika status HTTP 409 atau `err.code === "DUPLICATE_LAB_ORDER_WARNING"`:
      - Membaca daftar nama tes (`duplicateTestNames`) dan nomor order sebelumnya (`previousOrderIds`).
      - Memunculkan modal dialog SweetAlert: *"Peringatan: Pemeriksaan [Nama Tes] telah dipesan dalam 24 jam terakhir pada Order [No Order]. Apakah Anda yakin ingin tetap membuat order ulang ini?"*.
      - Jika dokter memilih *"Ya / Lanjutkan"*: frontend secara otomatis mengirim ulang request AJAX ke Bilreg dengan parameter `allowDuplicateConfirmation: true`.
      - Jika dokter memilih *"Batal"*: order tidak diteruskan ke Bilreg dan menampilkan toastr peringatan.
    - Jika respons mengembalikan error `LAB_TEST_DEFINITION_NOT_FOUND`:
      - Menampilkan modal alert ramah klinisi: *"Pemeriksaan [Nama Tarif] belum dikonfigurasi definisi tes teknisnya di laboratorium Bilreg. Silakan hubungi petugas Laboratorium/IT."*.
- **Notes**:
  - Target files:
    - `assets/js/MedicalChart/chartNote.js`
  - Implementation Notes:
    - Mengimplementasikan penanganan respons error HTTP 409 (`DUPLICATE_LAB_ORDER_WARNING`) pada fungsi `sendOrderLabToBilreg` di `chartNote.js`.
    - Mengekstrak informasi `duplicateTestNames` dan `previousOrderIds` dari payload respons JSend Bilreg API.
    - Menampilkan dialog konfirmasi interaktif SweetAlert (`Swal.fire`) dengan opsi "Ya, Tetap Buat Order" dan "Batal".
    - Mengimplementasikan retry otomatis via AJAX dengan payload parameter `allowDuplicateConfirmation: true` ketika dokter memilih override, disertai notifikasi toastr sukses memuat `orderNo`.
    - Menangani pembatalan konfirmasi oleh dokter dengan notifikasi toastr pembatalan.
    - Menangani respons HTTP 400 untuk exception `LAB_TEST_DEFINITION_NOT_FOUND` dengan menampilkan modal error informatif (`Swal.fire`) yang menyertakan nama pemeriksaan/tarif terkait agar klinisi dapat berkoordinasi dengan laboratorium/IT.
    - Mempertahankan toastr error fallback untuk jenis error lainnya.
    - Memverifikasi validitas sintaks javascript `chartNote.js` menggunakan `node -c`.
  - Changed Files:
    - `c001_emr2_frontend/assets/js/MedicalChart/chartNote.js`

---

## P3 - EMR Frontend Lifecycle & Cancellation Protection (`c001_emr2_frontend`)

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P3-S05
- **Title**: Sinkronisasi Status Real-Time & Proteksi Pembatalan Order Lab (`orderLab.js`)
- **Implementation Status**: IMPLEMENTED
- **Review Status**: GO
- **Objective**: Menyelaraskan modul `orderLab.js` dengan status siklus hidup order Bilreg LWF: mengecek status order real-time, mengunci tombol hapus jika spesimen sudah diambil, dan memanggil endpoint pembatalan Bilreg saat order dibatalkan sebelum pengambilan sampel.
- **Depends On**: P2-S03
- **Repository**: `c001_emr2_frontend`
- **Completion Criteria**:
  - Saat card order lab ditampilkan atau dibuka pada modal EMR (`setOrderLabFromChart`):
    - Jika order memiliki `ChartOrderId`, memanggil query `GET config.base_api_bilreg + 'LabContext/LabOrderFeature/byEmrOrderId/' + chartOrderId`.
    - Jika status Bilreg `>= 3` (`SampleCollected`, `Charged`, `Done`):
      - Tombol `.deleteOrderLab` disembunyikan/di-disable.
      - Menampilkan badge/teks status: *"Spesimen telah diambil di Lab"*.
      - Mencegah aksi hapus dengan peringatan bahwa sampel telah diambil.
  - Saat dokter menghapus order yang masih berstatus `1` (`Ordered`) atau `2` (`Deferred`):
    - Memunculkan dialog SweetAlert konfirmasi pembatalan dan input alasan pembatalan medis dokter.
    - Mengirimkan request pembatalan ke endpoint Bilreg: `POST config.base_api_bilreg + 'LabContext/LabOrderFeature/cancel'` dengan payload: `{ orderId: orderId, cancellationReason: reason, userId: user.UserrID }`.
    - Setelah Bilreg mengonfirmasi pembatalan (status 200 OK): menghapus card order dari UI EMR dan menampilkan toastr sukses.
- **Notes**:
  - Target files:
    - `assets/js/MedicalChart/orderLab.js`
  - Implementation Notes:
    - Menambahkan helper integrasi Bilreg (`isBilregLabOrderActive`, `getBilregApiBaseUrl`, `getBilregAuthToken`, `getBilregStatusName`, `getBilregStatusBadgeHtml`, `applyBilregCardStatus`, `getBilregOrderStatus`, `syncBilregOrderStatus`) pada `assets/js/MedicalChart/orderLab.js`.
    - Mengintegrasikan sinkronisasi status real-time pada `setOrderLabFromChart(data, orderId)` saat order memiliki `ChartOrderId` via query AJAX GET `GET ${config.base_api_bilreg}LabContext/LabOrderFeature/byEmrOrderId/${chartOrderId}` dengan Authorization bearer token Bilreg (cookie `myAuthBilreg`).
    - Menyimpan atribut `bilregOrderId`, `orderStatus`, dan `bilregOrderNo` pada objek order `listOrderLaborat` serta atribut dataset DOM card.
    - Mengunci dan men-disable tombol `.deleteOrderLab` serta menampilkan badge status visual informatif ("Spesimen telah diambil di Lab" / "Sedang diproses Lab") saat status Bilreg `>= 3` (`SampleCollected`, `Charged`, `Done`).
    - Menambahkan fallback protection pada event handler `.deleteOrderLab` dan modal `shown.bs.modal` sehingga jika order telah disampling (`status >= 3`), aksi pembatalan langsung dicegah dengan menampilkan notifikasi peringatan `Swal.fire`.
    - Mengimplementasikan alur pembatalan interaktif untuk order berstatus `1` (`Ordered`) atau `2` (`Deferred`): memunculkan dialog konfirmasi SweetAlert dengan input textarea alasan pembatalan medis dokter dan mengirimkan request pembatalan ke endpoint Bilreg `POST ${config.base_api_bilreg}LabContext/LabOrderFeature/cancel` payload `{ orderId, cancellationReason, reason, userId }`.
    - Setelah Bilreg mengonfirmasi pembatalan (HTTP 200 OK), card dihapus dari UI EMR, item dikeluarkan dari `listOrderLaborat`, counter di-update, dan toastr sukses ditampilkan.
    - Mempertahankan alur delete lokal EMR jika order belum terkirim ke Bilreg / belum memiliki `ChartOrderId`.
    - Memverifikasi validitas sintaks javascript `orderLab.js` menggunakan `node -c` (valid tanpa error) dan memastikan format line ending CRLF.
  - Changed Files:
    - `c001_emr2_frontend/assets/js/MedicalChart/orderLab.js`

---

## P4 - End-to-End Integration & LIS Alignment Verification (`b09-bilreg-api`)

Implementation Status: NOT-STARTED
Review Status: NOT-REVIEWED

### P4-S06
- **Title**: Pengujian Verifikasi Terpadu Alur EMR → Bilreg LWF → OWARE → Worklist Lab
- **Implementation Status**: NOT-STARTED
- **Review Status**: NOT-REVIEWED
- **Objective**: Menjalankan pengujian integrasi end-to-end menyeluruh untuk memastikan rantai proses berjalan tanpa celah sesuai seluruh kriteria penerimaan arsitektur target.
- **Depends On**: P1-S02, P2-S04, P3-S05
- **Repository**: `b09-bilreg-api`
- **Completion Criteria**:
  - Skenario 1 (Happy Path Order Baru):
    - Order lab dibuat di EMR -> terkirim ke Bilreg -> record terbentuk di `BILRG_LabOrder` dengan `EmrOrderId` terisi dan `OrderNo` sah -> order muncul pada worklist laboratorium (`GET /worklist`) -> tidak ada pemanggilan `sendOrderLIS` langsung dari EMR.
  - Skenario 2 (Peringatan Duplikasi 24 Jam):
    - Order dengan tes yang sama dibuat kedua kalinya untuk pasien yang sama -> Bilreg merespons HTTP 409 -> UI EMR menampilkan prompt SweetAlert -> dokter memilih "Lanjutkan" -> order sukses dibuat dengan `AllowDuplicateConfirmation: true`.
  - Skenario 3 (Proteksi Pembatalan Berdasarkan Status Sampling):
    - Order yang baru dibuat dapat dibatalkan dari EMR -> status di Bilreg berubah menjadi `Cancelled`.
    - Order yang spesimennya telah disampling di lab (`SampleCollected`) terkunci di EMR dan tidak dapat dihapus dokter.
  - Skenario 4 (Penerusan ke OWARE):
    - Order yang telah di-charge di Bilreg masuk ke antrean `BILRG_LabOwareOutboundQueue` dan diteruskan ke endpoint OWARE `POST /api/order/lab/bilreg` menggunakan `OrderNo` resmi Bilreg.
- **Notes**:
  - Target files / suite:
    - `src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/`
    - Manual / automated integration test script

---

# 6. Change Log

- **2026-11-02 (v1.0)**: Inisiasi Rencana Implementasi integrasi order laboratorium dari EMR (`orderLab.js` & `chartNote.js`) ke Bilreg (`LabOrderFeature`), mencakup 4 fase dan 6 slice kerja independen dengan target repositori terisolasi. Status perencanaan disetujui (`Execution Approval: APPROVED`).
