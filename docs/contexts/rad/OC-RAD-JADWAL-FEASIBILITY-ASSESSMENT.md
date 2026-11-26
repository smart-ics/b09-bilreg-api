---
Title: Feasibility Assessment: Penjadwalan Pemeriksaan Radiologi (JadwalRadiologi)
Code: OC-RAD-JADWAL
Artifact: FEASIBILITY-ASSESSMENT
Version: 1.0
LastUpdated: 2026-11-25
Status: READY-FOR-PLANNING
---

# 1. Request Summary

Penilaian kelayakan terhadap implementasi fitur Penjadwalan Pemeriksaan Radiologi (*Radiology Examination Scheduling*) untuk merealisasikan Outcome kanonikal [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md).

Referenced artifacts:
- DOMAIN: [08-RADIOLOGI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/08-RADIOLOGI-DOMAIN.md) (`RAD-JADWAL`, `RAD-ORDER`, `RAD-EXAM`, `RAD-EXPERTISE`)
- FEATURE: [OC-RAD-JADWAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEATURE.md)
- ISSUE: [OC-RAD-JADWAL-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-ISSUE.md)
- CANONICAL OUTCOME: [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md)

## Objective

Menilai kelayakan teknis, menganalisis kesenjangan (*gap analysis*), mengidentifikasi risiko, asumsi, dan pertanyaan terbuka, serta menyusun rekomendasi keputusan penutupan gap (*gap closure decisions*) untuk merealisasikan kapabilitas `RAD-JADWAL` ke dalam bounded context `RadContext` di backend `b09-bilreg-api` dan modul `Radiology` di frontend `c012_myhospital_web`.

---

# 2. Current State

## Existing Behavior

### 1. Backend (`b09-bilreg-api`):
- Bounded context `RadContext` saat ini telah mengimplementasikan kapabilitas manajemen order radiologi melalui `RadOrderFeature` ([RadOrderModel.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/RadContext/RadOrderFeature/RadOrderModel.cs), [RadOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/RadContext/RadOrderController.cs), dan tabel `BILRG_RadOrder`).
- Enum status order ([RadOrderStatusEnum.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/RadContext/RadOrderFeature/RadOrderStatusEnum.cs)) telah mencakup nilai `Requested = 1`, `Scheduled = 2`, `Cancelled = 3`, dan `Charged = 4`.
- Pada `RadOrderModel.cs`, terdapat metode `Schedule(string userId, DateTime scheduledAt)` primitif yang memvalidasi `RadOrderStatus == Requested` dan mengubah status menjadi `Scheduled`. Namun, metode ini tidak menyimpan alokasi mesin, ruangan, dokter penanggung jawab, waktu mulai/selesai, checklist keselamatan, maupun catatan persiapan.
- Tidak terdapat metode untuk mengembalikan status order kembali ke `Requested` saat sebuah jadwal dibatalkan atau dipindahkan (*decoupled unschedule*).
- Belum ada modul, aggregate, repository, tabel basis data SQL, atau endpoint API untuk entitas `RadJadwal` (*JadwalRadiologi*) di `b09-bilreg-api`.

### 2. Frontend (`c012_myhospital_web`):
- Terdapat modul fungsional di `src/modules/Radiology` dengan komponen antarmuka:
  - [RadSchedulingPanel.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/components/RadSchedulingPanel.vue): Menampilkan visualisasi lini masa (*timeline*) per modalitas (`xray`, `usg`, `ct`, `mri`) per jam operasional (08:00 - 17:00).
  - [TindakanRadiologi.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/views/TindakanRadiologi.vue): Menampilkan daftar kerja order, panel aksi pemeriksaan, serta filter modalitas dan status.
- Seluruh data jadwal dan order pada frontend saat ini masih berbasis data tiruan lokal (*mock data*) di in-memory state (`INITIAL_ORDERS`, `mockRadOrders.ts`), menggunakan pengenal numerik tiruan, dan belum terhubung ke API backend riil melalui TanStack Query.

## Existing Constraints

