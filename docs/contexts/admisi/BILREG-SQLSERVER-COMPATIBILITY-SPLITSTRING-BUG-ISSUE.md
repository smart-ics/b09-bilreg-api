# ISSUE

## Metadata

ID: ISSUE-BILREG-SQLSERVER-COMPATIBILITY-SPLITSTRING-001
Type: BUG
Status: Open
Title: Kegagalan Eksekusi Kueri Basis Data pada Microsoft SQL Server Versi di Bawah 2016 Terkait Penggunaan STRING_SPLIT

## Source

Reported By: Klien / Pengguna Operasional Basis Data Rumah Sakit
Reported Date: 2026-11-03

## Description

Pada implementasi backend `b09-bilreg-api`, terdapat beberapa kueri SQL di layer infrastruktur yang memanggil fungsi bawaan `STRING_SPLIT` untuk memecah daftar identitas (ID) bertipe teks CSV menjadi baris data.

Fungsi `STRING_SPLIT` merupakan fitur yang baru tersedia pada Microsoft SQL Server versi 2016 (13.x) ke atas. Pada rumah sakit atau klien yang masih menjalankan Microsoft SQL Server versi sebelum 2016 (seperti SQL Server 2008, 2008 R2, 2012, atau 2014), pemanggilan fungsi ini menyebabkan eksekusi kueri gagal dengan galat basis data (*'STRING_SPLIT' is not a recognized built-in function name*).

## Desired Outcome

1. Seluruh kueri basis data pada fitur terkait (antrian loket admisi, pencarian riwayat registrasi/pasien, dan batch data perjalanan rawat inap) dapat dieksekusi secara sukses dan stabil pada basis data klien dengan Microsoft SQL Server versi di bawah 2016 (seperti SQL Server 2008 s.d. 2014) maupun versi modern.
2. Tidak ada ketergantungan wajib untuk melakukan upgrade versi mesin basis data SQL Server pada server klien.
3. Fungsionalitas pencarian data, penyaringan antrian loket, dan pemuatan batch data tetap menghasilkan data yang akurat dan mempertahankan performa responsif.

## Current Situation

1. Kueri pada layer infrastruktur backend `b09-bilreg-api` mengandalkan fungsi `STRING_SPLIT` bawaan T-SQL:
   - Pada pemfilteran titik layanan antrian loket admisi (`AdmissionQueueOperationalProjection`).
   - Pada pencarian riwayat registrasi berdasarkan kumpulan ID pasien atau ID registrasi (`RegistrationHistoryReader`).
   - Pada pemuatan batch pelengkap data perjalanan pasien rawat inap (`JourneyDal.Batch`).
2. Klien yang menjalankan Microsoft SQL Server versi sebelum 2016 mengalami kegagalan proses pada endpoint dan fitur tersebut karena fungsi `STRING_SPLIT` belum dikenali oleh mesin basis data.

## Evidence

1. **File dan Baris Kueri Terdampak pada `b09-bilreg-api`**:
   - `src/bilreg/Bilreg.Infrastructure/AdmisiContext/AntrianFeature/AdmissionQueueOperationalProjection.cs` (baris 42 dan 58):
     ```sql
     AND (@ServicePointIds IS NULL OR q.ServicePointCode IN (
         SELECT value FROM STRING_SPLIT(@ServicePointIds, ',')))
     ```
   - `src/bilreg/Bilreg.Infrastructure/AdmisiContext/RegFeature/RegistrationHistoryReader.cs` (baris 51 dan 79):
     ```sql
     AND reg.fs_mr IN (
         SELECT LTRIM(RTRIM(value))
         FROM STRING_SPLIT(@patientIdsCsv, ',')
         WHERE LTRIM(RTRIM(value)) <> ''
     )
     ```
     dan
     ```sql
     AND reg.fs_kd_reg IN (
         SELECT LTRIM(RTRIM(value))
         FROM STRING_SPLIT(@registrationIdsCsv, ',')
         WHERE LTRIM(RTRIM(value)) <> ''
     )
     ```
   - `src/bilreg/Bilreg.Infrastructure/AdmisiRanapContext/JourneyFeature/JourneyDal.Batch.cs` (baris 283 dan 294):
     ```sql
     FROM (SELECT DISTINCT LTRIM(RTRIM(value)) AS RegId FROM STRING_SPLIT(@RegIdCsv, ',') WHERE LTRIM(RTRIM(value)) <> '') x
     ```
     dan
     ```sql
     AND h.fs_kd_reg IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@RegIdCsv, ',') WHERE LTRIM(RTRIM(value)) <> '')
     ```
2. **Kesesuaian Dokumentasi Resmi**:
   - Berdasarkan dokumentasi resmi Microsoft SQL Server, fungsi bawaan `STRING_SPLIT` memerlukan level kompatibilitas basis data minimum 130 (SQL Server 2016). Versi di bawahnya (SQL Server 2008/2012/2014) tidak memiliki fungsi ini secara bawaan.

## Notes

- Masalah ini bersifat internal backend (`b09-bilreg-api`) pada layer persistensi/infrastruktur kueri, tanpa mengubah kontrak antarmuka API ke frontend (`c012_myhospital_web`).
- Downstream analysis (seperti `BUG-INVESTIGATION` / `ARCHITECTURE`) perlu memperhitungkan batasan parameter SQL Server (maksimum 2.100 parameter per kueri) dan implikasi performa saat merancang perbaikan.
