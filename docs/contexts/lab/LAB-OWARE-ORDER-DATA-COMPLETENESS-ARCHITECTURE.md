---
Title: Spesifikasi Arsitektur Teknis Pengayaan Kelengkapan Data Order Laboratorium untuk Integrasi LIS
Code: LAB-OWARE-ORDER-DATA-COMPLETENESS-ARCHITECTURE
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-10-30
---

# 1. Overview

Arsitektur teknis ini mendefinisikan realisasi spesifikasi teknis lintas-service antara **`b09-bilreg-api`** dan **`a046_oware_ordermiddlewareapi`** untuk memperkaya payload data order laboratorium yang diambil oleh middleware OWARE dari Bilreg. Pengayaan ini melengkapi data demografi pasien, data registrasi/penjamin, serta data lokasi ruangan rawat inap yang diwajibkan oleh vendor-vendor Laboratory Information System (LIS) eksternal seperti Sysmex, HCLAB, Softmedix, Prolims, dan Wynacom.

Referenced artifacts:
- ISSUE: [LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-DATA-COMPLETENESS-ISSUE.md)
- FEASIBILITY ASSESSMENT: [LAB-OWARE-ORDER-DATA-COMPLETENESS-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-DATA-COMPLETENESS-FEASIBILITY-ASSESSMENT.md)
- DOMAIN: [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md)

---

# 2. Architectural Basis

## Business Context

Sesuai [lab-domain.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/lab-domain.md), Laboratory Workflow Feature (LWF) di Bilreg memegang otoritas alur operasional klinis dan penagihan, sedangkan OWARE bertindak sebagai jembatan infrastruktur integrasi ke mesin penganalisis (*analyzer*) dan LIS pihak ketiga. Untuk mencetak label barcode spesimen dan meregistrasikan tes pada mesin LIS, LIS membutuhkan identitas unik pasien (NIK), alamat, nomor kontak, jenis rawat (Rawat Jalan / Rawat Inap / IGD), penjamin/asuransi, serta ruangan perawatan pasien.

## Analysis Input

Berdasarkan keputusan resmi pada [LAB-OWARE-ORDER-DATA-COMPLETENESS-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/lab/LAB-OWARE-ORDER-DATA-COMPLETENESS-FEASIBILITY-ASSESSMENT.md):
- **GAP-001 & GAP-003**: Mengadopsi strategi *Single-Payload Enrichment* pada `LabOrderGetResponse` (Bilreg) dan `BilregLabOrderResponseDto` (OWARE).
- **GAP-002 & OQ-002**: `LabOrderGetHandler` di Bilreg memuat data demografi via `IPasienRepo` dan data ruangan rawat inap langsung dari `RegModel.Bangsal` yang sudah diload oleh `IRegRepo` tanpa menambah dependensi ke `IPakaiBedRepo`.
- **OQ-001 & GAP-004**: Bilreg mengirimkan representasi domain internal (`JenisReg`), dan `OrderLabBilregBuilder.cs` di OWARE yang bertugas mentranslasikannya ke format LIS (`"OP"` vs `"IP"`).
- **OQ-003**: Menerapkan *Graceful Fallback* jika data pasien atau registrasi sekunder tidak ditemukan (misal pada order pasien luar `External`), menggunakan nilai default `""` tanpa melempar exception.

---

# 3. Scope

## Included

1. **`b09-bilreg-api` (Application Layer)**:
   - Penambahan properti pengayaan pada record `LabOrderGetResponse`:
     - Demografi: `Nik`, `Alamat`, `Kota`, `TempatLahir`, `PhoneNo` (semua bertipe `string`, default `""`).
     - Registrasi & Penjamin: `JenisReg` (`string`, default `""`), `TipeJaminanId` (`string`, default `""`), `TipeJaminanName` (`string`, default `""`), `PesertaJaminanId` (`string`, default `""`).
     - Ruangan Rawat Inap: `BangsalId` (`string`, default `""`), `BangsalName` (`string`, default `""`), `BedName` (`string`, default `""`).
   - Pendaftaran dependensi `IPasienRepo? pasienRepo = null` pada konstruktor `LabOrderGetHandler`.
   - Logika penarikan data demografi dari `PasienModel` dan data penjamin/bangsal dari `RegModel` dengan mekanisme *Graceful Fallback*.
   - Unit test pada `Bilreg.Test` (`LabOrderGetHandlerTest.cs`) memvalidasi pemetaan field baru secara komprehensif.

