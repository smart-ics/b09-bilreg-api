# ISSUE

## Metadata

ID: ISSUE-POLIS-LEGACY-NULL-ERROR-94-001
Type: BUG
Status: Open
Title: Muncul Run-time error '94' (Invalid use of Null) di fo-legacy saat membuka data polis asuransi

## Source

Reported By: Pengguna / Tim Operasional Front Office
Reported Date: 2026-11-27

## Description

Pengguna mengalami kegagalan saat membuka data polis asuransi di aplikasi legacy `myHospital XI - Front Office` (`fo-legacy`) pada layar `[MSB-35] Polis Asuransi`. Ketika pengguna memasukkan kode polis tertentu (misalnya `PS00183968`), aplikasi menampilkan galat `Run-time error '94': Invalid use of Null` dan gagal memuat detail polis ke dalam form.

## Desired Outcome

1. Data polis asuransi yang tersimpan di basis data dapat dibuka dan ditampilkan dengan normal di layar `[MSB-35] Polis Asuransi` pada `fo-legacy`, tanpa galat `Run-time error '94': Invalid use of Null`.
2. Seluruh data polis yang dibuat maupun diperbarui melalui sistem baru (`b09-bilreg-api` dan `c012_myhospital_web`) kompatibel dengan pembacaan data di `fo-legacy`.

## Current Situation

1. Saat pengguna memasukkan `Kode Polis` `PS00183968` di layar `[MSB-35] Polis Asuransi` (`fo-legacy`), aplikasi langsung memunculkan dialog `Run-time error '94': Invalid use of Null`.
2. Seluruh field detail pada form (`Tipe Jaminan`, `No.Polis`, `Atas Nama`, `Expired`, `Hak Kelas Ranap`) serta daftar MR di grid bawah belum terisi saat galat muncul.
3. Terdapat perbedaan kolom yang diisi/dibaca antara `fo-legacy` (`clsPolis.cls`, `frmSetupPolis.frm`) dengan `b09-bilreg-api` (`PolisDal.cs`, `PolisCoverDal.cs`) dan `c012_myhospital_web` (`CreatePolisForm.vue`), khususnya pada kolom-kolom tabel `ta_polis` dan `ta_polis_cover`.

## Evidence

1. **Screenshot Galat dari Pengguna**:
   - Layar: `[MSB-35] Polis Asuransi` (`myHospital XI - Front Office`)
   - Input: `Kode Polis` = `PS00183968`
   - Pesan galat: `Run-time error '94': Invalid use of Null`
   - Kondisi form: seluruh field detail (`Tipe Jaminan`, `No.Polis`, `Atas Nama`, `Expired`, `Hak Kelas Ranap`) masih kosong.
2. **Perilaku Pembacaan Data di `fo-legacy`**:
   - `fo-legacy/fo_trial/fo/form_billing/frmSetupPolis.frm` (baris 679): memanggil `oPolis.Kode = txtEntry(Index).text` sebelum mengisi field UI.
   - `fo-legacy/fo_trial/bilmaster_lib/class/clsPolis.cls` (`Property Let Kode`, baris 60–90): membaca kolom `ta_polis` (`fs_no_polis`, `fs_atas_nama`, `fs_kd_tipe_jaminan`, `fd_expired`, `fs_kd_kelas_ri`, `fb_cover_rj`, `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`) langsung ke variabel VB6 tanpa `ISNULL(...)` untuk sebagian besar kolom tersebut.
   - `fo-legacy/fo_trial/bilmaster_lib/class/clsPolis.cls` (`Populate`, baris 104–116): membaca `ta_polis_cover.fd_tgl_expired` dan memformatnya dengan `My.TglDMY(...)` tanpa `ISNULL(...)`.
3. **Kolom yang Ditulis oleh `b09-bilreg-api` dan `c012_myhospital_web`**:
   - `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JaminanFeature/PolisDal.cs` (`Insert`/`Update`): hanya menulis `fs_kd_polis`, `fs_no_polis`, `fs_atas_nama`, `fd_expired`, `fs_kd_tipe_jaminan`, `fb_cover_rj`, `fs_kd_kelas_ri` (tidak menyertakan `fs_kd_bu`, `fs_departemen`, `fs_no_payroll`, maupun kolom audit `crt*`/`upd*`).
   - `b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JaminanFeature/PolisCoverDal.cs` (`Insert`): hanya memetakan `fs_kd_polis`, `fs_mr`, `fs_kd_status` (tidak menyertakan `fd_tgl_expired`).
   - `worktrees/logistik-medis-bff/c012_myhospital_web/src/modules/BillingBase/components/CreatePolisForm.vue`: tidak memiliki input Badan Usaha, Departemen, No. Payroll, maupun tanggal kedaluwarsa per peserta.

## Notes

- Artifact ini bersifat *solution-neutral* sebagai pintu masuk investigasi bug. Penelusuran kondisi data aktual pada tabel `ta_polis` dan `ta_polis_cover` (termasuk record `PS00183968` dan constraint `DEFAULT` di basis data) serta penetapan perbaikannya menjadi cakupan tahap `BUG-INVESTIGATION`.
