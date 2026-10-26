# ISSUE

## Metadata

ID: ISSUE-BILREG-EMR-PREFIX-ANTRIAN-001
Type: CHANGE-REQUEST
Status: OPEN
Title: Pengiriman Prefix Antrian Rawat Jalan Berdasarkan Master Ruang (HiDok_Ruang) ke EMR25 Dashboard

## Source

Reported By: User
Reported Date: 2026-10-24

## Description

Pada proses registrasi pasien di Bilreg (`b09-bilreg-api`), sistem mempublikasikan data antrian ke endpoint EMR25 (`a054_emr25_api`) di `POST api/Dashboard/addReg`. Saat ini, payload registrasi yang dikirimkan belum menyertakan atribut prefix antrian (`PrefixAntrian`).

Pengguna meminta agar pada pendaftaran Rawat Jalan (baik Walk-In maupun By Booking), sistem Bilreg menyertakan nilai `PrefixAntrian` yang bersumber dari data master ruangan (`HiDok_Ruang.PrefixAntrian`) melalui jadwal praktek dokter terkait. 

Untuk pendaftaran Rawat Darurat (IGD), atribut `PrefixAntrian` tidak perlu dikirimkan (tetap kosong) karena alur pelayanan IGD tidak menggunakan prefix antrian. 

Mekanisme penyimpanan antrian lokal sebelum pengiriman ke EMR25 harus tetap dipertahankan, tanpa mengubah alur dasar pengiriman dan tanpa memerlukan penambahan tabel atau kolom fisik baru pada database lokal.

## Desired Outcome

1. Payload pendaftaran Rawat Jalan (Walk-In dan By Booking) yang dikirim ke EMR25 melalui endpoint `POST api/Dashboard/addReg` membawa atribut `PrefixAntrian` dengan nilai kode prefix dari master ruang terkait (misalnya "A", "B").
2. Nilai `PrefixAntrian` diperoleh dari data master ruang yang terhubung dengan jadwal praktek dokter yang bersangkutan.
3. Pendaftaran Rawat Darurat (IGD) tetap berjalan normal tanpa mengirimkan prefix antrian (nilai default/kosong).
4. Data antrian registrasi Rawat Jalan tetap disimpan terlebih dahulu ke penyimpanan lokal sebelum dikirim ke EMR25 tanpa memerlukan modifikasi skema tabel lokal baru.
5. Menjaga kompatibilitas penuh (*zero breaking change*) terhadap endpoint EMR25 yang sudah mendukung properti `PrefixAntrian`.

## Current Situation

1. Kontrak DTO [IDashboardEmrAddRegService.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IDashboardEmrAddRegService.cs) pada `AddAntrianEmrByRegCommand` hanya memiliki 9 parameter dan belum menyediakan parameter `PrefixAntrian`.
2. Handler registrasi rawat jalan walk-in ([RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs)) dan rawat jalan booking ([RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs)) membentuk payload antrian EMR tanpa menyertakan `PrefixAntrian`, meskipun entitas jadwal telah berelasi dengan `RuangType` yang memuat `PrefixAntrian` dari tabel `HiDok_Ruang`.
3. Handler registrasi rawat darurat ([RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs)) saat ini memanggil `AddAntrianEmrByRegCommand` secara langsung dengan jam dan nomor antrian default tanpa prefix antrian.
4. Pada database lokal, antrian outbound EMR disimpan di tabel `BILRG_EmrAntrianOutboundQueue` menggunakan kolom `PayloadJson` bertipe `NVARCHAR(MAX)` melalui [EmrAntrianOutboundEnqueueService.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundEnqueueService.cs).
5. Pada sisi penerima `a054_emr25_api`, [AddRegCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/a054_emr25_api/Emr25.Application/DashboardContext/DashboardAgg/UseCases/AddRegCommand.cs) telah mendefinisikan parameter opsional `string PrefixAntrian = ""` dan telah siap memproses nilai tersebut ke tabel antrian dan periksa awal (`_AWAL`).

## Evidence

- Kontrak & Outbox EMR Bilreg:
  - [IDashboardEmrAddRegService.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/IDashboardEmrAddRegService.cs#L11-L22)
  - [EmrAntrianOutboundPayloadBuilder.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundPayloadBuilder.cs#L17-L27)
  - [EmrAntrianOutboundEnqueueService.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/EmrAntrianOutboundFeature/EmrAntrianOutboundEnqueueService.cs#L37-L50)
  - [BILRG_EmrAntrianOutboundQueue.sql](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.SqlDb/AdmisiContext/EmrAntrianOutboundFeature/BILRG_EmrAntrianOutboundQueue.sql#L1-L14)
- Alur Registrasi Bilreg:
  - [RegJalanWalkInCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanWalkInCommand.cs#L349-L356)
  - [RegJalanByBookingCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegJalanByBookingCmd.cs#L338-L345)
  - [RegDaruratCreateCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/AdmisiContext/RegFeature/UseCases/RegDaruratCreateCmd.cs#L301-L312)
- Master Ruang & Jadwal:
  - [RuangType.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Domain/AdmisiContext/JadwalPraktekFeature/RuangType.cs#L3-L18)
  - [RuangDal.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/RuangDal.cs#L26-L106)
  - [JadwalPraktekDal.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Infrastructure/AdmisiContext/JadwalPraktekFeature/JadwalPraktekDal.cs#L106-L127)
- Kontrak Penerima EMR25:
  - [AddRegCommand.cs](file:///d:/project_aktif/project_MyHospitalWeb/a054_emr25_api/Emr25.Application/DashboardContext/DashboardAgg/UseCases/AddRegCommand.cs#L17-L20)
  - [DashboardController.cs](file:///d:/project_aktif/project_MyHospitalWeb/a054_emr25_api/Emr25.Api/Controllers/DashboardController.cs#L29-L34)

## Notes

- Sesuai arahan bisnis, pelayanan IGD tidak menggunakan prefix antrian sehingga tidak perlu dipetakan ke master ruang.
- Penyimpanan lokal untuk Rawat Jalan tetap menggunakan mekanisme outbox yang ada (`BILRG_EmrAntrianOutboundQueue`) di mana payload tersimpan dalam bentuk JSON, sehingga tidak memerlukan perubahan skema tabel fisik lokal.