2. **`a046_oware_ordermiddlewareapi` (Application & Infrastructure Layer)**:
   - Penambahan properti padanan pada `BilregLabOrderResponseDto`:
     - `Nik`, `Address`, `City`, `BirthPlace`, `PhoneNo`, `JenisReg`, `TipeJaminanId`, `TipeJaminanName`, `PesertaJaminanId`, `BangsalId`, `BangsalName`, `BedName`.
   - Pembaruan `OrderLabBilregBuilder.cs`:
     - Memetakan field DTO ke `OrderLabModel`.
     - Logika translasi `JenisReg`: jika bernilai `"RegInap"` (atau mengandung `"Inap"`) -> set `JenisInap = "IP"`; selain itu -> set `JenisInap = "OP"`.
     - Mengisi `BedName = !string.IsNullOrWhiteSpace(dto.BedName) ? dto.BedName : dto.BangsalName`.
   - Unit test pada `Oware.Test` (`OrderLabBilregBuilderTest.cs` dan `BilregLabOrderClientTest.cs`).

## Excluded

- Modifikasi skema tabel database di Bilreg maupun OWARE (tabel `ta_pasien`, `ta_registrasi`, dan `OWARE_OrderLab` sudah memiliki kolom terkait).
- Modifikasi frontend HIS Web `c012_myhospital_web` (kontrak DTO bersifat aditif sehingga backward-compatible).
- Integrasi ke `IPakaiBedRepo` (sesuai keputusan OQ-002).

---

# 4. Technical Decisions

### TD-01: Single-Payload Enrichment Contract on `LabOrderGetResponse`
Kontrak respons `LabOrderGetResponse` diperluas dengan parameter opsional yang memiliki nilai default kosong (`= ""`). Hal ini menjamin:
- **Backward-Compatibility**: Seluruh *caller* dan unit test eksisting yang menginstansiasi `LabOrderGetResponse` secara posisional tetap valid tanpa perlu refactor menyeluruh.
- **Efisiensi Jaringan**: OWARE mendapatkan data lengkap dalam 1 kali round-trip HTTP (`GET api/LabContext/LabOrderFeature/{orderId}`) tanpa memicu *chatty API requests*.

### TD-02: Graceful Fallback Strategy for Incomplete & External Orders
Pada `LabOrderGetHandler`:
- Jika `order.Patient.PatientId` terisi, handler mencoba memuat `PasienModel` via `_pasienRepo?.LoadEntity(PasienModel.Key(patientId))`.
- Jika `PasienModel` tidak ditemukan (misal pasien eksternal atau master belum lengkap), nilai `Nik`, `Alamat`, `Kota`, `TempatLahir`, dan `PhoneNo` bernilai string kosong `""`.
- Jika `order.Patient.RegId` terisi, handler mencoba memuat `RegModel` via `_regRepo?.LoadEntity(RegModel.Key(regId))`.
- Jika `RegModel` tidak ditemukan, nilai `JenisReg`, `TipeJaminanId`, `TipeJaminanName`, `PesertaJaminanId`, `BangsalId`, dan `BangsalName` bernilai string kosong `""`.
- Handler tidak boleh melempar *unhandled exception* yang dapat menggagalkan proses sinkronisasi order.

### TD-03: Anti-Corruption Layer (ACL) for LIS Patient Types
Bilreg tidak mengenal terminologi LIS pihak ketiga (`"OP"` vs `"IP"`). Bilreg secara eksklusif menggunakan ubiquitous language internalnya: `JenisRegEnum` (`RegJalan`, `RegInap`, `RegIgd`).
Di sisi OWARE, `OrderLabBilregBuilder` bertindak sebagai Anti-Corruption Layer yang mentranslasikan:
```csharp
order.JenisInap = (dto.JenisReg == "RegInap" || dto.JenisReg == ((int)JenisRegEnum.RegInap).ToString())
    ? "IP"
    : "OP";
```