1. **Arsitektur Backend**: Backend `b09-bilreg-api` menerapkan standar Pragmatic Tactical DDD, C# .NET, explicit persistence SQL Server via Dapper/ADO.NET murni, dan isolasi domain yang ketat.
2. **Kemandirian Registrasi**: Jadwal radiologi dapat dibuat untuk kunjungan mendatang (*advance booking*) berdasarkan rekam medis pasien (`PAS-DATSOS`) dan nomor order, sebelum nomor registrasi kunjungan hari-H aktif diterbitkan.
3. **Pemisahan Wewenang Domain**: `RAD-JADWAL` tidak boleh mengelola eksekusi tombol fisik mesin/pengiriman citra DICOM (`RAD-EXAM`), pembacaan ekspertise (`RAD-EXPERTISE`), penagihan tarif uang (`TRK-BILLING`), atau shift jaga perawat/radiografer (`ORG-PPA`).

---

# 3. Gap Analysis

| ID | Severity | Gap |
|------|------|------|
| **GAP-001** | CRITICAL | **Ketiadaan Aggregate Persisten `RadJadwal` di Backend**: Belum ada model domain, interface repository, migrasi tabel basis data SQL (`BILRG_RadJadwal`), use case application, maupun REST API controller untuk entitas jadwal radiologi mandiri. |
| **GAP-002** | CRITICAL | **Ketiadaan Validasi Invarian No-Overlap Waktu & Alokasi Mesin**: Belum ada mekanisme logika validasi ketiadaan tabrakan slot waktu (*no-collision/overlap detection*) per mesin fisik spesifik (`EquipmentId`) pada rentang `StartTime` hingga `EndTime` (+ buffer desinfeksi). |
| **GAP-003** | MAJOR | **Ketidakselarasan Siklus Hidup Order vs Pembatalan Jadwal**: `RadOrderModel` belum memiliki metode `Unschedule()` untuk mengembalikan status dari `Scheduled` ke `Requested` saat jadwal dibatalkan, serta belum ada relasi kaskade saat order dibatalkan agar slot jadwal otomatis terlepas. |
| **GAP-004** | MAJOR | **Ketiadaan Perekaman Panduan Persiapan & Checklist Keselamatan (Clearance Gate)**: Belum ada struktur penyimpanan panduan pasien (puasa, eGFR) dan checklist keselamatan (implan logam, alergi, kehamilan) beserta penanda kesiapan klinis (`IsArrived`, `IsCleared`, `ClearanceNotes`). |
| **GAP-005** | MAJOR | **Ketiadaan Jejak Audit Penjadwalan Ulang (*In-Place Reschedule*) & Pembatalan Bersebab**: Belum ada mekanisme audit untuk mempertahankan `JadwalRadId` yang sama saat pemindahan jadwal sambil mencatat `RescheduleCount`, slot lama, dan alasan resmi pembatalan. |
| **GAP-006** | MINOR | **Ketiadaan Publikasi Milestone ke `ADM-TRACKER`**: Transisi status jadwal belum terhubung dengan pengiriman pembaruan tonggak perjalanan pasien terpusat. |
| **GAP-007** | MAJOR | **Frontend UI Masih Berbasis Mock Data Tanpa Integrasi API Riil**: Komponen `RadSchedulingPanel.vue` dan `TindakanRadiologi.vue` belum mengonsumsi endpoint API backend melalui TanStack Query dan DTO kontrak resmi. |

---

# 4. Open Questions

| ID | Question | Impact |
|------|------|------|
| **OQ-001** | Bagaimana resolusi master data mesin diagnostik fisik (`EquipmentId`) dan ruangan radiologi (`RoomId`) di `b09-bilreg-api`? | Menentukan apakah `RadContext` menggunakan tabel referensi master mesin lokal (`BILRG_RadEquipment`) atau mengonsumsi adapter lookup layanan organisasi (`ORG-LAYANAN`). |
| **OQ-002** | Bagaimana penentuan durasi turnaround buffer pembersihan/desinfeksi alat antar pasien? | Menentukan apakah buffer waktu dihitung otomatis berdasarkan konfigurasi standar modalitas (misal: MRI 15 menit, CT 10 menit) atau diinputkan manual oleh petugas loket saat menjadwalkan. |
| **OQ-003** | Di mana tanggung jawab pencetakan Lembar Konfirmasi Jadwal dan Panduan Persiapan Pasien ditempatkan? | Menentukan apakah pencetakan lembar konfirmasi menggunakan fitur cetak langsung browser berbasis template HTML/CSS di frontend atau melalui dokumen PDF yang digenerate oleh backend service. |

