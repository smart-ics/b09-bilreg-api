# ISSUE

## Metadata

ID: ISSUE-ADMISI-PASIEN-SASET-BUG-002
Type: BUG
Status: OPEN
Title: Endpoint Persetujuan Satu Sehat Tidak Terpanggil Saat Tombol 'Setuju' Diklik pada Modal Konfirmasi Registrasi Admisi

## Source

Reported By: User / Petugas Registrasi Admisi
Reported Date: 2026-10-08
Related Feature: PASIEN-SATU-SEHAT-APPROVAL
Severity: MAJOR

## Description

Pada formulir registrasi masuk (Pendaftaran Rawat Jalan / Admisi), ketika petugas memilih pasien (pasien jaminan non-BPJS yang belum disetujui Satu Sehat), melengkapi data registrasi, dan menekan tombol "Simpan Registrasi", pop-up modal "Konfirmasi Persetujuan Satu Sehat" ditampilkan ke layar. Namun saat petugas mengklik tombol "Setuju", sistem tidak memanggil (*hit*) endpoint `{bilreg} PATCH /api/Pasien/approveUploadSaset`. Akibatnya, data persetujuan Satu Sehat untuk pasien yang didaftarkan tidak tersimpan ke dalam tabel database `tc_mr_saset`.

## Desired Outcome

Ketika petugas mengklik tombol "Setuju" pada pop-up modal Konfirmasi Persetujuan Satu Sehat, sistem secara konsisten memanggil endpoint `{bilreg} PATCH /api/Pasien/approveUploadSaset` dengan payload `pasienId` yang sesuai, sehingga data status persetujuan Satu Sehat pasien tersimpan / ter-update secara benar pada tabel database `tc_mr_saset`.

## Current Situation

Setelah mengklik tombol "Setuju" pada pop-up modal Konfirmasi Persetujuan Satu Sehat, pemanggilan endpoint `{bilreg} PATCH /api/Pasien/approveUploadSaset` tidak dieksekusi. Pendaftaran pasien mungkin berlanjut tersimpan, namun record persetujuan Satu Sehat pasien di tabel database `tc_mr_saset` tidak terbentuk atau tidak terisi.

## Evidence

- **Lokasi Layar**: Form Registrasi Masuk / Pendaftaran Rawat Jalan (Admisi).
- **Langkah Reproduksi**:
  1. Buka form registrasi masuk Admisi.
  2. Pilih data pasien (status `#SatuSehat` abu-abu / belum disetujui).
  3. Lengkapi data pendaftaran dan pilih jenis jaminan Non-BPJS (misal Umum / Tunai).
  4. Klik tombol **Simpan Registrasi**.
  5. Pop-up modal dialog "Konfirmasi Persetujuan Satu Sehat" muncul.
  6. Klik tombol **Setuju**.
- **Observasi Sistem**:
  - Pada Network tab / traffic API, endpoint `{bilreg} PATCH /api/Pasien/approveUploadSaset` tidak ter-hit.
  - Query ke database pada tabel `tc_mr_saset` untuk pasien terkait menunjukkan tidak ada data persetujuan yang tercatat.
- **Komponen & Endpoint Terkait**:
  - Form Workspace: [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue)
  - Composable Aksi: [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts)
  - Komponen Modal: [DialogPersetujuanSatuSehat.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue)
  - Target Endpoint: `{bilreg} PATCH /api/Pasien/approveUploadSaset` ([PasienController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs))
  - Target Tabel Database: `tc_mr_saset`

## Notes

- Masalah ini berdampak langsung pada kelengkapan informed consent dan kepatuhan pencatatan status integrasi Satu Sehat Kemenkes pada sistem RS.
- Sesuai batasan peran Issue Intake, investigasi teknis mendalam mengenai penyebab tidak terpanggilnya endpoint (apakah akibat event handler modal, promise resolver alur dialog, atau interaksi pemanggilan API) akan dilakukan pada tahap BUG-INVESTIGATION.