### TD-04: Ward Location Resolution via `RegModel.Bangsal`
Sesuai keputusan OQ-002, ruangan rawat inap pasien diambil langsung dari `RegModel.Bangsal`:
- `BangsalId`: `reg.Bangsal.BangsalId`
- `BangsalName`: `reg.Bangsal.BangsalName`
- `BedName`: `reg.Bangsal.BangsalName` (karena nama ruangan adalah informasi utama yang dicetak pada label tabung spesimen dan barcode LIS)

---

# 5. Component Responsibilities

| Component | Repository | Responsibility |
|---|---|---|
| `LabOrderGetResponse` | `b09-bilreg-api` | Mendefinisikan kontrak data respons detail order lab dengan penambahan atribut demografi, registrasi/penjamin, dan bangsal. |
| `LabOrderGetHandler` | `b09-bilreg-api` | Mengorkestrasi penarikan entitas `LabOrderModel`, `RegModel` (via `IRegRepo`), dan `PasienModel` (via `IPasienRepo`) serta memetakan ke `LabOrderGetResponse` dengan *graceful fallback*. |
| `BilregLabOrderResponseDto` | `a046_oware_ordermiddlewareapi` | Merepresentasikan struktur JSON dari endpoint Bilreg untuk deserialisasi di OWARE. |
| `OrderLabBilregBuilder` | `a046_oware_ordermiddlewareapi` | Memetakan atribut dari `BilregLabOrderResponseDto` ke entitas domain `OrderLabModel`, termasuk translasi `JenisReg` ke `JenisInap` ("OP" vs "IP"). |
| `OrderLabHubService` | `a046_oware_ordermiddlewareapi` | Meneruskan entitas `OrderLabModel` yang telah terisi lengkap ke adapter vendor LIS. |

---

# 6. Integration Design

```
[Bilreg Lab Order Workflow]
           │
           │ (1) POST /api/order/lab/bilreg { orderId }
           ▼
[OWARE: CreateOrderLabFromBilregHandler]
           │
           │ (2) GET /api/LabContext/LabOrderFeature/{orderId}
           ▼
[Bilreg: LabOrderGetHandler]
      │
      ├── Load LabOrderModel (order data)
      ├── Load PasienModel (via IPasienRepo: Nik, Alamat, PhoneNo, TempatLahir)
      └── Load RegModel (via IRegRepo: JenisReg, TipeJaminan, Polis, Bangsal)
      │
      ▼ (3) HTTP 200 LabOrderGetResponse (Enriched Single Payload)
[OWARE: BilregLabOrderClient]
      │
      ▼ (4) DTO to Model
[OWARE: OrderLabBilregBuilder]
      │ - Map Nik, Address, City, PhoneNo, BirthPlace
      │ - Map TipeJaminanName, PesertaJaminanId
      │ - Translate JenisReg -> JenisInap ("OP" / "IP")
      │ - Map BangsalName & BedName
      ▼
[OWARE: _orderWriter.Save(orderLab)]
      │
      ▼ (5) Dispatch to LIS Adapter (Sysmex / HCLAB / Softmedix / Prolims)
[Vendor LIS Service]
```

---

# 7. Data Ownership

| Data Domain | Authoritative Owner | Consumers |
|---|---|---|
| Order Status & Clinical Tests | `b09-bilreg-api` (`LabContext`) | OWARE, HIS Web |
| Patient Demographics (`Nik`, `Address`, `PhoneNo`, `BirthPlace`) | `b09-bilreg-api` (`PasienContext`) | `LabContext`, OWARE |
| Registration & Insurance (`JenisReg`, `TipeJaminan`, `Polis`) | `b09-bilreg-api` (`AdmisiContext`) | `LabContext`, OWARE |
| Ward Location (`Bangsal`) | `b09-bilreg-api` (`AdmisiContext` / `BedUsageContext`) | `LabContext`, OWARE |
| LIS Ingestion Payload (`OWARE_OrderLab`) | `a046_oware_ordermiddlewareapi` | Vendor LIS Engines |

