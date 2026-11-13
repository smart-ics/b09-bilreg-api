# ISSUE

## Metadata

ID: ISSUE-BILREG-ANTRIAN-POLI-PREFIX-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Penerapan Prefix Ruangan pada Sesi Antrian Poli/Dokter di Bilreg, Display Antrian, dan Worklist Rawat Jalan

## Source

Reported By: User
Reported Date: 2026-11-12

## Description

Pada proses pendaftaran rawat jalan dan pemanggilan antrian pemeriksaan dokter di poliklinik, nomor antrian pasien saat ini masih dihasilkan, disimpan, dan ditampilkan sebagai angka urut murni (`NoUrut`: `int`), misalnya nomor `3`.

Tabel master ruangan (`HiDok_Ruang`) telah memiliki kolom `prefixAntrian` (misalnya `"A"` untuk Poli Penyakit Dalam, `"B"` untuk Poli Anak, atau kode 2 huruf seperti `"OB"` untuk Obgyn), dan endpoint master ruang (`GET /api/Ruang/list` dan `GET /api/Ruang/{id}`) telah mengekspos properti tersebut. Selain itu, nilai prefix tersebut telah berhasil diteruskan ke dasbor EMR25 melalui Outbox Queue.

Pengguna meminta agar prefix ruangan dapat digunakan secara terintegrasi pada antrian pemeriksaan poli/dokter:
1. Nomor antrian dapat ditampilkan dengan format prefix (misalnya antrian nomor 3 menjadi `"A3"`).
2. Sesi antrian poli/dokter di subsistem Bilreg (`b09-bilreg-api`) mencatat snapshot prefix ruangan terkait.
3. Display monitor dan mesin pemanggilan suara klinik (`c013-kiosk-queue-display-web`) memanggil nomor antrian menggunakan kombinasi prefix dan nomor urut.
4. Tampilan antarmuka pengguna pada worklist dokter dan perawat (`c012_myhospital_web`) menyajikan nomor antrian yang memuat prefix ruangan.

## Desired Outcome

1. **Snapshot Prefix di Sesi Antrian**: Sesi antrian dokter di `BILRG_Antrian` merekam snapshot prefix ruangan dari jadwal praktek dokter yang bersangkutan saat sesi dibuka.
2. **Penyajian Format Antrian Terpadu**: Tersedia format nomor antrian yang menggabungkan prefix ruangan dan nomor urut (contoh: `"A3"`, bukan hanya angka `"3"`).
3. **Penyelarasan Display & Audio Antrian**: Layar display antrian poli dan mesin audio pemanggilan (`c013-kiosk-queue-display-web`) dapat menyuarakan huruf prefix dan nomor urut (misalnya: *"Nomor antrean A tiga, silakan menuju..."*).
4. **Konsistensi Tampilan Worklist**: Daftar antrian dan banner pasien pada modul rawat jalan (`c012_myhospital_web`) menampilkan nomor antrian berprefix secara seragam.
5. **Integritas Numerik Terjaga**: Nomor urut antrian (`NoUrut`) tetap dipertahankan bertipe numerik (`int`) untuk menjamin keandalan pengurutan, konkurensi, dan sequence generator.

## Current Situation

1. **Master Ruang & Endpoint**:
   - Kolom `HiDok_Ruang.prefixAntrian` telah tersedia dan dimodelkan pada `RuangType`, `RuangDal`, dan `RuangRepo`.
   - Endpoint `GET /api/Ruang/list`, `GET /api/Ruang/{id}`, dan `GET /api/Antrian/header/{tglYmd}/list` telah mengembalikan nilai `prefixAntrian`.