---

# 5. Assumptions

| ID | Assumption |
|------|------|
| **ASM-001** | Setiap mesin diagnostik radiologi fisik yang dapat dijadwalkan (CT-Scan, MRI, USG, Fluoroskopi) memiliki identifier unik dan terhubung ke kategori modalitas yang konsisten dengan order. |
| **ASM-002** | Estimasi durasi pemeriksaan dapat dihitung dari akumulasi estimasi waktu tindakan pada item order radiologi ditambah buffer turnaround alat standar. |
| **ASM-003** | Penjadwalan advance booking dapat dilakukan sebelum registrasi rawat jalan hari-H dibuka, asalkan nomor rekam medis (RM) dan identitas pasien valid pada `PAS-DATSOS`. |
| **ASM-004** | Order dengan tingkat urgensi CITO atau pemeriksaan X-Ray konvensional rutin dapat langsung dilayani di `RAD-EXAM` tanpa melalui pembentukan rekaman `RadJadwal`. |

---

# 6. Risks

| ID | Risk | Impact | Mitigation |
|------|------|------|------|
| **RISK-001** | Terjadinya *Race Condition / Double Booking* saat dua petugas secara bersamaan menjadwalkan mesin yang sama pada rentang waktu yang beririsan. | Dua pasien datang pada jam yang sama untuk mesin yang sama, menyebabkan komplain dan gangguan pelayanan klinis. | Terapkan transaksi atomik atau kuncian tingkat baris (*table/index range lock*) di basis data SQL Server saat validasi overlap dilakukan sebelum insert/update jadwal. |
| **RISK-002** | Pasien tiba di instalasi pada hari-H tanpa kepatuhan persiapan klinis (misal: belum puasa atau nilai eGFR belum keluar). | Pemeriksaan tertunda, mesin menganggur (*idle resource*), atau tindakan kontras berisiko fatal bagi pasien. | Terbitkan Lembar Panduan Persiapan tertulis saat jadwal dibuat, dan kunci status `Completed` dengan gerbang kesiapan klinis (`IsCleared = true`). |
| **RISK-003** | Pembatalan jadwal tidak sinkron dengan status `RadOrderModel`, menyebabkan order menggantung pada status `Scheduled` tanpa ada slot mesin aktif. | Pasien tidak dapat dijadwalkan ulang atau ditagihkan karena validasi status order menolak aksi lanjutan. | Wajibkan use case pembatalan jadwal memanggil metode domain resmi `Unschedule()` pada `RadOrderModel` dalam satu batas transaksi. |

---

# 7. Recommendations

## Option A: Membentuk Aggregate Terpisah `RadJadwal` di Bounded Context `RadContext` (Direkomendasikan)
Membangun aggregate root `RadJadwalModel` yang mandiri di dalam `RadContext`, memiliki siklus hidup sendiri (`Scheduled`, `Completed`, `Cancelled`), tabel persisten sendiri `BILRG_RadJadwal`, dan terikat ke `OrderId` sebagai referensi.

### Advantages:
- Mematuhi prinsip Tactical DDD dan separation of concerns: order mengurus permintaan klinis, jadwal mengurus alokasi kapasitas waktu dan mesin fisik.
- Memungkinkan pembatalan jadwal secara independen (*decoupled cancellation*) tanpa merusak data order klinis DPJP.
- Memfasilitasi jejak audit pemindahan jadwal (*reschedule history*) dan checklist skrining keselamatan tanpa membebani tabel order.
- Mempermudah query visualisasi kalender mesin/worklist scheduler yang terisolasi dari query transaksi order harian.

