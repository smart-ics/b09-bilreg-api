# ISSUE

## Metadata

ID: ISSUE-BILREG-HIDOK-BOOKING-SCHEDULE-MISMATCH-BUG-001
Type: BUG
Status: OPEN
Title: Kegagalan Validasi Jadwal pada Usecase BookingCreateFromHidokCommand Akibat Perbedaan fs_jam_mulai vs fs_jam_mulai_real

## Source

Reported By: Arf (Integrasi Sistem FO Desktop - HiDok - BilregApi)
Reported Date: 2026-11-20

## Description

Pada sistem rumah sakit, terdapat integrasi antara Front Office (FO) Desktop legacy, platform eksternal HiDok, dan backend BilregApi:
1. FO Desktop mengelola master jadwal dokter di tabel `ta_jadwal_dokter`.
2. FO Desktop mengirimkan data jadwal dokter ke platform HiDok menggunakan field `fs_jam_mulai_real`. Nilai ini disimpan oleh HiDok dan ditampilkan kepada pasien sebagai jam operasional praktek saat memilih booking.
3. Di sisi `BilregApi`, migrasi jadwal dokter dari `ta_jadwal_dokter` ke tabel `BILRG_JadwalPraktek` menggunakan field `fs_jam_mulai`. Hal ini dirancang agar tetap selaras dengan pembentukan antrian di FO Desktop (`ta_no_antrian_map`).
4. Ketika pasien melakukan booking melalui aplikasi HiDok, HiDok mengirimkan permintaan booking ke `BilregApi` melalui endpoint `POST api/Booking/createFromHidok` yang diproses oleh usecase `BookingCreateFromHidokCommand`. Nilai parameter `JamMulai` yang dikirim oleh HiDok bersumber dari `fs_jam_mulai_real`.
5. Pada kondisi jadwal dokter di mana `fs_jam_mulai != fs_jam_mulai_real` (misal jam formal administrasi 08:00 sedangkan jam mulai real dokter 08:30 atau selisih jam), validasi jadwal dokter di `BookingScheduleResolver` gagal menemukan jadwal yang cocok, sehingga sistem melempar exception `ArgumentException: "Jadwal tidak ditemukan"` dan transaksi booking eksternal gagal tersimpan.

## Desired Outcome

1. Transaksi booking dari sistem luar (HiDok) melalui `BookingCreateFromHidokCommand` dapat berhasil memvalidasi dan memetakan jadwal dokter yang dimaksud meskipun terdapat perbedaan antara jam yang dikirim HiDok (`fs_jam_mulai_real`) dan jam template di Bilreg (`fs_jam_mulai`).
2. Transaksi booking di `BilregApi` tetap tersimpan dengan jam jadwal formal (`fs_jam_mulai`) yang konsisten dengan nomor antrian dan pemetaan antrian FO Desktop (`ta_no_antrian_map`).
3. Alur pendaftaran booking internal dari screen web admisi (`BookingCreateCmd`) tetap berjalan normal tanpa dampak regresi (*zero breaking change*).

## Current Situation

1. Handler `BookingCreateFromHidokHandler` menerima `JamMulai` dari payload HiDok (yang berisi nilai `fs_jam_mulai_real`).
2. Handler memanggil `BookingScheduleResolver.Resolve(_featureResolver, _jadwalPraktekRepo, dokter, tglBerobat, jamMulai)` yang melakukan pencocokan eksak `x.JamMulai == jamMulai` terhadap master `BILRG_JadwalPraktek`.
3. Karena `BILRG_JadwalPraktek.JamMulai` menyimpan `fs_jam_mulai`, maka jika `fs_jam_mulai != fs_jam_mulai_real`, pencocokan menghasilkan `null` dan melempar `ArgumentException("Jadwal tidak ditemukan")`.
4. Akibatnya, pasien di aplikasi HiDok mengalami kegagalan saat memesan jadwal dokter yang memiliki perbedaan jam real dan jam formal.

## Evidence

1. Skema master `ta_jadwal_dokter` di database legacy menyimpan dua kolom jam:
   - `fs_jam_mulai` (jam template / formal administrasi)
   - `fs_jam_mulai_real` (jam praktek aktual yang dikirim ke HiDok)
2. Implementasi query migrasi pada [`JadwalFoDal.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/JadwalFoDal.cs#L29):
   ```sql
   aa.fs_jam_mulai AS JamMulai,
   aa.fs_jam_selesai AS JamSelesai
   FROM ta_jadwal_dokter aa
   ```
3. Implementasi handler pada [`BookingCreateFromHidokCommand.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/BookingFeature/UseCases/BookingCreateFromHidokCommand.cs#L76-L80):
   ```csharp
   var tglBerobat = DateOnly.Parse(request.TglBerobat);
   var jamMulai = TimeOnly.Parse(request.JamMulai);
   var schedule = BookingScheduleResolver.Resolve(
       _featureResolver, _jadwalPraktekRepo, dokter, tglBerobat, jamMulai);
   ```
4. Logika pencarian jadwal pada [`LegacyJadwalPraktekLookup.cs`](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/JadwalPraktekFeature/LegacyJadwalPraktekLookup.cs#L20-L23):
   ```csharp
   return listJadwal
       .Where(x => x.Hari == hari)
       .FirstOrDefault(x => x.JamMulai == jamMulai)
       ?? throw new ArgumentException("Jadwal tidak ditemukan");
   ```

## Notes

- Kolom hari pada `ta_jadwal_dokter` menggunakan format VB6 legacy (`fn_hari`, 1 = Minggu s/d 7 = Sabtu), sedangkan BilregApi menggunakan C# `DayOfWeek` (0 = Minggu s/d 6 = Sabtu).
- Masalah ini spesifik terjadi pada integrasi usecase eksternal `BookingCreateFromHidokCommand` dan tidak dialami oleh booking lokal admisi web (`BookingCreateCmd`).
- Tahapan analisis mendalam, perancangan arsitektur, dan rencana implementasi akan ditindaklanjuti pada tahap downstream (`BUG-INVESTIGATION` / `ARCHITECTURE`).
