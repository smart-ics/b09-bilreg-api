---
Title: Feasibility Assessment - Penyelarasan Definisi Pemeriksaan Laboratorium & Pemesanan (OC-LAB-TEST v1.1)
Code: OC-LAB-TEST
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-10-16
Status: NOT-READY
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan perubahan (*CHANGE-REQUEST*) berdasarkan [OC-LAB-TEST-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-ISSUE.md) (`ISSUE-LAB-TEST-001`) untuk menyelaraskan kapabilitas Definisi Pemeriksaan Laboratorium (`LAB-TEST`) dan Pemesanan Pemeriksaan Laboratorium (`LAB-ORDER`) pada backend Bilreg (`b09-bilreg-api`) sesuai pembaruan spesifikasi kanonikal [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) (Versi 1.1, Approved 2026-10-15).

Referenced artifacts:

- ISSUE: [OC-LAB-TEST-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-ISSUE.md) (`ISSUE-LAB-TEST-001`)
- OUTCOME: [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) (Versi 1.1)
- FEATURE: [OC-LAB-TEST-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-FEATURE.md)
- RELATED OUTCOME: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- RELATED FEATURE: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)

## Objective

Menilai kelayakan teknis, domain bisnis, dan dampak arsitektural dari adopsi aturan *Shared Tariff (Many-to-One)* dan pencantuman keterangan nama tes pada transaksi penagihan (`TRK-BILLING`); mengidentifikasi kesenjangan (*gaps*) antara kode sumber eksisting dengan spesifikasi `OC-LAB-TEST.md` v1.1; merumuskan opsi solusi rekomendasi; serta mendokumentasikan pertanyaan terbuka (*open questions*) untuk disepakati sebelum pembaruan arsitektur teknis disusun.

---

# 2. Current State

Fakta kondisi implementasi saat ini berdasarkan penelusuran kode sumber backend `b09-bilreg-api`:

## Existing Behavior

1. **Struktur Master Definisi Tes ([`LabTestDefinitionModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabTestDefinitionFeature/LabTestDefinitionModel.cs))**:
   - Model agregat `LabTestDefinitionModel` merekam data: `TestDefinitionId` (LTD + 4 digit heksadesimal), `TarifId`, `TarifCode`, `TarifName`, `LabTestCode`, `LabTestName`, `SpecimenType`, `VacutainerType` (`VacutainerTypeEnum`), `IsActive`, jejak audit, dan koleksi komponen `Components` (`LabTestComponentModel`).
   - Setiap definisi tes berhasil mengikat tepat 1 jenis spesimen dan 1 wadah vacutainer, serta menyusun parameter analitik terurut berurutan (`SequenceNo`) tanpa komponen duplikat.
2. **Validasi Invariant Konflik Tarif Tunggal ([`LabTestDefinitionSupport.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabTestDefinitionFeature/LabTestDefinitionSupport.cs))**:
   - Metode `EnsureNoActiveTarifConflict` memeriksa keberadaan definisi tes aktif lain dengan `TarifId` yang sama melalui `repo.HasActiveTarifConflict(tarifId, excludeTestDefinitionId)`.
   - Jika ditemukan, sistem melempar exception:
     `InvalidOperationException("LAB_TEST_DEFINITION_TARIF_CONFLICT: Another active LabTestDefinition already exists for this TarifId.")`.
   - Hal ini secara ketat memaksakan relasi 1-to-1 antara tarif dan definisi tes laboratorium.
