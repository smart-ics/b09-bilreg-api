# ISSUE

## Metadata

ID: LAB-ORDER-PA-MICRO-ACCOMMODATION-ISSUE
Type: CHANGE-REQUEST
Status: OPEN
Title: Akomodasi Order Laboratorium untuk Pemeriksaan Patologi Anatomi dan Mikrobiologi

## Source

Reported By: User
Reported Date: 2026-10-30

## Description

Saat ini sistem order laboratorium pada HIS / Bilreg belum dapat mengakomodasi proses pemesanan (order) untuk jenis pemeriksaan Patologi (khususnya Patologi Anatomi / Lab PA) dan Mikrobiologi (Lab Micro). Alur order lab yang berjalan saat ini masih terbatas dan berorientasi pada pemeriksaan Patologi Klinik (Clinical Pathology / PK) standar dengan parameter analit tunggal/berulang dan tabung vacutainer darah/cairan tubuh rutin.

Pengguna menginginkan agar sistem order laboratorium dapat mengakomodasi kebutuhan pemesanan pemeriksaan ketika target pemeriksaannya adalah Patologi (PA) dan Mikrobiologi.

Sementara itu, pada layer middleware integrasi LIS/PACS (`a046_oware_ordermiddlewareapi`), kapabilitas bridging untuk menerima hasil (*result ingestion/bridging*) pemeriksaan Patologi Anatomi dan Mikrobiologi sudah diimplementasikan dan tersedia. Namun, siklus pemesanan (*order intake & outbound*) dari hulu belum memiliki struktur data, klasifikasi, dan antarmuka pemesanan yang memadai untuk kedua disiplin laboratorium tersebut.

## Desired Outcome

1. **Akomodasi Pemesanan Patologi (PA)**:
   - Sistem order laboratorium mampu menerima dan mengelola order untuk kelompok pemeriksaan Patologi Anatomi.
   - Menampung informasi spesifik yang dibutuhkan untuk pemeriksaan PA, seperti asal jaringan / lokasi sediaan anatomis (`SpecimenSite`), diagnosa klinis pengantar (`ClinicalDiagnosis`), jenis prosedur/tindakan PA (`ProcedureType`), dan penandaan terkait onkologi/kanker jika relevan.
2. **Akomodasi Pemesanan Mikrobiologi (Micro)**:
   - Sistem order laboratorium mampu menerima dan mengelola order untuk kelompok pemeriksaan Mikrobiologi (baik non-kultur/mikroskopis maupun biakan/kultur dan uji kepekaan antibiotik / AST).
   - Menampung informasi spesifik mikrobiologi seperti jenis spesimen biologis, lokasi/sumber bahan klinis, serta indikasi kecurigaan mikroorganisme.
3. **Keterhubungan Integrasi Hulu-Hilir (HIS -> Bilreg -> OWARE -> LIS)**:
   - Data order laboratorium Patologi dan Mikrobiologi dapat diproses di Bilreg dan ditransmisikan secara tepat ke OWARE (`a046_oware_ordermiddlewareapi`) dengan klasifikasi tipe order yang sesuai (`OrderTypeEnum.LabPa` dan `OrderTypeEnum.LabMicro`).
   - Melengkapi siklus operasional laboratorium bersama bridging hasil PA (`/api/result/lab/pa`) dan Mikrobiologi (`/api/result/lab/micro`) yang sudah ada.

## Current Situation

1. **Model Order Bilreg**:
   - Model `LabOrderModel` dan `LabOrderItemModel` pada `b09-bilreg-api` saat ini dibangun dengan asumsi dasar pemeriksaan Patologi Klinik (menggunakan `VacutainerTypeEnum`, `RequiredTubeCount`, `LabTestCode`, `LabTestName`).
   - Belum tersedia pemodelan atau atribut untuk mencatat asal organ / lokasi jaringan sediaan (`SpecimenSite`), tipe prosedur PA, maupun penandaan khusus spesimen mikrobiologi pada entitas order laboratorium.
