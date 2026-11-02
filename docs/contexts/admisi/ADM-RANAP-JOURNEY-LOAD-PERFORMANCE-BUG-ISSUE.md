# ISSUE

## Metadata

ID: ISS-ADM-REG-009
Type: BUG
Status: Open
Title: Latensi Tinggi dan Timeout pada Pemuatan Daftar Pasien Aktif dan Detail Perjalanan Pasien Admisi Rawat Inap

## Source

Reported By: User Operasional Admisi Rawat Inap
Reported Date: 2026-10-26

## Description

Saat membuka layar Admisi Rawat Inap pada antarmuka frontend (`c012_myhospital_web`), pemuatan daftar pasien aktif rawat inap berjalan sangat lambat. Endpoint yang diakses adalah `bilregapi/api/admisi-ranap/journeys?scope=active&pageSize=20`.

Selanjutnya, saat pengguna memilih salah satu pasien yang ada di daftar untuk menampilkan riwayat perjalanan pasien (*patient journey*), proses pemuatan dan penampilan data detail terasa sangat lama. Endpoint yang diakses adalah `bilregapi/api/admisi-ranap/journeys/rsv%3ARSV097XD67QY`. Pada percobaan pertama (*first hit*), permintaan selalu gagal (mengalami *timeout*), dan data baru berhasil muncul setelah pengguna mencoba mengklik ulang beberapa kali.

## Desired Outcome

1. Layar Admisi Rawat Inap dapat memuat daftar pasien aktif secara responsif dan cepat pada pemanggilan pertama saat antarmuka dibuka.
2. Pemilihan kartu pasien pada daftar dapat memuat dan menampilkan informasi detail perjalanan pasien secara andal pada percobaan pertama, tanpa mengalami kegagalan *timeout* (30 detik) dan tanpa mengharuskan pengguna melakukan percobaan ulang (*retry*).

## Current Situation

1. **Pemuatan Daftar Pasien Aktif**:
   - Panggilan ke `GET bilregapi/api/admisi-ranap/journeys?scope=active&pageSize=20` membutuhkan waktu eksekusi yang lama sebelum daftar pasien dan facet tahapan tampil di layar.
2. **Pemuatan Detail Perjalanan Pasien**:
   - Panggilan ke `GET bilregapi/api/admisi-ranap/journeys/rsv%3ARSV097XD67QY` pada hit pertama selalu gagal karena melampaui batas waktu klien frontend (*timeout* 30 detik).
   - Antarmuka menampilkan status galat / gagal muat pada panel workspace perjalanan pasien.
   - Setelah dicoba kembali 2 hingga 3 kali, permintaan baru berhasil mengembalikan data dan panel detail dapat menampilkan informasi perjalanan pasien.

## Evidence

1. **Endpoint yang Terlibat**:
   - Daftar pasien aktif: `bilregapi/api/admisi-ranap/journeys?scope=active&pageSize=20`
   - Detail perjalanan pasien: `bilregapi/api/admisi-ranap/journeys/rsv%3ARSV097XD67QY`
2. **Kondisi Skema Indeks Basis Data (`HOSPITAL_PKL`)**:
   - `sp_helpindex ta_trs_billing`: Terverifikasi memiliki indeks `IX_ta_trs_billing_fs_kd_reg` dan `IX_ta_trs_billing_fs_kd_reg2` pada kolom `FS_KD_REG`.
   - `sp_helpindex BILRG_AdmAdmission`: Terverifikasi memiliki indeks `IX_bilrg_admadmission_reservationid` pada kolom `ReservationId, VodDate, RegId` dan `IX_bilrg_admadmission_opnamerequestid` pada kolom `OpnameRequestId, VodDate, RegId`.
3. **Batas Waktu Klien Frontend**:
   - Konfigurasi `bilregApi` pada frontend (`c012_myhospital_web`) menetapkan batas waktu permintaan sebesar 30.000 ms (30 detik).

## Notes

- Masalah terjadi pada interaksi antara backend `b09-bilreg-api` dan frontend `c012_myhospital_web`.
- Investigasi teknis terkait kueri proyeksi daftar dan perilaku konkurensi didokumentasikan pada `ADM-RANAP-JOURNEY-LOAD-PERFORMANCE-BUG-INVESTIGATION.md`.
