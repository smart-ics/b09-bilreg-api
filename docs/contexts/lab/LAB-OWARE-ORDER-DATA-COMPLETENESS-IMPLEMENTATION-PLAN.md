---
Title: Implementation Plan Pengayaan Kelengkapan Data Order Laboratorium untuk Integrasi LIS
Code: LAB-OWARE-ORDER-DATA-COMPLETENESS-IMPLEMENTATION-PLAN
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-10-30
Status: NOT-STARTED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan pengayaan data order laboratorium lintas-service antara **`b09-bilreg-api`** dan **`a046_oware_ordermiddlewareapi`** untuk menyediakan kelengkapan data demografi pasien (NIK, alamat, kota, telepon, tempat lahir), data registrasi/penjamin (`JenisReg`, tipe jaminan, nomor polis), dan ruangan rawat inap (`BangsalName`, `BedName`) yang dibutuhkan oleh sistem LIS eksternal.

Planning Mode: **FEATURE-PLANNING**

Referenced artifacts:
- ARCHITECTURE: [LAB-OWARE-ORDER-DATA-COMPLETENESS-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-DATA-COMPLETENESS-ARCHITECTURE.md)
- FEASIBILITY-ASSESSMENT: [LAB-OWARE-ORDER-DATA-COMPLETENESS-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-DATA-COMPLETENESS-FEASIBILITY-ASSESSMENT.md)
- ISSUE: [LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE.md)

Architecture Applicability: **`ARCHITECTURE-REQUIRED`**

---

# 2. Planning Scope

Rencana implementasi ini mencakup 2 phase yang mencakup masing-masing repository:
1. **Phase 1 (`b09-bilreg-api`)**: Memperkaya kontrak DTO `LabOrderGetResponse`, mendaftarkan dependensi `IPasienRepo` pada `LabOrderGetHandler`, mengimplementasikan penarikan data demografi dan bangsal dengan *graceful fallback*, dan memverifikasi unit test di `Bilreg.Test`.
2. **Phase 2 (`a046_oware_ordermiddlewareapi`)**: Memperbarui DTO `BilregLabOrderResponseDto`, mengimplementasikan pemetaan data dan translasi `JenisReg` ke `JenisInap` ("OP" vs "IP") pada `OrderLabBilregBuilder`, dan memverifikasi unit test di `Oware.Test`.

---

# 3. Dependencies

- **Eksternal**: Tidak ada dependensi ke library pihak ketiga baru (hanya memanfaatkan pustaka dan dependensi yang sudah ada).
- **Antar-Slice**:
  - `P1-S01`: Tidak memiliki dependensi (`Depends On: None`).
  - `P2-S02`: Bergantung pada pemenuhan kontrak DTO di `P1-S01` (`Depends On: P1-S01`).

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| **P1** | NOT-STARTED | NOT-REVIEWED | 0/1 |
| **P2** | NOT-STARTED | NOT-REVIEWED | 0/1 |

- **Plan Status**: `NOT-STARTED`
- **Execution Approval**: **`APPROVED`**

---

# 5. Phases

## P1 - Backend Bilreg API: Provider Contract & Enrichment Handler

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

---

### P1-S01

**Title**: Pengayaan `LabOrderGetResponse` dan `LabOrderGetHandler` dengan Data Demografi Pasien dan Ruang Rawat

**Implementation Status**: NOT-STARTED  
**Review Status**: NOT-REVIEWED

