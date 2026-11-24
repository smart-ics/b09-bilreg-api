---
Title: Implementation Plan - Penerapan Prefix Ruangan pada Sesi Antrian Poli/Dokter di Bilreg, Display Antrian, dan Worklist Rawat Jalan
Code: ANTRIAN-POLI-PREFIX
Artifact: IMPLEMENTATION-PLAN
Version: 1.0
LastUpdated: 2026-11-13
Status: COMPLETED
Execution Approval: APPROVED
---

# 1. Objective

Mengimplementasikan penerapan kode prefix ruangan poliklinik (`HiDok_Ruang.prefixAntrian`) secara terintegrasi pada pembuatan sesi antrian dokter di database Bilreg (`b09-bilreg-api`), kontrak API pendaftaran dan antrian, cetakan struk tiket mandiri serta tampilan/suara pemanggilan monitor klinik (`c013-kiosk-queue-display-web`), dan tampilan nomor antrian pada modul pendaftaran serta daftar pemeriksaan dokter (`c012_myhospital_web`).

Planning Mode: FEATURE-PLANNING

Referenced artifacts:
- ISSUE: [ANTRIAN-POLI-PREFIX-ISSUE.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-ISSUE.md) (`ISSUE-BILREG-ANTRIAN-POLI-PREFIX-001`)
- FEASIBILITY-ASSESSMENT: [ANTRIAN-POLI-PREFIX-FEASIBILITY-ASSESSMENT.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-FEASIBILITY-ASSESSMENT.md) (Status: `READY-FOR-PLANNING`)
- ARCHITECTURE: [ANTRIAN-POLI-PREFIX-ARCHITECTURE.md](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/docs/contexts/admisi/ANTRIAN-POLI-PREFIX-ARCHITECTURE.md)

Architecture Applicability: ARCHITECTURE-REQUIRED

---

# 2. Planning Scope

Rencana implementasi ini mencakup 4 fase dan 8 irisan (*slices*) kerja yang tersebar di 3 repositori:

1. **`b09-bilreg-api` (Fase 1 & Fase 2)**:
   - Perluasan kolom `BILRG_Antrian.QueuePrefixSnapshot` menjadi `VARCHAR(5)` dan penambahan dukungan persistensi update bersyarat (`UpdatePrefixIfEmpty`) di `AntrianDal`.
   - Normalisasi prefix poli (0..2 karakter `[A-Z]`) dan metode pemformatan ringkas (`FormatClinicQueueLabel`) di `AntrianModel`.
   - Ekstraksi `PrefixAntrian` dari ruang jadwal praktek pada `AntrianFactory.Create`.
   - Pewarisan snapshot prefix pada use case registrasi walk-in, ubah kunjungan, dan booking.
   - Penyelarasan respons API aditif (`noAntrian: int`, `queueLabel: string`, `prefixAntrian: string`) pada `RegJalanCreateResponse`, `BookingCreateResponse`, dan `QueGetAntrianResponse`.
   - Penyelarasan kueri tampilan layar klinik (`ClinicQueueDisplayQueries`) dan pembaruan unit test terkait.
2. **`c013-kiosk-queue-display-web` (Fase 3)**:
   - Penambahan field opsional `queueLabel` dan `prefixAntrian` pada skema `packages/shared-types`.
   - Penyesuaian cetak tiket struk mandiri `kiosk-web` dan pengujian keterbacaan/lafal suara pemanggilan `display-web`.
3. **`c012_myhospital_web` (Fase 4)**:
   - Penyesuaian skema Zod DTO pada modul `Admisi` dan `Outpatient`.
   - Tampilan nomor antrian berprefix pada bukti registrasi pendaftaran dan daftar periksa/banner pasien poliklinik.

---

# 3. Dependencies

