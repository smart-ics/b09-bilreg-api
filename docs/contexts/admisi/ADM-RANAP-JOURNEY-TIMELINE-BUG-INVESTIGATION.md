# BUG-INVESTIGATION

## Context

Issue: [ADM-RANAP-JOURNEY-TIMELINE-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/ADM-RANAP-JOURNEY-TIMELINE-BUG-ISSUE.md) (ISS-ADM-REG-008)  
Problem Summary: Stempel waktu `occurredAt` pada array `timeline[]` pada respons endpoint `GET /api/admisi-ranap/journeys/{journeyId}` bernilai identik untuk semua peristiwa pada suatu entitas (misal pembuatan reservasi `kind 3`, realisasi reservasi `kind 6`, dan pendaftaran admisi `kind 7` bernilai sama persis), padahal data di database memiliki perbedaan waktu `CrtDate` dan `UpdDate` yang nyata.

## Current State

Saat ini, pada endpoint pemantauan perjalanan pasien rawat inap (`GET /api/admisi-ranap/journeys/{journeyId}`):

1. **Pemanggilan Respons API**:
   - Respons API mengembalikan objek `JourneyDetailWorkspace` yang memuat array `timeline` (`JourneyTimelineEvent[]`).
   - Setiap elemen `JourneyTimelineEvent` memiliki properti `kind`, `occurredAt`, `relatedRecordId`, dan `summary`.
   - Pada kasus pengujian dengan data nyata `rsv:RSV097XD67QY`:
     - Peristiwa `kind 3 (Reservasi dibuat)`: `occurredAt = 2026-02-11T09:18:51.38`
     - Peristiwa `kind 6 (Reservasi direalisasikan)`: `occurredAt = 2026-02-11T09:18:51.38`
     - Peristiwa `kind 7 (Admisi didaftarkan)`: `occurredAt = 2026-02-11T09:18:51.38`
2. **Kondisi Basis Data Riil**:
   - Tabel `BILRG_AdmReservation`:
     - `CrtDate = 2026-02-11 09:16:57.760` (Waktu reservasi dibuat oleh petugas).
     - `UpdDate = 2026-02-11 09:18:51.380` (Waktu status reservasi diubah menjadi Realized / direalisasikan menjadi admisi).
     - Terdapat jeda waktu nyata sekitar ~2 menit antara pembuatan dan realisasi.
   - Tabel `BILRG_AdmAdmission`:
     - `AdmissionDate = 2026-02-11 09:18:51.380`
     - `CrtDate = 2026-02-11 09:18:51.380`
     - `UpdDate = 3000-01-01` (Sentinel void)
   - Tabel `BILRG_BedWaitingList`:
     - `CrtDate = 2026-02-11 09:31:13.293`
     - `UpdDate = 3000-01-01` (Sentinel void)
3. **Presentasi di Frontend (`c012_myhospital_web`)**:
   - Komponen `JourneyTimeline.vue` me-render langsung elemen `timeline` dari respons backend dan memformat `event.occurredAt` menggunakan `formatRanapDateTime()`.
   - Akibat stempel waktu yang seragam dari API, antarmuka menampilkan urutan peristiwa dengan jam dan menit yang persis sama, mengaburkan durasi jeda operasional riil dan menimbulkan kesan kesalahan visual pada riwayat pasien.

## Problem Analysis

Investigasi penelusuran kode program dan aliran data menemukan tiga akar penyebab utama:

### 1. Penyusutan Informasi pada Kontrak Fact Agregasi (*Lossy Fact Contracts*)
- Pada berkas `src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/JourneyContracts.cs`:
  - `OpnameRequestJourneyFact` hanya memiliki field `StatusAt`.
  - `ReservationJourneyFact` hanya memiliki field `StatusAt`.
  - `AdmissionJourneyFact` hanya memiliki field `AdmissionAt`.
