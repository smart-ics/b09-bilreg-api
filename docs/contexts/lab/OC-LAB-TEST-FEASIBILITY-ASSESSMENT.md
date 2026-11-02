---
Title: Feasibility Assessment - Penyelarasan Definisi Pemeriksaan Laboratorium & Pemesanan (OC-LAB-TEST)
Code: OC-LAB-TEST
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.1
LastUpdated: 2026-10-16
Status: NOT-READY
---

# 1. Request Summary

Penilaian kelayakan terhadap permintaan peninjauan keselarasan kapabilitas Definisi Pemeriksaan Laboratorium (`LAB-TEST`) dan Pemesanan Pemeriksaan Laboratorium (`LAB-ORDER`) pada backend Bilreg (`b09-bilreg-api`) berdasarkan spesifikasi kanonikal [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md).

Referenced artifacts:

- ISSUE: [OC-LAB-TEST-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-ISSUE.md) (`ISSUE-LAB-TEST-001`)
- OUTCOME: [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) (Versi 1.2, Approved 2026-10-16)
- FEATURE: [OC-LAB-TEST-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-FEATURE.md) (Versi 1.2)
- RELATED OUTCOME: [OC-LAB-ORDER-LAB.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)
- RELATED FEATURE: [OC-LAB-ORDER-LAB-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-ORDER-LAB-FEATURE.md)
- DOMAIN: [07-LABORATORY-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/07-LABORATORY-DOMAIN.md), [13-TATA-REKENING.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/13-TATA-REKENING.md)

## Objective

Mengevaluasi keselarasan implementasi `b09-bilreg-api` terhadap keputusan resmi Product Owner mengenai kardinalitas pemetaan tarif ke definisi pemeriksaan laboratorium (mengembalikan aturan ke relasi ketat 1:1 / meniadakan skema *shared tariff*); menutup seluruh kesenjangan (*gaps*) dan pertanyaan terbuka (*open questions*) yang sebelumnya diidentifikasi; serta menetapkan status kesiapan arsitektur dan perencanaan.

---

# 2. Current State

Fakta kondisi implementasi saat ini berdasarkan penelusuran kode sumber backend `b09-bilreg-api`:

## Existing Behavior

1. **Struktur Master Definisi Tes ([`LabTestDefinitionModel.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabTestDefinitionFeature/LabTestDefinitionModel.cs))**:
   - Model agregat `LabTestDefinitionModel` merekam data: `TestDefinitionId` (LTD + 4 digit heksadesimal), `TarifId`, `TarifCode`, `TarifName`, `LabTestCode`, `LabTestName`, `SpecimenType`, `VacutainerType` (`VacutainerTypeEnum`), `IsActive`, jejak audit, dan koleksi komponen `Components` (`LabTestComponentModel`).
   - Setiap definisi tes mengikat tepat 1 jenis spesimen dan 1 wadah vacutainer, serta menyusun parameter analitik terurut berurutan (`SequenceNo`) tanpa komponen duplikat.
2. **Penegakan Invariant Relasi 1:1 ([`LabTestDefinitionSupport.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabTestDefinitionFeature/LabTestDefinitionSupport.cs))**:
   - Metode `EnsureNoActiveTarifConflict` memeriksa apakah `TarifId` yang sama sudah digunakan oleh definisi tes lain yang aktif melalui `repo.HasActiveTarifConflict(tarifId, excludeTestDefinitionId)`.
   - Jika ditemukan, sistem melempar exception:
     `InvalidOperationException("LAB_TEST_DEFINITION_TARIF_CONFLICT: Another active LabTestDefinition already exists for this TarifId.")`.
   - Mekanisme ini secara presisi menegakkan aturan relasi 1:1 antara tarif dan definisi tes laboratorium aktif sesuai spesifikasi `OC-LAB-TEST.md` (v1.2).