2. **Terputusnya Snapshot Sesi Dokter**:
   - Pembuatan sesi antrian dokter pada [AntrianFactory.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianFactory.cs#L25-L64) (`Create(DateOnly, JadwalPraktekType)` dan `Create(DateOnly, JadwalPraktekEffective)`) belum mengoper nilai prefix dari `Ruang` jadwal ke parameter `queuePrefixSnapshot` dari [AntrianModel.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs#L19-L30), sehingga kolom `BILRG_Antrian.QueuePrefixSnapshot` untuk sesi dokter selalu tersimpan kosong (`""`).
3. **Batasan Validasi Domain & Kolom Fisik Database**:
   - Kolom `QueuePrefixSnapshot` pada tabel database [BILRG_Antrian.sql](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/AdmisiContext/AntrianFeature/BILRG_Antrian.sql#L10) berukuran `CHAR(1)`.
   - Invariant validasi domain pada [AntrianModel.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs#L30) saat ini menggunakan `AdmissionServicePointModel.NormalizePrefix` yang membatasi nilai tepat 1 karakter (`A-Z`), sedangkan prefix ruangan poliklinik dapat memiliki panjang hingga 2 karakter alfanumerik (misalnya `"OB"`).
4. **Formatting Label**:
   - Fungsi [FormatQueueLabel](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs#L135-L141) pada `AntrianModel` saat ini memformat nomor urut dengan padding 4 digit (`$"{QueuePrefixSnapshot}{noUrut:D4}"` $\rightarrow$ `"A0003"`), belum mengakomodasi format ringkas tanpa padding (`"A3"`) yang diharapkan untuk antrian poli dokter.
5. **Display & Pemanggilan Antrian**:
   - Query snapshot monitor klinik pada [ClinicQueueDisplayQueries.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/UseCases/ClinicQueueDisplayQueries.cs#L129) telah memanggil `queue.FormatQueueLabel(call.NoUrut)`. Namun karena `QueuePrefixSnapshot` pada sesi dokter bernilai kosong, fungsi mengembalikan `null` dan selalu jatuh ke fallback `call.NoUrut.ToString()` (angka murni `"3"`).
   - Di sisi display web ([clinicAnnouncement.ts](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/apps/display-web/src/lib/clinicAnnouncement.ts#L130-L140)), parser audio telah siap membaca huruf dan angka (`parseQueueLabel`), tetapi karena label yang diterima dari API hanya angka `"3"`, audio yang dibunyikan tidak memuat suara huruf prefix.
6. **Frontend Worklist**:
   - Komponen rawat jalan pada `c012_myhospital_web` (`PatientInfoBanner.vue` dan `TindakanWorkbenchColumn.vue`) masih menampilkan `patient.noAntrian` numerik tanpa penggabungan prefix ruang.

## Evidence

- **Bilreg Core (b09-bilreg-api)**:
  - Definisi Skema Database: [BILRG_Antrian.sql](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/AdmisiContext/AntrianFeature/BILRG_Antrian.sql#L10)
  - Factory Sesi Antrian: [AntrianFactory.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianFactory.cs#L41-L64)
  - Model Sesi Antrian & Label Formatting: [AntrianModel.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs#L28-L30) & [AntrianModel.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/AntrianFeature/AntrianModel.cs#L135-L141)
  - Query Snapshot Display Poli: [ClinicQueueDisplayQueries.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/AntrianFeature/UseCases/ClinicQueueDisplayQueries.cs#L128-L130)
  - Master Ruangan DAL: [RuangDal.cs](file:///home/hlysx/projects/ics/billingweb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/RuangDal.cs#L44-L62)
- **Display & Kiosk (c013-kiosk-queue-display-web)**:
  - Audio Engine & Parser Panggilan Poli: [clinicAnnouncement.ts](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/apps/display-web/src/lib/clinicAnnouncement.ts#L128-L140)
  - Parser Label Huruf & Angka: [announcementGate.ts](file:///home/hlysx/projects/ics/billingweb/c013-kiosk-queue-display-web/apps/display-web/src/lib/announcementGate.ts#L53-L63)
- **Frontend Web (c012_myhospital_web)**:
  - Display Kolom Worklist: [TindakanWorkbenchColumn.vue](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web/src/modules/Outpatient/components/TindakanWorkbenchColumn.vue#L368-L380)
  - Banner Info Pasien: [PatientInfoBanner.vue](file:///home/hlysx/projects/ics/billingweb/c012_myhospital_web/src/modules/Outpatient/components/PatientInfoBanner.vue#L350-L360)

## Notes

- Dokumen ini merupakan artifak intake masalah/permintaan (solution-neutral) dan tidak menetapkan keputusan implementasi atau rancangan arsitektur final.
- Analisis kelayakan teknis mendalam terhadap skema database, kompatibilitas domain, dan alur data display akan dievaluasi pada tahap selanjutnya (`FEASIBILITY-ASSESSMENT`).