- Tidak ada properti terpisah untuk mencatat waktu penciptaan entitas (`CreatedAt`) maupun waktu modifikasi/pembatalan admisi (`UpdatedAt`).
- Akibatnya, model fakta yang dihasilkan tidak memiliki kapasitas untuk membawa dua dimensi waktu sekaligus (waktu dibuat vs waktu status berubah).

### 2. Pembuangan Nilai `CrtDate` pada Lapisan DAL (*Data Access Layer*)
- Pada berkas `JourneyDal.cs` (baris 378, 428, 520, 528, 555) dan `JourneyDal.Batch.cs` (baris 327, 390):
  - Kueri `SELECT` SQL sebenarnya sudah membaca kolom `CrtDate` dan `UpdDate` dari tabel `BILRG_AdmOpnameRequest` dan `BILRG_AdmReservation`.
  - Namun, pemetaan ke DTO Fact mengompresi kedua nilai tersebut menggunakan ekspresi kondisional:
    `StatusAt = r.UpdDate == VoidSentinel ? r.CrtDate : r.UpdDate`
  - Begitu entitas mengalami perubahan status (seperti Reservasi menjadi `Realized` atau Opname Request menjadi `Fulfilled`), `UpdDate` menggantikan nilai `CrtDate`. Nilai `CrtDate` asli tidak disimpan ke mana pun dan hilang dari memori agregasi.
  - Untuk entitas Admisi (`BILRG_AdmAdmission`), DAL hanya memetakan `AdmissionAt = r.AdmissionDate`, sementara `CrtDate` dan `UpdDate` tidak disimpan sama sekali ke dalam `AdmissionJourneyFact`.

### 3. Resolver Memakai Ulang Satu Timestamp untuk Peristiwa yang Berbeda Karakter
- Pada berkas `Release1JourneyStageResolver.cs` (`BuildTimeline`, baris 382-473):
  - Peristiwa `JourneyTimelineEventKind.OpnameRequested` (pembuatan opname) mengambil nilai `opn.StatusAt` (yang berisi `UpdDate` jika sudah fulfilled).
  - Peristiwa `JourneyTimelineEventKind.ReservationCreated` (pembuatan reservasi) mengambil nilai `rsv.StatusAt` (yang berisi `UpdDate` jika sudah realized).
  - Peristiwa `JourneyTimelineEventKind.AdmissionUpdated` dan `AdmissionCancelled` mengambil nilai `admission.AdmissionAt` alih-alih `UpdDate` dari rekaman admisi.
  - Hal ini menyebabkan peristiwa pembuatan selalu tertimpa oleh waktu status terakhir.

### 4. Titik Buta pada Pengujian Otomatis (*Test Suite Blind Spot*)
- Pada berkas pengujian integrasi `JourneyDalIntegrationTest.cs` (baris 478-506):
  - Metode pembibitan data uji (`SeedReservation`, `SeedOpnameRequest`, `SeedAdmission`) mengisi kolom `CrtDate` dan `UpdDate` dengan nilai `DateTime.Now` secara serentak.
  - Karena pada data uji `CrtDate == UpdDate`, pengujian tidak pernah mendeteksi bahwa `CrtDate` terbuang saat data diperbarui.
- Pada berkas pengujian unit `Release1JourneyStageResolverTest.cs`, belum ada skenario uji yang meng-assert nilai `occurredAt` pada array `Timeline` hasil resolusi.

## Affected Components

