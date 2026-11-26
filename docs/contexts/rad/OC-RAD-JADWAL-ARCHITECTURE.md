---
Title: Penjadwalan Pemeriksaan Radiologi (JadwalRadiologi) Architecture
Code: OC-RAD-JADWAL
Artifact: ARCHITECTURE
Version: 1.0
LastUpdated: 2026-11-25
---

# 1. Overview

Arsitektur ini merealisasikan spesifikasi fitur **[OC-RAD-JADWAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEATURE.md)** yang diturunkan dari outcome kanonikal bisnis **[OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md)**, dengan mengonsumsi keputusan penutupan kesenjangan yang disahkan pada **[OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md)**.

Arsitektur ini menetapkan cetak biru teknis menyeluruh bagi backend `b09-bilreg-api` dan frontend `c012_myhospital_web`:
1. **Penerapan Standalone Aggregate Root `RadJadwalModel`**:
   - Memisahkan secara tegas tanggung jawab pemesanan klinis (`RadOrderModel`) dari alokasi kapasitas fisik dan slot waktu (`RadJadwalModel`).
   - Menyimpan fakta bisnis persisten jadwal pada tabel terdedikasi `BILRG_RadJadwal`.
2. **Penegakan Invarian Ketiadaan Tabrakan Waktu (No-Overlap Invariant)**:
   - Validasi ketersediaan slot bebas benturan (*collision-free*) berbasis mesin fisik (`EquipmentId`), tanggal periksa (`ScheduleDate`), dan rentang waktu (`StartTime` hingga `EndTime` inklusif turnaround buffer).
   - Penegakan proteksi konkuren (*race condition*) melalui kuncian transaksi atomik pada basis data SQL Server.
3. **Sinkronisasi Siklus Hidup Dua Arah (Decoupled Two-Way Lifecycle Sync)**:
   - Penambahan metode domain `Unschedule()` pada `RadOrderModel` untuk mengembalikan status order menjadi `Requested` saat jadwal dibatalkan, tanpa merusak order klinis DPJP.
   - Pembatalan `RadOrder` secara otomatis memicu pembatalan slot jadwal yang terhubung (*cascading cancellation*).
4. **Gerbang Keselamatan Pasien & Kesiapan Klinis (Safety Clearance Gate)**:
   - Struktur data persiapan pasien (puasa, obat, eGFR) dan checklist keselamatan (implan logam, alergi, kehamilan) tersimpan secara eksplisit.
   - Status jadwal hanya dapat ditransisikan ke `Completed` jika pasien telah tiba (`IsArrived = true`) dan diverifikasi aman (`IsCleared = true`).
5. **Histori Penjadwalan Ulang Non-Destruktif (In-Place Reschedule)**:
   - Mempertahankan identifier unik `JadwalRadId` dan `ScheduleNo` yang sama, melepaskan slot lama, menaikkan `RescheduleCount`, dan mencatat riwayat mutasi pada tabel pelacak `BILRG_RadJadwalReschedule`.
6. **Kedaulatan Master Mesin Radiologi Lokal (`BILRG_RadEquipment`)**:
   - Tabel referensi master lokal di dalam `RadContext` untuk menjamin independensi bounded context radiologi dan kecepatan respon kueri kalender scheduler.
7. **Integrasi Milestone Tangguh (*Resilient Tracker Integration*)**:
   - Penerbitan pembaruan status jadwal ke modul pelacakan perjalanan pasien (`ADM-TRACKER`) melalui port `IRadTrackerIntegration` dengan eksekusi *non-blocking*.
8. **Modernisasi Frontend Berbasis TanStack Query & Web Print**:
   - Composable reaktif `useRadJadwal` di frontend `c012_myhospital_web` dengan schema Zod.
   - Pencetakan Lembar Konfirmasi Jadwal langsung via dialog print browser berbasis HTML/CSS.

---

# 2. Architectural Basis

## Business Context

Pemeriksaan radiologi berkapasitas terbatas (seperti CT-Scan, MRI, USG, Fluoroskopi, dan tindakan intervensi) menuntut kehadiran fisik pasien pada mesin diagnostik tertentu. Rumah sakit membutuhkan kepastian alokasi waktu bebas benturan, pemenuhan syarat persiapan klinis sebelum pemindaian, dan transparansi perjalanan pasien dari order hingga pemeriksaan fisik.

Sesuai [08-RADIOLOGI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/08-RADIOLOGI-DOMAIN.md) dan [OC-RAD-JADWAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEATURE.md):
- `RAD-ORDER`: Mengelola permintaan klinis.
- `RAD-JADWAL`: Mengelola alokasi kapasitas mesin fisik dan slot waktu pasien.
- `RAD-EXAM`: Mengelola eksekusi pemindaian fisik di mesin setelah gerbang keselamatan lolos.
- `RAD-EXPERTISE`: Mengelola pembacaan hasil diagnostik oleh dokter spesialis radiologi.

## Analysis Input

Berdasarkan kesepakatan pada [OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md) dan sesi konfirmasi `/grill-me`:

