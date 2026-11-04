# ISSUE

## Metadata

ID: ISSUE-ADM-RANAP-VOID-REG-SOURCE-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Opsi Pembatalan Otomatis Dokumen Sumber (Reservasi / Permintaan Opname) Saat Pembatalan Registrasi Rawat Inap

## Source

Reported By: User
Reported Date: 2026-11-03

## Description

Pada proses pelayanan Admisi Rawat Inap, pembatalan registrasi pasien dan pembatalan reservasi telah berjalan lancar. Namun, pengguna membutuhkan fleksibilitas tambahan saat membatalkan registrasi rawat inap yang berasal dari dokumen reservasi atau permintaan opname.

Saat dialog pop-up konfirmasi pembatalan registrasi muncul:
1. Pengguna meminta agar ditampilkan kotak centang (*checkbox*) konfirmasi untuk menentukan apakah dokumen asal (dokumen Reservasi atau Permintaan Opname) yang menjadi dasar registrasi tersebut juga ikut dibatalkan (*void*) secara otomatis.
2. Apabila kotak centang tersebut dicentang (*checked*), sistem akan secara otomatis membatalkan (*void*) dokumen reservasi atau permintaan opname terkait bersamaan dengan pembatalan registrasi.
3. Apabila kotak centang tidak dicentang (*unchecked*), sistem hanya membatalkan registrasi/admisi tanpa membatalkan dokumen reservasi atau permintaan opname.
4. Kotak centang tersebut harus dalam keadaan tercentang secara default saat dialog muncul (*default checked*), serta ditampilkan dengan ukuran font dan warna yang jelas/kontras agar pengguna langsung menyadari opsi ini aktif.

## Desired Outcome

1. Antarmuka dialog pembatalan registrasi rawat inap menyajikan kontrol kotak centang (*checkbox*) untuk mengonfirmasi pembatalan otomatis dokumen sumber (Reservasi atau Permintaan Opname).
2. Nilai awal (*default state*) dari kotak centang tersebut saat pop-up dibuka adalah tercentang (*checked*).
3. Tampilan visual label dan penanda kotak centang memiliki tipografi dan pewarnaan yang jelas, kontras, serta mudah diperhatikan oleh pengguna agar tidak terjadi ketidaksengajaan pembatalan dokumen sumber.
4. Ketika konfirmasi pembatalan registrasi dieksekusi dengan kotak centang tercentang, dokumen reservasi atau permintaan opname yang terkait secara otomatis ikut dibatalkan (*void*).
5. Ketika konfirmasi pembatalan registrasi dieksekusi dengan kotak centang tidak tercentang, dokumen reservasi atau permintaan opname tetap dipertahankan (tidak dibatalkan).

## Current Situation

1. Komponen dialog pembatalan registrasi pada frontend ([CoordinatedCancelDialog.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/CoordinatedCancelDialog.vue)) dan pengelolanya ([useJourneyActionHost.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts)) saat ini hanya menyediakan input teks untuk alasan pembatalan (*reason*). Tidak ada kontrol pilihan untuk menentukan nasib dokumen sumber.
2. Pada logika backend ([AdmCoordinatedCancelCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs)), eksekusi pembatalan saat ini secara baku selalu menjalankan pemulihan dokumen sumber (`_repo.RestoreSource`), sehingga dokumen sumber kembali ke status aktif dan tidak ada opsi dari pemanggil untuk langsung membatalkannya dalam alur yang sama.

## Evidence

- Komponen dialog pembatalan frontend:
  - [CoordinatedCancelDialog.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/ranap/CoordinatedCancelDialog.vue)
  - [useJourneyActionHost.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useJourneyActionHost.ts)
  - [AdmisiRanapConsole.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/views/AdmisiRanapConsole.vue)
- Use case pembatalan terkoordinasi backend:
  - [AdmCoordinatedCancelCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiRanapContext/AdmissionFeature/UseCases/AdmCoordinatedCancelCmd.cs)

## Notes

- Kategori: **CHANGE-REQUEST** (permintaan peningkatan fitur pada alur pembatalan admisi terkoordinasi).
- Sifat dokumen: Murni penangkapan kebutuhan pengguna (*solution-neutral*).
- Pertimbangan teknis lebih lanjut (seperti kontrak parameter API apakah menambahkan flag boolean `voidSource`, penanganan kasus ketika registrasi dibuat secara mandiri/tanpa reservasi/opname, serta integrasi audit log) akan dibahas pada tahap analisis kelayakan (*FEASIBILITY-ASSESSMENT*) dan pembaruan spesifikasi fitur oleh analis/arsitek.
