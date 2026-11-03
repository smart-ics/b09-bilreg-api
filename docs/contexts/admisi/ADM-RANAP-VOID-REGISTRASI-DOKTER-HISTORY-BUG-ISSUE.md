# ISSUE

## Metadata

ID: ISSUE-ADM-RANAP-VOID-REG-DOKTER-001
Type: BUG
Status: OPEN
Title: Kegagalan Pembatalan Registrasi Rawat Inap (Void Admisi) Akibat Ketiadaan Riwayat Dokter dan Inkonsistensi Syarat Validasi

## Source

Reported By: User / Operasional Admisi
Reported Date: 2026-11-03

## Description

Pada alur pelayanan Admisi Rawat Inap, saat pengguna melakukan pembatalan registrasi (void admisi) terhadap pasien yang belum memiliki transaksi tagihan (`ta_trs_billing` kosong), sistem mengalami kegagalan dan menampilkan pesan konflik ("Perjalanan berubah sejak terakhir dimuat. Data dimuat ulang — tinjau ulang sebelum melanjutkan.").

Permasalahan ini mencakup dua hal:
1. **Ketidaklengkapan Persistensi Registrasi Masuk**: Saat registrasi rawat inap dibuat (khususnya alur realisasi reservasi), data riwayat penugasan dokter pada tabel `ta_reg_history_dokter` tidak tersimpan untuk pasien terkait, meskipun pada data registrasi induk (`ta_registrasi`) kolom dokter medis terisi.
2. **Inkonsistensi Syarat Validasi Pembatalan**: Logika pembatalan registrasi terkoordinasi menolak proses pembatalan karena ketiadaan riwayat dokter aktif di `ta_reg_history_dokter` (`ActiveDoctorAssignments < 1`). Padahal sesuai ketentuan bisnis, pembatalan registrasi rawat inap seharusnya hanya dibatasi jika pasien sudah memiliki transaksi tagihan di Tata Rekening (`ta_trs_billing`). Selama pasien belum memiliki billing, registrasi seharusnya dapat dibatalkan.

## Desired Outcome

1. Registrasi pasien rawat inap yang belum memiliki transaksi billing (`ta_trs_billing` kosong) dapat dibatalkan secara terkoordinasi tanpa terhalang ketiadaan data riwayat penugasan dokter di `ta_reg_history_dokter`.
2. Alur pendaftaran admisi rawat inap (termasuk realisasi reservasi dan opname request) menjamin persistensi data riwayat penugasan dokter ke tabel `ta_reg_history_dokter` tersimpan secara lengkap dan konsisten saat registrasi terbentuk.
3. Pesan penolakan yang ditampilkan kepada pengguna di antarmuka (UI) bersifat informatif dan merefleksikan alasan bisnis yang valid apabila pembatalan ditolak (misalnya karena terhalang transaksi billing).

## Current Situation

1. Pengguna membuka antarmuka Admisi Rawat Inap dan memilih pasien aktif:
   - Nama: HERI KUSPRATIWI, NY
   - No. RM: 00-17-31-21
   - No. Registrasi: `RGA79QTXGI`
   - Sumber Awal: Reservasi (`RSV0A1I35R3W`)
   - Status: "Menunggu Diterima Bangsal" (Handover akomodasi telah dibuat, belum diterima bangsal)
2. Pasien dipastikan belum memiliki tagihan pada sistem Tata Rekening (`ta_trs_billing` count = 0).
3. Pada tabel basis data, tabel `ta_reg_history_dokter` untuk registrasi `RGA79QTXGI` tidak memiliki data sama sekali (0 baris).
4. Pengguna menekan tombol "Batalkan Registrasi" dan mengonfirmasi dengan alasan pembatalan "batal berobat".
5. Sistem memunculkan notifikasi toast: *"Perjalanan berubah sejak terakhir dimuat. Data dimuat ulang — tinjau ulang sebelum melanjutkan."* dan pembatalan tidak terjadi.
6. Permintaan mutasi `POST /api/admisi-ranap/admission/RGA79QTXGI/cancel` menerima respon HTTP `409 Conflict` dengan kode internal `CC_STATE_INCONSISTENT` karena validasi memeriksa `state.ActiveDoctorAssignments < 1`.

## Evidence

1. **Tangkapan Layar Dialog Batalkan Registrasi & Pesan Error**:
   - File: [evidence/void-registrasi-conflict-error.png](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/evidence/void-registrasi-conflict-error.png)
   - Bukti: Dialog "Batalkan Registrasi" menampilkan isian alasan pembatalan dan toast error HTTP 409 di pojok kanan atas.
2. **Tangkapan Layar Detail Perjalanan Pasien**:
   - File: [evidence/pasien-menunggu-diterima-bangsal-detail.png](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/evidence/pasien-menunggu-diterima-bangsal-detail.png)
   - Bukti: Kondisi saat ini menunjukkan "Menunggu penerimaan bangsal" untuk pasien `RGA79QTXGI` dengan pemilik tugas bangsal ST.ANNA.
3. **Pengecekan Telemetri Basis Data (`HOSPITAL_PKL`)**:
   - `ta_registrasi` (`RGA79QTXGI`): `fs_kd_jenis_reg = '1'`, `fs_kd_medis = '10127'`, `fd_tgl_void = '3000-01-01'`.
   - `ta_trs_billing` (`RGA79QTXGI`): `0` baris (tidak ada transaksi tagihan).
   - `ta_reg_history_dokter` (`RGA79QTXGI`): `0` baris (kosong).
   - `BILRG_BedWaitingList` (`RGA79QTXGI`): `WTL0A1I3RYQ9`, status `0` (Waiting).
   - `BILRG_AdmReservation` (`RSV0A1I35R3W`): status `2` (Realized).

## Notes

- Kategori: **BUG** (defisiensi persistensi data registrasi dan inkonsistensi batasan aturan pembatalan admisi).
- Komponen terkait:
  - Backend API: `b09-bilreg-api` (`AdmissionRegistrationOrchestrator`, `AdmCoordinatedCancelCmd`, `CoordinatedCancellationRepo`).
  - Frontend Web: `c012_myhospital_web` (`useJourneyActionHost` untuk mapping respon error 409).
- Dokumen ini bersifat solution-neutral sesuai prinsip Knowledge-Centric SDLC (ICA).
- Tahap berikutnya: **BUG-INVESTIGATION** oleh peran *ica-analyst* atau *ica-architect*.