- **Prasyarat Lingkungan Database**: Server SQL Server lokal siap menjalankan migrasi DDL pada tabel `BILRG_Antrian`.
- **Ketergantungan Eksternal**: EMR25 (`a054_emr25_api`) telah memiliki dukungan `PrefixAntrian` pada endpoint `POST api/Dashboard/addReg` (diimplementasikan pada fase sebelumnya).
- **Aturan Dependensi Irisan**:
  - `Depends On` mendeklarasikan prasyarat implementasi.
  - Pemenuhan dependensi mensyaratkan irisan target berstatus implementasi `IMPLEMENTED`.
  - Irisan downstream dapat segera dieksekusi setelah irisan upstream berstatus `IMPLEMENTED` tanpa menunggu status review `GO`.

---

# 4. Progress Summary

| Phase | Implementation Status | Review Status | Progress |
|---|---|---|---|
| P1 - Backend Foundation & Persistence (`b09-bilreg-api`) | IMPLEMENTED | GO | 3/3 |
| P2 - Backend Display, Queries & API Contracts (`b09-bilreg-api`) | IMPLEMENTED | GO | 1/1 |
| P3 - Kiosk & Queue Display Integration (`c013-kiosk-queue-display-web`) | IMPLEMENTED | GO | 2/2 |
| P4 - Web Client Integration & Verification (`c012_myhospital_web`) | IMPLEMENTED | GO | 2/2 |

---

# 5. Phases

## P1 - Backend Foundation & Persistence (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P1-S01

Title: Skema Database & Persistensi DAL QueuePrefixSnapshot

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui skema tabel `BILRG_Antrian.sql` agar kolom `QueuePrefixSnapshot` bertipe `VARCHAR(5)` dan menambahkan metode `UpdatePrefixIfEmpty` pada `IAntrianDal` serta implementasi `AntrianDal` untuk mendukung backfill satu kali yang aman secara atomik.

Depends On: None

Repository: b09-bilreg-api

Completion Criteria:
- File skema [BILRG_Antrian.sql](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/AdmisiContext/AntrianFeature/BILRG_Antrian.sql) mendefinisikan `QueuePrefixSnapshot VARCHAR(5) NOT NULL DEFAULT('')`.
- Skrip migrasi DDL idempotent disiapkan untuk dijalankan pada basis data lokal/target.
- Interface `IAntrianDal` dan class `AntrianDal` memiliki metode `UpdatePrefixIfEmpty(string antrianId, string queuePrefixSnapshot)`.
- Data Access Layer mampu menyimpan dan memperbarui prefix hingga 5 karakter tanpa pemotongan data.

Notes:
- Menggunakan query eksplisit Dapper dan mematuhi kaidah `ConnStringHelper`.

Implementation Notes:
- Memperbarui skema `BILRG_Antrian.sql` dengan `QueuePrefixSnapshot VARCHAR(5) NOT NULL CONSTRAINT DF_BILRG_Antrian_QueuePrefixSnapshot DEFAULT('')`.
- Menambahkan skrip migrasi idempotent `BILRG_Antrian_M3_QueuePrefixSnapshot_Widen_Alter.sql`.
- Menambahkan method `UpdatePrefixIfEmpty(string antrianId, string queuePrefixSnapshot)` pada `IAntrianDal` dan implementasi atomik di `AntrianDal`.
- Build dan verifikasi test `AntrianRepoAreEqualTest` (7 passed) serta `AdmissionQueueMigrationManifestTest` (2 passed) lulus tanpa error.

Changed Files:
- `src/bilreg/Bilreg.SqlDb/AdmisiContext/AntrianFeature/BILRG_Antrian.sql`
- `src/bilreg/Bilreg.SqlDb/AdmisiContext/AntrianFeature/BILRG_Antrian_M3_QueuePrefixSnapshot_Widen_Alter.sql`
- `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AntrianDal.cs`

---

### P1-S02

Title: Domain Logic Normalisasi Prefix & Formatter Antrian Poli

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui entitas `AntrianModel` untuk menyediakan method validasi prefix poli (`NormalizeClinicPrefix`) yang mendukung 0..2 huruf kapital `A-Z` (sentinel `"-"` dipetakan ke `""`), metode pemformatan ringkas `FormatClinicQueueLabel(int noUrut)`, serta metode domain pembaruan prefix bersyarat `TryUpdatePrefix`.