1. **Backend (`b09-bilreg-api`)**:
   - [`JourneyContracts.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/JourneyContracts.cs): Penambahan field `CreatedAt` pada `OpnameRequestJourneyFact` & `ReservationJourneyFact`, serta `CreatedAt` dan `UpdatedAt` pada `AdmissionJourneyFact`.
   - [`JourneyDal.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.cs): Pemetaan `CreatedAt` dan `UpdatedAt` pada metode `LoadOpnameJourney`, `LoadReservationJourney`, dan `AssembleAdmissionBag`.
   - [`JourneyDal.Batch.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs): Pemetaan `CreatedAt` pada pemuatan batch `FoldOpnameJourney` dan `FoldReservationJourney`.
   - [`Release1JourneyStageResolver.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolver.cs): Penyesuaian `BuildTimeline` agar menggunakan `CreatedAt` untuk event pembuatan dan `StatusAt`/`UpdatedAt` untuk event perubahan status.
   - Test Suite:
     - [`Release1JourneyStageResolverTest.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/Release1JourneyStageResolverTest.cs): Penambahan unit test verifikasi disparitas waktu timeline.
     - [`JourneyDalIntegrationTest.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Test/AdmisiRanapContext/JourneyFeature/JourneyDalIntegrationTest.cs): Pembaruan data seed agar `CrtDate != UpdDate` dan assertion timeline.
2. **Frontend (`c012_myhospital_web`)**:
   - [`ranapJourney.ts`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/types/ranapJourney.ts): Tipe data `JourneyTimelineEvent` sudah kompatibel penuh (`occurredAt?: string | null`).
   - [`JourneyTimeline.vue`](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/journey/JourneyTimeline.vue): Komponen presentasi langsung menampilkan stempel waktu yang akurat tanpa perlu perubahan kode.
3. **Basis Data**:
   - Tabel `BILRG_AdmOpnameRequest`, `BILRG_AdmReservation`, `BILRG_AdmAdmission`, `BILRG_BedWaitingList`: Tidak memerlukan perubahan DDL maupun migrasi data (kolom `CrtDate` dan `UpdDate` sudah ada di database).

## Impact Assessment

- **Business Impact**:
  - Rekam jejak kronologis perjalanan pasien rawat inap menjadi akurat dan dapat dipercaya sebagai instrumen audit dan monitoring mutu layanan admisi.
  - Waktu tunggu riil antara permintaan/reservasi dibuat hingga pasien terealisasi masuk admisi dapat diukur secara tepat.
- **Operational Impact**:
  - Petugas loket pendaftaran dan kepala ruangan mendapatkan visibilitas timeline yang jelas tanpa kebingungan akibat timestamp identik semu.
- **Technical Impact**:
  - Beban komputasi nol: Query SQL yang ada sudah mengambil kolom yang dibutuhkan, tidak ada penambahan join, table scan, atau round-trip baru.
  - Kompatibilitas mundur penuh: Struktur kontrak wire JSON `timeline` di API v1 tidak berubah (tetap berformat `kind`, `occurredAt`, `relatedRecordId`, `summary`).

## Assumptions

- **ASM-01**: Kolom `CrtDate` pada seluruh tabel entitas admisi secara andal merepresentasikan waktu pembuatan data pertama kali oleh sistem/pengguna.
- **ASM-02**: Untuk peristiwa `AdmissionRegistered`, stempel waktu yang diutamakan secara bisnis adalah `AdmissionDate` (tanggal medis admisi), dengan fallback ke `CrtDate` jika `AdmissionDate` null.
- **ASM-03**: Kolom `UpdDate` yang bernilai `3000-01-01` (VoidSentinel) menandakan rekaman belum pernah dimodifikasi; nilai ini harus diperlakukan sebagai `null` untuk status update events.
- **ASM-04**: Pengurutan tampilan timeline tetap mempertahankan urutan kategori saat ini (Opname -> Reservasi -> Admisi -> Waiting List) untuk menjaga konsistensi alur visual antarmuka pengguna tanpa pergeseran urutan tak terduga.

## Open Questions & Gap Closures

- **OQ-01 (Penanganan Data Anomali Legacy `UpdDate < CrtDate`)**:
  - *Keputusan*: **CLOSED**. Tampilkan tanggal apa adanya sesuai rekaman basis data tanpa manipulasi atau penyesuaian artifisial (*no artificial min/max swapping or clamping*). Timeline harus merefleksikan rekaman audit trail riil di database.