```text
[08-RADIOLOGI-DOMAIN.md] + [OC-RAD-JADWAL-FEATURE.md]
                           +
        [OC-RAD-JADWAL-FEASIBILITY-ASSESSMENT.md]
                           ↓
             [OC-RAD-JADWAL-ARCHITECTURE.md]
```

---

# 3. Scope

## Included

1. **Domain Layer Backend (`Bilreg.Domain.RadContext.RadJadwalFeature`)**:
   - Aggregate Root `RadJadwalModel` (status: `Scheduled`, `Completed`, `Cancelled`).
   - Value Objects: `RadTimeRangeType`, `RadPreparationInfoType`, `RadSafetyChecklistType`, `RadClearanceType`, `RadRescheduleHistoryType`.
   - Domain Enum: `RadJadwalStatusEnum`, `RadCancellationCategoryEnum`.
   - Modifikasi `RadOrderModel` di `RadOrderFeature`: penambahan metode `Unschedule(userId, unscheduledAt)`.
2. **Application Layer Backend (`Bilreg.Application.RadContext.RadJadwalFeature`)**:
   - Commands: `RadJadwalCreateCmd`, `RadJadwalRescheduleCmd`, `RadJadwalCancelCmd`, `RadJadwalClearanceCmd`.
   - Queries: `RadJadwalGetQuery`, `RadJadwalWorklistQuery`, `RadJadwalCalendarTimelineQuery`, `RadEquipmentLookupQuery`.
   - Port Interfaces: `IRadJadwalRepo`, `IRadJadwalDal`, `IRadEquipmentDal`, `IRadTrackerIntegration`.
   - DTOs & Validation Guard Rules.
3. **Infrastructure Layer Backend (`Bilreg.Infrastructure.RadContext.RadJadwalFeature`)**:
   - `RadJadwalRepo`, `RadJadwalDal`, `RadEquipmentDal` menggunakan pure Dapper / ADO.NET SQL Server.
   - Implementasi kueri deteksi tumpang tindih waktu (*overlap query*) dengan transaksi berpelindung locking.
   - Implementasi `RadTrackerIntegration` memanggil service tracker secara *resilient* (try-catch log).
4. **API Layer Backend (`Bilreg.Api.Controllers.RadContext`)**:
   - `RadJadwalController`: Endpoint CRUD & operasional jadwal radiologi.
   - `RadEquipmentController`: Endpoint lookup master alat fisik radiologi.
5. **Database Layer Backend (`Bilreg.SqlDb.RadContext`)**:
   - Skrip migrasi DDL tabel: `BILRG_RadJadwal`, `BILRG_RadEquipment`, `BILRG_RadJadwalReschedule`.
   - Indexing untuk performa kueri kalender dan deteksi benturan waktu.
6. **Frontend Layer (`c012_myhospital_web`)**:
   - Modul `src/modules/Radiology`:
     - Service API klien & TanStack Query composables (`useRadJadwal`, `useRadEquipment`).
     - Refaktorisasi komponen visual [RadSchedulingPanel.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/components/RadSchedulingPanel.vue) agar mengonsumsi data timeline live.
     - Refaktorisasi [TindakanRadiologi.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/views/TindakanRadiologi.vue) untuk modal dialog penjadwalan, reschedule, pembatalan, dan clearance check.
     - Komponen print dialog Lembar Konfirmasi Jadwal & Panduan Persiapan Pasien.
7. **Test Suites**:
   - Backend: Unit test model domain, command/query handlers, dan repository/DAL di `Bilreg.Test`.
   - Frontend: Unit tests / type check (`pnpm tc:app`) dan verifikasi lint.

## Excluded

1. Pengoperasian tombol eksposur alat pemindaian, transmisi gambar PACS / DICOM (`RAD-EXAM`).
2. Penyusunan ekspertise klinis dan verifikasi hasil bacaan radiolog (`RAD-EXPERTISE`).
3. Transaksi kasir penerimaan pembayaran tunai/non-tunai (`TRK-KASIR`).
4. Pendaftaran registrasi admisi sentral dan cetak SEP BPJS (`ADM-REG` / `BPJ-VCLAIM`).
5. Pengelolaan shift kerja harian staf medis / radiografer (`ORG-PPA`).

---

# 4. Technical Decisions

## TD-01: Standalone Aggregate Root `RadJadwalModel` & Skema Basis Data Mandiri

**Keputusan**: Entitas `JadwalRadiologi` dimodelkan sebagai aggregate root tersendiri `RadJadwalModel` dalam namespace `Bilreg.Domain.RadContext.RadJadwalFeature`.
- Memiliki tabel persisten sendiri `BILRG_RadJadwal`.
- Menyimpan referensi `OrderId` dan `OrderNo` sebagai foreign reference logis (bukan tight database foreign key cascade).
- Memiliki siklus hidup sendiri (`Scheduled` $\rightarrow$ `Completed` atau `Cancelled`) yang independen dari siklus hidup teknis order.

