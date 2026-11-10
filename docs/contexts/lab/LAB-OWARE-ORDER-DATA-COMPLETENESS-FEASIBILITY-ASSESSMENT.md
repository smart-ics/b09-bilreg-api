---
Title: Feasibility Assessment Pengayaan Kelengkapan Data Order Laboratorium untuk Integrasi LIS
Code: LAB-OWARE-ORDER-DATA-COMPLETENESS-FEASIBILITY-ASSESSMENT
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-10-30
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Pengayaan data order laboratorium pada payload respons endpoint Bilreg `GET api/LabContext/LabOrderFeature/{orderId}` agar menyediakan informasi demografis, registrasi, penjamin, dan lokasi ruang perawatan (rawat inap) yang dibutuhkan oleh sistem Laboratory Information System (LIS) eksternal melalui middleware OWARE.

Referenced artifacts:

- ISSUE: [LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE.md)
- DOMAIN: [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- FEASIBILITY (Fase 1): [LAB-OWARE-ORDER-INTEGRATION-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-INTEGRATION-FEASIBILITY-ASSESSMENT.md)
- ARCHITECTURE (Fase 1): [LAB-OWARE-ORDER-INTEGRATION-ARCHITECTURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-INTEGRATION-ARCHITECTURE.md)

## Objective

1. Mengidentifikasi seluruh atribut klinis, demografis, dan administratif yang diwajibkan oleh vendor LIS (Sysmex, HCLAB, Softmedix, Prolims, Wynacom, dll.) pada middleware OWARE.
2. Memetakan ketersediaan sumber data tersebut pada bounded context Bilreg (`AdmisiContext/RegFeature`, `PasienContext/PasienFeature`, `BedUsageContext/PakaiBedFeature`).
3. Menganalisis gap kontrak DTO lintas-service antara `b09-bilreg-api` dan `a046_oware_ordermiddlewareapi`.
4. Merumuskan opsi solusi pengayaan data yang aman, performan, dan backward-compatible.

---

# 2. Current State

## Existing Behavior

1. **Alur Ingestion Saat Ini**:
   - Background worker di Bilreg mengirim order ke OWARE melalui `POST /api/order/lab/bilreg` dengan payload `{ "orderId": "..." }`.
   - Handler di OWARE (`CreateOrderLabFromBilregHandler.cs`) memanggil kembali endpoint Bilreg: `GET api/LabContext/LabOrderFeature/{orderId}` via `BilregLabOrderClient.cs`.
   - DTO respons yang diterima OWARE adalah `BilregLabOrderResponseDto`.
   - `OrderLabBilregBuilder.cs` menyusun entitas `OrderLabModel` dan menyimpannya ke database OWARE sebelum mengirimkan data ke LIS via `OrderLabHubService`.

2. **Keterbatasan Payload Respons Bilreg (`LabOrderGetResponse`)**:
   - Endpoint Bilreg `GET api/LabContext/LabOrderFeature/{orderId}` ditangani oleh `LabOrderGetHandler.cs`.
   - Data pasien yang dikembalikan saat ini hanya bersumber dari `order.Patient` (`PatientSnapshotType`):
     - `PatientId`, `PatientName`, `BirthDate`, `Gender`, `AgeAtOrder`.
   - Data administratif, demografi detail, dan kamar perawatan **belum dimuat sama sekali**:
     - Data Pasien: NIK, alamat lengkap, kota/kabupaten, tempat lahir, dan nomor telepon tidak ada pada respons.
     - Data Registrasi: `JenisReg` / tipe kunjungan, data penjamin/asuransi (`TipeJaminanId`, `TipeJaminanName`), dan nomor kepesertaan/polis (`PesertaJaminanId`) tidak ada.
     - Data Rawat Inap: nomor bed, nama bed, kamar, dan bangsal perawatan tidak ada.

3. **Keterbatasan Pemetaan di OWARE (`OrderLabBilregBuilder.cs`)**:
   - Pada baris 60–88 `OrderLabBilregBuilder.cs`, properti-properti berikut diisi string kosong (`string.Empty`):
     - `JenisInap = string.Empty`
     - `TipeJaminanId = string.Empty`, `TipeJaminanName = string.Empty`, `PesertaJaminanId = string.Empty`
     - `BirthPlace = string.Empty`, `Nik = string.Empty`, `PhoneNo = string.Empty`, `Address = string.Empty`, `City = string.Empty`
     - `BedId = string.Empty`, `BedName = string.Empty`, `KamarId = string.Empty`, `KamarName = string.Empty`, `BangsalId = string.Empty`, `BangsalName = string.Empty`

4. **Dampak pada LIS Eksternal**:
   - Adapter LIS di OWARE (misal `OrderLabTarifHclabService`, `OrderLabTarifSysmexService`, `OrderLabTarifSoftmedixService`) mengevaluasi `JenisInap` untuk menentukan tipe pasien:
     - `var patType = req.JenisInap.Trim() == "" ? "OP" : "IP";`
     - Akibatnya, seluruh order dari Bilreg (termasuk pasien Rawat Inap) diproses sebagai **Outpatient (OP / Rawat Jalan)**.
     - NIK dikirim sebagai `"-"`, alamat kosong, dan pada Softmedix/Wynacom nomor bed dikirim sebagai default `"0000"` sehingga menyulitkan perawat/analis laboratorium dalam melacak lokasi pasien rawat inap.

## Existing Constraints

1. **Prinsip Backward-Compatibility**: Kontrak `GET api/LabContext/LabOrderFeature/{orderId}` sudah digunakan oleh Frontend HIS Web (`c012_myhospital_web`). Penambahan properti baru pada `LabOrderGetResponse` tidak boleh memecahkan *consumer* eksisting.
2. **Kemandirian Bounded Context**: Context Lab di Bilreg tidak boleh memiliki *hard-coupling* yang berlebihan terhadap internal domain context lain; pemanggilan data registrasi dan pasien harus melalui kontrak/abstraksi repo yang sah (`IRegRepo`, `IPasienRepo`, `IPakaiBedRepo`).
3. **Standarisasi Ubiquitous Language**: Format representasi jenis kunjungan di Bilreg berbasis enum (`JenisRegEnum`: `RegJalan`, `RegInap`, `RegIgd`), sedangkan LIS umumnya menggunakan format 2 karakter ("OP" / "IP" atau "RJ" / "RI"). Translasi ke standar LIS harus menjadi tanggung jawab builder di middleware OWARE, bukan mencemari domain Bilreg.

---

# 3. Gap Analysis

| ID | Severity | Gap |
|------|------|------|
| **GAP-001** | CRITICAL | **Kontrak Response DTO Bilreg (`LabOrderGetResponse`) Belum Memuat Data Demografi Pasien dan Ruang Rawat**: Payload `LabOrderGetResponse` belum memiliki properti untuk NIK, alamat lengkap, kota, tempat lahir, nomor telepon, jenis registrasi/kunjungan, penjamin, nomor peserta/polis, serta informasi bed/kamar/bangsal. |
| **GAP-002** | CRITICAL | **`LabOrderGetHandler` Belum Melakukan Enrichment Data Pasien, Registrasi, dan Bed**: Handler saat ini hanya membaca snapshot order internal. Meskipun `IRegRepo` sudah terinjeksi (digunakan untuk tarif), handler belum memetakan properti registrasi, belum menginjeksi `IPasienRepo` untuk demografi pasien, dan belum menginjeksi repo bed untuk pasien rawat inap. |
| **GAP-003** | MAJOR | **Kontrak Konsumen di OWARE (`BilregLabOrderResponseDto`) Belum Sinkron**: Class DTO penerima di OWARE belum memiliki definisi field baru, sehingga payload pengayaan dari Bilreg tidak akan terdeserialisasi jika DTO ini tidak diperbarui. |
| **GAP-004** | MAJOR | **`OrderLabBilregBuilder` di OWARE Masih Mengisi Atribut dengan String Kosong**: Builder belum memetakan data hasil enrichment ke properti `OrderLabModel` dan belum memiliki logika penentuan `JenisInap` ("OP" vs "IP") berbasis jenis registrasi. |

---

# 4. Open Questions

| ID | Question | Impact |
|------|------|------|
| **OQ-001** | Bagaimana format penyajian data registrasi/jenis kunjungan dari Bilreg ke OWARE? Apakah Bilreg mengirim string deskriptif/kode internal (`"RegJalan"`, `"RegInap"`, `"RegIgd"`), atau integer kode enum? | Menentukan kontrak API yang bersih antara Bilreg dan OWARE serta cara OWARE mentranslasikannya ke format vendor LIS ("OP" / "IP"). |
| **OQ-002** | Bagaimana strategi penarikan data Bed/Kamar untuk pasien Rawat Inap pada `LabOrderGetHandler`? Apakah mengambil alokasi bed aktif dari `IPakaiBedRepo` berdasarkan `RegId`, atau snapshot dari `RegModel`? | Memastikan keakuratan nomor bed dan bangsal jika pasien rawat inap pernah mengalami perpindahan tempat tidur/kamar (*transfer*). |
| **OQ-003** | Bagaimana penanganan order laboratorium eksternal (`LabOrderSourceEnum.External`) yang tidak memiliki registrasi rumah sakit (`RegId` kosong) atau master pasien lokal? | Menentukan mekanisme fallback yang aman agar handler tidak mengalami *NullReferenceException* saat pasien tidak terdaftar di `ta_registrasi` atau `ta_pasien`. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| **ASM-001** | Penambahan field opsional pada JSON respons `GET api/LabContext/LabOrderFeature/{orderId}` tidak memengaruhi fungsionalitas Frontend HIS Web (`c012_myhospital_web`). |
| **ASM-002** | Master data pasien di Bilreg (`ta_pasien`) menyimpan NIK, alamat (jalan, RT/RW, kelurahan, kota), tempat lahir, dan nomor telepon/HP secara persisten melalui entitas `PasienModel`. |
| **ASM-003** | LIS eksternal mengharapkan nilai `JenisInap` berupa `"OP"` untuk Rawat Jalan & IGD, serta `"IP"` untuk Rawat Inap. |
| **ASM-004** | Nomor kepesertaan jaminan diambil dari `Polis.PolisNo` atau nomor SEP/Eligibility jika penjamin adalah BPJS Kesehatan. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| **RISK-001** | **Overhead Performa pada Query Detail Order**: Penarikan data dari `IRegRepo`, `IPasienRepo`, dan `IPakaiBedRepo` secara beruntun dapat meningkatkan latensi `GET /api/LabContext/LabOrderFeature/{orderId}`. | MAJOR | Gunakan query by primary key (`PasienModel.Key`, `RegModel.Key`). Pemanggilan hanya dilakukan jika ID terkait tersedia (`PatientId` / `RegId` tidak kosong). Jika perlu, gunakan data transfer object/DAL yang membaca join tabel secara langsung. |
| **RISK-002** | **Inkonsistensi Status Bed pada Pasien Rawat Inap**: Pasien dipindahkan ke bed lain setelah order lab dibuat, sehingga bed saat order berbeda dengan bed saat pengambilan spesimen. | MINOR | Ambil data bed yang berstatus aktif saat query detail order dieksekusi agar informasi lokasi pasien yang terkirim ke LIS adalah lokasi terkini pasien. |
| **RISK-003** | **Data Demografi Pasien Belum Lengkap di SIMRS**: Pasien darurat/IGD mungkin belum memiliki NIK atau alamat saat order lab pertama dibuat. | MINOR | Nilai string fallback yang aman (default `"-"` atau empty string) pada builder OWARE agar payload LIS tetap valid secara sintaksis. |

---

# 7. Recommendations

## Option A (Recommended): Pengayaan Kontrak Tunggal pada `LabOrderGetResponse` (Single-Payload Enrichment)

Bilreg memperkaya respons `GET api/LabContext/LabOrderFeature/{orderId}` dengan menambahkan properti demografi, penjamin, dan ruang rawat langsung pada `LabOrderGetResponse`.

```
[Bilreg: LabOrderGetHandler]
      │
      ├── 1. Load LabOrderModel (order data)
      ├── 2. Load RegModel via IRegRepo (JenisReg, TipeJaminan, Polis, Bangsal)
      ├── 3. Load PasienModel via IPasienRepo (Nik, TempatLahir, Alamat, NoTelepon)
      └── 4. Load PakaiBedModel via IPakaiBedRepo (BedName, KamarName jika Ranap)
      │
      ▼
[LabOrderGetResponse JSON Payload] ──► [OWARE: BilregLabOrderClient]
                                                │
                                                ▼
                                    [OrderLabBilregBuilder]
                                    (Mapping ke OrderLabModel & format LIS)
```

### Advantages
- **Efisien**: OWARE hanya melakukan **1 kali HTTP request** untuk mendapatkan seluruh kelengkapan order lab.
- **Konsisten**: Mengikuti arsitektur eksisting tanpa menambah endpoint baru.
- **Isolasi**: Format internal Bilreg tetap bersih; transformasi ke representasi LIS dilakukan di layer adapter OWARE.

### Disadvantages
- `LabOrderGetHandler` di Bilreg memerlukan dependensi baru (`IPasienRepo`, `IPakaiBedRepo` atau query reader terpadu).

---

## Option B: Multiple Endpoint Ingestion (Chatty HTTP Calls)

OWARE memanggil endpoint terpisah di Bilreg: `GET /api/LabOrder/{id}`, lalu `GET /api/Pasien/{id}`, lalu `GET /api/Reg/{id}`.

### Advantages
- Masing-masing handler di Bilreg tetap terpisah tanpa coupling data.

### Disadvantages
- Menghasilkan 3 round-trip HTTP request untuk setiap kali order diproses, meningkatkan latensi hingga 300% dan memperbesar risiko timeout/kegagalan parsial jaringan.
- Tidak direkomendasikan untuk integrasi middleware infrastruktur berkecepatan tinggi.

---

# 8. Gap Closure

## GAP-001
- **Status**: CLOSED
- **Decision**: Menerapkan strategi *Single-Payload Enrichment* dengan menambahkan field demografi pasien (`Nik`, `Alamat`, `Kota`, `TempatLahir`, `PhoneNo`), data registrasi (`JenisReg`, `TipeJaminanId`, `TipeJaminanName`, `PesertaJaminanId`), dan data ruangan (`BangsalId`, `BangsalName`, `BedName`) langsung pada DTO response `LabOrderGetResponse` di Bilreg.
- **Rationale**: Menghindari *chatty requests* dan latensi jaringan tinggi dari OWARE ke Bilreg, serta menjamin seluruh data yang dibutuhkan LIS tersedia dalam satu kali panggilan HTTP.
- **Impact**: Kontrak `LabOrderGetResponse` bertambah secara backward-compatible.
- **Architecture Impact**: Memperbarui kontrak DTO publik pada `Bilreg.Application` dan `Oware.Application`.
- **Resolved By**: User & ica-analyst (sesi /grill-me)
- **Resolved Date**: 2026-10-30

## GAP-002
- **Status**: CLOSED
- **Decision**: Menginjeksi `IPasienRepo` ke `LabOrderGetHandler` di Bilreg untuk memuat data demografi pasien, serta memanfaatkan `IRegRepo` yang sudah terinjeksi untuk memuat data registrasi, penjamin, dan ruangan bangsal (`reg.Bangsal`).
- **Rationale**: Data registrasi dan pasien sudah tersedia di database Bilreg; handler cukup memuatnya secara terarah berdasarkan `regId` dan `patientId`.
- **Impact**: `LabOrderGetHandler` melakukan pemanggilan repository tambahan saat memproses query detail order lab.
- **Architecture Impact**: Registrasi dependensi `IPasienRepo` pada handler dan pemetaan data model ke response DTO.
- **Resolved By**: User & ica-analyst (sesi /grill-me)
- **Resolved Date**: 2026-10-30

## GAP-003
- **Status**: CLOSED
- **Decision**: Menambahkan properti-properti padanan pada `BilregLabOrderResponseDto` di `a046_oware_ordermiddlewareapi`: `Nik`, `Address`, `City`, `BirthPlace`, `PhoneNo`, `JenisReg`, `TipeJaminanId`, `TipeJaminanName`, `PesertaJaminanId`, `BangsalId`, `BangsalName`, `BedName`.
- **Rationale**: Memastikan OWARE dapat mendeserialisasi data pengayaan yang dikirimkan oleh endpoint Bilreg.
- **Impact**: DTO konsumen OWARE sinkron 1:1 dengan payload respons Bilreg.
- **Architecture Impact**: Sinkronisasi kontrak interface DTO lintas-service.
- **Resolved By**: User & ica-analyst (sesi /grill-me)
- **Resolved Date**: 2026-10-30

## GAP-004
- **Status**: CLOSED
- **Decision**: Memperbarui `OrderLabBilregBuilder.cs` di OWARE untuk memetakan data dari DTO respons ke properti `OrderLabModel`, serta menerapkan translasi otomatis `JenisReg` menjadi `JenisInap` ("OP" vs "IP").
- **Rationale**: Mengakhiri pengisian string kosong pada builder dan memastikan seluruh adapter vendor LIS menerima data demografis, administratif, dan lokasi pasien secara akurat.
- **Impact**: Order lab yang tersimpan di OWARE dan dikirim ke vendor LIS memiliki data demografi, penjamin, dan ruangan lengkap.
- **Architecture Impact**: Implementasi Anti-Corruption Layer (ACL) translasi domain di OWARE.
- **Resolved By**: User & ica-analyst (sesi /grill-me)
- **Resolved Date**: 2026-10-30

## OQ-001
- **Status**: CLOSED
- **Decision**: Bilreg mengirimkan nilai enum/kode internal domain (`JenisReg`: "RegJalan", "RegInap", "RegIgd"), dan builder di OWARE (`OrderLabBilregBuilder.cs`) yang bertugas mentranslasikan nilai tersebut menjadi `"OP"` (untuk Rajal dan IGD) atau `"IP"` (untuk Ranap).
- **Rationale**: Menjaga kemurnian ubiquitous language domain Bilreg agar tidak terikat dengan format spesifik vendor LIS.
- **Impact**: Kontrak Bilreg tetap standar; OWARE mengelola logika translasi LIS.
- **Architecture Impact**: Penetapan batas tanggung jawab representasi data antar-service.
- **Resolved By**: User & ica-analyst (sesi /grill-me)
- **Resolved Date**: 2026-10-30

## OQ-002
- **Status**: CLOSED
- **Decision**: Mengambil informasi ruangan rawat inap langsung dari `RegModel.Bangsal` (`BangsalId`, `BangsalName`) tanpa melakukan query tambahan ke `IPakaiBedRepo` untuk detail nomor tempat tidur. Untuk field `BedName`, diisi dengan nama bangsal (atau string default jika LIS hanya membutuhkan informasi ruangan). Untuk Rawat Jalan & IGD, field ruangan dikosongkan.
- **Rationale**: Menyederhanakan penarikan data, menghindari join query yang berat atau ketergantungan baru ke `BedUsageContext`, dan tetap memenuhi kebutuhan utama LIS dalam mengetahui ruangan perawatan pasien.
- **Impact**: Handler `LabOrderGetHandler` tidak perlu menginjeksi `IPakaiBedRepo`.
- **Architecture Impact**: Desain pemuatan data menjadi *lean* dan performan.
- **Resolved By**: User & ica-analyst (sesi /grill-me)
- **Resolved Date**: 2026-10-30

## OQ-003
- **Status**: CLOSED
- **Decision**: Menerapkan *Graceful Fallback*: Jika data registrasi atau pasien tidak ditemukan di database Bilreg (misal order pasien luar `LabOrderSourceEnum.External` atau registrasi darurat), gunakan data dari snapshot `order.Patient` dan isi atribut yang hilang dengan string kosong `""` / `"-"` tanpa melempar exception/error.
- **Rationale**: Mencegah kegagalan proses integrasi (*crash*) pada skenario order pasien luar atau data belum lengkap.
- **Impact**: Sistem toleran terhadap anomali data pasien rujukan luar.
- **Architecture Impact**: Ketahanan (*fault-tolerance*) handler terhadap ketiadaan master data sekunder.
- **Resolved By**: User & ica-analyst (sesi /grill-me)
- **Resolved Date**: 2026-10-30

---

# 9. Architecture Applicability

## Decision

**`ARCHITECTURE-REQUIRED`**

## Rationale

Perubahan ini melibatkan integrasi lintas-service antara `b09-bilreg-api` dan `a046_oware_ordermiddlewareapi`, perubahan kontrak publik DTO (`LabOrderGetResponse` dan `BilregLabOrderResponseDto`), penambahan dependensi antar-context di Bilreg (`AdmisiContext`, `PasienContext`), dan standardisasi aturan pemetaan data ke domain `OrderLabModel` untuk vendor-vendor LIS pihak ketiga.

---

# 10. Planning Readiness

## Readiness Checklist

- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status

**`READY-FOR-PLANNING`**

## Notes

Seluruh GAP (GAP-001 s.d GAP-004) dan Open Questions (OQ-001 s.d OQ-003) telah diselesaikan dan disepakati pada sesi Gap Closure bersama user. Seluruh checklist kesiapan telah terpenuhi 100%. Gerbang kelayakan `READY-FOR-PLANNING` telah disahkan secara resmi oleh peran `ica-architect` pada tanggal 2026-10-30 untuk melanjutkan ke tahap penyusunan spesifikasi arsitektur teknis (*ARCHITECTURE*) dan rencana implementasi (*IMPLEMENTATION-PLAN*).

---

# 11. References

Referenced artifacts:
- [LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE.md)
- [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)
- [LAB-OWARE-ORDER-INTEGRATION-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-INTEGRATION-FEASIBILITY-ASSESSMENT.md)

Referenced codebase locations:
- [LabOrderGetQuery.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/LabContext/LabOrderFeature/UseCases/LabOrderGetQuery.cs)
- [LabOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/LabContext/LabOrderFeature/LabOrderController.cs)
- [RegModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/RegFeature/RegModel.cs)
- [PasienModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienModel.cs)
- [PakaiBedModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/BedUsageContext/PakaiBedFeature/PakaiBedModel.cs)
- [OrderLabBilregBuilder.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Workers/Creators/OrderLabBilregBuilder.cs)
- [BilregLabOrderResponseDto.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/Contracts/DTOs/BilregLabOrderResponseDto.cs)
- [CreateOrderLabFromBilregCommand.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Application/UseCases/Commands/CreateOrderLabFromBilregCommand.cs)
- [BilregLabOrderClient.cs](file:///D:/Project.Aktif/_hospital_web/a046_oware_ordermiddlewareapi/Oware.Infrastructure/ServicesBilreg/BilregLabOrderClient.cs)