- **OQ-02 (Prioritas Stempel Waktu `AdmissionRegistered`)**:
  - *Keputusan*: **CLOSED**. Gunakan `AdmissionDate` (tanggal medis rawat inap) sebagai acuan utama, dengan fallback ke `CrtDate` (waktu pencatatan sistem) jika `AdmissionDate` tidak terisi (`admission.AdmissionAt ?? admission.CreatedAt`).
- **OQ-03 (Urutan Elemen Array `timeline[]`)**:
  - *Keputusan*: **CLOSED**. Pertahankan pengurutan berbasis kategori siklus admisi saat ini (Opname -> Reservasi -> Admisi -> Waiting List) untuk menjaga stabilitas kontrak urutan bagi frontend `c012_myhospital_web` dan menghindari regresi visual pada UI.
- **OQ-04 (Cakupan Penambahan Properti `CreatedAt` pada DTO Fact)**:
  - *Keputusan*: **CLOSED**. Penambahan properti `CreatedAt` difokuskan pada 3 entitas yang mengalami peristiwa ganda (*dual-event*): `OpnameRequestJourneyFact`, `ReservationJourneyFact`, dan `AdmissionJourneyFact` (ditambah `UpdatedAt`). Entitas `WaitingListJourneyFact` tetap menggunakan `StatusAt` tunggal karena hanya memproyeksikan satu peristiwa per rekaman antrean.
- **OQ-05 (Versi Endpoint API)**:
  - *Keputusan*: **CLOSED**. Tetap menggunakan API v1 tanpa *version bumping* karena struktur skema respons JSON wire format tidak mengalami perubahan (*non-breaking change*).

## Alternative Evaluation

### Alternatif 1: Kompensasi Waktu pada Antarmuka Frontend (Workaround di Vue)
- **Deskripsi**: Frontend mendeteksi jika `occurredAt` sama persis, lalu menyembunyikan jam pada event kedua atau menambahkan selisih detik artifisial.
- **Kelemahan**: Melanggar prinsip *Single Source of Truth*. Frontend tidak memiliki akses ke `CrtDate` riil karena sudah dibuang oleh backend di lapisan DAL. Menyembunyikan atau memanipulasi waktu adalah anti-pattern dalam sistem audit rumah sakit.
- **Keputusan**: **DITOLAK**.

### Alternatif 2: Introduksi Tabel Log Event Khusus (Full Event Sourcing)
- **Deskripsi**: Merancang tabel riwayat baru (misal `BILRG_AdmJourneyEventLog`) untuk mencatat setiap pergantian status sebagai entitas event individual.
- **Kelemahan**: Skala perubahan terlalu besar (*over-engineering* untuk kebutuhan saat ini), memerlukan migrasi skema tabel DDL, trigger database, dan pengubahan seluruh alur penulisan transaksi admisi.
- **Keputusan**: **DITOLAK**.

### Alternatif 3: Pengayaan Kontrak Fact Agregasi & Koreksi Resolver DAL (Pilihan Terpilih)
- **Deskripsi**: Menambahkan properti waktu inisiasi (`CreatedAt`) dan waktu pembaruan (`UpdatedAt`) pada kontrak fact, memanfaatkan kolom `CrtDate` dan `UpdDate` yang sudah dibaca oleh DAL, dan memperbarui `BuildTimeline` di resolver.
- **Keunggulan**: Sangat terfokus, nol round-trip tambahan ke database, nol perubahan skema basis data, nol breaking change pada API wire format, dan menyelesaikan akar masalah secara tuntas di sumbernya.
- **Keputusan**: **DISETUJUI**.

## Recommended Decision

