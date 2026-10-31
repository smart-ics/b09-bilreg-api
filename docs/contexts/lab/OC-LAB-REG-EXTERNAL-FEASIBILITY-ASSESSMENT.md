---
Title: Feasibility Assessment - Registrasi Pasien Eksternal Laboratorium
Code: OC-LAB-REG-EXTERNAL
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-10-13
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan perubahan (*CHANGE-REQUEST*) untuk mengimplementasikan kapabilitas pencatatan resmi pendaftaran langsung pasien luar laboratorium (*direct external lab registration*) sebagai fakta bisnis persisten (*persisted business fact*) sesuai spesifikasi [OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md).

Referenced artifacts:

- ISSUE: [OC-LAB-REG-EXTERNAL-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-ISSUE.md) (`ISSUE-LAB-REG-EXTERNAL-001`)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md) dan [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- OUTCOME: [OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md)
- FEATURE: [OC-LAB-REG-EXTERNAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-FEATURE.md)

## Objective

Menilai kelayakan teknis, operasional, arsitektural, dan dampak implementasi dari penambahan kapabilitas `LAB-EXTERNAL` (`RegExternal`) pada backend Bilreg (`b09-bilreg-api`) dan antarmuka web LWF (`c012_myhospital_web`); mengidentifikasi kesenjangan (*gaps*), pertanyaan terbuka (*open questions*), asumsi, risiko, serta mendokumentasikan keputusan resmi penutupan celah (*Gap Closure*) sebelum perancangan arsitektur teknis difinalisasi oleh Arsitek.

---

# 2. Current State

Fakta kondisi sistem saat ini berdasarkan investigasi artefak dan kode sumber:

## Existing Behavior

1. **Kondisi Backend Bilreg (`b09-bilreg-api`)**:
   - Pada `Bilreg.Domain.LabContext`, saat ini hanya terdapat 5 fitur: `LabComponentMasterFeature`, `LabOrderFeature`, `LabOwareFeature`, `LabResultFeature`, dan `LabTestDefinitionFeature`. Belum ada fitur atau agregat `LabExternalFeature` / `RegExternalFeature`.
   - Pada [LabOrderCreateExternalPatientCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateExternalPatientCmd.cs), use case menerima parameter `RegId`, `PatientId`, `PatientName`, `BirthDateYmd`, `Gender`, dan daftar `Items`. Handler ini langsung membentuk [PatientSnapshotType.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/PatientSnapshotType.cs) dan memanggil `LabOrderModel.CreateExternal(...)`, menyimpan data langsung ke tabel `BILRG_LabOrder`.
   - Tidak ada entitas persisten yang menyimpan fakta registrasi eksternal secara independen. Tidak ada penerbitan nomor identitas operasional sekali pakai (`ExternalMrNo`).
   - Pada alur pembebanan biaya di [LabOrderChargeCmd.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderChargeCmd.cs), handler melakukan pemanggilan:
     ```csharp
     var regKey = RegModel.Key(order.Patient.RegId);
     var reg = LoadReg(regKey);
     ```
     di mana `LoadReg` mencari registrasi ke `_regRepo` (Admisi Rawat Jalan/Reguler). Jika order pasien luar tidak memiliki `RegId` admisi yang sah di `_regRepo`, proses `LabOrderChargeCmd` **gagal dengan `KeyNotFoundException`**.
   - Integrasi billing laboratorium pada [LabBillingIntegration.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabBillingIntegration.cs) saat ini masih berupa *stub / mock* (`TDK-FAKE-xxxx`).
   - Tidak terdapat tabel `BILRG_LabExternal` atau tabel penunjang registrasi luar di `Bilreg.SqlDb/LabContext`.

2. **Kondisi Frontend Web (`c012_myhospital_web`)**:
   - Dialog registrasi walk-in saat ini diimplementasikan pada [WalkInExamDialog.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/WalkInExamDialog.vue), yang hanya meminta data: `patientName`, `birthDate`, `gender`, dan `address`.
   - Belum terdapat input untuk NIK/KTP, nomor telepon/WhatsApp, dan informasi asal rujukan (APS vs Rujukan Luar dengan dokter perujuk dan instansi pengirim).
   - Pada [lab-walk-in-order.ts](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/lib/lab-walk-in-order.ts), pendaftaran walk-in langsung disatukan dengan pemesanan tes menggunakan data stub hardcoded: `DEFAULT_WALK_IN_LAB_ITEMS` (`T-WALKIN`, `TR-WALKIN`), tanpa alur pembentukan akun penagihan kasir atau pemilihan tarif riil.

## Existing Constraints

1. **Domain Boundary & Isolation**: Registrasi eksternal laboratorium dilarang menyisipkan data pasien ke dalam master data pasien permanen rumah sakit (`PAS-DATSOS`) dan dilarang membentuk kunjungan reguler di `ADM-REG`.
2. **One-Time Identity**: Nomor identitas/rekam medis eksternal yang diterbitkan bersifat sekali pakai (*one-time use only*) dan tidak boleh digunakan untuk registrasi kunjungan rumah sakit umum lainnya.
3. **Penyelesaian Finansial Mandiri (*Cash-First Principle*)**: Pasien eksternal wajib memiliki akun penagihan kasir langsung di domain Tata Rekening (`TRK-BILLING` dan `TRK-KASIR`), dan pembayaran diselesaikan sebelum pengambilan spesimen atau penyerahan hasil.
4. **Validasi Pembatalan**: Registrasi eksternal tidak boleh dibatalkan jika telah terdapat pembayaran lunas di kasir atau spesimen telah diambil pada order lab terkait.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|---|---|---|
| GAP-001 | CRITICAL | Ketiadaan Agregat, Tabel Database, DAL, dan Fitur Khusus `LabExternal` (`LAB-EXTERNAL`) di Backend Bilreg. Saat ini belum ada mekanisme persisten untuk menyimpan entitas registrasi eksternal. |
| GAP-002 | CRITICAL | Kegagalan Alur Billing Charge (`LabOrderChargeCmd`) untuk Pasien Eksternal. Handler saat ini mewajibkan pembacaan `RegModel` dari `_regRepo` (Admisi), yang menyebabkan *runtime crash* jika `RegId` bukan registrasi admisi reguler. |
| GAP-003 | CRITICAL | Ketiadaan Mekanisme Penerbitan Nomor Rekam Medis Operasional Sekali Pakai (`ExternalMrNo`). Belum ada generator nomor identitas sementara khusus eksternal. |
| GAP-004 | MAJOR | Ketiadaan Perekaman Informasi Asal Rujukan (APS vs Rujukan Luar dengan detail dokter perujuk dan instansi luar) serta Kontak/NIK Pasien pada backend dan frontend. |
| GAP-005 | MAJOR | Ketiadaan Pengelolaan Siklus Hidup Registrasi Eksternal (`Active`, `Completed`, `Cancelled`) beserta aturan pembatalan non-destructive dan pencatatan audit pembatalan. |
| GAP-006 | MAJOR | Antarmuka pengguna walk-in pada frontend (`WalkInExamDialog.vue` & `lab-walk-in-order.ts`) masih menggunakan stub hardcoded item order dan belum memfasilitasi pembentukan akun billing kasir. |

---

# 4. Open Questions

| ID | Question | Impact |
|---|---|---|
| OQ-001 | Bagaimana konvensi format dan skema generator untuk nomor rekam medis operasional sekali pakai (`ExternalMrNo`)? | Mempengaruhi perancangan komponen sequencer atau autunumber di layer domain/infrastruktur. |
| OQ-002 | Bagaimana mekanisme pembentukan akun penagihan langsung di domain Tata Rekening (`TRK-BILLING`) untuk registrasi eksternal—apakah via direct service integration di Bilreg atau modul kasir laboratorium terintegrasi? | Mempengaruhi kontrak antarmuka integrasi finansial dan relasi dependensi modul. |
| OQ-003 | Apakah proses input pendaftaran eksternal dan pembuatan order laboratorium di UI dipisahkan menjadi dua langkah berurutan (Registrasi Eksternal → Buka Order Lab & Kasir), atau disatukan dalam satu wizard/dialog transaksi? | Mempengaruhi desain interaksi layar operasional laboratorium dan endpoint API yang dipanggil oleh frontend. |

---

# 5. Assumptions

| ID | Assumption |
|---|---|
| ASM-001 | Pelayanan laboratorium eksternal ditujukan untuk pembayaran mandiri/umum (Tunai/Non-Tunai) dan tidak memerlukan integrasi bridging BPJS VClaim. |
| ASM-002 | Domain Tata Rekening (`TRK-BILLING` & `TRK-KASIR`) mendukung pembentukan akun tagihan kasir penunjang yang merujuk pada `RegExternalId` tanpa memerlukan kunjungan admisi rumah sakit. |
| ASM-003 | Master tarif pemeriksaan yang digunakan oleh pasien eksternal mengacu pada tabel tarif rawat jalan/penunjang reguler (`TRK-TARIF`) dengan kelas/kategori umum. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|---|---|---|---|
| RISK-001 | Inkonsistensi data jika registrasi eksternal tersimpan tetapi pembuatan akun billing kasir di `TRK-BILLING` gagal. | CRITICAL | Membungkus pembentukan registrasi eksternal dan akun billing dalam batas transaksional yang aman (*transaction scope* / orkestrasi atomik). |
| RISK-002 | Penggunaan kembali `ExternalMrNo` untuk episode rawat jalan/rawat inap umum akibat ketidakjelasan pemisahan data. | MAJOR | Menetapkan format penomoran dengan prefiks unik (misal: `EX-`) dan menegakkan validasi domain bahwa identitas ini ditolak oleh modul admisi reguler. |
| RISK-003 | Regresi pada alur kerja operasional laboratorium jika use case existing `LabOrderCreateExternalPatientCmd` diubah tanpa backwards compatibility. | MEDIUM | Merancang use case baru `LabExternalCreateCmd` dan menyesuaikan `LabOrderCreateExternalPatientCmd` agar mengonsumsi konteks `RegExternal` yang sah. |

---

# 7. Recommendations

## Option A: Membangun Fitur Independen `LabExternalFeature` di dalam `Bilreg.Domain.LabContext` (Direkomendasikan)

Membuat bounded feature baru `LabExternalFeature` yang mencakup:
- Entitas persisten `LabExternalModel` (`RegExternalId`, `ExternalMrNo`, Demografi, Kategori Rujukan, Status, Audit).
- Tabel database tersendiri `BILRG_LabExternal`.
- Sequencer khusus untuk `ExternalMrNo`.
- Handler `LabExternalCreateCmd` dan `LabExternalCancelCmd`.
- Penyesuaian `LabOrderFeature` agar mengenali `RegExternalId` sebagai `RegId` valid tanpa mencari ke `_regRepo` admisi reguler saat proses charging/penagihan.

### Advantages

- Sepenuhnya patuh pada prinsip DDD dan pemisahan kapabilitas kanonikal (`LAB-EXTERNAL` terpisah dari `LAB-ORDER`).
- Menjamin isolasi mutlak dari domain `Pasien` dan `Admission`.
- Memberikan audit trail dan siklus hidup registrasi yang transparan.

### Disadvantages

- Memerlukan penambahan tabel baru, DAL, DTO, dan use case baru di backend dan pembaruan komponen UI di frontend.

## Option B: Memperluas Tabel `BILRG_LabOrder` secara In-line Tanpa Entitas Registrasi Terpisah

Menambahkan kolom-kolom demografi dan rujukan luar langsung ke dalam tabel `BILRG_LabOrder`.

### Advantages

- Tidak memerlukan entitas baru di luar `LabOrderFeature`.

### Disadvantages

- Melanggar batas kapabilitas bisnis (`LAB-EXTERNAL` vs `LAB-ORDER`).
- Menyebabkan duplikasi data jika satu registrasi eksternal memiliki beberapa order atau revisi transaksi.
- Tidak menyediakan nomor rekam medis operasional sekali pakai yang independen untuk transaksi kasir penunjang.

---

# 8. Gap Closure

## GAP-001: Ketiadaan Agregat dan Tabel Khusus `LabExternal`
- **Status:** CLOSED
- **Decision:** Membentuk bounded feature baru `LabExternalFeature` di `Bilreg.Domain.LabContext`, `Bilreg.Application.LabContext`, dan `Bilreg.Infrastructure.LabContext` dengan tabel database `BILRG_LabExternal`.
- **Rationale:** Memenuhi spesifikasi kanonikal `OC-LAB-REG-EXTERNAL` bahwa pendaftaran eksternal adalah entitas fakta bisnis persisten yang sah dan mandiri di bawah kapabilitas `LAB-EXTERNAL`.
- **Impact:** Pembuatan model domain, repository, DAL, migration script database, dan use cases baru.
- **Architecture Impact:** Penambahan sub-namespace baru `LabExternalFeature` di dalam LabContext.
- **Resolved By:** Analyst (Konsisten dengan spesifikasi OC-LAB-REG-EXTERNAL)
- **Resolved Date:** 2026-10-13

## GAP-002: Kegagalan Alur Billing Charge untuk Pasien Eksternal
- **Status:** CLOSED
- **Decision:** Menyesuaikan mekanisme resolusi registrasi pada `LabOrderChargeCmd` agar mendukung dua jalur registrasi: jika order berasal dari registrasi admisi (`OrderSource != ExternalPatient`), membaca dari `_regRepo`; jika order berasal dari registrasi eksternal, membaca konteks penagihan dari `LabExternal` atau akun billing mandiri.
- **Rationale:** Mencegah `KeyNotFoundException` saat melakukan charge tindakan order laboratorium pasien luar.
- **Impact:** Refaktorisasi handler `LabOrderChargeHandler` dan abstraksi resolusi tarif/billing.
- **Architecture Impact:** Penyesuaian kontrak integrasi antara `LabOrderFeature` dan komponen billing untuk context eksternal.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-13

## GAP-003: Ketiadaan Nomor Rekam Medis Operasional Sekali Pakai
- **Status:** CLOSED
- **Decision:** Menyediakan sequencer/generator nomor rekam medis operasional sekali pakai (`ExternalMrNo`) dengan format khusus (15 karakter: `EX-{yyyyMM}-{00000}`) yang secara tegas terpisah dari nomor rekam medis master rumah sakit dan kompatibel dengan batasan legacy `PasienId VARCHAR(15)`.
- **Rationale:** Memenuhi ketentuan kanonikal *one-time use only* tanpa mencemari master data pasien (`PAS-DATSOS`).
- **Impact:** Penambahan logika penomoran di service sequencer domain laboratorium.
- **Architecture Impact:** Pemanfaatan `ISequencer` atau `AutoNumber` provider untuk prefiks `EX`.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-13

## GAP-004: Perekaman Informasi Asal Rujukan dan Demografi Lengkap
- **Status:** CLOSED
- **Decision:** Merekam atribut lengkap sesuai spesifikasi: Demografi (Nama, NIK opsional, Tanggal Lahir/Usia, Gender wajib, No Telepon/WA, Alamat) dan Informasi Rujukan (Kategori Kedatangan APS vs Rujukan Luar, Nama Dokter Perujuk, Instansi Pengirim, Diagnosis Rujukan).
- **Rationale:** Data jenis kelamin dan usia mutlak dibutuhkan untuk penentuan rentang nilai normal analitik instrumen lab, sedangkan nomor kontak dibutuhkan untuk penyerahan hasil digital.
- **Impact:** Perluasan payload command dan form input frontend.
- **Architecture Impact:** Nilai rujukan menjadi value object atau atribut dalam agregat `LabExternalModel`.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-13

## GAP-005: Manajemen Siklus Hidup dan Aturan Pembatalan
- **Status:** CLOSED
- **Decision:** Menerapkan state machine siklus hidup `Active` → `Completed` atau `Cancelled` dengan aturan validasi: pembatalan ditolak jika telah ada transaksi pembayaran di kasir atau spesimen telah diambil pada order lab terkait. Jejak audit pembatalan dicatat secara non-destructive.
- **Rationale:** Mencegah inkonsistensi operasional dan finansial pada instalasi laboratorium.
- **Impact:** Penambahan method `Cancel(...)` pada model domain dengan guard clauses validasi status kasir/spesimen.
- **Architecture Impact:** Pemeriksaan kondisi cross-feature (`LabExternal` memeriksa status pembayaran dan status order).
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-13

## GAP-006: Antarmuka UI Walk-In Menggunakan Stub Hardcoded
- **Status:** CLOSED
- **Decision:** Memperbarui antarmuka pendaftaran pasien eksternal di modul Laboratory frontend agar menangkap seluruh field demografi dan rujukan yang diwajibkan, serta memicu endpoint `LabExternalCreate` sebelum melanjutkan ke pemilihan tindakan/order lab riil.
- **Rationale:** Menghilangkan data stub dummy (`T-WALKIN`) dan memastikan integritas data operasional end-to-end.
- **Impact:** Pembaruan komponen dialog dan form di frontend `c012_myhospital_web`.
- **Architecture Impact:** Pemisahan atau orkestrasi panggilan API registrasi eksternal dan pembuatan order.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-13

---

## OQ-001: Format dan Penomoran `ExternalMrNo`
- **Status:** CLOSED
- **Decision:** Menggunakan format terstandarisasi dengan prefiks `EX-` diikuti penomoran sekuensial (format 15 karakter: `EX-{yyyyMM}-{00000}`) yang dikelola oleh sequencer khusus lab guna menjamin kompatibilitas dengan kolom `PasienId VARCHAR(15)` sistem legacy.
- **Rationale:** Menjamin keunikan nomor, kejelasan identitas sebagai nomor sekali pakai, dan mencegah benturan dengan format nomor rekam medis master rumah sakit.
- **Impact:** Penambahan generator nomor sequence di layer domain/infrastruktur.
- **Architecture Impact:** Diatur dalam arsitektur penomoran `ISequencer`.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-13

## OQ-002: Mekanisme Pembentukan Akun Penagihan Kasir di `TRK-BILLING`
- **Status:** CLOSED
- **Decision:** Akun penagihan kasir langsung di Tata Rekening dibentuk secara atomik/terkoordinasi pada saat registrasi eksternal disimpan, menghasilkan `BillingAccountId` yang disimpan pada entitas `LabExternal`.
- **Rationale:** Menjamin kepatuhan terhadap prinsip *Cash-First* dan kesiapan transaksi di loket kasir laboratorium.
- **Impact:** Integrasi antara use case registrasi eksternal dengan billing service.
- **Architecture Impact:** Kontrak antarmuka `ILabBillingIntegration` diperluas untuk mendukung pembentukan akun billing registrasi eksternal.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-13

## OQ-003: Alur Pendaftaran dan Order di UI
- **Status:** CLOSED
- **Decision:** Di UI disajikan sebagai alur terpadu yang mulus bagi petugas: Form Pendaftaran Pasien Luar (menyimpan `RegExternal` dan menerbitkan `ExternalMrNo`) yang langsung membuka sesi pemilihan item tes lab riil (`LAB-ORDER`) dengan konteks pasien eksternal tersebut.
- **Rationale:** Menjaga kecepatan pelayanan (*fast-track*) bagi staf laboratorium tanpa mengorbankan integritas batas kapabilitas backend.
- **Impact:** Desain antarmuka dialog walk-in yang mendukung alur multi-tahap (Data Pasien Eksternal → Pilihan Pemeriksaan).
- **Architecture Impact:** Pemanggilan API sekuensial atau komposisi orkestrasi di layer frontend/BFF.
- **Resolved By:** Analyst
- **Resolved Date:** 2026-10-13

---

# 9. Architecture Applicability

Record the Architecture Applicability decision.

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Perubahan ini memperkenalkan kapabilitas baru `LAB-EXTERNAL` yang membutuhkan:
1. Pembentukan komponen dan agregat domain baru (`LabExternalModel`) serta persistensi database baru (`BILRG_LabExternal`).
2. Perubahan batas integrasi antara `LabOrderFeature` dan `_regRepo` (Admisi) yang mempengaruhi penanganan pembebanan biaya (*charging*).
3. Penambahan generator identitas operasional sekali pakai (`ExternalMrNo`).
4. Koordinasi baru antara `LabExternal` dan domain Tata Rekening (`TRK-BILLING` / `TRK-KASIR`).
Oleh karena itu, diperlukan perancangan arsitektur teknis formal oleh Arsitek (*ica-architect*) sebelum implementasi kode dapat direncanakan.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved (Keputusan gap closure telah didefinisikan secara konseptual)
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated (Telah ditinjau dan disahkan oleh Arsitek)

## Status

**READY-FOR-PLANNING**

## Notes

Gerbang `READY-FOR-PLANNING` telah resmi diberikan oleh peran **ica-architect** pada tanggal 2026-10-13. Seluruh kesenjangan (*GAPs*) dan pertanyaan terbuka (*OQs*) telah tuntas ditutup dengan keputusan fungsional (*Gap Closure*), serta spesifikasi Screen Inventory dan arsitektur teknis telah dirumuskan untuk mendukung tahapan perencanaan implementasi (*IMPLEMENTATION-PLAN*).

---

# 11. References

Referenced artifacts:

- ISSUE: [OC-LAB-REG-EXTERNAL-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-ISSUE.md)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md)
- OUTCOME: [OC-LAB-REG-EXTERNAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-REG-EXTERNAL.md)
- FEATURE: [OC-LAB-REG-EXTERNAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-REG-EXTERNAL-FEATURE.md)

Referenced codebase locations:

- [`b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateExternalPatientCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderCreateExternalPatientCmd.cs)
- [`b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature/LabOrderModel.cs)
- [`b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderChargeCmd.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderChargeCmd.cs)
- [`b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabBillingIntegration.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/Integration/LabBillingIntegration.cs)
- [`c012_myhospital_web/src/modules/Laboratory/components/WalkInExamDialog.vue`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/components/WalkInExamDialog.vue)
- [`c012_myhospital_web/src/modules/Laboratory/lib/lab-walk-in-order.ts`](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Laboratory/lib/lab-walk-in-order.ts)