```csharp
namespace Bilreg.Domain.RadContext.RadJadwalFeature;

public class RadJadwalModel : IRadJadwalKey
{
    public const string IdPrefix = "JAD";
    public const string ScheduleNoPrefix = "SCH";

    public string JadwalRadId { get; init; }
    public string ScheduleNo { get; init; }
    public string OrderId { get; init; }
    public string OrderNo { get; init; }
    public string RegId { get; init; }
    public PatientSnapshotType Patient { get; init; }
    public RadModalityEnum Modality { get; init; }
    public string EquipmentId { get; private set; }
    public string EquipmentName { get; private set; }
    public string RoomId { get; private set; }
    public string RoomName { get; private set; }
    public string DoctorRadiologyId { get; private set; }
    public string DoctorRadiologyName { get; private set; }
    
    // Waktu & Durasi
    public DateOnly ScheduleDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public int TurnaroundBufferMinutes { get; private set; }

    // Persiapan & Safety Clearance
    public string PreparationNotes { get; private set; }
    public string LabRequirements { get; private set; }
    public RadSafetyChecklistType SafetyChecklist { get; private set; }
    public bool IsArrived { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public bool IsCleared { get; private set; }
    public string ClearanceNotes { get; private set; }
    public string ClearedByUserId { get; private set; }
    public DateTime? ClearedAt { get; private set; }

    // Status Siklus Hidup & Audit Reschedule
    public RadJadwalStatusEnum Status { get; private set; }
    public int RescheduleCount { get; private set; }
    public string CancellationReason { get; private set; }
    public RadCancellationCategoryEnum? CancellationCategory { get; private set; }
    public AuditTrailType AuditTrail { get; init; }
}
```

## TD-02: Algoritma Collision Detection & No-Overlap Invariant

**Keputusan**: Validasi rentang waktu tumpang-tindih dieksekusi sebelum penyimpanan jadwal baru atau penjadwalan ulang:
- Formula overlap interval pada mesin yang sama (`EquipmentId`) pada tanggal yang sama (`ScheduleDate`):
  $$\text{Existing.StartTime} < \text{New.EndTime} \quad \text{AND} \quad \text{Existing.EndTime} > \text{New.StartTime}$$
- Hanya jadwal berstatus aktif (`Status == Scheduled`) yang dievaluasi sebagai konflik.
- Jadwal yang sedang di-reschedule mengecualikan `JadwalRadId`-nya sendiri dari kueri collision.
- Pada lapisan infrastruktur SQL Server, pengecekan collision dan insert/update dibungkus dalam transaksi atomik dengan petunjuk kuncian baris (`WITH (UPDLOCK, HOLDLOCK)`) untuk mencegah kondisi balapan (*race condition*).

## TD-03: Two-Way Decoupled Lifecycle Synchronization

**Keputusan**: Bounded context mempertahankan sinkronisasi siklus hidup dua arah:
1. **Saat Jadwal Dibuat**:
   - `RadJadwalCreateHandler` menyimpan `RadJadwalModel` (status: `Scheduled`).
   - Memanggil `RadOrderModel.Schedule(userId, scheduledAt)` dan memperbarui status order menjadi `Scheduled`.
2. **Saat Jadwal Dibatalkan**:
   - `RadJadwalCancelHandler` menandai jadwal sebagai `Cancelled` dan melepaskan slot mesin.
   - Mengambil agregat order dan memanggil metode baru `RadOrderModel.Unschedule(userId, at)`, sehingga status order kembali menjadi `Requested`. Permintaan klinis tetap sah dan siap dijadwalkan ulang.
3. **Saat Order Dibatalkan DPJP**:
   - `RadOrderCancelHandler` membatalkan order. Jika order memiliki jadwal aktif (`Status == Scheduled`), handler secara otomatis membatalkan alokasi slot jadwal terkait dengan kategori alasan `Order Dibatalkan Pengorder`.

## TD-04: Structured Safety Clearance Gate

**Keputusan**: Penegakan gerbang kesiapan klinis (*clearance gate*) pra-tindakan:
- Sebelum pasien memasuki ruang pemindaian radiologi (`RAD-EXAM`), pasien wajib diverifikasi kesiapannya:
  1. Pasien tiba di loket radiologi: `IsArrived = true`, mencatat waktu kedatangan.
  2. Checklist keselamatan dan persiapan dipatuhi: `IsCleared = true`, mencatat `ClearanceNotes` dan ID petugas pemeriksa.
- **Invariant Gate**: Metode domain `Complete()` pada `RadJadwalModel` akan melempar `InvalidOperationException` jika `IsArrived == false` atau `IsCleared == false`.
- Transisi status ke `Completed` menandai selesainya slot jadwal dan mengalirkan referensi `JadwalRadId` ke antrean eksekusi fisik `RAD-EXAM`.

## TD-05: Non-Destructive In-Place Reschedule Pattern

**Keputusan**: Pemindahan jadwal mempertahankan `JadwalRadId` dan `ScheduleNo`:
- Metode domain `Reschedule(newDate, newStart, newEnd, newEquipmentId, reason, userId)`:
  - Memvalidasi ketiadaan konflik pada slot dan mesin baru.
  - Menyimpan snapshot slot lama ke tabel audit `BILRG_RadJadwalReschedule`.
  - Memperbarui waktu dan mesin pada `BILRG_RadJadwal`, menaikkan `RescheduleCount += 1`.
  - Merekam audit modifikasi user.

