# ISSUE

## Metadata

ID: ISSUE-ADMISI-RANAP-REKONSILIASI-ORDER-001
Type: BUG
Status: OPEN
Title: Pasien Hasil Registrasi Reservasi Menampilkan Status 'Perlu Pemeriksaan Data' dan Workspace Journey Mengalami Hang saat Diklik

## Source

Reported By: User
Reported Date: 2026-11-05

## Description

Pada modul Admisi Rawat Inap, pasien yang telah berhasil didaftarkan dari alur reservasi (contoh: pasien "SRI AJENG RETNO", No. RM `00-00-60-63` dengan nomor order `OPN0A31CDRTQ`) menampilkan badge merah dengan label "Perlu Pemeriksaan Data" dan keterangan:
> "Perlu rekonsiliasi data — Otoritas Release 1 tidak dapat menentukan tahap operasional secara aman..."

Padahal pendaftaran rawat inap telah sukses diproses dan seharusnya pasien langsung masuk ke tahap operasional "Menunggu Diterima Bangsal" (`WardAcceptanceRequired`).

Selain itu, ketika pengguna mengeklik kartu (*card*) pasien tersebut di daftar worklist panel samping kiri, panel utama (*journey workspace*) mengalami waktu muat yang sangat lama / tertahan pada tampilan kerangka (*skeleton loading / hang*) tanpa menampilkan data perjalanan pasien.

Dalam penelusuran struktur data terkait, pada entitas pendaftaran rawat inap (`BILRG_AdmAdmission`) masih tercatat penggunaan field warisan `OpnameRequestId` dan `ReservationId`, padahal sistem telah beralih menggunakan field tunggal terpadu `InpatientOrderId` (`BILRG_AdmInpatientOrder`).

## Desired Outcome

1. Pasien yang berhasil didaftarkan melalui alur pemesanan rawat inap (baik dari reservasi maupun permintaan opname) berada pada tahap operasional yang tepat sesuai alurnya, yaitu status **"Menunggu Diterima Bangsal"** (`WardAcceptanceRequired`), tanpa terkunci pada status anomali "Perlu Pemeriksaan Data" (`NeedsReconciliation`).
2. Saat kartu pasien di panel worklist diklik, panel utama (*Journey Workspace*) dapat segera menampilkan detail perjalanan pasien secara responsif dan lengkap tanpa terhenti pada status loading (*hang/skeleton*).
3. Entitas dan aliran data pendaftaran rawat inap (`BILRG_AdmAdmission`) secara konsisten menggunakan referensi order terpadu `InpatientOrderId` dan tidak lagi bergantung pada kolom warisan `OpnameRequestId` maupun `ReservationId`.

## Current Situation

1. Pengguna membuat data reservasi rawat inap baru dan sistem menerbitkan ID order (misal: `OPN0A31CDRTQ` untuk pasien "SRI AJENG RETNO").
2. Pengguna menyelesaikan registrasi admisi rawat inap untuk pasien tersebut.
3. Setelah registrasi selesai, kartu pasien muncul di panel kiri *Worklist Pasien Aktif*.
4. Pada kartu pasien, label status menunjukkan badge merah **"Perlu Pemeriksaan Data"** dengan teks rekonsiliasi Release 1, bukannya "Menunggu Diterima Bangsal".
5. Pengguna mengeklik kartu pasien tersebut untuk membuka detail perjalanan.
6. Area kerja (*Journey Workspace*) panel kanan menampilkan kerangka (*skeleton loading*) secara terus-menerus dan membutuhkan waktu sangat lama / tidak memunculkan data detail perjalanan pasien.
7. Di tabel basis data `BILRG_AdmAdmission`, data masih melibatkan kolom `OpnameRequestId` dan `ReservationId` selain `InpatientOrderId`.

## Evidence

1. **Tangkapan Layar Worklist Pasien Aktif**:
   - Lokasi file: [admisi-ranap-perlu-pemeriksaan-data-hang.png](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/docs/modules/admisi/evidence/admisi-ranap-perlu-pemeriksaan-data-hang.png)
   - Deskripsi: Tangkapan layar worklist admisi ranap menampilkan kartu pasien "SRI AJENG RETNO" (RM `00-00-60-63`) dengan badge merah **"Perlu Pemeriksaan Data"** dan keterangan *"Perlu rekonsiliasi data — Otoritas Release 1 tidak dapat menentukan tahap operasional secara aman..."*.

## Notes

- Masalah diklasifikasikan sebagai **BUG** yang berdampak pada modul backend `b09-bilreg-api` (konteks Admisi Ranap / Journey Feature) serta integrasi tampilan konsol pada `c012_myhospital_web`.
- Dokumen ini disusun secara *solution-neutral* (fokus pada deskripsi masalah, situasi teramati, dan hasil yang diharapkan) sesuai tata kelola Knowledge-Centric SDLC (ICA / `ica-issuer`).
- Rute alur kerja selanjutnya: **BUG-INVESTIGATION** oleh peran `ica-analyst` atau `ica-architect` sebelum pembaruan arsitektur dan perencanaan implementasi.