Depends On: P1-S01

Repository: b09-bilreg-api

Completion Criteria:
- `AntrianModel.NormalizeClinicPrefix(string? raw)` mengizinkan panjang 0..2 karakter `[A-Z]`, menolak karakter non-huruf, dan memetakan sentinel `"-"` menjadi string kosong `""`.
- `AntrianModel.FormatClinicQueueLabel(int noUrut)` menghasilkan format ringkas tanpa leading zero (contoh: `"A3"`, `"OB12"`, atau `"3"` bila tanpa prefix).
- `AntrianModel.FormatQueueLabel(int noUrut)` tetap mempertahankan format 4 digit (`A0001`) untuk Loket Admisi.
- Suite pengujian unit `AntrianModelTest` lulus memverifikasi normalisasi 1-2 huruf, penolakan input tidak valid, dan pemformatan ringkas.

Notes:
- Mempertahankan aturan Tactical DDD; logika domain tidak boleh bocor ke query handler atau controller.

Implementation Notes:
- Menambahkan method domain static `NormalizeClinicPrefix(string? raw)` pada `AntrianModel` yang mendukung 0..2 huruf kapital ASCII `[A-Z]`, menolak karakter selain huruf ASCII kapital atau panjang > 2, serta memetakan sentinel `"-"`, whitespace, atau null/empty menjadi string kosong `""`.
- Menambahkan method domain `FormatClinicQueueLabel(int noUrut)` pada `AntrianModel` yang menghasilkan format ringkas tanpa leading zero (`${prefix}${noUrut}` atau `${noUrut}`).
- Mempertahankan method `FormatQueueLabel(int noUrut)` untuk format 4 digit loket admisi (`A0001`).
- Menambahkan method domain `TryUpdatePrefix(string? newPrefix)` pada `AntrianModel` yang mengizinkan pembaruan snapshot hanya ketika nilai saat ini kosong, dan menolak penimpaan jika sudah terisi (immutability).
- Mengubah setter properti `QueuePrefixSnapshot` menjadi `private set` dan menyelaraskan constructor `AntrianModel` dengan `NormalizeClinicPrefix`.
- Menambahkan suite pengujian unit `AntrianModelTest` (39 test cases) di `src/bilreg/Bilreg.Test/AdmisiContext/AntrianFeature/AntrianModelTest.cs` dan memverifikasi seluruh test lulus.

Changed Files:
- `src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs`
- `src/bilreg/Bilreg.Test/AdmisiContext/AntrianFeature/AntrianModelTest.cs`

---

### P1-S03

Title: Pewarisan Prefix Ruang Jadwal pada Factory & Alur Registrasi/Booking

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui `AntrianFactory.Create` untuk jadwal praktek agar meneruskan `Ruang.PrefixAntrian` ke instansiasi `AntrianModel`, serta menyelaraskan handler registrasi walk-in (`RegJalanWalkInCommand`), ubah kunjungan (`RegJalanUbahKunjunganCmd`), dan booking (`BookingCreateCmd`, `BookingCreateFromHidokCommand`) agar mewariskan prefix ruangan jadwal dan mengembalikan response DTO berlabel aditif.

Depends On: P1-S02

Repository: b09-bilreg-api

Completion Criteria:
- `AntrianFactory.Create(DateOnly, JadwalPraktekEffective)` dan `Create(DateOnly, JadwalPraktekType)` mengekstrak `Ruang?.PrefixAntrian` dan mengisikannya ke `queuePrefixSnapshot`.
- Record `RegJalanCreateResponse` memiliki parameter aditif opsional `string QueueLabel = ""` dan `string PrefixAntrian = ""`.
- Record `BookingCreateResponse` memiliki parameter aditif opsional `string QueueLabel = ""` dan `string PrefixAntrian = ""`.
- Handler pendaftaran dan booking mengisi `QueueLabel` pada response dengan label berprefix (misal `"A3"`).
- Jika sesi antrian yang ada sebelumnya memiliki snapshot kosong, alur pendaftaran/booking memicu `TryUpdatePrefix` untuk mengisi prefix ruangan jadwal baru.
- Suite unit test alur pendaftaran dan booking lulus verifikasi.