## TD-06: Master Referensi Mesin Lokal `BILRG_RadEquipment`

**Keputusan**: Bounded context `RadContext` memiliki tabel master lokal `BILRG_RadEquipment`:
- Kolom: `EquipmentId VARCHAR(10) PK`, `EquipmentName VARCHAR(100)`, `Modality VARCHAR(20)`, `RoomId VARCHAR(10)`, `RoomName VARCHAR(100)`, `IsActive BIT`.
- Menyediakan endpoint lookup `GET api/rad/equipment` untuk mengisi dropdown pemilihan mesin fisik di antarmuka frontend berdasarkan modalitas order.

## TD-07: Resilient Integration Port `IRadTrackerIntegration`

**Keputusan**: Komunikasi pembaruan milestone ke `ADM-TRACKER` diisolasi di balik port:
```csharp
namespace Bilreg.Application.RadContext.RadJadwalFeature;

public interface IRadTrackerIntegration
{
    Task PublishMilestoneAsync(string regId, string jadwalRadId, string milestone, string note, CancellationToken ct = default);
}
```
- Implementasi pada Infrastructure membungkus pemanggilan service pelacak pasien dalam blok try-catch dengan logging error, sehingga kegagalan eksternal tracker tidak menggagalkan transaksi reservasi jadwal radiologi.

## TD-08: Automatic Turnaround Buffer Calculation

**Keputusan**: Durasi total slot dihitung otomatis:
$$\text{EndTime} = \text{StartTime} + \sum(\text{ItemDuration}) + \text{TurnaroundBuffer}$$
- Nilai default buffer: 10 menit (CT, USG, Fluoroskopi), 15 menit (MRI).
- Antarmuka frontend menyediakan opsi bagi petugas loket untuk menyesuaikan durasi akhir sebelum reservasi disimpan.

## TD-09: Client-Side Browser Print Layout

**Keputusan**: Lembar Konfirmasi Jadwal dan Panduan Persiapan Pasien dicetak langsung dari antarmuka web melalui CSS media print layout yang rapi:
- Komponen `RadScheduleConfirmationPrint.vue` menerima data DTO jadwal terverifikasi.
- Menghasilkan lembar cetak standar rumah sakit memuat data identitas pasien, nomor transaksi, jam tiba, jam pemeriksaan, nama mesin & ruangan, panduan puasa/minum, dan checklist skrining yang wajib dibawa.

## TD-10: Frontend Declarative Data Fetching & Cache Invalidation

**Keputusan**: Modul `Radiology` di `c012_myhospital_web` menggunakan TanStack Query:
- `useRadJadwalCalendarQuery(date, modality)`: Mengambil daftar slot terjadwal untuk visualisasi timeline harian.
- `useRadJadwalWorklistQuery(params)`: Mengambil daftar order terjadwal dan belum terjadwal.
- Mutasi (`useCreateRadJadwalMutation`, `useRescheduleRadJadwalMutation`, `useCancelRadJadwalMutation`, `useClearanceRadJadwalMutation`): Secara otomatis memicu invalidasi cache query kunci `['rad-jadwal']` dan `['rad-order']` sehingga UI selalu terbarui tanpa refresh manual.

---

# 5. Component Responsibilities