**Objective**:
1. Memperluas record `LabOrderGetResponse` pada [`LabOrderGetQuery.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs) dengan menambahkan properti opsional berikut di akhir parameter (dengan nilai default `""`):
   - `Nik = ""`
   - `Alamat = ""`
   - `Kota = ""`
   - `TempatLahir = ""`
   - `PhoneNo = ""`
   - `JenisReg = ""`
   - `TipeJaminanId = ""`
   - `TipeJaminanName = ""`
   - `PesertaJaminanId = ""`
   - `BangsalId = ""`
   - `BangsalName = ""`
   - `BedName = ""`
2. Menginjeksi `IPasienRepo? pasienRepo = null` ke konstruktor `LabOrderGetHandler`.
3. Pada metode `Handle`:
   - Jika `order.Patient.PatientId` tidak kosong, panggil `_pasienRepo.LoadEntity(PasienModel.Key(patientId))` untuk memuat `PasienModel`. Ambil `Nik = pasien.Ktp.Nik`, `Alamat` (gabungan `Alamat.Alamat`, `Rt`, `Rw`, `Kelurahan.NamaKelurahan`), `Kota = pasien.Kelurahan.Kota.KotaName` (atau fallback alamat), `TempatLahir = pasien.TempatLahir`, dan `PhoneNo` dari `pasien.ListContact` (prioritaskan `Mobile` atau `Phone`). Jika tidak ditemukan atau null, fallback ke `""`.
   - Menggunakan `_regRepo` yang sudah terinjeksi: ambil `JenisReg = reg.JenisReg.ToString()`, `TipeJaminanId = reg.TipeJaminan.TipeJaminanId`, `TipeJaminanName = reg.TipeJaminan.TipeJaminanName`, `PesertaJaminanId = reg.Polis.PolisNo` (atau `reg.Eligibility.SepNo`), `BangsalId = reg.Bangsal.BangsalId`, `BangsalName = reg.Bangsal.BangsalName`, dan `BedName = reg.Bangsal.BangsalName`. Jika tidak ditemukan, fallback ke `""`.
4. Memperbarui unit test pada [`LabOrderGetHandlerTest.cs`](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Test/LabContext/LabOrderFeature/LabOrderGetHandlerTest.cs) untuk memvalidasi pemetaan seluruh field baru pada kondisi pasien terdaftar maupun fallback pasien eksternal.

**Depends On**: None

**Repository**: `b09-bilreg-api`

**Completion Criteria**:
- `b09-bilreg-api.sln` build berhasil dengan 0 Errors.
- Seluruh unit test pada `Bilreg.Test` yang relevan (`LabOrderGetHandlerTest` dan test suite `LabOrderFeature`) lulus 100%.

**Notes**:
- Pastikan parameter baru diletakkan di akhir record `LabOrderGetResponse` dengan default value agar backward-compatible.

---

## P2 - Backend OWARE API: Consumer DTO & Builder Translation

Implementation Status: NOT-STARTED  
Review Status: NOT-REVIEWED

---

### P2-S02

**Title**: Sinkronisasi `BilregLabOrderResponseDto` dan Pemetaan LIS pada `OrderLabBilregBuilder`

**Implementation Status**: NOT-STARTED  
**Review Status**: NOT-REVIEWED

**Objective**:
1. Memperbarui DTO konsumen [`BilregLabOrderResponseDto.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/DTOs/BilregLabOrderResponseDto.cs) dengan properti:
   - `public string Nik { get; set; } = string.Empty;`
   - `public string Address { get; set; } = string.Empty;`
   - `public string City { get; set; } = string.Empty;`
   - `public string BirthPlace { get; set; } = string.Empty;`
   - `public string PhoneNo { get; set; } = string.Empty;`
   - `public string JenisReg { get; set; } = string.Empty;`
   - `public string TipeJaminanId { get; set; } = string.Empty;`
   - `public string TipeJaminanName { get; set; } = string.Empty;`
   - `public string PesertaJaminanId { get; set; } = string.Empty;`
   - `public string BangsalId { get; set; } = string.Empty;`
   - `public string BangsalName { get; set; } = string.Empty;`
   - `public string BedName { get; set; } = string.Empty;`
2. Memperbarui [`OrderLabBilregBuilder.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Workers/Creators/OrderLabBilregBuilder.cs):
   - Memetakan field DTO ke `OrderLabModel`:
     - `order.Nik = dto.Nik ?? string.Empty;`
     - `order.Address = dto.Address ?? string.Empty;`
     - `order.City = dto.City ?? string.Empty;`
     - `order.BirthPlace = dto.BirthPlace ?? string.Empty;`
     - `order.PhoneNo = dto.PhoneNo ?? string.Empty;`
     - `order.TipeJaminanId = dto.TipeJaminanId ?? string.Empty;`
     - `order.TipeJaminanName = dto.TipeJaminanName ?? string.Empty;`
     - `order.PesertaJaminanId = dto.PesertaJaminanId ?? string.Empty;`
     - `order.BangsalId = dto.BangsalId ?? string.Empty;`
     - `order.BangsalName = dto.BangsalName ?? string.Empty;`
     - `order.BedName = !string.IsNullOrWhiteSpace(dto.BedName) ? dto.BedName : (dto.BangsalName ?? string.Empty);`
   - Translasi `JenisInap`:
     - Jika `dto.JenisReg` sama dengan `"RegInap"` atau bernilai `"1"` -> `order.JenisInap = "IP"`.
     - Selain itu (Rawat Jalan / IGD / External) -> `order.JenisInap = "OP"`.
3. Memperbarui unit test pada `Oware.Test`:
   - [`OrderLabBilregBuilderTest.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Test/OrderLabBilregBuilderTest.cs) untuk memvalidasi translasi `JenisInap` ("OP" vs "IP") dan kelengkapan field demografi/penjamin/ruangan.
   - [`BilregLabOrderClientTest.cs`](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Test/BilregLabOrderClientTest.cs) untuk memvalidasi deserialisasi JSON payload yang diperkaya.

**Depends On**: P1-S01

**Repository**: `a046_oware_ordermiddlewareapi`

**Completion Criteria**:
- Solusi `Oware` build berhasil dengan 0 Errors.
- Seluruh targeted unit tests di `Oware.Test` lulus 100%.

**Notes**:
- Pastikan `RemoveNull()` pada `OrderLabModel` tetap terpanggil untuk menjaga kebersihan data.

---

# 6. Change Log

- **2026-10-30**: Initial creation of Implementation Plan. Approved by ica-architect.
