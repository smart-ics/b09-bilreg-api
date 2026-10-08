# ISSUE

## Metadata

ID: ISSUE-ADMISI-PASIEN-SASET-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Integrasi Data Status Satu Sehat Pasien dan Konfirmasi Persetujuan Upload pada Registrasi Admisi

## Source

Reported By: User
Reported Date: 2026-10-07

## Description

Saat ini data pasien pada sistem Bilreg (`PasienModel`) belum memiliki data detail integrasi Satu Sehat (`tc_mr_saset`), sehingga informasi persetujuan (*consent*) upload maupun view Satu Sehat belum tersedia dalam agregat data sosial pasien (`PasienGetQuery`). Selain itu, pada antarmuka registrasi pasien (Admisi), petugas belum dapat melihat status aktivasi/persetujuan Satu Sehat pasien, dan belum terdapat mekanisme otomatis untuk meminta konfirmasi persetujuan (*approval upload*) data Satu Sehat saat melakukan simpan registrasi untuk pasien non-BPJS.

Diminta perubahan menyeluruh terpadu (backend dan frontend) yang mencakup:
1. Penyimpanan dan pemuatan data detail Satu Sehat pasien dari tabel `tc_mr_saset` pada agregat pasien.
2. Penyertaan informasi data Satu Sehat pada respon query detail data pasien (`PasienGetQuery`).
3. Penyediaan endpoint command untuk persetujuan upload Satu Sehat (`PasienApproveUploadSasetCmd`).
4. Visualisasi status/indikator Satu Sehat (`#SatuSehat`) pada panel data pasien di layar registrasi admisi.
5. Mekanisme persetujuan upload Satu Sehat saat simpan registrasi berdasarkan jenis jaminan:
   - Pasien BPJS (wajib upload): otomatis memanggil `PasienApproveUploadSasetCmd` tanpa konfirmasi ke user/pasien jika `IsApprovedUpload` masih false/belum disetujui.
   - Pasien Non-BPJS: memunculkan dialog konfirmasi persetujuan upload Satu Sehat jika `IsApprovedUpload` masih false/belum disetujui, dan memanggil `PasienApproveUploadSasetCmd` jika pengguna menyetujui (OK).
   - Seluruh pemanggilan bersifat *non-blocking* terhadap keberlanjutan proses simpan registrasi.

## Desired Outcome

1. **Backend - Model & Persistensi Data Satu Sehat**:
   - Model pasien (`PasienModel`) memuat informasi detail Satu Sehat (`PasienSasetModel`) yang mencakup field:
     - `SasetId` (string)
     - `IsApprovedUpload` (boolean)
     - `TglJamApprovedUpload` (datetime)
     - `FileGeneralConcentUpload` (string)
     - `IsApprovedView` (boolean)
     - `TglJamApprovedView` (datetime)
     - `FileGeneralConcentView` (string)
   - Tersedia komponen DAL (`PasienSasetDal`) yang mengakses tabel `tc_mr_saset` untuk operasi `Insert`, `Update`, `Delete`, dan `GetData` berbasis `KodeMr` / `PasienId` (relasi 1-to-1).
   - `PasienRepo` menangani pemuatan (`LoadEntity`) dan penyimpanan (`SaveChanges`) data `PasienSasetModel` secara konsisten.

2. **Backend - Query Data Pasien**:
   - Respon endpoint `PasienGetQuery` (`PasienGetResponse`) memuat objek data `PasienSaset` agar dapat dikonsumsi oleh aplikasi klien.

3. **Backend - Command Persetujuan Upload Satu Sehat**:
   - Tersedia endpoint baru melalui `PasienController` untuk mengeksekusi usecase `PasienApproveUploadSasetCmd`.
   - Command menerima parameter `PasienId` dan melakukan *insert* atau *update* status persetujuan upload (`IsApprovedUpload` & `TglJamApprovedUpload`).

4. **Frontend - Indikator Visual Satu Sehat**:
   - Pada panel ringkasan data pasien di antarmuka registrasi (kotak data pasien):
     - Menampilkan flag/label `#SatuSehat`.
     - Teks berwarna **hijau** jika pasien memiliki data Satu Sehat dan `IsApprovedUpload` bernilai true.
     - Teks berwarna **abu-abu** jika data Satu Sehat belum tersedia atau `IsApprovedUpload` belum disetujui / bernilai false.

