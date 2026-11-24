# BUG-INVESTIGATION

## Context

Issue: [BOOKING-HIDOK-SCHEDULE-MISMATCH-BUG-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/BOOKING-HIDOK-SCHEDULE-MISMATCH-BUG-ISSUE.md)  
Problem Summary: Kegagalan validasi jadwal dokter pada usecase `BookingCreateFromHidokCommand` (`POST api/Booking/createFromHidok`) yang memicu `ArgumentException: "Jadwal tidak ditemukan"` saat aplikasi eksternal (HiDok) mengirimkan parameter `JamMulai` yang bersumber dari field `fs_jam_mulai_real`, sementara master jadwal BilregApi (`BILRG_JadwalPraktek`) menyimpan field `fs_jam_mulai`.

---

## Current State

1. **Struktur Master Jadwal di Legacy FO Desktop**:
   Pada basis data legacy rumah sakit, tabel `ta_jadwal_dokter` menyimpan jadwal praktek dokter dengan dua kolom jam yang berbeda fungsi:
   - `fs_jam_mulai`: Jam formal/administrasi yang digunakan untuk alokasi antrian dan integrasi FO Desktop (`ta_no_antrian_map`).
   - `fs_jam_mulai_real`: Jam operasional nyata dokter mulai praktek di rumah sakit.
2. **Alur Sinkronisasi FO Desktop ke HiDok**:
   Aplikasi FO Desktop mengirimkan jadwal praktek ke platform HiDok menggunakan nilai dari kolom `fs_jam_mulai_real`. Nilai ini disimpan oleh HiDok dan ditampilkan kepada pasien pada saat memilih sesi booking.
3. **Alur Migrasi Jadwal ke BilregApi**:
   Lapisan akses data `JadwalFoDal` di `b09-bilreg-api` memigrasikan jadwal dari `ta_jadwal_dokter` ke tabel `BILRG_JadwalPraktek` dengan memetakan kolom `aa.fs_jam_mulai AS JamMulai`. Hal ini dilakukan agar nomor antrian dan slot jadwal di BilregApi tetap konsisten dengan data antrian FO Desktop.
4. **Alur Booking dari HiDok ke BilregApi**:
   - Ketika pasien memesan layanan melalui HiDok, HiDok mengirimkan HTTP POST ke endpoint `api/Booking/createFromHidok` yang dipetakan ke `BookingCreateFromHidokCommand`.
   - Nilai properti `request.JamMulai` yang dikirimkan oleh HiDok adalah nilai jam operasional real (`fs_jam_mulai_real`).
   - Pada `BookingCreateFromHidokHandler`, sistem memanggil `BookingScheduleResolver.Resolve(_featureResolver, _jadwalPraktekRepo, dokter, tglBerobat, jamMulai)`.
   - Logika pencarian jadwal di `JadwalPraktekResolver` maupun `LegacyJadwalPraktekLookup` melakukan pencocokan eksak (`x.JamMulai == jamMulai`).
5. **Gejala Cacat (Defect Symptom)**:
   Pada jadwal dokter yang memiliki selisih antara jam formal dan jam real (`fs_jam_mulai != fs_jam_mulai_real`, baik selisih menit maupun selisih jam):
   - Pencarian jadwal gagal menghasilkan kecocokan (`null`).
   - Sistem melempar exception: `ArgumentException("Jadwal tidak ditemukan")`.
   - Permintaan booking gagal diproses, dan pasien di HiDok tidak dapat menyelesaikan pemesanan.

---

## Problem Analysis

### 1. Diskrepansi Semantik Jam Antar-Sistem
Terdapat perbedaan makna semantik pada field jam yang dipertukarkan:
- **HiDok**: Menganggap jam yang dikirim adalah jam operasional dokter (`fs_jam_mulai_real`).
- **BilregApi**: Memvalidasi jam berdasarkan jam formal template antrian (`fs_jam_mulai`).

Karena kedua sistem menggunakan acuan kolom yang berbeda dari tabel sumber `ta_jadwal_dokter`, pencocokan langsung secara eksak tidak dapat berlangsung tanpa lapisan translasi.

### 2. Ketiadaan Lapisan Penerjemah (Translator) pada Pintu Masuk HiDok
Saat ini `BookingCreateFromHidokHandler` langsung meneruskan parameter `request.JamMulai` tanpa memeriksa apakah jam tersebut merupakan jam real yang perlu dipetakan kembali ke jam formal Bilreg. Di sisi lain, tabel `BILRG_JadwalPraktek` tidak boleh diubah kolom jamnya menjadi jam real karena akan merusak kompatibilitas antrian dengan FO Desktop (`ta_no_antrian_map`).

