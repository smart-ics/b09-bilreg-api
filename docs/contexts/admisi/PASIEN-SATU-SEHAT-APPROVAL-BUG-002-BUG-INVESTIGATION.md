# BUG-INVESTIGATION

## Context

Issue: [PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ISSUE.md](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/docs/contexts/admisi/PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ISSUE.md)
Problem Summary: Endpoint `{bilreg} PATCH /api/Pasien/approveUploadSaset` tidak terpanggil saat tombol "Setuju" diklik pada modal konfirmasi persetujuan Satu Sehat di form registrasi admisi rawat jalan, sehingga data persetujuan Satu Sehat pasien tidak tersimpan ke database `tc_mr_saset`.

## Current State

Pada formulir pendaftaran rawat jalan (Admisi):
1. Petugas memilih pasien jaminan Non-BPJS (misalnya Umum / Tunai) yang status Satu Sehat-nya belum disetujui (`isApprovedUpload = false`).
2. Petugas melengkapi formulir dan menekan tombol **Simpan Registrasi**.
3. Sistem mendeteksi bahwa pasien Non-BPJS belum disetujui, lalu menampilkan pop-up modal dialog `DialogPersetujuanSatuSehat` melalui `requestSatuSehatApproval`.
4. Dialog muncul ke layar menampilkan nama pasien, nomor rekam medis, dan opsi tombol **Batal** serta **Setuju**.
5. Petugas mengklik tombol **Setuju**.
6. Dialog langsung tertutup seketika. Indikator loading (`isPending`) pada tombol konfirmasi tidak sempat ditampilkan.
7. Di background network traffic: endpoint `{bilreg} PATCH /api/Pasien/approveUploadSaset` sama sekali tidak pernah di-request.
8. Sistem langsung melanjutkan penyimpanan registrasi kunjungan pasien ke backend.
9. Pada tabel database `tc_mr_saset`, record persetujuan pasien tidak terbentuk/tidak terisi (`isApprovedUpload` tetap tidak aktif).

## Problem Analysis

### Temuan Analisis Alur & Bukti Eksekusi

1. **Primitif Komponen Modal Memaksa Auto-Close pada Tombol Konfirmasi**:
   - Komponen `DialogPersetujuanSatuSehat.vue` mengimplementasikan tombol "Setuju" menggunakan `<AlertDialogAction>`.
   - Di dalam pustaka UI primitif (`reka-ui` yang mendasari `shadcn-vue`), `<AlertDialogAction>` membungkus `<DialogClose>` yang memiliki handler bawaan `@click="rootContext.onOpenChange(false)"`.
   - Ketika petugas mengklik tombol "Setuju", Reka-UI secara otomatis dan sinkron mengeksekusi penutupan dialog dan mengemisikan event `@update:open="false"` ke root `<AlertDialog>`.

2. **Propagasi Event `cancel` yang Prematur pada Penutupan Dialog**:
   - Pada `DialogPersetujuanSatuSehat.vue`:
     ```ts
     function onOpenChange(value: boolean) {
       emit('update:open', value)
       if (!value) {
         emit('cancel')
       }
     }
     ```
   - Handler `onOpenChange` menyamakan setiap kejadian `open === false` sebagai aksi pembatalan (`emit('cancel')`), tanpa membedakan apakah penutupan terjadi karena klik "Batal", klik overlay/ESC, atau klik tombol "Setuju".
   - Selain itu, pada file induk [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue), komponen dipasang dengan binding:
     ```html
     @update:open="(val) => { if (!val) handleCancelSatuSehat() }"
     ```
     yang mempertegas eksekusi pembatalan setiap kali dialog tertutup.

3. **Inversi Resolusi Promise pada Alur Pra-Simpan Registrasi**:
   - Alur pra-simpan registrasi pada [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts) menahan eksekusi menggunakan Promise `requestSatuSehatApproval`:
     ```ts
     const handleCancelSatuSehat = () => {
       openDialogStates.satuSehatConfirmation = false
       if (satuSehatDialog.resolve) {
         satuSehatDialog.resolve(false)
         satuSehatDialog.resolve = null
       }
     }
     ```
   - Karena klik tombol "Setuju" langsung memicu penutupan dialog dan mengemisikan `cancel`, fungsi `handleCancelSatuSehat` langsung dieksekusi lebih dulu.
   - Akibatnya, `satuSehatDialog.resolve` dipanggil dengan nilai `false` dan di-reset menjadi `null`.
   - Sekalipun event `confirm` dipancarkan setelahnya atau secara paralel, `satuSehatDialog.resolve` sudah `null` dan Promise sudah berstatus *resolved (false)*.