| Layer / Sub-Sistem | Komponen | Tanggung Jawab Utama |
|---|---|---|
| **Domain** (`Bilreg.Domain`) | `RadJadwalModel` | Aggregate Root jadwal radiologi, menegakkan invarian validitas waktu, clearance gate, reschedule, dan pembatalan. |
| **Domain** (`Bilreg.Domain`) | `RadSafetyChecklistType` | Value Object struktur checklist skrining keselamatan (implan, alergi, kehamilan). |
| **Domain** (`Bilreg.Domain`) | `RadOrderModel` (Extension) | Penambahan metode `Unschedule()` untuk mentransisikan kembali status order dari `Scheduled` ke `Requested`. |
| **Application** (`Bilreg.Application`) | `RadJadwalCreateCmd` & Handler | Validasi kelayakan order, kalkulasi durasi, kueri collision detection, simpan jadwal baru, dan update status order. |
| **Application** (`Bilreg.Application`) | `RadJadwalRescheduleCmd` & Handler | Validasi slot baru bebas benturan, pembaruan in-place, pencatatan histori reschedule, dan audit trail. |
| **Application** (`Bilreg.Application`) | `RadJadwalCancelCmd` & Handler | Pembatalan slot jadwal, pelepasan kapasitas mesin, dan pemanggilan `RadOrder.Unschedule()`. |
| **Application** (`Bilreg.Application`) | `RadJadwalClearanceCmd` & Handler | Perekaman status kedatangan pasien (`IsArrived`) dan verifikasi skrining keselamatan (`IsCleared`). |
| **Application** (`Bilreg.Application`) | `RadJadwalCalendarTimelineQuery` & Handler | Mengambil alokasi jadwal terpetakan ke slot waktu dan mesin untuk visualisasi kalender scheduler. |
| **Application** (`Bilreg.Application`) | `IRadJadwalDal` / `IRadJadwalRepo` | Kontrak port akses data jadwal, pengecekan overlap waktu, dan penulisan transaksi. |
| **Application** (`Bilreg.Application`) | `IRadTrackerIntegration` | Kontrak port pengiriman milestone jadwal ke modul tracker perjalanan pasien. |
| **Infrastructure** (`Bilreg.Infrastructure`) | `RadJadwalDal` & `RadJadwalRepo` | Implementasi Dapper / ADO.NET SQL Server dengan transaksi berpelindung kuncian konkurensi. |
| **Infrastructure** (`Bilreg.Infrastructure`) | `RadEquipmentDal` | Implementasi akses data master lokal mesin fisik dan ruangan pemeriksaan radiologi. |
| **Infrastructure** (`Bilreg.Infrastructure`) | `RadTrackerIntegration` | Adapter pemanggil service pelacak pasien dengan penanganan kesalahan toleran. |
| **API** (`Bilreg.Api`) | `RadJadwalController` | REST API controller mengekspos endpoint pemesanan jadwal, reschedule, pembatalan, clearance, dan kalender. |
| **API** (`Bilreg.Api`) | `RadEquipmentController` | REST API controller mengekspos endpoint daftar master mesin diagnostik radiologi. |
| **Database** (`Bilreg.SqlDb`) | Migrasi SQL Tables | DDL tabel `BILRG_RadJadwal`, `BILRG_RadEquipment`, dan `BILRG_RadJadwalReschedule` beserta index performa. |
| **Frontend** (`c012_myhospital_web`) | `useRadJadwal.ts` | Composable TanStack Query untuk kueri timeline slot, worklist, dan mutasi aksi penjadwalan. |
| **Frontend** (`c012_myhospital_web`) | `RadSchedulingPanel.vue` | Komponen visual timeline kalender per mesin dan jam operasional, reaktif terhadap data live query. |
| **Frontend** (`c012_myhospital_web`) | `RadScheduleDialog.vue` | Dialog form reservasi jadwal baru: pemilihan mesin, estimasi durasi + buffer, panduan persiapan, dan checklist. |
| **Frontend** (`c012_myhospital_web`) | `RadClearanceDialog.vue` | Dialog form verifikasi kedatangan pasien dan pengisian checklist keselamatan sebelum dialihkan ke pemindaian fisik. |
| **Frontend** (`c012_myhospital_web`) | `RadScheduleConfirmationPrint.vue` | Komponen cetak Lembar Konfirmasi Jadwal dan Panduan Persiapan Pasien terformat rapi. |

---

# 6. Integration Design

| Source Component | Target Component | Purpose & Contract |
|---|---|---|
| `RadJadwalCreateHandler` | `IRadJadwalDal.IsSlotOverlapping` | Memvalidasi ketiadaan tabrakan waktu pada mesin fisik yang sama sebelum jadwal baru disimpan. |
| `RadJadwalCreateHandler` | `IRadOrderRepo` | Membaca order klinis dan memanggil `order.Schedule()` untuk memperbarui status order menjadi `Scheduled`. |
| `RadJadwalCancelHandler` | `IRadOrderRepo` | Memanggil `order.Unschedule()` untuk mengembalikan status order kembali ke `Requested` saat jadwal dibatalkan. |
| `RadOrderCancelHandler` | `IRadJadwalRepo` | Membatalkan jadwal aktif yang terhubung secara otomatis saat order klinis dibatalkan oleh DPJP. |
| `RadJadwalClearanceHandler` | `RadJadwalModel.Complete()` | Menegakkan verifikasi `IsArrived = true` dan `IsCleared = true` sebelum status jadwal ditutup menjadi `Completed`. |
| `RadJadwalHandlers` | `IRadTrackerIntegration` | Mempublikasikan milestone `Scheduled`, `Rescheduled`, `Cancelled`, `Completed` ke modul pelacakan pasien. |
| `RadSchedulingPanel.vue` | `GET api/rad/jadwal/calendar` | Mengambil data lini masa penggunaan mesin per tanggal untuk visualisasi scheduler frontend. |
| `RadScheduleDialog.vue` | `POST api/rad/jadwal` | Mengirim payload reservasi jadwal baru dari form loket radiologi ke backend API. |

---

# 7. Data Ownership