### Disadvantages:
- Memerlukan sinkronisasi status antara aggregate `RadJadwal` dan `RadOrder` melalui domain handler atau use case orchestration.

---

## Option B: Memperluas Aggregate `RadOrderModel` dengan Kolom Alokasi Jadwal
Menambahkan atribut `EquipmentId`, `RoomId`, `ScheduleDate`, `StartTime`, `EndTime`, dan status persiapan langsung ke dalam tabel `BILRG_RadOrder`.

### Advantages:
- Tidak memerlukan tabel basis data baru; seluruh data tersimpan dalam satu entitas.

### Disadvantages:
- Melanggar batas tanggung jawab domain: order klinis menjadi terikat erat dengan alokasi operasional alat.
- Sulit menangani riwayat reschedule *in-place* dan pembatalan parsial.
- Memperumit pencarian konflik tumpang tindih waktu pada mesin radiologi karena tabel order memuat seluruh data non-jadwal.

---

# 8. Gap Closure

## GAP-001: Ketiadaan Aggregate Persisten `RadJadwal` di Backend
### Decision:
Membentuk aggregate baru `RadJadwalModel` di bawah namespace `Bilreg.Domain.RadContext.RadJadwalFeature`, interface `IRadJadwalRepo` dan DAL di `Bilreg.Infrastructure`, skrip tabel `BILRG_RadJadwal` di `Bilreg.SqlDb`, serta REST API `RadJadwalController` di `Bilreg.Api`.
### Rationale:
Sesuai rekomendasi Opsi A dan kanonikal [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md), `JadwalRadiologi` adalah fakta bisnis persisten mandiri.
### Impact:
Penambahan file komponen baru pada seluruh lapisan backend `b09-bilreg-api`.
### Architecture Impact:
Target arsitektur wajib memetakan aggregate `RadJadwalModel`, field-field transaksi, serta kontrak DTO API.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## GAP-002: Ketiadaan Validasi Invarian No-Overlap Waktu & Alokasi Mesin
### Decision:
Menambahkan query ketersediaan slot (*collision detection*) pada `IRadJadwalDal` yang memverifikasi bahwa tidak ada jadwal berstatus `Scheduled` pada `EquipmentId` yang sama di mana rentang `(StartTime < NewEndTime AND EndTime > NewStartTime)` bernilai benar pada tanggal yang sama.
### Rationale:
Mencegah *double-booking* alat fisik radiologi yang bernilai tinggi dan berkapasitas terbatas.
### Impact:
Logika validasi pra-penyimpanan wajib menolak eksekusi dan melempar exception bisnis jika terjadi tabrakan waktu.
### Architecture Impact:
Arsitektur wajib menetapkan spesifikasi algoritma kueri deteksi tumpang-tindih waktu dan penanganan toleransi buffer.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## GAP-003: Ketidakselarasan Siklus Hidup Order vs Pembatalan Jadwal
### Decision:
Menambahkan metode `Unschedule(string userId, DateTime unscheduledAt)` pada `RadOrderModel` yang mentransisikan kembali `RadOrderStatus` dari `Scheduled` menjadi `Requested`, serta merekam jejak audit pembatalan jadwal.
### Rationale:
Sesuai aturan bisnis, pembatalan jadwal melepaskan slot mesin dan mengembalikan order ke status belum terjadwal tanpa membatalkan permintaan klinis DPJP.
### Impact:
Modifikasi pada `RadOrderModel.cs` dan penambahan unit test siklus hidup pada `Bilreg.Test`.
### Architecture Impact:
Arsitektur wajib memodelkan use case pembatalan jadwal yang mengorkestrasikan pembaruan `RadJadwal` dan pemanggilan `RadOrder.Unschedule()`.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## GAP-004: Ketiadaan Perekaman Panduan Persiapan & Checklist Keselamatan
### Decision:
Merekam panduan persiapan (teks instruksi, prasyarat lab seperti eGFR/kreatinin) dan checklist skrining keselamatan (implan feromagnetik, alergi kontras, kehamilan) sebagai bagian dari entitas `RadJadwal`, dilengkapi dengan penanda `IsArrived`, `IsCleared`, dan `ClearanceNotes`.
### Rationale:
Menjamin keselamatan pasien (*patient safety clearance gate*) sebelum pasien memasuki ruang pemindaian radiologi.
### Impact:
Kolom checklist dan catatan persiapan ditambahkan pada tabel `BILRG_RadJadwal`.
### Architecture Impact:
Spesifikasi DTO dan kontrak antarmuka API wajib mengakomodasi struktur data checklist keselamatan.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## GAP-005: Ketiadaan Jejak Audit Reschedule In-Place & Alasan Pembatalan
### Decision:
Menyediakan properti `RescheduleCount`, `PreviousScheduleDate`, `PreviousStartTime`, `PreviousEndTime`, `PreviousEquipmentId`, `RescheduleReason`, serta kategori pembatalan resmi (`CancellationCategory`) pada model `RadJadwal`.
### Rationale:
Mendukung pelacakan operasional dan audit kepatuhan tanpa menghapus histori jadwal sebelumnya.
### Impact:
Tabel dan model domain mencatat mutasi jadwal secara non-destruktif.
### Architecture Impact:
Use case `RadJadwalRescheduleCmd` dan `RadJadwalCancelCmd` didefinisikan secara eksplisit pada arsitektur.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## GAP-006: Ketiadaan Publikasi Milestone ke ADM-TRACKER
### Decision:
Membuat adapter integrasi ke service pelacak pasien (`IPasienTrackerService`) yang mempublikasikan milestone saat jadwal berhasil dibuat (`Scheduled`), dijadwalkan ulang (`Rescheduled`), dibatalkan (`Cancelled`), atau diselesaikan (`Completed`).
### Rationale:
Menjamin visibilitas alur pelayanan pasien terintegrasi di seluruh instalasi rumah sakit.
### Impact:
Penambahan panggilan service integrasi pada handler use case penjadwalan.
### Architecture Impact:
Arsitektur menetapkan kontrak integrasi antara `RadContext` dan `PasienTrackerContext`.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## GAP-007: Frontend UI Masih Berbasis Mock Data Tanpa Integrasi API Riil
### Decision:
Memperbarui modul `Radiology` di `c012_myhospital_web` dengan membuat service API klien, TanStack Query hooks (`useRadJadwalQuery`, `useCreateRadJadwalMutation`, `useRescheduleRadJadwalMutation`, `useCancelRadJadwalMutation`), dan menghubungkannya ke komponen visual [RadSchedulingPanel.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/components/RadSchedulingPanel.vue) serta [TindakanRadiologi.vue](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology/views/TindakanRadiologi.vue).
### Rationale:
Menyediakan antarmuka operasional yang responsif dan terhubung langsung ke basis data persisten backend.
### Impact:
Penghapusan mock data lokal di frontend dan standardisasi skema DTO menggunakan TypeScript / Zod.
### Architecture Impact:
Arsitektur wajib memetakan endpoint REST API dan struktur JSON response yang dikonsumsi frontend.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## OQ-001: Resolusi Master Data Mesin Fisik & Ruangan Radiologi
### Decision:
Pada tahap awal, `RadContext` menyediakan tabel master referensi lokal `BILRG_RadEquipment` yang mendefinisikan mesin fisik (`EquipmentId`, `EquipmentName`, `Modality`, `RoomId`, `RoomName`, `IsActive`) untuk menjamin kedaulatan domain radiologi tanpa dependensi langsung yang rapuh ke tabel organisasi lain.
### Rationale:
Menjaga prinsip Tactical DDD bounded context independence dan kecepatan respon validasi tabrakan jadwal.
### Impact:
Penambahan migrasi tabel referensi mesin radiologi dan endpoint lookup master mesin.
### Architecture Impact:
Arsitektur memodelkan `RadEquipment` sebagai entitas pendukung di `RadContext`.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## OQ-002: Penentuan Durasi Turnaround Buffer Pembersihan Mesin
### Decision:
Sistem menetapkan nilai default *Turnaround Buffer* sebesar 10 menit untuk CT/USG/Fluoroskopi dan 15 menit untuk MRI secara otomatis, dengan fleksibilitas bagi petugas loket untuk menyesuaikan durasi akhir sebelum reservasi dikonfirmasi.
### Rationale:
Memberikan estimasi waktu selesai yang realistis dan mencegah benturan jadwal akibat proses pembersihan atau reposisi pasien.
### Impact:
Kalkulasi `EndTime = StartTime + TotalItemDuration + TurnaroundBuffer`.
### Architecture Impact:
Dicantumkan dalam spesifikasi invarian dan use case pembuatan jadwal.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