2. **Middleware OWARE (`a046_oware_ordermiddlewareapi`)**:
   - Telah memiliki enum tipe order yang mencakup PA dan Micro: `OrderTypeEnum` (`Pacs`, `Lab`, `LabPa`, `LabMicro`) pada [OrderTypeEnum.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/ValueObjects/OrderTypeEnum.cs#L3-L6).
   - Telah menyediakan model domain dan endpoint penerimaan hasil pemeriksaan Patologi Anatomi (`LabPaResultModel.cs`, `POST /api/result/lab/pa`) dan Mikrobiologi (`LabMicroResultModel.cs`, `POST /api/result/lab/micro`) pada [ResultController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs#L214-L245).
   - Namun, endpoint order lab pada OWARE [OrderController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/OrderController.cs#L72-L96) (`POST /api/order/lab`, `POST /api/order/lab/bilreg`) saat ini hanya memetakan model `OrderLabModel` umum tanpa penanganan khusus untuk alur order PA dan Mikrobiologi.
3. **Antarmuka Pemesanan Lab**:
   - Formulir/canvas pembuatan order laboratorium pada frontend (`LabOrderCreateCanvas.vue`) belum menyediakan pemilihan disiplin / kategori pemeriksaan untuk Patologi Anatomi dan Mikrobiologi beserta field data klinis pendukungnya.

## Evidence

- **Kapabilitas Bridging Hasil Lab PA di OWARE**:
  - Model domain hasil PA: [LabPaResultModel.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/Entities/Lab/LabPaResultModel.cs#L10-L36)
  - Endpoint penerimaan hasil PA: [ResultController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs#L214-L229)
  - Spesifikasi domain PA: [LAB-PA-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/docs/LAB-PA-DOMAIN.md)
- **Kapabilitas Bridging Hasil Lab Mikrobiologi di OWARE**:
  - Model domain hasil Mikrobiologi: [LabMicroResultModel.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/Entities/Lab/LabMicroResultModel.cs#L8-L19)
  - Endpoint penerimaan hasil Mikrobiologi: [ResultController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/ResultController.cs#L231-L245)
  - Spesifikasi kanonikal mikrobiologi: [CANONICAL-DOMAIN-SPECIFICATION-MICROBIOLOGY.md](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/docs/CANONICAL-DOMAIN-SPECIFICATION-MICROBIOLOGY.md)
- **Klasifikasi Order di OWARE**:
  - Definisi tipe order: [OrderTypeEnum.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Domain/ValueObjects/OrderTypeEnum.cs#L3-L6)
  - Kontroler pemesanan eksisting di OWARE: [OrderController.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Api/Controllers/OrderController.cs#L72-L96)
- **Struktur Order Lab Bilreg**:
  - Definisi order lab di Bilreg: [LabOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs#L7-L86)
  - Definisi item order lab di Bilreg: [LabOrderItemModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderItemModel.cs#L3-L15)
- **Antarmuka Pemesanan Lab**:
  - Komponen canvas order: [LabOrderCreateCanvas.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/canvas/LabOrderCreateCanvas.vue)

## Notes

- Kebutuhan ini diklasifikasikan sebagai CHANGE-REQUEST yang bersifat lintas-komponen (Frontend Pemesanan Lab, Backend `b09-bilreg-api` LabContext, dan Middleware `a046_oware_ordermiddlewareapi`).
- Batasan peran Issue Intake (`ica-issuer`): Dokumen ini murni menangkap permintaan perubahan, situasi saat ini, hasil yang diharapkan, serta bukti rujukan pendukung tanpa menetapkan analisis arsitektur, rincian teknis payload, atau strategi implementasi.
- Alur selanjutnya: ISSUE ini diteruskan ke `ica-analyst` untuk dilakukan tahapan Discovery dan Analisis Kelayakan (*FEASIBILITY-ASSESSMENT*).