---

# 8. Database Design

## New Tables
*Tidak ada (None).*

## Modified Tables
*Tidak ada (None).*

## Relationships & Persistensi
Seluruh data yang diperlukan sudah tersedia dan tersimpan persisten pada tabel-tabel basis data SIMRS:
- `ta_pasien`: Menyimpan NIK, alamat (jalan, RT/RW, kelurahan, kota), tempat lahir, dan nomor kontak.
- `ta_registrasi`: Menyimpan jenis registrasi (`c_jenis_reg`), penjamin (`fs_kd_tipe_jaminan`), polis/asuransi (`fs_no_polis`), dan ruangan bangsal (`fs_kd_bangsal`).
- `OWARE_OrderLab`: Tabel OWARE yang telah memiliki kolom `JenisInap`, `Nik`, `Address`, `City`, `PhoneNo`, `BirthPlace`, `TipeJaminanName`, `PesertaJaminanId`, `BedName`, `BangsalName`.

---

# 9. Cross-Cutting Concerns

1. **Performance**: Pemanggilan `PasienModel` dan `RegModel` pada `LabOrderGetHandler` dilakukan berbasis Primary Key / Clustered Index (`IPasienRepo.LoadEntity`, `IRegRepo.LoadEntity`) dengan waktu eksekusi < 5 ms, sehingga tidak membebani transaksi.
2. **Null Safety & Resilience**: Pengambilan nomor telepon memprioritaskan kontak bertipe `Mobile` atau `Phone` dari `PasienModel.ListContact`. Pengambilan alamat menggabungkan jalan, RT/RW, dan kelurahan menjadi satu kesatuan string alamat yang ramah dibaca oleh sistem LIS.

---

# 10. Implementation Constraints

1. **Explicit Persistence & Tactical DDD**: Implementasi pada `b09-bilreg-api` harus mengikuti standar Tactical DDD yang berlaku, menggunakan interface repository `IPasienRepo` dan `IRegRepo` tanpa melakukan *direct query SQL* di dalam Application Handler.
2. **Backward-Compatibility**: Penambahan parameter pada `LabOrderGetResponse` wajib diletakkan di akhir parameter list dengan *default parameter value* agar tidak merusak kompilasi pada caller lain.
3. **Multi-Repo Isolation**: Perubahan dilakukan terpisah dan independen per repository:
   - Slice 1: `b09-bilreg-api`
   - Slice 2: `a046_oware_ordermiddlewareapi`

---

# 11. Acceptance Conditions

1. **`b09-bilreg-api`**:
   - `LabOrderGetResponse` memiliki field `Nik`, `Alamat`, `Kota`, `TempatLahir`, `PhoneNo`, `JenisReg`, `TipeJaminanId`, `TipeJaminanName`, `PesertaJaminanId`, `BangsalId`, `BangsalName`, `BedName`.
   - `LabOrderGetHandler` berhasil mengisi field-field tersebut dari `PasienModel` dan `RegModel`.
   - Jika `PasienModel` atau `RegModel` tidak ditemukan, handler mengembalikan nilai default tanpa melempar exception.
   - Solusi `b09-bilreg-api.sln` build dengan 0 Errors, dan targeted unit tests di `Bilreg.Test` lulus 100%.

2. **`a046_oware_ordermiddlewareapi`**:
   - `BilregLabOrderResponseDto` mendefinisikan seluruh properti baru.
   - `OrderLabBilregBuilder` memetakan NIK, alamat, kontak, penjamin, ruangan bangsal, dan mentranslasikan `JenisReg` menjadi `JenisInap` ("IP" untuk Ranap, "OP" untuk Rajal/IGD).
   - Solusi OWARE build dengan 0 Errors, dan targeted unit tests di `Oware.Test` lulus 100%.