| Data Entity | Authoritative Owner | Batasan Tanggung Jawab |
|---|---|---|
| **Alokasi Jadwal & Slot Waktu** (`JadwalRadiologi`) | `RadContext (RAD-JADWAL)` | Pemilik mutlak kapasitas mesin, rentang waktu periksa, dan status jadwal. |
| **Permintaan Pemeriksaan Klinis** (`OrderRadiologi`) | `RadContext (RAD-ORDER)` | Pemilik mutlak item tindakan klinis, urgensi, dan dokter pengorder. |
| **Master Mesin & Ruang Radiologi** (`RadEquipment`) | `RadContext` (Lokal Cache) | Pemilik data operasional modalitas mesin fisik dan nomor ruangan radiologi. |
| **Identitas Pasien** (`PatientSnapshot`) | `Pasien Context (PAS-DATSOS)` | Read-only snapshot nomor RM, nama, tanggal lahir, kontak pasien. |
| **Konteks Kunjungan** (`RegId`) | `Admisi Context (ADM-REG)` | Read-only konteks episode registrasi kunjungan aktif pasien. |
| **Milestone Pasien** (`PasienTracker`) | `Admisi Context (ADM-TRACKER)` | Menerima pembaruan tahapan pelayanan untuk visualisasi alur terpusat. |

---

# 8. Database Design

## New Tables

### 1. `BILRG_RadJadwal`
Tabel utama pencatatan entitas jadwal pemeriksaan radiologi persisten.

| Kolom | Tipe Data | Keterangan |
|---|---|---|
| `JadwalRadId` | `VARCHAR(16)` | Primary Key (Format: `JAD` + NunaId). |
| `ScheduleNo` | `VARCHAR(20)` | Unique Transaction Number (Format: `SCH-YYYYMMDD-XXXX`). |
| `OrderId` | `VARCHAR(16)` | Foreign reference ke `BILRG_RadOrder.OrderId` (Indexed). |
| `OrderNo` | `VARCHAR(20)` | Nomor order radiologi. |
| `RegId` | `VARCHAR(16)` | Nomor registrasi kunjungan pasien (Indexed). |
| `PasienId` | `VARCHAR(16)` | Nomor Rekam Medis (RM) pasien. |
| `PasienName` | `VARCHAR(100)` | Snapshot nama lengkap pasien. |
| `BirthDate` | `DATETIME` | Tanggal lahir pasien. |
| `Gender` | `VARCHAR(1)` | Jenis kelamin ('L' / 'P'). |
| `Phone` | `VARCHAR(25)` | Nomor kontak pasien. |
| `Modality` | `VARCHAR(20)` | Modalitas pemeriksaan (`CT`, `MRI`, `USG`, `Fluoroscopy`, dll.). |
| `EquipmentId` | `VARCHAR(10)` | Identifier mesin fisik radiologi (Indexed). |
| `EquipmentName` | `VARCHAR(100)` | Nama mesin fisik radiologi. |
| `RoomId` | `VARCHAR(10)` | Identifier ruangan pemeriksaan radiologi. |
| `RoomName` | `VARCHAR(100)` | Nama ruangan pemeriksaan radiologi. |
| `DoctorRadiologyId` | `VARCHAR(16)` | ID dokter spesialis radiologi penanggung jawab (bila ada). |
| `DoctorRadiologyName` | `VARCHAR(100)` | Nama dokter spesialis radiologi penanggung jawab. |
| `ScheduleDate` | `DATE` | Tanggal pemeriksaan yang dialokasikan (Indexed). |
| `StartTime` | `TIME(0)` | Waktu mulai pemeriksaan (Indexed). |
| `EndTime` | `TIME(0)` | Waktu selesai pemeriksaan (Indexed). |
| `DurationMinutes` | `INT` | Total durasi pemeriksaan (menit). |
| `TurnaroundBufferMinutes` | `INT` | Durasi buffer desinfeksi/turnaround alat (menit). |
| `PreparationNotes` | `VARCHAR(500)` | Instruksi persiapan pasien (puasa, obat, dll.). |
| `LabRequirements` | `VARCHAR(200)` | Persyaratan hasil lab (eGFR, serum kreatinin). |
| `SafetyChecklistJson` | `VARCHAR(MAX)` | Snapshot JSON checklist skrining keselamatan pra-tindakan. |
| `IsArrived` | `BIT` | Penanda pasien telah tiba di instalasi radiologi (0 = False, 1 = True). |
| `ArrivedAt` | `DATETIME` | Waktu pencatatan kedatangan pasien (NULL jika belum). |
| `IsCleared` | `BIT` | Penanda lolos skrining keselamatan pra-tindakan (0 = False, 1 = True). |
| `ClearanceNotes` | `VARCHAR(500)` | Catatan hasil verifikasi skrining keselamatan. |
| `ClearedUserId` | `VARCHAR(50)` | Identitas petugas yang memverifikasi skrining keselamatan. |
| `ClearedAt` | `DATETIME` | Waktu verifikasi skrining keselamatan. |
| `Status` | `INT` | Status jadwal: 1 = `Scheduled`, 2 = `Completed`, 3 = `Cancelled` (Indexed). |
| `RescheduleCount` | `INT` | Jumlah frekuensi jadwal telah dipindahkan (Default: 0). |
| `CancellationCategory` | `VARCHAR(50)` | Kategori pembatalan (`Pasien Batal`, `Kondisi Pasien Tidak Stabil`, `Mesin Rusak`, `No-Show`, dll.). |
| `CancellationReason` | `VARCHAR(200)` | Keterangan rinci alasan pembatalan. |
| `CreatedUserId` | `VARCHAR(50)` | User pembuat jadwal awal. |
| `CreatedDate` | `DATETIME` | Waktu pembuatan jadwal awal. |
| `ModifiedUserId` | `VARCHAR(50)` | User pengubah terakhir. |
| `ModifiedDate` | `DATETIME` | Waktu pengubahan terakhir. |