Notes:
- Parameter baru diletakkan di akhir record dengan nilai default guna memastikan *backward compatibility*.

---

## P2 - Backend Display, Queries & API Contracts (`b09-bilreg-api`)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P2-S04

Title: Penyelarasan Display Queries, Unit Tests & Contract Detail Antrian

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Menyelaraskan kueri tampilan layar antrian klinik (`ClinicQueueDisplayQueries`) agar memformat nomor antrian panggilan aktif dan daftar tunggu dengan format ringkas poli (`A3`), memperbarui ekspektasi unit test `ClinicQueueGetDisplaySnapshotHandlerTest`, serta menambahkan properti string aditif pada DTO `QueGetAntrianResponse`.

Depends On: P1-S03

Repository: b09-bilreg-api

Completion Criteria:
- `ClinicQueueDisplayQueries.cs` memanggil `queue?.FormatClinicQueueLabel(...)` sehingga menghasilkan format ringkas (`A3`, `OB12`).
- Unit test `ClinicQueueGetDisplaySnapshotHandlerTest.cs` diperbarui ekspektasinya dari `"A0001"` / `"A0003"` menjadi `"A1"` / `"A3"` dan seluruh pengujian lulus.
- Record `QueGetAntrianResponse` pada `QueGetAntrianQuery.cs` menambahkan properti aditif `string QueueLabel = ""` dan `string PrefixAntrian = ""`.
- Controller `RegController`, `BookingController`, dan `AntrianController` mengekspos payload JSON yang memuat properti `queueLabel` dan `prefixAntrian`.

Notes:
- Query proyeksi daftar kerja loket admisi (`AdmissionQueueOperationalProjection`) tidak diubah dan tetap mempertahankan format 4 digit.

---

## P3 - Kiosk & Queue Display Integration (`c013-kiosk-queue-display-web`)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P3-S05

Title: Pembaruan Skema Zod & Shared Types Antrian Kiosk/Display

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui tipe dan skema validasi Zod pada package `packages/shared-types` (`returnCreateWalkInSchema` dan skema detail antrian) agar menerima properti aditif opsional `queueLabel` dan `prefixAntrian` dari API Bilreg.

Depends On: P2-S04

Repository: c013-kiosk-queue-display-web

Completion Criteria:
- File `packages/shared-types/src/index.ts` dan `packages/shared-types/src/clinic.ts` mendefinisikan `queueLabel: z.string().optional()` dan `prefixAntrian: z.string().optional()`.
- Package shared-types berhasil di-build (`pnpm build` di direktori packages/shared-types) tanpa galat tipe TypeScript.

Notes:
- Menggunakan skema aditif non-breaking agar client tetap toleran terhadap payload tanpa label.

---

### P3-S06

Title: Cetak Tiket Struk Kiosk & Verifikasi Tampilan/Suara Display Klinik

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui generator dan cetakan struk registrasi mandiri di `kiosk-web` (`useKioskSelfPrint.ts`, `registrationReceipt.ts`) agar mencetak label berprefix (`A3`), serta memverifikasi monitor `display-web` dan mesin suara `clinicAnnouncement.ts` dalam memutar panggilan nomor berprefix 1-2 huruf.

Depends On: P3-S05

Repository: c013-kiosk-queue-display-web

