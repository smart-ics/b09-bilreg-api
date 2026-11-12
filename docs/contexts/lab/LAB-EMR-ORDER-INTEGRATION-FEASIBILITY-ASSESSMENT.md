---
Title: Feasibility Assessment Integrasi Order Laboratorium EMR ke Order Lab Bilreg
Code: LAB-EMR-ORDER-INTEGRATION
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-11-02
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap integrasi dan penyelarasan alur pembuatan permintaan pemeriksaan laboratorium (*laboratory examination order*) dari modul Electronic Medical Record (EMR) Medical Chart ([orderLab.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/orderLab.js) dan [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js)) ke sistem Order Lab Bilreg ([LabOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs) pada `b09-bilreg-api`).

Referenced artifacts:
- ISSUE: [LAB-EMR-ORDER-INTEGRATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-EMR-ORDER-INTEGRATION-ISSUE.md)
- DOMAIN Bilreg: [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- OUTCOME MyHosWeb: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- FEATURE Bilreg: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- INTEGRATION SPEC: [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md) (§3 EMR Integration)
- WORKFLOW SPEC: [lab-workflow.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-workflow.md)

## Objective

1. Menganalisis kondisi eksisting alur pemesanan laboratorium pada frontend EMR ([orderLab.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/orderLab.js), [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js)) dan backend EMR (`a005_emr2backend`).
2. Menganalisis kesiapan endpoint dan domain logic pada backend Bilreg (`b09-bilreg-api`), khususnya handler `LabOrderCreateFromEmrCmd` dan aturan validasi domain terkait (registrasi aktif, resolusi definisi tes, verifikasi dokter pengorder, dan pencegahan duplikasi tes 24 jam).
3. Mengidentifikasi kesenjangan (*gaps*), pertanyaan terbuka (*open questions*), asumsi, dan risiko teknis dalam menghubungkan kedua sistem secara andal.
4. Mengevaluasi dampak terhadap integrasi downstream ke middleware LIS/analyzer OWARE (`a046_oware_ordermiddlewareapi`) guna mengeliminasi alur bypass langsung dari EMR.
5. Merumuskan keputusan penutupan celah teknis (*gap closure decisions*) yang disepakati melalui sesi penajaman (*grill-me*) sebelum diserahkan ke peran **Architect** (`ica-architect`).

---

# 2. Current State

## Existing Behavior

### 1. Modul Order Lab di Frontend EMR (`c001_emr2_frontend`)
- **Antarmuka & Seleksi Tes**:
  - Diimplementasikan pada [orderLab.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/orderLab.js) melalui modal pencarian tarif laboratorium (`#modalOrderLab`).
  - Item tes yang dipilih disimpan dalam objek `orderLaborat.ListItem` dengan atribut: `TariffID`, `TariffName`, `Keterangan`, `GrupJenisPemeriksaanId`, `GrupJenisPemeriksaanName`, dan `Qty`.
  - Order dikelompokkan ke dalam array global `listOrderLaborat` dengan atribut: `OrderID` (integer counter lokal di frontend), `OrderTypeID = "LB"`, `Description`, `ReffId`, `ImplementationID`, dan `TglRencanaPeriksa`.
- **Penyimpanan Medical Chart**:
  - Pada [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js) (baris 2416–2426 & 2823), `listOrderLaborat` digabungkan ke properti `ListOrder` di payload utama chart.
  - Data dikirimkan ke backend EMR melalui endpoint `Chart/Add` atau `Chart/Update/{chartId}`.
- **Bypass Langsung ke OWARE**:
  - Pada [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js) (baris 3141–3150), setelah chart berhasil disimpan, frontend memeriksa flag:
    ```javascript
    const isOrderLab = data.ListOrder.some((x) => x.OrderTypeID == "LB");
    if (config.medicalChart.oware_order_lis_without_tu && isOrderLab) {
      sendOrderLIS(result.ChartID);
    }
    ```
  - Fungsi `sendOrderLIS` memanggil endpoint OWARE middleware secara langsung: `POST config.base_api_oware + 'Order/lab/chart'` dengan payload `{ chartId: chartId }`.
  - **Temuan Kritis**: Jalur ini mem-bypass Bilreg sepenuhnya. Tidak ada pemanggilan ke API Order Lab Bilreg, sehingga order tidak masuk ke basis data `BILRG_LabOrder`, tidak tercatat di worklist laboratorium, tidak memicu pengambilan sampel (`SampleCollection`), dan tidak memicu pembebanan billing (`TRK-BILLING`).

### 2. Backend EMR (`a005_emr2backend`)
- Menyimpan entitas order ke tabel database rekam medis `dbo.EMREC_ChartOrder` (dengan kunci primer `ChartOrderID VARCHAR(17)`) dan `dbo.EMREC_ChartOrderItem` (dengan rincian `TariffID`).
- Metode `ChartBL.Add` mengembalikan `ChartResponse` yang memuat `ListOrder` lengkap dengan `ChartOrderId` resmi untuk setiap entitas order lab.
- Menyediakan query [`ListOrderLabByChartIdQuery.cs`](file:///D:/Project.Aktif/_emr/a005_emr2backend/Emr2.Application/ChartContext/UseCases/Queries/ListOrderLabByChartIdQuery.cs) yang mengembalikan rincian order lab (`ChartId`, `RegId`, `PasienId`, `PasienName`, `DokterOrderId`, `DokterOrderName`, `LayananId`, `LayananName`, `ListTariff`).

### 3. Backend Order Lab Bilreg (`b09-bilreg-api`)
- **Kontrak Integrasi EMR**:
  - Sesuai spesifikasi [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md) (§3), Bilreg telah menyediakan endpoint resmi:
    - `POST /api/LabContext/LabOrderFeature/fromEmr` (alias `from-emr`)
    - `GET /api/LabContext/LabOrderFeature/byEmrOrderId/{emrOrderId}`
    - `POST /api/LabContext/LabOrderFeature/cancel`
- **Domain Logic & Invariant Enforcement**:
  - Diimplementasikan pada [`LabOrderCreateFromEmrCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs) dan aggregate method `LabOrderModel.CreateFromEmr(...)`.
  - **Validasi Kunjungan Aktif (EX-01)**: Memverifikasi `RegId` ke `_regRepo` (`ADM-REG`). Jika kunjungan tidak ditemukan atau sudah tutup/discharge (`!reg.IsAktif`), order ditolak.
  - **Validasi Dokter Perujuk**: Jika `DokterPerujukId` diisi, divalidasi ke `_ppaRepo` (`ORG-PPA`).
  - **Resolusi Definisi Tes & Spesimen**: Menggunakan [`LabTestResolutionService.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs) untuk memetakan setiap `TarifId` ke `LabTestDefinitionModel` yang aktif, menentukan jenis tabung (*vacutainer*), jenis spesimen, dan komponen pemeriksaan. Jika tarif belum dipetakan, sistem melempar exception `LAB_TEST_DEFINITION_NOT_FOUND`.
  - **Deteksi Duplikasi Tes 24 Jam (EX-05)**: Memeriksa tes aktif pasien dalam 24 jam terakhir. Jika terdeteksi tes yang sama dan `AllowDuplicateConfirmation` bernilai `false`, sistem melempar exception `DuplicateLabOrderWarningException` (HTTP 409).
  - **Persistensi Kanonikal**: Menyimpan entitas `LabOrderModel` ke tabel `BILRG_LabOrder` dan rincian item ke `BILRG_LabOrderItem` serta `BILRG_LabOrderItemComponent`, menerbitkan `OrderNo` unik, dan mengembalikan `LabOrderCreateFromEmrResponse` (`EmrOrderId`, `LabOrderStatus`, `OrderNo`, `OrderId`).

### 4. Middleware LIS/Analyzer (`a046_oware_ordermiddlewareapi`)
- Telah disiapkan endpoint penerima dari Bilreg `POST /api/order/lab/bilreg` melalui `CreateOrderLabFromBilregCommand` berdasarkan spesifikasi `LAB-OWARE-ORDER-INTEGRATION`.
- Bilreg LWF dirancang sebagai satu-satunya *operational source of truth* yang meneruskan order ke OWARE setelah status order mencapai `Charged` / siap diproses.

## Existing Constraints

1. **Kedaulatan Operasional Bilreg LWF**: Sesuai prinsip domain pada [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md), alur kerja teknis laboratorium, persiapan pengambilan spesimen (`LAB-COLLECT`), pembebanan billing (`TRK-BILLING`), dan hasil tes (`LAB-RESULT`) sepenuhnya berada di bawah otoritas Bilreg LWF. EMR hanya bertindak sebagai inisiator permintaan (*clinical ordering initiator*).
2. **Keterikatan Episode Kunjungan Valid**: Setiap order laboratorium wajib terikat pada registrasi kunjungan aktif (`RegId`).
3. **Immutability Definisi Tes**: Order yang berhasil dibuat di Bilreg menyimpan snapshot definitif dari tarif, tes, tabung vacutainer, dan komponen hasil.
4. **Isolasi Database Antar-Service**: EMR dan Bilreg merupakan bounded context terpisah dengan basis data masing-masing. Komunikasi antar-keduanya harus berlangsung melalui protokol resmi (REST API atau messaging terstandar), bukan manipulasi langsung lintas database schema.

---

# 3. Gap Analysis

| ID | Severity | Status | Gap Description |
|---|---|---|---|
| **GAP-001** | CRITICAL | CLOSED | **Ketiadaan Transmisi Order dari EMR ke Bilreg**: Frontend EMR saat ini memanggil OWARE secara langsung (`sendOrderLIS` via `POST /api/order/lab/chart`) dan tidak pernah memanggil endpoint Bilreg `POST /api/LabContext/LabOrderFeature/fromEmr`. |
| **GAP-002** | CRITICAL | CLOSED | **Ketidaksesuaian Kontrak Payload Antara EMR dan `LabOrderCreateFromEmrCmd`**: Struktur data di `orderLab.js` (`orderLaborat`) belum menyusun DTO yang sesuai dengan `LabOrderCreateFromEmrCmd` di Bilreg (terutama kebutuhan `EmrOrderId` unik persisten, format tanggal lahir `BirthDateYmd`, jenis kelamin `Gender`, pemetaan `Items: [{ TarifId, TarifName }]`, serta tingkat urgensi `Urgency`). |
| **GAP-003** | MAJOR | CLOSED | **Kebutuhan Penetapan `EmrOrderId` Unik Antar-Sistem**: Pada frontend EMR, `OrderID` hanya berupa integer urutan lokal (1, 2, ...). Jika sebuah Medical Chart memuat lebih dari satu order lab, identifier `ChartID` saja tidak cukup untuk menjadi kunci korelasi yang unik. |
| **GAP-004** | CRITICAL | CLOSED | **Potensi Kegagalan Resolusi Tes (`LAB_TEST_DEFINITION_NOT_FOUND`)**: Handler Bilreg mewajibkan setiap `TarifId` memiliki definisi uji aktif (`LabTestDefinitionModel`). Jika dokter di EMR memilih tarif laboratorium yang belum dikonfigurasi di tabel `BILRG_LabTestDefinition`, order akan ditolak dengan error `LAB_TEST_DEFINITION_NOT_FOUND`. |
| **GAP-005** | MAJOR | CLOSED | **Ketiadaan Penanganan Interaktif Duplikasi Tes 24 Jam (`EX-05`) di EMR**: Bilreg melempar exception `DuplicateLabOrderWarningException` (HTTP 409) saat mendeteksi parameter uji yang sama dalam 24 jam terakhir. Frontend EMR (`orderLab.js`) belum memiliki mekanisme penangkap error ini untuk menampilkan konfirmasi ke dokter dan mengirim ulang dengan flag `AllowDuplicateConfirmation = true`. |
| **GAP-006** | MAJOR | CLOSED | **Bypass OWARE Eksisting Harus Dinonaktifkan**: Pemanggilan langsung dari EMR ke OWARE (`sendOrderLIS`) mengakibatkan transmisi ke LIS tidak sinkron dengan nomor order Bilreg (`OrderNo`), dan berisiko menimbulkan duplikasi order jika Bilreg juga mengirimkan order yang sama ke OWARE. |
| **GAP-007** | MAJOR | CLOSED | **Ketidaksinkronan Alur Pembatalan & Perubahan Order**: `orderLab.js` memiliki fitur hapus order (`deleteOrderLab`), namun hanya memeriksa status implementasi lokal (`EMREC_ChartOrderImple`). EMR belum memanggil endpoint status Bilreg (`GET /api/LabContext/LabOrderFeature/byEmrOrderId/{emrOrderId}`) atau endpoint pembatalan Bilreg untuk memastikan order yang spesimennya sudah diambil tidak dapat dihapus sepihak oleh dokter. |
| **GAP-008** | MINOR | CLOSED | **Kesesuaian Master Dokter Pengorder (`DokterPerujukId`)**: ID dokter yang dikirim dari EMR (`chart.DokterID` / `user.UserrID`) harus dipastikan cocok dan terdaftar aktif pada master tenaga medis (`ORG-PPA`) di Bilreg agar tidak ditolak validasi. |

---

# 4. Open Questions

| ID | Status | Question | Impact |
|---|---|---|---|
| **OQ-001** | CLOSED | Di layer manakah integrasi transmisi order lab ke Bilreg sebaiknya dieksekusi: langsung dari Frontend EMR (`c001_emr2_frontend` via AJAX/Fetch ke Bilreg API) atau dari Backend EMR (`a005_emr2backend` saat menyimpan `Chart/Add`)? | Menentukan mekanisme transmisi, penanganan interaktif konfirmasi duplikasi 24 jam, dan arsitektur komunikasi antar-service. |
| **OQ-002** | CLOSED | Apa format identitas kanonikal untuk `EmrOrderId`? Apakah menggunakan `ChartOrderID` (PK tabel `EMREC_ChartOrder` di EMR backend) atau `${ChartID}-${OrderID}`? | Menentukan kestabilan korelasi order dua arah antara EMR dan Bilreg, terutama saat dilakukan pelacakan status atau pembatalan. |
| **OQ-003** | CLOSED | Bagaimana strategi penanganan jika dokter di EMR memilih tarif lab yang belum memiliki pemetaan `LabTestDefinition` di Bilreg? | Menentukan kesiapan data dan penanganan kesalahan yang ramah klinisi di UI EMR. |
| **OQ-004** | CLOSED | Bagaimana alur UX di modal EMR ketika Bilreg mengembalikan warning duplikasi tes 24 jam (`DuplicateLabOrderWarningException`)? | Memberikan kontrol medis langsung kepada dokter untuk mengonfirmasi justifikasi pemesanan ulang tes diagnostik. |
| **OQ-005** | CLOSED | Bagaimana mekanisme penonaktifan alur bypass OWARE lama (`sendOrderLIS`) tanpa mengganggu operasional rumah sakit yang sedang berjalan? | Menghindari duplikasi order ke mesin analyzer laboratorium selama masa transisi. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| **ASM-001** | Master tarif tindakan laboratorium pada dropdown seleksi di EMR (`appLab.$root.$refs.orderlab.listTarif`) menggunakan kode/ID tarif (`TariffID`) yang sama persis dengan master tarif (`TRK-TARIF`) di Bilreg. |
| **ASM-002** | Episode kunjungan pasien yang sedang dibuka pada EMR selalu memiliki nomor registrasi (`RegId`) yang sah dan berstatus aktif (`IsAktif = true`) pada subsistem admisi Bilreg (`ADM-REG`). |
| **ASM-003** | Pengguna dokter yang menginput order di EMR (`DokterID` / `UserrID`) memiliki ID yang terdaftar dan valid di master PPA Bilreg (`ORG-PPA`). |
| **ASM-004** | Setelah integrasi ini aktif, tanggung jawab pengiriman order ke analyzer/LIS melalui middleware OWARE dialihkan sepenuhnya ke alur outbound Bilreg (`LabOwareFeature`), sehingga EMR tidak lagi mengirim order langsung ke OWARE. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| **RISK-001** | Transmisi order dari sisi client browser rentan terputus (*network glitch*, user menutup tab) setelah chart tersimpan di EMR. | Rekam medis mencatat order lab, tetapi instalasi lab tidak pernah menerima order tersebut di Bilreg (*silent clinical drop*). | Tampilkan feedback visual jelas (loading & toastr/swal konfirmasi sukses Bilreg) saat simpan chart, serta sediakan status indikator pada riwayat chart EMR. |
| **RISK-002** | Dokter memesan pemeriksaan lab dengan tarif yang belum terkonfigurasi pada `BILRG_LabTestDefinition` di Bilreg. | Transmisi order ditolak dengan exception `LAB_TEST_DEFINITION_NOT_FOUND`, memblokir alur pelayanan klinis pasien. | Lakukan audit kelengkapan master `LabTestDefinition` untuk seluruh tarif lab aktif di EMR sebelum integrasi dirilis. Berikan pesan validasi yang jelas di UI. |
| **RISK-003** | Terjadi duplikasi pengiriman ke analyzer LIS jika EMR tetap memanggil `sendOrderLIS` bersamaan dengan pengiriman otomatis dari Bilreg ke OWARE. | Pasien mendapatkan dua nomor order di mesin analyzer LIS, memicu pengerjaan tes ganda dan pemborosan reagen. | Alihkan pemanggilan OWARE hanya melalui Bilreg; bypass `sendOrderLIS` tidak dieksekusi jika pengiriman ke Bilreg sukses. |
| **RISK-004** | Dokter membatalkan/menghapus order di EMR padahal spesimen darah pasien telah diambil oleh petugas flebotomi di laboratorium. | Ketidaksinkronan status medis dan komplain pasien karena sampel sudah diambil namun order klinis hilang. | Kunci tombol hapus order di EMR jika status order di Bilreg sudah mencapai `Collected` atau `InProgress` dengan memeriksa query `byEmrOrderId`. |

---

# 7. Recommendations

## Option A: Frontend Direct API Integration (c001_emr2_frontend → Bilreg API) - **SELECTED**

Frontend EMR ([chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js)), setelah menerima respons sukses dari `Chart/Add` beserta daftar `ChartOrderId`, memanggil endpoint Bilreg `POST /api/LabContext/LabOrderFeature/fromEmr` secara langsung melalui AJAX untuk setiap order lab yang ada pada chart.

### Advantages
- Memungkinkan penanganan interaktif secara real-time terhadap peringatan duplikasi tes 24 jam (HTTP 409) dengan memunculkan dialog konfirmasi SweetAlert langsung kepada dokter pengorder.
- Menggunakan `config.base_api_bilreg` yang sudah terpasang di konfigurasi EMR tanpa merombak backend EMR (`a005_emr2backend`).
- Menggantikan alur bypass OWARE lama (`sendOrderLIS`) secara bersih di tempat pemanggilannya pada `chartNote.js`.

### Disadvantages
- Rentan kegagalan koneksi browser jika pengguna menutup tab sebelum transmisi selesai (dimitigasi dengan loading blocking dan notifikasi jelas).

---

## Option B: Backend-to-Backend Synchronous Push (EMR Backend → Bilreg API)

Setelah EMR backend (`a005_emr2backend`) berhasil mempersistensi `Chart/Add` dan menerbitkan `ChartOrderID`, backend EMR memanggil REST endpoint Bilreg `POST /api/LabContext/LabOrderFeature/fromEmr` secara sinkron menggunakan `HttpClient` terisolasi.

### Advantages
- Keandalan transmisi di sisi server, tidak bergantung pada koneksi browser.

### Disadvantages
- Sulit memfasilitasi dialog interaktif konfirmasi duplikasi 24 jam ke dokter karena error harus dibungkus ulang melalui response DTO `Chart/Add`.
- Membutuhkan modifikasi kode backend EMR legacy.

---

# 8. Gap Closure

*(Keputusan hasil sesi penajaman / grill-me pada 2026-11-02)*

## GAP-001: Ketiadaan Transmisi Order dari EMR ke Bilreg
- **Status**: CLOSED
- **Decision**: Transmisi order lab dari EMR ke Bilreg dieksekusi melalui Frontend EMR Orchestration ([chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js)) segera setelah `Chart/Add` sukses, memanggil endpoint Bilreg `POST /api/LabContext/LabOrderFeature/fromEmr`.
- **Rationale**: Memudahkan dialog interaktif konfirmasi duplikasi tes 24 jam langsung kepada dokter dan konsisten dengan pola integrasi modul modern lain pada `c001_emr2_frontend`.
- **Impact**: Frontend EMR menambahkan fungsi pemanggil API Bilreg; alur lama bypass OWARE digantikan.
- **Architecture Impact**: Menetapkan integrasi client-to-service langsung antara `c001_emr2_frontend` dan `b09-bilreg-api`.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## GAP-002: Ketidaksesuaian Kontrak Payload Antara EMR dan `LabOrderCreateFromEmrCmd`
- **Status**: CLOSED
- **Decision**: Frontend EMR menyusun payload DTO yang selaras dengan `LabOrderCreateFromEmrCmd`:
  - `UserId`: ID pengguna login EMR (`user.UserrID`)
  - `EmrOrderId`: `ChartOrderId` resmi dari respons backend EMR
  - `RegId`: `activeRegID`
  - `PatientId`: `pasienIDCurrent`
  - `PatientName`: Nama pasien aktif
  - `BirthDateYmd`: Tanggal lahir pasien format `YYYY-MM-DD`
  - `Gender`: Jenis kelamin pasien (`L` atau `P`)
  - `Items`: Daftar item dari `ListItem` (`TariffID` → `TarifId`, `TariffName` → `TarifName`)
  - `DokterPerujukId`: `dokterID` pengorder (terdaftar di `ORG-PPA`)
  - `DokterPerujukName`: `dokterName`
  - `LayananPerujukId`: `activeLynID`
  - `LayananPerujukName`: Nama unit pengorder
  - `ClinicalNote`: Keterangan order dari `Description`
  - `Urgency`: Nilai 0 (Rutin) atau 1 (Cito)
  - `AllowDuplicateConfirmation`: `false` (default)
- **Rationale**: Memenuhi seluruh invariant yang divalidasi oleh `LabOrderCreateFromEmrHandler` di Bilreg.
- **Impact**: Pemetaan atribut lengkap dan tervalidasi sebelum transmisi.
- **Architecture Impact**: Standardisasi kontrak DTO transmisi dari EMR ke Bilreg.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## GAP-003: Kebutuhan Penetapan `EmrOrderId` Unik Antar-Sistem
- **Status**: CLOSED
- **Decision**: Menggunakan `ChartOrderId` resmi dari respons `Chart/Add` EMR (PK tabel `dbo.EMREC_ChartOrder`, misal: `10000000000001234`) sebagai `EmrOrderId` di Bilreg.
- **Rationale**: `ChartOrderId` merupakan identifier persisten resmi dari basis data rekam medis yang unik untuk setiap entitas order lab dalam rekam medis, memastikan keunikan bahkan jika satu chart note memuat beberapa order lab.
- **Impact**: Transmisi ke Bilreg dilakukan setelah `Chart/Add` mengembalikan data `result.ListOrder`.
- **Architecture Impact**: Korelasi identitas persisten berbasis `ChartOrderId` ↔ `BILRG_LabOrder.EmrOrderId`.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## GAP-004: Potensi Kegagalan Resolusi Tes (`LAB_TEST_DEFINITION_NOT_FOUND`)
- **Status**: CLOSED
- **Decision**:
  1. Tampilkan notifikasi alert error yang jelas di UI EMR jika respons Bilreg mengembalikan error `LAB_TEST_DEFINITION_NOT_FOUND`, dengan menyebutkan nama pemeriksaan yang belum terkonfigurasi.
  2. Lakukan audit sinkronisasi data master `BILRG_LabTestDefinition` di Bilreg untuk seluruh tarif lab aktif di `TRK-TARIF` sebelum tahap go-live.
- **Rationale**: Mencegah kegagalan tak terduga saat dokter memesan pemeriksaan di poli/bangsal, dan memberikan transparansi diagnosa masalah kepada tim IT/Lab.
- **Impact**: Dibutuhkan langkah verifikasi kelengkapan master data tes lab di Bilreg.
- **Architecture Impact**: Penanganan error kontraktual yang terstruktur pada error handler frontend EMR.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## GAP-005: Ketiadaan Penanganan Interaktif Duplikasi Tes 24 Jam (`EX-05`) di EMR
- **Status**: CLOSED
- **Decision**: Jika Bilreg merespons dengan HTTP 409 / `DuplicateLabOrderWarningException`, frontend EMR menampilkan dialog konfirmasi SweetAlert yang memuat daftar nama tes duplikat dan nomor order sebelumnya. Jika dokter memilih "Lanjutkan", sistem mengirim ulang request ke Bilreg dengan parameter `AllowDuplicateConfirmation = true`. Jika memilih "Batal", order dibatalkan.
- **Rationale**: Memenuhi aturan bisnis medis `EX-05` di mana klinisi DPJP memiliki diskresi klinis untuk memesan tes ulang (misal pemantauan serial hemoglobin atau elektrolit) dengan justifikasi sadar.
- **Impact**: UX di EMR lebih ramah dan aman bagi klinisi.
- **Architecture Impact**: Menghubungkan exception domain Bilreg ke alur retry interaktif di UI client.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## GAP-006: Bypass OWARE Eksisting Harus Dinonaktifkan
- **Status**: CLOSED
- **Decision**: Alihkan pengiriman ke LIS sepenuhnya melalui Bilreg: jika order berhasil dikirim ke Bilreg, pemanggilan fungsi `sendOrderLIS` pada [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js) tidak dijalankan. Tanggung jawab pengiriman ke OWARE/LIS didelegasikan ke alur outbound Bilreg (`LabOwareFeature`).
- **Rationale**: Menghindari duplikasi pengiriman order ke analyzer LIS dan memastikan nomor order yang diterima LIS adalah `OrderNo` resmi dari Bilreg.
- **Impact**: Jalur bypass direct OWARE dari EMR dinonaktifkan.
- **Architecture Impact**: Eliminasi bypass integration boundary; penegakan Bilreg LWF sebagai single source of truth.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## GAP-007: Ketidaksinkronan Alur Pembatalan & Perubahan Order
- **Status**: CLOSED
- **Decision**:
  1. Saat membuka detail order lab di EMR, EMR melakukan query status ke Bilreg melalui `GET /api/LabContext/LabOrderFeature/byEmrOrderId/{emrOrderId}`.
  2. Jika status order di Bilreg telah mencapai `SampleCollected` / `InProgress` / `Charged`, tombol hapus order di EMR dikunci (disabled) dengan notifikasi bahwa spesimen telah diambil.
  3. Jika status masih `Ordered` / `Deferred`, dokter diizinkan menghapus order, dan frontend EMR secara otomatis memanggil endpoint pembatalan Bilreg `POST /api/LabContext/LabOrderFeature/cancel` dengan mengirimkan alasan pembatalan dokter.
- **Rationale**: Mencegah inkonsistensi status klinis di mana order dihapus di rekam medis tetapi darah pasien telah diambil di laboratorium.
- **Impact**: Penambahan sinkronisasi status real-time pada UI card order lab di EMR.
- **Architecture Impact**: Alur siklus hidup pembatalan tersinkronisasi dua arah secara *non-destructive*.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## GAP-008: Kesesuaian Master Dokter Pengorder (`DokterPerujukId`)
- **Status**: CLOSED
- **Decision**: Memastikan atribut `DokterPerujukId` yang dikirimkan menggunakan `chart.DokterID` yang terdaftar pada master PPA (`ORG-PPA`). Jika `DokterPerujukId` kosong atau belum terdaftar di master PPA, sistem tetap mengirimkan `DokterPerujukName` agar tidak menggagalkan penyimpanan di Bilreg.
- **Rationale**: `LabOrderCreateFromEmrHandler` di Bilreg hanya memvalidasi PPA jika `DokterPerujukId` terisi; fallback nama dokter menjaga fleksibilitas operasional.
- **Impact**: Tidak ada kegagalan order akibat ketidakcocokan kode dokter.
- **Architecture Impact**: Standar pemetaan kredensial tenaga medis perujuk.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

---

## OQ-001: Layer Transmisi Integrasi
- **Status**: CLOSED
- **Decision**: Frontend EMR Orchestration ([chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js)) via AJAX langsung ke Bilreg API.
- **Rationale**: Lihat GAP-001.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## OQ-002: Format Identitas EmrOrderId
- **Status**: CLOSED
- **Decision**: Menggunakan `ChartOrderId` dari respons `Chart/Add` EMR.
- **Rationale**: Lihat GAP-003.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## OQ-003: Penanganan Tarif Tanpa Konfigurasi LabTestDefinition
- **Status**: CLOSED
- **Decision**: Alert error di UI EMR + audit kelengkapan master data sebelum go-live.
- **Rationale**: Lihat GAP-004.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## OQ-004: Alur UX Peringatan Duplikasi 24 Jam
- **Status**: CLOSED
- **Decision**: Konfirmasi interaktif SweetAlert + retry dengan `AllowDuplicateConfirmation = true`.
- **Rationale**: Lihat GAP-005.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

## OQ-005: Strategi Penonaktifan Bypass OWARE Lama
- **Status**: CLOSED
- **Decision**: Dialihkan sepenuhnya melalui Bilreg LWF; `sendOrderLIS` tidak dieksekusi saat pengiriman Bilreg sukses.
- **Rationale**: Lihat GAP-006.
- **Resolved By**: User & Analyst (Grill-Me Session)
- **Resolved Date**: 2026-11-02

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Integrasi ini menghubungkan dua sistem utama yang berbeda bounded context dan repository (`c001_emr2_frontend` / `a005_emr2backend` dengan `b09-bilreg-api`), mengubah batas integrasi dengan middleware OWARE, menetapkan kontrak payload baru, mendefinisikan korelasi identitas persisten (`EmrOrderId`), serta mengatur penanganan alur konkurensi (peringatan duplikasi tes 24 jam dan pembatalan order). Oleh karena itu, diperlukan dokumen ARCHITECTURE resmi untuk menetapkan realisasi teknis target sebelum perencanaan implementasi.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] Konteks bisnis dan kebutuhan integrasi terdefinisi jelas
- [x] Analisis kondisi eksisting codebase EMR dan Bilreg telah dilakukan secara mendalam
- [x] Seluruh celah teknis (GAP-001 s.d. GAP-008) telah diidentifikasi dan ditutup (CLOSED)
- [x] Seluruh pertanyaan terbuka (OQ-001 s.d. OQ-005) telah dijawab dan diputuskan (CLOSED)
- [x] Asumsi dan risiko telah didokumentasikan beserta mitigasinya
- [x] Keputusan penutupan gap dan pemilihan opsi arsitektur telah disetujui (Sesi Grill-Me Selesai)
- [x] Status READY-FOR-PLANNING diberikan oleh peran Architect

## Status

**READY-FOR-PLANNING**

## Notes

Dokumen ini telah diperbarui oleh peran **Analyst** (`ica-analyst`) pasca-sesi penajaman (*grill-me*). Seluruh celah kritis dan pertanyaan terbuka telah memiliki keputusan definitif. Dokumen ini sekarang siap dikonsumsi oleh peran **Architect** (`ica-architect`) untuk menyusun target arsitektur teknis dan memberikan status persetujuan **READY-FOR-PLANNING**.

---

# 11. References

Referenced artifacts:
- [LAB-EMR-ORDER-INTEGRATION-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-EMR-ORDER-INTEGRATION-ISSUE.md)
- [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- [lab-integration.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-integration.md)
- [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)

Referenced codebase locations:
- EMR Frontend: [orderLab.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/orderLab.js)
- EMR Frontend: [chartNote.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/assets/js/MedicalChart/chartNote.js)
- EMR Frontend Config: [config.js](file:///D:/Project.Aktif/_emr/c001_emr2_frontend/config/config.js)
- EMR Backend: [ChartController.cs](file:///D:/Project.Aktif/_emr/a005_emr2backend/Emr2.Api/Controllers/ChartController.cs)
- EMR Backend: [ChartOrderModel.cs](file:///D:/Project.Aktif/_emr/a005_emr2backend/Emr2.Domain/LegacyLib/ChartDomain/Model/ChartOrderModel.cs)
- Bilreg Controller: [LabOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabOrderFeature/LabOrderController.cs)
- Bilreg Command Handler: [LabOrderCreateFromEmrCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateFromEmrCmd.cs)
- Bilreg Test Resolution: [LabTestResolutionService.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs)
- Bilreg Aggregate: [LabOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs)
- OWARE Controller: [OrderController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/OrderController.cs)