4. **Bypass Logika Pemanggilan API Persetujuan**:
   - Pada `handleSubmitRegister` [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts):
     ```ts
     const approved = await requestSatuSehatApproval(patientName, targetPasienId)
     if (approved) {
       satuSehatDialog.isPending = true
       try {
         await performApprovalNonBlocking(targetPasienId)
       } finally {
         satuSehatDialog.isPending = false
         openDialogStates.satuSehatConfirmation = false
       }
     } else {
       openDialogStates.satuSehatConfirmation = false
     }
     ```
   - Karena `approved` bernilai `false`, sistem masuk ke blok `else`.
   - Fungsi `performApprovalNonBlocking(targetPasienId)` yang bertugas memanggil `approveUploadSasetMutation.mutateAsync({ pasienId: targetPasienId })` dilewati (*bypassed*).
   - Dialog tertutup, dan pendaftaran langsung melanjutkan langkah simpan registrasi pasien tanpa mencatat persetujuan Satu Sehat.

5. **Kesiapan Backend dan Client Service**:
   - Pemeriksaan pada backend [PasienController.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Api/Controllers/PasienContext/PasienController.cs) dan handler [PasienApproveUploadSasetCmd.cs](file:///d:/project_aktif/project_MyHospitalWeb/b09-bilreg-api/src/bilreg/Bilreg.Application/PasienContext/PasienFeature/PasienApproveUploadSasetCmd.cs) menunjukkan bahwa endpoint `PATCH /api/Pasien/approveUploadSaset` telah diimplementasikan dengan benar.
   - Client service [PasienService.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/BillingBase/services/PasienService.ts) (`useApproveUploadSaset`) juga telah terkonfigurasi dengan benar menuju URL `pasien/approveUploadSaset`.
   - Masalah murni terlokalisasi pada siklus hidup dan koordinasi event dialog frontend saat tombol konfirmasi diklik.

## Affected Components

- **Workflows & UI Logic**:
  - Komponen modal persetujuan informed consent: [DialogPersetujuanSatuSehat.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/DialogPersetujuanSatuSehat.vue)
  - Composable alur registrasi admisi: [useRegistrasiActions.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/useRegistrasiActions.ts)
  - Integrasi dialog workspace admisi: [LegacyRegistrationWorkspace.vue](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/LegacyRegistrationWorkspace.vue)
  - Pengujian komponen dan composable:
    - [DialogPersetujuanSatuSehat.spec.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/components/registrationAssistance/__tests__/DialogPersetujuanSatuSehat.spec.ts)
    - [useRegistrasiActions.satuSehat.spec.ts](file:///d:/project_aktif/project_MyHospitalWeb/c012_myhospital_web/src/modules/Admisi/composables/__tests__/useRegistrasiActions.satuSehat.spec.ts)

## Impact Assessment

- **Business Impact**:
  - Pelanggaran regulasi Satu Sehat Kemenkes RI: Informed consent persetujuan upload data medis yang telah disetujui pasien secara verbal di loket tidak tersimpan ke dalam database RS, sehingga rekam medis pasien tidak dapat diunggah ke platform Satu Sehat.
- **Operational Impact**:
  - Petugas loket berasumsi data persetujuan telah tersimpan karena dialog tertutup dan registrasi berhasil disimpan, padahal status persetujuan di sistem sebenarnya tidak berubah.
- **Technical Impact**:
  - Status `isApprovedUpload` pada tabel `tc_mr_saset` tetap tidak aktif / record tidak terbuat untuk seluruh pasien Non-BPJS yang didaftarkan melalui alur tersebut.

## Assumptions

- ASM-INV-001: Tombol aksi konfirmasi ("Setuju") harus bertindak sebagai tombol aksi mandiri yang tidak memicu penutupan dialog primitif secara prematur sebelum proses persetujuan asinkron selesai.
- ASM-INV-002: Penutupan dialog secara tidak langsung (misal perubahan status visibilitas) tidak boleh memicu pembatalan (`cancel`) jika sedang berada dalam fase persetujuan/penyimpanan aktif (`isPending = true`).
- ASM-INV-003: Backend endpoint `PATCH /api/Pasien/approveUploadSaset` dan client hook `useApproveUploadSaset` sudah valid dan siap mengeksekusi penyimpanan data persetujuan ke tabel `tc_mr_saset`.

## Open Questions

Tidak ada pertanyaan terbuka yang menghambat. Penyebab kegagalan pemanggilan endpoint telah teridentifikasi secara jelas melalui analisis event loop dan siklus hidup komponen dialog.

## Recommended Decision

1. Rekayasa ulang mekanisme interaksi tombol aksi pada dialog persetujuan Satu Sehat:
   - Gunakan tombol aksi biasa (bukan tombol primitif auto-close `DialogClose`/`AlertDialogAction`) untuk tombol "Setuju", sehingga dialog tetap terbuka dan menampilkan state indikator loading (`isPending`) selama proses penyimpanan berlangsung.
   - Pastikan klik pada tombol "Setuju" hanya memancarkan event `confirm` dan tidak memancarkan event `cancel` atau `update:open(false)` prematur.
2. Perbaiki penanganan event penutupan dialog pada komponen induk agar hanya mengeksekusi pembatalan jika penutupan dipicu oleh pengguna secara eksplisit (tombol Batal atau dismiss di luar proses simpan), bukan akibat transisi penutupan pasca-sukses.
3. Tutup dialog secara teratur melalui kontrol visibilitas state (`openDialogStates.satuSehatConfirmation = false`) setelah proses approval selesai.

## Decision

Memperbarui arsitektur penanganan event dan siklus keterbukaan modal konfirmasi persetujuan Satu Sehat pada modul admisi:
1. Melepaskan tombol aksi "Setuju" dari primitif auto-close dialog agar tidak memicu emisi event `cancel` dan penutupan paksa dialog.
2. Memastikan pemanggilan `confirm` mengaktifkan state `isPending`, mengeksekusi pemanggilan endpoint `PATCH /api/Pasien/approveUploadSaset` dengan parameter `pasienId`, dan menutup dialog secara teratur setelah pemanggilan selesai.
3. Menjaga isolasi pemanggilan `cancel` hanya ketika pengguna secara nyata menolak persetujuan (mengklik tombol "Batal").

## Decision Rationale

1. **Jaminan Eksekusi Informed Consent**: Menghilangkan race condition antara event auto-close dialog dan promise resolver persetujuan, sehingga endpoint approval dipanggil secara konsisten dan data persetujuan tersimpan ke `tc_mr_saset`.
2. **Kesesuaian Desain Asinkron UI**: Mengakomodasi state `isPending` dengan indikator loading pada tombol konfirmasi sesuai rancangan awal, memberikan umpan balik visual yang jelas bagi petugas loket selama komunikasi API berlangsung.
3. **Pemisahan Semantik Aksi Konfirmasi dan Pembatalan**: Mencegah event penutupan dialog umum (`onOpenChange(false)`) mengintersepsi dan membatalkan aksi konfirmasi yang sah dari pengguna.

## Architecture Applicability

### Decision

ARCHITECTURE-REQUIRED

### Rationale

Meskipun kontrak endpoint backend `{bilreg} PATCH /api/Pasien/approveUploadSaset` tidak berubah, perbaikan ini membutuhkan spesifikasi arsitektur teknis formal (`PASIEN-SATU-SEHAT-APPROVAL-BUG-002-ARCHITECTURE.md`) guna meredefinisi tata kelola siklus hidup komponen UI (decoupling dialog primitive auto-close), protokol propagasi event asinkron (`confirm` vs `cancel`), state machine asinkron (`isPending`), serta koordinasi penutupan dialog antar komponen (`DialogPersetujuanSatuSehat.vue`, `LegacyRegistrationWorkspace.vue`, dan `useRegistrasiActions.ts`). Hal ini esensial untuk menghilangkan race condition dan menjamin panduan implementasi yang deterministik bagi implementer dan reviewer.