Menerapkan perbaikan backend sesuai **Alternatif 3**:
1. Menambahkan field `CreatedAt` pada `OpnameRequestJourneyFact` dan `ReservationJourneyFact`, serta `CreatedAt` dan `UpdatedAt` pada `AdmissionJourneyFact` di `JourneyContracts.cs`.
2. Mengisi field tersebut dari kolom `CrtDate` dan `UpdDate` pada `JourneyDal.cs` dan `JourneyDal.Batch.cs` (mengabaikan `UpdDate` jika bernilai sentinel void `3000-01-01`).
3. Mengoreksi pemetaan waktu di `Release1JourneyStageResolver.cs`:
   - `OpnameRequested` -> `opn.CreatedAt ?? opn.StatusAt`
   - `ReservationCreated` -> `rsv.CreatedAt ?? rsv.StatusAt`
   - `AdmissionRegistered` -> `admission.AdmissionAt ?? admission.CreatedAt`
   - `AdmissionUpdated` / `Cancelled` -> `admission.UpdatedAt ?? admission.AdmissionAt`
4. Menambahkan skenario pengujian unit dan integrasi untuk memverifikasi akurasi stempel waktu timeline.

## Decision

Menyetujui secara definitif hasil analisis investigasi cacat stempel waktu journey bersama seluruh resolusi kesepakatan (*closed gaps*):
1. Kontrak fact agregasi diperluas dengan field `CreatedAt` dan `UpdatedAt` dengan nilai default null agar kompatibel mundur. Entitas Waiting List tetap mempertahankan `StatusAt`.
2. Lapisan DAL memetakan nilai `CrtDate` dan `UpdDate` dari kueri SQL yang telah tersedia ke dalam kontrak fact.
3. Resolver `BuildTimeline` menggunakan field `CreatedAt` untuk event pembentukan dan `StatusAt`/`UpdatedAt` untuk event perubahan status, dengan urutan kategori tahapan admisi tetap dipertahankan.
4. Nilai waktu lama yang anomali (`UpdDate < CrtDate`) ditampilkan apa adanya tanpa manipulasi angka, dan endpoint API dipertahankan pada v1.
5. Test suite dilengkapi dengan pengujian assert timeline dengan disparitas waktu eksplisit.

## Decision Rationale

1. **Efisiensi Sumber Daya**: Mengoptimalkan kueri SQL yang sudah ada tanpa menimbulkan *overhead* penambahan kueri atau beban ke database.
2. **Kepatuhan Terhadap Integritas Audit Trail**: Memastikan kronologi peristiwa rawat inap menampilkan waktu nyata (*actual occurrence time*) yang akuntabel.
3. **Ketahanan Sistem (*Fault Tolerance*)**: Menerapkan pola *null-coalescing fallback* (`CreatedAt ?? StatusAt`), sehingga rekaman data lama atau data tanpa tanggal pembaharuan tetap dapat di-resolve dengan aman tanpa memicu *NullReferenceException*.
4. **Isolasi Risiko (*Low Risk Scope*)**: Perbaikan terbatas pada modul presentasi journey tanpa menyentuh modul transaksi utama registrasi, transaksi billing, maupun logika penentuan tahapan operasional (*stage resolution*).

## Architecture Applicability

### Decision

**ARCHITECTURE-NOT-REQUIRED**

### Rationale

Perbaikan cacat ini memenuhi kriteria `ARCHITECTURE-NOT-REQUIRED` karena:
- Tidak ada penambahan komponen atau subsistem baru.
- Tidak ada perubahan batas integrasi atau kontrak antarmuka eksternal (wire format respons JSON `GET /api/admisi-ranap/journeys/{journeyId}` tetap sama persis).
- Tidak ada perubahan skema persistensi basis data (DDL) maupun perubahan kepemilikan data.
- Pola arsitektur `JourneyFeature` yang ada saat ini (Agregasi Fact -> Resolusi Stage & Timeline) sudah sangat memadai dan tetap dipertahankan; perbaikan hanya mencakup pengkayaan field DTO internal fact dan assignment stempel waktu di resolver internal.

Oleh karena itu, tahapan SDLC dapat langsung melangkah ke penyusunan **IMPLEMENTATION-PLAN** oleh peran yang berwenang.