### 3. Perbedaan Konvensi Index Hari Antara VB6 dan C#
Tabel sumber `ta_jadwal_dokter` menggunakan kolom `fn_hari` dengan konvensi index hari warisan Visual Basic 6 (1-based, dimulai dari Minggu):
- VB6 (`vbSunday` = 1, `vbMonday` = 2, ..., `vbSaturday` = 7).
- C# `System.DayOfWeek` (0-based, dimulai dari Minggu: `Sunday` = 0, `Monday` = 1, ..., `Saturday` = 6).

Formula pemetaan dari tanggal kunjungan pasien di C# ke kolom `fn_hari` legacy adalah:
$$\mathbf{fn\_hari = (int)tglBerobat.DayOfWeek + 1}$$

Formula ini terbukti konsisten dengan logika pemetaan balik yang sudah diterapkan pada query migrasi `JadwalFoDal.cs` (`aa.fn_hari - 1 AS Hari`).

### 4. Batasan Ruang Lingkup Masalah
Cacat ini terisolasi secara eksklusif pada pemanggilan `BookingCreateFromHidokCommand`. Pendaftaran booking lokal melalui web admisi internal (`BookingCreateCmd`) menggunakan nilai `JamMulai` yang dipilih langsung dari dropdown jadwal Bilreg (`fs_jam_mulai`), sehingga tidak mengalami masalah ini.

---

## Affected Components

1. **Layer Application (`Bilreg.Application`)**:
   - `AdmisiContext/JadwalPraktekFeature/IJadwalPraktekRepo.cs`: Penambahan method kontrak pencarian jam formal berdasarkan jam real legacy.
   - `AdmisiContext/BookingFeature/UseCases/BookingCreateFromHidokCommand.cs`: Penambahan alur resolusi translasi jam sebelum pemanggilan `BookingScheduleResolver.Resolve`.
2. **Layer Infrastructure (`Bilreg.Infrastructure`)**:
   - `AdmisiContext/JadwalPraktekFeature/IJadwalFoDal.cs` & `JadwalFoDal.cs`: Penambahan method query lookup ke tabel `ta_jadwal_dokter` untuk memetakan `(DokterId, fn_hari, JamMulaiReal)` ke `fs_jam_mulai`.
   - `AdmisiContext/JadwalPraktekFeature/JadwalPraktekRepo.cs`: Implementasi delegasi method dari interface `IJadwalPraktekRepo` ke `IJadwalFoDal`.
3. **Layer Test (`Bilreg.Test`)**:
   - `AdmisiContext/BookingFeature/BookingCreateFromHidokHandlerTest.cs`: Penyesuaian mock dan penambahan kasus uji untuk skenario jam real yang berbeda dengan jam formal.

---

## Impact Assessment

- **Business Impact**: Pasien pengguna aplikasi mobile HiDok gagal melakukan reservasi dokter spesialis yang jam prakteknya memiliki perbedaan jam mulai real vs formal, berpotensi menurunkan kepuasan pasien dan volume kunjungan pendaftaran daring.
- **Operational Impact**: Pasien yang gagal melakukan reservasi online beralih ke pendaftaran langsung (walk-in) di rumah sakit, meningkatkan beban antrian fisik di loket pendaftaran rawat jalan.
- **Technical Impact**: Terjadi exception error 400/500 pada integrasi API eksternal saat menerima data booking yang sebenarnya valid dari perspektif jadwal operasional dokter.

---

## Assumptions

- **ASM-001**: Tabel master legacy `ta_jadwal_dokter` berada pada basis data yang sama dan dapat diakses oleh `JadwalFoDal` menggunakan koneksi basis data yang sudah terkonfigurasi.
- **ASM-002**: Kombinasi `(fs_kd_dokter, fn_hari, fs_jam_mulai_real)` pada `ta_jadwal_dokter` dapat mengidentifikasi baris jadwal dokter yang dimaksud pada hari tersebut.
- **ASM-003**: Alur booking lokal admisi web (`BookingCreateCmd`) tidak diubah dan tetap mengacu pada `fs_jam_mulai`.
- **ASM-004**: Nilai `LayananId` pada entitas booking tetap diambil secara otomatis dari relasi jadwal praktek (`schedule.Effective.Layanan` atau `schedule.LegacyJadwal.Layanan`) yang berhasil di-resolve.

---

## Open Questions

Tidak ada pertanyaan terbuka yang menghambat. Seluruh mekanisme teknis translasi jam, pemetaan index hari, dan batasan implementasi telah terverifikasi secara tuntas.

---

## Alternative Evaluation

### Opsi A: Range / Proximity Matching Berdasarkan Waktu di C#
Mencari jadwal dokter di hari tersebut yang rentang waktunya melingkupi jam yang dikirim (`JamMulai <= req < JamSelesai`).
- *Kelemahan*: Di rumah sakit nyata, banyak dokter memiliki lebih dari 1 sesi praktek di hari yang sama (misal pagi dan sore) dengan selisih waktu yang cukup besar dan batas jam selesai yang bervariasi, sehingga berisiko salah memetakan sesi dokter.