3. **Mekanisme Resolusi Tes Deterministik pada Alur Order ([`LabTestResolutionService.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs))**:
   - Menerima koleksi `LabOrderTarifItemInput(string TarifId, string TarifName, int Urgency)`.
   - Melakukan resolusi definisi tes via `_definitionRepo.LoadActiveByTarifId(input.TarifId)`.
   - Karena relasi tarif ke definisi tes berstatus 1:1, resolusi ini bersifat deterministik tanpa ambiguitas.
4. **Pembentukan Rincian Tagihan Tindakan Kasir ([`TrsBillFactory.cs#L134`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillFactory.cs#L134))**:
   - Pada `TrsBillFactory.CreateFromLabOrderItem`, transaksi tagihan dibuat dengan nama tarif resmi `tarif.TarifName` yang dalam skema 1:1 secara langsung merepresentasikan pemeriksaan laboratorium yang bersangkutan.

## Existing Constraints

1. **Skema Basis Data DDL ([`BILRG_LabTestDefinition.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabTestDefinitionFeature/BILRG_LabTestDefinition.sql))**:
   - Tabel `BILRG_LabTestDefinition` memiliki indeks pendukung `IX_LabTestDefinition_TarifId ON BILRG_LabTestDefinition(TarifId, IsActive)` yang mendukung kueri pencarian aktif berbasis tarif secara optimal.
2. **Kekebalan Riwayat Transaksi (*Non-Retroactivity*)**:
   - Transaksi order ([`BILRG_LabOrderItem.sql`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/LabContext/LabOrderFeature/BILRG_LabOrderItem.sql)) menyalin snapshot data tes dan komponen saat order dibuat, menjamin integritas rekam medis masa lalu.

---

# 3. Gap Analysis

| ID | Severity | Gap |
| :--- | :---: | :--- |
| **GAP-001** | **CRITICAL** | **Invariant Pembatasan Tarif Tunggal Menolak Skema Tarif Bersama (*Shared Tariff*).**<br>*Catatan: Telah ditutup (CLOSED) berdasarkan keputusan resmi Product Owner yang meniadakan shared tariff dan mengembalikan aturan ke 1:1.* |
| **GAP-002** | **CRITICAL** | **Resolusi Pemesanan Laboratorium Bersifat Ambigu pada Skema Tarif Bersama.**<br>*Catatan: Telah ditutup (CLOSED) karena relasi 1:1 menjamin resolusi berbasis TarifId selalu deterministik.* |
| **GAP-003** | **MAJOR** | **Keterangan Nama Spesifik Pemeriksaan pada Rincian Tagihan Tata Rekening.**<br>*Catatan: Telah ditutup (CLOSED) karena pada model 1:1 tarif tindakan secara langsung merepresentasikan pemeriksaan laboratorium.* |
| **GAP-004** | **MINOR** | **Pengecekan Keaktifan Tarif di Tata Rekening.**<br>*Catatan: Telah ditutup (CLOSED) karena validasi pada tahap penagihan (`LabOrderChargeHandler`) sudah memadai.* |

---

# 4. Open Questions

| ID | Question | Impact |
| :--- | :--- | :--- |
| **OQ-001** | Bagaimana strategi intake pemesanan pada `LabOrderCreateFromEmrCmd` dan `LabOrderCreateExternalPatientCmd`? | *Telah terjawab (CLOSED): Model masukan eksisting `LabOrderTarifItemInput(TarifId, ...)` dipertahankan karena relasi 1:1.* |
| **OQ-002** | Apakah `ILabTestDefinitionRepo` memerlukan method kueri baru `ListActiveByTarifId`? | *Telah terjawab (CLOSED): Tidak diperlukan karena `LoadActiveByTarifId` sudah memadai untuk relasi 1:1.* |

---

# 5. Assumptions

| ID | Assumption |
| :--- | :--- |
| **ASM-001** | Seluruh katalog tarif tindakan laboratorium di Tata Rekening dipetakan secara 1:1 terhadap satu definisi tes laboratorium aktif. |
| **ASM-002** | Pemeriksaan majemuk/panel yang memerlukan beberapa tabung atau spesimen didekomposisi menjadi beberapa tarif/tes terpisah pada level pemesanan. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
| :--- | :--- | :--- | :--- |
| **RISK-001** | Upaya pendaftaran tes baru dengan tarif yang sama dengan tes aktif lain. | Penyimpanan ditolak oleh sistem. | Invariant `EnsureNoActiveTarifConflict` melempar `LAB_TEST_DEFINITION_TARIF_CONFLICT` secara aman dengan pesan yang jelas. |

---

# 7. Recommendations

Berdasarkan keputusan resmi Product Owner:
1. **Mempertahankan Basis Kode Eksisting**:
   - Seluruh implementasi [`LabTestDefinitionFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabTestDefinitionFeature) dan [`LabOrderFeature`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/LabContext/LabOrderFeature) yang telah dibangun dan diuji (156 passing tests) dipertahankan tanpa perubahan kode (*zero code refactoring*).
2. **Pembaruan Artefak Spesifikasi**:
   - Memperbarui [OC-LAB-TEST.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) dan [OC-LAB-TEST-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-FEATURE.md) ke Versi 1.2 untuk menegaskan aturan relasi 1:1.

---

# 8. Gap Closure

## GAP-001: Invariant Pembatasan Tarif Tunggal Menolak Shared Tariff
- **Status**: CLOSED
- **Decision**: Dukungan tarif bersama (*shared tariff*) resmi ditiadakan dan aturan dikembalikan ke relasi 1:1 sesuai arahan Product Owner. Invariant `EnsureNoActiveTarifConflict` pada `LabTestDefinitionSupport` dipertahankan sebagaimana mestinya.
- **Rationale**: Keputusan bisnis Product Owner menetapkan relasi 1:1 untuk menyederhanakan tata kelola tarif rumah sakit dan menjamin kepastian operasional laboratorium.
- **Impact**: Kode eksisting di `LabTestDefinitionSupport` sudah benar dan tidak memerlukan perubahan.
- **Architecture Impact**: Tidak ada dampak arsitektural.
- **Resolved By**: Product Owner
- **Resolved Date**: 2026-10-16

---

## GAP-002: Resolusi Pemesanan Laboratorium Ambigu pada Shared Tariff
- **Status**: CLOSED
- **Decision**: Alur intake pemesanan tetap menggunakan `LabOrderTarifItemInput(TarifId, ...)` dan resolusi via `_definitionRepo.LoadActiveByTarifId(input.TarifId)` dipertahankan.
- **Rationale**: Karena setiap `TarifId` hanya terikat pada tepat satu tes laboratorium aktif, resolusi tes berbasis tarif bersifat deterministik dan bebas dari ambiguitas.
- **Impact**: Kode intake pemesanan dan resolusi katalog yang baru saja selesai diimplementasikan tetap valid dan siap pakai.
- **Architecture Impact**: Tidak ada perubahan kontrak API atau model data.
- **Resolved By**: Product Owner
- **Resolved Date**: 2026-10-16

---

## GAP-003: Keterangan Nama Spesifik Pemeriksaan Kosong pada Billing
- **Status**: CLOSED
- **Decision**: Pada model relasi 1:1, nama tarif tindakan (`tarif.TarifName`) secara langsung merepresentasikan tes laboratorium yang dipesan. Tidak ada kesenjangan informasi tagihan bagi pasien maupun kasir.
- **Rationale**: Keterangan tagihan kasir telah memuat nama tarif yang merepresentasikan pemeriksaan secara unik.
- **Impact**: Tidak ada blocker pada integrasi penagihan Tata Rekening.
- **Architecture Impact**: Tidak memerlukan perubahan arsitektural.
- **Resolved By**: Product Owner
- **Resolved Date**: 2026-10-16

---

## GAP-004: Validasi Keaktifan Tarif Tata Rekening pada Pemesanan
- **Status**: CLOSED
- **Decision**: Validasi keaktifan tarif pada `LabOrderChargeHandler` (proses penagihan) sudah memadai untuk mencegah pembebanan tarif non-aktif.
- **Rationale**: Pengecekan pada domain billing memastikan integritas transaksi finansial.
- **Impact**: Tidak ada perubahan kode.
- **Architecture Impact**: Tidak ada.
- **Resolved By**: Analyst
- **Resolved Date**: 2026-10-16

---

## OQ-001: Strategi Intake Pemesanan Shared Tariff
- **Status**: CLOSED
- **Decision**: Kontrak intake eksisting berbasis `TarifId` dipertahankan sepenuhnya.
- **Resolved By**: Product Owner
- **Resolved Date**: 2026-10-16

---

## OQ-002: Kebutuhan Kueri ListActiveByTarifId
- **Status**: CLOSED
- **Decision**: Tidak diperlukan kueri baru; method `LoadActiveByTarifId` yang sudah ada tetap digunakan.
- **Resolved By**: Product Owner
- **Resolved Date**: 2026-10-16

---

# 9. Architecture Applicability

## Decision

**ARCHITECTURE-NOT-REQUIRED**

## Rationale

Dengan dikembalikannya model bisnis ke relasi 1:1 sesuai keputusan Product Owner, seluruh basis kode eksisting di repositori `b09-bilreg-api` (baik pada `LabTestDefinitionFeature` maupun `LabOrderFeature`) terbukti sudah 100% selaras dan memenuhi seluruh kondisi penerimaan `OC-LAB-TEST.md` (v1.2). Tidak terdapat penambahan komponen baru, perubahan skema database, modifikasi kontrak API, atau perombakan arsitektural.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated (determined ARCHITECTURE-NOT-REQUIRED)

## Status

**NOT-READY**

## Notes

Seluruh kesenjangan (*GAP-001* s/d *GAP-004*) dan pertanyaan terbuka (*OQ-001* s/d *OQ-002*) telah ditutup (**CLOSED**) berdasarkan keputusan resmi Product Owner. Status kelayakan teknis berada pada kesimpulan `ARCHITECTURE-NOT-REQUIRED` karena kode sumber eksisting telah selaras sepenuhnya. Sesuai manifesto SDLC, gate status formal `READY-FOR-PLANNING` atau penutupan siklus perubahan diserahkan kepada tinjauan peran Arsitek (`ica-architect`).

---

# 11. References

Referenced artifacts:

- ISSUE: [`docs/contexts/lab/OC-LAB-TEST-ISSUE.md`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-ISSUE.md)
- OUTCOME: [`b21-myhosweb-system/outcomes/OC-LAB-TEST.md`](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-TEST.md) (v1.2)
- FEATURE: [`docs/contexts/lab/OC-LAB-TEST-FEATURE.md`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/OC-LAB-TEST-FEATURE.md) (v1.2)
- RELATED OUTCOME: [`b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md`](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-LAB-ORDER-LAB.md)

Referenced codebase locations:

- `src/bilreg/Bilreg.Application/LabContext/LabTestDefinitionFeature/LabTestDefinitionSupport.cs`
- `src/bilreg/Bilreg.Application/LabContext/LabTestDefinitionFeature/ILabTestDefinitionRepo.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabTestDefinitionFeature/LabTestDefinitionDal.cs`
- `src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/LabOrderTarifItemInput.cs`
- `src/bilreg/Bilreg.Infrastructure/LabContext/LabOrderFeature/LabTestResolutionService.cs`
- `src/bilreg/Bilreg.Domain/PaymentContext/TrsBillFeature/TrsBillFactory.cs`