### 2. `BILRG_RadEquipment`
Tabel master lokal referensi mesin diagnostik fisik instalasi radiologi.

| Kolom | Tipe Data | Keterangan |
|---|---|---|
| `EquipmentId` | `VARCHAR(10)` | Primary Key (misal: `EQ-CT01`, `EQ-MRI01`, `EQ-USG01`). |
| `EquipmentName` | `VARCHAR(100)` | Nama mesin radiologi (misal: 'CT-Scan 128 Slice Siemens'). |
| `Modality` | `VARCHAR(20)` | Modalitas mesin (`CT`, `MRI`, `USG`, `Fluoroscopy`, `CR`). |
| `RoomId` | `VARCHAR(10)` | Identifier ruangan fisik. |
| `RoomName` | `VARCHAR(100)` | Nama ruangan fisik (misal: 'Ruang CT-Scan 1'). |
| `IsActive` | `BIT` | Status operasional mesin (1 = Aktif, 0 = Rusak/Maintenance). |

### 3. `BILRG_RadJadwalReschedule`
Tabel audit trail riwayat pemindahan jadwal (*reschedule history*).

| Kolom | Tipe Data | Keterangan |
|---|---|---|
| `RescheduleId` | `VARCHAR(16)` | Primary Key (Format: `RSC` + NunaId). |
| `JadwalRadId` | `VARCHAR(16)` | Foreign Key ke `BILRG_RadJadwal.JadwalRadId` (Indexed). |
| `RescheduleIndex` | `INT` | Urutan revisi ke-N (1, 2, ...). |
| `OldScheduleDate` | `DATE` | Tanggal pemeriksaan sebelumnya. |
| `OldStartTime` | `TIME(0)` | Waktu mulai sebelumnya. |
| `OldEndTime` | `TIME(0)` | Waktu selesai sebelumnya. |
| `OldEquipmentId` | `VARCHAR(10)` | Mesin fisik sebelumnya. |
| `OldEquipmentName` | `VARCHAR(100)` | Nama mesin fisik sebelumnya. |
| `Reason` | `VARCHAR(200)` | Alasan resmi pemindahan jadwal. |
| `RescheduledBy` | `VARCHAR(50)` | User yang mengeksekusi pemindahan. |
| `RescheduledAt` | `DATETIME` | Waktu pencatatan reschedule. |

## Modified Tables

### `BILRG_RadOrder`
- Tidak memerlukan penambahan kolom baru pada skema basis data fisik karena tabel `BILRG_RadOrder` telah memiliki kolom `RadOrderStatus` (yang sudah mendukung status `Scheduled = 2`).
- Pada lapisan kode aplikasi dan domain, metode `Unschedule()` diperkenalkan untuk memodifikasi nilai `RadOrderStatus` kembali menjadi `Requested = 1`.

## Relationships

```text
+-----------------------+              +-----------------------+
|    BILRG_RadOrder     |              |   BILRG_RadEquipment  |
+-----------------------+              +-----------------------+
| PK OrderId            |              | PK EquipmentId        |
|    OrderNo            |              |    EquipmentName      |
|    RadOrderStatus     |              |    Modality           |
+-----------+-----------+              +-----------+-----------+
            | 1                                    | 1
            |                                      |
            | 1..* (Histori)                       | 1..* (Alokasi)
            v                                      v
+-----------+--------------------------------------+-----------+
|                        BILRG_RadJadwal                       |
+--------------------------------------------------------------+
| PK JadwalRadId                                               |
|    ScheduleNo                                                |
| FK OrderId                                                   |
| FK EquipmentId                                               |
|    ScheduleDate, StartTime, EndTime                          |
|    IsArrived, IsCleared, Status, RescheduleCount             |
+------------------------------+-------------------------------+
                               | 1
                               | 0..*
                               v
+------------------------------+-------------------------------+
|                  BILRG_RadJadwalReschedule                   |
+--------------------------------------------------------------+
| PK RescheduleId                                              |
| FK JadwalRadId                                               |
|    OldScheduleDate, OldStartTime, OldEndTime, Reason         |
+--------------------------------------------------------------+
```

## Migration Considerations

1. **Idempotent DDL Execution**: Seluruh skrip migrasi tabel SQL Server ditulis dengan pelindung `IF NOT EXISTS (...) CREATE TABLE ...`.
2. **Indexing Composite**: Dibuat indeks gabungan pada `BILRG_RadJadwal(EquipmentId, ScheduleDate, Status, StartTime, EndTime)` untuk menjamin performa sub-milidetik kueri pengecekan benturan waktu (*collision detection*).
3. **Seed Master Data Awal**: Disediakan skrip seed awal untuk mengisi master mesin fisik contoh (`BILRG_RadEquipment`) untuk modalitas CT, MRI, USG, dan Fluoroskopi.