Completion Criteria:
- Struk cetak thermal `kiosk-web` merender nomor antrian menggunakan `queueLabel || noAntrian.toString()`.
- Tampilan kartu antrian `display-web` menampilkan label antrian `"A3"` atau `"OB12"`.
- Mesin pemanggilan suara `clinicAnnouncement.ts` melalui fungsi `parseQueueLabel` membedah dan memutar urutan audio huruf dan angka secara tepat.
- Lint dan type-check aplikasi `kiosk-web` dan `display-web` berhasil tanpa kesalahan.

Notes:
- Parser suara sudah mendukung regex `^([A-Za-z]+)(\d+)$`, verifikasi difokuskan pada integritas pemutaran audio beruntun.

---

## P4 - Web Client Integration & Verification (`c012_myhospital_web`)

Implementation Status: IMPLEMENTED  
Review Status: GO

### P4-S07

Title: Integrasi Label Antrian pada Modul Admisi Web Client

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui tipe data skema pendaftaran/booking di modul `Admisi` (`registrasi.ts`, `booking.ts`) dan menampilkan nomor antrian berprefix pada layar konfirmasi/bukti registrasi rawat jalan.

Depends On: P2-S04

Repository: c012_myhospital_web

Completion Criteria:
- File `src/modules/Admisi/types/registrasi.ts` dan `booking.ts` menambahkan properti opsional `queueLabel?: string` dan `prefixAntrian?: string`.
- Komponen bukti/hasil registrasi rawat jalan menampilkan nomor antrian berprefix (`data.queueLabel || data.noAntrian`).
- Type-check `pnpm tc:app` dan `pnpm lint` pada `c012_myhospital_web` lulus tanpa galat.

Notes:
- Bekerja strictly di dalam direktori `c012_myhospital_web`.

---

### P4-S08

Title: Integrasi Label Antrian pada Modul Outpatient & Banner Pasien

Implementation Status: IMPLEMENTED  
Review Status: GO

Objective:
Memperbarui tipe data `antrianDetailSchema` pada modul `Outpatient` (`antrian.ts`) dan menyelaraskan komponen daftar tunggu/pemanggilan periksa poli (`TindakanWorkbenchColumn.vue`) serta banner identitas pasien (`PatientInfoBanner.vue`) agar menampilkan nomor antrian berprefix ringkas.

Depends On: P4-S07

Repository: c012_myhospital_web

Completion Criteria:
- File `src/modules/Outpatient/types/antrian.ts` mendefinisikan `queueLabel?: string`.
- Komponen `TindakanWorkbenchColumn.vue` menampilkan label `patient.queueLabel || patient.noAntrian`.
- Komponen `PatientInfoBanner.vue` menampilkan nomor antrian berprefix yang selaras dengan tiket fisik pasien.
- Type-check `pnpm tc:app` dan `pnpm lint` pada `c012_myhospital_web` lulus tanpa galat.
- Cache type-check dibersihkan dan diverifikasi (`rm -f node_modules/.tmp/tsconfig.app.tsbuildinfo && pnpm tc:app`).

Notes:
- Tidak menjalankan monster spec yang tidak terkait langsung; verifikasi proporsional dengan `pnpm tc:app` dan `pnpm lint`.

---

# 6. Change Log

- **2026-11-13**: Reconciliasi status artifact oleh orchestrator atas instruksi pengguna (peran architect): seluruh 8 irisan diselaraskan menjadi `IMPLEMENTED` dengan review `GO`, header fase P1–P4 dan tabel Progress Summary diperbarui, dan status plan ditetapkan `COMPLETED`. Tidak ada perubahan terhadap objective, completion criteria, atau struktur irisan.
- **2026-11-12**: Struktur awal IMPLEMENTATION-PLAN disusun oleh `ica-architect` berdasarkan target arsitektur `ANTRIAN-POLI-PREFIX-ARCHITECTURE.md` dan keputusan penutupan gap pada `ANTRIAN-POLI-PREFIX-FEASIBILITY-ASSESSMENT.md`. Status persetujuan eksekusi ditetapkan menjadi `APPROVED` untuk 4 fase dan 8 irisan kerja lintas 3 repositori.