5. **Frontend - Alur Persetujuan Upload Saat Simpan Registrasi (Berdasarkan Jaminan)**:
   - Saat pengguna menekan tombol **Simpan Registrasi**, sistem memeriksa apakah status persetujuan `IsApprovedUpload` bernilai false (atau data Satu Sehat belum ada), serta memvalidasi grup jaminan via endpoint `{GET} /JetliAPi/api/GrupJaminan/map` menggunakan `TipeJaminanId`:
     - **Pasien BPJS** (response endpoint bernilai 200 dan terdapat data mapping):
       - Pasien BPJS **wajib mengirimkan data ke Satu Sehat**.
       - Sistem secara otomatis memanggil endpoint `PasienApproveUploadSasetCmd` **tanpa konfirmasi ke user/pasien** sebelum proses simpan registrasi dijalankan.
     - **Pasien Non-BPJS** (response endpoint tidak mengembalikan data mapping):
       - Sistem memunculkan pop-up dialog konfirmasi persetujuan upload data ke Satu Sehat kepada petugas/user.
       - Jika pengguna memilih **OK**, sistem memanggil endpoint `PasienApproveUploadSasetCmd` sebelum proses simpan registrasi.
       - Jika pengguna membatalkan/menolak konfirmasi, sistem tetap melanjutkan simpan registrasi tanpa memanggil `PasienApproveUploadSasetCmd`.
   - **Toleransi Kesalahan (Non-blocking)**: Baik untuk pasien BPJS maupun non-BPJS, kegagalan respon dari pemanggilan endpoint `PasienApproveUploadSasetCmd` tidak membatalkan atau memblokir proses simpan registrasi (simpan registrasi tetap dilanjutkan).

## Current Situation

1. Agregat pasien pada backend ([PasienModel.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienModel.cs)) hanya memiliki properti flat sementara (`PatientSasetId`, `ApprovedUploadSaset`, `ApprovedViewSaset`) dan belum terhubung ke tabel `tc_mr_saset`.
2. Belum terdapat `PasienSasetDal` di Bilreg untuk mengelola tabel `tc_mr_saset`.
3. [PasienRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienRepo.cs) belum melakukan read/write ke data detail Satu Sehat saat `LoadEntity` dan `SaveChanges`.
4. Respon query pasien [PasienGetQuery.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienGetQuery.cs) belum menyertakan kontrak data Satu Sehat pasien.
5. Belum ada usecase command dan rute controller untuk menyetujui upload Satu Sehat di [PasienController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs).
6. Pada aplikasi web ([LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue)), komponen header data pasien belum memiliki visualisasi flag status `#SatuSehat`.
7. Tombol "Simpan Registrasi" langsung memproses penyimpanan registrasi tanpa memeriksa status Satu Sehat maupun grup jaminan BPJS / non-BPJS.

## Evidence

- Struktur query database `tc_mr_saset`:
  ```sql
  SELECT
      aa.KodeMr, aa.KodeSaset, 
      aa.IsApprovedUpload, aa.TglJamApprovedUpload, aa.FileGeneralConcentUpload,
      aa.IsApprovedView, aa.TglJamApprovedView, aa.FileGeneralConcentView
  FROM 
      tc_mr_saset aa
  WHERE 
      aa.KodeMr = @PasienId
  ```
- File terkait backend:
  - Domain Model: [PasienModel.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Domain/PasienContext/PasienFeature/PasienModel.cs)
  - Repository: [PasienRepo.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/PasienContext/PasienFeature/PasienRepo.cs)
  - Query Use Case: [PasienGetQuery.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienGetQuery.cs)
  - Web API Controller: [PasienController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs)
- File terkait frontend:
  - Form & Workspace Registrasi: [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue)
  - Composable Pasien Registrasi: [useRegistrasiPasien.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiPasien.ts)
  - Tipe Data Pasien: [pasien.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/BillingBase/types/pasien.ts)
- Tampilan Antarmuka Registrasi Pasien (Screenshot acuan kotak merah penempatan flag `#SatuSehat` dan tombol simpan registrasi):
  - [evidence/pasien-satusehat-ui-box.png](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/evidence/pasien-satusehat-ui-box.png)
  - Gambar menunjukkan panel ringkasan data pasien (kotak merah/oranye berisi Nama, MR, Tgl Lahir, Alamat) sebagai target penempatan indikator `#SatuSehat`, serta tombol `Simpan Registrasi` dan bagian `Jaminan Pasien` sebagai pemicu evaluasi alur approval upload Satu Sehat.
- Referensi Endpoint Eksternal:
  - Pengecekan mapping grup jaminan: `{GET} /JetliAPi/api/GrupJaminan/map` dengan query/parameter `TipeJaminanId`.

## Notes

- Kebutuhan ini disatukan dalam satu ISSUE terpadu agar fase analisis, arsitektur, dan implementasi dari backend hingga antarmuka pengguna dapat diselesaikan secara serempak dan berurutan.
- Pengecekan jaminan BPJS dilakukan melalui response endpoint `/JetliAPi/api/GrupJaminan/map`. Status non-BPJS didefinisikan apabila tidak ditemukan mapping data dari respon tersebut.
- Pasien dengan jaminan BPJS memiliki kewajiban regulasi untuk pengiriman data Satu Sehat, sehingga persetujuan upload dijalankan secara otomatis tanpa memunculkan prompt konfirmasi ke pengguna/pasien.
- Pemanggilan `PasienApproveUploadSasetCmd` sebelum registrasi didesain bersifat toleran terhadap kegagalan (non-blocking) agar tidak menghambat pelayanan registrasi pasien di loket.