## OQ-003: Mekanisme Cetak Lembar Konfirmasi Jadwal
### Decision:
Pencetakan Lembar Konfirmasi Jadwal dan Panduan Persiapan Pasien dilaksanakan langsung pada frontend web (`c012_myhospital_web`) menggunakan komponen print dialog HTML/CSS yang memformat data DTO jadwal terverifikasi.
### Rationale:
Memberikan kecepatan respon operasional instan bagi petugas loket tanpa menambah overhead komputasi render PDF pada backend server.
### Impact:
Komponen cetak konfirmasi jadwal disediakan di frontend.
### Architecture Impact:
Tidak memerlukan service PDF backend baru.
### Resolved By:
Analyst & Domain Spec Alignment
### Resolved Date:
2026-11-25

---

# 9. Architecture Applicability

## Decision
**ARCHITECTURE-REQUIRED**

## Rationale
Implementasi fitur `OC-RAD-JADWAL` membutuhkan:
1. Pembentukan aggregate root baru `RadJadwalModel` dan tabel basis data baru `BILRG_RadJadwal`.
2. Penegakan invarian bisnis baru mengenai deteksi benturan waktu (*collision-free scheduling*) dan alokasi mesin fisik.
3. Modifikasi metode siklus hidup pada aggregate eksisting `RadOrderModel` (`Unschedule`).
4. Penyusunan use case application commands/queries baru, controller REST API, dan integrasi frontend TanStack Query.
Perubahan ini merupakan modifikasi struktural dan penambahan komponen baru yang wajib dirancang secara formal melalui dokumen **ARCHITECTURE**.