---

# 9. Cross-Cutting Concerns

1. **Concurrency & Race Condition Control**:
   - Pengecekan overlap dan penyimpanan reservasi slot dibungkus dalam kuncian transaksi database `HOLDLOCK, UPDLOCK` agar dua pengguna yang memilih slot waktu beririsan pada detik yang sama tidak menghasilkan *double-booking*.
2. **Audit Trail Reschedule & Cancellation**:
   - Pemindahan jadwal dan pembatalan bersifat non-destruktif. Setiap aksi mencatat identitas user, stempel waktu, slot lama, dan alasan resmi.
3. **Resilience & Fault Isolation**:
   - Kegagalan pengiriman notifikasi ke `ADM-TRACKER` tidak membatalkan komit transaksi pemesanan jadwal radiologi.
4. **Data Normalization & DTO Validation**:
   - Seluruh payload request API divalidasi ketat menggunakan FluentValidation di backend dan Zod di frontend.

---

# 10. Implementation Constraints

1. **Framework & Standard**:
   - Backend: C# .NET 8 / 9, Pragmatic Tactical DDD, explicit persistence SQL Server menggunakan `System.Data.SqlClient` + Dapper murni (`Nuna.Lib.DataAccessHelper`, `MayBe<T>`, `TransactionScope`). Dilarang menggunakan Entity Framework Core.
   - Pemetaan Tipe Tanggal & Waktu Dapper: Karena `System.Data.SqlClient` tidak mendukung langsung `DateOnly`/`TimeOnly`, DTO/DAL wajib memetakan `ScheduleDate` sebagai `DateTime` dan `StartTime`/`EndTime` sebagai `TimeSpan` (atau `string` terformat), lalu mengonversinya ke `DateOnly`/`TimeOnly` pada model domain.
   - Registrasi DI & Kontrak API: Seluruh registrasi DI (`IRadJadwalRepo`, `IRadJadwalDal`, `IRadEquipmentDal`, `IRadTrackerIntegration`) ditempatkan di `Bilreg.Api/Configurations/InfrastructureService.cs`. Controller menggunakan `[Authorize]` dan membungkus respons dengan `JSendOk` / `JSendFail` (`Nuna.Lib.ActionResultHelper`) selaras dengan `RadOrderController`.
   - Frontend: Vue 3 (Composition API dengan `<script setup lang="ts">`), Tailwind CSS v4, TanStack Query v5, Zod, Luxon untuk manipulasi waktu.
2. **Directory & Workspace Protocol**:
   - Backend: File ditempatkan pada bounded context `src/bilreg/Bilreg.*/RadContext/RadJadwalFeature`.
   - Frontend: Seluruh perkakas frontend (`pnpm`, `vue-tsc`, `vitest`) dijalankan strictly di dalam subdirektori `c012_myhospital_web`.
3. **No Redundant Regression Tests**:
   - Implementer dilarang menjalankan pengujian penuh seluruh workspace (`pnpm test` bare). Hanya unit test spesifik yang ditargetkan pada fitur yang dimodifikasi.

---

# 11. Acceptance Conditions

Arsitektur ini dianggap terimplementasi secara lengkap apabila:
1. **AC-01 (Creation)**: Endpoint `POST api/rad/jadwal` berhasil membentuk rekaman jadwal unik `JadwalRadId`, `ScheduleNo`, mengikat `OrderId`, dan mengubah status order menjadi `Scheduled`.
2. **AC-02 (Allocation)**: Alokasi mesin fisik, ruangan, dan dokter Sp.Rad tersimpan dengan `StartTime` dan `EndTime` terhitung otomatis (+ buffer desinfeksi).
3. **AC-03 (No-Overlap Invariant)**: Sistem secara konsisten menolak (HTTP 409 Conflict / Domain Exception) upaya pembuatan atau pemindahan jadwal yang tumpang tindih waktu pada mesin yang sama.
4. **AC-04 (Selective Scheduling)**: Order berprioritas Cito dan pemeriksaan X-Ray rutin dapat langsung diproses tanpa kewajiban reservasi slot di `RadJadwal`.
5. **AC-05 (Confirmation Print)**: Frontend web dapat mencetak Lembar Konfirmasi Jadwal dan Panduan Persiapan Pasien.
6. **AC-06 (Reschedule Audit)**: Endpoint `POST api/rad/jadwal/{id}/reschedule` memindahkan slot jadwal, menaikkan `RescheduleCount`, melepaskan slot lama, dan mencatat histori pada `BILRG_RadJadwalReschedule`.
7. **AC-07 (Decoupled Cancellation)**: Endpoint `POST api/rad/jadwal/{id}/cancel` membatalkan jadwal, melepaskan slot mesin, dan memanggil `RadOrder.Unschedule()` sehingga order kembali berstatus `Requested`.
8. **AC-08 (Clearance Gate & Milestone)**: Endpoint `POST api/rad/jadwal/{id}/clearance` memvalidasi `IsArrived` dan `IsCleared`, mentransisikan status menjadi `Completed`, dan mempublikasikan status ke `ADM-TRACKER`.