3. **Mekanisme Resolusi Tes pada Alur Order ([`LabTestResolutionService.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs))**:
   - Menerima koleksi `LabOrderTarifItemInput(string TarifId, string TarifName, int Urgency)`.
   - Melakukan resolusi definisi tes via `_definitionRepo.LoadActiveByTarifId(input.TarifId)`.
   - Implementasi pada DAL ([`LabTestDefinitionDal.cs#L181`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabTestDefinitionFeature/LabTestDefinitionDal.cs#L181)) menggunakan kueri:
     `SELECT TOP 1 ... FROM BILRG_LabTestDefinition WHERE TarifId = @TarifId AND IsActive = 1`.
   - Apabila terdapat beberapa pemeriksaan aktif yang berbagi tarif yang sama (*shared tariff*), metode ini mengembalikan baris acak pertama dan tidak dapat mengetahui pemeriksaan spesifik apa yang sesungguhnya dipesan oleh dokter/pengorder.
4. **Pembentukan Rincian Tagihan Tindakan Kasir ([`TrsBillFactory.cs#L134`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillFactory.cs#L134))**:
   - Pada `TrsBillFactory.CreateFromLabOrderItem`, instansiasi `TrsBillKetType` mengisi parameter keterangan dengan string kosong `""`:
     `var ketBilling = new TrsBillKetType(tarif.TarifName, "", tarif.TarifId, 1, "");`.
   - Nama spesifik tes laboratorium (`labOrderItem.LabTestName`) tidak dicantumkan ke dalam pos keterangan tagihan tindakan pasien.

## Existing Constraints

1. **Skema Basis Data DDL ([`BILRG_LabTestDefinition.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabTestDefinitionFeature/BILRG_LabTestDefinition.sql))**:
   - Indeks pada kolom `TarifId` didefinisikan sebagai `CREATE INDEX IX_LabTestDefinition_TarifId ON BILRG_LabTestDefinition(TarifId, IsActive);` (bukan *unique index*). Skema SQL Server secara fisik sudah mendukung banyak baris dengan `TarifId` yang sama.
2. **Kekebalan Riwayat Transaksi (*Non-Retroactivity*)**:
   - Transaksi order ([`BILRG_LabOrderItem.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrderItem.sql)) menyalin snapshot `TestDefinitionId`, `LabTestCode`, `LabTestName`, `TarifId`, `TarifCode`, dan `TarifName`. Setiap perubahan master tidak memengaruhi order yang telah terbit sebelumnya.

---

# 3. Gap Analysis

| ID | Severity | Gap |
| :--- | :---: | :--- |
| **GAP-001** | **CRITICAL** | **Invariant Pembatasan Tarif Tunggal (*Single Active Definition per TarifId*) Menolak Skema Tarif Bersama (*Shared Tariff*).**<br>Metode `EnsureNoActiveTarifConflict` pada `LabTestDefinitionSupport` melempar `LAB_TEST_DEFINITION_TARIF_CONFLICT` jika ada lebih dari 1 definisi tes aktif yang menggunakan `TarifId` yang sama. Hal ini bertentangan secara langsung dengan `OC-LAB-TEST.md` (v1.1) §5.1 #2 dan AC-03 yang secara eksplisit mensyaratkan dukungan relasi *Many-to-One* (satu tarif dapat digunakan bersama oleh beberapa pemeriksaan aktif). |
| **GAP-002** | **CRITICAL** | **Resolusi Pemesanan Laboratorium Bersifat Ambigu pada Skema Tarif Bersama.**<br>Model masukan `LabOrderTarifItemInput` saat ini hanya menerima `TarifId`, dan `LabTestResolutionService.ResolveByTarifItems` melakukan resolusi via `LoadActiveByTarifId(tarifId)`. Jika satu tarif digunakan bersama oleh beberapa definisi tes aktif, sistem tidak dapat menentukan definisi tes spesifik mana yang dipesan oleh pengorder. Hal ini melanggar `OC-LAB-TEST.md` §5.3 dan AC-05. |
| **GAP-003** | **MAJOR** | **Ketiadaan Keterangan Nama Spesifik Pemeriksaan pada Rincian Penagihan Tata Rekening.**<br>Pada `TrsBillFactory.CreateFromLabOrderItem`, instansiasi `TrsBillKetType` mengisi keterangan tagihan dengan string kosong `""`. Hal ini melanggar `OC-LAB-TEST.md` §7 #1 dan AC-05 yang mewajibkan sistem mencantumkan nama tarif resmi disertai keterangan nama spesifik pemeriksaan laboratorium yang dipesan (`labOrderItem.LabTestName`). |
| **GAP-004** | **MINOR** | **Pengecekan Keaktifan Tarif Bersama di Tata Rekening Belum Terintegrasi pada Intake Pemesanan (EX-LTD-02).**<br>Pengecualian EX-LTD-02 mensyaratkan pemesanan diblokir jika tarif bersama dinonaktifkan di Tata Rekening. Saat ini validasi tarif ke `ITarifRepo` baru terjadi pada tahap `LabOrderChargeHandler`, belum dilakukan pada saat resolusi awal order di `LabTestResolutionService`. |

---

# 4. Open Questions

| ID | Question | Impact |
| :--- | :--- | :--- |
| **OQ-001** | Bagaimana strategi intake pemesanan pada `LabOrderCreateFromEmrCmd` dan `LabOrderCreateExternalPatientCmd` untuk mendukung skema tarif bersama tanpa merusak kompatibilitas klien yang sudah ada? Apakah `TestDefinitionId` dijadikan parameter opsional dengan mekanisme fallback otomatis (*hybrid resolution*), atau wajib diisi pada setiap pemesanan? | Menentukan kontrak API intake order laboratorium dan fleksibilitas integrasi dengan EMR. |
| **OQ-002** | Apakah `ILabTestDefinitionRepo` perlu menyediakan method `ListActiveByTarifId(string tarifId)` untuk mendeteksi apakah suatu tarif berstatus *single-test* atau *shared-tariff* saat resolusi pemesanan dijalankan? | Memengaruhi perancangan port repository dan DAL kueri data akses. |

---

# 5. Assumptions

| ID | Assumption |
| :--- | :--- |
| **ASM-001** | Skema tabel basis data SQL Server `BILRG_LabTestDefinition` tidak memiliki unique constraint pada `TarifId`, sehingga tidak diperlukan penambahan skrip migrasi DDL pada basis data untuk mendukung Many-to-One. |
| **ASM-002** | Sebagian besar tarif laboratorium saat ini masih bersifat 1-to-1, sedangkan skema tarif bersama (*shared tariff*) diterapkan secara bertahap pada panel tes terstandar (seperti elektrolit satuan atau kimia darah rutin). |
| **ASM-003** | Domain Tata Rekening (`TRK-BILLING`) mampu menerima dan menampilkan keterangan tindakan (`TrsBillKetType.Keterangan`) pada lembar rincian tagihan kasir dan struk pembayaran pasien. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
| :--- | :--- | :--- | :--- |
| **RISK-001** | Terjadinya kegagalan resolusi tes jika klien EMR lama hanya mengirimkan `TarifId` untuk pemeriksaan yang menggunakan tarif bersama. | Order gagal dibuat atau terjadi pengecualian sistem saat runtime intake. | Terapkan resolusi cerdas (*hybrid resolution*): jika `TarifId` hanya memiliki 1 tes aktif, otomatis resolve tes tersebut; jika memiliki > 1 tes aktif (shared tariff), wajibkan `TestDefinitionId` dengan pesan validasi yang jelas. |
| **RISK-002** | Inkonsistensi data jika pengelola laboratorium mendaftarkan tes baru dengan tarif bersama yang salah. | Pemeriksaan dipetakan ke tarif tindakan yang tidak sesuai kelas biayanya. | Tambahkan validasi konfirmasi pada antarmuka master dan validasi keberadaan tarif aktif di `TRK-TARIF` saat pendaftaran tes. |

---

# 7. Recommendations

## Option A: Pendekatan Resolusi Adaptif / Hibrida (*Hybrid Resolution* - Recommended)

1. **Penyesuaian Model Masukan Order**:
   - Perkaya `LabOrderTarifItemInput` dengan properti opsional `string? TestDefinitionId = null`.
2. **Pencabutan Invariant Pembatas Tarif Tunggal**:
   - Hapus pemanggilan `EnsureNoActiveTarifConflict` pada alur Create, Update, dan Activate di `LabTestDefinitionFeature`.
3. **Logika Resolusi Berjenjang (*Deterministic Resolution*)**:
   - Pada `LabTestResolutionService.ResolveByTarifItems`:
     - **Kasus 1**: Jika `TestDefinitionId` disediakan, muat definisi tes secara langsung via `_definitionRepo.LoadEntity(TestDefinitionId)`. Pastikan `definition.TarifId == input.TarifId` dan tes berstatus aktif.
     - **Kasus 2**: Jika `TestDefinitionId` tidak diisi: kueri seluruh tes aktif untuk tarif tersebut (`_definitionRepo.ListActiveByTarifId(input.TarifId)`).
       - Jika ditemukan **tepat 1 tes aktif**: gunakan tes tersebut secara otomatis (*backward-compatible* untuk tarif non-shared).
       - Jika ditemukan **lebih dari 1 tes aktif** (*shared tariff*): lemparkan exception informatif:
         `"SHARED_TARIF_AMBIGUITY: TarifId '{input.TarifId}' dikaitkan ke beberapa pemeriksaan laboratorium aktif. Spesifikasikan TestDefinitionId secara eksplisit."`
4. **Penyematan Keterangan Tes pada Penagihan**:
   - Perbarui `TrsBillFactory.CreateFromLabOrderItem` baris 134 menjadi:
     `var ketBilling = new TrsBillKetType(tarif.TarifName, labOrderItem.LabTestName, tarif.TarifId, 1, "");`.

### Keunggulan
- Sepenuhnya memenuhi kriteria `OC-LAB-TEST.md` (v1.1) AC-03 dan AC-05.
- Menjaga kompatibilitas penuh (*100% backward compatible*) untuk pemeriksaan yang menggunakan tarif tunggal (1-to-1).
- Mencegah ambiguitas pemesanan pada skema tarif bersama (*shared tariff*).
- Menampilkan rincian tagihan kasir yang transparan dan informatif bagi pasien.

### Kelemahan
- Memerlukan penambahan method kueri `ListActiveByTarifId` pada port repository dan DAL `LabTestDefinition`.

---

## Option B: Pendekatan Eksklusif Berbasis `TestDefinitionId` (*Strict TestDefinitionId*)

1. Mengubah seluruh model intake order `LabOrderTarifItemInput` menjadi `LabOrderTestItemInput(string TestDefinitionId, ...)` dan mewajibkan `TestDefinitionId`.
2. Resolusi tidak lagi menggunakan `TarifId` sebagai kunci pencarian awal, melainkan mengambil `TarifId` dari dalam `LabTestDefinitionModel`.

### Keunggulan
- Sangat sederhana secara konseptual karena order selalu mengacu langsung pada definisi tes.

### Kelemahan
- Merupakan *breaking change* pada kontrak API intake order laboratorium eksisting (`from-emr` dan `external-patient`).
- Klien pemanggil (seperti modul konsul EMR yang memilih tarif tindakan dari katalog billing) terpaksa dirombak untuk memilih tes laboratorium alih-alih tarif.

---

# 8. Gap Closure

*(Bagian ini mencatat status dan keputusan penutupan kesenjangan sebelum perencanaan dimulai)*

## GAP-001: Invariant Pembatasan Tarif Tunggal Menolak Shared Tariff
- **Status**: OPEN
- **Rekomendasi Keputusan**: Cabut pengecekan `EnsureNoActiveTarifConflict` dan izinkan pendaftaran/aktivasi multiple `LabTestDefinition` dengan `TarifId` yang sama sesuai AC-03.

## GAP-002: Resolusi Pemesanan Laboratorium Ambigu pada Shared Tariff
- **Status**: OPEN
- **Rekomendasi Keputusan**: Adopsi Opsi Rekomendasi A (Resolusi Hibrida): tambahkan `TestDefinitionId` opsional pada `LabOrderTarifItemInput`, dan dukung resolusi deterministik berbasis `TestDefinitionId` atau fallback tunggal.

## GAP-003: Keterangan Nama Spesifik Pemeriksaan Kosong pada Billing
- **Status**: OPEN
- **Rekomendasi Keputusan**: Pasang `labOrderItem.LabTestName` ke parameter `Keterangan` pada `TrsBillKetType` di `TrsBillFactory.CreateFromLabOrderItem`.

## GAP-004: Validasi Keaktifan Tarif Tata Rekening pada Pemesanan
- **Status**: OPEN
- **Rekomendasi Keputusan**: Tambahkan validasi keaktifan tarif ke `ITarifRepo` pada `LabTestResolutionService` saat order diproses guna memenuhi EX-LTD-02.

---

## OQ-001: Strategi Intake Pemesanan Shared Tariff
- **Status**: OPEN
- **Rekomendasi Jawaban**: Gunakan pendekatan hibrida (Opsi A) agar order tarif tunggal eksisting tidak terganggu, namun order tarif bersama terlindungi dari ambiguitas.

## OQ-002: Kebutuhan Kueri ListActiveByTarifId
- **Status**: OPEN
- **Rekomendasi Jawaban**: Tambahkan method `ListActiveByTarifId(string tarifId)` pada `ILabTestDefinitionRepo` dan `ILabTestDefinitionDal`.

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-REQUIRED**

## Rationale

Penyelarasan ini melibatkan modifikasi kontrak antar-kapabilitas (`LabTestDefinitionFeature` ↔ `LabOrderFeature` ↔ `TrsBillingFeature`), penambahan kueri baru pada data access layer, pencabutan domain invariant pada application service, serta penanganan resolusi deterministik pada perbatasan integrasi intake order. Oleh karena itu, diperlukan perancangan arsitektur teknis formal oleh Arsitek (`ica-architect`).

---

# 10. Planning Readiness

## Readiness Checklist

- [ ] All critical gaps resolved
- [ ] All required decisions recorded
- [ ] All blocking open questions resolved
- [ ] Architecture can be finalized or updated

## Status

**NOT-READY**

## Notes

Status kelayakan tetap `NOT-READY` selama diskusi keputusan penutupan GAP (`GAP-001` s/d `GAP-004`) dan pertanyaan terbuka (`OQ-001` s/d `OQ-002`) belum difinalisasi bersama tim/pengguna. Status `READY-FOR-PLANNING` hanya dapat diberikan oleh peran Arsitek (`ica-architect`) setelah seluruh keputusan penutupan gap disetujui.

---

# 11. References

Referenced artifacts:

- ISSUE: [`docs/contexts/lab/OC-LAB-TEST-ISSUE.md`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-ISSUE.md)
- OUTCOME: [`b21-myhosweb-system/outcomes/OC-LAB-TEST.md`](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) (v1.1)
- FEATURE: [`docs/contexts/lab/OC-LAB-TEST-FEATURE.md`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-FEATURE.md)
- RELATED OUTCOME: [`b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md`](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)

Referenced codebase locations:

- `src/bilreg/Bilreg.Application/LabContext/LabTestDefinitionFeature/LabTestDefinitionSupport.cs`
- `src/bilreg/Bilreg.Application/LabContext/LabTestDefinitionFeature/ILabTestDefinitionRepo.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabTestDefinitionFeature/LabTestDefinitionDal.cs`
- `src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/LabOrderTarifItemInput.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs`
- `src/bilreg/Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillFactory.cs`