### Opsi B: Penambahan Kolom `JamMulaiReal` pada Tabel `BILRG_JadwalPraktek`
Menambah kolom fisik di database BilregApi, mengubah skrip migrasi, DTO, dan domain model.
- *Kelemahan*: Membutuhkan `ALTER TABLE`, migrasi ulang basis data, dan menambah kompleksitas skema master data Bilreg yang seharusnya bersih dari atribut legacy yang tidak esensial bagi antrian internal.

### Opsi C: Translasi Jam melalui Query Lookup ke `ta_jadwal_dokter` (Terpilih)
Menggunakan `ta_jadwal_dokter` sebagai kamus penerjemah saat request dari HiDok diterima: mencari `fs_jam_mulai` berdasarkan `(fs_kd_dokter, fn_hari, fs_jam_mulai_real)`.
- *Kelebihan*:
  1. 100% deterministik dan akurat sesuai data asli di FO Desktop.
  2. Nol perubahan skema basis data (`BILRG_JadwalPraktek` tidak berubah).
  3. Mengakomodasi dokter dengan jadwal multi-sesi di hari yang sama.
  4. Terisolasi hanya pada usecase `BookingCreateFromHidokCommand`.

---

## Recommended Decision

Mengadopsi **Opsi C**: Menyediakan fungsi pencarian jam formal pada lapisan akses data `ta_jadwal_dokter`, lalu menerapkannya pada `BookingCreateFromHidokHandler` untuk menerjemahkan jam operasional HiDok menjadi jam jadwal formal sebelum jadwal di-resolve.

---

## Decision

Menetapkan perbaikan teknis terfokus pada use case intake HiDok:
1. Menyediakan method `GetJamMulaiFromReal(string dokterId, int fnHari, string jamMulaiReal)` pada `IJadwalFoDal` / `JadwalFoDal` yang menjalankan query ke `ta_jadwal_dokter`:
   ```sql
   SELECT TOP 1 fs_jam_mulai 
   FROM ta_jadwal_dokter 
   WHERE fs_kd_dokter = @dokterId 
     AND fn_hari = @fnHari 
     AND (fs_jam_mulai_real = @jamMulaiReal OR fs_jam_mulai = @jamMulaiReal)
   ```
2. Meneruskan method tersebut melalui interface `IJadwalPraktekRepo` di layer Application dan implementasinya di `JadwalPraktekRepo`.
3. Pada `BookingCreateFromHidokHandler`:
   - Konversikan tanggal kunjungan ke index hari VB6: `fnHari = (int)tglBerobat.DayOfWeek + 1`.
   - Lakukan translasi jam: panggil `_jadwalPraktekRepo.GetJamMulaiFromReal(dokter.PpaId, fnHari, request.JamMulai)`.
   - Jika ditemukan jam formal, gunakan nilai tersebut sebagai jam mulai untuk resolusi jadwal pada `BookingScheduleResolver.Resolve`. Jika tidak ditemukan, gunakan nilai asli dari request sebagai fallback.
   - Transaksi booking selanjutnya tetap tersimpan menggunakan jam formal yang selaras dengan antrian FO Desktop.

---

## Decision Rationale

1. **Integritas Sistem Warisan & Antrian**: Nilai `JamMulai` pada `BILRG_JadwalPraktek` dan `BILRG_Booking` tetap konsisten dengan data antrian FO Desktop (`ta_no_antrian_map`).
2. **Nol Perubahan Skema Basis Data (Zero Schema Impact)**: Menghindari perubahan DDL pada tabel basis data BilregApi serta tidak memerlukan proses migrasi ulang data master.
3. **Presisi Multi-Sesi**: Menjamin keakuratan pemilihan sesi bagi dokter yang memiliki jadwal lebih dari satu kali dalam satu hari.
4. **Isolasi Risiko**: Perubahan terlokalisasi hanya pada alur penerimaan booking dari HiDok, menjamin alur pendaftaran lokal admisi web berjalan tanpa potensi regresi.

---

## Architecture Applicability

### Decision

**ARCHITECTURE-NOT-REQUIRED**

### Rationale

Perbaikan cacat ini memenuhi kriteria **ARCHITECTURE-NOT-REQUIRED** karena:
- Tidak ada penambahan entitas domain baru, *value object* baru, atau perubahan *domain invariants*.
- Tidak ada perubahan skema fisik basis data (tidak ada DDL `ALTER TABLE` atau penambahan kolom baru).
- Tidak ada batas integrasi atau protokol komunikasi baru; endpoint API eksternal tetap mempertahankan kontrak request dan response yang sudah ada.
- Perubahan murni merupakan penambahan method pembacaan (query read-only) pada lapisan DAL legacy (`JadwalFoDal`) dan penyesuaian lokal alur translasi parameter pada handler `BookingCreateFromHidokHandler`.

Tahapan SDLC dapat langsung melangkah ke perancangan rencana implementasi (**IMPLEMENTATION-PLAN**) dan eksekusi kode.