---

# 10. Planning Readiness

## Readiness Checklist
- [x] All critical gaps resolved
- [x] All required decisions recorded
- [x] All blocking open questions resolved
- [x] Architecture can be finalized or updated

## Status
READY-FOR-PLANNING

## Notes
Seluruh gap kritis (GAP-001 s/d GAP-007) dan pertanyaan terbuka (OQ-001 s/d OQ-003) telah dianalisis secara komprehensif, dikonfirmasi melalui sesi alignment /grill-me, dan ditutup secara definitif. Status gerbang READY-FOR-PLANNING secara resmi disahkan oleh peran ica-architect. Dokumen target teknis ARCHITECTURE dapat disusun untuk menjadi acuan perancangan implementasi.

---

# 11. References

Referenced artifacts:
- [08-RADIOLOGI-DOMAIN.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/domain/08-RADIOLOGI-DOMAIN.md)
- [OC-RAD-JADWAL-FEATURE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-FEATURE.md)
- [OC-RAD-JADWAL-ISSUE.md](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/docs/contexts/rad/OC-RAD-JADWAL-ISSUE.md)
- [OC-RAD-JADWAL.md](file:///D:/Project.Aktif/_hospital_web/b21-myhosweb-system/outcomes/OC-RAD-JADWAL.md)

Referenced codebase locations:
- [Bilreg.Domain/RadContext/RadOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Domain/RadContext/RadOrderFeature)
- [Bilreg.Application/RadContext/RadOrderFeature](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Application/RadContext/RadOrderFeature)
- [Bilreg.Api/Controllers/RadContext/RadOrderController.cs](file:///D:/Project.Aktif/_hospital_web/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/RadContext/RadOrderController.cs)
- [c012_myhospital_web/src/modules/Radiology](file:///D:/Project.Aktif/_hospital_web/c012_myhospital_web/src/modules/Radiology)
